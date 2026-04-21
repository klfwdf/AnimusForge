using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class RomanceSystemBehavior : CampaignBehaviorBase
{
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

	private static readonly Regex DivorcePairRegex = new Regex("\\[ACTION:DIVORCE:([^\\]:]+):([^\\]:]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private Dictionary<string, int> _privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static RomanceSystemBehavior Instance { get; private set; }

	public RomanceSystemBehavior()
	{
		Instance = this;
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
		dataStore.SyncData<Dictionary<string, int>>("_romancePrivateLove_v1", ref _privateLove);
		if (_marriageRecordStorage == null)
		{
			_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData<Dictionary<string, string>>("_romanceMarriageRecords_v1", ref _marriageRecordStorage);
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
			string text = list[i];
			if (string.IsNullOrWhiteSpace(text))
			{
				_privateLove.Remove(text);
				continue;
			}
			int num = ClampLove(_privateLove[text]);
			if (num == 0)
			{
				_privateLove.Remove(text);
			}
			else
			{
				_privateLove[text] = num;
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
		return string.Join("\u001f", NormalizeId(record.LeftHeroId), NormalizeId(record.RightHeroId), record.Type ?? "", record.PayerClanId ?? "", record.ReceiverClanId ?? "", record.BridePriceAmount.ToString(), record.IsActive ? "1" : "0");
	}

	private static bool TryDeserializeMarriageRecord(string raw, out MarriageRecord record)
	{
		record = null;
		string[] array = (raw ?? "").Split('\u001f');
		if (array.Length < 7)
		{
			return false;
		}
		record = new MarriageRecord
		{
			LeftHeroId = NormalizeId(array[0]),
			RightHeroId = NormalizeId(array[1]),
			Type = (array[2] ?? ""),
			PayerClanId = (array[3] ?? ""),
			ReceiverClanId = (array[4] ?? ""),
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
			Hero val = Hero.Find(text);
			if (val != null)
			{
				return val;
			}
		}
		catch
		{
		}
		try
		{
			return Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals(NormalizeId(((MBObjectBase)x).StringId), text, StringComparison.OrdinalIgnoreCase)));
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
			return ((IEnumerable<Clan>)Clan.All)?.FirstOrDefault((Clan x) => x != null && string.Equals(NormalizeId(((MBObjectBase)x).StringId), text, StringComparison.OrdinalIgnoreCase));
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
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (_marriageRecordStorage == null)
			{
				_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
			_marriageRecordStorage[text] = SerializeMarriageRecord(record);
		}
	}

	private MarriageRecord GetMarriageRecord(Hero left, Hero right)
	{
		string text = BuildMarriageRecordKey((left != null) ? ((MBObjectBase)left).StringId : null, (right != null) ? ((MBObjectBase)right).StringId : null);
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

	private static string GetHeroDisplayWithClan(Hero hero)
	{
		if (hero == null)
		{
			return "未知对象";
		}
		return GetClanNameSafe(hero) + "的" + (((object)hero.Name)?.ToString() ?? ((MBObjectBase)hero).StringId ?? "未知");
	}

	private static string BuildBridePriceSummary(MarriageRecord record)
	{
		if (record == null || record.BridePriceAmount <= 0)
		{
			return "无明确彩礼记录";
		}
		Clan val = FindClanById(record.PayerClanId);
		Clan val2 = FindClanById(record.ReceiverClanId);
		string arg = ((val == null) ? null : ((object)val.Name)?.ToString()) ?? "未知家族";
		string arg2 = ((val2 == null) ? null : ((object)val2.Name)?.ToString()) ?? "未知家族";
		return $"由{arg}向{arg2}支付彩礼 {record.BridePriceAmount:N0} 第纳尔";
	}

	private IEnumerable<string> EnumerateActiveMarriageSituationLines(Hero speaker)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Hero mainHero = Hero.MainHero;
		Clan clan = ((mainHero != null) ? mainHero.Clan : null);
		Clan clan2 = ((speaker != null) ? speaker.Clan : null);
		if (clan != null && clan2 != null)
		{
			foreach (Hero item in GetClanMembersCompat(clan))
			{
				Hero spouse = ((item != null) ? item.Spouse : null);
				if (item != null && spouse != null && spouse.Clan == clan2)
				{
					string text = BuildMarriageRecordKey(((MBObjectBase)item).StringId, ((MBObjectBase)spouse).StringId);
					if (hashSet.Add(text))
					{
						MarriageRecord marriageRecord = GetMarriageRecord(item, spouse);
						yield return "- " + GetHeroDisplayWithClan(item) + " 与 " + GetHeroDisplayWithClan(spouse) + " 已成婚；" + BuildBridePriceSummary(marriageRecord) + "。";
					}
				}
			}
		}
		if (speaker != null && Hero.MainHero != null && (speaker.Spouse == Hero.MainHero || Hero.MainHero.Spouse == speaker))
		{
			string text2 = BuildMarriageRecordKey(((MBObjectBase)Hero.MainHero).StringId, ((MBObjectBase)speaker).StringId);
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
			List<string> list = (from x in EnumerateActiveMarriageSituationLines(speaker)
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【你们两家的婚配情况】");
			if (list.Count <= 0)
			{
				stringBuilder.Append("当前未发现玩家家族与你方家族之间的已成婚记录。");
			}
			else
			{
				for (int num = 0; num < list.Count; num++)
				{
					stringBuilder.AppendLine(list[num]);
				}
				stringBuilder.Append("若谈离婚，必须明确是哪两人离婚。若你们愿意返还彩礼，可按以上明细协商返还金额；也可以明确表示不返还。");
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
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
		string text = (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim().ToLowerInvariant();
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
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected O, but got Unknown
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
		Logger.Log("Romance", $"hero={((MBObjectBase)hero).StringId} reason={reason} love={privateLove}->{num}");
		int num2 = num - privateLove;
		if (num2 == 0)
		{
			return;
		}
		try
		{
			string text2 = ((num2 > 0) ? "+" : "") + num2;
			Color val = ((num2 > 0) ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u));
			InformationManager.DisplayMessage(new InformationMessage("[私人关系] " + ((object)hero.Name)?.ToString() + " " + text2 + "（当前" + num + "）", val));
		}
		catch
		{
		}
	}

	public void AdjustPrivateLove(Hero hero, int delta, string reason)
	{
		if (hero != null && delta != 0)
		{
			SetPrivateLove(hero, GetPrivateLove(hero) + delta, reason);
		}
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
			string text3 = BuildMarriageSuitabilityHint(left, right);
			if (ShouldForceMarriageDespiteEncounterRuntimeBlockers(left, right, out var reason))
			{
				Logger.Log("Romance", "[WARN] MarriageAction switching to forced encounter-safe path: " + reason);
				Logger.Log("Romance", "[WARN] MarriageAction left=" + text);
				Logger.Log("Romance", "[WARN] MarriageAction right=" + text2);
				return TryForceApplyMarriageAction(left, right, out failReason);
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				failReason = text3;
				Logger.Log("Romance", "[WARN] MarriageAction blocked by precheck: " + text3);
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
			MethodInfo method = type.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public, null, new Type[3]
			{
				typeof(Hero),
				typeof(Hero),
				typeof(bool)
			}, null);
			if (method != null)
			{
				method.Invoke(null, new object[3] { left, right, true });
				if (((left != null) ? left.Spouse : null) == right && ((right != null) ? right.Spouse : null) == left)
				{
					return true;
				}
				failReason = "MarriageAction 未抛异常，但婚姻未实际建立。";
				Logger.Log("Romance", "[WARN] MarriageAction returned without marriage state change.");
				Logger.Log("Romance", "[WARN] MarriageAction left(after)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(after)=" + DescribeHeroMarriageState(right));
				return false;
			}
			MethodInfo method2 = type.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public, null, new Type[2]
			{
				typeof(Hero),
				typeof(Hero)
			}, null);
			if (method2 != null)
			{
				method2.Invoke(null, new object[2] { left, right });
				if (((left != null) ? left.Spouse : null) == right && ((right != null) ? right.Spouse : null) == left)
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
			if (((left != null) ? left.Spouse : null) == right && ((right != null) ? right.Spouse : null) == left)
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
		catch (Exception ex3)
		{
			if (((left != null) ? left.Spouse : null) == right && ((right != null) ? right.Spouse : null) == left)
			{
				Logger.Log("Romance", "[WARN] MarriageAction wrapper threw after marriage state changed: " + ex3);
				Logger.Log("Romance", "[WARN] MarriageAction left(partial-success)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(partial-success)=" + DescribeHeroMarriageState(right));
				TryEmitMarriageFallbackNotifications(left, right);
				return true;
			}
			failReason = ex3.GetType().Name + ": " + ex3.Message;
			Logger.Log("Romance", "[ERROR] MarriageAction invoke wrapper failed: " + ex3);
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
			Campaign current = Campaign.Current;
			if (current != null)
			{
				ConversationManager conversationManager = current.ConversationManager;
				if (((conversationManager != null) ? new bool?(conversationManager.IsConversationInProgress) : ((bool?)null)) == true)
				{
					return true;
				}
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
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty partyBelongedTo2 = hero.PartyBelongedTo;
			if (((partyBelongedTo2 != null) ? partyBelongedTo2.Army : null) != null)
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
			Campaign current = Campaign.Current;
			if (current != null)
			{
				GameModels models = current.Models;
				bool? obj;
				if (models == null)
				{
					obj = null;
				}
				else
				{
					MarriageModel marriageModel = models.MarriageModel;
					obj = ((marriageModel != null) ? new bool?(marriageModel.IsCoupleSuitableForMarriage(left, right)) : ((bool?)null));
				}
				bool? flag = obj;
				if (flag == true)
				{
					return false;
				}
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
		Campaign current = Campaign.Current;
		object obj;
		if (current == null)
		{
			obj = null;
		}
		else
		{
			GameModels models = current.Models;
			obj = ((models != null) ? models.MarriageModel : null);
		}
		MarriageModel val = (MarriageModel)obj;
		if (val == null)
		{
			reason = "未找到原版 MarriageModel。";
			return false;
		}
		if (!val.IsClanSuitableForMarriage(left.Clan) || !val.IsClanSuitableForMarriage(right.Clan))
		{
			reason = "有一方家族当前不适合结婚。";
			return false;
		}
		try
		{
			Clan clan = left.Clan;
			if (((clan != null) ? clan.Leader : null) == left)
			{
				Clan clan2 = right.Clan;
				if (((clan2 != null) ? clan2.Leader : null) == right)
				{
					reason = "双方都是族长。";
					return false;
				}
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
			MethodInfo method = ((object)val).GetType().GetMethod("AreHeroesRelated", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method == null)
			{
				method = typeof(DefaultMarriageModel).GetMethod("AreHeroesRelated", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			bool flag = default(bool);
			int num;
			if (method != null)
			{
				object obj4 = method.Invoke(val, new object[3] { left, right, 3 });
				if (obj4 is bool)
				{
					flag = (bool)obj4;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
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
			int num;
			if (!hero.IsFemale)
			{
				Campaign current = Campaign.Current;
				int? obj5;
				if (current == null)
				{
					obj5 = null;
				}
				else
				{
					GameModels models = current.Models;
					if (models == null)
					{
						obj5 = null;
					}
					else
					{
						MarriageModel marriageModel = models.MarriageModel;
						obj5 = ((marriageModel != null) ? new int?(marriageModel.MinimumMarriageAgeMale) : ((int?)null));
					}
				}
				num = obj5 ?? 18;
			}
			else
			{
				Campaign current2 = Campaign.Current;
				int? obj6;
				if (current2 == null)
				{
					obj6 = null;
				}
				else
				{
					GameModels models2 = current2.Models;
					if (models2 == null)
					{
						obj6 = null;
					}
					else
					{
						MarriageModel marriageModel2 = models2.MarriageModel;
						obj6 = ((marriageModel2 != null) ? new int?(marriageModel2.MinimumMarriageAgeFemale) : ((int?)null));
					}
				}
				num = obj6 ?? 18;
			}
			int num2 = num;
			CharacterObject characterObject = hero.CharacterObject;
			if (characterObject != null && ((BasicCharacterObject)characterObject).Age < (float)num2)
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
			bool flag = true;
			((CampaignEventReceiver)CampaignEventDispatcher.Instance).CanHeroMarry(hero, ref flag);
			if (!flag)
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
			Campaign current = Campaign.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameModels models = current.Models;
				obj = ((models != null) ? models.MarriageModel : null);
			}
			MarriageModel val = (MarriageModel)obj;
			if (val == null)
			{
				failReason = "未找到原版 MarriageModel。";
				return false;
			}
			Hero val2 = left;
			Hero val3 = (val2.Spouse = right);
			val3.Spouse = val2;
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(val2, val3, val.GetEffectiveRelationIncrease(val2, val3), false);
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] Force marriage relation increase failed: " + ex);
			}
			Clan clanAfterMarriage = val.GetClanAfterMarriage(val2, val3);
			if (clanAfterMarriage != val2.Clan)
			{
				Hero val5 = val2;
				val2 = val3;
				val3 = val5;
			}
			bool flag = false;
			try
			{
				((CampaignEventReceiver)CampaignEventDispatcher.Instance).OnBeforeHeroesMarried(val2, val3, true);
			}
			catch (Exception ex2)
			{
				flag = true;
				Logger.Log("Romance", "[WARN] Force marriage event chain failed, will continue and backfill notifications: " + ex2);
			}
			if (val2.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(val2, clanAfterMarriage);
			}
			if (val3.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(val3, clanAfterMarriage);
			}
			try
			{
				MethodInfo method = typeof(Romance).GetMethod("EndAllCourtships", BindingFlags.Static | BindingFlags.NonPublic);
				if (method != null)
				{
					method.Invoke(null, new object[1] { val2 });
					method.Invoke(null, new object[1] { val3 });
				}
			}
			catch (Exception ex3)
			{
				Logger.Log("Romance", "[WARN] Force marriage end courtships failed: " + ex3);
			}
			try
			{
				ChangeRomanticStateAction.Apply(val2, val3, (RomanceLevelEnum)7);
			}
			catch (Exception ex4)
			{
				Logger.Log("Romance", "[WARN] Force marriage romantic state update failed: " + ex4);
			}
			if (((left != null) ? left.Spouse : null) == right && ((right != null) ? right.Spouse : null) == left)
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
		catch (Exception ex5)
		{
			if (((left != null) ? left.Spouse : null) == right && ((right != null) ? right.Spouse : null) == left)
			{
				Logger.Log("Romance", "[WARN] Force marriage threw after marriage state changed: " + ex5);
				TryEmitMarriageFallbackNotifications(left, right);
				return true;
			}
			failReason = ex5.GetType().Name + ": " + ex5.Message;
			Logger.Log("Romance", "[ERROR] Force marriage failed: " + ex5);
			return false;
		}
	}

	private static void HandleClanChangeAfterMarriageCompat(Hero hero, Clan clanAfterMarriage)
	{
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
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
					IFaction kingdom = (IFaction)(object)clanAfterMarriage.Kingdom;
					FactionHelper.FinishAllRelatedHostileActionsOfNobleToFaction(hero, (IFaction)(((object)kingdom) ?? ((object)clanAfterMarriage)));
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
			foreach (Hero item in (List<Hero>)(object)clan.Heroes)
			{
				if (item != null)
				{
					item.UpdateHomeSettlement();
				}
			}
			foreach (Hero item2 in (List<Hero>)(object)clanAfterMarriage.Heroes)
			{
				if (item2 != null)
				{
					item2.UpdateHomeSettlement();
				}
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
			Campaign current = Campaign.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameModels models = current.Models;
				obj = ((models != null) ? models.MarriageModel : null);
			}
			MarriageModel val = (MarriageModel)obj;
			if (val != null && !val.IsCoupleSuitableForMarriage(left, right))
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
			Clan clan = left.Clan;
			if (((clan != null) ? clan.Leader : null) == left)
			{
				Clan clan2 = right.Clan;
				if (((clan2 != null) ? clan2.Leader : null) == right)
				{
					list.Add("双方都是族长");
				}
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
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) != null)
			{
				reasons.Add(sideLabel + "正在地图事件中");
			}
		}
		catch
		{
		}
		try
		{
			MobileParty partyBelongedTo2 = hero.PartyBelongedTo;
			if (((partyBelongedTo2 != null) ? partyBelongedTo2.Army : null) != null)
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
			list.Add("id=" + ((MBObjectBase)hero).StringId);
		}
		catch
		{
		}
		try
		{
			list.Add("name=" + (object)hero.Name);
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
			Clan clan = hero.Clan;
			list.Add("clan=" + (((clan != null) ? ((MBObjectBase)clan).StringId : null) ?? "null"));
		}
		catch
		{
		}
		try
		{
			Clan clan2 = hero.Clan;
			list.Add("leader=" + ((((clan2 != null) ? clan2.Leader : null) == hero) ? "true" : "false"));
		}
		catch
		{
		}
		try
		{
			Hero spouse = hero.Spouse;
			list.Add("spouse=" + (((spouse != null) ? ((MBObjectBase)spouse).StringId : null) ?? "null"));
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
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			list.Add("mapEvent=" + ((((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) != null) ? "true" : "false"));
		}
		catch
		{
		}
		try
		{
			MobileParty partyBelongedTo2 = hero.PartyBelongedTo;
			list.Add("army=" + ((((partyBelongedTo2 != null) ? partyBelongedTo2.Army : null) != null) ? "true" : "false"));
		}
		catch
		{
		}
		return string.Join(", ", list);
	}

	private static void TryEmitMarriageFallbackNotifications(Hero left, Hero right)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		try
		{
			if (left == null || right == null)
			{
				return;
			}
			Hero val = (left.IsFemale ? right : left);
			Hero val2 = (left.IsFemale ? left : right);
			bool flag = LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive || Mission.Current != null;
			if (!flag)
			{
				MBInformationManager.ShowSceneNotification((SceneNotificationData)new MarriageSceneNotificationItem(val, val2, CampaignTime.Now, (RelevantContextType)0));
			}
			CharacterMarriedLogEntry val3 = new CharacterMarriedLogEntry(left, right);
			LogEntry.AddLogEntry((LogEntry)(object)val3);
			if (left.Clan == Clan.PlayerClan || right.Clan == Clan.PlayerClan)
			{
				Campaign current = Campaign.Current;
				if (current != null)
				{
					CampaignInformationManager campaignInformationManager = current.CampaignInformationManager;
					if (campaignInformationManager != null)
					{
						campaignInformationManager.NewMapNoticeAdded((InformationData)new MarriageMapNotification(left, right, val3.GetEncyclopediaText(), CampaignTime.Now));
					}
				}
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
			PropertyInfo property = ((object)clan).GetType().GetProperty("Lords", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				enumerable = property.GetValue(clan, null) as IEnumerable;
			}
			if (enumerable == null)
			{
				PropertyInfo property2 = ((object)clan).GetType().GetProperty("Heroes", BindingFlags.Instance | BindingFlags.Public);
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
			Hero hero = (Hero)((item is Hero) ? item : null);
			if (hero != null && hero != null)
			{
				yield return hero;
			}
		}
	}

	private static int GetMarriageCandidateMaxAgeSetting()
	{
		try
		{
			int num = DuelSettings.GetSettings()?.MarriageCandidateMaxAge ?? 55;
			if (num < 18)
			{
				num = 18;
			}
			return num;
		}
		catch
		{
			return 55;
		}
	}

	private static int GetMarriageCandidateMaxAgeGapSetting()
	{
		try
		{
			int num = DuelSettings.GetSettings()?.MarriageCandidateMaxAgeGap ?? 25;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
		catch
		{
			return 25;
		}
	}

	private static bool GetMarriageRequireOppositeGenderSetting()
	{
		try
		{
			return DuelSettings.GetSettings()?.MarriageRequireOppositeGender ?? true;
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
			if (hero.Age < 18f || hero.Age > (float)marriageCandidateMaxAgeSetting)
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
			int num = GetRelationSafe(target, leader);
			if (leader == target)
			{
				num = 40;
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
			double num2 = ((list.Count > 0) ? list.Average() : 0.0);
			List<int> list2 = new List<int>();
			foreach (Hero item in GetClanMembersCompat(target.Clan))
			{
				if (item != null && item != target && !(item.Age < 18f))
				{
					list2.Add(GetRelationSafe(target, item));
				}
			}
			double num3 = ((list2.Count > 0) ? list2.Average() : 0.0);
			int num4 = (int)Math.Round((double)num * 0.6 + num2 * 0.25 + num3 * 0.15);
			if (num4 < -100)
			{
				num4 = -100;
			}
			if (num4 > 100)
			{
				num4 = 100;
			}
			return num4;
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
			Hero val = Hero.Find(text);
			if (val != null)
			{
				return val;
			}
		}
		catch
		{
		}
		if (speaker != null && !string.IsNullOrWhiteSpace(((object)speaker.Name)?.ToString()) && string.Equals(text, ((object)speaker.Name).ToString(), StringComparison.OrdinalIgnoreCase))
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
		Hero val = null;
		try
		{
			val = Hero.Find(text);
		}
		catch
		{
			val = null;
		}
		if (val == null)
		{
			try
			{
				val = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)));
			}
			catch
			{
				val = null;
			}
		}
		if (val == null && string.Equals(text, ((object)mainHero.Name)?.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
		{
			val = mainHero;
		}
		if (val != null && val.Clan == mainHero.Clan)
		{
			return val;
		}
		return null;
	}

	private static int GetClanRelationWithPlayer(Hero clanLeader)
	{
		return GetRelationSafe(clanLeader, Hero.MainHero);
	}

	private static string GetClanNameSafe(Hero hero)
	{
		object obj;
		if (hero == null)
		{
			obj = null;
		}
		else
		{
			Clan clan = hero.Clan;
			obj = ((clan == null) ? null : ((object)clan.Name)?.ToString());
		}
		if (obj == null)
		{
			obj = "";
		}
		string text = (string)obj;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return "无家族";
	}

	private void RegisterMarriageRecord(Hero left, Hero right, string marriageType, Clan payerClan, Clan receiverClan, int bridePriceAmount)
	{
		if (left != null && right != null)
		{
			SaveMarriageRecord(new MarriageRecord
			{
				LeftHeroId = NormalizeId(((MBObjectBase)left).StringId),
				RightHeroId = NormalizeId(((MBObjectBase)right).StringId),
				Type = (marriageType ?? ""),
				PayerClanId = NormalizeId((payerClan != null) ? ((MBObjectBase)payerClan).StringId : null),
				ReceiverClanId = NormalizeId((receiverClan != null) ? ((MBObjectBase)receiverClan).StringId : null),
				BridePriceAmount = Math.Max(0, bridePriceAmount),
				IsActive = true
			});
		}
	}

	private void RecordFormalMarriageFacts(Hero leader, Hero playerClanHero, Hero targetHero, Clan payerClan, Clan receiverClan, int bridePriceAmount)
	{
		if (playerClanHero != null && targetHero != null)
		{
			RegisterMarriageRecord(playerClanHero, targetHero, "formal", payerClan, receiverClan, bridePriceAmount);
			string clanNameSafe = GetClanNameSafe(playerClanHero);
			string clanNameSafe2 = GetClanNameSafe(targetHero);
			string text = $"{clanNameSafe}的{playerClanHero.Name}";
			string text2 = $"{clanNameSafe2}的{targetHero.Name}";
			string text3 = BuildBridePriceSummary(GetMarriageRecord(playerClanHero, targetHero));
			if (leader != null)
			{
				MyBehavior.AppendExternalNpcFact(leader, "你已经同意 " + text + " 与 " + text2 + " 正式成婚。" + text3 + "。");
			}
			if (targetHero != leader)
			{
				MyBehavior.AppendExternalNpcFact(targetHero, "你已经与 " + text + " 正式成婚。" + text3 + "。");
			}
			if (playerClanHero == Hero.MainHero)
			{
				MyBehavior.AppendExternalPlayerFact(Hero.MainHero, "你已经与 " + text2 + " 正式成婚。" + text3 + "。");
			}
			else
			{
				MyBehavior.AppendExternalNpcFact(playerClanHero, "你已经与 " + text2 + " 正式成婚。" + text3 + "。");
			}
		}
	}

	private void RecordElopeMarriageFacts(Hero targetHero)
	{
		if (targetHero != null)
		{
			RegisterMarriageRecord(Hero.MainHero, targetHero, "elope", null, null, 0);
			string clanNameSafe = GetClanNameSafe(targetHero);
			MyBehavior.AppendExternalPlayerFact(Hero.MainHero, $"你已经与 {clanNameSafe}的{targetHero.Name} 私奔并成婚。");
			MyBehavior.AppendExternalNpcFact(targetHero, "你已经与玩家私奔并成婚。");
			Clan clan = targetHero.Clan;
			Hero val = ((clan != null) ? clan.Leader : null);
			if (val != null && val != targetHero)
			{
				MyBehavior.AppendExternalNpcFact(val, $"玩家已经与 {clanNameSafe}的{targetHero.Name} 私奔并成婚。");
			}
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
		Hero mainHero = Hero.MainHero;
		int? obj;
		if (mainHero == null)
		{
			obj = null;
		}
		else
		{
			Clan clan = mainHero.Clan;
			obj = ((clan != null) ? new int?(clan.Tier) : ((int?)null));
		}
		int? num = obj;
		int valueOrDefault = num.GetValueOrDefault();
		Clan clan2 = targetHero.Clan;
		int num2 = ((clan2 != null) ? clan2.Tier : 0);
		int num3 = valueOrDefault - num2;
		if (num3 <= -3)
		{
			status = $"正规联姻失败：家族等级差距过大（玩家={valueOrDefault}，对方={num2}）。";
			return false;
		}
		int clanRelationWithPlayer = GetClanRelationWithPlayer(leader);
		int num4 = RewardSystemBehavior.Instance?.GetEffectiveTrust(leader) ?? 0;
		int num5 = ((bridePrice.HasValue && bridePrice.Value > 0) ? bridePrice.Value : 0);
		int num6 = 0;
		bool flag = num3 <= -1;
		bool flag2 = num3 >= 1;
		if (flag && num5 > 0)
		{
			num6 = RewardSystemBehavior.Instance?.GetPlayerPrepaidGoldForExternal(leader) ?? 0;
		}
		if (num3 == -1)
		{
			if (clanRelationWithPlayer < 20 || num4 < 20)
			{
				status = $"正规联姻失败：门槛不足（家族关系={clanRelationWithPlayer}，族长综合信任={num4}；需要 >=20 / >=20）。";
				return false;
			}
		}
		else if (num3 != -2)
		{
			int num7 = ((num3 <= 0) ? 5 : (-5 * num3));
			if (num7 < -20)
			{
				num7 = -20;
			}
			if (clanRelationWithPlayer < num7 || num4 < num7)
			{
				status = $"正规联姻失败：门槛不足（当前关系={clanRelationWithPlayer}，信任={num4}；需要 >= {num7}）。";
				return false;
			}
			if (flag && num5 > 0 && num6 < num5)
			{
				status = $"正规联姻失败：彩礼尚未交足。约定彩礼 {num5:N0}，当前已主动交给族长 {num6:N0}，还差 {Math.Max(0, num5 - num6):N0}。";
				return false;
			}
		}
		if (flag && num5 > 0 && num6 < num5)
		{
			status = $"正规联姻失败：彩礼尚未交足。约定彩礼 {num5:N0}，当前已主动交给族长 {num6:N0}，还差 {Math.Max(0, num5 - num6):N0}。";
			return false;
		}
		if (flag2 && num5 > 0 && ((leader != null) ? new int?(leader.Gold) : ((int?)null)).GetValueOrDefault() < num5)
		{
			status = $"正规联姻失败：族长当前金币不足，无法支付承诺给玩家的彩礼 {num5:N0}。";
			return false;
		}
		if (!TryApplyMarriageAction(playerClanHero, targetHero, out var failReason))
		{
			status = "正规联姻失败：执行 MarriageAction 失败，" + failReason;
			return false;
		}
		int num8 = 0;
		if (flag && num5 > 0)
		{
			num8 = RewardSystemBehavior.Instance?.ConsumePlayerPrepaidGoldForExternal(leader, num5) ?? 0;
		}
		int num9 = 0;
		if (flag2 && num5 > 0 && leader != null)
		{
			num9 = Math.Min(num5, Math.Max(0, leader.Gold));
			if (num9 > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(leader, Hero.MainHero, num9, false);
			}
		}
		AdjustPrivateLove(targetHero, 10, "formal_marriage_success");
		Clan payerClan = null;
		Clan receiverClan = null;
		int bridePriceAmount = 0;
		if (num8 > 0)
		{
			Hero mainHero2 = Hero.MainHero;
			payerClan = ((mainHero2 != null) ? mainHero2.Clan : null);
			receiverClan = ((leader != null) ? leader.Clan : null);
			bridePriceAmount = num8;
		}
		else if (num9 > 0)
		{
			payerClan = ((leader != null) ? leader.Clan : null);
			Hero mainHero3 = Hero.MainHero;
			receiverClan = ((mainHero3 != null) ? mainHero3.Clan : null);
			bridePriceAmount = num9;
		}
		RecordFormalMarriageFacts(leader, playerClanHero, targetHero, payerClan, receiverClan, bridePriceAmount);
		status = $"正规联姻成功：{playerClanHero.Name} 与 {targetHero.Name} 已成婚。等级差D={num3}，家族关系={clanRelationWithPlayer}，族长综合信任={num4}。";
		if (num8 > 0)
		{
			status += $" 已核销你先前主动交付给族长的彩礼 {num8:N0} 第纳尔。";
		}
		if (num9 > 0)
		{
			status += $" 对方族长已向玩家支付彩礼 {num9:N0} 第纳尔。";
			MyBehavior.AppendExternalNpcFact(leader, $"你已经向玩家支付了彩礼 {num9:N0} 第纳尔。");
			MyBehavior.AppendExternalPlayerFact(Hero.MainHero, string.Format("你已经从 {0} 收到了彩礼 {1:N0} 第纳尔。", ((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "对方族长", num9));
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
		Clan clan = targetHero.Clan;
		Hero val = ((clan != null) ? clan.Leader : null);
		int clanRelationWithPlayer = GetClanRelationWithPlayer(val);
		int num = RewardSystemBehavior.Instance?.GetEffectiveTrust(val) ?? 0;
		int num2 = RewardSystemBehavior.Instance?.GetNpcTrust(targetHero) ?? 0;
		int privateLove = GetPrivateLove(targetHero);
		int num3 = 70;
		int num4 = 20;
		if (clanRelationWithPlayer < -20 || num < -20)
		{
			num3 = 85;
			num4 = 35;
		}
		int num5 = ComputeTargetFamilyHarmony(targetHero);
		if (num5 <= -40)
		{
			num3 -= 15;
			num4 -= 10;
		}
		else if (num5 <= -20)
		{
			num3 -= 8;
			num4 -= 5;
		}
		else if (num5 >= 20)
		{
			num3 += 10;
			num4 += 5;
		}
		if (num3 < 20)
		{
			num3 = 20;
		}
		if (num4 < -20)
		{
			num4 = -20;
		}
		if (privateLove < num3 || num2 < num4)
		{
			AdjustPrivateLove(targetHero, -3, "elope_rejected");
			status = $"私奔失败：门槛不足（L={privateLove}/{num3}，对象私人信任={num2}/{num4}，对象家族融洽度F={num5}）。";
			return false;
		}
		if (!TryApplyMarriageAction(Hero.MainHero, targetHero, out var failReason))
		{
			status = "私奔失败：执行 MarriageAction 失败，" + failReason;
			return false;
		}
		AdjustPrivateLove(targetHero, 12, "elope_success");
		RecordElopeMarriageFacts(targetHero);
		if (val != null && val != targetHero)
		{
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, val, -25, true);
			}
			catch
			{
			}
			RewardSystemBehavior.Instance?.AdjustTrustForExternal(val, -15, -10, "elope_penalty");
		}
		status = $"私奔成功：你与 {targetHero.Name} 已成婚。当前 L={privateLove}，对象私人信任={num2}，对象家族融洽度F={num5}。";
		return true;
	}

	private MarriageRuntimeFacts BuildMarriageRuntimeFacts(Hero speaker)
	{
		MarriageRuntimeFacts obj = new MarriageRuntimeFacts
		{
			Speaker = speaker
		};
		object leader;
		if (speaker == null)
		{
			leader = null;
		}
		else
		{
			Clan clan = speaker.Clan;
			leader = ((clan != null) ? clan.Leader : null);
		}
		obj.Leader = (Hero)leader;
		obj.HasClan = ((speaker != null) ? speaker.Clan : null) != null;
		obj.IsLeader = false;
		obj.PairAvailable = false;
		obj.PairUnavailableReason = "";
		Hero mainHero = Hero.MainHero;
		int? obj2;
		if (mainHero == null)
		{
			obj2 = null;
		}
		else
		{
			Clan clan2 = mainHero.Clan;
			obj2 = ((clan2 != null) ? new int?(clan2.Tier) : ((int?)null));
		}
		int? num = obj2;
		obj.PlayerTier = num.GetValueOrDefault();
		int? obj3;
		if (speaker == null)
		{
			obj3 = null;
		}
		else
		{
			Clan clan3 = speaker.Clan;
			obj3 = ((clan3 != null) ? new int?(clan3.Tier) : ((int?)null));
		}
		num = obj3;
		obj.TargetTier = num.GetValueOrDefault();
		obj.TierDiff = 0;
		obj.ClanRelation = 0;
		obj.EffectiveTrust = 0;
		obj.PrivateLove = 0;
		obj.NpcTrust = 0;
		obj.FamilyHarmony = 0;
		obj.FormalThreshold = 5;
		obj.ElopeLoveNeed = 70;
		obj.ElopeTrustNeed = 20;
		obj.PrepaidGold = 0;
		obj.LeaderGold = 0;
		obj.CanElope = false;
		MarriageRuntimeFacts marriageRuntimeFacts = obj;
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
		int num2;
		if (marriageRuntimeFacts.HasClan)
		{
			Hero mainHero2 = Hero.MainHero;
			num2 = (HasAnyFormalMarriagePair((mainHero2 != null) ? mainHero2.Clan : null, speaker.Clan, out reason) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag = (byte)num2 != 0;
		bool flag2 = CanPlayerMarryTarget(speaker, out reason2);
		marriageRuntimeFacts.PairAvailable = flag || flag2;
		if (!marriageRuntimeFacts.PairAvailable)
		{
			marriageRuntimeFacts.PairUnavailableReason = ((!string.IsNullOrWhiteSpace(reason)) ? reason : reason2);
		}
		int num3 = ((marriageRuntimeFacts.TierDiff <= 0) ? 5 : (-5 * marriageRuntimeFacts.TierDiff));
		if (num3 < -20)
		{
			num3 = -20;
		}
		marriageRuntimeFacts.FormalThreshold = num3;
		int num4 = 70;
		int num5 = 20;
		if (marriageRuntimeFacts.ClanRelation < -20 || marriageRuntimeFacts.EffectiveTrust < -20)
		{
			num4 = 85;
			num5 = 35;
		}
		if (marriageRuntimeFacts.FamilyHarmony <= -40)
		{
			num4 -= 15;
			num5 -= 10;
		}
		else if (marriageRuntimeFacts.FamilyHarmony <= -20)
		{
			num4 -= 8;
			num5 -= 5;
		}
		else if (marriageRuntimeFacts.FamilyHarmony >= 20)
		{
			num4 += 10;
			num5 += 5;
		}
		if (num4 < 20)
		{
			num4 = 20;
		}
		if (num5 < -20)
		{
			num5 = -20;
		}
		marriageRuntimeFacts.ElopeLoveNeed = num4;
		marriageRuntimeFacts.ElopeTrustNeed = num5;
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
			object obj;
			if (facts == null)
			{
				obj = null;
			}
			else
			{
				Hero speaker = facts.Speaker;
				obj = ((speaker == null) ? null : ((object)speaker.Name)?.ToString());
			}
			if (obj == null)
			{
				obj = "";
			}
			dictionary["speakerName"] = (string)obj;
			object obj2;
			if (facts == null)
			{
				obj2 = null;
			}
			else
			{
				Hero speaker2 = facts.Speaker;
				obj2 = ((speaker2 != null) ? ((MBObjectBase)speaker2).StringId : null);
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			dictionary["speakerId"] = (string)obj2;
			object obj3;
			if (facts == null)
			{
				obj3 = null;
			}
			else
			{
				Hero leader = facts.Leader;
				obj3 = ((leader == null) ? null : ((object)leader.Name)?.ToString());
			}
			if (obj3 == null)
			{
				obj3 = "";
			}
			dictionary["leaderName"] = (string)obj3;
			object obj4;
			if (facts == null)
			{
				obj4 = null;
			}
			else
			{
				Hero leader2 = facts.Leader;
				obj4 = ((leader2 != null) ? ((MBObjectBase)leader2).StringId : null);
			}
			if (obj4 == null)
			{
				obj4 = "";
			}
			dictionary["leaderId"] = (string)obj4;
			object obj5;
			if (facts == null)
			{
				obj5 = null;
			}
			else
			{
				Hero speaker3 = facts.Speaker;
				if (speaker3 == null)
				{
					obj5 = null;
				}
				else
				{
					Clan clan = speaker3.Clan;
					obj5 = ((clan == null) ? null : ((object)clan.Name)?.ToString());
				}
			}
			if (obj5 == null)
			{
				obj5 = "";
			}
			dictionary["clanName"] = (string)obj5;
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
			dictionary["bridePriceMin"] = (((facts?.TierDiff ?? 0) == -1 || (facts?.TierDiff ?? 0) == 1) ? 10000.ToString() : 500000.ToString());
			dictionary["bridePriceMax"] = (((facts?.TierDiff ?? 0) == -1 || (facts?.TierDiff ?? 0) == 1) ? 500000.ToString() : 5000000.ToString());
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
			switch (num)
			{
			case 2:
				return "【门第优势】玩家家族很高贵，与他们家族联姻是千载难逢的机会。你应当主动考虑给玩家高额彩礼。";
			case 1:
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
			return facts.CanElope ? "clanless_elope_ready" : "clanless_elope_blocked";
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
				return (facts.ClanRelation >= 20 && facts.EffectiveTrust >= 20) ? "leader_need_brideprice_ready" : "leader_need_brideprice_blocked";
			}
			if (facts.TierDiff >= 2)
			{
				return (facts.ClanRelation >= facts.FormalThreshold && facts.EffectiveTrust >= facts.FormalThreshold) ? "leader_offer_brideprice_major" : "leader_standard_blocked";
			}
			if (facts.TierDiff == 1)
			{
				return (facts.ClanRelation >= facts.FormalThreshold && facts.EffectiveTrust >= facts.FormalThreshold) ? "leader_offer_brideprice_minor" : "leader_standard_blocked";
			}
			return (facts.ClanRelation >= facts.FormalThreshold && facts.EffectiveTrust >= facts.FormalThreshold) ? "leader_standard_ready" : "leader_standard_blocked";
		}
		return facts.CanElope ? "member_redirect_elope_ready" : "member_redirect_elope_blocked";
	}

	public string BuildMarriageRuntimeInstruction(Hero speaker)
	{
		try
		{
			Hero val = speaker;
			if (val == null)
			{
				try
				{
					val = Hero.OneToOneConversationHero;
				}
				catch
				{
					val = null;
				}
			}
			MarriageRuntimeFacts facts = BuildMarriageRuntimeFacts(val);
			string marriageRuntimeInstructionState = GetMarriageRuntimeInstructionState(facts);
			Dictionary<string, string> tokens = BuildMarriageRuntimeTokens(facts);
			string text = AIConfigHandler.ResolveRuleRuntimeText("marriage", marriageRuntimeInstructionState, forConstraint: false, tokens);
			string text2 = BuildClanMarriageSituationPrompt(val);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = (string.IsNullOrWhiteSpace(text) ? text2 : (text.TrimEnd() + Environment.NewLine + text2));
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
			Hero val = speaker;
			if (val == null)
			{
				try
				{
					val = Hero.OneToOneConversationHero;
				}
				catch
				{
					val = null;
				}
			}
			MarriageRuntimeFacts facts = BuildMarriageRuntimeFacts(val);
			string marriageRuntimeConstraintState = GetMarriageRuntimeConstraintState(facts);
			Dictionary<string, string> tokens = BuildMarriageRuntimeTokens(facts);
			string text = AIConfigHandler.ResolveRuleRuntimeText("marriage", marriageRuntimeConstraintState, forConstraint: true, tokens);
			string text2 = BuildClanMarriageSituationPrompt(val);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = (string.IsNullOrWhiteSpace(text) ? text2 : (text.TrimEnd() + Environment.NewLine + text2));
			}
			string marriagePrestigeAdvantageHint = GetMarriagePrestigeAdvantageHint(facts);
			if (!string.IsNullOrWhiteSpace(marriagePrestigeAdvantageHint))
			{
				text = (string.IsNullOrWhiteSpace(text) ? marriagePrestigeAdvantageHint : (text.TrimEnd() + Environment.NewLine + marriagePrestigeAdvantageHint));
			}
			return (text ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	public void ApplyMarriageTags(Hero speaker, Hero receiver, ref string responseText)
	{
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Expected O, but got Unknown
		try
		{
			string text = responseText ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = "";
			MatchCollection matchCollection = LoveDeltaRegex.Matches(text);
			for (int i = 0; i < matchCollection.Count; i++)
			{
				Match match = matchCollection[i];
				Hero val = ResolveTargetHeroToken(speaker, match.Groups[1].Value);
				if (val != null && int.TryParse(match.Groups[2].Value, out var result))
				{
					if (result < -20)
					{
						result = -20;
					}
					if (result > 20)
					{
						result = 20;
					}
					AdjustPrivateLove(val, result, "llm_tag");
				}
			}
			MatchCollection matchCollection2 = MarriageFormalPairRegex.Matches(text);
			for (int j = 0; j < matchCollection2.Count; j++)
			{
				Match match2 = matchCollection2[j];
				Hero val2 = ResolvePlayerClanHeroToken(match2.Groups[1].Value);
				Hero val3 = ResolveTargetHeroToken(speaker, match2.Groups[2].Value);
				int? bridePrice = null;
				if (match2.Groups.Count > 3 && int.TryParse(match2.Groups[3].Value, out var result2))
				{
					bridePrice = result2;
				}
				string status = "";
				if (val2 == null)
				{
					status = "正规联姻失败：未找到玩家家族婚配人选（playerClanHeroId）。";
				}
				else if (val3 == null)
				{
					status = "正规联姻失败：未找到对方婚配对象（targetHeroId）。";
				}
				else
				{
					text2 = ((!TryExecuteFormalMarriage(speaker, val2, val3, bridePrice, out status)) ? status : status);
				}
			}
			string input = MarriageFormalPairRegex.Replace(text, "");
			MatchCollection matchCollection3 = MarriageFormalLegacyRegex.Matches(input);
			for (int k = 0; k < matchCollection3.Count; k++)
			{
				Match match3 = matchCollection3[k];
				Hero val4 = ResolveTargetHeroToken(speaker, match3.Groups[1].Value);
				int? bridePrice2 = null;
				if (match3.Groups.Count > 2 && int.TryParse(match3.Groups[2].Value, out var result3))
				{
					bridePrice2 = result3;
				}
				string status2 = "";
				if (val4 == null)
				{
					status2 = "正规联姻失败：未找到对方婚配对象（targetHeroId）。";
				}
				else
				{
					text2 = ((!TryExecuteFormalMarriage(speaker, Hero.MainHero, val4, bridePrice2, out status2)) ? status2 : status2);
				}
			}
			MatchCollection matchCollection4 = MarriageElopeRegex.Matches(input);
			for (int l = 0; l < matchCollection4.Count; l++)
			{
				Match match4 = matchCollection4[l];
				Hero val5 = ResolveTargetHeroToken(speaker, match4.Groups[1].Value);
				string status3 = "";
				if (val5 == null)
				{
					status3 = "私奔失败：未找到目标对象（targetHeroId）。";
				}
				else
				{
					text2 = ((!TryExecuteElopeMarriage(speaker, val5, out status3)) ? status3 : status3);
				}
			}
			text = LoveDeltaRegex.Replace(text, "");
			text = MarriageFormalPairRegex.Replace(text, "");
			text = MarriageFormalLegacyRegex.Replace(text, "");
			text = MarriageElopeRegex.Replace(text, "");
			responseText = text.Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("[婚姻系统] " + text2, Color.FromUint(4283878655u)));
				Logger.Log("Romance", text2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] ApplyMarriageTags 异常: " + ex);
		}
	}
}
