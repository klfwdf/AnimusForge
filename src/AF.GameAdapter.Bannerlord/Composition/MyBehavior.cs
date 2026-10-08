using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
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
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior : CampaignBehaviorBase
{
    private ExternalActionObservationBannerlordAdapter _externalActionObservations;
    private ExternalActionObservationBannerlordAdapter ExternalActionObservations => _externalActionObservations ??= new ExternalActionObservationBannerlordAdapter(new ExternalActionObservationApplication(RecordExternalNpcAction,RecordExternalPlayerAction,RecordNpcMajorAction,RecordNpcRecentAction,RecordEventSourceMaterial),_memoryBusinessState.NextActionSequence,RemoveGenericEscapeRecentActionForPlayerRescue);
	private readonly CampaignMaterialPersistenceAdapter _materialPersistence;
	private readonly CampaignVoicePersonaPersistenceAdapter _voicePersonaPersistence = new CampaignVoicePersonaPersistenceAdapter();
	private readonly PoliticalDecisionRecordCaptureAdapter _politicalDecisionCapture;
	private PatienceResponseBannerlordAdapter _patienceResponseRuntime; private PatienceResponseBannerlordAdapter PatienceResponses => _patienceResponseRuntime ??= new PatienceResponseBannerlordAdapter(_patienceOwner.Snapshot, GetNowCampaignDay, GetRelationLevelText, SyncTownRebelliousStateFromCurrentLoyalty, new PatienceResponseApplication(_patienceOwner.Apply, _patienceOwner.OverrideNeutral));
    private PersonaIdentityPromptCaptureAdapter.PatiencePromptCapturePorts _personaPatiencePromptCapture;
    private PersonaIdentityPromptCaptureAdapter.PatiencePromptCapturePorts PersonaPatiencePromptCapture => _personaPatiencePromptCapture ??= new PersonaIdentityPromptCaptureAdapter.PatiencePromptCapturePorts(hero => PatienceResponses.GetHeroSnapshot(hero), (key, name, display) => PatienceResponses.GetUnnamedSnapshot(key, name, display), GetRelationLevelText);
	public static MyBehavior Instance { get; private set; }

	// Database packages are user-authored files; reject invalid byte sequences instead of silently replacing characters during reload preflight.

	private enum ChatMode
	{
		Normal,
		Give,
		Show
	}

	internal enum WeeklyReportUiStage
	{
		None,
		Failure,
		RetryProgress
	}

	internal enum SaveAndExitStage
	{
		None,
		WaitingForCurrentSave,
		WaitingForRequestedQuickSave
	}

	internal enum SaveAndExitReason
	{
		None,
		MissingOnnx,
		WeeklyReport
	}

	private enum UniversalApiRoute
	{
		Main,
		Auxiliary,
		EventAndRebellion
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

	internal class HeroShownRecord
	{
		public int ShownGold;

		public Dictionary<string, int> ShownItems = new Dictionary<string, int>();
	}

	public enum PartyTransferEntrySection
	{
		PlayerTroops,
		PlayerPrisoners,
		NpcTroops,
		NpcVolunteers,
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

		public Settlement SourceSettlement;

		public Hero VolunteerOwner;

		public List<int> VolunteerSlotIndices;
	}

	public enum SettlementTransferEntrySection
	{
		PlayerFiefs,
		NpcFiefs
	}

	public enum SettlementTransferAssetKind
	{
		Settlement,
		Workshop,
		Caravan
	}

	public sealed class SettlementTransferPromptEntry
	{
		public int PromptIndex;

		public SettlementTransferEntrySection Section;

		public SettlementTransferAssetKind AssetKind;

		public Settlement Settlement;

		public Workshop Workshop;

		public MobileParty CaravanParty;

		public Hero OwnerHero;

		public string SettlementId;

		public string AssetId;

		public string DisplayName;

		public string TypeLabel;

		public int DailyIncomeDenars;

		public int GuidePriceDenars;

		public Clan OwnerClan;
	}

	internal class DialogueDay
	{
		public int GameDayIndex;

		public string GameDate;

		public List<string> Lines = new List<string>();

		// Non-visible idempotency markers for memory-only recovery. They are
		// additive JSON fields; legacy saves deserialize with an empty map.
		public Dictionary<string, string> MemoryCommitMarkers = new Dictionary<string, string>(StringComparer.Ordinal);
	}








	internal sealed class MemorySummaryExecutionResult
	{
		public MemorySummaryInput Source;

		public MemorySummaryJob Job;

		public CompressedMemoryBlock Block;

		public string Error = "";

		// A queued source can disappear while waiting for the daily worker; it is not an API failure and must not retry.
		public bool IsObsolete;

		public bool Success => Block != null;
	}



	internal sealed class MemoryOverviewExecutionResult
	{
		public MemorySummaryInput Source;

		public MemoryOverviewJob Job;

		public MemoryOverviewState State;

		public string Error = "";

		// A queued source can disappear while waiting for the daily worker; it is not an API failure and must not retry.
		public bool IsObsolete;

		public bool Success => State != null && !string.IsNullOrWhiteSpace(State.Summary);
	}



	internal sealed class MajorActionSummaryExecutionResult
	{
		public MemorySummaryInput Source;

		public MajorActionSummaryJob Job;

		public MajorActionSummaryState State;

		public string Error = "";

		// A queued source can disappear while waiting for the daily worker; it is not an API failure and must not retry.
		public bool IsObsolete;

		public bool Success => State != null && !string.IsNullOrWhiteSpace(State.Summary);
	}

	internal sealed class DailySummaryQueueResult
	{
		public MemorySummaryExecutionResult MemoryResult;

		public MajorActionSummaryExecutionResult MajorActionResult;

		public MemoryOverviewExecutionResult MemoryOverviewResult;
	}

	internal sealed class NpcActionFacts
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

	internal class NpcPersonaProfile
	{
		public string HeroId;

		public string HeroName;

		public string Personality;

		public string Background;

		public string VoiceId;
	}


	internal sealed class EventRecordEntry
	{
		public string EventId;

		public int WeekIndex;

		public string EventKind;

		public string ScopeKingdomId;

		public string Title;

		public string ShortSummary;

		// Optional narrative digest, separate from the locally captured event facts.
		public string BulletinAnecdote = "";

		public string Summary;

		public string TagText;

		public string PromptText;

		public int CreatedDay;

		public string CreatedDate;

		// Optional additive JSON metadata; old saves deserialize an empty list.
		// Stored with the issue itself so trimming the 48-layout visual cache cannot
		// remove historical country associations. Native save keys stay unchanged.
		public List<string> BulletinKingdomIds = new List<string>();

		public List<EventMaterialReference> Materials = new List<EventMaterialReference>();
	}

	// A native-conversation preprocess runs off the Bannerlord thread.  Keep its
	// weekly-report input as plain copied values so it never touches Campaign
	// collections or the mutable event record graph.
	public sealed class WeeklyPromptSnapshot
	{
		internal static readonly WeeklyPromptSnapshot Empty = new WeeklyPromptSnapshot("", "", "", "", "", "");

		internal readonly string ShortReportsIncludingNpc;

		internal readonly string ShortReportsExcludingNpc;

		internal readonly string NpcFullReport;

		internal readonly string WorldFullReport;

		internal readonly string SurroundingsFullReport;

		internal readonly string NpcKingdomId;

		internal readonly string SurroundingsKingdomId;

		internal WeeklyPromptSnapshot(string shortReportsIncludingNpc, string shortReportsExcludingNpc, string npcFullReport, string worldFullReport, string surroundingsFullReport, string npcKingdomId, string surroundingsKingdomId = "")
		{
			ShortReportsIncludingNpc = shortReportsIncludingNpc ?? "";
			ShortReportsExcludingNpc = shortReportsExcludingNpc ?? "";
			NpcFullReport = npcFullReport ?? "";
			WorldFullReport = worldFullReport ?? "";
			SurroundingsFullReport = surroundingsFullReport ?? "";
			NpcKingdomId = npcKingdomId ?? "";
			SurroundingsKingdomId = surroundingsKingdomId ?? "";
		}
	}

	internal sealed class WeeklyPromptReportSnapshot
	{
		public readonly int WeekIndex;

		public readonly int CreatedDay;

		public readonly string Title;

		public readonly string ShortSummary;

		public readonly string Summary;

		public WeeklyPromptReportSnapshot(int weekIndex, int createdDay, string title, string shortSummary, string summary)
		{
			WeekIndex = Math.Max(0, weekIndex);
			CreatedDay = Math.Max(0, createdDay);
			Title = title ?? "";
			ShortSummary = shortSummary ?? "";
			Summary = summary ?? "";
		}
	}

	public sealed class WorldWeeklyReportHistoryEntry
	{
		internal WorldWeeklyReportHistoryEntry(string sourceId, int weekIndex, int createdDay, string createdDate, string publishedTitle, string publishedReportText)
		{
			SourceId = sourceId;
			WeekIndex = weekIndex;
			CreatedDay = createdDay;
			CreatedDate = createdDate;
			PublishedTitle = publishedTitle;
			PublishedReportText = publishedReportText;
		}

		public string SourceId { get; }

		public int WeekIndex { get; }

		public int CreatedDay { get; }

		public string CreatedDate { get; }

		public string PublishedTitle { get; }

		// Weeklies retain their published text; bulletins expose only locally captured fact
		// summaries. Narrative bodies/titles and anecdotes never become diplomacy facts.
		public string PublishedReportText { get; }
	}

	public sealed class WeeklyReportBrowserEntryData
	{
		public string EventId;

		internal string ArchiveKind;
		internal string OpenTargetId;
		// Optional card/reader subtitle for non-weekly kinds (diplomacy, policy); null keeps the legacy period label.
		internal string KindLabelText;

		public int WeekIndex;

		public string Title;

		public string BodyText;

		public string CreatedDate;

		public int CreatedDay;

		public string TagText;

		public bool HasFullReport;
	}

	public sealed class WeeklyReportBrowserCountryData
	{
		public string CountryId;

		public string DisplayName;

		public bool IsWorld;

		public List<WeeklyReportBrowserEntryData> Reports = new List<WeeklyReportBrowserEntryData>();
	}


	internal sealed class TownStatSnapshot
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

		public string EntityPostprocessContext;

		// Keep the current turn's preprocessor entities with the prompt context.
		// The main prompt can be assembled after an async/thread boundary, where the
		// AsyncLocal auxiliary-entity cache is no longer a reliable source.
		public MentionedWorldEntities MentionedEntities = new MentionedWorldEntities();

		public List<string> ExplicitMentionedKingdomIds = new List<string>();

		public List<string> PreprocessRuleIds = new List<string>();

		public List<string> PreprocessExcludedRuleIds = new List<string>();

		public string PreprocessExcludedRuleBlock;

		public bool UseDuelContext;

		public bool UseRewardContext;

		public bool IsLoanContext;

		public bool IsQualified;
	}

#pragma warning disable 0649
#pragma warning restore 0649





	internal class PatienceState : PatienceRecord { public PatienceState() { } }

	internal class PatienceStateSaveModel
	{
		public float Value { get; set; }

		public float LastDay { get; set; }

		public int NoInterestRounds { get; set; }

		public int ExhaustedRefusalCount { get; set; }
	}

internal struct PatienceSnapshot
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



	internal enum ExportImportScope
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

	internal class UnnamedPersonaSingleJson
	{
		public string Key;

		public string Personality;

		public string Background;
	}

	internal sealed class EventWorldOpeningSummaryJson
	{
		public string Summary;
	}


	// A fully preflighted, in-memory database replacement plan prevents the U-terminal flow from reading files after it starts changing the save.

	// A snapshot is held only during one confirmation click and restores already-mutated subsystems if an unexpected later step fails.

	internal sealed class WeeklyReportPromptProfile
	{
		public int Preset;

		public int MinWords;

		public int MaxWords;

		public string Label;
	}







	internal sealed class WeeklyReportBatchExecutionResult
	{
		public int BatchIndex;

		public WeeklyReportBatchRequest Batch;

		public WeeklyReportBatchRequestResult Result;

		public long ElapsedMilliseconds;
	}

	internal sealed class DevWeeklyReportBatchPreviewEntry
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

	internal sealed class WeeklyReportGenerationResult
	{
		public int SuccessCount;

		public int FailureCount;

		public bool Completed;

		public bool BlockedByFatalFailure;

		public bool BlockedByChangedRecord;

		public WeeklyReportRetryContext RetryContext;
	}

	internal enum WeeklyPromptPreparationResult
	{
		Canceled,
		Prepared,
		Failed
	}

	internal sealed class PendingWeeklyPromptPreparationContext
	{
		public long RuntimeGeneration;

		public WeeklyMaterialStageCursor<WeeklyReportBatchRequest> Cursor;

		public TaskCompletionSource<WeeklyPromptPreparationResult> CompletionSource;
	}

	internal sealed class PendingWeeklyWaveLaunchContext
	{
		public long RuntimeGeneration;

		public WeeklyReportMaterialRevisionOwner.Snapshot SourceSnapshot;

		public string DisplayLabel;

		public int WaveIndex;

		public int TotalWaves;

		public int TotalTargets;

		public int TotalBatches;

		public int BurstSize;

		public int FirstBatchIndex;

		public List<WeeklyReportBatchRequest> Batches;

		public TaskCompletionSource<List<Task<WeeklyReportBatchExecutionResult>>> CompletionSource;
	}

	internal sealed class PendingWeeklyBatchApiAttemptContext
	{
		public long RuntimeGeneration;

		public string SystemPrompt;

		public string UserPrompt;

		public TaskCompletionSource<Task<ApiCallResult>> CompletionSource;
	}

	internal sealed class PendingWeeklyReportCommitContext
	{
		public long RuntimeGeneration;

		public WeeklyReportMaterialRevisionOwner.Snapshot SourceSnapshot;

		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public string DisplayLabel = "";

		public bool OpenViewerWhenDone;

		public bool QueueBlockingPopupOnFatalFailure;

		public bool IsAutoGeneration;

		public List<string> PopupCandidateKingdomIds = new List<string>();

		public bool RequiresFreshMaterials;

		public bool WeeklyReportNoticeNearestKingdomResolved;

		public string WeeklyReportNoticeNearestKingdomId = "";

		public List<WeeklyEventMaterialPreviewGroup> Groups = new List<WeeklyEventMaterialPreviewGroup>();

		public Dictionary<string, WeeklyEventMaterialPreviewGroup> GroupMap;

		public Dictionary<string, string> CapturedRecordStates;

		public List<WeeklyReportBatchExecutionResult> Executions = new List<WeeklyReportBatchExecutionResult>();

		public int ExecutionIndex;

		public int BlockIndex;

		public PendingWeeklyReportBlockCommit CurrentBlockCommit;

		public bool CurrentBlockRejected;

		public bool CurrentPreviewCaptured;

		public HashSet<string> CurrentParsedReportIds;

		public WeeklyReportCommitTargetOwner<WeeklyEventMaterialPreviewGroup> Targets = new WeeklyReportCommitTargetOwner<WeeklyEventMaterialPreviewGroup>();

		public List<string> FailureMessages = new List<string>();

		public List<WeeklyEventMaterialPreviewGroup> FailedGroups = new List<WeeklyEventMaterialPreviewGroup>();

		public int SuccessCount;

		public int FailureCount;

		public HashSet<string> WeeklyReportNoticeEventIdsQueued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public HashSet<string> AttemptedWriteReportIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public TaskCompletionSource<WeeklyReportGenerationResult> CompletionSource;
	}

	internal sealed class PendingWeeklyReportBlockCommit
	{
		public WeeklyEventMaterialPreviewGroup Group;

		public string ReportId = "";

		public string Title = "";

		public string ShortSummary = "";

		public string Report = "";

		public string TagText = "";

		public string PromptText = "";

		public WeeklyReportBlockMaterialCursor<EventMaterialReference> MaterialCursor;
	}

	internal enum DailyMaintenanceTaskKind
	{
		SealPastDailyMemoryDrafts,
		StartMemorySummaryQueue,
		QueueDirtyMemoryOverviewScan,
		QueueFullMemoryOverviewScan,
		DiscontinueLandlessRebelKingdoms,
		EnsureWeekZeroOpeningSummaryEvents,
		RecordMissedStrategicWorldEvents,
		ApplyKingdomStabilityRelationAdjustments,
		ProcessWeeklyKingdomRebellions,
		PrepareAutoWeeklyReports
	}

	internal sealed class DailyMaintenanceJob
	{
		public DailyMaintenanceTaskKind Kind;

		public int DayIndex;

		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public string Reason = "";
	}

	internal sealed class PendingAutoWeeklyReportBuild
	{
		public WeeklyReportMaterialRevisionOwner.Snapshot SourceSnapshot;

		public int WeekIndex;

		public int StartDay;

		public int EndDay;

		public bool PreviewGroupsInitialized;

		public bool PreviewGroupsComplete;

		public int PreviewSourceMaterialIndex;

		public WeeklyActionMaterialCursor<NpcActionEntry, Hero> RecentActionCursor;

		public WeeklyActionMaterialCursor<NpcActionEntry, Hero> MajorActionCursor;

		public WeeklyMaterialStageCursor<WeeklyEventMaterialPreviewGroup> AggregationCursor;

		public HashSet<string> FullReportKingdomIds;

		public WeeklyMaterialStageCursor<WeeklyEventMaterialPreviewGroup> PromptMaterialCursor;

		public bool Ordered;

		public WeeklyMaterialStageCursor<WeeklyReportBatchRequest> BatchPromptCursor;

		public List<Kingdom> Kingdoms = new List<Kingdom>();

		public List<WeeklyEventMaterialPreviewGroup> Groups = new List<WeeklyEventMaterialPreviewGroup>();

		public List<WeeklyReportBatchRequest> Batches = new List<WeeklyReportBatchRequest>();

		public List<EventSourceMaterialEntry> EventSourceMaterialSnapshot = new List<EventSourceMaterialEntry>();
	}

	internal sealed class WeeklyReportRetryContext
	{
		public WeeklyReportMaterialRevisionOwner.Snapshot SourceSnapshot;

		public List<WeeklyEventMaterialPreviewGroup> Groups = new List<WeeklyEventMaterialPreviewGroup>();

		public Dictionary<string, string> CapturedRecordStates;

		public List<string> PopupCandidateKingdomIds = new List<string>();

		public bool RequiresFreshMaterials;

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

	internal sealed class WeekZeroShortSummaryRequest
	{
		public string EventId;

		public string EventKind;

		public string KingdomId;

		public string Title;

		public string Summary;

		public string SourceHash;

		public string GenerationKey;
	}

	internal sealed class KingdomRebellionCandidateInfo
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

	internal sealed class KingdomRebellionFollowerInfo
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

	internal sealed class KingdomRebellionResolutionResult
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

	internal sealed class RebelKingdomNamingResult
	{
		public string FormalName;

		public string ShortName;

		public string EncyclopediaText;

		public bool Success;

		public string FailureReason;

		public int AttemptsUsed;

		public bool IsRateLimit;

		public bool IsRequestsPerMinuteLimit;

		public bool IsQuotaLimit;

		public int? RetryAfterSeconds;
	}

	internal sealed class PendingDevForcedKingdomRebellionContext
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

	internal sealed class PendingAutomaticKingdomRebellionContext
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

		public string CivilWarFactionId;

		public RebelKingdomNamingResult NamingResult;
	}

	internal sealed class ClanVisualSnapshot
	{
		public Banner Banner;

		public uint Color;

		public uint Color2;

		public uint BackgroundColor;

		public uint IconColor;
	}

	internal sealed class RebelFactionColorChoice
	{
		public uint BackgroundColor;

		public uint IconColor;

		public Banner Banner;
	}

	internal sealed class ApiCallResult
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

	private readonly ShownResourceRecordOwner _shownResourceRecords = new ShownResourceRecordOwner();
	private Dictionary<string, HeroShownRecord> _shownRecords { get => _shownResourceRecords.Records; set => _shownResourceRecords.Records = value; }

	private readonly CampaignShownRecordPersistenceAdapter _shownPersistence;
	private Dictionary<string, string> _shownRecordStorage { get => _shownPersistence.Storage; set => _shownPersistence.Storage = value; }

	private readonly BuiltInRuleStickyCarry _builtInRuleStickyCarry = new BuiltInRuleStickyCarry();

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

	private const int RecentNpcActionWindowDays = NpcActionLedger.RecentWindowDays;

	private const int MaxRecentNpcActionEntriesPerHero = NpcActionLedger.MaxRecentEntriesPerHero;

	private const int MaxMajorNpcActionEntriesPerHero = NpcActionLedger.MaxMajorEntriesPerHero;

	private const int MajorNpcBattleTroopThreshold = 500;

	private const int UniversalApiDefaultMaxTokens = DuelSettings.DefaultGeneralApiMaxTokens;

	private const int EventAndRebellionApiDefaultMaxTokens = DuelSettings.DefaultEventAndRebellionApiMaxTokens;

	private const int KingdomStabilityMinValue = 0;

	private const int KingdomStabilityMaxValue = 100;

	private const int KingdomStabilityDefaultValue = 50;

	private const int PlayerCreatedKingdomInitialStabilityValue = KingdomStabilityGameAdapter.PlayerCreatedKingdomInitialStabilityValue;

	private const int RebelKingdomInitialStabilityValue = 50;


	private const double ShoutPromptContextSlowStageMs = 1000.0;

	private const double ShoutPromptContextHardBudgetMs = 15000.0;

	private Dictionary<string, List<DialogueDay>> _dialogueHistory { get => _memoryBusinessState.History; set => _memoryBusinessState.History = value; }

	private Dictionary<string, string> _dialogueHistoryStorage { get => _memoryBusinessState.HistoryStorage; set => _memoryBusinessState.HistoryStorage = value; }

    private Func<Hero, CharacterObject, int, string> _npcMajorRuleCapture;
    private Func<Hero, CharacterObject, int, string> NpcMajorRuleCapture => _npcMajorRuleCapture ??= (hero, character, agentIndex) =>
        MemoryEntityIdentityBannerlordAdapter.BuildNpcMajorActionsRuntimeInstruction(
            _memoryBusinessState, _npcActionRecords, GetCurrentGameDayIndexSafe,
            PromptRuleCaptureBannerlordAdapter.ResolveRuleTargetKey, hero, character, agentIndex);
	private readonly MemoryBusinessStateOwner _memoryBusinessState = new MemoryBusinessStateOwner();
	private CampaignMemoryPersistenceAdapter _memoryPersistence;
	private CampaignMemoryPersistenceAdapter MemoryPersistence => _memoryPersistence ??= new CampaignMemoryPersistenceAdapter(_memoryBusinessState);

	private Dictionary<string, List<DailyMemoryDraft>> _dailyMemoryDrafts { get => _memoryBusinessState.Drafts; set => _memoryBusinessState.Drafts = value; }

	private Dictionary<string, string> _dailyMemoryDraftStorage { get => _memoryBusinessState.DraftStorage; set => _memoryBusinessState.DraftStorage = value; }

	private Dictionary<string, List<CompressedMemoryBlock>> _compressedMemoryBlocks { get => _memoryBusinessState.Blocks; set => _memoryBusinessState.Blocks = value; }

	private Dictionary<string, string> _compressedMemoryBlockStorage { get => _memoryBusinessState.BlockStorage; set => _memoryBusinessState.BlockStorage = value; }

	private List<WeeklyMemoryMaterialTrigger> _pendingWeeklyMemoryMaterialTriggers { get => _memoryBusinessState.PendingWeeklyTriggers; set => _memoryBusinessState.PendingWeeklyTriggers = value; }

	private List<MemorySummaryJob> _memorySummaryQueue { get => _memoryBusinessState.DailyQueue; set => _memoryBusinessState.DailyQueue = value; }

	private string _memorySummaryQueueJsonStorage { get => MemoryPersistence.DailyQueueJson; set => MemoryPersistence.DailyQueueJson = value; }


	private ref bool _memorySummaryFailurePopupActive => ref _memoryFailureNotices.Active;

	private int _nativeConversationMemorySessionCounter { get => _memoryBusinessState._nativeConversationMemorySessionCounter; set => _memoryBusinessState._nativeConversationMemorySessionCounter = value; }

	private int _activeNativeConversationMemorySessionId { get => _memoryBusinessState._activeNativeConversationMemorySessionId; set => _memoryBusinessState._activeNativeConversationMemorySessionId = value; }

	private string _memoryRuntimeSessionKey { get => _memoryBusinessState._memoryRuntimeSessionKey; set => _memoryBusinessState._memoryRuntimeSessionKey = value; }

	private readonly MemoryEntityIdentityBannerlordAdapter _memoryEntityIdentity = new MemoryEntityIdentityBannerlordAdapter();
	private readonly Action<string,string> _memoryIdentityMerge;
	private ref Dictionary<MobileParty, string> _wildernessNonHeroPartyMemoryIds => ref _memoryEntityIdentity.PartyMemoryIds;

	// Both destruction callbacks are raised for the same party. The second pass would only rescan empty memory containers.
	private const int DestroyedPartyMemoryCleanupDedupLimit = CampaignBattleRecordCaptureAdapter.DestroyedPartyMemoryCleanupDedupLimit;

	private HashSet<PartyBase> _destroyedPartyMemoryCleanupDedup => _campaignBattleRecordCapture.DestroyedPartyMemoryCleanupDedup;

	private Dictionary<string, MemoryOverviewState> _memoryOverviewStates { get => _memoryBusinessState.Overviews; set => _memoryBusinessState.Overviews = value; }

	private Dictionary<string, string> _memoryOverviewStateStorage { get => _memoryBusinessState.OverviewStorage; set => _memoryBusinessState.OverviewStorage = value; }

	private List<MemoryOverviewJob> _memoryOverviewQueue { get => _memoryBusinessState.OverviewQueue; set => _memoryBusinessState.OverviewQueue = value; }

	private string _memoryOverviewQueueJsonStorage { get => MemoryPersistence.OverviewQueueJson; set => MemoryPersistence.OverviewQueueJson = value; }

	private Dictionary<string, MajorActionSummaryState> _npcMajorActionSummaries { get => _memoryBusinessState.MajorSummaries; set => _memoryBusinessState.MajorSummaries = value; }

	private Dictionary<string, string> _npcMajorActionSummaryStorage { get => _memoryBusinessState.MajorStorage; set => _memoryBusinessState.MajorStorage = value; }

	private List<MajorActionSummaryJob> _npcMajorActionSummaryQueue { get => _memoryBusinessState.MajorQueue; set => _memoryBusinessState.MajorQueue = value; }

	private string _npcMajorActionSummaryQueueJsonStorage { get => MemoryPersistence.MajorQueueJson; set => MemoryPersistence.MajorQueueJson = value; }

	private const double DailyMaintenanceDefaultFrameBudgetMs = 3.0;

	private const int DailyMaintenanceMaxJobsPerTick = 8;

	private Queue<DailyMaintenanceJob> _dailyMaintenanceQueue => _dailyMaintenanceController.Jobs;

	private HashSet<string> _dailyMaintenanceJobKeys => _dailyMaintenanceController.JobKeys;

	private HashSet<string> _dirtyMemoryOverviewIds { get => _memoryBusinessState.DirtyOverviewIds; }

	private Queue<string> _pendingMemoryOverviewCandidateScanIds { get => _memoryBusinessState.OverviewCandidateIds; }

	private HashSet<string> _pendingMemoryOverviewCandidateScanIdSet { get => _memoryBusinessState.OverviewCandidateIdSet; }

	private PendingAutoWeeklyReportBuild _pendingAutoWeeklyReportBuild { get => _weeklyEventRecords.PendingBuild; set => _weeklyEventRecords.PendingBuild=value; }

	private List<string> _dailyMemoryDraftSealOwnerKeys { get => _memoryBusinessState.Sealing.OwnerKeys; set => _memoryBusinessState.Sealing.OwnerKeys = value; }

	private int _dailyMemoryDraftSealOwnerIndex { get => _memoryBusinessState.Sealing.OwnerIndex; set => _memoryBusinessState.Sealing.OwnerIndex = value; }

	private int _dailyMemoryDraftSealDraftIndex { get => _memoryBusinessState.Sealing.DraftIndex; set => _memoryBusinessState.Sealing.DraftIndex = value; }

	private int _dailyMemoryDraftSealTargetDay { get => _memoryBusinessState.Sealing.TargetDay; set => _memoryBusinessState.Sealing.TargetDay = value; }

	private HashSet<string> _dailyMemoryDraftSealQueued { get => _memoryBusinessState.Sealing.Queued; set => _memoryBusinessState.Sealing.Queued = value; }

	private HashSet<string> _dailyMemoryDraftSealQueuedMajor { get => _memoryBusinessState.Sealing.QueuedMajor; set => _memoryBusinessState.Sealing.QueuedMajor = value; }

	private readonly WeeklyReportCommitQueueOwner<PendingWeeklyReportCommitContext, WeeklyReportGenerationResult> _weeklyReportCommitQueue =
		new WeeklyReportCommitQueueOwner<PendingWeeklyReportCommitContext, WeeklyReportGenerationResult>(CompletePendingWeeklyReportCommit, () => new WeeklyReportGenerationResult());

	private readonly WeeklyReportCommitQueueOwner<PendingWeeklyPromptPreparationContext, WeeklyPromptPreparationResult> _weeklyPromptPreparationQueue =
		new WeeklyReportCommitQueueOwner<PendingWeeklyPromptPreparationContext, WeeklyPromptPreparationResult>(CompletePendingWeeklyPromptPreparation, () => WeeklyPromptPreparationResult.Canceled);

	private readonly WeeklyReportCommitQueueOwner<PendingWeeklyWaveLaunchContext, List<Task<WeeklyReportBatchExecutionResult>>> _weeklyWaveLaunchQueue =
		new WeeklyReportCommitQueueOwner<PendingWeeklyWaveLaunchContext, List<Task<WeeklyReportBatchExecutionResult>>>(CompletePendingWeeklyWaveLaunch, () => null);

	private readonly WeeklyReportCommitQueueOwner<PendingWeeklyBatchApiAttemptContext, Task<ApiCallResult>> _weeklyBatchApiAttemptQueue =
		new WeeklyReportCommitQueueOwner<PendingWeeklyBatchApiAttemptContext, Task<ApiCallResult>>(CompletePendingWeeklyBatchApiAttempt, () => null);

	private KingdomMaintenanceOwner<Kingdom> _kingdomMaintenance = new KingdomMaintenanceOwner<Kingdom>();
	private KingdomMaintenanceOwner<Kingdom> KingdomMaintenance => _kingdomMaintenance ??= new KingdomMaintenanceOwner<Kingdom>();

	private bool _weekZeroOpeningSummaryMaintenanceWorldProcessed { get => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceWorldProcessed; set => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceWorldProcessed=value; }

	private List<Kingdom> _weekZeroOpeningSummaryMaintenanceKingdoms { get => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceKingdoms; set => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceKingdoms=value; }

	private int _weekZeroOpeningSummaryMaintenanceCursor { get => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceCursor; set => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceCursor=value; }

	private bool _weekZeroOpeningSummaryMaintenanceChanged { get => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceChanged; set => _weekZeroShortSummaries._weekZeroOpeningSummaryMaintenanceChanged=value; }

	private List<Kingdom> _missedStrategicWorldEventMaintenanceKingdoms { get => _worldBulletinEventCapture._missedStrategicWorldEventMaintenanceKingdoms; set => _worldBulletinEventCapture._missedStrategicWorldEventMaintenanceKingdoms=value; }

	private HashSet<string> _missedStrategicWorldEventMaintenanceStableKeys { get => _worldBulletinEventCapture._missedStrategicWorldEventMaintenanceStableKeys; set => _worldBulletinEventCapture._missedStrategicWorldEventMaintenanceStableKeys=value; }

	private int _missedStrategicWorldEventMaintenanceCursor { get => _worldBulletinEventCapture._missedStrategicWorldEventMaintenanceCursor; set => _worldBulletinEventCapture._missedStrategicWorldEventMaintenanceCursor=value; }

	private List<EventSourceMaterialEntry> _weeklyEventSourceMaterialBuildSnapshot { get => _campaignMaterialRecords.WeeklyBuildSnapshot; set => _campaignMaterialRecords.WeeklyBuildSnapshot=value; }

	private const double MemoryOverviewCandidateScanThrottleSeconds = 10.0;

	private int _lastMemoryMaintenanceObservedGameDay { get => _dailyMaintenanceController.LastMemoryMaintenanceObservedGameDay; set => _dailyMaintenanceController.LastMemoryMaintenanceObservedGameDay = value; }

	private long _lastMemoryOverviewCandidateScanUtcTicks { get => _memoryBusinessState.LastMemoryOverviewCandidateScanUtcTicks; set => _memoryBusinessState.LastMemoryOverviewCandidateScanUtcTicks=value; }

	private Dictionary<string, List<NpcActionEntry>> _npcMajorActions { get => _memoryBusinessState.MajorActions; set => _memoryBusinessState.MajorActions = value; }

	private Dictionary<string, string> _npcMajorActionStorage { get => _memoryBusinessState.MajorActionStorage; set => _memoryBusinessState.MajorActionStorage = value; }

	private Dictionary<string, List<NpcActionEntry>> _npcRecentActions { get => _memoryBusinessState.RecentActions; set => _memoryBusinessState.RecentActions = value; }

	private Dictionary<string, string> _npcRecentActionStorage { get => _memoryBusinessState.RecentActionStorage; set => _memoryBusinessState.RecentActionStorage = value; }


	private int _npcActionGlobalOrderCounter { get => _memoryBusinessState.ActionGlobalOrderCounter; set => _memoryBusinessState.ActionGlobalOrderCounter = value; }

	private readonly PersonaProfileStateOwner _personaProfiles = new PersonaProfileStateOwner();
	private Dictionary<string, NpcPersonaProfile> _npcPersonaProfiles { get => _personaProfiles.Profiles; set => _personaProfiles.Profiles = value; }

	private readonly CampaignPersonaPersistenceAdapter _personaPersistence;
	private Dictionary<string, string> _npcPersonaProfileStorage { get => _personaPersistence.Storage; set => _personaPersistence.Storage = value; }

	private readonly CampaignCharacterRecordCaptureAdapter _campaignCharacterRecordCapture;
    private readonly WeeklyTownStatCaptureAdapter _weeklyTownStatCapture;
    private readonly Action<Kingdom,string> _settlementOwnerChangeRebelCleanup;
	private readonly MemoryHistoryCommitBannerlordAdapter _memoryHistoryCommit;
 private readonly Func<MemorySealingPort> _captureMemorySealingPort;
 private readonly CampaignDailyMaintenanceController _dailyMaintenanceController;
 private readonly WorldBulletinEventCaptureAdapter _worldBulletinEventCapture;
 private readonly KingdomStabilityGameAdapter _kingdomStabilityGameAdapter;
 private static readonly Func<Kingdom, int> _readCurrentKingdomStability = kingdom => Instance?._kingdomStabilityGameAdapter.GetKingdomStabilityValue(kingdom) ?? KingdomStabilityPolicy.KingdomStabilityDefaultValue;
 private readonly Func<bool,Task> _startMemorySummaryRun;
 private readonly MemoryHistoryContextReadCapabilities _memoryHistoryContext;
	private readonly CampaignSaveExitController _campaignSaveExit;
	private readonly WeeklyEventRecordStateOwner _weeklyEventRecords = new WeeklyEventRecordStateOwner();
	private Dictionary<string, string> _eventKingdomOpeningSummaries { get => _weeklyEventRecords.KingdomOpenings; set => _weeklyEventRecords.KingdomOpenings = value; }

	private readonly CampaignWeeklyRecordPersistenceAdapter _weeklyRecordPersistence;
	private Dictionary<string, string> _eventKingdomOpeningSummaryStorage { get => _weeklyRecordPersistence.OpeningStorage; set => _weeklyRecordPersistence.OpeningStorage = value; }

	private string _eventWorldOpeningSummary { get => _weeklyEventRecords.WorldOpening; set => _weeklyEventRecords.WorldOpening = value; }

	private List<EventRecordEntry> _eventRecordEntries { get => _weeklyEventRecords.Records; set => _weeklyEventRecords.Records = value; }
	private long _publishedWorldWeeklyHistoryRevision { get => _weeklyEventRecords.PublishedHistoryRevision; set => _weeklyEventRecords.PublishedHistoryRevision = value; }
	// Runtime-only revision lets the open world-message timeline detect weekly additions without resanitizing this list every frame.
	private long _worldMessageWeeklyTimelineRevision { get => _weeklyEventRecords.TimelineRevision; set => _weeklyEventRecords.TimelineRevision = value; }

	private string _eventRecordJsonStorage { get => _weeklyRecordPersistence.RecordJsonStorage; set => _weeklyRecordPersistence.RecordJsonStorage = value; }


	private readonly WeeklyReportMaterialRevisionOwner _weeklyReportMaterialRevisions = new WeeklyReportMaterialRevisionOwner();

	private string _eventSourceMaterialJsonStorage { get => _materialPersistence.JsonStorage; set => _materialPersistence.JsonStorage = value; }


	private KingdomStabilityOwner _kingdomStability = new KingdomStabilityOwner();
	private KingdomStabilityOwner KingdomStability => _kingdomStability ??= new KingdomStabilityOwner();

	private Dictionary<string, int> _kingdomStabilityValues { get => KingdomStability.Values; set => KingdomStability.Values = value; }

	private readonly CampaignKingdomPersistenceAdapter _kingdomPersistence;
	private Dictionary<string,string> _kingdomStabilityStorage { get => _kingdomPersistence.StabilityStorage; set => _kingdomPersistence.StabilityStorage = value; }

	private Dictionary<string, int> _kingdomStabilityRelationAppliedOffsets { get => KingdomStability.RelationOffsets; set => KingdomStability.RelationOffsets = value; }

	private Dictionary<string,string> _kingdomStabilityRelationOffsetStorage { get => _kingdomPersistence.RelationStorage; set => _kingdomPersistence.RelationStorage = value; }

	private Dictionary<string, int> _weeklyReportAppliedStabilityDeltas { get => KingdomStability.WeeklyDeltas; set => KingdomStability.WeeklyDeltas = value; }

	private Dictionary<string,string> _weeklyReportAppliedStabilityDeltaStorage { get => _kingdomPersistence.WeeklyStorage; set => _kingdomPersistence.WeeklyStorage = value; }

	private readonly CampaignCivilWarPersistenceAdapter _civilWarPersistence;
	private string _civilWarJsonStorage { get => _civilWarPersistence.JsonStorage; set => _civilWarPersistence.JsonStorage=value; }

	private readonly RebelKingdomIdentityOwner _rebelKingdomIdentity = new RebelKingdomIdentityOwner();
 private readonly KingdomRebellionGameAdapter _kingdomRebellionGameAdapter;
 private readonly KingdomRebellionRuntimeController _kingdomRebellionRuntime;

	private Dictionary<string,string> _modCreatedRebelKingdomIdStorage { get => _kingdomPersistence.RebelStorage; set => _kingdomPersistence.RebelStorage = value; }

	private int _lastAutoGeneratedWeeklyReportWeek { get => _weeklyEventRecords.LastAutoGeneratedWeek; set => _weeklyEventRecords.LastAutoGeneratedWeek=value; }

	private ref int _lastProcessedKingdomRebellionWeek => ref _kingdomRebellionRuntime.LastProcessedWeek;

	private const int WeeklyReportReadingXpBatchSize = WeeklyNoticeGameAdapter.WeeklyReportReadingXpBatchSize;
    private static readonly Func<string,string,int,string> _weeklyNoticeDefaultTitle = (kind, kingdom, week) => WeeklyEditorProjection.BuildWeeklyReportBrowserDefaultTitle(WeeklyEditorDisplay, kind, kingdom, week);
    private static readonly Func<EventRecordEntry,string> _weeklyNoticeSubtitle = entry => WeeklyEventRecordStateOwner.BuildWeeklyReportPopupSubtitle(entry, MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay, PersonaIdentityPromptCaptureAdapter.GetSeasonTextZhForPrompt);

	private readonly WeeklyNoticeStateOwner _weeklyNoticeOwner = new WeeklyNoticeStateOwner();
	private readonly WeeklyNoticeGameAdapter _weeklyNoticeGameAdapter;
 private WeeklyNoticePort _weeklyNoticePort;
 private WeeklyNoticePort WeeklyNoticePort => _weeklyNoticePort ??= new WeeklyNoticePort { FindRecord = FindWeeklyReportRecordById, IsBulletin = IsWorldBulletinEventId, NearestKingdom = ResolveNearestWeeklyReportKingdomId, Log = Logger.Log, Publish = record => MBInformationManager.AddNotice(new AnimusForgeWeeklyReportMapNotification(record.EventId, BuildWeeklyReportNoticeTitle(record), BuildWeeklyReportNoticeDescription(record))) };
 private List<string> _unreadWeeklyReportNoticeEventIds { get => _weeklyNoticeOwner.Unread; set => _weeklyNoticeOwner.Unread = value; }

	private List<string> _weeklyReportReadingXpClaimedEventIds { get => _weeklyNoticeOwner.ReadingXpClaimedEventIds; set => _weeklyNoticeOwner.ReadingXpClaimedEventIds = value; }

	private int _weeklyReportReadingXpPendingCount { get => _weeklyNoticeOwner.ReadingXpPendingCount; set => _weeklyNoticeOwner.ReadingXpPendingCount = value; }

	private int _weeklyReportReadingXpPendingCharm { get => _weeklyNoticeOwner.ReadingXpPendingCharm; set => _weeklyNoticeOwner.ReadingXpPendingCharm = value; }

	private int _weeklyReportReadingXpPendingLeadership { get => _weeklyNoticeOwner.ReadingXpPendingLeadership; set => _weeklyNoticeOwner.ReadingXpPendingLeadership = value; }

	private int _weeklyReportReadingXpPendingSteward { get => _weeklyNoticeOwner.ReadingXpPendingSteward; set => _weeklyNoticeOwner.ReadingXpPendingSteward = value; }

	private HashSet<string> _weeklyReportNoticeEventIdsShownThisSession { get => _weeklyNoticeOwner.Shown; }

	private bool _weeklyReportNoticeQueueNormalizedForCurrentPolicy { get => _weeklyNoticeOwner.NormalizedForPolicy; set => _weeklyNoticeOwner.NormalizedForPolicy = value; }

	private MapNotificationView _weeklyReportRegisteredMapNotificationView { get => _weeklyNoticeGameAdapter.RegisteredMapNotificationView; set => _weeklyNoticeGameAdapter.RegisteredMapNotificationView = value; }

	private bool _weeklyReportGenerationInProgress { get => _weeklyEventRecords.GenerationInProgress; set => _weeklyEventRecords.GenerationInProgress=value; }

	private WeeklyReportUiStage _weeklyReportUiStage { get => WeeklyEditor.UiStage; set => WeeklyEditor.UiStage = value; }

	private WeeklyReportRetryContext _weeklyReportRetryContext { get => WeeklyEditor.RetryContext; set => WeeklyEditor.RetryContext = value; }

	private bool _weeklyReportManualRetryInProgress { get => WeeklyEditor.ManualRetryInProgress; set => WeeklyEditor.ManualRetryInProgress = value; }

	private int _weeklyReportManualRetryVersion { get => WeeklyEditor.ManualRetryVersion; set => WeeklyEditor.ManualRetryVersion = value; }

	private List<DevWeeklyReportBatchPreviewEntry> _latestWeeklyReportBatchDevPreviews = new List<DevWeeklyReportBatchPreviewEntry>();

	private bool _pendingWeeklyReportManualRetryResult { get => WeeklyEditor.PendingManualRetryResult; set => WeeklyEditor.PendingManualRetryResult = value; }

	private bool _pendingWeeklyReportManualRetrySucceeded { get => WeeklyEditor.PendingManualRetrySucceeded; set => WeeklyEditor.PendingManualRetrySucceeded = value; }

	private string _pendingWeeklyReportManualRetryMessage { get => WeeklyEditor.PendingManualRetryMessage; set => WeeklyEditor.PendingManualRetryMessage = value; }

	private WeeklyReportRetryContext _pendingWeeklyReportManualRetryContext { get => WeeklyEditor.PendingManualRetryContext; set => WeeklyEditor.PendingManualRetryContext = value; }



	private long _weeklyReportUiResumeAfterUtcTicks { get => WeeklyEditor.UiResumeAfterUtcTicks; set => WeeklyEditor.UiResumeAfterUtcTicks = value; }

	private bool _weeklyReportReopenAfterApiConfig { get => WeeklyEditor.ReopenAfterApiConfig; set => WeeklyEditor.ReopenAfterApiConfig = value; }

	private long _weeklyReportReopenAfterApiConfigUtcTicks { get => WeeklyEditor.ReopenAfterApiConfigUtcTicks; set => WeeklyEditor.ReopenAfterApiConfigUtcTicks = value; }

	private bool _missingOnnxGateActive { get => _campaignSaveExit._missingOnnxGateActive; set => _campaignSaveExit._missingOnnxGateActive = value; }
    internal bool IsMissingOnnxGateActive => _missingOnnxGateActive;

	private long _missingOnnxGateResumeAfterUtcTicks { get => _campaignSaveExit._missingOnnxGateResumeAfterUtcTicks; set => _campaignSaveExit._missingOnnxGateResumeAfterUtcTicks = value; }

	private bool _pendingMissingOnnxGateCheck { get => _campaignSaveExit._pendingMissingOnnxGateCheck; set => _campaignSaveExit._pendingMissingOnnxGateCheck = value; }

	private long _pendingMissingOnnxGateCheckAfterUtcTicks { get => _campaignSaveExit._pendingMissingOnnxGateCheckAfterUtcTicks; set => _campaignSaveExit._pendingMissingOnnxGateCheckAfterUtcTicks = value; }

	private SaveAndExitStage _saveAndExitStage { get => _campaignSaveExit._saveAndExitStage; set => _campaignSaveExit._saveAndExitStage = value; }

	private SaveAndExitReason _saveAndExitReason { get => _campaignSaveExit._saveAndExitReason; set => _campaignSaveExit._saveAndExitReason = value; }

	private AutomaticKingdomRebellionOwner<PendingAutomaticKingdomRebellionContext> _automaticKingdomRebellions = new AutomaticKingdomRebellionOwner<PendingAutomaticKingdomRebellionContext>();
	private AutomaticKingdomRebellionOwner<PendingAutomaticKingdomRebellionContext> AutomaticKingdomRebellions => _automaticKingdomRebellions ??= new AutomaticKingdomRebellionOwner<PendingAutomaticKingdomRebellionContext>();

	// Keep Coup independent of the rebellion scheduler's backing fields. Main thread only.
	internal bool HasBlockingRebellionFlowForCoup()
	{
		return AutomaticKingdomRebellions.FlowActive || !AutomaticKingdomRebellions.CanStart
			|| _devForcedKingdomRebellionInProgress || _weeklyReportGenerationInProgress;
	}

	private bool _devForcedKingdomRebellionInProgress { get => KingdomRebellionEditor.DevForcedInProgress; set => KingdomRebellionEditor.DevForcedInProgress = value; }

	private bool _pendingDevForcedKingdomRebellionReady { get => KingdomRebellionEditor.PendingDevReady; set => KingdomRebellionEditor.PendingDevReady = value; }

	private PendingDevForcedKingdomRebellionContext _pendingDevForcedKingdomRebellionContext { get => KingdomRebellionEditor.PendingDevContext; set => KingdomRebellionEditor.PendingDevContext = value; }

	private PendingAutomaticKingdomRebellionContext _blockedAutomaticKingdomRebellionContext { get => KingdomRebellionEditor.BlockedAutomaticContext; set => KingdomRebellionEditor.BlockedAutomaticContext = value; }

	private PendingDevForcedKingdomRebellionContext _blockedDevForcedKingdomRebellionContext { get => KingdomRebellionEditor.BlockedDevContext; set => KingdomRebellionEditor.BlockedDevContext = value; }

	private bool _kingdomRebellionReopenAfterApiConfig { get => KingdomRebellionEditor.ReopenAfterApiConfig; set => KingdomRebellionEditor.ReopenAfterApiConfig = value; }

	private long _kingdomRebellionReopenAfterApiConfigUtcTicks { get => KingdomRebellionEditor.ReopenAfterApiConfigUtcTicks; set => KingdomRebellionEditor.ReopenAfterApiConfigUtcTicks = value; }

	private ConcurrentQueue<Action> _kingdomRebellionNamingMainThreadActions => _kingdomRebellionRuntime.NamingMainThreadActions;

	private readonly WeeklyAutoScheduleOwner _weeklyAutoSchedule = new WeeklyAutoScheduleOwner();

	private readonly WeekZeroOpeningSummaryGenerationController _weekZeroShortSummaries;

	private readonly WeeklyFullReportCompletionOwner _weeklyFullReportCompletions;

	private string _voiceMappingJsonStorage { get => _voicePersonaPersistence.VoiceJsonStorage; set => _voicePersonaPersistence.VoiceJsonStorage = value; }

	private string _voiceMappingExportFolderStorage { get => _voicePersonaPersistence.VoiceFolderStorage; set => _voicePersonaPersistence.VoiceFolderStorage = value; }

	private string _unnamedPersonaJsonStorage { get => _voicePersonaPersistence.UnnamedJsonStorage; set => _voicePersonaPersistence.UnnamedJsonStorage = value; }

	private Dictionary<string, TownStatSnapshot> _townStatSnapshots => _weeklyTownStatCapture._townStatSnapshots;

	private Dictionary<string, TownStatSnapshot> _townStatWeekBaselineSnapshots => _weeklyTownStatCapture._townStatWeekBaselineSnapshots;

	private Dictionary<string, int> _townStatWeekBaselineWeekIndexes => _weeklyTownStatCapture._townStatWeekBaselineWeekIndexes;

	private HashSet<string> _recentlyDefeatedByPlayer { get => _campaignBattleRecordCapture.RecentlyDefeatedByPlayer; set => _campaignBattleRecordCapture.RecentlyDefeatedByPlayer = value; }

	private HashSet<string> _playerDefeatedHeroBattleFactKeys => _campaignBattleRecordCapture.PlayerDefeatedHeroBattleFactKeys;

	private HashSet<string> _recentlyReleasedPrisoners { get => _campaignCharacterRecordCapture.RecentlyReleasedPrisoners; set => _campaignCharacterRecordCapture.RecentlyReleasedPrisoners = value; }

	private static int _cachedPlayerClanTier;

	private static long _cachedPlayerClanTierUtcTicks;

	private List<Hero> _devEditableHeroes { get => EditorSession.EditableHeroes; set => EditorSession.EditableHeroes = value; }

	private Hero _devEditingHero { get => EditorSession.SelectedHero; set => EditorSession.SelectedHero = value; }

	private const int PatienceMaxCap = 80;

	private const int PatienceMinCap = 10;

	private const int PatienceDefaultMaxForUnnamed = 30;

	private const float PatienceRecoveryPerDay = 4f;

	private const int PatienceNoInterestPenaltyThreshold = 3;

	private const int RelationGainOnJoy = 1;

	private const int RelationPenaltyOnBored = -1;

	private const int RelationPenaltyOnAnnoyed = -2;

	private const int RoyalDomainConversationJoyLoyaltyDelta = 2;

	private const int RoyalDomainConversationDelightedLoyaltyDelta = 4;

	private static readonly string[] PatienceLevelTexts = new string[10] { "枯竭", "烦躁", "不耐", "冷淡", "一般", "尚可", "愿听", "投入", "热络", "兴致高" };



	private static readonly string[] RelationAiBehaviorTexts = new string[10] { "把玩家视为重大威胁，语气强硬且排斥，不愿合作。", "明显敌意与戒备，倾向拒绝请求，回应尖锐。", "主观反感较强，容易挑刺，合作意愿很低。", "保持距离与怀疑，只做最基本交流。", "态度偏冷，交流克制，基本不主动示好。", "无明显好恶，按利益与场面决定态度。", "对玩家有一定熟悉感，语气较缓，可有限合作。", "整体友好，愿意倾听并给出建设性回应。", "明显信任玩家，交流积极，合作意愿高。", "高度信任与支持，语气亲近，优先站在玩家一边。" };

	private static readonly Regex MoodTagRegex = new Regex("\\[ACTION:MOOD:([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex TransferTroopTagRegex = PartyTransferTagCodec.TroopRegex;

	private static readonly Regex TransferPrisonerTagRegex = PartyTransferTagCodec.PrisonerRegex;

	private readonly PatienceOwner<PatienceState> _patienceOwner = new PatienceOwner<PatienceState>();

	private readonly CampaignPatiencePersistenceAdapter _patiencePersistence;
	private Dictionary<string, string> _patienceStorage { get => _patiencePersistence.Storage; set => _patiencePersistence.Storage = value; }

	private readonly object _patienceLock = new object();
















private readonly CampaignBattleRecordCaptureAdapter _campaignBattleRecordCapture;

private readonly Func<MemoryBusinessStateOwner> _loadMemoryQueueState;

public MyBehavior()
	{
        _weeklyTownStatCapture = new WeeklyTownStatCaptureAdapter(() => _campaignCharacterRecordCapture);
        _settlementOwnerChangeRebelCleanup = (kingdom, reason) => _kingdomRebellionGameAdapter.TryDiscontinueLandlessModRebelKingdom(kingdom, reason);
        _weeklyNoticeGameAdapter = new WeeklyNoticeGameAdapter(_weeklyNoticeOwner, eventId => WeeklyRuntime.FindWeeklyReportRecordById(eventId), () => WeeklyNoticePort, () => ReferenceEquals(Instance, this), (entry, id) => WeeklyEditor.TryShowWorldBulletinPanel(entry, id), _weeklyNoticeDefaultTitle, _weeklyNoticeSubtitle);
        _memoryIdentityMerge = (source, target) => MemoryIdentityState.MergeMemoryEntityDataById(source, target);
        _loadMemoryQueueState = () => MemoryQueueState;
        _campaignBattleRecordCapture = new CampaignBattleRecordCaptureAdapter(() => _campaignCharacterRecordCapture, AppendExternalDialogueHistory, new RemovedPartyMemoryPorts { State = _memoryBusinessState, IdentityState = () => MemoryIdentityState, Recovery = () => MemoryRecoveryState, Identity = () => _memoryEntityIdentity });
        _worldBulletinEventCapture = new WorldBulletinEventCaptureAdapter(_campaignMaterialRecords, () => _campaignCharacterRecordCapture, _weeklyReportMaterialRevisions.MarkAll, () => _kingdomStabilityGameAdapter, () => WorldBulletinState);
        _captureMemorySealingPort = () => MemorySealingCapabilities;
        _dailyMaintenanceController = new CampaignDailyMaintenanceController(_memoryBusinessState, () => _pendingAutoWeeklyReportBuild != null,
            new CampaignDailyMaintenanceCapabilities {
                Schedule = _weeklyAutoSchedule, CurrentDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe,
                LastAutoWeek = () => _lastAutoGeneratedWeeklyReportWeek, WeeklyRunning = () => _weeklyReportGenerationInProgress,
                SummaryRunning = () => _memorySummaryRunOwner.IsRunning, RebellionFlow = () => AutomaticKingdomRebellions.FlowActive,
                Seal = (start, budget) => _memoryBusinessState.TrySealPastDailyMemoryDrafts(_captureMemorySealingPort, start, budget),
                SealWithPendingProbe = (start, budget) => _memoryBusinessState.TrySealPastDailyMemoryDrafts(_captureMemorySealingPort, start, budget, requirePendingProbe: true),
                QueueDirty = () => MemoryQueueState.QueueDirtyMemoryOverviewCandidatesForDeferredScan(),
                QueueAll = () => MemoryQueueState.QueueAllMemoryOverviewCandidatesForDeferredScan(),
                Scan = (start, budget) => MemoryQueueState.ProcessMemoryOverviewCandidateScanBudget(start, budget, WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded),
                StartSummary = () => MemorySummaryApplicationAdapter.TryStartMemorySummaryQueue(MemoryQueueState, _memorySummaryRunOwner,
                    MemoryEntityIdentityBannerlordAdapter.IsDialogueOrLetterChainBusyForMemorySummary, _startMemorySummaryRun,
                    MemoryOverviewCandidateScanThrottleSeconds),
                EnsureOpening = () => _weekZeroShortSummaries.EnsureWeekZeroOpeningSummaryEvents(),
                OpeningSlice = () => _weekZeroShortSummaries.ProcessWeekZeroOpeningSummaryEventsSlice(),
                ApplyRelations = () => _kingdomStabilityGameAdapter.ApplyKingdomStabilityRelationAdjustments(),
                RelationSlice = () => _kingdomStabilityGameAdapter.ProcessKingdomStabilityRelationAdjustmentsSlice(),
                DiscontinueRebels = reason => _kingdomRebellionGameAdapter.TryDiscontinueLandlessModRebelKingdoms(reason), RecordMissedEvents = _worldBulletinEventCapture.TryRecordMissedStrategicWorldEvents,
                WeeklyRebellions = week => _kingdomRebellionRuntime.TryProcessWeeklyKingdomRebellions(week), MissedEventsSlice = _worldBulletinEventCapture.ProcessMissedStrategicWorldEventsSlice,
                RebellionSlice = week => _kingdomRebellionRuntime.ProcessWeeklyKingdomRebellionsSlice(week), InitializePendingWeekly = job => WeeklyRuntime.TryInitializePendingAutoWeeklyReportBuild(job),
                ProcessPendingWeekly = (start,budget) => WeeklyRuntime.ProcessPendingAutoWeeklyReportBuildBudget(start,budget), StartAutoWeekly = (week,day) => WeeklyRuntime.StartAutoWeeklyReportsForWeek(week,day)
            });
        _kingdomStabilityGameAdapter = new KingdomStabilityGameAdapter(() => KingdomStability, () => _kingdomPersistence.RelationStorage, () => KingdomMaintenance, new KingdomCampaignRecordPorts { Revisions = _weeklyReportMaterialRevisions, Record = () => _campaignCharacterRecordCapture, Rebellion = () => _kingdomRebellionGameAdapter });
        _startMemorySummaryRun = force => CreateMemorySummaryQueueRunRuntime().RunAsync(force);
        _memoryHistoryContext = new MemoryHistoryContextReadCapabilities
        {
            IsEligible = MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory,
            Overview = id => MemorySummaryApplicationAdapter.BuildMemoryOverviewContextById(MemoryQueueState, id, MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory, LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings),
            Compressed = (id, player, npc, snapshot) => MemoryRecallInputCaptureAdapter.BuildCompressedMemoryContextById(MemoryRecallCapturePorts, id, player, npc, snapshot),
            Trace = MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace
        };
        _campaignSaveExit = new CampaignSaveExitController(() => QueueWeeklyReportFailurePopup(_weeklyReportRetryContext, showImmediate: true));
        _memoryHistoryCommit = new MemoryHistoryCommitBannerlordAdapter(_memoryBusinessState, () => MemoryRecoveryState);
        _campaignCharacterRecordCapture = new CampaignCharacterRecordCaptureAdapter(_memoryBusinessState, _npcActionRecords, _campaignMaterialRecords, _weeklyReportMaterialRevisions.MarkAll, _weeklyReportMaterialRevisions.MarkDay, entry => NpcActionEditorProjection.BuildDevNpcActionPreviewText(NpcActionEditorDisplay, entry), () => _weeklyEventRecords, ApplyWeeklyPromptMaterialAggregation, () => MemoryQueueState, new CharacterPersonaReadinessPorts { Needs = hero => NpcPersonaGenerationApplication.NeedsNpcPersonaGeneration(hero), InFlight = hero => NpcPersonaGenerationApplication.IsNpcPersonaGenerationInFlight(hero), Ensure = hero => NpcPersonaGenerationApplication.EnsureNpcPersonaGeneratedAsync(hero) }, () => _worldBulletinEventCapture);
        _kingdomPersistence = new CampaignKingdomPersistenceAdapter(_kingdomStability, _rebelKingdomIdentity);
        _kingdomRebellionGameAdapter = new KingdomRebellionGameAdapter(_rebelKingdomIdentity, () => _kingdomStability, _kingdomPersistence);
        _kingdomRebellionRuntime = new KingdomRebellionRuntimeController(_weeklyEventRecords, GetWeeklyReportRequestIntervalMs, () => _kingdomRebellionGameAdapter, () => _kingdomStabilityGameAdapter, new KingdomRebellionPumpCapabilities { Maintenance = () => KingdomMaintenance, Automatic = () => AutomaticKingdomRebellions, EditableKingdoms = EventEditorProjection.GetDevEditableKingdoms, RecentFacts = _campaignMaterialRecords.GetRecentKingdomEventFacts, DeferredWeekly = TryStartDeferredAutoWeeklyReports, CompletionPopup = ShowAutomaticKingdomRebellionCompletionPopup, NamingFailurePopup = ShowAutomaticKingdomRebellionNamingFailurePopup, ClearBlockedNamingUi = () => { _blockedAutomaticKingdomRebellionContext = null; _kingdomRebellionReopenAfterApiConfig = false; } });
        _civilWarPersistence = new CampaignCivilWarPersistenceAdapter(TeamModuleServices.CivilWar.Save, TeamModuleServices.CivilWar.Load);
        _weekZeroShortSummaries = new WeekZeroOpeningSummaryGenerationController(_weeklyEventRecords,
            GetWeeklyReportRequestIntervalMs, IsWorldBulletinEnabled,
            () => ReferenceEquals(Instance, this) && Volatile.Read(ref _campaignRuntimeRetired) == 0,
            AnimusForge.Refactor.Adapters.WeeklyPromptCaptureAdapter.BuildWeekZeroShortSummarySystemPrompt,
            AnimusForge.Refactor.Adapters.WeeklyPromptCaptureAdapter.BuildWeekZeroShortSummaryUserPrompt,
            CallWeeklyReportApiDetailed, NotifyWorldMessageWeeklyTimelineChanged, eventId => WeeklyRuntime.FindWeeklyReportRecordById(eventId));
		_materialPersistence = new CampaignMaterialPersistenceAdapter(_campaignMaterialRecords);
		_politicalDecisionCapture = new PoliticalDecisionRecordCaptureAdapter(RecordNpcMajorAction, RecordNpcRecentAction, RecordEventSourceMaterial, RecordExternalPlayerAction);
		_memoryRecoveryPersistence = new CampaignMemoryRecoveryPersistenceAdapter(MemoryRecoveryState);
        _weeklyActionOutcomePublication = new WeeklyActionOutcomePublicationOwner(new WeeklyOutcomeApplicationCapabilities {
            Memory = _memoryBusinessState,
            CurrentDay = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe,
            CurrentDate = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDateTextSafe,
            IsEligible = MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory,
            CaptureOutcome = (WeeklyMemoryMaterialOutcomeCandidate candidate, bool isNonHero, string npcName, out WeeklyActionOutcomeMaterialContext context, out WeeklyMaterialValuePort values, out string error) => WeeklyMaterialValueBannerlordAdapter.TryCaptureOutcomeContext(candidate, isNonHero, npcName,
                MemoryRecordRules.NormalizeMemoryHeroId, MemoryBusinessStateOwner.IsNonHeroMemoryId,
                MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory,
                MemoryEntityIdentityBannerlordAdapter.FindHeroById, MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory,
                MemoryEntityIdentityBannerlordAdapter.ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial,
                MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe, MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDateTextSafe,
                out context, out values, out error)
        });
		_weeklyActionOutcomePersistence = new CampaignWeeklyActionOutcomePersistenceAdapter(EnsureWeeklyActionOutcomePublication());
        ExecutionWitnesses = new ExecutionWitnessObservationController(_executionTranscripts,GetCurrentGameDayIndexSafe,BuildNonHeroMemoryIdForExternal,id => GetDialogueHistoryEntriesByIdForExternal(id,1).Count>0,CommitExternalDialogueHistoryRecoverable,k => GetKingdomId(k),(kind,label,text,key,kingdom,place,world,realm,actor,actorRealm)=>RecordEventSourceMaterial(kind,label,text,key,kingdom,place,world,realm,actor,actorRealm),(kind,key,score,sentence,player,group,detail,participants,kingdoms)=>CaptureWorldBulletinEvent(kind,key,score,sentence,player,group,detail,participants,kingdoms));
		_personaPersistence = new CampaignPersonaPersistenceAdapter(_personaProfiles);
		_shownPersistence = new CampaignShownRecordPersistenceAdapter(_shownResourceRecords);
		_weeklyRecordPersistence = new CampaignWeeklyRecordPersistenceAdapter(_weeklyEventRecords);
		_patiencePersistence = new CampaignPatiencePersistenceAdapter(_patienceOwner, _patienceLock);
		_weeklyFullReportCompletions = new WeeklyFullReportCompletionOwner(
			() => ReferenceEquals(Instance, this), SaveRuntimeGuard.IsStale);
		Instance = this;
	}

	private static int ClampRecentDialogueTurns(int turns) => MemoryBusinessStateOwner.ClampRecentDialogueTurns(turns);

	private static int CountPromptChars(string text)
	{
		return (!string.IsNullOrEmpty(text)) ? text.Length : 0;
	}

	private static int GetRecentDialogueTurnsFromSettings() => MemoryBusinessStateOwner.GetRecentDialogueTurnsFromSettings();


	private static int GetHistoryReturnCapFromSettings() => DeveloperRootEditorController.ReadHistoryReturnCap(() => DuelSettings.GetSettings()?.HistoryRecallTopN, HistoryArchiveRecallOwner.ClampHistoryReturnCap);

	private static int GetMemoryCompressionDenominatorFromSettings()
	{
        return LlmRequestConfigurationCaptureAdapter.GetMemoryCompressionDenominatorFromSettings();
    }

	private static int GetMemorySummaryRequestsPerMinuteFromSettings() => MemoryBusinessStateOwner.GetMemorySummaryRequestsPerMinuteFromSettings();

	private static int GetMemoryOverviewStartBlockCountFromSettings()
	{
        return LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings();
    }

	private static int GetMemoryOverviewTargetCharsFromSettings()
	{
        return LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewTargetCharsFromSettings();
    }

	private static int GetMemoryCandidateLimitFromSettings() => MemoryBusinessStateOwner.GetMemoryCandidateLimitFromSettings();

	private static int GetMemoryFinalInjectCountFromSettings() => MemoryBusinessStateOwner.GetMemoryFinalInjectCountFromSettings();

	private static int GetMemoryPreprocessModeFromSettings() => MemoryBusinessStateOwner.GetMemoryPreprocessModeFromSettings();

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.BattleStarted.AddNonSerializedListener(this, OnBattleStartedForEncounterDiag);
		CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, OnSaveOver);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnMemoryConversationEnded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEndForDefeatMemory);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompletedForActionHistory);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAddedForActionHistory);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemovedForActionHistory);
		CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChangedForActionHistory);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyedForNonHeroMemoryCleanup);
		CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemovedForNonHeroMemoryCleanup);
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
		CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, OnSiegeAftermathApplied);
		CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
		RegisterWorldBulletinEvents();
		MBInformationManager.OnRemoveMapNotice -= OnMapNoticeRemoved;
		MBInformationManager.OnRemoveMapNotice += OnMapNoticeRemoved;
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ResetRuntimeForLoadedSave("new_game_created");
        // New campaigns do not necessarily raise the loaded-save completion event.
        QueueMissingOnnxGateCheck(TimeSpan.Zero);
		// The civil-war owner is process-wide; only save loading replaces it, so a new campaign must clear it.
		_civilWarPersistence.ResetForNewCampaign();
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ResetRuntimeForLoadedSave("game_loaded");
	}

	private void ResetRuntimeForLoadedSave(string reason)
	{
		try
		{
			SaveRuntimeGuard.AdvanceGeneration(reason);
			ResetExecutionMemoryRuntime();
			PublicExecutionOrderRuntime.Reset();
			if (reason == "new_game_created") _executionTranscripts.Load(null);
			ResetLocalTransientRuntimeForLoadedSave(reason);
			ShoutBehavior.ResetTransientRuntimeForLoadedSaveExternal(reason);
			CourierDeliveryBehavior.ResetTransientRuntimeForLoadedSaveExternal(reason);
			AIConfigHandler.ClearLatestAuxiliaryMentionedEntitiesForExternal();
		}
		catch (Exception ex)
		{
			Logger.Log("SaveRuntimeGuard", "[WARN] reset runtime failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private void ResetLocalTransientRuntimeForLoadedSave(string reason)
	{
		try
		{
			CancelWeeklyFullReportCompletions();
            _weekZeroShortSummaries.ResetTransientRuntime();
			ResetMemorySummaryMainThreadActions();
			ResetTailPersistenceTransientState(reason);
			_builtInRuleStickyCarry.Clear();
			_playerDefeatedHeroBattleFactKeys.Clear();
			_memorySummaryRunOwner.Reset();
			ResetMemoryFailureNotices();
			_lastMemoryMaintenanceObservedGameDay = -1;
			_nativeConversationMemorySessionCounter = 0;
			_activeNativeConversationMemorySessionId = -1;
			_pendingWeeklyMemoryMaterialTriggers.Clear();
			_pendingAutoWeeklyReportBuild = null;
			_weeklyReportGenerationInProgress = false;
			_weeklyAutoSchedule.Clear();
			ResetPendingWeeklyKingdomRebellionMaintenance();
			_weeklyPromptPreparationQueue.CancelAll();
			_weeklyWaveLaunchQueue.CancelAll();
			_weeklyBatchApiAttemptQueue.CancelAll();
			_weeklyReportCommitQueue.CancelAll();
			_dailyMaintenanceQueue.Clear();
			_dailyMaintenanceJobKeys.Clear();
			ResetDailyMemoryDraftSealSliceState();
			_destroyedPartyMemoryCleanupDedup.Clear();
			_dirtyMemoryOverviewIds.Clear();
			_pendingMemoryOverviewCandidateScanIds.Clear();
			_pendingMemoryOverviewCandidateScanIdSet.Clear();
			_npcPersonaGeneration.Reset();
			ResetWorldBulletinForRuntime(reason);
			Logger.Log("SaveRuntimeGuard", "local_transient_cleared reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("SaveRuntimeGuard", "[WARN] local transient clear failed: " + ex.Message);
		}
	}

	private void OnHeroComesOfAge(Hero hero) => _campaignCharacterRecordCapture.OnHeroComesOfAge(hero);

	private bool ShouldAutoGeneratePersonaForAdultHero(Hero hero) => _campaignCharacterRecordCapture.ShouldAutoGeneratePersonaForAdultHero(hero);

	private void OnMapEventEnded(MapEvent mapEvent) => _campaignBattleRecordCapture.OnMapEventEnded(mapEvent);

	private void OnPlayerBattleEndForDefeatMemory(MapEvent mapEvent) => _campaignBattleRecordCapture.OnPlayerBattleEndForDefeatMemory(mapEvent);

	private void RecordPlayerDefeatedHeroBattleFact(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero, string reason) => _campaignBattleRecordCapture.RecordPlayerDefeatedHeroBattleFact(mapEvent, defeatedSide, defeatedHero, reason);

	public static void RecordNpcActionForExternal(Hero actorHero, string text, string stableKey, string actionKind, bool isMajor, bool isRecent, Hero targetHero = null, Settlement settlement = null, string locationText = null, bool allowNonLordHero = false, bool? won = null)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => { owner.RecordExternalNpcAction(actorHero, text, stableKey, actionKind, isMajor, isRecent, targetHero, settlement, locationText, allowNonLordHero, won); }, nameof(RecordNpcActionForExternal))) return;

		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordExternalNpcAction(actorHero, text, stableKey, actionKind, isMajor, isRecent, targetHero, settlement, locationText, allowNonLordHero, won);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordNpcActionForExternal: " + ex.Message);
		}
	}

	public static void RecordPlayerActionForExternal(string text, string stableKey, string actionKind, bool isMajor, Hero targetHero = null, Settlement settlement = null, string locationText = null, bool? won = null)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordExternalPlayerAction(text, stableKey, actionKind, isMajor, targetHero, settlement, locationText, won);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "[ERROR] RecordPlayerActionForExternal bridge: " + ex.Message);
		}
	}

	public static void RecordPlayerHighValueRpCraftForExternal(
		string batchId,
		string requestedName,
		string finalDisplayName,
		int investedDenars,
		int craftedItemValue,
		string crafterHeroId,
		string crafterDisplayName,
		string outcomeLabel)
	{
		if (investedDenars <= 10000)
		{
			return;
		}
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())
				?.RecordExternalPlayerHighValueRpCraft(
					batchId,
					requestedName,
					finalDisplayName,
					investedDenars,
					craftedItemValue,
					crafterHeroId,
					crafterDisplayName,
					outcomeLabel);
		}
		catch (Exception ex)
		{
			Logger.Log(
				"PlayerNotoriety",
				"[PlayerRpCraft][WARN] record high-value craft failed: "
					+ ex.Message);
		}
	}

	public static void RecordVoteDealFulfilledForExternal(Hero npc, KingdomDecision decision, DecisionOutcome chosenOutcome, string dealId, string targetDecisionTitle, string targetOptionTitle)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordVoteDealFulfilled(npc, decision, chosenOutcome, dealId, targetDecisionTitle, targetOptionTitle);
		}
		catch (Exception ex)
		{
			Logger.Log("VoteDeal", "[ERROR] record vote deal action failed: " + ex.Message);
		}
	}

	private void OnQuestCompletedForActionHistory(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
        ExternalActionObservations.OnQuestCompletedForActionHistory(quest,detail);
    }

	private void OnNewCompanionAddedForActionHistory(Hero newCompanion)
	{
        ExternalActionObservations.OnNewCompanionAddedForActionHistory(newCompanion);
    }

	private void OnCompanionRemovedForActionHistory(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
        ExternalActionObservations.OnCompanionRemovedForActionHistory(companion,detail);
    }

	private void OnGovernorChangedForActionHistory(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
        ExternalActionObservations.OnGovernorChangedForActionHistory(fortification,oldGovernor,newGovernor);
    }

	private void RecordNpcDefeatedByPlayerRecentAction(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero) => _campaignBattleRecordCapture.RecordNpcDefeatedByPlayerRecentAction(mapEvent, defeatedSide, defeatedHero);

	private void RecordPlayerBattleDefeatRecentAction(MapEvent mapEvent) => _campaignBattleRecordCapture.RecordPlayerBattleDefeatRecentAction(mapEvent);

	private static string BuildMapEventParticipantStableKey(MapEvent mapEvent, MapEventSide side, Hero hero, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildMapEventParticipantStableKey(mapEvent, side, hero, locationLabel);

	private static string BuildPlayerDefeatedHeroBattleFactKey(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildPlayerDefeatedHeroBattleFactKey(mapEvent, defeatedSide, defeatedHero, locationLabel);

	private bool HasRecordedPlayerDefeatedHeroBattleFact(MapEvent mapEvent, MapEventSide defeatedSide, Hero defeatedHero) => _campaignBattleRecordCapture.HasRecordedPlayerDefeatedHeroBattleFact(mapEvent, defeatedSide, defeatedHero);

	private static MapEventSide GetMapEventSideByBattleSide(MapEvent mapEvent, BattleSideEnum side) => CampaignBattleRecordCaptureAdapter.GetMapEventSideByBattleSide(mapEvent, side);

	private static string BuildPlayerDefeatedHeroDialogueFact(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildPlayerDefeatedHeroDialogueFact(mapEvent, defeatedSide, locationLabel);

	private static string BuildNpcDefeatedByPlayerRecentActionText(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildNpcDefeatedByPlayerRecentActionText(mapEvent, defeatedSide, locationLabel);

	private static string BuildPlayerBattleDefeatRecentActionText(MapEvent mapEvent, MapEventSide playerSide, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildPlayerBattleDefeatRecentActionText(mapEvent, playerSide, locationLabel);

	private static void AppendBattleOpponentDetails(StringBuilder stringBuilder, MapEventSide opponentSide, string label) => CampaignBattleRecordCaptureAdapter.AppendBattleOpponentDetails(stringBuilder, opponentSide, label);

	private static void AppendBattleCasualtyDetails(StringBuilder stringBuilder, MapEventSide defeatedSide, MapEventSide winningSide) => CampaignBattleRecordCaptureAdapter.AppendBattleCasualtyDetails(stringBuilder, defeatedSide, winningSide);

	private void RecordPlayerHideoutClearRecentAction(MapEvent mapEvent) => _campaignBattleRecordCapture.RecordPlayerHideoutClearRecentAction(mapEvent);

	private void RecordPlayerRoutineBanditDefeatRecentAction(MapEvent mapEvent) => _campaignBattleRecordCapture.RecordPlayerRoutineBanditDefeatRecentAction(mapEvent);

	private void RecordExternalNpcAction(Hero actorHero, string text, string stableKey, string actionKind, bool isMajor, bool isRecent, Hero targetHero, Settlement settlement, string locationText, bool allowNonLordHero, bool? won) => _campaignCharacterRecordCapture.RecordExternalNpcAction(actorHero, text, stableKey, actionKind, isMajor, isRecent, targetHero, settlement, locationText, allowNonLordHero, won);

	private void RecordExternalPlayerAction(string text, string stableKey, string actionKind, bool isMajor, Hero targetHero, Settlement settlement, string locationText, bool? won) => _campaignCharacterRecordCapture.RecordExternalPlayerAction(text, stableKey, actionKind, isMajor, targetHero, settlement, locationText, won);

	private void RecordExternalPlayerHighValueRpCraft(
		string batchId,
		string requestedName,
		string finalDisplayName,
		int investedDenars,
		int craftedItemValue,
		string crafterHeroId,
		string crafterDisplayName,
		string outcomeLabel)
	{
        ExternalActionObservations.RecordExternalPlayerHighValueRpCraft(batchId,requestedName,finalDisplayName,investedDenars,craftedItemValue,crafterHeroId,crafterDisplayName,outcomeLabel);
    }

	private void RecordVoteDealFulfilled(Hero npc, KingdomDecision decision, DecisionOutcome chosenOutcome, string dealId, string targetDecisionTitle, string targetOptionTitle)
	{
        ExternalActionObservations.RecordVoteDealFulfilled(npc,decision,chosenOutcome,dealId,targetDecisionTitle,targetOptionTitle);
    }

	private static string BuildExternalActionStableKey(string actionKind, Hero actorHero, Hero targetHero, string text) => CampaignCharacterRecordCaptureAdapter.BuildExternalActionStableKey(actionKind, actorHero, targetHero, text);

		private static Settlement ResolveQuestActionSettlement(Hero questGiver) => CampaignCharacterRecordCaptureAdapter.ResolveQuestActionSettlement(questGiver);

		private static Settlement ResolveCurrentActionSettlement(Hero hero) => CampaignCharacterRecordCaptureAdapter.ResolveCurrentActionSettlement(hero);

		private static bool IsPlayerRelatedGovernorChange(Settlement settlement, Hero oldGovernor, Hero newGovernor) => CampaignCharacterRecordCaptureAdapter.IsPlayerRelatedGovernorChange(settlement, oldGovernor, newGovernor);

		private static string CleanExternalActionTitle(string value) => CampaignCharacterRecordCaptureAdapter.CleanExternalActionTitle(value);

		private static string GetQuestCompletionActionKind(QuestBase.QuestCompleteDetails detail) => CampaignCharacterRecordCaptureAdapter.GetQuestCompletionActionKind(detail);

		private static string GetQuestCompletionDetailLabel(QuestBase.QuestCompleteDetails detail) => CampaignCharacterRecordCaptureAdapter.GetQuestCompletionDetailLabel(detail);

		private static string GetCompanionRemovedDetailLabel(RemoveCompanionAction.RemoveCompanionDetail detail) => CampaignCharacterRecordCaptureAdapter.GetCompanionRemovedDetailLabel(detail);

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner) => _campaignBattleRecordCapture.OnHeroPrisonerTaken(capturer, prisoner);

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification) => _campaignBattleRecordCapture.OnHeroPrisonerReleased(prisoner, party, capturerFaction, detail, showNotification);

	public static void RecordPlayerPrisonBreakRescueForExternal(Hero rescuedHero)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordPlayerPrisonBreakRescue(rescuedHero);
		}
		catch (Exception ex)
		{
			Logger.Log("PrisonBreakRescue", "RecordPlayerPrisonBreakRescueForExternal failed: " + ex.Message);
		}
	}

	private void RecordPlayerPrisonBreakRescue(Hero rescuedHero)
	{
        ExternalActionObservations.RecordPlayerPrisonBreakRescue(rescuedHero);
    }

	private static Settlement ResolvePrisonBreakRescueSettlement(Hero rescuedHero, Hero player)
	{
        return ExternalActionObservationBannerlordAdapter.ResolvePrisonBreakRescueSettlement(rescuedHero,player);
    }

	private static string BuildPrisonBreakRescueStableKey(Hero player, Hero rescuedHero, Settlement settlement, int day, int hour)
	{
        return ExternalActionObservationBannerlordAdapter.BuildPrisonBreakRescueStableKey(player,rescuedHero,settlement,day,hour);
    }

	private void RemoveGenericEscapeRecentActionForPlayerRescue(Hero rescuedHero, int day) => _campaignCharacterRecordCapture.RemoveGenericEscapeRecentActionForPlayerRescue(rescuedHero, day);

	private void OnArmyCreated(Army army) => _campaignBattleRecordCapture.OnArmyCreated(army);

	private void OnArmyGathered(Army army, IMapPoint gatheringPoint) => _campaignBattleRecordCapture.OnArmyGathered(army, gatheringPoint);

	private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isNoNotification) => _campaignBattleRecordCapture.OnArmyDispersed(army, reason, isNoNotification);

	private void OnPartyJoinedArmy(MobileParty party) => _campaignBattleRecordCapture.OnPartyJoinedArmy(party);

	private void OnPartyLeftArmy(MobileParty party, Army army) => _campaignBattleRecordCapture.OnPartyLeftArmy(party, army);

	private void OnSiegeEventStarted(SiegeEvent siegeEvent) => _campaignBattleRecordCapture.OnSiegeEventStarted(siegeEvent);

	private void OnSiegeEventEnded(SiegeEvent siegeEvent) => _campaignBattleRecordCapture.OnSiegeEventEnded(siegeEvent);

	private void OnMobilePartyJoinedSiege(MobileParty party) => _campaignBattleRecordCapture.OnMobilePartyJoinedSiege(party);

	private void OnMobilePartyLeftSiege(MobileParty party) => _campaignBattleRecordCapture.OnMobilePartyLeftSiege(party);

	private void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, MapEvent.BattleTypes battleType) => _campaignBattleRecordCapture.OnSiegeCompleted(settlement, party, siegeSuccess, battleType);

	private void OnDailyTickParty(MobileParty party) => _campaignBattleRecordCapture.OnDailyTickParty(party);

	private void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification) => _campaignCharacterRecordCapture.OnBeforeHeroesMarried(hero1, hero2, showNotification);

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification) => _kingdomStabilityGameAdapter.OnClanChangedKingdom(clan, oldKingdom, newKingdom, detail, showNotification);

	private void RecordPlayerClanChangedKingdomActionIfRelevant(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, string narrative, string stableKey) => _kingdomStabilityGameAdapter.RecordPlayerClanChangedKingdomActionIfRelevant(clan, oldKingdom, newKingdom, detail, narrative, stableKey);

	private void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom) => _kingdomStabilityGameAdapter.OnClanDefected(clan, oldKingdom, newKingdom);

		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved) => _politicalDecisionCapture.OnKingdomDecisionConcluded(decision, chosenOutcome, isPlayerInvolved);

	private void OnRulingClanChanged(Kingdom kingdom, Clan eventRulingClan) => _kingdomStabilityGameAdapter.OnRulingClanChanged(kingdom, eventRulingClan);

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail) => _campaignCharacterRecordCapture.OnSettlementOwnerChanged(settlement, openToClaim, newOwner, oldOwner, capturerHero, detail, _settlementOwnerChangeRebelCleanup);

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification) => _campaignCharacterRecordCapture.OnHeroKilled(victim, killer, detail, showNotification);

	private void RecordExecutedVictimAction(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, string stablePrefix, VengeanceExecutionFacts executionFacts = null) => _campaignCharacterRecordCapture.RecordExecutedVictimAction(victim, killer, detail, stablePrefix, executionFacts);

	private void RecordPlayerExecutionWeeklyMaterial(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, string stablePrefix, VengeanceExecutionFacts executionFacts = null) => _campaignCharacterRecordCapture.RecordPlayerExecutionWeeklyMaterial(victim, killer, detail, stablePrefix, executionFacts);

	private void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount) => _campaignCharacterRecordCapture.OnGivenBirth(mother, aliveChildren, stillbornCount);

	private void OnClanLeaderChanged(Hero oldLeader, Hero newLeader) => _kingdomStabilityGameAdapter.OnClanLeaderChanged(oldLeader, newLeader);

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		try
		{
			RecordWarOrPeaceMaterial("war_declared", "宣战", faction1, faction2, detail.ToString());
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnWarDeclared: " + ex.Message);
		}
	}

	private void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		try
		{
			RecordWarOrPeaceMaterial("peace_made", "停战议和", side1Faction, side2Faction, detail.ToString());
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnMakePeace: " + ex.Message);
		}
	}

	private void RecordWarOrPeaceMaterial(string materialKind, string actionLabel, IFaction faction1, IFaction faction2, string detailText) => _politicalDecisionCapture.RecordWarOrPeaceMaterial(materialKind, actionLabel, faction1, faction2, detailText);

	private void TryRecordMissedStrategicWorldEvents() => _worldBulletinEventCapture.TryRecordMissedStrategicWorldEvents();



	private void OnDailyTickTown(Town town)
	{
		try
		{
			TrackTownWeeklyMaterialChanges(town);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnDailyTickTown: " + ex.Message);
		}
	}

	private static bool IsDialogueOrLetterChainBusyForMemorySummary() => MemoryEntityIdentityBannerlordAdapter.IsDialogueOrLetterChainBusyForMemorySummary();

	private static bool IsNonSceneNativeConversationActiveForMemory() => MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory();

	private int GetOrStartActiveNativeConversationMemorySessionId() => _memoryBusinessState.GetOrStartActiveNativeConversationMemorySessionId(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory);

	private int GetCurrentNativeConversationMemorySessionIdForSuppression() => _memoryBusinessState.GetCurrentNativeConversationMemorySessionIdForSuppression(MemoryEntityIdentityBannerlordAdapter.IsNonSceneNativeConversationActiveForMemory);

	private string BuildCurrentMemorySessionKey(int sceneSessionId, int dialogueSessionId) => _memoryBusinessState.BuildCurrentMemorySessionKey(sceneSessionId, dialogueSessionId);

	private void OnMemoryConversationEnded(IEnumerable<CharacterObject> characters)
	{
		ShoutBehavior.InvalidateNativeConversationAdmissionOnConversationEnd();
		// Authoritative end event: also retires text when a mechanism force-closes the UI.
		ConversationHelper.Clear();
		string memorySessionKey = _activeNativeConversationMemorySessionId >= 0
			? BuildCurrentMemorySessionKey(-1, _activeNativeConversationMemorySessionId)
			: string.Empty;
		PlayerNotorietyBehavior.FinalizeConversationForExternal(
			characters,
			memorySessionKey);
		_activeNativeConversationMemorySessionId = -1;
	}

	private void TryEnqueueMajorActionSummaryForDraft(DailyMemoryDraft draft, HashSet<string> queuedMajorHeroIds, bool ownerAlreadyEligible = false) => MemoryQueueState.TryEnqueueMajorActionSummaryForDraft(draft, queuedMajorHeroIds, ownerAlreadyEligible);

	private void TryEnqueueMemoryOverviewForHero(Hero hero, List<CompressedMemoryBlock> blocks = null) => MemoryEntityIdentityBannerlordAdapter.TryEnqueueMemoryOverviewForHero(MemoryQueueState, hero, blocks);

	private void TryEnqueueMemoryOverviewForMemoryId(string memoryId, string memoryName, List<CompressedMemoryBlock> blocks = null) => MemoryQueueState.TryEnqueueMemoryOverviewForMemoryId(memoryId, memoryName, blocks);

	private void TryEnqueueMemoryOverviewForAllCandidates() => MemoryQueueState.TryEnqueueMemoryOverviewForAllCandidates();

	private void MarkMemoryOverviewDirty(string memoryId) => MemoryQueueState.MarkMemoryOverviewDirty(memoryId);

	private void EnqueueMemoryOverviewCandidateScanId(string memoryId) => MemoryQueueState.EnqueueMemoryOverviewCandidateScanId(memoryId);

	private void QueueDirtyMemoryOverviewCandidatesForDeferredScan() => MemoryQueueState.QueueDirtyMemoryOverviewCandidatesForDeferredScan();

	private void QueueAllMemoryOverviewCandidatesForDeferredScan() => MemoryQueueState.QueueAllMemoryOverviewCandidatesForDeferredScan();

	private int ProcessMemoryOverviewCandidateScanBudget(long startTimestamp, double budgetMs) => MemoryQueueState.ProcessMemoryOverviewCandidateScanBudget(startTimestamp, budgetMs, WeeklyReportRuntimeOwner.IsDailyMaintenanceBudgetExceeded);

	private bool TrySealPastDailyMemoryDrafts(long startTimestamp = 0L, double budgetMs = double.MaxValue, bool requirePendingProbe = false) => _memoryBusinessState.TrySealPastDailyMemoryDrafts(_captureMemorySealingPort, startTimestamp, budgetMs, requirePendingProbe);

	private void ResetDailyMemoryDraftSealSliceState() { _memoryBusinessState.Sealing.Reset(); }

		private bool HasCompressedMemoryBlock(string heroId, int dayIndex) => _memoryBusinessState.HasCompressedMemoryBlock(heroId, dayIndex);

	private void TryStartMemorySummaryQueue(bool forceOverviewCandidateScan = false) => MemorySummaryApplicationAdapter.TryStartMemorySummaryQueue(MemoryQueueState, _memorySummaryRunOwner, MemoryEntityIdentityBannerlordAdapter.IsDialogueOrLetterChainBusyForMemorySummary, _startMemorySummaryRun, MemoryOverviewCandidateScanThrottleSeconds, forceOverviewCandidateScan);

	private bool ShouldScanMemoryOverviewCandidates(bool force) => MemoryQueueState.ShouldScanMemoryOverviewCandidates(force, MemoryOverviewCandidateScanThrottleSeconds);

	private Task ProcessMemorySummaryQueueAsync(bool forceOverviewCandidateScan = false) => CreateMemorySummaryQueueRunRuntime().RunAsync(forceOverviewCandidateScan);

	private Task RunDailySummaryQueueItemsAsync(List<object> queueItems, int burstSize, List<MemorySummaryExecutionResult> results, List<MajorActionSummaryExecutionResult> majorResults, List<MemoryOverviewExecutionResult> overviewResults, MemorySummaryRunOwner.Lease run = null) => CreateMemorySummaryQueueRunRuntime().RunQueueItemsAsync(queueItems, burstSize, results, majorResults, overviewResults, run);

	private Task<DailySummaryQueueResult> ExecuteDailySummaryQueueItemAsync(object item, MemorySummaryRunOwner.Lease run = null) => MemorySummaryApplication.ExecuteDailySummaryQueueItemAsync(item, run);

	private DailyMemoryDraft FindMemoryDraft(MemorySummaryJob job) => MemoryQueueState.FindMemoryDraft(job);

	private MemoryBusinessStateOwner MemoryQueueState { get { _memoryBusinessState.QueuePort ??= new MemoryQueuePort { IsEntityEligible = IsMemoryEntityEligibleForCompressedMemory, OverviewStartCount = GetMemoryOverviewStartBlockCountFromSettings, CurrentDay = () => (int)CampaignTime.Now.ToDays, Log = Logger.Log }; return _memoryBusinessState; } }
	private bool HasMemorySummaryJobStillPending(MemorySummaryJob job) => MemoryQueueState.HasMemorySummaryJobStillPending(job);

	private Task<MemorySummaryExecutionResult> ExecuteMemorySummaryJobAsync(MemorySummaryJob job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null) => MemorySummaryApplication.ExecuteMemorySummaryJobAsync(job, maxAttempts, expectedJobFingerprint, run);

	private Task<MajorActionSummaryExecutionResult> ExecuteMajorActionSummaryJobAsync(MajorActionSummaryJob job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null) => MemorySummaryApplication.ExecuteMajorActionSummaryJobAsync(job, maxAttempts, expectedJobFingerprint, run);

	private Task<MemoryOverviewExecutionResult> ExecuteMemoryOverviewJobAsync(MemoryOverviewJob job, int maxAttempts, string expectedJobFingerprint = null, MemorySummaryRunOwner.Lease run = null) => MemorySummaryApplication.ExecuteMemoryOverviewJobAsync(job, maxAttempts, expectedJobFingerprint, run);

	private static string BuildMemoryOverviewSummarySystemPrompt(int targetChars)
	{
        return MemorySummaryInputCaptureAdapter.BuildMemoryOverviewSummarySystemPrompt(targetChars);
    }

	private string BuildMemoryOverviewSummaryUserPrompt(Hero hero, MemoryOverviewState existingState, List<CompressedMemoryBlock> sourceBlocks, int targetChars)
	{
        return MemorySummaryInputCaptureAdapter.BuildMemoryOverviewSummaryUserPrompt(hero, existingState, sourceBlocks, targetChars);
    }

	private static string BuildMemoryOverviewBlockSourceText(CompressedMemoryBlock block) => MemorySummaryApplicationAdapter.BuildMemoryOverviewBlockSourceText(block);

	private static bool TryParseMemoryOverviewResponse(string content, Hero hero, MemoryOverviewJob job, MemoryOverviewState existingState, List<CompressedMemoryBlock> sourceBlocks, out MemoryOverviewState state, out string error) => MemorySummaryApplicationAdapter.TryParseMemoryOverviewResponse(content, hero, job, existingState, sourceBlocks, out state, out error);

	private bool ApplyMemoryOverviewSuccess(MemoryOverviewJob job, MemoryOverviewState state) => MemorySummaryApplication.ApplyMemoryOverviewSuccess(job, state);

	private void MarkMemoryOverviewFailure(MemoryOverviewJob job, string error)
	{
		_memoryBusinessState.FailOverview(job, error);
	}

	private static string BuildMajorActionSummarySystemPrompt(int targetChars)
	{
        return MemorySummaryInputCaptureAdapter.BuildMajorActionSummarySystemPrompt(targetChars);
    }

	private string BuildMajorActionSummaryUserPrompt(Hero hero, MajorActionSummaryState existingState, List<NpcActionEntry> sourceActions, int targetChars)
	{
        return MemorySummaryInputCaptureAdapter.BuildMajorActionSummaryUserPrompt(hero, existingState, sourceActions, targetChars);
    }

	private static bool TryParseMajorActionSummaryResponse(string content, Hero hero, MajorActionSummaryJob job, List<NpcActionEntry> allActions, out MajorActionSummaryState state, out string error) => MemorySummaryApplicationAdapter.TryParseMajorActionSummaryResponse(content, hero, job, allActions, out state, out error);

	private bool ApplyMajorActionSummarySuccess(MajorActionSummaryJob job, MajorActionSummaryState state) => MemorySummaryApplication.ApplyMajorActionSummarySuccess(job, state);

	private void MarkMajorActionSummaryFailure(MajorActionSummaryJob job, string error)
	{
		_memoryBusinessState.FailMajor(job, error);
	}

	private static string BuildMemorySummarySystemPrompt(DailyMemoryDraft draft)
	{
        return MemorySummaryInputCaptureAdapter.BuildMemorySummarySystemPrompt(draft);
    }

	private static string BuildCompressionWritingRequirementsPromptSection(string requirements)
	{
		return MemorySummaryRules.WritingRequirements(requirements);
	}

	private static string BuildMemorySummaryUserPrompt(Hero hero, DailyMemoryDraft draft)
	{
        return MemorySummaryInputCaptureAdapter.BuildMemorySummaryUserPrompt(hero, draft);
    }

	private static bool TryParseMemorySummaryResponse(string content, Hero hero, DailyMemoryDraft draft, out CompressedMemoryBlock block, out string error) => MemorySummaryApplicationAdapter.TryParseMemorySummaryResponse(content, hero, draft, BuildDailyMemoryLineForPrompt, out block, out error);

	private bool ApplyMemorySummarySuccess(MemorySummaryJob job, CompressedMemoryBlock block) => MemorySummaryApplication.ApplyMemorySummarySuccess(job, block);

	private void MarkMemorySummaryFailure(MemorySummaryJob job, string error)
	{
		_memoryBusinessState.FailDaily(job, error);
	}

	private void OnDailyTick()
	{
		try
		{
			if (!IsDeferredDailyMaintenanceEnabled())
			{
				RunDailyMaintenanceSynchronously();
				return;
			}
			EnqueueDailyMaintenanceForCurrentDay("daily_tick");
		}
		catch (Exception ex)
		{
			_weeklyReportGenerationInProgress = false;
			Logger.Log("EventWeeklyReport", "[ERROR] OnDailyTick auto-generate failed: " + ex.Message);
		}
	}

	private static bool IsDeferredDailyMaintenanceEnabled() => CampaignDailyMaintenanceController.IsDeferredDailyMaintenanceEnabled();

	private static double GetDailyMaintenanceFrameBudgetMs() => CampaignDailyMaintenanceController.GetDailyMaintenanceFrameBudgetMs();

	private void RunDailyMaintenanceSynchronously() => _dailyMaintenanceController.RunDailyMaintenanceSynchronously();

	private void EnqueueDailyMaintenanceForCurrentDay(string reason) => _dailyMaintenanceController.EnqueueDailyMaintenanceForCurrentDay(reason);

	private void QueueDeferredAutoWeeklyReportsForWeek(int weekIndex, int currentGameDayIndexSafe, string reason) => _dailyMaintenanceController.QueueDeferredAutoWeeklyReportsForWeek(weekIndex, currentGameDayIndexSafe, reason);

	private void EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind kind, int dayIndex = -1, int weekIndex = 0, int startDay = 0, int endDay = 0, string reason = "") => _dailyMaintenanceController.EnqueueDailyMaintenanceJob(kind, dayIndex, weekIndex, startDay, endDay, reason);

	private static string BuildDailyMaintenanceJobKey(DailyMaintenanceTaskKind kind, int dayIndex, int weekIndex, int startDay, int endDay) => CampaignDailyMaintenanceController.BuildDailyMaintenanceJobKey(kind, dayIndex, weekIndex, startDay, endDay);


	private bool HasPendingDeferredDailyMaintenanceWork() => _dailyMaintenanceController.HasPendingDeferredDailyMaintenanceWork();

	private void ProcessDeferredDailyMaintenance() => _dailyMaintenanceController.ProcessDeferredDailyMaintenance();

	private bool ExecuteDailyMaintenanceJob(DailyMaintenanceJob job, long startTimestamp, double budgetMs) => _dailyMaintenanceController.ExecuteDailyMaintenanceJob(job, startTimestamp, budgetMs);

	private bool ProcessKingdomStabilityRelationAdjustmentsSlice() => _kingdomStabilityGameAdapter.ProcessKingdomStabilityRelationAdjustmentsSlice();

	private void TryInitializePendingAutoWeeklyReportBuild(DailyMaintenanceJob job) => WeeklyRuntime.TryInitializePendingAutoWeeklyReportBuild(job);

	private void ProcessPendingAutoWeeklyReportBuildBudget(long startTimestamp, double budgetMs) => WeeklyRuntime.ProcessPendingAutoWeeklyReportBuildBudget(startTimestamp, budgetMs);

	private bool ProcessPendingAutoWeeklyReportPreviewGroupsBudget(PendingAutoWeeklyReportBuild context, long startTimestamp, double budgetMs) => WeeklyRuntime.ProcessPendingAutoWeeklyReportPreviewGroupsBudget(context, startTimestamp, budgetMs);

	private void ProcessPendingAutoWeeklyReportSourceMaterial(PendingAutoWeeklyReportBuild context, EventSourceMaterialEntry item) => WeeklyRuntime.ProcessPendingAutoWeeklyReportSourceMaterial(context, item);

	private bool ProcessPendingAutoWeeklyReportActionSlice(PendingAutoWeeklyReportBuild context, bool recentOnly) => WeeklyRuntime.ProcessPendingAutoWeeklyReportActionSlice(context, recentOnly);

	private void ProcessPendingAutoWeeklyReportNpcAction(PendingAutoWeeklyReportBuild context, Hero hero, NpcActionEntry action, bool recentOnly) => WeeklyRuntime.ProcessPendingAutoWeeklyReportNpcAction(context, hero, action, recentOnly);

	private WeeklyEventMaterialPreviewGroup BuildWeeklyEventMaterialPreviewGroupWithSnapshot(Func<WeeklyEventMaterialPreviewGroup> builder, List<EventSourceMaterialEntry> snapshot) => _campaignMaterialRecords.BuildWeeklyEventMaterialPreviewGroupWithSnapshot(builder, snapshot);

	private void ProcessPendingWeeklyReportAggregationBudget(PendingAutoWeeklyReportBuild context, long startTimestamp, double budgetMs) => WeeklyRuntime.ProcessPendingWeeklyReportAggregationBudget(context, startTimestamp, budgetMs);

	private void ProcessPendingWeeklyReportPromptMaterialsBudget(PendingAutoWeeklyReportBuild context, long startTimestamp, double budgetMs) => WeeklyRuntime.ProcessPendingWeeklyReportPromptMaterialsBudget(context, startTimestamp, budgetMs);

	private void ProcessPendingWeeklyReportBatchPromptsBudget(PendingAutoWeeklyReportBuild context, long startTimestamp, double budgetMs) => WeeklyRuntime.ProcessPendingWeeklyReportBatchPromptsBudget(context, startTimestamp, budgetMs);

	private void FinalizePendingAutoWeeklyReportBuild(PendingAutoWeeklyReportBuild context) => WeeklyRuntime.FinalizePendingAutoWeeklyReportBuild(context);

	private void StartAutoWeeklyReportsForWeek(int weekIndex, int currentGameDayIndexSafe) => WeeklyRuntime.StartAutoWeeklyReportsForWeek(weekIndex, currentGameDayIndexSafe);

	private void TryStartDeferredAutoWeeklyReports() => WeeklyRuntime.TryStartDeferredAutoWeeklyReports();

	private async Task GenerateAutoWeeklyReportsAsync(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay, List<WeeklyReportBatchRequest> preparedBatches = null, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshotOverride = null) => await WeeklyRuntime.GenerateAutoWeeklyReportsAsync(groups, weekIndex, startDay, endDay, preparedBatches, sourceSnapshotOverride);

	// Normal callers keep the existing sanitation behavior; database reload passes false to preserve unrelated dynamic history exactly.
	private void EnsureWeekZeroOpeningSummaryEvents(bool sanitizeAfter = true) => _weekZeroShortSummaries.EnsureWeekZeroOpeningSummaryEvents(sanitizeAfter);

	private bool ProcessWeekZeroOpeningSummaryEventsSlice() => _weekZeroShortSummaries.ProcessWeekZeroOpeningSummaryEventsSlice();

	private void FinalizeWeekZeroOpeningSummaryMaintenance() => _weekZeroShortSummaries.FinalizeWeekZeroOpeningSummaryMaintenance();

	private bool ProcessMissedStrategicWorldEventsSlice() => _worldBulletinEventCapture.ProcessMissedStrategicWorldEventsSlice();

	private void ResetMissedStrategicWorldEventMaintenance() => _worldBulletinEventCapture.ResetMissedStrategicWorldEventMaintenance();

	private static string ComputeWeekZeroShortSummarySourceHash(string sourceText) => WeekZeroOpeningSummaryGenerationController.ComputeWeekZeroShortSummarySourceHash(sourceText);

	private static string BuildWeekZeroPromptText(string sourceHash, bool llmGenerated) => WeekZeroOpeningSummaryGenerationController.BuildWeekZeroPromptText(sourceHash, llmGenerated);

	private static bool HasWeekZeroLlmShortSummary(EventRecordEntry entry, string sourceHash) => WeekZeroOpeningSummaryGenerationController.HasWeekZeroLlmShortSummary(entry, sourceHash);

	private static string NormalizeWeekZeroShortSummaryResponse(string rawResponse, string fallbackSource) => WeekZeroOpeningSummaryGenerationController.NormalizeWeekZeroShortSummaryResponse(rawResponse, fallbackSource);

	private static string BuildWeekZeroShortSummarySystemPrompt(string eventKind, string kingdomId, string title)
	{
        return WeeklyPromptCaptureAdapter.BuildWeekZeroShortSummarySystemPrompt(eventKind, kingdomId, title);
    }

	private static string BuildWeekZeroShortSummaryUserPrompt(string summary)
	{
        return WeeklyPromptCaptureAdapter.BuildWeekZeroShortSummaryUserPrompt(summary);
    }

	private static int GetWeekZeroShortSummaryQueuePriority(WeekZeroShortSummaryRequest request, Dictionary<string, int> kingdomOrder) => WeekZeroOpeningSummaryGenerationController.GetWeekZeroShortSummaryQueuePriority(request, kingdomOrder);

	private void SortWeekZeroShortSummaryPendingQueue() => _weekZeroShortSummaries.SortWeekZeroShortSummaryPendingQueue();

	private void EnsureWeekZeroShortSummaryQueueWorker() => _weekZeroShortSummaries.EnsureWeekZeroShortSummaryQueueWorker();

	private Task WaitForWeekZeroShortSummaryRequestSlotAsync() => _weekZeroShortSummaries.WaitForWeekZeroShortSummaryRequestSlotAsync();

	private Task<bool> GenerateWeekZeroShortSummaryWithRetriesAsync(WeekZeroShortSummaryRequest request) => _weekZeroShortSummaries.GenerateWeekZeroShortSummaryWithRetriesAsync(request);

	private Task<bool> ApplyWeekZeroShortSummaryOnMainThreadAsync(string eventId, string sourceHash, string shortSummary) => _weekZeroShortSummaries.ApplyWeekZeroShortSummaryOnMainThreadAsync(eventId, sourceHash, shortSummary);

	private void ProcessWeekZeroShortSummaryMainThreadActions() => _weekZeroShortSummaries.ProcessWeekZeroShortSummaryMainThreadActions();

	private Task ProcessWeekZeroShortSummaryQueueAsync() => _weekZeroShortSummaries.ProcessWeekZeroShortSummaryQueueAsync();

	private void TryQueueWeekZeroShortSummaryGeneration(EventRecordEntry entry, string sourceHash) => _weekZeroShortSummaries.TryQueueWeekZeroShortSummaryGeneration(entry, sourceHash);

	private bool UpsertWeekZeroOpeningSummaryEvent(string eventKind, string kingdomId, string title, string summary, string materialLabel, string materialType, bool sanitizeAfter = true) => _weekZeroShortSummaries.UpsertWeekZeroOpeningSummaryEvent(eventKind, kingdomId, title, summary, materialLabel, materialType, sanitizeAfter);

	private void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions) => _campaignBattleRecordCapture.OnSiegeAftermathApplied(attackerParty, settlement, aftermathType, previousSettlementOwner, partyContributions);

	private void OnVillageBeingRaided(Village village) => _campaignBattleRecordCapture.OnVillageBeingRaided(village);

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent) => _campaignBattleRecordCapture.OnRaidCompleted(winnerSide, raidEvent);

	private void OnKingdomDestroyed(Kingdom destroyedKingdom) => _worldBulletinEventCapture.OnKingdomDestroyed(destroyedKingdom);

	private void RecordKingdomDestroyedMaterial(Kingdom destroyedKingdom, string source) => _worldBulletinEventCapture.RecordKingdomDestroyedMaterial(destroyedKingdom, source);

	private static string BuildKingdomDestroyedSnapshotText(Kingdom destroyedKingdom, Clan rulingClan) => WorldBulletinEventCaptureAdapter.BuildKingdomDestroyedSnapshotText(destroyedKingdom, rulingClan);

	private void OnClanDestroyed(Clan destroyedClan) => _kingdomStabilityGameAdapter.OnClanDestroyed(destroyedClan);

	private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize) => _campaignCharacterRecordCapture.OnTournamentFinished(winner, participants, town, prize);

	private void RecordTournamentParticipantNpcActions(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, string settlementDisplayName, string prizeDisplayName, string participantSummary, Dictionary<string, string> tournamentParticipantRankLabels, string stableKey) => _campaignCharacterRecordCapture.RecordTournamentParticipantNpcActions(winner, participants, town, settlementDisplayName, prizeDisplayName, participantSummary, tournamentParticipantRankLabels, stableKey);

	private static List<Hero> BuildTournamentParticipantHeroes(MBReadOnlyList<CharacterObject> participants, Hero championHero) => CampaignCharacterRecordCaptureAdapter.BuildTournamentParticipantHeroes(participants, championHero);

	private static string BuildTournamentParticipantActionText(string settlementDisplayName, string championName, bool isChampion, string prizeDisplayName, string participantSummary, string tournamentRankLabel) => CampaignCharacterRecordCaptureAdapter.BuildTournamentParticipantActionText(settlementDisplayName, championName, isChampion, prizeDisplayName, participantSummary, tournamentRankLabel);

	private static Dictionary<string, string> BuildTournamentParticipantRankLabels(CharacterObject winner, Town town) => CampaignCharacterRecordCaptureAdapter.BuildTournamentParticipantRankLabels(winner, town);

	private static string BuildTournamentEliminationRankLabel(int furthestRoundIndex, int roundCount) => CampaignCharacterRecordCaptureAdapter.BuildTournamentEliminationRankLabel(furthestRoundIndex, roundCount);

	private static Kingdom ResolveTournamentHostKingdom(Town town) => CampaignCharacterRecordCaptureAdapter.ResolveTournamentHostKingdom(town);

	private static Hero GetTournamentHero(CharacterObject character) => CampaignCharacterRecordCaptureAdapter.GetTournamentHero(character);

	private static string GetTournamentHeroId(CharacterObject character) => CampaignCharacterRecordCaptureAdapter.GetTournamentHeroId(character);

	private static string GetTournamentCharacterId(CharacterObject character) => CampaignCharacterRecordCaptureAdapter.GetTournamentCharacterId(character);

	private static string GetTournamentCharacterDisplayName(CharacterObject character, string fallback) => CampaignCharacterRecordCaptureAdapter.GetTournamentCharacterDisplayName(character, fallback);

	private static string BuildTournamentWinnerStatusText(CharacterObject winner) => CampaignCharacterRecordCaptureAdapter.BuildTournamentWinnerStatusText(winner);

	private static string BuildTournamentCharacterRoleSuffix(CharacterObject character) => CampaignCharacterRecordCaptureAdapter.BuildTournamentCharacterRoleSuffix(character);

	private static string BuildTournamentParticipantSummary(MBReadOnlyList<CharacterObject> participants, CharacterObject winner) => CampaignCharacterRecordCaptureAdapter.BuildTournamentParticipantSummary(participants, winner);

	private static string GetTournamentPrizeDisplayName(ItemObject prize) => CampaignCharacterRecordCaptureAdapter.GetTournamentPrizeDisplayName(prize);

		private static bool ShouldTrackNpcActionHero(Hero hero, bool allowNonLordHero = false) => CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(hero, allowNonLordHero);

		private static List<Hero> GetTrackedLordsForClan(Clan clan) => CampaignCharacterRecordCaptureAdapter.GetTrackedLordsForClan(clan);

	private static string GetHeroDisplayName(Hero hero) => MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(hero);

	private static string GetClanDisplayName(Clan clan) => MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan);

	private static string GetKingdomDisplayName(Kingdom kingdom, string fallback = "某个王国") => MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, fallback);

	private static string GetSettlementDisplayName(Settlement settlement) => MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);

	private static string GetSettlementTypeLabel(Settlement settlement) => MemoryEntityIdentityBannerlordAdapter.GetSettlementTypeLabel(settlement);



	private static string BuildOrderedHeroPairStableKey(string prefix, Hero hero1, Hero hero2) => CampaignCharacterRecordCaptureAdapter.BuildOrderedHeroPairStableKey(prefix, hero1, hero2);

	private static string BuildClanChangedKingdomStableKey(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail) => KingdomStabilityGameAdapter.BuildClanChangedKingdomStableKey(clan, oldKingdom, newKingdom, detail);

	private static string GetClanChangedKingdomActionKind(ChangeKingdomAction.ChangeKingdomActionDetail detail) => KingdomStabilityGameAdapter.GetClanChangedKingdomActionKind(detail);

	private static string BuildClanChangedKingdomNarrative(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail) => KingdomStabilityGameAdapter.BuildClanChangedKingdomNarrative(clan, oldKingdom, newKingdom, detail);

		private static string BuildKingdomDecisionStableKey(KingdomDecision decision) => PoliticalDecisionRecordCaptureAdapter.BuildKingdomDecisionStableKey(decision);

		private static string BuildKingdomDecisionNarrative(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved, bool forProposer) => PoliticalDecisionRecordCaptureAdapter.BuildKingdomDecisionNarrative(decision, chosenOutcome, isPlayerInvolved, forProposer);

		private static string AppendEventMaterialSentence(string text, string addition) => PoliticalDecisionRecordCaptureAdapter.AppendEventMaterialSentence(text, addition);

		private static string BuildKingdomDecisionPoliticalTriggerReason(KingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildKingdomDecisionPoliticalTriggerReason(decision, chosenOutcome);

		private static string BuildWarOrPeacePoliticalTriggerReason(string materialKind, IFaction faction1, IFaction faction2, string detailText) => PoliticalDecisionRecordCaptureAdapter.BuildWarOrPeacePoliticalTriggerReason(materialKind, faction1, faction2, detailText);

		private static string BuildDeclareWarPoliticalTriggerReason(IFaction declaringFaction, IFaction targetFaction, Clan evaluatingClan, DeclareWarDecision decision, string detailText) => PoliticalDecisionRecordCaptureAdapter.BuildDeclareWarPoliticalTriggerReason(declaringFaction, targetFaction, evaluatingClan, decision, detailText);

		private static string BuildMakePeacePoliticalTriggerReason(IFaction peaceFaction, IFaction targetFaction, Clan evaluatingClan, MakePeaceKingdomDecision decision, string detailText) => PoliticalDecisionRecordCaptureAdapter.BuildMakePeacePoliticalTriggerReason(peaceFaction, targetFaction, evaluatingClan, decision, detailText);

		private static string BuildKingdomPolicyPoliticalTriggerReason(KingdomPolicyDecision decision) => PoliticalDecisionRecordCaptureAdapter.BuildKingdomPolicyPoliticalTriggerReason(decision);

		private static bool SafeGetKingdomPolicyDecisionIsInverted(KingdomPolicyDecision decision) => PoliticalDecisionRecordCaptureAdapter.SafeGetKingdomPolicyDecisionIsInverted(decision);

		private static float? SafeCalculateKingdomPolicySupport(KingdomPolicyDecision decision, Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeCalculateKingdomPolicySupport(decision, clan);

		private static string GetPolicyDisplayName(PolicyObject policy) => PoliticalDecisionRecordCaptureAdapter.GetPolicyDisplayName(policy);

		private static string BuildPolicyDominantWeightPart(PolicyObject policy, bool isInverted) => PoliticalDecisionRecordCaptureAdapter.BuildPolicyDominantWeightPart(policy, isInverted);

		private static string BuildPolicyWeightSummaryPart(PolicyObject policy) => PoliticalDecisionRecordCaptureAdapter.BuildPolicyWeightSummaryPart(policy);

		private static string FormatPoliticalPolicyWeight(float value) => PoliticalDecisionRecordCaptureAdapter.FormatPoliticalPolicyWeight(value);

		private static string BuildPolicyProposerProfilePart(Clan clan) => PoliticalDecisionRecordCaptureAdapter.BuildPolicyProposerProfilePart(clan);

		private static string BuildPolicyLeaderTraitPart(Hero leader) => PoliticalDecisionRecordCaptureAdapter.BuildPolicyLeaderTraitPart(leader);

		private static void AddPolicyLeaderTraitLabel(List<string> parts, string label, int level) => PoliticalDecisionRecordCaptureAdapter.AddPolicyLeaderTraitLabel(parts, label, level);

		private static void AddPolicySupportTendencyPart(List<string> parts, float? support) => PoliticalDecisionRecordCaptureAdapter.AddPolicySupportTendencyPart(parts, support);

		private static string GetPolicySupportTendencyLabel(float support) => PoliticalDecisionRecordCaptureAdapter.GetPolicySupportTendencyLabel(support);

		private static string BuildKingSelectionPoliticalTriggerReason(KingSelectionKingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildKingSelectionPoliticalTriggerReason(decision, chosenOutcome);

		private static Hero ResolveKingSelectionOutcomeKing(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.ResolveKingSelectionOutcomeKing(chosenOutcome);

		private static void AddKingSelectionFormulaRulePart(List<string> parts) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionFormulaRulePart(parts);

		private static void AddKingSelectionCandidateProfileParts(List<string> parts, Hero chosenKing, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionCandidateProfileParts(parts, chosenKing, kingdom);

		private static float? SafeGetDiplomacyClanStrength(Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeGetDiplomacyClanStrength(clan);

		private static void AddKingSelectionCandidateFiefPart(List<string> parts, Clan clan, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionCandidateFiefPart(parts, clan, kingdom);

		private static void AddKingSelectionCandidateWarPartyPart(List<string> parts, Clan clan) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionCandidateWarPartyPart(parts, clan);

		private static void AddKingSelectionCandidateRankingPart(List<string> parts, KingSelectionKingdomDecision decision, Hero chosenKing) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionCandidateRankingPart(parts, decision, chosenKing);

		private static Clan SafeGetKingSelectionClanToExclude(KingSelectionKingdomDecision decision) => PoliticalDecisionRecordCaptureAdapter.SafeGetKingSelectionClanToExclude(decision);

		private static void AddKingSelectionOutcomeScorePart(List<string> parts, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionOutcomeScorePart(parts, chosenOutcome);

		private static void AddKingSelectionVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionVoteSupportPart(parts, chosenOutcome);

		private static void AddKingSelectionVoterTraitPart(List<string> parts, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.AddKingSelectionVoterTraitPart(parts, kingdom);

		private static string BuildStartAlliancePoliticalTriggerReason(StartAllianceDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildStartAlliancePoliticalTriggerReason(decision, chosenOutcome);

		private static string BuildTradeAgreementPoliticalTriggerReason(TradeAgreementDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildTradeAgreementPoliticalTriggerReason(decision, chosenOutcome);

		private static string BuildProposeCallToWarPoliticalTriggerReason(ProposeCallToWarAgreementDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildProposeCallToWarPoliticalTriggerReason(decision, chosenOutcome);

		private static string BuildAcceptCallToWarPoliticalTriggerReason(AcceptCallToWarAgreementDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildAcceptCallToWarPoliticalTriggerReason(decision, chosenOutcome);

		private static bool? SafeGetStartAllianceOutcomeShouldStart(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.SafeGetStartAllianceOutcomeShouldStart(chosenOutcome);

		private static bool? SafeGetTradeAgreementOutcomeShouldStart(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.SafeGetTradeAgreementOutcomeShouldStart(chosenOutcome);

		private static bool? SafeGetProposeCallToWarOutcomeShouldCall(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.SafeGetProposeCallToWarOutcomeShouldCall(chosenOutcome);

		private static bool? SafeGetAcceptCallToWarOutcomeShouldAccept(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.SafeGetAcceptCallToWarOutcomeShouldAccept(chosenOutcome);

		private static string GetAgreementOutcomeLabel(bool? accepted, string positiveLabel, string negativeLabel) => PoliticalDecisionRecordCaptureAdapter.GetAgreementOutcomeLabel(accepted, positiveLabel, negativeLabel);

		private static float? SafeGetStartAllianceSupportScore(StartAllianceDecision decision, Clan clan, out string reasonText) => PoliticalDecisionRecordCaptureAdapter.SafeGetStartAllianceSupportScore(decision, clan, out reasonText);

		private static float? SafeGetTradeAgreementSupportScore(Kingdom sourceKingdom, Kingdom targetKingdom, Clan clan, out string reasonText) => PoliticalDecisionRecordCaptureAdapter.SafeGetTradeAgreementSupportScore(sourceKingdom, targetKingdom, clan, out reasonText);

		private static float? SafeGetCallingToWarScore(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom targetKingdom, Clan clan, out string reasonText) => PoliticalDecisionRecordCaptureAdapter.SafeGetCallingToWarScore(callingKingdom, calledKingdom, targetKingdom, clan, out reasonText);

		private static float? SafeGetJoiningWarScore(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom targetKingdom, Clan clan, out string reasonText) => PoliticalDecisionRecordCaptureAdapter.SafeGetJoiningWarScore(callingKingdom, calledKingdom, targetKingdom, clan, out reasonText);

		private static void AddAgreementModelScorePart(List<string> parts, string label, float? score, string reasonText) => PoliticalDecisionRecordCaptureAdapter.AddAgreementModelScorePart(parts, label, score, reasonText);

		private static void AddTradeAgreementScorePart(List<string> parts, float? score, string reasonText) => PoliticalDecisionRecordCaptureAdapter.AddTradeAgreementScorePart(parts, score, reasonText);

		private static void AddAgreementReasonTextPart(List<string> parts, string reasonText) => PoliticalDecisionRecordCaptureAdapter.AddAgreementReasonTextPart(parts, reasonText);

		private static void AddAgreementFinalSupportPart(List<string> parts, KingdomDecision decision, Clan clan, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.AddAgreementFinalSupportPart(parts, decision, clan, chosenOutcome);

		private static void AddAgreementVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome, bool? accepted, string positiveLabel, string negativeLabel) => PoliticalDecisionRecordCaptureAdapter.AddAgreementVoteSupportPart(parts, chosenOutcome, accepted, positiveLabel, negativeLabel);

		private static void AddKingdomDiplomaticStatusPart(List<string> parts, Kingdom sourceKingdom, Kingdom targetKingdom) => PoliticalDecisionRecordCaptureAdapter.AddKingdomDiplomaticStatusPart(parts, sourceKingdom, targetKingdom);

		private static void AddCallToWarDiplomaticStatusParts(List<string> parts, Kingdom callingKingdom, Kingdom calledKingdom, Kingdom targetKingdom, bool isAcceptanceDecision) => PoliticalDecisionRecordCaptureAdapter.AddCallToWarDiplomaticStatusParts(parts, callingKingdom, calledKingdom, targetKingdom, isAcceptanceDecision);

		private static void AddCallToWarStrengthParts(List<string> parts, Kingdom firstKingdom, Kingdom secondKingdom, Kingdom targetKingdom) => PoliticalDecisionRecordCaptureAdapter.AddCallToWarStrengthParts(parts, firstKingdom, secondKingdom, targetKingdom);

		private static string BuildExpelClanPoliticalTriggerReason(ExpelClanFromKingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildExpelClanPoliticalTriggerReason(decision, chosenOutcome);

		private static bool? SafeGetExpelClanOutcomeShouldBeExpelled(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.SafeGetExpelClanOutcomeShouldBeExpelled(chosenOutcome);

		private static void AddExpelClanTargetProfileParts(List<string> parts, Clan targetClan, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanTargetProfileParts(parts, targetClan, kingdom);

		private static float? SafeGetClanInfluence(Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeGetClanInfluence(clan);

		private static float? SafeGetClanRenown(Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeGetClanRenown(clan);

		private static void AddExpelClanFiefValueParts(List<string> parts, Clan targetClan, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanFiefValueParts(parts, targetClan, kingdom);

		private static float? SafeGetSettlementValueForKingdom(Settlement settlement, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.SafeGetSettlementValueForKingdom(settlement, kingdom);

		private static void AddExpelClanWarPartyParts(List<string> parts, Clan targetClan) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanWarPartyParts(parts, targetClan);

		private static void AddExpelClanRelationNetworkParts(List<string> parts, Clan targetClan, Kingdom kingdom) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanRelationNetworkParts(parts, targetClan, kingdom);

		private static int? SafeGetClanRelation(Clan sourceClan, Clan targetClan) => PoliticalDecisionRecordCaptureAdapter.SafeGetClanRelation(sourceClan, targetClan);

		private static void AddExpelClanRelationToKeyClanPart(List<string> parts, string label, Clan targetClan, Clan keyClan) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanRelationToKeyClanPart(parts, label, targetClan, keyClan);

		private static void AddExpelClanDefaultFormulaPart(List<string> parts, bool? shouldExpel) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanDefaultFormulaPart(parts, shouldExpel);

		private static void AddExpelClanVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome, bool? shouldExpel) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanVoteSupportPart(parts, chosenOutcome, shouldExpel);

		private static void AddExpelClanRelationCostPart(List<string> parts, bool? shouldExpel) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanRelationCostPart(parts, shouldExpel);

		private static void AddExpelClanFormulaValuePart(List<string> parts, ExpelClanFromKingdomDecision decision, DecisionOutcome chosenOutcome, bool? shouldExpel) => PoliticalDecisionRecordCaptureAdapter.AddExpelClanFormulaValuePart(parts, decision, chosenOutcome, shouldExpel);

		private static string BuildSettlementClaimantPreliminaryPoliticalTriggerReason(SettlementClaimantPreliminaryDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildSettlementClaimantPreliminaryPoliticalTriggerReason(decision, chosenOutcome);

		private static string BuildSettlementClaimantPoliticalTriggerReason(SettlementClaimantDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildSettlementClaimantPoliticalTriggerReason(decision, chosenOutcome);

		private static Clan SafeResolveSettlementClaimantPreliminaryOwnerClan(SettlementClaimantPreliminaryDecision decision) => PoliticalDecisionRecordCaptureAdapter.SafeResolveSettlementClaimantPreliminaryOwnerClan(decision);

		private static bool? SafeGetSettlementClaimantPreliminaryOutcomeShouldChange(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.SafeGetSettlementClaimantPreliminaryOutcomeShouldChange(chosenOutcome);

		private static Clan ResolveSettlementClaimantOutcomeClan(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.ResolveSettlementClaimantOutcomeClan(chosenOutcome);

		private static float? SafeCalculateSettlementClaimantPreliminarySupport(SettlementClaimantPreliminaryDecision decision, Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeCalculateSettlementClaimantPreliminarySupport(decision, clan);

		private static float? SafeDetermineKingdomDecisionSupport(KingdomDecision decision, Clan clan, DecisionOutcome outcome) => PoliticalDecisionRecordCaptureAdapter.SafeDetermineKingdomDecisionSupport(decision, clan, outcome);

		private static void AddSettlementOwnerRelationNetworkParts(List<string> parts, Clan ownerClan) => PoliticalDecisionRecordCaptureAdapter.AddSettlementOwnerRelationNetworkParts(parts, ownerClan);

		private static int? SafeGetLeaderRelation(Hero source, Hero target) => PoliticalDecisionRecordCaptureAdapter.SafeGetLeaderRelation(source, target);

		private static void AddSettlementOwnerStrengthPart(List<string> parts, Clan ownerClan) => PoliticalDecisionRecordCaptureAdapter.AddSettlementOwnerStrengthPart(parts, ownerClan);

		private static void AddSettlementClaimantCandidateFormulaParts(List<string> parts, Settlement settlement, Clan clan) => PoliticalDecisionRecordCaptureAdapter.AddSettlementClaimantCandidateFormulaParts(parts, settlement, clan);

		private static void AddSettlementClaimantExistingFiefParts(List<string> parts, Settlement targetSettlement, Clan clan) => PoliticalDecisionRecordCaptureAdapter.AddSettlementClaimantExistingFiefParts(parts, targetSettlement, clan);

		private static string BuildSettlementClaimantDistancePart(float nearest, float secondNearest) => PoliticalDecisionRecordCaptureAdapter.BuildSettlementClaimantDistancePart(nearest, secondNearest);

		private static void AddSettlementClaimantSpecialBonusParts(List<string> parts, Settlement settlement, Clan clan) => PoliticalDecisionRecordCaptureAdapter.AddSettlementClaimantSpecialBonusParts(parts, settlement, clan);

		private static float? SafeGetSettlementClaimantAdjustedStrength(Settlement settlement, Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeGetSettlementClaimantAdjustedStrength(settlement, clan);

		private static float? SafeGetClanStrength(Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeGetClanStrength(clan);

		private static float? SafeGetSettlementValueForClan(Settlement settlement, Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeGetSettlementValueForClan(settlement, clan);

		private static void AddSettlementClaimantCandidateRankingPart(List<string> parts, SettlementClaimantDecision decision, Clan chosenClan) => PoliticalDecisionRecordCaptureAdapter.AddSettlementClaimantCandidateRankingPart(parts, decision, chosenClan);

		private static void AddSettlementClaimantVoteSupportPart(List<string> parts, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.AddSettlementClaimantVoteSupportPart(parts, chosenOutcome);

		private static void AddSettlementSupportTendencyPart(List<string> parts, string label, float? support) => PoliticalDecisionRecordCaptureAdapter.AddSettlementSupportTendencyPart(parts, label, support);

		private static string GetSettlementSupportTendencyLabel(float support) => PoliticalDecisionRecordCaptureAdapter.GetSettlementSupportTendencyLabel(support);

		private static Clan ResolvePoliticalReasonEvaluatorClan(IFaction faction) => PoliticalDecisionRecordCaptureAdapter.ResolvePoliticalReasonEvaluatorClan(faction);

		private static float? SafeGetPoliticalDecisionThreshold(IFaction faction) => PoliticalDecisionRecordCaptureAdapter.SafeGetPoliticalDecisionThreshold(faction);

		private static float? SafeCalculateDeclareWarSupport(DeclareWarDecision decision, Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeCalculateDeclareWarSupport(decision, clan);

		private static float? SafeCalculateMakePeaceSupport(MakePeaceKingdomDecision decision, Clan clan) => PoliticalDecisionRecordCaptureAdapter.SafeCalculateMakePeaceSupport(decision, clan);

		private static string BuildPeaceTributeParameter(MakePeaceKingdomDecision decision, IFaction peaceFaction) => PoliticalDecisionRecordCaptureAdapter.BuildPeaceTributeParameter(decision, peaceFaction);

		private static void AddPoliticalFactionParameterParts(List<string> parts, IFaction sourceFaction, IFaction targetFaction, Clan evaluatingClan) => PoliticalDecisionRecordCaptureAdapter.AddPoliticalFactionParameterParts(parts, sourceFaction, targetFaction, evaluatingClan);

		private static void AddPoliticalFallbackPartIfNeeded(List<string> parts, bool hasModelReason, string actionLabel, string detailText) => PoliticalDecisionRecordCaptureAdapter.AddPoliticalFallbackPartIfNeeded(parts, hasModelReason, actionLabel, detailText);

		private static string GetPoliticalEventDetailLabel(string detailText) => PoliticalDecisionRecordCaptureAdapter.GetPoliticalEventDetailLabel(detailText);

		private static string BuildPoliticalStrengthComparisonPart(IFaction sourceFaction, IFaction targetFaction, float? sourceStrength, float? targetStrength) => PoliticalDecisionRecordCaptureAdapter.BuildPoliticalStrengthComparisonPart(sourceFaction, targetFaction, sourceStrength, targetStrength);

		private static string GetPoliticalStrengthComparisonLabel(float sourceStrength, float targetStrength, string targetName) => PoliticalDecisionRecordCaptureAdapter.GetPoliticalStrengthComparisonLabel(sourceStrength, targetStrength, targetName);

		private static float? SafeGetFactionStrength(IFaction faction) => PoliticalDecisionRecordCaptureAdapter.SafeGetFactionStrength(faction);

		private static int? SafeGetFactionWarPartyCount(IFaction faction) => PoliticalDecisionRecordCaptureAdapter.SafeGetFactionWarPartyCount(faction);

		private static int? SafeGetPoliticalRelation(Clan evaluatingClan, IFaction sourceFaction, IFaction targetFaction) => PoliticalDecisionRecordCaptureAdapter.SafeGetPoliticalRelation(evaluatingClan, sourceFaction, targetFaction);

		private static void AddPoliticalReasonNumberPart(List<string> parts, string label, float value) => PoliticalDecisionRecordCaptureAdapter.AddPoliticalReasonNumberPart(parts, label, value);

		private static void AddPoliticalReasonNumberPart(List<string> parts, string label, float? value) => PoliticalDecisionRecordCaptureAdapter.AddPoliticalReasonNumberPart(parts, label, value);

		private static void AddPoliticalScoreAgainstThresholdPart(List<string> parts, string label, float score, float? threshold, string thresholdLabel) => PoliticalDecisionRecordCaptureAdapter.AddPoliticalScoreAgainstThresholdPart(parts, label, score, threshold, thresholdLabel);

		private static bool IsExtremePoliticalReasonScore(float value) => PoliticalDecisionRecordCaptureAdapter.IsExtremePoliticalReasonScore(value);

		private static string GetPoliticalScoreThresholdRelation(float score, float threshold) => PoliticalDecisionRecordCaptureAdapter.GetPoliticalScoreThresholdRelation(score, threshold);

		private static string FormatPoliticalReasonNumber(float? value) => PoliticalDecisionRecordCaptureAdapter.FormatPoliticalReasonNumber(value);











		private static string BuildPlayerKingdomDecisionActionText(KingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildPlayerKingdomDecisionActionText(decision, chosenOutcome);

		private static Hero ResolveKingdomDecisionActionTargetHero(KingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.ResolveKingdomDecisionActionTargetHero(decision, chosenOutcome);

		private static void ApplyKingdomDecisionSpecificFacts(NpcActionFacts facts, KingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.ApplyKingdomDecisionSpecificFacts(facts, decision, chosenOutcome);

	private static string BuildSettlementOwnerChangedStableKey(Settlement settlement, Hero newOwner, Hero oldOwner, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail) => CampaignCharacterRecordCaptureAdapter.BuildSettlementOwnerChangedStableKey(settlement, newOwner, oldOwner, detail);

	private static string GetSettlementOwnerChangeDetailLabel(ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail) => CampaignCharacterRecordCaptureAdapter.GetSettlementOwnerChangeDetailLabel(detail);

	private static string GetHeroKilledVerb(KillCharacterAction.KillCharacterActionDetail detail) => CampaignCharacterRecordCaptureAdapter.GetHeroKilledVerb(detail);

	private static bool IsExecutionKillDetail(KillCharacterAction.KillCharacterActionDetail detail) => CampaignCharacterRecordCaptureAdapter.IsExecutionKillDetail(detail);

	private static string BuildExecutedVictimActionText(Hero killer, KillCharacterAction.KillCharacterActionDetail detail) => CampaignCharacterRecordCaptureAdapter.BuildExecutedVictimActionText(killer, detail);

	// Vengeance public executions park method/charge facts just before the vanilla death.
	internal static VengeanceExecutionFacts ResolvePublicExecutionFacts(Hero victim, KillCharacterAction.KillCharacterActionDetail detail) => CampaignCharacterRecordCaptureAdapter.ResolvePublicExecutionFacts(victim, detail);

	private static string BuildPublicExecutionVenueText(VengeanceExecutionFacts facts) => CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionVenueText(facts);

	private static string BuildPublicExecutionMethodText(VengeanceExecutionFacts facts) => CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionMethodText(facts);

	private static string BuildPublicExecutionChargeText(VengeanceExecutionFacts facts) => CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionChargeText(facts);

	private static string BuildPublicExecutionKillerActionText(Hero victim, VengeanceExecutionFacts facts) => CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionKillerActionText(victim, facts);

	private static string BuildPublicExecutionVictimActionText(Hero killer, VengeanceExecutionFacts facts) => CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionVictimActionText(killer, facts);

	private static string BuildPublicExecutionClanSuffix(Hero killer, VengeanceExecutionFacts facts) => CampaignCharacterRecordCaptureAdapter.BuildPublicExecutionClanSuffix(killer, facts);

	private static Settlement ResolveHeroExecutionSettlement(Hero victim, Hero killer) => CampaignCharacterRecordCaptureAdapter.ResolveHeroExecutionSettlement(victim, killer);

	private static string ResolveHeroExecutionLocationText(Settlement settlement, Hero victim, Hero killer) => CampaignCharacterRecordCaptureAdapter.ResolveHeroExecutionLocationText(settlement, victim, killer);

	private static Kingdom ResolveHeroKingdomForWeeklyMaterial(Hero hero) => MemoryEntityIdentityBannerlordAdapter.ResolveHeroKingdomForWeeklyMaterial(hero);

	private static string BuildPlayerExecutionWeeklySnapshot(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, Kingdom victimKingdom, Kingdom killerKingdom, string locationText, string gameDate, VengeanceExecutionFacts executionFacts = null) => CampaignCharacterRecordCaptureAdapter.BuildPlayerExecutionWeeklySnapshot(victim, killer, detail, victimKingdom, killerKingdom, locationText, gameDate, executionFacts);

	private static string BuildPrisonerTakenStableKey(Hero capturerHero, Hero prisoner) => CampaignBattleRecordCaptureAdapter.BuildPrisonerTakenStableKey(capturerHero, prisoner);

	private static string BuildPrisonerReleasedStableKey(Hero capturerHero, Hero prisoner, EndCaptivityDetail detail) => CampaignBattleRecordCaptureAdapter.BuildPrisonerReleasedStableKey(capturerHero, prisoner, detail);

	private static string GetEndCaptivityDetailLabel(EndCaptivityDetail detail) => CampaignBattleRecordCaptureAdapter.GetEndCaptivityDetailLabel(detail);

	private static string GetSiegeAftermathLabel(SiegeAftermathAction.SiegeAftermath aftermathType) => CampaignBattleRecordCaptureAdapter.GetSiegeAftermathLabel(aftermathType);

	private static string GetBattleSideLabel(BattleSideEnum side)
	{
		return side switch
		{
			BattleSideEnum.Attacker => "进攻方",
			BattleSideEnum.Defender => "防守方",
			_ => side.ToString()
		};
	}

	private static string GetRaidOutcomeLabel(BattleSideEnum side) => CampaignBattleRecordCaptureAdapter.GetRaidOutcomeLabel(side);

		private static string BuildKingdomDecisionSupporterSummary(KingdomDecision decision, DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.BuildKingdomDecisionSupporterSummary(decision, chosenOutcome);

		private static string GetKingdomDecisionTitle(KingdomDecision decision) => PoliticalDecisionRecordCaptureAdapter.GetKingdomDecisionTitle(decision);

		private static string GetKingdomDecisionOutcomeTitle(DecisionOutcome chosenOutcome) => PoliticalDecisionRecordCaptureAdapter.GetKingdomDecisionOutcomeTitle(chosenOutcome);

		private static bool IsTrivialKingdomDecisionOutcomeTitle(string text) => PoliticalDecisionRecordCaptureAdapter.IsTrivialKingdomDecisionOutcomeTitle(text);

		private static string GetSupportWeightLabel(Supporter.SupportWeights supportWeight) => PoliticalDecisionRecordCaptureAdapter.GetSupportWeightLabel(supportWeight);

	private static bool ShouldMentionBattleHero(Hero hero) => CampaignBattleRecordCaptureAdapter.ShouldMentionBattleHero(hero);

	private static string GetNpcActionHeroKey(Hero hero) => CampaignCharacterRecordCaptureAdapter.GetNpcActionHeroKey(hero);

		private static int GetCurrentGameDayIndexSafe() => MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();

		private static string GetCurrentGameDateTextSafe() => MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDateTextSafe();


	private static string BuildPrefixedEventSourceStableKey(string prefix, string stableKey, string fallbackText) => CampaignMaterialRecordOwner.BuildPrefixedEventSourceStableKey(prefix, stableKey, fallbackText);

		private static string GetClanId(Clan clan) => MemoryEntityIdentityBannerlordAdapter.GetClanId(clan);

		private static string GetKingdomId(Kingdom kingdom) => MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom);

		private static string GetKingdomId(IFaction faction) => MemoryEntityIdentityBannerlordAdapter.GetKingdomId(faction);

	private static string GetHeroId(Hero hero) => MemoryEntityIdentityBannerlordAdapter.GetHeroId(hero);

		private static string GetSettlementId(Settlement settlement) => MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement);

	private static int ClampKingdomStabilityValue(int value)
	{
		return KingdomStabilityPolicy.ClampKingdomStabilityValue(value);
	}

	private int GetKingdomStabilityValue(Kingdom kingdom) => _kingdomStabilityGameAdapter.GetKingdomStabilityValue(kingdom);

	private int GetKingdomStabilityValue(string kingdomId)
	{
		return KingdomStability.Get(kingdomId);
	}

	public static int GetKingdomStabilityValueForExternal(Kingdom kingdom)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.GetKingdomStabilityValue(kingdom) ?? KingdomStabilityDefaultValue;
		}
		catch
		{
			return KingdomStabilityDefaultValue;
		}
	}

	public static bool IsModCreatedRebelKingdomForExternal(Kingdom kingdom)
	{
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior != null && behavior.IsKnownOrLegacyModCreatedRebelKingdom(kingdom);
		}
		catch
		{
			return false;
		}
	}

	public static bool TryAdjustKingdomStabilityForExternal(Kingdom kingdom, int delta, string reason, out int before, out int after)
	{
		before = KingdomStabilityDefaultValue;
		after = KingdomStabilityDefaultValue;
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (behavior == null || kingdom == null)
			{
				return false;
			}
			before = behavior.GetKingdomStabilityValue(kingdom);
			after = ClampKingdomStabilityValue(before + delta);
			behavior.SetKingdomStabilityValue(kingdom, after);
			Logger.Log("KingdomStability", "[CustomPolicy] adjusted kingdom=" + GetKingdomId(kingdom)
				+ " delta=" + delta.ToString(CultureInfo.InvariantCulture)
				+ " before=" + before.ToString(CultureInfo.InvariantCulture)
				+ " after=" + after.ToString(CultureInfo.InvariantCulture)
				+ " reason=" + ((reason ?? "").Trim()));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomStability", "[CustomPolicy][WARN] adjust failed kingdom=" + GetKingdomId(kingdom)
				+ " delta=" + delta.ToString(CultureInfo.InvariantCulture)
				+ " error=" + ex.Message);
			return false;
		}
	}

	public static bool TryDiscontinueLandlessKingdomForExternal(Kingdom kingdom, string reason)
	{
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior != null && behavior.TryDiscontinueLandlessKingdom(kingdom, reason, requireKnownModRebelKingdom: true, allowPlayerKingdom: false);
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[WARN] external rebel cleanup failed: " + ex.Message);
			return false;
		}
	}

	public static string BuildKingdomStabilityEncyclopediaTextForExternal(Kingdom kingdom)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.BuildKingdomStabilityEncyclopediaText(kingdom) ?? "";
		}
		catch (Exception ex)
		{
			Logger.Log("EncyclopediaKingdomStability", "[WARN] Failed to build kingdom stability encyclopedia text: " + ex.Message);
			return "";
		}
	}

	private string BuildKingdomStabilityEncyclopediaText(Kingdom kingdom) => _kingdomStabilityGameAdapter.BuildKingdomStabilityEncyclopediaText(kingdom);

	private void SetKingdomStabilityValue(Kingdom kingdom, int value) => _kingdomStabilityGameAdapter.SetKingdomStabilityValue(kingdom, value);

	private static int GetKingdomStabilityRelationTargetOffset(int stabilityValue)
	{
		return KingdomStabilityPolicy.GetKingdomStabilityRelationTargetOffset(stabilityValue);
	}

	private static KingdomStabilityTier GetKingdomStabilityTier(int value)
	{
		return (KingdomStabilityTier)KingdomStabilityPolicy.GetKingdomStabilityTier(value);
	}

	private static string GetKingdomStabilityTierText(int value) => KingdomStabilityGameAdapter.GetKingdomStabilityTierText(value);

	private static string FormatKingdomStabilityRelationOffsetText(int offset) => KingdomStabilityGameAdapter.FormatKingdomStabilityRelationOffsetText(offset);

	private static bool ShouldApplyKingdomStabilityRelationAdjustmentToClan(Clan clan, Kingdom kingdom) => KingdomStabilityGameAdapter.ShouldApplyKingdomStabilityRelationAdjustmentToClan(clan, kingdom);

	private static IEnumerable<Hero> GetClanHeroesForKingdomStabilityRelationAdjustment(Clan clan) => KingdomStabilityGameAdapter.GetClanHeroesForKingdomStabilityRelationAdjustment(clan);

	private static string BuildKingdomStabilityRelationOffsetKey(string kingdomId, Hero sourceHero, Hero targetHero) => KingdomStabilityGameAdapter.BuildKingdomStabilityRelationOffsetKey(kingdomId, sourceHero, targetHero);

	private static bool TryResolveKingdomStabilityRelationOffsetKey(string key, out string kingdomId, out Hero sourceHero, out Hero targetHero) => KingdomStabilityGameAdapter.TryResolveKingdomStabilityRelationOffsetKey(key, out kingdomId, out sourceHero, out targetHero);

	private static int ClampHeroRelationValue(int value)
	{
		return MBMath.ClampInt(value, -100, 100);
	}

	private int ApplyKingdomStabilityRelationOffsetToPair(string pairKey, Hero sourceHero, Hero targetHero, int desiredOffset) => _kingdomStabilityGameAdapter.ApplyKingdomStabilityRelationOffsetToPair(pairKey, sourceHero, targetHero, desiredOffset);

	private void ApplyKingdomStabilityRelationAdjustmentsForKingdom(Kingdom kingdom) => _kingdomStabilityGameAdapter.ApplyKingdomStabilityRelationAdjustmentsForKingdom(kingdom);

	private void ClearKingdomStabilityRelationAdjustmentsForKingdom(string kingdomId) => _kingdomStabilityGameAdapter.ClearKingdomStabilityRelationAdjustmentsForKingdom(kingdomId);

	private void ClearKingdomStabilityRelationAdjustments() => _kingdomStabilityGameAdapter.ClearKingdomStabilityRelationAdjustments();

	private void ApplyKingdomStabilityRelationAdjustments() => _kingdomStabilityGameAdapter.ApplyKingdomStabilityRelationAdjustments();

	private static int GetLowClanCountRoyalDomainLoyaltyAdjustment(int stabilityValue, int activeClanCount)
	{
		return KingdomStabilityPolicy.GetLowClanCountRoyalDomainLoyaltyAdjustment(stabilityValue, activeClanCount);
	}

	private static int CountActiveKingdomClansForLowClanCountRule(Kingdom kingdom) => KingdomStabilityGameAdapter.CountActiveKingdomClansForLowClanCountRule(kingdom);

	private static void SyncTownRebelliousStateFromCurrentLoyalty(Town town) => KingdomStabilityGameAdapter.SyncTownRebelliousStateFromCurrentLoyalty(town);

	private void ApplyRulingClanSettlementLoyaltyAdjustmentForLowClanCountKingdom(Town town) => _kingdomStabilityGameAdapter.ApplyRulingClanSettlementLoyaltyAdjustmentForLowClanCountKingdom(town);

public static int GetKingdomStabilityRoyalDomainLoyaltyAdjustmentForTown(Town town) => KingdomStabilityGameAdapter.GetKingdomStabilityRoyalDomainLoyaltyAdjustmentForTown(town, _readCurrentKingdomStability);

	private static float GetKingdomRebellionWeeklyChance(int stabilityValue)
	{
		return KingdomStabilityPolicy.GetKingdomRebellionWeeklyChance(stabilityValue);
	}

	private static string FormatKingdomRebellionChance(float chance) => KingdomStabilityGameAdapter.FormatKingdomRebellionChance(chance);

	private static int GetKingdomStabilityWeeklyBalancingDelta(int stabilityValue)
	{
		return KingdomStabilityPolicy.GetKingdomStabilityWeeklyBalancingDelta(stabilityValue);
	}

	private static string BuildClanFortificationSummary(Clan clan) => KingdomStabilityGameAdapter.BuildClanFortificationSummary(clan);

	private static string NormalizeRebelPromptSourceText(string text, int maxLength = 1200)
	{
		return RebellionNamingRules.NormalizeSource(text, maxLength);
	}

	private string BuildHeroBackgroundForRebelNamingPrompt(Hero hero) => KingdomRebellionGameAdapter.BuildHeroBackgroundForRebelNamingPrompt(hero);

	private string BuildKingdomBackgroundForRebelNamingPrompt(Kingdom kingdom) => KingdomRebellionGameAdapter.BuildKingdomBackgroundForRebelNamingPrompt(kingdom, _weeklyEventRecords);

	private string BuildSettlementBackgroundForRebelNamingPrompt(Settlement settlement) => KingdomRebellionGameAdapter.BuildSettlementBackgroundForRebelNamingPrompt(settlement);

	private string BuildRebelSettlementSummaryForNamingPrompt(IEnumerable<Settlement> settlements)
    { return RebelNamingPromptSummaryCaptureAdapter.BuildRebelSettlementSummaryForNamingPrompt(settlements); }

	private string BuildRebelBackgroundForNamingPrompt(Kingdom oldKingdom, int weekIndex) => KingdomRebellionGameAdapter.BuildRebelBackgroundForNamingPrompt(oldKingdom, weekIndex, _weeklyEventRecords);

	private EventRecordEntry FindWeeklyReportRecordByWeek(string eventKind, string scopeKingdomId, int weekIndex) => _weeklyEventRecords.FindWeeklyReportRecordByWeek(eventKind, scopeKingdomId, weekIndex);

	private static string BuildWeeklyReportLeadInForRebelNamingPrompt(EventRecordEntry entry)
	{
		return RebellionNamingRules.WeeklyLeadIn(entry?.Title, entry?.ShortSummary, entry?.Summary);
	}

	private static string NormalizeRebelKingdomNameToken(string text, int maxLength)
	{
		return RebellionNamingRules.NormalizeName(text, maxLength);
	}

	private static string NormalizeRebelKingdomLoreText(string text)
	{
		return RebellionNamingRules.NormalizeLore(text);
	}

	private static ClanVisualSnapshot CaptureClanVisualSnapshot(Clan clan) => KingdomRebellionGameAdapter.CaptureClanVisualSnapshot(clan);

	private static void RestoreClanVisualSnapshot(Clan clan, ClanVisualSnapshot snapshot) => KingdomRebellionGameAdapter.RestoreClanVisualSnapshot(clan, snapshot);

	private static void PrepareClanVisualForRebelKingdomCreation(Clan clan, ClanVisualSnapshot snapshot) => KingdomRebellionGameAdapter.PrepareClanVisualForRebelKingdomCreation(clan, snapshot);

	private static void MarkClanVisualsDirty(Clan clan) => KingdomRebellionGameAdapter.MarkClanVisualsDirty(clan);

	private static List<uint> GetBannerPaletteColors() => KingdomRebellionGameAdapter.GetBannerPaletteColors();

	private static HashSet<uint> CollectUsedFactionPrimaryColors(Kingdom oldKingdom, Clan founderClan) => KingdomRebellionGameAdapter.CollectUsedFactionPrimaryColors(oldKingdom, founderClan);

	private static int ComputeColorDistance(uint colorA, uint colorB)
	{
		return RebellionRules.ColorDistance(colorA, colorB);
	}

	private static RebelFactionColorChoice BuildRandomUniqueRebelFactionColors(Clan clan, Kingdom oldKingdom, ClanVisualSnapshot snapshot) => KingdomRebellionGameAdapter.BuildRandomUniqueRebelFactionColors(clan, oldKingdom, snapshot);

	private static void ApplyRebelFactionColorChoiceToClan(Clan clan, RebelFactionColorChoice colorChoice) => KingdomRebellionGameAdapter.ApplyRebelFactionColorChoiceToClan(clan, colorChoice);

	private static bool IsDuplicateKingdomName(string text) => KingdomRebellionGameAdapter.IsDuplicateKingdomName(text);

	private static string BuildRebelKingdomNamingSystemPrompt() => KingdomRebellionRuntimeController.BuildRebelKingdomNamingSystemPrompt();

	private static string BuildRebelFollowerSummaryForNamingPrompt(IEnumerable<Clan> followerClans)
    { return RebelNamingPromptSummaryCaptureAdapter.BuildRebelFollowerSummaryForNamingPrompt(followerClans); }

	private string BuildRebelKingdomNamingUserPrompt(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null, IReadOnlyCollection<string> existingNames = null) => _kingdomRebellionRuntime.BuildRebelKingdomNamingUserPrompt(clan, oldKingdom, weekIndex, followerClans, existingNames);

	private void BuildRebelKingdomNamingRequest(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans, out string systemPrompt, out string userPrompt, IReadOnlyCollection<string> existingNames = null) => _kingdomRebellionRuntime.BuildRebelKingdomNamingRequest(clan, oldKingdom, weekIndex, followerClans, out systemPrompt, out userPrompt, existingNames);

	private static bool TryParseRebelKingdomNamingResponse(string rawResponse, out string formalName, out string shortName, out string encyclopediaText)
	{
		return RebellionNamingRules.TryParse(rawResponse, out formalName, out shortName, out encyclopediaText);
	}

	private const int RebelKingdomNamingTimeoutMs = RebellionNamingOwner.TimeoutMs;

	private const int RebelKingdomNamingMaxAttempts = RebellionNamingOwner.MaxAttempts;

	private static RebelKingdomNamingResult BuildFailedRebelKingdomNamingResult(string failureReason, int attemptsUsed = 0) => KingdomRebellionRuntimeController.BuildFailedRebelKingdomNamingResult(failureReason, attemptsUsed);

	private RebelKingdomNamingResult GenerateRebelKingdomNamingFromPrompts(string systemPrompt, string userPrompt, string logTarget, int maxAttempts = RebelKingdomNamingMaxAttempts, IReadOnlyCollection<string> existingNames = null) => _kingdomRebellionRuntime.GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, logTarget, maxAttempts, existingNames);

	private static string[] CaptureRebellionExistingNames() => KingdomRebellionRuntimeController.CaptureRebellionExistingNames();

	private RebelKingdomNamingResult GenerateRebelKingdomNaming(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null, int maxAttempts = RebelKingdomNamingMaxAttempts) => _kingdomRebellionRuntime.GenerateRebelKingdomNaming(clan, oldKingdom, weekIndex, followerClans, maxAttempts);

	private static RebellionClanFacts CaptureRebellionClanFacts(Clan clan, Kingdom kingdom) => KingdomRebellionGameAdapter.CaptureRebellionClanFacts(clan, kingdom);
	private bool TryValidateClanForKingdomRebellion(Clan clan, Kingdom kingdom, bool forceTrigger, out string note, out int relationToKing, out int townCount, out int castleCount, Dictionary<Clan, RebellionClanFacts> captured = null) => _kingdomRebellionGameAdapter.TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger, out note, out relationToKing, out townCount, out castleCount, captured);

	private static float ComputeKingdomRebellionCandidateScore(Clan clan, Kingdom kingdom, int relationToKing, int townCount, int castleCount) => KingdomRebellionGameAdapter.ComputeKingdomRebellionCandidateScore(clan, kingdom, relationToKing, townCount, castleCount);

	private bool TryValidateClanForRebelFollower(Clan clan, Kingdom kingdom, Clan leaderClan, bool forceTrigger, out string note, out int relationToKing, out int relationToLeader, out int townCount, out int castleCount, Dictionary<Clan, RebellionClanFacts> captured = null) => _kingdomRebellionGameAdapter.TryValidateClanForRebelFollower(clan, kingdom, leaderClan, forceTrigger, out note, out relationToKing, out relationToLeader, out townCount, out castleCount, captured);

	private static bool IsEligibleRebelFollowerByStandardRules(int relationToKing, int relationToLeader, float score)
	{
		return RebellionRules.FollowerEligible(relationToKing, relationToLeader);
	}

	private static bool IsEligibleRebelFollowerByRelativeFallback(int relationToKing, int relationToLeader, float score)
	{
		return RebellionRules.FollowerFallback();
	}

	private static float ComputeKingdomRebellionFollowerScore(Clan clan, Kingdom kingdom, Clan leaderClan, int relationToKing, int relationToLeader) => KingdomRebellionGameAdapter.ComputeKingdomRebellionFollowerScore(clan, kingdom, leaderClan, relationToKing, relationToLeader);

	private List<KingdomRebellionFollowerInfo> EvaluateKingdomRebellionFollowers(Kingdom kingdom, Clan leaderClan, bool forceTrigger, Dictionary<Clan, RebellionClanFacts> captured = null) => _kingdomRebellionGameAdapter.EvaluateKingdomRebellionFollowers(kingdom, leaderClan, forceTrigger, captured);

	private List<KingdomRebellionCandidateInfo> EvaluateKingdomRebellionCandidates(Kingdom kingdom, bool forceTrigger, Dictionary<Clan, RebellionClanFacts> captured = null) => _kingdomRebellionGameAdapter.EvaluateKingdomRebellionCandidates(kingdom, forceTrigger, captured);

	private KingdomRebellionResolutionResult ResolveKingdomRebellion(Kingdom kingdom, int weekIndex, bool executeAction, bool forceTrigger) => _kingdomRebellionRuntime.ResolveKingdomRebellion(kingdom, weekIndex, executeAction, forceTrigger);

	private bool TryExecuteKingdomRebellion(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, List<Clan> followerClans, out string message) => _kingdomRebellionRuntime.TryExecuteKingdomRebellion(clan, kingdom, weekIndex, forceTrigger, followerClans, out message);

	private static bool IsRebelKingdomNamingSuccess(RebelKingdomNamingResult namingResult) => KingdomRebellionRuntimeController.IsRebelKingdomNamingSuccess(namingResult);

	private static string BuildRebelKingdomNamingFailureExecutionMessage(RebelKingdomNamingResult namingResult) => KingdomRebellionRuntimeController.BuildRebelKingdomNamingFailureExecutionMessage(namingResult);

	private static void AppendRebelKingdomNamingResultLines(StringBuilder stringBuilder, RebelKingdomNamingResult namingResult)
		=> KingdomRebellionEditorController.AppendRebelKingdomNamingResultLines(stringBuilder, namingResult);

	private static void ReportRebelKingdomNamingFailure(RebelKingdomNamingResult namingResult)
		=> KingdomRebellionEditorController.ReportRebelKingdomNamingFailure(namingResult);

	private bool TryExecuteKingdomRebellionWithNaming(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, int relationToKing, int townCount, int castleCount, RebelKingdomNamingResult rebelKingdomNamingResult, List<Clan> followerClans, out string message) => _kingdomRebellionRuntime.TryExecuteKingdomRebellionWithNaming(clan, kingdom, weekIndex, forceTrigger, relationToKing, townCount, castleCount, rebelKingdomNamingResult, followerClans, out message);

	private void MarkModCreatedRebelKingdom(Kingdom kingdom) => _kingdomRebellionGameAdapter.MarkModCreatedRebelKingdom(kingdom);

	private bool IsKnownOrLegacyModCreatedRebelKingdom(Kingdom kingdom) => _kingdomRebellionGameAdapter.IsKnownOrLegacyModCreatedRebelKingdom(kingdom);

	private static Clan FindKingdomClanMatchingBanner(Kingdom kingdom, Banner banner, Clan excludedClan) => KingdomRebellionGameAdapter.FindKingdomClanMatchingBanner(kingdom, banner, excludedClan);

	private static HashSet<string> BuildEventSourceMaterialStableKeySet(List<EventSourceMaterialEntry> source) => CampaignMaterialRecordOwner.BuildEventSourceMaterialStableKeySet(source);

	private HashSet<string> BuildEventSourceMaterialStableKeySet() => _campaignMaterialRecords.BuildEventSourceMaterialStableKeySet();

	private static void MarkKingdomBannerVisualsDirty(Kingdom kingdom) => KingdomRebellionGameAdapter.MarkKingdomBannerVisualsDirty(kingdom);

	private bool TrySyncModCreatedRebelKingdomBannerToRulingClan(Kingdom kingdom, Clan eventRulingClan, string reason) => _kingdomRebellionGameAdapter.TrySyncModCreatedRebelKingdomBannerToRulingClan(kingdom, eventRulingClan, reason);

	private void SyncModCreatedRebelKingdomBannersOnGameLoad() => _kingdomRebellionGameAdapter.SyncModCreatedRebelKingdomBannersOnGameLoad();

	private bool TryDiscontinueLandlessKingdom(Kingdom kingdom, string reason, bool requireKnownModRebelKingdom, bool allowPlayerKingdom) => _kingdomRebellionGameAdapter.TryDiscontinueLandlessKingdom(kingdom, reason, requireKnownModRebelKingdom, allowPlayerKingdom);

	private bool TryDiscontinueLandlessModRebelKingdom(Kingdom kingdom, string reason) => _kingdomRebellionGameAdapter.TryDiscontinueLandlessModRebelKingdom(kingdom, reason);

	private void TryDiscontinueLandlessModRebelKingdoms(string reason) => _kingdomRebellionGameAdapter.TryDiscontinueLandlessModRebelKingdoms(reason);

	private static void FinalizeMapEventsForKingdomDiscontinuation(Clan clan) => KingdomRebellionGameAdapter.FinalizeMapEventsForKingdomDiscontinuation(clan);

	private void CleanupModCreatedRebelKingdomState(string kingdomId) => _kingdomRebellionGameAdapter.CleanupModCreatedRebelKingdomState(kingdomId);

	private void TryProcessWeeklyKingdomRebellions(int weekIndex) => _kingdomRebellionRuntime.TryProcessWeeklyKingdomRebellions(weekIndex);

	private bool ProcessWeeklyKingdomRebellionsSlice(int weekIndex) => _kingdomRebellionRuntime.ProcessWeeklyKingdomRebellionsSlice(weekIndex);

	private bool CompleteWeeklyKingdomRebellionMaintenance(int weekIndex) => _kingdomRebellionRuntime.CompleteWeeklyKingdomRebellionMaintenance(weekIndex);

	private void ResetPendingWeeklyKingdomRebellionMaintenance() => _kingdomRebellionRuntime.ResetPendingWeeklyKingdomRebellionMaintenance();

	public static void QueueCivilWarSplitForExternal(Kingdom kingdom, Clan clan)
		{
			try
			{
				MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
				if (behavior == null || kingdom == null || clan == null) return;
				int week = Math.Max(1, GetCurrentGameDayIndexSafe() / 7);
				var result = new KingdomRebellionResolutionResult
				{
					Kingdom = kingdom,
					WeekIndex = week,
					Forced = true,
					SelectedClan = clan,
					PassedChanceGate = true,
					StabilityValue = behavior.GetKingdomStabilityValue(kingdom)
				};
				behavior.QueueAutomaticKingdomRebellion(result);
				behavior.TryStartNextAutomaticKingdomRebellionAsync();
			}
			catch (Exception ex)
			{
				Logger.Log("KingdomCivilWar", "[ERROR] split queue failed: " + ex.Message);
			}
		}

		public static void QueueCivilWarRebellionForExternal(Kingdom kingdom, Clan leader, List<Clan> followers, string factionId, bool startNow)
		{
			try
			{
				MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
				if (behavior == null || kingdom == null || leader == null || string.IsNullOrWhiteSpace(factionId)) return;
				KingdomRebellionResolutionResult result = new KingdomRebellionResolutionResult
				{
					Kingdom = kingdom,
					WeekIndex = Math.Max(1, GetCurrentGameDayIndexSafe() / 7),
					Forced = true,
					SelectedClan = leader,
					SelectedFollowerClans = followers ?? new List<Clan>(),
					PassedChanceGate = true,
					StabilityValue = behavior.GetKingdomStabilityValue(kingdom)
				};
				behavior.QueueAutomaticKingdomRebellion(result, factionId);
				if (startNow) behavior.TryStartNextAutomaticKingdomRebellionAsync();
			}
			catch (Exception ex)
			{
				Logger.Log("KingdomCivilWar", "[ERROR] civil war rebellion queue failed: " + ex.Message);
			}
		}

		private void QueueAutomaticKingdomRebellion(KingdomRebellionResolutionResult result, string civilWarFactionId = null) => _kingdomRebellionRuntime.QueueAutomaticKingdomRebellion(result, civilWarFactionId);

	private void CancelPendingAutomaticKingdomRebellions(string reason) => _kingdomRebellionRuntime.CancelPendingAutomaticKingdomRebellions(reason);

	private void TryStartNextAutomaticKingdomRebellionAsync() => _kingdomRebellionRuntime.TryStartNextAutomaticKingdomRebellionAsync();

	private void ProcessPendingAutomaticKingdomRebellionResult() => _kingdomRebellionRuntime.ProcessPendingAutomaticKingdomRebellionResult();

	// Civil-war requests: the owner may have dissolved the faction while this entry waited in the queue.
	private static bool IsStaleCivilWarRebellion(PendingAutomaticKingdomRebellionContext context) => KingdomRebellionRuntimeController.IsStaleCivilWarRebellion(context);

	private static void NotifyCivilWarRebellionFailed(PendingAutomaticKingdomRebellionContext context, string reason) => KingdomRebellionRuntimeController.NotifyCivilWarRebellionFailed(context, reason);

	private void ShowAutomaticKingdomRebellionCompletionPopup(PendingAutomaticKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool success, string executionMessage)
		=> KingdomRebellionEditor.ShowAutomaticKingdomRebellionCompletionPopup(context, kingdom, clan, followerClans, success, executionMessage);

	private void ShowAutomaticKingdomRebellionNamingFailurePopup(PendingAutomaticKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool afterApiRepair)
		=> KingdomRebellionEditor.ShowAutomaticKingdomRebellionNamingFailurePopup(context, kingdom, clan, followerClans, afterApiRepair);

	private void RetryAutomaticKingdomRebellionNamingAsync(PendingAutomaticKingdomRebellionContext context) => _kingdomRebellionRuntime.RetryAutomaticKingdomRebellionNamingAsync(context);

	private void ContinueAutomaticKingdomRebellionFlow() => _kingdomRebellionRuntime.ContinueAutomaticKingdomRebellionFlow();

	private static Settlement ResolveGatheringPointSettlement(IMapPoint gatheringPoint, MobileParty fallbackParty = null) => CampaignBattleRecordCaptureAdapter.ResolveGatheringPointSettlement(gatheringPoint, fallbackParty);

	private static Settlement ResolveSettlementForPartyBase(PartyBase party) => CampaignBattleRecordCaptureAdapter.ResolveSettlementForPartyBase(party);

	private static string GetLocationLabelForPartyBase(PartyBase party) => CampaignBattleRecordCaptureAdapter.GetLocationLabelForPartyBase(party);

	private static string ResolveGatheringPointLabel(IMapPoint gatheringPoint, MobileParty fallbackParty = null) => CampaignBattleRecordCaptureAdapter.ResolveGatheringPointLabel(gatheringPoint, fallbackParty);

		private static NpcActionFacts CreateNpcActionFacts(string actionKind, Hero actorHero = null) => CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts(actionKind, actorHero);

		private static void ApplyActorFacts(NpcActionFacts facts, Hero hero) => CampaignCharacterRecordCaptureAdapter.ApplyActorFacts(facts, hero);

		private static void ApplyTargetFacts(NpcActionFacts facts, Hero hero) => CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(facts, hero);

		private static void ApplySettlementFacts(NpcActionFacts facts, Settlement settlement, Hero currentOwnerOverride = null, Hero previousOwnerOverride = null, string locationText = null) => CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(facts, settlement, currentOwnerOverride, previousOwnerOverride, locationText);

		private static void AddRelatedFactionFacts(NpcActionFacts facts, IFaction faction) => CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(facts, faction);

		private static void AddUniqueId(List<string> list, string id) => CampaignCharacterRecordCaptureAdapter.AddUniqueId(list, id);

		private static void CopyFactIds(List<string> source, List<string> destination) => CampaignCharacterRecordCaptureAdapter.CopyFactIds(source, destination);

	public static List<string> GetRecentKingdomEventFactsForExternal(string kingdomId, int limit)
 { try { return (Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?._campaignMaterialRecords.GetRecentKingdomEventFacts(kingdomId,limit) ?? new List<string>(); } catch (Exception ex) { Logger.Log("KingdomCivilWar", "[WARN] recent event facts failed: " + ex.Message); return new List<string>(); } }

		private static string LimitEventFact(string text) => CampaignMaterialRecordOwner.LimitEventFact(text);

		public static void RecordEventSourceMaterialForExternal(string materialKind, string label, string snapshotText, string stableKey, string kingdomId, bool includeInWorld, bool includeInKingdom)
		{
			try
			{
				(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordEventSourceMaterial(materialKind, label, snapshotText, stableKey, kingdomId, "", includeInWorld, includeInKingdom);
			}
			catch (Exception ex)
			{
				Logger.Log("EventMaterial", "[ERROR] external material failed: " + ex.Message);
			}
		}

	private static bool IsPlayerWeeklySourceMaterial(string materialKind, string actorHeroId, string stableKey) => CampaignCharacterRecordCaptureAdapter.IsPlayerWeeklySourceMaterial(materialKind, actorHeroId, stableKey);

	public static void RecordNobleGatheringWeeklyMaterialForExternal(string stableKey, string label, string snapshotText, string kingdomId, string settlementId, string actorHeroId, bool includeInWorld)
	{
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			behavior?.RecordEventSourceMaterial(
				"noble_gathering",
				label,
				snapshotText,
				stableKey,
				kingdomId,
				settlementId,
				includeInWorld,
				includeInKingdom: true,
				actorHeroId: actorHeroId,
				actorKingdomId: kingdomId);
			Logger.Log("EventWeeklyReport", "[NobleGathering] source_material_recorded key=" + (stableKey ?? "") + " kingdom=" + (kingdomId ?? "") + " world=" + includeInWorld);
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[NobleGathering][WARN] record weekly material failed: " + ex.Message);
		}
	}

	public static void RecordWorldDiplomacyWeeklyMaterialForExternal(string stableKey, string label, string snapshotText, string kingdomId, string actorHeroId, string actorKingdomId, bool includeInWorld, int day, string gameDate)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordEventSourceMaterial(
				"world_diplomacy",
				label,
				snapshotText,
				stableKey,
				kingdomId,
				"",
				includeInWorld,
				includeInKingdom: true,
				actorHeroId: actorHeroId,
				actorKingdomId: actorKingdomId,
				dayOverride: day,
				gameDateOverride: gameDate);
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[WorldDiplomacy][WARN] record weekly material failed: " + ex.Message);
		}
	}

	public static void RecordPolicySystemWeeklyMaterialForExternal(string eventKind, string label, string snapshot, string stableKey, string targetKingdomId, bool includeInWorld, string actorHeroId, string actorKingdomId, int day, string gameDate)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordEventSourceMaterial(
				eventKind,
				label,
				snapshot,
				stableKey,
				targetKingdomId,
				"",
				includeInWorld,
				includeInKingdom: true,
				actorHeroId: actorHeroId,
				actorKingdomId: actorKingdomId,
				dayOverride: day,
				gameDateOverride: gameDate);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Weekly", "bridge-failed", ex.Message);
		}
	}

	public static void RecordNpcPublicFeedbackEventMaterialForExternal(string stableKey, string kingdomId, string kingdomName, string npcHeroId, string npcName, string policyName, string feedbackSummary, int day = -1, string gameDate = "", bool includeInWorld = false)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordNpcPublicFeedbackEventMaterialInternal(stableKey, "", "", kingdomId, kingdomName, npcHeroId, npcName, policyName, feedbackSummary, day, gameDate, includeInWorld);
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[NpcPublicFeedback][WARN] record event material failed: " + ex.Message);
		}
	}

	public static void RecordNpcPublicFeedbackEventMaterialForExternal(string eventId, string title, string summary, string detailText, string kingdomId, string kingdomName, string actorHeroId, string actorHeroName, string policyId, string policyName, int day, string gameDate, bool includeInWorld)
	{
		try
		{
			string text = !string.IsNullOrWhiteSpace(detailText) ? detailText : (!string.IsNullOrWhiteSpace(summary) ? summary : title);
			(Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordNpcPublicFeedbackEventMaterialInternal(eventId, title, policyId, kingdomId, kingdomName, actorHeroId, actorHeroName, policyName, text, day, gameDate, includeInWorld);
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[NpcPublicFeedback][WARN] record event material failed: " + ex.Message);
		}
	}

	private void RecordNpcPublicFeedbackEventMaterialInternal(string stableKey, string eventTitle, string policyId, string kingdomId, string kingdomName, string npcHeroId, string npcName, string policyName, string feedbackSummary, int day, string gameDate, bool includeInWorld) => _campaignCharacterRecordCapture.RecordNpcPublicFeedbackEventMaterialInternal(stableKey, eventTitle, policyId, kingdomId, kingdomName, npcHeroId, npcName, policyName, feedbackSummary, day, gameDate, includeInWorld);

	public static void RecordPlayerKingdomRenameWeeklyMaterialForExternal(string oldKingdomName, string newKingdomName, string oldInformalName, string newInformalName, string kingdomId, int day, string gameDate, string stableKey)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordPlayerKingdomRenameWeeklyMaterialInternal(oldKingdomName, newKingdomName, oldInformalName, newInformalName, kingdomId, day, gameDate, stableKey);
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[PlayerKingdomRename][WARN] record weekly material failed: " + ex.Message);
		}
	}

	private void RecordPlayerKingdomRenameWeeklyMaterialInternal(string oldKingdomName, string newKingdomName, string oldInformalName, string newInformalName, string kingdomId, int day, string gameDate, string stableKey) => _campaignCharacterRecordCapture.RecordPlayerKingdomRenameWeeklyMaterialInternal(oldKingdomName, newKingdomName, oldInformalName, newInformalName, kingdomId, day, gameDate, stableKey);

		private static string LimitCustomPolicyWeeklyMaterialText(string text, int maxChars) => PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(text, maxChars);

	public static void RecordPlayerSceneConflictWeeklyMaterialForExternal(string text, string stableKey, int day, string gameDate, string settlementId, string settlementName, string locationText)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordPlayerSceneConflictWeeklyMaterialInternal(text, stableKey, day, gameDate, settlementId, settlementName, locationText);
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[PlayerSceneConflict][WARN] record weekly material failed: " + ex.Message);
		}
	}

	private void RecordPlayerSceneConflictWeeklyMaterialInternal(string text, string stableKey, int day, string gameDate, string settlementId, string settlementName, string locationText) => _campaignCharacterRecordCapture.RecordPlayerSceneConflictWeeklyMaterialInternal(text, stableKey, day, gameDate, settlementId, settlementName, locationText);

	public static void MarkWeeklyMemoryMaterialTriggerForExternal(Hero targetHero, string nonHeroMemoryId, string npcName, string normalizedTagText, int sceneSessionId = -1, int nativeDialogueSessionId = -1, int targetAgentIndex = -1, List<RewardSystemBehavior.RewardItemInfo> rewardOptions = null, List<PartyTransferPromptEntry> partyTransferTroopOptions = null, List<PartyTransferPromptEntry> partyTransferPrisonerOptions = null, List<SettlementTransferPromptEntry> settlementTransferNpcOptions = null, List<SettlementTransferPromptEntry> settlementTransferPlayerOptions = null, bool suppressImplicitDialogueSession = false)
	{
		if (!TWParallel.IsMainThread())
		{
			var rewards = CopyMemoryRewardOptions(rewardOptions);
			var troops = CopyMemoryPartyOptions(partyTransferTroopOptions);
			var prisoners = CopyMemoryPartyOptions(partyTransferPrisonerOptions);
			var settlements = CopyMemorySettlementOptions(settlementTransferNpcOptions);
			DeferMemorySourceWriteIfNeeded(owner => owner.MarkWeeklyMemoryMaterialTriggerInternal(targetHero, nonHeroMemoryId, npcName, normalizedTagText, sceneSessionId, nativeDialogueSessionId, targetAgentIndex, rewards, troops, prisoners, settlements, suppressImplicitDialogueSession), nameof(MarkWeeklyMemoryMaterialTriggerForExternal));
			return;
		}

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.MarkWeeklyMemoryMaterialTriggerInternal(targetHero, nonHeroMemoryId, npcName, normalizedTagText, sceneSessionId, nativeDialogueSessionId, targetAgentIndex, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, suppressImplicitDialogueSession);
		}
		catch
		{
		}
	}

	internal static void MarkWeeklyMemoryMaterialTriggerWithAllSnapshotsForExternal(Hero targetHero, string nonHeroMemoryId, string npcName, string normalizedTagText, int sceneSessionId = -1, int nativeDialogueSessionId = -1, int targetAgentIndex = -1, List<RewardSystemBehavior.RewardItemInfo> rewardOptions = null, List<PartyTransferPromptEntry> partyTransferTroopOptions = null, List<PartyTransferPromptEntry> partyTransferPrisonerOptions = null, List<SettlementTransferPromptEntry> settlementTransferNpcOptions = null, bool suppressImplicitDialogueSession = false, List<PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null)
	{
		if (!TWParallel.IsMainThread())
		{
			var rewards = CopyMemoryRewardOptions(rewardOptions);
			var troops = CopyMemoryPartyOptions(partyTransferTroopOptions);
			var prisoners = CopyMemoryPartyOptions(partyTransferPrisonerOptions);
			var settlements = CopyMemorySettlementOptions(settlementTransferNpcOptions);
			var allTroops = CopyMemoryPartyOptions(partyTransferAllTroopOptions);
			var allPrisoners = CopyMemoryPartyOptions(partyTransferAllPrisonerOptions);
			DeferMemorySourceWriteIfNeeded(owner => owner.MarkWeeklyMemoryMaterialTriggerInternal(targetHero, nonHeroMemoryId, npcName, normalizedTagText, sceneSessionId, nativeDialogueSessionId, targetAgentIndex, rewards, troops, prisoners, settlements, suppressImplicitDialogueSession, allTroops, allPrisoners), nameof(MarkWeeklyMemoryMaterialTriggerWithAllSnapshotsForExternal));
			return;
		}

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.MarkWeeklyMemoryMaterialTriggerInternal(targetHero, nonHeroMemoryId, npcName, normalizedTagText, sceneSessionId, nativeDialogueSessionId, targetAgentIndex, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, suppressImplicitDialogueSession, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions);
		}
		catch
		{
		}
	}

	private void TrackTownWeeklyMaterialChanges(Town town) => _weeklyTownStatCapture.TrackTownWeeklyMaterialChanges(town);

	private int GetCurrentWeeklyIndexSafe() => WeeklyTownStatCaptureAdapter.GetCurrentWeeklyIndexSafe();

	private static TownStatSnapshot CaptureTownSnapshot(Town town) => WeeklyTownStatCaptureAdapter.CaptureTownSnapshot(town);

	private const int TownStatReasonMaxStats = WeeklyTownStatCaptureAdapter.TownStatReasonMaxStats;
	private const int TownStatReasonMaxLinesPerStat = WeeklyTownStatCaptureAdapter.TownStatReasonMaxLinesPerStat;
	private const int TownStatReasonMaxLineLength = WeeklyTownStatCaptureAdapter.TownStatReasonMaxLineLength;

	private static string BuildTownStatChangeReasonText(Town town, HashSet<string> changedLabels) => WeeklyTownStatCaptureAdapter.BuildTownStatChangeReasonText(town, changedLabels);

	private static void AddTownStatModelReasonPart(List<string> parts, string label, Func<ExplainedNumber> calculator) => WeeklyTownStatCaptureAdapter.AddTownStatModelReasonPart(parts, label, calculator);

	private static string BuildTownStatExplainedNumberReason(string label, ExplainedNumber explainedNumber) => WeeklyTownStatCaptureAdapter.BuildTownStatExplainedNumberReason(label, explainedNumber);

	private static string CondenseTownStatReasonLineName(string text) => WeeklyTownStatCaptureAdapter.CondenseTownStatReasonLineName(text);

	private static void AppendTownChangeLine(List<string> lines, string label, float oldValue, float newValue, float threshold) => WeeklyTownStatCaptureAdapter.AppendTownChangeLine(lines, label, oldValue, newValue, threshold);

	private static void AppendTownChangeLine(List<string> lines, string label, int oldValue, int newValue, int threshold) => WeeklyTownStatCaptureAdapter.AppendTownChangeLine(lines, label, oldValue, newValue, threshold);

	private static bool AppendTownChangeRangeLine(List<string> lines, string label, float oldValue, float newValue, float threshold) => WeeklyTownStatCaptureAdapter.AppendTownChangeRangeLine(lines, label, oldValue, newValue, threshold);

	private static bool AppendTownChangeRangeLine(List<string> lines, string label, int oldValue, int newValue, int threshold) => WeeklyTownStatCaptureAdapter.AppendTownChangeRangeLine(lines, label, oldValue, newValue, threshold);

	private static string BuildTownChangeQualitativeLine(string label, float delta, float threshold) => WeeklyTownStatCaptureAdapter.BuildTownChangeQualitativeLine(label, delta, threshold);


	private static List<NpcActionEntry> SanitizeNpcActionEntries(List<NpcActionEntry> source, bool keepOnlyRecentWindow)
	{
		return NpcActionLedger.SanitizeNpcActionEntries(source, keepOnlyRecentWindow, GetCurrentGameDayIndexSafe());
	}

	private static bool ShouldSuppressNpcMajorAction(string actionKind, string stableKey, string text)
	{
		return NpcActionLedger.ShouldSuppressNpcMajorAction(actionKind, stableKey, text);
	}

	private void RecordNpcMajorAction(Hero hero, string text, string stableKey, NpcActionFacts facts = null, bool allowNonLordHero = false) => _campaignCharacterRecordCapture.RecordNpcMajorAction(hero, text, stableKey, facts, allowNonLordHero);

	private void RecordNpcRecentAction(Hero hero, string text, string stableKey, bool dedupeAcrossWindow = false, NpcActionFacts facts = null, bool allowNonLordHero = false) => _campaignCharacterRecordCapture.RecordNpcRecentAction(hero, text, stableKey, dedupeAcrossWindow, facts, allowNonLordHero);


	private static string ResolveHeroCultureId(string heroId) => CampaignCharacterRecordCaptureAdapter.ResolveHeroCultureId(heroId);

	private static Settlement ResolveSettlementById(string settlementId) => CampaignCharacterRecordCaptureAdapter.ResolveSettlementById(settlementId);


	private bool HasRecentNpcActionStableKeyWithinWindow(Hero hero, string stableKey, int currentDay) => _campaignCharacterRecordCapture.HasRecentNpcActionStableKeyWithinWindow(hero, stableKey, currentDay);

	private static int CompareNpcActionTimeline(NpcActionEntry left, NpcActionEntry right)
	{
		if (ReferenceEquals(left, right)) return 0;
		if (left == null) return -1;
		if (right == null) return 1;
		return NpcActionLedger.CompareTimeline(left.Day, left.Sequence, left.Order, left.GameDate, right.Day, right.Sequence, right.Order, right.GameDate);
	}

	private static string GetArmyDisplayName(Army army) => MemoryEntityIdentityBannerlordAdapter.GetArmyDisplayName(army);

	private static Settlement ResolveSiegeSettlement(SiegeEvent siegeEvent) => CampaignBattleRecordCaptureAdapter.ResolveSiegeSettlement(siegeEvent);

	private static IEnumerable<Hero> GetHeroesFromSiegeEventSide(SiegeEvent siegeEvent, BattleSideEnum side) => CampaignBattleRecordCaptureAdapter.GetHeroesFromSiegeEventSide(siegeEvent, side);

	private static string GetMapEventLocationLabel(MapEvent mapEvent) => CampaignBattleRecordCaptureAdapter.GetMapEventLocationLabel(mapEvent);

	private static string GetPrimaryOtherSideLabel(MapEventSide side) => CampaignBattleRecordCaptureAdapter.GetPrimaryOtherSideLabel(side);

	private static string BuildMapEventSideNarrativeLabel(MapEventSide side) => CampaignBattleRecordCaptureAdapter.BuildMapEventSideNarrativeLabel(side);

	private static string BuildMapEventNonLordPartyListText(MapEventSide side, int maxCount = 5, bool includeCounts = true) => CampaignBattleRecordCaptureAdapter.BuildMapEventNonLordPartyListText(side, maxCount, includeCounts);

	private static string BuildMapEventNonLordPartyDisplayText(PartyBase partyBase, MapEventParty party, bool includeCounts) => CampaignBattleRecordCaptureAdapter.BuildMapEventNonLordPartyDisplayText(partyBase, party, includeCounts);

	private static string GetPartyBaseDisplayName(PartyBase partyBase) => CampaignBattleRecordCaptureAdapter.GetPartyBaseDisplayName(partyBase);

	private static string GetMapEventNonLordPartyTypeLabel(PartyBase partyBase) => CampaignBattleRecordCaptureAdapter.GetMapEventNonLordPartyTypeLabel(partyBase);

	private static int GetMapEventTroopCount(MapEvent mapEvent) => CampaignBattleRecordCaptureAdapter.GetMapEventTroopCount(mapEvent);

	private static int CountTrackedLordParties(MapEventSide side) => CampaignBattleRecordCaptureAdapter.CountTrackedLordParties(side);

	private static int GetPartyBaseTroopCount(PartyBase party) => CampaignBattleRecordCaptureAdapter.GetPartyBaseTroopCount(party);

	private static int GetSiegeEventTroopCount(SiegeEvent siegeEvent) => CampaignBattleRecordCaptureAdapter.GetSiegeEventTroopCount(siegeEvent);

	private static bool ShouldRecordMajorBattleAction(int troopCount) => CampaignBattleRecordCaptureAdapter.ShouldRecordMajorBattleAction(troopCount);

	private static int GetTroopRosterTotalManCount(TroopRoster roster) => CampaignBattleRecordCaptureAdapter.GetTroopRosterTotalManCount(roster);

		private static string GetFactionDisplayName(IFaction faction, string fallback = "某势力") => MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(faction, fallback);

		private static string GetHeroFactionDisplayName(Hero hero, IFaction fallbackFaction = null) => MemoryEntityIdentityBannerlordAdapter.GetHeroFactionDisplayName(hero, fallbackFaction);

	private static string BuildBattleHeroDisplayName(Hero hero, bool isHighlighted, string highlightTag) => CampaignBattleRecordCaptureAdapter.BuildBattleHeroDisplayName(hero, isHighlighted, highlightTag);

	private static string StripBattlePlayerMarker(string text) => CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker(text);

	private static string BuildTrackedHeroListText(IEnumerable<Hero> heroes, Hero highlightedHero, string highlightTag, int maxCount = 5) => CampaignBattleRecordCaptureAdapter.BuildTrackedHeroListText(heroes, highlightedHero, highlightTag, maxCount);

	private static string BuildTrackedHeroListText(IEnumerable<Hero> heroes, int maxCount = 5) => CampaignBattleRecordCaptureAdapter.BuildTrackedHeroListText(heroes, maxCount);

	private static string BuildTrackedHeroListText(MapEventSide side, int maxCount = 5) => CampaignBattleRecordCaptureAdapter.BuildTrackedHeroListText(side, maxCount);

	private static string BuildTrackedHeroListText(SiegeEvent siegeEvent, BattleSideEnum side, int maxCount = 5) => CampaignBattleRecordCaptureAdapter.BuildTrackedHeroListText(siegeEvent, side, maxCount);

	private static string BuildMapEventCasualtyText(MapEventSide side) => CampaignBattleRecordCaptureAdapter.BuildMapEventCasualtyText(side);

	private static int GetMapEventSideCasualtyCount(MapEventSide side) => CampaignBattleRecordCaptureAdapter.GetMapEventSideCasualtyCount(side);

	private static int GetMapEventPartyCommittedTroopCount(MapEventParty party) => CampaignBattleRecordCaptureAdapter.GetMapEventPartyCommittedTroopCount(party);

	private static int GetMapEventSideCommittedTroopCount(MapEventSide side) => CampaignBattleRecordCaptureAdapter.GetMapEventSideCommittedTroopCount(side);

	private static string BuildMapEventCommittedTroopText(MapEventSide side) => CampaignBattleRecordCaptureAdapter.BuildMapEventCommittedTroopText(side);

	private static string BuildMapEventStandoutText(MapEventSide side, MapEventSide oppositeSide = null) => CampaignBattleRecordCaptureAdapter.BuildMapEventStandoutText(side, oppositeSide);

	private static string BuildMapEventAftermathText(MapEvent mapEvent, MapEventSide side, bool won, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildMapEventAftermathText(mapEvent, side, won, locationLabel);

	private static string BuildArmyCommandClause(MapEventSide side, Hero actorHero) => CampaignBattleRecordCaptureAdapter.BuildArmyCommandClause(side, actorHero);

	private static string BuildMapEventActorRoleClause(Hero actorHero, MapEventSide side) => CampaignBattleRecordCaptureAdapter.BuildMapEventActorRoleClause(actorHero, side);

	private static string BuildMapEventNarrative(MapEvent mapEvent, MapEventSide side, Hero actorHero, bool won, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildMapEventNarrative(mapEvent, side, actorHero, won, locationLabel);

	private static string BuildMapEventStableKey(MapEvent mapEvent, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildMapEventStableKey(mapEvent, locationLabel);

	private static string BuildRoutineBanditDefeatStableKey(MapEvent mapEvent, MapEventSide defeatedSide, string locationLabel) => CampaignBattleRecordCaptureAdapter.BuildRoutineBanditDefeatStableKey(mapEvent, defeatedSide, locationLabel);

		private static string NormalizeWeeklyPromptKeyPart(string value) => MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(value);

	private static void ApplyMapEventOppositeSideFacts(NpcActionFacts facts, MapEventSide oppositeSide) => CampaignBattleRecordCaptureAdapter.ApplyMapEventOppositeSideFacts(facts, oppositeSide);

	private static string BuildPlayerAddressedInput(Hero hero, string playerText)
	{
		return BuildPlayerAddressedInputForName(hero?.Name?.ToString(), playerText, hero);
	}

	private static string BuildPlayerAddressedInputForName(string npcName, string playerText, Hero observer = null, string actualTargetName = null)
    {
        return PersonaIdentityPromptCaptureAdapter.BuildPlayerAddressedInputForName(npcName, playerText, observer, actualTargetName);
    }

	private static string MergeUserHistoryAndInput(string historyContext, string addressedInput)
    { return PersonaIdentityPromptCaptureAdapter.MergeUserHistoryAndInput(historyContext, addressedInput); }

	private static string JoinPromptSections(params string[] sections)
    { return PersonaIdentityPromptCaptureAdapter.JoinPromptSections(sections); }

	private static string BuildPlayerCustomPromptRuleBlock()
    { return PersonaIdentityPromptCaptureAdapter.BuildPlayerCustomPromptRuleBlock(); }

	private static string AppendPlayerCustomPromptRuleToSystemPrompt(string systemPrompt)
	{
		return JoinPromptSections(BuildPlayerCustomPromptRuleBlock(), systemPrompt);
	}

	public static string AppendPlayerCustomPromptRuleToSystemPromptForExternal(string systemPrompt)
	{
		return AppendPlayerCustomPromptRuleToSystemPrompt(systemPrompt);
	}

	private const string SceneHistorySessionMarkerPrefix = DialogueHistoryLedger.SceneSessionMarkerPrefix;

	private static bool IsActiveSceneSessionHistoryLine(string line) => MemoryHistoryCommitBannerlordAdapter.IsActiveSceneSessionHistoryLine(line);

	private static string BuildPlayerPublicDisplayNameForPrompt()
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt();
	}

	private static string BuildPlayerPublicDisplayNameForPrompt(Hero observer)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(observer);
	}

	private static string BuildPlayerPublicDisplayNameForPrompt(string observerKey, string cultureId)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(observerKey, cultureId);
	}

	private static string BuildPlayerPublicDisplayNameForPrompt(Hero observer, CharacterObject observerCharacter, int targetAgentIndex = -1)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(observer, observerCharacter, targetAgentIndex);
	}

	private static bool TryBuildPlayerPublicDisplayNameForPrompt(out string displayName, out bool isCompleteIdentity)
	{
		return PersonaIdentityPromptCaptureAdapter.TryBuildPlayerPublicDisplayNameForPrompt(out displayName, out isCompleteIdentity);
	}

	private static bool TryBuildPlayerPublicDisplayNameForPrompt(Hero observer, out string displayName, out bool isCompleteIdentity)
	{
		return PersonaIdentityPromptCaptureAdapter.TryBuildPlayerPublicDisplayNameForPrompt(observer, out displayName, out isCompleteIdentity);
	}

	private static bool TryBuildPlayerPublicDisplayNameForPrompt(string observerKey, string cultureId, out string displayName, out bool isCompleteIdentity)
	{
		return PersonaIdentityPromptCaptureAdapter.TryBuildPlayerPublicDisplayNameForPrompt(observerKey, cultureId, out displayName, out isCompleteIdentity);
	}

	private static Hero ResolveCurrentPlayerIdentityObserverForPrompt()
	{
		return PersonaIdentityPromptCaptureAdapter.ResolveCurrentPlayerIdentityObserverForPrompt();
	}

	public static string BuildPlayerPublicDisplayNameForExternal()
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForExternal();
	}

	public static string BuildPlayerPublicDisplayNameForExternal(Hero observer)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForExternal(observer);
	}

	public static string BuildPlayerPublicDisplayNameForExternal(string observerKey, string cultureId)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForExternal(observerKey, cultureId);
	}

	private static bool DoesPlayerNotorietyObserverKnowPlayer(Hero observerHero, CharacterObject observerCharacter = null, int targetAgentIndex = -1)
	{
		return PersonaIdentityPromptCaptureAdapter.DoesPlayerNotorietyObserverKnowPlayer(observerHero, observerCharacter, targetAgentIndex);
	}

	private static string NormalizePlayerHistoryLineForPrompt(string line, string targetDisplayName, bool addressToYou = true)
    { return PersonaIdentityPromptCaptureAdapter.NormalizePlayerHistoryLineForPrompt(line, targetDisplayName, addressToYou); }

	private static bool TryStripPlayerSpeechPrefix(string line, out string stripped)
	{
        return MemoryRecallInputCaptureAdapter.TryStripPlayerSpeechPrefix(line, out stripped);
    }

	private static string BuildSiegeStartNarrative(Settlement settlement, bool isAttacker, SiegeEvent siegeEvent)
    { return PersonaIdentityPromptCaptureAdapter.BuildSiegeStartNarrative(settlement, isAttacker, siegeEvent); }

	private void TrackNpcActionsFromMapEvent(MapEvent mapEvent) => _campaignBattleRecordCapture.TrackNpcActionsFromMapEvent(mapEvent);

	private static bool ShouldSkipRoutineBanditDefeatMapEvent(MapEvent mapEvent) => CampaignBattleRecordCaptureAdapter.ShouldSkipRoutineBanditDefeatMapEvent(mapEvent);

	private static MapEventSide GetMapEventDefeatedSide(MapEvent mapEvent) => CampaignBattleRecordCaptureAdapter.GetMapEventDefeatedSide(mapEvent);

	private static bool IsRoutineBanditMapEventSide(MapEventSide side) => CampaignBattleRecordCaptureAdapter.IsRoutineBanditMapEventSide(side);

	private static bool IsBanditOrOutlawPartyBase(PartyBase party) => CampaignBattleRecordCaptureAdapter.IsBanditOrOutlawPartyBase(party);

	private static bool IsBanditOrMonsterMapEvent(MapEvent mapEvent) => CampaignBattleRecordCaptureAdapter.IsBanditOrMonsterMapEvent(mapEvent);

	private static bool IsBanditOrMonsterMapEventSide(MapEventSide side) => CampaignBattleRecordCaptureAdapter.IsBanditOrMonsterMapEventSide(side);

	private void TrackNpcActionsFromMapEventSide(MapEvent mapEvent, MapEventSide side, bool won, bool isMajor, string locationLabel, string mapEventStableKey, bool allowNonLordHero = false) => _campaignBattleRecordCapture.TrackNpcActionsFromMapEventSide(mapEvent, side, won, isMajor, locationLabel, mapEventStableKey, allowNonLordHero);

	private static string GetNearestSettlementNameForParty(MobileParty party) => CampaignBattleRecordCaptureAdapter.GetNearestSettlementNameForParty(party);

	private static string BuildRecentPartyBehaviorText(MobileParty party) => CampaignBattleRecordCaptureAdapter.BuildRecentPartyBehaviorText(party);

	private static string BuildRecentPartyBehaviorText(MobileParty party, string locationText, string armyDisplayName) => CampaignBattleRecordCaptureAdapter.BuildRecentPartyBehaviorText(party, locationText, armyDisplayName);

	private static string BuildRecentPartyBehaviorStableKey(MobileParty party) => CampaignBattleRecordCaptureAdapter.BuildRecentPartyBehaviorStableKey(party);

	private static string BuildRecentPartyBehaviorStableKey(MobileParty party, string locationText, string armyDisplayName) => CampaignBattleRecordCaptureAdapter.BuildRecentPartyBehaviorStableKey(party, locationText, armyDisplayName);

		private string BuildNpcActionSummary(Hero hero, bool recentOnly) => CampaignCharacterRecordCaptureAdapter.BuildNpcActionSummary(_memoryBusinessState, _npcActionRecords, hero, recentOnly, GetCurrentGameDayIndexSafe);

		private static string RenderNpcActionEntriesForPrompt(Hero hero, List<NpcActionEntry> entries) => CampaignCharacterRecordCaptureAdapter.RenderNpcActionEntriesForPrompt(hero, entries);

		private static string RenderNpcActionPromptText(Hero hero, string rawText) => NpcActionRecordOwner.RenderText(hero?.Name?.ToString()?.Trim(), rawText, CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker);

	private static string RewriteNpcActionSecondPersonPronouns(string rawText, string actorName) => NpcActionRecordOwner.RewriteNpcActionSecondPersonPronouns(rawText, actorName);

		private static string BuildNpcActionMetadataNarrativeSuffix(NpcActionEntry entry) => CampaignCharacterRecordCaptureAdapter.BuildNpcActionMetadataNarrativeSuffix(entry);

	private static string TranslateNpcActionKindForPrompt(string actionKind)
	{
		return WeeklyAggregateEventLineOwner.TranslateNpcActionKindForPrompt(actionKind);
	}

	private static string ResolveDisplayNameBySettlementEntry(NpcActionEntry entry) => MemoryEntityIdentityBannerlordAdapter.ResolveDisplayNameBySettlementEntry(entry);

	private static string ResolveHeroName(string heroId) => MemoryEntityIdentityBannerlordAdapter.ResolveHeroName(heroId);

	private static string ResolveClanName(string clanId) => MemoryEntityIdentityBannerlordAdapter.ResolveClanName(clanId);

	private static string ResolveKingdomName(string kingdomId) => MemoryEntityIdentityBannerlordAdapter.ResolveKingdomName(kingdomId);

		private string BuildNpcMajorActionsRuntimeInstruction(Hero hero, CharacterObject targetCharacter = null, int targetAgentIndex = -1) => MemoryEntityIdentityBannerlordAdapter.BuildNpcMajorActionsRuntimeInstruction(_memoryBusinessState, _npcActionRecords, GetCurrentGameDayIndexSafe, BuildRuleTargetKeyForExternal, hero, targetCharacter, targetAgentIndex);

	private string BuildResidentRecentActionsPrompt(Hero hero, CharacterObject targetCharacter = null, int targetAgentIndex = -1) => MemoryEntityIdentityBannerlordAdapter.BuildResidentRecentActionsPrompt(_memoryBusinessState,_npcActionRecords,MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe,BuildRuleTargetKeyForExternal,hero,targetCharacter,targetAgentIndex);

	private string BuildNpcActionsRuntimeConstraintHint(Hero hero, bool recentOnly, CharacterObject targetCharacter = null, int targetAgentIndex = -1) => MemoryEntityIdentityBannerlordAdapter.BuildNpcActionsRuntimeConstraintHint(_memoryBusinessState, _npcActionRecords, GetCurrentGameDayIndexSafe, BuildRuleTargetKeyForExternal, hero, recentOnly, targetCharacter, targetAgentIndex);

	private string BuildNpcCurrentActionFact(Hero hero) => _campaignCharacterRecordCapture.BuildNpcCurrentActionFact(hero);

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("MyBehavior.OnCampaignTick"))
		{
			try
			{
				ProcessOneTailPersistenceRecoveryOnTick();
				bool processedWeeklyReportCommits = false;
			if (_weeklyPromptPreparationQueue.HasPending)
			{
				using (PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessPendingWeeklyPromptPreparations"))
				{
					processedWeeklyReportCommits = ProcessPendingWeeklyPromptPreparations();
				}
			}
			if (!processedWeeklyReportCommits && _weeklyWaveLaunchQueue.HasPending)
			{
				using (PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessPendingWeeklyWaveLaunches"))
				{
					processedWeeklyReportCommits = ProcessPendingWeeklyWaveLaunches();
				}
			}
			if (!processedWeeklyReportCommits && _weeklyBatchApiAttemptQueue.HasPending)
			{
				using (PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessPendingWeeklyBatchApiAttempts"))
				{
					processedWeeklyReportCommits = ProcessPendingWeeklyBatchApiAttempts();
				}
			}
			if (!processedWeeklyReportCommits && _weeklyReportCommitQueue.HasPending)
			{
				using (PerfProbe.Scope("MyBehavior.OnCampaignTick.ProcessPendingWeeklyReportCommits"))
				{
					processedWeeklyReportCommits = ProcessPendingWeeklyReportCommits();
				}
			}
			RunCampaignMemoryMaintenanceCycle(processedWeeklyReportCommits);
			using (PerfProbe.Scope("MyBehavior.OnCampaignTick.CachePlayerClanTier"))
			{
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
		}
		catch
		{
		}
		}
	}

	private void RunCampaignMemoryMaintenanceCycle(bool processedWeeklyReportCommits) => _dailyMaintenanceController.RunCampaignMemoryMaintenanceCycle(processedWeeklyReportCommits);

	private void TryRunCampaignMemoryMaintenance() => _dailyMaintenanceController.TryRunCampaignMemoryMaintenance();

	public override void SyncData(IDataStore dataStore)
	{
		SyncExecutionTranscripts(dataStore);
		if (_shownRecords == null)
		{
			_shownRecords = new Dictionary<string, HeroShownRecord>();
		}
		if (_shownRecordStorage == null)
		{
			_shownRecordStorage = new Dictionary<string, string>();
		}
		_memoryBusinessState.EnsureHistoryAndDailyPersistenceContainers();
		if (_memorySummaryQueueJsonStorage == null)
		{
			_memorySummaryQueueJsonStorage = "";
		}
		_memoryBusinessState.EnsureOverviewPersistenceContainers();
		if (_memoryOverviewQueueJsonStorage == null)
		{
			_memoryOverviewQueueJsonStorage = "";
		}
		if (_wildernessNonHeroPartyMemoryIds == null)
		{
			_wildernessNonHeroPartyMemoryIds = new Dictionary<MobileParty, string>();
		}
		_memoryBusinessState.EnsureMajorSummaryPersistenceContainers();
		if (_npcMajorActionSummaryQueueJsonStorage == null)
		{
			_npcMajorActionSummaryQueueJsonStorage = "";
		}
		EnsureNpcActionRecordContainers();
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
		WeeklyEventDataImportOwner.EnsureContainers(ref _weeklyEventRecords.KingdomOpenings, ref _weeklyRecordPersistence.OpeningStorage,
            ref _weeklyEventRecords.WorldOpening, ref _weeklyEventRecords.Records, ref _weeklyRecordPersistence.RecordJsonStorage);
		_campaignMaterialRecords.EnsureMaterials();
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

		if (_modCreatedRebelKingdomIdStorage == null)
		{
			_modCreatedRebelKingdomIdStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		_unreadWeeklyReportNoticeEventIds = SanitizeUnreadWeeklyReportNoticeEventIds(_unreadWeeklyReportNoticeEventIds);
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
		if (dataStore != null && dataStore.IsLoading)
		{
			ResetRuntimeForLoadedSave("sync_load");
		}
		try
		{
			if (dataStore != null && dataStore.IsSaving)
			{
				SanitizeWildernessNonHeroPartyMemoryIdMap(removeInactive: false);
				dataStore.SyncData<Dictionary<MobileParty, string>>("_af_wildernessNonHeroPartyMemoryIds_v1", ref _wildernessNonHeroPartyMemoryIds);
				LogNonHeroMemoryTrace("stage=sync_save_begin partyGuidMap=" + (_wildernessNonHeroPartyMemoryIds?.Count ?? 0) + " dialogueOwners=" + CountNonHeroDialogueHistoryOwners() + " dialogueLines=" + CountNonHeroDialogueHistoryLines() + " dailyDraftOwners=" + CountNonHeroDailyDraftOwners() + " dailyDraftLines=" + CountNonHeroDailyDraftLines() + " sample=" + BuildNonHeroMemorySampleIds());
				_shownPersistence.Save(dataStore);
				MemoryPersistence.Save(dataStore, LogNonHeroMemoryTrace, IsNonHeroMemoryId);
				CampaignNpcActionPersistenceAdapter.Save(dataStore, _memoryBusinessState, SanitizeNpcActionEntries);
				dataStore.SyncData("_npcActionGlobalOrderCounter_v1", ref _memoryBusinessState.ActionGlobalOrderCounter);
				_personaPersistence.Save(dataStore, TryPrepareNpcPersonaProfileForWrite);
				_weeklyRecordPersistence.SaveOpenings(dataStore);
				_weeklyRecordPersistence.SaveRecords(dataStore, NormalizeEventRecordEntriesInPlace);
				CampaignWeeklyNoticePersistenceAdapter.Save(dataStore, _weeklyNoticeOwner, id => FindWeeklyReportRecordById(id) != null);
				_materialPersistence.Save(dataStore);
				_kingdomPersistence.Save(dataStore);
				dataStore.SyncData("_lastAutoGeneratedWeeklyReportWeek_v1", ref _weeklyEventRecords.LastAutoGeneratedWeek);
dataStore.SyncData("_lastProcessedKingdomRebellionWeek_v1", ref _lastProcessedKingdomRebellionWeek);
					_civilWarPersistence.Save(dataStore);
				_voicePersonaPersistence.Save(dataStore, () => VoiceMapper.ExportMappingJson(pretty: false), VoiceMapper.GetPreferredExportFolder, () => ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false));
				SyncTailPersistenceData(dataStore);
				return;
			}
			dataStore.SyncData<Dictionary<MobileParty, string>>("_af_wildernessNonHeroPartyMemoryIds_v1", ref _wildernessNonHeroPartyMemoryIds);
			SanitizeWildernessNonHeroPartyMemoryIdMap(removeInactive: false);
			LogNonHeroMemoryTrace("stage=sync_load_party_map partyGuidMap=" + (_wildernessNonHeroPartyMemoryIds?.Count ?? 0));
			_shownPersistence.Load(dataStore);
			MemoryPersistence.Load(dataStore,
				() => LogNonHeroMemoryTrace("stage=sync_load_dialogue_restored owners=" + CountNonHeroDialogueHistoryOwners() + " lines=" + CountNonHeroDialogueHistoryLines() + " storageOwners=" + (_dialogueHistoryStorage?.Keys.Count(IsNonHeroMemoryId) ?? 0) + " sample=" + BuildNonHeroMemorySampleIds()),
				() => LogNonHeroMemoryTrace("stage=sync_load_daily_restored owners=" + CountNonHeroDailyDraftOwners() + " lines=" + CountNonHeroDailyDraftLines() + " storageOwners=" + (_dailyMemoryDraftStorage?.Keys.Count(IsNonHeroMemoryId) ?? 0) + " sample=" + BuildNonHeroMemorySampleIds()));
			CampaignNpcActionPersistenceAdapter.Load(dataStore, _memoryBusinessState, SanitizeNpcActionEntries);
			_personaPersistence.Load(dataStore);
			RebuildNpcRecentActionStableKeyIndex();
			dataStore.SyncData("_npcActionGlobalOrderCounter_v1", ref _memoryBusinessState.ActionGlobalOrderCounter);
			NormalizeNpcActionSequences(_npcMajorActions);
			NormalizeNpcActionSequences(_npcRecentActions);
			_npcActionGlobalOrderCounter = Math.Max(_npcActionGlobalOrderCounter, GetMaxNpcActionSequence(_npcMajorActions, _npcRecentActions, _eventSourceMaterials));
			_weeklyRecordPersistence.LoadOpenings(dataStore);
			_weeklyRecordPersistence.LoadRecords(dataStore, NormalizeEventRecordEntriesInPlace);
			CampaignWeeklyNoticePersistenceAdapter.Load(dataStore, _weeklyNoticeOwner, id => FindWeeklyReportRecordById(id) != null);
			_weeklyNoticeOwner.ResetShown();
			_weeklyReportNoticeQueueNormalizedForCurrentPolicy = false;
			_weeklyReportRegisteredMapNotificationView = null;
			_materialPersistence.Load(dataStore);
			RebuildEventSourceMaterialIndex();
			_weeklyReportMaterialRevisions.MarkAll();
			_weeklyReportMaterialRevisions.MarkOpening();
			dataStore.SyncData("_lastAutoGeneratedWeeklyReportWeek_v1", ref _weeklyEventRecords.LastAutoGeneratedWeek);
			_kingdomPersistence.Load(dataStore);
dataStore.SyncData("_lastProcessedKingdomRebellionWeek_v1", ref _lastProcessedKingdomRebellionWeek);
				_civilWarPersistence.Load(dataStore);
			_voicePersonaPersistence.Load(dataStore, VoiceMapper.SetPreferredExportFolder, json => VoiceMapper.ImportMappingJson(json), json => ShoutUtils.ImportUnnamedPersonaStateJson(json, overwriteExisting: true));
			SyncTailPersistenceData(dataStore);
		}
		catch (Exception ex10)
		{
			Logger.Log("DialogueHistory", "[ERROR] SyncData v2 failed: " + ex10.ToString());
			_shownRecords = new Dictionary<string, HeroShownRecord>();
			_shownRecordStorage = new Dictionary<string, string>();
			_dialogueHistory = new Dictionary<string, List<DialogueDay>>();
			_dialogueHistoryStorage = new Dictionary<string, string>();
			_dailyMemoryDrafts = new Dictionary<string, List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);
			_dailyMemoryDraftStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_compressedMemoryBlocks = new Dictionary<string, List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);
			_compressedMemoryBlockStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_memorySummaryQueue = new List<MemorySummaryJob>();
			_memorySummaryQueueJsonStorage = "";
			_nativeConversationMemorySessionCounter = 0;
			_activeNativeConversationMemorySessionId = -1;
			_memoryOverviewStates = new Dictionary<string, MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);
			_memoryOverviewStateStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_memoryOverviewQueue = new List<MemoryOverviewJob>();
			_memoryOverviewQueueJsonStorage = "";
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
			_rebelKingdomIdentity.Clear();
			_modCreatedRebelKingdomIdStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
		starter.AddGameMenuOption("AnimusForge_dev_root", "AnimusForge_dev_root_kingdom_strategic_profiles", "国家战略与性格", DevRootSubOptionCondition, DevRootKingdomStrategicProfilesOptionConsequence);
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
				if (ShouldDisableSettlementOverlayQuickTalk(heroToTalkTo))
				{
					e.IsTalkAvailable(arg1: false, new TextObject("该交谈选项已被模组禁用，请使用造访进入场景后再互动。"));
				}
			}
		}
		catch
		{
		}
	}

	private static bool ShouldDisableSettlementOverlayQuickTalk(Hero heroToTalkTo)
	{
		return heroToTalkTo != null && !heroToTalkTo.IsPlayerCompanion;
	}

	private void OnMissionStarted(IMission mission)
	{
		Mission currentMission = Mission.Current;
		if (mission != null && currentMission != null)
		{
			try
			{
				string arg = currentMission.SceneName ?? "Unknown";
				string arg2 = MobileParty.MainParty?.CurrentSettlement?.Name?.ToString() ?? "";
				MissionMode mode = currentMission.Mode;
				Logger.Log("SceneInfo", $"[MyBehavior.OnMissionStarted] SceneName={arg}, Mode={mode}, Settlement={arg2}");
				LordEncounterBehavior.LogEncounterDiagnostic("MyBehavior.OnMissionStarted", "mission_started_scene_" + arg + "_mode_" + mode);
			}
			catch
			{
			}
		}
	}

	private void OnBattleStartedForEncounterDiag(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification) => _campaignBattleRecordCapture.OnBattleStartedForEncounterDiag(attackerParty, defenderParty, subject, showNotification);

	private static bool ShouldWriteBattleStartEncounterDiagnostic(PartyBase attackerParty, PartyBase defenderParty) => CampaignBattleRecordCaptureAdapter.ShouldWriteBattleStartEncounterDiagnostic(attackerParty, defenderParty);

	private static string DescribeBattleStartPartyForEncounterDiag(PartyBase party) => CampaignBattleRecordCaptureAdapter.DescribeBattleStartPartyForEncounterDiag(party);

	private static string EncounterDiagEscape(string text) => MemoryBusinessStateOwner.EncounterDiagEscape(text);

	private NpcPersonaProfile GetNpcPersonaProfile(Hero npc, bool createIfMissing) => _personaProfiles.Get(npc?.StringId, createIfMissing);

	private void SaveNpcPersonaProfile(Hero npc, NpcPersonaProfile profile) => _personaProfiles.Save(npc?.StringId, profile, StampNpcPersonaProfile);

	private void GetNpcPersonaStrings(Hero hero, out string personality, out string background) => _personaProfiles.GetNpcPersonaStrings(hero?.StringId, out personality, out background);

	private bool NeedsNpcPersonaGeneration(Hero hero)
	{
		return NpcPersonaGenerationApplication.NeedsNpcPersonaGeneration(hero);
	}

	private bool IsNpcPersonaGenerationInFlight(Hero hero)
	{
		return NpcPersonaGenerationApplication.IsNpcPersonaGenerationInFlight(hero);
	}

	private void GetNpcPersonaGenerationRuntimeState(Hero hero, out bool active, out bool coolingDown)
	{
		NpcPersonaGenerationApplication.GetNpcPersonaGenerationRuntimeState(hero, out active, out coolingDown);
	}

	private string GetNpcVoiceId(Hero hero) => _personaProfiles.GetNpcVoiceId(hero?.StringId);

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

	private static string TrimToMaxChars(string s, int maxChars)
	{
		return JsonResponseTextCodec.TrimToMaxChars(s, maxChars);
	}

	private static string NormalizeGeneratedPersonaText(string text)
	{
		return NpcPersonaProfilePolicy.NormalizeGenerated(text);
	}

	private static string NormalizePersonaPromptSourceText(string text, int maxLength = 1200)
	{
		return NpcPersonaTextRules.NormalizePersonaPromptSourceText(text, maxLength);
	}

	private static string GetHeroEncyclopediaBackgroundForPersonaPrompt(Hero hero, int maxLength = 1000)
	{
		return PersonaGenerationFactCaptureAdapter.GetHeroEncyclopediaBackgroundForPersonaPrompt(hero, maxLength);
	}

	private static string GetClanEncyclopediaBackgroundForPersonaPrompt(Clan clan, int maxLength = 1000)
	{
		return PersonaGenerationFactCaptureAdapter.GetClanEncyclopediaBackgroundForPersonaPrompt(clan, maxLength);
	}

	private static Kingdom ResolveKingdomForPersonaPrompt(Hero hero)
	{
		return PersonaGenerationFactCaptureAdapter.ResolveKingdomForPersonaPrompt(hero);
	}

	private static string GetKingdomEncyclopediaBackgroundForPersonaPrompt(Kingdom kingdom, int maxLength = 1200)
	{
		return PersonaGenerationFactCaptureAdapter.GetKingdomEncyclopediaBackgroundForPersonaPrompt(kingdom, maxLength);
	}

	private static string BuildHeroBasicBackgroundForPersonaPrompt(Hero hero)
	{
		return PersonaGenerationFactCaptureAdapter.BuildHeroBasicBackgroundForPersonaPrompt(hero);
	}

	private static string BuildClanOverviewForPersonaPrompt(Clan clan)
	{
		return PersonaGenerationFactCaptureAdapter.BuildClanOverviewForPersonaPrompt(clan);
	}

	private const string NpcSkillLevelReference = "技能水平参考: 0-49平庸，50-99一般，100-149良好，150-199极佳，200-274大师，275以上传奇。";

	private string BuildHeroFactsForPersonaGeneration(Hero hero)
	{
		return PersonaGenerationFactCaptureAdapter.BuildHeroFactsForPersonaGeneration(hero);
	}

	private static string BuildPromotedNonHeroCompanionFactsForPersonaGeneration(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string equipmentSummary)
	{
		return PersonaGenerationFactCaptureAdapter.BuildPromotedNonHeroCompanionFactsForPersonaGeneration(hero, personalName, originalFullName, originalTroopName, originalTroopId, cultureName, sceneLabel, joinEventFact, equipmentSummary);
	}

	private static string GetClanTierReputationLabel(int tier) => PersonaIntroTextRules.GetClanTierReputationLabel(tier);

	private static string BuildAgeBracketLabel(float age) => PersonaIntroTextRules.BuildAgeBracketLabel(age);

	private static bool ContainsIgnoreCase(string text, string token) => MemoryBusinessStateOwner.ContainsIgnoreCase(text, token);

	private static bool IsNpcAskingForConfirmation(string npcText) => MemoryBusinessStateOwner.IsNpcAskingForConfirmation(npcText);

	/// <summary>Cached tier first; live Clan/MainHero read only when the cache is cold. Game-thread read.</summary>
	private static int ResolvePlayerClanTierForPrompt()
	{
        return PersonaIdentityPromptCaptureAdapter.ResolvePlayerClanTierForPrompt(_cachedPlayerClanTier);
    }

	private PromptRoutingPorts CreatePromptRoutingPorts()
	{
		return new PromptRoutingPorts
		{
			AuxiliaryHits = (string text, string secondary, int cap, HashSet<string> excluded, MentionedWorldEntities mentions) =>
			{
				List<GuardrailRuleHit> hits = AIConfigHandler.GetGuardrailSemanticRuleHitsForPreprocess(text, secondary, cap, true, excluded, out var discovered);
				mentions?.Merge(discovered);
				return hits;
			},
			SemanticEvaluator = CreateBuiltInTopicSemanticEvaluator,
			CanInjectGatedRule = AIConfigHandler.CanInjectRuleTopicIntoPreprocessForExternal,
			StickyCarry = _builtInRuleStickyCarry,
			Log = (string category, string message) => { try { Logger.Log(category, message); } catch { } }
		};
	}

	/// <summary>Built-in topics carry their own configured instruction/keywords; others resolve by rule tag.</summary>
	private static PromptTopicSemanticEvaluator CreateBuiltInTopicSemanticEvaluator(string tag, string input, string secondaryInput, HashSet<string> excludedRuleIdSet) => MemoryBusinessStateOwner.CreateBuiltInTopicSemanticEvaluator(tag, input, secondaryInput, excludedRuleIdSet);

	internal static PromptRuntimeTargetBinding CreatePromptRuntimeTargetBinding(string kingdomId, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
        return SharedPromptCaptureBannerlordAdapter.CreatePromptRuntimeTargetBinding(kingdomId, targetHero, targetCharacter, targetAgentIndex);
    }

	/// <summary>Game thread: binding + eligibility facts, so worker-side retrieval never resolves Hero/Mission live.</summary>
	internal static PromptRuleEligibility CapturePromptRuleEligibility(Hero targetHero, CharacterObject targetCharacter, PromptRuntimeTargetBinding binding)
	{
		return AIConfigHandler.CapturePromptRuleEligibility(targetHero, targetCharacter, binding);
	}

	private static string ResolveBuiltInRuleStickyTargetKey(Hero targetHero, CharacterObject targetCharacter)
	{
        return SharedPromptCaptureBannerlordAdapter.ResolveBuiltInRuleStickyTargetKey(targetHero, targetCharacter);
    }

	public void OnEngineTick()
	{
		try
		{
			ProcessPendingMissingOnnxGateCheck();
			ProcessMissingOnnxGateUiResume();
			ProcessMemorySummaryMainThreadActions();
			ProcessPendingMemoryFailureNotice();
			ProcessPendingWeeklyReportManualRetryResult();
			ProcessWeeklyReportUiResume();
			TryPublishUnreadWeeklyReportMapNotifications();
			ProcessWeekZeroShortSummaryMainThreadActions();
			ProcessWeeklyFullReportCompletions();
			ProcessWorldBulletinMainThreadActions();
			ProcessKingdomRebellionApiRepairResume();
			ProcessKingdomRebellionNamingMainThreadActions();
			ProcessPendingDevForcedKingdomRebellionResult();
			ProcessPendingAutomaticKingdomRebellionResult();
			TryStartDeferredAutoWeeklyReports();
		}
		catch
		{
		}
	}

	private void EnqueueKingdomRebellionNamingMainThreadAction(long runtimeGeneration, Action action, string source) => _kingdomRebellionRuntime.EnqueueKingdomRebellionNamingMainThreadAction(runtimeGeneration, action, source);

	private void ProcessKingdomRebellionNamingMainThreadActions() => _kingdomRebellionRuntime.ProcessKingdomRebellionNamingMainThreadActions();

	private void OnGameLoadFinished()
	{
		QueueMissingOnnxGateCheck(TimeSpan.Zero);
		RebuildRuntimeDerivedIndexes();
		ActivateTailPersistenceAfterLoad();
		SyncModCreatedRebelKingdomBannersOnGameLoad();
		// The campaign Hero registry is complete at this lifecycle point, so stale saved compression work can be
		// cancelled before load-finished maintenance queues any LLM request.
		CancelUnavailableHeroCompressionWorkAfterLoad();
		if (IsDeferredDailyMaintenanceEnabled())
		{
			int currentDay = GetCurrentGameDayIndexSafe();
			QueueDeferredAutoWeeklyReportsForWeek((currentDay > 0) ? (currentDay / 7) : 0, currentDay, "game_load_finished");
			EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.DiscontinueLandlessRebelKingdoms, currentDay, reason: "game_load_finished");
			EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.SealPastDailyMemoryDrafts, currentDay, reason: "game_load_finished");
			EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.QueueFullMemoryOverviewScan, currentDay, reason: "game_load_finished");
			EnqueueDailyMaintenanceJob(DailyMaintenanceTaskKind.StartMemorySummaryQueue, currentDay, reason: "game_load_finished");
		}
		else
		{
			TryDiscontinueLandlessModRebelKingdoms("game_load_finished");
			TrySealPastDailyMemoryDrafts();
			QueueAllMemoryOverviewCandidatesForDeferredScan();
			ProcessMemoryOverviewCandidateScanBudget(0L, double.MaxValue);
			TryStartMemorySummaryQueue();
			int currentDay = GetCurrentGameDayIndexSafe();
			int missingWeek = WeeklyReportSchedulePolicy.ResolveOldestMissingWeek(_lastAutoGeneratedWeeklyReportWeek, currentDay);
			if (missingWeek > 0)
			{
				StartAutoWeeklyReportsForWeek(missingWeek, currentDay);
			}
		}
	}

	private void RebuildRuntimeDerivedIndexes()
	{
		RebuildEventSourceMaterialIndex();
		RebuildNpcRecentActionStableKeyIndex();
	}



	private void RebuildNpcRecentActionStableKeyIndex() => _npcActionRecords.RebuildNpcRecentActionStableKeyIndex(_npcRecentActions);



	public void QueueMissingOnnxGateCheckAfterOnboarding()
	{
        _campaignSaveExit.QueueMissingOnnxGateCheckAfterOnboarding();
    }

	private void QueueMissingOnnxGateCheck(TimeSpan delay)
	{
        _campaignSaveExit.QueueMissingOnnxGateCheck(delay);
    }

	private void ProcessPendingMissingOnnxGateCheck()
	{
        _campaignSaveExit.ProcessPendingMissingOnnxGateCheck();
    }

	private void EvaluateMissingOnnxGate()
	{
        _campaignSaveExit.EvaluateMissingOnnxGate();
    }

	private void ProcessMissingOnnxGateUiResume()
	{
        _campaignSaveExit.ProcessMissingOnnxGateUiResume();
    }

	private void ShowMissingOnnxGatePopup()
	{
        _campaignSaveExit.ShowMissingOnnxGatePopup();
    }

	private void ExitCurrentGameBecauseOnnxMissing()
	{
        _campaignSaveExit.ExitCurrentGameBecauseOnnxMissing();
    }

	private static bool HasCompleteRequiredOnnxFiles()
	{
        return CampaignSaveExitController.HasCompleteRequiredOnnxFiles();
    }

	private void ProcessPendingWeeklyReportManualRetryResult()
		=> WeeklyEditor.ProcessPendingWeeklyReportManualRetryResult();
	private void ProcessWeeklyReportUiResume()
		=> WeeklyEditor.ProcessWeeklyReportUiResume();
	 private KingdomRebellionEditorController _kingdomRebellionEditor;
 private KingdomRebellionEditorController KingdomRebellionEditor => _kingdomRebellionEditor ?? (_kingdomRebellionEditor = new KingdomRebellionEditorController(
  () => _kingdomRebellionRuntime, () => AutomaticKingdomRebellions, SaveRuntimeGuard.CaptureGeneration, OpenDevKingdomStabilityDetailMenu));
private void OpenKingdomRebellionApiRepairFlow()
		=> KingdomRebellionEditor.OpenKingdomRebellionApiRepairFlow();

	private void ProcessKingdomRebellionApiRepairResume()
		=> KingdomRebellionEditor.ProcessKingdomRebellionApiRepairResume();

	private void ProcessPendingDevForcedKingdomRebellionResult()
		=> KingdomRebellionEditor.ProcessPendingDevForcedKingdomRebellionResult();

	private void ShowDevForcedKingdomRebellionNamingFailurePopup(PendingDevForcedKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool afterApiRepair)
		=> KingdomRebellionEditor.ShowDevForcedKingdomRebellionNamingFailurePopup(context, kingdom, clan, followerClans, afterApiRepair);

	private void RetryBlockedDevForcedKingdomRebellionNaming()
		=> KingdomRebellionEditor.RetryBlockedDevForcedKingdomRebellionNaming();

	private void StartDevForcedKingdomRebellionAsync(Kingdom kingdom, Clan clan, int weekIndex, int relationToKing, int townCount, int castleCount, List<Clan> followerClans)
		=> KingdomRebellionEditor.StartDevForcedKingdomRebellionAsync(kingdom, clan, weekIndex, relationToKing, townCount, castleCount, followerClans);

	private static bool TryResolveActiveKingdomRuledByHeroForPrompt(Hero hero, out Kingdom kingdom)
	{
		return PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(hero, out kingdom);
	}

	private static string BuildHeroIdentityTitleForPrompt(Hero hero) => PersonaIdentityPromptCaptureAdapter.BuildHeroIdentityTitleForPrompt(hero);

	private static void GetHeroFactionAndLiegeForPrompt(Hero hero, out string factionName, out string liegeName)
	{
		PersonaIdentityPromptCaptureAdapter.GetHeroFactionAndLiegeForPrompt(hero, out factionName, out liegeName);
	}

	private static string BuildFactionLineForPrompt(string label, string factionName, string liegeName)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt(label, factionName, liegeName);
	}

	private static string GetHeroCultureNameForPrompt(Hero hero)
	{
		return PersonaIdentityPromptCaptureAdapter.GetHeroCultureNameForPrompt(hero);
	}

	private static string BuildNpcClanRoleHintForPrompt(Hero npcHero)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildNpcClanRoleHintForPrompt(npcHero);
	}

	private static IEnumerable<Hero> GetClanMembersForPrompt(Clan clan)
    { return PersonaIdentityPromptCaptureAdapter.GetClanMembersForPrompt(clan); }

	private const int MarriageCandidateMinAgeForPrompt = PersonaIdentityPromptCaptureAdapter.MarriageCandidateMinAgeForPrompt;

	private const int MarriageCandidateMaxAgeForPrompt = PersonaIdentityPromptCaptureAdapter.MarriageCandidateMaxAgeForPrompt;

	private const int MarriageCandidateMaxAgeGapForPrompt = PersonaIdentityPromptCaptureAdapter.MarriageCandidateMaxAgeGapForPrompt;

	private static int GetMarriageCandidateMaxAgeSettingForPrompt()
    { return PersonaIdentityPromptCaptureAdapter.GetMarriageCandidateMaxAgeSettingForPrompt(); }

	private static int GetMarriageCandidateMaxAgeGapSettingForPrompt()
    { return PersonaIdentityPromptCaptureAdapter.GetMarriageCandidateMaxAgeGapSettingForPrompt(); }

	private static bool IsMarriageGenderCompatibleForPrompt(Hero candidate, Hero player)
    { return PersonaIdentityPromptCaptureAdapter.IsMarriageGenderCompatibleForPrompt(candidate, player); }

	private static bool IsMarriageAgeCompatibleForPrompt(Hero candidate, Hero player)
    { return PersonaIdentityPromptCaptureAdapter.IsMarriageAgeCompatibleForPrompt(candidate, player); }

	private static bool IsMarriageCandidateForPrompt(Hero hero, Hero player)
    { return PersonaIdentityPromptCaptureAdapter.IsMarriageCandidateForPrompt(hero, player); }

	private static bool IsMarriagePoolCandidateForPrompt(Hero hero)
    { return PersonaIdentityPromptCaptureAdapter.IsMarriagePoolCandidateForPrompt(hero); }

	private static string GetMarriageCandidateGenderLabelForPrompt(Hero hero)
    { return PersonaIdentityPromptCaptureAdapter.GetMarriageCandidateGenderLabelForPrompt(hero); }

	private static string GetNativeSpouseLabelForMarriagePrompt(Hero hero)
    { return PersonaIdentityPromptCaptureAdapter.GetNativeSpouseLabelForMarriagePrompt(hero); }

	private static string BuildClanUnmarriedCandidatesForPrompt(Hero npcHero, Hero player, int maxEntries = 12)
    { return PersonaIdentityPromptCaptureAdapter.BuildClanUnmarriedCandidatesForPrompt(npcHero, player, maxEntries); }

	private static string GetEquipmentContextLabelForPrompt(bool useCivilianEquipment) => PersonaIntroTextRules.GetEquipmentContextLabelForPrompt(useCivilianEquipment);

	private static bool TryResolveEquipmentContextForPrompt(Hero hero, out bool useCivilianEquipment)
    { return PersonaIdentityPromptCaptureAdapter.TryResolveEquipmentContextForPrompt(hero, out useCivilianEquipment); }

	private static ItemObject TryGetAgentEquipmentItemForPrompt(Hero hero, EquipmentIndex index)
    { return PersonaIdentityPromptCaptureAdapter.TryGetAgentEquipmentItemForPrompt(hero, index); }

	private static ItemObject TryGetHeroEquipmentItemForPrompt(Hero hero, EquipmentIndex index, bool useCivilianEquipment)
    { return PersonaIdentityPromptCaptureAdapter.TryGetHeroEquipmentItemForPrompt(hero, index, useCivilianEquipment); }

	private static bool IsWeaponEquipmentIndexForPrompt(EquipmentIndex index) => EquipmentPromptCaptureAdapter.IsWeaponEquipmentIndexForPrompt(index);

	private static void AddEquipmentSummaryItemForPrompt(Dictionary<string, int> counts, Dictionary<string, string> names, EquipmentIndex index, ItemObject item) => EquipmentPromptCaptureAdapter.AddEquipmentSummaryItemForPrompt(counts, names, index, item);

	private static List<string> BuildEquipmentSummaryItemLinesForPrompt(Dictionary<string, int> counts, Dictionary<string, string> names, int maxEntries) => PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(counts, names, maxEntries);

	private static string BuildHeroEquipmentSummaryForPrompt(Hero hero, int maxEntries = 8) => EquipmentPromptCaptureAdapter.BuildHeroEquipmentSummaryForPrompt(hero, CreateHeroEquipmentPromptLivePort(), maxEntries);

	private string BuildPlayerIdentityInfoForPrompt(Hero playerHero, bool includeRuleGatedFields, bool includeTradePricing, bool includeGuidePriceDetails, bool includeMarriageCandidates = false, Hero targetHero = null)
    { return PersonaIdentityPromptCaptureAdapter.BuildPlayerIdentityInfoForPrompt(playerHero, includeRuleGatedFields, includeTradePricing, includeGuidePriceDetails, includeMarriageCandidates, targetHero); }

	private static string BuildNpcPlayerKinshipPromptLine(Hero npcHero, bool includeSameClanFallback)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildNpcPlayerKinshipPromptLine(npcHero, includeSameClanFallback);
	}

	public static string BuildNpcPlayerKinshipPromptLineForExternal(Hero npcHero)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildNpcPlayerKinshipPromptLineForExternal(npcHero);
	}

	private static string ResolveNpcPlayerKinshipText(Hero npcHero, Hero mainHero, string playerDisplayName, bool includeSameClanFallback, out bool isPlayerParent)
	{
		return PersonaIdentityPromptCaptureAdapter.ResolveNpcPlayerKinshipText(npcHero, mainHero, playerDisplayName, includeSameClanFallback, out isPlayerParent);
	}

	private static string GetPlayerDisplayNameForRelationshipPrompt(Hero mainHero)
	{
		return PersonaIdentityPromptCaptureAdapter.GetPlayerDisplayNameForRelationshipPrompt(mainHero);
	}

	private static bool ContainsHero(IEnumerable<Hero> heroes, Hero target)
	{
		return PersonaIdentityPromptCaptureAdapter.ContainsHero(heroes, target);
	}

	private static bool ShareKnownParent(Hero left, Hero right)
	{
		return PersonaIdentityPromptCaptureAdapter.ShareKnownParent(left, right);
	}

	private static string BuildPlayerSiblingLabelForPrompt(Hero npcHero, Hero mainHero)
	{
		return PersonaIdentityPromptCaptureAdapter.BuildPlayerSiblingLabelForPrompt(npcHero, mainHero);
	}

	private string BuildNpcIdentityInfoForPrompt(Hero npcHero, bool includeTradePricing, bool includeMarriageCandidates = false)
    { return PersonaIdentityPromptCaptureAdapter.BuildNpcIdentityInfoForPrompt(npcHero, includeTradePricing, includeMarriageCandidates); }

	private static string BuildNpcInventorySummaryHeader(Hero npcHero)
    { return PersonaIdentityPromptCaptureAdapter.BuildNpcInventorySummaryHeader(npcHero); }

	private static string BuildNobleEtiquettePromptForHero(Hero npcHero)
    { return PersonaIdentityPromptCaptureAdapter.BuildNobleEtiquettePromptForHero(npcHero); }

	public static string BuildNobleEtiquettePromptForExternal(Hero npcHero)
	{
		return BuildNobleEtiquettePromptForHero(npcHero);
	}

	private string BuildNpcSystemTopPromptIntro(Hero npcHero, bool includeTradePricing) => CreatePersonaEquipmentPromptCaptureAdapter().BuildNpcSystemIntro(npcHero, includeTradePricing);

	private static int GetDaysInSeasonSafeForPrompt()
	{
		return PersonaIdentityPromptCaptureAdapter.GetDaysInSeasonSafeForPrompt();
	}

	private static int GetDaysInYearSafeForPrompt()
	{
		return PersonaIdentityPromptCaptureAdapter.GetDaysInYearSafeForPrompt();
	}

	private static string GetSeasonTextZhForPrompt(int seasonIndexZeroBased)
	{
		return PersonaIdentityPromptCaptureAdapter.GetSeasonTextZhForPrompt(seasonIndexZeroBased);
	}

	private static int GetCurrentHourOfDaySafeForPrompt()
    { return PersonaIdentityPromptCaptureAdapter.GetCurrentHourOfDaySafeForPrompt(); }

	public static int GetCurrentMemoryGameHourForExternal()
	{
		return PersonaIdentityPromptCaptureAdapter.GetCurrentMemoryGameHourForExternal();
	}

	private static string GetTimeOfDayTextZhForPrompt(int hourOfDay)
	{
		return PersonaIdentityPromptCaptureAdapter.GetTimeOfDayTextZhForPrompt(hourOfDay);
	}

	private static bool IsDayTimeForPrompt(int hourOfDay)
	{
		return PersonaIdentityPromptCaptureAdapter.IsDayTimeForPrompt(hourOfDay);
	}

	private static string GetTimeOfDayDetailTextZhForPrompt(int hourOfDay)
	{
		return PersonaIdentityPromptCaptureAdapter.GetTimeOfDayDetailTextZhForPrompt(hourOfDay);
	}

	private string BuildCurrentDateFactForPrompt()
	{
		return PersonaIdentityPromptCaptureAdapter.BuildCurrentDateFactForPrompt();
	}

	private static bool TryParsePersonaJson(string text, out string personality, out string background)
	{
		return NpcPersonaTextRules.TryParsePersonaJson(text, out personality, out background);
	}

	private static string ExtractLoosePersonaJsonField(string text, params string[] fieldNames)
	{
		return NpcPersonaTextRules.ExtractLoosePersonaJsonField(text, fieldNames);
	}

	private static string AppendNpcPersonaGenerationRequirementsToSystemPrompt(string systemPrompt)
	{
		return NpcPersonaTextRules.AppendNpcPersonaGenerationRequirementsToSystemPrompt(systemPrompt, DuelSettings.GetSettings()?.NpcPersonaGenerationRequirements);
	}

		private static SkillObject[] GetPromotedCompanionSkillObjects() => CampaignCharacterRecordCaptureAdapter.GetPromotedCompanionSkillObjects();

		private static string BuildPromotedHeroSkillSummary(Hero hero) => CampaignCharacterRecordCaptureAdapter.BuildPromotedHeroSkillSummary(hero);

		private static bool TryApplyPromotedHeroSkillJson(Hero hero, string raw) => CampaignCharacterRecordCaptureAdapter.TryApplyPromotedHeroSkillJson(hero, raw);

		private static Dictionary<string, SkillObject> BuildPromotedSkillMap() => CampaignCharacterRecordCaptureAdapter.BuildPromotedSkillMap();

	private static string NormalizePromotedSkillKey(string key)
	{
		return NpcPersonaTextRules.NormalizePromotedSkillKey(key);
	}

	private static string BuildPlayerClanUnmarriedCandidatesForPrompt(Hero playerHero, Hero targetHero, int maxEntries = 12)
    { return PersonaIdentityPromptCaptureAdapter.BuildPlayerClanUnmarriedCandidatesForPrompt(playerHero, targetHero, maxEntries); }

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

	public static int TransferItemsFromRosterByStringId(ItemRoster sourceRoster, ItemRoster targetRoster, string itemId, int amount, out ItemObject transferredItem)
	{
		return PartyAssetTransferBannerlordAdapter.TransferItemsFromRosterByStringId(sourceRoster, targetRoster, itemId, amount, out transferredItem);
	}

	public static int RemoveItemsFromRosterByStringId(ItemRoster itemRoster, string itemId, int amount, out ItemObject removedItem)
	{
		return TransferItemsFromRosterByStringId(itemRoster, null, itemId, amount, out removedItem);
	}

	private static string ParseLordIdFromUnnamedKey(string unnamedKey) => MemoryEntityIdentityBannerlordAdapter.ParseLordIdFromUnnamedKey(unnamedKey);

	internal static Hero ResolveTransferTargetHeroFromAgent(Agent agent) => CampaignCharacterRecordCaptureAdapter.ResolveTransferTargetHeroFromAgent(agent);

	private HeroShownRecord GetShownRecord(Hero hero)
	{
		return GetShownRecord(hero, null, createIfMissing: true);
	}

	private HeroShownRecord GetShownRecord(Hero hero, string targetKey, bool createIfMissing) => _shownResourceRecords.Get(ResolveShownRecordKey(hero, targetKey), createIfMissing);

	private void RecordShownResources(Hero hero, string targetKey, int shownGold, Dictionary<string, int> shownItems)
	{
		if (shownGold <= 0 && (shownItems == null || shownItems.Count == 0)) return;
		_shownResourceRecords.Record(ResolveShownRecordKey(hero, targetKey), shownGold, shownItems);
	}

	private int GetRemainingShowableGold(Hero hero, string targetKey, int currentGold) => _shownResourceRecords.RemainingGold(ResolveShownRecordKey(hero, targetKey), currentGold);

	private int GetRemainingShowableItemCount(Hero hero, string targetKey, string itemId, int currentAmount)
	{
		if (string.IsNullOrWhiteSpace(itemId)) return Math.Max(0, currentAmount);
		return _shownResourceRecords.RemainingItemCount(ResolveShownRecordKey(hero, targetKey), itemId, currentAmount);
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
		return CampaignShownRecordPersistenceAdapter.NormalizeKey(targetKey);
	}

	private static string NormalizeMemoryHeroId(string heroId)
	{
		return MemoryRecordRules.NormalizeMemoryHeroId(heroId);
	}
	private static string GetMemoryHeroId(Hero hero) => CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero);

	private const string NonHeroMemoryIdPrefix = "af_nonhero:";

		private static bool IsNonHeroMemoryId(string memoryId) => MemoryBusinessStateOwner.IsNonHeroMemoryId(memoryId);

	private static void LogNonHeroMemoryTrace(string message) => MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace(message);

	private static void LogNonHeroMemoryTrace(Func<string> messageFactory) => MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace(messageFactory);

	private static int CountDialogueHistoryLines(IEnumerable<DialogueDay> records) => MemoryHistoryCommitBannerlordAdapter.CountDialogueHistoryLines(records);

	private static int CountDailyMemoryDraftLines(IEnumerable<DailyMemoryDraft> drafts) => MemoryBusinessStateOwner.CountDailyMemoryDraftLines(drafts);

	private int CountNonHeroDialogueHistoryOwners() => _memoryHistoryCommit.CountNonHeroDialogueHistoryOwners();

	private int CountNonHeroDialogueHistoryLines()
	{
        return _memoryHistoryCommit.CountNonHeroDialogueHistoryLines();
    }

	private int CountNonHeroDailyDraftOwners() => _memoryHistoryCommit.CountNonHeroDailyDraftOwners();

	private int CountNonHeroDailyDraftLines() => _memoryBusinessState.CountNonHeroDailyDraftLines();

	private string BuildNonHeroMemorySampleIds(int maxCount = 5) => _memoryHistoryCommit.BuildNonHeroMemorySampleIds(maxCount);

	private static string BuildNonHeroMemoryId(string unnamedKey) => CampaignCharacterRecordCaptureAdapter.BuildNonHeroMemoryId(unnamedKey);

	public static string BuildNonHeroMemoryIdForExternal(string unnamedKey)
	{
		return BuildNonHeroMemoryId(unnamedKey);
	}

	public static string GetOrCreateWildernessNonHeroPartyMemoryKeyForExternal(MobileParty party)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.GetOrCreateWildernessNonHeroPartyMemoryKey(party) ?? "";
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] get wilderness non-hero party memory key failed: " + ex.Message);
			return "";
		}
	}

	public static string GetExistingWildernessNonHeroPartyMemoryKeyForExternal(MobileParty party)
	{
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.GetExistingWildernessNonHeroPartyMemoryKey(party) ?? "";
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] get existing wilderness non-hero party memory key failed: " + ex.Message);
			return "";
		}
	}

	private string GetOrCreateWildernessNonHeroPartyMemoryKey(MobileParty party) => _memoryEntityIdentity.GetOrCreateWildernessNonHeroPartyMemoryKey(party);

	private string GetExistingWildernessNonHeroPartyMemoryKey(MobileParty party) => _memoryEntityIdentity.GetExistingWildernessNonHeroPartyMemoryKey(party);

	private static bool IsValidWildernessNonHeroMemoryParty(MobileParty party) => MemoryEntityIdentityBannerlordAdapter.IsValidWildernessNonHeroMemoryParty(party);

	private void SanitizeWildernessNonHeroPartyMemoryIdMap(bool removeInactive) => _memoryEntityIdentity.SanitizeWildernessNonHeroPartyMemoryIdMap(removeInactive);

	private void RemoveWildernessNonHeroPartyMemory(MobileParty party, PartyBase partyBase, string reason) => _memoryEntityIdentity.RemoveWildernessNonHeroPartyMemory(_campaignBattleRecordCapture, party, partyBase, reason);

	private bool TryReserveDestroyedPartyMemoryCleanup(PartyBase partyBase, MobileParty party) => _campaignBattleRecordCapture.TryReserveDestroyedPartyMemoryCleanup(partyBase, party);

	public static void MigrateNonHeroPartyIndexMemoryForExternal(string canonicalMemoryId, string baseUnnamedKey)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.MigrateNonHeroPartyIndexMemory(canonicalMemoryId, baseUnnamedKey);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] migrate non-hero party_index memory failed: " + ex.Message);
		}
	}

	public static void MigrateNonHeroPartyScopedMemoryForExternal(string canonicalMemoryId, string partyKey)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => { owner.MigrateNonHeroPartyScopedMemory(canonicalMemoryId, partyKey); }, nameof(MigrateNonHeroPartyScopedMemoryForExternal))) return;

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.MigrateNonHeroPartyScopedMemory(canonicalMemoryId, partyKey);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] migrate non-hero party scoped memory failed: " + ex.Message);
		}
	}

	private void MigrateNonHeroPartyIndexMemory(string canonicalMemoryId, string baseUnnamedKey) => MemoryEntityIdentityBannerlordAdapter.MigrateNonHeroPartyIndexMemory(canonicalMemoryId, baseUnnamedKey);

	private void MigrateNonHeroPartyScopedMemory(string canonicalMemoryId, string partyKey) => MemoryEntityIdentityBannerlordAdapter.MigrateNonHeroPartyScopedMemory(_memoryIdentityMerge, canonicalMemoryId, partyKey);

	private HashSet<string> CollectNonHeroMemoryAliasesByPrefix(string legacyPrefix, string targetId) => MemoryEntityIdentityBannerlordAdapter.CollectNonHeroMemoryAliasesByPrefix(_memoryBusinessState, legacyPrefix, targetId);

	private HashSet<string> CollectNonHeroMemoryAliasesByNeedle(string needle, string targetId) => MemoryEntityIdentityBannerlordAdapter.CollectNonHeroMemoryAliasesByNeedle(_memoryBusinessState, needle, targetId);

	private MemoryIdentityPort _memoryIdentityPort;
 private MemoryBusinessStateOwner MemoryIdentityState { get { var owner=MemoryQueueState;owner.IdentityPort ??= _memoryIdentityPort ??= new MemoryIdentityPort { LoadHistory=LoadDialogueHistoryById, SaveHistory=SaveDialogueHistoryById, Recovery=()=>MemoryRecoveryState, RefreshRecentIndex=RefreshNpcRecentActionStableKeyIndexForHero, MarkWeeklySourcesDirty=()=>_weeklyReportMaterialRevisions.MarkAll(), MarkOverviewDirty=MarkMemoryOverviewDirty };return owner; } }
 private void MergeMemoryEntityDataById(string sourceMemoryId,string targetMemoryId) => MemoryIdentityState.MergeMemoryEntityDataById(sourceMemoryId,targetMemoryId);

	private static List<DialogueDay> MergeDialogueDayLists(IEnumerable<DialogueDay> targetDays,IEnumerable<DialogueDay> sourceDays) => MemoryBusinessStateOwner.MergeDialogueDayLists(targetDays,sourceDays);

	private static List<DailyMemoryDraft> RetargetDailyMemoryDrafts(IEnumerable<DailyMemoryDraft> drafts, string targetMemoryId)
	{
		return MemoryBusinessStateOwner.RetargetDailyMemoryDrafts(drafts, targetMemoryId);
	}

	private static List<DailyMemoryDraft> MergeDailyMemoryDraftLists(IEnumerable<DailyMemoryDraft> targetDrafts, IEnumerable<DailyMemoryDraft> sourceDrafts, string targetMemoryId)
	{
		return MemoryBusinessStateOwner.MergeDailyMemoryDraftLists(targetDrafts, sourceDrafts, targetMemoryId);
	}

	private static List<CompressedMemoryBlock> RetargetCompressedMemoryBlocks(IEnumerable<CompressedMemoryBlock> blocks, string targetMemoryId)
	{
		return MemoryBusinessStateOwner.RetargetCompressedMemoryBlocks(blocks, targetMemoryId);
	}

	private static List<CompressedMemoryBlock> MergeCompressedMemoryBlockLists(IEnumerable<CompressedMemoryBlock> targetBlocks, IEnumerable<CompressedMemoryBlock> sourceBlocks, string targetMemoryId)
	{
		return MemoryBusinessStateOwner.MergeCompressedMemoryBlockLists(targetBlocks, sourceBlocks, targetMemoryId);
	}

	private static string MergeDistinctTextBlocks(string first, string second)
	{
		return MemoryBusinessStateOwner.MergeDistinctTextBlocks(first, second);
	}

	private void RetargetMemoryQueues(string sourceMemoryId, string targetMemoryId)
	{
		_memoryBusinessState.RetargetMemoryQueues(sourceMemoryId, targetMemoryId);
	}

	private void MergeMemoryOverviewStateById(string sourceMemoryId, string targetMemoryId)
	{
		_memoryBusinessState.MergeMemoryOverviewStateById(sourceMemoryId, targetMemoryId);
	}

	private void MergeMajorActionSummaryStateById(string sourceMemoryId, string targetMemoryId)
	{
		_memoryBusinessState.MergeMajorActionSummaryStateById(sourceMemoryId, targetMemoryId);
	}

	private void MergeNpcActionStorageById(Dictionary<string,List<NpcActionEntry>> storage,string sourceMemoryId,string targetMemoryId,bool keepOnlyRecentWindow) => MemoryIdentityState.MergeNpcActionStorageById(storage,sourceMemoryId,targetMemoryId,keepOnlyRecentWindow);

	private void RetargetMemoryOverviewCandidateScanIds(string sourceMemoryId, string targetMemoryId)
	{
		_memoryBusinessState.RetargetMemoryOverviewCandidateScanIds(sourceMemoryId, targetMemoryId);
	}

	private List<string> BuildNonHeroPartyMemoryNeedles(MobileParty mobileParty, PartyBase partyBase = null) => _memoryEntityIdentity.BuildNonHeroPartyMemoryNeedles(mobileParty, partyBase);

	private static bool ContainsExactNonHeroPartyNeedle(string memoryId, string partyNeedle) => MemoryEntityIdentityBannerlordAdapter.ContainsExactNonHeroPartyNeedle(memoryId, partyNeedle);

	private static bool IsNonHeroMemoryIdForParty(string memoryId, List<string> partyNeedles) => CampaignBattleRecordCaptureAdapter.IsNonHeroMemoryIdForParty(memoryId, partyNeedles);

	private bool CancelUnavailableHeroCompressionWorkById(string memoryId, string reason) => MemoryQueueState.CancelUnavailableHeroCompressionWorkById(memoryId, reason);

	private void CancelUnavailableHeroCompressionWorkAfterLoad() => MemoryEntityIdentityBannerlordAdapter.CancelUnavailableHeroCompressionWorkAfterLoad(_memoryBusinessState, _loadMemoryQueueState);

	private void RemoveMemoryEntityDataById(string memoryId) => MemoryIdentityState.RemoveMemoryEntityDataById(memoryId);

	private void CleanupNonHeroMemoryForRemovedParty(PartyBase partyBase, MobileParty mobileParty, string reason) => _campaignBattleRecordCapture.CleanupNonHeroMemoryForRemovedParty(partyBase, mobileParty, reason);

	private void OnMobilePartyDestroyedForNonHeroMemoryCleanup(MobileParty mobileParty, PartyBase destroyerParty) => _campaignBattleRecordCapture.OnMobilePartyDestroyedForNonHeroMemoryCleanup(mobileParty, destroyerParty);

	private void OnPartyRemovedForNonHeroMemoryCleanup(PartyBase party) => _campaignBattleRecordCapture.OnPartyRemovedForNonHeroMemoryCleanup(party);

	private static bool IsMemoryEntityEligibleForCompressedMemory(string memoryId) => MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory(memoryId);

	private static bool IsHeroNpcEligibleForCompressedMemory(Hero hero) => MemoryEntityIdentityBannerlordAdapter.IsHeroNpcEligibleForCompressedMemory(hero);

	private static List<WeeklyMemoryMaterialTrigger> SanitizeWeeklyMemoryMaterialTriggers(IEnumerable<WeeklyMemoryMaterialTrigger> triggers)
	{
		return MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(triggers);
	}
	private static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(IEnumerable<DailyMemoryDraft> drafts)
	{
		return MemoryRecordRules.SanitizeDailyMemoryDrafts(drafts);
	}
	private static void BindDailyMemoryDraftWeeklyTrigger(WeeklyMemoryMaterialTrigger trigger, string memoryId, int gameDayIndex, string gameDate)
	{
		MemoryRecordRules.BindDailyMemoryDraftWeeklyTrigger(trigger, memoryId, gameDayIndex, gameDate);
	}
	private static DailyMemoryLine SanitizeDailyMemoryDraftLine(DailyMemoryLine x, DailyMemoryDraft draft)
	{
		return MemoryRecordRules.SanitizeDailyMemoryDraftLine(x, draft);
	}
	private static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(DailyMemoryDraft sourceEntry, HashSet<string> seen)
	{
		return MemoryRecordRules.SanitizeDailyMemoryDraftEntry(sourceEntry, seen);
	}
	private static List<CompressedMemoryBlock> SanitizeCompressedMemoryBlocks(IEnumerable<CompressedMemoryBlock> blocks)
	{
		return MemoryRecordRules.SanitizeCompressedMemoryBlocks(blocks);
	}
	private static List<MemorySummaryJob> SanitizeMemorySummaryQueue(IEnumerable<MemorySummaryJob> jobs)
	{
		return MemoryRecordRules.SanitizeMemorySummaryQueue(jobs);
	}
	private static List<MemorySummaryJob> NormalizeMemorySummaryQueue(IEnumerable<MemorySummaryJob> jobs)
	{
		return MemoryRecordRules.NormalizeMemorySummaryQueue(jobs);
	}
	private static MemoryOverviewState SanitizeMemoryOverviewState(MemoryOverviewState state)
	{
		return MemoryRecordRules.SanitizeMemoryOverviewState(state);
	}
	private static List<MemoryOverviewJob> SanitizeMemoryOverviewQueue(IEnumerable<MemoryOverviewJob> jobs)
	{
		return MemoryRecordRules.SanitizeMemoryOverviewQueue(jobs);
	}
	private MemoryOverviewState GetMemoryOverviewState(string heroId) => MemoryQueueState.GetMemoryOverviewState(heroId);

	private static bool IsMemoryBlockIncludedInOverview(CompressedMemoryBlock block, HashSet<string> includedIds) => MemoryBusinessStateOwner.IsMemoryBlockIncludedInOverview(block, includedIds);

	private bool HasMemoryOverviewPendingBlocks(string heroId, List<CompressedMemoryBlock> blocks) => MemoryQueueState.HasMemoryOverviewPendingBlocks(heroId, blocks);

	private bool HasMemoryOverviewJobStillPending(MemoryOverviewJob job) => MemoryQueueState.HasMemoryOverviewJobStillPending(job);

	private static MajorActionSummaryState SanitizeMajorActionSummaryState(MajorActionSummaryState state)
	{
		return MemoryRecordRules.SanitizeMajorActionSummaryState(state);
	}
	private static List<MajorActionSummaryJob> SanitizeMajorActionSummaryQueue(IEnumerable<MajorActionSummaryJob> jobs)
	{
		return MemoryRecordRules.SanitizeMajorActionSummaryQueue(jobs);
	}
	private static List<MajorActionSummaryJob> NormalizeMajorActionSummaryQueue(IEnumerable<MajorActionSummaryJob> jobs)
	{
		return MemoryRecordRules.NormalizeMajorActionSummaryQueue(jobs);
	}
	private MajorActionSummaryState GetMajorActionSummaryState(string heroId) => MemoryQueueState.GetMajorActionSummaryState(heroId);

	private static void GetMajorActionMaxCursor(IEnumerable<NpcActionEntry> actions, out int day, out int sequence) => MemoryBusinessStateOwner.GetMajorActionMaxCursor(actions, out day, out sequence);

	private static bool IsNpcActionAfterSummaryCursor(NpcActionEntry action, MajorActionSummaryState state) => MemoryBusinessStateOwner.IsNpcActionAfterSummaryCursor(action, state);

	private bool HasMajorActionsNeedingSummary(string heroId, List<NpcActionEntry> actions) => MemoryQueueState.HasMajorActionsNeedingSummary(heroId, actions);

	private bool HasMajorActionSummaryJobStillPending(MajorActionSummaryJob job) => MemoryQueueState.HasMajorActionSummaryJobStillPending(job);

	private static string BuildMajorActionSummarySourceLine(Hero hero, NpcActionEntry entry) => MemorySummaryApplicationAdapter.BuildMajorActionSummarySourceLine(hero, entry);

	private static int GetMajorActionSummaryTargetChars(MajorActionSummaryState state, Hero hero, List<NpcActionEntry> sourceActions) => MemorySummaryApplicationAdapter.GetMajorActionSummaryTargetChars(state, hero, sourceActions);

	private static string BuildCompressedMemoryBlockId(string heroId, int dayIndex)
	{
		return MemoryRecordRules.BuildCompressedMemoryBlockId(heroId, dayIndex);
	}
	private List<DailyMemoryDraft> LoadDailyMemoryDrafts(Hero hero)
	{
		return LoadDailyMemoryDraftsById(GetMemoryHeroId(hero));
	}

	private List<DailyMemoryDraft> LoadDailyMemoryDraftsById(string memoryId)
	{
		return _memoryBusinessState.LoadDrafts(memoryId);
	}

	private void SaveDailyMemoryDrafts(Hero hero, List<DailyMemoryDraft> drafts)
	{
		SaveDailyMemoryDraftsById(GetMemoryHeroId(hero), drafts);
	}

	private void SaveDailyMemoryDraftsById(string memoryId, List<DailyMemoryDraft> drafts)
	{
		_memoryBusinessState.SaveDrafts(memoryId, drafts);
	}

	private List<CompressedMemoryBlock> LoadCompressedMemoryBlocks(Hero hero)
	{
		return LoadCompressedMemoryBlocksById(GetMemoryHeroId(hero));
	}

	private List<CompressedMemoryBlock> LoadCompressedMemoryBlocksById(string memoryId)
	{
		return _memoryBusinessState.LoadBlocks(memoryId);
	}

	private void SaveCompressedMemoryBlocks(Hero hero, List<CompressedMemoryBlock> blocks)
	{
		SaveCompressedMemoryBlocksById(GetMemoryHeroId(hero), blocks);
	}

	private void SaveCompressedMemoryBlocksById(string memoryId, List<CompressedMemoryBlock> blocks)
	{
		_memoryBusinessState.SaveBlocks(memoryId, blocks, MarkMemoryOverviewDirty);
	}

	private static string ResolveCurrentMemorySceneLabel()
	{
		return SceneLocationPromptCaptureAdapter.ResolveCurrentMemorySceneLabel();
	}

	public static string ResolveCurrentMemorySceneLabelForExternal()
	{
		return ResolveCurrentMemorySceneLabel();
	}

	private static bool IsUnknownMemorySceneLabel(string scene) => UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(scene);

	private static string BuildCurrentWildernessMemorySceneLabel()
	{
		return SceneLocationPromptCaptureAdapter.BuildCurrentWildernessMemorySceneLabel();
	}

	private void AppendDailyMemoryLine(Hero hero, string speaker, string text, bool isAfef, bool isLlmDialogue, int sceneSessionId = -1, int targetAgentIndex = -1, string targetName = null)
	{
		if (!IsHeroNpcEligibleForCompressedMemory(hero))
		{
			return;
		}
		string heroName = (hero.Name?.ToString() ?? "NPC").Trim();
		AppendDailyMemoryLineById(GetMemoryHeroId(hero), string.IsNullOrWhiteSpace(heroName) ? "NPC" : heroName, speaker, text, isAfef, isLlmDialogue, sceneSessionId, targetAgentIndex, targetName);
	}

	private bool AppendDailyMemoryLineById(string memoryId, string memoryName, string speaker, string text, bool isAfef, bool isLlmDialogue, int sceneSessionId = -1, int targetAgentIndex = -1, string targetName = null) => _memoryHistoryCommit.AppendDailyMemoryLineById(memoryId, memoryName, speaker, text, isAfef, isLlmDialogue, sceneSessionId, targetAgentIndex, targetName);

	// Read raw owner state, not prompt-filtered history. Sanitization retains object
	// references; an older identical line cannot acknowledge this append.
	private bool IsDailyMemoryLinePublished(string normalizedMemoryId, int gameDayIndex, DailyMemoryDraft draft, DailyMemoryLine line) => _memoryBusinessState.IsDailyMemoryLinePublished(normalizedMemoryId, gameDayIndex, draft, line);

	private bool IsDialogueHistoryPublished(string normalizedMemoryId, List<DialogueDay> records)
	{
        return _memoryHistoryCommit.IsDialogueHistoryPublished(normalizedMemoryId, records);
    }

	private List<DialogueDay> LoadDialogueHistory(Hero hero) => _memoryHistoryCommit.LoadDialogueHistory(hero);

	private List<DialogueDay> LoadDialogueHistoryById(string memoryId) => _memoryHistoryCommit.LoadDialogueHistoryById(memoryId);

	private void TryEnsureFirstMeetingNpcFactForConversation(Hero hero)
	{
        _memoryHistoryCommit.TryEnsureFirstMeetingNpcFactForConversation(hero);
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
        return _memoryHistoryCommit.GetFirstMeetingNpcFactTextForPromptIfNeededInternal(hero, persistToHistory);
    }

	private static string BuildFirstMeetingNpcFactText()
	{
        return MemoryHistoryCommitBannerlordAdapter.BuildFirstMeetingNpcFactText();
    }

	private static string BuildFirstMeetingNpcFactText(Hero observer)
	{
        return MemoryHistoryCommitBannerlordAdapter.BuildFirstMeetingNpcFactText(observer);
    }

	private static bool HasDialogueHistoryLine(List<DialogueDay> records, string targetLine)
	{
        return MemoryHistoryCommitBannerlordAdapter.HasDialogueHistoryLine(records, targetLine);
    }

	private static bool HasMeaningfulDirectConversationHistory(List<DialogueDay> records)
	{
        return MemoryHistoryCommitBannerlordAdapter.HasMeaningfulDirectConversationHistory(records);
    }

	private static bool HasMeaningfulConversationHistoryIncludingActiveScene(List<DialogueDay> records)
	{
        return MemoryBusinessStateOwner.HasMeaningfulConversationHistoryIncludingActiveScene(records);
    }

	private void SaveDialogueHistory(Hero hero, List<DialogueDay> records)
	{
        _memoryHistoryCommit.SaveDialogueHistory(hero, records);
    }

	private void SaveDialogueHistoryById(string memoryId, List<DialogueDay> records) => _memoryHistoryCommit.SaveDialogueHistoryById(memoryId, records);

	public static void AppendExternalDialogueHistory(Hero hero, string playerText, string aiText, string extraFact)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => { owner.AppendDialogueHistory(hero, playerText, aiText, extraFact); }, nameof(AppendExternalDialogueHistory))) return;

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistory(hero, playerText, aiText, extraFact);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Strict batch boundary for detached commits. Applied confirms runtime daily
	/// and recent-history acceptance only, not SyncData/disk persistence. Failure
	/// may follow partial writes; callers must not replay actions or assume rollback.
	/// </summary>
	public static MemoryCommitResult CommitExternalDialogueHistory(string memoryId, bool isNonHero, string npcName, string playerText, string aiText, string extraFact)
	{
		// Preserve the public six-argument ABI and its original loose-session behavior.
		return CommitDialogueHistoryWithScene(memoryId, isNonHero, npcName, playerText, aiText, extraFact, -1);
	}

	public static void AppendExternalSceneDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId, int playerTargetAgentIndex = -1, string playerTargetName = null)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => { owner.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName); }, nameof(AppendExternalSceneDialogueHistory))) return;

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName);
		}
		catch
		{
		}
	}

	public static void AppendExternalNonHeroDialogueHistory(string nonHeroMemoryId, string npcName, string playerText, string aiText, string extraFact)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => { owner.AppendDialogueHistoryById(nonHeroMemoryId, npcName, playerText, aiText, extraFact); }, nameof(AppendExternalNonHeroDialogueHistory))) return;

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistoryById(nonHeroMemoryId, npcName, playerText, aiText, extraFact);
		}
		catch
		{
		}
	}

	public static void AppendExternalNonHeroSceneDialogueHistory(string nonHeroMemoryId, string npcName, string playerText, string aiText, string extraFact, int sceneSessionId, int playerTargetAgentIndex = -1, string playerTargetName = null)
	{
		if (DeferMemorySourceWriteIfNeeded(owner => { owner.AppendDialogueHistoryById(nonHeroMemoryId, npcName, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName); }, nameof(AppendExternalNonHeroSceneDialogueHistory))) return;

		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.AppendDialogueHistoryById(nonHeroMemoryId, npcName, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName);
		}
		catch
		{
		}
	}

	private bool AppendDialogueHistory(Hero hero, string playerText, string aiText, string extraFact, int sceneSessionId = -1, int playerTargetAgentIndex = -1, string playerTargetName = null) => _memoryHistoryCommit.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName);

	private bool AppendDialogueHistoryById(string memoryId, string npcNameForMemory, string playerText, string aiText, string extraFact, int sceneSessionId = -1, int playerTargetAgentIndex = -1, string playerTargetName = null) => _memoryHistoryCommit.AppendDialogueHistoryById(memoryId, npcNameForMemory, playerText, aiText, extraFact, sceneSessionId, playerTargetAgentIndex, playerTargetName);

	private string GetLatestNpcDialogueUtterance(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1) => _memoryHistoryCommit.GetLatestNpcDialogueUtterance(targetHero, targetCharacter, targetAgentIndex);

	private static bool IsSceneShoutObserverHistoryLine(string line) => MemoryBusinessStateOwner.IsSceneShoutObserverHistoryLine(line);

	private static string GetLatestSceneNpcDialogueUtteranceFallback(int targetAgentIndex) => MemoryHistoryCommitBannerlordAdapter.GetLatestSceneNpcDialogueUtteranceFallback(targetAgentIndex);

	public static string BuildHistoryContextForExternal(Hero hero, int maxLines = 20, string currentInput = null, string secondaryInput = null, bool includeCurrentActiveSceneSession = false)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildHistoryContext(hero, maxLines, currentInput, secondaryInput, includeCurrentActiveSceneSession);
		}
		catch
		{
			return "";
		}
	}

	public static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForExternal(Hero hero, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				return new List<ConversationMessage>();
			}
			return myBehavior.BuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession) ?? new List<ConversationMessage>();
		}
		catch
		{
			return new List<ConversationMessage>();
		}
	}

	public static List<ConversationMessage> BuildNonHeroUncompressedMemoryRoleMessagesForExternal(string nonHeroMemoryId, string npcName, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new List<ConversationMessage>();
			}
			string memoryName = string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim();
			return myBehavior.BuildUncompressedMemoryRoleMessagesById(nonHeroMemoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession) ?? new List<ConversationMessage>();
		}
		catch
		{
			return new List<ConversationMessage>();
		}
	}

	public static string BuildNonHeroHistoryContextForExternal(string nonHeroMemoryId, string npcName, int maxLines = 20, string currentInput = null, string secondaryInput = null, bool includeCurrentActiveSceneSession = false)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return "";
			}
			return myBehavior.BuildHistoryContextById(nonHeroMemoryId, npcName, maxLines, currentInput, secondaryInput, includeCurrentActiveSceneSession);
		}
		catch
		{
			return "";
		}
	}

	public static List<AnimusForgeDialogueHistoryEntry> GetDialogueHistoryEntriesForExternal(Hero hero, int maxLines = 260)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			return myBehavior.GetDialogueHistoryEntries(hero, maxLines);
		}
		catch
		{
			return new List<AnimusForgeDialogueHistoryEntry>();
		}
	}

	public static List<AnimusForgeDialogueHistoryEntry> GetDialogueHistoryEntriesByIdForExternal(string memoryId, int maxLines = 260)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return new List<AnimusForgeDialogueHistoryEntry>();
			}
			return myBehavior.GetDialogueHistoryEntriesById(memoryId, maxLines);
		}
		catch
		{
			return new List<AnimusForgeDialogueHistoryEntry>();
		}
	}

	private List<AnimusForgeDialogueHistoryEntry> GetDialogueHistoryEntries(Hero hero, int maxLines)
	{
        return _memoryHistoryCommit.GetDialogueHistoryEntries(hero, maxLines);
    }

	private List<AnimusForgeDialogueHistoryEntry> GetDialogueHistoryEntriesById(string memoryId, int maxLines)
	{
        return _memoryHistoryCommit.GetDialogueHistoryEntriesById(memoryId, maxLines);
    }

	private static void ClassifyDialogueHistoryLine(string line, out string speaker, out string text, out string kind)
	{
        MemoryHistoryCommitBannerlordAdapter.ClassifyDialogueHistoryLine(line, out speaker, out text, out kind);
    }

	private static int FindDialogueHistorySpeakerDelimiter(string line) => ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(line);

	private static bool IsLikelyPlayerHistorySpeaker(string speaker) => ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker);

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

	public static bool HasMeaningfulDialogueHistoryForExternal(Hero hero)
	{
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior != null && behavior.HasMeaningfulDialogueHistoryInternal(hero);
		}
		catch
		{
			return false;
		}
	}

	public static int GetLastMeaningfulDialogueDayForExternal(Hero hero)
	{
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior?.GetLastMeaningfulDialogueDayInternal(hero) ?? -1;
		}
		catch
		{
			return -1;
		}
	}

	public static bool TryGetLatestNpcRecentActionForExternal(Hero hero, out string stableKey, out string actionText, out int day)
	{
		stableKey = "";
		actionText = "";
		day = -1;
		try
		{
			MyBehavior behavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior != null && behavior.TryGetLatestNpcRecentActionInternal(hero, out stableKey, out actionText, out day);
		}
		catch
		{
			stableKey = "";
			actionText = "";
			day = -1;
			return false;
		}
	}

	public static bool TryGetLatestMeaningfulDialogueForExternal(Hero hero, out string stableKey, out string dialogueText, out int day)
	{
		stableKey = "";
		dialogueText = "";
		day = -1;
		try
		{
			MyBehavior behavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior != null && behavior.TryGetLatestMeaningfulDialogueInternal(hero, out stableKey, out dialogueText, out day);
		}
		catch
		{
			stableKey = "";
			dialogueText = "";
			day = -1;
			return false;
		}
	}

	public static bool TryGetLatestCompressedMemoryForExternal(Hero hero, out string stableKey, out string gameDate, out string memoryText, out int day)
	{
		stableKey = "";
		gameDate = "";
		memoryText = "";
		day = -1;
		try
		{
			MyBehavior behavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior != null && behavior.TryGetLatestCompressedMemoryInternal(hero, out stableKey, out gameDate, out memoryText, out day);
		}
		catch
		{
			stableKey = "";
			gameDate = "";
			memoryText = "";
			day = -1;
			return false;
		}
	}

	private bool HasMeaningfulDialogueHistoryInternal(Hero hero)
	{
		return GetLastMeaningfulDialogueDayInternal(hero) >= 0;
	}

	private int GetLastMeaningfulDialogueDayInternal(Hero hero) => _memoryHistoryCommit.GetLastMeaningfulDialogueDayInternal(hero);

	private bool TryGetLatestMeaningfulDialogueInternal(Hero hero, out string stableKey, out string dialogueText, out int day) => _memoryHistoryCommit.TryGetLatestMeaningfulDialogueInternal(hero, out stableKey, out dialogueText, out day);

	private bool TryGetLatestCompressedMemoryInternal(Hero hero, out string stableKey, out string gameDate, out string memoryText, out int day) => _memoryHistoryCommit.TryGetLatestCompressedMemoryInternal(hero, out stableKey, out gameDate, out memoryText, out day);

	private static bool IsMeaningfulDialogueLineForProactiveLetter(string line) => MemoryHistoryCommitBannerlordAdapter.IsMeaningfulDialogueLineForProactiveLetter(line);

	private bool TryGetLatestNpcRecentActionInternal(Hero hero, out string stableKey, out string actionText, out int day) => _memoryHistoryCommit.TryGetLatestNpcRecentActionInternal(hero, out stableKey, out actionText, out day);

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

	private static void AppendPlayerExtraFactLine(StringBuilder sb, string extraFact) => MemoryBusinessStateOwner.AppendPlayerExtraFactLine(sb, extraFact);

	private string BuildRecentNpcFactContext(Hero hero, int maxLines = 4)
    { return PromptRuleCaptureBannerlordAdapter.BuildRecentNpcFactContext(_memoryHistoryCommit.LoadDialogueHistory, hero, maxLines); }

	private string BuildGuardrailSemanticContext(Hero hero, string extraFact)
	{
        return PromptRuleCaptureBannerlordAdapter.BuildGuardrailSemanticContext(SharedRequestCapturePorts.LoadHistory, hero, extraFact);
    }

	private static string NormalizeGuardrailSemanticContextLine(string line)
	{
        return PromptRuleCaptureBannerlordAdapter.NormalizeGuardrailSemanticContextLine(line);
    }

	private static bool ShouldIncludeGuardrailSemanticContextLine(string line)
	{
        return PromptRuleCaptureBannerlordAdapter.ShouldIncludeGuardrailSemanticContextLine(line);
    }

	private static string ResolveTargetKingdomIdForRules(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
        return PromptRuleCaptureBannerlordAdapter.ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
    }

	private static string ResolveRuleTargetKey(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
        return PromptRuleCaptureBannerlordAdapter.ResolveRuleTargetKey(targetHero, targetCharacter, targetAgentIndex);
    }

	public static string BuildRuleTargetKeyForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		return ResolveRuleTargetKey(targetHero, targetCharacter, targetAgentIndex);
	}

	private static void AddPlayerCompanionOrFamilyRuleExclusionsForTarget(HashSet<string> excludedRuleIds, Hero targetHero, CharacterObject targetCharacter = null)
	{
        PromptRuleCaptureBannerlordAdapter.AddPlayerCompanionOrFamilyRuleExclusionsForTarget(excludedRuleIds, targetHero, targetCharacter);
    }

	private static void AddWorldMapCommandRuleExclusionForTarget(HashSet<string> excludedRuleIds, Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
        PromptRuleCaptureBannerlordAdapter.AddWorldMapCommandRuleExclusionForTarget(excludedRuleIds, targetHero, targetCharacter, targetAgentIndex);
    }

	private static bool ShouldExcludeWorldMapCommandRuleForTarget(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
        return PromptRuleCaptureBannerlordAdapter.ShouldExcludeWorldMapCommandRuleForTarget(targetHero, targetCharacter, targetAgentIndex);
    }

	private static void AddSceneMoveRuleExclusionForCurrentMission(HashSet<string> excludedRuleIds)
	{
        PromptRuleCaptureBannerlordAdapter.AddSceneMoveRuleExclusionForCurrentMission(excludedRuleIds);
    }

	private string ResolvePreselectedRuleInstructionBody(string ruleId, string body, bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		return PromptRuleCaptureBannerlordAdapter.CaptureSelectedRuleInstruction(ruleId, body, hasAnyHero, targetHero, targetCharacter, targetAgentIndex, NpcMajorRuleCapture);
	}

	private string BuildMatchedExtraRuleInstructionsFromPreselectedRules(IEnumerable<string> preselectedRuleIds, int maxRules, bool hasAnyHero, HashSet<string> excludedRuleIdSet, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		return ExtraRuleInstructionComposer.AssembleMatchedInstructions(PromptRuleCaptureBannerlordAdapter.CaptureMatchedPreselectedRuleInstructions(preselectedRuleIds, maxRules, hasAnyHero, excludedRuleIdSet, targetHero, targetCharacter, targetAgentIndex, NpcMajorRuleCapture));
	}

	private string BuildExtraRuleInstructions(string input, string npcLastUtterance, Hero targetHero, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preselectedRuleIds = null, List<GuardrailRuleHit> fallbackHits = null)
    {
        return PromptRuleCaptureBannerlordAdapter.BuildExtraRuleInstructions(NpcMajorRuleCapture, input, npcLastUtterance, targetHero, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIds, preselectedRuleIds, fallbackHits);
    }

	private static string BuildRestrictedRuleReferralHint(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, HashSet<string> excludedRuleIdSet)
    {
        return PromptRuleCaptureBannerlordAdapter.BuildRestrictedRuleReferralHint(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet);
    }

	private static bool AnyRestrictedReferralRuleBlocked(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, HashSet<string> excludedRuleIdSet, params string[] ruleIds)
    {
        return PromptRuleCaptureBannerlordAdapter.AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, ruleIds);
    }

	private static bool IsRestrictedReferralRuleBlocked(string ruleId, bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, HashSet<string> excludedRuleIdSet)
    {
        return PromptRuleCaptureBannerlordAdapter.IsRestrictedReferralRuleBlocked(ruleId, hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet);
    }

	private static void AddReferralTip(List<string> tips, string tip)
    {
        PromptRuleCaptureBannerlordAdapter.AddReferralTip(tips, tip);
    }

	private static string BuildNobleDeferenceRuntimeInstruction(bool hasAnyHero)
    {
        return PromptRuleCaptureBannerlordAdapter.BuildNobleDeferenceRuntimeInstruction(hasAnyHero);
    }

	private static bool IsSceneFollowingAgentForRules(int targetAgentIndex)
    {
        return PromptRuleCaptureBannerlordAdapter.IsSceneFollowingAgentForRules(targetAgentIndex);
    }

	private static string ReplaceSceneMechanismRuleForFollowing(string text)
    {
        return PromptRuleCaptureBannerlordAdapter.ReplaceSceneMechanismRuleForFollowing(text);
    }

	private static string AppendPlayerPartySharedResourcePrompt(string text, Hero targetHero, CharacterObject targetCharacter = null)
	{
        return SharedPromptCaptureBannerlordAdapter.AppendPlayerPartySharedResourcePrompt(text, targetHero, targetCharacter);
    }

	private static Agent ResolveDuelRuntimeTargetAgent(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
    {
        return PromptRuleCaptureBannerlordAdapter.ResolveDuelRuntimeTargetAgent(targetHero, targetCharacter, targetAgentIndex);
    }

	private static bool HasDuelRuntimeTarget(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
    {
        return PromptRuleCaptureBannerlordAdapter.HasDuelRuntimeTarget(targetHero, targetCharacter, targetAgentIndex);
    }

	private static string BuildDuelRuntimeInstruction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
    {
        return PromptRuleCaptureBannerlordAdapter.BuildDuelRuntimeInstruction(targetHero, targetCharacter, targetAgentIndex);
    }

	private string BuildTriggeredRuleInstructions(string input, Hero targetHero, bool useDuelContext, bool isQualified, int playerTier, bool useRewardContext, bool isLoanContext, bool isSurroundingsContext, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, string npcLastUtterance = null, bool includeDuelStakeContext = false, bool playerWonLastDuel = false, bool worldMapPartyCommandContext = false, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preselectedRuleIds = null, bool suppressForcedMeetingTaunt = false, List<GuardrailRuleHit> fallbackHits = null)
    {
        return PromptRuleCaptureBannerlordAdapter.BuildTriggeredRuleInstructions(NpcMajorRuleCapture, input, targetHero, useDuelContext, isQualified, playerTier, useRewardContext, isLoanContext, isSurroundingsContext, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, npcLastUtterance, includeDuelStakeContext, playerWonLastDuel, worldMapPartyCommandContext, excludedRuleIds, preselectedRuleIds, suppressForcedMeetingTaunt, fallbackHits);
    }

	/// <summary>
	/// Game-thread capture of every runtime rule body the triggered-rule block can contain. Bodies are
	/// resolved only when their topic applies (legacy laziness), so no extra Reward/Duel reads happen.
	/// Scheduled builds format worker-selected fallback hits here; synchronous compatibility callers
	/// may still select their own hits before the same game-thread runtime instruction capture.
	/// </summary>
	private PromptRuleInstructionSections CaptureRuleInstructionSections(string input, Hero targetHero, bool useDuelContext, bool isQualified, int playerTier, bool useRewardContext, bool isLoanContext, bool isSurroundingsContext, bool hasAnyHero, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, string npcLastUtterance, bool includeDuelStakeContext, bool playerWonLastDuel, bool worldMapPartyCommandContext, IEnumerable<string> excludedRuleIds, IEnumerable<string> preselectedRuleIds, bool suppressForcedMeetingTaunt, List<GuardrailRuleHit> fallbackHits)
    {
        return PromptRuleCaptureBannerlordAdapter.CaptureRuleInstructionSections(NpcMajorRuleCapture, input, targetHero, useDuelContext, isQualified, playerTier, useRewardContext, isLoanContext, isSurroundingsContext, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, npcLastUtterance, includeDuelStakeContext, playerWonLastDuel, worldMapPartyCommandContext, excludedRuleIds, preselectedRuleIds, suppressForcedMeetingTaunt, fallbackHits);
    }

	private static string ResolveRewardInstructionForPrompt(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter)
    {
        return PromptRuleCaptureBannerlordAdapter.ResolveRewardInstructionForPrompt(hasAnyHero, targetHero, targetCharacter);
    }

	private static string ResolveLoanInstructionForPrompt(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter)
    {
        return PromptRuleCaptureBannerlordAdapter.ResolveLoanInstructionForPrompt(hasAnyHero, targetHero, targetCharacter);
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

	public static bool IsDevDataManagementEnabledForExternal()
	{
		try
		{
			return DuelSettings.GetSettings()?.EnableDevEditHistory ?? false;
		}
		catch
		{
			return false;
		}
	}

	// The U-terminal uses this narrow bridge instead of exposing developer import menus or their broader data scopes.
	public static bool OpenDatabaseReloadFromTerminal(Action onReturn)
	{
		try
		{
			MyBehavior myBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return false;
			}
			myBehavior.OpenDatabaseReloadFolderPicker(onReturn);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DatabaseReload", "[WARN] Failed to open database reload terminal flow: " + ex.Message);
			return false;
		}
	}

	public static void OpenHeroPersonaEditorForExternal(Hero hero)
	{
		OpenHeroPersonaEditorForExternal(hero, null);
	}

	public static void OpenHeroPersonaEditorForExternal(Hero hero, Action onFinished)
	{
		try
		{
			if (!IsDevDataManagementEnabledForExternal())
			{
				InformationManager.DisplayMessage(new InformationMessage("开发者数据管理未开启（请在 MCM 中启用）。"));
				try
				{
					onFinished?.Invoke();
				}
				catch
				{
				}
				return;
			}
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				try
				{
					onFinished?.Invoke();
				}
				catch
				{
				}
				return;
			}
			myBehavior.OpenDevPersonaMenuFromExternal(hero, onFinished);
		}
		catch (Exception ex)
		{
			Logger.Log("EncyclopediaPersona", "[WARN] 打开 Hero 个性背景编辑失败: " + ex.Message);
			try
			{
				onFinished?.Invoke();
			}
			catch
			{
			}
		}
	}

	public static void OpenHeroPersonaRerollForExternal(Hero hero)
	{
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("当前无法为该 Hero 重新生成个性与背景。"));
				return;
			}
			_ = myBehavior.RunHeroPersonaRerollAsync(hero, null);
		}
		catch (Exception ex)
		{
			Logger.Log("EncyclopediaPersona", "[WARN] 打开 Hero 重生个性背景失败: " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("打开重生个性背景功能失败。"));
		}
	}

	public static void OpenHeroNpcEditorForExternal(Hero hero)
	{
		OpenHeroNpcEditorForExternal(hero, null);
	}

	public static void OpenHeroNpcEditorForExternal(Hero hero, Action onFinished)
	{
		try
		{
			if (!IsDevDataManagementEnabledForExternal())
			{
				InformationManager.DisplayMessage(new InformationMessage("开发者数据管理未开启（请在 MCM 中启用）。"));
				try
				{
					onFinished?.Invoke();
				}
				catch
				{
				}
				return;
			}
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				try
				{
					onFinished?.Invoke();
				}
				catch
				{
				}
				return;
			}
			myBehavior.OpenDevNpcEditorFromExternal(hero, onFinished);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcEditor", "[WARN] 打开 HeroNPC 编辑失败: " + ex.Message);
			try
			{
				onFinished?.Invoke();
			}
			catch
			{
			}
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

	public static bool TryGetNpcPersonaGenerationRuntimeStateForExternal(Hero hero, out bool active, out bool coolingDown)
	{
		active = false;
		coolingDown = false;
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || hero == null)
			{
				return false;
			}
			myBehavior.GetNpcPersonaGenerationRuntimeState(hero, out active, out coolingDown);
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

	private static string BuildHeroArmyRuntimeFactForPrompt(Hero hero)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildHeroArmyRuntimeFactForPrompt(hero);
	}

	private static string BuildPlayerArmyRuntimeFactForPrompt(Hero observerHero, CharacterObject observerCharacter, int targetAgentIndex)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerArmyRuntimeFactForPrompt(observerHero, observerCharacter, targetAgentIndex);
	}

	private static List<string> BuildArmyHeroListForPrompt(Army army, Hero perspectiveHero, Hero leaderHero, int maxCount, out int totalHeroCount)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildArmyHeroListForPrompt(army, perspectiveHero, leaderHero, maxCount, out totalHeroCount);
	}

	private static List<string> BuildArmyHeroListForPrompt(Army army, Hero perspectiveHero, Hero leaderHero, int maxCount, out int totalHeroCount, Hero displayOverrideHero, string displayOverrideName)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildArmyHeroListForPrompt(army, perspectiveHero, leaderHero, maxCount, out totalHeroCount, displayOverrideHero, displayOverrideName);
	}

	public static string BuildHeroArmyRuntimeFactForExternal(Hero hero)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildHeroArmyRuntimeFactForExternal(hero);
	}

	public static string BuildPlayerCrimeRatingPromptLineForExternal(IFaction perspectiveFaction)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerCrimeRatingPromptLineForExternal(perspectiveFaction);
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

	public static bool IsHeroActiveKingdomRulerForExternal(Hero hero)
	{
		return TryResolveActiveKingdomRuledByHeroForPrompt(hero, out Kingdom _);
	}

	public static string BuildPlayerCourierSenderIdentityForExternal()
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerCourierSenderIdentityForExternal();
	}

	public static string BuildPlayerCourierSenderIdentityForExternal(Hero observer)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerCourierSenderIdentityForExternal(observer);
	}

	public static string BuildPlayerCourierRecipientIdentityForExternal(Hero observer)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerCourierRecipientIdentityForExternal(observer);
	}

	private static string BuildPlayerCourierIdentityForExternal(Hero observer, string participantLabel, string formalAddressContext)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerCourierIdentityForExternal(observer, participantLabel, formalAddressContext);
	}

	public static string BuildPlayerVassalageRelationPromptLineForExternal(Hero observer, CharacterObject observerCharacter = null, string kingdomIdOverride = null, string counterpartKingdomLabel = "玩家所在王国", int targetAgentIndex = -1)
	{
		return ArmyIdentityPromptCaptureAdapter.BuildPlayerVassalageRelationPromptLineForExternal(observer, observerCharacter, kingdomIdOverride, counterpartKingdomLabel, targetAgentIndex);
	}

	private static Kingdom ResolveObserverKingdomForVassalagePrompt(Hero observer, CharacterObject observerCharacter, string kingdomIdOverride, int targetAgentIndex)
	{
		return ArmyIdentityPromptCaptureAdapter.ResolveObserverKingdomForVassalagePrompt(observer, observerCharacter, kingdomIdOverride, targetAgentIndex);
	}

	private static Kingdom ResolveAgentKingdomForVassalagePrompt(int targetAgentIndex)
	{
		return ArmyIdentityPromptCaptureAdapter.ResolveAgentKingdomForVassalagePrompt(targetAgentIndex);
	}

	private static Kingdom ResolveCurrentSettlementKingdomForVassalagePrompt()
	{
		return ArmyIdentityPromptCaptureAdapter.ResolveCurrentSettlementKingdomForVassalagePrompt();
	}

	private static Kingdom ResolveKingdomFromFactionForVassalagePrompt(IFaction faction)
	{
		return ArmyIdentityPromptCaptureAdapter.ResolveKingdomFromFactionForVassalagePrompt(faction);
	}

	private static Kingdom ResolvePlayerKingdomForVassalagePrompt()
	{
		return ArmyIdentityPromptCaptureAdapter.ResolvePlayerKingdomForVassalagePrompt();
	}

	public static void RecordNonHeroRecentActionForExternal(string nonHeroMemoryId, string npcName, string text, string stableKey, string actionKind = "")
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.RecordNonHeroRecentAction(nonHeroMemoryId, npcName, text, stableKey, actionKind);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordNonHeroRecentActionForExternal: " + ex.Message);
		}
	}

	private void RecordNonHeroRecentAction(string nonHeroMemoryId, string npcName, string text, string stableKey, string actionKind) => _campaignCharacterRecordCapture.RecordNonHeroRecentAction(nonHeroMemoryId, npcName, text, stableKey, actionKind);

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

	public static void RecordDuelResultForExternal(Hero targetHero, bool playerWon, string duelContext = null)
	{
		try
		{
			MyBehavior myBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				Logger.Log("NpcAction", "[WARN] RecordDuelResultForExternal skipped: MyBehavior unavailable.");
				return;
			}
			myBehavior.RecordDuelResult(targetHero, playerWon, duelContext);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordDuelResultForExternal: " + ex.Message);
		}
	}

	private void RecordDuelResult(Hero targetHero, bool playerWon, string duelContext)
	{
        ExternalActionObservations.RecordDuelResult(targetHero,playerWon,duelContext);
    }

	private static string BuildDuelResultLocationText(string duelContext)
	{
        return ExternalActionObservationBannerlordAdapter.BuildDuelResultLocationText(duelContext);
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

	public void RecordAnimusForgeSiegeInterventionForExternal(
		MobileParty attackerParty,
		Settlement settlement,
		SiegeAftermathAction.SiegeAftermath aftermath,
		Clan previousOwner,
		string trigger,
		string detail,
		int selectedSoldiers,
		int lootItemTotal,
		int lootStackKinds,
		int lootValue,
		int marketGoldLoot,
		int civilianGoldLoot,
		int civilianTargetsLooted,
		int killedCivilianUnits,
		int killedNotables,
		bool plunderStarted,
		bool massacreStarted)
	{
        ExternalActionObservations.RecordAnimusForgeSiegeInterventionForExternal(attackerParty,settlement,aftermath,previousOwner,trigger,detail,selectedSoldiers,lootItemTotal,lootStackKinds,lootValue,marketGoldLoot,civilianGoldLoot,civilianTargetsLooted,killedCivilianUnits,killedNotables,plunderStarted,massacreStarted);
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

	public static WeeklyPromptSnapshot CaptureWeeklyPromptSnapshotForExternal(Hero targetHero, CharacterObject targetCharacter = null, string kingdomIdOverride = null)
	{
        return WeeklyPromptCaptureAdapter.CaptureWeeklyPromptSnapshotForExternal(ResolveWeeklyCapturePorts, targetHero, targetCharacter, kingdomIdOverride);
    }

	public static ShoutPromptContext BuildShoutPromptContextForExternal(Hero targetHero, string input, string extraFact, string cultureIdOverride = null, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, bool suppressDynamicRuleAndLore = false, bool usePrefetchedLoreContext = false, string prefetchedLoreContext = null, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preprocessExcludedRuleIds = null, IEnumerable<string> forcedPreprocessRuleIds = null, MentionedWorldEntities preprocessMentionedEntities = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null)
	{
		try
		{
			MyBehavior myBehavior = (weeklyPromptSnapshot != null || !TWParallel.IsMainThread()) ? Instance : Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return SharedPromptCaptureBannerlordAdapter.CreateEmptyShoutPromptContext();
			}
			return myBehavior.BuildShoutPromptContextForExternalInternal(targetHero, input, extraFact, cultureIdOverride, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext, excludedRuleIds, preprocessExcludedRuleIds, forcedPreprocessRuleIds, preprocessMentionedEntities, weeklyPromptSnapshot);
		}
		catch (PreprocessFormatException)
		{
			throw;
		}
		catch
		{
			return SharedPromptCaptureBannerlordAdapter.CreateEmptyShoutPromptContext();
		}
	}

	internal static void LogShoutPromptContextStage(string stage, Stopwatch totalSw, Stopwatch stageSw, PromptBuildRequest request, string detail = null, bool immediate = true)
	{
		LogShoutPromptContextStage(stage, totalSw, stageSw, request?.TargetHeroId ?? request?.TargetCharacterId, request?.TargetAgentIndex ?? -1, detail, immediate);
	}

	private static void LogShoutPromptContextStage(string stage, Stopwatch totalSw, Stopwatch stageSw, string targetId, int targetAgentIndex, string detail, bool immediate)
	{
        SharedPromptCaptureBannerlordAdapter.LogShoutPromptContextStage(stage, totalSw, stageSw, targetId, targetAgentIndex, detail, immediate);
    }

	private static void LogShoutPromptContextStage(string stage, Stopwatch totalSw, Stopwatch stageSw, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string detail = null, bool immediate = true)
	{
        SharedPromptCaptureBannerlordAdapter.LogShoutPromptContextStage(stage, totalSw, stageSw, targetHero, targetCharacter, targetAgentIndex, detail, immediate);
    }

	public static List<string> RunCourierRulePreprocessForExternal(Hero targetHero, string input, string extraFact, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, IEnumerable<string> excludedRuleIds = null)
	{
		MentionedWorldEntities ignoredMentionedEntities;
		return RunCourierRulePreprocessForExternal(targetHero, input, extraFact, out ignoredMentionedEntities, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIds);
	}

	public static List<string> RunCourierRulePreprocessForExternal(Hero targetHero, string input, string extraFact, out MentionedWorldEntities mentionedEntities, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, IEnumerable<string> excludedRuleIds = null)
	{
		List<string> result = new List<string>();
		mentionedEntities = new MentionedWorldEntities();
		try
		{
			MyBehavior myBehavior = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null || string.IsNullOrWhiteSpace(input))
			{
				return result;
			}
			return myBehavior.RunCourierRulePreprocessInternal(targetHero, input, extraFact, out mentionedEntities, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIds);
		}
		catch (PreprocessFormatException)
		{
			throw;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("CourierDelivery", "[Preprocess] failed: " + ex.Message);
			}
			catch
			{
			}
			mentionedEntities = new MentionedWorldEntities();
			return result;
		}
	}

	private List<string> RunCourierRulePreprocessInternal(Hero targetHero, string input, string extraFact, out MentionedWorldEntities mentionedEntities, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, IEnumerable<string> excludedRuleIds)
	{
		return SharedPromptCaptureBannerlordAdapter.RunCourierRulePreprocessInternal(SharedRequestCapturePorts, targetHero, input, extraFact, out mentionedEntities, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIds);
	}

	/// <summary>Detached input for the Courier preprocess retrieval; captured on the game thread.</summary>
	internal sealed class CourierPreprocessRequest
	{
		internal string Input;
		internal string TargetHeroId;
		internal string TargetCharacterId;
		internal PromptRuntimeTargetBinding Target;
		internal PromptRuleEligibility Eligibility;
		internal HashSet<string> ExcludedRuleIds;
		internal string GuardrailSemanticContext;
		internal string NpcLastUtterance;
	}

	/// <summary>Step 1 (game thread): exclusion set, target binding, history context. Null when the GCCZ scene bypasses preprocess.</summary>
	internal CourierPreprocessRequest BeginCourierRulePreprocess(Hero targetHero, string input, string extraFact, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, IEnumerable<string> excludedRuleIds)
	{
		return SharedPromptCaptureBannerlordAdapter.BeginCourierRulePreprocess(SharedRequestCapturePorts, targetHero, input, extraFact, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIds);
	}

	/// <summary>Step 2 (any thread): semantic rule retrieval on the detached request. Caller applies the target binding to the ambient context.</summary>
	internal List<string> RunCourierRulePreprocessRetrieval(CourierPreprocessRequest request, out MentionedWorldEntities mentionedEntities)
	{
		return SharedPromptCaptureBannerlordAdapter.RunCourierRulePreprocessRetrieval(request, out mentionedEntities);
	}

	public static string BuildHeroPrisonerStatusPromptLineForExternal(Hero hero)
	{ return SharedPromptCaptureBannerlordAdapter.BuildHeroPrisonerStatusPromptLineForExternal(hero); }

	public static bool WasHeroDefeatedByPlayerForExternal(Hero hero)
	{
		try
		{
			return hero != null
				&& !string.IsNullOrWhiteSpace(hero.StringId)
				&& Instance?._recentlyDefeatedByPlayer?.Contains(hero.StringId) == true;
		}
		catch
		{
			return false;
		}
	}

	// Primary runtime chat path: scene shout / non-native conversation UI.
	// The build is five schedulable steps: Begin (game thread) -> Routing (any thread) ->
	// Knowledge snapshot (game thread) -> Knowledge retrieval (any thread) -> Complete (game thread).
	// Channels with their own schedulers (Native, Courier) call the steps directly and
	// re-validate ownership between them; this method is the sequential composition for the rest.
	private ShoutPromptContext BuildShoutPromptContextForExternalInternal(Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, bool suppressDynamicRuleAndLore = false, bool usePrefetchedLoreContext = false, string prefetchedLoreContext = null, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preprocessExcludedRuleIds = null, IEnumerable<string> forcedPreprocessRuleIds = null, MentionedWorldEntities preprocessMentionedEntities = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null)
	{
        return SharedPromptCaptureBannerlordAdapter.BuildShoutPromptContextForExternalInternal(ExternalPromptBuildCapture, targetHero, input, extraFact, cultureIdOverride, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext, excludedRuleIds, preprocessExcludedRuleIds, forcedPreprocessRuleIds, preprocessMentionedEntities, weeklyPromptSnapshot);
    }

	internal static ShoutPromptContext CreateEmptyShoutPromptContext()
	{
        return SharedPromptCaptureBannerlordAdapter.CreateEmptyShoutPromptContext();
    }

	/// <summary>
	/// Step 1 (game thread): capture the detached request, publish the exclusion lists and the
	/// preprocess-excluded block. Returns null for an empty input (legacy early return).
	/// Callers own the guardrail runtime scope; this step does not touch ambient retrieval context.
	/// </summary>
	internal PromptBuildPhases BeginSharedPromptBuild(Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext, IEnumerable<string> excludedRuleIds, IEnumerable<string> preprocessExcludedRuleIds, IEnumerable<string> forcedPreprocessRuleIds, MentionedWorldEntities preprocessMentionedEntities)
	{
        return SharedPromptCaptureBannerlordAdapter.BeginSharedPromptBuild(SharedRequestCapturePorts, targetHero, input, extraFact, cultureIdOverride, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext, excludedRuleIds, preprocessExcludedRuleIds, forcedPreprocessRuleIds, preprocessMentionedEntities);
    }

	/// <summary>
	/// Step 2 (any thread): topic routing plus mention retrieval. Reads only the detached request,
	/// configuration and caches; may call the auxiliary router / ONNX. The caller must have applied
	/// the request target to the ambient retrieval context on this thread (see ApplyGuardrailRuntimeTarget).
	/// </summary>
	internal void RunSharedPromptRouting(PromptBuildPhases phases) => SharedPromptRoutingRuntime.Run(CaptureSharedPromptRoutingWork(phases));

	/// <summary>Game thread: prepare the Lore index and capture entity candidates after routing supplied all mentions.</summary>
	internal void CaptureSharedKnowledgeSnapshot(PromptBuildPhases phases, Hero targetHero)
	{
        SharedPromptCaptureBannerlordAdapter.CaptureSharedKnowledgeSnapshot(phases, targetHero);
    }

	/// <summary>Game thread: detach only the fields needed by the Knowledge worker.</summary>
	internal static PromptKnowledgeWorkInput CreateSharedKnowledgeWorkInput(PromptBuildPhases phases)
	{
        return SharedPromptCaptureBannerlordAdapter.CreateSharedKnowledgeWorkInput(phases);
    }

	/// <summary>Worker: select candidates from a detached input, never from the mixed game capture.</summary>
	internal static PromptKnowledgeWorkResult RunSharedKnowledgeRetrieval(PromptKnowledgeWorkInput input)
	{
        return SharedPromptCaptureBannerlordAdapter.RunSharedKnowledgeRetrieval(input);
    }

	internal static void ApplySharedKnowledgeRetrieval(PromptBuildPhases phases, PromptKnowledgeWorkResult result)
	{
        SharedPromptCaptureBannerlordAdapter.ApplySharedKnowledgeRetrieval(phases, result);
    }

	// Compatibility for synchronous Scene and setter-only callers; scheduled channels use the detached overload.
	internal void RunSharedKnowledgeRetrieval(PromptBuildPhases phases)
	{
        SharedPromptCaptureBannerlordAdapter.RunSharedKnowledgeRetrieval(phases);
    }

	/// <summary>
	/// Step 3 (game thread): section capture, pure assembly and runtime appendices. The caller must
	/// have re-validated ownership/generation before invoking it after a thread hop.
	/// </summary>
	internal ShoutPromptContext CompleteSharedPromptBuild(PromptBuildPhases phases, Hero targetHero, CharacterObject targetCharacter, WeeklyPromptSnapshot weeklyPromptSnapshot)
	{
        return SharedPromptCaptureBannerlordAdapter.CompleteSharedPromptBuild(CreatePromptContextCapturePorts(phases.Request, phases.Routing, phases.Retrieval, targetHero ?? targetCharacter?.HeroObject, targetCharacter, phases.TotalStopwatch, phases.StageStopwatch), phases, targetHero, targetCharacter, weeklyPromptSnapshot);
    }

	/// <summary>Phase 1: identity, exclusion sets, qualification, history context. Game-thread reads only; no network.</summary>
	private PromptBuildRequest CapturePromptBuildRequest(Hero targetHero, CharacterObject targetCharacter, string input, string extraFact, string cultureIdOverride, string kingdomIdOverride, int targetAgentIndex, bool hasAnyHero, bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext, IEnumerable<string> excludedRuleIds, IEnumerable<string> preprocessExcludedRuleIds, IEnumerable<string> forcedPreprocessRuleIds)
	{
        return SharedPromptCaptureBannerlordAdapter.CapturePromptBuildRequest(SharedRequestCapturePorts, targetHero, targetCharacter, input, extraFact, cultureIdOverride, kingdomIdOverride, targetAgentIndex, hasAnyHero, suppressDynamicRuleAndLore, usePrefetchedLoreContext, prefetchedLoreContext, excludedRuleIds, preprocessExcludedRuleIds, forcedPreprocessRuleIds);
    }

	private static PromptRoutingInput CreatePromptRoutingInput(PromptBuildRequest request) => SharedPromptRoutingRuntime.CaptureInput(request);

	private static void LogPromptRoutingDiagnostics(PromptBuildRequest request, PromptRoutingResult routing, PromptRoutingInput routingInput)
	{
        SharedPromptRoutingRuntime.LogPromptRoutingDiagnostics(request, routing, routingInput);
    }

	/// <summary>Phase 3: context flags, mentions, lore and every Extras section, captured in legacy order. Game-thread reads.</summary>

	/// <summary>Phase 5: team-module runtime prompt (GCCZ) and shared party resource appendices; diagnostics. Game-thread reads.</summary>
	private void ApplyPromptRuntimeAppendices(ShoutPromptContext shoutPromptContext, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string cultureIdOverride, Stopwatch promptContextTotalSw, Stopwatch promptContextStageSw)
	{
        SharedPromptCaptureBannerlordAdapter.ApplyPromptRuntimeAppendices(shoutPromptContext, targetHero, targetCharacter, targetAgentIndex, cultureIdOverride, promptContextTotalSw, promptContextStageSw);
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

	private void AppendLoreToHistory(Hero hero, string loreText) => _memoryHistoryCommit.AppendLoreToHistory(hero, loreText);

	private static bool IsPlayerTurnStartLine(string line) => MemoryBusinessStateOwner.IsPlayerTurnStartLine(line);


	private static bool IsSingleUseNpcFactLine(string line) => MemoryHistoryCommitBannerlordAdapter.IsSingleUseNpcFactLine(line);

	private static bool IsFirstMeetingNpcFactLine(string line)
	{
        return MemoryHistoryCommitBannerlordAdapter.IsFirstMeetingNpcFactLine(line);
    }

	private static string NormalizeFirstMeetingNpcFactForPrompt(string line)
	{
        return MemoryHistoryCommitBannerlordAdapter.NormalizeFirstMeetingNpcFactForPrompt(line);
    }

	private static bool IsFirstMeetingNpcFactBody(string text) => MemoryHistoryCommitBannerlordAdapter.IsFirstMeetingNpcFactBody(text);

	private static bool IsMeaningfulDirectConversationLine(string line) => MemoryBusinessStateOwner.IsMeaningfulDirectConversationLine(line);

	private static bool IsMeaningfulConversationLine(string line, bool includeActiveScene) => MemoryBusinessStateOwner.IsMeaningfulConversationLine(line, includeActiveScene, MemoryHistoryCommitBannerlordAdapter.IsActiveSceneSessionHistoryLine);

	private static bool RemoveExpiredSingleUseNpcFactLines(List<DialogueDay> records) => MemoryHistoryCommitBannerlordAdapter.RemoveExpiredSingleUseNpcFactLines(records);

	private static bool IsLoreInjectionHistoryLine(string line) => MemoryBusinessStateOwner.IsLoreInjectionHistoryLine(line);









	private static string StripSpeakerPrefixForRecall(string line) => MemoryHistoryCommitBannerlordAdapter.StripSpeakerPrefixForRecall(line);







	private static List<string> BuildRenderedHistoryLines(List<HistoryLineEntry> entries, string targetDisplayName = null, bool addressToYou = true) => MemoryBusinessStateOwner.BuildRenderedHistoryLines(entries, targetDisplayName, addressToYou);






	private static bool IsCurrentInputPlayerLine(string line, string currentInput) => MemoryBusinessStateOwner.IsCurrentInputPlayerLine(line, currentInput);


	private static string StripMemoryTitleDateTime(string title)
	{
		return MemoryRecordRules.StripMemoryTitleDateTime(title);
	}


	private static string StripJsonResponseEnvelope(string content)
	{
		return JsonResponseTextCodec.StripJsonResponseEnvelope(content);
	}



	private static List<string> ExtractJsonObjectPayloads(string text)
	{
		return JsonResponseTextCodec.ExtractJsonObjectPayloads(text);
	}



	private static bool TryParseBestSummaryJsonObject(string content, string[] requiredPrimaryKeys, string[] requiredSecondaryKeys, out JObject obj, out string error) => MemoryBusinessStateOwner.TryParseBestSummaryJsonObject(content, requiredPrimaryKeys, requiredSecondaryKeys, out obj, out error);

	private static bool TryParseTaggedSummaryObject(string content, string[] requiredPrimaryKeys, string[] requiredSecondaryKeys, out JObject obj) => MemoryBusinessStateOwner.TryParseTaggedSummaryObject(content, requiredPrimaryKeys, requiredSecondaryKeys, out obj);

	private static void AddTaggedSummaryProperty(string text, JObject obj, string propertyName, string[] tagNames) => MemoryBusinessStateOwner.AddTaggedSummaryProperty(text, obj, propertyName, tagNames);

	private static bool TryExtractTaggedBlock(string text, string tagName, out string value) => MemoryBusinessStateOwner.TryExtractTaggedBlock(text, tagName, out value);

	private static bool TryParseLooseSummaryJsonObject(string content, string[] requiredPrimaryKeys, string[] requiredSecondaryKeys, out JObject obj) => MemoryBusinessStateOwner.TryParseLooseSummaryJsonObject(content, requiredPrimaryKeys, requiredSecondaryKeys, out obj);

	private static void AddLooseJsonStringProperties(string text, JObject obj, string[] names) => MemoryBusinessStateOwner.AddLooseJsonStringProperties(text, obj, names);

	private static bool TryExtractLooseJsonStringProperty(string text, string propertyName, out string value)
	{
		return JsonResponseTextCodec.TryExtractLooseJsonStringProperty(text, propertyName, out value);
	}

	private static int SkipJsonWhitespace(string text, int index)
	{
		return JsonResponseTextCodec.SkipJsonWhitespace(text, index);
	}

	private static bool TryReadLooseJsonStringValue(string text, int quoteIndex, out string value)
	{
		return JsonResponseTextCodec.TryReadLooseJsonStringValue(text, quoteIndex, out value);
	}

	private static string BuildRequiredJsonFieldDescription(string[] primaryKeys, string[] secondaryKeys) => MemoryBusinessStateOwner.BuildRequiredJsonFieldDescription(primaryKeys, secondaryKeys);

	private static string BuildRequiredJsonFieldGroupDescription(string[] keys) => MemoryBusinessStateOwner.BuildRequiredJsonFieldGroupDescription(keys);

	private static bool HasAnyNonWhiteSpaceJsonProperty(JObject obj, string[] names) => MemoryBusinessStateOwner.HasAnyNonWhiteSpaceJsonProperty(obj, names);

	private static bool IsEmptySummaryMarker(string text)
	{
		return MemorySummaryRules.IsEmptyMarker(text);
	}

	private static string GetJsonStringIgnoreCase(JObject obj, params string[] names)
	{
		return JsonResponseTextCodec.GetJsonStringIgnoreCase(obj, names);
	}

	private static JToken GetJsonPropertyIgnoreCase(JObject obj, params string[] names)
	{
		return JsonResponseTextCodec.GetJsonPropertyIgnoreCase(obj, names);
	}

	private static string BuildSummaryJsonParseFailureMessage(string prefix, string parseError, string content) => MemoryBusinessStateOwner.BuildSummaryJsonParseFailureMessage(prefix, parseError, content);

	private static string FormatMemoryHourRange(int startHour, int endHour)
	{
		return MemoryRecallContextOwner.FormatMemoryHourRange(startHour, endHour);
	}

	private static string FormatCompressedMemoryAgeSuffix(CompressedMemoryBlock block, int? capturedDay = null)
	{
		return MemoryRecallContextOwner.FormatCompressedMemoryAgeSuffix(block, capturedDay ?? GetCurrentGameDayIndexSafe());
	}

	private static string BuildDailyMemoryLineForPrompt(DailyMemoryLine line) => MemorySummaryApplicationAdapter.BuildDailyMemoryLineForPrompt(line, ResolveMemoryLineSceneForPrompt);

	private static string ResolveMemoryLineSceneForPrompt(DailyMemoryLine line) => ResolveCapturedMemoryLineSceneForPrompt(line);

	private static int CountDailyMemorySummarySourceChars(DailyMemoryDraft draft) => MemorySummaryPlanningRules.CountDailySourceChars(draft);

	private static bool HasDailyMemoryDraftAfefLines(DailyMemoryDraft draft)
	{
		return MemorySummaryPlanningRules.HasAfef(draft);
	}





	private static bool TryParseMemoryPreprocessIds(string content, IEnumerable<int> allowedIds, int finalCount, out List<int> selectedIds, out string error)
	{
		return MemoryRecallContextOwner.TryParseMemoryPreprocessIds(content, allowedIds, finalCount, out selectedIds, out error);
	}

	private static string BuildMemoryPreprocessFormatError(string reason, string content)
	{
		return MemoryRecallContextOwner.BuildMemoryPreprocessFormatError(new MemoryRecallRequest { FormatFailureDetail = (reasonText, reply) => LlmRetryPrompt.BuildFailureDetail(reasonText, reply) }, reason, content);
	}

	private void ShowCompressedMemoryBlockingPopup(string title, string message, long runtimeGeneration)
    {
        PublishMemoryFailureNotice(title, message, runtimeGeneration);
    }

	private List<ConversationMessage> BuildUncompressedMemoryRoleMessages(Hero hero, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false) => CaptureAndBuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);

	private List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById(string memoryId, string memoryName, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false) => CaptureAndBuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);






	private static int GetCurrentSceneSessionIdForDailyMemorySuppression() => MemoryHistoryCommitBannerlordAdapter.GetCurrentSceneSessionIdForDailyMemorySuppression();


	private string BuildMemoryOverviewContext(Hero hero)
	{
		return BuildMemoryOverviewContextById(GetMemoryHeroId(hero));
	}

	private string BuildMemoryOverviewContextById(string memoryId) => MemorySummaryApplicationAdapter.BuildMemoryOverviewContextById(MemoryQueueState, memoryId, MemoryEntityIdentityBannerlordAdapter.IsMemoryEntityEligibleForCompressedMemory, LlmRequestConfigurationCaptureAdapter.GetMemoryOverviewStartBlockCountFromSettings);

	private string BuildCompressedMemoryContext(Hero hero, string currentInput, string secondaryInput)
	{
		return BuildCompressedMemoryContextById(GetMemoryHeroId(hero), currentInput, secondaryInput);
	}



	private string BuildHistoryContext(Hero hero, int maxLines = 0, string currentInput = null, string secondaryInput = null, bool includeCurrentActiveSceneSession = false)
	{
		if (hero == null)
		{
			return "";
		}
		return BuildHistoryContextById(GetMemoryHeroId(hero), hero.Name?.ToString() ?? "NPC", maxLines, currentInput, secondaryInput, includeCurrentActiveSceneSession);
	}

	private string BuildHistoryContextById(string memoryId, string memoryName, int maxLines = 0, string currentInput = null, string secondaryInput = null, bool includeCurrentActiveSceneSession = false, HistoryPromptSnapshot snapshot = null) => _memoryBusinessState.BuildHistoryContextById(_memoryHistoryContext, memoryId, memoryName, maxLines, currentInput, secondaryInput, includeCurrentActiveSceneSession, snapshot);

	private static bool TryGetPrimaryUniversalApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName)
	{
		return LlmRequestConfigurationCaptureAdapter.TryGetPrimaryUniversalApiConfig(settings, out effectiveApiUrl, out apiKey, out modelName);
	}

	private static bool TryGetEventAndRebellionDedicatedApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName, out bool hasAnyField)
	{
		return LlmRequestConfigurationCaptureAdapter.TryGetEventAndRebellionDedicatedApiConfig(settings, out effectiveApiUrl, out apiKey, out modelName, out hasAnyField);
	}

	private static bool TryGetAuxiliaryDedicatedApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName, out bool hasAnyField)
	{
		return LlmRequestConfigurationCaptureAdapter.TryGetAuxiliaryDedicatedApiConfig(settings, out effectiveApiUrl, out apiKey, out modelName, out hasAnyField);
	}

	private static bool TryResolveUniversalApiConfig(DuelSettings settings, UniversalApiRoute route, out string effectiveApiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage)
	{
		return LlmRequestConfigurationCaptureAdapter.TryResolveUniversalApiConfig(settings, (ConfiguredChatRoute)(int)route, out effectiveApiUrl, out apiKey, out modelName, out resolvedRoute, out errorMessage);
	}

	private static string TrimUniversalApiRawForLog(string text, int maxChars = 3000)
	{
		return ConfiguredChatApplicationAdapter.TrimUniversalApiRawForLog(text, maxChars);
	}

	private static bool LooksLikeUniversalThinkingControlError(string responseBody)
	{
		return ConfiguredChatApplicationAdapter.LooksLikeUniversalThinkingControlError(responseBody);
	}

	private static void ResolveUniversalThinkingSettings(DuelSettings settings, string resolvedRoute, out bool thinkingEnabled, out string effort)
	{
		LlmRequestConfigurationCaptureAdapter.ResolveUniversalThinkingSettings(settings, resolvedRoute, out thinkingEnabled, out effort);
	}

	private static float ResolveUniversalApiTemperature(DuelSettings settings, string resolvedRoute)
	{
		return LlmRequestConfigurationCaptureAdapter.ResolveUniversalApiTemperature(settings, resolvedRoute);
	}

	private static int ResolveUniversalMaxTokens(DuelSettings settings, string resolvedRoute)
	{
		return LlmRequestConfigurationCaptureAdapter.ResolveUniversalMaxTokens(settings, resolvedRoute);
	}

	private static string ExtractUniversalGeminiCandidateText(JToken candidate)
	{
		return ConfiguredChatApplicationAdapter.ExtractUniversalGeminiCandidateText(candidate);
	}

	private static string ExtractUniversalStreamDelta(JObject json)
	{
		return ConfiguredChatApplicationAdapter.ExtractUniversalStreamDelta(json);
	}

	private static bool IsUniversalStreamNonContentChunk(JObject json)
	{
		return ConfiguredChatApplicationAdapter.IsUniversalStreamNonContentChunk(json);
	}

	private static string ExtractUniversalContentTokenText(JToken token)
	{
		return ConfiguredChatApplicationAdapter.ExtractUniversalContentTokenText(token);
	}

	private static string ExtractUniversalNonStreamContent(JObject json)
	{
		return ConfiguredChatApplicationAdapter.ExtractUniversalNonStreamContent(json);
	}

	private Task<ApiCallResult> CallWeeklyReportApiDetailed(string systemPrompt, string userPrompt)
	{
		return ConfiguredChatApplicationAdapter.CallWeeklyReportApiDetailed(systemPrompt, userPrompt);
	}

	private Task<ApiCallResult> CallRebelKingdomNamingGatewayDetailed(string systemPrompt, string userPrompt)
	{
		return ConfiguredChatApplicationAdapter.CallRebelKingdomNamingGatewayDetailed(systemPrompt, userPrompt);
	}

	private Task<ApiCallResult> CallAuxiliaryGatewayDetailed(string systemPrompt, string userPrompt, string source, int maxTokens, bool forceThinkingDisabled)
	{
		return ConfiguredChatApplicationAdapter.CallAuxiliaryGatewayDetailed(systemPrompt, userPrompt, source, maxTokens, forceThinkingDisabled);
	}

	private Task<ApiCallResult> CallUniversalApiDetailed(string sys, string user, bool logToEventLogs = false, string eventLogSource = "EventWeeklyReport", UniversalApiRoute route = UniversalApiRoute.Main, bool streamResponse = true, bool forceThinkingDisabled = false)
	{
		return ConfiguredChatApplicationAdapter.CallUniversalApiDetailed(sys, user, logToEventLogs, eventLogSource, (ConfiguredChatRoute)(int)route, streamResponse, forceThinkingDisabled);
	}

	private Task<string> CallUniversalApi(string sys, string user)
	{
		return ConfiguredChatApplicationAdapter.CallUniversalApi(sys, user);
	}

	public static Task<string> CallAuxiliaryApiTextForExternal(string sys, string user, string source = "ExternalAuxiliary")
	{
		return ConfiguredChatApplicationAdapter.CallAuxiliaryApiTextForExternal(sys, user, source, () => (Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>()) != null);
	}

	private static string StripActionTags(string text) => MemoryBusinessStateOwner.StripActionTags(text);

	private string CleanAIResponse(string input) => MemoryBusinessStateOwner.CleanAIResponse(input);

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





private static int GetRelationWithPlayerSafe(Hero hero)
	{
		return PatienceResponseBannerlordAdapter.GetRelationWithPlayerSafe(hero);
	}

	private static int ComputePatienceMaxFromRelation(int relation)
	{
		return PatienceRules.ComputePatienceMaxFromRelation(relation);
	}

	private static int ToTenLevelIndexByRatio(float current, int max)
	{
		return PatienceRules.ToTenLevelIndexByRatio(current, max);
	}

	private static int ToTenLevelIndexByRelation(int relation)
	{
		return PatienceRules.ToTenLevelIndexByRelation(relation);
	}

	private static string GetPatienceLevelText(float current, int max)
	{
		return PatienceRules.GetPatienceLevelText(current, max);
	}

	private static string GetRelationLevelText(int relation)
	{
		return PatienceRules.GetRelationLevelText(relation);
	}

	private static int GetRelationLevelIndex(int relation)
	{
		return PatienceRules.GetRelationLevelIndex(relation);
	}

private static void FillTrustSnapshot(ref PatienceSnapshot snap, Hero hero)
	{
		PatienceResponseBannerlordAdapter.FillTrustSnapshot(ref snap, hero);
	}








private static void FillSettlementMerchantTrustSnapshot(ref PatienceSnapshot snap, CharacterObject character)
	{
		PatienceResponseBannerlordAdapter.FillSettlementMerchantTrustSnapshot(ref snap, character);
	}

	private static bool ShouldOmitClanRelationFromPrompt(Hero targetHero)
	{
        return PersonaIdentityPromptCaptureAdapter.ShouldOmitClanRelationFromPrompt(targetHero);
    }

	private static string BuildCompactStateLine(PatienceSnapshot snap, bool includeClanRelation = true)
	{
		return PatiencePromptProjectionComposer.BuildCompactStateLine(snap, includeClanRelation);
	}

	private static string BuildSceneInlineStateLine(PatienceSnapshot snap, bool includeClanRelation = true)
	{
		return PatiencePromptProjectionComposer.BuildSceneInlineStateLine(snap, PersonaIdentityPromptCaptureAdapter.CapturePlayerPronoun(), includeClanRelation && string.IsNullOrWhiteSpace(snap.RelationLevel) ? GetRelationLevelText(snap.Relation) : snap.RelationLevel, includeClanRelation);
	}

	private static string BuildSceneInlineStateText(PatienceSnapshot snap, bool includeRelationPenalty, bool includeClanRelation = true)
	{
		return PatiencePromptProjectionComposer.BuildSceneInlineStateText(snap, includeRelationPenalty, PersonaIdentityPromptCaptureAdapter.CapturePlayerPronoun(), snap.Current <= 0.01f ? PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt() : null, includeClanRelation && string.IsNullOrWhiteSpace(snap.RelationLevel) ? GetRelationLevelText(snap.Relation) : snap.RelationLevel, includeClanRelation);
	}

	private static string BuildHeroPatienceKey(Hero hero) => PatienceRules.HeroKey(hero?.StringId);

	private static string BuildUnnamedPatienceKey(string unnamedKey, string npcName) => PatienceRules.UnnamedKey(unnamedKey, npcName);



private PatienceSnapshot GetHeroPatienceSnapshot(Hero hero)
	{
		return PatienceResponses.GetHeroSnapshot(hero);
	}

private PatienceSnapshot GetUnnamedPatienceSnapshot(string unnamedKey, string npcName, string displayName = null)
	{
		return PatienceResponses.GetUnnamedSnapshot(unnamedKey, npcName, displayName);
	}

	private static PatienceMood ParseMoodToken(string token)
	{
		return PatienceRules.ParseMoodToken(token);
	}

	private static PatienceMood ExtractMoodAndStripTag(ref string text)
	{
		return PatienceRules.ExtractMoodAndStripTag(ref text);
	}

	private static int ComputePatienceDelta(PatienceMood mood, ref int noInterestRounds)
	{
		return PatienceRules.ComputePatienceDelta(mood, ref noInterestRounds);
	}

	private static int ComputeNativeRelationDelta(PatienceMood mood, int currentRelation)
	{
		return PatienceRules.ComputeNativeRelationDelta(mood, currentRelation);
	}

	private static int ComputePrivateLoveDelta(PatienceMood mood)
	{
		return PatienceRules.ComputePrivateLoveDelta(mood);
	}

	private static int ComputeRoyalDomainConversationLoyaltyDelta(PatienceMood mood)
	{
		return PatienceRules.ComputeRoyalDomainConversationLoyaltyDelta(mood);
	}

private static Settlement GetCurrentRoyalDomainConversationSettlement()
	{
		return PatienceResponseBannerlordAdapter.GetCurrentRoyalDomainConversationSettlement();
	}

private static bool CanApplyRoyalDomainConversationLoyaltyForHero(Hero targetHero)
	{
		return PatienceResponseBannerlordAdapter.CanApplyRoyalDomainConversationLoyaltyForHero(targetHero);
	}

private static void ApplyRoyalDomainConversationLoyaltyFromMood(PatienceMood mood, Hero targetHero, string unnamedKey, string npcName, bool directConversation)
	{
		MyBehavior owner=Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>(); owner?.PatienceResponses.ApplyRoyalDomainConversationLoyaltyFromMood(mood, targetHero, unnamedKey, npcName, directConversation);
	}

	private bool ApplyPatienceDeltaInternal(string key, int maxPatience, PatienceMood mood, out int before, out int after)
	{
		var result = _patienceOwner.Apply(key, maxPatience, GetNowCampaignDay(), mood);
		before = result.Before; after = result.After;
		return result.BecameExhausted;
	}

	private bool ApplyPatiencePostprocessMoodOverrideInternal(string key, int maxPatience, PatienceMood mood, out int before, out int after, out int delta)
	{
		var result = _patienceOwner.OverrideNeutral(key, maxPatience, GetNowCampaignDay(), mood);
		before = result.Before; after = result.After; delta = result.Delta;
		return result.BecameExhausted;
	}

	private static string BuildExhaustedReply(string npcName, int relation, int refusalCount = 1)
	{
		return PatiencePromptProjectionComposer.BuildExhaustedReply(npcName, relation, refusalCount);
	}

	private static string BuildExhaustedPatienceInstruction(bool includeRelationPenalty)
	{
		return PatiencePromptProjectionComposer.BuildExhaustedPatienceInstruction(includeRelationPenalty, BuildPlayerPublicDisplayNameForPrompt());
	}

	private static string BuildPatiencePromptText(PatienceSnapshot snap, bool includeClanRelation = true)
	{
		return PatiencePromptProjectionComposer.BuildPatiencePromptText(snap, snap.Current <= 0.01f ? PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt() : null, includeClanRelation);
	}

	private string BuildDirectPatiencePrompt(Hero targetHero, out bool exhausted, out string exhaustedReply)
	{
        return PersonaIdentityPromptCaptureAdapter.BuildDirectPatiencePrompt(PersonaPatiencePromptCapture, targetHero, out exhausted, out exhaustedReply);
    }

private void ApplyPatienceFromHeroResponse(Hero targetHero, ref string aiResponse, bool directConversation)
	{
		PatienceResponses.ApplyHero(targetHero, ref aiResponse, directConversation, false);
	}

private void ApplyPatienceFromUnnamedResponse(string unnamedKey, string npcName, ref string aiResponse)
	{
		PatienceResponses.ApplyUnnamed(unnamedKey, npcName, ref aiResponse, false);
	}

private void ApplyPatiencePostprocessMoodOverrideFromHeroResponse(Hero targetHero, ref string aiResponse, bool directConversation)
	{
		PatienceResponses.ApplyHero(targetHero, ref aiResponse, directConversation, true);
	}

private void ApplyPatiencePostprocessMoodOverrideFromUnnamedResponse(string unnamedKey, string npcName, ref string aiResponse)
	{
		PatienceResponses.ApplyUnnamed(unnamedKey, npcName, ref aiResponse, true);
	}

	private bool TryGetHeroSceneStatus(Hero hero, out string statusLine, out bool canSpeak)
	{
        return PersonaIdentityPromptCaptureAdapter.TryGetHeroSceneStatus(PersonaPatiencePromptCapture, hero, out statusLine, out canSpeak);
    }

	private bool TryGetHeroSceneInlineState(Hero hero, out string stateText, out bool canSpeak)
	{
        return PersonaIdentityPromptCaptureAdapter.TryGetHeroSceneInlineState(PersonaPatiencePromptCapture, hero, out stateText, out canSpeak);
    }

	private bool TryGetUnnamedSceneStatus(string unnamedKey, string npcName, string displayName, out string statusLine, out bool canSpeak)
	{
        return PersonaIdentityPromptCaptureAdapter.TryGetUnnamedSceneStatus(PersonaPatiencePromptCapture, unnamedKey, npcName, displayName, out statusLine, out canSpeak);
    }

	private bool TryGetUnnamedSceneInlineState(string unnamedKey, string npcName, string displayName, out string stateText, out bool canSpeak)
	{
        return PersonaIdentityPromptCaptureAdapter.TryGetUnnamedSceneInlineState(PersonaPatiencePromptCapture, unnamedKey, npcName, displayName, out stateText, out canSpeak);
    }

	private bool TryGetUnnamedSceneStatus(string unnamedKey, string npcName, out string statusLine, out bool canSpeak)
	{
		return TryGetUnnamedSceneStatus(unnamedKey, npcName, null, out statusLine, out canSpeak);
	}

	private static string BuildScenePatienceInstruction()
	{
		return PatiencePromptProjectionComposer.BuildScenePatienceInstruction();
	}

	private void SyncPatienceData(IDataStore dataStore) => _patiencePersistence.Sync(dataStore);

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

	public static void ApplyPostprocessMoodFromSceneHeroResponseExternal(Hero targetHero, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatiencePostprocessMoodOverrideFromHeroResponse(targetHero, ref aiResponse, directConversation: false);
		}
		catch
		{
		}
	}

	public static void ApplyPostprocessMoodFromSceneUnnamedResponseExternal(string unnamedKey, string npcName, ref string aiResponse)
	{
		try
		{
			(Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.ApplyPatiencePostprocessMoodOverrideFromUnnamedResponse(unnamedKey, npcName, ref aiResponse);
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

	public static bool TryGetSceneHeroInlineStateForExternal(Hero hero, out string stateText, out bool canSpeak)
	{
		stateText = "";
		canSpeak = true;
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.TryGetHeroSceneInlineState(hero, out stateText, out canSpeak) ?? false;
		}
		catch
		{
			stateText = "";
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

	public static bool TryGetSceneUnnamedInlineStateForExternal(string unnamedKey, string npcName, string displayName, out string stateText, out bool canSpeak)
	{
		stateText = "";
		canSpeak = true;
		try
		{
			return (Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.TryGetUnnamedSceneInlineState(unnamedKey, npcName, displayName, out stateText, out canSpeak) ?? false;
		}
		catch
		{
			stateText = "";
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

	private void DevRootMenuInit(MenuCallbackArgs args) => DeveloperRootEditorController.InitializeRootMenuTitle(title => args.MenuTitle = new TextObject(title));

	private bool DevRootEntryCondition(MenuCallbackArgs args) => DeveloperRootEditorController.RootEntryCondition(() => args.optionLeaveType = GameMenuOption.LeaveType.Submenu, () => DuelSettings.GetSettings()?.EnableDevEditHistory ?? false);

	private void DevRootEntryConsequence(MenuCallbackArgs args) => DeveloperRootEditorController.RootEntryConsequence(() => DuelSettings.GetSettings()?.EnableDevEditHistory ?? false, GameMenu.SwitchToMenu);

	private bool DevRootSubOptionCondition(MenuCallbackArgs args) => DeveloperRootEditorController.RootEntryCondition(() => args.optionLeaveType = GameMenuOption.LeaveType.Submenu, () => DuelSettings.GetSettings()?.EnableDevEditHistory ?? false);

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

	private void DevRootKingdomStrategicProfilesOptionConsequence(MenuCallbackArgs args) => DeveloperRootEditorController.OpenKingdomStrategicProfilesMenu(() => { var behavior = KingdomStrategicProfileBehavior.Instance; return behavior == null ? null : new Action(behavior.OpenDevMenu); });

	private void DevRootAllOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevAllDataMenu();
	}

	private void DevRootVoiceMappingOptionConsequence(MenuCallbackArgs args)
	{
		OpenDevVoiceMappingMenu();
	}

	private bool DevRootBackCondition(MenuCallbackArgs args) => DeveloperRootEditorController.RootBackCondition(() => args.optionLeaveType = GameMenuOption.LeaveType.Leave);

	private void DevRootBackConsequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("town");
	}




















	private static bool IsKingdomEligibleForWeeklyReport(Kingdom kingdom) => MemoryEntityIdentityBannerlordAdapter.IsKingdomEligibleForWeeklyReport(kingdom);

	private static bool IsKingdomEligibleForWeeklyReport(string kingdomId) => MemoryEntityIdentityBannerlordAdapter.IsKingdomEligibleForWeeklyReport(kingdomId);




	private string GetKingdomOpeningSummary(Kingdom kingdom)
        => kingdom == null ? "" : WeeklyEventDataImportOwner.GetKingdomOpeningSummary(kingdom.StringId, _eventKingdomOpeningSummaries);

	private void SaveKingdomOpeningSummary(Kingdom kingdom, string summary)
    {
        if (kingdom == null) return;
        WeeklyEventDataImportOwner.SaveKingdomOpeningSummary(kingdom.StringId, summary, ref _weeklyEventRecords.KingdomOpenings,
            _weeklyReportMaterialRevisions.MarkOpening);
    }

	private static Kingdom FindKingdomById(string kingdomId) => MemoryEntityIdentityBannerlordAdapter.FindKingdomById(kingdomId);

	private static Clan FindClanById(string clanId) => MemoryEntityIdentityBannerlordAdapter.FindClanById(clanId);


	// SyncData already owns the event list. Normalize that list in place instead of
	// cloning every material graph solely to serialize or deserialize it.






















	private string ResolveKingdomOpeningSummaryById(string kingdomId)
	{
		Kingdom kingdom = FindKingdomById(kingdomId);
		return (kingdom == null) ? "" : GetKingdomOpeningSummary(kingdom);
	}





	private NpcActionEntry ResolveEventMaterialNpcAction(EventMaterialReference material) => _campaignCharacterRecordCapture.ResolveEventMaterialNpcAction(material);

	private static string TranslateEventKindForDev(string eventKind) => WeeklyReportRuntimeOwner.TranslateEventKindForDev(eventKind);

	private static string TranslateEventMaterialTypeForDev(string materialType) => WeeklyReportRuntimeOwner.TranslateEventMaterialTypeForDev(materialType);

	private static string ResolveSettlementDisplay(string settlementId) => MemoryEntityIdentityBannerlordAdapter.ResolveSettlementDisplay(settlementId);




	private List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups() => _campaignCharacterRecordCapture.BuildWeeklyEventMaterialPreviewGroups();

	private List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups(int startDay, int endDay) => _campaignCharacterRecordCapture.BuildWeeklyEventMaterialPreviewGroups(startDay, endDay);

	internal static EventMaterialReference BuildWeeklyPromptResolvedVillageRaidMaterial(List<EventMaterialReference> source)
	{
		return WeeklyPromptMaterialOwner.BuildResolvedVillageRaidMaterial(source, ResolveSettlementDisplay);
	}

	private static bool DoesEventSourceMaterialRelateToKingdom(EventSourceMaterialEntry item, Kingdom kingdom) => CampaignMaterialRecordOwner.DoesEventSourceMaterialRelateToKingdom(item, kingdom?.StringId);

	private static bool IsWeeklyPromptSiegeAggregateMaterial(EventMaterialReference material)
    {
        return WeeklyPromptCaptureAdapter.IsWeeklyPromptSiegeAggregateMaterial(material);
    }

	private static bool IsWeeklyPromptArmyAggregateMaterial(EventMaterialReference material)
    {
        return WeeklyPromptCaptureAdapter.IsWeeklyPromptArmyAggregateMaterial(material);
    }

	private static bool IsWeeklyPromptClanChangeAggregateMaterial(EventMaterialReference material)
    {
        return WeeklyPromptCaptureAdapter.IsWeeklyPromptClanChangeAggregateMaterial(material);
    }

	internal static string ResolveHeroKingdomIdForPrompt(string heroId)
	{
		Hero heroById = FindHeroById(heroId);
		return (heroById?.MapFaction?.StringId ?? heroById?.Clan?.Kingdom?.StringId ?? "").Trim();
	}

	private void ApplyWeeklyPromptMaterialAggregation(WeeklyEventMaterialPreviewGroup group)
	{
		WeeklyMaterialAggregationOwner.Apply(group, BuildWeeklyPromptAggregateCategoryMaterial);
	}

	private static List<EventMaterialReference> BuildWeeklyPromptMaterialsFromAggregatedBuckets(WeeklyEventMaterialPreviewGroup group, List<EventMaterialReference> source, bool includeRawFallback)
	{
		return OrderWeeklyPreviewMaterials((source ?? new List<EventMaterialReference>()).Where((EventMaterialReference x) => x != null).Select(CloneEventMaterialReference).ToList()).ToList();
	}



	private static string BuildWeeklyPromptAggregateShortSnippet(string text, int maxLen)
    {
        return WeeklyPromptCaptureAdapter.BuildWeeklyPromptAggregateShortSnippet(text, maxLen);
    }

	private static EventMaterialReference BuildWeeklyPromptAggregateShortCategoryMaterial(WeeklyEventMaterialPreviewGroup group, string category, List<EventMaterialReference> materials)
    {
        return WeeklyPromptCaptureAdapter.BuildWeeklyPromptAggregateShortCategoryMaterial(group, category, materials);
    }

	private static EventMaterialReference BuildWeeklyPromptAggregateShortRawMaterial(WeeklyEventMaterialPreviewGroup group, string rawKey, List<EventMaterialReference> materials)
    {
        return WeeklyPromptCaptureAdapter.BuildWeeklyPromptAggregateShortRawMaterial(group, rawKey, materials);
    }

	internal static EventMaterialReference CloneEventMaterialReference(EventMaterialReference material) => WeeklyPromptMaterialOwner.CloneEventMaterialReference(material);

	internal static bool IsWeeklyPromptAggregatableMaterial(EventMaterialReference material)
	{
		return WeeklyMaterialAggregationOwner.IsWeeklyPromptAggregatableMaterial(material);
	}

	internal static List<string> GetWeeklyPromptAggregateCategoryOrder()
	{
		return WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryOrder();
	}

	internal static string ResolveWeeklyPromptAggregateCategory(EventMaterialReference material)
	{
		return WeeklyMaterialAggregationOwner.ResolveWeeklyPromptAggregateCategory(material);
	}

	internal static string BuildWeeklyPromptAggregateEventKey(EventMaterialReference material)
	{
		return WeeklyMaterialAggregationOwner.BuildWeeklyPromptAggregateEventKey(material);
	}

	private static string NormalizeWeeklyPromptAggregateStableKey(string stableKey)
	{
		return WeeklyMaterialAggregationOwner.NormalizeWeeklyPromptAggregateStableKey(stableKey);
	}

	private EventMaterialReference BuildWeeklyPromptAggregateCategoryMaterial(WeeklyEventMaterialPreviewGroup group, string category, Dictionary<string, List<EventMaterialReference>> eventBuckets)
	{
		return WeeklyMaterialAggregationOwner.BuildCategoryMaterial(group, category, eventBuckets, GetWeeklyPromptAggregateCategoryLabel(category), BuildWeeklyPromptAggregateEventLine);
	}

	private static void AppendMaterialReferenceIds(EventMaterialReference source, EventMaterialReference destination)
	{
		WeeklyMaterialAggregationOwner.AppendMaterialReferenceIds(source, destination);
	}

	private static readonly WeeklyAggregateEventLineOwner WeeklyAggregateEventLine =
		new WeeklyAggregateEventLineOwner(new WeeklyAggregateGameFacts(
			ResolveHeroDisplay, ResolveClanDisplay, ResolveKingdomDisplay, ResolveSettlementDisplay,
			CaptureWeeklySettlementName, CaptureWeeklySettlementNameWithType, CaptureWeeklyHeroFact));

	private string BuildWeeklyPromptAggregateEventLine(List<EventMaterialReference> materials)
	{
		return WeeklyAggregateEventLine.Render(materials);
	}

	// The renderer is called only during synchronous main-thread material preparation.
	private static WeeklyHeroFact CaptureWeeklyHeroFact(string heroId) => MemoryEntityIdentityBannerlordAdapter.CaptureWeeklyHeroFact(heroId);

	private static string CaptureWeeklySettlementName(string settlementId) => MemoryEntityIdentityBannerlordAdapter.CaptureWeeklySettlementName(settlementId);

	private static string CaptureWeeklySettlementNameWithType(string settlementId) => MemoryEntityIdentityBannerlordAdapter.CaptureWeeklySettlementNameWithType(settlementId);

	private static bool TryParseWeeklyPromptPrisonerPair(string stableKey, out string captorHeroId, out string prisonerHeroId)
	{
		return WeeklyAggregateEventLineOwner.TryParseWeeklyPromptPrisonerPair(stableKey, out captorHeroId, out prisonerHeroId);
	}

	private static List<string> ResolveHeroNames(IEnumerable<string> heroIds) => WeeklyAggregateEventLineOwner.ResolveNames(heroIds, ResolveHeroDisplay);

	private static List<string> ResolveClanNames(IEnumerable<string> clanIds) => WeeklyAggregateEventLineOwner.ResolveNames(clanIds, ResolveClanDisplay);

	private static List<string> ResolveKingdomNames(IEnumerable<string> kingdomIds) => WeeklyAggregateEventLineOwner.ResolveNames(kingdomIds, ResolveKingdomDisplay);

	private static string GetWeeklyPromptAggregateCategoryLabel(string category)
	{
		return WeeklyMaterialAggregationOwner.GetWeeklyPromptAggregateCategoryLabel(category);
	}



	private static int GetEventAndRebellionApiMaxTokens()
	{
		return LlmRequestConfigurationCaptureAdapter.GetEventAndRebellionApiMaxTokens();
	}

	private static int GetWeeklyReportRequestIntervalMs() => WeeklyReportRuntimeOwner.GetWeeklyReportRequestIntervalMs(GetWeeklyReportRequestsPerMinute());

	private static int GetWeeklyReportBatchSize()
	{
		return 3;
	}



	private static readonly WeeklyGenerationRules _weeklyGenerationRules = new WeeklyGenerationRules(PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal);


	private static List<string> BuildWeeklyBatchExpectedReportIds(WeeklyReportBatchRequest batch) => WeeklyGenerationRules.BuildWeeklyBatchExpectedReportIds(batch);


#if false
	private static string BuildWeeklyReportBatchDisplayLabel(WeeklyReportBatchRequest batch)
	{
		List<string> list = (batch?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Select(BuildWeeklyReportGroupDisplayLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		return (list.Count == 0) ? "链懡鍚嶅懆鎶ユ壒娆? : ("鎵规锛? + string.Join(" | ", list));
	}

#endif


	private static bool IsWeeklyReportBatchPromptPrepared(WeeklyReportBatchRequest batch) => WeeklyGenerationRules.IsWeeklyReportBatchPromptPrepared(batch);

	private void PrepareWeeklyReportBatchPrompt(WeeklyReportBatchRequest batch) => WeeklyRuntime.PrepareWeeklyReportBatchPrompt(batch);

	private static List<WeeklyEventMaterialPreviewGroup> BuildRemainingWeeklyReportGroupsForRetry(List<WeeklyReportBatchRequest> batches, int batchIndex, IEnumerable<WeeklyEventMaterialPreviewGroup> currentBatchRemainingGroups) => WeeklyReportRuntimeOwner.BuildRemainingWeeklyReportGroupsForRetry(batches, batchIndex, currentBatchRemainingGroups);




	private DevWeeklyReportBatchPreviewEntry FindLatestWeeklyReportBatchDevPreview(WeeklyReportBatchRequest batch) => WeeklyRuntime.FindLatestWeeklyReportBatchDevPreview(batch);

	private static string BuildWeeklyReportFailureReason(string response, bool parseFailed) => WeeklyGenerationRules.BuildWeeklyReportFailureReason(response, parseFailed);

	private static string NormalizeWeeklyReportTagText(string text) => WeeklyGenerationRules.NormalizeWeeklyReportTagText(text);

	private static bool TryValidateWeeklyReportTagText(string rawTagText, out string normalizedTagText, out string stabilityTag, out string failureReason) => WeeklyGenerationRules.TryValidateWeeklyReportTagText(rawTagText, out normalizedTagText, out stabilityTag, out failureReason);

	private static int GetWeeklyReportStabilityDeltaForTag(string tag) => WeeklyGenerationRules.GetWeeklyReportStabilityDeltaForTag(tag);

	private static int ExtractWeeklyReportStabilityDelta(string tagText) => WeeklyGenerationRules.ExtractWeeklyReportStabilityDelta(tagText);

	private static string BuildFallbackWeeklyReportShortSummary(string report) => WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(report);

	private static string NeutralizeWeeklyReportScenarioName(string text) => WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(text);

	private WeeklyPromptSnapshot CaptureWeeklyPromptSnapshot(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)
    {
        return WeeklyPromptCaptureAdapter.CaptureWeeklyPromptSnapshot(WeeklyCapturePorts, targetHero, targetCharacter, kingdomIdOverride);
    }

	private static List<string> SelectWeeklyShortReportKingdomIdsFromSnapshot(string npcKingdomId, bool excludeNpcKingdom, bool npcKingdomEligible, IEnumerable<string> proximityOrderedKingdomIds, IEnumerable<string> fallbackKingdomIds) => WeeklyEventRecordStateOwner.SelectWeeklyShortReportKingdomIdsFromSnapshot(npcKingdomId, excludeNpcKingdom, npcKingdomEligible, proximityOrderedKingdomIds, fallbackKingdomIds);

	private Dictionary<string, WeeklyPromptReportSnapshot> CaptureLatestWeeklyPromptReportSnapshots(ISet<string> kingdomIds, out WeeklyPromptReportSnapshot worldReport)
    {
        return WeeklyPromptCaptureAdapter.CaptureLatestWeeklyPromptReportSnapshots(_eventRecordEntries, kingdomIds, out worldReport);
    }

	private static WeeklyPromptReportSnapshot CreateWeeklyPromptReportSnapshot(EventRecordEntry entry)
    {
        return WeeklyPromptCaptureAdapter.CreateWeeklyPromptReportSnapshot(entry);
    }

	private static bool IsNewerWeeklyPromptReportSnapshot(WeeklyPromptReportSnapshot candidate, WeeklyPromptReportSnapshot current)
    {
        return WeeklyPromptCaptureAdapter.IsNewerWeeklyPromptReportSnapshot(candidate, current);
    }

	private static string BuildWeeklyShortReportsPromptBlockFromSnapshot(IEnumerable<string> kingdomIds, IReadOnlyDictionary<string, WeeklyPromptReportSnapshot> reports, IReadOnlyDictionary<string, string> kingdomDisplays)
    {
        return WeeklyPromptCaptureAdapter.BuildWeeklyShortReportsPromptBlockFromSnapshot(kingdomIds, reports, kingdomDisplays);
    }

	private static string BuildSingleWeeklyFullReportPromptBlockFromSnapshot(string header, WeeklyPromptReportSnapshot entry)
    {
        return WeeklyPromptCaptureAdapter.BuildSingleWeeklyFullReportPromptBlockFromSnapshot(header, entry);
    }

	private static string BuildTriggeredWeeklyFullReportsPromptBlockFromSnapshot(string triggeredRuleInstructions, WeeklyPromptSnapshot weeklyPromptSnapshot)
    {
        return WeeklyPromptCaptureAdapter.BuildTriggeredWeeklyFullReportsPromptBlockFromSnapshot(triggeredRuleInstructions, weeklyPromptSnapshot);
    }

	private static string ResolveWeeklyReportNpcKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null) => MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);

	private static string ResolveWeeklyReportSurroundingsKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null) => MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyReportSurroundingsKingdomId(targetHero, targetCharacter, kingdomIdOverride);

	private EventRecordEntry FindLatestWeeklyReportRecord(string eventKind, string kingdomId = null) => _weekZeroShortSummaries.FindLatestWeeklyReportRecord(eventKind, kingdomId);


	public static string BuildRecentWeeklyReportContextForKingdomExternal(string kingdomId, int maxItems = 3)
	{
		return GetLatestKingdomWeeklyShortSummaryForExternal(kingdomId);
	}

	public static string GetLatestKingdomWeeklyShortSummaryForExternal(string kingdomId)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.GetLatestKingdomWeeklyShortSummaryInternal(kingdomId) ?? "";
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[WeeklyShortSummary][WARN] get latest kingdom weekly summary failed: " + ex.Message);
			return "";
		}
	}

	public static IReadOnlyList<WorldWeeklyReportHistoryEntry> GetPublishedWorldWeeklyReportHistoryForExternal(int minWeekExclusive = -1)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>())?.GetPublishedWorldWeeklyReportHistoryInternal(minWeekExclusive) ?? Array.Empty<WorldWeeklyReportHistoryEntry>();
		}
		catch (Exception ex)
		{
			Logger.Log("EventWeeklyReport", "[WorldWeeklyHistory][WARN] get published world weekly history failed: " + ex.Message);
			return Array.Empty<WorldWeeklyReportHistoryEntry>();
		}
	}

	public static long GetPublishedWorldWeeklyReportHistoryRevisionForExternal()
	{
		try
		{
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior == null ? 0L : Math.Max(0L, Volatile.Read(ref behavior._weeklyEventRecords.PublishedHistoryRevision));
		}
		catch
		{
			return 0L;
		}
	}

	public static long GetWorldMessageWeeklyTimelineRevisionForExternal()
	{
		try
		{
			// This is a constant-time runtime signal for an already open UI; it does not serialize or traverse report records.
			MyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			return behavior == null ? 0L : Math.Max(0L, Volatile.Read(ref behavior._weeklyEventRecords.TimelineRevision));
		}
		catch
		{
			return 0L;
		}
	}

	private void NotifyWorldMessageWeeklyTimelineChanged()
	{
		// All mutation sites call this after publishing a visible weekly record or changing its displayed text.
		Interlocked.Increment(ref _weeklyEventRecords.TimelineRevision);
	}

	private static string BuildPublishedWorldWeeklyProductState(EventRecordEntry entry)
	{
        return WeeklyEventRecordStateOwner.BuildPublishedWorldWeeklyProductState(entry);
    }

	private static void AppendPublishedWorldWeeklyProductField(StringBuilder builder, string value)
	{
        WeeklyEventRecordStateOwner.AppendPublishedWorldWeeklyProductField(builder, value);
    }

	private void NotifyPublishedWorldWeeklyProductChanged(string previousProductState, EventRecordEntry currentEntry)
	{
        _weeklyEventRecords.NotifyPublishedWorldWeeklyProductChanged(previousProductState, currentEntry);
    }

	private string BuildPublishedWorldWeeklyProductsFingerprint() => _weeklyEventRecords.BuildPublishedWorldWeeklyProductsFingerprint(WeekZeroOpeningSummaryGenerationController.ComputeWeekZeroShortSummarySourceHash);

	private IReadOnlyList<WorldWeeklyReportHistoryEntry> GetPublishedWorldWeeklyReportHistoryInternal(int minWeekExclusive) => _weeklyEventRecords.GetPublishedWorldWeeklyReportHistoryInternal(minWeekExclusive);

	private string GetLatestKingdomWeeklyShortSummaryInternal(string kingdomId) => _weekZeroShortSummaries.GetLatestKingdomWeeklyShortSummaryInternal(kingdomId);







	private static List<EventMaterialReference> CloneWeeklyReportMaterials(List<EventMaterialReference> materials) => WeeklyReportRuntimeOwner.CloneWeeklyReportMaterials(materials);

	private static WeeklyEventMaterialPreviewGroup BuildWeeklyReportRecordPreviewGroup(EventRecordEntry entry) => WeeklyReportRuntimeOwner.BuildWeeklyReportRecordPreviewGroup(entry);

	public Task<bool> GenerateWeeklyReportFullByEventIdAsync(string eventId) => WeeklyRuntime.GenerateWeeklyReportFullByEventIdAsync(eventId);

	private static string BuildWeeklyFullReportSourceState(EventRecordEntry entry) => WeeklyReportRuntimeOwner.BuildWeeklyFullReportSourceState(entry);









	private Task<bool> QueueWeeklyFullReportCompletionAsync(long runtimeGeneration, Func<bool> apply)
	{
		return _weeklyFullReportCompletions.Enqueue(runtimeGeneration, apply);
	}

	private void ProcessWeeklyFullReportCompletions()
	{
		_weeklyFullReportCompletions.Process();
	}

	private void CancelWeeklyFullReportCompletions()
	{
		_weeklyFullReportCompletions.Cancel();
	}

	private static void ShowWeeklyFullOnDemandProgressPopup(EventRecordEntry entry) => WeeklyReportEditorController.ShowWeeklyFullOnDemandProgressPopup(entry, _weeklyNoticeDefaultTitle);

	private static void ShowWeeklyFullOnDemandFailurePopup(string message) => WeeklyReportEditorController.ShowWeeklyFullOnDemandFailurePopup(message);

	private List<string> SelectWeeklyShortReportKingdomIds(string npcKingdomId, bool excludeNpcKingdom) => MemoryEntityIdentityBannerlordAdapter.SelectWeeklyShortReportKingdomIds(npcKingdomId, excludeNpcKingdom);

	private string BuildWeeklyShortReportsPromptBlock(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride, bool excludeNpcKingdom, WeeklyPromptSnapshot weeklyPromptSnapshot = null)
    {
        return WeeklyPromptCaptureAdapter.BuildWeeklyShortReportsPromptBlock(WeeklyCapturePorts, targetHero, targetCharacter, kingdomIdOverride, excludeNpcKingdom, weeklyPromptSnapshot);
    }

	private static string BuildSingleWeeklyFullReportPromptBlock(string header, EventRecordEntry entry)
    {
        return WeeklyPromptCaptureAdapter.BuildSingleWeeklyFullReportPromptBlock(header, entry);
    }

	private string BuildTriggeredWeeklyFullReportsPromptBlock(string triggeredRuleInstructions, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null)
    {
        return WeeklyPromptCaptureAdapter.BuildTriggeredWeeklyFullReportsPromptBlock(WeeklyCapturePorts, triggeredRuleInstructions, targetHero, targetCharacter, kingdomIdOverride, weeklyPromptSnapshot);
    }

	private bool ShouldExcludeNpcShortReportFromWeeklyShortLayer(string triggeredRuleInstructions, Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null, WeeklyPromptSnapshot weeklyPromptSnapshot = null) => WeekZeroOpeningSummaryGenerationController.ShouldExcludeNpcShortReportFromWeeklyShortLayer(triggeredRuleInstructions,targetHero,targetCharacter,kingdomIdOverride,weeklyPromptSnapshot);

	private static bool ContainsAnyIgnoreCase(string text, params string[] tokens) => MemoryBusinessStateOwner.ContainsAnyIgnoreCase(text, tokens);

	private static bool IsQuotaLimitResponseBody(string responseBody)
	{
		return ConfiguredChatApplicationAdapter.IsQuotaLimitResponseBody(responseBody);
	}

	private static bool IsRequestsPerMinuteLimitResponseBody(string responseBody)
	{
		return ConfiguredChatApplicationAdapter.IsRequestsPerMinuteLimitResponseBody(responseBody);
	}

	private static bool IsGenericRateLimitResponseBody(string responseBody)
	{
		return ConfiguredChatApplicationAdapter.IsGenericRateLimitResponseBody(responseBody);
	}

	private static int? TryGetRetryAfterSeconds(HttpResponseMessage response)
	{
		return ConfiguredChatApplicationAdapter.TryGetRetryAfterSeconds(response);
	}

	private static bool HasRequestsPerMinuteRateLimitHeaders(HttpResponseMessage response)
	{
		return ConfiguredChatApplicationAdapter.HasRequestsPerMinuteRateLimitHeaders(response);
	}

	private static string BuildApiCallFailureMessage(HttpStatusCode statusCode, string responseBody, int? retryAfterSeconds, bool isRateLimit, bool isRequestsPerMinuteLimit, bool isQuotaLimit)
	{
		return ConfiguredChatApplicationAdapter.BuildApiCallFailureMessage(statusCode, responseBody, retryAfterSeconds, isRateLimit, isRequestsPerMinuteLimit, isQuotaLimit);
	}

	private static void TryPersistMcmSettings(DuelSettings settings) => DeveloperRootEditorController.TryPersistMcmSettings(settings);

	private static readonly WeeklyGenerationAttemptOwner _weeklyGenerationAttemptOwner = new WeeklyGenerationAttemptOwner(_weeklyGenerationRules);
 private WeeklyGenerationAttemptPort CreateWeeklyGenerationAttemptPort() => new WeeklyGenerationAttemptPort { CallGroup = CallWeeklyReportApiDetailed, CallBatch = CallWeeklyReportBatchApiAttemptAsync, LogExchange = Logger.LogEventPromptExchange, Log = Logger.Log, Delay = milliseconds => Task.Delay(milliseconds) };
 private async Task<WeeklyReportRequestResult> GenerateWeeklyReportGroupWithRetriesAsync(WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay, int maxAttempts) { string system = BuildWeeklyReportSystemPrompt(group); string user = BuildWeeklyReportUserPrompt(group, weekIndex, startDay, endDay); return await _weeklyGenerationAttemptOwner.GenerateWeeklyReportGroupWithRetriesAsync(group, weekIndex, startDay, endDay, maxAttempts, system, user, BuildWeeklyReportPromptPreviewText(group, system, user), BuildWeeklyReportGroupDisplayLabel(group), CreateWeeklyGenerationAttemptPort()); }

	private Task<ApiCallResult> CallWeeklyReportBatchApiAttemptAsync(string systemPrompt, string userPrompt, long runtimeGeneration, bool firstAttempt) => WeeklyRuntime.CallWeeklyReportBatchApiAttemptAsync(systemPrompt, userPrompt, runtimeGeneration, firstAttempt);

	private async Task<WeeklyReportBatchRequestResult> GenerateWeeklyReportBatchWithRetriesAsync(WeeklyReportBatchRequest batch, int maxAttempts, long runtimeGeneration = 0L) => await _weeklyGenerationAttemptOwner.GenerateWeeklyReportBatchWithRetriesAsync(batch, maxAttempts, runtimeGeneration, BuildWeeklyReportBatchDisplayLabel(batch), CreateWeeklyGenerationAttemptPort());

	private static void CaptureWeeklyReportBatchAttemptFailureMetadata(WeeklyReportBatchRequestResult result, ApiCallResult attempt) => WeeklyGenerationRules.CaptureWeeklyReportBatchAttemptFailureMetadata(result, attempt);

	private Task<WeeklyReportBatchExecutionResult> ExecuteWeeklyReportBatchAsync(WeeklyReportBatchRequest batch, int batchIndex, int maxAttempts, long runtimeGeneration) => WeeklyRuntime.ExecuteWeeklyReportBatchAsync(batch, batchIndex, maxAttempts, runtimeGeneration);


	private void QueueWeeklyReportFailurePopup(WeeklyReportRetryContext context, bool showImmediate = false)
		=> WeeklyEditor.QueueWeeklyReportFailurePopup(context, showImmediate);
	private void ShowWeeklyReportFailurePopup(bool ignoreDelay = false)
		=> WeeklyEditor.ShowWeeklyReportFailurePopup(ignoreDelay);
	private void OpenWeeklyReportRpmLimitInput()
		=> WeeklyEditor.OpenWeeklyReportRpmLimitInput();
	private void ShowWeeklyReportRetryProgressPopup()
		=> WeeklyEditor.ShowWeeklyReportRetryProgressPopup();
	private void BeginRetryBlockedWeeklyReports()
		=> WeeklyEditor.BeginRetryBlockedWeeklyReports();
	private List<WeeklyEventMaterialPreviewGroup> SelectFreshWeeklyReportRetryGroups(List<WeeklyEventMaterialPreviewGroup> freshGroups, List<WeeklyEventMaterialPreviewGroup> failedGroups)
		=> WeeklyEditor.SelectFreshWeeklyReportRetryGroups(freshGroups, failedGroups);
	private void BeginFreshWeeklyReportRetry(WeeklyReportRetryContext staleContext)
		=> WeeklyEditor.BeginFreshWeeklyReportRetry(staleContext);
	private void CancelWeeklyReportManualRetryAndReturn()
		=> WeeklyEditor.CancelWeeklyReportManualRetryAndReturn();
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
        _campaignSaveExit.BeginSaveAndExitCurrentGame(reason);
    }

	private void OnSaveOver(bool isSuccessful, string saveName)
	{
        _campaignSaveExit.OnSaveOver(isSuccessful, saveName);
    }

	private void HandleSaveAndExitFailure(SaveAndExitReason reason, string message)
	{
        _campaignSaveExit.HandleSaveAndExitFailure(reason, message);
    }

	private Task RetryBlockedWeeklyReportsAsync(WeeklyReportRetryContext context, int retryVersion)
		=> WeeklyEditor.RetryBlockedWeeklyReportsAsync(context, retryVersion);
	private void OpenWeeklyReportApiRepairFlow()
		=> WeeklyEditor.OpenWeeklyReportApiRepairFlow();
		private static Vec2? GetPlayerPartyPositionVec2() => MemoryEntityIdentityBannerlordAdapter.GetPlayerPartyPositionVec2();

		private static List<string> GetKingdomIdsByPlayerProximity(IEnumerable<string> kingdomIds) => MemoryEntityIdentityBannerlordAdapter.GetKingdomIdsByPlayerProximity(kingdomIds);

	private static List<WeeklyEventMaterialPreviewGroup> OrderWeeklyReportGenerationGroups(List<WeeklyEventMaterialPreviewGroup> groups) => MemoryEntityIdentityBannerlordAdapter.OrderWeeklyReportGenerationGroups(groups);






	private static string BuildWeeklyReportFullOnDemandSystemPrompt(WeeklyEventMaterialPreviewGroup group)
	{
        return WeeklyPromptCaptureAdapter.BuildWeeklyReportFullOnDemandSystemPrompt(WeeklyRequestPromptSettings, group);
    }

	private string BuildWeeklyReportFullOnDemandUserPrompt(WeeklyEventMaterialPreviewGroup group, int weekIndex)
	{
        return WeeklyPromptCaptureAdapter.BuildWeeklyReportFullOnDemandUserPrompt(WeeklyRequestPromptCapturePorts, group, weekIndex);
    }

	private string BuildWeeklyReportCurrentKingdomStabilityTierText(WeeklyEventMaterialPreviewGroup group) => _kingdomStabilityGameAdapter.BuildWeeklyReportCurrentKingdomStabilityTierText(group);

	private string BuildWeeklyReportUserPrompt(WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay)
	{
        return WeeklyPromptCaptureAdapter.BuildWeeklyReportUserPrompt(WeeklyRequestPromptCapturePorts, group, weekIndex, startDay, endDay);
    }

	private string GetPreviousWeeklyReportText(WeeklyEventMaterialPreviewGroup group, int currentWeekIndex) => _weeklyEventRecords.GetPreviousWeeklyReportText(group, currentWeekIndex, PersonaIdentityPromptCaptureAdapter.GetSeasonTextZhForPrompt);

	private static string BuildWeeklyReportMaterialLines(WeeklyEventMaterialPreviewGroup group) => WeeklyEventRecordStateOwner.BuildWeeklyReportMaterialLines(group, MemoryEntityIdentityBannerlordAdapter.BuildWeeklyReportMaterialLine, MemoryEntityIdentityBannerlordAdapter.AnnotateWeeklyReportRulerInMaterialText);

	private static string BuildWeeklyReportMaterialLines(List<EventMaterialReference> materials) => WeeklyEventRecordStateOwner.BuildWeeklyReportMaterialLines(materials, MemoryEntityIdentityBannerlordAdapter.BuildWeeklyReportMaterialLine);

	private static string BuildWeeklyReportMaterialLine(EventMaterialReference material) => MemoryEntityIdentityBannerlordAdapter.BuildWeeklyReportMaterialLine(material);


	private static string BuildWeeklyBatchReportSystemPrompt(WeeklyReportBatchRequest batch)
	{
        return WeeklyPromptCaptureAdapter.BuildWeeklyBatchReportSystemPrompt(WeeklyRequestPromptSettings, batch);
    }

	private string BuildWeeklyBatchReportUserPrompt(WeeklyReportBatchRequest batch)
	{
        return WeeklyPromptCaptureAdapter.BuildWeeklyBatchReportUserPrompt(WeeklyRequestPromptCapturePorts, batch);
    }

	private string BuildWeeklyBatchPromptInputBlock(WeeklyEventMaterialPreviewGroup group, int weekIndex)
	{
        return WeeklyPromptCaptureAdapter.BuildWeeklyBatchPromptInputBlock(WeeklyRequestPromptCapturePorts, group, weekIndex);
    }

	private static string BuildWeeklyReportPromptMaterialLines(WeeklyEventMaterialPreviewGroup group)
	{ return WeeklyPromptCaptureAdapter.BuildWeeklyReportPromptMaterialLines(group); }

	private static string AnnotateWeeklyReportRulerInMaterialText(string materialText, WeeklyEventMaterialPreviewGroup group) => MemoryEntityIdentityBannerlordAdapter.AnnotateWeeklyReportRulerInMaterialText(materialText, group);

	private static bool HasWeeklyReportRulerContextMaterial(WeeklyEventMaterialPreviewGroup group) => WeeklyEventRecordStateOwner.HasWeeklyReportRulerContextMaterial(group);

	private static bool HasWeeklyReportRulerContextMaterial(IEnumerable<EventMaterialReference> materials) => WeeklyEventRecordStateOwner.HasWeeklyReportRulerContextMaterial(materials);

	private static string AnnotateWeeklyReportRulerNameInText(string text, string rulerName) => WeeklyEventRecordStateOwner.AnnotateWeeklyReportRulerNameInText(text, rulerName);


	private static bool TryParseWeeklyBatchResponse(string rawResponse, WeeklyReportBatchRequest batch, out List<WeeklyReportBatchBlockResult> blocks, out List<string> missingReportIds, out string failureReason) => _weeklyGenerationRules.TryParseWeeklyBatchResponse(rawResponse, batch, out blocks, out missingReportIds, out failureReason);

	private static bool TryParseWeeklyBatchBlock(string rawBlockBody, WeeklyReportBatchRequest batch, Dictionary<string, WeeklyEventMaterialPreviewGroup> groupMap, out WeeklyReportBatchBlockResult block) => _weeklyGenerationRules.TryParseWeeklyBatchBlock(rawBlockBody, batch, groupMap, out block);

	private static string ExtractWeeklyBatchHeaderValue(string text, string key) => WeeklyGenerationRules.ExtractWeeklyBatchHeaderValue(text, key);

	private static bool TryParseWeeklyReportResponse(string rawResponse, WeeklyEventMaterialPreviewGroup group, int weekIndex, out string title, out string shortSummary, out string report, out string tagText) => _weeklyGenerationRules.TryParseWeeklyReportResponse(rawResponse, group, weekIndex, out title, out shortSummary, out report, out tagText);

	private static bool TryParseWeeklyFullOnDemandReportResponse(string rawResponse, out string title, out string shortSummary, out string report) => _weeklyGenerationRules.TryParseWeeklyFullOnDemandReportResponse(rawResponse, out title, out shortSummary, out report);

	private static string BuildDefaultWeeklyReportTitle(WeeklyEventMaterialPreviewGroup group, int weekIndex) => WeeklyEventRecordStateOwner.BuildDefaultWeeklyReportTitle(group, weekIndex, PersonaIdentityPromptCaptureAdapter.GetSeasonTextZhForPrompt, MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay);

	private static string BuildWeeklyEpicWeekLabel(int weekIndex) => WeeklyEventRecordStateOwner.BuildWeeklyEpicWeekLabel(weekIndex, PersonaIdentityPromptCaptureAdapter.GetSeasonTextZhForPrompt);

	private static string ConvertWeekNumberToChineseOrdinal(int weekNumber) => MemoryBusinessStateOwner.ConvertWeekNumberToChineseOrdinal(weekNumber);

	private void ApplyWeeklyReportStabilityDelta(WeeklyEventMaterialPreviewGroup group, string eventId, string tagText) => _kingdomStabilityGameAdapter.ApplyWeeklyReportStabilityDelta(group, eventId, tagText);

	private void UpsertWeeklyReportEventRecord(WeeklyEventMaterialPreviewGroup group, int weekIndex, string title, string shortSummary, string report, string tagText, string promptText, bool sanitizeAfter = true)
	{
		List<EventMaterialReference> materials = OrderWeeklyPreviewMaterials(group?.Materials).Where((EventMaterialReference x) => x != null).Select(CloneEventMaterialReference).ToList();
		UpsertWeeklyReportEventRecord(group, weekIndex, title, shortSummary, report, tagText, promptText, materials, sanitizeAfter);
	}


	private List<EventSourceMaterialEntry> GetWeeklyEventSourceMaterialsForBuild() => _campaignMaterialRecords.GetWeeklyEventSourceMaterialsForBuild();

	private WeeklyEventMaterialPreviewGroup CreateWorldWeeklyEventMaterialPreviewGroup() => _weeklyEventRecords.CreateWorldWeeklyEventMaterialPreviewGroup();

	private Dictionary<string, Hero> BuildWeeklyNpcActionHeroLookup() => _campaignCharacterRecordCapture.BuildWeeklyNpcActionHeroLookup();

	private static Hero ResolveWeeklyNpcActionHero(Dictionary<string, Hero> actionHeroLookup, string heroId) => MemoryEntityIdentityBannerlordAdapter.ResolveWeeklyNpcActionHero(actionHeroLookup, heroId);

	private WeeklyEventMaterialPreviewGroup BuildWorldWeeklyEventMaterialPreviewGroup(int startDay, int endDay, Dictionary<string, Hero> actionHeroLookup) => _campaignCharacterRecordCapture.BuildWorldWeeklyEventMaterialPreviewGroup(startDay, endDay, actionHeroLookup);

	private WeeklyEventMaterialPreviewGroup CreateKingdomWeeklyEventMaterialPreviewGroup(Kingdom kingdom) => MemoryEntityIdentityBannerlordAdapter.CreateKingdomWeeklyEventMaterialPreviewGroup(_weeklyEventRecords, kingdom);

	private WeeklyEventMaterialPreviewGroup BuildKingdomWeeklyEventMaterialPreviewGroup(Kingdom kingdom, int startDay, int endDay, Dictionary<string, Hero> actionHeroLookup) => _campaignCharacterRecordCapture.BuildKingdomWeeklyEventMaterialPreviewGroup(kingdom, startDay, endDay, actionHeroLookup);

	private static void TryAddKingdomCurrentRulerMaterial(List<EventMaterialReference> materials, Kingdom kingdom) => MemoryEntityIdentityBannerlordAdapter.TryAddKingdomCurrentRulerMaterial(materials, kingdom);

	private static Hero FindHeroById(string heroId) => MemoryEntityIdentityBannerlordAdapter.FindHeroById(heroId);

	private static bool DoesNpcActionRelateToKingdom(NpcActionEntry entry, Kingdom kingdom) => NpcActionRecordOwner.DoesNpcActionRelateToKingdom(entry, kingdom?.StringId);

	private static bool ShouldIncludeKingdomPreviewAction(Hero hero, NpcActionEntry entry, Kingdom kingdom) => MemoryEntityIdentityBannerlordAdapter.ShouldIncludeKingdomPreviewAction(hero, entry, kingdom);

	private static bool ShouldIncludeWorldPreviewAction(Hero hero, NpcActionEntry entry) => MemoryEntityIdentityBannerlordAdapter.ShouldIncludeWorldPreviewAction(hero, entry);

	private static bool ShouldSuppressWeeklyPreviewAction(NpcActionEntry entry) => MemoryEntityIdentityBannerlordAdapter.ShouldSuppressWeeklyPreviewAction(entry);

	private static bool IsMapEventNpcAction(NpcActionEntry entry) => MemoryBusinessStateOwner.IsMapEventNpcAction(entry);

	private static bool IsBanditClanId(string clanId) => MemoryEntityIdentityBannerlordAdapter.IsBanditClanId(clanId);

	private static bool IsBanditKingdomId(string kingdomId) => MemoryEntityIdentityBannerlordAdapter.IsBanditKingdomId(kingdomId);

	private static bool ContainsRoutineBanditText(string text) => CampaignBattleRecordCaptureAdapter.ContainsRoutineBanditText(text);

	private static bool IsPrisonerTakenAction(NpcActionEntry entry) => MemoryBusinessStateOwner.IsPrisonerTakenAction(entry);

	private static bool IsLeadershipCaptureAction(NpcActionEntry entry) => MemoryEntityIdentityBannerlordAdapter.IsLeadershipCaptureAction(entry);

	private static bool IsPrisonerReleasedAction(NpcActionEntry entry) => MemoryBusinessStateOwner.IsPrisonerReleasedAction(entry);

	private static bool IsLeadershipReleaseAction(NpcActionEntry entry) => MemoryEntityIdentityBannerlordAdapter.IsLeadershipReleaseAction(entry);

	private static string GetCapturedHeroId(NpcActionEntry entry) => MemoryBusinessStateOwner.GetCapturedHeroId(entry);

	private static string GetReleasedHeroId(NpcActionEntry entry) => MemoryBusinessStateOwner.GetReleasedHeroId(entry);

	private static bool IsArmyCommanderDailyBehavior(NpcActionEntry entry) => MemoryBusinessStateOwner.IsArmyCommanderDailyBehavior(entry);

	private static bool IsDailyBehaviorRaid(NpcActionEntry entry) => MemoryBusinessStateOwner.IsDailyBehaviorRaid(entry);

	private static bool IsDailyBehaviorDefend(NpcActionEntry entry) => MemoryBusinessStateOwner.IsDailyBehaviorDefend(entry);

	private void TryAddPreviewActionMaterial(List<EventMaterialReference> materials, Hero hero, NpcActionEntry entry, bool recentOnly) => _campaignCharacterRecordCapture.TryAddPreviewActionMaterial(materials, hero, entry, recentOnly);

	private static void TryAddPreviewSourceMaterial(List<EventMaterialReference> materials, EventSourceMaterialEntry entry) => CampaignCharacterRecordCaptureAdapter.TryAddPreviewSourceMaterial(materials, entry);



	internal static IEnumerable<EventMaterialReference> OrderWeeklyPreviewMaterials(List<EventMaterialReference> materials) => WeeklyPromptMaterialOwner.OrderWeeklyPreviewMaterials(materials);

	private static void NormalizeNpcActionSequences(Dictionary<string, List<NpcActionEntry>> storage) => NpcActionRecordOwner.NormalizeNpcActionSequences(storage);

	private static int GetMaxNpcActionSequence(params Dictionary<string, List<NpcActionEntry>>[] storages) => NpcActionRecordOwner.GetMaxNpcActionSequence(storages);

	private static int GetMaxNpcActionSequence(Dictionary<string, List<NpcActionEntry>> majorStorage, Dictionary<string, List<NpcActionEntry>> recentStorage, List<EventSourceMaterialEntry> sourceMaterials) => NpcActionRecordOwner.GetMaxNpcActionSequence(majorStorage, recentStorage, sourceMaterials);

	private static int GetWeeklyPreviewMaterialSortBucket(EventMaterialReference material) => WeeklyPromptMaterialOwner.GetWeeklyPreviewMaterialSortBucket(material);













	private async Task GenerateDevWeeklyReportsAsync()
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshot = _weeklyReportMaterialRevisions.Capture(num, currentGameDayIndexSafe);
		List<WeeklyEventMaterialPreviewGroup> list = OrderWeeklyReportGenerationGroups(BuildWeeklyEventMaterialPreviewGroups(num, currentGameDayIndexSafe));
		await GenerateWeeklyReportsBatchedAsyncInternal(list, num2, num, currentGameDayIndexSafe, "本周周报草案", openViewerWhenDone: false, queueBlockingPopupOnFatalFailure: true, isAutoGeneration: false, sourceSnapshotOverride: sourceSnapshot);
	}



	private Task<List<Task<WeeklyReportBatchExecutionResult>>> EnqueueWeeklyWaveLaunchAsync(List<WeeklyReportBatchRequest> wave, int firstBatchIndex, int waveIndex, int totalWaves, int totalTargets, int totalBatches, int burstSize, string displayLabel, long runtimeGeneration, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshot) => WeeklyRuntime.EnqueueWeeklyWaveLaunchAsync(wave, firstBatchIndex, waveIndex, totalWaves, totalTargets, totalBatches, burstSize, displayLabel, runtimeGeneration, sourceSnapshot);

	private static void CompletePendingWeeklyWaveLaunch(PendingWeeklyWaveLaunchContext context, List<Task<WeeklyReportBatchExecutionResult>> result)
	{
		context?.CompletionSource?.TrySetResult(result);
	}

	private bool ProcessPendingWeeklyWaveLaunches() => WeeklyRuntime.ProcessPendingWeeklyWaveLaunches();

	private static void CompletePendingWeeklyBatchApiAttempt(PendingWeeklyBatchApiAttemptContext context, Task<ApiCallResult> result)
	{
		context?.CompletionSource?.TrySetResult(result);
	}

	private bool ProcessPendingWeeklyBatchApiAttempts() => WeeklyRuntime.ProcessPendingWeeklyBatchApiAttempts();


	private static void CompletePendingWeeklyPromptPreparation(PendingWeeklyPromptPreparationContext context, WeeklyPromptPreparationResult result)
	{
		context?.CompletionSource?.TrySetResult(result);
	}

	private bool ProcessPendingWeeklyPromptPreparations() => WeeklyRuntime.ProcessPendingWeeklyPromptPreparations();


	private bool ProcessPendingWeeklyReportCommits() => WeeklyRuntime.ProcessPendingWeeklyReportCommits();

	private static void CompletePendingWeeklyReportCommit(PendingWeeklyReportCommitContext context, WeeklyReportGenerationResult result)
	{
		try
		{
			context?.CompletionSource?.TrySetResult(result ?? new WeeklyReportGenerationResult());
		}
		catch
		{
		}
	}









	private static string ResolveNearestWeeklyReportKingdomId(IEnumerable<string> kingdomIds) => WeeklyNoticeGameAdapter.ResolveNearestWeeklyReportKingdomId(kingdomIds);


	private static bool CanPublishWeeklyReportMapNotification() => WeeklyNoticeGameAdapter.CanPublishWeeklyReportMapNotification();

	private bool TryEnsureWeeklyReportMapNotificationRegistered() => _weeklyNoticeGameAdapter.TryEnsureWeeklyReportMapNotificationRegistered();

	private void TryPublishUnreadWeeklyReportMapNotifications() => _weeklyNoticeGameAdapter.TryPublishUnreadWeeklyReportMapNotifications();

	private void QueueWeeklyReportMapNotice(string eventId) => _weeklyNoticeGameAdapter.QueueWeeklyReportMapNotice(eventId);

	private void NormalizeUnreadWeeklyReportNoticesForCurrentPolicy() => _weeklyNoticeOwner.NormalizeUnreadWeeklyReportNoticesForCurrentPolicy(WeeklyNoticePort);

	private static bool IsWeeklyReportMapNotificationEnabled() => WeeklyNoticeGameAdapter.IsWeeklyReportMapNotificationEnabled();

	private void OnMapNoticeRemoved(InformationData data) => _weeklyNoticeGameAdapter.OnMapNoticeRemoved(data);

	private void MarkWeeklyReportNoticeRead(string eventId) => _weeklyNoticeOwner.MarkRead(eventId);

	internal bool OpenWeeklyReportNoticeFromMap(string eventId) => _weeklyNoticeGameAdapter.OpenWeeklyReportNoticeFromMap(eventId);

	private void TryAwardWeeklyReportReadingXp(string eventId) => _weeklyNoticeGameAdapter.TryAwardWeeklyReportReadingXp(eventId);

	private static bool CanAwardWeeklyReportReadingXp(Hero hero, out string reason) => WeeklyNoticeGameAdapter.CanAwardWeeklyReportReadingXp(hero, out reason);

	private static bool TryAwardWeeklyReportReadingXpBatch(Hero hero, int leadershipXp, int charmXp, int stewardXp, out string failureReason) => WeeklyNoticeGameAdapter.TryAwardWeeklyReportReadingXpBatch(hero, leadershipXp, charmXp, stewardXp, out failureReason);

	private void NormalizeWeeklyReportReadingXpPendingBatch() => _weeklyNoticeOwner.NormalizeReadingXpPendingBatch();

	private static int CalculateWeeklyReportReadingXp(int meaningfulUnitCount, int xpPerHundred, int skillCap) => WeeklyNoticeGameAdapter.CalculateWeeklyReportReadingXp(meaningfulUnitCount, xpPerHundred, skillCap);

	private static string BuildWeeklyReportNoticeTitle(EventRecordEntry entry) => WeeklyNoticeGameAdapter.BuildWeeklyReportNoticeTitle(entry, _weeklyNoticeDefaultTitle);

	private static string BuildWeeklyReportPopupBodyText(EventRecordEntry entry) => WeeklyEventRecordStateOwner.BuildWeeklyReportPopupBodyText(entry);

	private static string BuildWeeklyReportPopupSubtitle(EventRecordEntry entry) => _weeklyNoticeSubtitle(entry);

	private static string BuildWeeklyReportNoticeDescription(EventRecordEntry entry) => WeeklyNoticeGameAdapter.BuildWeeklyReportNoticeDescription(entry, _weeklyNoticeSubtitle);



	private static List<string> SanitizeUnreadWeeklyReportNoticeEventIds(IEnumerable<string> source) => WeeklyNoticeStateOwner.SanitizeUnreadWeeklyReportNoticeEventIds(source);

	private static List<string> SanitizeWeeklyReportReadingXpClaimedEventIds(IEnumerable<string> source) => WeeklyNoticeGameAdapter.SanitizeWeeklyReportReadingXpClaimedEventIds(source);

	private static List<string> SanitizeWeeklyReportEventIds(IEnumerable<string> source) => WeeklyNoticeStateOwner.SanitizeWeeklyReportEventIds(source);


	private async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsBatchedAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration, IEnumerable<string> popupCandidateKingdomIdsOverride = null, List<WeeklyReportBatchRequest> preparedBatches = null, long runtimeGeneration = 0L, Dictionary<string, string> capturedRecordStatesOverride = null, WeeklyReportMaterialRevisionOwner.Snapshot sourceSnapshotOverride = null)
	{
		if (runtimeGeneration <= 0L)
		{
			runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		}
		return await GenerateWeeklyReportsMinuteBurstAsyncInternal(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, queueBlockingPopupOnFatalFailure, isAutoGeneration, popupCandidateKingdomIdsOverride, preparedBatches, runtimeGeneration, capturedRecordStatesOverride, sourceSnapshotOverride);
	}

	private async Task<WeeklyReportGenerationResult> GenerateWeeklyReportsAsyncInternal(List<WeeklyEventMaterialPreviewGroup> list, int weekIndex, int startDay, int endDay, string displayLabel, bool openViewerWhenDone, bool queueBlockingPopupOnFatalFailure, bool isAutoGeneration)
	{
		return await GenerateWeeklyReportsBatchedAsyncInternal(list, weekIndex, startDay, endDay, displayLabel, openViewerWhenDone, queueBlockingPopupOnFatalFailure, isAutoGeneration);
	}





	private void ClearAllDataForCurrentSave()
	{
		RetireConversationRequestsForDeveloperClear();
		OnDeveloperClearWeeklyActionOutcomes(SaveRuntimeGuard.CurrentGeneration);
		ClearInteractionMemoryRecoveryForDeveloperClear();
		_npcPersonaGeneration.Reset();
		CancelWeeklyFullReportCompletions();
		ResetMemorySummaryMainThreadActions();
		_shownPersistence.ResetForCurrentSave();
		_memoryBusinessState.ClearHistoryAndDailyForCurrentSave();
		_memorySummaryQueueJsonStorage = "[]";
		_memorySummaryRunOwner.Reset();
		ResetMemoryFailureNotices();
		_nativeConversationMemorySessionCounter = 0;
		_activeNativeConversationMemorySessionId = -1;
		_memoryBusinessState.ClearOverviewForCurrentSave();
		_memoryOverviewQueueJsonStorage = "[]";
		_memoryBusinessState.ClearMajorSummaryForCurrentSave();
		_npcMajorActionSummaryQueueJsonStorage = "[]";
		ResetNpcActionRecordContainers();
		_npcActionGlobalOrderCounter = 0;
		_personaPersistence.ResetForCurrentSave();
		WeeklyEventDataImportOwner.ResetContainers(ref _weeklyEventRecords.KingdomOpenings, ref _weeklyRecordPersistence.OpeningStorage,
            ref _weeklyEventRecords.WorldOpening, ref _weeklyEventRecords.Records, ref _weeklyRecordPersistence.RecordJsonStorage);
		_campaignMaterialRecords.ResetMaterials();
		_eventSourceMaterialJsonStorage = "[]";
		_weeklyReportMaterialRevisions.MarkAll();
		_weeklyReportMaterialRevisions.MarkOpening();
		_kingdomPersistence.ResetForCurrentSave();
		_lastAutoGeneratedWeeklyReportWeek = -1;
		_lastProcessedKingdomRebellionWeek = -1;
		_weeklyNoticeOwner.ResetForCurrentSave();
		_weeklyReportRegisteredMapNotificationView = null;
		_weeklyReportGenerationInProgress = false;
		_weeklyReportUiStage = WeeklyReportUiStage.None;
		_weeklyReportRetryContext = null;
		_weeklyReportManualRetryInProgress = false;
		_weeklyReportManualRetryVersion = 0;
		_latestWeeklyReportBatchDevPreviews = new List<DevWeeklyReportBatchPreviewEntry>();
		_pendingWeeklyReportManualRetryResult = false;
		_pendingWeeklyReportManualRetrySucceeded = false;
		_pendingWeeklyReportManualRetryMessage = "";
		_pendingWeeklyReportManualRetryContext = null;
		_weeklyReportUiResumeAfterUtcTicks = 0L;
		_weeklyReportReopenAfterApiConfig = false;
		_weeklyReportReopenAfterApiConfigUtcTicks = 0L;
		_devForcedKingdomRebellionInProgress = false;
		_pendingDevForcedKingdomRebellionReady = false;
		_pendingDevForcedKingdomRebellionContext = null;
		AutomaticKingdomRebellions.Cancel();
		_blockedAutomaticKingdomRebellionContext = null;
		_blockedDevForcedKingdomRebellionContext = null;
		_kingdomRebellionReopenAfterApiConfig = false;
		_kingdomRebellionReopenAfterApiConfigUtcTicks = 0L;
		_kingdomRebellionRuntime.ClearNamingMainThreadActions();
		_weeklyAutoSchedule.Clear();
		_weekZeroShortSummaries.ResetTransientRuntime();
		_voiceMappingJsonStorage = "";
		_voiceMappingExportFolderStorage = "";
		_unnamedPersonaJsonStorage = "";
		_patiencePersistence.ResetForCurrentSave();
		_dailyMaintenanceController.ClearQueuedJobs();
		ResetDailyMemoryDraftSealSliceState();
		_memoryBusinessState.ClearOverviewDiscoveryForCurrentSave();
		_pendingAutoWeeklyReportBuild = null;
		KingdomMaintenance.ResetRelations();
		ResetPendingWeeklyKingdomRebellionMaintenance();
		RebuildRuntimeDerivedIndexes();
		RewardSystemBehavior.Instance?.ImportDebtEntries(new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
		KingdomStrategicProfileBehavior.Instance?.ResetAllProfilesToDefaults();
		ClearKnowledgeDataForCurrentSave();
		ClearVoiceMappingDataForCurrentSave();
		ClearUnnamedPersonaDataForCurrentSave();
		Logger.Log("DevDataManagement", "Cleared all current-save AnimusForge data from dev menu.");
	}

	private void ClearKnowledgeDataForCurrentSave()
	{
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			knowledgeLibraryBehavior?.ImportRulesJson("{\"Version\":1,\"Rules\":[]}", overwrite: true);
		}
		catch (Exception ex)
		{
			Logger.Log("DevDataManagement", "[WARN] Clear Knowledge failed: " + ex.Message);
		}
	}

	private void ClearVoiceMappingDataForCurrentSave() => VoiceFiles.ClearVoiceMappingDataForCurrentSave();

	private void ClearUnnamedPersonaDataForCurrentSave() => PersonaProfileFiles.ClearUnnamedPersonaDataForCurrentSave(value => _unnamedPersonaJsonStorage = value);






























	private static string BuildNpcActionActorNarrativeText(NpcActionEntry entry) => MemoryEntityIdentityBannerlordAdapter.BuildNpcActionActorNarrativeText(entry);

	private static bool HasStructuredNpcActionMetadata(NpcActionEntry entry)
	{
		return entry != null && (!string.IsNullOrWhiteSpace(entry.ActionKind) || !string.IsNullOrWhiteSpace(entry.LocationText) || !string.IsNullOrWhiteSpace(entry.SettlementId) || !string.IsNullOrWhiteSpace(entry.TargetHeroId) || !string.IsNullOrWhiteSpace(entry.TargetClanId) || !string.IsNullOrWhiteSpace(entry.TargetKingdomId) || !string.IsNullOrWhiteSpace(entry.SettlementOwnerClanId) || !string.IsNullOrWhiteSpace(entry.SettlementOwnerKingdomId) || !string.IsNullOrWhiteSpace(entry.ActorHeroId) || !string.IsNullOrWhiteSpace(entry.ActorClanId) || !string.IsNullOrWhiteSpace(entry.ActorKingdomId));
	}


	private static string ResolveHeroDisplay(string heroId) => MemoryEntityIdentityBannerlordAdapter.ResolveHeroDisplay(heroId);

	internal static string ResolveClanDisplay(string clanId)
	{
		string text = ResolveClanName(clanId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (clanId ?? "").Trim();
	}

	internal static string ResolveKingdomDisplay(string kingdomId) => MemoryEntityIdentityBannerlordAdapter.ResolveKingdomDisplay(kingdomId);














	private static string BuildDevHistoryPreview(string line, int maxLen = 56)
		=> MemoryEditorProjection.BuildDevHistoryPreview(line, maxLen);





	private static bool TryParseDevSelectionInt(string id, string prefix, out int value) => DeveloperRootEditorController.TryParseDevSelectionInt(id, prefix, out value);































	private static DailyMemoryDraft FindDevDailyMemoryDraft(List<DailyMemoryDraft> drafts, int dayIndex) => MemoryHistoryCommitBannerlordAdapter.FindDevDailyMemoryDraft(drafts, dayIndex);

	private static DailyMemoryLine FindDevDailyMemoryLine(List<DailyMemoryDraft> drafts, int dayIndex, int lineIndex) => MemoryEditorProjection.FindDevDailyMemoryLine(FindDevDailyMemoryDraft(drafts, dayIndex), lineIndex);









































































	private void ReturnToDevRootMenu() => DeveloperRootEditorController.ReturnToRootMenu(GameMenu.SwitchToMenu);







	// The terminal owns the return callback, so this picker never falls through to the developer-data root menu.

	// Preflight reads every needed source before the destructive confirmation; it deliberately never imports AIConfig.json or prompt files.

	// This performs only strict file reads and bounded source scans, keeping every normal validation failure before confirmation non-destructive.

	// This is an explicit user-confirmed O(n) save update; none of its loops run on the campaign tick path.

	// Clear only NPC VoiceId fields, preserving all stored profile metadata, personality and background text byte-for-byte.

	// Read package JSON with a throwing UTF-8 decoder so an invalid byte sequence never becomes a silent, destructive default value.

	// Deserialization is intentionally strict for this replace flow; ordinary developer imports keep their existing tolerant behavior.

	// VoiceMapper treats unknown or wrongly typed properties as empty pools, so verify the complete export shape before overwrite can clear a live mapping.

	// Gather every supported lore source before confirmation.  This O(n) scan occurs only after the player explicitly chooses reload.

	// Preserve source entries with accidental duplicate IDs by deriving a deterministic in-memory ID; source files remain immutable.

	// Only same-original-ID collisions are removed; unrelated source rules still reach the strict validator and are rejected rather than silently changed.
	private static int RemoveDuplicateKeywordsFromDisambiguatedDatabaseRule(KnowledgeLibraryBehavior.LoreRule rule, string sourceLabel, string originalRuleId, Dictionary<string, string> keywordOwnerOriginalRuleIds)
		=> KnowledgeImportValidationOwner.RemoveDuplicateKeywordsFromDisambiguatedDatabaseRule(rule, sourceLabel, originalRuleId, keywordOwnerOriginalRuleIds);

	// File names are stable inside an exported package, with a numeric suffix only when a package repeats the same filename-derived identity.

	// Opening summaries are static world knowledge.  Dynamic EventRecords are purposely not read by this reload workflow.

	// Capture all mutable reload targets before the first replacement.  Serialized copies avoid aliasing existing save objects during rollback.

	// Roll back every subsystem that may have changed before a later subsystem rejected the preflighted plan.

	// Replace only static opening knowledge, then rebuild the derived week-zero entries while leaving all dynamic event records untouched.

	// Only canonical week-zero world/kingdom IDs are derived from opening summaries; every other event record remains save-owned dynamic history.
	private static bool IsDatabaseReloadOpeningEvent(EventRecordEntry entry) => WeeklyEventDataImportOwner.IsCanonicalOpeningEvent(entry);

	private static bool IsDatabaseReloadOpeningEventId(string eventId) => WeeklyEventDataImportOwner.IsCanonicalOpeningEventId(eventId);









	private MemoryImportExportState CaptureMemoryImportExportState()
		=> MemoryImportExportOwner.Capture(_memoryBusinessState);

	private CompressedMemoryExportBundle BuildCompressedMemoryExportBundle(string heroId)
		=> MemoryHistoryFiles.BuildCompressedMemoryExportBundle(heroId);

	private bool ApplyCompressedMemoryExportBundle(string heroId, CompressedMemoryExportBundle bundle, bool overwriteExisting)
		=> MemoryHistoryFiles.ApplyCompressedMemoryExportBundle(heroId, bundle, overwriteExisting);

	private bool HasCompressedMemoryDataForHero(string heroId)
		=> MemoryHistoryFiles.HasCompressedMemoryDataForHero(heroId);



























































}

public sealed class MyBehaviorSaveableTypeDefiner : SaveableTypeDefiner
{
	public MyBehaviorSaveableTypeDefiner()
		: base(711100)
	{
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(Dictionary<MobileParty, string>));
	}
}
