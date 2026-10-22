using AnimusForge;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class KingdomStrategicProfileBehavior : CampaignBehaviorBase
{
	private const string Source = "KingdomStrategicProfile";
	private const string SaveKey = "_af_kingdom_strategic_profiles_v1";
	private const string ExportDirectoryName = "kingdom_profiles";
	private const string FullExportFileName = "KingdomProfiles.json";
	private const int SchemaVersion = 1;
	private const int MaxProfileTextChars = 6000;
	private const long MaxImportFileBytes = 32L * 1024L * 1024L;
	private const int MaxImportProfileCount = 2048;
	private const int FoundingGenerationMaxAttempts = 3;
	private const int FoundingGenerationMaxTokens = 600;
	private const int FoundingGenerationTimeoutMilliseconds = 90000;

	private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
	private static readonly Dictionary<string, KingdomStrategicProfileTemplate> AuthoredDefaults = BuildAuthoredDefaults();

	private readonly ConcurrentQueue<FoundingProfileRequest> _foundingRequests = new ConcurrentQueue<FoundingProfileRequest>();
	private readonly ConcurrentQueue<FoundingProfileResult> _foundingResults = new ConcurrentQueue<FoundingProfileResult>();
	private readonly HashSet<string> _queuedFoundingKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Kingdom> _runtimeKingdomCache = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);

	private KingdomStrategicProfileStorage _storage = new KingdomStrategicProfileStorage();
	private bool _foundingRequestRunning;
	private volatile bool _hasFoundingWork;
	private string _activeFoundingKingdomId = "";
	private long _activeFoundingRuntimeGeneration;
	private bool _acceptRuntimeFoundings;
	private int _lastMissingApiLogDay = -1;

	public static KingdomStrategicProfileBehavior Instance { get; private set; }

	public KingdomStrategicProfileBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			NormalizeStorage(ensureCurrentKingdoms: true);
			string json = JsonConvert.SerializeObject(_storage, Formatting.None);
			CampaignSaveChunkHelper.SaveChunkedString(dataStore, SaveKey, json, Source);
			return;
		}
		if (!dataStore.IsLoading)
		{
			return;
		}
		try
		{
			string json = CampaignSaveChunkHelper.LoadChunkedString(dataStore, SaveKey, Source);
			_storage = string.IsNullOrWhiteSpace(json)
				? new KingdomStrategicProfileStorage()
				: JsonConvert.DeserializeObject<KingdomStrategicProfileStorage>(json) ?? new KingdomStrategicProfileStorage();
		}
		catch (Exception ex) when (!(ex is CampaignSaveChunkHelper.PersistenceIntegrityException))
		{
			_storage = new KingdomStrategicProfileStorage();
			Log("load failed: " + ex.Message);
		}
		ResetTransientRuntime("load");
		NormalizeStorage(ensureCurrentKingdoms: false, recoverInterruptedGeneration: true);
	}

	internal int GetProfileCountForDev()
	{
		return _storage?.Profiles?.Count ?? 0;
	}

	internal int GetPlayerOverrideCountForDev()
	{
		return _storage?.Profiles?.Values.Count(x => x?.IsPlayerOverride == true) ?? 0;
	}

	internal bool TryGetEffectiveProfile(string kingdomId, out string nationalPersonality, out string longTermStrategy)
	{
		nationalPersonality = "";
		longTermStrategy = "";
		string id = NormalizeId(kingdomId);
		if (string.IsNullOrEmpty(id) || _storage?.Profiles == null || !_storage.Profiles.TryGetValue(id, out KingdomStrategicProfileRecord profile) || profile == null)
		{
			return false;
		}
		nationalPersonality = profile.NationalPersonality ?? "";
		longTermStrategy = profile.LongTermStrategy ?? "";
		return !string.IsNullOrWhiteSpace(nationalPersonality) || !string.IsNullOrWhiteSpace(longTermStrategy);
	}

	internal bool TryGetOrCreateEffectiveProfile(Kingdom kingdom, out string nationalPersonality, out string longTermStrategy)
	{
		nationalPersonality = "";
		longTermStrategy = "";
		if (kingdom == null)
		{
			return false;
		}

		string id = NormalizeId(kingdom.StringId);
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		if (_storage?.Profiles != null
			&& _storage.Profiles.TryGetValue(id, out KingdomStrategicProfileRecord existing)
			&& existing != null)
		{
			nationalPersonality = existing.NationalPersonality ?? "";
			longTermStrategy = existing.LongTermStrategy ?? "";
			return true;
		}

		bool runtimeFounded = _acceptRuntimeFoundings && !AuthoredDefaults.ContainsKey(id);
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded);
		if (profile == null)
		{
			return false;
		}
		if (runtimeFounded && profile.RequiresFoundingGeneration)
		{
			QueueFoundingGeneration(kingdom, force: false, showConfigError: false);
		}
		nationalPersonality = profile.NationalPersonality ?? "";
		longTermStrategy = profile.LongTermStrategy ?? "";
		return true;
	}

	internal bool ExportAllToDirectory(string exportRoot, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			if (string.IsNullOrWhiteSpace(exportRoot))
			{
				detailMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportDirectoryEmpty", "The export directory is empty.");
				return false;
			}
			EnsureCurrentKingdomProfiles(runtimeFounded: false);
			string directory = Path.Combine(Path.GetFullPath(exportRoot), ExportDirectoryName);
			Directory.CreateDirectory(directory);
			KingdomStrategicProfileExportPackage package = BuildExportPackage(_storage.Profiles.Values);
			WriteJsonAtomic(Path.Combine(directory, FullExportFileName), package);
			detailMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ExportedCount", "Exported {COUNT} kingdom profile(s).", ("COUNT", package.Profiles.Count.ToString(CultureInfo.InvariantCulture)));
			return true;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			Log("export failed: " + ex);
			return false;
		}
	}

	internal bool ExportSingleToDirectory(string exportRoot, Kingdom kingdom, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			if (kingdom == null || string.IsNullOrWhiteSpace(kingdom.StringId))
			{
				detailMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportTargetNotFound", "The kingdom to export could not be found.");
				return false;
			}
			KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
			if (profile == null)
			{
				detailMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportProfileNotFound", "The kingdom profile does not exist.");
				return false;
			}
			string directory = Path.Combine(Path.GetFullPath(exportRoot), ExportDirectoryName, "kingdoms");
			Directory.CreateDirectory(directory);
			string fileName = BuildSingleExportFileName(profile);
			WriteJsonAtomic(Path.Combine(directory, fileName), CloneForExport(profile));
			detailMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ExportedPrefix", "Exported: {KINGDOM}", ("KINGDOM", profile.KingdomName ?? profile.KingdomId));
			return true;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			Log("single export failed: " + ex);
			return false;
		}
	}

	internal bool InspectImportDirectory(string importRoot, out int totalCount, out int duplicateCount, out int skippedCount, out string errorMessage)
	{
		totalCount = 0;
		duplicateCount = 0;
		skippedCount = 0;
		errorMessage = "";
		try
		{
			EnsureCurrentKingdomProfiles(runtimeFounded: false);
			if (!TryLoadImportEntries(importRoot, out List<KingdomStrategicProfileRecord> entries, out int invalidCount, out string loadError, allowMissing: true))
			{
				errorMessage = loadError;
				return false;
			}
			KingdomImportTargetIndex targets = BuildImportTargetIndex();
			HashSet<string> resolvedTargetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KingdomStrategicProfileRecord entry in entries)
			{
				if (!TryResolveImportTarget(entry, targets, out Kingdom kingdom, out _))
				{
					skippedCount++;
					continue;
				}
				string id = NormalizeId(kingdom.StringId);
				if (!resolvedTargetIds.Add(id))
				{
					skippedCount++;
					continue;
				}
				totalCount++;
				if (_storage.Profiles.TryGetValue(id, out KingdomStrategicProfileRecord current) && HasImportCollision(current))
				{
					duplicateCount++;
				}
			}
			skippedCount += invalidCount;
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	// The U-terminal prepares one immutable source snapshot so confirmation never re-reads a package that may have changed on disk.
	internal bool TryBuildDatabaseReloadPlan(string importRoot, out KingdomDatabaseReloadPlan plan, out string errorMessage)
	{
		plan = null;
		errorMessage = "";
		try
		{
			if (!TryLoadImportEntries(importRoot, out List<KingdomStrategicProfileRecord> entries, out int invalidCount, out string loadError, allowMissing: false))
			{
				errorMessage = loadError;
				return false;
			}
			if (invalidCount > 0)
			{
				errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_InvalidProfileCount", "The package contains {COUNT} invalid or duplicate kingdom profile(s).", ("COUNT", invalidCount.ToString(CultureInfo.InvariantCulture)));
				return false;
			}
			KingdomImportTargetIndex kingdomImportTargetIndex = BuildImportTargetIndex();
			Dictionary<string, KingdomStrategicProfileRecord> dictionary = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> preservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> skippedLabels = new List<string>();
			int skippedCount = 0;
			int skippedStaticCount = 0;
			var skippedStaticLabels = new List<string>();
			foreach (KingdomStrategicProfileRecord entry in entries)
			{
				if (!TryResolveImportTarget(entry, kingdomImportTargetIndex, out Kingdom kingdom, out string warning) || kingdom == null)
				{
					if (IsDynamicKingdomId(entry.KingdomId))
					{
						string skippedId = NormalizeId(entry.KingdomId);
						preservedIds.Add(skippedId);
						if (skippedLabels.Count < 8) skippedLabels.Add(skippedId + " (" + (entry.KingdomName ?? "") + ")");
						skippedCount++;
						continue;
					}
					string unmatchedId = NormalizeId(entry.KingdomId);
					skippedStaticLabels.Add(unmatchedId + " (" + (entry.KingdomName ?? "") + ")");
					if (++skippedStaticCount > 3)
					{
						errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_UnmatchedLimitExceeded", "More than 3 non-dynamic kingdom profiles could not be matched. Import stopped without changing the save. First unmatched entries: {PROFILES}", ("PROFILES", string.Join(", ", skippedStaticLabels)));
						return false;
					}
					preservedIds.Add(unmatchedId);
					continue;
				}
				string text = NormalizeId(kingdom.StringId);
				if (string.IsNullOrWhiteSpace(text) || dictionary.ContainsKey(text))
				{
					errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_DuplicateTarget", "The package contains a duplicate or invalid kingdom target: {TARGET}", ("TARGET", kingdom.Name?.ToString() ?? text));
					return false;
				}
				dictionary[text] = CloneForExport(entry);
			}
			if (dictionary.Count <= 0 && skippedCount == 0 && skippedStaticCount == 0)
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NoReloadableProfiles", "The package contains no kingdom profiles that can be reloaded.");
				return false;
			}
			// Preflight must not initialize missing profiles in the current save.  Count only live campaign kingdoms for the confirmation text.
			preservedIds.ExceptWith(dictionary.Keys);
			int num = GetCurrentKingdoms().Count((Kingdom x) => x != null && !dictionary.ContainsKey(NormalizeId(x.StringId)) && !preservedIds.Contains(NormalizeId(x.StringId)));
			plan = new KingdomDatabaseReloadPlan
			{
				SourceProfilesByTargetId = dictionary,
				CurrentProfilesResetToDefaultCount = num,
				PreservedProfileIds = preservedIds,
				SkippedDynamicProfileCount = skippedCount,
				SkippedDynamicProfileLabels = skippedLabels,
				SkippedStaticProfileCount = skippedStaticCount,
				SkippedStaticProfileLabels = skippedStaticLabels
			};
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			Log("database reload plan failed: " + ex);
			return false;
		}
	}

	// A serialized detached copy lets the terminal restore kingdom state if a future step throws after the candidate swap.
	internal bool TryCaptureDatabaseReloadRollbackJson(out string snapshotJson, out string errorMessage)
	{
		snapshotJson = "";
		errorMessage = "";
		try
		{
			snapshotJson = JsonConvert.SerializeObject(_storage ?? new KingdomStrategicProfileStorage(), Formatting.None);
			if (string.IsNullOrWhiteSpace(snapshotJson))
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SerializationEmpty", "The serialized rollback snapshot is empty.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			Log("database reload snapshot failed: " + ex);
			return false;
		}
	}

	// Restore is used only by the same user-confirmed reload click; no normalization is applied so the previous save state is retained verbatim.
	internal bool TryRestoreDatabaseReloadRollbackJson(string snapshotJson, out string errorMessage)
	{
		errorMessage = "";
		try
		{
			if (string.IsNullOrWhiteSpace(snapshotJson))
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RollbackSnapshotEmpty", "The rollback snapshot is empty.");
				return false;
			}
			KingdomStrategicProfileStorage restoredStorage = JsonConvert.DeserializeObject<KingdomStrategicProfileStorage>(snapshotJson);
			if (restoredStorage == null)
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RollbackSnapshotInvalid", "The rollback snapshot is invalid.");
				return false;
			}
			restoredStorage.Profiles ??= new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
			_storage = restoredStorage;
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			Log("database reload rollback failed: " + ex);
			return false;
		}
	}

	// Apply a fully parsed plan on a cloned storage object, so a failure cannot leave a reset-but-unimported kingdom profile set.
	internal bool TryApplyDatabaseReloadPlan(KingdomDatabaseReloadPlan plan, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			if (plan?.SourceProfilesByTargetId == null || (plan.SourceProfilesByTargetId.Count <= 0 && plan.SkippedDynamicProfileCount <= 0 && plan.SkippedStaticProfileCount <= 0))
			{
				detailMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ReloadPlanEmpty", "The kingdom reload plan is empty.");
				return false;
			}
			// Never call EnsureCurrentKingdomProfiles here: it writes directly to the live save.  Build and normalize a complete candidate instead.
			KingdomStrategicProfileStorage kingdomStrategicProfileStorage = BuildDatabaseReloadCandidateStorage(plan.PreservedProfileIds);
			if (kingdomStrategicProfileStorage?.Profiles == null)
			{
				detailMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ReloadCloneFailed", "The kingdom profile reload copy could not be created.");
				return false;
			}
			int currentCampaignDay = GetCurrentCampaignDay();
			foreach (KeyValuePair<string, KingdomStrategicProfileRecord> profile in kingdomStrategicProfileStorage.Profiles)
			{
				if (!plan.PreservedProfileIds.Contains(NormalizeId(profile.Key)))
					ResetProfileToDefaultForDatabaseReload(profile.Value, currentCampaignDay);
			}
			foreach (KeyValuePair<string, KingdomStrategicProfileRecord> sourceProfileByTargetId in plan.SourceProfilesByTargetId)
			{
				if (!kingdomStrategicProfileStorage.Profiles.TryGetValue(NormalizeId(sourceProfileByTargetId.Key), out KingdomStrategicProfileRecord value2) || value2 == null)
				{
					detailMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ReloadTargetMissing", "The world changed and the kingdom target could not be found: {TARGET}", ("TARGET", sourceProfileByTargetId.Key));
					return false;
				}
				ApplyDatabaseSourceProfile(value2, sourceProfileByTargetId.Value, currentCampaignDay);
			}
			_storage = kingdomStrategicProfileStorage;
			detailMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ReloadComplete", "Reloaded {IMPORTED} kingdom profile(s); the other {RESET} current kingdom profile(s) returned to their defaults.", ("IMPORTED", plan.SourceProfilesByTargetId.Count.ToString(CultureInfo.InvariantCulture)), ("RESET", plan.CurrentProfilesResetToDefaultCount.ToString(CultureInfo.InvariantCulture)));
			if (plan.SkippedDynamicProfileCount > 0)
				detailMessage += " " + DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ReloadSkippedDynamic", "Skipped {COUNT} unmatched dynamic kingdom profile(s); existing save profiles for those IDs were preserved. First entries: {PROFILES}", ("COUNT", plan.SkippedDynamicProfileCount.ToString(CultureInfo.InvariantCulture)), ("PROFILES", string.Join(", ", plan.SkippedDynamicProfileLabels)));
			if (plan.SkippedStaticProfileCount > 0)
				detailMessage += " " + DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ReloadSkippedStatic", "Skipped {COUNT} unmatched non-dynamic kingdom profile(s) (maximum 3): {PROFILES}", ("COUNT", plan.SkippedStaticProfileCount.ToString(CultureInfo.InvariantCulture)), ("PROFILES", string.Join(", ", plan.SkippedStaticProfileLabels)));
			return true;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			Log("database reload apply failed: " + ex);
			return false;
		}
	}

	internal bool ImportAllFromDirectory(string importRoot, bool overwriteExisting, out string detailMessage)
	{
		return ImportFromDirectoryInternal(importRoot, null, overwriteExisting, out detailMessage);
	}

	internal bool ImportSingleFromDirectory(string importRoot, Kingdom targetKingdom, bool overwriteExisting, out string detailMessage)
	{
		return ImportFromDirectoryInternal(importRoot, targetKingdom, overwriteExisting, out detailMessage);
	}

	internal void ResetAllProfilesToDefaults()
	{
		EnsureCurrentKingdomProfiles(runtimeFounded: false);
		if (_storage?.Profiles == null)
		{
			return;
		}
		int day = GetCurrentCampaignDay();
		foreach (KingdomStrategicProfileRecord profile in _storage.Profiles.Values)
		{
			if (profile == null)
			{
				continue;
			}
			profile.NationalPersonality = profile.DefaultNationalPersonality ?? "";
			profile.LongTermStrategy = profile.DefaultLongTermStrategy ?? "";
			profile.HasPersonalityOverride = false;
			profile.HasStrategyOverride = false;
			profile.IsPlayerOverride = false;
			profile.UpdatedDay = day;
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		_storage = new KingdomStrategicProfileStorage();
		ResetTransientRuntime("new-game");
		EnsureCurrentKingdomProfiles(runtimeFounded: false);
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ResetTransientRuntime("game-loaded");
		NormalizeStorage(ensureCurrentKingdoms: true, recoverInterruptedGeneration: true);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		NormalizeStorage(ensureCurrentKingdoms: true);
		_acceptRuntimeFoundings = true;
		QueueEligibleFoundingProfiles(force: false);
	}

	private void OnKingdomCreated(Kingdom kingdom)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(kingdom.StringId))
		{
			return;
		}
		bool runtimeFounded = _acceptRuntimeFoundings && !AuthoredDefaults.ContainsKey(NormalizeId(kingdom.StringId));
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded);
		if (runtimeFounded && profile?.RequiresFoundingGeneration == true)
		{
			QueueFoundingGeneration(kingdom, force: false, showConfigError: false);
		}
	}

	private void OnCampaignTick(float dt)
	{
		if (!_hasFoundingWork)
		{
			return;
		}
		ProcessFoundingResults();
		TryStartNextFoundingRequest();
		_hasFoundingWork = _foundingRequestRunning || !_foundingRequests.IsEmpty || !_foundingResults.IsEmpty;
	}

	private void OnDailyTick()
	{
		QueueEligibleFoundingProfiles(force: false);
	}

	private void ResetTransientRuntime(string reason)
	{
		while (_foundingRequests.TryDequeue(out _))
		{
		}
		while (_foundingResults.TryDequeue(out _))
		{
		}
		_queuedFoundingKingdomIds.Clear();
		_runtimeKingdomCache.Clear();
		_foundingRequestRunning = false;
		_hasFoundingWork = false;
		_activeFoundingKingdomId = "";
		_activeFoundingRuntimeGeneration = 0L;
		_acceptRuntimeFoundings = false;
		_lastMissingApiLogDay = -1;
		Log("runtime reset: " + (reason ?? ""));
	}

	private void NormalizeStorage(bool ensureCurrentKingdoms, bool recoverInterruptedGeneration = false)
	{
		_storage ??= new KingdomStrategicProfileStorage();
		_storage.Version = SchemaVersion;
		_storage.Profiles ??= new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, Kingdom> kingdoms = GetCurrentKingdoms()
			.GroupBy(x => NormalizeId(x.StringId), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		Dictionary<string, KingdomStrategicProfileRecord> normalized = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, KingdomStrategicProfileRecord> pair in _storage.Profiles)
		{
			string id = NormalizeId(pair.Value?.KingdomId ?? pair.Key);
			if (string.IsNullOrEmpty(id) || pair.Value == null)
			{
				continue;
			}
			kingdoms.TryGetValue(id, out Kingdom kingdom);
			NormalizeProfile(pair.Value, kingdom, recoverInterruptedGeneration);
			pair.Value.KingdomId = id;
			normalized[id] = pair.Value;
		}
		_storage.Profiles = normalized;
		if (ensureCurrentKingdoms)
		{
			foreach (Kingdom kingdom in kingdoms.Values)
			{
				EnsureProfile(kingdom, runtimeFounded: false);
			}
		}
	}

	private void EnsureCurrentKingdomProfiles(bool runtimeFounded)
	{
		_storage ??= new KingdomStrategicProfileStorage();
		_storage.Profiles ??= new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
		foreach (Kingdom kingdom in GetCurrentKingdoms())
		{
			EnsureProfile(kingdom, runtimeFounded);
		}
	}

	private KingdomStrategicProfileRecord EnsureProfile(Kingdom kingdom, bool runtimeFounded)
	{
		if (kingdom == null)
		{
			return null;
		}
		string id = NormalizeId(kingdom.StringId);
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		_runtimeKingdomCache[id] = kingdom;
		_storage ??= new KingdomStrategicProfileStorage();
		_storage.Profiles ??= new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
		if (_storage.Profiles.TryGetValue(id, out KingdomStrategicProfileRecord existing) && existing != null)
		{
			NormalizeProfile(existing, kingdom, recoverInterruptedGeneration: false);
			return existing;
		}

		KingdomStrategicProfileTemplate template;
		bool requiresGeneration = (runtimeFounded || IsDynamicKingdomId(id)) && !AuthoredDefaults.TryGetValue(id, out template);
		if (requiresGeneration)
		{
			template = BuildFoundingFallback(kingdom);
		}
		else if (!AuthoredDefaults.TryGetValue(id, out template))
		{
			template = BuildGenericDefault(kingdom);
		}
		int day = GetCurrentCampaignDay();
		KingdomStrategicProfileRecord profile = new KingdomStrategicProfileRecord
		{
			KingdomId = id,
			KingdomName = GetKingdomName(kingdom),
			CultureId = kingdom.Culture?.StringId ?? "",
			RulerHeroId = GetRuler(kingdom)?.StringId ?? "",
			DefaultNationalPersonality = CleanProfileText(template.NationalPersonality),
			DefaultLongTermStrategy = CleanProfileText(template.LongTermStrategy),
			NationalPersonality = CleanProfileText(template.NationalPersonality),
			LongTermStrategy = CleanProfileText(template.LongTermStrategy),
			DefaultSource = requiresGeneration ? "founding_fallback" : (AuthoredDefaults.ContainsKey(id) ? "authored_default" : "generic_default"),
			IsPlayerOverride = false,
			RequiresFoundingGeneration = requiresGeneration,
			GenerationState = requiresGeneration ? "pending" : "not_required",
			GenerationAttemptCount = 0,
			NextGenerationRetryDay = day,
			CreatedDay = day,
			UpdatedDay = day
		};
		_storage.Profiles[id] = profile;
		Log("profile created kingdom=" + id + " source=" + profile.DefaultSource);
		return profile;
	}

	private void NormalizeProfile(KingdomStrategicProfileRecord profile, Kingdom kingdom, bool recoverInterruptedGeneration)
	{
		if (profile == null)
		{
			return;
		}
		profile.KingdomId = NormalizeId(profile.KingdomId ?? kingdom?.StringId);
		if (kingdom != null)
		{
			_runtimeKingdomCache[profile.KingdomId] = kingdom;
			profile.KingdomName = GetKingdomName(kingdom);
			profile.CultureId = kingdom.Culture?.StringId ?? profile.CultureId ?? "";
			profile.RulerHeroId = GetRuler(kingdom)?.StringId ?? profile.RulerHeroId ?? "";
		}
		profile.KingdomName = CleanSingleLine(profile.KingdomName, 240);
		profile.CultureId = CleanSingleLine(profile.CultureId, 160);
		profile.RulerHeroId = CleanSingleLine(profile.RulerHeroId, 240);
		profile.DefaultNationalPersonality = CleanProfileText(profile.DefaultNationalPersonality);
		profile.DefaultLongTermStrategy = CleanProfileText(profile.DefaultLongTermStrategy);
		if (string.IsNullOrWhiteSpace(profile.DefaultNationalPersonality) && string.IsNullOrWhiteSpace(profile.DefaultLongTermStrategy))
		{
			KingdomStrategicProfileTemplate fallback = kingdom == null ? BuildOrphanFallback(profile.KingdomName) : BuildGenericDefault(kingdom);
			profile.DefaultNationalPersonality = fallback.NationalPersonality;
			profile.DefaultLongTermStrategy = fallback.LongTermStrategy;
			profile.DefaultSource = "generic_default";
		}
		profile.NationalPersonality = CleanProfileText(profile.NationalPersonality);
		profile.LongTermStrategy = CleanProfileText(profile.LongTermStrategy);
		if (profile.IsPlayerOverride && !profile.HasPersonalityOverride && !profile.HasStrategyOverride)
		{
			profile.HasPersonalityOverride = true;
			profile.HasStrategyOverride = true;
		}
		profile.IsPlayerOverride = profile.HasPersonalityOverride || profile.HasStrategyOverride;
		if (!profile.HasPersonalityOverride)
		{
			profile.NationalPersonality = profile.DefaultNationalPersonality;
		}
		if (!profile.HasStrategyOverride)
		{
			profile.LongTermStrategy = profile.DefaultLongTermStrategy;
		}
		profile.DefaultSource = CleanSingleLine(profile.DefaultSource, 80);
		if (string.IsNullOrWhiteSpace(profile.DefaultSource))
		{
			profile.DefaultSource = "generic_default";
		}
		if (kingdom != null
			&& IsDynamicKingdomId(profile.KingdomId)
			&& string.Equals(profile.DefaultSource, "generic_default", StringComparison.OrdinalIgnoreCase)
			&& !profile.RequiresFoundingGeneration)
		{
			KingdomStrategicProfileTemplate foundingFallback = BuildFoundingFallback(kingdom);
			profile.DefaultNationalPersonality = CleanProfileText(foundingFallback.NationalPersonality);
			profile.DefaultLongTermStrategy = CleanProfileText(foundingFallback.LongTermStrategy);
			profile.DefaultSource = "founding_fallback";
			profile.RequiresFoundingGeneration = true;
			profile.GenerationState = "pending";
			profile.GenerationAttemptCount = 0;
			profile.NextGenerationRetryDay = GetCurrentCampaignDay();
			if (!profile.HasPersonalityOverride)
			{
				profile.NationalPersonality = profile.DefaultNationalPersonality;
			}
			if (!profile.HasStrategyOverride)
			{
				profile.LongTermStrategy = profile.DefaultLongTermStrategy;
			}
		}
		if (string.Equals(profile.DefaultSource, "founding_fallback", StringComparison.OrdinalIgnoreCase) && !profile.RequiresFoundingGeneration)
		{
			profile.RequiresFoundingGeneration = true;
			profile.GenerationState = "pending";
		}
		profile.GenerationState = CleanSingleLine(profile.GenerationState, 40);
		if (profile.RequiresFoundingGeneration)
		{
			if (string.IsNullOrWhiteSpace(profile.GenerationState)
				|| (recoverInterruptedGeneration && string.Equals(profile.GenerationState, "running", StringComparison.OrdinalIgnoreCase)))
			{
				profile.GenerationState = "pending";
			}
		}
		else if (string.IsNullOrWhiteSpace(profile.GenerationState))
		{
			profile.GenerationState = string.Equals(profile.DefaultSource, "llm_founding", StringComparison.OrdinalIgnoreCase) ? "complete" : "not_required";
		}
		profile.GenerationAttemptCount = Math.Max(0, profile.GenerationAttemptCount);
		profile.NextGenerationRetryDay = Math.Max(0, profile.NextGenerationRetryDay);
		profile.LastGenerationError = CleanSingleLine(profile.LastGenerationError, 1000);
		profile.CreatedDay = Math.Max(0, profile.CreatedDay);
		profile.UpdatedDay = Math.Max(profile.CreatedDay, profile.UpdatedDay);
	}

	private void QueueEligibleFoundingProfiles(bool force)
	{
		if (_storage?.Profiles == null || _storage.Profiles.Count == 0)
		{
			return;
		}
		foreach (KingdomStrategicProfileRecord profile in _storage.Profiles.Values)
		{
			if (profile?.RequiresFoundingGeneration != true || string.IsNullOrWhiteSpace(profile.KingdomId))
			{
				continue;
			}
			if (!_runtimeKingdomCache.TryGetValue(NormalizeId(profile.KingdomId), out Kingdom kingdom) || kingdom == null)
			{
				continue;
			}
			if (kingdom.IsEliminated)
			{
				continue;
			}
			QueueFoundingGeneration(kingdom, force, showConfigError: false);
		}
	}

	private bool QueueFoundingGeneration(Kingdom kingdom, bool force, bool showConfigError)
	{
		if (kingdom == null || kingdom.IsEliminated)
		{
			if (showConfigError && kingdom?.IsEliminated == true)
			{
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EliminatedGenerationSkipped", "The eliminated kingdom keeps its current profile; no new LLM generation will be requested.")));
			}
			return false;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: true);
		if (profile == null)
		{
			return false;
		}
		if (force)
		{
			profile.RequiresFoundingGeneration = true;
			profile.GenerationState = "pending";
			profile.GenerationAttemptCount = 0;
			profile.NextGenerationRetryDay = GetCurrentCampaignDay();
			profile.LastGenerationError = "";
		}
		if (!profile.RequiresFoundingGeneration)
		{
			return false;
		}
		int day = GetCurrentCampaignDay();
		if (!force && (profile.GenerationAttemptCount >= FoundingGenerationMaxAttempts || day < profile.NextGenerationRetryDay))
		{
			return false;
		}
		string id = NormalizeId(profile.KingdomId);
		if (_queuedFoundingKingdomIds.Contains(id))
		{
			return false;
		}
		if (!NpcPolicyLlmClient.IsConfiguredForNpcPolicy(out string configError))
		{
			profile.GenerationState = "pending";
			profile.LastGenerationError = string.IsNullOrWhiteSpace(configError) ? DiplomacyMiscPromptTexts.Get("VassalageBehavior_ApiConfigError", "Event and rebellion API not configured.") : configError;
			if (showConfigError)
			{
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationConfigError", "The kingdom founding profile could not be generated because the event/rebellion API is not configured.")));
			}
			if (_lastMissingApiLogDay != day)
			{
				_lastMissingApiLogDay = day;
				Log("founding generation waiting for API configuration");
			}
			return false;
		}
		FoundingProfileRequest request = new FoundingProfileRequest
		{
			KingdomId = id,
			KingdomName = GetKingdomName(kingdom),
			RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration()
		};
		_foundingRequests.Enqueue(request);
		_queuedFoundingKingdomIds.Add(id);
		_hasFoundingWork = true;
		profile.GenerationState = "pending";
		profile.LastGenerationError = "";
		Log("founding generation queued kingdom=" + id);
		return true;
	}

	private void TryStartNextFoundingRequest()
	{
		if (_foundingRequestRunning || !_foundingRequests.TryDequeue(out FoundingProfileRequest request) || request == null)
		{
			return;
		}
		if (SaveRuntimeGuard.IsStale(request.RuntimeGeneration, Source + "_request_start"))
		{
			_queuedFoundingKingdomIds.Remove(NormalizeId(request.KingdomId));
			return;
		}
		if (_storage?.Profiles == null || !_storage.Profiles.TryGetValue(NormalizeId(request.KingdomId), out KingdomStrategicProfileRecord profile) || profile == null || !profile.RequiresFoundingGeneration)
		{
			_queuedFoundingKingdomIds.Remove(NormalizeId(request.KingdomId));
			return;
		}
		if (!_runtimeKingdomCache.TryGetValue(NormalizeId(request.KingdomId), out Kingdom kingdom) || kingdom == null || kingdom.IsEliminated)
		{
			_queuedFoundingKingdomIds.Remove(NormalizeId(request.KingdomId));
			profile.GenerationState = kingdom?.IsEliminated == true ? "paused_eliminated" : "failed";
			profile.LastGenerationError = kingdom?.IsEliminated == true
				? DiplomacyMiscPromptTexts.Get("VassalageBehavior_KingdomEliminated", "The kingdom has been eliminated; automatic founding card generation stopped.")
				: DiplomacyMiscPromptTexts.Get("VassalageBehavior_KingdomMissing", "The kingdom object no longer exists; cannot generate founding card.");
			return;
		}
		request.KingdomName = GetKingdomName(kingdom);
		request.SystemPrompt = BuildFoundingProfilePrompt(kingdom);
		_foundingRequestRunning = true;
		_activeFoundingKingdomId = NormalizeId(request.KingdomId);
		_activeFoundingRuntimeGeneration = request.RuntimeGeneration;
		profile.GenerationState = "running";
		profile.GenerationAttemptCount++;
		try
		{
			_ = Task.Run(() => ProcessFoundingRequestAsync(request));
		}
		catch (Exception ex)
		{
			_foundingRequestRunning = false;
			_activeFoundingKingdomId = "";
			_activeFoundingRuntimeGeneration = 0L;
			_queuedFoundingKingdomIds.Remove(NormalizeId(request.KingdomId));
			profile.GenerationState = "failed";
			profile.LastGenerationError = CleanSingleLine(ex.Message, 1000);
			profile.NextGenerationRetryDay = GetCurrentCampaignDay() + 1;
		}
	}

	private async Task ProcessFoundingRequestAsync(FoundingProfileRequest request)
	{
		FoundingProfileResult result = new FoundingProfileResult
		{
			KingdomId = request?.KingdomId ?? "",
			KingdomName = request?.KingdomName ?? "",
			RuntimeGeneration = request?.RuntimeGeneration ?? 0L
		};
		try
		{
			if (request == null || SaveRuntimeGuard.IsStale(request.RuntimeGeneration, Source + "_api_start"))
			{
				result.ErrorMessage = SaveRuntimeGuard.BuildStaleRequestErrorText();
			}
			else
			{
				PromptPackage prompt = LegacyPolicyLlmGateway.BuildPromptPackage(
					request.SystemPrompt,
					FoundingGenerationMaxTokens,
					"event-and-rebellion");
				LlmGenerateRequest gatewayRequest = LegacyPolicyLlmGateway.BuildRequest(
					prompt,
					null,
					Source,
					request.RuntimeGeneration,
					FoundingGenerationTimeoutMilliseconds,
					InteractionStage.MainReply);
				LlmGenerateResult gatewayResult = await new LegacyPolicyLlmGateway(eventAndRebellionRoute: true)
					.GenerateAsync(gatewayRequest, CancellationToken.None).ConfigureAwait(false);
				NpcPolicyApiCallResult apiResult = LegacyPolicyLlmGateway.ToLegacyResult(gatewayResult);
				if (!apiResult.Success)
				{
									result.ErrorMessage = apiResult.ErrorMessage ?? DiplomacyMiscPromptTexts.Get("WorldDiplomacy_ApiRequestFailed", "API request failed.");
				}
				else if (TryParseFoundingProfile(apiResult.Content, out string personality, out string strategy, out string parseError))
				{
					result.Success = true;
					result.NationalPersonality = personality;
					result.LongTermStrategy = strategy;
				}
				else
				{
					result.ErrorMessage = parseError;
				}
			}
		}
		catch (Exception ex)
		{
			result.ErrorMessage = ex.Message;
		}
		finally
		{
			_foundingResults.Enqueue(result);
			_hasFoundingWork = true;
		}
	}

	private void ProcessFoundingResults()
	{
		while (_foundingResults.TryDequeue(out FoundingProfileResult result))
		{
			string id = NormalizeId(result?.KingdomId);
			bool ownsActiveRequest = result != null
				&& string.Equals(id, _activeFoundingKingdomId, StringComparison.OrdinalIgnoreCase)
				&& result.RuntimeGeneration == _activeFoundingRuntimeGeneration;
			if (result == null || string.IsNullOrEmpty(id) || SaveRuntimeGuard.IsStale(result.RuntimeGeneration, Source + "_commit"))
			{
				if (ownsActiveRequest)
				{
					_foundingRequestRunning = false;
					_activeFoundingKingdomId = "";
					_activeFoundingRuntimeGeneration = 0L;
					_queuedFoundingKingdomIds.Remove(id);
				}
				continue;
			}
			if (!ownsActiveRequest)
			{
				Log("founding result discarded because it no longer owns the active request kingdom=" + id);
				continue;
			}
			_foundingRequestRunning = false;
			_activeFoundingKingdomId = "";
			_activeFoundingRuntimeGeneration = 0L;
			_queuedFoundingKingdomIds.Remove(id);
			if (_storage?.Profiles == null || !_storage.Profiles.TryGetValue(id, out KingdomStrategicProfileRecord profile) || profile == null || !profile.RequiresFoundingGeneration)
			{
				continue;
			}
			int day = GetCurrentCampaignDay();
			if (result.Success)
			{
				profile.DefaultNationalPersonality = CleanProfileText(result.NationalPersonality);
				profile.DefaultLongTermStrategy = CleanProfileText(result.LongTermStrategy);
				profile.DefaultSource = "llm_founding";
				profile.RequiresFoundingGeneration = false;
				profile.GenerationState = "complete";
				profile.LastGenerationError = "";
				profile.NextGenerationRetryDay = 0;
				if (!profile.HasPersonalityOverride)
				{
					profile.NationalPersonality = profile.DefaultNationalPersonality;
				}
				if (!profile.HasStrategyOverride)
				{
					profile.LongTermStrategy = profile.DefaultLongTermStrategy;
				}
				profile.UpdatedDay = day;
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationCompletedPrefix", "Generated and locked the national personality and long-term strategy for new kingdom “") + (profile.KingdomName ?? id) + DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationCompletedSuffix", "”.")));
				Log("founding generation committed kingdom=" + id);
			}
			else
			{
				profile.GenerationState = "failed";
				profile.LastGenerationError = CleanSingleLine(result.ErrorMessage, 1000);
				int backoffDays = Math.Min(7, Math.Max(1, 1 << Math.Min(3, profile.GenerationAttemptCount)));
				profile.NextGenerationRetryDay = day + backoffDays;
				profile.UpdatedDay = day;
				Log("founding generation failed kingdom=" + id + " attempt=" + profile.GenerationAttemptCount.ToString(CultureInfo.InvariantCulture) + " error=" + profile.LastGenerationError);
			}
		}
	}

	private bool ImportFromDirectoryInternal(string importRoot, Kingdom selectedKingdom, bool overwriteExisting, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			EnsureCurrentKingdomProfiles(runtimeFounded: false);
			string effectiveImportRoot = importRoot;
			if (selectedKingdom != null && !TryResolveSingleImportSource(importRoot, selectedKingdom, out effectiveImportRoot, out string singleSourceError))
			{
				detailMessage = singleSourceError;
				return false;
			}
			if (!TryLoadImportEntries(effectiveImportRoot, out List<KingdomStrategicProfileRecord> entries, out int invalidCount, out string loadError, allowMissing: false))
			{
				detailMessage = loadError;
				return false;
			}
			KingdomImportTargetIndex targets = BuildImportTargetIndex();
			int imported = 0;
			int duplicatesSkipped = 0;
			int unmatched = invalidCount;
			string selectedId = NormalizeId(selectedKingdom?.StringId);
			HashSet<string> resolvedTargetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KingdomStrategicProfileRecord entry in entries)
			{
				if (!TryResolveImportTarget(entry, targets, out Kingdom kingdom, out _))
				{
					unmatched++;
					continue;
				}
				if (!string.IsNullOrEmpty(selectedId) && !string.Equals(selectedId, NormalizeId(kingdom.StringId), StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string resolvedId = NormalizeId(kingdom.StringId);
				if (!resolvedTargetIds.Add(resolvedId))
				{
					unmatched++;
					continue;
				}
				KingdomStrategicProfileRecord current = EnsureProfile(kingdom, runtimeFounded: false);
				if (current == null)
				{
					unmatched++;
					continue;
				}
				if (HasImportCollision(current) && !overwriteExisting)
				{
					duplicatesSkipped++;
					continue;
				}
				AdoptImportedDefaultWhenApplicable(current, entry);
				ApplyImportedOverrides(current, entry);
				current.UpdatedDay = GetCurrentCampaignDay();
				imported++;
			}
			if (selectedKingdom != null && imported == 0 && duplicatesSkipped == 0)
			{
				detailMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ImportNoSafeMatch", "The import contains no safely matching profile for “{KINGDOM}”. Dynamic kingdom IDs match only when the names agree.", ("KINGDOM", GetKingdomName(selectedKingdom)));
				return false;
			}
			detailMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ImportComplete", "Imported {IMPORTED}; skipped duplicates {DUPLICATES}; invalid or unmatched {UNMATCHED}.", ("IMPORTED", imported.ToString(CultureInfo.InvariantCulture)), ("DUPLICATES", duplicatesSkipped.ToString(CultureInfo.InvariantCulture)), ("UNMATCHED", unmatched.ToString(CultureInfo.InvariantCulture)));
			return true;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			Log("import failed: " + ex);
			return false;
		}
	}

	private bool TryResolveSingleImportSource(string sourcePath, Kingdom targetKingdom, out string resolvedSource, out string errorMessage)
	{
		resolvedSource = "";
		errorMessage = "";
		if (targetKingdom == null || string.IsNullOrWhiteSpace(sourcePath))
		{
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SingleImportTargetOrPathEmpty", "The single-kingdom import target or path is empty.");
			return false;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(sourcePath);
		}
		catch (Exception ex)
		{
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_InvalidImportPathPrefix", "The import path is invalid: ") + ex.Message;
			return false;
		}
		if (File.Exists(fullPath))
		{
			resolvedSource = fullPath;
			return true;
		}
		if (!Directory.Exists(fullPath))
		{
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportDirectoryNotFound", "The import directory does not exist.");
			return false;
		}
		KingdomStrategicProfileRecord targetProfile = EnsureProfile(targetKingdom, runtimeFounded: false);
		string stableFileName = BuildSingleExportFileName(targetProfile);
		List<string> singleDirectories = new List<string>
		{
			Path.Combine(fullPath, ExportDirectoryName, "kingdoms"),
			Path.Combine(fullPath, "kingdoms")
		};
		if (string.Equals(new DirectoryInfo(fullPath).Name, "kingdoms", StringComparison.OrdinalIgnoreCase))
		{
			singleDirectories.Insert(0, fullPath);
		}
		foreach (string directory in singleDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			string exactFile = Path.Combine(directory, stableFileName);
			if (File.Exists(exactFile))
			{
				resolvedSource = exactFile;
				return true;
			}
		}
		string aggregate = Path.Combine(fullPath, ExportDirectoryName, FullExportFileName);
		if (!File.Exists(aggregate))
		{
			aggregate = Path.Combine(fullPath, FullExportFileName);
		}
		if (File.Exists(aggregate))
		{
			resolvedSource = aggregate;
			return true;
		}
		KingdomImportTargetIndex targets = BuildImportTargetIndex();
		string targetId = NormalizeId(targetKingdom.StringId);
		List<string> matchingFiles = new List<string>();
		foreach (string directory in singleDirectories.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			foreach (string file in Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
			{
				if (!TryReadImportFile(file, out List<KingdomStrategicProfileRecord> candidates, out _, out _))
				{
					continue;
				}
				if (candidates.Any(entry => TryResolveImportTarget(entry, targets, out Kingdom resolvedKingdom, out _)
					&& string.Equals(NormalizeId(resolvedKingdom?.StringId), targetId, StringComparison.OrdinalIgnoreCase)))
				{
					matchingFiles.Add(file);
				}
			}
		}
		matchingFiles = matchingFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (matchingFiles.Count == 1)
		{
			resolvedSource = matchingFiles[0];
			return true;
		}
		if (matchingFiles.Count > 1)
		{
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_MultipleSingleImports", "Multiple single-kingdom files match this kingdom. Delete old copies or select the target JSON file directly.");
			return false;
		}
		errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_SingleImportNoSafeMatch", "The import directory contains no safely matching profile for “{KINGDOM}”.", ("KINGDOM", GetKingdomName(targetKingdom)));
		return false;
	}

	private void AdoptImportedDefaultWhenApplicable(KingdomStrategicProfileRecord current, KingdomStrategicProfileRecord imported)
	{
		if (current == null || imported == null)
		{
			return;
		}
		string importedSource = CleanSingleLine(imported.DefaultSource, 80);
		bool isFoundingDefault = string.Equals(importedSource, "llm_founding", StringComparison.OrdinalIgnoreCase);
		bool isBundleDefault = string.Equals(importedSource, "bundle_default", StringComparison.OrdinalIgnoreCase);
		if (!isFoundingDefault && !isBundleDefault)
		{
			return;
		}
		if (isFoundingDefault)
		{
			bool acceptsFoundingBaseline = IsDynamicKingdomId(current.KingdomId)
				|| current.RequiresFoundingGeneration
				|| string.Equals(current.DefaultSource, "founding_fallback", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(current.DefaultSource, "llm_founding", StringComparison.OrdinalIgnoreCase);
			if (!acceptsFoundingBaseline)
			{
				return;
			}
		}
		string personality = CleanProfileText(imported.DefaultNationalPersonality);
		string strategy = CleanProfileText(imported.DefaultLongTermStrategy);
		if (isBundleDefault)
		{
			bool legacyWholeCardOverride = imported.IsPlayerOverride && !imported.HasPersonalityOverride && !imported.HasStrategyOverride;
			if (string.IsNullOrWhiteSpace(personality) && !imported.HasPersonalityOverride && !legacyWholeCardOverride)
			{
				personality = CleanProfileText(imported.NationalPersonality);
			}
			if (string.IsNullOrWhiteSpace(strategy) && !imported.HasStrategyOverride && !legacyWholeCardOverride)
			{
				strategy = CleanProfileText(imported.LongTermStrategy);
			}
			// Keep the inferred bundle baseline aligned so the following override pass
			// does not misclassify clean package text as a player edit.
			imported.DefaultNationalPersonality = personality;
			imported.DefaultLongTermStrategy = strategy;
		}
		if (string.IsNullOrWhiteSpace(personality) && string.IsNullOrWhiteSpace(strategy))
		{
			return;
		}
		current.DefaultNationalPersonality = personality;
		current.DefaultLongTermStrategy = strategy;
		current.DefaultSource = isBundleDefault ? "bundle_default" : "llm_founding";
		current.RequiresFoundingGeneration = false;
		current.GenerationState = isBundleDefault ? "not_required" : "complete";
		current.GenerationAttemptCount = 0;
		current.NextGenerationRetryDay = 0;
		current.LastGenerationError = "";
	}

	private static bool HasImportCollision(KingdomStrategicProfileRecord profile)
	{
		return profile != null && (profile.IsPlayerOverride
			|| profile.HasPersonalityOverride
			|| profile.HasStrategyOverride
			|| string.Equals(profile.DefaultSource, "bundle_default", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(profile.DefaultSource, "llm_founding", StringComparison.OrdinalIgnoreCase));
	}

	private static void ApplyImportedOverrides(KingdomStrategicProfileRecord current, KingdomStrategicProfileRecord imported)
	{
		if (current == null || imported == null)
		{
			return;
		}
		bool legacyOverride = imported.IsPlayerOverride && !imported.HasPersonalityOverride && !imported.HasStrategyOverride;
		string importedPersonality = CleanProfileText(imported.NationalPersonality);
		string importedStrategy = CleanProfileText(imported.LongTermStrategy);
		string importedDefaultPersonality = CleanProfileText(imported.DefaultNationalPersonality);
		string importedDefaultStrategy = CleanProfileText(imported.DefaultLongTermStrategy);
		bool personalityOverride = imported.HasPersonalityOverride || legacyOverride
			|| (!string.IsNullOrWhiteSpace(importedPersonality)
				&& !string.Equals(importedPersonality, importedDefaultPersonality, StringComparison.Ordinal));
		bool strategyOverride = imported.HasStrategyOverride || legacyOverride
			|| (!string.IsNullOrWhiteSpace(importedStrategy)
				&& !string.Equals(importedStrategy, importedDefaultStrategy, StringComparison.Ordinal));
		current.HasPersonalityOverride = personalityOverride;
		current.HasStrategyOverride = strategyOverride;
		current.IsPlayerOverride = personalityOverride || strategyOverride;
		current.NationalPersonality = personalityOverride ? importedPersonality : (current.DefaultNationalPersonality ?? "");
		current.LongTermStrategy = strategyOverride ? importedStrategy : (current.DefaultLongTermStrategy ?? "");
	}

	private bool TryLoadImportEntries(string sourcePath, out List<KingdomStrategicProfileRecord> entries, out int invalidCount, out string errorMessage, bool allowMissing)
	{
		entries = new List<KingdomStrategicProfileRecord>();
		invalidCount = 0;
		errorMessage = "";
		if (string.IsNullOrWhiteSpace(sourcePath))
		{
			errorMessage = allowMissing ? "" : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportPathEmpty", "The import path is empty.");
			return allowMissing;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(sourcePath);
		}
		catch (Exception ex)
		{
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_InvalidImportPathPrefix", "The import path is invalid: ") + ex.Message;
			return false;
		}
		List<string> files = new List<string>();
		if (File.Exists(fullPath))
		{
			if (!string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_OnlyJson", "Only JSON files can be imported.");
				return false;
			}
			files.Add(fullPath);
		}
		else if (Directory.Exists(fullPath))
		{
			string aggregate = Path.Combine(fullPath, ExportDirectoryName, FullExportFileName);
			if (!File.Exists(aggregate))
			{
				aggregate = Path.Combine(fullPath, FullExportFileName);
			}
			string singles = Path.Combine(fullPath, ExportDirectoryName, "kingdoms");
			if (!Directory.Exists(singles))
			{
				singles = Path.Combine(fullPath, "kingdoms");
			}
			if (!Directory.Exists(singles) && string.Equals(new DirectoryInfo(fullPath).Name, "kingdoms", StringComparison.OrdinalIgnoreCase))
			{
				singles = fullPath;
			}
			if (File.Exists(aggregate))
			{
				files.Add(aggregate);
			}
			else if (Directory.Exists(singles))
			{
				files.AddRange(Directory.GetFiles(singles, "*.json", SearchOption.TopDirectoryOnly).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
			}
		}
		if (files.Count == 0)
		{
			errorMessage = allowMissing ? "" : DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_NoImportFiles", "Could not find {FULL} or single-kingdom JSON files.", ("FULL", ExportDirectoryName + "/" + FullExportFileName));
			return allowMissing;
		}
		if (files.Count > MaxImportProfileCount)
		{
			errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ImportFileLimit", "The import contains more than the safe limit of {COUNT} files.", ("COUNT", MaxImportProfileCount.ToString(CultureInfo.InvariantCulture)));
			return false;
		}
		HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string file in files)
		{
			if (!TryReadImportFile(file, out List<KingdomStrategicProfileRecord> fileEntries, out int fileInvalid, out string fileError))
			{
				errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ImportFileError", "{FILE}: {ERROR}", ("FILE", Path.GetFileName(file)), ("ERROR", fileError));
				return false;
			}
			invalidCount += fileInvalid;
			foreach (KingdomStrategicProfileRecord entry in fileEntries)
			{
				if (entries.Count >= MaxImportProfileCount)
				{
					errorMessage = DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_ProfileLimit", "The package contains more than the safe limit of {COUNT} kingdom profiles.", ("COUNT", MaxImportProfileCount.ToString(CultureInfo.InvariantCulture)));
					return false;
				}
				string id = NormalizeId(entry?.KingdomId);
				if (string.IsNullOrEmpty(id) || !seenIds.Add(id))
				{
					invalidCount++;
					continue;
				}
				entry.KingdomId = id;
				entries.Add(entry);
			}
		}
		return true;
	}

	private bool TryReadImportFile(string filePath, out List<KingdomStrategicProfileRecord> entries, out int invalidCount, out string errorMessage)
	{
		entries = new List<KingdomStrategicProfileRecord>();
		invalidCount = 0;
		errorMessage = "";
		try
		{
			FileInfo info = new FileInfo(filePath);
			if (!info.Exists || info.Length <= 0L)
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_FileEmpty", "The file does not exist or is empty.");
				return false;
			}
			if (info.Length > MaxImportFileBytes)
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_FileTooLarge", "The file exceeds the 32 MB safety limit.");
				return false;
			}
			string json = File.ReadAllText(filePath, StrictUtf8);
			JToken token = JToken.Parse(json);
			if (token is not JObject root)
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RootObjectRequired", "The root element must be a JSON object.");
				return false;
			}
			JToken profilesToken = GetPropertyIgnoreCase(root, "Profiles");
			if (profilesToken is JObject profilesObject)
			{
				HashSet<string> localIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (JProperty property in profilesObject.Properties())
				{
					string id = NormalizeId(property.Name);
					KingdomStrategicProfileRecord entry = property.Value?.ToObject<KingdomStrategicProfileRecord>();
					if (entry == null || string.IsNullOrEmpty(id) || !localIds.Add(id))
					{
						invalidCount++;
						continue;
					}
					entry.KingdomId = NormalizeId(entry.KingdomId);
					if (string.IsNullOrEmpty(entry.KingdomId))
					{
						entry.KingdomId = id;
					}
					if (TryNormalizeImportEntry(entry))
					{
						entries.Add(entry);
					}
					else
					{
						invalidCount++;
					}
				}
				return true;
			}
			if (GetPropertyIgnoreCase(root, "KingdomId") != null)
			{
				KingdomStrategicProfileRecord entry = root.ToObject<KingdomStrategicProfileRecord>();
				if (TryNormalizeImportEntry(entry))
				{
					entries.Add(entry);
					return true;
				}
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SingleCardInvalid", "The single-kingdom profile has no valid ID, profile text, default text, or explicit override marker.");
				return false;
			}
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFormatRequired", "The JSON structure is not a kingdom profile export.");
			return false;
		}
		catch (DecoderFallbackException)
		{
			errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_InvalidUtf8", "The file is not valid UTF-8.");
			return false;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static bool TryNormalizeImportEntry(KingdomStrategicProfileRecord entry)
	{
		if (entry == null)
		{
			return false;
		}
		entry.KingdomId = NormalizeId(entry.KingdomId);
		entry.KingdomName = CleanSingleLine(entry.KingdomName, 240);
		entry.CultureId = CleanSingleLine(entry.CultureId, 160);
		entry.NationalPersonality = CleanProfileText(entry.NationalPersonality);
		entry.LongTermStrategy = CleanProfileText(entry.LongTermStrategy);
		entry.DefaultNationalPersonality = CleanProfileText(entry.DefaultNationalPersonality);
		entry.DefaultLongTermStrategy = CleanProfileText(entry.DefaultLongTermStrategy);
		entry.DefaultSource = CleanSingleLine(entry.DefaultSource, 80);
		bool hasText = !string.IsNullOrWhiteSpace(entry.NationalPersonality)
			|| !string.IsNullOrWhiteSpace(entry.LongTermStrategy)
			|| !string.IsNullOrWhiteSpace(entry.DefaultNationalPersonality)
			|| !string.IsNullOrWhiteSpace(entry.DefaultLongTermStrategy);
		bool hasExplicitOverride = entry.HasPersonalityOverride || entry.HasStrategyOverride || entry.IsPlayerOverride;
		return !string.IsNullOrEmpty(entry.KingdomId) && (hasText || hasExplicitOverride);
	}

	private KingdomImportTargetIndex BuildImportTargetIndex()
	{
		KingdomImportTargetIndex index = new KingdomImportTargetIndex();
		foreach (Kingdom kingdom in GetCurrentKingdoms())
		{
			string id = NormalizeId(kingdom.StringId);
			if (!string.IsNullOrEmpty(id) && !index.ById.ContainsKey(id))
			{
				index.ById[id] = kingdom;
			}
			string name = NormalizeName(GetKingdomName(kingdom));
			if (!string.IsNullOrEmpty(name))
			{
				if (!index.ByName.TryGetValue(name, out List<Kingdom> list))
				{
					list = new List<Kingdom>();
					index.ByName[name] = list;
				}
				list.Add(kingdom);
			}
		}
		return index;
	}

	private static bool TryResolveImportTarget(KingdomStrategicProfileRecord entry, KingdomImportTargetIndex index, out Kingdom kingdom, out string warning)
	{
		kingdom = null;
		warning = "";
		if (entry == null || index == null)
		{
			warning = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EmptyCard", "The kingdom profile is empty.");
			return false;
		}
		string id = NormalizeId(entry.KingdomId);
		string importedName = NormalizeName(entry.KingdomName);
		if (!string.IsNullOrEmpty(id) && index.ById.TryGetValue(id, out Kingdom exact))
		{
			if (!IsDynamicKingdomId(id)
				|| (!string.IsNullOrEmpty(importedName) && string.Equals(importedName, NormalizeName(GetKingdomName(exact)), StringComparison.OrdinalIgnoreCase)))
			{
				kingdom = exact;
				return true;
			}
			warning = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DynamicNameMismatch", "The dynamic kingdom ID matches, but the names differ.");
		}
		if (!string.IsNullOrEmpty(importedName) && index.ByName.TryGetValue(importedName, out List<Kingdom> matches) && matches.Count == 1)
		{
			kingdom = matches[0];
			return true;
		}
		warning = string.IsNullOrEmpty(warning) ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NoUniqueMatch", "The current world has no unique matching kingdom.") : warning;
		return false;
	}

	private static KingdomStrategicProfileExportPackage BuildExportPackage(IEnumerable<KingdomStrategicProfileRecord> profiles)
	{
		KingdomStrategicProfileExportPackage package = new KingdomStrategicProfileExportPackage
		{
			Version = SchemaVersion,
			ExportedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
			Profiles = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase)
		};
		foreach (KingdomStrategicProfileRecord profile in (profiles ?? Enumerable.Empty<KingdomStrategicProfileRecord>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
			.OrderBy(x => x.KingdomId, StringComparer.OrdinalIgnoreCase))
		{
			package.Profiles[NormalizeId(profile.KingdomId)] = CloneForExport(profile);
		}
		return package;
	}

	private static KingdomStrategicProfileRecord CloneForExport(KingdomStrategicProfileRecord source)
	{
		if (source == null)
		{
			return null;
		}
		return new KingdomStrategicProfileRecord
		{
			KingdomId = source.KingdomId ?? "",
			KingdomName = source.KingdomName ?? "",
			CultureId = source.CultureId ?? "",
			RulerHeroId = source.RulerHeroId ?? "",
			NationalPersonality = source.NationalPersonality ?? "",
			LongTermStrategy = source.LongTermStrategy ?? "",
			DefaultNationalPersonality = source.DefaultNationalPersonality ?? "",
			DefaultLongTermStrategy = source.DefaultLongTermStrategy ?? "",
			DefaultSource = source.DefaultSource ?? "",
			HasPersonalityOverride = source.HasPersonalityOverride,
			HasStrategyOverride = source.HasStrategyOverride,
			IsPlayerOverride = source.IsPlayerOverride,
			RequiresFoundingGeneration = source.RequiresFoundingGeneration,
			GenerationState = source.GenerationState ?? "",
			GenerationAttemptCount = source.GenerationAttemptCount,
			NextGenerationRetryDay = source.NextGenerationRetryDay,
			LastGenerationError = source.LastGenerationError ?? "",
			CreatedDay = source.CreatedDay,
			UpdatedDay = source.UpdatedDay
		};
	}

	// Deep-copy profile storage before an explicit database reload; it is used as a candidate, never as a hot-path cache.
	private static KingdomStrategicProfileStorage CloneStorage(KingdomStrategicProfileStorage source)
	{
		KingdomStrategicProfileStorage kingdomStrategicProfileStorage = new KingdomStrategicProfileStorage
		{
			Version = source?.Version ?? SchemaVersion,
			Profiles = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase)
		};
		foreach (KeyValuePair<string, KingdomStrategicProfileRecord> item in source?.Profiles ?? new Dictionary<string, KingdomStrategicProfileRecord>())
		{
			string text = NormalizeId(item.Value?.KingdomId ?? item.Key);
			if (!string.IsNullOrWhiteSpace(text) && item.Value != null)
			{
				kingdomStrategicProfileStorage.Profiles[text] = CloneForExport(item.Value);
			}
		}
		return kingdomStrategicProfileStorage;
	}

	// Build the complete reload candidate without touching _storage; this cold-path copy is the boundary that keeps cancel/failure non-destructive.
	private KingdomStrategicProfileStorage BuildDatabaseReloadCandidateStorage(ISet<string> preservedProfileIds)
	{
		KingdomStrategicProfileStorage candidate = CloneStorage(_storage);
		foreach (Kingdom kingdom in GetCurrentKingdoms())
		{
			string kingdomId = NormalizeId(kingdom?.StringId);
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				continue;
			}
			// A skipped dynamic ID is not permission to normalize or reset that save's unrelated kingdom.
			if (preservedProfileIds.Contains(kingdomId)) continue;
			if (candidate.Profiles.TryGetValue(kingdomId, out KingdomStrategicProfileRecord profile) && profile != null)
			{
				// Normalize the cloned record so reset defaults follow the same rules as ordinary campaign initialization.
				NormalizeProfile(profile, kingdom, recoverInterruptedGeneration: false);
			}
			else
			{
				candidate.Profiles[kingdomId] = BuildInitialProfileForDatabaseReloadCandidate(kingdom);
			}
		}
		return candidate;
	}

	// This mirrors EnsureProfile's baseline selection, but reload deliberately keeps dynamic kingdoms on their local fallback instead of queuing an unexpected API generation.
	private KingdomStrategicProfileRecord BuildInitialProfileForDatabaseReloadCandidate(Kingdom kingdom)
	{
		string kingdomId = NormalizeId(kingdom?.StringId);
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return null;
		}
		KingdomStrategicProfileTemplate template;
		bool requiresGeneration = IsDynamicKingdomId(kingdomId) && !AuthoredDefaults.TryGetValue(kingdomId, out template);
		if (requiresGeneration)
		{
			template = BuildFoundingFallback(kingdom);
		}
		else if (!AuthoredDefaults.TryGetValue(kingdomId, out template))
		{
			template = BuildGenericDefault(kingdom);
		}
		int day = GetCurrentCampaignDay();
		return new KingdomStrategicProfileRecord
		{
			KingdomId = kingdomId,
			KingdomName = GetKingdomName(kingdom),
			CultureId = kingdom?.Culture?.StringId ?? "",
			RulerHeroId = GetRuler(kingdom)?.StringId ?? "",
			DefaultNationalPersonality = CleanProfileText(template.NationalPersonality),
			DefaultLongTermStrategy = CleanProfileText(template.LongTermStrategy),
			NationalPersonality = CleanProfileText(template.NationalPersonality),
			LongTermStrategy = CleanProfileText(template.LongTermStrategy),
			DefaultSource = requiresGeneration ? "database_reload_default" : (AuthoredDefaults.ContainsKey(kingdomId) ? "authored_default" : "generic_default"),
			IsPlayerOverride = false,
			RequiresFoundingGeneration = false,
			GenerationState = "not_required",
			GenerationAttemptCount = 0,
			NextGenerationRetryDay = 0,
			CreatedDay = day,
			UpdatedDay = day
		};
	}

	// Reset only the effective override state; unmatched dynamic kingdoms keep their local baseline and must not cause an implicit LLM request after reload.
	private static void ResetProfileToDefaultForDatabaseReload(KingdomStrategicProfileRecord profile, int day)
	{
		if (profile == null)
		{
			return;
		}
		profile.NationalPersonality = profile.DefaultNationalPersonality ?? "";
		profile.LongTermStrategy = profile.DefaultLongTermStrategy ?? "";
		profile.HasPersonalityOverride = false;
		profile.HasStrategyOverride = false;
		profile.IsPlayerOverride = false;
		profile.DefaultSource = "database_reload_default";
		profile.RequiresFoundingGeneration = false;
		profile.GenerationState = "not_required";
		profile.GenerationAttemptCount = 0;
		profile.NextGenerationRetryDay = 0;
		profile.LastGenerationError = "";
		profile.UpdatedDay = day;
	}

	// A database package establishes a new baseline, rather than carrying another save's player-override flags forward.
	private static void ApplyDatabaseSourceProfile(KingdomStrategicProfileRecord current, KingdomStrategicProfileRecord source, int day)
	{
		if (current == null || source == null)
		{
			return;
		}
		string text = CleanProfileText(source.NationalPersonality);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = CleanProfileText(source.DefaultNationalPersonality);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = current.DefaultNationalPersonality ?? "";
		}
		string text2 = CleanProfileText(source.LongTermStrategy);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = CleanProfileText(source.DefaultLongTermStrategy);
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = current.DefaultLongTermStrategy ?? "";
		}
		current.DefaultNationalPersonality = text;
		current.DefaultLongTermStrategy = text2;
		current.NationalPersonality = text;
		current.LongTermStrategy = text2;
		current.DefaultSource = "database_reload";
		current.HasPersonalityOverride = false;
		current.HasStrategyOverride = false;
		current.IsPlayerOverride = false;
		current.RequiresFoundingGeneration = false;
		current.GenerationState = "not_required";
		current.GenerationAttemptCount = 0;
		current.NextGenerationRetryDay = 0;
		current.LastGenerationError = "";
		current.UpdatedDay = day;
	}

	private static void WriteJsonAtomic(string filePath, object value)
	{
		string directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
		if (string.IsNullOrWhiteSpace(directory))
		{
					throw new InvalidOperationException(DiplomacyMiscPromptTexts.Get("ProfileDevUi_ExportDirectoryInvalid", "Export directory is invalid."));
		}
		Directory.CreateDirectory(directory);
		string tempPath = Path.Combine(directory, "." + Path.GetFileName(filePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			string json = JsonConvert.SerializeObject(value, Formatting.Indented);
			File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			if (File.Exists(filePath))
			{
				try
				{
					File.Replace(tempPath, filePath, null, ignoreMetadataErrors: true);
					return;
				}
				catch
				{
					File.Copy(tempPath, filePath, overwrite: true);
					File.Delete(tempPath);
					return;
				}
			}
			File.Move(tempPath, filePath);
		}
		finally
		{
			try
			{
				if (File.Exists(tempPath))
				{
					File.Delete(tempPath);
				}
			}
			catch
			{
			}
		}
	}

	private static string BuildSingleExportFileName(KingdomStrategicProfileRecord profile)
	{
		string raw = profile?.KingdomId ?? "kingdom";
		foreach (char invalid in Path.GetInvalidFileNameChars())
		{
			raw = raw.Replace(invalid, '_');
		}
		return raw.Trim().Trim('.') + ".json";
	}

	private static string BuildFoundingProfilePrompt(Kingdom kingdom)
	{
		Hero ruler = GetRuler(kingdom);
		string settlements = DiplomacyPromptTexts.Get("FoundingProfileNoFiefs", "No fiefs");
		try
		{
			List<string> names = (kingdom?.Settlements ?? Enumerable.Empty<Settlement>())
				.Where(x => x != null && (x.IsTown || x.IsCastle))
				.Select(x => x.Name?.ToString() ?? "")
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Take(8)
				.ToList();
			if (names.Count > 0)
			{
				settlements = string.Join(DiplomacyPromptTexts.Get("FoundingProfileListSeparator", ", "), names);
			}
		}
		catch
		{
		}
		string enemies = DiplomacyPromptTexts.Get("FoundingProfileNoEnemies", "No clearly identified enemy kingdoms");
		try
		{
			List<string> names = GetCurrentKingdoms()
				.Where(x => x != kingdom && !x.IsEliminated && FactionManager.IsAtWarAgainstFaction(kingdom, x))
				.Select(GetKingdomName)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Take(8)
				.ToList();
			if (names.Count > 0)
			{
				enemies = string.Join(DiplomacyPromptTexts.Get("FoundingProfileListSeparator", ", "), names);
			}
		}
		catch
		{
		}
		StringBuilder prompt = new StringBuilder(700);
		prompt.AppendLine(DiplomacyPromptTexts.Get("FoundingProfileTask", "Generate a fixed character profile for the newly founded kingdom. Output only one line of JSON, with no explanation:"));
		prompt.AppendLine("{\"national_personality\":\"...\",\"long_term_strategy\":\"...\"}");
		prompt.AppendLine(DiplomacyPromptTexts.Get("FoundingProfileLengthRule", "Each field must contain 50-160 characters. Personality describes decision style, values and boundaries; strategy describes long-term actionable national goals. Do not include numbers or system terminology."));
		prompt.AppendLine(DiplomacyPromptTexts.Get("FoundingProfileKingdom", "Kingdom: ") + GetKingdomName(kingdom) + "（" + (kingdom?.StringId ?? "") + "）");
		prompt.AppendLine(DiplomacyPromptTexts.Get("FoundingProfileCulture", "Culture: ") + (kingdom?.Culture?.Name?.ToString() ?? kingdom?.Culture?.StringId ?? DiplomacyPromptTexts.Get("FoundingProfileUnknown", "Unknown")));
		prompt.AppendLine(DiplomacyPromptTexts.Get("FoundingProfileRuler", "Ruler: ") + (ruler?.Name?.ToString() ?? DiplomacyPromptTexts.Get("FoundingProfileUnknown", "Unknown")));
		prompt.AppendLine(DiplomacyPromptTexts.Get("FoundingProfileFiefs", "Fiefs: ") + settlements);
		prompt.Append(DiplomacyPromptTexts.Get("FoundingProfileEnemies", "Current enemy kingdoms: ") + enemies);
		return prompt.ToString();
	}

	private static bool TryParseFoundingProfile(string content, out string personality, out string strategy, out string errorMessage)
	{
		personality = "";
		strategy = "";
		errorMessage = "";
		try
		{
			string text = (content ?? "").Trim();
			int start = text.IndexOf('{');
			int end = text.LastIndexOf('}');
			if (start < 0 || end <= start)
			{
							errorMessage = DiplomacyMiscPromptTexts.Get("WorldDiplomacy_MissingJson", "No JSON object in LLM return.");
				return false;
			}
			JObject root = JObject.Parse(text.Substring(start, end - start + 1));
			personality = CleanProfileText(GetStringPropertyIgnoreCase(root, "national_personality", "NationalPersonality", "personality"));
			strategy = CleanProfileText(GetStringPropertyIgnoreCase(root, "long_term_strategy", "LongTermStrategy", "strategy"));
			if (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(strategy))
			{
						errorMessage = DiplomacyMiscPromptTexts.Get("WorldDiplomacy_MissingFields", "LLM JSON is missing national_personality or long_term_strategy.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
					errorMessage = DiplomacyMiscPromptTexts.Get("WorldDiplomacy_ParseFailedPrefix", "LLM JSON parsing failed: ") + ex.Message;
			return false;
		}
	}

	private static string GetStringPropertyIgnoreCase(JObject obj, params string[] names)
	{
		if (obj == null || names == null)
		{
			return "";
		}
		foreach (string name in names)
		{
			JToken value = GetPropertyIgnoreCase(obj, name);
			if (value != null && value.Type != JTokenType.Null)
			{
				return value.ToString();
			}
		}
		return "";
	}

	private static JToken GetPropertyIgnoreCase(JObject obj, string name)
	{
		return obj?.Properties().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
	}

	private static Dictionary<string, KingdomStrategicProfileTemplate> BuildAuthoredDefaults()
	{
		return new Dictionary<string, KingdomStrategicProfileTemplate>(StringComparer.OrdinalIgnoreCase)
		{
			["empire"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_North_Personality", "Highest principles are imperial law, senatorial tradition, and strict order; considers itself responsible for preserving the old imperial institutions, skilled in law and alliances, wary of warlords, and reluctant to recognize breakaway regimes."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_North_Strategy", "Reunify the empire under the Northern Empire's legitimacy: secure core provinces, steadily weaken and annex the Southern and Western Empires, retake key strongholds, and restore an empire under a single central authority.")),
			["empire_s"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_South_Personality", "Values royal legitimacy, courtly order, and governing continuity; favors diplomacy, marriage alliances, and institutional assimilation, resolute and vengeful when authority is challenged, believing reunification must center on the rightful throne."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_South_Strategy", "Reunify the empire through the Southern Empire's dynastic succession: consolidate southern wealthy provinces and allies, win over old nobility and cities, steadily defeat Northern and Western Empires, and establish a widely acknowledged monarchy.")),
			["empire_w"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_West_Personality", "Values martial prowess, discipline, strength, and meritocracy; decisions are direct and unyielding, quick to reward capable commanders and allies, swift to punish weakness and betrayal, believing the empire can only be reforged by victors."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_West_Strategy", "Reunify the empire through the Western Empire's military might: integrate western legions and strategic fortresses, exert relentless pressure on North and South, seize key cities and corridors, and build a new empire backed by a strong central martial elite.")),
			["vlandia"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Vlandia_Personality", "Pragmatic, confident, and martial, respecting feudal contracts, fief rewards, and noble honor; adept at trading land for loyalty, keenly interested in rich territory and trade routes, pressing neighboring realms when weakness shows."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Vlandia_Strategy", "Consolidate the western seaboard and vassal network, seize inland-connecting castles and trade routes, prioritize weakening Battanian and imperial western frontiers, and let Vlandian lords and knights dominate the continent's west.")),
			["battania"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Battania_Personality", "Treasures clan freedom, ancient forests, and native traditions; intensely sensitive to foreign encroachment, skilled in ambushes and alliances but averse to central authority, harboring generational hostility toward land-grabbers."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Battania_Strategy", "Hold and reclaim the Battanian highlands and ancestral woodlands, expel encroaching Vlandian and imperial forces from core mountains, ally with available neighbors to break encirclement, and restore a free highland realm.")),
			["sturgia"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Sturgia_Personality", "Hardy, oath-bound, valuing clan and warrior honor, enduring long hardships; respects strength and reciprocity, reacts forcefully to humiliation or border incursions, while guarding against domestic factionalism and southern expansion."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Sturgia_Strategy", "Unite and secure the northern reaches, guard frostbound towns and east-west transit, reclaim lost ancestral borders, and once stable, push southward toward ports, farmland, and buffer zones to guarantee lasting strength.")),
			["aserai"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Aserai_Personality", "Values family prestige, reciprocity, commercial prosperity, and desert order; patient negotiators balancing rival interests, fiercely retaliating against trade, oasis, or tribal indignities, usually avoiding profitless wars of attrition."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Aserai_Strategy", "Maintain unity across Nahasa tribes, control oases, ports, and caravan routes, protect southern depths from foreign incursions, and once trade and finance are secure, seize adjacent wealthy hubs to make the Aserai an unrivaled power.")),
			["khuzait"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Khuzait_Personality", "Mobile, assertive, respecting triumph and obedience, swift to pivot or ally as fortunes shift; treats tribute and submission as proof of order, probing vulnerable borders and relentlessly pressing defiant adversaries."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Khuzait_Strategy", "Unify eastern steppes and horse archers, push westward to secure pastures, towns, and transit arteries, forcing realms to submit or pay tribute, breaking the eastern empire and rivals to establish steppe-and-heartland hegemony.")),
			["nord"] = new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Nord_Personality", "Martial, daring, prizing plunder, renown, and chieftain prestige; masters of sea expeditions and surprise assaults, respecting brave foes while scorning weakness, relying on repeated victories and spoils to preserve cohesion."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Nord_Strategy", "Continuously assault continental kingdoms: seize coastal ports and raiding footholds, progressively plunder, weaken, and subjugate inland realms, steadily expanding Nord settlements into the interior.")),
		};
	}

	private static KingdomStrategicProfileTemplate BuildGenericDefault(Kingdom kingdom)
	{
		string culture = NormalizeId(kingdom?.Culture?.StringId);
		string name = GetKingdomName(kingdom);
		if (culture.IndexOf("empire", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_GenericEmpire_Personality", "Emphasizes institutions, law, legitimacy, and civic administration, wielding bureaucracy, diplomacy, and legions to uphold central authority, vigilant against rebellion and acting decisively when challenged."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_GenericEmpire_Strategy", "Consolidate existing provinces and imperial legitimacy, recapture strategic territories, undermine rival claimants, and integrate neighboring factions into a stable, unified hegemony."));
		}
		if (culture.IndexOf("nord", StringComparison.OrdinalIgnoreCase) >= 0 || culture.IndexOf("sturg", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return new KingdomStrategicProfileTemplate(
            DiplomacyMiscPromptTexts.Get("StrategicProfile_GenericMartial_Personality", "Resilient and martial, valuing oaths, martial renown, and shared spoils; endures hardship but will not abide border encroachment, public insults, or feeble leadership."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_GenericMartial_Strategy", "Unify and hold core ancestral lands and transit hubs, then contest fertile borders, harbors, and buffer regions, expanding influence through sustainable triumphs to prevent domination by stronger realms."));
		}
		return new KingdomStrategicProfileTemplate(
            (name + DiplomacyMiscPromptTexts.Get("StrategicProfile_GenericIndependent_Personality", " values independence, survival, legitimacy, and pragmatic interests; allies and negotiates cautiously based on strength, but maintains firm boundaries when territory, succession, or dignity is threatened.")),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_GenericIndependent_Strategy", "Consolidate core holdings and internal cohesion, secure revenue, manpower, and critical arteries, neutralizing immediate perils and securing regional allies to ensure enduring survival and prestige."));
	}

	private static KingdomStrategicProfileTemplate BuildFoundingFallback(Kingdom kingdom)
	{
        string ruler = GetRuler(kingdom)?.Name?.ToString() ?? DiplomacyMiscPromptTexts.Get("StrategicProfile_NewRulerFallback", "New Ruler");
		return new KingdomStrategicProfileTemplate(
            string.Format(DiplomacyMiscPromptTexts.Get("StrategicProfile_Rebel_PersonalityFormat", "This is a newly established realm led by {0}, prioritizing survival, unity, founding legitimacy, and freedom from the old order; allying pragmatically when weak, reacting fiercely to restoration, annexation, or insults."), ruler),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Rebel_Strategy", "Consolidate founding legitimacy and territory, recruit adherents, resist suppression from the former sovereign or rivals, secure strategic hubs and armed forces, and grow into an enduring sovereign kingdom."));
	}

	private static KingdomStrategicProfileTemplate BuildOrphanFallback(string kingdomName)
	{
        string name = string.IsNullOrWhiteSpace(kingdomName) ? DiplomacyMiscPromptTexts.Get("StrategicProfile_DefaultKingdomFallback", "the kingdom") : kingdomName;
		return new KingdomStrategicProfileTemplate(
            name + DiplomacyMiscPromptTexts.Get("StrategicProfile_Default_Personality", " values independence, order, and dynastic survival, balancing pragmatic gain and sovereign dignity, watchful against direct threats."),
            DiplomacyMiscPromptTexts.Get("StrategicProfile_Default_Strategy", "Consolidate core domains and internal harmony, ensure long-term viability, neutralize primary perils, and extend regional influence when conditions ripen."));
	}

	private static List<Kingdom> GetCurrentKingdoms()
	{
		try
		{
			IEnumerable<Kingdom> kingdoms = Kingdom.All;
			return kingdoms == null
				? new List<Kingdom>()
				: kingdoms.Where(x => x != null && !string.IsNullOrWhiteSpace(x.StringId)).ToList();
		}
		catch
		{
			return new List<Kingdom>();
		}
	}

	private static Hero GetRuler(Kingdom kingdom)
	{
		try
		{
			return kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		}
		catch
		{
			return null;
		}
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
        return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? DiplomacyMiscPromptTexts.Get("StrategicProfile_GeneralKingdom", "kingdom");
	}

	private static int GetCurrentCampaignDay()
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

	private static string NormalizeId(string value)
	{
		return (value ?? "").Trim().ToLowerInvariant();
	}

	private static string NormalizeName(string value)
	{
		return CleanSingleLine(value, 240).ToLowerInvariant();
	}

	private static bool IsDynamicKingdomId(string kingdomId)
	{
		string id = NormalizeId(kingdomId);
		return id.StartsWith("new_kingdom", StringComparison.OrdinalIgnoreCase)
			|| id.StartsWith("rebel_kingdom", StringComparison.OrdinalIgnoreCase)
			|| id.StartsWith("rebellion_", StringComparison.OrdinalIgnoreCase);
	}

	private static string CleanProfileText(string value)
	{
		return AnimusForgeTextInputSanitizer.SanitizeMultiline(value, MaxProfileTextChars).Trim();
	}

	private static string CleanSingleLine(string value, int maxChars)
	{
		return AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, maxChars).Trim();
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log(Source, message ?? "");
		}
		catch
		{
		}
	}

	private sealed class KingdomImportTargetIndex
	{
		public readonly Dictionary<string, Kingdom> ById = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
		public readonly Dictionary<string, List<Kingdom>> ByName = new Dictionary<string, List<Kingdom>>(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class FoundingProfileRequest
	{
		public string KingdomId = "";
		public string KingdomName = "";
		public string SystemPrompt = "";
		public long RuntimeGeneration;
	}

	private sealed class FoundingProfileResult
	{
		public string KingdomId = "";
		public string KingdomName = "";
		public string NationalPersonality = "";
		public string LongTermStrategy = "";
		public string ErrorMessage = "";
		public long RuntimeGeneration;
		public bool Success;
	}
}

internal sealed class KingdomStrategicProfileStorage
{
	[JsonProperty("Version")]
	public int Version { get; set; } = 1;

	[JsonProperty("Profiles")]
	public Dictionary<string, KingdomStrategicProfileRecord> Profiles { get; set; } = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
}

// Carries already parsed, target-resolved profile data from terminal preflight to the one-shot storage swap.
internal sealed class KingdomDatabaseReloadPlan
{
	public Dictionary<string, KingdomStrategicProfileRecord> SourceProfilesByTargetId { get; set; } = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);

	public int CurrentProfilesResetToDefaultCount { get; set; }

	public HashSet<string> PreservedProfileIds { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public int SkippedStaticProfileCount { get; set; }

	public List<string> SkippedStaticProfileLabels { get; set; } = new List<string>();

	public int SkippedDynamicProfileCount { get; set; }

	public List<string> SkippedDynamicProfileLabels { get; set; } = new List<string>();
}

internal sealed class KingdomStrategicProfileExportPackage
{
	[JsonProperty("Version")]
	public int Version { get; set; } = 1;

	[JsonProperty("ExportedAtUtc")]
	public string ExportedAtUtc { get; set; } = "";

	[JsonProperty("Profiles")]
	public Dictionary<string, KingdomStrategicProfileRecord> Profiles { get; set; } = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class KingdomStrategicProfileRecord
{
	[JsonProperty("KingdomId")]
	public string KingdomId { get; set; } = "";

	[JsonProperty("KingdomName")]
	public string KingdomName { get; set; } = "";

	[JsonProperty("CultureId")]
	public string CultureId { get; set; } = "";

	[JsonProperty("RulerHeroId")]
	public string RulerHeroId { get; set; } = "";

	[JsonProperty("NationalPersonality")]
	public string NationalPersonality { get; set; } = "";

	[JsonProperty("LongTermStrategy")]
	public string LongTermStrategy { get; set; } = "";

	[JsonProperty("DefaultNationalPersonality")]
	public string DefaultNationalPersonality { get; set; } = "";

	[JsonProperty("DefaultLongTermStrategy")]
	public string DefaultLongTermStrategy { get; set; } = "";

	[JsonProperty("DefaultSource")]
	public string DefaultSource { get; set; } = "";

	[JsonProperty("HasPersonalityOverride")]
	public bool HasPersonalityOverride { get; set; }

	[JsonProperty("HasStrategyOverride")]
	public bool HasStrategyOverride { get; set; }

	[JsonProperty("IsPlayerOverride")]
	public bool IsPlayerOverride { get; set; }

	[JsonProperty("RequiresFoundingGeneration")]
	public bool RequiresFoundingGeneration { get; set; }

	[JsonProperty("GenerationState")]
	public string GenerationState { get; set; } = "";

	[JsonProperty("GenerationAttemptCount")]
	public int GenerationAttemptCount { get; set; }

	[JsonProperty("NextGenerationRetryDay")]
	public int NextGenerationRetryDay { get; set; }

	[JsonProperty("LastGenerationError")]
	public string LastGenerationError { get; set; } = "";

	[JsonProperty("CreatedDay")]
	public int CreatedDay { get; set; }

	[JsonProperty("UpdatedDay")]
	public int UpdatedDay { get; set; }
}

internal readonly struct KingdomStrategicProfileTemplate
{
	public readonly string NationalPersonality;
	public readonly string LongTermStrategy;

	public KingdomStrategicProfileTemplate(string nationalPersonality, string longTermStrategy)
	{
		NationalPersonality = nationalPersonality ?? "";
		LongTermStrategy = longTermStrategy ?? "";
	}
}
