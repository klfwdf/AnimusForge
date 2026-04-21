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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public class MyBehavior : CampaignBehaviorBase
{
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

	private bool _pendingWeeklyReportManualRetryResult;

	private bool _pendingWeeklyReportManualRetrySucceeded;

	private string _pendingWeeklyReportManualRetryMessage = "";

	private WeeklyReportRetryContext _pendingWeeklyReportManualRetryContext;

	private long _weeklyReportUiResumeAfterUtcTicks;

	private bool _weeklyReportReopenAfterApiConfig;

	private long _weeklyReportReopenAfterApiConfigUtcTicks;

	private bool _devForcedKingdomRebellionInProgress;

	private bool _pendingDevForcedKingdomRebellionReady;

	private PendingDevForcedKingdomRebellionContext _pendingDevForcedKingdomRebellionContext;

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

	private const int RebelKingdomNamingTimeoutMs = 60000;

	private const string SceneHistorySessionMarkerPrefix = "[AF_SCENE_SESSION:";

	private const int MarriageCandidateMinAgeForPrompt = 18;

	private const int MarriageCandidateMaxAgeForPrompt = 55;

	private const int MarriageCandidateMaxAgeGapForPrompt = 25;

	public static MyBehavior Instance { get; private set; }

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
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener((object)this, (Action<IMission>)OnMissionStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener((object)this, (Action<float>)OnCampaignTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener((object)this, (Action<MapEvent>)OnMapEventEnded);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener((object)this, (Action<PartyBase, Hero>)OnHeroPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener((object)this, (Action<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>)OnHeroPrisonerReleased);
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener((object)this, (Action<Town>)OnDailyTickTown);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, (Action)OnDailyTick);
		CampaignEvents.ArmyCreated.AddNonSerializedListener((object)this, (Action<Army>)OnArmyCreated);
		CampaignEvents.ArmyGathered.AddNonSerializedListener((object)this, (Action<Army, IMapPoint>)OnArmyGathered);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener((object)this, (Action<Army, ArmyDispersionReason, bool>)OnArmyDispersed);
		CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener((object)this, (Action<MobileParty>)OnPartyJoinedArmy);
		CampaignEvents.OnPartyLeftArmyEvent.AddNonSerializedListener((object)this, (Action<MobileParty, Army>)OnPartyLeftArmy);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener((object)this, (Action<SiegeEvent>)OnSiegeEventStarted);
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener((object)this, (Action<SiegeEvent>)OnSiegeEventEnded);
		CampaignEvents.OnMobilePartyJoinedToSiegeEventEvent.AddNonSerializedListener((object)this, (Action<MobileParty>)OnMobilePartyJoinedSiege);
		CampaignEvents.OnMobilePartyLeftSiegeEventEvent.AddNonSerializedListener((object)this, (Action<MobileParty>)OnMobilePartyLeftSiege);
		CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener((object)this, (Action<Settlement, MobileParty, bool, BattleTypes>)OnSiegeCompleted);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener((object)this, (Action<MobileParty>)OnDailyTickParty);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener((object)this, (Action<Hero, Hero, bool>)OnBeforeHeroesMarried);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener((object)this, (Action<Clan, Kingdom, Kingdom, ChangeKingdomActionDetail, bool>)OnClanChangedKingdom);
		CampaignEvents.OnClanDefectedEvent.AddNonSerializedListener((object)this, (Action<Clan, Kingdom, Kingdom>)OnClanDefected);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener((object)this, (Action<KingdomDecision, DecisionOutcome, bool>)OnKingdomDecisionConcluded);
		CampaignEvents.RulingClanChanged.AddNonSerializedListener((object)this, (Action<Kingdom, Clan>)OnRulingClanChanged);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener((object)this, (Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementDetail>)OnSettlementOwnerChanged);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener((object)this, (Action<Hero, Hero, KillCharacterActionDetail, bool>)OnHeroKilled);
		CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener((object)this, (Action<Hero, List<Hero>, int>)OnGivenBirth);
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener((object)this, (Action<Hero, Hero>)OnClanLeaderChanged);
		CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener((object)this, (Action<MobileParty, Settlement, SiegeAftermath, Clan, Dictionary<MobileParty, float>>)OnSiegeAftermathApplied);
		CampaignEvents.VillageBeingRaided.AddNonSerializedListener((object)this, (Action<Village>)OnVillageBeingRaided);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener((object)this, (Action<BattleSideEnum, RaidEventComponent>)OnRaidCompleted);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener((object)this, (Action<Kingdom>)OnKingdomDestroyed);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener((object)this, (Action<Clan>)OnClanDestroyed);
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				return;
			}
			MapEventSide val = (((int)mapEvent.PlayerSide == 1) ? mapEvent.DefenderSide : mapEvent.AttackerSide);
			foreach (MapEventParty item in (List<MapEventParty>)(object)val.Parties)
			{
				PartyBase party = item.Party;
				Hero val2 = ((party != null) ? party.LeaderHero : null);
				if (val2 != null && val2 != Hero.MainHero && val2.IsLord)
				{
					_recentlyDefeatedByPlayer.Add(((MBObjectBase)val2).StringId);
					Logger.Log("BattleStatus", $"原版战斗结束：玩家击败了 {val2.Name}");
					AppendExternalDialogueHistory(val2, null, null, $"你在一场战斗中被 {Hero.MainHero.Name} 击败了。");
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
			object obj = ((capturer != null) ? capturer.LeaderHero : null);
			if (obj == null)
			{
				if (capturer == null)
				{
					obj = null;
				}
				else
				{
					MobileParty mobileParty = capturer.MobileParty;
					obj = ((mobileParty != null) ? mobileParty.LeaderHero : null);
				}
			}
			Hero val = (Hero)obj;
			Settlement settlement = ResolveSettlementForPartyBase(capturer);
			string locationLabelForPartyBase = GetLocationLabelForPartyBase(capturer);
			string text = BuildPrisonerTakenStableKey(val, prisoner);
			if (ShouldTrackNpcActionHero(val))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("prisoner_taken_captor", val);
				npcActionFacts.LocationText = locationLabelForPartyBase;
				ApplySettlementFacts(npcActionFacts, settlement, null, null, locationLabelForPartyBase);
				ApplyTargetFacts(npcActionFacts, prisoner);
				if (((prisoner != null) ? prisoner.MapFaction : null) != null)
				{
					AddRelatedFactionFacts(npcActionFacts, prisoner.MapFaction);
				}
				string text2 = "你俘虏了" + GetHeroDisplayName(prisoner) + "。";
				RecordNpcMajorAction(val, text2, text + ":captor", npcActionFacts);
				RecordNpcRecentAction(val, text2, text + ":captor", dedupeAcrossWindow: false, npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(prisoner))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("prisoner_taken_prisoner", prisoner);
				npcActionFacts2.LocationText = locationLabelForPartyBase;
				ApplySettlementFacts(npcActionFacts2, settlement, null, null, locationLabelForPartyBase);
				if (val != null)
				{
					ApplyTargetFacts(npcActionFacts2, val);
				}
				else
				{
					object obj2;
					if (capturer == null)
					{
						obj2 = null;
					}
					else
					{
						MobileParty mobileParty2 = capturer.MobileParty;
						obj2 = ((mobileParty2 != null) ? mobileParty2.MapFaction : null);
					}
					if (obj2 != null)
					{
						AddRelatedFactionFacts(npcActionFacts2, capturer.MobileParty.MapFaction);
					}
				}
				string text3 = ((val != null) ? ("你被" + GetHeroDisplayName(val) + "俘虏了。") : "你被敌方俘虏了。");
				RecordNpcMajorAction(prisoner, text3, text + ":prisoner", npcActionFacts2);
				RecordNpcRecentAction(prisoner, text3, text + ":prisoner", dedupeAcrossWindow: false, npcActionFacts2);
			}
			if (prisoner == null || prisoner == Hero.MainHero || !prisoner.IsLord)
			{
				return;
			}
			if (((capturer != null) ? capturer.LeaderHero : null) != Hero.MainHero)
			{
				object obj3;
				if (capturer == null)
				{
					obj3 = null;
				}
				else
				{
					MobileParty mobileParty3 = capturer.MobileParty;
					obj3 = ((mobileParty3 != null) ? mobileParty3.ActualClan : null);
				}
				if (obj3 != Clan.PlayerClan)
				{
					return;
				}
			}
			Logger.Log("BattleStatus", $"NPC {prisoner.Name} 被玩家俘虏");
			AppendExternalDialogueHistory(prisoner, null, null, $"你被 {Hero.MainHero.Name} 俘虏了，你现在是他的囚犯。");
		}
		catch (Exception ex)
		{
			Logger.Log("BattleStatus", "[ERROR] OnHeroPrisonerTaken: " + ex.Message);
		}
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Expected I4, but got Unknown
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			object obj = ((party != null) ? party.LeaderHero : null);
			if (obj == null)
			{
				if (party == null)
				{
					obj = null;
				}
				else
				{
					MobileParty mobileParty = party.MobileParty;
					obj = ((mobileParty != null) ? mobileParty.LeaderHero : null);
				}
			}
			Hero val = (Hero)obj;
			Settlement settlement = ResolveSettlementForPartyBase(party);
			string locationLabelForPartyBase = GetLocationLabelForPartyBase(party);
			string text = BuildPrisonerReleasedStableKey(val, prisoner, detail);
			string endCaptivityDetailLabel = GetEndCaptivityDetailLabel(detail);
			if (ShouldTrackNpcActionHero(prisoner))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("prisoner_released_prisoner", prisoner);
				npcActionFacts.LocationText = locationLabelForPartyBase;
				ApplySettlementFacts(npcActionFacts, settlement, null, null, locationLabelForPartyBase);
				if (val != null)
				{
					ApplyTargetFacts(npcActionFacts, val);
				}
				else
				{
					AddRelatedFactionFacts(npcActionFacts, capturerFaction);
				}
				string text2 = (string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? "你结束了囚禁状态。" : ("你" + endCaptivityDetailLabel + "并恢复了自由。"));
				RecordNpcMajorAction(prisoner, text2, text + ":prisoner", npcActionFacts);
				RecordNpcRecentAction(prisoner, text2, text + ":prisoner", dedupeAcrossWindow: false, npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(val))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("prisoner_released_captor", val);
				npcActionFacts2.LocationText = locationLabelForPartyBase;
				ApplySettlementFacts(npcActionFacts2, settlement, null, null, locationLabelForPartyBase);
				ApplyTargetFacts(npcActionFacts2, prisoner);
				string text3 = (string.IsNullOrWhiteSpace(endCaptivityDetailLabel) ? ("你失去了对" + GetHeroDisplayName(prisoner) + "的控制。") : (GetHeroDisplayName(prisoner) + endCaptivityDetailLabel + "，不再是你的囚犯。"));
				RecordNpcMajorAction(val, text3, text + ":captor", npcActionFacts2);
				RecordNpcRecentAction(val, text3, text + ":captor", dedupeAcrossWindow: false, npcActionFacts2);
			}
			if (prisoner == null || prisoner == Hero.MainHero || !prisoner.IsLord)
			{
				return;
			}
			Hero mainHero = Hero.MainHero;
			if (capturerFaction != ((mainHero != null) ? mainHero.MapFaction : null) && ((party != null) ? party.LeaderHero : null) != Hero.MainHero)
			{
				object obj2;
				if (party == null)
				{
					obj2 = null;
				}
				else
				{
					MobileParty mobileParty2 = party.MobileParty;
					obj2 = ((mobileParty2 != null) ? mobileParty2.ActualClan : null);
				}
				if (obj2 != Clan.PlayerClan)
				{
					return;
				}
			}
			_recentlyReleasedPrisoners.Add(((MBObjectBase)prisoner).StringId);
			if (1 == 0)
			{
			}
			string text4 = (int)detail switch
			{
				0 => "通过支付赎金", 
				4 => "被主动释放", 
				1 => "因和平协议", 
				3 => "成功逃脱", 
				_ => "", 
			};
			if (1 == 0)
			{
			}
			string text5 = text4;
			Logger.Log("BattleStatus", $"NPC {prisoner.Name} {text5}获得自由 (detail={detail})");
			AppendExternalDialogueHistory(prisoner, null, null, $"你{text5}从 {Hero.MainHero.Name} 的囚禁中获得了自由。");
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
			object obj = ((army != null) ? army.ArmyOwner : null);
			if (obj == null)
			{
				if (army == null)
				{
					obj = null;
				}
				else
				{
					MobileParty leaderParty = army.LeaderParty;
					obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
				}
			}
			Hero val = (Hero)obj;
			if (ShouldTrackNpcActionHero(val))
			{
				string armyDisplayName = GetArmyDisplayName(army);
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_create", val);
				Settlement settlement = ResolveGatheringPointSettlement((IMapPoint)(object)((army != null) ? army.LeaderParty : null), (army != null) ? army.LeaderParty : null);
				npcActionFacts.LocationText = GetNearestSettlementNameForParty((army != null) ? army.LeaderParty : null);
				ApplySettlementFacts(npcActionFacts, settlement, null, null, npcActionFacts.LocationText);
				RecordNpcMajorAction(val, "你组建并统领了" + armyDisplayName + "。", "army_create:" + armyDisplayName, npcActionFacts);
				RecordNpcRecentAction(val, "你组建并统领了" + armyDisplayName + "。", "army_create:" + armyDisplayName, dedupeAcrossWindow: false, npcActionFacts);
			}
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
			object obj = ((army != null) ? army.ArmyOwner : null);
			if (obj == null)
			{
				if (army == null)
				{
					obj = null;
				}
				else
				{
					MobileParty leaderParty = army.LeaderParty;
					obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
				}
			}
			Hero val = (Hero)obj;
			if (ShouldTrackNpcActionHero(val))
			{
				Settlement settlement = ResolveGatheringPointSettlement(gatheringPoint, (army != null) ? army.LeaderParty : null);
				string text = ResolveGatheringPointLabel(gatheringPoint, (army != null) ? army.LeaderParty : null);
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_gather", val);
				npcActionFacts.LocationText = text;
				ApplySettlementFacts(npcActionFacts, settlement, null, null, text);
				RecordNpcRecentAction(val, "你率领" + GetArmyDisplayName(army) + "在" + text + "集结。", "army_gather:" + GetArmyDisplayName(army) + ":" + text, dedupeAcrossWindow: false, npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnArmyGathered: " + ex.Message);
		}
	}

	private void OnArmyDispersed(Army army, ArmyDispersionReason reason, bool isNoNotification)
	{
		try
		{
			object obj = ((army != null) ? army.ArmyOwner : null);
			if (obj == null)
			{
				if (army == null)
				{
					obj = null;
				}
				else
				{
					MobileParty leaderParty = army.LeaderParty;
					obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
				}
			}
			Hero val = (Hero)obj;
			if (ShouldTrackNpcActionHero(val))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_disperse", val);
				npcActionFacts.LocationText = GetNearestSettlementNameForParty((army != null) ? army.LeaderParty : null);
				RecordNpcRecentAction(val, "你统领的" + GetArmyDisplayName(army) + "已解散。", "army_disperse:" + GetArmyDisplayName(army), dedupeAcrossWindow: false, npcActionFacts);
			}
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
			Hero val = ((party != null) ? party.LeaderHero : null);
			Army val2 = ((party != null) ? party.Army : null);
			if (ShouldTrackNpcActionHero(val) && val2 != null && party != val2.LeaderParty && val != val2.ArmyOwner)
			{
				MobileParty leaderParty = val2.LeaderParty;
				if (val != ((leaderParty != null) ? leaderParty.LeaderHero : null))
				{
					string armyDisplayName = GetArmyDisplayName(val2);
					NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_join", val);
					npcActionFacts.LocationText = GetNearestSettlementNameForParty(party);
					RecordNpcMajorAction(val, "你加入了" + armyDisplayName + "。", "army_join:" + armyDisplayName, npcActionFacts);
					RecordNpcRecentAction(val, "你加入了" + armyDisplayName + "。", "army_join:" + armyDisplayName, dedupeAcrossWindow: false, npcActionFacts);
				}
			}
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
			Hero val = ((party != null) ? party.LeaderHero : null);
			if (ShouldTrackNpcActionHero(val))
			{
				string armyDisplayName = GetArmyDisplayName(army);
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("army_leave", val);
				npcActionFacts.LocationText = GetNearestSettlementNameForParty(party);
				RecordNpcRecentAction(val, "你离开了" + armyDisplayName + "。", "army_leave:" + armyDisplayName, dedupeAcrossWindow: false, npcActionFacts);
			}
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
			Settlement val = ResolveSiegeSettlement(siegeEvent);
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, (BattleSideEnum)1))
			{
				string text = BuildSiegeStartNarrative(val, isAttacker: true, siegeEvent);
				string text2 = ((val == null) ? null : ((object)val.Name)?.ToString()) ?? "某处要塞";
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_start_attack", item);
				ApplySettlementFacts(npcActionFacts, val);
				npcActionFacts.TargetKingdomId = GetKingdomId((val != null) ? val.MapFaction : null);
				RecordNpcMajorAction(item, text, "siege_start:" + text2, npcActionFacts);
				RecordNpcRecentAction(item, text, "siege_start:" + text2, dedupeAcrossWindow: false, npcActionFacts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, (BattleSideEnum)0))
			{
				string text3 = BuildSiegeStartNarrative(val, isAttacker: false, siegeEvent);
				string text4 = ((val == null) ? null : ((object)val.Name)?.ToString()) ?? "某处要塞";
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("siege_start_defend", item2);
				ApplySettlementFacts(npcActionFacts2, val);
				object faction;
				if (siegeEvent == null)
				{
					faction = null;
				}
				else
				{
					BesiegerCamp besiegerCamp = siegeEvent.BesiegerCamp;
					if (besiegerCamp == null)
					{
						faction = null;
					}
					else
					{
						MobileParty leaderParty = besiegerCamp.LeaderParty;
						faction = ((leaderParty != null) ? leaderParty.MapFaction : null);
					}
				}
				npcActionFacts2.TargetKingdomId = GetKingdomId((IFaction)faction);
				RecordNpcMajorAction(item2, text3, "siege_defend:" + text4, npcActionFacts2);
				RecordNpcRecentAction(item2, text3, "siege_defend:" + text4, dedupeAcrossWindow: false, npcActionFacts2);
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
			Settlement val = ResolveSiegeSettlement(siegeEvent);
			string text = ((val == null) ? null : ((object)val.Name)?.ToString()) ?? "某处要塞";
			foreach (Hero item in GetHeroesFromSiegeEventSide(siegeEvent, (BattleSideEnum)1))
			{
				NpcActionFacts facts = CreateNpcActionFacts("siege_end_attack", item);
				ApplySettlementFacts(facts, val);
				RecordNpcRecentAction(item, "你结束了对" + text + "的围城。", "siege_end:" + text, dedupeAcrossWindow: false, facts);
			}
			foreach (Hero item2 in GetHeroesFromSiegeEventSide(siegeEvent, (BattleSideEnum)0))
			{
				NpcActionFacts facts2 = CreateNpcActionFacts("siege_end_defend", item2);
				ApplySettlementFacts(facts2, val);
				RecordNpcRecentAction(item2, text + "的守城战已经结束。", "siege_end_defend:" + text, dedupeAcrossWindow: false, facts2);
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
			Hero val = ((party != null) ? party.LeaderHero : null);
			if (ShouldTrackNpcActionHero(val))
			{
				Settlement val2 = party.BesiegedSettlement ?? ResolveSiegeSettlement(party.SiegeEvent);
				string text = ((val2 == null) ? null : ((object)val2.Name)?.ToString()) ?? "某处要塞";
				NpcActionFacts facts = CreateNpcActionFacts("siege_join", val);
				ApplySettlementFacts(facts, val2);
				RecordNpcRecentAction(val, "你加入了对" + text + "的围城。", "siege_join:" + text, dedupeAcrossWindow: false, facts);
			}
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
			Hero val = ((party != null) ? party.LeaderHero : null);
			if (ShouldTrackNpcActionHero(val))
			{
				Settlement val2 = party.BesiegedSettlement ?? ResolveSiegeSettlement(party.SiegeEvent);
				string text = ((val2 == null) ? null : ((object)val2.Name)?.ToString()) ?? "某处要塞";
				bool flag = val2 != null && ((party != null) ? party.MapFaction : null) != null && val2.MapFaction == party.MapFaction;
				string text2 = (flag ? (text + "结清了战利品和战俘") : (text + "处理完了围城中产生的战利品以及战俘。"));
				NpcActionFacts facts = CreateNpcActionFacts("siege_leave", val);
				ApplySettlementFacts(facts, val2);
				RecordNpcRecentAction(val, text2, "siege_leave:" + text + ":" + flag, dedupeAcrossWindow: false, facts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnMobilePartyLeftSiege: " + ex.Message);
		}
	}

	private void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, BattleTypes battleType)
	{
		try
		{
			Hero val = ((party != null) ? party.LeaderHero : null);
			if (ShouldTrackNpcActionHero(val))
			{
				string text = ((settlement == null) ? null : ((object)settlement.Name)?.ToString()) ?? "某处要塞";
				string text2 = (siegeSuccess ? ("你在" + text + "的围城中获胜。") : ("你在" + text + "的围城中失利。"));
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("siege_complete", val);
				ApplySettlementFacts(npcActionFacts, settlement);
				npcActionFacts.Won = siegeSuccess;
				RecordNpcMajorAction(val, text2, "siege_complete:" + text + ":" + siegeSuccess, npcActionFacts);
				RecordNpcRecentAction(val, text2, "siege_complete:" + text + ":" + siegeSuccess, dedupeAcrossWindow: false, npcActionFacts);
			}
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
			Hero val = ((party != null) ? party.LeaderHero : null);
			if (!ShouldTrackNpcActionHero(val))
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
				NpcActionFacts facts = CreateNpcActionFacts("daily_behavior", val);
				Army army = party.Army;
				MobileParty val2 = ((army != null) ? army.LeaderParty : null) ?? party;
				Settlement settlement = ((val2 != null) ? val2.BesiegedSettlement : null) ?? ResolveSiegeSettlement((val2 != null) ? val2.SiegeEvent : null) ?? ((val2 != null) ? val2.TargetSettlement : null) ?? party.TargetSettlement ?? party.CurrentSettlement ?? party.LastVisitedSettlement;
				ApplySettlementFacts(facts, settlement, null, null, GetNearestSettlementNameForParty(party));
				object obj;
				if (val2 == null)
				{
					obj = null;
				}
				else
				{
					MobileParty targetParty = val2.TargetParty;
					obj = ((targetParty != null) ? targetParty.LeaderHero : null);
				}
				if (obj != null)
				{
					ApplyTargetFacts(facts, val2.TargetParty.LeaderHero);
				}
				RecordNpcRecentAction(val, text, text2, dedupeAcrossWindow: true, facts);
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
			string stableKey = BuildOrderedHeroPairStableKey("marriage", hero1, hero2);
			if (ShouldTrackNpcActionHero(hero1))
			{
				NpcActionFacts facts = CreateNpcActionFacts("marriage", hero1);
				ApplyTargetFacts(facts, hero2);
				RecordNpcMajorAction(hero1, "你与" + GetHeroDisplayName(hero2) + "缔结了婚姻。", stableKey, facts);
				RecordNpcRecentAction(hero1, "你与" + GetHeroDisplayName(hero2) + "缔结了婚姻。", stableKey, dedupeAcrossWindow: false, facts);
			}
			if (ShouldTrackNpcActionHero(hero2))
			{
				NpcActionFacts facts2 = CreateNpcActionFacts("marriage", hero2);
				ApplyTargetFacts(facts2, hero1);
				RecordNpcMajorAction(hero2, "你与" + GetHeroDisplayName(hero1) + "缔结了婚姻。", stableKey, facts2);
				RecordNpcRecentAction(hero2, "你与" + GetHeroDisplayName(hero1) + "缔结了婚姻。", stableKey, dedupeAcrossWindow: false, facts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnBeforeHeroesMarried: " + ex.Message);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomActionDetail detail, bool showNotification)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Invalid comparison between Unknown and I4
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Invalid comparison between Unknown and I4
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
				RecordNpcRecentAction(item, text2, text, dedupeAcrossWindow: false, npcActionFacts);
			}
			if ((int)detail == 4 && clan != null && oldKingdom != null)
			{
				string text3 = BuildClanFortificationSummary(clan);
				string text4 = text2;
				if (!string.IsNullOrWhiteSpace(text3))
				{
					text4 = text4 + " " + text3;
				}
				Settlement settlement = ((IEnumerable<Settlement>)clan.Settlements)?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));
				RecordEventSourceMaterial("kingdom_rebellion", "家族叛乱 - " + GetClanDisplayName(clan), text4, text + ":material", GetKingdomId(oldKingdom), GetSettlementId(settlement), includeInWorld: true, includeInKingdom: true);
			}
			else if ((int)detail == 7 && clan != null && newKingdom != null)
			{
				Settlement settlement2 = ((IEnumerable<Settlement>)clan.Settlements)?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));
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
			string stableKey = BuildClanChangedKingdomStableKey(clan, oldKingdom, newKingdom, (ChangeKingdomActionDetail)2);
			string text = "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + GetKingdomDisplayName(oldKingdom, "原王国") + "，转投" + GetKingdomDisplayName(newKingdom, "新王国") + "。";
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_defected", item);
				npcActionFacts.TargetKingdomId = GetKingdomId(newKingdom);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(oldKingdom));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(newKingdom));
				RecordNpcMajorAction(item, text, stableKey, npcActionFacts);
				RecordNpcRecentAction(item, text, stableKey, dedupeAcrossWindow: false, npcActionFacts);
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
			if (decision != null)
			{
				string text = BuildKingdomDecisionStableKey(decision);
				string text2 = BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer: true);
				Clan proposerClan = decision.ProposerClan;
				Hero val = ((proposerClan != null) ? proposerClan.Leader : null);
				if (ShouldTrackNpcActionHero(val))
				{
					NpcActionFacts npcActionFacts = CreateNpcActionFacts("kingdom_decision_concluded", val);
					AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(decision.ProposerClan));
					AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
					ApplyKingdomDecisionSpecificFacts(npcActionFacts, decision, chosenOutcome);
					RecordNpcMajorAction(val, text2, text + ":proposer", npcActionFacts);
					RecordNpcRecentAction(val, text2, text + ":proposer", dedupeAcrossWindow: false, npcActionFacts);
				}
				Clan obj = decision.DetermineChooser();
				Hero val2 = ((obj != null) ? obj.Leader : null);
				if (ShouldTrackNpcActionHero(val2) && !string.Equals(GetHeroId(val2), GetHeroId(val), StringComparison.OrdinalIgnoreCase))
				{
					NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("kingdom_decision_concluded", val2);
					AddUniqueId(npcActionFacts2.RelatedClanIds, GetClanId(decision.ProposerClan));
					AddUniqueId(npcActionFacts2.RelatedKingdomIds, GetKingdomId(decision.Kingdom));
					ApplyKingdomDecisionSpecificFacts(npcActionFacts2, decision, chosenOutcome);
					string text3 = BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer: false);
					RecordNpcMajorAction(val2, text3, text + ":chooser", npcActionFacts2);
					RecordNpcRecentAction(val2, text3, text + ":chooser", dedupeAcrossWindow: false, npcActionFacts2);
				}
				string text4 = BuildKingdomDecisionSupporterSummary(decision, chosenOutcome);
				if (!string.IsNullOrWhiteSpace(text4))
				{
					string kingdomId = GetKingdomId(decision.Kingdom);
					RecordEventSourceMaterial("kingdom_decision_support", "决议支持明细 - " + GetKingdomDisplayName(decision.Kingdom, "该王国"), text4, text + ":supporters", kingdomId, "", includeInWorld: false, includeInKingdom: true);
				}
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
			string stableKey = "ruling_clan_changed:" + GetKingdomId(kingdom) + ":" + GetClanId(newRulingClan);
			string text = "你所在的" + GetClanDisplayName(newRulingClan) + "家族已成为" + GetKingdomDisplayName(kingdom, "该王国") + "的执政家族。";
			foreach (Hero item in GetTrackedLordsForClan(newRulingClan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("ruling_clan_changed", item);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(newRulingClan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(kingdom));
				RecordNpcMajorAction(item, text, stableKey, npcActionFacts);
				RecordNpcRecentAction(item, text, stableKey, dedupeAcrossWindow: false, npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnRulingClanChanged: " + ex.Message);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementDetail detail)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = BuildSettlementOwnerChangedStableKey(settlement, newOwner, oldOwner, detail);
			string settlementDisplayName = GetSettlementDisplayName(settlement);
			if (ShouldTrackNpcActionHero(newOwner))
			{
				NpcActionFacts facts = CreateNpcActionFacts("settlement_owner_changed_gain", newOwner);
				ApplyTargetFacts(facts, oldOwner);
				ApplySettlementFacts(facts, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(newOwner, "你获得了" + settlementDisplayName + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":gain", facts);
				RecordNpcRecentAction(newOwner, "你获得了" + settlementDisplayName + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":gain", dedupeAcrossWindow: false, facts);
			}
			if (ShouldTrackNpcActionHero(oldOwner))
			{
				NpcActionFacts facts2 = CreateNpcActionFacts("settlement_owner_changed_loss", oldOwner);
				ApplyTargetFacts(facts2, newOwner);
				ApplySettlementFacts(facts2, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(oldOwner, "你失去了" + settlementDisplayName + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":loss", facts2);
				RecordNpcRecentAction(oldOwner, "你失去了" + settlementDisplayName + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":loss", dedupeAcrossWindow: false, facts2);
			}
			if (ShouldTrackNpcActionHero(capturerHero) && !string.Equals(GetHeroId(capturerHero), GetHeroId(newOwner), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts facts3 = CreateNpcActionFacts("settlement_owner_changed_capture", capturerHero);
				ApplyTargetFacts(facts3, newOwner);
				ApplySettlementFacts(facts3, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(capturerHero, "你促成了" + settlementDisplayName + "的易主，新的所有者是" + GetHeroDisplayName(newOwner) + "。", text + ":capture", facts3);
				RecordNpcRecentAction(capturerHero, "你促成了" + settlementDisplayName + "的易主，新的所有者是" + GetHeroDisplayName(newOwner) + "。", text + ":capture", dedupeAcrossWindow: false, facts3);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSettlementOwnerChanged: " + ex.Message);
		}
	}

	private unsafe void OnHeroKilled(Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (victim == null)
			{
				return;
			}
			string text = "hero_killed:" + GetHeroId(victim) + ":" + ((object)(*(KillCharacterActionDetail*)(&detail))/*cast due to .constrained prefix*/).ToString();
			if (ShouldTrackNpcActionHero(killer))
			{
				NpcActionFacts facts = CreateNpcActionFacts("hero_killed", killer);
				ApplyTargetFacts(facts, victim);
				RecordNpcMajorAction(killer, "你" + GetHeroKilledVerb(detail) + GetHeroDisplayName(victim) + "。", text + ":killer", facts);
				RecordNpcRecentAction(killer, "你" + GetHeroKilledVerb(detail) + GetHeroDisplayName(victim) + "。", text + ":killer", dedupeAcrossWindow: false, facts);
			}
			Clan clan = victim.Clan;
			Hero val = ((clan != null) ? clan.Leader : null);
			if (ShouldTrackNpcActionHero(val) && !string.Equals(GetHeroId(val), GetHeroId(victim), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_member_killed", val);
				ApplyTargetFacts(npcActionFacts, victim);
				if (killer != null)
				{
					AddUniqueId(npcActionFacts.RelatedHeroIds, GetHeroId(killer));
					AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(killer.Clan));
					AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(killer.MapFaction));
				}
				string text2 = "你所在的" + GetClanDisplayName(victim.Clan) + "家族失去了" + GetHeroDisplayName(victim) + "。";
				RecordNpcMajorAction(val, text2, text + ":clan", npcActionFacts);
				RecordNpcRecentAction(val, text2, text + ":clan", dedupeAcrossWindow: false, npcActionFacts);
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
			if (ShouldTrackNpcActionHero(mother))
			{
				Hero val = aliveChildren?.FirstOrDefault((Hero x) => x != null);
				string stableKey = "birth:" + GetHeroId(mother) + ":" + GetHeroId(val);
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("birth", mother);
				if (val != null)
				{
					AddUniqueId(npcActionFacts.RelatedHeroIds, GetHeroId(val));
				}
				string text = "你诞下一名子嗣";
				if (val != null)
				{
					text = text + "，孩子是" + GetHeroDisplayName(val);
				}
				if (stillbornCount > 0)
				{
					text = text + "。此次分娩还伴随" + stillbornCount + "名夭折婴儿";
				}
				text += "。";
				RecordNpcMajorAction(mother, text, stableKey, npcActionFacts);
				RecordNpcRecentAction(mother, text, stableKey, dedupeAcrossWindow: false, npcActionFacts);
			}
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
			Clan val = ((newLeader != null) ? newLeader.Clan : null) ?? ((oldLeader != null) ? oldLeader.Clan : null);
			if (val == null)
			{
				return;
			}
			string stableKey = "clan_leader_changed:" + GetClanId(val) + ":" + GetHeroId(newLeader);
			foreach (Hero item in GetTrackedLordsForClan(val))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_leader_changed", item);
				ApplyTargetFacts(npcActionFacts, newLeader);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(val));
				string text = (string.Equals(GetHeroId(item), GetHeroId(newLeader), StringComparison.OrdinalIgnoreCase) ? ("你已成为" + GetClanDisplayName(val) + "家族的新族长。") : (GetHeroDisplayName(newLeader) + "已成为" + GetClanDisplayName(val) + "家族的新族长。"));
				RecordNpcMajorAction(item, text, stableKey, npcActionFacts);
				RecordNpcRecentAction(item, text, stableKey, dedupeAcrossWindow: false, npcActionFacts);
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
			if (currentGameDayIndexSafe > 0 && currentGameDayIndexSafe % 7 == 0)
			{
				TryProcessWeeklyKingdomRebellions(currentGameDayIndexSafe / 7);
			}
			if (_weeklyReportGenerationInProgress)
			{
				return;
			}
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null && settings.AutoGenerateWeeklyReports && currentGameDayIndexSafe > 0 && currentGameDayIndexSafe % 7 == 0)
			{
				int num = currentGameDayIndexSafe / 7;
				if (num > 0 && _lastAutoGeneratedWeeklyReportWeek < num)
				{
					int startDay = Math.Max(0, (num - 1) * 7);
					int endDay = currentGameDayIndexSafe - 1;
					List<WeeklyEventMaterialPreviewGroup> groups = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups(startDay, endDay));
					_weeklyReportGenerationInProgress = true;
					GenerateAutoWeeklyReportsAsync(groups, num, startDay, endDay);
				}
			}
		}
		catch (Exception ex)
		{
			_weeklyReportGenerationInProgress = false;
			Logger.Log("EventWeeklyReport", "[ERROR] OnDailyTick auto-generate failed: " + ex.Message);
		}
	}

	private async Task GenerateAutoWeeklyReportsAsync(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay)
	{
		try
		{
			WeeklyReportGenerationResult weeklyReportGenerationResult = await GenerateWeeklyReportsAsyncInternal(groups, weekIndex, startDay, endDay, "第" + weekIndex + "周自动周报", openViewerWhenDone: false, queueBlockingPopupOnFatalFailure: true, isAutoGeneration: true);
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
				string text = ((object)devEditableKingdom.Name)?.ToString() ?? ((MBObjectBase)devEditableKingdom).StringId ?? "王国";
				UpsertWeekZeroOpeningSummaryEvent("kingdom", ((MBObjectBase)devEditableKingdom).StringId ?? "", text + "第0天开局概要", kingdomOpeningSummary, text + " 开局概要", "kingdom_opening_summary");
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
		return "[BOOTSTRAP][WEEK0_SHORT=" + (llmGenerated ? "LLM" : "FALLBACK") + "][SOURCE=" + (sourceHash ?? "").Trim() + "]";
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
			string text2 = BuildFallbackWeeklyReportShortSummary(fallbackSource);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
		}
		return text.Trim();
	}

	private static string BuildWeekZeroShortSummarySystemPrompt(string eventKind, string kingdomId, string title)
	{
		string text = (string.Equals((eventKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "世界第0周短周报" : (ResolveKingdomDisplay(kingdomId) + "第0周短周报"));
		return "你要把开局概况压缩成一条适合 NPC 常驻读取的短周报。只输出 [SHORT] 段，不要输出 [TITLE]、[REPORT]、[TAGS]。内容必须是 20-140 个汉字，简洁、客观、像第0周的局势提要，不要使用列表，不要解释规则，不要添加编造事实。\n当前对象：" + text + "\n标题参考：" + (title ?? "").Trim();
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
				return (value != 0) ? (value + 1) : 0;
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
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(from x in _weekZeroShortSummaryPendingQueue
			where x != null && string.Equals((x.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)
			select x.KingdomId);
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int num = 0; num < kingdomIdsByPlayerProximity.Count; num++)
		{
			string text = (kingdomIdsByPlayerProximity[num] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = num;
			}
		}
		_weekZeroShortSummaryPendingQueue.Sort(delegate(WeekZeroShortSummaryRequest a, WeekZeroShortSummaryRequest b)
		{
			int weekZeroShortSummaryQueuePriority = GetWeekZeroShortSummaryQueuePriority(a, dictionary);
			int weekZeroShortSummaryQueuePriority2 = GetWeekZeroShortSummaryQueuePriority(b, dictionary);
			int num2 = weekZeroShortSummaryQueuePriority.CompareTo(weekZeroShortSummaryQueuePriority2);
			return (num2 != 0) ? num2 : string.Compare((a?.Title ?? "").Trim(), (b?.Title ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
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
			ProcessWeekZeroShortSummaryQueueAsync();
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
					break;
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
			ApiCallResult apiCallResult = await CallUniversalApiDetailed(BuildWeekZeroShortSummarySystemPrompt(request.EventKind, request.KingdomId, request.Title), BuildWeekZeroShortSummaryUserPrompt(request.Summary));
			if (apiCallResult.Success)
			{
				string text = NormalizeWeekZeroShortSummaryResponse(apiCallResult.Content, request.Summary);
				if (!string.IsNullOrWhiteSpace(text))
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
				Logger.Log("EventWeeklyReport", "[Week0Short][WARN] 短周报解析为空 " + request.EventId + "，尝试 " + i + "/3");
			}
			else
			{
				Logger.Log("EventWeeklyReport", "[Week0Short][WARN] 短周报生成失败 " + request.EventId + "，尝试 " + i + "/3 -> " + (apiCallResult.ErrorMessage ?? "未知错误"));
			}
			if (i < 3)
			{
				int num = Math.Max(1200, GetWeeklyReportRequestIntervalMs());
				if (apiCallResult?.IsRateLimit ?? false)
				{
					num = Math.Max(num, GetWeeklyReportRequestIntervalMs());
				}
				if (apiCallResult?.RetryAfterSeconds.HasValue ?? false)
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
					Exception ex2 = ex;
					Logger.Log("EventWeeklyReport", "[Week0Short][ERROR] " + ex2.Message);
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
					ProcessWeekZeroShortSummaryQueueAsync();
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
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || HasWeekZeroLlmShortSummary(entry, text) || GetCurrentGameDayIndexSafe() < 7)
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
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (_eventRecordEntries == null)
			{
				_eventRecordEntries = new List<EventRecordEntry>();
			}
			string text2 = "weekly_report:" + (eventKind ?? "").Trim().ToLowerInvariant() + ":0:" + (kingdomId ?? "").Trim();
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
			string sourceHash = ComputeWeekZeroShortSummarySourceHash(text);
			if (!HasWeekZeroLlmShortSummary(eventRecordEntry, sourceHash) || !string.Equals((eventRecordEntry.Summary ?? "").Trim(), text, StringComparison.Ordinal))
			{
				eventRecordEntry.ShortSummary = BuildFallbackWeeklyReportShortSummary(text);
				eventRecordEntry.PromptText = BuildWeekZeroPromptText(sourceHash, llmGenerated: false);
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
			TryQueueWeekZeroShortSummaryGeneration(eventRecordEntry, sourceHash);
		}
	}

	private unsafe void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Invalid comparison between Unknown and I4
		try
		{
			string settlementDisplayName = GetSettlementDisplayName(settlement);
			string siegeAftermathLabel = GetSiegeAftermathLabel(aftermathType);
			string kingdomId = GetKingdomId((attackerParty != null) ? attackerParty.MapFaction : null);
			string text = GetHeroDisplayName((attackerParty != null) ? attackerParty.LeaderHero : null) + "在攻取" + settlementDisplayName + "后选择了" + siegeAftermathLabel + "。";
			string clanDisplayName = GetClanDisplayName(previousSettlementOwner);
			string kingdomDisplayName = GetKingdomDisplayName((previousSettlementOwner != null) ? previousSettlementOwner.Kingdom : null, "原所属王国");
			if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text = text + " 该地此前由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			RecordEventSourceMaterial("siege_aftermath", "围城后处理 - " + settlementDisplayName, text, "siege_aftermath:" + GetSettlementId(settlement) + ":" + ((object)(*(SiegeAftermath*)(&aftermathType))/*cast due to .constrained prefix*/).ToString() + ":" + GetHeroId((attackerParty != null) ? attackerParty.LeaderHero : null), kingdomId, GetSettlementId(settlement), settlement != null && settlement.IsTown && (int)aftermathType != 2, includeInKingdom: true);
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
			Settlement val = ((village != null) ? ((SettlementComponent)village).Settlement : null);
			string settlementDisplayName = GetSettlementDisplayName(val);
			string text = settlementDisplayName + "村庄正在遭到掠夺。";
			string clanDisplayName = GetClanDisplayName((val != null) ? val.OwnerClan : null);
			IFaction obj = ((val != null) ? val.MapFaction : null);
			string kingdomDisplayName = GetKingdomDisplayName((Kingdom)(object)((obj is Kingdom) ? obj : null), "所属王国");
			if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text = text + " 该地由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			RecordEventSourceMaterial("village_raid_started", "掠夺开始 - " + settlementDisplayName + "村庄", text, "village_raid_started:" + GetSettlementId(val), GetKingdomId((val != null) ? val.MapFaction : null), GetSettlementId(val), includeInWorld: false, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnVillageBeingRaided: " + ex.Message);
		}
	}

	private unsafe void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Settlement val = ((raidEvent != null) ? raidEvent.MapEventSettlement : null);
			string settlementDisplayName = GetSettlementDisplayName(val);
			string text = settlementDisplayName + "村庄的掠夺结果是：" + GetRaidOutcomeLabel(winnerSide) + "。";
			object obj;
			if (raidEvent == null)
			{
				obj = null;
			}
			else
			{
				MapEventSide attackerSide = raidEvent.AttackerSide;
				if (attackerSide == null)
				{
					obj = null;
				}
				else
				{
					PartyBase leaderParty = attackerSide.LeaderParty;
					obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
				}
			}
			Hero val2 = (Hero)obj;
			if (val2 != null)
			{
				text = text + " 发起掠夺的一方领袖是" + GetHeroDisplayName(val2) + "。";
			}
			RecordEventSourceMaterial("raid_completed", "掠夺结果 - " + settlementDisplayName + "村庄", text, "raid_completed:" + GetSettlementId(val) + ":" + ((object)(*(BattleSideEnum*)(&winnerSide))/*cast due to .constrained prefix*/).ToString(), GetKingdomId((val != null) ? val.MapFaction : null), GetSettlementId(val), includeInWorld: false, includeInKingdom: true);
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
			string kingdomDisplayName = GetKingdomDisplayName(destroyedKingdom);
			string snapshotText = kingdomDisplayName + "已经覆灭。";
			RecordEventSourceMaterial("kingdom_destroyed", "王国覆灭 - " + kingdomDisplayName, snapshotText, "kingdom_destroyed:" + GetKingdomId(destroyedKingdom), GetKingdomId(destroyedKingdom), "", includeInWorld: true, includeInKingdom: true);
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
			string kingdomId = GetKingdomId((destroyedClan != null) ? destroyedClan.Kingdom : null);
			string snapshotText = clanDisplayName + "家族已经覆灭。";
			int num;
			if (destroyedClan != null)
			{
				Kingdom kingdom = destroyedClan.Kingdom;
				num = ((destroyedClan == ((kingdom != null) ? kingdom.RulingClan : null)) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool includeInWorld = (byte)num != 0;
			RecordEventSourceMaterial("clan_destroyed", "家族覆灭 - " + clanDisplayName, snapshotText, "clan_destroyed:" + GetClanId(destroyedClan), kingdomId, "", includeInWorld, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnClanDestroyed: " + ex.Message);
		}
	}

	private static bool ShouldTrackNpcActionHero(Hero hero)
	{
		return hero != null && hero != Hero.MainHero && hero.IsLord && !string.IsNullOrWhiteSpace(((MBObjectBase)hero).StringId);
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
			foreach (Hero lord in (List<Hero>)(object)clan.Heroes)
			{
				if (ShouldTrackNpcActionHero(lord) && !list.Any((Hero x) => string.Equals(((MBObjectBase)x).StringId, ((MBObjectBase)lord).StringId, StringComparison.OrdinalIgnoreCase)))
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
		string text = (((hero == null) ? null : ((object)hero.Name)?.ToString()) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "某位领主" : text;
	}

	private static string GetClanDisplayName(Clan clan)
	{
		string text = (((clan == null) ? null : ((object)clan.Name)?.ToString()) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "某个" : text;
	}

	private static string GetKingdomDisplayName(Kingdom kingdom, string fallback = "某个王国")
	{
		string text = (((kingdom == null) ? null : ((object)kingdom.Name)?.ToString()) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}

	private static string GetSettlementDisplayName(Settlement settlement)
	{
		string text = (((settlement == null) ? null : ((object)settlement.Name)?.ToString()) ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "某处定居点";
		}
		if (settlement != null && settlement.IsTown && !text.EndsWith("市", StringComparison.Ordinal))
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
		string settlementTypeLabel = GetSettlementTypeLabel(settlement);
		return string.IsNullOrWhiteSpace(settlementTypeLabel) ? "" : ("（" + settlementTypeLabel + "）");
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

	private unsafe static string BuildClanChangedKingdomStableKey(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomActionDetail detail)
	{
		return "clan_changed_kingdom:" + GetClanId(clan) + ":" + GetKingdomId(oldKingdom) + ":" + GetKingdomId(newKingdom) + ":" + ((object)(*(ChangeKingdomActionDetail*)(&detail))/*cast due to .constrained prefix*/).ToString();
	}

	private static string BuildClanChangedKingdomNarrative(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomActionDetail detail)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		string kingdomDisplayName = GetKingdomDisplayName(oldKingdom, "原王国");
		string kingdomDisplayName2 = GetKingdomDisplayName(newKingdom, "新王国");
		return (int)detail switch
		{
			0 => "你所在的" + GetClanDisplayName(clan) + "家族已作为佣兵加入" + kingdomDisplayName2 + "。", 
			1 => "你所在的" + GetClanDisplayName(clan) + "家族已正式加入" + kingdomDisplayName2 + "。", 
			2 => "你所在的" + GetClanDisplayName(clan) + "家族已背离" + kingdomDisplayName + "，改投" + kingdomDisplayName2 + "。", 
			3 => "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + kingdomDisplayName + "。", 
			4 => "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + kingdomDisplayName + "并发动叛乱。", 
			5 => "你所在的" + GetClanDisplayName(clan) + "家族已结束在" + kingdomDisplayName + "的佣兵服务。", 
			7 => "你所在的" + GetClanDisplayName(clan) + "家族已建立" + kingdomDisplayName2 + "。", 
			8 => "由于" + kingdomDisplayName + "覆灭，你所在的" + GetClanDisplayName(clan) + "家族已脱离原王国。", 
			6 => "你所在的" + GetClanDisplayName(clan) + "家族因家族灭亡而退出原王国体系。", 
			_ => "你所在的" + GetClanDisplayName(clan) + "家族发生了王国归属变更。", 
		};
	}

	private static string BuildKingdomDecisionStableKey(KingdomDecision decision)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		string text = (((object)decision)?.GetType()?.Name ?? "decision").Trim();
		string kingdomId = GetKingdomId((decision != null) ? decision.Kingdom : null);
		string clanId = GetClanId((decision != null) ? decision.ProposerClan : null);
		string text2 = "";
		try
		{
			text2 = ((decision != null) ? ((object)decision.TriggerTime/*cast due to .constrained prefix*/).ToString() : null) ?? "";
		}
		catch
		{
			text2 = "";
		}
		return "kingdom_decision:" + kingdomId + ":" + clanId + ":" + text + ":" + text2;
	}

	private static string BuildKingdomDecisionNarrative(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved, bool forProposer)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		try
		{
			text = ((object)new KingdomDecisionConcludedLogEntry(decision, chosenOutcome, isPlayerInvolved).GetNotificationText())?.ToString() ?? "";
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
				text = ((decision == null) ? null : ((object)decision.GetGeneralTitle())?.ToString()) ?? "";
			}
			catch
			{
				text = "";
			}
		}
		string kingdomDisplayName = GetKingdomDisplayName((decision != null) ? decision.Kingdom : null, "该王国");
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
		DeclareWarDecision val = (DeclareWarDecision)(object)((decision is DeclareWarDecision) ? decision : null);
		if (val != null)
		{
			AddRelatedFactionFacts(facts, val.FactionToDeclareWarOn);
		}
		else
		{
			MakePeaceKingdomDecision val2 = (MakePeaceKingdomDecision)(object)((decision is MakePeaceKingdomDecision) ? decision : null);
			if (val2 != null)
			{
				AddRelatedFactionFacts(facts, val2.FactionToMakePeaceWith);
			}
			else
			{
				SettlementClaimantPreliminaryDecision val3 = (SettlementClaimantPreliminaryDecision)(object)((decision is SettlementClaimantPreliminaryDecision) ? decision : null);
				if (val3 != null)
				{
					ApplySettlementFacts(facts, val3.Settlement);
					List<string> relatedClanIds = facts.RelatedClanIds;
					Settlement settlement = val3.Settlement;
					AddUniqueId(relatedClanIds, GetClanId((settlement != null) ? settlement.OwnerClan : null));
					List<string> relatedKingdomIds = facts.RelatedKingdomIds;
					Settlement settlement2 = val3.Settlement;
					AddUniqueId(relatedKingdomIds, GetKingdomId((settlement2 != null) ? settlement2.MapFaction : null));
				}
			}
		}
		KingSelectionDecisionOutcome val4 = (KingSelectionDecisionOutcome)(object)((chosenOutcome is KingSelectionDecisionOutcome) ? chosenOutcome : null);
		if (val4 != null)
		{
			ApplyTargetFacts(facts, val4.King);
		}
	}

	private unsafe static string BuildSettlementOwnerChangedStableKey(Settlement settlement, Hero newOwner, Hero oldOwner, ChangeOwnerOfSettlementDetail detail)
	{
		return "settlement_owner_changed:" + GetSettlementId(settlement) + ":" + GetHeroId(newOwner) + ":" + GetHeroId(oldOwner) + ":" + ((object)(*(ChangeOwnerOfSettlementDetail*)(&detail))/*cast due to .constrained prefix*/).ToString();
	}

	private static string GetSettlementOwnerChangeDetailLabel(ChangeOwnerOfSettlementDetail detail)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected I4, but got Unknown
		return (detail - 1) switch
		{
			0 => "围城", 
			1 => "易物", 
			2 => "脱离王国", 
			3 => "王国决议", 
			4 => "赠与", 
			5 => "叛乱", 
			6 => "家族覆灭", 
			_ => "常规变更", 
		};
	}

	private static string GetHeroKilledVerb(KillCharacterActionDetail detail)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected I4, but got Unknown
		switch (detail - 1)
		{
		case 0:
			return "谋杀了";
		case 1:
			return "间接导致分娩中失去了";
		case 2:
			return "见证了";
		case 3:
			return "在战场上杀死了";
		case 4:
			return "在战斗中重创并导致死亡的是";
		case 5:
		case 6:
			return "处决了";
		default:
			return "使其死亡：";
		}
	}

	private static string BuildPrisonerTakenStableKey(Hero capturerHero, Hero prisoner)
	{
		return "prisoner_taken:" + GetHeroId(capturerHero) + ":" + GetHeroId(prisoner);
	}

	private unsafe static string BuildPrisonerReleasedStableKey(Hero capturerHero, Hero prisoner, EndCaptivityDetail detail)
	{
		return "prisoner_released:" + GetHeroId(capturerHero) + ":" + GetHeroId(prisoner) + ":" + ((object)(*(EndCaptivityDetail*)(&detail))/*cast due to .constrained prefix*/).ToString();
	}

	private static string GetEndCaptivityDetailLabel(EndCaptivityDetail detail)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected I4, but got Unknown
		return (int)detail switch
		{
			0 => "通过赎金获释", 
			4 => "被主动释放", 
			1 => "因议和获释", 
			3 => "成功逃脱", 
			2 => "在战后获释", 
			6 => "在补偿后获释", 
			5 => "在囚禁中死亡", 
			_ => "脱离囚禁", 
		};
	}

	private unsafe static string GetSiegeAftermathLabel(SiegeAftermath aftermathType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected I4, but got Unknown
		SiegeAftermath val = aftermathType;
		SiegeAftermath val2 = val;
		return (int)val2 switch
		{
			0 => "毁灭", 
			1 => "劫掠", 
			2 => "宽恕", 
			_ => ((object)(*(SiegeAftermath*)(&aftermathType))/*cast due to .constrained prefix*/).ToString(), 
		};
	}

	private unsafe static string GetBattleSideLabel(BattleSideEnum side)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if (1 == 0)
		{
		}
		string result = (((int)side == 0) ? "防守方" : (((int)side != 1) ? ((object)(*(BattleSideEnum*)(&side))/*cast due to .constrained prefix*/).ToString() : "进攻方"));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetRaidOutcomeLabel(BattleSideEnum side)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if (1 == 0)
		{
		}
		string result = (((int)side == 0) ? "掠夺被击退" : (((int)side != 1) ? "掠夺中止" : "掠夺成功"));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string BuildKingdomDecisionSupporterSummary(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (decision == null || chosenOutcome == null)
		{
			return "";
		}
		string text = "";
		try
		{
			text = ((object)chosenOutcome.GetDecisionTitle())?.ToString() ?? "";
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = ((object)chosenOutcome.GetDecisionDescription())?.ToString() ?? "";
			}
			catch
			{
				text = "";
			}
		}
		List<string> list = new List<string>();
		foreach (Supporter item in chosenOutcome.SupporterList ?? new List<Supporter>())
		{
			object obj3;
			if (item == null)
			{
				obj3 = null;
			}
			else
			{
				Clan clan = item.Clan;
				obj3 = ((clan != null) ? clan.Leader : null);
			}
			if (obj3 != null)
			{
				list.Add(GetHeroDisplayName(item.Clan.Leader) + "（" + GetSupportWeightLabel(item.SupportWeight) + "）");
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return GetKingdomDisplayName(decision.Kingdom, "该王国") + "的决议“" + (string.IsNullOrWhiteSpace(text) ? "本次决议结果" : text.Trim()) + "”最终得到这些支持者表态：" + string.Join("；", list) + "。";
	}

	private unsafe static string GetSupportWeightLabel(SupportWeights supportWeight)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		SupportWeights val = supportWeight;
		SupportWeights val2 = val;
		return (int)val2 switch
		{
			2 => "轻度支持", 
			3 => "强力支持", 
			4 => "全力推动", 
			1 => "中立", 
			0 => "选择", 
			_ => ((object)(*(SupportWeights*)(&supportWeight))/*cast due to .constrained prefix*/).ToString(), 
		};
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
		return hero.IsLord && !string.IsNullOrWhiteSpace(((MBObjectBase)hero).StringId);
	}

	private static string GetNpcActionHeroKey(Hero hero)
	{
		return (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim();
	}

	private static int GetCurrentGameDayIndexSafe()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			return Math.Max(0, (int)Math.Floor(((CampaignTime)(ref now)).ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static string GetCurrentGameDateTextSafe()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = ((object)CampaignTime.Now/*cast due to .constrained prefix*/).ToString();
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
		return (((clan != null) ? ((MBObjectBase)clan).StringId : null) ?? "").Trim();
	}

	private static string GetKingdomId(Kingdom kingdom)
	{
		return (((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null) ?? "").Trim();
	}

	private static string GetKingdomId(IFaction faction)
	{
		Kingdom val = (Kingdom)(object)((faction is Kingdom) ? faction : null);
		if (val != null)
		{
			return GetKingdomId(val);
		}
		return "";
	}

	private static string GetHeroId(Hero hero)
	{
		return (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim();
	}

	private static string GetSettlementId(Settlement settlement)
	{
		return (((settlement != null) ? ((MBObjectBase)settlement).StringId : null) ?? "").Trim();
	}

	private static int ClampKingdomStabilityValue(int value)
	{
		return Math.Max(0, Math.Min(100, value));
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
			return 50;
		}
		if (_kingdomStabilityValues == null)
		{
			_kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_kingdomStabilityValues.TryGetValue(text, out var value))
		{
			return ClampKingdomStabilityValue(value);
		}
		return 50;
	}

	private void SetKingdomStabilityValue(Kingdom kingdom, int value)
	{
		string kingdomId = GetKingdomId(kingdom);
		if (!string.IsNullOrWhiteSpace(kingdomId))
		{
			if (_kingdomStabilityValues == null)
			{
				_kingdomStabilityValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			}
			_kingdomStabilityValues[kingdomId] = ClampKingdomStabilityValue(value);
			ApplyKingdomStabilityRelationAdjustmentsForKingdom(kingdom);
		}
	}

	private static int GetKingdomStabilityRelationTargetOffset(int stabilityValue)
	{
		return MBMath.ClampInt((ClampKingdomStabilityValue(stabilityValue) - 50) / 2, -25, 25);
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
		return GetKingdomStabilityTier(value) switch
		{
			KingdomStabilityTier.ExtremelyHigh => "极高", 
			KingdomStabilityTier.High => "高", 
			KingdomStabilityTier.FairlyHigh => "较高", 
			KingdomStabilityTier.Average => "一般", 
			KingdomStabilityTier.Poor => "较差", 
			KingdomStabilityTier.VeryPoor => "很差", 
			_ => "极差", 
		};
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
		if (((clan != null) ? clan.Heroes : null) == null)
		{
			return Enumerable.Empty<Hero>();
		}
		return ((IEnumerable<Hero>)clan.Heroes).Where((Hero x) => x != null && x.IsAlive && !x.IsChild).Distinct();
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
		object obj = ((kingdom != null) ? kingdom.Leader : null);
		if (obj == null)
		{
			if (kingdom == null)
			{
				obj = null;
			}
			else
			{
				Clan rulingClan = kingdom.RulingClan;
				obj = ((rulingClan != null) ? rulingClan.Leader : null);
			}
		}
		Hero val = (Hero)obj;
		int kingdomStabilityRelationTargetOffset = GetKingdomStabilityRelationTargetOffset(GetKingdomStabilityValue(kingdom));
		if (kingdom != null && !kingdom.IsEliminated && val != null && kingdomStabilityRelationTargetOffset != 0 && kingdom.Clans != null)
		{
			foreach (Clan item in (List<Clan>)(object)kingdom.Clans)
			{
				if (!ShouldApplyKingdomStabilityRelationAdjustmentToClan(item, kingdom))
				{
					continue;
				}
				foreach (Hero item2 in GetClanHeroesForKingdomStabilityRelationAdjustment(item))
				{
					string text = BuildKingdomStabilityRelationOffsetKey(kingdomId, val, item2);
					if (!string.IsNullOrWhiteSpace(text))
					{
						dictionary[text] = kingdomStabilityRelationTargetOffset;
					}
				}
			}
		}
		List<string> list = _kingdomStabilityRelationAppliedOffsets.Keys.Where((string x) => !string.IsNullOrWhiteSpace(x) && x.StartsWith(kingdomId + "|", StringComparison.OrdinalIgnoreCase)).ToList();
		string kingdomId2;
		foreach (string item3 in list)
		{
			int value = 0;
			dictionary.TryGetValue(item3, out value);
			if (!TryResolveKingdomStabilityRelationOffsetKey(item3, out kingdomId2, out var sourceHero, out var targetHero))
			{
				_kingdomStabilityRelationAppliedOffsets.Remove(item3);
				continue;
			}
			int num = ApplyKingdomStabilityRelationOffsetToPair(item3, sourceHero, targetHero, value);
			if (num == 0)
			{
				_kingdomStabilityRelationAppliedOffsets.Remove(item3);
			}
			else
			{
				_kingdomStabilityRelationAppliedOffsets[item3] = num;
			}
		}
		foreach (KeyValuePair<string, int> item4 in dictionary)
		{
			if (!list.Contains(item4.Key) && TryResolveKingdomStabilityRelationOffsetKey(item4.Key, out kingdomId2, out var sourceHero2, out var targetHero2))
			{
				int num2 = ApplyKingdomStabilityRelationOffsetToPair(item4.Key, sourceHero2, targetHero2, item4.Value);
				if (num2 != 0)
				{
					_kingdomStabilityRelationAppliedOffsets[item4.Key] = num2;
				}
			}
		}
	}

	private void ApplyKingdomStabilityRelationAdjustments()
	{
		if (Kingdom.All == null || ((List<Kingdom>)(object)Kingdom.All).Count == 0)
		{
			return;
		}
		foreach (Kingdom item in ((IEnumerable<Kingdom>)Kingdom.All).Where((Kingdom x) => x != null))
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
		if (((kingdom != null) ? kingdom.Clans : null) == null)
		{
			return 0;
		}
		return ((IEnumerable<Clan>)kingdom.Clans).Count((Clan x) => x != null && !x.IsEliminated && !x.IsUnderMercenaryService && !x.IsClanTypeMercenary);
	}

	private static void SyncTownRebelliousStateFromCurrentLoyalty(Town town)
	{
		if (((town != null) ? ((SettlementComponent)town).Settlement : null) == null)
		{
			return;
		}
		Campaign current = Campaign.Current;
		int? obj;
		if (current == null)
		{
			obj = null;
		}
		else
		{
			GameModels models = current.Models;
			if (models == null)
			{
				obj = null;
			}
			else
			{
				SettlementLoyaltyModel settlementLoyaltyModel = models.SettlementLoyaltyModel;
				obj = ((settlementLoyaltyModel != null) ? new int?(settlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold) : ((int?)null));
			}
		}
		int num = obj ?? 25;
		bool inRebelliousState = town.InRebelliousState;
		town.InRebelliousState = town.Loyalty <= (float)num;
		if (inRebelliousState != town.InRebelliousState)
		{
			((CampaignEventReceiver)CampaignEventDispatcher.Instance).TownRebelliousStateChanged(town, town.InRebelliousState);
		}
	}

	private void ApplyRulingClanSettlementLoyaltyAdjustmentForLowClanCountKingdom(Town town)
	{
		if (((town != null) ? ((SettlementComponent)town).Settlement : null) == null || !((SettlementComponent)town).Settlement.IsFortification)
		{
			return;
		}
		Clan ownerClan = ((SettlementComponent)town).Settlement.OwnerClan;
		Kingdom val = ((ownerClan != null) ? ownerClan.Kingdom : null);
		if (ownerClan == null || val == null || val.IsEliminated || ownerClan != val.RulingClan)
		{
			return;
		}
		int num = CountActiveKingdomClansForLowClanCountRule(val);
		if (num > 7)
		{
			return;
		}
		int lowClanCountRoyalDomainLoyaltyAdjustment = GetLowClanCountRoyalDomainLoyaltyAdjustment(GetKingdomStabilityValue(val), num);
		if (lowClanCountRoyalDomainLoyaltyAdjustment == 0)
		{
			return;
		}
		Campaign current = Campaign.Current;
		int? obj;
		if (current == null)
		{
			obj = null;
		}
		else
		{
			GameModels models = current.Models;
			if (models == null)
			{
				obj = null;
			}
			else
			{
				SettlementLoyaltyModel settlementLoyaltyModel = models.SettlementLoyaltyModel;
				obj = ((settlementLoyaltyModel != null) ? new int?(settlementLoyaltyModel.MaximumLoyaltyInSettlement) : ((int?)null));
			}
		}
		float num2 = obj ?? 100;
		town.Loyalty = MBMath.ClampFloat(town.Loyalty + (float)lowClanCountRoyalDomainLoyaltyAdjustment, 0f, num2);
		SyncTownRebelliousStateFromCurrentLoyalty(town);
	}

	private static float GetKingdomRebellionWeeklyChance(int stabilityValue)
	{
		return GetKingdomStabilityTier(stabilityValue) switch
		{
			KingdomStabilityTier.Poor => 0.005f, 
			KingdomStabilityTier.VeryPoor => 0.05f, 
			KingdomStabilityTier.ExtremelyPoor => 0.25f, 
			_ => 0f, 
		};
	}

	private static string FormatKingdomRebellionChance(float chance)
	{
		float num = Math.Max(0f, chance) * 100f;
		return num.ToString((num < 1f) ? "0.0##" : "0.##") + "%";
	}

	private static string BuildClanFortificationSummary(Clan clan)
	{
		List<Settlement> list = ((clan == null) ? null : ((IEnumerable<Settlement>)clan.Settlements)?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).ToList()) ?? new List<Settlement>();
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
		Settlement val = ((clan == null) ? null : ((IEnumerable<Settlement>)clan.Settlements)?.FirstOrDefault((Settlement x) => x != null && x.IsTown)) ?? ((clan == null) ? null : ((IEnumerable<Settlement>)clan.Settlements)?.FirstOrDefault((Settlement x) => x != null && x.IsCastle));
		string text = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
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
		Settlement val = ((clan == null) ? null : ((IEnumerable<Settlement>)clan.Settlements)?.FirstOrDefault((Settlement x) => x != null && x.IsTown)) ?? ((clan == null) ? null : ((IEnumerable<Settlement>)clan.Settlements)?.FirstOrDefault((Settlement x) => x != null && x.IsCastle));
		string text2 = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
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
		string heroDisplayName = GetHeroDisplayName((clan != null) ? clan.Leader : null);
		string kingdomDisplayName = GetKingdomDisplayName(oldKingdom, "原王国");
		string text = (string.IsNullOrWhiteSpace(formalName) ? "这一新生政权" : formalName.Trim());
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
		string text = NormalizeRebelPromptSourceText(((object)hero.EncyclopediaText)?.ToString() ?? "", 900);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string heroDisplayName = GetHeroDisplayName(hero);
		string clanDisplayName = GetClanDisplayName(hero.Clan);
		CultureObject culture = hero.Culture;
		string text2 = (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
		string text3 = (string.IsNullOrWhiteSpace(text2) ? "" : (text2 + "文化"));
		return NormalizeRebelPromptSourceText(heroDisplayName + "出身于" + clanDisplayName + "家族，现为" + text3 + "背景的领主。", 300);
	}

	private string BuildKingdomBackgroundForRebelNamingPrompt(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "无";
		}
		string text = NormalizeRebelPromptSourceText(((object)kingdom.EncyclopediaText)?.ToString() ?? "");
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string kingdomOpeningSummary = GetKingdomOpeningSummary(kingdom);
		text = NormalizeRebelPromptSourceText(kingdomOpeningSummary);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return GetKingdomDisplayName(kingdom, "该王国") + "是当前叛乱所脱离的旧政权。";
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
		text2 = text2.Trim().Trim('"', '\'', '“', '”', '‘', '’', '：', ':', '-', '·');
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (clan == null)
		{
			return null;
		}
		Banner banner = clan.Banner;
		return new ClanVisualSnapshot
		{
			Banner = ((banner == null) ? ((Banner)null) : new Banner(banner)),
			Color = clan.Color,
			Color2 = clan.Color2,
			BackgroundColor = ((banner != null) ? banner.GetPrimaryColor() : clan.Color),
			IconColor = ((banner != null) ? banner.GetFirstIconColor() : clan.Color2)
		};
	}

	private static void RestoreClanVisualSnapshot(Clan clan, ClanVisualSnapshot snapshot)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		if (clan != null && snapshot != null)
		{
			clan.Color = snapshot.Color;
			clan.Color2 = snapshot.Color2;
			if (snapshot.Banner != null)
			{
				clan.Banner = new Banner(snapshot.Banner);
			}
			clan.UpdateBannerColor(snapshot.BackgroundColor, snapshot.IconColor);
		}
	}

	private static void PrepareClanVisualForRebelKingdomCreation(Clan clan, ClanVisualSnapshot snapshot)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		if (clan != null && snapshot != null)
		{
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
	}

	private static void MarkClanVisualsDirty(Clan clan)
	{
		if (clan == null)
		{
			return;
		}
		try
		{
			foreach (Settlement item in (List<Settlement>)(object)clan.Settlements)
			{
				if (item != null)
				{
					PartyBase party = item.Party;
					if (party != null)
					{
						party.SetVisualAsDirty();
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (WarPartyComponent item2 in (List<WarPartyComponent>)(object)clan.WarPartyComponents)
			{
				if (item2 != null)
				{
					PartyBase party2 = ((PartyComponent)item2).Party;
					if (party2 != null)
					{
						party2.SetVisualAsDirty();
					}
				}
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
			if (color != 3735928559u && hashSet.Add(color))
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
			foreach (Kingdom item in (List<Kingdom>)(object)Kingdom.All)
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
			foreach (Clan item2 in (List<Clan>)(object)Clan.All)
			{
				if (item2 != null && item2 != founderClan && !item2.IsEliminated && item2.Kingdom == null && item2.Settlements != null && ((IEnumerable<Settlement>)item2.Settlements).Any((Settlement x) => x != null && (x.IsTown || x.IsCastle)))
				{
					Banner banner = item2.Banner;
					hashSet.Add((banner != null) ? banner.GetPrimaryColor() : item2.Color);
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
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Expected O, but got Unknown
		List<uint> bannerPaletteColors = GetBannerPaletteColors();
		uint num = (uint)(((int?)snapshot?.BackgroundColor) ?? (-3357781));
		uint num2 = (uint)(((int?)snapshot?.IconColor) ?? (-1));
		if (bannerPaletteColors.Count == 0)
		{
			return new RebelFactionColorChoice
			{
				BackgroundColor = num,
				IconColor = ((num2 != num) ? num2 : 4289374890u),
				Banner = ((snapshot?.Banner == null) ? ((Banner)null) : new Banner(snapshot.Banner, num, (num2 != num) ? num2 : 4289374890u))
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
		List<uint> source = bannerPaletteColors.Where((uint x) => x != backgroundColor).ToList();
		uint num3 = num2;
		if (num3 == backgroundColor || BannerManager.GetColorId(num3) < 0 || ComputeColorDistance(backgroundColor, num3) < 140)
		{
			num3 = source.OrderByDescending((uint x) => ComputeColorDistance(backgroundColor, x)).FirstOrDefault();
			if (num3 == 0)
			{
				num3 = source.FirstOrDefault();
			}
			if (num3 == 0 || num3 == backgroundColor)
			{
				num3 = uint.MaxValue;
			}
		}
		Banner val = snapshot?.Banner ?? ((clan != null) ? clan.ClanOriginalBanner : null) ?? ((clan != null) ? clan.Banner : null);
		Banner val2 = (Banner)((val == null) ? ((object)Banner.CreateRandomClanBanner(Extensions.GetDeterministicHashCode(((clan != null) ? ((MBObjectBase)clan).StringId : null) ?? "new_kingdom"))) : ((object)new Banner(val, backgroundColor, num3)));
		val2.ChangePrimaryColor(backgroundColor);
		if (num3 != uint.MaxValue)
		{
			val2.ChangeIconColors(num3);
		}
		return new RebelFactionColorChoice
		{
			BackgroundColor = backgroundColor,
			IconColor = num3,
			Banner = val2
		};
	}

	private static void ApplyRebelFactionColorChoiceToClan(Clan clan, RebelFactionColorChoice colorChoice)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		if (clan != null && colorChoice != null)
		{
			clan.Color = colorChoice.BackgroundColor;
			clan.Color2 = colorChoice.IconColor;
			if (colorChoice.Banner != null)
			{
				clan.Banner = new Banner(colorChoice.Banner);
			}
			clan.UpdateBannerColor(colorChoice.BackgroundColor, colorChoice.IconColor);
		}
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
			return ((IEnumerable<Kingdom>)Kingdom.All).Any((Kingdom x) => x != null && string.Equals((((object)x.Name)?.ToString() ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
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
			if (followerClan != null)
			{
				string clanDisplayName = GetClanDisplayName(followerClan);
				string heroDisplayName = GetHeroDisplayName(followerClan.Leader);
				string text = BuildClanFortificationSummary(followerClan);
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "暂无可用封地摘要";
				}
				list.Add(clanDisplayName + "（族长：" + heroDisplayName + "；" + text + "）");
			}
		}
		if (list.Count == 0)
		{
			return "无";
		}
		return string.Join("\n", from x in list.Distinct(StringComparer.OrdinalIgnoreCase)
			select "- " + x);
	}

	private string BuildRebelKingdomNamingUserPrompt(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null)
	{
		List<string> list = new List<string>();
		try
		{
			foreach (Kingdom item in ((IEnumerable<Kingdom>)Kingdom.All).Where((Kingdom x) => x != null))
			{
				string text = (((object)item.Name)?.ToString() ?? "").Trim();
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
		List<Settlement> list2 = ((clan == null) ? null : ((IEnumerable<Settlement>)clan.Settlements)?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).ToList()) ?? new List<Settlement>();
		string text3 = ((list2.Count > 0) ? string.Join("、", list2.Select(GetSettlementDisplayName)) : "无");
		string value = BuildRebelFollowerSummaryForNamingPrompt(followerClans);
		string value2 = BuildHeroBackgroundForRebelNamingPrompt((clan != null) ? clan.Leader : null);
		string value3 = BuildHeroBackgroundForRebelNamingPrompt((oldKingdom != null) ? oldKingdom.Leader : null);
		string value4 = BuildKingdomBackgroundForRebelNamingPrompt(oldKingdom);
		EventRecordEntry entry = FindWeeklyReportRecordByWeek("world", "", weekIndex - 1);
		EventRecordEntry entry2 = FindWeeklyReportRecordByWeek("kingdom", GetKingdomId(oldKingdom), weekIndex - 1);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【叛乱建国命名任务】");
		stringBuilder.AppendLine("当前周次：第 " + Math.Max(0, weekIndex) + " 周");
		stringBuilder.AppendLine("主导家族：" + GetClanDisplayName(clan));
		stringBuilder.AppendLine("主导族长：" + GetHeroDisplayName((clan != null) ? clan.Leader : null));
		object obj2;
		if (clan == null)
		{
			obj2 = null;
		}
		else
		{
			CultureObject culture = clan.Culture;
			obj2 = ((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString());
		}
		if (obj2 == null)
		{
			obj2 = "";
		}
		stringBuilder.AppendLine("家族文化：" + ((string)obj2).Trim());
		stringBuilder.AppendLine("原所属王国：" + GetKingdomDisplayName(oldKingdom, "原王国"));
		stringBuilder.AppendLine("原所属王国领袖（被背叛者）：" + GetHeroDisplayName((oldKingdom != null) ? oldKingdom.Leader : null));
		stringBuilder.AppendLine("原王国执政家族：" + GetClanDisplayName((oldKingdom != null) ? oldKingdom.RulingClan : null));
		stringBuilder.AppendLine("当前核心封地：" + text3);
		stringBuilder.AppendLine("叛乱背景：该家族因与旧王国统治层关系恶化，在王国内部稳定度恶化时带着现有封地脱离旧王国，并准备自立为新的政治实体。");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【联合响应家族】");
		stringBuilder.AppendLine(value);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【叛乱家族族长背景】");
		stringBuilder.AppendLine(value2);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【原王国统治者背景】");
		stringBuilder.AppendLine(value3);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【原王国背景】");
		stringBuilder.AppendLine(value4);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【上周世界周报】");
		stringBuilder.AppendLine(BuildWeeklyReportLeadInForRebelNamingPrompt(entry));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【上周原王国周报】");
		stringBuilder.AppendLine(BuildWeeklyReportLeadInForRebelNamingPrompt(entry2));
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
			text += "（新）";
		}
		string shortName = BuildRebelKingdomFallbackShortName(text, clan);
		string encyclopediaText = BuildRebelKingdomFallbackLoreText(clan, oldKingdom, text, weekIndex);
		return new RebelKingdomNamingResult
		{
			FormalName = text,
			ShortName = shortName,
			EncyclopediaText = encyclopediaText,
			UsedFallback = true,
			FailureReason = (failureReason ?? "").Trim(),
			AttemptsUsed = 0
		};
	}

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
				Task result = Task.WhenAny(task, Task.Delay(60000)).GetAwaiter().GetResult();
				apiCallResult = ((result != task) ? new ApiCallResult
				{
					ErrorMessage = "叛乱命名请求超时（60 秒）。"
				} : task.GetAwaiter().GetResult());
			}
			catch (Exception ex)
			{
				apiCallResult = new ApiCallResult
				{
					ErrorMessage = ex.Message
				};
			}
			string replyText = ((apiCallResult != null && apiCallResult.Success) ? (apiCallResult.Content ?? "") : ("错误: " + (apiCallResult?.ErrorMessage ?? "未知错误")));
			Logger.LogEventPromptExchange((logTarget ?? "叛乱建国命名") + " [尝试 " + i + "/" + maxAttempts + "]", "【System Prompt】\n" + systemPrompt + "\n\n【User Prompt】\n" + userPrompt, replyText);
			if (apiCallResult != null && apiCallResult.Success && TryParseRebelKingdomNamingResponse(apiCallResult.Content, out var formalName, out var shortName, out var encyclopediaText))
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
				rebelKingdomNamingResult.FailureReason = ((apiCallResult != null && apiCallResult.Success) ? "模型返回无法按 [NAME]/[SHORT]/[LORE] 解析。" : (apiCallResult?.ErrorMessage ?? "命名请求失败。"));
			}
			rebelKingdomNamingResult.AttemptsUsed = i;
			if (i < maxAttempts)
			{
				int num = 1200;
				if (apiCallResult != null && apiCallResult.IsRateLimit)
				{
					num = Math.Max(num, GetWeeklyReportRequestIntervalMs());
				}
				if (apiCallResult != null && apiCallResult.RetryAfterSeconds.HasValue)
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
		if (clan != kingdom.RulingClan)
		{
			Hero leader = kingdom.Leader;
			if (clan != ((leader != null) ? leader.Clan : null))
			{
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
				Hero leader2 = clan.Leader;
				if (leader2 == null || !leader2.IsAlive || leader2.IsChild)
				{
					note = "族长状态无效。";
					return false;
				}
				if (leader2.IsPrisoner)
				{
					note = "族长当前被囚，暂不触发带城反出。";
					return false;
				}
				townCount = ((IEnumerable<Settlement>)clan.Settlements)?.Count((Settlement x) => x != null && x.IsTown) ?? 0;
				castleCount = ((IEnumerable<Settlement>)clan.Settlements)?.Count((Settlement x) => x != null && x.IsCastle) ?? 0;
				if (townCount + castleCount <= 0)
				{
					note = "该家族没有城镇或城堡，不能带城反出。";
					return false;
				}
				try
				{
					relationToKing = ((kingdom.Leader != null) ? leader2.GetRelation(kingdom.Leader) : 0);
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
		}
		note = "执政家族不会作为叛乱候选。";
		return false;
	}

	private static float ComputeKingdomRebellionCandidateScore(Clan clan, Kingdom kingdom, int relationToKing, int townCount, int castleCount)
	{
		float num = 0f;
		num += Math.Min(900f, Math.Max(0f, (clan != null) ? clan.Renown : 0f) * 0.45f);
		num += (float)(Math.Max(0, (clan != null) ? clan.Tier : 0) * 140);
		num += Math.Min(420f, Math.Max(0f, (clan != null) ? clan.CurrentTotalStrength : 0f) / 4f);
		num += (float)(townCount * 140);
		num += (float)(castleCount * 70);
		num += (float)Math.Max(0, -relationToKing) * 1.5f;
		if (((clan == null) ? null : clan.Leader?.Culture) != null && ((kingdom == null) ? null : kingdom.Leader?.Culture) != null && clan.Leader.Culture == kingdom.Leader.Culture)
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
		num += (float)Math.Max(0, relationToLeader) * 3.5f;
		num += (float)Math.Max(0, -relationToKing) * 2.5f;
		if (((clan == null) ? null : clan.Leader?.Culture) != null && ((leaderClan == null) ? null : leaderClan.Leader?.Culture) != null && clan.Leader.Culture == leaderClan.Leader.Culture)
		{
			num += 6f;
		}
		if (((clan == null) ? null : clan.Leader?.Culture) != null && ((kingdom == null) ? null : kingdom.Leader?.Culture) != null && clan.Leader.Culture == kingdom.Leader.Culture)
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
		IEnumerable<Clan> clans = (IEnumerable<Clan>)kingdom.Clans;
		IEnumerable<Clan> enumerable = clans ?? Enumerable.Empty<Clan>();
		foreach (Clan item in enumerable)
		{
			if (item == null)
			{
				continue;
			}
			KingdomRebellionFollowerInfo kingdomRebellionFollowerInfo = new KingdomRebellionFollowerInfo
			{
				Clan = item,
				ClanId = GetClanId(item),
				ClanName = GetClanDisplayName(item)
			};
			if (!TryValidateClanForRebelFollower(item, kingdom, leaderClan, forceTrigger, out var note, out var relationToKing, out var relationToLeader, out var townCount, out var castleCount))
			{
				kingdomRebellionFollowerInfo.Eligible = false;
				kingdomRebellionFollowerInfo.Note = note;
				kingdomRebellionFollowerInfo.RelationToKing = relationToKing;
				kingdomRebellionFollowerInfo.RelationToLeader = relationToLeader;
				kingdomRebellionFollowerInfo.TownCount = Math.Max(0, townCount);
				kingdomRebellionFollowerInfo.CastleCount = Math.Max(0, castleCount);
				kingdomRebellionFollowerInfo.ClanTier = Math.Max(0, item.Tier);
			}
			else
			{
				kingdomRebellionFollowerInfo.RelationToKing = relationToKing;
				kingdomRebellionFollowerInfo.RelationToLeader = relationToLeader;
				kingdomRebellionFollowerInfo.TownCount = townCount;
				kingdomRebellionFollowerInfo.CastleCount = castleCount;
				kingdomRebellionFollowerInfo.ClanTier = Math.Max(0, item.Tier);
				kingdomRebellionFollowerInfo.Score = ComputeKingdomRebellionFollowerScore(item, kingdom, leaderClan, relationToKing, relationToLeader);
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
		return (from x in list
			orderby x.Eligible descending, x.Score descending
			select x).ThenBy((KingdomRebellionFollowerInfo x) => x.ClanName ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private List<KingdomRebellionCandidateInfo> EvaluateKingdomRebellionCandidates(Kingdom kingdom, bool forceTrigger)
	{
		List<KingdomRebellionCandidateInfo> list = new List<KingdomRebellionCandidateInfo>();
		if (kingdom == null)
		{
			return list;
		}
		IEnumerable<Clan> clans = (IEnumerable<Clan>)kingdom.Clans;
		IEnumerable<Clan> enumerable = clans ?? Enumerable.Empty<Clan>();
		foreach (Clan item in enumerable)
		{
			if (item != null)
			{
				KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = new KingdomRebellionCandidateInfo
				{
					Clan = item,
					ClanId = GetClanId(item),
					ClanName = GetClanDisplayName(item)
				};
				if (!TryValidateClanForKingdomRebellion(item, kingdom, forceTrigger, out var note, out var relationToKing, out var townCount, out var castleCount))
				{
					kingdomRebellionCandidateInfo.Eligible = false;
					kingdomRebellionCandidateInfo.Note = note;
					kingdomRebellionCandidateInfo.RelationToKing = relationToKing;
					kingdomRebellionCandidateInfo.TownCount = Math.Max(0, townCount);
					kingdomRebellionCandidateInfo.CastleCount = Math.Max(0, castleCount);
					kingdomRebellionCandidateInfo.TotalFortificationCount = kingdomRebellionCandidateInfo.TownCount + kingdomRebellionCandidateInfo.CastleCount;
					kingdomRebellionCandidateInfo.ClanTier = Math.Max(0, item.Tier);
				}
				else
				{
					kingdomRebellionCandidateInfo.Eligible = true;
					kingdomRebellionCandidateInfo.Note = (forceTrigger ? "满足强制触发条件。" : "满足自动叛乱条件。");
					kingdomRebellionCandidateInfo.RelationToKing = relationToKing;
					kingdomRebellionCandidateInfo.TownCount = townCount;
					kingdomRebellionCandidateInfo.CastleCount = castleCount;
					kingdomRebellionCandidateInfo.TotalFortificationCount = townCount + castleCount;
					kingdomRebellionCandidateInfo.ClanTier = Math.Max(0, item.Tier);
					kingdomRebellionCandidateInfo.Score = ComputeKingdomRebellionCandidateScore(item, kingdom, relationToKing, townCount, castleCount);
				}
				list.Add(kingdomRebellionCandidateInfo);
			}
		}
		return (from x in list
			orderby x.Eligible descending, x.Score descending
			select x).ThenBy((KingdomRebellionCandidateInfo x) => x.ClanName ?? "", StringComparer.OrdinalIgnoreCase).ToList();
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
		int num = (kingdomRebellionResolutionResult.StabilityValue = GetKingdomStabilityValue(kingdom));
		kingdomRebellionResolutionResult.StabilityTierText = GetKingdomStabilityTierText(num);
		kingdomRebellionResolutionResult.TriggerChance = GetKingdomRebellionWeeklyChance(num);
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
			item.PreviewFollowerClanNames = (from x in EvaluateKingdomRebellionFollowers(kingdom, item.Clan, forceTrigger: false)
				where x != null && x.Eligible && x.Clan != null
				select GetClanDisplayName(x.Clan) into x
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
		kingdomRebellionResolutionResult.SelectedFollowerClans = (from x in kingdomRebellionResolutionResult.FollowerCandidates
			where x != null && x.Eligible && x.Clan != null
			select x.Clan).ToList();
		if (!executeAction)
		{
			string text = ((kingdomRebellionResolutionResult.SelectedFollowerClans.Count > 0) ? ("；预计跟随家族 " + string.Join("、", kingdomRebellionResolutionResult.SelectedFollowerClans.Select(GetClanDisplayName))) : "；当前没有会联合响应的其他家族。");
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
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		//IL_01c4: Expected O, but got Unknown
		//IL_01c4: Expected O, but got Unknown
		//IL_01c4: Expected O, but got Unknown
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
		ClanVisualSnapshot snapshot = CaptureClanVisualSnapshot(clan);
		ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(clan, true);
		RestoreClanVisualSnapshot(clan, snapshot);
		RebelFactionColorChoice rebelFactionColorChoice = BuildRandomUniqueRebelFactionColors(clan, kingdom, snapshot);
		ApplyRebelFactionColorChoiceToClan(clan, rebelFactionColorChoice);
		KingdomManager val = Campaign.Current?.KingdomManager;
		if (val == null)
		{
			message = clanDisplayName + " 已从 " + kingdomDisplayName + " 反出，但当前未找到 KingdomManager，未能继续建立新王国。";
			Logger.Log("KingdomRebellion", "[ERROR] KingdomManager unavailable after rebellion leave. clan=" + GetClanId(clan));
			return true;
		}
		val.CreateKingdom(new TextObject(text, (Dictionary<string, object>)null), new TextObject(text2, (Dictionary<string, object>)null), clan.Culture ?? kingdom.Culture, clan, (MBReadOnlyList<PolicyObject>)null, new TextObject(text3, (Dictionary<string, object>)null), new TextObject(text, (Dictionary<string, object>)null), (TextObject)null);
		Kingdom kingdom2 = clan.Kingdom;
		if (kingdom != null && !kingdom.IsEliminated)
		{
			SetKingdomStabilityValue(kingdom, 50);
		}
		if (kingdom2 != null && !kingdom2.IsEliminated)
		{
			SetKingdomStabilityValue(kingdom2, 30);
		}
		List<string> list = new List<string>();
		if (kingdom2 != null && followerClans != null && followerClans.Count > 0)
		{
			foreach (Clan item in from x in followerClans.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => GetClanId(x), StringComparer.OrdinalIgnoreCase)
				select x.First())
			{
				try
				{
					if (!item.IsEliminated && item.Kingdom == kingdom && !item.IsUnderMercenaryService && !item.IsClanTypeMercenary)
					{
						ChangeKingdomAction.ApplyByJoinToKingdomByDefection(item, kingdom, kingdom2, default(CampaignTime), true);
						list.Add(GetClanDisplayName(item));
						MarkClanVisualsDirty(item);
					}
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomRebellion", "[WARN] Failed to attach follower clan to rebel kingdom. leader=" + GetClanId(clan) + " follower=" + GetClanId(item) + " error=" + ex.Message);
				}
			}
		}
		MarkClanVisualsDirty(clan);
		string text4 = BuildClanFortificationSummary(clan);
		message = clanDisplayName + " 已从 " + kingdomDisplayName + " 反出，并建立了 " + text + "。";
		if (!string.IsNullOrWhiteSpace(text4))
		{
			message = message + " " + text4;
		}
		if (list.Count > 0)
		{
			message = message + " 联合响应家族：" + string.Join("、", list) + "。";
		}
		if (rebelKingdomNamingResult != null && rebelKingdomNamingResult.UsedFallback)
		{
			Logger.Log("KingdomRebellion", "[WARN] Rebel kingdom naming used fallback. clan=" + GetClanId(clan) + " reason=" + rebelKingdomNamingResult.FailureReason);
		}
		Logger.Log("KingdomRebellion", "[EXECUTE] week=" + weekIndex + " kingdom=" + GetKingdomId(kingdom) + " clan=" + GetClanId(clan) + " forced=" + forceTrigger + " relation=" + relationToKing + " towns=" + townCount + " castles=" + castleCount + " newKingdomName=" + text + " fallback=" + ((rebelKingdomNamingResult != null && rebelKingdomNamingResult.UsedFallback) ? "1" : "0") + " followerCount=" + list.Count + " followers=" + string.Join("|", list) + " bgColor=0x" + rebelFactionColorChoice.BackgroundColor.ToString("X8") + " iconColor=0x" + rebelFactionColorChoice.IconColor.ToString("X8"));
		return true;
	}

	private void TryProcessWeeklyKingdomRebellions(int weekIndex)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
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
					KingdomRebellionResolutionResult kingdomRebellionResolutionResult = ResolveKingdomRebellion(devEditableKingdom, weekIndex, executeAction: true, forceTrigger: false);
					if (kingdomRebellionResolutionResult != null && kingdomRebellionResolutionResult.Executed && !string.IsNullOrWhiteSpace(kingdomRebellionResolutionResult.Message))
					{
						InformationManager.DisplayMessage(new InformationMessage("[王国叛乱] " + kingdomRebellionResolutionResult.Message, new Color(1f, 0.75f, 0.3f, 1f)));
					}
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomRebellion", "[ERROR] Weekly resolution failed for kingdom " + GetKingdomId(devEditableKingdom) + ": " + ex.Message);
				}
			}
			_lastProcessedKingdomRebellionWeek = Math.Max(_lastProcessedKingdomRebellionWeek, weekIndex);
		}
		catch (Exception ex2)
		{
			Logger.Log("KingdomRebellion", "[ERROR] TryProcessWeeklyKingdomRebellions failed: " + ex2);
		}
	}

	private static Settlement ResolveGatheringPointSettlement(IMapPoint gatheringPoint, MobileParty fallbackParty = null)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Settlement val = (Settlement)(object)((gatheringPoint is Settlement) ? gatheringPoint : null);
		if (val != null)
		{
			return val;
		}
		try
		{
			if (gatheringPoint != null)
			{
				CampaignVec2 position = gatheringPoint.Position;
				Settlement val2 = SettlementHelper.FindNearestSettlementToPoint(ref position, (Func<Settlement, bool>)((Settlement x) => x != null && !x.IsHideout));
				if (val2 != null)
				{
					return val2;
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
				return SettlementHelper.FindNearestSettlementToMobileParty(fallbackParty, (NavigationType)3, (Func<Settlement, bool>)((Settlement x) => x != null && !x.IsHideout));
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement ResolveSettlementForPartyBase(PartyBase party)
	{
		MobileParty val = ((party != null) ? party.MobileParty : null);
		if (val == null)
		{
			return null;
		}
		return val.BesiegedSettlement ?? ResolveSiegeSettlement(val.SiegeEvent) ?? val.TargetSettlement ?? val.CurrentSettlement ?? val.LastVisitedSettlement;
	}

	private static string GetLocationLabelForPartyBase(PartyBase party)
	{
		string nearestSettlementNameForParty = GetNearestSettlementNameForParty((party != null) ? party.MobileParty : null);
		return string.IsNullOrWhiteSpace(nearestSettlementNameForParty) ? "" : nearestSettlementNameForParty;
	}

	private static string ResolveGatheringPointLabel(IMapPoint gatheringPoint, MobileParty fallbackParty = null)
	{
		Settlement val = ResolveGatheringPointSettlement(gatheringPoint, fallbackParty);
		string text = (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (((gatheringPoint == null) ? null : ((object)gatheringPoint.Name)?.ToString()) ?? "").Trim();
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
		if (facts != null && hero != null)
		{
			facts.ActorHeroId = GetHeroId(hero);
			facts.ActorClanId = GetClanId(hero.Clan);
			facts.ActorKingdomId = GetKingdomId(hero.MapFaction);
			AddUniqueId(facts.RelatedHeroIds, facts.ActorHeroId);
			AddUniqueId(facts.RelatedClanIds, facts.ActorClanId);
			AddUniqueId(facts.RelatedKingdomIds, facts.ActorKingdomId);
		}
	}

	private static void ApplyTargetFacts(NpcActionFacts facts, Hero hero)
	{
		if (facts != null && hero != null)
		{
			facts.TargetHeroId = GetHeroId(hero);
			facts.TargetClanId = GetClanId(hero.Clan);
			facts.TargetKingdomId = GetKingdomId(hero.MapFaction);
			AddUniqueId(facts.RelatedHeroIds, facts.TargetHeroId);
			AddUniqueId(facts.RelatedClanIds, facts.TargetClanId);
			AddUniqueId(facts.RelatedKingdomIds, facts.TargetKingdomId);
		}
	}

	private static void ApplySettlementFacts(NpcActionFacts facts, Settlement settlement, Hero currentOwnerOverride = null, Hero previousOwnerOverride = null, string locationText = null)
	{
		if (facts != null && settlement != null)
		{
			facts.SettlementId = GetSettlementId(settlement);
			facts.SettlementName = (((object)settlement.Name)?.ToString() ?? "").Trim();
			facts.LocationText = (string.IsNullOrWhiteSpace(locationText) ? facts.SettlementName : locationText.Trim());
			object obj = currentOwnerOverride;
			if (obj == null)
			{
				Clan ownerClan = settlement.OwnerClan;
				obj = ((ownerClan != null) ? ownerClan.Leader : null);
			}
			Hero val = (Hero)obj;
			facts.SettlementOwnerHeroId = GetHeroId(val);
			facts.SettlementOwnerClanId = GetClanId(((val != null) ? val.Clan : null) ?? settlement.OwnerClan);
			facts.SettlementOwnerKingdomId = GetKingdomId(((val != null) ? val.MapFaction : null) ?? settlement.MapFaction);
			facts.PreviousSettlementOwnerHeroId = GetHeroId(previousOwnerOverride);
			facts.PreviousSettlementOwnerClanId = GetClanId((previousOwnerOverride != null) ? previousOwnerOverride.Clan : null);
			facts.PreviousSettlementOwnerKingdomId = GetKingdomId((previousOwnerOverride != null) ? previousOwnerOverride.MapFaction : null);
			AddUniqueId(facts.RelatedHeroIds, facts.SettlementOwnerHeroId);
			AddUniqueId(facts.RelatedClanIds, facts.SettlementOwnerClanId);
			AddUniqueId(facts.RelatedKingdomIds, facts.SettlementOwnerKingdomId);
			AddUniqueId(facts.RelatedHeroIds, facts.PreviousSettlementOwnerHeroId);
			AddUniqueId(facts.RelatedClanIds, facts.PreviousSettlementOwnerClanId);
			AddUniqueId(facts.RelatedKingdomIds, facts.PreviousSettlementOwnerKingdomId);
		}
	}

	private static void AddRelatedFactionFacts(NpcActionFacts facts, IFaction faction)
	{
		if (facts == null || faction == null)
		{
			return;
		}
		Kingdom val = (Kingdom)(object)((faction is Kingdom) ? faction : null);
		if (val != null)
		{
			string kingdomId = GetKingdomId(val);
			if (string.IsNullOrWhiteSpace(facts.TargetKingdomId))
			{
				facts.TargetKingdomId = kingdomId;
			}
			AddUniqueId(facts.RelatedKingdomIds, kingdomId);
			return;
		}
		Clan val2 = (Clan)(object)((faction is Clan) ? faction : null);
		if (val2 != null)
		{
			string clanId = GetClanId(val2);
			if (string.IsNullOrWhiteSpace(facts.TargetClanId))
			{
				facts.TargetClanId = clanId;
			}
			AddUniqueId(facts.RelatedClanIds, clanId);
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(val2.Kingdom));
		}
	}

	private static void AddUniqueId(List<string> list, string id)
	{
		string text = (id ?? "").Trim();
		if (list != null && !string.IsNullOrWhiteSpace(text) && !list.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
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
		if (!string.IsNullOrWhiteSpace(text))
		{
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
				eventSourceMaterialEntry.IncludeInWorld |= includeInWorld;
				eventSourceMaterialEntry.IncludeInKingdom |= includeInKingdom;
			}
			else
			{
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
		}
	}

	private void TrackTownWeeklyMaterialChanges(Town town)
	{
		if (((town != null) ? ((SettlementComponent)town).Settlement : null) == null || !((SettlementComponent)town).Settlement.IsFortification)
		{
			return;
		}
		string settlementId = GetSettlementId(((SettlementComponent)town).Settlement);
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
		if (list.Count != 0)
		{
			string settlementDisplayName = GetSettlementDisplayName(((SettlementComponent)town).Settlement);
			string text = settlementDisplayName + "本日出现定居点状态波动：" + string.Join("；", list) + "。";
			string clanDisplayName = GetClanDisplayName(((SettlementComponent)town).Settlement.OwnerClan);
			IFaction mapFaction = ((SettlementComponent)town).Settlement.MapFaction;
			string kingdomDisplayName = GetKingdomDisplayName((Kingdom)(object)((mapFaction is Kingdom) ? mapFaction : null), "所属王国");
			if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
			{
				text = text + " 当前由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
			}
			RecordEventSourceMaterial("settlement_stats", "定居点状态变化 - " + settlementDisplayName, text, "settlement_stats:" + settlementId + ":" + GetCurrentGameDayIndexSafe(), GetKingdomId(((SettlementComponent)town).Settlement.MapFaction), settlementId, includeInWorld: false, includeInKingdom: true);
		}
	}

	private static TownStatSnapshot CaptureTownSnapshot(Town town)
	{
		TownStatSnapshot obj = new TownStatSnapshot
		{
			Prosperity = ((town != null) ? town.Prosperity : 0f),
			Loyalty = ((town != null) ? town.Loyalty : 0f),
			Security = ((town != null) ? town.Security : 0f),
			FoodStocks = ((town != null) ? ((Fief)town).FoodStocks : 0f)
		};
		float? obj2;
		if (town == null)
		{
			obj2 = null;
		}
		else
		{
			Settlement settlement = ((SettlementComponent)town).Settlement;
			obj2 = ((settlement != null) ? new float?(settlement.Militia) : ((float?)null));
		}
		float? num = obj2;
		obj.Militia = num.GetValueOrDefault();
		int? obj3;
		if (town == null)
		{
			obj3 = null;
		}
		else
		{
			MobileParty garrisonParty = ((Fief)town).GarrisonParty;
			if (garrisonParty == null)
			{
				obj3 = null;
			}
			else
			{
				TroopRoster memberRoster = garrisonParty.MemberRoster;
				obj3 = ((memberRoster != null) ? new int?(memberRoster.TotalManCount) : ((int?)null));
			}
		}
		int? num2 = obj3;
		obj.Garrison = num2.GetValueOrDefault();
		return obj;
	}

	private static void AppendTownChangeLine(List<string> lines, string label, float oldValue, float newValue, float threshold)
	{
		if (lines != null)
		{
			float num = newValue - oldValue;
			if (!(MathF.Abs(num) < threshold))
			{
				lines.Add(label + ((num > 0f) ? "上升" : "下降") + MathF.Abs(num).ToString("0.#"));
			}
		}
	}

	private static void AppendTownChangeLine(List<string> lines, string label, int oldValue, int newValue, int threshold)
	{
		if (lines != null)
		{
			int num = newValue - oldValue;
			if (Math.Abs(num) >= threshold)
			{
				lines.Add(label + ((num > 0) ? "上升" : "下降") + Math.Abs(num));
			}
		}
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
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = currentGameDayIndexSafe - 21 + 1;
		int num2 = 0;
		int num3 = 0;
		foreach (NpcActionEntry item in source)
		{
			if (item != null)
			{
				string text = (item.Text ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && (!keepOnlyRecentWindow || item.Day >= num))
				{
					list.Add(new NpcActionEntry
					{
						Day = Math.Max(0, item.Day),
						Order = ((item.Order > 0) ? item.Order : (++num2)),
						Sequence = ((item.Sequence > 0) ? item.Sequence : (++num3)),
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
			}
		}
		return (from x in list
			orderby x.Day, (x.Sequence > 0) ? x.Sequence : int.MaxValue, x.Order
			select x).ThenBy((NpcActionEntry x) => x.GameDate ?? "", StringComparer.Ordinal).ToList();
	}

	private void RecordNpcMajorAction(Hero hero, string text, string stableKey, NpcActionFacts facts = null)
	{
		RecordNpcActionInternal(_npcMajorActions, hero, text, stableKey, keepOnlyRecentWindow: false, dedupeAcrossWindow: false, 160, facts, isMajor: true);
	}

	private void RecordNpcRecentAction(Hero hero, string text, string stableKey, bool dedupeAcrossWindow = false, NpcActionFacts facts = null)
	{
		RecordNpcActionInternal(_npcRecentActions, hero, text, stableKey, keepOnlyRecentWindow: true, dedupeAcrossWindow, 96, facts, isMajor: false);
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
				value = (storage[npcActionHeroKey] = new List<NpcActionEntry>());
			}
			if (keepOnlyRecentWindow)
			{
				int num = currentGameDayIndexSafe - 21 + 1;
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
			int order = ((value.Count <= 0) ? 1 : (value.Max((NpcActionEntry x) => (x != null && x.Day == currentGameDayIndexSafe) ? x.Order : 0) + 1));
			int sequence = ++_npcActionGlobalOrderCounter;
			value.Add(CreateNpcActionEntry(hero, text2, text3, currentGameDayIndexSafe, order, sequence, facts, isMajor));
			value = (from x in value
				orderby x.Day, (x.Sequence > 0) ? x.Sequence : int.MaxValue, x.Order
				select x).ThenBy((NpcActionEntry x) => x.GameDate ?? "", StringComparer.Ordinal).ToList();
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
		string text = ((army == null) ? null : ((object)army.Name)?.ToString());
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj = ((army != null) ? army.ArmyOwner : null);
		if (obj == null)
		{
			if (army == null)
			{
				obj = null;
			}
			else
			{
				MobileParty leaderParty = army.LeaderParty;
				obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
			}
		}
		Hero val = (Hero)obj;
		string text2 = ((val == null) ? null : ((object)val.Name)?.ToString());
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
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (((item != null) ? item.SiegeEvent : null) == siegeEvent)
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
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<Hero> list = new List<Hero>();
		if (siegeEvent == null)
		{
			return list;
		}
		try
		{
			foreach (PartyBase item2 in siegeEvent.GetInvolvedPartiesForEventType((BattleTypes)5))
			{
				if (item2 != null && item2.Side == side)
				{
					Hero leaderHero = item2.LeaderHero;
					string item = ((leaderHero == Hero.MainHero) ? "__player__" : GetNpcActionHeroKey(leaderHero));
					if (ShouldMentionBattleHero(leaderHero) && hashSet.Add(item))
					{
						list.Add(leaderHero);
					}
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
		object obj;
		if (mapEvent == null)
		{
			obj = null;
		}
		else
		{
			Settlement mapEventSettlement = mapEvent.MapEventSettlement;
			obj = ((mapEventSettlement == null) ? null : ((object)mapEventSettlement.Name)?.ToString());
		}
		string text = (string)obj;
		return string.IsNullOrWhiteSpace(text) ? "野外" : text.Trim();
	}

	private static string GetPrimaryOtherSideLabel(MapEventSide side)
	{
		object obj;
		if (side == null)
		{
			obj = null;
		}
		else
		{
			PartyBase leaderParty = side.LeaderParty;
			if (leaderParty == null)
			{
				obj = null;
			}
			else
			{
				Hero leaderHero = leaderParty.LeaderHero;
				obj = ((leaderHero == null) ? null : ((object)leaderHero.Name)?.ToString());
			}
		}
		string text = (string)obj;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj2;
		if (side == null)
		{
			obj2 = null;
		}
		else
		{
			PartyBase leaderParty2 = side.LeaderParty;
			obj2 = ((leaderParty2 == null) ? null : ((object)leaderParty2.Name)?.ToString());
		}
		text = (string)obj2;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj3;
		if (side == null)
		{
			obj3 = null;
		}
		else
		{
			IFaction mapFaction = side.MapFaction;
			obj3 = ((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString());
		}
		text = (string)obj3;
		return string.IsNullOrWhiteSpace(text) ? "敌军" : text.Trim();
	}

	private static int GetMapEventTroopCount(MapEvent mapEvent)
	{
		try
		{
			return Math.Max(0, (mapEvent != null) ? mapEvent.GetNumberOfInvolvedMen() : 0);
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
			if (((side != null) ? side.Parties : null) == null)
			{
				return 0;
			}
			foreach (MapEventParty item in (List<MapEventParty>)(object)side.Parties)
			{
				object hero;
				if (item == null)
				{
					hero = null;
				}
				else
				{
					PartyBase party = item.Party;
					hero = ((party != null) ? party.LeaderHero : null);
				}
				if (ShouldTrackNpcActionHero((Hero)hero))
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
			return Math.Max(0, (roster != null) ? roster.TotalManCount : 0);
		}
		catch
		{
			return 0;
		}
	}

	private static string GetFactionDisplayName(IFaction faction, string fallback = "某势力")
	{
		string text = ((faction == null) ? null : ((object)faction.Name)?.ToString());
		return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
	}

	private static string GetHeroFactionDisplayName(Hero hero, IFaction fallbackFaction = null)
	{
		return GetFactionDisplayName(((hero != null) ? hero.MapFaction : null) ?? fallbackFaction);
	}

	private static string BuildBattleHeroDisplayName(Hero hero, bool isHighlighted, string highlightTag)
	{
		string text = ((hero == null) ? null : ((object)hero.Name)?.ToString()?.Trim());
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ((hero == Hero.MainHero) ? "玩家" : "");
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
			text = text + "（" + highlightTag + "）";
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
		foreach (Hero item2 in from x in heroes.Where(ShouldMentionBattleHero)
			orderby x == highlightedHero descending
			select x)
		{
			string text = BuildBattleHeroDisplayName(item2, item2 == highlightedHero, highlightTag);
			string item = ((item2 == Hero.MainHero) ? "__player__" : GetNpcActionHeroKey(item2));
			if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(item))
			{
				list.Add(text);
			}
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
		if (((side != null) ? side.Parties : null) == null)
		{
			return "";
		}
		IEnumerable<Hero> heroes = ((IEnumerable<MapEventParty>)side.Parties).Select(delegate(MapEventParty x)
		{
			object result;
			if (x == null)
			{
				result = null;
			}
			else
			{
				PartyBase party = x.Party;
				result = ((party != null) ? party.LeaderHero : null);
			}
			return (Hero)result;
		});
		PartyBase leaderParty = side.LeaderParty;
		return BuildTrackedHeroListText(heroes, (leaderParty != null) ? leaderParty.LeaderHero : null, "统帅", maxCount);
	}

	private static string BuildTrackedHeroListText(SiegeEvent siegeEvent, BattleSideEnum side, int maxCount = 5)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		if ((int)side != 1)
		{
			if (siegeEvent == null)
			{
				obj = null;
			}
			else
			{
				Settlement besiegedSettlement = siegeEvent.BesiegedSettlement;
				if (besiegedSettlement == null)
				{
					obj = null;
				}
				else
				{
					Clan ownerClan = besiegedSettlement.OwnerClan;
					obj = ((ownerClan != null) ? ownerClan.Leader : null);
				}
			}
		}
		else if (siegeEvent == null)
		{
			obj = null;
		}
		else
		{
			BesiegerCamp besiegerCamp = siegeEvent.BesiegerCamp;
			if (besiegerCamp == null)
			{
				obj = null;
			}
			else
			{
				MobileParty leaderParty = besiegerCamp.LeaderParty;
				obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
			}
		}
		Hero highlightedHero = (Hero)obj;
		return BuildTrackedHeroListText(GetHeroesFromSiegeEventSide(siegeEvent, side), highlightedHero, "统帅", maxCount);
	}

	private static string BuildMapEventCasualtyText(MapEventSide side)
	{
		if (((side != null) ? side.Parties : null) == null)
		{
			return "";
		}
		int num = 0;
		int num2 = 0;
		try
		{
			foreach (MapEventParty item in (List<MapEventParty>)(object)side.Parties)
			{
				num += GetTroopRosterTotalManCount((item != null) ? item.DiedInBattle : null);
				num2 += GetTroopRosterTotalManCount((item != null) ? item.WoundedInBattle : null);
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
		PartyBase party2 = party.Party;
		int troopRosterTotalManCount = GetTroopRosterTotalManCount((party2 != null) ? party2.MemberRoster : null);
		int troopRosterTotalManCount2 = GetTroopRosterTotalManCount(party.DiedInBattle);
		int troopRosterTotalManCount3 = GetTroopRosterTotalManCount(party.WoundedInBattle);
		return Math.Max(0, troopRosterTotalManCount + troopRosterTotalManCount2 + troopRosterTotalManCount3);
	}

	private static string BuildMapEventCommittedTroopText(MapEventSide side)
	{
		if (((side != null) ? side.Parties : null) == null)
		{
			return "";
		}
		int num = 0;
		try
		{
			foreach (MapEventParty item in (List<MapEventParty>)(object)side.Parties)
			{
				num += GetMapEventPartyCommittedTroopCount(item);
			}
		}
		catch
		{
		}
		return (num > 0) ? (num + "人") : "";
	}

	private static string BuildMapEventStandoutText(MapEventSide side)
	{
		if (((side != null) ? side.Parties : null) == null)
		{
			return "";
		}
		List<MapEventParty> list = ((IEnumerable<MapEventParty>)side.Parties).Where(delegate(MapEventParty x)
		{
			object hero;
			if (x == null)
			{
				hero = null;
			}
			else
			{
				PartyBase party2 = x.Party;
				hero = ((party2 != null) ? party2.LeaderHero : null);
			}
			return ShouldMentionBattleHero((Hero)hero);
		}).ToList();
		if (list.Count <= 4)
		{
			return "";
		}
		MapEventParty val = list.OrderByDescending((MapEventParty x) => x.ContributionToBattle).FirstOrDefault();
		if (val == null || val.ContributionToBattle <= 0)
		{
			return "";
		}
		int num = list.Sum((MapEventParty x) => Math.Max(0, x.ContributionToBattle));
		PartyBase party = val.Party;
		Hero val2 = ((party != null) ? party.LeaderHero : null);
		PartyBase leaderParty = side.LeaderParty;
		string text = BuildBattleHeroDisplayName(val2, ((leaderParty != null) ? leaderParty.LeaderHero : null) == val2, "统帅");
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (num <= 0)
		{
			return text + "战场贡献最突出。";
		}
		int num2 = (int)Math.Round((double)(100 * val.ContributionToBattle) / (double)num);
		if (num2 < 30)
		{
			return "";
		}
		string factionDisplayName = GetFactionDisplayName(((val2 != null) ? val2.MapFaction : null) ?? side.MapFaction);
		if (num2 > 90)
		{
			return "万军辟易！这根本不是一场战争，而是" + factionDisplayName + "的" + text + "与他部队的专属杀戮秀！敌人在他面前如同草芥一般不堪一击！";
		}
		if (num2 > 75)
		{
			return factionDisplayName + "的" + text + "几乎包揽了绝大多数的战果！他的大军如同利刃般撕裂敌阵，所向披靡！";
		}
		if (num2 > 50)
		{
			return factionDisplayName + "的" + text + "与他的部队在战场上战无不胜！他是" + factionDisplayName + "的英雄！";
		}
		return factionDisplayName + "的" + text + "与他的部队是我方的中流砥柱！他拥有最高的贡献！";
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
		object obj;
		if (side == null)
		{
			obj = null;
		}
		else
		{
			PartyBase leaderParty = side.LeaderParty;
			obj = ((leaderParty != null) ? leaderParty.LeaderHero : null);
		}
		Hero val = (Hero)obj;
		string text = ((val == null) ? null : ((object)val.Name)?.ToString()?.Trim());
		if (string.IsNullOrWhiteSpace(text) || val == actorHero)
		{
			return "";
		}
		return text + "统帅的军团";
	}

	private static string BuildMapEventNarrative(MapEvent mapEvent, MapEventSide side, Hero actorHero, bool won, string locationLabel)
	{
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Invalid comparison between Unknown and I4
		if (mapEvent == null || side == null)
		{
			return "";
		}
		string heroFactionDisplayName = GetHeroFactionDisplayName(actorHero, side.MapFaction);
		MapEventSide otherSide = side.OtherSide;
		string factionDisplayName = GetFactionDisplayName((otherSide != null) ? otherSide.MapFaction : null, GetPrimaryOtherSideLabel(side.OtherSide));
		Settlement mapEventSettlement = mapEvent.MapEventSettlement;
		string text = BuildArmyCommandClause(side, actorHero);
		string value;
		if (!mapEvent.IsSiegeAssault && !mapEvent.IsSiegeOutside && !mapEvent.IsSallyOut)
		{
			value = ((!mapEvent.IsRaid) ? (won ? ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "" : (text + "于")) + locationLabel + "击败了" + factionDisplayName + "。") : ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "" : (text + "于")) + locationLabel + "败给了" + factionDisplayName + "。")) : (won ? ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "" : (text + "于")) + locationLabel + "对" + factionDisplayName + "发动的袭掠中得手。") : ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "" : (text + "于")) + locationLabel + "对" + factionDisplayName + "发动的袭掠中失利。")));
		}
		else
		{
			string text2 = ((mapEventSettlement == null) ? null : ((object)mapEventSettlement.Name)?.ToString());
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = locationLabel;
			}
			MapEventSide otherSide2 = side.OtherSide;
			string factionDisplayName2 = GetFactionDisplayName((otherSide2 != null) ? otherSide2.MapFaction : null, factionDisplayName);
			value = (((int)side.MissionSide != 1) ? (won ? ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "" : (text + "参与的")) + text2.Trim() + "保卫战中击退了" + factionDisplayName2 + "。") : ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "" : (text + "参与的")) + text2.Trim() + "保卫战中败给了" + factionDisplayName2 + "。")) : (won ? ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "对" : (text + "对")) + factionDisplayName2 + "的领土" + text2.Trim() + "的围城战中获胜。") : ("你作为" + heroFactionDisplayName + "的领主，在" + (string.IsNullOrWhiteSpace(text) ? "对" : (text + "对")) + factionDisplayName2 + "的领土" + text2.Trim() + "的围攻中失利。")));
		}
		StringBuilder stringBuilder = new StringBuilder(value);
		string value2 = BuildTrackedHeroListText(side);
		string value3 = BuildTrackedHeroListText(side.OtherSide);
		if (!string.IsNullOrWhiteSpace(value2))
		{
			stringBuilder.Append(" 我方领主：").Append(value2).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(value3))
		{
			stringBuilder.Append(" 敌方领主：").Append(value3).Append('。');
		}
		string text3 = BuildMapEventCommittedTroopText(side);
		string text4 = BuildMapEventCommittedTroopText(side.OtherSide);
		if (!string.IsNullOrWhiteSpace(text3) || !string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.Append(" 战前投入兵力：我方").Append(string.IsNullOrWhiteSpace(text3) ? "不详" : text3);
			stringBuilder.Append("；敌方").Append(string.IsNullOrWhiteSpace(text4) ? "不详" : text4).Append('。');
		}
		string value4 = BuildMapEventCasualtyText(side);
		string value5 = BuildMapEventCasualtyText(side.OtherSide);
		stringBuilder.Append(" 我方死伤：").Append(value4);
		stringBuilder.Append("；敌方死伤：").Append(value5).Append('。');
		string value6 = BuildMapEventStandoutText(side);
		if (!string.IsNullOrWhiteSpace(value6))
		{
			stringBuilder.Append(" ：").Append(value6);
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
		string text3 = ((hero == null) ? null : ((object)hero.Name)?.ToString()?.Trim());
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "该NPC";
		}
		return text2 + "对" + text3 + "说: " + text;
	}

	private static string TagSceneSessionHistoryLine(string line, int sceneSessionId)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || sceneSessionId < 0)
		{
			return text;
		}
		return string.Format("{0}{1}] {2}", "[AF_SCENE_SESSION:", sceneSessionId, text);
	}

	private static bool TryStripSceneSessionHistoryMarker(string line, out string stripped, out int sceneSessionId)
	{
		stripped = (line ?? "").Trim();
		sceneSessionId = -1;
		if (string.IsNullOrWhiteSpace(stripped) || !stripped.StartsWith("[AF_SCENE_SESSION:", StringComparison.Ordinal))
		{
			return false;
		}
		int num = stripped.IndexOf(']');
		if (num <= "[AF_SCENE_SESSION:".Length)
		{
			return false;
		}
		string s = stripped.Substring("[AF_SCENE_SESSION:".Length, num - "[AF_SCENE_SESSION:".Length).Trim();
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
			Clan clan = mainHero.Clan;
			int num = ((clan != null) ? clan.Tier : 0);
			if (num >= 2)
			{
				string text = (((object)mainHero.Name)?.ToString() ?? "").Trim();
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
			CultureObject culture = mainHero.Culture;
			string text3 = (((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString()) ?? "").Trim();
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
		string stripped = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(stripped))
		{
			return "";
		}
		TryStripSceneSessionHistoryMarker(stripped, out stripped, out var _);
		if (!TryStripPlayerSpeechPrefix(stripped, out var stripped2))
		{
			return stripped;
		}
		string text = BuildPlayerPublicDisplayNameForPrompt();
		string text2 = (addressToYou ? "你" : (targetDisplayName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "该NPC";
		}
		return text + "对" + text2 + "说: " + stripped2;
	}

	private static bool TryStripPlayerSpeechPrefix(string line, out string stripped)
	{
		stripped = "";
		string stripped2 = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(stripped2))
		{
			return false;
		}
		TryStripSceneSessionHistoryMarker(stripped2, out stripped2, out var _);
		if (stripped2.StartsWith("玩家:", StringComparison.Ordinal))
		{
			stripped = stripped2.Substring("玩家:".Length).Trim();
			return true;
		}
		if (stripped2.StartsWith("玩家：", StringComparison.Ordinal))
		{
			stripped = stripped2.Substring("玩家：".Length).Trim();
			return true;
		}
		int num = stripped2.IndexOf("（player）对", StringComparison.Ordinal);
		if (num >= 0)
		{
			int num2 = stripped2.IndexOf("说:", num, StringComparison.Ordinal);
			if (num2 < 0)
			{
				num2 = stripped2.IndexOf("说：", num, StringComparison.Ordinal);
			}
			if (num2 >= 0)
			{
				stripped = stripped2.Substring(num2 + 2).Trim();
				return true;
			}
		}
		return false;
	}

	private static string BuildSiegeStartNarrative(Settlement settlement, bool isAttacker, SiegeEvent siegeEvent)
	{
		string text = ((settlement == null) ? null : ((object)settlement.Name)?.ToString()) ?? "某处要塞";
		string factionDisplayName = GetFactionDisplayName((settlement != null) ? settlement.MapFaction : null, "守军");
		object faction;
		if (siegeEvent == null)
		{
			faction = null;
		}
		else
		{
			BesiegerCamp besiegerCamp = siegeEvent.BesiegerCamp;
			if (besiegerCamp == null)
			{
				faction = null;
			}
			else
			{
				MobileParty leaderParty = besiegerCamp.LeaderParty;
				faction = ((leaderParty != null) ? leaderParty.MapFaction : null);
			}
		}
		string factionDisplayName2 = GetFactionDisplayName((IFaction)faction, "攻方");
		StringBuilder stringBuilder = new StringBuilder(isAttacker ? ("你参与了对" + factionDisplayName + "领土" + text + "的围攻。") : ("你参与了" + text + "的守城，对抗" + factionDisplayName2 + "。"));
		string value = BuildTrackedHeroListText(siegeEvent, (BattleSideEnum)1);
		string value2 = BuildTrackedHeroListText(siegeEvent, (BattleSideEnum)0);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.Append(" 攻方领主：").Append(value).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(value2))
		{
			stringBuilder.Append(" 守方领主：").Append(value2).Append('。');
		}
		return stringBuilder.ToString();
	}

	private void TrackNpcActionsFromMapEvent(MapEvent mapEvent)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Invalid comparison between Unknown and I4
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Invalid comparison between Unknown and I4
		if (mapEvent != null && mapEvent.HasWinner)
		{
			int mapEventTroopCount = GetMapEventTroopCount(mapEvent);
			bool isMajor = mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut || mapEvent.IsRaid || mapEventTroopCount >= 120 || CountTrackedLordParties(mapEvent.AttackerSide) + CountTrackedLordParties(mapEvent.DefenderSide) >= 2;
			string mapEventLocationLabel = GetMapEventLocationLabel(mapEvent);
			TrackNpcActionsFromMapEventSide(mapEvent, mapEvent.AttackerSide, (int)mapEvent.WinningSide == 1, isMajor, mapEventLocationLabel);
			TrackNpcActionsFromMapEventSide(mapEvent, mapEvent.DefenderSide, (int)mapEvent.WinningSide == 0, isMajor, mapEventLocationLabel);
		}
	}

	private void TrackNpcActionsFromMapEventSide(MapEvent mapEvent, MapEventSide side, bool won, bool isMajor, string locationLabel)
	{
		if (mapEvent == null || ((side != null) ? side.Parties : null) == null)
		{
			return;
		}
		foreach (MapEventParty item in (List<MapEventParty>)(object)side.Parties)
		{
			object obj;
			if (item == null)
			{
				obj = null;
			}
			else
			{
				PartyBase party = item.Party;
				obj = ((party != null) ? party.LeaderHero : null);
			}
			Hero val = (Hero)obj;
			if (ShouldTrackNpcActionHero(val))
			{
				string text = BuildMapEventNarrative(mapEvent, side, val, won, locationLabel);
				string stableKey = "mapevent:" + (((MBObjectBase)mapEvent).StringId ?? locationLabel) + ":" + won + ":" + ((MBObjectBase)val).StringId;
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("map_event", val);
				npcActionFacts.LocationText = locationLabel;
				npcActionFacts.Won = won;
				ApplySettlementFacts(npcActionFacts, mapEvent.MapEventSettlement, null, null, locationLabel);
				MapEventSide otherSide = side.OtherSide;
				AddRelatedFactionFacts(npcActionFacts, (otherSide != null) ? otherSide.MapFaction : null);
				if (isMajor)
				{
					RecordNpcMajorAction(val, text, stableKey, npcActionFacts);
				}
				RecordNpcRecentAction(val, text, stableKey, dedupeAcrossWindow: false, npcActionFacts);
				string text2 = BuildMapEventAftermathText(mapEvent, side, won, locationLabel);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("map_event_aftermath", val);
					npcActionFacts2.LocationText = locationLabel;
					npcActionFacts2.Won = won;
					ApplySettlementFacts(npcActionFacts2, mapEvent.MapEventSettlement, null, null, locationLabel);
					MapEventSide otherSide2 = side.OtherSide;
					AddRelatedFactionFacts(npcActionFacts2, (otherSide2 != null) ? otherSide2.MapFaction : null);
					RecordNpcRecentAction(val, text2, "mapevent_aftermath:" + (((MBObjectBase)mapEvent).StringId ?? locationLabel) + ":" + won + ":" + ((MBObjectBase)val).StringId, dedupeAcrossWindow: false, npcActionFacts2);
				}
			}
		}
	}

	private static string GetNearestSettlementNameForParty(MobileParty party)
	{
		Settlement val = ((party != null) ? party.BesiegedSettlement : null) ?? ResolveSiegeSettlement((party != null) ? party.SiegeEvent : null);
		if (val == null)
		{
			object obj;
			if (party == null)
			{
				obj = null;
			}
			else
			{
				Army army = party.Army;
				if (army == null)
				{
					obj = null;
				}
				else
				{
					MobileParty leaderParty = army.LeaderParty;
					obj = ((leaderParty != null) ? leaderParty.BesiegedSettlement : null);
				}
			}
			if (obj == null)
			{
				object siegeEvent;
				if (party == null)
				{
					siegeEvent = null;
				}
				else
				{
					Army army2 = party.Army;
					if (army2 == null)
					{
						siegeEvent = null;
					}
					else
					{
						MobileParty leaderParty2 = army2.LeaderParty;
						siegeEvent = ((leaderParty2 != null) ? leaderParty2.SiegeEvent : null);
					}
				}
				obj = ResolveSiegeSettlement((SiegeEvent)siegeEvent);
			}
			val = (Settlement)obj;
		}
		string text = ((val == null) ? null : ((object)val.Name)?.ToString());
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj2;
		if (party == null)
		{
			obj2 = null;
		}
		else
		{
			Army army3 = party.Army;
			if (army3 == null)
			{
				obj2 = null;
			}
			else
			{
				MobileParty leaderParty3 = army3.LeaderParty;
				if (leaderParty3 == null)
				{
					obj2 = null;
				}
				else
				{
					Settlement targetSettlement = leaderParty3.TargetSettlement;
					obj2 = ((targetSettlement == null) ? null : ((object)targetSettlement.Name)?.ToString());
				}
			}
		}
		text = (string)obj2;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj3;
		if (party == null)
		{
			obj3 = null;
		}
		else
		{
			Settlement targetSettlement2 = party.TargetSettlement;
			obj3 = ((targetSettlement2 == null) ? null : ((object)targetSettlement2.Name)?.ToString());
		}
		text = (string)obj3;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj4;
		if (party == null)
		{
			obj4 = null;
		}
		else
		{
			Settlement currentSettlement = party.CurrentSettlement;
			obj4 = ((currentSettlement == null) ? null : ((object)currentSettlement.Name)?.ToString());
		}
		text = (string)obj4;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj5;
		if (party == null)
		{
			obj5 = null;
		}
		else
		{
			Settlement lastVisitedSettlement = party.LastVisitedSettlement;
			obj5 = ((lastVisitedSettlement == null) ? null : ((object)lastVisitedSettlement.Name)?.ToString());
		}
		text = (string)obj5;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		object obj6;
		if (party == null)
		{
			obj6 = null;
		}
		else
		{
			Settlement besiegedSettlement = party.BesiegedSettlement;
			obj6 = ((besiegedSettlement == null) ? null : ((object)besiegedSettlement.Name)?.ToString());
		}
		text = (string)obj6;
		return string.IsNullOrWhiteSpace(text) ? "附近一带" : text.Trim();
	}

	private static string BuildRecentPartyBehaviorText(MobileParty party)
	{
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Expected I4, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected I4, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Invalid comparison between Unknown and I4
		if (party == null)
		{
			return "";
		}
		Army army = party.Army;
		MobileParty val = ((army != null) ? army.LeaderParty : null) ?? party;
		string nearestSettlementNameForParty = GetNearestSettlementNameForParty(party);
		if (party.Army != null)
		{
			string text = ((party.Army.LeaderParty == party) ? "你最近正率领" : "你最近正随");
			string armyDisplayName = GetArmyDisplayName(party.Army);
			AiBehavior? val2 = ((val != null) ? new AiBehavior?(val.DefaultBehavior) : ((AiBehavior?)null));
			AiBehavior? val3 = val2;
			if (val3.HasValue)
			{
				AiBehavior valueOrDefault = val3.GetValueOrDefault();
				switch (valueOrDefault - 2)
				{
				default:
					if ((int)valueOrDefault != 13)
					{
						if ((int)valueOrDefault != 15)
						{
							break;
						}
						return text + armyDisplayName + "守备" + nearestSettlementNameForParty + "。";
					}
					return text + armyDisplayName + "在" + nearestSettlementNameForParty + "附近巡逻。";
				case 3:
					return text + armyDisplayName + "围攻" + nearestSettlementNameForParty + "。";
				case 1:
					return text + armyDisplayName + "强攻" + nearestSettlementNameForParty + "。";
				case 0:
					return text + armyDisplayName + "前往" + nearestSettlementNameForParty + "。";
				case 4:
				{
					object obj;
					if (val == null)
					{
						obj = null;
					}
					else
					{
						MobileParty targetParty = val.TargetParty;
						if (targetParty == null)
						{
							obj = null;
						}
						else
						{
							Hero leaderHero = targetParty.LeaderHero;
							obj = ((leaderHero == null) ? null : ((object)leaderHero.Name)?.ToString());
						}
					}
					if (obj == null)
					{
						if (val == null)
						{
							obj = null;
						}
						else
						{
							MobileParty targetParty2 = val.TargetParty;
							obj = ((targetParty2 == null) ? null : ((object)targetParty2.Name)?.ToString());
						}
					}
					string text2 = (string)obj;
					return string.IsNullOrWhiteSpace(text2) ? (text + armyDisplayName + "追击一支部队。") : (text + armyDisplayName + "追击" + text2.Trim() + "。");
				}
				case 2:
					break;
				}
			}
			return text + armyDisplayName + "在" + nearestSettlementNameForParty + "一带行动。";
		}
		AiBehavior defaultBehavior = party.DefaultBehavior;
		AiBehavior val4 = defaultBehavior;
		switch (val4 - 2)
		{
		case 3:
			return "你最近在围攻" + nearestSettlementNameForParty + "。";
		case 1:
			return "你最近在强攻" + nearestSettlementNameForParty + "。";
		case 13:
			return "你最近在守备" + nearestSettlementNameForParty + "。";
		case 2:
			return "你最近在袭扰" + nearestSettlementNameForParty + "。";
		case 11:
			return "你最近在" + nearestSettlementNameForParty + "附近巡逻。";
		case 0:
			return "你最近正前往" + nearestSettlementNameForParty + "。";
		case 4:
		{
			MobileParty targetParty5 = party.TargetParty;
			object obj3;
			if (targetParty5 == null)
			{
				obj3 = null;
			}
			else
			{
				Hero leaderHero3 = targetParty5.LeaderHero;
				obj3 = ((leaderHero3 == null) ? null : ((object)leaderHero3.Name)?.ToString());
			}
			if (obj3 == null)
			{
				MobileParty targetParty6 = party.TargetParty;
				obj3 = ((targetParty6 == null) ? null : ((object)targetParty6.Name)?.ToString());
			}
			string text4 = (string)obj3;
			return string.IsNullOrWhiteSpace(text4) ? "你最近在追击一支部队。" : ("你最近在追击" + text4.Trim() + "。");
		}
		case 12:
		{
			MobileParty targetParty3 = party.TargetParty;
			object obj2;
			if (targetParty3 == null)
			{
				obj2 = null;
			}
			else
			{
				Hero leaderHero2 = targetParty3.LeaderHero;
				obj2 = ((leaderHero2 == null) ? null : ((object)leaderHero2.Name)?.ToString());
			}
			if (obj2 == null)
			{
				MobileParty targetParty4 = party.TargetParty;
				obj2 = ((targetParty4 == null) ? null : ((object)targetParty4.Name)?.ToString());
			}
			string text3 = (string)obj2;
			return string.IsNullOrWhiteSpace(text3) ? "你最近在护送一支部队。" : ("你最近在护送" + text3.Trim() + "。");
		}
		default:
			return "";
		}
	}

	private static string BuildRecentPartyBehaviorStableKey(MobileParty party)
	{
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		if (party == null)
		{
			return "";
		}
		Army army = party.Army;
		MobileParty val = ((army != null) ? army.LeaderParty : null) ?? party;
		string nearestSettlementNameForParty = GetNearestSettlementNameForParty(party);
		object obj;
		if (val == null)
		{
			obj = null;
		}
		else
		{
			MobileParty targetParty = val.TargetParty;
			obj = ((targetParty != null) ? ((MBObjectBase)targetParty).StringId : null);
		}
		if (obj == null)
		{
			if (val == null)
			{
				obj = null;
			}
			else
			{
				MobileParty targetParty2 = val.TargetParty;
				obj = ((targetParty2 == null) ? null : ((object)targetParty2.Name)?.ToString());
			}
			if (obj == null)
			{
				obj = "";
			}
		}
		string text = (string)obj;
		string text2 = ((party.Army != null) ? GetArmyDisplayName(party.Army) : "");
		if (party.Army != null)
		{
			return "daily_behavior:army:" + ((object)((val != null) ? new AiBehavior?(val.DefaultBehavior) : ((AiBehavior?)null)).GetValueOrDefault()/*cast due to .constrained prefix*/).ToString() + ":" + nearestSettlementNameForParty + ":" + text + ":" + text2;
		}
		return "daily_behavior:" + ((object)party.DefaultBehavior/*cast due to .constrained prefix*/).ToString() + ":" + nearestSettlementNameForParty + ":" + text + ":" + text2;
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
		List<NpcActionEntry> list = SanitizeNpcActionEntries(value, recentOnly);
		if (list.Count <= 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = int.MinValue;
		string a = null;
		foreach (NpcActionEntry item in list)
		{
			string text = ((!string.IsNullOrWhiteSpace(item.GameDate)) ? item.GameDate.Trim() : ("第 " + item.Day + " 日"));
			if (item.Day != num || !string.Equals(a, text, StringComparison.Ordinal))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine("—— " + text + " ——");
				num = item.Day;
				a = text;
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
		string text2 = ((hero == null) ? null : ((object)hero.Name)?.ToString()?.Trim());
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
		return (actionKind ?? "").Trim().ToLowerInvariant() switch
		{
			"army_create" => "组建军团的行动", 
			"army_gather" => "军团集结行动", 
			"army_disperse" => "军团解散行动", 
			"army_join" => "加入军团的行动", 
			"army_leave" => "离开军团的行动", 
			"siege_start_attack" => "参与围攻的行动", 
			"siege_start_defend" => "参与守城的行动", 
			"siege_end_attack" => "围城结束后的攻方行动", 
			"siege_end_defend" => "围城结束后的守方行动", 
			"siege_join" => "加入围城的行动", 
			"siege_leave" => "离开围城的行动", 
			"siege_complete" => "围城结果事件", 
			"daily_behavior" => "近期行军动向", 
			"map_event" => "战场交锋", 
			"map_event_aftermath" => "战后余波", 
			"marriage" => "联姻事件", 
			"clan_changed_kingdom" => "家族更换效忠对象的事件", 
			"clan_defected" => "家族叛逃事件", 
			"kingdom_decision_concluded" => "王国决议事件", 
			"ruling_clan_changed" => "执政家族变更事件", 
			"settlement_owner_changed_gain" => "定居点归属增加事件", 
			"settlement_owner_changed_loss" => "定居点归属失去事件", 
			"settlement_owner_changed_capture" => "定居点易主事件", 
			"hero_killed" => "英雄死亡事件", 
			"clan_member_killed" => "家族成员死亡事件", 
			"prisoner_taken_captor" => "俘获领主事件", 
			"prisoner_taken_prisoner" => "被俘事件", 
			"prisoner_released_captor" => "囚犯获释事件", 
			"prisoner_released_prisoner" => "结束囚禁事件", 
			"birth" => "家族新生事件", 
			"clan_leader_changed" => "家族族长更替事件", 
			_ => "", 
		};
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
			Settlement val = ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Settlement x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
			return (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
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
			Hero val = ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Hero x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
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
			Clan val = ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Clan x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
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
			Kingdom val = ((IEnumerable<Kingdom>)Kingdom.All).FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (((val == null) ? null : ((object)val.Name)?.ToString()) ?? "").Trim();
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
		Dictionary<string, string> tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["majorActionSummary"] = text ?? "" };
		return AIConfigHandler.ResolveRuleRuntimeText("npc_major_actions", stateKey, forConstraint: false, tokens);
	}

	private string BuildNpcRecentActionsRuntimeInstruction(Hero hero)
	{
		string text = BuildNpcActionSummary(hero, recentOnly: true);
		string stateKey = (string.IsNullOrWhiteSpace(text) ? "no_data" : "has_data");
		Dictionary<string, string> tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["recentActionSummary"] = text ?? "" };
		return AIConfigHandler.ResolveRuleRuntimeText("npc_recent_actions", stateKey, forConstraint: false, tokens);
	}

	private string BuildNpcActionsRuntimeConstraintHint(Hero hero, bool recentOnly)
	{
		string value = BuildNpcActionSummary(hero, recentOnly);
		if (!string.IsNullOrWhiteSpace(value))
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
				Settlement currentSettlement = hero.CurrentSettlement;
				string text2 = (((currentSettlement == null) ? null : ((object)currentSettlement.Name)?.ToString()) ?? "").Trim();
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
			return string.IsNullOrWhiteSpace(text) ? "" : text;
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
				Clan playerClan = Clan.PlayerClan;
				num = ((playerClan != null) ? playerClan.Tier : 0);
			}
			catch
			{
			}
			if (num <= 0)
			{
				try
				{
					Hero mainHero = Hero.MainHero;
					int? obj2;
					if (mainHero == null)
					{
						obj2 = null;
					}
					else
					{
						Clan clan = mainHero.Clan;
						obj2 = ((clan != null) ? new int?(clan.Tier) : ((int?)null));
					}
					int? num2 = obj2;
					num = num2.GetValueOrDefault();
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
					if (flag)
					{
						try
						{
							_shownRecordStorage[shownRecord.Key] = JsonConvert.SerializeObject((object)shownRecord.Value);
						}
						catch (Exception ex)
						{
							Logger.Log("TradeShown", "[ERROR] Serialize shown record for " + shownRecord.Key + ": " + ex.Message);
						}
					}
				}
				Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_shownRecordStorage);
				dataStore.SyncData<Dictionary<string, string>>("_shownRecords_v1", ref dictionary);
				_dialogueHistoryStorage.Clear();
				foreach (KeyValuePair<string, List<DialogueDay>> item in _dialogueHistory)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						try
						{
							string value = JsonConvert.SerializeObject((object)item.Value);
							_dialogueHistoryStorage[item.Key] = value;
						}
						catch (Exception ex2)
						{
							Logger.Log("DialogueHistory", "[ERROR] Serialize history for " + item.Key + ": " + ex2.Message);
						}
					}
				}
				Dictionary<string, string> dictionary2 = CampaignSaveChunkHelper.FlattenStringDictionary(_dialogueHistoryStorage);
				dataStore.SyncData<Dictionary<string, string>>("_dialogueHistory_v2", ref dictionary2);
				_npcMajorActionStorage.Clear();
				foreach (KeyValuePair<string, List<NpcActionEntry>> npcMajorAction in _npcMajorActions)
				{
					if (!string.IsNullOrEmpty(npcMajorAction.Key) && npcMajorAction.Value != null && npcMajorAction.Value.Count > 0)
					{
						try
						{
							string value2 = JsonConvert.SerializeObject((object)npcMajorAction.Value);
							_npcMajorActionStorage[npcMajorAction.Key] = value2;
						}
						catch (Exception ex3)
						{
							Logger.Log("NpcAction", "[ERROR] Serialize major actions for " + npcMajorAction.Key + ": " + ex3.Message);
						}
					}
				}
				Dictionary<string, string> dictionary3 = CampaignSaveChunkHelper.FlattenStringDictionary(_npcMajorActionStorage);
				dataStore.SyncData<Dictionary<string, string>>("_npcMajorActions_v1", ref dictionary3);
				_npcRecentActionStorage.Clear();
				foreach (KeyValuePair<string, List<NpcActionEntry>> npcRecentAction in _npcRecentActions)
				{
					if (!string.IsNullOrEmpty(npcRecentAction.Key) && npcRecentAction.Value != null && npcRecentAction.Value.Count > 0)
					{
						try
						{
							string value3 = JsonConvert.SerializeObject((object)npcRecentAction.Value);
							_npcRecentActionStorage[npcRecentAction.Key] = value3;
						}
						catch (Exception ex4)
						{
							Logger.Log("NpcAction", "[ERROR] Serialize recent actions for " + npcRecentAction.Key + ": " + ex4.Message);
						}
					}
				}
				Dictionary<string, string> dictionary4 = CampaignSaveChunkHelper.FlattenStringDictionary(_npcRecentActionStorage);
				dataStore.SyncData<Dictionary<string, string>>("_npcRecentActions_v1", ref dictionary4);
				dataStore.SyncData<int>("_npcActionGlobalOrderCounter_v1", ref _npcActionGlobalOrderCounter);
				_npcPersonaProfileStorage.Clear();
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile2 in _npcPersonaProfiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile2.Key) && npcPersonaProfile2.Value != null)
					{
						try
						{
							string value4 = JsonConvert.SerializeObject((object)npcPersonaProfile2.Value);
							_npcPersonaProfileStorage[npcPersonaProfile2.Key] = value4;
						}
						catch (Exception ex5)
						{
							Logger.Log("NpcPersona", "[ERROR] Serialize profile for " + npcPersonaProfile2.Key + ": " + ex5.Message);
						}
					}
				}
				Dictionary<string, string> dictionary5 = CampaignSaveChunkHelper.FlattenStringDictionary(_npcPersonaProfileStorage);
				dataStore.SyncData<Dictionary<string, string>>("_npcPersonaProfiles_v1", ref dictionary5);
				_eventKingdomOpeningSummaryStorage.Clear();
				foreach (KeyValuePair<string, string> eventKingdomOpeningSummary in _eventKingdomOpeningSummaries)
				{
					string text = (eventKingdomOpeningSummary.Key ?? "").Trim();
					string value5 = (eventKingdomOpeningSummary.Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value5))
					{
						_eventKingdomOpeningSummaryStorage[text] = value5;
					}
				}
				Dictionary<string, string> dictionary6 = CampaignSaveChunkHelper.FlattenStringDictionary(_eventKingdomOpeningSummaryStorage);
				dataStore.SyncData<Dictionary<string, string>>("_eventKingdomOpeningSummaries_v1", ref dictionary6);
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventWorldOpeningSummary_v1", _eventWorldOpeningSummary ?? "", "EventOpeningSummary");
				try
				{
					_eventRecordJsonStorage = JsonConvert.SerializeObject((object)SanitizeEventRecordEntries(_eventRecordEntries));
				}
				catch (Exception ex6)
				{
					_eventRecordJsonStorage = "[]";
					Logger.Log("EventRecord", "[ERROR] Serialize event records failed: " + ex6.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventRecordEntries_v1", _eventRecordJsonStorage ?? "[]", "EventRecord");
				try
				{
					_eventSourceMaterialJsonStorage = JsonConvert.SerializeObject((object)SanitizeEventSourceMaterials(_eventSourceMaterials));
				}
				catch (Exception ex7)
				{
					_eventSourceMaterialJsonStorage = "[]";
					Logger.Log("EventMaterial", "[ERROR] Serialize event source materials failed: " + ex7.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_eventSourceMaterials_v1", _eventSourceMaterialJsonStorage ?? "[]", "EventMaterial");
				_kingdomStabilityStorage.Clear();
				foreach (KeyValuePair<string, int> kingdomStabilityValue in _kingdomStabilityValues)
				{
					string text2 = (kingdomStabilityValue.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						_kingdomStabilityStorage[text2] = ClampKingdomStabilityValue(kingdomStabilityValue.Value).ToString();
					}
				}
				Dictionary<string, string> dictionary7 = CampaignSaveChunkHelper.FlattenStringDictionary(_kingdomStabilityStorage);
				dataStore.SyncData<Dictionary<string, string>>("_kingdomStability_v1", ref dictionary7);
				_kingdomStabilityRelationOffsetStorage.Clear();
				foreach (KeyValuePair<string, int> kingdomStabilityRelationAppliedOffset in _kingdomStabilityRelationAppliedOffsets)
				{
					string text3 = (kingdomStabilityRelationAppliedOffset.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						_kingdomStabilityRelationOffsetStorage[text3] = kingdomStabilityRelationAppliedOffset.Value.ToString();
					}
				}
				Dictionary<string, string> dictionary8 = CampaignSaveChunkHelper.FlattenStringDictionary(_kingdomStabilityRelationOffsetStorage);
				dataStore.SyncData<Dictionary<string, string>>("_kingdomStabilityRelationOffsets_v1", ref dictionary8);
				_weeklyReportAppliedStabilityDeltaStorage.Clear();
				foreach (KeyValuePair<string, int> weeklyReportAppliedStabilityDelta in _weeklyReportAppliedStabilityDeltas)
				{
					string text4 = (weeklyReportAppliedStabilityDelta.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						_weeklyReportAppliedStabilityDeltaStorage[text4] = weeklyReportAppliedStabilityDelta.Value.ToString();
					}
				}
				Dictionary<string, string> dictionary9 = CampaignSaveChunkHelper.FlattenStringDictionary(_weeklyReportAppliedStabilityDeltaStorage);
				dataStore.SyncData<Dictionary<string, string>>("_weeklyReportAppliedStabilityDeltas_v1", ref dictionary9);
				dataStore.SyncData<int>("_lastAutoGeneratedWeeklyReportWeek_v1", ref _lastAutoGeneratedWeeklyReportWeek);
				dataStore.SyncData<int>("_lastProcessedKingdomRebellionWeek_v1", ref _lastProcessedKingdomRebellionWeek);
				try
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				catch (Exception ex8)
				{
					_voiceMappingJsonStorage = "";
					Logger.Log("VoiceMapper", "[ERROR] Serialize voice mapping for save failed: " + ex8.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_voiceMapping_v1", _voiceMappingJsonStorage, "VoiceMapper");
				_voiceMappingExportFolderStorage = VoiceMapper.GetPreferredExportFolder() ?? "";
				dataStore.SyncData<string>("_voiceMapping_export_folder_v1", ref _voiceMappingExportFolderStorage);
				try
				{
					_unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson() ?? "";
				}
				catch (Exception ex9)
				{
					_unnamedPersonaJsonStorage = "";
					Logger.Log("UnnamedPersona", "[ERROR] Serialize unnamed persona for save failed: " + ex9.Message);
				}
				CampaignSaveChunkHelper.SaveChunkedString(dataStore, "_unnamed_persona_v1", _unnamedPersonaJsonStorage, "UnnamedPersona");
				SyncPatienceData(dataStore);
				return;
			}
			_shownRecords.Clear();
			_shownRecordStorage.Clear();
			Dictionary<string, string> stored = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_shownRecords_v1", ref stored);
			_shownRecordStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored, "TradeShown");
			if (_shownRecordStorage != null)
			{
				foreach (KeyValuePair<string, string> item2 in _shownRecordStorage)
				{
					if (string.IsNullOrWhiteSpace(item2.Key) || string.IsNullOrWhiteSpace(item2.Value))
					{
						continue;
					}
					try
					{
						HeroShownRecord heroShownRecord = JsonConvert.DeserializeObject<HeroShownRecord>(item2.Value);
						if (heroShownRecord == null)
						{
							continue;
						}
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
						_shownRecords[NormalizeShownRecordKey(item2.Key)] = heroShownRecord;
					}
					catch (Exception ex10)
					{
						Logger.Log("TradeShown", "[ERROR] Deserialize shown record for " + item2.Key + ": " + ex10.Message);
					}
				}
			}
			_dialogueHistory.Clear();
			_dialogueHistoryStorage.Clear();
			Dictionary<string, string> stored2 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_dialogueHistory_v2", ref stored2);
			_dialogueHistoryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored2, "DialogueHistory");
			if (_dialogueHistoryStorage != null)
			{
				foreach (KeyValuePair<string, string> item3 in _dialogueHistoryStorage)
				{
					if (string.IsNullOrEmpty(item3.Key) || string.IsNullOrEmpty(item3.Value))
					{
						continue;
					}
					try
					{
						List<DialogueDay> list = JsonConvert.DeserializeObject<List<DialogueDay>>(item3.Value);
						if (list != null)
						{
							_dialogueHistory[item3.Key] = list;
						}
					}
					catch (Exception ex11)
					{
						Logger.Log("DialogueHistory", "[ERROR] Deserialize history for " + item3.Key + ": " + ex11.Message);
					}
				}
			}
			_npcMajorActions.Clear();
			_npcMajorActionStorage.Clear();
			Dictionary<string, string> stored3 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_npcMajorActions_v1", ref stored3);
			_npcMajorActionStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored3, "NpcAction");
			if (_npcMajorActionStorage != null)
			{
				foreach (KeyValuePair<string, string> item4 in _npcMajorActionStorage)
				{
					if (string.IsNullOrEmpty(item4.Key) || string.IsNullOrEmpty(item4.Value))
					{
						continue;
					}
					try
					{
						List<NpcActionEntry> source = JsonConvert.DeserializeObject<List<NpcActionEntry>>(item4.Value) ?? new List<NpcActionEntry>();
						source = SanitizeNpcActionEntries(source, keepOnlyRecentWindow: false);
						if (source.Count > 0)
						{
							_npcMajorActions[item4.Key] = source;
						}
					}
					catch (Exception ex12)
					{
						Logger.Log("NpcAction", "[ERROR] Deserialize major actions for " + item4.Key + ": " + ex12.Message);
					}
				}
			}
			_npcRecentActions.Clear();
			_npcRecentActionStorage.Clear();
			Dictionary<string, string> stored4 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_npcRecentActions_v1", ref stored4);
			_npcRecentActionStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored4, "NpcAction");
			if (_npcRecentActionStorage != null)
			{
				foreach (KeyValuePair<string, string> item5 in _npcRecentActionStorage)
				{
					if (string.IsNullOrEmpty(item5.Key) || string.IsNullOrEmpty(item5.Value))
					{
						continue;
					}
					try
					{
						List<NpcActionEntry> source2 = JsonConvert.DeserializeObject<List<NpcActionEntry>>(item5.Value) ?? new List<NpcActionEntry>();
						source2 = SanitizeNpcActionEntries(source2, keepOnlyRecentWindow: true);
						if (source2.Count > 0)
						{
							_npcRecentActions[item5.Key] = source2;
						}
					}
					catch (Exception ex13)
					{
						Logger.Log("NpcAction", "[ERROR] Deserialize recent actions for " + item5.Key + ": " + ex13.Message);
					}
				}
			}
			_npcPersonaProfiles.Clear();
			_npcPersonaProfileStorage.Clear();
			Dictionary<string, string> stored5 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_npcPersonaProfiles_v1", ref stored5);
			_npcPersonaProfileStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored5, "NpcPersona");
			if (_npcPersonaProfileStorage != null)
			{
				foreach (KeyValuePair<string, string> item6 in _npcPersonaProfileStorage)
				{
					if (string.IsNullOrEmpty(item6.Key) || string.IsNullOrEmpty(item6.Value))
					{
						continue;
					}
					try
					{
						NpcPersonaProfile npcPersonaProfile = JsonConvert.DeserializeObject<NpcPersonaProfile>(item6.Value);
						if (npcPersonaProfile != null)
						{
							_npcPersonaProfiles[item6.Key] = npcPersonaProfile;
						}
					}
					catch (Exception ex14)
					{
						Logger.Log("NpcPersona", "[ERROR] Deserialize profile for " + item6.Key + ": " + ex14.Message);
					}
				}
			}
			dataStore.SyncData<int>("_npcActionGlobalOrderCounter_v1", ref _npcActionGlobalOrderCounter);
			NormalizeNpcActionSequences(_npcMajorActions);
			NormalizeNpcActionSequences(_npcRecentActions);
			_npcActionGlobalOrderCounter = Math.Max(_npcActionGlobalOrderCounter, GetMaxNpcActionSequence(_npcMajorActions, _npcRecentActions, _eventSourceMaterials));
			_eventKingdomOpeningSummaries.Clear();
			_eventKingdomOpeningSummaryStorage.Clear();
			Dictionary<string, string> stored6 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_eventKingdomOpeningSummaries_v1", ref stored6);
			_eventKingdomOpeningSummaryStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored6, "EventOpeningSummary");
			if (_eventKingdomOpeningSummaryStorage != null)
			{
				foreach (KeyValuePair<string, string> item7 in _eventKingdomOpeningSummaryStorage)
				{
					string text5 = (item7.Key ?? "").Trim();
					string value6 = (item7.Value ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(value6))
					{
						_eventKingdomOpeningSummaries[text5] = value6;
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
					List<EventRecordEntry> source3 = JsonConvert.DeserializeObject<List<EventRecordEntry>>(_eventRecordJsonStorage) ?? new List<EventRecordEntry>();
					_eventRecordEntries = SanitizeEventRecordEntries(source3);
				}
				catch (Exception ex15)
				{
					Logger.Log("EventRecord", "[ERROR] Deserialize event records failed: " + ex15.Message);
					_eventRecordEntries = new List<EventRecordEntry>();
				}
			}
			_eventSourceMaterials.Clear();
			_eventSourceMaterialJsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_eventSourceMaterials_v1", "EventMaterial") ?? "";
			if (!string.IsNullOrWhiteSpace(_eventSourceMaterialJsonStorage))
			{
				try
				{
					List<EventSourceMaterialEntry> source4 = JsonConvert.DeserializeObject<List<EventSourceMaterialEntry>>(_eventSourceMaterialJsonStorage) ?? new List<EventSourceMaterialEntry>();
					_eventSourceMaterials = SanitizeEventSourceMaterials(source4);
				}
				catch (Exception ex16)
				{
					Logger.Log("EventMaterial", "[ERROR] Deserialize event source materials failed: " + ex16.Message);
					_eventSourceMaterials = new List<EventSourceMaterialEntry>();
				}
			}
			dataStore.SyncData<int>("_lastAutoGeneratedWeeklyReportWeek_v1", ref _lastAutoGeneratedWeeklyReportWeek);
			_kingdomStabilityValues.Clear();
			_kingdomStabilityStorage.Clear();
			Dictionary<string, string> stored7 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_kingdomStability_v1", ref stored7);
			_kingdomStabilityStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored7, "KingdomStability");
			if (_kingdomStabilityStorage != null)
			{
				foreach (KeyValuePair<string, string> item8 in _kingdomStabilityStorage)
				{
					string text6 = (item8.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6) && int.TryParse((item8.Value ?? "").Trim(), out var result))
					{
						_kingdomStabilityValues[text6] = ClampKingdomStabilityValue(result);
					}
				}
			}
			_kingdomStabilityRelationAppliedOffsets.Clear();
			_kingdomStabilityRelationOffsetStorage.Clear();
			Dictionary<string, string> stored8 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_kingdomStabilityRelationOffsets_v1", ref stored8);
			_kingdomStabilityRelationOffsetStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored8, "KingdomStabilityRelation");
			if (_kingdomStabilityRelationOffsetStorage != null)
			{
				foreach (KeyValuePair<string, string> item9 in _kingdomStabilityRelationOffsetStorage)
				{
					string text7 = (item9.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text7) && int.TryParse((item9.Value ?? "").Trim(), out var result2))
					{
						_kingdomStabilityRelationAppliedOffsets[text7] = MBMath.ClampInt(result2, -100, 100);
					}
				}
			}
			_weeklyReportAppliedStabilityDeltas.Clear();
			_weeklyReportAppliedStabilityDeltaStorage.Clear();
			Dictionary<string, string> stored9 = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_weeklyReportAppliedStabilityDeltas_v1", ref stored9);
			_weeklyReportAppliedStabilityDeltaStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored9, "WeeklyReportStability");
			if (_weeklyReportAppliedStabilityDeltaStorage != null)
			{
				foreach (KeyValuePair<string, string> item10 in _weeklyReportAppliedStabilityDeltaStorage)
				{
					string text8 = (item10.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text8) && int.TryParse((item10.Value ?? "").Trim(), out var result3))
					{
						_weeklyReportAppliedStabilityDeltas[text8] = result3;
					}
				}
			}
			dataStore.SyncData<int>("_lastProcessedKingdomRebellionWeek_v1", ref _lastProcessedKingdomRebellionWeek);
			_voiceMappingExportFolderStorage = "";
			dataStore.SyncData<string>("_voiceMapping_export_folder_v1", ref _voiceMappingExportFolderStorage);
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
				catch (Exception ex17)
				{
					Logger.Log("VoiceMapper", "[ERROR] Restore voice mapping from save failed: " + ex17.Message);
				}
			}
			_unnamedPersonaJsonStorage = CampaignSaveChunkHelper.LoadChunkedString(dataStore, "_unnamed_persona_v1", "UnnamedPersona");
			try
			{
				ShoutUtils.ImportUnnamedPersonaStateJson(_unnamedPersonaJsonStorage);
			}
			catch (Exception ex18)
			{
				Logger.Log("UnnamedPersona", "[ERROR] Restore unnamed persona from save failed: " + ex18.Message);
			}
			SyncPatienceData(dataStore);
		}
		catch (Exception ex19)
		{
			Logger.Log("DialogueHistory", "[ERROR] SyncData v2 failed: " + ex19.ToString());
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
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_006b: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_009d: Expected O, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00cf: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected O, but got Unknown
		//IL_0101: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_0133: Expected O, but got Unknown
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Expected O, but got Unknown
		//IL_0165: Expected O, but got Unknown
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Expected O, but got Unknown
		//IL_0197: Expected O, but got Unknown
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Expected O, but got Unknown
		//IL_01c9: Expected O, but got Unknown
		AIConfigHandler.ReloadConfig();
		AIConfigHandler.TryStartBackgroundSemanticWarmup("session_launch");
		TryHookOverlayQuickTalkDisable();
		starter.AddGameMenu("AnimusForge_dev_root", "{=!}开发者工具", new OnInitDelegate(DevRootMenuInit), (MenuOverlayType)3, (MenuFlags)0, (object)null);
		starter.AddGameMenuOption("town", "AnimusForge_dev_root_entry", "【开发】数据管理", new OnConditionDelegate(DevRootEntryCondition), new OnConsequenceDelegate(DevRootEntryConsequence), false, 99, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_hero", "HeroNPC编辑（领主/流浪者/同伴）", new OnConditionDelegate(DevRootSubOptionCondition), new OnConsequenceDelegate(DevRootHeroOptionConsequence), false, -1, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_nonhero", "非heroNPC编辑（士兵/平民/无名/无姓NPC）", new OnConditionDelegate(DevRootSubOptionCondition), new OnConsequenceDelegate(DevRootNonHeroOptionConsequence), false, -1, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_knowledge", "知识编辑", new OnConditionDelegate(DevRootSubOptionCondition), new OnConsequenceDelegate(DevRootKnowledgeOptionConsequence), false, -1, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_event", "事件编辑", new OnConditionDelegate(DevRootSubOptionCondition), new OnConsequenceDelegate(DevRootEventOptionConsequence), false, -1, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_all", "全部导出/导入", new OnConditionDelegate(DevRootSubOptionCondition), new OnConsequenceDelegate(DevRootAllOptionConsequence), false, -1, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_voice", "声音映射管理（VoiceMapping）", new OnConditionDelegate(DevRootSubOptionCondition), new OnConsequenceDelegate(DevRootVoiceMappingOptionConsequence), false, -1, false, (object)null);
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_back", "返回", new OnConditionDelegate(DevRootBackCondition), new OnConsequenceDelegate(DevRootBackConsequence), true, -1, false, (object)null);
	}

	private void TryHookOverlayQuickTalkDisable()
	{
		if (_overlayQuickTalkDisableHooked)
		{
			return;
		}
		try
		{
			Game current = Game.Current;
			EventManager val = ((current != null) ? current.EventManager : null);
			if (val != null)
			{
				val.RegisterEvent<SettlementOverylayQuickTalkPermissionEvent>((Action<SettlementOverylayQuickTalkPermissionEvent>)OnSettlementOverlayQuickTalkPermission);
				_overlayQuickTalkDisableHooked = true;
			}
		}
		catch
		{
		}
	}

	private void OnSettlementOverlayQuickTalkPermission(SettlementOverylayQuickTalkPermissionEvent e)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		try
		{
			if (e != null && e.IsTalkAvailable != null)
			{
				Hero heroToTalkTo = e.HeroToTalkTo;
				if (heroToTalkTo != null && !heroToTalkTo.IsPlayerCompanion && (heroToTalkTo.IsLord || heroToTalkTo.IsNotable))
				{
					e.IsTalkAvailable(arg1: false, new TextObject("该交谈选项已被模组禁用，请使用造访进入场景后再互动。", (Dictionary<string, object>)null));
				}
			}
		}
		catch
		{
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (mission == null || Mission.Current == null)
		{
			return;
		}
		try
		{
			string arg = Mission.Current.SceneName ?? "Unknown";
			MobileParty mainParty = MobileParty.MainParty;
			object obj;
			if (mainParty == null)
			{
				obj = null;
			}
			else
			{
				Settlement currentSettlement = mainParty.CurrentSettlement;
				obj = ((currentSettlement == null) ? null : ((object)currentSettlement.Name)?.ToString());
			}
			if (obj == null)
			{
				obj = "";
			}
			string arg2 = (string)obj;
			MissionMode mode = Mission.Current.Mode;
			Logger.Log("SceneInfo", $"[MyBehavior.OnMissionStarted] SceneName={arg}, Mode={mode}, Settlement={arg2}");
		}
		catch
		{
		}
	}

	private NpcPersonaProfile GetNpcPersonaProfile(Hero npc, bool createIfMissing)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = ((MBObjectBase)npc).StringId;
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
		string stringId = ((MBObjectBase)npc).StringId;
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
			string stringId = ((MBObjectBase)hero).StringId;
			if (!string.IsNullOrEmpty(stringId) && _npcPersonaProfiles != null && _npcPersonaProfiles.TryGetValue(stringId, out var value) && value != null)
			{
				personality = value.Personality ?? "";
				background = value.Background ?? "";
			}
		}
	}

	private bool NeedsNpcPersonaGeneration(Hero hero)
	{
		if (hero == null || string.IsNullOrWhiteSpace(((MBObjectBase)hero).StringId))
		{
			return false;
		}
		GetNpcPersonaStrings(hero, out var personality, out var background);
		return string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(background);
	}

	private bool IsNpcPersonaGenerationInFlight(Hero hero)
	{
		string text = (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim();
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
		string stringId = ((MBObjectBase)hero).StringId;
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
				stringBuilder.AppendLine("文化: " + ((MBObjectBase)hero.Culture).StringId);
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null)
			{
				stringBuilder.AppendLine($"家族: {hero.Clan.Name} (StringId={((MBObjectBase)hero.Clan).StringId})");
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null && hero.Clan.Kingdom != null)
			{
				stringBuilder.AppendLine($"势力: {hero.Clan.Kingdom.Name} (StringId={((MBObjectBase)hero.Clan.Kingdom).StringId})");
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			Kingdom val = ((clan != null) ? clan.Kingdom : null);
			if (val != null && val.Leader != null)
			{
				stringBuilder.AppendLine($"势力领袖: {val.Leader.Name} (HeroId={((MBObjectBase)val.Leader).StringId})");
				if (hero.IsFactionLeader)
				{
					stringBuilder.AppendLine("效忠: 你本人即该势力领袖");
				}
				else
				{
					stringBuilder.AppendLine($"效忠: {val.Leader.Name}");
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
			var list = (from x in (from sk in (IEnumerable<SkillObject>)Skills.All
					select new
					{
						Skill = sk,
						Value = hero.GetSkillValue(sk)
					} into x
					orderby x.Value descending, ((MBObjectBase)x.Skill).StringId
					select x).Take(8)
				where x.Value > 0
				select x).ToList();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("技能(最高8项): " + string.Join(", ", list.Select(x => $"{((MBObjectBase)x.Skill).StringId}={x.Value}")));
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
				list2.Add("父亲:" + (object)hero.Father.Name);
			}
			if (hero.Mother != null)
			{
				list2.Add("母亲:" + (object)hero.Mother.Name);
			}
			if (hero.Spouse != null)
			{
				list2.Add("配偶:" + (object)hero.Spouse.Name);
			}
			try
			{
				List<string> list3 = (from c in ((IEnumerable<Hero>)hero.Children)?.Take(6)
					select (c == null) ? null : ((object)c.Name)?.ToString() into n
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
		if (1 == 0)
		{
		}
		string result = num switch
		{
			1 => "小有名气", 
			2 => "崭露新贵", 
			3 => "声名清贵", 
			4 => "门第高华", 
			5 => "威权显赫", 
			_ => "贵不可言", 
		};
		if (1 == 0)
		{
		}
		return result;
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
		string text = ((targetHero != null) ? ((MBObjectBase)targetHero).StringId : null) ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			text = ((targetCharacter != null) ? ((MBObjectBase)targetCharacter).StringId : null) ?? "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			object obj;
			if (targetCharacter == null)
			{
				obj = null;
			}
			else
			{
				Hero heroObject = targetCharacter.HeroObject;
				obj = ((heroObject != null) ? ((MBObjectBase)heroObject).StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			text = (string)obj;
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
		}
		else if (duel || reward || loan)
		{
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
	}

	public void OnEngineTick()
	{
		try
		{
			ProcessPendingWeeklyReportManualRetryResult();
			ProcessWeeklyReportUiResume();
			ProcessPendingDevForcedKingdomRebellionResult();
		}
		catch
		{
		}
	}

	private void ProcessPendingWeeklyReportManualRetryResult()
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		if (!_pendingWeeklyReportManualRetryResult)
		{
			return;
		}
		bool pendingWeeklyReportManualRetrySucceeded = _pendingWeeklyReportManualRetrySucceeded;
		string text = (_pendingWeeklyReportManualRetryMessage ?? "").Trim();
		WeeklyReportRetryContext pendingWeeklyReportManualRetryContext = _pendingWeeklyReportManualRetryContext;
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
		if (pendingWeeklyReportManualRetryContext != null)
		{
			_weeklyReportRetryContext = pendingWeeklyReportManualRetryContext;
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
		if (_weeklyReportReopenAfterApiConfig && _weeklyReportRetryContext != null && !_pendingWeeklyReportManualRetryResult && !InformationManager.IsAnyInquiryActive() && DateTime.UtcNow.Ticks >= _weeklyReportReopenAfterApiConfigUtcTicks)
		{
			_weeklyReportReopenAfterApiConfig = false;
			ShowWeeklyReportFailurePopup(ignoreDelay: true);
		}
		if (_weeklyReportRetryContext != null && !_pendingWeeklyReportManualRetryResult && (_weeklyReportUiStage == WeeklyReportUiStage.Failure || _weeklyReportUiStage == WeeklyReportUiStage.RetryProgress) && !InformationManager.IsAnyInquiryActive() && DateTime.UtcNow.Ticks >= _weeklyReportUiResumeAfterUtcTicks)
		{
			if (_weeklyReportUiStage == WeeklyReportUiStage.RetryProgress && _weeklyReportManualRetryInProgress)
			{
				ShowWeeklyReportRetryProgressPopup();
			}
			else if (_weeklyReportUiStage == WeeklyReportUiStage.Failure)
			{
				ShowWeeklyReportFailurePopup(ignoreDelay: true);
			}
		}
	}

	private void ProcessPendingDevForcedKingdomRebellionResult()
	{
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Expected O, but got Unknown
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
		Clan val = FindClanById(pendingDevForcedKingdomRebellionContext.ClanId);
		List<Clan> list = (from x in (pendingDevForcedKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(FindClanById)
			where x != null
			select x).ToList();
		bool flag;
		string message;
		if (kingdom == null || val == null)
		{
			flag = false;
			message = "叛乱命名已完成，但目标王国或家族状态已变化，无法继续执行。";
		}
		else
		{
			flag = TryExecuteKingdomRebellionWithNaming(val, kingdom, pendingDevForcedKingdomRebellionContext.WeekIndex, forceTrigger: true, pendingDevForcedKingdomRebellionContext.RelationToKing, pendingDevForcedKingdomRebellionContext.TownCount, pendingDevForcedKingdomRebellionContext.CastleCount, pendingDevForcedKingdomRebellionContext.NamingResult, list, out message);
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (pendingDevForcedKingdomRebellionContext.NamingResult != null)
		{
			stringBuilder.AppendLine("命名结果：");
			stringBuilder.AppendLine("- 正式名：" + (pendingDevForcedKingdomRebellionContext.NamingResult.FormalName ?? "").Trim());
			stringBuilder.AppendLine("- 简称：" + (pendingDevForcedKingdomRebellionContext.NamingResult.ShortName ?? "").Trim());
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
		stringBuilder.AppendLine((message ?? "").Trim());
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(flag ? "强制叛乱执行完成" : "强制叛乱执行失败", stringBuilder.ToString().TrimEnd(), true, false, "返回详情", "", (Action)delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom ?? FindKingdomById(pendingDevForcedKingdomRebellionContext.KingdomId));
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void StartDevForcedKingdomRebellionAsync(Kingdom kingdom, Clan clan, int weekIndex, int relationToKing, int townCount, int castleCount, List<Clan> followerClans)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Expected O, but got Unknown
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
		List<Clan> list = (from x in followerClans?.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => GetClanId(x), StringComparer.OrdinalIgnoreCase)
			select x.First()).ToList() ?? new List<Clan>();
		BuildRebelKingdomNamingRequest(clan, kingdom, weekIndex, list, out var systemPrompt, out var userPrompt, out var fallbackResult);
		_devForcedKingdomRebellionInProgress = true;
		_pendingDevForcedKingdomRebellionReady = false;
		_pendingDevForcedKingdomRebellionContext = null;
		InformationManager.ShowInquiry(new InquiryData("正在生成叛乱建国命名", "系统正在后台请求 LLM 为这次叛乱生成新王国的名称与百科简介。\n\n这一步完成后，才会真正执行家族反出与建国。\n请稍候，结果完成后会自动弹出。", false, false, "", "", (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, fallbackResult, "叛乱建国命名 - " + GetClanId(clan));
			_pendingDevForcedKingdomRebellionContext = new PendingDevForcedKingdomRebellionContext
			{
				KingdomId = GetKingdomId(kingdom),
				ClanId = GetClanId(clan),
				WeekIndex = weekIndex,
				RelationToKing = relationToKing,
				TownCount = townCount,
				CastleCount = castleCount,
				FollowerClanIds = (from x in list.Select(GetClanId)
					where !string.IsNullOrWhiteSpace(x)
					select x).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
				NamingResult = namingResult
			};
			_pendingDevForcedKingdomRebellionReady = true;
		});
	}

	private static string BuildHeroIdentityTitleForPrompt(Hero hero)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected I4, but got Unknown
		if (hero == null)
		{
			return "未知身份";
		}
		try
		{
			if ((int)hero.Occupation == 16)
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
			Kingdom val = ((clan != null) ? clan.Kingdom : null);
			if (clan != null && clan.IsUnderMercenaryService && val != null)
			{
				string text = (((object)val.Name)?.ToString() ?? "").Trim();
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
			IFaction mapFaction = hero.MapFaction;
			string text2 = (((mapFaction == null) ? null : ((object)mapFaction.Name)?.ToString()) ?? "").Trim();
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
			Occupation occupation = hero.Occupation;
			Occupation val2 = occupation;
			switch (val2 - 17)
			{
			case 1:
				return "商人";
			case 0:
				return "工匠";
			case 4:
				return "帮派首领";
			case 3:
				return "村长";
			case 2:
				return "教士";
			case 5:
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
			Clan clan = hero.Clan;
			Kingdom val = ((clan != null) ? clan.Kingdom : null);
			if (val != null)
			{
				string text = (((object)val.Name)?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					factionName = text;
				}
				Hero leader = val.Leader;
				string text2 = (((leader == null) ? null : ((object)leader.Name)?.ToString()) ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					liegeName = text2;
				}
				if (val.Leader == hero)
				{
					string text3 = (((object)hero.Name)?.ToString() ?? "").Trim();
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
				string text4 = (((object)mapFaction.Name)?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4))
				{
					factionName = text4;
				}
				Hero leader2 = mapFaction.Leader;
				string text5 = (((leader2 == null) ? null : ((object)leader2.Name)?.ToString()) ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text5))
				{
					liegeName = text5;
				}
				if (mapFaction.Leader == hero)
				{
					string text6 = (((object)hero.Name)?.ToString() ?? "").Trim();
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
			Clan clan2 = hero.Clan;
			if (clan2 != null)
			{
				string text7 = (((object)clan2.Name)?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text7))
				{
					factionName = text7;
				}
				Hero leader3 = clan2.Leader;
				string text8 = (((leader3 == null) ? null : ((object)leader3.Name)?.ToString()) ?? "").Trim();
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
			object obj;
			if (hero == null)
			{
				obj = null;
			}
			else
			{
				CultureObject culture = hero.Culture;
				obj = ((culture == null) ? null : ((object)((BasicCultureObject)culture).Name)?.ToString());
			}
			if (obj == null)
			{
				obj = "";
			}
			string text = ((string)obj).Trim();
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
		HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (object item in enumerable)
		{
			Hero hero = (Hero)((item is Hero) ? item : null);
			if (hero != null && hero != null)
			{
				string text = (((MBObjectBase)hero).StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && yielded.Add(text))
				{
					yield return hero;
				}
			}
		}
	}

	private static int GetMarriageCandidateMaxAgeSettingForPrompt()
	{
		try
		{
			int num = DuelSettings.GetSettings()?.MarriageCandidateMaxAge ?? 55;
			if (num < 18)
			{
				num = 18;
			}
			if (num > 80)
			{
				num = 80;
			}
			return num;
		}
		catch
		{
			return 55;
		}
	}

	private static int GetMarriageCandidateMaxAgeGapSettingForPrompt()
	{
		try
		{
			int num = DuelSettings.GetSettings()?.MarriageCandidateMaxAgeGap ?? 25;
			if (num < 0)
			{
				num = 0;
			}
			if (num > 60)
			{
				num = 60;
			}
			return num;
		}
		catch
		{
			return 25;
		}
	}

	private static bool GetMarriageRequireOppositeGenderSettingForPrompt()
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
			if (candidate.Age < 18f || candidate.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
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
			if (hero.Age < 18f || hero.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
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
			Clan val = ((npcHero != null) ? npcHero.Clan : null);
			if (val == null)
			{
				return "无（目标无家族）";
			}
			Hero leader = val.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(val)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（该家族当前没有基础适婚条件下的未婚成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int num = 0; num < list.Count; num++)
			{
				Hero val2 = list[num];
				string text = ((object)val2.Name)?.ToString() ?? ("Hero#" + num);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(val2);
				int num2 = (int)Math.Round(val2.Age);
				string text2 = "";
				if (val2 == npcHero)
				{
					text2 = "（你自己）";
				}
				else if (val2 == leader)
				{
					text2 = "（族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num2} | StringId={((MBObjectBase)val2).StringId}");
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
			Settlement val = Settlement.CurrentSettlement ?? ((hero != null) ? hero.CurrentSettlement : null);
			if (val != null)
			{
				useCivilianEquipment = !val.IsVillage;
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
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (hero != Hero.MainHero || Agent.Main == null || !Agent.Main.IsActive())
		{
			return null;
		}
		try
		{
			EquipmentElement val = Agent.Main.SpawnEquipment[index];
			ItemObject item = ((EquipmentElement)(ref val)).Item;
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
			MissionWeapon val2 = Agent.Main.Equipment[index];
			return ((MissionWeapon)(ref val2)).Item;
		}
		catch
		{
			return null;
		}
	}

	private static ItemObject TryGetHeroEquipmentItemForPrompt(Hero hero, EquipmentIndex index, bool useCivilianEquipment)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (hero == null)
		{
			return null;
		}
		ItemObject val = TryGetAgentEquipmentItemForPrompt(hero, index);
		if (val != null)
		{
			return val;
		}
		try
		{
			Equipment val2 = (useCivilianEquipment ? hero.CivilianEquipment : hero.BattleEquipment);
			if (val2 == null)
			{
				return null;
			}
			EquipmentElement val3 = val2[index];
			return ((EquipmentElement)(ref val3)).Item;
		}
		catch
		{
			return null;
		}
	}

	private unsafe static string BuildHeroEquipmentSummaryForPrompt(Hero hero, int maxEntries = 8)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (hero == null)
		{
			return "未知";
		}
		bool useCivilianEquipment = false;
		bool flag = TryResolveEquipmentContextForPrompt(hero, out useCivilianEquipment);
		string equipmentContextLabelForPrompt = GetEquipmentContextLabelForPrompt(useCivilianEquipment);
		try
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			EquipmentIndex[] array2 = (EquipmentIndex[])(object)array;
			EquipmentIndex[] array3 = array2;
			for (int i = 0; i < array3.Length; i++)
			{
				EquipmentIndex index = array3[i];
				ItemObject val = TryGetHeroEquipmentItemForPrompt(hero, index, useCivilianEquipment);
				if (val != null)
				{
					string text = (((MBObjectBase)val).StringId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text))
					{
						text = ((object)(*(EquipmentIndex*)(&index))/*cast due to .constrained prefix*/).ToString();
					}
					string value = (((object)val.Name)?.ToString() ?? "").Trim();
					if (string.IsNullOrWhiteSpace(value))
					{
						value = text;
					}
					if (!dictionary.ContainsKey(text))
					{
						dictionary[text] = 0;
						names[text] = value;
					}
					dictionary[text]++;
				}
			}
			if (dictionary.Count == 0)
			{
				return flag ? (equipmentContextLabelForPrompt + "：未读取到可识别装备") : "未能判断当前应显示民用装还是战斗装，且未读取到可识别装备";
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
			return equipmentContextLabelForPrompt + "：" + string.Join("、", values);
		}
		catch
		{
			return flag ? (equipmentContextLabelForPrompt + "：读取装备失败") : "装备信息读取失败";
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
			Clan clan = playerHero.Clan;
			num = ((clan != null) ? clan.Tier : 0);
			Clan clan2 = playerHero.Clan;
			string text2 = (((clan2 == null) ? null : ((object)clan2.Name)?.ToString()) ?? "").Trim();
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
			Clan clan3 = playerHero.Clan;
			flag = ((clan3 != null) ? clan3.Kingdom : null) != null;
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
			stringBuilder.AppendLine($"筛选口径：仅列出玩家家族内部基础适婚池（年龄 {18}-{marriageCandidateMaxAgeSettingForPrompt}、未婚、非囚犯）。具体能否与对方成员成婚，仍以性别限制、年龄差和运行时婚姻规则为准。");
			stringBuilder.AppendLine(BuildPlayerClanUnmarriedCandidatesForPrompt(playerHero, targetHero));
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
			Clan clan = npcHero.Clan;
			num = ((clan != null) ? clan.Tier : 0);
			Clan clan2 = npcHero.Clan;
			string text2 = (((clan2 == null) ? null : ((object)clan2.Name)?.ToString()) ?? "").Trim();
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
		Hero val = null;
		try
		{
			Clan clan3 = npcHero.Clan;
			val = ((clan3 != null) ? clan3.Leader : null);
		}
		catch
		{
			val = null;
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
				string text7 = BuildPlayerPublicDisplayNameForPrompt();
				if (string.IsNullOrWhiteSpace(text7))
				{
					text7 = "玩家";
				}
				string text8 = (npcHero.IsFemale ? "丈夫" : "妻子");
				stringBuilder.AppendLine("NPC与" + text7 + "的关系：合法配偶（" + text7 + "是你的" + text8 + "）；你必须认出" + text7 + "并以配偶关系进行回应。");
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
		if (val != null)
		{
			if (val == npcHero)
			{
				stringBuilder.AppendLine("该家族族长：你本人（StringId=" + ((MBObjectBase)npcHero).StringId + "）");
			}
			else
			{
				stringBuilder.AppendLine("该家族族长：" + (((object)val.Name)?.ToString() ?? "未知") + "（StringId=" + ((MBObjectBase)val).StringId + "）");
			}
		}
		stringBuilder.AppendLine("NPC年纪：" + text5);
		stringBuilder.AppendLine("NPC存款：" + num2 + " 第纳尔");
		stringBuilder.AppendLine("NPC装备：" + text6);
		if (includeMarriageCandidates)
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			stringBuilder.AppendLine("【该家族可婚配未婚成员（事实清单）】");
			stringBuilder.AppendLine($"筛选口径：仅列出该家族内部基础适婚池（年龄 {18}-{marriageCandidateMaxAgeSettingForPrompt}、未婚、非囚犯）。具体能否与玩家家族成员成婚，仍以性别限制、年龄差和运行时婚姻规则为准。");
			stringBuilder.AppendLine(BuildClanUnmarriedCandidatesForPrompt(npcHero, Hero.MainHero));
		}
		if (includeTradePricing && RewardSystemBehavior.Instance != null)
		{
			try
			{
				string text9 = RewardSystemBehavior.Instance.BuildInventorySummaryForAI(npcHero);
				if (!string.IsNullOrWhiteSpace(text9))
				{
					stringBuilder.AppendLine("【NPC当前可用财富与物品】(注意：你不可以转移超出数量的物品，钱，如果你没有那么多，请实话实说");
					stringBuilder.AppendLine(text9);
					Logger.Log("Logic", "[Context] InventorySummary=\n" + text9);
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
		string value = "无家族";
		int num = 0;
		try
		{
			Clan clan = npcHero.Clan;
			num = ((clan != null) ? clan.Tier : 0);
			Clan clan2 = npcHero.Clan;
			string text = (((clan2 == null) ? null : ((object)clan2.Name)?.ToString()) ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				value = text;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(npcHero, out var factionName, out var liegeName);
		string text2 = (factionName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "无（独立）";
		}
		string text3 = (liegeName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text3) && text3 != "无" && !text3.EndsWith("（本人）", StringComparison.Ordinal))
		{
			text2 = text2 + "（效忠：" + text3 + "）";
		}
		string value2 = (((object)npcHero.Name)?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value2))
		{
			value2 = "未知人物";
		}
		string value3 = BuildHeroIdentityTitleForPrompt(npcHero);
		string value4 = GetClanTierReputationLabel(num) + $"（{Math.Max(0, num)} level）";
		string value5 = BuildHeroEquipmentSummaryForPrompt(npcHero);
		string value6 = BuildAgeBracketLabel(npcHero.Age);
		string text4 = GetHeroCultureNameForPrompt(npcHero);
		if (!string.IsNullOrWhiteSpace(text4) && !text4.EndsWith("人", StringComparison.Ordinal))
		{
			text4 += "人";
		}
		GetNpcPersonaStrings(npcHero, out var personality, out var background);
		string value7 = (string.IsNullOrWhiteSpace(personality) ? "暂无记录" : personality.Trim());
		string value8 = (string.IsNullOrWhiteSpace(background) ? "暂无记录" : background.Trim());
		string value9 = (npcHero.IsFemale ? "女性成员" : "男性成员");
		try
		{
			Clan clan3 = npcHero.Clan;
			Hero val = ((clan3 != null) ? clan3.Leader : null);
			if (val != null && val == npcHero)
			{
				value9 = "族长";
			}
		}
		catch
		{
		}
		string text5 = "";
		if (includeTradePricing && RewardSystemBehavior.Instance != null)
		{
			try
			{
				text5 = RewardSystemBehavior.Instance.BuildInventorySummaryForAI(npcHero);
			}
			catch
			{
				text5 = "";
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("你是").Append(text2).Append("的")
			.Append(value)
			.Append("的")
			.Append(value2)
			.Append("，你是家族中的")
			.Append(value9)
			.Append("，你的身份是")
			.Append(value3)
			.Append("，你")
			.Append(value4)
			.Append("，你身上穿着")
			.Append(value5)
			.Append("，你的个性为：")
			.Append(value7)
			.Append("，你的背景是：")
			.Append(value8)
			.Append("，你的年纪是")
			.Append(value6)
			.Append("，你是")
			.Append(text4)
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
		if (1 == 0)
		{
		}
		string result = num switch
		{
			0 => "春", 
			1 => "夏", 
			2 => "秋", 
			_ => "冬", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private unsafe string BuildCurrentDateFactForPrompt()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			int num = Math.Max(0, (int)Math.Floor(((CampaignTime)(ref now)).ToDays));
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
				now = CampaignTime.Now;
				text = ((object)(*(CampaignTime*)(&now))/*cast due to .constrained prefix*/).ToString();
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
			JObject val = JObject.Parse(text2);
			personality = ((object)(val["personality"] ?? val["Personality"]))?.ToString() ?? "";
			background = ((object)(val["background"] ?? val["Background"]))?.ToString() ?? "";
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
		string id = ((MBObjectBase)hero).StringId;
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
			Exception ex3 = ex2;
			Logger.Log("NpcPersona", "[ERROR] AutoGen failed: " + ex3.Message);
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
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
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
			string text = $"可用数量: {tradeResourceOption.AvailableAmount}";
			list2.Add(new InquiryElement((object)i, $"{tradeResourceOption.Name} (×{tradeResourceOption.AvailableAmount})", (ImageIdentifier)null, true, text));
		}
		string text2 = ((_pendingTrade != null && _pendingTrade.IsGive) ? "给予其物品" : "展示物品");
		string text3 = "选择要使用的物品或第纳尔（可多选）：";
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(text2, text3, list2, true, 1, list2.Count, "确定", "取消", (Action<List<InquiryElement>>)OnMultiResourceSelected, (Action<List<InquiryElement>>)OnMultiResourceCancelled, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, true, false);
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
			return;
		}
		_pendingTradeItemIndex = 0;
		ShowAmountInquiryForCurrentItem();
	}

	private void OnMultiResourceCancelled(List<InquiryElement> _)
	{
		_pendingTrade = null;
		_pendingTradeItems.Clear();
	}

	private void ShowAmountInquiryForCurrentItem()
	{
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
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
		string text = ((_pendingTrade != null && _pendingTrade.IsGive) ? "给予数量" : "展示数量");
		string text2 = $"[{_pendingTradeItemIndex + 1}/{_pendingTradeItems.Count}] 你可以使用的 {pendingTradeItem.ItemName} 数量最多为 {num}。\n请输入 1 到 {num} 的整数：";
		int maxAmount = num;
		InformationManager.ShowTextInquiry(new TextInquiryData(text, text2, true, true, "确定", "返回", (Action<string>)delegate(string input)
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Expected O, but got Unknown
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
		}, (Action)delegate
		{
			string npcName = ((Hero.OneToOneConversationHero != null) ? ((object)Hero.OneToOneConversationHero.Name).ToString() : "陌生人");
			ShowResourceSelectionInquiry(npcName);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
	}

	private void ShowTradeChatInput()
	{
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Expected O, but got Unknown
		string text = ((Hero.OneToOneConversationHero != null) ? ((object)Hero.OneToOneConversationHero.Name).ToString() : "陌生人");
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
		string text3 = "与 " + text + " 交流";
		string text4 = text2 + "\n请输入你想说的话：";
		InformationManager.ShowTextInquiry(new TextInquiryData(text3, text4, true, true, "说", "取消", (Action<string>)OnTradeChatConfirmed, (Action)null, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
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
		Hero mainHero = Hero.MainHero;
		MobileParty val = ((mainHero != null) ? mainHero.PartyBelongedTo : null);
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
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, targetHero, num, false);
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
				if (val == null)
				{
					continue;
				}
				ItemRoster itemRoster = val.ItemRoster;
				if (itemRoster == null)
				{
					continue;
				}
				object obj2 = pendingTradeItem.ItemId;
				if (obj2 == null)
				{
					ItemObject item = pendingTradeItem.Item;
					obj2 = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "";
				}
				string text = ((string)obj2).Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				MobileParty partyBelongedTo = targetHero.PartyBelongedTo;
				ItemRoster targetRoster = ((partyBelongedTo != null) ? partyBelongedTo.ItemRoster : null);
				ItemObject transferredItem;
				int num2 = TransferItemsFromRosterByStringId(itemRoster, targetRoster, text, pendingTradeItem.Amount, out transferredItem);
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
			Clan val = ((playerHero != null) ? playerHero.Clan : null);
			if (val == null)
			{
				return "无（玩家无家族）";
			}
			Hero leader = val.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(val)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（玩家家族当前没有基础适婚条件下的未婚成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int num = 0; num < list.Count; num++)
			{
				Hero val2 = list[num];
				string text = ((object)val2.Name)?.ToString() ?? ("Hero#" + num);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(val2);
				int num2 = (int)Math.Round(val2.Age);
				string text2 = "";
				if (val2 == playerHero)
				{
					text2 = "（玩家本人）";
				}
				else if (val2 == leader)
				{
					text2 = "（玩家家族族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num2} | StringId={((MBObjectBase)val2).StringId}");
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
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.RecordShownResources(hero, targetKey, shownGold, shownItems);
		}
		catch
		{
		}
	}

	public static int GetRemainingShowableGoldForExternal(Hero hero, string targetKey, int currentGold)
	{
		try
		{
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.GetRemainingShowableGold(hero, targetKey, currentGold) ?? Math.Max(0, currentGold);
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.GetRemainingShowableItemCount(hero, targetKey, itemId, currentAmount) ?? Math.Max(0, currentAmount);
		}
		catch
		{
			return Math.Max(0, currentAmount);
		}
	}

	private static string GetTradeTargetDisplayName(Hero targetHero)
	{
		string text = (((targetHero == null) ? null : ((object)targetHero.Name)?.ToString()) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "对方" : text;
	}

	private string BuildGiveFactText(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return "";
		}
		Hero mainHero = Hero.MainHero;
		string text = ((mainHero == null) ? null : ((object)mainHero.Name)?.ToString()) ?? "玩家";
		string tradeTargetDisplayName = GetTradeTargetDisplayName(targetHero);
		List<string> list = new List<string>();
		foreach (PendingTradeItem pendingTradeItem in _pendingTradeItems)
		{
			if (pendingTradeItem.Amount <= 0)
			{
				continue;
			}
			if (pendingTradeItem.IsGold)
			{
				list.Add($"{pendingTradeItem.Amount} 第纳尔");
				continue;
			}
			object obj = pendingTradeItem.ItemId;
			if (obj == null)
			{
				ItemObject item = pendingTradeItem.Item;
				obj = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "";
			}
			string itemId = (string)obj;
			string arg = RewardSystemBehavior.Instance?.BuildItemValueFactSuffixForExternal(targetHero ?? Hero.MainHero, itemId, pendingTradeItem.Amount) ?? "";
			list.Add($"{pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}{arg}");
		}
		if (list.Count == 0)
		{
			return "";
		}
		return text + "已经将 " + string.Join("、", list) + " 交给 " + tradeTargetDisplayName + "。";
	}

	private string BuildShowFactText(Hero targetHero)
	{
		if (_pendingTradeItems == null || _pendingTradeItems.Count == 0)
		{
			return "";
		}
		Hero mainHero = Hero.MainHero;
		string text = ((mainHero == null) ? null : ((object)mainHero.Name)?.ToString()) ?? "玩家";
		string tradeTargetDisplayName = GetTradeTargetDisplayName(targetHero);
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
					string arg = RewardSystemBehavior.Instance?.BuildInventoryActualItemValueFactSuffixForExternal(pendingTradeItem.Item, pendingTradeItem.Amount, pendingTradeItem.InventoryUnitValue) ?? "";
					num += RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(pendingTradeItem.Item, pendingTradeItem.Amount, pendingTradeItem.InventoryUnitValue) ?? 0;
					list.Add($"{pendingTradeItem.Amount} 个 {pendingTradeItem.ItemName}{arg}");
				}
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		return text + "给 " + tradeTargetDisplayName + " 看了看 总值为 " + num + " 第纳尔的各类财物：" + string.Join("、", list) + "，但没有将其给入你的库存。";
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
			if (pendingTradeItem != null && pendingTradeItem.Amount > 0)
			{
				num = ((!pendingTradeItem.IsGold) ? (num + (RewardSystemBehavior.Instance?.EstimateInventoryActualItemValueForExternal(pendingTradeItem.Item, pendingTradeItem.Amount, pendingTradeItem.InventoryUnitValue) ?? 0)) : (num + pendingTradeItem.Amount));
			}
		}
		return num;
	}

	private void ShowPendingDisplayValueMessage(Hero targetHero, long totalValue)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		if (totalValue > 0)
		{
			string tradeTargetDisplayName = GetTradeTargetDisplayName(targetHero);
			InformationManager.DisplayMessage(new InformationMessage("【展示估值】你向 " + tradeTargetDisplayName + " 展示了总值为 " + totalValue + " 第纳尔的财物。", new Color(0.95f, 0.85f, 0.25f, 1f)));
		}
	}

	private List<TradeResourceOption> BuildResourceOptions(Hero targetHero)
	{
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		List<TradeResourceOption> list = new List<TradeResourceOption>();
		Hero mainHero = Hero.MainHero;
		MobileParty val = ((mainHero != null) ? mainHero.PartyBelongedTo : null);
		if (val == null)
		{
			return list;
		}
		ItemRoster itemRoster = val.ItemRoster;
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
				EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
				int amount = ((ItemRosterElement)(ref elementCopyAtIndex)).Amount;
				if (item == null || amount <= 0)
				{
					continue;
				}
				string text = (((MBObjectBase)item).StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (dictionary.TryGetValue(text, out var value))
					{
						value.AvailableAmount += amount;
						value.InventoryTotalValue += (long)amount * (long)(RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement) ?? 1);
						continue;
					}
					dictionary[text] = new TradeResourceOption
					{
						IsGold = false,
						ItemId = text,
						Name = ((object)item.Name).ToString(),
						AvailableAmount = amount,
						Item = item,
						InventoryTotalValue = (long)amount * (long)(RewardSystemBehavior.Instance?.GetInventoryActualItemUnitValueForExternal(((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement) ?? 1)
					};
				}
			}
			foreach (TradeResourceOption value3 in dictionary.Values)
			{
				int num2 = value3.AvailableAmount;
				value3.InventoryUnitValue = ((num2 <= 0) ? 1 : Math.Max(1, (int)Math.Round((double)value3.InventoryTotalValue / (double)num2, MidpointRounding.AwayFromZero)));
				if (_pendingTrade != null && !_pendingTrade.IsGive && heroShownRecord != null && heroShownRecord.ShownItems.TryGetValue(value3.ItemId, out var value2))
				{
					num2 = Math.Max(0, num2 - value2);
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
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		transferredItem = null;
		if (sourceRoster == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
		{
			return 0;
		}
		string b = itemId.Trim();
		int num = amount;
		int num2 = 0;
		while (num > 0)
		{
			bool flag = false;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = sourceRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = ((ItemRosterElement)(ref elementCopyAtIndex)).EquipmentElement;
				ItemObject item = ((EquipmentElement)(ref equipmentElement)).Item;
				if (item == null || ((ItemRosterElement)(ref elementCopyAtIndex)).Amount <= 0 || !string.Equals(((MBObjectBase)item).StringId ?? "", b, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				transferredItem = transferredItem ?? item;
				int num3 = Math.Min(((ItemRosterElement)(ref elementCopyAtIndex)).Amount, num);
				if (num3 > 0)
				{
					sourceRoster.AddToCounts(equipmentElement, -num3);
					if (targetRoster != null)
					{
						targetRoster.AddToCounts(equipmentElement, num3);
					}
					num -= num3;
					num2 += num3;
					flag = true;
					break;
				}
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
			if (!string.IsNullOrWhiteSpace(text) && shownItem.Value > 0)
			{
				if (!shownRecord.ShownItems.ContainsKey(text))
				{
					shownRecord.ShownItems[text] = 0;
				}
				shownRecord.ShownItems[text] += shownItem.Value;
			}
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
		string text = ((hero != null) ? ((MBObjectBase)hero).StringId : null);
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

	private void StartAiConversation(string input, string extraFact)
	{
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Expected O, but got Unknown
		Hero targetHero = Hero.OneToOneConversationHero;
		CharacterObject targetCharacter = null;
		int targetAgentIndex = -1;
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
				ConversationManager conversationManager = current.ConversationManager;
				obj = ((conversationManager != null) ? conversationManager.OneToOneConversationCharacter : null);
			}
			targetCharacter = (CharacterObject)obj;
		}
		catch
		{
		}
		try
		{
			Campaign current2 = Campaign.Current;
			object obj3;
			if (current2 == null)
			{
				obj3 = null;
			}
			else
			{
				ConversationManager conversationManager2 = current2.ConversationManager;
				obj3 = ((conversationManager2 != null) ? conversationManager2.OneToOneConversationAgent : null);
			}
			object obj4 = ((obj3 is Agent) ? obj3 : null);
			targetAgentIndex = ((obj4 != null) ? ((Agent)obj4).Index : (-1));
		}
		catch
		{
			targetAgentIndex = -1;
		}
		Hero obj6 = targetHero;
		object obj7 = ((obj6 == null) ? null : ((object)obj6.Name)?.ToString());
		if (obj7 == null)
		{
			CharacterObject obj8 = targetCharacter;
			obj7 = ((obj8 == null) ? null : ((object)((BasicCharacterObject)obj8).Name)?.ToString()) ?? "NPC";
		}
		string npcName = (string)obj7;
		Hero obj9 = targetHero;
		object obj10;
		if (obj9 == null)
		{
			obj10 = null;
		}
		else
		{
			CultureObject culture = obj9.Culture;
			obj10 = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
		}
		if (obj10 == null)
		{
			CharacterObject obj11 = targetCharacter;
			if (obj11 == null)
			{
				obj10 = null;
			}
			else
			{
				CultureObject culture2 = obj11.Culture;
				obj10 = ((culture2 != null) ? ((MBObjectBase)culture2).StringId : null);
			}
			if (obj10 == null)
			{
				obj10 = "neutral";
			}
		}
		string cultureId = (string)obj10;
		int playerTier = Hero.MainHero.Clan.Tier;
		string characterDescription = GetHeroDescriptionSafe(targetHero);
		string locationInfo = GetCurrentLocationInfoSafe();
		bool hasUnpaidDebt = RewardSystemBehavior.Instance != null && targetHero != null && RewardSystemBehavior.Instance.HasUnpaidDebt(targetHero);
		if (hasUnpaidDebt)
		{
			Logger.Log("Logic", "[Trade] 检测到玩家尚欠 " + npcName + " 的货款或物品，疑似上一笔交易逃单。");
		}
		InformationManager.DisplayMessage(new InformationMessage(npcName + " 正在思考...", new Color(0.7f, 0.7f, 0.7f, 1f)));
		Task.Run(async delegate
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Hero obj12 = targetHero;
			using (Logger.BeginTrace("direct", (obj12 != null) ? ((MBObjectBase)obj12).StringId : null, npcName))
			{
				Dictionary<string, object> obj13 = new Dictionary<string, object> { ["npc"] = npcName };
				Hero obj14 = targetHero;
				obj13["heroId"] = ((obj14 != null) ? ((MBObjectBase)obj14).StringId : null) ?? "";
				obj13["inputLen"] = (input ?? "").Length;
				obj13["hasUnpaidDebt"] = hasUnpaidDebt;
				Logger.Obs("DirectChat", "start", obj13);
				await Task.Delay(300);
				SimulateLeftClick();
				try
				{
					if (targetHero != null)
					{
						if (!TryGetNpcPersonaGenerationStatusForExternal(targetHero, out var needsGeneration, out var _) || needsGeneration)
						{
							InformationManager.DisplayMessage(new InformationMessage(BuildNpcPersonaGenerationHintForExternal(targetHero), new Color(1f, 0.85f, 0.3f, 1f)));
						}
						EnsureNpcPersonaGeneratedAsync(targetHero);
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
					RewardSystemBehavior.SettlementMerchantKind kind;
					bool isSettlementMerchant = targetHero == null && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out kind);
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
					bool useRewardContext = isRewardContext;
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
					Logger.Log("Logic", string.Format("[SemanticTrigger] DuelHit={0} [{1}] RewardHit={2} [{3}] LoanHit={4} [{5}] SurroundingsHit={6} [{7}] KingdomServiceHit={8} [{9}] MarriageHit={10} [{11}] NpcRecall={12} Input='{13}' NPC='{14}'", isTriggerWordDetected, duelHits, isRewardContext, rewardHits, isLoanContext, loanHits, isSurroundingsContext, surroundingsHits, isKingdomServiceHit, kingdomServiceHits, isMarriageHit, marriageHits, string.IsNullOrWhiteSpace(npcLastUtterance) ? "off" : "on", input, npcName));
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
								hasPos = ((CampaignVec2)(ref pos)).IsValid();
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
					string loreContext = ((targetHero != null) ? AIConfigHandler.GetLoreContext(input, targetHero, npcLastUtterance) : AIConfigHandler.GetLoreContext(input, targetCharacter, null, npcLastUtterance));
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
					if (targetHero != null && !string.IsNullOrEmpty(((MBObjectBase)targetHero).StringId))
					{
						string text9 = BuildPlayerPublicDisplayNameForPrompt();
						if (string.IsNullOrWhiteSpace(text9))
						{
							text9 = "玩家";
						}
						if (_recentlyDefeatedByPlayer.Remove(((MBObjectBase)targetHero).StringId))
						{
							sb.AppendLine("【原版战斗结果】你刚刚在一场战斗中被" + text9 + "击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。");
						}
						if (_recentlyReleasedPrisoners.Remove(((MBObjectBase)targetHero).StringId))
						{
							sb.AppendLine("【释放通知】你之前被" + text9 + "俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。");
						}
					}
					AppendPlayerExtraFactLine(sb, extraFact);
					if (!string.IsNullOrWhiteSpace(guardrailClarifyHint))
					{
						sb.AppendLine(guardrailClarifyHint);
					}
					string triggeredRuleInstructions = BuildTriggeredRuleInstructions(input, targetHero, useDuelContext, isQualified, playerTier, useRewardContext, isLoanContext, isSurroundingsContext, targetHero != null, targetCharacter, null, targetAgentIndex, npcLastUtterance, includeDuelStakeContext, playerWonLastDuelForRule);
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
					string[] obj18 = new string[9] { "[AI Request] NPC=", npcName, " HeroId=", null, null, null, null, null, null };
					Hero obj19 = targetHero;
					obj18[3] = ((obj19 != null) ? ((MBObjectBase)obj19).StringId : null) ?? "null";
					obj18[4] = "\n[SYSTEM]=\n";
					obj18[5] = finalSystemPrompt;
					obj18[6] = "\n[USER]=\n";
					obj18[7] = addressedInput;
					obj18[8] = "\n";
					Logger.Log("Logic", string.Concat(obj18));
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
						//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
						//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
						//IL_01d5: Expected O, but got Unknown
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
							string text10 = streamBuf.ToString();
							try
							{
								if (ConversationHelper.HasActiveVM)
								{
									ConversationHelper.UpdateDialogText(StripActionTags(text10));
								}
								else
								{
									if (chunkCount <= 3)
									{
										Logger.Log("Logic", $"[HTTP-Stream] ⚠\ufe0f VM 未捕获！chunk #{chunkCount}, 使用 DisplayMessage 回退");
									}
									string text11 = ((text10.Length > 150) ? ("..." + text10.Substring(text10.Length - 150)) : text10);
									InformationManager.DisplayMessage(new InformationMessage("[" + capturedNpcName + "] " + text11, new Color(0.85f, 0.85f, 0.6f, 1f)));
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
						InformationManager.DisplayMessage(new InformationMessage("[API连接失败] " + streamError, new Color(1f, 0.3f, 0.3f, 1f)));
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
					InformationManager.DisplayMessage(new InformationMessage(npcName + " 已准备好回应。", new Color(0f, 1f, 0f, 1f)));
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
				PartyBase partyBelongedToAsPrisoner = hero.PartyBelongedToAsPrisoner;
				object obj;
				if (partyBelongedToAsPrisoner == null)
				{
					obj = null;
				}
				else
				{
					Hero leaderHero = partyBelongedToAsPrisoner.LeaderHero;
					obj = ((leaderHero == null) ? null : ((object)leaderHero.Name)?.ToString());
				}
				string text = (string)obj;
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
		string stringId = ((MBObjectBase)hero).StringId;
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.GetFirstMeetingNpcFactTextForPromptIfNeededInternal(hero, persistToHistory) ?? "";
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
		List<DialogueDay> records = LoadDialogueHistory(hero);
		if (HasDialogueHistoryLine(records, text) || HasMeaningfulDirectConversationHistory(records))
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
			string stringId = ((MBObjectBase)hero).StringId;
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
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.AppendDialogueHistory(hero, playerText, aiText, extraFact);
		}
		catch
		{
		}
	}

	public static void AppendExternalSceneDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId)
	{
		try
		{
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId);
		}
		catch
		{
		}
	}

	private unsafe void AppendDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId = -1)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (hero == null || (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact)))
		{
			return;
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(hero);
			CampaignTime now = CampaignTime.Now;
			int dayIndex = (int)((CampaignTime)(ref now)).ToDays;
			now = CampaignTime.Now;
			string gameDate = ((object)(*(CampaignTime*)(&now))/*cast due to .constrained prefix*/).ToString();
			string text = ((object)hero.Name)?.ToString() ?? "NPC";
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
		Hero val = targetHero ?? ((targetCharacter != null) ? targetCharacter.HeroObject : null);
		if (val == null)
		{
			return GetLatestSceneNpcDialogueUtteranceFallback(targetAgentIndex);
		}
		try
		{
			List<DialogueDay> list = LoadDialogueHistory(val);
			if (list == null || list.Count == 0)
			{
				return GetLatestSceneNpcDialogueUtteranceFallback(targetAgentIndex);
			}
			for (int num = list.Count - 1; num >= 0; num--)
			{
				DialogueDay dialogueDay = list[num];
				if (dialogueDay?.Lines != null && dialogueDay.Lines.Count > 0)
				{
					for (int num2 = dialogueDay.Lines.Count - 1; num2 >= 0; num2--)
					{
						string text = (dialogueDay.Lines[num2] ?? "").Trim();
						if (!string.IsNullOrWhiteSpace(text) && !IsActiveSceneSessionHistoryLine(text) && !IsLoreInjectionHistoryLine(text) && !IsSystemFactLine(text) && !IsPlayerTurnStartLine(text))
						{
							return StripSpeakerPrefixForRecall(text);
						}
					}
				}
			}
		}
		catch
		{
		}
		return GetLatestSceneNpcDialogueUtteranceFallback(targetAgentIndex);
	}

	private static string GetLatestSceneNpcDialogueUtteranceFallback(int targetAgentIndex)
	{
		if (targetAgentIndex < 0)
		{
			return "";
		}
		string latestSceneNpcUtteranceForExternal = ShoutBehavior.GetLatestSceneNpcUtteranceForExternal(targetAgentIndex);
		return string.IsNullOrWhiteSpace(latestSceneNpcUtteranceForExternal) ? "" : StripSpeakerPrefixForRecall(latestSceneNpcUtteranceForExternal);
	}

	public static string BuildHistoryContextForExternal(Hero hero, int maxLines = 20, string currentInput = null, string secondaryInput = null)
	{
		try
		{
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			if (!string.IsNullOrWhiteSpace(text))
			{
				string text2 = (((object)hero.Name)?.ToString() ?? "NPC").Trim();
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "NPC";
				}
				AppendExternalDialogueHistory(hero, null, null, "[AFEF NPC行为补充] " + text2 + ": " + text);
			}
		}
		catch
		{
		}
	}

	public static void AppendExternalPlayerFact(Hero hero, string factText)
	{
		try
		{
			if (hero != null)
			{
				string text = (factText ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					AppendExternalDialogueHistory(hero, null, null, "[AFEF玩家行为补充] " + text);
				}
			}
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
							if (!string.IsNullOrWhiteSpace(text))
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
			if (!string.IsNullOrWhiteSpace(text3))
			{
				if (text3.Length > 180)
				{
					text3 = text3.Substring(0, 180);
				}
				string text4 = text3.Replace("\r", " ").Replace("\n", " ");
				if (text4.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text4.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
				{
					list.Add(text4);
				}
				else
				{
					list.Add("[AFEF玩家行为补充] " + text4);
				}
			}
			string text5 = VanillaIssueOfferBridge.BuildRagSemanticStateForExternal(hero);
			if (!string.IsNullOrWhiteSpace(text5))
			{
				list.Add(text5);
			}
			if (list.Count <= 0)
			{
				return "";
			}
			string text6 = string.Join("\n", list);
			if (text6.Length > 700)
			{
				text6 = text6.Substring(text6.Length - 700);
			}
			return text6;
		}
		catch
		{
			return "";
		}
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
			object obj;
			if (targetHero == null)
			{
				obj = null;
			}
			else
			{
				Clan clan = targetHero.Clan;
				if (clan == null)
				{
					obj = null;
				}
				else
				{
					Kingdom kingdom = clan.Kingdom;
					obj = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
				}
			}
			if (obj == null)
			{
				if (targetHero == null)
				{
					obj = null;
				}
				else
				{
					IFaction mapFaction = targetHero.MapFaction;
					obj = ((mapFaction != null) ? mapFaction.StringId : null);
				}
				if (obj == null)
				{
					obj = "";
				}
			}
			string text2 = ((string)obj).Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.ToLowerInvariant();
			}
			Hero val = ((targetCharacter != null) ? targetCharacter.HeroObject : null);
			object obj2;
			if (val == null)
			{
				obj2 = null;
			}
			else
			{
				Clan clan2 = val.Clan;
				if (clan2 == null)
				{
					obj2 = null;
				}
				else
				{
					Kingdom kingdom2 = clan2.Kingdom;
					obj2 = ((kingdom2 != null) ? ((MBObjectBase)kingdom2).StringId : null);
				}
			}
			if (obj2 == null)
			{
				if (val == null)
				{
					obj2 = null;
				}
				else
				{
					IFaction mapFaction2 = val.MapFaction;
					obj2 = ((mapFaction2 != null) ? mapFaction2.StringId : null);
				}
				if (obj2 == null)
				{
					obj2 = "";
				}
			}
			string text3 = ((string)obj2).Trim();
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
			object obj = ((targetHero != null) ? ((MBObjectBase)targetHero).StringId : null);
			if (obj == null)
			{
				if (targetCharacter == null)
				{
					obj = null;
				}
				else
				{
					Hero heroObject = targetCharacter.HeroObject;
					obj = ((heroObject != null) ? ((MBObjectBase)heroObject).StringId : null);
				}
				if (obj == null)
				{
					obj = "";
				}
			}
			string text = ((string)obj).Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return "hero:" + text;
			}
			if (targetAgentIndex >= 0)
			{
				return "agent:" + targetAgentIndex;
			}
			string text2 = (((targetCharacter != null) ? ((MBObjectBase)targetCharacter).StringId : null) ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				string text3 = (((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null) ?? "").Trim().ToLowerInvariant();
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
		int guardrailRuleReturnCap = AIConfigHandler.GuardrailRuleReturnCap;
		string guardrailRuntimeTargetKingdom = ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
		AIConfigHandler.SetGuardrailRuntimeTargetKingdom(guardrailRuntimeTargetKingdom);
		object obj = ((targetHero != null) ? ((MBObjectBase)targetHero).StringId : null);
		if (obj == null)
		{
			if (targetCharacter == null)
			{
				obj = null;
			}
			else
			{
				Hero heroObject = targetCharacter.HeroObject;
				obj = ((heroObject != null) ? ((MBObjectBase)heroObject).StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
		}
		string guardrailRuntimeTargetHero = (string)obj;
		AIConfigHandler.SetGuardrailRuntimeTargetHero(guardrailRuntimeTargetHero);
		AIConfigHandler.SetGuardrailRuntimeTargetCharacter(((targetCharacter != null) ? ((MBObjectBase)targetCharacter).StringId : null) ?? "");
		AIConfigHandler.SetGuardrailRuntimeTargetTroop(((targetCharacter != null) ? ((MBObjectBase)targetCharacter).StringId : null) ?? "");
		AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank((targetHero != null || targetCharacter == null) ? "" : (((BasicCharacterObject)targetCharacter).IsSoldier ? "soldier" : "commoner"));
		AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(targetAgentIndex);
		try
		{
			text = AIConfigHandler.BuildMatchedExtraRuleInstructions(input, npcLastUtterance, AIConfigHandler.GuardrailRuleReturnCap, hasAnyHero);
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
		if (!string.IsNullOrWhiteSpace(text2) && (string.IsNullOrWhiteSpace(text) || text.IndexOf("【附加规则:lords_hall_access】", StringComparison.OrdinalIgnoreCase) < 0) && CountInjectedRuleBlocks(text) < guardrailRuleReturnCap)
		{
			string text3 = "【附加规则:lords_hall_access】" + Environment.NewLine + text2;
			text = (string.IsNullOrWhiteSpace(text) ? text3 : (text.TrimEnd() + Environment.NewLine + text3));
		}
		if (IsSceneFollowingAgentForRules(targetAgentIndex))
		{
			text = ReplaceSceneMechanismRuleForFollowing(text);
		}
		return text;
	}

	private static bool IsSceneFollowingAgentForRules(int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex < 0 || Mission.Current == null || PlayerEncounter.LocationEncounter == null || LocationComplex.Current == null)
			{
				return false;
			}
			Agent val = ((IEnumerable<Agent>)Mission.Current.Agents)?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (val == null || val == Agent.Main)
			{
				return false;
			}
			LocationCharacter val2 = LocationComplex.Current.FindCharacter((IAgent)(object)val);
			AccompanyingCharacter val3 = ((val2 != null) ? PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(val2) : null);
			return val3 != null && val3.IsFollowingPlayerAtMissionStart;
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
		int num = text.IndexOf("【附加规则:scene_mechanism_actions】", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return text;
		}
		int num2 = text.IndexOf("【附加规则:", num + 1, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return text.Substring(0, num).TrimEnd() + Environment.NewLine + "【附加规则:scene_mechanism_actions】\r\n【当前正跟随玩家】若此人明确让你停止跟随且你同意，可在句末输出[STP]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]，多人可写[ASS:id1,id2]；若此人改让你带路去找【可带路目标】，也可输出[GUI:id]。";
		}
		return text.Substring(0, num).TrimEnd() + Environment.NewLine + "【附加规则:scene_mechanism_actions】\r\n【当前正跟随玩家】若此人明确让你停止跟随且你同意，可在句末输出[STP]；若此人改让你去叫【可召目标】中的人，也可输出[ASS:id]，多人可写[ASS:id1,id2]；若此人改让你带路去找【可带路目标】，也可输出[GUI:id]。" + Environment.NewLine + text.Substring(num2).TrimStart();
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
						string text = BuildPlayerPublicDisplayNameForPrompt();
						if (string.IsNullOrWhiteSpace(text))
						{
							text = "玩家";
						}
						AppendRuleBlock(stringBuilder, "duel", $"{text}触发了决斗相关话题，但等级({playerTier})过低。请拒绝决斗并羞辱其不自量力。严禁使用决斗标签。");
					}
				}
			}
			if (AIConfigHandler.RewardEnabled && useRewardContext)
			{
				string text2 = "";
				if (!hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null)
				{
					string text3 = RewardSystemBehavior.Instance.BuildSettlementMerchantRewardInstruction(targetCharacter);
					if (!string.IsNullOrWhiteSpace(text3))
					{
						string text4 = AIConfigHandler.BuildRuntimeRewardInstructionForExternal(null, targetCharacter);
						text2 = (string.IsNullOrWhiteSpace(text4) ? text3 : (text4.Trim() + "\n" + text3.Trim()));
					}
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = (hasAnyHero ? AIConfigHandler.BuildRuntimeRewardInstructionForExternal(targetHero, targetCharacter) : AIConfigHandler.RewardNonHeroInstruction);
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = AIConfigHandler.RewardInstruction;
				}
				AppendRuleBlock(stringBuilder, "reward", text2);
				if (AIConfigHandler.DuelStakeEnabled && includeDuelStakeContext)
				{
					string body = (playerWonLastDuel ? AIConfigHandler.DuelStakePlayerWinInstruction : AIConfigHandler.DuelStakeNpcWinInstruction);
					AppendRuleBlock(stringBuilder, "duel_stake", body);
				}
			}
			if (AIConfigHandler.LoanEnabled && isLoanContext)
			{
				RewardSystemBehavior.SettlementMerchantKind kind;
				bool flag = !hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out kind);
				string text5 = ((hasAnyHero || flag) ? AIConfigHandler.BuildRuntimeLoanInstructionForExternal(targetHero, targetCharacter) : AIConfigHandler.LoanNonHeroInstruction);
				if (string.IsNullOrWhiteSpace(text5))
				{
					text5 = AIConfigHandler.LoanInstruction;
				}
				AppendRuleBlock(stringBuilder, "loan", text5);
			}
			if (AIConfigHandler.SurroundingsEnabled && isSurroundingsContext)
			{
				AppendRuleBlock(stringBuilder, "surroundings", AIConfigHandler.SurroundingsInstruction);
			}
			string text6 = BuildExtraRuleInstructions(input, npcLastUtterance, targetHero, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex);
			if (!string.IsNullOrWhiteSpace(text6))
			{
				stringBuilder.AppendLine(text6.Trim());
			}
			string text7 = SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(targetHero, targetCharacter, targetAgentIndex);
			if (!string.IsNullOrWhiteSpace(text7))
			{
				stringBuilder.AppendLine(text7.Trim());
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
		string text = ((hero == null) ? null : ((object)hero.Name)?.ToString()?.Trim());
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
			Campaign current = Campaign.Current;
			MyBehavior inst = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.BuildNpcMajorActionsRuntimeInstruction(hero) ?? "";
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.BuildNpcRecentActionsRuntimeInstruction(hero) ?? "";
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.BuildNpcCurrentActionFact(hero) ?? "";
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.BuildNpcActionsRuntimeConstraintHint(hero, recentOnly) ?? "";
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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

	private ShoutPromptContext BuildShoutPromptContextForExternalInternal(Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, bool suppressDynamicRuleAndLore = false, bool usePrefetchedLoreContext = false, string prefetchedLoreContext = null)
	{
		//IL_0a0c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f6: Unknown result type (might be due to invalid IL or missing references)
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
		object obj;
		if (string.IsNullOrEmpty(cultureIdOverride))
		{
			if (targetHero == null)
			{
				obj = null;
			}
			else
			{
				CultureObject culture = targetHero.Culture;
				obj = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
			}
			if (obj == null)
			{
				obj = "neutral";
			}
		}
		else
		{
			obj = cultureIdOverride;
		}
		string text = (string)obj;
		int num = _cachedPlayerClanTier;
		if (num <= 0)
		{
			try
			{
				Clan playerClan = Clan.PlayerClan;
				num = ((playerClan != null) ? playerClan.Tier : 0);
			}
			catch
			{
			}
			if (num <= 0)
			{
				try
				{
					Hero mainHero = Hero.MainHero;
					int? obj3;
					if (mainHero == null)
					{
						obj3 = null;
					}
					else
					{
						Clan clan = mainHero.Clan;
						obj3 = ((clan != null) ? new int?(clan.Tier) : ((int?)null));
					}
					int? num2 = obj3;
					num = num2.GetValueOrDefault();
				}
				catch
				{
				}
			}
		}
		int num3 = DuelSettings.GetSettings()?.MinimumClanTier ?? 0;
		bool isQualified = num >= num3;
		string latestNpcDialogueUtterance = GetLatestNpcDialogueUtterance(targetHero, targetCharacter, targetAgentIndex);
		RewardSystemBehavior.SettlementMerchantKind kind;
		bool flag = targetHero == null && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out kind);
		List<string> duelTriggerKeywords = AIConfigHandler.DuelTriggerKeywords;
		bool flag2 = false;
		string matchedKeyword = "";
		float score = 0f;
		if (!suppressDynamicRuleAndLore)
		{
			flag2 = AIConfigHandler.IsGuardrailSemanticHit(input, latestNpcDialogueUtterance, "duel", AIConfigHandler.DuelInstruction, duelTriggerKeywords, out matchedKeyword, out score);
		}
		bool duel = flag2;
		bool flag3 = targetHero != null && flag2;
		List<string> rewardTriggerKeywords = AIConfigHandler.RewardTriggerKeywords;
		bool flag4 = false;
		string matchedKeyword2 = "";
		float score2 = 0f;
		if (!suppressDynamicRuleAndLore && AIConfigHandler.RewardEnabled)
		{
			flag4 = AIConfigHandler.IsGuardrailSemanticHit(input, latestNpcDialogueUtterance, "reward", AIConfigHandler.RewardInstruction, rewardTriggerKeywords, out matchedKeyword2, out score2);
		}
		bool reward = flag4;
		List<string> loanTriggerKeywords = AIConfigHandler.LoanTriggerKeywords;
		bool flag5 = false;
		string matchedKeyword3 = "";
		float score3 = 0f;
		if (!suppressDynamicRuleAndLore && AIConfigHandler.LoanEnabled)
		{
			flag5 = AIConfigHandler.IsGuardrailSemanticHit(input, latestNpcDialogueUtterance, "loan", AIConfigHandler.LoanInstruction, loanTriggerKeywords, out matchedKeyword3, out score3);
		}
		bool loan = flag5;
		List<string> surroundingsTriggerKeywords = AIConfigHandler.SurroundingsTriggerKeywords;
		bool flag6 = false;
		string matchedKeyword4 = "";
		float score4 = 0f;
		if (!suppressDynamicRuleAndLore && AIConfigHandler.SurroundingsEnabled)
		{
			flag6 = AIConfigHandler.IsGuardrailSemanticHit(input, latestNpcDialogueUtterance, "surroundings", AIConfigHandler.SurroundingsInstruction, surroundingsTriggerKeywords, out matchedKeyword4, out score4);
		}
		string guardrailRuleInstruction = AIConfigHandler.GetGuardrailRuleInstruction("kingdom_service");
		List<string> guardrailRuleKeywords = AIConfigHandler.GetGuardrailRuleKeywords("kingdom_service");
		string matchedKeyword5 = "";
		float score5 = 0f;
		bool flag7 = !suppressDynamicRuleAndLore && AIConfigHandler.IsGuardrailSemanticHit(input, latestNpcDialogueUtterance, "kingdom_service", guardrailRuleInstruction, guardrailRuleKeywords, out matchedKeyword5, out score5);
		string guardrailRuleInstruction2 = AIConfigHandler.GetGuardrailRuleInstruction("marriage");
		List<string> guardrailRuleKeywords2 = AIConfigHandler.GetGuardrailRuleKeywords("marriage");
		string matchedKeyword6 = "";
		float score6 = 0f;
		bool flag8 = !suppressDynamicRuleAndLore && AIConfigHandler.IsGuardrailSemanticHit(input, latestNpcDialogueUtterance, "marriage", guardrailRuleInstruction2, guardrailRuleKeywords2, out matchedKeyword6, out score6);
		if (!suppressDynamicRuleAndLore && TryConsumeRuleStickyCarry(targetHero, targetCharacter, input, out var duel2, out var reward2, out var loan2))
		{
			if (!flag2 && duel2)
			{
				flag2 = true;
				matchedKeyword = "sticky";
				score = Math.Max(score, 0.18f);
			}
			if (!flag4 && reward2)
			{
				flag4 = true;
				matchedKeyword2 = "sticky";
				score2 = Math.Max(score2, 0.18f);
			}
			if (!flag5 && loan2)
			{
				flag5 = true;
				matchedKeyword3 = "sticky";
				score3 = Math.Max(score3, 0.18f);
			}
		}
		if (!suppressDynamicRuleAndLore)
		{
			UpdateRuleStickyCarryFromHits(targetHero, targetCharacter, duel, reward, loan);
		}
		flag3 = targetHero != null && flag2;
		bool flag9 = flag4 || flag;
		bool flag10 = flag5 || flag;
		string value = "";
		if (!suppressDynamicRuleAndLore && !flag3 && !flag9 && !flag10 && !flag6)
		{
			value = AIConfigHandler.BuildGuardrailClarificationHint(input, flag2, score, flag4, score2, flag5, score3, flag6, score4);
		}
		string text2 = (string.IsNullOrWhiteSpace(matchedKeyword) ? "" : $"{matchedKeyword}@{score:0.00}");
		string text3 = (string.IsNullOrWhiteSpace(matchedKeyword2) ? "" : $"{matchedKeyword2}@{score2:0.00}");
		string text4 = (string.IsNullOrWhiteSpace(matchedKeyword3) ? "" : $"{matchedKeyword3}@{score3:0.00}");
		string text5 = (string.IsNullOrWhiteSpace(matchedKeyword4) ? "" : $"{matchedKeyword4}@{score4:0.00}");
		string text6 = (string.IsNullOrWhiteSpace(matchedKeyword5) ? "" : $"{matchedKeyword5}@{score5:0.00}");
		string text7 = (string.IsNullOrWhiteSpace(matchedKeyword6) ? "" : $"{matchedKeyword6}@{score6:0.00}");
		string text8 = ((targetHero == null) ? null : ((object)targetHero.Name)?.ToString()) ?? "某人";
		Logger.Log("Logic", string.Format("[SemanticTrigger-Shout] DuelHit={0} [{1}] RewardHit={2} [{3}] LoanHit={4} [{5}] SurroundingsHit={6} [{7}] KingdomServiceHit={8} [{9}] MarriageHit={10} [{11}] NpcRecall={12} Input='{13}' NPC='{14}'", flag2, text2, flag4, text3, flag5, text4, flag6, text5, flag7, text6, flag8, text7, string.IsNullOrWhiteSpace(latestNpcDialogueUtterance) ? "off" : "on", input, text8));
		StringBuilder stringBuilder = new StringBuilder();
		string value2 = "";
		string text9 = "none";
		if (!suppressDynamicRuleAndLore && usePrefetchedLoreContext && !string.IsNullOrWhiteSpace(prefetchedLoreContext))
		{
			value2 = prefetchedLoreContext ?? "";
			text9 = "prefetched";
		}
		else if (!suppressDynamicRuleAndLore && targetHero != null)
		{
			value2 = AIConfigHandler.GetLoreContext(input, targetHero, latestNpcDialogueUtterance);
			text9 = ((usePrefetchedLoreContext && string.IsNullOrWhiteSpace(prefetchedLoreContext)) ? "prefetch_empty_fallback_hero" : "hero");
		}
		else if (!suppressDynamicRuleAndLore && targetCharacter != null)
		{
			value2 = AIConfigHandler.GetLoreContext(input, targetCharacter, kingdomIdOverride, latestNpcDialogueUtterance);
			text9 = ((usePrefetchedLoreContext && string.IsNullOrWhiteSpace(prefetchedLoreContext)) ? "prefetch_empty_fallback_character" : "character");
		}
		try
		{
			Logger.Log("LoreMatch", "shout_prompt_lore_ctx source=" + text9 + " heroId=" + (((targetHero != null) ? ((MBObjectBase)targetHero).StringId : null) ?? "null") + " charId=" + (((targetCharacter != null) ? ((MBObjectBase)targetCharacter).StringId : null) ?? "null") + " kingdomIdOverride=" + kingdomIdOverride);
		}
		catch
		{
		}
		if (RewardSystemBehavior.Instance != null && targetHero != null)
		{
			if (flag10)
			{
				string value3 = RewardSystemBehavior.Instance.BuildDueDateReferenceForAI();
				if (!string.IsNullOrEmpty(value3))
				{
					stringBuilder.AppendLine(value3);
				}
				string value4 = RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero);
				if (!string.IsNullOrEmpty(value4))
				{
					stringBuilder.AppendLine(value4);
				}
			}
			if (!flag10 && !flag9)
			{
				string value5 = RewardSystemBehavior.Instance.BuildTrustPromptForAI(targetHero);
				if (!string.IsNullOrEmpty(value5))
				{
					stringBuilder.AppendLine(value5);
				}
			}
		}
		if (RewardSystemBehavior.Instance != null && flag9 && targetHero == null && targetCharacter != null)
		{
			string value6 = RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter);
			if (!string.IsNullOrWhiteSpace(value6))
			{
				stringBuilder.AppendLine(value6);
			}
		}
		string value7 = RewardSystemBehavior.Instance?.BuildNpcBehaviorSupplementForAI(targetHero, targetCharacter);
		if (!string.IsNullOrWhiteSpace(value7))
		{
			stringBuilder.AppendLine("【AFEF NPC行为补充】");
			stringBuilder.AppendLine(value7);
		}
		bool includeDuelStakeContext = false;
		bool playerWonLastDuel = false;
		if (targetHero != null && DuelBehavior.TryConsumeLastDuelResult(targetHero, out var playerWon))
		{
			includeDuelStakeContext = true;
			playerWonLastDuel = playerWon;
			string text10 = BuildPlayerPublicDisplayNameForPrompt();
			if (string.IsNullOrWhiteSpace(text10))
			{
				text10 = "玩家";
			}
			if (playerWon)
			{
				stringBuilder.AppendLine("【战斗结果】你刚刚在一场正式的决斗中输给了" + text10 + "。请在态度和言语中体现这一点，并认真考虑履行你在决斗前约定的赌注或补偿。");
				if (AIConfigHandler.RewardEnabled)
				{
					flag9 = true;
				}
			}
			else
			{
				stringBuilder.AppendLine("【战斗结果】你刚刚在一场正式的决斗中打败了" + text10 + "。你可以据此调整对" + text10 + "的态度，或提醒" + text10 + "履行之前约定的赌注。");
				if (AIConfigHandler.RewardEnabled)
				{
					flag9 = true;
				}
			}
		}
		if (targetHero != null && !string.IsNullOrEmpty(((MBObjectBase)targetHero).StringId))
		{
			string text11 = BuildPlayerPublicDisplayNameForPrompt();
			if (string.IsNullOrWhiteSpace(text11))
			{
				text11 = "玩家";
			}
			if (_recentlyDefeatedByPlayer.Contains(((MBObjectBase)targetHero).StringId))
			{
				stringBuilder.AppendLine("【原版战斗结果】你刚刚在一场战斗中被" + text11 + "击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。");
			}
			if (_recentlyReleasedPrisoners.Contains(((MBObjectBase)targetHero).StringId))
			{
				stringBuilder.AppendLine("【释放通知】你之前被" + text11 + "俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。");
			}
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine(value);
		}
		if (flag6)
		{
			bool flag11 = false;
			CampaignVec2 pos;
			try
			{
				if (LordEncounterBehavior.IsEncounterMeetingMissionActive)
				{
					flag11 = LordEncounterBehavior.TryGetSavedMainPartyPosition(out pos);
				}
				else
				{
					pos = MobileParty.MainParty.Position;
					flag11 = ((CampaignVec2)(ref pos)).IsValid();
				}
			}
			catch
			{
				flag11 = false;
				pos = default(CampaignVec2);
			}
			if (flag11)
			{
				string value8 = ShoutUtils.BuildNearbySettlementsDetailForPrompt(pos, targetHero);
				if (!string.IsNullOrWhiteSpace(value8))
				{
					stringBuilder.AppendLine(value8);
				}
			}
		}
		string text12 = (suppressDynamicRuleAndLore ? "" : BuildTriggeredRuleInstructions(input, targetHero, flag3, isQualified, num, flag9, flag10, flag6, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, latestNpcDialogueUtterance, includeDuelStakeContext, playerWonLastDuel));
		bool excludeNpcKingdom = ShouldExcludeNpcShortReportFromWeeklyShortLayer(text12, targetHero, targetCharacter, kingdomIdOverride);
		string value9 = BuildWeeklyShortReportsPromptBlock(targetHero, targetCharacter, kingdomIdOverride, excludeNpcKingdom);
		if (!string.IsNullOrWhiteSpace(value9))
		{
			stringBuilder.AppendLine(value9);
		}
		if (!string.IsNullOrWhiteSpace(text12))
		{
			stringBuilder.AppendLine(text12);
		}
		string value10 = BuildTriggeredWeeklyFullReportsPromptBlock(text12, targetHero, targetCharacter, kingdomIdOverride);
		if (!string.IsNullOrWhiteSpace(value10))
		{
			stringBuilder.AppendLine(value10);
		}
		if (!string.IsNullOrEmpty(value2))
		{
			stringBuilder.AppendLine(value2);
		}
		bool flag12 = flag9 || flag10 || flag3;
		bool flag13 = targetHero != null && flag8;
		bool flag14 = num >= 2;
		bool flag15 = flag14;
		shoutPromptContext.Extras = stringBuilder.ToString();
		shoutPromptContext.UseDuelContext = flag3;
		shoutPromptContext.UseRewardContext = flag9;
		shoutPromptContext.IsLoanContext = flag10;
		shoutPromptContext.IsQualified = isQualified;
		return shoutPromptContext;
	}

	public static void AppendExternalLoreHistory(Hero hero, string loreText)
	{
		try
		{
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.AppendLoreToHistory(hero, loreText);
		}
		catch
		{
		}
	}

	private unsafe void AppendLoreToHistory(Hero hero, string loreText)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
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
			CampaignTime now = CampaignTime.Now;
			int dayIndex = (int)((CampaignTime)(ref now)).ToDays;
			now = CampaignTime.Now;
			string gameDate = ((object)(*(CampaignTime*)(&now))/*cast due to .constrained prefix*/).ToString();
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
					text2 = (((hero == null) ? null : ((object)hero.Name)?.ToString()) ?? "").Trim();
				}
				catch
				{
					text2 = "";
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = "该NPC";
				}
				string text3 = BuildPlayerPublicDisplayNameForPrompt();
				if (string.IsNullOrWhiteSpace(text3))
				{
					text3 = "玩家";
				}
				text = "【以下是关于（相关）的背景知识，" + text2 + "可酌情参考，但不要假设" + text3 + "提起过此话题】\n" + text;
			}
			string text4 = text;
			if (dialogueDay.Lines.Count > 0)
			{
				string a = dialogueDay.Lines[dialogueDay.Lines.Count - 1] ?? "";
				if (string.Equals(a, text4, StringComparison.Ordinal))
				{
					return;
				}
			}
			dialogueDay.Lines.Add(text4);
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
		string stripped;
		return TryStripPlayerSpeechPrefix(line, out stripped);
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
		string text3 = text;
		foreach (char c in text3)
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
		return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c >= '一' && c <= '鿿');
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
		string text2 = text;
		foreach (char c in text2)
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
		string stripped = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(stripped))
		{
			return "";
		}
		TryStripSceneSessionHistoryMarker(stripped, out stripped, out var _);
		if (stripped.StartsWith("- ", StringComparison.Ordinal))
		{
			stripped = stripped.Substring(2).TrimStart();
		}
		if (stripped.StartsWith("—— ", StringComparison.Ordinal))
		{
			return "";
		}
		if (stripped.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal))
		{
			return stripped.Substring("[AFEF玩家行为补充]".Length).Trim();
		}
		if (stripped.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
		{
			return stripped.Substring("[AFEF NPC行为补充]".Length).Trim();
		}
		if (TryStripPlayerSpeechPrefix(stripped, out var stripped2))
		{
			return stripped2;
		}
		int num = stripped.IndexOf(':');
		if (num > 0 && num < 20)
		{
			return stripped.Substring(num + 1).Trim();
		}
		return stripped;
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
		string text = BuildPlayerPublicDisplayNameForPrompt();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		string[] array2 = new string[3]
		{
			"你记得" + text + "当时提到过：",
			"你回想起" + text + "曾说过：",
			"你依稀记得" + text + "提过："
		};
		string[] array3 = new string[3] { "你记得当时谈到过一件具体事项：", "你回想起之前提过一件具体事项：", "你依稀记得还讨论过一件具体事项：" };
		string text2 = "";
		int num = 0;
		for (int i = 0; i < rawArchiveLines.Count; i++)
		{
			string text3 = (rawArchiveLines[i] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("—— ", StringComparison.Ordinal) && text3.EndsWith(" ——", StringComparison.Ordinal))
			{
				text2 = text3;
				continue;
			}
			if (num >= maxItems)
			{
				break;
			}
			string text4 = StripSpeakerPrefixForRecall(text3);
			text4 = TrimRecallSnippet(text4);
			if (!string.IsNullOrWhiteSpace(text4) && hashSet.Add(text4))
			{
				string text5 = (IsSystemFactLine(text3) ? ("你清楚记得一条已发生的事实：" + text4) : (IsPlayerTurnStartLine(text3) ? (array2[num % array2.Length] + text4) : ((!ContainsStructuredSignal(text3)) ? (array[num % array.Length] + text4) : (array3[num % array3.Length] + text4))));
				string text6 = (string.IsNullOrWhiteSpace(text2) ? "—— 日期未知 ——" : text2);
				if (list.Count <= 0 || !string.Equals(list[list.Count - 1], text6, StringComparison.Ordinal))
				{
					list.Add(text6);
				}
				list.Add("- " + text5);
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

	private static List<string> SplitHistoryRecallIntents(string query, int maxParts = 4)
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
		return IntentQueryOptimizer.OptimizeSplitIntents(list, Math.Max(1, maxParts));
	}

	private static List<WeightedRecallQueryInput> BuildHistoryRecallQueryInputs(List<HistoryLineEntry> recent, string currentInput, string secondaryInput)
	{
		List<WeightedRecallQueryInput> list = new List<WeightedRecallQueryInput>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = (string.IsNullOrWhiteSpace(currentInput) ? BuildHistoryQueryText(recent) : currentInput.Trim());
		appendInputs(SplitHistoryRecallIntents(text, 2), 2);
		string text2 = (secondaryInput ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2) && !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			appendInputs(SplitHistoryRecallIntents(text2, 2), 2);
		}
		return list;
		void appendInputs(List<string> intents, int perSourceLimit)
		{
			if (intents != null && intents.Count > 0 && perSourceLimit > 0)
			{
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
						if (num >= perSourceLimit || list.Count >= 4)
						{
							break;
						}
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
		if (!TryStripPlayerSpeechPrefix(line, out var stripped))
		{
			return false;
		}
		string b = currentInput.Trim();
		return string.Equals(stripped, b, StringComparison.Ordinal);
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
			matchMode = ((!flag) ? ((num2 > 1) ? "semantic_multi" : "semantic") : ((num2 > 1) ? "rerank_multi" : "rerank"));
			Dictionary<int, HistoryRecallAggregate> dictionary = new Dictionary<int, HistoryRecallAggregate>();
			for (int num3 = 0; num3 < list2.Count; num3++)
			{
				WeightedRecallQueryInput weightedRecallQueryInput = list2[num3];
				bool onnxUsed2;
				List<RecallLineScore> list4 = FindHistoryCandidateScores(list3, weightedRecallQueryInput, recallPerIntent, out onnxUsed2);
				if (onnxUsed2)
				{
					onnxUsed = true;
				}
				if (list4 == null || list4.Count <= 0)
				{
					continue;
				}
				bool rerankUsed;
				List<RecallLineScore> list5 = RerankHistoryCandidateScores(weightedRecallQueryInput.Text, list4, rerankPerIntent, out rerankUsed, weightedRecallQueryInput.Weight);
				if (list5 == null || list5.Count <= 0)
				{
					continue;
				}
				for (int num4 = 0; num4 < list5.Count; num4++)
				{
					RecallLineScore recallLineScore = list5[num4];
					int num5 = recallLineScore?.Entry?.Index ?? int.MinValue;
					if (num5 == int.MinValue)
					{
						continue;
					}
					double rerankScore = recallLineScore.RerankScore;
					if (!dictionary.TryGetValue(num5, out var value))
					{
						value = (dictionary[num5] = new HistoryRecallAggregate
						{
							Hit = new ArchiveHit
							{
								Entry = recallLineScore.Entry,
								Score = rerankScore,
								BaseScore = recallLineScore.BaseScore,
								RerankScore = recallLineScore.RerankScore
							},
							ScoreSum = rerankScore,
							HitCount = 1,
							BestRank = num4 + 1,
							BestScore = recallLineScore.RerankScore
						});
						continue;
					}
					value.ScoreSum += rerankScore;
					value.HitCount++;
					if (num4 + 1 < value.BestRank)
					{
						value.BestRank = num4 + 1;
					}
					if (recallLineScore.RerankScore >= value.BestScore)
					{
						value.BestScore = recallLineScore.RerankScore;
						value.Hit.Entry = recallLineScore.Entry;
						value.Hit.BaseScore = recallLineScore.BaseScore;
						value.Hit.RerankScore = recallLineScore.RerankScore;
					}
					dictionary[num5] = value;
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
			for (int num6 = 0; num6 < list6.Count; num6++)
			{
				ArchiveHit archiveHit = list6[num6];
				int num7 = archiveHit?.Entry?.Index ?? int.MinValue;
				if (num7 != int.MinValue && !hashSet.Contains(num7))
				{
					list.Add(archiveHit);
					hashSet.Add(num7 - 1);
					hashSet.Add(num7);
					hashSet.Add(num7 + 1);
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
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
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
			CampaignTime now = CampaignTime.Now;
			int num2 = (int)((CampaignTime)(ref now)).ToDays;
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
			string matchMode = "none";
			int rerankPerIntent = 0;
			int recallPerIntent = 0;
			List<ArchiveHit> list6 = FindRelevantArchiveHits(list4, list5, historyReturnCapFromSettings, out onnxUsed, out matchMode, out rerankPerIntent, out recallPerIntent);
			int num7 = list5.Sum((WeightedRecallQueryInput x) => x.Text?.Length ?? 0);
			string text = (((hero == null) ? null : ((object)hero.Name)?.ToString()) ?? "").Trim();
			List<string> list7 = BuildRenderedHistoryLines(list3, text);
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
				for (int num8 = 0; num8 < list4.Count; num8++)
				{
					HistoryLineEntry historyLineEntry2 = list4[num8];
					if (historyLineEntry2 != null)
					{
						dictionary[historyLineEntry2.Index] = historyLineEntry2;
					}
				}
				List<HistoryLineEntry> list10 = new List<HistoryLineEntry>();
				HashSet<int> hashSet2 = new HashSet<int>();
				for (int num9 = 0; num9 < list9.Count; num9++)
				{
					HistoryLineEntry historyLineEntry3 = list9[num9];
					if (historyLineEntry3 == null)
					{
						continue;
					}
					int[] array = new int[3]
					{
						historyLineEntry3.Index - 1,
						historyLineEntry3.Index,
						historyLineEntry3.Index + 1
					};
					int[] array2 = array;
					foreach (int key in array2)
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
					string text3 = NormalizePlayerHistoryLineForPrompt(item3.Line ?? "", text);
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
			if (list7.Count > 0)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					text = "该NPC";
				}
				stringBuilder.AppendLine("【" + BuildPlayerPublicDisplayNameForPrompt() + "与" + text + "（NPC名称的对话与互动）的近期对话】");
				foreach (string item4 in list7)
				{
					stringBuilder.AppendLine(item4);
				}
			}
			if (list8.Count > 0)
			{
				stringBuilder.AppendLine("【长期记忆摘要】");
				foreach (string item5 in list8)
				{
					stringBuilder.AppendLine(item5);
				}
			}
			string text4 = stringBuilder.ToString().TrimEnd();
			Logger.Log("DialogueHistory", string.Format("context hero={0} totalLines={1} recentLines={2} olderLines={3} archiveHits={4} onnxUsed={5} returnCap={6} matchMode={7} rerankPerIntent={8} recallPerIntent={9} queryCount={10} queryLen={11} npcRecall={12} chars={13}", ((MBObjectBase)hero).StringId ?? "", list2.Count, list3.Count, list4.Count, list8?.Count ?? 0, onnxUsed, historyReturnCapFromSettings, matchMode, rerankPerIntent, recallPerIntent, list5.Count, num7, string.IsNullOrWhiteSpace(secondaryInput) ? "off" : "on", text4.Length));
			Logger.Obs("History", "build_context", new Dictionary<string, object>
			{
				["heroId"] = ((MBObjectBase)hero).StringId ?? "",
				["totalLines"] = list2.Count,
				["recentLines"] = list3.Count,
				["olderLines"] = list4.Count,
				["archiveHits"] = list8?.Count ?? 0,
				["onnxUsed"] = onnxUsed,
				["returnCap"] = historyReturnCapFromSettings,
				["matchMode"] = matchMode,
				["rerankPerIntent"] = rerankPerIntent,
				["recallPerIntent"] = recallPerIntent,
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

	private async Task<ApiCallResult> CallUniversalApiDetailed(string sys, string user)
	{
		ApiCallResult apiCallResult = new ApiCallResult();
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
			string jsonBody = JsonConvert.SerializeObject((object)body);
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
			Logger.Log("Logic", httpLog.ToString());
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
			try
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
				request.Content = (HttpContent)new StringContent(jsonBody, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await DuelSettings.GlobalClient.SendAsync(request, (HttpCompletionOption)1);
				try
				{
					string statusLine = (int)response.StatusCode + " " + response.ReasonPhrase;
					Logger.Log("Logic", "[HTTP] 响应状态: " + statusLine);
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
						Logger.Log("Logic", "[HTTP] 响应流为空。");
						apiCallResult.ErrorMessage = "响应流为空";
						return apiCallResult;
					}
					using StreamReader reader = new StreamReader(stream);
					StringBuilder fullContent = new StringBuilder();
					while (true)
					{
						string text3;
						string text2 = (text3 = await reader.ReadLineAsync());
						string line = text3;
						if (text2 == null)
						{
							break;
						}
						if (string.IsNullOrWhiteSpace(line) || line == "data: [DONE]" || !line.StartsWith("data: "))
						{
							continue;
						}
						try
						{
							JObject json = JObject.Parse(line.Substring(6));
							JToken obj = json["choices"];
							object obj2;
							if (obj == null)
							{
								obj2 = null;
							}
							else
							{
								JToken obj3 = obj[(object)0];
								if (obj3 == null)
								{
									obj2 = null;
								}
								else
								{
									JToken obj4 = obj3[(object)"delta"];
									obj2 = ((obj4 == null) ? null : ((object)obj4[(object)"content"])?.ToString());
								}
							}
							if (obj2 == null)
							{
								obj2 = "";
							}
							string delta = (string)obj2;
							fullContent.Append(delta);
						}
						catch (Exception ex)
						{
							Exception ex2 = ex;
							Exception parseEx = ex2;
							Logger.Log("Logic", "[HTTP] 流解析异常: " + parseEx.ToString());
						}
					}
					string raw = fullContent.ToString();
					Logger.Log("Logic", "[HTTP] 流式原始内容=\n" + raw);
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
			Exception ex3 = ex;
			Exception ex4 = ex3;
			Logger.Log("Logic", "[ERROR] CallUniversalApi 异常: " + ex4.ToString());
			apiCallResult.ErrorMessage = ex4.Message;
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
		return Regex.Replace(text, "\\[ACTION:[^\\]]*\\]", "").Trim();
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
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			return (float)((CampaignTime)(ref now)).ToDays;
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
			snap.PrivateLoveLevel = RomanceSystemBehavior.GetPrivateLoveLevelText(snap.PrivateLove = (RomanceSystemBehavior.Instance?.GetPrivateLove(hero)).GetValueOrDefault());
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
			for (int num8 = 0; num8 < list2.Count; num8++)
			{
				if (list.Count >= num)
				{
					break;
				}
				RecallLineScore recallLineScore = list2[num8];
				int num9 = recallLineScore?.Entry?.Index ?? int.MinValue;
				if (num9 != int.MinValue && !(recallLineScore.RerankScore < num2) && hashSet.Add(num9))
				{
					list.Add(recallLineScore);
					num7++;
				}
			}
			if (list.Count < num)
			{
				for (int num10 = 0; num10 < list2.Count; num10++)
				{
					if (list.Count >= num)
					{
						break;
					}
					RecallLineScore recallLineScore2 = list2[num10];
					int num11 = recallLineScore2?.Entry?.Index ?? int.MinValue;
					if (num11 != int.MinValue && hashSet.Add(num11))
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
			for (int num = 0; num < count; num++)
			{
				HistoryLineEntry historyLineEntry = list2[num];
				string text = historyLineEntry.Line ?? "";
				double num2 = ((count <= 1) ? 1.0 : ((double)num / (double)(count - 1)));
				double num3 = (IsSystemFactLine(text) ? 1.0 : (ContainsStructuredSignal(text) ? 0.78 : 0.35));
				double num4 = ComputeLexicalOverlapScore(text, queryInput.Terms) * (double)Math.Max(0f, queryInput.Weight);
				double num5 = 0.6 * num3 + 0.3 * num2 + 0.1 * num4;
				double num6 = num5;
				if (array != null)
				{
					string text2 = text.Trim();
					if (text2.Length > 200)
					{
						text2 = text2.Substring(0, 200);
					}
					if (!string.IsNullOrWhiteSpace(text2) && instance.TryGetEmbedding(text2, out var vector2) && vector2 != null && vector2.Length != 0)
					{
						int num7 = Math.Min(array.Length, vector2.Length);
						double num8 = 0.0;
						for (int num9 = 0; num9 < num7; num9++)
						{
							num8 += (double)array[num9] * (double)vector2[num9];
						}
						double num10 = (num8 + 1.0) * 0.5 * (double)Math.Max(0f, queryInput.Weight);
						if (num10 < 0.0)
						{
							num10 = 0.0;
						}
						if (num10 > 1.0)
						{
							num10 = 1.0;
						}
						num6 = num10 * 0.9 + num5 * 0.1;
					}
				}
				list.Add(new RecallLineScore
				{
					Entry = historyLineEntry,
					RawScore = num6,
					BaseScore = num5,
					RerankScore = num6
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
			List<RecallLineScore> list2 = (from x in recalled ?? new List<RecallLineScore>()
				where x?.Entry != null
				orderby x.RawScore descending, x.BaseScore descending, x.Entry?.Index ?? (-1) descending
				select x).ToList();
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
			rerankUsed = flag;
			List<string> list3 = null;
			List<float> scores = null;
			bool flag2 = false;
			if (flag)
			{
				list3 = new List<string>(list2.Count);
				for (int num2 = 0; num2 < list2.Count; num2++)
				{
					list3.Add((list2[num2]?.Entry == null) ? "" : BuildHistoryRerankText(list2[num2].Entry));
				}
				flag2 = onnxCrossEncoderReranker.TryScoreBatch(input, list3, out scores) && scores != null && scores.Count == list2.Count;
			}
			rerankUsed = flag && flag2;
			double num3 = Math.Max(0f, scoreWeight);
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				RecallLineScore recallLineScore = list2[num4];
				if (recallLineScore?.Entry != null)
				{
					double rawScore = recallLineScore.RawScore;
					double rerankScore = rawScore;
					if (flag && flag2 && list3 != null && num4 < list3.Count && !string.IsNullOrWhiteSpace(list3[num4]) && scores != null && num4 < scores.Count)
					{
						rerankScore = (double)scores[num4] * num3;
					}
					list.Add(new RecallLineScore
					{
						Entry = recallLineScore.Entry,
						RawScore = rawScore,
						BaseScore = recallLineScore.BaseScore,
						RerankScore = rerankScore
					});
				}
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
			if (RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(character, out var kind))
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				if (currentSettlement != null)
				{
					int settlementLocalPublicTrust = RewardSystemBehavior.Instance.GetSettlementLocalPublicTrust(currentSettlement);
					int settlementSharedPublicTrust = RewardSystemBehavior.Instance.GetSettlementSharedPublicTrust(currentSettlement);
					int num = settlementLocalPublicTrust + settlementSharedPublicTrust;
					int trust = (snap.Trust = RewardSystemBehavior.Instance.GetSettlementMerchantEffectiveTrust(currentSettlement, kind));
					snap.PublicTrust = num;
					snap.TrustLevel = RewardSystemBehavior.GetTrustLevelText(trust);
					snap.PublicTrustLevel = RewardSystemBehavior.GetTrustLevelText(num);
				}
			}
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
		string text = (((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "").Trim().ToLower();
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
			DisplayName = (((hero == null) ? null : ((object)hero.Name)?.ToString()) ?? "NPC"),
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
		PatienceSnapshot snap = new PatienceSnapshot
		{
			Key = text,
			DisplayName = ((!string.IsNullOrWhiteSpace(displayName)) ? displayName.Trim() : (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim())),
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
			return snap;
		}
		float nowCampaignDay = GetNowCampaignDay();
		float value;
		lock (_patienceLock)
		{
			PatienceState orCreateStateUnsafe = GetOrCreateStateUnsafe(text, 30, nowCampaignDay);
			RecoverPatienceUnsafe(orCreateStateUnsafe, 30, nowCampaignDay);
			value = orCreateStateUnsafe.Value;
		}
		snap.Current = value;
		snap.PatienceLevel = GetPatienceLevelText(value, 30);
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
				ConversationManager conversationManager = current.ConversationManager;
				obj = ((conversationManager != null) ? conversationManager.OneToOneConversationCharacter : null);
			}
			CharacterObject character = (CharacterObject)obj;
			FillSettlementMerchantTrustSnapshot(ref snap, character);
		}
		catch
		{
		}
		return snap;
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
			if (text == "鎰夊揩")
			{
				break;
			}
			switch (text)
			{
			default:
				switch (text)
				{
				default:
					if (!(text == "鏃犺亰"))
					{
						return PatienceMood.Neutral;
					}
					goto case "bored";
				case "bored":
				case "boring":
					return PatienceMood.Bored;
				case "鐢熸皵":
					break;
				}
				break;
			case "annoyed":
			case "angry":
			case "upset":
			case "irritated":
			case "displeased":
			case "涓嶆偊":
				break;
			}
			return PatienceMood.Annoyed;
		case "joy":
		case "happy":
		case "positive":
		case "amused":
		case "friendly":
		case "鍠滄偊":
			break;
		}
		return PatienceMood.Joy;
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
		return mood switch
		{
			PatienceMood.Delighted => 2, 
			PatienceMood.Joy => 1, 
			PatienceMood.Bored => -1, 
			PatienceMood.Annoyed => -2, 
			_ => 0, 
		};
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
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, targetHero, num, true);
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
			Logger.Log("Patience", $"hero={((MBObjectBase)targetHero).StringId} mood={patienceMood} value={before}->{after}/{heroPatienceSnapshot.Max} relation={relation}->{num3} delta={num} privateLove={privateLove}->{num4} deltaLove={num2}");
			Logger.Obs("Patience", "hero_update", new Dictionary<string, object>
			{
				["heroId"] = ((MBObjectBase)targetHero).StringId ?? "",
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
			Logger.Log("Patience", "hero=" + ((MBObjectBase)targetHero).StringId + " reached_zero=true defer_exhausted_to_next_round=" + (directConversation ? "direct" : "scene"));
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
			statusLine = statusLine + "\n  " + BuildExhaustedPatienceInstruction(includeRelationPenalty: true);
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
			statusLine = statusLine + "\n  " + BuildExhaustedPatienceInstruction(includeRelationPenalty: false);
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
								PatienceStateSaveModel patienceStateSaveModel = new PatienceStateSaveModel
								{
									Value = patienceState.Value.Value,
									LastDay = patienceState.Value.LastDay,
									NoInterestRounds = patienceState.Value.NoInterestRounds,
									ExhaustedRefusalCount = patienceState.Value.ExhaustedRefusalCount
								};
								_patienceStorage[patienceState.Key] = JsonConvert.SerializeObject((object)patienceStateSaveModel);
							}
							catch
							{
							}
						}
					}
				}
				Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_patienceStorage);
				dataStore.SyncData<Dictionary<string, string>>("_patienceStates_v1", ref dictionary);
				return;
			}
			lock (_patienceLock)
			{
				_patienceStates.Clear();
				_patienceStorage.Clear();
			}
			Dictionary<string, string> stored = new Dictionary<string, string>();
			dataStore.SyncData<Dictionary<string, string>>("_patienceStates_v1", ref stored);
			_patienceStorage = CampaignSaveChunkHelper.RestoreStringDictionary(stored, "Patience");
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
						PatienceStateSaveModel patienceStateSaveModel2 = JsonConvert.DeserializeObject<PatienceStateSaveModel>(item.Value);
						if (patienceStateSaveModel2 != null)
						{
							_patienceStates[item.Key] = new PatienceState
							{
								Value = patienceStateSaveModel2.Value,
								LastDay = patienceStateSaveModel2.LastDay,
								NoInterestRounds = patienceStateSaveModel2.NoInterestRounds,
								ExhaustedRefusalCount = patienceStateSaveModel2.ExhaustedRefusalCount
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
			Campaign current = Campaign.Current;
			MyBehavior myBehavior = ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null);
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
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.ApplyPatienceFromHeroResponse(targetHero, ref aiResponse, directConversation: true);
		}
		catch
		{
		}
	}

	public static void ApplyPatienceFromSceneHeroResponseExternal(Hero targetHero, ref string aiResponse)
	{
		try
		{
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.ApplyPatienceFromHeroResponse(targetHero, ref aiResponse, directConversation: false);
		}
		catch
		{
		}
	}

	public static void ApplyPatienceFromSceneUnnamedResponseExternal(string unnamedKey, string npcName, ref string aiResponse)
	{
		try
		{
			Campaign current = Campaign.Current;
			((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.ApplyPatienceFromUnnamedResponse(unnamedKey, npcName, ref aiResponse);
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.TryGetHeroSceneStatus(hero, out statusLine, out canSpeak) ?? false;
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
			Campaign current = Campaign.Current;
			return ((current != null) ? current.GetCampaignBehavior<MyBehavior>() : null)?.TryGetUnnamedSceneStatus(unnamedKey, npcName, displayName, out statusLine, out canSpeak) ?? false;
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
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		try
		{
			args.MenuTitle = new TextObject("开发者工具", (Dictionary<string, object>)null);
		}
		catch
		{
		}
	}

	private bool DevRootEntryCondition(MenuCallbackArgs args)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		args.optionLeaveType = (LeaveType)2;
		return DuelSettings.GetSettings()?.EnableDevEditHistory ?? false;
	}

	private void DevRootEntryConsequence(MenuCallbackArgs args)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
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
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		args.optionLeaveType = (LeaveType)2;
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
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		args.optionLeaveType = (LeaveType)16;
		return true;
	}

	private void DevRootBackConsequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("town");
	}

	private void OpenDevHeroNpcMenu()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected O, but got Unknown
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"edit_hero", "编辑 HeroNPC（历史/赊账/个性背景）…", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"single_ie", "单个 HeroNPC 导入/导出…", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_hero_all", "全量导出（HeroNPC：历史+赊账+个性背景，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_hero_all", "全量导入（HeroNPC：历史+赊账+个性背景，选文件夹）", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("HeroNPC 编辑/导入/导出", "选择要执行的操作：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevHeroNpcMenuSelected, (Action<List<InquiryElement>>)delegate
		{
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Expected O, but got Unknown
		EnsureWeekZeroOpeningSummaryEvents();
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"edit_world_summary", "编辑世界开局概要", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"edit_kingdom_summary", "编辑王国开局概要", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"preview_weekly_materials", "查看本周事件素材预览", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"preview_weekly_prompts", "预览本周周报 Prompt", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"generate_weekly_reports", "生成本周周报草案", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"kingdom_stability_lab", "王国稳定度与叛乱实验", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"view_events", "查看事件与素材", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_event_data", "全量导出（事件编辑，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_event_data", "全量导入（事件编辑，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"clear_all", "清空全部事件概要", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		string text = BuildDevEventEditorMenuDescription();
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("事件编辑", text, list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevEventEditorMenuSelected, (Action<List<InquiryElement>>)delegate
		{
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OnDevEventEditorMenuSelected(List<InquiryElement> selected)
	{
		if (selected != null && selected.Count != 0)
		{
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
			num2 = ((IEnumerable<Kingdom>)Kingdom.All).Count((Kingdom x) => x != null && !string.IsNullOrWhiteSpace(((MBObjectBase)x).StringId));
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("世界开局概要：" + (string.IsNullOrWhiteSpace(_eventWorldOpeningSummary) ? "未设置" : "已设置"));
		stringBuilder.AppendLine("王国开局概要：" + num + "/" + num2 + " 已设置");
		stringBuilder.AppendLine("事件记录：" + ((_eventRecordEntries != null) ? SanitizeEventRecordEntries(_eventRecordEntries).Count : 0) + " 条");
		stringBuilder.AppendLine("周报篇幅档位：" + GetWeeklyReportPromptProfile().Label);
		int count = GetDevEditableKingdoms().Count;
		int num3 = GetDevEditableKingdoms().Count((Kingdom x) => GetKingdomStabilityValue(x) != 50);
		stringBuilder.AppendLine("王国稳定度：" + num3 + "/" + count + " 已偏离默认值");
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevEditWorldOpeningSummary()
	{
		DevTextEditorHelper.ShowLongTextEditor("编辑世界开局概要", "这段文本会作为世界事件系统的初始背景底稿。", "请输入世界开局概要（留空=清空）。", _eventWorldOpeningSummary ?? "", delegate(string input)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
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
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		List<Kingdom> devEditableKingdoms = GetDevEditableKingdoms();
		if (devEditableKingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的王国。"));
			OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (Kingdom item in devEditableKingdoms)
		{
			string text = BuildDevKingdomSummaryLabel(item);
			list.Add(new InquiryElement((object)new DevKingdomSummaryMenuItem
			{
				KingdomId = (((MBObjectBase)item).StringId ?? ""),
				DisplayName = (((object)item.Name)?.ToString() ?? ((MBObjectBase)item).StringId ?? "王国")
			}, text, (ImageIdentifier)null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("请选择要编辑的王国开局概要。");
		stringBuilder.AppendLine("这些文本会作为该王国后续每周事件生成的基础背景。");
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑王国开局概要", stringBuilder.ToString().TrimEnd(), list, true, 0, 1, "编辑", "返回", (Action<List<InquiryElement>>)OnDevKingdomOpeningSummaryMenuSelected, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventEditorMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OnDevKingdomOpeningSummaryMenuSelected(List<InquiryElement> selected)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		if (selected == null || selected.Count == 0)
		{
			OpenDevEventEditorMenu();
		}
		else if (selected[0].Identifier is string text && text == "back")
		{
			OpenDevEventEditorMenu();
		}
		else if (selected[0].Identifier is DevKingdomSummaryMenuItem devKingdomSummaryMenuItem)
		{
			Kingdom val = FindKingdomById(devKingdomSummaryMenuItem.KingdomId);
			if (val == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到对应的王国。"));
				OpenDevKingdomOpeningSummaryMenu();
			}
			else
			{
				OpenDevEditKingdomOpeningSummary(val);
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
		string kingdomOpeningSummary = GetKingdomOpeningSummary(kingdom);
		string text2 = ((object)kingdom.Name)?.ToString() ?? ((MBObjectBase)kingdom).StringId ?? "王国";
		string subtitleText = "这段文本会作为该王国的开局底稿，供后续事件系统生成该王国的周事件时参考。";
		DevTextEditorHelper.ShowLongTextEditor("编辑王国开局概要 - " + text2, subtitleText, "请输入该王国的开局概要（留空=清空）。", kingdomOpeningSummary, delegate(string input)
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Expected O, but got Unknown
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
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		InformationManager.ShowInquiry(new InquiryData("确认清空事件概要", "这会清空世界开局概要，以及所有王国的开局概要。\n此操作不可撤销，是否继续？", true, true, "确认清空", "取消", (Action)delegate
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
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
		}, (Action)delegate
		{
			OpenDevEventEditorMenu();
		}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void OpenDevKingdomStabilityLabMenu()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected O, but got Unknown
		List<Kingdom> devEditableKingdoms = GetDevEditableKingdoms();
		if (devEditableKingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可测试的王国。"));
			OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (Kingdom item in devEditableKingdoms)
		{
			list.Add(new InquiryElement((object)(((MBObjectBase)item).StringId ?? ""), BuildDevKingdomStabilityLabel(item), (ImageIdentifier)null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里用于实验王国稳定度与带城反出逻辑。");
		stringBuilder.AppendLine("默认所有王国稳定度都是 50（一般），不会自动叛乱。");
		stringBuilder.AppendLine("现在周报里的 STAB_* 评级标签也会改变王国稳定度；手动调整与周报评级都会影响后续周结算。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("请选择一个王国进入详情。");
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("王国稳定度与叛乱实验", stringBuilder.ToString().TrimEnd(), list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevEventEditorMenu();
			}
			else
			{
				string kingdomId = selected[0].Identifier as string;
				Kingdom val2 = FindKingdomById(kingdomId);
				if (val2 == null)
				{
					InformationManager.DisplayMessage(new InformationMessage("找不到对应的王国。"));
					OpenDevKingdomStabilityLabMenu();
				}
				else
				{
					OpenDevKingdomStabilityDetailMenu(val2);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventEditorMenu();
		}, "", true);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		List<KingdomRebellionCandidateInfo> source = EvaluateKingdomRebellionCandidates(kingdom, forceTrigger: false);
		List<KingdomRebellionCandidateInfo> list = source.Where((KingdomRebellionCandidateInfo x) => x?.Eligible ?? false).Take(5).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("王国：" + GetKingdomDisplayName(kingdom, "某王国"));
		stringBuilder.AppendLine("当前稳定度：" + kingdomStabilityValue + "（" + GetKingdomStabilityTierText(kingdomStabilityValue) + "）");
		stringBuilder.AppendLine("当前非王族关系修正：" + FormatKingdomStabilityRelationOffsetText(kingdomStabilityRelationTargetOffset) + "（作用于国王与本国非王族家族成年成员）");
		stringBuilder.AppendLine("本周叛乱概率：" + FormatKingdomRebellionChance(kingdomRebellionWeeklyChance));
		stringBuilder.AppendLine("当前国王：" + GetHeroDisplayName(kingdom.Leader));
		stringBuilder.AppendLine("执政家族：" + GetClanDisplayName(kingdom.RulingClan));
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(" ");
		if (list.Count > 0)
		{
			stringBuilder.AppendLine("当前可自动叛乱候选：");
			foreach (KingdomRebellionCandidateInfo item in list)
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
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement((object)"set_value", "调整稳定度", (ImageIdentifier)null),
			new InquiryElement((object)"test_roll", "测试本周叛乱判定", (ImageIdentifier)null),
			new InquiryElement((object)"force_rebellion", "强制触发该王国叛乱", (ImageIdentifier)null),
			new InquiryElement((object)"back", "返回王国列表", (ImageIdentifier)null)
		};
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("王国稳定度详情 - " + GetKingdomDisplayName(kingdom, "王国"), BuildDevKingdomStabilityDetailText(kingdom), list, true, 0, 1, "执行", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevKingdomStabilityLabMenu();
			}
			else
			{
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
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevKingdomStabilityLabMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevEditKingdomStability(Kingdom kingdom)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int kingdomStabilityValue = GetKingdomStabilityValue(kingdom);
		string text = "请输入 0-100 之间的稳定度数值。\n\n当前值：" + kingdomStabilityValue + "（" + GetKingdomStabilityTierText(kingdomStabilityValue) + "）\n默认值：50（一般）\n\n档位：\n90-100 极高\n75-89 高\n60-74 较高\n40-59 一般\n25-39 较差\n10-24 很差\n0-9 极差";
		InformationManager.ShowTextInquiry(new TextInquiryData("调整稳定度 - " + GetKingdomDisplayName(kingdom, "王国"), text, true, true, "保存", "返回", (Action<string>)delegate(string input)
		{
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Expected O, but got Unknown
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			if (!int.TryParse((input ?? "").Trim(), out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入 0-100 的整数。"));
				OpenDevEditKingdomStability(kingdom);
			}
			else
			{
				result = ClampKingdomStabilityValue(result);
				SetKingdomStabilityValue(kingdom, result);
				InformationManager.DisplayMessage(new InformationMessage(GetKingdomDisplayName(kingdom, "王国") + " 的稳定度已更新为 " + result + "（" + GetKingdomStabilityTierText(result) + "）。"));
				OpenDevKingdomStabilityDetailMenu(kingdom);
			}
		}, (Action)delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
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
				if (item != null)
				{
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
		}
		if (result.FollowerCandidates != null && result.FollowerCandidates.Count > 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("联合响应候选：");
			foreach (KingdomRebellionFollowerInfo item2 in result.FollowerCandidates.Take(8))
			{
				if (item2 != null)
				{
					stringBuilder.AppendLine("- " + item2.ClanName + " | " + (item2.Eligible ? "会加入" : "不会加入") + " | 对国王 " + item2.RelationToKing + " | 对领袖 " + item2.RelationToLeader + " | 评分 " + item2.Score.ToString("0.0"));
					if (!string.IsNullOrWhiteSpace(item2.Note))
					{
						stringBuilder.AppendLine("  " + item2.Note.Trim());
					}
				}
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private void RunDevKingdomRebellionTest(Kingdom kingdom)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int weekIndex = Math.Max(1, GetCurrentGameDayIndexSafe() / 7);
		KingdomRebellionResolutionResult result = ResolveKingdomRebellion(kingdom, weekIndex, executeAction: false, forceTrigger: false);
		InformationManager.ShowInquiry(new InquiryData("叛乱测试结果 - " + GetKingdomDisplayName(kingdom, "王国"), BuildKingdomRebellionResolutionText(result), true, false, "返回详情", "", (Action)delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void ConfirmForceDevKingdomRebellion(Kingdom kingdom)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("强制触发王国叛乱", "这会跳过稳定度概率，但不会放宽关系条件；系统仍只会从当前满足自动叛乱条件的家族中，选择一个最适合带城反出的家族执行。\n\n如果当前没有满足关系条件的候选家族，本次将不会发生叛乱。\n\n该操作会真实改动存档中的王国格局，是否继续？", true, true, "强制执行", "取消", (Action)delegate
		{
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Expected O, but got Unknown
			int weekIndex = Math.Max(1, GetCurrentGameDayIndexSafe() / 7);
			KingdomRebellionResolutionResult kingdomRebellionResolutionResult = ResolveKingdomRebellion(kingdom, weekIndex, executeAction: false, forceTrigger: true);
			Clan selectedClan = kingdomRebellionResolutionResult?.SelectedClan;
			KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = kingdomRebellionResolutionResult?.Candidates?.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Clan == selectedClan);
			if (selectedClan == null || kingdomRebellionCandidateInfo == null)
			{
				InformationManager.ShowInquiry(new InquiryData("强制叛乱结果 - " + GetKingdomDisplayName(kingdom, "王国"), BuildKingdomRebellionResolutionText(kingdomRebellionResolutionResult), true, false, "返回详情", "", (Action)delegate
				{
					OpenDevKingdomStabilityDetailMenu(kingdom);
				}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
			}
			else
			{
				StartDevForcedKingdomRebellionAsync(kingdom, selectedClan, weekIndex, kingdomRebellionCandidateInfo.RelationToKing, kingdomRebellionCandidateInfo.TownCount, kingdomRebellionCandidateInfo.CastleCount, kingdomRebellionResolutionResult.SelectedFollowerClans);
			}
		}, (Action)delegate
		{
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private static List<Kingdom> GetDevEditableKingdoms()
	{
		try
		{
			return ((IEnumerable<Kingdom>)Kingdom.All).Where((Kingdom x) => x != null && !string.IsNullOrWhiteSpace(((MBObjectBase)x).StringId)).OrderBy((Kingdom x) => ((object)x.Name)?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((Kingdom x) => ((MBObjectBase)x).StringId ?? "", StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return new List<Kingdom>();
		}
	}

	private string BuildDevKingdomSummaryLabel(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "无效王国";
		}
		string text = ((object)kingdom.Name)?.ToString() ?? ((MBObjectBase)kingdom).StringId ?? "王国";
		string kingdomOpeningSummary = GetKingdomOpeningSummary(kingdom);
		string text2 = (string.IsNullOrWhiteSpace(kingdomOpeningSummary) ? "未设置" : "已设置");
		string text3 = BuildDevSummaryPreview(kingdomOpeningSummary, 72);
		if (string.IsNullOrWhiteSpace(text3))
		{
			return text + " [" + text2 + "]";
		}
		return text + " [" + text2 + "] " + text3;
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
		string text = (((MBObjectBase)kingdom).StringId ?? "").Trim();
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
		string text = (((MBObjectBase)kingdom).StringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string value = (summary ?? "").Trim();
			if (string.IsNullOrWhiteSpace(value))
			{
				_eventKingdomOpeningSummaries.Remove(text);
			}
			else
			{
				_eventKingdomOpeningSummaries[text] = value;
			}
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
			return ((IEnumerable<Kingdom>)Kingdom.All).FirstOrDefault((Kingdom x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
			return ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Clan x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
					if (material != null)
					{
						eventRecordEntry.Materials.Add(new EventMaterialReference
						{
							MaterialType = (material.MaterialType ?? "").Trim(),
							Label = (material.Label ?? "").Trim(),
							SnapshotText = (material.SnapshotText ?? "").Trim(),
							HeroId = (material.HeroId ?? "").Trim(),
							KingdomId = (material.KingdomId ?? "").Trim(),
							SettlementId = (material.SettlementId ?? "").Trim(),
							RecentOnly = material.RecentOnly,
							ActionStableKey = (material.ActionStableKey ?? "").Trim(),
							ActionDay = material.ActionDay,
							ActionOrder = material.ActionOrder,
							ActionSequence = material.ActionSequence
						});
					}
				}
			}
			list.Add(eventRecordEntry);
		}
		return (from x in list
			orderby x.WeekIndex descending, x.CreatedDay descending
			select x).ThenBy((EventRecordEntry x) => x.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
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
			if (item != null && !string.IsNullOrWhiteSpace(text))
			{
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
		}
		return (from x in list
			orderby x.Day, (x.Sequence > 0) ? x.Sequence : int.MaxValue
			select x).ThenBy((EventSourceMaterialEntry x) => x.Label ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void OpenDevEventViewerMenu(int page)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Expected O, but got Unknown
		List<EventRecordEntry> list = SanitizeEventRecordEntries(_eventRecordEntries);
		if (page < 0)
		{
			page = 0;
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / 14.0));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		if (page > 0)
		{
			list2.Add(new InquiryElement((object)"prev_page", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement((object)"next_page", "下一页", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (EventRecordEntry item in list.Skip(page * 14).Take(14))
		{
			list2.Add(new InquiryElement((object)item, BuildDevEventRecordItemLabel(item), (ImageIdentifier)null));
		}
		string text = BuildDevEventViewerDescription(list, page, num);
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("事件查看器", text, list2, true, 0, 1, "查看", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
			else if (selected[0].Identifier is EventRecordEntry entry)
			{
				OpenDevEventRecordDetail(entry, page);
			}
			else
			{
				OpenDevEventViewerMenu(page);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventEditorMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private string BuildDevEventViewerDescription(List<EventRecordEntry> entries, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里用于查看事件系统已经记录下来的事件，以及每条事件引用了哪些素材。");
		stringBuilder.AppendLine("素材可能包括：世界开局概要、王国开局概要、NPC近期行动、NPC重大行动，以及未来接入的其他摘要。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("事件总数：" + (entries?.Count ?? 0));
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
		string text2 = (string.IsNullOrWhiteSpace(entry.CreatedDate) ? ("第 " + Math.Max(0, entry.CreatedDay) + " 日") : entry.CreatedDate.Trim());
		string text3 = (string.IsNullOrWhiteSpace(entry.ScopeKingdomId) ? "" : ResolveKingdomDisplay(entry.ScopeKingdomId));
		string text4 = (string.IsNullOrWhiteSpace(text3) ? "" : (" [" + text3 + "]"));
		int num = ((entry.Materials != null) ? entry.Materials.Count : 0);
		return text2 + " [" + text + "] 第" + Math.Max(0, entry.WeekIndex) + "周" + text4 + " " + (entry.Title ?? "").Trim() + " (素材 " + num + " 条)";
	}

	private void OpenDevEventRecordDetail(EventRecordEntry entry, int returnPage)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected O, but got Unknown
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回事件列表", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			list.Add(new InquiryElement((object)"view_short", "查看短摘要", (ImageIdentifier)null));
		}
		list.Add(new InquiryElement((object)"view_report", "查看周报正文", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"edit_title", "编辑标题", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"edit_report", "编辑周报正文", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(entry.TagText))
		{
			list.Add(new InquiryElement((object)"view_tags", "查看标签层", (ImageIdentifier)null));
		}
		if (!string.IsNullOrWhiteSpace(entry.PromptText))
		{
			list.Add(new InquiryElement((object)"view_prompt", "查看请求 Prompt", (ImageIdentifier)null));
		}
		if ((entry.Materials?.Count).GetValueOrDefault() > 0)
		{
			list.Add(new InquiryElement((object)"view_materials", "查看素材列表", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("事件详情", BuildDevEventRecordDetailText(entry), list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventViewerMenu(returnPage);
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "back":
					OpenDevEventViewerMenu(returnPage);
					break;
				case "edit_title":
					OpenDevEditEventRecordTitle(entry, returnPage);
					break;
				case "view_report":
					OpenDevEventReportDetail(entry, returnPage);
					break;
				case "view_short":
					OpenDevEventShortSummaryDetail(entry, returnPage);
					break;
				case "edit_report":
					OpenDevEditEventRecordReport(entry, returnPage);
					break;
				case "view_tags":
					OpenDevEventTagDetail(entry, returnPage);
					break;
				case "view_prompt":
					OpenDevEventPromptDetail(entry, returnPage);
					break;
				case "view_materials":
					OpenDevEventMaterialList(entry, returnPage, 0);
					break;
				default:
					OpenDevEventRecordDetail(entry, returnPage);
					break;
				}
			}
			else
			{
				OpenDevEventRecordDetail(entry, returnPage);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventViewerMenu(returnPage);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private string BuildDevEventRecordDetailText(EventRecordEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "事件标题", (entry.Title ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "事件类型", TranslateEventKindForDev(entry.EventKind));
		AppendDevNpcActionField(stringBuilder, "周数", "第 " + Math.Max(0, entry.WeekIndex) + " 周");
		AppendDevNpcActionField(stringBuilder, "生成日期", (!string.IsNullOrWhiteSpace(entry.CreatedDate)) ? entry.CreatedDate.Trim() : ("第 " + Math.Max(0, entry.CreatedDay) + " 日"));
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

	private void OpenDevEventReportDetail(EventRecordEntry entry, int returnPage)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		InformationManager.ShowInquiry(new InquiryData("周报正文 - " + (entry?.Title ?? "").Trim(), string.IsNullOrWhiteSpace(entry?.Summary) ? "当前这条事件还没有正文。" : entry.Summary.Trim(), true, false, "返回事件详情", "", (Action)delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void OpenDevEventShortSummaryDetail(EventRecordEntry entry, int returnPage)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		InformationManager.ShowInquiry(new InquiryData("短摘要 - " + (entry?.Title ?? "").Trim(), string.IsNullOrWhiteSpace(entry?.ShortSummary) ? "当前这条事件还没有短摘要。" : entry.ShortSummary.Trim(), true, false, "返回事件详情", "", (Action)delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void OpenDevEventTagDetail(EventRecordEntry entry, int returnPage)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		InformationManager.ShowInquiry(new InquiryData("标签层 - " + (entry?.Title ?? "").Trim(), string.IsNullOrWhiteSpace(entry?.TagText) ? "当前这条事件还没有标签层。" : entry.TagText.Trim(), true, false, "返回事件详情", "", (Action)delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void OpenDevEventMaterialList(EventRecordEntry entry, int returnPage, int page)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Expected O, but got Unknown
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Expected O, but got Unknown
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
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / 14.0));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回事件详情", (ImageIdentifier)null));
		if (page > 0)
		{
			list2.Add(new InquiryElement((object)"prev_page", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement((object)"next_page", "下一页", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (EventMaterialReference item in list.Skip(page * 14).Take(14))
		{
			list2.Add(new InquiryElement((object)item, BuildDevEventMaterialItemLabel(item), (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("事件素材列表", BuildDevEventMaterialListText(entry, page, num), list2, true, 0, 1, "查看素材", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
			else if (selected[0].Identifier is EventMaterialReference material)
			{
				OpenDevEventMaterialDetail(entry, material, returnPage, page);
			}
			else
			{
				OpenDevEventMaterialList(entry, returnPage, page);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private string BuildDevEventMaterialListText(EventRecordEntry entry, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendDevNpcActionField(stringBuilder, "事件标题", (entry?.Title ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "素材数量", ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		AppendDevNpcActionField(stringBuilder, "页码", page + 1 + "/" + Math.Max(1, totalPages));
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
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Expected O, but got Unknown
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
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
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
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		InformationManager.ShowInquiry(new InquiryData("请求 Prompt - " + (entry?.Title ?? "").Trim(), string.IsNullOrWhiteSpace(entry?.PromptText) ? "当前没有保存该条周报的请求 Prompt。" : entry.PromptText.Trim(), true, false, "返回事件详情", "", (Action)delegate
		{
			OpenDevEventRecordDetail(entry, returnPage);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
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
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		string text = BuildDevEventMaterialDetailText(material);
		string text2 = BuildDevEventMaterialItemLabel(material);
		InformationManager.ShowInquiry(new InquiryData("素材详情 - " + text2, text, true, false, "返回事件详情", "", (Action)delegate
		{
			OpenDevEventMaterialList(entry, returnPage, returnMaterialPage);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
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
		switch ((material.MaterialType ?? "").Trim().ToLowerInvariant())
		{
		case "world_opening_summary":
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine((!string.IsNullOrWhiteSpace(material.SnapshotText)) ? material.SnapshotText.Trim() : (_eventWorldOpeningSummary ?? "").Trim());
			break;
		case "kingdom_opening_summary":
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine((!string.IsNullOrWhiteSpace(material.SnapshotText)) ? material.SnapshotText.Trim() : ResolveKingdomOpeningSummaryById(material.KingdomId));
			break;
		case "npc_recent_action":
		case "npc_major_action":
		{
			NpcActionEntry npcActionEntry = ResolveEventMaterialNpcAction(material);
			if (npcActionEntry != null)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(BuildDevNpcActionDetailText(npcActionEntry));
				break;
			}
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
			break;
		}
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
		Kingdom val = FindKingdomById(kingdomId);
		return (val == null) ? "" : GetKingdomOpeningSummary(val);
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
			string path = (Directory.Exists(text) ? text : importDir);
			string path2 = Path.Combine(path, "WorldOpeningSummary.json");
			if (File.Exists(path2))
			{
				payload.HasWorldSummaryFile = true;
				EventWorldOpeningSummaryJson eventWorldOpeningSummaryJson = ReadJson<EventWorldOpeningSummaryJson>(path2);
				payload.WorldSummary = (eventWorldOpeningSummaryJson?.Summary ?? "").Trim();
			}
			string path3 = Path.Combine(path, "KingdomOpeningSummaries.json");
			if (File.Exists(path3))
			{
				payload.HasKingdomSummariesFile = true;
				Dictionary<string, string> dictionary = ReadJson<Dictionary<string, string>>(path3) ?? new Dictionary<string, string>();
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					string text2 = (item.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						payload.KingdomSummaries[text2] = (item.Value ?? "").Trim();
					}
				}
			}
			string path4 = Path.Combine(path, "EventRecords.json");
			if (File.Exists(path4))
			{
				payload.HasEventRecordsFile = true;
				List<EventRecordEntry> source = ReadJson<List<EventRecordEntry>>(path4) ?? new List<EventRecordEntry>();
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
			foreach (KeyValuePair<string, string> kingdomSummary in payload.KingdomSummaries)
			{
				string text = (kingdomSummary.Key ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string value = (kingdomSummary.Value ?? "").Trim();
				if (overwriteExisting)
				{
					if (string.IsNullOrWhiteSpace(value))
					{
						_eventKingdomOpeningSummaries.Remove(text);
					}
					else
					{
						_eventKingdomOpeningSummaries[text] = value;
					}
				}
				else if (!_eventKingdomOpeningSummaries.ContainsKey(text) && !string.IsNullOrWhiteSpace(value))
				{
					_eventKingdomOpeningSummaries[text] = value;
				}
			}
		}
		if (!payload.HasEventRecordsFile)
		{
			return;
		}
		if (_eventRecordEntries == null)
		{
			_eventRecordEntries = new List<EventRecordEntry>();
		}
		if (overwriteExisting)
		{
			Dictionary<string, EventRecordEntry> dictionary = new Dictionary<string, EventRecordEntry>(StringComparer.OrdinalIgnoreCase);
			foreach (EventRecordEntry eventRecordEntry in _eventRecordEntries)
			{
				string text2 = (eventRecordEntry?.EventId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					dictionary[text2] = eventRecordEntry;
				}
			}
			foreach (EventRecordEntry eventRecord in payload.EventRecords)
			{
				string text3 = (eventRecord?.EventId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text3))
				{
					dictionary[text3] = eventRecord;
				}
			}
			_eventRecordEntries = SanitizeEventRecordEntries(dictionary.Values.ToList());
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(from x in _eventRecordEntries
			where x != null && !string.IsNullOrWhiteSpace(x.EventId)
			select x.EventId.Trim(), StringComparer.OrdinalIgnoreCase);
		foreach (EventRecordEntry eventRecord2 in payload.EventRecords)
		{
			string text4 = (eventRecord2?.EventId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text4) && hashSet.Add(text4))
			{
				_eventRecordEntries.Add(eventRecord2);
			}
		}
		_eventRecordEntries = SanitizeEventRecordEntries(_eventRecordEntries);
	}

	private NpcActionEntry ResolveEventMaterialNpcAction(EventMaterialReference material)
	{
		if (material == null || string.IsNullOrWhiteSpace(material.HeroId))
		{
			return null;
		}
		Hero val = Hero.FindFirst((Func<Hero, bool>)((Hero h) => h != null && string.Equals((((MBObjectBase)h).StringId ?? "").Trim(), (material.HeroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)));
		if (val == null)
		{
			return null;
		}
		List<NpcActionEntry> devNpcActionEntries = GetDevNpcActionEntries(val, material.RecentOnly);
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
		return (eventKind ?? "").Trim().ToLowerInvariant() switch
		{
			"world" => "世界事件", 
			"kingdom" => "王国事件", 
			"player_local" => "玩家周边事件", 
			_ => string.IsNullOrWhiteSpace(eventKind) ? "未分类事件" : eventKind.Trim(), 
		};
	}

	private static string TranslateEventMaterialTypeForDev(string materialType)
	{
		return (materialType ?? "").Trim().ToLowerInvariant() switch
		{
			"world_opening_summary" => "世界开局概要", 
			"kingdom_opening_summary" => "王国开局概要", 
			"npc_recent_action" => "NPC近期行动", 
			"npc_major_action" => "NPC重大行动", 
			"raw_text" => "原始文本素材", 
			_ => string.IsNullOrWhiteSpace(materialType) ? "未分类素材" : materialType.Trim(), 
		};
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
			Settlement val = Settlement.Find(text);
			if (val != null)
			{
				string text2 = ((object)val.Name)?.ToString() ?? "";
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
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		List<WeeklyEventMaterialPreviewGroup> list = BuildWeeklyEventMaterialPreviewGroups();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			list2.Add(new InquiryElement((object)item, BuildWeeklyEventMaterialPreviewGroupLabel(item), (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("本周事件素材预览", BuildWeeklyEventMaterialPreviewMenuDescription(list), list2, true, 0, 1, "查看", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
				OpenDevWeeklyEventMaterialPreviewGroupDetail(weeklyEventMaterialPreviewGroup, 0);
			}
			else
			{
				OpenDevWeeklyEventMaterialPreviewMenu();
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventEditorMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private string BuildWeeklyEventMaterialPreviewMenuDescription(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		int num3 = groups?.Sum((WeeklyEventMaterialPreviewGroup x) => (x?.Materials?.Count).GetValueOrDefault()) ?? 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示“如果现在生成本周事件”，系统会拿去喂给事件生成器的素材池。");
		stringBuilder.AppendLine("当前按世界事件与各王国事件分组展示。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("当前周数：第 " + num2 + " 周");
		stringBuilder.AppendLine("当前取材区间：第 " + num + " 日 到 第 " + currentGameDayIndexSafe + " 日");
		stringBuilder.AppendLine("分组数量：" + (groups?.Count ?? 0));
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
		string text = (string.IsNullOrWhiteSpace(group.Summary) ? "" : BuildDevSummaryPreview(group.Summary, 44));
		string text2 = ((group.Materials != null) ? group.Materials.Count : 0) + " 条素材";
		return (group.Title ?? "未命名分组") + " [" + text2 + "]" + (string.IsNullOrWhiteSpace(text) ? "" : (" " + text));
	}

	private List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups()
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int startDay = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		return BuildWeeklyEventMaterialPreviewGroups(startDay, currentGameDayIndexSafe);
	}

	private List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups(int startDay, int endDay)
	{
		List<WeeklyEventMaterialPreviewGroup> list = new List<WeeklyEventMaterialPreviewGroup>();
		list.Add(BuildWorldWeeklyEventMaterialPreviewGroup(startDay, endDay));
		foreach (Kingdom devEditableKingdom in GetDevEditableKingdoms())
		{
			WeeklyEventMaterialPreviewGroup item = BuildKingdomWeeklyEventMaterialPreviewGroup(devEditableKingdom, startDay, endDay);
			list.Add(item);
		}
		return list;
	}

	private static WeeklyReportPromptProfile GetWeeklyReportPromptProfile()
	{
		int num = 2;
		try
		{
			num = ClampInt(DuelSettings.GetSettings()?.WeeklyReportLengthPreset ?? 2, 1, 4);
		}
		catch
		{
			num = 2;
		}
		if (1 == 0)
		{
		}
		WeeklyReportPromptProfile result = num switch
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
		if (1 == 0)
		{
		}
		return result;
	}

	private static int GetWeeklyReportRequestsPerMinute()
	{
		try
		{
			return ClampInt(DuelSettings.GetSettings()?.WeeklyReportRequestsPerMinute ?? 5, 1, 20);
		}
		catch
		{
			return 5;
		}
	}

	private static int GetWeeklyReportRequestIntervalMs()
	{
		int weeklyReportRequestsPerMinute = GetWeeklyReportRequestsPerMinute();
		double a = 60000.0 / (double)Math.Max(1, weeklyReportRequestsPerMinute);
		return Math.Max(0, (int)Math.Ceiling(a));
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
		string[] array = text2.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text3 in array)
		{
			string text4 = text3.Trim();
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list.Add(text4);
			}
		}
		return string.Join("\n", list).Trim();
	}

	private static int GetWeeklyReportStabilityDeltaForTag(string tag)
	{
		return (tag ?? "").Trim().ToUpperInvariant() switch
		{
			"STAB_DOWN_4" => -25, 
			"STAB_DOWN_3" => -15, 
			"STAB_DOWN_2" => -10, 
			"STAB_DOWN_1" => -5, 
			"STAB_UP_1" => 5, 
			"STAB_UP_2" => 10, 
			"STAB_UP_3" => 15, 
			"STAB_UP_4" => 25, 
			_ => 0, 
		};
	}

	private static int ExtractWeeklyReportStabilityDelta(string tagText)
	{
		string text = NormalizeWeeklyReportTagText(tagText);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int result = 0;
		string[] array = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			string text3 = (text2 ?? "").Trim();
			if (string.Equals(text3, "STAB_FLAT", StringComparison.OrdinalIgnoreCase))
			{
				result = 0;
			}
			else if (text3.StartsWith("STAB_", StringComparison.OrdinalIgnoreCase))
			{
				result = GetWeeklyReportStabilityDeltaForTag(text3);
			}
		}
		return result;
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
		object kingdom;
		if (targetHero == null)
		{
			kingdom = null;
		}
		else
		{
			Clan clan = targetHero.Clan;
			kingdom = ((clan != null) ? clan.Kingdom : null);
		}
		text = GetKingdomId((Kingdom)kingdom);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId((targetHero != null) ? targetHero.MapFaction : null);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		Hero val = ((targetCharacter != null) ? targetCharacter.HeroObject : null);
		object kingdom2;
		if (val == null)
		{
			kingdom2 = null;
		}
		else
		{
			Clan clan2 = val.Clan;
			kingdom2 = ((clan2 != null) ? clan2.Kingdom : null);
		}
		text = GetKingdomId((Kingdom)kingdom2);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId((val != null) ? val.MapFaction : null);
		return (text ?? "").Trim();
	}

	private static string ResolveWeeklyReportSurroundingsKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		string kingdomId = GetKingdomId((currentSettlement != null) ? currentSettlement.MapFaction : null);
		if (!string.IsNullOrWhiteSpace(kingdomId))
		{
			return kingdomId;
		}
		object faction;
		if (targetHero == null)
		{
			faction = null;
		}
		else
		{
			Settlement currentSettlement2 = targetHero.CurrentSettlement;
			faction = ((currentSettlement2 != null) ? currentSettlement2.MapFaction : null);
		}
		kingdomId = GetKingdomId((IFaction)faction);
		if (!string.IsNullOrWhiteSpace(kingdomId))
		{
			return kingdomId;
		}
		kingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		if (!string.IsNullOrWhiteSpace(kingdomId))
		{
			return kingdomId;
		}
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(from x in GetDevEditableKingdoms()
			select (x != null) ? ((MBObjectBase)x).StringId : null);
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
		return (from x in SanitizeEventRecordEntries(_eventRecordEntries)
			where x != null && string.Equals((x.EventKind ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) && string.Equals((x.ScopeKingdomId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase)
			orderby x.WeekIndex descending, x.CreatedDay descending
			select x).FirstOrDefault();
	}

	private List<string> SelectWeeklyShortReportKingdomIds(string npcKingdomId, bool excludeNpcKingdom)
	{
		string text = (npcKingdomId ?? "").Trim();
		List<string> list = (from x in GetKingdomIdsByPlayerProximity(from x in GetDevEditableKingdoms()
				select (x != null) ? ((MBObjectBase)x).StringId : null)
			where !string.IsNullOrWhiteSpace(x)
			select x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			list = (from x in GetDevEditableKingdoms()
				select (((x != null) ? ((MBObjectBase)x).StringId : null) ?? "").Trim() into x
				where !string.IsNullOrWhiteSpace(x)
				select x).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		if (excludeNpcKingdom && !string.IsNullOrWhiteSpace(text))
		{
			list.RemoveAll((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		List<string> list2 = list.Take(3).ToList();
		if (!excludeNpcKingdom && !string.IsNullOrWhiteSpace(text) && !list2.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			if (list2.Count >= 3)
			{
				list2.RemoveAt(list2.Count - 1);
			}
			list2.Insert(0, text);
		}
		return (from x in list2
			where !string.IsNullOrWhiteSpace(x)
			select x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
	}

	private string BuildWeeklyShortReportsPromptBlock(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride, bool excludeNpcKingdom)
	{
		string npcKingdomId = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		List<string> list = SelectWeeklyShortReportKingdomIds(npcKingdomId, excludeNpcKingdom);
		if (list.Count == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (string item in list)
		{
			EventRecordEntry eventRecordEntry = FindLatestWeeklyReportRecord("kingdom", item);
			string value = BuildFallbackWeeklyReportShortSummary(eventRecordEntry?.ShortSummary ?? eventRecordEntry?.Summary);
			if (eventRecordEntry != null && !string.IsNullOrWhiteSpace(value))
			{
				if (num == 0)
				{
					stringBuilder.AppendLine("【近期三个王国短周报】");
					stringBuilder.AppendLine("以下为最近三个相关王国的短周报，只作背景事实参考；不要把其中事实错说到别的王国。");
				}
				num++;
				stringBuilder.Append("- ").Append(ResolveKingdomDisplay(item));
				if (eventRecordEntry.WeekIndex >= 0)
				{
					stringBuilder.Append("（第").Append(eventRecordEntry.WeekIndex).Append("周）");
				}
				stringBuilder.Append("：").AppendLine(value);
			}
		}
		return (num > 0) ? stringBuilder.ToString().TrimEnd() : "";
	}

	private static string BuildSingleWeeklyFullReportPromptBlock(string header, EventRecordEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		string value = (entry.Summary ?? "").Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【" + (header ?? "").Trim() + "】");
		if (!string.IsNullOrWhiteSpace(entry.Title))
		{
			stringBuilder.AppendLine("标题：" + entry.Title.Trim());
		}
		if (entry.WeekIndex >= 0)
		{
			stringBuilder.AppendLine("周次：第" + entry.WeekIndex + "周");
		}
		stringBuilder.AppendLine(value);
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
		string text = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		string text2 = ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder();
		if (flag || flag2)
		{
			EventRecordEntry entry = FindLatestWeeklyReportRecord("kingdom", text);
			string value = BuildSingleWeeklyFullReportPromptBlock("NPC所属王国完整周报", entry);
			if (!string.IsNullOrWhiteSpace(value) && hashSet.Add("kingdom:" + (text ?? "").Trim()))
			{
				stringBuilder.AppendLine(value);
			}
			EventRecordEntry entry2 = FindLatestWeeklyReportRecord("world", "");
			string value2 = BuildSingleWeeklyFullReportPromptBlock("世界完整周报", entry2);
			if (!string.IsNullOrWhiteSpace(value2) && hashSet.Add("world"))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine(value2);
			}
		}
		if (flag3)
		{
			EventRecordEntry entry3 = FindLatestWeeklyReportRecord("kingdom", text2);
			string value3 = BuildSingleWeeklyFullReportPromptBlock("周边相关王国完整周报", entry3);
			if (!string.IsNullOrWhiteSpace(value3) && hashSet.Add("kingdom:" + (text2 ?? "").Trim()))
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine(value3);
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
			string text = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
			string b = ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);
			if (!string.IsNullOrWhiteSpace(text) && string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
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
		foreach (string value in tokens)
		{
			if (!string.IsNullOrWhiteSpace(value) && text2.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
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
			HttpResponseHeaders headers = response.Headers;
			if (headers != null)
			{
				RetryConditionHeaderValue retryAfter = headers.RetryAfter;
				if (((retryAfter != null) ? retryAfter.Delta : ((TimeSpan?)null)).HasValue)
				{
					return Math.Max(0, (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds));
				}
			}
			IEnumerable<string> enumerable = default(IEnumerable<string>);
			if (response.Headers != null && ((HttpHeaders)response.Headers).TryGetValues("Retry-After", ref enumerable))
			{
				string text = enumerable?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x));
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
		if (((response != null) ? response.Headers : null) == null)
		{
			return false;
		}
		try
		{
			foreach (KeyValuePair<string, IEnumerable<string>> item in (HttpHeaders)response.Headers)
			{
				string text = (item.Key ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (ContainsAnyIgnoreCase(text, "ratelimit", "rate-limit", "limit-requests", "remaining-requests", "reset-requests"))
					{
						return true;
					}
					string responseBody = string.Join(" ", item.Value ?? Enumerable.Empty<string>());
					if (IsRequestsPerMinuteLimitResponseBody(responseBody))
					{
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

	private static string BuildApiCallFailureMessage(HttpStatusCode statusCode, string responseBody, int? retryAfterSeconds, bool isRateLimit, bool isRequestsPerMinuteLimit, bool isQuotaLimit)
	{
		int num = (int)statusCode;
		string value = num + " " + statusCode;
		string value2 = (responseBody ?? "").Trim();
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
		stringBuilder.Append("（HTTP ").Append(value).Append("）");
		if (retryAfterSeconds.HasValue)
		{
			stringBuilder.Append("，建议等待 ").Append(retryAfterSeconds.Value).Append(" 秒后再试");
		}
		if (!string.IsNullOrWhiteSpace(value2))
		{
			stringBuilder.Append("：").Append(value2);
		}
		return stringBuilder.ToString();
	}

	private static void TryPersistMcmSettings(DuelSettings settings)
	{
		try
		{
			(((object)settings)?.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null))?.Invoke(settings, null);
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
		string text3 = (weeklyReportRequestResult.PromptPreview = BuildWeeklyReportPromptPreviewText(group, text, text2));
		string text4 = BuildWeeklyReportGroupDisplayLabel(group);
		for (int i = 1; i <= Math.Max(1, maxAttempts); i++)
		{
			ApiCallResult apiCallResult = await CallUniversalApiDetailed(text, text2);
			Logger.LogEventPromptExchange(replyText: apiCallResult.Success ? (apiCallResult.Content ?? "") : ("错误: " + (apiCallResult.ErrorMessage ?? "未知错误")), targetLabel: text4 + " [尝试 " + i + "/" + maxAttempts + "]", requestText: text3);
			if (!apiCallResult.Success)
			{
				weeklyReportRequestResult.FailureReason = BuildWeeklyReportFailureReason(apiCallResult.ErrorMessage, parseFailed: false);
				weeklyReportRequestResult.AttemptsUsed = i;
				weeklyReportRequestResult.IsRateLimit = apiCallResult.IsRateLimit;
				weeklyReportRequestResult.IsRequestsPerMinuteLimit = apiCallResult.IsRequestsPerMinuteLimit;
				weeklyReportRequestResult.IsQuotaLimit = apiCallResult.IsQuotaLimit;
				weeklyReportRequestResult.RetryAfterSeconds = apiCallResult.RetryAfterSeconds;
			}
			else
			{
				if (TryParseWeeklyReportResponse(apiCallResult.Content, group, weekIndex, out var title, out var shortSummary, out var report, out var tagText))
				{
					weeklyReportRequestResult.Success = true;
					weeklyReportRequestResult.Title = title;
					weeklyReportRequestResult.ShortSummary = shortSummary;
					weeklyReportRequestResult.Report = report;
					weeklyReportRequestResult.TagText = tagText;
					weeklyReportRequestResult.AttemptsUsed = i;
					return weeklyReportRequestResult;
				}
				weeklyReportRequestResult.FailureReason = BuildWeeklyReportFailureReason(apiCallResult.Content, parseFailed: true);
				weeklyReportRequestResult.AttemptsUsed = i;
				title = null;
				shortSummary = null;
				report = null;
				tagText = null;
			}
			if (i < maxAttempts)
			{
				Logger.Log("EventWeeklyReport", text4 + " 第" + i + "次请求失败，准备自动重试。原因：" + weeklyReportRequestResult.FailureReason);
				int num = 1200;
				if (weeklyReportRequestResult.IsRateLimit)
				{
					num = Math.Max(num, GetWeeklyReportRequestIntervalMs());
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
			AttemptsUsed = (requestResult?.AttemptsUsed ?? 0),
			IsRateLimit = (requestResult?.IsRateLimit ?? false),
			IsRequestsPerMinuteLimit = (requestResult?.IsRequestsPerMinuteLimit ?? false),
			IsQuotaLimit = (requestResult?.IsQuotaLimit ?? false),
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
		if (context != null)
		{
			_weeklyReportRetryContext = context;
			_weeklyReportUiStage = WeeklyReportUiStage.Failure;
			_weeklyReportUiResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(showImmediate ? 60.0 : 180.0).Ticks;
			if (showImmediate && !InformationManager.IsAnyInquiryActive())
			{
				ShowWeeklyReportFailurePopup(ignoreDelay: true);
			}
		}
	}

	private void ShowWeeklyReportFailurePopup(bool ignoreDelay = false)
	{
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Expected O, but got Unknown
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
			text = (string.IsNullOrWhiteSpace(weeklyReportRetryContext.DisplayLabel) ? ("第" + weeklyReportRetryContext.WeekIndex + "周周报") : weeklyReportRetryContext.DisplayLabel);
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
		string text2 = (weeklyReportRetryContext.IsRequestsPerMinuteLimit ? "修改RPM并重试" : "手动重试");
		InformationManager.ShowInquiry(new InquiryData("周事件生成失败", stringBuilder.ToString().TrimEnd(), true, true, text2, "调整API信息", (Action)delegate
		{
			if (weeklyReportRetryContext.IsRequestsPerMinuteLimit)
			{
				OpenWeeklyReportRpmLimitInput();
			}
			else
			{
				BeginRetryBlockedWeeklyReports();
			}
		}, (Action)delegate
		{
			OpenWeeklyReportApiRepairFlow();
		}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
	}

	private void OpenWeeklyReportRpmLimitInput()
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
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
			InformationManager.ShowTextInquiry(new TextInquiryData("修改周报RPM限制", "检测到本次失败疑似触发 RPM 限流。\n\n请输入新的【每分钟最多生成周报数】（范围 1-20）。\n当前值：" + weeklyReportRequestsPerMinute + "\n建议先改为：" + num + "\n\n保存后会立即按新速率继续补跑当前周报。", true, true, "保存并重试", "返回", (Action<string>)delegate(string input)
			{
				//IL_007f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0089: Expected O, but got Unknown
				//IL_0026: Unknown result type (might be due to invalid IL or missing references)
				//IL_0030: Expected O, but got Unknown
				string s = (input ?? "").Trim();
				if (!int.TryParse(s, out var result))
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
			}, (Action)delegate
			{
				ShowWeeklyReportFailurePopup(ignoreDelay: true);
			}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("打开 RPM 输入框失败：" + ex.Message));
			ShowWeeklyReportFailurePopup(ignoreDelay: true);
		}
	}

	private void ShowWeeklyReportRetryProgressPopup()
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		if (_weeklyReportRetryContext != null && _weeklyReportManualRetryInProgress)
		{
			_weeklyReportUiStage = WeeklyReportUiStage.RetryProgress;
			_weeklyReportUiResumeAfterUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(150.0).Ticks;
			string text = "正在重试生成第" + _weeklyReportRetryContext.WeekIndex + "周周报中的这个事件，请稍候……\n\n- 当前失败分组：" + (_weeklyReportRetryContext.FailedGroupTitle ?? "未命名分组") + "\n- 后台会再次按每条分组三次重试的规则执行\n- 如果你不想继续等待，可以直接退出当前存档\n- 也可以返回上一界面，稍后再决定是否继续重试";
			InformationManager.ShowInquiry(new InquiryData("正在重试生成此事件", text, true, true, "退出当前存档", "返回上一界面", (Action)ExitCurrentGameFromWeeklyReportGate, (Action)CancelWeeklyReportManualRetryAndReturn, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
	}

	private void BeginRetryBlockedWeeklyReports()
	{
		if (_weeklyReportRetryContext != null && !_weeklyReportManualRetryInProgress)
		{
			_weeklyReportManualRetryInProgress = true;
			int retryVersion = ++_weeklyReportManualRetryVersion;
			_weeklyReportUiStage = WeeklyReportUiStage.RetryProgress;
			ShowWeeklyReportRetryProgressPopup();
			RetryBlockedWeeklyReportsAsync(_weeklyReportRetryContext, retryVersion);
		}
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
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
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
			MBGameManager.EndGame();
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("退出当前存档失败：" + ex.Message));
			QueueWeeklyReportFailurePopup(_weeklyReportRetryContext, showImmediate: true);
		}
	}

	private async Task RetryBlockedWeeklyReportsAsync(WeeklyReportRetryContext context, int retryVersion)
	{
		try
		{
			WeeklyReportGenerationResult weeklyReportGenerationResult = await GenerateWeeklyReportsAsyncInternal(context.Groups, context.WeekIndex, context.StartDay, context.EndDay, context.DisplayLabel, context.OpenViewerWhenDone, queueBlockingPopupOnFatalFailure: false, context.IsAutoGeneration);
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
				_pendingWeeklyReportManualRetryContext = weeklyReportGenerationResult?.RetryContext ?? context;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			if (retryVersion == _weeklyReportManualRetryVersion)
			{
				Logger.Log("EventWeeklyReport", "[ERROR] RetryBlockedWeeklyReportsAsync failed: " + ex2);
				_pendingWeeklyReportManualRetrySucceeded = false;
				_pendingWeeklyReportManualRetryMessage = "周事件补跑异常失败：" + ex2.Message;
				_pendingWeeklyReportManualRetryContext = context;
			}
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
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
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
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			CampaignVec2? val = ((mainParty != null) ? new CampaignVec2?(mainParty.Position) : ((CampaignVec2?)null));
			if (val.HasValue)
			{
				CampaignVec2 value = val.Value;
				if (((CampaignVec2)(ref value)).IsValid())
				{
					value = val.Value;
					return ((CampaignVec2)(ref value)).ToVec2();
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static List<string> GetKingdomIdsByPlayerProximity(IEnumerable<string> kingdomIds)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		List<string> result = new List<string>();
		Vec2? playerPartyPositionVec = GetPlayerPartyPositionVec2();
		if (!playerPartyPositionVec.HasValue)
		{
			return result;
		}
		Vec2 value = playerPartyPositionVec.Value;
		Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		try
		{
			HashSet<string> hashSet = new HashSet<string>(from x in kingdomIds ?? Enumerable.Empty<string>()
				where !string.IsNullOrWhiteSpace(x)
				select x.Trim(), StringComparer.OrdinalIgnoreCase);
			foreach (Settlement item in (List<Settlement>)(object)Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				IFaction mapFaction = item.MapFaction;
				IFaction obj = ((mapFaction is Kingdom) ? mapFaction : null);
				object obj2 = ((obj != null) ? ((MBObjectBase)obj).StringId : null);
				if (obj2 == null)
				{
					Clan ownerClan = item.OwnerClan;
					if (ownerClan == null)
					{
						obj2 = null;
					}
					else
					{
						Kingdom kingdom = ownerClan.Kingdom;
						obj2 = ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null);
					}
					if (obj2 == null)
					{
						obj2 = "";
					}
				}
				string text = ((string)obj2).Trim();
				if (!string.IsNullOrWhiteSpace(text) && hashSet.Contains(text))
				{
					CampaignVec2 gatePosition = item.GatePosition;
					Vec2 val = ((CampaignVec2)(ref gatePosition)).ToVec2();
					float num = val.x - value.x;
					float num2 = val.y - value.y;
					float num3 = num * num + num2 * num2;
					if (!dictionary.TryGetValue(text, out var value2) || num3 < value2)
					{
						dictionary[text] = num3;
					}
				}
			}
		}
		catch
		{
		}
		return (from x in dictionary.OrderBy((KeyValuePair<string, float> x) => x.Value).ThenBy((KeyValuePair<string, float> x) => x.Key, StringComparer.OrdinalIgnoreCase)
			select x.Key).ToList();
	}

	private static List<WeeklyEventMaterialPreviewGroup> OrderWeeklyReportGenerationGroups(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		List<WeeklyEventMaterialPreviewGroup> source = (groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(from x in source
			where string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)
			select x.KingdomId);
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int num = 0; num < kingdomIdsByPlayerProximity.Count; num++)
		{
			string text = (kingdomIdsByPlayerProximity[num] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = num;
			}
		}
		return source.OrderBy(delegate(WeeklyEventMaterialPreviewGroup x)
		{
			if (string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = (x.KingdomId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2) && dictionary.TryGetValue(text2, out var value))
				{
					return (value != 0) ? (value + 1) : 0;
				}
				return 1000;
			}
			return string.Equals((x.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? 1 : 2000;
		}).ThenBy((WeeklyEventMaterialPreviewGroup x) => x.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static string BuildWeeklyReportSystemPrompt(WeeklyEventMaterialPreviewGroup group)
	{
		WeeklyReportPromptProfile weeklyReportPromptProfile = GetWeeklyReportPromptProfile();
		bool flag = string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		string text = (flag ? "世界周报" : "王国周报");
		string value = (string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase) ? "你的任务是根据本周素材，写出一篇宏观的大陆周报，总结这一周整个卡拉迪亚发生了什么。" : "你的任务是根据本周素材，写出一篇聚焦单个王国的周报，总结这一周这个王国发生了什么。");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("你是一名负责整理卡拉迪亚时局的史官。");
		stringBuilder.AppendLine("你不是在自由编造故事，而是在根据给定素材生成一篇流利、可信、克制的周报。");
		stringBuilder.AppendLine(value);
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
		stringBuilder.AppendLine("- 当前档位：" + weeklyReportPromptProfile.Label);
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
		Kingdom val = FindKingdomById(group?.KingdomId);
		if (val == null)
		{
			return "";
		}
		return "当前王国稳定度评级：" + GetKingdomStabilityTierText(GetKingdomStabilityValue(val));
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
		string value = BuildWeeklyReportCurrentKingdomStabilityTierText(group);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine(value);
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
			if (item != null)
			{
				string text = BuildWeeklyReportMaterialLine(item);
				if (!string.IsNullOrWhiteSpace(text))
				{
					stringBuilder.AppendLine("- " + text.Trim());
				}
			}
		}
		string text2 = stringBuilder.ToString().TrimEnd();
		return string.IsNullOrWhiteSpace(text2) ? "无可用素材。" : text2;
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
		AppendDevNpcActionField(stringBuilder, "MaxTokens", 5000.ToString());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【System Prompt】");
		stringBuilder.AppendLine(systemPrompt ?? "");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【User Prompt】");
		stringBuilder.AppendLine(userPrompt ?? "");
		return stringBuilder.ToString().TrimEnd();
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
		Kingdom val = null;
		if (string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			val = FindKingdomById(group?.KingdomId);
			if (val != null)
			{
				num = ExtractWeeklyReportStabilityDelta(tagText);
			}
		}
		if (num != value && val != null)
		{
			SetKingdomStabilityValue(val, GetKingdomStabilityValue(val) - value + num);
			if (num == 0)
			{
				_weeklyReportAppliedStabilityDeltas.Remove(text);
			}
			else
			{
				_weeklyReportAppliedStabilityDeltas[text] = num;
			}
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
		eventRecordEntry.Materials = (from x in OrderWeeklyPreviewMaterials(@group?.Materials)
			select (x == null) ? null : new EventMaterialReference
			{
				MaterialType = (x.MaterialType ?? "").Trim(),
				Label = (x.Label ?? "").Trim(),
				SnapshotText = (x.SnapshotText ?? "").Trim(),
				HeroId = (x.HeroId ?? "").Trim(),
				KingdomId = (x.KingdomId ?? "").Trim(),
				SettlementId = (x.SettlementId ?? "").Trim(),
				RecentOnly = x.RecentOnly,
				ActionStableKey = (x.ActionStableKey ?? "").Trim(),
				ActionDay = x.ActionDay,
				ActionOrder = x.ActionOrder,
				ActionSequence = x.ActionSequence
			} into x
			where x != null
			select x).ToList();
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
		foreach (KeyValuePair<string, List<NpcActionEntry>> item2 in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero val = FindHeroById(item2.Key);
			if (val == null || item2.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item3 in item2.Value)
			{
				if (item3 != null && item3.Day >= startDay && item3.Day <= endDay && ShouldIncludeWorldPreviewAction(val, item3))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, val, item3, recentOnly: true);
				}
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> item4 in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero val2 = FindHeroById(item4.Key);
			if (val2 == null || item4.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item5 in item4.Value)
			{
				if (item5 != null && item5.Day >= startDay && item5.Day <= endDay && ShouldIncludeWorldPreviewAction(val2, item5))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, val2, item5, recentOnly: false);
				}
			}
		}
		weeklyEventMaterialPreviewGroup.Summary = "世界事件将使用世界开局概要，以及本周更能代表大陆格局变化的重大行动、领导层震荡与世界级通用素材。军团从属加入或离开这类细碎动作会被压缩。";
		return weeklyEventMaterialPreviewGroup;
	}

	private WeeklyEventMaterialPreviewGroup BuildKingdomWeeklyEventMaterialPreviewGroup(Kingdom kingdom, int startDay, int endDay)
	{
		string text = ((kingdom == null) ? null : ((object)kingdom.Name)?.ToString()) ?? ((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null) ?? "王国";
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = new WeeklyEventMaterialPreviewGroup
		{
			GroupKind = "kingdom",
			KingdomId = (((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null) ?? ""),
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
				KingdomId = (((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null) ?? ""),
				SnapshotText = kingdomOpeningSummary
			});
		}
		foreach (EventSourceMaterialEntry item in SanitizeEventSourceMaterials(_eventSourceMaterials))
		{
			if (item != null && item.IncludeInKingdom && item.Day >= startDay && item.Day <= endDay && string.Equals((item.KingdomId ?? "").Trim(), (((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null) ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				TryAddPreviewSourceMaterial(weeklyEventMaterialPreviewGroup.Materials, item);
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> item2 in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero val = FindHeroById(item2.Key);
			if (val == null || item2.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item3 in item2.Value)
			{
				if (item3 != null && item3.Day >= startDay && item3.Day <= endDay && DoesNpcActionRelateToKingdom(item3, kingdom) && ShouldIncludeKingdomPreviewAction(val, item3, kingdom))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, val, item3, recentOnly: true);
				}
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> item4 in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero val2 = FindHeroById(item4.Key);
			if (val2 == null || item4.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item5 in item4.Value)
			{
				if (item5 != null && item5.Day >= startDay && item5.Day <= endDay && DoesNpcActionRelateToKingdom(item5, kingdom))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, val2, item5, recentOnly: false);
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
			return Hero.FindFirst((Func<Hero, bool>)((Hero h) => h != null && string.Equals((((MBObjectBase)h).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)));
		}
		catch
		{
			return null;
		}
	}

	private static bool DoesNpcActionRelateToKingdom(NpcActionEntry entry, Kingdom kingdom)
	{
		string text = (((kingdom != null) ? ((MBObjectBase)kingdom).StringId : null) ?? "").Trim();
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
		string a = (entry.ActionKind ?? "").Trim();
		if (!string.Equals(a, "daily_behavior", StringComparison.OrdinalIgnoreCase))
		{
			if (string.Equals(a, "army_join", StringComparison.OrdinalIgnoreCase))
			{
				Clan clan = hero.Clan;
				return ((clan != null) ? clan.Leader : null) == hero;
			}
			return true;
		}
		if (IsArmyCommanderDailyBehavior(entry))
		{
			return true;
		}
		string text = (entry.StableKey ?? "").Trim();
		if (text.IndexOf("raidsettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("袭扰", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text.IndexOf("defendsettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("守备", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("保卫", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text2 = (((MBObjectBase)kingdom).StringId ?? "").Trim();
		string text3 = (entry.ActorKingdomId ?? "").Trim();
		string a2 = (entry.SettlementOwnerKingdomId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3) && !string.Equals(text3, text2, StringComparison.OrdinalIgnoreCase) && string.Equals(a2, text2, StringComparison.OrdinalIgnoreCase))
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
		string a = (entry.ActionKind ?? "").Trim();
		if (string.Equals(a, "army_join", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "army_leave", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(a, "daily_behavior", StringComparison.OrdinalIgnoreCase))
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
		string a = (entry?.ActionKind ?? "").Trim();
		return string.Equals(a, "prisoner_taken_captor", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "prisoner_taken_prisoner", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsLeadershipCaptureAction(NpcActionEntry entry)
	{
		Hero val = FindHeroById(GetCapturedHeroId(entry));
		int result;
		if (val != null)
		{
			Clan clan = val.Clan;
			result = ((((clan != null) ? clan.Leader : null) == val || val.IsFactionLeader) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	private static bool IsPrisonerReleasedAction(NpcActionEntry entry)
	{
		string a = (entry?.ActionKind ?? "").Trim();
		return string.Equals(a, "prisoner_released_captor", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsLeadershipReleaseAction(NpcActionEntry entry)
	{
		Hero val = FindHeroById(GetReleasedHeroId(entry));
		int result;
		if (val != null)
		{
			Clan clan = val.Clan;
			result = ((((clan != null) ? clan.Leader : null) == val || val.IsFactionLeader) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	private static string GetCapturedHeroId(NpcActionEntry entry)
	{
		string a = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(a, "prisoner_taken_captor", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.TargetHeroId ?? "").Trim();
		}
		if (string.Equals(a, "prisoner_taken_prisoner", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.ActorHeroId ?? "").Trim();
		}
		return "";
	}

	private static string GetReleasedHeroId(NpcActionEntry entry)
	{
		string a = (entry?.ActionKind ?? "").Trim();
		if (string.Equals(a, "prisoner_released_captor", StringComparison.OrdinalIgnoreCase))
		{
			return (entry?.TargetHeroId ?? "").Trim();
		}
		if (string.Equals(a, "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase))
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
		EventMaterialReference eventMaterialReference = materials.FirstOrDefault((EventMaterialReference x) => x != null && string.Equals((x.HeroId ?? "").Trim(), (((MBObjectBase)hero).StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) && x.ActionDay.GetValueOrDefault() == entry.Day && string.Equals((x.ActionStableKey ?? "").Trim(), (entry.StableKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		string label = ResolveHeroDisplay(((MBObjectBase)hero).StringId) + " - " + BuildDevSummaryPreview(BuildDevNpcActionPreviewText(entry), 60);
		EventMaterialReference eventMaterialReference2 = new EventMaterialReference
		{
			MaterialType = (recentOnly ? "npc_recent_action" : "npc_major_action"),
			Label = label,
			SnapshotText = BuildDevNpcActionPreviewText(entry),
			HeroId = (((MBObjectBase)hero).StringId ?? ""),
			KingdomId = (entry.ActorKingdomId ?? ""),
			SettlementId = (entry.SettlementId ?? ""),
			RecentOnly = recentOnly,
			ActionStableKey = (entry.StableKey ?? ""),
			ActionDay = entry.Day,
			ActionOrder = entry.Order,
			ActionSequence = entry.Sequence
		};
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
		}
		else
		{
			materials.Add(eventMaterialReference2);
		}
	}

	private static void TryAddPreviewSourceMaterial(List<EventMaterialReference> materials, EventSourceMaterialEntry entry)
	{
		if (materials != null && entry != null && !materials.Any((EventMaterialReference x) => x != null && string.Equals((x.MaterialType ?? "").Trim(), "raw_text", StringComparison.OrdinalIgnoreCase) && x.ActionDay.GetValueOrDefault() == entry.Day && x.ActionSequence.GetValueOrDefault() == entry.Sequence && string.Equals((x.ActionStableKey ?? "").Trim(), (entry.StableKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			materials.Add(new EventMaterialReference
			{
				MaterialType = "raw_text",
				Label = (entry.Label ?? "").Trim(),
				SnapshotText = (entry.SnapshotText ?? "").Trim(),
				KingdomId = (entry.KingdomId ?? "").Trim(),
				SettlementId = (entry.SettlementId ?? "").Trim(),
				ActionStableKey = (entry.StableKey ?? "").Trim(),
				ActionDay = entry.Day,
				ActionSequence = entry.Sequence
			});
		}
	}

	private void OpenDevWeeklyEventMaterialPreviewGroupDetail(WeeklyEventMaterialPreviewGroup group, int page)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Expected O, but got Unknown
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Expected O, but got Unknown
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
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / 16.0));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回分组列表", (ImageIdentifier)null));
		if (page > 0)
		{
			list2.Add(new InquiryElement((object)"prev_page", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement((object)"next_page", "下一页", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		foreach (EventMaterialReference item in list.Skip(page * 16).Take(16))
		{
			list2.Add(new InquiryElement((object)item, BuildWeeklyPreviewMaterialLabel(item), (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(group.Title ?? "素材预览", BuildWeeklyPreviewGroupDetailText(group, page, num), list2, true, 0, 1, "查看素材", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
			else if (selected[0].Identifier is EventMaterialReference material)
			{
				OpenDevWeeklyPreviewMaterialDetail(group, material, page);
			}
			else
			{
				OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevWeeklyEventMaterialPreviewMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static string BuildWeeklyPreviewMaterialLabel(EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材";
		}
		string text = TranslateEventMaterialTypeForDev(material.MaterialType);
		string text2 = (material.ActionDay.HasValue ? ("[第" + material.ActionDay.Value + "日] ") : "");
		if ((material.MaterialType ?? "").Trim().StartsWith("npc_", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = ResolveHeroDisplay(material.HeroId);
			string text4 = ResolveSettlementDisplay(material.SettlementId);
			string text5 = BuildDevSummaryPreview(material.SnapshotText, 18);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return text2 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text3) ? "某领主" : text3) + " - " + text4;
			}
			if (!string.IsNullOrWhiteSpace(text5))
			{
				return text2 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text3) ? "某领主" : text3) + " - " + text5;
			}
			return text2 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text3) ? "某领主" : text3);
		}
		string text6 = BuildDevSummaryPreview((!string.IsNullOrWhiteSpace(material.Label)) ? material.Label : material.SnapshotText, 24);
		return text2 + "[" + text + "] " + (string.IsNullOrWhiteSpace(text6) ? "无预览" : text6);
	}

	private static IEnumerable<EventMaterialReference> OrderWeeklyPreviewMaterials(List<EventMaterialReference> materials)
	{
		return (from x in materials ?? new List<EventMaterialReference>()
			orderby GetWeeklyPreviewMaterialSortBucket(x), x?.ActionDay ?? int.MinValue, x?.ActionSequence ?? int.MaxValue, x?.ActionOrder ?? int.MinValue
			select x).ThenBy((EventMaterialReference x) => x?.Label ?? "", StringComparer.OrdinalIgnoreCase);
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
			foreach (NpcActionEntry item2 in (from x in item.Value
				orderby x?.Day ?? int.MinValue, x?.Order ?? int.MinValue
				select x).ThenBy((NpcActionEntry x) => x?.GameDate ?? "", StringComparer.Ordinal))
			{
				if (item2 != null && item2.Sequence <= 0)
				{
					num = (item2.Sequence = num + 1);
				}
			}
		}
	}

	private static int GetMaxNpcActionSequence(params Dictionary<string, List<NpcActionEntry>>[] storages)
	{
		int num = 0;
		Dictionary<string, List<NpcActionEntry>>[] array = storages ?? Array.Empty<Dictionary<string, List<NpcActionEntry>>>();
		foreach (Dictionary<string, List<NpcActionEntry>> dictionary in array)
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
		AppendDevNpcActionField(stringBuilder, "页码", page + 1 + "/" + Math.Max(1, totalPages));
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
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		string text = BuildDevEventMaterialDetailText(material);
		InformationManager.ShowInquiry(new InquiryData("本周素材详情", text, true, false, "返回素材列表", "", (Action)delegate
		{
			OpenDevWeeklyEventMaterialPreviewGroupDetail(group, returnPage);
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void OpenDevWeeklyReportPromptPreviewMenu()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		List<WeeklyEventMaterialPreviewGroup> list = BuildWeeklyEventMaterialPreviewGroups();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			list2.Add(new InquiryElement((object)item, BuildWeeklyEventMaterialPreviewGroupLabel(item), (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("本周周报 Prompt 预览", BuildWeeklyReportPromptPreviewMenuDescription(list), list2, true, 0, 1, "查看 Prompt", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevEventEditorMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private string BuildWeeklyReportPromptPreviewMenuDescription(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示当前这一周会发给大模型的周报请求 Prompt。");
		stringBuilder.AppendLine("当前只在开发态使用，生成结果会写回事件编辑，不会自动发给 NPC。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("分组数量：" + (groups?.Count ?? 0));
		stringBuilder.AppendLine("篇幅档位：" + GetWeeklyReportPromptProfile().Label);
		stringBuilder.AppendLine("每分钟生成上限：" + GetWeeklyReportRequestsPerMinute());
		stringBuilder.AppendLine("MaxTokens：" + 5000);
		return stringBuilder.ToString().TrimEnd();
	}

	private void OpenDevWeeklyReportPromptDetail(WeeklyEventMaterialPreviewGroup group)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int startDay = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int weekIndex = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		string systemPrompt = BuildWeeklyReportSystemPrompt(group);
		string userPrompt = BuildWeeklyReportUserPrompt(group, weekIndex, startDay, currentGameDayIndexSafe);
		InformationManager.ShowInquiry(new InquiryData("周报 Prompt 详情", BuildWeeklyReportPromptPreviewText(group, systemPrompt, userPrompt), true, false, "返回 Prompt 列表", "", (Action)delegate
		{
			OpenDevWeeklyReportPromptPreviewMenu();
		}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void ConfirmGenerateDevWeeklyReports()
	{
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Expected O, but got Unknown
		List<WeeklyEventMaterialPreviewGroup> source = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups());
		int num = source.Count((WeeklyEventMaterialPreviewGroup x) => x != null);
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(from x in source
			where string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)
			select x.KingdomId);
		string text = ((kingdomIdsByPlayerProximity.Count > 0) ? string.Join(" -> ", from x in kingdomIdsByPlayerProximity.Select(ResolveKingdomDisplay)
			where !string.IsNullOrWhiteSpace(x)
			select x) : "无");
		InformationManager.ShowInquiry(new InquiryData("生成本周周报草案", "即将按当前周素材生成开发态周报草案。\n\n- 生成对象：世界周报 + 各王国周报\n- 生成结果：写入事件编辑中的事件记录\n- NPC 会常驻读取近期三个王国短周报；命中特定规则时读取完整周报\n- 生成优先级：最近王国 > 世界事件 > 其他王国按距离依次生成\n\n本次预计请求数：" + num + "\n篇幅档位：" + GetWeeklyReportPromptProfile().Label + "\n每分钟生成上限：" + GetWeeklyReportRequestsPerMinute() + "\n按距离排序的王国：" + text + "\nMaxTokens：" + 5000 + "\n\n是否开始？", true, true, "开始生成", "取消", (Action)delegate
		{
			GenerateDevWeeklyReportsAsync();
		}, (Action)delegate
		{
			OpenDevEventEditorMenu();
		}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private async Task GenerateDevWeeklyReportsAsync()
	{
		List<WeeklyEventMaterialPreviewGroup> list = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups());
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		await GenerateWeeklyReportsAsyncInternal(list, num2, num, currentGameDayIndexSafe, "本周周报草案", openViewerWhenDone: true, queueBlockingPopupOnFatalFailure: true, isAutoGeneration: false);
	}

	private async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration)
	{
		WeeklyReportGenerationResult weeklyReportGenerationResult = new WeeklyReportGenerationResult();
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
	}

	private void OpenDevAllDataMenu()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"export_all", "导出（全部，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_all", "导入（全部，选择文件夹）", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("全部导出/导入", "选择要执行的操作：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevAllDataMenuSelected, (Action<List<InquiryElement>>)delegate
		{
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		_devEditingHero = null;
		_devHistorySearchQuery = string.Empty;
		_devHeroSelectionQuery = string.Empty;
		_devEditableHeroes = BuildDevEditableHeroList();
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的 NPC。"));
			OpenDevHeroNpcMenu();
		}
		else
		{
			OpenDevTownEditorHeroSelectionPaged(0, null);
		}
	}

	private void OpenDevTownEditorHeroSelectionPaged(int page, string query)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Expected O, but got Unknown
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected O, but got Unknown
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Expected O, but got Unknown
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Expected O, but got Unknown
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Expected O, but got Unknown
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
		string text = (_devHeroSelectionQuery = (query ?? "").Trim());
		List<Hero> list = _devEditableHeroes;
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = _devEditableHeroes.Where(delegate(Hero h)
			{
				string text6 = ((((h != null) ? h.Name : null) != (TextObject)null) ? ((object)h.Name).ToString() : "").Trim().ToLowerInvariant();
				string text7 = (((h != null) ? ((MBObjectBase)h).StringId : null) ?? "").Trim().ToLowerInvariant();
				return text6.Contains(q) || text7.Contains(q);
			}).ToList();
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		list2.Add(new InquiryElement((object)"search", "搜索 NPC", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement((object)"clear_search", "清空搜索", (ImageIdentifier)null));
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / 40.0));
		if (page >= num)
		{
			page = num - 1;
		}
		_devHeroSelectionPage = page;
		if (page > 0)
		{
			list2.Add(new InquiryElement((object)"prev_page", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement((object)"next_page", "下一页", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		int count = page * 40;
		foreach (Hero item in list.Skip(count).Take(40))
		{
			string text2 = ((item == null) ? null : ((object)item.Name)?.ToString()) ?? "NPC";
			string text3 = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "";
			string text4 = (string.IsNullOrEmpty(text3) ? text2 : (text2 + " (ID=" + text3 + ")"));
			list2.Add(new InquiryElement((object)item, text4, (ImageIdentifier)null));
		}
		string text5 = $"全部 NPC：{_devEditableHeroes.Count} 个";
		text5 += $"\n当前结果：{list.Count} 个，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 = text5 + "\n搜索关键词：" + text;
		}
		if (list.Count == 0)
		{
			text5 += "\n没有匹配结果，可以重新搜索。";
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑 HeroNPC - 选择NPC", text5, list2, true, 0, 1, "确定", "返回", (Action<List<InquiryElement>>)OnDevHeroSelected, (Action<List<InquiryElement>>)delegate
		{
			OpenDevHeroNpcMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevEditLine(Hero npc, int dayIndex, int lineIndex)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected O, but got Unknown
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
		string initialText = dialogueDay.Lines[lineIndex] ?? "";
		string text = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text2 = ((object)npc.Name)?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑对话行 - " + text2, "当前日期: " + text, "下方输入框可直接修改整条内容，留空则删除该行。", initialText, delegate(string input)
		{
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input);
		}, delegate
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
		});
	}

	private void ApplyDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string input)
	{
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
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
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		if (npc != null)
		{
			string text = ((object)npc.Name)?.ToString() ?? "NPC";
			string text2 = "编辑对话行 - " + text;
			string text3 = "当前日期: " + displayDate + "\n原内容已载入下方输入框，可直接编辑。\n留空则删除该行。";
			InformationManager.ShowTextInquiry(new TextInquiryData(text2, text3, true, true, "保存", "返回", (Action<string>)delegate(string input)
			{
				ApplyDevEditLineInput(npc, dayIndex, lineIndex, input);
			}, (Action)delegate
			{
				OpenDevHistoryLineSelection(npc, dayIndex);
			}, false, (Func<string, Tuple<bool, string>>)null, "", currentValue ?? ""), false, false);
		}
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
				Hero val = Hero.FindFirst((Func<Hero, bool>)((Hero h) => h != null && string.Equals((((MBObjectBase)h).StringId ?? "").Trim(), text6, StringComparison.OrdinalIgnoreCase)));
				if (val != null && val != Hero.MainHero)
				{
					list.Add(val);
				}
			}
			catch
			{
			}
		};
		try
		{
			foreach (Hero item in (List<Hero>)(object)Hero.AllAliveHeroes)
			{
				if (item != null && item != Hero.MainHero)
				{
					action(((MBObjectBase)item).StringId);
				}
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
			list = list.OrderBy((Hero h) => ((object)h.Name)?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => ((MBObjectBase)h).StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		}
		return list;
	}

	private void OnDevHeroSelected(List<InquiryElement> selected)
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
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
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索 NPC", "输入 NPC 名称或 HeroId，可查询全部 NPC。", true, true, "搜索", "返回", (Action<string>)delegate(string input)
				{
					OpenDevTownEditorHeroSelectionPaged(0, input);
				}, (Action)delegate
				{
					OpenDevTownEditorHeroSelectionPaged(0, _devHeroSelectionQuery);
				}, false, (Func<string, Tuple<bool, string>>)null, _devHeroSelectionQuery ?? "", ""), false, false);
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
		object identifier = selected[0].Identifier;
		Hero val = (Hero)((identifier is Hero) ? identifier : null);
		if (val == null)
		{
			OpenDevHeroNpcMenu();
			return;
		}
		_devEditingHero = val;
		_devHistorySearchQuery = string.Empty;
		ShowDevEditInquiry(_devEditingHero);
	}

	private void ShowDevEditInquiry(Hero npc)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		if (npc != null)
		{
			_devEditingHero = npc;
			string text = ((object)npc.Name)?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine(" - 编辑对话历史");
			stringBuilder.AppendLine(" - 编辑赊账/欠款");
			stringBuilder.AppendLine(" - 编辑角色个性/历史背景");
			stringBuilder.AppendLine(" - 查看行动记录（结构化）");
			stringBuilder.AppendLine(" - 切换 NPC");
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement((object)"edit_history", "编辑对话历史", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"edit_debt", "编辑赊账/欠款", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"edit_persona", "编辑角色个性/历史背景", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"view_actions", "查看行动记录（结构化）", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"switch_npc", "切换 NPC", (ImageIdentifier)null));
			MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑 HeroNPC - " + text, stringBuilder.ToString(), list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevNpcMainMenuSelected, (Action<List<InquiryElement>>)delegate
			{
				OpenDevHeroNpcMenu();
			}, "", false);
			MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
		}
	}

	private void OpenDevSetDebtGoldSimple(Hero npc)
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		if (npc == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		RewardSystemBehavior.Instance.GetDebtSnapshot(npc, out var owedGold, out var items);
		string text = ((object)npc.Name)?.ToString() ?? "NPC";
		string text2 = "当前金币欠款: " + owedGold + "。\n输入新的金币欠款数值（允许为 0）：";
		InformationManager.ShowTextInquiry(new TextInquiryData("设置金币欠款 - " + text, text2, true, true, "确定", "返回", (Action<string>)delegate(string input)
		{
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Expected O, but got Unknown
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Expected O, but got Unknown
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
		}, (Action)delegate
		{
			OpenDevDebtMenu(npc);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
	}

	private void OpenDevDebtMenu(Hero npc)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		string text = ((object)npc.Name)?.ToString() ?? "NPC";
		string text2 = "请选择要执行的操作：";
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				string text3 = RewardSystemBehavior.Instance.BuildDebtEditorSummary(npc, 10);
				if (!string.IsNullOrWhiteSpace(text3))
				{
					text2 = text3;
				}
			}
		}
		catch
		{
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"set_gold", "设置金币欠款", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"clear_debt", "清空所有欠款", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑赊账/欠款 - " + text, text2, list, true, 0, 1, "执行", "返回", (Action<List<InquiryElement>>)OnDevDebtMenuSelected, (Action<List<InquiryElement>>)delegate
		{
			ShowDevEditInquiry(npc);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OnDevDebtMenuSelected(List<InquiryElement> selected)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
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
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected O, but got Unknown
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected O, but got Unknown
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Expected O, but got Unknown
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
		int num = Math.Max(1, (int)Math.Ceiling((double)devNpcActionEntries.Count / 18.0));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"recent", "查看近期行动", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"major", "查看重大行动", (ImageIdentifier)null));
		if (page > 0)
		{
			list.Add(new InquiryElement((object)"prev_page", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement((object)"next_page", "下一页", (ImageIdentifier)null));
		}
		list.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		int count = page * 18;
		foreach (NpcActionEntry item in devNpcActionEntries.Skip(count).Take(18))
		{
			list.Add(new InquiryElement((object)item, BuildDevNpcActionItemLabel(item), (ImageIdentifier)null));
		}
		string text = ((object)npc.Name)?.ToString() ?? "NPC";
		string text2 = BuildDevNpcActionMenuDescription(npc, recentOnly, page, num, devNpcActionEntries.Count, devNpcActionEntries2.Count);
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("行动记录 - " + text, text2, list, true, 0, 1, "查看", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				ShowDevEditInquiry(npc);
			}
			else if (selected[0].Identifier is string text3)
			{
				switch (text3)
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
			else if (selected[0].Identifier is NpcActionEntry entry)
			{
				OpenDevNpcActionDetail(npc, recentOnly, page, entry);
			}
			else
			{
				OpenDevNpcActionMenu(npc, recentOnly, page);
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			ShowDevEditInquiry(npc);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		return SanitizeNpcActionEntries(value, recentOnly);
	}

	private string BuildDevNpcActionMenuDescription(Hero npc, bool recentOnly, int page, int totalPages, int currentCount, int majorCount)
	{
		string text = ((npc == null) ? null : ((object)npc.Name)?.ToString()) ?? "NPC";
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
		string text = ((!string.IsNullOrWhiteSpace(entry.GameDate)) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日"));
		string text2 = BuildDevNpcActionPreviewText(entry);
		if (text2.Length > 108)
		{
			text2 = text2.Substring(0, 108) + "...";
		}
		return text + " " + text2;
	}

	private void OpenDevNpcActionDetail(Hero npc, bool recentOnly, int page, NpcActionEntry entry)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		if (npc == null || entry == null)
		{
			ShowDevEditInquiry(npc);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回行动列表", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("行动详情 - " + (((object)npc.Name)?.ToString() ?? "NPC"), BuildDevNpcActionDetailText(entry), list, true, 0, 1, "返回", "关闭", (Action<List<InquiryElement>>)delegate
		{
			OpenDevNpcActionMenu(npc, recentOnly, page);
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevNpcActionMenu(npc, recentOnly, page);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private static string BuildDevNpcActionDetailText(NpcActionEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string value = BuildDevNpcActionNarrative(entry);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine("【易读说明】");
			stringBuilder.AppendLine(value);
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("【原始字段】");
		AppendDevNpcActionField(stringBuilder, "日期", (!string.IsNullOrWhiteSpace(entry.GameDate)) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日"));
		AppendDevNpcActionField(stringBuilder, "日序", entry.Day.ToString());
		AppendDevNpcActionField(stringBuilder, "显示文本", (entry.Text ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "StableKey", (entry.StableKey ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "行动类型", GetDevNpcActionKindDisplay(entry.ActionKind));
		AppendDevNpcActionField(stringBuilder, "是否重大行动", entry.IsMajor ? "是" : "否");
		AppendDevNpcActionField(stringBuilder, "结果", (!entry.Won.HasValue) ? "" : (entry.Won.Value ? "获胜" : "失利"));
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
		string text = ((!string.IsNullOrWhiteSpace(entry.GameDate)) ? entry.GameDate.Trim() : ("第 " + entry.Day + " 日"));
		string text2 = TranslateNpcActionKindForPrompt(entry.ActionKind);
		string text3 = ResolveHeroDisplay(entry.ActorHeroId);
		string text4 = (string.IsNullOrWhiteSpace(text3) ? "该人物" : text3);
		string text5 = ResolveDisplayNameBySettlementEntry(entry);
		string text6 = ResolveHeroDisplay(entry.TargetHeroId);
		string text7 = ResolveClanDisplay(entry.TargetClanId);
		string text8 = ResolveKingdomDisplay(entry.TargetKingdomId);
		string text9 = ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text10 = ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text11 = ResolveClanDisplay(entry.PreviousSettlementOwnerClanId);
		string text12 = ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId);
		string text13 = ResolveClanDisplay(entry.ActorClanId);
		string text14 = ResolveKingdomDisplay(entry.ActorKingdomId);
		list.Add(text + "。");
		string text15 = BuildNpcActionActorNarrativeText(entry);
		if (!string.IsNullOrWhiteSpace(text15))
		{
			list.Add(text15 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("这属于" + text2 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text13) && !string.IsNullOrWhiteSpace(text14))
		{
			list.Add(text4 + "所属家族是" + text13 + "，隶属于" + text14 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text13))
		{
			list.Add(text4 + "所属家族是" + text13 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text14))
		{
			list.Add(text4 + "隶属于" + text14 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add("地点是" + text5 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text9) && !string.IsNullOrWhiteSpace(text10))
		{
			list.Add("当时该定居点由" + text9 + "掌控，隶属于" + text10 + "。");
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add("当时该定居点由" + text9 + "掌控。");
		}
		else if (!string.IsNullOrWhiteSpace(text10))
		{
			list.Add("当时该定居点隶属于" + text10 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text11) && !string.IsNullOrWhiteSpace(text12))
		{
			list.Add("在此之前，这里由" + text11 + "掌控，归属" + text12 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add("直接涉及的人物是" + text6 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add("涉及的家族是" + text7 + "。");
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add("涉及的王国是" + text8 + "。");
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
		if (stringBuilder != null && !string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.AppendLine(label + "：" + value.Trim());
		}
	}

	private static string JoinDevActionIds(List<string> ids)
	{
		if (ids == null || ids.Count == 0)
		{
			return "";
		}
		List<string> list = (from x in ids
			where !string.IsNullOrWhiteSpace(x)
			select x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return (list.Count == 0) ? "" : string.Join(", ", list);
	}

	private void OpenDevPersonaMenu(Hero npc)
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		if (npc != null)
		{
			_devEditingHero = npc;
			string text = ((object)npc.Name)?.ToString() ?? "NPC";
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
			list.Add(new InquiryElement((object)"set_personality", "设置/修改个性", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"set_background", "设置/修改历史背景", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"set_voice", "设置/修改音色ID", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"clear_persona", "清空个性、历史背景与音色ID", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
			MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑个性/历史背景 - " + text, stringBuilder.ToString(), list, true, 0, 1, "执行", "返回", (Action<List<InquiryElement>>)OnDevPersonaMenuSelected, (Action<List<InquiryElement>>)delegate
			{
				ShowDevEditInquiry(npc);
			}, "", false);
			MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
		}
	}

	private void OnDevPersonaMenuSelected(List<InquiryElement> selected)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
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
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		if (npc == null)
		{
			return;
		}
		string text = ((object)npc.Name)?.ToString() ?? "NPC";
		string text2 = _devHistorySearchQuery ?? string.Empty;
		InformationManager.ShowTextInquiry(new TextInquiryData("搜索对话历史 - " + text, "输入关键词，按内容模糊匹配该 NPC 的全部对话历史。\n留空将不改变当前搜索。", true, true, "搜索", "返回", (Action<string>)delegate(string input)
		{
			if (!string.IsNullOrWhiteSpace(input))
			{
				_devHistorySearchQuery = input.Trim();
			}
			OpenDevHistoryDateSelection(npc);
		}, (Action)delegate
		{
			OpenDevHistoryDateSelection(npc);
		}, false, (Func<string, Tuple<bool, string>>)null, text2, ""), false, false);
	}

	private void ConfirmDevClearAllDialogueHistory(Hero npc)
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
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
		string arg = ((object)npc.Name)?.ToString() ?? "NPC";
		string text = $"将删除 {arg} 的全部对话历史（共 {num} 条）。\n此操作不可撤销，是否继续？";
		InformationManager.ShowInquiry(new InquiryData("确认删除对话历史", text, true, true, "确认删除", "取消", (Action)delegate
		{
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Expected O, but got Unknown
			if (_dialogueHistory == null)
			{
				_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			}
			string text2 = ((MBObjectBase)npc).StringId ?? string.Empty;
			if (!string.IsNullOrEmpty(text2))
			{
				_dialogueHistory.Remove(text2);
			}
			_devHistorySearchQuery = string.Empty;
			InformationManager.DisplayMessage(new InformationMessage("已删除该NPC的全部对话历史。"));
			ShowDevEditInquiry(npc);
		}, (Action)delegate
		{
			OpenDevHistoryDateSelection(npc);
		}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), false, false);
	}

	private void OpenDevHistoryDateSelection(Hero npc)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Expected O, but got Unknown
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Expected O, but got Unknown
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Expected O, but got Unknown
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
		list2.Add(new InquiryElement((object)"__search__", "搜索对话历史", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement((object)"__clear_search__", "清空搜索", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__clear_all__", "一键删除该NPC全部对话历史", (ImageIdentifier)null));
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		if (string.IsNullOrWhiteSpace(text))
		{
			foreach (DialogueDay item in list)
			{
				if (item != null && item.Lines != null && item.Lines.Count != 0)
				{
					string text2 = ((!string.IsNullOrEmpty(item.GameDate)) ? item.GameDate : $"第 {item.GameDayIndex} 日");
					string text3 = text2 + $" (共 {item.Lines.Count} 条)";
					list2.Add(new InquiryElement((object)item.GameDayIndex, text3, (ImageIdentifier)null));
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
					string text4 = item2.Lines[num] ?? string.Empty;
					if (text4.ToLowerInvariant().Contains(value))
					{
						string arg2 = BuildDevHistoryPreview(text4);
						string text5 = $"{arg} / 第{num + 1}条: {arg2}";
						list2.Add(new InquiryElement((object)new Tuple<int, int>(item2.GameDayIndex, num), text5, (ImageIdentifier)null));
					}
				}
			}
		}
		string text6 = ((object)npc.Name)?.ToString() ?? "NPC";
		string text7 = "请选择要查看/编辑的日期。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text7 = text7 + "\n当前搜索：" + BuildDevHistoryPreview(text, 40);
		}
		if (list2.Count <= 4)
		{
			text7 += "\n\n没有匹配结果。";
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑对话历史 - " + text6, text7, list2, true, 0, 1, "下一步", "返回", (Action<List<InquiryElement>>)OnDevHistoryDateSelected, (Action<List<InquiryElement>>)delegate
		{
			ShowDevEditInquiry(npc);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected O, but got Unknown
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Expected O, but got Unknown
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
		list2.Add(new InquiryElement((object)"back", "返回日期列表", (ImageIdentifier)null));
		list2.Add(new InquiryElement((object)"search", "搜索该NPC历史", (ImageIdentifier)null));
		for (int num = 0; num < dialogueDay.Lines.Count; num++)
		{
			string line = dialogueDay.Lines[num] ?? "";
			string arg = BuildDevHistoryPreview(line);
			string text = $"{num + 1}. {arg}";
			list2.Add(new InquiryElement((object)new Tuple<int, int>(dayIndex, num), text, (ImageIdentifier)null));
		}
		string text2 = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text3 = ((object)npc.Name)?.ToString() ?? "NPC";
		string text4 = "请选择要编辑的对话行。";
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑对话行 - " + text3, text4 + "\n当前日期: " + text2, list2, true, 0, 1, "编辑", "返回", (Action<List<InquiryElement>>)OnDevHistoryLineSelected, (Action<List<InquiryElement>>)delegate
		{
			OpenDevHistoryDateSelection(npc);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
			string text = ((object)npc.Name)?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor("设置个性 - " + text, "当前个性已载入下方输入框。", "请输入新的个性描述（留空=清空）。", personality ?? "", delegate(string input)
			{
				//IL_0078: Unknown result type (might be due to invalid IL or missing references)
				//IL_0082: Expected O, but got Unknown
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
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		if (npc != null)
		{
			_devEditingHero = npc;
			string npcVoiceId = GetNpcVoiceId(npc);
			string text = ((object)npc.Name)?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("当前音色ID：");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(npcVoiceId) ? "（未设置 - 使用VoiceMapping自动分配）" : npcVoiceId);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("请输入新的音色ID（留空=清空，将使用VoiceMapping自动分配）：");
			stringBuilder.AppendLine("提示：音色ID取决于您使用的TTS平台。");
			stringBuilder.AppendLine("  火山引擎例如: BV001_streaming");
			stringBuilder.AppendLine("  GPT-SoVITS例如: 参考音频文件名");
			InformationManager.ShowTextInquiry(new TextInquiryData("设置音色ID - " + text, stringBuilder.ToString(), true, true, "保存", "返回", (Action<string>)delegate(string input)
			{
				//IL_0066: Unknown result type (might be due to invalid IL or missing references)
				//IL_0070: Expected O, but got Unknown
				NpcPersonaProfile npcPersonaProfile = GetNpcPersonaProfile(npc, createIfMissing: true) ?? new NpcPersonaProfile();
				npcPersonaProfile.VoiceId = (input ?? "").Trim();
				SaveNpcPersonaProfile(npc, npcPersonaProfile);
				string text2 = (string.IsNullOrWhiteSpace(input) ? "音色ID已清空（将使用VoiceMapping自动分配）." : ("音色ID已更新为: " + input.Trim()));
				InformationManager.DisplayMessage(new InformationMessage(text2));
				OpenDevPersonaMenu(npc);
			}, (Action)delegate
			{
				OpenDevPersonaMenu(npc);
			}, false, (Func<string, Tuple<bool, string>>)null, npcVoiceId ?? "", ""), false, false);
		}
	}

	private void OpenDevSetBackground(Hero npc)
	{
		if (npc != null)
		{
			_devEditingHero = npc;
			GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = ((object)npc.Name)?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor("设置历史背景 - " + text, "当前历史背景已载入下方输入框。", "请输入新的历史背景描述（留空=清空）。", background ?? "", delegate(string input)
			{
				//IL_0078: Unknown result type (might be due to invalid IL or missing references)
				//IL_0082: Expected O, but got Unknown
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
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"single_export_rule", "导出（单个知识条目，从列表选择）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"single_import_rule", "导入（单个知识条目，从文件夹选择）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_knowledge", "全量导出（Knowledge，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_knowledge", "全量导入（Knowledge，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"edit_knowledge", "编辑 Knowledge 条目…", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("Knowledge 导入/导出", "选择要执行的操作：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevKnowledgeMenuSelected, (Action<List<InquiryElement>>)delegate
		{
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OnDevKnowledgeMenuSelected(List<InquiryElement> selected)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
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
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		object obj = KnowledgeLibraryBehavior.Instance;
		if (obj == null)
		{
			Campaign current = Campaign.Current;
			obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
		}
		List<KnowledgeLibraryBehavior.RuleIndexItem> list = ((KnowledgeLibraryBehavior)obj)?.GetRuleIndexItemsForDev(400);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (KnowledgeLibraryBehavior.RuleIndexItem item in list)
		{
			string text = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = (item?.Label ?? text).Trim();
				list2.Add(new InquiryElement((object)text, text2, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("导出单个知识条目 - 选择条目", "请选择要导出的知识条目：", list2, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevKnowledgeMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevKnowledgeSingleImportSelection(string folderName)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		List<string> knowledgeRuleIdsFromImportFolderForDev = GetKnowledgeRuleIdsFromImportFolderForDev(folderName, 400);
		if (knowledgeRuleIdsFromImportFolderForDev == null || knowledgeRuleIdsFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (string item in knowledgeRuleIdsFromImportFolderForDev)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement((object)text, text, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("导入单个知识条目 - 选择条目", "请选择要导入的知识条目：", list, true, 0, 1, "导入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevKnowledgeMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevUnnamedPersonaMenu()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"single_export_unnamed", "导出（单个未命名NPC，从列表选择）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"single_import_unnamed", "导入（单个未命名NPC，从文件夹选择）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_unnamed", "全量导出（未命名NPC，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_unnamed", "全量导入（未命名NPC，选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"edit_unnamed", "编辑未命名NPC条目…", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("未命名NPC 导入/导出", "选择要执行的操作：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevUnnamedPersonaMenuSelected, (Action<List<InquiryElement>>)delegate
		{
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Expected O, but got Unknown
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Expected O, but got Unknown
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string text = (item.Key ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = (string.IsNullOrWhiteSpace(item.Label) ? text : item.Label);
					list.Add(new InquiryElement((object)text, text2 + " (Key=" + text + ")", (ImageIdentifier)null));
				}
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("导出单个未命名NPC - 选择条目", "请选择要导出的条目：", list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevUnnamedPersonaMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevUnnamedPersonaSingleImportSelection(string folderName)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		List<string> unnamedPersonaKeysFromImportFolderForDev = GetUnnamedPersonaKeysFromImportFolderForDev(folderName, 400);
		if (unnamedPersonaKeysFromImportFolderForDev == null || unnamedPersonaKeysFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (string item in unnamedPersonaKeysFromImportFolderForDev)
		{
			string text = (item ?? "").Trim().ToLower();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement((object)text, text, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("导入单个未命名NPC - 选择条目", "请选择要导入的条目：", list, true, 0, 1, "导入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevUnnamedPersonaMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevUnnamedPersonaIndexSelection()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Expected O, but got Unknown
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string text = (string.IsNullOrWhiteSpace(item.Label) ? item.Key : item.Label);
				list.Add(new InquiryElement((object)item.Key, text, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("编辑未命名NPC - 选择条目", "选择一个条目进行编辑：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevUnnamedPersonaIndexSelected, (Action<List<InquiryElement>>)delegate
		{
			OpenDevUnnamedPersonaMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Expected O, but got Unknown
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
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Expected O, but got Unknown
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Expected O, but got Unknown
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Expected O, but got Unknown
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Expected O, but got Unknown
		if (_devEditableHeroes == null || _devEditableHeroes.Count == 0)
		{
			_devEditableHeroes = BuildDevEditableHeroList() ?? new List<Hero>();
		}
		if (page < 0)
		{
			page = 0;
		}
		string text = (_devSingleNpcSelectionQuery = (query ?? "").Trim());
		List<Hero> list = _devEditableHeroes ?? new List<Hero>();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			list = list.Where(delegate(Hero h)
			{
				string text5 = ((((h != null) ? h.Name : null) != (TextObject)null) ? ((object)h.Name).ToString() : "").Trim().ToLowerInvariant();
				string text6 = (((h != null) ? ((MBObjectBase)h).StringId : null) ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		list2.Add(new InquiryElement((object)"pick_from_export", "从导出文件夹选择NPC…", (ImageIdentifier)null));
		list2.Add(new InquiryElement((object)"manual_id", "手动输入 HeroId…", (ImageIdentifier)null));
		list2.Add(new InquiryElement((object)"search", "搜索 NPC", (ImageIdentifier)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement((object)"clear_search", "清空搜索", (ImageIdentifier)null));
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)list.Count / 40.0));
		if (page >= num)
		{
			page = num - 1;
		}
		_devSingleNpcSelectionPage = page;
		if (page > 0)
		{
			list2.Add(new InquiryElement((object)"prev_page", "上一页", (ImageIdentifier)null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement((object)"next_page", "下一页", (ImageIdentifier)null));
		}
		list2.Add(new InquiryElement((object)"__sep__", "----------------", (ImageIdentifier)null));
		int count = page * 40;
		foreach (Hero item in list.Skip(count).Take(40))
		{
			if (item != null)
			{
				string text2 = (((MBObjectBase)item).StringId ?? "").Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					string text3 = ((object)item.Name)?.ToString() ?? "NPC";
					list2.Add(new InquiryElement((object)item, text3 + " (ID=" + text2 + ")", (ImageIdentifier)null));
				}
			}
		}
		string text4 = $"可从当前存档全部 NPC 中选择，也可从旧导出文件夹读取 JSON。\n全部 NPC：{_devEditableHeroes.Count} 个；当前结果：{list.Count} 个，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text4 = text4 + "\n搜索关键词：" + text;
		}
		if (list.Count == 0)
		{
			text4 += "\n没有匹配结果，可以重新搜索。";
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - 选择NPC", text4, list2, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevSingleNpcHeroSelected, (Action<List<InquiryElement>>)delegate
		{
			OpenDevHeroNpcMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevSingleNpcHeroSelectionFromExportFolder(string folderName)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Expected O, but got Unknown
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Expected O, but got Unknown
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Expected O, but got Unknown
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
				string[] array = files;
				foreach (string text3 in array)
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
		list2.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		foreach (KeyValuePair<string, string> item2 in idToName.OrderBy((KeyValuePair<string, string> k) => k.Key))
		{
			string text6 = (item2.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text6))
			{
				string text7 = (item2.Value ?? "").Trim();
				string text8 = (string.IsNullOrEmpty(text7) ? (text6 ?? "") : (text7 + " (ID=" + text6 + ")"));
				list2.Add(new InquiryElement((object)text6, text8, (ImageIdentifier)null));
			}
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("从导出文件夹选择 NPC", "目录：\n" + text2 + "\n\n请选择要导入/导出的 NPC：", list2, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevSingleNpcHeroSelection();
			}
			else
			{
				string text9 = selected[0].Identifier as string;
				if (text9 == "back")
				{
					OpenDevSingleNpcHeroSelection();
				}
				else
				{
					text9 = (text9 ?? "").Trim();
					if (string.IsNullOrEmpty(text9))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						_devOpsHeroId = text9;
						string value2 = "";
						try
						{
							idToName.TryGetValue(text9, out value2);
						}
						catch
						{
							value2 = "";
						}
						if (string.IsNullOrWhiteSpace(value2))
						{
							value2 = text9;
						}
						_devOpsHeroName = value2;
						OpenDevSingleNpcOpsMenu();
					}
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevSingleNpcHeroSelection();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OnDevSingleNpcHeroSelected(List<InquiryElement> selected)
	{
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Expected O, but got Unknown
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Expected O, but got Unknown
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
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索 NPC", "输入 NPC 名称或 HeroId，可查询全部 NPC。", true, true, "搜索", "返回", (Action<string>)delegate(string input)
				{
					OpenDevSingleNpcHeroSelectionPaged(0, input);
				}, (Action)delegate
				{
					OpenDevSingleNpcHeroSelectionPaged(_devSingleNpcSelectionPage, _devSingleNpcSelectionQuery);
				}, false, (Func<string, Tuple<bool, string>>)null, _devSingleNpcSelectionQuery ?? "", ""), false, false);
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
				InformationManager.ShowTextInquiry(new TextInquiryData("手动输入 HeroId", "请输入 HeroId（例如：lord_... / wanderer_...）。\n\n说明：这里不会创建新的游戏角色，只是把导入/导出数据写入到该 HeroId 对应的存档数据里；只有当游戏里存在/将来出现同 ID 的 Hero 时，这些数据才会被使用。", true, true, "确定", "返回", (Action<string>)delegate(string input)
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
							Hero obj = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && ((MBObjectBase)x).StringId == manualHeroId));
							string text3 = ((obj == null) ? null : ((object)obj.Name)?.ToString()) ?? "";
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
				}, (Action)delegate
				{
					OpenDevSingleNpcHeroSelection();
				}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
				return;
			}
		}
		object identifier = selected[0].Identifier;
		Hero val = (Hero)((identifier is Hero) ? identifier : null);
		if (val == null)
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (((MBObjectBase)val).StringId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
			OpenDevSingleNpcHeroSelection();
		}
		else
		{
			_devOpsHeroId = text2;
			_devOpsHeroName = ((object)val.Name)?.ToString() ?? "NPC";
			OpenDevSingleNpcOpsMenu();
		}
	}

	private void OpenDevSingleNpcOpsMenu()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected O, but got Unknown
		string text = (_devOpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (string.IsNullOrEmpty(_devOpsHeroName) ? "NPC" : _devOpsHeroName);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"export_persona", "导出（个性/背景，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_persona", "导入（个性/背景，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_history", "导出（对话历史，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_history", "导入（对话历史，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_debt", "导出（欠款，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_debt", "导入（欠款，选择文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"change_hero", "切换NPC", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - " + text2 + " (ID=" + text + ")", "选择要执行的操作：", list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevSingleNpcOpsSelected, (Action<List<InquiryElement>>)delegate
		{
			OpenDevSingleNpcHeroSelection();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		string text = (key ?? "").ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"male_young" => "青男", 
			"male_middle" => "中男", 
			"male_old" => "老男", 
			"female_young" => "青女", 
			"female_middle" => "中女", 
			"female_old" => "老女", 
			_ => key ?? "", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void OpenDevVoiceMappingMenu()
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Expected O, but got Unknown
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Expected O, but got Unknown
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Expected O, but got Unknown
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Expected O, but got Unknown
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组数量概览（详细ID请进分组查看）");
		stringBuilder.AppendLine("------------------------------");
		int num = 0;
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		string[] array = allGroupKeys;
		foreach (string text in array)
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
		list.Add(new InquiryElement((object)"add_voice", "添加声音ID到分组", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"remove_voice", "删除分组中的声音ID", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"set_fallback", "设置全局兜底声音(fallback)", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"export_voice_map", "导出映射（选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"import_voice_map", "导入映射（选文件夹）", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"reload", "重新加载配置文件", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("声音映射管理", stringBuilder.ToString(), list, true, 0, 1, "进入", "返回", (Action<List<InquiryElement>>)OnDevVoiceMappingMenuSelected, (Action<List<InquiryElement>>)delegate
		{
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OnDevVoiceMappingMenuSelected(List<InquiryElement> selected)
	{
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		if (selected == null || selected.Count == 0)
		{
			return;
		}
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
				VoiceMapper.ImportMappingJson(_voiceMappingJsonStorage);
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

	private void OpenDevVoiceMappingSelectGroup(bool isAdd)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		string text = (isAdd ? "添加声音ID - 选择分组" : "删除声音ID - 选择分组");
		string text2 = (isAdd ? "选择分组后输入要添加的声音ID。" : "选择要删除声音ID的分组：");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		string[] array = allGroupKeys;
		foreach (string text3 in array)
		{
			List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(text3);
			string text4 = $"{GetVoiceGroupShortName(text3)} ({voicesForGroup.Count})";
			if (isAdd || voicesForGroup.Count != 0)
			{
				list.Add(new InquiryElement((object)text3, text4, (ImageIdentifier)null));
			}
		}
		if (!isAdd && list.Count <= 1)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有任何声音ID可删除。"));
			OpenDevVoiceMappingMenu();
			return;
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(text, text2, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingMenu();
			}
			else
			{
				string text5 = selected[0].Identifier as string;
				if (text5 == "back")
				{
					OpenDevVoiceMappingMenu();
				}
				else if (isAdd)
				{
					OpenDevVoiceMappingAddVoice(text5);
				}
				else
				{
					OpenDevVoiceMappingRemoveVoice(text5);
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevVoiceMappingMenu();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevVoiceMappingAddVoice(string groupKey)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组: " + displayName);
		stringBuilder.AppendLine($"当前数量: {voicesForGroup.Count}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("请输入要添加的声音ID：");
		stringBuilder.AppendLine("（与MCM中的TTS声音ID一致）");
		InformationManager.ShowTextInquiry(new TextInquiryData("添加声音ID - " + displayName, stringBuilder.ToString(), true, true, "添加", "返回", (Action<string>)delegate(string input)
		{
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Expected O, but got Unknown
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
		}, (Action)delegate
		{
			OpenDevVoiceMappingSelectGroup(isAdd: true);
		}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
	}

	private void OpenDevVoiceMappingRemoveVoice(string groupKey)
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		if (voicesForGroup.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("[" + displayName + "] 分组没有声音ID可删除。"));
			OpenDevVoiceMappingSelectGroup(isAdd: false);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"back", "返回", (ImageIdentifier)null));
		list.Add(new InquiryElement((object)"clear_all", "⚠ 清空该分组所有声音", (ImageIdentifier)null));
		for (int i = 0; i < voicesForGroup.Count; i++)
		{
			list.Add(new InquiryElement((object)voicesForGroup[i], $"{i + 1}. {voicesForGroup[i]}", (ImageIdentifier)null));
		}
		MultiSelectionInquiryData val = new MultiSelectionInquiryData("删除声音ID - " + displayName, $"分组: {displayName}\n当前共 {voicesForGroup.Count} 个声音ID。\n选择要删除的声音：", list, true, 0, 1, "删除", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Expected O, but got Unknown
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Expected O, but got Unknown
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
		}, (Action<List<InquiryElement>>)delegate
		{
			OpenDevVoiceMappingSelectGroup(isAdd: false);
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void OpenDevVoiceMappingSetFallback()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("全局兜底声音(fallback)：当某个分组没有配置声音ID时，使用此声音。");
		stringBuilder.AppendLine("当前: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "（未设置，最终回退到MCM全局设置）" : fallbackVoice));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("输入新的 fallback 声音ID（留空=清除）：");
		InformationManager.ShowTextInquiry(new TextInquiryData("设置全局兜底声音(fallback)", stringBuilder.ToString(), true, true, "保存", "返回", (Action<string>)delegate(string input)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected O, but got Unknown
			string text = (input ?? "").Trim();
			VoiceMapper.SetFallbackVoice(text);
			string text2 = (string.IsNullOrWhiteSpace(text) ? "fallback 已清除" : ("fallback 已设置为: " + text));
			InformationManager.DisplayMessage(new InformationMessage(text2));
			OpenDevVoiceMappingMenu();
		}, (Action)delegate
		{
			OpenDevVoiceMappingMenu();
		}, false, (Func<string, Tuple<bool, string>>)null, fallbackVoice ?? "", ""), false, false);
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
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Expected O, but got Unknown
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
		list.Add(new InquiryElement((object)"__input__", isExport ? "手动输入文件夹名…" : "手动输入文件夹名/路径…", (ImageIdentifier)null));
		if (!isExport)
		{
			list.Add(new InquiryElement((object)"__latest__", "使用最新导出（自动）", (ImageIdentifier)null));
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(playerExportsRootPath);
			List<DirectoryInfo> list2 = (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string text = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement((object)item.Name, text, (ImageIdentifier)null));
			}
		}
		catch
		{
		}
		string text2 = (isExport ? "选择目标文件夹（可覆盖已有）。" : "选择来源文件夹。");
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(title, text2, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				if (text3 == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名/路径", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出；也可输入完整路径（文件夹或 .json 文件）。", true, true, "确定", "取消", (Action<string>)delegate(string input)
					{
						onSelectedFolder(input);
						onReturn();
					}, (Action)delegate
					{
						onReturn();
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
				}
				else if (!isExport && text3 == "__latest__")
				{
					onSelectedFolder("");
					onReturn();
				}
				else
				{
					onSelectedFolder(text3);
					onReturn();
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
	}

	private void ShowDuplicateImportInquiry(string title, string text, Action onOverwrite, Action onSkipDuplicates, Action onCancel)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Expected O, but got Unknown
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
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement((object)"__overwrite__", "覆盖导入", (ImageIdentifier)null),
				new InquiryElement((object)"__skip__", "只导入非重复信息", (ImageIdentifier)null),
				new InquiryElement((object)"__cancel__", "取消", (ImageIdentifier)null)
			};
			MultiSelectionInquiryData val = new MultiSelectionInquiryData(title, text, list, true, 0, 1, "选择", "取消", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
			}, (Action<List<InquiryElement>>)delegate
			{
				onCancel();
			}, "", false);
			MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Expected O, but got Unknown
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
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement((object)"__overwrite__", "覆盖导出", (ImageIdentifier)null),
				new InquiryElement((object)"__new__", "改用新文件夹（自动）", (ImageIdentifier)null),
				new InquiryElement((object)"__cancel__", "取消", (ImageIdentifier)null)
			};
			MultiSelectionInquiryData val = new MultiSelectionInquiryData(title, text, list, true, 0, 1, "选择", "取消", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
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
			}, (Action<List<InquiryElement>>)delegate
			{
				onCancel();
			}, "", false);
			MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}

	private void OpenFolderPicker(string title, bool isExport, ExportImportScope scope, Action onReturn, string heroId)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Expected O, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected O, but got Unknown
		if (onReturn == null)
		{
			onReturn = ReturnToDevRootMenu;
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		Directory.CreateDirectory(playerExportsRootPath);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement((object)"__input__", "手动输入文件夹名…", (ImageIdentifier)null));
		if (!isExport)
		{
			list.Add(new InquiryElement((object)"__latest__", "使用最新导出（自动）", (ImageIdentifier)null));
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(playerExportsRootPath);
			List<DirectoryInfo> list2 = (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string text = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement((object)item.Name, text, (ImageIdentifier)null));
			}
		}
		catch
		{
		}
		string text2 = (isExport ? "选择要导出的目标文件夹（可覆盖已有）。" : "选择要导入的来源文件夹。");
		MultiSelectionInquiryData val = new MultiSelectionInquiryData(title, text2, list, true, 0, 1, "选择", "返回", (Action<List<InquiryElement>>)delegate(List<InquiryElement> selected)
		{
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Expected O, but got Unknown
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text3 = selected[0].Identifier as string;
				if (text3 == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出。", true, true, "确定", "取消", (Action<string>)delegate(string input)
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
					}, (Action)delegate
					{
						onReturn();
					}, false, (Func<string, Tuple<bool, string>>)null, "", ""), false, false);
				}
				else if (!isExport && text3 == "__latest__")
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
						ResolveAndRunExportImport(isExport, scope, text3);
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport, scope, text3, heroId);
					}
					onReturn();
				}
			}
		}, (Action<List<InquiryElement>>)delegate
		{
			onReturn();
		}, "", false);
		MBInformationManager.ShowMultiSelectionInquiry(val, false, false);
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
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
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
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
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
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
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
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
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
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Expected O, but got Unknown
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Expected O, but got Unknown
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Expected O, but got Unknown
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
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
				//IL_006a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0074: Expected O, but got Unknown
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
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Expected O, but got Unknown
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
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Expected O, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Expected O, but got Unknown
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
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
				//IL_006c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0076: Expected O, but got Unknown
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
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Expected O, but got Unknown
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
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Expected O, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Expected O, but got Unknown
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
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
				//IL_006c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0076: Expected O, but got Unknown
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
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Expected O, but got Unknown
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
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Expected O, but got Unknown
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Expected O, but got Unknown
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
		//IL_0546: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
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
				string[] array2 = array;
				foreach (string text in array2)
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
				string[] array3 = files2;
				string[] array4 = array3;
				foreach (string text3 in array4)
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
				string[] array5 = files3;
				string[] array6 = array5;
				foreach (string text5 in array6)
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
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_0022: Expected O, but got Unknown
				//IL_0283: Unknown result type (might be due to invalid IL or missing references)
				//IL_028d: Expected O, but got Unknown
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
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_0022: Expected O, but got Unknown
				//IL_0286: Unknown result type (might be due to invalid IL or missing references)
				//IL_0290: Expected O, but got Unknown
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
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Expected O, but got Unknown
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Expected O, but got Unknown
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Expected O, but got Unknown
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Expected O, but got Unknown
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
			if (!TryExportKnowledgeToDir(text, out var exportedCount, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导出失败，已跳过。原因：" + error));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("Knowledge 已导出 " + exportedCount + " 条。"));
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
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
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
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
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
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
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
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
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
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
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
				return;
			}
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + text + "（Knowledge " + exportedCount + " 条）"));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	private void ExportEventData(string folderName)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
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
		foreach (KeyValuePair<string, string> eventKingdomOpeningSummary in _eventKingdomOpeningSummaries)
		{
			string text = (eventKingdomOpeningSummary.Key ?? "").Trim();
			string value = (eventKingdomOpeningSummary.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
			{
				dictionary[text] = value;
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
			object obj = KnowledgeLibraryBehavior.Instance;
			if (obj == null)
			{
				Campaign current = Campaign.Current;
				obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj;
			if (knowledgeLibraryBehavior == null)
			{
				error = "KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (!knowledgeLibraryBehavior.TryValidateKnowledgeExport(out error))
			{
				return false;
			}
			string text = knowledgeLibraryBehavior.ExportRulesJson();
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = (string.IsNullOrWhiteSpace(text) ? null : JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text));
			if (knowledgeFile?.Rules == null || knowledgeFile.Rules.Count <= 0)
			{
				error = "当前没有可导出的知识条目。";
				return false;
			}
			string text2 = Path.Combine(exportDir, "knowledge", "rules");
			Directory.CreateDirectory(text2);
			ClearJsonFiles(text2);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
			{
				string text3 = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text3) || !hashSet.Add(text3))
				{
					continue;
				}
				rule.Id = text3;
				string text4 = "";
				try
				{
					text4 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text4 = "";
				}
				string text5 = text3;
				char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
				char[] array = invalidFileNameChars;
				foreach (char oldChar in array)
				{
					text5 = text5.Replace(oldChar, '_');
				}
				text5 = (text5 ?? "").Trim();
				if (text5.Length > 80)
				{
					text5 = text5.Substring(0, 80);
				}
				if (string.IsNullOrEmpty(text5))
				{
					text5 = "rule";
				}
				string text6 = text4;
				char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
				char[] array2 = invalidFileNameChars2;
				foreach (char oldChar2 in array2)
				{
					text6 = text6.Replace(oldChar2, '_');
				}
				text6 = (text6 ?? "").Trim();
				if (text6.Length > 35)
				{
					text6 = text6.Substring(0, 35);
				}
				string text7 = text5;
				if (!string.IsNullOrEmpty(text6))
				{
					text7 = text7 + "__" + text6;
				}
				string path = Path.Combine(text2, text7 + ".json");
				if (File.Exists(path))
				{
					for (int num3 = 2; num3 <= 999; num3++)
					{
						string text8 = Path.Combine(text2, text7 + "__" + num3 + ".json");
						if (!File.Exists(text8))
						{
							path = text8;
							break;
						}
					}
				}
				string contents = JsonConvert.SerializeObject((object)rule, (Formatting)1);
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
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Expected O, but got Unknown
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：RuleId 为空。"));
				return;
			}
			object obj = KnowledgeLibraryBehavior.Instance;
			if (obj == null)
			{
				Campaign current = Campaign.Current;
				obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj;
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
			char[] array = invalidFileNameChars;
			foreach (char oldChar in array)
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
			char[] array2 = invalidFileNameChars2;
			foreach (char oldChar2 in array2)
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
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected O, but got Unknown
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Expected O, but got Unknown
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Expected O, but got Unknown
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
			object obj = KnowledgeLibraryBehavior.Instance;
			if (obj == null)
			{
				Campaign current = Campaign.Current;
				obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
			}
			KnowledgeLibraryBehavior kb = (KnowledgeLibraryBehavior)obj;
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
			string text2 = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件为空。"));
				return;
			}
			KnowledgeLibraryBehavior.LoreRule rule = null;
			try
			{
				rule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text2);
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
			string text3 = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text3) && !string.Equals(text3, id, StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件中的 Id 不匹配（文件Id=" + text3 + "）。"));
				return;
			}
			if (string.IsNullOrEmpty(text3))
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
				//IL_001e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0028: Expected O, but got Unknown
				//IL_0070: Unknown result type (might be due to invalid IL or missing references)
				//IL_007a: Expected O, but got Unknown
				//IL_0051: Unknown result type (might be due to invalid IL or missing references)
				//IL_005b: Expected O, but got Unknown
				if (!ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject((object)rule, (Formatting)0)))
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
				//IL_0050: Unknown result type (might be due to invalid IL or missing references)
				//IL_005a: Expected O, but got Unknown
				//IL_0031: Unknown result type (might be due to invalid IL or missing references)
				//IL_003b: Expected O, but got Unknown
				if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject((object)rule, (Formatting)0), overwrite: false))
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
					string text2 = File.ReadAllText(path, Encoding.UTF8);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text2);
					if (knowledgeFile2?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
						{
							string text3 = (rule2?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text3))
							{
								rule2.Id = text3;
								if (hashSet.Add(text3))
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
				string text3 = kb.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(text3))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text3);
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
					string text4 = (rule2?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text4) || (overwriteExisting && string.Equals(text4, text, StringComparison.OrdinalIgnoreCase)))
					{
						continue;
					}
					foreach (string item in GetKnowledgeKeywordsForCompare(rule2))
					{
						if (dictionary2.TryGetValue(item, out var value) && !string.Equals(value, text4, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item] = text4;
					}
				}
			}
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				if (dictionary2.TryGetValue(item2.Key, out var value2) && !string.Equals(value2, text, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item2.Key + "）。当前存档中该关键词属于规则 " + value2 + "，导入规则为 " + text + "。";
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
			object obj = KnowledgeLibraryBehavior.Instance;
			if (obj == null)
			{
				Campaign current = Campaign.Current;
				obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj;
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
				string text2 = knowledgeLibraryBehavior.ExportRulesJson();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					knowledgeFile = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text2);
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
					string text3 = (rule?.Id ?? "").Trim();
					if (string.IsNullOrEmpty(text3) || hashSet.Contains(text3))
					{
						continue;
					}
					foreach (string item3 in GetKnowledgeKeywordsForCompare(rule))
					{
						if (dictionary2.TryGetValue(item3, out var value2) && !string.Equals(value2, text3, StringComparison.OrdinalIgnoreCase))
						{
							error = "导入失败：当前存档中已存在重复关键词（" + item3 + "），请先在游戏内修复后再导入。";
							return false;
						}
						dictionary2[item3] = text3;
					}
				}
			}
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule item4 in list2)
			{
				string value3 = (item4?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(value3))
				{
					continue;
				}
				foreach (string item5 in GetKnowledgeKeywordsForCompare(item4))
				{
					dictionary3[item5] = value3;
				}
			}
			foreach (KeyValuePair<string, string> item6 in dictionary3)
			{
				if (dictionary2.TryGetValue(item6.Key, out var value4) && !string.Equals(value4, item6.Value, StringComparison.OrdinalIgnoreCase))
				{
					error = "导入失败：关键词冲突（" + item6.Key + "）。当前存档中该关键词属于规则 " + value4 + "，导入规则为 " + item6.Value + "。";
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
				string[] array3 = array2;
				foreach (string path in array3)
				{
					try
					{
						string a = Path.GetFileName(path) ?? "";
						if (string.Equals(a, "AIConfig.json", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "KnowledgeRules.json", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string text = File.ReadAllText(path, Encoding.UTF8);
						if (string.IsNullOrWhiteSpace(text))
						{
							continue;
						}
						KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text);
						string text2 = (loreRule?.Id ?? "").Trim();
						if (!string.IsNullOrEmpty(text2))
						{
							loreRule.Id = text2;
							if (hashSet.Add(text2))
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
			char[] array = invalidFileNameChars;
			foreach (char oldChar in array)
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
			string[] array2 = files2;
			string[] array3 = array2;
			foreach (string text4 in array3)
			{
				try
				{
					string text5 = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(text5))
					{
						continue;
					}
					KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text5);
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
				string[] array3 = array2;
				foreach (string file in array3)
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
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected O, but got Unknown
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
			char[] array = invalidFileNameChars;
			foreach (char oldChar in array)
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
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Expected O, but got Unknown
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
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
			char[] array = invalidFileNameChars;
			foreach (char oldChar in array)
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
			string[] array2 = files2;
			string[] array3 = array2;
			foreach (string text4 in array3)
			{
				try
				{
					string text5 = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(text5))
					{
						continue;
					}
					UnnamedPersonaSingleJson unnamedPersonaSingleJson = JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(text5);
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
				string[] array3 = array2;
				foreach (string path in array3)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string text2 = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(text2))
						{
							string text3 = (JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text2)?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text3) && hashSet.Add(text3))
							{
								list.Add(text3);
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
				string[] array3 = array2;
				foreach (string path in array3)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string text2 = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(text2))
						{
							string text3 = (JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(text2)?.Key ?? "").Trim().ToLower();
							if (!string.IsNullOrEmpty(text3) && hashSet.Add(text3))
							{
								list.Add(text3);
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
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
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
			string[] array2 = array;
			foreach (string text in array2)
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
				//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ca: Expected O, but got Unknown
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
				//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
				//IL_00cb: Expected O, but got Unknown
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
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
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
			string[] array2 = array;
			foreach (string text in array2)
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
				//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ca: Expected O, but got Unknown
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
				//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
				//IL_00cb: Expected O, but got Unknown
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
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
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
			string[] array2 = array;
			foreach (string text in array2)
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
				//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
				//IL_00af: Expected O, but got Unknown
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
				//IL_009d: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a7: Expected O, but got Unknown
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
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
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
			int num4 = (payload.HasKingdomSummariesFile ? payload.KingdomSummaries.Count : 0);
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
			int num6 = (payload.HasEventRecordsFile ? payload.EventRecords.Count : 0);
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
				foreach (EventRecordEntry eventRecord in payload.EventRecords)
				{
					string text2 = (eventRecord?.EventId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && hashSet.Contains(text2))
					{
						num5++;
					}
				}
			}
			bool flag = num + num3 + num5 > 0;
			Action action = delegate
			{
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Expected O, but got Unknown
				ApplyImportedEventData(payload, overwriteExisting: true);
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Expected O, but got Unknown
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
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
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
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
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
				object obj = KnowledgeLibraryBehavior.Instance;
				if (obj == null)
				{
					Campaign current = Campaign.Current;
					obj = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
				}
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj;
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
						string text2 = File.ReadAllText(path, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text2);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text3 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text3))
								{
									hashSet2.Add(text3);
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
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_0022: Expected O, but got Unknown
				//IL_0098: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a2: Expected O, but got Unknown
				//IL_007c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0086: Expected O, but got Unknown
				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0066: Expected O, but got Unknown
				string detailMessage;
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true, out detailMessage))
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
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_0022: Expected O, but got Unknown
				//IL_0098: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a2: Expected O, but got Unknown
				//IL_007c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0086: Expected O, but got Unknown
				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0066: Expected O, but got Unknown
				string detailMessage;
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false, out detailMessage))
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
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
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
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
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
			string value = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
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
				//IL_004a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0054: Expected O, but got Unknown
				bool flag2 = VoiceMapper.ImportMappingFromFile(text);
				if (flag2)
				{
					_voiceMappingJsonStorage = VoiceMapper.ExportMappingJson(pretty: false) ?? "";
				}
				InformationManager.DisplayMessage(new InformationMessage(flag2 ? ("导入完成：" + importDir) : "导入失败：VoiceMapping JSON 无效。"));
			};
			Action onSkipDuplicates = delegate
			{
				//IL_004a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0054: Expected O, but got Unknown
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
		//IL_0d47: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d51: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
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
			int num13 = 0;
			int num14 = 0;
			int num15 = 0;
			int num16 = 0;
			int num17 = 0;
			int num18 = 0;
			string vmJson = null;
			string vmPath = null;
			EventImportPayload eventPayload = null;
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				string[] array2 = array;
				foreach (string text in array2)
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
				string[] array3 = files2;
				string[] array4 = array3;
				foreach (string text3 in array4)
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
				string[] array5 = files3;
				string[] array6 = array5;
				foreach (string text5 in array6)
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
					string[] array7 = null;
					try
					{
						array7 = Directory.GetFiles(item, "*.json");
					}
					catch
					{
						array7 = null;
					}
					if (array7 == null)
					{
						continue;
					}
					string[] array8 = array7;
					string[] array9 = array8;
					foreach (string path4 in array9)
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
							int num19 = text8.LastIndexOf("__", StringComparison.Ordinal);
							if (num19 > 0)
							{
								text8 = text8.Substring(0, num19);
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
				object obj4 = KnowledgeLibraryBehavior.Instance;
				if (obj4 == null)
				{
					Campaign current5 = Campaign.Current;
					obj4 = ((current5 != null) ? current5.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
				}
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj4;
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
						string text10 = File.ReadAllText(path5, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text10);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text11 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text11))
								{
									hashSet3.Add(text11);
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
				string text12 = Path.Combine(importDir, "voice_mapping", "VoiceMapping.json");
				if (!File.Exists(text12))
				{
					text12 = Path.Combine(importDir, "VoiceMapping.json");
				}
				if (File.Exists(text12))
				{
					string text13 = File.ReadAllText(text12, Encoding.UTF8);
					if (!string.IsNullOrWhiteSpace(text13))
					{
						vmPath = text12;
						vmJson = text13;
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
						num14 = 1;
						if (!string.IsNullOrWhiteSpace(_eventWorldOpeningSummary))
						{
							num13 = 1;
						}
					}
					if (eventPayload.HasKingdomSummariesFile)
					{
						num16 = eventPayload.KingdomSummaries.Count;
						if (_eventKingdomOpeningSummaries != null)
						{
							foreach (string key4 in eventPayload.KingdomSummaries.Keys)
							{
								if (!string.IsNullOrWhiteSpace(key4) && _eventKingdomOpeningSummaries.ContainsKey(key4))
								{
									num15++;
								}
							}
						}
					}
					if (eventPayload.HasEventRecordsFile)
					{
						num18 = eventPayload.EventRecords.Count;
						HashSet<string> hashSet4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						if (_eventRecordEntries != null)
						{
							foreach (EventRecordEntry eventRecordEntry in _eventRecordEntries)
							{
								string text14 = (eventRecordEntry?.EventId ?? "").Trim();
								if (!string.IsNullOrWhiteSpace(text14))
								{
									hashSet4.Add(text14);
								}
							}
						}
						foreach (EventRecordEntry eventRecord in eventPayload.EventRecords)
						{
							string text15 = (eventRecord?.EventId ?? "").Trim();
							if (!string.IsNullOrWhiteSpace(text15) && hashSet4.Contains(text15))
							{
								num17++;
							}
						}
					}
				}
			}
			catch
			{
				num13 = 0;
				num14 = 0;
				num15 = 0;
				num16 = 0;
				num17 = 0;
				num18 = 0;
				eventPayload = null;
			}
			bool flag2 = num + num3 + num5 + num7 + num9 + num11 + num13 + num15 + num17 > 0;
			Action action = delegate
			{
				//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
				//IL_02d6: Expected O, but got Unknown
				//IL_0395: Unknown result type (might be due to invalid IL or missing references)
				//IL_039f: Expected O, but got Unknown
				//IL_0379: Unknown result type (might be due to invalid IL or missing references)
				//IL_0383: Expected O, but got Unknown
				//IL_035a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0364: Expected O, but got Unknown
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
				_unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson() ?? "";
				if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导入失败，已跳过。原因：" + (string.IsNullOrWhiteSpace(detailMessage) ? "未知错误。" : detailMessage)));
				}
				else if (!string.IsNullOrWhiteSpace(detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage(detailMessage));
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
				//IL_02d9: Expected O, but got Unknown
				//IL_0399: Unknown result type (might be due to invalid IL or missing references)
				//IL_03a3: Expected O, but got Unknown
				//IL_037d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0387: Expected O, but got Unknown
				//IL_035e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0368: Expected O, but got Unknown
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
				_unnamedPersonaJsonStorage = ShoutUtils.ExportUnnamedPersonaStateJson() ?? "";
				if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false, out var detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage("警告：Knowledge 导入失败，已跳过。原因：" + (string.IsNullOrWhiteSpace(detailMessage) ? "未知错误。" : detailMessage)));
				}
				else if (!string.IsNullOrWhiteSpace(detailMessage))
				{
					InformationManager.DisplayMessage(new InformationMessage(detailMessage));
				}
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (flag2)
			{
				string text16 = "导入数据与当前游戏存在重复。\n个性/背景：" + num + "/" + num2 + "\n对话历史：" + num3 + "/" + num4 + "\n欠款：" + num5 + "/" + num6 + "\n未命名NPC：" + num7 + "/" + num8 + "\nKnowledge：" + num9 + "/" + num10 + "\n声音映射：" + num11 + "/" + num12 + "\n世界开局概要：" + num13 + "/" + num14 + "\n王国开局概要：" + num15 + "/" + num16 + "\n事件记录：" + num17 + "/" + num18 + "\n请选择处理方式：";
				ShowDuplicateImportInquiry("检测到重复 - 全部导入", text16, action, onSkipDuplicates, delegate
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
		string detailMessage;
		return ImportKnowledgeFromDir(importDir, overwriteExisting: true, out detailMessage);
	}

	private bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting)
	{
		string detailMessage;
		return ImportKnowledgeFromDir(importDir, overwriteExisting, out detailMessage);
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
				object obj4 = KnowledgeLibraryBehavior.Instance;
				if (obj4 == null)
				{
					Campaign current = Campaign.Current;
					obj4 = ((current != null) ? current.GetCampaignBehavior<KnowledgeLibraryBehavior>() : null);
				}
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = (KnowledgeLibraryBehavior)obj4;
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
								detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount + " 条，失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : ("；原因：" + firstFailedReason));
							}
						}
						else
						{
							detailMessage = ((failedCount > 0) ? ("知识规则全部导入失败。失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : ("；原因：" + firstFailedReason))) : "知识规则写入失败。");
						}
					}
					else
					{
						string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
						if (File.Exists(path))
						{
							string text4 = File.ReadAllText(path);
							KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(text4);
							if (TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile2, overwriteExisting, out var importedCount2, out var failedCount2, out var firstFailedRuleId2, out var firstFailedReason2))
							{
								result = true;
								if (failedCount2 > 0)
								{
									detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount2 + " 条，失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : ("；原因：" + firstFailedReason2));
								}
							}
							else
							{
								detailMessage = ((failedCount2 > 0) ? ("知识规则全部导入失败。失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : ("；原因：" + firstFailedReason2))) : "知识规则写入失败。");
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
			string json = JsonConvert.SerializeObject((object)knowledgeFile, (Formatting)0);
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
				string json2 = JsonConvert.SerializeObject((object)rule, (Formatting)0);
				if (kb.ImportSingleRuleJson(json2, overwriteExisting))
				{
					importedCount++;
					continue;
				}
				failedCount++;
				if (string.IsNullOrWhiteSpace(firstFailedRuleId))
				{
					firstFailedRuleId = (rule.Id ?? "").Trim();
					firstFailedReason = BuildKnowledgeRuleImportFailureMessage(kb, rule, overwriteExisting);
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
			string value = (rule?.Id ?? "").Trim();
			if (rule == null)
			{
				return "规则为空。";
			}
			if (string.IsNullOrWhiteSpace(value))
			{
				return "RuleId 为空。";
			}
			if (rule.RagShortTexts != null)
			{
				for (int i = 0; i < rule.RagShortTexts.Count; i++)
				{
					string text = (rule.RagShortTexts[i] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrWhiteSpace(text) && text.Length > 100)
					{
						return "RAG专用短句超过 " + 100 + " 字符限制。";
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
				if (loreVariant != null)
				{
					string key = BuildKnowledgeWhenSignatureForImport(loreVariant.When);
					if (dictionary.TryGetValue(key, out var value))
					{
						firstIndex = value;
						secondIndex = i;
						return true;
					}
					dictionary[key] = i;
				}
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
			string text6 = ((!(when?.IsFemale).HasValue) ? "any" : (when.IsFemale.Value ? "female" : "male"));
			string text7 = ((!(when?.IsClanLeader).HasValue) ? "any" : (when.IsClanLeader.Value ? "leader" : "not_leader"));
			string text8 = string.Join("|", from kv in NormalizeWhenSkillMinForImport(when?.SkillMin)
				select kv.Key + ":" + kv.Value);
			if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3) && string.IsNullOrEmpty(text4) && string.IsNullOrEmpty(text5) && text6 == "any" && text7 == "any" && string.IsNullOrEmpty(text8))
			{
				return "__generic__";
			}
			return "hero=" + text + ";culture=" + text2 + ";kingdom=" + text3 + ";settlement=" + text4 + ";role=" + text5 + ";gender=" + text6 + ";clan=" + text7 + ";skill=" + text8;
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
			string[] array2 = array;
			foreach (string text2 in array2)
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
			Hero obj = Hero.FindFirst((Func<Hero, bool>)((Hero x) => x != null && ((MBObjectBase)x).StringId == id));
			text = ((obj == null) ? null : ((object)obj.Name)?.ToString()) ?? "";
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
		char[] array = invalidFileNameChars;
		foreach (char oldChar in array)
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
		char[] array = invalidFileNameChars;
		foreach (char oldChar in array)
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
		string contents = JsonConvert.SerializeObject(obj, (Formatting)1);
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
			string text = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(text);
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
			string[] array = files;
			foreach (string path in array)
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
		string stringId = ((MBObjectBase)hero).StringId;
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
		string text = ((hero != null) ? ((MBObjectBase)hero).StringId : null) ?? "_nohero_";
		using SHA256 sHA = SHA256.Create();
		string s = text + "|" + cultureId + "|" + basePrompt + "|" + guardrail + "|" + characterDescription + "|" + personaContext;
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
		StringBuilder stringBuilder = new StringBuilder("direct_", 39);
		for (int i = 0; i < 16; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
