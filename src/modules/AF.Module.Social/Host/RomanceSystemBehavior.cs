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

	private static readonly string[] LoveLevelTexts = new string[10] { SocialPromptTexts.Get("P047"), SocialPromptTexts.Get("P048"), SocialPromptTexts.Get("P049"), SocialPromptTexts.Get("P050"), SocialPromptTexts.Get("P051"), SocialPromptTexts.Get("P052"), SocialPromptTexts.Get("P053"), SocialPromptTexts.Get("P054"), SocialPromptTexts.Get("P055"), SocialPromptTexts.Get("P056") };

	private static readonly Regex LoveDeltaRegex = new Regex("\\[ACTION:LOVE_DELTA:([^\\]:]+):([+\\-]?\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageFormalPairRegex = new Regex("\\[ACTION:MARRIAGE_FORMAL:([^\\]:]+):([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageClanPairRegex = new Regex("\\[ACTION:MARRIAGE_CLAN:([^\\]:]+):([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageFormalLegacyRegex = new Regex("\\[ACTION:MARRIAGE_FORMAL:([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageElopeRegex = new Regex("\\[ACTION:MARRIAGE_ELOPE:([^\\]:]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex DivorcePairRegex = new Regex("\\[ACTION:DIVORCE:([^\\]:]+):([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private readonly RomanceRelationshipOwner _relationshipOwner = new RomanceRelationshipOwner();
	private Dictionary<string, int> _privateLove { get => _relationshipOwner.PrivateLove; set => _relationshipOwner.PrivateLove = value; }

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


	public RomanceSystemBehavior()
	{
		Instance = this;
	}

	public static void SetMarriagePostprocessContextEnabled(Hero speaker, bool enabled)
	{
		Instance?._relationshipOwner.SetContext(speaker?.StringId, enabled);
	}

	private static bool ConsumeMarriagePostprocessContextEnabled(Hero speaker)
	{
		return Instance?._relationshipOwner.ConsumeContext(speaker?.StringId) == true;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnBeforeHeroesMarried);
	}

	private void OnGameLoadFinished()
	{
		NormalizeMarriedPlayerCompanionFamilySlots("game_load_finished");
	}

	private void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification)
	{
		NormalizeMarriedPlayerCompanionFamilySlots("before_heroes_married", hero1, hero2);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore.IsLoading) _relationshipOwner.ClearContext();
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		Dictionary<string, int> privateLoveForSync = _privateLove;
		dataStore.SyncData("_romancePrivateLove_v1", ref privateLoveForSync);
		_privateLove = privateLoveForSync;
		if (_marriageRecordStorage == null)
		{
			_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		Dictionary<string, string> marriageRecordStorageForSync = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_marriageRecordStorage, "_romanceMarriageRecords_v1", "Romance") : _marriageRecordStorage;
		dataStore.SyncData("_romanceMarriageRecords_v1", ref marriageRecordStorageForSync);
		if (dataStore.IsLoading)
		{
			_marriageRecordStorage = CampaignSaveChunkHelper.RestoreStringDictionary(marriageRecordStorageForSync, "Romance");
		}
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_marriageRecordStorage == null)
		{
			_marriageRecordStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		_relationshipOwner.NormalizeLove();
	}

	private static int ClampLove(int value) => RomanceRelationshipOwner.ClampLove(value);

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

	private bool HasActiveMarriageRecord(Hero left, Hero right)
	{
		try
		{
			MarriageRecord marriageRecord = GetMarriageRecord(left, right);
			return marriageRecord != null && marriageRecord.IsActive;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasActiveAnimusMarriageRecord(Hero left, Hero right)
	{
		try
		{
			return Instance?.HasActiveMarriageRecord(left, right) == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasExistingMarriageBetween(Hero left, Hero right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		try
		{
			if (left.Spouse == right || right.Spouse == left)
			{
				return true;
			}
		}
		catch
		{
		}
		return HasActiveAnimusMarriageRecord(left, right);
	}

	private static bool HasDifferentNativeSpouse(Hero hero, Hero intendedSpouse)
	{
		try
		{
			return hero?.Spouse != null && hero.Spouse != intendedSpouse;
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldUseAnimusForgeMultiMarriage(Hero left, Hero right)
	{
		return HasDifferentNativeSpouse(left, right) || HasDifferentNativeSpouse(right, left);
	}

	// 原版拒绝两位族长成婚，是为了避免按普通婚姻规则把其中一位移出其家族。
	// AnimusForge 的授权婚姻标签允许这类联姻，因此执行时必须保留双方的家族归属。
	private static bool AreBothClanLeaders(Hero left, Hero right)
	{
		try
		{
			return left != null && right != null && left.Clan != null && right.Clan != null && left.Clan.Leader == left && right.Clan.Leader == right;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSameGenderMarriagePair(Hero left, Hero right)
	{
		try
		{
			return left != null && right != null && left.IsFemale == right.IsFemale;
		}
		catch
		{
			return false;
		}
	}

	private static Clan GetClanAfterAnimusMarriage(Hero firstHero, Hero secondHero)
	{
		if (firstHero == null || secondHero == null)
		{
			return firstHero?.Clan ?? secondHero?.Clan;
		}
		try
		{
			if (!IsSameGenderMarriagePair(firstHero, secondHero))
			{
				Clan vanillaClan = Campaign.Current?.Models?.MarriageModel?.GetClanAfterMarriage(firstHero, secondHero);
				if (vanillaClan != null)
				{
					return vanillaClan;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[WARN] GetClanAfterMarriage fallback used: " + ex);
		}
		try
		{
			if (firstHero.IsHumanPlayerCharacter)
			{
				return firstHero.Clan;
			}
			if (secondHero.IsHumanPlayerCharacter)
			{
				return secondHero.Clan;
			}
			if (firstHero.Clan?.Leader == firstHero)
			{
				return firstHero.Clan;
			}
			if (secondHero.Clan?.Leader == secondHero)
			{
				return secondHero.Clan;
			}
		}
		catch
		{
		}
		return firstHero.Clan ?? secondHero.Clan;
	}

	private IEnumerable<MarriageRecord> EnumerateActiveMarriageRecords()
	{
		if (_marriageRecordStorage == null || _marriageRecordStorage.Count <= 0)
		{
			yield break;
		}
		foreach (string value in _marriageRecordStorage.Values.ToList())
		{
			if (TryDeserializeMarriageRecord(value, out var record) && record != null && record.IsActive)
			{
				yield return record;
			}
		}
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
			return SocialPromptTexts.Get("P028");
		}
		return GetClanNameSafe(hero) + SocialPromptTexts.Get("P029") + (hero.Name?.ToString() ?? hero.StringId ?? SocialPromptTexts.Get("P030"));
	}

	private static string GetMarriageGenderLabel(Hero hero)
	{
		if (hero == null)
		{
			return SocialPromptTexts.Get("P030");
		}
		try
		{
			return hero.IsFemale ? SocialPromptTexts.Get("P031") : SocialPromptTexts.Get("P032");
		}
		catch
		{
			return SocialPromptTexts.Get("P030");
		}
	}

	private static string GetHeroDisplayWithClanAndGender(Hero hero)
	{
		return GetHeroDisplayWithClan(hero) + SocialPromptTexts.Get("P033") + GetMarriageGenderLabel(hero) + SocialPromptTexts.Get("P090");
	}

	private static string BuildBridePriceSummary(MarriageRecord record, bool forPrompt = false)
	{
		if (record == null || record.BridePriceAmount <= 0)
		{
			return (forPrompt ? SocialPromptTexts.Get("P085") : SocialSourceTexts.Get("S113"));
		}
		Clan clan = FindClanById(record.PayerClanId);
		Clan clan2 = FindClanById(record.ReceiverClanId);
		string text = clan?.Name?.ToString() ?? (forPrompt ? SocialPromptTexts.Get("P086") : SocialSourceTexts.Get("S114"));
		string text2 = clan2?.Name?.ToString() ?? (forPrompt ? SocialPromptTexts.Get("P086") : SocialSourceTexts.Get("S114"));
		return string.Format((forPrompt ? SocialPromptTexts.Get("P087") : SocialSourceTexts.Get("S115")), text, text2, record.BridePriceAmount);
	}

	private static string GetMarriageRelationLabel(Hero hero, Hero reference)
	{
		if (hero == null || reference == null) return SocialPromptTexts.Get("MarriageRelationUnknown");
		if (hero == reference) return SocialPromptTexts.Get("MarriageRelationSelf");
		if (hero == reference.Father) return SocialPromptTexts.Get("MarriageRelationFather");
		if (hero == reference.Mother) return SocialPromptTexts.Get("MarriageRelationMother");
		if (hero.Father == reference || hero.Mother == reference)
			return SocialPromptTexts.Get(hero.IsFemale ? "MarriageRelationDaughter" : "MarriageRelationSon");
		if (hero.Spouse == reference || reference.Spouse == hero)
			return SocialPromptTexts.Get("MarriageRelationSpouse");
		if ((hero.Father != null && hero.Father == reference.Father) || (hero.Mother != null && hero.Mother == reference.Mother))
		{
			if (hero.Age == reference.Age) return SocialPromptTexts.Get(hero.IsFemale ? "MarriageRelationSister" : "MarriageRelationBrother");
			return SocialPromptTexts.Get(hero.IsFemale
				? (hero.Age > reference.Age ? "MarriageRelationOlderSister" : "MarriageRelationYoungerSister")
				: (hero.Age > reference.Age ? "MarriageRelationOlderBrother" : "MarriageRelationYoungerBrother"));
		}
		return SocialPromptTexts.Get(hero.Clan != null && hero.Clan == reference.Clan ? "MarriageRelationClanMember" : "MarriageRelationUnknown");
	}

	private static string BuildMarriageHeroFactLine(Hero hero, string tokenName, Hero speaker)
	{
		if (hero == null)
		{
			return "";
		}
		string text = "";
		try
		{
			if (hero.Spouse != null)
			{
				text = SocialPromptTexts.Get("P034") + (hero.Spouse.Name?.ToString() ?? hero.Spouse.StringId ?? SocialPromptTexts.Get("P030"));
			}
		}
		catch
		{
		}
		return "- " + GetHeroDisplayWithClan(hero) + string.Format(SocialPromptTexts.Get("P035"), tokenName, hero.StringId, GetMarriageGenderLabel(hero), hero.Age, text)
			+ string.Format(SocialPromptTexts.Get("MarriageCandidateRelations"), GetMarriageRelationLabel(hero, Hero.MainHero), GetMarriageRelationLabel(hero, speaker));
	}

	private static string BuildMarriageIdentityBlock(Hero speaker)
	{
		Hero player = Hero.MainHero;
		Hero leader = speaker?.Clan?.Leader;
		return string.Format(SocialPromptTexts.Get("MarriageIdentity"), player?.Name, player?.StringId,
			speaker?.Name, speaker?.StringId, leader?.Name, leader?.StringId,
			speaker != null && speaker == leader ? SocialPromptTexts.Get("MarriageSpeakerLeader") : SocialPromptTexts.Get("MarriageSpeakerMember"));
	}

	private static string BuildFactBlock(string title, List<string> lines, string emptyText = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(title);
		if (lines == null || lines.Count <= 0)
		{
			stringBuilder.Append(emptyText ?? SocialPromptTexts.Get("P014"));
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

	private IEnumerable<string> EnumerateActiveMarriageSituationLines(Hero speaker)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Clan clan = Hero.MainHero?.Clan;
		Clan clan2 = speaker?.Clan;
		foreach (MarriageRecord record in EnumerateActiveMarriageRecords())
		{
			Hero hero = FindHeroById(record.LeftHeroId);
			Hero hero2 = FindHeroById(record.RightHeroId);
			if (hero == null || hero2 == null)
			{
				continue;
			}
			bool flag = false;
			if (clan != null && clan2 != null)
			{
				flag = (hero.Clan == clan && hero2.Clan == clan2) || (hero2.Clan == clan && hero.Clan == clan2);
			}
			if (!flag && speaker != null && Hero.MainHero != null)
			{
				flag = (hero == Hero.MainHero && hero2 == speaker) || (hero2 == Hero.MainHero && hero == speaker);
			}
			if (!flag)
			{
				continue;
			}
			string text = BuildMarriageRecordKey(hero.StringId, hero2.StringId);
			if (hashSet.Add(text))
			{
				yield return "- " + GetHeroDisplayWithClanAndGender(hero) + SocialPromptTexts.Get("P036") + GetHeroDisplayWithClanAndGender(hero2) + SocialPromptTexts.Get("P037") + BuildBridePriceSummary(record, forPrompt: true) + "。";
			}
		}
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
					yield return "- " + GetHeroDisplayWithClanAndGender(item) + SocialPromptTexts.Get("P036") + GetHeroDisplayWithClanAndGender(spouse) + SocialPromptTexts.Get("P037") + BuildBridePriceSummary(marriageRecord, forPrompt: true) + "。";
				}
			}
		}
		if (speaker != null && Hero.MainHero != null && (speaker.Spouse == Hero.MainHero || Hero.MainHero.Spouse == speaker))
		{
			string text2 = BuildMarriageRecordKey(Hero.MainHero.StringId, speaker.StringId);
			if (hashSet.Add(text2))
			{
				MarriageRecord marriageRecord2 = GetMarriageRecord(Hero.MainHero, speaker);
				yield return "- " + GetHeroDisplayWithClanAndGender(Hero.MainHero) + SocialPromptTexts.Get("P036") + GetHeroDisplayWithClanAndGender(speaker) + SocialPromptTexts.Get("P037") + BuildBridePriceSummary(marriageRecord2, forPrompt: true) + "。";
			}
		}
	}

	private string BuildClanMarriageSituationPrompt(Hero speaker)
	{
		try
		{
			List<string> list = EnumerateActiveMarriageSituationLines(speaker).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<string> list2 = GetMarriageClanMembers(Clan.PlayerClan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "playerClanHeroId", speaker)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			List<string> list3 = ((speaker?.Clan != null) ? GetMarriageClanMembers(speaker.Clan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "targetHeroId", speaker)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>());
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(BuildMarriageIdentityBlock(speaker));
			stringBuilder.AppendLine(SocialPromptTexts.Get("P038"));
			stringBuilder.AppendLine(SocialPromptTexts.Get("P039"));
			if (list2.Count <= 0)
			{
				stringBuilder.AppendLine(SocialPromptTexts.Get("P014"));
			}
			else
			{
				for (int i = 0; i < list2.Count; i++)
				{
					stringBuilder.AppendLine(list2[i]);
				}
			}
			stringBuilder.AppendLine(SocialPromptTexts.Get("P040"));
			stringBuilder.AppendLine(SocialPromptTexts.Get("P041"));
			if (list3.Count <= 0)
			{
				stringBuilder.AppendLine(SocialPromptTexts.Get("P014"));
			}
			else
			{
				for (int j = 0; j < list3.Count; j++)
				{
					stringBuilder.AppendLine(list3[j]);
				}
			}
			stringBuilder.AppendLine(SocialPromptTexts.Get("P042"));
			if (list.Count <= 0)
			{
				stringBuilder.AppendLine(SocialPromptTexts.Get("P043"));
			}
			else
			{
				for (int l = 0; l < list.Count; l++)
				{
					stringBuilder.AppendLine(list[l]);
				}
			}
			stringBuilder.Append(SocialPromptTexts.Get("P044"));
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
			List<string> list = GetMarriageClanMembers(Clan.PlayerClan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "playerClanHeroId", speaker)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			return BuildMarriageIdentityBlock(speaker) + Environment.NewLine + BuildFactBlock(SocialPromptTexts.Get("P038"), list);
		}
		catch
		{
			return SocialPromptTexts.Get("P045");
		}
	}

	private string BuildMarriagePostprocessTargetCandidatesBlock(Hero speaker)
	{
		try
		{
			List<string> list = ((speaker?.Clan != null) ? GetMarriageClanMembers(speaker.Clan).Where(IsFormalMarriageCandidate).Select((Hero x) => BuildMarriageHeroFactLine(x, "targetHeroId", speaker)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>());
			return BuildFactBlock(SocialPromptTexts.Get("P040"), list);
		}
		catch
		{
			return SocialPromptTexts.Get("P046");
		}
	}

	private static int ToLoveLevelIndex(int value) => RomanceRelationshipOwner.ToLoveLevelIndex(value);

	private static string BuildLoveKey(Hero hero)
	{
		string text = (hero?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	public static bool IsPlayerCompanionOrFamily(Hero hero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			Clan playerClan = Clan.PlayerClan ?? mainHero?.Clan;
			if (hero == null || mainHero == null || hero == mainHero)
			{
				return false;
			}
			if (hero.IsPlayerCompanion || (playerClan != null && hero.CompanionOf == playerClan))
			{
				return true;
			}
			if (playerClan != null && hero.Clan == playerClan)
			{
				return true;
			}
			if (hero.Spouse == mainHero || mainHero.Spouse == hero || hero.Father == mainHero || hero.Mother == mainHero || mainHero.Father == hero || mainHero.Mother == hero)
			{
				return true;
			}
			if ((hero.Father != null && hero.Father == mainHero.Father) || (hero.Mother != null && hero.Mother == mainHero.Mother))
			{
				return true;
			}
			if (hero.Children != null && hero.Children.Contains(mainHero))
			{
				return true;
			}
			if (mainHero.Children != null && mainHero.Children.Contains(hero))
			{
				return true;
			}
			// Former spouses (either side of the record).
			if ((mainHero.ExSpouses != null && mainHero.ExSpouses.Contains(hero)) || (hero.ExSpouses != null && hero.ExSpouses.Contains(mainHero)))
			{
				return true;
			}
			// Grandparents / grandchildren. Per-candidate O(children) walk; no global scan.
			if (IsGrandparentOf(hero, mainHero) || IsGrandparentOf(mainHero, hero))
			{
				return true;
			}
			return false;
		}
		catch
		{
			// Fail closed: an unreadable relation is treated as family and excluded.
			return true;
		}
	}

	private static bool IsGrandparentOf(Hero elder, Hero younger)
	{
		Hero father = younger.Father;
		Hero mother = younger.Mother;
		return (father != null && (father.Father == elder || father.Mother == elder))
			|| (mother != null && (mother.Father == elder || mother.Mother == elder));
	}

	public static bool TryGetPrivateLoveAsPlayerRelation(Hero hero, out int relation)
	{
		relation = 0;
		if (!IsPlayerCompanionOrFamily(hero))
		{
			return false;
		}
		try
		{
			relation = Instance?.GetPrivateLove(hero) ?? 0;
		}
		catch
		{
			relation = 0;
		}
		return true;
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
		return _relationshipOwner.GetLove(text);
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
		int privateLove = _relationshipOwner.GetLove(text);
		int num = _relationshipOwner.SetLove(text, value);
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
			InformationManager.DisplayMessage(new InformationMessage(SocialUiLocalization.Format("AF_SOCIAL_UI_PrivateRelationshipDelta", "[Private relationship] {NAME} {DELTA} (current {CURRENT}).", ("NAME", hero.Name), ("DELTA", text2), ("CURRENT", num)), color));
			if (num3 > 0)
			{
				int npcTrust = RewardSystemBehavior.Instance?.GetNpcTrust(hero) ?? num3;
				InformationManager.DisplayMessage(new InformationMessage(SocialUiLocalization.Format("AF_SOCIAL_UI_TrustDelta", "[Trust change] {NAME}'s personal trust in you +{DELTA} (private relationship increase; current {CURRENT}).", ("NAME", hero.Name), ("DELTA", num3), ("CURRENT", npcTrust)), Color.FromUint(4278242559u)));
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
			if (left == Hero.MainHero && TryGetPrivateLoveAsPlayerRelation(right, out var rightRelation))
			{
				return rightRelation;
			}
			if (right == Hero.MainHero && TryGetPrivateLoveAsPlayerRelation(left, out var leftRelation))
			{
				return leftRelation;
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
			if (!hero.IsAlive || hero.IsDead)
			{
				return false;
			}
		}
		catch
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
		return true;
	}

	private static bool CanPlayerMarryTarget(Hero targetHero, out string reason)
	{
		reason = "";
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || targetHero == null)
		{
			reason = SocialSourceTexts.Get("S116");
			return false;
		}
		if (mainHero == targetHero)
		{
			reason = SocialSourceTexts.Get("S117");
			return false;
		}
		if (HasExistingMarriageBetween(mainHero, targetHero))
		{
			reason = SocialSourceTexts.Get("S118");
			return false;
		}
		if (!IsMarriageableHero(mainHero))
		{
			reason = SocialSourceTexts.Get("S119");
			return false;
		}
		if (!IsMarriageableHero(targetHero))
		{
			reason = SocialSourceTexts.Get("S120");
			return false;
		}
		return true;
	}

	// 动作标签已由上游规则授权；执行阶段只保留关系数据完整性约束，不能再用原版资格状态否决该标签。
	private static bool CanExecuteMarriageTagPair(Hero left, Hero right, out string reason)
	{
		reason = "";
		if (left == null || right == null)
		{
			reason = SocialSourceTexts.Get("S116");
			return false;
		}
		if (left == right)
		{
			reason = SocialSourceTexts.Get("S117");
			return false;
		}
		if (!IsMarriageableHero(left))
		{
			reason = SocialSourceTexts.Get("S151");
			return false;
		}
		if (!IsMarriageableHero(right))
		{
			reason = SocialSourceTexts.Get("S152");
			return false;
		}
		if (HasExistingMarriageBetween(left, right))
		{
			reason = SocialSourceTexts.Get("S118");
			return false;
		}
		return true;
	}

	private static bool TryApplyMarriageAction(Hero left, Hero right, out string failReason, bool bypassMarriageEligibilityForAuthorizedTag = false)
	{
		failReason = "";
		using IDisposable notificationScope = MarriageSceneNotificationSafety.BeginScope(left, right);
		string text = DescribeHeroMarriageState(left);
		string text2 = DescribeHeroMarriageState(right);
		try
		{
			if (bypassMarriageEligibilityForAuthorizedTag && !CanExecuteMarriageTagPair(left, right, out var authorizedTagReason))
			{
				failReason = authorizedTagReason;
				return false;
			}
			if (ShouldUseAnimusForgeMultiMarriage(left, right))
			{
				Logger.Log("Romance", "[WARN] MarriageAction switching to AnimusForge multi-marriage path because at least one side already has a native spouse.");
				Logger.Log("Romance", "[WARN] MarriageAction left=" + text);
				Logger.Log("Romance", "[WARN] MarriageAction right=" + text2);
				return TryApplyAnimusForgeMultiMarriageAction(left, right, out failReason, bypassMarriageEligibilityForAuthorizedTag);
			}
			if (bypassMarriageEligibilityForAuthorizedTag)
			{
				Logger.Log("Romance", "[MarriageAction] Authorized marriage tag bypassed vanilla eligibility checks and is using the forced path.");
				if (AreBothClanLeaders(left, right))
				{
					Logger.Log("Romance", "[MarriageAction] Authorized marriage tag accepted a two-clan-leader pair; both clan memberships will be preserved.");
				}
				return TryForceApplyMarriageAction(left, right, out failReason);
			}
			if (IsSameGenderMarriagePair(left, right))
			{
				if (!TryValidateMarriagePairIgnoringRuntimeBlockers(left, right, out var sameGenderReason))
				{
					failReason = sameGenderReason;
					Logger.Log("Romance", "[WARN] Same-gender MarriageAction blocked by AnimusForge precheck: " + sameGenderReason);
					Logger.Log("Romance", "[WARN] Same-gender left=" + text);
					Logger.Log("Romance", "[WARN] Same-gender right=" + text2);
					return false;
				}
				Logger.Log("Romance", "[MarriageAction] Same-gender pair accepted; using AnimusForge forced marriage path.");
				return TryForceApplyMarriageAction(left, right, out failReason);
			}
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
				failReason = SocialSourceTexts.Get("S121");
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
					NormalizeMarriedPlayerCompanionFamilySlots("marriage_action_success", left, right);
					return true;
				}
				failReason = SocialSourceTexts.Get("S122");
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
					NormalizeMarriedPlayerCompanionFamilySlots("marriage_action_success", left, right);
					return true;
				}
				failReason = SocialSourceTexts.Get("S122");
				Logger.Log("Romance", "[WARN] MarriageAction returned without marriage state change.");
				Logger.Log("Romance", "[WARN] MarriageAction left(after)=" + DescribeHeroMarriageState(left));
				Logger.Log("Romance", "[WARN] MarriageAction right(after)=" + DescribeHeroMarriageState(right));
				return false;
			}
			failReason = SocialSourceTexts.Get("S123");
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
				NormalizeMarriedPlayerCompanionFamilySlots("marriage_action_partial_success", left, right);
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
				NormalizeMarriedPlayerCompanionFamilySlots("marriage_action_partial_success", left, right);
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

	private static bool TryApplyAnimusForgeMultiMarriageAction(Hero left, Hero right, out string failReason, bool bypassMarriageEligibilityForAuthorizedTag = false)
	{
		failReason = "";
		using IDisposable notificationScope = MarriageSceneNotificationSafety.BeginScope(left, right);
		try
		{
			if (left == null || right == null)
			{
				failReason = SocialSourceTexts.Get("S116");
				return false;
			}
			if (HasExistingMarriageBetween(left, right))
			{
				failReason = SocialSourceTexts.Get("S118");
				return false;
			}
			if (!bypassMarriageEligibilityForAuthorizedTag && !TryValidateMarriagePairIgnoringRuntimeBlockers(left, right, out var reason, allowExistingSpouses: true))
			{
				failReason = reason;
				return false;
			}
			if (bypassMarriageEligibilityForAuthorizedTag)
			{
				Logger.Log("Romance", "[MarriageAction] Authorized marriage tag bypassed vanilla multi-marriage eligibility checks.");
			}
			var marriageModel = Campaign.Current?.Models?.MarriageModel;
			if (marriageModel == null)
			{
				failReason = SocialSourceTexts.Get("S124");
				return false;
			}
			Hero hero = left;
			Hero hero2 = right;
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, hero2, marriageModel.GetEffectiveRelationIncrease(hero, hero2), false);
			}
			catch (Exception ex)
			{
				Logger.Log("Romance", "[WARN] Multi-marriage relation increase failed: " + ex);
			}
			bool preserveClanMembership = AreBothClanLeaders(hero, hero2);
			Clan clanAfterMarriage = preserveClanMembership ? null : GetClanAfterAnimusMarriage(hero, hero2);
			if (!preserveClanMembership && clanAfterMarriage != null && clanAfterMarriage != hero.Clan)
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
				Logger.Log("Romance", "[WARN] Multi-marriage event chain failed, will continue and backfill notifications: " + ex2);
			}
			if (!preserveClanMembership && clanAfterMarriage != null && hero.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(hero, clanAfterMarriage);
			}
			if (!preserveClanMembership && clanAfterMarriage != null && hero2.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(hero2, clanAfterMarriage);
			}
			if (preserveClanMembership)
			{
				Logger.Log("Romance", "[MarriageAction] Multi-marriage retained both clan leaders in their original clans.");
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
				Logger.Log("Romance", "[WARN] Multi-marriage end courtships failed: " + ex3);
			}
			try
			{
				ChangeRomanticStateAction.Apply(hero, hero2, Romance.RomanceLevelEnum.Marriage);
			}
			catch (Exception ex4)
			{
				Logger.Log("Romance", "[WARN] Multi-marriage romantic state update failed: " + ex4);
			}
			if (flag)
			{
				TryEmitMarriageFallbackNotifications(left, right);
			}
			Logger.Log("Romance", "[MarriageAction] AnimusForge multi-marriage path succeeded without replacing native spouse pointers.");
			return true;
		}
		catch (Exception ex5)
		{
			failReason = ex5.GetType().Name + ": " + ex5.Message;
			Logger.Log("Romance", "[ERROR] Multi-marriage action failed: " + ex5);
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
		reason = SocialSourceTexts.Get("S125");
		return true;
	}

	private static bool TryValidateMarriagePairIgnoringRuntimeBlockers(Hero left, Hero right, out string reason, bool allowExistingSpouses = false)
	{
		reason = "";
		if (left == null || right == null)
		{
			reason = SocialSourceTexts.Get("S116");
			return false;
		}
		var marriageModel = Campaign.Current?.Models?.MarriageModel;
		if (marriageModel == null)
		{
			reason = SocialSourceTexts.Get("S124");
			return false;
		}
		if (!marriageModel.IsClanSuitableForMarriage(left.Clan) || !marriageModel.IsClanSuitableForMarriage(right.Clan))
		{
			reason = SocialSourceTexts.Get("S126");
			return false;
		}
		if (!CanHeroMarryIgnoringRuntimeBlockers(left, out reason, allowExistingSpouses) || !CanHeroMarryIgnoringRuntimeBlockers(right, out reason, allowExistingSpouses))
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
				reason = SocialSourceTexts.Get("S127");
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
				reason = SocialSourceTexts.Get("S128");
				return false;
			}
			Hero courtedHeroInOtherClan2 = Romance.GetCourtedHeroInOtherClan(right, left);
			if (courtedHeroInOtherClan2 != null && courtedHeroInOtherClan2 != left)
			{
				reason = SocialSourceTexts.Get("S129");
				return false;
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("Romance", "[WARN] Courtship conflict check failed: " + ex2);
		}
		return true;
	}

	private static bool CanHeroMarryIgnoringRuntimeBlockers(Hero hero, out string reason, bool allowExistingSpouses = false)
	{
		reason = "";
		if (hero == null)
		{
			reason = SocialSourceTexts.Get("S130");
			return false;
		}
		try
		{
			if (!hero.IsActive)
			{
				reason = string.Format(SocialSourceTexts.Get("S131"), hero.Name);
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (!allowExistingSpouses && hero.Spouse != null)
			{
				reason = string.Format(SocialSourceTexts.Get("S132"), hero.Name);
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
				reason = string.Format(SocialSourceTexts.Get("S133"), hero.Name);
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
				reason = string.Format(SocialSourceTexts.Get("S134"), hero.Name);
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
				reason = string.Format(SocialSourceTexts.Get("S135"), hero.Name);
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (!allowExistingSpouses)
			{
				bool result = true;
				CampaignEventDispatcher.Instance.CanHeroMarry(hero, ref result);
				if (!result)
				{
					reason = string.Format(SocialSourceTexts.Get("S136"), hero.Name);
					return false;
				}
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
		using IDisposable notificationScope = MarriageSceneNotificationSafety.BeginScope(left, right);
		try
		{
			if (left == null || right == null)
			{
				failReason = SocialSourceTexts.Get("S116");
				return false;
			}
			var marriageModel = Campaign.Current?.Models?.MarriageModel;
			if (marriageModel == null)
			{
				failReason = SocialSourceTexts.Get("S124");
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
			bool preserveClanMembership = AreBothClanLeaders(hero, hero2);
			Clan clanAfterMarriage = preserveClanMembership ? null : GetClanAfterAnimusMarriage(hero, hero2);
			if (!preserveClanMembership && clanAfterMarriage != null && clanAfterMarriage != hero.Clan)
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
			if (!preserveClanMembership && clanAfterMarriage != null && hero.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(hero, clanAfterMarriage);
			}
			if (!preserveClanMembership && clanAfterMarriage != null && hero2.Clan != clanAfterMarriage)
			{
				HandleClanChangeAfterMarriageCompat(hero2, clanAfterMarriage);
			}
			if (preserveClanMembership)
			{
				Logger.Log("Romance", "[MarriageAction] Forced marriage retained both clan leaders in their original clans.");
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
				NormalizeMarriedPlayerCompanionFamilySlots("force_marriage_success", left, right);
				return true;
			}
			failReason = SocialSourceTexts.Get("S137");
			return false;
		}
		catch (Exception ex)
		{
			if (left?.Spouse == right && right?.Spouse == left)
			{
				Logger.Log("Romance", "[WARN] Force marriage threw after marriage state changed: " + ex);
				NormalizeMarriedPlayerCompanionFamilySlots("force_marriage_partial_success", left, right);
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
			return SocialSourceTexts.Get("S116");
		}
		List<string> list = new List<string>();
		try
		{
			var marriageModel = Campaign.Current?.Models?.MarriageModel;
			if (marriageModel != null && !marriageModel.IsCoupleSuitableForMarriage(left, right) && (!IsSameGenderMarriagePair(left, right) || !TryValidateMarriagePairIgnoringRuntimeBlockers(left, right, out var _)))
			{
				list.Add(SocialSourceTexts.Get("S138"));
			}
		}
		catch (Exception ex)
		{
			list.Add(SocialSourceTexts.Get("S139") + ex.GetType().Name);
		}
		try
		{
			if (left.Clan == null || right.Clan == null)
			{
				list.Add(SocialSourceTexts.Get("S140"));
			}
		}
		catch
		{
		}
		TryAppendHeroMarriageBlockers(left, SocialSourceTexts.Get("S141"), list);
		TryAppendHeroMarriageBlockers(right, SocialSourceTexts.Get("S142"), list);
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
				reasons.Add(sideLabel + SocialSourceTexts.Get("S143"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S144"));
			}
		}
		catch
		{
		}
		try
		{
			if (!hero.IsLord)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S145"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsMinorFactionHero)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S146"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsNotable)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S147"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.IsTemplate)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S148"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.PartyBelongedTo?.MapEvent != null)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S149"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.PartyBelongedTo?.Army != null)
			{
				reasons.Add(sideLabel + SocialSourceTexts.Get("S150"));
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
			bool flag2 = false;
			if (!flag)
			{
				flag2 = MarriageSceneNotificationSafety.ShowSafeNotification(hero, hero2, SceneNotificationData.RelevantContextType.Any);
			}
			var characterMarriedLogEntry = new TaleWorlds.CampaignSystem.LogEntries.CharacterMarriedLogEntry(left, right);
			TaleWorlds.CampaignSystem.LogEntries.LogEntry.AddLogEntry(characterMarriedLogEntry);
			if (left.Clan == Clan.PlayerClan || right.Clan == Clan.PlayerClan)
			{
				Campaign.Current?.CampaignInformationManager?.NewMapNoticeAdded(new TaleWorlds.CampaignSystem.MapNotificationTypes.MarriageMapNotification(left, right, characterMarriedLogEntry.GetEncyclopediaText(), CampaignTime.Now));
			}
			string information = flag ? SocialUiLocalization.Text("AF_SOCIAL_UI_MarriageFallbackMission", "[Marriage system] The marriage has taken effect; the meeting or mission scene is active, so the marriage record and notice were restored.") : (flag2 ? SocialUiLocalization.Text("AF_SOCIAL_UI_MarriageFallbackNotice", "[Marriage system] The marriage has taken effect; the wedding notice was restored.") : SocialUiLocalization.Text("AF_SOCIAL_UI_MarriageFallbackQueued", "[Marriage system] The marriage has taken effect; the wedding notice has been queued by the marriage event."));
			InformationManager.DisplayMessage(new InformationMessage(information, Color.FromUint(4283878655u)));
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] Emit marriage fallback notifications failed: " + ex);
		}
	}

	private static int NormalizeMarriedPlayerCompanionFamilySlots(string reason, params Hero[] priorityHeroes)
	{
		int num = 0;
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || Hero.MainHero == null)
			{
				return 0;
			}
			List<Hero> list = new List<Hero>();
			Action<Hero> addCandidate = delegate(Hero hero)
			{
				if (hero != null && !list.Contains(hero))
				{
					list.Add(hero);
				}
			};
			if (priorityHeroes != null)
			{
				foreach (Hero priorityHero in priorityHeroes)
				{
					addCandidate(priorityHero);
					addCandidate(priorityHero?.Spouse);
				}
			}
			addCandidate(Hero.MainHero.Spouse);
			try
			{
				foreach (Hero companion in playerClan.Companions)
				{
					addCandidate(companion);
				}
			}
			catch
			{
			}
			try
			{
				foreach (Hero hero in playerClan.Heroes)
				{
					addCandidate(hero);
				}
			}
			catch
			{
			}
			foreach (Hero hero2 in list)
			{
				if (TryMoveMarriedPlayerCompanionToFamily(hero2, reason))
				{
					num++;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[WARN] Normalize married companion family slots failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
		return num;
	}

	private static bool ShouldMoveMarriedPlayerCompanionToFamily(Hero hero, Clan playerClan)
	{
		if (hero == null || playerClan == null || Hero.MainHero == null || hero == Hero.MainHero)
		{
			return false;
		}
		Hero spouse = hero.Spouse;
		if (spouse == null)
		{
			return false;
		}
		bool flag = spouse == Hero.MainHero || Hero.MainHero.Spouse == hero;
		bool flag2 = hero.Clan == playerClan && spouse.Clan == playerClan;
		if (!flag && !flag2)
		{
			return false;
		}
		bool flag3 = hero.CompanionOf == playerClan || hero.IsPlayerCompanion;
		bool flag4 = hero.Clan == playerClan && (!hero.IsLord || playerClan.AliveLords?.Contains(hero) != true);
		return flag3 || flag4;
	}

	private static bool TryMoveMarriedPlayerCompanionToFamily(Hero hero, string reason)
	{
		Clan playerClan = Clan.PlayerClan;
		if (!ShouldMoveMarriedPlayerCompanionToFamily(hero, playerClan))
		{
			return false;
		}
		string text = DescribeHeroMarriageState(hero);
		try
		{
			bool flag = false;
			if (hero.CompanionOf == playerClan)
			{
				hero.CompanionOf = null;
				flag = true;
			}
			if (!hero.IsLord)
			{
				hero.SetNewOccupation(Occupation.Lord);
				flag = true;
			}
			bool flag2 = playerClan.AliveLords?.Contains(hero) == true;
			if (hero.Clan != playerClan || !flag2)
			{
				if (hero.Clan == playerClan && !flag2)
				{
					hero.Clan = null;
				}
				hero.Clan = playerClan;
				flag = true;
			}
			try
			{
				hero.UpdateHomeSettlement();
				hero.Spouse?.UpdateHomeSettlement();
			}
			catch
			{
			}
			if (flag)
			{
				Logger.Log("Romance", "[MarriageCompanionFamilySlot] moved reason=" + (reason ?? "") + " before={" + text + "} after={" + DescribeHeroMarriageState(hero) + "}");
			}
			return flag;
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[WARN] Move married companion to family failed reason=" + (reason ?? "") + " before={" + text + "} error=" + ex);
			return false;
		}
	}

	private static IEnumerable<Hero> GetClanMembersCompat(Clan clan)
	{
		if (clan == null)
		{
			yield break;
		}
		Hero hero = null;
		try
		{
			hero = clan.Leader;
		}
		catch
		{
		}
		// Clan.Heroes/Lords can be a stale or partial cache while campaign data is changing.
		// The clan leader must still be visible to the marriage flow, including a kingdom ruler.
		if (hero != null)
		{
			yield return hero;
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
			if (item is Hero hero2 && hero2 != null && hero2 != hero)
			{
				yield return hero2;
			}
		}
	}

	// Marriage-only enumeration: never change the family-harmony/relation consumers above.
	private static IEnumerable<Hero> GetMarriageClanMembers(Clan clan)
	{
		if (clan == null) yield break;
		HashSet<Hero> seen = new HashSet<Hero>();
		Hero leader = clan.Leader;
		if (leader != null && leader.Clan == clan && seen.Add(leader)) yield return leader;
		if (clan.Heroes != null)
			foreach (Hero hero in clan.Heroes)
				if (hero != null && hero.Clan == clan && seen.Add(hero)) yield return hero;
		if (clan.Companions != null)
			foreach (Hero hero in clan.Companions)
				if (hero != null && hero.Clan == clan && seen.Add(hero)) yield return hero;
	}

	private static bool IsMarriageAuthorityHero(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			Clan clan = hero.Clan;
			return clan != null && (clan.Leader == hero || clan.Kingdom?.Leader == hero);
		}
		catch
		{
			return false;
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

	private static bool IsFormalMarriageCandidate(Hero hero)
	{
		if (!IsMarriageableHero(hero))
		{
			return false;
		}
		try
		{
			int marriageCandidateMaxAgeSetting = GetMarriageCandidateMaxAgeSetting();
			if (!RomanceRelationshipOwner.IsCandidateAge(hero.Age, marriageCandidateMaxAgeSetting, hero.Age > (float)marriageCandidateMaxAgeSetting && IsMarriageAuthorityHero(hero)))
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
			reason = SocialSourceTexts.Get("S116");
			return false;
		}
		if (HasExistingMarriageBetween(left, right))
		{
			reason = SocialSourceTexts.Get("S118");
			return false;
		}
		if (!IsFormalMarriageCandidate(left))
		{
			reason = SocialSourceTexts.Get("S151");
			return false;
		}
		if (!IsFormalMarriageCandidate(right))
		{
			reason = SocialSourceTexts.Get("S152");
			return false;
		}
		try
		{
			if (!IsMarriageAuthorityHero(left) && !IsMarriageAuthorityHero(right) && !RomanceRelationshipOwner.IsAgeGapAllowed(left.Age, right.Age, GetMarriageCandidateMaxAgeGapSetting()))
			{
				reason = SocialSourceTexts.Get("S153");
				return false;
			}
		}
		catch
		{
			reason = SocialSourceTexts.Get("S154");
			return false;
		}
		return true;
	}

	private static bool HasAnyFormalMarriagePair(Clan playerClan, Clan targetClan, out string reason)
	{
		reason = "";
		if (playerClan == null || targetClan == null)
		{
			reason = SocialSourceTexts.Get("S155");
			return false;
		}
		if (playerClan == targetClan)
		{
			reason = SocialSourceTexts.Get("S156");
			return false;
		}
		List<Hero> list = GetMarriageClanMembers(playerClan).Where(IsFormalMarriageCandidate).ToList();
		if (list.Count <= 0)
		{
			reason = SocialSourceTexts.Get("S157");
			return false;
		}
		List<Hero> list2 = GetMarriageClanMembers(targetClan).Where(IsFormalMarriageCandidate).ToList();
		if (list2.Count <= 0)
		{
			reason = SocialSourceTexts.Get("S158");
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
		reason = SocialSourceTexts.Get("S159");
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

	private static Hero ResolveMarriageHeroId(string token)
	{
		string id = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)) return null;
		try { return Hero.Find(id); }
		catch { return null; }
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
		return SocialSourceTexts.Get("S160");
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
		string text3 = string.Format(SocialSourceTexts.Get("S161"), text, playerClanHero.Name);
		string text4 = string.Format(SocialSourceTexts.Get("S161"), text2, targetHero.Name);
		string text5 = BuildBridePriceSummary(GetMarriageRecord(playerClanHero, targetHero));
		if (leader != null)
		{
			MyBehavior.AppendExternalNpcFact(leader, string.Format(SocialSourceTexts.Get("S162"), text3, text4, text5));
		}
		if (targetHero != leader)
		{
			MyBehavior.AppendExternalNpcFact(targetHero, string.Format(SocialSourceTexts.Get("S163"), text3, text5));
		}
		if (playerClanHero == Hero.MainHero)
		{
			MyBehavior.AppendExternalPlayerFact(Hero.MainHero, string.Format(SocialSourceTexts.Get("S163"), text4, text5));
		}
		else
		{
			MyBehavior.AppendExternalNpcFact(playerClanHero, string.Format(SocialSourceTexts.Get("S163"), text4, text5));
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
		MyBehavior.AppendExternalPlayerFact(Hero.MainHero, string.Format(SocialSourceTexts.Get("S164"), text, targetHero.Name));
		MyBehavior.AppendExternalNpcFact(targetHero, SocialSourceTexts.Get("S165"));
		Hero hero = targetHero.Clan?.Leader;
		if (hero != null && hero != targetHero)
		{
			MyBehavior.AppendExternalNpcFact(hero, string.Format(SocialSourceTexts.Get("S166"), text, targetHero.Name));
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
		string text3 = string.Format(SocialSourceTexts.Get("S161"), text, playerClanHero.Name);
		string text4 = string.Format(SocialSourceTexts.Get("S161"), text2, targetHero.Name);
		string text5 = (refundedAmount > 0) ? string.Format(SocialSourceTexts.Get("S167"), refundedAmount) : "";
		MyBehavior.AppendExternalPlayerFact(Hero.MainHero, string.Format(SocialSourceTexts.Get("S168"), text3, text4, text5).Trim());
		MyBehavior.AppendExternalNpcFact(targetHero, string.Format(SocialSourceTexts.Get("S169"), text3, text5).Trim());
		if (playerClanHero != Hero.MainHero)
		{
			MyBehavior.AppendExternalNpcFact(playerClanHero, string.Format(SocialSourceTexts.Get("S169"), text4, text5).Trim());
		}
		Hero hero = targetHero.Clan?.Leader;
		if (hero != null && hero != targetHero)
		{
			MyBehavior.AppendExternalNpcFact(hero, string.Format(SocialSourceTexts.Get("S170"), text, text5).Trim());
		}
	}

	private static bool CanArrangeFormalMarriagePair(Hero playerClanHero, Hero targetHero, out string reason)
	{
		reason = "";
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || playerClanHero == null || targetHero == null)
		{
			reason = SocialSourceTexts.Get("S116");
			return false;
		}
		if (playerClanHero.Clan == null || playerClanHero.Clan != mainHero.Clan)
		{
			reason = SocialSourceTexts.Get("S171");
			return false;
		}
		if (playerClanHero == targetHero)
		{
			reason = SocialSourceTexts.Get("S172");
			return false;
		}
		if (HasExistingMarriageBetween(playerClanHero, targetHero))
		{
			reason = SocialSourceTexts.Get("S118");
			return false;
		}
		if (playerClanHero.Clan != null && targetHero.Clan != null && playerClanHero.Clan == targetHero.Clan)
		{
			reason = SocialSourceTexts.Get("S156");
			return false;
		}
		if (!IsMarriageableHero(playerClanHero))
		{
			reason = SocialSourceTexts.Get("S173");
			return false;
		}
		if (!IsMarriageableHero(targetHero))
		{
			reason = SocialSourceTexts.Get("S174");
			return false;
		}
		if (!IsFormalMarriagePairCompatible(playerClanHero, targetHero, out reason))
		{
			return false;
		}
		return true;
	}

	private static bool CanExecuteFormalMarriageTagPair(Hero speaker, Hero playerClanHero, Hero targetHero, bool clanMarriage, out string reason)
	{
		if (!CanExecuteMarriageTagPair(playerClanHero, targetHero, out reason))
		{
			return false;
		}
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			reason = SocialSourceTexts.Get("S175");
			return false;
		}
		if (clanMarriage && playerClanHero == mainHero)
		{
			reason = SocialPromptTexts.Get("MarriageInvalidClanSubject");
			return false;
		}
		if (playerClanHero.Clan == null || playerClanHero.Clan != mainHero.Clan)
		{
			reason = SocialSourceTexts.Get("S171");
			return false;
		}
		if (targetHero.Clan == null)
		{
			reason = SocialSourceTexts.Get("S176");
			return false;
		}
		if (playerClanHero.Clan == targetHero.Clan)
		{
			reason = SocialSourceTexts.Get("S156");
			return false;
		}
		if (speaker == null || targetHero.Clan != speaker.Clan || speaker.Clan?.Leader != speaker)
		{
			reason = SocialPromptTexts.Get("MarriageInvalidApprover");
			return false;
		}
		return true;
	}

	private static bool CanExecuteElopeMarriageTagPair(Hero speaker, Hero targetHero, out string reason)
	{
		if (speaker == null || targetHero != speaker)
		{
			reason = SocialPromptTexts.Get("MarriageInvalidElopeSubject");
			return false;
		}
		return CanExecuteMarriageTagPair(Hero.MainHero, targetHero, out reason);
	}

	private bool TryExecuteFormalMarriage(Hero speaker, Hero playerClanHero, Hero targetHero, int? bridePrice, out string status, bool clanMarriage = false)
	{
		status = "";
		if (!CanExecuteFormalMarriageTagPair(speaker, playerClanHero, targetHero, clanMarriage, out var reason))
		{
			status = SocialSourceTexts.Get("S177") + reason;
			return false;
		}
		if (targetHero.Clan == null)
		{
			status = SocialSourceTexts.Get("S178");
			return false;
		}
		Hero leader = targetHero.Clan.Leader;
		if (leader == null)
		{
			status = SocialSourceTexts.Get("S179");
			return false;
		}
		int num = Hero.MainHero?.Clan?.Tier ?? 0;
		int num2 = targetHero.Clan?.Tier ?? 0;
		int num3 = num - num2;
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
		if (!TryApplyMarriageAction(playerClanHero, targetHero, out var failReason, bypassMarriageEligibilityForAuthorizedTag: true))
		{
			Logger.Log("Romance", "[WARN] MarriageAction failed after LLM tag, fallback to force apply: " + failReason);
			if (ShouldUseAnimusForgeMultiMarriage(playerClanHero, targetHero))
			{
				status = SocialSourceTexts.Get("S180") + failReason;
				return false;
			}
			if (!TryForceApplyMarriageAction(playerClanHero, targetHero, out failReason))
			{
				status = SocialSourceTexts.Get("S181") + failReason;
				return false;
			}
		}
		int num7 = 0;
		if (flag && num4 > 0)
		{
			num7 = RewardSystemBehavior.Instance?.ConsumePlayerPrepaidGoldForExternal(leader, Math.Min(num4, Math.Max(0, num5))) ?? 0;
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
		status = string.Format(SocialSourceTexts.Get("S182"), playerClanHero.Name, targetHero.Name, num3, clanRelationWithPlayer, effectiveTrust);
		if (num7 > 0)
		{
			status += string.Format(SocialSourceTexts.Get("S183"), num7);
		}
		if (num8 > 0)
		{
			status += string.Format(SocialSourceTexts.Get("S184"), num8);
			MyBehavior.AppendExternalNpcFact(leader, string.Format(SocialSourceTexts.Get("S185"), num8));
			MyBehavior.AppendExternalPlayerFact(Hero.MainHero, string.Format(SocialSourceTexts.Get("S187"), leader?.Name?.ToString() ?? SocialSourceTexts.Get("S186"), num8));
		}
		return true;
	}

	private bool TryExecuteElopeMarriage(Hero speaker, Hero targetHero, out string status)
	{
		status = "";
		if (!CanExecuteElopeMarriageTagPair(speaker, targetHero, out var reason))
		{
			status = SocialSourceTexts.Get("S188") + reason;
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
		if (!TryApplyMarriageAction(Hero.MainHero, targetHero, out var failReason, bypassMarriageEligibilityForAuthorizedTag: true))
		{
			Logger.Log("Romance", "[WARN] Elope MarriageAction failed after LLM tag, fallback to force apply: " + failReason);
			if (ShouldUseAnimusForgeMultiMarriage(Hero.MainHero, targetHero))
			{
				status = SocialSourceTexts.Get("S189") + failReason;
				return false;
			}
			if (!TryForceApplyMarriageAction(Hero.MainHero, targetHero, out failReason))
			{
				status = SocialSourceTexts.Get("S190") + failReason;
				return false;
			}
		}
		AdjustPrivateLove(targetHero, 12, "elope_success");
		RecordElopeMarriageFacts(targetHero);
		if (leader != null && leader != targetHero)
		{
			try
			{
				if (TryGetPrivateLoveAsPlayerRelation(leader, out var _))
				{
					Instance?.AdjustPrivateLove(leader, -25, "elope_leader_relation_delta");
				}
				else
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, leader, -25);
				}
			}
			catch
			{
			}
			RewardSystemBehavior.Instance?.AdjustTrustForExternal(leader, -15, -10, "elope_penalty");
		}
		status = string.Format(SocialSourceTexts.Get("S191"), targetHero.Name, privateLove, npcTrust, num3);
		return true;
	}

	private bool TryExecuteDivorce(Hero speaker, Hero playerClanHero, Hero targetHero, int? refundAmount, out string status)
	{
		status = "";
		if (playerClanHero == null || targetHero == null)
		{
			status = SocialSourceTexts.Get("S192");
			return false;
		}
		MarriageRecord marriageRecord = GetMarriageRecord(playerClanHero, targetHero);
		if (marriageRecord == null || !marriageRecord.IsActive)
		{
			status = SocialSourceTexts.Get("S193");
			return false;
		}
		if (!HasActiveAnimusMarriageRecord(playerClanHero, targetHero) && playerClanHero.Spouse != targetHero && targetHero.Spouse != playerClanHero)
		{
			status = SocialSourceTexts.Get("S194");
			return false;
		}
		int num = (refundAmount.HasValue && refundAmount.Value > 0) ? refundAmount.Value : 0;
		if (num > 0 && marriageRecord.BridePriceAmount > 0)
		{
			Clan clan = FindClanById(marriageRecord.PayerClanId);
			Clan clan2 = FindClanById(marriageRecord.ReceiverClanId);
			Hero hero = clan2?.Leader;
			Hero hero2 = Hero.MainHero;
			num = Math.Min(num, Math.Max(0, marriageRecord.BridePriceAmount));
			num = Math.Min(num, Math.Max(0, hero?.Gold ?? 0));
			if (hero != null && hero2 != null && num > 0 && clan != Clan.PlayerClan && clan2 == Clan.PlayerClan)
			{
				GiveGoldAction.ApplyBetweenCharacters(hero, hero2, num);
			}
			else
			{
				num = 0;
			}
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
			status = SocialSourceTexts.Get("S195") + ex.Message;
			return false;
		}
		MarriageRecord marriageRecord2 = marriageRecord;
		marriageRecord2.IsActive = false;
		SaveMarriageRecord(marriageRecord2);
		AdjustPrivateLove(targetHero, -12, "divorce");
		RecordDivorceFacts(playerClanHero, targetHero, num);
		status = string.Format(SocialSourceTexts.Get("S196"), playerClanHero.Name, targetHero.Name);
		if (num > 0)
		{
			status += string.Format(SocialSourceTexts.Get("S197"), num);
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
			marriageRuntimeFacts.PairUnavailableReason = SocialSourceTexts.Get("S198");
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
				return SocialPromptTexts.Get("P077");
			}
			if (num == -2)
			{
				return SocialPromptTexts.Get("P078");
			}
			if (num == -1)
			{
				return SocialPromptTexts.Get("P079");
			}
			if (num >= 3)
			{
				return SocialPromptTexts.Get("P080");
			}
			if (num == 2)
			{
				return SocialPromptTexts.Get("P081");
			}
			if (num == 1)
			{
				return SocialPromptTexts.Get("P082");
			}
		}
		catch
		{
		}
		return "";
	}

	private static string GetMarriageRuntimeInstructionState(MarriageRuntimeFacts facts)
	{
		return RomanceRelationshipOwner.GetMarriageRuntimeInstructionState(Hero.MainHero != null, facts?.Speaker != null,
			facts?.PairAvailable == true, facts?.HasClan == true, facts?.IsLeader == true, facts?.CanElope == true,
			facts?.TierDiff ?? 0, facts?.ClanRelation ?? 0, facts?.EffectiveTrust ?? 0, facts?.FormalThreshold ?? 0);
	}

	private static string GetMarriageRuntimeConstraintState(MarriageRuntimeFacts facts)
	{
		return RomanceRelationshipOwner.GetMarriageRuntimeConstraintState(Hero.MainHero != null, facts?.Speaker != null,
			facts?.PairAvailable == true, facts?.HasClan == true, facts?.IsLeader == true, facts?.CanElope == true,
			facts?.TierDiff ?? 0, facts?.ClanRelation ?? 0, facts?.EffectiveTrust ?? 0, facts?.FormalThreshold ?? 0);
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

	private bool HasEligibleMarriageDivorcePair(Hero speaker)
	{
		try
		{
			if (speaker == null || _marriageRecordStorage == null || _marriageRecordStorage.Count <= 0)
			{
				return false;
			}
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
				Hero playerClanHero = (hero.Clan == Clan.PlayerClan) ? hero : ((hero2.Clan == Clan.PlayerClan) ? hero2 : null);
				Hero targetHero = (playerClanHero == hero) ? hero2 : ((playerClanHero == hero2) ? hero : null);
				if (CanAcceptMarriageDivorcePostprocessTag(speaker, playerClanHero, targetHero))
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

	private static bool CanAcceptMarriageFormalPostprocessTag(Hero speaker, Hero playerClanHero, Hero targetHero, string runtimeState)
	{
		if (speaker == null || playerClanHero == null || targetHero == null)
		{
			return false;
		}
		if (!IsMarriageFormalPostprocessReadyState(runtimeState))
		{
			return false;
		}
		if (targetHero.Clan == null || targetHero.Clan != speaker.Clan || targetHero.Clan?.Leader != speaker)
		{
			return false;
		}
		return CanArrangeFormalMarriagePair(playerClanHero, targetHero, out var _);
	}

	private static bool CanAcceptMarriageElopePostprocessTag(Hero speaker, Hero targetHero, string runtimeState)
	{
		if (speaker == null || targetHero == null || targetHero != speaker)
		{
			return false;
		}
		if (!IsMarriageElopePostprocessReadyState(runtimeState))
		{
			return false;
		}
		return CanPlayerMarryTarget(targetHero, out var _);
	}

	private bool CanAcceptMarriageDivorcePostprocessTag(Hero speaker, Hero playerClanHero, Hero targetHero)
	{
		if (!IsMarriageDivorcePostprocessEligible(speaker, playerClanHero, targetHero))
		{
			return false;
		}
		MarriageRecord marriageRecord = GetMarriageRecord(playerClanHero, targetHero);
		return marriageRecord != null && marriageRecord.IsActive;
	}

	private static string StripMarriageActionTags(string text)
	{
		string text2 = text ?? "";
		text2 = LoveDeltaRegex.Replace(text2, "");
		text2 = MarriageFormalPairRegex.Replace(text2, "");
		text2 = MarriageClanPairRegex.Replace(text2, "");
		text2 = MarriageFormalLegacyRegex.Replace(text2, "");
		text2 = MarriageElopeRegex.Replace(text2, "");
		text2 = DivorcePairRegex.Replace(text2, "");
		return text2.Trim();
	}

	private static bool ContainsMarriageActionTags(string text)
	{
		string text2 = text ?? "";
		return LoveDeltaRegex.IsMatch(text2) || MarriageClanPairRegex.IsMatch(text2) || MarriageFormalPairRegex.IsMatch(text2) || MarriageFormalLegacyRegex.IsMatch(text2) || MarriageElopeRegex.IsMatch(text2) || DivorcePairRegex.IsMatch(text2);
	}

	private List<PostprocessRuleEntry> BuildRuntimeMarriagePostprocessRules(Hero speaker)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		try
		{
			List<PostprocessRuleEntry> guardrailRulePostprocessRules = AIConfigHandler.GetGuardrailRulePostprocessRules("marriage") ?? new List<PostprocessRuleEntry>();
			if (guardrailRulePostprocessRules.Count <= 0)
			{
				return list;
			}
			MarriageRuntimeFacts marriageRuntimeFacts = BuildMarriageRuntimeFacts(speaker);
			string marriageRuntimeConstraintState = GetMarriageRuntimeConstraintState(marriageRuntimeFacts);
			bool isLeaderSpeaker = IsMarriagePostprocessClanLeaderSpeaker(speaker);
			bool allowFormal = IsMarriageFormalPostprocessEligibleSpeaker(speaker, out var formalBlockedReason);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in guardrailRulePostprocessRules)
			{
				string text = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				bool isFormal = text.StartsWith("[ACTION:MARRIAGE_FORMAL:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[ACTION:MARRIAGE_CLAN:", StringComparison.OrdinalIgnoreCase);
				bool isElope = text.StartsWith("[ACTION:MARRIAGE_ELOPE:", StringComparison.OrdinalIgnoreCase);
				bool isDivorce = text.StartsWith("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase);
				if (isFormal && !allowFormal)
				{
					continue;
				}
				if (!isFormal && !isLeaderSpeaker && !isElope && !isDivorce)
				{
					continue;
				}
				if (hashSet.Add(text))
				{
					string description = guardrailRulePostprocessRule.Description;
					if (isElope && speaker != null && !string.IsNullOrWhiteSpace(speaker.StringId))
					{
						description = ((description ?? "").TrimEnd() + SocialPromptTexts.Get("P083") + speaker.StringId).Trim();
					}
					if (isDivorce && speaker != null && !string.IsNullOrWhiteSpace(speaker.StringId))
					{
						description = ((description ?? "").TrimEnd() + SocialPromptTexts.Get("P084") + speaker.StringId).Trim();
					}
					list.Add(new PostprocessRuleEntry
					{
						Tag = text,
						Description = description
					});
				}
			}
			Logger.Log("Romance", "[MarriagePostprocessRules] runtimeFiltered=True leaderSpeaker=" + isLeaderSpeaker
				+ " formalAllowed=" + allowFormal
				+ " formalBlockedReason=" + (formalBlockedReason ?? "")
				+ " state=" + marriageRuntimeConstraintState
				+ " rules=" + ((list.Count == 0) ? SocialSourceTexts.Get("S207") : string.Join(",", list.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] BuildRuntimeMarriagePostprocessRules failed: " + ex.Message);
		}
		return list;
	}

	private static bool IsMarriagePostprocessClanLeaderSpeaker(Hero speaker)
	{
		try
		{
			return speaker != null && speaker.Clan?.Leader == speaker;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMarriageFormalPostprocessEligibleSpeaker(Hero speaker, out string blockedReason)
	{
		blockedReason = "";
		try
		{
			if (!IsMarriagePostprocessClanLeaderSpeaker(speaker))
			{
				blockedReason = "speaker_not_clan_leader";
				return false;
			}
			if (IsPlayerCompanionOrFamily(speaker))
			{
				blockedReason = "player_companion_or_family";
				return false;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			if (playerClan == null || speaker.Clan == null)
			{
				blockedReason = "missing_clan";
				return false;
			}
			if (speaker.Clan == playerClan)
			{
				blockedReason = "same_player_clan";
				return false;
			}
			if (!HasAnyFormalMarriagePair(playerClan, speaker.Clan, out var reason))
			{
				blockedReason = string.IsNullOrWhiteSpace(reason) ? "no_formal_pair" : reason;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			blockedReason = "exception:" + ex.Message;
			return false;
		}
	}

	public bool ShouldInjectMarriagePostprocessForExternal(Hero speaker)
	{
		return BuildRuntimeMarriagePostprocessRules(speaker).Count > 0;
	}

	public List<PostprocessRuleEntry> BuildRuntimeMarriagePostprocessRulesForExternal(Hero speaker)
	{
		return BuildRuntimeMarriagePostprocessRules(speaker);
	}

	public string BuildMarriagePostprocessPlayerCandidatesBlockForExternal(Hero speaker)
	{
		return BuildMarriagePostprocessPlayerCandidatesBlock(speaker);
	}

	public string BuildMarriagePostprocessTargetCandidatesBlockForExternal(Hero speaker)
	{
		return BuildMarriagePostprocessTargetCandidatesBlock(speaker);
	}

	// Captured on the game thread; normalization after HTTP must not read live Heroes.
	internal sealed class MarriagePostprocessContext
	{
		internal string PlayerHeroId = "";
		internal string SpeakerHeroId = "";
		internal bool FormalAllowed;
		internal HashSet<string> PlayerHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		internal HashSet<string> TargetHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		internal string PlayerCandidates = "";
		internal string TargetCandidates = "";
	}

	internal MarriagePostprocessContext CaptureMarriagePostprocessContextForExternal(Hero speaker)
	{
		MarriagePostprocessContext context = new MarriagePostprocessContext();
		Hero player = Hero.MainHero;
		Clan playerClan = Clan.PlayerClan ?? player?.Clan;
		context.PlayerHeroId = player?.StringId ?? "";
		context.SpeakerHeroId = IsMarriageableHero(speaker) ? (speaker.StringId ?? "") : "";
		context.FormalAllowed = speaker != null && speaker.Clan != null && speaker.Clan != playerClan && speaker.Clan.Leader == speaker;
		List<string> playerLines = new List<string>();
		List<string> targetLines = new List<string>();
		foreach (Hero hero in GetMarriageClanMembers(playerClan))
			if (IsFormalMarriageCandidate(hero) && !string.IsNullOrWhiteSpace(hero.StringId) && context.PlayerHeroIds.Add(hero.StringId))
				playerLines.Add(BuildMarriageHeroFactLine(hero, "playerClanHeroId", speaker));
		foreach (Hero hero in GetMarriageClanMembers(speaker?.Clan))
			if (IsFormalMarriageCandidate(hero) && !string.IsNullOrWhiteSpace(hero.StringId) && context.TargetHeroIds.Add(hero.StringId))
				targetLines.Add(BuildMarriageHeroFactLine(hero, "targetHeroId", speaker));
		context.PlayerCandidates = BuildMarriageIdentityBlock(speaker) + Environment.NewLine + BuildFactBlock(SocialPromptTexts.Get("P038"), playerLines);
		context.TargetCandidates = BuildFactBlock(SocialPromptTexts.Get("P040"), targetLines);
		return context;
	}

	private string NormalizeMarriagePostprocessTags(string raw, List<PostprocessRuleEntry> rules, MarriagePostprocessContext context)
	{
		List<string> ruleTags = (rules ?? new List<PostprocessRuleEntry>()).Select(x => (x?.Tag ?? "").Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
		bool allowFormal = ruleTags.Any(x => x.StartsWith("[ACTION:MARRIAGE_FORMAL:", StringComparison.OrdinalIgnoreCase));
		bool allowClan = ruleTags.Any(x => x.StartsWith("[ACTION:MARRIAGE_CLAN:", StringComparison.OrdinalIgnoreCase));
		bool allowElope = ruleTags.Any(x => x.StartsWith("[ACTION:MARRIAGE_ELOPE:", StringComparison.OrdinalIgnoreCase));
		bool allowDivorce = ruleTags.Any(x => x.StartsWith("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase));
		List<string> tags = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in MarriageFormalPairRegex.Matches(raw ?? "").Cast<Match>().Concat(MarriageClanPairRegex.Matches(raw ?? "").Cast<Match>()))
		{
			bool clan = match.Value.StartsWith("[ACTION:MARRIAGE_CLAN:", StringComparison.OrdinalIgnoreCase);
			string left = match.Groups[1].Value.Trim();
			string right = match.Groups[2].Value.Trim();
			if ((clan ? allowClan : allowFormal) && context?.FormalAllowed == true
				&& context.PlayerHeroIds.Contains(left) && context.TargetHeroIds.Contains(right)
				&& !string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
				&& (!clan || !string.Equals(left, context.PlayerHeroId, StringComparison.OrdinalIgnoreCase))
				&& seen.Add(match.Value)) tags.Add(match.Value);
		}
		foreach (Match match in MarriageElopeRegex.Matches(raw ?? ""))
			if (allowElope && !string.IsNullOrWhiteSpace(context?.SpeakerHeroId)
				&& string.Equals(match.Groups[1].Value.Trim(), context.SpeakerHeroId, StringComparison.OrdinalIgnoreCase)
				&& seen.Add(match.Value)) tags.Add(match.Value);
		foreach (Match match in DivorcePairRegex.Matches(raw ?? ""))
			if (allowDivorce && seen.Add(match.Value)) tags.Add(match.Value);
		return string.Join("\n", tags);
	}

	internal string NormalizeMarriagePostprocessTagsWithContextForExternal(string raw, List<PostprocessRuleEntry> rules, MarriagePostprocessContext context)
	{
		return NormalizeMarriagePostprocessTags(raw, rules, context);
	}

	public string NormalizeMarriagePostprocessTagsForExternal(string raw, List<PostprocessRuleEntry> rules, Hero speaker = null)
	{
		return NormalizeMarriagePostprocessTags(raw, rules, CaptureMarriagePostprocessContextForExternal(speaker));
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
		MarriagePostprocessContext marriageContext = CaptureMarriagePostprocessContextForExternal(speaker);
		bool injectClanFacts = IsMarriagePostprocessClanLeaderSpeaker(speaker);
		string text4 = injectClanFacts ? marriageContext.PlayerCandidates : null;
		string text5 = injectClanFacts ? marriageContext.TargetCandidates : null;
		string text7 = AIConfigHandler.BuildActionPostprocessSystemPrompt(text2, text3, speaker?.Name?.ToString() ?? "NPC", null, null, null, text4, text5);
		string text8 = AIConfigHandler.BuildActionPostprocessUserPrompt(actionPostprocessUserPromptTemplate, text2, speaker?.Name?.ToString() ?? "NPC", SocialPromptTexts.Get("P014"), text, null, null, null, text4, text5);
		if (!AIConfigHandler.TryCallAuxiliaryActionPostprocess(text7, text8, 5000, 0f, out var content, out var error))
		{
			Logger.Log("Romance", "[MarriagePostprocess] Request failed: " + error);
			return text.Trim();
		}
		string text9 = NormalizeMarriagePostprocessTags(content, list, marriageContext);
		if (string.IsNullOrWhiteSpace(text9))
		{
			return text.Trim();
		}
		string text10 = (text + "\n" + text9).Trim();
		Logger.Log("Romance", "[MarriagePostprocess] RAW=\n" + content + "\nFINAL=\n" + text10 + "\n");
		return text10;
	}

	public void ApplyMarriageTags(Hero speaker, Hero receiver, ref string responseText, bool runPostprocessIfMissing = true)
	{
		try
		{
			string text = responseText ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (runPostprocessIfMissing && !ContainsMarriageActionTags(text))
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
			foreach (Match match2 in MarriageFormalPairRegex.Matches(text).Cast<Match>().Concat(MarriageClanPairRegex.Matches(text).Cast<Match>()))
			{
				bool clanMarriage = match2.Value.StartsWith("[ACTION:MARRIAGE_CLAN:", StringComparison.OrdinalIgnoreCase);
				bool executed = false;
				Hero hero2 = ResolveMarriageHeroId(match2.Groups[1].Value);
				Hero hero3 = ResolveMarriageHeroId(match2.Groups[2].Value);
				int? bridePrice = null;
				if (match2.Groups.Count > 3 && int.TryParse(match2.Groups[3].Value, out var result2))
				{
					bridePrice = result2;
				}
				string status = "";
				if (hero2 == null)
				{
					value = status = SocialSourceTexts.Get("S209");
				}
				else if (hero3 == null)
				{
					value = status = SocialSourceTexts.Get("S210");
				}
				else
				{
					executed = TryExecuteFormalMarriage(speaker, hero2, hero3, bridePrice, out status, clanMarriage);
					value = status;
				}
				Logger.Log("Romance", "[MarriageTagExecution] family=" + (clanMarriage ? "CLAN" : "FORMAL")
					+ " approver=" + (speaker?.StringId ?? "") + " left=" + (hero2?.StringId ?? "")
					+ " right=" + (hero3?.StringId ?? "") + " executed=" + executed);
			}
			string text2 = MarriageClanPairRegex.Replace(MarriageFormalPairRegex.Replace(text, ""), "");
			if (MarriageFormalLegacyRegex.IsMatch(text2))
				Logger.Log("Romance", "[MarriageTagRejected] reason=legacy_missing_initiator");

			MatchCollection matchCollection4 = MarriageElopeRegex.Matches(text2);
			for (int l = 0; l < matchCollection4.Count; l++)
			{
				Match match4 = matchCollection4[l];
				Hero hero5 = ResolveMarriageHeroId(match4.Groups[1].Value);
				string status3 = "";
				if (hero5 == null)
				{
					status3 = SocialSourceTexts.Get("S211");
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
					status4 = SocialSourceTexts.Get("S212");
				}
				else if (hero7 == null)
				{
					status4 = SocialSourceTexts.Get("S213");
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
			text = MarriageClanPairRegex.Replace(text, "");
			text = MarriageFormalLegacyRegex.Replace(text, "");
			text = MarriageElopeRegex.Replace(text, "");
			text = DivorcePairRegex.Replace(text, "");
			responseText = text.Trim();
			if (!string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage(SocialUiLocalization.Format("AF_SOCIAL_UI_MarriageStatus", "[Marriage system] {STATUS}", ("STATUS", SocialUiLocalization.MarriageStatusForDisplay(value))), Color.FromUint(4283878655u)));
				Logger.Log("Romance", value);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] ApplyMarriageTags exception: " + ex);
		}
	}
}
