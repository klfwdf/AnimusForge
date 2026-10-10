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
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public class KnowledgeLibraryBehavior : CampaignBehaviorBase
{
	public const int RagShortTextMaxLength = 100;

	public const int RagShortTextGeneratedMaxLength = 50;

	private const int RagShortTextGenerationMaxTokens = 5000;

	private const int RagShortTextAutoGenerateCount = 3;

	private struct LoreContextCacheItem
	{
		public long Version;

		public long Ticks;

		public string Value;
	}

	public class RuleIndexItem
	{
		public string Id;

		public string Label;
	}

	public class KnowledgeFile
	{
		public int Version = 1;

		public string PlayerAppearance = "";

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

	// 词汇映射的最小存储单元：
	// SourceText 是提示词里要被替换的原文本，Kind/TargetId 决定去哪里取值，
	// EmptyValueText 和 TrueText/FalseText 分别用于空值兜底和状态判断分支。
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

	private const string StorageKey = "_knowledge_rules_v1_json";

	private const string StorageChunkCountKey = "_knowledge_rules_v1_json_chunk_count";

	private const string StorageChunkKeyPrefix = "_knowledge_rules_v1_json_chunk_";

	private const int StorageChunkMaxBytes = 12000;

	private const int LegacyInlineStorageMaxBytes = 240;

	private const int MaxStorageChunkCount = 262144;

	private const string PlayerPersonaRuleId = "rule_animus_player_persona";

	internal const string PlayerPersonaRawNameBeginMarker = "\uE100AF_PLAYER_PERSONA_NAME_BEGIN\uE101";

	internal const string PlayerPersonaRawNameEndMarker = "\uE100AF_PLAYER_PERSONA_NAME_END\uE101";

	// 下面这组常量定义了词汇映射支持的全部取值类型。
	// 编辑器里看到的“你想取谁的什么”最终都会落到这些 kind 上。
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

	private static readonly object _loreContextCacheLock = new object();

	private static Dictionary<string, LoreContextCacheItem> _loreContextCache = new Dictionary<string, LoreContextCacheItem>();

	private const int LoreContextCacheMax = 256;

		private const int RuleListPageSize = 60;

	private long _ruleIndexCacheVersion = -1L;

	private List<RuleIndexItem> _ruleIndexCache;

	private bool _loadedSaveIndexBuildAttempted;

	private sealed class IndexPorts : IKnowledgeIndexPorts
	{
		public bool EmbeddingAvailable => OnnxEmbeddingEngine.Instance?.IsAvailable == true;
		public bool TryGetEmbedding(string text, out float[] vector)
		{
			OnnxEmbeddingEngine engine = OnnxEmbeddingEngine.Instance;
			vector = null;
			return engine != null && engine.TryGetEmbedding(text, out vector);
		}
		public bool RerankerAvailable => OnnxCrossEncoderReranker.Instance?.IsAvailable == true;
		public bool TryScoreBatch(string query, IReadOnlyList<string> documents, out List<float> scores)
		{
			OnnxCrossEncoderReranker reranker = OnnxCrossEncoderReranker.Instance;
			scores = null;
			return reranker != null && reranker.TryScoreBatch(query, documents, out scores);
		}
		public int SemanticTopK => PromptLoreSettingsScope.Value?.SemanticTopK ?? AIConfigHandler.KnowledgeSemanticTopK;
		public float SemanticMinScore => PromptLoreSettingsScope.Value?.SemanticMinScore ?? AIConfigHandler.KnowledgeSemanticMinScore;
		public void Log(string channel, string message) => Logger.Log(channel, message);
	}

	// Single retrieval index (sparse + ONNX + ranked cache). Static like the legacy caches: one rule library per process.
	private static readonly KnowledgeRuleIndex Index = new KnowledgeRuleIndex(new IndexPorts(), GetPublishedRulesForIndex);
	private static readonly AsyncLocal<PromptLoreSettings> PromptLoreSettingsScope = new AsyncLocal<PromptLoreSettings>();
	private static IReadOnlyList<LoreRule> _publishedRules;
	private static long _publishedRulesVersion = -1L;

	private static IReadOnlyList<LoreRule> GetPublishedRulesForIndex()
	{
		return _publishedRulesVersion == Index.Version ? _publishedRules : Instance?._file?.Rules;
	}

	// Called on the game thread at most once per data version, never once per prompt.
	private static void PublishPromptRules()
	{
		long version = Index.Version;
		if (_publishedRulesVersion == version)
		{
			return;
		}
		List<LoreRule> source = Instance?._file?.Rules;
		List<LoreRule> snapshot = new List<LoreRule>(source?.Count ?? 0);
		if (source != null)
		{
			foreach (LoreRule rule in source)
			{
				if (rule == null) { snapshot.Add(null); continue; }
				LoreRule copy = new LoreRule
				{
					Id = rule.Id,
					Keywords = rule.Keywords == null ? null : new List<string>(rule.Keywords),
					RagShortTexts = rule.RagShortTexts == null ? null : new List<string>(rule.RagShortTexts),
					SemanticPrototypes = rule.SemanticPrototypes == null ? null : new List<string>(rule.SemanticPrototypes),
					Variants = rule.Variants == null ? null : rule.Variants.Select(variant => variant == null ? null : new LoreVariant
					{
						Priority = variant.Priority,
						Content = variant.Content,
						When = variant.When == null ? null : new LoreWhen
						{
							HeroIds = variant.When.HeroIds == null ? null : new List<string>(variant.When.HeroIds),
							Cultures = variant.When.Cultures == null ? null : new List<string>(variant.When.Cultures),
							KingdomIds = variant.When.KingdomIds == null ? null : new List<string>(variant.When.KingdomIds),
							SettlementIds = variant.When.SettlementIds == null ? null : new List<string>(variant.When.SettlementIds),
							Roles = variant.When.Roles == null ? null : new List<string>(variant.When.Roles),
							IdentityIds = variant.When.IdentityIds == null ? null : new List<string>(variant.When.IdentityIds),
							IsFemale = variant.When.IsFemale,
							IsClanLeader = variant.When.IsClanLeader,
							SkillMin = variant.When.SkillMin == null ? null : new Dictionary<string, int>(variant.When.SkillMin, variant.When.SkillMin.Comparer)
						}
					}).ToList(),
					TextMappings = rule.TextMappings == null ? null : rule.TextMappings.Select(mapping => mapping == null ? null : new LoreTextMapping
					{
						SourceText = mapping.SourceText, Kind = mapping.Kind, TargetId = mapping.TargetId,
						AgeMin = mapping.AgeMin, AgeMax = mapping.AgeMax, EmptyValueText = mapping.EmptyValueText,
						TrueText = mapping.TrueText, FalseText = mapping.FalseText
					}).ToList()
				};
				snapshot.Add(copy);
			}
		}
		_publishedRules = snapshot;
		_publishedRulesVersion = version;
	}

	internal static PromptLoreSettings CapturePromptLoreSettings()
	{
		try
		{
			return new PromptLoreSettings
			{
				Enabled = KnowledgeRetrievalEnabledSafe(),
				SemanticTopK = AIConfigHandler.KnowledgeSemanticTopK,
				SemanticMinScore = AIConfigHandler.KnowledgeSemanticMinScore
			};
		}
		catch
		{
			return null;
		}
	}
	private static bool KnowledgeRetrievalEnabledSafe()
	{
		try
		{
			return AIConfigHandler.KnowledgeRetrievalEnabled;
		}
		catch
		{
			return true;
		}
	}

	private static readonly LoreCandidateRetriever Retriever = new LoreCandidateRetriever(Index, Logger.Log);

	/// <summary>Game thread: finish a cold index build before a prompt worker can query it.</summary>
	internal static long PreparePromptLoreRetrieval(MentionedWorldEntities mentions)
	{
		if (Instance == null || mentions == null || mentions.IsEmpty)
		{
			return Index.Version;
		}
		try
		{
			PublishPromptRules();
			if (!Index.SparseReady) Index.EnsureVectorIndex();
			if (!Index.OnnxReady && OnnxEmbeddingEngine.Instance?.IsAvailable == true) Index.EnsureOnnxIndex();
			return Index.SparseReady ? Index.Version : 0L;
		}
		catch
		{
			return 0L;
		}
	}

	/// <summary>Worker: select candidates from the prepared index without reading a Hero or Campaign.</summary>
	internal static LoreCandidateRules CollectPromptLoreCandidates(MentionedWorldEntities mentions, long preparedVersion, PromptLoreSettings settings)
	{
		if (Instance == null || settings == null || preparedVersion <= 0L || Index.Version != preparedVersion)
		{
			return null;
		}
		PromptLoreSettings previous = PromptLoreSettingsScope.Value;
		try
		{
			PromptLoreSettingsScope.Value = settings;
			LoreCandidateRules candidates = Retriever.CollectCandidateRules(mentions?.Entities, settings.Enabled);
			return Index.Version == preparedVersion ? candidates : null;
		}
		finally
		{
			PromptLoreSettingsScope.Value = previous;
		}
	}

	public static KnowledgeLibraryBehavior Instance { get; private set; }

	/// <summary>Worker: retain the normal recall/rerank pool, but consume only its first rule.</summary>
	internal static LoreRule CollectPersonaLoreRule(MentionedWorldEntities mentions, long preparedVersion, PromptLoreSettings settings)
	{
		return CollectPromptLoreCandidates(mentions, preparedVersion, settings)?.OrderedRules?.FirstOrDefault();
	}

	/// <summary>Game thread: render exactly one rule for the target Hero, with no dialogue keyword slots.</summary>
	internal string BuildPersonaLoreSource(Hero hero, LoreRule rule, long expectedVersion)
	{
		if (!ReferenceEquals(Instance, this) || hero == null || rule == null
			|| expectedVersion <= 0L || Index.Version != expectedVersion || !CanInjectKnowledgeRule(rule, includePlayerPersona: false))
		{
			return "";
		}
		string heroId = (hero.StringId ?? "").Trim();
		string cultureId = (hero.Culture?.StringId ?? "neutral").Trim().ToLowerInvariant();
		string kingdomId = (hero.Clan?.Kingdom?.StringId ?? hero.MapFaction?.StringId ?? "").Trim().ToLowerInvariant();
		string settlementId = (hero.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
		string role = hero.IsLord ? "lord" : hero.IsNotable ? "notable" : RoleFromOccupation(hero.Occupation);
		LoreVariant variant = PickBestVariant(rule, hero, null, heroId, cultureId, kingdomId, settlementId, role,
			hero.IsFemale, hero.Clan != null && hero.Clan.Leader == hero);
		string content = variant == null ? "" : ApplyRuleTextMappings(rule, variant.Content ?? "", hero, null).Trim();
		Logger.Log("NpcPersona", "lore_source hero=" + heroId + " rule=" + (rule.Id ?? "")
			+ " version=" + expectedVersion + " result=" + (variant == null ? "variant_miss" : content.Length == 0 ? "content_empty" : "hit"));
		if (content.Length == 0) return "";
		string title = rule.Keywords?.FirstOrDefault(keyword => !string.IsNullOrWhiteSpace(keyword))?.Trim() ?? rule.Id ?? "";
		return "【世界书资料：" + title + "】\n" + content;
	}

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

	private static string NormalizePlayerAppearanceForStorage(string input)
	{
		return (input ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
	}

	private string GetPlayerAppearanceForPrompt()
	{
		try
		{
			return NormalizePlayerAppearanceForStorage(_file?.PlayerAppearance);
		}
		catch
		{
			return "";
		}
	}

	private static string BuildPermanentPlayerAppearanceContext(string npcDisplayName, string appearance)
	{
		string text = NormalizePlayerAppearanceForStorage(appearance);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = (npcDisplayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "该NPC";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("【玩家外貌信息（常驻）】");
		stringBuilder.AppendLine(text2 + "与玩家面对面互动时，都可以直接观察到这些外貌特征；");
		stringBuilder.AppendLine(text);
		return stringBuilder.ToString().TrimEnd();
	}

	private static void AppendPermanentPlayerAppearanceContext(StringBuilder sb, string npcDisplayName, string appearance)
	{
		if (sb == null)
		{
			return;
		}
		string text = BuildPermanentPlayerAppearanceContext(npcDisplayName, appearance);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (sb.Length > 0)
		{
			sb.AppendLine();
		}
		sb.AppendLine(text);
	}

	private static void TouchRuleData()
	{
		Index.Touch();
		_publishedRules = null;
		_publishedRulesVersion = -1L;
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
	}

	private static bool SafeSyncData<T>(IDataStore dataStore, string key, ref T data)
	{
		try
		{
			return dataStore != null && dataStore.SyncData(key, ref data);
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
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		while (num4 < text.Length)
		{
			int utf8ScalarByteCount = GetUtf8ScalarByteCount(text, num4, out int charCount);
			if (num3 > 0 && num3 + utf8ScalarByteCount > num)
			{
				list.Add(text.Substring(num2, num4 - num2));
				num2 = num4;
				num3 = 0;
			}
			num3 += utf8ScalarByteCount;
			num4 += charCount;
		}
		if (num4 > num2)
		{
			list.Add(text.Substring(num2, num4 - num2));
		}
		return list;
	}

	private static int GetUtf8ScalarByteCount(string value, int index, out int charCount)
	{
		char c = value[index];
		if (char.IsHighSurrogate(c) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
		{
			charCount = 2;
			return 4;
		}
		charCount = 1;
		if (c <= '\u007f')
		{
			return 1;
		}
		return (c <= '\u07ff') ? 2 : 3;
	}

	private static void SaveStorageJson(IDataStore dataStore, string json)
	{
		string text = json ?? "";
		List<string> list = SplitUtf8Chunks(text, StorageChunkMaxBytes);
		int count = list.Count;
		SafeSyncData(dataStore, StorageChunkCountKey, ref count);
		for (int i = 0; i < list.Count; i++)
		{
			string text2 = list[i] ?? "";
			SafeSyncData(dataStore, StorageChunkKeyPrefix + i, ref text2);
		}
		string text3 = (GetUtf8ByteCount(text) <= LegacyInlineStorageMaxBytes) ? text : "";
		SafeSyncData(dataStore, StorageKey, ref text3);
	}

	private static string LoadStorageJson(IDataStore dataStore)
	{
		string text = TryLoadChunkedStorageJson(dataStore);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = "";
		return SafeSyncData(dataStore, StorageKey, ref text2) ? (text2 ?? "") : "";
	}

	private static string TryLoadChunkedStorageJson(IDataStore dataStore)
	{
		int data = 0;
		if (!SafeSyncData(dataStore, StorageChunkCountKey, ref data) || data <= 0 || data > MaxStorageChunkCount)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < data; i++)
		{
			string text = "";
			if (!SafeSyncData(dataStore, StorageChunkKeyPrefix + i, ref text))
			{
				return "";
			}
			stringBuilder.Append(text ?? "");
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

	private static string BuildKnowledgeHitRateDetail(string detail, string secondaryInput)
	{
		string text = (detail ?? "").Trim();
		string text2 = (secondaryInput ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string text3 = string.IsNullOrWhiteSpace(text2) ? "off" : "on";
		string value = $"npcRecall={text3} secondaryLen={(string.IsNullOrWhiteSpace(text2) ? 0 : text2.Length)}";
		return string.IsNullOrWhiteSpace(text) ? value : (text + " " + value);
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
			foreach (char c in text)
			{
				stringBuilder.Append(char.IsWhiteSpace(c) ? '_' : c);
			}
			text = stringBuilder.ToString();
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
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

	private bool TryFindRuleIdByKeyword(string keyword, string excludeRuleId, out string foundRuleId)
	{
		foundRuleId = null;
		try
		{
			string text = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword);
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
					string text4 = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword2);
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

	private bool TryFindRuleByExactKeyword(string keyword, HashSet<string> excludeRuleIds, out LoreRule matchedRule)
	{
		matchedRule = null;
		try
		{
			string text = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword);
			if (string.IsNullOrEmpty(text) || _file == null || _file.Rules == null)
			{
				return false;
			}
			foreach (LoreRule rule in _file.Rules)
			{
				if (rule == null)
				{
					continue;
				}
				string text2 = (rule.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text2) || (excludeRuleIds != null && excludeRuleIds.Contains(text2)) || rule.Keywords == null || rule.Keywords.Count <= 0)
				{
					continue;
				}
				foreach (string keyword2 in rule.Keywords)
				{
					string text3 = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword2);
					if (!string.IsNullOrEmpty(text3) && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase))
					{
						matchedRule = rule;
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static string GetPlayerKeywordSlotKeyword()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null || (mainHero.Clan?.Tier ?? 0) < 2)
			{
				return "";
			}
			return (mainHero.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static bool IsPlayerPersonaRule(LoreRule rule)
	{
		return rule != null && string.Equals((rule.Id ?? "").Trim(), PlayerPersonaRuleId, StringComparison.OrdinalIgnoreCase);
	}

	// Keeps external database importers from guessing which lore rule is the player's protected profile.
	public static bool IsPlayerPersonaRuleId(string ruleId)
	{
		return string.Equals((ruleId ?? "").Trim(), PlayerPersonaRuleId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool CanObserverReceivePlayerPersona(Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			Hero observerHero = npcHero ?? npcCharacter?.HeroObject;
			if (observerHero != null)
			{
				if (observerHero == Hero.MainHero)
				{
					return true;
				}
				return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerHero);
			}
			if (npcCharacter == null)
			{
				return false;
			}
			string observerKey = MyBehavior.BuildRuleTargetKeyForExternal(null, npcCharacter);
			string cultureId = (npcCharacter.Culture?.StringId ?? "").Trim();
			return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerKey, cultureId);
		}
		catch
		{
			return false;
		}
	}

	private static bool CanInjectKnowledgeRule(LoreRule rule, bool includePlayerPersona)
	{
		return includePlayerPersona || !IsPlayerPersonaRule(rule);
	}

	private static string ProtectPlayerPersonaRawNameReferences(string text)
	{
		string result = text ?? "";
		if (string.IsNullOrEmpty(result))
		{
			return result;
		}
		string actualPlayerName = "";
		try
		{
			actualPlayerName = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			actualPlayerName = "";
		}
		const string actualNamePlaceholder = "\uE102AF_PLAYER_PERSONA_ACTUAL_NAME\uE103";
		if (!string.IsNullOrWhiteSpace(actualPlayerName) && !string.Equals(actualPlayerName, "玩家", StringComparison.Ordinal))
		{
			result = result.Replace(actualPlayerName, actualNamePlaceholder);
		}
		result = result.Replace("玩家", PlayerPersonaRawNameBeginMarker + "玩家" + PlayerPersonaRawNameEndMarker);
		if (!string.IsNullOrWhiteSpace(actualPlayerName) && !string.Equals(actualPlayerName, "玩家", StringComparison.Ordinal))
		{
			result = result.Replace(actualNamePlaceholder, PlayerPersonaRawNameBeginMarker + actualPlayerName + PlayerPersonaRawNameEndMarker);
		}
		return result;
	}

	internal static string StripPlayerPersonaRawNameMarkersForExternal(string text)
	{
		return (text ?? "").Replace(PlayerPersonaRawNameBeginMarker, "").Replace(PlayerPersonaRawNameEndMarker, "");
	}

	private static string BuildExactKeywordSlotCacheSignature()
	{
		string text = LoreCandidateRetriever.NormalizeKeywordForCompare(GetPlayerKeywordSlotKeyword());
		return string.IsNullOrEmpty(text) ? "player_slot=off" : ("player_slot=" + text);
	}

	private bool TryAppendExactKeywordSlotLore(StringBuilder stringBuilder, ref bool wroteHeader, LoreRule rule, bool includePlayerPersona, string matchMode, Hero npcHero, CharacterObject npcCharacter, string heroId, string cultureId, string kingdomId, string settlementId, string role, bool isFemale, bool isClanLeader, string npcDisplayName, string inputText, string secondaryInput, HashSet<string> injectedRuleIds)
	{
		if (rule == null || !CanInjectKnowledgeRule(rule, includePlayerPersona))
		{
			return false;
		}
		string text = (rule.Id ?? "").Trim();
		if (!string.IsNullOrEmpty(text) && injectedRuleIds != null && injectedRuleIds.Contains(text))
		{
			return false;
		}
		string text2 = "";
		try
		{
			text2 = rule.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
		}
		catch
		{
			text2 = "";
		}
		try
		{
			Logger.Log("LoreMatch", "knowledge_hit rule=" + text + " mode=" + matchMode + " hero=" + heroId + " culture=" + cultureId + " kingdom=" + kingdomId + " settlement=" + settlementId + " role=" + role);
		}
		catch
		{
		}
		LoreVariant loreVariant = PickBestVariant(rule, npcHero, npcCharacter, heroId, cultureId, kingdomId, settlementId, role, isFemale, isClanLeader);
		if (loreVariant == null)
		{
			try
			{
				Logger.Log("LoreMatch", "variant_miss rule=" + text + " hero=" + heroId + " culture=" + cultureId + " kingdom=" + kingdomId + " role=" + role);
			}
			catch
			{
			}
			try
			{
				Logger.RecordHitRate("knowledge", text ?? "__unknown__", hit: false, BuildKnowledgeHitRateDetail("reason=variant_miss mode=" + matchMode, secondaryInput), inputText);
			}
			catch
			{
			}
			return false;
		}
		string text3 = ApplyRuleTextMappings(rule, loreVariant.Content ?? "", npcHero, npcCharacter).Trim();
		if (string.IsNullOrEmpty(text3))
		{
			try
			{
				Logger.Log("LoreMatch", "content_empty rule=" + text + " hero=" + heroId + " culture=" + cultureId + " kingdom=" + kingdomId + " role=" + role);
			}
			catch
			{
			}
			try
			{
				Logger.RecordHitRate("knowledge", text ?? "__unknown__", hit: false, BuildKnowledgeHitRateDetail("reason=content_empty mode=" + matchMode, secondaryInput), inputText);
			}
			catch
			{
			}
			return false;
		}
		try
		{
			Logger.Log("LoreMatch", "variant_hit rule=" + text + " hero=" + heroId + " culture=" + cultureId + " kingdom=" + kingdomId + " role=" + role);
		}
		catch
		{
		}
		try
		{
			Logger.RecordHitRate("knowledge", text ?? "__unknown__", hit: true, BuildKnowledgeHitRateDetail("reason=variant_hit mode=" + matchMode, secondaryInput), inputText);
		}
		catch
		{
		}
		if (!wroteHeader)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("参与互动让你的脑海里浮现了这些知识");
			wroteHeader = true;
		}
		string text4 = string.IsNullOrWhiteSpace(text2) ? (text ?? "相关语义") : text2;
		string knowledgeHeader = "【以下是关于（" + text4 + "）的背景知识，" + npcDisplayName + "可酌情参考作为聊天素材】";
		if (IsPlayerPersonaRule(rule))
		{
			knowledgeHeader = ProtectPlayerPersonaRawNameReferences(knowledgeHeader);
			text3 = ProtectPlayerPersonaRawNameReferences(text3);
		}
		stringBuilder.AppendLine(knowledgeHeader);
		stringBuilder.AppendLine(text3);
		if (!string.IsNullOrEmpty(text) && injectedRuleIds != null)
		{
			injectedRuleIds.Add(text);
		}
		return true;
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
			if (_ruleIndexCache != null && _ruleIndexCacheVersion == Index.Version)
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
			_ruleIndexCacheVersion = Index.Version;
		}
		catch
		{
			try
			{
				_ruleIndexCache = _ruleIndexCache ?? new List<RuleIndexItem>();
				_ruleIndexCacheVersion = Index.Version;
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
			string key = tag + ":" + KnowledgeRuleIndex.Hash8(text) + ":" + heroId + ":" + cultureId + ":" + kingdomId + ":" + role;
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
			string text9 = KnowledgeRuleIndex.Hash8(inputText ?? "");
			Logger.Log("LoreMatch", $"lore_ctx source={text} invalid={invalidContext} hero={text2} char={text3} culture={text4} kingdom={text5} settlement={text6} role={text7} female={isFemale} clanLeader={isClanLeader} kingdomOverride={text8} inputHash={text9}");
		}
		catch
		{
		}
	}

	public KnowledgeLibraryBehavior()
	{
		Instance = this;
		TouchRuleData();
	}

	public override void RegisterEvents()
	{
	}

	private void BuildIndexesForLoadedSave()
	{
		if (_loadedSaveIndexBuildAttempted)
		{
			return;
		}
		_loadedSaveIndexBuildAttempted = true;
		try
		{
			long version = Index.Version;
			if (version <= 0)
			{
				version = 1L;
			}
			Logger.Log("KnowledgeIndexWarmup", $"start source=save_load version={version}");
			RunIndexBuildForLoadedSave(version);
		}
		catch (Exception ex)
		{
			Logger.Log("KnowledgeIndexWarmup", "complete source=save_load failed=" + (ex.Message ?? "index build exception"));
		}
	}

	private void RunIndexBuildForLoadedSave(long version)
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
			PublishPromptRules();
			Index.EnsureVectorIndex();
			flag = Index.SparseReady && Index.Version == version;
			num = Index.SparseEntryCount;
		}
		catch (Exception ex)
		{
			text = ex.Message ?? "vector index warmup exception";
		}
		try
		{
			Index.EnsureOnnxIndex();
			bool flag5 = _file == null || _file.Rules == null || _file.Rules.Count == 0;
			num2 = Index.OnnxEntryCount;
			flag2 = Index.OnnxReady && Index.Version == version && (num2 > 0 || flag5);
		}
		catch (Exception ex2)
		{
			text2 = ex2.Message ?? "onnx index warmup exception";
		}
		stopwatch.Stop();
		bool flag3 = Index.Version != version;
		Logger.Log("KnowledgeIndexWarmup", $"complete source=save_load version={version} stale={flag3} retryPending=False ms={Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)} sparseOk={flag} sparseEntries={num} onnxOk={flag2} onnxEntries={num2} sparseError={text} onnxError={text2}");
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_file == null)
		{
			_file = new KnowledgeFile();
		}
		_file.PlayerAppearance = NormalizePlayerAppearanceForStorage(_file.PlayerAppearance);
		StripSemanticPrototypes();
		if (dataStore != null && dataStore.IsSaving)
		{
			try
			{
				_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
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
		if (dataStore != null && dataStore.IsLoading)
		{
			TouchRuleData();
			BuildIndexesForLoadedSave();
		}
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

	// 确保规则上始终有 TextMappings 列表，避免编辑器和运行时到处判空。
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

	// 只要知识库里存在有效的词汇映射，就不能安全复用通用 lore context 缓存，
	// 因为最终提示词会依赖当前 NPC / 玩家 / 绑定对象的即时状态。
	private bool HasAnyTextMappings()
	{
		try
		{
			return _file?.Rules?.Any((LoreRule r) => r?.TextMappings != null && r.TextMappings.Any((LoreTextMapping m) => m != null && !string.IsNullOrWhiteSpace(m.SourceText) && !string.IsNullOrWhiteSpace(m.Kind) && !string.IsNullOrWhiteSpace(m.TargetId))) == true;
		}
		catch
		{
			return false;
		}
	}

	// status|source|condition 是状态型映射的内部协议格式，
	// 例如“当前NPC是否已婚”会被编码成一条 kind，而不是普通字段取值。
	private static bool IsStatusMappingKind(string kind)
	{
		return ((kind ?? "").Trim()).StartsWith("status|", StringComparison.OrdinalIgnoreCase);
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
		string[] array = ((kind ?? "").Trim()).Split(new char[1] { '|' }, StringSplitOptions.None);
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

	private static string GetStatusSourceLabel(string sourceKey) => KnowledgeMappingUiTexts.StatusSource(sourceKey);
	private static string GetStatusSourceTargetTypeLabel(string sourceKey) => KnowledgeMappingUiTexts.TargetTypeForSource(sourceKey);
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
		return (sourceKey ?? "").Trim().ToLowerInvariant() switch
		{
			"current_npc_hero" => TextMappingTargetCurrentNpc,
			"current_npc_clan" => TextMappingTargetCurrentNpc,
			"current_npc_kingdom" => TextMappingTargetCurrentNpc,
			"current_npc_settlement" => TextMappingTargetCurrentNpc,
			"player_hero" => TextMappingTargetPlayer,
			"player_clan" => TextMappingTargetPlayer,
			"player_kingdom" => TextMappingTargetPlayer,
			"player_settlement" => TextMappingTargetPlayer,
			"bound_hero" => TextMappingTargetBoundHero,
			"bound_hero_clan" => TextMappingTargetBoundHero,
			"bound_kingdom" => TextMappingTargetBoundKingdom,
			"bound_settlement" => TextMappingTargetBoundSettlement,
			"bound_settlement_owner_clan" => TextMappingTargetBoundSettlement,
			_ => ""
		};
	}

	private static string GetStatusConditionLabel(string statusKey) => KnowledgeMappingUiTexts.StatusCondition(statusKey);
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

	// 把内部 kind 转成编辑器里可读的中文标签，方便列表展示和搜索。
	private static string GetTextMappingKindLabel(string kind)
	{
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var statusKey)) return KnowledgeMappingUiTexts.StatusLabel(sourceKey, statusKey);
		return KnowledgeMappingUiTexts.Kind(kind);
	}
	private static string GetTextMappingTargetTypeLabel(string kind)
	{
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var _)) return KnowledgeMappingUiTexts.TargetTypeForSource(sourceKey);
		return KnowledgeMappingUiTexts.TargetTypeForKind(kind);
	}
	private static string GetAutomaticTargetIdForKind(string kind)
	{
		if (TryParseStatusMappingKind(kind, out var sourceKey, out var _))
		{
			return GetAutomaticTargetIdForStatusSource(sourceKey);
		}
		return (kind ?? "").Trim() switch
		{
			TextMappingKindCurrentNpcName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanKingdomName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanKingdomLeaderName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanAllTowns => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanAllVillages => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanAllSettlements => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanMembers => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanMaleMembers => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanFemaleMembers => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcClanAgeRangeMembers => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomLeaderName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomRulingClanName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomCultureName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomInitialHomeSettlementName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAllClans => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAllLords => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAllTowns => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAllCastles => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAllVillages => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAllSettlements => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomActivePolicies => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomAlliedKingdoms => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcKingdomWarFactions => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcSpouseName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcFatherName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcMotherName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementOwnerClanName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementOwnerLeaderName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomLeaderName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementCultureName => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementNotables => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementParties => TextMappingTargetCurrentNpc,
			TextMappingKindCurrentNpcCurrentSettlementBoundVillages => TextMappingTargetCurrentNpc,
			TextMappingKindPlayerName => TextMappingTargetPlayer,
			TextMappingKindPlayerClanName => TextMappingTargetPlayer,
			TextMappingKindPlayerClanKingdomName => TextMappingTargetPlayer,
			TextMappingKindPlayerClanKingdomLeaderName => TextMappingTargetPlayer,
			TextMappingKindPlayerClanAllTowns => TextMappingTargetPlayer,
			TextMappingKindPlayerClanAllVillages => TextMappingTargetPlayer,
			TextMappingKindPlayerClanAllSettlements => TextMappingTargetPlayer,
			TextMappingKindPlayerClanMembers => TextMappingTargetPlayer,
			TextMappingKindPlayerClanMaleMembers => TextMappingTargetPlayer,
			TextMappingKindPlayerClanFemaleMembers => TextMappingTargetPlayer,
			TextMappingKindPlayerClanAgeRangeMembers => TextMappingTargetPlayer,
			TextMappingKindPlayerKingdomName => TextMappingTargetPlayer,
			TextMappingKindPlayerKingdomLeaderName => TextMappingTargetPlayer,
			TextMappingKindPlayerSpouseName => TextMappingTargetPlayer,
			TextMappingKindPlayerCurrentSettlementName => TextMappingTargetPlayer,
			TextMappingKindBoundKingdomName => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomLeaderName => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomRulingClanName => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomCultureName => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomInitialHomeSettlementName => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAllClans => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAllLords => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAllTowns => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAllCastles => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAllVillages => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAllSettlements => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomActivePolicies => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomAlliedKingdoms => TextMappingTargetBoundKingdom,
			TextMappingKindBoundKingdomWarFactions => TextMappingTargetBoundKingdom,
			TextMappingKindBoundSettlementName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanKingdomName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanKingdomLeaderName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanAllTowns => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanAllVillages => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanAllSettlements => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanMembers => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanMaleMembers => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanFemaleMembers => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerClanAgeRangeMembers => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerLeaderName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerKingdomName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementOwnerKingdomLeaderName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementCultureName => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementNotables => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementParties => TextMappingTargetBoundSettlement,
			TextMappingKindBoundSettlementBoundVillages => TextMappingTargetBoundSettlement,
			TextMappingKindBoundHeroName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanKingdomName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanKingdomLeaderName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanAllTowns => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanAllVillages => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanAllSettlements => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanMembers => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanMaleMembers => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanFemaleMembers => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroClanAgeRangeMembers => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroKingdomName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroKingdomLeaderName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroSpouseName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroFatherName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroMotherName => TextMappingTargetBoundHero,
			TextMappingKindBoundHeroCurrentSettlementName => TextMappingTargetBoundHero,
			_ => ""
		};
	}

	private static string GetHeroDisplayName(Hero hero)
	{
		try
		{
			return (hero?.Name?.ToString() ?? "").Trim();
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
			return (clan?.Name?.ToString() ?? "").Trim();
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
			return (kingdom?.Name?.ToString() ?? "").Trim();
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
			return (culture?.Name?.ToString() ?? "").Trim();
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
			return (faction?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	// 下面这些 Build*Text 方法专门把“列表型信息”整理成适合注入提示词的短文本，
	// 比如王国全部家族、定居点驻留队伍、绑定村庄等。
	private static string GetSettlementDisplayName(Settlement settlement)
	{
		try
		{
			return (settlement?.Name?.ToString() ?? "").Trim();
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
		string text = GetSettlementDisplayName(settlement);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = GetSettlementTypeLabel(settlement);
		return string.IsNullOrWhiteSpace(text2) ? text : (text + "（" + text2 + "）");
	}

	private static string BuildKingdomSettlementListText(Kingdom kingdom, bool includeTowns, bool includeCastles, bool includeVillages)
	{
		try
		{
			if (kingdom?.Settlements == null)
			{
				return "";
			}
			List<Settlement> list = new List<Settlement>();
			foreach (Settlement settlement in kingdom.Settlements)
			{
				if (settlement == null)
				{
					continue;
				}
				if (settlement.IsTown && includeTowns)
				{
					list.Add(settlement);
				}
				else if (settlement.IsCastle && includeCastles)
				{
					list.Add(settlement);
				}
				else if (settlement.IsVillage && includeVillages)
				{
					list.Add(settlement);
				}
			}
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list.OrderBy((Settlement s) => GetSettlementTypeOrder(s)).ThenBy((Settlement s) => GetSettlementDisplayName(s), StringComparer.OrdinalIgnoreCase).Select(FormatSettlementWithType).Where((string x) => !string.IsNullOrWhiteSpace(x)));
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
			foreach (Settlement item in Settlement.All)
			{
				if (item == null || item.OwnerClan != clan)
				{
					continue;
				}
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
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list.OrderBy((Settlement s) => GetSettlementTypeOrder(s)).ThenBy((Settlement s) => GetSettlementDisplayName(s), StringComparer.OrdinalIgnoreCase).Select(FormatSettlementWithType).Where((string x) => !string.IsNullOrWhiteSpace(x)));
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
			foreach (Hero allAliveHero in Hero.AllAliveHeroes)
			{
				if (allAliveHero != null && allAliveHero.Clan == clan)
				{
					list.Add(allAliveHero);
				}
			}
			return list.OrderByDescending((Hero h) => (clan.Leader == h) ? 1 : 0).ThenBy((Hero h) => GetHeroDisplayName(h), StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => h.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
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
			List<Hero> clanLivingMembers = GetClanLivingMembers(clan);
			if (predicate != null)
			{
				clanLivingMembers = clanLivingMembers.Where((Hero h) => h != null && predicate(h)).ToList();
			}
			if (clanLivingMembers.Count == 0)
			{
				return "";
			}
			return string.Join("，", clanLivingMembers.Select(GetHeroDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)));
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
			if (kingdom?.Clans == null)
			{
				return "";
			}
			List<Clan> list = kingdom.Clans.Where((Clan c) => c != null).OrderByDescending((Clan c) => (kingdom.RulingClan == c) ? 1 : 0).ThenBy((Clan c) => GetClanDisplayName(c), StringComparer.OrdinalIgnoreCase).ThenBy((Clan c) => c.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list.Select(GetClanDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)));
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
			if (kingdom?.AliveLords == null)
			{
				return "";
			}
			List<Hero> list = kingdom.AliveLords.Where((Hero h) => h != null).OrderByDescending((Hero h) => (kingdom.Leader == h) ? 1 : 0).ThenBy((Hero h) => GetHeroDisplayName(h), StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => h.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return string.Join("，", list.Select(GetHeroDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)));
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
			List<string> list = policies.Where((PolicyObject p) => p != null).Select((PolicyObject p) => (p.Name?.ToString() ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
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
			List<string> list = kingdoms.Where((Kingdom k) => k != null).Select(GetKingdomDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
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
			List<string> list = factions.Where((IFaction f) => f != null).Select(GetFactionDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
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
		case TextMappingKindClanAgeRangeMembers:
		case TextMappingKindCurrentNpcClanAgeRangeMembers:
		case TextMappingKindPlayerClanAgeRangeMembers:
		case TextMappingKindBoundSettlementOwnerClanAgeRangeMembers:
		case TextMappingKindBoundHeroClanAgeRangeMembers:
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
			return Kingdom.All.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
			return Clan.All.FirstOrDefault((Clan x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
			return Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
			return Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
				LoreWhen when = variant?.When;
				if (when?.HeroIds != null)
				{
					foreach (string heroId in when.HeroIds)
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
				if (when?.IdentityIds == null)
				{
					continue;
				}
				foreach (string identityId in when.IdentityIds)
				{
					if (!TryParseRoleIdentityId(identityId, out var kind, out var targetId) || !string.Equals(kind, "hero", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (!string.IsNullOrWhiteSpace(targetId))
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
			return hero?.Clan?.Kingdom ?? ((hero?.MapFaction != null && hero.MapFaction.IsKingdomFaction) ? hero.MapFaction as Kingdom : null);
		}
		catch
		{
			return null;
		}
	}

	private static string GetHeroClanName(Hero hero)
	{
		return GetClanDisplayName(hero?.Clan);
	}

	private static string GetHeroKingdomName(Hero hero)
	{
		return GetKingdomDisplayName(GetHeroKingdom(hero));
	}

	private static string GetClanKingdomName(Clan clan)
	{
		return GetKingdomDisplayName(clan?.Kingdom);
	}

	private static string GetHeroKingdomLeaderName(Hero hero)
	{
		return GetHeroDisplayName(GetHeroKingdom(hero)?.Leader);
	}

	private static string GetClanKingdomLeaderName(Clan clan)
	{
		return GetHeroDisplayName(clan?.Kingdom?.Leader);
	}

	private static Kingdom GetSettlementKingdom(Settlement settlement)
	{
		try
		{
			return settlement?.OwnerClan?.Kingdom ?? ((settlement?.MapFaction != null && settlement.MapFaction.IsKingdomFaction) ? settlement.MapFaction as Kingdom : null);
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
		return GetHeroDisplayName(GetSettlementKingdom(settlement)?.Leader);
	}

	private static string GetSettlementCultureName(Settlement settlement)
	{
		return GetCultureDisplayName(settlement?.Culture);
	}

	private static string BuildSettlementNotableListText(Settlement settlement)
	{
		try
		{
			if (settlement?.Notables == null)
			{
				return "";
			}
			List<string> list = settlement.Notables.Where((Hero h) => h != null).Select(GetHeroDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
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
			if (settlement?.Parties == null)
			{
				return "";
			}
			List<string> list = settlement.Parties.Where((MobileParty p) => p?.Party != null).Select((MobileParty p) => (p.Party.Name?.ToString() ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
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
			if (settlement?.BoundVillages == null)
			{
				return "";
			}
			List<string> list = settlement.BoundVillages.Where((Village v) => v?.Settlement != null).Select((Village v) => FormatSettlementWithType(v.Settlement)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase).ToList();
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
		return GetHeroDisplayName(hero?.Spouse);
	}

	private static string GetHeroFatherName(Hero hero)
	{
		return GetHeroDisplayName(hero?.Father);
	}

	private static string GetHeroMotherName(Hero hero)
	{
		return GetHeroDisplayName(hero?.Mother);
	}

	private static string GetHeroCurrentSettlementName(Hero hero)
	{
		return GetSettlementDisplayName(hero?.CurrentSettlement ?? hero?.StayingInSettlement);
	}

	// 运行时解析层：
	// 根据 mapping.TargetId 和 kind，把“当前NPC/玩家/绑定对象/手动指定对象”
	// 统一还原成真正的 Hero/Clan/Kingdom/Settlement 实例。
	private static Hero ResolveRuntimeHeroFromMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			if (string.Equals(text, TextMappingTargetCurrentNpc, StringComparison.OrdinalIgnoreCase))
			{
				return npcHero ?? ((npcCharacter != null && npcCharacter.IsHero) ? npcCharacter.HeroObject : null);
			}
			if (string.Equals(text, TextMappingTargetPlayer, StringComparison.OrdinalIgnoreCase))
			{
				return Hero.MainHero;
			}
			if (string.Equals(text, TextMappingTargetBoundHero, StringComparison.OrdinalIgnoreCase))
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
			if (string.Equals(text, TextMappingTargetBoundKingdom, StringComparison.OrdinalIgnoreCase))
			{
				return ResolveBoundKingdomFromRule(rule);
			}
			Hero hero = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
			if (hero != null && (string.Equals(text, TextMappingTargetCurrentNpc, StringComparison.OrdinalIgnoreCase) || string.Equals(text, TextMappingTargetPlayer, StringComparison.OrdinalIgnoreCase) || string.Equals(text, TextMappingTargetBoundHero, StringComparison.OrdinalIgnoreCase)))
			{
				return GetHeroKingdom(hero);
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
			if (string.Equals(text, TextMappingTargetBoundSettlement, StringComparison.OrdinalIgnoreCase))
			{
				return ResolveBoundSettlementFromRule(rule);
			}
			Hero hero = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
			if (hero != null && (string.Equals(text, TextMappingTargetCurrentNpc, StringComparison.OrdinalIgnoreCase) || string.Equals(text, TextMappingTargetPlayer, StringComparison.OrdinalIgnoreCase) || string.Equals(text, TextMappingTargetBoundHero, StringComparison.OrdinalIgnoreCase)))
			{
				return hero.CurrentSettlement ?? hero.StayingInSettlement;
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
			Hero hero = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
			if (hero != null && (string.Equals(text, TextMappingTargetCurrentNpc, StringComparison.OrdinalIgnoreCase) || string.Equals(text, TextMappingTargetPlayer, StringComparison.OrdinalIgnoreCase) || string.Equals(text, TextMappingTargetBoundHero, StringComparison.OrdinalIgnoreCase)))
			{
				return hero.Clan;
			}
			if (string.Equals(text, TextMappingTargetBoundSettlement, StringComparison.OrdinalIgnoreCase))
			{
				return ResolveBoundSettlementFromRule(rule)?.OwnerClan;
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
			string text = (sourceKey ?? "").Trim().ToLowerInvariant();
			switch (GetStatusSourceObjectKind(text))
			{
			case "hero":
			{
				Hero hero = ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter);
				string text5 = GetHeroDisplayName(hero);
				string statusSourceLabel4 = GetStatusSourceLabel(text);
				return string.IsNullOrWhiteSpace(text5) ? KnowledgeUiTexts.MappingTargetNoValue(statusSourceLabel4) : KnowledgeUiTexts.MappingTargetValue(statusSourceLabel4, text5);
			}
			case "clan":
			{
				Clan clan = ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter);
				string text4 = GetClanDisplayName(clan);
				string statusSourceLabel3 = GetStatusSourceLabel(text);
				return string.IsNullOrWhiteSpace(text4) ? KnowledgeUiTexts.MappingTargetNoValue(statusSourceLabel3) : KnowledgeUiTexts.MappingTargetValue(statusSourceLabel3, text4);
			}
			case "kingdom":
			{
				Kingdom kingdom = ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter);
				string text3 = GetKingdomDisplayName(kingdom);
				string statusSourceLabel2 = GetStatusSourceLabel(text);
				return string.IsNullOrWhiteSpace(text3) ? KnowledgeUiTexts.MappingTargetNoValue(statusSourceLabel2) : KnowledgeUiTexts.MappingTargetValue(statusSourceLabel2, text3);
			}
			case "settlement":
			{
				Settlement settlement = ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter);
				string text2 = GetSettlementDisplayName(settlement);
				string statusSourceLabel = GetStatusSourceLabel(text);
				return string.IsNullOrWhiteSpace(text2) ? KnowledgeUiTexts.MappingTargetNoValue(statusSourceLabel) : KnowledgeUiTexts.MappingTargetValue(statusSourceLabel, text2);
			}
			default:
				return GetStatusSourceLabel(text);
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
			return hero != null && hero.Children != null && hero.Children.Count > 0;
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
			return hero != null && hero.ExSpouses != null && hero.ExSpouses.Count > 0;
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
			foreach (Settlement item in Settlement.All)
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
			foreach (Settlement settlement in kingdom.Settlements)
			{
				if (settlement != null && (predicate == null || predicate(settlement)))
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

	// 状态型映射不会直接返回对象名字，而是先算布尔结果，再走真/假文案分支。
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
		switch ((statusKey ?? "").Trim().ToLowerInvariant())
		{
		case "is_eliminated":
			return kingdom.IsEliminated;
		case "has_leader":
			return kingdom.Leader != null;
		case "has_ruling_clan":
			return kingdom.RulingClan != null;
		case "has_any_settlement":
			return KingdomHasSettlement(kingdom, null);
		case "has_any_town":
			return KingdomHasSettlement(kingdom, (Settlement s) => s != null && s.IsTown);
		case "has_any_castle":
			return KingdomHasSettlement(kingdom, (Settlement s) => s != null && s.IsCastle);
		case "has_any_village":
			return KingdomHasSettlement(kingdom, (Settlement s) => s != null && s.IsVillage);
		case "has_any_clan":
			return HasAnyItems(kingdom.Clans);
		case "has_any_lord":
			return HasAnyItems(kingdom.AliveLords);
		case "has_active_policies":
			return HasAnyItems(kingdom.ActivePolicies);
		case "has_any_war":
			return HasAnyItems(kingdom.FactionsAtWarWith);
		case "has_any_allies":
			return HasAnyItems(kingdom.AlliedKingdoms);
		default:
			return null;
		}
	}

	private static bool? EvaluateSettlementStatus(Settlement settlement, string statusKey)
	{
		if (settlement == null)
		{
			return null;
		}
		switch ((statusKey ?? "").Trim().ToLowerInvariant())
		{
		case "is_active":
			return settlement.IsActive;
		case "is_town":
			return settlement.IsTown;
		case "is_castle":
			return settlement.IsCastle;
		case "is_village":
			return settlement.IsVillage;
		case "is_fortification":
			return settlement.IsFortification;
		case "is_hideout":
			return settlement.IsHideout;
		case "is_under_siege":
			return settlement.IsUnderSiege;
		case "is_under_raid":
			return settlement.IsUnderRaid;
		case "is_raided":
			return settlement.IsRaided;
		case "is_starving":
			return settlement.IsStarving;
		case "is_rebellious":
			return settlement.InRebelliousState;
		case "has_port":
			return settlement.HasPort;
		case "has_owner":
			return settlement.Owner != null;
		case "has_owner_clan":
			return settlement.OwnerClan != null;
		case "has_notables":
			return HasAnyItems(settlement.Notables);
		case "has_parties":
			return HasAnyItems(settlement.Parties);
		default:
			return null;
		}
	}

	private static bool? EvaluateStatusMapping(LoreTextMapping mapping, LoreRule rule, Hero npcHero, CharacterObject npcCharacter)
	{
		if (!TryParseStatusMappingKind(mapping?.Kind, out var sourceKey, out var statusKey))
		{
			return null;
		}
		switch (GetStatusSourceObjectKind(sourceKey))
		{
		case "hero":
			return EvaluateHeroStatus(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter), statusKey, mapping);
		case "clan":
			return EvaluateClanStatus(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), statusKey, mapping);
		case "kingdom":
			return EvaluateKingdomStatus(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), statusKey);
		case "settlement":
			return EvaluateSettlementStatus(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter), statusKey);
		default:
			return null;
		}
	}

	// 编辑器里展示的“目标说明 / 当前预览”都依赖这里的可读化处理，
	// 它会尽量把自动目标和手动目标都解释成当前玩家能看懂的名字。
	private static string GetTextMappingTargetDisplayName(LoreTextMapping mapping, LoreRule rule = null, Hero npcHero = null, CharacterObject npcCharacter = null)
	{
		try
		{
			string text = (mapping?.TargetId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return KnowledgeUiTexts.MappingTargetNotSelected;
			}
			if (TryParseStatusMappingKind(mapping?.Kind, out var sourceKey, out var _))
			{
				return GetStatusMappingTargetDisplayName(mapping, rule, npcHero, npcCharacter, sourceKey);
			}
			if (string.Equals(text, TextMappingTargetCurrentNpc, StringComparison.OrdinalIgnoreCase))
			{
				string text2 = GetHeroDisplayName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
				return KnowledgeUiTexts.MappingTargetCurrentNpc(string.IsNullOrWhiteSpace(text2) ? "" : KnowledgeUiTexts.MappingTargetNameSuffix(text2));
			}
			if (string.Equals(text, TextMappingTargetPlayer, StringComparison.OrdinalIgnoreCase))
			{
				string text3 = GetHeroDisplayName(Hero.MainHero);
				return KnowledgeUiTexts.MappingTargetPlayer(string.IsNullOrWhiteSpace(text3) ? "" : KnowledgeUiTexts.MappingTargetNameSuffix(text3));
			}
			if (string.Equals(text, TextMappingTargetBoundKingdom, StringComparison.OrdinalIgnoreCase))
			{
				string text4 = GetKingdomDisplayName(ResolveBoundKingdomFromRule(rule));
				return KnowledgeUiTexts.MappingTargetBoundKingdom(string.IsNullOrWhiteSpace(text4) ? KnowledgeUiTexts.MappingTargetUnresolvedSuffix : KnowledgeUiTexts.MappingTargetNameSuffix(text4));
			}
			if (string.Equals(text, TextMappingTargetBoundSettlement, StringComparison.OrdinalIgnoreCase))
			{
				string text5 = GetSettlementDisplayName(ResolveBoundSettlementFromRule(rule));
				return KnowledgeUiTexts.MappingTargetBoundSettlement(string.IsNullOrWhiteSpace(text5) ? KnowledgeUiTexts.MappingTargetUnresolvedSuffix : KnowledgeUiTexts.MappingTargetNameSuffix(text5));
			}
			if (string.Equals(text, TextMappingTargetBoundHero, StringComparison.OrdinalIgnoreCase))
			{
				string text6 = GetHeroDisplayName(ResolveBoundHeroFromRule(rule));
				return KnowledgeUiTexts.MappingTargetBoundHero(string.IsNullOrWhiteSpace(text6) ? KnowledgeUiTexts.MappingTargetUnresolvedSuffix : KnowledgeUiTexts.MappingTargetNameSuffix(text6));
			}
			switch ((mapping?.Kind ?? "").Trim())
			{
			case TextMappingKindKingdomName:
			case TextMappingKindKingdomLeaderName:
			case TextMappingKindKingdomRulingClanName:
			case TextMappingKindKingdomCultureName:
			case TextMappingKindKingdomInitialHomeSettlementName:
			case TextMappingKindKingdomAllClans:
			case TextMappingKindKingdomAllLords:
			case TextMappingKindKingdomAllTowns:
			case TextMappingKindKingdomAllCastles:
			case TextMappingKindKingdomAllVillages:
			case TextMappingKindKingdomAllSettlements:
			case TextMappingKindKingdomActivePolicies:
			case TextMappingKindKingdomAlliedKingdoms:
			case TextMappingKindKingdomWarFactions:
			{
				Kingdom kingdom = FindKingdomById(text);
				string text11 = GetKingdomDisplayName(kingdom);
				return string.IsNullOrWhiteSpace(text11) ? text : text11;
			}
			case TextMappingKindSettlementName:
			case TextMappingKindSettlementOwnerClanName:
			case TextMappingKindSettlementOwnerLeaderName:
			case TextMappingKindSettlementOwnerKingdomName:
			case TextMappingKindSettlementOwnerKingdomLeaderName:
			case TextMappingKindSettlementCultureName:
			case TextMappingKindSettlementNotables:
			case TextMappingKindSettlementParties:
			case TextMappingKindSettlementBoundVillages:
			{
				Settlement settlement = FindSettlementById(text);
				string text10 = GetSettlementDisplayName(settlement);
				return string.IsNullOrWhiteSpace(text10) ? text : text10;
			}
			case TextMappingKindClanName:
			case TextMappingKindClanLeaderName:
			case TextMappingKindClanKingdomName:
			case TextMappingKindClanKingdomLeaderName:
			case TextMappingKindClanAllTowns:
			case TextMappingKindClanAllVillages:
			case TextMappingKindClanAllSettlements:
			case TextMappingKindClanMembers:
			case TextMappingKindClanMaleMembers:
			case TextMappingKindClanFemaleMembers:
			case TextMappingKindClanAgeRangeMembers:
			{
				Clan clan = FindClanById(text);
				string text9 = GetClanDisplayName(clan);
				return string.IsNullOrWhiteSpace(text9) ? text : text9;
			}
			case TextMappingKindHeroName:
			case TextMappingKindHeroClanName:
			case TextMappingKindHeroKingdomName:
			case TextMappingKindHeroKingdomLeaderName:
			case TextMappingKindHeroSpouseName:
			case TextMappingKindHeroFatherName:
			case TextMappingKindHeroMotherName:
			case TextMappingKindHeroCurrentSettlementName:
			{
				Hero hero = FindHeroById(text);
				string text8 = GetHeroDisplayName(hero);
				return string.IsNullOrWhiteSpace(text8) ? text : text8;
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

	// 真正的取值入口：
	// 先看是不是状态映射；如果不是，再按 kind 读取对象字段或拼装列表文本。
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
			case TextMappingKindKingdomName:
			case TextMappingKindBoundKingdomName:
			case TextMappingKindCurrentNpcKingdomName:
				return GetKingdomDisplayName(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindKingdomLeaderName:
			case TextMappingKindBoundKingdomLeaderName:
			case TextMappingKindCurrentNpcKingdomLeaderName:
				return GetHeroDisplayName(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.Leader);
			case TextMappingKindKingdomRulingClanName:
			case TextMappingKindCurrentNpcKingdomRulingClanName:
			case TextMappingKindBoundKingdomRulingClanName:
				return GetClanDisplayName(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.RulingClan);
			case TextMappingKindKingdomCultureName:
			case TextMappingKindCurrentNpcKingdomCultureName:
			case TextMappingKindBoundKingdomCultureName:
				return GetCultureDisplayName(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.Culture);
			case TextMappingKindKingdomInitialHomeSettlementName:
			case TextMappingKindCurrentNpcKingdomInitialHomeSettlementName:
			case TextMappingKindBoundKingdomInitialHomeSettlementName:
				return GetSettlementDisplayName(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.InitialHomeSettlement);
			case TextMappingKindKingdomAllClans:
			case TextMappingKindCurrentNpcKingdomAllClans:
			case TextMappingKindBoundKingdomAllClans:
				return BuildKingdomClanListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindKingdomAllLords:
			case TextMappingKindCurrentNpcKingdomAllLords:
			case TextMappingKindBoundKingdomAllLords:
				return BuildKingdomLordListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindKingdomAllTowns:
			case TextMappingKindCurrentNpcKingdomAllTowns:
			case TextMappingKindBoundKingdomAllTowns:
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: false, includeVillages: false);
			case TextMappingKindKingdomAllCastles:
			case TextMappingKindCurrentNpcKingdomAllCastles:
			case TextMappingKindBoundKingdomAllCastles:
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: false, includeCastles: true, includeVillages: false);
			case TextMappingKindKingdomAllVillages:
			case TextMappingKindCurrentNpcKingdomAllVillages:
			case TextMappingKindBoundKingdomAllVillages:
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: false, includeCastles: false, includeVillages: true);
			case TextMappingKindKingdomAllSettlements:
			case TextMappingKindCurrentNpcKingdomAllSettlements:
			case TextMappingKindBoundKingdomAllSettlements:
				return BuildKingdomSettlementListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: true, includeVillages: true);
			case TextMappingKindKingdomActivePolicies:
			case TextMappingKindCurrentNpcKingdomActivePolicies:
			case TextMappingKindBoundKingdomActivePolicies:
				return BuildPolicyListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.ActivePolicies);
			case TextMappingKindKingdomAlliedKingdoms:
			case TextMappingKindCurrentNpcKingdomAlliedKingdoms:
			case TextMappingKindBoundKingdomAlliedKingdoms:
				return BuildKingdomListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.AlliedKingdoms);
			case TextMappingKindKingdomWarFactions:
			case TextMappingKindCurrentNpcKingdomWarFactions:
			case TextMappingKindBoundKingdomWarFactions:
				return BuildFactionListText(ResolveRuntimeKingdomFromMapping(mapping, rule, npcHero, npcCharacter)?.FactionsAtWarWith);
			case TextMappingKindSettlementName:
			case TextMappingKindBoundSettlementName:
			case TextMappingKindCurrentNpcCurrentSettlementName:
				return GetSettlementDisplayName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindSettlementOwnerClanName:
			case TextMappingKindBoundSettlementOwnerClanName:
			case TextMappingKindCurrentNpcCurrentSettlementOwnerClanName:
				return GetClanDisplayName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter)?.OwnerClan);
			case TextMappingKindSettlementOwnerLeaderName:
			case TextMappingKindBoundSettlementOwnerLeaderName:
			case TextMappingKindCurrentNpcCurrentSettlementOwnerLeaderName:
				return GetHeroDisplayName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter)?.OwnerClan?.Leader);
			case TextMappingKindSettlementOwnerKingdomName:
			case TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomName:
			case TextMappingKindBoundSettlementOwnerKingdomName:
				return GetSettlementKingdomName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindSettlementOwnerKingdomLeaderName:
			case TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomLeaderName:
			case TextMappingKindBoundSettlementOwnerKingdomLeaderName:
				return GetSettlementKingdomLeaderName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindSettlementCultureName:
			case TextMappingKindCurrentNpcCurrentSettlementCultureName:
			case TextMappingKindBoundSettlementCultureName:
				return GetSettlementCultureName(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindSettlementNotables:
			case TextMappingKindCurrentNpcCurrentSettlementNotables:
			case TextMappingKindBoundSettlementNotables:
				return BuildSettlementNotableListText(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindSettlementParties:
			case TextMappingKindCurrentNpcCurrentSettlementParties:
			case TextMappingKindBoundSettlementParties:
				return BuildSettlementPartyListText(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindSettlementBoundVillages:
			case TextMappingKindCurrentNpcCurrentSettlementBoundVillages:
			case TextMappingKindBoundSettlementBoundVillages:
				return BuildSettlementBoundVillageListText(ResolveRuntimeSettlementFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindClanName:
				return GetClanDisplayName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindClanLeaderName:
				return GetHeroDisplayName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter)?.Leader);
			case TextMappingKindClanKingdomName:
			case TextMappingKindCurrentNpcClanKingdomName:
			case TextMappingKindPlayerClanKingdomName:
			case TextMappingKindBoundSettlementOwnerClanKingdomName:
			case TextMappingKindBoundHeroClanKingdomName:
				return GetClanKingdomName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindClanKingdomLeaderName:
			case TextMappingKindCurrentNpcClanKingdomLeaderName:
			case TextMappingKindPlayerClanKingdomLeaderName:
			case TextMappingKindBoundSettlementOwnerClanKingdomLeaderName:
			case TextMappingKindBoundHeroClanKingdomLeaderName:
				return GetClanKingdomLeaderName(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindClanAllTowns:
			case TextMappingKindCurrentNpcClanAllTowns:
			case TextMappingKindPlayerClanAllTowns:
			case TextMappingKindBoundSettlementOwnerClanAllTowns:
			case TextMappingKindBoundHeroClanAllTowns:
				return BuildClanOwnedSettlementListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: false, includeVillages: false);
			case TextMappingKindClanAllVillages:
			case TextMappingKindCurrentNpcClanAllVillages:
			case TextMappingKindPlayerClanAllVillages:
			case TextMappingKindBoundSettlementOwnerClanAllVillages:
			case TextMappingKindBoundHeroClanAllVillages:
				return BuildClanOwnedSettlementListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: false, includeCastles: false, includeVillages: true);
			case TextMappingKindClanAllSettlements:
			case TextMappingKindCurrentNpcClanAllSettlements:
			case TextMappingKindPlayerClanAllSettlements:
			case TextMappingKindBoundSettlementOwnerClanAllSettlements:
			case TextMappingKindBoundHeroClanAllSettlements:
				return BuildClanOwnedSettlementListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), includeTowns: true, includeCastles: true, includeVillages: true);
			case TextMappingKindClanMembers:
			case TextMappingKindCurrentNpcClanMembers:
			case TextMappingKindPlayerClanMembers:
			case TextMappingKindBoundSettlementOwnerClanMembers:
			case TextMappingKindBoundHeroClanMembers:
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), (Hero h) => true);
			case TextMappingKindClanMaleMembers:
			case TextMappingKindCurrentNpcClanMaleMembers:
			case TextMappingKindPlayerClanMaleMembers:
			case TextMappingKindBoundSettlementOwnerClanMaleMembers:
			case TextMappingKindBoundHeroClanMaleMembers:
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), (Hero h) => h != null && !h.IsFemale);
			case TextMappingKindClanFemaleMembers:
			case TextMappingKindCurrentNpcClanFemaleMembers:
			case TextMappingKindPlayerClanFemaleMembers:
			case TextMappingKindBoundSettlementOwnerClanFemaleMembers:
			case TextMappingKindBoundHeroClanFemaleMembers:
				return BuildClanMemberListText(ResolveRuntimeClanFromMapping(mapping, rule, npcHero, npcCharacter), (Hero h) => h != null && h.IsFemale);
			case TextMappingKindClanAgeRangeMembers:
			case TextMappingKindCurrentNpcClanAgeRangeMembers:
			case TextMappingKindPlayerClanAgeRangeMembers:
			case TextMappingKindBoundSettlementOwnerClanAgeRangeMembers:
			case TextMappingKindBoundHeroClanAgeRangeMembers:
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
			case TextMappingKindHeroName:
			case TextMappingKindCurrentNpcName:
			case TextMappingKindPlayerName:
			case TextMappingKindBoundHeroName:
				return GetHeroDisplayName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroClanName:
			case TextMappingKindCurrentNpcClanName:
			case TextMappingKindPlayerClanName:
			case TextMappingKindBoundHeroClanName:
				return GetHeroClanName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroKingdomName:
			case TextMappingKindPlayerKingdomName:
			case TextMappingKindBoundHeroKingdomName:
				return GetHeroKingdomName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroKingdomLeaderName:
			case TextMappingKindPlayerKingdomLeaderName:
			case TextMappingKindBoundHeroKingdomLeaderName:
				return GetHeroKingdomLeaderName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroSpouseName:
			case TextMappingKindCurrentNpcSpouseName:
			case TextMappingKindPlayerSpouseName:
			case TextMappingKindBoundHeroSpouseName:
				return GetHeroSpouseName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroFatherName:
			case TextMappingKindCurrentNpcFatherName:
			case TextMappingKindBoundHeroFatherName:
				return GetHeroFatherName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroMotherName:
			case TextMappingKindCurrentNpcMotherName:
			case TextMappingKindBoundHeroMotherName:
				return GetHeroMotherName(ResolveRuntimeHeroFromMapping(mapping, rule, npcHero, npcCharacter));
			case TextMappingKindHeroCurrentSettlementName:
			case TextMappingKindPlayerCurrentSettlementName:
			case TextMappingKindBoundHeroCurrentSettlementName:
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

	// 空值时优先使用 EmptyValueText；如果连兜底都没有，就保持“取不到值”状态。
	private static string ResolveTextMappingValueOrFallback(LoreTextMapping mapping, LoreRule rule = null, Hero npcHero = null, CharacterObject npcCharacter = null)
	{
		string text = ResolveTextMappingValue(mapping, rule, npcHero, npcCharacter);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return GetTextMappingEmptyValueText(mapping);
	}

	// 这个摘要会直接出现在“编辑词汇映射”列表里，所以强调的是：
	// 原文本、映射类型、目标对象和当前预览值。
	private static string BuildTextMappingSummary(LoreRule rule, LoreTextMapping mapping)
	{
		string text = TrimPreview((mapping?.SourceText ?? "").Trim(), 28);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = KnowledgeUiTexts.MappingSummaryEmpty;
		}
		string text2 = GetTextMappingKindLabel(mapping?.Kind);
		string text3 = TrimPreview(GetTextMappingTargetDisplayName(mapping, rule), 24);
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = KnowledgeUiTexts.MappingSummaryNotSelected;
		}
		string text4 = TrimPreview(ResolveTextMappingValueOrFallback(mapping, rule), 24);
		if (string.IsNullOrWhiteSpace(text4))
		{
			text4 = KnowledgeUiTexts.MappingSummaryNoValue;
		}
		string text5 = TrimPreview(GetTextMappingEmptyValueText(mapping), 18);
		if (IsTextMappingAgeRangeKind(mapping?.Kind))
		{
			int num = NormalizeMemberAgeBound(mapping?.AgeMin, 0);
			int num2 = NormalizeMemberAgeBound(mapping?.AgeMax, 120);
			text2 = KnowledgeUiTexts.MappingSummaryAgeRange(text2, num, num2);
		}
		if (IsStatusMappingKind(mapping?.Kind))
		{
			string text6 = TrimPreview(GetTextMappingStatusTrueText(mapping), 10);
			string text7 = TrimPreview(GetTextMappingStatusFalseText(mapping), 10);
			text2 = KnowledgeUiTexts.MappingSummaryStatus(text2, string.IsNullOrWhiteSpace(text6) ? KnowledgeUiTexts.MappingSummaryEmpty : text6, string.IsNullOrWhiteSpace(text7) ? KnowledgeUiTexts.MappingSummaryEmpty : text7);
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			text4 = text4 + KnowledgeUiTexts.MappingSummaryEmptyFallback(text5);
		}
		return KnowledgeUiTexts.MappingSummary(text, text2, text3, text4);
	}

	// 命中知识后，最终会在变体内容上执行这里的多轮替换。
	// 它不是模板引擎，而是受控的字符串替换，所以 SourceText 应该尽量明确。
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
					if (string.IsNullOrWhiteSpace(text3))
					{
						continue;
					}
					string text4 = text.Replace(text2, text3);
					if (!string.Equals(text4, text, StringComparison.Ordinal))
					{
						text = text4;
						flag = true;
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
		return BuildLoreContext(inputText, npcHero, secondaryInput, null);
	}

	public string BuildLoreContext(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities)
	{
		return BuildLoreContextInternal(inputText, npcHero, secondaryInput, mentionedEntities, includePlayerContext: true, null);
	}

	internal string BuildLoreContextWithCandidates(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities, LoreCandidateRules candidates)
	{
		return BuildLoreContextInternal(inputText, npcHero, secondaryInput, mentionedEntities, includePlayerContext: true, candidates);
	}

	public string BuildLoreContextWithoutPlayerContext(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities = null)
	{
		return BuildLoreContextInternal(inputText, npcHero, secondaryInput, mentionedEntities, includePlayerContext: false, null);
	}

	public long GetRuleDataVersionForExternal()
	{
		return Index.Version;
	}

	private string BuildLoreContextInternal(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities, bool includePlayerContext, LoreCandidateRules preselectedCandidates)
	{
		string text = (inputText ?? "").Trim();
		MentionedWorldEntities loreMentionedEntities = mentionedEntities?.Clone() ?? new MentionedWorldEntities();
		if (string.IsNullOrEmpty(text) && loreMentionedEntities.IsEmpty)
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
		bool includePlayerPersona = includePlayerContext && CanObserverReceivePlayerPersona(npcHero, null);
		string text2 = (npcHero?.StringId ?? "").Trim();
		string text3 = (npcHero?.Culture?.StringId ?? "neutral").Trim().ToLower();
		string text4 = "";
		try
		{
			text4 = (npcHero?.Clan?.Kingdom?.StringId ?? npcHero?.MapFaction?.StringId ?? "").Trim().ToLower();
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
			flag = npcHero?.IsFemale ?? false;
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
			text6 = (Settlement.CurrentSettlement?.StringId ?? npcHero?.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			text6 = "";
		}
		string text7 = "";
		try
		{
			text7 = (npcHero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			text7 = "";
		}
		string text7Keyword = text7;
		if (string.IsNullOrWhiteSpace(text7))
		{
			text7 = "该NPC";
		}
		string text8 = (text7 ?? "").Replace("|", " ").Trim();
		LogLoreContextTrace("hero", text2, "", text3, text4, text6, text5, flag, flag2, "", text);
		long ruleDataVersion = Index.Version;
		bool allowLoreContextCache = !HasAnyTextMappings();
		string mentionSignature = LoreCandidateRetriever.BuildMentionSignature(loreMentionedEntities.Entities, KnowledgeRuleIndex.GetLoreInjectLimit(Index.GetKnowledgeReturnCap()));
		string key = KnowledgeRuleIndex.Hash8($"{ruleDataVersion}|H|{text2}|{text8}|{text3}|{text4}|{text6}|{text5}|{(flag ? 1 : 0)}|{(flag2 ? 1 : 0)}|player_context={(includePlayerContext ? 1 : 0)}|player_persona={(includePlayerPersona ? 1 : 0)}|{BuildExactKeywordSlotCacheSignature()}|{mentionSignature}|{text}");
		if (allowLoreContextCache && TryGetLoreContextCache(key, ruleDataVersion, out var value))
		{
			return value;
		}
		string playerAppearanceForPrompt = includePlayerContext ? GetPlayerAppearanceForPrompt() : "";
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
			string textAppearanceOnly = BuildPermanentPlayerAppearanceContext(text7, playerAppearanceForPrompt);
			if (allowLoreContextCache)
			{
				PutLoreContextCache(key, ruleDataVersion, textAppearanceOnly);
			}
			return textAppearanceOnly;
		}
		bool flag3 = false;
		int num2 = 0;
		int num3 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		AppendPermanentPlayerAppearanceContext(stringBuilder, text7, playerAppearanceForPrompt);
		LoreCandidateRules candidateRules = preselectedCandidates ?? Retriever.CollectCandidateRules(loreMentionedEntities.Entities, KnowledgeRetrievalEnabledSafe());
		int loreInjectLimit = candidateRules?.InjectLimit ?? 0;
		if (loreInjectLimit <= 0)
		{
			loreInjectLimit = KnowledgeRuleIndex.GetLoreInjectLimit(Index.GetKnowledgeReturnCap());
		}
		string matchMode = candidateRules?.MatchMode ?? "none";
		List<LoreRule> list = candidateRules?.OrderedRules;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (!includePlayerContext)
		{
			// External systems such as world diplomacy provide a small, ordered list of
			// authoritative entities. Prefer their exact lore entries before semantic
			// recall so a shared culture name (for example, all three Imperial realms)
			// cannot displace the named kingdom. This path is cached by the caller and by
			// the lore-context cache, so the bounded exact scan is not on a daily hot path.
			foreach (string entityTerm in LoreCandidateRetriever.BuildMentionTerms(loreMentionedEntities.Entities))
			{
				if (num3 >= loreInjectLimit)
				{
					break;
				}
				if (TryFindRuleByExactKeyword(entityTerm, hashSet, out var matchedEntityRule)
					&& TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, matchedEntityRule, includePlayerPersona, "keyword_slot_external_entity", npcHero, null, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
				{
					num3++;
					num2++;
				}
			}
		}
		if (list != null)
		{
			foreach (LoreRule item in list)
			{
				if (num3 >= loreInjectLimit)
				{
					break;
				}
				if (TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, item, includePlayerPersona, matchMode, npcHero, null, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
				{
					num3++;
					num2++;
				}
			}
		}
		string text9 = includePlayerContext ? GetPlayerKeywordSlotKeyword() : "";
		if (includePlayerContext && !string.IsNullOrWhiteSpace(text9) && TryFindRuleByExactKeyword(text9, hashSet, out var matchedRule) && TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, matchedRule, includePlayerPersona, "keyword_slot_player", npcHero, null, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
		{
			num2++;
		}
		if (!string.IsNullOrWhiteSpace(text7Keyword) && TryFindRuleByExactKeyword(text7Keyword, hashSet, out var matchedNpcRule) && TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, matchedNpcRule, includePlayerPersona, "keyword_slot_npc", npcHero, null, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
		{
			num2++;
		}
		string text11 = ((num2 > 0 || !string.IsNullOrWhiteSpace(playerAppearanceForPrompt)) ? stringBuilder.ToString() : "");
		if (num2 == 0)
		{
			LogLoreMissOnce(((list == null || list.Count == 0) ? "rule_miss" : "variant_or_content_miss"), text, num, text2, text3, text4, text5);
			try
			{
				string value2 = ((list == null || list.Count == 0) ? $"reason=rule_miss rules={num} inputLen={text.Length} mode={matchMode} mentionTerms={LoreCandidateRetriever.CountMentionTerms(loreMentionedEntities.Entities)}" : $"reason=variant_or_content_miss candidates={list?.Count ?? 0} mode={matchMode} inputLen={text.Length} mentionTerms={LoreCandidateRetriever.CountMentionTerms(loreMentionedEntities.Entities)}");
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail(value2, secondaryInput), text);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: true, BuildKnowledgeHitRateDetail($"reason=ok matched={num2} candidates={list?.Count ?? 0} mode={matchMode} inputLen={text.Length} mentionTerms={LoreCandidateRetriever.CountMentionTerms(loreMentionedEntities.Entities)}", secondaryInput), text);
			}
			catch
			{
			}
		}
		if (allowLoreContextCache)
		{
			PutLoreContextCache(key, ruleDataVersion, text11);
		}
		return text11;
	}

	private static string RoleFromOccupation(Occupation occ)
	{
		return occ switch
		{
			Occupation.Soldier => "soldier", 
			Occupation.Villager => "villager", 
			Occupation.Townsfolk => "townsfolk", 
			Occupation.Wanderer => "wanderer", 
			Occupation.Lord => "lord", 
			_ => "commoner", 
		};
	}

	private static string GetRoleKeyForHero(Hero hero)
	{
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
			if (character.IsSoldier)
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
			string text = (npcCharacter?.StringId ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return (npcHero?.CharacterObject?.StringId ?? "").Trim();
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
		return ((roleKey ?? "").Trim().ToLowerInvariant()) switch
		{
			"lord" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleLord", "Lord"),
			"notable" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleNotable", "Notable"),
			"wanderer" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleWanderer", "Wanderer"),
			"soldier" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleSoldier", "Soldier"),
			"villager" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleVillager", "Villager"),
			"townsfolk" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleTownsfolk", "Townsfolk"),
			"commoner" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleCommoner", "Unclassified"),
			_ => string.IsNullOrWhiteSpace(roleKey) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleUnknown", "Unknown") : roleKey
		};
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
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
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

	public string BuildLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride = null, string secondaryInput = null)
	{
		return BuildLoreContext(inputText, npcCharacter, kingdomIdOverride, secondaryInput, null);
	}

	public string BuildLoreContext(string inputText, CharacterObject npcCharacter, string kingdomIdOverride, string secondaryInput, MentionedWorldEntities mentionedEntities)
	{
		return BuildLoreContextWithCandidates(inputText, npcCharacter, kingdomIdOverride, secondaryInput, mentionedEntities, null);
	}

	internal string BuildLoreContextWithCandidates(string inputText, CharacterObject npcCharacter, string kingdomIdOverride, string secondaryInput, MentionedWorldEntities mentionedEntities, LoreCandidateRules preselectedCandidates)
	{
		string text = (inputText ?? "").Trim();
		MentionedWorldEntities loreMentionedEntities = mentionedEntities?.Clone() ?? new MentionedWorldEntities();
		if (string.IsNullOrEmpty(text) && loreMentionedEntities.IsEmpty)
		{
			return "";
		}
		Hero hero = null;
		try
		{
			hero = ((npcCharacter != null && npcCharacter.IsHero) ? npcCharacter.HeroObject : null);
		}
		catch
		{
			hero = null;
		}
		string text2 = (hero?.StringId ?? "").Trim();
		string textCharId = (npcCharacter?.StringId ?? "").Trim();
		if (hero == null && npcCharacter == null)
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
		bool includePlayerPersona = CanObserverReceivePlayerPersona(hero, npcCharacter);
		string text3 = (hero?.Culture?.StringId ?? npcCharacter?.Culture?.StringId ?? "neutral").Trim().ToLower();
		string text4 = (kingdomIdOverride ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text4))
		{
			try
			{
				text4 = (hero?.Clan?.Kingdom?.StringId ?? hero?.MapFaction?.StringId ?? "").Trim().ToLower();
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
				text4 = (MobileParty.ConversationParty?.MapFaction?.StringId ?? "").Trim().ToLower();
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
				text4 = (Settlement.CurrentSettlement?.OwnerClan?.Kingdom?.StringId ?? Settlement.CurrentSettlement?.MapFaction?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text4 = "";
			}
		}
		string text5 = "commoner";
		try
		{
			if (hero != null)
			{
				text5 = (hero.IsLord ? "lord" : ((!hero.IsNotable) ? RoleFromOccupation(hero.Occupation) : "notable"));
			}
			else if (npcCharacter != null)
			{
				text5 = ((!npcCharacter.IsSoldier) ? RoleFromOccupation(npcCharacter.Occupation) : "soldier");
			}
		}
		catch
		{
		}
		bool flag = false;
		try
		{
			flag = hero?.IsFemale ?? npcCharacter?.IsFemale ?? false;
		}
		catch
		{
		}
		bool flag2 = false;
		try
		{
			flag2 = hero != null && hero.Clan != null && hero.Clan.Leader == hero;
		}
		catch
		{
		}
		string text6 = "";
		try
		{
			text6 = (Settlement.CurrentSettlement?.StringId ?? hero?.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			text6 = "";
		}
		string text7 = "";
		try
		{
			text7 = (hero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			text7 = "";
		}
		if (string.IsNullOrWhiteSpace(text7))
		{
			try
			{
				text7 = (npcCharacter?.Name?.ToString() ?? "").Trim();
			}
			catch
			{
				text7 = "";
			}
		}
		string text7Keyword = text7;
		if (string.IsNullOrWhiteSpace(text7))
		{
			text7 = "该NPC";
		}
		string text8 = (text7 ?? "").Replace("|", " ").Trim();
		LogLoreContextTrace((hero != null) ? "character_hero" : "character", text2, textCharId, text3, text4, text6, text5, flag, flag2, kingdomIdOverride, text);
		long ruleDataVersion = Index.Version;
		bool allowLoreContextCache = !HasAnyTextMappings();
		string mentionSignature = LoreCandidateRetriever.BuildMentionSignature(loreMentionedEntities.Entities, KnowledgeRuleIndex.GetLoreInjectLimit(Index.GetKnowledgeReturnCap()));
		string key = KnowledgeRuleIndex.Hash8($"{ruleDataVersion}|C|{text2}|{text8}|{text3}|{text4}|{text6}|{text5}|{(flag ? 1 : 0)}|{(flag2 ? 1 : 0)}|player_persona={(includePlayerPersona ? 1 : 0)}|{BuildExactKeywordSlotCacheSignature()}|{mentionSignature}|{text}");
		if (allowLoreContextCache && TryGetLoreContextCache(key, ruleDataVersion, out var value))
		{
			return value;
		}
		string playerAppearanceForPrompt = GetPlayerAppearanceForPrompt();
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
			string textAppearanceOnly = BuildPermanentPlayerAppearanceContext(text7, playerAppearanceForPrompt);
			if (allowLoreContextCache)
			{
				PutLoreContextCache(key, ruleDataVersion, textAppearanceOnly);
			}
			return textAppearanceOnly;
		}
		bool flag3 = false;
		int num2 = 0;
		int num3 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		AppendPermanentPlayerAppearanceContext(stringBuilder, text7, playerAppearanceForPrompt);
		LoreCandidateRules candidateRules = preselectedCandidates ?? Retriever.CollectCandidateRules(loreMentionedEntities.Entities, KnowledgeRetrievalEnabledSafe());
		int loreInjectLimit = candidateRules?.InjectLimit ?? KnowledgeRuleIndex.GetLoreInjectLimit(Index.GetKnowledgeReturnCap());
		string matchMode = candidateRules?.MatchMode ?? "none";
		List<LoreRule> list = candidateRules?.OrderedRules;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (list != null)
		{
			foreach (LoreRule item in list)
			{
				if (num3 >= loreInjectLimit)
				{
					break;
				}
				if (TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, item, includePlayerPersona, matchMode, hero, npcCharacter, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
				{
					num3++;
					num2++;
				}
			}
		}
		string text9 = GetPlayerKeywordSlotKeyword();
		if (!string.IsNullOrWhiteSpace(text9) && TryFindRuleByExactKeyword(text9, hashSet, out var matchedRule) && TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, matchedRule, includePlayerPersona, "keyword_slot_player", hero, npcCharacter, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
		{
			num2++;
		}
		if (!string.IsNullOrWhiteSpace(text7Keyword) && TryFindRuleByExactKeyword(text7Keyword, hashSet, out var matchedNpcRule) && TryAppendExactKeywordSlotLore(stringBuilder, ref flag3, matchedNpcRule, includePlayerPersona, "keyword_slot_npc", hero, npcCharacter, text2, text3, text4, text6, text5, flag, flag2, text7, text, secondaryInput, hashSet))
		{
			num2++;
		}
		string text11 = ((num2 > 0 || !string.IsNullOrWhiteSpace(playerAppearanceForPrompt)) ? stringBuilder.ToString() : "");
		if (num2 == 0)
		{
			LogLoreMissOnce(((list == null || list.Count == 0) ? "rule_miss" : "variant_or_content_miss"), text, num, text2, text3, text4, text5);
			try
			{
				string value2 = ((list == null || list.Count == 0) ? $"reason=rule_miss rules={num} inputLen={text.Length} mode={matchMode} mentionTerms={LoreCandidateRetriever.CountMentionTerms(loreMentionedEntities.Entities)}" : $"reason=variant_or_content_miss candidates={list?.Count ?? 0} mode={matchMode} inputLen={text.Length} mentionTerms={LoreCandidateRetriever.CountMentionTerms(loreMentionedEntities.Entities)}");
				Logger.RecordHitRate("knowledge", "__query__", hit: false, BuildKnowledgeHitRateDetail(value2, secondaryInput), text);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				Logger.RecordHitRate("knowledge", "__query__", hit: true, BuildKnowledgeHitRateDetail($"reason=ok matched={num2} candidates={list?.Count ?? 0} mode={matchMode} inputLen={text.Length} mentionTerms={LoreCandidateRetriever.CountMentionTerms(loreMentionedEntities.Entities)}", secondaryInput), text);
			}
			catch
			{
			}
		}
		if (allowLoreContextCache)
		{
			PutLoreContextCache(key, ruleDataVersion, text11);
		}
		return text11;
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
			return JsonConvert.SerializeObject(_file, pretty ? Formatting.Indented : Formatting.None);
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
				if (!string.IsNullOrWhiteSpace(LoreCandidateRetriever.NormalizeKeywordForCompare(rule.RagShortTexts[i])))
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
				error = KnowledgeUiTexts.ExportRuleEmpty;
				return false;
			}
			int num = 0;
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text = LoreCandidateRetriever.NormalizeKeywordForCompare(rule.RagShortTexts[i]);
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					num++;
					if (text.Length > RagShortTextMaxLength)
					{
						string text2 = GetRuleDisplayNameForExport(rule);
						string text3 = (rule.Id ?? "").Trim();
						error = KnowledgeUiTexts.ExportRagTooLong(text2, text3, RagShortTextMaxLength);
						return false;
					}
				}
			}
			if (requireAtLeastOne && num <= 0)
			{
				string text4 = GetRuleDisplayNameForExport(rule);
				string text5 = (rule.Id ?? "").Trim();
				error = KnowledgeUiTexts.ExportRagMissing(text4, text5);
				return false;
			}
		}
		catch (Exception ex)
		{
			error = KnowledgeUiTexts.ExportRagValidationFailed(ex.Message);
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
		return (rule?.Id ?? KnowledgeUiTexts.UnnamedKnowledge).Trim();
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
			error = KnowledgeUiTexts.ExportValidationFailed(ex.Message);
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
			error = KnowledgeUiTexts.ExportValidationFailed(ex.Message);
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
				error = KnowledgeUiTexts.ExportRuleIdEmpty;
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
				error = KnowledgeUiTexts.ExportRuleNotFound(text);
				return false;
			}
			return TryValidateRuleForExport(loreRule, out error);
		}
		catch (Exception ex)
		{
			error = KnowledgeUiTexts.ExportSingleValidationFailed(ex.Message);
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
			return JsonConvert.SerializeObject(loreRule, pretty ? Formatting.Indented : Formatting.None);
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
				_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
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
			knowledgeFile.PlayerAppearance = NormalizePlayerAppearanceForStorage(knowledgeFile.PlayerAppearance);
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
			_file.PlayerAppearance = NormalizePlayerAppearanceForStorage(_file.PlayerAppearance);
			if (overwrite)
			{
				_file = knowledgeFile;
			}
			else
			{
				if (string.IsNullOrWhiteSpace(_file.PlayerAppearance) && !string.IsNullOrWhiteSpace(knowledgeFile.PlayerAppearance))
				{
					_file.PlayerAppearance = knowledgeFile.PlayerAppearance;
				}
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
				_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
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

	// The terminal calls this read-only pass before confirmation, so malformed source lore cannot start a partial reload.
	public bool TryValidateDatabaseRulesPreservingPlayer(IEnumerable<LoreRule> sourceRules, out int removedCount, out int importedCount, out string error)
	{
		return TryBuildDatabaseRulesPreservingPlayer(sourceRules, out var _, out removedCount, out importedCount, out error);
	}

	// This user-triggered replacement is intentionally a one-shot O(n) operation: it swaps database lore while
	// retaining the only two player-owned fields, so normal retrieval never pays this cost.
	public bool ReplaceDatabaseRulesPreservingPlayer(IEnumerable<LoreRule> sourceRules, out int removedCount, out int importedCount, out string error)
	{
		if (!TryBuildDatabaseRulesPreservingPlayer(sourceRules, out KnowledgeFile knowledgeFile, out removedCount, out importedCount, out error))
		{
			return false;
		}
		_file = knowledgeFile;
		StripSemanticPrototypes();
		try
		{
			_storageJson = JsonConvert.SerializeObject(_file, Formatting.None);
		}
		catch
		{
		}
		TouchRuleData();
		return true;
	}

	// Build the replacement on locals first, so validation failure leaves the current save's knowledge untouched.
	private bool TryBuildDatabaseRulesPreservingPlayer(IEnumerable<LoreRule> sourceRules, out KnowledgeFile replacement, out int removedCount, out int importedCount, out string error)
	{
		replacement = null;
		removedCount = 0;
		importedCount = 0;
		error = "";
		try
		{
			if (sourceRules == null)
			{
				error = KnowledgeUiTexts.ReloadNoRules;
				return false;
			}
			List<LoreRule> list = new List<LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (LoreRule sourceRule in sourceRules)
			{
				if (sourceRule == null)
				{
					continue;
				}
				string text = (sourceRule.Id ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					error = KnowledgeUiTexts.ReloadEmptyRuleId;
					return false;
				}
				// A source package may contain its own player profile, but it must never replace this save's player data.
				if (IsPlayerPersonaRuleId(text))
				{
					continue;
				}
				if (!hashSet.Add(text))
				{
					error = KnowledgeUiTexts.ReloadDuplicateRuleId(text);
					return false;
				}
				sourceRule.Id = text;
				if (!TryValidateRagShortTexts(sourceRule, requireAtLeastOne: false, out var text2) || !ValidateVariantConditionsUnique(sourceRule, out var _, out var _))
				{
					error = string.IsNullOrWhiteSpace(text2) ? KnowledgeUiTexts.ReloadInvalidRule(text, "") : text2;
					return false;
				}
				list.Add(sourceRule);
			}
			if (list.Count <= 0)
			{
				error = KnowledgeUiTexts.ReloadNoReplaceableRules;
				return false;
			}
			KnowledgeFile knowledgeFile = _file ?? new KnowledgeFile();
			List<LoreRule> list2 = knowledgeFile.Rules ?? new List<LoreRule>();
			LoreRule loreRule = list2.FirstOrDefault(IsPlayerPersonaRule);
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (loreRule != null)
			{
				foreach (string keyword in loreRule.Keywords ?? Enumerable.Empty<string>())
				{
					string text3 = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword);
					if (!string.IsNullOrWhiteSpace(text3))
					{
						dictionary[text3] = PlayerPersonaRuleId;
					}
				}
			}
			foreach (LoreRule loreRule2 in list)
			{
				foreach (string keyword2 in loreRule2.Keywords ?? Enumerable.Empty<string>())
				{
					string text4 = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword2);
					if (string.IsNullOrWhiteSpace(text4))
					{
						continue;
					}
					if (dictionary.TryGetValue(text4, out var value) && !string.Equals(value, loreRule2.Id, StringComparison.OrdinalIgnoreCase))
					{
						error = KnowledgeUiTexts.ReloadKeywordConflict(text4);
						return false;
					}
					dictionary[text4] = loreRule2.Id;
				}
			}
			removedCount = list2.Count((LoreRule rule) => rule != null && !IsPlayerPersonaRule(rule));
			replacement = new KnowledgeFile
			{
				Version = (knowledgeFile.Version > 0) ? knowledgeFile.Version : 1,
				PlayerAppearance = NormalizePlayerAppearanceForStorage(knowledgeFile.PlayerAppearance),
				Rules = new List<LoreRule>()
			};
			if (loreRule != null)
			{
				replacement.Rules.Add(loreRule);
			}
			replacement.Rules.AddRange(list);
			importedCount = list.Count;
			return true;
		}
		catch (Exception ex)
		{
			error = KnowledgeUiTexts.ReloadFailed(ex.Message);
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
				if (loreVariant == null)
				{
					continue;
				}
				string whenSignature = BuildWhenSignature(loreVariant.When);
				if (dictionary.TryGetValue(whenSignature, out var value))
				{
					firstIndex = value;
					secondIndex = i;
					return false;
				}
				dictionary[whenSignature] = i;
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
			string text = BuildWhenSignature(when);
			for (int i = 0; i < rule.Variants.Count; i++)
			{
				if (i == excludeIndex)
				{
					continue;
				}
				LoreVariant loreVariant = rule.Variants[i];
				if (loreVariant != null && string.Equals(BuildWhenSignature(loreVariant.When), text, StringComparison.Ordinal))
				{
					return i;
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
			string text7 = (loreWhen.IsFemale.HasValue ? (loreWhen.IsFemale.Value ? "female" : "male") : "any");
			string text8 = (loreWhen.IsClanLeader.HasValue ? (loreWhen.IsClanLeader.Value ? "leader" : "not_leader") : "any");
			string text9 = string.Join("|", NormalizeWhenSkillMin(loreWhen.SkillMin).Select((KeyValuePair<string, int> kv) => kv.Key + ":" + kv.Value));
			return $"hero={text};culture={text2};kingdom={text3};settlement={text4};role={text5};identity={text6};gender={text7};clan={text8};skill={text9}";
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
			result = list.Select((string x) => (x ?? "").Trim().ToLowerInvariant()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).OrderBy((string x) => x, StringComparer.Ordinal).ToList();
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
			result = skillMin.Where((KeyValuePair<string, int> kv) => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value >= 0).Select((KeyValuePair<string, int> kv) => new KeyValuePair<string, int>((kv.Key ?? "").Trim().ToLowerInvariant(), kv.Value)).GroupBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.Ordinal).Select((IGrouping<string, KeyValuePair<string, int>> g) => new KeyValuePair<string, int>(g.Key, g.Max((KeyValuePair<string, int> x) => x.Value))).OrderBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.Ordinal).ToList();
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
		LoreWhen loreWhen = new LoreWhen
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
		return loreWhen;
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
		string text = ((existingIndex >= 0) ? ("#" + (existingIndex + 1)) : KnowledgeUiTexts.ExistingConditionFallback);
		string text2 = ((NormalizeWhenForStorage(CloneWhen(when)) == null) ? KnowledgeUiTexts.GeneralCondition : BuildWhenDetail(NormalizeWhenForStorage(CloneWhen(when))));
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=AF_KNOWLEDGE_DuplicateConditionTitle}Duplicate condition").ToString(), KnowledgeUiTexts.DuplicateVariantCondition(text, text2), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=AF_KNOWLEDGE_Back}Back").ToString(), "", null, null));
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
				MBReadOnlyList<SkillObject> mBReadOnlyList = Game.Current?.ObjectManager?.GetObjectTypeList<SkillObject>();
				if (mBReadOnlyList != null)
				{
					foreach (SkillObject item in mBReadOnlyList)
					{
						string text = (item?.StringId ?? "").Trim();
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
			SkillObject skillObject = FindSkillById(skillId);
			if (skillObject == null)
			{
				return false;
			}
			if (npcHero != null)
			{
				value = npcHero.GetSkillValue(skillObject);
				return true;
			}
			if (npcCharacter != null)
			{
				value = npcCharacter.GetSkillValue(skillObject);
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
					if (!TryParseRoleIdentityId(when.IdentityIds[n], out var kind, out var targetId))
					{
						continue;
					}
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
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit", KnowledgeUiTexts.EditorExisting, null));
		list.Add(new InquiryElement("create", KnowledgeUiTexts.EditorCreate, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.EditorTitle, KnowledgeUiTexts.ChooseAction, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	public void OpenPlayerPersonaSetup(Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		LoreRule orCreatePlayerPersonaRule = GetOrCreatePlayerPersonaRule();
		if (orCreatePlayerPersonaRule == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.PlayerPersonaCreateFailed));
			onReturn();
			return;
		}
		EnsurePlayerPersonaForcedKeyword(orCreatePlayerPersonaRule, showConflictMessage: true);
		OpenPlayerPersonaKeywordSetupMenu(orCreatePlayerPersonaRule, delegate
		{
			OpenPlayerPersonaIntroSetupMenu(orCreatePlayerPersonaRule, onReturn);
		});
	}

	public void OpenPlayerAppearanceAndBackgroundEditor(Action onReturn)
	{
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		LoreRule orCreatePlayerPersonaRule = GetOrCreatePlayerPersonaRule();
		if (orCreatePlayerPersonaRule == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.PlayerAppearanceCreateFailed));
			onReturn();
			return;
		}
		EnsurePlayerPersonaForcedKeyword(orCreatePlayerPersonaRule, showConflictMessage: true);
		OpenPlayerPersonaIntroSetupMenu(orCreatePlayerPersonaRule, onReturn);
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
		LoreRule loreRule = FindRule(PlayerPersonaRuleId);
		bool flag = false;
		if (loreRule == null)
		{
			loreRule = new LoreRule
			{
				Id = PlayerPersonaRuleId,
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
		string text = (Hero.MainHero?.Name?.ToString() ?? "玩家").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "玩家";
		}
		return text;
	}

	private bool EnsurePlayerPersonaForcedKeyword(LoreRule rule, bool showConflictMessage)
	{
		if (rule == null)
		{
			return false;
		}
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		string playerPersonaForcedKeyword = GetPlayerPersonaForcedKeyword();
		string text = LoreCandidateRetriever.NormalizeKeywordForCompare(playerPersonaForcedKeyword);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		bool flag = false;
		rule.Keywords.RemoveAll((string x) => string.IsNullOrWhiteSpace(x));
		for (int num = rule.Keywords.Count - 1; num >= 0; num--)
		{
			string text2 = LoreCandidateRetriever.NormalizeKeywordForCompare(rule.Keywords[num]);
			if (!string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (num == 0)
			{
				while (rule.Keywords.Count > 1 && string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(rule.Keywords[1]), text, StringComparison.OrdinalIgnoreCase))
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
		string foundRuleId;
		if (TryFindRuleIdByKeyword(playerPersonaForcedKeyword, rule.Id, out foundRuleId))
		{
			if (flag)
			{
				TouchRuleData();
			}
			if (showConflictMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.PlayerNameKeywordConflict(foundRuleId)));
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
		string text = LoreCandidateRetriever.NormalizeKeywordForCompare(GetPlayerPersonaForcedKeyword());
		foreach (string keyword in rule.Keywords)
		{
			string text2 = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword);
			if (!string.IsNullOrEmpty(text2) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase) && !list.Any((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), text2, StringComparison.OrdinalIgnoreCase)))
			{
				list.Add(keyword.Trim());
			}
		}
		return list;
	}

	private void OpenPlayerPersonaKeywordSetupMenu(LoreRule rule, Action onContinue)
	{
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
		string text = KnowledgeUiTexts.PlayerKeywordSetupBody(playerPersonaForcedKeyword, flag ? "" : KnowledgeUiTexts.PlayerNameConflictStatus, (playerPersonaOptionalKeywords.Count > 0) ? string.Join(" / ", playerPersonaOptionalKeywords) : KnowledgeUiTexts.NoValue);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("add", KnowledgeUiTexts.AddOptionalName, null));
		if (playerPersonaOptionalKeywords.Count > 0)
		{
			list.Add(new InquiryElement("remove", KnowledgeUiTexts.RemoveOptionalName, null));
		}
		list.Add(new InquiryElement("next", KnowledgeUiTexts.OpenAppearanceBackground, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.PlayerKeywordSetupTitle, text, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Continue, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "add":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.AddOptionalNameTitle, KnowledgeUiTexts.AddOptionalNamePrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						string text2 = (input ?? "").Trim();
						string kwNorm = LoreCandidateRetriever.NormalizeKeywordForCompare(text2);
						if (string.IsNullOrEmpty(kwNorm))
						{
							OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
						}
						else
						{
							bool flag = false;
							try
							{
								flag = rule.Keywords.Any((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), kwNorm, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag = false;
							}
							string foundRuleId;
							if (flag)
							{
								InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.KeywordAlreadyExists));
								OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
							}
							else if (TryFindRuleIdByKeyword(text2, rule.Id, out foundRuleId))
							{
								InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.KeywordConflict(foundRuleId)));
								OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
							}
							else
							{
								rule.Keywords.Add(text2);
								TouchRuleData();
								OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
							}
						}
					}, delegate
					{
						OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
					}));
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
		}, delegate
		{
			onContinue();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenPlayerPersonaKeywordRemoveMenu(LoreRule rule, Action onContinue)
	{
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
			list.Add(new InquiryElement(item, item, null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.RemoveOptionalNameTitle, KnowledgeUiTexts.RemoveOptionalNamePrompt, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Delete, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
					rule.Keywords.RemoveAll((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), LoreCandidateRetriever.NormalizeKeywordForCompare(text), StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
			}
		}, delegate
		{
			OpenPlayerPersonaKeywordSetupMenu(rule, onContinue);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenPlayerPersonaIntroSetupMenu(LoreRule rule, Action onDone)
	{
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
		string appearanceText = GetPlayerAppearanceForPrompt();
		string simpleText = "";
		if (playerPersonaSimpleVariantIndex >= 0 && rule.Variants != null && playerPersonaSimpleVariantIndex < rule.Variants.Count)
		{
			simpleText = rule.Variants[playerPersonaSimpleVariantIndex]?.Content ?? "";
		}
		List<int> playerPersonaDetailedVariantIndices = GetPlayerPersonaDetailedVariantIndices(rule);
		string text2 = BuildPlayerPersonaIntroMenuBody(appearanceText, simpleText, playerPersonaDetailedVariantIndices.Count);
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("appearance", KnowledgeUiTexts.PlayerAppearanceOption, BuildPlayerPersonaOptionDetail(appearanceText), isPrimary: true),
			new DevLargeSelectionPopup.Option("simple", KnowledgeUiTexts.PlayerBackgroundOption, BuildPlayerPersonaOptionDetail(simpleText), isPrimary: true),
			new DevLargeSelectionPopup.Option("detailed", KnowledgeUiTexts.PlayerDetailedBackgroundOption, KnowledgeUiTexts.PlayerDetailedBackgroundCount(playerPersonaDetailedVariantIndices.Count)),
			new DevLargeSelectionPopup.Option("done", KnowledgeUiTexts.PlayerAppearanceBackgroundDone, KnowledgeUiTexts.PlayerAppearanceBackgroundDoneDetail)
		};
		if (DevLargeSelectionPopup.Show(KnowledgeUiTexts.PlayerAppearanceBackgroundTitle, KnowledgeUiTexts.PlayerAppearanceBackgroundDescription, text2, options, delegate(string selectedId)
		{
			HandlePlayerPersonaIntroSelection(rule, onDone, selectedId);
		}, onDone, KnowledgeUiTexts.Skip))
		{
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("appearance", KnowledgeUiTexts.PlayerAppearanceOption, null));
		list.Add(new InquiryElement("simple", KnowledgeUiTexts.PlayerBackgroundOption, null));
		list.Add(new InquiryElement("detailed", KnowledgeUiTexts.PlayerDetailedBackgroundOption, null));
		list.Add(new InquiryElement("done", KnowledgeUiTexts.PlayerAppearanceBackgroundDone, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.PlayerAppearanceBackgroundTitle, text2, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Skip, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPlayerPersonaIntroSetupMenu(rule, onDone);
			}
			else
			{
				HandlePlayerPersonaIntroSelection(rule, onDone, selected[0].Identifier as string);
			}
		}, delegate
		{
			onDone();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildPlayerPersonaIntroMenuBody(string appearanceText, string simpleText, int detailedCount)
	{
		return KnowledgeUiTexts.PlayerAppearanceBackgroundBody(string.IsNullOrWhiteSpace(appearanceText) ? KnowledgeUiTexts.NoValue : NormalizePlayerAppearanceForStorage(appearanceText), string.IsNullOrWhiteSpace(simpleText) ? KnowledgeUiTexts.NoValue : (simpleText ?? "").Trim(), detailedCount);
	}

	private static string BuildPlayerPersonaOptionDetail(string text)
	{
		int length = string.IsNullOrWhiteSpace(text) ? 0 : (text ?? "").Trim().Length;
		if (length <= 0)
		{
			return KnowledgeUiTexts.PlayerOptionUnset;
		}
		return KnowledgeUiTexts.PlayerOptionFilled(length);
	}

	private void HandlePlayerPersonaIntroSelection(LoreRule rule, Action onDone, string selectedId)
	{
		switch (selectedId)
		{
		case "appearance":
			OpenPlayerPersonaAppearanceEditor(delegate
			{
				OpenPlayerPersonaIntroSetupMenu(rule, onDone);
			});
			break;
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
			onDone?.Invoke();
			break;
		default:
			OpenPlayerPersonaIntroSetupMenu(rule, onDone);
			break;
		}
	}

	private void OpenPlayerPersonaAppearanceEditor(Action onReturn)
	{
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
		string text = GetPlayerAppearanceForPrompt();
		DevTextEditorHelper.ShowLongTextEditor(KnowledgeUiTexts.PlayerAppearanceEditorTitle, KnowledgeUiTexts.PlayerAppearanceEditorIntro, KnowledgeUiTexts.PlayerAppearanceEditorHint, text, delegate(string input)
		{
			string text2 = NormalizePlayerAppearanceForStorage(input);
			if (!string.Equals(_file.PlayerAppearance ?? "", text2, StringComparison.Ordinal))
			{
				_file.PlayerAppearance = text2;
				TouchRuleData();
			}
			onReturn();
		}, delegate
		{
			onReturn();
		}, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Back);
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
		string text = ((playerPersonaSimpleVariantIndex >= 0 && playerPersonaSimpleVariantIndex < rule.Variants.Count) ? (rule.Variants[playerPersonaSimpleVariantIndex]?.Content ?? "") : "");
		DevTextEditorHelper.ShowLongTextEditor(KnowledgeUiTexts.PlayerBackgroundEditorTitle, KnowledgeUiTexts.PlayerBackgroundEditorIntro, KnowledgeUiTexts.PlayerBackgroundEditorHint, text, delegate(string input)
		{
			string text2 = (input ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				if (playerPersonaSimpleVariantIndex >= 0 && playerPersonaSimpleVariantIndex < rule.Variants.Count)
				{
					rule.Variants.RemoveAt(playerPersonaSimpleVariantIndex);
					TouchRuleData();
				}
			}
			else if (playerPersonaSimpleVariantIndex >= 0 && playerPersonaSimpleVariantIndex < rule.Variants.Count)
			{
				LoreVariant loreVariant2 = rule.Variants[playerPersonaSimpleVariantIndex];
				if (loreVariant2 != null)
				{
					loreVariant2.Priority = 0;
					loreVariant2.When = null;
					loreVariant2.Content = input ?? "";
					TouchRuleData();
				}
			}
			else
			{
				rule.Variants.Add(new LoreVariant
				{
					Priority = 0,
					When = null,
					Content = input ?? ""
				});
				TouchRuleData();
			}
			onReturn();
		}, delegate
		{
			onReturn();
		}, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Back);
	}

	private void OpenPlayerPersonaDetailedIntroMenu(LoreRule rule, Action onReturn)
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
		List<int> playerPersonaDetailedVariantIndices = GetPlayerPersonaDetailedVariantIndices(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("add", KnowledgeUiTexts.AddDetailedBackground, null));
		foreach (int item in playerPersonaDetailedVariantIndices)
		{
			LoreVariant loreVariant = rule.Variants[item];
			string arg = BuildWhenLabel(loreVariant.When);
			string text = TrimPreview(loreVariant.Content ?? "", 24);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = KnowledgeUiTexts.EmptyPreview;
			}
			list.Add(new InquiryElement(item, $"#{item + 1} {arg}  {text}", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.DetailedBackgroundListTitle, KnowledgeUiTexts.DetailedBackgroundListDescription, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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
				int duplicateVariantIndex = FindDuplicateVariantIndex(rule, loreWhen, num);
				if (duplicateVariantIndex >= 0)
				{
					ShowDuplicateVariantConditionPrompt(loreWhen, duplicateVariantIndex);
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
			text2 = KnowledgeUiTexts.EmptyPreview;
		}
		string text3 = KnowledgeUiTexts.DetailedBackgroundEditorDescription(text, text2);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("content", KnowledgeUiTexts.EditDetailedBackgroundContent, null));
		list.Add(new InquiryElement("when", KnowledgeUiTexts.EditDetailedBackgroundCondition, null));
		list.Add(new InquiryElement("delete", KnowledgeUiTexts.DeleteDetailedBackground, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.DetailedBackgroundEditorTitle, text3, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
					DevTextEditorHelper.ShowLongTextEditor(KnowledgeUiTexts.DetailedBackgroundContentTitle, KnowledgeUiTexts.DetailedBackgroundContentIntro, KnowledgeUiTexts.DetailedBackgroundContentHint, v.Content ?? "", delegate(string input)
					{
						v.Content = input ?? "";
						TouchRuleData();
						OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
					}, delegate
					{
						OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
					}, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel);
					break;
				case "when":
				{
					LoreWhen loreWhen = CloneWhen(v.When) ?? new LoreWhen();
					OpenWhenEditor(loreWhen, delegate
					{
						loreWhen = NormalizeWhenForStorage(loreWhen);
						if (loreWhen == null)
						{
							InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.DetailedBackgroundRequiresCondition));
							OpenPlayerPersonaDetailedVariantEditor(rule, idx, onReturn, isNewVariant);
						}
						else
						{
							int duplicateVariantIndex = FindDuplicateVariantIndex(rule, loreWhen, idx);
							if (duplicateVariantIndex >= 0)
							{
								ShowDuplicateVariantConditionPrompt(loreWhen, duplicateVariantIndex);
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
		}, delegate
		{
			if (isNewVariant && idx >= 0 && idx < rule.Variants.Count && string.IsNullOrWhiteSpace(rule.Variants[idx]?.Content))
			{
				rule.Variants.RemoveAt(idx);
				TouchRuleData();
			}
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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
		if (onReturn == null)
		{
			onReturn = delegate
			{
			};
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.SettingsUnavailable));
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
		string text = AIConfigHandler.KnowledgeRetrievalFromMcm ? KnowledgeUiTexts.RetrievalSourceMcm : KnowledgeUiTexts.RetrievalSourceFile;
		string text2 = KnowledgeUiTexts.RetrievalMode;
		string descriptionText = KnowledgeUiTexts.RetrievalDescription(text, text2, AIConfigHandler.KnowledgeRetrievalEnabled ? KnowledgeUiTexts.RetrievalOn : KnowledgeUiTexts.RetrievalOff, AIConfigHandler.KnowledgeSemanticFirst ? KnowledgeUiTexts.RetrievalYes : KnowledgeUiTexts.RetrievalNoKeywordFirst, AIConfigHandler.KnowledgeSemanticTopK);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("source", KnowledgeUiTexts.RetrievalSwitchSource(settings.UseMcmKnowledgeRetrieval ? KnowledgeUiTexts.RetrievalSourceFile : KnowledgeUiTexts.RetrievalSourceMcm), null));
		list.Add(new InquiryElement("enabled", KnowledgeUiTexts.RetrievalToggle(settings.KnowledgeRetrievalEnabled ? KnowledgeUiTexts.RetrievalOff : KnowledgeUiTexts.RetrievalOn), null));
		list.Add(new InquiryElement("topk", KnowledgeUiTexts.RetrievalTopK(topK), null));
		list.Add(new InquiryElement("reset", KnowledgeUiTexts.RetrievalReset, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.RetrievalSettingsTitle, descriptionText, list, isExitShown: true, 0, 1, KnowledgeUiTexts.RetrievalSelect, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRetrievalSettingsMenu(onReturn);
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				if (string.IsNullOrEmpty(text3))
				{
					OpenRetrievalSettingsMenu(onReturn);
				}
				else
				{
					switch (text3)
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
						InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.RetrievalTopKTitle, KnowledgeUiTexts.RetrievalTopKPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
						{
							string s = (input ?? "").Trim();
							if (!int.TryParse(s, out var result))
							{
								InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.InvalidInteger));
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
						}, delegate
						{
							OpenRetrievalSettingsMenu(onReturn);
						}, shouldInputBeObfuscated: false, null, topK.ToString(CultureInfo.InvariantCulture)));
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRuleListMenu(Action onReturn)
	{
		OpenRuleListMenuPaged(onReturn, 0, null);
	}

	private void OpenRuleListMenuPaged(Action onReturn, int page, string query)
	{
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
		string text = KnowledgeUiTexts.EditorExisting;
		string titleText = ((num5 > 0) ? KnowledgeUiTexts.RuleListTitle(num3, page + 1, num5) : KnowledgeUiTexts.RuleListEmptyTitle);
		string text2 = KnowledgeUiTexts.RuleListChoose;
		if (!string.IsNullOrWhiteSpace(q))
		{
			text2 = text2 + "\n" + KnowledgeUiTexts.RuleListSearch(TrimPreview(q, 80));
		}
		if (num3 <= 0)
		{
			text2 += "\n\n" + KnowledgeUiTexts.RuleListNoMatches;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.SearchByKeywordId, null));
		if (!string.IsNullOrWhiteSpace(q))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (num5 > 0 && page + 1 < num5)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list3.Add(new InquiryElement("__create__", KnowledgeUiTexts.EditorCreate, null));
		list3.Add(new InquiryElement("__bulk_delete__", KnowledgeUiTexts.BulkDelete, null));
		if (list.Count > 0)
		{
			list3.Add(new InquiryElement("__sep__", "----------------", null));
		}
		foreach (RuleIndexItem item in list)
		{
			string text3 = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text3))
			{
				string title = (item?.Label ?? text3).Trim();
				list3.Add(new InquiryElement(text3, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, text2, list3, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRuleListMenuPaged(onReturn, page, query);
			}
			else
			{
				string text4 = selected[0].Identifier as string;
				switch (text4)
				{
				case "__sep__":
					OpenRuleListMenuPaged(onReturn, page, query);
					break;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.SearchKnowledgeTitle, KnowledgeUiTexts.SearchKnowledgePrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						OpenRuleListMenuPaged(onReturn, 0, input);
					}, delegate
					{
						OpenRuleListMenuPaged(onReturn, page, query);
					}, shouldInputBeObfuscated: false, null, q));
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
					string id = text4;
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void CreateNewRule(Action onReturn)
	{
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
		InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.CreateKnowledgeTitle, KnowledgeUiTexts.CreateKnowledgePrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Create, KnowledgeUiTexts.Back, delegate(string input)
		{
			string text = (input ?? "").Trim();
			string text2 = BuildRuleIdFromKeyword(text);
			string foundRuleId;
			if (string.IsNullOrEmpty(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.CreateRuleEmpty));
				CreateNewRule(onReturn);
			}
			else if (HasRuleIdIgnoreCase(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.CreateRuleDuplicate(text2)));
				CreateNewRule(onReturn);
			}
			else if (TryFindRuleIdByKeyword(text, null, out foundRuleId))
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.CreateRuleKeywordConflict(text, foundRuleId)));
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
		}, delegate
		{
			OpenRuleListMenu(onReturn);
		}));
	}

	private void OpenRuleBulkDeleteMenuPaged(Action onReturn, int page, string query)
	{
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
		string[] array = null;
		try
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				array = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			}
		}
		catch
		{
			array = null;
		}
		int num = page * RuleListPageSize;
		int num2 = num + RuleListPageSize;
		int num3 = 0;
		List<RuleIndexItem> list = new List<RuleIndexItem>();
		try
		{
			List<RuleIndexItem> list2 = _ruleIndexCache ?? new List<RuleIndexItem>();
			for (int i = 0; i < list2.Count; i++)
			{
				RuleIndexItem ruleIndexItem = list2[i];
				if (ruleIndexItem != null && IsRuleListMatch(ruleIndexItem, array))
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
		int num4 = ((num3 > 0) ? ((num3 + RuleListPageSize - 1) / RuleListPageSize) : 0);
		if (num4 > 0 && page >= num4)
		{
			OpenRuleBulkDeleteMenuPaged(onReturn, num4 - 1, query);
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
	list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.SearchByKeywordId, null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (num4 > 0 && page + 1 < num4)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		if (list.Count > 0)
		{
			list3.Add(new InquiryElement("__sep__", "----------------", null));
			foreach (RuleIndexItem item in list)
			{
				string text2 = (item?.Id ?? "").Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					string label = (item?.Label ?? text2).Trim();
					list3.Add(new InquiryElement(text2, label, null));
				}
			}
		}
	string text3 = (num4 > 0) ? KnowledgeUiTexts.BulkDeleteTitle(num3, page + 1, num4) : KnowledgeUiTexts.BulkDeleteEmptyTitle;
	string text4 = KnowledgeUiTexts.BulkDeleteDescription(TrimPreview(text, 80), num3 <= 0);
	MultiSelectionInquiryData data = new MultiSelectionInquiryData(text3, text4, list3, isExitShown: true, 0, Math.Max(1, list3.Count), KnowledgeUiTexts.DeleteSelected, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
				return;
			}
			List<string> list4 = new List<string>();
			foreach (InquiryElement item2 in selected)
			{
				string text5 = item2?.Identifier as string;
				if (!string.IsNullOrWhiteSpace(text5))
				{
					list4.Add(text5);
				}
			}
			if (list4.Count == 0)
			{
				OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
				return;
			}
			List<string> list5 = list4.Where((string x) => x.StartsWith("__", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
			if (list5.Count > 0)
			{
				if (list4.Count > 1)
				{
					InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.BulkActionsCannotMix));
					OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
					return;
				}
				switch (list5[0])
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.SearchKnowledgeTitle, KnowledgeUiTexts.SearchKnowledgePrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						OpenRuleBulkDeleteMenuPaged(onReturn, 0, input);
					}, delegate
					{
						OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
					}, shouldInputBeObfuscated: false, null, text));
					return;
				case "__clear__":
					OpenRuleBulkDeleteMenuPaged(onReturn, 0, null);
					return;
				case "__prev__":
					OpenRuleBulkDeleteMenuPaged(onReturn, page - 1, query);
					return;
				case "__next__":
					OpenRuleBulkDeleteMenuPaged(onReturn, page + 1, query);
					return;
				default:
					OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
					return;
				}
			}
			List<RuleIndexItem> list6 = list.Where((RuleIndexItem x) => x != null && list4.Contains((x.Id ?? "").Trim(), StringComparer.OrdinalIgnoreCase)).ToList();
			if (list6.Count == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.NoRulesSelected));
				OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
				return;
			}
			List<string> list7 = list6.Select((RuleIndexItem x) => (x.Id ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			string text6 = string.Join("\n", list6.Take(8).Select((RuleIndexItem x) => "- " + TrimPreview((x.Label ?? x.Id ?? "").Trim(), 80)));
			if (list6.Count > 8)
			{
				text6 = text6 + "\n- " + KnowledgeUiTexts.BulkDeleteMore(list6.Count - 8);
			}
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=AF_KNOWLEDGE_ConfirmBulkDeleteTitle}Confirm bulk deletion").ToString(), KnowledgeUiTexts.ConfirmBulkDelete(text6, list7.Count), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=AF_KNOWLEDGE_ConfirmDelete}Delete").ToString(), new TextObject("{=AF_KNOWLEDGE_Back}Back").ToString(), delegate
			{
				int num5 = 0;
				try
				{
					HashSet<string> hashSet = new HashSet<string>(list7, StringComparer.OrdinalIgnoreCase);
					num5 = _file.Rules.RemoveAll((LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id) && hashSet.Contains(r.Id.Trim()));
					if (!string.IsNullOrWhiteSpace(_editingRuleId) && hashSet.Contains(_editingRuleId.Trim()))
					{
						_editingRuleId = null;
					}
					TouchRuleData();
				}
				catch
				{
					num5 = 0;
				}
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Deleted(num5)));
				OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
			}, delegate
			{
				OpenRuleBulkDeleteMenuPaged(onReturn, page, query);
			}));
		}, delegate
		{
			OpenRuleListMenuPaged(onReturn, page, query);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRuleEditorMenu(LoreRule rule, Action onReturn)
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
		string text = ((rule.Keywords != null && rule.Keywords.Count > 0) ? rule.Keywords[0] : (rule.Id ?? "Knowledge"));
		EnsureRagShortTexts(rule);
		EnsureTextMappings(rule);
		string descriptionText = KnowledgeUiTexts.RuleMenuDescription(rule.Keywords?.Count ?? 0, rule.RagShortTexts?.Count ?? 0, rule.Variants?.Count ?? 0, rule.TextMappings?.Count ?? 0);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("keywords", KnowledgeUiTexts.EditKeywords, null));
		list.Add(new InquiryElement("rag_short_texts", KnowledgeUiTexts.EditRag, null));
		list.Add(new InquiryElement("generate_rag_short_texts", KnowledgeUiTexts.GenerateRag, null));
		list.Add(new InquiryElement("variants", KnowledgeUiTexts.EditVariants, null));
		list.Add(new InquiryElement("text_mappings", KnowledgeUiTexts.EditMappings, null));
		list.Add(new InquiryElement("delete", KnowledgeUiTexts.DeleteKnowledge, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.RuleMenuTitle(text), descriptionText, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	// 词汇映射的主菜单，负责列出当前规则下的全部映射并进入新增/编辑流程。
	private void OpenTextMappingMenu(LoreRule rule, Action onReturn)
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
		EnsureTextMappings(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__add__", KnowledgeUiTexts.AddMapping, null));
		if (rule.TextMappings != null)
		{
			for (int i = 0; i < rule.TextMappings.Count; i++)
			{
				LoreTextMapping loreTextMapping = rule.TextMappings[i];
				if (loreTextMapping != null)
				{
					list.Add(new InquiryElement(i, $"#{i + 1} {BuildTextMappingSummary(rule, loreTextMapping)}", null));
				}
			}
		}
		string descriptionText = KnowledgeUiTexts.MappingListDescription;
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.EditMappings, descriptionText, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingMenu(rule, onReturn);
			}
			else if ((selected[0].Identifier as string) == "__add__")
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	// 创建/编辑流程分成三步：
	// 输入原文本 -> 选择 kind -> 选择目标并补充额外配置（年龄段或状态文案）。
	private void CreateTextMapping(LoreRule rule, Action onReturn)
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
		EnsureTextMappings(rule);
		InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.AddMappingTitle, KnowledgeUiTexts.AddMappingPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.NextStep, KnowledgeUiTexts.Cancel, delegate(string input)
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
		}, delegate
		{
			OpenTextMappingMenu(rule, onReturn);
		}));
	}

	private void OpenTextMappingEditor(LoreRule rule, int idx, Action onReturn)
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
		string text2 = GetTextMappingEmptyValueText(loreTextMapping);
		string text3 = GetTextMappingStatusTrueText(loreTextMapping);
		string text4 = GetTextMappingStatusFalseText(loreTextMapping);
		string text5 = (!string.IsNullOrWhiteSpace(text)) ? text : (string.IsNullOrWhiteSpace(text2) ? KnowledgeUiTexts.MappingSummaryNoValue : text2);
		string sourceText = string.IsNullOrWhiteSpace(loreTextMapping.SourceText) ? KnowledgeUiTexts.MappingSummaryEmpty : loreTextMapping.SourceText.Trim();
		string ageRange = "";
		if (IsTextMappingAgeRangeKind(loreTextMapping.Kind))
		{
			int num = NormalizeMemberAgeBound(loreTextMapping.AgeMin, 0);
			int num2 = NormalizeMemberAgeBound(loreTextMapping.AgeMax, 120);
			ageRange = num + "-" + num2;
		}
		string statusText = IsStatusMappingKind(loreTextMapping.Kind) ? KnowledgeUiTexts.MappingEditorStatus(string.IsNullOrWhiteSpace(text3) ? KnowledgeUiTexts.NoValue : text3, string.IsNullOrWhiteSpace(text4) ? KnowledgeUiTexts.NoValue : text4) : "";
		string emptyText = string.IsNullOrWhiteSpace(text2) ? "" : KnowledgeUiTexts.MappingEditorEmpty(text2);
		string ageText = string.IsNullOrEmpty(ageRange) ? "" : KnowledgeUiTexts.MappingEditorAgeRange(ageRange);
		string descriptionText = KnowledgeUiTexts.MappingEditorDescription(sourceText, GetTextMappingKindLabel(loreTextMapping.Kind), GetTextMappingTargetTypeLabel(loreTextMapping.Kind), GetTextMappingTargetDisplayName(loreTextMapping, rule), statusText, emptyText, text5, ageText);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("source", KnowledgeUiTexts.EditSource, null));
		list.Add(new InquiryElement("target", KnowledgeUiTexts.EditTarget, null));
		if (IsStatusMappingKind(loreTextMapping.Kind))
		{
			list.Add(new InquiryElement("true_text", KnowledgeUiTexts.EditTrueText, null));
			list.Add(new InquiryElement("false_text", KnowledgeUiTexts.EditFalseText, null));
		}
		list.Add(new InquiryElement("empty_value", KnowledgeUiTexts.EditEmptyText, null));
		if (IsTextMappingAgeRangeKind(loreTextMapping.Kind))
		{
			list.Add(new InquiryElement("age_range", KnowledgeUiTexts.EditAgeRange, null));
		}
		list.Add(new InquiryElement("delete", KnowledgeUiTexts.DeleteMapping, null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.MappingEditorTitle, descriptionText, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingEditor(rule, idx, onReturn);
			}
			else
			{
				switch (selected[0].Identifier as string)
				{
				case "source":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.EditSource, KnowledgeUiTexts.EditSourcePrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						string text = (input ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(text))
						{
							loreTextMapping.SourceText = text;
							TouchRuleData();
						}
						OpenTextMappingEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, shouldInputBeObfuscated: false, null, loreTextMapping.SourceText ?? ""));
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
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.EditTrueText, KnowledgeUiTexts.TrueTextPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						loreTextMapping.TrueText = (input ?? "").Trim();
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, shouldInputBeObfuscated: false, null, loreTextMapping.TrueText ?? ""));
					break;
				case "false_text":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.EditFalseText, KnowledgeUiTexts.FalseTextPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						loreTextMapping.FalseText = (input ?? "").Trim();
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, shouldInputBeObfuscated: false, null, loreTextMapping.FalseText ?? ""));
					break;
				case "empty_value":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.EditEmptyText, KnowledgeUiTexts.EmptyTextPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						loreTextMapping.EmptyValueText = (input ?? "").Trim();
						TouchRuleData();
						OpenTextMappingEditor(rule, idx, onReturn);
					}, delegate
					{
						OpenTextMappingEditor(rule, idx, onReturn);
					}, shouldInputBeObfuscated: false, null, loreTextMapping.EmptyValueText ?? ""));
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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

	// 状态映射和年龄段映射都需要额外参数，这里单独拆成小编辑器，避免主流程太乱。
	private void OpenTextMappingAgeRangeEditor(LoreTextMapping mapping, Action<LoreTextMapping> onConfigured, Action onCancel)
	{
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
		InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.AgeRangeTitle, KnowledgeUiTexts.AgeRangePrompt(num, num2), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=AF_KNOWLEDGE_Confirm}Confirm").ToString(), new TextObject("{=AF_KNOWLEDGE_Back}Back").ToString(), delegate(string input)
		{
			string[] array = ((input ?? "").Trim()).Replace(" ", "").Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 2 || !int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2))
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.InvalidAgeRange));
				OpenTextMappingAgeRangeEditor(loreTextMapping, onConfigured, onCancel);
				return;
			}
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
		}, delegate
		{
			onCancel();
		}, shouldInputBeObfuscated: false, null, $"{num}-{num2}"));
	}

	private void OpenTextMappingStatusOutcomeEditor(LoreTextMapping mapping, Action<LoreTextMapping> onConfigured, Action onCancel)
	{
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
		InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.SetTrueTextTitle, KnowledgeUiTexts.SetTrueTextPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.NextStep, KnowledgeUiTexts.Back, delegate(string input)
		{
			loreTextMapping.TrueText = (input ?? "").Trim();
			InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.SetFalseTextTitle, KnowledgeUiTexts.SetFalseTextPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Finish, KnowledgeUiTexts.Back, delegate(string input2)
			{
				loreTextMapping.FalseText = (input2 ?? "").Trim();
				onConfigured(loreTextMapping);
			}, delegate
			{
				OpenTextMappingStatusOutcomeEditor(loreTextMapping, onConfigured, onCancel);
			}, shouldInputBeObfuscated: false, null, loreTextMapping.FalseText ?? ""));
		}, delegate
		{
			onCancel();
		}, shouldInputBeObfuscated: false, null, loreTextMapping.TrueText ?? ""));
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
			return;
		}
		if (IsTextMappingAgeRangeKind(mapping.Kind) && (mapping.AgeMin == null || mapping.AgeMax == null))
		{
			OpenTextMappingAgeRangeEditor(mapping, delegate(LoreTextMapping result)
			{
				ConfigureTextMappingAfterTargetSelection(result, onPicked, onCancel);
			}, onCancel);
			return;
		}
		if (IsStatusMappingKind(mapping.Kind) && string.IsNullOrWhiteSpace(GetTextMappingStatusTrueText(mapping)) && string.IsNullOrWhiteSpace(GetTextMappingStatusFalseText(mapping)))
		{
			OpenTextMappingStatusOutcomeEditor(mapping, onPicked, onCancel);
			return;
		}
		onPicked(mapping);
	}

	// kind 选择器负责“映射类型”这一层，不直接关心目标对象。
	// 它支持分类浏览和全文搜索，适合在映射项很多时快速定位。
	private static string GetTextMappingKindCategoryLabel(string categoryKey)
	{
		return (categoryKey ?? "").Trim() switch
		{
			"status" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryStatus", "Status checks"),
			"current_npc" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryCurrentNpc", "What about the current NPC?"),
			"player" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryPlayer", "What about the player?"),
			"bound" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryBound", "What about the object bound to this knowledge?"),
			"kingdom" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryKingdom", "What about the kingdom?"),
			"settlement" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategorySettlement", "What about the settlement?"),
			"clan" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryClan", "What about the clan?"),
			"hero" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryHero", "What about the hero?"),
			_ => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryOther", "Other")
		};
	}

	private static string GetTextMappingKindCategoryDescription(string categoryKey)
	{
		return (categoryKey ?? "").Trim() switch
		{
			"status" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryStatusDescription", "These mappings check a status first, then output your true/false text. Other mapping tokens can also be used in those texts."),
			"current_npc" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryCurrentNpcDescription", "These mappings read values from the NPC in the current interaction; choose which value to retrieve."),
			"player" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryPlayerDescription", "These mappings read values from the player character; choose which value to retrieve."),
			"bound" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryBoundDescription", "These mappings read from the kingdom, settlement or hero uniquely bound by this knowledge entry's conditions."),
			"kingdom" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryKingdomDescription", "Choose a kingdom, then choose which value to retrieve."),
			"settlement" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategorySettlementDescription", "Choose a settlement, then choose which value to retrieve."),
			"clan" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryClanDescription", "Choose a clan, then choose which value to retrieve."),
			"hero" => KnowledgeUiTexts.Ui("AF_KNOWLEDGE_MapCategoryHeroDescription", "Choose a hero, then choose which value to retrieve."),
			_ => KnowledgeUiTexts.MappingChooseType
		};
	}

	private static List<string> GetTextMappingKindCategoryKeys()
	{
		return new List<string> { "status", "current_npc", "player", "bound", "kingdom", "settlement", "clan", "hero" };
	}

	private static void AddTextMappingKindOption(List<TextMappingKindOption> list, string categoryKey, string kind, string label)
	{
		if (list == null || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		list.Add(new TextMappingKindOption
		{
			CategoryKey = categoryKey,
			Kind = kind,
			Label = label
		});
	}

	private static void AddStatusMappingKindOption(List<TextMappingKindOption> list, string categoryKey, string sourceKey, string statusKey)
	{
		AddTextMappingKindOption(list, categoryKey, BuildStatusMappingKind(sourceKey, statusKey), KnowledgeMappingUiTexts.StatusLabel(sourceKey, statusKey));
	}

	private static void AddHeroStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey)
	{
		foreach (string statusKey in new[] { "is_alive", "is_dead", "is_disabled", "is_missing", "is_married", "is_widowed", "is_female", "is_male", "is_child", "is_adult", "is_in_age_range", "is_clan_leader", "is_kingdom_leader", "is_governor", "is_prisoner", "is_in_settlement", "is_in_field", "is_wanderer", "is_notable", "is_lord", "is_merchant", "is_gang_leader", "is_artisan", "is_preacher", "is_headman", "is_minor_faction_hero", "is_party_leader", "is_player_companion", "is_rebel", "is_wounded", "is_known_to_player", "has_children", "has_father", "has_mother", "has_home_settlement" })
			AddStatusMappingKindOption(list, categoryKey, sourceKey, statusKey);
	}

	private static void AddClanStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey)
	{
		foreach (string statusKey in new[] { "is_eliminated", "has_kingdom", "has_leader", "has_any_settlement", "has_any_town", "has_any_castle", "has_any_village", "has_members", "has_male_members", "has_female_members", "has_age_range_members", "is_mercenary", "is_minor_faction", "is_rebel_clan", "is_noble", "is_bandit_faction", "is_outlaw" })
			AddStatusMappingKindOption(list, categoryKey, sourceKey, statusKey);
	}

	private static void AddKingdomStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey)
	{
		foreach (string statusKey in new[] { "is_eliminated", "has_leader", "has_ruling_clan", "has_any_settlement", "has_any_town", "has_any_castle", "has_any_village", "has_any_clan", "has_any_lord", "has_active_policies", "has_any_war", "has_any_allies" })
			AddStatusMappingKindOption(list, categoryKey, sourceKey, statusKey);
	}

	private static void AddSettlementStatusKindOptions(List<TextMappingKindOption> list, string categoryKey, string sourceKey)
	{
		foreach (string statusKey in new[] { "is_active", "is_town", "is_castle", "is_village", "is_fortification", "is_hideout", "is_under_siege", "is_under_raid", "is_raided", "is_starving", "is_rebellious", "has_port", "has_owner", "has_owner_clan", "has_notables", "has_parties" })
			AddStatusMappingKindOption(list, categoryKey, sourceKey, statusKey);
	}

	private static List<TextMappingKindOption> BuildTextMappingKindOptions(string categoryKey)
	{
		List<TextMappingKindOption> list = new List<TextMappingKindOption>();
		string text = (categoryKey ?? "").Trim();
		switch (text)
		{
		case "status":
			AddHeroStatusKindOptions(list, text, "current_npc_hero");
			AddHeroStatusKindOptions(list, text, "player_hero");
			AddHeroStatusKindOptions(list, text, "bound_hero");
			AddHeroStatusKindOptions(list, text, "hero");
			AddClanStatusKindOptions(list, text, "current_npc_clan");
			AddClanStatusKindOptions(list, text, "player_clan");
			AddClanStatusKindOptions(list, text, "bound_settlement_owner_clan");
			AddClanStatusKindOptions(list, text, "bound_hero_clan");
			AddClanStatusKindOptions(list, text, "clan");
			AddKingdomStatusKindOptions(list, text, "current_npc_kingdom");
			AddKingdomStatusKindOptions(list, text, "player_kingdom");
			AddKingdomStatusKindOptions(list, text, "bound_kingdom");
			AddKingdomStatusKindOptions(list, text, "kingdom");
			AddSettlementStatusKindOptions(list, text, "current_npc_settlement");
			AddSettlementStatusKindOptions(list, text, "player_settlement");
			AddSettlementStatusKindOptions(list, text, "bound_settlement");
			AddSettlementStatusKindOptions(list, text, "settlement");
			break;
		case "current_npc":
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanMembers));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanMaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanMaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanFemaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanFemaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanAgeRangeMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanAgeRangeMembers));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcClanAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcClanAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomRulingClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomRulingClanName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomCultureName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomCultureName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomInitialHomeSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomInitialHomeSettlementName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAllClans, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAllClans));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAllLords, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAllLords));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAllCastles, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAllCastles));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomActivePolicies, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomActivePolicies));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomAlliedKingdoms, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomAlliedKingdoms));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcKingdomWarFactions, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcKingdomWarFactions));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcSpouseName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcSpouseName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcFatherName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcFatherName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcMotherName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcMotherName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementOwnerClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementOwnerClanName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementOwnerLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementOwnerLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementOwnerKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementCultureName, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementCultureName));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementNotables, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementNotables));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementParties, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementParties));
			AddTextMappingKindOption(list, text, TextMappingKindCurrentNpcCurrentSettlementBoundVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindCurrentNpcCurrentSettlementBoundVillages));
			break;
		case "player":
			AddTextMappingKindOption(list, text, TextMappingKindPlayerName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanMembers));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanMaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanMaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanFemaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanFemaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanAgeRangeMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanAgeRangeMembers));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerClanAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerClanAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerSpouseName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerSpouseName));
			AddTextMappingKindOption(list, text, TextMappingKindPlayerCurrentSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindPlayerCurrentSettlementName));
			break;
		case "bound":
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomRulingClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomRulingClanName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomCultureName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomCultureName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomInitialHomeSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomInitialHomeSettlementName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAllClans, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAllClans));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAllLords, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAllLords));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAllCastles, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAllCastles));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomActivePolicies, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomActivePolicies));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomAlliedKingdoms, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomAlliedKingdoms));
			AddTextMappingKindOption(list, text, TextMappingKindBoundKingdomWarFactions, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundKingdomWarFactions));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanMaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanMaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanFemaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanFemaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanAgeRangeMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanAgeRangeMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerClanAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerClanAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementOwnerKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementOwnerKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementCultureName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementCultureName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementNotables, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementNotables));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementParties, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementParties));
			AddTextMappingKindOption(list, text, TextMappingKindBoundSettlementBoundVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundSettlementBoundVillages));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanMaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanMaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanFemaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanFemaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanAgeRangeMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanAgeRangeMembers));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroClanAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroClanAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroSpouseName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroSpouseName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroFatherName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroFatherName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroMotherName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroMotherName));
			AddTextMappingKindOption(list, text, TextMappingKindBoundHeroCurrentSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindBoundHeroCurrentSettlementName));
			break;
		case "kingdom":
			AddTextMappingKindOption(list, text, TextMappingKindKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomRulingClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomRulingClanName));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomCultureName, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomCultureName));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomInitialHomeSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomInitialHomeSettlementName));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAllClans, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAllClans));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAllLords, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAllLords));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAllCastles, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAllCastles));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAllSettlements));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomActivePolicies, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomActivePolicies));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomAlliedKingdoms, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomAlliedKingdoms));
			AddTextMappingKindOption(list, text, TextMappingKindKingdomWarFactions, KnowledgeMappingUiTexts.Kind(TextMappingKindKingdomWarFactions));
			break;
		case "settlement":
			AddTextMappingKindOption(list, text, TextMappingKindSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementName));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementOwnerLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementOwnerLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementOwnerClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementOwnerClanName));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementOwnerKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementOwnerKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementOwnerKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementOwnerKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementCultureName, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementCultureName));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementNotables, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementNotables));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementParties, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementParties));
			AddTextMappingKindOption(list, text, TextMappingKindSettlementBoundVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindSettlementBoundVillages));
			break;
		case "clan":
			AddTextMappingKindOption(list, text, TextMappingKindClanLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindClanLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindClanName));
			AddTextMappingKindOption(list, text, TextMappingKindClanKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindClanKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindClanKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindClanKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindClanMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindClanMembers));
			AddTextMappingKindOption(list, text, TextMappingKindClanMaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindClanMaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindClanFemaleMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindClanFemaleMembers));
			AddTextMappingKindOption(list, text, TextMappingKindClanAgeRangeMembers, KnowledgeMappingUiTexts.Kind(TextMappingKindClanAgeRangeMembers));
			AddTextMappingKindOption(list, text, TextMappingKindClanAllTowns, KnowledgeMappingUiTexts.Kind(TextMappingKindClanAllTowns));
			AddTextMappingKindOption(list, text, TextMappingKindClanAllVillages, KnowledgeMappingUiTexts.Kind(TextMappingKindClanAllVillages));
			AddTextMappingKindOption(list, text, TextMappingKindClanAllSettlements, KnowledgeMappingUiTexts.Kind(TextMappingKindClanAllSettlements));
			break;
		case "hero":
			AddTextMappingKindOption(list, text, TextMappingKindHeroName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroClanName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroClanName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroKingdomName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroKingdomName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroKingdomLeaderName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroKingdomLeaderName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroSpouseName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroSpouseName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroFatherName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroFatherName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroMotherName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroMotherName));
			AddTextMappingKindOption(list, text, TextMappingKindHeroCurrentSettlementName, KnowledgeMappingUiTexts.Kind(TextMappingKindHeroCurrentSettlementName));
			break;
		}
		return list;
	}

	private static List<TextMappingKindOption> BuildAllTextMappingKindOptions()
	{
		List<TextMappingKindOption> list = new List<TextMappingKindOption>();
		foreach (string text in GetTextMappingKindCategoryKeys())
		{
			list.AddRange(BuildTextMappingKindOptions(text));
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
		string text2 = (GetTextMappingKindCategoryLabel(option.CategoryKey) + " " + (option.Label ?? "") + " " + GetTextMappingKindLabel(option.Kind) + " " + (option.Kind ?? "")).ToLowerInvariant();
		return array.All((string part) => text2.Contains(part));
	}

	private void OpenTextMappingKindSearchResults(string query, Action<string> onPickKind, Action onCancel, int page)
	{
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
		List<TextMappingKindOption> list = BuildAllTextMappingKindOptions().Where((TextMappingKindOption x) => TextMappingKindOptionMatchesQuery(x, text)).OrderBy((TextMappingKindOption x) => GetTextMappingKindCategoryLabel(x.CategoryKey), StringComparer.OrdinalIgnoreCase).ThenBy((TextMappingKindOption x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((TextMappingKindOption x) => x.Kind ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		const int pageSize = 18;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		List<TextMappingKindOption> list2 = list.Skip(page * pageSize).Take(pageSize).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.Search, null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.Back, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list3.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (TextMappingKindOption item in list2)
		{
			string text2 = "【" + GetTextMappingKindCategoryLabel(item.CategoryKey) + "】" + (item.Label ?? GetTextMappingKindLabel(item.Kind));
			list3.Add(new InquiryElement(item.Kind, text2, null));
		}
		string text3 = KnowledgeUiTexts.MappingSearchResults(TrimPreview(text, 60), list.Count, list.Count == 0);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.MappingSearchTitle, text3, list3, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingKindPicker(onPickKind, onCancel);
				return;
			}
			string text4 = ((selected[0].Identifier as string) ?? "").Trim();
			switch (text4)
			{
			case "__search__":
				InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.MappingSearchTitle, KnowledgeUiTexts.MappingSearchPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Back, delegate(string input)
				{
					OpenTextMappingKindSearchResults(input, onPickKind, onCancel, 0);
				}, delegate
				{
					OpenTextMappingKindSearchResults(text, onPickKind, onCancel, page);
				}, shouldInputBeObfuscated: false, null, text));
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
		}, delegate
		{
			OpenTextMappingKindPicker(onPickKind, onCancel);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenTextMappingKindPicker(Action<string> onPickKind, Action onCancel)
	{
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
		list.Add(new InquiryElement("__search__", KnowledgeUiTexts.MappingSearchTitle, null));
		list.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (string text in GetTextMappingKindCategoryKeys())
		{
			list.Add(new InquiryElement(text, GetTextMappingKindCategoryLabel(text), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.MappingSearchMenuTitle, KnowledgeUiTexts.MappingSearchMenuDescription, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onCancel();
				return;
			}
			string text2 = ((selected[0].Identifier as string) ?? "").Trim();
			switch (text2)
			{
			case "__search__":
				InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.MappingSearchTitle, KnowledgeUiTexts.MappingSearchPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Back, delegate(string input)
				{
					OpenTextMappingKindSearchResults(input, onPickKind, onCancel, 0);
				}, delegate
				{
					OpenTextMappingKindPicker(onPickKind, onCancel);
				}));
				break;
			case "__sep__":
				OpenTextMappingKindPicker(onPickKind, onCancel);
				break;
			default:
				if (string.IsNullOrWhiteSpace(text2))
				{
					onCancel();
				}
				else
				{
					OpenTextMappingKindPickerByCategory(text2, onPickKind, onCancel);
				}
				break;
			}
		}, delegate
		{
			onCancel();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenTextMappingKindPickerByCategory(string categoryKey, Action<string> onPickKind, Action onCancel)
	{
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
		string text = (categoryKey ?? "").Trim();
		List<TextMappingKindOption> list = BuildTextMappingKindOptions(text);
		if (list.Count == 0)
		{
			OpenTextMappingKindPicker(onPickKind, onCancel);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (TextMappingKindOption item in list)
		{
			list2.Add(new InquiryElement(item.Kind, item.Label, null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.MappingChooseTypeTitle, GetTextMappingKindCategoryDescription(text), list2, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenTextMappingKindPicker(onPickKind, onCancel);
			}
			else
			{
				string text2 = ((selected[0].Identifier as string) ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					OpenTextMappingKindPicker(onPickKind, onCancel);
				}
				else
				{
					onPickKind(text2);
				}
			}
		}, delegate
		{
			OpenTextMappingKindPicker(onPickKind, onCancel);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenKeywordMenu(LoreRule rule, Action onReturn)
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
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__add__", KnowledgeUiTexts.AddKeyword, null));
		if (rule.Keywords.Count > 0)
		{
			list.Add(new InquiryElement("__remove__", KnowledgeUiTexts.RemoveKeyword, null));
		}
		foreach (string keyword in rule.Keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				list.Add(new InquiryElement("__k__" + keyword, KnowledgeUiTexts.KeywordLabel(keyword), null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.KeywordMenuTitle, KnowledgeUiTexts.KeywordMenuDescription, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenKeywordMenu(rule, onReturn);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__add__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.AddKeywordTitle, KnowledgeUiTexts.AddKeywordPrompt, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						string text2 = (input ?? "").Trim();
						string kwNorm = LoreCandidateRetriever.NormalizeKeywordForCompare(text2);
						if (string.IsNullOrEmpty(kwNorm))
						{
							OpenKeywordMenu(rule, onReturn);
						}
						else
						{
							bool flag = false;
							try
							{
								flag = rule.Keywords.Any((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), kwNorm, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag = false;
							}
							string foundRuleId;
							if (flag)
							{
								InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.KeywordInRuleAlreadyExists));
								OpenKeywordMenu(rule, onReturn);
							}
							else if (TryFindRuleIdByKeyword(text2, rule.Id, out foundRuleId))
							{
								InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.CreateRuleKeywordConflict(text2, foundRuleId)));
								OpenKeywordMenu(rule, onReturn);
							}
							else
							{
								rule.Keywords.Add(text2);
								TouchRuleData();
								OpenKeywordMenu(rule, onReturn);
							}
						}
					}, delegate
					{
						OpenKeywordMenu(rule, onReturn);
					}));
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenKeywordRemoveMenu(LoreRule rule, Action onReturn)
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
		if (rule.Keywords == null)
		{
			rule.Keywords = new List<string>();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string keyword in rule.Keywords)
		{
			if (!string.IsNullOrWhiteSpace(keyword))
			{
				list.Add(new InquiryElement(keyword, keyword, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.RemoveKeywordTitle, KnowledgeUiTexts.RemoveKeywordPrompt, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Delete, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			OpenKeywordMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRagShortTextMenu(LoreRule rule, Action onReturn)
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
		EnsureRagShortTexts(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__add__", KnowledgeUiTexts.AddRag, null));
		list.Add(new InquiryElement("__generate__", KnowledgeUiTexts.GenerateRag, null));
		if (rule.RagShortTexts.Count > 0)
		{
			list.Add(new InquiryElement("__remove__", KnowledgeUiTexts.RemoveRag, null));
		}
		foreach (string ragShortText in rule.RagShortTexts)
		{
			string text = LoreCandidateRetriever.NormalizeKeywordForCompare(ragShortText);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new InquiryElement("__r__" + text, KnowledgeUiTexts.RagLabel(text), null));
			}
		}
		string descriptionText = KnowledgeUiTexts.RagMenuDescription(RagShortTextMaxLength, RagShortTextGeneratedMaxLength, RagShortTextGenerationMaxTokens);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.RagMenuTitle, descriptionText, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRagShortTextMenu(rule, onReturn);
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "__add__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.AddRagTitle, KnowledgeUiTexts.AddRagPrompt(RagShortTextMaxLength), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						string text3 = LoreCandidateRetriever.NormalizeKeywordForCompare(input);
						if (string.IsNullOrEmpty(text3))
						{
							OpenRagShortTextMenu(rule, onReturn);
						}
						else if (text3.Length > RagShortTextMaxLength)
						{
							InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.RagTooLong(RagShortTextMaxLength)));
							OpenRagShortTextMenu(rule, onReturn);
						}
						else
						{
							bool flag = false;
							try
							{
								flag = rule.RagShortTexts.Any((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), text3, StringComparison.OrdinalIgnoreCase));
							}
							catch
							{
								flag = false;
							}
							if (flag)
							{
								InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.RagAlreadyExists));
							}
							else
							{
								rule.RagShortTexts.Add(text3);
								TouchRuleData();
							}
							OpenRagShortTextMenu(rule, onReturn);
						}
					}, delegate
					{
						OpenRagShortTextMenu(rule, onReturn);
					}));
				}
				else if (text2 == "__generate__")
				{
					GenerateRagShortTextsByLlm(rule, delegate
					{
						OpenRagShortTextMenu(rule, onReturn);
					});
				}
				else if (text2 == "__remove__")
				{
					OpenRagShortTextRemoveMenu(rule, onReturn);
				}
				else
				{
					OpenRagShortTextMenu(rule, onReturn);
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRagShortTextRemoveMenu(LoreRule rule, Action onReturn)
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
		EnsureRagShortTexts(rule);
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (string ragShortText in rule.RagShortTexts)
		{
			string text = LoreCandidateRetriever.NormalizeKeywordForCompare(ragShortText);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.RemoveRagTitle, KnowledgeUiTexts.RemoveRagPrompt, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Delete, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRagShortTextMenu(rule, onReturn);
			}
			else
			{
				string text2 = LoreCandidateRetriever.NormalizeKeywordForCompare(selected[0].Identifier as string);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					rule.RagShortTexts.RemoveAll((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), text2, StringComparison.OrdinalIgnoreCase));
					TouchRuleData();
				}
				OpenRagShortTextMenu(rule, onReturn);
			}
		}, delegate
		{
			OpenRagShortTextMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildRagShortTextGenerationSystemPrompt()
	{
		return "你是一个本地RAG知识库的“检索短句生成器”。\n" + "你的输出会直接用于知识召回与重排，不是给NPC直接说的话。\n" + "请严格遵守：\n" + "1) 只围绕提供的知识材料生成，不得编造；\n" + "2) 每条都要显式保留实体名、主题词或关键概念，便于语义检索命中；\n" + "3) 可以写成简洁描述句或简洁提问式短句，但必须适合RAG程序检索；\n" + "4) 禁止第一人称口吻、抒情、解释、提示词说明、系统术语、ACTION标签；\n" + "5) 禁止直接照抄长提示词正文，只保留可检索的核心事实；\n" + "6) 每条最多 " + RagShortTextGeneratedMaxLength + " 个字符；\n" + "7) 只输出 JSON 数组字符串，例如 [\"...\",\"...\"]；禁止 Markdown 代码块、标题、序号和额外说明。";
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
				targetCount = RagShortTextAutoGenerateCount;
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
						string text2 = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword);
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
						string text3 = LoreCandidateRetriever.NormalizeKeywordForCompare(item);
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
						if (string.IsNullOrEmpty(text4))
						{
							continue;
						}
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
			stringBuilder.AppendLine("1) 每条 8~" + RagShortTextGeneratedMaxLength + " 字，中文自然，不要太虚；");
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
			maxCount = RagShortTextAutoGenerateCount;
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
			JArray jArray = JArray.Parse(text);
			foreach (JToken item in jArray)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(item?.ToString() ?? "");
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
				JObject jObject = JObject.Parse(text);
				JArray jArray2 = null;
				if (jObject["rag_short_texts"] is JArray jArray3)
				{
					jArray2 = jArray3;
				}
				else if (jObject["items"] is JArray jArray4)
				{
					jArray2 = jArray4;
				}
				else if (jObject["data"] is JArray jArray5)
				{
					jArray2 = jArray5;
				}
				if (jArray2 != null)
				{
					foreach (JToken item2 in jArray2)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(item2?.ToString() ?? "");
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
			foreach (string raw2 in array)
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
			int num;
			for (num = 0; num < text.Length && char.IsDigit(text[num]); num++)
			{
			}
			if (num > 0 && num < text.Length)
			{
				char c = text[num];
				if (c == '.' || c == '、' || c == ')' || c == '）' || c == ':' || c == '：')
				{
					text = text.Substring(num + 1).Trim();
				}
			}
			text = text.Trim().Trim('"').Trim('\'').Trim();
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
			text = LoreCandidateRetriever.NormalizeKeywordForCompare(text);
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			if (text.Length > RagShortTextGeneratedMaxLength)
			{
				text = text.Substring(0, RagShortTextGeneratedMaxLength).Trim();
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
		void add(string raw)
		{
			string text = NormalizeRagShortTextCandidate(raw);
			if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		List<string> list2 = new List<string>();
		try
		{
			if (rule?.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					string text = LoreCandidateRetriever.NormalizeKeywordForCompare(keyword);
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
	}

	private void GenerateRagShortTextsByLlm(LoreRule rule, Action onDone)
	{
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
					string text = LoreCandidateRetriever.NormalizeKeywordForCompare(ragShortText);
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
		string text2 = BuildRagShortTextGenerationUserPrompt(rule, RagShortTextAutoGenerateCount, list);
		if (string.IsNullOrWhiteSpace(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.RagGenerationNoSource));
			onDone();
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.RagGenerating));
		try
		{
			string systemPrompt = BuildRagShortTextGenerationSystemPrompt();
			string raw = RequestLlmTextOnce(systemPrompt, text2, RagShortTextGenerationMaxTokens, 0.2f);
			List<string> list2 = ParseRagShortTextCandidates(raw, Math.Max(RagShortTextAutoGenerateCount * 2, 6));
			if (list2.Count <= 0)
			{
				LlmRetryPrompt.ShowFailurePopup(KnowledgeUiTexts.RagParseFailureTitle, LlmRetryPrompt.BuildFailureDetail(KnowledgeUiTexts.RagParseFailureReason, raw));
				list2 = BuildDeterministicRagShortTextFallback(rule, RagShortTextAutoGenerateCount);
			}
			int num = 0;
			foreach (string item in list2)
			{
				string text3 = NormalizeRagShortTextCandidate(item);
				if (string.IsNullOrWhiteSpace(text3) || rule.RagShortTexts.Any((string x) => string.Equals(LoreCandidateRetriever.NormalizeKeywordForCompare(x), text3, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}
				rule.RagShortTexts.Add(text3);
				num++;
				if (num >= RagShortTextAutoGenerateCount)
				{
					break;
				}
			}
			if (num > 0)
			{
				TouchRuleData();
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.RagGenerated(num)));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.RagNoNew));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[Knowledge] 生成RAG专用短句失败: " + ex);
			LlmRetryPrompt.ShowFailurePopup(KnowledgeUiTexts.RagGenerationFailureTitle, ex.Message);
		}
		onDone();
	}

	private void OpenPrototypeMenu(LoreRule rule, Action onReturn)
	{
		InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.PrototypeDisabled));
		onReturn?.Invoke();
	}

	private void OpenPrototypeViewMenu(LoreRule rule, Action onReturn, int page, string query)
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
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeSearch", "Search prototype sentences"), null));
		if (!string.IsNullOrWhiteSpace(q))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list3.Add(new InquiryElement("__back__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeBack", "Back to prototype menu"), null));
		if (list2.Count > 0)
		{
			list3.Add(new InquiryElement("__sep__", "----------------", null));
		}
		for (int num3 = 0; num3 < list2.Count; num3++)
		{
			int num4 = num2 + num3;
			string s = list2[num3];
			string title = $"{num4 + 1}. {TrimPreview(s, 90)}";
			list3.Add(new InquiryElement("__item__" + num4, title, null));
		}
		string text2 = KnowledgeUiTexts.PrototypeCountSummary(count, TrimPreview(q, 40), page + 1, num, list2.Count <= 0);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeViewTitle", "View prototype sentences"), text2, list3, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenPrototypeViewMenu(rule, onReturn, page, q);
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				switch (text3)
				{
				case "__back__":
					OpenPrototypeMenu(rule, onReturn);
					return;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeSearchTitle", "Search prototype sentences"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeSearchPrompt", "Enter keywords:"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Back, delegate(string input)
					{
						OpenPrototypeViewMenu(rule, onReturn, 0, (input ?? "").Trim());
					}, delegate
					{
						OpenPrototypeViewMenu(rule, onReturn, page, q);
					}, shouldInputBeObfuscated: false, null, q));
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
					if (text3.StartsWith("__item__", StringComparison.Ordinal) && int.TryParse(text3.Substring("__item__".Length), out var result) && result >= 0 && result < filtered.Count)
					{
						string text4 = filtered[result] ?? "";
						InformationManager.ShowInquiry(new InquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeDetailTitle", "Prototype sentence"), text4, isAffirmativeOptionShown: true, isNegativeOptionShown: false, KnowledgeUiTexts.Back, "", delegate
						{
							OpenPrototypeViewMenu(rule, onReturn, page, q);
						}, null));
						return;
					}
					break;
				}
				case null:
					break;
				}
				OpenPrototypeViewMenu(rule, onReturn, page, q);
			}
		}, delegate
		{
			OpenPrototypeMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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
			List<string> list4 = new List<string>();
			try
			{
				if (rule.RagShortTexts != null)
				{
					foreach (string ragShortText in rule.RagShortTexts)
					{
						string text3 = (ragShortText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
						if (!string.IsNullOrEmpty(text3))
						{
							if (list4.Count >= 12)
							{
								break;
							}
							list4.Add(text3);
						}
					}
				}
			}
			catch
			{
			}
			stringBuilder.AppendLine("RAG专用短句: " + ((list4.Count > 0) ? string.Join(" | ", list4) : "（无）"));
			List<string> list2 = new List<string>();
			try
			{
				IEnumerable<string> enumerable = existingForPrompt ?? rule.SemanticPrototypes;
				if (enumerable != null)
				{
					foreach (string item in enumerable)
					{
						string text3 = (item ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
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
			stringBuilder.AppendLine("已有原型句: " + ((list2.Count > 0) ? string.Join(" | ", list2) : "（无）"));
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
			JArray jArray = JArray.Parse(text);
			foreach (JToken item in jArray)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(item?.ToString() ?? "");
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
				JObject jObject = JObject.Parse(text);
				JArray jArray2 = null;
				if (jObject["prototypes"] is JArray jArray3)
				{
					jArray2 = jArray3;
				}
				else if (jObject["items"] is JArray jArray4)
				{
					jArray2 = jArray4;
				}
				else if (jObject["data"] is JArray jArray5)
				{
					jArray2 = jArray5;
				}
				if (jArray2 != null)
				{
					foreach (JToken item2 in jArray2)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(item2?.ToString() ?? "");
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
					string json = text.Substring(num3, num4 - num3 + 1);
					JArray jArray6 = JArray.Parse(json);
					foreach (JToken item3 in jArray6)
					{
						if (result.Count >= maxCount)
						{
							break;
						}
						addCandidate(item3?.ToString() ?? "");
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
			foreach (string s in array2)
			{
				if (result.Count >= maxCount)
				{
					break;
				}
				addCandidate(s);
			}
		}
		return result;
		void addCandidate(string raw2)
		{
			string t = NormalizePrototypeText(raw2);
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
			foreach (string format in array2)
			{
				add(string.Format(format, item));
				if (list.Count >= maxCount)
				{
					return list;
				}
			}
		}
		string[] array3 = array;
		foreach (string format2 in array3)
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
		int actualMaxTokens = Math.Max(1, Math.Min(Math.Min(RagShortTextGenerationMaxTokens, maxTokens), settings.GetMainApiMaxTokens()));
		PromptPackage prompt = new PromptPackage(
			new[]
			{
				new PromptMessage("system", systemPrompt ?? string.Empty),
				new PromptMessage("user", userPrompt ?? string.Empty)
			},
			actualMaxTokens,
			string.IsNullOrWhiteSpace(settings.ModelName) ? "knowledge-rag" : settings.ModelName);
		string apiLine;
#if BANNERLORD_1_4_OR_GREATER
		apiLine = "1.4";
#else
		apiLine = "1.3";
#endif
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		TraceContext trace = new TraceContext(
			"af-knowledge-rag-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
			runtimeGeneration,
			runtimeGeneration,
			"knowledge",
			apiLine);
		LlmProviderSnapshot provider = new LlmProviderSnapshot(
			"knowledge-rag",
			effectiveApiUrl,
			string.IsNullOrWhiteSpace(settings.ModelName) ? "knowledge-rag" : settings.ModelName,
			45000,
			actualMaxTokens);
		LegacyKnowledgeRagGateway gateway = new LegacyKnowledgeRagGateway(
			_ => settings.ApiKey,
			temperature: temperature
			);
		LlmGenerateResult generated = gateway.GenerateAsync(
			new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
			CancellationToken.None).GetAwaiter().GetResult();
		if (generated == null || generated.Status != LlmResultStatus.Succeeded || string.IsNullOrWhiteSpace(generated.RawText))
		{
			throw new Exception("RAG API 请求失败：" + (generated?.ErrorCode ?? "empty_result"));
		}
		return generated.RawText.Trim();
	}

	private void GenerateSemanticPrototypesByLlm(LoreRule rule, Action onDone)
	{
		InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.PrototypeDisabled));
		onDone?.Invoke();
	}

	private void OpenPrototypeRemoveMenu(LoreRule rule, Action onReturn)
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
				list.Add(new InquiryElement(text, TrimPreview(text, 80), null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeRemoveTitle", "Delete prototype sentence"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PrototypeRemovePrompt", "Select a prototype sentence to delete:"), list, isExitShown: true, 0, 1, KnowledgeUiTexts.Delete, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			OpenPrototypeMenu(rule, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenVariantMenu(LoreRule rule, Action onReturn)
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
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__add__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantAdd", "Add prompt"), null));
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
				string title = $"#{num + 1} {arg}  {text}";
				list.Add(new InquiryElement(num, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantMenuTitle", "Edit prompts"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantMenuDescription", "Leave the condition empty for a general prompt; any condition makes it specific. The most specific matching prompt is selected."), list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenVariantMenu(rule, onReturn);
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "__add__")
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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
		string text5 = KnowledgeUiTexts.VariantEditorDescription(text, text4, flag);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("content", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantEditContent", "Edit prompt content"), null));
		list.Add(new InquiryElement("when", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantEditCondition", "Edit conditions"), null));
		list.Add(new InquiryElement("delete", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantDelete", "Delete this prompt"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantEditorTitle", "Prompt editor"), text5, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
					DevTextEditorHelper.ShowLongTextEditor(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantEditContent", "Edit prompt content"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantCurrentCondition", "Current conditions: {CONDITION}", ("CONDITION", text)), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_VariantContentPrompt", "Enter content."), v.Content ?? "", delegate(string input)
					{
						int duplicateVariantIndex = FindDuplicateVariantIndex(rule, v.When, idx);
						if (duplicateVariantIndex >= 0)
						{
							ShowDuplicateVariantConditionPrompt(v.When, duplicateVariantIndex);
							OpenVariantEditor(rule, idx, onReturn, isNewVariant);
							return;
						}
						v.Content = input ?? "";
						TouchRuleData();
						OpenVariantEditor(rule, idx, onReturn, isNewVariant);
					}, delegate
					{
						OpenVariantEditor(rule, idx, onReturn, isNewVariant);
					}, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel);
					break;
				case "when":
				{
					LoreWhen loreWhen = CloneWhen(v.When) ?? new LoreWhen();
					OpenWhenEditor(loreWhen, delegate
					{
						loreWhen = NormalizeWhenForStorage(loreWhen);
						int duplicateVariantIndex2 = FindDuplicateVariantIndex(rule, loreWhen, idx);
						if (duplicateVariantIndex2 >= 0)
						{
							ShowDuplicateVariantConditionPrompt(loreWhen, duplicateVariantIndex2);
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
		}, delegate
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
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenWhenEditor(LoreWhen when, Action onReturn)
	{
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
		list.Add(new InquiryElement("culture", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenCulture", "Culture (CultureId)"), null));
		list.Add(new InquiryElement("kingdom", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenKingdom", "Faction/kingdom (KingdomId)"), null));
		list.Add(new InquiryElement("settlement", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlement", "Settlement (SettlementId)"), null));
		list.Add(new InquiryElement("role", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRole", "Identity (category + specific NPC)"), null));
		list.Add(new InquiryElement("gender", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenGender", "Gender (any/male/female)"), null));
		list.Add(new InquiryElement("clan_leader", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenClanLeader", "Clan leader (any/yes/no)"), null));
		list.Add(new InquiryElement("skill", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSkill", "Skill level (SkillId >= value)"), null));
		list.Add(new InquiryElement("clear", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenClear", "Clear all conditions (make general)"), null));
		string descriptionText = BuildWhenDetail(when);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenEditorTitle", "Edit conditions"), descriptionText, list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
					OpenStringListEditor(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenCulture", "Culture (CultureId)"), when.Cultures, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "kingdom":
					if (when.KingdomIds == null)
					{
						when.KingdomIds = new List<string>();
					}
					OpenStringListEditor(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenKingdom", "Faction/kingdom (KingdomId)"), when.KingdomIds, delegate
					{
						OpenWhenEditor(when, onReturn);
					});
					break;
				case "settlement":
					if (when.SettlementIds == null)
					{
						when.SettlementIds = new List<string>();
					}
					OpenStringListEditor(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlement", "Settlement (SettlementId)"), when.SettlementIds, delegate
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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
				Hero hero = Hero.FindFirst((Hero x) => x != null && string.Equals(x.StringId, targetId, StringComparison.OrdinalIgnoreCase));
				string text = hero?.Name?.ToString();
				if (string.IsNullOrWhiteSpace(text))
				{
					text = targetId;
				}
				return text + "（HeroId=" + targetId + "）";
			}
			if (kind == "char")
			{
				CharacterObject characterObject = null;
				try
				{
					MBReadOnlyList<CharacterObject> objectTypeList = Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>();
					if (objectTypeList != null)
					{
						characterObject = objectTypeList.FirstOrDefault((CharacterObject x) => x != null && string.Equals(x.StringId, targetId, StringComparison.OrdinalIgnoreCase));
					}
				}
				catch
				{
				}
				string text2 = characterObject?.Name?.ToString();
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
		Hero hero = null;
		try
		{
			hero = Hero.FindFirst((Hero x) => x != null && string.Equals(x.StringId, text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
		}
		string text2 = hero?.Name?.ToString();
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
			Hero hero = Hero.FindFirst((Hero x) => x != null && string.Equals(x.StringId, text, StringComparison.OrdinalIgnoreCase));
			if (hero != null)
			{
				return GetRoleKeyForHero(hero);
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
			return;
		}
		ToggleListValueIgnoreCase(when.IdentityIds, text);
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
				Hero hero = Hero.FindFirst((Hero x) => x != null && string.Equals(x.StringId, targetId, StringComparison.OrdinalIgnoreCase));
				return GetRoleKeyForHero(hero);
			}
			if (kind == "char")
			{
				MBReadOnlyList<CharacterObject> objectTypeList = Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>();
				CharacterObject characterObject = objectTypeList?.FirstOrDefault((CharacterObject x) => x != null && string.Equals(x.StringId, targetId, StringComparison.OrdinalIgnoreCase));
				return GetRoleKeyForCharacter(characterObject);
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
		string text = (query ?? "").Trim();
		bool flag = !string.IsNullOrWhiteSpace(text);
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
						list2 = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(h.StringId)).ToList();
					}
				}
				catch
				{
				}
				if (list2.Count == 0)
				{
					foreach (Hero allAliveHero in Hero.AllAliveHeroes)
					{
						if (allAliveHero != null && !string.IsNullOrWhiteSpace(allAliveHero.StringId))
						{
							list2.Add(allAliveHero);
						}
					}
				}
				foreach (Hero item in list2)
				{
					if (!string.Equals(GetRoleKeyForHero(item), roleKey, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					string text2 = item.Name?.ToString();
					if (string.IsNullOrWhiteSpace(text2))
					{
						text2 = item.StringId;
					}
					string text3 = text2 + "（HeroId=" + item.StringId + "）";
					if (!flag || text3.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || item.StringId.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						string text4 = EncodeRoleIdentityHeroId(item.StringId);
						if (!string.IsNullOrEmpty(text4))
						{
							list.Add(new RoleDetailOption
							{
								EncodedId = text4,
								Label = text3
							});
						}
					}
				}
			}
			else
			{
				MBReadOnlyList<CharacterObject> objectTypeList = Game.Current?.ObjectManager?.GetObjectTypeList<CharacterObject>();
				if (objectTypeList != null)
				{
					foreach (CharacterObject item2 in objectTypeList)
					{
						if (item2 == null || string.IsNullOrWhiteSpace(item2.StringId) || !string.Equals(GetRoleKeyForCharacter(item2), roleKey, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string text5 = item2.Name?.ToString();
						if (string.IsNullOrWhiteSpace(text5))
						{
							text5 = item2.StringId;
						}
						string text6 = text5 + "（CharacterId=" + item2.StringId + "）";
						if (!flag || text6.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || item2.StringId.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							string text7 = EncodeRoleIdentityCharacterId(item2.StringId);
							if (!string.IsNullOrEmpty(text7))
							{
								list.Add(new RoleDetailOption
								{
									EncodedId = text7,
									Label = text6
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
		roleKey = ((roleKey ?? "").Trim().ToLowerInvariant());
		List<RoleDetailOption> list = BuildRoleDetailOptions(roleKey, query);
		if (when.HeroIds != null && when.HeroIds.Count > 0)
		{
			string text = (query ?? "").Trim();
			bool flag = !string.IsNullOrWhiteSpace(text);
			HashSet<string> hashSet = new HashSet<string>(list.Select((RoleDetailOption x) => (x.EncodedId ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
			foreach (string heroId in when.HeroIds)
			{
				if (!string.Equals(GetRoleKeyForLegacyHeroId(heroId), roleKey, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string text2 = EncodeRoleIdentityHeroId(heroId);
				if (string.IsNullOrWhiteSpace(text2) || hashSet.Contains(text2))
				{
					continue;
				}
				string text3 = BuildLegacyHeroIdentityDisplayText(heroId);
				if (flag && text3.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0 && ((heroId ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0))
				{
					continue;
				}
				list.Add(new RoleDetailOption
				{
					EncodedId = text2,
					Label = text3
				});
				hashSet.Add(text2);
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
		list3.Add(new InquiryElement("__toggle_all__", (flag2 ? "[√] " : "[ ] ") + KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleToggleAll", "Whole category: {ROLE}", ("ROLE", GetRoleDisplayName(roleKey))), null));
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.RoleSearchOption(query), null));
		if (!string.IsNullOrWhiteSpace(query))
		{
			list3.Add(new InquiryElement("__clear_search__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleClearSearch", "Clear search"), null));
		}
		if (num3 > 0)
		{
			list3.Add(new InquiryElement("__clear_specific__", KnowledgeUiTexts.RoleClearSpecificOption(num3), null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page < num2 - 1)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		foreach (RoleDetailOption item3 in list2)
		{
			bool flag3 = IsRoleIdentitySelected(when, item3.EncodedId);
			list3.Add(new InquiryElement(item3.EncodedId, (flag3 ? "[√] " : "[ ] ") + item3.Label, null));
		}
		string descriptionText = KnowledgeUiTexts.RoleDescription(GetRoleDisplayName(roleKey), flag2, num3, list.Count, page + 1, num2);
		int num4 = Math.Max(1, list3.Count);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleDetailTitle", "Specific identity - {ROLE}", ("ROLE", GetRoleDisplayName(roleKey))), descriptionText, list3, isExitShown: true, 0, num4, KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleSwitchSelection", "Switch selection"), KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
				return;
			}
			List<string> list4 = selected.Select((InquiryElement x) => x?.Identifier as string).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
			if (list4.Count == 0)
			{
				OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
				return;
			}
			List<string> list5 = list4.Where((string x) => x.StartsWith("__", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).ToList();
			if (list5.Count > 0)
			{
				if (list4.Count > 1)
				{
					InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleMultiSelectError", "Search, paging, and whole-category operations cannot be selected together with specific NPCs.")));
					OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
					return;
				}
				string text = list5[0];
				switch (text)
				{
				case "__toggle_all__":
					ToggleListValueIgnoreCase(when.Roles, roleKey);
					TouchRuleData();
					OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
					break;
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleSearchTitle", "Search {ROLE}", ("ROLE", GetRoleDisplayName(roleKey))), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleSearchPrompt", "Enter a name, HeroId or CharacterId. Search is limited to this category."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Back, delegate(string input)
					{
						OpenRoleCategoryEditor(when, roleKey, onReturn, 0, input);
					}, delegate
					{
						OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
					}, shouldInputBeObfuscated: false, null, query ?? ""));
					break;
				case "__clear_search__":
					OpenRoleCategoryEditor(when, roleKey, onReturn, 0, null);
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
				return;
			}
			foreach (string item4 in list4.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				ToggleRoleIdentitySelection(when, item4);
			}
			TouchRuleData();
			OpenRoleCategoryEditor(when, roleKey, onReturn, page, query);
		}, delegate
		{
			OpenRoleEditor(when, onReturn);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenRoleEditor(LoreWhen when, Action onReturn)
	{
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
		foreach (string roleCategoryKey in GetRoleCategoryKeys())
		{
			bool flag = ListContainsIgnoreCase(when.Roles, roleCategoryKey);
			int num = CountSelectedRoleIdentities(when, roleCategoryKey);
			string text = KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleCategoryItem", "{PREFIX}{ROLE}{SPECIFIC}", ("PREFIX", flag ? "[√] " : ""), ("ROLE", GetRoleDisplayName(roleCategoryKey)), ("SPECIFIC", num > 0 ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleSpecificCount", " (specific entries selected: {COUNT})", ("COUNT", num.ToString())) : ""));
			list.Add(new InquiryElement(roleCategoryKey, text, null));
		}
		if (when.IdentityIds.Count > 0)
		{
			list.Add(new InquiryElement("__clear_all_specific__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleClearAllSpecific", "Clear all specific identities"), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleEditorTitle", "Identity conditions"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleEditorDescription", "Select a category first, then choose specific NPCs in it. Whole-category selection remains supported; specific entries can be searched and paged within the category."), list, isExitShown: true, 0, 1, KnowledgeUiTexts.Enter, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenRoleEditor(when, onReturn);
				return;
			}
			string text = selected[0].Identifier as string;
			if (text == "__clear_all_specific__")
			{
				when.IdentityIds.Clear();
				TouchRuleData();
				OpenRoleEditor(when, onReturn);
				return;
			}
			OpenRoleCategoryEditor(when, text, onReturn);
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
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
				list2 = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(h.StringId)).ToList();
			}
		}
		catch
		{
		}
		if (list2.Count == 0)
		{
			try
			{
				foreach (Hero allAliveHero in Hero.AllAliveHeroes)
				{
					if (allAliveHero != null && !string.IsNullOrWhiteSpace(allAliveHero.StringId))
					{
						list2.Add(allAliveHero);
					}
				}
			}
			catch
			{
			}
		}
		list2 = (from h in list2
			orderby (h.Name != null) ? h.Name.ToString() : "", h.StringId
			select h).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoNpc", "No selectable NPCs found.")));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (Hero item in list2)
		{
			string text = ((item.Name != null) ? item.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			string text2 = "";
			try
			{
				text2 = (item.IsLord ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleLord", "Lord") : (item.IsNotable ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleNotable", "Notable") : ((!item.IsWanderer) ? item.Occupation.ToString() : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_RoleWanderer", "Wanderer"))));
			}
			catch
			{
				text2 = "";
			}
			list3.Add(new InquiryElement(item.StringId, text + " (" + text2 + ", " + item.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNpcTitle", "Select NPCs"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerMultiDescription", "Select multiple entries; confirm to add them to the list."), list3, isExitShown: true, 0, list3.Count, KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerAdd", "Add"), KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected != null)
			{
				foreach (InquiryElement item2 in selected)
				{
					string id = item2.Identifier as string;
					id = (id ?? "").Trim();
					if (!string.IsNullOrEmpty(id) && !list.Any((string x) => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
					{
						list.Add(id);
					}
				}
			}
			onReturn();
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenCultureIdPicker(List<string> list, Action onReturn)
	{
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
			MBReadOnlyList<CultureObject> mBReadOnlyList = Game.Current?.ObjectManager?.GetObjectTypeList<CultureObject>();
			if (mBReadOnlyList != null)
			{
				foreach (CultureObject item in mBReadOnlyList)
				{
					if (item != null && !string.IsNullOrWhiteSpace(item.StringId) && !item.IsBandit && !(item.StringId == "neutral_culture"))
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
			orderby (c.Name != null) ? c.Name.ToString() : "", c.StringId
			select c).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoCulture", "No selectable cultures found.")));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (CultureObject item2 in list2)
		{
			string text = ((item2.Name != null) ? item2.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item2.StringId;
			}
			list3.Add(new InquiryElement(item2.StringId.ToLower(), text + " (" + item2.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerCultureTitle", "Select culture"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerMultiDescription", "Select multiple entries; confirm to add them to the list."), list3, isExitShown: true, 0, list3.Count, KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerAdd", "Add"), KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenFactionIdPicker(List<string> list, Action onReturn)
	{
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
			orderby !f.IsKingdomFaction, (f.Name != null) ? f.Name.ToString() : "", f.StringId
			select f).ToList();
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoFaction", "No selectable factions found.")));
			onReturn();
			return;
		}
		List<InquiryElement> list3 = new List<InquiryElement>();
		foreach (IFaction item in list2)
		{
			string text = ((item.Name != null) ? item.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			string text2 = (item.IsKingdomFaction ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerFactionTypeKingdom", "Kingdom") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerFactionTypeFaction", "Faction"));
			list3.Add(new InquiryElement(item.StringId.ToLower(), text + " (" + text2 + ", " + item.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerFactionTitle", "Select faction/kingdom"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerMultiDescription", "Select multiple entries; confirm to add them to the list."), list3, isExitShown: true, 0, list3.Count, KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerAdd", "Add"), KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSettlementIdPicker(List<string> list, Action onReturn)
	{
		OpenSettlementIdPickerPaged(list, onReturn, 0, null);
	}

	private void OpenSettlementIdPickerPaged(List<string> list, Action onReturn, int page, string query)
	{
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
			foreach (Settlement item in Settlement.All)
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.StringId))
				{
					list2.Add(item);
				}
			}
		}
		catch
		{
		}
		list2 = list2.OrderBy((Settlement s) => (s.Name != null) ? s.Name.ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Settlement s) => s.StringId, StringComparer.OrdinalIgnoreCase).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list2 = list2.Where(delegate(Settlement s)
			{
				string text2 = ((s?.Name != null) ? s.Name.ToString() : "").Trim().ToLowerInvariant();
				string text3 = (s?.StringId ?? "").Trim().ToLowerInvariant();
				return text2.Contains(q) || text3.Contains(q);
			}).ToList();
		}
		if (list2.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoSettlement", "No selectable settlements found.") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchNoMatch", "No matching {KIND} found: {QUERY}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement")), ("QUERY", text))));
			onReturn();
			return;
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)list2.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		int num2 = page * pageSize;
		List<Settlement> list3 = list2.Skip(num2).Take(pageSize).ToList();
		List<InquiryElement> list4 = new List<InquiryElement>();
		list4.Add(new InquiryElement("__search__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement"))), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list4.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list4.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page + 1 < num)
		{
			list4.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list4.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (Settlement item2 in list3)
		{
			string text4 = ((item2.Name != null) ? item2.Name.ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = item2.StringId;
			}
			string text5 = (item2.IsVillage ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSettlementTypeVillage", "Village") : (item2.IsCastle ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSettlementTypeCastle", "Castle") : (item2.IsTown ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSettlementTypeTown", "Town") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSettlementTypeSettlement", "Settlement"))));
			list4.Add(new InquiryElement(item2.StringId.ToLowerInvariant(), text4 + " (" + text5 + ", " + item2.StringId + ")", null));
		}
		string text6 = KnowledgeUiTexts.PickerPageSummary(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerKindSettlements", "settlements"), list2.Count, page + 1, num, TrimPreview(text, 60), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSelectOneSuffix", " Select one to add it to the list."));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSettlementTitle", "Select settlement"), text6, list4, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
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
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement"))), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchPrompt", "Enter {KIND} name or {ID}:", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement")), ("ID", "SettlementId")), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						OpenSettlementIdPickerPaged(list, onReturn, 0, input);
					}, delegate
					{
						OpenSettlementIdPickerPaged(list, onReturn, page, text);
					}, shouldInputBeObfuscated: false, null, text));
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	// 目标选择器负责“这个 kind 具体取谁”的那一层：
	// 自动目标直接返回 token，手动目标则打开王国/定居点/家族/英雄选择器。
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
				return;
			case "settlement":
				OpenSettlementPickerSingle(onPickTargetId, onCancel);
				return;
			case "clan":
				OpenClanPickerSingle(onPickTargetId, onCancel);
				return;
			case "hero":
				OpenHeroPickerSingle(onPickTargetId, onCancel);
				return;
			default:
				onCancel();
				return;
			}
		}
		switch ((kind ?? "").Trim())
		{
		case TextMappingKindKingdomName:
		case TextMappingKindKingdomLeaderName:
		case TextMappingKindKingdomRulingClanName:
		case TextMappingKindKingdomCultureName:
		case TextMappingKindKingdomInitialHomeSettlementName:
		case TextMappingKindKingdomAllClans:
		case TextMappingKindKingdomAllLords:
		case TextMappingKindKingdomAllTowns:
		case TextMappingKindKingdomAllCastles:
		case TextMappingKindKingdomAllVillages:
		case TextMappingKindKingdomAllSettlements:
		case TextMappingKindKingdomActivePolicies:
		case TextMappingKindKingdomAlliedKingdoms:
		case TextMappingKindKingdomWarFactions:
			OpenKingdomPickerSingle(onPickTargetId, onCancel);
			break;
		case TextMappingKindSettlementName:
		case TextMappingKindSettlementOwnerClanName:
		case TextMappingKindSettlementOwnerLeaderName:
		case TextMappingKindSettlementOwnerKingdomName:
		case TextMappingKindSettlementOwnerKingdomLeaderName:
		case TextMappingKindSettlementCultureName:
		case TextMappingKindSettlementNotables:
		case TextMappingKindSettlementParties:
		case TextMappingKindSettlementBoundVillages:
			OpenSettlementPickerSingle(onPickTargetId, onCancel);
			break;
		case TextMappingKindClanName:
		case TextMappingKindClanLeaderName:
		case TextMappingKindClanKingdomName:
		case TextMappingKindClanKingdomLeaderName:
		case TextMappingKindClanAllTowns:
		case TextMappingKindClanAllVillages:
		case TextMappingKindClanAllSettlements:
		case TextMappingKindClanMembers:
		case TextMappingKindClanMaleMembers:
		case TextMappingKindClanFemaleMembers:
		case TextMappingKindClanAgeRangeMembers:
			OpenClanPickerSingle(onPickTargetId, onCancel);
			break;
		case TextMappingKindHeroName:
		case TextMappingKindHeroClanName:
		case TextMappingKindHeroKingdomName:
		case TextMappingKindHeroKingdomLeaderName:
		case TextMappingKindHeroSpouseName:
		case TextMappingKindHeroFatherName:
		case TextMappingKindHeroMotherName:
		case TextMappingKindHeroCurrentSettlementName:
			OpenHeroPickerSingle(onPickTargetId, onCancel);
			break;
		default:
			onCancel();
			break;
		}
	}

	private void OpenKingdomPickerSingle(Action<string> onPickTargetId, Action onCancel)
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
		List<Kingdom> list = new List<Kingdom>();
		try
		{
			if (Kingdom.All != null)
			{
				foreach (Kingdom item in Kingdom.All)
				{
					if (item != null && !string.IsNullOrWhiteSpace(item.StringId))
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((Kingdom k) => (k.Name != null) ? k.Name.ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Kingdom k) => k.StringId, StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoKingdom", "No selectable kingdoms found.")));
			onCancel();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (Kingdom item2 in list)
		{
			string text = ((item2.Name != null) ? item2.Name.ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item2.StringId;
			}
			string text2 = (item2.Leader?.Name?.ToString() ?? "").Trim();
			list2.Add(new InquiryElement(item2.StringId, string.IsNullOrWhiteSpace(text2) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerEntryWithLeader", "{NAME} ({ID})", ("NAME", text), ("ID", item2.StringId)) : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerEntryWithLeaderNamed", "{NAME} ({LABEL}: {LEADER})", ("NAME", text), ("LABEL", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerCurrentLeader", "current leader")), ("LEADER", text2)), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerKingdomTitle", "Select kingdom"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerKingdomDescription", "Select one kingdom as the mapping target."), list2, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onCancel();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenHeroPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		OpenHeroPickerSinglePaged(onPickTargetId, onCancel, 0, null);
	}

	private void OpenHeroPickerSinglePaged(Action<string> onPickTargetId, Action onCancel, int page, string query)
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
				list = devEditableHeroListForExternal.Where((Hero h) => h != null && !string.IsNullOrWhiteSpace(h.StringId)).ToList();
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			try
			{
				foreach (Hero allAliveHero in Hero.AllAliveHeroes)
				{
					if (allAliveHero != null && !string.IsNullOrWhiteSpace(allAliveHero.StringId))
					{
						list.Add(allAliveHero);
					}
				}
			}
			catch
			{
			}
		}
		list = (from h in list
			orderby (h.Name != null) ? h.Name.ToString() : "", h.StringId
			select h).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Hero h)
			{
				string text2 = ((h?.Name != null) ? h.Name.ToString() : "").Trim().ToLowerInvariant();
				string text3 = (h?.StringId ?? "").Trim().ToLowerInvariant();
				return text2.Contains(q) || text3.Contains(q);
			}).ToList();
		}
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoHero", "No selectable heroes found.") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchNoMatch", "No matching {KIND} found: {QUERY}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Hero")), ("QUERY", text))));
			onCancel();
			return;
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		int num2 = page * pageSize;
		List<Hero> list2 = list.Skip(num2).Take(pageSize).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Hero"))), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list3.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (Hero item in list2)
		{
			string text4 = ((item.Name != null) ? item.Name.ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = item.StringId;
			}
			list3.Add(new InquiryElement(item.StringId, text4 + " (" + item.StringId + ")", null));
		}
		string text5 = KnowledgeUiTexts.PickerPageSummary(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerKindHeroes", "heroes"), list.Count, page + 1, num, TrimPreview(text, 60), "");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerHeroTitle", "Select hero"), text5, list3, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text6 = selected[0].Identifier as string;
				switch (text6)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Hero"))), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchPrompt", "Enter {KIND} name or {ID}:", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Hero")), ("ID", "HeroId")), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						OpenHeroPickerSinglePaged(onPickTargetId, onCancel, 0, input);
					}, delegate
					{
						OpenHeroPickerSinglePaged(onPickTargetId, onCancel, page, text);
					}, shouldInputBeObfuscated: false, null, text));
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
					string text7 = ((text6 ?? "")).Trim();
					if (string.IsNullOrWhiteSpace(text7))
					{
						onCancel();
					}
					else
					{
						onPickTargetId(text7);
					}
					break;
				}
				}
			}
		}, delegate
		{
			onCancel();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenClanPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		OpenClanPickerSinglePaged(onPickTargetId, onCancel, 0, null);
	}

	private void OpenClanPickerSinglePaged(Action<string> onPickTargetId, Action onCancel, int page, string query)
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
		if (page < 0)
		{
			page = 0;
		}
		List<Clan> list = new List<Clan>();
		try
		{
			if (Clan.All != null)
			{
				foreach (Clan item in Clan.All)
				{
					if (item != null && !string.IsNullOrWhiteSpace(item.StringId) && !item.IsBanditFaction)
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((Clan c) => (c.Name != null) ? c.Name.ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Clan c) => c.StringId, StringComparer.OrdinalIgnoreCase).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Clan c)
			{
				string text2 = ((c?.Name != null) ? c.Name.ToString() : "").Trim().ToLowerInvariant();
				string text3 = (c?.StringId ?? "").Trim().ToLowerInvariant();
				return text2.Contains(q) || text3.Contains(q);
			}).ToList();
		}
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoClan", "No selectable clans found.") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchNoMatch", "No matching {KIND} found: {QUERY}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Clan")), ("QUERY", text))));
			onCancel();
			return;
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		int num2 = page * pageSize;
		List<Clan> list2 = list.Skip(num2).Take(pageSize).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Clan"))), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list3.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (Clan item2 in list2)
		{
			string text4 = ((item2.Name != null) ? item2.Name.ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = item2.StringId;
			}
			string text5 = (item2.Leader?.Name?.ToString() ?? "").Trim();
			list3.Add(new InquiryElement(item2.StringId, string.IsNullOrWhiteSpace(text5) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerEntryWithLeader", "{NAME} ({ID})", ("NAME", text4), ("ID", item2.StringId)) : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerEntryWithLeaderNamed", "{NAME} ({LABEL}: {LEADER})", ("NAME", text4), ("LABEL", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerCurrentClanLeader", "current clan leader")), ("LEADER", text5)), null));
		}
		string text6 = KnowledgeUiTexts.PickerPageSummary(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerKindClans", "clans"), list.Count, page + 1, num, TrimPreview(text, 60), "");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerClanTitle", "Select clan"), text6, list3, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text7 = selected[0].Identifier as string;
				switch (text7)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Clan"))), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchPrompt", "Enter {KIND} name or {ID}:", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Clan")), ("ID", "ClanId")), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						OpenClanPickerSinglePaged(onPickTargetId, onCancel, 0, input);
					}, delegate
					{
						OpenClanPickerSinglePaged(onPickTargetId, onCancel, page, text);
					}, shouldInputBeObfuscated: false, null, text));
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
					string text8 = ((text7 ?? "")).Trim();
					if (string.IsNullOrWhiteSpace(text8))
					{
						onCancel();
					}
					else
					{
						onPickTargetId(text8);
					}
					break;
				}
				}
			}
		}, delegate
		{
			onCancel();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSettlementPickerSingle(Action<string> onPickTargetId, Action onCancel)
	{
		OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, 0, null);
	}

	private void OpenSettlementPickerSinglePaged(Action<string> onPickTargetId, Action onCancel, int page, string query)
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
		if (page < 0)
		{
			page = 0;
		}
		List<Settlement> list = new List<Settlement>();
		try
		{
			foreach (Settlement item in Settlement.All)
			{
				if (item != null && !string.IsNullOrWhiteSpace(item.StringId))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((Settlement s) => (s.Name != null) ? s.Name.ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((Settlement s) => s.StringId, StringComparer.OrdinalIgnoreCase).ToList();
		string text = (query ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Settlement s)
			{
				string text2 = ((s?.Name != null) ? s.Name.ToString() : "").Trim().ToLowerInvariant();
				string text3 = (s?.StringId ?? "").Trim().ToLowerInvariant();
				return text2.Contains(q) || text3.Contains(q);
			}).ToList();
		}
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(text) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerNoSettlement", "No selectable settlements found.") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchNoMatch", "No matching {KIND} found: {QUERY}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement")), ("QUERY", text))));
			onCancel();
			return;
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		int num2 = page * pageSize;
		List<Settlement> list2 = list.Skip(num2).Take(pageSize).ToList();
		List<InquiryElement> list3 = new List<InquiryElement>();
		list3.Add(new InquiryElement("__search__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement"))), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list3.Add(new InquiryElement("__clear__", KnowledgeUiTexts.ClearSearch, null));
		}
		if (page > 0)
		{
			list3.Add(new InquiryElement("__prev__", KnowledgeUiTexts.PreviousPage, null));
		}
		if (page + 1 < num)
		{
			list3.Add(new InquiryElement("__next__", KnowledgeUiTexts.NextPage, null));
		}
		list3.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (Settlement item2 in list2)
		{
			string text4 = ((item2.Name != null) ? item2.Name.ToString() : "").Trim();
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = item2.StringId;
			}
			string text5 = (item2.OwnerClan?.Leader?.Name?.ToString() ?? "").Trim();
			list3.Add(new InquiryElement(item2.StringId, string.IsNullOrWhiteSpace(text5) ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerEntryWithLeader", "{NAME} ({ID})", ("NAME", text4), ("ID", item2.StringId)) : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerEntryWithLeaderNamed", "{NAME} ({LABEL}: {LEADER})", ("NAME", text4), ("LABEL", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerCurrentRuler", "current ruler")), ("LEADER", text5)), null));
		}
		string text6 = KnowledgeUiTexts.PickerPageSummary(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerKindSettlements", "settlements"), list.Count, page + 1, num, TrimPreview(text, 60), "");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSettlementTitle", "Select settlement"), text6, list3, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onCancel();
			}
			else
			{
				string text7 = selected[0].Identifier as string;
				switch (text7)
				{
				case "__search__":
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchTitle", "Search {KIND}", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement"))), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerSearchPrompt", "Enter {KIND} name or {ID}:", ("KIND", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement")), ("ID", "SettlementId")), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Search, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, 0, input);
					}, delegate
					{
						OpenSettlementPickerSinglePaged(onPickTargetId, onCancel, page, text);
					}, shouldInputBeObfuscated: false, null, text));
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
					string text8 = ((text7 ?? "")).Trim();
					if (string.IsNullOrWhiteSpace(text8))
					{
						onCancel();
					}
					else
					{
						onPickTargetId(text8);
					}
					break;
				}
				}
			}
		}, delegate
		{
			onCancel();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenStringListEditor(string title, List<string> list, Action onReturn)
	{
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
			list2.Add(new InquiryElement("__pick__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerStringListPick", "Select from list"), null));
		}
		list2.Add(new InquiryElement("__add__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerStringListAdd", "Enter manually (one at a time)"), null));
		if (list.Count > 0)
		{
			list2.Add(new InquiryElement("__remove__", KnowledgeUiTexts.Delete, null));
		}
		foreach (string item in list)
		{
			if (!string.IsNullOrWhiteSpace(item))
			{
				list2.Add(new InquiryElement("__v__" + item, item, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerStringListDescription", "Enter one value at a time; confirm to add it to the list."), list2, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
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
					InformationManager.ShowTextInquiry(new TextInquiryData(title, KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerStringListInputPrompt", "Enter a value:"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
					{
						string v = (input ?? "").Trim();
						if (!string.IsNullOrEmpty(v) && !list.Any((string x) => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
						{
							list.Add(v);
						}
						TouchRuleData();
						onReturn();
					}, delegate
					{
						onReturn();
					}));
					break;
				case "__remove__":
					OpenStringListRemoveMenu(title, list, onReturn);
					break;
				default:
					onReturn();
					break;
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenStringListRemoveMenu(string title, List<string> list, Action onReturn)
	{
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
				list2.Add(new InquiryElement(item, item, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.StringListRemoveTitle(title), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_PickerStringListRemovePrompt", "Select an item to delete:"), list2, isExitShown: true, 0, 1, KnowledgeUiTexts.Delete, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSkillMinEditor(LoreWhen when, Action onReturn)
	{
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
		list.Add(new InquiryElement("__pick__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillPicker", "Select skill from list"), null));
		list.Add(new InquiryElement("__add__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillManual", "Enter SkillId manually"), null));
		if (when.SkillMin.Count > 0)
		{
			list.Add(new InquiryElement("__remove__", KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillRemove", "Delete skill condition"), null));
		}
		foreach (KeyValuePair<string, int> item in when.SkillMin.OrderBy((KeyValuePair<string, int> k) => k.Key, StringComparer.OrdinalIgnoreCase))
		{
			string text = (item.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, KnowledgeUiTexts.SkillItem(text, item.Value), null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillTitle", "Skill level conditions"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillDescription", "Configure SkillId >= value. A value of 0 means no threshold (Skill>=0); a negative value removes the skill condition."), list, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
		{
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
					InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillManualTitle", "Enter SkillId manually"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillPrompt", "Enter SkillId:"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
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
					}, delegate
					{
						OpenSkillMinEditor(when, onReturn);
					}));
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSkillMinRemoveMenu(LoreWhen when, Action onReturn)
	{
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
				list.Add(new InquiryElement(text, KnowledgeUiTexts.SkillItem(text, item.Value), null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillRemoveTitle", "Delete skill condition"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillRemovePrompt", "Select an item to delete:"), list, isExitShown: true, 0, 1, KnowledgeUiTexts.Delete, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenSkillMinValueInput(LoreWhen when, string skillId, Action onReturn)
	{
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
		InformationManager.ShowTextInquiry(new TextInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillSetTitle", "Set skill minimum"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillSetPrompt", "Enter minimum skill level (integer):"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, KnowledgeUiTexts.Confirm, KnowledgeUiTexts.Cancel, delegate(string input)
		{
			string s = (input ?? "").Trim();
			if (!int.TryParse(s, out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.InvalidInteger));
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
		}, delegate
		{
			onReturn();
		}, shouldInputBeObfuscated: false, null, num.ToString()));
	}

	private void OpenSkillPicker(Action<string> onPickSkillId, Action onReturn)
	{
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
					if (value != null && !string.IsNullOrWhiteSpace(value.StringId))
					{
						list.Add(value);
					}
				}
			}
		}
		catch
		{
		}
		list = list.OrderBy((SkillObject s) => (s.Name != null) ? s.Name.ToString() : "", StringComparer.OrdinalIgnoreCase).ThenBy((SkillObject s) => s.StringId, StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillNoSkills", "No selectable skills found.")));
			onReturn();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		foreach (SkillObject item in list)
		{
			string text = ((item.Name != null) ? item.Name.ToString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = item.StringId;
			}
			list2.Add(new InquiryElement(item.StringId, text + " (" + item.StringId + ")", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillPickerTitle", "Select skill"), KnowledgeUiTexts.Ui("AF_KNOWLEDGE_SkillPickerDescription", "Select a skill, then set its minimum level."), list2, isExitShown: true, 0, 1, KnowledgeUiTexts.Select, KnowledgeUiTexts.Back, delegate(List<InquiryElement> selected)
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
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildWhenLabel(LoreWhen when)
	{
		if (when == null)
		{
			return KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenGeneral", "General");
		}
		List<string> list = new List<string>();
		if (when.Cultures != null && when.Cultures.Count > 0) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenCultureLabel", "Culture"));
		if (when.KingdomIds != null && when.KingdomIds.Count > 0) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenKingdomLabel", "Faction"));
		if (when.SettlementIds != null && when.SettlementIds.Count > 0) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSettlementLabel", "Settlement"));
		if (when.Roles != null && when.Roles.Count > 0) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenRoleLabel", "Identity"));
		if ((when.HeroIds != null && when.HeroIds.Count > 0) || (when.IdentityIds != null && when.IdentityIds.Count > 0)) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSpecificIdentityLabel", "Specific identity"));
		if (when.IsFemale.HasValue) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenGenderLabel", "Gender"));
		if (when.IsClanLeader.HasValue) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenClanLeaderLabel", "Clan leader"));
		if (when.SkillMin != null && when.SkillMin.Count > 0) list.Add(KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenSkillLabel", "Skill"));
		if (list.Count == 0) return KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenGeneral", "General");
		return KnowledgeUiTexts.WhenLabelExclusive(string.Join("+", list));
	}

	private static string BuildWhenDetail(LoreWhen when)
	{
		if (when == null) return KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenDetailGeneral", "Current: general (no conditions)");
		string skills = KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenEmpty", "(empty)");
		try
		{
			if (when.SkillMin != null && when.SkillMin.Count > 0)
			{
				skills = string.Join(", ", from kv in when.SkillMin.Where((KeyValuePair<string, int> kv) => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0).OrderBy((KeyValuePair<string, int> kv) => kv.Key, StringComparer.OrdinalIgnoreCase)
					select (kv.Key ?? "").Trim() + ">=" + kv.Value);
			}
		}
		catch
		{
			skills = KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenEmpty", "(empty)");
		}
		string gender = !when.IsFemale.HasValue ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenAny", "Any") : (when.IsFemale.Value ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenFemale", "Female") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenMale", "Male"));
		string clanLeader = !when.IsClanLeader.HasValue ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenAny", "Any") : (when.IsClanLeader.Value ? KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenYes", "Yes") : KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenNo", "No"));
		return KnowledgeUiTexts.WhenDetail(ListOrEmpty(when.Cultures), ListOrEmpty(when.KingdomIds), ListOrEmpty(when.SettlementIds), ListOrEmpty(FormatRoleListForDisplay(when.Roles)), ListOrEmpty(FormatEffectiveIdentityListForDisplay(when)), skills, gender, clanLeader);
	}
	private static List<string> FormatRoleListForDisplay(List<string> list)
	{
		List<string> result = new List<string>();
		try
		{
			if (list == null || list.Count == 0)
			{
				return result;
			}
			foreach (string item in list)
			{
				string text = (item ?? "").Trim();
				if (!string.IsNullOrEmpty(text))
				{
					result.Add(GetRoleDisplayName(text) + "（" + text + "）");
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<string> FormatIdentityListForDisplay(List<string> list)
	{
		List<string> result = new List<string>();
		try
		{
			if (list == null || list.Count == 0)
			{
				return result;
			}
			foreach (string item in list)
			{
				string text = ResolveRoleIdentityDisplayText(item);
				if (!string.IsNullOrWhiteSpace(text))
				{
					result.Add(text);
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static List<string> FormatEffectiveIdentityListForDisplay(LoreWhen when)
	{
		List<string> result = new List<string>();
		try
		{
			if (when == null)
			{
				return result;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (when.HeroIds != null)
			{
				foreach (string heroId in when.HeroIds)
				{
					string text = BuildLegacyHeroIdentityDisplayText(heroId);
					if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
					{
						result.Add(text);
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
						result.Add(text2);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static string ListOrEmpty(List<string> list)
	{
		if (list == null || list.Count == 0)
		{
			return KnowledgeUiTexts.Ui("AF_KNOWLEDGE_WhenEmpty", "(empty)");
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
