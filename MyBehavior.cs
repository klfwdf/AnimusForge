using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class MyBehavior : CampaignBehaviorBase
{
	public static MyBehavior Instance { get; private set; }

	private enum ChatMode
	{
		Normal,
		Give,
		Show
	}

	private enum WeeklyReportUiStage
	{
		None,
		Failure,
		RetryProgress
	}

	private enum SaveAndExitStage
	{
		None,
		WaitingForCurrentSave,
		WaitingForRequestedQuickSave
	}

	private enum SaveAndExitReason
	{
		None,
		MissingOnnx,
		WeeklyReport
	}

	private enum KingdomStabilityTier
	{
		ExtremelyPoor,
		VeryPoor,
		Poor,
		Average,
		FairlyHigh,
		High,
		ExtremelyHigh
	}

	private class PendingTradeContext
	{
		public bool IsGive;
	}

	private class PendingTradeItem
	{
		public bool IsGold;

		public string ItemId;

		public string ItemName;

		public int Amount;

		public ItemObject Item;

		public int InventoryUnitValue;
	}

	private class HeroShownRecord
	{
		public int ShownGold;

		public Dictionary<string, int> ShownItems = new Dictionary<string, int>();
	}

	private class TradeResourceOption
	{
		public bool IsGold;

		public string ItemId;

		public string Name;

		public int AvailableAmount;

		public ItemObject Item;

		public long InventoryTotalValue;

		public int InventoryUnitValue;
	}

	public enum PartyTransferEntrySection
	{
		PlayerTroops,
		PlayerPrisoners,
		NpcTroops,
		NpcPrisoners
	}

	public sealed class PartyTransferPromptEntry
	{
		public int PromptIndex;

		public PartyTransferEntrySection Section;

		public CharacterObject Character;

		public string DisplayName;

		public int Count;

		public int WoundedCount;

		public int WageDenarsPerDay;

		public int HirePriceDenarsPerUnit;

		public int BuyPriceDenarsPerUnit;

		public bool IsHero;

		public PartyBase OwnerParty;
	}

	private class DialogueDay
	{
		public int GameDayIndex;

		public string GameDate;

		public List<string> Lines = new List<string>();
	}

	private class NpcActionEntry
	{
		public int Day;

		public int Order;

		public int Sequence;

		public string GameDate;

		public string Text;

		public string StableKey;

		public string ActionKind;

		public string ActorHeroId;

		public string ActorClanId;

		public string ActorKingdomId;

		public string TargetHeroId;

		public string TargetClanId;

		public string TargetKingdomId;

		public string SettlementId;

		public string SettlementName;

		public string SettlementOwnerHeroId;

		public string SettlementOwnerClanId;

		public string SettlementOwnerKingdomId;

		public string PreviousSettlementOwnerHeroId;

		public string PreviousSettlementOwnerClanId;

		public string PreviousSettlementOwnerKingdomId;

		public string LocationText;

		public bool? Won;

		public bool IsMajor;

		public List<string> RelatedHeroIds = new List<string>();

		public List<string> RelatedClanIds = new List<string>();

		public List<string> RelatedKingdomIds = new List<string>();
	}

	private sealed class NpcActionFacts
	{
		public string ActionKind;

		public string ActorHeroId;

		public string ActorClanId;

		public string ActorKingdomId;

		public string TargetHeroId;

		public string TargetClanId;

		public string TargetKingdomId;

		public string SettlementId;

		public string SettlementName;

		public string SettlementOwnerHeroId;

		public string SettlementOwnerClanId;

		public string SettlementOwnerKingdomId;

		public string PreviousSettlementOwnerHeroId;

		public string PreviousSettlementOwnerClanId;

		public string PreviousSettlementOwnerKingdomId;

		public string LocationText;

		public bool? Won;

		public bool IsMajor;

		public List<string> RelatedHeroIds = new List<string>();

		public List<string> RelatedClanIds = new List<string>();

		public List<string> RelatedKingdomIds = new List<string>();
	}

	private class NpcPersonaProfile
	{
		public string Personality;

		public string Background;

		public string VoiceId;
	}

	private sealed class DevKingdomSummaryMenuItem
	{
		public string KingdomId;

		public string DisplayName;
	}

	private sealed class EventMaterialReference
	{
		public string MaterialType;

		public string Label;

		public string SnapshotText;

		public string HeroId;

		public string KingdomId;

		public string SettlementId;

		public bool RecentOnly;

		public string ActionKind;

		public string ActorHeroId;

		public string ActorClanId;

		public string ActorKingdomId;

		public string TargetHeroId;

		public string TargetClanId;

		public string TargetKingdomId;

		public string SettlementOwnerClanId;

		public string SettlementOwnerKingdomId;

		public string PreviousSettlementOwnerClanId;

		public string PreviousSettlementOwnerKingdomId;

		public List<string> RelatedHeroIds = new List<string>();

		public List<string> RelatedClanIds = new List<string>();

		public List<string> RelatedKingdomIds = new List<string>();

		public List<string> SourceStableKeys = new List<string>();

		public List<string> SourceActionKinds = new List<string>();

		public int SourceMaterialCount;

		public string ActionStableKey;

		public int? ActionDay;

		public int? ActionOrder;

		public int? ActionSequence;
	}

	private sealed class EventRecordEntry
	{
		public string EventId;

		public int WeekIndex;

		public string EventKind;

		public string ScopeKingdomId;

		public string Title;

		public string ShortSummary;

		public string Summary;

		public string TagText;

		public string PromptText;

		public int CreatedDay;

		public string CreatedDate;

		public List<EventMaterialReference> Materials = new List<EventMaterialReference>();
	}

	private sealed class EventSourceMaterialEntry
	{
		public int Day;

		public int Sequence;

		public string GameDate;

		public string MaterialKind;

		public string Label;

		public string SnapshotText;

		public string StableKey;

		public string KingdomId;

		public string SettlementId;

		public bool IncludeInWorld;

		public bool IncludeInKingdom;
	}

	private sealed class TownStatSnapshot
	{
		public float Prosperity;

		public float Loyalty;

		public float Security;

		public float FoodStocks;

		public float Militia;

		public int Garrison;
	}

	public class ShoutPromptContext
	{
		public string Extras;

		public bool UseDuelContext;

		public bool UseRewardContext;

		public bool IsLoanContext;

		public bool IsQualified;
	}

	private sealed class HistoryLineEntry
	{
		public int Day;

		public string Date;

		public string Line;

		public int Index;
	}

	private sealed class ArchiveHit
	{
		public HistoryLineEntry Entry;

		public double Score;

		public double BaseScore;

		public double RerankScore;
	}

	private sealed class WeightedRecallQueryInput
	{
		public string Text;

		public float Weight;

		public List<string> Terms = new List<string>();
	}

	private sealed class RecallLineScore
	{
		public HistoryLineEntry Entry;

		public double RawScore;

		public double BaseScore;

		public double RerankScore;
	}

	private sealed class HistoryRecallAggregate
	{
		public ArchiveHit Hit;

		public double ScoreSum;

		public int HitCount;

		public int BestRank = int.MaxValue;

		public double BestScore;
	}

	private class PatienceState
	{
		public float Value;

		public float LastDay;

		public int NoInterestRounds;

		public int ExhaustedRefusalCount;
	}

	private class PatienceStateSaveModel
	{
		public float Value { get; set; }

		public float LastDay { get; set; }

		public int NoInterestRounds { get; set; }

		public int ExhaustedRefusalCount { get; set; }
	}

	private struct PatienceSnapshot
	{
		public string Key;

		public string DisplayName;

		public int Relation;

		public int Trust;

		public int PublicTrust;

		public int PrivateLove;

		public int Max;

		public float Current;

		public string PatienceLevel;

		public string RelationLevel;

		public string TrustLevel;

		public string PublicTrustLevel;

		public string PrivateLoveLevel;
	}

	private enum PatienceMood
	{
		Neutral,
		Delighted,
		Joy,
		Annoyed,
		Bored
	}

	private enum ExportImportScope
	{
		All,
		HeroNpcAll,
		PersonalityBackground,
		UnnamedPersona,
		DialogueHistory,
		Debt,
		EventData,
		Knowledge,
		VoiceMapping
	}

	private class UnnamedPersonaSingleJson
	{
		public string Key;

		public string Personality;

		public string Background;
	}

	private sealed class EventWorldOpeningSummaryJson
	{
		public string Summary;
	}

	private sealed class EventImportPayload
	{
		public bool HasWorldSummaryFile;

		public string WorldSummary;

		public bool HasKingdomSummariesFile;

		public Dictionary<string, string> KingdomSummaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public bool HasEventRecordsFile;

		public List<EventRecordEntry> EventRecords = new List<EventRecordEntry>();
	}

	private sealed class WeeklyEventMaterialPreviewGroup
	{
		public string GroupKind;

		public string KingdomId;

		public string Title;

		public string Summary;

		public List<EventMaterialReference> Materials = new List<EventMaterialReference>();
	}

	private sealed class WeeklyReportPromptProfile
	{
		public int Preset;

		public int MinWords;

		public int MaxWords;

		public string Label;
	}

	private sealed class WeeklyReportRequestResult
	{
		public bool Success;

		public string Title;

		public string ShortSummary;

		public string Report;

		public string TagText;

		public string PromptPreview;

		public string FailureReason;

		public int AttemptsUsed;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}

	private sealed class WeeklyReportBatchRequest
	{
		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public List<WeeklyEventMaterialPreviewGroup> Groups = new List<WeeklyEventMaterialPreviewGroup>();
	}

	private sealed class WeeklyReportBatchBlockResult
	{
		public string ReportId;

		public string Kind;

		public string KingdomId;

		public string Title;

		public string ShortSummary;

		public string Report;

		public string TagText;

		public bool Parsed;

		public string FailureReason;
	}

	private sealed class WeeklyReportBatchRequestResult
	{
		public bool Success = false;

		public string PromptPreview = "";

		public string RawResponse = "";

		public string FailureReason = "";

		public List<WeeklyReportBatchBlockResult> Blocks = new List<WeeklyReportBatchBlockResult>();

		public List<string> MissingReportIds = new List<string>();

		public int AttemptsUsed = 0;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}

	private sealed class WeeklyReportBatchExecutionResult
	{
		public int BatchIndex;

		public WeeklyReportBatchRequest Batch;

		public WeeklyReportBatchRequestResult Result;

		public long ElapsedMilliseconds;
	}

	private sealed class DevWeeklyReportBatchPreviewEntry
	{
		public string PreviewKey = "";

		public string BatchLabel = "";

		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public List<string> ReportIds = new List<string>();

		public string PromptPreview = "";

		public string ResponsePreview = "";

		public bool Success;

		public string FailureReason = "";

		public int AttemptsUsed;
	}

	private sealed class WeeklyReportGenerationResult
	{
		public int SuccessCount;

		public int FailureCount;

		public bool Completed;

		public bool BlockedByFatalFailure;

		public WeeklyReportRetryContext RetryContext;
	}

	private sealed class WeeklyReportRetryContext
	{
		public List<WeeklyEventMaterialPreviewGroup> Groups = new List<WeeklyEventMaterialPreviewGroup>();

		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public string DisplayLabel;

		public bool OpenViewerWhenDone;

		public bool IsAutoGeneration;

		public string FailedGroupTitle;

		public string FailedReason;

		public int AttemptsUsed;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}

	private sealed class WeekZeroShortSummaryRequest
	{
		public string EventId;

		public string EventKind;

		public string KingdomId;

		public string Title;

		public string Summary;

		public string SourceHash;

		public string GenerationKey;
	}

	private sealed class KingdomRebellionCandidateInfo
	{
		public Clan Clan;

		public string ClanId;

		public string ClanName;

		public int RelationToKing;

		public int TownCount;

		public int CastleCount;

		public int TotalFortificationCount;

		public int ClanTier;

		public float Score;

		public bool Eligible;

		public string Note;

		public List<string> PreviewFollowerClanNames = new List<string>();
	}

	private sealed class KingdomRebellionFollowerInfo
	{
		public Clan Clan;

		public string ClanId;

		public string ClanName;

		public int RelationToKing;

		public int RelationToLeader;

		public int TownCount;

		public int CastleCount;

		public int ClanTier;

		public float Score;

		public bool Eligible;

		public string Note;
	}

	private sealed class KingdomRebellionResolutionResult
	{
		public Kingdom Kingdom;

		public int WeekIndex;

		public bool Forced;

		public int StabilityValue;

		public string StabilityTierText;

		public float TriggerChance;

		public float? Roll;

		public bool PassedChanceGate;

		public Clan SelectedClan;

		public List<Clan> SelectedFollowerClans = new List<Clan>();

		public bool Executed;

		public string Message;

		public List<KingdomRebellionCandidateInfo> Candidates = new List<KingdomRebellionCandidateInfo>();

		public List<KingdomRebellionFollowerInfo> FollowerCandidates = new List<KingdomRebellionFollowerInfo>();
	}

	private sealed class RebelKingdomNamingResult
	{
		public string FormalName;

		public string ShortName;

		public string EncyclopediaText;

		public bool UsedFallback;

		public string FailureReason;

		public int AttemptsUsed;
	}

	private sealed class PendingDevForcedKingdomRebellionContext
	{
		public string KingdomId;

		public string ClanId;

		public int WeekIndex;

		public int RelationToKing;

		public int TownCount;

		public int CastleCount;

		public List<string> FollowerClanIds = new List<string>();

		public RebelKingdomNamingResult NamingResult;
	}

	private sealed class PendingAutomaticKingdomRebellionContext
	{
		public string KingdomId;

		public string ClanId;

		public int WeekIndex;

		public int StabilityValue;

		public string StabilityTierText;

		public int RelationToKing;

		public int TownCount;

		public int CastleCount;

		public List<string> FollowerClanIds = new List<string>();

		public RebelKingdomNamingResult NamingResult;
	}

	private sealed class ClanVisualSnapshot
	{
		public Banner Banner;

		public uint Color;

		public uint Color2;

		public uint BackgroundColor;

		public uint IconColor;
	}

	private sealed class RebelFactionColorChoice
	{
		public uint BackgroundColor;

		public uint IconColor;

		public Banner Banner;
	}

	private sealed class ApiCallResult
	{
		public bool Success;

		public string Content;

		public string ErrorMessage;

		public int? StatusCode;

		public string ResponseBody;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}

	private const int MOUSEEVENTF_LEFTDOWN = 2;

	private const int MOUSEEVENTF_LEFTUP = 4;

	private string _aiResponseText = "";

	private PendingTradeContext _pendingTrade;

	private List<PendingTradeItem> _pendingTradeItems = new List<PendingTradeItem>();

	private int _pendingTradeItemIndex;

	private Dictionary<string, HeroShownRecord> _shownRecords = new Dictionary<string, HeroShownRecord>();

	private Dictionary<string, string> _shownRecordStorage = new Dictionary<string, string>();

	private List<TradeResourceOption> _currentTradeOptions = new List<TradeResourceOption>();

	private bool _nextDuelRiskWarningByLiteral = true;

	private string _ruleStickyTargetKey;

	private int _ruleStickyDuelRoundsLeft;

	private int _ruleStickyRewardRoundsLeft;

	private int _ruleStickyLoanRoundsLeft;

	private long _suppressAutoClickUntilUtcTicks;

	private bool _overlayQuickTalkDisableHooked;

	private const int MaxDialogueHistoryLines = 260;

	private const int HistoryRecentTurnsDefault = 20;

	private const int HistoryRecentTurnsMin = 1;

	private const int HistoryRecentTurnsMax = 80;

	private const int HistoryArchiveSectionMaxChars = 900;

	private const int HistoryArchiveTopK = 10;

	private const int HistoryArchiveCandidateLimit = 260;

	private const int HistoryOnnxRerankLimit = 120;

	private const int HistoryArchiveRecallMaxItems = 12;

	private const int RecentNpcActionWindowDays = 21;

	private const int MaxRecentNpcActionEntriesPerHero = 96;

	private const int MaxMajorNpcActionEntriesPerHero = 160;

	private const int WeeklyReportRequestMaxTokens = 5000;

	private const int KingdomStabilityMinValue = 0;

	private const int KingdomStabilityMaxValue = 100;

	private const int KingdomStabilityDefaultValue = 50;

	private const int RebelKingdomInitialStabilityValue = 30;

	private Dictionary<string, List<DialogueDay>> _dialogueHistory = new Dictionary<string, List<DialogueDay>>();

	private Dictionary<string, string> _dialogueHistoryStorage = new Dictionary<string, string>();

	private Dictionary<string, List<NpcActionEntry>> _npcMajorActions = new Dictionary<string, List<NpcActionEntry>>();

	private Dictionary<string, string> _npcMajorActionStorage = new Dictionary<string, string>();

	private Dictionary<string, List<NpcActionEntry>> _npcRecentActions = new Dictionary<string, List<NpcActionEntry>>();

	private Dictionary<string, string> _npcRecentActionStorage = new Dictionary<string, string>();

	private int _npcActionGlobalOrderCounter;

	private Dictionary<string, NpcPersonaProfile> _npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();

	private Dictionary<string, string> _npcPersonaProfileStorage = new Dictionary<string, string>();

	private Dictionary<string, string> _eventKingdomOpeningSummaries = new Dictionary<string, string>();

	private Dictionary<string, string> _eventKingdomOpeningSummaryStorage = new Dictionary<string, string>();

	private string _eventWorldOpeningSummary = "";

	private List<EventRecordEntry> _eventRecordEntries = new List<EventRecordEntry>();

	private string _eventRecordJsonStorage = "";

	private List<EventSourceMaterialEntry> _eventSourceMaterials = new List<EventSourceMaterialEntry>();

	private string _eventSourceMaterialJsonStorage = "";

	private Dictionary<string, int> _kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _kingdomStabilityStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _kingdomStabilityRelationAppliedOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _kingdomStabilityRelationOffsetStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _weeklyReportAppliedStabilityDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _weeklyReportAppliedStabilityDeltaStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private int _lastAutoGeneratedWeeklyReportWeek = -1;

	private int _lastProcessedKingdomRebellionWeek = -1;

	private bool _weeklyReportGenerationInProgress;

	private WeeklyReportUiStage _weeklyReportUiStage;

	private WeeklyReportRetryContext _weeklyReportRetryContext;

	private bool _weeklyReportManualRetryInProgress;

	private int _weeklyReportManualRetryVersion;

	private List<DevWeeklyReportBatchPreviewEntry> _latestWeeklyReportBatchDevPreviews = new List<DevWeeklyReportBatchPreviewEntry>();

	private bool _pendingWeeklyReportManualRetryResult;

	private bool _pendingWeeklyReportManualRetrySucceeded;

	private string _pendingWeeklyReportManualRetryMessage = "";

	private WeeklyReportRetryContext _pendingWeeklyReportManualRetryContext;

	private long _weeklyReportUiResumeAfterUtcTicks;

	private bool _weeklyReportReopenAfterApiConfig;

	private long _weeklyReportReopenAfterApiConfigUtcTicks;

	private bool _missingOnnxGateActive;

	private long _missingOnnxGateResumeAfterUtcTicks;

	private SaveAndExitStage _saveAndExitStage;

	private SaveAndExitReason _saveAndExitReason;

	private bool _devForcedKingdomRebellionInProgress;

	private bool _pendingDevForcedKingdomRebellionReady;

	private PendingDevForcedKingdomRebellionContext _pendingDevForcedKingdomRebellionContext;

	private bool _automaticKingdomRebellionFlowActive;

	private bool _automaticKingdomRebellionInProgress;

	private bool _pendingAutomaticKingdomRebellionReady;

	private PendingAutomaticKingdomRebellionContext _pendingAutomaticKingdomRebellionContext;

	private readonly List<PendingAutomaticKingdomRebellionContext> _queuedAutomaticKingdomRebellions = new List<PendingAutomaticKingdomRebellionContext>();

	private int _pendingAutoWeeklyReportWeek;

	private readonly object _weekZeroShortSummaryQueueLock = new object();

	private readonly HashSet<string> _weekZeroShortSummaryGenerationInFlight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _weekZeroShortSummaryGenerationAttempted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly List<WeekZeroShortSummaryRequest> _weekZeroShortSummaryPendingQueue = new List<WeekZeroShortSummaryRequest>();

	private bool _weekZeroShortSummaryQueueProcessing;

	private long _weekZeroShortSummaryLastRequestUtcTicks;

	private string _voiceMappingJsonStorage = "";

	private string _voiceMappingExportFolderStorage = "";

	private string _unnamedPersonaJsonStorage = "";

	private readonly Dictionary<string, TownStatSnapshot> _townStatSnapshots = new Dictionary<string, TownStatSnapshot>(StringComparer.OrdinalIgnoreCase);

	private readonly object _npcPersonaAutoGenLock = new object();

	private HashSet<string> _npcPersonaAutoGenInFlight = new HashSet<string>();

	private HashSet<string> _recentlyDefeatedByPlayer = new HashSet<string>();

	private HashSet<string> _recentlyReleasedPrisoners = new HashSet<string>();

	private static int _cachedPlayerClanTier;

	private static long _cachedPlayerClanTierUtcTicks;

	private List<Hero> _devEditableHeroes = new List<Hero>();

	private Hero _devEditingHero;

	private const int PatienceMaxCap = 80;

	private const int PatienceMinCap = 10;

	private const int PatienceDefaultMaxForUnnamed = 30;

	private const float PatienceRecoveryPerDay = 4f;

	private const int PatienceNoInterestPenaltyThreshold = 3;

	private const int RelationGainOnJoy = 1;

	private const int RelationPenaltyOnBored = -1;

	private const int RelationPenaltyOnAnnoyed = -2;

	private static readonly string[] PatienceLevelTexts = new string[10] { "枯竭", "烦躁", "不耐", "冷淡", "一般", "尚可", "愿听", "投入", "热络", "兴致高" };

	private static readonly string[] RelationLevelTexts = new string[10] { "死敌", "敌对", "厌恶", "疏离", "冷漠", "中立", "熟络", "友好", "亲近", "至交" };

	private static readonly string[] RelationAiBehaviorTexts = new string[10] { "把玩家视为重大威胁，语气强硬且排斥，不愿合作。", "明显敌意与戒备，倾向拒绝请求，回应尖锐。", "主观反感较强，容易挑刺，合作意愿很低。", "保持距离与怀疑，只做最基本交流。", "态度偏冷，交流克制，基本不主动示好。", "无明显好恶，按利益与场面决定态度。", "对玩家有一定熟悉感，语气较缓，可有限合作。", "整体友好，愿意倾听并给出建设性回应。", "明显信任玩家，交流积极，合作意愿高。", "高度信任与支持，语气亲近，优先站在玩家一边。" };

	private static readonly Regex MoodTagRegex = new Regex("\\[ACTION:MOOD:([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex TransferTroopTagRegex = new Regex("\\[ATT:(\\d+):(\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex TransferPrisonerTagRegex = new Regex("\\[ATP:(\\d+):(\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private Dictionary<string, PatienceState> _patienceStates = new Dictionary<string, PatienceState>();

	private Dictionary<string, string> _patienceStorage = new Dictionary<string, string>();

	private readonly object _patienceLock = new object();

	private string _devOpsHeroId;

	private string _devOpsHeroName;

	private bool _devPendingKnowledgeSingleImportPicked;

	private string _devPendingKnowledgeSingleImportFolderName;

	private bool _devPendingUnnamedSingleImportPicked;

	private string _devPendingUnnamedSingleImportFolderName;

	private string _devHistorySearchQuery = string.Empty;

	private string _devHeroSelectionQuery = string.Empty;

	private string _devSingleNpcSelectionQuery = string.Empty;

	private int _devHeroSelectionPage;

	private int _devSingleNpcSelectionPage;

	[DllImport("user32.dll")]
	private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

	public MyBehavior()
	{
		Instance = this;
	}

	private static int ClampRecentDialogueTurns(int turns)
	{
		if (turns < 1)
		{
			return 1;
		}
		if (turns > 80)
		{
			return 80;
		}
		return turns;
	}

	private static int CountPromptChars(string text)
	{
		return (!string.IsNullOrEmpty(text)) ? text.Length : 0;
	}

	private static int GetRecentDialogueTurnsFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return ClampRecentDialogueTurns(settings.RecentDialogueTurns);
			}
		}
		catch
		{
		}
		return 20;
	}

	private static int ClampHistoryReturnCap(int value)
	{
		if (value < 1)
		{
			value = 1;
		}
		if (value > 12)
		{
			value = 12;
		}
		return value;
	}

	private static int GetHistoryReturnCapFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return ClampHistoryReturnCap(settings.HistoryRecallTopN);
			}
		}
		catch
		{
		}
		return 4;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, OnDailyTickTown);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
		CampaignEvents.ArmyGathered.AddNonSerializedListener(this, OnArmyGathered);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
		CampaignEvents.OnPartyLeftArmyEvent.AddNonSerializedListener(this, OnPartyLeftArmy);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnSiegeEventEnded);
		CampaignEvents.OnMobilePartyJoinedToSiegeEventEvent.AddNonSerializedListener(this, OnMobilePartyJoinedSiege);
		CampaignEvents.OnMobilePartyLeftSiegeEventEvent.AddNonSerializedListener(this, OnMobilePartyLeftSiege);
		CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener(this, OnSiegeCompleted);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnBeforeHeroesMarried);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnClanDefectedEvent.AddNonSerializedListener(this, OnClanDefected);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
		CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, OnSiegeAftermathApplied);
		CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				return;
			}
			MapEventSide mapEventSide = ((mapEvent.PlayerSide == BattleSideEnum.Attacker) ? mapEvent.DefenderSide : mapEvent.AttackerSide);
			foreach (MapEventParty party in mapEventSide.Parties)
			{
				Hero hero = party.Party?.LeaderHero;
				if (hero != null && hero != Hero.MainHero && hero.IsLord)
				{
					_recentlyDefeatedByPlayer.Add(hero.StringId);
					Logger.Log("BattleStatus", $"原版战斗结束：玩家击败了 {hero.Name}");
					AppendExternalDialogueHistory(hero, null, null, $"你在一场战斗中被 {Hero.MainHero.Name} 击败了。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnMapEventEnded: " + ex.Message);
		}
		try
		{
			TrackNpcActionsFromMapEvent(mapEvent);
		}
		catch (Exception ex2)
		{
			Logger.Log("NpcAction", "[ERROR] TrackNpcActionsFromMapEvent: " + ex2.Message);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		try
		{
			Hero hero = capturer?.LeaderHero ?? capturer?.MobileParty?.LeaderHero;
			Settlement settlement = ResolveSettlementForPartyBase(capturer);
			string text = GetLocationLabelForPartyBase(capturer);
			string text2 = BuildPrisonerTakenStableKey(hero, prisoner);
			if (ShouldTrackNpcActionHero(hero))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("prisoner_taken_captor", hero);
				npcActionFacts.LocationText = text;
				ApplySettlementFacts(npcActionFacts, settlement, locationText: text);
				ApplyTargetFacts(npcActionFacts, prisoner);
				if (prisoner?.MapFaction != null)
				{
					AddRelatedFactionFacts(npcActionFacts, prisoner.MapFaction);
				}
				string text3 = "你俘虏了" + GetHeroDisplayName(prisoner) + "。";
				RecordNpcMajorAction(hero, text3, text2 + ":captor", npcActionFacts);
				RecordNpcRecentAction(hero, text3, text2 + ":captor", facts: npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(prisoner))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("prisoner_taken_prisoner", prisoner);
				npcActionFacts2.LocationText = text;
				ApplySettlementFacts(npcActionFacts2, settlement, locationText: text);
				if (hero != null)
				{
					ApplyTargetFacts(npcActionFacts2, hero);
				}
				else if (capturer?.MobileParty?.MapFaction != null)
				{
					AddRelatedFactionFacts(npcActionFacts2, capturer.MobileParty.MapFaction);
				}
				string text4 = ((hero != null) ? ("你被" + GetHeroDisplayName(hero) + "俘虏了。") : "你被敌方俘虏了。");
				RecordNpcMajorAction(prisoner, text4, text2 + ":prisoner", npcActionFacts2);
				RecordNpcRecentAction(prisoner, text4, text2 + ":prisoner", facts: npcActionFacts2);
			}
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturer?.LeaderHero == Hero.MainHero || capturer?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} 被玩家俘虏");
				AppendExternalDialogueHistory(prisoner, null, null, $"你被 {Hero.MainHero.Name} 俘虏了，你现在是他的囚犯。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerTaken: " + ex.Message);
		}
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		try
		{
			Hero hero = party?.LeaderHero ?? party?.MobileParty?.LeaderHero;
			Settlement settlement = ResolveSettlementForPartyBase(party);
			string text = GetLocationLabelForPartyBase(party);
			string text2 = BuildPrisonerReleasedStableKey(hero, prisoner, detail);
			string endCaptivityDetailLabel = GetEndCaptivityDetailLabel(detail);
			if (ShouldTrackNpcActionHero(prisoner))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("prisoner_released_prisoner", prisoner);
				npcActionFacts.LocationText = text;
				ApplySettlementFacts(npcActionFacts, settlement, locationText: text);
				if (hero != null)
				{
					ApplyTargetFacts(npcActionFacts, hero);
				}
				else
				{
					AddRelatedFactionFacts(npcActionFacts, capturerFaction);
				}
				string text3 = string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? "你结束了囚禁状态。" : ("你" + endCaptivityDetailLabel + "并恢复了自由。");
				RecordNpcMajorAction(prisoner, text3, text2 + ":prisoner", npcActionFacts);
				RecordNpcRecentAction(prisoner, text3, text2 + ":prisoner", facts: npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(hero))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("prisoner_released_captor", hero);
				npcActionFacts2.LocationText = text;
				ApplySettlementFacts(npcActionFacts2, settlement, locationText: text);
				ApplyTargetFacts(npcActionFacts2, prisoner);
				string text4 = string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? ("你失去了对" + GetHeroDisplayName(prisoner) + "的控制。") : (GetHeroDisplayName(prisoner) + endCaptivityDetailLabel + "，不再是你的囚犯。");
				RecordNpcMajorAction(hero, text4, text2 + ":captor", npcActionFacts2);
				RecordNpcRecentAction(hero, text4, text2 + ":captor", facts: npcActionFacts2);
			}
			if (prisoner != null && prisoner != Hero.MainHero && prisoner.IsLord && (capturerFaction == Hero.MainHero?.MapFaction || party?.LeaderHero == Hero.MainHero || party?.MobileParty?.ActualClan == Clan.PlayerClan))
			{
				_recentlyReleasedPrisoners.Add(prisoner.StringId);
				string text5 = detail switch
				{
					EndCaptivityDetail.Ransom => "通过支付赎金", 
					EndCaptivityDetail.ReleasedByChoice => "被主动释放", 
					EndCaptivityDetail.ReleasedAfterPeace => "因和平协议", 
					EndCaptivityDetail.ReleasedAfterEscape => "成功逃脱", 
					_ => "", 
				};
				Logger.Log("BattleStatus", $"NPC {prisoner.Name} {text5}获得自由 (detail={detail})");
				AppendExternalDialogueHistory(prisoner, null, null, $"你{text5}从 {Hero.MainHero.Name} 的囚禁中获得了自由。");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerReleased: " + ex.Message);
		}
	}

	private void OnArmyCreated(Army army)
	{
		try
		{
			Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			if (!ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			string text = GetArmyDisplayName(army);
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_create", hero);
			Settlement settlement = ResolveGatheringPointSettlement(army?.LeaderParty, army?.LeaderParty);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(army?.LeaderParty);
			ApplySettlementFacts(npcActionFacts, settlement, locationText: npcActionFacts.LocationText);
			RecordNpcMajorAction(hero, "你组建并统领了" + text + "。", "army_create:" + text, npcActionFacts);
			RecordNpcRecentAction(hero, "你组建并统领了" + text + "。", "army_create:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyCreated: " + ex.Message);
		}
	}

	private void OnArmyGathered(Army army, IMapPoint gatheringPoint)
	{
		try
		{
			Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			if (!ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			Settlement settlement = ResolveGatheringPointSettlement(gatheringPoint, army?.LeaderParty);
			string text = ResolveGatheringPointLabel(gatheringPoint, army?.LeaderParty);
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_gather", hero);
			npcActionFacts.LocationText = text;
			ApplySettlementFacts(npcActionFacts, settlement, locationText: text);
			RecordNpcRecentAction(hero, "你率领" + GetArmyDisplayName(army) + "在" + text + "集结。", "army_gather:" + GetArmyDisplayName(army) + ":" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyGathered: " + ex.Message);
		}
	}

	private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isNoNotification)
	{
		try
		{
			Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
			if (!ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_disperse", hero);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(army?.LeaderParty);
			RecordNpcRecentAction(hero, "你统领的" + GetArmyDisplayName(army) + "已解散。", "army_disperse:" + GetArmyDisplayName(army), facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyDispersed: " + ex.Message);
		}
	}

	private void OnPartyJoinedArmy(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			Army army = party?.Army;
			if (!ShouldTrackNpcActionHero(leaderHero) || army == null)
			{
				return;
			}
			if (party == army.LeaderParty || leaderHero == army.ArmyOwner || leaderHero == army.LeaderParty?.LeaderHero)
			{
				return;
			}
			string text = GetArmyDisplayName(army);
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_join", leaderHero);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(party);
			RecordNpcMajorAction(leaderHero, "你加入了" + text + "。", "army_join:" + text, npcActionFacts);
			RecordNpcRecentAction(leaderHero, "你加入了" + text + "。", "army_join:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnPartyJoinedArmy: " + ex.Message);
		}
	}

	private void OnPartyLeftArmy(MobileParty party, Army army)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			string text = GetArmyDisplayName(army);
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_leave", leaderHero);
			npcActionFacts.LocationText = GetNearestSettlementNameForParty(party);
			RecordNpcRecentAction(leaderHero, "你离开了" + text + "。", "army_leave:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnPartyLeftArmy: " + ex.Message);
		}
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		try
		{
			Settlement settlement = ResolveSiegeSettlement(siegeEvent);
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Attacker))
			{
				string text = BuildSiegeStartNarrative(settlement, isAttacker: true, siegeEvent);
				string text2 = settlement?.Name?.ToString() ?? "某处要塞";
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_start_attack", item);
				ApplySettlementFacts(npcActionFacts, settlement);
				npcActionFacts.TargetKingdomId = GetKingdomId(settlement?.MapFaction);
				RecordNpcMajorAction(item, text, "siege_start:" + text2, npcActionFacts);
				RecordNpcRecentAction(item, text, "siege_start:" + text2, facts: npcActionFacts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Defender))
			{
				string text3 = BuildSiegeStartNarrative(settlement, isAttacker: false, siegeEvent);
				string text4 = settlement?.Name?.ToString() ?? "某处要塞";
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("siege_start_defend", item2);
				ApplySettlementFacts(npcActionFacts2, settlement);
				npcActionFacts2.TargetKingdomId = GetKingdomId(siegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction);
				RecordNpcMajorAction(item2, text3, "siege_defend:" + text4, npcActionFacts2);
				RecordNpcRecentAction(item2, text3, "siege_defend:" + text4, facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSiegeEventStarted: " + ex.Message);
		}
	}

	private void OnSiegeEventEnded(SiegeEvent siegeEvent)
	{
		try
		{
			Settlement settlement = ResolveSiegeSettlement(siegeEvent);
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Attacker))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_end_attack", item);
				ApplySettlementFacts(npcActionFacts, settlement);
				RecordNpcRecentAction(item, "你结束了对" + text + "的围城。", "siege_end:" + text, facts: npcActionFacts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, BattleSideEnum.Defender))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("siege_end_defend", item2);
				ApplySettlementFacts(npcActionFacts2, settlement);
				RecordNpcRecentAction(item2, text + "的守城战已经结束。", "siege_end_defend:" + text, facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSiegeEventEnded: " + ex.Message);
		}
	}

	private void OnMobilePartyJoinedSiege(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			Settlement settlement = party.BesiegedSettlement ?? ResolveSiegeSettlement(party.SiegeEvent);
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_join", leaderHero);
			ApplySettlementFacts(npcActionFacts, settlement);
			RecordNpcRecentAction(leaderHero, "你加入了对" + text + "的围城。", "siege_join:" + text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnMobilePartyJoinedSiege: " + ex.Message);
		}
	}

	private void OnMobilePartyLeftSiege(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			Settlement settlement = party.BesiegedSettlement ?? ResolveSiegeSettlement(party.SiegeEvent);
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			bool flag = settlement != null && party?.MapFaction != null && settlement.MapFaction == party.MapFaction;
			string text2 = (flag ? (text + "结清了战利品和战俘") : (text + "处理完了围城中产生的战利品以及战俘。"));
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_leave", leaderHero);
			ApplySettlementFacts(npcActionFacts, settlement);
			RecordNpcRecentAction(leaderHero, text2, "siege_leave:" + text + ":" + flag, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnMobilePartyLeftSiege: " + ex.Message);
		}
	}

	private void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, MapEvent.BattleTypes battleType)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			string text = settlement?.Name?.ToString() ?? "某处要塞";
			string text2 = (siegeSuccess ? ("你在" + text + "的围城中获胜。") : ("你在" + text + "的围城中失利。"));
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_complete", leaderHero);
			ApplySettlementFacts(npcActionFacts, settlement);
			npcActionFacts.Won = siegeSuccess;
			RecordNpcMajorAction(leaderHero, text2, "siege_complete:" + text + ":" + siegeSuccess, npcActionFacts);
			RecordNpcRecentAction(leaderHero, text2, "siege_complete:" + text + ":" + siegeSuccess, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSiegeCompleted: " + ex.Message);
		}
	}

	private void OnDailyTickParty(MobileParty party)
	{
		try
		{
			Hero leaderHero = party?.LeaderHero;
			if (!ShouldTrackNpcActionHero(leaderHero))
			{
				return;
			}
			string text = BuildRecentPartyBehaviorText(party);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = BuildRecentPartyBehaviorStableKey(party);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("daily_behavior", leaderHero);
				MobileParty mobileParty = party.Army?.LeaderParty ?? party;
				Settlement settlement = mobileParty?.BesiegedSettlement ?? ResolveSiegeSettlement(mobileParty?.SiegeEvent) ?? mobileParty?.TargetSettlement ?? party.TargetSettlement ?? party.CurrentSettlement ?? party.LastVisitedSettlement;
				ApplySettlementFacts(npcActionFacts, settlement, locationText: GetNearestSettlementNameForParty(party));
				if (mobileParty?.TargetParty?.LeaderHero != null)
				{
					ApplyTargetFacts(npcActionFacts, mobileParty.TargetParty.LeaderHero);
				}
				RecordNpcRecentAction(leaderHero, text, text2, dedupeAcrossWindow: true, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnDailyTickParty: " + ex.Message);
		}
	}

	private void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification)
	{
		try
		{
			string text = BuildOrderedHeroPairStableKey("marriage", hero1, hero2);
			if (ShouldTrackNpcActionHero(hero1))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("marriage", hero1);
				ApplyTargetFacts(npcActionFacts, hero2);
				RecordNpcMajorAction(hero1, "你与" + GetHeroDisplayName(hero2) + "缔结了婚姻。", text, npcActionFacts);
				RecordNpcRecentAction(hero1, "你与" + GetHeroDisplayName(hero2) + "缔结了婚姻。", text, facts: npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(hero2))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("marriage", hero2);
				ApplyTargetFacts(npcActionFacts2, hero1);
				RecordNpcMajorAction(hero2, "你与" + GetHeroDisplayName(hero1) + "缔结了婚姻。", text, npcActionFacts2);
				RecordNpcRecentAction(hero2, "你与" + GetHeroDisplayName(hero1) + "缔结了婚姻。", text, facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnBeforeHeroesMarried: " + ex.Message);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		try
		{
			string text = BuildClanChangedKingdomStableKey(clan, oldKingdom, newKingdom, detail);
			string text2 = BuildClanChangedKingdomNarrative(clan, oldKingdom, newKingdom, detail);
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_changed_kingdom", item);
				npcActionFacts.TargetKingdomId = GetKingdomId(newKingdom ?? oldKingdom);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(oldKingdom));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(newKingdom));
				RecordNpcMajorAction(item, text2, text, npcActionFacts);
				RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
			if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion && clan != null && oldKingdom != null)
			{
				string clanFortificationSummary = BuildClanFortificationSummary(clan);
				string text3 = text2;
				if (!string.IsNullOrWhiteSpace(clanFortificationSummary))
				{
					text3 = text3 + " " + clanFortificationSummary;
				}
				Settlement settlement = clan.Settlements?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));
				RecordEventSourceMaterial("kingdom_rebellion", "家族叛乱 - " + GetClanDisplayName(clan), text3, text + ":material", GetKingdomId(oldKingdom), GetSettlementId(settlement), includeInWorld: true, includeInKingdom: true);
			}
			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom && clan != null && newKingdom != null)
			{
				Settlement settlement2 = clan.Settlements?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));
				RecordEventSourceMaterial("kingdom_created", "新王国建立 - " + GetKingdomDisplayName(newKingdom, "新王国"), text2, text + ":material", GetKingdomId(newKingdom), GetSettlementId(settlement2), includeInWorld: true, includeInKingdom: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnClanChangedKingdom: " + ex.Message);
		}
	}

	private void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
		try
		{
			string text = BuildClanChangedKingdomStableKey(clan, oldKingdom, newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection);
			string text2 = "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + GetKingdomDisplayName(oldKingdom, "原王国") + "，转投" + GetKingdomDisplayName(newKingdom, "新王国") + "。";
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_defected", item);
				npcActionFacts.TargetKingdomId = GetKingdomId(newKingdom);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(oldKingdom));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(newKingdom));
				RecordNpcMajorAction(item, text2, text, npcActionFacts);
				RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnClanDefected: " + ex.Message);
		}
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		try
		{
			if (decision == null)
			{
				return;
			}
			string text = BuildKingdomDecisionStableKey(decision);
			string text2 = BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer: true);
			Hero leader = decision.ProposerClan?.Leader;
			if (ShouldTrackNpcActionHero(leader))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("kingdom_decision_concluded", leader);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(decision.ProposerClan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
				ApplyKingdomDecisionSpecificFacts(npcActionFacts, decision, chosenOutcome);
				RecordNpcMajorAction(leader, text2, text + ":proposer", npcActionFacts);
				RecordNpcRecentAction(leader, text2, text + ":proposer", facts: npcActionFacts);
			}
			Hero leader2 = decision.DetermineChooser()?.Leader;
			if (ShouldTrackNpcActionHero(leader2) && !string.Equals(GetHeroId(leader2), GetHeroId(leader), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("kingdom_decision_concluded", leader2);
				AddUniqueId(npcActionFacts2.RelatedClanIds, GetClanId(decision.ProposerClan));
				AddUniqueId(npcActionFacts2.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
				ApplyKingdomDecisionSpecificFacts(npcActionFacts2, decision, chosenOutcome);
				string text3 = BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer: false);
				RecordNpcMajorAction(leader2, text3, text + ":chooser", npcActionFacts2);
				RecordNpcRecentAction(leader2, text3, text + ":chooser", facts: npcActionFacts2);
			}
			string text4 = BuildKingdomDecisionSupporterSummary(decision, chosenOutcome);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				string kingdomId = GetKingdomId(decision.Kingdom);
				RecordEventSourceMaterial("kingdom_decision_support", "决议支持明细 - " + GetKingdomDisplayName(decision.Kingdom, "该王国"), text4, text + ":supporters", kingdomId, "", includeInWorld: false, includeInKingdom: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnKingdomDecisionConcluded: " + ex.Message);
		}
	}

	private void OnRulingClanChanged(Kingdom kingdom, Clan newRulingClan)
	{
		try
		{
			string text = "ruling_clan_changed:" + GetKingdomId(kingdom) + ":" + GetClanId(newRulingClan);
			string text2 = "你所在的" + GetClanDisplayName(newRulingClan) + "家族已成为" + GetKingdomDisplayName(kingdom, "该王国") + "的执政家族。";
			foreach (Hero item in GetTrackedLordsForClan(newRulingClan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("ruling_clan_changed", item);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(newRulingClan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(kingdom));
				RecordNpcMajorAction(item, text2, text, npcActionFacts);
				RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnRulingClanChanged: " + ex.Message);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		try
		{
			string text = BuildSettlementOwnerChangedStableKey(settlement, newOwner, oldOwner, detail);
			string text2 = GetSettlementDisplayName(settlement);
			if (ShouldTrackNpcActionHero(newOwner))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("settlement_owner_changed_gain", newOwner);
				ApplyTargetFacts(npcActionFacts, oldOwner);
				ApplySettlementFacts(npcActionFacts, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(newOwner, "你获得了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":gain", npcActionFacts);
				RecordNpcRecentAction(newOwner, "你获得了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":gain", facts: npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(oldOwner))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("settlement_owner_changed_loss", oldOwner);
				ApplyTargetFacts(npcActionFacts2, newOwner);
				ApplySettlementFacts(npcActionFacts2, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(oldOwner, "你失去了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":loss", npcActionFacts2);
				RecordNpcRecentAction(oldOwner, "你失去了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":loss", facts: npcActionFacts2);
			}
			if (ShouldTrackNpcActionHero(capturerHero) && !string.Equals(GetHeroId(capturerHero), GetHeroId(newOwner), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts3 = CreateNpcActionFacts("settlement_owner_changed_capture", capturerHero);
				ApplyTargetFacts(npcActionFacts3, newOwner);
				ApplySettlementFacts(npcActionFacts3, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(capturerHero, "你促成了" + text2 + "的易主，新的所有者是" + GetHeroDisplayName(newOwner) + "。", text + ":capture", npcActionFacts3);
				RecordNpcRecentAction(capturerHero, "你促成了" + text2 + "的易主，新的所有者是" + GetHeroDisplayName(newOwner) + "。", text + ":capture", facts: npcActionFacts3);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSettlementOwnerChanged: " + ex.Message);
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			if (victim == null)
			{
				return;
			}
			string text = "hero_killed:" + GetHeroId(victim) + ":" + detail;
			if (ShouldTrackNpcActionHero(killer))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("hero_killed", killer);
				ApplyTargetFacts(npcActionFacts, victim);
				RecordNpcMajorAction(killer, "你" + GetHeroKilledVerb(detail) + GetHeroDisplayName(victim) + "。", text + ":killer", npcActionFacts);
				RecordNpcRecentAction(killer, "你" + GetHeroKilledVerb(detail) + GetHeroDisplayName(victim) + "。", text + ":killer", facts: npcActionFacts);
			}
			Hero leader = victim.Clan?.Leader;
			if (ShouldTrackNpcActionHero(leader) && !string.Equals(GetHeroId(leader), GetHeroId(victim), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("clan_member_killed", leader);
				ApplyTargetFacts(npcActionFacts2, victim);
				if (killer != null)
				{
					AddUniqueId(npcActionFacts2.RelatedHeroIds, GetHeroId(killer));
					AddUniqueId(npcActionFacts2.RelatedClanIds, GetClanId(killer.Clan));
					AddUniqueId(npcActionFacts2.RelatedKingdomIds, GetKingdomId(killer.MapFaction));
				}
				string text2 = "你所在的" + GetClanDisplayName(victim.Clan) + "家族失去了" + GetHeroDisplayName(victim) + "。";
				RecordNpcMajorAction(leader, text2, text + ":clan", npcActionFacts2);
				RecordNpcRecentAction(leader, text2, text + ":clan", facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnHeroKilled: " + ex.Message);
		}
	}

	private void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
	{
		try
		{
			if (!ShouldTrackNpcActionHero(mother))
			{
				return;
			}
			Hero hero = aliveChildren?.FirstOrDefault((Hero x) => x != null);
			string text = "birth:" + GetHeroId(mother) + ":" + GetHeroId(hero);
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("birth", mother);
			if (hero != null)
			{
				AddUniqueId(npcActionFacts.RelatedHeroIds, GetHeroId(hero));
			}
			string text2 = "你诞下一名子嗣";
			if (hero != null)
			{
				text2 = text2 + "，孩子是" + GetHeroDisplayName(hero);
			}
			if (stillbornCount > 0)
			{
				text2 = text2 + "。此次分娩还伴随" + stillbornCount + "名夭折婴儿";
			}
			text2 += "。";
			RecordNpcMajorAction(mother, text2, text, npcActionFacts);
			RecordNpcRecentAction(mother, text2, text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnGivenBirth: " + ex.Message);
		}
	}

	private void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		try
		{
			Clan clan = newLeader?.Clan ?? oldLeader?.Clan;
			if (clan == null)
			{
				return;
			}
			string text = "clan_leader_changed:" + GetClanId(clan) + ":" + GetHeroId(newLeader);
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_leader_changed", item);
				ApplyTargetFacts(npcActionFacts, newLeader);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				string text2 = string.Equals(GetHeroId(item), GetHeroId(newLeader), StringComparison.OrdinalIgnoreCase) ? ("你已成为" + GetClanDisplayName(clan) + "家族的新族长。") : (GetHeroDisplayName(newLeader) + "已成为" + GetClanDisplayName(clan) + "家族的新族长。");
				RecordNpcMajorAction(item, text2, text, npcActionFacts);
				RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnClanLeaderChanged: " + ex.Message);
		}
	}

	private void OnDailyTickTown(Town town)
	{
		try
		{
			ApplyRulingClanSettlementLoyaltyAdjustmentForLowClanCountKingdom(town);
			TrackTownWeeklyMaterialChanges(town);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnDailyTickTown: " + ex.Message);
		}
	}

	private void OnDailyTick()
	{
		try
		{
			EnsureWeekZeroOpeningSummaryEvents();
			ApplyKingdomStabilityRelationAdjustments();
			int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
			int num = ((currentGameDayIndexSafe > 0) ? (currentGameDayIndexSafe / 7) : 0);
			if (currentGameDayIndexSafe > 0 && currentGameDayIndexSafe % 7 == 0)
			{
				TryProcessWeeklyKingdomRebellions(num);
			}
			if (_weeklyReportGenerationInProgress)
			{
				return;
			}
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || !settings.AutoGenerateWeeklyReports)
			{
				return;
			}
			if (currentGameDayIndexSafe <= 0 || currentGameDayIndexSafe % 7 != 0)
			{
				return;
			}
			if (num <= 0 || _lastAutoGeneratedWeeklyReportWeek >= num)
			{
				return;
			}
			if (_automaticKingdomRebellionFlowActive)
			{
				_pendingAutoWeeklyReportWeek = Math.Max(_pendingAutoWeeklyReportWeek, num);
				return;
			}
			StartAutoWeeklyReportsForWeek(num, currentGameDayIndexSafe);
		}
		catch (Exception ex)
		{
			_weeklyReportGenerationInProgress = false;
			Logger.Log("EventWeeklyReport", "[ERROR] OnDailyTick auto-generate failed: " + ex.Message);
		}
	}

	private void StartAutoWeeklyReportsForWeek(int weekIndex, int currentGameDayIndexSafe)
	{
		if (weekIndex <= 0 || _lastAutoGeneratedWeeklyReportWeek >= weekIndex || _weeklyReportGenerationInProgress)
		{
			return;
		}
		if (_automaticKingdomRebellionFlowActive)
		{
			_pendingAutoWeeklyReportWeek = Math.Max(_pendingAutoWeeklyReportWeek, weekIndex);
			return;
		}
		int startDay = Math.Max(0, (weekIndex - 1) * 7);
		int endDay = currentGameDayIndexSafe - 1;
		List<WeeklyEventMaterialPreviewGroup> list = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups(startDay, endDay));
		_weeklyReportGenerationInProgress = true;
		_pendingAutoWeeklyReportWeek = 0;
		_ = GenerateAutoWeeklyReportsAsync(list, weekIndex, startDay, endDay);
	}

	private void TryStartDeferredAutoWeeklyReports()
	{
		if (_pendingAutoWeeklyReportWeek <= 0 || _weeklyReportGenerationInProgress || _automaticKingdomRebellionFlowActive)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.AutoGenerateWeeklyReports)
		{
			return;
		}
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		if (currentGameDayIndexSafe <= 0)
		{
			return;
		}
		int num = currentGameDayIndexSafe / 7;
		if (num < _pendingAutoWeeklyReportWeek)
		{
			return;
		}
		StartAutoWeeklyReportsForWeek(_pendingAutoWeeklyReportWeek, currentGameDayIndexSafe);
	}

	private async Task GenerateAutoWeeklyReportsAsync(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay)
	{
		try
		{
			WeeklyReportGenerationResult weeklyReportGenerationResult = await GenerateWeeklyReportsBatchedAsyncInternal(groups, weekIndex, startDay, endDay, "第" + weekIndex + "周自动周报", openViewerWhenDone: false, queueBlockingPopupOnFatalFailure: true, isAutoGeneration: true);
			if (weeklyReportGenerationResult != null && weeklyReportGenerationResult.Completed && !weeklyReportGenerationResult.BlockedByFatalFailure)
			{
				_lastAutoGeneratedWeeklyReportWeek = Math.Max(_lastAutoGeneratedWeeklyReportWeek, weekIndex);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[ERROR] GenerateAutoWeeklyReportsAsync failed: " + ex);
		}
		finally
		{
			_weeklyReportGenerationInProgress = false;
		}
	}

	private void EnsureWeekZeroOpeningSummaryEvents()
	{
		UpsertWeekZeroOpeningSummaryEvent("world", "", "第0天世界开局概要", _eventWorldOpeningSummary, "世界开局概要", "world_opening_summary");
		foreach (Kingdom devEditableKingdom in GetDevEditableKingdoms())
		{
			string kingdomOpeningSummary = GetKingdomOpeningSummary(devEditableKingdom);
			if (!string.IsNullOrWhiteSpace(kingdomOpeningSummary))
			{
				string text = devEditableKingdom.Name?.ToString() ?? (devEditableKingdom.StringId ?? "王国");
				UpsertWeekZeroOpeningSummaryEvent("kingdom", devEditableKingdom.StringId ?? "", text + "第0天开局概要", kingdomOpeningSummary, text + " 开局概要", "kingdom_opening_summary");
			}
		}
	}

	private static string ComputeWeekZeroShortSummarySourceHash(string sourceText)
	{
		string text = (sourceText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			byte[] array = SHA1.Create().ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder(12);
			for (int i = 0; i < 6 && i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return text.Length.ToString();
		}
	}

	private static string BuildWeekZeroPromptText(string sourceHash, bool llmGenerated)
	{
		return "[BOOTSTRAP][WEEK0_SHORT=" + (llmGenerated ? "LLM" : "FALLBACK") + "][SOURCE=" + ((sourceHash ?? "").Trim()) + "]";
	}

	private static bool HasWeekZeroLlmShortSummary(EventRecordEntry entry, string sourceHash)
	{
		if (entry == null)
		{
			return false;
		}
		string text = (entry.PromptText ?? "").Trim();
		string text2 = (sourceHash ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		return text.IndexOf("[BOOTSTRAP][WEEK0_SHORT=LLM][SOURCE=" + text2 + "]", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string NormalizeWeekZeroShortSummaryResponse(string rawResponse, string fallbackSource)
	{
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return BuildFallbackWeeklyReportShortSummary(fallbackSource);
		}
		Match match = Regex.Match(text, "\\[SHORT\\]\\s*(?<short>[\\s\\S]+)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			text = (match.Groups["short"]?.Value ?? "").Trim();
		}
		if (text.StartsWith("[") && text.IndexOf(']') > 0)
		{
			int num = text.IndexOf(']');
			if (num >= 0 && num + 1 < text.Length)
			{
				text = text.Substring(num + 1).Trim();
			}
		}
		text = text.Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return BuildFallbackWeeklyReportShortSummary(fallbackSource);
		}
		if (text.Length > 140)
		{
			text = BuildFallbackWeeklyReportShortSummary(text);
		}
		if (text.Length < 20)
		{
			string fallbackWeeklyReportShortSummary = BuildFallbackWeeklyReportShortSummary(fallbackSource);
			if (!string.IsNullOrWhiteSpace(fallbackWeeklyReportShortSummary))
			{
				return fallbackWeeklyReportShortSummary;
			}
		}
		return text.Trim();
	}

	private static string BuildWeekZeroShortSummarySystemPrompt(string eventKind, string kingdomId, string title)
	{
		string text = string.Equals((eventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界第0周短周报" : (ResolveKingdomDisplay(kingdomId) + "第0周短周报");
		return "你要把开局概况压缩成一条适合 NPC 常驻读取的短周报。只输出 [SHORT] 段，不要输出 [TITLE]、[REPORT]、[TAGS]。内容必须是 20-140 个汉字，简洁、客观、像第0周的局势提要，不要使用列表，不要解释规则，不要添加编造事实。\n当前对象：" + text + "\n标题参考：" + ((title ?? "").Trim());
	}

	private static string BuildWeekZeroShortSummaryUserPrompt(string summary)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("请根据下面这段起始概况，写一条第0周短周报。");
		stringBuilder.AppendLine("输出格式必须为：");
		stringBuilder.AppendLine("[SHORT] 你的短周报");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("起始概况：");
		stringBuilder.AppendLine((summary ?? "").Trim());
		return stringBuilder.ToString().TrimEnd();
	}

	private static int GetWeekZeroShortSummaryQueuePriority(WeekZeroShortSummaryRequest request, Dictionary<string, int> kingdomOrder)
	{
		if (request == null)
		{
			return 5000;
		}
		if (string.Equals((request.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			string text = (request.KingdomId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && kingdomOrder != null && kingdomOrder.TryGetValue(text, out var value))
			{
				return (value == 0) ? 0 : (value + 1);
			}
			return 1000;
		}
		if (string.Equals((request.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}
		return 3000;
	}

	private void SortWeekZeroShortSummaryPendingQueue()
	{
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(_weekZeroShortSummaryPendingQueue.Where((WeekZeroShortSummaryRequest x) => x != null && string.Equals((x.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeekZeroShortSummaryRequest x) => x.KingdomId));
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < kingdomIdsByPlayerProximity.Count; i++)
		{
			string text = (kingdomIdsByPlayerProximity[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = i;
			}
		}
		_weekZeroShortSummaryPendingQueue.Sort(delegate(WeekZeroShortSummaryRequest a, WeekZeroShortSummaryRequest b)
		{
			int weekZeroShortSummaryQueuePriority = GetWeekZeroShortSummaryQueuePriority(a, dictionary);
			int weekZeroShortSummaryQueuePriority2 = GetWeekZeroShortSummaryQueuePriority(b, dictionary);
			int num = weekZeroShortSummaryQueuePriority.CompareTo(weekZeroShortSummaryQueuePriority2);
			if (num != 0)
			{
				return num;
			}
			return string.Compare((a?.Title ?? "").Trim(), (b?.Title ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
		});
	}

	private void EnsureWeekZeroShortSummaryQueueWorker()
	{
		bool flag = false;
		lock (_weekZeroShortSummaryQueueLock)
		{
			if (!_weekZeroShortSummaryQueueProcessing && _weekZeroShortSummaryPendingQueue.Count > 0)
			{
				_weekZeroShortSummaryQueueProcessing = true;
				flag = true;
			}
		}
		if (flag)
		{
			_ = ProcessWeekZeroShortSummaryQueueAsync();
		}
	}

	private async Task WaitForWeekZeroShortSummaryRequestSlotAsync()
	{
		int weeklyReportRequestIntervalMs = GetWeeklyReportRequestIntervalMs();
		if (weeklyReportRequestIntervalMs <= 0)
		{
			lock (_weekZeroShortSummaryQueueLock)
			{
				_weekZeroShortSummaryLastRequestUtcTicks = DateTime.UtcNow.Ticks;
			}
			return;
		}
		while (true)
		{
			int num = 0;
			lock (_weekZeroShortSummaryQueueLock)
			{
				long ticks = DateTime.UtcNow.Ticks;
				long num2 = TimeSpan.FromMilliseconds(weeklyReportRequestIntervalMs).Ticks;
				long num3 = _weekZeroShortSummaryLastRequestUtcTicks + num2;
				if (_weekZeroShortSummaryLastRequestUtcTicks <= 0 || ticks >= num3)
				{
					_weekZeroShortSummaryLastRequestUtcTicks = ticks;
					return;
				}
				num = Math.Max(40, (int)Math.Ceiling(new TimeSpan(num3 - ticks).TotalMilliseconds));
			}
			await Task.Delay(num);
		}
	}

	private async Task<bool> GenerateWeekZeroShortSummaryWithRetriesAsync(WeekZeroShortSummaryRequest request)
	{
		if (request == null || string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.Summary))
		{
			return false;
		}
		for (int i = 1; i <= 3; i++)
		{
			await WaitForWeekZeroShortSummaryRequestSlotAsync();
			ApiCallResult apiCallResult = await CallUniversalApiDetailed(BuildWeekZeroShortSummarySystemPrompt(request.EventKind, request.KingdomId, request.Title), BuildWeekZeroShortSummaryUserPrompt(request.Summary), logToEventLogs: true, eventLogSource: "EventWeeklyReport");
			if (apiCallResult.Success)
			{
				string text = NormalizeWeekZeroShortSummaryResponse(apiCallResult.Content, request.Summary);
				if (string.IsNullOrWhiteSpace(text))
				{
					Logger.Log("EventWeeklyReport", "[Week0Short][WARN] 短周报解析为空 " + request.EventId + "，尝试 " + i + "/3");
				}
				else
				{
					EventRecordEntry eventRecordEntry = _eventRecordEntries?.FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), request.EventId, StringComparison.OrdinalIgnoreCase));
					if (eventRecordEntry == null)
					{
						return false;
					}
					string text2 = ComputeWeekZeroShortSummarySourceHash(eventRecordEntry.Summary ?? "");
					if (!string.Equals(text2, (request.SourceHash ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
					eventRecordEntry.ShortSummary = text.Trim();
					eventRecordEntry.PromptText = BuildWeekZeroPromptText(request.SourceHash, llmGenerated: true);
					_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
					return true;
				}
			}
			else
			{
				Logger.Log("EventWeeklyReport", "[Week0Short][WARN] 短周报生成失败 " + request.EventId + "，尝试 " + i + "/3 -> " + (apiCallResult.ErrorMessage ?? "未知错误"));
			}
			if (i < 3)
			{
				int num = Math.Max(1200, GetWeeklyReportRequestIntervalMs());
				if (apiCallResult != null && apiCallResult.IsRateLimit)
				{
					num = Math.Max(num, GetWeeklyReportRequestIntervalMs());
				}
				if (apiCallResult?.RetryAfterSeconds != null)
				{
					num = Math.Max(num, apiCallResult.RetryAfterSeconds.Value * 1000);
				}
				await Task.Delay(num);
			}
		}
		return false;
	}

	private async Task ProcessWeekZeroShortSummaryQueueAsync()
	{
		try
		{
			while (true)
			{
				WeekZeroShortSummaryRequest weekZeroShortSummaryRequest = null;
				lock (_weekZeroShortSummaryQueueLock)
				{
					SortWeekZeroShortSummaryPendingQueue();
					if (_weekZeroShortSummaryPendingQueue.Count > 0)
					{
						weekZeroShortSummaryRequest = _weekZeroShortSummaryPendingQueue[0];
						_weekZeroShortSummaryPendingQueue.RemoveAt(0);
					}
				}
				if (weekZeroShortSummaryRequest == null)
				{
					break;
				}
				try
				{
					await GenerateWeekZeroShortSummaryWithRetriesAsync(weekZeroShortSummaryRequest);
				}
				catch (Exception ex)
				{
					Logger.Log("EventWeeklyReport", "[Week0Short][ERROR] " + ex.Message);
				}
				finally
				{
					lock (_weekZeroShortSummaryQueueLock)
					{
						_weekZeroShortSummaryGenerationInFlight.Remove((weekZeroShortSummaryRequest.GenerationKey ?? "").Trim());
					}
				}
			}
		}
		finally
		{
			lock (_weekZeroShortSummaryQueueLock)
			{
				_weekZeroShortSummaryQueueProcessing = false;
				if (_weekZeroShortSummaryPendingQueue.Count > 0)
				{
					_weekZeroShortSummaryQueueProcessing = true;
					_ = ProcessWeekZeroShortSummaryQueueAsync();
				}
			}
		}
	}

	private void TryQueueWeekZeroShortSummaryGeneration(EventRecordEntry entry, string sourceHash)
	{
		if (entry == null)
		{
			return;
		}
		string text = (sourceHash ?? "").Trim();
		string text2 = (entry.EventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || HasWeekZeroLlmShortSummary(entry, text))
		{
			return;
		}
		if (GetCurrentGameDayIndexSafe() < 7)
		{
			return;
		}
		string text3 = text2 + "|" + text;
		lock (_weekZeroShortSummaryQueueLock)
		{
			if (_weekZeroShortSummaryGenerationInFlight.Contains(text3) || _weekZeroShortSummaryGenerationAttempted.Contains(text3))
			{
				return;
			}
			_weekZeroShortSummaryGenerationInFlight.Add(text3);
			_weekZeroShortSummaryGenerationAttempted.Add(text3);
			_weekZeroShortSummaryPendingQueue.Add(new WeekZeroShortSummaryRequest
			{
				EventId = text2,
				EventKind = (entry.EventKind ?? "").Trim(),
				KingdomId = (entry.ScopeKingdomId ?? "").Trim(),
				Title = (entry.Title ?? "").Trim(),
				Summary = (entry.Summary ?? "").Trim(),
				SourceHash = text,
				GenerationKey = text3
			});
			SortWeekZeroShortSummaryPendingQueue();
		}
		EnsureWeekZeroShortSummaryQueueWorker();
	}

	private void UpsertWeekZeroOpeningSummaryEvent(string eventKind, string kingdomId, string title, string summary, string materialLabel, string materialType)
	{
		string text = (summary ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (_eventRecordEntries == null)
		{
			_eventRecordEntries = new List<EventRecordEntry>();
		}
		string text2 = "weekly_report:" + (eventKind ?? "").Trim().ToLowerInvariant() + ":0:" + ((kingdomId ?? "").Trim());
		EventRecordEntry eventRecordEntry = _eventRecordEntries.FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
		if (eventRecordEntry == null)
		{
			eventRecordEntry = new EventRecordEntry
			{
				EventId = text2
			};
			_eventRecordEntries.Add(eventRecordEntry);
		}
		eventRecordEntry.EventKind = (eventKind ?? "").Trim();
		eventRecordEntry.ScopeKingdomId = (kingdomId ?? "").Trim();
		eventRecordEntry.WeekIndex = 0;
		eventRecordEntry.Title = (title ?? "").Trim();
		string text3 = ComputeWeekZeroShortSummarySourceHash(text);
		bool flag = HasWeekZeroLlmShortSummary(eventRecordEntry, text3) && string.Equals((eventRecordEntry.Summary ?? "").Trim(), text, StringComparison.Ordinal);
		if (!flag)
		{
			eventRecordEntry.ShortSummary = BuildFallbackWeeklyReportShortSummary(text);
			eventRecordEntry.PromptText = BuildWeekZeroPromptText(text3, llmGenerated: false);
		}
		eventRecordEntry.Summary = text;
		eventRecordEntry.TagText = "";
		eventRecordEntry.CreatedDay = 0;
		eventRecordEntry.CreatedDate = "第 0 日";
		eventRecordEntry.Materials = new List<EventMaterialReference>
		{
			new EventMaterialReference
			{
				MaterialType = materialType,
				Label = materialLabel,
				SnapshotText = text,
				KingdomId = (kingdomId ?? "").Trim()
			}
		};
		_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
		TryQueueWeekZeroShortSummaryGeneration(eventRecordEntry, text3);
	}

	private void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		try
		{
			string settlementDisplayName = GetSettlementDisplayName(settlement);
			string siegeAftermathLabel = GetSiegeAftermathLabel(aftermathType);
			string kingdomId = GetKingdomId(attackerParty?.MapFaction);
			string text = GetHeroDisplayName(attackerParty?.LeaderHero) + "在攻取" + settlementDisplayName + "后选择了" + siegeAftermathLabel + "。";
			string text2 = GetClanDisplayName(previousSettlementOwner);
			string kingdomDisplayName = GetKingdomDisplayName(previousSettlementOwner?.Kingdom, "原所属王国");
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text += " 该地此前由" + text2 + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			RecordEventSourceMaterial("siege_aftermath", "围城后处理 - " + settlementDisplayName, text, "siege_aftermath:" + GetSettlementId(settlement) + ":" + aftermathType + ":" + GetHeroId(attackerParty?.LeaderHero), kingdomId, GetSettlementId(settlement), includeInWorld: settlement?.IsTown == true && aftermathType != SiegeAftermathAction.SiegeAftermath.ShowMercy, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnSiegeAftermathApplied: " + ex.Message);
		}
	}

	private void OnVillageBeingRaided(Village village)
	{
		try
		{
			Settlement settlement = village?.Settlement;
			string settlementDisplayName = GetSettlementDisplayName(settlement);
			string text = settlementDisplayName + "村庄正在遭到掠夺。";
			string clanDisplayName = GetClanDisplayName(settlement?.OwnerClan);
			string kingdomDisplayName = GetKingdomDisplayName(settlement?.MapFaction as Kingdom, "所属王国");
			if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text += " 该地由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			RecordEventSourceMaterial("village_raid_started", "掠夺开始 - " + settlementDisplayName + "村庄", text, "village_raid_started:" + GetSettlementId(settlement), GetKingdomId(settlement?.MapFaction), GetSettlementId(settlement), includeInWorld: false, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnVillageBeingRaided: " + ex.Message);
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		try
		{
			Settlement settlement = raidEvent?.MapEventSettlement;
			string settlementDisplayName = GetSettlementDisplayName(settlement);
			string text = settlementDisplayName + "村庄的掠夺结果是：" + GetRaidOutcomeLabel(winnerSide) + "。";
			Hero hero = raidEvent?.AttackerSide?.LeaderParty?.LeaderHero;
			if (hero != null)
			{
				text = text + " 发起掠夺的一方领袖是" + GetHeroDisplayName(hero) + "。";
			}
			RecordEventSourceMaterial("raid_completed", "掠夺结果 - " + settlementDisplayName + "村庄", text, "raid_completed:" + GetSettlementId(settlement) + ":" + winnerSide, GetKingdomId(settlement?.MapFaction), GetSettlementId(settlement), includeInWorld: false, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnRaidCompleted: " + ex.Message);
		}
	}

	private void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		try
		{
			string kingdomDisplayName = GetKingdomDisplayName(destroyedKingdom, "某个王国");
			string text = kingdomDisplayName + "已经覆灭。";
			RecordEventSourceMaterial("kingdom_destroyed", "王国覆灭 - " + kingdomDisplayName, text, "kingdom_destroyed:" + GetKingdomId(destroyedKingdom), GetKingdomId(destroyedKingdom), "", includeInWorld: true, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnKingdomDestroyed: " + ex.Message);
		}
	}

	private void OnClanDestroyed(Clan destroyedClan)
	{
		try
		{
			string clanDisplayName = GetClanDisplayName(destroyedClan);
			string kingdomId = GetKingdomId(destroyedClan?.Kingdom);
			string text = clanDisplayName + "家族已经覆灭。";
			bool includeInWorld = destroyedClan != null && destroyedClan == destroyedClan.Kingdom?.RulingClan;
			RecordEventSourceMaterial("clan_destroyed", "家族覆灭 - " + clanDisplayName, text, "clan_destroyed:" + GetClanId(destroyedClan), kingdomId, "", includeInWorld, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnClanDestroyed: " + ex.Message);
		}
	}

	private static bool ShouldTrackNpcActionHero(Hero hero)
	{
		return hero != null && hero != Hero.MainHero && hero.IsLord && !string.IsNullOrWhiteSpace(hero.StringId);
	}

	private static List<Hero> GetTrackedLordsForClan(Clan clan)
	{
		List<Hero> list = new List<Hero>();
		if (clan == null)
		{
			return list;
		}
		try
		{
			foreach (Hero lord in clan.Heroes)
			{
				if (ShouldTrackNpcActionHero(lord) && !list.Any((Hero x) => string.Equals(x.StringId, lord.StringId, StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(lord);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static string GetHeroDisplayName(Hero hero)
	{
		string text = (hero?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "某位领主" : text;
	}

	private static string GetClanDisplayName(Clan clan)
	{
		string text = (clan?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "某个" : text;
	}

	private static string GetKingdomDisplayName(Kingdom kingdom, string fallback = "某个王国")
	{
		string text = (kingdom?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}

	private static string GetSettlementDisplayName(Settlement settlement)
	{
		string text = (settlement?.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "某处定居点";
		}
		if (settlement?.IsTown == true && !text.EndsWith("市", StringComparison.Ordinal))
		{
			return text + "市";
		}
		return text;
	}

	private static string GetSettlementTypeLabel(Settlement settlement)
	{
		if (settlement == null)
		{
			return "";
		}
		if (settlement.IsVillage)
		{
			return "村庄";
		}
		if (settlement.IsTown)
		{
			return "城镇";
		}
		if (settlement.IsCastle)
		{
			return "城堡";
		}
		return "定居点";
	}

	private static string BuildSettlementTypeSuffix(Settlement settlement)
	{
		string text = GetSettlementTypeLabel(settlement);
		return string.IsNullOrWhiteSpace(text) ? "" : ("（" + text + "）");
	}

	private static string BuildOrderedHeroPairStableKey(string prefix, Hero hero1, Hero hero2)
	{
		string text = GetHeroId(hero1);
		string text2 = GetHeroId(hero2);
		if (string.CompareOrdinal(text, text2) > 0)
		{
			string text3 = text;
			text = text2;
			text2 = text3;
		}
		return prefix + ":" + text + ":" + text2;
	}

	private static string BuildClanChangedKingdomStableKey(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail)
	{
		return "clan_changed_kingdom:" + GetClanId(clan) + ":" + GetKingdomId(oldKingdom) + ":" + GetKingdomId(newKingdom) + ":" + detail;
	}

	private static string BuildClanChangedKingdomNarrative(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail)
	{
		string kingdomDisplayName = GetKingdomDisplayName(oldKingdom, "原王国");
		string kingdomDisplayName2 = GetKingdomDisplayName(newKingdom, "新王国");
		switch (detail)
		{
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary:
			return "你所在的" + GetClanDisplayName(clan) + "家族已作为佣兵加入" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom:
			return "你所在的" + GetClanDisplayName(clan) + "家族已正式加入" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection:
			return "你所在的" + GetClanDisplayName(clan) + "家族已背离" + kingdomDisplayName + "，改投" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom:
			return "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + kingdomDisplayName + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion:
			return "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + kingdomDisplayName + "并发动叛乱。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary:
			return "你所在的" + GetClanDisplayName(clan) + "家族已结束在" + kingdomDisplayName + "的佣兵服务。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom:
			return "你所在的" + GetClanDisplayName(clan) + "家族已建立" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction:
			return "由于" + kingdomDisplayName + "覆灭，你所在的" + GetClanDisplayName(clan) + "家族已脱离原王国。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction:
			return "你所在的" + GetClanDisplayName(clan) + "家族因家族灭亡而退出原王国体系。";
		default:
			return "你所在的" + GetClanDisplayName(clan) + "家族发生了王国归属变更。";
		}
	}

	private static string BuildKingdomDecisionStableKey(KingdomDecision decision)
	{
		string text = (decision?.GetType()?.Name ?? "decision").Trim();
		string text2 = GetKingdomId(decision?.Kingdom);
		string text3 = GetClanId(decision?.ProposerClan);
		string text4 = "";
		try
		{
			text4 = decision?.TriggerTime.ToString() ?? "";
		}
		catch
		{
			text4 = "";
		}
		return "kingdom_decision:" + text2 + ":" + text3 + ":" + text + ":" + text4;
	}

	private static string BuildKingdomDecisionNarrative(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved, bool forProposer)
	{
		string text = "";
		try
		{
			text = new KingdomDecisionConcludedLogEntry(decision, chosenOutcome, isPlayerInvolved).GetNotificationText()?.ToString() ?? "";
		}
		catch
		{
			text = "";
		}
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = decision?.GetGeneralTitle()?.ToString() ?? "";
			}
			catch
			{
				text = "";
			}
		}
		string kingdomDisplayName = GetKingdomDisplayName(decision?.Kingdom, "该王国");
		if (string.IsNullOrWhiteSpace(text))
		{
			return (forProposer ? "你推动的" : "你所参与的") + kingdomDisplayName + "王国决议已经得出结果。";
		}
		return (forProposer ? "你推动的" : "你所参与的") + kingdomDisplayName + "王国决议已有结果：" + text;
	}

	private static void ApplyKingdomDecisionSpecificFacts(NpcActionFacts facts, KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		if (facts == null || decision == null)
		{
			return;
		}
		AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
		if (decision is DeclareWarDecision declareWarDecision)
		{
			AddRelatedFactionFacts(facts, declareWarDecision.FactionToDeclareWarOn);
		}
		else if (decision is MakePeaceKingdomDecision makePeaceKingdomDecision)
		{
			AddRelatedFactionFacts(facts, makePeaceKingdomDecision.FactionToMakePeaceWith);
		}
		else if (decision is SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision)
		{
			ApplySettlementFacts(facts, settlementClaimantPreliminaryDecision.Settlement);
			AddUniqueId(facts.RelatedClanIds, GetClanId(settlementClaimantPreliminaryDecision.Settlement?.OwnerClan));
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(settlementClaimantPreliminaryDecision.Settlement?.MapFaction));
		}
		if (chosenOutcome is KingSelectionKingdomDecision.KingSelectionDecisionOutcome kingSelectionDecisionOutcome)
		{
			ApplyTargetFacts(facts, kingSelectionDecisionOutcome.King);
		}
	}

	private static string BuildSettlementOwnerChangedStableKey(Settlement settlement, Hero newOwner, Hero oldOwner, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		return "settlement_owner_changed:" + GetSettlementId(settlement) + ":" + GetHeroId(newOwner) + ":" + GetHeroId(oldOwner) + ":" + detail;
	}

	private static string GetSettlementOwnerChangeDetailLabel(ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		switch (detail)
		{
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege:
			return "围城";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter:
			return "易物";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByLeaveFaction:
			return "脱离王国";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByKingDecision:
			return "王国决议";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByGift:
			return "赠与";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion:
			return "叛乱";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByClanDestruction:
			return "家族覆灭";
		default:
			return "常规变更";
		}
	}

	private static string GetHeroKilledVerb(KillCharacterAction.KillCharacterActionDetail detail)
	{
		switch (detail)
		{
		case KillCharacterAction.KillCharacterActionDetail.Murdered:
			return "谋杀了";
		case KillCharacterAction.KillCharacterActionDetail.DiedInLabor:
			return "间接导致分娩中失去了";
		case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
			return "见证了";
		case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
			return "在战场上杀死了";
		case KillCharacterAction.KillCharacterActionDetail.WoundedInBattle:
			return "在战斗中重创并导致死亡的是";
		case KillCharacterAction.KillCharacterActionDetail.Executed:
		case KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent:
			return "处决了";
		default:
			return "使其死亡：";
		}
	}

	private static string BuildPrisonerTakenStableKey(Hero capturerHero, Hero prisoner)
	{
		return "prisoner_taken:" + GetHeroId(capturerHero) + ":" + GetHeroId(prisoner);
	}

	private static string BuildPrisonerReleasedStableKey(Hero capturerHero, Hero prisoner, EndCaptivityDetail detail)
	{
		return "prisoner_released:" + GetHeroId(capturerHero) + ":" + GetHeroId(prisoner) + ":" + detail;
	}

	private static string GetEndCaptivityDetailLabel(EndCaptivityDetail detail)
	{
		switch (detail)
		{
		case EndCaptivityDetail.Ransom:
			return "通过赎金获释";
		case EndCaptivityDetail.ReleasedByChoice:
			return "被主动释放";
		case EndCaptivityDetail.ReleasedAfterPeace:
			return "因议和获释";
		case EndCaptivityDetail.ReleasedAfterEscape:
			return "成功逃脱";
		case EndCaptivityDetail.ReleasedAfterBattle:
			return "在战后获释";
		case EndCaptivityDetail.ReleasedByCompensation:
			return "在补偿后获释";
		case EndCaptivityDetail.Death:
			return "在囚禁中死亡";
		default:
			return "脱离囚禁";
		}
	}

	private static string GetSiegeAftermathLabel(SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			return "毁灭";
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			return "劫掠";
		case SiegeAftermathAction.SiegeAftermath.ShowMercy:
			return "宽恕";
		default:
			return aftermathType.ToString();
		}
	}

	private static string GetBattleSideLabel(BattleSideEnum side)
	{
		return side switch
		{
			BattleSideEnum.Attacker => "进攻方",
			BattleSideEnum.Defender => "防守方",
			_ => side.ToString()
		};
	}

	private static string GetRaidOutcomeLabel(BattleSideEnum side)
	{
		return side switch
		{
			BattleSideEnum.Attacker => "掠夺成功",
			BattleSideEnum.Defender => "掠夺被击退",
			_ => "掠夺中止"
		};
	}

	private static string BuildKingdomDecisionSupporterSummary(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		if (decision == null || chosenOutcome == null)
		{
			return "";
		}
		string text = "";
		try
		{
			text = chosenOutcome.GetDecisionTitle()?.ToString() ?? "";
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = chosenOutcome.GetDecisionDescription()?.ToString() ?? "";
			}
			catch
			{
				text = "";
			}
		}
		List<string> list = new List<string>();
		foreach (Supporter supporter in chosenOutcome.SupporterList ?? new List<Supporter>())
		{
			if (supporter?.Clan?.Leader == null)
			{
				continue;
			}
			list.Add(GetHeroDisplayName(supporter.Clan.Leader) + "（" + GetSupportWeightLabel(supporter.SupportWeight) + "）");
		}
		if (list.Count == 0)
		{
			return "";
		}
		return GetKingdomDisplayName(decision.Kingdom, "该王国") + "的决议“" + (string.IsNullOrWhiteSpace(text) ? "本次决议结果" : text.Trim()) + "”最终得到这些支持者表态：" + string.Join("；", list) + "。";
	}

	private static string GetSupportWeightLabel(Supporter.SupportWeights supportWeight)
	{
		switch (supportWeight)
		{
		case Supporter.SupportWeights.SlightlyFavor:
			return "轻度支持";
		case Supporter.SupportWeights.StronglyFavor:
			return "强力支持";
		case Supporter.SupportWeights.FullyPush:
			return "全力推动";
		case Supporter.SupportWeights.StayNeutral:
			return "中立";
		case Supporter.SupportWeights.Choose:
			return "选择";
		default:
			return supportWeight.ToString();
		}
	}

	private static bool ShouldMentionBattleHero(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		if (hero == Hero.MainHero)
		{
			return true;
		}
		return hero.IsLord && !string.IsNullOrWhiteSpace(hero.StringId);
	}

	private static string GetNpcActionHeroKey(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static int GetCurrentGameDayIndexSafe()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static string GetCurrentGameDateTextSafe()
	{
		try
		{
			string text = CampaignTime.Now.ToString();
			return string.IsNullOrWhiteSpace(text) ? ("第 " + GetCurrentGameDayIndexSafe() + " 日") : text.Trim();
		}
		catch
		{
			return "第 " + GetCurrentGameDayIndexSafe() + " 日";
		}
	}

	private static string NormalizeNpcActionStableKey(string stableKey, string fallbackText)
	{
		string text = (stableKey ?? fallbackText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return string.IsNullOrWhiteSpace(text) ? "" : text.ToLowerInvariant();
	}

	private static string GetClanId(Clan clan)
	{
		return (clan?.StringId ?? "").Trim();
	}

	private static string GetKingdomId(Kingdom kingdom)
	{
		return (kingdom?.StringId ?? "").Trim();
	}

	private static string GetKingdomId(IFaction faction)
	{
		if (faction is Kingdom kingdom)
		{
			return GetKingdomId(kingdom);
		}
		return "";
	}

	private static string GetHeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static string GetSettlementId(Settlement settlement)
	{
		return (settlement?.StringId ?? "").Trim();
	}

	private static int ClampKingdomStabilityValue(int value)
	{
		return Math.Max(KingdomStabilityMinValue, Math.Min(KingdomStabilityMaxValue, value));
	}

	private int GetKingdomStabilityValue(Kingdom kingdom)
	{
		return GetKingdomStabilityValue(GetKingdomId(kingdom));
	}

	private int GetKingdomStabilityValue(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return KingdomStabilityDefaultValue;
		}
		if (_kingdomStabilityValues == null)
		{
			_kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_kingdomStabilityValues.TryGetValue(text, out var value))
		{
			return ClampKingdomStabilityValue(value);
		}
		return KingdomStabilityDefaultValue;
	}

	private void SetKingdomStabilityValue(Kingdom kingdom, int value)
	{
		string text = GetKingdomId(kingdom);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (_kingdomStabilityValues == null)
		{
			_kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_kingdomStabilityValues[text] = ClampKingdomStabilityValue(value);
		ApplyKingdomStabilityRelationAdjustmentsForKingdom(kingdom);
	}

	private static int GetKingdomStabilityRelationTargetOffset(int stabilityValue)
	{
		return MBMath.ClampInt((ClampKingdomStabilityValue(stabilityValue) - KingdomStabilityDefaultValue) / 2, -25, 25);
	}

	private static KingdomStabilityTier GetKingdomStabilityTier(int value)
	{
		int num = ClampKingdomStabilityValue(value);
		if (num >= 90)
		{
			return KingdomStabilityTier.ExtremelyHigh;
		}
		if (num >= 75)
		{
			return KingdomStabilityTier.High;
		}
		if (num >= 60)
		{
			return KingdomStabilityTier.FairlyHigh;
		}
		if (num >= 40)
		{
			return KingdomStabilityTier.Average;
		}
		if (num >= 25)
		{
			return KingdomStabilityTier.Poor;
		}
		if (num >= 10)
		{
			return KingdomStabilityTier.VeryPoor;
		}
		return KingdomStabilityTier.ExtremelyPoor;
	}

	private static string GetKingdomStabilityTierText(int value)
	{
		switch (GetKingdomStabilityTier(value))
		{
		case KingdomStabilityTier.ExtremelyHigh:
			return "极高";
		case KingdomStabilityTier.High:
			return "高";
		case KingdomStabilityTier.FairlyHigh:
			return "较高";
		case KingdomStabilityTier.Average:
			return "一般";
		case KingdomStabilityTier.Poor:
			return "较差";
		case KingdomStabilityTier.VeryPoor:
			return "很差";
		default:
			return "极差";
		}
	}

	private static string FormatKingdomStabilityRelationOffsetText(int offset)
	{
		if (offset > 0)
		{
			return "+" + offset;
		}
		return offset.ToString();
	}

	private static bool ShouldApplyKingdomStabilityRelationAdjustmentToClan(Clan clan, Kingdom kingdom)
	{
		return clan != null && kingdom != null && clan.Kingdom == kingdom && clan != kingdom.RulingClan && !clan.IsEliminated && !clan.IsUnderMercenaryService && !clan.IsClanTypeMercenary;
	}

	private static IEnumerable<Hero> GetClanHeroesForKingdomStabilityRelationAdjustment(Clan clan)
	{
		if (clan?.Heroes == null)
		{
			return Enumerable.Empty<Hero>();
		}
		return clan.Heroes.Where((Hero x) => x != null && x.IsAlive && !x.IsChild).Distinct();
	}

	private static string BuildKingdomStabilityRelationOffsetKey(string kingdomId, Hero sourceHero, Hero targetHero)
	{
		string text = (kingdomId ?? "").Trim();
		string heroId = GetHeroId(sourceHero);
		string heroId2 = GetHeroId(targetHero);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(heroId2))
		{
			return "";
		}
		return text + "|" + heroId + "|" + heroId2;
	}

	private static bool TryResolveKingdomStabilityRelationOffsetKey(string key, out string kingdomId, out Hero sourceHero, out Hero targetHero)
	{
		kingdomId = "";
		sourceHero = null;
		targetHero = null;
		string[] array = (key ?? "").Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 3)
		{
			return false;
		}
		kingdomId = (array[0] ?? "").Trim();
		sourceHero = FindHeroById(array[1]);
		targetHero = FindHeroById(array[2]);
		return !string.IsNullOrWhiteSpace(kingdomId);
	}

	private static int ClampHeroRelationValue(int value)
	{
		return MBMath.ClampInt(value, -100, 100);
	}

	private int ApplyKingdomStabilityRelationOffsetToPair(string pairKey, Hero sourceHero, Hero targetHero, int desiredOffset)
	{
		if (_kingdomStabilityRelationAppliedOffsets == null)
		{
			_kingdomStabilityRelationAppliedOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (string.IsNullOrWhiteSpace(pairKey))
		{
			return 0;
		}
		int value = 0;
		_kingdomStabilityRelationAppliedOffsets.TryGetValue(pairKey, out value);
		if (sourceHero == null || targetHero == null || sourceHero == targetHero)
		{
			return value;
		}
		int heroRelation = CharacterRelationManager.GetHeroRelation(sourceHero, targetHero);
		int num = heroRelation - value;
		int num2 = ClampHeroRelationValue(num + desiredOffset);
		if (num2 != heroRelation)
		{
			sourceHero.SetPersonalRelation(targetHero, num2);
		}
		return num2 - num;
	}

	private void ApplyKingdomStabilityRelationAdjustmentsForKingdom(Kingdom kingdom)
	{
		if (_kingdomStabilityRelationAppliedOffsets == null)
		{
			_kingdomStabilityRelationAppliedOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		string kingdomId = GetKingdomId(kingdom);
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Hero hero = kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		int kingdomStabilityRelationTargetOffset = GetKingdomStabilityRelationTargetOffset(GetKingdomStabilityValue(kingdom));
		if (kingdom != null && !kingdom.IsEliminated && hero != null && kingdomStabilityRelationTargetOffset != 0 && kingdom.Clans != null)
		{
			foreach (Clan clan in kingdom.Clans)
			{
				if (!ShouldApplyKingdomStabilityRelationAdjustmentToClan(clan, kingdom))
				{
					continue;
				}
				foreach (Hero item in GetClanHeroesForKingdomStabilityRelationAdjustment(clan))
				{
					string text = BuildKingdomStabilityRelationOffsetKey(kingdomId, hero, item);
					if (!string.IsNullOrWhiteSpace(text))
					{
						dictionary[text] = kingdomStabilityRelationTargetOffset;
					}
				}
			}
		}
		List<string> list = _kingdomStabilityRelationAppliedOffsets.Keys.Where((string x) => !string.IsNullOrWhiteSpace(x) && x.StartsWith(kingdomId + "|", StringComparison.OrdinalIgnoreCase)).ToList();
		foreach (string item2 in list)
		{
			int desiredOffset = 0;
			dictionary.TryGetValue(item2, out desiredOffset);
			if (!TryResolveKingdomStabilityRelationOffsetKey(item2, out var _, out var sourceHero, out var targetHero))
			{
				_kingdomStabilityRelationAppliedOffsets.Remove(item2);
				continue;
			}
			int num = ApplyKingdomStabilityRelationOffsetToPair(item2, sourceHero, targetHero, desiredOffset);
			if (num == 0)
			{
				_kingdomStabilityRelationAppliedOffsets.Remove(item2);
			}
			else
			{
				_kingdomStabilityRelationAppliedOffsets[item2] = num;
			}
		}
		foreach (KeyValuePair<string, int> item3 in dictionary)
		{
			if (list.Contains(item3.Key))
			{
				continue;
			}
			if (!TryResolveKingdomStabilityRelationOffsetKey(item3.Key, out var _, out var sourceHero2, out var targetHero2))
			{
				continue;
			}
			int num2 = ApplyKingdomStabilityRelationOffsetToPair(item3.Key, sourceHero2, targetHero2, item3.Value);
			if (num2 != 0)
			{
				_kingdomStabilityRelationAppliedOffsets[item3.Key] = num2;
			}
		}
	}

	private void ApplyKingdomStabilityRelationAdjustments()
	{
		if (Kingdom.All == null || Kingdom.All.Count == 0)
		{
			return;
		}
		foreach (Kingdom item in Kingdom.All.Where((Kingdom x) => x != null))
		{
			ApplyKingdomStabilityRelationAdjustmentsForKingdom(item);
		}
	}

	private static int GetLowClanCountRoyalDomainLoyaltyAdjustment(int stabilityValue, int activeClanCount)
	{
		int num = Math.Max(0, activeClanCount);
		switch (GetKingdomStabilityTier(stabilityValue))
		{
		case KingdomStabilityTier.ExtremelyHigh:
			switch (num)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				return 9;
			case 4:
				return 6;
			case 5:
				return 3;
			case 6:
				return 2;
			case 7:
				return 1;
			default:
				return 0;
			}
		case KingdomStabilityTier.High:
			switch (num)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				return 6;
			case 4:
				return 4;
			case 5:
				return 2;
			case 6:
				return 1;
			default:
				return 0;
			}
		case KingdomStabilityTier.FairlyHigh:
			switch (num)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				return 3;
			case 4:
				return 2;
			case 5:
				return 1;
			default:
				return 0;
			}
		case KingdomStabilityTier.Poor:
			switch (num)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				return -3;
			case 4:
				return -2;
			case 5:
				return -1;
			default:
				return 0;
			}
		case KingdomStabilityTier.VeryPoor:
			switch (num)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				return -6;
			case 4:
				return -4;
			case 5:
				return -2;
			case 6:
				return -1;
			default:
				return 0;
			}
		case KingdomStabilityTier.ExtremelyPoor:
			switch (num)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				return -9;
			case 4:
				return -6;
			case 5:
				return -3;
			case 6:
				return -2;
			case 7:
				return -1;
			default:
				return 0;
			}
		default:
			return 0;
		}
	}

	private static int CountActiveKingdomClansForLowClanCountRule(Kingdom kingdom)
	{
		if (kingdom?.Clans == null)
		{
			return 0;
		}
		return kingdom.Clans.Count((Clan x) => x != null && !x.IsEliminated && !x.IsUnderMercenaryService && !x.IsClanTypeMercenary);
	}

	private static void SyncTownRebelliousStateFromCurrentLoyalty(Town town)
	{
		if (town?.Settlement == null)
		{
			return;
		}
		int rebelliousStateStartLoyaltyThreshold = Campaign.Current?.Models?.SettlementLoyaltyModel?.RebelliousStateStartLoyaltyThreshold ?? 25;
		bool inRebelliousState = town.InRebelliousState;
		town.InRebelliousState = town.Loyalty <= (float)rebelliousStateStartLoyaltyThreshold;
		if (inRebelliousState != town.InRebelliousState)
		{
			CampaignEventDispatcher.Instance.TownRebelliousStateChanged(town, town.InRebelliousState);
		}
	}

	private void ApplyRulingClanSettlementLoyaltyAdjustmentForLowClanCountKingdom(Town town)
	{
		if (town?.Settlement == null || !town.Settlement.IsFortification)
		{
			return;
		}
		Clan ownerClan = town.Settlement.OwnerClan;
		Kingdom kingdom = ownerClan?.Kingdom;
		if (ownerClan == null || kingdom == null || kingdom.IsEliminated || ownerClan != kingdom.RulingClan)
		{
			return;
		}
		int num = CountActiveKingdomClansForLowClanCountRule(kingdom);
		if (num > 7)
		{
			return;
		}
		int lowClanCountRoyalDomainLoyaltyAdjustment = GetLowClanCountRoyalDomainLoyaltyAdjustment(GetKingdomStabilityValue(kingdom), num);
		if (lowClanCountRoyalDomainLoyaltyAdjustment == 0)
		{
			return;
		}
		float maximumLoyaltyInSettlement = Campaign.Current?.Models?.SettlementLoyaltyModel?.MaximumLoyaltyInSettlement ?? 100;
		town.Loyalty = MBMath.ClampFloat(town.Loyalty + (float)lowClanCountRoyalDomainLoyaltyAdjustment, 0f, maximumLoyaltyInSettlement);
		SyncTownRebelliousStateFromCurrentLoyalty(town);
	}

	private static float GetKingdomRebellionWeeklyChance(int stabilityValue)
	{
		switch (GetKingdomStabilityTier(stabilityValue))
		{
		case KingdomStabilityTier.Poor:
			return 0.005f;
		case KingdomStabilityTier.VeryPoor:
			return 0.05f;
		case KingdomStabilityTier.ExtremelyPoor:
			return 0.25f;
		default:
			return 0f;
		}
	}

	private static string FormatKingdomRebellionChance(float chance)
	{
		float num = Math.Max(0f, chance) * 100f;
		return num.ToString((num < 1f) ? "0.0##" : "0.##") + "%";
	}

	private static int GetKingdomStabilityWeeklyBalancingDelta(int stabilityValue)
	{
		switch (GetKingdomStabilityTier(stabilityValue))
		{
		case KingdomStabilityTier.ExtremelyHigh:
			return -5;
		case KingdomStabilityTier.High:
			return -3;
		case KingdomStabilityTier.VeryPoor:
			return 3;
		case KingdomStabilityTier.ExtremelyPoor:
			return 5;
		default:
			return 0;
		}
	}

	private static string BuildClanFortificationSummary(Clan clan)
	{
		List<Settlement> list = clan?.Settlements?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).ToList() ?? new List<Settlement>();
		if (list.Count == 0)
		{
			return "";
		}
		List<string> list2 = list.Select(GetSettlementDisplayName).Take(4).ToList();
		string text = string.Join("、", list2);
		if (list.Count > list2.Count)
		{
			text = text + " 等" + list.Count + "处";
		}
		return GetClanDisplayName(clan) + "家族当前掌握的核心封地包括：" + text + "。";
	}

	private static string BuildRebelKingdomFallbackFormalName(Clan clan)
	{
		Settlement settlement = clan?.Settlements?.FirstOrDefault((Settlement x) => x != null && x.IsTown) ?? clan?.Settlements?.FirstOrDefault((Settlement x) => x != null && x.IsCastle);
		string text = (settlement?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text + "自由邦";
		}
		string clanDisplayName = GetClanDisplayName(clan);
		if (!string.IsNullOrWhiteSpace(clanDisplayName) && clanDisplayName != "某个")
		{
			return clanDisplayName + "新邦";
		}
		return "新兴自由邦";
	}

	private static string BuildRebelKingdomFallbackShortName(string formalName, Clan clan)
	{
		string text = (formalName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && text.Length <= 10)
		{
			return text;
		}
		Settlement settlement = clan?.Settlements?.FirstOrDefault((Settlement x) => x != null && x.IsTown) ?? clan?.Settlements?.FirstOrDefault((Settlement x) => x != null && x.IsCastle);
		string text2 = (settlement?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		string clanDisplayName = GetClanDisplayName(clan);
		if (!string.IsNullOrWhiteSpace(clanDisplayName) && clanDisplayName != "某个")
		{
			return clanDisplayName;
		}
		return "新邦";
	}

	private static string BuildRebelKingdomFallbackLoreText(Clan clan, Kingdom oldKingdom, string formalName, int weekIndex)
	{
		string clanDisplayName = GetClanDisplayName(clan);
		string heroDisplayName = GetHeroDisplayName(clan?.Leader);
		string kingdomDisplayName = GetKingdomDisplayName(oldKingdom, "原王国");
		string text = string.IsNullOrWhiteSpace(formalName) ? "这一新生政权" : formalName.Trim();
		return text + "建立于第" + Math.Max(0, weekIndex) + "周，由" + heroDisplayName + "统领的" + clanDisplayName + "家族在脱离" + kingdomDisplayName + "后组建，宣称要以自身控制的封地为核心重整地方秩序。";
	}

	private static string NormalizeRebelPromptSourceText(string text, int maxLength = 1200)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		if (text2.Length > maxLength)
		{
			text2 = text2.Substring(0, maxLength).TrimEnd();
		}
		return text2.Trim();
	}

	private string BuildHeroBackgroundForRebelNamingPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "无";
		}
		string text = NormalizeRebelPromptSourceText(hero.EncyclopediaText?.ToString() ?? "", 900);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string heroDisplayName = GetHeroDisplayName(hero);
		string clanDisplayName = GetClanDisplayName(hero.Clan);
		string text2 = (hero.Culture?.Name?.ToString() ?? "").Trim();
		string value = string.IsNullOrWhiteSpace(text2) ? "" : (text2 + "文化");
		return NormalizeRebelPromptSourceText(heroDisplayName + "出身于" + clanDisplayName + "家族，现为" + value + "背景的领主。", 300);
	}

	private string BuildKingdomBackgroundForRebelNamingPrompt(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "无";
		}
		string text = NormalizeRebelPromptSourceText(kingdom.EncyclopediaText?.ToString() ?? "", 1200);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string kingdomOpeningSummary = GetKingdomOpeningSummary(kingdom);
		text = NormalizeRebelPromptSourceText(kingdomOpeningSummary, 1200);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return GetKingdomDisplayName(kingdom, "该王国") + "是当前叛乱所脱离的旧政权。";
	}

	private string BuildSettlementBackgroundForRebelNamingPrompt(Settlement settlement)
	{
		if (settlement == null)
		{
			return "无";
		}
		string text = NormalizeRebelPromptSourceText(settlement.EncyclopediaText?.ToString() ?? "", 600);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string settlementDisplayName = GetSettlementDisplayName(settlement);
		string text2 = settlement.IsTown ? "城镇" : (settlement.IsCastle ? "城堡" : "定居点");
		string text3 = (settlement.Culture?.Name?.ToString() ?? "").Trim();
		string kingdomDisplayName = GetKingdomDisplayName(settlement.MapFaction as Kingdom, "无明确王国归属");
		return NormalizeRebelPromptSourceText(settlementDisplayName + "是一处" + text2 + "，文化为" + (string.IsNullOrWhiteSpace(text3) ? "未知" : text3) + "，当前归属于" + kingdomDisplayName + "。", 220);
	}

	private string BuildRebelSettlementSummaryForNamingPrompt(IEnumerable<Settlement> settlements)
	{
		if (settlements == null)
		{
			return "无";
		}
		List<string> list = new List<string>();
		foreach (Settlement settlement in settlements.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).Take(4))
		{
			string settlementDisplayName = GetSettlementDisplayName(settlement);
			string settlementBackgroundForRebelNamingPrompt = BuildSettlementBackgroundForRebelNamingPrompt(settlement);
			list.Add("- " + settlementDisplayName + "：" + settlementBackgroundForRebelNamingPrompt);
		}
		if (list.Count == 0)
		{
			return "无";
		}
		return string.Join("\n", list);
	}

	private string BuildRebelBackgroundForNamingPrompt(Kingdom oldKingdom, int weekIndex)
	{
		EventRecordEntry weeklyReportRecordByWeek = FindWeeklyReportRecordByWeek("kingdom", GetKingdomId(oldKingdom), weekIndex - 1);
		string text = NormalizeRebelPromptSourceText(weeklyReportRecordByWeek?.ShortSummary ?? "", 320);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = NormalizeRebelPromptSourceText(weeklyReportRecordByWeek?.Summary ?? "", 420);
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "叛乱背景：上周" + GetKingdomDisplayName(oldKingdom, "原王国") + "周报提到，" + text + "。这场叛乱正是在这样的局势中爆发，主导家族带着现有封地脱离旧王国，准备自立为新的政治实体。";
		}
		return "叛乱背景：该家族因与旧王国统治层关系恶化，在王国内部稳定度恶化时带着现有封地脱离旧王国，并准备自立为新的政治实体。";
	}

	private EventRecordEntry FindWeeklyReportRecordByWeek(string eventKind, string scopeKingdomId, int weekIndex)
	{
		string text = (eventKind ?? "").Trim();
		string text2 = (scopeKingdomId ?? "").Trim();
		return SanitizeEventRecordEntries(_eventRecordEntries).FirstOrDefault((EventRecordEntry x) => x != null && x.WeekIndex == weekIndex && string.Equals((x.EventKind ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) && string.Equals((x.ScopeKingdomId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildWeeklyReportLeadInForRebelNamingPrompt(EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "无";
		}
		string text = NormalizeRebelPromptSourceText(entry.ShortSummary, 260);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = NormalizeRebelPromptSourceText(entry.Summary, 500);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "无";
		}
		string text2 = NormalizeRebelPromptSourceText(entry.Title, 80);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text2 + "：" + text;
	}

	private static string NormalizeRebelKingdomNameToken(string text, int maxLength)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}
		text2 = text2.Trim().Trim('\"', '\'', '“', '”', '‘', '’', '：', ':', '-', '·');
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		if (text2.Length > maxLength)
		{
			text2 = text2.Substring(0, maxLength).TrimEnd();
		}
		return text2.Trim();
	}

	private static string NormalizeRebelKingdomLoreText(string text)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}
		return text2.Trim();
	}

	private static ClanVisualSnapshot CaptureClanVisualSnapshot(Clan clan)
	{
		if (clan == null)
		{
			return null;
		}
		Banner banner = clan.Banner;
		return new ClanVisualSnapshot
		{
			Banner = ((banner != null) ? new Banner(banner) : null),
			Color = clan.Color,
			Color2 = clan.Color2,
			BackgroundColor = (banner?.GetPrimaryColor() ?? clan.Color),
			IconColor = (banner?.GetFirstIconColor() ?? clan.Color2)
		};
	}

	private static void RestoreClanVisualSnapshot(Clan clan, ClanVisualSnapshot snapshot)
	{
		if (clan == null || snapshot == null)
		{
			return;
		}
		clan.Color = snapshot.Color;
		clan.Color2 = snapshot.Color2;
		if (snapshot.Banner != null)
		{
			clan.Banner = new Banner(snapshot.Banner);
		}
		clan.UpdateBannerColor(snapshot.BackgroundColor, snapshot.IconColor);
	}

	private static void PrepareClanVisualForRebelKingdomCreation(Clan clan, ClanVisualSnapshot snapshot)
	{
		if (clan == null || snapshot == null)
		{
			return;
		}
		uint backgroundColor = snapshot.BackgroundColor;
		uint iconColor = snapshot.IconColor;
		clan.Color = backgroundColor;
		clan.Color2 = iconColor;
		if (snapshot.Banner != null)
		{
			clan.Banner = new Banner(snapshot.Banner, backgroundColor, iconColor);
		}
		clan.UpdateBannerColor(backgroundColor, iconColor);
	}

	private static void MarkClanVisualsDirty(Clan clan)
	{
		if (clan == null)
		{
			return;
		}
		try
		{
			foreach (Settlement settlement in clan.Settlements)
			{
				settlement?.Party?.SetVisualAsDirty();
			}
		}
		catch
		{
		}
		try
		{
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				warPartyComponent?.Party?.SetVisualAsDirty();
			}
		}
		catch
		{
		}
	}

	private static List<uint> GetBannerPaletteColors()
	{
		List<uint> list = new List<uint>();
		HashSet<uint> hashSet = new HashSet<uint>();
		for (int i = 0; i <= 1024; i++)
		{
			uint color = BannerManager.GetColor(i);
			if (color != 3735928559U && hashSet.Add(color))
			{
				list.Add(color);
			}
		}
		return list;
	}

	private static HashSet<uint> CollectUsedFactionPrimaryColors(Kingdom oldKingdom, Clan founderClan)
	{
		HashSet<uint> hashSet = new HashSet<uint>();
		try
		{
			foreach (Kingdom item in Kingdom.All)
			{
				if (item != null && !item.IsEliminated)
				{
					hashSet.Add(item.PrimaryBannerColor);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Clan item2 in Clan.All)
			{
				if (item2 == null || item2 == founderClan || item2.IsEliminated || item2.Kingdom != null)
				{
					continue;
				}
				if (item2.Settlements != null && item2.Settlements.Any((Settlement x) => x != null && (x.IsTown || x.IsCastle)))
				{
					hashSet.Add(item2.Banner?.GetPrimaryColor() ?? item2.Color);
				}
			}
		}
		catch
		{
		}
		if (oldKingdom != null)
		{
			hashSet.Add(oldKingdom.PrimaryBannerColor);
			hashSet.Add(oldKingdom.Color);
		}
		return hashSet;
	}

	private static int ComputeColorDistance(uint colorA, uint colorB)
	{
		int num = (int)((colorA >> 16) & 0xFF);
		int num2 = (int)((colorA >> 8) & 0xFF);
		int num3 = (int)(colorA & 0xFF);
		int num4 = (int)((colorB >> 16) & 0xFF);
		int num5 = (int)((colorB >> 8) & 0xFF);
		int num6 = (int)(colorB & 0xFF);
		return Math.Abs(num - num4) + Math.Abs(num2 - num5) + Math.Abs(num3 - num6);
	}

	private static RebelFactionColorChoice BuildRandomUniqueRebelFactionColors(Clan clan, Kingdom oldKingdom, ClanVisualSnapshot snapshot)
	{
		List<uint> bannerPaletteColors = GetBannerPaletteColors();
		uint num = snapshot?.BackgroundColor ?? 4291609515U;
		uint num2 = snapshot?.IconColor ?? uint.MaxValue;
		if (bannerPaletteColors.Count == 0)
		{
			return new RebelFactionColorChoice
			{
				BackgroundColor = num,
				IconColor = ((num2 != num) ? num2 : 4289374890U),
				Banner = ((snapshot?.Banner != null) ? new Banner(snapshot.Banner, num, (num2 != num) ? num2 : 4289374890U) : null)
			};
		}
		HashSet<uint> usedFactionPrimaryColors = CollectUsedFactionPrimaryColors(oldKingdom, clan);
		List<uint> list = bannerPaletteColors.Where((uint x) => !usedFactionPrimaryColors.Contains(x) && x != num).ToList();
		if (list.Count == 0)
		{
			list = bannerPaletteColors.Where((uint x) => x != num).ToList();
		}
		if (list.Count == 0)
		{
			list = bannerPaletteColors;
		}
		uint backgroundColor = list[MBRandom.RandomInt(list.Count)];
		List<uint> list2 = bannerPaletteColors.Where((uint x) => x != backgroundColor).ToList();
		uint iconColor = num2;
		if (iconColor == backgroundColor || BannerManager.GetColorId(iconColor) < 0 || ComputeColorDistance(backgroundColor, iconColor) < 140)
		{
			iconColor = list2.OrderByDescending((uint x) => ComputeColorDistance(backgroundColor, x)).FirstOrDefault();
			if (iconColor == 0)
			{
				iconColor = list2.FirstOrDefault();
			}
			if (iconColor == 0 || iconColor == backgroundColor)
			{
				iconColor = uint.MaxValue;
			}
		}
		Banner banner = snapshot?.Banner ?? clan?.ClanOriginalBanner ?? clan?.Banner;
		Banner banner2 = ((banner != null) ? new Banner(banner, backgroundColor, iconColor) : Banner.CreateRandomClanBanner((clan?.StringId ?? "new_kingdom").GetDeterministicHashCode()));
		banner2.ChangePrimaryColor(backgroundColor);
		if (iconColor != uint.MaxValue)
		{
			banner2.ChangeIconColors(iconColor);
		}
		return new RebelFactionColorChoice
		{
			BackgroundColor = backgroundColor,
			IconColor = iconColor,
			Banner = banner2
		};
	}

	private static void ApplyRebelFactionColorChoiceToClan(Clan clan, RebelFactionColorChoice colorChoice)
	{
		if (clan == null || colorChoice == null)
		{
			return;
		}
		clan.Color = colorChoice.BackgroundColor;
		clan.Color2 = colorChoice.IconColor;
		if (colorChoice.Banner != null)
		{
			clan.Banner = new Banner(colorChoice.Banner);
		}
		clan.UpdateBannerColor(colorChoice.BackgroundColor, colorChoice.IconColor);
	}

	private static bool IsDuplicateKingdomName(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		try
		{
			return Kingdom.All.Any((Kingdom x) => x != null && string.Equals((x.Name?.ToString() ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	private static string BuildRebelKingdomNamingSystemPrompt()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("你是一名负责为卡拉迪亚叛乱政权命名的史官。");
		stringBuilder.AppendLine("你的任务是根据给定素材，为一个刚刚脱离旧国家的新国家生成国家名称与百科简介。");
		stringBuilder.AppendLine("命名要求：");
		stringBuilder.AppendLine("1. 名称必须符合卡拉迪亚中世纪风格，不要使用现代政治术语。");
		stringBuilder.AppendLine("2. 尽量以原王国名称为基础生成名称。");
		stringBuilder.AppendLine("3. 正式名要自然、庄重、可作为百科词条标题；简称要更短，适合显示。");
		stringBuilder.AppendLine("4. 不要与现有王国重名。");
		stringBuilder.AppendLine("5. 百科简介应像原版百科文本，简洁、客观、概括其建立背景。");
		stringBuilder.AppendLine("6. 只输出固定字段，不要解释。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("输出格式：");
		stringBuilder.AppendLine("[NAME]正式国名");
		stringBuilder.AppendLine("[SHORT]简称");
		stringBuilder.AppendLine("[LORE]百科简介");
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildRebelFollowerSummaryForNamingPrompt(IEnumerable<Clan> followerClans)
	{
		if (followerClans == null)
		{
			return "无";
		}
		List<string> list = new List<string>();
		foreach (Clan followerClan in followerClans)
		{
			if (followerClan == null)
			{
				continue;
			}
			string clanDisplayName = GetClanDisplayName(followerClan);
			string heroDisplayName = GetHeroDisplayName(followerClan.Leader);
			string text = BuildClanFortificationSummary(followerClan);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "暂无可用封地摘要";
			}
			list.Add(clanDisplayName + "（族长：" + heroDisplayName + "；" + text + "）");
		}
		if (list.Count == 0)
		{
			return "无";
		}
		return string.Join("\n", list.Distinct(StringComparer.OrdinalIgnoreCase).Select((string x) => "- " + x));
	}

	private string BuildRebelKingdomNamingUserPrompt(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null)
	{
		List<string> list = new List<string>();
		try
		{
			foreach (Kingdom item in Kingdom.All.Where((Kingdom x) => x != null))
			{
				string text = (item.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		string text2 = string.Join("、", list.Distinct(StringComparer.OrdinalIgnoreCase));
		List<Settlement> list2 = clan?.Settlements?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).ToList() ?? new List<Settlement>();
		string text3 = (list2.Count > 0) ? string.Join("、", list2.Select(GetSettlementDisplayName)) : "无";
		string rebelSettlementSummaryForNamingPrompt = BuildRebelSettlementSummaryForNamingPrompt(list2);
		string rebelFollowerSummaryForNamingPrompt = BuildRebelFollowerSummaryForNamingPrompt(followerClans);
		string heroBackgroundForRebelNamingPrompt = BuildHeroBackgroundForRebelNamingPrompt(clan?.Leader);
		string heroBackgroundForRebelNamingPrompt2 = BuildHeroBackgroundForRebelNamingPrompt(oldKingdom?.Leader);
		string kingdomBackgroundForRebelNamingPrompt = BuildKingdomBackgroundForRebelNamingPrompt(oldKingdom);
		string rebelBackgroundForNamingPrompt = BuildRebelBackgroundForNamingPrompt(oldKingdom, weekIndex);
		EventRecordEntry weeklyReportRecordByWeek = FindWeeklyReportRecordByWeek("world", "", weekIndex - 1);
		EventRecordEntry weeklyReportRecordByWeek2 = FindWeeklyReportRecordByWeek("kingdom", GetKingdomId(oldKingdom), weekIndex - 1);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【叛乱建国命名任务】");
		stringBuilder.AppendLine("当前周次：第 " + Math.Max(0, weekIndex) + " 周");
		stringBuilder.AppendLine("主导家族：" + GetClanDisplayName(clan));
		stringBuilder.AppendLine("主导族长：" + GetHeroDisplayName(clan?.Leader));
		stringBuilder.AppendLine("家族文化：" + ((clan?.Culture?.Name?.ToString() ?? "").Trim()));
		stringBuilder.AppendLine("原所属王国：" + GetKingdomDisplayName(oldKingdom, "原王国"));
		stringBuilder.AppendLine("原所属王国领袖（被背叛者）：" + GetHeroDisplayName(oldKingdom?.Leader));
		stringBuilder.AppendLine("原王国执政家族：" + GetClanDisplayName(oldKingdom?.RulingClan));
		stringBuilder.AppendLine("当前核心封地：" + text3);
		stringBuilder.AppendLine(rebelBackgroundForNamingPrompt);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【叛乱核心定居点百科】");
		stringBuilder.AppendLine(rebelSettlementSummaryForNamingPrompt);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【联合响应家族】");
		stringBuilder.AppendLine(rebelFollowerSummaryForNamingPrompt);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【叛乱家族族长背景】");
		stringBuilder.AppendLine(heroBackgroundForRebelNamingPrompt);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【原王国统治者背景】");
		stringBuilder.AppendLine(heroBackgroundForRebelNamingPrompt2);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【原王国背景】");
		stringBuilder.AppendLine(kingdomBackgroundForRebelNamingPrompt);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【上周世界周报】");
		stringBuilder.AppendLine(BuildWeeklyReportLeadInForRebelNamingPrompt(weeklyReportRecordByWeek));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【上周原王国周报】");
		stringBuilder.AppendLine(BuildWeeklyReportLeadInForRebelNamingPrompt(weeklyReportRecordByWeek2));
		if (!string.IsNullOrWhiteSpace(text2))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("现有王国名称（禁止重名）：" + text2);
		}
		stringBuilder.AppendLine("请生成一个正式名、一个简称，以及一段百科简介。");
		return stringBuilder.ToString().TrimEnd();
	}

	private void BuildRebelKingdomNamingRequest(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans, out string systemPrompt, out string userPrompt, out RebelKingdomNamingResult fallbackResult)
	{
		systemPrompt = BuildRebelKingdomNamingSystemPrompt();
		userPrompt = BuildRebelKingdomNamingUserPrompt(clan, oldKingdom, weekIndex, followerClans);
		fallbackResult = BuildFallbackStaticRebelKingdomNamingResult(clan, oldKingdom, weekIndex, "");
	}

	private static bool TryParseRebelKingdomNamingResponse(string rawResponse, out string formalName, out string shortName, out string encyclopediaText)
	{
		formalName = "";
		shortName = "";
		encyclopediaText = "";
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Match match = Regex.Match(text, "\\[NAME\\](?<name>[\\s\\S]*?)(?=\\[SHORT\\]|\\[LORE\\]|$)", RegexOptions.IgnoreCase);
		Match match2 = Regex.Match(text, "\\[SHORT\\](?<short>[\\s\\S]*?)(?=\\[LORE\\]|$)", RegexOptions.IgnoreCase);
		Match match3 = Regex.Match(text, "\\[LORE\\](?<lore>[\\s\\S]*)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			formalName = NormalizeRebelKingdomNameToken(match.Groups["name"]?.Value ?? "", 24);
		}
		if (match2.Success)
		{
			shortName = NormalizeRebelKingdomNameToken(match2.Groups["short"]?.Value ?? "", 14);
		}
		if (match3.Success)
		{
			encyclopediaText = NormalizeRebelKingdomLoreText(match3.Groups["lore"]?.Value ?? "");
		}
		return !string.IsNullOrWhiteSpace(formalName) && !string.IsNullOrWhiteSpace(shortName) && !string.IsNullOrWhiteSpace(encyclopediaText);
	}

	private static RebelKingdomNamingResult BuildFallbackStaticRebelKingdomNamingResult(Clan clan, Kingdom oldKingdom, int weekIndex, string failureReason)
	{
		string text = BuildRebelKingdomFallbackFormalName(clan);
		if (IsDuplicateKingdomName(text))
		{
			text = text + "（新）";
		}
		string text2 = BuildRebelKingdomFallbackShortName(text, clan);
		string text3 = BuildRebelKingdomFallbackLoreText(clan, oldKingdom, text, weekIndex);
		return new RebelKingdomNamingResult
		{
			FormalName = text,
			ShortName = text2,
			EncyclopediaText = text3,
			UsedFallback = true,
			FailureReason = (failureReason ?? "").Trim(),
			AttemptsUsed = 0
		};
	}

	private const int RebelKingdomNamingTimeoutMs = 60000;

	private RebelKingdomNamingResult GenerateRebelKingdomNamingFromPrompts(string systemPrompt, string userPrompt, RebelKingdomNamingResult fallbackResult, string logTarget, int maxAttempts = 1)
	{
		RebelKingdomNamingResult rebelKingdomNamingResult = fallbackResult ?? new RebelKingdomNamingResult
		{
			FormalName = "新兴自由邦",
			ShortName = "新邦",
			EncyclopediaText = "这一新生政权在地方割据与内乱中形成。",
			UsedFallback = true
		};
		for (int i = 1; i <= Math.Max(1, maxAttempts); i++)
		{
			ApiCallResult apiCallResult = null;
			try
			{
				Task<ApiCallResult> task = CallUniversalApiDetailed(systemPrompt, userPrompt);
				Task task2 = Task.WhenAny(task, Task.Delay(RebelKingdomNamingTimeoutMs)).GetAwaiter().GetResult();
				if (task2 == task)
				{
					apiCallResult = task.GetAwaiter().GetResult();
				}
				else
				{
					apiCallResult = new ApiCallResult
					{
						ErrorMessage = "叛乱命名请求超时（60 秒）。"
					};
				}
			}
			catch (Exception ex)
			{
				apiCallResult = new ApiCallResult
				{
					ErrorMessage = ex.Message
				};
			}
			string text = apiCallResult?.Success == true ? (apiCallResult.Content ?? "") : ("错误: " + (apiCallResult?.ErrorMessage ?? "未知错误"));
			Logger.LogEventPromptExchange((logTarget ?? "叛乱建国命名") + " [尝试 " + i + "/" + maxAttempts + "]", "【System Prompt】\n" + (systemPrompt ?? "") + "\n\n【User Prompt】\n" + (userPrompt ?? ""), text);
			if (apiCallResult?.Success == true && TryParseRebelKingdomNamingResponse(apiCallResult.Content, out var formalName, out var shortName, out var encyclopediaText))
			{
				formalName = NormalizeRebelKingdomNameToken(formalName, 24);
				shortName = NormalizeRebelKingdomNameToken(shortName, 14);
				encyclopediaText = NormalizeRebelKingdomLoreText(encyclopediaText);
				if (!string.IsNullOrWhiteSpace(formalName) && !string.IsNullOrWhiteSpace(shortName) && !string.IsNullOrWhiteSpace(encyclopediaText) && !IsDuplicateKingdomName(formalName))
				{
					return new RebelKingdomNamingResult
					{
						FormalName = formalName,
						ShortName = shortName,
						EncyclopediaText = encyclopediaText,
						UsedFallback = false,
						AttemptsUsed = i
					};
				}
				rebelKingdomNamingResult = fallbackResult ?? rebelKingdomNamingResult;
				rebelKingdomNamingResult.UsedFallback = true;
				rebelKingdomNamingResult.FailureReason = "模型返回的国名无效或与现有王国重名。";
			}
			else
			{
				rebelKingdomNamingResult = fallbackResult ?? rebelKingdomNamingResult;
				rebelKingdomNamingResult.UsedFallback = true;
				rebelKingdomNamingResult.FailureReason = (apiCallResult?.Success == true) ? "模型返回无法按 [NAME]/[SHORT]/[LORE] 解析。" : (apiCallResult?.ErrorMessage ?? "命名请求失败。");
			}
			rebelKingdomNamingResult.AttemptsUsed = i;
			if (i < maxAttempts)
			{
				int num = 1200;
				if (apiCallResult?.IsRateLimit == true)
				{
					num = Math.Max(num, GetWeeklyReportRequestIntervalMs());
				}
				if (apiCallResult?.RetryAfterSeconds != null)
				{
					num = Math.Max(num, apiCallResult.RetryAfterSeconds.Value * 1000);
				}
				Thread.Sleep(num);
			}
		}
		return rebelKingdomNamingResult;
	}

	private RebelKingdomNamingResult BuildFallbackRebelKingdomNamingResult(Clan clan, Kingdom oldKingdom, int weekIndex, string failureReason = "")
	{
		return BuildFallbackStaticRebelKingdomNamingResult(clan, oldKingdom, weekIndex, failureReason);
	}

	private RebelKingdomNamingResult GenerateRebelKingdomNaming(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null, int maxAttempts = 1)
	{
		if (clan == null)
		{
			return BuildFallbackRebelKingdomNamingResult(clan, oldKingdom, weekIndex, "主导家族为空，无法请求命名。");
		}
		BuildRebelKingdomNamingRequest(clan, oldKingdom, weekIndex, followerClans, out var systemPrompt, out var userPrompt, out var fallbackResult);
		return GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, fallbackResult, "叛乱建国命名 - " + GetClanId(clan), maxAttempts);
	}

	private bool TryValidateClanForKingdomRebellion(Clan clan, Kingdom kingdom, bool forceTrigger, out string note, out int relationToKing, out int townCount, out int castleCount)
	{
		note = "";
		relationToKing = 0;
		townCount = 0;
		castleCount = 0;
		if (clan == null)
		{
			note = "家族为空。";
			return false;
		}
		if (kingdom == null || kingdom.IsEliminated)
		{
			note = "王国不存在或已灭亡。";
			return false;
		}
		if (clan == Clan.PlayerClan)
		{
			note = "玩家家族不参与该系统的自动叛乱。";
			return false;
		}
		if (clan.Kingdom != kingdom)
		{
			note = "该家族当前不隶属于此王国。";
			return false;
		}
		if (clan == kingdom.RulingClan || clan == kingdom.Leader?.Clan)
		{
			note = "执政家族不会作为叛乱候选。";
			return false;
		}
		if (clan.IsEliminated)
		{
			note = "该家族已灭亡。";
			return false;
		}
		if (clan.IsBanditFaction || clan.IsMinorFaction || clan.IsRebelClan)
		{
			note = "该家族派系类型不适合纳入该叛乱逻辑。";
			return false;
		}
		if (clan.IsUnderMercenaryService || clan.IsClanTypeMercenary)
		{
			note = "佣兵家族不会作为叛乱候选。";
			return false;
		}
		Hero leader = clan.Leader;
		if (leader == null || !leader.IsAlive || leader.IsChild)
		{
			note = "族长状态无效。";
			return false;
		}
		if (leader.IsPrisoner)
		{
			note = "族长当前被囚，暂不触发带城反出。";
			return false;
		}
		townCount = clan.Settlements?.Count((Settlement x) => x != null && x.IsTown) ?? 0;
		castleCount = clan.Settlements?.Count((Settlement x) => x != null && x.IsCastle) ?? 0;
		if (townCount + castleCount <= 0)
		{
			note = "该家族没有城镇或城堡，不能带城反出。";
			return false;
		}
		try
		{
			relationToKing = ((kingdom.Leader != null) ? leader.GetRelation(kingdom.Leader) : 0);
		}
		catch
		{
			relationToKing = 0;
		}
		if (!forceTrigger && relationToKing > -5)
		{
			note = "与国王关系未低于 -5，暂不列入自动叛乱候选。";
			return false;
		}
		return true;
	}

	private static float ComputeKingdomRebellionCandidateScore(Clan clan, Kingdom kingdom, int relationToKing, int townCount, int castleCount)
	{
		float num = 0f;
		num += Math.Min(900f, Math.Max(0f, clan?.Renown ?? 0f) * 0.45f);
		num += Math.Max(0, clan?.Tier ?? 0) * 140;
		num += Math.Min(420f, Math.Max(0f, clan?.CurrentTotalStrength ?? 0f) / 4f);
		num += townCount * 140;
		num += castleCount * 70;
		num += Math.Max(0, -relationToKing) * 1.5f;
		if (clan?.Leader?.Culture != null && kingdom?.Leader?.Culture != null && clan.Leader.Culture == kingdom.Leader.Culture)
		{
			num += 8f;
		}
		return num;
	}

	private bool TryValidateClanForRebelFollower(Clan clan, Kingdom kingdom, Clan leaderClan, bool forceTrigger, out string note, out int relationToKing, out int relationToLeader, out int townCount, out int castleCount)
	{
		note = "";
		relationToKing = 0;
		relationToLeader = 0;
		townCount = 0;
		castleCount = 0;
		if (clan == null || leaderClan == null)
		{
			note = "家族为空。";
			return false;
		}
		if (clan == leaderClan)
		{
			note = "主导家族不作为跟随候选。";
			return false;
		}
		if (!TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger: true, out note, out relationToKing, out townCount, out castleCount))
		{
			return false;
		}
		Hero leader = clan.Leader;
		Hero leader2 = leaderClan.Leader;
		if (leader == null || leader2 == null)
		{
			note = "族长状态无效。";
			return false;
		}
		try
		{
			relationToLeader = leader.GetRelation(leader2);
		}
		catch
		{
			relationToLeader = 0;
		}
		return true;
	}

	private static bool IsEligibleRebelFollowerByStandardRules(int relationToKing, int relationToLeader, float score)
	{
		return relationToLeader - relationToKing >= 15;
	}

	private static bool IsEligibleRebelFollowerByRelativeFallback(int relationToKing, int relationToLeader, float score)
	{
		return false;
	}

	private static float ComputeKingdomRebellionFollowerScore(Clan clan, Kingdom kingdom, Clan leaderClan, int relationToKing, int relationToLeader)
	{
		float num = 0f;
		num += Math.Max(0, relationToLeader) * 3.5f;
		num += Math.Max(0, -relationToKing) * 2.5f;
		if (clan?.Leader?.Culture != null && leaderClan?.Leader?.Culture != null && clan.Leader.Culture == leaderClan.Leader.Culture)
		{
			num += 6f;
		}
		if (clan?.Leader?.Culture != null && kingdom?.Leader?.Culture != null && clan.Leader.Culture == kingdom.Leader.Culture)
		{
			num -= 4f;
		}
		return num;
	}

	private List<KingdomRebellionFollowerInfo> EvaluateKingdomRebellionFollowers(Kingdom kingdom, Clan leaderClan, bool forceTrigger)
	{
		List<KingdomRebellionFollowerInfo> list = new List<KingdomRebellionFollowerInfo>();
		if (kingdom == null || leaderClan == null)
		{
			return list;
		}
		IEnumerable<Clan> enumerable = kingdom.Clans ?? Enumerable.Empty<Clan>();
		foreach (Clan clan in enumerable)
		{
			if (clan == null)
			{
				continue;
			}
			KingdomRebellionFollowerInfo kingdomRebellionFollowerInfo = new KingdomRebellionFollowerInfo
			{
				Clan = clan,
				ClanId = GetClanId(clan),
				ClanName = GetClanDisplayName(clan)
			};
			if (!TryValidateClanForRebelFollower(clan, kingdom, leaderClan, forceTrigger, out var note, out var relationToKing, out var relationToLeader, out var townCount, out var castleCount))
			{
				kingdomRebellionFollowerInfo.Eligible = false;
				kingdomRebellionFollowerInfo.Note = note;
				kingdomRebellionFollowerInfo.RelationToKing = relationToKing;
				kingdomRebellionFollowerInfo.RelationToLeader = relationToLeader;
				kingdomRebellionFollowerInfo.TownCount = Math.Max(0, townCount);
				kingdomRebellionFollowerInfo.CastleCount = Math.Max(0, castleCount);
				kingdomRebellionFollowerInfo.ClanTier = Math.Max(0, clan.Tier);
			}
			else
			{
				kingdomRebellionFollowerInfo.RelationToKing = relationToKing;
				kingdomRebellionFollowerInfo.RelationToLeader = relationToLeader;
				kingdomRebellionFollowerInfo.TownCount = townCount;
				kingdomRebellionFollowerInfo.CastleCount = castleCount;
				kingdomRebellionFollowerInfo.ClanTier = Math.Max(0, clan.Tier);
				kingdomRebellionFollowerInfo.Score = ComputeKingdomRebellionFollowerScore(clan, kingdom, leaderClan, relationToKing, relationToLeader);
				bool flag = IsEligibleRebelFollowerByStandardRules(relationToKing, relationToLeader, kingdomRebellionFollowerInfo.Score);
				bool flag2 = IsEligibleRebelFollowerByRelativeFallback(relationToKing, relationToLeader, kingdomRebellionFollowerInfo.Score);
				kingdomRebellionFollowerInfo.Eligible = flag || flag2;
				int num = relationToLeader - relationToKing;
				if (flag)
				{
					kingdomRebellionFollowerInfo.Note = "满足联合叛乱跟随条件：对叛乱领袖关系减去对国王关系 = " + num + "（>= 15）。";
				}
				else
				{
					kingdomRebellionFollowerInfo.Note = "不满足联合叛乱跟随条件：对叛乱领袖关系减去对国王关系 = " + num + "（需 >= 15）。";
				}
			}
			list.Add(kingdomRebellionFollowerInfo);
		}
		return list.OrderByDescending((KingdomRebellionFollowerInfo x) => x.Eligible).ThenByDescending((KingdomRebellionFollowerInfo x) => x.Score).ThenBy((KingdomRebellionFollowerInfo x) => x.ClanName ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private List<KingdomRebellionCandidateInfo> EvaluateKingdomRebellionCandidates(Kingdom kingdom, bool forceTrigger)
	{
		List<KingdomRebellionCandidateInfo> list = new List<KingdomRebellionCandidateInfo>();
		if (kingdom == null)
		{
			return list;
		}
		IEnumerable<Clan> enumerable = kingdom.Clans ?? Enumerable.Empty<Clan>();
		foreach (Clan clan in enumerable)
		{
			if (clan == null)
			{
				continue;
			}
			KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = new KingdomRebellionCandidateInfo
			{
				Clan = clan,
				ClanId = GetClanId(clan),
				ClanName = GetClanDisplayName(clan)
			};
			if (!TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger, out var note, out var relationToKing, out var townCount, out var castleCount))
			{
				kingdomRebellionCandidateInfo.Eligible = false;
				kingdomRebellionCandidateInfo.Note = note;
				kingdomRebellionCandidateInfo.RelationToKing = relationToKing;
				kingdomRebellionCandidateInfo.TownCount = Math.Max(0, townCount);
				kingdomRebellionCandidateInfo.CastleCount = Math.Max(0, castleCount);
				kingdomRebellionCandidateInfo.TotalFortificationCount = kingdomRebellionCandidateInfo.TownCount + kingdomRebellionCandidateInfo.CastleCount;
				kingdomRebellionCandidateInfo.ClanTier = Math.Max(0, clan.Tier);
			}
			else
			{
				kingdomRebellionCandidateInfo.Eligible = true;
				kingdomRebellionCandidateInfo.Note = forceTrigger ? "满足强制触发条件。" : "满足自动叛乱条件。";
				kingdomRebellionCandidateInfo.RelationToKing = relationToKing;
				kingdomRebellionCandidateInfo.TownCount = townCount;
				kingdomRebellionCandidateInfo.CastleCount = castleCount;
				kingdomRebellionCandidateInfo.TotalFortificationCount = townCount + castleCount;
				kingdomRebellionCandidateInfo.ClanTier = Math.Max(0, clan.Tier);
				kingdomRebellionCandidateInfo.Score = ComputeKingdomRebellionCandidateScore(clan, kingdom, relationToKing, townCount, castleCount);
			}
			list.Add(kingdomRebellionCandidateInfo);
		}
		return list.OrderByDescending((KingdomRebellionCandidateInfo x) => x.Eligible).ThenByDescending((KingdomRebellionCandidateInfo x) => x.Score).ThenBy((KingdomRebellionCandidateInfo x) => x.ClanName ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private KingdomRebellionResolutionResult ResolveKingdomRebellion(Kingdom kingdom, int weekIndex, bool executeAction, bool forceTrigger)
	{
		KingdomRebellionResolutionResult kingdomRebellionResolutionResult = new KingdomRebellionResolutionResult
		{
			Kingdom = kingdom,
			WeekIndex = weekIndex,
			Forced = forceTrigger
		};
		if (kingdom == null)
		{
			kingdomRebellionResolutionResult.Message = "找不到目标王国。";
			return kingdomRebellionResolutionResult;
		}
		int kingdomStabilityValue = GetKingdomStabilityValue(kingdom);
		kingdomRebellionResolutionResult.StabilityValue = kingdomStabilityValue;
		kingdomRebellionResolutionResult.StabilityTierText = GetKingdomStabilityTierText(kingdomStabilityValue);
		kingdomRebellionResolutionResult.TriggerChance = GetKingdomRebellionWeeklyChance(kingdomStabilityValue);
		if (kingdom.IsEliminated)
		{
			kingdomRebellionResolutionResult.Message = GetKingdomDisplayName(kingdom, "该王国") + "已灭亡，跳过叛乱判定。";
			return kingdomRebellionResolutionResult;
		}
		if (kingdom.Leader == null)
		{
			kingdomRebellionResolutionResult.Message = GetKingdomDisplayName(kingdom, "该王国") + "当前缺少有效领袖，跳过叛乱判定。";
			return kingdomRebellionResolutionResult;
		}
		kingdomRebellionResolutionResult.Candidates = EvaluateKingdomRebellionCandidates(kingdom, forceTrigger: false);
		foreach (KingdomRebellionCandidateInfo item in kingdomRebellionResolutionResult.Candidates.Where((KingdomRebellionCandidateInfo x) => x != null && x.Clan != null))
		{
			item.PreviewFollowerClanNames = EvaluateKingdomRebellionFollowers(kingdom, item.Clan, forceTrigger: false).Where((KingdomRebellionFollowerInfo x) => x != null && x.Eligible && x.Clan != null).Select((KingdomRebellionFollowerInfo x) => GetClanDisplayName(x.Clan)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		if (!forceTrigger)
		{
			if (kingdomRebellionResolutionResult.TriggerChance <= 0f)
			{
				kingdomRebellionResolutionResult.Message = GetKingdomDisplayName(kingdom, "该王国") + "当前稳定度为“" + kingdomRebellionResolutionResult.StabilityTierText + "”，本周没有叛乱概率。";
				return kingdomRebellionResolutionResult;
			}
			float randomFloat = MBRandom.RandomFloat;
			kingdomRebellionResolutionResult.Roll = randomFloat;
			kingdomRebellionResolutionResult.PassedChanceGate = randomFloat < kingdomRebellionResolutionResult.TriggerChance;
			if (!kingdomRebellionResolutionResult.PassedChanceGate)
			{
				kingdomRebellionResolutionResult.Message = GetKingdomDisplayName(kingdom, "该王国") + "本周叛乱抽签未命中。当前档位“" + kingdomRebellionResolutionResult.StabilityTierText + "”，概率 " + FormatKingdomRebellionChance(kingdomRebellionResolutionResult.TriggerChance) + "，本次掷值 " + randomFloat.ToString("0.000") + "。";
				return kingdomRebellionResolutionResult;
			}
		}
		else
		{
			kingdomRebellionResolutionResult.PassedChanceGate = true;
		}
		KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = kingdomRebellionResolutionResult.Candidates.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Eligible && x.Clan != null);
		if (kingdomRebellionCandidateInfo == null)
		{
			kingdomRebellionResolutionResult.Message = GetKingdomDisplayName(kingdom, "该王国") + "当前没有满足条件的带城叛乱候选家族。";
			return kingdomRebellionResolutionResult;
		}
		kingdomRebellionResolutionResult.SelectedClan = kingdomRebellionCandidateInfo.Clan;
		kingdomRebellionResolutionResult.FollowerCandidates = EvaluateKingdomRebellionFollowers(kingdom, kingdomRebellionCandidateInfo.Clan, forceTrigger: false);
		kingdomRebellionResolutionResult.SelectedFollowerClans = kingdomRebellionResolutionResult.FollowerCandidates.Where((KingdomRebellionFollowerInfo x) => x != null && x.Eligible && x.Clan != null).Select((KingdomRebellionFollowerInfo x) => x.Clan).ToList();
		if (!executeAction)
		{
			string text = (kingdomRebellionResolutionResult.SelectedFollowerClans.Count > 0) ? ("；预计跟随家族 " + string.Join("、", kingdomRebellionResolutionResult.SelectedFollowerClans.Select(GetClanDisplayName))) : "；当前没有会联合响应的其他家族。";
			kingdomRebellionResolutionResult.Message = "当前最可能反出的家族是 " + kingdomRebellionCandidateInfo.ClanName + "（关系 " + kingdomRebellionCandidateInfo.RelationToKing + "，评分 " + kingdomRebellionCandidateInfo.Score.ToString("0.0") + "）" + text;
			return kingdomRebellionResolutionResult;
		}
		kingdomRebellionResolutionResult.Executed = TryExecuteKingdomRebellion(kingdomRebellionCandidateInfo.Clan, kingdom, weekIndex, forceTrigger, kingdomRebellionResolutionResult.SelectedFollowerClans, out var message);
		kingdomRebellionResolutionResult.Message = message;
		return kingdomRebellionResolutionResult;
	}

	private bool TryExecuteKingdomRebellion(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, List<Clan> followerClans, out string message)
	{
		message = "";
		if (!TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger: true, out var note, out var relationToKing, out var townCount, out var castleCount))
		{
			message = "执行叛乱失败：" + note;
			return false;
		}
		try
		{
			RebelKingdomNamingResult rebelKingdomNamingResult = GenerateRebelKingdomNaming(clan, kingdom, weekIndex, followerClans);
			return TryExecuteKingdomRebellionWithNaming(clan, kingdom, weekIndex, forceTrigger, relationToKing, townCount, castleCount, rebelKingdomNamingResult, followerClans, out message);
		}
		catch (Exception ex)
		{
			message = "执行叛乱失败：" + ex.Message;
			Logger.Log("KingdomRebellion", "[ERROR] TryExecuteKingdomRebellion failed: " + ex);
			return false;
		}
	}

	private bool TryExecuteKingdomRebellionWithNaming(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, int relationToKing, int townCount, int castleCount, RebelKingdomNamingResult rebelKingdomNamingResult, List<Clan> followerClans, out string message)
	{
		message = "";
		string kingdomDisplayName = GetKingdomDisplayName(kingdom, "该王国");
		string clanDisplayName = GetClanDisplayName(clan);
		string text = NormalizeRebelKingdomNameToken(rebelKingdomNamingResult?.FormalName ?? "", 24);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = BuildRebelKingdomFallbackFormalName(clan);
		}
		string text2 = NormalizeRebelKingdomNameToken(rebelKingdomNamingResult?.ShortName ?? "", 14);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = BuildRebelKingdomFallbackShortName(text, clan);
		}
		string text3 = NormalizeRebelKingdomLoreText(rebelKingdomNamingResult?.EncyclopediaText ?? "");
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = BuildRebelKingdomFallbackLoreText(clan, kingdom, text, weekIndex);
		}
		ClanVisualSnapshot clanVisualSnapshot = CaptureClanVisualSnapshot(clan);
		ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(clan, showNotification: true);
		RestoreClanVisualSnapshot(clan, clanVisualSnapshot);
		RebelFactionColorChoice rebelFactionColorChoice = BuildRandomUniqueRebelFactionColors(clan, kingdom, clanVisualSnapshot);
		ApplyRebelFactionColorChoiceToClan(clan, rebelFactionColorChoice);
		KingdomManager kingdomManager = Campaign.Current?.KingdomManager;
		if (kingdomManager == null)
		{
			message = clanDisplayName + " 已从 " + kingdomDisplayName + " 反出，但当前未找到 KingdomManager，未能继续建立新王国。";
			Logger.Log("KingdomRebellion", "[ERROR] KingdomManager unavailable after rebellion leave. clan=" + GetClanId(clan));
			return true;
		}
		kingdomManager.CreateKingdom(new TextObject(text, null), new TextObject(text2, null), clan.Culture ?? kingdom.Culture, clan, null, new TextObject(text3, null), new TextObject(text, null), null);
		Kingdom kingdom2 = clan.Kingdom;
		if (kingdom != null && !kingdom.IsEliminated)
		{
			SetKingdomStabilityValue(kingdom, KingdomStabilityDefaultValue);
		}
		if (kingdom2 != null && !kingdom2.IsEliminated)
		{
			SetKingdomStabilityValue(kingdom2, RebelKingdomInitialStabilityValue);
		}
		List<string> list = new List<string>();
		if (kingdom2 != null && followerClans != null && followerClans.Count > 0)
		{
			foreach (Clan followerClan in followerClans.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => GetClanId(x), StringComparer.OrdinalIgnoreCase).Select((IGrouping<string, Clan> x) => x.First()))
			{
				try
				{
					if (followerClan.IsEliminated || followerClan.Kingdom != kingdom || followerClan.IsUnderMercenaryService || followerClan.IsClanTypeMercenary)
					{
						continue;
					}
					ChangeKingdomAction.ApplyByJoinToKingdomByDefection(followerClan, kingdom, kingdom2, default(CampaignTime), showNotification: true);
					list.Add(GetClanDisplayName(followerClan));
					MarkClanVisualsDirty(followerClan);
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomRebellion", "[WARN] Failed to attach follower clan to rebel kingdom. leader=" + GetClanId(clan) + " follower=" + GetClanId(followerClan) + " error=" + ex.Message);
				}
			}
		}
		MarkClanVisualsDirty(clan);
		string clanFortificationSummary = BuildClanFortificationSummary(clan);
		message = clanDisplayName + " 已从 " + kingdomDisplayName + " 反出，并建立了 " + text + "。";
		if (!string.IsNullOrWhiteSpace(clanFortificationSummary))
		{
			message = message + " " + clanFortificationSummary;
		}
		if (list.Count > 0)
		{
			message = message + " 联合响应家族：" + string.Join("、", list) + "。";
		}
		if (rebelKingdomNamingResult?.UsedFallback == true)
		{
			Logger.Log("KingdomRebellion", "[WARN] Rebel kingdom naming used fallback. clan=" + GetClanId(clan) + " reason=" + (rebelKingdomNamingResult.FailureReason ?? ""));
		}
		Logger.Log("KingdomRebellion", "[EXECUTE] week=" + weekIndex + " kingdom=" + GetKingdomId(kingdom) + " clan=" + GetClanId(clan) + " forced=" + forceTrigger + " relation=" + relationToKing + " towns=" + townCount + " castles=" + castleCount + " newKingdomName=" + text + " fallback=" + ((rebelKingdomNamingResult?.UsedFallback ?? false) ? "1" : "0") + " followerCount=" + list.Count + " followers=" + string.Join("|", list) + " bgColor=0x" + rebelFactionColorChoice.BackgroundColor.ToString("X8") + " iconColor=0x" + rebelFactionColorChoice.IconColor.ToString("X8"));
		return true;
	}

	private void TryProcessWeeklyKingdomRebellions(int weekIndex)
	{
		if (weekIndex <= 0 || _lastProcessedKingdomRebellionWeek >= weekIndex)
		{
			return;
		}
		try
		{
			foreach (Kingdom devEditableKingdom in GetDevEditableKingdoms())
			{
				try
				{
					int kingdomStabilityValue = GetKingdomStabilityValue(devEditableKingdom);
					int kingdomStabilityWeeklyBalancingDelta = GetKingdomStabilityWeeklyBalancingDelta(kingdomStabilityValue);
					if (kingdomStabilityWeeklyBalancingDelta != 0)
					{
						SetKingdomStabilityValue(devEditableKingdom, kingdomStabilityValue + kingdomStabilityWeeklyBalancingDelta);
					}
					KingdomRebellionResolutionResult kingdomRebellionResolutionResult = ResolveKingdomRebellion(devEditableKingdom, weekIndex, executeAction: false, forceTrigger: false);
					if (kingdomRebellionResolutionResult != null && kingdomRebellionResolutionResult.PassedChanceGate && kingdomRebellionResolutionResult.SelectedClan != null)
					{
						QueueAutomaticKingdomRebellion(kingdomRebellionResolutionResult);
					}
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomRebellion", "[ERROR] Weekly resolution failed for kingdom " + GetKingdomId(devEditableKingdom) + ": " + ex.Message);
				}
			}
			_lastProcessedKingdomRebellionWeek = Math.Max(_lastProcessedKingdomRebellionWeek, weekIndex);
			if (_queuedAutomaticKingdomRebellions.Count > 0)
			{
				_automaticKingdomRebellionFlowActive = true;
				TryStartNextAutomaticKingdomRebellionAsync();
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("KingdomRebellion", "[ERROR] TryProcessWeeklyKingdomRebellions failed: " + ex2);
		}
	}

	private void QueueAutomaticKingdomRebellion(KingdomRebellionResolutionResult result)
	{
		if (result?.Kingdom == null || result.SelectedClan == null)
		{
			return;
		}
		KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = result.Candidates?.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Clan == result.SelectedClan);
		PendingAutomaticKingdomRebellionContext item = new PendingAutomaticKingdomRebellionContext
		{
			KingdomId = GetKingdomId(result.Kingdom),
			ClanId = GetClanId(result.SelectedClan),
			WeekIndex = result.WeekIndex,
			StabilityValue = result.StabilityValue,
			StabilityTierText = result.StabilityTierText,
			RelationToKing = kingdomRebellionCandidateInfo?.RelationToKing ?? 0,
			TownCount = kingdomRebellionCandidateInfo?.TownCount ?? 0,
			CastleCount = kingdomRebellionCandidateInfo?.CastleCount ?? 0,
			FollowerClanIds = (result.SelectedFollowerClans ?? new List<Clan>()).Where((Clan x) => x != null && x != result.SelectedClan).Select(GetClanId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
		};
		_queuedAutomaticKingdomRebellions.Add(item);
	}

	private void TryStartNextAutomaticKingdomRebellionAsync()
	{
		if (_automaticKingdomRebellionInProgress || _pendingAutomaticKingdomRebellionReady)
		{
			return;
		}
		PendingAutomaticKingdomRebellionContext pendingAutomaticKingdomRebellionContext = _queuedAutomaticKingdomRebellions.FirstOrDefault();
		if (pendingAutomaticKingdomRebellionContext == null)
		{
			_automaticKingdomRebellionFlowActive = false;
			return;
		}
		_queuedAutomaticKingdomRebellions.RemoveAt(0);
		Kingdom kingdom = FindKingdomById(pendingAutomaticKingdomRebellionContext.KingdomId);
		Clan clan = FindClanById(pendingAutomaticKingdomRebellionContext.ClanId);
		List<Clan> list = (pendingAutomaticKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(FindClanById).Where((Clan x) => x != null && x != clan).ToList();
		if (kingdom == null || clan == null)
		{
			ShowAutomaticKingdomRebellionCompletionPopup(pendingAutomaticKingdomRebellionContext, kingdom, clan, list, success: false, "本周自动叛乱已命中，但目标王国或家族状态已变化，无法继续执行。");
			return;
		}
		BuildRebelKingdomNamingRequest(clan, kingdom, pendingAutomaticKingdomRebellionContext.WeekIndex, list, out var systemPrompt, out var userPrompt, out var fallbackResult);
		_automaticKingdomRebellionInProgress = true;
		_pendingAutomaticKingdomRebellionReady = false;
		_pendingAutomaticKingdomRebellionContext = null;
		InformationManager.ShowInquiry(new InquiryData("正在生成叛乱建国命名", "系统正在为本周自动叛乱生成新王国的名称与百科简介。\n\n这一步完成前不会继续本轮自动叛乱与周报流程。\n请稍候，结果完成后会自动弹出。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, fallbackResult, "自动叛乱建国命名 - " + GetClanId(clan), 1);
			pendingAutomaticKingdomRebellionContext.NamingResult = namingResult;
			_pendingAutomaticKingdomRebellionContext = pendingAutomaticKingdomRebellionContext;
			_pendingAutomaticKingdomRebellionReady = true;
		});
	}

	private void ProcessPendingAutomaticKingdomRebellionResult()
	{
		if (!_pendingAutomaticKingdomRebellionReady)
		{
			return;
		}
		PendingAutomaticKingdomRebellionContext pendingAutomaticKingdomRebellionContext = _pendingAutomaticKingdomRebellionContext;
		_pendingAutomaticKingdomRebellionReady = false;
		_pendingAutomaticKingdomRebellionContext = null;
		_automaticKingdomRebellionInProgress = false;
		if (pendingAutomaticKingdomRebellionContext == null)
		{
			return;
		}
		Kingdom kingdom = FindKingdomById(pendingAutomaticKingdomRebellionContext.KingdomId);
		Clan clan = FindClanById(pendingAutomaticKingdomRebellionContext.ClanId);
		List<Clan> list = (pendingAutomaticKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(FindClanById).Where((Clan x) => x != null && x != clan).ToList();
		string executionMessage;
		bool success;
		if (kingdom == null || clan == null)
		{
			success = false;
			executionMessage = "叛乱命名已完成，但目标王国或家族状态已变化，无法继续执行。";
		}
		else
		{
			success = TryExecuteKingdomRebellionWithNaming(clan, kingdom, pendingAutomaticKingdomRebellionContext.WeekIndex, forceTrigger: false, pendingAutomaticKingdomRebellionContext.RelationToKing, pendingAutomaticKingdomRebellionContext.TownCount, pendingAutomaticKingdomRebellionContext.CastleCount, pendingAutomaticKingdomRebellionContext.NamingResult, list, out executionMessage);
		}
		ShowAutomaticKingdomRebellionCompletionPopup(pendingAutomaticKingdomRebellionContext, kingdom, clan, list, success, executionMessage);
	}

	private void ShowAutomaticKingdomRebellionCompletionPopup(PendingAutomaticKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool success, string executionMessage)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (kingdom != null)
		{
			stringBuilder.AppendLine("王国：" + GetKingdomDisplayName(kingdom, "某王国"));
		}
		if (!string.IsNullOrWhiteSpace(context?.StabilityTierText))
		{
			stringBuilder.AppendLine("触发时稳定度：" + context.StabilityValue + "（" + context.StabilityTierText + "）");
		}
		if (clan != null)
		{
			stringBuilder.AppendLine("主导家族：" + GetClanDisplayName(clan));
		}
		if (context?.NamingResult != null)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("命名结果：");
			stringBuilder.AppendLine("- 正式名：" + ((context.NamingResult.FormalName ?? "").Trim()));
			stringBuilder.AppendLine("- 简称：" + ((context.NamingResult.ShortName ?? "").Trim()));
			stringBuilder.AppendLine("- 来源：" + (context.NamingResult.UsedFallback ? "本地兜底" : "LLM"));
			if (!string.IsNullOrWhiteSpace(context.NamingResult.EncyclopediaText))
			{
				stringBuilder.AppendLine("- 百科简介：" + context.NamingResult.EncyclopediaText.Trim());
			}
			if (!string.IsNullOrWhiteSpace(context.NamingResult.FailureReason))
			{
				stringBuilder.AppendLine("- 命名说明：" + context.NamingResult.FailureReason.Trim());
			}
		}
		if (followerClans != null && followerClans.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("联合响应家族：");
			stringBuilder.AppendLine("- " + string.Join("、", followerClans.Select(GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("执行结果：");
		stringBuilder.AppendLine((executionMessage ?? "").Trim());
		if (_queuedAutomaticKingdomRebellions.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("后续仍有 " + _queuedAutomaticKingdomRebellions.Count + " 场自动叛乱待处理。点击“继续”后将进入下一场。");
		}
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(success ? "自动叛乱执行完成" : "自动叛乱执行失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "继续", "", delegate
		{
			ContinueAutomaticKingdomRebellionFlow();
		}, null), pauseGameActiveState: true);
	}

	private void ContinueAutomaticKingdomRebellionFlow()
	{
		if (_queuedAutomaticKingdomRebellions.Count > 0)
		{
			TryStartNextAutomaticKingdomRebellionAsync();
			return;
		}
		_automaticKingdomRebellionFlowActive = false;
		TryStartDeferredAutoWeeklyReports();
	}

	private static Settlement ResolveGatheringPointSettlement(IMapPoint gatheringPoint, MobileParty fallbackParty = null)
	{
		if (gatheringPoint is Settlement settlement)
		{
			return settlement;
		}
		try
		{
			if (gatheringPoint != null)
			{
				Settlement settlement2 = Helpers.SettlementHelper.FindNearestSettlementToPoint(gatheringPoint.Position, (Settlement x) => x != null && !x.IsHideout);
				if (settlement2 != null)
				{
					return settlement2;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (fallbackParty != null)
			{
				return Helpers.SettlementHelper.FindNearestSettlementToMobileParty(fallbackParty, MobileParty.NavigationType.All, (Settlement x) => x != null && !x.IsHideout);
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement ResolveSettlementForPartyBase(PartyBase party)
	{
		MobileParty mobileParty = party?.MobileParty;
		if (mobileParty == null)
		{
			return null;
		}
		return mobileParty.BesiegedSettlement ?? ResolveSiegeSettlement(mobileParty.SiegeEvent) ?? mobileParty.TargetSettlement ?? mobileParty.CurrentSettlement ?? mobileParty.LastVisitedSettlement;
	}

	private static string GetLocationLabelForPartyBase(PartyBase party)
	{
		string text = GetNearestSettlementNameForParty(party?.MobileParty);
		return string.IsNullOrWhiteSpace(text) ? "" : text;
	}

	private static string ResolveGatheringPointLabel(IMapPoint gatheringPoint, MobileParty fallbackParty = null)
	{
		Settlement settlement = ResolveGatheringPointSettlement(gatheringPoint, fallbackParty);
		string text = (settlement?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (gatheringPoint?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "集结地", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "Gathering Point", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		text = GetNearestSettlementNameForParty(fallbackParty);
		return string.IsNullOrWhiteSpace(text) ? "集结地" : text;
	}

	private static NpcActionFacts CreateNpcActionFacts(string actionKind, Hero actorHero = null)
	{
		NpcActionFacts npcActionFacts = new NpcActionFacts
		{
			ActionKind = (actionKind ?? "").Trim()
		};
		ApplyActorFacts(npcActionFacts, actorHero);
		return npcActionFacts;
	}

	private static void ApplyActorFacts(NpcActionFacts facts, Hero hero)
	{
		if (facts == null || hero == null)
		{
			return;
		}
		facts.ActorHeroId = GetHeroId(hero);
		facts.ActorClanId = GetClanId(hero.Clan);
		facts.ActorKingdomId = GetKingdomId(hero.MapFaction);
		AddUniqueId(facts.RelatedHeroIds, facts.ActorHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.ActorClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.ActorKingdomId);
	}

	private static void ApplyTargetFacts(NpcActionFacts facts, Hero hero)
	{
		if (facts == null || hero == null)
		{
			return;
		}
		facts.TargetHeroId = GetHeroId(hero);
		facts.TargetClanId = GetClanId(hero.Clan);
		facts.TargetKingdomId = GetKingdomId(hero.MapFaction);
		AddUniqueId(facts.RelatedHeroIds, facts.TargetHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.TargetClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.TargetKingdomId);
	}

	private static void ApplySettlementFacts(NpcActionFacts facts, Settlement settlement, Hero currentOwnerOverride = null, Hero previousOwnerOverride = null, string locationText = null)
	{
		if (facts == null || settlement == null)
		{
			return;
		}
		facts.SettlementId = GetSettlementId(settlement);
		facts.SettlementName = (settlement.Name?.ToString() ?? "").Trim();
		facts.LocationText = string.IsNullOrWhiteSpace(locationText) ? facts.SettlementName : locationText.Trim();
		Hero hero = currentOwnerOverride ?? settlement.OwnerClan?.Leader;
		Hero hero2 = previousOwnerOverride;
		facts.SettlementOwnerHeroId = GetHeroId(hero);
		facts.SettlementOwnerClanId = GetClanId(hero?.Clan ?? settlement.OwnerClan);
		facts.SettlementOwnerKingdomId = GetKingdomId(hero?.MapFaction ?? settlement.MapFaction);
		facts.PreviousSettlementOwnerHeroId = GetHeroId(hero2);
		facts.PreviousSettlementOwnerClanId = GetClanId(hero2?.Clan);
		facts.PreviousSettlementOwnerKingdomId = GetKingdomId(hero2?.MapFaction);
		AddUniqueId(facts.RelatedHeroIds, facts.SettlementOwnerHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.SettlementOwnerClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.SettlementOwnerKingdomId);
		AddUniqueId(facts.RelatedHeroIds, facts.PreviousSettlementOwnerHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.PreviousSettlementOwnerClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.PreviousSettlementOwnerKingdomId);
	}

	private static void AddRelatedFactionFacts(NpcActionFacts facts, IFaction faction)
	{
		if (facts == null || faction == null)
		{
			return;
		}
		if (faction is Kingdom kingdom)
		{
			string kingdomId = GetKingdomId(kingdom);
			if (string.IsNullOrWhiteSpace(facts.TargetKingdomId))
			{
				facts.TargetKingdomId = kingdomId;
			}
			AddUniqueId(facts.RelatedKingdomIds, kingdomId);
			return;
		}
		if (faction is Clan clan)
		{
			string clanId = GetClanId(clan);
			if (string.IsNullOrWhiteSpace(facts.TargetClanId))
			{
				facts.TargetClanId = clanId;
			}
			AddUniqueId(facts.RelatedClanIds, clanId);
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(clan.Kingdom));
		}
	}

	private static void AddUniqueId(List<string> list, string id)
	{
		string text = (id ?? "").Trim();
		if (list == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!list.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			list.Add(text);
		}
	}

	private static void CopyFactIds(List<string> source, List<string> destination)
	{
		if (source == null || destination == null)
		{
			return;
		}
		foreach (string item in source)
		{
			AddUniqueId(destination, item);
		}
	}

	private void RecordEventSourceMaterial(string materialKind, string label, string snapshotText, string stableKey, string kingdomId, string settlementId, bool includeInWorld, bool includeInKingdom)
	{
		string text = (snapshotText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (_eventSourceMaterials == null)
		{
			_eventSourceMaterials = new List<EventSourceMaterialEntry>();
		}
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		string text2 = NormalizeNpcActionStableKey(stableKey, label + ":" + text);
		EventSourceMaterialEntry eventSourceMaterialEntry = _eventSourceMaterials.FirstOrDefault((EventSourceMaterialEntry x) => x != null && x.Day == currentGameDayIndexSafe && string.Equals((x.StableKey ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
		if (eventSourceMaterialEntry != null)
		{
			eventSourceMaterialEntry.Label = (label ?? "").Trim();
			eventSourceMaterialEntry.SnapshotText = text;
			eventSourceMaterialEntry.MaterialKind = (materialKind ?? "").Trim();
			eventSourceMaterialEntry.KingdomId = (kingdomId ?? "").Trim();
			eventSourceMaterialEntry.SettlementId = (settlementId ?? "").Trim();
			eventSourceMaterialEntry.IncludeInWorld = eventSourceMaterialEntry.IncludeInWorld || includeInWorld;
			eventSourceMaterialEntry.IncludeInKingdom = eventSourceMaterialEntry.IncludeInKingdom || includeInKingdom;
			return;
		}
		_eventSourceMaterials.Add(new EventSourceMaterialEntry
		{
			Day = currentGameDayIndexSafe,
			Sequence = ++_npcActionGlobalOrderCounter,
			GameDate = GetCurrentGameDateTextSafe(),
			MaterialKind = (materialKind ?? "").Trim(),
			Label = (label ?? "").Trim(),
			SnapshotText = text,
			StableKey = text2,
			KingdomId = (kingdomId ?? "").Trim(),
			SettlementId = (settlementId ?? "").Trim(),
			IncludeInWorld = includeInWorld,
			IncludeInKingdom = includeInKingdom
		});
		_eventSourceMaterials = SanitizeEventSourceMaterials(_eventSourceMaterials);
	}

	private void TrackTownWeeklyMaterialChanges(Town town)
	{
		if (town?.Settlement == null || !town.Settlement.IsFortification)
		{
			return;
		}
		string settlementId = GetSettlementId(town.Settlement);
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return;
		}
		TownStatSnapshot townStatSnapshot = CaptureTownSnapshot(town);
		if (!_townStatSnapshots.TryGetValue(settlementId, out var value) || value == null)
		{
			_townStatSnapshots[settlementId] = townStatSnapshot;
			return;
		}
		List<string> list = new List<string>();
		AppendTownChangeLine(list, "繁荣", value.Prosperity, townStatSnapshot.Prosperity, 100f);
		AppendTownChangeLine(list, "忠诚", value.Loyalty, townStatSnapshot.Loyalty, 2f);
		AppendTownChangeLine(list, "治安", value.Security, townStatSnapshot.Security, 2f);
		AppendTownChangeLine(list, "粮食", value.FoodStocks, townStatSnapshot.FoodStocks, 5f);
		AppendTownChangeLine(list, "民兵", value.Militia, townStatSnapshot.Militia, 8f);
		AppendTownChangeLine(list, "驻军", value.Garrison, townStatSnapshot.Garrison, 10f);
		_townStatSnapshots[settlementId] = townStatSnapshot;
		if (list.Count == 0)
		{
			return;
		}
		string settlementDisplayName = GetSettlementDisplayName(town.Settlement);
		string text = settlementDisplayName + "本日出现定居点状态波动：" + string.Join("；", list) + "。";
		string clanDisplayName = GetClanDisplayName(town.Settlement.OwnerClan);
		string kingdomDisplayName = GetKingdomDisplayName(town.Settlement.MapFaction as Kingdom, "所属王国");
		if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
		{
			text = text + " 当前由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
		}
		RecordEventSourceMaterial("settlement_stats", "定居点状态变化 - " + settlementDisplayName, text, "settlement_stats:" + settlementId + ":" + GetCurrentGameDayIndexSafe(), GetKingdomId(town.Settlement.MapFaction), settlementId, includeInWorld: false, includeInKingdom: true);
	}

	private static TownStatSnapshot CaptureTownSnapshot(Town town)
	{
		return new TownStatSnapshot
		{
			Prosperity = town?.Prosperity ?? 0f,
			Loyalty = town?.Loyalty ?? 0f,
			Security = town?.Security ?? 0f,
			FoodStocks = town?.FoodStocks ?? 0f,
			Militia = town?.Settlement?.Militia ?? 0f,
			Garrison = town?.GarrisonParty?.MemberRoster?.TotalManCount ?? 0
		};
	}

	private static void AppendTownChangeLine(List<string> lines, string label, float oldValue, float newValue, float threshold)
	{
		if (lines == null)
		{
			return;
		}
		float num = newValue - oldValue;
		if (MathF.Abs(num) < threshold)
		{
			return;
		}
		lines.Add(label + (num > 0f ? "上升" : "下降") + MathF.Abs(num).ToString("0.#"));
	}

	private static void AppendTownChangeLine(List<string> lines, string label, int oldValue, int newValue, int threshold)
	{
		if (lines == null)
		{
			return;
		}
		int num = newValue - oldValue;
		if (Math.Abs(num) < threshold)
		{
			return;
		}
		lines.Add(label + (num > 0 ? "上升" : "下降") + Math.Abs(num));
	}

	private static NpcActionEntry CreateNpcActionEntry(Hero hero, string text, string stableKey, int day, int order, int sequence, NpcActionFacts facts, bool isMajor)
	{
		NpcActionFacts npcActionFacts = facts ?? CreateNpcActionFacts("", hero);
		if (string.IsNullOrWhiteSpace(npcActionFacts.ActorHeroId))
		{
			ApplyActorFacts(npcActionFacts, hero);
		}
		npcActionFacts.IsMajor = isMajor;
		NpcActionEntry npcActionEntry = new NpcActionEntry
		{
			Day = Math.Max(0, day),
			Order = Math.Max(1, order),
			Sequence = Math.Max(0, sequence),
			GameDate = GetCurrentGameDateTextSafe(),
			Text = (text ?? "").Trim(),
			StableKey = NormalizeNpcActionStableKey(stableKey, text),
			ActionKind = (npcActionFacts.ActionKind ?? "").Trim(),
			ActorHeroId = (npcActionFacts.ActorHeroId ?? "").Trim(),
			ActorClanId = (npcActionFacts.ActorClanId ?? "").Trim(),
			ActorKingdomId = (npcActionFacts.ActorKingdomId ?? "").Trim(),
			TargetHeroId = (npcActionFacts.TargetHeroId ?? "").Trim(),
			TargetClanId = (npcActionFacts.TargetClanId ?? "").Trim(),
			TargetKingdomId = (npcActionFacts.TargetKingdomId ?? "").Trim(),
			SettlementId = (npcActionFacts.SettlementId ?? "").Trim(),
			SettlementName = (npcActionFacts.SettlementName ?? "").Trim(),
			SettlementOwnerHeroId = (npcActionFacts.SettlementOwnerHeroId ?? "").Trim(),
			SettlementOwnerClanId = (npcActionFacts.SettlementOwnerClanId ?? "").Trim(),
			SettlementOwnerKingdomId = (npcActionFacts.SettlementOwnerKingdomId ?? "").Trim(),
			PreviousSettlementOwnerHeroId = (npcActionFacts.PreviousSettlementOwnerHeroId ?? "").Trim(),
			PreviousSettlementOwnerClanId = (npcActionFacts.PreviousSettlementOwnerClanId ?? "").Trim(),
			PreviousSettlementOwnerKingdomId = (npcActionFacts.PreviousSettlementOwnerKingdomId ?? "").Trim(),
			LocationText = (npcActionFacts.LocationText ?? "").Trim(),
			Won = npcActionFacts.Won,
			IsMajor = isMajor
		};
		CopyFactIds(npcActionFacts.RelatedHeroIds, npcActionEntry.RelatedHeroIds);
		CopyFactIds(npcActionFacts.RelatedClanIds, npcActionEntry.RelatedClanIds);
		CopyFactIds(npcActionFacts.RelatedKingdomIds, npcActionEntry.RelatedKingdomIds);
		return npcActionEntry;
	}

	private static List<NpcActionEntry> SanitizeNpcActionEntries(List<NpcActionEntry> source, bool keepOnlyRecentWindow)
	{
		List<NpcActionEntry> list = new List<NpcActionEntry>();
		if (source == null || source.Count <= 0)
		{
			return list;
		}
		int num = GetCurrentGameDayIndexSafe();
		int num2 = num - RecentNpcActionWindowDays + 1;
		int num3 = 0;
		int num4 = 0;
		foreach (NpcActionEntry item in source)
		{
			if (item == null)
			{
				continue;
			}
			string text = (item.Text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (keepOnlyRecentWindow && item.Day < num2)
			{
				continue;
			}
			list.Add(new NpcActionEntry
			{
				Day = Math.Max(0, item.Day),
				Order = ((item.Order > 0) ? item.Order : (++num3)),
				Sequence = ((item.Sequence > 0) ? item.Sequence : (++num4)),
				GameDate = (item.GameDate ?? "").Trim(),
				Text = text,
				StableKey = NormalizeNpcActionStableKey(item.StableKey, text),
				ActionKind = (item.ActionKind ?? "").Trim(),
				ActorHeroId = (item.ActorHeroId ?? "").Trim(),
				ActorClanId = (item.ActorClanId ?? "").Trim(),
				ActorKingdomId = (item.ActorKingdomId ?? "").Trim(),
				TargetHeroId = (item.TargetHeroId ?? "").Trim(),
				TargetClanId = (item.TargetClanId ?? "").Trim(),
				TargetKingdomId = (item.TargetKingdomId ?? "").Trim(),
				SettlementId = (item.SettlementId ?? "").Trim(),
				SettlementName = (item.SettlementName ?? "").Trim(),
				SettlementOwnerHeroId = (item.SettlementOwnerHeroId ?? "").Trim(),
				SettlementOwnerClanId = (item.SettlementOwnerClanId ?? "").Trim(),
				SettlementOwnerKingdomId = (item.SettlementOwnerKingdomId ?? "").Trim(),
				PreviousSettlementOwnerHeroId = (item.PreviousSettlementOwnerHeroId ?? "").Trim(),
				PreviousSettlementOwnerClanId = (item.PreviousSettlementOwnerClanId ?? "").Trim(),
				PreviousSettlementOwnerKingdomId = (item.PreviousSettlementOwnerKingdomId ?? "").Trim(),
				LocationText = (item.LocationText ?? "").Trim(),
				Won = item.Won,
				IsMajor = item.IsMajor
			});
			CopyFactIds(item.RelatedHeroIds, list[list.Count - 1].RelatedHeroIds);
			CopyFactIds(item.RelatedClanIds, list[list.Count - 1].RelatedClanIds);
			CopyFactIds(item.RelatedKingdomIds, list[list.Count - 1].RelatedKingdomIds);
		}
		return list.OrderBy((NpcActionEntry x) => x.Day).ThenBy((NpcActionEntry x) => (x.Sequence > 0) ? x.Sequence : int.MaxValue).ThenBy((NpcActionEntry x) => x.Order).ThenBy((NpcActionEntry x) => x.GameDate ?? "", StringComparer.Ordinal).ToList();
	}

	private void RecordNpcMajorAction(Hero hero, string text, string stableKey, NpcActionFacts facts = null)
	{
		RecordNpcActionInternal(_npcMajorActions, hero, text, stableKey, keepOnlyRecentWindow: false, dedupeAcrossWindow: false, MaxMajorNpcActionEntriesPerHero, facts, isMajor: true);
	}

	private void RecordNpcRecentAction(Hero hero, string text, string stableKey, bool dedupeAcrossWindow = false, NpcActionFacts facts = null)
	{
		RecordNpcActionInternal(_npcRecentActions, hero, text, stableKey, keepOnlyRecentWindow: true, dedupeAcrossWindow, MaxRecentNpcActionEntriesPerHero, facts, isMajor: false);
	}

	private void RecordNpcActionInternal(Dictionary<string, List<NpcActionEntry>> storage, Hero hero, string text, string stableKey, bool keepOnlyRecentWindow, bool dedupeAcrossWindow, int maxEntries, NpcActionFacts facts, bool isMajor)
	{
		try
		{
			if (storage == null || !ShouldTrackNpcActionHero(hero))
			{
				return;
			}
			string npcActionHeroKey = GetNpcActionHeroKey(hero);
			string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(npcActionHeroKey) || string.IsNullOrWhiteSpace(text2))
			{
				return;
			}
			string text3 = NormalizeNpcActionStableKey(stableKey, text2);
			int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
			if (!storage.TryGetValue(npcActionHeroKey, out var value) || value == null)
			{
				value = new List<NpcActionEntry>();
				storage[npcActionHeroKey] = value;
			}
			if (keepOnlyRecentWindow)
			{
				int num = currentGameDayIndexSafe - RecentNpcActionWindowDays + 1;
				value.RemoveAll((NpcActionEntry x) => x == null || x.Day < num || string.IsNullOrWhiteSpace(x.Text));
			}
			else
			{
				value.RemoveAll((NpcActionEntry x) => x == null || string.IsNullOrWhiteSpace(x.Text));
			}
			if (dedupeAcrossWindow)
			{
				if (value.Any((NpcActionEntry x) => x != null && string.Equals(x.StableKey ?? "", text3, StringComparison.OrdinalIgnoreCase)))
				{
					return;
				}
			}
			else if (value.Any((NpcActionEntry x) => x != null && x.Day == currentGameDayIndexSafe && (string.Equals(x.StableKey ?? "", text3, StringComparison.OrdinalIgnoreCase) || string.Equals((x.Text ?? "").Trim(), text2, StringComparison.Ordinal))))
			{
				return;
			}
			int order = (value.Count > 0) ? (value.Max((NpcActionEntry x) => (x != null && x.Day == currentGameDayIndexSafe) ? x.Order : 0) + 1) : 1;
			int sequence = ++_npcActionGlobalOrderCounter;
			value.Add(CreateNpcActionEntry(hero, text2, text3, currentGameDayIndexSafe, order, sequence, facts, isMajor));
			value = value.OrderBy((NpcActionEntry x) => x.Day).ThenBy((NpcActionEntry x) => (x.Sequence > 0) ? x.Sequence : int.MaxValue).ThenBy((NpcActionEntry x) => x.Order).ThenBy((NpcActionEntry x) => x.GameDate ?? "", StringComparer.Ordinal).ToList();
			if (maxEntries > 0 && value.Count > maxEntries)
			{
				value = value.Skip(value.Count - maxEntries).ToList();
			}
			storage[npcActionHeroKey] = value;
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordNpcActionInternal: " + ex.Message);
		}
	}

	private static string GetArmyDisplayName(Army army)
	{
		string text = army?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
		string text2 = hero?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text2) ? "一支军团" : (text2.Trim() + "的军团");
	}

	private static Settlement ResolveSiegeSettlement(SiegeEvent siegeEvent)
	{
		if (siegeEvent == null)
		{
			return null;
		}
		try
		{
			foreach (Settlement item in Settlement.All)
			{
				if (item?.SiegeEvent == siegeEvent)
				{
					return item;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static IEnumerable<Hero> GetHeroesFromSiegeEventSide(SiegeEvent siegeEvent, BattleSideEnum side)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<Hero> list = new List<Hero>();
		if (siegeEvent == null)
		{
			return list;
		}
		try
		{
			foreach (PartyBase involvedPartiesForEventType in siegeEvent.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))
			{
				if (involvedPartiesForEventType == null || involvedPartiesForEventType.Side != side)
				{
					continue;
				}
				Hero leaderHero = involvedPartiesForEventType.LeaderHero;
				string npcActionHeroKey = (leaderHero == Hero.MainHero) ? "__player__" : GetNpcActionHeroKey(leaderHero);
				if (ShouldMentionBattleHero(leaderHero) && hashSet.Add(npcActionHeroKey))
				{
					list.Add(leaderHero);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static string GetMapEventLocationLabel(MapEvent mapEvent)
	{
		string text = mapEvent?.MapEventSettlement?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? "野外" : text.Trim();
	}

	private static string GetPrimaryOtherSideLabel(MapEventSide side)
	{
		string text = side?.LeaderParty?.LeaderHero?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = side?.LeaderParty?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = side?.MapFaction?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? "敌军" : text.Trim();
	}

	private static int GetMapEventTroopCount(MapEvent mapEvent)
	{
		try
		{
			return Math.Max(0, mapEvent?.GetNumberOfInvolvedMen() ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static int CountTrackedLordParties(MapEventSide side)
	{
		int num = 0;
		try
		{
			if (side?.Parties == null)
			{
				return 0;
			}
			foreach (MapEventParty party in side.Parties)
			{
				if (ShouldTrackNpcActionHero(party?.Party?.LeaderHero))
				{
					num++;
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static int GetTroopRosterTotalManCount(TroopRoster roster)
	{
		try
		{
			return Math.Max(0, roster?.TotalManCount ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	private static string GetFactionDisplayName(IFaction faction, string fallback = "某势力")
	{
		string text = faction?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
	}

	private static string GetHeroFactionDisplayName(Hero hero, IFaction fallbackFaction = null)
	{
		return GetFactionDisplayName(hero?.MapFaction ?? fallbackFaction, "某势力");
	}

	private static string BuildBattleHeroDisplayName(Hero hero, bool isHighlighted, string highlightTag)
	{
		string text = hero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (hero == Hero.MainHero) ? "玩家" : "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (hero == Hero.MainHero)
		{
			text += "（player）";
		}
		if (isHighlighted && !string.IsNullOrWhiteSpace(highlightTag))
		{
			text += "（" + highlightTag + "）";
		}
		return text;
	}

	private static string BuildTrackedHeroListText(IEnumerable<Hero> heroes, Hero highlightedHero, string highlightTag, int maxCount = 5)
	{
		if (heroes == null)
		{
			return "";
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		foreach (Hero item in heroes.Where(ShouldMentionBattleHero).OrderByDescending((Hero x) => x == highlightedHero))
		{
			string text = BuildBattleHeroDisplayName(item, item == highlightedHero, highlightTag);
			string npcActionHeroKey = (item == Hero.MainHero) ? "__player__" : GetNpcActionHeroKey(item);
			if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(npcActionHeroKey))
			{
				continue;
			}
			list.Add(text);
		}
		if (list.Count <= 0)
		{
			return "";
		}
		if (list.Count <= maxCount)
		{
			return string.Join("、", list);
		}
		return string.Join("、", list.Take(maxCount)) + "等" + list.Count + "人";
	}

	private static string BuildTrackedHeroListText(IEnumerable<Hero> heroes, int maxCount = 5)
	{
		return BuildTrackedHeroListText(heroes, null, "", maxCount);
	}

	private static string BuildTrackedHeroListText(MapEventSide side, int maxCount = 5)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		return BuildTrackedHeroListText(side.Parties.Select((MapEventParty x) => x?.Party?.LeaderHero), side.LeaderParty?.LeaderHero, "统帅", maxCount);
	}

	private static string BuildTrackedHeroListText(SiegeEvent siegeEvent, BattleSideEnum side, int maxCount = 5)
	{
		Hero highlightedHero = ((side == BattleSideEnum.Attacker) ? (siegeEvent?.BesiegerCamp?.LeaderParty?.LeaderHero) : (siegeEvent?.BesiegedSettlement?.OwnerClan?.Leader));
		return BuildTrackedHeroListText(GetHeroesFromSiegeEventSide(siegeEvent, side), highlightedHero, "统帅", maxCount);
	}

	private static string BuildMapEventCasualtyText(MapEventSide side)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		int num = 0;
		int num2 = 0;
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				num += GetTroopRosterTotalManCount(party?.DiedInBattle);
				num2 += GetTroopRosterTotalManCount(party?.WoundedInBattle);
			}
		}
		catch
		{
		}
		return "阵亡" + num + "、负伤" + num2;
	}

	private static int GetMapEventPartyCommittedTroopCount(MapEventParty party)
	{
		if (party == null)
		{
			return 0;
		}
		int troopRosterTotalManCount = GetTroopRosterTotalManCount(party.Party?.MemberRoster);
		int troopRosterTotalManCount2 = GetTroopRosterTotalManCount(party.DiedInBattle);
		int troopRosterTotalManCount3 = GetTroopRosterTotalManCount(party.WoundedInBattle);
		return Math.Max(0, troopRosterTotalManCount + troopRosterTotalManCount2 + troopRosterTotalManCount3);
	}

	private static string BuildMapEventCommittedTroopText(MapEventSide side)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		int num = 0;
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				num += GetMapEventPartyCommittedTroopCount(party);
			}
		}
		catch
		{
		}
		return (num > 0) ? (num + "人") : "";
	}

	private static string BuildMapEventStandoutText(MapEventSide side)
	{
		if (side?.Parties == null)
		{
			return "";
		}
		List<MapEventParty> list = side.Parties.Where((MapEventParty x) => ShouldMentionBattleHero(x?.Party?.LeaderHero)).ToList();
		if (list.Count <= 4)
		{
			return "";
		}
		MapEventParty mapEventParty = list.OrderByDescending((MapEventParty x) => x.ContributionToBattle).FirstOrDefault();
		if (mapEventParty == null || mapEventParty.ContributionToBattle <= 0)
		{
			return "";
		}
		int num = list.Sum((MapEventParty x) => Math.Max(0, x.ContributionToBattle));
		Hero leaderHero = mapEventParty.Party?.LeaderHero;
		string text = BuildBattleHeroDisplayName(leaderHero, side.LeaderParty?.LeaderHero == leaderHero, "统帅");
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (num <= 0)
		{
			return text + "战场贡献最突出。";
		}
		int num2 = (int)Math.Round((double)(100 * mapEventParty.ContributionToBattle) / (double)num);
		if (num2 < 30)
		{
			return "";
		}
		string text2 = GetFactionDisplayName(leaderHero?.MapFaction ?? side.MapFaction, "某势力");
		if (num2 > 90)
		{
			return "万军辟易！这根本不是一场战争，而是" + text2 + "的" + text + "与他部队的专属杀戮秀！敌人在他面前如同草芥一般不堪一击！";
		}
		if (num2 > 75)
		{
			return text2 + "的" + text + "几乎包揽了绝大多数的战果！他的大军如同利刃般撕裂敌阵，所向披靡！";
		}
		if (num2 > 50)
		{
			return text2 + "的" + text + "与他的部队在战场上战无不胜！他是" + text2 + "的英雄！";
		}
		return text2 + "的" + text + "与他的部队是我方的中流砥柱！他拥有最高的贡献！";
	}

	private static string BuildMapEventAftermathText(MapEvent mapEvent, MapEventSide side, bool won, string locationLabel)
	{
		if (mapEvent == null || side == null)
		{
			return "";
		}
		if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut)
		{
			return "";
		}
		if (mapEvent.IsRaid)
		{
			return won ? (locationLabel + "的袭掠已经结束，你正在清点缴获并整顿部队。") : (locationLabel + "的袭掠已经结束，你正在收拢部队并处理残局。");
		}
		return won ? ("这场发生在" + locationLabel + "的战斗已经结束，你正在整顿部队并清点战果。") : ("这场发生在" + locationLabel + "的战斗已经结束，你正在收拢残部并处理败战残局。");
	}

	private static string BuildArmyCommandClause(MapEventSide side, Hero actorHero)
	{
		Hero leaderHero = side?.LeaderParty?.LeaderHero;
		string text = leaderHero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text) || leaderHero == actorHero)
		{
			return "";
		}
		return text + "统帅的军团";
	}

	private static string BuildMapEventNarrative(MapEvent mapEvent, MapEventSide side, Hero actorHero, bool won, string locationLabel)
	{
		if (mapEvent == null || side == null)
		{
			return "";
		}
		string text = GetHeroFactionDisplayName(actorHero, side.MapFaction);
		string text2 = GetFactionDisplayName(side.OtherSide?.MapFaction, GetPrimaryOtherSideLabel(side.OtherSide));
		Settlement mapEventSettlement = mapEvent.MapEventSettlement;
		string text3 = BuildArmyCommandClause(side, actorHero);
		string text4;
		if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut)
		{
			string text5 = mapEventSettlement?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text5))
			{
				text5 = locationLabel;
			}
			string text6 = GetFactionDisplayName(side.OtherSide?.MapFaction, text2);
			text4 = ((side.MissionSide == BattleSideEnum.Attacker) ? (won ? ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "对" : (text3 + "对")) + text6 + "的领土" + text5.Trim() + "的围城战中获胜。") : ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "对" : (text3 + "对")) + text6 + "的领土" + text5.Trim() + "的围攻中失利。")) : (won ? ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "参与的")) + text5.Trim() + "保卫战中击退了" + text6 + "。") : ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "参与的")) + text5.Trim() + "保卫战中败给了" + text6 + "。")));
		}
		else if (mapEvent.IsRaid)
		{
			text4 = (won ? ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "于")) + locationLabel + "对" + text2 + "发动的袭掠中得手。") : ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "于")) + locationLabel + "对" + text2 + "发动的袭掠中失利。"));
		}
		else
		{
			text4 = (won ? ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "于")) + locationLabel + "击败了" + text2 + "。") : ("你作为" + text + "的领主，在" + (string.IsNullOrWhiteSpace(text3) ? "" : (text3 + "于")) + locationLabel + "败给了" + text2 + "。"));
		}
		StringBuilder stringBuilder = new StringBuilder(text4);
		string text7 = BuildTrackedHeroListText(side, 5);
		string text8 = BuildTrackedHeroListText(side.OtherSide, 5);
		if (!string.IsNullOrWhiteSpace(text7))
		{
			stringBuilder.Append(" 我方领主：").Append(text7).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			stringBuilder.Append(" 敌方领主：").Append(text8).Append('。');
		}
		string text9 = BuildMapEventCommittedTroopText(side);
		string text10 = BuildMapEventCommittedTroopText(side.OtherSide);
		if (!string.IsNullOrWhiteSpace(text9) || !string.IsNullOrWhiteSpace(text10))
		{
			stringBuilder.Append(" 战前投入兵力：我方").Append(string.IsNullOrWhiteSpace(text9) ? "不详" : text9);
			stringBuilder.Append("；敌方").Append(string.IsNullOrWhiteSpace(text10) ? "不详" : text10).Append('。');
		}
		string text11 = BuildMapEventCasualtyText(side);
		string text12 = BuildMapEventCasualtyText(side.OtherSide);
		stringBuilder.Append(" 我方死伤：").Append(text11);
		stringBuilder.Append("；敌方死伤：").Append(text12).Append('。');
		string text13 = BuildMapEventStandoutText(side);
		if (!string.IsNullOrWhiteSpace(text13))
		{
			stringBuilder.Append(" ：").Append(text13);
		}
		return stringBuilder.ToString();
	}

	private static string BuildPlayerAddressedInput(Hero hero, string playerText)
	{
		string text = (playerText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = BuildPlayerPublicDisplayNameForPrompt();
		string text3 = hero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "该NPC";
		}
		return text2 + "对" + text3 + "说: " + text;
	}

	private const string SceneHistorySessionMarkerPrefix = "[AF_SCENE_SESSION:";

	private static string TagSceneSessionHistoryLine(string line, int sceneSessionId)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || sceneSessionId < 0)
		{
			return text;
		}
		return $"{SceneHistorySessionMarkerPrefix}{sceneSessionId}] {text}";
	}

	private static bool TryStripSceneSessionHistoryMarker(string line, out string stripped, out int sceneSessionId)
	{
		stripped = (line ?? "").Trim();
		sceneSessionId = -1;
		if (string.IsNullOrWhiteSpace(stripped) || !stripped.StartsWith(SceneHistorySessionMarkerPrefix, StringComparison.Ordinal))
		{
			return false;
		}
		int num = stripped.IndexOf(']');
		if (num <= SceneHistorySessionMarkerPrefix.Length)
		{
			return false;
		}
		string s = stripped.Substring(SceneHistorySessionMarkerPrefix.Length, num - SceneHistorySessionMarkerPrefix.Length).Trim();
		if (!int.TryParse(s, out sceneSessionId))
		{
			sceneSessionId = -1;
			return false;
		}
		stripped = stripped.Substring(num + 1).TrimStart();
		return true;
	}

	private static bool IsActiveSceneSessionHistoryLine(string line)
	{
		if (Mission.Current == null || !ShoutUtils.IsInValidScene())
		{
			return false;
		}
		if (!TryStripSceneSessionHistoryMarker(line, out var _, out var sceneSessionId))
		{
			return false;
		}
		return sceneSessionId == ShoutBehavior.GetCurrentSceneHistorySessionIdForExternal();
	}

	private static string BuildPlayerPublicDisplayNameForPrompt()
	{
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return "玩家";
		}
		try
		{
			int num = mainHero.Clan?.Tier ?? 0;
			if (num >= 2)
			{
				string text = (mainHero.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		string text2 = "未知";
		try
		{
			string text3 = (mainHero.Culture?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text2 = text3;
			}
		}
		catch
		{
		}
		string text4 = "未知";
		try
		{
			text4 = BuildAgeBracketLabel(mainHero.Age);
		}
		catch
		{
			text4 = "未知";
		}
		return text2 + text4;
	}

	public static string BuildPlayerPublicDisplayNameForExternal()
	{
		return BuildPlayerPublicDisplayNameForPrompt();
	}

	private static string NormalizePlayerHistoryLineForPrompt(string line, string targetDisplayName, bool addressToYou = true)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		TryStripSceneSessionHistoryMarker(text, out text, out var _);
		if (!TryStripPlayerSpeechPrefix(text, out var stripped))
		{
			return text;
		}
		string text2 = BuildPlayerPublicDisplayNameForPrompt();
		string text3 = addressToYou ? "你" : ((targetDisplayName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "该NPC";
		}
		return text2 + "对" + text3 + "说: " + stripped;
	}

	private static bool TryStripPlayerSpeechPrefix(string line, out string stripped)
	{
		stripped = "";
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		TryStripSceneSessionHistoryMarker(text, out text, out var _);
		if (text.StartsWith("玩家:", StringComparison.Ordinal))
		{
			stripped = text.Substring("玩家:".Length).Trim();
			return true;
		}
		if (text.StartsWith("玩家：", StringComparison.Ordinal))
		{
			stripped = text.Substring("玩家：".Length).Trim();
			return true;
		}
		int num = text.IndexOf("（player）对", StringComparison.Ordinal);
		if (num >= 0)
		{
			int num2 = text.IndexOf("说:", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				num2 = text.IndexOf("说：", num, StringComparison.Ordinal);
			}
			if (num2 >= 0)
			{
				stripped = text.Substring(num2 + 2).Trim();
				return true;
			}
		}
		return false;
	}

	private static string BuildSiegeStartNarrative(Settlement settlement, bool isAttacker, SiegeEvent siegeEvent)
	{
		string text = settlement?.Name?.ToString() ?? "某处要塞";
		string text2 = GetFactionDisplayName(settlement?.MapFaction, "守军");
		string text3 = GetFactionDisplayName(siegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction, "攻方");
		StringBuilder stringBuilder = new StringBuilder(isAttacker ? ("你参与了对" + text2 + "领土" + text + "的围攻。") : ("你参与了" + text + "的守城，对抗" + text3 + "。"));
		string text4 = BuildTrackedHeroListText(siegeEvent, BattleSideEnum.Attacker, 5);
		string text5 = BuildTrackedHeroListText(siegeEvent, BattleSideEnum.Defender, 5);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.Append(" 攻方领主：").Append(text4).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			stringBuilder.Append(" 守方领主：").Append(text5).Append('。');
		}
		return stringBuilder.ToString();
	}

	private void TrackNpcActionsFromMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.HasWinner)
		{
			return;
		}
		int mapEventTroopCount = GetMapEventTroopCount(mapEvent);
		bool flag = mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut || mapEvent.IsRaid || mapEventTroopCount >= 120 || CountTrackedLordParties(mapEvent.AttackerSide) + CountTrackedLordParties(mapEvent.DefenderSide) >= 2;
		string mapEventLocationLabel = GetMapEventLocationLabel(mapEvent);
		TrackNpcActionsFromMapEventSide(mapEvent, mapEvent.AttackerSide, mapEvent.WinningSide == BattleSideEnum.Attacker, flag, mapEventLocationLabel);
		TrackNpcActionsFromMapEventSide(mapEvent, mapEvent.DefenderSide, mapEvent.WinningSide == BattleSideEnum.Defender, flag, mapEventLocationLabel);
	}

	private void TrackNpcActionsFromMapEventSide(MapEvent mapEvent, MapEventSide side, bool won, bool isMajor, string locationLabel)
	{
		if (mapEvent == null || side?.Parties == null)
		{
			return;
		}
		foreach (MapEventParty party in side.Parties)
		{
			Hero leaderHero = party?.Party?.LeaderHero;
			if (!ShouldTrackNpcActionHero(leaderHero))
			{
				continue;
			}
			string text = BuildMapEventNarrative(mapEvent, side, leaderHero, won, locationLabel);
			string text2 = "mapevent:" + (mapEvent.StringId ?? locationLabel) + ":" + won + ":" + (leaderHero.StringId ?? "");
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("map_event", leaderHero);
			npcActionFacts.LocationText = locationLabel;
			npcActionFacts.Won = won;
			ApplySettlementFacts(npcActionFacts, mapEvent.MapEventSettlement, locationText: locationLabel);
			AddRelatedFactionFacts(npcActionFacts, side.OtherSide?.MapFaction);
			if (isMajor)
			{
				RecordNpcMajorAction(leaderHero, text, text2, npcActionFacts);
			}
			RecordNpcRecentAction(leaderHero, text, text2, facts: npcActionFacts);
			string text3 = BuildMapEventAftermathText(mapEvent, side, won, locationLabel);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("map_event_aftermath", leaderHero);
				npcActionFacts2.LocationText = locationLabel;
				npcActionFacts2.Won = won;
				ApplySettlementFacts(npcActionFacts2, mapEvent.MapEventSettlement, locationText: locationLabel);
				AddRelatedFactionFacts(npcActionFacts2, side.OtherSide?.MapFaction);
				RecordNpcRecentAction(leaderHero, text3, "mapevent_aftermath:" + (mapEvent.StringId ?? locationLabel) + ":" + won + ":" + (leaderHero.StringId ?? ""), facts: npcActionFacts2);
			}
		}
	}

	private static string GetNearestSettlementNameForParty(MobileParty party)
	{
		Settlement settlement = party?.BesiegedSettlement ?? ResolveSiegeSettlement(party?.SiegeEvent);
		if (settlement == null)
		{
			settlement = party?.Army?.LeaderParty?.BesiegedSettlement ?? ResolveSiegeSettlement(party?.Army?.LeaderParty?.SiegeEvent);
		}
		string text = settlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.Army?.LeaderParty?.TargetSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.TargetSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.CurrentSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.LastVisitedSettlement?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		text = party?.BesiegedSettlement?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? "附近一带" : text.Trim();
	}

	private static string BuildRecentPartyBehaviorText(MobileParty party)
	{
		if (party == null)
		{
			return "";
		}
		MobileParty mobileParty = party.Army?.LeaderParty ?? party;
		string text = GetNearestSettlementNameForParty(party);
		if (party.Army != null)
		{
			string text2 = (party.Army.LeaderParty == party) ? "你最近正率领" : "你最近正随";
			string text3 = GetArmyDisplayName(party.Army);
			switch (mobileParty?.DefaultBehavior)
			{
			case TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement:
				return text2 + text3 + "围攻" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement:
				return text2 + text3 + "强攻" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.DefendSettlement:
				return text2 + text3 + "守备" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.GoToSettlement:
				return text2 + text3 + "前往" + text + "。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.PatrolAroundPoint:
				return text2 + text3 + "在" + text + "附近巡逻。";
			case TaleWorlds.CampaignSystem.Party.AiBehavior.EngageParty:
				{
					string text4 = mobileParty?.TargetParty?.LeaderHero?.Name?.ToString() ?? mobileParty?.TargetParty?.Name?.ToString();
					return string.IsNullOrWhiteSpace(text4) ? (text2 + text3 + "追击一支部队。") : (text2 + text3 + "追击" + text4.Trim() + "。");
				}
			default:
				return text2 + text3 + "在" + text + "一带行动。";
			}
		}
		switch (party.DefaultBehavior)
		{
		case TaleWorlds.CampaignSystem.Party.AiBehavior.BesiegeSettlement:
			return "你最近在围攻" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.AssaultSettlement:
			return "你最近在强攻" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.DefendSettlement:
			return "你最近在守备" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.RaidSettlement:
			return "你最近在袭扰" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.PatrolAroundPoint:
			return "你最近在" + text + "附近巡逻。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.GoToSettlement:
			return "你最近正前往" + text + "。";
		case TaleWorlds.CampaignSystem.Party.AiBehavior.EngageParty:
			{
				string text2 = party.TargetParty?.LeaderHero?.Name?.ToString() ?? party.TargetParty?.Name?.ToString();
				return string.IsNullOrWhiteSpace(text2) ? "你最近在追击一支部队。" : ("你最近在追击" + text2.Trim() + "。");
			}
		case TaleWorlds.CampaignSystem.Party.AiBehavior.EscortParty:
			{
				string text3 = party.TargetParty?.LeaderHero?.Name?.ToString() ?? party.TargetParty?.Name?.ToString();
				return string.IsNullOrWhiteSpace(text3) ? "你最近在护送一支部队。" : ("你最近在护送" + text3.Trim() + "。");
			}
		default:
			return "";
		}
	}

	private static string BuildRecentPartyBehaviorStableKey(MobileParty party)
	{
		if (party == null)
		{
			return "";
		}
		MobileParty mobileParty = party.Army?.LeaderParty ?? party;
		string text = GetNearestSettlementNameForParty(party);
		string text2 = mobileParty?.TargetParty?.StringId ?? mobileParty?.TargetParty?.Name?.ToString() ?? "";
		string text3 = (party.Army != null) ? GetArmyDisplayName(party.Army) : "";
		if (party.Army != null)
		{
			return "daily_behavior:army:" + (mobileParty?.DefaultBehavior).GetValueOrDefault() + ":" + text + ":" + text2 + ":" + text3;
		}
		return "daily_behavior:" + party.DefaultBehavior + ":" + text + ":" + text2 + ":" + text3;
	}

	private string BuildNpcActionSummary(Hero hero, bool recentOnly)
	{
		string npcActionHeroKey = GetNpcActionHeroKey(hero);
		if (string.IsNullOrWhiteSpace(npcActionHeroKey))
		{
			return "";
		}
		Dictionary<string, List<NpcActionEntry>> dictionary = (recentOnly ? _npcRecentActions : _npcMajorActions);
		if (dictionary == null || !dictionary.TryGetValue(npcActionHeroKey, out var value) || value == null || value.Count <= 0)
		{
			return "";
		}
		List<NpcActionEntry> list = SanitizeNpcActionEntries(value, keepOnlyRecentWindow: recentOnly);
		if (list.Count <= 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = int.MinValue;
		string text = null;
		foreach (NpcActionEntry item in list)
		{
			string text2 = !string.IsNullOrWhiteSpace(item.GameDate) ? item.GameDate.Trim() : ("第 " + item.Day + " 日");
			if (item.Day != num || !string.Equals(text, text2, StringComparison.Ordinal))
			{
				if (stringBuilder.Length > 0)
			{
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("—— " + text2 + " ——");
			num = item.Day;
			text = text2;
		}
			stringBuilder.AppendLine("- " + RenderNpcActionPromptText(hero, item.Text) + BuildNpcActionMetadataNarrativeSuffix(item));
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string RenderNpcActionPromptText(Hero hero, string rawText)
	{
		string text = (rawText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = hero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "该人物";
		}
		if (text.StartsWith("你", StringComparison.Ordinal))
		{
			return text2 + text.Substring(1);
		}
		if (text.StartsWith(text2, StringComparison.Ordinal))
		{
			return text;
		}
		return text2 + "：" + text;
	}

	private static string BuildNpcActionMetadataNarrativeSuffix(NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		string text = TranslateNpcActionKindForPrompt(entry.ActionKind);
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add("这属于" + text);
		}
		string text2 = ResolveDisplayNameBySettlementEntry(entry);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("事情发生在" + text2);
		}
		string text3 = ResolveHeroName(entry.TargetHeroId);
		if (!string.IsNullOrWhiteSpace(text3))
		{
			list.Add("主要涉及的人物是" + text3);
		}
		string text4 = ResolveClanName(entry.TargetClanId);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add("对方家族是" + text4);
		}
		string text5 = ResolveKingdomName(entry.TargetKingdomId);
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("对方所属王国是" + text5);
		}
		string text6 = ResolveClanName(entry.SettlementOwnerClanId);
		string text7 = ResolveKingdomName(entry.SettlementOwnerKingdomId);
		if (!string.IsNullOrWhiteSpace(text6) && !string.IsNullOrWhiteSpace(text7))
		{
			list.Add("当时该定居点由" + text6 + "掌控，隶属于" + text7);
		}
		else if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("当时该定居点由" + text6 + "掌控");
		}
		else if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("当时该定居点隶属于" + text7);
		}
		string text8 = ResolveClanName(entry.PreviousSettlementOwnerClanId);
		string text9 = ResolveKingdomName(entry.PreviousSettlementOwnerKingdomId);
		if (!string.IsNullOrWhiteSpace(text8) && !string.IsNullOrWhiteSpace(text9))
		{
			list.Add("此前这里由" + text8 + "掌控，归属" + text9);
		}
		else if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("此前这里由" + text8 + "掌控");
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("此前这里归属" + text9);
		}
		if (entry.Won.HasValue)
		{
			list.Add("结果是" + (entry.Won.Value ? "获胜" : "失利"));
		}
		if (entry.IsMajor)
		{
			list.Add("这是一件重大行动");
		}
		if (list.Count <= 0)
		{
			return "";
		}
		return " " + string.Join("；", list) + "。";
	}

	private static string TranslateNpcActionKindForPrompt(string actionKind)
	{
		switch ((actionKind ?? "").Trim().ToLowerInvariant())
		{
		case "army_create":
			return "组建军团的行动";
		case "army_gather":
			return "军团集结行动";
		case "army_disperse":
			return "军团解散行动";
		case "army_join":
			return "加入军团的行动";
		case "army_leave":
			return "离开军团的行动";
		case "siege_start_attack":
			return "参与围攻的行动";
		case "siege_start_defend":
			return "参与守城的行动";
		case "siege_end_attack":
			return "围城结束后的攻方行动";
		case "siege_end_defend":
			return "围城结束后的守方行动";
		case "siege_join":
			return "加入围城的行动";
		case "siege_leave":
			return "离开围城的行动";
		case "siege_complete":
			return "围城结果事件";
		case "daily_behavior":
			return "近期行军动向";
		case "map_event":
			return "战场交锋";
		case "map_event_aftermath":
			return "战后余波";
		case "marriage":
			return "联姻事件";
		case "clan_changed_kingdom":
			return "家族更换效忠对象的事件";
		case "clan_defected":
			return "家族叛逃事件";
		case "kingdom_decision_concluded":
			return "王国决议事件";
		case "ruling_clan_changed":
			return "执政家族变更事件";
		case "settlement_owner_changed_gain":
			return "定居点归属增加事件";
		case "settlement_owner_changed_loss":
			return "定居点归属失去事件";
		case "settlement_owner_changed_capture":
			return "定居点易主事件";
		case "hero_killed":
			return "英雄死亡事件";
		case "clan_member_killed":
			return "家族成员死亡事件";
		case "prisoner_taken_captor":
			return "俘获领主事件";
		case "prisoner_taken_prisoner":
			return "被俘事件";
		case "prisoner_released_captor":
			return "囚犯获释事件";
		case "prisoner_released_prisoner":
			return "结束囚禁事件";
		case "birth":
			return "家族新生事件";
		case "clan_leader_changed":
			return "家族族长更替事件";
		default:
			return "";
		}
	}

	private static string ResolveDisplayNameBySettlementEntry(NpcActionEntry entry)
	{
		string text = (entry?.LocationText ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (entry?.SettlementName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (entry?.SettlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		try
		{
			Settlement settlement = Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
			return (settlement?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveHeroName(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (hero?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveClanName(string clanId)
	{
		string text = (clanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Clan clan = Clan.All.FirstOrDefault((Clan x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (clan?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveKingdomName(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Kingdom kingdom = Kingdom.All.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (kingdom?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private string BuildNpcMajorActionsRuntimeInstruction(Hero hero)
	{
		string text = BuildNpcActionSummary(hero, recentOnly: false);
		string stateKey = (string.IsNullOrWhiteSpace(text) ? "no_data" : "has_data");
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["majorActionSummary"] = text ?? ""
		};
		return AIConfigHandler.ResolveRuleRuntimeText("npc_major_actions", stateKey, forConstraint: false, dictionary);
	}

	private string BuildNpcRecentActionsRuntimeInstruction(Hero hero)
	{
		string text = BuildNpcActionSummary(hero, recentOnly: true);
		string stateKey = (string.IsNullOrWhiteSpace(text) ? "no_data" : "has_data");
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["recentActionSummary"] = text ?? ""
		};
		return AIConfigHandler.ResolveRuleRuntimeText("npc_recent_actions", stateKey, forConstraint: false, dictionary);
	}

	private string BuildNpcActionsRuntimeConstraintHint(Hero hero, bool recentOnly)
	{
		string text = BuildNpcActionSummary(hero, recentOnly);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return AIConfigHandler.ResolveRuleRuntimeText(recentOnly ? "npc_recent_actions" : "npc_major_actions", "no_data", forConstraint: true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
	}

	private string BuildNpcCurrentActionFact(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return "";
			}
			string text = BuildRecentPartyBehaviorText(hero.PartyBelongedTo);
			if (string.IsNullOrWhiteSpace(text))
			{
				string text2 = (hero.CurrentSettlement?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					text = "你当前在" + text2 + "停留。";
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				string npcActionHeroKey = GetNpcActionHeroKey(hero);
				if (!string.IsNullOrWhiteSpace(npcActionHeroKey) && _npcRecentActions != null && _npcRecentActions.TryGetValue(npcActionHeroKey, out var value) && value != null)
				{
					NpcActionEntry npcActionEntry = SanitizeNpcActionEntries(value, keepOnlyRecentWindow: true).LastOrDefault();
					if (npcActionEntry != null && npcActionEntry.Day >= GetCurrentGameDayIndexSafe() - 1)
					{
						text = npcActionEntry.Text;
					}
				}
			}
			text = RenderNpcActionPromptText(hero, text);
			return string.IsNullOrWhiteSpace(text) ? "" : (text);
		}
		catch
		{
			return "";
		}
	}

	private void OnCampaignTick(float dt)
	{
		try
		{
			ProcessPendingWeeklyReportManualRetryResult();
			ProcessWeeklyReportUiResume();
			int num = 0;
			try
			{
				num = Clan.PlayerClan?.Tier ?? 0;
			}
			catch
			{
			}
			if (num <= 0)
			{
				try
				{
					num = (Hero.MainHero?.Clan?.Tier).GetValueOrDefault();
				}
				catch
				{
				}
			}
			_cachedPlayerClanTier = num;
			_cachedPlayerClanTierUtcTicks = DateTime.UtcNow.Ticks;
		}
		catch
		{
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_shownRecords == null)
		{
			_shownRecords = new Dictionary<string, HeroShownRecord>();
		}
		if (_shownRecordStorage == null)
		{
			_shownRecordStorage = new Dictionary<string, string>();
		}
		if (_dialogueHistory == null)
		{
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
		}
		if (_dialogueHistoryStorage == null)
		{
			_dialogueHistoryStorage = new Dictionary<string, string>();
		}
		if (_npcMajorActions == null)
		{
			_npcMajorActions = new Dictionary<string, List<NpcActionEntry>>();
		}
		if (_npcMajorActionStorage == null)
		{
			_npcMajorActionStorage = new Dictionary<string, string>();
		}
		if (_npcRecentActions == null)
		{
			_npcRecentActions = new Dictionary<string, List<NpcActionEntry>>();
		}
		if (_npcRecentActionStorage == null)
		{
			_npcRecentActionStorage = new Dictionary<string, string>();
		}
		if (_npcActionGlobalOrderCounter < 0)
		{
			_npcActionGlobalOrderCounter = 0;
		}
		if (_npcPersonaProfiles == null)
		{
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
		}
		if (_npcPersonaProfileStorage == null)
		{
			_npcPersonaProfileStorage = new Dictionary<string, string>();
		}
		if (_eventKingdomOpeningSummaries == null)
		{
			_eventKingdomOpeningSummaries = new Dictionary<string, string>();
		}
		if (_eventKingdomOpeningSummaryStorage == null)
		{
			_eventKingdomOpeningSummaryStorage = new Dictionary<string, string>();
		}
		if (_eventWorldOpeningSummary == null)
		{
			_eventWorldOpeningSummary = "";
		}
		if (_eventRecordEntries == null)
		{
			_eventRecordEntries = new List<EventRecordEntry>();
		}
		if (_eventRecordJsonStorage == null)
		{
			_eventRecordJsonStorage = "";
		}
		if (_eventSourceMaterials == null)
		{
			_eventSourceMaterials = new List<EventSourceMaterialEntry>();
		}
		if (_eventSourceMaterialJsonStorage == null)
		{
			_eventSourceMaterialJsonStorage = "";
		}
		if (_kingdomStabilityValues == null)
		{
			_kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_kingdomStabilityStorage == null)
		{
			_kingdomStabilityStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_kingdomStabilityRelationAppliedOffsets == null)
		{
			_kingdomStabilityRelationAppliedOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_kingdomStabilityRelationOffsetStorage == null)
		{
			_kingdomStabilityRelationOffsetStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_weeklyReportAppliedStabilityDeltas == null)
		{
			_weeklyReportAppliedStabilityDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_weeklyReportAppliedStabilityDeltaStorage == null)
		{
			_weeklyReportAppliedStabilityDeltaStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_lastAutoGeneratedWeeklyReportWeek < -1)
		{
			_lastAutoGeneratedWeeklyReportWeek = -1;
		}
		if (_lastProcessedKingdomRebellionWeek < -1)
		{
			_lastProcessedKingdomRebellionWeek = -1;
		}
		if (_voiceMappingJsonStorage == null)
		{
			_voiceMappingJsonStorage = "";
		}
		if (_voiceMappingExportFolderStorage == null)
		{
			_voiceMappingExportFolderStorage = "";
		}
		if (_unnamedPersonaJsonStorage == null)
		{
			_unnamedPersonaJsonStorage = "";
		}
		try
		{
			if (dataStore != null && dataStore.IsSaving)
			{
				_shownRecordStorage.Clear();
				foreach (KeyValuePair<string, HeroShownRecord> shownRecord in _shownRecords)
				{
					if (string.IsNullOrWhiteSpace(shownRecord.Key) || shownRecord.Value == null)
					{
						continue;
					}
					bool flag = Math.Max(0, shownRecord.Value.ShownGold) > 0;
					if (!flag && shownRecord.Value.ShownItems != null)
					{
						foreach (KeyValuePair<string, int> shownItem in shownRecord.Value.ShownItems)
						{
							if (!string.IsNullOrWhiteSpace(shownItem.Key) && shownItem.Value > 0)
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						continue;
					}
					try
					{
						_shownRecordStorage[shownRecord.Key] = JsonConvert.SerializeObject(shownRecord.Value);
					}
					catch (Exception ex)
					{
						Logger.Log("TradeShown", "[ERROR] Serialize shown record for " + shownRecord.Key + ": " + ex.Message);
					}
				}
				Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_shownRecordStorage);
				dataStore.SyncData("_shownRecords_v1", ref dictionary);
				_dialogueHistoryStorage.Clear();
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						try
						{
							string value = JsonConvert.SerializeObject(item.Value);
							_dialogueHistoryStorage[item.Key] = value;
						}
						catch (Exception ex)
						{
							Logger.Log("DialogueHistory", "[ERROR] Serialize history for " + item.Key + ": " + ex.Message);
						}
					}
				}
				Dictionary<string, string> dictionary2 = CampaignSaveChunkHelper.FlattenStringDictionary(_dialogueHistoryStorage);
				dataStore.SyncData("_dialogueHistory_v2", ref dictionary2);
				_npcMajorActionStorage.Clear();
				foreach (KeyValuePair<string, List<NpcActionEntry>> npcMajorAction in _npcMajorActions)
				{
					if (!string.IsNullOrEmpty(npcMajorAction.Key) && npcMajorAction.Value != null && npcMajorAction.Value.Count > 0)
					{
						try
						{
							string value2 = JsonConvert.SerializeObject(npcMajorAction.Value);
							_npcMajorActionStorage[npcMajorAction.Key] = value2;
						}
						catch (Exception ex2)
						{
							Logger.Log("NpcAction", "[ERROR] Serialize major actions for " + npcMajorAction.Key + ": " + ex2.Message);
						}
					}
				}
				Dictionary<string, string> dictionary3 = CampaignSaveChunkHelper.FlattenStringDictionary(_npcMajorActionStorage);
				dataStore.SyncData("_npcMajorActions_v1", ref dictionary3);
				_npcRecentActionStorage.Clear();
				foreach (KeyValuePair<string, List<NpcActionEntry>> npcRecentAction in _npcRecentActions)
				{
					if (!string.IsNullOrEmpty(npcRecentAction.Key) && npcRecentAction.Value != null && npcRecentAction.Value.Count > 0)
					{
						try
						{
							string value3 = JsonConvert.SerializeObject(npcRecentAction.Value);
							_npcRecentActionStorage[npcRecentAction.Key] = value3;
						}
						catch (Exception ex3)
						{
							Logger.Log("NpcAction", "[ERROR] Serialize recent actions for " + npcRecentAction.Key + ": " + ex3.Message);
						}
					}
				}
				Dictionary<string, string> dictionary4 = CampaignSaveChunkHelper.FlattenStringDictionary(_npcRecentActionStorage);
				dataStore.SyncData("_npcRecentActions_v1", ref dictionary4);
				dataStore.SyncData("_npcActionGlobalOrderCounter_v1", ref _npcActionGlobalOrderCounter);
				_npcPersonaProfileStorage.Clear();
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile2 in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile2.Key) && npcPersonaProfile2.Value != null)
					{
						try
						{
							string value4 = JsonConvert.SerializeObject(npcPersonaProfile2.Value);
							_npcPersonaProfileStorage[npcPersonaProfile2.Key] = value4;
						}
						catch (Exception ex4)
						{
							Logger.Log("NpcPersona", "[ERROR] Serialize profile for " + npcPersonaProfile2.Key + ": " + ex4.Message);
						}
					}
				}
				Dictionary<string, string> dictionary5 = CampaignSaveChunkHelper.FlattenStringDictionary(_npcPersonaProfileStorage);
				dataStore.SyncData("_npcPersonaProfiles_v1", ref dictionary5);
				_eventKingdomOpeningSummaryStorage.Clear();
				foreach (KeyValuePair<string, string> item2 in _eventKingdomOpeningSummaries)
				{
					string text = (item2.Key ?? "").Trim();
					string text2 = (item2.Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
					{
						_eventKingdomOpeningSummaryStorage[text] = text2;
					}
				}
				Dictionary<string, string> dictionary6 = CampaignSaveChunkHelper.FlattenStringDictionary(_eventKingdomOpeningSummaryStorage);
				dataStore.SyncData("_eventKingdomOpeningSummaries_v1", ref dictionary6);
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventWorldOpeningSummary_v1", _eventWorldOpeningSummary ?? "", "EventOpeningSummary");
				try
				{
					_eventRecordJsonStorage = JsonConvert.SerializeObject(SanitizeEventRecordEntries(_eventRecordEntries));
				}
				catch (Exception ex5)
				{
					_eventRecordJsonStorage = "[]";
					Logger.Log("EventRecord", "[ERROR] Serialize event records failed: " + ex5.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventRecordEntries_v1", _eventRecordJsonStorage ?? "[]", "EventRecord");
				try
				{
					_eventSourceMaterialJsonStorage = JsonConvert.SerializeObject(SanitizeEventSourceMaterials(_eventSourceMaterials));
				}
				catch (Exception ex6)
				{
					_eventSourceMaterialJsonStorage = "[]";
					Logger.Log("EventMaterial", "[ERROR] Serialize event source materials failed: " + ex6.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventSourceMaterials_v1", _eventSourceMaterialJsonStorage ?? "[]", "EventMaterial");
				_kingdomStabilityStorage.Clear();
				foreach (KeyValuePair<string, int> kingdomStabilityValue in _kingdomStabilityValues)
				{
					string text3 = (kingdomStabilityValue.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						_kingdomStabilityStorage[text3] = ClampKingdomStabilityValue(kingdomStabilityValue.Value).ToString();
					}
				}
				Dictionary<string, string> dictionary13 = CampaignSaveChunkHelper.FlattenStringDictionary(_kingdomStabilityStorage);
				dataStore.SyncData("_kingdomStability_v1", ref dictionary13);
				_kingdomStabilityRelationOffsetStorage.Clear();
				foreach (KeyValuePair<string, int> kingdomStabilityRelationAppliedOffset in _kingdomStabilityRelationAppliedOffsets)
				{
					string text4 = (kingdomStabilityRelationAppliedOffset.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						_kingdomStabilityRelationOffsetStorage[text4] = kingdomStabilityRelationAppliedOffset.Value.ToString();
					}
				}
				Dictionary<string, string> dictionary14b = CampaignSaveChunkHelper.FlattenStringDictionary(_kingdomStabilityRelationOffsetStorage);
				dataStore.SyncData("_kingdomStabilityRelationOffsets_v1", ref dictionary14b);
				_weeklyReportAppliedStabilityDeltaStorage.Clear();
				foreach (KeyValuePair<string, int> weeklyReportAppliedStabilityDelta in _weeklyReportAppliedStabilityDeltas)
				{
					string text5 = (weeklyReportAppliedStabilityDelta.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5))
					{
						_weeklyReportAppliedStabilityDeltaStorage[text5] = weeklyReportAppliedStabilityDelta.Value.ToString();
					}
				}
				Dictionary<string, string> dictionary14c = CampaignSaveChunkHelper.FlattenStringDictionary(_weeklyReportAppliedStabilityDeltaStorage);
				dataStore.SyncData("_weeklyReportAppliedStabilityDeltas_v1", ref dictionary14c);
				dataStore.SyncData("_lastAutoGeneratedWeeklyReportWeek_v1", ref _lastAutoGeneratedWeeklyReportWeek);
				dataStore.SyncData("_lastProcessedKingdomRebellionWeek_v1", ref _lastProcessedKingdomRebellionWeek);
				try
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				catch (Exception ex7)
				{
					_voiceMappingJsonStorage = "";
					Logger.Log("VoiceMapper", "[ERROR] Serialize voice mapping for save failed: " + ex7.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_voiceMapping_v1", _voiceMappingJsonStorage, "VoiceMapper");
				_voiceMappingExportFolderStorage = VoiceMapper.GetPreferredExportFolder() ?? "";
				dataStore.SyncData("_voiceMapping_export_folder_v1", ref _voiceMappingExportFolderStorage);
				try
				{
					_unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false) ?? "";
				}
				catch (Exception ex8)
				{
					_unnamedPersonaJsonStorage = "";
					Logger.Log("UnnamedPersona", "[ERROR] Serialize unnamed persona for save failed: " + ex8.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_unnamed_persona_v1", _unnamedPersonaJsonStorage, "UnnamedPersona");
				SyncPatienceData(dataStore);
				return;
			}
			_shownRecords.Clear();
			_shownRecordStorage.Clear();
			Dictionary<string, string> dictionary7 = new Dictionary<string, string>();
			dataStore.SyncData("_shownRecords_v1", ref dictionary7);
			_shownRecordStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary7, "TradeShown");
			if (_shownRecordStorage != null)
			{
				foreach (KeyValuePair<string, string> shownRecord2 in _shownRecordStorage)
				{
					if (string.IsNullOrWhiteSpace(shownRecord2.Key) || string.IsNullOrWhiteSpace(shownRecord2.Value))
					{
						continue;
					}
					try
					{
						HeroShownRecord heroShownRecord = JsonConvert.DeserializeObject<HeroShownRecord>(shownRecord2.Value);
						if (heroShownRecord != null)
						{
							if (heroShownRecord.ShownGold < 0)
							{
								heroShownRecord.ShownGold = 0;
							}
							if (heroShownRecord.ShownItems == null)
							{
								heroShownRecord.ShownItems = new Dictionary<string, int>();
							}
							else
							{
								heroShownRecord.ShownItems = heroShownRecord.ShownItems.Where((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).ToDictionary((KeyValuePair<string, int> x) => x.Key, (KeyValuePair<string, int> x) => x.Value);
							}
							_shownRecords[NormalizeShownRecordKey(shownRecord2.Key)] = heroShownRecord;
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("TradeShown", "[ERROR] Deserialize shown record for " + shownRecord2.Key + ": " + ex2.Message);
					}
				}
			}
			_dialogueHistory.Clear();
			_dialogueHistoryStorage.Clear();
			Dictionary<string, string> dictionary8 = new Dictionary<string, string>();
			dataStore.SyncData("_dialogueHistory_v2", ref dictionary8);
			_dialogueHistoryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary8, "DialogueHistory");
			if (_dialogueHistoryStorage != null)
			{
				foreach (KeyValuePair<string, string> item2 in _dialogueHistoryStorage)
				{
					if (string.IsNullOrEmpty(item2.Key) || string.IsNullOrEmpty(item2.Value))
					{
						continue;
					}
					try
					{
						List<DialogueDay> list = JsonConvert.DeserializeObject<List<DialogueDay>>(item2.Value);
						if (list != null)
						{
							_dialogueHistory[item2.Key] = list;
						}
					}
					catch (Exception ex3)
					{
						Logger.Log("DialogueHistory", "[ERROR] Deserialize history for " + item2.Key + ": " + ex3.Message);
					}
				}
			}
			_npcMajorActions.Clear();
			_npcMajorActionStorage.Clear();
			Dictionary<string, string> dictionary9 = new Dictionary<string, string>();
			dataStore.SyncData("_npcMajorActions_v1", ref dictionary9);
			_npcMajorActionStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary9, "NpcAction");
			if (_npcMajorActionStorage != null)
			{
				foreach (KeyValuePair<string, string> item3 in _npcMajorActionStorage)
				{
					if (string.IsNullOrEmpty(item3.Key) || string.IsNullOrEmpty(item3.Value))
					{
						continue;
					}
					try
					{
						List<NpcActionEntry> list2 = JsonConvert.DeserializeObject<List<NpcActionEntry>>(item3.Value) ?? new List<NpcActionEntry>();
						list2 = SanitizeNpcActionEntries(list2, keepOnlyRecentWindow: false);
						if (list2.Count > 0)
						{
							_npcMajorActions[item3.Key] = list2;
						}
					}
					catch (Exception ex4)
					{
						Logger.Log("NpcAction", "[ERROR] Deserialize major actions for " + item3.Key + ": " + ex4.Message);
					}
				}
			}
			_npcRecentActions.Clear();
			_npcRecentActionStorage.Clear();
			Dictionary<string, string> dictionary10 = new Dictionary<string, string>();
			dataStore.SyncData("_npcRecentActions_v1", ref dictionary10);
			_npcRecentActionStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary10, "NpcAction");
			if (_npcRecentActionStorage != null)
			{
				foreach (KeyValuePair<string, string> item4 in _npcRecentActionStorage)
				{
					if (string.IsNullOrEmpty(item4.Key) || string.IsNullOrEmpty(item4.Value))
					{
						continue;
					}
					try
					{
						List<NpcActionEntry> list3 = JsonConvert.DeserializeObject<List<NpcActionEntry>>(item4.Value) ?? new List<NpcActionEntry>();
						list3 = SanitizeNpcActionEntries(list3, keepOnlyRecentWindow: true);
						if (list3.Count > 0)
						{
							_npcRecentActions[item4.Key] = list3;
						}
					}
					catch (Exception ex5)
					{
						Logger.Log("NpcAction", "[ERROR] Deserialize recent actions for " + item4.Key + ": " + ex5.Message);
					}
				}
			}
			_npcPersonaProfiles.Clear();
			_npcPersonaProfileStorage.Clear();
			Dictionary<string, string> dictionary11 = new Dictionary<string, string>();
			dataStore.SyncData("_npcPersonaProfiles_v1", ref dictionary11);
			_npcPersonaProfileStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary11, "NpcPersona");
			if (_npcPersonaProfileStorage != null)
			{
				foreach (KeyValuePair<string, string> item5 in _npcPersonaProfileStorage)
				{
					if (string.IsNullOrEmpty(item5.Key) || string.IsNullOrEmpty(item5.Value))
					{
						continue;
					}
					try
					{
						NpcPersonaProfile npcPersonaProfile = JsonConvert.DeserializeObject<NpcPersonaProfile>(item5.Value);
						if (npcPersonaProfile != null)
						{
							_npcPersonaProfiles[item5.Key] = npcPersonaProfile;
						}
					}
					catch (Exception ex6)
					{
						Logger.Log("NpcPersona", "[ERROR] Deserialize profile for " + item5.Key + ": " + ex6.Message);
					}
				}
			}
			dataStore.SyncData("_npcActionGlobalOrderCounter_v1", ref _npcActionGlobalOrderCounter);
			NormalizeNpcActionSequences(_npcMajorActions);
			NormalizeNpcActionSequences(_npcRecentActions);
			_npcActionGlobalOrderCounter = Math.Max(_npcActionGlobalOrderCounter, GetMaxNpcActionSequence(_npcMajorActions, _npcRecentActions, _eventSourceMaterials));
			_eventKingdomOpeningSummaries.Clear();
			_eventKingdomOpeningSummaryStorage.Clear();
			Dictionary<string, string> dictionary12 = new Dictionary<string, string>();
			dataStore.SyncData("_eventKingdomOpeningSummaries_v1", ref dictionary12);
			_eventKingdomOpeningSummaryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary12, "EventOpeningSummary");
			if (_eventKingdomOpeningSummaryStorage != null)
			{
				foreach (KeyValuePair<string, string> item6 in _eventKingdomOpeningSummaryStorage)
				{
					string text3 = (item6.Key ?? "").Trim();
					string text4 = (item6.Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text4))
					{
						_eventKingdomOpeningSummaries[text3] = text4;
					}
				}
			}
			_eventWorldOpeningSummary = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventWorldOpeningSummary_v1", "EventOpeningSummary") ?? "";
			_eventRecordEntries.Clear();
			_eventRecordJsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventRecordEntries_v1", "EventRecord") ?? "";
			if (!string.IsNullOrWhiteSpace(_eventRecordJsonStorage))
			{
				try
				{
					List<EventRecordEntry> list4 = JsonConvert.DeserializeObject<List<EventRecordEntry>>(_eventRecordJsonStorage) ?? new List<EventRecordEntry>();
					_eventRecordEntries = SanitizeEventRecordEntries(list4);
				}
				catch (Exception ex7)
				{
					Logger.Log("EventRecord", "[ERROR] Deserialize event records failed: " + ex7.Message);
					_eventRecordEntries = new List<EventRecordEntry>();
				}
			}
			_eventSourceMaterials.Clear();
			_eventSourceMaterialJsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventSourceMaterials_v1", "EventMaterial") ?? "";
			if (!string.IsNullOrWhiteSpace(_eventSourceMaterialJsonStorage))
			{
				try
				{
					List<EventSourceMaterialEntry> list5 = JsonConvert.DeserializeObject<List<EventSourceMaterialEntry>>(_eventSourceMaterialJsonStorage) ?? new List<EventSourceMaterialEntry>();
					_eventSourceMaterials = SanitizeEventSourceMaterials(list5);
				}
				catch (Exception ex8)
				{
					Logger.Log("EventMaterial", "[ERROR] Deserialize event source materials failed: " + ex8.Message);
					_eventSourceMaterials = new List<EventSourceMaterialEntry>();
				}
			}
			dataStore.SyncData("_lastAutoGeneratedWeeklyReportWeek_v1", ref _lastAutoGeneratedWeeklyReportWeek);
			_kingdomStabilityValues.Clear();
			_kingdomStabilityStorage.Clear();
			Dictionary<string, string> dictionary14 = new Dictionary<string, string>();
			dataStore.SyncData("_kingdomStability_v1", ref dictionary14);
			_kingdomStabilityStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary14, "KingdomStability");
			if (_kingdomStabilityStorage != null)
			{
				foreach (KeyValuePair<string, string> item7 in _kingdomStabilityStorage)
				{
					string text5 = (item7.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5) && int.TryParse((item7.Value ?? "").Trim(), out var result))
					{
						_kingdomStabilityValues[text5] = ClampKingdomStabilityValue(result);
					}
				}
			}
			_kingdomStabilityRelationAppliedOffsets.Clear();
			_kingdomStabilityRelationOffsetStorage.Clear();
			Dictionary<string, string> dictionary15 = new Dictionary<string, string>();
			dataStore.SyncData("_kingdomStabilityRelationOffsets_v1", ref dictionary15);
			_kingdomStabilityRelationOffsetStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary15, "KingdomStabilityRelation");
			if (_kingdomStabilityRelationOffsetStorage != null)
			{
				foreach (KeyValuePair<string, string> item8 in _kingdomStabilityRelationOffsetStorage)
				{
					string text6 = (item8.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6) && int.TryParse((item8.Value ?? "").Trim(), out var result2))
					{
						_kingdomStabilityRelationAppliedOffsets[text6] = MBMath.ClampInt(result2, -100, 100);
					}
				}
			}
			_weeklyReportAppliedStabilityDeltas.Clear();
			_weeklyReportAppliedStabilityDeltaStorage.Clear();
			Dictionary<string, string> dictionary16 = new Dictionary<string, string>();
			dataStore.SyncData("_weeklyReportAppliedStabilityDeltas_v1", ref dictionary16);
			_weeklyReportAppliedStabilityDeltaStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary16, "WeeklyReportStability");
			if (_weeklyReportAppliedStabilityDeltaStorage != null)
			{
				foreach (KeyValuePair<string, string> item9 in _weeklyReportAppliedStabilityDeltaStorage)
				{
					string text7 = (item9.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text7) && int.TryParse((item9.Value ?? "").Trim(), out var result3))
					{
						_weeklyReportAppliedStabilityDeltas[text7] = result3;
					}
				}
			}
			dataStore.SyncData("_lastProcessedKingdomRebellionWeek_v1", ref _lastProcessedKingdomRebellionWeek);
			_voiceMappingExportFolderStorage = "";
			dataStore.SyncData("_voiceMapping_export_folder_v1", ref _voiceMappingExportFolderStorage);
			VoiceMapper.SetPreferredExportFolder(_voiceMappingExportFolderStorage);
			_voiceMappingJsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_voiceMapping_v1", "VoiceMapper");
			if (!string.IsNullOrWhiteSpace(_voiceMappingJsonStorage))
			{
				try
				{
					if (!VoiceMapper.ImportMappingJson(_voiceMappingJsonStorage))
					{
						Logger.Log("VoiceMapper", "[WARN] Save-loaded voice mapping was invalid; kept current file-backed mapping.");
					}
				}
				catch (Exception ex8)
				{
					Logger.Log("VoiceMapper", "[ERROR] Restore voice mapping from save failed: " + ex8.Message);
				}
			}
			_unnamedPersonaJsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_unnamed_persona_v1", "UnnamedPersona");
			try
			{
				ShoutUtils.ImportUnnamedPersonaStateJson(_unnamedPersonaJsonStorage, overwriteExisting: true);
			}
			catch (Exception ex9)
			{
				Logger.Log("UnnamedPersona", "[ERROR] Restore unnamed persona from save failed: " + ex9.Message);
			}
			SyncPatienceData(dataStore);
		}
		catch (Exception ex10)
		{
			Logger.Log("DialogueHistory", "[ERROR] SyncData v2 failed: " + ex10.ToString());
			_shownRecords = new Dictionary<string, HeroShownRecord>();
			_shownRecordStorage = new Dictionary<string, string>();
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			_dialogueHistoryStorage = new Dictionary<string, string>();
			_npcMajorActions = new Dictionary<string, List<NpcActionEntry>>();
			_npcMajorActionStorage = new Dictionary<string, string>();
			_npcRecentActions = new Dictionary<string, List<NpcActionEntry>>();
			_npcRecentActionStorage = new Dictionary<string, string>();
			_npcActionGlobalOrderCounter = 0;
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
			_npcPersonaProfileStorage = new Dictionary<string, string>();
			_eventKingdomOpeningSummaries = new Dictionary<string, string>();
			_eventKingdomOpeningSummaryStorage = new Dictionary<string, string>();
			_eventWorldOpeningSummary = "";
			_eventRecordEntries = new List<EventRecordEntry>();
			_eventRecordJsonStorage = "";
			_eventSourceMaterials = new List<EventSourceMaterialEntry>();
			_eventSourceMaterialJsonStorage = "";
			_kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_kingdomStabilityStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_kingdomStabilityRelationAppliedOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_kingdomStabilityRelationOffsetStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_weeklyReportAppliedStabilityDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_weeklyReportAppliedStabilityDeltaStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_lastAutoGeneratedWeeklyReportWeek = -1;
			_lastProcessedKingdomRebellionWeek = -1;
			_voiceMappingJsonStorage = "";
			_voiceMappingExportFolderStorage = "";
			_unnamedPersonaJsonStorage = "";
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AIConfigHandler.ReloadConfig();
		AIConfigHandler.TryStartBackgroundSemanticWarmup("session_launch");
		TryHookOverlayQuickTalkDisable();
		starter.AddGameMenu("AnimusForge_dev_root", "{=!}开发者工具", DevRootMenuInit, GameMenu.MenuOverlayType.SettlementWithBoth);
		starter.AddGameMenuOption("town", "AnimusForge_dev_root_entry", "【开发】数据管理", DevRootEntryCondition, DevRootEntryConsequence, isLeave: false, 99);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_hero", "HeroNPC编辑（领主/流浪者/同伴）", DevRootSubOptionCondition, DevRootHeroOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_nonhero", "非heroNPC编辑（士兵/平民/无名/无姓NPC）", DevRootSubOptionCondition, DevRootNonHeroOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_knowledge", "知识编辑", DevRootSubOptionCondition, DevRootKnowledgeOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_event", "事件编辑", DevRootSubOptionCondition, DevRootEventOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_all", "全部导出/导入", DevRootSubOptionCondition, DevRootAllOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_voice", "声音映射管理（VoiceMapping）", DevRootSubOptionCondition, DevRootVoiceMappingOptionConsequence);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_back", "返回", DevRootBackCondition, DevRootBackConsequence, isLeave: true);
	}

	private void TryHookOverlayQuickTalkDisable()
	{
		if (_overlayQuickTalkDisableHooked)
		{
			return;
		}
		try
		{
			EventManager eventManager = Game.Current?.EventManager;
			if (eventManager != null)
			{
				eventManager.RegisterEvent<SettlementOverylayQuickTalkPermissionEvent>(OnSettlementOverlayQuickTalkPermission);
				_overlayQuickTalkDisableHooked = true;
			}
		}
		catch
		{
		}
	}

	private void OnSettlementOverlayQuickTalkPermission(SettlementOverylayQuickTalkPermissionEvent e)
	{
		try
		{
			if (e != null && e.IsTalkAvailable != null)
			{
				Hero heroToTalkTo = e.HeroToTalkTo;
				if (heroToTalkTo != null && !heroToTalkTo.IsPlayerCompanion && (heroToTalkTo.IsLord || heroToTalkTo.IsNotable))
				{
					e.IsTalkAvailable(arg1: false, new TextObject("该交谈选项已被模组禁用，请使用造访进入场景后再互动。"));
				}
			}
		}
		catch
		{
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		if (mission != null && Mission.Current != null)
		{
			try
			{
				string arg = Mission.Current.SceneName ?? "Unknown";
				string arg2 = MobileParty.MainParty?.CurrentSettlement?.Name?.ToString() ?? "";
				MissionMode mode = Mission.Current.Mode;
				Logger.Log("SceneInfo", $"[MyBehavior.OnMissionStarted] SceneName={arg}, Mode={mode}, Settlement={arg2}");
			}
			catch
			{
			}
		}
	}

	private NpcPersonaProfile GetNpcPersonaProfile(Hero npc, bool createIfMissing)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_npcPersonaProfiles == null)
		{
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
		}
		if (_npcPersonaProfiles.TryGetValue(stringId, out var value))
		{
			return value;
		}
		if (!createIfMissing)
		{
			return null;
		}
		value = new NpcPersonaProfile();
		_npcPersonaProfiles[stringId] = value;
		return value;
	}

	private void SaveNpcPersonaProfile(Hero npc, NpcPersonaProfile profile)
	{
		if (npc == null)
		{
			return;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return;
		}
		if (_npcPersonaProfiles == null)
		{
			_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
		}
		string text = (profile?.Personality ?? "").Trim();
		string text2 = (profile?.Background ?? "").Trim();
		string text3 = (profile?.VoiceId ?? "").Trim();
		if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3))
		{
			_npcPersonaProfiles.Remove(stringId);
			return;
		}
		if (profile == null)
		{
			profile = new NpcPersonaProfile();
		}
		profile.Personality = text;
		profile.Background = text2;
		profile.VoiceId = text3;
		_npcPersonaProfiles[stringId] = profile;
	}

	private void GetNpcPersonaStrings(Hero hero, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (hero != null)
		{
			string stringId = hero.StringId;
			if (!string.IsNullOrEmpty(stringId) && _npcPersonaProfiles != null && _npcPersonaProfiles.TryGetValue(stringId, out var value) && value != null)
			{
				personality = value.Personality ?? "";
				background = value.Background ?? "";
			}
		}
	}

	private bool NeedsNpcPersonaGeneration(Hero hero)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return false;
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		return string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background);
	}

	private bool IsNpcPersonaGenerationInFlight(Hero hero)
	{
		string text = (hero?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		lock (_npcPersonaAutoGenLock)
		{
			return _npcPersonaAutoGenInFlight.Contains(text);
		}
	}

	private string GetNpcVoiceId(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return "";
		}
		if (_npcPersonaProfiles == null)
		{
			return "";
		}
		if (_npcPersonaProfiles.TryGetValue(stringId, out var value) && value != null)
		{
			return (value.VoiceId ?? "").Trim();
		}
		return "";
	}

	public static string GetNpcVoiceIdForExternal(Hero hero)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				return "";
			}
			return myBehavior.GetNpcVoiceId(hero);
		}
		catch
		{
			return "";
		}
	}

	private string BuildNpcPersonaContext(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		string text = (personality ?? "").Trim();
		string text2 = (background ?? "").Trim();
		if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrEmpty(text))
		{
			stringBuilder.AppendLine("【角色个性】" + text);
		}
		if (!string.IsNullOrEmpty(text2))
		{
			stringBuilder.AppendLine("【角色背景】" + text2);
		}
		stringBuilder.AppendLine("【约束】你必须在言行与态度上保持与上述个性、背景一致，不要编造与其冲突的经历。");
		return stringBuilder.ToString();
	}

	private static string TrimToMaxChars(string s, int maxChars)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return "";
		}
		s = s.Trim();
		if (s.Length <= maxChars)
		{
			return s;
		}
		return s.Substring(0, maxChars).Trim();
	}

	private string BuildHeroFactsForPersonaGeneration(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"姓名: {hero.Name}");
		stringBuilder.AppendLine($"年龄: {hero.Age:F0}");
		try
		{
			stringBuilder.AppendLine("性别: " + (hero.IsFemale ? "女" : "男"));
		}
		catch
		{
		}
		try
		{
			if (hero.Culture != null)
			{
				stringBuilder.AppendLine("文化: " + hero.Culture.StringId);
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null)
			{
				stringBuilder.AppendLine($"家族: {hero.Clan.Name} (StringId={hero.Clan.StringId})");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null && hero.Clan.Kingdom != null)
			{
				stringBuilder.AppendLine($"势力: {hero.Clan.Kingdom.Name} (StringId={hero.Clan.Kingdom.StringId})");
			}
		}
		catch
		{
		}
		try
		{
			Kingdom kingdom = hero.Clan?.Kingdom;
			if (kingdom != null && kingdom.Leader != null)
			{
				stringBuilder.AppendLine($"势力领袖: {kingdom.Leader.Name} (HeroId={kingdom.Leader.StringId})");
				if (hero.IsFactionLeader)
				{
					stringBuilder.AppendLine("效忠: 你本人即该势力领袖");
				}
				else
				{
					stringBuilder.AppendLine($"效忠: {kingdom.Leader.Name}");
				}
			}
		}
		catch
		{
		}
		string text = "英雄";
		try
		{
			if (hero.IsFactionLeader)
			{
				text = "统治者/派系领袖";
			}
			else if (hero.IsLord)
			{
				text = "领主";
			}
			else if (hero.IsWanderer)
			{
				text = "流浪者";
			}
			else if (hero.IsNotable)
			{
				text = "要人";
			}
		}
		catch
		{
		}
		stringBuilder.AppendLine("身份: " + text);
		try
		{
			stringBuilder.AppendLine($"特质: Mercy={hero.GetTraitLevel(DefaultTraits.Mercy)}, Valor={hero.GetTraitLevel(DefaultTraits.Valor)}, Honor={hero.GetTraitLevel(DefaultTraits.Honor)}, Generosity={hero.GetTraitLevel(DefaultTraits.Generosity)}, Calculating={hero.GetTraitLevel(DefaultTraits.Calculating)}");
		}
		catch
		{
		}
		try
		{
			var list = (from x in (from sk in Skills.All
					select new
					{
						Skill = sk,
						Value = hero.GetSkillValue(sk)
					} into x
					orderby x.Value descending, x.Skill.StringId
					select x).Take(8)
				where x.Value > 0
				select x).ToList();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("技能(最高8项): " + string.Join(", ", list.Select(x => $"{x.Skill.StringId}={x.Value}")));
			}
		}
		catch
		{
		}
		try
		{
			List<string> list2 = new List<string>();
			if (hero.Father != null)
			{
				list2.Add("父亲:" + hero.Father.Name);
			}
			if (hero.Mother != null)
			{
				list2.Add("母亲:" + hero.Mother.Name);
			}
			if (hero.Spouse != null)
			{
				list2.Add("配偶:" + hero.Spouse.Name);
			}
			try
			{
				List<string> list3 = (from c in hero.Children?.Take(6)
					select c?.Name?.ToString() into n
					where !string.IsNullOrWhiteSpace(n)
					select n).ToList();
				if (list3 != null && list3.Count > 0)
				{
					list2.Add("子女:" + string.Join("、", list3));
				}
			}
			catch
			{
			}
			if (list2.Count > 0)
			{
				stringBuilder.AppendLine("家族成员: " + string.Join(" | ", list2));
			}
		}
		catch
		{
		}
		return stringBuilder.ToString().Trim();
	}

	private static string GetClanTierReputationLabel(int tier)
	{
		int num = Math.Max(0, tier);
		if (num <= 0)
		{
			return "身份低微";
		}
		return num switch
		{
			1 => "小有名气", 
			2 => "崭露新贵", 
			3 => "声名清贵", 
			4 => "门第高华", 
			5 => "威权显赫", 
			_ => "贵不可言", 
		};
	}

	private static string BuildAgeBracketLabel(float age)
	{
		if (age >= 60f)
		{
			return "老年";
		}
		if (age >= 46f)
		{
			return "中年";
		}
		if (age >= 30f)
		{
			return "壮年";
		}
		if (age >= 18f)
		{
			return "青年";
		}
		if (age > 0f)
		{
			return "少年";
		}
		return "未知";
	}

	private static string BuildDialogueFormatGuardrailPrompt()
	{
		return "【常驻发言格式】你的可见回复必须使用“台词（动作）”结构：只保留一段台词，并在末尾仅使用一组全角括号（）描述动作；不要输出额外旁白、心理活动、评注或第二段叙述。若需输出 ACTION 标签，只能追加在整段文本末尾，且不得写入括号内。";
	}

	private static bool ContainsIgnoreCase(string text, string token)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
		{
			return false;
		}
		return text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsShortAckForRuleFollowup(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.Length > 20)
		{
			return false;
		}
		string[] array = new string[17]
		{
			"好", "好的", "行", "可以", "同意", "确认", "就这样", "继续", "嗯", "是",
			"对", "我选雇佣兵", "雇佣兵", "我选封臣", "封臣", "那就按这个", "那就这么办"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (ContainsIgnoreCase(text, array[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsNpcAskingForConfirmation(string npcText)
	{
		string text = StripActionTags(npcText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!text.Contains("？") && !text.Contains("?"))
		{
			return false;
		}
		string[] array = new string[13]
		{
			"是否", "要不要", "你要", "你是要", "确认", "同意", "愿意", "还是", "雇佣兵", "封臣",
			"成交吗", "继续吗", "要这样吗"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (ContainsIgnoreCase(text, array[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static string ResolveRuleStickyTargetKey(Hero targetHero, CharacterObject targetCharacter)
	{
		string text = targetHero?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			text = targetCharacter?.StringId ?? "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = targetCharacter?.HeroObject?.StringId ?? "";
		}
		return (text ?? "").Trim().ToLowerInvariant();
	}

	private static int GetBuiltInRuleStickyTurnLimit(string ruleId)
	{
		switch ((ruleId ?? "").Trim().ToLowerInvariant())
		{
		case "duel":
		case "reward":
			return 2;
		case "loan":
			return 3;
		default:
			return 0;
		}
	}

	private void ClearRuleStickyCarry()
	{
		_ruleStickyTargetKey = null;
		_ruleStickyDuelRoundsLeft = 0;
		_ruleStickyRewardRoundsLeft = 0;
		_ruleStickyLoanRoundsLeft = 0;
	}

	private bool TryConsumeRuleStickyCarry(Hero targetHero, CharacterObject targetCharacter, string playerInput, out bool duel, out bool reward, out bool loan)
	{
		duel = false;
		reward = false;
		loan = false;
		string text = ResolveRuleStickyTargetKey(targetHero, targetCharacter);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(_ruleStickyTargetKey) || (_ruleStickyDuelRoundsLeft <= 0 && _ruleStickyRewardRoundsLeft <= 0 && _ruleStickyLoanRoundsLeft <= 0))
		{
			ClearRuleStickyCarry();
			return false;
		}
		if (!string.Equals(_ruleStickyTargetKey, text, StringComparison.Ordinal))
		{
			ClearRuleStickyCarry();
			return false;
		}
		if (!IsShortAckForRuleFollowup(playerInput))
		{
			ClearRuleStickyCarry();
			return false;
		}
		if (_ruleStickyDuelRoundsLeft > 0)
		{
			duel = true;
			_ruleStickyDuelRoundsLeft--;
		}
		if (_ruleStickyRewardRoundsLeft > 0)
		{
			reward = true;
			_ruleStickyRewardRoundsLeft--;
		}
		if (_ruleStickyLoanRoundsLeft > 0)
		{
			loan = true;
			_ruleStickyLoanRoundsLeft--;
		}
		if (!duel && !reward && !loan)
		{
			ClearRuleStickyCarry();
			return false;
		}
		if (_ruleStickyDuelRoundsLeft <= 0 && _ruleStickyRewardRoundsLeft <= 0 && _ruleStickyLoanRoundsLeft <= 0)
		{
			ClearRuleStickyCarry();
		}
		try
		{
			Logger.Log("GuardrailSemantic", $"builtin_rule_sticky_consume target={text} duel={duel} reward={reward} loan={loan} left=({_ruleStickyDuelRoundsLeft},{_ruleStickyRewardRoundsLeft},{_ruleStickyLoanRoundsLeft})");
		}
		catch
		{
		}
		return true;
	}

	private void UpdateRuleStickyCarryFromHits(Hero targetHero, CharacterObject targetCharacter, bool duel, bool reward, bool loan)
	{
		string text = ResolveRuleStickyTargetKey(targetHero, targetCharacter);
		if (string.IsNullOrWhiteSpace(text))
		{
			ClearRuleStickyCarry();
			return;
		}
		if (!(duel || reward || loan))
		{
			return;
		}
		_ruleStickyTargetKey = text;
		_ruleStickyDuelRoundsLeft = (duel ? GetBuiltInRuleStickyTurnLimit("duel") : 0);
		_ruleStickyRewardRoundsLeft = (reward ? GetBuiltInRuleStickyTurnLimit("reward") : 0);
		_ruleStickyLoanRoundsLeft = (loan ? GetBuiltInRuleStickyTurnLimit("loan") : 0);
		try
		{
			Logger.Log("GuardrailSemantic", $"builtin_rule_sticky_prime target={text} duel={_ruleStickyDuelRoundsLeft} reward={_ruleStickyRewardRoundsLeft} loan={_ruleStickyLoanRoundsLeft}");
		}
		catch
		{
		}
	}

	public void OnEngineTick()
	{
		try
		{
			ProcessMissingOnnxGateUiResume();
			ProcessPendingWeeklyReportManualRetryResult();
			ProcessWeeklyReportUiResume();
			ProcessPendingDevForcedKingdomRebellionResult();
			ProcessPendingAutomaticKingdomRebellionResult();
			TryStartDeferredAutoWeeklyReports();
		}
		catch
		{
		}
	}

	private void OnGameLoadFinished()
	{
		try
		{
			if (HasCompleteRequiredOnnxFiles())
			{
				_missingOnnxGateActive = false;
				_missingOnnxGateResumeAfterUtcTicks = 0L;
				return;
			}
			_missingOnnxGateActive = true;
			_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks;
			ShowMissingOnnxGatePopup();
			try
			{
				Logger.Log("OnnxGate", "save load blocked because required ONNX files are missing.");
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("OnnxGate", "failed to evaluate ONNX gate: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	private void ProcessMissingOnnxGateUiResume()
	{
		if (!_missingOnnxGateActive)
		{
			return;
		}
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return;
		}
		if (InformationManager.IsAnyInquiryActive())
		{
			return;
		}
		if (DateTime.UtcNow.Ticks < _missingOnnxGateResumeAfterUtcTicks)
		{
			return;
		}
		ShowMissingOnnxGatePopup();
	}

	private void ShowMissingOnnxGatePopup()
	{
		_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(100.0).Ticks;
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData("缺少ONNX文件", "检测到你的mod缺乏ONNX文件，请前往群文件下载RAG专用模型，并将里面的onnx拖入AnimusForge的mod文件中", isAffirmativeOptionShown: true, isNegativeOptionShown: false, "保存并退出", "", ExitCurrentGameBecauseOnnxMissing, null), pauseGameActiveState: true);
	}

	private void ExitCurrentGameBecauseOnnxMissing()
	{
		try
		{
			_missingOnnxGateActive = false;
			_missingOnnxGateResumeAfterUtcTicks = 0L;
			InformationManager.HideInquiry();
			BeginSaveAndExitCurrentGame(SaveAndExitReason.MissingOnnx);
		}
		catch (Exception ex)
		{
			HandleSaveAndExitFailure(SaveAndExitReason.MissingOnnx, "保存并退出失败：" + ex.Message);
		}
	}

	private static bool HasCompleteRequiredOnnxFiles()
	{
		try
		{
			string moduleRootPath = GetModuleRootPath();
			if (string.IsNullOrWhiteSpace(moduleRootPath))
			{
				return false;
			}
			string text = Path.Combine(moduleRootPath, "ONNX");
			string text2 = Path.Combine(text, "onnx");
			if (!Directory.Exists(text) && !Directory.Exists(text2))
			{
				return false;
			}
			string text3 = FindFirstExistingFile(Path.Combine(text, "tokenizer.json"), Path.Combine(text2, "tokenizer.json"));
			if (string.IsNullOrEmpty(text3))
			{
				return false;
			}
			string text4 = FindFirstExistingFile(Path.Combine(text, "config.json"), Path.Combine(text2, "config.json"));
			if (string.IsNullOrEmpty(text4))
			{
				return false;
			}
			string text5 = FindFirstExistingFile(Path.Combine(text2, "model_quantized.onnx"), Path.Combine(text, "model_quantized.onnx"));
			if (!string.IsNullOrEmpty(text5))
			{
				return true;
			}
			string text6 = FindFirstExistingFile(Path.Combine(text2, "model.onnx"), Path.Combine(text, "model.onnx"));
			if (string.IsNullOrEmpty(text6))
			{
				return false;
			}
			return File.Exists(text6 + "_data");
		}
		catch
		{
			return false;
		}
	}

	private static string FindFirstExistingFile(params string[] files)
	{
		try
		{
			if (files == null)
			{
				return null;
			}
			foreach (string text in files)
			{
				if (!string.IsNullOrWhiteSpace(text) && File.Exists(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private void ProcessPendingWeeklyReportManualRetryResult()
	{
		if (!_pendingWeeklyReportManualRetryResult)
		{
			return;
		}
		bool pendingWeeklyReportManualRetrySucceeded = _pendingWeeklyReportManualRetrySucceeded;
		string text = (_pendingWeeklyReportManualRetryMessage ?? "").Trim();
		WeeklyReportRetryContext weeklyReportRetryContext = _pendingWeeklyReportManualRetryContext;
		_pendingWeeklyReportManualRetryResult = false;
		_pendingWeeklyReportManualRetrySucceeded = false;
		_pendingWeeklyReportManualRetryMessage = "";
		_pendingWeeklyReportManualRetryContext = null;
		_weeklyReportManualRetryInProgress = false;
		InformationManager.HideInquiry();
		if (pendingWeeklyReportManualRetrySucceeded)
		{
			_weeklyReportRetryContext = null;
			_weeklyReportUiStage = WeeklyReportUiStage.None;
			if (!string.IsNullOrWhiteSpace(text))
			{
				InformationManager.DisplayMessage(new InformationMessage(text));
			}
			return;
		}
		if (weeklyReportRetryContext != null)
		{
			_weeklyReportRetryContext = weeklyReportRetryContext;
		}
		_weeklyReportUiStage = WeeklyReportUiStage.None;
		if (!string.IsNullOrWhiteSpace(text))
		{
			InformationManager.DisplayMessage(new InformationMessage(text));
		}
		QueueWeeklyReportFailurePopup(_weeklyReportRetryContext, showImmediate: true);
	}

	private void ProcessWeeklyReportUiResume()
	{
		if (_weeklyReportReopenAfterApiConfig && _weeklyReportRetryContext != null && !_pendingWeeklyReportManualRetryResult)
		{
			if (!InformationManager.IsAnyInquiryActive() && DateTime.UtcNow.Ticks >= _weeklyReportReopenAfterApiConfigUtcTicks)
			{
				_weeklyReportReopenAfterApiConfig = false;
				ShowWeeklyReportFailurePopup(ignoreDelay: true);
			}
		}
		if (_weeklyReportRetryContext == null || _pendingWeeklyReportManualRetryResult)
		{
			return;
		}
		if (_weeklyReportUiStage != WeeklyReportUiStage.Failure && _weeklyReportUiStage != WeeklyReportUiStage.RetryProgress)
		{
			return;
		}
		if (InformationManager.IsAnyInquiryActive())
		{
			return;
		}
		if (DateTime.UtcNow.Ticks < _weeklyReportUiResumeAfterUtcTicks)
		{
			return;
		}
		if (_weeklyReportUiStage == WeeklyReportUiStage.RetryProgress && _weeklyReportManualRetryInProgress)
		{
			ShowWeeklyReportRetryProgressPopup();
			return;
		}
		if (_weeklyReportUiStage == WeeklyReportUiStage.Failure)
		{
			ShowWeeklyReportFailurePopup(ignoreDelay: true);
		}
	}

	private void ProcessPendingDevForcedKingdomRebellionResult()
	{
		if (!_pendingDevForcedKingdomRebellionReady)
		{
			return;
		}
		PendingDevForcedKingdomRebellionContext pendingDevForcedKingdomRebellionContext = _pendingDevForcedKingdomRebellionContext;
		_pendingDevForcedKingdomRebellionReady = false;
		_pendingDevForcedKingdomRebellionContext = null;
		_devForcedKingdomRebellionInProgress = false;
		if (pendingDevForcedKingdomRebellionContext == null)
		{
			return;
		}
		Kingdom kingdom = FindKingdomById(pendingDevForcedKingdomRebellionContext.KingdomId);
		Clan clan = FindClanById(pendingDevForcedKingdomRebellionContext.ClanId);
		List<Clan> list = (pendingDevForcedKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(FindClanById).Where((Clan x) => x != null).ToList();
		string text;
		bool flag;
		if (kingdom == null || clan == null)
		{
			flag = false;
			text = "叛乱命名已完成，但目标王国或家族状态已变化，无法继续执行。";
		}
		else
		{
			flag = TryExecuteKingdomRebellionWithNaming(clan, kingdom, pendingDevForcedKingdomRebellionContext.WeekIndex, forceTrigger: true, pendingDevForcedKingdomRebellionContext.RelationToKing, pendingDevForcedKingdomRebellionContext.TownCount, pendingDevForcedKingdomRebellionContext.CastleCount, pendingDevForcedKingdomRebellionContext.NamingResult, list, out text);
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (pendingDevForcedKingdomRebellionContext.NamingResult != null)
		{
			stringBuilder.AppendLine("命名结果：");
			stringBuilder.AppendLine("- 正式名：" + ((pendingDevForcedKingdomRebellionContext.NamingResult.FormalName ?? "").Trim()));
			stringBuilder.AppendLine("- 简称：" + ((pendingDevForcedKingdomRebellionContext.NamingResult.ShortName ?? "").Trim()));
			stringBuilder.AppendLine("- 来源：" + (pendingDevForcedKingdomRebellionContext.NamingResult.UsedFallback ? "本地兜底" : "LLM"));
			if (!string.IsNullOrWhiteSpace(pendingDevForcedKingdomRebellionContext.NamingResult.EncyclopediaText))
			{
				stringBuilder.AppendLine("- 百科简介：" + pendingDevForcedKingdomRebellionContext.NamingResult.EncyclopediaText.Trim());
			}
			if (!string.IsNullOrWhiteSpace(pendingDevForcedKingdomRebellionContext.NamingResult.FailureReason))
			{
				stringBuilder.AppendLine("- 命名说明：" + pendingDevForcedKingdomRebellionContext.NamingResult.FailureReason.Trim());
			}
			stringBuilder.AppendLine();
		}
		if (list.Count > 0)
		{
			stringBuilder.AppendLine("联合响应家族：");
			stringBuilder.AppendLine("- " + string.Join("、", list.Select(GetClanDisplayName)));
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("执行结果：");
		stringBuilder.AppendLine((text ?? "").Trim());
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(flag ? "强制叛乱执行完成" : "强制叛乱执行失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回详情", "", delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom ?? FindKingdomById(pendingDevForcedKingdomRebellionContext.KingdomId));
		}, null));
	}

	private void StartDevForcedKingdomRebellionAsync(Kingdom kingdom, Clan clan, int weekIndex, int relationToKing, int townCount, int castleCount, List<Clan> followerClans)
	{
		if (_devForcedKingdomRebellionInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前已有一条强制叛乱任务正在后台运行，请稍候。"));
			return;
		}
		if (kingdom == null || clan == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法启动强制叛乱：找不到目标王国或家族。"));
			return;
		}
		List<Clan> list = followerClans?.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => GetClanId(x), StringComparer.OrdinalIgnoreCase).Select((IGrouping<string, Clan> x) => x.First()).ToList() ?? new List<Clan>();
		BuildRebelKingdomNamingRequest(clan, kingdom, weekIndex, list, out var systemPrompt, out var userPrompt, out var fallbackResult);
		_devForcedKingdomRebellionInProgress = true;
		_pendingDevForcedKingdomRebellionReady = false;
		_pendingDevForcedKingdomRebellionContext = null;
		InformationManager.ShowInquiry(new InquiryData("正在生成叛乱建国命名", "系统正在后台请求 LLM 为这次叛乱生成新王国的名称与百科简介。\n\n这一步完成后，才会真正执行家族反出与建国。\n请稍候，结果完成后会自动弹出。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, fallbackResult, "叛乱建国命名 - " + GetClanId(clan), 1);
			_pendingDevForcedKingdomRebellionContext = new PendingDevForcedKingdomRebellionContext
			{
				KingdomId = GetKingdomId(kingdom),
				ClanId = GetClanId(clan),
				WeekIndex = weekIndex,
				RelationToKing = relationToKing,
				TownCount = townCount,
				CastleCount = castleCount,
				FollowerClanIds = list.Select(GetClanId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
				NamingResult = namingResult
			};
			_pendingDevForcedKingdomRebellionReady = true;
		});
	}

	private static string BuildHeroIdentityTitleForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "未知身份";
		}
		try
		{
			if (hero.Occupation == Occupation.Wanderer)
			{
				return "流浪者";
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			Kingdom kingdom = clan?.Kingdom;
			if (clan != null && clan.IsUnderMercenaryService && kingdom != null)
			{
				string text = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text + "的雇佣兵";
				}
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (hero.MapFaction?.Name?.ToString() ?? "").Trim();
			if (hero.IsFactionLeader)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2 + "的统治者";
				}
				return "统治者";
			}
			if (hero.IsLord)
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2 + "的封臣";
				}
				return "领主";
			}
			if (hero.IsWanderer)
			{
				return "流浪者";
			}
			if (hero.IsNotable)
			{
				return "地方要人";
			}
			switch (hero.Occupation)
			{
			case Occupation.Merchant:
				return "商人";
			case Occupation.Artisan:
				return "工匠";
			case Occupation.GangLeader:
				return "帮派首领";
			case Occupation.Headman:
				return "村长";
			case Occupation.Preacher:
				return "教士";
			case Occupation.RuralNotable:
				return "乡绅";
			}
		}
		catch
		{
		}
		return "普通角色";
	}

	private static void GetHeroFactionAndLiegeForPrompt(Hero hero, out string factionName, out string liegeName)
	{
		factionName = "无（独立）";
		liegeName = "无";
		if (hero == null)
		{
			return;
		}
		try
		{
			Kingdom kingdom = hero.Clan?.Kingdom;
			if (kingdom != null)
			{
				string text = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					factionName = text;
				}
				string text2 = (kingdom.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					liegeName = text2;
				}
				if (kingdom.Leader == hero)
				{
					string text3 = (hero.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						liegeName = text3 + "（本人）";
					}
				}
				return;
			}
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = hero.MapFaction;
			if (mapFaction != null)
			{
				string text4 = (mapFaction.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4))
				{
					factionName = text4;
				}
				string text5 = (mapFaction.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text5))
				{
					liegeName = text5;
				}
				if (mapFaction.Leader == hero)
				{
					string text6 = (hero.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6))
					{
						liegeName = text6 + "（本人）";
					}
				}
				return;
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			if (clan != null)
			{
				string text7 = (clan.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text7))
				{
					factionName = text7;
				}
				string text8 = (clan.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text8))
				{
					liegeName = text8;
				}
			}
		}
		catch
		{
		}
	}

	private static string BuildFactionLineForPrompt(string label, string factionName, string liegeName)
	{
		string text = (factionName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "无（独立）";
		}
		string text2 = (liegeName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || text2 == "无" || text2.EndsWith("（本人）", StringComparison.Ordinal))
		{
			return label + text;
		}
		return label + text + "（效忠：" + text2 + "）";
	}

	private static string GetHeroCultureNameForPrompt(Hero hero)
	{
		try
		{
			string text = (hero?.Culture?.Name?.ToString() ?? "").Trim();
			return string.IsNullOrWhiteSpace(text) ? "未知文化" : text;
		}
		catch
		{
			return "未知文化";
		}
	}

	private static string BuildNpcClanRoleHintForPrompt(Hero npcHero)
	{
		try
		{
			if (npcHero == null || npcHero.Clan == null)
			{
				return "";
			}
			Hero leader = npcHero.Clan.Leader;
			if (leader == null)
			{
				return "";
			}
			if (leader == npcHero)
			{
				return "你是家族的族长";
			}
			if (npcHero.Spouse == leader || leader.Spouse == npcHero)
			{
				return "你是家族族长的配偶";
			}
			if (npcHero.Father == leader || npcHero.Mother == leader)
			{
				return npcHero.IsFemale ? "你是家族族长的女儿" : "你是家族族长的儿子";
			}
			if (leader.Father == npcHero || leader.Mother == npcHero)
			{
				return npcHero.IsFemale ? "你是家族族长的母亲" : "你是家族族长的父亲";
			}
		}
		catch
		{
		}
		return "";
	}

	private static IEnumerable<Hero> GetClanMembersForPrompt(Clan clan)
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
		HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (object item in enumerable)
		{
			if (item is Hero hero && hero != null)
			{
				string text = (hero.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && yielded.Add(text))
				{
					yield return hero;
				}
			}
		}
	}

	private const int MarriageCandidateMinAgeForPrompt = 18;

	private const int MarriageCandidateMaxAgeForPrompt = 55;

	private const int MarriageCandidateMaxAgeGapForPrompt = 25;

	private static int GetMarriageCandidateMaxAgeSettingForPrompt()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAge).GetValueOrDefault(MarriageCandidateMaxAgeForPrompt);
			if (valueOrDefault < MarriageCandidateMinAgeForPrompt)
			{
				valueOrDefault = MarriageCandidateMinAgeForPrompt;
			}
			if (valueOrDefault > 80)
			{
				valueOrDefault = 80;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeForPrompt;
		}
	}

	private static int GetMarriageCandidateMaxAgeGapSettingForPrompt()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAgeGap).GetValueOrDefault(MarriageCandidateMaxAgeGapForPrompt);
			if (valueOrDefault < 0)
			{
				valueOrDefault = 0;
			}
			if (valueOrDefault > 60)
			{
				valueOrDefault = 60;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeGapForPrompt;
		}
	}

	private static bool GetMarriageRequireOppositeGenderSettingForPrompt()
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

	private static bool IsMarriageGenderCompatibleForPrompt(Hero candidate, Hero player)
	{
		try
		{
			if (candidate == null || player == null)
			{
				return true;
			}
			if (!GetMarriageRequireOppositeGenderSettingForPrompt())
			{
				return true;
			}
			return candidate.IsFemale != player.IsFemale;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsMarriageAgeCompatibleForPrompt(Hero candidate, Hero player)
	{
		try
		{
			if (candidate == null)
			{
				return false;
			}
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			if (candidate.Age < (float)MarriageCandidateMinAgeForPrompt || candidate.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
			{
				return false;
			}
			if (player != null)
			{
				return Math.Abs(candidate.Age - player.Age) <= (float)GetMarriageCandidateMaxAgeGapSettingForPrompt();
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMarriageCandidateForPrompt(Hero hero, Hero player)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (!IsMarriageAgeCompatibleForPrompt(hero, player))
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
			if (hero.Spouse != null)
			{
				return false;
			}
		}
		catch
		{
		}
		if (player != null && hero == player)
		{
			return false;
		}
		if (!IsMarriageGenderCompatibleForPrompt(hero, player))
		{
			return false;
		}
		return true;
	}

	private static bool IsMarriagePoolCandidateForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			if (hero.Age < (float)MarriageCandidateMinAgeForPrompt || hero.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
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

	private static string GetMarriageCandidateGenderLabelForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			return hero.IsFemale ? "女" : "男";
		}
		catch
		{
			return "未知";
		}
	}

	private static string BuildClanUnmarriedCandidatesForPrompt(Hero npcHero, Hero player, int maxEntries = 12)
	{
		try
		{
			Clan clan = npcHero?.Clan;
			if (clan == null)
			{
				return "无（目标无家族）";
			}
			Hero leader = clan.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(clan)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（该家族当前没有基础适婚条件下的未婚成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				Hero hero = list[i];
				string text = hero.Name?.ToString() ?? ("Hero#" + i);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(hero);
				int num = (int)Math.Round(hero.Age);
				string text2 = "";
				if (hero == npcHero)
				{
					text2 = "（你自己）";
				}
				else if (hero == leader)
				{
					text2 = "（族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num} | StringId={hero.StringId}");
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "无（生成未婚名单时发生异常）";
		}
	}

	private static string GetEquipmentContextLabelForPrompt(bool useCivilianEquipment)
	{
		return useCivilianEquipment ? "常服" : "战斗装";
	}

	private static bool TryResolveEquipmentContextForPrompt(Hero hero, out bool useCivilianEquipment)
	{
		useCivilianEquipment = false;
		try
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				useCivilianEquipment = current.DoesMissionRequireCivilianEquipment;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? hero?.CurrentSettlement;
			if (settlement != null)
			{
				useCivilianEquipment = !settlement.IsVillage;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static ItemObject TryGetAgentEquipmentItemForPrompt(Hero hero, EquipmentIndex index)
	{
		if (hero != Hero.MainHero || Agent.Main == null || !Agent.Main.IsActive())
		{
			return null;
		}
		try
		{
			ItemObject item = Agent.Main.SpawnEquipment[index].Item;
			if (item != null)
			{
				return item;
			}
		}
		catch
		{
		}
		try
		{
			return Agent.Main.Equipment[index].Item;
		}
		catch
		{
			return null;
		}
	}

	private static ItemObject TryGetHeroEquipmentItemForPrompt(Hero hero, EquipmentIndex index, bool useCivilianEquipment)
	{
		if (hero == null)
		{
			return null;
		}
		ItemObject itemObject = TryGetAgentEquipmentItemForPrompt(hero, index);
		if (itemObject != null)
		{
			return itemObject;
		}
		try
		{
			Equipment equipment = (useCivilianEquipment ? hero.CivilianEquipment : hero.BattleEquipment);
			if (equipment == null)
			{
				return null;
			}
			return equipment[index].Item;
		}
		catch
		{
			return null;
		}
	}

	private static string BuildHeroEquipmentSummaryForPrompt(Hero hero, int maxEntries = 8)
	{
		if (hero == null)
		{
			return "未知";
		}
		bool useCivilianEquipment = false;
		bool flag = TryResolveEquipmentContextForPrompt(hero, out useCivilianEquipment);
		string text = GetEquipmentContextLabelForPrompt(useCivilianEquipment);
		try
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9]
			{
				EquipmentIndex.NumAllWeaponSlots,
				EquipmentIndex.Body,
				EquipmentIndex.Leg,
				EquipmentIndex.Gloves,
				EquipmentIndex.Cape,
				EquipmentIndex.WeaponItemBeginSlot,
				EquipmentIndex.Weapon1,
				EquipmentIndex.Weapon2,
				EquipmentIndex.Weapon3
			};
			EquipmentIndex[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				EquipmentIndex index = array2[i];
				ItemObject item = TryGetHeroEquipmentItemForPrompt(hero, index, useCivilianEquipment);
				if (item != null)
				{
					string text2 = (item.StringId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text2))
					{
						text2 = index.ToString();
					}
					string value = (item.Name?.ToString() ?? "").Trim();
					if (string.IsNullOrWhiteSpace(value))
					{
						value = text2;
					}
					if (!dictionary.ContainsKey(text2))
					{
						dictionary[text2] = 0;
						names[text2] = value;
					}
					dictionary[text2]++;
				}
			}
			if (dictionary.Count == 0)
			{
				return (flag ? (text + "：未读取到可识别装备") : "未能判断当前应显示民用装还是战斗装，且未读取到可识别装备");
			}
			List<string> values = (from x in (from x in dictionary.Select(delegate(KeyValuePair<string, int> kv)
					{
						string value2;
						string name = (names.TryGetValue(kv.Key, out value2) ? value2 : kv.Key);
						return new
						{
							Name = name,
							Count = kv.Value
						};
					})
					orderby x.Count descending
					select x).ThenBy(x => x.Name, StringComparer.Ordinal).Take(Math.Max(1, maxEntries))
				select x.Name + "x" + x.Count).ToList();
			return text + "：" + string.Join("、", values);
		}
		catch
		{
			return (flag ? (text + "：读取装备失败") : "装备信息读取失败");
		}
	}

	private string BuildPlayerIdentityInfoForPrompt(Hero playerHero, bool includeRuleGatedFields, bool includeTradePricing, bool includeMarriageCandidates = false, Hero targetHero = null)
	{
		if (playerHero == null)
		{
			return "";
		}
		int num = 0;
		string text = "无家族";
		try
		{
			num = playerHero.Clan?.Tier ?? 0;
			string text2 = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(playerHero, out var factionName, out var liegeName);
		bool flag = false;
		try
		{
			flag = playerHero.Clan?.Kingdom != null;
		}
		catch
		{
			flag = false;
		}
		string text3 = BuildHeroIdentityTitleForPrompt(playerHero);
		string heroCultureNameForPrompt = GetHeroCultureNameForPrompt(playerHero);
		string text4 = BuildAgeBracketLabel(playerHero.Age);
		string text5 = BuildHeroEquipmentSummaryForPrompt(playerHero);
		string value = "";
		if (includeTradePricing)
		{
			try
			{
				if (RewardSystemBehavior.Instance != null)
				{
					value = RewardSystemBehavior.Instance.BuildVisibleEquipmentValueSummaryForAI(playerHero);
				}
			}
			catch
			{
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[player身份信息]");
		stringBuilder.AppendLine("玩家文化：" + heroCultureNameForPrompt);
		if (includeRuleGatedFields)
		{
			stringBuilder.AppendLine("玩家声望：" + GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
			if (flag)
			{
				stringBuilder.AppendLine(BuildFactionLineForPrompt("玩家势力：", factionName, liegeName));
				stringBuilder.AppendLine("玩家身份：" + text3);
			}
			stringBuilder.AppendLine("玩家家族：" + text + $"（{Math.Max(0, num)} level，玩家是家族的族长）");
		}
		else if (flag)
		{
			stringBuilder.AppendLine(BuildFactionLineForPrompt("玩家势力：", factionName, liegeName));
		}
		else
		{
			stringBuilder.AppendLine("提示：你感觉此人只是个普通人。");
		}
		stringBuilder.AppendLine("玩家年纪：" + text4);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine("玩家装备：" + text5);
			stringBuilder.AppendLine(value);
		}
		else
		{
			stringBuilder.AppendLine("玩家装备：" + text5);
		}
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			stringBuilder.AppendLine("【玩家家族可婚配未婚成员（事实清单）】");
			stringBuilder.AppendLine($"筛选口径：仅列出玩家家族内部基础适婚池（年龄 {MarriageCandidateMinAgeForPrompt}-{marriageCandidateMaxAgeSettingForPrompt}、未婚、非囚犯）。具体能否与对方成员成婚，仍以性别限制、年龄差和运行时婚姻规则为准。");
			stringBuilder.AppendLine(BuildPlayerClanUnmarriedCandidatesForPrompt(playerHero, targetHero, 12));
		}
		return stringBuilder.ToString().Trim();
	}

	private string BuildNpcIdentityInfoForPrompt(Hero npcHero, bool includeTradePricing, bool includeMarriageCandidates = false)
	{
		if (npcHero == null)
		{
			return "";
		}
		int num = 0;
		string text = "无家族";
		try
		{
			num = npcHero.Clan?.Tier ?? 0;
			string text2 = (npcHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(npcHero, out var factionName, out var liegeName);
		string text3 = BuildHeroIdentityTitleForPrompt(npcHero);
		string heroCultureNameForPrompt = GetHeroCultureNameForPrompt(npcHero);
		string text4 = BuildNpcClanRoleHintForPrompt(npcHero);
		string text5 = BuildAgeBracketLabel(npcHero.Age);
		string text6 = BuildHeroEquipmentSummaryForPrompt(npcHero);
		int num2 = 0;
		try
		{
			num2 = Math.Max(0, npcHero.Gold);
		}
		catch
		{
			num2 = 0;
		}
		Hero hero = null;
		try
		{
			hero = npcHero.Clan?.Leader;
		}
		catch
		{
			hero = null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[NPC身份信息]");
		stringBuilder.AppendLine("NPC文化：" + heroCultureNameForPrompt);
		stringBuilder.AppendLine("NPC声望：" + GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）");
		stringBuilder.AppendLine(BuildFactionLineForPrompt("NPC势力：", factionName, liegeName));
		stringBuilder.AppendLine("NPC身份：" + text3);
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && npcHero != null && npcHero != mainHero && (npcHero.Spouse == mainHero || mainHero.Spouse == npcHero))
			{
				string playerDisplayName = BuildPlayerPublicDisplayNameForPrompt();
				if (string.IsNullOrWhiteSpace(playerDisplayName))
				{
					playerDisplayName = "玩家";
				}
				string text7 = (npcHero.IsFemale ? "丈夫" : "妻子");
				stringBuilder.AppendLine("NPC与" + playerDisplayName + "的关系：合法配偶（" + playerDisplayName + "是你的" + text7 + "）；你必须认出" + playerDisplayName + "并以配偶关系进行回应。");
			}
		}
		catch
		{
		}
		if (!string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.AppendLine("NPC家族：" + text + $"（{Math.Max(0, num)} level，{text4}）");
		}
		else
		{
			stringBuilder.AppendLine("NPC家族：" + text + $"（{Math.Max(0, num)} level）");
		}
		if (hero != null)
		{
			if (hero == npcHero)
			{
				stringBuilder.AppendLine("该家族族长：你本人（StringId=" + (npcHero.StringId ?? "") + "）");
			}
			else
			{
				stringBuilder.AppendLine("该家族族长：" + (hero.Name?.ToString() ?? "未知") + "（StringId=" + (hero.StringId ?? "") + "）");
			}
		}
		stringBuilder.AppendLine("NPC年纪：" + text5);
		stringBuilder.AppendLine("NPC存款：" + num2 + " 第纳尔");
		stringBuilder.AppendLine("NPC装备：" + text6);
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			stringBuilder.AppendLine("【该家族可婚配未婚成员（事实清单）】");
			stringBuilder.AppendLine($"筛选口径：仅列出该家族内部基础适婚池（年龄 {MarriageCandidateMinAgeForPrompt}-{marriageCandidateMaxAgeSettingForPrompt}、未婚、非囚犯）。具体能否与玩家家族成员成婚，仍以性别限制、年龄差和运行时婚姻规则为准。");
			stringBuilder.AppendLine(BuildClanUnmarriedCandidatesForPrompt(npcHero, Hero.MainHero, 12));
		}
		if (includeTradePricing && RewardSystemBehavior.Instance != null)
		{
			try
			{
				string text7 = RewardSystemBehavior.Instance.BuildInventorySummaryForAI(npcHero);
				if (!string.IsNullOrWhiteSpace(text7))
				{
					stringBuilder.AppendLine("【NPC当前可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说");
					stringBuilder.AppendLine(text7);
					Logger.Log("Logic", "[Context] InventorySummary=\n" + text7);
				}
			}
			catch
			{
			}
		}
		return stringBuilder.ToString().Trim();
	}

	private string BuildNpcSystemTopPromptIntro(Hero npcHero, bool includeTradePricing)
	{
		if (npcHero == null)
		{
			return "";
		}
		string clanName = "无家族";
		int clanTier = 0;
		try
		{
			clanTier = npcHero.Clan?.Tier ?? 0;
			string rawClanName = (npcHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(rawClanName))
			{
				clanName = rawClanName;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(npcHero, out var factionName, out var liegeName);
		string factionDisplay = (factionName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(factionDisplay))
		{
			factionDisplay = "无（独立）";
		}
		string liegeDisplay = (liegeName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(liegeDisplay) && liegeDisplay != "无" && !liegeDisplay.EndsWith("（本人）", StringComparison.Ordinal))
		{
			factionDisplay = factionDisplay + "（效忠：" + liegeDisplay + "）";
		}
		string heroName = (npcHero.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(heroName))
		{
			heroName = "未知人物";
		}
		string identityTitle = BuildHeroIdentityTitleForPrompt(npcHero);
		string reputationText = GetClanTierReputationLabel(clanTier) + $"（{Math.Max(0, clanTier)} level）";
		string equipmentText = BuildHeroEquipmentSummaryForPrompt(npcHero);
		string ageText = BuildAgeBracketLabel(npcHero.Age);
		string cultureText = GetHeroCultureNameForPrompt(npcHero);
		if (!string.IsNullOrWhiteSpace(cultureText) && !cultureText.EndsWith("人", StringComparison.Ordinal))
		{
			cultureText += "人";
		}
		GetNpcPersonaStrings(npcHero, out var personality, out var background);
		string personalityText = string.IsNullOrWhiteSpace(personality) ? "暂无记录" : personality.Trim();
		string backgroundText = string.IsNullOrWhiteSpace(background) ? "暂无记录" : background.Trim();
		string clanRole = npcHero.IsFemale ? "女性成员" : "男性成员";
		try
		{
			Hero leader = npcHero.Clan?.Leader;
			if (leader != null && leader == npcHero)
			{
				clanRole = "族长";
			}
		}
		catch
		{
		}
		string inventorySummary = "";
		if (includeTradePricing && RewardSystemBehavior.Instance != null)
		{
			try
			{
				inventorySummary = RewardSystemBehavior.Instance.BuildInventorySummaryForAI(npcHero);
			}
			catch
			{
				inventorySummary = "";
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("你是")
			.Append(factionDisplay)
			.Append("的")
			.Append(clanName)
			.Append("的")
			.Append(heroName)
			.Append("，你是家族中的")
			.Append(clanRole)
			.Append("，你的身份是")
			.Append(identityTitle)
			.Append("，你")
			.Append(reputationText)
			.Append("，你身上穿着")
			.Append(equipmentText)
			.Append("，你的个性为：")
			.Append(personalityText)
			.Append("，你的背景是：")
			.Append(backgroundText)
			.Append("，你的年纪是")
			.Append(ageText)
			.Append("，你是")
			.Append(cultureText)
			.Append("。");
		return stringBuilder.ToString().Trim();
	}

	private static int GetDaysInSeasonSafeForPrompt()
	{
		try
		{
			int daysInSeason = CampaignTime.DaysInSeason;
			if (daysInSeason > 0)
			{
				return daysInSeason;
			}
		}
		catch
		{
		}
		return 21;
	}

	private static int GetDaysInYearSafeForPrompt()
	{
		try
		{
			int daysInYear = CampaignTime.DaysInYear;
			if (daysInYear > 0)
			{
				return daysInYear;
			}
		}
		catch
		{
		}
		return GetDaysInSeasonSafeForPrompt() * 4;
	}

	private static string GetSeasonTextZhForPrompt(int seasonIndexZeroBased)
	{
		int num = seasonIndexZeroBased % 4;
		if (num < 0)
		{
			num += 4;
		}
		return num switch
		{
			0 => "春", 
			1 => "夏", 
			2 => "秋", 
			_ => "冬", 
		};
	}

	private string BuildCurrentDateFactForPrompt()
	{
		try
		{
			int num = Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
			int daysInSeasonSafeForPrompt = GetDaysInSeasonSafeForPrompt();
			int daysInYearSafeForPrompt = GetDaysInYearSafeForPrompt();
			int num2 = num / Math.Max(1, daysInYearSafeForPrompt);
			int num3 = num % Math.Max(1, daysInYearSafeForPrompt);
			int seasonIndexZeroBased = num3 / Math.Max(1, daysInSeasonSafeForPrompt);
			int num4 = num3 % Math.Max(1, daysInSeasonSafeForPrompt) + 1;
			string seasonTextZhForPrompt = GetSeasonTextZhForPrompt(seasonIndexZeroBased);
			string text = "";
			try
			{
				text = CampaignTime.Now.ToString();
			}
			catch
			{
				text = "";
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = $"{num2}年{seasonTextZhForPrompt}第{num4}天";
			}
			return $"当前游戏日期：{text}（绝对天数第 {num} 天；{num2}年{seasonTextZhForPrompt}第{num4}天）";
		}
		catch
		{
			return "";
		}
	}

	private static bool TryParsePersonaJson(string text, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.Trim();
		int num = text2.IndexOf('{');
		int num2 = text2.LastIndexOf('}');
		if (num >= 0 && num2 > num)
		{
			text2 = text2.Substring(num, num2 - num + 1);
		}
		try
		{
			JObject jObject = JObject.Parse(text2);
			personality = (jObject["personality"] ?? jObject["Personality"])?.ToString() ?? "";
			background = (jObject["background"] ?? jObject["Background"])?.ToString() ?? "";
			return true;
		}
		catch
		{
			return false;
		}
	}

	private async Task EnsureNpcPersonaGeneratedAsync(Hero hero)
	{
		if (hero == null)
		{
			return;
		}
		string id = hero.StringId;
		if (string.IsNullOrEmpty(id))
		{
			return;
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		bool needP = string.IsNullOrWhiteSpace(personality);
		bool needB = string.IsNullOrWhiteSpace(background);
		if (!needP && !needB)
		{
			return;
		}
		lock (_npcPersonaAutoGenLock)
		{
			if (_npcPersonaAutoGenInFlight.Contains(id))
			{
				return;
			}
			_npcPersonaAutoGenInFlight.Add(id);
		}
		try
		{
			string sys = "你是《骑马与砍杀2：霸主》NPC的人设生成器。你只输出严格 JSON，不要输出任何额外文字。JSON 仅包含两个字段：personality 和 background。personality 大约 300 个中文字符；background 大约 300 个中文字符。内容必须符合提供的事实，不要杜撰与事实冲突的家族关系或身份；若事实中提供了势力/效忠信息，必须保持一致，禁止声称效忠于其他统治者或属于其他势力。";
			string facts = BuildHeroFactsForPersonaGeneration(hero);
			string user = "请基于以下信息生成该 NPC 的【个性】与【历史背景】。\n" + facts;
			string resp = await CallUniversalApi(sys, user);
			if (!string.IsNullOrWhiteSpace(resp) && !resp.StartsWith("错误") && TryParsePersonaJson(resp, out var genP, out var genB))
			{
				genP = TrimToMaxChars(genP, 380);
				genB = TrimToMaxChars(genB, 380);
				GetNpcPersonaStrings(hero, out var curP, out var curB);
				NpcPersonaProfile prof = GetNpcPersonaProfile(hero, createIfMissing: true) ?? new NpcPersonaProfile();
				prof.Personality = (string.IsNullOrWhiteSpace(curP) ? genP : curP.Trim());
				prof.Background = (string.IsNullOrWhiteSpace(curB) ? genB : curB.Trim());
				SaveNpcPersonaProfile(hero, prof);
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Logger.Log("NpcPersona", "[ERROR] AutoGen failed: " + ex2.Message);
		}
		finally
		{
			lock (_npcPersonaAutoGenLock)
			{
				_npcPersonaAutoGenInFlight.Remove(id);
			}
		}
	}

	private void OnChatConfirmed(string input)
	{
		if (!string.IsNullOrWhiteSpace(input))
		{
			StartAiConversation(input, null);
		}
	}

	private void BeginGiveFlow(string npcName)
	{
		_pendingTrade = new PendingTradeContext
		{
			IsGive = true
		};
		ShowResourceSelectionInquiry(npcName);
	}

	private void BeginShowFlow(string npcName)
	{
		_pendingTrade = new PendingTradeContext
		{
			IsGive = false
		};
		ShowResourceSelectionInquiry(npcName);
	}

	private void ShowResourceSelectionInquiry(string npcName)
	{
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		List<TradeResourceOption> list = (_currentTradeOptions = BuildResourceOptions(oneToOneConversationHero));
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你没有可以使用的物品或第纳尔。"));
			_pendingTrade = null;
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		for (int i = 0; i < list.Count; i++)
		{
			TradeResourceOption tradeResourceOption = list[i];
			string hint = $"可用数量: {tradeResourceOption.AvailableAmount}";
			list2.Add(new InquiryElement(i, $"{tradeResourceOption.Name} (×{tradeResourceOption.AvailableAmount})", null, isEnabled: true, hint));
		}
		string titleText = ((_pendingTrade != null && _pendingTrade.IsGive) ? "给予其物品" : "展示物品");
		string descriptionText = "选择要使用的物品或第纳尔（可多选）：";
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list2, isExitShown: true, 1, list2.Count, "确定", "取消", OnMultiResourceSelected, OnMultiResourceCancelled, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OnMultiResourceSelected(List<InquiryElement> selectedElements)
	{
		if (selectedElements == null || selectedElements.Count == 0 || _currentTradeOptions == null)
		{
			_pendingTrade = null;
			return;
		}
		_pendingTradeItems = new List<PendingTradeItem>();
		foreach (InquiryElement selectedElement in selectedElements)
		{
			int num = (int)selectedElement.Identifier;
			if (num >= 0 && num < _currentTradeOptions.Count)
			{
				TradeResourceOption tradeResourceOption = _currentTradeOptions[num];
				_pendingTradeItems.Add(new PendingTradeItem
				{
					IsGold = tradeResourceOption.IsGold,
					ItemId = tradeResourceOption.ItemId,
					ItemName = tradeResourceOption.Name,
					Item = tradeResourceOption.Item,
					InventoryUnitValue = tradeResourceOption.InventoryUnitValue,
					Amount = 0
				});
			}
		}
		if (_pendingTradeItems.Count == 0)
		{
			_pendingTrade = null;
		}
		else
		{
			_pendingTradeItemIndex = 0;
			ShowAmountInquiryForCurrentItem();
		}
	}

	private void OnMultiResourceCancelled(List<InquiryElement> _)
	{
		_pendingTrade = null;
		_pendingTradeItems.Clear();
	}

	private void ShowAmountInquiryForCurrentItem()
	{
		if (_pendingTradeItemIndex >= _pendingTradeItems.Count)
		{
			ShowTradeChatInput();
			return;
		}
		PendingTradeItem pendingTradeItem = _pendingTradeItems[_pendingTradeItemIndex];
		int num = 0;
		foreach (TradeResourceOption currentTradeOption in _currentTradeOptions)
		{
			if (currentTradeOption.IsGold == pendingTradeItem.IsGold && currentTradeOption.ItemId == pendingTradeItem.ItemId)
			{
				num = currentTradeOption.AvailableAmount;
				break;
			}
		}
		if (num <= 0)
		{
			_pendingTradeItemIndex++;
			ShowAmountInquiryForCurrentItem();
			return;
		}
		string titleText = ((_pendingTrade != null && _pendingTrade.IsGive) ? "给予数量" : "展示数量");
		string text = $"[{_pendingTradeItemIndex + 1}/{_pendingTradeItems.Count}] 你可以使用的 {pendingTradeItem.ItemName} 数量最多为 {num}。\n请输入 1 到 {num} 的整数：";
		int maxAmount = num;
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
		{
			if (!int.TryParse(input, out var result) || result <= 0 || result > maxAmount)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的数量。"));
				ShowAmountInquiryForCurrentItem();
			}
			else
			{
				_pendingTradeItems[_pendingTradeItemIndex].Amount = result;
				_pendingTradeItemIndex++;
				ShowAmountInquiryForCurrentItem();
			}
		}, delegate
		{
			string npcName = ((Hero.OneToOneConversationHero != null) ? Hero.OneToOneConversationHero.Name.ToString() : "陌生人");
			ShowResourceSelectionInquiry(npcName);
		}));
	}

	private void ShowTradeChatInput()
	{
		string text = ((Hero.OneToOneConversationHero != null) ? Hero.OneToOneConversationHero.Name.ToString() : "陌生人");
		bool flag = _pendingTrade != null && _pendingTrade.IsGive;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount > 0)
			{
				if (pendingTradeItem.IsGold)
				{
					stringBuilder.AppendLine(flag ? $"  · 给予 {pendingTradeItem.Amount} 第纳尔" : $"  · 展示 {pendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					stringBuilder.AppendLine(flag ? $"  · 给予 {pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}" : $"  · 展示 {pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}");
				}
			}
		}
		string text2 = (flag ? "你准备给予对方以下物品：\n" : "你准备向对方展示以下物品：\n") + stringBuilder.ToString();
		string titleText = "与 " + text + " 交流";
		string text3 = text2 + "\n请输入你想说的话：";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text3, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "说", "取消", OnTradeChatConfirmed, null));
	}

	private void OnTradeChatConfirmed(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			_pendingTrade = null;
			_pendingTradeItems.Clear();
			return;
		}
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		string extraFact = "";
		if (_pendingTrade != null && _pendingTradeItems != null && _pendingTradeItems.Count > 0)
		{
			if (_pendingTrade.IsGive)
			{
				ApplyGiveTransfer(oneToOneConversationHero);
				extraFact = BuildGiveFactText(oneToOneConversationHero);
			}
			else
			{
				ApplyShowRecord(oneToOneConversationHero);
				extraFact = BuildShowFactText(oneToOneConversationHero);
				ShowPendingDisplayValueMessage(oneToOneConversationHero, EstimatePendingShowTotalValue());
			}
		}
		_pendingTradeItems.Clear();
		StartAiConversation(input, extraFact);
	}

	private void ApplyGiveTransfer(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0 || targetHero == null)
		{
			return;
		}
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (pendingTradeItem.IsGold)
			{
				int num = Math.Min(pendingTradeItem.Amount, Hero.MainHero.Gold);
				if (num > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, targetHero, num);
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(targetHero, num, null, 0);
					}
					catch
					{
					}
				}
			}
			else
			{
				if (mobileParty == null)
				{
					continue;
				}
				ItemRoster itemRoster = mobileParty.ItemRoster;
				if (itemRoster == null)
				{
					continue;
				}
				string text = (pendingTradeItem.ItemId ?? pendingTradeItem.Item?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				ItemObject itemObject;
				ItemRoster itemRoster2 = targetHero.PartyBelongedTo?.ItemRoster;
				int num2 = TransferItemsFromRosterByStringId(itemRoster, itemRoster2, text, pendingTradeItem.Amount, out itemObject);
				if (num2 > 0)
				{
					try
					{
						RewardSystemBehavior.Instance?.RecordPlayerPrepaidTransfer(targetHero, 0, text, num2);
					}
					catch
					{
					}
				}
			}
		}
	}

	private void ApplyShowRecord(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0 || targetHero == null)
		{
			return;
		}
		HeroShownRecord shownRecord = GetShownRecord(targetHero);
		if (shownRecord == null)
		{
			return;
		}
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (pendingTradeItem.IsGold)
			{
				shownRecord.ShownGold += pendingTradeItem.Amount;
				continue;
			}
			if (!shownRecord.ShownItems.ContainsKey(pendingTradeItem.ItemId))
			{
				shownRecord.ShownItems[pendingTradeItem.ItemId] = 0;
			}
			shownRecord.ShownItems[pendingTradeItem.ItemId] += pendingTradeItem.Amount;
		}
	}

	private static string BuildPlayerClanUnmarriedCandidatesForPrompt(Hero playerHero, Hero targetHero, int maxEntries = 12)
	{
		try
		{
			Clan clan = playerHero?.Clan;
			if (clan == null)
			{
				return "无（玩家无家族）";
			}
			Hero leader = clan.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(clan)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（玩家家族当前没有基础适婚条件下的未婚成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				Hero hero = list[i];
				string text = hero.Name?.ToString() ?? ("Hero#" + i);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(hero);
				int num = (int)Math.Round(hero.Age);
				string text2 = "";
				if (hero == playerHero)
				{
					text2 = "（玩家本人）";
				}
				else if (hero == leader)
				{
					text2 = "（玩家家族族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num} | StringId={hero.StringId}");
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "无（生成玩家家族未婚名单时发生异常）";
		}
	}

	public static void RecordShownResourcesForExternal(Hero hero, string targetKey, int shownGold, Dictionary<string, int> shownItems)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordShownResources(hero, targetKey, shownGold, shownItems);
		}
		catch
		{
		}
	}

	public static int GetRemainingShowableGoldForExternal(Hero hero, string targetKey, int currentGold)
	{
		try
		{
			MyBehavior campaignBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (campaignBehavior == null)
			{
				return Math.Max(0, currentGold);
			}
			return campaignBehavior.GetRemainingShowableGold(hero, targetKey, currentGold);
		}
		catch
		{
			return Math.Max(0, currentGold);
		}
	}

	public static int GetRemainingShowableItemCountForExternal(Hero hero, string targetKey, string itemId, int currentAmount)
	{
		try
		{
			MyBehavior campaignBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (campaignBehavior == null)
			{
				return Math.Max(0, currentAmount);
			}
			return campaignBehavior.GetRemainingShowableItemCount(hero, targetKey, itemId, currentAmount);
		}
		catch
		{
			return Math.Max(0, currentAmount);
		}
	}

	private static string GetTradeTargetDisplayName(Hero targetHero)
	{
		string text = (targetHero?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "对方" : text;
	}

	private string BuildGiveFactText(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return "";
		}
		string text = Hero.MainHero?.Name?.ToString() ?? "玩家";
		string text2 = GetTradeTargetDisplayName(targetHero);
		List<string> list = new List<string>();
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount > 0)
			{
				if (pendingTradeItem.IsGold)
				{
					list.Add($"{pendingTradeItem.Amount} 第纳尔");
				}
				else
				{
					string text4 = pendingTradeItem.ItemId ?? pendingTradeItem.Item?.StringId ?? "";
					string text5 = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(targetHero ?? Hero.MainHero, text4, pendingTradeItem.Amount) ?? "";
					list.Add($"{pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}{text5}");
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return text + "已经将 " + string.Join("、", list) + " 交给 " + text2 + "。";
	}

	private string BuildShowFactText(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return "";
		}
		string text = Hero.MainHero?.Name?.ToString() ?? "玩家";
		string text2 = GetTradeTargetDisplayName(targetHero);
		List<string> list = new List<string>();
		long num = 0L;
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount > 0)
			{
				if (pendingTradeItem.IsGold)
				{
					list.Add($"{pendingTradeItem.Amount} 第纳尔");
					num += pendingTradeItem.Amount;
				}
				else
				{
					string text3 = RewardSystemBehavior.Instance?.BuildInventoryActualItemValueFactSuffixForExternal(pendingTradeItem.Item, pendingTradeItem.Amount, pendingTradeItem.InventoryUnitValue) ?? "";
					num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(pendingTradeItem.Item, pendingTradeItem.Amount, pendingTradeItem.InventoryUnitValue) ?? 0L;
					list.Add($"{pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}{text3}");
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return text + "给 " + text2 + " 看了看 总值为 " + num + " 第纳尔的各类财物：" + string.Join("、", list) + "，但没有将其给入你的库存。";
	}

	private long EstimatePendingShowTotalValue()
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return 0L;
		}
		long num = 0L;
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem == null || pendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (pendingTradeItem.IsGold)
			{
				num += pendingTradeItem.Amount;
			}
			else
			{
				num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(pendingTradeItem.Item, pendingTradeItem.Amount, pendingTradeItem.InventoryUnitValue) ?? 0L;
			}
		}
		return num;
	}

	private void ShowPendingDisplayValueMessage(Hero targetHero, long totalValue)
	{
		if (totalValue <= 0)
		{
			return;
		}
		string tradeTargetDisplayName = GetTradeTargetDisplayName(targetHero);
		InformationManager.DisplayMessage(new InformationMessage("【展示估值】你向 " + tradeTargetDisplayName + " 展示了总值为 " + totalValue + " 第纳尔的财物。", new Color(0.95f, 0.85f, 0.25f)));
	}

	private List<TradeResourceOption> BuildResourceOptions(Hero targetHero)
	{
		List<TradeResourceOption> list = new List<TradeResourceOption>();
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return list;
		}
		ItemRoster itemRoster = mobileParty.ItemRoster;
		HeroShownRecord heroShownRecord = ((_pendingTrade != null && !_pendingTrade.IsGive) ? GetShownRecord(targetHero) : null);
		int gold = Hero.MainHero.Gold;
		if (_pendingTrade != null && _pendingTrade.IsGive)
		{
			if (gold > 0)
			{
				list.Add(new TradeResourceOption
				{
					IsGold = true,
					ItemId = null,
					Name = "第纳尔",
					AvailableAmount = gold,
					Item = null
				});
			}
		}
		else
		{
			int num = gold;
			if (heroShownRecord != null)
			{
				num = Math.Max(0, gold - heroShownRecord.ShownGold);
			}
			if (num > 0)
			{
				list.Add(new TradeResourceOption
				{
					IsGold = true,
					ItemId = null,
					Name = "第纳尔",
					AvailableAmount = num,
					Item = null
				});
			}
		}
		if (itemRoster != null)
		{
			Dictionary<string, TradeResourceOption> dictionary = new Dictionary<string, TradeResourceOption>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				int amount = elementCopyAtIndex.Amount;
				if (item == null || amount <= 0)
				{
					continue;
				}
				string text = (item.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (dictionary.TryGetValue(text, out var value2))
				{
					value2.AvailableAmount += amount;
					value2.InventoryTotalValue += (long)amount * (RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(elementCopyAtIndex.EquipmentElement) ?? 1);
				}
				else
				{
					dictionary[text] = new TradeResourceOption
					{
						IsGold = false,
						ItemId = text,
						Name = item.Name.ToString(),
						AvailableAmount = amount,
						Item = item,
						InventoryTotalValue = (long)amount * (RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(elementCopyAtIndex.EquipmentElement) ?? 1)
					};
				}
			}
			foreach (TradeResourceOption value3 in dictionary.Values)
			{
				int num2 = value3.AvailableAmount;
				value3.InventoryUnitValue = (num2 > 0) ? Math.Max(1, (int)Math.Round((double)value3.InventoryTotalValue / (double)num2, MidpointRounding.AwayFromZero)) : 1;
				if (_pendingTrade != null && !_pendingTrade.IsGive && heroShownRecord != null && heroShownRecord.ShownItems.TryGetValue(value3.ItemId, out var value4))
				{
					num2 = Math.Max(0, num2 - value4);
				}
				if (num2 > 0)
				{
					value3.AvailableAmount = num2;
					list.Add(value3);
				}
			}
		}
		return list;
	}

	public static int TransferItemsFromRosterByStringId(ItemRoster sourceRoster, ItemRoster targetRoster, string itemId, int amount, out ItemObject transferredItem)
	{
		transferredItem = null;
		if (sourceRoster == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
		{
			return 0;
		}
		string text = itemId.Trim();
		int num = amount;
		int num2 = 0;
		while (num > 0)
		{
			bool flag = false;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = sourceRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement.Item;
				if (item == null || elementCopyAtIndex.Amount <= 0 || !string.Equals(item.StringId ?? "", text, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				transferredItem = transferredItem ?? item;
				int num3 = Math.Min(elementCopyAtIndex.Amount, num);
				if (num3 <= 0)
				{
					continue;
				}
				sourceRoster.AddToCounts(equipmentElement, -num3);
				targetRoster?.AddToCounts(equipmentElement, num3);
				num -= num3;
				num2 += num3;
				flag = true;
				break;
			}
			if (!flag)
			{
				break;
			}
		}
		return num2;
	}

	public static int RemoveItemsFromRosterByStringId(ItemRoster itemRoster, string itemId, int amount, out ItemObject removedItem)
	{
		return TransferItemsFromRosterByStringId(itemRoster, null, itemId, amount, out removedItem);
	}

	private static string ParseLordIdFromUnnamedKey(string unnamedKey)
	{
		string text = (unnamedKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		string text2 = text.Substring(num + ":lord:".Length).Trim();
		int num2 = text2.IndexOf(':');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2).Trim();
		}
		return text2;
	}

	private static Hero ResolveTransferTargetHeroFromAgent(Agent agent)
	{
		try
		{
			CharacterObject characterObject = agent?.Character as CharacterObject;
			if (characterObject?.HeroObject != null)
			{
				return characterObject.HeroObject;
			}
		}
		catch
		{
		}
		try
		{
			string text = ParseLordIdFromUnnamedKey(ShoutUtils.ExtractNpcData(agent)?.UnnamedKey);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return Hero.Find(text);
			}
		}
		catch
		{
		}
		try
		{
			return Settlement.CurrentSettlement?.OwnerClan?.Leader;
		}
		catch
		{
			return null;
		}
	}

	private static PartyBase ResolvePartyTransferCounterpartyInternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				if (hero.PartyBelongedTo?.Party != null)
				{
					return hero.PartyBelongedTo.Party;
				}
				if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null)
				{
					return hero.PartyBelongedToAsPrisoner;
				}
				if (hero.Clan?.Leader?.PartyBelongedTo?.Party != null)
				{
					return hero.Clan.Leader.PartyBelongedTo.Party;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (targetAgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				PartyBase partyBase = agent?.Origin?.BattleCombatant as PartyBase;
				if (partyBase != null)
				{
					return partyBase;
				}
				Hero hero2 = ResolveTransferTargetHeroFromAgent(agent);
				if (hero2?.PartyBelongedTo?.Party != null)
				{
					return hero2.PartyBelongedTo.Party;
				}
				if (hero2?.Clan?.Leader?.PartyBelongedTo?.Party != null)
				{
					return hero2.Clan.Leader.PartyBelongedTo.Party;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement?.Town?.GarrisonParty?.Party != null)
			{
				return Settlement.CurrentSettlement.Town.GarrisonParty.Party;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement?.OwnerClan?.Leader?.PartyBelongedTo?.Party != null)
			{
				return Settlement.CurrentSettlement.OwnerClan.Leader.PartyBelongedTo.Party;
			}
		}
		catch
		{
		}
		try
		{
			return Settlement.CurrentSettlement?.Party;
		}
		catch
		{
			return null;
		}
	}

	public static PartyBase ResolvePartyTransferCounterpartyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		return ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
	}

	private static string ResolvePartyTransferTargetDisplayName(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		string text = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		try
		{
			if (targetAgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				text = (agent?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "对方";
	}

	private static string GetPartyTransferEntryDisplayName(CharacterObject character)
	{
		if (character == null)
		{
			return "未知";
		}
		string text = (character.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (character.StringId ?? "未知").Trim();
	}

	private static void AddPartyTransferEntriesFromRoster(List<PartyTransferPromptEntry> entries, TroopRoster roster, PartyBase ownerParty, PartyTransferEntrySection section, ref int nextPromptIndex)
	{
		if (entries == null || roster == null)
		{
			return;
		}
		bool flag = section == PartyTransferEntrySection.PlayerPrisoners || section == PartyTransferEntrySection.NpcPrisoners;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			CharacterObject character = elementCopyAtIndex.Character;
			if (character == null || elementCopyAtIndex.Number <= 0)
			{
				continue;
			}
			if (!flag && (character.IsHero || character == CharacterObject.PlayerCharacter))
			{
				continue;
			}
			PartyTransferPromptEntry item = new PartyTransferPromptEntry
			{
				PromptIndex = nextPromptIndex++,
				Section = section,
				Character = character,
				DisplayName = GetPartyTransferEntryDisplayName(character),
				Count = Math.Max(0, elementCopyAtIndex.Number),
				WoundedCount = flag ? 0 : Math.Max(0, elementCopyAtIndex.WoundedNumber),
				WageDenarsPerDay = flag ? 0 : Math.Max(1, character.TroopWage),
				HirePriceDenarsPerUnit = flag ? 0 : GetPartyTransferHirePrice(character),
				BuyPriceDenarsPerUnit = flag ? GetPartyTransferPrisonerPrice(character) : 0,
				IsHero = character.IsHero,
				OwnerParty = ownerParty
			};
			entries.Add(item);
		}
	}

	private static int GetPartyTransferHirePrice(CharacterObject character)
	{
		if (character == null)
		{
			return 1;
		}
		try
		{
			return Math.Max(1, Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(character, Hero.MainHero, withoutItemCost: false).RoundedResultNumber);
		}
		catch
		{
			return 1;
		}
	}

	private static int GetPartyTransferPrisonerPrice(CharacterObject character)
	{
		if (character == null)
		{
			return 1;
		}
		try
		{
			return Math.Max(1, Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(character, null));
		}
		catch
		{
			return 1;
		}
	}

	private static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesInternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		List<PartyTransferPromptEntry> list = new List<PartyTransferPromptEntry>();
		PartyBase partyBase = Hero.MainHero?.PartyBelongedTo?.Party ?? MobileParty.MainParty?.Party;
		PartyBase partyBase2 = ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
		int num = 1;
		if (partyBase != null)
		{
			AddPartyTransferEntriesFromRoster(list, partyBase.MemberRoster, partyBase, PartyTransferEntrySection.PlayerTroops, ref num);
			AddPartyTransferEntriesFromRoster(list, partyBase.PrisonRoster, partyBase, PartyTransferEntrySection.PlayerPrisoners, ref num);
		}
		if (partyBase2 != null)
		{
			AddPartyTransferEntriesFromRoster(list, partyBase2.MemberRoster, partyBase2, PartyTransferEntrySection.NpcTroops, ref num);
			AddPartyTransferEntriesFromRoster(list, partyBase2.PrisonRoster, partyBase2, PartyTransferEntrySection.NpcPrisoners, ref num);
		}
		return list;
	}

	public static List<PartyTransferPromptEntry> BuildPartyTransferPromptEntriesForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			return BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex);
		}
		catch
		{
			return new List<PartyTransferPromptEntry>();
		}
	}

	private static void AppendPartyTransferPromptSection(StringBuilder sb, string header, IEnumerable<PartyTransferPromptEntry> entries, bool isPrisoner, bool showPromptIndex)
	{
		if (sb == null)
		{
			return;
		}
		sb.AppendLine(header);
		List<PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<PartyTransferPromptEntry>()).ToList();
		if (list.Count == 0)
		{
			sb.AppendLine("（无）");
			return;
		}
		foreach (PartyTransferPromptEntry item in list)
		{
			if (item == null)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (showPromptIndex)
			{
				stringBuilder.Append(item.PromptIndex).Append(" ");
			}
			stringBuilder.Append(item.DisplayName).Append(" | 数量 ").Append(Math.Max(0, item.Count));
			if (isPrisoner)
			{
				if (item.IsHero)
				{
					stringBuilder.Append(" | 英雄俘虏");
				}
				stringBuilder.Append(" | 购买价 ").Append(Math.Max(1, item.BuyPriceDenarsPerUnit)).Append("第纳尔/人");
			}
			else
			{
				if (item.WoundedCount > 0)
				{
					stringBuilder.Append(" | 其中伤兵 ").Append(item.WoundedCount);
				}
				stringBuilder.Append(" | 日薪 ").Append(Math.Max(1, item.WageDenarsPerDay)).Append("第纳尔/天");
				stringBuilder.Append(" | 雇佣价 ").Append(Math.Max(1, item.HirePriceDenarsPerUnit)).Append("第纳尔/人");
			}
			sb.AppendLine(stringBuilder.ToString());
		}
	}

	private static int GetPartyTransferTroopTier(PartyTransferPromptEntry entry)
	{
		return Math.Max(0, entry?.Character?.Tier ?? 0);
	}

	private static int ResolvePartyTransferRecruitTrustLevelIndex(Hero targetHero, CharacterObject targetCharacter)
	{
		int num = 6;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				return RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance?.GetEffectiveTrust(hero) ?? 0);
			}
			if (targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
			{
				return RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(Settlement.CurrentSettlement, kind));
			}
		}
		catch
		{
		}
		return num;
	}

	private static int ResolvePartyTransferRecruitMaxTier(int trustLevelIndex)
	{
		if (trustLevelIndex <= 4)
		{
			return 0;
		}
		if (trustLevelIndex == 5)
		{
			return 1;
		}
		if (trustLevelIndex == 6)
		{
			return 2;
		}
		if (trustLevelIndex == 7)
		{
			return 3;
		}
		if (trustLevelIndex == 8)
		{
			return 4;
		}
		return int.MaxValue;
	}

	private static void AppendPartyTransferHiddenTroopSection(StringBuilder sb, string header, IEnumerable<PartyTransferPromptEntry> entries)
	{
		if (sb == null)
		{
			return;
		}
		List<PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<PartyTransferPromptEntry>()).Where((PartyTransferPromptEntry x) => x != null).ToList();
		if (list.Count == 0)
		{
			return;
		}
		sb.AppendLine(header);
		foreach (PartyTransferPromptEntry item in list)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(item.DisplayName).Append(" | 阶级 ").Append(GetPartyTransferTroopTier(item)).Append(" | 数量 ").Append(Math.Max(0, item.Count));
			if (item.WoundedCount > 0)
			{
				stringBuilder.Append(" | 其中伤兵 ").Append(item.WoundedCount);
			}
			sb.AppendLine(stringBuilder.ToString());
		}
	}

	private static List<PartyTransferPromptEntry> BuildDisplayIndexedPartyTransferEntries(IEnumerable<PartyTransferPromptEntry> entries)
	{
		List<PartyTransferPromptEntry> list = new List<PartyTransferPromptEntry>();
		int num = 1;
		foreach (PartyTransferPromptEntry entry in entries ?? Enumerable.Empty<PartyTransferPromptEntry>())
		{
			if (entry == null)
			{
				continue;
			}
			list.Add(new PartyTransferPromptEntry
			{
				PromptIndex = num++,
				Section = entry.Section,
				Character = entry.Character,
				DisplayName = entry.DisplayName,
				Count = entry.Count,
				WoundedCount = entry.WoundedCount,
				WageDenarsPerDay = entry.WageDenarsPerDay,
				HirePriceDenarsPerUnit = entry.HirePriceDenarsPerUnit,
				BuyPriceDenarsPerUnit = entry.BuyPriceDenarsPerUnit,
				IsHero = entry.IsHero,
				OwnerParty = entry.OwnerParty
			});
		}
		return list;
	}

	private static PartyTransferPromptEntry ResolveNpcTransferEntryByDisplayIndex(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferEntrySection section, int displayIndex)
	{
		if (displayIndex <= 0)
		{
			return null;
		}
		List<PartyTransferPromptEntry> list = BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex);
		IEnumerable<PartyTransferPromptEntry> enumerable = Enumerable.Empty<PartyTransferPromptEntry>();
		if (section == PartyTransferEntrySection.NpcTroops)
		{
			int partyTransferRecruitTrustLevelIndex = ResolvePartyTransferRecruitTrustLevelIndex(targetHero, targetCharacter);
			int partyTransferRecruitMaxTier = ResolvePartyTransferRecruitMaxTier(partyTransferRecruitTrustLevelIndex);
			if (partyTransferRecruitMaxTier > 0)
			{
				enumerable = list.Where((PartyTransferPromptEntry x) => x != null && x.Section == PartyTransferEntrySection.NpcTroops && GetPartyTransferTroopTier(x) > 0 && GetPartyTransferTroopTier(x) <= partyTransferRecruitMaxTier);
			}
		}
		else if (section == PartyTransferEntrySection.NpcPrisoners)
		{
			enumerable = list.Where((PartyTransferPromptEntry x) => x != null && x.Section == PartyTransferEntrySection.NpcPrisoners);
		}
		return enumerable.Skip(displayIndex - 1).FirstOrDefault();
	}

	public static string BuildPartyTransferRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		try
		{
			List<PartyTransferPromptEntry> list = BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex);
			string text = BuildPlayerPublicDisplayNameForPrompt();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			int partyTransferRecruitTrustLevelIndex = ResolvePartyTransferRecruitTrustLevelIndex(targetHero, targetCharacter);
			int partyTransferRecruitMaxTier = ResolvePartyTransferRecruitMaxTier(partyTransferRecruitTrustLevelIndex);
			List<PartyTransferPromptEntry> list2 = list.Where((PartyTransferPromptEntry x) => x != null && x.Section == PartyTransferEntrySection.NpcTroops).ToList();
			List<PartyTransferPromptEntry> list3 = list2.Where((PartyTransferPromptEntry x) => GetPartyTransferTroopTier(x) > 0 && GetPartyTransferTroopTier(x) <= partyTransferRecruitMaxTier).ToList();
			List<PartyTransferPromptEntry> list4 = list2.Where((PartyTransferPromptEntry x) => !list3.Contains(x)).ToList();
			List<PartyTransferPromptEntry> list5 = BuildDisplayIndexedPartyTransferEntries(list3);
			List<PartyTransferPromptEntry> list6 = BuildDisplayIndexedPartyTransferEntries(list.Where((PartyTransferPromptEntry x) => x.Section == PartyTransferEntrySection.NpcPrisoners));
			StringBuilder stringBuilder = new StringBuilder();
			string runtimeHint = AIConfigHandler.BuildRuntimePartyTransferInstructionForExternal(targetHero, targetCharacter);
			if (!string.IsNullOrWhiteSpace(runtimeHint))
			{
				stringBuilder.AppendLine(runtimeHint.Trim());
			}
			if (partyTransferRecruitMaxTier <= 0)
			{
				stringBuilder.AppendLine("【当前招募限制】你当前对" + text + "的信任不足，任何士兵都不可开放招募。本轮不要展示【你当前可转移部队】清单，也绝不可以输出任何 ATT 标签。");
			}
			else if (partyTransferRecruitMaxTier == int.MaxValue)
			{
				stringBuilder.AppendLine("【当前招募限制】你当前对" + text + "的信任已足够，所有已编号的士兵都可正常谈招募。若你决定放人，可在句末输出 [ATT:序号:数量]。");
			}
			else
			{
				stringBuilder.AppendLine("【当前招募限制】你当前只可向" + text + "开放 " + partyTransferRecruitMaxTier + " 阶及以下士兵的招募。只有本轮仍带编号的士兵才可使用 ATT。");
				stringBuilder.AppendLine("【隐藏编号硬约束】未编号的更高阶兵种绝不可以卖给" + text + "，也绝不可以对其输出 ATT 标签。");
			}
			stringBuilder.AppendLine("若你决定把【你当前可转移俘虏】中的俘虏交给玩家，可在句末输出 [ATP:序号:数量]。");
			stringBuilder.AppendLine("可用于 ATT/ATP 的序号只来自本轮仍带编号的清单。未编号或未展示的兵种/俘虏，绝不可以使用 ATT/ATP。");
			stringBuilder.AppendLine("ATT 只能用于部队序号，ATP 只能用于俘虏序号；数量不得超过当前数量。若本轮尚未明确成交，就不要输出。");
			stringBuilder.AppendLine("日薪表示每名士兵每天需要支付多少第纳尔；雇佣价与购买价表示当前谈判指导单价。");
			stringBuilder.AppendLine("当前与你交易的人：" + text);
			AppendPartyTransferPromptSection(stringBuilder, "【玩家当前可转移部队】：", list.Where((PartyTransferPromptEntry x) => x.Section == PartyTransferEntrySection.PlayerTroops), isPrisoner: false, showPromptIndex: false);
			AppendPartyTransferPromptSection(stringBuilder, "【玩家当前可转移俘虏】：", list.Where((PartyTransferPromptEntry x) => x.Section == PartyTransferEntrySection.PlayerPrisoners), isPrisoner: true, showPromptIndex: false);
			if (partyTransferRecruitMaxTier > 0)
			{
				AppendPartyTransferPromptSection(stringBuilder, "【你当前可转移部队】：", list5, isPrisoner: false, showPromptIndex: true);
			}
			if (partyTransferRecruitMaxTier > 0 && partyTransferRecruitMaxTier < int.MaxValue)
			{
				AppendPartyTransferHiddenTroopSection(stringBuilder, "【由于你对" + text + "不够信任，你当前不可向" + text + "开放招募的更高阶部队】：", list4);
			}
			AppendPartyTransferPromptSection(stringBuilder, "【你当前可转移俘虏】：", list6, isPrisoner: true, showPromptIndex: true);
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private static int TransferPartyMemberEntry(PartyTransferPromptEntry entry, int requestedAmount, PartyBase targetParty)
	{
		if (entry == null || targetParty == null || entry.OwnerParty == null || entry.OwnerParty == targetParty || entry.Character == null)
		{
			return 0;
		}
		TroopRoster memberRoster = entry.OwnerParty.MemberRoster;
		TroopRoster memberRoster2 = targetParty.MemberRoster;
		if (memberRoster == null || memberRoster2 == null)
		{
			return 0;
		}
		int num = memberRoster.FindIndexOfTroop(entry.Character);
		if (num < 0)
		{
			return 0;
		}
		TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(num);
		int num2 = Math.Min(Math.Max(0, requestedAmount), Math.Max(0, elementCopyAtIndex.Number));
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = 0;
		if (elementCopyAtIndex.Number > 0 && elementCopyAtIndex.WoundedNumber > 0)
		{
			num3 = (num2 >= elementCopyAtIndex.Number) ? elementCopyAtIndex.WoundedNumber : Math.Min(num2, Math.Max(0, (int)Math.Round((double)elementCopyAtIndex.WoundedNumber * (double)num2 / (double)elementCopyAtIndex.Number, MidpointRounding.AwayFromZero)));
		}
		int num4 = 0;
		if (elementCopyAtIndex.Number > 0 && elementCopyAtIndex.Xp > 0)
		{
			num4 = (num2 >= elementCopyAtIndex.Number) ? elementCopyAtIndex.Xp : Math.Max(0, (int)Math.Round((double)elementCopyAtIndex.Xp * (double)num2 / (double)elementCopyAtIndex.Number, MidpointRounding.AwayFromZero));
		}
		memberRoster.AddToCounts(entry.Character, -num2, insertAtFront: false, -num3, 0, false, -1);
		if (num4 > 0)
		{
			memberRoster.AddXpToTroop(entry.Character, -num4);
		}
		memberRoster2.AddToCounts(entry.Character, num2, insertAtFront: false, num3, 0, false, -1);
		if (num4 > 0)
		{
			memberRoster2.AddXpToTroop(entry.Character, num4);
		}
		return num2;
	}

	private static int TransferPartyPrisonerEntry(PartyTransferPromptEntry entry, int requestedAmount, PartyBase targetParty)
	{
		if (entry == null || targetParty == null || entry.OwnerParty == null || entry.OwnerParty == targetParty || entry.Character == null)
		{
			return 0;
		}
		if (entry.Character.IsHero)
		{
			if (requestedAmount <= 0)
			{
				return 0;
			}
			try
			{
				TransferPrisonerAction.Apply(entry.Character, entry.OwnerParty, targetParty);
				return 1;
			}
			catch
			{
				return 0;
			}
		}
		TroopRoster prisonRoster = entry.OwnerParty.PrisonRoster;
		if (prisonRoster == null)
		{
			return 0;
		}
		int num = prisonRoster.FindIndexOfTroop(entry.Character);
		if (num < 0)
		{
			return 0;
		}
		TroopRosterElement elementCopyAtIndex = prisonRoster.GetElementCopyAtIndex(num);
		int num2 = Math.Min(Math.Max(0, requestedAmount), Math.Max(0, elementCopyAtIndex.Number));
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = 0;
		if (elementCopyAtIndex.Number > 0 && elementCopyAtIndex.Xp > 0)
		{
			num3 = (num2 >= elementCopyAtIndex.Number) ? elementCopyAtIndex.Xp : Math.Max(0, (int)Math.Round((double)elementCopyAtIndex.Xp * (double)num2 / (double)elementCopyAtIndex.Number, MidpointRounding.AwayFromZero));
		}
		prisonRoster.AddToCounts(entry.Character, -num2, insertAtFront: false, 0, 0, false, -1);
		if (num3 > 0)
		{
			prisonRoster.AddXpToTroop(entry.Character, -num3);
		}
		targetParty.AddPrisoner(entry.Character, num2);
		if (num3 > 0)
		{
			targetParty.PrisonRoster?.AddXpToTroop(entry.Character, num3);
		}
		return num2;
	}

	private static string BuildNpcToPlayerTroopTransferFact(string npcName, PartyTransferPromptEntry entry, int amount)
	{
		return "[AFEF NPC行为补充] " + npcName + "已将" + amount + "名" + entry.DisplayName + "转入玩家麾下。";
	}

	private static string BuildNpcToPlayerPrisonerTransferFact(string npcName, PartyTransferPromptEntry entry, int amount)
	{
		if (entry?.IsHero ?? false)
		{
			return "[AFEF NPC行为补充] " + npcName + "已将俘虏" + entry.DisplayName + "交给玩家。";
		}
		return "[AFEF NPC行为补充] " + npcName + "已将" + amount + "名" + entry.DisplayName + "俘虏交给玩家。";
	}

	public static bool TryApplyPartyTransferTagsForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		try
		{
			string text = content ?? "";
			MatchCollection matchCollection = TransferTroopTagRegex.Matches(text);
			MatchCollection matchCollection2 = TransferPrisonerTagRegex.Matches(text);
			if ((matchCollection?.Count ?? 0) <= 0 && (matchCollection2?.Count ?? 0) <= 0)
			{
				return false;
			}
			List<PartyTransferPromptEntry> list = BuildPartyTransferPromptEntriesInternal(targetHero, targetCharacter, targetAgentIndex);
			PartyBase party = Hero.MainHero?.PartyBelongedTo?.Party ?? MobileParty.MainParty?.Party;
			string text2 = ResolvePartyTransferTargetDisplayName(targetHero, targetCharacter, targetAgentIndex);
			bool flag = false;
			if (party != null)
			{
				foreach (Match item in matchCollection)
				{
					if (!item.Success)
					{
						continue;
					}
					int num = 0;
					int num2 = 0;
					int.TryParse(item.Groups[1].Value, out num);
					int.TryParse(item.Groups[2].Value, out num2);
					PartyTransferPromptEntry partyTransferPromptEntry = ResolveNpcTransferEntryByDisplayIndex(targetHero, targetCharacter, targetAgentIndex, PartyTransferEntrySection.NpcTroops, num);
					int num3 = TransferPartyMemberEntry(partyTransferPromptEntry, num2, party);
					if (num3 > 0)
					{
						flag = true;
						generatedFacts.Add(BuildNpcToPlayerTroopTransferFact(text2, partyTransferPromptEntry, num3));
						notifications.Add("已获得 " + num3 + " 名" + partyTransferPromptEntry.DisplayName);
					}
				}
				foreach (Match item2 in matchCollection2)
				{
					if (!item2.Success)
					{
						continue;
					}
					int num4 = 0;
					int num5 = 0;
					int.TryParse(item2.Groups[1].Value, out num4);
					int.TryParse(item2.Groups[2].Value, out num5);
					PartyTransferPromptEntry partyTransferPromptEntry2 = ResolveNpcTransferEntryByDisplayIndex(targetHero, targetCharacter, targetAgentIndex, PartyTransferEntrySection.NpcPrisoners, num4);
					int num6 = TransferPartyPrisonerEntry(partyTransferPromptEntry2, num5, party);
					if (num6 > 0)
					{
						flag = true;
						generatedFacts.Add(BuildNpcToPlayerPrisonerTransferFact(text2, partyTransferPromptEntry2, num6));
						notifications.Add("已获得 " + (partyTransferPromptEntry2.IsHero ? ("俘虏" + partyTransferPromptEntry2.DisplayName) : (num6 + " 名" + partyTransferPromptEntry2.DisplayName + "俘虏")));
					}
				}
			}
			text = TransferTroopTagRegex.Replace(text, "").Trim();
			text = TransferPrisonerTagRegex.Replace(text, "").Trim();
			content = text;
			return flag;
		}
		catch
		{
			content = TransferTroopTagRegex.Replace(content ?? "", "").Trim();
			content = TransferPrisonerTagRegex.Replace(content, "").Trim();
			return false;
		}
	}

	public static string BuildPlayerToNpcTroopTransferFactForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
	{
		string text = ResolvePartyTransferTargetDisplayName(targetHero, targetCharacter, targetAgentIndex);
		string text2 = BuildPlayerPublicDisplayNameForPrompt();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		return "[AFEF玩家行为补充] " + text2 + "已将" + amount + "名" + entry.DisplayName + "转入" + text + "的麾下。";
	}

	public static string BuildPlayerToNpcPrisonerTransferFactForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
	{
		string text = ResolvePartyTransferTargetDisplayName(targetHero, targetCharacter, targetAgentIndex);
		string text2 = BuildPlayerPublicDisplayNameForPrompt();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "玩家";
		}
		if (entry?.IsHero ?? false)
		{
			return "[AFEF玩家行为补充] " + text2 + "已将俘虏" + entry.DisplayName + "交给" + text + "。";
		}
		return "[AFEF玩家行为补充] " + text2 + "已将" + amount + "名" + entry.DisplayName + "俘虏交给" + text + "。";
	}

	public static int TransferPlayerPartyEntryToCounterpartyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, PartyTransferPromptEntry entry, int amount)
	{
		PartyBase partyBase = ResolvePartyTransferCounterpartyInternal(targetHero, targetCharacter, targetAgentIndex);
		if (entry == null || partyBase == null)
		{
			return 0;
		}
		if (entry.Section == PartyTransferEntrySection.PlayerTroops)
		{
			return TransferPartyMemberEntry(entry, amount, partyBase);
		}
		if (entry.Section == PartyTransferEntrySection.PlayerPrisoners)
		{
			return TransferPartyPrisonerEntry(entry, amount, partyBase);
		}
		return 0;
	}

	private HeroShownRecord GetShownRecord(Hero hero)
	{
		return GetShownRecord(hero, null, createIfMissing: true);
	}

	private HeroShownRecord GetShownRecord(Hero hero, string targetKey, bool createIfMissing)
	{
		string text = ResolveShownRecordKey(hero, targetKey);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (!_shownRecords.TryGetValue(text, out var value))
		{
			if (!createIfMissing)
			{
				return null;
			}
			value = new HeroShownRecord();
			_shownRecords[text] = value;
		}
		if (value.ShownItems == null)
		{
			value.ShownItems = new Dictionary<string, int>();
		}
		return value;
	}

	private void RecordShownResources(Hero hero, string targetKey, int shownGold, Dictionary<string, int> shownItems)
	{
		if (shownGold <= 0 && (shownItems == null || shownItems.Count == 0))
		{
			return;
		}
		HeroShownRecord shownRecord = GetShownRecord(hero, targetKey, createIfMissing: true);
		if (shownRecord == null)
		{
			return;
		}
		if (shownGold > 0)
		{
			shownRecord.ShownGold += shownGold;
		}
		if (shownItems == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> shownItem in shownItems)
		{
			string text = (shownItem.Key ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || shownItem.Value <= 0)
			{
				continue;
			}
			if (!shownRecord.ShownItems.ContainsKey(text))
			{
				shownRecord.ShownItems[text] = 0;
			}
			shownRecord.ShownItems[text] += shownItem.Value;
		}
	}

	private int GetRemainingShowableGold(Hero hero, string targetKey, int currentGold)
	{
		HeroShownRecord shownRecord = GetShownRecord(hero, targetKey, createIfMissing: false);
		if (shownRecord == null)
		{
			return Math.Max(0, currentGold);
		}
		return Math.Max(0, currentGold - Math.Max(0, shownRecord.ShownGold));
	}

	private int GetRemainingShowableItemCount(Hero hero, string targetKey, string itemId, int currentAmount)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return Math.Max(0, currentAmount);
		}
		HeroShownRecord shownRecord = GetShownRecord(hero, targetKey, createIfMissing: false);
		if (shownRecord == null || shownRecord.ShownItems == null || !shownRecord.ShownItems.TryGetValue(itemId, out var value))
		{
			return Math.Max(0, currentAmount);
		}
		return Math.Max(0, currentAmount - Math.Max(0, value));
	}

	private static string ResolveShownRecordKey(Hero hero, string targetKey)
	{
		string text = hero?.StringId;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = targetKey;
		}
		return NormalizeShownRecordKey(text);
	}

	private static string NormalizeShownRecordKey(string targetKey)
	{
		return (targetKey ?? "").Trim().ToLowerInvariant();
	}

	// Maintenance note: this direct-dialogue path is kept for compatibility only.
	// The mod's primary NPC chat path is the scene shout chain; prioritize that path for new work.
	private void StartAiConversation(string input, string extraFact)
	{
		Hero targetHero = Hero.OneToOneConversationHero;
		CharacterObject targetCharacter = null;
		int targetAgentIndex = -1;
		try
		{
			targetCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
		}
		catch
		{
		}
		try
		{
			targetAgentIndex = (Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent)?.Index ?? (-1);
		}
		catch
		{
			targetAgentIndex = -1;
		}
		string npcName = targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC";
		string cultureId = targetHero?.Culture?.StringId ?? targetCharacter?.Culture?.StringId ?? "neutral";
		int playerTier = Hero.MainHero.Clan.Tier;
		string characterDescription = GetHeroDescriptionSafe(targetHero);
		string locationInfo = GetCurrentLocationInfoSafe();
		bool hasUnpaidDebt = RewardSystemBehavior.Instance != null && targetHero != null && RewardSystemBehavior.Instance.HasUnpaidDebt(targetHero);
		if (hasUnpaidDebt)
		{
			Logger.Log("Logic", "[Trade] 检测到玩家尚欠 " + npcName + " 的货款或物品，疑似上一笔交易逃单。");
		}
		InformationManager.DisplayMessage(new InformationMessage(npcName + " 正在思考...", new Color(0.7f, 0.7f, 0.7f)));
		Task.Run(async delegate
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			using (Logger.BeginTrace("direct", targetHero?.StringId, npcName))
			{
				Logger.Obs("DirectChat", "start", new Dictionary<string, object>
				{
					["npc"] = npcName,
					["heroId"] = targetHero?.StringId ?? "",
					["inputLen"] = (input ?? "").Length,
					["hasUnpaidDebt"] = hasUnpaidDebt
				});
				await Task.Delay(300);
				SimulateLeftClick();
				try
				{
					if (targetHero != null)
					{
						bool showPersonaHint = !TryGetNpcPersonaGenerationStatusForExternal(targetHero, out var needsGeneration, out _) || needsGeneration;
						if (showPersonaHint)
						{
							InformationManager.DisplayMessage(new InformationMessage(BuildNpcPersonaGenerationHintForExternal(targetHero), new Color(1f, 0.85f, 0.3f)));
						}
						_ = EnsureNpcPersonaGeneratedAsync(targetHero);
					}
					AIConfigHandler.SetGuardrailSemanticContext(BuildGuardrailSemanticContext(targetHero, extraFact));
					bool patienceExhausted = false;
					string patienceExhaustedReply = "";
					string patiencePrompt = BuildDirectPatiencePromptForExternal(targetHero, out patienceExhausted, out patienceExhaustedReply);
					try
					{
						string badge = BuildDirectNamePatienceBadgeForExternal(targetHero);
						ConversationHelper.SetNameSuffix(badge);
					}
					catch
					{
					}
					string npcLastUtterance = GetLatestNpcDialogueUtterance(targetHero, targetCharacter);
					bool isSettlementMerchant = targetHero == null && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var _);
					List<string> triggerKeywords = AIConfigHandler.DuelTriggerKeywords;
					string matchedKeyword;
					bool duelLiteralHit = ContainsLiteralKeywordHit(input, triggerKeywords, out matchedKeyword);
					_nextDuelRiskWarningByLiteral = duelLiteralHit;
					bool isTriggerWordDetected = false;
					string duelHitKeyword = "";
					float duelHitScore = 0f;
					if (!patienceExhausted)
					{
						isTriggerWordDetected = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "duel", AIConfigHandler.DuelInstruction, triggerKeywords, out duelHitKeyword, out duelHitScore);
					}
					bool liveDuelSemanticHit = isTriggerWordDetected;
					bool useDuelContext = isTriggerWordDetected;
					List<string> rewardKeywords = AIConfigHandler.RewardTriggerKeywords;
					bool isRewardContext = false;
					string rewardHitKeyword = "";
					float rewardHitScore = 0f;
					if (!patienceExhausted && AIConfigHandler.RewardEnabled)
					{
						isRewardContext = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "reward", AIConfigHandler.RewardInstruction, rewardKeywords, out rewardHitKeyword, out rewardHitScore);
					}
					bool liveRewardSemanticHit = isRewardContext;
					if (isSettlementMerchant)
					{
						isRewardContext = true;
					}
					List<string> loanKeywords = AIConfigHandler.LoanTriggerKeywords;
					bool isLoanContext = false;
					string loanHitKeyword = "";
					float loanHitScore = 0f;
					if (!patienceExhausted && AIConfigHandler.LoanEnabled)
					{
						isLoanContext = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "loan", AIConfigHandler.LoanInstruction, loanKeywords, out loanHitKeyword, out loanHitScore);
					}
					bool liveLoanSemanticHit = isLoanContext;
					if (isSettlementMerchant)
					{
						isLoanContext = true;
					}
					List<string> surroundingsKeywords = AIConfigHandler.SurroundingsTriggerKeywords;
					bool isSurroundingsContext = false;
					string surroundingsHitKeyword = "";
					float surroundingsHitScore = 0f;
					if (!patienceExhausted && AIConfigHandler.SurroundingsEnabled)
					{
						isSurroundingsContext = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "surroundings", AIConfigHandler.SurroundingsInstruction, surroundingsKeywords, out surroundingsHitKeyword, out surroundingsHitScore);
					}
					bool isKingdomServiceHit = false;
					string kingdomServiceHitKeyword = "";
					float kingdomServiceHitScore = 0f;
					if (!patienceExhausted)
					{
						isKingdomServiceHit = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "kingdom_service", AIConfigHandler.GetGuardrailRuleInstruction("kingdom_service"), AIConfigHandler.GetGuardrailRuleKeywords("kingdom_service"), out kingdomServiceHitKeyword, out kingdomServiceHitScore);
					}
					bool isMarriageHit = false;
					string marriageHitKeyword = "";
					float marriageHitScore = 0f;
					if (!patienceExhausted)
					{
						isMarriageHit = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "marriage", AIConfigHandler.GetGuardrailRuleInstruction("marriage"), AIConfigHandler.GetGuardrailRuleKeywords("marriage"), out marriageHitKeyword, out marriageHitScore);
					}
					bool partyTransferHit = false;
					string partyTransferHitKeyword = "";
					float partyTransferHitScore = 0f;
					if (!patienceExhausted)
					{
						partyTransferHit = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "party_transfer", AIConfigHandler.GetGuardrailRuleInstruction("party_transfer"), AIConfigHandler.GetGuardrailRuleKeywords("party_transfer"), out partyTransferHitKeyword, out partyTransferHitScore);
					}
					bool useRewardContext = isRewardContext;
					if (partyTransferHit)
					{
						useRewardContext = AIConfigHandler.RewardEnabled || useRewardContext;
						if (AIConfigHandler.LoanEnabled)
						{
							isLoanContext = true;
						}
					}
					if (patienceExhausted)
					{
						useDuelContext = false;
						useRewardContext = false;
						isLoanContext = false;
						isSurroundingsContext = false;
					}
					else
					{
						if (TryConsumeRuleStickyCarry(targetHero, targetCharacter, input, out var carryDuel, out var carryReward, out var carryLoan))
						{
							if (!isTriggerWordDetected && carryDuel)
							{
								isTriggerWordDetected = true;
								useDuelContext = true;
								duelHitKeyword = "sticky";
								duelHitScore = Math.Max(duelHitScore, 0.18f);
							}
							if (!isRewardContext && carryReward)
							{
								isRewardContext = true;
								useRewardContext = true;
								rewardHitKeyword = "sticky";
								rewardHitScore = Math.Max(rewardHitScore, 0.18f);
							}
							if (!isLoanContext && carryLoan)
							{
								isLoanContext = true;
								loanHitKeyword = "sticky";
								loanHitScore = Math.Max(loanHitScore, 0.18f);
							}
						}
						UpdateRuleStickyCarryFromHits(targetHero, targetCharacter, liveDuelSemanticHit, liveRewardSemanticHit, liveLoanSemanticHit);
					}
					string guardrailClarifyHint = "";
					if (!patienceExhausted && !useDuelContext && !useRewardContext && !isLoanContext && !isSurroundingsContext)
					{
						guardrailClarifyHint = AIConfigHandler.BuildGuardrailClarificationHint(input, isTriggerWordDetected, duelHitScore, isRewardContext, rewardHitScore, isLoanContext, loanHitScore, isSurroundingsContext, surroundingsHitScore);
					}
					string duelHits = (string.IsNullOrWhiteSpace(duelHitKeyword) ? "" : $"{duelHitKeyword}@{duelHitScore:0.00}");
					string rewardHits = (string.IsNullOrWhiteSpace(rewardHitKeyword) ? "" : $"{rewardHitKeyword}@{rewardHitScore:0.00}");
					string loanHits = (string.IsNullOrWhiteSpace(loanHitKeyword) ? "" : $"{loanHitKeyword}@{loanHitScore:0.00}");
					string surroundingsHits = (string.IsNullOrWhiteSpace(surroundingsHitKeyword) ? "" : $"{surroundingsHitKeyword}@{surroundingsHitScore:0.00}");
					string kingdomServiceHits = (string.IsNullOrWhiteSpace(kingdomServiceHitKeyword) ? "" : $"{kingdomServiceHitKeyword}@{kingdomServiceHitScore:0.00}");
					string marriageHits = (string.IsNullOrWhiteSpace(marriageHitKeyword) ? "" : $"{marriageHitKeyword}@{marriageHitScore:0.00}");
					string partyTransferHits = (string.IsNullOrWhiteSpace(partyTransferHitKeyword) ? "" : $"{partyTransferHitKeyword}@{partyTransferHitScore:0.00}");
					Logger.Log("Logic", $"[SemanticTrigger] DuelHit={isTriggerWordDetected} [{duelHits}] RewardHit={isRewardContext} [{rewardHits}] LoanHit={isLoanContext} [{loanHits}] PartyTransferHit={partyTransferHit} [{partyTransferHits}] SurroundingsHit={isSurroundingsContext} [{surroundingsHits}] KingdomServiceHit={isKingdomServiceHit} [{kingdomServiceHits}] MarriageHit={isMarriageHit} [{marriageHits}] NpcRecall={(string.IsNullOrWhiteSpace(npcLastUtterance) ? "off" : "on")} Input='{input}' NPC='{npcName}'");
					Logger.Obs("DirectChat", "keywords", new Dictionary<string, object>
					{
						["duelHit"] = isTriggerWordDetected,
						["duelHitKeyword"] = duelHitKeyword ?? "",
						["duelHitScore"] = Math.Round(duelHitScore, 3),
						["rewardHit"] = isRewardContext,
						["rewardHitKeyword"] = rewardHitKeyword ?? "",
						["rewardHitScore"] = Math.Round(rewardHitScore, 3),
						["loanHit"] = isLoanContext,
						["loanHitKeyword"] = loanHitKeyword ?? "",
						["loanHitScore"] = Math.Round(loanHitScore, 3),
						["partyTransferHit"] = partyTransferHit,
						["partyTransferHitKeyword"] = partyTransferHitKeyword ?? "",
						["partyTransferHitScore"] = Math.Round(partyTransferHitScore, 3),
						["surroundingsHit"] = isSurroundingsContext,
						["surroundingsHitKeyword"] = surroundingsHitKeyword ?? "",
						["surroundingsHitScore"] = Math.Round(surroundingsHitScore, 3),
						["kingdomServiceHit"] = isKingdomServiceHit,
						["kingdomServiceHitKeyword"] = kingdomServiceHitKeyword ?? "",
						["kingdomServiceHitScore"] = Math.Round(kingdomServiceHitScore, 3),
						["marriageHit"] = isMarriageHit,
						["marriageHitKeyword"] = marriageHitKeyword ?? "",
						["marriageHitScore"] = Math.Round(marriageHitScore, 3),
						["clarifyHint"] = (string.IsNullOrWhiteSpace(guardrailClarifyHint) ? "" : "on"),
						["npcRecall"] = !string.IsNullOrWhiteSpace(npcLastUtterance)
					});
					StringBuilder sb = new StringBuilder();
					Stopwatch swPromptBuild = Stopwatch.StartNew();
					string basePrompt = AIConfigHandler.GlobalPrompt;
					if (string.IsNullOrEmpty(basePrompt))
					{
						basePrompt = "你是一个中世纪领主。";
					}
					string guardrail = AIConfigHandler.GlobalGuardrail;
					string personaContext = BuildNpcPersonaContext(targetHero);
					bool personaInFlight = false;
					bool personaReadyForFixedCache = IsPersonaReadyForFixedLayer(targetHero, out personaInFlight);
					string fixedLayerCacheKey = BuildDirectFixedLayerCacheKey(targetHero, cultureId, basePrompt, guardrail, characterDescription, personaContext, personaReadyForFixedCache);
					bool fixedLayerCacheHit = false;
					string fixedLayerText = PromptComposer.GetOrBuildFixedLayer(fixedLayerCacheKey, personaReadyForFixedCache, delegate
					{
						StringBuilder stringBuilder = new StringBuilder();
						stringBuilder.AppendLine(basePrompt);
						if (!string.IsNullOrEmpty(guardrail))
						{
							stringBuilder.AppendLine(guardrail);
						}
						stringBuilder.AppendLine(BuildDialogueFormatGuardrailPrompt());
						if (!string.IsNullOrWhiteSpace(characterDescription))
						{
							stringBuilder.AppendLine(characterDescription);
						}
						if (!string.IsNullOrEmpty(personaContext))
						{
							stringBuilder.AppendLine(personaContext);
						}
						return stringBuilder.ToString();
					}, out fixedLayerCacheHit);
					DuelSettings settings = DuelSettings.GetSettings();
					int minTier = settings.MinimumClanTier;
					bool isQualified = playerTier >= minTier;
					if (!string.IsNullOrWhiteSpace(locationInfo))
					{
						sb.AppendLine(locationInfo);
					}
					string currentDateFact = BuildCurrentDateFactForPrompt();
					if (!string.IsNullOrWhiteSpace(currentDateFact))
					{
						sb.AppendLine(currentDateFact);
					}
					if (isSurroundingsContext)
					{
						bool hasPos;
						CampaignVec2 pos;
						try
						{
							if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
							{
								hasPos = LordEncounterBehavior.TryGetSavedMainPartyPosition(out pos);
							}
							else
							{
								pos = MobileParty.MainParty.Position;
								hasPos = pos.IsValid();
							}
						}
						catch
						{
							hasPos = false;
							pos = default(CampaignVec2);
						}
						if (hasPos)
						{
							string nearby = ShoutUtils.BuildNearbySettlementsDetailForPrompt(pos, targetHero);
							if (!string.IsNullOrWhiteSpace(nearby))
							{
								sb.AppendLine(nearby);
							}
						}
					}
					try
					{
						if (!LordEncounterBehavior.IsEncounterMeetingMissionActive)
						{
							string nativeInfo = (ShoutUtils.GetNativeSettlementInfoForPrompt() ?? "").Replace("\r", "").Trim();
							string heroNpcLine = (ShoutUtils.BuildCurrentSettlementHeroNpcLineForPrompt() ?? "").Replace("\r", "").Trim();
							if (!string.IsNullOrWhiteSpace(nativeInfo) && nativeInfo.Length > 700)
							{
								nativeInfo = nativeInfo.Substring(0, 700) + "…";
							}
							if (!string.IsNullOrWhiteSpace(nativeInfo) || !string.IsNullOrWhiteSpace(heroNpcLine))
							{
								sb.AppendLine(" ");
								sb.AppendLine("【当前定居点（原版到达介绍）】：");
								if (!string.IsNullOrWhiteSpace(nativeInfo))
								{
									sb.AppendLine(nativeInfo);
								}
								if (!string.IsNullOrWhiteSpace(heroNpcLine))
								{
									sb.AppendLine(heroNpcLine);
								}
							}
						}
					}
					catch
					{
					}
					string loreContext = (targetHero != null) ? AIConfigHandler.GetLoreContext(input, targetHero, npcLastUtterance) : AIConfigHandler.GetLoreContext(input, targetCharacter, null, npcLastUtterance);
					if (!string.IsNullOrEmpty(loreContext))
					{
						sb.AppendLine(loreContext);
					}
					string historyContext = BuildHistoryContext(targetHero, 0, input, npcLastUtterance);
					if (!string.IsNullOrEmpty(historyContext))
					{
						sb.AppendLine(historyContext);
					}
					string npcBehaviorSupplement = RewardSystemBehavior.Instance?.BuildNpcBehaviorSupplementForAI(targetHero, targetCharacter);
					if (!string.IsNullOrWhiteSpace(npcBehaviorSupplement))
					{
						sb.AppendLine("【AFEF NPC行为补充】");
						sb.AppendLine(npcBehaviorSupplement);
					}
					if (RewardSystemBehavior.Instance != null && targetHero != null)
					{
						if (isLoanContext)
						{
							string dueDateHint = RewardSystemBehavior.Instance.BuildDueDateReferenceForAI();
							if (!string.IsNullOrEmpty(dueDateHint))
							{
								sb.AppendLine(dueDateHint);
							}
							string debtHint = RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero);
							if (!string.IsNullOrEmpty(debtHint))
							{
								sb.AppendLine(debtHint);
							}
						}
						if (!isLoanContext && !isRewardContext)
						{
							string trustHint = RewardSystemBehavior.Instance.BuildTrustPromptForAI(targetHero);
							if (!string.IsNullOrEmpty(trustHint))
							{
								sb.AppendLine(trustHint);
							}
						}
					}
					if (RewardSystemBehavior.Instance != null && (isRewardContext || isLoanContext || useDuelContext) && targetHero == null && targetCharacter != null)
					{
						string settlementMerchantDebtHintForAI = RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter);
						if (!string.IsNullOrWhiteSpace(settlementMerchantDebtHintForAI))
						{
							sb.AppendLine(settlementMerchantDebtHintForAI);
						}
					}
					if (!string.IsNullOrEmpty(patiencePrompt))
					{
						sb.AppendLine(patiencePrompt);
					}
					bool includeDuelStakeContext = false;
					bool playerWonLastDuelForRule = false;
					if (targetHero != null && DuelBehavior.TryConsumeLastDuelResult(targetHero, out var playerWonLastDuel))
					{
						includeDuelStakeContext = true;
						playerWonLastDuelForRule = playerWonLastDuel;
						string text8 = BuildPlayerPublicDisplayNameForPrompt();
						if (string.IsNullOrWhiteSpace(text8))
						{
							text8 = "玩家";
						}
						if (playerWonLastDuel)
						{
							sb.AppendLine("【战斗结果】你刚刚在一场正式的决斗中输给了" + text8 + "。赌注应已在决斗结束瞬间自动结算；如果【AFEF玩家行为补充】中已经记录你已支付/仍欠款，请不要重复支付，只需确认或解释。");
							if (AIConfigHandler.RewardEnabled && !AIConfigHandler.DuelStakeEnabled)
							{
								useRewardContext = true;
							}
						}
						else
						{
							sb.AppendLine("【战斗结果】你刚刚在一场正式的决斗中打败了" + text8 + "。赌注应已在决斗结束瞬间自动结算；如果【AFEF玩家行为补充】中已经记录" + text8 + "欠你多少，请不要重复记账，只需确认并催促履行。");
							if (AIConfigHandler.RewardEnabled && !AIConfigHandler.DuelStakeEnabled)
							{
								useRewardContext = true;
							}
						}
					}
					if (targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
					{
						string text9 = BuildPlayerPublicDisplayNameForPrompt();
						if (string.IsNullOrWhiteSpace(text9))
						{
							text9 = "玩家";
						}
						if (_recentlyDefeatedByPlayer.Remove(targetHero.StringId))
						{
							sb.AppendLine("【原版战斗结果】你刚刚在一场战斗中被" + text9 + "击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。");
						}
						if (_recentlyReleasedPrisoners.Remove(targetHero.StringId))
						{
							sb.AppendLine("【释放通知】你之前被" + text9 + "俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。");
						}
					}
					AppendPlayerExtraFactLine(sb, extraFact);
					if (!string.IsNullOrWhiteSpace(guardrailClarifyHint))
					{
						sb.AppendLine(guardrailClarifyHint);
					}
					string triggeredRuleInstructions = BuildTriggeredRuleInstructions(input, targetHero, useDuelContext, isQualified, playerTier, useRewardContext, isLoanContext, isSurroundingsContext, hasAnyHero: targetHero != null, targetCharacter: targetCharacter, kingdomIdOverride: null, targetAgentIndex: targetAgentIndex, npcLastUtterance: npcLastUtterance, includeDuelStakeContext: includeDuelStakeContext, playerWonLastDuel: playerWonLastDuelForRule);
					bool excludeNpcShortReport = ShouldExcludeNpcShortReportFromWeeklyShortLayer(triggeredRuleInstructions, targetHero, targetCharacter);
					string weeklyShortReportsPromptBlock = BuildWeeklyShortReportsPromptBlock(targetHero, targetCharacter, null, excludeNpcShortReport);
					if (!string.IsNullOrWhiteSpace(weeklyShortReportsPromptBlock))
					{
						sb.AppendLine(weeklyShortReportsPromptBlock);
					}
					if (!string.IsNullOrWhiteSpace(triggeredRuleInstructions))
					{
						sb.AppendLine(triggeredRuleInstructions);
					}
					string triggeredWeeklyFullReportsPromptBlock = BuildTriggeredWeeklyFullReportsPromptBlock(triggeredRuleInstructions, targetHero, targetCharacter);
					if (!string.IsNullOrWhiteSpace(triggeredWeeklyFullReportsPromptBlock))
					{
						sb.AppendLine(triggeredWeeklyFullReportsPromptBlock);
					}
					bool includeTradePricing = useRewardContext || isLoanContext || useDuelContext;
					bool includeMarriageCandidates = targetHero != null && isMarriageHit;
					bool playerIdentityAlwaysOn = playerTier >= 2;
					bool includePlayerRuleGatedFields = playerIdentityAlwaysOn;
					string playerIdentityInfo = BuildPlayerIdentityInfoForPrompt(Hero.MainHero, includePlayerRuleGatedFields, includeTradePricing, includeMarriageCandidates, targetHero);
					if (!string.IsNullOrWhiteSpace(playerIdentityInfo))
					{
						sb.AppendLine(playerIdentityInfo);
					}
					string npcIdentityInfo = BuildNpcIdentityInfoForPrompt(targetHero, includeTradePricing, includeMarriageCandidates);
					if (!string.IsNullOrWhiteSpace(npcIdentityInfo))
					{
						sb.AppendLine(npcIdentityInfo);
					}
					Logger.Log("Logic", "[Context] SystemPrompt_Base=" + basePrompt.Replace("\n", "\\n"));
					string deltaLayerText = sb.ToString();
					string finalSystemPrompt = PromptComposer.Compose(fixedLayerText, deltaLayerText, "直接对话");
					string npcSystemTopIntro = BuildNpcSystemTopPromptIntro(targetHero, includeTradePricing);
					if (!string.IsNullOrWhiteSpace(npcSystemTopIntro))
					{
						finalSystemPrompt = npcSystemTopIntro + "\n" + finalSystemPrompt;
					}
					swPromptBuild.Stop();
					Logger.Log("Logic", $"[PromptLayer] mode=direct fixedChars={PromptComposer.CountChars(fixedLayerText)} deltaChars={PromptComposer.CountChars(deltaLayerText)} totalChars={PromptComposer.CountChars(finalSystemPrompt)} fixedCacheHit={fixedLayerCacheHit} personaReady={personaReadyForFixedCache} personaInFlight={personaInFlight} fixedCacheSize={PromptComposer.GetFixedLayerCacheSize()}");
					Logger.Obs("Prompt", "compose", new Dictionary<string, object>
					{
						["mode"] = "direct",
						["fixedChars"] = PromptComposer.CountChars(fixedLayerText),
						["deltaChars"] = PromptComposer.CountChars(deltaLayerText),
						["totalChars"] = PromptComposer.CountChars(finalSystemPrompt),
						["fixedCacheHit"] = fixedLayerCacheHit,
						["personaReady"] = personaReadyForFixedCache,
						["personaInFlight"] = personaInFlight,
						["buildMs"] = Math.Round(swPromptBuild.Elapsed.TotalMilliseconds, 2)
					});
					Logger.Metric("prompt.compose.direct", ok: true, swPromptBuild.Elapsed.TotalMilliseconds);
					string addressedInput = BuildPlayerAddressedInput(targetHero, input);
					Logger.Log("Logic", "[AI Request] NPC=" + npcName + " HeroId=" + (targetHero?.StringId ?? "null") + "\n[SYSTEM]=\n" + finalSystemPrompt + "\n[USER]=\n" + addressedInput + "\n");
					List<object> apiMessages = new List<object>
					{
						new
						{
							role = "system",
							content = finalSystemPrompt
						},
						new
						{
							role = "user",
							content = addressedInput
						}
					};
					string streamResult = null;
					string streamError = null;
					StringBuilder streamBuf = new StringBuilder();
					int lastDispLen = 0;
					string capturedNpcName = npcName;
					int chunkCount = 0;
					double firstChunkMs = -1.0;
					Stopwatch swApi = Stopwatch.StartNew();
					Logger.Log("Logic", "[HTTP-Stream] 直接对话流式请求 NPC=" + npcName);
					ConversationHelper.BeginStreaming();
					await ShoutNetwork.CallApiWithMessagesStream(apiMessages, 5000, delegate(string delta)
					{
						chunkCount++;
						streamBuf.Append(delta);
						int length = streamBuf.Length;
						if (chunkCount == 1)
						{
							firstChunkMs = swApi.Elapsed.TotalMilliseconds;
							Logger.Log("Logic", $"[HTTP-Stream] 首个 chunk 到达, HasActiveVM={ConversationHelper.HasActiveVM}, delta.len={delta.Length}");
							Logger.Obs("API", "first_chunk", new Dictionary<string, object>
							{
								["mode"] = "direct_stream",
								["firstChunkMs"] = Math.Round(firstChunkMs, 2),
								["deltaLen"] = delta.Length,
								["hasActiveVm"] = ConversationHelper.HasActiveVM
							});
						}
						if (length - lastDispLen >= 2)
						{
							string text = streamBuf.ToString();
							try
							{
								if (ConversationHelper.HasActiveVM)
								{
									ConversationHelper.UpdateDialogText(StripActionTags(text));
								}
								else
								{
									if (chunkCount <= 3)
									{
										Logger.Log("Logic", $"[HTTP-Stream] ⚠\ufe0f VM 未捕获！chunk #{chunkCount}, 使用 DisplayMessage 回退");
									}
									string text2 = ((text.Length > 150) ? ("..." + text.Substring(text.Length - 150)) : text);
									InformationManager.DisplayMessage(new InformationMessage("[" + capturedNpcName + "] " + text2, new Color(0.85f, 0.85f, 0.6f)));
								}
							}
							catch
							{
							}
							lastDispLen = length;
						}
					}, delegate(string fullText)
					{
						streamResult = fullText;
					}, delegate(string err)
					{
						streamError = err;
					});
					ConversationHelper.EndStreaming();
					swApi.Stop();
					if (!string.IsNullOrEmpty(streamError))
					{
						InformationManager.DisplayMessage(new InformationMessage("[API连接失败] " + streamError, new Color(1f, 0.3f, 0.3f)));
						throw new Exception(streamError);
					}
					string result = CleanAIResponse(streamResult ?? "");
					string rawResult = result;
					if (!patienceExhausted && targetHero != null)
					{
						VanillaIssueOfferBridge.ApplyIssueOfferTags(targetHero, ref result);
					}
					if (!patienceExhausted && AIConfigHandler.RewardEnabled && RewardSystemBehavior.Instance != null)
					{
						if (targetHero != null)
						{
							RewardSystemBehavior.Instance.ApplyRewardTags(targetHero, Hero.MainHero, ref result);
							RomanceSystemBehavior.Instance?.ApplyMarriageTags(targetHero, Hero.MainHero, ref result);
						}
						else if (targetCharacter != null)
						{
							RewardSystemBehavior.Instance.ApplyMerchantRewardTags(targetCharacter, Hero.MainHero, ref result);
						}
					}
					if (!patienceExhausted && string.IsNullOrEmpty(streamError))
					{
						try
						{
							if (TryApplyPartyTransferTagsForExternal(targetHero, targetCharacter, targetAgentIndex, ref result, out var generatedFacts, out var notifications))
							{
								Hero hero = targetHero ?? targetCharacter?.HeroObject;
								if (hero != null && generatedFacts != null)
								{
									foreach (string generatedFact in generatedFacts)
									{
										AppendExternalDialogueHistory(hero, null, null, generatedFact);
									}
								}
								if (notifications != null)
								{
									foreach (string notification in notifications)
									{
										if (!string.IsNullOrWhiteSpace(notification))
										{
											InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
										}
									}
								}
							}
						}
						catch
						{
						}
					}
					if (patienceExhausted && string.IsNullOrEmpty(streamError))
					{
						result = StripActionTags(result);
						if (string.IsNullOrWhiteSpace(result))
						{
							result = patienceExhaustedReply ?? string.Empty;
						}
					}
					if (!patienceExhausted && string.IsNullOrEmpty(streamError))
					{
						ApplyPatienceFromDirectResponseExternal(targetHero, ref result);
					}
					try
					{
						string badgeAfter = BuildDirectNamePatienceBadgeForExternal(targetHero);
						ConversationHelper.SetNameSuffix(badgeAfter);
					}
					catch
					{
					}
					Logger.Log("Logic", "[AI Response] NPC=" + npcName + " RAW=\n" + rawResult + "\nFINAL=\n" + result + "\n");
					int actionTagCount;
					try
					{
						actionTagCount = Regex.Matches(rawResult ?? "", "\\[ACTION:[^\\]]+\\]").Count;
					}
					catch
					{
						actionTagCount = 0;
					}
					Logger.Obs("API", "complete", new Dictionary<string, object>
					{
						["mode"] = "direct_stream",
						["ok"] = string.IsNullOrEmpty(streamError),
						["error"] = streamError ?? "",
						["latencyMs"] = Math.Round(swApi.Elapsed.TotalMilliseconds, 2),
						["firstChunkMs"] = ((firstChunkMs >= 0.0) ? Math.Round(firstChunkMs, 2) : (-1.0)),
						["chunkCount"] = chunkCount,
						["resultLen"] = (result ?? "").Length,
						["actionTagCount"] = actionTagCount
					});
					Logger.Metric("api.direct.stream", string.IsNullOrEmpty(streamError), swApi.Elapsed.TotalMilliseconds);
					sw.Stop();
					Logger.Obs("DirectChat", "round_done", new Dictionary<string, object>
					{
						["elapsedMs"] = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
						["responseLen"] = (result ?? "").Length
					});
					Logger.Metric("round.direct.total", ok: true, sw.Elapsed.TotalMilliseconds);
					_aiResponseText = result;
					AppendDialogueHistory(targetHero, input, result, extraFact);
					InformationManager.DisplayMessage(new InformationMessage(npcName + " 已准备好回应。", new Color(0f, 1f, 0f)));
					await Task.Delay(300);
					bool meetingTauntEscalated = false;
					bool sceneTauntEscalated = false;
					try
					{
						LordEncounterBehavior.TryProcessMeetingTauntAction(targetHero, ref _aiResponseText, out meetingTauntEscalated);
					}
					catch
					{
						meetingTauntEscalated = false;
					}
					try
					{
						SceneTauntBehavior.TryProcessSceneTauntAction(targetHero, targetCharacter, targetAgentIndex, ref _aiResponseText, out sceneTauntEscalated);
					}
					catch
					{
						sceneTauntEscalated = false;
					}
					bool hasDuelTag = _aiResponseText != null && _aiResponseText.Contains("[ACTION:DUEL]");
					if (hasDuelTag && !meetingTauntEscalated && !sceneTauntEscalated)
					{
						try
						{
							_suppressAutoClickUntilUtcTicks = DateTime.UtcNow.AddSeconds(30.0).Ticks;
						}
						catch
						{
						}
					}
					if (!meetingTauntEscalated && !sceneTauntEscalated)
					{
						CheckDuelAndAutoExit(ref _aiResponseText, isQualified);
					}
					_aiResponseText = StripActionTags(_aiResponseText);
					try
					{
						string ttsVoice = GetNpcVoiceId(targetHero);
						if (string.IsNullOrWhiteSpace(ttsVoice))
						{
							ttsVoice = VoiceMapper.ResolveVoiceId(targetHero);
						}
						TtsEngine.Instance.SpeakAsync(_aiResponseText, -1, -1f, -1, ttsVoice);
					}
					catch
					{
					}
					if (!hasDuelTag && !meetingTauntEscalated && !sceneTauntEscalated)
					{
						SimulateLeftClick();
					}
				}
				catch (Exception ex)
				{
					_aiResponseText = "（NPC 似乎走神了...）";
					Logger.Log("Logic", "[ERROR] StartAiConversation 异常: " + ex.ToString());
					Logger.Obs("DirectChat", "error", new Dictionary<string, object>
					{
						["message"] = ex.Message,
						["type"] = ex.GetType().Name
					});
					Logger.Metric("round.direct.total", ok: false, sw.Elapsed.TotalMilliseconds);
					await Task.Delay(300);
					SimulateLeftClick();
				}
			}
		});
	}

	private string GetHeroDescriptionSafe(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (hero.IsPrisoner)
			{
				string text = hero.PartyBelongedToAsPrisoner?.LeaderHero?.Name?.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					stringBuilder.Append(" 你目前是" + text + "的囚犯，被关押着，失去了自由。");
				}
				else
				{
					stringBuilder.Append(" 你目前是囚犯，被关押着，失去了自由。");
				}
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	private void CheckDuelAndAutoExit(ref string aiResponse, bool isQualified)
	{
		if (string.IsNullOrEmpty(aiResponse))
		{
			return;
		}
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				DuelBehavior.TryCacheDuelStakeFromText(oneToOneConversationHero, ref aiResponse);
			}
		}
		catch
		{
		}
		if (aiResponse.Contains("[ACTION:DUEL]"))
		{
			Hero oneToOneConversationHero2 = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero2 != null)
			{
				DuelBehavior.TryCacheDuelAfterLinesFromText(oneToOneConversationHero2, ref aiResponse);
				DuelBehavior.TryCacheDuelStakeFromText(oneToOneConversationHero2, ref aiResponse);
			}
			aiResponse = aiResponse.Replace("[ACTION:DUEL]", "").Trim();
			if (isQualified && oneToOneConversationHero2 != null)
			{
				try
				{
					DuelBehavior.SetNextDuelRiskWarningEnabled(_nextDuelRiskWarningByLiteral);
				}
				catch
				{
				}
				_nextDuelRiskWarningByLiteral = true;
				if (LordEncounterBehavior.IsEncounterMeetingMissionActive && Mission.Current != null && DuelBehavior.Instance != null)
				{
					DuelBehavior.Instance.StartDuelViaAI(oneToOneConversationHero2);
				}
				else
				{
					DuelBehavior.PrepareDuel(oneToOneConversationHero2, 10f);
				}
			}
			return;
		}
		Task.Run(async delegate
		{
			await Task.Delay(4000);
			try
			{
				if (DateTime.UtcNow.Ticks < _suppressAutoClickUntilUtcTicks)
				{
					return;
				}
			}
			catch
			{
			}
			SimulateLeftClick();
		});
	}

	private List<DialogueDay> LoadDialogueHistory(Hero hero)
	{
		if (hero == null)
		{
			return new List<DialogueDay>();
		}
		if (_dialogueHistory == null)
		{
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return new List<DialogueDay>();
		}
		if (_dialogueHistory.TryGetValue(stringId, out var value) && value != null)
		{
			return value;
		}
		return new List<DialogueDay>();
	}

	private void TryEnsureFirstMeetingNpcFactForConversation(Hero hero)
	{
		GetFirstMeetingNpcFactTextForPromptIfNeededInternal(hero, persistToHistory: true);
	}

	public static string GetFirstMeetingNpcFactTextForPromptIfNeeded(Hero hero, bool persistToHistory = true)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.GetFirstMeetingNpcFactTextForPromptIfNeededInternal(hero, persistToHistory) ?? "";
		}
		catch
		{
			return "";
		}
	}

	private string GetFirstMeetingNpcFactTextForPromptIfNeededInternal(Hero hero, bool persistToHistory)
	{
		if (hero == null || Hero.MainHero == null)
		{
			return "";
		}
		string text = BuildFirstMeetingNpcFactText();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		List<DialogueDay> list = LoadDialogueHistory(hero);
		if (HasDialogueHistoryLine(list, text) || HasMeaningfulDirectConversationHistory(list))
		{
			return "";
		}
		if (persistToHistory)
		{
			AppendDialogueHistory(hero, null, null, text);
		}
		return text;
	}

	private static string BuildFirstMeetingNpcFactText()
	{
		string text = BuildPlayerPublicDisplayNameForPrompt();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return "[AFEF NPC行为补充] 你第一次见到" + text;
	}

	private static bool HasDialogueHistoryLine(List<DialogueDay> records, string targetLine)
	{
		string text = (targetLine ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || records == null || records.Count == 0)
		{
			return false;
		}
		foreach (DialogueDay record in records)
		{
			if (record?.Lines == null)
			{
				continue;
			}
			foreach (string line in record.Lines)
			{
				if (string.Equals((line ?? "").Trim(), text, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool HasMeaningfulDirectConversationHistory(List<DialogueDay> records)
	{
		if (records == null || records.Count == 0)
		{
			return false;
		}
		foreach (DialogueDay record in records)
		{
			if (record?.Lines == null)
			{
				continue;
			}
			foreach (string line in record.Lines)
			{
				string text = (line ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && !IsActiveSceneSessionHistoryLine(text) && !IsSystemFactLine(text) && !IsLoreInjectionHistoryLine(text))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void SaveDialogueHistory(Hero hero, List<DialogueDay> records)
	{
		if (hero != null && records != null)
		{
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			string stringId = hero.StringId;
			if (!string.IsNullOrEmpty(stringId))
			{
				_dialogueHistory[stringId] = records;
			}
		}
	}

	public static void AppendExternalDialogueHistory(Hero hero, string playerText, string aiText, string extraFact)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistory(hero, playerText, aiText, extraFact);
		}
		catch
		{
		}
	}

	public static void AppendExternalSceneDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId);
		}
		catch
		{
		}
	}

	private void AppendDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId = -1)
	{
		if (hero == null || (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact)))
		{
			return;
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			int dayIndex = (int)CampaignTime.Now.ToDays;
			string gameDate = CampaignTime.Now.ToString();
			string text = hero.Name?.ToString() ?? "NPC";
			DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
			if (dialogueDay == null)
			{
				dialogueDay = new DialogueDay
				{
					GameDayIndex = dayIndex,
					GameDate = gameDate
				};
				list.Add(dialogueDay);
			}
			if (!string.IsNullOrWhiteSpace(playerText))
			{
				string text2 = BuildPlayerAddressedInput(hero, playerText);
				dialogueDay.Lines.Add((sceneSessionId >= 0) ? TagSceneSessionHistoryLine(text2, sceneSessionId) : text2);
			}
			if (!string.IsNullOrWhiteSpace(extraFact))
			{
				string text3 = extraFact.Trim();
				if (text3.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text3.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
				{
					dialogueDay.Lines.Add(text3);
				}
				else
				{
					dialogueDay.Lines.Add("[AFEF玩家行为补充] " + text3);
				}
			}
			if (!string.IsNullOrWhiteSpace(aiText))
			{
				string text4 = aiText.Trim();
				if (text4.StartsWith("[场景喊话]", StringComparison.Ordinal))
				{
					dialogueDay.Lines.Add((sceneSessionId >= 0) ? TagSceneSessionHistoryLine(text4, sceneSessionId) : text4);
				}
				else
				{
					string text5 = text + ": " + text4;
					dialogueDay.Lines.Add((sceneSessionId >= 0) ? TagSceneSessionHistoryLine(text5, sceneSessionId) : text5);
				}
			}
			List<(int, string, string)> list2 = new List<(int, string, string)>();
			foreach (DialogueDay item in list)
			{
				if (item.Lines == null)
				{
					continue;
				}
				foreach (string line in item.Lines)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						list2.Add((item.GameDayIndex, item.GameDate, line));
					}
				}
			}
			if (list2.Count > 260)
			{
				list2 = list2.Skip(list2.Count - 260).ToList();
			}
			List<DialogueDay> list3 = new List<DialogueDay>();
			foreach (var entry in list2)
			{
				DialogueDay dialogueDay2 = list3.FirstOrDefault((DialogueDay x) => x.GameDayIndex == entry.Item1);
				if (dialogueDay2 == null)
				{
					dialogueDay2 = new DialogueDay
					{
						GameDayIndex = entry.Item1,
						GameDate = entry.Item2
					};
					list3.Add(dialogueDay2);
				}
				dialogueDay2.Lines.Add(entry.Item3);
			}
			SaveDialogueHistory(hero, list3);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 追加失败: " + ex.Message);
		}
	}

	private string GetLatestNpcDialogueUtterance(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (hero == null)
		{
			return "";
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			if (list == null || list.Count == 0)
			{
				return "";
			}
			for (int i = list.Count - 1; i >= 0; i--)
			{
				DialogueDay dialogueDay = list[i];
				if (dialogueDay?.Lines == null || dialogueDay.Lines.Count <= 0)
				{
					continue;
				}
				for (int num = dialogueDay.Lines.Count - 1; num >= 0; num--)
				{
					string text = (dialogueDay.Lines[num] ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !IsActiveSceneSessionHistoryLine(text) && !IsLoreInjectionHistoryLine(text) && !IsSystemFactLine(text) && !IsPlayerTurnStartLine(text))
					{
						return StripSpeakerPrefixForRecall(text);
					}
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static string GetLatestSceneNpcDialogueUtteranceFallback(int targetAgentIndex)
	{
		if (targetAgentIndex < 0)
		{
			return "";
		}
		string text = ShoutBehavior.GetLatestSceneNpcUtteranceForExternal(targetAgentIndex);
		return string.IsNullOrWhiteSpace(text) ? "" : StripSpeakerPrefixForRecall(text);
	}

	public static string BuildHistoryContextForExternal(Hero hero, int maxLines = 20, string currentInput = null, string secondaryInput = null)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildHistoryContext(hero, maxLines, currentInput, secondaryInput);
		}
		catch
		{
			return "";
		}
	}

	public static string BuildRecentNpcFactContextForExternal(Hero hero, int maxLines = 4)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildRecentNpcFactContext(hero, maxLines);
		}
		catch
		{
			return "";
		}
	}

	public static void AppendExternalNpcFact(Hero hero, string factText)
	{
		try
		{
			if (hero == null)
			{
				return;
			}
			string text = (factText ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = (hero.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = "NPC";
			}
			AppendExternalDialogueHistory(hero, null, null, "[AFEF NPC行为补充] " + text2 + ": " + text);
		}
		catch
		{
		}
	}

	public static void AppendExternalPlayerFact(Hero hero, string factText)
	{
		try
		{
			if (hero == null)
			{
				return;
			}
			string text = (factText ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			AppendExternalDialogueHistory(hero, null, null, "[AFEF玩家行为补充] " + text);
		}
		catch
		{
		}
	}

	private static void AppendPlayerExtraFactLine(StringBuilder sb, string extraFact)
	{
		if (sb == null)
		{
			return;
		}
		string text = (extraFact ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
			{
				sb.AppendLine(text);
			}
			else
			{
				sb.AppendLine("[AFEF玩家行为补充] " + text);
			}
		}
	}

	private string BuildRecentNpcFactContext(Hero hero, int maxLines = 4)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			if (list == null || list.Count == 0)
			{
				return "";
			}
			List<string> list2 = new List<string>();
			foreach (DialogueDay item in list)
			{
				if (item?.Lines == null)
				{
					continue;
				}
				foreach (string line in item.Lines)
				{
					string text = (line ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
					{
						list2.Add(text);
					}
				}
			}
			if (list2.Count <= 0)
			{
				return "";
			}
			int num = Math.Max(1, maxLines);
			if (list2.Count > num)
			{
				list2 = list2.Skip(list2.Count - num).ToList();
			}
			return string.Join("\n", list2);
		}
		catch
		{
			return "";
		}
	}

	private string BuildGuardrailSemanticContext(Hero hero, string extraFact)
	{
		try
		{
			List<string> list = new List<string>();
			if (hero != null)
			{
				List<DialogueDay> list2 = LoadDialogueHistory(hero);
				if (list2 != null && list2.Count > 0)
				{
					List<string> list3 = new List<string>();
					foreach (DialogueDay item in list2)
					{
						if (item == null || item.Lines == null)
						{
							continue;
						}
						for (int i = 0; i < item.Lines.Count; i++)
						{
							string text = (item.Lines[i] ?? "").Trim();
							if (ShouldIncludeGuardrailSemanticContextLine(text))
							{
								list3.Add(text);
							}
						}
					}
					int num = Math.Min(6, list3.Count);
					for (int j = list3.Count - num; j < list3.Count; j++)
					{
						if (j >= 0 && j < list3.Count)
						{
							string text2 = list3[j];
							if (text2.Length > 120)
							{
								text2 = text2.Substring(0, 120);
							}
							list.Add(text2);
						}
					}
				}
			}
			string text3 = (extraFact ?? "").Trim();
			if (ShouldIncludeGuardrailSemanticContextLine(text3))
			{
				if (text3.Length > 180)
				{
					text3 = text3.Substring(0, 180);
				}
				string text5 = text3.Replace("\r", " ").Replace("\n", " ");
				if (text5.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text5.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
				{
					list.Add(text5);
				}
				else
				{
					list.Add("[AFEF玩家行为补充] " + text5);
				}
			}
			string text6 = VanillaIssueOfferBridge.BuildRagSemanticStateForExternal(hero);
			if (!string.IsNullOrWhiteSpace(text6))
			{
				list.Add(text6);
			}
			if (list.Count <= 0)
			{
				return "";
			}
			string text4 = string.Join("\n", list);
			if (text4.Length > 700)
			{
				text4 = text4.Substring(text4.Length - 700);
			}
			return text4;
		}
		catch
		{
			return "";
		}
	}

	private static bool ShouldIncludeGuardrailSemanticContextLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (IsActiveSceneSessionHistoryLine(text) || IsLoreInjectionHistoryLine(text) || IsPlayerTurnStartLine(text))
		{
			return false;
		}
		if (text.IndexOf("参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		if (text.IndexOf("【以下是关于（", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		if (text.IndexOf("【触发相关话题/背景】", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		return true;
	}

	private static string ResolveTargetKingdomIdForRules(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		try
		{
			string text = (kingdomIdOverride ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.ToLowerInvariant();
			}
			string text2 = (targetHero?.Clan?.Kingdom?.StringId ?? targetHero?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.ToLowerInvariant();
			}
			Hero heroObject = targetCharacter?.HeroObject;
			string text3 = (heroObject?.Clan?.Kingdom?.StringId ?? heroObject?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3.ToLowerInvariant();
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveRuleTargetKey(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			string text = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return "hero:" + text;
			}
			if (targetAgentIndex >= 0)
			{
				return "agent:" + targetAgentIndex;
			}
			string text2 = (targetCharacter?.StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string text3 = (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text3) ? ("troop:" + text2) : ("troop:" + text2 + "@" + text3);
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildRuleTargetKeyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		return ResolveRuleTargetKey(targetHero, targetCharacter, targetAgentIndex);
	}

	private string BuildExtraRuleInstructions(string input, string npcLastUtterance, Hero targetHero, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1)
	{
		string text = "";
		string text2 = "";
		int num = AIConfigHandler.GuardrailRuleReturnCap;
		string targetKingdomId = ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
		AIConfigHandler.SetGuardrailRuntimeTargetKingdom(targetKingdomId);
		string text3 = targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "";
		AIConfigHandler.SetGuardrailRuntimeTargetHero(text3);
		AIConfigHandler.SetGuardrailRuntimeTargetCharacter(targetCharacter?.StringId ?? "");
		AIConfigHandler.SetGuardrailRuntimeTargetTroop(targetCharacter?.StringId ?? "");
		AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank((targetHero == null && targetCharacter != null) ? (targetCharacter.IsSoldier ? "soldier" : "commoner") : "");
		AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(targetAgentIndex);
		try
		{
			text = AIConfigHandler.BuildMatchedExtraRuleInstructions(input, npcLastUtterance, AIConfigHandler.GuardrailRuleReturnCap, hasAnyHero);
			if (!string.IsNullOrWhiteSpace(text) && text.IndexOf("【附加规则:party_transfer】", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string partyTransferRuntimeInstructionForExternal = BuildPartyTransferRuntimeInstructionForExternal(targetHero, targetCharacter, targetAgentIndex);
				if (!string.IsNullOrWhiteSpace(partyTransferRuntimeInstructionForExternal))
				{
					text = ReplaceSingleRuleBlockBody(text, "party_transfer", partyTransferRuntimeInstructionForExternal);
				}
			}
			// Always keep this rule present for the lords-hall gate guard, regardless of semantic hits.
			text2 = (AIConfigHandler.BuildRuntimeLordsHallAccessInstructionForExternal() ?? "").Trim();
		}
		finally
		{
			AIConfigHandler.SetGuardrailRuntimeTargetKingdom("");
			AIConfigHandler.SetGuardrailRuntimeTargetHero("");
			AIConfigHandler.SetGuardrailRuntimeTargetCharacter("");
			AIConfigHandler.SetGuardrailRuntimeTargetTroop("");
			AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank("");
			AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(-1);
		}
		if (!string.IsNullOrWhiteSpace(text2) && (string.IsNullOrWhiteSpace(text) || text.IndexOf("【附加规则:lords_hall_access】", StringComparison.OrdinalIgnoreCase) < 0) && CountInjectedRuleBlocks(text) < num)
		{
			string text4 = "【附加规则:lords_hall_access】" + Environment.NewLine + text2;
			text = string.IsNullOrWhiteSpace(text) ? text4 : (text.TrimEnd() + Environment.NewLine + text4);
		}
		if (IsSceneFollowingAgentForRules(targetAgentIndex))
		{
			text = ReplaceSceneMechanismRuleForFollowing(text);
		}
		return PrependExtraRuleDisclaimer(text);
	}

	private static string ReplaceSingleRuleBlockBody(string text, string ruleId, string newBody)
	{
		string text2 = (text ?? "").Trim();
		string text3 = (ruleId ?? "").Trim();
		string text4 = (newBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3) || string.IsNullOrWhiteSpace(text4))
		{
			return text2;
		}
		string value = "【附加规则:" + text3 + "】";
		int num = text2.IndexOf(value, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return text2;
		}
		int num2 = text2.IndexOf("【附加规则:", num + value.Length, StringComparison.Ordinal);
		string text5 = text2.Substring(0, num).TrimEnd();
		string text6 = value;
		string text7 = ((num2 >= 0) ? text2.Substring(num2).TrimStart() : "");
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(text5))
		{
			stringBuilder.AppendLine(text5);
		}
		stringBuilder.AppendLine(text6);
		stringBuilder.Append(text4.Trim());
		if (!string.IsNullOrWhiteSpace(text7))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append(text7);
		}
		return stringBuilder.ToString().Trim();
	}

	private static bool IsSceneFollowingAgentForRules(int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex < 0 || Mission.Current == null || PlayerEncounter.LocationEncounter == null || TaleWorlds.CampaignSystem.Settlements.Locations.LocationComplex.Current == null)
			{
				return false;
			}
			Agent agent = Mission.Current.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (agent == null || agent == Agent.Main)
			{
				return false;
			}
			TaleWorlds.CampaignSystem.Settlements.Locations.LocationCharacter locationCharacter = TaleWorlds.CampaignSystem.Settlements.Locations.LocationComplex.Current.FindCharacter(agent);
			AccompanyingCharacter accompanyingCharacter = ((locationCharacter != null) ? PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter) : null);
			return accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart;
		}
		catch
		{
			return false;
		}
	}

	private static string ReplaceSceneMechanismRuleForFollowing(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		const string replacement = "【附加规则:scene_mechanism_actions】" + "\r\n" + "【当前正跟随玩家】若此人明确让你停止跟随且你同意，可在句末输出[STP]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]，多人可写[ASS:id1,id2]；若此人改让你带路去找【可带路目标】，也可输出[GUI:id]。";
		int num = text.IndexOf("【附加规则:scene_mechanism_actions】", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return text;
		}
		int num2 = text.IndexOf("【附加规则:", num + 1, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return text.Substring(0, num).TrimEnd() + Environment.NewLine + replacement;
		}
		return text.Substring(0, num).TrimEnd() + Environment.NewLine + replacement + Environment.NewLine + text.Substring(num2).TrimStart();
	}

	private static int CountInjectedRuleBlocks(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		while (num2 >= 0 && num2 < text.Length)
		{
			num2 = text.IndexOf("【附加规则:", num2, StringComparison.Ordinal);
			if (num2 < 0)
			{
				break;
			}
			num++;
			num2 += 6;
		}
		return num;
	}

	private static string PrependExtraRuleDisclaimer(string text)
	{
		const string disclaimer = "【说明】你不必提到附加规则内的内容，除非有人问起。";
		if (string.IsNullOrWhiteSpace(text) || CountInjectedRuleBlocks(text) <= 0)
		{
			return text;
		}
		if (text.IndexOf(disclaimer, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		return disclaimer + Environment.NewLine + text.TrimStart();
	}

	private static void AppendRuleBlock(StringBuilder sb, string ruleId, string body)
	{
		if (sb != null)
		{
			string text = (ruleId ?? "").Trim();
			string value = (body ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
			{
				sb.AppendLine("【附加规则:" + text + "】");
				sb.AppendLine(value);
			}
		}
	}

	private string BuildTriggeredRuleInstructions(string input, Hero targetHero, bool useDuelContext, bool isQualified, int playerTier, bool useRewardContext, bool isLoanContext, bool isSurroundingsContext, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, string npcLastUtterance = null, bool includeDuelStakeContext = false, bool playerWonLastDuel = false)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (useDuelContext)
			{
				if (!hasAnyHero)
				{
					string duelNonHeroInstruction = AIConfigHandler.DuelNonHeroInstruction;
					if (!string.IsNullOrWhiteSpace(duelNonHeroInstruction))
					{
						AppendRuleBlock(stringBuilder, "duel", duelNonHeroInstruction);
					}
				}
				else
				{
					string value = AIConfigHandler.DuelInstruction;
					if (string.IsNullOrWhiteSpace(value))
					{
						value = "受到侮辱可发起决斗。";
					}
					if (isQualified)
					{
						StringBuilder stringBuilder2 = new StringBuilder();
						stringBuilder2.AppendLine(value);
						stringBuilder2.AppendLine("如果你决定同意决斗并使用 [ACTION:DUEL]，请在同一条回复末尾额外输出两条隐藏标签（不会展示给玩家，用于战后喊话）：");
						stringBuilder2.AppendLine("[ACTION:DUEL_LINE_WIN:你获胜后要喊的一句话]");
						stringBuilder2.AppendLine("[ACTION:DUEL_LINE_LOSE:你战败后要喊的一句话]");
						stringBuilder2.Append("两句话都必须是纯口语一句话；不要写解释；不要包含括号/星号；不要出现 ] 字符。");
						AppendRuleBlock(stringBuilder, "duel", stringBuilder2.ToString());
					}
					else
					{
						string text10 = BuildPlayerPublicDisplayNameForPrompt();
						if (string.IsNullOrWhiteSpace(text10))
						{
							text10 = "玩家";
						}
						AppendRuleBlock(stringBuilder, "duel", $"{text10}触发了决斗相关话题，但等级({playerTier})过低。请拒绝决斗并羞辱其不自量力。严禁使用决斗标签。");
					}
				}
			}
			if (AIConfigHandler.RewardEnabled && useRewardContext)
			{
				string text = "";
				if (!hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null)
				{
					string settlementMerchantRewardInstruction = RewardSystemBehavior.Instance.BuildSettlementMerchantRewardInstruction(targetCharacter);
					if (!string.IsNullOrWhiteSpace(settlementMerchantRewardInstruction))
					{
						string rewardInstruction = AIConfigHandler.BuildRuntimeRewardInstructionForExternal(null, targetCharacter);
						text = (string.IsNullOrWhiteSpace(rewardInstruction) ? settlementMerchantRewardInstruction : (rewardInstruction.Trim() + "\n" + settlementMerchantRewardInstruction.Trim()));
					}
				}
				if (string.IsNullOrWhiteSpace(text))
				{
					text = (hasAnyHero ? AIConfigHandler.BuildRuntimeRewardInstructionForExternal(targetHero, targetCharacter) : AIConfigHandler.RewardNonHeroInstruction);
				}
				if (string.IsNullOrWhiteSpace(text))
				{
					text = AIConfigHandler.RewardInstruction;
				}
				AppendRuleBlock(stringBuilder, "reward", text);
				if (AIConfigHandler.DuelStakeEnabled && includeDuelStakeContext)
				{
					string body = (playerWonLastDuel ? AIConfigHandler.DuelStakePlayerWinInstruction : AIConfigHandler.DuelStakeNpcWinInstruction);
					AppendRuleBlock(stringBuilder, "duel_stake", body);
				}
			}
			if (AIConfigHandler.LoanEnabled && isLoanContext)
			{
				bool flag11 = !hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var _);
				string text2 = ((hasAnyHero || flag11) ? AIConfigHandler.BuildRuntimeLoanInstructionForExternal(targetHero, targetCharacter) : AIConfigHandler.LoanNonHeroInstruction);
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = AIConfigHandler.LoanInstruction;
				}
				AppendRuleBlock(stringBuilder, "loan", text2);
			}
			if (AIConfigHandler.SurroundingsEnabled && isSurroundingsContext)
			{
				AppendRuleBlock(stringBuilder, "surroundings", AIConfigHandler.SurroundingsInstruction);
			}
			string text3 = BuildExtraRuleInstructions(input, npcLastUtterance, targetHero, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex);
			if (!string.IsNullOrWhiteSpace(text3) && text3.IndexOf("【附加规则:party_transfer】", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				bool flag12 = stringBuilder.ToString().IndexOf("【附加规则:reward】", StringComparison.OrdinalIgnoreCase) >= 0;
				bool flag13 = stringBuilder.ToString().IndexOf("【附加规则:loan】", StringComparison.OrdinalIgnoreCase) >= 0;
				if (AIConfigHandler.RewardEnabled && !flag12)
				{
					string rewardText = "";
					if (!hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null)
					{
						string settlementMerchantRewardInstruction = RewardSystemBehavior.Instance.BuildSettlementMerchantRewardInstruction(targetCharacter);
						if (!string.IsNullOrWhiteSpace(settlementMerchantRewardInstruction))
						{
							string rewardInstruction = AIConfigHandler.BuildRuntimeRewardInstructionForExternal(null, targetCharacter);
							rewardText = (string.IsNullOrWhiteSpace(rewardInstruction) ? settlementMerchantRewardInstruction : (rewardInstruction.Trim() + "\n" + settlementMerchantRewardInstruction.Trim()));
						}
					}
					if (string.IsNullOrWhiteSpace(rewardText))
					{
						rewardText = (hasAnyHero ? AIConfigHandler.BuildRuntimeRewardInstructionForExternal(targetHero, targetCharacter) : AIConfigHandler.RewardNonHeroInstruction);
					}
					if (string.IsNullOrWhiteSpace(rewardText))
					{
						rewardText = AIConfigHandler.RewardInstruction;
					}
					if (!string.IsNullOrWhiteSpace(rewardText))
					{
						AppendRuleBlock(stringBuilder, "reward", rewardText);
					}
				}
				if (AIConfigHandler.LoanEnabled && !flag13)
				{
					bool flag11 = !hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var _);
					string text5 = ((hasAnyHero || flag11) ? AIConfigHandler.BuildRuntimeLoanInstructionForExternal(targetHero, targetCharacter) : AIConfigHandler.LoanNonHeroInstruction);
					if (string.IsNullOrWhiteSpace(text5))
					{
						text5 = AIConfigHandler.LoanInstruction;
					}
					if (!string.IsNullOrWhiteSpace(text5))
					{
						AppendRuleBlock(stringBuilder, "loan", text5);
					}
				}
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				stringBuilder.AppendLine(text3.Trim());
			}
			string text4 = SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(targetHero, targetCharacter, targetAgentIndex);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				stringBuilder.AppendLine(text4.Trim());
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	public static List<Hero> GetDevEditableHeroListForExternal()
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new List<Hero>();
			}
			List<Hero> list = myBehavior.BuildDevEditableHeroList();
			return list ?? new List<Hero>();
		}
		catch
		{
			return new List<Hero>();
		}
	}

	public static void GetNpcPersonaForExternal(Hero hero, out string personality, out string background)
	{
		personality = "";
		background = "";
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior != null && hero != null)
			{
				myBehavior.GetNpcPersonaStrings(hero, out personality, out background);
			}
		}
		catch
		{
		}
	}

	public static bool TryGetNpcPersonaGenerationStatusForExternal(Hero hero, out bool needsGeneration, out bool inFlight)
	{
		needsGeneration = false;
		inFlight = false;
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				return false;
			}
			needsGeneration = myBehavior.NeedsNpcPersonaGeneration(hero);
			inFlight = myBehavior.IsNpcPersonaGenerationInFlight(hero);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static string BuildNpcPersonaGenerationHintForExternal(Hero hero)
	{
		string text = hero?.Name?.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "该NPC";
		}
		return "正在生成" + text + "的个性与背景，请稍等......";
	}

	public static async Task EnsureNpcPersonaGeneratedForExternalAsync(Hero hero)
	{
		try
		{
			MyBehavior inst = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (inst != null && hero != null)
			{
				await inst.EnsureNpcPersonaGeneratedAsync(hero);
			}
		}
		catch
		{
		}
	}

	public static string BuildCurrentDateFactForExternal()
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildCurrentDateFactForPrompt() ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string GetClanTierReputationLabelForExternal(int tier)
	{
		return GetClanTierReputationLabel(tier);
	}

	public static string BuildAgeBracketLabelForExternal(float age)
	{
		return BuildAgeBracketLabel(age);
	}

	public static string BuildHeroEquipmentSummaryForExternal(Hero hero)
	{
		return BuildHeroEquipmentSummaryForPrompt(hero);
	}

	public static string BuildHeroIdentityTitleForExternal(Hero hero)
	{
		return BuildHeroIdentityTitleForPrompt(hero);
	}

	public static string BuildNpcMajorActionsRuntimeInstructionForExternal(Hero hero)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.BuildNpcMajorActionsRuntimeInstruction(hero) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildNpcRecentActionsRuntimeInstructionForExternal(Hero hero)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.BuildNpcRecentActionsRuntimeInstruction(hero) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildNpcCurrentActionFactForExternal(Hero hero)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.BuildNpcCurrentActionFact(hero) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static string BuildNpcActionsRuntimeConstraintHintForExternal(Hero hero, bool recentOnly)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.BuildNpcActionsRuntimeConstraintHint(hero, recentOnly) ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static ShoutPromptContext BuildShoutPromptContextForExternal(Hero targetHero, string input, string extraFact, string cultureIdOverride = null, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, bool suppressDynamicRuleAndLore = false, bool usePrefetchedLoreContext = false, string prefetchedLoreContext = null)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new ShoutPromptContext
				{
					Extras = "",
					UseDuelContext = false,
					UseRewardContext = false,
					IsLoanContext = false,
					IsQualified = true
				};
			}
			return myBehavior.BuildShoutPromptContextForExternalInternal(targetHero, input, extraFact, cultureIdOverride, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext);
		}
		catch
		{
			return new ShoutPromptContext
			{
				Extras = "",
				UseDuelContext = false,
				UseRewardContext = false,
				IsLoanContext = false,
				IsQualified = true
			};
		}
	}

	// Primary runtime chat path: scene shout / non-native conversation UI.
	private ShoutPromptContext BuildShoutPromptContextForExternalInternal(Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, bool suppressDynamicRuleAndLore = false, bool usePrefetchedLoreContext = false, string prefetchedLoreContext = null)
	{
		ShoutPromptContext shoutPromptContext = new ShoutPromptContext
		{
			Extras = "",
			UseDuelContext = false,
			UseRewardContext = false,
			IsLoanContext = false,
			IsQualified = true
		};
		if (string.IsNullOrWhiteSpace(input))
		{
			return shoutPromptContext;
		}
		if (targetHero != null)
		{
			TryEnsureFirstMeetingNpcFactForConversation(targetHero);
		}
		AIConfigHandler.SetGuardrailSemanticContext(suppressDynamicRuleAndLore ? "" : BuildGuardrailSemanticContext(targetHero, extraFact));
		string text = ((!string.IsNullOrEmpty(cultureIdOverride)) ? cultureIdOverride : (targetHero?.Culture?.StringId ?? "neutral"));
		int num = _cachedPlayerClanTier;
		if (num <= 0)
		{
			try
			{
				num = Clan.PlayerClan?.Tier ?? 0;
			}
			catch
			{
			}
			if (num <= 0)
			{
				try
				{
					num = (Hero.MainHero?.Clan?.Tier).GetValueOrDefault();
				}
				catch
				{
				}
			}
		}
		int num2 = DuelSettings.GetSettings()?.MinimumClanTier ?? 0;
		bool isQualified = num >= num2;
		string npcLastUtterance = GetLatestNpcDialogueUtterance(targetHero, targetCharacter, targetAgentIndex);
		bool flagMerchant = targetHero == null && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var _);
		List<string> duelTriggerKeywords = AIConfigHandler.DuelTriggerKeywords;
		bool flag = false;
		string matchedKeyword = "";
		float score = 0f;
		if (!suppressDynamicRuleAndLore)
		{
			flag = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "duel", AIConfigHandler.DuelInstruction, duelTriggerKeywords, out matchedKeyword, out score);
		}
		bool liveDuelSemanticHit = flag;
		bool flag2 = targetHero != null && flag;
		List<string> rewardTriggerKeywords = AIConfigHandler.RewardTriggerKeywords;
		bool flag3 = false;
		string matchedKeyword2 = "";
		float score2 = 0f;
		if (!suppressDynamicRuleAndLore && AIConfigHandler.RewardEnabled)
		{
			flag3 = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "reward", AIConfigHandler.RewardInstruction, rewardTriggerKeywords, out matchedKeyword2, out score2);
		}
		bool liveRewardSemanticHit = flag3;
		List<string> loanTriggerKeywords = AIConfigHandler.LoanTriggerKeywords;
		bool flag4 = false;
		string matchedKeyword3 = "";
		float score3 = 0f;
		if (!suppressDynamicRuleAndLore && AIConfigHandler.LoanEnabled)
		{
			flag4 = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "loan", AIConfigHandler.LoanInstruction, loanTriggerKeywords, out matchedKeyword3, out score3);
		}
		bool liveLoanSemanticHit = flag4;
		List<string> surroundingsTriggerKeywords = AIConfigHandler.SurroundingsTriggerKeywords;
		bool flag5 = false;
		string matchedKeyword4 = "";
		float score4 = 0f;
		if (!suppressDynamicRuleAndLore && AIConfigHandler.SurroundingsEnabled)
		{
			flag5 = AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "surroundings", AIConfigHandler.SurroundingsInstruction, surroundingsTriggerKeywords, out matchedKeyword4, out score4);
		}
		string guardrailRuleInstruction = AIConfigHandler.GetGuardrailRuleInstruction("kingdom_service");
		List<string> guardrailRuleKeywords = AIConfigHandler.GetGuardrailRuleKeywords("kingdom_service");
		string matchedKeyword5 = "";
		float score5 = 0f;
		bool flag6 = !suppressDynamicRuleAndLore && AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "kingdom_service", guardrailRuleInstruction, guardrailRuleKeywords, out matchedKeyword5, out score5);
		string guardrailMarriageInstruction = AIConfigHandler.GetGuardrailRuleInstruction("marriage");
		List<string> guardrailMarriageKeywords = AIConfigHandler.GetGuardrailRuleKeywords("marriage");
		string matchedKeyword6 = "";
		float score6 = 0f;
		bool marriageHit = !suppressDynamicRuleAndLore && AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "marriage", guardrailMarriageInstruction, guardrailMarriageKeywords, out matchedKeyword6, out score6);
		string guardrailPartyTransferInstruction = AIConfigHandler.GetGuardrailRuleInstruction("party_transfer");
		List<string> guardrailPartyTransferKeywords = AIConfigHandler.GetGuardrailRuleKeywords("party_transfer");
		string matchedKeyword7 = "";
		float score7 = 0f;
		bool partyTransferHit = !suppressDynamicRuleAndLore && AIConfigHandler.IsGuardrailSemanticHit(input, npcLastUtterance, "party_transfer", guardrailPartyTransferInstruction, guardrailPartyTransferKeywords, out matchedKeyword7, out score7);
		if (!suppressDynamicRuleAndLore && TryConsumeRuleStickyCarry(targetHero, targetCharacter, input, out var carryDuel, out var carryReward, out var carryLoan))
		{
			if (!flag && carryDuel)
			{
				flag = true;
				matchedKeyword = "sticky";
				score = Math.Max(score, 0.18f);
			}
			if (!flag3 && carryReward)
			{
				flag3 = true;
				matchedKeyword2 = "sticky";
				score2 = Math.Max(score2, 0.18f);
			}
			if (!flag4 && carryLoan)
			{
				flag4 = true;
				matchedKeyword3 = "sticky";
				score3 = Math.Max(score3, 0.18f);
			}
		}
		if (!suppressDynamicRuleAndLore)
		{
			UpdateRuleStickyCarryFromHits(targetHero, targetCharacter, liveDuelSemanticHit, liveRewardSemanticHit, liveLoanSemanticHit);
		}
		flag2 = targetHero != null && flag;
		bool flag7 = flag3 || flagMerchant;
		bool flag8 = flag4 || flagMerchant;
		if (partyTransferHit)
		{
			flag7 = flag7 || AIConfigHandler.RewardEnabled;
			flag8 = flag8 || AIConfigHandler.LoanEnabled;
		}
		string value = "";
		if (!suppressDynamicRuleAndLore && !flag2 && !flag7 && !flag8 && !flag5)
		{
			value = AIConfigHandler.BuildGuardrailClarificationHint(input, flag, score, flag3, score2, flag4, score3, flag5, score4);
		}
		string text2 = (string.IsNullOrWhiteSpace(matchedKeyword) ? "" : $"{matchedKeyword}@{score:0.00}");
		string text3 = (string.IsNullOrWhiteSpace(matchedKeyword2) ? "" : $"{matchedKeyword2}@{score2:0.00}");
		string text4 = (string.IsNullOrWhiteSpace(matchedKeyword3) ? "" : $"{matchedKeyword3}@{score3:0.00}");
		string text5 = (string.IsNullOrWhiteSpace(matchedKeyword4) ? "" : $"{matchedKeyword4}@{score4:0.00}");
		string text6 = (string.IsNullOrWhiteSpace(matchedKeyword5) ? "" : $"{matchedKeyword5}@{score5:0.00}");
		string text8 = (string.IsNullOrWhiteSpace(matchedKeyword6) ? "" : $"{matchedKeyword6}@{score6:0.00}");
		string text9 = (string.IsNullOrWhiteSpace(matchedKeyword7) ? "" : $"{matchedKeyword7}@{score7:0.00}");
		string text7 = targetHero?.Name?.ToString() ?? "某人";
		Logger.Log("Logic", $"[SemanticTrigger-Shout] DuelHit={flag} [{text2}] RewardHit={flag3} [{text3}] LoanHit={flag4} [{text4}] PartyTransferHit={partyTransferHit} [{text9}] SurroundingsHit={flag5} [{text5}] KingdomServiceHit={flag6} [{text6}] MarriageHit={marriageHit} [{text8}] NpcRecall={(string.IsNullOrWhiteSpace(npcLastUtterance) ? "off" : "on")} Input='{input}' NPC='{text7}'");
		StringBuilder stringBuilder = new StringBuilder();
		string loreContext = "";
		string loreCtxSource = "none";
		if (!suppressDynamicRuleAndLore && usePrefetchedLoreContext && !string.IsNullOrWhiteSpace(prefetchedLoreContext))
		{
			loreContext = prefetchedLoreContext ?? "";
			loreCtxSource = "prefetched";
		}
		else if (!suppressDynamicRuleAndLore && targetHero != null)
		{
			loreContext = AIConfigHandler.GetLoreContext(input, targetHero, npcLastUtterance);
			loreCtxSource = ((usePrefetchedLoreContext && string.IsNullOrWhiteSpace(prefetchedLoreContext)) ? "prefetch_empty_fallback_hero" : "hero");
		}
		else if (!suppressDynamicRuleAndLore && targetCharacter != null)
		{
			loreContext = AIConfigHandler.GetLoreContext(input, targetCharacter, kingdomIdOverride, npcLastUtterance);
			loreCtxSource = ((usePrefetchedLoreContext && string.IsNullOrWhiteSpace(prefetchedLoreContext)) ? "prefetch_empty_fallback_character" : "character");
		}
		try
		{
			Logger.Log("LoreMatch", $"shout_prompt_lore_ctx source={loreCtxSource} heroId={(targetHero?.StringId ?? "null")} charId={(targetCharacter?.StringId ?? "null")} kingdomIdOverride={(kingdomIdOverride ?? "")}");
		}
		catch
		{
		}
		if (RewardSystemBehavior.Instance != null && targetHero != null)
		{
			if (flag8)
			{
				string value4 = RewardSystemBehavior.Instance.BuildDueDateReferenceForAI();
				if (!string.IsNullOrEmpty(value4))
				{
					stringBuilder.AppendLine(value4);
				}
				string value5 = RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero);
				if (!string.IsNullOrEmpty(value5))
				{
					stringBuilder.AppendLine(value5);
				}
			}
			if (!flag8 && !flag7)
			{
				string value6 = RewardSystemBehavior.Instance.BuildTrustPromptForAI(targetHero);
				if (!string.IsNullOrEmpty(value6))
				{
					stringBuilder.AppendLine(value6);
				}
			}
		}
		if (RewardSystemBehavior.Instance != null && flag7 && targetHero == null && targetCharacter != null)
		{
			string value6a = RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter);
			if (!string.IsNullOrWhiteSpace(value6a))
			{
				stringBuilder.AppendLine(value6a);
			}
		}
		string value6c = RewardSystemBehavior.Instance?.BuildNpcBehaviorSupplementForAI(targetHero, targetCharacter);
		if (!string.IsNullOrWhiteSpace(value6c))
		{
			stringBuilder.AppendLine("【AFEF NPC行为补充】");
			stringBuilder.AppendLine(value6c);
		}
		bool includeDuelStakeContext = false;
		bool playerWonLastDuelForRule = false;
		if (targetHero != null && DuelBehavior.TryConsumeLastDuelResult(targetHero, out var playerWon))
		{
			includeDuelStakeContext = true;
			playerWonLastDuelForRule = playerWon;
			string playerDisplayName = BuildPlayerPublicDisplayNameForPrompt();
			if (string.IsNullOrWhiteSpace(playerDisplayName))
			{
				playerDisplayName = "玩家";
			}
			if (playerWon)
			{
				stringBuilder.AppendLine("【战斗结果】你刚刚在一场正式的决斗中输给了" + playerDisplayName + "。请在态度和言语中体现这一点，并认真考虑履行你在决斗前约定的赌注或补偿。");
				if (AIConfigHandler.RewardEnabled)
				{
					flag7 = true;
				}
			}
			else
			{
				stringBuilder.AppendLine("【战斗结果】你刚刚在一场正式的决斗中打败了" + playerDisplayName + "。你可以据此调整对" + playerDisplayName + "的态度，或提醒" + playerDisplayName + "履行之前约定的赌注。");
				if (AIConfigHandler.RewardEnabled)
				{
					flag7 = true;
				}
			}
		}
		if (targetHero != null && !string.IsNullOrEmpty(targetHero.StringId))
		{
			string playerDisplayName2 = BuildPlayerPublicDisplayNameForPrompt();
			if (string.IsNullOrWhiteSpace(playerDisplayName2))
			{
				playerDisplayName2 = "玩家";
			}
			if (_recentlyDefeatedByPlayer.Contains(targetHero.StringId))
			{
				stringBuilder.AppendLine("【原版战斗结果】你刚刚在一场战斗中被" + playerDisplayName2 + "击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。");
			}
			if (_recentlyReleasedPrisoners.Contains(targetHero.StringId))
			{
				stringBuilder.AppendLine("【释放通知】你之前被" + playerDisplayName2 + "俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。");
			}
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine(value);
		}
		if (flag5)
		{
			bool flag9 = false;
			CampaignVec2 pos;
			try
			{
				if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
				{
					flag9 = LordEncounterBehavior.TryGetSavedMainPartyPosition(out pos);
				}
				else
				{
					pos = MobileParty.MainParty.Position;
					flag9 = pos.IsValid();
				}
			}
			catch
			{
				flag9 = false;
				pos = default(CampaignVec2);
			}
			if (flag9)
			{
				string value7 = ShoutUtils.BuildNearbySettlementsDetailForPrompt(pos, targetHero);
				if (!string.IsNullOrWhiteSpace(value7))
				{
					stringBuilder.AppendLine(value7);
				}
			}
		}
		string value8 = suppressDynamicRuleAndLore ? "" : BuildTriggeredRuleInstructions(input, targetHero, flag2, isQualified, num, flag7, flag8, flag5, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, npcLastUtterance, includeDuelStakeContext, playerWonLastDuelForRule);
		bool excludeNpcShortReport2 = ShouldExcludeNpcShortReportFromWeeklyShortLayer(value8, targetHero, targetCharacter, kingdomIdOverride);
		string value8a = BuildWeeklyShortReportsPromptBlock(targetHero, targetCharacter, kingdomIdOverride, excludeNpcShortReport2);
		if (!string.IsNullOrWhiteSpace(value8a))
		{
			stringBuilder.AppendLine(value8a);
		}
		if (!string.IsNullOrWhiteSpace(value8))
		{
			stringBuilder.AppendLine(value8);
		}
		string value8b = BuildTriggeredWeeklyFullReportsPromptBlock(value8, targetHero, targetCharacter, kingdomIdOverride);
		if (!string.IsNullOrWhiteSpace(value8b))
		{
			stringBuilder.AppendLine(value8b);
		}
		if (!string.IsNullOrEmpty(loreContext))
		{
			stringBuilder.AppendLine(loreContext);
		}
		bool includeTradePricing = flag7 || flag8 || flag2;
		bool includeMarriageCandidates = targetHero != null && marriageHit;
		bool flag10 = num >= 2;
		bool includeRuleGatedFields = flag10;
		shoutPromptContext.Extras = stringBuilder.ToString();
		shoutPromptContext.UseDuelContext = flag2;
		shoutPromptContext.UseRewardContext = flag7;
		shoutPromptContext.IsLoanContext = flag8;
		shoutPromptContext.IsQualified = isQualified;
		return shoutPromptContext;
	}

	public static void AppendExternalLoreHistory(Hero hero, string loreText)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendLoreToHistory(hero, loreText);
		}
		catch
		{
		}
	}

	private void AppendLoreToHistory(Hero hero, string loreText)
	{
		if (hero == null)
		{
			return;
		}
		loreText = (loreText ?? "").Trim();
		if (string.IsNullOrEmpty(loreText))
		{
			return;
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			int dayIndex = (int)CampaignTime.Now.ToDays;
			string gameDate = CampaignTime.Now.ToString();
			DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d != null && d.GameDayIndex == dayIndex);
			if (dialogueDay == null)
			{
				dialogueDay = new DialogueDay
				{
					GameDayIndex = dayIndex,
					GameDate = gameDate
				};
				list.Add(dialogueDay);
			}
			if (dialogueDay.Lines == null)
			{
				dialogueDay.Lines = new List<string>();
			}
			string text = loreText.Replace("\r", "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			bool flag = text.IndexOf("【触发相关话题/背景】", StringComparison.OrdinalIgnoreCase) >= 0;
			bool flag2 = text.IndexOf("【玩家触发了（", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("【以下是关于（", StringComparison.OrdinalIgnoreCase) >= 0;
			if (!flag && !flag2)
			{
				string text2 = "";
				try
				{
					text2 = (hero?.Name?.ToString() ?? "").Trim();
				}
				catch
				{
					text2 = "";
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "该NPC";
				}
				string text4 = BuildPlayerPublicDisplayNameForPrompt();
				if (string.IsNullOrWhiteSpace(text4))
				{
					text4 = "玩家";
				}
				text = "【以下是关于（相关）的背景知识，" + text2 + "可酌情参考，但不要假设" + text4 + "提起过此话题】\n" + text;
			}
			string text3 = text;
			if (dialogueDay.Lines.Count > 0)
			{
				string a = dialogueDay.Lines[dialogueDay.Lines.Count - 1] ?? "";
				if (string.Equals(a, text3, StringComparison.Ordinal))
				{
					return;
				}
			}
			dialogueDay.Lines.Add(text3);
			int num = 0;
			foreach (DialogueDay item in list)
			{
				num += (item?.Lines?.Count).GetValueOrDefault();
			}
			while (num > 260)
			{
				DialogueDay dialogueDay2 = list.FirstOrDefault((DialogueDay d) => d != null && d.Lines != null && d.Lines.Count > 0);
				if (dialogueDay2 == null)
				{
					break;
				}
				dialogueDay2.Lines.RemoveAt(0);
				num--;
				if (dialogueDay2.Lines.Count == 0 && dialogueDay2.GameDayIndex != dayIndex)
				{
					list.Remove(dialogueDay2);
				}
			}
			SaveDialogueHistory(hero, list);
		}
		catch
		{
		}
	}

	private static bool IsPlayerTurnStartLine(string line)
	{
		return TryStripPlayerSpeechPrefix(line, out var _);
	}

	private static bool IsSystemFactLine(string line)
	{
		string text = (line ?? "").TrimStart();
		return text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal);
	}

	private static bool IsLoreInjectionHistoryLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.StartsWith("【玩家触发了（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【以下是关于（", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("【触发相关话题/背景】", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.IndexOf("参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) >= 0)
		{
			return true;
		}
		if (text.IndexOf("应当知晓以下信息", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

	private static bool ContainsStructuredSignal(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		if (text2.Contains("债务id") || text2.Contains("欠款") || text2.Contains("还款") || text2.Contains("赊账") || text2.Contains("借款") || text2.Contains("第纳尔") || text2.Contains("价格") || text2.Contains("交易") || text2.Contains("决斗") || text2.Contains("赌注") || text2.Contains("[action:") || text2.Contains("guideprice=") || text2.Contains("|") || text2.Contains("id:"))
		{
			return true;
		}
		foreach (char c in text)
		{
			if (c >= '0' && c <= '9')
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsRecallIntentQuery(string query)
	{
		string text = (query ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		return text2.Contains("以前") || text2.Contains("之前") || text2.Contains("上次") || text2.Contains("上回") || text2.Contains("那次") || text2.Contains("当时") || text2.Contains("还记得") || text2.Contains("你记得") || text2.Contains("你忘了") || text2.Contains("曾经") || text2.Contains("过去") || text2.Contains("前些天") || text2.Contains("前阵子") || text2.Contains("old") || text2.Contains("before") || text2.Contains("last time") || text2.Contains("remember");
	}

	private static bool ContainsLiteralKeywordHit(string input, List<string> keywords, out string matchedKeyword)
	{
		matchedKeyword = "";
		string text = (input ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text) || keywords == null || keywords.Count <= 0)
		{
			return false;
		}
		for (int i = 0; i < keywords.Count; i++)
		{
			string text2 = (keywords[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string value = text2.ToLowerInvariant();
				if (text.Contains(value))
				{
					matchedKeyword = text2;
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsTermChar(char c)
	{
		return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c >= '\u4E00' && c <= '\u9FFF');
	}

	private static List<string> ExtractQueryTerms(string query)
	{
		List<string> terms = new List<string>();
		string text = (query ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return terms;
		}
		StringBuilder cur = new StringBuilder();
		Action action = delegate
		{
			if (cur.Length < 2)
			{
				cur.Clear();
			}
			else
			{
				string item = cur.ToString();
				if (!terms.Contains(item))
				{
					terms.Add(item);
				}
				cur.Clear();
			}
		};
		foreach (char c in text)
		{
			if (IsTermChar(c))
			{
				cur.Append(c);
			}
			else
			{
				action();
			}
		}
		action();
		if (terms.Count > 24)
		{
			terms = terms.Take(24).ToList();
		}
		return terms;
	}

	private static double ComputeLexicalOverlapScore(string text, List<string> terms)
	{
		if (string.IsNullOrWhiteSpace(text) || terms == null || terms.Count <= 0)
		{
			return 0.0;
		}
		int num = 0;
		for (int i = 0; i < terms.Count; i++)
		{
			string value = terms[i];
			if (!string.IsNullOrWhiteSpace(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				num++;
			}
		}
		if (num <= 0)
		{
			return 0.0;
		}
		double num2 = (double)num / (double)Math.Max(1, terms.Count);
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		if (num2 > 1.0)
		{
			num2 = 1.0;
		}
		return num2;
	}

	private static string StripSpeakerPrefixForRecall(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		TryStripSceneSessionHistoryMarker(text, out text, out var _);
		if (text.StartsWith("- ", StringComparison.Ordinal))
		{
			text = text.Substring(2).TrimStart();
		}
		if (text.StartsWith("—— ", StringComparison.Ordinal))
		{
			return "";
		}
		if (text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
		{
			return text.Substring("[AFEF玩家行为补充]".Length).Trim();
		}
		if (text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			return text.Substring("[AFEF NPC行为补充]".Length).Trim();
		}
		if (TryStripPlayerSpeechPrefix(text, out var stripped))
		{
			return stripped;
		}
		text = ShoutUtils.StripNamePrefixedLineSafely(text, 20);
		return text;
	}

	private static string TrimRecallSnippet(string text, int maxChars = 80)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (text2.Length <= maxChars)
		{
			return text2;
		}
		return text2.Substring(0, maxChars).TrimEnd('，', '。', '；', '、', ',', ';', '.') + "…";
	}

	private static List<string> BuildRecallToneLines(List<string> rawArchiveLines, int maxItems = 12)
	{
		List<string> list = new List<string>();
		if (rawArchiveLines == null || rawArchiveLines.Count == 0 || maxItems <= 0)
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string[] array = new string[3] { "你隐约记得那次闲聊提到：", "你回想起之前似乎说过：", "你依稀记得当时聊到过：" };
		string playerDisplayName = BuildPlayerPublicDisplayNameForPrompt();
		if (string.IsNullOrWhiteSpace(playerDisplayName))
		{
			playerDisplayName = "玩家";
		}
		string[] array2 = new string[3] { "你记得" + playerDisplayName + "当时提到过：", "你回想起" + playerDisplayName + "曾说过：", "你依稀记得" + playerDisplayName + "提过：" };
		string[] array3 = new string[3] { "你记得当时谈到过一件具体事项：", "你回想起之前提过一件具体事项：", "你依稀记得还讨论过一件具体事项：" };
		string text = "";
		int num = 0;
		for (int i = 0; i < rawArchiveLines.Count; i++)
		{
			string text2 = (rawArchiveLines[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("—— ", StringComparison.Ordinal) && text2.EndsWith(" ——", StringComparison.Ordinal))
			{
				text = text2;
				continue;
			}
			if (num >= maxItems)
			{
				break;
			}
			string text3 = StripSpeakerPrefixForRecall(text2);
			text3 = TrimRecallSnippet(text3);
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				string text4 = (IsSystemFactLine(text2) ? ("你清楚记得一条已发生的事实：" + text3) : (IsPlayerTurnStartLine(text2) ? (array2[num % array2.Length] + text3) : ((!ContainsStructuredSignal(text2)) ? (array[num % array.Length] + text3) : (array3[num % array3.Length] + text3))));
				string text5 = (string.IsNullOrWhiteSpace(text) ? "—— 日期未知 ——" : text);
				if (list.Count <= 0 || !string.Equals(list[list.Count - 1], text5, StringComparison.Ordinal))
				{
					list.Add(text5);
				}
				list.Add("- " + text4);
				num++;
			}
		}
		return list;
	}

	private static List<string> BuildRenderedHistoryLines(List<HistoryLineEntry> entries, string targetDisplayName = null, bool addressToYou = true)
	{
		List<string> list = new List<string>();
		if (entries == null || entries.Count == 0)
		{
			return list;
		}
		int num = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			HistoryLineEntry historyLineEntry = entries[i];
			if (historyLineEntry != null && !string.IsNullOrWhiteSpace(historyLineEntry.Line))
			{
				if (historyLineEntry.Day != 0 && historyLineEntry.Day != num)
				{
					num = historyLineEntry.Day;
					string text = ((!string.IsNullOrWhiteSpace(historyLineEntry.Date)) ? historyLineEntry.Date : $"第 {historyLineEntry.Day} 日");
					list.Add("—— " + text + " ——");
				}
				list.Add(NormalizePlayerHistoryLineForPrompt(historyLineEntry.Line, targetDisplayName, addressToYou));
			}
		}
		return list;
	}

	private static List<string> TakeTailByCharBudget(List<string> lines, int budget)
	{
		List<string> list = new List<string>();
		if (lines == null || lines.Count == 0 || budget <= 0)
		{
			return list;
		}
		int num = 0;
		for (int num2 = lines.Count - 1; num2 >= 0; num2--)
		{
			string text = lines[num2] ?? "";
			if (text.Length > 0)
			{
				int num3 = text.Length + 1;
				if (num + num3 > budget && list.Count > 0)
				{
					break;
				}
				list.Add(text);
				num += num3;
			}
		}
		list.Reverse();
		return list;
	}

	private static string BuildHistoryQueryText(List<HistoryLineEntry> recent)
	{
		if (recent == null || recent.Count == 0)
		{
			return "";
		}
		int num = Math.Min(8, recent.Count);
		IEnumerable<string> values = from x in recent.Skip(recent.Count - num)
			select x?.Line ?? "" into x
			where !string.IsNullOrWhiteSpace(x)
			select x;
		return string.Join("\n", values);
	}

	private static List<string> SplitHistoryRecallIntents(string query, int maxParts = IntentQueryOptimizer.MaxCombinedIntentCount)
	{
		List<string> list = new List<string>();
		string text = (query ?? "").Replace("\r", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		list.Add(text);
		try
		{
			string[] array = Regex.Split(text, "[，。！？；：,.!?;:\\n]+");
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = (array[i] ?? "").Trim();
				if (text2.Length >= 2 && !list.Contains(text2))
				{
					list.Add(text2);
				}
			}
		}
		catch
		{
		}
		list = IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
		return list;
	}

	private static List<WeightedRecallQueryInput> BuildHistoryRecallQueryInputs(List<HistoryLineEntry> recent, string currentInput, string secondaryInput)
	{
		List<WeightedRecallQueryInput> list = new List<WeightedRecallQueryInput>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = (string.IsNullOrWhiteSpace(currentInput) ? BuildHistoryQueryText(recent) : currentInput.Trim());
		appendInputs(SplitHistoryRecallIntents(text, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker);
		string text2 = (secondaryInput ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			appendInputs(SplitHistoryRecallIntents(text2, IntentQueryOptimizer.MaxIntentCountPerSpeaker), IntentQueryOptimizer.MaxIntentCountPerSpeaker);
		}
		return list;

		void appendInputs(List<string> intents, int perSourceLimit)
		{
			if (intents == null || intents.Count <= 0 || perSourceLimit <= 0)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < intents.Count; i++)
			{
				string text3 = (intents[i] ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
				{
					WeightedRecallQueryInput weightedRecallQueryInput = new WeightedRecallQueryInput
					{
						Text = text3,
						Weight = 1f
					};
					weightedRecallQueryInput.Terms = ExtractQueryTerms(text3);
					list.Add(weightedRecallQueryInput);
					num++;
					if (num >= perSourceLimit || list.Count >= IntentQueryOptimizer.MaxCombinedIntentCount)
					{
						break;
					}
				}
			}
		}
	}

	private static bool IsCurrentInputPlayerLine(string line, string currentInput)
	{
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(currentInput))
		{
			return false;
		}
		if (!TryStripPlayerSpeechPrefix(line, out var text))
		{
			return false;
		}
		string b = currentInput.Trim();
		return string.Equals(text, b, StringComparison.Ordinal);
	}

	private List<ArchiveHit> FindRelevantArchiveHits(List<HistoryLineEntry> older, List<WeightedRecallQueryInput> queryInputs, int returnCap, out bool onnxUsed, out string matchMode, out int rerankPerIntent, out int recallPerIntent)
	{
		onnxUsed = false;
		matchMode = "none";
		rerankPerIntent = 0;
		recallPerIntent = 0;
		List<ArchiveHit> list = new List<ArchiveHit>();
		try
		{
			List<WeightedRecallQueryInput> list2 = (queryInputs ?? new List<WeightedRecallQueryInput>()).Where((WeightedRecallQueryInput x) => x != null && !string.IsNullOrWhiteSpace(x.Text) && x.Weight > 0f).ToList();
			if (older == null || older.Count == 0 || returnCap <= 0 || list2.Count == 0)
			{
				return list;
			}
			List<HistoryLineEntry> list3 = older.Where((HistoryLineEntry x) => x != null && !string.IsNullOrWhiteSpace(x.Line)).ToList();
			if (list3.Count <= 0)
			{
				return list;
			}
			int num = ClampHistoryReturnCap(returnCap);
			int num2 = Math.Max(1, list2.Count);
			int historyRerankBudget = GetHistoryRerankBudget(num);
			rerankPerIntent = GetHistoryPerIntentRerank(historyRerankBudget, num2);
			recallPerIntent = GetHistoryPerIntentRecall(rerankPerIntent);
			bool flag = false;
			try
			{
				flag = OnnxCrossEncoderReranker.Instance.IsAvailable;
			}
			catch
			{
				flag = false;
			}
			matchMode = (flag ? ((num2 > 1) ? "rerank_multi" : "rerank") : ((num2 > 1) ? "semantic_multi" : "semantic"));
			Dictionary<int, HistoryRecallAggregate> dictionary = new Dictionary<int, HistoryRecallAggregate>();
			for (int i = 0; i < list2.Count; i++)
			{
				WeightedRecallQueryInput weightedRecallQueryInput = list2[i];
				List<RecallLineScore> list4 = FindHistoryCandidateScores(list3, weightedRecallQueryInput, recallPerIntent, out var onnxUsed2);
				if (onnxUsed2)
				{
					onnxUsed = true;
				}
				if (list4 == null || list4.Count <= 0)
				{
					continue;
				}
				List<RecallLineScore> list5 = RerankHistoryCandidateScores(weightedRecallQueryInput.Text, list4, rerankPerIntent, out var _, weightedRecallQueryInput.Weight);
				if (list5 == null || list5.Count <= 0)
				{
					continue;
				}
				for (int j = 0; j < list5.Count; j++)
				{
					RecallLineScore recallLineScore = list5[j];
					int num3 = recallLineScore?.Entry?.Index ?? int.MinValue;
					if (num3 == int.MinValue)
					{
						continue;
					}
					double num4 = recallLineScore.RerankScore;
					if (!dictionary.TryGetValue(num3, out var value))
					{
						value = new HistoryRecallAggregate
						{
							Hit = new ArchiveHit
							{
								Entry = recallLineScore.Entry,
								Score = num4,
								BaseScore = recallLineScore.BaseScore,
								RerankScore = recallLineScore.RerankScore
							},
							ScoreSum = num4,
							HitCount = 1,
							BestRank = j + 1,
							BestScore = recallLineScore.RerankScore
						};
						dictionary[num3] = value;
						continue;
					}
					value.ScoreSum += num4;
					value.HitCount++;
					if (j + 1 < value.BestRank)
					{
						value.BestRank = j + 1;
					}
					if (recallLineScore.RerankScore >= value.BestScore)
					{
						value.BestScore = recallLineScore.RerankScore;
						value.Hit.Entry = recallLineScore.Entry;
						value.Hit.BaseScore = recallLineScore.BaseScore;
						value.Hit.RerankScore = recallLineScore.RerankScore;
					}
					dictionary[num3] = value;
				}
			}
			if (dictionary.Count <= 0)
			{
				return list;
			}
			List<ArchiveHit> list6 = (from x in dictionary.Values
				let finalScore = Math.Min(1.0, x.ScoreSum / (double)Math.Max(1, x.HitCount) + (double)(x.HitCount - 1) * 0.08)
				orderby finalScore descending, x.BestRank, (x.Hit?.Entry != null) ? x.Hit.Entry.Index : (-1) descending
				select new ArchiveHit
				{
					Entry = x.Hit.Entry,
					Score = finalScore,
					BaseScore = x.Hit.BaseScore,
					RerankScore = x.Hit.RerankScore
				}).ToList();
			HashSet<int> hashSet = new HashSet<int>();
			for (int k = 0; k < list6.Count; k++)
			{
				ArchiveHit archiveHit = list6[k];
				int num5 = archiveHit?.Entry?.Index ?? int.MinValue;
				if (num5 != int.MinValue && !hashSet.Contains(num5))
				{
					list.Add(archiveHit);
					hashSet.Add(num5 - 1);
					hashSet.Add(num5);
					hashSet.Add(num5 + 1);
					if (list.Count >= num)
					{
						break;
					}
				}
			}
			try
			{
				Logger.Log("DialogueHistory", $"candidate_pool mode={matchMode} returnCap={num} rerankBudget={historyRerankBudget} rerankPerIntent={rerankPerIntent} recallPerIntent={recallPerIntent} intents={num2} got={list.Count}");
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

	private string BuildHistoryContext(Hero hero, int maxLines = 0, string currentInput = null, string secondaryInput = null)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			TryEnsureFirstMeetingNpcFactForConversation(hero);
			int num = ((maxLines <= 0) ? GetRecentDialogueTurnsFromSettings() : ClampRecentDialogueTurns(maxLines));
			List<DialogueDay> list = LoadDialogueHistory(hero);
			if (list == null || list.Count == 0)
			{
				return "";
			}
			int num2 = (int)CampaignTime.Now.ToDays;
			List<HistoryLineEntry> list2 = new List<HistoryLineEntry>();
			int num3 = 0;
			foreach (DialogueDay item in list)
			{
				if (item == null || item.Lines == null || (item.GameDayIndex != 0 && item.GameDayIndex > num2))
				{
					continue;
				}
				foreach (string line in item.Lines)
				{
					if (!string.IsNullOrWhiteSpace(line) && !IsActiveSceneSessionHistoryLine(line) && !IsLoreInjectionHistoryLine(line))
					{
						list2.Add(new HistoryLineEntry
						{
							Day = item.GameDayIndex,
							Date = item.GameDate,
							Line = line.Trim(),
							Index = num3++
						});
					}
				}
			}
			if (list2.Count == 0)
			{
				return "";
			}
			if (!string.IsNullOrWhiteSpace(currentInput))
			{
				int num4 = list2.Count - 1;
				if (num4 >= 0 && IsCurrentInputPlayerLine(list2[num4].Line, currentInput))
				{
					list2.RemoveAt(num4);
				}
			}
			if (list2.Count == 0)
			{
				return "";
			}
			int count = 0;
			int num5 = 0;
			bool flag = false;
			for (int num6 = list2.Count - 1; num6 >= 0; num6--)
			{
				if (IsPlayerTurnStartLine(list2[num6].Line))
				{
					num5++;
					if (num5 >= num)
					{
						count = num6;
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				count = Math.Max(0, list2.Count - num * 3);
			}
			List<HistoryLineEntry> list3 = list2.Skip(count).ToList();
			List<HistoryLineEntry> list4 = list2.Take(count).ToList();
			List<WeightedRecallQueryInput> list5 = BuildHistoryRecallQueryInputs(list3, currentInput, secondaryInput);
			int historyReturnCapFromSettings = GetHistoryReturnCapFromSettings();
			bool onnxUsed = false;
			string historyMatchMode = "none";
			int historyRerankPerIntent = 0;
			int historyRecallPerIntent = 0;
			List<ArchiveHit> list6 = FindRelevantArchiveHits(list4, list5, historyReturnCapFromSettings, out onnxUsed, out historyMatchMode, out historyRerankPerIntent, out historyRecallPerIntent);
			int num7 = list5.Sum((WeightedRecallQueryInput x) => x.Text?.Length ?? 0);
			string text = (hero?.Name?.ToString() ?? "").Trim();
			List<string> list13 = BuildRenderedHistoryLines(list3, text, addressToYou: true);
			List<string> list8 = new List<string>();
			if (list6 != null && list6.Count > 0)
			{
				List<HistoryLineEntry> list9 = new List<HistoryLineEntry>();
				HashSet<int> hashSet = new HashSet<int>();
				foreach (ArchiveHit item2 in list6)
				{
					HistoryLineEntry historyLineEntry = item2?.Entry;
					if (historyLineEntry != null && hashSet.Add(historyLineEntry.Index))
					{
						list9.Add(historyLineEntry);
					}
				}
				list9 = (from e in list9
					orderby e.Day, e.Index
					select e).ToList();
				Dictionary<int, HistoryLineEntry> dictionary = new Dictionary<int, HistoryLineEntry>();
				for (int num9 = 0; num9 < list4.Count; num9++)
				{
					HistoryLineEntry historyLineEntry3 = list4[num9];
					if (historyLineEntry3 != null)
					{
						dictionary[historyLineEntry3.Index] = historyLineEntry3;
					}
				}
				List<HistoryLineEntry> list10 = new List<HistoryLineEntry>();
				HashSet<int> hashSet2 = new HashSet<int>();
				for (int num10 = 0; num10 < list9.Count; num10++)
				{
					HistoryLineEntry historyLineEntry4 = list9[num10];
					if (historyLineEntry4 == null)
					{
						continue;
					}
					int[] array = new int[3]
					{
						historyLineEntry4.Index - 1,
						historyLineEntry4.Index,
						historyLineEntry4.Index + 1
					};
					foreach (int key in array)
					{
						if (dictionary.TryGetValue(key, out var value) && value != null && !string.IsNullOrWhiteSpace(value.Line) && hashSet2.Add(value.Index))
						{
							list10.Add(value);
						}
					}
				}
				list10 = (from e in list10
					orderby e.Day, e.Index
					select e).ToList();
				List<string> list11 = new List<string>();
				int num11 = int.MinValue;
				string a = null;
				foreach (HistoryLineEntry item3 in list10)
				{
					string text2 = ((!string.IsNullOrWhiteSpace(item3.Date)) ? item3.Date.Trim() : ((item3.Day != 0) ? ("第 " + item3.Day + " 日") : ""));
					if (!string.IsNullOrWhiteSpace(text2) && (item3.Day != num11 || !string.Equals(a, text2, StringComparison.Ordinal)))
					{
						list11.Add("—— " + text2 + " ——");
						num11 = item3.Day;
						a = text2;
					}
					string text3 = NormalizePlayerHistoryLineForPrompt(item3.Line ?? "", text, addressToYou: true);
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list11.Add(text3);
					}
				}
				list8 = BuildRecallToneLines(list11);
				list8 = TakeTailByCharBudget(list8, 900);
			}
			StringBuilder stringBuilder = new StringBuilder(4096);
			stringBuilder.AppendLine(" ");
			if (list13.Count > 0)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "该NPC";
				}
				stringBuilder.AppendLine($"【{BuildPlayerPublicDisplayNameForPrompt()}与{text}（NPC名称的对话与互动）的近期对话】");
				foreach (string item4 in list13)
				{
					stringBuilder.AppendLine(item4);
				}
			}
			if (list8.Count > 0)
			{
				stringBuilder.AppendLine("【长期记忆摘要】");
				foreach (string item6 in list8)
				{
					stringBuilder.AppendLine(item6);
				}
			}
			string text4 = stringBuilder.ToString().TrimEnd();
			Logger.Log("DialogueHistory", string.Format("context hero={0} totalLines={1} recentLines={2} olderLines={3} archiveHits={4} onnxUsed={5} returnCap={6} matchMode={7} rerankPerIntent={8} recallPerIntent={9} queryCount={10} queryLen={11} npcRecall={12} chars={13}", hero.StringId ?? "", list2.Count, list3.Count, list4.Count, list8?.Count ?? 0, onnxUsed, historyReturnCapFromSettings, historyMatchMode, historyRerankPerIntent, historyRecallPerIntent, list5.Count, num7, string.IsNullOrWhiteSpace(secondaryInput) ? "off" : "on", text4.Length));
			Logger.Obs("History", "build_context", new Dictionary<string, object>
			{
				["heroId"] = hero.StringId ?? "",
				["totalLines"] = list2.Count,
				["recentLines"] = list3.Count,
				["olderLines"] = list4.Count,
				["archiveHits"] = list8?.Count ?? 0,
				["onnxUsed"] = onnxUsed,
				["returnCap"] = historyReturnCapFromSettings,
				["matchMode"] = historyMatchMode,
				["rerankPerIntent"] = historyRerankPerIntent,
				["recallPerIntent"] = historyRecallPerIntent,
				["queryCount"] = list5.Count,
				["queryLen"] = num7,
				["npcRecall"] = !string.IsNullOrWhiteSpace(secondaryInput),
				["chars"] = text4.Length
			});
			Logger.Metric("history.build_context");
			return text4;
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[错误] 构建上下文失败: " + ex.Message);
			Logger.Obs("History", "build_context_error", new Dictionary<string, object>
			{
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("history.build_context", ok: false);
			return "";
		}
	}

	private string GetCurrentLocationInfoSafe()
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
			{
				string encounterMeetingLocationInfoOverride = LordEncounterBehavior.EncounterMeetingLocationInfoOverride;
				encounterMeetingLocationInfoOverride = (encounterMeetingLocationInfoOverride ?? "").Trim();
				if (!string.IsNullOrEmpty(encounterMeetingLocationInfoOverride))
				{
					return encounterMeetingLocationInfoOverride;
				}
			}
			string text = "";
			try
			{
				text = (ShoutUtils.GetCurrentSceneDescription() ?? "").Replace("\r", "").Replace("\n", " ").Trim();
			}
			catch
			{
				text = "";
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (text.StartsWith("位于 ", StringComparison.Ordinal) || text.StartsWith("靠近 ", StringComparison.Ordinal))
				{
					return "你" + text + "。";
				}
				return "你位于 " + text.TrimEnd('。', '.', ' ') + "。";
			}
			if (Settlement.CurrentSettlement != null)
			{
				return $"你位于 {Settlement.CurrentSettlement.Name}。";
			}
			if (MobileParty.MainParty != null)
			{
				return "你身处野外。";
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	private void SimulateLeftClick()
	{
		mouse_event(2, 0, 0, 0, 0);
		Thread.Sleep(30);
		mouse_event(4, 0, 0, 0, 0);
	}

	private async Task<ApiCallResult> CallUniversalApiDetailed(string sys, string user, bool logToEventLogs = false, string eventLogSource = "EventWeeklyReport")
	{
		ApiCallResult apiCallResult = new ApiCallResult();
		Action<string> apiLog = delegate(string message)
		{
			if (logToEventLogs)
			{
				Logger.LogEvent(eventLogSource, message);
			}
			else
			{
				Logger.Log("Logic", message);
			}
		};
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null || string.IsNullOrEmpty(settings.ApiKey))
			{
				apiCallResult.ErrorMessage = "请检查 MCM 设置。";
				return apiCallResult;
			}
			string effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl);
			var body = new
			{
				model = settings.ModelName,
				messages = new[]
				{
					new
					{
						role = "system",
						content = sys
					},
					new
					{
						role = "user",
						content = user
					}
				},
				max_tokens = 5000,
				stream = true,
				temperature = 0.8
			};
			string jsonBody = JsonConvert.SerializeObject(body);
			StringBuilder httpLog = new StringBuilder();
			httpLog.AppendLine("[HTTP] 请求发送到:");
			httpLog.AppendLine("  Url: " + effectiveApiUrl);
			if (!string.Equals(effectiveApiUrl, settings.ApiUrl, StringComparison.Ordinal))
			{
				httpLog.AppendLine("  Note: 已自动补全 /v1/chat/completions 尾缀");
			}
			httpLog.AppendLine("  Model: " + settings.ModelName);
			httpLog.AppendLine("  MaxTokens: 5000, Stream: true, Temperature: 0.8");
			httpLog.AppendLine("  SystemPrompt:");
			httpLog.AppendLine(sys);
			httpLog.AppendLine("  UserInput:");
			httpLog.AppendLine(user);
			apiLog(httpLog.ToString());
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
			try
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
				request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await DuelSettings.GlobalClient.SendAsync(request, (HttpCompletionOption)1);
				try
				{
					string statusLine = (int)response.StatusCode + " " + response.ReasonPhrase;
					apiLog("[HTTP] 响应状态: " + statusLine);
					if (!response.IsSuccessStatusCode)
					{
						string text = await response.Content.ReadAsStringAsync();
						apiCallResult.StatusCode = (int)response.StatusCode;
						apiCallResult.ResponseBody = text ?? "";
						apiCallResult.RetryAfterSeconds = TryGetRetryAfterSeconds(response);
						apiCallResult.IsQuotaLimit = response.StatusCode == (HttpStatusCode)429 && IsQuotaLimitResponseBody(text);
						apiCallResult.IsRequestsPerMinuteLimit = response.StatusCode == (HttpStatusCode)429 && !apiCallResult.IsQuotaLimit && (IsRequestsPerMinuteLimitResponseBody(text) || HasRequestsPerMinuteRateLimitHeaders(response));
						apiCallResult.IsRateLimit = response.StatusCode == (HttpStatusCode)429 || apiCallResult.IsRequestsPerMinuteLimit || (!apiCallResult.IsQuotaLimit && IsGenericRateLimitResponseBody(text));
						apiCallResult.ErrorMessage = BuildApiCallFailureMessage(response.StatusCode, text, apiCallResult.RetryAfterSeconds, apiCallResult.IsRateLimit, apiCallResult.IsRequestsPerMinuteLimit, apiCallResult.IsQuotaLimit);
						return apiCallResult;
					}
					using Stream stream = await response.Content.ReadAsStreamAsync();
					if (stream == null)
					{
						apiLog("[HTTP] 响应流为空。");
						apiCallResult.ErrorMessage = "响应流为空";
						return apiCallResult;
					}
					using StreamReader reader = new StreamReader(stream);
					StringBuilder fullContent = new StringBuilder();
					while (true)
					{
						string text;
						string line = (text = await reader.ReadLineAsync());
						if (text == null)
						{
							break;
						}
						if (!string.IsNullOrWhiteSpace(line) && !(line == "data: [DONE]") && line.StartsWith("data: "))
						{
							try
							{
								JObject json = JObject.Parse(line.Substring(6));
								string delta = json["choices"]?[0]?["delta"]?["content"]?.ToString() ?? "";
								fullContent.Append(delta);
							}
							catch (Exception ex)
							{
								Exception parseEx = ex;
								apiLog("[HTTP] 流解析异常: " + parseEx.ToString());
							}
						}
					}
					string raw = fullContent.ToString();
					apiLog("[HTTP] 流式原始内容=\n" + raw);
					apiCallResult.Success = true;
					apiCallResult.Content = CleanAIResponse(raw);
					return apiCallResult;
				}
				finally
				{
					((IDisposable)response)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)request)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			apiLog("[ERROR] CallUniversalApi 异常: " + ex2.ToString());
			apiCallResult.ErrorMessage = ex2.Message;
			return apiCallResult;
		}
	}

	private async Task<string> CallUniversalApi(string sys, string user)
	{
		ApiCallResult apiCallResult = await CallUniversalApiDetailed(sys, user);
		if (apiCallResult.Success)
		{
			return apiCallResult.Content ?? "";
		}
		return "错误: " + (apiCallResult.ErrorMessage ?? "未知错误");
	}

	private static string StripActionTags(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		text = Regex.Replace(text, "\\[ACTION:[^\\]]*\\]", "");
		text = TransferTroopTagRegex.Replace(text, "");
		text = TransferPrisonerTagRegex.Replace(text, "");
		return text.Trim();
	}

	private string CleanAIResponse(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}
		input = Regex.Replace(input, "<think>.*?</think>", "", RegexOptions.Singleline);
		input = input.Replace("**", "").Replace("#", "").Replace("`", "");
		return input.Trim();
	}

	private static float GetNowCampaignDay()
	{
		try
		{
			return (float)CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0f;
		}
	}

	private static int ClampInt(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static float ClampFloat(float value, float min, float max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static int GetRelationWithPlayerSafe(Hero hero)
	{
		try
		{
			if (hero == null || Hero.MainHero == null)
			{
				return 0;
			}
			return hero.GetRelation(Hero.MainHero);
		}
		catch
		{
			return 0;
		}
	}

	private static int ComputePatienceMaxFromRelation(int relation)
	{
		double a = ((relation >= 0) ? (30.0 + (double)relation * 0.5) : (30.0 + (double)relation * 0.18));
		return ClampInt((int)Math.Round(a), 10, 80);
	}

	private static int ToTenLevelIndexByRatio(float current, int max)
	{
		if (max <= 0)
		{
			return 1;
		}
		double num = ClampFloat(current / (float)max, 0f, 1f);
		int num2 = (int)Math.Ceiling(num * 10.0);
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

	private static int ToTenLevelIndexByRelation(int relation)
	{
		double num = ((double)ClampInt(relation, -100, 100) + 100.0) / 200.0;
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

	private static string GetPatienceLevelText(float current, int max)
	{
		int num = ToTenLevelIndexByRatio(current, max);
		return PatienceLevelTexts[num - 1];
	}

	private static string GetRelationLevelText(int relation)
	{
		int num = ToTenLevelIndexByRelation(relation);
		return RelationLevelTexts[num - 1];
	}

	private static int GetRelationLevelIndex(int relation)
	{
		return ToTenLevelIndexByRelation(relation);
	}

	private static void FillTrustSnapshot(ref PatienceSnapshot snap, Hero hero)
	{
		snap.Trust = 0;
		snap.PublicTrust = 0;
		snap.PrivateLove = 0;
		snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0);
		if (hero == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		try
		{
			int publicTrust = RewardSystemBehavior.Instance.GetPublicTrust(hero);
			int trust = (snap.Trust = RewardSystemBehavior.Instance.GetEffectiveTrust(hero));
			snap.PublicTrust = publicTrust;
			snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(trust);
			snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(publicTrust);
			int privateLove = (snap.PrivateLove = (RomanceSystemBehavior.Instance?.GetPrivateLove(hero)).GetValueOrDefault());
			snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(privateLove);
		}
		catch
		{
		}
	}

	private static int GetHistoryRerankBudget(int returnCap)
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

	private static int GetHistoryPerIntentRerank(int rerankBudget, int intentCount)
	{
		int num = ((intentCount > 0) ? intentCount : 1);
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

	private static int GetHistoryPerIntentRecall(int rerankPerIntent)
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

	private static string BuildHistoryRerankText(HistoryLineEntry entry)
	{
		string text = (entry?.Line ?? "").Trim();
		if (text.Length > 220)
		{
			text = text.Substring(0, 220);
		}
		return text;
	}

	private static List<RecallLineScore> SelectHistoryCandidateScores(List<RecallLineScore> scored, string source, string input, int topK)
	{
		List<RecallLineScore> list = new List<RecallLineScore>();
		try
		{
			int num = ((topK <= 0) ? 4 : topK);
			double num2 = 0.21;
			List<RecallLineScore> list2 = (from x in scored
				where x?.Entry != null && !double.IsNaN(x.RerankScore)
				orderby x.RerankScore descending, x.BaseScore descending, (x.Entry != null) ? x.Entry.Index : (-1) descending
				select x).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			double num3 = ((list2.Count > 0) ? list2[0].RerankScore : 0.0);
			double num4 = ((list2.Count > 1) ? list2[1].RerankScore : 0.0);
			double num5 = ((list2.Count > 0) ? list2[0].BaseScore : 0.0);
			double num6 = ((list2.Count > 1) ? list2[1].BaseScore : 0.0);
			HashSet<int> hashSet = new HashSet<int>();
			int num7 = 0;
			for (int i = 0; i < list2.Count; i++)
			{
				if (list.Count >= num)
				{
					break;
				}
				RecallLineScore recallLineScore = list2[i];
				int num8 = recallLineScore?.Entry?.Index ?? int.MinValue;
				if (num8 == int.MinValue || recallLineScore.RerankScore < num2 || !hashSet.Add(num8))
				{
					continue;
				}
				list.Add(recallLineScore);
				num7++;
			}
			if (list.Count < num)
			{
				for (int j = 0; j < list2.Count; j++)
				{
					if (list.Count >= num)
					{
						break;
					}
					RecallLineScore recallLineScore2 = list2[j];
					int num9 = recallLineScore2?.Entry?.Index ?? int.MinValue;
					if (num9 != int.MinValue && hashSet.Add(num9))
					{
						list.Add(recallLineScore2);
					}
				}
			}
			try
			{
				Logger.Log("DialogueHistory", $"semantic_accept source={source} mode=scored selected={list.Count} strictSelected={num7} topN={num} minScore={num2:0.000} bestRaw={num3:0.000} second={num4:0.000} bestEvidence={num5:0.000} secondEvidence={num6:0.000}");
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

	private List<RecallLineScore> FindHistoryCandidateScores(List<HistoryLineEntry> older, WeightedRecallQueryInput queryInput, int topK, out bool onnxUsed)
	{
		onnxUsed = false;
		List<RecallLineScore> list = new List<RecallLineScore>();
		try
		{
			if (older == null || older.Count == 0 || queryInput == null || string.IsNullOrWhiteSpace(queryInput.Text) || topK <= 0)
			{
				return list;
			}
			List<HistoryLineEntry> list2 = older.Where((HistoryLineEntry x) => x != null && !string.IsNullOrWhiteSpace(x.Line)).ToList();
			if (list2.Count <= 0)
			{
				return list;
			}
			if (list2.Count > 260)
			{
				list2 = list2.Skip(list2.Count - 260).ToList();
			}
			int count = list2.Count;
			float[] array = null;
			OnnxEmbeddingEngine instance = OnnxEmbeddingEngine.Instance;
			if (instance != null && instance.IsAvailable && queryInput.Text.Trim().Length >= 2 && instance.TryGetEmbedding(queryInput.Text, out var vector) && vector != null && vector.Length != 0)
			{
				array = vector;
				onnxUsed = true;
			}
			for (int i = 0; i < count; i++)
			{
				HistoryLineEntry historyLineEntry = list2[i];
				string text = historyLineEntry.Line ?? "";
				double num = ((count <= 1) ? 1.0 : ((double)i / (double)(count - 1)));
				double num2 = (IsSystemFactLine(text) ? 1.0 : (ContainsStructuredSignal(text) ? 0.78 : 0.35));
				double num3 = ComputeLexicalOverlapScore(text, queryInput.Terms) * (double)Math.Max(0f, queryInput.Weight);
				double num4 = 0.6 * num2 + 0.3 * num + 0.1 * num3;
				double num5 = num4;
				if (array != null)
				{
					string text2 = text.Trim();
					if (text2.Length > 200)
					{
						text2 = text2.Substring(0, 200);
					}
					if (!string.IsNullOrWhiteSpace(text2) && instance.TryGetEmbedding(text2, out var vector2) && vector2 != null && vector2.Length != 0)
					{
						int num6 = Math.Min(array.Length, vector2.Length);
						double num7 = 0.0;
						for (int j = 0; j < num6; j++)
						{
							num7 += (double)array[j] * (double)vector2[j];
						}
						double num8 = (num7 + 1.0) * 0.5 * (double)Math.Max(0f, queryInput.Weight);
						if (num8 < 0.0)
						{
							num8 = 0.0;
						}
						if (num8 > 1.0)
						{
							num8 = 1.0;
						}
						num5 = num8 * 0.9 + num4 * 0.1;
					}
				}
				list.Add(new RecallLineScore
				{
					Entry = historyLineEntry,
					RawScore = num5,
					BaseScore = num4,
					RerankScore = num5
				});
			}
			list = (from x in list
				orderby x.RawScore descending, x.BaseScore descending, (x.Entry != null) ? x.Entry.Index : (-1) descending
				select x).Take(topK).ToList();
		}
		catch
		{
		}
		return list;
	}

	private List<RecallLineScore> RerankHistoryCandidateScores(string input, List<RecallLineScore> recalled, int rerankTopK, out bool rerankUsed, float scoreWeight = 1f)
	{
		rerankUsed = false;
		List<RecallLineScore> list = new List<RecallLineScore>();
		try
		{
			List<RecallLineScore> list2 = (recalled ?? new List<RecallLineScore>()).Where((RecallLineScore x) => x?.Entry != null).OrderByDescending((RecallLineScore x) => x.RawScore).ThenByDescending((RecallLineScore x) => x.BaseScore).ThenByDescending((RecallLineScore x) => x.Entry?.Index ?? (-1)).ToList();
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
				flag = onnxCrossEncoderReranker != null && onnxCrossEncoderReranker.IsAvailable;
			}
			catch
			{
				flag = false;
			}
			rerankUsed = flag;
			List<string> list3 = null;
			List<float> list4 = null;
			bool flag2 = false;
			if (flag)
			{
				list3 = new List<string>(list2.Count);
				for (int i = 0; i < list2.Count; i++)
				{
					list3.Add((list2[i]?.Entry == null) ? "" : BuildHistoryRerankText(list2[i].Entry));
				}
				flag2 = onnxCrossEncoderReranker.TryScoreBatch(input, list3, out list4) && list4 != null && list4.Count == list2.Count;
			}
			rerankUsed = flag && flag2;
			double num2 = (double)Math.Max(0f, scoreWeight);
			for (int i = 0; i < list2.Count; i++)
			{
				RecallLineScore recallLineScore = list2[i];
				if (recallLineScore?.Entry == null)
				{
					continue;
				}
				double num3 = recallLineScore.RawScore;
				double num4 = num3;
				if (flag && flag2 && list3 != null && i < list3.Count && !string.IsNullOrWhiteSpace(list3[i]) && list4 != null && i < list4.Count)
				{
					num4 = (double)list4[i] * num2;
				}
				list.Add(new RecallLineScore
				{
					Entry = recallLineScore.Entry,
					RawScore = num3,
					BaseScore = recallLineScore.BaseScore,
					RerankScore = num4
				});
			}
			list = SelectHistoryCandidateScores(list, (flag && flag2) ? "cross_encoder" : "recall_fallback", input, num);
		}
		catch
		{
		}
		return list;
	}

	private static void FillSettlementMerchantTrustSnapshot(ref PatienceSnapshot snap, CharacterObject character)
	{
		snap.Trust = 0;
		snap.PublicTrust = 0;
		snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0);
		if (character == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		try
		{
			if (!RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(character, out var kind))
			{
				return;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement == null)
			{
				return;
			}
			int settlementPublicTrust = RewardSystemBehavior.Instance.GetSettlementLocalPublicTrust(currentSettlement);
			int settlementSharedPublicTrust = RewardSystemBehavior.Instance.GetSettlementSharedPublicTrust(currentSettlement);
			int num = settlementPublicTrust + settlementSharedPublicTrust;
			int settlementMerchantEffectiveTrust = RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(currentSettlement, kind);
			snap.Trust = settlementMerchantEffectiveTrust;
			snap.PublicTrust = num;
			snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(settlementMerchantEffectiveTrust);
			snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(num);
		}
		catch
		{
		}
	}

	private static string BuildCompactStateLine(PatienceSnapshot snap)
	{
		int num = (int)Math.Round(snap.Current);
		return $"P(耐心)={num}/{snap.Max}({snap.PatienceLevel}) | R(家族关系)={snap.Relation}({snap.RelationLevel}) | T(综合信任)={snap.Trust}({snap.TrustLevel}) | L(私人关系)={snap.PrivateLove}({snap.PrivateLoveLevel})";
	}

	private static string BuildHeroPatienceKey(Hero hero)
	{
		string text = (hero?.StringId ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	private static string BuildUnnamedPatienceKey(string unnamedKey, string npcName)
	{
		string text = (unnamedKey ?? "").Trim().ToLower();
		if (!string.IsNullOrEmpty(text))
		{
			return "unnamed:" + text;
		}
		string text2 = (npcName ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text2))
		{
			return "";
		}
		return "name:" + text2;
	}

	private PatienceState GetOrCreateStateUnsafe(string key, int maxPatience, float nowDay)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}
		if (!_patienceStates.TryGetValue(key, out var value) || value == null)
		{
			value = new PatienceState
			{
				Value = maxPatience,
				LastDay = nowDay,
				NoInterestRounds = 0
			};
			_patienceStates[key] = value;
		}
		return value;
	}

	private void RecoverPatienceUnsafe(PatienceState state, int maxPatience, float nowDay)
	{
		if (state == null)
		{
			return;
		}
		if (nowDay > state.LastDay)
		{
			float num = nowDay - state.LastDay;
			if (num > 0f)
			{
				state.Value += num * 4f;
				state.LastDay = nowDay;
				if (state.NoInterestRounds > 0)
				{
					int num2 = (int)Math.Floor(num);
					if (num2 > 0)
					{
						state.NoInterestRounds = Math.Max(0, state.NoInterestRounds - num2);
					}
				}
			}
		}
		state.Value = ClampFloat(state.Value, 0f, maxPatience);
		if (state.Value > 0.01f && state.ExhaustedRefusalCount > 0)
		{
			state.ExhaustedRefusalCount = 0;
		}
	}

	private PatienceSnapshot GetHeroPatienceSnapshot(Hero hero)
	{
		PatienceSnapshot snap = new PatienceSnapshot
		{
			Key = "",
			DisplayName = (hero?.Name?.ToString() ?? "NPC"),
			Relation = 0,
			Trust = 0,
			PublicTrust = 0,
			PrivateLove = 0,
			Max = 30,
			Current = 30f,
			PatienceLevel = "一般",
			RelationLevel = "中立",
			TrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0)
		};
		if (hero == null)
		{
			return snap;
		}
		string text = BuildHeroPatienceKey(hero);
		if (string.IsNullOrEmpty(text))
		{
			return snap;
		}
		int relationWithPlayerSafe = GetRelationWithPlayerSafe(hero);
		int num = ComputePatienceMaxFromRelation(relationWithPlayerSafe);
		float nowCampaignDay = GetNowCampaignDay();
		float value;
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(text, num, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, num, nowCampaignDay);
			value = orCreateStateUnsafe.Value;
		}
		snap.Key = text;
		snap.Relation = relationWithPlayerSafe;
		snap.Max = num;
		snap.Current = value;
		snap.PatienceLevel = GetPatienceLevelText(value, num);
		snap.RelationLevel = GetRelationLevelText(relationWithPlayerSafe);
		FillTrustSnapshot(ref snap, hero);
		return snap;
	}

	private PatienceSnapshot GetUnnamedPatienceSnapshot(string unnamedKey, string npcName, string displayName = null)
	{
		string text = BuildUnnamedPatienceKey(unnamedKey, npcName);
		PatienceSnapshot result = new PatienceSnapshot
		{
			Key = text,
			DisplayName = (string.IsNullOrWhiteSpace(displayName) ? (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim()) : displayName.Trim()),
			Relation = 0,
			Trust = 0,
			PublicTrust = 0,
			PrivateLove = 0,
			Max = 30,
			Current = 30f,
			PatienceLevel = GetPatienceLevelText(30f, 30),
			RelationLevel = GetRelationLevelText(0),
			TrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(0),
			PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(0)
		};
		if (string.IsNullOrWhiteSpace(text))
		{
			return result;
		}
		float nowCampaignDay = GetNowCampaignDay();
		float value;
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(text, 30, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, 30, nowCampaignDay);
			value = orCreateStateUnsafe.Value;
		}
		result.Current = value;
		result.PatienceLevel = GetPatienceLevelText(value, 30);
		try
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter;
			FillSettlementMerchantTrustSnapshot(ref result, oneToOneConversationCharacter);
		}
		catch
		{
		}
		return result;
	}

	private static PatienceMood ParseMoodToken(string token)
	{
		string text = (token ?? "").Trim().ToLower();
		if (string.IsNullOrEmpty(text))
		{
			return PatienceMood.Neutral;
		}
		switch (text)
		{
		case "delighted":
		case "very_happy":
		case "thrilled":
		case "heartwarmed":
		case "heart_warmed":
		case "affectionate":
		case "fond":
		case "sweet":
			return PatienceMood.Delighted;
		default:
			if (!(text == "鎰夊揩"))
			{
				switch (text)
				{
				default:
					if (!(text == "鐢熸皵"))
					{
						if (text == "bored" || text == "boring" || text == "鏃犺亰")
						{
							return PatienceMood.Bored;
						}
						return PatienceMood.Neutral;
					}
					goto case "annoyed";
				case "annoyed":
				case "angry":
				case "upset":
				case "irritated":
				case "displeased":
				case "涓嶆偊":
					return PatienceMood.Annoyed;
				}
			}
			goto case "joy";
		case "joy":
		case "happy":
		case "positive":
		case "amused":
		case "friendly":
		case "鍠滄偊":
			return PatienceMood.Joy;
		}
	}

	private static PatienceMood ExtractMoodAndStripTag(ref string text)
	{
		PatienceMood result = PatienceMood.Neutral;
		string text2 = text ?? "";
		MatchCollection matchCollection = MoodTagRegex.Matches(text2);
		if (matchCollection != null && matchCollection.Count > 0)
		{
			string value = matchCollection[matchCollection.Count - 1].Groups[1].Value;
			result = ParseMoodToken(value);
			text2 = MoodTagRegex.Replace(text2, "");
		}
		text = (text2 ?? "").Trim();
		return result;
	}

	private static int ComputePatienceDelta(PatienceMood mood, ref int noInterestRounds)
	{
		int num;
		switch (mood)
		{
		case PatienceMood.Delighted:
			num = 2;
			noInterestRounds = Math.Max(0, noInterestRounds - 3);
			break;
		case PatienceMood.Joy:
			num = 1;
			noInterestRounds = Math.Max(0, noInterestRounds - 2);
			break;
		case PatienceMood.Annoyed:
			num = -3;
			noInterestRounds++;
			break;
		case PatienceMood.Bored:
			num = -2;
			noInterestRounds++;
			break;
		default:
			num = -1;
			noInterestRounds++;
			break;
		}
		if (mood != PatienceMood.Joy && mood != PatienceMood.Delighted && noInterestRounds >= 3)
		{
			num--;
		}
		return num;
	}

	private static int ComputeNativeRelationDelta(PatienceMood mood, int currentRelation)
	{
		switch (mood)
		{
		case PatienceMood.Delighted:
			if (currentRelation >= 95)
			{
				return 0;
			}
			return 1;
		case PatienceMood.Annoyed:
			if (currentRelation <= -95)
			{
				return 0;
			}
			return -1;
		default:
			return 0;
		}
	}

	private static int ComputePrivateLoveDelta(PatienceMood mood)
	{
		switch (mood)
		{
		case PatienceMood.Delighted:
			return 2;
		case PatienceMood.Joy:
			return 1;
		case PatienceMood.Bored:
			return -1;
		case PatienceMood.Annoyed:
			return -2;
		default:
			return 0;
		}
	}

	private bool ApplyPatienceDeltaInternal(string key, int maxPatience, PatienceMood mood, out int before, out int after)
	{
		before = 0;
		after = 0;
		if (string.IsNullOrWhiteSpace(key) || maxPatience <= 0)
		{
			return false;
		}
		float nowCampaignDay = GetNowCampaignDay();
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(key, maxPatience, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, maxPatience, nowCampaignDay);
			int num = (int)Math.Round(orCreateStateUnsafe.Value);
			int num2 = ComputePatienceDelta(mood, ref orCreateStateUnsafe.NoInterestRounds);
			orCreateStateUnsafe.Value = ClampFloat(orCreateStateUnsafe.Value + (float)num2, 0f, maxPatience);
			orCreateStateUnsafe.LastDay = nowCampaignDay;
			int num3 = (int)Math.Round(orCreateStateUnsafe.Value);
			before = num;
			after = num3;
			return num > 0 && num3 <= 0;
		}
	}

	private static string BuildExhaustedReply(string npcName, int relation, int refusalCount = 1)
	{
		string text = (string.IsNullOrWhiteSpace(npcName) ? "我" : npcName);
		if (refusalCount < 1)
		{
			refusalCount = 1;
		}
		if (relation >= 40)
		{
			string[] array = new string[3]
			{
				text + "压低声音：今天我确实有些乏了，改天再聊。",
				text + "轻叹：我现在没有余力应付长谈，先到这里吧。",
				text + "摇头道：等我缓一缓，再听你说。"
			};
			return array[(refusalCount - 1) % array.Length];
		}
		if (relation <= -20)
		{
			string[] array2 = new string[3]
			{
				text + "皱眉道：我眼下有要事，没空陪你闲扯，退下。",
				text + "冷声道：军务在身，今天到此为止。",
				text + "摆手打断：我还要处理别的事，改日再说。"
			};
			return array2[(refusalCount - 1) % array2.Length];
		}
		string[] array3 = new string[3]
		{
			text + "摆摆手：我这边还有事，今天就先到这。",
			text + "皱了皱眉：行程紧，改日再谈。",
			text + "侧过身：先到这里，我得去处理别的事。"
		};
		return array3[(refusalCount - 1) % array3.Length];
	}

	private static string BuildExhaustedPatienceInstruction(bool includeRelationPenalty)
	{
		string text = BuildPlayerPublicDisplayNameForPrompt();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		if (!includeRelationPenalty)
		{
			return "耐心已归零：本轮优先用军务在身、行程紧、需要离开、精神疲惫或另有要事等理由，给出委婉的回避式回复，尽量收束或回避当前话题；若" + text + "继续追问，仍可继续回应，但语气应明显更谨慎或更不耐烦，不要直说系统规则或数值。";
		}
		return "耐心已归零：本轮优先用军务在身、行程紧、需要离开、精神疲惫或另有要事等理由，给出委婉的回避式回复，尽量收束或回避当前话题；若" + text + "继续追问，仍可继续回应，但语气应明显更谨慎或更不耐烦，系统会额外降低你与" + text + "的关系，不要直说系统规则或数值。";
	}

	private static string BuildPatiencePromptText(PatienceSnapshot snap)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【4.四值状态】");
		stringBuilder.AppendLine(BuildCompactStateLine(snap));
		stringBuilder.AppendLine("【NPC耐心状态】");
		if (snap.Current <= 0.01f)
		{
			stringBuilder.AppendLine(BuildExhaustedPatienceInstruction(includeRelationPenalty: true));
		}
		else
		{
			stringBuilder.AppendLine("耐心越低，语气越不耐烦；若耐心耗尽，应优先给出委婉的回避式回复。");
		}
		stringBuilder.AppendLine("规则：情绪标签优先影响私人关系 L，并在最强正负情绪下顺带影响原生关系 R。DELIGHTED=L+2 且 R+1；JOY=L+1；NEUTRAL=0；BORED=L-1；ANNOYED=L-2 且 R-1。回复末尾必须且只能追加一个 [ACTION:MOOD:DELIGHTED/JOY/NEUTRAL/BORED/ANNOYED]；耐心低时更不耐烦，耐心归零后仍可继续对话，但继续交流会额外降低关系。");
		return stringBuilder.ToString();
	}

	private string BuildDirectPatiencePrompt(Hero targetHero, out bool exhausted, out string exhaustedReply)
	{
		exhausted = false;
		exhaustedReply = "";
		if (targetHero == null)
		{
			return "";
		}
		PatienceSnapshot heroPatienceSnapshot = GetHeroPatienceSnapshot(targetHero);
		if (heroPatienceSnapshot.Current <= 0.01f)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(BuildPatiencePromptText(heroPatienceSnapshot).TrimEnd());
			stringBuilder.AppendLine("除 [ACTION:MOOD:...] 外，不要输出任何 ACTION 标签。");
			return stringBuilder.ToString();
		}
		return BuildPatiencePromptText(heroPatienceSnapshot);
	}

	private void ApplyPatienceFromHeroResponse(Hero targetHero, ref string aiResponse, bool directConversation)
	{
		if (targetHero == null)
		{
			string text = aiResponse ?? "";
			ExtractMoodAndStripTag(ref text);
			aiResponse = text;
			return;
		}
		PatienceSnapshot heroPatienceSnapshot = GetHeroPatienceSnapshot(targetHero);
		PatienceMood patienceMood = ExtractMoodAndStripTag(ref aiResponse);
		int before;
		int after;
		bool flag = ApplyPatienceDeltaInternal(heroPatienceSnapshot.Key, heroPatienceSnapshot.Max, patienceMood, out before, out after);
		int relation = heroPatienceSnapshot.Relation;
		int privateLove = heroPatienceSnapshot.PrivateLove;
		int num = ComputeNativeRelationDelta(patienceMood, relation);
		int num2 = ComputePrivateLoveDelta(patienceMood);
		bool flag2 = before <= 0;
		if (flag2)
		{
			num--;
		}
		int num3 = relation;
		if (num != 0)
		{
			try
			{
				if (Hero.MainHero != null && targetHero != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, targetHero, num);
					num3 = GetRelationWithPlayerSafe(targetHero);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Patience", "[WARN] apply relation delta failed: " + ex.Message);
			}
		}
		if (num2 != 0)
		{
			RomanceSystemBehavior.Instance?.AdjustPrivateLove(targetHero, num2, "mood_tag");
		}
		int num4 = RomanceSystemBehavior.Instance?.GetPrivateLove(targetHero) ?? privateLove;
		try
		{
			Logger.Log("Patience", $"hero={targetHero.StringId} mood={patienceMood} value={before}->{after}/{heroPatienceSnapshot.Max} relation={relation}->{num3} delta={num} privateLove={privateLove}->{num4} deltaLove={num2}");
			Logger.Obs("Patience", "hero_update", new Dictionary<string, object>
			{
				["heroId"] = targetHero.StringId ?? "",
				["mood"] = patienceMood.ToString(),
				["patienceBefore"] = before,
				["patienceAfter"] = after,
				["patienceMax"] = heroPatienceSnapshot.Max,
				["relationBefore"] = relation,
				["relationAfter"] = num3,
				["relationDelta"] = num,
				["privateLoveBefore"] = privateLove,
				["privateLoveAfter"] = num4,
				["privateLoveDelta"] = num2,
				["continuedAtZero"] = flag2,
				["directConversation"] = directConversation
			});
			Logger.Metric("patience.hero_update");
		}
		catch
		{
		}
		if (!flag)
		{
			return;
		}
		try
		{
			Logger.Log("Patience", "hero=" + targetHero.StringId + " reached_zero=true defer_exhausted_to_next_round=" + (directConversation ? "direct" : "scene"));
		}
		catch
		{
		}
	}

	private void ApplyPatienceFromUnnamedResponse(string unnamedKey, string npcName, ref string aiResponse)
	{
		PatienceSnapshot unnamedPatienceSnapshot = GetUnnamedPatienceSnapshot(unnamedKey, npcName);
		PatienceMood patienceMood = ExtractMoodAndStripTag(ref aiResponse);
		if (string.IsNullOrWhiteSpace(unnamedPatienceSnapshot.Key))
		{
			return;
		}
		int before;
		int after;
		bool flag = ApplyPatienceDeltaInternal(unnamedPatienceSnapshot.Key, unnamedPatienceSnapshot.Max, patienceMood, out before, out after);
		try
		{
			Logger.Log("Patience", $"unnamed={unnamedPatienceSnapshot.Key} mood={patienceMood} value={before}->{after}/{unnamedPatienceSnapshot.Max}");
			Logger.Obs("Patience", "unnamed_update", new Dictionary<string, object>
			{
				["key"] = unnamedPatienceSnapshot.Key ?? "",
				["mood"] = patienceMood.ToString(),
				["patienceBefore"] = before,
				["patienceAfter"] = after,
				["patienceMax"] = unnamedPatienceSnapshot.Max
			});
			Logger.Metric("patience.unnamed_update");
		}
		catch
		{
		}
		if (!flag)
		{
			return;
		}
		try
		{
			Logger.Log("Patience", "unnamed=" + unnamedPatienceSnapshot.Key + " reached_zero=true defer_exhausted_to_next_round=scene");
		}
		catch
		{
		}
	}

	private bool TryGetHeroSceneStatus(Hero hero, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		if (hero == null)
		{
			return false;
		}
		PatienceSnapshot heroPatienceSnapshot = GetHeroPatienceSnapshot(hero);
		int num = (int)Math.Round(heroPatienceSnapshot.Current);
		statusLine = "- " + heroPatienceSnapshot.DisplayName + ": " + BuildCompactStateLine(heroPatienceSnapshot);
		if (num <= 0)
		{
			statusLine += "，耐心已归零";
			statusLine += "\n  " + BuildExhaustedPatienceInstruction(includeRelationPenalty: true);
		}
		return true;
	}

	private bool TryGetUnnamedSceneStatus(string unnamedKey, string npcName, string displayName, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		PatienceSnapshot unnamedPatienceSnapshot = GetUnnamedPatienceSnapshot(unnamedKey, npcName, displayName);
		if (string.IsNullOrWhiteSpace(unnamedPatienceSnapshot.Key))
		{
			return false;
		}
		int num = (int)Math.Round(unnamedPatienceSnapshot.Current);
		statusLine = $"- {unnamedPatienceSnapshot.DisplayName}: P(耐心)={num}/{unnamedPatienceSnapshot.Max}({unnamedPatienceSnapshot.PatienceLevel}) | R(家族关系)=0(中立) | T(综合信任)={unnamedPatienceSnapshot.Trust}({unnamedPatienceSnapshot.TrustLevel}) | L(私人关系)=0({RomanceSystemBehavior.GetPrivateLoveLevelText(0)})";
		if (num <= 0)
		{
			statusLine += "，耐心已归零";
			statusLine += "\n  " + BuildExhaustedPatienceInstruction(includeRelationPenalty: false);
		}
		return true;
	}

	private bool TryGetUnnamedSceneStatus(string unnamedKey, string npcName, out string statusLine, out bool canSpeak)
	{
		return TryGetUnnamedSceneStatus(unnamedKey, npcName, null, out statusLine, out canSpeak);
	}

	private static string BuildScenePatienceInstruction()
	{
		return "【耐心标签要求】：你必须在你回复的末尾加入且只能加入一个合适的标签。强烈被打动/明显心动：[ACTION:MOOD:DELIGHTED]；喜悦友好：[ACTION:MOOD:JOY]；平淡：[ACTION:MOOD:NEUTRAL]；无聊冷淡：[ACTION:MOOD:BORED]；恼火反感：[ACTION:MOOD:ANNOYED]。并始终按四值状态中的 P/R/T/L 来决定语气与合作度。";
	}

	private void SyncPatienceData(IDataStore dataStore)
	{
		if (_patienceStates == null)
		{
			_patienceStates = new Dictionary<string, PatienceState>();
		}
		if (_patienceStorage == null)
		{
			_patienceStorage = new Dictionary<string, string>();
		}
		if (dataStore == null)
		{
			return;
		}
		try
		{
			if (dataStore.IsSaving)
			{
				lock (_patienceLock)
				{
					_patienceStorage.Clear();
					foreach (KeyValuePair<string, PatienceState> patienceState in _patienceStates)
					{
						if (!string.IsNullOrWhiteSpace(patienceState.Key) && patienceState.Value != null)
						{
							try
							{
								PatienceStateSaveModel value = new PatienceStateSaveModel
								{
									Value = patienceState.Value.Value,
									LastDay = patienceState.Value.LastDay,
									NoInterestRounds = patienceState.Value.NoInterestRounds,
									ExhaustedRefusalCount = patienceState.Value.ExhaustedRefusalCount
								};
								_patienceStorage[patienceState.Key] = JsonConvert.SerializeObject(value);
							}
							catch
							{
							}
						}
					}
				}
				Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_patienceStorage);
				dataStore.SyncData("_patienceStates_v1", ref dictionary);
				return;
			}
			lock (_patienceLock)
			{
				_patienceStates.Clear();
				_patienceStorage.Clear();
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			dataStore.SyncData("_patienceStates_v1", ref dictionary2);
			_patienceStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary2, "Patience");
			if (_patienceStorage == null)
			{
				return;
			}
			lock (_patienceLock)
			{
				foreach (KeyValuePair<string, string> item in _patienceStorage)
				{
					if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
					{
						continue;
					}
					try
					{
						PatienceStateSaveModel patienceStateSaveModel = JsonConvert.DeserializeObject<PatienceStateSaveModel>(item.Value);
						if (patienceStateSaveModel != null)
						{
							_patienceStates[item.Key] = new PatienceState
							{
								Value = patienceStateSaveModel.Value,
								LastDay = patienceStateSaveModel.LastDay,
								NoInterestRounds = patienceStateSaveModel.NoInterestRounds,
								ExhaustedRefusalCount = patienceStateSaveModel.ExhaustedRefusalCount
							};
						}
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Patience", "[ERROR] SyncPatienceData failed: " + ex.Message);
			lock (_patienceLock)
			{
				_patienceStates = new Dictionary<string, PatienceState>();
				_patienceStorage = new Dictionary<string, string>();
			}
		}
	}

	public static string BuildDirectPatiencePromptForExternal(Hero targetHero, out bool exhausted, out string exhaustedReply)
	{
		exhausted = false;
		exhaustedReply = "";
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildDirectPatiencePrompt(targetHero, out exhausted, out exhaustedReply);
		}
		catch
		{
			exhausted = false;
			exhaustedReply = "";
			return "";
		}
	}

	public static void ApplyPatienceFromDirectResponseExternal(Hero targetHero, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatienceFromHeroResponse(targetHero, ref aiResponse, directConversation: true);
		}
		catch
		{
		}
	}

	public static void ApplyPatienceFromSceneHeroResponseExternal(Hero targetHero, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatienceFromHeroResponse(targetHero, ref aiResponse, directConversation: false);
		}
		catch
		{
		}
	}

	public static void ApplyPatienceFromSceneUnnamedResponseExternal(string unnamedKey, string npcName, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatienceFromUnnamedResponse(unnamedKey, npcName, ref aiResponse);
		}
		catch
		{
		}
	}

	public static bool TryGetSceneHeroPatienceStatusForExternal(Hero hero, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.TryGetHeroSceneStatus(hero, out statusLine, out canSpeak) ?? false;
		}
		catch
		{
			statusLine = "";
			canSpeak = true;
			return false;
		}
	}

	public static bool TryGetSceneUnnamedPatienceStatusForExternal(string unnamedKey, string npcName, out string statusLine, out bool canSpeak)
	{
		return TryGetSceneUnnamedPatienceStatusForExternal(unnamedKey, npcName, null, out statusLine, out canSpeak);
	}

	public static bool TryGetSceneUnnamedPatienceStatusForExternal(string unnamedKey, string npcName, string displayName, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.TryGetUnnamedSceneStatus(unnamedKey, npcName, displayName, out statusLine, out canSpeak) ?? false;
		}
		catch
		{
			statusLine = "";
			canSpeak = true;
			return false;
		}
	}

	public static string GetScenePatienceInstructionForExternal()
	{
		return BuildScenePatienceInstruction();
	}

	public static string BuildDirectNamePatienceBadgeForExternal(Hero targetHero)
	{
		return "";
	}

	public static string BuildScenePatienceBadgeForHeroExternal(Hero hero)
	{
		return "";
	}

	public static string BuildScenePatienceBadgeForUnnamedExternal(string unnamedKey, string npcName)
	{
		return "";
	}

	private void DevRootMenuInit(MenuCallbackArgs args)
	{
		try
		{
			args.MenuTitle = new TextObject("开发者工具");
		}
		catch
		{
		}
	}

	private bool DevRootEntryCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return DuelSettings.GetSettings()?.EnableDevEditHistory ?? false;
	}

	private void DevRootEntryConsequence(MenuCallbackArgs args)
	{
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableDevEditHistory)
		{
			InformationManager.DisplayMessage(new InformationMessage("开发者数据管理未开启（请在 MCM 中启用）。"));
		}
		else
		{
			GameMenu.SwitchToMenu("AnimusForge_dev_root");
		}
	}

	private bool DevRootSubOptionCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return DuelSettings.GetSettings()?.EnableDevEditHistory ?? false;
	}

	private void DevRootHeroOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevHeroNpcMenu();
	}

	private void DevRootNonHeroOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevUnnamedPersonaMenu();
	}

	private void DevRootKnowledgeOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevKnowledgeMenu();
	}

	private void DevRootEventOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevEventEditorMenu();
	}

	private void DevRootAllOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevAllDataMenu();
	}

	private void DevRootVoiceMappingOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevVoiceMappingMenu();
	}

	private bool DevRootBackCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void DevRootBackConsequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("town");
	}

	private void OpenDevHeroNpcMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit_hero", "编辑 HeroNPC（历史/赊账/个性背景）…", null));
		list.Add(new InquiryElement("single_ie", "单个 HeroNPC 导入/导出…", null));
		list.Add(new InquiryElement("export_hero_all", "全量导出（HeroNPC：历史+赊账+个性背景，选文件夹）", null));
		list.Add(new InquiryElement("import_hero_all", "全量导入（HeroNPC：历史+赊账+个性背景，选文件夹）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("HeroNPC 编辑/导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevHeroNpcMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHeroNpcMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "edit_hero":
				OpenDevTownEditorHeroSelection();
				break;
			case "single_ie":
				OpenDevSingleNpcHeroSelection();
				break;
			case "export_hero_all":
				OpenExportFolderPicker("全量导出（HeroNPC）- 选择文件夹", ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			case "import_hero_all":
				OpenImportFolderPicker("全量导入（HeroNPC）- 选择文件夹", ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			}
		}
	}

	private void OpenDevEventEditorMenu()
	{
		EnsureWeekZeroOpeningSummaryEvents();
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit_world_summary", "编辑世界开局概要", null));
		list.Add(new InquiryElement("edit_kingdom_summary", "编辑王国开局概要", null));
		list.Add(new InquiryElement("preview_weekly_materials", "查看本周事件素材预览", null));
		list.Add(new InquiryElement("preview_weekly_prompts", "预览本周周报 Prompt", null));
		list.Add(new InquiryElement("generate_weekly_reports", "生成本周周报草案", null));
		list.Add(new InquiryElement("kingdom_stability_lab", "王国稳定度与叛乱实验", null));
		list.Add(new InquiryElement("view_events", "查看事件与素材", null));
		list.Add(new InquiryElement("export_event_data", "全量导出（事件编辑，选文件夹）", null));
		list.Add(new InquiryElement("import_event_data", "全量导入（事件编辑，选文件夹）", null));
		list.Add(new InquiryElement("clear_all", "清空全部事件概要", null));
		list.Add(new InquiryElement("back", "返回", null));
		string text = BuildDevEventEditorMenuDescription();
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件编辑", text, list, isExitShown: true, 0, 1, "进入", "返回", OnDevEventEditorMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevEventEditorMenuSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "edit_world_summary":
			OpenDevEditWorldOpeningSummary();
			break;
		case "edit_kingdom_summary":
			OpenDevKingdomOpeningSummaryMenu();
			break;
		case "preview_weekly_materials":
			OpenDevWeeklyEventMaterialPreviewMenu();
			break;
		case "preview_weekly_prompts":
			OpenDevWeeklyReportPromptPreviewMenu();
			break;
		case "generate_weekly_reports":
			ConfirmGenerateDevWeeklyReports();
			break;
		case "kingdom_stability_lab":
			OpenDevKingdomStabilityLabMenu();
			break;
		case "view_events":
			OpenDevEventViewerMenu(0);
			break;
		case "export_event_data":
			OpenExportFolderPicker("全量导出（事件编辑）- 选择文件夹", ExportImportScope.EventData, OpenDevEventEditorMenu);
			break;
		case "import_event_data":
			OpenImportFolderPicker("全量导入（事件编辑）- 选择文件夹", ExportImportScope.EventData, OpenDevEventEditorMenu);
			break;
		case "clear_all":
			ConfirmClearAllEventOpeningSummaries();
			break;
		}
	}

	private string BuildDevEventEditorMenuDescription()
	{
		int num = 0;
		if (_eventKingdomOpeningSummaries != null)
		{
			num = _eventKingdomOpeningSummaries.Count((KeyValuePair<string, string> x) => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value));
		}
		int num2 = 0;
		try
		{
			num2 = Kingdom.All.Count((Kingdom x) => x != null && !string.IsNullOrWhiteSpace(x.StringId));
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("世界开局概要：" + (string.IsNullOrWhiteSpace(_eventWorldOpeningSummary) ? "未设置" : "已设置"));
		stringBuilder.AppendLine("王国开局概要：" + num + "/" + num2 + " 已设置");
		stringBuilder.AppendLine("事件记录：" + ((_eventRecordEntries != null) ? SanitizeEventRecordEntries(_eventRecordEntries).Count : 0) + " 条");
		stringBuilder.AppendLine("周报篇幅档位：" + GetWeeklyReportPromptProfile().Label);
		int num3 = GetDevEditableKingdoms().Count;
		int num4 = GetDevEditableKingdoms().Count((Kingdom x) => GetKingdomStabilityValue(x) != KingdomStabilityDefaultValue);
		stringBuilder.AppendLine("王国稳定度：" + num4 + "/" + num3 + " 已偏离默认值");
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevEditWorldOpeningSummary()
	{
		DevTextEditorHelper.ShowLongTextEditor("编辑世界开局概要", "这段文本会作为世界事件系统的初始背景底稿。", "请输入世界开局概要（留空=清空）。", _eventWorldOpeningSummary ?? "", delegate(string input)
		{
			_eventWorldOpeningSummary = (input ?? "").Trim();
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(_eventWorldOpeningSummary) ? "已清空世界开局概要。" : "世界开局概要已更新。"));
			OpenDevEventEditorMenu();
		}, delegate
		{
			OpenDevEventEditorMenu();
		});
	}

	private void OpenDevKingdomOpeningSummaryMenu()
	{
		List<Kingdom> list = GetDevEditableKingdoms();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的王国。"));
			OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (Kingdom item in list)
		{
			string devKingdomSummaryLabel = BuildDevKingdomSummaryLabel(item);
			list2.Add(new InquiryElement(new DevKingdomSummaryMenuItem
			{
				KingdomId = item.StringId ?? "",
				DisplayName = item.Name?.ToString() ?? (item.StringId ?? "王国")
			}, devKingdomSummaryLabel, null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("请选择要编辑的王国开局概要。");
		stringBuilder.AppendLine("这些文本会作为该王国后续每周事件生成的基础背景。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑王国开局概要", stringBuilder.ToString().TrimEnd(), list2, isExitShown: true, 0, 1, "编辑", "返回", OnDevKingdomOpeningSummaryMenuSelected, delegate
		{
			OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevKingdomOpeningSummaryMenuSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevEventEditorMenu();
			return;
		}
		if (selected[0].Identifier is string text && text == "back")
		{
			OpenDevEventEditorMenu();
			return;
		}
		if (selected[0].Identifier is DevKingdomSummaryMenuItem devKingdomSummaryMenuItem)
		{
			Kingdom kingdom = FindKingdomById(devKingdomSummaryMenuItem.KingdomId);
			if (kingdom == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到对应的王国。"));
				OpenDevKingdomOpeningSummaryMenu();
			}
			else
			{
				OpenDevEditKingdomOpeningSummary(kingdom);
			}
		}
		else
		{
			OpenDevKingdomOpeningSummaryMenu();
		}
	}

	private void OpenDevEditKingdomOpeningSummary(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenDevKingdomOpeningSummaryMenu();
			return;
		}
		string text = GetKingdomOpeningSummary(kingdom);
		string text2 = kingdom.Name?.ToString() ?? (kingdom.StringId ?? "王国");
		string subtitleText = "这段文本会作为该王国的开局底稿，供后续事件系统生成该王国的周事件时参考。";
		DevTextEditorHelper.ShowLongTextEditor("编辑王国开局概要 - " + text2, subtitleText, "请输入该王国的开局概要（留空=清空）。", text, delegate(string input)
		{
			SaveKingdomOpeningSummary(kingdom, input);
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(input) ? ("已清空 " + text2 + " 的开局概要。") : (text2 + " 的开局概要已更新。")));
			OpenDevKingdomOpeningSummaryMenu();
		}, delegate
		{
			OpenDevKingdomOpeningSummaryMenu();
		});
	}

	private void ConfirmClearAllEventOpeningSummaries()
	{
		InformationManager.ShowInquiry(new InquiryData("确认清空事件概要", "这会清空世界开局概要，以及所有王国的开局概要。\n此操作不可撤销，是否继续？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认清空", "取消", delegate
		{
			_eventWorldOpeningSummary = "";
			if (_eventKingdomOpeningSummaries == null)
			{
				_eventKingdomOpeningSummaries = new Dictionary<string, string>();
			}
			else
			{
				_eventKingdomOpeningSummaries.Clear();
			}
			InformationManager.DisplayMessage(new InformationMessage("已清空全部事件概要。"));
			OpenDevEventEditorMenu();
		}, delegate
		{
			OpenDevEventEditorMenu();
		}));
	}

	private void OpenDevKingdomStabilityLabMenu()
	{
		List<Kingdom> devEditableKingdoms = GetDevEditableKingdoms();
		if (devEditableKingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可测试的王国。"));
			OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (Kingdom item in devEditableKingdoms)
		{
			list.Add(new InquiryElement(item.StringId ?? "", BuildDevKingdomStabilityLabel(item), null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里用于实验王国稳定度与带城反出逻辑。");
		stringBuilder.AppendLine("默认所有王国稳定度都是 50（一般），不会自动叛乱。");
		stringBuilder.AppendLine("现在周报里的 STAB_* 评级标签也会改变王国稳定度；手动调整与周报评级都会影响后续周结算。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("请选择一个王国进入详情。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("王国稳定度与叛乱实验", stringBuilder.ToString().TrimEnd(), list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventEditorMenu();
				return;
			}
			if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevEventEditorMenu();
				return;
			}
			string kingdomId = selected[0].Identifier as string;
			Kingdom kingdom = FindKingdomById(kingdomId);
			if (kingdom == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到对应的王国。"));
				OpenDevKingdomStabilityLabMenu();
			}
			else
			{
				OpenDevKingdomStabilityDetailMenu(kingdom);
			}
		}, delegate
		{
			OpenDevEventEditorMenu();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildDevKingdomStabilityLabel(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "未知王国";
		}
		int kingdomStabilityValue = GetKingdomStabilityValue(kingdom);
		return GetKingdomDisplayName(kingdom, "王国") + " [" + GetKingdomStabilityTierText(kingdomStabilityValue) + "/" + kingdomStabilityValue + "] 叛乱周概率 " + FormatKingdomRebellionChance(GetKingdomRebellionWeeklyChance(kingdomStabilityValue));
	}

	private string BuildDevKingdomStabilityDetailText(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "找不到对应的王国。";
		}
		int kingdomStabilityValue = GetKingdomStabilityValue(kingdom);
		float kingdomRebellionWeeklyChance = GetKingdomRebellionWeeklyChance(kingdomStabilityValue);
		int kingdomStabilityRelationTargetOffset = GetKingdomStabilityRelationTargetOffset(kingdomStabilityValue);
		int kingdomStabilityWeeklyBalancingDelta = GetKingdomStabilityWeeklyBalancingDelta(kingdomStabilityValue);
		List<KingdomRebellionCandidateInfo> list = EvaluateKingdomRebellionCandidates(kingdom, forceTrigger: false);
		List<KingdomRebellionCandidateInfo> list2 = list.Where((KingdomRebellionCandidateInfo x) => x != null && x.Eligible).Take(5).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("王国：" + GetKingdomDisplayName(kingdom, "某王国"));
		stringBuilder.AppendLine("当前稳定度：" + kingdomStabilityValue + "（" + GetKingdomStabilityTierText(kingdomStabilityValue) + "）");
		stringBuilder.AppendLine("动态平衡周修正：" + FormatKingdomStabilityRelationOffsetText(kingdomStabilityWeeklyBalancingDelta));
		stringBuilder.AppendLine("当前非王族关系修正：" + FormatKingdomStabilityRelationOffsetText(kingdomStabilityRelationTargetOffset) + "（作用于国王与本国非王族家族成年成员）");
		stringBuilder.AppendLine("本周叛乱概率：" + FormatKingdomRebellionChance(kingdomRebellionWeeklyChance));
		stringBuilder.AppendLine("当前国王：" + GetHeroDisplayName(kingdom.Leader));
		stringBuilder.AppendLine("执政家族：" + GetClanDisplayName(kingdom.RulingClan));
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(" ");
		if (list2.Count > 0)
		{
			stringBuilder.AppendLine("当前可自动叛乱候选：");
			foreach (KingdomRebellionCandidateInfo item in list2)
			{
				stringBuilder.AppendLine("- " + item.ClanName + " | 关系 " + item.RelationToKing + " | 城镇 " + item.TownCount + " | 城堡 " + item.CastleCount + " | 评分 " + item.Score.ToString("0.0"));
			}
		}
		else
		{
			stringBuilder.AppendLine("当前没有满足自动叛乱条件的家族。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevKingdomStabilityDetailMenu(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("set_value", "调整稳定度", null),
			new InquiryElement("test_roll", "测试本周叛乱判定", null),
			new InquiryElement("force_rebellion", "强制触发该王国叛乱", null),
			new InquiryElement("back", "返回王国列表", null)
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("王国稳定度详情 - " + GetKingdomDisplayName(kingdom, "王国"), BuildDevKingdomStabilityDetailText(kingdom), list, isExitShown: true, 0, 1, "执行", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevKingdomStabilityLabMenu();
				return;
			}
			switch (selected[0].Identifier as string)
			{
			case "set_value":
				OpenDevEditKingdomStability(kingdom);
				break;
			case "test_roll":
				RunDevKingdomRebellionTest(kingdom);
				break;
			case "force_rebellion":
				ConfirmForceDevKingdomRebellion(kingdom);
				break;
			default:
				OpenDevKingdomStabilityLabMenu();
				break;
			}
		}, delegate
		{
			OpenDevKingdomStabilityLabMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevEditKingdomStability(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int kingdomStabilityValue = GetKingdomStabilityValue(kingdom);
		string text = "请输入 0-100 之间的稳定度数值。\n\n当前值：" + kingdomStabilityValue + "（" + GetKingdomStabilityTierText(kingdomStabilityValue) + "）\n默认值：50（一般）\n\n档位：\n90-100 极高\n75-89 高\n60-74 较高\n40-59 一般\n25-39 较差\n10-24 很差\n0-9 极差";
		InformationManager.ShowTextInquiry(new TextInquiryData("调整稳定度 - " + GetKingdomDisplayName(kingdom, "王国"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			if (!int.TryParse((input ?? "").Trim(), out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入 0-100 的整数。"));
				OpenDevEditKingdomStability(kingdom);
				return;
			}
			result = ClampKingdomStabilityValue(result);
			SetKingdomStabilityValue(kingdom, result);
			InformationManager.DisplayMessage(new InformationMessage(GetKingdomDisplayName(kingdom, "王国") + " 的稳定度已更新为 " + result + "（" + GetKingdomStabilityTierText(result) + "）。"));
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}));
	}

	private static string BuildKingdomRebellionResolutionText(KingdomRebellionResolutionResult result)
	{
		if (result == null)
		{
			return "当前没有可显示的叛乱结果。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("王国：" + GetKingdomDisplayName(result.Kingdom, "某王国"));
		stringBuilder.AppendLine("稳定度：" + result.StabilityValue + "（" + (result.StabilityTierText ?? "未知") + "）");
		if (!result.Forced)
		{
			stringBuilder.AppendLine("本周叛乱概率：" + FormatKingdomRebellionChance(result.TriggerChance));
			if (result.Roll.HasValue)
			{
				stringBuilder.AppendLine("本次掷值：" + result.Roll.Value.ToString("0.000"));
			}
		}
		else
		{
			stringBuilder.AppendLine("本次模式：强制触发（跳过概率门槛）");
		}
		if (result.SelectedClan != null)
		{
			stringBuilder.AppendLine("选中的家族：" + GetClanDisplayName(result.SelectedClan));
		}
		if (result.SelectedFollowerClans != null && result.SelectedFollowerClans.Count > 0)
		{
			stringBuilder.AppendLine("预计联合响应家族：" + string.Join("、", result.SelectedFollowerClans.Select(GetClanDisplayName)));
		}
		if (!string.IsNullOrWhiteSpace(result.Message))
		{
			stringBuilder.AppendLine("结果：" + result.Message.Trim());
		}
		if (result.Candidates != null && result.Candidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("候选家族：");
			foreach (KingdomRebellionCandidateInfo item in result.Candidates.Take(8))
			{
				if (item == null)
				{
					continue;
				}
				stringBuilder.AppendLine("- " + item.ClanName + " | " + (item.Eligible ? "可触发" : "不可触发") + " | 关系 " + item.RelationToKing + " | 城镇 " + item.TownCount + " | 城堡 " + item.CastleCount + " | 评分 " + item.Score.ToString("0.0"));
				if (!string.IsNullOrWhiteSpace(item.Note))
				{
					stringBuilder.AppendLine("  " + item.Note.Trim());
				}
				if (item.PreviewFollowerClanNames != null && item.PreviewFollowerClanNames.Count > 0)
				{
					stringBuilder.AppendLine("  若由其主导叛乱，预计联合响应：" + string.Join("、", item.PreviewFollowerClanNames));
				}
				else
				{
					stringBuilder.AppendLine("  若由其主导叛乱，预计没有其他家族联合响应。");
				}
			}
		}
		if (result.FollowerCandidates != null && result.FollowerCandidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("联合响应候选：");
			foreach (KingdomRebellionFollowerInfo item2 in result.FollowerCandidates.Take(8))
			{
				if (item2 == null)
				{
					continue;
				}
				stringBuilder.AppendLine("- " + item2.ClanName + " | " + (item2.Eligible ? "会加入" : "不会加入") + " | 对国王 " + item2.RelationToKing + " | 对领袖 " + item2.RelationToLeader + " | 评分 " + item2.Score.ToString("0.0"));
				if (!string.IsNullOrWhiteSpace(item2.Note))
				{
					stringBuilder.AppendLine("  " + item2.Note.Trim());
				}
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void RunDevKingdomRebellionTest(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int num = Math.Max(1, GetCurrentGameDayIndexSafe() / 7);
		KingdomRebellionResolutionResult result = ResolveKingdomRebellion(kingdom, num, executeAction: false, forceTrigger: false);
		InformationManager.ShowInquiry(new InquiryData("叛乱测试结果 - " + GetKingdomDisplayName(kingdom, "王国"), BuildKingdomRebellionResolutionText(result), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回详情", "", delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, null));
	}

	private void ConfirmForceDevKingdomRebellion(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("强制触发王国叛乱", "这会跳过稳定度概率，但不会放宽关系条件；系统仍只会从当前满足自动叛乱条件的家族中，选择一个最适合带城反出的家族执行。\n\n如果当前没有满足关系条件的候选家族，本次将不会发生叛乱。\n\n该操作会真实改动存档中的王国格局，是否继续？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "强制执行", "取消", delegate
		{
			int num = Math.Max(1, GetCurrentGameDayIndexSafe() / 7);
			KingdomRebellionResolutionResult kingdomRebellionResolutionResult = ResolveKingdomRebellion(kingdom, num, executeAction: false, forceTrigger: true);
			Clan selectedClan = kingdomRebellionResolutionResult?.SelectedClan;
			KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = kingdomRebellionResolutionResult?.Candidates?.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Clan == selectedClan);
			if (selectedClan == null || kingdomRebellionCandidateInfo == null)
			{
				InformationManager.ShowInquiry(new InquiryData("强制叛乱结果 - " + GetKingdomDisplayName(kingdom, "王国"), BuildKingdomRebellionResolutionText(kingdomRebellionResolutionResult), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回详情", "", delegate
				{
					OpenDevKingdomStabilityDetailMenu(kingdom);
				}, null));
				return;
			}
			StartDevForcedKingdomRebellionAsync(kingdom, selectedClan, num, kingdomRebellionCandidateInfo.RelationToKing, kingdomRebellionCandidateInfo.TownCount, kingdomRebellionCandidateInfo.CastleCount, kingdomRebellionResolutionResult.SelectedFollowerClans);
		}, delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}));
	}

	private static List<Kingdom> GetDevEditableKingdoms()
	{
		try
		{
			return Kingdom.All.Where((Kingdom x) => x != null && !string.IsNullOrWhiteSpace(x.StringId)).OrderBy((Kingdom x) => x.Name?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((Kingdom x) => x.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
			return new List<Kingdom>();
		}
	}

	private static bool IsKingdomEligibleForWeeklyReport(Kingdom kingdom)
	{
		return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId);
	}

	private static bool IsKingdomEligibleForWeeklyReport(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return IsKingdomEligibleForWeeklyReport(FindKingdomById(text));
	}

	private static bool IsWeeklyReportGroupEligible(WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return false;
		}
		string text = (group.GroupKind ?? "").Trim();
		if (string.Equals(text, "world", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!string.Equals(text, "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return IsKingdomEligibleForWeeklyReport(group.KingdomId);
	}

	private string BuildDevKingdomSummaryLabel(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "无效王国";
		}
		string text = kingdom.Name?.ToString() ?? (kingdom.StringId ?? "王国");
		string kingdomOpeningSummary = GetKingdomOpeningSummary(kingdom);
		string text2 = string.IsNullOrWhiteSpace(kingdomOpeningSummary) ? "未设置" : "已设置";
		string devSummaryPreview = BuildDevSummaryPreview(kingdomOpeningSummary, 72);
		if (string.IsNullOrWhiteSpace(devSummaryPreview))
		{
			return text + " [" + text2 + "]";
		}
		return text + " [" + text2 + "] " + devSummaryPreview;
	}

	private static string BuildDevSummaryPreview(string text, int maxLen)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		bool flag = false;
		foreach (char c in text)
		{
			if (char.IsWhiteSpace(c))
			{
				if (!flag)
				{
					stringBuilder.Append(' ');
					flag = true;
				}
			}
			else
			{
				stringBuilder.Append(c);
				flag = false;
			}
		}
		string text2 = stringBuilder.ToString().Trim();
		if (text2.Length <= maxLen)
		{
			return text2;
		}
		return text2.Substring(0, Math.Max(1, maxLen)) + "...";
	}

	private string GetKingdomOpeningSummary(Kingdom kingdom)
	{
		if (kingdom == null || _eventKingdomOpeningSummaries == null)
		{
			return "";
		}
		string text = (kingdom.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (!_eventKingdomOpeningSummaries.TryGetValue(text, out var value))
		{
			return "";
		}
		return (value ?? "").Trim();
	}

	private void SaveKingdomOpeningSummary(Kingdom kingdom, string summary)
	{
		if (kingdom == null)
		{
			return;
		}
		if (_eventKingdomOpeningSummaries == null)
		{
			_eventKingdomOpeningSummaries = new Dictionary<string, string>();
		}
		string text = (kingdom.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = (summary ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			_eventKingdomOpeningSummaries.Remove(text);
		}
		else
		{
			_eventKingdomOpeningSummaries[text] = text2;
		}
	}

	private static Kingdom FindKingdomById(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Kingdom.All.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Clan FindClanById(string clanId)
	{
		string text = (clanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Clan.All.FirstOrDefault((Clan x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static List<EventRecordEntry> SanitizeEventRecordEntries(List<EventRecordEntry> source)
	{
		List<EventRecordEntry> list = new List<EventRecordEntry>();
		if (source == null)
		{
			return list;
		}
		foreach (EventRecordEntry item in source)
		{
			if (item == null)
			{
				continue;
			}
			string text = (item.EventId ?? "").Trim();
			string text2 = (item.Title ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			EventRecordEntry eventRecordEntry = new EventRecordEntry
			{
				EventId = text,
				WeekIndex = Math.Max(0, item.WeekIndex),
				EventKind = (item.EventKind ?? "").Trim(),
				ScopeKingdomId = (item.ScopeKingdomId ?? "").Trim(),
				Title = text2,
				ShortSummary = BuildFallbackWeeklyReportShortSummary(item.ShortSummary),
				Summary = (item.Summary ?? "").Trim(),
				TagText = NormalizeWeeklyReportTagText(item.TagText),
				PromptText = (item.PromptText ?? "").Trim(),
				CreatedDay = Math.Max(0, item.CreatedDay),
				CreatedDate = (item.CreatedDate ?? "").Trim(),
				Materials = new List<EventMaterialReference>()
			};
			if (string.IsNullOrWhiteSpace(eventRecordEntry.ShortSummary))
			{
				eventRecordEntry.ShortSummary = BuildFallbackWeeklyReportShortSummary(eventRecordEntry.Summary);
			}
			if (item.Materials != null)
			{
				foreach (EventMaterialReference material in item.Materials)
				{
					if (material == null)
					{
						continue;
					}
					eventRecordEntry.Materials.Add(new EventMaterialReference
					{
						MaterialType = (material.MaterialType ?? "").Trim(),
						Label = (material.Label ?? "").Trim(),
						SnapshotText = (material.SnapshotText ?? "").Trim(),
						HeroId = (material.HeroId ?? "").Trim(),
						KingdomId = (material.KingdomId ?? "").Trim(),
						SettlementId = (material.SettlementId ?? "").Trim(),
						RecentOnly = material.RecentOnly,
						ActionKind = (material.ActionKind ?? "").Trim(),
						ActorHeroId = (material.ActorHeroId ?? "").Trim(),
						ActorClanId = (material.ActorClanId ?? "").Trim(),
						ActorKingdomId = (material.ActorKingdomId ?? "").Trim(),
						TargetHeroId = (material.TargetHeroId ?? "").Trim(),
						TargetClanId = (material.TargetClanId ?? "").Trim(),
						TargetKingdomId = (material.TargetKingdomId ?? "").Trim(),
						SettlementOwnerClanId = (material.SettlementOwnerClanId ?? "").Trim(),
						SettlementOwnerKingdomId = (material.SettlementOwnerKingdomId ?? "").Trim(),
						PreviousSettlementOwnerClanId = (material.PreviousSettlementOwnerClanId ?? "").Trim(),
						PreviousSettlementOwnerKingdomId = (material.PreviousSettlementOwnerKingdomId ?? "").Trim(),
						RelatedHeroIds = new List<string>((material.RelatedHeroIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						RelatedClanIds = new List<string>((material.RelatedClanIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						RelatedKingdomIds = new List<string>((material.RelatedKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						SourceStableKeys = new List<string>((material.SourceStableKeys ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						SourceActionKinds = new List<string>((material.SourceActionKinds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
						SourceMaterialCount = Math.Max(0, material.SourceMaterialCount),
						ActionStableKey = (material.ActionStableKey ?? "").Trim(),
						ActionDay = material.ActionDay,
						ActionOrder = material.ActionOrder,
						ActionSequence = material.ActionSequence
					});
				}
			}
			list.Add(eventRecordEntry);
		}
		return list.OrderByDescending((EventRecordEntry x) => x.WeekIndex).ThenByDescending((EventRecordEntry x) => x.CreatedDay).ThenBy((EventRecordEntry x) => x.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials(List<EventSourceMaterialEntry> source)
	{
		List<EventSourceMaterialEntry> list = new List<EventSourceMaterialEntry>();
		if (source == null)
		{
			return list;
		}
		foreach (EventSourceMaterialEntry item in source)
		{
			string text = (item?.SnapshotText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (item == null || string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			list.Add(new EventSourceMaterialEntry
			{
				Day = Math.Max(0, item.Day),
				Sequence = Math.Max(0, item.Sequence),
				GameDate = (item.GameDate ?? "").Trim(),
				MaterialKind = (item.MaterialKind ?? "").Trim(),
				Label = (item.Label ?? "").Trim(),
				SnapshotText = text,
				StableKey = NormalizeNpcActionStableKey(item.StableKey, text),
				KingdomId = (item.KingdomId ?? "").Trim(),
				SettlementId = (item.SettlementId ?? "").Trim(),
				IncludeInWorld = item.IncludeInWorld,
				IncludeInKingdom = item.IncludeInKingdom
			});
		}
		return list.OrderBy((EventSourceMaterialEntry x) => x.Day).ThenBy((EventSourceMaterialEntry x) => (x.Sequence > 0) ? x.Sequence : int.MaxValue).ThenBy((EventSourceMaterialEntry x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void OpenDevEventViewerMenu(int page)
	{
		List<EventRecordEntry> list = SanitizeEventRecordEntries(_eventRecordEntries);
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 14;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", "下一页", null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventRecordEntry item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, BuildDevEventRecordMenuLabel(item), null));
		}
		string text = BuildDevEventViewerDescription(list, page, num);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件查看器", text, list2, isExitShown: true, 0, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text2)
			{
				switch (text2)
				{
				case "back":
					OpenDevEventEditorMenu();
					break;
				case "prev_page":
					OpenDevEventViewerMenu(page - 1);
					break;
				case "next_page":
					OpenDevEventViewerMenu(page + 1);
					break;
				default:
					OpenDevEventViewerMenu(page);
					break;
				}
			}
			else if (selected[0].Identifier is EventRecordEntry eventRecordEntry)
			{
				OpenDevEventRecordDetail(eventRecordEntry, page);
			}
			else
			{
				OpenDevEventViewerMenu(page);
			}
		}, delegate
		{
			OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildDevEventViewerDescription(List<EventRecordEntry> entries, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里用于查看事件系统已经记录下来的事件，以及每条事件引用了哪些素材。");
		stringBuilder.AppendLine("素材可能包括：世界开局概要、王国开局概要、NPC近期行动、NPC重大行动，以及未来接入的其他摘要。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("事件总数：" + ((entries != null) ? entries.Count : 0));
		stringBuilder.AppendLine("页码：" + (page + 1) + "/" + Math.Max(1, totalPages));
		if (entries == null || entries.Count == 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("当前还没有事件记录。");
			stringBuilder.AppendLine("后续周事件系统接入后，每条事件都会显示在这里，并能展开查看引用素材。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildDevEventRecordItemLabel(EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "无效事件";
		}
		string text = TranslateEventKindForDev(entry.EventKind);
		string text2 = string.IsNullOrWhiteSpace(entry.CreatedDate) ? ("第 " + Math.Max(0, entry.CreatedDay) + " 日") : entry.CreatedDate.Trim();
		string text3 = string.IsNullOrWhiteSpace(entry.ScopeKingdomId) ? "" : ResolveKingdomDisplay(entry.ScopeKingdomId);
		string text4 = string.IsNullOrWhiteSpace(text3) ? "" : (" [" + text3 + "]");
		int count = (entry.Materials != null) ? entry.Materials.Count : 0;
		return text2 + " [" + text + "] 第" + Math.Max(0, entry.WeekIndex) + "周" + text4 + " " + (entry.Title ?? "").Trim() + " (素材 " + count + " 条)";
	}

	private static string BuildDevEventRecordMenuLabel(EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "无效事件";
		}
		bool isWorld = string.Equals((entry.EventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		string kingdomName = ResolveKingdomDisplay(entry.ScopeKingdomId);
		if (isWorld || string.IsNullOrWhiteSpace(kingdomName))
		{
			return "世界周报";
		}
		return kingdomName;
	}

	private void OpenDevEventRecordDetail(EventRecordEntry entry, int returnPage)
	{
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回事件列表", null));
		if (!string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			list2.Add(new InquiryElement("view_short", "查看短摘要", null));
		}
		list2.Add(new InquiryElement("view_report", "查看周报正文", null));
		list2.Add(new InquiryElement("edit_title", "编辑标题", null));
		list2.Add(new InquiryElement("edit_report", "编辑周报正文", null));
		if (!string.IsNullOrWhiteSpace(entry.TagText))
		{
			list2.Add(new InquiryElement("view_tags", "查看标签层", null));
		}
		if (!string.IsNullOrWhiteSpace(entry.PromptText))
		{
			list2.Add(new InquiryElement("view_prompt", "查看请求 Prompt", null));
		}
		if ((entry.Materials?.Count).GetValueOrDefault() > 0)
		{
			list2.Add(new InquiryElement("view_materials", "查看素材列表", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件详情", BuildDevEventRecordCompactDetailText(entry), list2, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventViewerMenu(returnPage);
			}
			else if (selected[0].Identifier is string text)
			{
				if (text == "back")
				{
					OpenDevEventViewerMenu(returnPage);
				}
				else if (text == "edit_title")
				{
					OpenDevEditEventRecordTitle(entry, returnPage);
				}
				else if (text == "view_report")
				{
					OpenDevEventReportDetail(entry, returnPage);
				}
				else if (text == "view_short")
				{
					OpenDevEventShortSummaryDetail(entry, returnPage);
				}
				else if (text == "edit_report")
				{
					OpenDevEditEventRecordReport(entry, returnPage);
				}
				else if (text == "view_tags")
				{
					OpenDevEventTagDetail(entry, returnPage);
				}
				else if (text == "view_prompt")
				{
					OpenDevEventPromptDetail(entry, returnPage);
				}
				else if (text == "view_materials")
				{
					OpenDevEventMaterialList(entry, returnPage, 0);
				}
				else
				{
					OpenDevEventRecordDetail(entry, returnPage);
				}
			}
			else
			{
				OpenDevEventRecordDetail(entry, returnPage);
			}
		}, delegate
		{
			OpenDevEventViewerMenu(returnPage);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildDevEventRecordDetailText(EventRecordEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "事件标题", (entry.Title ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "事件类型", TranslateEventKindForDev(entry.EventKind));
		AppendDevNpcActionField(stringBuilder, "周数", "第 " + Math.Max(0, entry.WeekIndex) + " 周");
		AppendDevNpcActionField(stringBuilder, "生成日期", !string.IsNullOrWhiteSpace(entry.CreatedDate) ? entry.CreatedDate.Trim() : ("第 " + Math.Max(0, entry.CreatedDay) + " 日"));
		AppendDevNpcActionField(stringBuilder, "归属王国", ResolveKingdomDisplay(entry.ScopeKingdomId));
		AppendDevNpcActionField(stringBuilder, "素材数量", ((entry.Materials != null) ? entry.Materials.Count : 0).ToString());
		AppendDevNpcActionField(stringBuilder, "短摘要", string.IsNullOrWhiteSpace(entry.ShortSummary) ? "未保存" : BuildDevSummaryPreview(entry.ShortSummary, 48));
		AppendDevNpcActionField(stringBuilder, "标签层", string.IsNullOrWhiteSpace(entry.TagText) ? "未保存" : "已保存");
		AppendDevNpcActionField(stringBuilder, "Prompt", string.IsNullOrWhiteSpace(entry.PromptText) ? "未保存" : "已保存");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【操作说明】");
		stringBuilder.AppendLine("短摘要、周报正文、标签层、Prompt 和素材列表都已拆分为单独入口，不再直接铺在这一页。");
		if (string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			stringBuilder.AppendLine("当前这条事件还没有短摘要。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已有短摘要，可通过“查看短摘要”进入。");
		}
		if (string.IsNullOrWhiteSpace(entry.Summary))
		{
			stringBuilder.AppendLine("当前这条事件还没有正文。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已有正文，可通过“查看周报正文”或“编辑周报正文”进入。");
		}
		if (string.IsNullOrWhiteSpace(entry.TagText))
		{
			stringBuilder.AppendLine("当前这条事件还没有标签层。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已有标签层，可通过“查看标签层”进入。");
		}
		if (entry.Materials == null || entry.Materials.Count == 0)
		{
			stringBuilder.AppendLine("当前这条事件还没有挂载任何素材引用。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已挂载素材，可通过“查看素材列表”进入。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private string BuildDevEventRecordCompactDetailText(EventRecordEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "事件标题", (entry?.Title ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "事件类型", TranslateEventKindForDev(entry?.EventKind));
		AppendDevNpcActionField(stringBuilder, "周数", "第" + Math.Max(0, entry?.WeekIndex ?? 0) + " 周");
		AppendDevNpcActionField(stringBuilder, "生成日期", !string.IsNullOrWhiteSpace(entry?.CreatedDate) ? entry.CreatedDate.Trim() : ("第" + Math.Max(0, entry?.CreatedDay ?? 0) + " 日"));
		AppendDevNpcActionField(stringBuilder, "归属王国", ResolveKingdomDisplay(entry?.ScopeKingdomId));
		AppendDevNpcActionField(stringBuilder, "素材数量", ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		AppendDevNpcActionField(stringBuilder, "短摘要", string.IsNullOrWhiteSpace(entry?.ShortSummary) ? "未保存" : BuildDevSummaryPreview(entry.ShortSummary, 48));
		AppendDevNpcActionField(stringBuilder, "标签层", string.IsNullOrWhiteSpace(entry?.TagText) ? "未保存" : "已保存");
		AppendDevNpcActionField(stringBuilder, "Prompt", string.IsNullOrWhiteSpace(entry?.PromptText) ? "未保存" : "已保存");
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevEventReportDetail(EventRecordEntry entry, int returnPage)
	{
		InformationManager.ShowInquiry(new InquiryData("周报正文 - " + ((entry?.Title ?? "").Trim()), string.IsNullOrWhiteSpace(entry?.Summary) ? "当前这条事件还没有正文。" : entry.Summary.Trim(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回事件详情", "", delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, null));
	}

	private void OpenDevEventShortSummaryDetail(EventRecordEntry entry, int returnPage)
	{
		InformationManager.ShowInquiry(new InquiryData("短摘要 - " + ((entry?.Title ?? "").Trim()), string.IsNullOrWhiteSpace(entry?.ShortSummary) ? "当前这条事件还没有短摘要。" : entry.ShortSummary.Trim(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回事件详情", "", delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, null));
	}

	private void OpenDevEventTagDetail(EventRecordEntry entry, int returnPage)
	{
		InformationManager.ShowInquiry(new InquiryData("标签层 - " + ((entry?.Title ?? "").Trim()), string.IsNullOrWhiteSpace(entry?.TagText) ? "当前这条事件还没有标签层。" : entry.TagText.Trim(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回事件详情", "", delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, null));
	}

	private void OpenDevEventMaterialList(EventRecordEntry entry, int returnPage, int page)
	{
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		List<EventMaterialReference> list = entry.Materials ?? new List<EventMaterialReference>();
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 14;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回事件详情", null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", "下一页", null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventMaterialReference item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, BuildDevEventMaterialItemLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件素材列表", BuildDevEventMaterialListText(entry, page, num), list2, isExitShown: true, 0, 1, "查看素材", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventRecordDetail(entry, returnPage);
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "back":
					OpenDevEventRecordDetail(entry, returnPage);
					break;
				case "prev_page":
					OpenDevEventMaterialList(entry, returnPage, page - 1);
					break;
				case "next_page":
					OpenDevEventMaterialList(entry, returnPage, page + 1);
					break;
				default:
					OpenDevEventMaterialList(entry, returnPage, page);
					break;
				}
			}
			else if (selected[0].Identifier is EventMaterialReference eventMaterialReference)
			{
				OpenDevEventMaterialDetail(entry, eventMaterialReference, returnPage, page);
			}
			else
			{
				OpenDevEventMaterialList(entry, returnPage, page);
			}
		}, delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildDevEventMaterialListText(EventRecordEntry entry, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "事件标题", (entry?.Title ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "素材数量", ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		AppendDevNpcActionField(stringBuilder, "页码", (page + 1) + "/" + Math.Max(1, totalPages));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("下方列表中的每一项，都是这条周报在生成时引用过的素材。");
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevEditEventRecordTitle(EventRecordEntry entry, int returnPage)
	{
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		DevTextEditorHelper.ShowLongTextEditor("编辑周报标题", "修改当前这条周报记录的标题。", "请输入新的标题（留空将回退为默认标题）。", entry.Title ?? "", delegate(string input)
		{
			entry.Title = (input ?? "").Trim();
			if (string.IsNullOrWhiteSpace(entry.Title))
			{
				entry.Title = BuildDefaultWeeklyReportTitle(new WeeklyEventMaterialPreviewGroup
				{
					GroupKind = entry.EventKind,
					KingdomId = entry.ScopeKingdomId
				}, entry.WeekIndex);
			}
			_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
			InformationManager.DisplayMessage(new InformationMessage("周报标题已更新。"));
			OpenDevEventRecordDetail(FindEventRecordById(entry.EventId) ?? entry, returnPage);
		}, delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		});
	}

	private void OpenDevEditEventRecordReport(EventRecordEntry entry, int returnPage)
	{
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		DevTextEditorHelper.ShowLongTextEditor("编辑周报正文", "这里修改的是最终展示出来的周报正文，不会改动原始素材。", "请输入新的周报正文。", entry.Summary ?? "", delegate(string input)
		{
			entry.Summary = (input ?? "").Trim();
			_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
			InformationManager.DisplayMessage(new InformationMessage("周报正文已更新。"));
			OpenDevEventRecordDetail(FindEventRecordById(entry.EventId) ?? entry, returnPage);
		}, delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		});
	}

	private void OpenDevEventPromptDetail(EventRecordEntry entry, int returnPage)
	{
		InformationManager.ShowInquiry(new InquiryData("请求 Prompt - " + ((entry?.Title ?? "").Trim()), string.IsNullOrWhiteSpace(entry?.PromptText) ? "当前没有保存该条周报的请求 Prompt。" : entry.PromptText.Trim(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回事件详情", "", delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, null));
	}

	private static string BuildDevEventMaterialItemLabel(EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材";
		}
		string text = TranslateEventMaterialTypeForDev(material.MaterialType);
		string text2 = BuildDevSummaryPreview(material.Label, 56);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = BuildDevSummaryPreview(material.SnapshotText, 56);
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "无预览";
		}
		return "[" + text + "] " + text2;
	}

	private void OpenDevEventMaterialDetail(EventRecordEntry entry, EventMaterialReference material, int returnPage, int returnMaterialPage)
	{
		string text = BuildDevEventMaterialDetailText(material);
		string text2 = BuildDevEventMaterialItemLabel(material);
		InformationManager.ShowInquiry(new InquiryData("素材详情 - " + text2, text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回事件详情", "", delegate
		{
			OpenDevEventMaterialList(entry, returnPage, returnMaterialPage);
		}, null));
	}

	private string BuildDevEventMaterialDetailText(EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "素材类型", TranslateEventMaterialTypeForDev(material.MaterialType));
		AppendDevNpcActionField(stringBuilder, "素材标签", (material.Label ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "人物", ResolveHeroDisplay(material.HeroId));
		AppendDevNpcActionField(stringBuilder, "王国", ResolveKingdomDisplay(material.KingdomId));
		AppendDevNpcActionField(stringBuilder, "定居点", ResolveSettlementDisplay(material.SettlementId));
		AppendDevNpcActionField(stringBuilder, "行动类型", GetDevNpcActionKindDisplay(material.ActionKind));
		AppendDevNpcActionField(stringBuilder, "相关人物", string.Join("、", ResolveHeroNames(material.RelatedHeroIds)));
		AppendDevNpcActionField(stringBuilder, "相关家族", string.Join("、", ResolveClanNames(material.RelatedClanIds)));
		AppendDevNpcActionField(stringBuilder, "相关王国", string.Join("、", ResolveKingdomNames(material.RelatedKingdomIds)));
		AppendDevNpcActionField(stringBuilder, "原始素材数", Math.Max(0, material.SourceMaterialCount).ToString());
		AppendDevNpcActionField(stringBuilder, "来源StableKey", string.Join(" | ", (material.SourceStableKeys ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())));
		AppendDevNpcActionField(stringBuilder, "来源ActionKind", string.Join(" | ", (material.SourceActionKinds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())));
		switch ((material.MaterialType ?? "").Trim().ToLowerInvariant())
		{
		case "world_opening_summary":
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine(!string.IsNullOrWhiteSpace(material.SnapshotText) ? material.SnapshotText.Trim() : ((_eventWorldOpeningSummary ?? "").Trim()));
			break;
		case "kingdom_opening_summary":
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine(!string.IsNullOrWhiteSpace(material.SnapshotText) ? material.SnapshotText.Trim() : ResolveKingdomOpeningSummaryById(material.KingdomId));
			break;
		case "npc_recent_action":
		case "npc_major_action":
			NpcActionEntry npcActionEntry = ResolveEventMaterialNpcAction(material);
			if (npcActionEntry != null)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(BuildDevNpcActionDetailText(npcActionEntry));
			}
			else
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("【素材正文】");
				if (!string.IsNullOrWhiteSpace(material.SnapshotText))
				{
					stringBuilder.AppendLine(material.SnapshotText.Trim());
				}
				else
				{
					stringBuilder.AppendLine("未能在当前行动记录中定位到这条 NPC 行为，可能是旧记录被裁剪掉了。");
				}
			}
			break;
		default:
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(material.SnapshotText) ? "这条素材当前没有额外快照文本。" : material.SnapshotText.Trim());
			break;
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private string ResolveKingdomOpeningSummaryById(string kingdomId)
	{
		Kingdom kingdom = FindKingdomById(kingdomId);
		return (kingdom == null) ? "" : GetKingdomOpeningSummary(kingdom);
	}

	private EventRecordEntry FindEventRecordById(string eventId)
	{
		string text = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return SanitizeEventRecordEntries(_eventRecordEntries).FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

	private bool TryLoadEventDataFromImportDir(string importDir, out EventImportPayload payload, out string error)
	{
		payload = new EventImportPayload();
		error = "";
		try
		{
			string text = Path.Combine(importDir, "event_data");
			string text2 = Directory.Exists(text) ? text : importDir;
			string path = Path.Combine(text2, "WorldOpeningSummary.json");
			if (File.Exists(path))
			{
				payload.HasWorldSummaryFile = true;
				EventWorldOpeningSummaryJson eventWorldOpeningSummaryJson = ReadJson<EventWorldOpeningSummaryJson>(path);
				payload.WorldSummary = (eventWorldOpeningSummaryJson?.Summary ?? "").Trim();
			}
			string path2 = Path.Combine(text2, "KingdomOpeningSummaries.json");
			if (File.Exists(path2))
			{
				payload.HasKingdomSummariesFile = true;
				Dictionary<string, string> dictionary = ReadJson<Dictionary<string, string>>(path2) ?? new Dictionary<string, string>();
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					string text3 = (item.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						payload.KingdomSummaries[text3] = (item.Value ?? "").Trim();
					}
				}
			}
			string path3 = Path.Combine(text2, "EventRecords.json");
			if (File.Exists(path3))
			{
				payload.HasEventRecordsFile = true;
				List<EventRecordEntry> source = ReadJson<List<EventRecordEntry>>(path3) ?? new List<EventRecordEntry>();
				payload.EventRecords = SanitizeEventRecordEntries(source);
			}
			if (!payload.HasWorldSummaryFile && !payload.HasKingdomSummariesFile && !payload.HasEventRecordsFile)
			{
				error = "找不到 event_data\\WorldOpeningSummary.json、event_data\\KingdomOpeningSummaries.json 或 event_data\\EventRecords.json。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private void ApplyImportedEventData(EventImportPayload payload, bool overwriteExisting)
	{
		if (payload == null)
		{
			return;
		}
		if (payload.HasWorldSummaryFile && (overwriteExisting || string.IsNullOrWhiteSpace(_eventWorldOpeningSummary)))
		{
			_eventWorldOpeningSummary = (payload.WorldSummary ?? "").Trim();
		}
		if (payload.HasKingdomSummariesFile)
		{
			if (_eventKingdomOpeningSummaries == null)
			{
				_eventKingdomOpeningSummaries = new Dictionary<string, string>();
			}
			foreach (KeyValuePair<string, string> item in payload.KingdomSummaries)
			{
				string text = (item.Key ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string text2 = (item.Value ?? "").Trim();
				if (overwriteExisting)
				{
					if (string.IsNullOrWhiteSpace(text2))
					{
						_eventKingdomOpeningSummaries.Remove(text);
					}
					else
					{
						_eventKingdomOpeningSummaries[text] = text2;
					}
				}
				else if (!_eventKingdomOpeningSummaries.ContainsKey(text) && !string.IsNullOrWhiteSpace(text2))
				{
					_eventKingdomOpeningSummaries[text] = text2;
				}
			}
		}
		if (payload.HasEventRecordsFile)
		{
			if (_eventRecordEntries == null)
			{
				_eventRecordEntries = new List<EventRecordEntry>();
			}
			if (overwriteExisting)
			{
				Dictionary<string, EventRecordEntry> dictionary = new Dictionary<string, EventRecordEntry>(StringComparer.OrdinalIgnoreCase);
				foreach (EventRecordEntry eventRecordEntry in _eventRecordEntries)
				{
					string text3 = (eventRecordEntry?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						dictionary[text3] = eventRecordEntry;
					}
				}
				foreach (EventRecordEntry eventRecordEntry2 in payload.EventRecords)
				{
					string text4 = (eventRecordEntry2?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						dictionary[text4] = eventRecordEntry2;
					}
				}
				_eventRecordEntries = SanitizeEventRecordEntries(dictionary.Values.ToList());
			}
			else
			{
				HashSet<string> hashSet = new HashSet<string>(_eventRecordEntries.Where((EventRecordEntry x) => x != null && !string.IsNullOrWhiteSpace(x.EventId)).Select((EventRecordEntry x) => x.EventId.Trim()), StringComparer.OrdinalIgnoreCase);
				foreach (EventRecordEntry eventRecordEntry3 in payload.EventRecords)
				{
					string text5 = (eventRecordEntry3?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5) && hashSet.Add(text5))
					{
						_eventRecordEntries.Add(eventRecordEntry3);
					}
				}
				_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
			}
		}
	}

	private NpcActionEntry ResolveEventMaterialNpcAction(EventMaterialReference material)
	{
		if (material == null || string.IsNullOrWhiteSpace(material.HeroId))
		{
			return null;
		}
		Hero hero = Hero.FindFirst((Hero h) => h != null && string.Equals((h.StringId ?? "").Trim(), (material.HeroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		if (hero == null)
		{
			return null;
		}
		List<NpcActionEntry> devNpcActionEntries = GetDevNpcActionEntries(hero, material.RecentOnly);
		if (devNpcActionEntries == null || devNpcActionEntries.Count == 0)
		{
			return null;
		}
		NpcActionEntry npcActionEntry = devNpcActionEntries.FirstOrDefault((NpcActionEntry x) => x != null && (!material.ActionDay.HasValue || x.Day == material.ActionDay.Value) && (!material.ActionOrder.HasValue || x.Order == material.ActionOrder.Value) && (string.IsNullOrWhiteSpace(material.ActionStableKey) || string.Equals((x.StableKey ?? "").Trim(), material.ActionStableKey.Trim(), StringComparison.OrdinalIgnoreCase)));
		if (npcActionEntry != null)
		{
			return npcActionEntry;
		}
		if (!string.IsNullOrWhiteSpace(material.ActionStableKey))
		{
			npcActionEntry = devNpcActionEntries.FirstOrDefault((NpcActionEntry x) => x != null && string.Equals((x.StableKey ?? "").Trim(), material.ActionStableKey.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		return npcActionEntry;
	}

	private static string TranslateEventKindForDev(string eventKind)
	{
		switch ((eventKind ?? "").Trim().ToLowerInvariant())
		{
		case "world":
			return "世界事件";
		case "kingdom":
			return "王国事件";
		case "player_local":
			return "玩家周边事件";
		default:
			return string.IsNullOrWhiteSpace(eventKind) ? "未分类事件" : eventKind.Trim();
		}
	}

	private static string TranslateEventMaterialTypeForDev(string materialType)
	{
		string text = (materialType ?? "").Trim().ToLowerInvariant();
		if (text.StartsWith("prompt_agg_", StringComparison.OrdinalIgnoreCase))
		{
			return "周报聚合素材";
		}
		switch (text)
		{
		case "world_opening_summary":
			return "世界开局概要";
		case "kingdom_opening_summary":
			return "王国开局概要";
		case "npc_recent_action":
			return "NPC近期行动";
		case "npc_major_action":
			return "NPC重大行动";
		case "raw_text":
			return "原始文本素材";
		default:
			return string.IsNullOrWhiteSpace(materialType) ? "未分类素材" : materialType.Trim();
		}
	}

	private static string ResolveSettlementDisplay(string settlementId)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Settlement settlement = Settlement.Find(text);
			if (settlement != null)
			{
				string text2 = settlement.Name?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2;
				}
			}
		}
		catch
		{
		}
		return text;
	}

	private void OpenDevWeeklyEventMaterialPreviewMenu()
	{
		List<WeeklyEventMaterialPreviewGroup> list = BuildWeeklyEventMaterialPreviewGroups();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			list2.Add(new InquiryElement(item, BuildWeeklyEventMaterialPreviewGroupLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周事件素材预览", BuildWeeklyEventMaterialPreviewMenuDescription(list), list2, isExitShown: true, 0, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup)
			{
				OpenDevWeeklyEventMaterialPreviewGroupDetail(weeklyEventMaterialPreviewGroup, 0);
			}
			else
			{
				OpenDevWeeklyEventMaterialPreviewMenu();
			}
		}, delegate
		{
			OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildWeeklyEventMaterialPreviewMenuDescription(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		int num3 = (groups != null) ? groups.Sum((WeeklyEventMaterialPreviewGroup x) => (x?.Materials?.Count).GetValueOrDefault()) : 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示“如果现在生成本周事件”，系统会拿去喂给事件生成器的素材池。");
		stringBuilder.AppendLine("当前按世界事件与各王国事件分组展示。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("当前周数：第 " + num2 + " 周");
		stringBuilder.AppendLine("当前取材区间：第 " + num + " 日 到 第 " + currentGameDayIndexSafe + " 日");
		stringBuilder.AppendLine("分组数量：" + ((groups != null) ? groups.Count : 0));
		stringBuilder.AppendLine("素材总数：" + num3);
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("当前已接入的素材：世界开局概要、王国开局概要、本周 NPC 行动。");
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildWeeklyEventMaterialPreviewGroupLabel(WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return "无效分组";
		}
		string text = string.IsNullOrWhiteSpace(group.Summary) ? "" : BuildDevSummaryPreview(group.Summary, 44);
		string text2 = ((group.Materials != null) ? group.Materials.Count : 0) + " 条素材";
		return (group.Title ?? "未命名分组") + " [" + text2 + "]" + (string.IsNullOrWhiteSpace(text) ? "" : (" " + text));
	}

	private List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups()
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		return BuildWeeklyEventMaterialPreviewGroups(num, currentGameDayIndexSafe);
	}

	private List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups(int startDay, int endDay)
	{
		List<WeeklyEventMaterialPreviewGroup> list = new List<WeeklyEventMaterialPreviewGroup>();
		list.Add(BuildWorldWeeklyEventMaterialPreviewGroup(startDay, endDay));
		foreach (Kingdom devEditableKingdom in GetDevEditableKingdoms())
		{
			if (!IsKingdomEligibleForWeeklyReport(devEditableKingdom))
			{
				continue;
			}
			WeeklyEventMaterialPreviewGroup item = BuildKingdomWeeklyEventMaterialPreviewGroup(devEditableKingdom, startDay, endDay);
			list.Add(item);
		}
		foreach (WeeklyEventMaterialPreviewGroup item2 in list)
		{
			ApplyWeeklyPromptMaterialAggregation(item2);
		}
		return list;
	}

	private void ApplyWeeklyPromptMaterialAggregation(WeeklyEventMaterialPreviewGroup group)
	{
		if (group?.Materials == null || group.Materials.Count == 0)
		{
			return;
		}
		List<EventMaterialReference> list = OrderWeeklyPreviewMaterials(group.Materials).Where((EventMaterialReference x) => x != null).Select(CloneEventMaterialReference).ToList();
		List<EventMaterialReference> list2 = list.Where((EventMaterialReference x) => !IsWeeklyPromptAggregatableMaterial(x)).ToList();
		List<EventMaterialReference> list3 = list.Where(IsWeeklyPromptAggregatableMaterial).ToList();
		if (list3.Count == 0)
		{
			group.Materials = list2;
			return;
		}
		Dictionary<string, Dictionary<string, List<EventMaterialReference>>> dictionary = new Dictionary<string, Dictionary<string, List<EventMaterialReference>>>(StringComparer.OrdinalIgnoreCase);
		foreach (EventMaterialReference item in list3)
		{
			string text = ResolveWeeklyPromptAggregateCategory(item);
			string text2 = BuildWeeklyPromptAggregateEventKey(item);
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = new Dictionary<string, List<EventMaterialReference>>(StringComparer.OrdinalIgnoreCase);
				dictionary[text] = value;
			}
			if (!value.TryGetValue(text2, out var value2))
			{
				value2 = new List<EventMaterialReference>();
				value[text2] = value2;
			}
			value2.Add(item);
		}
		List<EventMaterialReference> list4 = new List<EventMaterialReference>();
		foreach (string item2 in GetWeeklyPromptAggregateCategoryOrder())
		{
			if (dictionary.TryGetValue(item2, out var value3))
			{
				EventMaterialReference eventMaterialReference = BuildWeeklyPromptAggregateCategoryMaterial(group, item2, value3);
				if (eventMaterialReference != null)
				{
					list4.Add(eventMaterialReference);
				}
			}
		}
		group.Materials = OrderWeeklyPreviewMaterials(list2.Concat(list4).ToList()).ToList();
	}

	private static EventMaterialReference CloneEventMaterialReference(EventMaterialReference material)
	{
		if (material == null)
		{
			return null;
		}
		return new EventMaterialReference
		{
			MaterialType = (material.MaterialType ?? "").Trim(),
			Label = (material.Label ?? "").Trim(),
			SnapshotText = (material.SnapshotText ?? "").Trim(),
			HeroId = (material.HeroId ?? "").Trim(),
			KingdomId = (material.KingdomId ?? "").Trim(),
			SettlementId = (material.SettlementId ?? "").Trim(),
			RecentOnly = material.RecentOnly,
			ActionKind = (material.ActionKind ?? "").Trim(),
			ActorHeroId = (material.ActorHeroId ?? "").Trim(),
			ActorClanId = (material.ActorClanId ?? "").Trim(),
			ActorKingdomId = (material.ActorKingdomId ?? "").Trim(),
			TargetHeroId = (material.TargetHeroId ?? "").Trim(),
			TargetClanId = (material.TargetClanId ?? "").Trim(),
			TargetKingdomId = (material.TargetKingdomId ?? "").Trim(),
			SettlementOwnerClanId = (material.SettlementOwnerClanId ?? "").Trim(),
			SettlementOwnerKingdomId = (material.SettlementOwnerKingdomId ?? "").Trim(),
			PreviousSettlementOwnerClanId = (material.PreviousSettlementOwnerClanId ?? "").Trim(),
			PreviousSettlementOwnerKingdomId = (material.PreviousSettlementOwnerKingdomId ?? "").Trim(),
			RelatedHeroIds = new List<string>((material.RelatedHeroIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedClanIds = new List<string>((material.RelatedClanIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedKingdomIds = new List<string>((material.RelatedKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceStableKeys = new List<string>((material.SourceStableKeys ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceActionKinds = new List<string>((material.SourceActionKinds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceMaterialCount = Math.Max(0, material.SourceMaterialCount),
			ActionStableKey = (material.ActionStableKey ?? "").Trim(),
			ActionDay = material.ActionDay,
			ActionOrder = material.ActionOrder,
			ActionSequence = material.ActionSequence
		};
	}

	private static bool IsWeeklyPromptAggregatableMaterial(EventMaterialReference material)
	{
		if (material == null)
		{
			return false;
		}
		string text = (material.MaterialType ?? "").Trim();
		return string.Equals(text, "npc_recent_action", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "npc_major_action", StringComparison.OrdinalIgnoreCase);
	}

	private static List<string> GetWeeklyPromptAggregateCategoryOrder()
	{
		return new List<string>
		{
			"strategic_shift",
			"decision",
			"siege",
			"settlement_change",
			"captivity",
			"release",
			"army",
			"battle",
			"movement",
			"other"
		};
	}

	private static string ResolveWeeklyPromptAggregateCategory(EventMaterialReference material)
	{
		string text = (material?.ActionKind ?? "").Trim().ToLowerInvariant();
		switch (text)
		{
		case "clan_changed_kingdom":
		case "clan_defected":
			return "strategic_shift";
		case "kingdom_decision_concluded":
			return "decision";
		case "siege_start_attack":
		case "siege_start_defend":
		case "siege_join":
		case "siege_leave":
		case "siege_end_attack":
		case "siege_end_defend":
		case "siege_complete":
			return "siege";
		case "settlement_owner_changed_gain":
		case "settlement_owner_changed_loss":
		case "settlement_owner_changed_capture":
			return "settlement_change";
		case "prisoner_taken_captor":
		case "prisoner_taken_prisoner":
			return "captivity";
		case "prisoner_released_captor":
		case "prisoner_released_prisoner":
			return "release";
		case "army_create":
		case "army_gather":
		case "army_disperse":
		case "army_join":
		case "army_leave":
			return "army";
		case "map_event":
		case "map_event_aftermath":
			return "battle";
		case "daily_behavior":
			return "movement";
		default:
			return "other";
		}
	}

	private static string BuildWeeklyPromptAggregateEventKey(EventMaterialReference material)
	{
		string text = NormalizeWeeklyPromptAggregateStableKey(material?.ActionStableKey);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = ((material?.ActionKind ?? "").Trim().ToLowerInvariant()) + "|" + ((material?.SettlementId ?? "").Trim().ToLowerInvariant()) + "|" + ((material?.KingdomId ?? "").Trim().ToLowerInvariant()) + "|" + ((material?.HeroId ?? "").Trim().ToLowerInvariant());
		return text.Trim();
	}

	private static string NormalizeWeeklyPromptAggregateStableKey(string stableKey)
	{
		string text = (stableKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = new string[6] { ":captor", ":prisoner", ":gain", ":loss", ":capture", ":chooser" };
		foreach (string value in array)
		{
			if (text.EndsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return text.Substring(0, text.Length - value.Length);
			}
		}
		if (text.EndsWith(":proposer", StringComparison.OrdinalIgnoreCase))
		{
			return text.Substring(0, text.Length - ":proposer".Length);
		}
		return text;
	}

	private EventMaterialReference BuildWeeklyPromptAggregateCategoryMaterial(WeeklyEventMaterialPreviewGroup group, string category, Dictionary<string, List<EventMaterialReference>> eventBuckets)
	{
		if (eventBuckets == null || eventBuckets.Count == 0)
		{
			return null;
		}
		List<List<EventMaterialReference>> list = eventBuckets.Values.Where((List<EventMaterialReference> x) => x != null && x.Count > 0).OrderBy((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionDay ?? int.MaxValue)).ThenBy((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionSequence ?? int.MaxValue)).ThenBy((List<EventMaterialReference> x) => x.FirstOrDefault((EventMaterialReference y) => y != null)?.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		string text = GetWeeklyPromptAggregateCategoryLabel(category);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[" + text + "]");
		foreach (List<EventMaterialReference> item in list)
		{
			string weeklyPromptAggregateEventLine = BuildWeeklyPromptAggregateEventLine(item);
			if (!string.IsNullOrWhiteSpace(weeklyPromptAggregateEventLine))
			{
				stringBuilder.AppendLine("- " + weeklyPromptAggregateEventLine.Trim());
			}
		}
		string text2 = stringBuilder.ToString().TrimEnd();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return null;
		}
		EventMaterialReference eventMaterialReference = new EventMaterialReference
		{
			MaterialType = "prompt_agg_" + (category ?? "").Trim().ToLowerInvariant(),
			Label = text,
			SnapshotText = text2,
			KingdomId = group?.KingdomId ?? "",
			ActionKind = "prompt_aggregate:" + category,
			SourceMaterialCount = list.Sum((List<EventMaterialReference> x) => x.Count),
			ActionDay = list.Min((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionDay ?? int.MaxValue)),
			ActionSequence = list.Min((List<EventMaterialReference> x) => x.Min((EventMaterialReference y) => y?.ActionSequence ?? int.MaxValue))
		};
		foreach (List<EventMaterialReference> item2 in list)
		{
			foreach (EventMaterialReference item3 in item2)
			{
				AppendMaterialReferenceIds(item3, eventMaterialReference);
			}
		}
		return eventMaterialReference;
	}

	private static void AppendMaterialReferenceIds(EventMaterialReference source, EventMaterialReference destination)
	{
		if (source == null || destination == null)
		{
			return;
		}
		AddUniqueId(destination.RelatedHeroIds, source.ActorHeroId);
		AddUniqueId(destination.RelatedHeroIds, source.TargetHeroId);
		AddUniqueId(destination.RelatedHeroIds, source.HeroId);
		CopyFactIds(source.RelatedHeroIds, destination.RelatedHeroIds);
		AddUniqueId(destination.RelatedClanIds, source.ActorClanId);
		AddUniqueId(destination.RelatedClanIds, source.TargetClanId);
		AddUniqueId(destination.RelatedClanIds, source.SettlementOwnerClanId);
		AddUniqueId(destination.RelatedClanIds, source.PreviousSettlementOwnerClanId);
		CopyFactIds(source.RelatedClanIds, destination.RelatedClanIds);
		AddUniqueId(destination.RelatedKingdomIds, source.ActorKingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.TargetKingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.KingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.SettlementOwnerKingdomId);
		AddUniqueId(destination.RelatedKingdomIds, source.PreviousSettlementOwnerKingdomId);
		CopyFactIds(source.RelatedKingdomIds, destination.RelatedKingdomIds);
		CopyFactIds(source.SourceStableKeys, destination.SourceStableKeys);
		CopyFactIds(source.SourceActionKinds, destination.SourceActionKinds);
		AddUniqueId(destination.SourceStableKeys, source.ActionStableKey);
		AddUniqueId(destination.SourceActionKinds, source.ActionKind);
	}

	private string BuildWeeklyPromptAggregateEventLine(List<EventMaterialReference> materials)
	{
		if (materials == null || materials.Count == 0)
		{
			return "";
		}
		string text = ResolveWeeklyPromptAggregateCategory(materials[0]);
		switch (text)
		{
		case "strategic_shift":
			return BuildWeeklyPromptAggregateClanChangeLine(materials);
		case "settlement_change":
			return BuildWeeklyPromptAggregateSettlementOwnerChangeLine(materials);
		case "captivity":
			return BuildWeeklyPromptAggregatePrisonerLine(materials, released: false);
		case "release":
			return BuildWeeklyPromptAggregatePrisonerLine(materials, released: true);
		case "decision":
			return BuildWeeklyPromptAggregateDecisionLine(materials);
		case "siege":
			return BuildWeeklyPromptAggregateSiegeLine(materials);
		case "movement":
			return BuildWeeklyPromptAggregateMovementLine(materials);
		default:
			return BuildWeeklyPromptAggregateGenericLine(materials);
		}
	}

	private string BuildWeeklyPromptAggregateGenericLine(List<EventMaterialReference> materials)
	{
		EventMaterialReference eventMaterialReference = materials.FirstOrDefault((EventMaterialReference x) => x != null);
		if (eventMaterialReference == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		AppendWeeklyPromptAggregateField(list, "人物归属", BuildWeeklyPromptAggregateHeroAffiliationValues(materials));
		list.Add("事件=" + BuildWeeklyPromptAggregateActionLabel(materials));
		AppendWeeklyPromptAggregateField(list, "人物", ResolveHeroNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[3] { x.ActorHeroId, x.TargetHeroId, x.HeroId }, delegate(EventMaterialReference x)
		{
			return x.RelatedHeroIds;
		})));
		AppendWeeklyPromptAggregateField(list, "家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[4] { x.ActorClanId, x.TargetClanId, x.SettlementOwnerClanId, x.PreviousSettlementOwnerClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "王国", ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[5] { x.ActorKingdomId, x.TargetKingdomId, x.KingdomId, x.SettlementOwnerKingdomId, x.PreviousSettlementOwnerKingdomId }, delegate(EventMaterialReference x)
		{
			return x.RelatedKingdomIds;
		})));
		AppendWeeklyPromptAggregateField(list, "地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateMovementLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=近期行军动向");
		AppendWeeklyPromptAggregateField(list, "人物归属", BuildWeeklyPromptAggregateHeroAffiliationValues(materials));
		AppendWeeklyPromptAggregateField(list, "动向明细", BuildWeeklyPromptAggregateMovementDetailValues(materials));
		AppendWeeklyPromptAggregateField(list, "涉及地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateDecisionLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=王国决议");
		AppendWeeklyPromptAggregateField(list, "议题明细", BuildWeeklyPromptAggregateDetailValues(materials));
		AppendWeeklyPromptAggregateField(list, "相关人物归属", BuildWeeklyPromptAggregateHeroAffiliationValues(materials));
		AppendWeeklyPromptAggregateField(list, "相关家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[3] { x.ActorClanId, x.TargetClanId, x.SettlementOwnerClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "相关王国", ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[4] { x.ActorKingdomId, x.TargetKingdomId, x.KingdomId, x.SettlementOwnerKingdomId }, delegate(EventMaterialReference x)
		{
			return x.RelatedKingdomIds;
		})));
		AppendWeeklyPromptAggregateField(list, "涉及地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateSiegeLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=" + BuildWeeklyPromptAggregateActionLabel(materials));
		AppendWeeklyPromptAggregateField(list, "地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		AppendWeeklyPromptAggregateField(list, "参与人物", ResolveHeroNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[2] { x.ActorHeroId, x.HeroId }, delegate(EventMaterialReference x)
		{
			return x.RelatedHeroIds;
		})));
		AppendWeeklyPromptAggregateField(list, "参与家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.ActorClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "参与王国", ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[2] { x.ActorKingdomId, x.KingdomId }, delegate(EventMaterialReference x)
		{
			return x.RelatedKingdomIds;
		})));
		AppendWeeklyPromptAggregateField(list, "目标王国", ResolveKingdomNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[2] { x.TargetKingdomId, x.SettlementOwnerKingdomId }, null)));
		List<string> list2 = materials.Where((EventMaterialReference x) => x != null && x.ActionKind == "siege_complete").Select(delegate(EventMaterialReference x)
		{
			if (!string.IsNullOrWhiteSpace(x.SnapshotText))
			{
				if (x.SnapshotText.IndexOf("获胜", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return "获胜";
				}
				if (x.SnapshotText.IndexOf("失利", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return "失利";
				}
			}
			return "";
		}).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		AppendWeeklyPromptAggregateField(list, "结果", list2);
		return string.Join("|", list);
	}

#if false
	private string BuildWeeklyPromptAggregatePrisonerLine(List<EventMaterialReference> materials, bool released)
	{
		List<string> list = new List<string>();
		list.Add("事件=" + (released ? "囚犯获释" : "人物被俘"));
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		List<string> list5 = new List<string>();
		List<string> list6 = new List<string>();
		List<string> list7 = new List<string>();
		foreach (EventMaterialReference item in materials.Where((EventMaterialReference x) => x != null))
		{
			string text = (item.ActionKind ?? "").Trim().ToLowerInvariant();
			if (!released)
			{
				if (text == "prisoner_taken_captor")
				{
					AddUniqueId(list2, item.ActorHeroId);
					AddUniqueId(list3, item.TargetHeroId);
					AddUniqueId(list4, item.ActorKingdomId);
					AddUniqueId(list5, item.TargetKingdomId);
					AddUniqueId(list6, item.ActorClanId);
					AddUniqueId(list7, item.TargetClanId);
				}
				else if (text == "prisoner_taken_prisoner")
				{
					AddUniqueId(list2, item.TargetHeroId);
					AddUniqueId(list3, item.ActorHeroId);
					AddUniqueId(list4, item.TargetKingdomId);
					AddUniqueId(list5, item.ActorKingdomId);
					AddUniqueId(list6, item.TargetClanId);
					AddUniqueId(list7, item.ActorClanId);
				}
			}
			else if (text == "prisoner_released_captor")
			{
				AddUniqueId(list2, item.TargetHeroId);
				AddUniqueId(list3, item.ActorHeroId);
				AddUniqueId(list4, item.TargetKingdomId);
				AddUniqueId(list5, item.ActorKingdomId);
				AddUniqueId(list6, item.TargetClanId);
				AddUniqueId(list7, item.ActorClanId);
			}
			else if (text == "prisoner_released_prisoner")
			{
				AddUniqueId(list2, item.TargetHeroId);
				AddUniqueId(list3, item.ActorHeroId);
				AddUniqueId(list4, item.TargetKingdomId);
				AddUniqueId(list5, item.ActorKingdomId);
				AddUniqueId(list6, item.TargetClanId);
				AddUniqueId(list7, item.ActorClanId);
			}
		}
		AppendWeeklyPromptAggregateField(list, released ? "原囚禁方" : "俘获者", ResolveHeroNames(list2));
		AppendWeeklyPromptAggregateField(list, released ? "获释者" : "被俘者", ResolveHeroNames(list3));
		AppendWeeklyPromptAggregateField(list, released ? "原囚禁方王国" : "俘获者王国", ResolveKingdomNames(list4));
		AppendWeeklyPromptAggregateField(list, released ? "获释者王国" : "被俘者王国", ResolveKingdomNames(list5));
		AppendWeeklyPromptAggregateField(list, "地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		if (released)
		{
			List<string> list6 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptReleaseDetail(x?.ActionStableKey)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			AppendWeeklyPromptAggregateField(list, "方式", list6);
		}
		return string.Join("|", list);
	}

#endif

	private string BuildWeeklyPromptAggregatePrisonerLine(List<EventMaterialReference> materials, bool released)
	{
		List<string> list = new List<string>();
		list.Add("事件=" + (released ? "人物获释" : "人物被俘"));
		List<string> captorHeroIds = new List<string>();
		List<string> prisonerHeroIds = new List<string>();
		List<string> captorClanIds = new List<string>();
		List<string> prisonerClanIds = new List<string>();
		List<string> captorKingdomIds = new List<string>();
		List<string> prisonerKingdomIds = new List<string>();
		List<Tuple<string, string, string>> captorEntries = new List<Tuple<string, string, string>>();
		List<Tuple<string, string, string>> prisonerEntries = new List<Tuple<string, string, string>>();
		foreach (EventMaterialReference item in materials.Where((EventMaterialReference x) => x != null))
		{
			string text = (item.ActionKind ?? "").Trim().ToLowerInvariant();
			if (!released)
			{
				if (text == "prisoner_taken_captor")
				{
					AddUniqueId(captorHeroIds, item.ActorHeroId);
					AddUniqueId(prisonerHeroIds, item.TargetHeroId);
					AddUniqueId(captorClanIds, item.ActorClanId);
					AddUniqueId(prisonerClanIds, item.TargetClanId);
					AddUniqueId(captorKingdomIds, item.ActorKingdomId);
					AddUniqueId(prisonerKingdomIds, item.TargetKingdomId);
					AddWeeklyPromptAggregateAffiliationEntry(captorEntries, item.ActorHeroId, item.ActorClanId, item.ActorKingdomId);
					AddWeeklyPromptAggregateAffiliationEntry(prisonerEntries, item.TargetHeroId, item.TargetClanId, item.TargetKingdomId);
				}
				else if (text == "prisoner_taken_prisoner")
				{
					AddUniqueId(captorHeroIds, item.TargetHeroId);
					AddUniqueId(prisonerHeroIds, item.ActorHeroId);
					AddUniqueId(captorClanIds, item.TargetClanId);
					AddUniqueId(prisonerClanIds, item.ActorClanId);
					AddUniqueId(captorKingdomIds, item.TargetKingdomId);
					AddUniqueId(prisonerKingdomIds, item.ActorKingdomId);
					AddWeeklyPromptAggregateAffiliationEntry(captorEntries, item.TargetHeroId, item.TargetClanId, item.TargetKingdomId);
					AddWeeklyPromptAggregateAffiliationEntry(prisonerEntries, item.ActorHeroId, item.ActorClanId, item.ActorKingdomId);
				}
			}
			else if (text == "prisoner_released_captor" || text == "prisoner_released_prisoner")
			{
				AddUniqueId(captorHeroIds, item.TargetHeroId);
				AddUniqueId(prisonerHeroIds, item.ActorHeroId);
				AddUniqueId(captorClanIds, item.TargetClanId);
				AddUniqueId(prisonerClanIds, item.ActorClanId);
				AddUniqueId(captorKingdomIds, item.TargetKingdomId);
				AddUniqueId(prisonerKingdomIds, item.ActorKingdomId);
				AddWeeklyPromptAggregateAffiliationEntry(captorEntries, item.TargetHeroId, item.TargetClanId, item.TargetKingdomId);
				AddWeeklyPromptAggregateAffiliationEntry(prisonerEntries, item.ActorHeroId, item.ActorClanId, item.ActorKingdomId);
			}
		}
		AppendWeeklyPromptAggregateField(list, released ? "原囚禁方归属" : "俘获者归属", BuildWeeklyPromptAggregateAffiliationValues(captorEntries));
		AppendWeeklyPromptAggregateField(list, released ? "获释者归属" : "被俘者归属", BuildWeeklyPromptAggregateAffiliationValues(prisonerEntries));
		AppendWeeklyPromptAggregateField(list, released ? "原囚禁方" : "俘获者", ResolveHeroNames(captorHeroIds));
		AppendWeeklyPromptAggregateField(list, released ? "获释者" : "被俘者", ResolveHeroNames(prisonerHeroIds));
		AppendWeeklyPromptAggregateField(list, released ? "原囚禁方家族" : "俘获者家族", ResolveClanNames(captorClanIds));
		AppendWeeklyPromptAggregateField(list, released ? "获释者家族" : "被俘者家族", ResolveClanNames(prisonerClanIds));
		AppendWeeklyPromptAggregateField(list, released ? "原囚禁方王国" : "俘获者王国", ResolveKingdomNames(captorKingdomIds));
		AppendWeeklyPromptAggregateField(list, released ? "获释者王国" : "被俘者王国", ResolveKingdomNames(prisonerKingdomIds));
		AppendWeeklyPromptAggregateField(list, "地点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		if (released)
		{
			List<string> list2 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptReleaseDetail(x?.ActionStableKey)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			AppendWeeklyPromptAggregateField(list, "方式", list2);
		}
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateSettlementOwnerChangeLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=定居点易主");
		AppendWeeklyPromptAggregateField(list, "定居点", ResolveSettlementNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.SettlementId }, null)));
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		List<string> list5 = new List<string>();
		List<string> list6 = new List<string>();
		foreach (EventMaterialReference item in materials.Where((EventMaterialReference x) => x != null))
		{
			string text = (item.ActionKind ?? "").Trim().ToLowerInvariant();
			if (text == "settlement_owner_changed_gain")
			{
				AddUniqueId(list2, item.ActorHeroId);
				AddUniqueId(list3, item.TargetHeroId);
				AddUniqueId(list5, item.ActorKingdomId);
				AddUniqueId(list6, item.TargetKingdomId);
			}
			else if (text == "settlement_owner_changed_loss")
			{
				AddUniqueId(list2, item.TargetHeroId);
				AddUniqueId(list3, item.ActorHeroId);
				AddUniqueId(list5, item.TargetKingdomId);
				AddUniqueId(list6, item.ActorKingdomId);
			}
			else if (text == "settlement_owner_changed_capture")
			{
				AddUniqueId(list4, item.ActorHeroId);
			}
		}
		AppendWeeklyPromptAggregateField(list, "新所有者", ResolveHeroNames(list2));
		AppendWeeklyPromptAggregateField(list, "失去者", ResolveHeroNames(list3));
		AppendWeeklyPromptAggregateField(list, "促成者", ResolveHeroNames(list4));
		AppendWeeklyPromptAggregateField(list, "新所有者王国", ResolveKingdomNames(list5));
		AppendWeeklyPromptAggregateField(list, "失去者王国", ResolveKingdomNames(list6));
		List<string> list7 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptSettlementChangeDetail(x?.ActionStableKey)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		AppendWeeklyPromptAggregateField(list, "方式", list7);
		return string.Join("|", list);
	}

	private string BuildWeeklyPromptAggregateClanChangeLine(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		list.Add("事件=家族归属变更");
		AppendWeeklyPromptAggregateField(list, "家族", ResolveClanNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[1] { x.ActorClanId }, delegate(EventMaterialReference x)
		{
			return x.RelatedClanIds;
		})));
		AppendWeeklyPromptAggregateField(list, "成员", ResolveHeroNames(CollectMaterialIds(materials, (EventMaterialReference x) => new string[2] { x.ActorHeroId, x.HeroId }, delegate(EventMaterialReference x)
		{
			return x.RelatedHeroIds;
		})));
		List<string> list2 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptClanChangeDetail(x?.ActionStableKey)?.oldKingdomId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> list3 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptClanChangeDetail(x?.ActionStableKey)?.newKingdomId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		List<string> list4 = materials.Select((EventMaterialReference x) => ParseWeeklyPromptClanChangeDetail(x?.ActionStableKey)?.detailLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		AppendWeeklyPromptAggregateField(list, "原王国", ResolveKingdomNames(list2));
		AppendWeeklyPromptAggregateField(list, "新王国", ResolveKingdomNames(list3));
		AppendWeeklyPromptAggregateField(list, "方式", list4);
		return string.Join("|", list);
	}

	private static string BuildWeeklyPromptAggregateActionLabel(List<EventMaterialReference> materials)
	{
		List<string> list = (materials ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).Select((EventMaterialReference x) => TranslateNpcActionKindForPrompt(x.ActionKind)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count > 0)
		{
			return string.Join("、", list);
		}
		string text = (materials?.FirstOrDefault()?.ActionKind ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "未分类行动" : text;
	}

	private static void AppendWeeklyPromptAggregateField(List<string> fields, string name, List<string> values)
	{
		if (fields == null || values == null)
		{
			return;
		}
		List<string> list = values.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			return;
		}
		fields.Add(name + "=" + string.Join("、", list));
	}

	private static List<string> BuildWeeklyPromptAggregateHeroAffiliationValues(List<EventMaterialReference> materials)
	{
		List<Tuple<string, string, string>> list = new List<Tuple<string, string, string>>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			if (item == null)
			{
				continue;
			}
			AddWeeklyPromptAggregateAffiliationEntry(list, item.ActorHeroId, item.ActorClanId, item.ActorKingdomId);
			AddWeeklyPromptAggregateAffiliationEntry(list, item.TargetHeroId, item.TargetClanId, item.TargetKingdomId);
			AddWeeklyPromptAggregateAffiliationEntry(list, item.HeroId, "", item.KingdomId);
			foreach (string relatedHeroId in item.RelatedHeroIds ?? new List<string>())
			{
				AddWeeklyPromptAggregateAffiliationEntry(list, relatedHeroId, "", "");
			}
		}
		return BuildWeeklyPromptAggregateAffiliationValues(list);
	}

	private static List<string> BuildWeeklyPromptAggregateAffiliationValues(IEnumerable<Tuple<string, string, string>> entries)
	{
		List<string> list = new List<string>();
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (Tuple<string, string, string> item in entries ?? Enumerable.Empty<Tuple<string, string, string>>())
		{
			string text = (item?.Item1 ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			AddUniqueId(list, text);
			Hero heroById = FindHeroById(text);
			string item2 = !string.IsNullOrWhiteSpace(item?.Item2) ? item.Item2 : (heroById?.Clan?.StringId ?? "");
			string item3 = !string.IsNullOrWhiteSpace(item?.Item3) ? item.Item3 : (heroById?.MapFaction?.StringId ?? heroById?.Clan?.Kingdom?.StringId ?? "");
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = new List<string>();
				dictionary[text] = value;
			}
			if (!dictionary2.TryGetValue(text, out var value2))
			{
				value2 = new List<string>();
				dictionary2[text] = value2;
			}
			AddUniqueId(value, item2);
			AddUniqueId(value2, item3);
		}
		return list.Select(delegate(string heroId)
		{
			string text = ResolveHeroDisplay(heroId);
			List<string> list2 = dictionary.ContainsKey(heroId) ? ResolveClanNames(dictionary[heroId]) : new List<string>();
			List<string> list3 = dictionary2.ContainsKey(heroId) ? ResolveKingdomNames(dictionary2[heroId]) : new List<string>();
			List<string> list4 = new List<string>();
			if (list2.Count > 0)
			{
				list4.Add("家族=" + string.Join("、", list2));
			}
			if (list3.Count > 0)
			{
				list4.Add("王国=" + string.Join("、", list3));
			}
			return (list4.Count == 0) ? text : (text + "(" + string.Join(";", list4) + ")");
		}).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
	}

	private static void AddWeeklyPromptAggregateAffiliationEntry(List<Tuple<string, string, string>> entries, string heroId, string clanId, string kingdomId)
	{
		string item = (heroId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(item))
		{
			entries?.Add(Tuple.Create(item, (clanId ?? "").Trim(), (kingdomId ?? "").Trim()));
		}
	}

	private static List<string> BuildWeeklyPromptAggregateMovementDetailValues(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			if (item == null)
			{
				continue;
			}
			string text = (item.SnapshotText ?? item.Label ?? "").Trim();
			if (((item.ActionStableKey ?? "").IndexOf("RaidSettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (item.ActionKind ?? "").Trim().Equals("daily_behavior", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(item.SettlementId) && text.IndexOf("掠", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				string text2 = BuildWeeklyPromptAggregateSingleAffiliationValue(item.ActorHeroId, item.ActorClanId, item.ActorKingdomId);
				string text3 = ResolveSettlementDisplay(item.SettlementId);
				List<string> list2 = new List<string>();
				string text4 = ResolveClanDisplay(item.SettlementOwnerClanId);
				string text5 = ResolveKingdomDisplay(item.SettlementOwnerKingdomId);
				if (!string.IsNullOrWhiteSpace(text4))
				{
					list2.Add("家族=" + text4);
				}
				if (!string.IsNullOrWhiteSpace(text5))
				{
					list2.Add("王国=" + text5);
				}
				string text6 = list2.Count == 0 ? "" : ("；守方归属=" + string.Join(";", list2));
				string text7 = string.IsNullOrWhiteSpace(text2) ? "某势力" : text2;
				string text8 = string.IsNullOrWhiteSpace(text3) ? "某地" : text3;
				string text9 = text7 + "正在掠夺" + text8 + text6;
				if (!list.Contains(text9))
				{
					list.Add(text9);
				}
				continue;
			}
			string text10 = ResolveHeroDisplay(!string.IsNullOrWhiteSpace(item.ActorHeroId) ? item.ActorHeroId : (!string.IsNullOrWhiteSpace(item.HeroId) ? item.HeroId : item.TargetHeroId));
			string text11 = text;
			if (string.IsNullOrWhiteSpace(text11))
			{
				continue;
			}
			string text12 = string.IsNullOrWhiteSpace(text10) ? text11 : (text10 + "：" + text11);
			if (!list.Contains(text12))
			{
				list.Add(text12);
			}
		}
		return list;
	}

	private static string BuildWeeklyPromptAggregateSingleAffiliationValue(string heroId, string clanId, string kingdomId)
	{
		return BuildWeeklyPromptAggregateAffiliationValues(new List<Tuple<string, string, string>> { Tuple.Create((heroId ?? "").Trim(), (clanId ?? "").Trim(), (kingdomId ?? "").Trim()) }).FirstOrDefault() ?? "";
	}

	private static List<string> BuildWeeklyPromptAggregateDetailValues(List<EventMaterialReference> materials)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			string text = (item?.SnapshotText ?? item?.Label ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static List<string> CollectMaterialIds(List<EventMaterialReference> materials, Func<EventMaterialReference, IEnumerable<string>> singleSelector, Func<EventMaterialReference, IEnumerable<string>> listSelector)
	{
		List<string> list = new List<string>();
		foreach (EventMaterialReference item in materials ?? new List<EventMaterialReference>())
		{
			if (item == null)
			{
				continue;
			}
			if (singleSelector != null)
			{
				foreach (string item2 in singleSelector(item) ?? Enumerable.Empty<string>())
				{
					AddUniqueId(list, item2);
				}
			}
			if (listSelector == null)
			{
				continue;
			}
			foreach (string item3 in listSelector(item) ?? Enumerable.Empty<string>())
			{
				AddUniqueId(list, item3);
			}
		}
		return list;
	}

	private static List<string> ResolveHeroNames(IEnumerable<string> heroIds)
	{
		return (heroIds ?? Enumerable.Empty<string>()).Select(ResolveHeroDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<string> ResolveClanNames(IEnumerable<string> clanIds)
	{
		return (clanIds ?? Enumerable.Empty<string>()).Select(ResolveClanDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<string> ResolveKingdomNames(IEnumerable<string> kingdomIds)
	{
		return (kingdomIds ?? Enumerable.Empty<string>()).Select(ResolveKingdomDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<string> ResolveSettlementNames(IEnumerable<string> settlementIds)
	{
		return (settlementIds ?? Enumerable.Empty<string>()).Select(ResolveSettlementDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string GetWeeklyPromptAggregateCategoryLabel(string category)
	{
		switch ((category ?? "").Trim().ToLowerInvariant())
		{
		case "strategic_shift":
			return "家族与王国归属";
		case "decision":
			return "王国决议";
		case "siege":
			return "围城与守城";
		case "settlement_change":
			return "定居点易主";
		case "captivity":
			return "人物被俘";
		case "release":
			return "人物获释";
		case "army":
			return "军团行动";
		case "battle":
			return "战场交锋";
		case "movement":
			return "行军与袭扰";
		default:
			return "其他事件";
		}
	}

	private static string ParseWeeklyPromptSettlementChangeDetail(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 5)
		{
			return "";
		}
		switch ((array[array.Length - 1] ?? "").Trim())
		{
		case "BySiege":
			return "围城";
		case "ByBarter":
			return "易物";
		case "ByLeaveFaction":
			return "脱离王国";
		case "ByKingDecision":
			return "王国决议";
		case "ByGift":
			return "赠与";
		case "ByRebellion":
			return "叛乱";
		case "ByClanDestruction":
			return "家族覆灭";
		default:
			return (array[array.Length - 1] ?? "").Trim();
		}
	}

	private static string ParseWeeklyPromptReleaseDetail(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 4)
		{
			return "";
		}
		switch ((array[array.Length - 1] ?? "").Trim())
		{
		case "Ransom":
			return "通过赎金获释";
		case "ReleasedByChoice":
			return "被主动释放";
		case "ReleasedAfterPeace":
			return "因议和获释";
		case "ReleasedAfterEscape":
			return "成功逃脱";
		case "ReleasedAfterBattle":
			return "战后获释";
		case "ReleasedByCompensation":
			return "补偿后获释";
		case "Death":
			return "囚禁中死亡";
		default:
			return (array[array.Length - 1] ?? "").Trim();
		}
	}

	private sealed class WeeklyPromptClanChangeParseResult
	{
		public string oldKingdomId;

		public string newKingdomId;

		public string detailLabel;
	}

	private static WeeklyPromptClanChangeParseResult ParseWeeklyPromptClanChangeDetail(string stableKey)
	{
		string[] array = (stableKey ?? "").Trim().Split(new char[1] { ':' }, StringSplitOptions.None);
		if (array.Length < 5)
		{
			return null;
		}
		return new WeeklyPromptClanChangeParseResult
		{
			oldKingdomId = (array[2] ?? "").Trim(),
			newKingdomId = (array[3] ?? "").Trim(),
			detailLabel = TranslateWeeklyPromptClanChangeDetail((array[4] ?? "").Trim())
		};
	}

	private static string TranslateWeeklyPromptClanChangeDetail(string detail)
	{
		switch ((detail ?? "").Trim())
		{
		case "JoinAsMercenary":
			return "佣兵加入";
		case "JoinKingdom":
			return "正式加入";
		case "JoinKingdomByDefection":
			return "叛逃改投";
		case "LeaveKingdom":
			return "脱离王国";
		case "LeaveWithRebellion":
			return "脱离并叛乱";
		case "LeaveAsMercenary":
			return "结束佣兵服务";
		case "CreateKingdom":
			return "建立新王国";
		case "LeaveByKingdomDestruction":
			return "原王国覆灭";
		case "LeaveByClanDestruction":
			return "家族覆灭";
		default:
			return (detail ?? "").Trim();
		}
	}

	private static WeeklyReportPromptProfile GetWeeklyReportPromptProfile()
	{
		int num = 2;
		try
		{
			num = ClampInt((DuelSettings.GetSettings()?.WeeklyReportLengthPreset).GetValueOrDefault(2), 1, 4);
		}
		catch
		{
			num = 2;
		}
		return num switch
		{
			1 => new WeeklyReportPromptProfile
			{
				Preset = 1,
				MinWords = 200,
				MaxWords = 400,
				Label = "第一档（200-400字）"
			},
			2 => new WeeklyReportPromptProfile
			{
				Preset = 2,
				MinWords = 200,
				MaxWords = 800,
				Label = "第二档（200-800字）"
			},
			3 => new WeeklyReportPromptProfile
			{
				Preset = 3,
				MinWords = 200,
				MaxWords = 1200,
				Label = "第三档（200-1200字）"
			},
			4 => new WeeklyReportPromptProfile
			{
				Preset = 4,
				MinWords = 200,
				MaxWords = 1500,
				Label = "第四档（200-1500字）"
			},
			_ => new WeeklyReportPromptProfile
			{
				Preset = 2,
				MinWords = 200,
				MaxWords = 800,
				Label = "第二档（200-800字）"
			},
		};
	}

	private static int GetWeeklyReportRequestsPerMinute()
	{
		try
		{
			return ClampInt((DuelSettings.GetSettings()?.WeeklyReportRequestsPerMinute).GetValueOrDefault(5), 1, 20);
		}
		catch
		{
			return 5;
		}
	}

	private static int GetWeeklyReportRequestIntervalMs()
	{
		int weeklyReportRequestsPerMinute = GetWeeklyReportRequestsPerMinute();
		double num = 60000.0 / Math.Max(1, weeklyReportRequestsPerMinute);
		return Math.Max(0, (int)Math.Ceiling(num));
	}

	private static int GetWeeklyReportBatchSize()
	{
		return 3;
	}

	private static string BuildWeeklyReportGroupDisplayLabel(WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return "未命名分组";
		}
		string text = (group.Title ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界周报" : "王国周报";
	}

	private static string BuildWeeklyReportGroupReportId(WeeklyEventMaterialPreviewGroup group)
	{
		if (group == null)
		{
			return "";
		}
		if (string.Equals((group.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return "world";
		}
		string text = (group.KingdomId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "" : ("kingdom:" + text);
	}

	private static Dictionary<string, WeeklyEventMaterialPreviewGroup> BuildWeeklyReportGroupMap(IEnumerable<WeeklyEventMaterialPreviewGroup> groups)
	{
		Dictionary<string, WeeklyEventMaterialPreviewGroup> dictionary = new Dictionary<string, WeeklyEventMaterialPreviewGroup>(StringComparer.OrdinalIgnoreCase);
		foreach (WeeklyEventMaterialPreviewGroup item in groups ?? Enumerable.Empty<WeeklyEventMaterialPreviewGroup>())
		{
			string text = BuildWeeklyReportGroupReportId(item);
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = item;
			}
		}
		return dictionary;
	}

	private static List<string> BuildWeeklyBatchExpectedReportIds(WeeklyReportBatchRequest batch)
	{
		return (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupReportId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static List<WeeklyReportBatchRequest> BuildWeeklyReportBatchRequests(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay)
	{
		List<WeeklyReportBatchRequest> list = new List<WeeklyReportBatchRequest>();
		List<WeeklyEventMaterialPreviewGroup> list2 = (groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null && IsWeeklyReportGroupEligible(x)).ToList();
		if (list2.Count == 0)
		{
			return list;
		}
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = list2.FirstOrDefault((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase));
		List<WeeklyEventMaterialPreviewGroup> list3 = list2.Where((WeeklyEventMaterialPreviewGroup x) => !string.Equals((x.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase)).ToList();
		if (weeklyEventMaterialPreviewGroup != null)
		{
			list.Add(new WeeklyReportBatchRequest
			{
				WeekIndex = weekIndex,
				StartDay = startDay,
				EndDay = endDay,
				Groups = new List<WeeklyEventMaterialPreviewGroup> { weeklyEventMaterialPreviewGroup }
			});
		}
		int weeklyReportBatchSize = Math.Max(1, GetWeeklyReportBatchSize());
		for (int i = 0; i < list3.Count; i += weeklyReportBatchSize)
		{
			list.Add(new WeeklyReportBatchRequest
			{
				WeekIndex = weekIndex,
				StartDay = startDay,
				EndDay = endDay,
				Groups = list3.Skip(i).Take(weeklyReportBatchSize).ToList()
			});
		}
		return list;
	}

#if false
	private static string BuildWeeklyReportBatchDisplayLabel(WeeklyReportBatchRequest batch)
	{
		List<string> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupDisplayLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		return (list.Count == 0) ? "链懡鍚嶅懆鎶ユ壒娆? : ("鎵规锛? + string.Join(" | ", list));
	}

#endif

	private static string BuildWeeklyReportBatchDisplayLabel(WeeklyReportBatchRequest batch)
	{
		List<string> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupDisplayLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		return (list.Count == 0) ? "未命名周报批次" : ("批次：" + string.Join(" | ", list));
	}

	private static List<WeeklyEventMaterialPreviewGroup> BuildRemainingWeeklyReportGroupsForRetry(List<WeeklyReportBatchRequest> batches, int batchIndex, IEnumerable<WeeklyEventMaterialPreviewGroup> currentBatchRemainingGroups)
	{
		List<WeeklyEventMaterialPreviewGroup> list = new List<WeeklyEventMaterialPreviewGroup>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		void AddGroup(WeeklyEventMaterialPreviewGroup group)
		{
			string text = BuildWeeklyReportGroupReportId(group);
			if (group != null && !string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
			{
				list.Add(group);
			}
		}
		foreach (WeeklyEventMaterialPreviewGroup item in currentBatchRemainingGroups ?? Enumerable.Empty<WeeklyEventMaterialPreviewGroup>())
		{
			AddGroup(item);
		}
		for (int i = batchIndex + 1; i < (batches?.Count ?? 0); i++)
		{
			foreach (WeeklyEventMaterialPreviewGroup item2 in batches[i]?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>())
			{
				AddGroup(item2);
			}
		}
		return list;
	}

	private static string BuildWeeklyReportBatchPreviewKey(WeeklyReportBatchRequest batch)
	{
		List<string> list = BuildWeeklyBatchExpectedReportIds(batch);
		return ((batch != null) ? batch.WeekIndex : 0) + "|" + string.Join("|", list);
	}

	private void CaptureWeeklyReportBatchDevPreview(WeeklyReportBatchRequest batch, WeeklyReportBatchRequestResult result)
	{
		if (batch == null || result == null)
		{
			return;
		}
		if (_latestWeeklyReportBatchDevPreviews == null)
		{
			_latestWeeklyReportBatchDevPreviews = new List<DevWeeklyReportBatchPreviewEntry>();
		}
		string text = BuildWeeklyReportBatchPreviewKey(batch);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		DevWeeklyReportBatchPreviewEntry devWeeklyReportBatchPreviewEntry = _latestWeeklyReportBatchDevPreviews.FirstOrDefault((DevWeeklyReportBatchPreviewEntry x) => x != null && string.Equals(x.PreviewKey, text, StringComparison.OrdinalIgnoreCase));
		if (devWeeklyReportBatchPreviewEntry == null)
		{
			devWeeklyReportBatchPreviewEntry = new DevWeeklyReportBatchPreviewEntry();
			_latestWeeklyReportBatchDevPreviews.Add(devWeeklyReportBatchPreviewEntry);
		}
		devWeeklyReportBatchPreviewEntry.PreviewKey = text;
		devWeeklyReportBatchPreviewEntry.BatchLabel = BuildWeeklyReportBatchDisplayLabel(batch);
		devWeeklyReportBatchPreviewEntry.WeekIndex = batch.WeekIndex;
		devWeeklyReportBatchPreviewEntry.StartDay = batch.StartDay;
		devWeeklyReportBatchPreviewEntry.EndDay = batch.EndDay;
		devWeeklyReportBatchPreviewEntry.ReportIds = BuildWeeklyBatchExpectedReportIds(batch);
		devWeeklyReportBatchPreviewEntry.PromptPreview = result.PromptPreview ?? "";
		devWeeklyReportBatchPreviewEntry.ResponsePreview = result.RawResponse ?? "";
		devWeeklyReportBatchPreviewEntry.Success = result.Success;
		devWeeklyReportBatchPreviewEntry.FailureReason = result.FailureReason ?? "";
		devWeeklyReportBatchPreviewEntry.AttemptsUsed = result.AttemptsUsed;
		while (_latestWeeklyReportBatchDevPreviews.Count > 24)
		{
			_latestWeeklyReportBatchDevPreviews.RemoveAt(0);
		}
	}

	private DevWeeklyReportBatchPreviewEntry FindLatestWeeklyReportBatchDevPreview(WeeklyReportBatchRequest batch)
	{
		string text = BuildWeeklyReportBatchPreviewKey(batch);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return (_latestWeeklyReportBatchDevPreviews ?? new List<DevWeeklyReportBatchPreviewEntry>()).LastOrDefault((DevWeeklyReportBatchPreviewEntry x) => x != null && string.Equals(x.PreviewKey, text, StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildWeeklyReportFailureReason(string response, bool parseFailed)
	{
		string text = (response ?? "").Trim();
		if (parseFailed)
		{
			return string.IsNullOrWhiteSpace(text) ? "模型返回为空，且无法解析。" : ("模型返回无法解析：\n" + text);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "接口返回为空响应。";
		}
		return text;
	}

	private static string NormalizeWeeklyReportTagText(string text)
	{
		string text2 = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (string item in text2.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string text3 = item.Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				list.Add(text3);
			}
		}
		return string.Join("\n", list).Trim();
	}

	private static int GetWeeklyReportStabilityDeltaForTag(string tag)
	{
		switch (((tag ?? "").Trim()).ToUpperInvariant())
		{
		case "STAB_DOWN_4":
			return -15;
		case "STAB_DOWN_3":
			return -10;
		case "STAB_DOWN_2":
			return -5;
		case "STAB_DOWN_1":
			return -1;
		case "STAB_UP_1":
			return 1;
		case "STAB_UP_2":
			return 5;
		case "STAB_UP_3":
			return 10;
		case "STAB_UP_4":
			return 15;
		default:
			return 0;
		}
	}

	private static int ExtractWeeklyReportStabilityDelta(string tagText)
	{
		string text = NormalizeWeeklyReportTagText(tagText);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int num = 0;
		foreach (string item in text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string text2 = (item ?? "").Trim();
			if (string.Equals(text2, "STAB_FLAT", StringComparison.OrdinalIgnoreCase))
			{
				num = 0;
				continue;
			}
			if (text2.StartsWith("STAB_", StringComparison.OrdinalIgnoreCase))
			{
				num = GetWeeklyReportStabilityDeltaForTag(text2);
			}
		}
		return num;
	}

	private static string BuildFallbackWeeklyReportShortSummary(string report)
	{
		string text = (report ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text.Contains("  "))
		{
			text = text.Replace("  ", " ");
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.Length <= 140)
		{
			return text;
		}
		string text2 = text.Substring(0, 140).TrimEnd();
		int num = Math.Max(text2.LastIndexOf('。'), Math.Max(text2.LastIndexOf('；'), Math.Max(text2.LastIndexOf('，'), text2.LastIndexOf(' '))));
		if (num >= 40)
		{
			text2 = text2.Substring(0, num).TrimEnd();
		}
		return text2.Trim();
	}

	private static bool HasInjectedRuleBlock(string instructions, string ruleId)
	{
		string text = (instructions ?? "").Trim();
		string text2 = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		return text.IndexOf("【附加规则:" + text2 + "】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static string ResolveWeeklyReportNpcKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		string text = (kingdomIdOverride ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(targetHero?.Clan?.Kingdom);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(targetHero?.MapFaction);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		Hero heroObject = targetCharacter?.HeroObject;
		text = GetKingdomId(heroObject?.Clan?.Kingdom);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(heroObject?.MapFaction);
		return (text ?? "").Trim();
	}

	private static string ResolveWeeklyReportSurroundingsKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		string text = GetKingdomId(Settlement.CurrentSettlement?.MapFaction);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(targetHero?.CurrentSettlement?.MapFaction);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(GetDevEditableKingdoms().Select((Kingdom x) => x?.StringId));
		return kingdomIdsByPlayerProximity.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "";
	}

	private EventRecordEntry FindLatestWeeklyReportRecord(string eventKind, string kingdomId = null)
	{
		string text = (eventKind ?? "").Trim();
		string text2 = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			EnsureWeekZeroOpeningSummaryEvents();
		}
		catch
		{
		}
		return SanitizeEventRecordEntries(_eventRecordEntries).Where((EventRecordEntry x) => x != null && string.Equals((x.EventKind ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) && string.Equals((x.ScopeKingdomId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase)).OrderByDescending((EventRecordEntry x) => x.WeekIndex).ThenByDescending((EventRecordEntry x) => x.CreatedDay).FirstOrDefault();
	}

	private List<string> SelectWeeklyShortReportKingdomIds(string npcKingdomId, bool excludeNpcKingdom)
	{
		string text = (npcKingdomId ?? "").Trim();
		bool flag = IsKingdomEligibleForWeeklyReport(text);
		List<string> list = GetKingdomIdsByPlayerProximity(GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select((Kingdom x) => x?.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			list = GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select((Kingdom x) => (x?.StringId ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		if (excludeNpcKingdom && !string.IsNullOrWhiteSpace(text))
		{
			list.RemoveAll((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		List<string> list2 = list.Take(3).ToList();
		if (!excludeNpcKingdom && flag && !string.IsNullOrWhiteSpace(text) && !list2.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			if (list2.Count >= 3)
			{
				list2.RemoveAt(list2.Count - 1);
			}
			list2.Insert(0, text);
		}
		return list2.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
	}

	private string BuildWeeklyShortReportsPromptBlock(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride, bool excludeNpcKingdom)
	{
		string weeklyReportNpcKingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		List<string> list = SelectWeeklyShortReportKingdomIds(weeklyReportNpcKingdomId, excludeNpcKingdom);
		if (list.Count == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (string item in list)
		{
			EventRecordEntry latestWeeklyReportRecord = FindLatestWeeklyReportRecord("kingdom", item);
			string text = BuildFallbackWeeklyReportShortSummary(latestWeeklyReportRecord?.ShortSummary ?? latestWeeklyReportRecord?.Summary);
			if (latestWeeklyReportRecord == null || string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (num == 0)
			{
				stringBuilder.AppendLine("【近期三个王国短周报】");
				stringBuilder.AppendLine("以下为最近三个相关王国的短周报，只作背景事实参考；不要把其中事实错说到别的王国。");
			}
			num++;
			stringBuilder.Append("- ").Append(ResolveKingdomDisplay(item));
			if (latestWeeklyReportRecord.WeekIndex >= 0)
			{
				stringBuilder.Append("（第").Append(latestWeeklyReportRecord.WeekIndex).Append("周）");
			}
			stringBuilder.Append("：").AppendLine(text);
		}
		return num > 0 ? stringBuilder.ToString().TrimEnd() : "";
	}

	private static string BuildSingleWeeklyFullReportPromptBlock(string header, EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string text = (entry.Summary ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【" + ((header ?? "").Trim()) + "】");
		if (!string.IsNullOrWhiteSpace(entry.Title))
		{
			stringBuilder.AppendLine("标题：" + entry.Title.Trim());
		}
		if (entry.WeekIndex >= 0)
		{
			stringBuilder.AppendLine("周次：第" + entry.WeekIndex + "周");
		}
		stringBuilder.AppendLine(text);
		return stringBuilder.ToString().TrimEnd();
	}

	private string BuildTriggeredWeeklyFullReportsPromptBlock(string triggeredRuleInstructions, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		bool flag = HasInjectedRuleBlock(triggeredRuleInstructions, "npc_major_actions");
		bool flag2 = HasInjectedRuleBlock(triggeredRuleInstructions, "npc_recent_actions");
		bool flag3 = HasInjectedRuleBlock(triggeredRuleInstructions, "surroundings");
		if (!flag && !flag2 && !flag3)
		{
			return "";
		}
		string weeklyReportNpcKingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		string weeklyReportSurroundingsKingdomId = ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder();
		if (flag || flag2)
		{
			EventRecordEntry latestWeeklyReportRecord = FindLatestWeeklyReportRecord("kingdom", weeklyReportNpcKingdomId);
			string singleWeeklyFullReportPromptBlock = BuildSingleWeeklyFullReportPromptBlock("NPC所属王国完整周报", latestWeeklyReportRecord);
			if (!string.IsNullOrWhiteSpace(singleWeeklyFullReportPromptBlock) && hashSet.Add("kingdom:" + (weeklyReportNpcKingdomId ?? "").Trim()))
			{
				stringBuilder.AppendLine(singleWeeklyFullReportPromptBlock);
			}
			EventRecordEntry latestWeeklyReportRecord2 = FindLatestWeeklyReportRecord("world", "");
			string singleWeeklyFullReportPromptBlock2 = BuildSingleWeeklyFullReportPromptBlock("世界完整周报", latestWeeklyReportRecord2);
			if (!string.IsNullOrWhiteSpace(singleWeeklyFullReportPromptBlock2) && hashSet.Add("world"))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine(singleWeeklyFullReportPromptBlock2);
			}
		}
		if (flag3)
		{
			EventRecordEntry latestWeeklyReportRecord3 = FindLatestWeeklyReportRecord("kingdom", weeklyReportSurroundingsKingdomId);
			string singleWeeklyFullReportPromptBlock3 = BuildSingleWeeklyFullReportPromptBlock("周边相关王国完整周报", latestWeeklyReportRecord3);
			if (!string.IsNullOrWhiteSpace(singleWeeklyFullReportPromptBlock3) && hashSet.Add("kingdom:" + (weeklyReportSurroundingsKingdomId ?? "").Trim()))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine(singleWeeklyFullReportPromptBlock3);
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private bool ShouldExcludeNpcShortReportFromWeeklyShortLayer(string triggeredRuleInstructions, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		if (HasInjectedRuleBlock(triggeredRuleInstructions, "npc_major_actions") || HasInjectedRuleBlock(triggeredRuleInstructions, "npc_recent_actions"))
		{
			return true;
		}
		if (HasInjectedRuleBlock(triggeredRuleInstructions, "surroundings"))
		{
			string weeklyReportNpcKingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
			string weeklyReportSurroundingsKingdomId = ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);
			if (!string.IsNullOrWhiteSpace(weeklyReportNpcKingdomId) && string.Equals(weeklyReportNpcKingdomId, weeklyReportSurroundingsKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsAnyIgnoreCase(string text, params string[] tokens)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || tokens == null)
		{
			return false;
		}
		foreach (string text3 in tokens)
		{
			if (!string.IsNullOrWhiteSpace(text3) && text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsQuotaLimitResponseBody(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "quota", "balance", "insufficient", "credit", "billing", "额度", "余额", "欠费");
	}

	private static bool IsRequestsPerMinuteLimitResponseBody(string responseBody)
	{
		string text = (responseBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (ContainsAnyIgnoreCase(text, "rpm", "requests per minute", "request per minute", "requests/min", "request/min", "requests per min", "request per min", "req/min", "req per min", "每分钟请求", "每分钟最多请求"))
		{
			return true;
		}
		bool flag = ContainsAnyIgnoreCase(text, "request", "requests", "请求", "req");
		bool flag2 = ContainsAnyIgnoreCase(text, "minute", "min", "/min", "per min", "per-minute", "每分钟");
		return flag && flag2;
	}

	private static bool IsGenericRateLimitResponseBody(string responseBody)
	{
		return ContainsAnyIgnoreCase(responseBody, "rate limit", "too many requests", "ratelimit", "限流", "请求过于频繁", "请求频率过高", "速率限制");
	}

	private static int? TryGetRetryAfterSeconds(HttpResponseMessage response)
	{
		if (response == null)
		{
			return null;
		}
		try
		{
			if (response.Headers?.RetryAfter?.Delta != null)
			{
				return Math.Max(0, (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds));
			}
			if (response.Headers != null && response.Headers.TryGetValues("Retry-After", out var values))
			{
				string text = values?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
				if (int.TryParse((text ?? "").Trim(), out var result))
				{
					return Math.Max(0, result);
				}
				if (DateTimeOffset.TryParse(text, out var result2))
				{
					return Math.Max(0, (int)Math.Ceiling((result2 - DateTimeOffset.UtcNow).TotalSeconds));
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool HasRequestsPerMinuteRateLimitHeaders(HttpResponseMessage response)
	{
		if (response?.Headers == null)
		{
			return false;
		}
		try
		{
			foreach (KeyValuePair<string, IEnumerable<string>> item in response.Headers)
			{
				string key = (item.Key ?? "").Trim();
				if (string.IsNullOrWhiteSpace(key))
				{
					continue;
				}
				if (ContainsAnyIgnoreCase(key, "ratelimit", "rate-limit", "limit-requests", "remaining-requests", "reset-requests"))
				{
					return true;
				}
				string text = string.Join(" ", item.Value ?? Enumerable.Empty<string>());
				if (IsRequestsPerMinuteLimitResponseBody(text))
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

	private static string BuildApiCallFailureMessage(HttpStatusCode statusCode, string responseBody, int? retryAfterSeconds, bool isRateLimit, bool isRequestsPerMinuteLimit, bool isQuotaLimit)
	{
		string text = ((int)statusCode).ToString() + " " + statusCode;
		string text2 = (responseBody ?? "").Trim();
		StringBuilder stringBuilder = new StringBuilder();
		if (isRequestsPerMinuteLimit)
		{
			stringBuilder.Append("请求疑似触发了 RPM（每分钟请求数）限流");
		}
		else if (isQuotaLimit)
		{
			stringBuilder.Append("账号额度或余额不足，导致请求被拒绝");
		}
		else if (isRateLimit)
		{
			stringBuilder.Append("请求触发了速率限制");
		}
		else
		{
			stringBuilder.Append("接口请求失败");
		}
		stringBuilder.Append("（HTTP ").Append(text).Append("）");
		if (retryAfterSeconds.HasValue)
		{
			stringBuilder.Append("，建议等待 ").Append(retryAfterSeconds.Value).Append(" 秒后再试");
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			stringBuilder.Append("：").Append(text2);
		}
		return stringBuilder.ToString();
	}

	private static void TryPersistMcmSettings(DuelSettings settings)
	{
		try
		{
			MethodInfo method = settings?.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
			method?.Invoke(settings, null);
		}
		catch
		{
		}
	}

	private async Task<WeeklyReportRequestResult> GenerateWeeklyReportGroupWithRetriesAsync(WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay, int maxAttempts)
	{
		WeeklyReportRequestResult weeklyReportRequestResult = new WeeklyReportRequestResult();
		string text = BuildWeeklyReportSystemPrompt(group);
		string text2 = BuildWeeklyReportUserPrompt(group, weekIndex, startDay, endDay);
		string text3 = BuildWeeklyReportPromptPreviewText(group, text, text2);
		weeklyReportRequestResult.PromptPreview = text3;
		string text4 = BuildWeeklyReportGroupDisplayLabel(group);
		for (int i = 1; i <= Math.Max(1, maxAttempts); i++)
		{
			ApiCallResult apiCallResult = await CallUniversalApiDetailed(text, text2, logToEventLogs: true, eventLogSource: "EventWeeklyReport");
			string text5 = apiCallResult.Success ? (apiCallResult.Content ?? "") : ("错误: " + (apiCallResult.ErrorMessage ?? "未知错误"));
			Logger.LogEventPromptExchange(text4 + " [尝试 " + i + "/" + maxAttempts + "]", text3, text5);
			if (!apiCallResult.Success)
			{
				weeklyReportRequestResult.FailureReason = BuildWeeklyReportFailureReason(apiCallResult.ErrorMessage, parseFailed: false);
				weeklyReportRequestResult.AttemptsUsed = i;
				weeklyReportRequestResult.IsRateLimit = apiCallResult.IsRateLimit;
				weeklyReportRequestResult.IsRequestsPerMinuteLimit = apiCallResult.IsRequestsPerMinuteLimit;
				weeklyReportRequestResult.IsQuotaLimit = apiCallResult.IsQuotaLimit;
				weeklyReportRequestResult.RetryAfterSeconds = apiCallResult.RetryAfterSeconds;
			}
			else if (!TryParseWeeklyReportResponse(apiCallResult.Content, group, weekIndex, out var title, out var shortSummary, out var report, out var tagText))
			{
				weeklyReportRequestResult.FailureReason = BuildWeeklyReportFailureReason(apiCallResult.Content, parseFailed: true);
				weeklyReportRequestResult.AttemptsUsed = i;
			}
			else
			{
				weeklyReportRequestResult.Success = true;
				weeklyReportRequestResult.Title = title;
				weeklyReportRequestResult.ShortSummary = shortSummary;
				weeklyReportRequestResult.Report = report;
				weeklyReportRequestResult.TagText = tagText;
				weeklyReportRequestResult.AttemptsUsed = i;
				return weeklyReportRequestResult;
			}
			if (i < maxAttempts)
			{
				Logger.Log("EventWeeklyReport", text4 + " 第" + i + "次请求失败，准备自动重试。原因：" + weeklyReportRequestResult.FailureReason);
				int num = 1200;
				if (weeklyReportRequestResult.IsRateLimit)
				{
					num = Math.Max(num, 60000);
				}
				if (weeklyReportRequestResult.RetryAfterSeconds.HasValue)
				{
					num = Math.Max(num, weeklyReportRequestResult.RetryAfterSeconds.Value * 1000);
				}
				await Task.Delay(num);
			}
		}
		return weeklyReportRequestResult;
	}

	private async Task<WeeklyReportBatchRequestResult> GenerateWeeklyReportBatchWithRetriesAsync(WeeklyReportBatchRequest batch, int maxAttempts)
	{
		WeeklyReportBatchRequestResult weeklyReportBatchRequestResult = new WeeklyReportBatchRequestResult();
		string text = BuildWeeklyBatchReportSystemPrompt();
		string text2 = BuildWeeklyBatchReportUserPrompt(batch);
		string text3 = BuildWeeklyBatchPromptPreviewText(batch, text, text2);
		string text4 = BuildWeeklyReportBatchDisplayLabel(batch);
		weeklyReportBatchRequestResult.PromptPreview = text3;
		for (int i = 1; i <= Math.Max(1, maxAttempts); i++)
		{
			ApiCallResult apiCallResult = await CallUniversalApiDetailed(text, text2, logToEventLogs: true, eventLogSource: "EventWeeklyReport");
			string text5 = apiCallResult.Success ? (apiCallResult.Content ?? "") : ("閿欒: " + (apiCallResult.ErrorMessage ?? "鏈煡閿欒"));
			weeklyReportBatchRequestResult.RawResponse = text5;
			Logger.LogEventPromptExchange(text4 + " [灏濊瘯 " + i + "/" + maxAttempts + "]", text3, text5);
			weeklyReportBatchRequestResult.AttemptsUsed = i;
			if (!apiCallResult.Success)
			{
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = BuildWeeklyReportFailureReason(apiCallResult.ErrorMessage, parseFailed: false);
				weeklyReportBatchRequestResult.IsRateLimit = apiCallResult.IsRateLimit;
				weeklyReportBatchRequestResult.IsRequestsPerMinuteLimit = apiCallResult.IsRequestsPerMinuteLimit;
				weeklyReportBatchRequestResult.IsQuotaLimit = apiCallResult.IsQuotaLimit;
				weeklyReportBatchRequestResult.RetryAfterSeconds = apiCallResult.RetryAfterSeconds;
				weeklyReportBatchRequestResult.Blocks = new List<WeeklyReportBatchBlockResult>();
				weeklyReportBatchRequestResult.MissingReportIds = BuildWeeklyBatchExpectedReportIds(batch);
			}
			else if (!TryParseWeeklyBatchResponse(apiCallResult.Content, batch, out var blocks, out var missingReportIds, out var failureReason))
			{
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = failureReason;
				weeklyReportBatchRequestResult.Blocks = blocks ?? new List<WeeklyReportBatchBlockResult>();
				weeklyReportBatchRequestResult.MissingReportIds = missingReportIds ?? BuildWeeklyBatchExpectedReportIds(batch);
			}
			else
			{
				weeklyReportBatchRequestResult.Blocks = blocks ?? new List<WeeklyReportBatchBlockResult>();
				weeklyReportBatchRequestResult.MissingReportIds = missingReportIds ?? new List<string>();
				if (weeklyReportBatchRequestResult.MissingReportIds.Count == 0)
				{
					weeklyReportBatchRequestResult.Success = true;
					weeklyReportBatchRequestResult.FailureReason = "";
					return weeklyReportBatchRequestResult;
				}
				weeklyReportBatchRequestResult.Success = false;
				weeklyReportBatchRequestResult.FailureReason = failureReason;
			}
			if (i < maxAttempts)
			{
#if false
				Logger.Log("EventWeeklyReport", text4 + " 绗? + i + "娆℃壒閲忚姹傚け璐ワ紝鍑嗗鑷姩閲嶈瘯銆傚師鍥狅細" + weeklyReportBatchRequestResult.FailureReason);
#endif
				Logger.Log("EventWeeklyReport", text4 + " 第 " + i + " 次批量请求失败，准备自动重试。原因：" + weeklyReportBatchRequestResult.FailureReason);
				int num = 1200;
				if (weeklyReportBatchRequestResult.IsRateLimit)
				{
					num = Math.Max(num, 60000);
				}
				if (weeklyReportBatchRequestResult.RetryAfterSeconds.HasValue)
				{
					num = Math.Max(num, weeklyReportBatchRequestResult.RetryAfterSeconds.Value * 1000);
				}
				await Task.Delay(num);
			}
		}
		return weeklyReportBatchRequestResult;
	}

	private async Task<WeeklyReportBatchExecutionResult> ExecuteWeeklyReportBatchAsync(WeeklyReportBatchRequest batch, int batchIndex, int maxAttempts)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		WeeklyReportBatchRequestResult result;
		try
		{
			result = await GenerateWeeklyReportBatchWithRetriesAsync(batch, maxAttempts);
		}
		catch (Exception ex)
		{
			result = new WeeklyReportBatchRequestResult
			{
				Success = false,
				FailureReason = "Batch execution crashed: " + ex.Message,
				AttemptsUsed = 1,
				Blocks = new List<WeeklyReportBatchBlockResult>(),
				MissingReportIds = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupReportId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			};
		}
		stopwatch.Stop();
		return new WeeklyReportBatchExecutionResult
		{
			BatchIndex = batchIndex,
			Batch = batch,
			Result = result,
			ElapsedMilliseconds = (long)Math.Max(0.0, stopwatch.Elapsed.TotalMilliseconds)
		};
	}

	private static WeeklyReportRetryContext CreateWeeklyReportRetryContext(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool isAutoGeneration, WeeklyEventMaterialPreviewGroup failedGroup, WeeklyReportRequestResult requestResult)
	{
		WeeklyReportRetryContext weeklyReportRetryContext = new WeeklyReportRetryContext
		{
			WeekIndex = weekIndex,
			StartDay = startDay,
			EndDay = endDay,
			DisplayLabel = (displayLabel ?? "").Trim(),
			OpenViewerWhenDone = openViewerWhenDone,
			IsAutoGeneration = isAutoGeneration,
			FailedGroupTitle = BuildWeeklyReportGroupDisplayLabel(failedGroup),
			FailedReason = (requestResult?.FailureReason ?? "").Trim(),
			AttemptsUsed = requestResult?.AttemptsUsed ?? 0,
			IsRateLimit = requestResult?.IsRateLimit ?? false,
			IsRequestsPerMinuteLimit = requestResult?.IsRequestsPerMinuteLimit ?? false,
			IsQuotaLimit = requestResult?.IsQuotaLimit ?? false,
			RetryAfterSeconds = requestResult?.RetryAfterSeconds
		};
		foreach (WeeklyEventMaterialPreviewGroup item in groups ?? new List<WeeklyEventMaterialPreviewGroup>())
		{
			if (item != null)
			{
				weeklyReportRetryContext.Groups.Add(item);
			}
		}
		return weeklyReportRetryContext;
	}

	private void QueueWeeklyReportFailurePopup(WeeklyReportRetryContext context, bool showImmediate = false)
	{
		if (context == null)
		{
			return;
		}
		_weeklyReportRetryContext = context;
		_weeklyReportUiStage = WeeklyReportUiStage.Failure;
		_weeklyReportUiResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(showImmediate ? 60.0 : 180.0).Ticks;
		if (showImmediate && !InformationManager.IsAnyInquiryActive())
		{
			ShowWeeklyReportFailurePopup(ignoreDelay: true);
		}
	}

	private void ShowWeeklyReportFailurePopup(bool ignoreDelay = false)
	{
		if (_weeklyReportRetryContext == null || _weeklyReportManualRetryInProgress)
		{
			return;
		}
		long ticks = DateTime.UtcNow.Ticks;
		if (!ignoreDelay && ticks < _weeklyReportUiResumeAfterUtcTicks)
		{
			return;
		}
		_weeklyReportUiStage = WeeklyReportUiStage.Failure;
		_weeklyReportUiResumeAfterUtcTicks = ticks + TimeSpan.FromMilliseconds(150.0).Ticks;
		WeeklyReportRetryContext weeklyReportRetryContext = _weeklyReportRetryContext;
		string text = "第" + weeklyReportRetryContext.WeekIndex + "周自动周报";
		if (!weeklyReportRetryContext.IsAutoGeneration)
		{
			text = string.IsNullOrWhiteSpace(weeklyReportRetryContext.DisplayLabel) ? ("第" + weeklyReportRetryContext.WeekIndex + "周周报") : weeklyReportRetryContext.DisplayLabel;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(text + "在生成过程中遇到了无法自动恢复的 API/模型错误。");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("失败分组：" + (weeklyReportRetryContext.FailedGroupTitle ?? "未命名分组"));
		stringBuilder.AppendLine("自动重试次数：" + Math.Max(weeklyReportRetryContext.AttemptsUsed, 0) + "/3");
		stringBuilder.AppendLine("时间范围：第 " + weeklyReportRetryContext.StartDay + " 天至第 " + weeklyReportRetryContext.EndDay + " 天");
		if (weeklyReportRetryContext.IsRequestsPerMinuteLimit)
		{
			int weeklyReportRequestsPerMinute = GetWeeklyReportRequestsPerMinute();
			int num = Math.Max(1, weeklyReportRequestsPerMinute / 2);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("系统判断这次更像是 RPM（每分钟请求数）超限。");
			stringBuilder.AppendLine("当前 MCM 的【每分钟最多生成周报数】=" + weeklyReportRequestsPerMinute + "。");
			stringBuilder.AppendLine("建议先下调到 " + num + " 左右，再继续补跑。");
			if (weeklyReportRetryContext.RetryAfterSeconds.HasValue)
			{
				stringBuilder.AppendLine("接口建议等待：" + weeklyReportRetryContext.RetryAfterSeconds.Value + " 秒。");
			}
		}
		if (!string.IsNullOrWhiteSpace(weeklyReportRetryContext.FailedReason))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("最后一次失败原因：");
			stringBuilder.AppendLine(weeklyReportRetryContext.FailedReason.Trim());
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(weeklyReportRetryContext.IsRequestsPerMinuteLimit ? "游戏将暂停在这里。你可以先直接修改 RPM 限制并立即重试，或进入 API 配置流程继续排查。" : "游戏将暂停在这里。你可以手动重试本周周报生成，或者先进入 API 配置流程修正后再回来重试。");
		string text2 = weeklyReportRetryContext.IsRequestsPerMinuteLimit ? "修改RPM并重试" : "手动重试";
		InformationManager.ShowInquiry(new InquiryData("周事件生成失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, text2, "调整API信息", delegate
		{
			if (weeklyReportRetryContext.IsRequestsPerMinuteLimit)
			{
				OpenWeeklyReportRpmLimitInput();
			}
			else
			{
				BeginRetryBlockedWeeklyReports();
			}
		}, delegate
		{
			OpenWeeklyReportApiRepairFlow();
		}), pauseGameActiveState: true);
	}

	private void OpenWeeklyReportRpmLimitInput()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("无法读取 MCM 设置，暂时不能修改 RPM 限制。"));
				ShowWeeklyReportFailurePopup(ignoreDelay: true);
				return;
			}
			int weeklyReportRequestsPerMinute = GetWeeklyReportRequestsPerMinute();
			int num = Math.Max(1, weeklyReportRequestsPerMinute / 2);
			InformationManager.ShowTextInquiry(new TextInquiryData("修改周报RPM限制", "检测到本次失败疑似触发 RPM 限流。\n\n请输入新的【每分钟最多生成周报数】（范围 1-20）。\n当前值：" + weeklyReportRequestsPerMinute + "\n建议先改为：" + num + "\n\n保存后会立即按新速率继续补跑当前周报。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存并重试", "返回", delegate(string input)
			{
				string text = (input ?? "").Trim();
				if (!int.TryParse(text, out var result))
				{
					InformationManager.DisplayMessage(new InformationMessage("请输入 1-20 之间的整数 RPM。"));
					OpenWeeklyReportRpmLimitInput();
				}
				else
				{
					result = Math.Max(1, Math.Min(20, result));
					settings.WeeklyReportRequestsPerMinute = result;
					TryPersistMcmSettings(settings);
					InformationManager.DisplayMessage(new InformationMessage("已将周报 RPM 上限更新为 " + result + "，现在开始重试。"));
					BeginRetryBlockedWeeklyReports();
				}
			}, delegate
			{
				ShowWeeklyReportFailurePopup(ignoreDelay: true);
			}));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开 RPM 输入框失败：" + ex.Message));
			ShowWeeklyReportFailurePopup(ignoreDelay: true);
		}
	}

	private void ShowWeeklyReportRetryProgressPopup()
	{
		if (_weeklyReportRetryContext == null || !_weeklyReportManualRetryInProgress)
		{
			return;
		}
		_weeklyReportUiStage = WeeklyReportUiStage.RetryProgress;
		_weeklyReportUiResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(150.0).Ticks;
		string text = "正在重试生成第" + _weeklyReportRetryContext.WeekIndex + "周周报中的这个事件，请稍候……\n\n- 当前失败分组：" + (_weeklyReportRetryContext.FailedGroupTitle ?? "未命名分组") + "\n- 后台会再次按每条分组三次重试的规则执行\n- 如果你不想继续等待，可以直接退出当前存档\n- 也可以返回上一界面，稍后再决定是否继续重试";
		InformationManager.ShowInquiry(new InquiryData("正在重试生成此事件", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存并退出", "返回上一界面", ExitCurrentGameFromWeeklyReportGate, CancelWeeklyReportManualRetryAndReturn), pauseGameActiveState: true);
	}

	private void BeginRetryBlockedWeeklyReports()
	{
		if (_weeklyReportRetryContext == null || _weeklyReportManualRetryInProgress)
		{
			return;
		}
		_weeklyReportManualRetryInProgress = true;
		int num = ++_weeklyReportManualRetryVersion;
		_weeklyReportUiStage = WeeklyReportUiStage.RetryProgress;
		ShowWeeklyReportRetryProgressPopup();
		_ = RetryBlockedWeeklyReportsAsync(_weeklyReportRetryContext, num);
	}

	private void CancelWeeklyReportManualRetryAndReturn()
	{
		_weeklyReportManualRetryVersion++;
		_weeklyReportManualRetryInProgress = false;
		_pendingWeeklyReportManualRetryResult = false;
		_pendingWeeklyReportManualRetrySucceeded = false;
		_pendingWeeklyReportManualRetryMessage = "";
		_pendingWeeklyReportManualRetryContext = null;
		_weeklyReportUiStage = WeeklyReportUiStage.None;
		InformationManager.HideInquiry();
		QueueWeeklyReportFailurePopup(_weeklyReportRetryContext, showImmediate: true);
	}

	private void ExitCurrentGameFromWeeklyReportGate()
	{
		try
		{
			_weeklyReportManualRetryVersion++;
			_weeklyReportManualRetryInProgress = false;
			_pendingWeeklyReportManualRetryResult = false;
			_pendingWeeklyReportManualRetrySucceeded = false;
			_pendingWeeklyReportManualRetryMessage = "";
			_pendingWeeklyReportManualRetryContext = null;
			_weeklyReportUiStage = WeeklyReportUiStage.None;
			_weeklyReportReopenAfterApiConfig = false;
			InformationManager.HideInquiry();
			BeginSaveAndExitCurrentGame(SaveAndExitReason.WeeklyReport);
		}
		catch (Exception ex)
		{
			HandleSaveAndExitFailure(SaveAndExitReason.WeeklyReport, "保存并退出失败：" + ex.Message);
		}
	}

	private void BeginSaveAndExitCurrentGame(SaveAndExitReason reason)
	{
		_saveAndExitReason = reason;
		SaveHandler saveHandler = Campaign.Current?.SaveHandler;
		if (saveHandler == null)
		{
			_saveAndExitReason = SaveAndExitReason.None;
			MBGameManager.EndGame();
			return;
		}
		if (saveHandler.IsSaving)
		{
			_saveAndExitStage = SaveAndExitStage.WaitingForCurrentSave;
			InformationManager.DisplayMessage(new InformationMessage("检测到当前已有保存进行中，完成后将再保存一次并自动退出。"));
			return;
		}
		_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
		saveHandler.QuickSaveCurrentGame();
		InformationManager.DisplayMessage(new InformationMessage("正在保存当前存档，保存完成后将自动退出。"));
	}

	private void OnSaveOver(bool isSuccessful, string saveName)
	{
		if (_saveAndExitStage == SaveAndExitStage.None || _saveAndExitReason == SaveAndExitReason.None)
		{
			return;
		}
		try
		{
			if (_saveAndExitStage == SaveAndExitStage.WaitingForCurrentSave)
			{
				SaveHandler saveHandler = Campaign.Current?.SaveHandler;
				if (saveHandler == null)
				{
					SaveAndExitReason saveAndExitReason = _saveAndExitReason;
					_saveAndExitStage = SaveAndExitStage.None;
					_saveAndExitReason = SaveAndExitReason.None;
					HandleSaveAndExitFailure(saveAndExitReason, "保存并退出失败：未找到存档保存器。");
					return;
				}
				_saveAndExitStage = SaveAndExitStage.WaitingForRequestedQuickSave;
				saveHandler.QuickSaveCurrentGame();
				InformationManager.DisplayMessage(new InformationMessage("正在保存当前存档，保存完成后将自动退出。"));
				return;
			}
			SaveAndExitReason saveAndExitReason2 = _saveAndExitReason;
			_saveAndExitStage = SaveAndExitStage.None;
			_saveAndExitReason = SaveAndExitReason.None;
			if (isSuccessful)
			{
				MBGameManager.EndGame();
				return;
			}
			HandleSaveAndExitFailure(saveAndExitReason2, "保存当前存档失败，已取消退出。");
		}
		catch (Exception ex)
		{
			SaveAndExitReason saveAndExitReason3 = _saveAndExitReason;
			_saveAndExitStage = SaveAndExitStage.None;
			_saveAndExitReason = SaveAndExitReason.None;
			HandleSaveAndExitFailure(saveAndExitReason3, "保存并退出失败：" + ex.Message);
		}
	}

	private void HandleSaveAndExitFailure(SaveAndExitReason reason, string message)
	{
		InformationManager.DisplayMessage(new InformationMessage(message));
		switch (reason)
		{
		case SaveAndExitReason.MissingOnnx:
			_missingOnnxGateActive = true;
			_missingOnnxGateResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(100.0).Ticks;
			ShowMissingOnnxGatePopup();
			break;
		case SaveAndExitReason.WeeklyReport:
			QueueWeeklyReportFailurePopup(_weeklyReportRetryContext, showImmediate: true);
			break;
		}
	}

	private async Task RetryBlockedWeeklyReportsAsync(WeeklyReportRetryContext context, int retryVersion)
	{
		WeeklyReportGenerationResult weeklyReportGenerationResult = null;
		try
		{
			weeklyReportGenerationResult = await GenerateWeeklyReportsBatchedAsyncInternal(context.Groups, context.WeekIndex, context.StartDay, context.EndDay, context.DisplayLabel, context.OpenViewerWhenDone, queueBlockingPopupOnFatalFailure: false, isAutoGeneration: context.IsAutoGeneration);
			if (retryVersion != _weeklyReportManualRetryVersion)
			{
				return;
			}
			_pendingWeeklyReportManualRetrySucceeded = weeklyReportGenerationResult != null && weeklyReportGenerationResult.Completed && !weeklyReportGenerationResult.BlockedByFatalFailure;
			if (_pendingWeeklyReportManualRetrySucceeded)
			{
				if (context.IsAutoGeneration)
				{
					_lastAutoGeneratedWeeklyReportWeek = Math.Max(_lastAutoGeneratedWeeklyReportWeek, context.WeekIndex);
				}
				_pendingWeeklyReportManualRetryMessage = "周事件补跑成功，已解除暂停。";
				_pendingWeeklyReportManualRetryContext = null;
			}
			else
			{
				_pendingWeeklyReportManualRetryMessage = "周事件补跑仍未成功，请检查 API / 模型配置后再试。";
				_pendingWeeklyReportManualRetryContext = (weeklyReportGenerationResult?.RetryContext ?? context);
			}
		}
		catch (Exception ex)
		{
			if (retryVersion != _weeklyReportManualRetryVersion)
			{
				return;
			}
			Logger.Log("EventWeeklyReport", "[ERROR] RetryBlockedWeeklyReportsAsync failed: " + ex);
			_pendingWeeklyReportManualRetrySucceeded = false;
			_pendingWeeklyReportManualRetryMessage = "周事件补跑异常失败：" + ex.Message;
			_pendingWeeklyReportManualRetryContext = context;
		}
		finally
		{
			if (retryVersion == _weeklyReportManualRetryVersion)
			{
				_pendingWeeklyReportManualRetryResult = true;
			}
		}
	}

	private void OpenWeeklyReportApiRepairFlow()
	{
		_weeklyReportUiStage = WeeklyReportUiStage.None;
		_weeklyReportReopenAfterApiConfig = true;
		_weeklyReportReopenAfterApiConfigUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(300.0).Ticks;
		if (!ModOnboardingBehavior.OpenApiRepairFlow())
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到 API 配置引导，请先检查 MCM 中的 Base URL、API Key 与模型名。"));
		}
	}

	private static Vec2? GetPlayerPartyPositionVec2()
	{
		try
		{
			CampaignVec2? campaignVec = MobileParty.MainParty?.Position;
			if (campaignVec.HasValue && campaignVec.Value.IsValid())
			{
				return campaignVec.Value.ToVec2();
			}
		}
		catch
		{
		}
		return null;
	}

	private static List<string> GetKingdomIdsByPlayerProximity(IEnumerable<string> kingdomIds)
	{
		List<string> list = new List<string>();
		Vec2? playerPartyPositionVec = GetPlayerPartyPositionVec2();
		if (!playerPartyPositionVec.HasValue)
		{
			return list;
		}
		Vec2 value = playerPartyPositionVec.Value;
		Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		try
		{
			HashSet<string> hashSet = new HashSet<string>((kingdomIds ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()), StringComparer.OrdinalIgnoreCase);
			foreach (Settlement item in Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				string text = ((item.MapFaction as Kingdom)?.StringId ?? item.OwnerClan?.Kingdom?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Contains(text))
				{
					continue;
				}
				Vec2 vec = item.GatePosition.ToVec2();
				float num = vec.x - value.x;
				float num2 = vec.y - value.y;
				float num3 = num * num + num2 * num2;
				if (!dictionary.TryGetValue(text, out var value2) || num3 < value2)
				{
					dictionary[text] = num3;
				}
			}
		}
		catch
		{
		}
		return dictionary.OrderBy((KeyValuePair<string, float> x) => x.Value).ThenBy((KeyValuePair<string, float> x) => x.Key, StringComparer.OrdinalIgnoreCase).Select((KeyValuePair<string, float> x) => x.Key).ToList();
	}

	private static List<WeeklyEventMaterialPreviewGroup> OrderWeeklyReportGenerationGroups(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		List<WeeklyEventMaterialPreviewGroup> list = (groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(list.Where((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeeklyEventMaterialPreviewGroup x) => x.KingdomId));
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < kingdomIdsByPlayerProximity.Count; i++)
		{
			string text = (kingdomIdsByPlayerProximity[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = i;
			}
		}
		return list.OrderBy(delegate(WeeklyEventMaterialPreviewGroup x)
		{
			if (string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
			{
				string text = (x.KingdomId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && dictionary.TryGetValue(text, out var value))
				{
					return (value == 0) ? 0 : (value + 1);
				}
				return 1000;
			}
			if (string.Equals((x.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
			{
				return 1;
			}
			return 2000;
		}).ThenBy((WeeklyEventMaterialPreviewGroup x) => x.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string BuildWeeklyReportSystemPrompt(WeeklyEventMaterialPreviewGroup group)
	{
		WeeklyReportPromptProfile weeklyReportPromptProfile = GetWeeklyReportPromptProfile();
		bool flag = string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		string text = flag ? "世界周报" : "王国周报";
		string text2 = string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "你的任务是根据本周素材，写出一篇宏观的大陆周报，总结这一周整个卡拉迪亚发生了什么。" : "你的任务是根据本周素材，写出一篇聚焦单个王国的周报，总结这一周这个王国发生了什么。";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("你是一名负责整理卡拉迪亚时局的史官。");
		stringBuilder.AppendLine("你不是在自由编造故事，而是在根据给定素材生成一篇流利、可信、克制的周报。");
		stringBuilder.AppendLine(text2);
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("写作要求：");
		stringBuilder.AppendLine("1. 必须覆盖本周素材中的关键变化，但不必逐条复述；允许将同类信息合并表达。");
		stringBuilder.AppendLine("2. 不要编造素材中没有明确支持的核心事实，不要把别国内容误写进本国周报。");
		stringBuilder.AppendLine("3. 如果素材偏零碎，应提炼成局势观察；如果素材很多，应归纳成若干主线。");
		stringBuilder.AppendLine("4. 文风应像编年史、政局纪要或贵族周报，清楚、流利、克制，不要写成小说对白。");
		stringBuilder.AppendLine("5. 不要使用系统术语、字段名、StableKey、素材标签或开发者说明。");
		stringBuilder.AppendLine("6. 标题要简洁，正文要完整，短摘要要适合后续注入 NPC prompt。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("篇幅要求：");
		stringBuilder.AppendLine($"- 当前档位：{weeklyReportPromptProfile.Label}");
		stringBuilder.AppendLine($"- 正文必须控制在 {weeklyReportPromptProfile.MinWords} 到 {weeklyReportPromptProfile.MaxWords} 字之间。");
		stringBuilder.AppendLine("- SHORT 短摘要必须控制在 20 到 140 字之间，且尽量写成一段紧凑事实摘要。");
		stringBuilder.AppendLine("- 素材少时靠近下限，素材多时靠近上限。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("输出格式：");
		stringBuilder.AppendLine("[TITLE]周报标题");
		stringBuilder.AppendLine("[SHORT]20-140字短摘要");
		stringBuilder.AppendLine("[REPORT]周报正文");
		stringBuilder.AppendLine("[TAGS]");
		stringBuilder.AppendLine(flag ? "STA+0" : "STAB_FLAT");
		stringBuilder.AppendLine("WAR+0");
		stringBuilder.AppendLine("TRE+0");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("如果素材不足以支持重大变化，也要如实写出本周局势概况，不要硬造戏剧化转折。");
		if (flag)
		{
			stringBuilder.AppendLine("TAGS 目前只是接口占位层，不要求改变任何游戏数值，但必须输出稳定、简短、可解析的标签文本。");
		}
		else
		{
			stringBuilder.AppendLine("对于王国周报，你必须在 [TAGS] 中追加且只能追加一个稳定度评级标签：STAB_DOWN_4、STAB_DOWN_3、STAB_DOWN_2、STAB_DOWN_1、STAB_FLAT、STAB_UP_1、STAB_UP_2、STAB_UP_3、STAB_UP_4。");
			stringBuilder.AppendLine("不要在标题、短摘要或正文中解释这个标签，也不要解释其后果。");
		}
		stringBuilder.AppendLine("不要输出除 [TITLE]、[SHORT]、[REPORT]、[TAGS] 之外的其他字段。");
		return stringBuilder.ToString().TrimEnd();
	}

	private string BuildWeeklyReportCurrentKingdomStabilityTierText(WeeklyEventMaterialPreviewGroup group)
	{
		if (!string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		Kingdom kingdom = FindKingdomById(group?.KingdomId);
		if (kingdom == null)
		{
			return "";
		}
		return "当前王国稳定度评级：" + GetKingdomStabilityTierText(GetKingdomStabilityValue(kingdom));
	}

	private string BuildWeeklyReportUserPrompt(WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay)
	{
		WeeklyReportPromptProfile weeklyReportPromptProfile = GetWeeklyReportPromptProfile();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【周报生成任务】");
		stringBuilder.AppendLine("当前要生成的是：" + (string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界周报" : "王国周报"));
		stringBuilder.AppendLine("当前周数：第 " + weekIndex + " 周");
		stringBuilder.AppendLine("本周取材区间：第 " + Math.Max(0, startDay) + " 日 到 第 " + Math.Max(startDay, endDay) + " 日");
		if (!string.IsNullOrWhiteSpace(group?.KingdomId))
		{
			stringBuilder.AppendLine("指定王国：" + ResolveKingdomDisplay(group.KingdomId));
		}
		string weeklyReportCurrentKingdomStabilityTierText = BuildWeeklyReportCurrentKingdomStabilityTierText(group);
		if (!string.IsNullOrWhiteSpace(weeklyReportCurrentKingdomStabilityTierText))
		{
			stringBuilder.AppendLine(weeklyReportCurrentKingdomStabilityTierText);
		}
		if (string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.AppendLine("隐藏稳定度评级要求：请只根据本周这个王国的整体局势，在 [TAGS] 中选择一个稳定度评级标签。");
			stringBuilder.AppendLine("若本周只是略有小问题，用 STAB_DOWN_1；若问题不小、明显走弱，用 STAB_DOWN_2；若形势恶劣，用 STAB_DOWN_3；若局势接近失控或已经失控，用 STAB_DOWN_4。");
			stringBuilder.AppendLine("若本周大体还行、略有起色，用 STAB_UP_1；若局势不错、明显改善，用 STAB_UP_2；若局势良好、政局趋稳，用 STAB_UP_3；若局势极佳、统治明显巩固，用 STAB_UP_4。");
			stringBuilder.AppendLine("若本周总体只是延续旧势，没有足够明显的改善或恶化，就用 STAB_FLAT。");
			stringBuilder.AppendLine("不要解释标签含义，不要在正文里提到你做了评级。");
		}
		stringBuilder.AppendLine("篇幅档位：" + weeklyReportPromptProfile.Label);
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("【上一周周报】");
		stringBuilder.AppendLine(GetPreviousWeeklyReportText(group, weekIndex));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【本周素材】");
		stringBuilder.AppendLine(BuildWeeklyReportMaterialLines(group));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("请将这些素材融合成一篇流利的周报，既不要逐条照抄，也不要漏掉本周显著的大事。");
		return stringBuilder.ToString().TrimEnd();
	}

	private string GetPreviousWeeklyReportText(WeeklyEventMaterialPreviewGroup group, int currentWeekIndex)
	{
		string text = (group?.GroupKind ?? "").Trim();
		string text2 = (group?.KingdomId ?? "").Trim();
		EventRecordEntry eventRecordEntry = SanitizeEventRecordEntries(_eventRecordEntries).FirstOrDefault((EventRecordEntry x) => x != null && x.WeekIndex == currentWeekIndex - 1 && string.Equals((x.EventKind ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) && string.Equals((x.ScopeKingdomId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
		if (eventRecordEntry == null || string.IsNullOrWhiteSpace(eventRecordEntry.Summary))
		{
			return "无";
		}
		return (eventRecordEntry.Title ?? "").Trim() + "\n" + eventRecordEntry.Summary.Trim();
	}

	private static string BuildWeeklyReportMaterialLines(WeeklyEventMaterialPreviewGroup group)
	{
		List<EventMaterialReference> list = OrderWeeklyPreviewMaterials(group?.Materials).ToList();
		if (list.Count == 0)
		{
			return "无可用素材。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (EventMaterialReference item in list)
		{
			if (item == null)
			{
				continue;
			}
			string weeklyReportMaterialLine = BuildWeeklyReportMaterialLine(item);
			if (!string.IsNullOrWhiteSpace(weeklyReportMaterialLine))
			{
				stringBuilder.AppendLine("- " + weeklyReportMaterialLine.Trim());
			}
		}
		string text = stringBuilder.ToString().TrimEnd();
		return string.IsNullOrWhiteSpace(text) ? "无可用素材。" : text;
	}

	private static string BuildWeeklyReportMaterialLine(EventMaterialReference material)
	{
		if (material == null)
		{
			return "";
		}
		string text = (material.MaterialType ?? "").Trim().ToLowerInvariant();
		if (text == "world_opening_summary")
		{
			return "世界开局概要：" + (material.SnapshotText ?? "").Trim();
		}
		if (text == "kingdom_opening_summary")
		{
			return ResolveKingdomDisplay(material.KingdomId) + "的开局概要：" + (material.SnapshotText ?? "").Trim();
		}
		string text2 = (material.SnapshotText ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return (material.Label ?? "").Trim();
	}

	private static string BuildWeeklyReportPromptPreviewText(WeeklyEventMaterialPreviewGroup group, string systemPrompt, string userPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "生成对象", string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界周报" : "王国周报");
		AppendDevNpcActionField(stringBuilder, "关联王国", ResolveKingdomDisplay(group?.KingdomId));
		AppendDevNpcActionField(stringBuilder, "篇幅档位", GetWeeklyReportPromptProfile().Label);
		AppendDevNpcActionField(stringBuilder, "MaxTokens", WeeklyReportRequestMaxTokens.ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【System Prompt】");
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【User Prompt】");
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildWeeklyBatchReportSystemPrompt()
	{
		WeeklyReportPromptProfile weeklyReportPromptProfile = GetWeeklyReportPromptProfile();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("你是一名负责整理卡拉迪亚时局的史官。");
		stringBuilder.AppendLine("你会一次收到多个周报目标，每个目标要么是世界周报，要么是单个王国周报。");
		stringBuilder.AppendLine("你必须严格按输入 block 分别生成，不得漏块，不得串写，不得合并不同 report_id。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("写作要求：");
		stringBuilder.AppendLine("1. 每个 block 只根据该 block 提供的上一周周报、稳定度说明和本周素材生成。");
		stringBuilder.AppendLine("2. 跨国事件只能从当前 block 的视角组织，不得把别国素材误写为本国主体。");
		stringBuilder.AppendLine("3. 不要编造输入素材中没有明确支持的核心事实。");
		stringBuilder.AppendLine("4. 文风应像编年史、政局纪要或贵族周报，清楚、流利、克制。");
		stringBuilder.AppendLine("5. 不要使用系统术语、字段名、StableKey、素材标签或开发者说明。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("篇幅要求：");
		stringBuilder.AppendLine($"- 当前档位：{weeklyReportPromptProfile.Label}");
		stringBuilder.AppendLine($"- 每个 block 的正文必须控制在 {weeklyReportPromptProfile.MinWords} 到 {weeklyReportPromptProfile.MaxWords} 字之间。");
		stringBuilder.AppendLine("- 每个 block 的 short 必须控制在 20 到 140 字之间。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("输出要求：");
		stringBuilder.AppendLine("1. 必须按输入顺序输出全部 block。");
		stringBuilder.AppendLine("2. 每个 block 都必须使用以下格式：");
		stringBuilder.AppendLine("[REPORT_BLOCK_BEGIN]");
		stringBuilder.AppendLine("report_id=原样返回输入中的 report_id");
		stringBuilder.AppendLine("kind=world 或 kingdom");
		stringBuilder.AppendLine("kingdom_id=如果 kind=kingdom 则原样返回输入中的 kingdom_id；如果 kind=world 则留空");
		stringBuilder.AppendLine("[TITLE]标题");
		stringBuilder.AppendLine("[SHORT]短摘要");
		stringBuilder.AppendLine("[REPORT]正文");
		stringBuilder.AppendLine("[TAGS]");
		stringBuilder.AppendLine("标签文本");
		stringBuilder.AppendLine("[REPORT_BLOCK_END]");
		stringBuilder.AppendLine("3. world block 与 kingdom block 都必须输出 [TITLE] [SHORT] [REPORT] [TAGS]。");
		stringBuilder.AppendLine("4. kingdom block 的 [TAGS] 中必须且只能包含一个稳定度评级标签：STAB_DOWN_4、STAB_DOWN_3、STAB_DOWN_2、STAB_DOWN_1、STAB_FLAT、STAB_UP_1、STAB_UP_2、STAB_UP_3、STAB_UP_4。");
		stringBuilder.AppendLine("5. world block 的 [TAGS] 保持简短可解析即可。");
		stringBuilder.AppendLine("6. 不要输出 [REPORT_BLOCK_BEGIN]/[REPORT_BLOCK_END] 之外的额外说明。");
		return stringBuilder.ToString().TrimEnd();
	}

	private string BuildWeeklyBatchReportUserPrompt(WeeklyReportBatchRequest batch)
	{
		StringBuilder stringBuilder = new StringBuilder();
		List<WeeklyEventMaterialPreviewGroup> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		stringBuilder.AppendLine("[BATCH]");
		stringBuilder.AppendLine("week_index=" + ((batch != null) ? batch.WeekIndex : 0));
		stringBuilder.AppendLine("day_range=" + ((batch != null) ? batch.StartDay : 0) + "-" + ((batch != null) ? batch.EndDay : 0));
		stringBuilder.AppendLine("block_count=" + list.Count);
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(BuildWeeklyBatchPromptInputBlock(item, (batch != null) ? batch.WeekIndex : 0));
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private string BuildWeeklyBatchPromptInputBlock(WeeklyEventMaterialPreviewGroup group, int weekIndex)
	{
		bool flag = string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(flag ? "[WORLD_BLOCK]" : "[KINGDOM_BLOCK]");
		stringBuilder.AppendLine("report_id=" + BuildWeeklyReportGroupReportId(group));
		stringBuilder.AppendLine("kind=" + (flag ? "world" : "kingdom"));
		if (!flag)
		{
			stringBuilder.AppendLine("kingdom_id=" + ((group?.KingdomId ?? "").Trim()));
			stringBuilder.AppendLine("kingdom_name=" + ResolveKingdomDisplay(group?.KingdomId));
			string weeklyReportCurrentKingdomStabilityTierText = BuildWeeklyReportCurrentKingdomStabilityTierText(group);
			if (!string.IsNullOrWhiteSpace(weeklyReportCurrentKingdomStabilityTierText))
			{
				stringBuilder.AppendLine("stability=" + weeklyReportCurrentKingdomStabilityTierText.Replace("当前王国稳定度评级：", "").Trim());
			}
		}
		stringBuilder.AppendLine("title_hint=" + BuildDefaultWeeklyReportTitle(group, weekIndex));
		stringBuilder.AppendLine("previous_report=");
		stringBuilder.AppendLine(GetPreviousWeeklyReportText(group, weekIndex));
		stringBuilder.AppendLine("materials=");
		stringBuilder.AppendLine(BuildWeeklyReportMaterialLines(group));
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildWeeklyBatchPromptPreviewText(WeeklyReportBatchRequest batch, string systemPrompt, string userPrompt)
	{
		StringBuilder stringBuilder = new StringBuilder();
		List<WeeklyEventMaterialPreviewGroup> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		AppendDevNpcActionField(stringBuilder, "批次周数", ((batch != null) ? batch.WeekIndex : 0).ToString());
		AppendDevNpcActionField(stringBuilder, "取材区间", ((batch != null) ? batch.StartDay : 0) + "-" + ((batch != null) ? batch.EndDay : 0));
		AppendDevNpcActionField(stringBuilder, "批次块数", list.Count.ToString());
		AppendDevNpcActionField(stringBuilder, "批次对象", string.Join(" | ", list.Select(BuildWeeklyReportGroupReportId).Where((string x) => !string.IsNullOrWhiteSpace(x))));
		AppendDevNpcActionField(stringBuilder, "MaxTokens", WeeklyReportRequestMaxTokens.ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【System Prompt】");
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【User Prompt】");
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
	}

	private static bool TryParseWeeklyBatchResponse(string rawResponse, WeeklyReportBatchRequest batch, out List<WeeklyReportBatchBlockResult> blocks, out List<string> missingReportIds, out string failureReason)
	{
		blocks = new List<WeeklyReportBatchBlockResult>();
		missingReportIds = new List<string>();
		failureReason = "";
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			failureReason = "批量周报响应为空。";
			missingReportIds = BuildWeeklyBatchExpectedReportIds(batch);
			return false;
		}
		Dictionary<string, WeeklyEventMaterialPreviewGroup> weeklyReportGroupMap = BuildWeeklyReportGroupMap(batch?.Groups);
		Dictionary<string, WeeklyReportBatchBlockResult> dictionary = new Dictionary<string, WeeklyReportBatchBlockResult>(StringComparer.OrdinalIgnoreCase);
		MatchCollection matchCollection = Regex.Matches(text, "\\[REPORT_BLOCK_BEGIN\\]\\s*(?<body>[\\s\\S]*?)\\s*\\[REPORT_BLOCK_END\\]", RegexOptions.IgnoreCase);
		if (matchCollection.Count == 0)
		{
			failureReason = "响应中没有找到任何 REPORT_BLOCK。";
			missingReportIds = BuildWeeklyBatchExpectedReportIds(batch);
			return false;
		}
		foreach (Match item in matchCollection)
		{
			WeeklyReportBatchBlockResult weeklyReportBatchBlockResult;
			string text2 = (item.Groups["body"]?.Value ?? "").Trim();
			if (!TryParseWeeklyBatchBlock(text2, batch, weeklyReportGroupMap, out weeklyReportBatchBlockResult))
			{
				weeklyReportBatchBlockResult = weeklyReportBatchBlockResult ?? new WeeklyReportBatchBlockResult();
			}
			if (!string.IsNullOrWhiteSpace(weeklyReportBatchBlockResult.ReportId) && !dictionary.ContainsKey(weeklyReportBatchBlockResult.ReportId))
			{
				dictionary[weeklyReportBatchBlockResult.ReportId] = weeklyReportBatchBlockResult;
			}
			blocks.Add(weeklyReportBatchBlockResult);
		}
		foreach (string item2 in BuildWeeklyBatchExpectedReportIds(batch))
		{
			if (!dictionary.ContainsKey(item2) || !dictionary[item2].Parsed)
			{
				missingReportIds.Add(item2);
			}
		}
		if (missingReportIds.Count > 0)
		{
			failureReason = "批量周报响应缺少部分 report_id：" + string.Join("、", missingReportIds);
		}
		return blocks.Any((WeeklyReportBatchBlockResult x) => x != null && x.Parsed);
	}

	private static bool TryParseWeeklyBatchBlock(string rawBlockBody, WeeklyReportBatchRequest batch, Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap, out WeeklyReportBatchBlockResult block)
	{
		block = new WeeklyReportBatchBlockResult();
		string text = (rawBlockBody ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			block.FailureReason = "空 block。";
			return false;
		}
		block.ReportId = ExtractWeeklyBatchHeaderValue(text, "report_id");
		block.Kind = ExtractWeeklyBatchHeaderValue(text, "kind");
		block.KingdomId = ExtractWeeklyBatchHeaderValue(text, "kingdom_id");
		if (string.IsNullOrWhiteSpace(block.ReportId))
		{
			block.FailureReason = "缺少 report_id。";
			return false;
		}
		if (groupMap == null || !groupMap.TryGetValue(block.ReportId, out var value) || value == null)
		{
			block.FailureReason = "report_id 未在输入批次中找到：" + block.ReportId;
			return false;
		}
		if (string.Equals((block.Kind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = (value.KingdomId ?? "").Trim();
			if (string.IsNullOrWhiteSpace((block.KingdomId ?? "").Trim()) || !string.Equals((block.KingdomId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase))
			{
				block.FailureReason = "kingdom_id 与输入批次不一致。";
				return false;
			}
		}
		string text3 = Regex.Replace(text, "^report_id=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		text3 = Regex.Replace(text3, "^kind=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		text3 = Regex.Replace(text3, "^kingdom_id=.*?$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase).Trim();
		if (!TryParseWeeklyReportResponse(text3, value, (batch != null) ? batch.WeekIndex : 0, out var title, out var shortSummary, out var report, out var tagText))
		{
			block.FailureReason = BuildWeeklyReportFailureReason(text3, parseFailed: true);
			return false;
		}
		block.Title = title;
		block.ShortSummary = shortSummary;
		block.Report = report;
		block.TagText = tagText;
		block.Parsed = true;
		return true;
	}

	private static string ExtractWeeklyBatchHeaderValue(string text, string key)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(key))
		{
			return "";
		}
		Match match = Regex.Match(text, "^" + Regex.Escape(key.Trim()) + "=(?<value>.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
		return match.Success ? ((match.Groups["value"]?.Value ?? "").Trim()) : "";
	}

	private static bool TryParseWeeklyReportResponse(string rawResponse, WeeklyEventMaterialPreviewGroup group, int weekIndex, out string title, out string shortSummary, out string report, out string tagText)
	{
		title = "";
		shortSummary = "";
		report = "";
		tagText = "";
		string text = (rawResponse ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Match match = Regex.Match(text, "\\[TITLE\\](?<title>[\\s\\S]*?)(?=\\[SHORT\\]|\\[REPORT\\]|\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match2 = Regex.Match(text, "\\[SHORT\\](?<short>[\\s\\S]*?)(?=\\[REPORT\\]|\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match3 = Regex.Match(text, "\\[REPORT\\](?<report>[\\s\\S]*?)(?=\\[TAGS\\]|$)", RegexOptions.IgnoreCase);
		Match match4 = Regex.Match(text, "\\[TAGS\\](?<tags>[\\s\\S]*)$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			title = (match.Groups["title"]?.Value ?? "").Trim();
		}
		if (match2.Success)
		{
			shortSummary = BuildFallbackWeeklyReportShortSummary(match2.Groups["short"]?.Value ?? "");
		}
		if (match4.Success)
		{
			tagText = NormalizeWeeklyReportTagText(match4.Groups["tags"]?.Value ?? "");
		}
		if (match3.Success)
		{
			report = (match3.Groups["report"]?.Value ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(report))
		{
			report = text;
		}
		if (string.IsNullOrWhiteSpace(shortSummary))
		{
			shortSummary = BuildFallbackWeeklyReportShortSummary(report);
		}
		if (string.IsNullOrWhiteSpace(title))
		{
			title = BuildDefaultWeeklyReportTitle(group, weekIndex);
		}
		return !string.IsNullOrWhiteSpace(report);
	}

	private static string BuildDefaultWeeklyReportTitle(WeeklyEventMaterialPreviewGroup group, int weekIndex)
	{
		if (string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase))
		{
			return "第" + weekIndex + "周世界周报";
		}
		string text = ResolveKingdomDisplay(group?.KingdomId);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "某王国";
		}
		return text + "第" + weekIndex + "周周报";
	}

	private void ApplyWeeklyReportStabilityDelta(WeeklyEventMaterialPreviewGroup group, string eventId, string tagText)
	{
		if (_weeklyReportAppliedStabilityDeltas == null)
		{
			_weeklyReportAppliedStabilityDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		string text = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		int value = 0;
		_weeklyReportAppliedStabilityDeltas.TryGetValue(text, out value);
		int num = 0;
		Kingdom kingdom = null;
		if (string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			kingdom = FindKingdomById(group?.KingdomId);
			if (kingdom != null)
			{
				num = ExtractWeeklyReportStabilityDelta(tagText);
			}
		}
		if (num == value)
		{
			return;
		}
		if (kingdom == null)
		{
			return;
		}
		SetKingdomStabilityValue(kingdom, GetKingdomStabilityValue(kingdom) - value + num);
		if (num == 0)
		{
			_weeklyReportAppliedStabilityDeltas.Remove(text);
		}
		else
		{
			_weeklyReportAppliedStabilityDeltas[text] = num;
		}
	}

	private void UpsertWeeklyReportEventRecord(WeeklyEventMaterialPreviewGroup group, int weekIndex, string title, string shortSummary, string report, string tagText, string promptText)
	{
		if (_eventRecordEntries == null)
		{
			_eventRecordEntries = new List<EventRecordEntry>();
		}
		string text = (group?.GroupKind ?? "").Trim().ToLowerInvariant();
		string text2 = (group?.KingdomId ?? "").Trim();
		string text3 = "weekly_report:" + text + ":" + weekIndex + ":" + text2;
		EventRecordEntry eventRecordEntry = _eventRecordEntries.FirstOrDefault((EventRecordEntry x) => x != null && string.Equals((x.EventId ?? "").Trim(), text3, StringComparison.OrdinalIgnoreCase));
		if (eventRecordEntry == null)
		{
			eventRecordEntry = new EventRecordEntry
			{
				EventId = text3
			};
			_eventRecordEntries.Add(eventRecordEntry);
		}
		eventRecordEntry.EventKind = text;
		eventRecordEntry.ScopeKingdomId = text2;
		eventRecordEntry.WeekIndex = weekIndex;
		eventRecordEntry.Title = (title ?? "").Trim();
		eventRecordEntry.ShortSummary = BuildFallbackWeeklyReportShortSummary(shortSummary);
		eventRecordEntry.Summary = (report ?? "").Trim();
		if (string.IsNullOrWhiteSpace(eventRecordEntry.ShortSummary))
		{
			eventRecordEntry.ShortSummary = BuildFallbackWeeklyReportShortSummary(eventRecordEntry.Summary);
		}
		eventRecordEntry.TagText = NormalizeWeeklyReportTagText(tagText);
		eventRecordEntry.PromptText = (promptText ?? "").Trim();
		eventRecordEntry.CreatedDay = GetCurrentGameDayIndexSafe();
		eventRecordEntry.CreatedDate = GetCurrentGameDateTextSafe();
		eventRecordEntry.Materials = OrderWeeklyPreviewMaterials(group?.Materials).Select(delegate(EventMaterialReference x)
		{
			if (x == null)
			{
				return null;
			}
			return new EventMaterialReference
			{
				MaterialType = (x.MaterialType ?? "").Trim(),
				Label = (x.Label ?? "").Trim(),
				SnapshotText = (x.SnapshotText ?? "").Trim(),
				HeroId = (x.HeroId ?? "").Trim(),
				KingdomId = (x.KingdomId ?? "").Trim(),
				SettlementId = (x.SettlementId ?? "").Trim(),
				RecentOnly = x.RecentOnly,
				ActionKind = (x.ActionKind ?? "").Trim(),
				ActorHeroId = (x.ActorHeroId ?? "").Trim(),
				ActorClanId = (x.ActorClanId ?? "").Trim(),
				ActorKingdomId = (x.ActorKingdomId ?? "").Trim(),
				TargetHeroId = (x.TargetHeroId ?? "").Trim(),
				TargetClanId = (x.TargetClanId ?? "").Trim(),
				TargetKingdomId = (x.TargetKingdomId ?? "").Trim(),
				SettlementOwnerClanId = (x.SettlementOwnerClanId ?? "").Trim(),
				SettlementOwnerKingdomId = (x.SettlementOwnerKingdomId ?? "").Trim(),
				PreviousSettlementOwnerClanId = (x.PreviousSettlementOwnerClanId ?? "").Trim(),
				PreviousSettlementOwnerKingdomId = (x.PreviousSettlementOwnerKingdomId ?? "").Trim(),
				RelatedHeroIds = new List<string>((x.RelatedHeroIds ?? new List<string>()).Where((string y) => !string.IsNullOrWhiteSpace(y)).Select((string y) => y.Trim())),
				RelatedClanIds = new List<string>((x.RelatedClanIds ?? new List<string>()).Where((string y) => !string.IsNullOrWhiteSpace(y)).Select((string y) => y.Trim())),
				RelatedKingdomIds = new List<string>((x.RelatedKingdomIds ?? new List<string>()).Where((string y) => !string.IsNullOrWhiteSpace(y)).Select((string y) => y.Trim())),
				SourceStableKeys = new List<string>((x.SourceStableKeys ?? new List<string>()).Where((string y) => !string.IsNullOrWhiteSpace(y)).Select((string y) => y.Trim())),
				SourceActionKinds = new List<string>((x.SourceActionKinds ?? new List<string>()).Where((string y) => !string.IsNullOrWhiteSpace(y)).Select((string y) => y.Trim())),
				SourceMaterialCount = Math.Max(0, x.SourceMaterialCount),
				ActionStableKey = (x.ActionStableKey ?? "").Trim(),
				ActionDay = x.ActionDay,
				ActionOrder = x.ActionOrder,
				ActionSequence = x.ActionSequence
			};
		}).Where((EventMaterialReference x) => x != null).ToList();
		ApplyWeeklyReportStabilityDelta(group, eventRecordEntry.EventId, eventRecordEntry.TagText);
		_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
	}

	private WeeklyEventMaterialPreviewGroup BuildWorldWeeklyEventMaterialPreviewGroup(int startDay, int endDay)
	{
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = new WeeklyEventMaterialPreviewGroup
		{
			GroupKind = "world",
			Title = "世界事件素材预览",
			Summary = "用于世界事件的本周素材。",
			Materials = new List<EventMaterialReference>()
		};
		if (!string.IsNullOrWhiteSpace(_eventWorldOpeningSummary))
		{
			weeklyEventMaterialPreviewGroup.Materials.Add(new EventMaterialReference
			{
				MaterialType = "world_opening_summary",
				Label = "世界开局概要",
				SnapshotText = (_eventWorldOpeningSummary ?? "").Trim()
			});
		}
		foreach (EventSourceMaterialEntry item in SanitizeEventSourceMaterials(_eventSourceMaterials))
		{
			if (item != null && item.IncludeInWorld && item.Day >= startDay && item.Day <= endDay)
			{
				TryAddPreviewSourceMaterial(weeklyEventMaterialPreviewGroup.Materials, item);
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcRecentAction in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = FindHeroById(npcRecentAction.Key);
			if (hero == null || npcRecentAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item in npcRecentAction.Value)
			{
				if (item != null && item.Day >= startDay && item.Day <= endDay && ShouldIncludeWorldPreviewAction(hero, item))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item, recentOnly: true);
				}
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcMajorAction in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = FindHeroById(npcMajorAction.Key);
			if (hero == null || npcMajorAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item in npcMajorAction.Value)
			{
				if (item != null && item.Day >= startDay && item.Day <= endDay && ShouldIncludeWorldPreviewAction(hero, item))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item, recentOnly: false);
				}
			}
		}
		weeklyEventMaterialPreviewGroup.Summary = "世界事件将使用世界开局概要，以及本周更能代表大陆格局变化的重大行动、领导层震荡与世界级通用素材。军团从属加入或离开这类细碎动作会被压缩。";
		return weeklyEventMaterialPreviewGroup;
	}

	private WeeklyEventMaterialPreviewGroup BuildKingdomWeeklyEventMaterialPreviewGroup(Kingdom kingdom, int startDay, int endDay)
	{
		string text = kingdom?.Name?.ToString() ?? (kingdom?.StringId ?? "王国");
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = new WeeklyEventMaterialPreviewGroup
		{
			GroupKind = "kingdom",
			KingdomId = kingdom?.StringId ?? "",
			Title = text + " 事件素材预览",
			Summary = text + " 本周会使用该国开局概要，以及与该国有关的本周高价值行动。普通行军会被压缩，守备、袭扰和外国领主入境会优先保留。",
			Materials = new List<EventMaterialReference>()
		};
		string kingdomOpeningSummary = GetKingdomOpeningSummary(kingdom);
		if (!string.IsNullOrWhiteSpace(kingdomOpeningSummary))
		{
			weeklyEventMaterialPreviewGroup.Materials.Add(new EventMaterialReference
			{
				MaterialType = "kingdom_opening_summary",
				Label = text + " 开局概要",
				KingdomId = kingdom?.StringId ?? "",
				SnapshotText = kingdomOpeningSummary
			});
		}
		foreach (EventSourceMaterialEntry item in SanitizeEventSourceMaterials(_eventSourceMaterials))
		{
			if (item != null && item.IncludeInKingdom && item.Day >= startDay && item.Day <= endDay && string.Equals((item.KingdomId ?? "").Trim(), (kingdom?.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				TryAddPreviewSourceMaterial(weeklyEventMaterialPreviewGroup.Materials, item);
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcRecentAction in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = FindHeroById(npcRecentAction.Key);
			if (hero == null || npcRecentAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item in npcRecentAction.Value)
			{
				if (item != null && item.Day >= startDay && item.Day <= endDay && DoesNpcActionRelateToKingdom(item, kingdom) && ShouldIncludeKingdomPreviewAction(hero, item, kingdom))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item, recentOnly: true);
				}
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcMajorAction in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = FindHeroById(npcMajorAction.Key);
			if (hero == null || npcMajorAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item2 in npcMajorAction.Value)
			{
				if (item2 != null && item2.Day >= startDay && item2.Day <= endDay && DoesNpcActionRelateToKingdom(item2, kingdom))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item2, recentOnly: false);
				}
			}
		}
		return weeklyEventMaterialPreviewGroup;
	}

	private static Hero FindHeroById(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Hero.FindFirst((Hero h) => h != null && string.Equals((h.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static bool DoesNpcActionRelateToKingdom(NpcActionEntry entry, Kingdom kingdom)
	{
		string text = (kingdom?.StringId ?? "").Trim();
		if (entry == null || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (string.Equals((entry.ActorKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((entry.TargetKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((entry.SettlementOwnerKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((entry.PreviousSettlementOwnerKingdomId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return entry.RelatedKingdomIds != null && entry.RelatedKingdomIds.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

	private static bool ShouldIncludeKingdomPreviewAction(Hero hero, NpcActionEntry entry, Kingdom kingdom)
	{
		if (hero == null || entry == null || kingdom == null)
		{
			return false;
		}
		string text = (entry.ActionKind ?? "").Trim();
		if (!string.Equals(text, "daily_behavior", StringComparison.OrdinalIgnoreCase))
		{
			if (string.Equals(text, "army_join", StringComparison.OrdinalIgnoreCase))
			{
				return hero.Clan?.Leader == hero;
			}
			return true;
		}
		if (IsArmyCommanderDailyBehavior(entry))
		{
			return true;
		}
		string text2 = (entry.StableKey ?? "").Trim();
		if (text2.IndexOf("raidsettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("袭扰", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text2.IndexOf("defendsettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("守备", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("保卫", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text3 = (kingdom.StringId ?? "").Trim();
		string text4 = (entry.ActorKingdomId ?? "").Trim();
		string text5 = (entry.SettlementOwnerKingdomId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text4) && !string.Equals(text4, text3, StringComparison.OrdinalIgnoreCase) && string.Equals(text5, text3, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	private static bool ShouldIncludeWorldPreviewAction(Hero hero, NpcActionEntry entry)
	{
		if (hero == null || entry == null)
		{
			return false;
		}
		string text = (entry.ActionKind ?? "").Trim();
		if (string.Equals(text, "army_join", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "army_leave", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(text, "daily_behavior", StringComparison.OrdinalIgnoreCase))
		{
			return IsArmyCommanderDailyBehavior(entry) || IsDailyBehaviorDefend(entry);
		}
		if (IsPrisonerTakenAction(entry))
		{
			return IsLeadershipCaptureAction(entry);
		}
		if (IsPrisonerReleasedAction(entry))
		{
			return IsLeadershipReleaseAction(entry);
		}
		return true;
	}

	private static bool IsPrisonerTakenAction(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		return string.Equals(text, "prisoner_taken_captor", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "prisoner_taken_prisoner", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsLeadershipCaptureAction(NpcActionEntry entry)
	{
		Hero capturedHero = FindHeroById(GetCapturedHeroId(entry));
		return capturedHero != null && (capturedHero.Clan?.Leader == capturedHero || capturedHero.IsFactionLeader);
	}

	private static bool IsPrisonerReleasedAction(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		return string.Equals(text, "prisoner_released_captor", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsLeadershipReleaseAction(NpcActionEntry entry)
	{
		Hero releasedHero = FindHeroById(GetReleasedHeroId(entry));
		return releasedHero != null && (releasedHero.Clan?.Leader == releasedHero || releasedHero.IsFactionLeader);
	}

	private static string GetCapturedHeroId(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(text, "prisoner_taken_captor", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.TargetHeroId ?? "").Trim();
		}
		if (string.Equals(text, "prisoner_taken_prisoner", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.ActorHeroId ?? "").Trim();
		}
		return "";
	}

	private static string GetReleasedHeroId(NpcActionEntry entry)
	{
		string text = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(text, "prisoner_released_captor", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.TargetHeroId ?? "").Trim();
		}
		if (string.Equals(text, "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.ActorHeroId ?? "").Trim();
		}
		return "";
	}

	private static bool IsArmyCommanderDailyBehavior(NpcActionEntry entry)
	{
		string text = (entry?.StableKey ?? "").Trim();
		if (!text.StartsWith("daily_behavior:army:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text2 = (entry?.Text ?? "").Trim();
		return text2.IndexOf("率领", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsDailyBehaviorRaid(NpcActionEntry entry)
	{
		string text = (entry?.StableKey ?? "").Trim();
		if (text.IndexOf("raidsettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text2 = (entry?.Text ?? "").Trim();
		return text2.IndexOf("袭扰", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsDailyBehaviorDefend(NpcActionEntry entry)
	{
		string text = (entry?.StableKey ?? "").Trim();
		if (text.IndexOf("defendsettlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text2 = (entry?.Text ?? "").Trim();
		return text2.IndexOf("守备", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("保卫", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void TryAddPreviewActionMaterial(List<EventMaterialReference> materials, Hero hero, NpcActionEntry entry, bool recentOnly)
	{
		if (materials == null || hero == null || entry == null)
		{
			return;
		}
		EventMaterialReference eventMaterialReference = materials.FirstOrDefault((EventMaterialReference x) => x != null && string.Equals((x.HeroId ?? "").Trim(), (hero.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) && x.ActionDay.GetValueOrDefault() == entry.Day && string.Equals((x.ActionStableKey ?? "").Trim(), (entry.StableKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		string text = ResolveHeroDisplay(hero.StringId) + " - " + BuildDevSummaryPreview(BuildDevNpcActionPreviewText(entry), 60);
		EventMaterialReference eventMaterialReference2 = new EventMaterialReference
		{
			MaterialType = recentOnly ? "npc_recent_action" : "npc_major_action",
			Label = text,
			SnapshotText = BuildDevNpcActionPreviewText(entry),
			HeroId = hero.StringId ?? "",
			KingdomId = entry.ActorKingdomId ?? "",
			SettlementId = entry.SettlementId ?? "",
			RecentOnly = recentOnly,
			ActionKind = entry.ActionKind ?? "",
			ActorHeroId = entry.ActorHeroId ?? "",
			ActorClanId = entry.ActorClanId ?? "",
			ActorKingdomId = entry.ActorKingdomId ?? "",
			TargetHeroId = entry.TargetHeroId ?? "",
			TargetClanId = entry.TargetClanId ?? "",
			TargetKingdomId = entry.TargetKingdomId ?? "",
			SettlementOwnerClanId = entry.SettlementOwnerClanId ?? "",
			SettlementOwnerKingdomId = entry.SettlementOwnerKingdomId ?? "",
			PreviousSettlementOwnerClanId = entry.PreviousSettlementOwnerClanId ?? "",
			PreviousSettlementOwnerKingdomId = entry.PreviousSettlementOwnerKingdomId ?? "",
			RelatedHeroIds = new List<string>((entry.RelatedHeroIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedClanIds = new List<string>((entry.RelatedClanIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedKingdomIds = new List<string>((entry.RelatedKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceStableKeys = new List<string>(),
			SourceActionKinds = new List<string>(),
			SourceMaterialCount = 1,
			ActionStableKey = entry.StableKey ?? "",
			ActionDay = entry.Day,
			ActionOrder = entry.Order,
			ActionSequence = entry.Sequence
		};
		AddUniqueId(eventMaterialReference2.SourceStableKeys, eventMaterialReference2.ActionStableKey);
		AddUniqueId(eventMaterialReference2.SourceActionKinds, eventMaterialReference2.ActionKind);
		if (eventMaterialReference != null)
		{
			if (!recentOnly && eventMaterialReference.RecentOnly)
			{
				int num = materials.IndexOf(eventMaterialReference);
				if (num >= 0)
				{
					materials[num] = eventMaterialReference2;
				}
			}
			return;
		}
		materials.Add(eventMaterialReference2);
	}

	private static void TryAddPreviewSourceMaterial(List<EventMaterialReference> materials, EventSourceMaterialEntry entry)
	{
		if (materials == null || entry == null)
		{
			return;
		}
		if (materials.Any((EventMaterialReference x) => x != null && string.Equals((x.MaterialType ?? "").Trim(), "raw_text", StringComparison.OrdinalIgnoreCase) && x.ActionDay.GetValueOrDefault() == entry.Day && x.ActionSequence.GetValueOrDefault() == entry.Sequence && string.Equals((x.ActionStableKey ?? "").Trim(), (entry.StableKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		materials.Add(new EventMaterialReference
		{
			MaterialType = "raw_text",
			Label = (entry.Label ?? "").Trim(),
			SnapshotText = (entry.SnapshotText ?? "").Trim(),
			KingdomId = (entry.KingdomId ?? "").Trim(),
			SettlementId = (entry.SettlementId ?? "").Trim(),
			SourceStableKeys = new List<string>(),
			SourceActionKinds = new List<string>(),
			SourceMaterialCount = 1,
			ActionStableKey = (entry.StableKey ?? "").Trim(),
			ActionDay = entry.Day,
			ActionSequence = entry.Sequence
		});
		EventMaterialReference eventMaterialReference = materials[materials.Count - 1];
		AddUniqueId(eventMaterialReference.SourceStableKeys, eventMaterialReference.ActionStableKey);
	}

	private void OpenDevWeeklyEventMaterialPreviewGroupDetail(WeeklyEventMaterialPreviewGroup group, int page)
	{
		if (group == null)
		{
			OpenDevWeeklyEventMaterialPreviewMenu();
			return;
		}
		List<EventMaterialReference> list = OrderWeeklyPreviewMaterials(group.Materials).ToList();
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 16;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回分组列表", null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", "下一页", null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventMaterialReference item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, BuildWeeklyPreviewMaterialLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(group.Title ?? "素材预览", BuildWeeklyPreviewGroupDetailText(group, page, num), list2, isExitShown: true, 0, 1, "查看素材", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevWeeklyEventMaterialPreviewMenu();
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "back":
					OpenDevWeeklyEventMaterialPreviewMenu();
					break;
				case "prev_page":
					OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page - 1);
					break;
				case "next_page":
					OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page + 1);
					break;
				default:
					OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page);
					break;
				}
			}
			else if (selected[0].Identifier is EventMaterialReference eventMaterialReference)
			{
				OpenDevWeeklyPreviewMaterialDetail(group, eventMaterialReference, page);
			}
			else
			{
				OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page);
			}
		}, delegate
		{
			OpenDevWeeklyEventMaterialPreviewMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildWeeklyPreviewMaterialLabel(EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材";
		}
		string text = TranslateEventMaterialTypeForDev(material.MaterialType);
		string text6 = material.ActionDay.HasValue ? ("[第" + material.ActionDay.Value + "日] ") : "";
		if ((material.MaterialType ?? "").Trim().StartsWith("npc_", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = ResolveHeroDisplay(material.HeroId);
			string text3 = ResolveSettlementDisplay(material.SettlementId);
			string text4 = BuildDevSummaryPreview(material.SnapshotText, 18);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? "某领主" : text2) + " - " + text3;
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? "某领主" : text2) + " - " + text4;
			}
			return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text2) ? "某领主" : text2);
		}
		string text5 = BuildDevSummaryPreview(!string.IsNullOrWhiteSpace(material.Label) ? material.Label : material.SnapshotText, 24);
		return text6 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text5) ? "无预览" : text5);
	}

	private static IEnumerable<EventMaterialReference> OrderWeeklyPreviewMaterials(List<EventMaterialReference> materials)
	{
		return (materials ?? new List<EventMaterialReference>()).OrderBy((EventMaterialReference x) => GetWeeklyPreviewMaterialSortBucket(x)).ThenBy((EventMaterialReference x) => x?.ActionDay ?? int.MinValue).ThenBy((EventMaterialReference x) => x?.ActionSequence ?? int.MaxValue).ThenBy((EventMaterialReference x) => x?.ActionOrder ?? int.MinValue).ThenBy((EventMaterialReference x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase);
	}

	private static void NormalizeNpcActionSequences(Dictionary<string, List<NpcActionEntry>> storage)
	{
		if (storage == null)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<string, List<NpcActionEntry>> item in storage.OrderBy((KeyValuePair<string, List<NpcActionEntry>> x) => x.Key ?? "", StringComparer.OrdinalIgnoreCase))
		{
			if (item.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item2 in item.Value.OrderBy((NpcActionEntry x) => x?.Day ?? int.MinValue).ThenBy((NpcActionEntry x) => x?.Order ?? int.MinValue).ThenBy((NpcActionEntry x) => x?.GameDate ?? "", StringComparer.Ordinal))
			{
				if (item2 != null && item2.Sequence <= 0)
				{
					item2.Sequence = ++num;
				}
			}
		}
	}

	private static int GetMaxNpcActionSequence(params Dictionary<string, List<NpcActionEntry>>[] storages)
	{
		int num = 0;
		foreach (Dictionary<string, List<NpcActionEntry>> dictionary in storages ?? Array.Empty<Dictionary<string, List<NpcActionEntry>>>())
		{
			if (dictionary == null)
			{
				continue;
			}
			foreach (List<NpcActionEntry> value in dictionary.Values)
			{
				if (value == null)
				{
					continue;
				}
				foreach (NpcActionEntry item in value)
				{
					if (item != null && item.Sequence > num)
					{
						num = item.Sequence;
					}
				}
			}
		}
		return num;
	}

	private static int GetMaxNpcActionSequence(Dictionary<string, List<NpcActionEntry>> majorStorage, Dictionary<string, List<NpcActionEntry>> recentStorage, List<EventSourceMaterialEntry> sourceMaterials)
	{
		int num = GetMaxNpcActionSequence(majorStorage, recentStorage);
		foreach (EventSourceMaterialEntry item in sourceMaterials ?? new List<EventSourceMaterialEntry>())
		{
			if (item != null && item.Sequence > num)
			{
				num = item.Sequence;
			}
		}
		return num;
	}

	private static int GetWeeklyPreviewMaterialSortBucket(EventMaterialReference material)
	{
		string text = (material?.MaterialType ?? "").Trim().ToLowerInvariant();
		if (text == "world_opening_summary" || text == "kingdom_opening_summary")
		{
			return 0;
		}
		return 1;
	}

	private string BuildWeeklyPreviewGroupDetailText(WeeklyEventMaterialPreviewGroup group, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "分组标题", group.Title ?? "");
		AppendDevNpcActionField(stringBuilder, "分组类型", TranslateEventKindForDev(group.GroupKind));
		AppendDevNpcActionField(stringBuilder, "关联王国", ResolveKingdomDisplay(group.KingdomId));
		AppendDevNpcActionField(stringBuilder, "素材数量", ((group.Materials != null) ? group.Materials.Count : 0).ToString());
		AppendDevNpcActionField(stringBuilder, "页码", (page + 1) + "/" + Math.Max(1, totalPages));
		if (!string.IsNullOrWhiteSpace(group.Summary))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【说明】");
			stringBuilder.AppendLine(group.Summary.Trim());
		}
		if (group.Materials == null || group.Materials.Count == 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("当前这个分组还没有可用素材。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevWeeklyPreviewMaterialDetail(WeeklyEventMaterialPreviewGroup group, EventMaterialReference material, int returnPage)
	{
		string text = BuildDevEventMaterialDetailText(material);
		InformationManager.ShowInquiry(new InquiryData("本周素材详情", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回素材列表", "", delegate
		{
			OpenDevWeeklyEventMaterialPreviewGroupDetail(group, returnPage);
		}, null));
	}

	private void OpenDevWeeklyReportPromptPreviewMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("single", "查看单组 Prompt", null));
		list.Add(new InquiryElement("batch", "查看 Batch Prompt/Response", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周周报 Prompt 预览", "选择要查看的调试视图。单组视图用于逐王国核对，Batch 视图用于查看实际批量请求和最近一次返回。", list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "single":
					OpenDevWeeklyReportSinglePromptPreviewMenu();
					break;
				case "batch":
					OpenDevWeeklyBatchPromptPreviewMenu();
					break;
				default:
					OpenDevEventEditorMenu();
					break;
				}
			}
			else
			{
				OpenDevEventEditorMenu();
			}
		}, delegate
		{
			OpenDevWeeklyReportPromptPreviewMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevWeeklyReportSinglePromptPreviewMenu()
	{
		List<WeeklyEventMaterialPreviewGroup> list = BuildWeeklyEventMaterialPreviewGroups();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			list2.Add(new InquiryElement(item, BuildWeeklyEventMaterialPreviewGroupLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周周报 Prompt 预览", BuildWeeklyReportPromptPreviewMenuDescription(list), list2, isExitShown: true, 0, 1, "查看 Prompt", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup)
			{
				OpenDevWeeklyReportPromptDetail(weeklyEventMaterialPreviewGroup);
			}
			else
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
		}, delegate
		{
			OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildWeeklyReportPromptPreviewMenuDescription(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示当前这一周会发给大模型的周报请求 Prompt。");
		stringBuilder.AppendLine("当前只在开发态使用，生成结果会写回事件编辑，不会自动发给 NPC。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("分组数量：" + ((groups != null) ? groups.Count : 0));
		stringBuilder.AppendLine("篇幅档位：" + GetWeeklyReportPromptProfile().Label);
		stringBuilder.AppendLine("每分钟生成上限：" + GetWeeklyReportRequestsPerMinute());
		stringBuilder.AppendLine("MaxTokens：" + WeeklyReportRequestMaxTokens);
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevWeeklyBatchPromptPreviewMenu()
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		List<WeeklyReportBatchRequest> list = BuildWeeklyReportBatchRequests(OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups()), num2, num, currentGameDayIndexSafe);
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (WeeklyReportBatchRequest item in list)
		{
			DevWeeklyReportBatchPreviewEntry latestWeeklyReportBatchDevPreview = FindLatestWeeklyReportBatchDevPreview(item);
			string text = (latestWeeklyReportBatchDevPreview == null) ? "未执行" : (latestWeeklyReportBatchDevPreview.Success ? "有响应" : "失败响应");
			int count = (item?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Count;
			list2.Add(new InquiryElement(item, BuildWeeklyReportBatchDisplayLabel(item) + " [" + count + "块 | " + text + "]", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周周报 Batch Prompt/Response", BuildWeeklyBatchPromptPreviewMenuDescription(list), list2, isExitShown: true, 0, 1, "查看详情", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is WeeklyReportBatchRequest weeklyReportBatchRequest)
			{
				OpenDevWeeklyBatchPromptDetail(weeklyReportBatchRequest);
			}
			else
			{
				OpenDevWeeklyBatchPromptPreviewMenu();
			}
		}, delegate
		{
			OpenDevWeeklyReportPromptPreviewMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private string BuildWeeklyBatchPromptPreviewMenuDescription(List<WeeklyReportBatchRequest> batches)
	{
		int num = (batches ?? new List<WeeklyReportBatchRequest>()).Sum((WeeklyReportBatchRequest x) => (x?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Count);
		int num2 = (batches ?? new List<WeeklyReportBatchRequest>()).Count((WeeklyReportBatchRequest x) => FindLatestWeeklyReportBatchDevPreview(x) != null);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示当前这一周实际会发送的批量周报请求。");
		stringBuilder.AppendLine("每个批次会显示完整 batch prompt，以及最近一次执行缓存下来的 response 原文。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("批次数量：" + ((batches != null) ? batches.Count : 0));
		stringBuilder.AppendLine("覆盖周报目标：" + num);
		stringBuilder.AppendLine("已有最近响应缓存：" + num2);
		stringBuilder.AppendLine("批次上限：" + GetWeeklyReportBatchSize());
		stringBuilder.AppendLine("篇幅档位：" + GetWeeklyReportPromptProfile().Label);
		stringBuilder.AppendLine("每分钟生成上限：" + GetWeeklyReportRequestsPerMinute());
		stringBuilder.AppendLine("MaxTokens：" + WeeklyReportRequestMaxTokens);
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevWeeklyReportPromptDetail(WeeklyEventMaterialPreviewGroup group)
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		string text = BuildWeeklyReportSystemPrompt(group);
		string text2 = BuildWeeklyReportUserPrompt(group, num2, num, currentGameDayIndexSafe);
		InformationManager.ShowInquiry(new InquiryData("周报 Prompt 详情", BuildWeeklyReportPromptPreviewText(group, text, text2), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回 Prompt 列表", "", delegate
		{
			OpenDevWeeklyReportPromptPreviewMenu();
		}, null));
	}

	private void OpenDevWeeklyBatchPromptDetail(WeeklyReportBatchRequest batch)
	{
		string text = BuildWeeklyBatchReportSystemPrompt();
		string text2 = BuildWeeklyBatchReportUserPrompt(batch);
		string text3 = BuildWeeklyBatchPromptPreviewText(batch, text, text2);
		DevWeeklyReportBatchPreviewEntry latestWeeklyReportBatchDevPreview = FindLatestWeeklyReportBatchDevPreview(batch);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(text3);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【最近一次 Response】");
		if (latestWeeklyReportBatchDevPreview == null)
		{
			stringBuilder.AppendLine("当前没有缓存的 batch response。需要先实际生成一次本周周报。");
		}
		else
		{
			stringBuilder.AppendLine("执行结果：" + (latestWeeklyReportBatchDevPreview.Success ? "成功" : "失败"));
			stringBuilder.AppendLine("尝试次数：" + latestWeeklyReportBatchDevPreview.AttemptsUsed);
			if (!string.IsNullOrWhiteSpace(latestWeeklyReportBatchDevPreview.FailureReason))
			{
				stringBuilder.AppendLine("失败原因：" + latestWeeklyReportBatchDevPreview.FailureReason);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(latestWeeklyReportBatchDevPreview.ResponsePreview) ? "响应原文为空。" : latestWeeklyReportBatchDevPreview.ResponsePreview.Trim());
		}
		InformationManager.ShowInquiry(new InquiryData("Batch Prompt/Response 详情", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回 Batch 列表", "", delegate
		{
			OpenDevWeeklyBatchPromptPreviewMenu();
		}, null));
	}

#if false
	private void ConfirmGenerateDevWeeklyReports()
	{
		List<WeeklyEventMaterialPreviewGroup> list = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups());
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		int num3 = BuildWeeklyReportBatchRequests(list, num2, num, currentGameDayIndexSafe).Count;
		List<string> list2 = GetKingdomIdsByPlayerProximity(list.Where((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeeklyEventMaterialPreviewGroup x) => x.KingdomId));
		string text = ((list2.Count > 0) ? string.Join(" -> ", list2.Select(ResolveKingdomDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x))) : "无");
		InformationManager.ShowInquiry(new InquiryData("生成本周周报草案", "即将按当前周素材生成开发态周报草案。\n\n- 生成对象：世界周报 + 各王国周报\n- 生成结果：写入事件编辑中的事件记录\n- NPC 会常驻读取近期三个王国短周报；命中特定规则时读取完整周报\n- 生成优先级：最近王国 > 世界事件 > 其他王国按距离依次生成\n\n本次预计请求数：" + num + "\n篇幅档位：" + GetWeeklyReportPromptProfile().Label + "\n每分钟生成上限：" + GetWeeklyReportRequestsPerMinute() + "\n按距离排序的王国：" + text + "\nMaxTokens：" + WeeklyReportRequestMaxTokens + "\n\n是否开始？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "开始生成", "取消", delegate
		{
			_ = GenerateDevWeeklyReportsAsync();
		}, delegate
		{
			OpenDevEventEditorMenu();
		}));
	}

#endif

	private void ConfirmGenerateDevWeeklyReports()
	{
		List<WeeklyEventMaterialPreviewGroup> list = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups());
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int startDay = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int weekIndex = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		int batchCount = BuildWeeklyReportBatchRequests(list, weekIndex, startDay, currentGameDayIndexSafe).Count;
		List<string> list2 = GetKingdomIdsByPlayerProximity(list.Where((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeeklyEventMaterialPreviewGroup x) => x.KingdomId));
		string text = ((list2.Count > 0) ? string.Join(" -> ", list2.Select(ResolveKingdomDisplay).Where((string x) => !string.IsNullOrWhiteSpace(x))) : "无");
		string message = "即将按当前周素材生成开发态周报草案。\n\n- 生成对象：世界周报 + 各王国周报\n- 生成结果：写入事件编辑中的事件记录\n- NPC 会常驻读取近期三个王国短周报；命中特定规则时读取完整周报\n- 生成优先级：最近王国 > 世界事件 > 其他王国按距离依次生成\n\n本次预计请求数：" + batchCount + "\n篇幅档位：" + GetWeeklyReportPromptProfile().Label + "\n每分钟生成上限：" + GetWeeklyReportRequestsPerMinute() + "\n按距离排序的王国：" + text + "\nMaxTokens：" + WeeklyReportRequestMaxTokens + "\n\n是否开始？";
		InformationManager.ShowInquiry(new InquiryData("生成本周周报草案", message, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "开始生成", "取消", delegate
		{
			_ = GenerateDevWeeklyReportsAsync();
		}, delegate
		{
			OpenDevEventEditorMenu();
		}));
	}

	private async Task GenerateDevWeeklyReportsAsync()
	{
		List<WeeklyEventMaterialPreviewGroup> list = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups());
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		await GenerateWeeklyReportsBatchedAsyncInternal(list, num2, num, currentGameDayIndexSafe, "本周周报草案", openViewerWhenDone: true, queueBlockingPopupOnFatalFailure: true, isAutoGeneration: false);
	}

	private async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsMinuteBurstAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration)
	{
		WeeklyReportGenerationResult generationResult = new WeeklyReportGenerationResult();
		list = (list ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null && IsWeeklyReportGroupEligible(x)).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可生成周报的分组。"));
			if (openViewerWhenDone)
			{
				OpenDevEventEditorMenu();
			}
			generationResult.Completed = true;
			return generationResult;
		}
		List<string> failureMessages = new List<string>();
		Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap = BuildWeeklyReportGroupMap(list);
		List<WeeklyReportBatchRequest> batches = BuildWeeklyReportBatchRequests(list, weekIndex, startDay, endDay);
		int burstSize = Math.Max(1, GetWeeklyReportRequestsPerMinute());
		int totalWaves = Math.Max(1, (int)Math.Ceiling((double)batches.Count / (double)burstSize));
		List<Task<WeeklyReportBatchExecutionResult>> runningTasks = new List<Task<WeeklyReportBatchExecutionResult>>();
		InformationManager.DisplayMessage(new InformationMessage("开始生成" + displayLabel + "，共 " + list.Count + " 条周报目标，" + batches.Count + " 个批次；将按分钟整批发送，每分钟同时发送 " + burstSize + " 个请求。"));
		for (int i = 0; i < batches.Count; i += burstSize)
		{
			List<WeeklyReportBatchRequest> wave = batches.Skip(i).Take(burstSize).Where((WeeklyReportBatchRequest x) => x != null && x.Groups != null && x.Groups.Count > 0).ToList();
			if (wave.Count == 0)
			{
				continue;
			}
			int waveIndex = i / burstSize + 1;
			string waveLabel = string.Join(" | ", wave.Select(BuildWeeklyReportBatchDisplayLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)));
			Logger.Log("EventWeeklyReport", "[BATCH-WAVE] " + displayLabel + " wave " + waveIndex + "/" + totalWaves + " launch count=" + wave.Count + " :: " + waveLabel);
			InformationManager.DisplayMessage(new InformationMessage("周报批次 " + waveIndex + "/" + totalWaves + " 已发出，共 " + wave.Count + " 个请求。"));
			for (int j = 0; j < wave.Count; j++)
			{
				runningTasks.Add(ExecuteWeeklyReportBatchAsync(wave[j], i + j, 3));
			}
			if (i + burstSize < batches.Count)
			{
				await Task.Delay(60000);
			}
		}
		WeeklyReportBatchExecutionResult[] completed = await Task.WhenAll(runningTasks);
		int successCount = 0;
		int failureCount = 0;
		List<WeeklyEventMaterialPreviewGroup> failedGroups = new List<WeeklyEventMaterialPreviewGroup>();
		foreach (WeeklyReportBatchExecutionResult execution in completed.OrderBy((WeeklyReportBatchExecutionResult x) => x.BatchIndex))
		{
			WeeklyReportBatchRequest batch = execution?.Batch;
			WeeklyReportBatchRequestResult batchResult = execution?.Result ?? new WeeklyReportBatchRequestResult
			{
				Success = false,
				FailureReason = "Batch execution returned no result.",
				Blocks = new List<WeeklyReportBatchBlockResult>(),
				MissingReportIds = new List<string>()
			};
			CaptureWeeklyReportBatchDevPreview(batch, batchResult);
			HashSet<string> parsedReportIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (WeeklyReportBatchBlockResult block in batchResult.Blocks ?? new List<WeeklyReportBatchBlockResult>())
			{
				if (block == null || !block.Parsed || string.IsNullOrWhiteSpace(block.ReportId) || !parsedReportIds.Add(block.ReportId))
				{
					continue;
				}
				if (!groupMap.TryGetValue(block.ReportId, out var group) || group == null)
				{
					continue;
				}
				UpsertWeeklyReportEventRecord(group, weekIndex, block.Title, block.ShortSummary, block.Report, block.TagText, batchResult.PromptPreview);
				successCount++;
			}
			List<WeeklyEventMaterialPreviewGroup> missingGroups = new List<WeeklyEventMaterialPreviewGroup>();
			foreach (string reportId in batchResult.MissingReportIds ?? new List<string>())
			{
				if (!parsedReportIds.Contains(reportId) && groupMap.TryGetValue(reportId, out var missingGroup) && missingGroup != null)
				{
					missingGroups.Add(missingGroup);
				}
			}
			if (!batchResult.Success && missingGroups.Count == 0)
			{
				foreach (WeeklyEventMaterialPreviewGroup group2 in batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>())
				{
					string reportId2 = BuildWeeklyReportGroupReportId(group2);
					if (!string.IsNullOrWhiteSpace(reportId2) && !parsedReportIds.Contains(reportId2))
					{
						missingGroups.Add(group2);
					}
				}
			}
			if (missingGroups.Count > 0)
			{
				failureCount += missingGroups.Count;
				failedGroups.AddRange(missingGroups.Where((WeeklyEventMaterialPreviewGroup x) => x != null));
				failureMessages.Add(BuildWeeklyReportBatchDisplayLabel(batch) + "：批量请求失败，未回退到单个王国请求 - " + (batchResult.FailureReason ?? "未知错误"));
			}
		}
		failedGroups = failedGroups.Where((WeeklyEventMaterialPreviewGroup x) => x != null).Distinct().ToList();
		if (failureMessages.Count > 0)
		{
			Logger.Log("EventWeeklyReport", string.Join("\n", failureMessages));
		}
		if (failedGroups.Count > 0)
		{
			WeeklyEventMaterialPreviewGroup firstFailedGroup = failedGroups.FirstOrDefault();
			WeeklyReportRequestResult failedRequest = new WeeklyReportRequestResult
			{
				Success = false,
				FailureReason = failureMessages.FirstOrDefault() ?? "批量请求失败。",
				AttemptsUsed = 3
			};
			generationResult.SuccessCount = successCount;
			generationResult.FailureCount = failureCount;
			generationResult.BlockedByFatalFailure = true;
			generationResult.RetryContext = CreateWeeklyReportRetryContext(failedGroups, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, isAutoGeneration, firstFailedGroup, failedRequest);
			InformationManager.DisplayMessage(new InformationMessage(displayLabel + " 生成中止：有 " + failureCount + " 条周报目标批量失败，未回退到单个王国请求。"));
			if (queueBlockingPopupOnFatalFailure)
			{
				QueueWeeklyReportFailurePopup(generationResult.RetryContext, showImmediate: true);
			}
			return generationResult;
		}
		InformationManager.DisplayMessage(new InformationMessage(displayLabel + " 生成完成：成功 " + successCount + " 条，失败 " + failureCount + " 条。"));
		if (openViewerWhenDone)
		{
			OpenDevEventViewerMenu(0);
		}
		generationResult.SuccessCount = successCount;
		generationResult.FailureCount = failureCount;
		generationResult.Completed = true;
		return generationResult;
	}

	private async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsBatchedAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration)
	{
		return await GenerateWeeklyReportsMinuteBurstAsyncInternal(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, queueBlockingPopupOnFatalFailure, isAutoGeneration);
#if false
		WeeklyReportGenerationResult weeklyReportGenerationResult = new WeeklyReportGenerationResult();
		list = (list ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null && IsWeeklyReportGroupEligible(x)).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可生成周报的分组。"));
			if (openViewerWhenDone)
			{
				OpenDevEventEditorMenu();
			}
			weeklyReportGenerationResult.Completed = true;
			return weeklyReportGenerationResult;
		}
		int num = 0;
		int num2 = 0;
		int weeklyReportRequestIntervalMs = GetWeeklyReportRequestIntervalMs();
		List<string> list2 = new List<string>();
		Dictionary<string, WeeklyEventMaterialPreviewGroup> weeklyReportGroupMap = BuildWeeklyReportGroupMap(list);
		List<WeeklyReportBatchRequest> list3 = BuildWeeklyReportBatchRequests(list, weekIndex, startDay, endDay);
		InformationManager.DisplayMessage(new InformationMessage("开始生成" + displayLabel + "，共 " + list.Count + " 条周报目标，" + list3.Count + " 个批次；每分钟上限 " + GetWeeklyReportRequestsPerMinute() + "。"));
		for (int i = 0; i < list3.Count; i++)
		{
			WeeklyReportBatchRequest weeklyReportBatchRequest = list3[i];
			if (weeklyReportBatchRequest == null || weeklyReportBatchRequest.Groups == null || weeklyReportBatchRequest.Groups.Count == 0)
			{
				continue;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			WeeklyReportBatchRequestResult weeklyReportBatchRequestResult = await GenerateWeeklyReportBatchWithRetriesAsync(weeklyReportBatchRequest, 3);
			CaptureWeeklyReportBatchDevPreview(weeklyReportBatchRequest, weeklyReportBatchRequestResult);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (WeeklyReportBatchBlockResult item in weeklyReportBatchRequestResult.Blocks ?? new List<WeeklyReportBatchBlockResult>())
			{
				if (item == null || !item.Parsed || string.IsNullOrWhiteSpace(item.ReportId) || !hashSet.Add(item.ReportId))
				{
					continue;
				}
				if (!weeklyReportGroupMap.TryGetValue(item.ReportId, out var value) || value == null)
				{
					continue;
				}
				UpsertWeeklyReportEventRecord(value, weekIndex, item.Title, item.ShortSummary, item.Report, item.TagText, weeklyReportBatchRequestResult.PromptPreview);
				num++;
			}
			List<WeeklyEventMaterialPreviewGroup> list4 = new List<WeeklyEventMaterialPreviewGroup>();
			foreach (string item2 in weeklyReportBatchRequestResult.MissingReportIds ?? new List<string>())
			{
				if (!hashSet.Contains(item2) && weeklyReportGroupMap.TryGetValue(item2, out var value2) && value2 != null)
				{
					list4.Add(value2);
				}
			}
			if (!weeklyReportBatchRequestResult.Success && list4.Count == 0)
			{
				foreach (WeeklyEventMaterialPreviewGroup item3 in weeklyReportBatchRequest.Groups.Where((WeeklyEventMaterialPreviewGroup x) => x != null))
				{
					string text = BuildWeeklyReportGroupReportId(item3);
					if (!string.IsNullOrWhiteSpace(text) && !hashSet.Contains(text))
					{
						list4.Add(item3);
					}
				}
			}
#if false
			if (!weeklyReportBatchRequestResult.Success && list4.Count > 0)
			{
				Logger.Log("EventWeeklyReport", BuildWeeklyReportBatchDisplayLabel(weeklyReportBatchRequest) + " 批量生成失败，回退到单组生成。原因：" + (weeklyReportBatchRequestResult.FailureReason ?? "未知错误"));
				foreach (WeeklyEventMaterialPreviewGroup item4 in list4)
				{
					if (item4 == null || !IsWeeklyReportGroupEligible(item4))
					{
						continue;
					}
					WeeklyReportRequestResult weeklyReportRequestResult = await GenerateWeeklyReportGroupWithRetriesAsync(item4, weekIndex, startDay, endDay, 3);
					if (!weeklyReportRequestResult.Success)
					{
						num2++;
						string text2 = BuildWeeklyReportGroupDisplayLabel(item4);
						list2.Add(text2 + "：批量请求失败后回退到单组仍连续 3 次请求失败 - " + (weeklyReportRequestResult.FailureReason ?? "未知错误"));
						List<WeeklyEventMaterialPreviewGroup> list5 = BuildRemainingWeeklyReportGroupsForRetry(list3, i, list4.SkipWhile((WeeklyEventMaterialPreviewGroup x) => !object.ReferenceEquals(x, item4)));
						if (list5.Count == 0)
						{
							list5 = BuildRemainingWeeklyReportGroupsForRetry(list3, i, new List<WeeklyEventMaterialPreviewGroup> { item4 });
						}
						weeklyReportGenerationResult.SuccessCount = num;
						weeklyReportGenerationResult.FailureCount = num2;
						weeklyReportGenerationResult.BlockedByFatalFailure = true;
						weeklyReportGenerationResult.RetryContext = CreateWeeklyReportRetryContext(list5, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, isAutoGeneration, item4, weeklyReportRequestResult);
						Logger.Log("EventWeeklyReport", string.Join("\n", list2));
						InformationManager.DisplayMessage(new InformationMessage(text2 + " 连续 3 次请求失败，已暂停等待手动处理。"));
						if (queueBlockingPopupOnFatalFailure)
						{
							QueueWeeklyReportFailurePopup(weeklyReportGenerationResult.RetryContext, showImmediate: true);
						}
						return weeklyReportGenerationResult;
					}
					UpsertWeeklyReportEventRecord(item4, weekIndex, weeklyReportRequestResult.Title, weeklyReportRequestResult.ShortSummary, weeklyReportRequestResult.Report, weeklyReportRequestResult.TagText, weeklyReportRequestResult.PromptPreview);
					hashSet.Add(BuildWeeklyReportGroupReportId(item4));
					num++;
				}
			}
#endif
			if (!weeklyReportBatchRequestResult.Success && list4.Count > 0)
			{
				num2 += list4.Count;
				string text2 = BuildWeeklyReportBatchDisplayLabel(weeklyReportBatchRequest);
				list2.Add(text2 + "：批量请求失败，未再回退到单个王国请求 - " + (weeklyReportBatchRequestResult.FailureReason ?? "未知错误"));
				List<WeeklyEventMaterialPreviewGroup> list5 = BuildRemainingWeeklyReportGroupsForRetry(list3, i, list4);
				WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = list4.FirstOrDefault((WeeklyEventMaterialPreviewGroup x) => x != null);
				WeeklyReportRequestResult weeklyReportRequestResult = new WeeklyReportRequestResult
				{
					Success = false,
					PromptPreview = weeklyReportBatchRequestResult.PromptPreview ?? "",
					FailureReason = weeklyReportBatchRequestResult.FailureReason ?? "未知错误",
					AttemptsUsed = weeklyReportBatchRequestResult.AttemptsUsed,
					IsRateLimit = weeklyReportBatchRequestResult.IsRateLimit,
					IsRequestsPerMinuteLimit = weeklyReportBatchRequestResult.IsRequestsPerMinuteLimit,
					IsQuotaLimit = weeklyReportBatchRequestResult.IsQuotaLimit,
					RetryAfterSeconds = weeklyReportBatchRequestResult.RetryAfterSeconds
				};
				weeklyReportGenerationResult.SuccessCount = num;
				weeklyReportGenerationResult.FailureCount = num2;
				weeklyReportGenerationResult.BlockedByFatalFailure = true;
				weeklyReportGenerationResult.RetryContext = CreateWeeklyReportRetryContext(list5, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, isAutoGeneration, weeklyEventMaterialPreviewGroup, weeklyReportRequestResult);
				Logger.Log("EventWeeklyReport", string.Join("\n", list2));
				InformationManager.DisplayMessage(new InformationMessage(text2 + " 批量请求失败，已停止，未再回退到单个王国请求。"));
				if (queueBlockingPopupOnFatalFailure)
				{
					QueueWeeklyReportFailurePopup(weeklyReportGenerationResult.RetryContext, showImmediate: true);
				}
				return weeklyReportGenerationResult;
			}
			stopwatch.Stop();
			if (i + 1 < list3.Count)
			{
				int num3 = weeklyReportRequestIntervalMs - (int)Math.Ceiling(stopwatch.Elapsed.TotalMilliseconds);
				if (num3 > 0)
				{
					await Task.Delay(num3);
				}
			}
		}
		InformationManager.DisplayMessage(new InformationMessage(displayLabel + "生成完成：成功 " + num + " 条，失败 " + num2 + " 条。"));
		if (list2.Count > 0)
		{
			Logger.Log("EventWeeklyReport", string.Join("\n", list2));
		}
		if (openViewerWhenDone)
		{
			OpenDevEventViewerMenu(0);
		}
		weeklyReportGenerationResult.SuccessCount = num;
		weeklyReportGenerationResult.FailureCount = num2;
		weeklyReportGenerationResult.Completed = true;
		return weeklyReportGenerationResult;
#endif
	}

	private async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration)
	{
#if false
		WeeklyReportGenerationResult weeklyReportGenerationResult = new WeeklyReportGenerationResult();
		list = (list ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null && IsWeeklyReportGroupEligible(x)).ToList();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可生成周报的分组。"));
			if (openViewerWhenDone)
			{
				OpenDevEventEditorMenu();
			}
			weeklyReportGenerationResult.Completed = true;
			return weeklyReportGenerationResult;
		}
		int num3 = 0;
		int num4 = 0;
		int weeklyReportRequestIntervalMs = GetWeeklyReportRequestIntervalMs();
		List<string> list2 = new List<string>();
		InformationManager.DisplayMessage(new InformationMessage("开始生成" + displayLabel + "，共 " + list.Count + " 条请求；每分钟上限 " + GetWeeklyReportRequestsPerMinute() + "。"));
		for (int i = 0; i < list.Count; i++)
		{
			WeeklyEventMaterialPreviewGroup item = list[i];
			if (item == null)
			{
				continue;
			}
			if (!IsWeeklyReportGroupEligible(item))
			{
				Logger.Log("EventWeeklyReport", "[SKIP] 周报分组已失效，跳过生成: " + BuildWeeklyReportGroupDisplayLabel(item) + " kingdom=" + ((item.KingdomId ?? "").Trim()));
				continue;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			WeeklyReportRequestResult weeklyReportRequestResult = await GenerateWeeklyReportGroupWithRetriesAsync(item, weekIndex, startDay, endDay, 3);
			if (!weeklyReportRequestResult.Success)
			{
				num4++;
				string text = BuildWeeklyReportGroupDisplayLabel(item);
				list2.Add(text + "：连续 3 次请求失败 - " + (weeklyReportRequestResult.FailureReason ?? "未知错误"));
				weeklyReportGenerationResult.SuccessCount = num3;
				weeklyReportGenerationResult.FailureCount = num4;
				weeklyReportGenerationResult.BlockedByFatalFailure = true;
				weeklyReportGenerationResult.RetryContext = CreateWeeklyReportRetryContext(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, isAutoGeneration, item, weeklyReportRequestResult);
				Logger.Log("EventWeeklyReport", string.Join("\n", list2));
				InformationManager.DisplayMessage(new InformationMessage(text + " 连续 3 次请求失败，已暂停游戏并等待手动处理。"));
				if (queueBlockingPopupOnFatalFailure)
				{
					QueueWeeklyReportFailurePopup(weeklyReportGenerationResult.RetryContext, showImmediate: true);
				}
				return weeklyReportGenerationResult;
			}
			UpsertWeeklyReportEventRecord(item, weekIndex, weeklyReportRequestResult.Title, weeklyReportRequestResult.ShortSummary, weeklyReportRequestResult.Report, weeklyReportRequestResult.TagText, weeklyReportRequestResult.PromptPreview);
			num3++;
			stopwatch.Stop();
			if (i + 1 < list.Count)
			{
				int num5 = weeklyReportRequestIntervalMs - (int)Math.Ceiling(stopwatch.Elapsed.TotalMilliseconds);
				if (num5 > 0)
				{
					await Task.Delay(num5);
				}
			}
		}
		InformationManager.DisplayMessage(new InformationMessage(displayLabel + "生成完成：成功 " + num3 + " 条，失败 " + num4 + " 条。"));
		if (list2.Count > 0)
		{
			Logger.Log("EventWeeklyReport", string.Join("\n", list2));
		}
		if (openViewerWhenDone)
		{
			OpenDevEventViewerMenu(0);
		}
		weeklyReportGenerationResult.SuccessCount = num3;
		weeklyReportGenerationResult.FailureCount = num4;
		weeklyReportGenerationResult.Completed = true;
		return weeklyReportGenerationResult;
#endif
		return await GenerateWeeklyReportsBatchedAsyncInternal(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, queueBlockingPopupOnFatalFailure, isAutoGeneration);
	}

	private void OpenDevAllDataMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_all", "导出（全部，选择文件夹）", null));
		list.Add(new InquiryElement("import_all", "导入（全部，选择文件夹）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("全部导出/导入", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevAllDataMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevAllDataMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
			string text = selected[0].Identifier as string;
			if (text == "export_all")
			{
				OpenExportFolderPicker("导出（全部）- 选择文件夹", ExportImportScope.All, OpenDevAllDataMenu);
			}
			else if (text == "import_all")
			{
				OpenImportFolderPicker("导入（全部）- 选择文件夹", ExportImportScope.All, OpenDevAllDataMenu);
			}
		}
	}

	private void OpenDevTownEditorHeroSelection()
	{
		_devEditingHero = null;
		_devHistorySearchQuery = string.Empty;
		_devHeroSelectionQuery = string.Empty;
		_devEditableHeroes = BuildDevEditableHeroList();
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的 NPC。"));
			OpenDevHeroNpcMenu();
			return;
		}
		OpenDevTownEditorHeroSelectionPaged(0, null);
	}

	private void OpenDevTownEditorHeroSelectionPaged(int page, string query)
	{
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			_devEditableHeroes = BuildDevEditableHeroList();
		}
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的 NPC。"));
			OpenDevHeroNpcMenu();
			return;
		}
		if (page < 0)
		{
			page = 0;
		}
		string text = (query ?? "").Trim();
		_devHeroSelectionQuery = text;
		List<Hero> filteredHeroes = _devEditableHeroes;
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			filteredHeroes = _devEditableHeroes.Where(delegate(Hero h)
			{
				string text5 = ((h?.Name != null) ? h.Name.ToString() : "").Trim().ToLowerInvariant();
				string text6 = (h?.StringId ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("search", "搜索 NPC", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(new InquiryElement("clear_search", "清空搜索", null));
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)filteredHeroes.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		_devHeroSelectionPage = page;
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", "下一页", null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (Hero item in filteredHeroes.Skip(num2).Take(pageSize))
		{
			string text2 = item?.Name?.ToString() ?? "NPC";
			string text3 = item?.StringId ?? "";
			string title = string.IsNullOrEmpty(text3) ? text2 : (text2 + " (ID=" + text3 + ")");
			list.Add(new InquiryElement(item, title, null));
		}
		string descriptionText = $"全部 NPC：{_devEditableHeroes.Count} 个";
		descriptionText = descriptionText + $"\n当前结果：{filteredHeroes.Count} 个，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			descriptionText = descriptionText + "\n搜索关键词：" + text;
		}
		if (filteredHeroes.Count == 0)
		{
			descriptionText += "\n没有匹配结果，可以重新搜索。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑 HeroNPC - 选择NPC", descriptionText, list, isExitShown: true, 0, 1, "确定", "返回", OnDevHeroSelected, delegate
		{
			OpenDevHeroNpcMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevEditLine(Hero npc, int dayIndex, int lineIndex)
	{
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || lineIndex < 0 || lineIndex >= dialogueDay.Lines.Count)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的对话行。"));
			OpenDevHistoryLineSelection(npc, dayIndex);
			return;
		}
		string currentValue = dialogueDay.Lines[lineIndex] ?? "";
		string displayDate = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text = npc.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑对话行 - " + text, "当前日期: " + displayDate, "下方输入框可直接修改整条内容，留空则删除该行。", currentValue, delegate(string input)
		{
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input);
		}, delegate
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
		});
	}

	private void ApplyDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string input)
	{
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> source = LoadDialogueHistory(npc);
		DialogueDay dialogueDay = source.FirstOrDefault((DialogueDay x) => x.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || lineIndex < 0 || lineIndex >= dialogueDay.Lines.Count)
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
			return;
		}
		if (string.IsNullOrWhiteSpace(input))
		{
			dialogueDay.Lines.RemoveAt(lineIndex);
		}
		else
		{
			dialogueDay.Lines[lineIndex] = input;
		}
		source = source.Where((DialogueDay x) => x != null && x.Lines != null && x.Lines.Count > 0).ToList();
		SaveDialogueHistory(npc, source);
		InformationManager.DisplayMessage(new InformationMessage("对话行已更新."));
		OpenDevHistoryLineSelection(npc, dayIndex);
	}

	private void OpenDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string currentValue, string displayDate)
	{
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string titleText = "编辑对话行 - " + text;
		string text2 = "当前日期: " + displayDate + "\n原内容已载入下方输入框，可直接编辑。\n留空则删除该行。";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input);
		}, delegate
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
		}, shouldInputBeObfuscated: false, null, "", currentValue ?? ""));
	}

	private List<Hero> BuildDevEditableHeroList()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<Hero> list = new List<Hero>();
		Action<string> action = delegate(string id)
		{
			string text6 = (id ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text6) || !hashSet.Add(text6))
			{
				return;
			}
			try
			{
				Hero hero5 = Hero.FindFirst((Hero h) => h != null && string.Equals((h.StringId ?? "").Trim(), text6, StringComparison.OrdinalIgnoreCase));
				if (hero5 != null && hero5 != Hero.MainHero)
				{
					list.Add(hero5);
				}
			}
			catch
			{
			}
		};
		try
		{
			foreach (Hero allAliveHero in Hero.AllAliveHeroes)
			{
				if (allAliveHero == null || allAliveHero == Hero.MainHero)
				{
					continue;
				}
				action(allAliveHero.StringId);
			}
		}
		catch
		{
		}
		if (_dialogueHistory != null)
		{
			foreach (KeyValuePair<string, List<DialogueDay>> item2 in _dialogueHistory)
			{
				if (!string.IsNullOrEmpty(item2.Key))
				{
					action(item2.Key);
				}
			}
		}
		if (_npcPersonaProfiles != null)
		{
			foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
			{
				if (!string.IsNullOrEmpty(npcPersonaProfile.Key))
				{
					action(npcPersonaProfile.Key);
				}
			}
		}
		if (RewardSystemBehavior.Instance != null)
		{
			List<string> allDebtorHeroIds = RewardSystemBehavior.Instance.GetAllDebtorHeroIds();
			if (allDebtorHeroIds != null)
			{
				foreach (string item3 in allDebtorHeroIds)
				{
					if (!string.IsNullOrEmpty(item3))
					{
						action(item3);
					}
				}
			}
		}
		if (list.Count > 1)
		{
			list = list.OrderBy((Hero h) => h.Name?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => h.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		}
		return list;
	}

	private void OnDevHeroSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text && text == "back")
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text2)
		{
			switch (text2)
			{
			case "search":
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索 NPC", "输入 NPC 名称或 HeroId，可查询全部 NPC。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
				{
					OpenDevTownEditorHeroSelectionPaged(0, input);
				}, delegate
				{
					OpenDevTownEditorHeroSelectionPaged(0, _devHeroSelectionQuery);
				}, shouldInputBeObfuscated: false, null, _devHeroSelectionQuery ?? ""));
				return;
			case "clear_search":
				OpenDevTownEditorHeroSelectionPaged(0, null);
				return;
			case "prev_page":
				OpenDevTownEditorHeroSelectionPaged(_devHeroSelectionPage - 1, _devHeroSelectionQuery);
				return;
			case "next_page":
				OpenDevTownEditorHeroSelectionPaged(_devHeroSelectionPage + 1, _devHeroSelectionQuery);
				return;
			case "__sep__":
				OpenDevTownEditorHeroSelectionPaged(_devHeroSelectionPage, _devHeroSelectionQuery);
				return;
			}
		}
		if (!(selected[0].Identifier is Hero devEditingHero))
		{
			OpenDevHeroNpcMenu();
			return;
		}
		_devEditingHero = devEditingHero;
		_devHistorySearchQuery = string.Empty;
		ShowDevEditInquiry(_devEditingHero);
	}

	private void ShowDevEditInquiry(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine(" - 编辑对话历史");
			stringBuilder.AppendLine(" - 编辑赊账/欠款");
			stringBuilder.AppendLine(" - 编辑角色个性/历史背景");
			stringBuilder.AppendLine(" - 查看行动记录（结构化）");
			stringBuilder.AppendLine(" - 切换 NPC");
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("edit_history", "编辑对话历史", null));
			list.Add(new InquiryElement("edit_debt", "编辑赊账/欠款", null));
			list.Add(new InquiryElement("edit_persona", "编辑角色个性/历史背景", null));
			list.Add(new InquiryElement("view_actions", "查看行动记录（结构化）", null));
			list.Add(new InquiryElement("switch_npc", "切换 NPC", null));
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑 HeroNPC - " + text, stringBuilder.ToString(), list, isExitShown: true, 0, 1, "进入", "返回", OnDevNpcMainMenuSelected, delegate
			{
				OpenDevHeroNpcMenu();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
	}

	private void OpenDevSetDebtGoldSimple(Hero npc)
	{
		if (npc == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		RewardSystemBehavior.Instance.GetDebtSnapshot(npc, out var owedGold, out var items);
		string text = npc.Name?.ToString() ?? "NPC";
		string text2 = "当前金币欠款: " + owedGold + "。\n输入新的金币欠款数值（允许为 0）：";
		InformationManager.ShowTextInquiry(new TextInquiryData("设置金币欠款 - " + text, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
		{
			int result;
			if (string.IsNullOrWhiteSpace(input))
			{
				OpenDevDebtMenu(npc);
			}
			else if (!int.TryParse(input, out result) || result < 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的非负整数."));
				OpenDevSetDebtGoldSimple(npc);
			}
			else
			{
				RewardSystemBehavior.Instance.SetDebt(npc, result, items);
				InformationManager.DisplayMessage(new InformationMessage("已更新金币欠款为 " + result + "。"));
				OpenDevDebtMenu(npc);
			}
		}, delegate
		{
			OpenDevDebtMenu(npc);
		}));
	}

	private void OpenDevDebtMenu(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		string text = npc.Name?.ToString() ?? "NPC";
		string descriptionText = "请选择要执行的操作：";
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				string text2 = RewardSystemBehavior.Instance.BuildDebtEditorSummary(npc, 10);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					descriptionText = text2;
				}
			}
		}
		catch
		{
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("set_gold", "设置金币欠款", null));
		list.Add(new InquiryElement("clear_debt", "清空所有欠款", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑赊账/欠款 - " + text, descriptionText, list, isExitShown: true, 0, 1, "执行", "返回", OnDevDebtMenuSelected, delegate
		{
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevDebtMenuSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			ShowDevEditInquiry(devEditingHero);
			return;
		}
		string text = selected[0].Identifier as string;
		if (text == "set_gold")
		{
			OpenDevSetDebtGoldSimple(devEditingHero);
		}
		else if (text == "clear_debt")
		{
			if (RewardSystemBehavior.Instance != null)
			{
				RewardSystemBehavior.Instance.SetDebt(devEditingHero, 0, new Dictionary<string, int>());
				InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的所有欠款."));
			}
			OpenDevDebtMenu(devEditingHero);
		}
		else
		{
			ShowDevEditInquiry(devEditingHero);
		}
	}

	private void OnDevNpcMainMenuSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero != null && selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "edit_history":
				OpenDevHistoryDateSelection(devEditingHero);
				break;
			case "edit_debt":
				OpenDevDebtMenu(devEditingHero);
				break;
			case "edit_persona":
				OpenDevPersonaMenu(devEditingHero);
				break;
			case "view_actions":
				OpenDevNpcActionMenu(devEditingHero, recentOnly: true, 0);
				break;
			case "switch_npc":
				OpenDevTownEditorHeroSelection();
				break;
			}
		}
	}

	private void OpenDevNpcActionMenu(Hero npc, bool recentOnly, int page)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<NpcActionEntry> devNpcActionEntries = GetDevNpcActionEntries(npc, recentOnly);
		List<NpcActionEntry> devNpcActionEntries2 = GetDevNpcActionEntries(npc, recentOnly: false);
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 18;
		int num = Math.Max(1, (int)Math.Ceiling((double)devNpcActionEntries.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("recent", "查看近期行动", null));
		list.Add(new InquiryElement("major", "查看重大行动", null));
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", "下一页", null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (NpcActionEntry item in devNpcActionEntries.Skip(num2).Take(pageSize))
		{
			list.Add(new InquiryElement(item, BuildDevNpcActionItemLabel(item), null));
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string descriptionText = BuildDevNpcActionMenuDescription(npc, recentOnly, page, num, devNpcActionEntries.Count, devNpcActionEntries2.Count);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("行动记录 - " + text, descriptionText, list, isExitShown: true, 0, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				ShowDevEditInquiry(npc);
			}
			else if (selected[0].Identifier is string text2)
			{
				switch (text2)
				{
				case "back":
					ShowDevEditInquiry(npc);
					break;
				case "recent":
					OpenDevNpcActionMenu(npc, recentOnly: true, 0);
					break;
				case "major":
					OpenDevNpcActionMenu(npc, recentOnly: false, 0);
					break;
				case "prev_page":
					OpenDevNpcActionMenu(npc, recentOnly, page - 1);
					break;
				case "next_page":
					OpenDevNpcActionMenu(npc, recentOnly, page + 1);
					break;
				default:
					OpenDevNpcActionMenu(npc, recentOnly, page);
					break;
				}
			}
			else if (selected[0].Identifier is NpcActionEntry npcActionEntry)
			{
				OpenDevNpcActionDetail(npc, recentOnly, page, npcActionEntry);
			}
			else
			{
				OpenDevNpcActionMenu(npc, recentOnly, page);
			}
		}, delegate
		{
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private List<NpcActionEntry> GetDevNpcActionEntries(Hero npc, bool recentOnly)
	{
		string npcActionHeroKey = GetNpcActionHeroKey(npc);
		if (string.IsNullOrWhiteSpace(npcActionHeroKey))
		{
			return new List<NpcActionEntry>();
		}
		Dictionary<string, List<NpcActionEntry>> dictionary = (recentOnly ? _npcRecentActions : _npcMajorActions);
		if (dictionary == null || !dictionary.TryGetValue(npcActionHeroKey, out var value) || value == null)
		{
			return new List<NpcActionEntry>();
		}
		return SanitizeNpcActionEntries(value, keepOnlyRecentWindow: recentOnly);
	}

	private string BuildDevNpcActionMenuDescription(Hero npc, bool recentOnly, int page, int totalPages, int currentCount, int majorCount)
	{
		string text = npc?.Name?.ToString() ?? "NPC";
		List<NpcActionEntry> devNpcActionEntries = GetDevNpcActionEntries(npc, recentOnly: true);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("当前 NPC：" + text);
		stringBuilder.AppendLine("当前视图：" + (recentOnly ? "近期行动" : "重大行动"));
		stringBuilder.AppendLine("近期行动数：" + devNpcActionEntries.Count);
		stringBuilder.AppendLine("重大行动数：" + majorCount);
		stringBuilder.AppendLine($"第 {page + 1}/{Math.Max(1, totalPages)} 页，本页类型总数：{currentCount}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("说明：");
		stringBuilder.AppendLine(" - 行动记录已按单个 NPC 存储");
		stringBuilder.AppendLine(" - 详情里会显示时间、地点、人物、家族、王国、定居点归属等结构化字段");
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildDevNpcActionItemLabel(NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "无效行动";
		}
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日");
		string text2 = BuildDevNpcActionPreviewText(entry);
		if (text2.Length > 108)
		{
			text2 = text2.Substring(0, 108) + "...";
		}
		return text + " " + text2;
	}

	private void OpenDevNpcActionDetail(Hero npc, bool recentOnly, int page, NpcActionEntry entry)
	{
		if (npc == null || entry == null)
		{
			ShowDevEditInquiry(npc);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回行动列表", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("行动详情 - " + (npc.Name?.ToString() ?? "NPC"), BuildDevNpcActionDetailText(entry), list, isExitShown: true, 0, 1, "返回", "关闭", delegate
		{
			OpenDevNpcActionMenu(npc, recentOnly, page);
		}, delegate
		{
			OpenDevNpcActionMenu(npc, recentOnly, page);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private static string BuildDevNpcActionDetailText(NpcActionEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = BuildDevNpcActionNarrative(entry);
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.AppendLine("【易读说明】");
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("【原始字段】");
		AppendDevNpcActionField(stringBuilder, "日期", !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日"));
		AppendDevNpcActionField(stringBuilder, "日序", entry.Day.ToString());
		AppendDevNpcActionField(stringBuilder, "显示文本", (entry.Text ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "StableKey", (entry.StableKey ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "行动类型", GetDevNpcActionKindDisplay(entry.ActionKind));
		AppendDevNpcActionField(stringBuilder, "是否重大行动", entry.IsMajor ? "是" : "否");
		AppendDevNpcActionField(stringBuilder, "结果", !entry.Won.HasValue ? "" : (entry.Won.Value ? "获胜" : "失利"));
		AppendDevNpcActionField(stringBuilder, "地点文本", (entry.LocationText ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "定居点", ResolveDisplayNameBySettlementEntry(entry));
		AppendDevNpcActionField(stringBuilder, "定居点ID", (entry.SettlementId ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "定居点所属领主", ResolveHeroDisplay(entry.SettlementOwnerHeroId));
		AppendDevNpcActionField(stringBuilder, "定居点所属家族", ResolveClanDisplay(entry.SettlementOwnerClanId));
		AppendDevNpcActionField(stringBuilder, "定居点所属王国", ResolveKingdomDisplay(entry.SettlementOwnerKingdomId));
		AppendDevNpcActionField(stringBuilder, "定居点前任领主", ResolveHeroDisplay(entry.PreviousSettlementOwnerHeroId));
		AppendDevNpcActionField(stringBuilder, "定居点前任家族", ResolveClanDisplay(entry.PreviousSettlementOwnerClanId));
		AppendDevNpcActionField(stringBuilder, "定居点前任王国", ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId));
		AppendDevNpcActionField(stringBuilder, "行动人物", ResolveHeroDisplay(entry.ActorHeroId));
		AppendDevNpcActionField(stringBuilder, "行动家族", ResolveClanDisplay(entry.ActorClanId));
		AppendDevNpcActionField(stringBuilder, "行动王国", ResolveKingdomDisplay(entry.ActorKingdomId));
		AppendDevNpcActionField(stringBuilder, "目标人物", ResolveHeroDisplay(entry.TargetHeroId));
		AppendDevNpcActionField(stringBuilder, "目标家族", ResolveClanDisplay(entry.TargetClanId));
		AppendDevNpcActionField(stringBuilder, "目标王国", ResolveKingdomDisplay(entry.TargetKingdomId));
		AppendDevNpcActionField(stringBuilder, "相关人物ID", JoinDevActionIds(entry.RelatedHeroIds));
		AppendDevNpcActionField(stringBuilder, "相关家族ID", JoinDevActionIds(entry.RelatedClanIds));
		AppendDevNpcActionField(stringBuilder, "相关王国ID", JoinDevActionIds(entry.RelatedKingdomIds));
		if (!HasStructuredNpcActionMetadata(entry))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("提示：这条行动缺少较完整的结构化元数据，可能是旧版本记录，或当时游戏未提供足够目标信息。");
			stringBuilder.AppendLine("建议让游戏继续跑 1-2 天，再观察新生成的行动记录。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildDevNpcActionPreviewText(NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "无效行动";
		}
		List<string> list = new List<string>();
		string text = BuildNpcActionActorNarrativeText(entry);
		string text2 = TranslateNpcActionKindForPrompt(entry.ActionKind);
		string text3 = ResolveDisplayNameBySettlementEntry(entry);
		string text4 = ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text5 = ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text6 = ResolveKingdomDisplay(entry.TargetKingdomId);
		string text7 = ResolveClanDisplay(entry.TargetClanId);
		string text8 = ResolveHeroDisplay(entry.TargetHeroId);
		string text9 = ResolveClanDisplay(entry.ActorClanId);
		string text10 = ResolveKingdomDisplay(entry.ActorKingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
		}
		else if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("这是一条" + text2);
		}
		if (!string.IsNullOrWhiteSpace(text3) && string.IsNullOrWhiteSpace(entry.SettlementId) && !text.Contains(text3))
		{
			list.Add("地点在" + text3);
		}
		if (!string.IsNullOrWhiteSpace(text9) && !string.IsNullOrWhiteSpace(text10))
		{
			list.Add("其所属家族是" + text9 + "，隶属于" + text10);
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("其所属家族是" + text9);
		}
		else if (!string.IsNullOrWhiteSpace(text10))
		{
			list.Add("其隶属于" + text10);
		}
		if (!string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(text4))
		{
			list.Add("当时该地由" + text5 + "掌控，隶属于" + text4);
		}
		else if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add("当时该地隶属于" + text4);
		}
		else if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("当时该地由" + text5 + "掌控");
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("涉及人物是" + text8);
		}
		else if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("涉及家族是" + text7);
		}
		else if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("涉及王国是" + text6);
		}
		if (entry.Won.HasValue)
		{
			list.Add("结果是" + (entry.Won.Value ? "获胜" : "失利"));
		}
		if (entry.IsMajor)
		{
			list.Add("属于重大行动");
		}
		if (list.Count == 0)
		{
			return "这条记录暂时没有可读摘要。";
		}
		if (!HasStructuredNpcActionMetadata(entry))
		{
			list.Add("这条记录的结构化信息较少");
		}
		return string.Join("；", list) + "。";
	}

	private static string BuildDevNpcActionNarrative(NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日");
		string text2 = TranslateNpcActionKindForPrompt(entry.ActionKind);
		string text3 = ResolveHeroDisplay(entry.ActorHeroId);
		string text15 = string.IsNullOrWhiteSpace(text3) ? "该人物" : text3;
		string text4 = ResolveDisplayNameBySettlementEntry(entry);
		string text5 = ResolveHeroDisplay(entry.TargetHeroId);
		string text6 = ResolveClanDisplay(entry.TargetClanId);
		string text7 = ResolveKingdomDisplay(entry.TargetKingdomId);
		string text8 = ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text9 = ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text10 = ResolveClanDisplay(entry.PreviousSettlementOwnerClanId);
		string text11 = ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId);
		string text12 = ResolveClanDisplay(entry.ActorClanId);
		string text13 = ResolveKingdomDisplay(entry.ActorKingdomId);
		list.Add(text + "。");
		string text14 = BuildNpcActionActorNarrativeText(entry);
		if (!string.IsNullOrWhiteSpace(text14))
		{
			list.Add(text14 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("这属于" + text2 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text12) && !string.IsNullOrWhiteSpace(text13))
		{
			list.Add(text15 + "所属家族是" + text12 + "，隶属于" + text13 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text12))
		{
			list.Add(text15 + "所属家族是" + text12 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text13))
		{
			list.Add(text15 + "隶属于" + text13 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add("地点是" + text4 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text8) && !string.IsNullOrWhiteSpace(text9))
		{
			list.Add("当时该定居点由" + text8 + "掌控，隶属于" + text9 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("当时该定居点由" + text8 + "掌控。");
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("当时该定居点隶属于" + text9 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text10) && !string.IsNullOrWhiteSpace(text11))
		{
			list.Add("在此之前，这里由" + text10 + "掌控，归属" + text11 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("直接涉及的人物是" + text5 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("涉及的家族是" + text6 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("涉及的王国是" + text7 + "。");
		}
		if (entry.Won.HasValue)
		{
			list.Add("结果是" + (entry.Won.Value ? "获胜" : "失利") + "。");
		}
		if (entry.IsMajor)
		{
			list.Add("这被归类为重大行动。");
		}
		return string.Join("", list);
	}

	private static string BuildNpcActionActorNarrativeText(NpcActionEntry entry)
	{
		string text = (entry?.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = ResolveHeroDisplay(entry.ActorHeroId);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		if (text.StartsWith("你的", StringComparison.Ordinal))
		{
			return text2 + "的" + text.Substring(2);
		}
		if (text.StartsWith("你", StringComparison.Ordinal))
		{
			return text2 + text.Substring(1);
		}
		return text.Replace(" 你", " " + text2).Replace("；你", "；" + text2).Replace("，你", "，" + text2);
	}

	private static bool HasStructuredNpcActionMetadata(NpcActionEntry entry)
	{
		return entry != null && (!string.IsNullOrWhiteSpace(entry.ActionKind) || !string.IsNullOrWhiteSpace(entry.LocationText) || !string.IsNullOrWhiteSpace(entry.SettlementId) || !string.IsNullOrWhiteSpace(entry.TargetHeroId) || !string.IsNullOrWhiteSpace(entry.TargetClanId) || !string.IsNullOrWhiteSpace(entry.TargetKingdomId) || !string.IsNullOrWhiteSpace(entry.SettlementOwnerClanId) || !string.IsNullOrWhiteSpace(entry.SettlementOwnerKingdomId) || !string.IsNullOrWhiteSpace(entry.ActorHeroId) || !string.IsNullOrWhiteSpace(entry.ActorClanId) || !string.IsNullOrWhiteSpace(entry.ActorKingdomId));
	}

	private static string GetDevNpcActionKindDisplay(string actionKind)
	{
		string text = (actionKind ?? "").Trim();
		string text2 = TranslateNpcActionKindForPrompt(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return string.IsNullOrWhiteSpace(text) ? text2 : (text2 + " (" + text + ")");
	}

	private static string ResolveHeroDisplay(string heroId)
	{
		string text = ResolveHeroName(heroId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (heroId ?? "").Trim();
	}

	private static string ResolveClanDisplay(string clanId)
	{
		string text = ResolveClanName(clanId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (clanId ?? "").Trim();
	}

	private static string ResolveKingdomDisplay(string kingdomId)
	{
		string text = ResolveKingdomName(kingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (kingdomId ?? "").Trim();
	}

	private static void AppendDevNpcActionField(StringBuilder stringBuilder, string label, string value)
	{
		if (stringBuilder == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		stringBuilder.AppendLine(label + "：" + value.Trim());
	}

	private static string JoinDevActionIds(List<string> ids)
	{
		if (ids == null || ids.Count == 0)
		{
			return "";
		}
		List<string> list = ids.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return (list.Count == 0) ? "" : string.Join(", ", list);
	}

	private void OpenDevPersonaMenu(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			string text = npc.Name?.ToString() ?? "NPC";
			GetNpcPersonaStrings(npc, out var personality, out var background);
			bool flag = !string.IsNullOrWhiteSpace(personality);
			bool flag2 = !string.IsNullOrWhiteSpace(background);
			string npcVoiceId = GetNpcVoiceId(npc);
			bool flag3 = !string.IsNullOrWhiteSpace(npcVoiceId);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine("当前个性：" + (flag ? "已设置" : "未设置"));
			stringBuilder.AppendLine("当前历史背景：" + (flag2 ? "已设置" : "未设置"));
			stringBuilder.AppendLine("当前音色ID：" + (flag3 ? npcVoiceId : "未设置（使用VoiceMapping自动分配）"));
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("set_personality", "设置/修改个性", null));
			list.Add(new InquiryElement("set_background", "设置/修改历史背景", null));
			list.Add(new InquiryElement("set_voice", "设置/修改音色ID", null));
			list.Add(new InquiryElement("clear_persona", "清空个性、历史背景与音色ID", null));
			list.Add(new InquiryElement("back", "返回", null));
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑个性/历史背景 - " + text, stringBuilder.ToString(), list, isExitShown: true, 0, 1, "执行", "返回", OnDevPersonaMenuSelected, delegate
			{
				ShowDevEditInquiry(npc);
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
	}

	private void OnDevPersonaMenuSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			ShowDevEditInquiry(devEditingHero);
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "set_personality":
			OpenDevSetPersonality(devEditingHero);
			break;
		case "set_background":
			OpenDevSetBackground(devEditingHero);
			break;
		case "set_voice":
			OpenDevSetVoiceId(devEditingHero);
			break;
		case "clear_persona":
			SaveNpcPersonaProfile(devEditingHero, new NpcPersonaProfile());
			InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的个性、历史背景与音色ID."));
			OpenDevPersonaMenu(devEditingHero);
			break;
		default:
			ShowDevEditInquiry(devEditingHero);
			break;
		}
	}

	private static string BuildDevHistoryPreview(string line, int maxLen = 56)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return "（空）";
		}
		StringBuilder stringBuilder = new StringBuilder(line.Length);
		bool flag = false;
		foreach (char c in line)
		{
			if (char.IsWhiteSpace(c))
			{
				if (!flag)
				{
					stringBuilder.Append(' ');
					flag = true;
				}
			}
			else
			{
				stringBuilder.Append(c);
				flag = false;
			}
		}
		string text = stringBuilder.ToString().Trim();
		if (text.Length <= maxLen)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxLen)) + "...";
	}

	private void OpenDevHistorySearchInput(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string soundEventPath = _devHistorySearchQuery ?? string.Empty;
		InformationManager.ShowTextInquiry(new TextInquiryData("搜索对话历史 - " + text, "输入关键词，按内容模糊匹配该 NPC 的全部对话历史。\n留空将不改变当前搜索。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
		{
			if (!string.IsNullOrWhiteSpace(input))
			{
				_devHistorySearchQuery = input.Trim();
			}
			OpenDevHistoryDateSelection(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}, shouldInputBeObfuscated: false, null, soundEventPath));
	}

	private void ConfirmDevClearAllDialogueHistory(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> list = LoadDialogueHistory(npc);
		int num = 0;
		if (list != null)
		{
			foreach (DialogueDay item in list)
			{
				num += (item?.Lines?.Count).GetValueOrDefault();
			}
		}
		string arg = npc.Name?.ToString() ?? "NPC";
		string text = $"将删除 {arg} 的全部对话历史（共 {num} 条）。\n此操作不可撤销，是否继续？";
		InformationManager.ShowInquiry(new InquiryData("确认删除对话历史", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认删除", "取消", delegate
		{
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			string text2 = npc.StringId ?? string.Empty;
			if (!string.IsNullOrEmpty(text2))
			{
				_dialogueHistory.Remove(text2);
			}
			_devHistorySearchQuery = string.Empty;
			InformationManager.DisplayMessage(new InformationMessage("已删除该NPC的全部对话历史。"));
			ShowDevEditInquiry(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}));
	}

	private void OpenDevHistoryDateSelection(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			ShowDevEditInquiry(npc);
			return;
		}
		list = list.OrderBy((DialogueDay d) => d.GameDayIndex).ToList();
		string text = (_devHistorySearchQuery ?? string.Empty).Trim();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__search__", "搜索对话历史", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement("__clear_search__", "清空搜索", null));
		}
		list2.Add(new InquiryElement("__clear_all__", "一键删除该NPC全部对话历史", null));
		list2.Add(new InquiryElement("back", "返回", null));
		if (string.IsNullOrWhiteSpace(text))
		{
			foreach (DialogueDay item in list)
			{
				if (item != null && item.Lines != null && item.Lines.Count != 0)
				{
					string text2 = ((!string.IsNullOrEmpty(item.GameDate)) ? item.GameDate : $"第 {item.GameDayIndex} 日");
					string title = text2 + $" (共 {item.Lines.Count} 条)";
					list2.Add(new InquiryElement(item.GameDayIndex, title, null));
				}
			}
		}
		else
		{
			string value = text.ToLowerInvariant();
			foreach (DialogueDay item2 in list)
			{
				if (item2 == null || item2.Lines == null || item2.Lines.Count == 0)
				{
					continue;
				}
				string arg = ((!string.IsNullOrEmpty(item2.GameDate)) ? item2.GameDate : $"第 {item2.GameDayIndex} 日");
				for (int num = 0; num < item2.Lines.Count; num++)
				{
					string text3 = item2.Lines[num] ?? string.Empty;
					if (text3.ToLowerInvariant().Contains(value))
					{
						string arg2 = BuildDevHistoryPreview(text3);
						string title2 = $"{arg} / 第{num + 1}条: {arg2}";
						list2.Add(new InquiryElement(new Tuple<int, int>(item2.GameDayIndex, num), title2, null));
					}
				}
			}
		}
		string text4 = npc.Name?.ToString() ?? "NPC";
		string text5 = "请选择要查看/编辑的日期。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 = text5 + "\n当前搜索：" + BuildDevHistoryPreview(text, 40);
		}
		if (list2.Count <= 4)
		{
			text5 += "\n\n没有匹配结果。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话历史 - " + text4, text5, list2, isExitShown: true, 0, 1, "下一步", "返回", OnDevHistoryDateSelected, delegate
		{
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHistoryDateSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			OpenDevHistoryDateSelection(devEditingHero);
			return;
		}
		if (selected[0].Identifier is string text)
		{
			switch (text)
			{
			case "back":
				ShowDevEditInquiry(devEditingHero);
				return;
			case "__search__":
				OpenDevHistorySearchInput(devEditingHero);
				return;
			case "__clear_search__":
				_devHistorySearchQuery = string.Empty;
				OpenDevHistoryDateSelection(devEditingHero);
				return;
			case "__clear_all__":
				ConfirmDevClearAllDialogueHistory(devEditingHero);
				return;
			}
		}
		if (selected[0].Identifier is Tuple<int, int> tuple)
		{
			OpenDevEditLine(devEditingHero, tuple.Item1, tuple.Item2);
			return;
		}
		if (!(selected[0].Identifier is int))
		{
			OpenDevHistoryDateSelection(devEditingHero);
			return;
		}
		int dayIndex = (int)selected[0].Identifier;
		OpenDevHistoryLineSelection(devEditingHero, dayIndex);
	}

	private void OpenDevHistoryLineSelection(Hero npc, int dayIndex)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || dialogueDay.Lines.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该日期下没有对话行。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回日期列表", null));
		list2.Add(new InquiryElement("search", "搜索该NPC历史", null));
		for (int num = 0; num < dialogueDay.Lines.Count; num++)
		{
			string line = dialogueDay.Lines[num] ?? "";
			string arg = BuildDevHistoryPreview(line);
			string title = $"{num + 1}. {arg}";
			list2.Add(new InquiryElement(new Tuple<int, int>(dayIndex, num), title, null));
		}
		string text = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text2 = npc.Name?.ToString() ?? "NPC";
		string text3 = "请选择要编辑的对话行。";
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话行 - " + text2, text3 + "\n当前日期: " + text, list2, isExitShown: true, 0, 1, "编辑", "返回", OnDevHistoryLineSelected, delegate
		{
			OpenDevHistoryDateSelection(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHistoryLineSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
		if (devEditingHero != null)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else if (selected[0].Identifier is string text2 && text2 == "search")
			{
				OpenDevHistorySearchInput(devEditingHero);
			}
			else if (!(selected[0].Identifier is Tuple<int, int> tuple))
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else
			{
				OpenDevEditLine(devEditingHero, tuple.Item1, tuple.Item2);
			}
		}
	}

	private void OpenDevSetPersonality(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = npc.Name?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor("设置个性 - " + text, "当前个性已载入下方输入框。", "请输入新的个性描述（留空=清空）。", personality ?? "", delegate(string input)
			{
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.Personality = (input ?? "").Trim();
				npcPersonaProfile.Background = (background ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				InformationManager.DisplayMessage(new InformationMessage("个性已更新."));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				OpenDevPersonaMenu(npc);
			});
		}
	}

	private void OpenDevSetVoiceId(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			string npcVoiceId = GetNpcVoiceId(npc);
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("当前音色ID：");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(npcVoiceId) ? "（未设置 - 使用VoiceMapping自动分配）" : npcVoiceId);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("请输入新的音色ID（留空=清空，将使用VoiceMapping自动分配）：");
			stringBuilder.AppendLine("提示：音色ID取决于您使用的TTS平台。");
			stringBuilder.AppendLine("  火山引擎例如: BV001_streaming");
			stringBuilder.AppendLine("  GPT-SoVITS例如: 参考音频文件名");
			InformationManager.ShowTextInquiry(new TextInquiryData("设置音色ID - " + text, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
			{
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.VoiceId = (input ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				string information = (string.IsNullOrWhiteSpace(input) ? "音色ID已清空（将使用VoiceMapping自动分配）." : ("音色ID已更新为: " + input.Trim()));
				InformationManager.DisplayMessage(new InformationMessage(information));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				OpenDevPersonaMenu(npc);
			}, shouldInputBeObfuscated: false, null, npcVoiceId ?? ""));
		}
	}

	private void OpenDevSetBackground(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = npc.Name?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor("设置历史背景 - " + text, "当前历史背景已载入下方输入框。", "请输入新的历史背景描述（留空=清空）。", background ?? "", delegate(string input)
			{
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.Personality = (personality ?? "").Trim();
				npcPersonaProfile.Background = (input ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				InformationManager.DisplayMessage(new InformationMessage("历史背景已更新."));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				OpenDevPersonaMenu(npc);
			});
		}
	}

	private void OpenDevKnowledgeMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_rule", "导出（单个知识条目，从列表选择）", null));
		list.Add(new InquiryElement("single_import_rule", "导入（单个知识条目，从文件夹选择）", null));
		list.Add(new InquiryElement("export_knowledge", "全量导出（Knowledge，选文件夹）", null));
		list.Add(new InquiryElement("import_knowledge", "全量导入（Knowledge，选文件夹）", null));
		list.Add(new InquiryElement("edit_knowledge", "编辑 Knowledge 条目…", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("Knowledge 导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevKnowledgeMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevKnowledgeMenuSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "edit_knowledge":
			if (KnowledgeLibraryBehavior.Instance != null)
			{
				KnowledgeLibraryBehavior.Instance.OpenEditorMenu(delegate
				{
					OpenDevKnowledgeMenu();
				});
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("KnowledgeLibraryBehavior 未初始化，无法打开知识编辑。"));
				OpenDevKnowledgeMenu();
			}
			break;
		case "single_export_rule":
			OpenDevKnowledgeSingleExportSelection();
			break;
		case "single_import_rule":
			_devPendingKnowledgeSingleImportPicked = false;
			_devPendingKnowledgeSingleImportFolderName = null;
			OpenFolderPickerWithCallback("导入（单个知识条目）- 选择文件夹", isExport: false, delegate(string folderName)
			{
				_devPendingKnowledgeSingleImportPicked = true;
				_devPendingKnowledgeSingleImportFolderName = folderName;
			}, delegate
			{
				if (!_devPendingKnowledgeSingleImportPicked)
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					string devPendingKnowledgeSingleImportFolderName = _devPendingKnowledgeSingleImportFolderName;
					_devPendingKnowledgeSingleImportPicked = false;
					_devPendingKnowledgeSingleImportFolderName = null;
					OpenDevKnowledgeSingleImportSelection(devPendingKnowledgeSingleImportFolderName);
				}
			});
			break;
		case "export_knowledge":
			OpenExportFolderPicker("全量导出（Knowledge）- 选择文件夹", ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		case "import_knowledge":
			OpenImportFolderPicker("全量导入（Knowledge）- 选择文件夹", ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		}
	}

	private void OpenDevKnowledgeSingleExportSelection()
	{
		List<KnowledgeLibraryBehavior.RuleIndexItem> list = (KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>())?.GetRuleIndexItemsForDev(400);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (KnowledgeLibraryBehavior.RuleIndexItem item in list)
		{
			string text = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				string title = (item?.Label ?? text).Trim();
				list2.Add(new InquiryElement(text, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导出单个知识条目 - 选择条目", "请选择要导出的知识条目：", list2, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevKnowledgeMenu();
			}
			else
			{
				string rid = selected[0].Identifier as string;
				if (rid == "back")
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					rid = (rid ?? "").Trim();
					if (string.IsNullOrEmpty(rid))
					{
						OpenDevKnowledgeMenu();
					}
					else
					{
						OpenFolderPickerWithCallback("导出（单个知识条目）- 选择文件夹", isExport: true, delegate(string folderName)
						{
							ExportSingleKnowledgeRuleData(folderName, rid);
						}, OpenDevKnowledgeMenu);
					}
				}
			}
		}, delegate
		{
			OpenDevKnowledgeMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevKnowledgeSingleImportSelection(string folderName)
	{
		List<string> knowledgeRuleIdsFromImportFolderForDev = GetKnowledgeRuleIdsFromImportFolderForDev(folderName, 400);
		if (knowledgeRuleIdsFromImportFolderForDev == null || knowledgeRuleIdsFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (string item in knowledgeRuleIdsFromImportFolderForDev)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导入单个知识条目 - 选择条目", "请选择要导入的知识条目：", list, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevKnowledgeMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					text2 = (text2 ?? "").Trim();
					if (string.IsNullOrEmpty(text2))
					{
						OpenDevKnowledgeMenu();
					}
					else
					{
						ImportSingleKnowledgeRuleData(folderName, text2);
						OpenDevKnowledgeMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevKnowledgeMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevUnnamedPersonaMenu()
	{
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_unnamed", "导出（单个未命名NPC，从列表选择）", null));
		list.Add(new InquiryElement("single_import_unnamed", "导入（单个未命名NPC，从文件夹选择）", null));
		list.Add(new InquiryElement("export_unnamed", "全量导出（未命名NPC，选文件夹）", null));
		list.Add(new InquiryElement("import_unnamed", "全量导入（未命名NPC，选文件夹）", null));
		list.Add(new InquiryElement("edit_unnamed", "编辑未命名NPC条目…", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("未命名NPC 导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevUnnamedPersonaMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevUnnamedPersonaMenuSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "single_export_unnamed":
			OpenDevUnnamedPersonaSingleExportSelection();
			break;
		case "single_import_unnamed":
			_devPendingUnnamedSingleImportPicked = false;
			_devPendingUnnamedSingleImportFolderName = null;
			OpenFolderPickerWithCallback("导入（单个未命名NPC）- 选择文件夹", isExport: false, delegate(string folderName)
			{
				_devPendingUnnamedSingleImportPicked = true;
				_devPendingUnnamedSingleImportFolderName = folderName;
			}, delegate
			{
				if (!_devPendingUnnamedSingleImportPicked)
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					string devPendingUnnamedSingleImportFolderName = _devPendingUnnamedSingleImportFolderName;
					_devPendingUnnamedSingleImportPicked = false;
					_devPendingUnnamedSingleImportFolderName = null;
					OpenDevUnnamedPersonaSingleImportSelection(devPendingUnnamedSingleImportFolderName);
				}
			});
			break;
		case "export_unnamed":
			OpenExportFolderPicker("全量导出（未命名NPC）- 选择文件夹", ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "import_unnamed":
			OpenImportFolderPicker("全量导入（未命名NPC）- 选择文件夹", ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "edit_unnamed":
			OpenDevUnnamedPersonaIndexSelection();
			break;
		}
	}

	private void OpenDevUnnamedPersonaSingleExportSelection()
	{
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string text = (item.Key ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = (string.IsNullOrWhiteSpace(item.Label) ? text : item.Label);
					list.Add(new InquiryElement(text, text2 + " (Key=" + text + ")", null));
				}
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导出单个未命名NPC - 选择条目", "请选择要导出的条目：", list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevUnnamedPersonaMenu();
			}
			else
			{
				string k = selected[0].Identifier as string;
				if (k == "back")
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					k = (k ?? "").Trim().ToLower();
					if (string.IsNullOrEmpty(k))
					{
						OpenDevUnnamedPersonaMenu();
					}
					else
					{
						OpenFolderPickerWithCallback("导出（单个未命名NPC）- 选择文件夹", isExport: true, delegate(string folderName)
						{
							ExportSingleUnnamedPersonaData(folderName, k);
						}, OpenDevUnnamedPersonaMenu);
					}
				}
			}
		}, delegate
		{
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevUnnamedPersonaSingleImportSelection(string folderName)
	{
		List<string> unnamedPersonaKeysFromImportFolderForDev = GetUnnamedPersonaKeysFromImportFolderForDev(folderName, 400);
		if (unnamedPersonaKeysFromImportFolderForDev == null || unnamedPersonaKeysFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (string item in unnamedPersonaKeysFromImportFolderForDev)
		{
			string text = (item ?? "").Trim().ToLower();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导入单个未命名NPC - 选择条目", "请选择要导入的条目：", list, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevUnnamedPersonaMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					text2 = (text2 ?? "").Trim().ToLower();
					if (string.IsNullOrEmpty(text2))
					{
						OpenDevUnnamedPersonaMenu();
					}
					else
					{
						ImportSingleUnnamedPersonaData(folderName, text2);
						OpenDevUnnamedPersonaMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevUnnamedPersonaIndexSelection()
	{
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string title = (string.IsNullOrWhiteSpace(item.Label) ? item.Key : item.Label);
				list.Add(new InquiryElement(item.Key, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑未命名NPC - 选择条目", "选择一个条目进行编辑：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevUnnamedPersonaIndexSelected, delegate
		{
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevUnnamedPersonaIndexSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevUnnamedPersonaMenu();
			return;
		}
		string text = selected[0].Identifier as string;
		if (text == "back")
		{
			OpenDevUnnamedPersonaMenu();
		}
		else
		{
			OpenDevUnnamedPersonaEdit(text);
		}
	}

	private void OpenDevUnnamedPersonaEdit(string key)
	{
		string k = (key ?? "").Trim();
		if (string.IsNullOrEmpty(k))
		{
			OpenDevUnnamedPersonaIndexSelection();
			return;
		}
		string personality = "";
		string background = "";
		try
		{
			ShoutUtils.TryGetUnnamedPersonaByKey(k, out personality, out background);
		}
		catch
		{
		}
		string text = (personality ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			text = (background ?? "").Trim();
		}
		string text2 = text.Replace("\r", "").Replace("\n", " ");
		if (text2.Length > 240)
		{
			text2 = text2.Substring(0, 240);
		}
		DevTextEditorHelper.ShowLongTextEditor("编辑未命名NPC - 描述", "Key: " + k, "请输入新的“描述”（留空=清空）。", text, delegate(string input)
		{
			string personality2 = (input ?? "").Trim();
			ShoutUtils.SaveUnnamedPersonaByKey(k, personality2, "");
			InformationManager.DisplayMessage(new InformationMessage("已保存：" + k));
			OpenDevUnnamedPersonaIndexSelection();
		}, delegate
		{
			OpenDevUnnamedPersonaIndexSelection();
		});
	}

	private void OpenDevSingleNpcHeroSelection()
	{
		_devSingleNpcSelectionQuery = string.Empty;
		_devSingleNpcSelectionPage = 0;
		_devEditableHeroes = BuildDevEditableHeroList() ?? new List<Hero>();
		OpenDevSingleNpcHeroSelectionPaged(0, null);
	}

	private void OpenDevSingleNpcHeroSelectionPaged(int page, string query)
	{
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			_devEditableHeroes = BuildDevEditableHeroList() ?? new List<Hero>();
		}
		if (page < 0)
		{
			page = 0;
		}
		string text = (query ?? "").Trim();
		_devSingleNpcSelectionQuery = text;
		List<Hero> filteredHeroes = _devEditableHeroes ?? new List<Hero>();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			filteredHeroes = filteredHeroes.Where(delegate(Hero h)
			{
				string text5 = ((h?.Name != null) ? h.Name.ToString() : "").Trim().ToLowerInvariant();
				string text6 = (h?.StringId ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("pick_from_export", "从导出文件夹选择NPC…", null));
		list.Add(new InquiryElement("manual_id", "手动输入 HeroId…", null));
		list.Add(new InquiryElement("search", "搜索 NPC", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(new InquiryElement("clear_search", "清空搜索", null));
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)filteredHeroes.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		_devSingleNpcSelectionPage = page;
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", "下一页", null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (Hero item in filteredHeroes.Skip(num2).Take(pageSize))
		{
			if (item == null)
			{
				continue;
			}
			string text2 = (item.StringId ?? "").Trim();
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = item.Name?.ToString() ?? "NPC";
				list.Add(new InquiryElement(item, text3 + " (ID=" + text2 + ")", null));
			}
		}
		string descriptionText = $"可从当前存档全部 NPC 中选择，也可从旧导出文件夹读取 JSON。\n全部 NPC：{_devEditableHeroes.Count} 个；当前结果：{filteredHeroes.Count} 个，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			descriptionText = descriptionText + "\n搜索关键词：" + text;
		}
		if (filteredHeroes.Count == 0)
		{
			descriptionText += "\n没有匹配结果，可以重新搜索。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - 选择NPC", descriptionText, list, isExitShown: true, 0, 1, "进入", "返回", OnDevSingleNpcHeroSelected, delegate
		{
			OpenDevHeroNpcMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevSingleNpcHeroSelectionFromExportFolder(string folderName)
	{
		string text = (folderName ?? "").Trim();
		string text2 = null;
		try
		{
			if (!string.IsNullOrEmpty(text) && Path.IsPathRooted(text) && Directory.Exists(text))
			{
				text2 = Path.GetFullPath(text);
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text2))
		{
			text2 = ResolveImportFolderPath(folderName);
		}
		if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到导出目录。"));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		Dictionary<string, string> idToName = new Dictionary<string, string>(StringComparer.Ordinal);
		List<string> list = new List<string>
		{
			Path.Combine(text2, "personality_background"),
			Path.Combine(text2, "dialogue_history"),
			Path.Combine(text2, "debt")
		};
		foreach (string item in list)
		{
			try
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] files = Directory.GetFiles(item, "*.json", SearchOption.TopDirectoryOnly);
				foreach (string text3 in files)
				{
					string text4 = TryParseHeroIdFromNpcFileName(text3);
					if (string.IsNullOrWhiteSpace(text4))
					{
						continue;
					}
					string value = "";
					try
					{
						string text5 = Path.GetFileNameWithoutExtension(text3) ?? "";
						int num = text5.IndexOf("__", StringComparison.Ordinal);
						if (num >= 0 && num + 2 < text5.Length)
						{
							value = (text5.Substring(num + 2) ?? "").Trim();
						}
					}
					catch
					{
					}
					if (!idToName.ContainsKey(text4))
					{
						idToName[text4] = value;
					}
					else if (string.IsNullOrWhiteSpace(idToName[text4]) && !string.IsNullOrWhiteSpace(value))
					{
						idToName[text4] = value;
					}
				}
			}
			catch
			{
			}
		}
		if (idToName.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该导出文件夹中没有可识别的 NPC JSON。"));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (KeyValuePair<string, string> item2 in idToName.OrderBy((KeyValuePair<string, string> k) => k.Key))
		{
			string text6 = (item2.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text6))
			{
				string text7 = (item2.Value ?? "").Trim();
				string title = (string.IsNullOrEmpty(text7) ? (text6 ?? "") : (text7 + " (ID=" + text6 + ")"));
				list2.Add(new InquiryElement(text6, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("从导出文件夹选择 NPC", "目录：\n" + text2 + "\n\n请选择要导入/导出的 NPC：", list2, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevSingleNpcHeroSelection();
			}
			else
			{
				string text8 = selected[0].Identifier as string;
				if (text8 == "back")
				{
					OpenDevSingleNpcHeroSelection();
				}
				else
				{
					text8 = (text8 ?? "").Trim();
					if (string.IsNullOrEmpty(text8))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						_devOpsHeroId = text8;
						string value2 = "";
						try
						{
							idToName.TryGetValue(text8, out value2);
						}
						catch
						{
							value2 = "";
						}
						if (string.IsNullOrWhiteSpace(value2))
						{
							value2 = text8;
						}
						_devOpsHeroName = value2;
						OpenDevSingleNpcOpsMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevSingleNpcHeroSelection();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevSingleNpcHeroSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text)
		{
			switch (text)
			{
			case "back":
				OpenDevHeroNpcMenu();
				return;
			case "search":
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索 NPC", "输入 NPC 名称或 HeroId，可查询全部 NPC。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
				{
					OpenDevSingleNpcHeroSelectionPaged(0, input);
				}, delegate
				{
					OpenDevSingleNpcHeroSelectionPaged(_devSingleNpcSelectionPage, _devSingleNpcSelectionQuery);
				}, shouldInputBeObfuscated: false, null, _devSingleNpcSelectionQuery ?? ""));
				return;
			case "clear_search":
				OpenDevSingleNpcHeroSelectionPaged(0, null);
				return;
			case "prev_page":
				OpenDevSingleNpcHeroSelectionPaged(_devSingleNpcSelectionPage - 1, _devSingleNpcSelectionQuery);
				return;
			case "next_page":
				OpenDevSingleNpcHeroSelectionPaged(_devSingleNpcSelectionPage + 1, _devSingleNpcSelectionQuery);
				return;
			case "__sep__":
				OpenDevSingleNpcHeroSelectionPaged(_devSingleNpcSelectionPage, _devSingleNpcSelectionQuery);
				return;
			case "pick_from_export":
				OpenFolderPickerWithCallback("单个 HeroNPC - 选择导入文件夹", isExport: false, delegate(string folderName)
				{
					OpenDevSingleNpcHeroSelectionFromExportFolder(folderName);
				}, OpenDevSingleNpcHeroSelection);
				return;
			case "manual_id":
				InformationManager.ShowTextInquiry(new TextInquiryData("手动输入 HeroId", "请输入 HeroId（例如：lord_... / wanderer_...）。\n\n说明：这里不会创建新的游戏角色，只是把导入/导出数据写入到该 HeroId 对应的存档数据里；只有当游戏里存在/将来出现同 ID 的 Hero 时，这些数据才会被使用。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
				{
					string manualHeroId = (input ?? "").Trim();
					if (string.IsNullOrEmpty(manualHeroId))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						_devOpsHeroId = manualHeroId;
						string devOpsHeroName = manualHeroId;
						try
						{
							string text3 = Hero.FindFirst((Hero x) => x != null && x.StringId == manualHeroId)?.Name?.ToString() ?? "";
							if (!string.IsNullOrWhiteSpace(text3))
							{
								devOpsHeroName = text3;
							}
						}
						catch
						{
						}
						_devOpsHeroName = devOpsHeroName;
						OpenDevSingleNpcOpsMenu();
					}
				}, delegate
				{
					OpenDevSingleNpcHeroSelection();
				}));
				return;
			}
		}
		if (!(selected[0].Identifier is Hero hero))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (hero.StringId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
			OpenDevSingleNpcHeroSelection();
		}
		else
		{
			_devOpsHeroId = text2;
			_devOpsHeroName = hero.Name?.ToString() ?? "NPC";
			OpenDevSingleNpcOpsMenu();
		}
	}

	private void OpenDevSingleNpcOpsMenu()
	{
		string text = (_devOpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (string.IsNullOrEmpty(_devOpsHeroName) ? "NPC" : _devOpsHeroName);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_persona", "导出（个性/背景，选择文件夹）", null));
		list.Add(new InquiryElement("import_persona", "导入（个性/背景，选择文件夹）", null));
		list.Add(new InquiryElement("export_history", "导出（对话历史，选择文件夹）", null));
		list.Add(new InquiryElement("import_history", "导入（对话历史，选择文件夹）", null));
		list.Add(new InquiryElement("export_debt", "导出（欠款，选择文件夹）", null));
		list.Add(new InquiryElement("import_debt", "导入（欠款，选择文件夹）", null));
		list.Add(new InquiryElement("change_hero", "切换NPC", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - " + text2 + " (ID=" + text + ")", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", OnDevSingleNpcOpsSelected, delegate
		{
			OpenDevSingleNpcHeroSelection();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevSingleNpcOpsSelected(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text = selected[0].Identifier as string;
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (_devOpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text3 = "单个 HeroNPC - " + (string.IsNullOrEmpty(_devOpsHeroName) ? "NPC" : _devOpsHeroName);
		switch (text)
		{
		case "back":
			OpenDevSingleNpcHeroSelection();
			break;
		case "change_hero":
			OpenDevSingleNpcHeroSelection();
			break;
		case "export_persona":
			OpenFolderPicker(text3 + " - 导出（个性/背景）", isExport: true, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_persona":
			OpenFolderPicker(text3 + " - 导入（个性/背景）", isExport: false, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_history":
			OpenFolderPicker(text3 + " - 导出（对话历史）", isExport: true, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_history":
			OpenFolderPicker(text3 + " - 导入（对话历史）", isExport: false, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_debt":
			OpenFolderPicker(text3 + " - 导出（欠款）", isExport: true, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_debt":
			OpenFolderPicker(text3 + " - 导入（欠款）", isExport: false, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		}
	}

	private static string GetVoiceGroupShortName(string key)
	{
		return (key ?? "").ToLowerInvariant() switch
		{
			"male_young" => "青男", 
			"male_middle" => "中男", 
			"male_old" => "老男", 
			"female_young" => "青女", 
			"female_middle" => "中女", 
			"female_old" => "老女", 
			_ => key ?? "", 
		};
	}

	private void OpenDevVoiceMappingMenu()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组数量概览（详细ID请进分组查看）");
		stringBuilder.AppendLine("------------------------------");
		int num = 0;
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		foreach (string text in allGroupKeys)
		{
			int num2 = VoiceMapper.GetVoicesForGroup(text)?.Count ?? 0;
			num += num2;
			stringBuilder.AppendLine($"  {GetVoiceGroupShortName(text)}: {num2}");
		}
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		stringBuilder.AppendLine("------------------------------");
		stringBuilder.AppendLine("兜底: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "未设置" : fallbackVoice));
		stringBuilder.AppendLine($"总ID: {num}");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("add_voice", "添加声音ID到分组", null));
		list.Add(new InquiryElement("remove_voice", "删除分组中的声音ID", null));
		list.Add(new InquiryElement("set_fallback", "设置全局兜底声音(fallback)", null));
		list.Add(new InquiryElement("export_voice_map", "导出映射（选文件夹）", null));
		list.Add(new InquiryElement("import_voice_map", "导入映射（选文件夹）", null));
		list.Add(new InquiryElement("reload", "重新加载配置文件", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("声音映射管理", stringBuilder.ToString(), list, isExitShown: true, 0, 1, "进入", "返回", OnDevVoiceMappingMenuSelected, delegate
		{
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevVoiceMappingMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "add_voice":
				OpenDevVoiceMappingSelectGroup(isAdd: true);
				break;
			case "remove_voice":
				OpenDevVoiceMappingSelectGroup(isAdd: false);
				break;
			case "set_fallback":
				OpenDevVoiceMappingSetFallback();
				break;
			case "export_voice_map":
				OpenExportFolderPicker("导出（VoiceMapping）- 选择文件夹", ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "import_voice_map":
				OpenImportFolderPicker("导入（VoiceMapping）- 选择文件夹", ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "reload":
				VoiceMapper.ReloadConfig();
				if (!string.IsNullOrWhiteSpace(_voiceMappingJsonStorage))
				{
					VoiceMapper.ImportMappingJson(_voiceMappingJsonStorage, overwriteExisting: true, saveToFile: false);
					InformationManager.DisplayMessage(new InformationMessage("已从当前存档重新加载声音映射。"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("当前存档中没有声音映射数据。"));
				}
				OpenDevVoiceMappingMenu();
				break;
			}
		}
	}

	private void OpenDevVoiceMappingSelectGroup(bool isAdd)
	{
		string titleText = (isAdd ? "添加声音ID - 选择分组" : "删除声音ID - 选择分组");
		string descriptionText = (isAdd ? "选择分组后输入要添加的声音ID。" : "选择要删除声音ID的分组：");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		foreach (string text in allGroupKeys)
		{
			List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(text);
			string title = $"{GetVoiceGroupShortName(text)} ({voicesForGroup.Count})";
			if (isAdd || voicesForGroup.Count != 0)
			{
				list.Add(new InquiryElement(text, title, null));
			}
		}
		if (!isAdd && list.Count <= 1)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有任何声音ID可删除。"));
			OpenDevVoiceMappingMenu();
			return;
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevVoiceMappingMenu();
				}
				else if (isAdd)
				{
					OpenDevVoiceMappingAddVoice(text2);
				}
				else
				{
					OpenDevVoiceMappingRemoveVoice(text2);
				}
			}
		}, delegate
		{
			OpenDevVoiceMappingMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevVoiceMappingAddVoice(string groupKey)
	{
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组: " + displayName);
		stringBuilder.AppendLine($"当前数量: {voicesForGroup.Count}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("请输入要添加的声音ID：");
		stringBuilder.AppendLine("（与MCM中的TTS声音ID一致）");
		InformationManager.ShowTextInquiry(new TextInquiryData("添加声音ID - " + displayName, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "添加", "返回", delegate(string input)
		{
			string text = (input ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				OpenDevVoiceMappingSelectGroup(isAdd: true);
			}
			else
			{
				VoiceMapper.AddVoiceToGroup(groupKey, text);
				InformationManager.DisplayMessage(new InformationMessage("已添加声音 \"" + text + "\" 到 [" + displayName + "]"));
				OpenDevVoiceMappingMenu();
			}
		}, delegate
		{
			OpenDevVoiceMappingSelectGroup(isAdd: true);
		}));
	}

	private void OpenDevVoiceMappingRemoveVoice(string groupKey)
	{
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		if (voicesForGroup.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("[" + displayName + "] 分组没有声音ID可删除。"));
			OpenDevVoiceMappingSelectGroup(isAdd: false);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("clear_all", "⚠ 清空该分组所有声音", null));
		for (int i = 0; i < voicesForGroup.Count; i++)
		{
			list.Add(new InquiryElement(voicesForGroup[i], $"{i + 1}. {voicesForGroup[i]}", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除声音ID - " + displayName, $"分组: {displayName}\n当前共 {voicesForGroup.Count} 个声音ID。\n选择要删除的声音：", list, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingSelectGroup(isAdd: false);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "back")
				{
					OpenDevVoiceMappingSelectGroup(isAdd: false);
				}
				else if (text == "clear_all")
				{
					List<string> voicesForGroup2 = VoiceMapper.GetVoicesForGroup(groupKey);
					foreach (string item in voicesForGroup2)
					{
						VoiceMapper.RemoveVoiceFromGroup(groupKey, item);
					}
					InformationManager.DisplayMessage(new InformationMessage("已清空 [" + displayName + "] 的所有声音ID"));
					OpenDevVoiceMappingMenu();
				}
				else
				{
					VoiceMapper.RemoveVoiceFromGroup(groupKey, text);
					InformationManager.DisplayMessage(new InformationMessage("已删除声音 \"" + text + "\" from [" + displayName + "]"));
					List<string> voicesForGroup3 = VoiceMapper.GetVoicesForGroup(groupKey);
					if (voicesForGroup3.Count > 0)
					{
						OpenDevVoiceMappingRemoveVoice(groupKey);
					}
					else
					{
						OpenDevVoiceMappingMenu();
					}
				}
			}
		}, delegate
		{
			OpenDevVoiceMappingSelectGroup(isAdd: false);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OpenDevVoiceMappingSetFallback()
	{
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("全局兜底声音(fallback)：当某个分组没有配置声音ID时，使用此声音。");
		stringBuilder.AppendLine("当前: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "（未设置，最终回退到MCM全局设置）" : fallbackVoice));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("输入新的 fallback 声音ID（留空=清除）：");
		InformationManager.ShowTextInquiry(new TextInquiryData("设置全局兜底声音(fallback)", stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			string text = (input ?? "").Trim();
			VoiceMapper.SetFallbackVoice(text);
			string information = (string.IsNullOrWhiteSpace(text) ? "fallback 已清除" : ("fallback 已设置为: " + text));
			InformationManager.DisplayMessage(new InformationMessage(information));
			OpenDevVoiceMappingMenu();
		}, delegate
		{
			OpenDevVoiceMappingMenu();
		}, shouldInputBeObfuscated: false, null, fallbackVoice ?? ""));
	}

	private void ReturnToDevRootMenu()
	{
		try
		{
			GameMenu.SwitchToMenu("AnimusForge_dev_root");
		}
		catch
		{
		}
	}

	private void OpenExportFolderPicker(string title, ExportImportScope scope)
	{
		OpenFolderPicker(title, isExport: true, scope, ReturnToDevRootMenu, null);
	}

	private void OpenExportFolderPicker(string title, ExportImportScope scope, Action onReturn)
	{
		OpenFolderPicker(title, isExport: true, scope, onReturn, null);
	}

	private void OpenImportFolderPicker(string title, ExportImportScope scope)
	{
		OpenFolderPicker(title, isExport: false, scope, ReturnToDevRootMenu, null);
	}

	private void OpenImportFolderPicker(string title, ExportImportScope scope, Action onReturn)
	{
		OpenFolderPicker(title, isExport: false, scope, onReturn, null);
	}

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope)
	{
		OpenFolderPicker(title, isExport, scope, ReturnToDevRootMenu, null);
	}

	private void OpenFolderPickerWithCallback(string title, bool isExport, Action<string> onSelectedFolder, Action onReturn)
	{
		if (onSelectedFolder == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = ReturnToDevRootMenu;
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		Directory.CreateDirectory(playerExportsRootPath);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", isExport ? "手动输入文件夹名…" : "手动输入文件夹名/路径…", null));
		if (!isExport)
		{
			list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(playerExportsRootPath);
			List<DirectoryInfo> list2 = (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string title2 = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement(item.Name, title2, null));
			}
		}
		catch
		{
		}
		string descriptionText = (isExport ? "选择目标文件夹（可覆盖已有）。" : "选择来源文件夹。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名/路径", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出；也可输入完整路径（文件夹或 .json 文件）。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						onSelectedFolder(input);
						onReturn();
					}, delegate
					{
						onReturn();
					}));
				}
				else if (!isExport && text == "__latest__")
				{
					onSelectedFolder("");
					onReturn();
				}
				else
				{
					onSelectedFolder(text);
					onReturn();
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void ShowDuplicateImportInquiry(string title, string text, Action onOverwrite, Action onSkipDuplicates, Action onCancel)
	{
		try
		{
			if (onOverwrite == null)
			{
				onOverwrite = delegate
				{
				};
			}
			if (onSkipDuplicates == null)
			{
				onSkipDuplicates = delegate
				{
				};
			}
			if (onCancel == null)
			{
				onCancel = delegate
				{
				};
			}
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("__overwrite__", "覆盖导入", null),
				new InquiryElement("__skip__", "只导入非重复信息", null),
				new InquiryElement("__cancel__", "取消", null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, "选择", "取消", delegate(List<InquiryElement> selected)
			{
				string text2 = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : "");
				if (text2 == "__overwrite__")
				{
					onOverwrite();
				}
				else if (text2 == "__skip__")
				{
					onSkipDuplicates();
				}
				else
				{
					onCancel();
				}
			}, delegate
			{
				onCancel();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}

	private static bool IsDirectoryNonEmpty(string dir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(dir))
			{
				return false;
			}
			if (!Directory.Exists(dir))
			{
				return false;
			}
			return Directory.EnumerateFileSystemEntries(dir).Any();
		}
		catch
		{
			return false;
		}
	}

	private void ShowOverwriteExportInquiry(string title, string text, Action onOverwrite, Action onNewFolder, Action onCancel)
	{
		try
		{
			if (onOverwrite == null)
			{
				onOverwrite = delegate
				{
				};
			}
			if (onNewFolder == null)
			{
				onNewFolder = delegate
				{
				};
			}
			if (onCancel == null)
			{
				onCancel = delegate
				{
				};
			}
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("__overwrite__", "覆盖导出", null),
				new InquiryElement("__new__", "改用新文件夹（自动）", null),
				new InquiryElement("__cancel__", "取消", null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, "选择", "取消", delegate(List<InquiryElement> selected)
			{
				string text2 = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : "");
				if (text2 == "__overwrite__")
				{
					onOverwrite();
				}
				else if (text2 == "__new__")
				{
					onNewFolder();
				}
				else
				{
					onCancel();
				}
			}, delegate
			{
				onCancel();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope, Action onReturn, string heroId)
	{
		if (onReturn == null)
		{
			onReturn = ReturnToDevRootMenu;
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		Directory.CreateDirectory(playerExportsRootPath);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", "手动输入文件夹名…", null));
		if (!isExport)
		{
			list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(playerExportsRootPath);
			List<DirectoryInfo> list2 = (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string title2 = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement(item.Name, title2, null));
			}
		}
		catch
		{
		}
		string descriptionText = (isExport ? "选择要导出的目标文件夹（可覆盖已有）。" : "选择要导入的来源文件夹。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
					{
						if (string.IsNullOrEmpty(heroId))
						{
							ResolveAndRunExportImport(isExport, scope, input);
						}
						else
						{
							ResolveAndRunExportImportForHero(isExport, scope, input, heroId);
						}
						onReturn();
					}, delegate
					{
						onReturn();
					}));
				}
				else if (!isExport && text == "__latest__")
				{
					if (string.IsNullOrEmpty(heroId))
					{
						ResolveAndRunExportImport(isExport: false, scope, "");
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport: false, scope, "", heroId);
					}
					onReturn();
				}
				else
				{
					if (string.IsNullOrEmpty(heroId))
					{
						ResolveAndRunExportImport(isExport, scope, text);
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport, scope, text, heroId);
					}
					onReturn();
				}
			}
		}, delegate
		{
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void ResolveAndRunExportImport(bool isExport, ExportImportScope scope, string folderName)
	{
		if (isExport)
		{
			Action run = delegate
			{
				if (scope == ExportImportScope.All)
				{
					ExportAllData(folderName);
				}
				else if (scope == ExportImportScope.HeroNpcAll)
				{
					ExportHeroNpcAllData(folderName);
				}
				else if (scope == ExportImportScope.PersonalityBackground)
				{
					ExportPersonaData(folderName);
				}
				else if (scope == ExportImportScope.UnnamedPersona)
				{
					ExportUnnamedPersonaData(folderName);
				}
				else if (scope == ExportImportScope.DialogueHistory)
				{
					ExportDialogueHistoryData(folderName);
				}
				else if (scope == ExportImportScope.Debt)
				{
					ExportDebtData(folderName);
				}
				else if (scope == ExportImportScope.EventData)
				{
					ExportEventData(folderName);
				}
				else if (scope == ExportImportScope.Knowledge)
				{
					ExportKnowledgeData(folderName);
				}
				else if (scope == ExportImportScope.VoiceMapping)
				{
					ExportVoiceMappingData(folderName);
				}
			};
			string value = SanitizeFolderName(folderName);
			if (!string.IsNullOrEmpty(value))
			{
				string playerExportsRootPath = GetPlayerExportsRootPath();
				Directory.CreateDirectory(playerExportsRootPath);
				string path = ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry("导出文件夹已存在", "目标文件夹已存在且包含内容：\n" + text + "\n是否覆盖导出？", delegate
					{
						run();
					}, delegate
					{
						ResolveAndRunExportImport(isExport: true, scope, "");
					}, delegate
					{
					});
					return;
				}
			}
			run();
		}
		else if (scope == ExportImportScope.All)
		{
			ImportAllData(folderName);
		}
		else if (scope == ExportImportScope.HeroNpcAll)
		{
			ImportHeroNpcAllData(folderName);
		}
		else if (scope == ExportImportScope.PersonalityBackground)
		{
			ImportPersonaData(folderName);
		}
		else if (scope == ExportImportScope.UnnamedPersona)
		{
			ImportUnnamedPersonaData(folderName);
		}
		else if (scope == ExportImportScope.DialogueHistory)
		{
			ImportDialogueHistoryData(folderName);
		}
		else if (scope == ExportImportScope.Debt)
		{
			ImportDebtData(folderName);
		}
		else if (scope == ExportImportScope.EventData)
		{
			ImportEventData(folderName);
		}
		else if (scope == ExportImportScope.Knowledge)
		{
			ImportKnowledgeData(folderName);
		}
		else if (scope == ExportImportScope.VoiceMapping)
		{
			ImportVoiceMappingData(folderName);
		}
	}

	private void ResolveAndRunExportImportForHero(bool isExport, ExportImportScope scope, string folderName, string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
		}
		else if (isExport)
		{
			Action run = delegate
			{
				if (scope == ExportImportScope.PersonalityBackground)
				{
					ExportSingleNpcPersonaData(folderName, id);
				}
				else if (scope == ExportImportScope.DialogueHistory)
				{
					ExportSingleNpcDialogueHistoryData(folderName, id);
				}
				else if (scope == ExportImportScope.Debt)
				{
					ExportSingleNpcDebtData(folderName, id);
				}
			};
			string value = SanitizeFolderName(folderName);
			if (!string.IsNullOrEmpty(value))
			{
				string playerExportsRootPath = GetPlayerExportsRootPath();
				Directory.CreateDirectory(playerExportsRootPath);
				string path = ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry("导出文件夹已存在", "目标文件夹已存在且包含内容：\n" + text + "\n是否覆盖导出？", delegate
					{
						run();
					}, delegate
					{
						ResolveAndRunExportImportForHero(isExport: true, scope, "", id);
					}, delegate
					{
					});
					return;
				}
			}
			run();
		}
		else if (scope == ExportImportScope.PersonalityBackground)
		{
			ImportSingleNpcPersonaData(folderName, id);
		}
		else if (scope == ExportImportScope.DialogueHistory)
		{
			ImportSingleNpcDialogueHistoryData(folderName, id);
		}
		else if (scope == ExportImportScope.Debt)
		{
			ImportSingleNpcDebtData(folderName, id);
		}
	}

	private void ExportSingleNpcPersonaData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			NpcPersonaProfile value = null;
			if (_npcPersonaProfiles != null)
			{
				_npcPersonaProfiles.TryGetValue(heroId, out value);
			}
			if (value == null)
			{
				value = new NpcPersonaProfile();
			}
			string path2 = Path.Combine(text2, BuildNpcDataFileName(heroId));
			WriteJson(path2, value);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text2);
			List<DialogueDay> value = null;
			if (_dialogueHistory != null)
			{
				_dialogueHistory.TryGetValue(heroId, out value);
			}
			if (value == null)
			{
				value = new List<DialogueDay>();
			}
			string path2 = Path.Combine(text2, BuildNpcDataFileName(heroId));
			WriteJson(path2, value);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			RewardSystemBehavior.DebtExportEntry value = null;
			(RewardSystemBehavior.Instance?.ExportDebtEntries())?.TryGetValue(heroId, out value);
			if (value == null)
			{
				value = new RewardSystemBehavior.DebtExportEntry();
			}
			string path2 = Path.Combine(text2, BuildNpcDataFileName(heroId));
			WriteJson(path2, value);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "debt");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "debt", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
					return;
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> all = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				all.Remove(heroId);
				rs.ImportDebtEntries(all);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				return;
			}
			RewardSystemBehavior.DebtExportEntry entry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text3);
			bool flag = entry != null;
			bool flag2 = all.ContainsKey(heroId);
			Action action = delegate
			{
				if (entry == null)
				{
					all.Remove(heroId);
				}
				else
				{
					all[heroId] = entry;
				}
				rs.ImportDebtEntries(all);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 欠款", "检测到该 NPC 已存在欠款记录：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcPersonaData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "personality_background");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "personality_background", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
					return;
				}
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			NpcPersonaProfile prof = ReadJson<NpcPersonaProfile>(text3);
			if (_npcPersonaProfiles == null)
			{
				_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
			}
			bool flag = prof != null;
			bool flag2 = false;
			try
			{
				flag2 = _npcPersonaProfiles.TryGetValue(heroId, out var value) && value != null;
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (prof == null)
				{
					_npcPersonaProfiles.Remove(heroId);
				}
				else
				{
					_npcPersonaProfiles[heroId] = prof;
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 个性/背景", "检测到该 NPC 已存在个性/背景：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "dialogue_history");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "dialogue_history", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 dialogue_history 目录。"));
					return;
				}
				text3 = FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			List<DialogueDay> list = ReadJson<List<DialogueDay>>(text3);
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			bool flag = list != null;
			bool flag2 = false;
			try
			{
				flag2 = _dialogueHistory.TryGetValue(heroId, out var value) && value != null;
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (list == null)
				{
					_dialogueHistory.Remove(heroId);
				}
				else
				{
					_dialogueHistory[heroId] = list;
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				ShowDuplicateImportInquiry("检测到重复 - 对话历史", "检测到该 NPC 已存在对话历史：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportHeroNpcAllData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && npcPersonaProfile.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(npcPersonaProfile.Key));
						WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			string text3 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text3);
			ClearJsonFiles(text3);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path3 = Path.Combine(text3, BuildNpcDataFileName(item.Key));
						WriteJson(path3, item.Value);
					}
				}
			}
			string text4 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text4);
			ClearJsonFiles(text4);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dictionary)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						string path4 = Path.Combine(text4, BuildNpcDataFileName(item2.Key));
						WriteJson(path4, item2.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportHeroNpcAllData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!ValidateUnnamedPersonaKeysForImport(importDir, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				return;
			}
			Dictionary<string, NpcPersonaProfile> pbNew = null;
			Dictionary<string, List<DialogueDay>> dhNew = null;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> debtNew = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					string text2 = TryParseHeroIdFromNpcFileName(text);
					if (!string.IsNullOrEmpty(text2))
					{
						NpcPersonaProfile npcPersonaProfile = ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							pbNew[text2] = npcPersonaProfile;
						}
					}
				}
				num2 = pbNew.Count;
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			string path2 = Path.Combine(importDir, "dialogue_history");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, List<DialogueDay>>();
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = TryParseHeroIdFromNpcFileName(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						List<DialogueDay> list = ReadJson<List<DialogueDay>>(text3);
						if (list != null)
						{
							dhNew[text4] = list;
						}
					}
				}
				num4 = dhNew.Count;
				if (_dialogueHistory != null)
				{
					foreach (string key2 in dhNew.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && _dialogueHistory.ContainsKey(key2))
						{
							num3++;
						}
					}
				}
			}
			string path3 = Path.Combine(importDir, "debt");
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = TryParseHeroIdFromNpcFileName(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error2))
				{
					InformationManager.DisplayMessage(new InformationMessage(error2));
				}
				else
				{
					if (pbNew != null)
					{
						if (_npcPersonaProfiles == null)
						{
							_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
						}
						foreach (KeyValuePair<string, NpcPersonaProfile> item in pbNew)
						{
							if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
							{
								_npcPersonaProfiles.Remove(item.Key);
								_npcPersonaProfiles[item.Key] = item.Value;
							}
						}
					}
					if (dhNew != null)
					{
						if (_dialogueHistory == null)
						{
							_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
						}
						foreach (KeyValuePair<string, List<DialogueDay>> item2 in dhNew)
						{
							if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
							{
								_dialogueHistory.Remove(item2.Key);
								_dialogueHistory[item2.Key] = item2.Value;
							}
						}
					}
					if (debtNew != null && rs != null)
					{
						Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
						foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item3 in debtNew)
						{
							if (!string.IsNullOrEmpty(item3.Key) && item3.Value != null)
							{
								dictionary.Remove(item3.Key);
								dictionary[item3.Key] = item3.Value;
							}
						}
						rs.ImportDebtEntries(dictionary);
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error2))
				{
					InformationManager.DisplayMessage(new InformationMessage(error2));
				}
				else
				{
					if (pbNew != null)
					{
						if (_npcPersonaProfiles == null)
						{
							_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
						}
						foreach (KeyValuePair<string, NpcPersonaProfile> item4 in pbNew)
						{
							if (!string.IsNullOrEmpty(item4.Key) && item4.Value != null && !_npcPersonaProfiles.ContainsKey(item4.Key))
							{
								_npcPersonaProfiles[item4.Key] = item4.Value;
							}
						}
					}
					if (dhNew != null)
					{
						if (_dialogueHistory == null)
						{
							_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
						}
						foreach (KeyValuePair<string, List<DialogueDay>> item5 in dhNew)
						{
							if (!string.IsNullOrEmpty(item5.Key) && item5.Value != null && !_dialogueHistory.ContainsKey(item5.Key))
							{
								_dialogueHistory[item5.Key] = item5.Value;
							}
						}
					}
					if (debtNew != null && rs != null)
					{
						Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
						foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item6 in debtNew)
						{
							if (!string.IsNullOrEmpty(item6.Key) && item6.Value != null && !dictionary.ContainsKey(item6.Key))
							{
								dictionary[item6.Key] = item6.Value;
							}
						}
						rs.ImportDebtEntries(dictionary);
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
				}
			};
			if (flag)
			{
				string text7 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n对话历史：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - HeroNPC 全量导入", text7, action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportAllData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && npcPersonaProfile.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(npcPersonaProfile.Key));
						WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			string text3 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text3);
			ClearJsonFiles(text3);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path3 = Path.Combine(text3, BuildNpcDataFileName(item.Key));
						WriteJson(path3, item.Value);
					}
				}
			}
			string text4 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text4);
			ClearJsonFiles(text4);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dictionary)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						string path4 = Path.Combine(text4, BuildNpcDataFileName(item2.Key));
						WriteJson(path4, item2.Value);
					}
				}
			}
			if (!TryExportKnowledgeToDir(text, out var exportedKnowledgeCount, out var knowledgeExportError))
			{
				InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导出失败，已跳过。原因：" + knowledgeExportError));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("Knowledge 已导出 " + exportedKnowledgeCount + " 条。"));
			}
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			string text5 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text5);
			ClearJsonFiles(text5);
			string path5 = Path.Combine(text5, "VoiceMapping.json");
			string text6 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = "{}";
			}
			File.WriteAllText(path5, text6, Encoding.UTF8);
			ExportEventDataToDir(text);
			VoiceMapper.SetPreferredExportFolder(text);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportUnnamedPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_npcPersonaProfiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && npcPersonaProfile.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(npcPersonaProfile.Key));
						WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportDialogueHistoryData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			if (_dialogueHistory != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(item.Key));
						WriteJson(path2, item.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportDebtData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item in dictionary)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path2 = Path.Combine(text2, BuildNpcDataFileName(item.Key));
						WriteJson(path2, item.Value);
					}
				}
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportKnowledgeData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			if (!TryExportKnowledgeToDir(text, out var exportedCount, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text + "（Knowledge " + exportedCount + " 条）"));
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportEventData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			ExportEventDataToDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportEventDataToDir(string exportDir)
	{
		string text = Path.Combine(exportDir, "event_data");
		Directory.CreateDirectory(text);
		ClearJsonFiles(text);
		WriteJson(Path.Combine(text, "WorldOpeningSummary.json"), new EventWorldOpeningSummaryJson
		{
			Summary = (_eventWorldOpeningSummary ?? "").Trim()
		});
		WriteJson(Path.Combine(text, "KingdomOpeningSummaries.json"), BuildEventKingdomSummaryExportMap());
		WriteJson(Path.Combine(text, "EventRecords.json"), SanitizeEventRecordEntries(_eventRecordEntries));
	}

	private Dictionary<string, string> BuildEventKingdomSummaryExportMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (_eventKingdomOpeningSummaries == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, string> item in _eventKingdomOpeningSummaries)
		{
			string text = (item.Key ?? "").Trim();
			string text2 = (item.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
			{
				dictionary[text] = text2;
			}
		}
		return dictionary;
	}

	private void ExportKnowledgeToDir(string exportDir)
	{
		TryExportKnowledgeToDir(exportDir, out var _, out var _);
	}

	private bool TryExportKnowledgeToDir(string exportDir, out int exportedCount, out string error)
	{
		exportedCount = 0;
		error = "";
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				error = "KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (!knowledgeLibraryBehavior.TryValidateKnowledgeExport(out error))
			{
				return false;
			}
			string value = knowledgeLibraryBehavior.ExportRulesJson();
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = (string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value));
			if (knowledgeFile?.Rules == null || knowledgeFile.Rules.Count <= 0)
			{
				error = "当前没有可导出的知识条目。";
				return false;
			}
			string text = Path.Combine(exportDir, "knowledge", "rules");
			Directory.CreateDirectory(text);
			ClearJsonFiles(text);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
			{
				string text2 = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text2) || !hashSet.Add(text2))
				{
					continue;
				}
				rule.Id = text2;
				string text3 = "";
				try
				{
					text3 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text3 = "";
				}
				string text4 = text2;
				char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
				foreach (char oldChar in invalidFileNameChars)
				{
					text4 = text4.Replace(oldChar, '_');
				}
				text4 = (text4 ?? "").Trim();
				if (text4.Length > 80)
				{
					text4 = text4.Substring(0, 80);
				}
				if (string.IsNullOrEmpty(text4))
				{
					text4 = "rule";
				}
				string text5 = text3;
				char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
				foreach (char oldChar2 in invalidFileNameChars2)
				{
					text5 = text5.Replace(oldChar2, '_');
				}
				text5 = (text5 ?? "").Trim();
				if (text5.Length > 35)
				{
					text5 = text5.Substring(0, 35);
				}
				string text6 = text4;
				if (!string.IsNullOrEmpty(text5))
				{
					text6 = text6 + "__" + text5;
				}
				string path = Path.Combine(text, text6 + ".json");
				if (File.Exists(path))
				{
					for (int num = 2; num <= 999; num++)
					{
						string text7 = Path.Combine(text, text6 + "__" + num + ".json");
						if (!File.Exists(text7))
						{
							path = text7;
							break;
						}
					}
				}
				string contents = JsonConvert.SerializeObject(rule, Formatting.Indented);
				File.WriteAllText(path, contents, Encoding.UTF8);
				exportedCount++;
			}
			if (exportedCount <= 0)
			{
				error = "没有成功写出任何知识文件。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private void ExportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：RuleId 为空。"));
				return;
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			if (!knowledgeLibraryBehavior.TryValidateSingleRuleExport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
				return;
			}
			string text2 = knowledgeLibraryBehavior.ExportSingleRuleJson(text, pretty: true);
			if (string.IsNullOrWhiteSpace(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该知识条目：" + text));
				return;
			}
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text3 = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text3);
			string text4 = Path.Combine(text3, "knowledge", "rules");
			Directory.CreateDirectory(text4);
			KnowledgeLibraryBehavior.LoreRule loreRule = null;
			try
			{
				loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text2);
			}
			catch
			{
				loreRule = null;
			}
			string text5 = "";
			try
			{
				text5 = loreRule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text5 = "";
			}
			string text6 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text6 = text6.Replace(oldChar, '_');
			}
			text6 = (text6 ?? "").Trim();
			if (text6.Length > 80)
			{
				text6 = text6.Substring(0, 80);
			}
			if (string.IsNullOrEmpty(text6))
			{
				text6 = "rule";
			}
			string text7 = text5;
			char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
			foreach (char oldChar2 in invalidFileNameChars2)
			{
				text7 = text7.Replace(oldChar2, '_');
			}
			text7 = (text7 ?? "").Trim();
			if (text7.Length > 35)
			{
				text7 = text7.Substring(0, 35);
			}
			string text8 = text6;
			if (!string.IsNullOrEmpty(text7))
			{
				text8 = text8 + "__" + text7;
			}
			string path2 = Path.Combine(text4, text8 + ".json");
			File.WriteAllText(path2, text2, Encoding.UTF8);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text3));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(id))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RuleId 为空。"));
				return;
			}
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			KnowledgeLibraryBehavior kb = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (kb == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			string dir = Path.Combine(importDir, "knowledge", "rules");
			string dir2 = Path.Combine(importDir, "knowledge", "single_rules");
			string dir3 = Path.Combine(importDir, "knowledge");
			string text = FindKnowledgeRuleJsonById(dir, id);
			if (string.IsNullOrEmpty(text))
			{
				text = FindKnowledgeRuleJsonById(dir2, id);
			}
			if (string.IsNullOrEmpty(text))
			{
				text = FindKnowledgeRuleJsonById(dir3, id);
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该知识条目的导出文件：" + id));
				return;
			}
			string value = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件为空。"));
				return;
			}
			KnowledgeLibraryBehavior.LoreRule rule = null;
			try
			{
				rule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
			}
			catch
			{
				rule = null;
			}
			if (rule == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text2 = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text2) && !string.Equals(text2, id, StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件中的 Id 不匹配（文件Id=" + text2 + "）。"));
				return;
			}
			if (string.IsNullOrEmpty(text2))
			{
				rule.Id = id;
			}
			bool flag = false;
			try
			{
				flag = !string.IsNullOrWhiteSpace(kb.ExportSingleRuleJson(id));
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None)))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：写入规则失败。"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None), overwrite: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + id));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			if (flag)
			{
				ShowDuplicateImportInquiry("检测到重复 - Knowledge", "检测到相同 RuleId：" + id + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
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
			foreach (char c in text)
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

	private static IEnumerable<string> GetKnowledgeKeywordsForCompare(KnowledgeLibraryBehavior.LoreRule rule)
	{
		if (rule?.Keywords == null || rule.Keywords.Count <= 0)
		{
			yield break;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string k in rule.Keywords)
		{
			string kk = NormalizeKeywordForCompare(k);
			if (!string.IsNullOrEmpty(kk) && seen.Add(kk))
			{
				yield return kk;
			}
		}
	}

	private static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir(string importDir)
	{
		try
		{
			List<KnowledgeLibraryBehavior.LoreRule> list = new List<KnowledgeLibraryBehavior.LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text = (rule?.Id ?? "").Trim();
					if (!string.IsNullOrEmpty(text))
					{
						rule.Id = text;
						if (hashSet.Add(text))
						{
							list.Add(rule);
						}
					}
				}
			}
			string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
			if (File.Exists(path))
			{
				try
				{
					string value = File.ReadAllText(path, Encoding.UTF8);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
					if (knowledgeFile2?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
						{
							string text2 = (rule2?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2))
							{
								rule2.Id = text2;
								if (hashSet.Add(text2))
								{
									list.Add(rule2);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			return list;
		}
		catch
		{
			return new List<KnowledgeLibraryBehavior.LoreRule>();
		}
	}

	private static bool ValidateKnowledgeKeywordsForSingleRuleImport(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting, out string error)
	{
		error = "";
		try
		{
			if (kb == null)
			{
				error = "导入失败：KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (rule == null)
			{
				error = "导入失败：规则为空。";
				return false;
			}
			string text = (rule.Id ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				error = "导入失败：RuleId 为空。";
				return false;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (rule.Keywords != null)
			{
				foreach (string keyword in rule.Keywords)
				{
					string text2 = NormalizeKeywordForCompare(keyword);
					if (!string.IsNullOrEmpty(text2))
					{
						if (dictionary.ContainsKey(text2))
						{
							error = "导入失败：导入规则中存在重复关键词（" + text2 + "）。";
							return false;
						}
						dictionary[text2] = text;
					}
				}
			}
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = null;
			try
			{
				string value = kb.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(value))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
				}
			}
			catch
			{
				knowledgeFile = null;
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile.Rules)
				{
					string text3 = (rule2?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text3) || (overwriteExisting && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase)))
					{
						continue;
					}
					foreach (string item in GetKnowledgeKeywordsForCompare(rule2))
					{
						if (dictionary2.TryGetValue(item, out var value2) && !string.Equals(value2, text3, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item] = text3;
					}
				}
			}
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				if (dictionary2.TryGetValue(item2.Key, out var value3) && !string.Equals(value3, text, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item2.Key + "）。当前存档中该关键词属于规则 " + value3 + "，导入规则为 " + text + "。";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：关键词校验异常：" + ex.Message;
			return false;
		}
	}

	private static bool ValidateKnowledgeKeywordsForImport(string importDir, bool overwriteExisting, out string error)
	{
		error = "";
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				error = "导入失败：KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			List<KnowledgeLibraryBehavior.LoreRule> list = LoadKnowledgeRulesFromImportDir(importDir);
			if (list == null || list.Count <= 0)
			{
				error = "导入失败：导入目录中未找到可导入的知识规则文件。";
				return false;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item in list)
			{
				string text = (item?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				foreach (string item2 in GetKnowledgeKeywordsForCompare(item))
				{
					if (dictionary.TryGetValue(item2, out var value) && !string.Equals(value, text, StringComparison.OrdinalIgnoreCase))
					{
						error = "导入失败：导入文件夹中存在重复关键词（" + item2 + "），分别属于规则 " + value + " 与 " + text + "。";
						return false;
					}
					dictionary[item2] = text;
				}
			}
			HashSet<string> existingIds = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			List<KnowledgeLibraryBehavior.LoreRule> list2 = list;
			if (!overwriteExisting)
			{
				list2 = list.Where((KnowledgeLibraryBehavior.LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id) && !existingIds.Contains((r.Id ?? "").Trim())).ToList();
			}
			HashSet<string> hashSet = (overwriteExisting ? new HashSet<string>(from r in list
				where r != null
				select (r.Id ?? "").Trim() into x
				where !string.IsNullOrEmpty(x)
				select x, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = null;
			try
			{
				string value2 = knowledgeLibraryBehavior.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(value2))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value2);
				}
			}
			catch
			{
				knowledgeFile = null;
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text2 = (rule?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text2) || hashSet.Contains(text2))
					{
						continue;
					}
					foreach (string item3 in GetKnowledgeKeywordsForCompare(rule))
					{
						if (dictionary2.TryGetValue(item3, out var value3) && !string.Equals(value3, text2, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item3 + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item3] = text2;
					}
				}
			}
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item4 in list2)
			{
				string value4 = (item4?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(value4))
				{
					continue;
				}
				foreach (string item5 in GetKnowledgeKeywordsForCompare(item4))
				{
					dictionary3[item5] = value4;
				}
			}
			foreach (KeyValuePair<string, string> item6 in dictionary3)
			{
				if (dictionary2.TryGetValue(item6.Key, out var value5) && !string.Equals(value5, item6.Value, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item6.Key + "）。当前存档中该关键词属于规则 " + value5 + "，导入规则为 " + item6.Value + "。";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：关键词校验异常：" + ex.Message;
			return false;
		}
	}

	private static KnowledgeLibraryBehavior.KnowledgeFile TryLoadKnowledgeRulesFromRuleFiles(string importDir)
	{
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				return null;
			}
			List<string> source = new List<string>
			{
				Path.Combine(importDir, "knowledge", "rules"),
				Path.Combine(importDir, "knowledge", "single_rules"),
				Path.Combine(importDir, "knowledge")
			};
			List<KnowledgeLibraryBehavior.LoreRule> list = new List<KnowledgeLibraryBehavior.LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					try
					{
						string a = Path.GetFileName(path) ?? "";
						if (string.Equals(a, "AIConfig.json", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "KnowledgeRules.json", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (string.IsNullOrWhiteSpace(value))
						{
							continue;
						}
						KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
						string text = (loreRule?.Id ?? "").Trim();
						if (!string.IsNullOrEmpty(text))
						{
							loreRule.Id = text;
							if (hashSet.Add(text))
							{
								list.Add(loreRule);
							}
						}
					}
					catch
					{
					}
				}
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return new KnowledgeLibraryBehavior.KnowledgeFile
			{
				Version = 1,
				Rules = list
			};
		}
		catch
		{
			return null;
		}
	}

	private static string FindKnowledgeRuleJsonById(string dir, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
					if (loreRule != null)
					{
						string a = (loreRule.Id ?? "").Trim();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private string TryGetUnnamedPersonaKeyFromImportFile(string file)
	{
		string text = null;
		try
		{
			text = (ReadJson<UnnamedPersonaSingleJson>(file)?.Key ?? "").Trim().ToLower();
		}
		catch
		{
			text = null;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				string text2 = Path.GetFileNameWithoutExtension(file) ?? "";
				int num = text2.LastIndexOf("__", StringComparison.Ordinal);
				if (num > 0)
				{
					text2 = text2.Substring(0, num);
				}
				text = (text2 ?? "").Trim().ToLower();
			}
			catch
			{
				text = null;
			}
		}
		return (text ?? "").Trim().ToLower();
	}

	private bool ValidateUnnamedPersonaKeysForImport(string importDir, out string error)
	{
		error = "";
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				error = "导入失败：找不到导出目录。";
				return false;
			}
			List<string> source = new List<string>
			{
				Path.Combine(importDir, "unnamed_persona"),
				importDir
			};
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string file in array2)
				{
					string text = TryGetUnnamedPersonaKeyFromImportFile(file);
					if (!string.IsNullOrEmpty(text))
					{
						if (!hashSet.Add(text))
						{
							error = "导入失败：导入文件夹中存在重复 Key（" + text + "）。";
							return false;
						}
						if (ShoutUtils.HasUnnamedPersonaKey(text))
						{
							error = "导入失败：Key 冲突（" + text + "）。当前游戏已存在该 Key，禁止导入覆盖。";
							return false;
						}
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：Key 校验异常：" + ex.Message;
			return false;
		}
	}

	private void ExportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：Key 为空。"));
				return;
			}
			string personality = "";
			string background = "";
			bool flag = false;
			try
			{
				flag = ShoutUtils.TryGetUnnamedPersonaByKey(text, out personality, out background);
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该未命名NPC条目：" + text));
				return;
			}
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text2 = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text2);
			string text3 = Path.Combine(text2, "unnamed_persona");
			Directory.CreateDirectory(text3);
			string text4 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text4 = text4.Replace(oldChar, '_');
			}
			text4 = text4.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text4.Contains("__"))
			{
				text4 = text4.Replace("__", "_");
			}
			text4 = (text4 ?? "").Trim();
			if (text4.Length > 120)
			{
				text4 = text4.Substring(0, 120);
			}
			if (string.IsNullOrEmpty(text4))
			{
				text4 = "unnamed";
			}
			string path2 = Path.Combine(text3, text4 + ".json");
			WriteJson(path2, new UnnamedPersonaSingleJson
			{
				Key = text,
				Personality = (personality ?? "").Trim(),
				Background = (background ?? "").Trim()
			});
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text2));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：Key 为空。"));
				return;
			}
			string text2 = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string dir = Path.Combine(text2, "unnamed_persona");
			string dir2 = text2;
			string text3 = FindUnnamedPersonaJsonByKey(dir, text);
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindUnnamedPersonaJsonByKey(dir2, text);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该未命名NPC条目的导出文件：" + text));
				return;
			}
			UnnamedPersonaSingleJson unnamedPersonaSingleJson = ReadJson<UnnamedPersonaSingleJson>(text3);
			if (unnamedPersonaSingleJson == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text4 = (unnamedPersonaSingleJson.Personality ?? "").Trim();
			string text5 = (unnamedPersonaSingleJson.Background ?? "").Trim();
			bool flag = !string.IsNullOrEmpty(text4) || !string.IsNullOrEmpty(text5);
			bool flag2 = false;
			try
			{
				flag2 = ShoutUtils.HasUnnamedPersonaKey(text);
			}
			catch
			{
				flag2 = false;
			}
			if (flag && flag2)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：检测到重复 Key（" + text + "），当前游戏已存在该条目，禁止导入覆盖。"));
				return;
			}
			ShoutUtils.SaveUnnamedPersonaByKey(text, text4, text5);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text2));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private static string FindUnnamedPersonaJsonByKey(string dir, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = text2.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text2.Contains("__"))
			{
				text2 = text2.Replace("__", "_");
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					UnnamedPersonaSingleJson unnamedPersonaSingleJson = JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value);
					if (unnamedPersonaSingleJson != null)
					{
						string a = (unnamedPersonaSingleJson.Key ?? "").Trim().ToLower();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private List<string> GetKnowledgeRuleIdsFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "knowledge", "rules"),
				Path.Combine(text, "knowledge", "single_rules"),
				Path.Combine(text, "knowledge")
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value)?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	private List<string> GetUnnamedPersonaKeysFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "unnamed_persona"),
				text
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value)?.Key ?? "").Trim().ToLower();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	private void ImportPersonaData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "personality_background");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, NpcPersonaProfile> dict = new Dictionary<string, NpcPersonaProfile>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = TryParseHeroIdFromNpcFileName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					NpcPersonaProfile npcPersonaProfile = ReadJson<NpcPersonaProfile>(text);
					if (npcPersonaProfile != null)
					{
						dict[text2] = npcPersonaProfile;
					}
				}
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in dict.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (_npcPersonaProfiles == null)
				{
					_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
				}
				foreach (KeyValuePair<string, NpcPersonaProfile> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						_npcPersonaProfiles.Remove(item.Key);
						_npcPersonaProfiles[item.Key] = item.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (_npcPersonaProfiles == null)
				{
					_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
				}
				foreach (KeyValuePair<string, NpcPersonaProfile> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null && !_npcPersonaProfiles.ContainsKey(item2.Key))
					{
						_npcPersonaProfiles[item2.Key] = item2.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 个性/背景", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportDialogueHistoryData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "dialogue_history");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 dialogue_history 目录。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, List<DialogueDay>> dict = new Dictionary<string, List<DialogueDay>>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = TryParseHeroIdFromNpcFileName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					List<DialogueDay> list = ReadJson<List<DialogueDay>>(text);
					if (list != null)
					{
						dict[text2] = list;
					}
				}
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				if (_dialogueHistory != null)
				{
					foreach (string key in dict.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _dialogueHistory.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (_dialogueHistory == null)
				{
					_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
				}
				foreach (KeyValuePair<string, List<DialogueDay>> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						_dialogueHistory.Remove(item.Key);
						_dialogueHistory[item.Key] = item.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (_dialogueHistory == null)
				{
					_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
				}
				foreach (KeyValuePair<string, List<DialogueDay>> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null && !_dialogueHistory.ContainsKey(item2.Key))
					{
						_dialogueHistory[item2.Key] = item2.Value;
					}
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 对话历史", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportDebtData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "debt");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
				return;
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dict = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = TryParseHeroIdFromNpcFileName(text);
				if (!string.IsNullOrEmpty(text2))
				{
					RewardSystemBehavior.DebtExportEntry debtExportEntry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text);
					if (debtExportEntry != null)
					{
						dict[text2] = debtExportEntry;
					}
				}
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> exist = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			int num = 0;
			int count = dict.Count;
			try
			{
				foreach (string key in dict.Keys)
				{
					if (!string.IsNullOrEmpty(key) && exist.ContainsKey(key))
					{
						num++;
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = exist ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						dictionary.Remove(item.Key);
						dictionary[item.Key] = item.Value;
					}
				}
				rs.ImportDebtEntries(dictionary);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = exist;
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null && !dictionary.ContainsKey(item2.Key))
					{
						dictionary[item2.Key] = item2.Value;
					}
				}
				rs.ImportDebtEntries(dictionary);
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - 欠款", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportEventData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!TryLoadEventDataFromImportDir(importDir, out var payload, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：" + error));
				return;
			}
			int num = 0;
			int num2 = 0;
			if (payload.HasWorldSummaryFile)
			{
				num2 = 1;
				if (!string.IsNullOrWhiteSpace(_eventWorldOpeningSummary))
				{
					num = 1;
				}
			}
			int num3 = 0;
			int num4 = payload.HasKingdomSummariesFile ? payload.KingdomSummaries.Count : 0;
			if (payload.HasKingdomSummariesFile && _eventKingdomOpeningSummaries != null)
			{
				foreach (string key in payload.KingdomSummaries.Keys)
				{
					if (!string.IsNullOrWhiteSpace(key) && _eventKingdomOpeningSummaries.ContainsKey(key))
					{
						num3++;
					}
				}
			}
			int num5 = 0;
			int num6 = payload.HasEventRecordsFile ? payload.EventRecords.Count : 0;
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_eventRecordEntries != null)
			{
				foreach (EventRecordEntry eventRecordEntry in _eventRecordEntries)
				{
					string text = (eventRecordEntry?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text))
					{
						hashSet.Add(text);
					}
				}
			}
			if (payload.HasEventRecordsFile)
			{
				foreach (EventRecordEntry eventRecordEntry2 in payload.EventRecords)
				{
					string text2 = (eventRecordEntry2?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && hashSet.Contains(text2))
					{
						num5++;
					}
				}
			}
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				ApplyImportedEventData(payload, overwriteExisting: true);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				ApplyImportedEventData(payload, overwriteExisting: false);
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (flag)
			{
				string text3 = "导入数据与当前游戏存在重复。\n世界开局概要：" + num + "/" + num2 + "\n王国开局概要：" + num3 + "/" + num4 + "\n事件记录：" + num5 + "/" + num6 + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - 事件编辑", text3, action, onSkipDuplicates, delegate
				{
					OpenDevEventEditorMenu();
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportUnnamedPersonaData(string folderName)
	{
		try
		{
			string text = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			if (!ValidateUnnamedPersonaKeysForImport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				return;
			}
			ShoutUtils.ImportUnnamedPersonaFromDir(text);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportKnowledgeData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			int num = 0;
			int num2 = 0;
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text))
							{
								hashSet2.Add(text);
							}
						}
					}
					string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet2.Count <= 0 && File.Exists(path))
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text2 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text2))
								{
									hashSet2.Add(text2);
								}
							}
						}
					}
					num2 = hashSet2.Count;
					foreach (string item in hashSet2)
					{
						if (hashSet.Contains(item))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
				num2 = 0;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：" + (string.IsNullOrWhiteSpace(detailMessage) ? "找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json" : detailMessage)));
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(detailMessage))
					{
						InformationManager.DisplayMessage(new InformationMessage(detailMessage));
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false, out var detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：" + (string.IsNullOrWhiteSpace(detailMessage) ? "找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json" : detailMessage)));
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(detailMessage))
					{
						InformationManager.DisplayMessage(new InformationMessage(detailMessage));
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
				}
			};
			if (num > 0)
			{
				ShowDuplicateImportInquiry("检测到重复 - Knowledge", "导入的 Knowledge 规则与当前游戏存在重复 RuleId。\n重复：" + num + " / 总计：" + num2 + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ExportVoiceMappingData(string folderName)
	{
		try
		{
			string playerExportsRootPath = GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = ResolveExportFolderName(folderName);
			string text = Path.Combine(playerExportsRootPath, path);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "voice_mapping");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			string path2 = Path.Combine(text2, "VoiceMapping.json");
			string text3 = VoiceMapper.ExportMappingJson();
			if (string.IsNullOrWhiteSpace(text3))
			{
				text3 = "{}";
			}
			File.WriteAllText(path2, text3, Encoding.UTF8);
			VoiceMapper.SetPreferredExportFolder(text);
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ImportVoiceMappingData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string text = null;
			string text2 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
			string text3 = Path.Combine(importDir, "VoiceMapping.json");
			if (File.Exists(text2))
			{
				text = text2;
			}
			else if (File.Exists(text3))
			{
				text = text3;
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 voice_mapping/VoiceMapping.json。"));
				return;
			}
			string json = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(json))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：VoiceMapping.json 为空。"));
				return;
			}
			bool flag = false;
			try
			{
				flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				bool flag2 = VoiceMapper.ImportMappingFromFile(text);
				if (flag2)
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			Action onSkipDuplicates = delegate
			{
				bool flag2 = VoiceMapper.ImportMappingFromFile(text, overwriteExisting: false);
				if (flag2)
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成（已合并）：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			if (flag)
			{
				ShowDuplicateImportInquiry("检测到重复 - VoiceMapping", "当前已存在声音映射。\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private void ImportAllData(string folderName)
	{
		try
		{
			string importDir = ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			Dictionary<string, NpcPersonaProfile> pbNew = null;
			Dictionary<string, List<DialogueDay>> dhNew = null;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> debtNew = null;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			int num10 = 0;
			int num11 = 0;
			int num12 = 0;
			int eventWorldDupCount = 0;
			int eventWorldTotalCount = 0;
			int eventKingdomDupCount = 0;
			int eventKingdomTotalCount = 0;
			int eventRecordDupCount = 0;
			int eventRecordTotalCount = 0;
			string vmJson = null;
			string vmPath = null;
			EventImportPayload eventPayload = null;
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					string text2 = TryParseHeroIdFromNpcFileName(text);
					if (!string.IsNullOrEmpty(text2))
					{
						NpcPersonaProfile npcPersonaProfile = ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							pbNew[text2] = npcPersonaProfile;
						}
					}
				}
				num2 = pbNew.Count;
				if (_npcPersonaProfiles != null)
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _npcPersonaProfiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			string path2 = Path.Combine(importDir, "dialogue_history");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, List<DialogueDay>>();
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = TryParseHeroIdFromNpcFileName(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						List<DialogueDay> list = ReadJson<List<DialogueDay>>(text3);
						if (list != null)
						{
							dhNew[text4] = list;
						}
					}
				}
				num4 = dhNew.Count;
				if (_dialogueHistory != null)
				{
					foreach (string key2 in dhNew.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && _dialogueHistory.ContainsKey(key2))
						{
							num3++;
						}
					}
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> existDebt = ((rs != null) ? (rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>()) : null);
			string path3 = Path.Combine(importDir, "debt");
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = TryParseHeroIdFromNpcFileName(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
			try
			{
				List<string> source = new List<string>
				{
					Path.Combine(importDir, "unnamed_persona"),
					importDir
				};
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string item in source.Distinct(StringComparer.OrdinalIgnoreCase))
				{
					if (!Directory.Exists(item))
					{
						continue;
					}
					string[] array4 = null;
					try
					{
						array4 = Directory.GetFiles(item, "*.json");
					}
					catch
					{
						array4 = null;
					}
					if (array4 == null)
					{
						continue;
					}
					string[] array5 = array4;
					foreach (string path4 in array5)
					{
						string text7 = null;
						try
						{
							text7 = (ReadJson<UnnamedPersonaSingleJson>(path4)?.Key ?? "").Trim().ToLower();
						}
						catch
						{
							text7 = null;
						}
						if (string.IsNullOrWhiteSpace(text7))
						{
							string text8 = Path.GetFileNameWithoutExtension(path4) ?? "";
							int num13 = text8.LastIndexOf("__", StringComparison.Ordinal);
							if (num13 > 0)
							{
								text8 = text8.Substring(0, num13);
							}
							text7 = (text8 ?? "").Trim().ToLower();
						}
						if (!string.IsNullOrEmpty(text7) && hashSet.Add(text7))
						{
							num8++;
							if (ShoutUtils.HasUnnamedPersonaKey(text7))
							{
								num7++;
							}
						}
					}
				}
			}
			catch
			{
				num7 = 0;
				num8 = 0;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet2 = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text9 = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text9))
							{
								hashSet3.Add(text9);
							}
						}
					}
					string path5 = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet3.Count <= 0 && File.Exists(path5))
					{
						string value = File.ReadAllText(path5, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text10 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text10))
								{
									hashSet3.Add(text10);
								}
							}
						}
					}
					num10 = hashSet3.Count;
					foreach (string item2 in hashSet3)
					{
						if (hashSet2.Contains(item2))
						{
							num9++;
						}
					}
				}
			}
			catch
			{
				num9 = 0;
				num10 = 0;
			}
			try
			{
				string path6 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
				if (!File.Exists(path6))
				{
					path6 = Path.Combine(importDir, "VoiceMapping.json");
				}
				if (File.Exists(path6))
				{
					string text11 = File.ReadAllText(path6, Encoding.UTF8);
					if (!string.IsNullOrWhiteSpace(text11))
					{
						vmPath = path6;
						vmJson = text11;
						num12 = 1;
						bool flag = false;
						try
						{
							flag = VoiceMapper.GetTotalVoiceCount() > 0 || !string.IsNullOrWhiteSpace(VoiceMapper.GetFallbackVoice());
						}
						catch
						{
							flag = false;
						}
						if (flag)
						{
							num11 = 1;
						}
					}
				}
			}
			catch
			{
				num11 = 0;
				num12 = 0;
				vmJson = null;
			}
			try
			{
				if (TryLoadEventDataFromImportDir(importDir, out eventPayload, out var _))
				{
					if (eventPayload.HasWorldSummaryFile)
					{
						eventWorldTotalCount = 1;
						if (!string.IsNullOrWhiteSpace(_eventWorldOpeningSummary))
						{
							eventWorldDupCount = 1;
						}
					}
					if (eventPayload.HasKingdomSummariesFile)
					{
						eventKingdomTotalCount = eventPayload.KingdomSummaries.Count;
						if (_eventKingdomOpeningSummaries != null)
						{
							foreach (string key4 in eventPayload.KingdomSummaries.Keys)
							{
								if (!string.IsNullOrWhiteSpace(key4) && _eventKingdomOpeningSummaries.ContainsKey(key4))
								{
									eventKingdomDupCount++;
								}
							}
						}
					}
					if (eventPayload.HasEventRecordsFile)
					{
						eventRecordTotalCount = eventPayload.EventRecords.Count;
						HashSet<string> hashSet4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						if (_eventRecordEntries != null)
						{
							foreach (EventRecordEntry eventRecordEntry in _eventRecordEntries)
							{
								string text12 = (eventRecordEntry?.EventId ?? "").Trim();
								if (!string.IsNullOrWhiteSpace(text12))
								{
									hashSet4.Add(text12);
								}
							}
						}
						foreach (EventRecordEntry eventRecordEntry2 in eventPayload.EventRecords)
						{
							string text13 = (eventRecordEntry2?.EventId ?? "").Trim();
							if (!string.IsNullOrWhiteSpace(text13) && hashSet4.Contains(text13))
							{
								eventRecordDupCount++;
							}
						}
					}
				}
			}
			catch
			{
				eventWorldDupCount = 0;
				eventWorldTotalCount = 0;
				eventKingdomDupCount = 0;
				eventKingdomTotalCount = 0;
				eventRecordDupCount = 0;
				eventRecordTotalCount = 0;
				eventPayload = null;
			}
			bool flag2 = num + num3 + num5 + num7 + num9 + num11 + eventWorldDupCount + eventKingdomDupCount + eventRecordDupCount > 0;
			Action action = delegate
			{
				if (pbNew != null)
				{
					if (_npcPersonaProfiles == null)
					{
						_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
					}
					foreach (KeyValuePair<string, NpcPersonaProfile> item3 in pbNew)
					{
						if (!string.IsNullOrEmpty(item3.Key) && item3.Value != null)
						{
							_npcPersonaProfiles.Remove(item3.Key);
							_npcPersonaProfiles[item3.Key] = item3.Value;
						}
					}
				}
				if (dhNew != null)
				{
					if (_dialogueHistory == null)
					{
						_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
					}
					foreach (KeyValuePair<string, List<DialogueDay>> item4 in dhNew)
					{
						if (!string.IsNullOrEmpty(item4.Key) && item4.Value != null)
						{
							_dialogueHistory.Remove(item4.Key);
							_dialogueHistory[item4.Key] = item4.Value;
						}
					}
				}
				if (debtNew != null && rs != null)
				{
					Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
					foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item5 in debtNew)
					{
						if (!string.IsNullOrEmpty(item5.Key) && item5.Value != null)
						{
							dictionary.Remove(item5.Key);
							dictionary[item5.Key] = item5.Value;
						}
					}
					rs.ImportDebtEntries(dictionary);
				}
				bool flag3 = true;
				if (!string.IsNullOrWhiteSpace(vmJson))
				{
					flag3 = ((!string.IsNullOrWhiteSpace(vmPath) && File.Exists(vmPath)) ? VoiceMapper.ImportMappingFromFile(vmPath) : VoiceMapper.ImportMappingJson(vmJson));
					if (flag3)
					{
						_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
					}
				}
				if (!flag3)
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：VoiceMapping 导入失败，已跳过。"));
				}
				if (eventPayload != null)
				{
					ApplyImportedEventData(eventPayload, overwriteExisting: true);
				}
				ShoutUtils.ImportUnnamedPersonaFromDir(importDir);
				_unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false) ?? "";
				if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var knowledgeImportMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导入失败，已跳过。原因：" + (string.IsNullOrWhiteSpace(knowledgeImportMessage) ? "未知错误。" : knowledgeImportMessage)));
				}
				else if (!string.IsNullOrWhiteSpace(knowledgeImportMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage(knowledgeImportMessage));
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				if (pbNew != null)
				{
					if (_npcPersonaProfiles == null)
					{
						_npcPersonaProfiles = new Dictionary<string, NpcPersonaProfile>();
					}
					foreach (KeyValuePair<string, NpcPersonaProfile> item6 in pbNew)
					{
						if (!string.IsNullOrEmpty(item6.Key) && item6.Value != null && !_npcPersonaProfiles.ContainsKey(item6.Key))
						{
							_npcPersonaProfiles[item6.Key] = item6.Value;
						}
					}
				}
				if (dhNew != null)
				{
					if (_dialogueHistory == null)
					{
						_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
					}
					foreach (KeyValuePair<string, List<DialogueDay>> item7 in dhNew)
					{
						if (!string.IsNullOrEmpty(item7.Key) && item7.Value != null && !_dialogueHistory.ContainsKey(item7.Key))
						{
							_dialogueHistory[item7.Key] = item7.Value;
						}
					}
				}
				if (debtNew != null && rs != null)
				{
					Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = existDebt ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
					foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item8 in debtNew)
					{
						if (!string.IsNullOrEmpty(item8.Key) && item8.Value != null && !dictionary.ContainsKey(item8.Key))
						{
							dictionary[item8.Key] = item8.Value;
						}
					}
					rs.ImportDebtEntries(dictionary);
				}
				bool flag3 = true;
				if (!string.IsNullOrWhiteSpace(vmJson))
				{
					flag3 = ((!string.IsNullOrWhiteSpace(vmPath) && File.Exists(vmPath)) ? VoiceMapper.ImportMappingFromFile(vmPath, overwriteExisting: false) : VoiceMapper.ImportMappingJson(vmJson, overwriteExisting: false));
					if (flag3)
					{
						_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
					}
				}
				if (!flag3)
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：VoiceMapping 导入失败，已跳过。"));
				}
				if (eventPayload != null)
				{
					ApplyImportedEventData(eventPayload, overwriteExisting: false);
				}
				ShoutUtils.ImportUnnamedPersonaFromDir(importDir, overwriteExisting: false);
				_unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false) ?? "";
				if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false, out var knowledgeImportMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导入失败，已跳过。原因：" + (string.IsNullOrWhiteSpace(knowledgeImportMessage) ? "未知错误。" : knowledgeImportMessage)));
				}
				else if (!string.IsNullOrWhiteSpace(knowledgeImportMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage(knowledgeImportMessage));
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (flag2)
			{
				string text14 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n对话历史：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n未命名NPC：" + num7 + "/" + num8 + "\nKnowledge：" + num9 + "/" + num10 + "\n声音映射：" + num11 + "/" + num12 + "\n世界开局概要：" + eventWorldDupCount + "/" + eventWorldTotalCount + "\n王国开局概要：" + eventKingdomDupCount + "/" + eventKingdomTotalCount + "\n事件记录：" + eventRecordDupCount + "/" + eventRecordTotalCount + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - 全部导入", text14, action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	private bool ImportKnowledgeFromDir(string importDir)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var _);
	}

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting, out var _);
	}

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			bool result = false;
			string text = Path.Combine(importDir, "knowledge", "AIConfig.json");
			if (File.Exists(text))
			{
				string moduleRootPath = GetModuleRootPath();
				string text2 = Path.Combine(moduleRootPath, "ModuleData", "AIConfig.json");
				string text3 = Path.Combine(moduleRootPath, "AIConfig.json");
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text2));
					if (overwriteExisting || !File.Exists(text2))
					{
						File.Copy(text, text2, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					if (overwriteExisting && File.Exists(text3))
					{
						File.Copy(text, text3, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					AIConfigHandler.ReloadConfig();
				}
				catch
				{
				}
				result = true;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile != null)
					{
						if (TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile, overwriteExisting, out var importedCount, out var failedCount, out var firstFailedRuleId, out var firstFailedReason))
						{
							result = true;
							if (failedCount > 0)
							{
								detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount + " 条，失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : "；原因：" + firstFailedReason);
							}
						}
						else
						{
							detailMessage = (failedCount > 0) ? ("知识规则全部导入失败。失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : "；原因：" + firstFailedReason)) : "知识规则写入失败。";
						}
					}
					else
					{
						string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
						if (File.Exists(path))
						{
							string json2 = File.ReadAllText(path);
							KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(json2);
							if (TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile2, overwriteExisting, out var importedCount2, out var failedCount2, out var firstFailedRuleId2, out var firstFailedReason2))
							{
								result = true;
								if (failedCount2 > 0)
								{
									detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount2 + " 条，失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : "；原因：" + firstFailedReason2);
								}
							}
							else
							{
								detailMessage = (failedCount2 > 0) ? ("知识规则全部导入失败。失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : "；原因：" + firstFailedReason2)) : "知识规则写入失败。";
							}
						}
						else if (!File.Exists(text))
						{
							detailMessage = "找不到 knowledge\\AIConfig.json、knowledge\\rules\\*.json 或 knowledge\\KnowledgeRules.json。";
						}
					}
				}
				else
				{
					detailMessage = "KnowledgeLibraryBehavior 未初始化。";
				}
			}
			catch
			{
				detailMessage = "知识导入过程发生异常。";
			}
			return result;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			return false;
		}
	}

	private static bool TryImportKnowledgeFileWithFallback(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile, bool overwriteExisting, out int importedCount, out int failedCount, out string firstFailedRuleId, out string firstFailedReason)
	{
		importedCount = 0;
		failedCount = 0;
		firstFailedRuleId = "";
		firstFailedReason = "";
		try
		{
			if (kb == null || knowledgeFile?.Rules == null)
			{
				return false;
			}
			string json = JsonConvert.SerializeObject(knowledgeFile, Formatting.None);
			if (kb.ImportRulesJson(json, overwriteExisting))
			{
				importedCount = knowledgeFile.Rules.Count((KnowledgeLibraryBehavior.LoreRule r) => r != null && !string.IsNullOrWhiteSpace(r.Id));
				return importedCount > 0;
			}
			foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
			{
				if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
				{
					continue;
				}
				string json2 = JsonConvert.SerializeObject(rule, Formatting.None);
				if (kb.ImportSingleRuleJson(json2, overwriteExisting))
				{
					importedCount++;
				}
				else
				{
					failedCount++;
					if (string.IsNullOrWhiteSpace(firstFailedRuleId))
					{
						firstFailedRuleId = (rule.Id ?? "").Trim();
						firstFailedReason = BuildKnowledgeRuleImportFailureMessage(kb, rule, overwriteExisting);
					}
				}
			}
			return importedCount > 0;
		}
		catch
		{
			importedCount = 0;
			failedCount = 0;
			firstFailedRuleId = "";
			firstFailedReason = "";
			return false;
		}
	}

	private static string BuildKnowledgeRuleImportFailureMessage(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting)
	{
		try
		{
			string text = (rule?.Id ?? "").Trim();
			if (rule == null)
			{
				return "规则为空。";
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return "RuleId 为空。";
			}
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text2 = (rule.RagShortTexts[i] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && text2.Length > KnowledgeLibraryBehavior.RagShortTextMaxLength)
					{
						return "RAG专用短句超过 " + KnowledgeLibraryBehavior.RagShortTextMaxLength + " 字符限制。";
					}
				}
			}
			if (!ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting, out var error))
			{
				return error;
			}
			if (TryFindDuplicateKnowledgeVariantCondition(rule, out var firstIndex, out var secondIndex))
			{
				return "规则内部存在重复条件的提示词：第 " + (firstIndex + 1) + " 条与第 " + (secondIndex + 1) + " 条条件完全相同。";
			}
			return "规则写入失败，可能是规则结构未通过校验。";
		}
		catch (Exception ex)
		{
			return "规则校验异常：" + ex.Message;
		}
	}

	private static bool TryFindDuplicateKnowledgeVariantCondition(KnowledgeLibraryBehavior.LoreRule rule, out int firstIndex, out int secondIndex)
	{
		firstIndex = -1;
		secondIndex = -1;
		try
		{
			if (rule?.Variants == null || rule.Variants.Count <= 1)
			{
				return false;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = 0; i < rule.Variants.Count; i++)
			{
				KnowledgeLibraryBehavior.LoreVariant loreVariant = rule.Variants[i];
				if (loreVariant == null)
				{
					continue;
				}
				string text = BuildKnowledgeWhenSignatureForImport(loreVariant.When);
				if (dictionary.TryGetValue(text, out var value))
				{
					firstIndex = value;
					secondIndex = i;
					return true;
				}
				dictionary[text] = i;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string BuildKnowledgeWhenSignatureForImport(KnowledgeLibraryBehavior.LoreWhen when)
	{
		try
		{
			string text = string.Join("|", NormalizeWhenStringListForImport(when?.HeroIds));
			string text2 = string.Join("|", NormalizeWhenStringListForImport(when?.Cultures));
			string text3 = string.Join("|", NormalizeWhenStringListForImport(when?.KingdomIds));
			string text4 = string.Join("|", NormalizeWhenStringListForImport(when?.SettlementIds));
			string text5 = string.Join("|", NormalizeWhenStringListForImport(when?.Roles));
			string text6 = (when?.IsFemale).HasValue ? (when.IsFemale.Value ? "female" : "male") : "any";
			string text7 = (when?.IsClanLeader).HasValue ? (when.IsClanLeader.Value ? "leader" : "not_leader") : "any";
			string text8 = string.Join("|", NormalizeWhenSkillMinForImport(when?.SkillMin).Select((KeyValuePair<string, int> kv) => kv.Key + ":" + kv.Value));
			if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3) && string.IsNullOrEmpty(text4) && string.IsNullOrEmpty(text5) && text6 == "any" && text7 == "any" && string.IsNullOrEmpty(text8))
			{
				return "__generic__";
			}
			return $"hero={text};culture={text2};kingdom={text3};settlement={text4};role={text5};gender={text6};clan={text7};skill={text8}";
		}
		catch
		{
			return "__generic__";
		}
	}

	private static List<string> NormalizeWhenStringListForImport(List<string> list)
	{
		List<string> list2 = new List<string>();
		try
		{
			if (list != null)
			{
				foreach (string item in list)
				{
					string text = (item ?? "").Trim();
					if (!string.IsNullOrEmpty(text) && !list2.Any((string x) => string.Equals(x, text, StringComparison.OrdinalIgnoreCase)))
					{
						list2.Add(text);
					}
				}
			}
			list2.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		return list2;
	}

	private static List<KeyValuePair<string, int>> NormalizeWhenSkillMinForImport(Dictionary<string, int> skillMin)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		try
		{
			if (skillMin != null)
			{
				foreach (KeyValuePair<string, int> item in skillMin)
				{
					string text = (item.Key ?? "").Trim();
					if (!string.IsNullOrEmpty(text) && item.Value >= 0)
					{
						list.Add(new KeyValuePair<string, int>(text, item.Value));
					}
				}
			}
			list = list.OrderBy((KeyValuePair<string, int> x) => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
		}
		return list;
	}

	private static string FindNpcJsonByHeroId(string dir, string heroId)
	{
		try
		{
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text = (heroId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			string result = null;
			DateTime dateTime = DateTime.MinValue;
			string[] files = Directory.GetFiles(dir, "*.json");
			string[] array = files;
			foreach (string text2 in array)
			{
				string text3 = TryParseHeroIdFromNpcFileName(text2);
				if (!(text3 != text))
				{
					DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text2);
					if (lastWriteTimeUtc > dateTime)
					{
						result = text2;
						dateTime = lastWriteTimeUtc;
					}
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static string BuildNpcDataFileName(string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			id = "unknown";
		}
		string text = "";
		try
		{
			text = Hero.FindFirst((Hero x) => x != null && x.StringId == id)?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "NPC";
		}
		string text2 = id + "__" + text;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text2 = text2.Replace(oldChar, '_');
		}
		return text2 + ".json";
	}

	private static string TryParseHeroIdFromNpcFileName(string filePath)
	{
		try
		{
			string text = Path.GetFileNameWithoutExtension(filePath) ?? "";
			int num = text.IndexOf("__", StringComparison.Ordinal);
			if (num <= 0)
			{
				return null;
			}
			string text2 = text.Substring(0, num);
			text2 = (text2 ?? "").Trim();
			return string.IsNullOrEmpty(text2) ? null : text2;
		}
		catch
		{
			return null;
		}
	}

	private static string GetPlayerExportsRootPath()
	{
		string moduleRootPath = GetModuleRootPath();
		return Path.Combine(moduleRootPath, "PlayerExports");
	}

	private static string GetModuleRootPath()
	{
		try
		{
			string location = typeof(SubModule).Assembly.Location;
			string text = (string.IsNullOrEmpty(location) ? "" : Path.GetDirectoryName(Path.GetFullPath(location)));
			DirectoryInfo directoryInfo = (string.IsNullOrEmpty(text) ? null : new DirectoryInfo(text));
			while (directoryInfo != null && directoryInfo.Exists)
			{
				if (File.Exists(Path.Combine(directoryInfo.FullName, "SubModule.xml")))
				{
					return directoryInfo.FullName;
				}
				directoryInfo = directoryInfo.Parent;
			}
		}
		catch
		{
		}
		try
		{
			return Path.GetFullPath(Directory.GetCurrentDirectory());
		}
		catch
		{
			return "";
		}
	}

	private static string SanitizeFolderName(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		return text.Trim();
	}

	private static void WriteJson(string path, object obj)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
		string contents = JsonConvert.SerializeObject(obj, Formatting.Indented);
		File.WriteAllText(path, contents, Encoding.UTF8);
	}

	private static T ReadJson<T>(string path) where T : class
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string value = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(value);
		}
		catch
		{
			return null;
		}
	}

	private static string FindLatestExportFolder(string root)
	{
		try
		{
			if (!Directory.Exists(root))
			{
				return null;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(root);
			return (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).FirstOrDefault()?.FullName;
		}
		catch
		{
			return null;
		}
	}

	private static void ClearJsonFiles(string dir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
			{
				return;
			}
			string[] files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
			foreach (string path in files)
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private string ResolveExportFolderName(string folderName)
	{
		string text = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text))
		{
			text = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		}
		return text;
	}

	private string ResolveImportFolderPath(string folderName)
	{
		string text = (folderName ?? "").Trim();
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				if (Path.IsPathRooted(text))
				{
					string fullPath = Path.GetFullPath(text);
					if (Directory.Exists(fullPath))
					{
						return fullPath;
					}
				}
			}
			catch
			{
			}
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		string text2 = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text2))
		{
			return FindLatestExportFolder(playerExportsRootPath);
		}
		return Path.Combine(playerExportsRootPath, text2);
	}

	private bool IsPersonaReadyForFixedLayer(Hero hero, out bool inFlight)
	{
		inFlight = false;
		if (hero == null)
		{
			return true;
		}
		string stringId = hero.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return true;
		}
		lock (_npcPersonaAutoGenLock)
		{
			if (_npcPersonaAutoGenInFlight.Contains(stringId))
			{
				inFlight = true;
				return false;
			}
		}
		return true;
	}

	private string BuildDirectFixedLayerCacheKey(Hero hero, string cultureId, string basePrompt, string guardrail, string characterDescription, string personaContext, bool personaReady)
	{
		if (!personaReady)
		{
			return null;
		}
		string text = hero?.StringId ?? "_nohero_";
		using SHA256 sHA = SHA256.Create();
		string s = text + "|" + (cultureId ?? "") + "|" + (basePrompt ?? "") + "|" + (guardrail ?? "") + "|" + (characterDescription ?? "") + "|" + (personaContext ?? "");
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
		StringBuilder stringBuilder = new StringBuilder("direct_", 39);
		for (int i = 0; i < 16; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
