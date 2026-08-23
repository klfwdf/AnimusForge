using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		catch (Exception ex)
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
				detailMessage = "导出目录为空。";
				return false;
			}
			EnsureCurrentKingdomProfiles(runtimeFounded: false);
			string directory = Path.Combine(Path.GetFullPath(exportRoot), ExportDirectoryName);
			Directory.CreateDirectory(directory);
			KingdomStrategicProfileExportPackage package = BuildExportPackage(_storage.Profiles.Values);
			WriteJsonAtomic(Path.Combine(directory, FullExportFileName), package);
			detailMessage = "已导出国家卡 " + package.Profiles.Count.ToString(CultureInfo.InvariantCulture) + " 条。";
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
				detailMessage = "找不到要导出的国家。";
				return false;
			}
			KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
			if (profile == null)
			{
				detailMessage = "国家卡不存在。";
				return false;
			}
			string directory = Path.Combine(Path.GetFullPath(exportRoot), ExportDirectoryName, "kingdoms");
			Directory.CreateDirectory(directory);
			string fileName = BuildSingleExportFileName(profile);
			WriteJsonAtomic(Path.Combine(directory, fileName), CloneForExport(profile));
			detailMessage = "已导出：" + (profile.KingdomName ?? profile.KingdomId);
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
				errorMessage = "资料包中存在 " + invalidCount.ToString(CultureInfo.InvariantCulture) + " 条无效或重复的王国资料。";
				return false;
			}
			KingdomImportTargetIndex kingdomImportTargetIndex = BuildImportTargetIndex();
			Dictionary<string, KingdomStrategicProfileRecord> dictionary = new Dictionary<string, KingdomStrategicProfileRecord>(StringComparer.OrdinalIgnoreCase);
			foreach (KingdomStrategicProfileRecord entry in entries)
			{
				if (!TryResolveImportTarget(entry, kingdomImportTargetIndex, out Kingdom kingdom, out string warning) || kingdom == null)
				{
					errorMessage = "资料包中的王国无法安全匹配当前世界：" + (warning ?? "未知原因");
					return false;
				}
				string text = NormalizeId(kingdom.StringId);
				if (string.IsNullOrWhiteSpace(text) || dictionary.ContainsKey(text))
				{
					errorMessage = "资料包中存在重复或无效的王国目标：" + (kingdom.Name?.ToString() ?? text);
					return false;
				}
				dictionary[text] = CloneForExport(entry);
			}
			if (dictionary.Count <= 0)
			{
				errorMessage = "资料包中没有可重载的王国性格与战略。";
				return false;
			}
			// Preflight must not initialize missing profiles in the current save.  Count only live campaign kingdoms for the confirmation text.
			int num = GetCurrentKingdoms().Count((Kingdom x) => x != null && !dictionary.ContainsKey(NormalizeId(x.StringId)));
			plan = new KingdomDatabaseReloadPlan
			{
				SourceProfilesByTargetId = dictionary,
				CurrentProfilesResetToDefaultCount = num
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
				errorMessage = "序列化结果为空。";
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
				errorMessage = "回滚快照为空。";
				return false;
			}
			KingdomStrategicProfileStorage restoredStorage = JsonConvert.DeserializeObject<KingdomStrategicProfileStorage>(snapshotJson);
			if (restoredStorage == null)
			{
				errorMessage = "回滚快照无效。";
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
			if (plan?.SourceProfilesByTargetId == null || plan.SourceProfilesByTargetId.Count <= 0)
			{
				detailMessage = "王国重载计划为空。";
				return false;
			}
			// Never call EnsureCurrentKingdomProfiles here: it writes directly to the live save.  Build and normalize a complete candidate instead.
			KingdomStrategicProfileStorage kingdomStrategicProfileStorage = BuildDatabaseReloadCandidateStorage();
			if (kingdomStrategicProfileStorage?.Profiles == null)
			{
				detailMessage = "无法创建王国资料重载副本。";
				return false;
			}
			int currentCampaignDay = GetCurrentCampaignDay();
			foreach (KingdomStrategicProfileRecord value in kingdomStrategicProfileStorage.Profiles.Values)
			{
				ResetProfileToDefaultForDatabaseReload(value, currentCampaignDay);
			}
			foreach (KeyValuePair<string, KingdomStrategicProfileRecord> sourceProfileByTargetId in plan.SourceProfilesByTargetId)
			{
				if (!kingdomStrategicProfileStorage.Profiles.TryGetValue(NormalizeId(sourceProfileByTargetId.Key), out KingdomStrategicProfileRecord value2) || value2 == null)
				{
					detailMessage = "当前世界已变化，找不到王国目标：" + sourceProfileByTargetId.Key;
					return false;
				}
				ApplyDatabaseSourceProfile(value2, sourceProfileByTargetId.Value, currentCampaignDay);
			}
			_storage = kingdomStrategicProfileStorage;
			detailMessage = "已重载 " + plan.SourceProfilesByTargetId.Count.ToString(CultureInfo.InvariantCulture) + " 个王国；其余 " + plan.CurrentProfilesResetToDefaultCount.ToString(CultureInfo.InvariantCulture) + " 个当前王国恢复默认资料。";
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
				InformationManager.DisplayMessage(new InformationMessage("已覆灭国家保留现有国家卡，但不会请求 LLM 重新生成。"));
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
			profile.LastGenerationError = string.IsNullOrWhiteSpace(configError) ? "事件与叛乱 API 尚未配置。" : configError;
			if (showConfigError)
			{
				InformationManager.DisplayMessage(new InformationMessage(profile.LastGenerationError));
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
				? "国家已覆灭，已停止自动生成建国卡。"
				: "国家对象已不存在，无法生成建国卡。";
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
				NpcPolicyApiCallResult apiResult = await NpcPolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(
					request.SystemPrompt,
					FoundingGenerationMaxTokens,
					FoundingGenerationTimeoutMilliseconds,
					Source,
					request.RuntimeGeneration,
					3);
				if (!apiResult.Success)
				{
					result.ErrorMessage = apiResult.ErrorMessage ?? "API 请求失败。";
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
				InformationManager.DisplayMessage(new InformationMessage("已为新国家“" + (profile.KingdomName ?? id) + "”生成并固化国家性格与长期战略。"));
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
				detailMessage = "导入文件中没有与“" + GetKingdomName(selectedKingdom) + "”安全匹配的国家卡。动态建国 ID 只有在名称一致时才会匹配。";
				return false;
			}
			detailMessage = "已导入 " + imported.ToString(CultureInfo.InvariantCulture)
				+ " 条；跳过重复 " + duplicatesSkipped.ToString(CultureInfo.InvariantCulture)
				+ " 条；无效或无法匹配 " + unmatched.ToString(CultureInfo.InvariantCulture) + " 条。";
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
			errorMessage = "单国导入目标或路径为空。";
			return false;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(sourcePath);
		}
		catch (Exception ex)
		{
			errorMessage = "导入路径无效：" + ex.Message;
			return false;
		}
		if (File.Exists(fullPath))
		{
			resolvedSource = fullPath;
			return true;
		}
		if (!Directory.Exists(fullPath))
		{
			errorMessage = "导入目录不存在。";
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
			errorMessage = "发现多份可匹配该国家的单国文件，请删除旧副本或直接选择目标 JSON 文件。";
			return false;
		}
		errorMessage = "导入目录中没有与“" + GetKingdomName(targetKingdom) + "”安全匹配的国家卡。";
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
			errorMessage = allowMissing ? "" : "导入路径为空。";
			return allowMissing;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(sourcePath);
		}
		catch (Exception ex)
		{
			errorMessage = "导入路径无效：" + ex.Message;
			return false;
		}
		List<string> files = new List<string>();
		if (File.Exists(fullPath))
		{
			if (!string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = "只允许导入 JSON 文件。";
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
			errorMessage = allowMissing ? "" : ("未找到 " + ExportDirectoryName + "/" + FullExportFileName + " 或单国 JSON 文件。");
			return allowMissing;
		}
		if (files.Count > MaxImportProfileCount)
		{
			errorMessage = "单次导入文件数量超过 " + MaxImportProfileCount.ToString(CultureInfo.InvariantCulture) + " 个安全上限。";
			return false;
		}
		HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string file in files)
		{
			if (!TryReadImportFile(file, out List<KingdomStrategicProfileRecord> fileEntries, out int fileInvalid, out string fileError))
			{
				errorMessage = Path.GetFileName(file) + "：" + fileError;
				return false;
			}
			invalidCount += fileInvalid;
			foreach (KingdomStrategicProfileRecord entry in fileEntries)
			{
				if (entries.Count >= MaxImportProfileCount)
				{
					errorMessage = "国家卡数量超过 " + MaxImportProfileCount.ToString(CultureInfo.InvariantCulture) + " 条安全上限。";
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
				errorMessage = "文件不存在或为空。";
				return false;
			}
			if (info.Length > MaxImportFileBytes)
			{
				errorMessage = "文件超过 32 MB 安全上限。";
				return false;
			}
			string json = File.ReadAllText(filePath, StrictUtf8);
			JToken token = JToken.Parse(json);
			if (token is not JObject root)
			{
				errorMessage = "根节点必须是 JSON 对象。";
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
				errorMessage = "单国国家卡缺少有效 ID，或没有正文、默认正文及明确覆盖标记。";
				return false;
			}
			errorMessage = "JSON 结构不是国家卡导出格式。";
			return false;
		}
		catch (DecoderFallbackException)
		{
			errorMessage = "文件不是有效的 UTF-8 编码。";
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
			warning = "空国家卡。";
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
			warning = "动态国家 ID 相同但名称不同。";
		}
		if (!string.IsNullOrEmpty(importedName) && index.ByName.TryGetValue(importedName, out List<Kingdom> matches) && matches.Count == 1)
		{
			kingdom = matches[0];
			return true;
		}
		warning = string.IsNullOrEmpty(warning) ? "当前世界中没有唯一匹配国家。" : warning;
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
	private KingdomStrategicProfileStorage BuildDatabaseReloadCandidateStorage()
	{
		KingdomStrategicProfileStorage candidate = CloneStorage(_storage);
		foreach (Kingdom kingdom in GetCurrentKingdoms())
		{
			string kingdomId = NormalizeId(kingdom?.StringId);
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				continue;
			}
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
			throw new InvalidOperationException("导出目录无效。");
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
		string settlements = "无领地";
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
				settlements = string.Join("、", names);
			}
		}
		catch
		{
		}
		string enemies = "无明确敌国";
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
				enemies = string.Join("、", names);
			}
		}
		catch
		{
		}
		StringBuilder prompt = new StringBuilder(700);
		prompt.AppendLine("为新建国家生成一张固定角色卡。只输出一行JSON，不解释：");
		prompt.AppendLine("{\"national_personality\":\"...\",\"long_term_strategy\":\"...\"}");
		prompt.AppendLine("两项各50-160字。性格写决策风格、价值观与底线；战略写长期、可执行的国家目标。不要写数值或系统术语。");
		prompt.AppendLine("国家：" + GetKingdomName(kingdom) + "（" + (kingdom?.StringId ?? "") + "）");
		prompt.AppendLine("文化：" + (kingdom?.Culture?.Name?.ToString() ?? kingdom?.Culture?.StringId ?? "未知"));
		prompt.AppendLine("统治者：" + (ruler?.Name?.ToString() ?? "未知"));
		prompt.AppendLine("领地：" + settlements);
		prompt.Append("当前敌国：" + enemies);
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
				errorMessage = "LLM 返回内容中没有 JSON 对象。";
				return false;
			}
			JObject root = JObject.Parse(text.Substring(start, end - start + 1));
			personality = CleanProfileText(GetStringPropertyIgnoreCase(root, "national_personality", "NationalPersonality", "personality"));
			strategy = CleanProfileText(GetStringPropertyIgnoreCase(root, "long_term_strategy", "LongTermStrategy", "strategy"));
			if (string.IsNullOrWhiteSpace(personality) || string.IsNullOrWhiteSpace(strategy))
			{
				errorMessage = "LLM JSON 缺少 national_personality 或 long_term_strategy。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = "LLM JSON 解析失败：" + ex.Message;
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
				"以帝国法统、元老院传统和严整秩序为最高准则；自认肩负保存旧帝国制度的责任，善于运用法律与政治联盟，但对割据者长期戒备，不轻易承认分裂政权的正统性。",
				"以北方帝国的法统重新统一帝国：稳住核心行省，逐步削弱并吞并南方帝国与西方帝国，夺回旧帝国重镇，最终恢复由单一中央政权统治的帝国。"),
			["empire_s"] = new KingdomStrategicProfileTemplate(
				"重视皇室合法性、宫廷秩序与治理连续性；倾向用外交、婚盟和制度吸纳争取支持，一旦皇权受到挑战便表现得坚决而记仇，认为统一必须围绕合法皇位完成。",
				"以南方帝国皇室的继承权统一帝国：巩固南方富庶行省与盟友，争取旧贵族和城市支持，逐步击败北方帝国与西方帝国，建立获得广泛承认的唯一皇权。"),
			["empire_w"] = new KingdomStrategicProfileTemplate(
				"崇尚军功、纪律、实力与能者统治；决策直接强硬，愿意奖励有能力的将领和盟友，也会迅速惩罚软弱与背叛，认为帝国只能由胜利者重新锻造。",
				"以西方帝国的军政力量统一帝国：整合西部军团和战略要塞，持续压迫北方帝国与南方帝国，夺取关键城市与交通线，最终建立由强势中央和军功集团支撑的新帝国。"),
			["vlandia"] = new KingdomStrategicProfileTemplate(
				"务实、自信而尚武，尊重封建契约、领地回报和贵族荣誉；善于以土地换取效忠，对富庶领土和战略通道有强烈兴趣，面对示弱的邻国往往会步步施压。",
				"巩固西海岸与既有封臣体系，夺取连接内陆的城堡和商路，优先削弱巴旦尼亚及帝国西境的阻碍者，让瓦兰迪亚的封建领主与骑士势力逐步主导大陆西部。"),
			["battania"] = new KingdomStrategicProfileTemplate(
				"珍视氏族自由、古老森林和本土传统；对外来占领高度敏感，善伏击、善结盟但不喜长期受制于中央权威，面对侵占故土者会保持跨世代的敌意。",
				"守住并收复巴旦尼亚高地与森林故土，优先驱逐侵入核心山区的瓦兰迪亚和帝国势力，联合可利用的邻国削弱包围，最终恢复一个不受外族支配的高地共同体。"),
			["sturgia"] = new KingdomStrategicProfileTemplate(
				"坚韧、重誓言、重家族与战士荣誉，能忍受长期困苦；平时尊重实力与互惠，受到羞辱或边境蚕食时反应强硬，对内部离心和南方扩张保持警惕。",
				"统一并稳固北方诸地，守住寒地城镇和东西交通，收复被邻国侵占的传统边境；在内部稳定后向富庶南方争取港口、农地与缓冲区，确保斯特吉亚长期强盛。"),
			["aserai"] = new KingdomStrategicProfileTemplate(
				"讲求家族声望、互惠、贸易利益与沙漠秩序；擅长耐心议价和多方平衡，但会坚决报复对商路、绿洲与部族尊严的侵犯，通常避免没有收益的消耗战。",
				"维持纳哈萨各部族团结，控制绿洲、港口与跨沙漠商路，防止帝国和外族进入南方腹地；在贸易与财政稳固后夺取邻近富庶节点，使阿塞莱成为南方不可挑战的强权。"),
			["khuzait"] = new KingdomStrategicProfileTemplate(
				"机动、进取、尊重胜利与服从，善于在实力变化时迅速结盟或转向；把贡赋和臣服视为秩序证明，对软弱边境会主动试探，对反抗者倾向持续施压。",
				"整合东部草原与各部骑兵，持续向西夺取牧地、城市和交通线，迫使沿途王国臣服或纳贡；逐步击破帝国东境及其他阻碍者，建立横跨草原与大陆腹地的霸权。"),
			["nord"] = new KingdomStrategicProfileTemplate(
				"尚武、冒险、重视战利品、声望与首领威望；善于海上远征和突然进攻，尊重勇敢的敌人却轻视软弱与空洞承诺，内部需要持续胜利和分配战果来维持凝聚。",
				"持续入侵大陆上的各个王国：先夺取沿海港口和可供远征的据点，再轮番劫掠、削弱并征服大陆势力，把诺德人的定居地和统治范围不断向内陆推进。")
		};
	}

	private static KingdomStrategicProfileTemplate BuildGenericDefault(Kingdom kingdom)
	{
		string culture = NormalizeId(kingdom?.Culture?.StringId);
		string name = GetKingdomName(kingdom);
		if (culture.IndexOf("empire", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return new KingdomStrategicProfileTemplate(
				"重视制度、秩序、法统与城市治理，善于利用官僚、外交和军队维护中央权威；对分裂和失控保持警惕，在合法性受到挑战时会采取强硬行动。",
				"巩固现有行省和统治合法性，夺取具有政治与交通价值的帝国旧地，削弱竞争性政权，并逐步把周边势力纳入一个稳定、统一的中央秩序。" );
		}
		if (culture.IndexOf("nord", StringComparison.OrdinalIgnoreCase) >= 0 || culture.IndexOf("sturg", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return new KingdomStrategicProfileTemplate(
				"坚韧尚武，重视誓言、战士声望和共同分配战果；能忍耐艰苦，却不会长期容忍边境蚕食、公开羞辱或软弱的领导。",
				"先统一并守住本族核心领地与交通节点，再争取富庶边境、港口和缓冲区；通过可持续的胜利扩大影响，确保国家不被更强邻国包围或支配。" );
		}
		return new KingdomStrategicProfileTemplate(
			(name + "重视独立、生存、统治合法性与现实利益；会根据实力谨慎结盟和谈判，但在核心领土、政权延续与国家尊严受到威胁时保持明确底线。"),
			"巩固核心领地与内部团结，保障财政、兵源和关键交通，优先消除直接威胁；在条件有利时争取相邻战略据点和可靠盟友，使国家获得长期生存能力与更大地区影响。" );
	}

	private static KingdomStrategicProfileTemplate BuildFoundingFallback(Kingdom kingdom)
	{
		string ruler = GetRuler(kingdom)?.Name?.ToString() ?? "新统治者";
		return new KingdomStrategicProfileTemplate(
			"这是由" + ruler + "领导的新建政权，重视生存、凝聚、建国合法性和摆脱旧秩序控制；实力不足时会务实结盟，遭遇复辟、吞并或公开羞辱时反应强硬。",
			"巩固建国合法性与现有领地，吸纳支持者并抵御旧宗主或周边强国的压制；站稳后夺取相邻战略据点、建立稳定财政与军队，发展为能够长期存续的独立王国。" );
	}

	private static KingdomStrategicProfileTemplate BuildOrphanFallback(string kingdomName)
	{
		string name = string.IsNullOrWhiteSpace(kingdomName) ? "该国" : kingdomName;
		return new KingdomStrategicProfileTemplate(
			name + "重视独立、秩序和政权延续，会在现实利益与国家尊严之间权衡，对直接威胁保持戒备。",
			"巩固核心领地与内部团结，保障长期生存，消除主要威胁，并在条件成熟时扩大地区影响。" );
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
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "国家";
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
