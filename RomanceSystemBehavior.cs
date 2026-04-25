using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class RomanceSystemBehavior : CampaignBehaviorBase
{
	private const int LoveMin = -100;

	private const int LoveMax = 100;

	private const int BridePriceTierMinusOneMin = 10000;

	private const int BridePriceTierMinusOneMax = 500000;

	private const int BridePriceTierMinusTwoMin = 500000;

	private const int BridePriceTierMinusTwoMax = 5000000;

	private const int MarriageCandidateMinAge = 18;

	private const int MarriageCandidateMaxAge = 55;

	private const int MarriageCandidateMaxAgeGap = 25;

	private static readonly string[] LoveLevelTexts = new string[10] { "极度排斥", "明显反感", "疏离警惕", "保持距离", "态度保留", "普通往来", "有些好感", "明显心动", "深度依恋", "非你不可" };

	private static readonly Regex LoveDeltaRegex = new Regex("\\[ACTION:LOVE_DELTA:([^\\]:]+):([+\\-]?\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageFormalPairRegex = new Regex("\\[ACTION:MARRIAGE_FORMAL:([^\\]:]+):([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageFormalLegacyRegex = new Regex("\\[ACTION:MARRIAGE_FORMAL:([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageElopeRegex = new Regex("\\[ACTION:MARRIAGE_ELOPE:([^\\]:]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex DivorcePairRegex = new Regex("\\[ACTION:DIVORCE:([^\\]:]+):([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private Dictionary<string, int> _privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private sealed class MarriageRecord
	{
		public string LeftHeroId = "";

		public string RightHeroId = "";

		public string Type = "";

		public string PayerClanId = "";

		public string ReceiverClanId = "";

		public int BridePriceAmount;

		public bool IsActive = true;
	}

	public static RomanceSystemBehavior Instance { get; private set; }

	private static readonly ConcurrentDictionary<string, bool> _marriagePostprocessContextBySpeaker = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

	public RomanceSystemBehavior()
	{
		Instance = this;
	}

	public static void SetMarriagePostprocessContextEnabled(Hero speaker, bool enabled)
	{
		string text = (speaker?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (enabled)
		{
			_marriagePostprocessContextBySpeaker[text] = true;
		}
		else
		{
			_marriagePostprocessContextBySpeaker.TryRemove(text, out var _);
		}
	}

	private static bool ConsumeMarriagePostprocessContextEnabled(Hero speaker)
	{
		string text = (speaker?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (_marriagePostprocessContextBySpeaker.TryGetValue(text, out var value) && value)
		{
			_marriagePostprocessContextBySpeaker.TryRemove(text, out var _);
			return true;
		}
		return false;
	}

	public override void RegisterEvents()
	{
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData("_romancePrivateLove_v1", ref _privateLove);
		if (_marriageRecordStorage == null)
		{
			_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData("_romanceMarriageRecords_v1", ref _marriageRecordStorage);
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_marriageRecordStorage == null)
		{
			_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		List<string> list = _privateLove.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			string key = list[i];
			if (string.IsNullOrWhiteSpace(key))
			{
				_privateLove.Remove(key);
				continue;
			}
			int value = ClampLove(_privateLove[key]);
			if (value == 0)
			{
				_privateLove.Remove(key);
			}
			else
			{
				_privateLove[key] = value;
			}
		}
	}

	private static int ClampLove(int value)
	{
		if (value < -100)
		{
			value = -100;
		}
		if (value > 100)
		{
			value = 100;
		}
		return value;
	}

	private static string NormalizeId(string value)
	{
		return (value ?? "").Trim();
	}

	private static string BuildMarriageRecordKey(string leftHeroId, string rightHeroId)
	{
		string text = NormalizeId(leftHeroId);
		string text2 = NormalizeId(rightHeroId);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return (string.Compare(text, text2, StringComparison.OrdinalIgnoreCase) <= 0) ? (text + "|" + text2) : (text2 + "|" + text);
	}

	private static string SerializeMarriageRecord(MarriageRecord record)
	{
		if (record == null)
		{
			return "";
		}
		return string.Join("\u001f", new string[7]
		{
			NormalizeId(record.LeftHeroId),
			NormalizeId(record.RightHeroId),
			record.Type ?? "",
			record.PayerClanId ?? "",
			record.ReceiverClanId ?? "",
			record.BridePriceAmount.ToString(),
			record.IsActive ? "1" : "0"
		});
	}

	private static bool TryDeserializeMarriageRecord(string raw, out MarriageRecord record)
	{
		record = null;
		string[] array = (raw ?? "").Split(new char[1] { '\u001f' });
		if (array.Length < 7)
		{
			return false;
		}
		record = new MarriageRecord
		{
			LeftHeroId = NormalizeId(array[0]),
			RightHeroId = NormalizeId(array[1]),
			Type = array[2] ?? "",
			PayerClanId = array[3] ?? "",
			ReceiverClanId = array[4] ?? "",
			IsActive = string.Equals(array[6], "1", StringComparison.OrdinalIgnoreCase)
		};
		int result = 0;
		int.TryParse(array[5], out result);
		record.BridePriceAmount = Math.Max(0, result);
		return !string.IsNullOrWhiteSpace(record.LeftHeroId) && !string.IsNullOrWhiteSpace(record.RightHeroId);
	}

	private static Hero FindHeroById(string heroId)
	{
		string text = NormalizeId(heroId);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			Hero hero = Hero.Find(text);
			if (hero != null)
			{
				return hero;
			}
		}
		catch
		{
		}
		try
		{
			return Hero.FindFirst((Hero x) => x != null && string.Equals(NormalizeId(x.StringId), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Clan FindClanById(string clanId)
	{
		string text = NormalizeId(clanId);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Clan.All?.FirstOrDefault((Clan x) => x != null && string.Equals(NormalizeId(x.StringId), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private void SaveMarriageRecord(MarriageRecord record)
	{
		if (record == null)
		{
			return;
		}
		string text = BuildMarriageRecordKey(record.LeftHeroId, record.RightHeroId);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (_marriageRecordStorage == null)
		{
			_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		_marriageRecordStorage[text] = SerializeMarriageRecord(record);
	}

	private MarriageRecord GetMarriageRecord(Hero left, Hero right)
	{
		string text = BuildMarriageRecordKey(left?.StringId, right?.StringId);
		if (string.IsNullOrWhiteSpace(text) || _marriageRecordStorage == null)
		{
			return null;
		}
		if (_marriageRecordStorage.TryGetValue(text, out var value) && TryDeserializeMarriageRecord(value, out var record))
		{
			return record;
		}
		return null;
	}

	private void RemoveMarriageRecord(Hero left, Hero right)
	{
		string text = BuildMarriageRecordKey(left?.StringId, right?.StringId);
		if (string.IsNullOrWhiteSpace(text) || _marriageRecordStorage == null)
		{
			return;
		}
		_marriageRecordStorage.Remove(text);
	}

	private static string GetHeroDisplayWithClan(Hero hero)
	{
		if (hero == null)
		{
			return "未知对象";
		}
		return GetClanNameSafe(hero) + "的" + (hero.Name?.ToString() ?? hero.StringId ?? "未知");
	}

	private static string BuildBridePriceSummary(MarriageRecord record)
	{
		if (record == null || record.BridePriceAmount <= 0)
		{
			return "无明确彩礼记录";
		}
		Clan clan = FindClanById(record.PayerClanId);
		Clan clan2 = FindClanById(record.ReceiverClanId);
		string text = clan?.Name?.ToString() ?? "未知家族";
		string text2 = clan2?.Name?.ToString() ?? "未知家族";
		return $"由{text}向{text2}支付彩礼 {record.BridePriceAmount:N0} 第纳尔";
	}

	private static string BuildMarriageHeroFactLine(Hero hero, string tokenName)
	{
		if (hero == null)
		{
			return "";
		}
		return "- " + GetHeroDisplayWithClan(hero) + $"（{tokenName}={hero.StringId}，年龄={hero.Age:0.#}）";
	}

	private static string BuildFactBlock(string title, List<string> lines, string emptyText = "（无）")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(title);
		if (lines == null || lines.Count <= 0)
		{
			stringBuilder.Append(emptyText);
		}
		else
		{
			for (int i = 0; i < lines.Count; i++)
			{
				if (!string.IsNullOrWhiteSpace(lines[i]))
				{
					stringBuilder.AppendLine(lines[i]);
				}
			}
		}
		return stringBuilder.ToString().Trim();
	}

	private IEnumerable<string> EnumerateFormalMarriageCandidatePairLines(Hero speaker)
	{
		Clan clan = Clan.PlayerClan;
		Clan clan2 = speaker?.Clan;
		if (clan == null || clan2 == null)
		{
			yield break;
		}
		List<Hero> list = GetClanMembersCompat(clan).Where(IsFormalMarriageCandidate).ToList();
		List<Hero> list2 = GetClanMembersCompat(clan2).Where(IsFormalMarriageCandidate).ToList();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Hero item in list)
		{
			foreach (Hero item2 in list2)
			{
				if (item == null || item2 == null || item == item2 || !IsFormalMarriagePairCompatible(item, item2, out var _))
				{
					continue;
				}
				string text = NormalizeId(item.StringId) + "|" + NormalizeId(item2.StringId);
				if (hashSet.Add(text))
				{
					yield return $"- {GetHeroDisplayWithClan(item)} 与 {GetHeroDisplayWithClan(item2)} 可正规成婚（playerClanHeroId={item.StringId}，targetHeroId={item2.StringId}）";
				}
			}
		}
	}

	private IEnumerable<string> EnumerateActiveMarriageSituationLines(Hero speaker)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Clan clan = Hero.MainHero?.Clan;
		Clan clan2 = speaker?.Clan;
		if (clan != null && clan2 != null)
		{
			foreach (Hero item in GetClanMembersCompat(clan))
			{
				Hero spouse = item?.Spouse;
				if (item == null || spouse == null || spouse.Clan != clan2)
				{
					continue;
				}
				string text = BuildMarriageRecordKey(item.StringId, spouse.StringId);
				if (hashSet.Add(text))
				{
					MarriageRecord marriageRecord = GetMarriageRecord(item, spouse);
					yield return "- " + GetHeroDisplayWithClan(item) + " 与 " + GetHeroDisplayWithClan(spouse) + " 已成婚；" + BuildBridePriceSummary(marriageRecord) + "。";
				}
			}
		}
		if (speaker != null && Hero.MainHero != null && (speaker.Spouse == Hero.MainHero || Hero.MainHero.Spouse == speaker))
		{
			string text2 = BuildMarriageRecordKey(Hero.MainHero.StringId, speaker.StringId);
			if (hashSet.Add(text2))
			{
				MarriageRecord marriageRecord2 = GetMarriageRecord(Hero.MainHero, speaker);
				yield return "- " + GetHeroDisplayWithClan(Hero.MainHero) + " 与 " + GetHeroDisplayWithClan(speaker) + " 已成婚；" + BuildBridePriceSummary(marriageRecord2) + "。";
			}
		}
	}

	private string BuildClanMarriageSituationPrompt(Hero speaker)
	{
		try
		{
			List<string> list = EnumerateActiveMarriageSituationLines(speaker).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<string> list2 = GetClanMembersCompat(Clan.PlayerClan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "playerClanHeroId")).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<string> list3 = ((speaker?.Clan != null) ? GetClanMembersCompat(speaker.Clan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "targetHeroId")).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>());
			List<string> list4 = EnumerateFormalMarriageCandidatePairLines(speaker).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【玩家家族可婚配未婚成员（事实清单）】");
			if (list2.Count <= 0)
			{
				stringBuilder.AppendLine("（无）");
			}
			else
			{
				for (int i = 0; i < list2.Count; i++)
				{
					stringBuilder.AppendLine(list2[i]);
				}
			}
			stringBuilder.AppendLine("【对方家族可婚配未婚成员（事实清单）】");
			if (list3.Count <= 0)
			{
				stringBuilder.AppendLine("（无）");
			}
			else
			{
				for (int j = 0; j < list3.Count; j++)
				{
					stringBuilder.AppendLine(list3[j]);
				}
			}
			stringBuilder.AppendLine("【当前可直接成立的正规婚配组合（事实清单）】");
			if (list4.Count <= 0)
			{
				stringBuilder.AppendLine("（无）");
			}
			else
			{
				for (int k = 0; k < list4.Count; k++)
				{
					stringBuilder.AppendLine(list4[k]);
				}
			}
			stringBuilder.AppendLine("【你们两家的现有婚姻（事实清单）】");
			if (list.Count <= 0)
			{
				stringBuilder.AppendLine("当前未发现玩家家族与你方家族之间的已成婚记录。");
			}
			else
			{
				for (int l = 0; l < list.Count; l++)
				{
					stringBuilder.AppendLine(list[l]);
				}
			}
			stringBuilder.Append("正规结婚只能在“当前可直接成立的正规婚配组合”里选人。离婚只能在“现有婚姻”里选人。若NPC愿意把先前收取的彩礼退还给玩家，可按以上明细协商返还金额；也可以明确表示不返还。不要把玩家向NPC还钱当作自动离婚结果。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private string BuildMarriagePostprocessPlayerCandidatesBlock(Hero speaker)
	{
		try
		{
			List<string> list = GetClanMembersCompat(Clan.PlayerClan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "playerClanHeroId")).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			return BuildFactBlock("【玩家家族可婚配未婚成员（事实清单）】", list);
		}
		catch
		{
			return "【玩家家族可婚配未婚成员（事实清单）】\n（无）";
		}
	}

	private string BuildMarriagePostprocessTargetCandidatesBlock(Hero speaker)
	{
		try
		{
			List<string> list = ((speaker?.Clan != null) ? GetClanMembersCompat(speaker.Clan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "targetHeroId")).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>());
			return BuildFactBlock("【对方家族可婚配未婚成员（事实清单）】", list);
		}
		catch
		{
			return "【对方家族可婚配未婚成员（事实清单）】\n（无）";
		}
	}

	private string BuildMarriagePostprocessFactHintBlock(Hero speaker)
	{
		try
		{
			List<string> list = EnumerateFormalMarriageCandidatePairLines(speaker).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<string> list2 = EnumerateActiveMarriageSituationLines(speaker).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【当前可直接成立的正规婚配组合（事实清单）】");
			if (list.Count <= 0)
			{
				stringBuilder.AppendLine("（无）");
			}
			else
			{
				for (int i = 0; i < list.Count; i++)
				{
					stringBuilder.AppendLine(list[i]);
				}
			}
			stringBuilder.AppendLine("【你们两家的现有婚姻（事实清单）】");
			if (list2.Count <= 0)
			{
				stringBuilder.AppendLine("当前未发现玩家家族与你方家族之间的已成婚记录。");
			}
			else
			{
				for (int j = 0; j < list2.Count; j++)
				{
					stringBuilder.AppendLine(list2[j]);
				}
			}
			stringBuilder.Append("正规结婚只能在“当前可直接成立的正规婚配组合”里选人。离婚只能在“现有婚姻”里选人。若NPC愿意把先前收取的彩礼退还给玩家，可按以上明细协商返还金额；也可以明确表示不返还。不要把玩家向NPC还钱当作自动离婚结果。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "【当前可直接成立的正规婚配组合（事实清单）】\n（无）\n【你们两家的现有婚姻（事实清单）】\n当前未发现玩家家族与你方家族之间的已成婚记录。";
		}
	}

	private static int ToLoveLevelIndex(int value)
	{
		double num = ((double)ClampLove(value) + 100.0) / 200.0;
		int num2 = (int)Math.Floor(num * 10.0) + 1;
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	private static string BuildLoveKey(Hero hero)
	{
		string text = (hero?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	public static string GetPrivateLoveLevelText(int value)
	{
		return LoveLevelTexts[ToLoveLevelIndex(value) - 1];
	}

	public int GetPrivateLove(Hero hero)
	{
		if (hero == null)
		{
			return 0;
		}
		string text = BuildLoveKey(hero);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_privateLove.TryGetValue(text, out var value))
		{
			return ClampLove(value);
		}
		return 0;
	}

	public void SetPrivateLove(Hero hero, int value, string reason)
	{
		if (hero == null)
		{
			return;
		}
		string text = BuildLoveKey(hero);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		int privateLove = GetPrivateLove(hero);
		int num = ClampLove(value);
		if (num == 0)
		{
			_privateLove.Remove(text);
		}
		else
		{
			_privateLove[text] = num;
		}
		Logger.Log("Romance", $"hero={hero.StringId} reason={reason} love={privateLove}->{num}");
		int num2 = num - privateLove;
		if (num2 == 0)
		{
			return;
		}
		int num3 = 0;
		if (num2 > 0)
		{
			try
			{
				num3 = RewardSystemBehavior.Instance?.AdjustPersonalTrustWholeDeltaForExternal(hero, num2, "private_love_sync_gain") ?? 0;
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] private love sync trust failed: " + ex.Message);
			}
		}
		try
		{
			string text2 = (num2 > 0 ? "+" : "") + num2;
			Color color = ((num2 > 0) ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u));
			InformationManager.DisplayMessage(new InformationMessage("[私人关系] " + hero.Name + " " + text2 + "（当前" + num + "）", color));
			if (num3 > 0)
			{
				int npcTrust = RewardSystemBehavior.Instance?.GetNpcTrust(hero) ?? num3;
				InformationManager.DisplayMessage(new InformationMessage("【信任变化】" + hero.Name + " 对你的个人信任 +" + num3 + "（因私人关系提升，当前" + npcTrust + "）", Color.FromUint(4278242559u)));
			}
		}
		catch
		{
		}
	}

	public void AdjustPrivateLove(Hero hero, int delta, string reason)
	{
		if (hero == null || delta == 0)
		{
			return;
		}
		SetPrivateLove(hero, GetPrivateLove(hero) + delta, reason);
	}

	private static int GetRelationSafe(Hero left, Hero right)
	{
		try
		{
			if (left == null || right == null)
			{
				return 0;
			}
			return left.GetRelation(right);
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsMarriageableHero(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (hero.IsPrisoner)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Age < 18f)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static bool CanPlayerMarryTarget(Hero targetHero, out string reason)
	{
		reason = "";
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || targetHero == null)
		{
			reason = "未找到婚配双方。";
			return false;
		}
		if (mainHero == targetHero)
		{
			reason = "不能和自己结婚。";
			return false;
		}
		if (!IsMarriageableHero(mainHero))
		{
			reason = "玩家当前状态不满足结婚条件。";
			return false;
		}
		if (!IsMarriageableHero(targetHero))
		{
			reason = "目标当前状态不满足结婚条件。";
			return false;
		}
		return true;
	}

	private static bool TryApplyMarriageAction(Hero left, Hero right, out string failReason)
	{
		failReason = "";
		string text = DescribeHeroMarriageState(left);
		string text2 = DescribeHeroMarriageState(right);
		try
		{
			string marriageSuitabilityHint = BuildMarriageSuitabilityHint(left, right);
			bool flag = ShouldForceMarriageDespiteEncounterRuntimeBlockers(left, right, out var forceReason);
			if (flag)
			{
				Logger.Log("Romance", "[WARN] MarriageAction switching to forced encounter-safe path: " + forceReason);
				Logger.Log("Romance", "[WARN] MarriageAction left=" + text);
				Logger.Log("Romance", "[WARN] MarriageAction right=" + text2);
				return TryForceApplyMarriageAction(left, right, out failReason);
			}
			if (!string.IsNullOrWhiteSpace(marriageSuitabilityHint))
			{
				failReason = marriageSuitabilityHint;
				Logger.Log("Romance", "[WARN] MarriageAction blocked by precheck: " + marriageSuitabilityHint);
				Logger.Log("Romance", "[WARN] MarriageAction left=" + text);
				Logger.Log("Romance", "[WARN] MarriageAction right=" + text2);
				return false;
			}
			Type type = typeof(ChangeRelationAction).Assembly.GetType("TaleWorlds.CampaignSystem.Actions.MarriageAction");
			if (type == null)
			{
				failReason = "未找到原版 MarriageAction。";
				return false;
			}
			MethodInfo method = type.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static, null, new Type[3]
			{
				typeof(Hero),
				typeof(Hero),
				typeof(bool)
			}, null);
			if (method != null)
			{
				method.Invoke(null, new object[3] { left, right, true });
				if (left?.Spouse == right && right?.Spouse == left)
				{
					return true;
				}
				failReason = "MarriageAction 未抛异常，但婚姻未实际建立。";
				Logger.Log("Romance", "[WARN] MarriageAction returned without marriage state change.");
				Logger.Log("Romance", "[WARN] MarriageAction left(after)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(after)=" + DescribeHeroMarriageState(right));
				return false;
			}
			MethodInfo method2 = type.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static, null, new Type[2]
			{
				typeof(Hero),
				typeof(Hero)
			}, null);
			if (method2 != null)
			{
				method2.Invoke(null, new object[2] { left, right });
				if (left?.Spouse == right && right?.Spouse == left)
				{
					return true;
				}
				failReason = "MarriageAction 未抛异常，但婚姻未实际建立。";
				Logger.Log("Romance", "[WARN] MarriageAction returned without marriage state change.");
				Logger.Log("Romance", "[WARN] MarriageAction left(after)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(after)=" + DescribeHeroMarriageState(right));
				return false;
			}
			failReason = "MarriageAction.Apply 签名不匹配。";
			return false;
		}
		catch (TargetInvocationException ex)
		{
			Exception ex2 = ex.InnerException ?? ex;
			if (left?.Spouse == right && right?.Spouse == left)
			{
				Logger.Log("Romance", "[WARN] MarriageAction threw after marriage state changed: " + ex2);
				Logger.Log("Romance", "[WARN] MarriageAction left(partial-success)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(partial-success)=" + DescribeHeroMarriageState(right));
				TryEmitMarriageFallbackNotifications(left, right);
				return true;
			}
			failReason = ex2.GetType().Name + ": " + ex2.Message;
			Logger.Log("Romance", "[ERROR] MarriageAction invoke failed: " + ex2);
			Logger.Log("Romance", "[ERROR] MarriageAction left=" + text);
			Logger.Log("Romance", "[ERROR] MarriageAction right=" + text2);
			return false;
		}
		catch (Exception ex)
		{
			if (left?.Spouse == right && right?.Spouse == left)
			{
				Logger.Log("Romance", "[WARN] MarriageAction wrapper threw after marriage state changed: " + ex);
				Logger.Log("Romance", "[WARN] MarriageAction left(partial-success)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(partial-success)=" + DescribeHeroMarriageState(right));
				TryEmitMarriageFallbackNotifications(left, right);
				return true;
			}
			failReason = ex.GetType().Name + ": " + ex.Message;
			Logger.Log("Romance", "[ERROR] MarriageAction invoke wrapper failed: " + ex);
			Logger.Log("Romance", "[ERROR] MarriageAction left=" + text);
			Logger.Log("Romance", "[ERROR] MarriageAction right=" + text2);
			return false;
		}
	}

	private static bool IsMarriageEncounterRuntimeContext()
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (PlayerEncounter.Current != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (Mission.Current != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool HasEncounterRuntimeMarriageBlocker(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (hero.PartyBelongedTo?.MapEvent != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.PartyBelongedTo?.Army != null)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool ShouldForceMarriageDespiteEncounterRuntimeBlockers(Hero left, Hero right, out string reason)
	{
		reason = "";
		if (!IsMarriageEncounterRuntimeContext())
		{
			return false;
		}
		if (!HasEncounterRuntimeMarriageBlocker(left) && !HasEncounterRuntimeMarriageBlocker(right))
		{
			return false;
		}
		try
		{
			if (Campaign.Current?.Models?.MarriageModel?.IsCoupleSuitableForMarriage(left, right) == true)
			{
				return false;
			}
		}
		catch
		{
		}
		if (!TryValidateMarriagePairIgnoringRuntimeBlockers(left, right, out reason))
		{
			return false;
		}
		reason = "当前处于会面/遭遇流程，且仅被地图事件/军团等运行时状态拦截；已改走强制婚姻执行路径。";
		return true;
	}

	private static bool TryValidateMarriagePairIgnoringRuntimeBlockers(Hero left, Hero right, out string reason)
	{
		reason = "";
		if (left == null || right == null)
		{
			reason = "未找到婚配双方。";
			return false;
		}
		var marriageModel = Campaign.Current?.Models?.MarriageModel;
		if (marriageModel == null)
		{
			reason = "未找到原版 MarriageModel。";
			return false;
		}
		if (!marriageModel.IsClanSuitableForMarriage(left.Clan) || !marriageModel.IsClanSuitableForMarriage(right.Clan))
		{
			reason = "有一方家族当前不适合结婚。";
			return false;
		}
		try
		{
			if (left.Clan?.Leader == left && right.Clan?.Leader == right)
			{
				reason = "双方都是族长。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (left.IsFemale == right.IsFemale)
			{
				reason = "双方性别相同。";
				return false;
			}
		}
		catch
		{
		}
		if (!CanHeroMarryIgnoringRuntimeBlockers(left, out reason) || !CanHeroMarryIgnoringRuntimeBlockers(right, out reason))
		{
			return false;
		}
		try
		{
			MethodInfo methodInfo = marriageModel.GetType().GetMethod("AreHeroesRelated", BindingFlags.Instance | BindingFlags.NonPublic);
			if (methodInfo == null)
			{
				methodInfo = typeof(DefaultMarriageModel).GetMethod("AreHeroesRelated", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			if (methodInfo != null && methodInfo.Invoke(marriageModel, new object[3] { left, right, 3 }) is bool flag && flag)
			{
				reason = "双方存在近亲关系。";
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[WARN] AreHeroesRelated reflection check failed: " + ex);
		}
		try
		{
			Hero courtedHeroInOtherClan = Romance.GetCourtedHeroInOtherClan(left, right);
			if (courtedHeroInOtherClan != null && courtedHeroInOtherClan != right)
			{
				reason = "玩家方当前已有其他婚配对象。";
				return false;
			}
			Hero courtedHeroInOtherClan2 = Romance.GetCourtedHeroInOtherClan(right, left);
			if (courtedHeroInOtherClan2 != null && courtedHeroInOtherClan2 != left)
			{
				reason = "对方当前已有其他婚配对象。";
				return false;
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("Romance", "[WARN] Courtship conflict check failed: " + ex2);
		}
		return true;
	}

	private static bool CanHeroMarryIgnoringRuntimeBlockers(Hero hero, out string reason)
	{
		reason = "";
		if (hero == null)
		{
			reason = "未找到婚配对象。";
			return false;
		}
		try
		{
			if (!hero.IsActive)
			{
				reason = $"{hero.Name} 当前未激活。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				reason = $"{hero.Name} 已有配偶。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (!hero.IsLord)
			{
				reason = $"{hero.Name} 不是领主英雄。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsMinorFactionHero || hero.IsNotable || hero.IsTemplate)
			{
				reason = $"{hero.Name} 当前不属于可婚配英雄类型。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			int num = (hero.IsFemale ? (Campaign.Current?.Models?.MarriageModel?.MinimumMarriageAgeFemale ?? 18) : (Campaign.Current?.Models?.MarriageModel?.MinimumMarriageAgeMale ?? 18));
			if (hero.CharacterObject?.Age < (float)num)
			{
				reason = $"{hero.Name} 年龄未达到婚配要求。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			bool result = true;
			CampaignEventDispatcher.Instance.CanHeroMarry(hero, ref result);
			if (!result)
			{
				reason = $"{hero.Name} 当前被其他系统规则禁止结婚。";
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[WARN] CanHeroMarry event check failed: " + ex);
		}
		return true;
	}

	private static bool TryForceApplyMarriageAction(Hero left, Hero right, out string failReason)
	{
		failReason = "";
		try
		{
			if (left == null || right == null)
			{
				failReason = "未找到婚配双方。";
				return false;
			}
			var marriageModel = Campaign.Current?.Models?.MarriageModel;
			if (marriageModel == null)
			{
				failReason = "未找到原版 MarriageModel。";
				return false;
			}
			Hero hero = left;
			Hero hero2 = right;
			hero.Spouse = hero2;
			hero2.Spouse = hero;
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, hero2, marriageModel.GetEffectiveRelationIncrease(hero, hero2), false);
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] Force marriage relation increase failed: " + ex);
			}
			Clan clanAfterMarriage = marriageModel.GetClanAfterMarriage(hero, hero2);
			if (clanAfterMarriage != hero.Clan)
			{
				Hero hero3 = hero;
				hero = hero2;
				hero2 = hero3;
			}
			bool flag = false;
			try
			{
				CampaignEventDispatcher.Instance.OnBeforeHeroesMarried(hero, hero2, true);
			}
			catch (Exception ex2)
			{
				flag = true;
				Logger.Log("Romance", "[WARN] Force marriage event chain failed, will continue and backfill notifications: " + ex2);
			}
			if (hero.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(hero, clanAfterMarriage);
			}
			if (hero2.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(hero2, clanAfterMarriage);
			}
			try
			{
				MethodInfo methodInfo = typeof(Romance).GetMethod("EndAllCourtships", BindingFlags.Static | BindingFlags.NonPublic);
				if (methodInfo != null)
				{
					methodInfo.Invoke(null, new object[1] { hero });
					methodInfo.Invoke(null, new object[1] { hero2 });
				}
			}
			catch (Exception ex3)
			{
				Logger.Log("Romance", "[WARN] Force marriage end courtships failed: " + ex3);
			}
			try
			{
				ChangeRomanticStateAction.Apply(hero, hero2, Romance.RomanceLevelEnum.Marriage);
			}
			catch (Exception ex4)
			{
				Logger.Log("Romance", "[WARN] Force marriage romantic state update failed: " + ex4);
			}
			if (left?.Spouse == right && right?.Spouse == left)
			{
				if (flag)
				{
					TryEmitMarriageFallbackNotifications(left, right);
				}
				return true;
			}
			failReason = "强制婚姻执行后婚姻未实际建立。";
			return false;
		}
		catch (Exception ex)
		{
			if (left?.Spouse == right && right?.Spouse == left)
			{
				Logger.Log("Romance", "[WARN] Force marriage threw after marriage state changed: " + ex);
				TryEmitMarriageFallbackNotifications(left, right);
				return true;
			}
			failReason = ex.GetType().Name + ": " + ex.Message;
			Logger.Log("Romance", "[ERROR] Force marriage failed: " + ex);
			return false;
		}
	}

	private static void HandleClanChangeAfterMarriageCompat(Hero hero, Clan clanAfterMarriage)
	{
		if (hero == null || clanAfterMarriage == null)
		{
			return;
		}
		Clan clan = hero.Clan;
		if (clan == null)
		{
			hero.Clan = clanAfterMarriage;
			return;
		}
		try
		{
			if (hero.GovernorOf != null)
			{
				ChangeGovernorAction.RemoveGovernorOf(hero);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[WARN] Remove governor during force marriage failed: " + ex);
		}
		try
		{
			if (hero.PartyBelongedTo != null)
			{
				if (clan.Kingdom != clanAfterMarriage.Kingdom)
				{
					if (hero.PartyBelongedTo.Army != null)
					{
						if (hero.PartyBelongedTo.Army.LeaderParty == hero.PartyBelongedTo)
						{
							DisbandArmyAction.ApplyByUnknownReason(hero.PartyBelongedTo.Army);
						}
						else
						{
							hero.PartyBelongedTo.Army = null;
						}
					}
					IFaction kingdom = clanAfterMarriage.Kingdom;
					FactionHelper.FinishAllRelatedHostileActionsOfNobleToFaction(hero, kingdom ?? clanAfterMarriage);
				}
				MobileParty partyBelongedTo = hero.PartyBelongedTo;
				bool flag = hero.PartyBelongedTo.LeaderHero == hero;
				partyBelongedTo.MemberRoster.RemoveTroop(hero.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
				MakeHeroFugitiveAction.Apply(hero, false);
				if (flag && partyBelongedTo.IsLordParty)
				{
					DisbandPartyAction.StartDisband(partyBelongedTo);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("Romance", "[WARN] Party/clan transfer during force marriage failed: " + ex2);
		}
		hero.Clan = clanAfterMarriage;
		try
		{
			foreach (Hero item in clan.Heroes)
			{
				item?.UpdateHomeSettlement();
			}
			foreach (Hero item2 in clanAfterMarriage.Heroes)
			{
				item2?.UpdateHomeSettlement();
			}
		}
		catch (Exception ex3)
		{
			Logger.Log("Romance", "[WARN] UpdateHomeSettlement during force marriage failed: " + ex3);
		}
	}

	private static string BuildMarriageSuitabilityHint(Hero left, Hero right)
	{
		if (left == null || right == null)
		{
			return "未找到婚配双方。";
		}
		List<string> list = new List<string>();
		try
		{
			var marriageModel = Campaign.Current?.Models?.MarriageModel;
			if (marriageModel != null && !marriageModel.IsCoupleSuitableForMarriage(left, right))
			{
				list.Add("原版 MarriageModel 判定该组合当前不适合结婚");
			}
		}
		catch (Exception ex)
		{
			list.Add("读取原版婚配判定失败: " + ex.GetType().Name);
		}
		try
		{
			if (left.IsFemale == right.IsFemale)
			{
				list.Add("双方性别相同");
			}
		}
		catch
		{
		}
		try
		{
			if (left.Clan == null || right.Clan == null)
			{
				list.Add("有一方没有家族");
			}
		}
		catch
		{
		}
		try
		{
			if (left.Clan?.Leader == left && right.Clan?.Leader == right)
			{
				list.Add("双方都是族长");
			}
		}
		catch
		{
		}
		TryAppendHeroMarriageBlockers(left, "玩家方", list);
		TryAppendHeroMarriageBlockers(right, "对方", list);
		return string.Join("；", list.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
	}

	private static void TryAppendHeroMarriageBlockers(Hero hero, string sideLabel, List<string> reasons)
	{
		if (hero == null || reasons == null)
		{
			return;
		}
		try
		{
			if (!hero.IsActive)
			{
				reasons.Add(sideLabel + "未激活");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				reasons.Add(sideLabel + "已有配偶");
			}
		}
		catch
		{
		}
		try
		{
			if (!hero.IsLord)
			{
				reasons.Add(sideLabel + "不是领主英雄");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsMinorFactionHero)
			{
				reasons.Add(sideLabel + "属于次要派系英雄");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsNotable)
			{
				reasons.Add(sideLabel + "是城镇要人");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsTemplate)
			{
				reasons.Add(sideLabel + "是模板英雄");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.PartyBelongedTo?.MapEvent != null)
			{
				reasons.Add(sideLabel + "正在地图事件中");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.PartyBelongedTo?.Army != null)
			{
				reasons.Add(sideLabel + "正在军团中");
			}
		}
		catch
		{
		}
	}

	private static string DescribeHeroMarriageState(Hero hero)
	{
		if (hero == null)
		{
			return "null";
		}
		List<string> list = new List<string>();
		try
		{
			list.Add("id=" + (hero.StringId ?? ""));
		}
		catch
		{
		}
		try
		{
			list.Add("name=" + hero.Name);
		}
		catch
		{
		}
		try
		{
			list.Add("gender=" + (hero.IsFemale ? "F" : "M"));
		}
		catch
		{
		}
		try
		{
			list.Add("age=" + hero.Age.ToString("0"));
		}
		catch
		{
		}
		try
		{
			list.Add("clan=" + (hero.Clan?.StringId ?? "null"));
		}
		catch
		{
		}
		try
		{
			list.Add("leader=" + ((hero.Clan?.Leader == hero) ? "true" : "false"));
		}
		catch
		{
		}
		try
		{
			list.Add("spouse=" + (hero.Spouse?.StringId ?? "null"));
		}
		catch
		{
		}
		try
		{
			list.Add("active=" + hero.IsActive);
		}
		catch
		{
		}
		try
		{
			list.Add("lord=" + hero.IsLord);
		}
		catch
		{
		}
		try
		{
			list.Add("template=" + hero.IsTemplate);
		}
		catch
		{
		}
		try
		{
			list.Add("notable=" + hero.IsNotable);
		}
		catch
		{
		}
		try
		{
			list.Add("minorFaction=" + hero.IsMinorFactionHero);
		}
		catch
		{
		}
		try
		{
			list.Add("prisoner=" + hero.IsPrisoner);
		}
		catch
		{
		}
		try
		{
			list.Add("mapEvent=" + ((hero.PartyBelongedTo?.MapEvent != null) ? "true" : "false"));
		}
		catch
		{
		}
		try
		{
			list.Add("army=" + ((hero.PartyBelongedTo?.Army != null) ? "true" : "false"));
		}
		catch
		{
		}
		return string.Join(", ", list);
	}

	private static void TryEmitMarriageFallbackNotifications(Hero left, Hero right)
	{
		try
		{
			if (left == null || right == null)
			{
				return;
			}
			Hero hero = (left.IsFemale ? right : left);
			Hero hero2 = (left.IsFemale ? left : right);
			bool flag = LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive || Mission.Current != null;
			if (!flag)
			{
				MBInformationManager.ShowSceneNotification(new TaleWorlds.CampaignSystem.SceneInformationPopupTypes.MarriageSceneNotificationItem(hero, hero2, CampaignTime.Now, SceneNotificationData.RelevantContextType.Any));
			}
			var characterMarriedLogEntry = new TaleWorlds.CampaignSystem.LogEntries.CharacterMarriedLogEntry(left, right);
			TaleWorlds.CampaignSystem.LogEntries.LogEntry.AddLogEntry(characterMarriedLogEntry);
			if (left.Clan == Clan.PlayerClan || right.Clan == Clan.PlayerClan)
			{
				Campaign.Current?.CampaignInformationManager?.NewMapNoticeAdded(new TaleWorlds.CampaignSystem.MapNotificationTypes.MarriageMapNotification(left, right, characterMarriedLogEntry.GetEncyclopediaText(), CampaignTime.Now));
			}
			InformationManager.DisplayMessage(new InformationMessage(flag ? "[婚姻系统] 检测到婚姻已生效；当前在会面/任务场景中，已补发婚姻记录与提示。" : "[婚姻系统] 检测到婚姻已生效，已补发婚礼通知。", Color.FromUint(4283878655u)));
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] Emit marriage fallback notifications failed: " + ex);
		}
	}

	private static IEnumerable<Hero> GetClanMembersCompat(Clan clan)
	{
		if (clan == null)
		{
			yield break;
		}
		IEnumerable enumerable = null;
		try
		{
			PropertyInfo property = clan.GetType().GetProperty("Lords", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				enumerable = property.GetValue(clan, null) as IEnumerable;
			}
			if (enumerable == null)
			{
				PropertyInfo property2 = clan.GetType().GetProperty("Heroes", BindingFlags.Instance | BindingFlags.Public);
				if (property2 != null)
				{
					enumerable = property2.GetValue(clan, null) as IEnumerable;
				}
			}
		}
		catch
		{
			enumerable = null;
		}
		if (enumerable == null)
		{
			yield break;
		}
		foreach (object item in enumerable)
		{
			if (item is Hero hero && hero != null)
			{
				yield return hero;
			}
		}
	}

	private static int GetMarriageCandidateMaxAgeSetting()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAge).GetValueOrDefault(MarriageCandidateMaxAge);
			if (valueOrDefault < MarriageCandidateMinAge)
			{
				valueOrDefault = MarriageCandidateMinAge;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAge;
		}
	}

	private static int GetMarriageCandidateMaxAgeGapSetting()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAgeGap).GetValueOrDefault(MarriageCandidateMaxAgeGap);
			if (valueOrDefault < 0)
			{
				valueOrDefault = 0;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeGap;
		}
	}

	private static bool GetMarriageRequireOppositeGenderSetting()
	{
		try
		{
			return (DuelSettings.GetSettings()?.MarriageRequireOppositeGender).GetValueOrDefault(true);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsFormalMarriageCandidate(Hero hero)
	{
		if (!IsMarriageableHero(hero))
		{
			return false;
		}
		try
		{
			int marriageCandidateMaxAgeSetting = GetMarriageCandidateMaxAgeSetting();
			if (hero.Age < (float)MarriageCandidateMinAge || hero.Age > (float)marriageCandidateMaxAgeSetting)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return true;
	}

	private static bool IsFormalMarriagePairCompatible(Hero left, Hero right, out string reason)
	{
		reason = "";
		if (left == null || right == null)
		{
			reason = "未找到婚配双方。";
			return false;
		}
		if (!IsFormalMarriageCandidate(left))
		{
			reason = "玩家方婚配人选不满足基础婚配条件。";
			return false;
		}
		if (!IsFormalMarriageCandidate(right))
		{
			reason = "对方婚配人选不满足基础婚配条件。";
			return false;
		}
		try
		{
			if (GetMarriageRequireOppositeGenderSetting() && left.IsFemale == right.IsFemale)
			{
				reason = "当前设置要求异性婚配。";
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Math.Abs(left.Age - right.Age) > (float)GetMarriageCandidateMaxAgeGapSetting())
			{
				reason = "双方年龄差超过当前设置允许范围。";
				return false;
			}
		}
		catch
		{
			reason = "无法确认双方年龄差是否满足婚配条件。";
			return false;
		}
		return true;
	}

	private static bool HasAnyFormalMarriagePair(Clan playerClan, Clan targetClan, out string reason)
	{
		reason = "";
		if (playerClan == null || targetClan == null)
		{
			reason = "未找到双方家族信息。";
			return false;
		}
		if (playerClan == targetClan)
		{
			reason = "正规联姻要求双方来自不同家族。";
			return false;
		}
		List<Hero> list = GetClanMembersCompat(playerClan).Where(IsFormalMarriageCandidate).ToList();
		if (list.Count <= 0)
		{
			reason = "玩家家族当前没有符合基础条件的未婚成员。";
			return false;
		}
		List<Hero> list2 = GetClanMembersCompat(targetClan).Where(IsFormalMarriageCandidate).ToList();
		if (list2.Count <= 0)
		{
			reason = "对方家族当前没有符合基础条件的未婚成员。";
			return false;
		}
		foreach (Hero item in list)
		{
			foreach (Hero item2 in list2)
			{
				if (item != null && item2 != null && item != item2 && IsFormalMarriagePairCompatible(item, item2, out var _))
				{
					return true;
				}
			}
		}
		reason = "双方家族当前没有满足性别和年龄差条件的未婚成员组合。";
		return false;
	}

	public int ComputeTargetFamilyHarmony(Hero target)
	{
		try
		{
			if (target == null || target.Clan == null)
			{
				return 0;
			}
			Hero leader = target.Clan.Leader;
			int relationSafe = GetRelationSafe(target, leader);
			if (leader == target)
			{
				relationSafe = 40;
			}
			List<int> list = new List<int>();
			if (target.Spouse != null && target.Spouse != target && target.Spouse.Clan == target.Clan)
			{
				list.Add(GetRelationSafe(target, target.Spouse));
			}
			if (target.Father != null && target.Father != target && target.Father.Clan == target.Clan)
			{
				list.Add(GetRelationSafe(target, target.Father));
			}
			if (target.Mother != null && target.Mother != target && target.Mother.Clan == target.Clan)
			{
				list.Add(GetRelationSafe(target, target.Mother));
			}
			double num = ((list.Count > 0) ? list.Average() : 0.0);
			List<int> list2 = new List<int>();
			foreach (Hero clanMembersCompat in GetClanMembersCompat(target.Clan))
			{
				if (clanMembersCompat != null && clanMembersCompat != target)
				{
					if (clanMembersCompat.Age < 18f)
					{
						continue;
					}
					list2.Add(GetRelationSafe(target, clanMembersCompat));
				}
			}
			double num2 = ((list2.Count > 0) ? list2.Average() : 0.0);
			int num3 = (int)Math.Round((double)relationSafe * 0.6 + num * 0.25 + num2 * 0.15);
			if (num3 < -100)
			{
				num3 = -100;
			}
			if (num3 > 100)
			{
				num3 = 100;
			}
			return num3;
		}
		catch
		{
			return 0;
		}
	}

	private Hero ResolveTargetHeroToken(Hero speaker, string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (string.Equals(text, "self", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "current", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "me", StringComparison.OrdinalIgnoreCase))
		{
			return speaker;
		}
		try
		{
			Hero hero = Hero.Find(text);
			if (hero != null)
			{
				return hero;
			}
		}
		catch
		{
		}
		if (speaker != null && !string.IsNullOrWhiteSpace(speaker.Name?.ToString()) && string.Equals(text, speaker.Name.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			return speaker;
		}
		return null;
	}

	private Hero ResolvePlayerClanHeroToken(string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return null;
		}
		if (string.Equals(text, "self", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "current", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "me", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "player", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "mainhero", StringComparison.OrdinalIgnoreCase))
		{
			return mainHero;
		}
		Hero hero = null;
		try
		{
			hero = Hero.Find(text);
		}
		catch
		{
			hero = null;
		}
		if (hero == null)
		{
			try
			{
				hero = Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				hero = null;
			}
		}
		if (hero == null && string.Equals(text, mainHero.Name?.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
		{
			hero = mainHero;
		}
		if (hero != null && hero.Clan == mainHero.Clan)
		{
			return hero;
		}
		return null;
	}

	private static int GetClanRelationWithPlayer(Hero clanLeader)
	{
		return GetRelationSafe(clanLeader, Hero.MainHero);
	}

	private static string GetClanNameSafe(Hero hero)
	{
		string text = hero?.Clan?.Name?.ToString() ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return "无家族";
	}

	private void RegisterMarriageRecord(Hero left, Hero right, string marriageType, Clan payerClan, Clan receiverClan, int bridePriceAmount)
	{
		if (left == null || right == null)
		{
			return;
		}
		SaveMarriageRecord(new MarriageRecord
		{
			LeftHeroId = NormalizeId(left.StringId),
			RightHeroId = NormalizeId(right.StringId),
			Type = marriageType ?? "",
			PayerClanId = NormalizeId(payerClan?.StringId),
			ReceiverClanId = NormalizeId(receiverClan?.StringId),
			BridePriceAmount = Math.Max(0, bridePriceAmount),
			IsActive = true
		});
	}

	private void RecordFormalMarriageFacts(Hero leader, Hero playerClanHero, Hero targetHero, Clan payerClan, Clan receiverClan, int bridePriceAmount)
	{
		if (playerClanHero == null || targetHero == null)
		{
			return;
		}
		RegisterMarriageRecord(playerClanHero, targetHero, "formal", payerClan, receiverClan, bridePriceAmount);
		string text = GetClanNameSafe(playerClanHero);
		string text2 = GetClanNameSafe(targetHero);
		string text3 = $"{text}的{playerClanHero.Name}";
		string text4 = $"{text2}的{targetHero.Name}";
		string text5 = BuildBridePriceSummary(GetMarriageRecord(playerClanHero, targetHero));
		if (leader != null)
		{
			MyBehavior.AppendExternalNpcFact(leader, $"你已经同意 {text3} 与 {text4} 正式成婚。{text5}。");
		}
		if (targetHero != leader)
		{
			MyBehavior.AppendExternalNpcFact(targetHero, $"你已经与 {text3} 正式成婚。{text5}。");
		}
		if (playerClanHero == Hero.MainHero)
		{
			MyBehavior.AppendExternalPlayerFact(Hero.MainHero, $"你已经与 {text4} 正式成婚。{text5}。");
		}
		else
		{
			MyBehavior.AppendExternalNpcFact(playerClanHero, $"你已经与 {text4} 正式成婚。{text5}。");
		}
	}

	private void RecordElopeMarriageFacts(Hero targetHero)
	{
		if (targetHero == null)
		{
			return;
		}
		RegisterMarriageRecord(Hero.MainHero, targetHero, "elope", null, null, 0);
		string text = GetClanNameSafe(targetHero);
		MyBehavior.AppendExternalPlayerFact(Hero.MainHero, $"你已经与 {text}的{targetHero.Name} 私奔并成婚。");
		MyBehavior.AppendExternalNpcFact(targetHero, "你已经与玩家私奔并成婚。");
		Hero hero = targetHero.Clan?.Leader;
		if (hero != null && hero != targetHero)
		{
			MyBehavior.AppendExternalNpcFact(hero, $"玩家已经与 {text}的{targetHero.Name} 私奔并成婚。");
		}
	}

	private void RecordDivorceFacts(Hero playerClanHero, Hero targetHero, int refundedAmount)
	{
		if (playerClanHero == null || targetHero == null)
		{
			return;
		}
		string text = GetClanNameSafe(playerClanHero);
		string text2 = GetClanNameSafe(targetHero);
		string text3 = $"{text}的{playerClanHero.Name}";
		string text4 = $"{text2}的{targetHero.Name}";
		string text5 = (refundedAmount > 0) ? $" 双方已议定返还彩礼 {refundedAmount:N0} 第纳尔。" : "";
		MyBehavior.AppendExternalPlayerFact(Hero.MainHero, $"你们已经解除 {text3} 与 {text4} 的婚姻关系。{text5}".Trim());
		MyBehavior.AppendExternalNpcFact(targetHero, $"你与 {text3} 的婚姻已经解除。{text5}".Trim());
		if (playerClanHero != Hero.MainHero)
		{
			MyBehavior.AppendExternalNpcFact(playerClanHero, $"你与 {text4} 的婚姻已经解除。{text5}".Trim());
		}
		Hero hero = targetHero.Clan?.Leader;
		if (hero != null && hero != targetHero)
		{
			MyBehavior.AppendExternalNpcFact(hero, $"你方家族与 {text} 的婚姻安排已经解除。{text5}".Trim());
		}
	}

	private static bool CanArrangeFormalMarriagePair(Hero playerClanHero, Hero targetHero, out string reason)
	{
		reason = "";
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || playerClanHero == null || targetHero == null)
		{
			reason = "未找到婚配双方。";
			return false;
		}
		if (playerClanHero.Clan == null || playerClanHero.Clan != mainHero.Clan)
		{
			reason = "玩家方婚配人选必须来自玩家家族。";
			return false;
		}
		if (playerClanHero == targetHero)
		{
			reason = "不能让同一人与自己结婚。";
			return false;
		}
		if (playerClanHero.Clan != null && targetHero.Clan != null && playerClanHero.Clan == targetHero.Clan)
		{
			reason = "正规联姻要求双方来自不同家族。";
			return false;
		}
		if (!IsMarriageableHero(playerClanHero))
		{
			reason = "玩家方婚配人选当前不满足基础婚配条件。";
			return false;
		}
		if (!IsMarriageableHero(targetHero))
		{
			reason = "对方婚配人选当前不满足基础婚配条件。";
			return false;
		}
		if (!IsFormalMarriagePairCompatible(playerClanHero, targetHero, out reason))
		{
			return false;
		}
		return true;
	}

	private bool TryExecuteFormalMarriage(Hero speaker, Hero playerClanHero, Hero targetHero, int? bridePrice, out string status)
	{
		status = "";
		if (!CanArrangeFormalMarriagePair(playerClanHero, targetHero, out var reason))
		{
			status = "正规联姻失败：" + reason;
			return false;
		}
		if (targetHero.Clan == null)
		{
			status = "正规联姻失败：目标没有家族，无法走家族联姻流程。";
			return false;
		}
		Hero leader = targetHero.Clan.Leader;
		if (leader == null)
		{
			status = "正规联姻失败：未找到对方家族族长。";
			return false;
		}
		if (speaker == null || speaker != leader)
		{
			status = "正规联姻失败：必须由对方家族族长亲自同意。";
			return false;
		}
		int num = Hero.MainHero?.Clan?.Tier ?? 0;
		int num2 = targetHero.Clan?.Tier ?? 0;
		int num3 = num - num2;
		if (num3 <= -3)
		{
			status = $"正规联姻失败：家族等级差距过大（玩家={num}，对方={num2}）。";
			return false;
		}
		int clanRelationWithPlayer = GetClanRelationWithPlayer(leader);
		int effectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(leader) ?? 0;
		int num4 = (bridePrice.HasValue && bridePrice.Value > 0) ? bridePrice.Value : 0;
		int num5 = 0;
		bool flag = num3 <= -1;
		bool flag2 = num3 >= 1;
		if (flag && num4 > 0)
		{
			num5 = RewardSystemBehavior.Instance?.GetPlayerPrepaidGoldForExternal(leader) ?? 0;
		}
		if (num3 == -1)
		{
			if (clanRelationWithPlayer < 20 || effectiveTrust < 20)
			{
				status = $"正规联姻失败：门槛不足（家族关系={clanRelationWithPlayer}，族长综合信任={effectiveTrust}；需要 >=20 / >=20）。";
				return false;
			}
		}
		else if (num3 != -2)
		{
			int num6 = ((num3 <= 0) ? 5 : (-5 * num3));
			if (num6 < -20)
			{
				num6 = -20;
			}
			if (clanRelationWithPlayer < num6 || effectiveTrust < num6)
			{
				status = $"正规联姻失败：门槛不足（当前关系={clanRelationWithPlayer}，信任={effectiveTrust}；需要 >= {num6}）。";
				return false;
			}
			if (flag && num4 > 0 && num5 < num4)
			{
				status = $"正规联姻失败：彩礼尚未交足。约定彩礼 {num4:N0}，当前已主动交给族长 {num5:N0}，还差 {Math.Max(0, num4 - num5):N0}。";
				return false;
			}
		}
		if (flag && num4 > 0 && num5 < num4)
		{
			status = $"正规联姻失败：彩礼尚未交足。约定彩礼 {num4:N0}，当前已主动交给族长 {num5:N0}，还差 {Math.Max(0, num4 - num5):N0}。";
			return false;
		}
		if (flag2 && num4 > 0 && (leader?.Gold).GetValueOrDefault() < num4)
		{
			status = $"正规联姻失败：族长当前金币不足，无法支付承诺给玩家的彩礼 {num4:N0}。";
			return false;
		}
		if (!TryApplyMarriageAction(playerClanHero, targetHero, out var failReason))
		{
			status = "正规联姻失败：执行 MarriageAction 失败，" + failReason;
			return false;
		}
		int num7 = 0;
		if (flag && num4 > 0)
		{
			num7 = RewardSystemBehavior.Instance?.ConsumePlayerPrepaidGoldForExternal(leader, num4) ?? 0;
		}
		int num8 = 0;
		if (flag2 && num4 > 0 && leader != null)
		{
			num8 = Math.Min(num4, Math.Max(0, leader.Gold));
			if (num8 > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(leader, Hero.MainHero, num8);
			}
		}
		AdjustPrivateLove(targetHero, 10, "formal_marriage_success");
		Clan clan = null;
		Clan clan2 = null;
		int num9 = 0;
		if (num7 > 0)
		{
			clan = Hero.MainHero?.Clan;
			clan2 = leader?.Clan;
			num9 = num7;
		}
		else if (num8 > 0)
		{
			clan = leader?.Clan;
			clan2 = Hero.MainHero?.Clan;
			num9 = num8;
		}
		RecordFormalMarriageFacts(leader, playerClanHero, targetHero, clan, clan2, num9);
		status = $"正规联姻成功：{playerClanHero.Name} 与 {targetHero.Name} 已成婚。等级差D={num3}，家族关系={clanRelationWithPlayer}，族长综合信任={effectiveTrust}。";
		if (num7 > 0)
		{
			status += $" 已核销你先前主动交付给族长的彩礼 {num7:N0} 第纳尔。";
		}
		if (num8 > 0)
		{
			status += $" 对方族长已向玩家支付彩礼 {num8:N0} 第纳尔。";
			MyBehavior.AppendExternalNpcFact(leader, $"你已经向玩家支付了彩礼 {num8:N0} 第纳尔。");
			MyBehavior.AppendExternalPlayerFact(Hero.MainHero, $"你已经从 {leader?.Name?.ToString() ?? "对方族长"} 收到了彩礼 {num8:N0} 第纳尔。");
		}
		return true;
	}

	private bool TryExecuteElopeMarriage(Hero speaker, Hero targetHero, out string status)
	{
		status = "";
		if (!CanPlayerMarryTarget(targetHero, out var reason))
		{
			status = "私奔失败：" + reason;
			return false;
		}
		if (speaker == null || speaker != targetHero)
		{
			status = "私奔失败：必须由结婚对象本人亲口同意。";
			return false;
		}
		Hero leader = targetHero.Clan?.Leader;
		int clanRelationWithPlayer = GetClanRelationWithPlayer(leader);
		int effectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(leader) ?? 0;
		int npcTrust = RewardSystemBehavior.Instance?.GetNpcTrust(targetHero) ?? 0;
		int privateLove = GetPrivateLove(targetHero);
		int num = 70;
		int num2 = 20;
		if (clanRelationWithPlayer < -20 || effectiveTrust < -20)
		{
			num = 85;
			num2 = 35;
		}
		int num3 = ComputeTargetFamilyHarmony(targetHero);
		if (num3 <= -40)
		{
			num -= 15;
			num2 -= 10;
		}
		else if (num3 <= -20)
		{
			num -= 8;
			num2 -= 5;
		}
		else if (num3 >= 20)
		{
			num += 10;
			num2 += 5;
		}
		if (num < 20)
		{
			num = 20;
		}
		if (num2 < -20)
		{
			num2 = -20;
		}
		if (privateLove < num || npcTrust < num2)
		{
			AdjustPrivateLove(targetHero, -3, "elope_rejected");
			status = $"私奔失败：门槛不足（L={privateLove}/{num}，对象私人信任={npcTrust}/{num2}，对象家族融洽度F={num3}）。";
			return false;
		}
		if (!TryApplyMarriageAction(Hero.MainHero, targetHero, out var failReason))
		{
			status = "私奔失败：执行 MarriageAction 失败，" + failReason;
			return false;
		}
		AdjustPrivateLove(targetHero, 12, "elope_success");
		RecordElopeMarriageFacts(targetHero);
		if (leader != null && leader != targetHero)
		{
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, leader, -25);
			}
			catch
			{
			}
			RewardSystemBehavior.Instance?.AdjustTrustForExternal(leader, -15, -10, "elope_penalty");
		}
		status = $"私奔成功：你与 {targetHero.Name} 已成婚。当前 L={privateLove}，对象私人信任={npcTrust}，对象家族融洽度F={num3}。";
		return true;
	}

	private bool TryExecuteDivorce(Hero speaker, Hero playerClanHero, Hero targetHero, int? refundAmount, out string status)
	{
		status = "";
		if (playerClanHero == null || targetHero == null)
		{
			status = "离婚失败：未找到离婚双方。";
			return false;
		}
		MarriageRecord marriageRecord = GetMarriageRecord(playerClanHero, targetHero);
		if (marriageRecord == null || !marriageRecord.IsActive)
		{
			status = "离婚失败：未找到有效婚姻记录。";
			return false;
		}
		if (playerClanHero.Spouse != targetHero && targetHero.Spouse != playerClanHero)
		{
			status = "离婚失败：双方当前并非彼此配偶。";
			return false;
		}
		bool flag = false;
		if (speaker == playerClanHero || speaker == targetHero)
		{
			flag = true;
		}
		else if (speaker != null && targetHero.Clan?.Leader == speaker)
		{
			flag = true;
		}
		if (!flag)
		{
			status = "离婚失败：当前说话人无权确认这桩离婚。";
			return false;
		}
		int num = (refundAmount.HasValue && refundAmount.Value > 0) ? refundAmount.Value : 0;
		if (num > 0 && marriageRecord.BridePriceAmount > 0)
		{
			Clan clan = FindClanById(marriageRecord.PayerClanId);
			Clan clan2 = FindClanById(marriageRecord.ReceiverClanId);
			if (clan == Clan.PlayerClan || clan2 != Clan.PlayerClan)
			{
				status = "离婚失败：自动返还金额只支持NPC向玩家退还先前收取的彩礼。若要由玩家向NPC付款，请使用现有给予选项单独处理。";
				return false;
			}
			Hero hero = clan2?.Leader;
			Hero hero2 = Hero.MainHero;
			if (hero == null || hero2 == null)
			{
				status = "离婚失败：未找到彩礼返还的付款方或收款方。";
				return false;
			}
			if (num > Math.Max(0, marriageRecord.BridePriceAmount))
			{
				status = $"离婚失败：返还金额 {num:N0} 超过原彩礼记录 {marriageRecord.BridePriceAmount:N0}。";
				return false;
			}
			if (Math.Max(0, hero.Gold) < num)
			{
				status = $"离婚失败：返还方当前金币不足，无法支付 {num:N0} 第纳尔。";
				return false;
			}
			GiveGoldAction.ApplyBetweenCharacters(hero, hero2, num);
		}
		try
		{
			if (playerClanHero.Spouse == targetHero)
			{
				playerClanHero.Spouse = null;
			}
			if (targetHero.Spouse == playerClanHero)
			{
				targetHero.Spouse = null;
			}
		}
		catch (Exception ex)
		{
			status = "离婚失败：解除配偶关系时异常，" + ex.Message;
			return false;
		}
		MarriageRecord marriageRecord2 = marriageRecord;
		marriageRecord2.IsActive = false;
		SaveMarriageRecord(marriageRecord2);
		AdjustPrivateLove(targetHero, -12, "divorce");
		RecordDivorceFacts(playerClanHero, targetHero, num);
		status = $"离婚成功：{playerClanHero.Name} 与 {targetHero.Name} 已解除婚姻关系。";
		if (num > 0)
		{
			status += $" 已返还彩礼 {num:N0} 第纳尔。";
		}
		return true;
	}

	private sealed class MarriageRuntimeFacts
	{
		public Hero Speaker;

		public Hero Leader;

		public bool PairAvailable;

		public string PairUnavailableReason;

		public bool HasClan;

		public bool IsLeader;

		public int PlayerTier;

		public int TargetTier;

		public int TierDiff;

		public int ClanRelation;

		public int EffectiveTrust;

		public int PrivateLove;

		public int NpcTrust;

		public int FamilyHarmony;

		public int FormalThreshold;

		public int ElopeLoveNeed;

		public int ElopeTrustNeed;

		public int PrepaidGold;

		public int LeaderGold;

		public bool CanElope;
	}

	private MarriageRuntimeFacts BuildMarriageRuntimeFacts(Hero speaker)
	{
		MarriageRuntimeFacts marriageRuntimeFacts = new MarriageRuntimeFacts
		{
			Speaker = speaker,
			Leader = speaker?.Clan?.Leader,
			HasClan = speaker?.Clan != null,
			IsLeader = false,
			PairAvailable = false,
			PairUnavailableReason = "",
			PlayerTier = Hero.MainHero?.Clan?.Tier ?? 0,
			TargetTier = speaker?.Clan?.Tier ?? 0,
			TierDiff = 0,
			ClanRelation = 0,
			EffectiveTrust = 0,
			PrivateLove = 0,
			NpcTrust = 0,
			FamilyHarmony = 0,
			FormalThreshold = 5,
			ElopeLoveNeed = 70,
			ElopeTrustNeed = 20,
			PrepaidGold = 0,
			LeaderGold = 0,
			CanElope = false
		};
		marriageRuntimeFacts.IsLeader = marriageRuntimeFacts.Leader != null && marriageRuntimeFacts.Leader == speaker;
		if (speaker == null)
		{
			marriageRuntimeFacts.PairUnavailableReason = "未找到当前对话对象。";
			return marriageRuntimeFacts;
		}
		marriageRuntimeFacts.TierDiff = marriageRuntimeFacts.PlayerTier - marriageRuntimeFacts.TargetTier;
		marriageRuntimeFacts.ClanRelation = GetClanRelationWithPlayer(marriageRuntimeFacts.Leader);
		marriageRuntimeFacts.EffectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(marriageRuntimeFacts.Leader) ?? 0;
		marriageRuntimeFacts.PrivateLove = GetPrivateLove(speaker);
		marriageRuntimeFacts.NpcTrust = RewardSystemBehavior.Instance?.GetNpcTrust(speaker) ?? 0;
		marriageRuntimeFacts.FamilyHarmony = ComputeTargetFamilyHarmony(speaker);
		string reason = "";
		string reason2 = "";
		bool flag = marriageRuntimeFacts.HasClan && HasAnyFormalMarriagePair(Hero.MainHero?.Clan, speaker.Clan, out reason);
		bool flag2 = CanPlayerMarryTarget(speaker, out reason2);
		marriageRuntimeFacts.PairAvailable = flag || flag2;
		if (!marriageRuntimeFacts.PairAvailable)
		{
			marriageRuntimeFacts.PairUnavailableReason = (!string.IsNullOrWhiteSpace(reason) ? reason : reason2);
		}
		int num = ((marriageRuntimeFacts.TierDiff <= 0) ? 5 : (-5 * marriageRuntimeFacts.TierDiff));
		if (num < -20)
		{
			num = -20;
		}
		marriageRuntimeFacts.FormalThreshold = num;
		int num2 = 70;
		int num3 = 20;
		if (marriageRuntimeFacts.ClanRelation < -20 || marriageRuntimeFacts.EffectiveTrust < -20)
		{
			num2 = 85;
			num3 = 35;
		}
		if (marriageRuntimeFacts.FamilyHarmony <= -40)
		{
			num2 -= 15;
			num3 -= 10;
		}
		else if (marriageRuntimeFacts.FamilyHarmony <= -20)
		{
			num2 -= 8;
			num3 -= 5;
		}
		else if (marriageRuntimeFacts.FamilyHarmony >= 20)
		{
			num2 += 10;
			num3 += 5;
		}
		if (num2 < 20)
		{
			num2 = 20;
		}
		if (num3 < -20)
		{
			num3 = -20;
		}
		marriageRuntimeFacts.ElopeLoveNeed = num2;
		marriageRuntimeFacts.ElopeTrustNeed = num3;
		marriageRuntimeFacts.CanElope = marriageRuntimeFacts.PairAvailable && marriageRuntimeFacts.PrivateLove >= marriageRuntimeFacts.ElopeLoveNeed && marriageRuntimeFacts.NpcTrust >= marriageRuntimeFacts.ElopeTrustNeed;
		if (marriageRuntimeFacts.Leader != null)
		{
			marriageRuntimeFacts.PrepaidGold = RewardSystemBehavior.Instance?.GetPlayerPrepaidGoldForExternal(marriageRuntimeFacts.Leader) ?? 0;
			try
			{
				marriageRuntimeFacts.LeaderGold = Math.Max(0, marriageRuntimeFacts.Leader.Gold);
			}
			catch
			{
				marriageRuntimeFacts.LeaderGold = 0;
			}
		}
		return marriageRuntimeFacts;
	}

	private static Dictionary<string, string> BuildMarriageRuntimeTokens(MarriageRuntimeFacts facts)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			dictionary["speakerName"] = facts?.Speaker?.Name?.ToString() ?? "";
			dictionary["speakerId"] = facts?.Speaker?.StringId ?? "";
			dictionary["leaderName"] = facts?.Leader?.Name?.ToString() ?? "";
			dictionary["leaderId"] = facts?.Leader?.StringId ?? "";
			dictionary["clanName"] = facts?.Speaker?.Clan?.Name?.ToString() ?? "";
			dictionary["playerTier"] = (facts?.PlayerTier ?? 0).ToString();
			dictionary["targetTier"] = (facts?.TargetTier ?? 0).ToString();
			dictionary["tierDiff"] = (facts?.TierDiff ?? 0).ToString();
			dictionary["clanRelation"] = (facts?.ClanRelation ?? 0).ToString();
			dictionary["effectiveTrust"] = (facts?.EffectiveTrust ?? 0).ToString();
			dictionary["privateLove"] = (facts?.PrivateLove ?? 0).ToString();
			dictionary["privateLoveLevel"] = GetPrivateLoveLevelText(facts?.PrivateLove ?? 0);
			dictionary["npcTrust"] = (facts?.NpcTrust ?? 0).ToString();
			dictionary["familyHarmony"] = (facts?.FamilyHarmony ?? 0).ToString();
			dictionary["formalThreshold"] = (facts?.FormalThreshold ?? 0).ToString();
			dictionary["elopeLoveNeed"] = (facts?.ElopeLoveNeed ?? 0).ToString();
			dictionary["elopeTrustNeed"] = (facts?.ElopeTrustNeed ?? 0).ToString();
			dictionary["prepaidGold"] = (facts?.PrepaidGold ?? 0).ToString();
			dictionary["leaderGold"] = (facts?.LeaderGold ?? 0).ToString();
			dictionary["bridePriceMin"] = ((facts?.TierDiff ?? 0) == -1 || (facts?.TierDiff ?? 0) == 1) ? BridePriceTierMinusOneMin.ToString() : BridePriceTierMinusTwoMin.ToString();
			dictionary["bridePriceMax"] = ((facts?.TierDiff ?? 0) == -1 || (facts?.TierDiff ?? 0) == 1) ? BridePriceTierMinusOneMax.ToString() : BridePriceTierMinusTwoMax.ToString();
			dictionary["pairUnavailableReason"] = facts?.PairUnavailableReason ?? "";
			dictionary["prestigeAdvantageHint"] = GetMarriagePrestigeAdvantageHint(facts);
		}
		catch
		{
		}
		return dictionary;
	}

	private static string GetMarriagePrestigeAdvantageHint(MarriageRuntimeFacts facts)
	{
		try
		{
			int num = facts?.TierDiff ?? 0;
			if (num <= -3)
			{
				return "【门第劣势】玩家家族对于你来说卑如蝼蚁，与他们联姻会让你非常丢脸。你应当明确拒绝并嘲讽。";
			}
			if (num == -2)
			{
				return "【门第劣势】玩家的家族对于你来说身份低微，与他们家族联姻会非常没有面子；不过如果他们愿意给大笔钱，你也可以考虑。";
			}
			if (num == -1)
			{
				return "【门第劣势】玩家家族比你的家族等级稍低，若玩家找你联姻，你应当提高要价。";
			}
			if (num >= 3)
			{
				return "【门第优势】玩家家族十分高贵。你应当主动与玩家家族联姻，并主动考虑给玩家高额彩礼。";
			}
			if (num == 2)
			{
				return "【门第优势】玩家家族很高贵，与他们家族联姻是千载难逢的机会。你应当主动考虑给玩家高额彩礼。";
			}
			if (num == 1)
			{
				return "【门第优势】玩家家族比你更高贵，你应当更加主动；若你同意联姻，应主动考虑给玩家彩礼。";
			}
		}
		catch
		{
		}
		return "";
	}

	private static string GetMarriageRuntimeInstructionState(MarriageRuntimeFacts facts)
	{
		if (Hero.MainHero == null)
		{
			return "no_player_hero";
		}
		if (facts == null || facts.Speaker == null)
		{
			return "no_target";
		}
		if (!facts.PairAvailable)
		{
			return "unavailable";
		}
		if (!facts.HasClan)
		{
			return "clanless_path";
		}
		if (facts.IsLeader)
		{
			return "leader_path";
		}
		return "member_path";
	}

	private static string GetMarriageRuntimeConstraintState(MarriageRuntimeFacts facts)
	{
		if (Hero.MainHero == null)
		{
			return "no_player_hero";
		}
		if (facts == null || facts.Speaker == null)
		{
			return "no_target";
		}
		if (!facts.PairAvailable)
		{
			return "unavailable";
		}
		if (!facts.HasClan)
		{
			return (facts.CanElope ? "clanless_elope_ready" : "clanless_elope_blocked");
		}
		if (facts.IsLeader)
		{
			if (facts.TierDiff <= -3)
			{
				return "leader_blocked_tier_gap";
			}
			if (facts.TierDiff == -2)
			{
				return "leader_need_heavy_brideprice";
			}
			if (facts.TierDiff == -1)
			{
				return ((facts.ClanRelation >= 20 && facts.EffectiveTrust >= 20) ? "leader_need_brideprice_ready" : "leader_need_brideprice_blocked");
			}
			if (facts.TierDiff >= 2)
			{
				return ((facts.ClanRelation >= facts.FormalThreshold && facts.EffectiveTrust >= facts.FormalThreshold) ? "leader_offer_brideprice_major" : "leader_standard_blocked");
			}
			if (facts.TierDiff == 1)
			{
				return ((facts.ClanRelation >= facts.FormalThreshold && facts.EffectiveTrust >= facts.FormalThreshold) ? "leader_offer_brideprice_minor" : "leader_standard_blocked");
			}
			return ((facts.ClanRelation >= facts.FormalThreshold && facts.EffectiveTrust >= facts.FormalThreshold) ? "leader_standard_ready" : "leader_standard_blocked");
		}
		return (facts.CanElope ? "member_redirect_elope_ready" : "member_redirect_elope_blocked");
	}

	public string BuildMarriageRuntimeInstruction(Hero speaker)
	{
		try
		{
			Hero hero = speaker;
			if (hero == null)
			{
				try
				{
					hero = Hero.OneToOneConversationHero;
				}
				catch
				{
					hero = null;
				}
			}
			MarriageRuntimeFacts marriageRuntimeFacts = BuildMarriageRuntimeFacts(hero);
			string marriageRuntimeInstructionState = GetMarriageRuntimeInstructionState(marriageRuntimeFacts);
			Dictionary<string, string> dictionary = BuildMarriageRuntimeTokens(marriageRuntimeFacts);
			string text = AIConfigHandler.ResolveRuleRuntimeText("marriage", marriageRuntimeInstructionState, forConstraint: false, dictionary);
			string text2 = BuildClanMarriageSituationPrompt(hero);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = string.IsNullOrWhiteSpace(text) ? text2 : (text.TrimEnd() + Environment.NewLine + text2);
			}
			return (text ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	public string BuildMarriageRuntimeConstraintHint(Hero speaker)
	{
		try
		{
			Hero hero = speaker;
			if (hero == null)
			{
				try
				{
					hero = Hero.OneToOneConversationHero;
				}
				catch
				{
					hero = null;
				}
			}
			MarriageRuntimeFacts marriageRuntimeFacts = BuildMarriageRuntimeFacts(hero);
			string marriageRuntimeConstraintState = GetMarriageRuntimeConstraintState(marriageRuntimeFacts);
			Dictionary<string, string> dictionary = BuildMarriageRuntimeTokens(marriageRuntimeFacts);
			string text = AIConfigHandler.ResolveRuleRuntimeText("marriage", marriageRuntimeConstraintState, forConstraint: true, dictionary);
			string text3 = GetMarriagePrestigeAdvantageHint(marriageRuntimeFacts);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text = string.IsNullOrWhiteSpace(text) ? text3 : (text.TrimEnd() + Environment.NewLine + text3);
			}
			return (text ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string BuildMarriagePostprocessRuleText(List<PostprocessRuleEntry> rules)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.Append("- ").Append(text);
				string text2 = (rule?.Description ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					stringBuilder.Append("：").Append(text2);
				}
				stringBuilder.AppendLine();
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static bool IsMarriageFormalPostprocessReadyState(string state)
	{
		switch ((state ?? "").Trim().ToLowerInvariant())
		{
		case "leader_need_brideprice_ready":
		case "leader_need_heavy_brideprice":
		case "leader_offer_brideprice_minor":
		case "leader_offer_brideprice_major":
		case "leader_standard_ready":
			return true;
		default:
			return false;
		}
	}

	private static bool IsMarriageElopePostprocessReadyState(string state)
	{
		switch ((state ?? "").Trim().ToLowerInvariant())
		{
		case "member_redirect_elope_ready":
		case "clanless_elope_ready":
			return true;
		default:
			return false;
		}
	}

	private static bool IsMarriageDivorcePostprocessEligible(Hero speaker, Hero playerClanHero, Hero targetHero)
	{
		if (speaker == null || playerClanHero == null || targetHero == null)
		{
			return false;
		}
		if (speaker == playerClanHero || speaker == targetHero)
		{
			return true;
		}
		return targetHero.Clan?.Leader == speaker;
	}

	private static string StripMarriageActionTags(string text)
	{
		string text2 = text ?? "";
		text2 = LoveDeltaRegex.Replace(text2, "");
		text2 = MarriageFormalPairRegex.Replace(text2, "");
		text2 = MarriageFormalLegacyRegex.Replace(text2, "");
		text2 = MarriageElopeRegex.Replace(text2, "");
		text2 = DivorcePairRegex.Replace(text2, "");
		return text2.Trim();
	}

	private static bool ContainsMarriageActionTags(string text)
	{
		string text2 = text ?? "";
		return LoveDeltaRegex.IsMatch(text2) || MarriageFormalPairRegex.IsMatch(text2) || MarriageFormalLegacyRegex.IsMatch(text2) || MarriageElopeRegex.IsMatch(text2) || DivorcePairRegex.IsMatch(text2);
	}

	private List<PostprocessRuleEntry> BuildRuntimeMarriagePostprocessRules(Hero speaker)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			MarriageRuntimeFacts marriageRuntimeFacts = BuildMarriageRuntimeFacts(speaker);
			string marriageRuntimeConstraintState = GetMarriageRuntimeConstraintState(marriageRuntimeFacts);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (IsMarriageFormalPostprocessReadyState(marriageRuntimeConstraintState) && Clan.PlayerClan != null && speaker?.Clan != null)
			{
				List<Hero> list2 = GetClanMembersCompat(Clan.PlayerClan).Where(IsFormalMarriageCandidate).ToList();
				List<Hero> list3 = GetClanMembersCompat(speaker.Clan).Where(IsFormalMarriageCandidate).ToList();
				foreach (Hero item in list2)
				{
					foreach (Hero item2 in list3)
					{
						if (item == null || item2 == null || item == item2 || !IsFormalMarriagePairCompatible(item, item2, out var _))
						{
							continue;
						}
						string text = "[ACTION:MARRIAGE_FORMAL:" + item.StringId + ":" + item2.StringId + "]";
						if (hashSet.Add(text))
						{
							list.Add(new PostprocessRuleEntry
							{
								Tag = text,
								Description = "如果NPC在<latest_reply>里明确同意这两人成婚，就输出这个；若同时明确了彩礼金额，也可在末尾追加 :金额"
							});
						}
					}
				}
			}
			if (IsMarriageElopePostprocessReadyState(marriageRuntimeConstraintState) && speaker != null && !string.IsNullOrWhiteSpace(speaker.StringId))
			{
				string text2 = "[ACTION:MARRIAGE_ELOPE:" + speaker.StringId + "]";
				if (hashSet.Add(text2))
				{
					list.Add(new PostprocessRuleEntry
					{
						Tag = text2,
						Description = "如果NPC在<latest_reply>里明确同意与玩家私奔，就输出这个"
					});
				}
			}
			if (_marriageRecordStorage != null && _marriageRecordStorage.Count > 0)
			{
				foreach (string value in _marriageRecordStorage.Values.ToList())
				{
					if (!TryDeserializeMarriageRecord(value, out var record) || record == null || !record.IsActive)
					{
						continue;
					}
					Hero hero = FindHeroById(record.LeftHeroId);
					Hero hero2 = FindHeroById(record.RightHeroId);
					if (hero == null || hero2 == null)
					{
						continue;
					}
					bool flag = hero.Clan == Clan.PlayerClan && hero2.Clan == speaker?.Clan;
					bool flag2 = hero2.Clan == Clan.PlayerClan && hero.Clan == speaker?.Clan;
					bool flag3 = hero == Hero.MainHero && hero2 == speaker;
					bool flag4 = hero2 == Hero.MainHero && hero == speaker;
					if ((!flag && !flag2 && !flag3 && !flag4) || !IsMarriageDivorcePostprocessEligible(speaker, flag ? hero : hero2, flag ? hero2 : hero))
					{
						continue;
					}
					Hero hero3 = flag ? hero : hero2;
					Hero hero4 = flag ? hero2 : hero;
					string text3 = "[ACTION:DIVORCE:" + hero3.StringId + ":" + hero4.StringId + "]";
					if (hashSet.Add(text3))
					{
						list.Add(new PostprocessRuleEntry
						{
							Tag = text3,
							Description = "如果NPC在<latest_reply>里明确同意这两人离婚，就输出这个。只有当NPC明确同意把钱退还给玩家时，才可在末尾追加 :金额；不要把玩家向NPC还钱写进这个标签"
						});
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] BuildRuntimeMarriagePostprocessRules failed: " + ex.Message);
		}
		return list;
	}

	private static string NormalizeMarriagePostprocessTags(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> hashSet = new HashSet<string>((rules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => (x?.Tag ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item in MarriageFormalPairRegex.Matches(raw ?? ""))
		{
			string text = (item?.Value ?? "").Trim();
			string text2 = "[ACTION:MARRIAGE_FORMAL:" + item.Groups[1].Value.Trim() + ":" + item.Groups[2].Value.Trim() + "]";
			if (!hashSet.Contains(text2) || !hashSet2.Add(text))
			{
				continue;
			}
			list.Add(text);
		}
		foreach (Match item2 in MarriageElopeRegex.Matches(raw ?? ""))
		{
			string text3 = (item2?.Value ?? "").Trim();
			if (!hashSet.Contains(text3) || !hashSet2.Add(text3))
			{
				continue;
			}
			list.Add(text3);
		}
		foreach (Match item3 in DivorcePairRegex.Matches(raw ?? ""))
		{
			string text4 = (item3?.Value ?? "").Trim();
			string text5 = "[ACTION:DIVORCE:" + item3.Groups[1].Value.Trim() + ":" + item3.Groups[2].Value.Trim() + "]";
			if (!hashSet.Contains(text5) || !hashSet2.Add(text4))
			{
				continue;
			}
			list.Add(text4);
		}
		return string.Join("\n", list.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

	private string TryRunMarriageActionPostprocess(Hero speaker, string replyText)
	{
		string text = StripMarriageActionTags(replyText);
		if (speaker == null || !AIConfigHandler.CanUseAuxiliaryActionPostprocess() || !ConsumeMarriagePostprocessContextEnabled(speaker))
		{
			return text.Trim();
		}
		string actionPostprocessSystemPrompt = AIConfigHandler.ActionPostprocessSystemPrompt;
		string actionPostprocessUserPromptTemplate = AIConfigHandler.ActionPostprocessUserPromptTemplate;
		if (string.IsNullOrWhiteSpace(actionPostprocessSystemPrompt) || string.IsNullOrWhiteSpace(actionPostprocessUserPromptTemplate))
		{
			return text.Trim();
		}
		List<PostprocessRuleEntry> list = BuildRuntimeMarriagePostprocessRules(speaker);
		if (list == null || list.Count == 0)
		{
			try
			{
				MarriageRuntimeFacts marriageRuntimeFacts = BuildMarriageRuntimeFacts(speaker);
				string marriageRuntimeConstraintState = GetMarriageRuntimeConstraintState(marriageRuntimeFacts);
				Logger.Log("Romance", "[MarriagePostprocess] skipped: no runtime rules"
					+ $" speaker={(speaker?.StringId ?? "")}"
					+ $" state={marriageRuntimeConstraintState}"
					+ $" isLeader={(marriageRuntimeFacts?.IsLeader ?? false)}"
					+ $" canElope={(marriageRuntimeFacts?.CanElope ?? false)}"
					+ $" pairAvailable={(marriageRuntimeFacts?.PairAvailable ?? false)}");
			}
			catch
			{
			}
			return text.Trim();
		}
		string text2 = BuildMarriagePostprocessRuleText(list);
		string text3 = BuildMarriagePostprocessRuleText(AIConfigHandler.ActionPostprocessMoodRules);
		string text4 = BuildMarriagePostprocessPlayerCandidatesBlock(speaker);
		string text5 = BuildMarriagePostprocessTargetCandidatesBlock(speaker);
		string text6 = BuildMarriagePostprocessFactHintBlock(speaker);
		string text7 = AIConfigHandler.BuildActionPostprocessSystemPrompt(text2, text3, speaker?.Name?.ToString() ?? "NPC", null, null, null, text4, text5, text6);
		string text8 = actionPostprocessUserPromptTemplate.Replace("{history}", "（无）")
			.Replace("{reply}", text);
		if (!AIConfigHandler.TryCallAuxiliaryActionPostprocess(text7, text8, 5000, 0f, out var content, out var error))
		{
			Logger.Log("Romance", "[MarriagePostprocess] 调用失败: " + error);
			return text.Trim();
		}
		string text9 = NormalizeMarriagePostprocessTags(content, list);
		if (string.IsNullOrWhiteSpace(text9))
		{
			return text.Trim();
		}
		string text10 = (text + "\n" + text9).Trim();
		Logger.Log("Romance", "[MarriagePostprocess] RAW=\n" + content + "\nFINAL=\n" + text10 + "\n");
		return text10;
	}

	public void ApplyMarriageTags(Hero speaker, Hero receiver, ref string responseText)
	{
		try
		{
			string text = responseText ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (!ContainsMarriageActionTags(text))
			{
				text = TryRunMarriageActionPostprocess(speaker, text);
			}
			string value = "";
			MatchCollection matchCollection = LoveDeltaRegex.Matches(text);
			for (int i = 0; i < matchCollection.Count; i++)
			{
				Match match = matchCollection[i];
				Hero hero = ResolveTargetHeroToken(speaker, match.Groups[1].Value);
				if (hero != null && int.TryParse(match.Groups[2].Value, out var result))
				{
					if (result < -20)
					{
						result = -20;
					}
					if (result > 20)
					{
						result = 20;
					}
					AdjustPrivateLove(hero, result, "llm_tag");
				}
			}
			MatchCollection matchCollection2 = MarriageFormalPairRegex.Matches(text);
			for (int j = 0; j < matchCollection2.Count; j++)
			{
				Match match2 = matchCollection2[j];
				Hero hero2 = ResolvePlayerClanHeroToken(match2.Groups[1].Value);
				Hero hero3 = ResolveTargetHeroToken(speaker, match2.Groups[2].Value);
				int? bridePrice = null;
				if (match2.Groups.Count > 3 && int.TryParse(match2.Groups[3].Value, out var result2))
				{
					bridePrice = result2;
				}
				string status = "";
				if (hero2 == null)
				{
					status = "正规联姻失败：未找到玩家家族婚配人选（playerClanHeroId）。";
				}
				else if (hero3 == null)
				{
					status = "正规联姻失败：未找到对方婚配对象（targetHeroId）。";
				}
				else if (TryExecuteFormalMarriage(speaker, hero2, hero3, bridePrice, out status))
				{
					value = status;
				}
				else
				{
					value = status;
				}
			}
			string text2 = MarriageFormalPairRegex.Replace(text, "");
			MatchCollection matchCollection3 = MarriageFormalLegacyRegex.Matches(text2);
			for (int k = 0; k < matchCollection3.Count; k++)
			{
				Match match3 = matchCollection3[k];
				Hero hero4 = ResolveTargetHeroToken(speaker, match3.Groups[1].Value);
				int? bridePrice2 = null;
				if (match3.Groups.Count > 2 && int.TryParse(match3.Groups[2].Value, out var result3))
				{
					bridePrice2 = result3;
				}
				string status2 = "";
				if (hero4 == null)
				{
					status2 = "正规联姻失败：未找到对方婚配对象（targetHeroId）。";
				}
				else if (TryExecuteFormalMarriage(speaker, Hero.MainHero, hero4, bridePrice2, out status2))
				{
					value = status2;
				}
				else
				{
					value = status2;
				}
			}
			MatchCollection matchCollection4 = MarriageElopeRegex.Matches(text2);
			for (int l = 0; l < matchCollection4.Count; l++)
			{
				Match match4 = matchCollection4[l];
				Hero hero5 = ResolveTargetHeroToken(speaker, match4.Groups[1].Value);
				string status3 = "";
				if (hero5 == null)
				{
					status3 = "私奔失败：未找到目标对象（targetHeroId）。";
				}
				else if (TryExecuteElopeMarriage(speaker, hero5, out status3))
				{
					value = status3;
				}
				else
				{
					value = status3;
				}
			}
			MatchCollection matchCollection5 = DivorcePairRegex.Matches(text2);
			for (int m = 0; m < matchCollection5.Count; m++)
			{
				Match match5 = matchCollection5[m];
				Hero hero6 = ResolvePlayerClanHeroToken(match5.Groups[1].Value);
				Hero hero7 = ResolveTargetHeroToken(speaker, match5.Groups[2].Value);
				int? refundAmount = null;
				if (match5.Groups.Count > 3 && int.TryParse(match5.Groups[3].Value, out var result4))
				{
					refundAmount = result4;
				}
				string status4 = "";
				if (hero6 == null)
				{
					status4 = "离婚失败：未找到玩家家族离婚对象（playerClanHeroId）。";
				}
				else if (hero7 == null)
				{
					status4 = "离婚失败：未找到对方离婚对象（targetHeroId）。";
				}
				else if (TryExecuteDivorce(speaker, hero6, hero7, refundAmount, out status4))
				{
					value = status4;
				}
				else
				{
					value = status4;
				}
			}
			text = LoveDeltaRegex.Replace(text, "");
			text = MarriageFormalPairRegex.Replace(text, "");
			text = MarriageFormalLegacyRegex.Replace(text, "");
			text = MarriageElopeRegex.Replace(text, "");
			text = DivorcePairRegex.Replace(text, "");
			responseText = text.Trim();
			if (!string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage("[婚姻系统] " + value, Color.FromUint(4283878655u)));
				Logger.Log("Romance", value);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] ApplyMarriageTags 异常: " + ex);
		}
	}
}
