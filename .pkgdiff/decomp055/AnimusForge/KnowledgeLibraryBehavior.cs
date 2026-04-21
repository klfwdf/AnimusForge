using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class KnowledgeLibraryBehavior : CampaignBehaviorBase
{
	private struct LoreContextCacheItem
	{
		public long Version;

		public long Ticks;

		public string Value;
	}

	private class VectorDoc
	{
		public LoreRule Rule;

		public Dictionary<string, int> Tf;

		public bool IsEvidence;
	}

	private class VectorRuleEntry
	{
		public LoreRule Rule;

		public string Seed;

		public Dictionary<string, float> Weights;

		public float Norm;

		public bool IsEvidence;
	}

	private class OnnxRuleEntry
	{
		public LoreRule Rule;

		public string Seed;

		public float[] Vector;

		public bool IsEvidence;
	}

	private class RuleScore
	{
		public LoreRule Rule;

		public float RawScore;

		public float EvidenceScore;

		public float RerankScore;
	}

	private class CandidateRules
	{
		public string MatchMode = "none";

		public List<LoreRule> OrderedRules = new List<LoreRule>();

		public int InjectLimit = 2;

		public int RecallPerIntent;

		public int RerankPerIntent;

		public int IntentCount = 1;
	}

	private struct RuleAggregate
	{
		public LoreRule Rule;

		public float Score;

		public int BestRank;

		public int HitCount;
	}

	private sealed class WeightedKnowledgeInput
	{
		public string Text;

		public float Weight = 1f;
	}

	public class RuleIndexItem
	{
		public string Id;

		public string Label;
	}

	public class KnowledgeFile
	{
		public int Version = 1;

		public List<LoreRule> Rules = new List<LoreRule>();
	}

	public class LoreRule
	{
		public string Id;

		public List<string> Keywords = new List<string>();

		public List<string> RagShortTexts = new List<string>();

		public List<string> SemanticPrototypes = new List<string>();

		public List<LoreVariant> Variants = new List<LoreVariant>();

		public List<LoreTextMapping> TextMappings = new List<LoreTextMapping>();
	}

	public class LoreVariant
	{
		public int Priority;

		public LoreWhen When;

		public string Content;
	}

	public class LoreWhen
	{
		public List<string> HeroIds;

		public List<string> Cultures;

		public List<string> KingdomIds;

		public List<string> SettlementIds;

		public List<string> Roles;

		public List<string> IdentityIds;

		public bool? IsFemale;

		public bool? IsClanLeader;

		public Dictionary<string, int> SkillMin;
	}

	public class LoreTextMapping
	{
		public string SourceText;

		public string Kind;

		public string TargetId;

		public int? AgeMin;

		public int? AgeMax;

		public string EmptyValueText;

		public string TrueText;

		public string FalseText;
	}

	private sealed class TextMappingKindOption
	{
		public string CategoryKey;

		public string Kind;

		public string Label;
	}

	private sealed class RoleDetailOption
	{
		public string EncodedId;

		public string Label;
	}

	public const int RagShortTextMaxLength = 100;

	public const int RagShortTextGeneratedMaxLength = 50;

	private const int RagShortTextGenerationMaxTokens = 5000;

	private const int RagShortTextAutoGenerateCount = 3;

	private const string StorageKey = "_knowledge_rules_v1_json";

	private const string StorageChunkCountKey = "_knowledge_rules_v1_json_chunk_count";

	private const string StorageChunkKeyPrefix = "_knowledge_rules_v1_json_chunk_";

	private const int StorageChunkMaxBytes = 240;

	private const int LegacyInlineStorageMaxBytes = 240;

	private const int MaxStorageChunkCount = 262144;

	private const string PlayerPersonaRuleId = "rule_animus_player_persona";

	private const string TextMappingKindKingdomName = "kingdom_name";

	private const string TextMappingKindKingdomLeaderName = "kingdom_leader_name";

	private const string TextMappingKindKingdomRulingClanName = "kingdom_ruling_clan_name";

	private const string TextMappingKindKingdomCultureName = "kingdom_culture_name";

	private const string TextMappingKindKingdomInitialHomeSettlementName = "kingdom_initial_home_settlement_name";

	private const string TextMappingKindKingdomAllClans = "kingdom_all_clans";

	private const string TextMappingKindKingdomAllLords = "kingdom_all_lords";

	private const string TextMappingKindKingdomAllTowns = "kingdom_all_towns";

	private const string TextMappingKindKingdomAllCastles = "kingdom_all_castles";

	private const string TextMappingKindKingdomAllVillages = "kingdom_all_villages";

	private const string TextMappingKindKingdomAllSettlements = "kingdom_all_settlements";

	private const string TextMappingKindKingdomActivePolicies = "kingdom_active_policies";

	private const string TextMappingKindKingdomAlliedKingdoms = "kingdom_allied_kingdoms";

	private const string TextMappingKindKingdomWarFactions = "kingdom_war_factions";

	private const string TextMappingKindSettlementName = "settlement_name";

	private const string TextMappingKindSettlementOwnerClanName = "settlement_owner_clan_name";

	private const string TextMappingKindSettlementOwnerLeaderName = "settlement_owner_leader_name";

	private const string TextMappingKindSettlementOwnerKingdomName = "settlement_owner_kingdom_name";

	private const string TextMappingKindSettlementOwnerKingdomLeaderName = "settlement_owner_kingdom_leader_name";

	private const string TextMappingKindSettlementCultureName = "settlement_culture_name";

	private const string TextMappingKindSettlementNotables = "settlement_notables";

	private const string TextMappingKindSettlementParties = "settlement_parties";

	private const string TextMappingKindSettlementBoundVillages = "settlement_bound_villages";

	private const string TextMappingKindClanName = "clan_name";

	private const string TextMappingKindClanLeaderName = "clan_leader_name";

	private const string TextMappingKindClanKingdomName = "clan_kingdom_name";

	private const string TextMappingKindClanKingdomLeaderName = "clan_kingdom_leader_name";

	private const string TextMappingKindHeroName = "hero_name";

	private const string TextMappingKindClanAllTowns = "clan_all_towns";

	private const string TextMappingKindClanAllVillages = "clan_all_villages";

	private const string TextMappingKindClanAllSettlements = "clan_all_settlements";

	private const string TextMappingKindClanMembers = "clan_members";

	private const string TextMappingKindClanMaleMembers = "clan_male_members";

	private const string TextMappingKindClanFemaleMembers = "clan_female_members";

	private const string TextMappingKindClanAgeRangeMembers = "clan_age_range_members";

	private const string TextMappingKindHeroClanName = "hero_clan_name";

	private const string TextMappingKindHeroKingdomName = "hero_kingdom_name";

	private const string TextMappingKindHeroKingdomLeaderName = "hero_kingdom_leader_name";

	private const string TextMappingKindHeroSpouseName = "hero_spouse_name";

	private const string TextMappingKindHeroFatherName = "hero_father_name";

	private const string TextMappingKindHeroMotherName = "hero_mother_name";

	private const string TextMappingKindHeroCurrentSettlementName = "hero_current_settlement_name";

	private const string TextMappingKindCurrentNpcName = "current_npc_name";

	private const string TextMappingKindCurrentNpcClanName = "current_npc_clan_name";

	private const string TextMappingKindCurrentNpcClanKingdomName = "current_npc_clan_kingdom_name";

	private const string TextMappingKindCurrentNpcClanKingdomLeaderName = "current_npc_clan_kingdom_leader_name";

	private const string TextMappingKindCurrentNpcClanAllTowns = "current_npc_clan_all_towns";

	private const string TextMappingKindCurrentNpcClanAllVillages = "current_npc_clan_all_villages";

	private const string TextMappingKindCurrentNpcClanAllSettlements = "current_npc_clan_all_settlements";

	private const string TextMappingKindCurrentNpcClanMembers = "current_npc_clan_members";

	private const string TextMappingKindCurrentNpcClanMaleMembers = "current_npc_clan_male_members";

	private const string TextMappingKindCurrentNpcClanFemaleMembers = "current_npc_clan_female_members";

	private const string TextMappingKindCurrentNpcClanAgeRangeMembers = "current_npc_clan_age_range_members";

	private const string TextMappingKindCurrentNpcKingdomName = "current_npc_kingdom_name";

	private const string TextMappingKindCurrentNpcKingdomLeaderName = "current_npc_kingdom_leader_name";

	private const string TextMappingKindCurrentNpcKingdomRulingClanName = "current_npc_kingdom_ruling_clan_name";

	private const string TextMappingKindCurrentNpcKingdomCultureName = "current_npc_kingdom_culture_name";

	private const string TextMappingKindCurrentNpcKingdomInitialHomeSettlementName = "current_npc_kingdom_initial_home_settlement_name";

	private const string TextMappingKindCurrentNpcKingdomAllClans = "current_npc_kingdom_all_clans";

	private const string TextMappingKindCurrentNpcKingdomAllLords = "current_npc_kingdom_all_lords";

	private const string TextMappingKindCurrentNpcKingdomAllTowns = "current_npc_kingdom_all_towns";

	private const string TextMappingKindCurrentNpcKingdomAllCastles = "current_npc_kingdom_all_castles";

	private const string TextMappingKindCurrentNpcKingdomAllVillages = "current_npc_kingdom_all_villages";

	private const string TextMappingKindCurrentNpcKingdomAllSettlements = "current_npc_kingdom_all_settlements";

	private const string TextMappingKindCurrentNpcKingdomActivePolicies = "current_npc_kingdom_active_policies";

	private const string TextMappingKindCurrentNpcKingdomAlliedKingdoms = "current_npc_kingdom_allied_kingdoms";

	private const string TextMappingKindCurrentNpcKingdomWarFactions = "current_npc_kingdom_war_factions";

	private const string TextMappingKindCurrentNpcSpouseName = "current_npc_spouse_name";

	private const string TextMappingKindCurrentNpcFatherName = "current_npc_father_name";

	private const string TextMappingKindCurrentNpcMotherName = "current_npc_mother_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementName = "current_npc_current_settlement_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementOwnerClanName = "current_npc_current_settlement_owner_clan_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementOwnerLeaderName = "current_npc_current_settlement_owner_leader_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomName = "current_npc_current_settlement_owner_kingdom_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomLeaderName = "current_npc_current_settlement_owner_kingdom_leader_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementCultureName = "current_npc_current_settlement_culture_name";

	private const string TextMappingKindCurrentNpcCurrentSettlementNotables = "current_npc_current_settlement_notables";

	private const string TextMappingKindCurrentNpcCurrentSettlementParties = "current_npc_current_settlement_parties";

	private const string TextMappingKindCurrentNpcCurrentSettlementBoundVillages = "current_npc_current_settlement_bound_villages";

	private const string TextMappingKindPlayerName = "player_name";

	private const string TextMappingKindPlayerClanName = "player_clan_name";

	private const string TextMappingKindPlayerClanKingdomName = "player_clan_kingdom_name";

	private const string TextMappingKindPlayerClanKingdomLeaderName = "player_clan_kingdom_leader_name";

	private const string TextMappingKindPlayerClanAllTowns = "player_clan_all_towns";

	private const string TextMappingKindPlayerClanAllVillages = "player_clan_all_villages";

	private const string TextMappingKindPlayerClanAllSettlements = "player_clan_all_settlements";

	private const string TextMappingKindPlayerClanMembers = "player_clan_members";

	private const string TextMappingKindPlayerClanMaleMembers = "player_clan_male_members";

	private const string TextMappingKindPlayerClanFemaleMembers = "player_clan_female_members";

	private const string TextMappingKindPlayerClanAgeRangeMembers = "player_clan_age_range_members";

	private const string TextMappingKindPlayerKingdomName = "player_kingdom_name";

	private const string TextMappingKindPlayerKingdomLeaderName = "player_kingdom_leader_name";

	private const string TextMappingKindPlayerSpouseName = "player_spouse_name";

	private const string TextMappingKindPlayerCurrentSettlementName = "player_current_settlement_name";

	private const string TextMappingKindBoundKingdomName = "bound_kingdom_name";

	private const string TextMappingKindBoundKingdomLeaderName = "bound_kingdom_leader_name";

	private const string TextMappingKindBoundKingdomRulingClanName = "bound_kingdom_ruling_clan_name";

	private const string TextMappingKindBoundKingdomCultureName = "bound_kingdom_culture_name";

	private const string TextMappingKindBoundKingdomInitialHomeSettlementName = "bound_kingdom_initial_home_settlement_name";

	private const string TextMappingKindBoundKingdomAllClans = "bound_kingdom_all_clans";

	private const string TextMappingKindBoundKingdomAllLords = "bound_kingdom_all_lords";

	private const string TextMappingKindBoundKingdomAllTowns = "bound_kingdom_all_towns";

	private const string TextMappingKindBoundKingdomAllCastles = "bound_kingdom_all_castles";

	private const string TextMappingKindBoundKingdomAllVillages = "bound_kingdom_all_villages";

	private const string TextMappingKindBoundKingdomAllSettlements = "bound_kingdom_all_settlements";

	private const string TextMappingKindBoundKingdomActivePolicies = "bound_kingdom_active_policies";

	private const string TextMappingKindBoundKingdomAlliedKingdoms = "bound_kingdom_allied_kingdoms";

	private const string TextMappingKindBoundKingdomWarFactions = "bound_kingdom_war_factions";

	private const string TextMappingKindBoundSettlementName = "bound_settlement_name";

	private const string TextMappingKindBoundSettlementOwnerClanName = "bound_settlement_owner_clan_name";

	private const string TextMappingKindBoundSettlementOwnerClanKingdomName = "bound_settlement_owner_clan_kingdom_name";

	private const string TextMappingKindBoundSettlementOwnerClanKingdomLeaderName = "bound_settlement_owner_clan_kingdom_leader_name";

	private const string TextMappingKindBoundSettlementOwnerClanAllTowns = "bound_settlement_owner_clan_all_towns";

	private const string TextMappingKindBoundSettlementOwnerClanAllVillages = "bound_settlement_owner_clan_all_villages";

	private const string TextMappingKindBoundSettlementOwnerClanAllSettlements = "bound_settlement_owner_clan_all_settlements";

	private const string TextMappingKindBoundSettlementOwnerClanMembers = "bound_settlement_owner_clan_members";

	private const string TextMappingKindBoundSettlementOwnerClanMaleMembers = "bound_settlement_owner_clan_male_members";

	private const string TextMappingKindBoundSettlementOwnerClanFemaleMembers = "bound_settlement_owner_clan_female_members";

	private const string TextMappingKindBoundSettlementOwnerClanAgeRangeMembers = "bound_settlement_owner_clan_age_range_members";

	private const string TextMappingKindBoundSettlementOwnerLeaderName = "bound_settlement_owner_leader_name";

	private const string TextMappingKindBoundSettlementOwnerKingdomName = "bound_settlement_owner_kingdom_name";

	private const string TextMappingKindBoundSettlementOwnerKingdomLeaderName = "bound_settlement_owner_kingdom_leader_name";

	private const string TextMappingKindBoundSettlementCultureName = "bound_settlement_culture_name";

	private const string TextMappingKindBoundSettlementNotables = "bound_settlement_notables";

	private const string TextMappingKindBoundSettlementParties = "bound_settlement_parties";

	private const string TextMappingKindBoundSettlementBoundVillages = "bound_settlement_bound_villages";

	private const string TextMappingKindBoundHeroName = "bound_hero_name";

	private const string TextMappingKindBoundHeroClanName = "bound_hero_clan_name";

	private const string TextMappingKindBoundHeroClanKingdomName = "bound_hero_clan_kingdom_name";

	private const string TextMappingKindBoundHeroClanKingdomLeaderName = "bound_hero_clan_kingdom_leader_name";

	private const string TextMappingKindBoundHeroClanAllTowns = "bound_hero_clan_all_towns";

	private const string TextMappingKindBoundHeroClanAllVillages = "bound_hero_clan_all_villages";

	private const string TextMappingKindBoundHeroClanAllSettlements = "bound_hero_clan_all_settlements";

	private const string TextMappingKindBoundHeroClanMembers = "bound_hero_clan_members";

	private const string TextMappingKindBoundHeroClanMaleMembers = "bound_hero_clan_male_members";

	private const string TextMappingKindBoundHeroClanFemaleMembers = "bound_hero_clan_female_members";

	private const string TextMappingKindBoundHeroClanAgeRangeMembers = "bound_hero_clan_age_range_members";

	private const string TextMappingKindBoundHeroKingdomName = "bound_hero_kingdom_name";

	private const string TextMappingKindBoundHeroKingdomLeaderName = "bound_hero_kingdom_leader_name";

	private const string TextMappingKindBoundHeroSpouseName = "bound_hero_spouse_name";

	private const string TextMappingKindBoundHeroFatherName = "bound_hero_father_name";

	private const string TextMappingKindBoundHeroMotherName = "bound_hero_mother_name";

	private const string TextMappingKindBoundHeroCurrentSettlementName = "bound_hero_current_settlement_name";

	private const string TextMappingTargetCurrentNpc = "__current_npc__";

	private const string TextMappingTargetPlayer = "__player__";

	private const string TextMappingTargetBoundKingdom = "__bound_kingdom__";

	private const string TextMappingTargetBoundSettlement = "__bound_settlement__";

	private const string TextMappingTargetBoundHero = "__bound_hero__";

	private string _storageJson = "";

	private KnowledgeFile _file = new KnowledgeFile();

	private string _editingRuleId;

	private static readonly object _loreLogLock = new object();

	private static Dictionary<string, long> _loreLogLastTicks = new Dictionary<string, long>();

	private static readonly object _skillCacheLock = new object();

	private static Dictionary<string, SkillObject> _skillByIdCache;

	private static long _ruleDataVersion = 1L;

	private static readonly object _vectorIndexLock = new object();

	private static List<VectorRuleEntry> _vectorRuleEntries;

	private static Dictionary<string, float> _vectorIdf;

	private static long _vectorIndexVersion = -1L;

	private static readonly object _onnxIndexLock = new object();

	private static List<OnnxRuleEntry> _onnxRuleEntries;

	private static long _onnxIndexVersion = -1L;

	private static int _indexWarmupState;

	private static long _indexWarmupVersion = -1L;

	private static readonly object _loreContextCacheLock = new object();

	private static Dictionary<string, LoreContextCacheItem> _loreContextCache = new Dictionary<string, LoreContextCacheItem>();

	private const int LoreContextCacheMax = 256;

	private const int RuleListPageSize = 60;

	private long _ruleIndexCacheVersion = -1L;

	private List<RuleIndexItem> _ruleIndexCache;

	public static KnowledgeLibraryBehavior Instance { get; private set; }

	private static string TrimPreview(string s, int maxChars)
	{
		s = (s ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		if (s.Length <= maxChars)
		{
			return s;
		}
		return s.Substring(0, maxChars).Trim();
	}

	private static string Hash8(string s)
	{
		uint num = 2166136261u;
		string text = s ?? "";
		for (int i = 0; i < text.Length; i++)
		{
			num ^= text[i];
			num *= 16777619;
		}
		return num.ToString("x8");
	}

	private static void TouchRuleData()
	{
		_ruleDataVersion++;
		if (_ruleDataVersion <= 0)
		{
			_ruleDataVersion = 1L;
		}
		try
		{
			lock (_loreContextCacheLock)
			{
				_loreContextCache.Clear();
			}
		}
		catch
		{
		}
		try
		{
			lock (_vectorIndexLock)
			{
				_vectorRuleEntries = null;
				_vectorIdf = null;
				_vectorIndexVersion = -1L;
			}
		}
		catch
		{
		}
		try
		{
			lock (_onnxIndexLock)
			{
				_onnxRuleEntries = null;
				_onnxIndexVersion = -1L;
			}
		}
		catch
		{
		}
		Interlocked.Exchange(ref _indexWarmupState, 0);
		Interlocked.Exchange(ref _indexWarmupVersion, -1L);
	}

	private static bool SafeSyncData<T>(IDataStore dataStore, string key, ref T data)
	{
		try
		{
			return dataStore != null && dataStore.SyncData<T>(key, ref data);
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("KnowledgeSave", "[WARN] SyncData failed for key " + key + ": " + ex.Message);
			}
			catch
			{
			}
			return false;
		}
	}

	private static int GetUtf8ByteCount(string value)
	{
		try
		{
			return Encoding.UTF8.GetByteCount(value ?? "");
		}
		catch
		{
			return 0;
		}
	}

	private static List<string> SplitUtf8Chunks(string value, int maxBytesPerChunk)
	{
		List<string> list = new List<string>();
		string text = value ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		int num = Math.Max(32, maxBytesPerChunk);
		StringBuilder stringBuilder = new StringBuilder();
		int num2 = 0;
		for (int i = 0; i < text.Length; i++)
		{
			int num3 = 1;
			if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
			{
				num3 = 2;
			}
			string value2 = text.Substring(i, num3);
			int utf8ByteCount = GetUtf8ByteCount(value2);
			if (stringBuilder.Length > 0 && num2 + utf8ByteCount > num)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				num2 = 0;
			}
			stringBuilder.Append(value2);
			num2 += utf8ByteCount;
			if (num3 == 2)
			{
				i++;
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	private static void SaveStorageJson(IDataStore dataStore, string json)
	{
		string text = json ?? "";
		List<string> list = SplitUtf8Chunks(text, 240);
		int data = list.Count;
		SafeSyncData(dataStore, "_knowledge_rules_v1_json_chunk_count", ref data);
		for (int i = 0; i < list.Count; i++)
		{
			string data2 = list[i] ?? "";
			SafeSyncData(dataStore, "_knowledge_rules_v1_json_chunk_" + i, ref data2);
		}
		string data3 = ((GetUtf8ByteCount(text) <= 240) ? text : "");
		SafeSyncData(dataStore, "_knowledge_rules_v1_json", ref data3);
	}

	private static string LoadStorageJson(IDataStore dataStore)
	{
		string text = TryLoadChunkedStorageJson(dataStore);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string data = "";
		return SafeSyncData(dataStore, "_knowledge_rules_v1_json", ref data) ? (data ?? "") : "";
	}

	private static string TryLoadChunkedStorageJson(IDataStore dataStore)
	{
		int data = 0;
		if (!SafeSyncData(dataStore, "_knowledge_rules_v1_json_chunk_count", ref data) || data <= 0 || data > 262144)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < data; i++)
		{
			string data2 = "";
			if (!SafeSyncData(dataStore, "_knowledge_rules_v1_json_chunk_" + i, ref data2))
			{
				return "";
			}
			stringBuilder.Append(data2 ?? "");
		}
		return stringBuilder.ToString();
	}

	private static bool TryDeserializeKnowledgeFile(string json, out KnowledgeFile knowledgeFile)
	{
		knowledgeFile = null;
		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}
			knowledgeFile = JsonConvert.DeserializeObject<KnowledgeFile>(json);
			return knowledgeFile != null;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("KnowledgeSave", "[WARN] Deserialize knowledge storage failed: " + ex.Message);
			}
			catch
			{
			}
			knowledgeFile = null;
			return false;
		}
	}

	private static bool TryGetLoreContextCache(string key, long version, out string value)
	{
		value = "";
		try
		{
			lock (_loreContextCacheLock)
			{
				if (_loreContextCache != null && _loreContextCache.TryGetValue(key, out var value2) && value2.Version == version)
				{
					value2.Ticks = DateTime.UtcNow.Ticks;
					_loreContextCache[key] = value2;
					value = value2.Value ?? "";
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void PutLoreContextCache(string key, long version, string value)
	{
		try
		{
			lock (_loreContextCacheLock)
			{
				if (_loreContextCache == null)
				{
					_loreContextCache = new Dictionary<string, LoreContextCacheItem>();
				}
				if (_loreContextCache.Count >= 256)
				{
					_loreContextCache.Clear();
				}
				_loreContextCache[key] = new LoreContextCacheItem
				{
					Version = version,
					Ticks = DateTime.UtcNow.Ticks,
					Value = (value ?? "")
				};
			}
		}
		catch
		{
		}
	}

	private static bool IsAsciiWordChar(char ch)
	{
		return ch < '\u0080' && (char.IsLetterOrDigit(ch) || ch == '_');
	}

	private static bool IsCjkChar(char ch)
	{
		return (ch >= '一' && ch <= '鿿') || (ch >= '㐀' && ch <= '䶿');
	}

	private static void AppendCjkTokens(StringBuilder seq, List<string> tokens)
	{
		if (seq == null || seq.Length <= 0 || tokens == null)
		{
			return;
		}
		if (seq.Length == 1)
		{
			tokens.Add("c1:" + seq[0]);
			return;
		}
		for (int i = 0; i < seq.Length - 1; i++)
		{
			tokens.Add("c2:" + seq.ToString(i, 2));
		}
		for (int j = 0; j < seq.Length; j++)
		{
			tokens.Add("c1:" + seq[j]);
		}
	}

	private static List<string> ExtractVectorTokens(string text)
	{
		List<string> list = new List<string>();
		try
		{
			string text2 = (text ?? "").ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text2))
			{
				return list;
			}
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			for (int i = 0; i < text2.Length; i++)
			{
				char c = text2[i];
				if (IsAsciiWordChar(c))
				{
					stringBuilder.Append(c);
					if (stringBuilder2.Length > 0)
					{
						AppendCjkTokens(stringBuilder2, list);
						stringBuilder2.Clear();
					}
					continue;
				}
				if (stringBuilder.Length > 0)
				{
					if (stringBuilder.Length >= 2)
					{
						list.Add("w:" + stringBuilder.ToString());
					}
					else
					{
						list.Add("w1:" + stringBuilder[0]);
					}
					stringBuilder.Clear();
				}
				if (IsCjkChar(c))
				{
					stringBuilder2.Append(c);
					continue;
				}
				if (stringBuilder2.Length > 0)
				{
					AppendCjkTokens(stringBuilder2, list);
					stringBuilder2.Clear();
				}
				if (char.IsLetterOrDigit(c))
				{
					list.Add("u:" + c);
				}
			}
			if (stringBuilder.Length > 0)
			{
				if (stringBuilder.Length >= 2)
				{
					list.Add("w:" + stringBuilder.ToString());
				}
				else
				{
					list.Add("w1:" + stringBuilder[0]);
				}
			}
			if (stringBuilder2.Length > 0)
			{
				AppendCjkTokens(stringBuilder2, list);
			}
		}
		catch
		{
		}
		return list;
	}

	private static Dictionary<string, int> CountTokens(List<string> tokens)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		try
		{
			if (tokens == null)
			{
				return dictionary;
			}
			for (int i = 0; i < tokens.Count; i++)
			{
				string text = (tokens[i] ?? "").Trim();
				if (!string.IsNullOrEmpty(text))
				{
					if (dictionary.TryGetValue(text, out var value))
					{
						dictionary[text] = value + 1;
					}
					else
					{
						dictionary[text] = 1;
					}
				}
			}
		}
		catch
		{
		}
		return dictionary;
	}

	private static Dictionary<string, float> BuildVectorWeights(Dictionary<string, int> tf, Dictionary<string, float> idf, out float norm)
	{
		norm = 0f;
		Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.Ordinal);
		try
		{
			if (tf == null || tf.Count <= 0)
			{
				return dictionary;
			}
			double num = 0.0;
			foreach (KeyValuePair<string, int> item in tf)
			{
				string text = item.Key ?? "";
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				int value = item.Value;
				if (value > 0)
				{
					float num2 = 1f;
					if (idf != null && idf.TryGetValue(text, out var value2))
					{
						num2 = value2;
					}
					float num3 = 1f + (float)Math.Log(1.0 + (double)value);
					float num4 = (dictionary[text] = num3 * num2);
					float num6 = num4;
					num += (double)num6 * (double)num6;
				}
			}
			norm = ((num > 0.0) ? ((float)Math.Sqrt(num)) : 0f);
		}
		catch
		{
			norm = 0f;
		}
		return dictionary;
	}

	private static float DotProduct(Dictionary<string, float> a, Dictionary<string, float> b)
	{
		try
		{
			if (a == null || b == null || a.Count <= 0 || b.Count <= 0)
			{
				return 0f;
			}
			if (a.Count > b.Count)
			{
				Dictionary<string, float> dictionary = a;
				a = b;
				b = dictionary;
			}
			double num = 0.0;
			foreach (KeyValuePair<string, float> item in a)
			{
				if (b.TryGetValue(item.Key, out var value))
				{
					num += (double)item.Value * (double)value;
				}
			}
			return (float)num;
		}
		catch
		{
			return 0f;
		}
	}

	private static string BuildRuleSearchText(LoreRule rule)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(rule.Id))
			{
				stringBuilder.Append(rule.Id).Append(' ');
			}
			if (rule.Keywords != null)
			{
				for (int i = 0; i < rule.Keywords.Count; i++)
				{
					string value = (rule.Keywords[i] ?? "").Trim();
					if (!string.IsNullOrEmpty(value))
					{
						stringBuilder.Append(value).Append(' ');
					}
				}
			}
			bool flag = false;
			if (rule.RagShortTexts != null && rule.RagShortTexts.Count > 0)
			{
				for (int j = 0; j < rule.RagShortTexts.Count; j++)
				{
					string value2 = (rule.RagShortTexts[j] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrEmpty(value2))
					{
						stringBuilder.Append(value2).Append(' ');
						flag = true;
					}
				}
			}
			if (!flag && rule.Variants != null)
			{
				for (int k = 0; k < rule.Variants.Count; k++)
				{
					LoreVariant loreVariant = rule.Variants[k];
					if (loreVariant != null)
					{
						string value3 = (loreVariant.Content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(value3))
						{
							stringBuilder.Append(value3).Append(' ');
						}
					}
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static void AddSemanticSeed(List<string> list, HashSet<string> seen, string raw, int maxLen = 260)
	{
		try
		{
			string text = (raw ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (text.Length > maxLen)
				{
					text = text.Substring(0, maxLen);
				}
				if (seen.Add(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
	}

	private static List<string> GetRuleTopicSeeds(LoreRule rule)
	{
		List<string> list = new List<string>();
		try
		{
			if (rule == null)
			{
				return list;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text = (rule.RagShortTexts[i] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						AddSemanticSeed(list, seen, text, 220);
					}
				}
			}
			if (list.Count > 0)
			{
				return list;
			}
			if (rule.Variants != null)
			{
				for (int j = 0; j < rule.Variants.Count; j++)
				{
					string text2 = (rule.Variants[j]?.Content ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						AddSemanticSeed(list, seen, text2, 320);
					}
				}
			}
			if (list.Count <= 0)
			{
				string raw = BuildRuleSearchText(rule);
				AddSemanticSeed(list, seen, raw, 320);
			}
			if (list.Count <= 0)
			{
				AddSemanticSeed(list, seen, rule?.Id, 200);
			}
		}
		catch
		{
		}
		return list;
	}

	private static List<string> GetRuleEvidenceSeeds(LoreRule rule)
	{
		List<string> list = new List<string>();
		try
		{
			if (rule == null)
			{
				return list;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			AddSemanticSeed(list, seen, rule.Id, 120);
			if (rule.Keywords != null)
			{
				for (int i = 0; i < rule.Keywords.Count; i++)
				{
					string text = (rule.Keywords[i] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						AddSemanticSeed(list, seen, text, 120);
						AddSemanticSeed(list, seen, "关于" + text, 160);
					}
				}
			}
			if (rule.RagShortTexts != null)
			{
				for (int j = 0; j < rule.RagShortTexts.Count; j++)
				{
					string text2 = (rule.RagShortTexts[j] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						AddSemanticSeed(list, seen, text2, 180);
					}
				}
			}
			if (list.Count <= 0 && rule.Variants != null)
			{
				for (int k = 0; k < rule.Variants.Count; k++)
				{
					string text3 = (rule.Variants[k]?.Content ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						AddSemanticSeed(list, seen, text3, 120);
						if (list.Count >= 2)
						{
							break;
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private void EnsureVectorIndex()
	{
		try
		{
			if (_vectorRuleEntries != null && _vectorIndexVersion == _ruleDataVersion)
			{
				return;
			}
			lock (_vectorIndexLock)
			{
				if (_vectorRuleEntries != null && _vectorIndexVersion == _ruleDataVersion)
				{
					return;
				}
				List<VectorDoc> docs = new List<VectorDoc>();
				Dictionary<string, int> df = new Dictionary<string, int>(StringComparer.Ordinal);
				if (_file != null && _file.Rules != null)
				{
					foreach (LoreRule rule in _file.Rules)
					{
						LoreRule r = rule;
						if (r != null)
						{
							List<string> ruleTopicSeeds = GetRuleTopicSeeds(r);
							List<string> ruleEvidenceSeeds = GetRuleEvidenceSeeds(r);
							addSeeds(ruleTopicSeeds, isEvidence: false);
							addSeeds(ruleEvidenceSeeds, isEvidence: true);
						}
						void addSeeds(IEnumerable<string> seeds, bool isEvidence)
						{
							if (seeds == null)
							{
								return;
							}
							foreach (string seed2 in seeds)
							{
								string text = (seed2 ?? "").Trim();
								if (!string.IsNullOrWhiteSpace(text))
								{
									List<string> tokens = ExtractVectorTokens(text);
									Dictionary<string, int> dictionary3 = CountTokens(tokens);
									if (dictionary3.Count > 0)
									{
										docs.Add(new VectorDoc
										{
											Rule = r,
											Tf = dictionary3,
											IsEvidence = isEvidence
										});
										HashSet<string> hashSet = new HashSet<string>(dictionary3.Keys, StringComparer.Ordinal);
										foreach (string item in hashSet)
										{
											if (df.TryGetValue(item, out var value2))
											{
												df[item] = value2 + 1;
											}
											else
											{
												df[item] = 1;
											}
										}
									}
								}
							}
						}
					}
				}
				int count = docs.Count;
				Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.Ordinal);
				if (count > 0)
				{
					foreach (KeyValuePair<string, int> item2 in df)
					{
						float value = 1f + (float)Math.Log(((double)count + 1.0) / ((double)item2.Value + 1.0));
						dictionary[item2.Key] = value;
					}
				}
				List<VectorRuleEntry> list = new List<VectorRuleEntry>();
				for (int i = 0; i < docs.Count; i++)
				{
					VectorDoc vectorDoc = docs[i];
					if (vectorDoc == null || vectorDoc.Rule == null || vectorDoc.Tf == null || vectorDoc.Tf.Count <= 0)
					{
						continue;
					}
					float norm = 0f;
					Dictionary<string, float> dictionary2 = BuildVectorWeights(vectorDoc.Tf, dictionary, out norm);
					if (dictionary2.Count <= 0 || norm <= 0f)
					{
						continue;
					}
					string seed = "";
					try
					{
						if (vectorDoc.Tf != null && vectorDoc.Tf.Count > 0)
						{
							seed = string.Join(" ", vectorDoc.Tf.OrderByDescending((KeyValuePair<string, int> x) => x.Value).Take(6).Select(delegate(KeyValuePair<string, int> x)
							{
								KeyValuePair<string, int> keyValuePair = x;
								return keyValuePair.Key;
							}));
						}
					}
					catch
					{
						seed = "";
					}
					list.Add(new VectorRuleEntry
					{
						Rule = vectorDoc.Rule,
						Seed = seed,
						Weights = dictionary2,
						Norm = norm,
						IsEvidence = vectorDoc.IsEvidence
					});
				}
				_vectorRuleEntries = list;
				_vectorIdf = dictionary;
				_vectorIndexVersion = _ruleDataVersion;
			}
		}
		catch
		{
		}
	}

	private void EnsureOnnxIndex()
	{
		try
		{
			if (_onnxRuleEntries != null && _onnxIndexVersion == _ruleDataVersion)
			{
				return;
			}
			lock (_onnxIndexLock)
			{
				if (_onnxRuleEntries != null && _onnxIndexVersion == _ruleDataVersion)
				{
					return;
				}
				List<OnnxRuleEntry> entries = new List<OnnxRuleEntry>();
				int num = 0;
				bool flag = false;
				try
				{
					OnnxEmbeddingEngine engine = OnnxEmbeddingEngine.Instance;
					flag = engine != null && engine.IsAvailable;
					if (flag && _file != null && _file.Rules != null)
					{
						for (int i = 0; i < _file.Rules.Count; i++)
						{
							LoreRule r = _file.Rules[i];
							if (r != null)
							{
								List<string> ruleTopicSeeds = GetRuleTopicSeeds(r);
								List<string> ruleEvidenceSeeds = GetRuleEvidenceSeeds(r);
								addSeeds(ruleTopicSeeds, isEvidence: false);
								addSeeds(ruleEvidenceSeeds, isEvidence: true);
							}
							void addSeeds(IEnumerable<string> seeds, bool isEvidence)
							{
								if (seeds == null)
								{
									return;
								}
								foreach (string seed in seeds)
								{
									string text = (seed ?? "").Trim();
									if (!string.IsNullOrWhiteSpace(text))
									{
										num++;
										if (engine.TryGetEmbedding(text, out var vector) && vector != null && vector.Length != 0)
										{
											entries.Add(new OnnxRuleEntry
											{
												Rule = r,
												Seed = text,
												Vector = vector,
												IsEvidence = isEvidence
											});
										}
									}
								}
							}
						}
					}
				}
				catch
				{
				}
				bool flag2 = _file == null || _file.Rules == null || _file.Rules.Count == 0;
				bool flag3 = num <= 0;
				if (!flag)
				{
					_onnxRuleEntries = null;
					_onnxIndexVersion = -1L;
				}
				else if (entries.Count > 0 || flag2 || flag3)
				{
					_onnxRuleEntries = entries;
					_onnxIndexVersion = _ruleDataVersion;
				}
				else
				{
					_onnxRuleEntries = null;
					_onnxIndexVersion = -1L;
				}
			}
		}
		catch
		{
		}
	}

	private static float DotProduct(float[] a, float[] b)
	{
		try
		{
			if (a == null || b == null || a.Length == 0 || b.Length == 0)
			{
				return 0f;
			}
			int num = ((a.Length < b.Length) ? a.Length : b.Length);
			double num2 = 0.0;
			for (int i = 0; i < num; i++)
			{
				num2 += (double)a[i] * (double)b[i];
			}
			return (float)num2;
		}
		catch
		{
			return 0f;
		}
	}

	private static int GetSemanticResultHardCap(int topK)
	{
		if (topK <= 0)
		{
			return 20;
		}
		return Math.Min(topK, 20);
	}

	private static int GetKnowledgeReturnCap()
	{
		try
		{
			int num = AIConfigHandler.KnowledgeSemanticTopK;
			if (num < 1)
			{
				num = 1;
			}
			if (num > 12)
			{
				num = 12;
			}
			return num;
		}
		catch
		{
			return 4;
		}
	}

	private static int GetKnowledgeRerankBudget(int returnCap)
	{
		int num = Math.Max(1, returnCap) * 3;
		if (num < 8)
		{
			num = 8;
		}
		if (num > 36)
		{
			num = 36;
		}
		return num;
	}

	private static int GetKnowledgePerIntentRerank(int rerankBudget, int intentCount)
	{
		int num = ((intentCount <= 0) ? 1 : intentCount);
		int num2 = (int)Math.Round((double)rerankBudget / (double)num, MidpointRounding.AwayFromZero);
		if (num2 < 4)
		{
			num2 = 4;
		}
		if (num2 > 12)
		{
			num2 = 12;
		}
		return num2;
	}

	private static int GetKnowledgePerIntentRecall(int rerankPerIntent)
	{
		int num = (int)Math.Round((double)rerankPerIntent * 2.5, MidpointRounding.AwayFromZero);
		if (num < 10)
		{
			num = 10;
		}
		if (num > 30)
		{
			num = 30;
		}
		return num;
	}

	private static int GetLoreInjectLimit(int returnCap)
	{
		if (returnCap < 1)
		{
			returnCap = 1;
		}
		if (returnCap > 12)
		{
			returnCap = 12;
		}
		return returnCap;
	}

	private static string BuildRuleRerankText(LoreRule rule)
	{
		try
		{
			string text = BuildRuleSearchText(rule);
			if (string.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			text = text.Replace("\r", " ").Replace("\n", " ").Trim();
			if (text.Length > 480)
			{
				text = text.Substring(0, 480);
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static List<RuleScore> CollapseRuleScoresByMax(List<RuleScore> scored)
	{
		List<RuleScore> list = new List<RuleScore>();
		try
		{
			if (scored == null || scored.Count <= 0)
			{
				return list;
			}
			Dictionary<string, RuleScore> dictionary = new Dictionary<string, RuleScore>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < scored.Count; i++)
			{
				RuleScore ruleScore = scored[i];
				if (ruleScore?.Rule == null)
				{
					continue;
				}
				string text = (ruleScore.Rule.Id ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "rule_" + i.ToString(CultureInfo.InvariantCulture);
				}
				float num = (float.IsNaN(ruleScore.RawScore) ? float.NegativeInfinity : ruleScore.RawScore);
				float num2 = (float.IsNaN(ruleScore.EvidenceScore) ? float.NegativeInfinity : ruleScore.EvidenceScore);
				if (!dictionary.TryGetValue(text, out var value) || value == null)
				{
					dictionary[text] = new RuleScore
					{
						Rule = ruleScore.Rule,
						RawScore = num,
						EvidenceScore = num2
					};
					continue;
				}
				float num3 = (float.IsNaN(value.RawScore) ? float.NegativeInfinity : value.RawScore);
				float num4 = (float.IsNaN(value.EvidenceScore) ? float.NegativeInfinity : value.EvidenceScore);
				if (num > num3)
				{
					num3 = num;
				}
				if (num2 > num4)
				{
					num4 = num2;
				}
				dictionary[text] = new RuleScore
				{
					Rule = ruleScore.Rule,
					RawScore = num3,
					EvidenceScore = num4
				};
			}
			foreach (KeyValuePair<string, RuleScore> item in dictionary)
			{
				RuleScore value2 = item.Value;
				if (value2 != null && value2.Rule != null)
				{
					float num5 = (float.IsNaN(value2.RawScore) ? float.NegativeInfinity : value2.RawScore);
					float num6 = (float.IsNaN(value2.EvidenceScore) ? float.NegativeInfinity : value2.EvidenceScore);
					if (float.IsNegativeInfinity(num5) && !float.IsNegativeInfinity(num6))
					{
						num5 = num6;
					}
					if (float.IsNegativeInfinity(num5))
					{
						num5 = 0f;
					}
					if (float.IsNegativeInfinity(num6))
					{
						num6 = 0f;
					}
					list.Add(new RuleScore
					{
						Rule = value2.Rule,
						RawScore = num5,
						EvidenceScore = num6
					});
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<RuleScore> SelectSemanticCandidateScores(List<RuleScore> scored, string source, string input, int topK)
	{
		List<RuleScore> list = new List<RuleScore>();
		try
		{
			if (scored == null || scored.Count <= 0)
			{
				return list;
			}
			int num = ((topK <= 0) ? 2 : topK);
			int semanticResultHardCap = GetSemanticResultHardCap(num);
			if (semanticResultHardCap > 0 && semanticResultHardCap < num)
			{
				num = semanticResultHardCap;
			}
			if (num < 1)
			{
				num = 1;
			}
			float num2 = 0f;
			try
			{
				num2 = AIConfigHandler.KnowledgeSemanticMinScore;
			}
			catch
			{
				num2 = 0.21f;
			}
			List<RuleScore> list2 = (from x in scored
				where x?.Rule != null && !float.IsNaN(x.RawScore)
				orderby x.RawScore descending, x.EvidenceScore descending
				select x).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			float num3 = ((list2.Count > 0) ? list2[0].RawScore : 0f);
			float num4 = ((list2.Count > 1) ? list2[1].RawScore : 0f);
			float num5 = ((list2.Count > 0) ? list2[0].EvidenceScore : 0f);
			float num6 = ((list2.Count > 1) ? list2[1].EvidenceScore : 0f);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num7 = 0;
			for (int num8 = 0; num8 < list2.Count; num8++)
			{
				if (list.Count >= num)
				{
					break;
				}
				RuleScore ruleScore = list2[num8];
				if (ruleScore?.Rule != null && !(ruleScore.RawScore < num2))
				{
					string text = (ruleScore.Rule.Id ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text) || hashSet.Add(text))
					{
						list.Add(ruleScore);
						num7++;
					}
				}
			}
			if (list.Count < num)
			{
				for (int num9 = 0; num9 < list2.Count; num9++)
				{
					if (list.Count >= num)
					{
						break;
					}
					RuleScore ruleScore2 = list2[num9];
					if (ruleScore2?.Rule != null)
					{
						string text2 = (ruleScore2.Rule.Id ?? "").Trim();
						if (string.IsNullOrWhiteSpace(text2) || hashSet.Add(text2))
						{
							list.Add(ruleScore2);
						}
					}
				}
			}
			try
			{
				Logger.Log("LoreMatch", $"semantic_accept source={source} mode=scored selected={list.Count} strictSelected={num7} topN={num} minScore={num2:0.000} bestRaw={num3:0.000} second={num4:0.000} bestEvidence={num5:0.000} secondEvidence={num6:0.000}");
			}
			catch
			{
			}
		}
		catch
		{
		}
		return list;
	}

	private List<LoreRule> SelectSemanticCandidates(List<RuleScore> scored, string source, string input, int topK)
	{
		List<LoreRule> list = new List<LoreRule>();
		try
		{
			List<RuleScore> list2 = SelectSemanticCandidateScores(scored, source, input, topK);
			for (int i = 0; i < list2.Count; i++)
			{
				if (list2[i]?.Rule != null)
				{
					list.Add(list2[i].Rule);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<RuleScore> FindOnnxCandidateScores(string input, int topK)
	{
		List<RuleScore> result = new List<RuleScore>();
		try
		{
			EnsureOnnxIndex();
			if (_onnxRuleEntries == null || _onnxRuleEntries.Count <= 0)
			{
				return result;
			}
			OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
			if (instance == null || !instance.IsAvailable)
			{
				return result;
			}
			if (!instance.TryGetEmbedding(input, out var vector) || vector == null || vector.Length == 0)
			{
				return result;
			}
			List<RuleScore> list = new List<RuleScore>();
			for (int i = 0; i < _onnxRuleEntries.Count; i++)
			{
				OnnxRuleEntry onnxRuleEntry = _onnxRuleEntries[i];
				if (onnxRuleEntry != null && onnxRuleEntry.Rule != null && onnxRuleEntry.Vector != null && onnxRuleEntry.Vector.Length != 0)
				{
					float num = DotProduct(vector, onnxRuleEntry.Vector);
					if (onnxRuleEntry.IsEvidence)
					{
						list.Add(new RuleScore
						{
							Rule = onnxRuleEntry.Rule,
							RawScore = float.NaN,
							EvidenceScore = num
						});
					}
					else
					{
						list.Add(new RuleScore
						{
							Rule = onnxRuleEntry.Rule,
							RawScore = num,
							EvidenceScore = float.NaN
						});
					}
				}
			}
			if (list.Count <= 0)
			{
				return result;
			}
			list = CollapseRuleScoresByMax(list);
			if (list.Count <= 0)
			{
				return result;
			}
			list = list.OrderByDescending((RuleScore x) => x.RawScore).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			result = SelectSemanticCandidateScores(list, "onnx", input, topK);
		}
		catch
		{
		}
		return result;
	}

	private List<RuleScore> FindSparseCandidateScores(string input, int topK)
	{
		List<RuleScore> result = new List<RuleScore>();
		try
		{
			EnsureVectorIndex();
			if (_vectorRuleEntries == null || _vectorRuleEntries.Count <= 0)
			{
				return result;
			}
			List<string> list = ExtractVectorTokens(input);
			if (list == null || list.Count <= 0)
			{
				return result;
			}
			Dictionary<string, int> dictionary = CountTokens(list);
			if (dictionary.Count <= 0)
			{
				return result;
			}
			float norm;
			Dictionary<string, float> dictionary2 = BuildVectorWeights(dictionary, _vectorIdf, out norm);
			if (dictionary2.Count <= 0 || norm <= 0f)
			{
				return result;
			}
			List<RuleScore> list2 = new List<RuleScore>();
			for (int i = 0; i < _vectorRuleEntries.Count; i++)
			{
				VectorRuleEntry vectorRuleEntry = _vectorRuleEntries[i];
				if (vectorRuleEntry == null || vectorRuleEntry.Rule == null || vectorRuleEntry.Weights == null || vectorRuleEntry.Weights.Count <= 0 || vectorRuleEntry.Norm <= 0f)
				{
					continue;
				}
				float num = DotProduct(dictionary2, vectorRuleEntry.Weights);
				if (!(num <= 0f))
				{
					float num2 = num / (norm * vectorRuleEntry.Norm);
					if (vectorRuleEntry.IsEvidence)
					{
						list2.Add(new RuleScore
						{
							Rule = vectorRuleEntry.Rule,
							RawScore = float.NaN,
							EvidenceScore = num2
						});
					}
					else
					{
						list2.Add(new RuleScore
						{
							Rule = vectorRuleEntry.Rule,
							RawScore = num2,
							EvidenceScore = float.NaN
						});
					}
				}
			}
			if (list2.Count <= 0)
			{
				return result;
			}
			list2 = CollapseRuleScoresByMax(list2);
			if (list2.Count <= 0)
			{
				return result;
			}
			list2 = list2.OrderByDescending((RuleScore x) => x.RawScore).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			result = SelectSemanticCandidateScores(list2, "sparse", input, topK);
		}
		catch
		{
		}
		return result;
	}

	private List<RuleScore> FindVectorCandidateScores(string input, int topK)
	{
		try
		{
			List<RuleScore> list = FindOnnxCandidateScores(input, topK);
			if (list != null && list.Count > 0)
			{
				try
				{
					Logger.Log("LoreMatch", $"semantic_source=onnx top={list.Count}");
				}
				catch
				{
				}
				return list;
			}
		}
		catch
		{
		}
		List<RuleScore> list2 = FindSparseCandidateScores(input, topK);
		if (list2 != null && list2.Count > 0)
		{
			try
			{
				Logger.Log("LoreMatch", $"semantic_source=sparse top={list2.Count}");
			}
			catch
			{
			}
		}
		return list2;
	}

	private List<LoreRule> FindOnnxCandidateRules(string input, int topK)
	{
		return (from x in FindOnnxCandidateScores(input, topK)
			where x?.Rule != null
			select x.Rule).ToList();
	}

	private List<LoreRule> FindSparseCandidateRules(string input, int topK)
	{
		return (from x in FindSparseCandidateScores(input, topK)
			where x?.Rule != null
			select x.Rule).ToList();
	}

	private List<LoreRule> FindVectorCandidateRules(string input, int topK)
	{
		return (from x in FindVectorCandidateScores(input, topK)
			where x?.Rule != null
			select x.Rule).ToList();
	}

	private static List<string> SplitKnowledgeIntents(string input, int maxParts = 4)
	{
		List<string> list = new List<string>();
		try
		{
			string text = (input ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return list;
			}
			List<string> list2 = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = text;
			foreach (char c in text2)
			{
				if (c == '。' || c == '！' || c == '!' || c == '？' || c == '?' || c == '；' || c == ';' || c == '，' || c == ',' || c == '、')
				{
					string text3 = stringBuilder.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list2.Add(text3);
					}
					stringBuilder.Clear();
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			string text4 = stringBuilder.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list2.Add(text4);
			}
			if (list2.Count <= 0)
			{
				list2.Add(text);
			}
			List<string> list3 = new List<string>();
			string[] array = new string[13]
			{
				"然后", "顺便", "另外", "再说", "并且", "而且", "以及", "同时", "还有", "再加上",
				"顺带", "并且还", "以及还"
			};
			for (int j = 0; j < list2.Count; j++)
			{
				string text5 = (list2[j] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text5))
				{
					continue;
				}
				bool flag = false;
				string[] array2 = array;
				foreach (string text6 in array2)
				{
					int num = text5.IndexOf(text6, StringComparison.Ordinal);
					if (num > 1 && num < text5.Length - text6.Length - 1)
					{
						string text7 = text5.Substring(0, num).Trim();
						string text8 = text5.Substring(num + text6.Length).Trim();
						if (text7.Length >= 2)
						{
							list3.Add(text7);
						}
						if (text8.Length >= 2)
						{
							list3.Add(text8);
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list3.Add(text5);
				}
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			list.Add(text);
			hashSet.Add(text);
			for (int l = 0; l < list3.Count; l++)
			{
				if (list.Count >= Math.Max(1, maxParts))
				{
					break;
				}
				string text9 = (list3[l] ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text9) && text9.Length >= 2 && hashSet.Add(text9))
				{
					list.Add(text9);
				}
			}
		}
		catch
		{
		}
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		if (list.Count <= 0)
		{
			string text10 = (input ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text10))
			{
				list = IntentQueryOptimizer.OptimizeSplitIntents(new List<string> { text10 }, 1);
			}
		}
		return list;
	}

	private static List<WeightedKnowledgeInput> BuildKnowledgeQueryInputs(string input, string secondaryInput)
	{
		List<WeightedKnowledgeInput> list = new List<WeightedKnowledgeInput>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			appendInputs(SplitKnowledgeIntents(input, 2), 2);
			string text = (secondaryInput ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, (input ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				appendInputs(SplitKnowledgeIntents(text, 2), 2);
			}
		}
		catch
		{
		}
		return list;
		void appendInputs(List<string> intents, int perSourceLimit)
		{
			if (intents != null && intents.Count > 0 && perSourceLimit > 0)
			{
				int num = 0;
				for (int i = 0; i < intents.Count; i++)
				{
					string text2 = (intents[i] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && hashSet.Add(text2))
					{
						list.Add(new WeightedKnowledgeInput
						{
							Text = text2,
							Weight = 1f
						});
						num++;
						if (num >= perSourceLimit || list.Count >= 4)
						{
							break;
						}
					}
				}
			}
		}
	}

	private static string BuildKnowledgeHitRateDetail(string detail, string secondaryInput)
	{
		string text = (detail ?? "").Trim();
		string text2 = (secondaryInput ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string arg = (string.IsNullOrWhiteSpace(text2) ? "off" : "on");
		if (text2.Length > 72)
		{
			text2 = text2.Substring(0, 72);
		}
		string text3 = $"npcRecall={arg} secondaryLen={((!string.IsNullOrWhiteSpace(text2)) ? text2.Length : 0)}";
		if (!string.IsNullOrWhiteSpace(text2))
		{
			text3 = text3 + " secondaryPreview=" + JsonConvert.ToString(text2);
		}
		return string.IsNullOrWhiteSpace(text) ? text3 : (text + " " + text3);
	}

	private List<RuleScore> RerankCandidateScores(string input, List<RuleScore> recalled, int rerankTopK, float scoreWeight = 1f)
	{
		List<RuleScore> list = new List<RuleScore>();
		try
		{
			List<RuleScore> list2 = (from x in recalled ?? new List<RuleScore>()
				where x?.Rule != null
				orderby x.RawScore descending, x.EvidenceScore descending
				select x).ThenBy((RuleScore x) => x?.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			int num = ((rerankTopK <= 0) ? 4 : rerankTopK);
			if (num > list2.Count)
			{
				num = list2.Count;
			}
			list2 = list2.Take(num).ToList();
			OnnxCrossEncoderReranker onnxCrossEncoderReranker = null;
			bool flag = false;
			try
			{
				onnxCrossEncoderReranker = OnnxCrossEncoderReranker.Instance;
				flag = onnxCrossEncoderReranker?.IsAvailable ?? false;
			}
			catch
			{
				flag = false;
			}
			List<string> list3 = null;
			List<float> scores = null;
			bool flag2 = false;
			if (flag)
			{
				list3 = new List<string>(list2.Count);
				for (int num2 = 0; num2 < list2.Count; num2++)
				{
					list3.Add((list2[num2]?.Rule == null) ? "" : BuildRuleRerankText(list2[num2].Rule));
				}
				flag2 = onnxCrossEncoderReranker.TryScoreBatch(input, list3, out scores) && scores != null && scores.Count == list2.Count;
			}
			float num3 = Math.Max(0f, scoreWeight);
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				RuleScore ruleScore = list2[num4];
				if (ruleScore?.Rule != null)
				{
					float num5 = ruleScore.RawScore;
					if (float.IsNaN(num5) || float.IsNegativeInfinity(num5))
					{
						num5 = ruleScore.EvidenceScore;
					}
					if (float.IsNaN(num5) || float.IsNegativeInfinity(num5))
					{
						num5 = 0f;
					}
					float num6 = num5 * num3;
					float num7 = num6;
					if (flag && flag2 && list3 != null && num4 < list3.Count && !string.IsNullOrWhiteSpace(list3[num4]) && scores != null && num4 < scores.Count)
					{
						num7 = scores[num4] * num3;
					}
					list.Add(new RuleScore
					{
						Rule = ruleScore.Rule,
						RawScore = num7,
						EvidenceScore = num6,
						RerankScore = num7
					});
				}
			}
			list = SelectSemanticCandidateScores(list, (flag && flag2) ? "cross_encoder" : "recall_fallback", input, num);
		}
		catch
		{
		}
		return list;
	}

	private List<LoreRule> MergeVectorRulesAcrossIntents(List<WeightedKnowledgeInput> intentInputs, int recallTopK, int rerankTopK, int injectLimit, out string matchMode)
	{
		List<LoreRule> result = new List<LoreRule>();
		matchMode = "none";
		try
		{
			List<WeightedKnowledgeInput> list = (intentInputs ?? new List<WeightedKnowledgeInput>()).Where((WeightedKnowledgeInput x) => x != null && !string.IsNullOrWhiteSpace(x.Text) && x.Weight > 0f).ToList();
			if (list.Count <= 0)
			{
				return result;
			}
			bool flag = false;
			try
			{
				flag = OnnxCrossEncoderReranker.Instance.IsAvailable;
			}
			catch
			{
				flag = false;
			}
			matchMode = ((!flag) ? ((list.Count > 1) ? "semantic_multi" : "semantic") : ((list.Count > 1) ? "rerank_multi" : "rerank"));
			Dictionary<LoreRule, RuleAggregate> dictionary = new Dictionary<LoreRule, RuleAggregate>();
			for (int num = 0; num < list.Count; num++)
			{
				WeightedKnowledgeInput weightedKnowledgeInput = list[num];
				List<RuleScore> list2 = FindVectorCandidateScores(weightedKnowledgeInput.Text, recallTopK);
				if (list2 == null || list2.Count <= 0)
				{
					continue;
				}
				List<RuleScore> list3 = RerankCandidateScores(weightedKnowledgeInput.Text, list2, rerankTopK, weightedKnowledgeInput.Weight);
				if (list3 == null || list3.Count <= 0)
				{
					continue;
				}
				for (int num2 = 0; num2 < list3.Count; num2++)
				{
					RuleScore ruleScore = list3[num2];
					if (ruleScore?.Rule == null)
					{
						continue;
					}
					float rawScore = ruleScore.RawScore;
					if (!dictionary.TryGetValue(ruleScore.Rule, out var value))
					{
						dictionary[ruleScore.Rule] = new RuleAggregate
						{
							Rule = ruleScore.Rule,
							Score = rawScore,
							BestRank = num2 + 1,
							HitCount = 1
						};
						continue;
					}
					value.Score += rawScore;
					value.HitCount++;
					if (num2 + 1 < value.BestRank)
					{
						value.BestRank = num2 + 1;
					}
					dictionary[ruleScore.Rule] = value;
				}
			}
			if (dictionary.Count <= 0)
			{
				return result;
			}
			int num3 = Math.Max(injectLimit * 2, rerankTopK * Math.Min(list.Count, 3));
			if (num3 < injectLimit)
			{
				num3 = injectLimit;
			}
			if (num3 > 24)
			{
				num3 = 24;
			}
			result = (from x in (from x in dictionary.Values
					orderby x.Score + (float)(x.HitCount - 1) * 0.08f descending, x.BestRank
					select x).ThenBy((RuleAggregate x) => x.Rule?.Id ?? "", StringComparer.OrdinalIgnoreCase)
				select x.Rule into x
				where x != null
				select x).Take(num3).ToList();
		}
		catch
		{
		}
		return result;
	}

	private CandidateRules CollectCandidateRules(string input, string secondaryInput = null)
	{
		CandidateRules candidateRules = new CandidateRules();
		try
		{
			List<WeightedKnowledgeInput> list = BuildKnowledgeQueryInputs(input, secondaryInput);
			if (list.Count > 1)
			{
				try
				{
					Logger.Log("LoreMatch", string.Format("intent_split count={0} intents={1}", list.Count, string.Join(" || ", list.Select((WeightedKnowledgeInput x) => $"{x.Text}@{x.Weight:0.00}"))));
				}
				catch
				{
				}
			}
			bool flag = true;
			try
			{
				flag = AIConfigHandler.KnowledgeRetrievalEnabled;
			}
			catch
			{
			}
			if (!flag)
			{
				return candidateRules;
			}
			int knowledgeReturnCap = GetKnowledgeReturnCap();
			int knowledgeRerankBudget = GetKnowledgeRerankBudget(knowledgeReturnCap);
			int num = Math.Max(1, list.Count);
			int knowledgePerIntentRerank = GetKnowledgePerIntentRerank(knowledgeRerankBudget, num);
			int knowledgePerIntentRecall = GetKnowledgePerIntentRecall(knowledgePerIntentRerank);
			int loreInjectLimit = GetLoreInjectLimit(knowledgeReturnCap);
			candidateRules.IntentCount = num;
			candidateRules.RerankPerIntent = knowledgePerIntentRerank;
			candidateRules.RecallPerIntent = knowledgePerIntentRecall;
			candidateRules.InjectLimit = loreInjectLimit;
			string matchMode;
			List<LoreRule> list2 = MergeVectorRulesAcrossIntents(list, knowledgePerIntentRecall, knowledgePerIntentRerank, loreInjectLimit, out matchMode);
			if (list2 != null && list2.Count > 0)
			{
				candidateRules.MatchMode = matchMode;
				candidateRules.OrderedRules = list2.Where((LoreRule x) => x != null).ToList();
				try
				{
					Logger.Log("LoreMatch", $"candidate_pool mode={candidateRules.MatchMode} returnCap={loreInjectLimit} rerankBudget={knowledgeRerankBudget} rerankPerIntent={knowledgePerIntentRerank} recallPerIntent={knowledgePerIntentRecall} intents={num} got={candidateRules.OrderedRules.Count}");
				}
				catch
				{
				}
				return candidateRules;
			}
			return candidateRules;
		}
		catch
		{
		}
		return candidateRules;
	}

	private static string SanitizeRuleIdPart(string s)
	{
		try
		{
			string text = (s ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			string text2 = text;
			foreach (char c in text2)
			{
				stringBuilder.Append(char.IsWhiteSpace(c) ? '_' : c);
			}
			text = stringBuilder.ToString();
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			char[] array = invalidFileNameChars;
			foreach (char oldChar in array)
			{
				text = text.Replace(oldChar, '_');
			}
			while (text.Contains("__"))
			{
				text = text.Replace("__", "_");
			}
			text = text.Trim('_');
			if (text.Length > 64)
			{
				text = text.Substring(0, 64).Trim('_');
			}
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static string NormalizeKeywordForCompare(string keyword)
	{
		try
		{
			string text = (keyword ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			bool flag = false;
			string text2 = text;
			foreach (char c in text2)
			{
				if (char.IsWhiteSpace(c))
				{
					if (!flag)
					{
						stringBuilder.Append(' ');
					}
					flag = true;
				}
				else
				{
					stringBuilder.Append(c);
					flag = false;
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private bool TryFindRuleIdByKeyword(string keyword, string excludeRuleId, out string foundRuleId)
	{
		foundRuleId = null;
		try
		{
			string text = NormalizeKeywordForCompare(keyword);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string text2 = (excludeRuleId ?? "").Trim();
			if (_file == null || _file.Rules == null)
			{
				return false;
			}
			foreach (LoreRule rule in _file.Rules)
			{
				if (rule == null)
				{
					continue;
				}
				string text3 = (rule.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text3) || (!string.IsNullOrEmpty(text2) && string.Equals(text3, text2, StringComparison.OrdinalIgnoreCase)) || rule.Keywords == null || rule.Keywords.Count <= 0)
				{
					continue;
				}
				foreach (string keyword2 in rule.Keywords)
				{
					string text4 = NormalizeKeywordForCompare(keyword2);
					if (string.IsNullOrEmpty(text4) || !string.Equals(text4, text, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					foundRuleId = text3;
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private string BuildRuleIdFromKeyword(string keyword)
	{
		string text = SanitizeRuleIdPart(keyword);
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return "rule_" + text;
	}

	private bool HasRuleIdIgnoreCase(string id)
	{
		try
		{
			string tid = (id ?? "").Trim();
			if (string.IsNullOrEmpty(tid))
			{
				return false;
			}
			if (_file == null || _file.Rules == null)
			{
				return false;
			}
			return _file.Rules.Any((LoreRule r) => r != null && string.Equals((r.Id ?? "").Trim(), tid, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	public List<RuleIndexItem> GetRuleIndexItemsForDev(int maxCount = 200)
	{
		List<RuleIndexItem> list = new List<RuleIndexItem>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreRule rule in _file.Rules)
			{
				if (list.Count >= maxCount)
				{
					break;
				}
				string text = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text) || !hashSet.Add(text))
				{
					continue;
				}
				string text2 = "";
				try
				{
					text2 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text2 = "";
				}
				string label = (string.IsNullOrWhiteSpace(text2) ? text : (text2 + " (" + text + ")"));
				list.Add(new RuleIndexItem
				{
					Id = text,
					Label = label
				});
			}
			try
			{
				list = list.OrderBy((RuleIndexItem x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			}
			catch
			{
			}
			if (list.Count > maxCount)
			{
				list.RemoveRange(maxCount, list.Count - maxCount);
			}
		}
		catch
		{
		}
		return list;
	}

	private void EnsureRuleIndexCache()
	{
		try
		{
			if (_ruleIndexCache != null && _ruleIndexCacheVersion == _ruleDataVersion)
			{
				return;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			List<RuleIndexItem> list = new List<RuleIndexItem>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreRule rule in _file.Rules)
			{
				string text = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text) || !hashSet.Add(text))
				{
					continue;
				}
				string text2 = "";
				try
				{
					text2 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text2 = "";
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = text;
				}
				string label = $"{text2} (关键词:{(rule?.Keywords?.Count).GetValueOrDefault()} 条, RAG短句:{(rule?.RagShortTexts?.Count).GetValueOrDefault()} 条, 提示词:{(rule?.Variants?.Count).GetValueOrDefault()} 条)";
				list.Add(new RuleIndexItem
				{
					Id = text,
					Label = label
				});
			}
			try
			{
				list = list.OrderBy((RuleIndexItem x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			}
			catch
			{
			}
			_ruleIndexCache = list;
			_ruleIndexCacheVersion = _ruleDataVersion;
		}
		catch
		{
			try
			{
				_ruleIndexCache = _ruleIndexCache ?? new List<RuleIndexItem>();
				_ruleIndexCacheVersion = _ruleDataVersion;
			}
			catch
			{
			}
		}
	}

	private static bool IsRuleListMatch(RuleIndexItem it, string[] tokens)
	{
		if (tokens == null || tokens.Length == 0)
		{
			return true;
		}
		string text = it?.Label ?? "";
		string text2 = it?.Id ?? "";
		for (int i = 0; i < tokens.Length; i++)
		{
			string value = (tokens[i] ?? "").Trim();
			if (!string.IsNullOrEmpty(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0 && text2.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool ShouldLogLore(string key, long minIntervalTicks)
	{
		try
		{
			long ticks = DateTime.UtcNow.Ticks;
			lock (_loreLogLock)
			{
				if (_loreLogLastTicks == null)
				{
					_loreLogLastTicks = new Dictionary<string, long>();
				}
				if (_loreLogLastTicks.TryGetValue(key, out var value) && ticks - value < minIntervalTicks)
				{
					return false;
				}
				_loreLogLastTicks[key] = ticks;
				if (_loreLogLastTicks.Count > 600)
				{
					_loreLogLastTicks.Clear();
				}
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	private static void LogLoreMissOnce(string tag, string input, int ruleCount, string heroId, string cultureId, string kingdomId, string role)
	{
		try
		{
			string text = TrimPreview(input, 80);
			string key = tag + ":" + Hash8(text) + ":" + heroId + ":" + cultureId + ":" + kingdomId + ":" + role;
			if (ShouldLogLore(key, TimeSpan.FromMilliseconds(1500.0).Ticks))
			{
				Logger.Log("LoreMatch", string.Format("{0} rules={1} hero={2} culture={3} kingdom={4} role={5} input={6}", tag, ruleCount, heroId ?? "", cultureId ?? "", kingdomId ?? "", role ?? "", text));
			}
		}
		catch
		{
		}
	}

	private static void LogLoreContextTrace(string source, string heroId, string charId, string cultureId, string kingdomId, string settlementId, string role, bool isFemale, bool isClanLeader, string kingdomOverride, string inputText, bool invalidContext = false)
	{
		try
		{
			string text = (source ?? "").Trim();
			string text2 = (heroId ?? "").Trim();
			string text3 = (charId ?? "").Trim();
			string text4 = (cultureId ?? "").Trim().ToLowerInvariant();
			string text5 = (kingdomId ?? "").Trim().ToLowerInvariant();
			string text6 = (settlementId ?? "").Trim().ToLowerInvariant();
			string text7 = (role ?? "").Trim().ToLowerInvariant();
			string text8 = (kingdomOverride ?? "").Trim().ToLowerInvariant();
			string text9 = Hash8(inputText ?? "");
			Logger.Log("LoreMatch", $"lore_ctx source={text} invalid={invalidContext} hero={text2} char={text3} culture={text4} kingdom={text5} settlement={text6} role={text7} female={isFemale} clanLeader={isClanLeader} kingdomOverride={text8} inputHash={text9}");
		}
		catch
		{
		}
	}

	public KnowledgeLibraryBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener((object)this, (Action<IMission>)OnMissionStarted);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		TryStartBackgroundIndexWarmup("session_launch");
	}

	private void OnMissionStarted(IMission mission)
	{
		if (mission != null && Mission.Current != null)
		{
			TryStartBackgroundIndexWarmup("mission_start");
		}
	}

	internal static void TryStartBackgroundIndexWarmup(string source)
	{
		try
		{
			KnowledgeLibraryBehavior instance = Instance;
			if (instance == null)
			{
				return;
			}
			long num = _ruleDataVersion;
			if (num <= 0)
			{
				num = 1L;
			}
			if ((Volatile.Read(ref _indexWarmupState) != 2 || Volatile.Read(ref _indexWarmupVersion) != num) && Interlocked.CompareExchange(ref _indexWarmupState, 1, 0) == 0)
			{
				Interlocked.Exchange(ref _indexWarmupVersion, num);
				string warmupSource = (string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim());
				Logger.Log("KnowledgeIndexWarmup", $"start source={warmupSource} version={num}");
				Task.Run(delegate
				{
					instance.RunIndexWarmup(warmupSource, num);
				});
			}
		}
		catch
		{
		}
	}

	private void RunIndexWarmup(string source, long version)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		bool flag = false;
		bool flag2 = false;
		int num = 0;
		int num2 = 0;
		string text = "";
		string text2 = "";
		try
		{
			EnsureVectorIndex();
			flag = _vectorRuleEntries != null && _vectorIndexVersion == version;
			num = _vectorRuleEntries?.Count ?? 0;
		}
		catch (Exception ex)
		{
			text = ex.Message ?? "vector index warmup exception";
		}
		try
		{
			EnsureOnnxIndex();
			bool flag3 = _file == null || _file.Rules == null || _file.Rules.Count == 0;
			num2 = _onnxRuleEntries?.Count ?? 0;
			flag2 = _onnxRuleEntries != null && _onnxIndexVersion == version && (num2 > 0 || flag3);
		}
		catch (Exception ex2)
		{
			text2 = ex2.Message ?? "onnx index warmup exception";
		}
		stopwatch.Stop();
		bool flag4 = _ruleDataVersion != version;
		bool flag5 = !flag4 && (!flag2 || num2 <= 0) && string.IsNullOrWhiteSpace(text2);
		if (flag4)
		{
			Interlocked.Exchange(ref _indexWarmupState, 0);
			Interlocked.Exchange(ref _indexWarmupVersion, -1L);
		}
		else if (flag5)
		{
			Interlocked.Exchange(ref _indexWarmupState, 0);
		}
		else
		{
			Interlocked.Exchange(ref _indexWarmupState, 2);
		}
		Logger.Log("KnowledgeIndexWarmup", $"complete source={source} version={version} stale={flag4} retryPending={flag5} ms={Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)} sparseOk={flag} sparseEntries={num} onnxOk={flag2} onnxEntries={num2} sparseError={text} onnxError={text2}");
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		StripSemanticPrototypes();
		if (dataStore != null && dataStore.IsSaving)
		{
			try
			{
				_storageJson = JsonConvert.SerializeObject((object)_file, (Formatting)0);
			}
			catch
			{
				_storageJson = "";
			}
			SaveStorageJson(dataStore, _storageJson);
		}
		else if (dataStore != null && dataStore.IsLoading)
		{
			_storageJson = LoadStorageJson(dataStore);
			if (TryDeserializeKnowledgeFile(_storageJson, out var knowledgeFile))
			{
				_file = knowledgeFile;
			}
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		StripSemanticPrototypes();
		TouchRuleData();
	}

	private void StripSemanticPrototypes()
	{
		try
		{
			if (_file == null || _file.Rules == null)
			{
				return;
			}
			for (int i = 0; i < _file.Rules.Count; i++)
			{
				LoreRule loreRule = _file.Rules[i];
				if (loreRule != null)
				{
					if (loreRule.TextMappings == null)
					{
						loreRule.TextMappings = new List<LoreTextMapping>();
					}
					if (loreRule.RagShortTexts == null)
					{
						loreRule.RagShortTexts = new List<string>();
					}
					if (loreRule.SemanticPrototypes == null)
					{
						loreRule.SemanticPrototypes = new List<string>();
					}
					else if (loreRule.SemanticPrototypes.Count > 0)
					{
						loreRule.SemanticPrototypes.Clear();
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void EnsureRagShortTexts(LoreRule rule)
	{
		try
		{
			if (rule != null && rule.RagShortTexts == null)
			{
				rule.RagShortTexts = new List<string>();
			}
		}
		catch
		{
		}
	}

	private static void EnsureTextMappings(LoreRule rule)
	{
		try
		{
			if (rule != null && rule.TextMappings == null)
			{
				rule.TextMappings = new List<LoreTextMapping>();
			}
		}
		catch
		{
		}
	}

	private bool HasAnyTextMappings()
	{
		try
		{
			KnowledgeFile file = _file;
			return file != null && file.Rules?.Any((LoreRule r) => r?.TextMappings != null && r.TextMappings.Any((LoreTextMapping m) => m != null && !string.IsNullOrWhiteSpace(m.SourceText) && !string.IsNullOrWhiteSpace(m.Kind) && !string.IsNullOrWhiteSpace(m.TargetId))) == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsStatusMappingKind(string kind)
	{
		return (kind ?? "").Trim().StartsWith("status|", StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildStatusMappingKind(string sourceKey, string statusKey)
	{
		sourceKey = (sourceKey ?? "").Trim().ToLowerInvariant();
		statusKey = (statusKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(sourceKey) || string.IsNullOrWhiteSpace(statusKey))
		{
			return "";
		}
		return "status|" + sourceKey + "|" + statusKey;
	}

	private static bool TryParseStatusMappingKind(string kind, out string sourceKey, out string statusKey)
	{
		sourceKey = "";
		statusKey = "";
		string[] array = (kind ?? "").Trim().Split(new char[1] { '|' }, StringSplitOptions.None);
		if (array.Length != 3 || !string.Equals(array[0], "status", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		sourceKey = (array[1] ?? "").Trim().ToLowerInvariant();
		statusKey = (array[2] ?? "").Trim().ToLowerInvariant();
		return !string.IsNullOrWhiteSpace(sourceKey) && !string.IsNullOrWhiteSpace(statusKey);
	}

	private static bool IsStatusAgeRangeKind(string kind)
	{
		if (!TryParseStatusMappingKind(kind, out var _, out var statusKey))
		{
			return false;
		}
		return string.Equals(statusKey, "is_in_age_range", StringComparison.OrdinalIgnoreCase) || string.Equals(statusKey, "has_age_range_members", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsTextMappingAgeRangeKind(string kind)
	{
		return IsClanAgeRangeMemberKind(kind) || IsStatusAgeRangeKind(kind);
	}

	private static string GetStatusSourceLabel(string sourceKey)
	{
		string text = (sourceKey ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"hero" => "英雄", 
			"current_npc_hero" => "当前NPC", 
			"player_hero" => "玩家", 
			"bound_hero" => "本知识绑定英雄", 
			"clan" => "家族", 
			"current_npc_clan" => "当前NPC所属家族", 
			"player_clan" => "玩家家族", 
			"bound_settlement_owner_clan" => "本知识绑定定居点统治家族", 
			"bound_hero_clan" => "本知识绑定英雄所属家族", 
			"kingdom" => "王国", 
			"current_npc_kingdom" => "当前NPC所属王国", 
			"player_kingdom" => "玩家所属王国", 
			"bound_kingdom" => "本知识绑定王国", 
			"settlement" => "定居点", 
			"current_npc_settlement" => "当前NPC所在定居点", 
			"player_settlement" => "玩家所在定居点", 
			"bound_settlement" => "本知识绑定定居点", 
			_ => "状态对象", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetStatusSourceTargetTypeLabel(string sourceKey)
	{
		return (sourceKey ?? "").Trim().ToLowerInvariant() switch
		{
			"hero" => "英雄", 
			"clan" => "家族", 
			"kingdom" => "王国", 
			"settlement" => "定居点", 
			_ => "自动目标", 
		};
	}

	private static string GetStatusSourceObjectKind(string sourceKey)
	{
		switch ((sourceKey ?? "").Trim().ToLowerInvariant())
		{
		case "hero":
		case "current_npc_hero":
		case "player_hero":
		case "bound_hero":
			return "hero";
		case "clan":
		case "current_npc_clan":
		case "player_clan":
		case "bound_settlement_owner_clan":
		case "bound_hero_clan":
			return "clan";
		case "kingdom":
		case "current_npc_kingdom":
		case "player_kingdom":
		case "bound_kingdom":
			return "kingdom";
		case "settlement":
		case "current_npc_settlement":
		case "player_settlement":
		case "bound_settlement":
			return "settlement";
		default:
			return "";
		}
	}

	private static string GetAutomaticTargetIdForStatusSource(string sourceKey)
	{
		string text = (sourceKey ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"current_npc_hero" => "__current_npc__", 
			"current_npc_clan" => "__current_npc__", 
			"current_npc_kingdom" => "__current_npc__", 
			"current_npc_settlement" => "__current_npc__", 
			"player_hero" => "__player__", 
			"player_clan" => "__player__", 
			"player_kingdom" => "__player__", 
			"player_settlement" => "__player__", 
			"bound_hero" => "__bound_hero__", 
			"bound_hero_clan" => "__bound_hero__", 
			"bound_kingdom" => "__bound_kingdom__", 
			"bound_settlement" => "__bound_settlement__", 
			"bound_settlement_owner_clan" => "__bound_settlement__", 
			_ => "", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetStatusConditionLabel(string statusKey)
	{
		string text = (statusKey ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"is_alive" => "是否存活", 
			"is_dead" => "是否死亡", 
			"is_disabled" => "是否失能", 
			"is_missing" => "是否失踪/逃亡", 
			"is_married" => "是否已婚", 
			"is_widowed" => "是否丧偶", 
			"is_female" => "是否女性", 
			"is_male" => "是否男性", 
			"is_child" => "是否未成年", 
			"is_adult" => "是否成年", 
			"is_in_age_range" => "是否在年龄段内", 
			"is_clan_leader" => "是否家族族长", 
			"is_kingdom_leader" => "是否王国领袖", 
			"is_governor" => "是否总督", 
			"is_prisoner" => "是否被俘", 
			"is_in_settlement" => "是否在定居点内", 
			"is_in_field" => "是否在野外", 
			"is_wanderer" => "是否流浪者", 
			"is_notable" => "是否要人", 
			"is_lord" => "是否领主", 
			"is_merchant" => "是否商人", 
			"is_gang_leader" => "是否帮派头目", 
			"is_artisan" => "是否工匠", 
			"is_preacher" => "是否传教士", 
			"is_headman" => "是否村长", 
			"is_minor_faction_hero" => "是否小势力英雄", 
			"is_party_leader" => "是否带队", 
			"is_player_companion" => "是否玩家同伴", 
			"is_rebel" => "是否叛军", 
			"is_wounded" => "是否受伤", 
			"is_known_to_player" => "是否被玩家认识", 
			"has_children" => "是否有子女", 
			"has_father" => "是否有父亲", 
			"has_mother" => "是否有母亲", 
			"has_home_settlement" => "是否有家乡定居点", 
			"is_eliminated" => "是否已灭亡/被消灭", 
			"has_kingdom" => "是否有所属王国", 
			"has_leader" => "是否有领袖", 
			"has_ruling_clan" => "是否有执政家族", 
			"has_any_settlement" => "是否拥有任何定居点", 
			"has_any_town" => "是否拥有任何城镇", 
			"has_any_castle" => "是否拥有任何城堡", 
			"has_any_village" => "是否拥有任何村庄", 
			"has_members" => "是否有成员", 
			"has_male_members" => "是否有男性成员", 
			"has_female_members" => "是否有女性成员", 
			"has_age_range_members" => "是否有年龄段成员", 
			"is_mercenary" => "是否雇佣兵家族", 
			"is_minor_faction" => "是否小势力", 
			"is_rebel_clan" => "是否叛军家族", 
			"is_noble" => "是否贵族家族", 
			"is_bandit_faction" => "是否匪帮势力", 
			"is_outlaw" => "是否法外势力", 
			"has_any_clan" => "是否有任何家族", 
			"has_any_lord" => "是否有任何领主", 
			"has_active_policies" => "是否有生效政策", 
			"has_any_war" => "是否处于战争中", 
			"has_any_allies" => "是否有盟友", 
			"is_active" => "是否活跃", 
			"is_town" => "是否城镇", 
			"is_castle" => "是否城堡", 
			"is_village" => "是否村庄", 
			"is_fortification" => "是否堡垒", 
			"is_hideout" => "是否藏身处", 
			"is_under_siege" => "是否被围攻", 
			"is_under_raid" => "是否正在被劫掠", 
			"is_raided" => "是否已被劫掠", 
			"is_starving" => "是否饥荒", 
			"is_rebellious" => "是否处于叛乱状态", 
			"has_port" => "是否有港口", 
			"has_owner" => "是否有统治者", 
			"has_owner_clan" => "是否有统治家族", 
			"has_notables" => "是否有要人", 
			"has_parties" => "是否有驻留队伍", 
			_ => "未知状态", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetTextMappingStatusTrueText(LoreTextMapping mapping)
	{
		try
		{
			return (mapping?.TrueText ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetTextMappingStatusFalseText(LoreTextMapping mapping)
	{
		try
		{
			return (mapping?.FalseText ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetTextMappingKindLabel(string kind)
	{
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var statusKey))
		{
			return "状态判断：" + GetStatusSourceLabel(sourceKey) + GetStatusConditionLabel(statusKey);
		}
		string text = (kind ?? "").Trim();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"kingdom_name" => "王国名称", 
			"kingdom_leader_name" => "王国当前领袖", 
			"kingdom_ruling_clan_name" => "王国执政家族", 
			"kingdom_culture_name" => "王国文化", 
			"kingdom_initial_home_settlement_name" => "王国初始都城", 
			"kingdom_all_clans" => "王国全部家族", 
			"kingdom_all_lords" => "王国全部领主", 
			"kingdom_all_towns" => "王国拥有的所有城镇", 
			"kingdom_all_castles" => "王国拥有的所有城堡", 
			"kingdom_all_villages" => "王国拥有的所有村庄", 
			"kingdom_all_settlements" => "王国拥有的所有定居点", 
			"kingdom_active_policies" => "王国生效政策", 
			"kingdom_allied_kingdoms" => "王国盟友", 
			"kingdom_war_factions" => "王国交战势力", 
			"settlement_name" => "定居点名称", 
			"settlement_owner_clan_name" => "定居点统治家族", 
			"settlement_owner_leader_name" => "定居点统治者", 
			"settlement_owner_kingdom_name" => "定居点所属王国", 
			"settlement_owner_kingdom_leader_name" => "定居点所属王国领袖", 
			"settlement_culture_name" => "定居点文化", 
			"settlement_notables" => "定居点要人", 
			"settlement_parties" => "定居点驻留队伍", 
			"settlement_bound_villages" => "定居点绑定村庄", 
			"clan_name" => "家族名称", 
			"clan_leader_name" => "家族当前族长", 
			"clan_kingdom_name" => "家族所属王国", 
			"clan_kingdom_leader_name" => "家族所属王国领袖", 
			"clan_all_towns" => "家族统治的所有城镇", 
			"clan_all_villages" => "家族统治的所有村子", 
			"clan_all_settlements" => "家族统治的所有定居点", 
			"clan_members" => "家族成员", 
			"clan_male_members" => "家族男性成员", 
			"clan_female_members" => "家族女性成员", 
			"clan_age_range_members" => "家族年龄段成员", 
			"hero_name" => "英雄当前名字", 
			"hero_clan_name" => "英雄所属家族", 
			"hero_kingdom_name" => "英雄所属王国", 
			"hero_kingdom_leader_name" => "英雄效忠君主", 
			"hero_spouse_name" => "英雄配偶", 
			"hero_father_name" => "英雄父亲", 
			"hero_mother_name" => "英雄母亲", 
			"hero_current_settlement_name" => "英雄当前定居点", 
			"current_npc_name" => "当前NPC名字", 
			"current_npc_clan_name" => "当前NPC所属家族", 
			"current_npc_clan_kingdom_name" => "当前NPC所属家族的所属王国", 
			"current_npc_clan_kingdom_leader_name" => "当前NPC所属家族的所属王国领袖", 
			"current_npc_clan_all_towns" => "当前NPC所属家族的所有城镇", 
			"current_npc_clan_all_villages" => "当前NPC所属家族的所有村子", 
			"current_npc_clan_all_settlements" => "当前NPC所属家族的所有定居点", 
			"current_npc_clan_members" => "当前NPC所属家族的成员", 
			"current_npc_clan_male_members" => "当前NPC所属家族的男性成员", 
			"current_npc_clan_female_members" => "当前NPC所属家族的女性成员", 
			"current_npc_clan_age_range_members" => "当前NPC所属家族的年龄段成员", 
			"current_npc_kingdom_name" => "当前NPC所属王国", 
			"current_npc_kingdom_leader_name" => "当前NPC效忠君主", 
			"current_npc_kingdom_ruling_clan_name" => "当前NPC所属王国执政家族", 
			"current_npc_kingdom_culture_name" => "当前NPC所属王国文化", 
			"current_npc_kingdom_initial_home_settlement_name" => "当前NPC所属王国初始都城", 
			"current_npc_kingdom_all_clans" => "当前NPC所属王国全部家族", 
			"current_npc_kingdom_all_lords" => "当前NPC所属王国全部领主", 
			"current_npc_kingdom_all_towns" => "当前NPC所属王国拥有的所有城镇", 
			"current_npc_kingdom_all_castles" => "当前NPC所属王国拥有的所有城堡", 
			"current_npc_kingdom_all_villages" => "当前NPC所属王国拥有的所有村庄", 
			"current_npc_kingdom_all_settlements" => "当前NPC所属王国拥有的所有定居点", 
			"current_npc_kingdom_active_policies" => "当前NPC所属王国生效政策", 
			"current_npc_kingdom_allied_kingdoms" => "当前NPC所属王国盟友", 
			"current_npc_kingdom_war_factions" => "当前NPC所属王国交战势力", 
			"current_npc_spouse_name" => "当前NPC配偶", 
			"current_npc_father_name" => "当前NPC父亲", 
			"current_npc_mother_name" => "当前NPC母亲", 
			"current_npc_current_settlement_name" => "当前NPC所在定居点", 
			"current_npc_current_settlement_owner_clan_name" => "当前NPC所在定居点统治家族", 
			"current_npc_current_settlement_owner_leader_name" => "当前NPC所在定居点统治者", 
			"current_npc_current_settlement_owner_kingdom_name" => "当前NPC所在定居点所属王国", 
			"current_npc_current_settlement_owner_kingdom_leader_name" => "当前NPC所在定居点所属王国领袖", 
			"current_npc_current_settlement_culture_name" => "当前NPC所在定居点文化", 
			"current_npc_current_settlement_notables" => "当前NPC所在定居点要人", 
			"current_npc_current_settlement_parties" => "当前NPC所在定居点驻留队伍", 
			"current_npc_current_settlement_bound_villages" => "当前NPC所在定居点绑定村庄", 
			"player_name" => "玩家名字", 
			"player_clan_name" => "玩家家族", 
			"player_clan_kingdom_name" => "玩家家族所属王国", 
			"player_clan_kingdom_leader_name" => "玩家家族所属王国领袖", 
			"player_clan_all_towns" => "玩家家族的所有城镇", 
			"player_clan_all_villages" => "玩家家族的所有村子", 
			"player_clan_all_settlements" => "玩家家族的所有定居点", 
			"player_clan_members" => "玩家家族的成员", 
			"player_clan_male_members" => "玩家家族的男性成员", 
			"player_clan_female_members" => "玩家家族的女性成员", 
			"player_clan_age_range_members" => "玩家家族的年龄段成员", 
			"player_kingdom_name" => "玩家所属王国", 
			"player_kingdom_leader_name" => "玩家效忠君主", 
			"player_spouse_name" => "玩家配偶", 
			"player_current_settlement_name" => "玩家所在定居点", 
			"bound_kingdom_name" => "本知识绑定王国", 
			"bound_kingdom_leader_name" => "本知识绑定王国领袖", 
			"bound_kingdom_ruling_clan_name" => "本知识绑定王国执政家族", 
			"bound_kingdom_culture_name" => "本知识绑定王国文化", 
			"bound_kingdom_initial_home_settlement_name" => "本知识绑定王国初始都城", 
			"bound_kingdom_all_clans" => "本知识绑定王国全部家族", 
			"bound_kingdom_all_lords" => "本知识绑定王国全部领主", 
			"bound_kingdom_all_towns" => "本知识绑定王国拥有的所有城镇", 
			"bound_kingdom_all_castles" => "本知识绑定王国拥有的所有城堡", 
			"bound_kingdom_all_villages" => "本知识绑定王国拥有的所有村庄", 
			"bound_kingdom_all_settlements" => "本知识绑定王国拥有的所有定居点", 
			"bound_kingdom_active_policies" => "本知识绑定王国生效政策", 
			"bound_kingdom_allied_kingdoms" => "本知识绑定王国盟友", 
			"bound_kingdom_war_factions" => "本知识绑定王国交战势力", 
			"bound_settlement_name" => "本知识绑定定居点", 
			"bound_settlement_owner_clan_name" => "本知识绑定定居点统治家族", 
			"bound_settlement_owner_clan_kingdom_name" => "本知识绑定定居点统治家族的所属王国", 
			"bound_settlement_owner_clan_kingdom_leader_name" => "本知识绑定定居点统治家族的所属王国领袖", 
			"bound_settlement_owner_clan_all_towns" => "本知识绑定定居点统治家族的所有城镇", 
			"bound_settlement_owner_clan_all_villages" => "本知识绑定定居点统治家族的所有村子", 
			"bound_settlement_owner_clan_all_settlements" => "本知识绑定定居点统治家族的所有定居点", 
			"bound_settlement_owner_clan_members" => "本知识绑定定居点统治家族的成员", 
			"bound_settlement_owner_clan_male_members" => "本知识绑定定居点统治家族的男性成员", 
			"bound_settlement_owner_clan_female_members" => "本知识绑定定居点统治家族的女性成员", 
			"bound_settlement_owner_clan_age_range_members" => "本知识绑定定居点统治家族的年龄段成员", 
			"bound_settlement_owner_leader_name" => "本知识绑定定居点统治者", 
			"bound_settlement_owner_kingdom_name" => "本知识绑定定居点所属王国", 
			"bound_settlement_owner_kingdom_leader_name" => "本知识绑定定居点所属王国领袖", 
			"bound_settlement_culture_name" => "本知识绑定定居点文化", 
			"bound_settlement_notables" => "本知识绑定定居点要人", 
			"bound_settlement_parties" => "本知识绑定定居点驻留队伍", 
			"bound_settlement_bound_villages" => "本知识绑定定居点绑定村庄", 
			"bound_hero_name" => "本知识绑定英雄", 
			"bound_hero_clan_name" => "本知识绑定英雄所属家族", 
			"bound_hero_clan_kingdom_name" => "本知识绑定英雄所属家族的所属王国", 
			"bound_hero_clan_kingdom_leader_name" => "本知识绑定英雄所属家族的所属王国领袖", 
			"bound_hero_clan_all_towns" => "本知识绑定英雄所属家族的所有城镇", 
			"bound_hero_clan_all_villages" => "本知识绑定英雄所属家族的所有村子", 
			"bound_hero_clan_all_settlements" => "本知识绑定英雄所属家族的所有定居点", 
			"bound_hero_clan_members" => "本知识绑定英雄所属家族的成员", 
			"bound_hero_clan_male_members" => "本知识绑定英雄所属家族的男性成员", 
			"bound_hero_clan_female_members" => "本知识绑定英雄所属家族的女性成员", 
			"bound_hero_clan_age_range_members" => "本知识绑定英雄所属家族的年龄段成员", 
			"bound_hero_kingdom_name" => "本知识绑定英雄所属王国", 
			"bound_hero_kingdom_leader_name" => "本知识绑定英雄效忠君主", 
			"bound_hero_spouse_name" => "本知识绑定英雄配偶", 
			"bound_hero_father_name" => "本知识绑定英雄父亲", 
			"bound_hero_mother_name" => "本知识绑定英雄母亲", 
			"bound_hero_current_settlement_name" => "本知识绑定英雄所在定居点", 
			_ => "未知映射", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetTextMappingTargetTypeLabel(string kind)
	{
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var statusKey))
		{
			return GetStatusSourceTargetTypeLabel(sourceKey);
		}
		string text = (kind ?? "").Trim();
		if (1 == 0)
		{
		}
		statusKey = text switch
		{
			"kingdom_name" => "王国", 
			"kingdom_leader_name" => "王国", 
			"kingdom_ruling_clan_name" => "王国", 
			"kingdom_culture_name" => "王国", 
			"kingdom_initial_home_settlement_name" => "王国", 
			"kingdom_all_clans" => "王国", 
			"kingdom_all_lords" => "王国", 
			"kingdom_all_towns" => "王国", 
			"kingdom_all_castles" => "王国", 
			"kingdom_all_villages" => "王国", 
			"kingdom_all_settlements" => "王国", 
			"kingdom_active_policies" => "王国", 
			"kingdom_allied_kingdoms" => "王国", 
			"kingdom_war_factions" => "王国", 
			"settlement_name" => "定居点", 
			"settlement_owner_clan_name" => "定居点", 
			"settlement_owner_leader_name" => "定居点", 
			"settlement_owner_kingdom_name" => "定居点", 
			"settlement_owner_kingdom_leader_name" => "定居点", 
			"settlement_culture_name" => "定居点", 
			"settlement_notables" => "定居点", 
			"settlement_parties" => "定居点", 
			"settlement_bound_villages" => "定居点", 
			"clan_name" => "家族", 
			"clan_leader_name" => "家族", 
			"clan_kingdom_name" => "家族", 
			"clan_kingdom_leader_name" => "家族", 
			"clan_all_towns" => "家族", 
			"clan_all_villages" => "家族", 
			"clan_all_settlements" => "家族", 
			"clan_members" => "家族", 
			"clan_male_members" => "家族", 
			"clan_female_members" => "家族", 
			"clan_age_range_members" => "家族", 
			"hero_name" => "英雄", 
			"hero_clan_name" => "英雄", 
			"hero_kingdom_name" => "英雄", 
			"hero_kingdom_leader_name" => "英雄", 
			"hero_spouse_name" => "英雄", 
			"hero_father_name" => "英雄", 
			"hero_mother_name" => "英雄", 
			"hero_current_settlement_name" => "英雄", 
			"current_npc_name" => "自动目标", 
			"current_npc_clan_name" => "自动目标", 
			"current_npc_clan_kingdom_name" => "自动目标", 
			"current_npc_clan_kingdom_leader_name" => "自动目标", 
			"current_npc_clan_all_towns" => "自动目标", 
			"current_npc_clan_all_villages" => "自动目标", 
			"current_npc_clan_all_settlements" => "自动目标", 
			"current_npc_clan_members" => "自动目标", 
			"current_npc_clan_male_members" => "自动目标", 
			"current_npc_clan_female_members" => "自动目标", 
			"current_npc_clan_age_range_members" => "自动目标", 
			"current_npc_kingdom_name" => "自动目标", 
			"current_npc_kingdom_leader_name" => "自动目标", 
			"current_npc_kingdom_ruling_clan_name" => "自动目标", 
			"current_npc_kingdom_culture_name" => "自动目标", 
			"current_npc_kingdom_initial_home_settlement_name" => "自动目标", 
			"current_npc_kingdom_all_clans" => "自动目标", 
			"current_npc_kingdom_all_lords" => "自动目标", 
			"current_npc_kingdom_all_towns" => "自动目标", 
			"current_npc_kingdom_all_castles" => "自动目标", 
			"current_npc_kingdom_all_villages" => "自动目标", 
			"current_npc_kingdom_all_settlements" => "自动目标", 
			"current_npc_kingdom_active_policies" => "自动目标", 
			"current_npc_kingdom_allied_kingdoms" => "自动目标", 
			"current_npc_kingdom_war_factions" => "自动目标", 
			"current_npc_spouse_name" => "自动目标", 
			"current_npc_father_name" => "自动目标", 
			"current_npc_mother_name" => "自动目标", 
			"current_npc_current_settlement_name" => "自动目标", 
			"current_npc_current_settlement_owner_clan_name" => "自动目标", 
			"current_npc_current_settlement_owner_leader_name" => "自动目标", 
			"current_npc_current_settlement_owner_kingdom_name" => "自动目标", 
			"current_npc_current_settlement_owner_kingdom_leader_name" => "自动目标", 
			"current_npc_current_settlement_culture_name" => "自动目标", 
			"current_npc_current_settlement_notables" => "自动目标", 
			"current_npc_current_settlement_parties" => "自动目标", 
			"current_npc_current_settlement_bound_villages" => "自动目标", 
			"player_name" => "自动目标", 
			"player_clan_name" => "自动目标", 
			"player_clan_kingdom_name" => "自动目标", 
			"player_clan_kingdom_leader_name" => "自动目标", 
			"player_clan_all_towns" => "自动目标", 
			"player_clan_all_villages" => "自动目标", 
			"player_clan_all_settlements" => "自动目标", 
			"player_clan_members" => "自动目标", 
			"player_clan_male_members" => "自动目标", 
			"player_clan_female_members" => "自动目标", 
			"player_clan_age_range_members" => "自动目标", 
			"player_kingdom_name" => "自动目标", 
			"player_kingdom_leader_name" => "自动目标", 
			"player_spouse_name" => "自动目标", 
			"player_current_settlement_name" => "自动目标", 
			"bound_kingdom_name" => "自动目标", 
			"bound_kingdom_leader_name" => "自动目标", 
			"bound_kingdom_ruling_clan_name" => "自动目标", 
			"bound_kingdom_culture_name" => "自动目标", 
			"bound_kingdom_initial_home_settlement_name" => "自动目标", 
			"bound_kingdom_all_clans" => "自动目标", 
			"bound_kingdom_all_lords" => "自动目标", 
			"bound_kingdom_all_towns" => "自动目标", 
			"bound_kingdom_all_castles" => "自动目标", 
			"bound_kingdom_all_villages" => "自动目标", 
			"bound_kingdom_all_settlements" => "自动目标", 
			"bound_kingdom_active_policies" => "自动目标", 
			"bound_kingdom_allied_kingdoms" => "自动目标", 
			"bound_kingdom_war_factions" => "自动目标", 
			"bound_settlement_name" => "自动目标", 
			"bound_settlement_owner_clan_name" => "自动目标", 
			"bound_settlement_owner_clan_kingdom_name" => "自动目标", 
			"bound_settlement_owner_clan_kingdom_leader_name" => "自动目标", 
			"bound_settlement_owner_clan_all_towns" => "自动目标", 
			"bound_settlement_owner_clan_all_villages" => "自动目标", 
			"bound_settlement_owner_clan_all_settlements" => "自动目标", 
			"bound_settlement_owner_clan_members" => "自动目标", 
			"bound_settlement_owner_clan_male_members" => "自动目标", 
			"bound_settlement_owner_clan_female_members" => "自动目标", 
			"bound_settlement_owner_clan_age_range_members" => "自动目标", 
			"bound_settlement_owner_leader_name" => "自动目标", 
			"bound_settlement_owner_kingdom_name" => "自动目标", 
			"bound_settlement_owner_kingdom_leader_name" => "自动目标", 
			"bound_settlement_culture_name" => "自动目标", 
			"bound_settlement_notables" => "自动目标", 
			"bound_settlement_parties" => "自动目标", 
			"bound_settlement_bound_villages" => "自动目标", 
			"bound_hero_name" => "自动目标", 
			"bound_hero_clan_name" => "自动目标", 
			"bound_hero_clan_kingdom_name" => "自动目标", 
			"bound_hero_clan_kingdom_leader_name" => "自动目标", 
			"bound_hero_clan_all_towns" => "自动目标", 
			"bound_hero_clan_all_villages" => "自动目标", 
			"bound_hero_clan_all_settlements" => "自动目标", 
			"bound_hero_clan_members" => "自动目标", 
			"bound_hero_clan_male_members" => "自动目标", 
			"bound_hero_clan_female_members" => "自动目标", 
			"bound_hero_clan_age_range_members" => "自动目标", 
			"bound_hero_kingdom_name" => "自动目标", 
			"bound_hero_kingdom_leader_name" => "自动目标", 
			"bound_hero_spouse_name" => "自动目标", 
			"bound_hero_father_name" => "自动目标", 
			"bound_hero_mother_name" => "自动目标", 
			"bound_hero_current_settlement_name" => "自动目标", 
			_ => "目标", 
		};
		if (1 == 0)
		{
		}
		return statusKey;
	}

	private static string GetAutomaticTargetIdForKind(string kind)
	{
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var statusKey))
		{
			return GetAutomaticTargetIdForStatusSource(sourceKey);
		}
		string text = (kind ?? "").Trim();
		if (1 == 0)
		{
		}
		statusKey = text switch
		{
			"current_npc_name" => "__current_npc__", 
			"current_npc_clan_name" => "__current_npc__", 
			"current_npc_clan_kingdom_name" => "__current_npc__", 
			"current_npc_clan_kingdom_leader_name" => "__current_npc__", 
			"current_npc_clan_all_towns" => "__current_npc__", 
			"current_npc_clan_all_villages" => "__current_npc__", 
			"current_npc_clan_all_settlements" => "__current_npc__", 
			"current_npc_clan_members" => "__current_npc__", 
			"current_npc_clan_male_members" => "__current_npc__", 
			"current_npc_clan_female_members" => "__current_npc__", 
			"current_npc_clan_age_range_members" => "__current_npc__", 
			"current_npc_kingdom_name" => "__current_npc__", 
			"current_npc_kingdom_leader_name" => "__current_npc__", 
			"current_npc_kingdom_ruling_clan_name" => "__current_npc__", 
			"current_npc_kingdom_culture_name" => "__current_npc__", 
			"current_npc_kingdom_initial_home_settlement_name" => "__current_npc__", 
			"current_npc_kingdom_all_clans" => "__current_npc__", 
			"current_npc_kingdom_all_lords" => "__current_npc__", 
			"current_npc_kingdom_all_towns" => "__current_npc__", 
			"current_npc_kingdom_all_castles" => "__current_npc__", 
			"current_npc_kingdom_all_villages" => "__current_npc__", 
			"current_npc_kingdom_all_settlements" => "__current_npc__", 
			"current_npc_kingdom_active_policies" => "__current_npc__", 
			"current_npc_kingdom_allied_kingdoms" => "__current_npc__", 
			"current_npc_kingdom_war_factions" => "__current_npc__", 
			"current_npc_spouse_name" => "__current_npc__", 
			"current_npc_father_name" => "__current_npc__", 
			"current_npc_mother_name" => "__current_npc__", 
			"current_npc_current_settlement_name" => "__current_npc__", 
			"current_npc_current_settlement_owner_clan_name" => "__current_npc__", 
			"current_npc_current_settlement_owner_leader_name" => "__current_npc__", 
			"current_npc_current_settlement_owner_kingdom_name" => "__current_npc__", 
			"current_npc_current_settlement_owner_kingdom_leader_name" => "__current_npc__", 
			"current_npc_current_settlement_culture_name" => "__current_npc__", 
			"current_npc_current_settlement_notables" => "__current_npc__", 
			"current_npc_current_settlement_parties" => "__current_npc__", 
			"current_npc_current_settlement_bound_villages" => "__current_npc__", 
			"player_name" => "__player__", 
			"player_clan_name" => "__player__", 
			"player_clan_kingdom_name" => "__player__", 
			"player_clan_kingdom_leader_name" => "__player__", 
			"player_clan_all_towns" => "__player__", 
			"player_clan_all_villages" => "__player__", 
			"player_clan_all_settlements" => "__player__", 
			"player_clan_members" => "__player__", 
			"player_clan_male_members" => "__player__", 
			"player_clan_female_members" => "__player__", 
			"player_clan_age_range_members" => "__player__", 
			"player_kingdom_name" => "__player__", 
			"player_kingdom_leader_name" => "__player__", 
			"player_spouse_name" => "__player__", 
			"player_current_settlement_name" => "__player__", 
			"bound_kingdom_name" => "__bound_kingdom__", 
			"bound_kingdom_leader_name" => "__bound_kingdom__", 
			"bound_kingdom_ruling_clan_name" => "__bound_kingdom__", 
			"bound_kingdom_culture_name" => "__bound_kingdom__", 
			"bound_kingdom_initial_home_settlement_name" => "__bound_kingdom__", 
			"bound_kingdom_all_clans" => "__bound_kingdom__", 
			"bound_kingdom_all_lords" => "__bound_kingdom__", 
			"bound_kingdom_all_towns" => "__bound_kingdom__", 
			"bound_kingdom_all_castles" => "__bound_kingdom__", 
			"bound_kingdom_all_villages" => "__bound_kingdom__", 
			"bound_kingdom_all_settlements" => "__bound_kingdom__", 
			"bound_kingdom_active_policies" => "__bound_kingdom__", 
			"bound_kingdom_allied_kingdoms" => "__bound_kingdom__", 
			"bound_kingdom_war_factions" => "__bound_kingdom__", 
			"bound_settlement_name" => "__bound_settlement__", 
			"bound_settlement_owner_clan_name" => "__bound_settlement__", 
			"bound_settlement_owner_clan_kingdom_name" => "__bound_settlement__", 
			"bound_settlement_owner_clan_kingdom_leader_name" => "__bound_settlement__", 
			"bound_settlement_owner_clan_all_towns" => "__bound_settlement__", 
			"bound_settlement_owner_clan_all_villages" => "__bound_settlement__", 
			"bound_settlement_owner_clan_all_settlements" => "__bound_settlement__", 
			"bound_settlement_owner_clan_members" => "__bound_settlement__", 
			"bound_settlement_owner_clan_male_members" => "__bound_settlement__", 
			"bound_settlement_owner_clan_female_members" => "__bound_settlement__", 
			"bound_settlement_owner_clan_age_range_members" => "__bound_settlement__", 
			"bound_settlement_owner_leader_name" => "__bound_settlement__", 
			"bound_settlement_owner_kingdom_name" => "__bound_settlement__", 
			"bound_settlement_owner_kingdom_leader_name" => "__bound_settlement__", 
			"bound_settlement_culture_name" => "__bound_settlement__", 
			"bound_settlement_notables" => "__bound_settlement__", 
			"bound_settlement_parties" => "__bound_settlement__", 
			"bound_settlement_bound_villages" => "__bound_settlement__", 
			"bound_hero_name" => "__bound_hero__", 
			"bound_hero_clan_name" => "__bound_hero__", 
			"bound_hero_clan_kingdom_name" => "__bound_hero__", 
			"bound_hero_clan_kingdom_leader_name" => "__bound_hero__", 
			"bound_hero_clan_all_towns" => "__bound_hero__", 
			"bound_hero_clan_all_villages" => "__bound_hero__", 
			"bound_hero_clan_all_settlements" => "__bound_hero__", 
			"bound_hero_clan_members" => "__bound_hero__", 
			"bound_hero_clan_male_members" => "__bound_hero__", 
			"bound_hero_clan_female_members" => "__bound_hero__", 
			"bound_hero_clan_age_range_members" => "__bound_hero__", 
			"bound_hero_kingdom_name" => "__bound_hero__", 
			"bound_hero_kingdom_leader_name" => "__bound_hero__", 
			"bound_hero_spouse_name" => "__bound_hero__", 
			"bound_hero_father_name" => "__bound_hero__", 
			"bound_hero_mother_name" => "__bound_hero__", 
			"bound_hero_current_settlement_name" => "__bound_hero__", 
			_ => "", 
		};
		if (1 == 0)
		{
		}
		return statusKey;
	}

	private static string GetHeroDisplayName(Hero hero)
	{
		try
		{
			return (((hero == null) ? null : ((object)hero.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetClanDisplayName(Clan clan)
	{
		try
		{
			return (((clan == null) ? null : ((object)clan.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetKingdomDisplayName(Kingdom kingdom)
	{
		try
		{
			return (((kingdom == null) ? null : ((object)kingdom.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetCultureDisplayName(CultureObject culture)
	{
		try
		{
			return (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetFactionDisplayName(IFaction faction)
	{
		try
		{
			return (((faction == null) ? null : ((object)faction.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetSettlementDisplayName(Settlement settlement)
	{
		try
		{
			return (((settlement == null) ? null : ((object)settlement.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetSettlementTypeLabel(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return "";
			}
			if (settlement.IsTown)
			{
				return "城镇";
			}
			if (settlement.IsCastle)
			{
				return "城堡";
			}
			if (settlement.IsVillage)
			{
				return "村子";
			}
		}
		catch
		{
		}
		return "定居点";
	}

	private static int GetSettlementTypeOrder(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return 99;
			}
			if (settlement.IsTown)
			{
				return 0;
			}
			if (settlement.IsCastle)
			{
				return 1;
			}
			if (settlement.IsVillage)
			{
				return 2;
			}
		}
		catch
		{
		}
		return 99;
	}

	private static string FormatSettlementWithType(Settlement settlement)
	{
		string settlementDisplayName = GetSettlementDisplayName(settlement);
		if (string.IsNullOrWhiteSpace(settlementDisplayName))
		{
			return "";
		}
		string settlementTypeLabel = GetSettlementTypeLabel(settlement);
		return string.IsNullOrWhiteSpace(settlementTypeLabel) ? settlementDisplayName : (settlementDisplayName + "（" + settlementTypeLabel + "）");
	}

	private static string BuildKingdomSettlementListText(Kingdom kingdom, bool includeTowns, bool includeCastles, bool includeVillages)
	{
		try
		{
			if (((kingdom != null) ? kingdom.Settlements : null) == null)
			{
				return "";
			}
			List<Settlement> list = new List<Settlement>();
			foreach (Settlement item in (List<Settlement>)(object)kingdom.Settlements)
			{
				if (item != null)
				{
					if (item.IsTown && includeTowns)
					{
						list.Add(item);
					}
					else if (item.IsCastle && includeCastles)
					{
						list.Add(item);
					}
					else if (item.IsVillage && includeVillages)
					{
						list.Add(item);
					}
				}
			}
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", from x in list.OrderBy((Settlement s) => GetSettlementTypeOrder(s)).ThenBy((Settlement s) => GetSettlementDisplayName(s), StringComparer.OrdinalIgnoreCase).Select(FormatSettlementWithType)
				where !string.IsNullOrWhiteSpace(x)
				select x);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildClanOwnedSettlementListText(Clan clan, bool includeTowns, bool includeCastles, bool includeVillages)
	{
		try
		{
			if (clan == null || Settlement.All == null)
			{
				return "";
			}
			List<Settlement> list = new List<Settlement>();
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item != null && item.OwnerClan == clan)
				{
					if (item.IsTown && includeTowns)
					{
						list.Add(item);
					}
					else if (item.IsCastle && includeCastles)
					{
						list.Add(item);
					}
					else if (item.IsVillage && includeVillages)
					{
						list.Add(item);
					}
				}
			}
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", from x in list.OrderBy((Settlement s) => GetSettlementTypeOrder(s)).ThenBy((Settlement s) => GetSettlementDisplayName(s), StringComparer.OrdinalIgnoreCase).Select(FormatSettlementWithType)
				where !string.IsNullOrWhiteSpace(x)
				select x);
		}
		catch
		{
			return "";
		}
	}

	private static List<Hero> GetClanLivingMembers(Clan clan)
	{
		List<Hero> list = new List<Hero>();
		try
		{
			if (clan == null)
			{
				return list;
			}
			foreach (Hero item in (List<Hero>)(object)Hero.AllAliveHeroes)
			{
				if (item != null && item.Clan == clan)
				{
					list.Add(item);
				}
			}
			return list.OrderByDescending((Hero h) => (clan.Leader == h) ? 1 : 0).ThenBy((Hero h) => GetHeroDisplayName(h), StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => ((MBObjectBase)h).StringId ?? "", StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return list;
		}
	}

	private static string BuildClanMemberListText(Clan clan, Func<Hero, bool> predicate)
	{
		try
		{
			List<Hero> list = GetClanLivingMembers(clan);
			if (predicate != null)
			{
				list = list.Where((Hero h) => h != null && predicate(h)).ToList();
			}
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", from x in list.Select(GetHeroDisplayName)
				where !string.IsNullOrWhiteSpace(x)
				select x);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildKingdomClanListText(Kingdom kingdom)
	{
		try
		{
			Kingdom obj = kingdom;
			if (((obj != null) ? obj.Clans : null) == null)
			{
				return "";
			}
			List<Clan> list = (from c in (IEnumerable<Clan>)kingdom.Clans
				where c != null
				orderby (kingdom.RulingClan == c) ? 1 : 0 descending
				select c).ThenBy((Clan c) => GetClanDisplayName(c), StringComparer.OrdinalIgnoreCase).ThenBy((Clan c) => ((MBObjectBase)c).StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", from x in list.Select(GetClanDisplayName)
				where !string.IsNullOrWhiteSpace(x)
				select x);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildKingdomLordListText(Kingdom kingdom)
	{
		try
		{
			Kingdom obj = kingdom;
			if (((obj != null) ? obj.AliveLords : null) == null)
			{
				return "";
			}
			List<Hero> list = (from h in (IEnumerable<Hero>)kingdom.AliveLords
				where h != null
				orderby (kingdom.Leader == h) ? 1 : 0 descending
				select h).ThenBy((Hero h) => GetHeroDisplayName(h), StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => ((MBObjectBase)h).StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", from x in list.Select(GetHeroDisplayName)
				where !string.IsNullOrWhiteSpace(x)
				select x);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildPolicyListText(IEnumerable<PolicyObject> policies)
	{
		try
		{
			if (policies == null)
			{
				return "";
			}
			List<string> list = (from p in policies
				where p != null
				select (((object)((PropertyObject)p).Name)?.ToString() ?? "").Trim() into x
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildKingdomListText(IEnumerable<Kingdom> kingdoms)
	{
		try
		{
			if (kingdoms == null)
			{
				return "";
			}
			List<string> list = (from x in kingdoms.Where((Kingdom k) => k != null).Select(GetKingdomDisplayName)
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildFactionListText(IEnumerable<IFaction> factions)
	{
		try
		{
			if (factions == null)
			{
				return "";
			}
			List<string> list = (from x in factions.Where((IFaction f) => f != null).Select(GetFactionDisplayName)
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list);
		}
		catch
		{
			return "";
		}
	}

	private static int NormalizeMemberAgeBound(int? value, int fallback)
	{
		int num = value ?? fallback;
		if (num < 0)
		{
			num = 0;
		}
		if (num > 120)
		{
			num = 120;
		}
		return num;
	}

	private static bool IsClanAgeRangeMemberKind(string kind)
	{
		switch ((kind ?? "").Trim())
		{
		case "clan_age_range_members":
		case "current_npc_clan_age_range_members":
		case "player_clan_age_range_members":
		case "bound_settlement_owner_clan_age_range_members":
		case "bound_hero_clan_age_range_members":
			return true;
		default:
			return false;
		}
	}

	private static Kingdom FindKingdomById(string id)
	{
		try
		{
			string text = (id ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || Kingdom.All == null)
			{
				return null;
			}
			return ((IEnumerable<Kingdom>)Kingdom.All).FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Clan FindClanById(string id)
	{
		try
		{
			string text = (id ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || Clan.All == null)
			{
				return null;
			}
			return ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Clan x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement FindSettlementById(string id)
	{
		try
		{
			string text = (id ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || Settlement.All == null)
			{
				return null;
			}
			return ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Settlement x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero FindHeroById(string id)
	{
		try
		{
			string text = (id ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)));
		}
		catch
		{
			return null;
		}
	}

	private static string TryGetSingleBoundIdFromRule(LoreRule rule, Func<LoreWhen, List<string>> selector)
	{
		try
		{
			if (rule?.Variants == null || selector == null)
			{
				return "";
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreVariant variant in rule.Variants)
			{
				List<string> list = selector(variant?.When);
				if (list == null)
				{
					continue;
				}
				foreach (string item in list)
				{
					string text = (item ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						hashSet.Add(text);
						if (hashSet.Count > 1)
						{
							return "";
						}
					}
				}
			}
			return hashSet.FirstOrDefault() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static string TryGetSingleBoundHeroIdFromRule(LoreRule rule)
	{
		try
		{
			if (rule?.Variants == null)
			{
				return "";
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreVariant variant in rule.Variants)
			{
				LoreWhen loreWhen = variant?.When;
				if (loreWhen?.HeroIds != null)
				{
					foreach (string heroId in loreWhen.HeroIds)
					{
						string text = (heroId ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(text))
						{
							hashSet.Add(text);
							if (hashSet.Count > 1)
							{
								return "";
							}
						}
					}
				}
				if (loreWhen?.IdentityIds == null)
				{
					continue;
				}
				foreach (string identityId in loreWhen.IdentityIds)
				{
					if (TryParseRoleIdentityId(identityId, out var kind, out var targetId) && string.Equals(kind, "hero", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(targetId))
					{
						hashSet.Add(targetId);
						if (hashSet.Count > 1)
						{
							return "";
						}
					}
				}
			}
			return hashSet.FirstOrDefault() ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static Hero ResolveBoundHeroFromRule(LoreRule rule)
	{
		return FindHeroById(TryGetSingleBoundHeroIdFromRule(rule));
	}

	private static Kingdom ResolveBoundKingdomFromRule(LoreRule rule)
	{
		return FindKingdomById(TryGetSingleBoundIdFromRule(rule, (LoreWhen when) => when?.KingdomIds));
	}

	private static Settlement ResolveBoundSettlementFromRule(LoreRule rule)
	{
		return FindSettlementById(TryGetSingleBoundIdFromRule(rule, (LoreWhen when) => when?.SettlementIds));
	}

	private static Kingdom GetHeroKingdom(Hero hero)
	{
		try
		{
			object obj;
			if (hero == null)
			{
				obj = null;
			}
			else
			{
				Clan clan = hero.Clan;
				obj = ((clan != null) ? clan.Kingdom : null);
			}
			if (obj == null)
			{
				obj = (object)((((hero != null) ? hero.MapFaction : null) != null && hero.MapFaction.IsKingdomFaction) ? /*isinst with value type is only supported in some contexts*/: null);
			}
			return (Kingdom)obj;
		}
		catch
		{
			return null;
		}
	}

	private static string GetHeroClanName(Hero hero)
	{
		return GetClanDisplayName((hero != null) ? hero.Clan : null);
	}

	private static string GetHeroKingdomName(Hero hero)
	{
		return GetKingdomDisplayName(GetHeroKingdom(hero));
	}

	private static string GetClanKingdomName(Clan clan)
	{
		return GetKingdomDisplayName((clan != null) ? clan.Kingdom : null);
	}

	private static string GetHeroKingdomLeaderName(Hero hero)
	{
		Kingdom heroKingdom = GetHeroKingdom(hero);
		return GetHeroDisplayName((heroKingdom != null) ? heroKingdom.Leader : null);
	}

	private static string GetClanKingdomLeaderName(Clan clan)
	{
		object hero;
		if (clan == null)
		{
			hero = null;
		}
		else
		{
			Kingdom kingdom = clan.Kingdom;
			hero = ((kingdom != null) ? kingdom.Leader : null);
		}
		return GetHeroDisplayName((Hero)hero);
	}

	private static Kingdom GetSettlementKingdom(Settlement settlement)
	{
		try
		{
			object obj;
			if (settlement == null)
			{
				obj = null;
			}
			else
			{
				Clan ownerClan = settlement.OwnerClan;
				obj = ((ownerClan != null) ? ownerClan.Kingdom : null);
			}
			if (obj == null)
			{
				obj = (object)((((settlement != null) ? settlement.MapFaction : null) != null && settlement.MapFaction.IsKingdomFaction) ? /*isinst with value type is only supported in some contexts*/: null);
			}
			return (Kingdom)obj;
		}
		catch
		{
			return null;
		}
	}

	private static string GetSettlementKingdomName(Settlement settlement)
	{
		return GetKingdomDisplayName(GetSettlementKingdom(settlement));
	}

	private static string GetSettlementKingdomLeaderName(Settlement settlement)
	{
		Kingdom settlementKingdom = GetSettlementKingdom(settlement);
		return GetHeroDisplayName((settlementKingdom != null) ? settlementKingdom.Leader : null);
	}

	private static string GetSettlementCultureName(Settlement settlement)
	{
		return GetCultureDisplayName(settlement?.Culture);
	}

	private static string BuildSettlementNotableListText(Settlement settlement)
	{
		try
		{
			if (((settlement != null) ? settlement.Notables : null) == null)
			{
				return "";
			}
			List<string> list = (from x in ((IEnumerable<Hero>)settlement.Notables).Where((Hero h) => h != null).Select(GetHeroDisplayName)
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildSettlementPartyListText(Settlement settlement)
	{
		try
		{
			if (((settlement != null) ? settlement.Parties : null) == null)
			{
				return "";
			}
			List<string> list = (from p in (IEnumerable<MobileParty>)settlement.Parties
				where ((p != null) ? p.Party : null) != null
				select (((object)p.Party.Name)?.ToString() ?? "").Trim() into x
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildSettlementBoundVillageListText(Settlement settlement)
	{
		try
		{
			if (((settlement != null) ? settlement.BoundVillages : null) == null)
			{
				return "";
			}
			List<string> list = (from v in (IEnumerable<Village>)settlement.BoundVillages
				where ((v != null) ? ((SettlementComponent)v).Settlement : null) != null
				select FormatSettlementWithType(((SettlementComponent)v).Settlement) into x
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list);
		}
		catch
		{
			return "";
		}
	}

	private static string GetHeroSpouseName(Hero hero)
	{
		return GetHeroDisplayName((hero != null) ? hero.Spouse : null);
	}

	private static string GetHeroFatherName(Hero hero)
	{
		return GetHeroDisplayName((hero != null) ? hero.Father : null);
	}

	private static string GetHeroMotherName(Hero hero)
	{
		return GetHeroDisplayName((hero != null) ? hero.Mother : null);
	}

	private static string GetHeroCurrentSettlementName(Hero hero)
	{
		return GetSettlementDisplayName(((hero != null) ? hero.CurrentSettlement : null) ?? ((hero != null) ? hero.StayingInSettlement : null));
	}

	private static Hero ResolveRuntimeHeroFromMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			if (string.Equals(text, "__current_npc__", StringComparison.OrdinalIgnoreCase))
			{
				return npcHero ?? ((npcCharacter != null && ((BasicCharacterObject)npcCharacter).IsHero) ? npcCharacter.HeroObject : null);
			}
			if (string.Equals(text, "__player__", StringComparison.OrdinalIgnoreCase))
			{
				return Hero.MainHero;
			}
			if (string.Equals(text, "__bound_hero__", StringComparison.OrdinalIgnoreCase))
			{
				return ResolveBoundHeroFromRule(rule);
			}
			return FindHeroById(text);
		}
		catch
		{
			return null;
		}
	}

	private static Kingdom ResolveRuntimeKingdomFromMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			if (string.Equals(text, "__bound_kingdom__", StringComparison.OrdinalIgnoreCase))
			{
				return ResolveBoundKingdomFromRule(rule);
			}
			Hero val = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
			if (val != null && (string.Equals(text, "__current_npc__", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "__player__", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "__bound_hero__", StringComparison.OrdinalIgnoreCase)))
			{
				return GetHeroKingdom(val);
			}
			return FindKingdomById(text);
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveRuntimeSettlementFromMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			if (string.Equals(text, "__bound_settlement__", StringComparison.OrdinalIgnoreCase))
			{
				return ResolveBoundSettlementFromRule(rule);
			}
			Hero val = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
			if (val != null && (string.Equals(text, "__current_npc__", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "__player__", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "__bound_hero__", StringComparison.OrdinalIgnoreCase)))
			{
				return val.CurrentSettlement ?? val.StayingInSettlement;
			}
			return FindSettlementById(text);
		}
		catch
		{
			return null;
		}
	}

	private static Clan ResolveRuntimeClanFromMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			Hero val = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
			if (val != null && (string.Equals(text, "__current_npc__", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "__player__", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "__bound_hero__", StringComparison.OrdinalIgnoreCase)))
			{
				return val.Clan;
			}
			if (string.Equals(text, "__bound_settlement__", StringComparison.OrdinalIgnoreCase))
			{
				Settlement obj = ResolveBoundSettlementFromRule(rule);
				return (obj != null) ? obj.OwnerClan : null;
			}
			return FindClanById(text);
		}
		catch
		{
			return null;
		}
	}

	private static string GetStatusMappingTargetDisplayName(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter, string sourceKey)
	{
		try
		{
			string sourceKey2 = (sourceKey ?? "").Trim().ToLowerInvariant();
			switch (GetStatusSourceObjectKind(sourceKey2))
			{
			case "hero":
			{
				Hero hero = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
				string heroDisplayName = GetHeroDisplayName(hero);
				string statusSourceLabel3 = GetStatusSourceLabel(sourceKey2);
				return string.IsNullOrWhiteSpace(heroDisplayName) ? (statusSourceLabel3 + "（当前无值）") : (statusSourceLabel3 + "（" + heroDisplayName + "）");
			}
			case "clan":
			{
				Clan clan = ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter);
				string clanDisplayName = GetClanDisplayName(clan);
				string statusSourceLabel2 = GetStatusSourceLabel(sourceKey2);
				return string.IsNullOrWhiteSpace(clanDisplayName) ? (statusSourceLabel2 + "（当前无值）") : (statusSourceLabel2 + "（" + clanDisplayName + "）");
			}
			case "kingdom":
			{
				Kingdom kingdom = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				string kingdomDisplayName = GetKingdomDisplayName(kingdom);
				string statusSourceLabel4 = GetStatusSourceLabel(sourceKey2);
				return string.IsNullOrWhiteSpace(kingdomDisplayName) ? (statusSourceLabel4 + "（当前无值）") : (statusSourceLabel4 + "（" + kingdomDisplayName + "）");
			}
			case "settlement":
			{
				Settlement settlement = ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter);
				string settlementDisplayName = GetSettlementDisplayName(settlement);
				string statusSourceLabel = GetStatusSourceLabel(sourceKey2);
				return string.IsNullOrWhiteSpace(settlementDisplayName) ? (statusSourceLabel + "（当前无值）") : (statusSourceLabel + "（" + settlementDisplayName + "）");
			}
			default:
				return GetStatusSourceLabel(sourceKey2);
			}
		}
		catch
		{
			return GetStatusSourceLabel(sourceKey);
		}
	}

	private static bool HasAnyItems<T>(IEnumerable<T> items, Func<T, bool> predicate = null)
	{
		try
		{
			if (items == null)
			{
				return false;
			}
			foreach (T item in items)
			{
				if (predicate == null || predicate(item))
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static bool HeroHasAnyChildren(Hero hero)
	{
		try
		{
			return hero != null && hero.Children != null && ((List<Hero>)(object)hero.Children).Count > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool HeroHasAnyExSpouses(Hero hero)
	{
		try
		{
			return hero != null && hero.ExSpouses != null && ((List<Hero>)(object)hero.ExSpouses).Count > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool ClanHasOwnedSettlement(Clan clan, Func<Settlement, bool> predicate)
	{
		try
		{
			if (clan == null || Settlement.All == null)
			{
				return false;
			}
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item != null && item.OwnerClan == clan && (predicate == null || predicate(item)))
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static bool KingdomHasSettlement(Kingdom kingdom, Func<Settlement, bool> predicate)
	{
		try
		{
			if (kingdom == null || kingdom.Settlements == null)
			{
				return false;
			}
			foreach (Settlement item in (List<Settlement>)(object)kingdom.Settlements)
			{
				if (item != null && (predicate == null || predicate(item)))
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static bool? EvaluateHeroStatus(Hero hero, string statusKey, LoreTextMapping mapping)
	{
		if (hero == null)
		{
			return null;
		}
		switch ((statusKey ?? "").Trim().ToLowerInvariant())
		{
		case "is_alive":
			return hero.IsAlive;
		case "is_dead":
			return hero.IsDead;
		case "is_disabled":
			return hero.IsDisabled;
		case "is_missing":
			return hero.IsFugitive;
		case "is_married":
			return hero.Spouse != null;
		case "is_widowed":
			return hero.Spouse == null && HeroHasAnyExSpouses(hero);
		case "is_female":
			return hero.IsFemale;
		case "is_male":
			return !hero.IsFemale;
		case "is_child":
			return hero.IsChild;
		case "is_adult":
			return !hero.IsChild;
		case "is_in_age_range":
		{
			int num = NormalizeMemberAgeBound(mapping?.AgeMin, 0);
			int num2 = NormalizeMemberAgeBound(mapping?.AgeMax, 120);
			if (num > num2)
			{
				int num3 = num;
				num = num2;
				num2 = num3;
			}
			int num4 = (int)Math.Round(hero.Age);
			return num4 >= num && num4 <= num2;
		}
		case "is_clan_leader":
			return hero.IsClanLeader;
		case "is_kingdom_leader":
			return hero.IsKingdomLeader;
		case "is_governor":
			return hero.GovernorOf != null;
		case "is_prisoner":
			return hero.IsPrisoner;
		case "is_in_settlement":
			return hero.CurrentSettlement != null || hero.StayingInSettlement != null;
		case "is_in_field":
			return hero.CurrentSettlement == null && hero.StayingInSettlement == null;
		case "is_wanderer":
			return hero.IsWanderer;
		case "is_notable":
			return hero.IsNotable;
		case "is_lord":
			return hero.IsLord;
		case "is_merchant":
			return hero.IsMerchant;
		case "is_gang_leader":
			return hero.IsGangLeader;
		case "is_artisan":
			return hero.IsArtisan;
		case "is_preacher":
			return hero.IsPreacher;
		case "is_headman":
			return hero.IsHeadman;
		case "is_minor_faction_hero":
			return hero.IsMinorFactionHero;
		case "is_party_leader":
			return hero.IsPartyLeader;
		case "is_player_companion":
			return hero.IsPlayerCompanion;
		case "is_rebel":
			return hero.IsRebel;
		case "is_wounded":
			return hero.IsWounded;
		case "is_known_to_player":
			return hero.IsKnownToPlayer;
		case "has_children":
			return HeroHasAnyChildren(hero);
		case "has_father":
			return hero.Father != null;
		case "has_mother":
			return hero.Mother != null;
		case "has_home_settlement":
			return hero.HomeSettlement != null;
		default:
			return null;
		}
	}

	private static bool? EvaluateClanStatus(Clan clan, string statusKey, LoreTextMapping mapping)
	{
		if (clan == null)
		{
			return null;
		}
		switch ((statusKey ?? "").Trim().ToLowerInvariant())
		{
		case "is_eliminated":
			return clan.IsEliminated;
		case "has_kingdom":
			return clan.Kingdom != null;
		case "has_leader":
			return clan.Leader != null;
		case "has_any_settlement":
			return ClanHasOwnedSettlement(clan, null);
		case "has_any_town":
			return ClanHasOwnedSettlement(clan, (Settlement s) => s != null && s.IsTown);
		case "has_any_castle":
			return ClanHasOwnedSettlement(clan, (Settlement s) => s != null && s.IsCastle);
		case "has_any_village":
			return ClanHasOwnedSettlement(clan, (Settlement s) => s != null && s.IsVillage);
		case "has_members":
			return GetClanLivingMembers(clan).Count > 0;
		case "has_male_members":
			return GetClanLivingMembers(clan).Any((Hero h) => h != null && !h.IsFemale);
		case "has_female_members":
			return GetClanLivingMembers(clan).Any((Hero h) => h != null && h.IsFemale);
		case "has_age_range_members":
		{
			int num = NormalizeMemberAgeBound(mapping?.AgeMin, 0);
			int num2 = NormalizeMemberAgeBound(mapping?.AgeMax, 120);
			if (num > num2)
			{
				int num3 = num;
				num = num2;
				num2 = num3;
			}
			return GetClanLivingMembers(clan).Any(delegate(Hero h)
			{
				if (h == null)
				{
					return false;
				}
				int num4 = (int)Math.Round(h.Age);
				return num4 >= num && num4 <= num2;
			});
		}
		case "is_mercenary":
			return clan.IsClanTypeMercenary || clan.IsUnderMercenaryService;
		case "is_minor_faction":
			return clan.IsMinorFaction;
		case "is_rebel_clan":
			return clan.IsRebelClan;
		case "is_noble":
			return clan.IsNoble;
		case "is_bandit_faction":
			return clan.IsBanditFaction;
		case "is_outlaw":
			return clan.IsOutlaw;
		default:
			return null;
		}
	}

	private static bool? EvaluateKingdomStatus(Kingdom kingdom, string statusKey)
	{
		if (kingdom == null)
		{
			return null;
		}
		return (statusKey ?? "").Trim().ToLowerInvariant() switch
		{
			"is_eliminated" => kingdom.IsEliminated, 
			"has_leader" => kingdom.Leader != null, 
			"has_ruling_clan" => kingdom.RulingClan != null, 
			"has_any_settlement" => KingdomHasSettlement(kingdom, null), 
			"has_any_town" => KingdomHasSettlement(kingdom, (Settlement s) => s != null && s.IsTown), 
			"has_any_castle" => KingdomHasSettlement(kingdom, (Settlement s) => s != null && s.IsCastle), 
			"has_any_village" => KingdomHasSettlement(kingdom, (Settlement s) => s != null && s.IsVillage), 
			"has_any_clan" => HasAnyItems((IEnumerable<Clan>)kingdom.Clans), 
			"has_any_lord" => HasAnyItems((IEnumerable<Hero>)kingdom.AliveLords), 
			"has_active_policies" => HasAnyItems(kingdom.ActivePolicies), 
			"has_any_war" => HasAnyItems((IEnumerable<IFaction>)kingdom.FactionsAtWarWith), 
			"has_any_allies" => HasAnyItems((IEnumerable<Kingdom>)kingdom.AlliedKingdoms), 
			_ => null, 
		};
	}

	private static bool? EvaluateSettlementStatus(Settlement settlement, string statusKey)
	{
		if (settlement == null)
		{
			return null;
		}
		return (statusKey ?? "").Trim().ToLowerInvariant() switch
		{
			"is_active" => settlement.IsActive, 
			"is_town" => settlement.IsTown, 
			"is_castle" => settlement.IsCastle, 
			"is_village" => settlement.IsVillage, 
			"is_fortification" => settlement.IsFortification, 
			"is_hideout" => settlement.IsHideout, 
			"is_under_siege" => settlement.IsUnderSiege, 
			"is_under_raid" => settlement.IsUnderRaid, 
			"is_raided" => settlement.IsRaided, 
			"is_starving" => settlement.IsStarving, 
			"is_rebellious" => settlement.InRebelliousState, 
			"has_port" => settlement.HasPort, 
			"has_owner" => settlement.Owner != null, 
			"has_owner_clan" => settlement.OwnerClan != null, 
			"has_notables" => HasAnyItems((IEnumerable<Hero>)settlement.Notables), 
			"has_parties" => HasAnyItems((IEnumerable<MobileParty>)settlement.Parties), 
			_ => null, 
		};
	}

	private static bool? EvaluateStatusMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		if (!TryParseStatusMappingKind(mapping?.Kind, out var sourceKey, out var statusKey))
		{
			return null;
		}
		return GetStatusSourceObjectKind(sourceKey) switch
		{
			"hero" => EvaluateHeroStatus(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter), statusKey, mapping), 
			"clan" => EvaluateClanStatus(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), statusKey, mapping), 
			"kingdom" => EvaluateKingdomStatus(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), statusKey), 
			"settlement" => EvaluateSettlementStatus(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter), statusKey), 
			_ => null, 
		};
	}

	private static string GetTextMappingTargetDisplayName(LoreTextMapping mapping, LoreRule rule = null, Hero npcHero = null, CharacterObject npcCharacter = null)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return "（未选择）";
			}
			if (TryParseStatusMappingKind(mapping?.Kind, out var sourceKey, out var _))
			{
				return GetStatusMappingTargetDisplayName(mapping, rule, npcHero, npcCharacter, sourceKey);
			}
			if (string.Equals(text, "__current_npc__", StringComparison.OrdinalIgnoreCase))
			{
				string heroDisplayName = GetHeroDisplayName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
				return string.IsNullOrWhiteSpace(heroDisplayName) ? "当前互动NPC" : ("当前互动NPC（" + heroDisplayName + "）");
			}
			if (string.Equals(text, "__player__", StringComparison.OrdinalIgnoreCase))
			{
				string heroDisplayName2 = GetHeroDisplayName(Hero.MainHero);
				return string.IsNullOrWhiteSpace(heroDisplayName2) ? "玩家" : ("玩家（" + heroDisplayName2 + "）");
			}
			if (string.Equals(text, "__bound_kingdom__", StringComparison.OrdinalIgnoreCase))
			{
				string kingdomDisplayName = GetKingdomDisplayName(ResolveBoundKingdomFromRule(rule));
				return string.IsNullOrWhiteSpace(kingdomDisplayName) ? "本知识绑定王国（未唯一确定）" : ("本知识绑定王国（" + kingdomDisplayName + "）");
			}
			if (string.Equals(text, "__bound_settlement__", StringComparison.OrdinalIgnoreCase))
			{
				string settlementDisplayName = GetSettlementDisplayName(ResolveBoundSettlementFromRule(rule));
				return string.IsNullOrWhiteSpace(settlementDisplayName) ? "本知识绑定定居点（未唯一确定）" : ("本知识绑定定居点（" + settlementDisplayName + "）");
			}
			if (string.Equals(text, "__bound_hero__", StringComparison.OrdinalIgnoreCase))
			{
				string heroDisplayName3 = GetHeroDisplayName(ResolveBoundHeroFromRule(rule));
				return string.IsNullOrWhiteSpace(heroDisplayName3) ? "本知识绑定英雄（未唯一确定）" : ("本知识绑定英雄（" + heroDisplayName3 + "）");
			}
			switch ((mapping?.Kind ?? "").Trim())
			{
			case "kingdom_name":
			case "kingdom_leader_name":
			case "kingdom_ruling_clan_name":
			case "kingdom_culture_name":
			case "kingdom_initial_home_settlement_name":
			case "kingdom_all_clans":
			case "kingdom_all_lords":
			case "kingdom_all_towns":
			case "kingdom_all_castles":
			case "kingdom_all_villages":
			case "kingdom_all_settlements":
			case "kingdom_active_policies":
			case "kingdom_allied_kingdoms":
			case "kingdom_war_factions":
			{
				Kingdom kingdom = FindKingdomById(text);
				string kingdomDisplayName2 = GetKingdomDisplayName(kingdom);
				return string.IsNullOrWhiteSpace(kingdomDisplayName2) ? text : kingdomDisplayName2;
			}
			case "settlement_name":
			case "settlement_owner_clan_name":
			case "settlement_owner_leader_name":
			case "settlement_owner_kingdom_name":
			case "settlement_owner_kingdom_leader_name":
			case "settlement_culture_name":
			case "settlement_notables":
			case "settlement_parties":
			case "settlement_bound_villages":
			{
				Settlement settlement = FindSettlementById(text);
				string settlementDisplayName2 = GetSettlementDisplayName(settlement);
				return string.IsNullOrWhiteSpace(settlementDisplayName2) ? text : settlementDisplayName2;
			}
			case "clan_name":
			case "clan_leader_name":
			case "clan_kingdom_name":
			case "clan_kingdom_leader_name":
			case "clan_all_towns":
			case "clan_all_villages":
			case "clan_all_settlements":
			case "clan_members":
			case "clan_male_members":
			case "clan_female_members":
			case "clan_age_range_members":
			{
				Clan clan = FindClanById(text);
				string clanDisplayName = GetClanDisplayName(clan);
				return string.IsNullOrWhiteSpace(clanDisplayName) ? text : clanDisplayName;
			}
			case "hero_name":
			case "hero_clan_name":
			case "hero_kingdom_name":
			case "hero_kingdom_leader_name":
			case "hero_spouse_name":
			case "hero_father_name":
			case "hero_mother_name":
			case "hero_current_settlement_name":
			{
				Hero hero = FindHeroById(text);
				string heroDisplayName4 = GetHeroDisplayName(hero);
				return string.IsNullOrWhiteSpace(heroDisplayName4) ? text : heroDisplayName4;
			}
			default:
				return text;
			}
		}
		catch
		{
			return (mapping?.TargetId ?? "").Trim();
		}
	}

	private static string ResolveTextMappingValue(LoreTextMapping mapping, LoreRule rule = null, Hero npcHero = null, CharacterObject npcCharacter = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace((mapping?.TargetId ?? "").Trim()))
			{
				return "";
			}
			bool? flag = EvaluateStatusMapping(mapping, rule, npcHero, npcCharacter);
			if (flag.HasValue)
			{
				return flag.Value ? GetTextMappingStatusTrueText(mapping) : GetTextMappingStatusFalseText(mapping);
			}
			switch ((mapping?.Kind ?? "").Trim())
			{
			case "kingdom_name":
			case "bound_kingdom_name":
			case "current_npc_kingdom_name":
				return GetKingdomDisplayName(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter));
			case "kingdom_leader_name":
			case "bound_kingdom_leader_name":
			case "current_npc_kingdom_leader_name":
			{
				Kingdom obj4 = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return GetHeroDisplayName((obj4 != null) ? obj4.Leader : null);
			}
			case "kingdom_ruling_clan_name":
			case "current_npc_kingdom_ruling_clan_name":
			case "bound_kingdom_ruling_clan_name":
			{
				Kingdom obj6 = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return GetClanDisplayName((obj6 != null) ? obj6.RulingClan : null);
			}
			case "kingdom_culture_name":
			case "current_npc_kingdom_culture_name":
			case "bound_kingdom_culture_name":
			{
				Kingdom obj3 = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return GetCultureDisplayName((obj3 != null) ? obj3.Culture : null);
			}
			case "kingdom_initial_home_settlement_name":
			case "current_npc_kingdom_initial_home_settlement_name":
			case "bound_kingdom_initial_home_settlement_name":
			{
				Kingdom obj10 = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return GetSettlementDisplayName((obj10 != null) ? obj10.InitialHomeSettlement : null);
			}
			case "kingdom_all_clans":
			case "current_npc_kingdom_all_clans":
			case "bound_kingdom_all_clans":
				return BuildKingdomClanListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter));
			case "kingdom_all_lords":
			case "current_npc_kingdom_all_lords":
			case "bound_kingdom_all_lords":
				return BuildKingdomLordListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter));
			case "kingdom_all_towns":
			case "current_npc_kingdom_all_towns":
			case "bound_kingdom_all_towns":
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: false, includeVillages: false);
			case "kingdom_all_castles":
			case "current_npc_kingdom_all_castles":
			case "bound_kingdom_all_castles":
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: false, includeCastles: true, includeVillages: false);
			case "kingdom_all_villages":
			case "current_npc_kingdom_all_villages":
			case "bound_kingdom_all_villages":
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: false, includeCastles: false, includeVillages: true);
			case "kingdom_all_settlements":
			case "current_npc_kingdom_all_settlements":
			case "bound_kingdom_all_settlements":
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: true, includeVillages: true);
			case "kingdom_active_policies":
			case "current_npc_kingdom_active_policies":
			case "bound_kingdom_active_policies":
			{
				Kingdom obj = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return BuildPolicyListText((obj != null) ? obj.ActivePolicies : null);
			}
			case "kingdom_allied_kingdoms":
			case "current_npc_kingdom_allied_kingdoms":
			case "bound_kingdom_allied_kingdoms":
			{
				Kingdom obj9 = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return BuildKingdomListText((IEnumerable<Kingdom>)((obj9 != null) ? obj9.AlliedKingdoms : null));
			}
			case "kingdom_war_factions":
			case "current_npc_kingdom_war_factions":
			case "bound_kingdom_war_factions":
			{
				Kingdom obj7 = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				return BuildFactionListText((IEnumerable<IFaction>)((obj7 != null) ? obj7.FactionsAtWarWith : null));
			}
			case "settlement_name":
			case "bound_settlement_name":
			case "current_npc_current_settlement_name":
				return GetSettlementDisplayName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "settlement_owner_clan_name":
			case "bound_settlement_owner_clan_name":
			case "current_npc_current_settlement_owner_clan_name":
			{
				Settlement obj5 = ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter);
				return GetClanDisplayName((obj5 != null) ? obj5.OwnerClan : null);
			}
			case "settlement_owner_leader_name":
			case "bound_settlement_owner_leader_name":
			case "current_npc_current_settlement_owner_leader_name":
			{
				Settlement obj2 = ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter);
				object hero;
				if (obj2 == null)
				{
					hero = null;
				}
				else
				{
					Clan ownerClan = obj2.OwnerClan;
					hero = ((ownerClan != null) ? ownerClan.Leader : null);
				}
				return GetHeroDisplayName((Hero)hero);
			}
			case "settlement_owner_kingdom_name":
			case "current_npc_current_settlement_owner_kingdom_name":
			case "bound_settlement_owner_kingdom_name":
				return GetSettlementKingdomName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "settlement_owner_kingdom_leader_name":
			case "current_npc_current_settlement_owner_kingdom_leader_name":
			case "bound_settlement_owner_kingdom_leader_name":
				return GetSettlementKingdomLeaderName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "settlement_culture_name":
			case "current_npc_current_settlement_culture_name":
			case "bound_settlement_culture_name":
				return GetSettlementCultureName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "settlement_notables":
			case "current_npc_current_settlement_notables":
			case "bound_settlement_notables":
				return BuildSettlementNotableListText(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "settlement_parties":
			case "current_npc_current_settlement_parties":
			case "bound_settlement_parties":
				return BuildSettlementPartyListText(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "settlement_bound_villages":
			case "current_npc_current_settlement_bound_villages":
			case "bound_settlement_bound_villages":
				return BuildSettlementBoundVillageListText(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case "clan_name":
				return GetClanDisplayName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter));
			case "clan_leader_name":
			{
				Clan obj8 = ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter);
				return GetHeroDisplayName((obj8 != null) ? obj8.Leader : null);
			}
			case "clan_kingdom_name":
			case "current_npc_clan_kingdom_name":
			case "player_clan_kingdom_name":
			case "bound_settlement_owner_clan_kingdom_name":
			case "bound_hero_clan_kingdom_name":
				return GetClanKingdomName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter));
			case "clan_kingdom_leader_name":
			case "current_npc_clan_kingdom_leader_name":
			case "player_clan_kingdom_leader_name":
			case "bound_settlement_owner_clan_kingdom_leader_name":
			case "bound_hero_clan_kingdom_leader_name":
				return GetClanKingdomLeaderName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter));
			case "clan_all_towns":
			case "current_npc_clan_all_towns":
			case "player_clan_all_towns":
			case "bound_settlement_owner_clan_all_towns":
			case "bound_hero_clan_all_towns":
				return BuildClanOwnedSettlementListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: false, includeVillages: false);
			case "clan_all_villages":
			case "current_npc_clan_all_villages":
			case "player_clan_all_villages":
			case "bound_settlement_owner_clan_all_villages":
			case "bound_hero_clan_all_villages":
				return BuildClanOwnedSettlementListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: false, includeCastles: false, includeVillages: true);
			case "clan_all_settlements":
			case "current_npc_clan_all_settlements":
			case "player_clan_all_settlements":
			case "bound_settlement_owner_clan_all_settlements":
			case "bound_hero_clan_all_settlements":
				return BuildClanOwnedSettlementListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: true, includeVillages: true);
			case "clan_members":
			case "current_npc_clan_members":
			case "player_clan_members":
			case "bound_settlement_owner_clan_members":
			case "bound_hero_clan_members":
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), (Hero h) => true);
			case "clan_male_members":
			case "current_npc_clan_male_members":
			case "player_clan_male_members":
			case "bound_settlement_owner_clan_male_members":
			case "bound_hero_clan_male_members":
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), (Hero h) => h != null && !h.IsFemale);
			case "clan_female_members":
			case "current_npc_clan_female_members":
			case "player_clan_female_members":
			case "bound_settlement_owner_clan_female_members":
			case "bound_hero_clan_female_members":
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), (Hero h) => h != null && h.IsFemale);
			case "clan_age_range_members":
			case "current_npc_clan_age_range_members":
			case "player_clan_age_range_members":
			case "bound_settlement_owner_clan_age_range_members":
			case "bound_hero_clan_age_range_members":
			{
				int num = NormalizeMemberAgeBound(mapping?.AgeMin, 0);
				int num2 = NormalizeMemberAgeBound(mapping?.AgeMax, 120);
				if (num > num2)
				{
					int num3 = num;
					num = num2;
					num2 = num3;
				}
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), delegate(Hero h)
				{
					if (h == null)
					{
						return false;
					}
					int num4 = (int)Math.Round(h.Age);
					return num4 >= num && num4 <= num2;
				});
			}
			case "hero_name":
			case "current_npc_name":
			case "player_name":
			case "bound_hero_name":
				return GetHeroDisplayName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_clan_name":
			case "current_npc_clan_name":
			case "player_clan_name":
			case "bound_hero_clan_name":
				return GetHeroClanName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_kingdom_name":
			case "player_kingdom_name":
			case "bound_hero_kingdom_name":
				return GetHeroKingdomName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_kingdom_leader_name":
			case "player_kingdom_leader_name":
			case "bound_hero_kingdom_leader_name":
				return GetHeroKingdomLeaderName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_spouse_name":
			case "current_npc_spouse_name":
			case "player_spouse_name":
			case "bound_hero_spouse_name":
				return GetHeroSpouseName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_father_name":
			case "current_npc_father_name":
			case "bound_hero_father_name":
				return GetHeroFatherName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_mother_name":
			case "current_npc_mother_name":
			case "bound_hero_mother_name":
				return GetHeroMotherName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case "hero_current_settlement_name":
			case "player_current_settlement_name":
			case "bound_hero_current_settlement_name":
				return GetHeroCurrentSettlementName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			default:
				return "";
			}
		}
		catch
		{
			return "";
		}
	}

	private static string GetTextMappingEmptyValueText(LoreTextMapping mapping)
	{
		try
		{
			return (mapping?.EmptyValueText ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveTextMappingValueOrFallback(LoreTextMapping mapping, LoreRule rule = null, Hero npcHero = null, CharacterObject npcCharacter = null)
	{
		string text = ResolveTextMappingValue(mapping, rule, npcHero, npcCharacter);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return GetTextMappingEmptyValueText(mapping);
	}

	private static string BuildTextMappingSummary(LoreRule rule, LoreTextMapping mapping)
	{
		string text = TrimPreview((mapping?.SourceText ?? "").Trim(), 28);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "（空）";
		}
		string text2 = GetTextMappingKindLabel(mapping?.Kind);
		string text3 = TrimPreview(GetTextMappingTargetDisplayName(mapping, rule), 24);
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "（未选择）";
		}
		string text4 = TrimPreview(ResolveTextMappingValueOrFallback(mapping, rule), 24);
		if (string.IsNullOrWhiteSpace(text4))
		{
			text4 = "（当前无值）";
		}
		string text5 = TrimPreview(GetTextMappingEmptyValueText(mapping), 18);
		if (IsTextMappingAgeRangeKind(mapping?.Kind))
		{
			int num = NormalizeMemberAgeBound(mapping?.AgeMin, 0);
			int num2 = NormalizeMemberAgeBound(mapping?.AgeMax, 120);
			text2 += $"（{num}-{num2}岁）";
		}
		if (IsStatusMappingKind(mapping?.Kind))
		{
			string text6 = TrimPreview(GetTextMappingStatusTrueText(mapping), 10);
			string text7 = TrimPreview(GetTextMappingStatusFalseText(mapping), 10);
			text2 = text2 + "（真:" + (string.IsNullOrWhiteSpace(text6) ? "空" : text6) + "/假:" + (string.IsNullOrWhiteSpace(text7) ? "空" : text7) + "）";
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			text4 = text4 + " [空=>" + text5 + "]";
		}
		return text + " -> " + text2 + "（" + text3 + "） => " + text4;
	}

	private string ApplyRuleTextMappings(LoreRule rule, string content, Hero npcHero = null, CharacterObject npcCharacter = null)
	{
		string text = content ?? "";
		try
		{
			if (string.IsNullOrEmpty(text) || rule == null)
			{
				return text;
			}
			EnsureTextMappings(rule);
			if (rule.TextMappings == null || rule.TextMappings.Count == 0)
			{
				return text;
			}
			int num = Math.Min(Math.Max(2, (rule.TextMappings?.Count ?? 0) + 1), 6);
			for (int i = 0; i < num; i++)
			{
				bool flag = false;
				foreach (LoreTextMapping textMapping in rule.TextMappings)
				{
					string text2 = (textMapping?.SourceText ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text2))
					{
						continue;
					}
					string text3 = ResolveTextMappingValueOrFallback(textMapping, rule, npcHero, npcCharacter);
					if (!string.IsNullOrWhiteSpace(text3))
					{
						string text4 = text.Replace(text2, text3);
						if (!string.Equals(text4, text, StringComparison.Ordinal))
						{
							text = text4;
							flag = true;
						}
					}
				}
				if (!flag)
				{
					break;
				}
			}
		}
		catch
		{
		}
		return text;
	}

	public string BuildLoreContext(string inputText, Hero npcHero, string secondaryInput = null)
	{
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		string text = (inputText ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		if (npcHero == null)
		{
			LogLoreContextTrace("hero", "", "", "neutral", "", "", "commoner", isFemale: false, isClanLeader: false, "", text, invalidContext: true);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=invalid_context source=hero inputLen={text.Length}", secondaryInput), text);
			}
			catch
			{
			}
			return "";
		}
		string text2 = (((npcHero != null) ? ((MBObjectBase)npcHero).StringId : null) ?? "").Trim();
		object obj2;
		if (npcHero == null)
		{
			obj2 = null;
		}
		else
		{
			CultureObject culture = npcHero.Culture;
			obj2 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
		}
		if (obj2 == null)
		{
			obj2 = "neutral";
		}
		string text3 = ((string)obj2).Trim().ToLower();
		string text4 = "";
		try
		{
			object obj3;
			if (npcHero == null)
			{
				obj3 = null;
			}
			else
			{
				Clan clan = npcHero.Clan;
				if (clan == null)
				{
					obj3 = null;
				}
				else
				{
					Kingdom kingdom = clan.Kingdom;
					obj3 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
				}
			}
			if (obj3 == null)
			{
				if (npcHero == null)
				{
					obj3 = null;
				}
				else
				{
					IFaction mapFaction = npcHero.MapFaction;
					obj3 = ((mapFaction != null) ? mapFaction.StringId : null);
				}
				if (obj3 == null)
				{
					obj3 = "";
				}
			}
			text4 = ((string)obj3).Trim().ToLower();
		}
		catch
		{
			text4 = "";
		}
		string text5 = "commoner";
		try
		{
			if (npcHero != null)
			{
				text5 = (npcHero.IsLord ? "lord" : ((!npcHero.IsNotable) ? RoleFromOccupation(npcHero.Occupation) : "notable"));
			}
		}
		catch
		{
		}
		bool flag = false;
		try
		{
			flag = npcHero != null && npcHero.IsFemale;
		}
		catch
		{
		}
		bool flag2 = false;
		try
		{
			flag2 = npcHero != null && npcHero.Clan != null && npcHero.Clan.Leader == npcHero;
		}
		catch
		{
		}
		string text6 = "";
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			object obj8 = ((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null);
			if (obj8 == null)
			{
				if (npcHero == null)
				{
					obj8 = null;
				}
				else
				{
					Settlement currentSettlement2 = npcHero.CurrentSettlement;
					obj8 = ((currentSettlement2 != null) ? ((MBObjectBase)currentSettlement2).StringId : null);
				}
				if (obj8 == null)
				{
					obj8 = "";
				}
			}
			text6 = ((string)obj8).Trim().ToLowerInvariant();
		}
		catch
		{
			text6 = "";
		}
		string text7 = "";
		try
		{
			text7 = (((npcHero == null) ? null : ((object)npcHero.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			text7 = "";
		}
		if (string.IsNullOrWhiteSpace(text7))
		{
			text7 = "该NPC";
		}
		string text8 = (text7 ?? "").Replace("|", " ").Trim();
		LogLoreContextTrace("hero", text2, "", text3, text4, text6, text5, flag, flag2, "", text);
		long ruleDataVersion = _ruleDataVersion;
		bool flag3 = !HasAnyTextMappings();
		string key = Hash8($"{ruleDataVersion}|H|{text2}|{text8}|{text3}|{text4}|{text6}|{text5}|{(flag ? 1 : 0)}|{(flag2 ? 1 : 0)}|{text}");
		if (flag3 && TryGetLoreContextCache(key, ruleDataVersion, out var value))
		{
			return value;
		}
		int num = 0;
		try
		{
			num = (_file?.Rules?.Count).GetValueOrDefault();
		}
		catch
		{
			num = 0;
		}
		if (num <= 0)
		{
			LogLoreMissOnce("rules_empty", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=rules_empty rules={num} inputLen={text.Length} mode=none", secondaryInput), text);
			}
			catch
			{
			}
			return "";
		}
		bool flag4 = false;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		CandidateRules candidateRules = CollectCandidateRules(text, secondaryInput);
		int num3 = candidateRules?.InjectLimit ?? GetLoreInjectLimit(GetKnowledgeReturnCap());
		string text9 = candidateRules?.MatchMode ?? "none";
		List<LoreRule> list = candidateRules?.OrderedRules;
		if (list == null || list.Count == 0)
		{
			LogLoreMissOnce("rule_miss", text, num, text2, text3, text4, text5);
			if (flag3)
			{
				PutLoreContextCache(key, ruleDataVersion, "");
			}
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=rule_miss rules={num} inputLen={text.Length} mode={text9}", secondaryInput), text);
			}
			catch
			{
			}
			return "";
		}
		foreach (LoreRule item in list)
		{
			if (num2 >= num3)
			{
				break;
			}
			if (item == null)
			{
				continue;
			}
			string text10 = "";
			try
			{
				text10 = item?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text10 = "";
			}
			try
			{
				Logger.Log("LoreMatch", "knowledge_hit rule=" + item.Id + " mode=" + text9 + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " settlement=" + text6 + " role=" + text5);
			}
			catch
			{
			}
			LoreVariant loreVariant = PickBestVariant(item, npcHero, null, text2, text3, text4, text6, text5, flag, flag2);
			if (loreVariant == null)
			{
				try
				{
					Logger.Log("LoreMatch", "variant_miss rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, BuildKnowledgeHitRateDetail("reason=variant_miss mode=" + text9, secondaryInput), text);
				}
				catch
				{
				}
				continue;
			}
			string value2 = ApplyRuleTextMappings(item, loreVariant.Content ?? "", npcHero).Trim();
			if (string.IsNullOrEmpty(value2))
			{
				try
				{
					Logger.Log("LoreMatch", "content_empty rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, BuildKnowledgeHitRateDetail("reason=content_empty mode=" + text9, secondaryInput), text);
				}
				catch
				{
				}
				continue;
			}
			try
			{
				Logger.Log("LoreMatch", "variant_hit rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
			}
			catch
			{
			}
			try
			{
				Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: true, BuildKnowledgeHitRateDetail("reason=variant_hit mode=" + text9, secondaryInput), text);
			}
			catch
			{
			}
			num2++;
			if (!flag4)
			{
				stringBuilder.AppendLine(" ");
				stringBuilder.AppendLine("参与互动让你的脑海里浮现了这些知识");
				flag4 = true;
			}
			string text11 = (string.IsNullOrWhiteSpace(text10) ? (item.Id ?? "相关语义") : text10);
			stringBuilder.AppendLine("【以下是关于（" + text11 + "）的背景知识，" + text7 + "可酌情参考作为聊天素材】");
			stringBuilder.AppendLine(value2);
		}
		string text12 = ((num2 > 0) ? stringBuilder.ToString() : "");
		if (num2 == 0)
		{
			LogLoreMissOnce("variant_or_content_miss", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=variant_or_content_miss candidates={list?.Count ?? 0} mode={text9} inputLen={text.Length}", secondaryInput), text);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: true, BuildKnowledgeHitRateDetail($"reason=ok matched={num2} candidates={list?.Count ?? 0} mode={text9} inputLen={text.Length}", secondaryInput), text);
			}
			catch
			{
			}
		}
		if (flag3)
		{
			PutLoreContextCache(key, ruleDataVersion, text12);
		}
		return text12;
	}

	private static string RoleFromOccupation(Occupation occ)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected I4, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		if (1 == 0)
		{
		}
		string result;
		switch (occ - 3)
		{
		default:
			if ((int)occ != 16)
			{
				goto case 1;
			}
			result = "wanderer";
			break;
		case 4:
			result = "soldier";
			break;
		case 3:
			result = "villager";
			break;
		case 5:
			result = "townsfolk";
			break;
		case 0:
			result = "lord";
			break;
		case 1:
		case 2:
			result = "commoner";
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetRoleKeyForHero(Hero hero)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (hero == null)
			{
				return "commoner";
			}
			if (hero.IsLord)
			{
				return "lord";
			}
			if (hero.IsNotable)
			{
				return "notable";
			}
			if (hero.IsWanderer)
			{
				return "wanderer";
			}
			return RoleFromOccupation(hero.Occupation);
		}
		catch
		{
			return "commoner";
		}
	}

	private static string GetRoleKeyForCharacter(CharacterObject character)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (character == null)
			{
				return "commoner";
			}
			Hero heroObject = character.HeroObject;
			if (heroObject != null)
			{
				return GetRoleKeyForHero(heroObject);
			}
			if (((BasicCharacterObject)character).IsSoldier)
			{
				return "soldier";
			}
			return RoleFromOccupation(character.Occupation);
		}
		catch
		{
			return "commoner";
		}
	}

	private static string GetCurrentCharacterId(Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			string text = (((npcCharacter != null) ? ((MBObjectBase)npcCharacter).StringId : null) ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			object obj;
			if (npcHero == null)
			{
				obj = null;
			}
			else
			{
				CharacterObject characterObject = npcHero.CharacterObject;
				obj = ((characterObject != null) ? ((MBObjectBase)characterObject).StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			return ((string)obj).Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string EncodeRoleIdentityHeroId(string heroId)
	{
		string text = (heroId ?? "").Trim();
		return string.IsNullOrEmpty(text) ? "" : ("hero:" + text.ToLowerInvariant());
	}

	private static string EncodeRoleIdentityCharacterId(string characterId)
	{
		string text = (characterId ?? "").Trim();
		return string.IsNullOrEmpty(text) ? "" : ("char:" + text.ToLowerInvariant());
	}

	private static bool TryParseRoleIdentityId(string encodedId, out string kind, out string targetId)
	{
		kind = "";
		targetId = "";
		string text = (encodedId ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		int num = text.IndexOf(':');
		if (num <= 0 || num >= text.Length - 1)
		{
			return false;
		}
		kind = text.Substring(0, num).Trim().ToLowerInvariant();
		targetId = text.Substring(num + 1).Trim();
		return !string.IsNullOrEmpty(kind) && !string.IsNullOrEmpty(targetId);
	}

	private static string GetRoleDisplayName(string roleKey)
	{
		string text = (roleKey ?? "").Trim().ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"lord" => "领主", 
			"notable" => "要人", 
			"wanderer" => "流浪者", 
			"soldier" => "士兵", 
			"villager" => "村民", 
			"townsfolk" => "镇民", 
			"commoner" => "未分类对象", 
			_ => string.IsNullOrWhiteSpace(roleKey) ? "未知" : roleKey, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string[] GetRoleCategoryKeys()
	{
		return new string[7] { "lord", "notable", "wanderer", "soldier", "villager", "townsfolk", "commoner" };
	}

	private static bool ListContainsIgnoreCase(List<string> list, string value)
	{
		if (list == null || list.Count == 0)
		{
			return false;
		}
		string text = (value ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		return list.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

	private static void ToggleListValueIgnoreCase(List<string> list, string value)
	{
		if (list == null)
		{
			return;
		}
		string text = (value ?? "").Trim();
		if (!string.IsNullOrEmpty(text))
		{
			int num = list.FindIndex((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			if (num >= 0)
			{
				list.RemoveAt(num);
			}
			else
			{
				list.Add(text);
			}
		}
	}

	public string BuildLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride = null, string secondaryInput = null)
	{
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		string text = (inputText ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		Hero val = null;
		try
		{
			val = ((npcCharacter != null && ((BasicCharacterObject)npcCharacter).IsHero) ? npcCharacter.HeroObject : null);
		}
		catch
		{
			val = null;
		}
		string text2 = (((val != null) ? ((MBObjectBase)val).StringId : null) ?? "").Trim();
		string charId = (((npcCharacter != null) ? ((MBObjectBase)npcCharacter).StringId : null) ?? "").Trim();
		if (val == null && npcCharacter == null)
		{
			LogLoreContextTrace("character", "", "", "neutral", "", "", "commoner", isFemale: false, isClanLeader: false, kingdomIdOverride, text, invalidContext: true);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=invalid_context source=character inputLen={text.Length}", secondaryInput), text);
			}
			catch
			{
			}
			return "";
		}
		object obj3;
		if (val == null)
		{
			obj3 = null;
		}
		else
		{
			CultureObject culture = val.Culture;
			obj3 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
		}
		if (obj3 == null)
		{
			if (npcCharacter == null)
			{
				obj3 = null;
			}
			else
			{
				CultureObject culture2 = npcCharacter.Culture;
				obj3 = ((culture2 != null) ? ((MBObjectBase)culture2).StringId : null);
			}
			if (obj3 == null)
			{
				obj3 = "neutral";
			}
		}
		string text3 = ((string)obj3).Trim().ToLower();
		string text4 = (kingdomIdOverride ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				object obj4;
				if (val == null)
				{
					obj4 = null;
				}
				else
				{
					Clan clan = val.Clan;
					if (clan == null)
					{
						obj4 = null;
					}
					else
					{
						Kingdom kingdom = clan.Kingdom;
						obj4 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
					}
				}
				if (obj4 == null)
				{
					if (val == null)
					{
						obj4 = null;
					}
					else
					{
						IFaction mapFaction = val.MapFaction;
						obj4 = ((mapFaction != null) ? mapFaction.StringId : null);
					}
					if (obj4 == null)
					{
						obj4 = "";
					}
				}
				text4 = ((string)obj4).Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				MobileParty conversationParty = MobileParty.ConversationParty;
				object obj6;
				if (conversationParty == null)
				{
					obj6 = null;
				}
				else
				{
					IFaction mapFaction2 = conversationParty.MapFaction;
					obj6 = ((mapFaction2 != null) ? mapFaction2.StringId : null);
				}
				if (obj6 == null)
				{
					obj6 = "";
				}
				text4 = ((string)obj6).Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				object obj8;
				if (currentSettlement == null)
				{
					obj8 = null;
				}
				else
				{
					Clan ownerClan = currentSettlement.OwnerClan;
					if (ownerClan == null)
					{
						obj8 = null;
					}
					else
					{
						Kingdom kingdom2 = ownerClan.Kingdom;
						obj8 = ((kingdom2 != null) ? ((MBObjectBase)kingdom2).StringId : null);
					}
				}
				if (obj8 == null)
				{
					Settlement currentSettlement2 = Settlement.CurrentSettlement;
					if (currentSettlement2 == null)
					{
						obj8 = null;
					}
					else
					{
						IFaction mapFaction3 = currentSettlement2.MapFaction;
						obj8 = ((mapFaction3 != null) ? mapFaction3.StringId : null);
					}
					if (obj8 == null)
					{
						obj8 = "";
					}
				}
				text4 = ((string)obj8).Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		string text5 = "commoner";
		try
		{
			if (val != null)
			{
				text5 = (val.IsLord ? "lord" : ((!val.IsNotable) ? RoleFromOccupation(val.Occupation) : "notable"));
			}
			else if (npcCharacter != null)
			{
				text5 = ((!((BasicCharacterObject)npcCharacter).IsSoldier) ? RoleFromOccupation(npcCharacter.Occupation) : "soldier");
			}
		}
		catch
		{
		}
		bool flag = false;
		try
		{
			flag = ((val != null) ? val.IsFemale : (npcCharacter != null && ((BasicCharacterObject)npcCharacter).IsFemale));
		}
		catch
		{
		}
		bool flag2 = false;
		try
		{
			flag2 = val != null && val.Clan != null && val.Clan.Leader == val;
		}
		catch
		{
		}
		string text6 = "";
		try
		{
			Settlement currentSettlement3 = Settlement.CurrentSettlement;
			object obj13 = ((currentSettlement3 != null) ? ((MBObjectBase)currentSettlement3).StringId : null);
			if (obj13 == null)
			{
				if (val == null)
				{
					obj13 = null;
				}
				else
				{
					Settlement currentSettlement4 = val.CurrentSettlement;
					obj13 = ((currentSettlement4 != null) ? ((MBObjectBase)currentSettlement4).StringId : null);
				}
				if (obj13 == null)
				{
					obj13 = "";
				}
			}
			text6 = ((string)obj13).Trim().ToLowerInvariant();
		}
		catch
		{
			text6 = "";
		}
		string text7 = "";
		try
		{
			text7 = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
		}
		catch
		{
			text7 = "";
		}
		if (string.IsNullOrWhiteSpace(text7))
		{
			try
			{
				text7 = (((npcCharacter == null) ? null : ((object)((BasicCharacterObject)npcCharacter).Name)?.ToString()) ?? "").Trim();
			}
			catch
			{
				text7 = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text7))
		{
			text7 = "该NPC";
		}
		string text8 = (text7 ?? "").Replace("|", " ").Trim();
		LogLoreContextTrace((val != null) ? "character_hero" : "character", text2, charId, text3, text4, text6, text5, flag, flag2, kingdomIdOverride, text);
		long ruleDataVersion = _ruleDataVersion;
		bool flag3 = !HasAnyTextMappings();
		string key = Hash8($"{ruleDataVersion}|C|{text2}|{text8}|{text3}|{text4}|{text6}|{text5}|{(flag ? 1 : 0)}|{(flag2 ? 1 : 0)}|{text}");
		if (flag3 && TryGetLoreContextCache(key, ruleDataVersion, out var value))
		{
			return value;
		}
		int num = 0;
		try
		{
			num = (_file?.Rules?.Count).GetValueOrDefault();
		}
		catch
		{
			num = 0;
		}
		if (num <= 0)
		{
			LogLoreMissOnce("rules_empty", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=rules_empty rules={num} inputLen={text.Length} mode=none", secondaryInput), text);
			}
			catch
			{
			}
			return "";
		}
		bool flag4 = false;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		CandidateRules candidateRules = CollectCandidateRules(text, secondaryInput);
		int num3 = candidateRules?.InjectLimit ?? GetLoreInjectLimit(GetKnowledgeReturnCap());
		string text9 = candidateRules?.MatchMode ?? "none";
		List<LoreRule> list = candidateRules?.OrderedRules;
		if (list == null || list.Count == 0)
		{
			LogLoreMissOnce("rule_miss", text, num, text2, text3, text4, text5);
			if (flag3)
			{
				PutLoreContextCache(key, ruleDataVersion, "");
			}
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=rule_miss rules={num} inputLen={text.Length} mode={text9}", secondaryInput), text);
			}
			catch
			{
			}
			return "";
		}
		foreach (LoreRule item in list)
		{
			if (num2 >= num3)
			{
				break;
			}
			if (item == null)
			{
				continue;
			}
			string text10 = "";
			try
			{
				text10 = item?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text10 = "";
			}
			try
			{
				Logger.Log("LoreMatch", "knowledge_hit rule=" + item.Id + " mode=" + text9 + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " settlement=" + text6 + " role=" + text5);
			}
			catch
			{
			}
			LoreVariant loreVariant = PickBestVariant(item, val, npcCharacter, text2, text3, text4, text6, text5, flag, flag2);
			if (loreVariant == null)
			{
				try
				{
					Logger.Log("LoreMatch", "variant_miss rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, BuildKnowledgeHitRateDetail("reason=variant_miss mode=" + text9, secondaryInput), text);
				}
				catch
				{
				}
				continue;
			}
			string value2 = ApplyRuleTextMappings(item, loreVariant.Content ?? "", val, npcCharacter).Trim();
			if (string.IsNullOrEmpty(value2))
			{
				try
				{
					Logger.Log("LoreMatch", "content_empty rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
				}
				catch
				{
				}
				try
				{
					Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: false, BuildKnowledgeHitRateDetail("reason=content_empty mode=" + text9, secondaryInput), text);
				}
				catch
				{
				}
				continue;
			}
			try
			{
				Logger.Log("LoreMatch", "variant_hit rule=" + item.Id + " hero=" + text2 + " culture=" + text3 + " kingdom=" + text4 + " role=" + text5);
			}
			catch
			{
			}
			try
			{
				Logger.RecordHitRate("knowledge", item.Id ?? "__unknown__", hit: true, BuildKnowledgeHitRateDetail("reason=variant_hit mode=" + text9, secondaryInput), text);
			}
			catch
			{
			}
			num2++;
			if (!flag4)
			{
				stringBuilder.AppendLine(" ");
				stringBuilder.AppendLine("参与互动让你的脑海里浮现了这些知识");
				flag4 = true;
			}
			string text11 = (string.IsNullOrWhiteSpace(text10) ? (item.Id ?? "相关语义") : text10);
			stringBuilder.AppendLine("【以下是关于（" + text11 + "）的背景知识，" + text7 + "可酌情参考作为聊天素材");
			stringBuilder.AppendLine(value2);
		}
		string text12 = ((num2 > 0) ? stringBuilder.ToString() : "");
		if (num2 == 0)
		{
			LogLoreMissOnce("variant_or_content_miss", text, num, text2, text3, text4, text5);
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail($"reason=variant_or_content_miss candidates={list?.Count ?? 0} mode={text9} inputLen={text.Length}", secondaryInput), text);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: true, BuildKnowledgeHitRateDetail($"reason=ok matched={num2} candidates={list?.Count ?? 0} mode={text9} inputLen={text.Length}", secondaryInput), text);
			}
			catch
			{
			}
		}
		if (flag3)
		{
			PutLoreContextCache(key, ruleDataVersion, text12);
		}
		return text12;
	}

	public string ExportRulesJson(bool pretty = false)
	{
		try
		{
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			return JsonConvert.SerializeObject((object)_file, (Formatting)(pretty ? 1 : 0));
		}
		catch
		{
			return "";
		}
	}

	private static int CountValidRagShortTexts(LoreRule rule)
	{
		int num = 0;
		try
		{
			if (rule?.RagShortTexts == null)
			{
				return 0;
			}
			for (int i = 0; i < rule.RagShortTexts.Count; i++)
			{
				if (!string.IsNullOrWhiteSpace(NormalizeKeywordForCompare(rule.RagShortTexts[i])))
				{
					num++;
				}
			}
		}
		catch
		{
			num = 0;
		}
		return num;
	}

	private static bool TryValidateRagShortTexts(LoreRule rule, bool requireAtLeastOne, out string error)
	{
		error = "";
		try
		{
			if (rule == null)
			{
				error = "知识条目为空。";
				return false;
			}
			int num = 0;
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text = NormalizeKeywordForCompare(rule.RagShortTexts[i]);
					if (!string.IsNullOrWhiteSpace(text))
					{
						num++;
						if (text.Length > 100)
						{
							string ruleDisplayNameForExport = GetRuleDisplayNameForExport(rule);
							string text2 = (rule.Id ?? "").Trim();
							error = (string.IsNullOrEmpty(text2) ? ("知识条目“" + ruleDisplayNameForExport + "”存在超过 " + 100 + " 字符的 RAG专用短句，请先缩短后再继续。") : ("知识条目“" + ruleDisplayNameForExport + "”（ID=" + text2 + "）存在超过 " + 100 + " 字符的 RAG专用短句，请先缩短后再继续。"));
							return false;
						}
					}
				}
			}
			if (requireAtLeastOne && num <= 0)
			{
				string ruleDisplayNameForExport2 = GetRuleDisplayNameForExport(rule);
				string text3 = (rule.Id ?? "").Trim();
				error = (string.IsNullOrEmpty(text3) ? ("知识条目“" + ruleDisplayNameForExport2 + "”缺少 RAG专用短句，请先填写后再导出。") : ("知识条目“" + ruleDisplayNameForExport2 + "”（ID=" + text3 + "）缺少 RAG专用短句，请先填写后再导出。"));
				return false;
			}
		}
		catch (Exception ex)
		{
			error = "校验 RAG专用短句失败：" + ex.Message;
			return false;
		}
		return true;
	}

	private static string GetRuleDisplayNameForExport(LoreRule rule)
	{
		try
		{
			string text = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		catch
		{
		}
		return (rule?.Id ?? "未命名知识").Trim();
	}

	private static bool TryValidateRuleForExport(LoreRule rule, out string error)
	{
		error = "";
		try
		{
			return TryValidateRagShortTexts(rule, requireAtLeastOne: true, out error);
		}
		catch (Exception ex)
		{
			error = "校验知识导出内容失败：" + ex.Message;
			return false;
		}
	}

	public bool TryValidateKnowledgeExport(out string error)
	{
		error = "";
		try
		{
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			foreach (LoreRule rule in _file.Rules)
			{
				if (rule != null && !TryValidateRuleForExport(rule, out error))
				{
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			error = "校验知识导出内容失败：" + ex.Message;
			return false;
		}
		return true;
	}

	public bool TryValidateSingleRuleExport(string ruleId, out string error)
	{
		error = "";
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				error = "RuleId 为空。";
				return false;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			LoreRule loreRule = _file.Rules.FirstOrDefault((LoreRule r) => r != null && string.Equals((r.Id ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			if (loreRule == null)
			{
				error = "找不到该知识条目：" + text;
				return false;
			}
			return TryValidateRuleForExport(loreRule, out error);
		}
		catch (Exception ex)
		{
			error = "校验单条知识导出内容失败：" + ex.Message;
			return false;
		}
	}

	public List<string> GetRuleIdsForDev(int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			foreach (string item in (from r in _file.Rules
				where r != null
				select (r.Id ?? "").Trim() into id
				where !string.IsNullOrEmpty(id)
				select id).OrderBy((string id) => id, StringComparer.OrdinalIgnoreCase))
			{
				if (list.Count >= maxCount)
				{
					break;
				}
				list.Add(item);
			}
		}
		catch
		{
		}
		return list;
	}

	public string ExportSingleRuleJson(string ruleId, bool pretty = false)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(id))
			{
				return "";
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			LoreRule loreRule = _file.Rules.FirstOrDefault((LoreRule r) => r != null && string.Equals((r.Id ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
			if (loreRule == null)
			{
				return "";
			}
			return JsonConvert.SerializeObject((object)loreRule, (Formatting)(pretty ? 1 : 0));
		}
		catch
		{
			return "";
		}
	}

	public bool ImportSingleRuleJson(string json, bool overwrite = true)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}
			LoreRule loreRule = JsonConvert.DeserializeObject<LoreRule>(json);
			if (loreRule == null)
			{
				return false;
			}
			string text = (loreRule.Id ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			loreRule.Id = text;
			if (!TryValidateRagShortTexts(loreRule, requireAtLeastOne: false, out var _))
			{
				return false;
			}
			if (!ValidateVariantConditionsUnique(loreRule, out var _, out var _))
			{
				return false;
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			int num = -1;
			for (int i = 0; i < _file.Rules.Count; i++)
			{
				LoreRule loreRule2 = _file.Rules[i];
				if (loreRule2 != null && string.Equals((loreRule2.Id ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				if (!overwrite)
				{
					return false;
				}
				_file.Rules[num] = loreRule;
			}
			else
			{
				_file.Rules.Add(loreRule);
			}
			try
			{
				_storageJson = JsonConvert.SerializeObject((object)_file, (Formatting)0);
			}
			catch
			{
			}
			TouchRuleData();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool ImportRulesJson(string json, bool overwrite = true)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}
			KnowledgeFile knowledgeFile = JsonConvert.DeserializeObject<KnowledgeFile>(json);
			if (knowledgeFile == null)
			{
				return false;
			}
			if (knowledgeFile.Rules == null)
			{
				knowledgeFile.Rules = new List<LoreRule>();
			}
			foreach (LoreRule rule in knowledgeFile.Rules)
			{
				if (rule != null && (!TryValidateRagShortTexts(rule, requireAtLeastOne: false, out var _) || !ValidateVariantConditionsUnique(rule, out var _, out var _)))
				{
					return false;
				}
			}
			if (_file == null)
			{
				_file = new KnowledgeFile();
			}
			if (_file.Rules == null)
			{
				_file.Rules = new List<LoreRule>();
			}
			if (overwrite)
			{
				_file = knowledgeFile;
			}
			else
			{
				foreach (LoreRule r in knowledgeFile.Rules)
				{
					if (r != null)
					{
						if (string.IsNullOrWhiteSpace(r.Id))
						{
							r.Id = "import_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
						}
						if (!_file.Rules.Any((LoreRule x) => x != null && string.Equals(x.Id, r.Id, StringComparison.OrdinalIgnoreCase)))
						{
							_file.Rules.Add(r);
						}
					}
				}
			}
			StripSemanticPrototypes();
			try
			{
				_storageJson = JsonConvert.SerializeObject((object)_file, (Formatting)0);
			}
			catch
			{
			}
			TouchRuleData();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool ValidateVariantConditionsUnique(LoreRule rule, out int firstIndex, out int secondIndex)
	{
		firstIndex = -1;
		secondIndex = -1;
		try
		{
			if (rule?.Variants == null || rule.Variants.Count <= 1)
			{
				return true;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = 0; i < rule.Variants.Count; i++)
			{
				LoreVariant loreVariant = rule.Variants[i];
				if (loreVariant != null)
				{
					string key = BuildWhenSignature(loreVariant.When);
					if (dictionary.TryGetValue(key, out var value))
					{
						firstIndex = value;
						secondIndex = i;
						return false;
					}
					dictionary[key] = i;
				}
			}
		}
		catch
		{
		}
		return true;
	}

	private static int FindDuplicateVariantIndex(LoreRule rule, LoreWhen when, int excludeIndex = -1)
	{
		try
		{
			if (rule?.Variants == null || rule.Variants.Count <= 0)
			{
				return -1;
			}
			string b = BuildWhenSignature(when);
			for (int i = 0; i < rule.Variants.Count; i++)
			{
				if (i != excludeIndex)
				{
					LoreVariant loreVariant = rule.Variants[i];
					if (loreVariant != null && string.Equals(BuildWhenSignature(loreVariant.When), b, StringComparison.Ordinal))
					{
						return i;
					}
				}
			}
		}
		catch
		{
		}
		return -1;
	}

	private static string BuildWhenSignature(LoreWhen when)
	{
		try
		{
			LoreWhen loreWhen = NormalizeWhenForStorage(CloneWhen(when));
			if (loreWhen == null)
			{
				return "__generic__";
			}
			string text = string.Join("|", NormalizeWhenStringList(loreWhen.HeroIds));
			string text2 = string.Join("|", NormalizeWhenStringList(loreWhen.Cultures));
			string text3 = string.Join("|", NormalizeWhenStringList(loreWhen.KingdomIds));
			string text4 = string.Join("|", NormalizeWhenStringList(loreWhen.SettlementIds));
			string text5 = string.Join("|", NormalizeWhenStringList(loreWhen.Roles));
			string text6 = string.Join("|", NormalizeWhenStringList(loreWhen.IdentityIds));
			string text7 = ((!loreWhen.IsFemale.HasValue) ? "any" : (loreWhen.IsFemale.Value ? "female" : "male"));
			string text8 = ((!loreWhen.IsClanLeader.HasValue) ? "any" : (loreWhen.IsClanLeader.Value ? "leader" : "not_leader"));
			string text9 = string.Join("|", from kv in NormalizeWhenSkillMin(loreWhen.SkillMin)
				select kv.Key + ":" + kv.Value);
			return "hero=" + text + ";culture=" + text2 + ";kingdom=" + text3 + ";settlement=" + text4 + ";role=" + text5 + ";identity=" + text6 + ";gender=" + text7 + ";clan=" + text8 + ";skill=" + text9;
		}
		catch
		{
			return "__generic__";
		}
	}

	private static List<string> NormalizeWhenStringList(List<string> list)
	{
		List<string> result = new List<string>();
		try
		{
			if (list == null || list.Count <= 0)
			{
				return result;
			}
			result = (from x in list
				select (x ?? "").Trim().ToLowerInvariant() into x
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.Ordinal).OrderBy((string x) => x, StringComparer.Ordinal).ToList();
		}
		catch
		{
		}
		return result;
	}

	private static List<KeyValuePair<string, int>> NormalizeWhenSkillMin(Dictionary<string, int> skillMin)
	{
		List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();
		try
		{
			if (skillMin == null || skillMin.Count <= 0)
			{
				return result;
			}
			result = (from g in (from kv in skillMin
					where !string.IsNullOrWhiteSpace(kv.Key) && kv.Value >= 0
					select new KeyValuePair<string, int>((kv.Key ?? "").Trim().ToLowerInvariant(), kv.Value)).GroupBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.Ordinal)
				select new KeyValuePair<string, int>(g.Key, g.Max((KeyValuePair<string, int> x) => x.Value))).OrderBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.Ordinal).ToList();
		}
		catch
		{
		}
		return result;
	}

	private static LoreWhen CloneWhen(LoreWhen when)
	{
		if (when == null)
		{
			return null;
		}
		return new LoreWhen
		{
			HeroIds = ((when.HeroIds != null) ? new List<string>(when.HeroIds) : null),
			Cultures = ((when.Cultures != null) ? new List<string>(when.Cultures) : null),
			KingdomIds = ((when.KingdomIds != null) ? new List<string>(when.KingdomIds) : null),
			SettlementIds = ((when.SettlementIds != null) ? new List<string>(when.SettlementIds) : null),
			Roles = ((when.Roles != null) ? new List<string>(when.Roles) : null),
			IdentityIds = ((when.IdentityIds != null) ? new List<string>(when.IdentityIds) : null),
			IsFemale = when.IsFemale,
			IsClanLeader = when.IsClanLeader,
			SkillMin = ((when.SkillMin != null) ? new Dictionary<string, int>(when.SkillMin, StringComparer.OrdinalIgnoreCase) : null)
		};
	}

	private static LoreWhen NormalizeWhenForStorage(LoreWhen when)
	{
		try
		{
			if (when == null)
			{
				return null;
			}
			when.HeroIds = NormalizeWhenStringList(when.HeroIds);
			when.Cultures = NormalizeWhenStringList(when.Cultures);
			when.KingdomIds = NormalizeWhenStringList(when.KingdomIds);
			when.SettlementIds = NormalizeWhenStringList(when.SettlementIds);
			when.Roles = NormalizeWhenStringList(when.Roles);
			when.IdentityIds = NormalizeWhenStringList(when.IdentityIds);
			if (when.HeroIds.Count == 0)
			{
				when.HeroIds = null;
			}
			if (when.Cultures.Count == 0)
			{
				when.Cultures = null;
			}
			if (when.KingdomIds.Count == 0)
			{
				when.KingdomIds = null;
			}
			if (when.SettlementIds.Count == 0)
			{
				when.SettlementIds = null;
			}
			if (when.Roles.Count == 0)
			{
				when.Roles = null;
			}
			if (when.IdentityIds.Count == 0)
			{
				when.IdentityIds = null;
			}
			List<KeyValuePair<string, int>> list = NormalizeWhenSkillMin(when.SkillMin);
			if (list.Count > 0)
			{
				when.SkillMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				foreach (KeyValuePair<string, int> item in list)
				{
					when.SkillMin[item.Key] = item.Value;
				}
			}
			else
			{
				when.SkillMin = null;
			}
			if (when.HeroIds == null && when.Cultures == null && when.KingdomIds == null && when.SettlementIds == null && when.Roles == null && when.IdentityIds == null && !when.IsFemale.HasValue && !when.IsClanLeader.HasValue && when.SkillMin == null)
			{
				return null;
			}
		}
		catch
		{
		}
		return when;
	}

	private void ShowDuplicateVariantConditionPrompt(LoreWhen when, int existingIndex)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		string text = ((existingIndex >= 0) ? ("#" + (existingIndex + 1)) : "已有");
		string text2 = ((NormalizeWhenForStorage(CloneWhen(when)) == null) ? "通用（无条件）" : BuildWhenDetail(NormalizeWhenForStorage(CloneWhen(when))));
		InformationManager.ShowInquiry(new InquiryData("条件重复", "不允许存在多个条件完全相同的提示词。\n\n重复对象：" + text + " 条提示词\n\n重复条件：\n" + text2, true, false, "返回", "", (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private static LoreVariant PickBestVariant(LoreRule rule, Hero npcHero, CharacterObject npcCharacter, string heroId, string cultureId, string kingdomId, string settlementId, string role, bool isFemale, bool isClanLeader)
	{
		if (rule == null || rule.Variants == null || rule.Variants.Count == 0)
		{
			return null;
		}
		LoreVariant loreVariant = null;
		int num = int.MinValue;
		int num2 = int.MinValue;
		foreach (LoreVariant variant in rule.Variants)
		{
			if (variant == null)
			{
				continue;
			}
			LoreWhen when = variant.When;
			if (!IsMatch(when, npcHero, npcCharacter, heroId, cultureId, kingdomId, settlementId, role, isFemale, isClanLeader, out var score))
			{
				continue;
			}
			int num3 = 0;
			try
			{
				if (when != null && when.SkillMin != null && when.SkillMin.Count > 0)
				{
					foreach (KeyValuePair<string, int> item in when.SkillMin)
					{
						int value = item.Value;
						if (value > 0)
						{
							num3 += value;
						}
					}
				}
			}
			catch
			{
				num3 = 0;
			}
			if (loreVariant == null || score > num || (score == num && num3 > num2))
			{
				loreVariant = variant;
				num = score;
				num2 = num3;
			}
		}
		return loreVariant;
	}

	private static void EnsureSkillByIdCache()
	{
		try
		{
			if (_skillByIdCache != null && _skillByIdCache.Count > 0)
			{
				return;
			}
			lock (_skillCacheLock)
			{
				if (_skillByIdCache != null && _skillByIdCache.Count > 0)
				{
					return;
				}
				Dictionary<string, SkillObject> dictionary = new Dictionary<string, SkillObject>(StringComparer.OrdinalIgnoreCase);
				Game current = Game.Current;
				object obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					MBObjectManager objectManager = current.ObjectManager;
					obj = ((objectManager != null) ? objectManager.GetObjectTypeList<SkillObject>() : null);
				}
				MBReadOnlyList<SkillObject> val = (MBReadOnlyList<SkillObject>)obj;
				if (val != null)
				{
					foreach (SkillObject item in (List<SkillObject>)(object)val)
					{
						string text = (((item != null) ? ((MBObjectBase)item).StringId : null) ?? "").Trim();
						if (!string.IsNullOrEmpty(text) && !dictionary.ContainsKey(text))
						{
							dictionary[text] = item;
						}
					}
				}
				_skillByIdCache = dictionary;
			}
		}
		catch
		{
		}
	}

	private static SkillObject FindSkillById(string skillId)
	{
		try
		{
			string text = (skillId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			EnsureSkillByIdCache();
			if (_skillByIdCache != null && _skillByIdCache.TryGetValue(text, out var value))
			{
				return value;
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool TryGetSkillValueById(string skillId, Hero npcHero, CharacterObject npcCharacter, out int value)
	{
		value = 0;
		try
		{
			SkillObject val = FindSkillById(skillId);
			if (val == null)
			{
				return false;
			}
			if (npcHero != null)
			{
				value = npcHero.GetSkillValue(val);
				return true;
			}
			if (npcCharacter != null)
			{
				value = ((BasicCharacterObject)npcCharacter).GetSkillValue(val);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsMatch(LoreWhen when, Hero npcHero, CharacterObject npcCharacter, string heroId, string cultureId, string kingdomId, string settlementId, string role, bool isFemale, bool isClanLeader, out int score)
	{
		score = 0;
		if (when == null)
		{
			return true;
		}
		if (when.HeroIds != null && when.HeroIds.Count > 0)
		{
			bool flag = false;
			for (int i = 0; i < when.HeroIds.Count; i++)
			{
				string text = (when.HeroIds[i] ?? "").Trim();
				if (!string.IsNullOrEmpty(text) && string.Equals(text, heroId, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			score++;
		}
		if (when.Cultures != null && when.Cultures.Count > 0)
		{
			bool flag2 = false;
			for (int j = 0; j < when.Cultures.Count; j++)
			{
				string text2 = (when.Cultures[j] ?? "").Trim();
				if (!string.IsNullOrEmpty(text2) && string.Equals(text2, cultureId, StringComparison.OrdinalIgnoreCase))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				return false;
			}
			score++;
		}
		if (when.KingdomIds != null && when.KingdomIds.Count > 0)
		{
			if (string.IsNullOrEmpty(kingdomId))
			{
				return false;
			}
			bool flag3 = false;
			for (int k = 0; k < when.KingdomIds.Count; k++)
			{
				string text3 = (when.KingdomIds[k] ?? "").Trim();
				if (!string.IsNullOrEmpty(text3) && string.Equals(text3, kingdomId, StringComparison.OrdinalIgnoreCase))
				{
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				return false;
			}
			score++;
		}
		if (when.SettlementIds != null && when.SettlementIds.Count > 0)
		{
			if (string.IsNullOrEmpty(settlementId))
			{
				return false;
			}
			bool flag4 = false;
			for (int l = 0; l < when.SettlementIds.Count; l++)
			{
				string text4 = (when.SettlementIds[l] ?? "").Trim();
				if (!string.IsNullOrEmpty(text4) && string.Equals(text4, settlementId, StringComparison.OrdinalIgnoreCase))
				{
					flag4 = true;
					break;
				}
			}
			if (!flag4)
			{
				return false;
			}
			score++;
		}
		if ((when.Roles != null && when.Roles.Count > 0) || (when.IdentityIds != null && when.IdentityIds.Count > 0))
		{
			bool flag5 = false;
			if (when.Roles != null)
			{
				for (int m = 0; m < when.Roles.Count; m++)
				{
					string text5 = (when.Roles[m] ?? "").Trim();
					if (!string.IsNullOrEmpty(text5) && string.Equals(text5, role, StringComparison.OrdinalIgnoreCase))
					{
						flag5 = true;
						break;
					}
				}
			}
			if (!flag5 && when.IdentityIds != null)
			{
				string currentCharacterId = GetCurrentCharacterId(npcHero, npcCharacter);
				string text6 = (heroId ?? "").Trim();
				for (int n = 0; n < when.IdentityIds.Count; n++)
				{
					if (TryParseRoleIdentityId(when.IdentityIds[n], out var kind, out var targetId))
					{
						if (kind == "hero" && !string.IsNullOrEmpty(text6) && string.Equals(targetId, text6, StringComparison.OrdinalIgnoreCase))
						{
							flag5 = true;
							break;
						}
						if (kind == "char" && !string.IsNullOrEmpty(currentCharacterId) && string.Equals(targetId, currentCharacterId, StringComparison.OrdinalIgnoreCase))
						{
							flag5 = true;
							break;
						}
					}
				}
			}
			if (!flag5)
			{
				return false;
			}
			score++;
		}
		if (when.IsFemale.HasValue)
		{
			if (isFemale != when.IsFemale.Value)
			{
				return false;
			}
			score++;
		}
		if (when.IsClanLeader.HasValue)
		{
			if (isClanLeader != when.IsClanLeader.Value)
			{
				return false;
			}
			score++;
		}
		if (when.SkillMin != null && when.SkillMin.Count > 0)
		{
			foreach (KeyValuePair<string, int> item in when.SkillMin)
			{
				string text7 = (item.Key ?? "").Trim();
				int value = item.Value;
				if (!string.IsNullOrEmpty(text7) && value >= 0)
				{
					if (!TryGetSkillValueById(text7, npcHero, npcCharacter, out var value2))
					{
						return false;
					}
					if (value2 < value)
					{
						return false;
					}
					score++;
				}
			}
		}
		return true;
	}

	public void OpenEditorMenu(Action onReturn)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"edit", "编辑现有知识", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"create", "创建新知识", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("知识编辑系统", "请选择操作：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "edit")
				{
					OpenRuleListMenu(onReturn);
				}
				else if (text == "create")
				{
					CreateNewRule(onReturn);
				}
				else
				{
					onReturn();
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	public void OpenPlayerPersonaSetup(Action onReturn)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		LoreRule orCreatePlayerPersonaRule = GetOrCreatePlayerPersonaRule();
		if (orCreatePlayerPersonaRule == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开玩家角色介绍失败：无法创建知识条目。"));
			onReturn();
			return;
		}
		EnsurePlayerPersonaForcedKeyword(orCreatePlayerPersonaRule, showConflictMessage: true);
		OpenPlayerPersonaKeywordSetupMenu(orCreatePlayerPersonaRule, delegate
		{
			OpenPlayerPersonaIntroSetupMenu(orCreatePlayerPersonaRule, onReturn);
		});
	}

	private LoreRule GetOrCreatePlayerPersonaRule()
	{
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		LoreRule loreRule = FindRule("rule_animus_player_persona");
		bool flag = false;
		if (loreRule == null)
		{
			loreRule = new LoreRule
			{
				Id = "rule_animus_player_persona",
				Keywords = new List<string>(),
				Variants = new List<LoreVariant>()
			};
			_file.Rules.Add(loreRule);
			flag = true;
		}
		if (loreRule.Keywords == null)
		{
			loreRule.Keywords = new List<string>();
			flag = true;
		}
		if (loreRule.Variants == null)
		{
			loreRule.Variants = new List<LoreVariant>();
			flag = true;
		}
		if (flag)
		{
			TouchRuleData();
		}
		return loreRule;
	}

	private static string GetPlayerPersonaForcedKeyword()
	{
		Hero mainHero = Hero.MainHero;
		string text = (((mainHero == null) ? null : ((object)mainHero.Name)?.ToString()) ?? "玩家").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "玩家";
		}
		return text;
	}

	private bool EnsurePlayerPersonaForcedKeyword(LoreRule rule, bool showConflictMessage)
	{
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Expected O, but got Unknown
		if (rule == null)
		{
			return false;
		}
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		string playerPersonaForcedKeyword = GetPlayerPersonaForcedKeyword();
		string text = NormalizeKeywordForCompare(playerPersonaForcedKeyword);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		bool flag = false;
		rule.Keywords.RemoveAll((string x) => string.IsNullOrWhiteSpace(x));
		for (int num = rule.Keywords.Count - 1; num >= 0; num--)
		{
			string a = NormalizeKeywordForCompare(rule.Keywords[num]);
			if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
			{
				if (num == 0)
				{
					while (rule.Keywords.Count > 1 && string.Equals(NormalizeKeywordForCompare(rule.Keywords[1]), text, StringComparison.OrdinalIgnoreCase))
					{
						rule.Keywords.RemoveAt(1);
						flag = true;
					}
					if (flag)
					{
						TouchRuleData();
					}
					return true;
				}
				rule.Keywords.RemoveAt(num);
				flag = true;
			}
		}
		if (TryFindRuleIdByKeyword(playerPersonaForcedKeyword, rule.Id, out var foundRuleId))
		{
			if (flag)
			{
				TouchRuleData();
			}
			if (showConflictMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage("玩家本名关键词未能自动加入，因为它已被其他知识占用（RuleId=" + foundRuleId + "）。"));
			}
			return false;
		}
		rule.Keywords.Insert(0, playerPersonaForcedKeyword);
		TouchRuleData();
		return true;
	}

	private List<string> GetPlayerPersonaOptionalKeywords(LoreRule rule)
	{
		List<string> list = new List<string>();
		if (rule?.Keywords == null || rule.Keywords.Count == 0)
		{
			return list;
		}
		string b = NormalizeKeywordForCompare(GetPlayerPersonaForcedKeyword());
		foreach (string keyword in rule.Keywords)
		{
			string text2 = NormalizeKeywordForCompare(keyword);
			if (!string.IsNullOrEmpty(text2) && !string.Equals(text2, b, StringComparison.OrdinalIgnoreCase) && !list.Any((string x) => string.Equals(NormalizeKeywordForCompare(x), text2, StringComparison.OrdinalIgnoreCase)))
			{
				list.Add(keyword.Trim());
			}
		}
		return list;
	}

	private void OpenPlayerPersonaKeywordSetupMenu(LoreRule rule, Action onContinue)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected O, but got Unknown
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Expected O, but got Unknown
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		if (rule == null)
		{
			onContinue?.Invoke();
			return;
		}
		if (onContinue == null)
		{
			onContinue = delegate
			{
			};
		}
		bool flag = EnsurePlayerPersonaForcedKeyword(rule, showConflictMessage: false);
		string playerPersonaForcedKeyword = GetPlayerPersonaForcedKeyword();
		List<string> playerPersonaOptionalKeywords = GetPlayerPersonaOptionalKeywords(rule);
		string text = "欢迎游玩Animusforge!你可以在此界面设置您的角色的额外称呼，如果您不需要，可以直接写您的角色介绍\n\n您的角色姓名：" + playerPersonaForcedKeyword + (flag ? "" : "（当前未能写入，因为与其他知识冲突）") + "\n额外称呼：" + ((playerPersonaOptionalKeywords.Count > 0) ? string.Join(" / ", playerPersonaOptionalKeywords) : "（无）");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"add", "添加额外称呼", (ImageIdentifier)null));
		if (playerPersonaOptionalKeywords.Count > 0)
		{
			list.Add(new InquiryElement((object)"remove", "删除额外称呼", (ImageIdentifier)null));
		}
		list.Add(new InquiryElement((object)"next", "编写角色介绍", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("玩家称呼设置", text, list, true, 0, 1, "选择", "继续", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "add":
					InformationManager.ShowTextInquiry(new TextInquiryData("添加额外称呼", "请输入一个额外称呼；玩家本名已由系统自动保留，无需重复输入。", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						//IL_0089: Unknown result type (might be due to invalid IL or missing references)
						//IL_0093: Expected O, but got Unknown
						//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
						//IL_00ed: Expected O, but got Unknown
						string text2 = (input ?? "").Trim();
						string kwNorm = NormalizeKeywordForCompare(text2);
						if (string.IsNullOrEmpty(kwNorm))
						{
							OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
						}
						else
						{
							bool flag2 = false;
							try
							{
								flag2 = rule.Keywords.Any((string x) => string.Equals(NormalizeKeywordForCompare(x), kwNorm, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag2 = false;
							}
							string foundRuleId;
							if (flag2)
							{
								InformationManager.DisplayMessage(new InformationMessage("这个称呼已经存在了。"));
								OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
							}
							else if (TryFindRuleIdByKeyword(text2, rule.Id, out foundRuleId))
							{
								InformationManager.DisplayMessage(new InformationMessage("添加失败：这个称呼已被其他知识占用（RuleId=" + foundRuleId + "）。"));
								OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
							}
							else
							{
								rule.Keywords.Add(text2);
								TouchRuleData();
								OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
							}
						}
					}, (Action)delegate
					{
						OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
					break;
				case "remove":
					OpenPlayerPersonaKeywordRemoveMenu(rule, onContinue);
					break;
				case "next":
					onContinue();
					break;
				default:
					OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onContinue();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenPlayerPersonaKeywordRemoveMenu(LoreRule rule, Action onContinue)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		if (rule == null)
		{
			onContinue?.Invoke();
			return;
		}
		if (onContinue == null)
		{
			onContinue = delegate
			{
			};
		}
		List<string> playerPersonaOptionalKeywords = GetPlayerPersonaOptionalKeywords(rule);
		if (playerPersonaOptionalKeywords.Count == 0)
		{
			OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string item in playerPersonaOptionalKeywords)
		{
			list.Add(new InquiryElement((object)item, item, (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除额外称呼", "系统保留的玩家本名不能删除；这里只会删除你额外添加的称呼。", list, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(text))
				{
					rule.Keywords.RemoveAll((string x) => string.Equals(NormalizeKeywordForCompare(x), NormalizeKeywordForCompare(text), StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenPlayerPersonaIntroSetupMenu(LoreRule rule, Action onDone)
	{
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Expected O, but got Unknown
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Expected O, but got Unknown
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Expected O, but got Unknown
		if (rule == null)
		{
			onDone?.Invoke();
			return;
		}
		if (onDone == null)
		{
			onDone = delegate
			{
			};
		}
		int playerPersonaSimpleVariantIndex = GetPlayerPersonaSimpleVariantIndex(rule);
		string text = "（未填写）";
		if (playerPersonaSimpleVariantIndex >= 0 && rule.Variants != null && playerPersonaSimpleVariantIndex < rule.Variants.Count)
		{
			text = TrimPreview(rule.Variants[playerPersonaSimpleVariantIndex]?.Content ?? "", 80);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "（未填写）";
			}
		}
		List<int> playerPersonaDetailedVariantIndices = GetPlayerPersonaDetailedVariantIndices(rule);
		string text2 = "角色介绍可以影响NPC对你的看法，当然你也可以设置不同的角色对你的看法。\n\n简单介绍（通用）：" + text + "\n细化介绍（有条件）：" + playerPersonaDetailedVariantIndices.Count + " 条";
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"simple", "角色介绍", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"detailed", "不同的人对您的角色的看法", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"done", "完成并开始游戏", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("玩家角色介绍", text2, list, true, 0, 1, "进入", "跳过", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaIntroSetupMenu(rule, onDone);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "simple":
					OpenPlayerPersonaSimpleIntroEditor(rule, delegate
					{
						OpenPlayerPersonaIntroSetupMenu(rule, onDone);
					});
					break;
				case "detailed":
					OpenPlayerPersonaDetailedIntroMenu(rule, delegate
					{
						OpenPlayerPersonaIntroSetupMenu(rule, onDone);
					});
					break;
				case "done":
					onDone();
					break;
				default:
					OpenPlayerPersonaIntroSetupMenu(rule, onDone);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onDone();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private int GetPlayerPersonaSimpleVariantIndex(LoreRule rule)
	{
		if (rule?.Variants == null)
		{
			return -1;
		}
		for (int i = 0; i < rule.Variants.Count; i++)
		{
			LoreVariant loreVariant = rule.Variants[i];
			if (loreVariant != null && NormalizeWhenForStorage(CloneWhen(loreVariant.When)) == null)
			{
				return i;
			}
		}
		return -1;
	}

	private List<int> GetPlayerPersonaDetailedVariantIndices(LoreRule rule)
	{
		List<int> list = new List<int>();
		if (rule?.Variants == null)
		{
			return list;
		}
		for (int i = 0; i < rule.Variants.Count; i++)
		{
			LoreVariant loreVariant = rule.Variants[i];
			if (loreVariant != null && NormalizeWhenForStorage(CloneWhen(loreVariant.When)) != null)
			{
				list.Add(i);
			}
		}
		return list;
	}

	private void OpenPlayerPersonaSimpleIntroEditor(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		int playerPersonaSimpleVariantIndex = GetPlayerPersonaSimpleVariantIndex(rule);
		string initialText = ((playerPersonaSimpleVariantIndex < 0 || playerPersonaSimpleVariantIndex >= rule.Variants.Count) ? "" : (rule.Variants[playerPersonaSimpleVariantIndex]?.Content ?? ""));
		DevTextEditorHelper.ShowLongTextEditor("简单的玩家角色介绍", "当前通用介绍已载入下方输入框。", "请输入通用介绍内容；留空表示不设置。", initialText, delegate(string input)
		{
			string value = (input ?? "").Trim();
			if (string.IsNullOrWhiteSpace(value))
			{
				if (playerPersonaSimpleVariantIndex >= 0 && playerPersonaSimpleVariantIndex < rule.Variants.Count)
				{
					rule.Variants.RemoveAt(playerPersonaSimpleVariantIndex);
					TouchRuleData();
				}
			}
			else if (playerPersonaSimpleVariantIndex >= 0 && playerPersonaSimpleVariantIndex < rule.Variants.Count)
			{
				LoreVariant loreVariant = rule.Variants[playerPersonaSimpleVariantIndex];
				if (loreVariant != null)
				{
					loreVariant.Priority = 0;
					loreVariant.When = null;
					loreVariant.Content = input ?? "";
					TouchRuleData();
				}
			}
			else
			{
				rule.Variants.Add(new LoreVariant
				{
					Priority = 0,
					When = null,
					Content = (input ?? "")
				});
				TouchRuleData();
			}
			onReturn();
		}, delegate
		{
			onReturn();
		}, "确定");
	}

	private void OpenPlayerPersonaDetailedIntroMenu(LoreRule rule, Action onReturn)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected O, but got Unknown
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		List<int> playerPersonaDetailedVariantIndices = GetPlayerPersonaDetailedVariantIndices(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"add", "新增一条细化介绍", (ImageIdentifier)null));
		foreach (int item in playerPersonaDetailedVariantIndices)
		{
			LoreVariant loreVariant = rule.Variants[item];
			string arg = BuildWhenLabel(loreVariant.When);
			string text = TrimPreview(loreVariant.Content ?? "", 24);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "（空）";
			}
			list.Add(new InquiryElement((object)item, $"#{item + 1} {arg}  {text}", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("细化的玩家角色介绍", "这里编辑的是带条件的介绍。它们写入后的结构，和其他知识提示词完全一样。", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaDetailedIntroMenu(rule, onReturn);
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "add")
				{
					CreatePlayerPersonaDetailedVariant(rule, delegate
					{
						OpenPlayerPersonaDetailedIntroMenu(rule, onReturn);
					});
				}
				else if (selected[0].Identifier is int idx)
				{
					OpenPlayerPersonaDetailedVariantEditor(rule, idx, delegate
					{
						OpenPlayerPersonaDetailedIntroMenu(rule, onReturn);
					});
				}
				else
				{
					OpenPlayerPersonaDetailedIntroMenu(rule, onReturn);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void CreatePlayerPersonaDetailedVariant(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		LoreVariant loreVariant = new LoreVariant
		{
			Priority = 0,
			When = new LoreWhen(),
			Content = ""
		};
		rule.Variants.Add(loreVariant);
		TouchRuleData();
		int num = rule.Variants.Count - 1;
		LoreWhen loreWhen = new LoreWhen();
		OpenWhenEditor(loreWhen, delegate
		{
			loreWhen = NormalizeWhenForStorage(loreWhen);
			if (loreWhen == null)
			{
				if (num >= 0 && num < rule.Variants.Count)
				{
					rule.Variants.RemoveAt(num);
					TouchRuleData();
				}
				onReturn();
			}
			else
			{
				int num2 = FindDuplicateVariantIndex(rule, loreWhen, num);
				if (num2 >= 0)
				{
					ShowDuplicateVariantConditionPrompt(loreWhen, num2);
					if (num >= 0 && num < rule.Variants.Count)
					{
						rule.Variants.RemoveAt(num);
						TouchRuleData();
					}
					onReturn();
				}
				else
				{
					loreVariant.When = loreWhen;
					TouchRuleData();
					OpenPlayerPersonaDetailedVariantEditor(rule, num, onReturn, isNewVariant: true);
				}
			}
		});
	}

	private void OpenPlayerPersonaDetailedVariantEditor(LoreRule rule, int idx, Action onReturn, bool isNewVariant = false)
	{
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Expected O, but got Unknown
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Expected O, but got Unknown
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Expected O, but got Unknown
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null || idx < 0 || idx >= rule.Variants.Count)
		{
			onReturn();
			return;
		}
		LoreVariant v = rule.Variants[idx];
		if (v == null)
		{
			onReturn();
			return;
		}
		string text = BuildWhenLabel(v.When);
		string text2 = TrimPreview(v.Content ?? "", 220);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "（空）";
		}
		string text3 = "当前条件：" + text + "\n\n当前内容预览：\n" + text2;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"content", "编辑介绍内容", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"when", "编辑条件", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"delete", "删除此条细化介绍", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("细化介绍编辑", text3, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "content":
					DevTextEditorHelper.ShowLongTextEditor("编辑介绍内容", "当前这条带条件的玩家角色介绍已载入下方输入框。", "可直接编辑整段内容。", v.Content ?? "", delegate(string input)
					{
						v.Content = input ?? "";
						TouchRuleData();
						OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
					}, delegate
					{
						OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
					}, "确定", "取消");
					break;
				case "when":
				{
					LoreWhen loreWhen = CloneWhen(v.When) ?? new LoreWhen();
					OpenWhenEditor(loreWhen, delegate
					{
						//IL_0025: Unknown result type (might be due to invalid IL or missing references)
						//IL_002f: Expected O, but got Unknown
						loreWhen = NormalizeWhenForStorage(loreWhen);
						if (loreWhen == null)
						{
							InformationManager.DisplayMessage(new InformationMessage("细化介绍至少需要保留一个条件；如果想写通用介绍，请回到上一层使用“简单的玩家角色介绍”。"));
							OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
						}
						else
						{
							int num = FindDuplicateVariantIndex(rule, loreWhen, idx);
							if (num >= 0)
							{
								ShowDuplicateVariantConditionPrompt(loreWhen, num);
								OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
							}
							else
							{
								v.When = loreWhen;
								TouchRuleData();
								OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
							}
						}
					});
					break;
				}
				case "delete":
					rule.Variants.RemoveAt(idx);
					TouchRuleData();
					onReturn();
					break;
				default:
					OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			if (isNewVariant && idx >= 0 && idx < rule.Variants.Count && string.IsNullOrWhiteSpace(rule.Variants[idx]?.Content))
			{
				rule.Variants.RemoveAt(idx);
				TouchRuleData();
			}
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static void TryPersistMcmSettings(DuelSettings s)
	{
		try
		{
			if (s != null)
			{
				MethodInfo method = ((object)s).GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
				if (method != null)
				{
					method.Invoke(s, null);
				}
			}
		}
		catch
		{
		}
	}

	private void OpenRetrievalSettingsMenu(Action onReturn)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Expected O, but got Unknown
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Expected O, but got Unknown
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Expected O, but got Unknown
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Expected O, but got Unknown
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置。"));
			onReturn();
			return;
		}
		int topK = settings.KnowledgeSemanticTopK;
		try
		{
			if (settings.KnowledgeDirectTopN > 0)
			{
				topK = settings.KnowledgeDirectTopN;
			}
		}
		catch
		{
		}
		if (topK < 1)
		{
			topK = 1;
		}
		if (topK > 12)
		{
			topK = 12;
		}
		settings.KnowledgeSemanticTopK = topK;
		try
		{
			settings.KnowledgeDirectTopN = topK;
		}
		catch
		{
		}
		string text = (AIConfigHandler.KnowledgeRetrievalFromMcm ? "MCM（当前生效）" : "RuleBehaviorPrompts.json（当前生效）");
		string text2 = "自动召回 + 本地精排";
		string text3 = "当前配置来源：" + text + "\n当前模式：" + text2 + "\n语义检索：" + (AIConfigHandler.KnowledgeRetrievalEnabled ? "开启" : "关闭") + "\n语义优先：" + (AIConfigHandler.KnowledgeSemanticFirst ? "是" : "否（关键词优先）") + "\n知识返回上限：" + AIConfigHandler.KnowledgeSemanticTopK + "\n说明：系统会自动按意图拆分后分配召回和精排预算，但最终最多只注入这么多条知识。";
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"source", "配置来源：" + (settings.UseMcmKnowledgeRetrieval ? "切到 RuleBehaviorPrompts.json" : "切到 MCM"), (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"enabled", "语义检索：" + (settings.KnowledgeRetrievalEnabled ? "关闭" : "开启"), (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"topk", "设置知识返回上限（当前 " + topK + "）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"reset", "恢复默认（MCM）", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("知识检索设置", text3, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			//IL_018d: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenRetrievalSettingsMenu(onReturn);
			}
			else
			{
				string text4 = selected[0].Identifier as string;
				if (string.IsNullOrEmpty(text4))
				{
					OpenRetrievalSettingsMenu(onReturn);
				}
				else
				{
					switch (text4)
					{
					case "source":
						settings.UseMcmKnowledgeRetrieval = !settings.UseMcmKnowledgeRetrieval;
						TryPersistMcmSettings(settings);
						TouchRuleData();
						OpenRetrievalSettingsMenu(onReturn);
						break;
					case "enabled":
						settings.UseMcmKnowledgeRetrieval = true;
						settings.KnowledgeRetrievalEnabled = !settings.KnowledgeRetrievalEnabled;
						TryPersistMcmSettings(settings);
						TouchRuleData();
						OpenRetrievalSettingsMenu(onReturn);
						break;
					case "topk":
						InformationManager.ShowTextInquiry(new TextInquiryData("设置知识返回上限", "请输入 1~12 的整数：", true, true, "确定", "取消", (Action<string>)delegate(string input)
						{
							//IL_0026: Unknown result type (might be due to invalid IL or missing references)
							//IL_0030: Expected O, but got Unknown
							string s = (input ?? "").Trim();
							if (!int.TryParse(s, out var result))
							{
								InformationManager.DisplayMessage(new InformationMessage("请输入有效整数。"));
								OpenRetrievalSettingsMenu(onReturn);
							}
							else
							{
								if (result < 1)
								{
									result = 1;
								}
								if (result > 12)
								{
									result = 12;
								}
								settings.UseMcmKnowledgeRetrieval = true;
								settings.KnowledgeSemanticTopK = result;
								try
								{
									settings.KnowledgeDirectTopN = result;
								}
								catch
								{
								}
								TryPersistMcmSettings(settings);
								TouchRuleData();
								OpenRetrievalSettingsMenu(onReturn);
							}
						}, (Action)delegate
						{
							OpenRetrievalSettingsMenu(onReturn);
						}, false, (Func<string, Tuple<bool, string>>)null, topK.ToString(CultureInfo.InvariantCulture), ""), false, false);
						break;
					case "reset":
						settings.UseMcmKnowledgeRetrieval = true;
						settings.KnowledgeRetrievalEnabled = true;
						settings.KnowledgeSemanticFirst = true;
						settings.KnowledgeSemanticTopK = 4;
						try
						{
							settings.KnowledgeDirectTopN = 4;
						}
						catch
						{
						}
						TryPersistMcmSettings(settings);
						TouchRuleData();
						OpenRetrievalSettingsMenu(onReturn);
						break;
					default:
						OpenRetrievalSettingsMenu(onReturn);
						break;
					}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenRuleListMenu(Action onReturn)
	{
		OpenRuleListMenuPaged(onReturn, 0, null);
	}

	private void OpenRuleListMenuPaged(Action onReturn, int page, string query)
	{
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Expected O, but got Unknown
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Expected O, but got Unknown
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Expected O, but got Unknown
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Expected O, but got Unknown
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Expected O, but got Unknown
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Expected O, but got Unknown
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Expected O, but got Unknown
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Expected O, but got Unknown
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		EnsureRuleIndexCache();
		string q = (query ?? "").Trim();
		string[] tokens = null;
		try
		{
			if (!string.IsNullOrWhiteSpace(q))
			{
				tokens = q.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			}
		}
		catch
		{
			tokens = null;
		}
		int num = page * 60;
		int num2 = num + 60;
		int num3 = 0;
		List<RuleIndexItem> list = new List<RuleIndexItem>();
		try
		{
			List<RuleIndexItem> list2 = _ruleIndexCache ?? new List<RuleIndexItem>();
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				RuleIndexItem ruleIndexItem = list2[num4];
				if (ruleIndexItem != null && IsRuleListMatch(ruleIndexItem, tokens))
				{
					if (num3 >= num && num3 < num2)
					{
						list.Add(ruleIndexItem);
					}
					num3++;
				}
			}
		}
		catch
		{
			num3 = 0;
			list = new List<RuleIndexItem>();
		}
		int num5 = ((num3 > 0) ? ((num3 + 60 - 1) / 60) : 0);
		if (num5 > 0 && page >= num5)
		{
			OpenRuleListMenuPaged(onReturn, num5 - 1, query);
			return;
		}
		string text = "编辑现有知识";
		string text2 = ((num5 > 0) ? $"{text}（{num3} 条，{page + 1}/{num5}）" : (text + "（0 条）"));
		string text3 = "请选择要编辑的知识：";
		if (!string.IsNullOrWhiteSpace(q))
		{
			text3 = text3 + "\n当前搜索：" + TrimPreview(q, 80);
		}
		if (num3 <= 0)
		{
			text3 += "\n\n没有匹配项。可使用“搜索”或“创建新知识”。";
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "搜索（按关键词/ID）", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(q))
		{
			list3.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (num5 > 0 && page + 1 < num5)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list3.Add(new InquiryElement((object)"__create__", "创建新知识", (ImageIdentifier)null));
		list3.Add(new InquiryElement((object)"__bulk_delete__", "批量删除知识", (ImageIdentifier)null));
		if (list.Count > 0)
		{
			list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		}
		foreach (RuleIndexItem item in list)
		{
			string text4 = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text4))
			{
				string text5 = (item?.Label ?? text4).Trim();
				list3.Add(new InquiryElement((object)text4, text5, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(text2, text3, list3, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c6: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenRuleListMenuPaged(onReturn, page, query);
			}
			else
			{
				string text6 = selected[0].Identifier as string;
				switch (text6)
				{
				case "__sep__":
					OpenRuleListMenuPaged(onReturn, page, query);
					break;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索知识", "输入关键词或 RuleId（可用空格分隔多个词）：", true, true, "搜索", "取消", (Action<string>)delegate(string input)
					{
						OpenRuleListMenuPaged(onReturn, 0, input);
					}, (Action)delegate
					{
						OpenRuleListMenuPaged(onReturn, page, query);
					}, false, (Func<string, Tuple<bool, string>>)null, q, ""), false, false);
					break;
				case "__clear__":
					OpenRuleListMenuPaged(onReturn, 0, null);
					break;
				case "__prev__":
					OpenRuleListMenuPaged(onReturn, page - 1, query);
					break;
				case "__next__":
					OpenRuleListMenuPaged(onReturn, page + 1, query);
					break;
				case "__create__":
					CreateNewRule(onReturn);
					break;
				case "__bulk_delete__":
					OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
					break;
				default:
				{
					string id = text6;
					LoreRule loreRule = FindRule(id);
					if (loreRule == null)
					{
						OpenRuleListMenuPaged(onReturn, page, query);
					}
					else
					{
						_editingRuleId = loreRule.Id;
						OpenRuleEditorMenu(loreRule, delegate
						{
							OpenRuleListMenuPaged(onReturn, page, query);
						});
					}
					break;
				}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void CreateNewRule(Action onReturn)
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		InformationManager.ShowTextInquiry(new TextInquiryData("创建新知识", "请输入该知识的“第一个关键词”（将用于该知识条目的ID与导出文件命名，不可重复；后续仍可继续添加更多关键词）：", true, true, "创建", "返回", (Action<string>)delegate(string input)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Expected O, but got Unknown
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Expected O, but got Unknown
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Expected O, but got Unknown
			string text = (input ?? "").Trim();
			string text2 = BuildRuleIdFromKeyword(text);
			string foundRuleId;
			if (string.IsNullOrEmpty(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("创建失败：第一个关键词为空或无法用于命名。"));
				CreateNewRule(onReturn);
			}
			else if (HasRuleIdIgnoreCase(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("创建失败：已存在同名知识条目（ID=" + text2 + "）。请换一个关键词。"));
				CreateNewRule(onReturn);
			}
			else if (TryFindRuleIdByKeyword(text, null, out foundRuleId))
			{
				InformationManager.DisplayMessage(new InformationMessage("创建失败：关键词已被其他知识条目占用（关键词=" + text + "，RuleId=" + foundRuleId + "）。"));
				CreateNewRule(onReturn);
			}
			else
			{
				LoreRule loreRule = new LoreRule
				{
					Id = text2,
					Keywords = new List<string>(),
					RagShortTexts = new List<string>(),
					TextMappings = new List<LoreTextMapping>()
				};
				if (!string.IsNullOrEmpty(text))
				{
					loreRule.Keywords.Add(text);
				}
				loreRule.Variants = new List<LoreVariant>();
				_file.Rules.Add(loreRule);
				TouchRuleData();
				_editingRuleId = loreRule.Id;
				OpenRuleEditorMenu(loreRule, delegate
				{
					OpenRuleListMenu(onReturn);
				});
			}
		}, (Action)delegate
		{
			OpenRuleListMenu(onReturn);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
	}

	private void OpenRuleBulkDeleteMenuPaged(Action onReturn, int page, string query)
	{
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Expected O, but got Unknown
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Expected O, but got Unknown
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Expected O, but got Unknown
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Expected O, but got Unknown
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Expected O, but got Unknown
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0439: Expected O, but got Unknown
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		if (_file.Rules == null)
		{
			_file.Rules = new List<LoreRule>();
		}
		EnsureRuleIndexCache();
		string text = (query ?? "").Trim();
		string[] tokens = null;
		try
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				tokens = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			}
		}
		catch
		{
			tokens = null;
		}
		int num = page * 60;
		int num2 = num + 60;
		int num3 = 0;
		List<RuleIndexItem> list = new List<RuleIndexItem>();
		try
		{
			List<RuleIndexItem> list2 = _ruleIndexCache ?? new List<RuleIndexItem>();
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				RuleIndexItem ruleIndexItem = list2[num4];
				if (ruleIndexItem != null && IsRuleListMatch(ruleIndexItem, tokens))
				{
					if (num3 >= num && num3 < num2)
					{
						list.Add(ruleIndexItem);
					}
					num3++;
				}
			}
		}
		catch
		{
			num3 = 0;
			list = new List<RuleIndexItem>();
		}
		int num5 = ((num3 > 0) ? ((num3 + 60 - 1) / 60) : 0);
		if (num5 > 0 && page >= num5)
		{
			OpenRuleBulkDeleteMenuPaged(onReturn, num5 - 1, query);
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "搜索（按关键词/ID）", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (num5 > 0 && page + 1 < num5)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		if (list.Count > 0)
		{
			list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
			foreach (RuleIndexItem item in list)
			{
				string text2 = (item?.Id ?? "").Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					string text3 = (item?.Label ?? text2).Trim();
					list3.Add(new InquiryElement((object)text2, text3, (ImageIdentifier)null));
				}
			}
		}
		string text4 = ((num5 > 0) ? $"批量删除知识（{num3} 条，{page + 1}/{num5}）" : "批量删除知识（0 条）");
		string text5 = "可多选当前页的知识后一起删除。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 = text5 + "\n当前搜索：" + TrimPreview(text, 80);
		}
		if (num3 <= 0)
		{
			text5 += "\n\n没有匹配项。";
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(text4, text5, list3, true, 0, Math.Max(1, list3.Count), "删除选中", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e0: Expected O, but got Unknown
			//IL_0156: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Expected O, but got Unknown
			//IL_020c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Expected O, but got Unknown
			//IL_043f: Unknown result type (might be due to invalid IL or missing references)
			//IL_044b: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
			}
			else
			{
				List<string> list4 = new List<string>();
				foreach (InquiryElement item2 in selected)
				{
					string text6 = item2?.Identifier as string;
					if (!string.IsNullOrWhiteSpace(text6))
					{
						list4.Add(text6);
					}
				}
				if (list4.Count == 0)
				{
					OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
				}
				else
				{
					List<string> list5 = list4.Where((string x) => x.StartsWith("__", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
					if (list5.Count > 0)
					{
						if (list4.Count > 1)
						{
							InformationManager.DisplayMessage(new InformationMessage("批量删除页里，操作按钮不能和知识条目一起多选。"));
							OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
						}
						else
						{
							switch (list5[0])
							{
							case "__search__":
								InformationManager.ShowTextInquiry(new TextInquiryData("搜索知识", "输入关键词或 RuleId（可用空格分隔多个词）：", true, true, "搜索", "取消", (Action<string>)delegate(string input)
								{
									OpenRuleBulkDeleteMenuPaged(onReturn, 0, input);
								}, (Action)delegate
								{
									OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
								}, false, (Func<string, Tuple<bool, string>>)null, text, ""), false, false);
								break;
							case "__clear__":
								OpenRuleBulkDeleteMenuPaged(onReturn, 0, null);
								break;
							case "__prev__":
								OpenRuleBulkDeleteMenuPaged(onReturn, page - 1, query);
								break;
							case "__next__":
								OpenRuleBulkDeleteMenuPaged(onReturn, page + 1, query);
								break;
							default:
								OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
								break;
							}
						}
					}
					else
					{
						List<RuleIndexItem> list6 = list.Where((RuleIndexItem x) => x != null && list4.Contains((x.Id ?? "").Trim(), StringComparer.OrdinalIgnoreCase)).ToList();
						if (list6.Count == 0)
						{
							InformationManager.DisplayMessage(new InformationMessage("没有选中可删除的知识。"));
							OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
						}
						else
						{
							List<string> list7 = (from x in list6
								select (x.Id ?? "").Trim() into x
								where !string.IsNullOrWhiteSpace(x)
								select x).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
							string text7 = string.Join("\n", from x in list6.Take(8)
								select "- " + TrimPreview((x.Label ?? x.Id ?? "").Trim(), 80));
							if (list6.Count > 8)
							{
								text7 = text7 + "\n- ... 以及另外 " + (list6.Count - 8) + " 条";
							}
							InformationManager.ShowInquiry(new InquiryData("确认批量删除", "将要删除以下知识：\n\n" + text7 + "\n\n共 " + list7.Count + " 条。此操作不可撤销。", true, true, "确认删除", "返回", (Action)delegate
							{
								//IL_00be: Unknown result type (might be due to invalid IL or missing references)
								//IL_00c8: Expected O, but got Unknown
								int num6 = 0;
								try
								{
									HashSet<string> hashSet = new HashSet<string>(list7, StringComparer.OrdinalIgnoreCase);
									num6 = _file.Rules.RemoveAll((LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id) && hashSet.Contains(r.Id.Trim()));
									if (!string.IsNullOrWhiteSpace(_editingRuleId) && hashSet.Contains(_editingRuleId.Trim()))
									{
										_editingRuleId = null;
									}
									TouchRuleData();
								}
								catch
								{
									num6 = 0;
								}
								InformationManager.DisplayMessage(new InformationMessage("已删除知识 " + num6 + " 条。"));
								OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
							}, (Action)delegate
							{
								OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
							}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
						}
					}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenRuleListMenuPaged(onReturn, page, query);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenRuleEditorMenu(LoreRule rule, Action onReturn)
	{
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected O, but got Unknown
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Expected O, but got Unknown
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Expected O, but got Unknown
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Expected O, but got Unknown
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Expected O, but got Unknown
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		string text = ((rule.Keywords != null && rule.Keywords.Count > 0) ? rule.Keywords[0] : (rule.Id ?? "知识"));
		EnsureRagShortTexts(rule);
		EnsureTextMappings(rule);
		string text2 = $"关键词：{rule.Keywords?.Count ?? 0} 条\nRAG专用短句：{rule.RagShortTexts?.Count ?? 0} 条\n提示词：{rule.Variants?.Count ?? 0} 条\n词汇映射：{rule.TextMappings?.Count ?? 0} 条";
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"keywords", "编辑关键词", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"rag_short_texts", "编辑RAG专用短句", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"generate_rag_short_texts", "一键生成RAG专用短句", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"variants", "编辑提示词（条件组合）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"text_mappings", "编辑词汇映射", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"delete", "删除此知识", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("知识 - " + text, text2, list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRuleEditorMenu(rule, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "keywords":
					OpenKeywordMenu(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "rag_short_texts":
					OpenRagShortTextMenu(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "generate_rag_short_texts":
					GenerateRagShortTextsByLlm(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "variants":
					OpenVariantMenu(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "text_mappings":
					OpenTextMappingMenu(rule, delegate
					{
						OpenRuleEditorMenu(rule, onReturn);
					});
					break;
				case "delete":
					_file.Rules.RemoveAll((LoreRule r) => r != null && r.Id == rule.Id);
					TouchRuleData();
					onReturn();
					break;
				default:
					OpenRuleEditorMenu(rule, onReturn);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenTextMappingMenu(LoreRule rule, Action onReturn)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		EnsureTextMappings(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__add__", "新增词汇映射", (ImageIdentifier)null));
		if (rule.TextMappings != null)
		{
			for (int num = 0; num < rule.TextMappings.Count; num++)
			{
				LoreTextMapping loreTextMapping = rule.TextMappings[num];
				if (loreTextMapping != null)
				{
					list.Add(new InquiryElement((object)num, $"#{num + 1} {BuildTextMappingSummary(rule, loreTextMapping)}", (ImageIdentifier)null));
				}
			}
		}
		string text = "这里只会替换当前知识条目下的所有提示词内容，不会影响其他知识。";
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑词汇映射", text, list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingMenu(rule, onReturn);
			}
			else if (selected[0].Identifier as string == "__add__")
			{
				CreateTextMapping(rule, delegate
				{
					OpenTextMappingMenu(rule, onReturn);
				});
			}
			else if (selected[0].Identifier is int idx)
			{
				OpenTextMappingEditor(rule, idx, delegate
				{
					OpenTextMappingMenu(rule, onReturn);
				});
			}
			else
			{
				OpenTextMappingMenu(rule, onReturn);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void CreateTextMapping(LoreRule rule, Action onReturn)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		EnsureTextMappings(rule);
		InformationManager.ShowTextInquiry(new TextInquiryData("新增词汇映射", "请输入要被替换的原文本。命中这条知识后，会把它替换成你选择的动态对象值。", true, true, "下一步", "取消", (Action<string>)delegate(string input)
		{
			string text = (input ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				OpenTextMappingMenu(rule, onReturn);
			}
			else
			{
				LoreTextMapping initial = new LoreTextMapping
				{
					SourceText = text
				};
				OpenTextMappingTargetSelection(initial, delegate(LoreTextMapping result)
				{
					EnsureTextMappings(rule);
					rule.TextMappings.Add(result);
					TouchRuleData();
					onReturn();
				}, delegate
				{
					OpenTextMappingMenu(rule, onReturn);
				});
			}
		}, (Action)delegate
		{
			OpenTextMappingMenu(rule, onReturn);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
	}

	private void OpenTextMappingEditor(LoreRule rule, int idx, Action onReturn)
	{
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Expected O, but got Unknown
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Expected O, but got Unknown
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Expected O, but got Unknown
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Expected O, but got Unknown
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Expected O, but got Unknown
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Expected O, but got Unknown
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Expected O, but got Unknown
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		EnsureTextMappings(rule);
		if (rule.TextMappings == null || idx < 0 || idx >= rule.TextMappings.Count)
		{
			onReturn();
			return;
		}
		LoreTextMapping loreTextMapping = rule.TextMappings[idx];
		if (loreTextMapping == null)
		{
			onReturn();
			return;
		}
		string text = ResolveTextMappingValue(loreTextMapping, rule);
		string textMappingEmptyValueText = GetTextMappingEmptyValueText(loreTextMapping);
		string textMappingStatusTrueText = GetTextMappingStatusTrueText(loreTextMapping);
		string textMappingStatusFalseText = GetTextMappingStatusFalseText(loreTextMapping);
		string text2 = ((!string.IsNullOrWhiteSpace(text)) ? text : (string.IsNullOrWhiteSpace(textMappingEmptyValueText) ? "（当前无值，空时保留原词）" : (textMappingEmptyValueText + "（空值兜底）")));
		string text3 = "原文本：" + (((loreTextMapping.SourceText ?? "").Trim() == "") ? "（空）" : loreTextMapping.SourceText.Trim()) + "\n映射类型：" + GetTextMappingKindLabel(loreTextMapping.Kind) + "\n目标" + GetTextMappingTargetTypeLabel(loreTextMapping.Kind) + "：" + GetTextMappingTargetDisplayName(loreTextMapping, rule);
		if (IsStatusMappingKind(loreTextMapping.Kind))
		{
			text3 = text3 + "\n状态为真文案：" + (string.IsNullOrWhiteSpace(textMappingStatusTrueText) ? "（未设置）" : textMappingStatusTrueText) + "\n状态为假文案：" + (string.IsNullOrWhiteSpace(textMappingStatusFalseText) ? "（未设置）" : textMappingStatusFalseText);
		}
		text3 = text3 + "\n空值返回文案：" + (string.IsNullOrWhiteSpace(textMappingEmptyValueText) ? "（未设置，空时保留原词）" : textMappingEmptyValueText) + "\n当前预览：" + text2;
		if (IsTextMappingAgeRangeKind(loreTextMapping.Kind))
		{
			int num = NormalizeMemberAgeBound(loreTextMapping.AgeMin, 0);
			int num2 = NormalizeMemberAgeBound(loreTextMapping.AgeMax, 120);
			text3 += $"\n年龄范围：{num}-{num2} 岁";
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"source", "编辑原文本", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"target", "编辑映射目标", (ImageIdentifier)null));
		if (IsStatusMappingKind(loreTextMapping.Kind))
		{
			list.Add(new InquiryElement((object)"true_text", "编辑状态为真文案", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"false_text", "编辑状态为假文案", (ImageIdentifier)null));
		}
		list.Add(new InquiryElement((object)"empty_value", "编辑空值返回文案", (ImageIdentifier)null));
		if (IsTextMappingAgeRangeKind(loreTextMapping.Kind))
		{
			list.Add(new InquiryElement((object)"age_range", "编辑年龄范围", (ImageIdentifier)null));
		}
		list.Add(new InquiryElement((object)"delete", "删除此映射", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("词汇映射编辑", text3, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Expected O, but got Unknown
			//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02df: Expected O, but got Unknown
			//IL_0278: Unknown result type (might be due to invalid IL or missing references)
			//IL_0284: Expected O, but got Unknown
			//IL_021d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingEditor(rule, idx, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "source":
					InformationManager.ShowTextInquiry(new TextInquiryData("编辑原文本", "请输入新的原文本：", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						string text4 = (input ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(text4))
						{
							loreTextMapping.SourceText = text4;
							TouchRuleData();
						}
						OpenTextMappingEditor(rule, idx, onReturn);
					}, (Action)delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, loreTextMapping.SourceText ?? "", ""), false, false);
					break;
				case "target":
					OpenTextMappingTargetSelection(loreTextMapping, delegate(LoreTextMapping result)
					{
						loreTextMapping.Kind = result.Kind;
						loreTextMapping.TargetId = result.TargetId;
						loreTextMapping.AgeMin = result.AgeMin;
						loreTextMapping.AgeMax = result.AgeMax;
						loreTextMapping.TrueText = result.TrueText;
						loreTextMapping.FalseText = result.FalseText;
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					});
					break;
				case "true_text":
					InformationManager.ShowTextInquiry(new TextInquiryData("编辑状态为真文案", "当状态判断结果为真时，使用这段文案替换原文本。留空表示真时不替换。", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						loreTextMapping.TrueText = (input ?? "").Trim();
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, (Action)delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, loreTextMapping.TrueText ?? "", ""), false, false);
					break;
				case "false_text":
					InformationManager.ShowTextInquiry(new TextInquiryData("编辑状态为假文案", "当状态判断结果为假时，使用这段文案替换原文本。留空表示假时不替换。", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						loreTextMapping.FalseText = (input ?? "").Trim();
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, (Action)delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, loreTextMapping.FalseText ?? "", ""), false, false);
					break;
				case "empty_value":
					InformationManager.ShowTextInquiry(new TextInquiryData("编辑空值返回文案", "当这条映射当前取不到值时，会改用这段文案替换原文本。留空表示空时保留原词。", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						loreTextMapping.EmptyValueText = (input ?? "").Trim();
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, (Action)delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, loreTextMapping.EmptyValueText ?? "", ""), false, false);
					break;
				case "age_range":
					OpenTextMappingAgeRangeEditor(loreTextMapping, delegate(LoreTextMapping result)
					{
						loreTextMapping.AgeMin = result.AgeMin;
						loreTextMapping.AgeMax = result.AgeMax;
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					});
					break;
				case "delete":
					rule.TextMappings.RemoveAt(idx);
					TouchRuleData();
					onReturn();
					break;
				default:
					OpenTextMappingEditor(rule, idx, onReturn);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static LoreTextMapping CloneTextMapping(LoreTextMapping mapping)
	{
		if (mapping == null)
		{
			return new LoreTextMapping();
		}
		return new LoreTextMapping
		{
			SourceText = mapping.SourceText,
			Kind = mapping.Kind,
			TargetId = mapping.TargetId,
			AgeMin = mapping.AgeMin,
			AgeMax = mapping.AgeMax,
			EmptyValueText = mapping.EmptyValueText,
			TrueText = mapping.TrueText,
			FalseText = mapping.FalseText
		};
	}

	private void OpenTextMappingAgeRangeEditor(LoreTextMapping mapping, Action<LoreTextMapping> onConfigured, Action onCancel)
	{
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		if (mapping == null || onConfigured == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		LoreTextMapping loreTextMapping = CloneTextMapping(mapping);
		int num = NormalizeMemberAgeBound(loreTextMapping.AgeMin, 0);
		int num2 = NormalizeMemberAgeBound(loreTextMapping.AgeMax, 120);
		InformationManager.ShowTextInquiry(new TextInquiryData("设置年龄范围", $"请输入年龄范围，格式为 最小-最大。\n例如：18-35\n当前值：{num}-{num2}", true, true, "确定", "返回", (Action<string>)delegate(string input)
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Expected O, but got Unknown
			string[] array = (input ?? "").Trim().Replace(" ", "").Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 2 || !int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2))
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法范围，例如 18-35。"));
				OpenTextMappingAgeRangeEditor(loreTextMapping, onConfigured, onCancel);
			}
			else
			{
				result = NormalizeMemberAgeBound(result, 0);
				result2 = NormalizeMemberAgeBound(result2, 120);
				if (result > result2)
				{
					int num3 = result;
					result = result2;
					result2 = num3;
				}
				loreTextMapping.AgeMin = result;
				loreTextMapping.AgeMax = result2;
				onConfigured(loreTextMapping);
			}
		}, (Action)delegate
		{
			onCancel();
		}, false, (Func<string, Tuple<bool, string>>)null, $"{num}-{num2}", ""), false, false);
	}

	private void OpenTextMappingStatusOutcomeEditor(LoreTextMapping mapping, Action<LoreTextMapping> onConfigured, Action onCancel)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		if (mapping == null || onConfigured == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		LoreTextMapping loreTextMapping = CloneTextMapping(mapping);
		InformationManager.ShowTextInquiry(new TextInquiryData("设置状态为真文案", "请输入当状态判断结果为真时要替换成的文本。留空表示真时不替换。", true, true, "下一步", "返回", (Action<string>)delegate(string input)
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected O, but got Unknown
			loreTextMapping.TrueText = (input ?? "").Trim();
			InformationManager.ShowTextInquiry(new TextInquiryData("设置状态为假文案", "请输入当状态判断结果为假时要替换成的文本。留空表示假时不替换。", true, true, "完成", "返回", (Action<string>)delegate(string text)
			{
				loreTextMapping.FalseText = (text ?? "").Trim();
				onConfigured(loreTextMapping);
			}, (Action)delegate
			{
				OpenTextMappingStatusOutcomeEditor(loreTextMapping, onConfigured, onCancel);
			}, false, (Func<string, Tuple<bool, string>>)null, loreTextMapping.FalseText ?? "", ""), false, false);
		}, (Action)delegate
		{
			onCancel();
		}, false, (Func<string, Tuple<bool, string>>)null, loreTextMapping.TrueText ?? "", ""), false, false);
	}

	private void OpenTextMappingTargetSelection(LoreTextMapping initial, Action<LoreTextMapping> onPicked, Action onCancel)
	{
		if (onPicked == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		LoreTextMapping loreTextMapping = CloneTextMapping(initial);
		OpenTextMappingKindPicker(delegate(string kind)
		{
			loreTextMapping.Kind = kind;
			string automaticTargetIdForKind = GetAutomaticTargetIdForKind(kind);
			if (!string.IsNullOrWhiteSpace(automaticTargetIdForKind))
			{
				loreTextMapping.TargetId = automaticTargetIdForKind;
				ConfigureTextMappingAfterTargetSelection(loreTextMapping, onPicked, onCancel);
			}
			else
			{
				OpenTextMappingTargetPicker(kind, delegate(string targetId)
				{
					loreTextMapping.TargetId = targetId;
					ConfigureTextMappingAfterTargetSelection(loreTextMapping, onPicked, onCancel);
				}, delegate
				{
					onCancel();
				});
			}
		}, delegate
		{
			onCancel();
		});
	}

	private void ConfigureTextMappingAfterTargetSelection(LoreTextMapping mapping, Action<LoreTextMapping> onPicked, Action onCancel)
	{
		if (mapping == null || onPicked == null)
		{
			onCancel?.Invoke();
		}
		else if (IsTextMappingAgeRangeKind(mapping.Kind) && (!mapping.AgeMin.HasValue || !mapping.AgeMax.HasValue))
		{
			OpenTextMappingAgeRangeEditor(mapping, delegate(LoreTextMapping result)
			{
				ConfigureTextMappingAfterTargetSelection(result, onPicked, onCancel);
			}, onCancel);
		}
		else if (IsStatusMappingKind(mapping.Kind) && string.IsNullOrWhiteSpace(GetTextMappingStatusTrueText(mapping)) && string.IsNullOrWhiteSpace(GetTextMappingStatusFalseText(mapping)))
		{
			OpenTextMappingStatusOutcomeEditor(mapping, onPicked, onCancel);
		}
		else
		{
			onPicked(mapping);
		}
	}

	private static string GetTextMappingKindCategoryLabel(string categoryKey)
	{
		string text = (categoryKey ?? "").Trim();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"status" => "状态判断", 
			"current_npc" => "当前NPC的什么？", 
			"player" => "玩家的什么？", 
			"bound" => "本知识绑定对象的什么？", 
			"kingdom" => "王国的什么？", 
			"settlement" => "定居点的什么？", 
			"clan" => "家族的什么？", 
			"hero" => "英雄的什么？", 
			_ => "其他", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetTextMappingKindCategoryDescription(string categoryKey)
	{
		string text = (categoryKey ?? "").Trim();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"status" => "这些映射会先判断一个状态，再根据结果输出你自己写的真/假文案。真文案和假文案里也可以继续写其他映射词。", 
			"current_npc" => "这些映射会自动从当前互动的 NPC 身上取值，你只需要决定要取它的哪一项信息。", 
			"player" => "这些映射会自动从玩家角色身上取值，你只需要决定要取玩家的哪一项信息。", 
			"bound" => "这些映射会从当前知识条目在条件组合里唯一绑定的王国 / 定居点 / 英雄身上取值。", 
			"kingdom" => "先选一个王国，再决定想取它的什么信息。", 
			"settlement" => "先选一个定居点，再决定想取它的什么信息。", 
			"clan" => "先选一个家族，再决定想取它的什么信息。", 
			"hero" => "先选一个英雄，再决定想取它的什么信息。", 
			_ => "选择一个具体映射项。", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static List<string> GetTextMappingKindCategoryKeys()
	{
		return new List<string> { "status", "current_npc", "player", "bound", "kingdom", "settlement", "clan", "hero" };
	}

	private static void AddTextMappingKindOption(List<TextMappingKindOption> list, string categoryKey, string kind, string label)
	{
		if (list != null && !string.IsNullOrWhiteSpace(kind) && !string.IsNullOrWhiteSpace(label))
		{
			list.Add(new TextMappingKindOption
			{
				CategoryKey = categoryKey,
				Kind = kind,
				Label = label
			});
		}
	}

	private static void AddStatusMappingKindOption(List<TextMappingKindOption> list, string categoryKey, string sourceKey, string statusKey, string label)
	{
		AddTextMappingKindOption(list, categoryKey, BuildStatusMappingKind(sourceKey, statusKey), label);
	}

	private static void AddHeroStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey, string prefix)
	{
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_alive", prefix + "：是否存活");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_dead", prefix + "：是否死亡");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_disabled", prefix + "：是否失能");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_missing", prefix + "：是否失踪/逃亡");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_married", prefix + "：是否已婚");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_widowed", prefix + "：是否丧偶");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_female", prefix + "：是否女性");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_male", prefix + "：是否男性");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_child", prefix + "：是否未成年");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_adult", prefix + "：是否成年");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_in_age_range", prefix + "：是否在年龄段内");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_clan_leader", prefix + "：是否家族族长");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_kingdom_leader", prefix + "：是否王国领袖");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_governor", prefix + "：是否总督");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_prisoner", prefix + "：是否被俘");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_in_settlement", prefix + "：是否在定居点内");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_in_field", prefix + "：是否在野外");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_wanderer", prefix + "：是否流浪者");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_notable", prefix + "：是否要人");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_lord", prefix + "：是否领主");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_merchant", prefix + "：是否商人");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_gang_leader", prefix + "：是否帮派头目");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_artisan", prefix + "：是否工匠");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_preacher", prefix + "：是否传教士");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_headman", prefix + "：是否村长");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_minor_faction_hero", prefix + "：是否小势力英雄");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_party_leader", prefix + "：是否带队");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_player_companion", prefix + "：是否玩家同伴");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_rebel", prefix + "：是否叛军");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_wounded", prefix + "：是否受伤");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_known_to_player", prefix + "：是否被玩家认识");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_children", prefix + "：是否有子女");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_father", prefix + "：是否有父亲");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_mother", prefix + "：是否有母亲");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_home_settlement", prefix + "：是否有家乡定居点");
	}

	private static void AddClanStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey, string prefix)
	{
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_eliminated", prefix + "：是否已灭绝");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_kingdom", prefix + "：是否有所属王国");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_leader", prefix + "：是否有族长");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_settlement", prefix + "：是否拥有任何定居点");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_town", prefix + "：是否拥有任何城镇");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_castle", prefix + "：是否拥有任何城堡");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_village", prefix + "：是否拥有任何村庄");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_members", prefix + "：是否有成员");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_male_members", prefix + "：是否有男性成员");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_female_members", prefix + "：是否有女性成员");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_age_range_members", prefix + "：是否有年龄段成员");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_mercenary", prefix + "：是否雇佣兵家族");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_minor_faction", prefix + "：是否小势力");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_rebel_clan", prefix + "：是否叛军家族");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_noble", prefix + "：是否贵族家族");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_bandit_faction", prefix + "：是否匪帮势力");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_outlaw", prefix + "：是否法外势力");
	}

	private static void AddKingdomStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey, string prefix)
	{
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_eliminated", prefix + "：是否已灭亡");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_leader", prefix + "：是否有领袖");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_ruling_clan", prefix + "：是否有执政家族");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_settlement", prefix + "：是否拥有任何定居点");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_town", prefix + "：是否拥有任何城镇");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_castle", prefix + "：是否拥有任何城堡");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_village", prefix + "：是否拥有任何村庄");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_clan", prefix + "：是否有任何家族");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_lord", prefix + "：是否有任何领主");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_active_policies", prefix + "：是否有生效政策");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_war", prefix + "：是否处于战争中");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_any_allies", prefix + "：是否有盟友");
	}

	private static void AddSettlementStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey, string prefix)
	{
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_active", prefix + "：是否活跃");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_town", prefix + "：是否城镇");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_castle", prefix + "：是否城堡");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_village", prefix + "：是否村庄");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_fortification", prefix + "：是否堡垒");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_hideout", prefix + "：是否藏身处");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_under_siege", prefix + "：是否被围攻");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_under_raid", prefix + "：是否正在被劫掠");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_raided", prefix + "：是否已被劫掠");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_starving", prefix + "：是否饥荒");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "is_rebellious", prefix + "：是否处于叛乱状态");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_port", prefix + "：是否有港口");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_owner", prefix + "：是否有统治者");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_owner_clan", prefix + "：是否有统治家族");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_notables", prefix + "：是否有要人");
		AddStatusMappingKindOption(list, categoryKey, sourceKey, "has_parties", prefix + "：是否有驻留队伍");
	}

	private static List<TextMappingKindOption> BuildTextMappingKindOptions(string categoryKey)
	{
		List<TextMappingKindOption> list = new List<TextMappingKindOption>();
		string text = (categoryKey ?? "").Trim();
		switch (text)
		{
		case "status":
			AddHeroStatusKindOptions(list, text, "current_npc_hero", "当前NPC");
			AddHeroStatusKindOptions(list, text, "player_hero", "玩家");
			AddHeroStatusKindOptions(list, text, "bound_hero", "本知识绑定英雄");
			AddHeroStatusKindOptions(list, text, "hero", "英雄");
			AddClanStatusKindOptions(list, text, "current_npc_clan", "当前NPC所属家族");
			AddClanStatusKindOptions(list, text, "player_clan", "玩家家族");
			AddClanStatusKindOptions(list, text, "bound_settlement_owner_clan", "本知识绑定定居点统治家族");
			AddClanStatusKindOptions(list, text, "bound_hero_clan", "本知识绑定英雄所属家族");
			AddClanStatusKindOptions(list, text, "clan", "家族");
			AddKingdomStatusKindOptions(list, text, "current_npc_kingdom", "当前NPC所属王国");
			AddKingdomStatusKindOptions(list, text, "player_kingdom", "玩家所属王国");
			AddKingdomStatusKindOptions(list, text, "bound_kingdom", "本知识绑定王国");
			AddKingdomStatusKindOptions(list, text, "kingdom", "王国");
			AddSettlementStatusKindOptions(list, text, "current_npc_settlement", "当前NPC所在定居点");
			AddSettlementStatusKindOptions(list, text, "player_settlement", "玩家所在定居点");
			AddSettlementStatusKindOptions(list, text, "bound_settlement", "本知识绑定定居点");
			AddSettlementStatusKindOptions(list, text, "settlement", "定居点");
			break;
		case "current_npc":
			AddTextMappingKindOption(list, text, "current_npc_name", "名字");
			AddTextMappingKindOption(list, text, "current_npc_clan_name", "所属家族");
			AddTextMappingKindOption(list, text, "current_npc_clan_kingdom_name", "所属家族的所属王国");
			AddTextMappingKindOption(list, text, "current_npc_clan_kingdom_leader_name", "所属家族的所属王国领袖");
			AddTextMappingKindOption(list, text, "current_npc_clan_members", "所属家族的成员");
			AddTextMappingKindOption(list, text, "current_npc_clan_male_members", "所属家族的男性成员");
			AddTextMappingKindOption(list, text, "current_npc_clan_female_members", "所属家族的女性成员");
			AddTextMappingKindOption(list, text, "current_npc_clan_age_range_members", "所属家族的年龄段成员");
			AddTextMappingKindOption(list, text, "current_npc_clan_all_towns", "所属家族的所有城镇");
			AddTextMappingKindOption(list, text, "current_npc_clan_all_villages", "所属家族的所有村子");
			AddTextMappingKindOption(list, text, "current_npc_clan_all_settlements", "所属家族的所有定居点");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_name", "所属王国");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_leader_name", "效忠君主");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_ruling_clan_name", "所属王国执政家族");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_culture_name", "所属王国文化");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_initial_home_settlement_name", "所属王国初始都城");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_all_clans", "所属王国全部家族");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_all_lords", "所属王国全部领主");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_all_towns", "所属王国拥有的所有城镇");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_all_castles", "所属王国拥有的所有城堡");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_all_villages", "所属王国拥有的所有村庄");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_all_settlements", "所属王国拥有的所有定居点");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_active_policies", "所属王国生效政策");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_allied_kingdoms", "所属王国盟友");
			AddTextMappingKindOption(list, text, "current_npc_kingdom_war_factions", "所属王国交战势力");
			AddTextMappingKindOption(list, text, "current_npc_spouse_name", "配偶");
			AddTextMappingKindOption(list, text, "current_npc_father_name", "父亲");
			AddTextMappingKindOption(list, text, "current_npc_mother_name", "母亲");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_name", "所在定居点");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_owner_clan_name", "所在定居点统治家族");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_owner_leader_name", "所在定居点统治者");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_owner_kingdom_name", "所在定居点所属王国");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_owner_kingdom_leader_name", "所在定居点所属王国领袖");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_culture_name", "所在定居点文化");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_notables", "所在定居点要人");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_parties", "所在定居点驻留队伍");
			AddTextMappingKindOption(list, text, "current_npc_current_settlement_bound_villages", "所在定居点绑定村庄");
			break;
		case "player":
			AddTextMappingKindOption(list, text, "player_name", "名字");
			AddTextMappingKindOption(list, text, "player_clan_name", "家族");
			AddTextMappingKindOption(list, text, "player_clan_kingdom_name", "家族所属王国");
			AddTextMappingKindOption(list, text, "player_clan_kingdom_leader_name", "家族所属王国领袖");
			AddTextMappingKindOption(list, text, "player_clan_members", "家族成员");
			AddTextMappingKindOption(list, text, "player_clan_male_members", "家族男性成员");
			AddTextMappingKindOption(list, text, "player_clan_female_members", "家族女性成员");
			AddTextMappingKindOption(list, text, "player_clan_age_range_members", "家族年龄段成员");
			AddTextMappingKindOption(list, text, "player_clan_all_towns", "家族的所有城镇");
			AddTextMappingKindOption(list, text, "player_clan_all_villages", "家族的所有村子");
			AddTextMappingKindOption(list, text, "player_clan_all_settlements", "家族的所有定居点");
			AddTextMappingKindOption(list, text, "player_kingdom_name", "所属王国");
			AddTextMappingKindOption(list, text, "player_kingdom_leader_name", "效忠君主");
			AddTextMappingKindOption(list, text, "player_spouse_name", "配偶");
			AddTextMappingKindOption(list, text, "player_current_settlement_name", "所在定居点");
			break;
		case "bound":
			AddTextMappingKindOption(list, text, "bound_kingdom_name", "王国名称");
			AddTextMappingKindOption(list, text, "bound_kingdom_leader_name", "王国领袖");
			AddTextMappingKindOption(list, text, "bound_kingdom_ruling_clan_name", "王国执政家族");
			AddTextMappingKindOption(list, text, "bound_kingdom_culture_name", "王国文化");
			AddTextMappingKindOption(list, text, "bound_kingdom_initial_home_settlement_name", "王国初始都城");
			AddTextMappingKindOption(list, text, "bound_kingdom_all_clans", "王国全部家族");
			AddTextMappingKindOption(list, text, "bound_kingdom_all_lords", "王国全部领主");
			AddTextMappingKindOption(list, text, "bound_kingdom_all_towns", "王国拥有的所有城镇");
			AddTextMappingKindOption(list, text, "bound_kingdom_all_castles", "王国拥有的所有城堡");
			AddTextMappingKindOption(list, text, "bound_kingdom_all_villages", "王国拥有的所有村庄");
			AddTextMappingKindOption(list, text, "bound_kingdom_all_settlements", "王国拥有的所有定居点");
			AddTextMappingKindOption(list, text, "bound_kingdom_active_policies", "王国生效政策");
			AddTextMappingKindOption(list, text, "bound_kingdom_allied_kingdoms", "王国盟友");
			AddTextMappingKindOption(list, text, "bound_kingdom_war_factions", "王国交战势力");
			AddTextMappingKindOption(list, text, "bound_settlement_name", "定居点名称");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_name", "定居点统治家族");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_kingdom_name", "定居点统治家族的所属王国");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_kingdom_leader_name", "定居点统治家族的所属王国领袖");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_members", "定居点统治家族的成员");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_male_members", "定居点统治家族的男性成员");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_female_members", "定居点统治家族的女性成员");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_age_range_members", "定居点统治家族的年龄段成员");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_all_towns", "定居点统治家族的所有城镇");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_all_villages", "定居点统治家族的所有村子");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_clan_all_settlements", "定居点统治家族的所有定居点");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_leader_name", "定居点统治者");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_kingdom_name", "定居点所属王国");
			AddTextMappingKindOption(list, text, "bound_settlement_owner_kingdom_leader_name", "定居点所属王国领袖");
			AddTextMappingKindOption(list, text, "bound_settlement_culture_name", "定居点文化");
			AddTextMappingKindOption(list, text, "bound_settlement_notables", "定居点要人");
			AddTextMappingKindOption(list, text, "bound_settlement_parties", "定居点驻留队伍");
			AddTextMappingKindOption(list, text, "bound_settlement_bound_villages", "定居点绑定村庄");
			AddTextMappingKindOption(list, text, "bound_hero_name", "英雄名字");
			AddTextMappingKindOption(list, text, "bound_hero_clan_name", "英雄所属家族");
			AddTextMappingKindOption(list, text, "bound_hero_clan_kingdom_name", "英雄所属家族的所属王国");
			AddTextMappingKindOption(list, text, "bound_hero_clan_kingdom_leader_name", "英雄所属家族的所属王国领袖");
			AddTextMappingKindOption(list, text, "bound_hero_clan_members", "英雄所属家族的成员");
			AddTextMappingKindOption(list, text, "bound_hero_clan_male_members", "英雄所属家族的男性成员");
			AddTextMappingKindOption(list, text, "bound_hero_clan_female_members", "英雄所属家族的女性成员");
			AddTextMappingKindOption(list, text, "bound_hero_clan_age_range_members", "英雄所属家族的年龄段成员");
			AddTextMappingKindOption(list, text, "bound_hero_clan_all_towns", "英雄所属家族的所有城镇");
			AddTextMappingKindOption(list, text, "bound_hero_clan_all_villages", "英雄所属家族的所有村子");
			AddTextMappingKindOption(list, text, "bound_hero_clan_all_settlements", "英雄所属家族的所有定居点");
			AddTextMappingKindOption(list, text, "bound_hero_kingdom_name", "英雄所属王国");
			AddTextMappingKindOption(list, text, "bound_hero_kingdom_leader_name", "英雄效忠君主");
			AddTextMappingKindOption(list, text, "bound_hero_spouse_name", "英雄配偶");
			AddTextMappingKindOption(list, text, "bound_hero_father_name", "英雄父亲");
			AddTextMappingKindOption(list, text, "bound_hero_mother_name", "英雄母亲");
			AddTextMappingKindOption(list, text, "bound_hero_current_settlement_name", "英雄所在定居点");
			break;
		case "kingdom":
			AddTextMappingKindOption(list, text, "kingdom_name", "名称");
			AddTextMappingKindOption(list, text, "kingdom_leader_name", "当前领袖");
			AddTextMappingKindOption(list, text, "kingdom_ruling_clan_name", "执政家族");
			AddTextMappingKindOption(list, text, "kingdom_culture_name", "文化");
			AddTextMappingKindOption(list, text, "kingdom_initial_home_settlement_name", "初始都城");
			AddTextMappingKindOption(list, text, "kingdom_all_clans", "全部家族");
			AddTextMappingKindOption(list, text, "kingdom_all_lords", "全部领主");
			AddTextMappingKindOption(list, text, "kingdom_all_towns", "拥有的所有城镇");
			AddTextMappingKindOption(list, text, "kingdom_all_castles", "拥有的所有城堡");
			AddTextMappingKindOption(list, text, "kingdom_all_villages", "拥有的所有村庄");
			AddTextMappingKindOption(list, text, "kingdom_all_settlements", "拥有的所有定居点");
			AddTextMappingKindOption(list, text, "kingdom_active_policies", "生效政策");
			AddTextMappingKindOption(list, text, "kingdom_allied_kingdoms", "盟友");
			AddTextMappingKindOption(list, text, "kingdom_war_factions", "交战势力");
			break;
		case "settlement":
			AddTextMappingKindOption(list, text, "settlement_name", "名称");
			AddTextMappingKindOption(list, text, "settlement_owner_leader_name", "统治者");
			AddTextMappingKindOption(list, text, "settlement_owner_clan_name", "统治家族");
			AddTextMappingKindOption(list, text, "settlement_owner_kingdom_name", "所属王国");
			AddTextMappingKindOption(list, text, "settlement_owner_kingdom_leader_name", "所属王国领袖");
			AddTextMappingKindOption(list, text, "settlement_culture_name", "文化");
			AddTextMappingKindOption(list, text, "settlement_notables", "要人");
			AddTextMappingKindOption(list, text, "settlement_parties", "驻留队伍");
			AddTextMappingKindOption(list, text, "settlement_bound_villages", "绑定村庄");
			break;
		case "clan":
			AddTextMappingKindOption(list, text, "clan_leader_name", "当前族长");
			AddTextMappingKindOption(list, text, "clan_name", "名称");
			AddTextMappingKindOption(list, text, "clan_kingdom_name", "所属王国");
			AddTextMappingKindOption(list, text, "clan_kingdom_leader_name", "所属王国领袖");
			AddTextMappingKindOption(list, text, "clan_members", "成员");
			AddTextMappingKindOption(list, text, "clan_male_members", "男性成员");
			AddTextMappingKindOption(list, text, "clan_female_members", "女性成员");
			AddTextMappingKindOption(list, text, "clan_age_range_members", "年龄段成员");
			AddTextMappingKindOption(list, text, "clan_all_towns", "统治的所有城镇");
			AddTextMappingKindOption(list, text, "clan_all_villages", "统治的所有村子");
			AddTextMappingKindOption(list, text, "clan_all_settlements", "统治的所有定居点");
			break;
		case "hero":
			AddTextMappingKindOption(list, text, "hero_name", "当前名字");
			AddTextMappingKindOption(list, text, "hero_clan_name", "所属家族");
			AddTextMappingKindOption(list, text, "hero_kingdom_name", "所属王国");
			AddTextMappingKindOption(list, text, "hero_kingdom_leader_name", "效忠君主");
			AddTextMappingKindOption(list, text, "hero_spouse_name", "配偶");
			AddTextMappingKindOption(list, text, "hero_father_name", "父亲");
			AddTextMappingKindOption(list, text, "hero_mother_name", "母亲");
			AddTextMappingKindOption(list, text, "hero_current_settlement_name", "当前定居点");
			break;
		}
		return list;
	}

	private static List<TextMappingKindOption> BuildAllTextMappingKindOptions()
	{
		List<TextMappingKindOption> list = new List<TextMappingKindOption>();
		foreach (string textMappingKindCategoryKey in GetTextMappingKindCategoryKeys())
		{
			list.AddRange(BuildTextMappingKindOptions(textMappingKindCategoryKey));
		}
		return list;
	}

	private static bool TextMappingKindOptionMatchesQuery(TextMappingKindOption option, string query)
	{
		if (option == null)
		{
			return false;
		}
		string text = (query ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		string[] array = text.ToLowerInvariant().Split(new char[4] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return true;
		}
		string text2 = (GetTextMappingKindCategoryLabel(option.CategoryKey) + " " + option.Label + " " + GetTextMappingKindLabel(option.Kind) + " " + option.Kind).ToLowerInvariant();
		return array.All((string part) => text2.Contains(part));
	}

	private void OpenTextMappingKindSearchResults(string query, Action<string> onPickKind, Action onCancel, int page)
	{
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Expected O, but got Unknown
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Expected O, but got Unknown
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Expected O, but got Unknown
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Expected O, but got Unknown
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Expected O, but got Unknown
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Expected O, but got Unknown
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Expected O, but got Unknown
		if (onPickKind == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		string text = (query ?? "").Trim();
		List<TextMappingKindOption> list = (from x in BuildAllTextMappingKindOptions()
			where TextMappingKindOptionMatchesQuery(x, text)
			select x).OrderBy((TextMappingKindOption x) => GetTextMappingKindCategoryLabel(x.CategoryKey), StringComparer.OrdinalIgnoreCase).ThenBy((TextMappingKindOption x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((TextMappingKindOption x) => x.Kind ?? "", StringComparer.OrdinalIgnoreCase)
			.ToList();
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / 18.0));
		if (page >= num)
		{
			page = num - 1;
		}
		List<TextMappingKindOption> list2 = list.Skip(page * 18).Take(18).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "重新搜索", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement((object)"__clear__", "返回分类列表", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (TextMappingKindOption item in list2)
		{
			string text2 = "【" + GetTextMappingKindCategoryLabel(item.CategoryKey) + "】" + (item.Label ?? GetTextMappingKindLabel(item.Kind));
			list3.Add(new InquiryElement((object)item.Kind, text2, (ImageIdentifier)null));
		}
		string text3 = "输入关键词搜索具体映射项，可用空格分隔多个词。\n例如：家族 王国 / 定居点 统治 / 配偶 / 年龄 / 状态 存活 / 王国 灭亡 / 定居点 围攻";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text3 = text3 + "\n当前搜索：" + TrimPreview(text, 60);
		}
		text3 += $"\n匹配结果：{list.Count} 项";
		if (list.Count == 0)
		{
			text3 += "\n\n没有匹配项，可以换个关键词继续搜。";
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("搜索映射类型", text3, list3, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingKindPicker(onPickKind, onCancel);
			}
			else
			{
				string text4 = ((selected[0].Identifier as string) ?? "").Trim();
				switch (text4)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索映射类型", "输入关键词，可用空格分隔多个词。\n例如：家族 王国 / 定居点 统治 / 配偶 / 年龄 / 状态 存活 / 王国 灭亡", true, true, "搜索", "返回", (Action<string>)delegate(string input)
					{
						OpenTextMappingKindSearchResults(input, onPickKind, onCancel, 0);
					}, (Action)delegate
					{
						OpenTextMappingKindSearchResults(text, onPickKind, onCancel, page);
					}, false, (Func<string, Tuple<bool, string>>)null, text, ""), false, false);
					break;
				case "__clear__":
					OpenTextMappingKindPicker(onPickKind, onCancel);
					break;
				case "__prev__":
					OpenTextMappingKindSearchResults(text, onPickKind, onCancel, page - 1);
					break;
				case "__next__":
					OpenTextMappingKindSearchResults(text, onPickKind, onCancel, page + 1);
					break;
				case "__sep__":
					OpenTextMappingKindSearchResults(text, onPickKind, onCancel, page);
					break;
				default:
					if (string.IsNullOrWhiteSpace(text4))
					{
						OpenTextMappingKindSearchResults(text, onPickKind, onCancel, page);
					}
					else
					{
						onPickKind(text4);
					}
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenTextMappingKindPicker(onPickKind, onCancel);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenTextMappingKindPicker(Action<string> onPickKind, Action onCancel)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected O, but got Unknown
		if (onPickKind == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__search__", "搜索映射类型", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (string textMappingKindCategoryKey in GetTextMappingKindCategoryKeys())
		{
			list.Add(new InquiryElement((object)textMappingKindCategoryKey, GetTextMappingKindCategoryLabel(textMappingKindCategoryKey), (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("你想取谁的什么？", "先选一个方向，或者直接搜索具体映射项。除了普通映射，现在也支持状态判断。组合很多时，优先用搜索会更快。", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text = ((selected[0].Identifier as string) ?? "").Trim();
				string text2 = text;
				string text3 = text2;
				if (!(text3 == "__search__"))
				{
					if (text3 == "__sep__")
					{
						OpenTextMappingKindPicker(onPickKind, onCancel);
					}
					else if (string.IsNullOrWhiteSpace(text))
					{
						onCancel();
					}
					else
					{
						OpenTextMappingKindPickerByCategory(text, onPickKind, onCancel);
					}
				}
				else
				{
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索映射类型", "输入关键词，可用空格分隔多个词。\n例如：家族 王国 / 定居点 统治 / 配偶 / 年龄 / 状态 存活 / 王国 灭亡", true, true, "搜索", "返回", (Action<string>)delegate(string input)
					{
						OpenTextMappingKindSearchResults(input, onPickKind, onCancel, 0);
					}, (Action)delegate
					{
						OpenTextMappingKindPicker(onPickKind, onCancel);
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onCancel();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenTextMappingKindPickerByCategory(string categoryKey, Action<string> onPickKind, Action onCancel)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Expected O, but got Unknown
		if (onPickKind == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		string categoryKey2 = (categoryKey ?? "").Trim();
		List<TextMappingKindOption> list = BuildTextMappingKindOptions(categoryKey2);
		if (list.Count == 0)
		{
			OpenTextMappingKindPicker(onPickKind, onCancel);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (TextMappingKindOption item in list)
		{
			list2.Add(new InquiryElement((object)item.Kind, item.Label, (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择映射类型", GetTextMappingKindCategoryDescription(categoryKey2), list2, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingKindPicker(onPickKind, onCancel);
			}
			else
			{
				string text = ((selected[0].Identifier as string) ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					OpenTextMappingKindPicker(onPickKind, onCancel);
				}
				else
				{
					onPickKind(text);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenTextMappingKindPicker(onPickKind, onCancel);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenKeywordMenu(LoreRule rule, Action onReturn)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__add__", "添加关键词（一次一个）", (ImageIdentifier)null));
		if (rule.Keywords.Count > 0)
		{
			list.Add(new InquiryElement((object)"__remove__", "删除关键词", (ImageIdentifier)null));
		}
		foreach (string keyword in rule.Keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				list.Add(new InquiryElement((object)("__k__" + keyword), "关键词：" + keyword, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑关键词", "一次输入一个关键词，确定后会加入列表。", list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenKeywordMenu(rule, onReturn);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__add__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData("添加关键词", "请输入一个关键词：", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						//IL_0089: Unknown result type (might be due to invalid IL or missing references)
						//IL_0093: Expected O, but got Unknown
						//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
						//IL_0108: Expected O, but got Unknown
						string text2 = (input ?? "").Trim();
						string kwNorm = NormalizeKeywordForCompare(text2);
						if (string.IsNullOrEmpty(kwNorm))
						{
							OpenKeywordMenu(rule, onReturn);
						}
						else
						{
							bool flag = false;
							try
							{
								flag = rule.Keywords.Any((string x) => string.Equals(NormalizeKeywordForCompare(x), kwNorm, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag = false;
							}
							string foundRuleId;
							if (flag)
							{
								InformationManager.DisplayMessage(new InformationMessage("添加失败：该关键词已存在于本知识条目中。"));
								OpenKeywordMenu(rule, onReturn);
							}
							else if (TryFindRuleIdByKeyword(text2, rule.Id, out foundRuleId))
							{
								InformationManager.DisplayMessage(new InformationMessage("添加失败：关键词已被其他知识条目占用（关键词=" + text2 + "，RuleId=" + foundRuleId + "）。"));
								OpenKeywordMenu(rule, onReturn);
							}
							else
							{
								rule.Keywords.Add(text2);
								TouchRuleData();
								OpenKeywordMenu(rule, onReturn);
							}
						}
					}, (Action)delegate
					{
						OpenKeywordMenu(rule, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
				}
				else if (text == "__remove__")
				{
					OpenKeywordRemoveMenu(rule, onReturn);
				}
				else
				{
					OpenKeywordMenu(rule, onReturn);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenKeywordRemoveMenu(LoreRule rule, Action onReturn)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string keyword in rule.Keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				list.Add(new InquiryElement((object)keyword, keyword, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除关键词", "选择要删除的关键词：", list, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenKeywordMenu(rule, onReturn);
			}
			else
			{
				string kw = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(kw))
				{
					rule.Keywords.RemoveAll((string x) => string.Equals(x, kw, StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenKeywordMenu(rule, onReturn);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenKeywordMenu(rule, onReturn);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenRagShortTextMenu(LoreRule rule, Action onReturn)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		EnsureRagShortTexts(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__add__", "添加RAG专用短句（一次一条）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"__generate__", "一键生成RAG专用短句", (ImageIdentifier)null));
		if (rule.RagShortTexts.Count > 0)
		{
			list.Add(new InquiryElement((object)"__remove__", "删除RAG专用短句", (ImageIdentifier)null));
		}
		foreach (string ragShortText in rule.RagShortTexts)
		{
			string text = NormalizeKeywordForCompare(ragShortText);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new InquiryElement((object)("__r__" + text), "RAG短句：" + text, (ImageIdentifier)null));
			}
		}
		string text2 = "用于知识检索与重排的短句。建议写成便于提问匹配的简短描述，不要直接复制完整提示词正文；手动填写每条最多 " + 100 + " 字符，AI 自动生成会限制在 " + 50 + " 字符内，max_tokens 上限为 " + 5000 + "。";
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑RAG专用短句", text2, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenRagShortTextMenu(rule, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "__add__":
					InformationManager.ShowTextInquiry(new TextInquiryData("添加RAG专用短句", "请输入一条RAG专用短句（最多 " + 100 + " 字符）：", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						//IL_006e: Unknown result type (might be due to invalid IL or missing references)
						//IL_0078: Expected O, but got Unknown
						//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
						//IL_00dd: Expected O, but got Unknown
						string text3 = NormalizeKeywordForCompare(input);
						if (string.IsNullOrEmpty(text3))
						{
							OpenRagShortTextMenu(rule, onReturn);
						}
						else if (text3.Length > 100)
						{
							InformationManager.DisplayMessage(new InformationMessage("添加失败：RAG专用短句不能超过 " + 100 + " 字符。"));
							OpenRagShortTextMenu(rule, onReturn);
						}
						else
						{
							bool flag = false;
							try
							{
								flag = rule.RagShortTexts.Any((string x) => string.Equals(NormalizeKeywordForCompare(x), text3, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag = false;
							}
							if (flag)
							{
								InformationManager.DisplayMessage(new InformationMessage("添加失败：该RAG专用短句已存在于本知识条目中。"));
							}
							else
							{
								rule.RagShortTexts.Add(text3);
								TouchRuleData();
							}
							OpenRagShortTextMenu(rule, onReturn);
						}
					}, (Action)delegate
					{
						OpenRagShortTextMenu(rule, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
					break;
				case "__generate__":
					GenerateRagShortTextsByLlm(rule, delegate
					{
						OpenRagShortTextMenu(rule, onReturn);
					});
					break;
				case "__remove__":
					OpenRagShortTextRemoveMenu(rule, onReturn);
					break;
				default:
					OpenRagShortTextMenu(rule, onReturn);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenRagShortTextRemoveMenu(LoreRule rule, Action onReturn)
	{
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		EnsureRagShortTexts(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string ragShortText in rule.RagShortTexts)
		{
			string text = NormalizeKeywordForCompare(ragShortText);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new InquiryElement((object)text, text, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除RAG专用短句", "选择要删除的RAG专用短句：", list, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRagShortTextMenu(rule, onReturn);
			}
			else
			{
				string text2 = NormalizeKeywordForCompare(selected[0].Identifier as string);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					rule.RagShortTexts.RemoveAll((string x) => string.Equals(NormalizeKeywordForCompare(x), text2, StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenRagShortTextMenu(rule, onReturn);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenRagShortTextMenu(rule, onReturn);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static string BuildRagShortTextGenerationSystemPrompt()
	{
		return "你是一个本地RAG知识库的“检索短句生成器”。\n你的输出会直接用于知识召回与重排，不是给NPC直接说的话。\n请严格遵守：\n1) 只围绕提供的知识材料生成，不得编造；\n2) 每条都要显式保留实体名、主题词或关键概念，便于语义检索命中；\n3) 可以写成简洁描述句或简洁提问式短句，但必须适合RAG程序检索；\n4) 禁止第一人称口吻、抒情、解释、提示词说明、系统术语、ACTION标签；\n5) 禁止直接照抄长提示词正文，只保留可检索的核心事实；\n6) 每条最多 " + 50 + " 个字符；\n7) 只输出 JSON 数组字符串，例如 [\"...\",\"...\"]；禁止 Markdown 代码块、标题、序号和额外说明。";
	}

	private static string BuildRagShortTextGenerationUserPrompt(LoreRule rule, int targetCount, IEnumerable<string> existingForPrompt = null)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			if (targetCount <= 0)
			{
				targetCount = 3;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string text = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				stringBuilder.AppendLine("规则ID: " + text);
			}
			List<string> list = new List<string>();
			try
			{
				if (rule.Keywords != null)
				{
					foreach (string keyword in rule.Keywords)
					{
						string text2 = NormalizeKeywordForCompare(keyword);
						if (!string.IsNullOrEmpty(text2))
						{
							list.Add(text2);
							if (list.Count >= 12)
							{
								break;
							}
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("关键词: " + ((list.Count > 0) ? string.Join(" / ", list) : "（无）"));
			List<string> list2 = new List<string>();
			try
			{
				IEnumerable<string> enumerable = existingForPrompt ?? rule.RagShortTexts;
				if (enumerable != null)
				{
					foreach (string item in enumerable)
					{
						string text3 = NormalizeKeywordForCompare(item);
						if (!string.IsNullOrEmpty(text3))
						{
							list2.Add(text3);
							if (list2.Count >= 12)
							{
								break;
							}
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("已有RAG短句: " + ((list2.Count > 0) ? string.Join(" | ", list2) : "（无）"));
			List<string> list3 = new List<string>();
			try
			{
				if (rule.Variants != null)
				{
					for (int i = 0; i < rule.Variants.Count; i++)
					{
						LoreVariant loreVariant = rule.Variants[i];
						if (loreVariant == null)
						{
							continue;
						}
						string text4 = (loreVariant.Content ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(text4))
						{
							if (text4.Length > 220)
							{
								text4 = text4.Substring(0, 220).Trim() + "...";
							}
							list3.Add($"#{i + 1} {BuildWhenLabel(loreVariant.When)}: {text4}");
							if (list3.Count >= 6)
							{
								break;
							}
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("提示词内容：");
			if (list3.Count <= 0)
			{
				stringBuilder.AppendLine("（无）");
			}
			else
			{
				foreach (string item2 in list3)
				{
					stringBuilder.AppendLine(item2);
				}
			}
			stringBuilder.AppendLine("任务：请基于“关键词 + 提示词内容”，生成 " + targetCount + " 条新的 RAG专用短句。");
			stringBuilder.AppendLine("生成要求：");
			stringBuilder.AppendLine("1) 每条 8~" + 50 + " 字，中文自然，不要太虚；");
			stringBuilder.AppendLine("2) 每条都要能帮助程序匹配玩家提问，尽量直接点明主题、实体与核心事实；");
			stringBuilder.AppendLine("3) 不要直接复制原提示词正文的大段表达；");
			stringBuilder.AppendLine("4) 不要与“已有RAG短句”重复；");
			stringBuilder.AppendLine("5) 只输出 JSON 数组字符串。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static List<string> ParseRagShortTextCandidates(string raw, int maxCount)
	{
		List<string> result = new List<string>();
		if (maxCount <= 0)
		{
			maxCount = 3;
		}
		if (string.IsNullOrWhiteSpace(raw))
		{
			return result;
		}
		string text = raw.Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			int num = text.IndexOf('\n');
			if (num >= 0)
			{
				int num2 = text.LastIndexOf("```", StringComparison.Ordinal);
				if (num2 > num)
				{
					text = text.Substring(num + 1, num2 - num - 1).Trim();
				}
			}
		}
		bool flag = false;
		try
		{
			JArray val = JArray.Parse(text);
			foreach (JToken item in val)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(((object)item)?.ToString() ?? "");
			}
			flag = true;
		}
		catch
		{
		}
		if (!flag)
		{
			try
			{
				JObject val2 = JObject.Parse(text);
				JArray val3 = null;
				JToken obj2 = val2["rag_short_texts"];
				JArray val4 = (JArray)(object)((obj2 is JArray) ? obj2 : null);
				if (val4 != null)
				{
					val3 = val4;
				}
				else
				{
					JToken obj3 = val2["items"];
					JArray val5 = (JArray)(object)((obj3 is JArray) ? obj3 : null);
					if (val5 != null)
					{
						val3 = val5;
					}
					else
					{
						JToken obj4 = val2["data"];
						JArray val6 = (JArray)(object)((obj4 is JArray) ? obj4 : null);
						if (val6 != null)
						{
							val3 = val6;
						}
					}
				}
				if (val3 != null)
				{
					foreach (JToken item2 in val3)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(((object)item2)?.ToString() ?? "");
					}
					flag = true;
				}
			}
			catch
			{
			}
		}
		if (!flag)
		{
			string[] array = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string raw2 in array2)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(raw2);
			}
		}
		return result;
		void addCandidate(string raw3)
		{
			string text2 = NormalizeRagShortTextCandidate(raw3);
			if (!string.IsNullOrWhiteSpace(text2) && !result.Any((string x) => string.Equals(x, text2, StringComparison.OrdinalIgnoreCase)))
			{
				result.Add(text2);
			}
		}
	}

	private static string NormalizeRagShortTextCandidate(string raw)
	{
		try
		{
			string text = (raw ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.StartsWith("```", StringComparison.Ordinal) || string.Equals(text, "```", StringComparison.Ordinal))
			{
				return string.Empty;
			}
			if (string.Equals(text, "json", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "```json", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			while (text.StartsWith("-") || text.StartsWith("*") || text.StartsWith("•"))
			{
				text = text.Substring(1).Trim();
			}
			int i;
			for (i = 0; i < text.Length && char.IsDigit(text[i]); i++)
			{
			}
			if (i > 0 && i < text.Length)
			{
				char c = text[i];
				if (c == '.' || c == '、' || c == ')' || c == '）' || c == ':' || c == '：')
				{
					text = text.Substring(i + 1).Trim();
				}
			}
			text = text.Trim().Trim('"').Trim('\'')
				.Trim();
			while (text.EndsWith(",") || text.EndsWith("，") || text.EndsWith(";") || text.EndsWith("；"))
			{
				text = text.Substring(0, text.Length - 1).Trim();
			}
			if (text.IndexOf("[ACTION:", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return string.Empty;
			}
			if (text.IndexOf("提示词", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("RAG", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("检索短句", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return string.Empty;
			}
			text = NormalizeKeywordForCompare(text);
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.Length > 50)
			{
				text = text.Substring(0, 50).Trim();
			}
			if (text.Length < 4)
			{
				return string.Empty;
			}
			return text;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static List<string> BuildDeterministicRagShortTextFallback(LoreRule rule, int maxCount)
	{
		List<string> list = new List<string>();
		if (maxCount <= 0)
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list2 = new List<string>();
		try
		{
			if (rule?.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					string text = NormalizeKeywordForCompare(keyword);
					if (!string.IsNullOrWhiteSpace(text))
					{
						list2.Add(text);
						if (list2.Count >= 6)
						{
							break;
						}
					}
				}
			}
		}
		catch
		{
		}
		foreach (string item in list2)
		{
			add(item + "是什么");
			if (list.Count >= maxCount)
			{
				return list;
			}
			add("介绍" + item);
			if (list.Count >= maxCount)
			{
				return list;
			}
			add(item + "的背景与特点");
			if (list.Count >= maxCount)
			{
				return list;
			}
		}
		try
		{
			if (rule?.Variants != null)
			{
				foreach (LoreVariant variant in rule.Variants)
				{
					string text2 = NormalizeRagShortTextCandidate(variant?.Content);
					if (!string.IsNullOrWhiteSpace(text2))
					{
						add(text2);
						if (list.Count >= maxCount)
						{
							return list;
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
		void add(string raw)
		{
			string text3 = NormalizeRagShortTextCandidate(raw);
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				list.Add(text3);
			}
		}
	}

	private void GenerateRagShortTextsByLlm(LoreRule rule, Action onDone)
	{
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Expected O, but got Unknown
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Expected O, but got Unknown
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Expected O, but got Unknown
		if (rule == null)
		{
			onDone?.Invoke();
			return;
		}
		if (onDone == null)
		{
			onDone = delegate
			{
			};
		}
		EnsureRagShortTexts(rule);
		List<string> list = new List<string>();
		try
		{
			if (rule.RagShortTexts != null)
			{
				foreach (string ragShortText in rule.RagShortTexts)
				{
					string text = NormalizeKeywordForCompare(ragShortText);
					if (!string.IsNullOrWhiteSpace(text) && !list.Any((string x) => string.Equals(x, text, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
			list.Clear();
		}
		string text2 = BuildRagShortTextGenerationUserPrompt(rule, 3, list);
		if (string.IsNullOrWhiteSpace(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("[知识] 生成失败：当前知识缺少可供生成的内容。"));
			onDone();
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage("[知识] 正在生成 RAG专用短句，请稍候..."));
		try
		{
			string systemPrompt = BuildRagShortTextGenerationSystemPrompt();
			string raw = RequestLlmTextOnce(systemPrompt, text2, 5000, 0.2f);
			List<string> list2 = ParseRagShortTextCandidates(raw, Math.Max(6, 6));
			if (list2.Count <= 0)
			{
				list2 = BuildDeterministicRagShortTextFallback(rule, 3);
			}
			int num = 0;
			foreach (string item in list2)
			{
				string text3 = NormalizeRagShortTextCandidate(item);
				if (!string.IsNullOrWhiteSpace(text3) && !rule.RagShortTexts.Any((string x) => string.Equals(NormalizeKeywordForCompare(x), text3, StringComparison.OrdinalIgnoreCase)))
				{
					rule.RagShortTexts.Add(text3);
					num++;
					if (num >= 3)
					{
						break;
					}
				}
			}
			if (num > 0)
			{
				TouchRuleData();
				InformationManager.DisplayMessage(new InformationMessage("[知识] 已生成并添加 " + num + " 条 RAG专用短句。"));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("[知识] 未生成新的 RAG专用短句，可能与现有内容重复。"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[Knowledge] 生成RAG专用短句失败: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("[知识] 生成RAG专用短句失败：" + TrimPreview(ex.Message, 120)));
		}
		onDone();
	}

	private void OpenPrototypeMenu(LoreRule rule, Action onReturn)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		InformationManager.DisplayMessage(new InformationMessage("[知识] 原型句功能已停用。请直接维护“关键词 + RAG专用短句 + 提示词”。"));
		onReturn?.Invoke();
	}

	private void OpenPrototypeViewMenu(LoreRule rule, Action onReturn, int page, string query)
	{
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Expected O, but got Unknown
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Expected O, but got Unknown
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Expected O, but got Unknown
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Expected O, but got Unknown
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Expected O, but got Unknown
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Expected O, but got Unknown
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Expected O, but got Unknown
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.SemanticPrototypes == null)
		{
			rule.SemanticPrototypes = new List<string>();
		}
		List<string> list = new List<string>();
		try
		{
			foreach (string semanticPrototype in rule.SemanticPrototypes)
			{
				string text = (semanticPrototype ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		string q = (query ?? "").Trim();
		List<string> filtered = list;
		if (!string.IsNullOrWhiteSpace(q))
		{
			string ql = q.ToLowerInvariant();
			filtered = list.Where((string x) => (x ?? "").ToLowerInvariant().Contains(ql)).ToList();
		}
		int count = filtered.Count;
		int num = ((count <= 0) ? 1 : ((int)Math.Ceiling((double)count / 12.0)));
		if (page < 0)
		{
			page = 0;
		}
		if (page >= num)
		{
			page = num - 1;
		}
		if (page < 0)
		{
			page = 0;
		}
		int num2 = page * 12;
		List<string> list2 = filtered.Skip(num2).Take(12).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "搜索原型句", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(q))
		{
			list3.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list3.Add(new InquiryElement((object)"__back__", "返回原型句菜单", (ImageIdentifier)null));
		if (list2.Count > 0)
		{
			list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		}
		for (int num3 = 0; num3 < list2.Count; num3++)
		{
			int num4 = num2 + num3;
			string s = list2[num3];
			string text2 = $"{num4 + 1}. {TrimPreview(s, 90)}";
			list3.Add(new InquiryElement((object)("__item__" + num4), text2, (ImageIdentifier)null));
		}
		string text3 = $"共 {count} 条";
		if (!string.IsNullOrWhiteSpace(q))
		{
			text3 = text3 + "（搜索：" + TrimPreview(q, 40) + "）";
		}
		text3 += $"，第 {page + 1}/{num} 页。";
		if (list2.Count <= 0)
		{
			text3 += "\n当前页无内容。";
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("查看原型句", text3, list3, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Expected O, but got Unknown
			//IL_021b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0227: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenPrototypeViewMenu(rule, onReturn, page, q);
			}
			else
			{
				string text4 = selected[0].Identifier as string;
				switch (text4)
				{
				case "__back__":
					OpenPrototypeMenu(rule, onReturn);
					return;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索原型句", "输入关键词：", true, true, "搜索", "返回", (Action<string>)delegate(string input)
					{
						OpenPrototypeViewMenu(rule, onReturn, 0, (input ?? "").Trim());
					}, (Action)delegate
					{
						OpenPrototypeViewMenu(rule, onReturn, page, q);
					}, false, (Func<string, Tuple<bool, string>>)null, q, ""), false, false);
					return;
				case "__clear__":
					OpenPrototypeViewMenu(rule, onReturn, 0, "");
					return;
				case "__prev__":
					OpenPrototypeViewMenu(rule, onReturn, page - 1, q);
					return;
				case "__next__":
					OpenPrototypeViewMenu(rule, onReturn, page + 1, q);
					return;
				default:
				{
					if (text4.StartsWith("__item__", StringComparison.Ordinal) && int.TryParse(text4.Substring("__item__".Length), out var result) && result >= 0 && result < filtered.Count)
					{
						string text5 = filtered[result] ?? "";
						InformationManager.ShowInquiry(new InquiryData("原型句详情", text5, true, false, "返回", "", (Action)delegate
						{
							OpenPrototypeViewMenu(rule, onReturn, page, q);
						}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
						return;
					}
					break;
				}
				case null:
					break;
				}
				OpenPrototypeViewMenu(rule, onReturn, page, q);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenPrototypeMenu(rule, onReturn);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static string BuildPrototypeGenerationUserPrompt(LoreRule rule, int targetCount, IEnumerable<string> existingForPrompt = null)
	{
		try
		{
			if (rule == null)
			{
				return "";
			}
			if (targetCount <= 0)
			{
				targetCount = 1;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string text = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				stringBuilder.AppendLine("规则ID: " + text);
			}
			List<string> list = new List<string>();
			try
			{
				if (rule.Keywords != null)
				{
					foreach (string keyword in rule.Keywords)
					{
						string text2 = (keyword ?? "").Trim();
						if (!string.IsNullOrEmpty(text2))
						{
							if (list.Count >= 12)
							{
								break;
							}
							list.Add(text2);
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("关键词: " + ((list.Count > 0) ? string.Join(" / ", list) : "（无）"));
			List<string> list2 = new List<string>();
			try
			{
				if (rule.RagShortTexts != null)
				{
					foreach (string ragShortText in rule.RagShortTexts)
					{
						string text3 = (ragShortText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(text3))
						{
							if (list2.Count >= 12)
							{
								break;
							}
							list2.Add(text3);
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("RAG专用短句: " + ((list2.Count > 0) ? string.Join(" | ", list2) : "（无）"));
			List<string> list3 = new List<string>();
			try
			{
				IEnumerable<string> enumerable = existingForPrompt ?? rule.SemanticPrototypes;
				if (enumerable != null)
				{
					foreach (string item in enumerable)
					{
						string text4 = (item ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(text4))
						{
							if (list3.Count >= 12)
							{
								break;
							}
							list3.Add(text4);
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("已有原型句: " + ((list3.Count > 0) ? string.Join(" | ", list3) : "（无）"));
			stringBuilder.AppendLine("注意：本次生成只允许参考“关键词”和“RAG专用短句”，不要参考提示词内容（Variants）。");
			stringBuilder.AppendLine("请生成 " + targetCount + " 条“语义原型句”，都必须表达同一意图但换不同说法。");
			stringBuilder.AppendLine("必须符合：");
			stringBuilder.AppendLine("1) 每条 8~30 字，中文自然口语；");
			stringBuilder.AppendLine("2) 禁止包含 ACTION 标签、系统词、解释性文字；");
			stringBuilder.AppendLine("3) 不要与“已有原型句”重复；");
			stringBuilder.AppendLine("4) 只输出 JSON 数组字符串，例如 [\"...\",\"...\"]；禁止 Markdown 代码块；");
			stringBuilder.AppendLine("5) 每条必须完整句，不要半句，不要前后引号，不要末尾逗号。");
			stringBuilder.AppendLine("6) 语义范围仅围绕关键词展开，不能引入关键词之外的新主题。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static List<string> ParsePrototypeCandidates(string raw, int maxCount)
	{
		List<string> result = new List<string>();
		if (maxCount <= 0)
		{
			maxCount = 12;
		}
		if (string.IsNullOrWhiteSpace(raw))
		{
			return result;
		}
		string text = raw.Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			int num = text.IndexOf('\n');
			if (num >= 0)
			{
				int num2 = text.LastIndexOf("```", StringComparison.Ordinal);
				if (num2 > num)
				{
					text = text.Substring(num + 1, num2 - num - 1).Trim();
				}
			}
		}
		bool flag = false;
		try
		{
			JArray val = JArray.Parse(text);
			foreach (JToken item in val)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(((object)item)?.ToString() ?? "");
			}
			flag = true;
		}
		catch
		{
		}
		if (!flag)
		{
			try
			{
				JObject val2 = JObject.Parse(text);
				JArray val3 = null;
				JToken obj2 = val2["prototypes"];
				JArray val4 = (JArray)(object)((obj2 is JArray) ? obj2 : null);
				if (val4 != null)
				{
					val3 = val4;
				}
				else
				{
					JToken obj3 = val2["items"];
					JArray val5 = (JArray)(object)((obj3 is JArray) ? obj3 : null);
					if (val5 != null)
					{
						val3 = val5;
					}
					else
					{
						JToken obj4 = val2["data"];
						JArray val6 = (JArray)(object)((obj4 is JArray) ? obj4 : null);
						if (val6 != null)
						{
							val3 = val6;
						}
					}
				}
				if (val3 != null)
				{
					foreach (JToken item2 in val3)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(((object)item2)?.ToString() ?? "");
					}
					flag = true;
				}
			}
			catch
			{
			}
		}
		if (!flag)
		{
			try
			{
				int num3 = text.IndexOf('[');
				int num4 = text.LastIndexOf(']');
				if (num3 >= 0 && num4 > num3)
				{
					string text2 = text.Substring(num3, num4 - num3 + 1);
					JArray val7 = JArray.Parse(text2);
					foreach (JToken item3 in val7)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(((object)item3)?.ToString() ?? "");
					}
					flag = true;
				}
			}
			catch
			{
			}
		}
		if (!flag)
		{
			string[] array = text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			string[] array3 = array2;
			foreach (string raw2 in array3)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(raw2);
			}
		}
		return result;
		void addCandidate(string raw3)
		{
			string t = NormalizePrototypeText(raw3);
			if (!string.IsNullOrWhiteSpace(t) && !result.Any((string x) => string.Equals(x, t, StringComparison.OrdinalIgnoreCase)))
			{
				result.Add(t);
			}
		}
	}

	private static string NormalizePrototypeText(string raw)
	{
		try
		{
			string text = (raw ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.StartsWith("```", StringComparison.Ordinal) || text.Equals("```", StringComparison.Ordinal))
			{
				return string.Empty;
			}
			if (string.Equals(text, "json", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			if (string.Equals(text, "```json", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			if (text.StartsWith("`") && text.EndsWith("`") && text.Length >= 2)
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			while (text.EndsWith(",") || text.EndsWith("，") || text.EndsWith(";") || text.EndsWith("；"))
			{
				text = text.Substring(0, text.Length - 1).Trim();
			}
			if ((text.StartsWith("\"") && (text.EndsWith("\"") || text.EndsWith("\","))) || (text.StartsWith("'") && (text.EndsWith("'") || text.EndsWith("',"))))
			{
				text = text.Trim().TrimEnd(',').Trim();
			}
			if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			if (text.StartsWith("'") && text.EndsWith("'") && text.Length >= 2)
			{
				text = text.Substring(1, text.Length - 2).Trim();
			}
			while (text.StartsWith("-") || text.StartsWith("*") || text.StartsWith("•"))
			{
				text = text.Substring(1).Trim();
			}
			int i;
			for (i = 0; i < text.Length && char.IsDigit(text[i]); i++)
			{
			}
			if (i > 0 && i < text.Length)
			{
				char c = text[i];
				if (c == '.' || c == '、' || c == ')' || c == '）' || c == ':' || c == '：')
				{
					text = text.Substring(i + 1).Trim();
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.IndexOf("[ACTION:", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return string.Empty;
			}
			if (text.StartsWith("[") || text.EndsWith("]"))
			{
				return string.Empty;
			}
			if (text.Length < 4)
			{
				return string.Empty;
			}
			if (text.Length > 48)
			{
				text = text.Substring(0, 48).Trim();
			}
			return text;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static List<string> BuildDeterministicPrototypeFallback(LoreRule rule, int maxCount)
	{
		List<string> list = new List<string>();
		if (maxCount <= 0)
		{
			return list;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> topics = new List<string>();
		HashSet<string> topicSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (rule?.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					addTopic(keyword);
				}
			}
		}
		catch
		{
		}
		if (topics.Count > 24)
		{
			topics = topics.Take(24).ToList();
		}
		string text = ((topics.Count > 0) ? topics[0] : "这件事");
		string[] array = new string[12]
		{
			"你现在想聊{0}吗", "你是在问{0}的情况吗", "我们继续说{0}这件事", "你想确认{0}的细节吗", "这话题和{0}有关吗", "你提到的核心是{0}吗", "先把{0}说清楚", "你更关心{0}的哪一部分", "我们围绕{0}继续谈", "关于{0}你还想知道什么",
			"你是想推进{0}这个话题吗", "你希望我解释{0}吗"
		};
		foreach (string item in topics)
		{
			string[] array2 = array;
			string[] array3 = array2;
			foreach (string format in array3)
			{
				add(string.Format(format, item));
				if (list.Count >= maxCount)
				{
					return list;
				}
			}
		}
		string[] array4 = array;
		string[] array5 = array4;
		foreach (string format2 in array5)
		{
			add(string.Format(format2, text));
			if (list.Count >= maxCount)
			{
				return list;
			}
		}
		int num = 1;
		while (list.Count < maxCount && num <= 20)
		{
			add("我们继续把" + text + "讲具体一些");
			add("你现在主要在问" + text);
			add("这次对话的重点是" + text);
			num++;
		}
		return list;
		void add(string raw)
		{
			if (list.Count < maxCount)
			{
				string text2 = NormalizePrototypeText(raw);
				if (!string.IsNullOrWhiteSpace(text2) && seen.Add(text2))
				{
					list.Add(text2);
				}
			}
		}
		void addTopic(string raw)
		{
			string text2 = NormalizePrototypeText(raw);
			if (!string.IsNullOrWhiteSpace(text2) && topicSeen.Add(text2))
			{
				topics.Add(text2);
			}
		}
	}

	private static string RequestLlmTextOnce(string systemPrompt, string userPrompt, int maxTokens, float? temperature = null)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
		//IL_015e: Expected O, but got Unknown
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Expected O, but got Unknown
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Expected O, but got Unknown
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || string.IsNullOrWhiteSpace(settings.ApiKey))
		{
			throw new Exception("API Key 未配置。");
		}
		string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
		if (string.IsNullOrWhiteSpace(effectiveApiUrl))
		{
			throw new Exception("API 地址为空。");
		}
		JObject val = new JObject
		{
			["model"] = JToken.op_Implicit(settings.ModelName),
			["max_tokens"] = JToken.op_Implicit(Math.Max(1, Math.Min(5000, maxTokens))),
			["stream"] = JToken.op_Implicit(false)
		};
		if (temperature.HasValue)
		{
			float num = Math.Max(0f, Math.Min(1.5f, temperature.Value));
			val["temperature"] = JToken.op_Implicit(num);
		}
		JArray val2 = new JArray();
		val2.Add((JToken)new JObject
		{
			["role"] = JToken.op_Implicit("system"),
			["content"] = JToken.op_Implicit(systemPrompt ?? "")
		});
		val2.Add((JToken)new JObject
		{
			["role"] = JToken.op_Implicit("user"),
			["content"] = JToken.op_Implicit(userPrompt ?? "")
		});
		JArray val3 = val2;
		val["messages"] = (JToken)(object)val3;
		string text = ((JToken)val).ToString((Formatting)0, Array.Empty<JsonConverter>());
		using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(45.0));
		HttpRequestMessage val4 = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
		try
		{
			val4.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
			val4.Content = (HttpContent)new StringContent(text, Encoding.UTF8, "application/json");
			HttpResponseMessage result = ((HttpMessageInvoker)DuelSettings.GlobalClient).SendAsync(val4, cancellationTokenSource.Token).GetAwaiter().GetResult();
			try
			{
				string result2 = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				if (!result.IsSuccessStatusCode)
				{
					string text2 = (result2 ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (text2.Length > 180)
					{
						text2 = text2.Substring(0, 180);
					}
					throw new Exception($"API请求失败: {result.StatusCode} {text2}");
				}
				try
				{
					JObject val5 = JObject.Parse(result2);
					string text3 = ((string)((JToken)val5).SelectToken("choices[0].message.content")) ?? ((string)((JToken)val5).SelectToken("choices[0].text")) ?? ((string)((JToken)val5).SelectToken("output_text")) ?? ((string)((JToken)val5).SelectToken("content")) ?? ((string)((JToken)val5).SelectToken("text"));
					if (string.IsNullOrWhiteSpace(text3))
					{
						throw new Exception("API 响应为空。");
					}
					return text3.Trim();
				}
				catch (Exception ex)
				{
					throw new Exception("API 响应解析失败: " + ex.Message);
				}
			}
			finally
			{
				((IDisposable)result)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val4)?.Dispose();
		}
	}

	private void GenerateSemanticPrototypesByLlm(LoreRule rule, Action onDone)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		InformationManager.DisplayMessage(new InformationMessage("[知识] 原型句功能已停用。"));
		onDone?.Invoke();
	}

	private void OpenPrototypeRemoveMenu(LoreRule rule, Action onReturn)
	{
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Expected O, but got Unknown
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.SemanticPrototypes == null)
		{
			rule.SemanticPrototypes = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string semanticPrototype in rule.SemanticPrototypes)
		{
			string text = (semanticPrototype ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new InquiryElement((object)text, TrimPreview(text, 80), (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除原型句", "选择要删除的原型句：", list, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPrototypeMenu(rule, onReturn);
			}
			else
			{
				string p = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(p))
				{
					rule.SemanticPrototypes.RemoveAll((string x) => string.Equals((x ?? "").Trim(), p.Trim(), StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenPrototypeMenu(rule, onReturn);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenPrototypeMenu(rule, onReturn);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenVariantMenu(LoreRule rule, Action onReturn)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Expected O, but got Unknown
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__add__", "新增提示词", (ImageIdentifier)null));
		for (int num = 0; num < rule.Variants.Count; num++)
		{
			LoreVariant loreVariant = rule.Variants[num];
			if (loreVariant != null)
			{
				string arg = BuildWhenLabel(loreVariant.When);
				string text = (loreVariant.Content ?? "").Trim();
				if (text.Length > 24)
				{
					text = text.Substring(0, 24) + "...";
				}
				string text2 = $"#{num + 1} {arg}  {text}";
				list.Add(new InquiryElement((object)num, text2, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑提示词", "条件留空=通用；填任意条件=专属。命中时会选最具体的一条。", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenVariantMenu(rule, onReturn);
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				if (text3 == "__add__")
				{
					CreateVariant(rule, delegate
					{
						OpenVariantMenu(rule, onReturn);
					});
				}
				else if (selected[0].Identifier is int num2)
				{
					if (num2 < 0 || num2 >= rule.Variants.Count)
					{
						OpenVariantMenu(rule, onReturn);
					}
					else
					{
						OpenVariantEditor(rule, num2, delegate
						{
							OpenVariantMenu(rule, onReturn);
						});
					}
				}
				else
				{
					OpenVariantMenu(rule, onReturn);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void CreateVariant(LoreRule rule, Action onReturn)
	{
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null)
		{
			rule.Variants = new List<LoreVariant>();
		}
		LoreVariant loreVariant = new LoreVariant();
		loreVariant.Priority = 0;
		loreVariant.When = new LoreWhen();
		loreVariant.Content = "";
		rule.Variants.Add(loreVariant);
		TouchRuleData();
		int idx = rule.Variants.Count - 1;
		OpenVariantEditor(rule, idx, onReturn, isNewVariant: true);
	}

	private void OpenVariantEditor(LoreRule rule, int idx, Action onReturn, bool isNewVariant = false)
	{
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Expected O, but got Unknown
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Expected O, but got Unknown
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Expected O, but got Unknown
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Expected O, but got Unknown
		if (rule == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (rule.Variants == null || idx < 0 || idx >= rule.Variants.Count)
		{
			onReturn();
			return;
		}
		LoreVariant v = rule.Variants[idx];
		if (v == null)
		{
			onReturn();
			return;
		}
		string text = BuildWhenLabel(v.When);
		string text2 = (v.Content ?? "").Trim();
		string text3 = (text2 ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		string text4 = TrimPreview(text2, 220);
		bool flag = !string.IsNullOrEmpty(text3) && text3.Length > 220;
		string text5 = "当前条件：" + text + "\n\n当前内容预览：\n" + (string.IsNullOrEmpty(text4) ? "（空）" : text4);
		if (flag)
		{
			text5 += "\n……（内容过长，仅显示前 220 字）";
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"content", "编辑提示词内容", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"when", "编辑条件", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"delete", "删除此提示词", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("提示词编辑", text5, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenVariantEditor(rule, idx, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "content":
					DevTextEditorHelper.ShowLongTextEditor("编辑提示词内容", "当前条件：" + text, "请输入内容。", v.Content ?? "", delegate(string input)
					{
						int num = FindDuplicateVariantIndex(rule, v.When, idx);
						if (num >= 0)
						{
							ShowDuplicateVariantConditionPrompt(v.When, num);
							OpenVariantEditor(rule, idx, onReturn, isNewVariant);
						}
						else
						{
							v.Content = input ?? "";
							TouchRuleData();
							OpenVariantEditor(rule, idx, onReturn, isNewVariant);
						}
					}, delegate
					{
						OpenVariantEditor(rule, idx, onReturn, isNewVariant);
					}, "确定", "取消");
					break;
				case "when":
				{
					LoreWhen loreWhen = CloneWhen(v.When) ?? new LoreWhen();
					OpenWhenEditor(loreWhen, delegate
					{
						loreWhen = NormalizeWhenForStorage(loreWhen);
						int num = FindDuplicateVariantIndex(rule, loreWhen, idx);
						if (num >= 0)
						{
							ShowDuplicateVariantConditionPrompt(loreWhen, num);
							OpenVariantEditor(rule, idx, onReturn, isNewVariant);
						}
						else
						{
							v.When = loreWhen;
							TouchRuleData();
							OpenVariantEditor(rule, idx, onReturn, isNewVariant);
						}
					});
					break;
				}
				case "delete":
					rule.Variants.RemoveAt(idx);
					TouchRuleData();
					onReturn();
					break;
				default:
					OpenVariantEditor(rule, idx, onReturn);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			if (isNewVariant && idx >= 0 && rule.Variants != null && idx < rule.Variants.Count)
			{
				LoreVariant loreVariant = rule.Variants[idx];
				if (loreVariant != null && FindDuplicateVariantIndex(rule, loreVariant.When, idx) >= 0)
				{
					rule.Variants.RemoveAt(idx);
					TouchRuleData();
				}
			}
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenWhenEditor(LoreWhen when, Action onReturn)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Expected O, but got Unknown
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"culture", "文化（CultureId）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"kingdom", "势力/王国（KingdomId）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"settlement", "定居点（SettlementId）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"role", "身份（大类 + 细分NPC）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"gender", "性别（不限/男/女）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"clan_leader", "是否家族族长（不限/是/否）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"skill", "技能等级（SkillId >= 值）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"clear", "清空全部条件（变成通用）", (ImageIdentifier)null));
		string text = BuildWhenDetail(when);
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑条件", text, list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenWhenEditor(when, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "culture":
					if (when.Cultures == null)
					{
						when.Cultures = new List<string>();
					}
					OpenStringListEditor("文化（CultureId）", when.Cultures, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "kingdom":
					if (when.KingdomIds == null)
					{
						when.KingdomIds = new List<string>();
					}
					OpenStringListEditor("势力/王国（KingdomId）", when.KingdomIds, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "settlement":
					if (when.SettlementIds == null)
					{
						when.SettlementIds = new List<string>();
					}
					OpenStringListEditor("定居点（SettlementId）", when.SettlementIds, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "role":
					OpenRoleEditor(when, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "gender":
					CycleGender(when);
					OpenWhenEditor(when, onReturn);
					break;
				case "clan_leader":
					CycleClanLeader(when);
					OpenWhenEditor(when, onReturn);
					break;
				case "skill":
					OpenSkillMinEditor(when, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "clear":
					when.HeroIds = null;
					when.Cultures = null;
					when.KingdomIds = null;
					when.SettlementIds = null;
					when.Roles = null;
					when.IdentityIds = null;
					when.IsFemale = null;
					when.IsClanLeader = null;
					when.SkillMin = null;
					TouchRuleData();
					OpenWhenEditor(when, onReturn);
					break;
				default:
					OpenWhenEditor(when, onReturn);
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static string ResolveRoleIdentityDisplayText(string encodedId)
	{
		try
		{
			if (!TryParseRoleIdentityId(encodedId, out var kind, out var targetId))
			{
				return (encodedId ?? "").Trim();
			}
			if (kind == "hero")
			{
				Hero val = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals(((MBObjectBase)x).StringId, targetId, StringComparison.OrdinalIgnoreCase)));
				string text = ((val == null) ? null : ((object)val.Name)?.ToString());
				if (string.IsNullOrWhiteSpace(text))
				{
					text = targetId;
				}
				return text + "（HeroId=" + targetId + "）";
			}
			if (kind == "char")
			{
				CharacterObject val2 = null;
				try
				{
					Game current = Game.Current;
					object obj;
					if (current == null)
					{
						obj = null;
					}
					else
					{
						MBObjectManager objectManager = current.ObjectManager;
						obj = ((objectManager != null) ? objectManager.GetObjectTypeList<CharacterObject>() : null);
					}
					MBReadOnlyList<CharacterObject> val3 = (MBReadOnlyList<CharacterObject>)obj;
					if (val3 != null)
					{
						val2 = ((IEnumerable<CharacterObject>)val3).FirstOrDefault((CharacterObject x) => x != null && string.Equals(((MBObjectBase)x).StringId, targetId, StringComparison.OrdinalIgnoreCase));
					}
				}
				catch
				{
				}
				string text2 = ((val2 == null) ? null : ((object)((BasicCharacterObject)val2).Name)?.ToString());
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = targetId;
				}
				return text2 + "（CharacterId=" + targetId + "）";
			}
		}
		catch
		{
		}
		return (encodedId ?? "").Trim();
	}

	private static string BuildLegacyHeroIdentityDisplayText(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		Hero val = null;
		try
		{
			val = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals(((MBObjectBase)x).StringId, text, StringComparison.OrdinalIgnoreCase)));
		}
		catch
		{
		}
		string text2 = ((val == null) ? null : ((object)val.Name)?.ToString());
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = text;
		}
		return text2 + "（HeroId=" + text + "）";
	}

	private static string GetRoleKeyForLegacyHeroId(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Hero val = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals(((MBObjectBase)x).StringId, text, StringComparison.OrdinalIgnoreCase)));
			if (val != null)
			{
				return GetRoleKeyForHero(val);
			}
		}
		catch
		{
		}
		return "commoner";
	}

	private static bool IsRoleIdentitySelected(LoreWhen when, string encodedId)
	{
		if (when == null)
		{
			return false;
		}
		if (ListContainsIgnoreCase(when.IdentityIds, encodedId))
		{
			return true;
		}
		if (TryParseRoleIdentityId(encodedId, out var kind, out var targetId) && string.Equals(kind, "hero", StringComparison.OrdinalIgnoreCase))
		{
			return ListContainsIgnoreCase(when.HeroIds, targetId);
		}
		return false;
	}

	private static void ToggleRoleIdentitySelection(LoreWhen when, string encodedId)
	{
		if (when == null)
		{
			return;
		}
		if (when.IdentityIds == null)
		{
			when.IdentityIds = new List<string>();
		}
		string text = (encodedId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (TryParseRoleIdentityId(text, out var kind, out var targetId) && string.Equals(kind, "hero", StringComparison.OrdinalIgnoreCase))
		{
			bool flag = ListContainsIgnoreCase(when.IdentityIds, text) || ListContainsIgnoreCase(when.HeroIds, targetId);
			when.IdentityIds.RemoveAll((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			when.HeroIds?.RemoveAll((string x) => string.Equals((x ?? "").Trim(), targetId, StringComparison.OrdinalIgnoreCase));
			if (!flag)
			{
				when.IdentityIds.Add(text);
			}
		}
		else
		{
			ToggleListValueIgnoreCase(when.IdentityIds, text);
		}
	}

	private static void ClearRoleSpecificSelections(LoreWhen when, string roleKey)
	{
		if (when == null)
		{
			return;
		}
		if (when.IdentityIds != null)
		{
			when.IdentityIds.RemoveAll((string x) => string.Equals(GetRoleKeyForIdentityId(x), roleKey, StringComparison.OrdinalIgnoreCase));
		}
		if (when.HeroIds != null)
		{
			when.HeroIds.RemoveAll((string x) => string.Equals(GetRoleKeyForLegacyHeroId(x), roleKey, StringComparison.OrdinalIgnoreCase));
		}
	}

	private static string GetRoleKeyForIdentityId(string encodedId)
	{
		try
		{
			if (!TryParseRoleIdentityId(encodedId, out var kind, out var targetId))
			{
				return "";
			}
			if (kind == "hero")
			{
				Hero hero = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && string.Equals(((MBObjectBase)x).StringId, targetId, StringComparison.OrdinalIgnoreCase)));
				return GetRoleKeyForHero(hero);
			}
			if (kind == "char")
			{
				Game current = Game.Current;
				object obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					MBObjectManager objectManager = current.ObjectManager;
					obj = ((objectManager != null) ? objectManager.GetObjectTypeList<CharacterObject>() : null);
				}
				CharacterObject character = ((IEnumerable<CharacterObject>)obj)?.FirstOrDefault((CharacterObject x) => x != null && string.Equals(((MBObjectBase)x).StringId, targetId, StringComparison.OrdinalIgnoreCase));
				return GetRoleKeyForCharacter(character);
			}
		}
		catch
		{
		}
		return "";
	}

	private static int CountSelectedRoleIdentities(LoreWhen when, string roleKey)
	{
		try
		{
			if (when == null)
			{
				return 0;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (when.IdentityIds != null)
			{
				foreach (string identityId in when.IdentityIds)
				{
					if (string.Equals(GetRoleKeyForIdentityId(identityId), roleKey, StringComparison.OrdinalIgnoreCase))
					{
						hashSet.Add((identityId ?? "").Trim());
					}
				}
			}
			if (when.HeroIds != null)
			{
				foreach (string heroId in when.HeroIds)
				{
					if (string.Equals(GetRoleKeyForLegacyHeroId(heroId), roleKey, StringComparison.OrdinalIgnoreCase))
					{
						string text = EncodeRoleIdentityHeroId(heroId);
						if (!string.IsNullOrWhiteSpace(text))
						{
							hashSet.Add(text);
						}
					}
				}
			}
			return hashSet.Count;
		}
		catch
		{
			return 0;
		}
	}

	private static List<RoleDetailOption> BuildRoleDetailOptions(string roleKey, string query)
	{
		List<RoleDetailOption> list = new List<RoleDetailOption>();
		string value = (query ?? "").Trim();
		bool flag = !string.IsNullOrWhiteSpace(value);
		try
		{
			if (roleKey == "lord" || roleKey == "notable" || roleKey == "wanderer")
			{
				List<Hero> list2 = new List<Hero>();
				try
				{
					List<Hero> devEditableHeroListForExternal = MyBehavior.GetDevEditableHeroListForExternal();
					if (devEditableHeroListForExternal != null && devEditableHeroListForExternal.Count > 0)
					{
						list2 = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(((MBObjectBase)h).StringId)).ToList();
					}
				}
				catch
				{
				}
				if (list2.Count == 0)
				{
					foreach (Hero item in (List<Hero>)(object)Hero.AllAliveHeroes)
					{
						if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId))
						{
							list2.Add(item);
						}
					}
				}
				foreach (Hero item2 in list2)
				{
					if (!string.Equals(GetRoleKeyForHero(item2), roleKey, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					string text = ((object)item2.Name)?.ToString();
					if (string.IsNullOrWhiteSpace(text))
					{
						text = ((MBObjectBase)item2).StringId;
					}
					string text2 = text + "（HeroId=" + ((MBObjectBase)item2).StringId + "）";
					if (!flag || text2.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || ((MBObjectBase)item2).StringId.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						string text3 = EncodeRoleIdentityHeroId(((MBObjectBase)item2).StringId);
						if (!string.IsNullOrEmpty(text3))
						{
							list.Add(new RoleDetailOption
							{
								EncodedId = text3,
								Label = text2
							});
						}
					}
				}
			}
			else
			{
				Game current3 = Game.Current;
				object obj2;
				if (current3 == null)
				{
					obj2 = null;
				}
				else
				{
					MBObjectManager objectManager = current3.ObjectManager;
					obj2 = ((objectManager != null) ? objectManager.GetObjectTypeList<CharacterObject>() : null);
				}
				MBReadOnlyList<CharacterObject> val = (MBReadOnlyList<CharacterObject>)obj2;
				if (val != null)
				{
					foreach (CharacterObject item3 in (List<CharacterObject>)(object)val)
					{
						if (item3 == null || string.IsNullOrWhiteSpace(((MBObjectBase)item3).StringId) || !string.Equals(GetRoleKeyForCharacter(item3), roleKey, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string text4 = ((object)((BasicCharacterObject)item3).Name)?.ToString();
						if (string.IsNullOrWhiteSpace(text4))
						{
							text4 = ((MBObjectBase)item3).StringId;
						}
						string text5 = text4 + "（CharacterId=" + ((MBObjectBase)item3).StringId + "）";
						if (!flag || text5.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || ((MBObjectBase)item3).StringId.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							string text6 = EncodeRoleIdentityCharacterId(((MBObjectBase)item3).StringId);
							if (!string.IsNullOrEmpty(text6))
							{
								list.Add(new RoleDetailOption
								{
									EncodedId = text6,
									Label = text5
								});
							}
						}
					}
				}
			}
		}
		catch
		{
		}
		return list.OrderBy((RoleDetailOption x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((RoleDetailOption x) => x.EncodedId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void OpenRoleCategoryEditor(LoreWhen when, string roleKey, Action onReturn, int page = 0, string query = null)
	{
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Expected O, but got Unknown
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Expected O, but got Unknown
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Expected O, but got Unknown
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Expected O, but got Unknown
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Expected O, but got Unknown
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Expected O, but got Unknown
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Expected O, but got Unknown
		//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b5: Expected O, but got Unknown
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.Roles == null)
		{
			when.Roles = new List<string>();
		}
		if (when.IdentityIds == null)
		{
			when.IdentityIds = new List<string>();
		}
		roleKey = (roleKey ?? "").Trim().ToLowerInvariant();
		List<RoleDetailOption> list = BuildRoleDetailOptions(roleKey, query);
		if (when.HeroIds != null && when.HeroIds.Count > 0)
		{
			string value = (query ?? "").Trim();
			bool flag = !string.IsNullOrWhiteSpace(value);
			HashSet<string> hashSet = new HashSet<string>(list.Select((RoleDetailOption x) => (x.EncodedId ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
			foreach (string heroId in when.HeroIds)
			{
				if (!string.Equals(GetRoleKeyForLegacyHeroId(heroId), roleKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string text = EncodeRoleIdentityHeroId(heroId);
				if (!string.IsNullOrWhiteSpace(text) && !hashSet.Contains(text))
				{
					string text2 = BuildLegacyHeroIdentityDisplayText(heroId);
					if (!flag || text2.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || (heroId ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						list.Add(new RoleDetailOption
						{
							EncodedId = text,
							Label = text2
						});
						hashSet.Add(text);
					}
				}
			}
			list = list.OrderBy((RoleDetailOption x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((RoleDetailOption x) => x.EncodedId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		}
		int num = 18;
		int num2 = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)num));
		page = Math.Max(0, Math.Min(page, num2 - 1));
		List<RoleDetailOption> list2 = list.Skip(page * num).Take(num).ToList();
		int num3 = CountSelectedRoleIdentities(when, roleKey);
		bool flag2 = ListContainsIgnoreCase(when.Roles, roleKey);
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__toggle_all__", (flag2 ? "[√] " : "[ ] ") + "整类生效：" + GetRoleDisplayName(roleKey), (ImageIdentifier)null));
		list3.Add(new InquiryElement((object)"__search__", string.IsNullOrWhiteSpace(query) ? "搜索本类具体NPC…" : ("搜索：" + query), (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(query))
		{
			list3.Add(new InquiryElement((object)"__clear_search__", "清除搜索", (ImageIdentifier)null));
		}
		if (num3 > 0)
		{
			list3.Add(new InquiryElement((object)"__clear_specific__", "清空本类细分已选（" + num3 + "）", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page < num2 - 1)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		foreach (RoleDetailOption item in list2)
		{
			bool flag3 = IsRoleIdentitySelected(when, item.EncodedId);
			list3.Add(new InquiryElement((object)item.EncodedId, (flag3 ? "[√] " : "[ ] ") + item.Label, (ImageIdentifier)null));
		}
		string text3 = string.Format("{0}：可直接勾整类，也可只勾本类里的具体NPC。\n当前整类：{1}\n当前细分已选：{2}\n结果总数：{3}，第 {4}/{5} 页", GetRoleDisplayName(roleKey), flag2 ? "已选中" : "未选中", num3, list.Count, page + 1, num2);
		int num4 = Math.Max(1, list3.Count);
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("身份细分 - " + GetRoleDisplayName(roleKey), text3, list3, true, 0, num4, "切换选中", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Expected O, but got Unknown
			//IL_0280: Unknown result type (might be due to invalid IL or missing references)
			//IL_028c: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
			}
			else
			{
				List<string> list4 = (from x in selected
					select x?.Identifier as string into x
					where !string.IsNullOrWhiteSpace(x)
					select x).ToList();
				if (list4.Count == 0)
				{
					OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
				}
				else
				{
					List<string> list5 = list4.Where((string x) => x.StartsWith("__", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
					if (list5.Count > 0)
					{
						if (list4.Count > 1)
						{
							InformationManager.DisplayMessage(new InformationMessage("搜索、翻页、整类切换这类操作项不能和细分NPC一起多选。"));
							OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
						}
						else
						{
							switch (list5[0])
							{
							case "__toggle_all__":
								ToggleListValueIgnoreCase(when.Roles, roleKey);
								TouchRuleData();
								OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
								break;
							case "__search__":
								InformationManager.ShowTextInquiry(new TextInquiryData("搜索 " + GetRoleDisplayName(roleKey), "输入名称、HeroId 或 CharacterId。搜索只在当前大类内进行。", true, true, "搜索", "返回", (Action<string>)delegate(string input)
								{
									OpenRoleCategoryEditor(when, roleKey, onReturn, 0, input);
								}, (Action)delegate
								{
									OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
								}, false, (Func<string, Tuple<bool, string>>)null, query ?? "", ""), false, false);
								break;
							case "__clear_search__":
								OpenRoleCategoryEditor(when, roleKey, onReturn);
								break;
							case "__clear_specific__":
								ClearRoleSpecificSelections(when, roleKey);
								TouchRuleData();
								OpenRoleCategoryEditor(when, roleKey, onReturn, 0, query);
								break;
							case "__prev__":
								OpenRoleCategoryEditor(when, roleKey, onReturn, page - 1, query);
								break;
							case "__next__":
								OpenRoleCategoryEditor(when, roleKey, onReturn, page + 1, query);
								break;
							default:
								OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
								break;
							}
						}
					}
					else
					{
						foreach (string item2 in list4.Distinct(StringComparer.OrdinalIgnoreCase))
						{
							ToggleRoleIdentitySelection(when, item2);
						}
						TouchRuleData();
						OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
					}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenRoleEditor(when, onReturn);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenRoleEditor(LoreWhen when, Action onReturn)
	{
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Expected O, but got Unknown
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Expected O, but got Unknown
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected O, but got Unknown
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.Roles == null)
		{
			when.Roles = new List<string>();
		}
		if (when.IdentityIds == null)
		{
			when.IdentityIds = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		string[] roleCategoryKeys = GetRoleCategoryKeys();
		foreach (string text in roleCategoryKeys)
		{
			bool flag = ListContainsIgnoreCase(when.Roles, text);
			int num2 = CountSelectedRoleIdentities(when, text);
			string text2 = (flag ? "[整类√] " : "") + GetRoleDisplayName(text);
			if (num2 > 0)
			{
				text2 = text2 + "（细分已选 " + num2 + "）";
			}
			list.Add(new InquiryElement((object)text, text2, (ImageIdentifier)null));
		}
		if (when.IdentityIds.Count > 0)
		{
			list.Add(new InquiryElement((object)"__clear_all_specific__", "清空全部细分身份", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("身份条件", "先选一个大类，再进入该类选择具体NPC。整类勾选继续保留；细分选择会在当前大类内搜索和分页。", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRoleEditor(when, onReturn);
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				if (text3 == "__clear_all_specific__")
				{
					when.IdentityIds.Clear();
					TouchRuleData();
					OpenRoleEditor(when, onReturn);
				}
				else
				{
					OpenRoleCategoryEditor(when, text3, onReturn);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static void CycleGender(LoreWhen when)
	{
		if (when != null)
		{
			if (!when.IsFemale.HasValue)
			{
				when.IsFemale = false;
			}
			else if (!when.IsFemale.Value)
			{
				when.IsFemale = true;
			}
			else
			{
				when.IsFemale = null;
			}
			TouchRuleData();
		}
	}

	private static void CycleClanLeader(LoreWhen when)
	{
		if (when != null)
		{
			if (!when.IsClanLeader.HasValue)
			{
				when.IsClanLeader = true;
			}
			else if (when.IsClanLeader.Value)
			{
				when.IsClanLeader = false;
			}
			else
			{
				when.IsClanLeader = null;
			}
			TouchRuleData();
		}
	}

	private void OpenHeroIdPicker(List<string> list, Action onReturn)
	{
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Expected O, but got Unknown
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Expected O, but got Unknown
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected O, but got Unknown
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<Hero> list2 = new List<Hero>();
		try
		{
			List<Hero> devEditableHeroListForExternal = MyBehavior.GetDevEditableHeroListForExternal();
			if (devEditableHeroListForExternal != null && devEditableHeroListForExternal.Count > 0)
			{
				list2 = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(((MBObjectBase)h).StringId)).ToList();
			}
		}
		catch
		{
		}
		if (list2.Count == 0)
		{
			try
			{
				foreach (Hero item in (List<Hero>)(object)Hero.AllAliveHeroes)
				{
					if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId))
					{
						list2.Add(item);
					}
				}
			}
			catch
			{
			}
		}
		list2 = (from h in list2
			orderby (h.Name != (TextObject)null) ? ((object)h.Name).ToString() : "", ((MBObjectBase)h).StringId
			select h).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选NPC。"));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (Hero item2 in list2)
		{
			string text = ((item2.Name != (TextObject)null) ? ((object)item2.Name).ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ((MBObjectBase)item2).StringId;
			}
			string text2 = "";
			try
			{
				text2 = (item2.IsLord ? "领主" : (item2.IsNotable ? "要人" : ((!item2.IsWanderer) ? ((object)item2.Occupation/*cast due to .constrained prefix*/).ToString() : "流浪者")));
			}
			catch
			{
				text2 = "";
			}
			list3.Add(new InquiryElement((object)((MBObjectBase)item2).StringId, text + " (" + text2 + ", " + ((MBObjectBase)item2).StringId + ")", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择NPC", "可多选，确认后加入列表。", list3, true, 0, list3.Count, "添加", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item3 in selected)
				{
					string id = item3.Identifier as string;
					id = (id ?? "").Trim();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenCultureIdPicker(List<string> list, Action onReturn)
	{
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Expected O, but got Unknown
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<CultureObject> list2 = new List<CultureObject>();
		try
		{
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				MBObjectManager objectManager = current.ObjectManager;
				obj = ((objectManager != null) ? objectManager.GetObjectTypeList<CultureObject>() : null);
			}
			MBReadOnlyList<CultureObject> val = (MBReadOnlyList<CultureObject>)obj;
			if (val != null)
			{
				foreach (CultureObject item in (List<CultureObject>)(object)val)
				{
					if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId) && !((BasicCultureObject)item).IsBandit && !(((MBObjectBase)item).StringId == "neutral_culture"))
					{
						list2.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		list2 = (from c in list2
			orderby (((BasicCultureObject)c).Name != (TextObject)null) ? ((object)((BasicCultureObject)c).Name).ToString() : "", ((MBObjectBase)c).StringId
			select c).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选文化。"));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (CultureObject item2 in list2)
		{
			string text = ((((BasicCultureObject)item2).Name != (TextObject)null) ? ((object)((BasicCultureObject)item2).Name).ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ((MBObjectBase)item2).StringId;
			}
			list3.Add(new InquiryElement((object)((MBObjectBase)item2).StringId.ToLower(), text + " (" + ((MBObjectBase)item2).StringId + ")", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val2 = new MultiSelectionInquiryData("选择文化", "可多选，确认后加入列表。", list3, true, 0, list3.Count, "添加", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item3 in selected)
				{
					string id = item3.Identifier as string;
					id = (id ?? "").Trim().ToLower();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val2, false, false);
	}

	private void OpenFactionIdPicker(List<string> list, Action onReturn)
	{
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Expected O, but got Unknown
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Expected O, but got Unknown
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<IFaction> list2 = new List<IFaction>();
		try
		{
			foreach (IFaction faction in Campaign.Current.Factions)
			{
				if (faction != null && !string.IsNullOrWhiteSpace(faction.StringId) && !faction.IsBanditFaction)
				{
					list2.Add(faction);
				}
			}
		}
		catch
		{
		}
		list2 = (from f in list2
			orderby !f.IsKingdomFaction, (f.Name != (TextObject)null) ? ((object)f.Name).ToString() : "", f.StringId
			select f).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选势力。"));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (IFaction item in list2)
		{
			string text = ((item.Name != (TextObject)null) ? ((object)item.Name).ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			string text2 = (item.IsKingdomFaction ? "王国" : "势力");
			list3.Add(new InquiryElement((object)item.StringId.ToLower(), text + " (" + text2 + ", " + item.StringId + ")", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择势力/王国", "可多选，确认后加入列表。", list3, true, 0, list3.Count, "添加", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item2 in selected)
				{
					string id = item2.Identifier as string;
					id = (id ?? "").Trim().ToLower();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenSettlementIdPicker(List<string> list, Action onReturn)
	{
		OpenSettlementIdPickerPaged(list, onReturn, 0, null);
	}

	private void OpenSettlementIdPickerPaged(List<string> list, Action onReturn, int page, string query)
	{
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Expected O, but got Unknown
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Expected O, but got Unknown
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Expected O, but got Unknown
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Expected O, but got Unknown
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Expected O, but got Unknown
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Expected O, but got Unknown
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Expected O, but got Unknown
		//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		if (page < 0)
		{
			page = 0;
		}
		List<Settlement> list2 = new List<Settlement>();
		try
		{
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId))
				{
					list2.Add(item);
				}
			}
		}
		catch
		{
		}
		list2 = list2.OrderBy((Settlement s) => (s.Name != (TextObject)null) ? ((object)s.Name).ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Settlement s) => ((MBObjectBase)s).StringId, StringComparer.OrdinalIgnoreCase).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list2 = list2.Where(delegate(Settlement s)
			{
				string text5 = ((((s != null) ? s.Name : null) != (TextObject)null) ? ((object)s.Name).ToString() : "").Trim().ToLowerInvariant();
				string text6 = (((s != null) ? ((MBObjectBase)s).StringId : null) ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? "未找到可选定居点。" : ("未找到匹配的定居点：" + text)));
			onReturn();
			return;
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)list2.Count / 40.0));
		if (page >= num)
		{
			page = num - 1;
		}
		int count = page * 40;
		List<Settlement> list3 = list2.Skip(count).Take(40).ToList();
		List<InquiryElement> list4 = new List<InquiryElement>();
		list4.Add(new InquiryElement((object)"__search__", "搜索定居点", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list4.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list4.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list4.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list4.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (Settlement item2 in list3)
		{
			string text2 = ((item2.Name != (TextObject)null) ? ((object)item2.Name).ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = ((MBObjectBase)item2).StringId;
			}
			string text3 = (item2.IsVillage ? "村庄" : (item2.IsCastle ? "城堡" : (item2.IsTown ? "城镇" : "定居点")));
			list4.Add(new InquiryElement((object)((MBObjectBase)item2).StringId.ToLowerInvariant(), text2 + " (" + text3 + ", " + ((MBObjectBase)item2).StringId + ")", (ImageIdentifier)null));
		}
		string text4 = $"共 {list2.Count} 个定居点，第 {page + 1}/{num} 页。选择一个后会加入列表。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text4 = text4 + "\n当前搜索：" + TrimPreview(text, 60);
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择定居点", text4, list4, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenSettlementIdPickerPaged(list, onReturn, page, text);
			}
			else
			{
				string text7 = selected[0].Identifier as string;
				switch (text7)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索定居点", "输入定居点名称或 SettlementId：", true, true, "搜索", "取消", (Action<string>)delegate(string input)
					{
						OpenSettlementIdPickerPaged(list, onReturn, 0, input);
					}, (Action)delegate
					{
						OpenSettlementIdPickerPaged(list, onReturn, page, text);
					}, false, (Func<string, Tuple<bool, string>>)null, text, ""), false, false);
					break;
				case "__clear__":
					OpenSettlementIdPickerPaged(list, onReturn, 0, null);
					break;
				case "__prev__":
					OpenSettlementIdPickerPaged(list, onReturn, page - 1, text);
					break;
				case "__next__":
					OpenSettlementIdPickerPaged(list, onReturn, page + 1, text);
					break;
				case "__sep__":
					OpenSettlementIdPickerPaged(list, onReturn, page, text);
					break;
				default:
					text7 = (text7 ?? "").Trim().ToLowerInvariant();
					if (!string.IsNullOrEmpty(text7) && !list.Any((string x) => string.Equals(x, text7, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(text7);
					}
					TouchRuleData();
					onReturn();
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenTextMappingTargetPicker(string kind, Action<string> onPickTargetId, Action onCancel)
	{
		if (onPickTargetId == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var _))
		{
			switch (GetStatusSourceObjectKind(sourceKey))
			{
			case "kingdom":
				OpenKingdomPickerSingle(onPickTargetId, onCancel);
				break;
			case "settlement":
				OpenSettlementPickerSingle(onPickTargetId, onCancel);
				break;
			case "clan":
				OpenClanPickerSingle(onPickTargetId, onCancel);
				break;
			case "hero":
				OpenHeroPickerSingle(onPickTargetId, onCancel);
				break;
			default:
				onCancel();
				break;
			}
		}
		else
		{
			switch ((kind ?? "").Trim())
			{
			case "kingdom_name":
			case "kingdom_leader_name":
			case "kingdom_ruling_clan_name":
			case "kingdom_culture_name":
			case "kingdom_initial_home_settlement_name":
			case "kingdom_all_clans":
			case "kingdom_all_lords":
			case "kingdom_all_towns":
			case "kingdom_all_castles":
			case "kingdom_all_villages":
			case "kingdom_all_settlements":
			case "kingdom_active_policies":
			case "kingdom_allied_kingdoms":
			case "kingdom_war_factions":
				OpenKingdomPickerSingle(onPickTargetId, onCancel);
				break;
			case "settlement_name":
			case "settlement_owner_clan_name":
			case "settlement_owner_leader_name":
			case "settlement_owner_kingdom_name":
			case "settlement_owner_kingdom_leader_name":
			case "settlement_culture_name":
			case "settlement_notables":
			case "settlement_parties":
			case "settlement_bound_villages":
				OpenSettlementPickerSingle(onPickTargetId, onCancel);
				break;
			case "clan_name":
			case "clan_leader_name":
			case "clan_kingdom_name":
			case "clan_kingdom_leader_name":
			case "clan_all_towns":
			case "clan_all_villages":
			case "clan_all_settlements":
			case "clan_members":
			case "clan_male_members":
			case "clan_female_members":
			case "clan_age_range_members":
				OpenClanPickerSingle(onPickTargetId, onCancel);
				break;
			case "hero_name":
			case "hero_clan_name":
			case "hero_kingdom_name":
			case "hero_kingdom_leader_name":
			case "hero_spouse_name":
			case "hero_father_name":
			case "hero_mother_name":
			case "hero_current_settlement_name":
				OpenHeroPickerSingle(onPickTargetId, onCancel);
				break;
			default:
				onCancel();
				break;
			}
		}
	}

	private void OpenKingdomPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Expected O, but got Unknown
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Expected O, but got Unknown
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Expected O, but got Unknown
		if (onPickTargetId == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		List<Kingdom> list = new List<Kingdom>();
		try
		{
			if (Kingdom.All != null)
			{
				foreach (Kingdom item in (List<Kingdom>)(object)Kingdom.All)
				{
					if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId))
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((Kingdom k) => (k.Name != (TextObject)null) ? ((object)k.Name).ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Kingdom k) => ((MBObjectBase)k).StringId, StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选王国。"));
			onCancel();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (Kingdom item2 in list)
		{
			string text = ((item2.Name != (TextObject)null) ? ((object)item2.Name).ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ((MBObjectBase)item2).StringId;
			}
			Hero leader = item2.Leader;
			string text2 = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
			list2.Add(new InquiryElement((object)((MBObjectBase)item2).StringId, string.IsNullOrWhiteSpace(text2) ? (text + " (" + ((MBObjectBase)item2).StringId + ")") : (text + "（现任领袖：" + text2 + "）"), (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择王国", "选择一个王国作为映射目标。", list2, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text3 = ((selected[0].Identifier as string) ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text3))
				{
					onCancel();
				}
				else
				{
					onPickTargetId(text3);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onCancel();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenHeroPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		OpenHeroPickerSinglePaged(onPickTargetId, onCancel, 0, null);
	}

	private void OpenHeroPickerSinglePaged(Action<string> onPickTargetId, Action onCancel, int page, string query)
	{
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Expected O, but got Unknown
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Expected O, but got Unknown
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Expected O, but got Unknown
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Expected O, but got Unknown
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Expected O, but got Unknown
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Expected O, but got Unknown
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Expected O, but got Unknown
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Expected O, but got Unknown
		if (onPickTargetId == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		List<Hero> list = new List<Hero>();
		try
		{
			List<Hero> devEditableHeroListForExternal = MyBehavior.GetDevEditableHeroListForExternal();
			if (devEditableHeroListForExternal != null && devEditableHeroListForExternal.Count > 0)
			{
				list = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(((MBObjectBase)h).StringId)).ToList();
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			try
			{
				foreach (Hero item in (List<Hero>)(object)Hero.AllAliveHeroes)
				{
					if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId))
					{
						list.Add(item);
					}
				}
			}
			catch
			{
			}
		}
		list = (from h in list
			orderby (h.Name != (TextObject)null) ? ((object)h.Name).ToString() : "", ((MBObjectBase)h).StringId
			select h).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Hero h)
			{
				string text4 = ((((h != null) ? h.Name : null) != (TextObject)null) ? ((object)h.Name).ToString() : "").Trim().ToLowerInvariant();
				string text5 = (((h != null) ? ((MBObjectBase)h).StringId : null) ?? "").Trim().ToLowerInvariant();
				return text4.Contains(q) || text5.Contains(q);
			}).ToList();
		}
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? "未找到可选英雄。" : ("未找到匹配的英雄：" + text)));
			onCancel();
			return;
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / 40.0));
		if (page >= num)
		{
			page = num - 1;
		}
		int count = page * 40;
		List<Hero> list2 = list.Skip(count).Take(40).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "搜索英雄", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (Hero item2 in list2)
		{
			string text2 = ((item2.Name != (TextObject)null) ? ((object)item2.Name).ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = ((MBObjectBase)item2).StringId;
			}
			list3.Add(new InquiryElement((object)((MBObjectBase)item2).StringId, text2 + " (" + ((MBObjectBase)item2).StringId + ")", (ImageIdentifier)null));
		}
		string text3 = $"共 {list.Count} 个英雄，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text3 = text3 + "\n当前搜索：" + TrimPreview(text, 60);
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择英雄", text3, list3, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text4 = selected[0].Identifier as string;
				switch (text4)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索英雄", "输入英雄名称或 HeroId：", true, true, "搜索", "取消", (Action<string>)delegate(string input)
					{
						OpenHeroPickerSinglePaged(onPickTargetId, onCancel, 0, input);
					}, (Action)delegate
					{
						OpenHeroPickerSinglePaged(onPickTargetId, onCancel, page, text);
					}, false, (Func<string, Tuple<bool, string>>)null, text, ""), false, false);
					break;
				case "__clear__":
					OpenHeroPickerSinglePaged(onPickTargetId, onCancel, 0, null);
					break;
				case "__prev__":
					OpenHeroPickerSinglePaged(onPickTargetId, onCancel, page - 1, text);
					break;
				case "__next__":
					OpenHeroPickerSinglePaged(onPickTargetId, onCancel, page + 1, text);
					break;
				case "__sep__":
					OpenHeroPickerSinglePaged(onPickTargetId, onCancel, page, text);
					break;
				default:
				{
					string text5 = (text4 ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text5))
					{
						onCancel();
					}
					else
					{
						onPickTargetId(text5);
					}
					break;
				}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onCancel();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenClanPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		OpenClanPickerSinglePaged(onPickTargetId, onCancel, 0, null);
	}

	private void OpenClanPickerSinglePaged(Action<string> onPickTargetId, Action onCancel, int page, string query)
	{
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Expected O, but got Unknown
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Expected O, but got Unknown
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Expected O, but got Unknown
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Expected O, but got Unknown
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Expected O, but got Unknown
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Expected O, but got Unknown
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bd: Expected O, but got Unknown
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Expected O, but got Unknown
		if (onPickTargetId == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		List<Clan> list = new List<Clan>();
		try
		{
			if (Clan.All != null)
			{
				foreach (Clan item in (List<Clan>)(object)Clan.All)
				{
					if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId) && !item.IsBanditFaction)
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((Clan c) => (c.Name != (TextObject)null) ? ((object)c.Name).ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Clan c) => ((MBObjectBase)c).StringId, StringComparer.OrdinalIgnoreCase).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Clan c)
			{
				string text5 = ((((c != null) ? c.Name : null) != (TextObject)null) ? ((object)c.Name).ToString() : "").Trim().ToLowerInvariant();
				string text6 = (((c != null) ? ((MBObjectBase)c).StringId : null) ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? "未找到可选家族。" : ("未找到匹配的家族：" + text)));
			onCancel();
			return;
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / 40.0));
		if (page >= num)
		{
			page = num - 1;
		}
		int count = page * 40;
		List<Clan> list2 = list.Skip(count).Take(40).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "搜索家族", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (Clan item2 in list2)
		{
			string text2 = ((item2.Name != (TextObject)null) ? ((object)item2.Name).ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = ((MBObjectBase)item2).StringId;
			}
			Hero leader = item2.Leader;
			string text3 = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
			list3.Add(new InquiryElement((object)((MBObjectBase)item2).StringId, string.IsNullOrWhiteSpace(text3) ? (text2 + " (" + ((MBObjectBase)item2).StringId + ")") : (text2 + "（当前族长：" + text3 + "）"), (ImageIdentifier)null));
		}
		string text4 = $"共 {list.Count} 个家族，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text4 = text4 + "\n当前搜索：" + TrimPreview(text, 60);
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择家族", text4, list3, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text5 = selected[0].Identifier as string;
				switch (text5)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索家族", "输入家族名称或 ClanId：", true, true, "搜索", "取消", (Action<string>)delegate(string input)
					{
						OpenClanPickerSinglePaged(onPickTargetId, onCancel, 0, input);
					}, (Action)delegate
					{
						OpenClanPickerSinglePaged(onPickTargetId, onCancel, page, text);
					}, false, (Func<string, Tuple<bool, string>>)null, text, ""), false, false);
					break;
				case "__clear__":
					OpenClanPickerSinglePaged(onPickTargetId, onCancel, 0, null);
					break;
				case "__prev__":
					OpenClanPickerSinglePaged(onPickTargetId, onCancel, page - 1, text);
					break;
				case "__next__":
					OpenClanPickerSinglePaged(onPickTargetId, onCancel, page + 1, text);
					break;
				case "__sep__":
					OpenClanPickerSinglePaged(onPickTargetId, onCancel, page, text);
					break;
				default:
				{
					string text6 = (text5 ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text6))
					{
						onCancel();
					}
					else
					{
						onPickTargetId(text6);
					}
					break;
				}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onCancel();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenSettlementPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, 0, null);
	}

	private void OpenSettlementPickerSinglePaged(Action<string> onPickTargetId, Action onCancel, int page, string query)
	{
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Expected O, but got Unknown
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Expected O, but got Unknown
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Expected O, but got Unknown
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Expected O, but got Unknown
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Expected O, but got Unknown
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Expected O, but got Unknown
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Expected O, but got Unknown
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Expected O, but got Unknown
		if (onPickTargetId == null)
		{
			onCancel?.Invoke();
			return;
		}
		if (onCancel == null)
		{
			onCancel = delegate
			{
			};
		}
		if (page < 0)
		{
			page = 0;
		}
		List<Settlement> list = new List<Settlement>();
		try
		{
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item != null && !string.IsNullOrWhiteSpace(((MBObjectBase)item).StringId))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((Settlement s) => (s.Name != (TextObject)null) ? ((object)s.Name).ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Settlement s) => ((MBObjectBase)s).StringId, StringComparer.OrdinalIgnoreCase).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Settlement s)
			{
				string text5 = ((((s != null) ? s.Name : null) != (TextObject)null) ? ((object)s.Name).ToString() : "").Trim().ToLowerInvariant();
				string text6 = (((s != null) ? ((MBObjectBase)s).StringId : null) ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? "未找到可选定居点。" : ("未找到匹配的定居点：" + text)));
			onCancel();
			return;
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / 40.0));
		if (page >= num)
		{
			page = num - 1;
		}
		int count = page * 40;
		List<Settlement> list2 = list.Skip(count).Take(40).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement((object)"__search__", "搜索定居点", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement((object)"__clear__", "清空搜索", (ImageIdentifier)null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement((object)"__prev__", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement((object)"__next__", "下一页", (ImageIdentifier)null));
		}
		list3.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (Settlement item2 in list2)
		{
			string text2 = ((item2.Name != (TextObject)null) ? ((object)item2.Name).ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = ((MBObjectBase)item2).StringId;
			}
			Clan ownerClan = item2.OwnerClan;
			object obj2;
			if (ownerClan == null)
			{
				obj2 = null;
			}
			else
			{
				Hero leader = ownerClan.Leader;
				obj2 = ((leader == null) ? null : ((object)leader.Name)?.ToString());
			}
			if (obj2 == null)
			{
				obj2 = "";
			}
			string text3 = ((string)obj2).Trim();
			list3.Add(new InquiryElement((object)((MBObjectBase)item2).StringId, string.IsNullOrWhiteSpace(text3) ? (text2 + " (" + ((MBObjectBase)item2).StringId + ")") : (text2 + "（当前统治者：" + text3 + "）"), (ImageIdentifier)null));
		}
		string text4 = $"共 {list.Count} 个定居点，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text4 = text4 + "\n当前搜索：" + TrimPreview(text, 60);
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择定居点", text4, list3, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text5 = selected[0].Identifier as string;
				switch (text5)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData("搜索定居点", "输入定居点名称或 SettlementId：", true, true, "搜索", "取消", (Action<string>)delegate(string input)
					{
						OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, 0, input);
					}, (Action)delegate
					{
						OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, page, text);
					}, false, (Func<string, Tuple<bool, string>>)null, text, ""), false, false);
					break;
				case "__clear__":
					OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, 0, null);
					break;
				case "__prev__":
					OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, page - 1, text);
					break;
				case "__next__":
					OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, page + 1, text);
					break;
				case "__sep__":
					OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, page, text);
					break;
				default:
				{
					string text6 = (text5 ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text6))
					{
						onCancel();
					}
					else
					{
						onPickTargetId(text6);
					}
					break;
				}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onCancel();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenStringListEditor(string title, List<string> list, Action onReturn)
	{
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Expected O, but got Unknown
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Expected O, but got Unknown
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		bool canPickHero = title != null && title.IndexOf("HeroId", StringComparison.OrdinalIgnoreCase) >= 0;
		bool canPickCulture = title != null && title.IndexOf("CultureId", StringComparison.OrdinalIgnoreCase) >= 0;
		bool canPickKingdom = title != null && title.IndexOf("KingdomId", StringComparison.OrdinalIgnoreCase) >= 0;
		bool canPickSettlement = title != null && title.IndexOf("SettlementId", StringComparison.OrdinalIgnoreCase) >= 0;
		List<InquiryElement> list2 = new List<InquiryElement>();
		if (canPickHero || canPickCulture || canPickKingdom || canPickSettlement)
		{
			list2.Add(new InquiryElement((object)"__pick__", "从列表选择", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__add__", "手动输入（一次一个）", (ImageIdentifier)null));
		if (list.Count > 0)
		{
			list2.Add(new InquiryElement((object)"__remove__", "删除", (ImageIdentifier)null));
		}
		foreach (string item in list)
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				list2.Add(new InquiryElement((object)("__v__" + item), item, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(title, "一次输入一个值，确定后加入列表。", list2, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
			//IL_015a: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "__pick__":
				{
					Action action = delegate
					{
						TouchRuleData();
						onReturn();
					};
					if (canPickHero)
					{
						OpenHeroIdPicker(list, action);
					}
					else if (canPickCulture)
					{
						OpenCultureIdPicker(list, action);
					}
					else if (canPickKingdom)
					{
						OpenFactionIdPicker(list, action);
					}
					else if (canPickSettlement)
					{
						OpenSettlementIdPicker(list, action);
					}
					else
					{
						action();
					}
					break;
				}
				case "__add__":
					InformationManager.ShowTextInquiry(new TextInquiryData(title, "请输入一个值：", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						string v = (input ?? "").Trim();
						if (!string.IsNullOrEmpty(v) && !list.Any((string x) => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(v);
						}
						TouchRuleData();
						onReturn();
					}, (Action)delegate
					{
						onReturn();
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
					break;
				case "__remove__":
					OpenStringListRemoveMenu(title, list, onReturn);
					break;
				default:
					onReturn();
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenStringListRemoveMenu(string title, List<string> list, Action onReturn)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (list == null)
		{
			list = new List<string>();
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (string item in list)
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				list2.Add(new InquiryElement((object)item, item, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除 - " + title, "选择要删除的项：", list2, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string v = selected[0].Identifier as string;
				if (!string.IsNullOrWhiteSpace(v))
				{
					list.RemoveAll((string x) => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
				}
				TouchRuleData();
				onReturn();
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenSkillMinEditor(LoreWhen when, Action onReturn)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Expected O, but got Unknown
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Expected O, but got Unknown
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.SkillMin == null)
		{
			when.SkillMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__pick__", "从列表选择技能", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"__add__", "手动输入 SkillId", (ImageIdentifier)null));
		if (when.SkillMin.Count > 0)
		{
			list.Add(new InquiryElement((object)"__remove__", "删除技能条件", (ImageIdentifier)null));
		}
		foreach (KeyValuePair<string, int> item in when.SkillMin.OrderBy((KeyValuePair<string, int> k) => k.Key, StringComparer.OrdinalIgnoreCase))
		{
			string text = (item.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement((object)text, text + " >= " + item.Value, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("技能等级条件", "配置 SkillId >= 数值。数值=0 表示无门槛（Skill>=0）；数值<0 表示移除该技能条件。", list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenSkillMinEditor(when, onReturn);
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				switch (text2)
				{
				case "__pick__":
					OpenSkillPicker(delegate(string id)
					{
						OpenSkillMinValueInput(when, id, delegate
						{
							OpenSkillMinEditor(when, onReturn);
						});
					}, delegate
					{
						OpenSkillMinEditor(when, onReturn);
					});
					break;
				case "__add__":
					InformationManager.ShowTextInquiry(new TextInquiryData("手动输入 SkillId", "请输入 SkillId：", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						string text3 = (input ?? "").Trim();
						if (string.IsNullOrEmpty(text3))
						{
							OpenSkillMinEditor(when, onReturn);
						}
						else
						{
							OpenSkillMinValueInput(when, text3, delegate
							{
								OpenSkillMinEditor(when, onReturn);
							});
						}
					}, (Action)delegate
					{
						OpenSkillMinEditor(when, onReturn);
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
					break;
				case "__remove__":
					OpenSkillMinRemoveMenu(when, delegate
					{
						OpenSkillMinEditor(when, onReturn);
					});
					break;
				default:
					if (!string.IsNullOrWhiteSpace(text2))
					{
						OpenSkillMinValueInput(when, text2, delegate
						{
							OpenSkillMinEditor(when, onReturn);
						});
					}
					else
					{
						OpenSkillMinEditor(when, onReturn);
					}
					break;
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenSkillMinRemoveMenu(LoreWhen when, Action onReturn)
	{
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Expected O, but got Unknown
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Expected O, but got Unknown
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.SkillMin == null || when.SkillMin.Count == 0)
		{
			onReturn();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (KeyValuePair<string, int> item in when.SkillMin.OrderBy((KeyValuePair<string, int> k) => k.Key, StringComparer.OrdinalIgnoreCase))
		{
			string text = (item.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement((object)text, text + " >= " + item.Value, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除技能条件", "选择要删除的项：", list, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text2 = (selected[0].Identifier as string) ?? "";
				text2 = text2.Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					when.SkillMin.Remove(text2);
					if (when.SkillMin.Count == 0)
					{
						when.SkillMin = null;
					}
					TouchRuleData();
				}
				onReturn();
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenSkillMinValueInput(LoreWhen when, string skillId, Action onReturn)
	{
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		if (when == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		if (when.SkillMin == null)
		{
			when.SkillMin = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		string id = (skillId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			onReturn();
			return;
		}
		int num = 0;
		try
		{
			if (when.SkillMin.TryGetValue(id, out var value))
			{
				num = value;
			}
		}
		catch
		{
			num = 0;
		}
		InformationManager.ShowTextInquiry(new TextInquiryData("设置技能下限", "请输入最低技能等级（整数）：", true, true, "确定", "取消", (Action<string>)delegate(string input)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Expected O, but got Unknown
			string s = (input ?? "").Trim();
			if (!int.TryParse(s, out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入有效整数。"));
				onReturn();
			}
			else
			{
				if (result < 0)
				{
					when.SkillMin.Remove(id);
					if (when.SkillMin.Count == 0)
					{
						when.SkillMin = null;
					}
				}
				else
				{
					when.SkillMin[id] = result;
				}
				TouchRuleData();
				onReturn();
			}
		}, (Action)delegate
		{
			onReturn();
		}, false, (Func<string, Tuple<bool, string>>)null, num.ToString(), ""), false, false);
	}

	private void OpenSkillPicker(Action<string> onPickSkillId, Action onReturn)
	{
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Expected O, but got Unknown
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Expected O, but got Unknown
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Expected O, but got Unknown
		if (onPickSkillId == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<SkillObject> list = new List<SkillObject>();
		try
		{
			EnsureSkillByIdCache();
			if (_skillByIdCache != null)
			{
				foreach (SkillObject value in _skillByIdCache.Values)
				{
					if (value != null && !string.IsNullOrWhiteSpace(((MBObjectBase)value).StringId))
					{
						list.Add(value);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((SkillObject s) => (((PropertyObject)s).Name != (TextObject)null) ? ((object)((PropertyObject)s).Name).ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((SkillObject s) => ((MBObjectBase)s).StringId, StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到可选技能。"));
			onReturn();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (SkillObject item in list)
		{
			string text = ((((PropertyObject)item).Name != (TextObject)null) ? ((object)((PropertyObject)item).Name).ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ((MBObjectBase)item).StringId;
			}
			list2.Add(new InquiryElement((object)((MBObjectBase)item).StringId, text + " (" + ((MBObjectBase)item).StringId + ")", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("选择技能", "选择一个技能后，继续设置最低等级。", list2, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text2 = (selected[0].Identifier as string) ?? "";
				text2 = text2.Trim();
				if (string.IsNullOrEmpty(text2))
				{
					onReturn();
				}
				else
				{
					onPickSkillId(text2);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static string BuildWhenLabel(LoreWhen when)
	{
		if (when == null)
		{
			return "通用";
		}
		List<string> list = new List<string>();
		if (when.Cultures != null && when.Cultures.Count > 0)
		{
			list.Add("文化");
		}
		if (when.KingdomIds != null && when.KingdomIds.Count > 0)
		{
			list.Add("势力");
		}
		if (when.SettlementIds != null && when.SettlementIds.Count > 0)
		{
			list.Add("定居点");
		}
		if (when.Roles != null && when.Roles.Count > 0)
		{
			list.Add("身份");
		}
		if ((when.HeroIds != null && when.HeroIds.Count > 0) || (when.IdentityIds != null && when.IdentityIds.Count > 0))
		{
			list.Add("细分身份");
		}
		if (when.IsFemale.HasValue)
		{
			list.Add("性别");
		}
		if (when.IsClanLeader.HasValue)
		{
			list.Add("族长");
		}
		if (when.SkillMin != null && when.SkillMin.Count > 0)
		{
			list.Add("技能");
		}
		if (list.Count == 0)
		{
			return "通用";
		}
		return "专属(" + string.Join("+", list) + ")";
	}

	private static string BuildWhenDetail(LoreWhen when)
	{
		if (when == null)
		{
			return "当前：通用（无条件）";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("当前条件：");
		stringBuilder.AppendLine("CultureId: " + ListOrEmpty(when.Cultures));
		stringBuilder.AppendLine("KingdomId: " + ListOrEmpty(when.KingdomIds));
		stringBuilder.AppendLine("SettlementId: " + ListOrEmpty(when.SettlementIds));
		stringBuilder.AppendLine("Roles: " + ListOrEmpty(FormatRoleListForDisplay(when.Roles)));
		stringBuilder.AppendLine("细分身份: " + ListOrEmpty(FormatEffectiveIdentityListForDisplay(when)));
		string text = "（空）";
		try
		{
			if (when.SkillMin != null && when.SkillMin.Count > 0)
			{
				text = string.Join(", ", when.SkillMin.Where((KeyValuePair<string, int> kv) => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0).OrderBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.OrdinalIgnoreCase).Select(delegate(KeyValuePair<string, int> kv)
				{
					KeyValuePair<string, int> keyValuePair = kv;
					string text2 = (keyValuePair.Key ?? "").Trim();
					keyValuePair = kv;
					return text2 + ">=" + keyValuePair.Value;
				}));
			}
		}
		catch
		{
			text = "（空）";
		}
		stringBuilder.AppendLine("SkillMin: " + text);
		stringBuilder.AppendLine("Gender: " + ((!when.IsFemale.HasValue) ? "不限" : (when.IsFemale.Value ? "女" : "男")));
		stringBuilder.AppendLine("ClanLeader: " + ((!when.IsClanLeader.HasValue) ? "不限" : (when.IsClanLeader.Value ? "是" : "否")));
		return stringBuilder.ToString();
	}

	private static List<string> FormatRoleListForDisplay(List<string> list)
	{
		List<string> list2 = new List<string>();
		try
		{
			if (list == null || list.Count == 0)
			{
				return list2;
			}
			foreach (string item in list)
			{
				string text = (item ?? "").Trim();
				if (!string.IsNullOrEmpty(text))
				{
					list2.Add(GetRoleDisplayName(text) + "（" + text + "）");
				}
			}
		}
		catch
		{
		}
		return list2;
	}

	private static List<string> FormatIdentityListForDisplay(List<string> list)
	{
		List<string> list2 = new List<string>();
		try
		{
			if (list == null || list.Count == 0)
			{
				return list2;
			}
			foreach (string item in list)
			{
				string text = ResolveRoleIdentityDisplayText(item);
				if (!string.IsNullOrWhiteSpace(text))
				{
					list2.Add(text);
				}
			}
		}
		catch
		{
		}
		return list2;
	}

	private static List<string> FormatEffectiveIdentityListForDisplay(LoreWhen when)
	{
		List<string> list = new List<string>();
		try
		{
			if (when == null)
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (when.HeroIds != null)
			{
				foreach (string heroId in when.HeroIds)
				{
					string text = BuildLegacyHeroIdentityDisplayText(heroId);
					if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
					{
						list.Add(text);
					}
				}
			}
			if (when.IdentityIds != null)
			{
				foreach (string identityId in when.IdentityIds)
				{
					string text2 = ResolveRoleIdentityDisplayText(identityId);
					if (!string.IsNullOrWhiteSpace(text2) && hashSet.Add(text2))
					{
						list.Add(text2);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static string ListOrEmpty(List<string> list)
	{
		if (list == null || list.Count == 0)
		{
			return "（空）";
		}
		return string.Join(", ", list.Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

	private LoreRule FindRule(string id)
	{
		if (_file == null || _file.Rules == null)
		{
			return null;
		}
		return _file.Rules.FirstOrDefault((LoreRule r) => r != null && r.Id == id);
	}
}
