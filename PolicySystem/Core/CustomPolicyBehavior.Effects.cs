using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private readonly PolicyEffectRuntimeIndex _policyEffectRuntimeIndex = new PolicyEffectRuntimeIndex();

	private sealed class PolicyEffectDailyRuntimePlanEntry
	{
		internal PolicyEffectInstanceSaveData Instance;

		internal IPolicyEffectModule Module;

		internal PolicyEffectPreparedInstance Prepared;

		internal PolicyEffectTargetKind TargetKind;

		internal string TargetId;
	}

	private sealed class PolicyEffectDailyRuntimePlan
	{
		internal readonly Dictionary<string, PolicyEffectDailyRuntimePlanEntry[]> SettlementEntries
			= new Dictionary<string, PolicyEffectDailyRuntimePlanEntry[]>(StringComparer.OrdinalIgnoreCase);

		internal PolicyEffectDailyRuntimePlanEntry[] NonSettlementEntries = Array.Empty<PolicyEffectDailyRuntimePlanEntry>();

		internal readonly Dictionary<string, PolicyEffectDailyRuntimePlanEntry> NonSettlementEntriesByKey
			= new Dictionary<string, PolicyEffectDailyRuntimePlanEntry>(StringComparer.OrdinalIgnoreCase);

		internal readonly HashSet<string> SettlementTargetIds
			= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal readonly HashSet<string> CanonicalSettlementTargetIds
			= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal int TargetExecutionCount;
	}

	private readonly Dictionary<string, PolicyEffectDailyRuntimePlan> _policyEffectDailyRuntimePlans
		= new Dictionary<string, PolicyEffectDailyRuntimePlan>(StringComparer.OrdinalIgnoreCase);

	// Daily game mutations are batched until the next active-effect persistence
	// boundary. The list is normally very small (one settlement batch or the cached
	// non-settlement plan) and is cleared immediately after a successful commit.
	private readonly Dictionary<string, List<PolicyEffectDailyExecutionOutcome>> _policyEffectDailyPersistenceTransactions
		= new Dictionary<string, List<PolicyEffectDailyExecutionOutcome>>(StringComparer.OrdinalIgnoreCase);

	// Populated when recovery state is written and once during load rebuild. The
	// daily scheduler uses this O(1) gate so normal effects do not scan up to 24
	// module runtime states just to prove that no exceptional marker exists.
	private readonly HashSet<string> _policyEffectPendingDailyCompensationEffectIds
		= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	// Corrupt persisted rows remain in the save dictionary as recovery evidence but
	// never enter runtime caches, indexes, or work queues. Add() is also the log-once gate.
	private readonly HashSet<string> _quarantinedActivePolicyEffectIds
		= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _policyTargetStructureDependencyEffectIds
		= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _policyTargetRelationDependencyEffectIds
		= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _legacyPolicyTargetRefreshEffectIds
		= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private sealed class PlayerPolicyMaintenanceRuntimeEntry
	{
		internal string EffectId;
		internal string RecordId;
		internal string PolicyName;
		internal int SubmittedDay;
		internal long CreatedUtcTicks;
		internal int DailyCost;
		internal ActivePolicyEffectSaveData Effect;
	}

	private readonly Dictionary<string, PlayerPolicyMaintenanceRuntimeEntry> _playerPolicyMaintenanceRuntimeIndex
		= new Dictionary<string, PlayerPolicyMaintenanceRuntimeEntry>(StringComparer.OrdinalIgnoreCase);

	private PlayerPolicyMaintenanceRuntimeEntry[] _playerPolicyMaintenanceSortedSnapshot
		= Array.Empty<PlayerPolicyMaintenanceRuntimeEntry>();

	private bool _playerPolicyMaintenanceSnapshotDirty = true;

	private sealed class PlayerPolicyMaintenanceSettlementContext
	{
		internal CustomPolicyBehavior Behavior;
		internal Clan Clan;
		internal int Day;
		internal int BeforeGold;
		internal int ExpectedGoldDelta;
		internal PlayerPolicyMaintenanceRuntimeEntry[] DueEntries = Array.Empty<PlayerPolicyMaintenanceRuntimeEntry>();
		internal bool[] Funded = Array.Empty<bool>();
		internal bool IntentPrepared;
	}

	[ThreadStatic]
	private static PlayerPolicyMaintenanceSettlementContext _playerPolicyMaintenanceSettlementContext;

	private const string PolicyEffectRollbackPendingPrefix = "rollbackPending:";

	private bool _policyEffectRuntimeIndexInitialized;

	private const int MaxWirePolicyEffects = 12;

	private const int MaxCompiledPolicyEffectInstances = 24;

	private const int MaxPolicyEffectPayloadBytes = 4 * 1024;

	private const int MaxPolicyEffectPayloadTotalBytes = 32 * 1024;

	private bool TryRegisterPolicyEffectBundleInternal(
		PolicyEffectBundleRegistration registration,
		out string effectId,
		out string failureReason)
	{
		effectId = string.Empty;
		failureReason = string.Empty;
		if (registration == null
			|| (registration.IsPermanentEffect ? registration.DurationDays != 0 : registration.DurationDays <= 0))
		{
			failureReason = "policy effect bundle 注册数据无效";
			return false;
		}
		string scope = string.IsNullOrWhiteSpace(registration.ScopeKind)
			? PolicyScopeKingdom
			: registration.ScopeKind.Trim();
		List<PolicyEffectInstanceSaveData> sourceInstances = registration.ModuleEffects
			?? new List<PolicyEffectInstanceSaveData>();
		if (sourceInstances.Count <= 0 || sourceInstances.Count > MaxCompiledPolicyEffectInstances)
		{
			failureReason = "policy effect bundle 的模块实例数量必须在 1-"
				+ MaxCompiledPolicyEffectInstances.ToString(CultureInfo.InvariantCulture) + " 之间";
			return false;
		}

		effectId = string.IsNullOrWhiteSpace(registration.EffectId)
			? Guid.NewGuid().ToString("N")
			: registration.EffectId.Trim();
		if (_activePolicyEffects.ContainsKey(effectId))
		{
			failureReason = "重复政策效果: " + effectId;
			return false;
		}

		HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
		List<PolicyEffectInstanceSaveData> normalizedInstances = new List<PolicyEffectInstanceSaveData>(sourceInstances.Count);
		int totalPayloadBytes = 0;
		PolicyTargetWorldSnapshot registrationTargetPlanSnapshot = null;
		for (int index = 0; index < sourceInstances.Count; index++)
		{
			PolicyEffectInstanceSaveData source = sourceInstances[index];
			if (source == null)
			{
				failureReason = "policy effect bundle 包含空的模块实例";
				return false;
			}
			string instanceId = (source?.InstanceId ?? string.Empty).Trim();
			string moduleId = (source?.ModuleId ?? string.Empty).Trim();
			// Legacy stability was an immediate OneShot. An uncommitted legacy shell
			// is adopted as the deferred implementation; completed legacy receipts are
			// intentionally left untouched and never replayed.
			if (string.Equals(moduleId, "kingdomStabilityOnce", StringComparison.Ordinal)
				&& source?.ExecutionReceipt == null
				&& source.LifecycleState != PolicyEffectLifecycleState.Completed
				&& source.LifecycleState != PolicyEffectLifecycleState.RolledBack)
			{
				moduleId = "kingdomStabilityNextDayOnce";
			}
			if (instanceId.Length <= 0 || !instanceIds.Add(instanceId))
			{
				failureReason = "policy effect bundle 包含空或重复的 instanceId";
				return false;
			}
			if (!PolicyEffectModuleCatalog.TryGet(moduleId, out IPolicyEffectModule module)
				|| !PolicyEffectModuleCatalog.IsAllowedForScope(module, scope))
			{
				failureReason = "policy effect bundle 包含未注册或作用域不兼容的模块: " + moduleId;
				return false;
			}
			JToken persistedPayload = source.Payload;
			if (!string.Equals(moduleId, (source.ModuleId ?? string.Empty).Trim(), StringComparison.Ordinal)
				&& source.Payload is JObject legacyPayload)
			{
				JObject migratedPayload = (JObject)legacyPayload.DeepClone();
				migratedPayload["moduleId"] = moduleId;
				persistedPayload = migratedPayload;
			}
			string payloadError = string.Empty;
			if (!TryValidatePolicyEffectPayloadSize(source.Payload, ref totalPayloadBytes, out failureReason)
				|| !module.TryMigratePayload(persistedPayload, source.PayloadSchemaVersion, out PolicyEffectPayload payload, out payloadError))
			{
				if (string.IsNullOrWhiteSpace(failureReason))
				{
					failureReason = "policy effect bundle payload 无效: " + module.Id + " / " + payloadError;
				}
				return false;
			}
			PolicyEffectCanonicalTargetSet targetSet = NormalizePolicyEffectCanonicalTargetSet(source.TargetSet);
			if (targetSet.TargetPlans.Count > 0
				&& !TryMaterializePolicyTargetPlansForRegistration(
					targetSet,
					module,
					scope,
					registration.TargetKingdomId,
					registration.IssuerKingdomId,
					registration.ProposerClanId,
					NormalizeIdList(registration.TargetFiefIds),
					ref registrationTargetPlanSnapshot,
					out targetSet,
					out string targetPlanError))
			{
				failureReason = "policy effect bundle TargetPlan materialization failed: "
					+ module.Id + " / " + targetPlanError;
				return false;
			}
			if (!PolicyEffectTargetJurisdiction.TryApply(
				targetSet,
				module,
				registration.TargetKingdomId,
				registration.IssuerKingdomId,
				targetSet.AuthorizedCrossKingdomIds,
				preserveLegacyCrossKingdoms: false,
				failOnUnauthorized: true,
				out targetSet,
				out string targetJurisdictionError))
			{
				failureReason = "policy effect bundle target jurisdiction failed: "
					+ module.Id + " / " + targetJurisdictionError;
				return false;
			}
			if (!HasPolicyEffectCanonicalTargetsForModule(module, targetSet))
			{
				failureReason = "policy effect bundle 缺少规范目标集合: " + module.Id;
				return false;
			}
			float startDay = source.StartDay > 0f ? source.StartDay : Math.Max(0, registration.SubmittedDay);
			float endDay = registration.IsPermanentEffect
				? 0f
				: source.EndDay > startDay ? source.EndDay : startDay + registration.DurationDays;
				normalizedInstances.Add(new PolicyEffectInstanceSaveData
				{
				MechanismContractVersion = source.MechanismContractVersion,
				MechanismContractHash = source.MechanismContractHash ?? string.Empty,
				ExpectedMechanismLegIds = new List<string>(source.ExpectedMechanismLegIds ?? new List<string>()),
				EffectPlanVersion = source.EffectPlanVersion,
				MechanismId = source.MechanismId ?? string.Empty,
				MechanismKind = source.MechanismKind,
				MechanismRole = source.MechanismRole,
				SourceOmitted = source.SourceOmitted,
				DestinationOmitted = source.DestinationOmitted,
					InstanceId = instanceId,
					PolicyId = FirstNonEmpty(source.PolicyId, registration.RecordId),
					ModuleId = module.Id,
					SourceModuleId = FirstNonEmpty(source.SourceModuleId, module.Id),
					ActorHeroId = FirstNonEmpty(source.ActorHeroId, registration.ActorHeroId),
				PayloadSchemaVersion = module.Descriptor.PayloadSchemaVersion,
				Payload = JToken.FromObject(payload),
				TargetSet = targetSet,
				LifecycleState = source.LifecycleState == PolicyEffectLifecycleState.Completed
					|| source.LifecycleState == PolicyEffectLifecycleState.RolledBack
					? source.LifecycleState
					: PolicyEffectLifecycleState.Active,
				StateSchemaVersion = Math.Max(0, source.StateSchemaVersion),
				RuntimeState = source.RuntimeState?.DeepClone(),
				ExecutionReceipt = ClonePolicyEffectExecutionReceipt(source.ExecutionReceipt),
				StartDay = startDay,
				EndDay = endDay,
				SourceScope = scope,
				Reason = source.Reason ?? registration.Reason ?? string.Empty
			});
		}
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> linkedGroup in normalizedInstances
			.Where(instance => instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion
				&& instance.MechanismKind == PolicyEffectMechanismKind.Linked)
			.GroupBy(instance => (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty),
				StringComparer.Ordinal))
		{
			if (!PolicyEffectMechanismContract.TryValidateLinkedGroup(linkedGroup, out string contractError))
			{
				failureReason = "policy effect bundle Linked contract invalid: " + contractError;
				return false;
			}
		}

		List<PolicyEffectExecutionReceipt> receipts = new List<PolicyEffectExecutionReceipt>();
		HashSet<string> receiptIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectExecutionReceipt receipt in registration.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
		{
			string receiptId = (receipt?.ReceiptId ?? string.Empty).Trim();
			if (receipt == null || receiptId.Length <= 0 || !receiptIds.Add(receiptId)
				|| !instanceIds.Contains((receipt.InstanceId ?? string.Empty).Trim()))
			{
				failureReason = "policy effect bundle 包含无效、重复或孤立的 execution receipt";
				return false;
			}
			receipts.Add(ClonePolicyEffectExecutionReceipt(receipt));
		}
		if (!PolicyEffectActivationCoordinator.TryActivate(
			normalizedInstances,
			receipts,
			BannerlordPolicyEffectGameBridge.Instance,
			GetCurrentCampaignDay(),
			out PolicyEffectActivationTransaction activation,
			out failureReason))
		{
			return false;
		}
		normalizedInstances = activation.Instances;
		receipts = receipts
			.Concat(activation.Receipts)
			.Where(item => item != null && !string.IsNullOrWhiteSpace(item.ReceiptId))
			.GroupBy(item => item.ReceiptId, StringComparer.Ordinal)
			.Select(group => ClonePolicyEffectExecutionReceipt(group.Last()))
			.ToList();
		registration.ModuleEffects = normalizedInstances;
		registration.ExecutionReceipts = receipts;

		ActivePolicyEffectSaveData activeEffect = new ActivePolicyEffectSaveData
		{
			Version = 8,
			ModuleEffects = normalizedInstances,
			ExecutionReceipts = receipts,
			ScopeKind = scope,
			LocalTargetScope = registration.LocalTargetScope ?? string.Empty,
			TargetFiefIds = NormalizeIdList(registration.TargetFiefIds),
			TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(normalizedInstances),
			TargetClanIds = NormalizeIdList(registration.TargetClanIds),
			DirectTargetSettlementIds = NormalizeIdList(registration.DirectTargetSettlementIds),
			FollowCurrentRulingClan = registration.FollowCurrentRulingClan,
			EffectId = effectId,
			RecordId = registration.RecordId ?? string.Empty,
			ProposerClanId = registration.ProposerClanId ?? string.Empty,
			IssuerKingdomId = registration.IssuerKingdomId ?? string.Empty,
			PolicyName = registration.PolicyName ?? string.Empty,
			DateText = registration.DateText ?? string.Empty,
			SubmittedDay = Math.Max(0, registration.SubmittedDay),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			TargetKingdomId = registration.TargetKingdomId ?? string.Empty,
			TargetKingdomName = registration.TargetKingdomName ?? string.Empty,
			TargetHandle = registration.TargetHandle ?? string.Empty,
			TargetLabel = registration.TargetLabel ?? string.Empty,
			TotalDurationDays = registration.DurationDays,
			RemainingDays = registration.DurationDays,
			IsPermanentEffect = registration.IsPermanentEffect,
			DailyMaintenanceGoldCost = Math.Max(0, registration.DailyMaintenanceGoldCost),
			TotalMaintenancePaidGold = Math.Max(0, registration.TotalMaintenancePaidGold),
			MaintenanceChargeEnabled = registration.MaintenanceChargeEnabled,
			MaintenanceFunded = !registration.MaintenanceChargeEnabled || registration.MaintenanceFunded,
			LastMaintenanceSettlementDay = registration.LastMaintenanceSettlementDay >= 0
				? registration.LastMaintenanceSettlementDay
				: GetCurrentCampaignDay(),
			LastEffectProcessedDay = registration.LastEffectProcessedDay >= 0
				? registration.LastEffectProcessedDay
				: GetCurrentCampaignDay(),
			LastAppliedDay = GetCurrentCampaignDay(),
			Reason = registration.Reason ?? string.Empty,
			Ended = false,
			EndReason = string.Empty
		};
		try
		{
			PersistActivePolicyEffect(effectId, activeEffect);
		}
		catch (Exception ex)
		{
			PolicyEffectActivationCoordinator.RollbackAppliedOneShots(
				activation,
				BannerlordPolicyEffectGameBridge.Instance,
				GetCurrentCampaignDay(),
				out string rollbackError);
			failureReason = "policy effect bundle persist failed: " + ex.Message
				+ (string.IsNullOrWhiteSpace(rollbackError) ? string.Empty : "; rollback=" + rollbackError);
			return false;
		}
		string persistedLifecycleState = normalizedInstances.Any(instance =>
			instance?.LifecycleState == PolicyEffectLifecycleState.Suspended)
			? "suspended"
			: "active";
		string persistedTargetHash = BuildPolicyEffectTargetFingerprint(
			normalizedInstances,
			out int persistedTargetCount);
		string persistedTargetSummary = BuildPolicyEffectTargetSetLogSummary(
			normalizedInstances.Select(instance => instance?.TargetSet));
		PolicySystemLog.Lifecycle("Effect", "active-bundle-created", "success", new PolicyLogContext
		{
			TransactionId = effectId,
			PolicyId = normalizedInstances.FirstOrDefault()?.PolicyId,
			RecordId = activeEffect.RecordId,
			EffectId = effectId,
			TargetHash = persistedTargetHash,
			TargetCount = persistedTargetCount,
			TargetKingdomId = activeEffect.TargetKingdomId,
			TargetKingdomName = activeEffect.TargetKingdomName,
			TargetName = activeEffect.TargetLabel,
			TargetSummary = persistedTargetSummary,
			StateBefore = "prepared",
			StateAfter = persistedLifecycleState,
			Counts = new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["moduleInstances"] = normalizedInstances.Count,
				["receipts"] = receipts.Count,
				["durationDays"] = activeEffect.TotalDurationDays
			}
		});
		foreach (PolicyEffectInstanceSaveData instance in normalizedInstances)
		{
			if (instance == null)
			{
				continue;
			}
			PolicySystemLog.Lifecycle("Effect", "instance-created", instance.LifecycleState.ToString(), new PolicyLogContext
			{
				TransactionId = effectId,
				PolicyId = instance.PolicyId,
				RecordId = activeEffect.RecordId,
				EffectId = effectId,
				MechanismId = instance.MechanismId,
				ModuleId = instance.ModuleId,
				InstanceId = instance.InstanceId,
				TargetHash = persistedTargetHash,
				TargetCount = persistedTargetCount,
				TargetSummary = BuildPolicyEffectTargetSetLogSummary(instance.TargetSet),
				StateAfter = instance.LifecycleState.ToString()
			});
		}
		foreach (PolicyEffectExecutionReceipt receipt in receipts)
		{
			if (receipt == null)
			{
				continue;
			}
			PolicySystemLog.Lifecycle("Effect", "receipt-created", receipt.Status.ToString(), new PolicyLogContext
			{
				TransactionId = effectId,
				PolicyId = receipt.PolicyId,
				RecordId = activeEffect.RecordId,
				EffectId = effectId,
				ModuleId = receipt.ModuleId,
				InstanceId = receipt.InstanceId,
				ReceiptId = receipt.ReceiptId,
				CampaignDay = float.IsNaN(receipt.CampaignDay) || float.IsInfinity(receipt.CampaignDay)
					? null
					: (int?)Math.Floor(receipt.CampaignDay)
			});
		}
		PolicySystemLog.Transaction(
			effectId,
			activeEffect.RecordId,
			effectId,
			string.Empty,
			"bundlePersisted",
			"success",
			targetHash: persistedTargetHash,
			targetCount: persistedTargetCount,
			executionReceipt: "receipts=" + receipts.Count.ToString(CultureInfo.InvariantCulture),
			stateBefore: "prepared",
			stateAfter: persistedLifecycleState);
		if (string.Equals(persistedLifecycleState, "suspended", StringComparison.Ordinal))
		{
			PolicySystemLog.Transaction(
				effectId,
				activeEffect.RecordId,
				effectId,
				string.Empty,
				"suspended",
				"success",
				targetHash: persistedTargetHash,
				targetCount: persistedTargetCount,
				stateBefore: "bundlePersisted",
				stateAfter: "suspended");
		}
		return true;
	}


	private static void ApplyPolicySettlementModelPatchesOnce()
	{
		if (_policySettlementModelPatchesApplied || Campaign.Current?.Models == null)
		{
			return;
		}
		try
		{
			Harmony harmony = new Harmony("com.AnimusForge.custompolicy.settlementmodels");
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementProsperityModel, "CalculateProsperityChange", new Type[2] { typeof(Town), typeof(bool) }, nameof(Patch_PolicyProsperityChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementProsperityModel, "CalculateHearthChange", new Type[2] { typeof(Village), typeof(bool) }, nameof(Patch_PolicyHearthChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementFoodModel, "CalculateTownFoodStocksChange", new Type[3] { typeof(Town), typeof(bool), typeof(bool) }, nameof(Patch_PolicyFoodChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementLoyaltyModel, "CalculateLoyaltyChange", new Type[2] { typeof(Town), typeof(bool) }, nameof(Patch_PolicyLoyaltyChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementSecurityModel, "CalculateSecurityChange", new Type[2] { typeof(Town), typeof(bool) }, nameof(Patch_PolicySecurityChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementMilitiaModel, "CalculateMilitiaChange", new Type[2] { typeof(Settlement), typeof(bool) }, nameof(Patch_PolicyMilitiaChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.SettlementTaxModel, "CalculateTownTax", new Type[2] { typeof(Town), typeof(bool) }, nameof(Patch_PolicyTownTax_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.BuildingConstructionModel, "CalculateDailyConstructionPower", new Type[2] { typeof(Town), typeof(bool) }, nameof(Patch_PolicyConstructionPower_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.BuildingConstructionModel, "CalculateDailyConstructionPowerWithoutBoost", new Type[1] { typeof(Town) }, nameof(Patch_PolicyConstructionPowerWithoutBoost_Postfix));
			PatchPolicySettlementModelMethod(harmony, Campaign.Current.Models.VolunteerModel, "GetDailyVolunteerProductionProbability", new Type[3] { typeof(Hero), typeof(int), typeof(Settlement) }, nameof(Patch_PolicyVolunteerProductionProbability_Postfix));
			_policySettlementModelPatchesApplied = true;
			PolicySystemLog.Write("Effect", "settlement-model-patches-applied", "AF policy effects now participate in vanilla settlement and volunteer calculations and tooltips");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Effect", "settlement-model-patches-failed", ex.ToString());
		}
	}

	private static void ApplyPolicyFinanceModelPatchesOnce()
	{
		if (_policyFinanceModelPatchesApplied || Campaign.Current?.Models?.ClanFinanceModel == null)
		{
			return;
		}
		try
		{
			Harmony harmony = new Harmony("com.AnimusForge.custompolicy.clanfinance");
			object financeModel = Campaign.Current.Models.ClanFinanceModel;
			Type[] signature = { typeof(Clan), typeof(bool), typeof(bool), typeof(bool) };
			PatchPolicySettlementModelMethod(harmony, financeModel, "CalculateClanGoldChange", signature, nameof(Patch_PolicyClanGoldChange_Postfix));
			PatchPolicySettlementModelMethod(harmony, financeModel, "CalculateClanIncome", signature, nameof(Patch_PolicyClanIncome_Postfix));
			PatchPolicySettlementModelMethod(harmony, financeModel, "CalculateClanExpenses", signature, nameof(Patch_PolicyClanExpenses_Postfix));
			System.Reflection.MethodInfo dailyTickClan = AccessTools.Method(
				typeof(ClanVariablesCampaignBehavior),
				"DailyTickClan",
				new[] { typeof(Clan) });
			if (dailyTickClan == null)
			{
				throw new MissingMethodException(typeof(ClanVariablesCampaignBehavior).FullName, "DailyTickClan(Clan)");
			}
			harmony.Patch(
				dailyTickClan,
				prefix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_PolicyClanDailyTick_Prefix)),
				postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_PolicyClanDailyTick_Postfix)),
				finalizer: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_PolicyClanDailyTick_Finalizer)));
			_policyFinanceModelPatchesApplied = true;
			PolicySystemLog.Write("Effect", "clan-finance-patches-applied", "player policy maintenance and positive daily hero gold now participate in vanilla clan finance reporting");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Effect", "clan-finance-patches-failed", ex.ToString());
		}
	}

	private static void ApplyPolicyClanPoliticsModelPatchesOnce()
	{
		if (_policyClanPoliticsModelPatchesApplied || Campaign.Current?.Models?.ClanPoliticsModel == null)
		{
			return;
		}
		try
		{
			Harmony harmony = new Harmony("com.AnimusForge.custompolicy.clanpolitics");
			object politicsModel = Campaign.Current.Models.ClanPoliticsModel;
			Type[] signature = { typeof(Clan), typeof(bool) };
			PatchPolicySettlementModelMethod(
				harmony,
				politicsModel,
				"CalculateInfluenceChange",
				signature,
				nameof(Patch_PolicyClanInfluenceChange_Postfix));
			_policyClanPoliticsModelPatchesApplied = true;
			PolicySystemLog.Write("Effect", "clan-politics-model-patch-applied",
				"AF daily clan influence effects now participate in vanilla influence descriptions without duplicate settlement");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Effect", "clan-politics-model-patch-failed", ex.ToString());
		}
	}

	private static void Patch_PolicyClanInfluenceChange_Postfix(
		Clan clan,
		bool __1,
		ref ExplainedNumber __result)
	{
		bool includeDescriptions = __1;
		if (!includeDescriptions)
		{
			return;
		}
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		string clanId = (clan?.StringId ?? string.Empty).Trim();
		if (behavior == null || clanId.Length == 0)
		{
			return;
		}
		AddIndexedAdditiveContributions(
			behavior._policyEffectRuntimeIndex,
			PolicyEffectHook.DailyScheduler,
			PolicyEffectTargetKind.Clan,
			clanId,
			includeDescriptions: true,
			ref __result);
	}

	private static void Patch_PolicyClanGoldChange_Postfix(
		Clan clan,
		bool includeDescriptions,
		bool applyWithdrawals,
		bool includeDetails,
		ref ExplainedNumber __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null || clan == null || clan != Clan.PlayerClan)
		{
			return;
		}
		if (applyWithdrawals)
		{
			behavior.PreparePlayerPolicyMaintenanceSettlement(clan, includeDescriptions, includeDetails, ref __result);
			return;
		}
		behavior.AddPlayerPolicyGoldIncomeReport(includeDescriptions, ref __result);
		behavior.AddPlayerPolicyMaintenanceObligations(includeDescriptions, includeDetails, ref __result);
	}

	private static void Patch_PolicyClanIncome_Postfix(
		Clan clan,
		bool includeDescriptions,
		bool applyWithdrawals,
		bool includeDetails,
		ref ExplainedNumber __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null || clan == null || clan != Clan.PlayerClan || applyWithdrawals)
		{
			return;
		}
		// This is a reporting projection only. The daily effect coordinator remains the
		// sole state-changing owner of heroGoldPerDay.
		behavior.AddPlayerPolicyGoldIncomeReport(includeDescriptions, ref __result);
	}

	private static void Patch_PolicyClanExpenses_Postfix(
		Clan clan,
		bool includeDescriptions,
		bool applyWithdrawals,
		bool includeDetails,
		ref ExplainedNumber __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null || clan == null || clan != Clan.PlayerClan)
		{
			return;
		}
		// Expenses is a reporting surface. The sole state-changing settlement entry is
		// CalculateClanGoldChange(..., applyWithdrawals:true).
		behavior.AddPlayerPolicyMaintenanceObligations(includeDescriptions, includeDetails, ref __result);
	}

	private void AddPlayerPolicyMaintenanceObligations(
		bool includeDescriptions,
		bool includeDetails,
		ref ExplainedNumber result)
	{
		PlayerPolicyMaintenanceRuntimeEntry[] entries = GetPlayerPolicyMaintenanceSortedSnapshot();
		if (entries.Length == 0)
		{
			return;
		}
		int currentDay = GetCurrentCampaignDay();
		int total = 0;
		for (int index = 0; index < entries.Length; index++)
		{
			PlayerPolicyMaintenanceRuntimeEntry entry = entries[index];
			if (!IsPlayerPolicyMaintenanceDue(entry, currentDay) || entry.DailyCost <= 0)
			{
				continue;
			}
			total = total > int.MaxValue - entry.DailyCost ? int.MaxValue : total + entry.DailyCost;
		}
		AddPlayerPolicyMaintenanceTotal(total, includeDescriptions, ref result);
	}

	private static void Patch_PolicyClanDailyTick_Prefix(Clan __0)
	{
		_playerPolicyMaintenanceSettlementContext = null;
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null || __0 == null || __0 != Clan.PlayerClan)
		{
			return;
		}
		_playerPolicyMaintenanceSettlementContext = new PlayerPolicyMaintenanceSettlementContext
		{
			Behavior = behavior,
			Clan = __0,
			Day = GetCurrentCampaignDay(),
			BeforeGold = __0.Gold
		};
	}

	private static void Patch_PolicyClanDailyTick_Postfix(Clan __0)
	{
		ReconcilePlayerPolicyMaintenanceSettlement(__0, null);
	}

	private static Exception Patch_PolicyClanDailyTick_Finalizer(Clan __0, Exception __exception)
	{
		ReconcilePlayerPolicyMaintenanceSettlement(__0, __exception);
		return __exception;
	}

	private static void ReconcilePlayerPolicyMaintenanceSettlement(Clan clan, Exception exception)
	{
		PlayerPolicyMaintenanceSettlementContext context = _playerPolicyMaintenanceSettlementContext;
		_playerPolicyMaintenanceSettlementContext = null;
		if (context?.Behavior == null || !context.IntentPrepared || context.Clan != clan)
		{
			return;
		}
		bool confirmed = PlayerPolicyMaintenancePlanner.IsSettlementGoldDeltaConfirmed(
			context.BeforeGold,
			clan?.Gold ?? context.BeforeGold,
			context.ExpectedGoldDelta);
		context.Behavior.CommitPlayerPolicyMaintenanceSettlement(
			context,
			confirmed,
			exception == null ? string.Empty : exception.GetType().Name + ": " + exception.Message);
	}

	private void PreparePlayerPolicyMaintenanceSettlement(
		Clan clan,
		bool includeDescriptions,
		bool includeDetails,
		ref ExplainedNumber result)
	{
		PlayerPolicyMaintenanceRuntimeEntry[] entries = GetPlayerPolicyMaintenanceSortedSnapshot();
		if (entries.Length == 0)
		{
			return;
		}
		int currentDay = GetCurrentCampaignDay();
		List<PlayerPolicyMaintenanceRuntimeEntry> due = new List<PlayerPolicyMaintenanceRuntimeEntry>(entries.Length);
		for (int index = 0; index < entries.Length; index++)
		{
			if (IsPlayerPolicyMaintenanceSettlementDue(entries[index], currentDay))
			{
				due.Add(entries[index]);
			}
		}
		if (due.Count == 0)
		{
			return;
		}
		int vanillaNetGold = MathF.Round(result.ResultNumber);
		long availableAfterVanilla = (long)Math.Max(0, clan.Gold) + vanillaNetGold;
		int availableGold = availableAfterVanilla <= 0
			? 0
			: availableAfterVanilla >= int.MaxValue ? int.MaxValue : (int)availableAfterVanilla;
		int[] costs = new int[due.Count];
		for (int index = 0; index < due.Count; index++)
		{
			costs[index] = due[index].DailyCost;
		}
		bool[] funded = PlayerPolicyMaintenancePlanner.AllocateStrictOldestPrefix(costs, availableGold);
		int paidTotal = 0;
		for (int index = 0; index < due.Count; index++)
		{
			PlayerPolicyMaintenanceRuntimeEntry entry = due[index];
			bool isFunded = entry.DailyCost <= 0 || funded[index];
			if (isFunded && entry.DailyCost > 0)
			{
				paidTotal = paidTotal > int.MaxValue - entry.DailyCost ? int.MaxValue : paidTotal + entry.DailyCost;
			}
		}
		AddPlayerPolicyMaintenanceTotal(paidTotal, includeDescriptions, ref result);
		PlayerPolicyMaintenanceSettlementContext context = _playerPolicyMaintenanceSettlementContext;
		if (context != null && context.Behavior == this && context.Clan == clan && context.Day == currentDay)
		{
			context.DueEntries = due.ToArray();
			context.Funded = funded;
			context.ExpectedGoldDelta = MathF.Round(result.ResultNumber);
			context.IntentPrepared = true;
		}
	}

	private void CommitPlayerPolicyMaintenanceSettlement(
		PlayerPolicyMaintenanceSettlementContext context,
		bool debitConfirmed,
		string exception)
	{
		for (int index = 0; index < context.DueEntries.Length; index++)
		{
			PlayerPolicyMaintenanceRuntimeEntry entry = context.DueEntries[index];
			ActivePolicyEffectSaveData effect = entry?.Effect;
			if (effect == null)
			{
				continue;
			}
			bool wasFunded = effect.MaintenanceFunded;
			bool isFunded = debitConfirmed && (entry.DailyCost <= 0 || context.Funded[index]);
			effect.MaintenanceFunded = isFunded;
			effect.LastMaintenanceSettlementDay = context.Day;
			if (isFunded && entry.DailyCost > 0)
			{
				effect.TotalMaintenancePaidGold = effect.TotalMaintenancePaidGold > int.MaxValue - entry.DailyCost
					? int.MaxValue
					: effect.TotalMaintenancePaidGold + entry.DailyCost;
			}
			PersistActivePolicyEffect(effect.EffectId, effect, structureChanged: wasFunded != isFunded);
			UpdatePlayerPolicyMaintenanceRecord(effect);
			if (wasFunded != isFunded && entry.DailyCost > 0)
			{
				NotifyPlayerPolicyMaintenanceStateChanged(effect, isFunded);
			}
			if (isFunded)
			{
				EnqueueActivePolicyEffectWork(effect.EffectId);
			}
		}
		if (!debitConfirmed)
		{
			PolicySystemLog.Failure("Finance", "maintenance-debit-mismatch",
				"policy maintenance was settled unfunded because the actual clan gold delta did not match the finance intent",
				"day=" + context.Day.ToString(CultureInfo.InvariantCulture)
				+ " expectedDelta=" + context.ExpectedGoldDelta.ToString(CultureInfo.InvariantCulture)
				+ " actualDelta=" + ((context.Clan?.Gold ?? context.BeforeGold) - context.BeforeGold).ToString(CultureInfo.InvariantCulture)
				+ (string.IsNullOrWhiteSpace(exception) ? string.Empty : " exception=" + exception));
		}
		PolicySystemLog.Transaction(
			"maintenance:" + context.Day.ToString(CultureInfo.InvariantCulture),
			string.Empty,
			string.Empty,
			string.Empty,
			"costCommitted",
			debitConfirmed ? "success" : "failed",
			errorKind: debitConfirmed ? string.Empty : "ActualGoldDeltaMismatch",
			targetCount: context.DueEntries.Length,
			costReceipt: "expectedDelta=" + context.ExpectedGoldDelta.ToString(CultureInfo.InvariantCulture)
				+ ";actualDelta=" + ((context.Clan?.Gold ?? context.BeforeGold) - context.BeforeGold).ToString(CultureInfo.InvariantCulture),
			stateBefore: "settlementIntent",
			stateAfter: debitConfirmed ? "funded" : "unfunded");
	}

	private static bool IsPlayerPolicyMaintenanceDue(PlayerPolicyMaintenanceRuntimeEntry entry, int currentDay)
	{
		return entry?.Effect != null
			&& IsPolicyEffectWithinDuration(entry.Effect)
			&& entry.Effect.MaintenanceChargeEnabled
			&& currentDay > entry.Effect.SubmittedDay;
	}

	private static bool IsPlayerPolicyMaintenanceSettlementDue(PlayerPolicyMaintenanceRuntimeEntry entry, int currentDay)
	{
		return IsPlayerPolicyMaintenanceDue(entry, currentDay)
			&& PlayerPolicyMaintenancePlanner.IsSettlementDue(
				entry.Effect.SubmittedDay,
				entry.Effect.LastMaintenanceSettlementDay,
				currentDay);
	}

	private void AddPlayerPolicyGoldIncomeReport(bool includeDescriptions, ref ExplainedNumber result)
	{
		string playerId = (Hero.MainHero?.StringId ?? string.Empty).Trim();
		if (playerId.Length == 0)
		{
			return;
		}
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = _policyEffectRuntimeIndex.GetContributions(
			PolicyEffectHook.DailyScheduler,
			PolicyEffectTargetKind.Hero,
			playerId);
		AddPlayerPolicyGoldIncomeContributions(contributions, includeDescriptions, ref result);
	}

	private static void AddPlayerPolicyGoldIncomeContributions(
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		float total = 0f;
		foreach (PolicyEffectRuntimeContribution contribution in contributions ?? Array.Empty<PolicyEffectRuntimeContribution>())
		{
			if (contribution == null
				|| !string.Equals(contribution.ModuleId, "heroGoldPerDay", StringComparison.Ordinal)
				|| contribution.Aggregation != PolicyEffectAggregationKind.IntegerDelta
				|| float.IsNaN(contribution.Value)
				|| float.IsInfinity(contribution.Value)
				|| contribution.Value <= 0.0001f)
			{
				continue;
			}
			if (includeDescriptions)
			{
				result.Add(contribution.Value, BuildPlayerPolicyIncomeExplanation(contribution.DisplayName));
			}
			else
			{
				total += contribution.Value;
			}
		}
		if (!includeDescriptions && total > 0.0001f && !float.IsInfinity(total))
		{
			result.Add(total, null);
		}
	}

	private static void AddPlayerPolicyMaintenanceTotal(int total, bool includeDescriptions, ref ExplainedNumber result)
	{
		if (total > 0)
		{
			result.Add(-total, includeDescriptions ? new TextObject("自定义政策维护费") : null);
		}
	}

	private static TextObject BuildPlayerPolicyIncomeExplanation(string policyName)
	{
		string name = (policyName ?? string.Empty).Replace("{", string.Empty).Replace("}", string.Empty).Trim();
		if (name.Length > 48)
		{
			name = name.Substring(0, 47).TrimEnd() + "…";
		}
		return new TextObject("《" + (name.Length == 0 ? "未命名政策" : name) + "》政策收入");
	}

	private static void PatchPolicySettlementModelMethod(Harmony harmony, object model, string methodName, Type[] argumentTypes, string postfixName)
	{
		System.Reflection.MethodInfo target = model == null ? null : AccessTools.Method(model.GetType(), methodName, argumentTypes);
		target = target?.GetDeclaredMember();
		if (target == null)
		{
			throw new MissingMethodException(model?.GetType().FullName ?? "(null)", methodName);
		}
		harmony.Patch(target, postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), postfixName));
	}

	private static void Patch_PolicyProsperityChange_Postfix(Town fortification, bool includeDescriptions, ref ExplainedNumber __result)
	{
		AddActivePolicySettlementEffects(fortification?.Settlement, PolicyEffectHook.SettlementProsperityDaily, includeDescriptions, ref __result);
	}

	private static void Patch_PolicyHearthChange_Postfix(Village village, bool includeDescriptions, ref ExplainedNumber __result)
	{
		AddActivePolicySettlementEffects(village?.Settlement, PolicyEffectHook.VillageHearthDaily, includeDescriptions, ref __result);
	}

	private static void Patch_PolicyFoodChange_Postfix(Town town, bool includeMarketStocks, bool includeDescriptions, ref ExplainedNumber __result)
	{
		AddActivePolicySettlementEffects(town?.Settlement, PolicyEffectHook.TownFoodDaily, includeDescriptions, ref __result);
	}

	private static void Patch_PolicyLoyaltyChange_Postfix(Town town, bool includeDescriptions, ref ExplainedNumber __result)
	{
		AddActivePolicySettlementEffects(town?.Settlement, PolicyEffectHook.TownLoyaltyDaily, includeDescriptions, ref __result);
	}

	private static void Patch_PolicySecurityChange_Postfix(Town town, bool includeDescriptions, ref ExplainedNumber __result)
	{
		AddActivePolicySettlementEffects(town?.Settlement, PolicyEffectHook.TownSecurityDaily, includeDescriptions, ref __result);
	}

	private static void Patch_PolicyMilitiaChange_Postfix(Settlement settlement, bool includeDescriptions, ref ExplainedNumber __result)
	{
		if (settlement?.Town != null)
		{
			AddActivePolicySettlementEffects(settlement, PolicyEffectHook.SettlementMilitiaDaily, includeDescriptions, ref __result);
		}
	}

	private static void Patch_PolicyVolunteerProductionProbability_Postfix(
		Hero hero,
		int index,
		Settlement settlement,
		ref float __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null
			|| hero?.VolunteerTypes == null
			|| index < 0
			|| index >= hero.VolunteerTypes.Length
			|| settlement == null
			|| string.IsNullOrWhiteSpace(settlement.StringId))
		{
			return;
		}
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions =
			behavior._policyEffectRuntimeIndex.GetContributions(
				PolicyEffectHook.VolunteerProductionProbability,
				PolicyEffectTargetKind.Settlement,
				settlement.StringId);
		__result = CalculatePolicyVolunteerProductionAdjustedProbability(__result, contributions);
	}

	internal static float CalculatePolicyVolunteerProductionAdjustedProbability(
		float originalProbability,
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions)
	{
		double totalPercent = 0d;
		for (int contributionIndex = 0; contributionIndex < (contributions?.Count ?? 0); contributionIndex++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[contributionIndex];
			if (contribution == null
				|| contribution.Aggregation != PolicyEffectAggregationKind.Additive)
			{
				continue;
			}
			totalPercent += contribution.Value;
		}
		if (double.IsNaN(totalPercent)
			|| double.IsInfinity(totalPercent)
			|| Math.Abs(totalPercent) <= 0.0001d)
		{
			return originalProbability;
		}
		double adjusted = (double)originalProbability * Math.Max(0d, 1d + totalPercent / 100d);
		return (float)Math.Max(0d, Math.Min(1d, adjusted));
	}

	private static void AddActivePolicySettlementEffects(
		Settlement settlement,
		PolicyEffectHook hook,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null || settlement == null)
		{
			return;
		}
		AddIndexedPolicyContributions(behavior, settlement, hook, includeDescriptions, ref result);
	}

	private static TextObject BuildPolicySettlementEffectExplanation(PolicyEffectRuntimeContribution contribution)
	{
		string policyName = (contribution?.DisplayName ?? "").Replace("{", "").Replace("}", "").Trim();
		if (policyName.Length > 48)
		{
			policyName = policyName.Substring(0, 47).TrimEnd() + "…";
		}
		return new TextObject("《" + (string.IsNullOrWhiteSpace(policyName) ? "未命名政策" : policyName) + "》");
	}

	private static void AddIndexedPolicyContributions(
		CustomPolicyBehavior behavior,
		Settlement settlement,
		PolicyEffectHook hook,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		if (behavior == null || settlement == null)
		{
			return;
		}

		string settlementId = settlement.StringId ?? string.Empty;
		AddIndexedAdditiveContributions(behavior._policyEffectRuntimeIndex, hook, PolicyEffectTargetKind.Settlement, settlementId, includeDescriptions, ref result);
		if (settlement.Town != null)
		{
			AddIndexedAdditiveContributions(behavior._policyEffectRuntimeIndex, hook, PolicyEffectTargetKind.Town, settlementId, includeDescriptions, ref result);
		}
		if (settlement.Village != null)
		{
			AddIndexedAdditiveContributions(behavior._policyEffectRuntimeIndex, hook, PolicyEffectTargetKind.Village, settlementId, includeDescriptions, ref result);
		}

		Clan ownerClan = settlement.OwnerClan ?? settlement.Village?.Bound?.OwnerClan;
		if (!string.IsNullOrWhiteSpace(ownerClan?.StringId))
		{
			AddIndexedAdditiveContributions(behavior._policyEffectRuntimeIndex, hook, PolicyEffectTargetKind.Clan, ownerClan.StringId, includeDescriptions, ref result);
		}
		Kingdom kingdom = ownerClan?.Kingdom ?? settlement.MapFaction as Kingdom;
		if (!string.IsNullOrWhiteSpace(kingdom?.StringId))
		{
			AddIndexedAdditiveContributions(behavior._policyEffectRuntimeIndex, hook, PolicyEffectTargetKind.Kingdom, kingdom.StringId, includeDescriptions, ref result);
		}
	}

	private static void AddIndexedAdditiveContributions(
		PolicyEffectRuntimeIndex index,
		PolicyEffectHook hook,
		PolicyEffectTargetKind targetKind,
		string targetId,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = index?.GetContributions(hook, targetKind, targetId);
		if (contributions == null)
		{
			return;
		}
		for (int contributionIndex = 0; contributionIndex < contributions.Count; contributionIndex++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[contributionIndex];
			if (contribution == null
				|| contribution.Aggregation != PolicyEffectAggregationKind.Additive
				|| Math.Abs(contribution.Value) <= 0.0001f)
			{
				continue;
			}
			if (includeDescriptions)
			{
				result.Add(contribution.Value, BuildPolicySettlementEffectExplanation(contribution));
			}
			else
			{
				result.Add(contribution.Value);
			}
		}
	}

	private static void Patch_PolicyTownTax_Postfix(Town town, bool includeDescriptions, ref ExplainedNumber __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		Settlement settlement = town?.Settlement;
		if (behavior == null || settlement == null)
		{
			return;
		}
		float originalTax = __result.ResultNumber;
		float baseTax = __result.BaseNumber;
		if (float.IsNaN(originalTax) || float.IsInfinity(originalTax) || originalTax <= PolicyTownTaxEpsilon
			|| float.IsNaN(baseTax) || float.IsInfinity(baseTax) || Math.Abs(baseTax) <= PolicyTownTaxEpsilon)
		{
			return;
		}
		bool applied = AddIndexedTownTaxContributions(behavior, settlement, originalTax, baseTax, includeDescriptions, ref __result);
		if (applied)
		{
			__result.LimitMin(0f);
		}
	}

	private static void Patch_PolicyConstructionPower_Postfix(Town town, bool includeDescriptions, ref ExplainedNumber __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		Settlement settlement = town?.Settlement;
		if (behavior == null || settlement == null)
		{
			return;
		}
		float baseConstructionPower = __result.BaseNumber;
		float originalConstructionPower = __result.ResultNumber;
		if (float.IsNaN(originalConstructionPower) || float.IsInfinity(originalConstructionPower) || originalConstructionPower <= 0.0001f
			|| float.IsNaN(baseConstructionPower) || float.IsInfinity(baseConstructionPower) || Math.Abs(baseConstructionPower) <= 0.0001f)
		{
			return;
		}
		bool applied = AddIndexedConstructionContributions(behavior, settlement, baseConstructionPower, includeDescriptions, ref __result);
		if (applied)
		{
			__result.LimitMin(0f);
		}
	}

	private static void Patch_PolicyConstructionPowerWithoutBoost_Postfix(Town town, ref int __result)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		Settlement settlement = town?.Settlement;
		if (behavior == null || settlement == null || __result <= 0)
		{
			return;
		}
		double totalConstructionPowerDelta = SumIndexedConstructionContributions(behavior, settlement);
		if (double.IsNaN(totalConstructionPowerDelta) || double.IsInfinity(totalConstructionPowerDelta) || Math.Abs(totalConstructionPowerDelta) <= 0.0001d)
		{
			return;
		}
		double adjusted = Math.Max(0d, __result + totalConstructionPowerDelta);
		if (double.IsNaN(adjusted) || adjusted <= 0d)
		{
			__result = 0;
			return;
		}
		__result = adjusted >= int.MaxValue
			? int.MaxValue
			: Math.Max(0, (int)Math.Round(adjusted, MidpointRounding.AwayFromZero));
	}

	private static bool AddIndexedTownTaxContributions(
		CustomPolicyBehavior behavior,
		Settlement settlement,
		float originalTax,
		float baseTax,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		bool applied = false;
		string settlementId = settlement?.StringId ?? string.Empty;
		applied |= AddIndexedTownTaxContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Settlement, settlementId, originalTax, baseTax, includeDescriptions, ref result);
		applied |= AddIndexedTownTaxContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Town, settlementId, originalTax, baseTax, includeDescriptions, ref result);
		Clan ownerClan = settlement?.OwnerClan;
		if (!string.IsNullOrWhiteSpace(ownerClan?.StringId))
		{
			applied |= AddIndexedTownTaxContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Clan, ownerClan.StringId, originalTax, baseTax, includeDescriptions, ref result);
		}
		Kingdom kingdom = ownerClan?.Kingdom ?? settlement?.MapFaction as Kingdom;
		if (!string.IsNullOrWhiteSpace(kingdom?.StringId))
		{
			applied |= AddIndexedTownTaxContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Kingdom, kingdom.StringId, originalTax, baseTax, includeDescriptions, ref result);
		}
		return applied;
	}

	private static bool AddIndexedTownTaxContributionsForTarget(
		PolicyEffectRuntimeIndex index,
		PolicyEffectTargetKind targetKind,
		string targetId,
		float originalTax,
		float baseTax,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		bool applied = false;
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = index?.GetContributions(PolicyEffectHook.TownTaxIncome, targetKind, targetId);
		if (contributions == null)
		{
			return false;
		}
		for (int contributionIndex = 0; contributionIndex < contributions.Count; contributionIndex++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[contributionIndex];
			if (contribution == null
				|| contribution.Aggregation != PolicyEffectAggregationKind.PercentPoints
				|| Math.Abs(contribution.Value) <= PolicyTownTaxEpsilon)
			{
				continue;
			}
			double adjustedFactor = ((double)originalTax / baseTax) * ((double)contribution.Value / 100.0);
			double combinedFactor = (double)result.SumOfFactors + adjustedFactor;
			double projectedTax = (double)result.BaseNumber + (double)result.BaseNumber * combinedFactor;
			if (double.IsNaN(adjustedFactor) || double.IsInfinity(adjustedFactor)
				|| adjustedFactor > float.MaxValue || adjustedFactor < -float.MaxValue
				|| double.IsNaN(combinedFactor) || double.IsInfinity(combinedFactor)
				|| combinedFactor > float.MaxValue || combinedFactor < -float.MaxValue
				|| double.IsNaN(projectedTax) || double.IsInfinity(projectedTax)
				|| projectedTax > float.MaxValue || projectedTax < -float.MaxValue)
			{
				continue;
			}
			result.AddFactor((float)adjustedFactor, includeDescriptions ? BuildPolicyTownTaxEffectExplanation(contribution) : null);
			applied = true;
		}
		return applied;
	}

	private static bool AddIndexedConstructionContributions(
		CustomPolicyBehavior behavior,
		Settlement settlement,
		float baseConstructionPower,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		bool applied = false;
		string settlementId = settlement?.StringId ?? string.Empty;
		applied |= AddIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Settlement, settlementId, baseConstructionPower, includeDescriptions, ref result);
		applied |= AddIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Town, settlementId, baseConstructionPower, includeDescriptions, ref result);
		Clan ownerClan = settlement?.OwnerClan;
		if (!string.IsNullOrWhiteSpace(ownerClan?.StringId))
		{
			applied |= AddIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Clan, ownerClan.StringId, baseConstructionPower, includeDescriptions, ref result);
		}
		Kingdom kingdom = ownerClan?.Kingdom ?? settlement?.MapFaction as Kingdom;
		if (!string.IsNullOrWhiteSpace(kingdom?.StringId))
		{
			applied |= AddIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Kingdom, kingdom.StringId, baseConstructionPower, includeDescriptions, ref result);
		}
		return applied;
	}

	private static bool AddIndexedConstructionContributionsForTarget(
		PolicyEffectRuntimeIndex index,
		PolicyEffectTargetKind targetKind,
		string targetId,
		float baseConstructionPower,
		bool includeDescriptions,
		ref ExplainedNumber result)
	{
		bool applied = false;
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = index?.GetContributions(PolicyEffectHook.SettlementConstructionDaily, targetKind, targetId);
		if (contributions == null)
		{
			return false;
		}
		for (int contributionIndex = 0; contributionIndex < contributions.Count; contributionIndex++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[contributionIndex];
			float constructionPowerDailyDelta = contribution?.Value ?? 0f;
			if (contribution == null
				|| contribution.Aggregation != PolicyEffectAggregationKind.Additive
				|| Math.Abs(constructionPowerDailyDelta) <= 0.0001f)
			{
				continue;
			}
			double adjustedFactor = (double)constructionPowerDailyDelta / baseConstructionPower;
			double combinedFactor = (double)result.SumOfFactors + adjustedFactor;
			double projectedConstructionPower = (double)result.BaseNumber + (double)result.BaseNumber * combinedFactor;
			if (double.IsNaN(adjustedFactor) || double.IsInfinity(adjustedFactor)
				|| adjustedFactor > float.MaxValue || adjustedFactor < -float.MaxValue
				|| double.IsNaN(combinedFactor) || double.IsInfinity(combinedFactor)
				|| combinedFactor > float.MaxValue || combinedFactor < -float.MaxValue
				|| double.IsNaN(projectedConstructionPower) || double.IsInfinity(projectedConstructionPower)
				|| projectedConstructionPower > float.MaxValue || projectedConstructionPower < -float.MaxValue)
			{
				continue;
			}
			result.AddFactor((float)adjustedFactor, includeDescriptions ? BuildPolicyConstructionSpeedEffectExplanation(contribution) : null);
			applied = true;
		}
		return applied;
	}

	private static double SumIndexedConstructionContributions(CustomPolicyBehavior behavior, Settlement settlement)
	{
		double total = 0d;
		string settlementId = settlement?.StringId ?? string.Empty;
		total += SumIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Settlement, settlementId);
		total += SumIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Town, settlementId);
		Clan ownerClan = settlement?.OwnerClan;
		if (!string.IsNullOrWhiteSpace(ownerClan?.StringId))
		{
			total += SumIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Clan, ownerClan.StringId);
		}
		Kingdom kingdom = ownerClan?.Kingdom ?? settlement?.MapFaction as Kingdom;
		if (!string.IsNullOrWhiteSpace(kingdom?.StringId))
		{
			total += SumIndexedConstructionContributionsForTarget(behavior?._policyEffectRuntimeIndex, PolicyEffectTargetKind.Kingdom, kingdom.StringId);
		}
		return total;
	}

	private static double SumIndexedConstructionContributionsForTarget(
		PolicyEffectRuntimeIndex index,
		PolicyEffectTargetKind targetKind,
		string targetId)
	{
		double total = 0d;
		IReadOnlyList<PolicyEffectRuntimeContribution> contributions = index?.GetContributions(PolicyEffectHook.SettlementConstructionDaily, targetKind, targetId);
		if (contributions == null)
		{
			return total;
		}
		for (int contributionIndex = 0; contributionIndex < contributions.Count; contributionIndex++)
		{
			PolicyEffectRuntimeContribution contribution = contributions[contributionIndex];
			if (contribution != null && contribution.Aggregation == PolicyEffectAggregationKind.Additive)
			{
				total += contribution.Value;
			}
		}
		return total;
	}

	private static TextObject BuildPolicyTownTaxEffectExplanation(PolicyEffectRuntimeContribution contribution)
	{
		string policyName = (contribution?.DisplayName ?? string.Empty).Replace("{", string.Empty).Replace("}", string.Empty).Trim();
		if (policyName.Length > 40)
		{
			policyName = policyName.Substring(0, 39).TrimEnd() + "…";
		}
		return new TextObject("《" + (string.IsNullOrWhiteSpace(policyName) ? "未命名政策" : policyName) + "》税收 " + FormatSigned(contribution?.Value ?? 0f) + "%");
	}

	private static TextObject BuildPolicyConstructionSpeedEffectExplanation(PolicyEffectRuntimeContribution contribution)
	{
		string policyName = (contribution?.DisplayName ?? string.Empty).Replace("{", string.Empty).Replace("}", string.Empty).Trim();
		if (policyName.Length > 40)
		{
			policyName = policyName.Substring(0, 39).TrimEnd() + "…";
		}
		return new TextObject("《" + (string.IsNullOrWhiteSpace(policyName) ? "未命名政策" : policyName) + "》建造力 " + FormatSigned(contribution?.Value ?? 0f));
	}

	private PolicyApplicationResult ApplyPolicyEffects(PolicyDraftRequest request, PolicyPostprocessResult postprocess)
	{
		PolicyApplicationResult result = new PolicyApplicationResult();
		bool isPermanentPlayerEffect = IsPermanentPlayerPolicyEffect(request);
		if (postprocess?.Effects == null || postprocess.Effects.Count == 0)
		{
            result.NoticeLines.Add("没有明确的数值变化。");
			return result;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		foreach (PolicyEffectDto effect in postprocess.Effects.Where(x => x != null))
		{
			Kingdom targetKingdom = ResolveTargetKingdom(effect, playerKingdom);
			if (targetKingdom == null)
			{
                result.NoticeLines.Add("跳过未知目标：" + (effect.TargetKingdomId ?? effect.TargetKingdomName ?? "未指定"));
				continue;
			}
			AppliedKingdomEffect applied = BuildContinuousEffectForKingdom(targetKingdom, effect);
			applied.IsPermanentEffect = isPermanentPlayerEffect;
			if (!applied.IsPermanentEffect && applied.DurationDays <= 0)
			{
				result.NoticeLines.Add("跳过持续时间无效的效果：" + GetKingdomName(targetKingdom));
				continue;
			}
			if (!HasAnyDailyDelta(applied))
			{
                result.NoticeLines.Add(GetKingdomName(targetKingdom) + "没有每日数值变化，但政策有效期仍保留 " + applied.DurationDays.ToString(CultureInfo.InvariantCulture) + " 天。");
			}
			result.AppliedEffectCount++;
			result.KingdomEffects.Add(applied);
		}
		if (result.KingdomEffects.Count == 0 && result.NoticeLines.Count == 0)
		{
            result.NoticeLines.Add("政策未产生可落地的数值变化。");
		}
		return result;
	}

	private PolicyApplicationResult ApplyLocalPolicyEffects(PolicyDraftRequest request, PolicyPostprocessResult postprocess, List<Settlement> selectedFiefs)
	{
		PolicyApplicationResult result = new PolicyApplicationResult();
		bool isPermanentPlayerEffect = IsPermanentPlayerPolicyEffect(request);
		List<Settlement> fiefs = (selectedFiefs ?? new List<Settlement>()).Where(IsPlayerOwnedLocalPolicyFief).ToList();
		List<PolicyEffectDto> effects = postprocess?.Effects?.Where(x => x != null).ToList() ?? new List<PolicyEffectDto>();
		if (effects.Any(x => !string.IsNullOrWhiteSpace(x.TargetHandle)))
		{
			Dictionary<string, PolicyTargetHandleSaveData> handleByKey = NormalizePolicyTargetHandles(request?.TargetHandles)
				.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
			foreach (PolicyEffectDto effect in effects)
			{
				string targetKey = (effect.TargetHandle ?? "").Trim();
				if (!handleByKey.TryGetValue(targetKey, out PolicyTargetHandleSaveData target)
					|| !IsPolicyTargetHandleAllowedForRequest(request, target))
				{
                    result.NoticeLines.Add("跳过未知地方目标句柄：" + targetKey);
					continue;
				}
				bool isSource = string.Equals(target.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase);
				int effectDuration = isPermanentPlayerEffect ? 0 : request?.ManualDurationDays > 0 ? request.ManualDurationDays : effect.DurationDays;
				if (!isPermanentPlayerEffect && effectDuration <= 0)
				{
					result.NoticeLines.Add("跳过持续时间无效的地方目标：" + targetKey);
					continue;
				}
				List<Settlement> targetSettlements = isSource
					? ExpandLocalPolicySettlements(fiefs)
					: ResolveLocalPolicyHandleSettlements(target, fiefs, request);
				AppliedKingdomEffect applied = BuildAppliedLocalPolicyEffect(
					request,
					effect,
					isSource ? LocalPolicyTargetScopeSource : LocalPolicyTargetScopeMentioned,
					isSource ? fiefs : new List<Settlement>(),
					targetSettlements,
					effectDuration);
				applied.IsPermanentEffect = isPermanentPlayerEffect;
				applied.TargetHandle = targetKey;
				applied.TargetLabel = BuildLocalPolicyEffectTargetLabel(
					isSource ? LocalPolicyTargetScopeSource : LocalPolicyTargetScopeMentioned,
					targetKey,
					target.DisplayName,
					string.Equals(target.Kind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase)
						|| (string.Equals(target.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase) && !target.FollowCurrentRulingClan)
						? new[] { target.EntityId }
						: Array.Empty<string>(),
					string.Equals(target.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase)
						? new[] { target.EntityId }
						: Array.Empty<string>(),
					target.FollowCurrentRulingClan,
					targetSettlements);
				if (!isSource)
				{
					if (string.Equals(target.Kind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase)
						|| (string.Equals(target.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase) && !target.FollowCurrentRulingClan))
					{
						applied.TargetClanIds = NormalizeIdList(new[] { target.EntityId });
					}
					else if (string.Equals(target.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase))
					{
						applied.DirectTargetSettlementIds = NormalizeIdList(new[] { target.EntityId });
					}
					applied.FollowCurrentRulingClan = target.FollowCurrentRulingClan;
				}
				applied.KingdomName = applied.TargetLabel;
				result.KingdomEffects.Add(applied);
			}
			result.AppliedEffectCount = result.KingdomEffects.Count;
			if (result.KingdomEffects.Count > 0 && result.KingdomEffects.All(x => !HasAnyDailyDelta(x)))
			{
                result.NoticeLines.Add("全部每日数值为 0：政策仍会计时、显示反馈并进入地方记录，费用仍按本次实际投入结算。");
			}
			return result;
		}
		PolicyEffectDto sourceEffect = effects.FirstOrDefault(x => string.Equals(NormalizeLocalPolicyTargetScope(x.TargetScope), LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase));
		if (sourceEffect == null || fiefs.Count <= 0)
		{
			return result;
		}
		int duration = isPermanentPlayerEffect ? 0 : request?.ManualDurationDays > 0 ? request.ManualDurationDays : sourceEffect.DurationDays;
		if (!isPermanentPlayerEffect && duration <= 0)
		{
			return result;
		}
		List<Settlement> sourceSettlements = ExpandLocalPolicySettlements(fiefs);
		AppliedKingdomEffect sourceApplied = BuildAppliedLocalPolicyEffect(
			request,
			sourceEffect,
			LocalPolicyTargetScopeSource,
			fiefs,
			sourceSettlements,
			duration);
		sourceApplied.IsPermanentEffect = isPermanentPlayerEffect;
		sourceApplied.TargetLabel = BuildLocalPolicyEffectTargetLabel(
			LocalPolicyTargetScopeSource,
			"S",
            "发布地",
			Array.Empty<string>(),
			Array.Empty<string>(),
			false,
			sourceSettlements);
		sourceApplied.KingdomName = sourceApplied.TargetLabel;
		result.KingdomEffects.Add(sourceApplied);
		if (HasLocalPolicyMentionSelectors(request))
		{
			PolicyEffectDto mentionedEffect = effects.FirstOrDefault(x => string.Equals(NormalizeLocalPolicyTargetScope(x.TargetScope), LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase));
			if (mentionedEffect != null)
			{
				List<Settlement> mentionedSettlements = ResolveLocalMentionedPolicySettlements(
					request.LocalMentionedClanIds,
					request.LocalMentionedSettlementIds,
					request.LocalMentionedCurrentRulingClan,
					fiefs);
				AppliedKingdomEffect mentionedApplied = BuildAppliedLocalPolicyEffect(
					request,
					mentionedEffect,
					LocalPolicyTargetScopeMentioned,
					new List<Settlement>(),
					mentionedSettlements,
					duration);
				mentionedApplied.TargetClanIds = NormalizeIdList(request.LocalMentionedClanIds);
				mentionedApplied.DirectTargetSettlementIds = NormalizeIdList(request.LocalMentionedSettlementIds);
				mentionedApplied.FollowCurrentRulingClan = request.LocalMentionedCurrentRulingClan;
				mentionedApplied.TargetLabel = BuildLocalPolicyEffectTargetLabel(
					LocalPolicyTargetScopeMentioned,
					"legacy-mentioned",
					"本国提及目标",
					mentionedApplied.TargetClanIds,
					mentionedApplied.DirectTargetSettlementIds,
					mentionedApplied.FollowCurrentRulingClan,
					mentionedSettlements);
				mentionedApplied.KingdomName = mentionedApplied.TargetLabel;
				result.KingdomEffects.Add(mentionedApplied);
			}
		}
		result.AppliedEffectCount = result.KingdomEffects.Count;
		if (result.KingdomEffects.All(x => !HasAnyDailyDelta(x)))
		{
            result.NoticeLines.Add("全部每日数值为 0：政策仍会计时、显示反馈并进入地方记录，费用仍按本次实际投入结算。");
		}
		return result;
	}

	private static string BuildPolicyTargetDisplayLabel(PolicyTargetHandleSaveData target, IEnumerable<Settlement> settlements)
	{
		string baseLabel = CleanPolicyDisplayText(target?.DisplayName ?? target?.Key ?? "目标");
		List<string> names = (settlements ?? Enumerable.Empty<Settlement>())
			.Where(x => x != null)
			.Select(x => x.Name?.ToString() ?? x.StringId)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (names.Count <= 0)
		{
			return baseLabel;
		}
		if (names.Count == 1 && string.Equals(names[0], baseLabel, StringComparison.OrdinalIgnoreCase))
		{
			return baseLabel;
		}
		const int visibleCount = 6;
        string visible = string.Join("、", names.Take(visibleCount));
		if (names.Count > visibleCount)
		{
            visible += "，另" + (names.Count - visibleCount).ToString(CultureInfo.InvariantCulture) + "处";
		}
		string prefix = string.Equals(target?.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase)
            ? "发布地"
			: baseLabel;
        return prefix + "（" + visible + "）";
	}

	private string BuildLocalPolicyEffectTargetLabel(
		string targetScope,
		string targetHandle,
		string storedLabel,
		IEnumerable<string> clanIds,
		IEnumerable<string> directSettlementIds,
		bool followCurrentRulingClan,
		IEnumerable<Settlement> settlements)
	{
		if (string.Equals(NormalizeLocalPolicyTargetScope(targetScope), LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase))
		{
			return BuildPolicyTargetDisplayLabel(
				new PolicyTargetHandleSaveData { Kind = PolicyTargetKindSource, DisplayName = "发布地及附属村庄" },
				settlements);
		}
		List<string> directIds = NormalizeIdList(directSettlementIds);
		List<string> clans = NormalizeIdList(clanIds);
		if (directIds.Count > 0 && clans.Count == 0 && !followCurrentRulingClan)
		{
			List<string> directNames = directIds
				.Select(id => ResolveSettlementById(id)?.Name?.ToString() ?? id)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			return "指定定居点：" + (directNames.Count <= 0
                ? "当前无符合条件目标"
                : string.Join("、", directNames.Take(6))
                    + (directNames.Count > 6 ? "，另" + (directNames.Count - 6).ToString(CultureInfo.InvariantCulture) + "处" : ""));
		}
		string baseLabel;
		if (followCurrentRulingClan)
		{
            baseLabel = "当前统治家族当前领地及附属村庄";
		}
		else if (clans.Count > 0)
		{
			List<string> clanNames = clans
				.Select(id => ResolveClanById(id)?.Name?.ToString() ?? id)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
            baseLabel = (clanNames.Count <= 0 ? "指定氏族" : string.Join("、", clanNames)) + "当前领地及附属村庄";
		}
		else
		{
			baseLabel = FirstNonEmpty(storedLabel, targetHandle, "本国目标");
		}
		return BuildPolicyTargetDisplayLabel(
			new PolicyTargetHandleSaveData { DisplayName = baseLabel },
			settlements);
	}

	private static AppliedKingdomEffect BuildAppliedLocalPolicyEffect(
		PolicyDraftRequest request,
		PolicyEffectDto effect,
		string targetScope,
		IEnumerable<Settlement> targetFiefs,
		IEnumerable<Settlement> targetSettlements,
		int duration)
	{
		List<Settlement> fiefs = (targetFiefs ?? Enumerable.Empty<Settlement>()).Where(x => x != null).ToList();
		List<Settlement> settlements = (targetSettlements ?? Enumerable.Empty<Settlement>())
			.Where(x => x != null)
			.GroupBy(x => x.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.ToList();
		bool isMentioned = string.Equals(targetScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase);
		return new AppliedKingdomEffect
		{
			ModuleEffects = CreatePolicyEffectSaveDataList(effect?.PreparedModuleEffect),
			EffectId = Guid.NewGuid().ToString("N"),
			ScopeKind = PolicyScopeLocal,
			LocalTargetScope = isMentioned ? LocalPolicyTargetScopeMentioned : LocalPolicyTargetScopeSource,
			TargetFiefIds = fiefs.Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			TargetSettlementIds = settlements.Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			KingdomId = request?.PlayerKingdomId ?? "",
			KingdomName = isMentioned
				? BuildPolicyTargetDisplayLabel(
					new PolicyTargetHandleSaveData { DisplayName = "本国提及目标" },
					settlements)
                : "发布地（" + string.Join("、", fiefs.Select(x => x.Name?.ToString() ?? x.StringId)) + "）",
			TownCount = settlements.Count(x => x?.Town != null),
			VillageCount = settlements.Count(x => x?.Village != null),
			DurationDays = duration,
			RemainingDays = duration,
			Reason = (effect?.Reason ?? "").Trim()
		};
	}

	private AppliedKingdomEffect BuildContinuousEffectForKingdom(Kingdom kingdom, PolicyEffectDto effect)
	{
		AppliedKingdomEffect applied = new AppliedKingdomEffect
		{
			ModuleEffects = CreatePolicyEffectSaveDataList(effect?.PreparedModuleEffect),
			EffectId = Guid.NewGuid().ToString("N"),
			TargetHandle = effect?.TargetHandle ?? "",
			TargetLabel = GetKingdomName(kingdom),
			KingdomId = kingdom?.StringId ?? "",
			KingdomName = GetKingdomName(kingdom),
			DurationDays = ClampPolicyEffectDurationDays(effect?.DurationDays ?? 0),
			RemainingDays = ClampPolicyEffectDurationDays(effect?.DurationDays ?? 0),
			Reason = (effect.Reason ?? "").Trim()
		};
		List<Settlement> settlements = GetKingdomSettlements(kingdom);
		applied.TownCount = settlements.Count(s => s?.Town != null);
		applied.VillageCount = settlements.Count(s => s?.Village != null);
		return applied;
	}

	private void OnDailyTick()
	{
		PolicyTargetSemanticRouter.MarkDynamicDirty();
		ReconcilePendingVassalExternalCommits();
		if (!_policyEffectRuntimeIndexInitialized)
		{
			RebuildActivePolicyEffectRuntimeIndex();
		}
		EnsureActivePolicyEffectWorkScheduled(GetCurrentCampaignDay());
	}

	private static bool IsPolicyEffectWithinDuration(ActivePolicyEffectSaveData effect)
	{
		return effect != null && !effect.Ended && (effect.IsPermanentEffect || effect.RemainingDays > 0);
	}

	private static bool IsPolicyEffectMaintenanceFunded(ActivePolicyEffectSaveData effect)
	{
		return effect != null
			&& (!effect.MaintenanceChargeEnabled
				|| effect.DailyMaintenanceGoldCost <= 0
				|| effect.MaintenanceFunded);
	}

	private ActivePolicyEffectSaveData GetActivePolicyEffectForWork(string effectId, string raw)
	{
		if (_quarantinedActivePolicyEffectIds.Contains(effectId ?? string.Empty))
		{
			return null;
		}
		if (_activePolicyEffectRuntimeCache.TryGetValue(effectId, out ActivePolicyEffectRuntimeEntry entry)
			&& entry?.Effect != null
			&& string.Equals(entry.Raw, raw, StringComparison.Ordinal))
		{
			return entry.Effect;
		}
		ActivePolicyEffectSaveData effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(raw);
		_activePolicyEffectRuntimeCache[effectId] = new ActivePolicyEffectRuntimeEntry
		{
			Raw = raw,
			Effect = effect
		};
		RefreshPlayerPolicyMaintenanceRuntimeIndex(effect);
		return effect;
	}

	// Load wiring must call this once after _activePolicyEffects has been restored. Model hooks
	// intentionally never rebuild the index, deserialize JSON, or scan the active-effect store.
	private void RebuildActivePolicyEffectRuntimeIndex()
	{
		List<PolicyEffectRuntimeContribution> contributions = new List<PolicyEffectRuntimeContribution>();
		_policyEffectDailyRuntimePlans.Clear();
		_policyEffectPendingDailyCompensationEffectIds.Clear();
		_policyTargetStructureDependencyEffectIds.Clear();
		_policyTargetRelationDependencyEffectIds.Clear();
		_legacyPolicyTargetRefreshEffectIds.Clear();
		_playerPolicyMaintenanceRuntimeIndex.Clear();
		_playerPolicyMaintenanceSnapshotDirty = true;
		foreach (KeyValuePair<string, string> item in _activePolicyEffects)
		{
			if (_quarantinedActivePolicyEffectIds.Contains(item.Key))
			{
				continue;
			}
			try
			{
				ActivePolicyEffectSaveData effect = GetActivePolicyEffectForWork(item.Key, item.Value);
				if (PolicyEffectActivationCoordinator.HasPendingDailyCompensation(effect?.ModuleEffects))
				{
					_policyEffectPendingDailyCompensationEffectIds.Add(item.Key);
				}
				if (!IsPolicyEffectWithinDuration(effect))
				{
					continue;
				}
				RefreshPlayerPolicyMaintenanceRuntimeIndex(effect);
				RefreshPolicyTargetDependencyIndex(item.Key, effect);
				if (IsPolicyEffectMaintenanceFunded(effect))
				{
					contributions.AddRange(BuildActivePolicyEffectRuntimeContributions(effect));
				}
				RebuildDailyPolicyEffectRuntimePlan(effect);
			}
			catch (Exception ex)
			{
				QuarantineActivePolicyEffect(item.Key, item.Value, "runtime-index: " + ex.Message);
			}
		}
		_policyEffectRuntimeIndex.Rebuild(contributions);
		_policyEffectRuntimeIndexInitialized = true;
		PolicySystemLog.Write("Effect", "runtime-index-rebuilt", "activeEffects=" + _activePolicyEffects.Count.ToString(CultureInfo.InvariantCulture)
			+ " contributions=" + _policyEffectRuntimeIndex.ContributionCount.ToString(CultureInfo.InvariantCulture)
			+ " structureVersion=" + _policyEffectRuntimeIndex.StructureVersion.ToString(CultureInfo.InvariantCulture));
	}

	private void ResetActivePolicyEffectRuntimeIndex()
	{
		_policyEffectRuntimeIndex.Clear();
		_policyEffectDailyRuntimePlans.Clear();
		_policyEffectDailyPersistenceTransactions.Clear();
		_policyEffectPendingDailyCompensationEffectIds.Clear();
		_policyTargetStructureDependencyEffectIds.Clear();
		_policyTargetRelationDependencyEffectIds.Clear();
		_legacyPolicyTargetRefreshEffectIds.Clear();
		_policyEffectRuntimeIndexInitialized = false;
		_playerPolicyMaintenanceRuntimeIndex.Clear();
		_playerPolicyMaintenanceSortedSnapshot = Array.Empty<PlayerPolicyMaintenanceRuntimeEntry>();
		_playerPolicyMaintenanceSnapshotDirty = true;
	}

	private void RefreshPlayerPolicyMaintenanceRuntimeIndex(ActivePolicyEffectSaveData effect)
	{
		string effectId = (effect?.EffectId ?? string.Empty).Trim();
		if (effectId.Length == 0)
		{
			return;
		}
		int dailyCost = Math.Max(0, effect.DailyMaintenanceGoldCost);
		if (!effect.MaintenanceChargeEnabled || dailyCost <= 0 || !IsPolicyEffectWithinDuration(effect))
		{
			if (_playerPolicyMaintenanceRuntimeIndex.Remove(effectId))
			{
				_playerPolicyMaintenanceSnapshotDirty = true;
			}
			return;
		}
		if (!_playerPolicyMaintenanceRuntimeIndex.TryGetValue(effectId, out PlayerPolicyMaintenanceRuntimeEntry entry))
		{
			entry = new PlayerPolicyMaintenanceRuntimeEntry { EffectId = effectId };
			_playerPolicyMaintenanceRuntimeIndex.Add(effectId, entry);
			_playerPolicyMaintenanceSnapshotDirty = true;
		}
		if (entry.SubmittedDay != effect.SubmittedDay
			|| entry.CreatedUtcTicks != effect.CreatedUtcTicks
			|| entry.DailyCost != dailyCost
			|| !string.Equals(entry.PolicyName, effect.PolicyName, StringComparison.Ordinal))
		{
			_playerPolicyMaintenanceSnapshotDirty = true;
		}
		entry.RecordId = effect.RecordId ?? string.Empty;
		entry.PolicyName = effect.PolicyName ?? string.Empty;
		entry.SubmittedDay = effect.SubmittedDay;
		entry.CreatedUtcTicks = effect.CreatedUtcTicks;
		entry.DailyCost = dailyCost;
		entry.Effect = effect;
	}

	private PlayerPolicyMaintenanceRuntimeEntry[] GetPlayerPolicyMaintenanceSortedSnapshot()
	{
		if (!_playerPolicyMaintenanceSnapshotDirty)
		{
			return _playerPolicyMaintenanceSortedSnapshot;
		}
		PlayerPolicyMaintenanceRuntimeEntry[] snapshot = _playerPolicyMaintenanceRuntimeIndex.Values.ToArray();
		Array.Sort(snapshot, delegate(PlayerPolicyMaintenanceRuntimeEntry left, PlayerPolicyMaintenanceRuntimeEntry right)
		{
			int order = (left?.SubmittedDay ?? 0).CompareTo(right?.SubmittedDay ?? 0);
			if (order != 0) return order;
			order = (left?.CreatedUtcTicks ?? 0L).CompareTo(right?.CreatedUtcTicks ?? 0L);
			if (order != 0) return order;
			return string.Compare(left?.EffectId ?? string.Empty, right?.EffectId ?? string.Empty, StringComparison.Ordinal);
		});
		_playerPolicyMaintenanceSortedSnapshot = snapshot;
		_playerPolicyMaintenanceSnapshotDirty = false;
		return snapshot;
	}

	private void UpdatePlayerPolicyMaintenanceRecord(ActivePolicyEffectSaveData activeEffect)
	{
		if (activeEffect == null || string.IsNullOrWhiteSpace(activeEffect.RecordId))
		{
			return;
		}
		string recordId = activeEffect.RecordId.Trim();
		if (IsLocalActivePolicyEffect(activeEffect) && _localPolicyRecords.TryGetValue(recordId, out string localRaw))
		{
			try
			{
				LocalPolicyRecordSaveData local = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(localRaw));
				if (local != null)
				{
					local.TotalMaintenancePaidGold = activeEffect.TotalMaintenancePaidGold;
					local.MaintenanceFunded = activeEffect.MaintenanceFunded;
					local.LastMaintenanceSettlementDay = activeEffect.LastMaintenanceSettlementDay;
					local.LastEffectProcessedDay = activeEffect.LastEffectProcessedDay;
					_localPolicyRecords[recordId] = JsonConvert.SerializeObject(local);
				}
			}
			catch (Exception ex)
			{
				PolicyDebugLog("maintenance-record-sync-failed", "recordId=" + recordId + " error=" + ex.Message);
			}
			return;
		}
		if (_policyRecordHistory.TryGetValue(recordId, out string policyRaw))
		{
			try
			{
				PolicyRecordSaveData policy = JsonConvert.DeserializeObject<PolicyRecordSaveData>(policyRaw);
				if (policy != null)
				{
					policy.TotalMaintenancePaidGold = activeEffect.TotalMaintenancePaidGold;
					policy.MaintenanceFunded = activeEffect.MaintenanceFunded;
					policy.LastMaintenanceSettlementDay = activeEffect.LastMaintenanceSettlementDay;
					policy.LastEffectProcessedDay = activeEffect.LastEffectProcessedDay;
					_policyRecordHistory[recordId] = JsonConvert.SerializeObject(policy);
				}
			}
			catch (Exception ex)
			{
				PolicyDebugLog("maintenance-record-sync-failed", "recordId=" + recordId + " error=" + ex.Message);
			}
		}
	}

	private static void NotifyPlayerPolicyMaintenanceStateChanged(ActivePolicyEffectSaveData effect, bool funded)
	{
		string name = string.IsNullOrWhiteSpace(effect?.PolicyName) ? "未命名政策" : effect.PolicyName.Trim();
		InformationManager.DisplayMessage(new InformationMessage(
			funded
				? "《" + name + "》维护费已恢复支付，数值效果恢复生效。"
				: "《" + name + "》维护费不足，今日数值效果暂停；下一日会自动重试。",
			funded ? Colors.Green : Colors.Yellow));
	}

	private void RefreshActivePolicyEffectRuntimeIndex(ActivePolicyEffectSaveData effect)
	{
		if (effect == null || string.IsNullOrWhiteSpace(effect.EffectId) || !IsPolicyEffectWithinDuration(effect))
		{
			_policyEffectRuntimeIndex.RemoveInstance(effect?.EffectId);
			if (!string.IsNullOrWhiteSpace(effect?.EffectId))
			{
				_policyEffectDailyRuntimePlans.Remove(effect.EffectId);
			}
			return;
		}
		_policyEffectRuntimeIndex.ReplaceInstance(effect.EffectId,
			IsPolicyEffectMaintenanceFunded(effect)
				? BuildActivePolicyEffectRuntimeContributions(effect)
				: Array.Empty<PolicyEffectRuntimeContribution>());
		RebuildDailyPolicyEffectRuntimePlan(effect);
		_policyEffectRuntimeIndexInitialized = true;
	}

	private static List<PolicyEffectRuntimeContribution> BuildActivePolicyEffectRuntimeContributions(ActivePolicyEffectSaveData effect)
	{
		return effect?.ModuleEffects == null || effect.ModuleEffects.Count == 0
			? new List<PolicyEffectRuntimeContribution>()
			: BuildModulePolicyEffectRuntimeContributions(effect);
	}

	private static List<PolicyEffectRuntimeContribution> BuildModulePolicyEffectRuntimeContributions(ActivePolicyEffectSaveData effect)
	{
		List<PolicyEffectRuntimeContribution> result = new List<PolicyEffectRuntimeContribution>();
		foreach (PolicyEffectInstanceSaveData instance in effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
		{
			if (instance == null
				|| instance.LifecycleState != PolicyEffectLifecycleState.Active
				|| !PolicyEffectSaveCodec.TryNormalizeInstance(
					instance,
					out PolicyEffectNormalizedInstance normalized,
					out _)
				|| normalized?.IsInert != false
				|| normalized.RuntimeInstance == null
				|| (normalized.Module is not IPolicyEffectModule module)
				|| module?.Descriptor == null)
			{
				continue;
			}
			PolicyEffectInstance runtime = normalized.RuntimeInstance;
			PolicyEffectCanonicalTargetSet runtimeTargetSet =
				NormalizePolicyEffectCanonicalTargetSet(runtime.TargetSet);
			if (!PolicyEffectCompiler.IsTargetBindingAuthorizedForModule(
				module,
				runtimeTargetSet,
				effect?.IssuerKingdomId))
			{
				PolicySystemLog.Failure(
					"Effect",
					"runtime-target-binding-rejected",
					"Persisted policy effect target does not match its declared binding; contribution skipped.",
					"effectId=" + (effect?.EffectId ?? string.Empty)
						+ " moduleId=" + module.Id
						+ " issuerKingdomId=" + (effect?.IssuerKingdomId ?? string.Empty));
				continue;
			}
			PolicyEffectPreparedInstance prepared = new PolicyEffectPreparedInstance
			{
				Descriptor = module.Descriptor,
				IdempotencyKey = instance.InstanceId,
				Instance = new PolicyEffectInstance
				{
					MechanismContractVersion = runtime.MechanismContractVersion,
					MechanismContractHash = runtime.MechanismContractHash ?? string.Empty,
					ExpectedMechanismLegIds = new List<string>(runtime.ExpectedMechanismLegIds ?? new List<string>()),
					EffectPlanVersion = runtime.EffectPlanVersion,
					MechanismId = runtime.MechanismId ?? string.Empty,
					MechanismKind = runtime.MechanismKind,
					MechanismRole = runtime.MechanismRole,
					SourceOmitted = runtime.SourceOmitted,
					DestinationOmitted = runtime.DestinationOmitted,
					InstanceId = runtime.InstanceId,
					PolicyId = runtime.PolicyId,
					ActorHeroId = runtime.ActorHeroId,
					ModuleId = module.Id,
					SourceModuleId = runtime.SourceModuleId,
					TargetSet = runtimeTargetSet,
					Payload = runtime.Payload,
					LifecycleState = runtime.LifecycleState,
					StartDay = runtime.StartDay,
					EndDay = runtime.EndDay,
					SourceScope = runtime.SourceScope,
					Reason = runtime.Reason
				}
			};
			IReadOnlyList<PolicyEffectModelContribution> contributions;
			if (module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.ModelModifier
				&& module is IModelModifierPolicyEffectModule modelModule)
			{
				contributions = modelModule.BuildModelContributions(prepared)
					?? Array.Empty<PolicyEffectModelContribution>();
			}
			else if (module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.DailyMutation
				&& module.Descriptor.Hook == PolicyEffectHook.DailyScheduler
				&& string.Equals(module.Id, "clanInfluencePerDay", StringComparison.Ordinal))
			{
				// The daily mutation remains the sole state-changing owner. This indexed
				// projection is consumed only by the vanilla description call above.
				contributions = PolicyEffectModuleRuntimeAdapters.BuildNumericModelContributions(module, prepared);
			}
			else if (module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.DailyMutation
				&& module.Descriptor.Hook == PolicyEffectHook.DailyScheduler
				&& string.Equals(module.Id, "heroGoldPerDay", StringComparison.Ordinal)
				&& module is IAtomicHeroGoldPolicyEffectModule heroGoldModule)
			{
				// The daily coordinator owns the mutation. This bounded Hero target projection
				// is only for vanilla finance reporting.
				contributions = BuildHeroGoldDailyReportingContributions(module, heroGoldModule, prepared);
			}
			else
			{
				continue;
			}
			for (int contributionIndex = 0; contributionIndex < contributions.Count; contributionIndex++)
			{
				PolicyEffectModelContribution contribution = contributions[contributionIndex];
				if (contribution == null
					|| string.IsNullOrWhiteSpace(contribution.TargetId)
					|| float.IsNaN(contribution.Value)
					|| float.IsInfinity(contribution.Value)
					|| Math.Abs(contribution.Value) <= 0.0001f)
				{
					continue;
				}
				result.Add(new PolicyEffectRuntimeContribution(
					effect?.EffectId,
					FirstNonEmpty(instance.PolicyId, effect?.RecordId),
					module.Id,
					effect?.PolicyName,
					contribution.Hook,
					contribution.TargetKind,
					contribution.TargetId,
					module.Descriptor.Aggregation,
					contribution.Value));
			}
		}
		return result;
	}

	private static IReadOnlyList<PolicyEffectModelContribution> BuildHeroGoldDailyReportingContributions(
		IPolicyEffectModule module,
		IAtomicHeroGoldPolicyEffectModule heroGoldModule,
		PolicyEffectPreparedInstance prepared)
	{
		if (module == null
			|| heroGoldModule == null
			|| prepared?.Instance?.TargetSet?.HeroIds == null
			|| !heroGoldModule.TryReadDelta(prepared.Instance.Payload, out int delta))
		{
			return Array.Empty<PolicyEffectModelContribution>();
		}
		List<PolicyEffectModelContribution> result = new List<PolicyEffectModelContribution>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (string heroId in prepared.Instance.TargetSet.HeroIds)
		{
			string normalizedHeroId = (heroId ?? string.Empty).Trim();
			if (normalizedHeroId.Length == 0 || !seen.Add(normalizedHeroId))
			{
				continue;
			}
			result.Add(new PolicyEffectModelContribution
			{
				InstanceId = prepared.Instance.InstanceId,
				ModuleId = module.Id,
				Hook = module.Descriptor.Hook,
				TargetKind = PolicyEffectTargetKind.Hero,
				TargetId = normalizedHeroId,
				Value = delta,
				DisplayText = module.DescribePayload(prepared.Instance.Payload)
			});
		}
		return result;
	}

	private void RebuildDailyPolicyEffectRuntimePlan(ActivePolicyEffectSaveData effect)
	{
		if (effect == null || string.IsNullOrWhiteSpace(effect.EffectId) || !IsPolicyEffectWithinDuration(effect))
		{
			if (!string.IsNullOrWhiteSpace(effect?.EffectId))
			{
				_policyEffectDailyRuntimePlans.Remove(effect.EffectId);
			}
			return;
		}

		Dictionary<string, List<PolicyEffectDailyRuntimePlanEntry>> settlementEntries
			= new Dictionary<string, List<PolicyEffectDailyRuntimePlanEntry>>(StringComparer.OrdinalIgnoreCase);
		List<PolicyEffectDailyRuntimePlanEntry> nonSettlementEntries = new List<PolicyEffectDailyRuntimePlanEntry>();
		List<PolicyEffectInstanceSaveData> instances = effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>();
		if (!PolicyEffectActivationCoordinator.ReconcileMechanismLifecycleStates(
			instances,
			out bool mechanismStateChanged,
			out string mechanismError))
		{
			_policyEffectDailyRuntimePlans.Remove(effect.EffectId);
			PolicySystemLog.Failure("Effect", "daily-mechanism-preflight-failed", mechanismError,
				"effectId=" + effect.EffectId);
			return;
		}
		if (mechanismStateChanged)
		{
			PolicySystemLog.Write("Effect", "module-lifecycle",
				"daily preflight reconciled mechanism lifecycle effectId=" + effect.EffectId);
		}
		int instanceCount = Math.Min(MaxCompiledPolicyEffectInstances, instances.Count);
		for (int index = 0; index < instanceCount; index++)
		{
			PolicyEffectInstanceSaveData instance = instances[index];
			if (instance == null
				|| instance.LifecycleState != PolicyEffectLifecycleState.Active
				|| !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.DailyMutation)
			{
				continue;
			}
			if (!module.Descriptor.SupportsIdempotency)
			{
				PolicySystemLog.Write("Effect", "daily-plan-rejected", "effectId=" + effect.EffectId
					+ " moduleId=" + module.Id + " reason=descriptor requires idempotency");
				continue;
			}
			if (!PolicyEffectActivationCoordinator.TryPrepareSavedInstance(
				instance,
				out IPolicyEffectModule preparedModule,
				out PolicyEffectPreparedInstance prepared,
				out string prepareError))
			{
				PolicySystemLog.Write("Effect", "daily-plan-rejected", "effectId=" + effect.EffectId
					+ " moduleId=" + module.Id + " reason=" + prepareError);
				continue;
			}

			PolicyEffectCanonicalTargetSet targetSet = prepared.Instance.TargetSet;
			HashSet<string> uniqueTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (PolicyEffectTargetKind targetKind in module.Descriptor.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>())
			{
				foreach (string rawTargetId in GetPolicyEffectTargetIds(targetSet, targetKind))
				{
					string targetId = (rawTargetId ?? string.Empty).Trim();
					string targetKey = BuildDailyPolicyEffectTargetKey(targetKind, targetId);
					if (targetId.Length == 0 || !uniqueTargets.Add(targetKey))
					{
						continue;
					}
					PolicyEffectDailyRuntimePlanEntry entry = new PolicyEffectDailyRuntimePlanEntry
					{
						Instance = instance,
						Module = preparedModule,
						Prepared = prepared,
						TargetKind = targetKind,
						TargetId = targetId
					};
					if (IsSettlementDailyPolicyEffectTarget(targetKind)
						&& instance.MechanismKind != PolicyEffectMechanismKind.Linked)
					{
						if (!settlementEntries.TryGetValue(targetKey, out List<PolicyEffectDailyRuntimePlanEntry> entries))
						{
							entries = new List<PolicyEffectDailyRuntimePlanEntry>();
							settlementEntries[targetKey] = entries;
						}
						entries.Add(entry);
					}
					else
					{
						nonSettlementEntries.Add(entry);
					}
				}
			}
		}

		PolicyEffectDailyRuntimePlan plan = new PolicyEffectDailyRuntimePlan
		{
			NonSettlementEntries = nonSettlementEntries.ToArray(),
			TargetExecutionCount = nonSettlementEntries.Count + settlementEntries.Sum(pair => pair.Value.Count)
		};
		foreach (KeyValuePair<string, List<PolicyEffectDailyRuntimePlanEntry>> pair in settlementEntries)
		{
			plan.SettlementEntries[pair.Key] = pair.Value.ToArray();
			foreach (PolicyEffectDailyRuntimePlanEntry entry in pair.Value)
			{
				if (!string.IsNullOrWhiteSpace(entry?.TargetId))
				{
					plan.SettlementTargetIds.Add(entry.TargetId);
				}
			}
		}
		foreach (PolicyEffectDailyRuntimePlanEntry entry in plan.NonSettlementEntries)
		{
			plan.NonSettlementEntriesByKey[BuildDailyPolicyEffectPlanEntryKey(entry)] = entry;
			if (IsSettlementDailyPolicyEffectTarget(entry.TargetKind)
				&& !string.IsNullOrWhiteSpace(entry.TargetId))
			{
				plan.CanonicalSettlementTargetIds.Add(entry.TargetId);
			}
		}
		foreach (string settlementId in plan.SettlementTargetIds)
		{
			plan.CanonicalSettlementTargetIds.Add(settlementId);
		}
		if (plan.TargetExecutionCount > 0)
		{
			_policyEffectDailyRuntimePlans[effect.EffectId] = plan;
		}
		else
		{
			_policyEffectDailyRuntimePlans.Remove(effect.EffectId);
		}
	}

	private static IEnumerable<string> GetPolicyEffectTargetIds(
		PolicyEffectCanonicalTargetSet targetSet,
		PolicyEffectTargetKind targetKind)
	{
		if (targetSet == null)
		{
			return Array.Empty<string>();
		}
		switch (targetKind)
		{
			case PolicyEffectTargetKind.Settlement:
				return (IEnumerable<string>)targetSet.SettlementIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Town:
				return (IEnumerable<string>)targetSet.TownIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Village:
				return (IEnumerable<string>)targetSet.VillageIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Clan:
				return (IEnumerable<string>)targetSet.ClanIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Kingdom:
				return (IEnumerable<string>)targetSet.KingdomIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Hero:
				return (IEnumerable<string>)targetSet.HeroIds ?? Array.Empty<string>();
			default:
				return Array.Empty<string>();
		}
	}

	private static bool IsSettlementDailyPolicyEffectTarget(PolicyEffectTargetKind targetKind)
	{
		return targetKind == PolicyEffectTargetKind.Settlement
			|| targetKind == PolicyEffectTargetKind.Town
			|| targetKind == PolicyEffectTargetKind.Village;
	}

	private static string BuildDailyPolicyEffectTargetKey(PolicyEffectTargetKind targetKind, string targetId)
	{
		return targetKind + ":" + (targetId ?? string.Empty).Trim();
	}

	private static string BuildDailyPolicyEffectPlanEntryKey(PolicyEffectDailyRuntimePlanEntry entry)
	{
		return (entry?.Instance?.InstanceId ?? string.Empty).Trim()
			+ "\u001f"
			+ ((int)(entry?.TargetKind ?? default)).ToString(CultureInfo.InvariantCulture)
			+ "\u001f"
			+ (entry?.TargetId ?? string.Empty).Trim();
	}

	private static bool TryPreflightFrozenLinkedDailyEntries(
		IReadOnlyCollection<PolicyEffectInstanceSaveData> instances,
		IReadOnlyCollection<string> frozenEntryKeys,
		ISet<string> currentEntryKeys,
		out string error)
	{
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> allInstances = (instances
			?? Array.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		Dictionary<string, PolicyEffectInstanceSaveData> instancesById = allInstances
			.Where(instance => !string.IsNullOrWhiteSpace(instance.InstanceId))
			.GroupBy(instance => instance.InstanceId.Trim(), StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
		HashSet<string> frozenInstanceIds = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> linkedGroups = new HashSet<string>(StringComparer.Ordinal);
		foreach (string rawEntryKey in frozenEntryKeys ?? Array.Empty<string>())
		{
			string entryKey = rawEntryKey ?? string.Empty;
			int separator = entryKey.IndexOf('\u001f');
			string instanceId = (separator < 0 ? entryKey : entryKey.Substring(0, separator)).Trim();
			if (instanceId.Length == 0
				|| !instancesById.TryGetValue(instanceId, out PolicyEffectInstanceSaveData instance))
			{
				continue;
			}
			frozenInstanceIds.Add(instanceId);
			if (instance.MechanismKind != PolicyEffectMechanismKind.Linked)
			{
				continue;
			}
			string groupKey = BuildPolicyEffectMechanismGroupKey(instance);
			linkedGroups.Add(groupKey);
			if (currentEntryKeys == null || !currentEntryKeys.Contains(entryKey))
			{
				SuspendDailyMechanismGroup(allInstances, instance);
				error = "frozen Linked daily target entry is no longer present: " + entryKey;
				return false;
			}
		}

		foreach (string groupKey in linkedGroups)
		{
			List<PolicyEffectInstanceSaveData> group = allInstances
				.Where(instance => instance.MechanismKind == PolicyEffectMechanismKind.Linked
					&& string.Equals(BuildPolicyEffectMechanismGroupKey(instance), groupKey, StringComparison.Ordinal))
				.ToList();
			foreach (PolicyEffectInstanceSaveData instance in group)
			{
				if (instance.LifecycleState != PolicyEffectLifecycleState.Active
					|| !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
					|| module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.DailyMutation)
				{
					continue;
				}
				if (!frozenInstanceIds.Contains((instance.InstanceId ?? string.Empty).Trim()))
				{
					SuspendDailyMechanismGroup(allInstances, instance);
					error = "frozen Linked daily bundle is missing required leg " + (instance.InstanceId ?? string.Empty);
					return false;
				}
			}
		}
		return true;
	}

	private static void SuspendDailyMechanismGroup(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		PolicyEffectInstanceSaveData anchor)
	{
		if (anchor == null)
		{
			return;
		}
		string groupKey = BuildPolicyEffectMechanismGroupKey(anchor);
		foreach (PolicyEffectInstanceSaveData instance in instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			if (instance != null
				&& instance.MechanismKind == PolicyEffectMechanismKind.Linked
				&& instance.LifecycleState == PolicyEffectLifecycleState.Active
				&& string.Equals(BuildPolicyEffectMechanismGroupKey(instance), groupKey, StringComparison.Ordinal))
			{
				instance.LifecycleState = PolicyEffectLifecycleState.Suspended;
			}
		}
	}

	private static string BuildPolicyEffectMechanismGroupKey(PolicyEffectInstanceSaveData instance)
	{
		return (instance?.PolicyId ?? string.Empty).Trim()
			+ "\u001f"
			+ (instance?.MechanismId ?? string.Empty).Trim();
	}

	private static bool HaveSamePolicyTargetIds(IEnumerable<string> left, IEnumerable<string> right)
	{
		HashSet<string> normalizedLeft = new HashSet<string>(
			(left ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()),
			StringComparer.OrdinalIgnoreCase);
		return normalizedLeft.SetEquals(
			(right ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()));
	}

	private static List<string> CollectPolicyEffectPrimarySettlementIds(
		IEnumerable<PolicyEffectInstanceSaveData> instances)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PolicyEffectInstanceSaveData instance in instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			PolicyEffectCanonicalTargetSet targetSet = instance?.TargetSet;
			if (targetSet == null)
			{
				continue;
			}
			HashSet<string> villageIds = new HashSet<string>(
				(targetSet.VillageIds ?? new List<string>())
					.Where(id => !string.IsNullOrWhiteSpace(id))
					.Select(id => id.Trim()),
				StringComparer.OrdinalIgnoreCase);
			foreach (string id in targetSet.ParentSettlementIds ?? new List<string>())
			{
				string normalized = (id ?? string.Empty).Trim();
				if (normalized.Length > 0)
				{
					result.Add(normalized);
				}
			}
			foreach (string id in (targetSet.TownIds ?? new List<string>())
				.Concat(targetSet.SettlementIds ?? new List<string>()))
			{
				string normalized = (id ?? string.Empty).Trim();
				if (normalized.Length <= 0 || villageIds.Contains(normalized))
				{
					continue;
				}
				result.Add(normalized);
			}
		}
		return result.OrderBy(id => id, StringComparer.Ordinal).ToList();
	}

	private static string BuildPolicyEffectTargetFingerprint(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		out int targetCount)
	{
		HashSet<string> targets = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData instance in instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			PolicyEffectCanonicalTargetSet targetSet = instance?.TargetSet;
			if (targetSet == null)
			{
				continue;
			}
			AddPolicyEffectTargetFingerprintIds(targets, "settlement", targetSet.SettlementIds);
			AddPolicyEffectTargetFingerprintIds(targets, "parentSettlement", targetSet.ParentSettlementIds);
			AddPolicyEffectTargetFingerprintIds(targets, "town", targetSet.TownIds);
			AddPolicyEffectTargetFingerprintIds(targets, "village", targetSet.VillageIds);
			AddPolicyEffectTargetFingerprintIds(targets, "clan", targetSet.ClanIds);
			AddPolicyEffectTargetFingerprintIds(targets, "kingdom", targetSet.KingdomIds);
			AddPolicyEffectTargetFingerprintIds(targets, "hero", targetSet.HeroIds);
		}
		targetCount = targets.Count;
		ulong hash = 14695981039346656037UL;
		foreach (string target in targets.OrderBy(value => value, StringComparer.Ordinal))
		{
			foreach (byte value in Encoding.UTF8.GetBytes(target))
			{
				hash ^= value;
				hash *= 1099511628211UL;
			}
			hash ^= (byte)'\n';
			hash *= 1099511628211UL;
		}
		return hash.ToString("x16", CultureInfo.InvariantCulture);
	}

	private static void AddPolicyEffectTargetFingerprintIds(
		ISet<string> targets,
		string targetKind,
		IEnumerable<string> targetIds)
	{
		foreach (string targetId in targetIds ?? Enumerable.Empty<string>())
		{
			string normalized = (targetId ?? string.Empty).Trim();
			if (normalized.Length > 0)
			{
				targets.Add(targetKind + ":" + normalized);
			}
		}
	}

	private bool RefreshActivePolicyEffectCanonicalTargets(
		ActivePolicyEffectSaveData activeEffect,
		IEnumerable<Settlement> sourceFiefs,
		Kingdom targetKingdom,
		bool dailyTick = false)
	{
		if (activeEffect == null)
		{
			return false;
		}
		List<Settlement> sourceSettlements = ExpandLocalPolicySettlements(sourceFiefs)
			.Where(settlement => settlement != null && !string.IsNullOrWhiteSpace(settlement.StringId))
			.GroupBy(settlement => settlement.StringId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		HashSet<string> sourceSettlementIds = new HashSet<string>(
			sourceSettlements.Select(settlement => settlement.StringId),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> explicitClanIds = new HashSet<string>(
			NormalizeIdList(activeEffect.TargetClanIds),
			StringComparer.OrdinalIgnoreCase);
		List<string> directSettlementIds = NormalizeIdList(activeEffect.DirectTargetSettlementIds);
		List<string> sourcePrimaryIds = NormalizeIdList((sourceFiefs ?? Enumerable.Empty<Settlement>())
			.Select(ResolvePrimaryPolicyFief)
			.Where(settlement => settlement != null)
			.Select(settlement => settlement.StringId));
		PolicyTargetWorldSnapshot targetPlanSnapshot = null;
		bool changed = false;
		foreach (PolicyEffectInstanceSaveData instance in activeEffect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
		{
			if (instance == null)
			{
				continue;
			}
			PolicyEffectCanonicalTargetSet current = NormalizePolicyEffectCanonicalTargetSet(instance.TargetSet);
			PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule refreshModule);
			if (refreshModule?.Descriptor?.TargetRefresh == PolicyEffectTargetRefreshKind.FrozenCanonicalIds)
			{
				// This module snapshots its canonical entity IDs when the policy is
				// approved. Related live objects (parties, fiefs, rosters) remain dynamic.
				instance.TargetSet = current;
				continue;
			}
			bool hasHeroSelector = current.SelectorIds.Any(PolicyHeroTargetSelectorResolver.IsKnownSelector);
			bool hasDailyMetricPlan = current.TargetPlans.Any(plan =>
				plan != null
				&& (plan.Dependencies & PolicyTargetPlanDependencies.DailyMetric) != 0);
			if (dailyTick && ShouldSkipDailyPolicyEffectTargetRefresh(
				current,
				refreshModule,
				hasDailyMetricPlan,
				hasHeroSelector))
			{
				// Static membership and settlement-owner projections are refreshed by
				// structure events. Keep the daily hot path free of world-wide scans.
				continue;
			}
			if (current.TargetPlans.Count > 0)
			{
				if (TryRefreshActivePolicyTargetPlanInstance(
					activeEffect,
					instance,
					current,
					targetKingdom,
					sourcePrimaryIds,
					ref targetPlanSnapshot,
					out PolicyEffectCanonicalTargetSet planTargetSet,
					out bool planStateChanged))
				{
					changed |= planStateChanged;
					if (!AreSamePolicyEffectCanonicalTargetSets(current, planTargetSet))
					{
						planTargetSet.StructureVersion = current.StructureVersion >= int.MaxValue
							? int.MaxValue
							: Math.Max(1, current.StructureVersion) + 1;
						changed = true;
					}
					instance.TargetSet = planTargetSet;
				}
				continue;
			}
			bool hasSourceSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'S');
			bool hasSettlementSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'L');
			bool hasClanSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'C');
			bool hasRulerSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'R');
			bool hasKingdomSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'K');
			bool hasDescriptorSelector = current.SelectorIds.Any(selectorId =>
				!PolicyHeroTargetSelectorResolver.IsKnownSelector(selectorId));
			bool hasRecognizedSelector = hasDescriptorSelector
				|| hasHeroSelector
				|| hasSourceSelector
				|| hasSettlementSelector
				|| hasClanSelector
				|| hasRulerSelector
				|| hasKingdomSelector;
			if (!hasRecognizedSelector)
			{
				// Legacy saves may only contain target collections. Preserve their narrowest
				// selector semantics instead of widening them to the active-effect union.
				hasSettlementSelector = current.ParentSettlementIds.Count > 0 || current.SettlementIds.Count > 0;
				hasClanSelector = current.ClanIds.Count > 0;
				hasKingdomSelector = current.KingdomIds.Count > 0;
			}

			List<Settlement> resolved = new List<Settlement>();
			if (hasDescriptorSelector)
			{
				string excludedClanId = activeEffect.ProposerClanId ?? string.Empty;
				foreach (string selectorId in current.SelectorIds.Where(selectorId =>
					!PolicyHeroTargetSelectorResolver.IsKnownSelector(selectorId)))
				{
					if (TryResolvePolicyTargetSelectorSettlements(
						selectorId,
						PolicyEffectScopes.Local,
						targetKingdom,
						excludedClanId,
						out List<Settlement> selectorSettlements,
						out string selectorError))
					{
						resolved.AddRange(selectorSettlements);
						PolicySystemLog.Write("Effect", "target-selector-resolved",
							"policyId=" + (activeEffect.RecordId ?? string.Empty)
							+ " selectorId=" + selectorId
							+ " refreshedCount=" + selectorSettlements.Count.ToString(CultureInfo.InvariantCulture));
					}
					else
					{
						PolicySystemLog.Failure("Effect", "target-selector-refresh-failed",
							"Selector refresh failed closed: " + selectorError,
							"policyId=" + (activeEffect.RecordId ?? string.Empty) + " selectorId=" + selectorId);
					}
				}
			}
			if (hasSourceSelector)
			{
				resolved.AddRange(sourceSettlements);
			}
			if (hasSettlementSelector)
			{
				HashSet<string> originalParentIds = new HashSet<string>(
					current.ParentSettlementIds,
					StringComparer.OrdinalIgnoreCase);
				List<string> instanceDirectIds = directSettlementIds
					.Where(id =>
					{
						Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(id));
						return primary != null && originalParentIds.Contains(primary.StringId ?? string.Empty);
					})
					.ToList();
				IEnumerable<string> parentIds = instanceDirectIds.Count > 0
					? instanceDirectIds
					: current.ParentSettlementIds;
				foreach (string parentId in parentIds)
				{
					Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(parentId));
					if (primary != null
						&& (targetKingdom == null || primary.OwnerClan?.Kingdom == targetKingdom))
					{
						resolved.AddRange(ExpandLocalPolicySettlements(new[] { primary }));
					}
				}
			}

			List<string> refreshedClanIds = new List<string>();
			List<string> refreshedHeroIds = new List<string>();
			RefreshPolicyHeroSelectorTargets(current, refreshModule, refreshedHeroIds, refreshedClanIds);
			if (hasClanSelector || hasRulerSelector)
			{
				IEnumerable<string> instanceExplicitClanIds = current.ClanIds;
				if (explicitClanIds.Count > 0 || current.FollowCurrentRulingClan)
				{
					instanceExplicitClanIds = instanceExplicitClanIds.Where(explicitClanIds.Contains);
				}
				foreach (string clanId in NormalizeIdList(instanceExplicitClanIds))
				{
					Clan clan = ResolveClanById(clanId);
					if (clan == null || clan.IsEliminated || clan.Kingdom == null
						|| (targetKingdom != null && clan.Kingdom != targetKingdom))
					{
						continue;
					}
					AddUniquePolicyEffectId(refreshedClanIds, clan.StringId);
					resolved.AddRange(ExpandLocalPolicySettlements(clan.Settlements ?? Enumerable.Empty<Settlement>()));
				}
				if (current.FollowCurrentRulingClan
					&& targetKingdom?.RulingClan != null)
				{
					AddUniquePolicyEffectId(refreshedClanIds, targetKingdom.RulingClan.StringId);
					resolved.AddRange(ExpandLocalPolicySettlements(
						targetKingdom.RulingClan.Settlements ?? Enumerable.Empty<Settlement>()));
				}
			}

			List<string> refreshedKingdomIds = new List<string>();
			if (hasKingdomSelector)
			{
				bool includeKingdomClans = PolicyEffectModuleCatalog.TryGet(
					instance.ModuleId,
					out IPolicyEffectModule kingdomProjectionModule)
					&& kingdomProjectionModule.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan);
				List<string> kingdomIds = NormalizeIdList(current.KingdomIds);
				foreach (string kingdomId in kingdomIds)
				{
					Kingdom kingdom = ResolveKingdomByIdOrName(kingdomId, string.Empty);
					if (kingdom == null || kingdom.IsEliminated)
					{
						continue;
					}
					AddUniquePolicyEffectId(refreshedKingdomIds, kingdom.StringId);
					if (includeKingdomClans)
					{
						foreach (Clan clan in ((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>())
							.Where(clan => clan != null && !clan.IsEliminated
								&& clan.Kingdom == kingdom && !string.IsNullOrWhiteSpace(clan.StringId)))
						{
							AddUniquePolicyEffectId(refreshedClanIds, clan.StringId);
						}
					}
					resolved.AddRange(GetKingdomSettlements(kingdom));
				}
			}

			List<Settlement> normalizedSettlements = resolved
				.Where(settlement => settlement != null
					&& !string.IsNullOrWhiteSpace(settlement.StringId)
					&& (hasSourceSelector || !sourceSettlementIds.Contains(settlement.StringId)))
				.GroupBy(settlement => settlement.StringId, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First())
				.ToList();
			PolicyEffectCanonicalTargetSet refreshed = new PolicyEffectCanonicalTargetSet
			{
				StructureVersion = current.StructureVersion,
				JurisdictionKind = current.JurisdictionKind,
				AuthorizedCrossKingdomIds = new List<string>(current.AuthorizedCrossKingdomIds ?? new List<string>()),
				SelectorHandles = new List<string>(current.SelectorHandles),
				SelectorIds = new List<string>(current.SelectorIds),
				ClanIds = refreshedClanIds,
				KingdomIds = refreshedKingdomIds,
				HeroIds = refreshedHeroIds,
				FollowCurrentRulingClan = current.FollowCurrentRulingClan
			};
			bool projectionFailed = false;
			foreach (Settlement primary in normalizedSettlements
				.Select(ResolvePrimaryPolicyFief)
				.Where(settlement => settlement != null && (settlement.IsTown || settlement.IsCastle))
				.GroupBy(settlement => settlement.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First()))
			{
				if (!AddPolicyEffectPrimaryTargetForModule(
					refreshed,
					primary,
					refreshModule,
					ResolvePolicyEffectPrimaryTargetOrigin(current),
					out _))
				{
					projectionFailed = true;
				}
			}
			if (projectionFailed && HasSettlementOwnerLeaderProjection(refreshModule))
			{
				refreshed.HeroIds.Clear();
			}
			refreshed = NormalizePolicyEffectCanonicalTargetSet(refreshed);
			PolicyEffectTargetJurisdiction.TryApply(
				refreshed,
				refreshModule,
				targetKingdom?.StringId ?? activeEffect.TargetKingdomId,
				activeEffect.IssuerKingdomId,
				current.AuthorizedCrossKingdomIds,
				preserveLegacyCrossKingdoms: true,
				failOnUnauthorized: false,
				out refreshed,
				out _);
			bool instanceChanged = !AreSamePolicyEffectCanonicalTargetSets(current, refreshed);
			if (instanceChanged)
			{
				refreshed.StructureVersion = current.StructureVersion >= int.MaxValue
					? int.MaxValue
					: Math.Max(1, current.StructureVersion) + 1;
				changed = true;
			}
			instance.TargetSet = refreshed;
		}
		if (PolicyEffectActivationCoordinator.ReconcileMechanismLifecycleStates(
			activeEffect.ModuleEffects,
			out bool mechanismStateChanged,
			out string mechanismError))
		{
			changed |= mechanismStateChanged;
		}
		else
		{
			PolicySystemLog.Failure("Effect", "effect-plan-lifecycle-refresh-failed",
				mechanismError, "policyId=" + (activeEffect.RecordId ?? string.Empty));
		}
		return changed;
	}

	private static bool TryMaterializePolicyTargetPlansForRegistration(
		PolicyEffectCanonicalTargetSet current,
		IPolicyEffectModule module,
		string scope,
		string targetKingdomId,
		string issuerKingdomId,
		string proposerClanId,
		IReadOnlyCollection<string> sourcePrimarySettlementIds,
		ref PolicyTargetWorldSnapshot snapshot,
		out PolicyEffectCanonicalTargetSet materialized,
		out string error)
	{
		materialized = null;
		error = string.Empty;
		if (current == null || module == null)
		{
			error = "TargetPlan registration context is incomplete";
			return false;
		}
		if (snapshot == null)
		{
			snapshot = PolicyTargetSemanticRouter.CaptureWorldSnapshot();
		}
		materialized = new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = current.StructureVersion,
			JurisdictionKind = current.JurisdictionKind,
			AuthorizedCrossKingdomIds = new List<string>(current.AuthorizedCrossKingdomIds ?? new List<string>()),
			SelectorHandles = new List<string>(current.SelectorHandles),
			SelectorIds = new List<string>(current.SelectorIds),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(current.TargetPlans),
			FollowCurrentRulingClan = current.FollowCurrentRulingClan
		};
		PolicyTargetPlanResolutionContext context = new PolicyTargetPlanResolutionContext
		{
			Scope = scope ?? string.Empty,
			TargetKingdomId = targetKingdomId ?? string.Empty,
			IssuerKingdomId = issuerKingdomId ?? string.Empty,
			PlayerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
			ProposerClanId = proposerClanId ?? string.Empty,
			SourceSettlementIds = sourcePrimarySettlementIds ?? Array.Empty<string>(),
			AllowPersistedValidatedReferences = true,
			Snapshot = snapshot
		};
		foreach (PolicyTargetPlanSaveData plan in materialized.TargetPlans)
		{
			if (!PolicyTargetPlanResolver.TryResolve(
				plan,
				context,
				out PolicyTargetPlanResolution resolution,
				out error))
			{
				materialized = null;
				return false;
			}
			foreach (string clanId in resolution.ClanIds)
			{
				AddUniquePolicyEffectId(materialized.ClanIds, clanId);
			}
			foreach (string kingdomId in resolution.KingdomIds)
			{
				AddUniquePolicyEffectId(materialized.KingdomIds, kingdomId);
			}
			foreach (string primaryId in PolicyTargetPlanResolver.ExpandPrimarySettlementIds(resolution, snapshot))
			{
				Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(primaryId));
				if (!AddPolicyEffectPrimaryTargetForModule(
					materialized,
					primary,
					module,
					PolicyEffectPrimaryTargetOrigin.TargetPlanPrimarySettlement,
					out error))
				{
					materialized = null;
					return false;
				}
			}
		}
		materialized = NormalizePolicyEffectCanonicalTargetSet(materialized);
		if (!PolicyEffectTargetJurisdiction.TryApply(
			materialized,
			module,
			targetKingdomId,
			issuerKingdomId,
			current.AuthorizedCrossKingdomIds,
			preserveLegacyCrossKingdoms: false,
			failOnUnauthorized: true,
			out materialized,
			out error))
		{
			return false;
		}
		return true;
	}

	internal static PolicyEffectLifecycleState ClassifyPolicyTargetPlanLifecycle(
		bool resolutionSucceeded,
		PolicyTargetPlanResolutionFailureKind failureKind,
		bool hasMaterializedTarget)
	{
		if (!resolutionSucceeded)
		{
			return PolicyEffectLifecycleState.Failed;
		}
		return hasMaterializedTarget
			? PolicyEffectLifecycleState.Active
			: PolicyEffectLifecycleState.Suspended;
	}

	private static bool TryRefreshActivePolicyTargetPlanInstance(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectInstanceSaveData instance,
		PolicyEffectCanonicalTargetSet current,
		Kingdom targetKingdom,
		IReadOnlyCollection<string> sourcePrimarySettlementIds,
		ref PolicyTargetWorldSnapshot snapshot,
		out PolicyEffectCanonicalTargetSet refreshed,
		out bool lifecycleChanged)
	{
		refreshed = current;
		lifecycleChanged = false;
		if (instance == null || current == null)
		{
			return false;
		}
		if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module))
		{
			return MarkPolicyTargetPlanInstanceFailed(
				activeEffect,
				instance,
				"unknown module",
				"UnknownModule",
				ref lifecycleChanged);
		}
		if (module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot
			|| PolicyEffectActivationCoordinator.HasFrozenScheduledTargets(instance)
			|| instance.LifecycleState == PolicyEffectLifecycleState.Completed
			|| instance.LifecycleState == PolicyEffectLifecycleState.RolledBack
			|| instance.LifecycleState == PolicyEffectLifecycleState.Failed)
		{
			return true;
		}
		try
		{
			if (snapshot == null)
			{
				snapshot = PolicyTargetSemanticRouter.CaptureWorldSnapshot();
			}
			PolicyEffectCanonicalTargetSet materialized = new PolicyEffectCanonicalTargetSet
			{
				StructureVersion = current.StructureVersion,
				JurisdictionKind = current.JurisdictionKind,
				AuthorizedCrossKingdomIds = new List<string>(current.AuthorizedCrossKingdomIds ?? new List<string>()),
				SelectorHandles = new List<string>(current.SelectorHandles),
				SelectorIds = new List<string>(current.SelectorIds),
				TargetPlans = PolicyTargetPlanResolver.NormalizePlans(current.TargetPlans),
				FollowCurrentRulingClan = current.FollowCurrentRulingClan
			};
			PolicyTargetPlanResolutionContext context = new PolicyTargetPlanResolutionContext
			{
				Scope = FirstNonEmpty(instance.SourceScope, activeEffect?.ScopeKind),
				TargetKingdomId = activeEffect?.TargetKingdomId ?? string.Empty,
				IssuerKingdomId = activeEffect?.IssuerKingdomId ?? string.Empty,
				PlayerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
				ProposerClanId = activeEffect?.ProposerClanId ?? string.Empty,
				SourceSettlementIds = sourcePrimarySettlementIds ?? Array.Empty<string>(),
				AllowPersistedValidatedReferences = true,
				Snapshot = snapshot
			};
			bool projectionFailed = false;
			foreach (PolicyTargetPlanSaveData plan in materialized.TargetPlans)
			{
				if (!PolicyTargetPlanResolver.TryResolve(
					plan,
					context,
					out PolicyTargetPlanResolution resolution,
					out PolicyTargetPlanResolutionFailureKind failureKind,
					out string planError))
				{
					return MarkPolicyTargetPlanInstanceFailed(
						activeEffect,
						instance,
						planError,
						failureKind.ToString(),
						ref lifecycleChanged);
				}
				foreach (string clanId in resolution.ClanIds)
				{
					AddUniquePolicyEffectId(materialized.ClanIds, clanId);
				}
				foreach (string kingdomId in resolution.KingdomIds)
				{
					AddUniquePolicyEffectId(materialized.KingdomIds, kingdomId);
				}
				foreach (string primaryId in PolicyTargetPlanResolver.ExpandPrimarySettlementIds(resolution, snapshot))
				{
					Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(primaryId));
					if (!AddPolicyEffectPrimaryTargetForModule(
						materialized,
						primary,
						module,
						PolicyEffectPrimaryTargetOrigin.TargetPlanPrimarySettlement,
						out _))
					{
						projectionFailed = true;
					}
				}
			}
			if (projectionFailed && HasSettlementOwnerLeaderProjection(module))
			{
				materialized.HeroIds.Clear();
			}

			refreshed = NormalizePolicyEffectCanonicalTargetSet(materialized);
			if (!PolicyEffectTargetJurisdiction.TryApply(
				refreshed,
				module,
				activeEffect?.TargetKingdomId,
				activeEffect?.IssuerKingdomId,
				current.AuthorizedCrossKingdomIds,
				preserveLegacyCrossKingdoms: true,
				failOnUnauthorized: false,
				out refreshed,
				out _))
			{
				refreshed = NormalizePolicyEffectCanonicalTargetSet(materialized);
			}
			PolicyEffectLifecycleState desiredState = ClassifyPolicyTargetPlanLifecycle(
				resolutionSucceeded: true,
				PolicyTargetPlanResolutionFailureKind.None,
				HasMaterializedPolicyEffectTargetForModule(module, refreshed));
			if (instance.LifecycleState != desiredState)
			{
				PolicyEffectLifecycleState previousState = instance.LifecycleState;
				PolicySystemLog.Write("Effect", "target-plan-lifecycle-changed",
					"policyId=" + (activeEffect?.RecordId ?? string.Empty)
					+ " instanceId=" + (instance.InstanceId ?? string.Empty)
					+ " from=" + instance.LifecycleState
					+ " to=" + desiredState);
				instance.LifecycleState = desiredState;
				lifecycleChanged = true;
				string targetHash = BuildPolicyEffectTargetFingerprint(
					new[] { instance },
					out int targetCount);
				PolicySystemLog.Transaction(
					(activeEffect?.EffectId ?? instance.InstanceId) + ":target-refresh",
					activeEffect?.RecordId,
					activeEffect?.EffectId,
					instance.MechanismId,
					desiredState == PolicyEffectLifecycleState.Active ? "active" : "suspended",
					"success",
					errorKind: desiredState == PolicyEffectLifecycleState.Suspended ? "EmptyResult" : string.Empty,
					targetHash: targetHash,
					targetCount: targetCount,
					stateBefore: previousState.ToString(),
					stateAfter: desiredState.ToString());
			}
			return true;
		}
		catch (Exception ex)
		{
			return MarkPolicyTargetPlanInstanceFailed(
				activeEffect,
				instance,
				ex.Message,
				"InternalFailure",
				ref lifecycleChanged);
		}
	}

	private static bool MarkPolicyTargetPlanInstanceFailed(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectInstanceSaveData instance,
		string error,
		string errorKind,
		ref bool lifecycleChanged)
	{
		if (instance.LifecycleState != PolicyEffectLifecycleState.Failed)
		{
			PolicySystemLog.Failure("Effect", "target-plan-invalidated",
				"TargetPlan failed closed: " + (error ?? string.Empty),
				"policyId=" + (activeEffect?.RecordId ?? string.Empty)
				+ " instanceId=" + (instance.InstanceId ?? string.Empty));
			instance.LifecycleState = PolicyEffectLifecycleState.Failed;
			lifecycleChanged = true;
			string targetHash = BuildPolicyEffectTargetFingerprint(
				new[] { instance },
				out int targetCount);
			PolicySystemLog.Transaction(
				(activeEffect?.EffectId ?? instance.InstanceId) + ":target-refresh",
				activeEffect?.RecordId,
				activeEffect?.EffectId,
				instance.MechanismId,
				"effectsCommitted",
				"failed",
				errorKind: FirstNonEmpty(errorKind, "InvalidTargetPlan"),
				targetHash: targetHash,
				targetCount: targetCount,
				stateBefore: "active",
				stateAfter: "failed");
		}
		return true;
	}

	private static bool HasMaterializedPolicyEffectTargetForModule(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet targetSet)
	{
		foreach (PolicyEffectTargetKind targetKind in module?.Descriptor?.TargetKinds
			?? Array.Empty<PolicyEffectTargetKind>())
		{
			switch (targetKind)
			{
				case PolicyEffectTargetKind.Settlement: if ((targetSet?.SettlementIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Town: if ((targetSet?.TownIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Village: if ((targetSet?.VillageIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Clan: if ((targetSet?.ClanIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Kingdom: if ((targetSet?.KingdomIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Hero: if ((targetSet?.HeroIds?.Count ?? 0) > 0) return true; break;
			}
		}
		return false;
	}

	private static bool HasPolicyEffectSelectorKind(IEnumerable<string> selectorHandles, char selectorKind)
	{
		char expected = char.ToUpperInvariant(selectorKind);
		return (selectorHandles ?? Enumerable.Empty<string>()).Any(handle =>
		{
			string normalized = (handle ?? string.Empty).Trim();
			return normalized.Length > 0
				&& char.ToUpperInvariant(normalized[0]) == expected
				&& (normalized.Length == 1 || char.IsDigit(normalized[1]) || normalized[1] == ':');
		});
	}

	private bool RefreshKingdomPolicyEffectCanonicalTargets(
		ActivePolicyEffectSaveData activeEffect,
		Kingdom targetKingdom,
		bool dailyTick = false)
	{
		if (activeEffect == null)
		{
			return false;
		}
		bool changed = false;
		PolicyTargetWorldSnapshot targetPlanSnapshot = null;
		foreach (PolicyEffectInstanceSaveData instance in activeEffect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
		{
			if (instance == null)
			{
				continue;
			}
			PolicyEffectCanonicalTargetSet current = NormalizePolicyEffectCanonicalTargetSet(instance.TargetSet);
			bool hasHeroSelector = current.SelectorIds.Any(PolicyHeroTargetSelectorResolver.IsKnownSelector);
			bool hasDailyMetricPlan = current.TargetPlans.Any(plan =>
				plan != null
				&& (plan.Dependencies & PolicyTargetPlanDependencies.DailyMetric) != 0);
			PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule refreshModule);
			if (dailyTick && ShouldSkipDailyPolicyEffectTargetRefresh(
				current,
				refreshModule,
				hasDailyMetricPlan,
				hasHeroSelector))
			{
				// Membership and settlement-owner changes are event-driven; do not
				// rescan kingdom structures on the daily hot path.
				continue;
			}
			if (current.TargetPlans.Count > 0)
			{
				if (TryRefreshActivePolicyTargetPlanInstance(
					activeEffect,
					instance,
					current,
					targetKingdom,
					Array.Empty<string>(),
					ref targetPlanSnapshot,
					out PolicyEffectCanonicalTargetSet planTargetSet,
					out bool planStateChanged))
				{
					changed |= planStateChanged;
					if (!AreSamePolicyEffectCanonicalTargetSets(current, planTargetSet))
					{
						planTargetSet.StructureVersion = current.StructureVersion >= int.MaxValue
							? int.MaxValue
							: Math.Max(1, current.StructureVersion) + 1;
						changed = true;
					}
					instance.TargetSet = planTargetSet;
				}
				continue;
			}
			bool hasSourceSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'S');
			bool hasSettlementSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'L');
			bool hasClanSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'C');
			bool hasRulerSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'R');
			bool hasKingdomSelector = HasPolicyEffectSelectorKind(current.SelectorHandles, 'K');
			bool hasDescriptorSelector = current.SelectorIds.Any(selectorId =>
				!PolicyHeroTargetSelectorResolver.IsKnownSelector(selectorId));
			bool hasRecognizedSelector = hasDescriptorSelector
				|| hasHeroSelector
				|| hasSourceSelector
				|| hasSettlementSelector
				|| hasClanSelector
				|| hasRulerSelector
				|| hasKingdomSelector;
			if (!hasRecognizedSelector)
			{
				// Legacy non-local saves may only carry canonical collections.
				hasSettlementSelector = current.ParentSettlementIds.Count > 0 || current.SettlementIds.Count > 0;
				hasClanSelector = current.ClanIds.Count > 0;
				hasKingdomSelector = current.KingdomIds.Count > 0;
			}

			List<Settlement> settlements = new List<Settlement>();
			if (hasSettlementSelector)
			{
				HashSet<string> originalParentIds = new HashSet<string>(
					current.ParentSettlementIds,
					StringComparer.OrdinalIgnoreCase);
				List<string> directIds = NormalizeIdList(activeEffect.DirectTargetSettlementIds)
					.Where(id =>
					{
						Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(id));
						return primary != null && originalParentIds.Contains(primary.StringId ?? string.Empty);
					})
					.ToList();
				IEnumerable<string> parentIds = directIds.Count > 0
					? directIds
					: current.ParentSettlementIds;
				foreach (string parentId in parentIds)
				{
					Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(parentId));
					if (targetKingdom != null
						&& primary != null
						&& primary.OwnerClan?.Kingdom == targetKingdom)
					{
						settlements.AddRange(ExpandLocalPolicySettlements(new[] { primary }));
					}
				}
			}

			List<string> refreshedClanIds = new List<string>();
			List<string> refreshedHeroIds = new List<string>();
			RefreshPolicyHeroSelectorTargets(current, refreshModule, refreshedHeroIds, refreshedClanIds);
			if (hasClanSelector)
			{
				IEnumerable<string> clanIds = current.ClanIds;
				HashSet<string> activeExplicitClanIds = new HashSet<string>(
					NormalizeIdList(activeEffect.TargetClanIds),
					StringComparer.OrdinalIgnoreCase);
				if ((hasRulerSelector || current.FollowCurrentRulingClan) && activeExplicitClanIds.Count > 0)
				{
					clanIds = clanIds.Where(activeExplicitClanIds.Contains);
				}
				foreach (string clanId in clanIds)
				{
					Clan clan = ResolveClanById(clanId);
					if (targetKingdom == null || clan == null || clan.IsEliminated
						|| clan.Kingdom == null || clan.Kingdom != targetKingdom)
					{
						continue;
					}
					AddUniquePolicyEffectId(refreshedClanIds, clan.StringId);
					settlements.AddRange(ExpandLocalPolicySettlements(clan.Settlements ?? Enumerable.Empty<Settlement>()));
				}
			}
			if (current.FollowCurrentRulingClan && targetKingdom?.RulingClan != null)
			{
				AddUniquePolicyEffectId(refreshedClanIds, targetKingdom.RulingClan.StringId);
				settlements.AddRange(ExpandLocalPolicySettlements(
					targetKingdom.RulingClan.Settlements ?? Enumerable.Empty<Settlement>()));
			}

			List<string> refreshedKingdomIds = new List<string>();
			if (hasKingdomSelector)
			{
				bool includeKingdomClans = PolicyEffectModuleCatalog.TryGet(
					instance.ModuleId,
					out IPolicyEffectModule kingdomProjectionModule)
					&& kingdomProjectionModule.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan);
				List<string> kingdomIds = NormalizeIdList(current.KingdomIds);
				foreach (string kingdomId in kingdomIds)
				{
					Kingdom kingdom = ResolveKingdomByIdOrName(kingdomId, string.Empty);
					if (kingdom == null || kingdom.IsEliminated)
					{
						continue;
					}
					AddUniquePolicyEffectId(refreshedKingdomIds, kingdom.StringId);
					if (includeKingdomClans)
					{
						foreach (Clan clan in ((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>())
							.Where(clan => clan != null && !clan.IsEliminated
								&& clan.Kingdom == kingdom && !string.IsNullOrWhiteSpace(clan.StringId)))
						{
							AddUniquePolicyEffectId(refreshedClanIds, clan.StringId);
						}
					}
					settlements.AddRange(GetKingdomSettlements(kingdom));
				}
			}
			// S is not a legal non-local selector. It intentionally contributes no targets;
			// invalid legacy instances therefore remain fail-closed instead of inheriting a union.
			List<Settlement> normalizedSettlements = settlements
				.Where(settlement => settlement != null && !string.IsNullOrWhiteSpace(settlement.StringId))
				.GroupBy(settlement => settlement.StringId, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First())
				.ToList();
			PolicyEffectCanonicalTargetSet refreshed = new PolicyEffectCanonicalTargetSet
			{
				StructureVersion = current.StructureVersion,
				JurisdictionKind = current.JurisdictionKind,
				AuthorizedCrossKingdomIds = new List<string>(current.AuthorizedCrossKingdomIds ?? new List<string>()),
				SelectorHandles = new List<string>(current.SelectorHandles),
				SelectorIds = new List<string>(current.SelectorIds),
				ClanIds = refreshedClanIds,
				KingdomIds = refreshedKingdomIds,
				HeroIds = refreshedHeroIds,
				FollowCurrentRulingClan = current.FollowCurrentRulingClan
			};
			bool projectionFailed = false;
			foreach (Settlement primary in normalizedSettlements
				.Select(ResolvePrimaryPolicyFief)
				.Where(settlement => settlement != null && (settlement.IsTown || settlement.IsCastle))
				.GroupBy(settlement => settlement.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First()))
			{
				if (!AddPolicyEffectPrimaryTargetForModule(
					refreshed,
					primary,
					refreshModule,
					ResolvePolicyEffectPrimaryTargetOrigin(current),
					out _))
				{
					projectionFailed = true;
				}
			}
			if (projectionFailed && HasSettlementOwnerLeaderProjection(refreshModule))
			{
				refreshed.HeroIds.Clear();
			}
			refreshed = NormalizePolicyEffectCanonicalTargetSet(refreshed);
			PolicyEffectTargetJurisdiction.TryApply(
				refreshed,
				refreshModule,
				targetKingdom?.StringId ?? activeEffect.TargetKingdomId,
				activeEffect.IssuerKingdomId,
				current.AuthorizedCrossKingdomIds,
				preserveLegacyCrossKingdoms: true,
				failOnUnauthorized: false,
				out refreshed,
				out _);
			if (!AreSamePolicyEffectCanonicalTargetSets(current, refreshed))
			{
				refreshed.StructureVersion = current.StructureVersion >= int.MaxValue
					? int.MaxValue
					: Math.Max(1, current.StructureVersion) + 1;
				changed = true;
			}
			instance.TargetSet = refreshed;
		}
		if (PolicyEffectActivationCoordinator.ReconcileMechanismLifecycleStates(
			activeEffect.ModuleEffects,
			out bool mechanismStateChanged,
			out string mechanismError))
		{
			changed |= mechanismStateChanged;
		}
		else
		{
			PolicySystemLog.Failure("Effect", "effect-plan-lifecycle-refresh-failed",
				mechanismError, "policyId=" + (activeEffect.RecordId ?? string.Empty));
		}
		return changed;
	}

	private static bool ShouldSkipDailyPolicyEffectTargetRefresh(
		PolicyEffectCanonicalTargetSet targetSet,
		IPolicyEffectModule module,
		bool hasDailyMetricPlan,
		bool hasHeroSelector)
	{
		if (targetSet == null || hasDailyMetricPlan || hasHeroSelector)
		{
			return false;
		}
		if (targetSet.TargetPlans.Count > 0)
		{
			return true;
		}
		IReadOnlyCollection<PolicyEffectTargetKind> targetKinds = module?.Descriptor?.TargetKinds;
		return targetKinds?.Contains(PolicyEffectTargetKind.Clan) == true
			|| (targetSet.ParentSettlementIds.Count > 0
				&& HasSettlementOwnerLeaderProjection(module)
				&& HasSettlementOwnerLeaderProjectionSource(targetSet));
	}

	private static bool HasSettlementOwnerLeaderProjection(IPolicyEffectModule module)
	{
		return module?.Descriptor?.TargetProjection
			== PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader;
	}

	private static bool HasSettlementOwnerLeaderProjectionSource(PolicyEffectCanonicalTargetSet targetSet)
	{
		return targetSet != null
			&& (HasPolicyEffectSelectorKind(targetSet.SelectorHandles, 'S')
				|| HasPolicyEffectSelectorKind(targetSet.SelectorHandles, 'L')
				|| targetSet.TargetPlans.Count > 0
				|| targetSet.SelectorIds.Any(selectorId =>
					!PolicyHeroTargetSelectorResolver.IsKnownSelector(selectorId))
				|| (targetSet.SelectorHandles.Count == 0
					&& targetSet.SelectorIds.Count == 0
					&& targetSet.TargetPlans.Count == 0
					&& targetSet.ParentSettlementIds.Count > 0));
	}

	private static PolicyEffectPrimaryTargetOrigin ResolvePolicyEffectPrimaryTargetOrigin(
		PolicyEffectCanonicalTargetSet targetSet)
	{
		if ((targetSet?.TargetPlans?.Count ?? 0) > 0)
		{
			return PolicyEffectPrimaryTargetOrigin.TargetPlanPrimarySettlement;
		}
		bool hasSettlementSelector = HasPolicyEffectSelectorKind(targetSet?.SelectorHandles, 'S')
			|| HasPolicyEffectSelectorKind(targetSet?.SelectorHandles, 'L')
			|| (targetSet?.SelectorIds?.Any(selectorId =>
				!PolicyHeroTargetSelectorResolver.IsKnownSelector(selectorId)) ?? false);
		bool hasAggregateSelector = HasPolicyEffectSelectorKind(targetSet?.SelectorHandles, 'C')
			|| HasPolicyEffectSelectorKind(targetSet?.SelectorHandles, 'R')
			|| HasPolicyEffectSelectorKind(targetSet?.SelectorHandles, 'K');
		if (hasSettlementSelector && !hasAggregateSelector)
		{
			return PolicyEffectPrimaryTargetOrigin.SettlementSelector;
		}
		if (!hasSettlementSelector
			&& !hasAggregateSelector
			&& (targetSet?.ParentSettlementIds?.Count ?? 0) > 0)
		{
			return PolicyEffectPrimaryTargetOrigin.LegacyCanonicalSettlement;
		}
		return PolicyEffectPrimaryTargetOrigin.AggregateSelector;
	}

	private static PolicyEffectPrimaryTargetOrigin ResolvePolicyEffectPrimaryTargetOrigin(
		PolicyTargetHandleSaveData target)
	{
		if (string.Equals(target?.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyEffectPrimaryTargetOrigin.TargetPlanPrimarySettlement;
		}
		if (string.Equals(target?.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(target?.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(target?.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyEffectPrimaryTargetOrigin.SettlementSelector;
		}
		return PolicyEffectPrimaryTargetOrigin.AggregateSelector;
	}

	private void RefreshPolicyEffectRuntimeTargetsAfterStructureChange(string reason)
	{
		BannerlordPolicyEffectGameBridge.Instance.InvalidateTargetCaches();
		int scanned = 0;
		int changedCount = 0;
		List<string> failures = new List<string>();
		bool relationOnly = string.Equals(reason, "war-declared", StringComparison.Ordinal)
			|| string.Equals(reason, "peace-made", StringComparison.Ordinal)
			|| string.Equals(reason, "alliance-changed", StringComparison.Ordinal);
		string[] candidateEffectIds = (relationOnly
			? _policyTargetRelationDependencyEffectIds
			: _policyTargetStructureDependencyEffectIds.Concat(_legacyPolicyTargetRefreshEffectIds))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		foreach (string effectId in candidateEffectIds)
		{
			if (!_activePolicyEffects.TryGetValue(effectId, out string activeRaw))
			{
				continue;
			}
			try
			{
				ActivePolicyEffectSaveData activeEffect = GetActivePolicyEffectForWork(effectId, activeRaw ?? string.Empty);
				if (!IsPolicyEffectWithinDuration(activeEffect))
				{
					continue;
				}
				scanned++;
				List<string> previousSettlementIds = NormalizeIdList(activeEffect.TargetSettlementIds);
				List<string> previousFiefIds = NormalizeIdList(activeEffect.TargetFiefIds);
				bool activeChanged;
				if (IsLocalActivePolicyEffect(activeEffect))
				{
					List<Settlement> sourceFiefs = ResolveOwnedLocalPolicyFiefs(
						GetLocalPolicySourceFiefIds(activeEffect.RecordId));
					Kingdom localTargetKingdom = ResolveKingdomByIdOrName(
						activeEffect.TargetKingdomId,
						activeEffect.TargetKingdomName);
					activeChanged = RefreshActivePolicyEffectCanonicalTargets(
						activeEffect,
						sourceFiefs,
						localTargetKingdom);
					activeEffect.TargetFiefIds = NormalizeIdList(sourceFiefs.Select(fief => fief.StringId));
				}
				else
				{
					Kingdom targetKingdom = ResolveKingdomByIdOrName(
						activeEffect.TargetKingdomId,
						activeEffect.TargetKingdomName);
					activeChanged = RefreshKingdomPolicyEffectCanonicalTargets(activeEffect, targetKingdom);
				}
				activeEffect.TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(activeEffect.ModuleEffects);
				activeChanged |= !HaveSamePolicyTargetIds(previousSettlementIds, activeEffect.TargetSettlementIds)
					|| !HaveSamePolicyTargetIds(previousFiefIds, activeEffect.TargetFiefIds);
				if (!activeChanged)
				{
					continue;
				}
				PersistActivePolicyEffect(effectId, activeEffect, structureChanged: false);
				changedCount++;
			}
			catch (Exception ex)
			{
				failures.Add((effectId ?? string.Empty) + ": " + ex.Message);
			}
		}
		if (changedCount > 0)
		{
			try
			{
				RebuildActivePolicyEffectRuntimeIndex();
			}
			catch (Exception ex)
			{
				failures.Add("runtime-index: " + ex.Message);
			}
		}
		if (changedCount > 0 || failures.Count > 0)
		{
			PolicySystemLog.Write("Effect", "runtime-target-structure-refresh",
				"reason=" + (reason ?? string.Empty)
				+ " scanned=" + scanned.ToString(CultureInfo.InvariantCulture)
				+ " changed=" + changedCount.ToString(CultureInfo.InvariantCulture)
				+ " failures=" + failures.Count.ToString(CultureInfo.InvariantCulture)
				+ (failures.Count == 0 ? string.Empty : " errors=" + string.Join(" | ", failures.Take(8))));
		}
	}

	private void PersistActivePolicyEffect(string effectId, ActivePolicyEffectSaveData effect, bool structureChanged = true)
	{
		if (string.IsNullOrWhiteSpace(effectId) || effect == null)
		{
			return;
		}
		bool hadPrevious = _activePolicyEffects.TryGetValue(effectId, out string previousRaw);
		try
		{
			string raw = JsonConvert.SerializeObject(effect);
			_activePolicyEffects[effectId] = raw;
			_activePolicyEffectRuntimeCache[effectId] = new ActivePolicyEffectRuntimeEntry
			{
				Raw = raw,
				Effect = effect
			};
			RefreshPlayerPolicyMaintenanceRuntimeIndex(effect);
			RefreshPolicyTargetDependencyIndex(effectId, effect);
			if (structureChanged)
			{
				RefreshActivePolicyEffectRuntimeIndex(effect);
			}
			_policyEffectDailyPersistenceTransactions.Remove(effectId);
			if (_policyEffectPendingDailyCompensationEffectIds.Contains(effectId)
				&& !PolicyEffectActivationCoordinator.HasPendingDailyCompensation(effect.ModuleEffects))
			{
				_policyEffectPendingDailyCompensationEffectIds.Remove(effectId);
			}
			_quarantinedActivePolicyEffectIds.Remove(effectId);
		}
		catch (Exception persistException)
		{
			Exception derivedIndexRollbackException = null;
			bool compensationSucceeded = true;
			string compensationError = string.Empty;
			try
			{
				if (hadPrevious)
				{
					_activePolicyEffects[effectId] = previousRaw;
				}
				else
				{
					_activePolicyEffects.Remove(effectId);
				}

				RemovePolicyEffectDerivedIndexesAfterPersistFailure(effectId);
				if (hadPrevious)
				{
					ActivePolicyEffectSaveData previousEffect
						= JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(previousRaw ?? string.Empty);
					if (previousEffect == null)
					{
						throw new InvalidOperationException("previous active policy effect snapshot is invalid");
					}
					_activePolicyEffectRuntimeCache[effectId] = new ActivePolicyEffectRuntimeEntry
					{
						Raw = previousRaw,
						Effect = previousEffect
					};
					RefreshPlayerPolicyMaintenanceRuntimeIndex(previousEffect);
					RefreshPolicyTargetDependencyIndex(effectId, previousEffect);
					RefreshActivePolicyEffectRuntimeIndex(previousEffect);
				}
			}
			catch (Exception rollbackException)
			{
				derivedIndexRollbackException = rollbackException;
				RemovePolicyEffectDerivedIndexesAfterPersistFailure(effectId);
			}
			finally
			{
				compensationSucceeded = TryCompensatePendingDailyPolicyEffects(
					effectId,
					effect,
					out compensationError);
			}
			if (!compensationSucceeded)
			{
				bool recoveryPersisted = TryPersistActivePolicyEffectRecoveryState(
					effectId,
					effect,
					out string recoveryError);
				throw new InvalidOperationException(
					"active policy effect persist failed and daily compensation was incomplete: "
						+ persistException.Message
						+ "; compensation=" + compensationError
						+ "; recoveryPersisted=" + recoveryPersisted.ToString(CultureInfo.InvariantCulture)
						+ (string.IsNullOrWhiteSpace(recoveryError)
							? string.Empty
							: "; recovery=" + recoveryError)
						+ (derivedIndexRollbackException == null
							? string.Empty
							: "; derived-index-rollback=" + derivedIndexRollbackException.Message),
					persistException);
			}
			if (derivedIndexRollbackException != null)
			{
				throw new InvalidOperationException(
					"active policy effect persist failed and derived index rollback was incomplete: "
						+ persistException.Message
						+ "; rollback=" + derivedIndexRollbackException.Message,
					persistException);
			}
			throw;
		}
	}

	// Last-resort persistence for a ScheduledOnce or daily transaction whose normal
	// commit failed. The raw recovery snapshot is authoritative: even when a derived
	// index cannot be rebuilt, keeping the receipt/compensation marker in the save
	// store prevents a mutation from being replayed after load.
	private bool TryPersistActivePolicyEffectRecoveryState(
		string effectId,
		ActivePolicyEffectSaveData effect,
		out string error)
	{
		error = string.Empty;
		string normalizedEffectId = (effectId ?? string.Empty).Trim();
		if (normalizedEffectId.Length == 0 || effect == null)
		{
			error = "active policy effect recovery state is incomplete";
			return false;
		}
		try
		{
			string raw = JsonConvert.SerializeObject(effect);
			_activePolicyEffects[normalizedEffectId] = raw;
			_quarantinedActivePolicyEffectIds.Remove(normalizedEffectId);
			RemovePolicyEffectDerivedIndexesAfterPersistFailure(normalizedEffectId);
			_activePolicyEffectRuntimeCache[normalizedEffectId] = new ActivePolicyEffectRuntimeEntry
			{
				Raw = raw,
				Effect = effect
			};
			if (PolicyEffectActivationCoordinator.HasPendingDailyCompensation(effect.ModuleEffects))
			{
				_policyEffectPendingDailyCompensationEffectIds.Add(normalizedEffectId);
			}
			else
			{
				_policyEffectPendingDailyCompensationEffectIds.Remove(normalizedEffectId);
			}
			try
			{
				RefreshPlayerPolicyMaintenanceRuntimeIndex(effect);
				RefreshPolicyTargetDependencyIndex(normalizedEffectId, effect);
				RefreshActivePolicyEffectRuntimeIndex(effect);
			}
			catch (Exception indexException)
			{
				// The raw marker is already durable. Disable this effect's derived
				// runtime indexes until the next normal load/rebuild rather than
				// reverting to a pre-transaction snapshot that could replay it.
				RemovePolicyEffectDerivedIndexesAfterPersistFailure(normalizedEffectId);
				_activePolicyEffectRuntimeCache[normalizedEffectId] = new ActivePolicyEffectRuntimeEntry
				{
					Raw = raw,
					Effect = effect
				};
				error = "recovery state persisted but derived indexes were disabled: "
					+ indexException.Message;
			}
			return true;
		}
		catch (Exception ex)
		{
			// Serialization itself can fail before the recovery marker reaches the
			// save dictionary. Keep the current session fail-closed by serving the
			// marker-bearing object from the cache for the still-current raw value.
			RemovePolicyEffectDerivedIndexesAfterPersistFailure(normalizedEffectId);
			if (_activePolicyEffects.TryGetValue(normalizedEffectId, out string currentRaw))
			{
				_activePolicyEffectRuntimeCache[normalizedEffectId] = new ActivePolicyEffectRuntimeEntry
				{
					Raw = currentRaw,
					Effect = effect
				};
			}
			if (PolicyEffectActivationCoordinator.HasPendingDailyCompensation(effect.ModuleEffects))
			{
				_policyEffectPendingDailyCompensationEffectIds.Add(normalizedEffectId);
			}
			error = ex.Message;
			return false;
		}
	}

	private void RemovePolicyEffectDerivedIndexesAfterPersistFailure(string effectId)
	{
		_activePolicyEffectRuntimeCache.Remove(effectId);
		_policyTargetStructureDependencyEffectIds.Remove(effectId);
		_policyTargetRelationDependencyEffectIds.Remove(effectId);
		_legacyPolicyTargetRefreshEffectIds.Remove(effectId);
		_policyEffectRuntimeIndex.RemoveInstance(effectId);
		_policyEffectDailyRuntimePlans.Remove(effectId);
		if (_playerPolicyMaintenanceRuntimeIndex.Remove(effectId))
		{
			_playerPolicyMaintenanceSnapshotDirty = true;
		}
	}

	private void RemoveActivePolicyEffect(string effectId)
	{
		if (string.IsNullOrWhiteSpace(effectId))
		{
			return;
		}
		_activePolicyEffects.Remove(effectId);
		_activePolicyEffectRuntimeCache.Remove(effectId);
		_policyTargetStructureDependencyEffectIds.Remove(effectId);
		_policyTargetRelationDependencyEffectIds.Remove(effectId);
		_legacyPolicyTargetRefreshEffectIds.Remove(effectId);
		_policyEffectRuntimeIndex.RemoveInstance(effectId);
		_policyEffectDailyRuntimePlans.Remove(effectId);
		_policyEffectDailyPersistenceTransactions.Remove(effectId);
		_policyEffectPendingDailyCompensationEffectIds.Remove(effectId);
		_quarantinedActivePolicyEffectIds.Remove(effectId);
		if (_playerPolicyMaintenanceRuntimeIndex.Remove(effectId))
		{
			_playerPolicyMaintenanceSnapshotDirty = true;
		}
	}

	private void RefreshPolicyTargetDependencyIndex(string effectId, ActivePolicyEffectSaveData effect)
	{
		string normalizedEffectId = (effectId ?? string.Empty).Trim();
		if (normalizedEffectId.Length == 0)
		{
			return;
		}
		_policyTargetStructureDependencyEffectIds.Remove(normalizedEffectId);
		_policyTargetRelationDependencyEffectIds.Remove(normalizedEffectId);
		_legacyPolicyTargetRefreshEffectIds.Remove(normalizedEffectId);
		if (!IsPolicyEffectWithinDuration(effect))
		{
			return;
		}
		bool hasPlan = false;
		foreach (PolicyEffectInstanceSaveData instance in effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
		{
			List<PolicyTargetPlanSaveData> plans = PolicyTargetPlanResolver.NormalizePlans(instance?.TargetSet?.TargetPlans);
			if (plans.Count > 0)
			{
				hasPlan = true;
				PolicyTargetPlanDependencies dependencies = plans.Aggregate(
					PolicyTargetPlanDependencies.None,
					(current, plan) => current | plan.Dependencies);
				if ((dependencies & PolicyTargetPlanDependencies.Structure) != 0)
				{
					_policyTargetStructureDependencyEffectIds.Add(normalizedEffectId);
				}
				if ((dependencies & PolicyTargetPlanDependencies.Relation) != 0)
				{
					_policyTargetRelationDependencyEffectIds.Add(normalizedEffectId);
				}
			}
		}
		if (!hasPlan)
		{
			// Canonical-only and legacy selector saves retain their historical
			// structure refresh path, but diplomacy events no longer scan them.
			_legacyPolicyTargetRefreshEffectIds.Add(normalizedEffectId);
		}
	}

	private void ProcessActivePolicyEffects(int currentDay)
	{
		if (_pendingActivePolicyEffectWork.Count <= 0)
		{
			return;
		}
		long startTimestamp = Stopwatch.GetTimestamp();
		double budgetMs = GetActivePolicyMaintenanceFrameBudgetMs();
		while (_pendingActivePolicyEffectWork.Count > 0 && !IsActivePolicyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
		{
			PendingActivePolicyEffectWork work = _pendingActivePolicyEffectWork.Peek();
			if (work == null || work.RuntimeGeneration != _activePolicyRuntimeGeneration)
			{
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			string key = (work.EffectId ?? "").Trim();
			if (!_activePolicyEffects.TryGetValue(key, out string raw) || string.IsNullOrWhiteSpace(raw))
			{
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			ActivePolicyEffectSaveData activeEffect = null;
			try
			{
				activeEffect = GetActivePolicyEffectForWork(key, raw);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("daily-load-skip", "active effect parse failed key=" + key + " error=" + ex.Message);
				QuarantineActivePolicyEffect(key, raw, "daily-load: " + ex.Message);
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (activeEffect == null || string.IsNullOrWhiteSpace(activeEffect.EffectId))
			{
				QuarantineActivePolicyEffect(key, raw, "daily-load: active effect identity is missing");
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			bool hasPendingDailyCompensation = _policyEffectPendingDailyCompensationEffectIds.Contains(key)
				&& PolicyEffectActivationCoordinator.HasPendingDailyCompensation(
					activeEffect.ModuleEffects);
			if (hasPendingDailyCompensation)
			{
				bool compensationResumed = PolicyEffectActivationCoordinator.TryResumeDailyCompensation(
					activeEffect.ModuleEffects,
					activeEffect.ExecutionReceipts,
					BannerlordPolicyEffectGameBridge.Instance,
					currentDay,
					out _,
					out string resumeError);
				try
				{
					// The resumed state, including a still-pending marker and its
					// updated error, must be durable before any other lifecycle work.
					PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				}
				catch (Exception persistException)
				{
					resumeError = FirstNonEmpty(resumeError, "daily compensation resume persist failed")
						+ "; persist=" + persistException.Message;
					bool recoveryPersisted = TryPersistActivePolicyEffectRecoveryState(
						key,
						activeEffect,
						out string recoveryError);
					resumeError += "; recoveryPersisted="
						+ recoveryPersisted.ToString(CultureInfo.InvariantCulture)
						+ (string.IsNullOrWhiteSpace(recoveryError)
							? string.Empty
							: "; recovery=" + recoveryError);
				}
				if (!compensationResumed || !string.IsNullOrWhiteSpace(resumeError))
				{
					PolicySystemLog.Failure("Effect", "daily-compensation-resume",
						FirstNonEmpty(resumeError, "daily compensation remains pending"),
						"effectId=" + (activeEffect.EffectId ?? key)
						+ " day=" + currentDay.ToString(CultureInfo.InvariantCulture)
						+ " completed=" + compensationResumed.ToString(CultureInfo.InvariantCulture));
				}
				PolicySystemLog.Transaction(
					(activeEffect.EffectId ?? key) + ":daily-compensation-resume",
					activeEffect.RecordId,
					activeEffect.EffectId ?? key,
					string.Empty,
					compensationResumed && string.IsNullOrWhiteSpace(resumeError)
						? "compensationCompleted" : "compensationPending",
					compensationResumed && string.IsNullOrWhiteSpace(resumeError) ? "success" : "failed",
					errorKind: compensationResumed && string.IsNullOrWhiteSpace(resumeError)
						? string.Empty : "DailyCompensationResumeFailure",
					stateBefore: "compensationPending",
					stateAfter: compensationResumed && string.IsNullOrWhiteSpace(resumeError)
						? "compensated" : "compensationPending");
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			_policyEffectPendingDailyCompensationEffectIds.Remove(key);
			if (IsPolicyEffectRollbackPending(activeEffect))
			{
				ProcessPendingPolicyEffectRollback(key, activeEffect, currentDay);
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (PolicyEffectActivationCoordinator.HasPendingScheduledCompensation(
				activeEffect.ModuleEffects))
			{
				bool compensationResumed = PolicyEffectActivationCoordinator.TryResumeScheduledCompensation(
					activeEffect.ModuleEffects,
					activeEffect.ExecutionReceipts,
					BannerlordPolicyEffectGameBridge.Instance,
					currentDay,
					out _,
					out string resumeError);
				try
				{
					// Persist either the cleared marker or the updated pending error before
					// allowing this bundle to participate in a later daily pass.
					PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				}
				catch (Exception persistException)
				{
					resumeError = FirstNonEmpty(resumeError, "scheduled compensation resume persist failed")
						+ "; persist=" + persistException.Message;
					bool recoveryPersisted = TryPersistActivePolicyEffectRecoveryState(
						key,
						activeEffect,
						out string recoveryError);
					resumeError += "; recoveryPersisted="
						+ recoveryPersisted.ToString(CultureInfo.InvariantCulture)
						+ (string.IsNullOrWhiteSpace(recoveryError)
							? string.Empty
							: "; recovery=" + recoveryError);
				}
				if (!compensationResumed || !string.IsNullOrWhiteSpace(resumeError))
				{
					PolicySystemLog.Failure("Effect", "scheduled-compensation-resume",
						FirstNonEmpty(resumeError, "scheduled compensation remains pending"),
						"effectId=" + (activeEffect.EffectId ?? key)
						+ " day=" + currentDay.ToString(CultureInfo.InvariantCulture)
						+ " completed=" + compensationResumed.ToString(CultureInfo.InvariantCulture));
				}
				PolicySystemLog.Transaction(
					(activeEffect.EffectId ?? key) + ":scheduled-compensation-resume",
					activeEffect.RecordId,
					activeEffect.EffectId ?? key,
					string.Empty,
					compensationResumed && string.IsNullOrWhiteSpace(resumeError)
						? "compensationCompleted" : "compensationPending",
					compensationResumed && string.IsNullOrWhiteSpace(resumeError) ? "success" : "failed",
					errorKind: compensationResumed && string.IsNullOrWhiteSpace(resumeError)
						? string.Empty : "ScheduledCompensationResumeFailure",
					stateBefore: "compensationPending",
					stateAfter: compensationResumed && string.IsNullOrWhiteSpace(resumeError)
						? "compensated" : "compensationPending");
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (!activeEffect.IsPermanentEffect && activeEffect.RemainingDays <= 0)
			{
				TryDispatchPolicyEffectLifecycleForActiveEffect(
					activeEffect,
					PolicyEffectLifecycleEventKind.Expired,
					(activeEffect.EffectId ?? key) + ":expired",
					out _,
					out _);
				activeEffect.Ended = true;
				activeEffect.EndReason = FirstNonEmpty(activeEffect.EndReason, "持续时间已结束");
				UpdatePolicyRecordEffectProgress(activeEffect);
				if (IsLocalActivePolicyEffect(activeEffect))
				{
					if (!IsMentionedLocalPolicyEffect(activeEffect))
					{
						MarkPlayerLocalPolicyEffectEnded(activeEffect, LocalPolicyStatusExpired, "自然到期");
					}
					else
					{
						UpdatePolicyRecordEffectProgress(activeEffect);
					}
				}
				else if (IsVassalActivePolicyEffect(activeEffect))
				{
					if (IsSourceVassalPolicyEffect(activeEffect))
					{
						MarkLocalPolicyEnded(activeEffect, LocalPolicyStatusExpired, "自然到期");
					}
				}
				else
				{
					MarkPolicyRecordEffectEnded(activeEffect, "持续时间已结束");
				}
				PolicySystemLog.Lifecycle("Effect", "expiry-complete", "success", new PolicyLogContext
				{
					TransactionId = (activeEffect.EffectId ?? key) + ":expiry",
					RecordId = activeEffect.RecordId,
					EffectId = activeEffect.EffectId ?? key,
					CampaignDay = currentDay,
					StateBefore = "active",
					StateAfter = "ended"
				});
				PolicySystemLog.Transaction(
					(activeEffect.EffectId ?? key) + ":ended",
					activeEffect.RecordId,
					activeEffect.EffectId ?? key,
					string.Empty,
					"ended",
					"success",
					stateBefore: "active",
					stateAfter: "ended");
				RemoveActivePolicyEffect(key);
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			PendingActivePolicyApplicationSaveData pending = activeEffect.PendingApplication;
			if (pending == null && (currentDay <= activeEffect.SubmittedDay || activeEffect.LastEffectProcessedDay >= currentDay))
			{
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (pending == null
				&& activeEffect.MaintenanceChargeEnabled
				&& activeEffect.DailyMaintenanceGoldCost > 0
				&& activeEffect.LastMaintenanceSettlementDay < currentDay)
			{
				// Wait for the player's vanilla clan finance settlement; it is the only
				// state-changing maintenance payment entry point.
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (pending == null
				&& activeEffect.MaintenanceChargeEnabled
				&& activeEffect.DailyMaintenanceGoldCost > 0
				&& !activeEffect.MaintenanceFunded)
			{
				activeEffect.LastEffectProcessedDay = currentDay;
				activeEffect.RemainingDays = PlayerPolicyMaintenancePlanner.AdvanceEffectDay(
					activeEffect.IsPermanentEffect,
					activeEffect.RemainingDays);
				bool unpaidEnded = !activeEffect.IsPermanentEffect && activeEffect.RemainingDays <= 0;
				activeEffect.Ended = unpaidEnded;
				activeEffect.EndReason = unpaidEnded ? "效果期限结束" : string.Empty;
				UpdatePolicyRecordEffectProgress(activeEffect);
				UpdatePlayerPolicyMaintenanceRecord(activeEffect);
				if (unpaidEnded)
				{
					TryDispatchPolicyEffectLifecycleForActiveEffect(
						activeEffect,
						PolicyEffectLifecycleEventKind.Expired,
						(activeEffect.EffectId ?? key) + ":expired",
						out _,
						out _);
					if (IsLocalActivePolicyEffect(activeEffect) && !IsMentionedLocalPolicyEffect(activeEffect))
					{
						MarkPlayerLocalPolicyEffectEnded(activeEffect, LocalPolicyStatusExpired, "自然到期");
					}
					else if (!IsVassalActivePolicyEffect(activeEffect))
					{
						MarkPolicyRecordEffectEnded(activeEffect, "效果期限结束", queueNaturalExpiry: false);
					}
					PolicySystemLog.Lifecycle("Effect", "expiry-complete", "success", new PolicyLogContext
					{
						TransactionId = (activeEffect.EffectId ?? key) + ":expiry",
						RecordId = activeEffect.RecordId,
						EffectId = activeEffect.EffectId ?? key,
						CampaignDay = currentDay,
						StateBefore = "active-unfunded",
						StateAfter = "ended"
					});
					RemoveActivePolicyEffect(key);
				}
				else
				{
					PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				}
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			bool isLocalEffect = IsLocalActivePolicyEffect(activeEffect);
			bool runtimeStructureChanged = false;
			List<Settlement> localSourceFiefs = null;
			List<string> previousLocalSettlementIds = null;
			Kingdom targetKingdom = null;
			if (isLocalEffect)
			{
				previousLocalSettlementIds = NormalizeIdList(activeEffect.TargetSettlementIds);
				localSourceFiefs = ResolveOwnedLocalPolicyFiefs(GetLocalPolicySourceFiefIds(activeEffect.RecordId));
				if (localSourceFiefs.Count <= 0)
				{
					activeEffect.EndReason = "全部目标封地已经失去";
					DispatchPolicyEffectAbolishedBeforeRemoval(
						activeEffect,
						"effect:" + FirstNonEmpty(activeEffect.EffectId, key) + ":target_lost",
						"target_lost");
					MarkPlayerLocalPolicyEffectEnded(activeEffect, LocalPolicyStatusTargetsLost, activeEffect.EndReason);
					RemoveActivePolicyEffect(key);
					CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
					continue;
				}
				if (IsMentionedLocalPolicyEffect(activeEffect))
				{
					List<Settlement> mentionedSettlements = ResolveLocalMentionedPolicySettlements(
						activeEffect.TargetClanIds,
						activeEffect.DirectTargetSettlementIds,
						activeEffect.FollowCurrentRulingClan,
						localSourceFiefs);
					activeEffect.TargetFiefIds = mentionedSettlements
						.Where(x => x.IsTown || x.IsCastle)
						.Select(x => x.StringId)
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToList();
					activeEffect.TargetSettlementIds = mentionedSettlements
						.Select(x => x.StringId)
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToList();
					activeEffect.TargetLabel = BuildLocalPolicyEffectTargetLabel(
						activeEffect.LocalTargetScope,
						activeEffect.TargetHandle,
						activeEffect.TargetLabel,
						activeEffect.TargetClanIds,
						activeEffect.DirectTargetSettlementIds,
						activeEffect.FollowCurrentRulingClan,
						mentionedSettlements);
				}
				else
				{
					List<string> previousFiefIds = NormalizeIdList(activeEffect.TargetFiefIds);
					List<Settlement> ownedFiefs = localSourceFiefs;
					activeEffect.TargetFiefIds = ownedFiefs.Select(x => x.StringId).ToList();
					List<Settlement> sourceSettlements = ExpandLocalPolicySettlements(ownedFiefs);
					activeEffect.TargetSettlementIds = sourceSettlements.Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
					activeEffect.TargetLabel = BuildLocalPolicyEffectTargetLabel(
						activeEffect.LocalTargetScope,
						activeEffect.TargetHandle,
						activeEffect.TargetLabel,
						Array.Empty<string>(),
						Array.Empty<string>(),
						false,
						sourceSettlements);
					if (previousFiefIds.Count != activeEffect.TargetFiefIds.Count)
					{
						UpdateLocalPolicyTargets(activeEffect.RecordId, activeEffect.TargetFiefIds);
						InvokeLocalPolicyLifecycleMemoryHook("target_lost", activeEffect.RecordId, activeEffect.TargetFiefIds);
					}
					if (activeEffect.TargetFiefIds.Count <= 0)
					{
						activeEffect.EndReason = "全部目标封地已经失去";
						DispatchPolicyEffectAbolishedBeforeRemoval(
							activeEffect,
							"effect:" + FirstNonEmpty(activeEffect.EffectId, key) + ":target_lost",
							"target_lost");
						MarkPlayerLocalPolicyEffectEnded(activeEffect, LocalPolicyStatusTargetsLost, activeEffect.EndReason);
						RemoveActivePolicyEffect(key);
						CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
						continue;
					}
				}
				targetKingdom = ResolveKingdomByIdOrName(
					activeEffect.TargetKingdomId,
					activeEffect.TargetKingdomName);
				runtimeStructureChanged |= RefreshActivePolicyEffectCanonicalTargets(
					activeEffect,
					localSourceFiefs,
					targetKingdom,
					dailyTick: true);
				activeEffect.TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(activeEffect.ModuleEffects);
				runtimeStructureChanged |= !HaveSamePolicyTargetIds(
					previousLocalSettlementIds,
					activeEffect.TargetSettlementIds);
			}
			else
			{
				targetKingdom = ResolveKingdomByIdOrName(activeEffect.TargetKingdomId, activeEffect.TargetKingdomName);
			}
			if (!isLocalEffect && (targetKingdom == null || targetKingdom.IsEliminated))
			{
				activeEffect.RemainingDays = 0;
				activeEffect.Ended = true;
				activeEffect.EndReason = "目标王国不存在或已经消亡";
				string missingTargetEvent = targetKingdom?.IsEliminated == true ? "kingdom_destroyed" : "target_lost";
				DispatchPolicyEffectAbolishedBeforeRemoval(
					activeEffect,
					"effect:" + FirstNonEmpty(activeEffect.EffectId, key) + ":" + missingTargetEvent,
					missingTargetEvent);
				if (IsSourceVassalPolicyEffect(activeEffect))
				{
					MarkLocalPolicyEnded(activeEffect, LocalPolicyStatusTargetsLost, activeEffect.EndReason);
				}
				else if (IsVassalActivePolicyEffect(activeEffect))
				{
					UpdatePolicyRecordEffectProgress(activeEffect);
				}
				else
				{
					MarkPolicyRecordEffectEnded(activeEffect, activeEffect.EndReason);
				}
				RemoveActivePolicyEffect(key);
				PolicyDebugLog("daily-ended-missing-target", "effectId=" + activeEffect.EffectId
					+ " recordId=" + (activeEffect.RecordId ?? "")
					+ " target=" + (activeEffect.TargetKingdomName ?? activeEffect.TargetKingdomId ?? ""));
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (!isLocalEffect)
			{
				List<string> previousSettlementIds = NormalizeIdList(activeEffect.TargetSettlementIds);
				runtimeStructureChanged |= RefreshKingdomPolicyEffectCanonicalTargets(
					activeEffect,
					targetKingdom,
					dailyTick: true);
				activeEffect.TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(activeEffect.ModuleEffects);
				runtimeStructureChanged |= !HaveSamePolicyTargetIds(
					previousSettlementIds,
					activeEffect.TargetSettlementIds);
			}
			if (runtimeStructureChanged)
			{
				RefreshActivePolicyEffectRuntimeIndex(activeEffect);
			}
			string scheduledPolicyId = activeEffect.ModuleEffects != null && activeEffect.ModuleEffects.Count > 0
				? activeEffect.ModuleEffects[0]?.PolicyId
				: null;
			string scheduledTransactionId = FirstNonEmpty(scheduledPolicyId, activeEffect.EffectId, key)
				+ ":scheduled:" + currentDay.ToString(CultureInfo.InvariantCulture);
			if (!PolicyEffectActivationCoordinator.TryExecuteScheduledOnce(
				activeEffect.ModuleEffects,
				activeEffect.ExecutionReceipts,
				BannerlordPolicyEffectGameBridge.Instance,
				currentDay,
				out List<string> scheduledCompletedInstanceIds,
				out bool scheduledStateChanged,
				out string scheduledError))
			{
				try
				{
					PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				}
				catch (Exception persistException)
				{
					scheduledError += "; persist=" + persistException.Message;
					if (PolicyEffectActivationCoordinator.HasPendingScheduledCompensation(
						activeEffect.ModuleEffects))
					{
						bool recoveryPersisted = TryPersistActivePolicyEffectRecoveryState(
							key,
							activeEffect,
							out string recoveryError);
						scheduledError += "; recoveryPersisted="
							+ recoveryPersisted.ToString(CultureInfo.InvariantCulture)
							+ (string.IsNullOrWhiteSpace(recoveryError)
								? string.Empty
								: "; recovery=" + recoveryError);
					}
				}
				PolicySystemLog.Failure("Effect", "scheduled-once-failed", scheduledError,
					"effectId=" + (activeEffect.EffectId ?? key)
					+ " day=" + currentDay.ToString(CultureInfo.InvariantCulture));
				PolicySystemLog.Lifecycle("Effect", "scheduled-complete", "failed", new PolicyLogContext
				{
					TransactionId = scheduledTransactionId,
					RecordId = activeEffect.RecordId,
					EffectId = activeEffect.EffectId ?? key,
					CampaignDay = currentDay,
					ErrorKind = "ScheduledExecutionFailure",
					MessageChars = scheduledError?.Length ?? 0,
					MessageHash = PolicySystemLog.HashSensitive(scheduledError),
					StateBefore = "active",
					StateAfter = "retryable"
				});
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
				continue;
			}
			if (scheduledStateChanged)
			{
				try
				{
					// Commit scheduled receipts before any daily mutation is allowed to run.
					PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				}
				catch (Exception persistException)
				{
					bool compensationSucceeded = PolicyEffectActivationCoordinator.TryCompensateScheduledOnceAfterPersistenceFailure(
						activeEffect.ModuleEffects,
						activeEffect.ExecutionReceipts,
						scheduledCompletedInstanceIds,
						BannerlordPolicyEffectGameBridge.Instance,
						currentDay,
						out _,
						out string compensationError);
					bool recoveryPersisted = TryPersistActivePolicyEffectRecoveryState(
						key,
						activeEffect,
						out string recoveryError);
					PolicySystemLog.Failure("Effect", "scheduled-once-persist-failed",
						persistException.Message
							+ (string.IsNullOrWhiteSpace(compensationError) ? string.Empty : "; compensation=" + compensationError)
							+ (string.IsNullOrWhiteSpace(recoveryError) ? string.Empty : "; recovery=" + recoveryError),
						"effectId=" + (activeEffect.EffectId ?? key)
						+ " day=" + currentDay.ToString(CultureInfo.InvariantCulture)
						+ " compensated=" + compensationSucceeded.ToString(CultureInfo.InvariantCulture)
						+ " recoveryPersisted=" + recoveryPersisted.ToString(CultureInfo.InvariantCulture));
					PolicySystemLog.Lifecycle("Effect", "scheduled-complete", "failed", new PolicyLogContext
					{
						TransactionId = scheduledTransactionId,
						RecordId = activeEffect.RecordId,
						EffectId = activeEffect.EffectId ?? key,
						CampaignDay = currentDay,
						ErrorKind = "ScheduledPersistenceFailure",
						ExceptionType = persistException.GetType().FullName,
						HResult = persistException.HResult,
						MessageChars = persistException.Message?.Length ?? 0,
						MessageHash = PolicySystemLog.HashSensitive(persistException.Message),
						StateBefore = "executed",
						StateAfter = compensationSucceeded ? "compensated" : "compensationPending",
						Counts = new Dictionary<string, int>(StringComparer.Ordinal)
						{
							["recoveryPersisted"] = recoveryPersisted ? 1 : 0
						}
					});
					CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
					continue;
				}
			}
			if (scheduledCompletedInstanceIds != null && scheduledCompletedInstanceIds.Count > 0)
			{
				PolicySystemLog.Lifecycle("Effect", "scheduled-complete", "success", new PolicyLogContext
				{
					TransactionId = scheduledTransactionId,
					RecordId = activeEffect.RecordId,
					EffectId = activeEffect.EffectId ?? key,
					CampaignDay = currentDay,
					Counts = new Dictionary<string, int>(StringComparer.Ordinal)
					{
						["completedInstances"] = scheduledCompletedInstanceIds.Count
					}
				});
			}
			if (pending == null)
			{
				activeEffect.PendingApplication = isLocalEffect
					? CreatePendingLocalPolicyApplication(activeEffect, currentDay)
					: CreatePendingActivePolicyApplication(targetKingdom, activeEffect, currentDay);
				FreezePendingNonSettlementDailyEntries(activeEffect, activeEffect.PendingApplication);
				PolicySystemLog.Lifecycle("Effect", "daily-start", "started", new PolicyLogContext
				{
					TransactionId = (activeEffect.EffectId ?? key) + ":daily:" + currentDay.ToString(CultureInfo.InvariantCulture),
					RecordId = activeEffect.RecordId,
					EffectId = activeEffect.EffectId ?? key,
					CampaignDay = currentDay,
					StateBefore = "active",
					StateAfter = "dailyPending"
				});
				PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				return;
			}
			if (pending.Day <= activeEffect.LastAppliedDay)
			{
				activeEffect.PendingApplication = null;
				PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: true, activeEffect: activeEffect);
				continue;
			}
			pending.SettlementIds = pending.SettlementIds ?? new List<string>();
			NarrowPendingSettlementTargetsToRuntimePlan(activeEffect, pending);
			FreezePendingNonSettlementDailyEntries(activeEffect, pending);
			pending.AppliedEffect = pending.AppliedEffect ?? CreateAppliedKingdomEffect(targetKingdom, activeEffect);
			pending.AppliedEffect.DetailLines = pending.AppliedEffect.DetailLines ?? new List<string>();
			if (pending.NextSettlementIndex < pending.SettlementIds.Count)
			{
				long applyTimestamp = Stopwatch.GetTimestamp();
				int processedSettlementCount = 0;
				string lastSettlementId = "";
				string settlementExecutionError = string.Empty;
				using (PerfProbe.Scope("CustomPolicy.ApplyActiveEffectToKingdom"))
				{
					while (pending.NextSettlementIndex < pending.SettlementIds.Count
						&& processedSettlementCount < ActivePolicySettlementBatchSize
						&& (processedSettlementCount == 0 || !IsActivePolicyMaintenanceBudgetExceeded(startTimestamp, budgetMs)))
					{
						lastSettlementId = pending.SettlementIds[pending.NextSettlementIndex];
						Settlement settlement = ResolveSettlementById(lastSettlementId);
						if (!ApplyActiveEffectToSettlement(
							settlement, activeEffect, pending.AppliedEffect, pending.Day, out settlementExecutionError))
						{
							break;
						}
						pending.NextSettlementIndex++;
						processedSettlementCount++;
					}
				}
				if (!string.IsNullOrWhiteSpace(settlementExecutionError))
				{
					if (!TryCompensatePendingDailyPolicyEffects(key, activeEffect, out string compensationError))
					{
						activeEffect.PendingApplication = pending;
						TryPersistActivePolicyEffectRecoveryState(key, activeEffect, out string recoveryError);
						PolicySystemLog.Failure("Effect", "daily-compensation-pending",
							settlementExecutionError + "; compensation=" + compensationError
								+ (string.IsNullOrWhiteSpace(recoveryError) ? string.Empty : "; recovery=" + recoveryError),
							"effectId=" + (activeEffect.EffectId ?? key) + " target=" + lastSettlementId);
					}
					else
					{
						activeEffect.PendingApplication = pending;
						PersistActivePolicyEffect(key, activeEffect, structureChanged: true);
					}
					CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
					continue;
				}
				if (processedSettlementCount > 0)
				{
					activeEffect.PendingApplication = pending;
					PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
					LogActivePolicyStageIfOverBudget("CustomPolicy.ApplyActiveEffectToKingdom", applyTimestamp, budgetMs, activeEffect.EffectId, lastSettlementId);
					return;
				}
			}
			if (!TryExecuteNonSettlementDailyPolicyEffects(
				activeEffect,
				pending,
				currentDay,
				startTimestamp,
				budgetMs,
				out string nonSettlementError,
				out bool compensateNonSettlementBatch))
			{
				if (compensateNonSettlementBatch
					&& !TryCompensatePendingDailyPolicyEffects(key, activeEffect, out string compensationError))
				{
					activeEffect.PendingApplication = pending;
					bool recoveryPersisted = TryPersistActivePolicyEffectRecoveryState(
						key,
						activeEffect,
						out string recoveryError);
					PolicySystemLog.Failure("Effect", "daily-compensation-pending",
						FirstNonEmpty(nonSettlementError, "non-settlement daily effect failed")
							+ "; compensation=" + compensationError
							+ "; recoveryPersisted=" + recoveryPersisted.ToString(CultureInfo.InvariantCulture)
							+ (string.IsNullOrWhiteSpace(recoveryError)
								? string.Empty
								: "; recovery=" + recoveryError),
						"effectId=" + (activeEffect.EffectId ?? key)
						+ " day=" + pending.Day.ToString(CultureInfo.InvariantCulture));
					CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
					continue;
				}
				activeEffect.PendingApplication = pending;
				PersistActivePolicyEffect(key, activeEffect, structureChanged: false);
				if (!string.IsNullOrWhiteSpace(nonSettlementError))
				{
					PolicySystemLog.Failure("Effect", "daily-non-settlement-failed",
						nonSettlementError,
						"effectId=" + (activeEffect.EffectId ?? key)
						+ " day=" + pending.Day.ToString(CultureInfo.InvariantCulture));
					CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: false);
					continue;
				}
				return;
			}
			LogDailyPolicyEffectCompletion(activeEffect, pending.Day);
			AppliedKingdomEffect actual = pending.AppliedEffect;
			activeEffect.LastAppliedDay = pending.Day;
			activeEffect.LastEffectProcessedDay = pending.Day;
			activeEffect.RemainingDays = PlayerPolicyMaintenancePlanner.AdvanceEffectDay(
				activeEffect.IsPermanentEffect,
				activeEffect.RemainingDays);
			bool ended = !activeEffect.IsPermanentEffect && activeEffect.RemainingDays <= 0;
			activeEffect.Ended = ended;
			activeEffect.EndReason = ended ? "持续时间结束" : "";
			activeEffect.PendingApplication = null;
			if (ended)
			{
				TryDispatchPolicyEffectLifecycleForActiveEffect(
					activeEffect,
					PolicyEffectLifecycleEventKind.Expired,
					(activeEffect.EffectId ?? key) + ":expired",
					out _,
					out _);
			}
			UpdatePolicyRecordEffectProgress(activeEffect);
			UpdatePlayerPolicyMaintenanceRecord(activeEffect);
			if (ended)
			{
				PolicySystemLog.Lifecycle("Effect", "expiry-complete", "success", new PolicyLogContext
				{
					TransactionId = (activeEffect.EffectId ?? key) + ":expiry",
					RecordId = activeEffect.RecordId,
					EffectId = activeEffect.EffectId ?? key,
					CampaignDay = pending.Day,
					StateBefore = "active",
					StateAfter = "ended"
				});
				PolicySystemLog.Transaction(
					(activeEffect.EffectId ?? key) + ":ended",
					activeEffect.RecordId,
					activeEffect.EffectId ?? key,
					string.Empty,
					"ended",
					"success",
					stateBefore: "active",
					stateAfter: "ended");
				RemoveActivePolicyEffect(key);
				if (isLocalEffect)
				{
					if (!IsMentionedLocalPolicyEffect(activeEffect))
					{
						MarkPlayerLocalPolicyEffectEnded(activeEffect, LocalPolicyStatusExpired, "自然到期");
						InvokeLocalPolicyLifecycleMemoryHook("expired", activeEffect.RecordId, activeEffect.TargetFiefIds);
					}
				}
				else if (IsVassalActivePolicyEffect(activeEffect))
				{
					if (IsSourceVassalPolicyEffect(activeEffect))
					{
						MarkLocalPolicyEnded(activeEffect, LocalPolicyStatusExpired, "自然到期");
					}
				}
				else
				{
					MarkPolicyRecordEffectEnded(activeEffect, "效果期限结束");
				}
				PolicyEffectLedgerLog("effect-ended", "recordId=" + (activeEffect.RecordId ?? "")
					+ " effectId=" + (activeEffect.EffectId ?? "")
					+ " reason=" + (activeEffect.EndReason ?? ""));
			}
			PolicyEffectLedgerLog("daily-apply", BuildPolicyEffectLedgerLine(activeEffect.RecordId, activeEffect.EffectId, actual, pending.Day, activeEffect.RemainingDays));
			CompleteActivePolicyEffectWork(work, currentDay, requeueIfStillDue: !ended, activeEffect: activeEffect);
		}
	}

	private void EnsureActivePolicyEffectWorkScheduled(int currentDay)
	{
		if (_lastActivePolicyScheduledDay == currentDay)
		{
			return;
		}
		_lastActivePolicyScheduledDay = currentDay;
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			if (_quarantinedActivePolicyEffectIds.Contains(item.Key))
			{
				continue;
			}
			ActivePolicyEffectSaveData activeEffect;
			try
			{
				activeEffect = GetActivePolicyEffectForWork(item.Key, item.Value ?? "");
			}
			catch
			{
				continue;
			}
			bool hasPendingDailyCompensation
				= _policyEffectPendingDailyCompensationEffectIds.Contains(item.Key);
			bool hasPendingScheduledCompensation
				= PolicyEffectActivationCoordinator.HasPendingScheduledCompensation(activeEffect?.ModuleEffects);
			bool hasPendingRollback = IsPolicyEffectRollbackPending(activeEffect);
			if (activeEffect == null
				|| string.IsNullOrWhiteSpace(activeEffect.EffectId)
				|| (!hasPendingDailyCompensation
					&& !hasPendingScheduledCompensation
					&& !hasPendingRollback
					&& !IsPolicyEffectWithinDuration(activeEffect)))
			{
				continue;
			}
			bool pending = activeEffect.PendingApplication != null && activeEffect.PendingApplication.Day > activeEffect.LastAppliedDay;
			bool dueToday = currentDay > activeEffect.SubmittedDay && activeEffect.LastEffectProcessedDay < currentDay;
			bool rollbackDue = hasPendingRollback && activeEffect.LastAppliedDay < currentDay;
			if (hasPendingDailyCompensation
				|| hasPendingScheduledCompensation
				|| (hasPendingRollback ? rollbackDue : pending || dueToday))
			{
				EnqueueActivePolicyEffectWork(activeEffect.EffectId);
			}
		}
	}

	private void EnqueueActivePolicyEffectWork(string effectId)
	{
		string key = (effectId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(key) || !_queuedActivePolicyEffectIds.Add(key))
		{
			return;
		}
		_pendingActivePolicyEffectWork.Enqueue(new PendingActivePolicyEffectWork
		{
			EffectId = key,
			RuntimeGeneration = _activePolicyRuntimeGeneration
		});
	}

	private void CompleteActivePolicyEffectWork(PendingActivePolicyEffectWork work, int currentDay, bool requeueIfStillDue, ActivePolicyEffectSaveData activeEffect = null)
	{
		if (_pendingActivePolicyEffectWork.Count > 0 && object.ReferenceEquals(_pendingActivePolicyEffectWork.Peek(), work))
		{
			_pendingActivePolicyEffectWork.Dequeue();
		}
		string effectId = (work?.EffectId ?? activeEffect?.EffectId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(effectId))
		{
			_queuedActivePolicyEffectIds.Remove(effectId);
		}
		if (requeueIfStillDue
			&& IsPolicyEffectWithinDuration(activeEffect)
			&& currentDay > activeEffect.SubmittedDay
			&& activeEffect.LastEffectProcessedDay < currentDay)
		{
			EnqueueActivePolicyEffectWork(activeEffect.EffectId);
		}
	}

	private PendingActivePolicyApplicationSaveData CreatePendingActivePolicyApplication(Kingdom kingdom, ActivePolicyEffectSaveData activeEffect, int currentDay)
	{
		return new PendingActivePolicyApplicationSaveData
		{
			Day = currentDay,
			SettlementIds = GetDailyRuntimeSettlementTargetIds(activeEffect),
			NextSettlementIndex = 0,
			NextNonSettlementIndex = 0,
			AppliedEffect = CreateAppliedKingdomEffect(kingdom, activeEffect)
		};
	}

	private PendingActivePolicyApplicationSaveData CreatePendingLocalPolicyApplication(ActivePolicyEffectSaveData activeEffect, int currentDay)
	{
		return new PendingActivePolicyApplicationSaveData
		{
			Day = currentDay,
			SettlementIds = GetDailyRuntimeSettlementTargetIds(activeEffect),
			NextSettlementIndex = 0,
			NextNonSettlementIndex = 0,
			AppliedEffect = CreateAppliedKingdomEffect(GetPlayerKingdom(), activeEffect)
		};
	}

	private List<string> GetDailyRuntimeSettlementTargetIds(ActivePolicyEffectSaveData activeEffect)
	{
		return _policyEffectDailyRuntimePlans.TryGetValue(activeEffect?.EffectId ?? string.Empty, out PolicyEffectDailyRuntimePlan plan)
			? plan.SettlementTargetIds.OrderBy(value => value, StringComparer.Ordinal).ToList()
			: new List<string>();
	}

	private List<string> GetCanonicalDailyRuntimeSettlementTargetIds(ActivePolicyEffectSaveData activeEffect)
	{
		return _policyEffectDailyRuntimePlans.TryGetValue(activeEffect?.EffectId ?? string.Empty, out PolicyEffectDailyRuntimePlan plan)
			? plan.CanonicalSettlementTargetIds.OrderBy(value => value, StringComparer.Ordinal).ToList()
			: new List<string>();
	}

	private void NarrowPendingSettlementTargetsToRuntimePlan(
		ActivePolicyEffectSaveData activeEffect,
		PendingActivePolicyApplicationSaveData pending)
	{
		if (pending == null)
		{
			return;
		}
		HashSet<string> allowed = _policyEffectDailyRuntimePlans.TryGetValue(
			activeEffect?.EffectId ?? string.Empty,
			out PolicyEffectDailyRuntimePlan plan)
			? plan.SettlementTargetIds
			: new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		pending.SettlementIds = (pending.SettlementIds ?? new List<string>())
			.Skip(Math.Max(0, pending.NextSettlementIndex))
			.Where(id => allowed.Contains(id ?? string.Empty))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		pending.NextSettlementIndex = 0;
	}

	private static double GetActivePolicyMaintenanceFrameBudgetMs()
	{
		try
		{
			return Math.Max(1.0, Math.Min(10.0, (DuelSettings.GetSettings()?.DailyMaintenanceFrameBudgetMs).GetValueOrDefault((int)ActivePolicyMaintenanceDefaultFrameBudgetMs)));
		}
		catch
		{
			return ActivePolicyMaintenanceDefaultFrameBudgetMs;
		}
	}

	private static bool IsActivePolicyMaintenanceBudgetExceeded(long startTimestamp, double budgetMs)
	{
		return budgetMs > 0.0 && (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency >= budgetMs;
	}

	private static void LogActivePolicyStageIfOverBudget(string stageName, long startTimestamp, double budgetMs, string effectId, string target)
	{
		double elapsedMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		if (budgetMs > 0.0 && elapsedMs >= budgetMs)
		{
			PolicySystemLog.WriteRuntime("Effect", "active-effect-stage-over-budget stage=" + (stageName ?? "")
				+ " elapsedMs=" + elapsedMs.ToString("0.000", CultureInfo.InvariantCulture)
				+ " budgetMs=" + budgetMs.ToString("0.000", CultureInfo.InvariantCulture)
				+ " effectId=" + (effectId ?? "")
				+ " target=" + (target ?? ""));
		}
	}

	private AppliedKingdomEffect CreateAppliedKingdomEffect(Kingdom kingdom, ActivePolicyEffectSaveData activeEffect)
	{
		bool isLocal = IsLocalActivePolicyEffect(activeEffect);
		List<string> canonicalSettlementIds = GetCanonicalDailyRuntimeSettlementTargetIds(activeEffect);
		return new AppliedKingdomEffect
		{
			ModuleEffects = ClonePolicyEffectSaveDataList(activeEffect?.ModuleEffects),
			ExecutionReceipts = ClonePolicyEffectExecutionReceipts(activeEffect?.ExecutionReceipts),
			EffectId = activeEffect?.EffectId ?? "",
			ScopeKind = isLocal ? PolicyScopeLocal : (IsVassalActivePolicyEffect(activeEffect) ? PolicyScopeVassal : PolicyScopeKingdom),
			LocalTargetScope = isLocal ? GetLocalPolicyTargetScope(activeEffect) : "",
			TargetHandle = activeEffect?.TargetHandle ?? "",
			TargetLabel = activeEffect?.TargetLabel ?? "",
			TargetFiefIds = NormalizeIdList(activeEffect?.TargetFiefIds),
			TargetSettlementIds = NormalizeIdList(activeEffect?.TargetSettlementIds),
			TargetClanIds = NormalizeIdList(activeEffect?.TargetClanIds),
			DirectTargetSettlementIds = NormalizeIdList(activeEffect?.DirectTargetSettlementIds),
			FollowCurrentRulingClan = activeEffect?.FollowCurrentRulingClan == true,
			KingdomId = kingdom?.StringId ?? activeEffect?.TargetKingdomId ?? "",
			KingdomName = isLocal
                ? FirstNonEmpty(activeEffect?.TargetLabel, "所选地方")
				: FirstNonEmpty(activeEffect?.TargetLabel, GetKingdomName(kingdom)),
			DurationDays = activeEffect?.TotalDurationDays ?? 0,
			RemainingDays = activeEffect?.RemainingDays ?? 0,
			TownCount = canonicalSettlementIds.Count(id => ResolveSettlementById(id)?.Town != null),
			VillageCount = canonicalSettlementIds.Count(id => ResolveSettlementById(id)?.Village != null),
			Reason = activeEffect?.Reason ?? ""
		};
	}

	private bool ApplyActiveEffectToSettlement(
		Settlement settlement,
		ActivePolicyEffectSaveData activeEffect,
		AppliedKingdomEffect applied,
		int currentDay,
		out string error)
	{
		error = string.Empty;
		if (settlement == null || applied == null)
		{
			return true;
		}
		return ExecuteDailyPolicyEffectsForSettlement(activeEffect, settlement, currentDay, out error);
	}

	private bool ExecuteDailyPolicyEffectsForSettlement(
		ActivePolicyEffectSaveData activeEffect,
		Settlement settlement,
		int campaignDay,
		out string error)
	{
		error = string.Empty;
		if (activeEffect == null
			|| settlement == null
			|| string.IsNullOrWhiteSpace(settlement.StringId)
			|| !_policyEffectDailyRuntimePlans.TryGetValue(activeEffect.EffectId ?? string.Empty, out PolicyEffectDailyRuntimePlan plan))
		{
			return true;
		}
		if (!ExecuteDailyPolicyEffectTargetEntries(activeEffect, plan, PolicyEffectTargetKind.Settlement, settlement.StringId, campaignDay, out error))
		{
			return false;
		}
		if (settlement.Town != null)
		{
			if (!ExecuteDailyPolicyEffectTargetEntries(activeEffect, plan, PolicyEffectTargetKind.Town, settlement.StringId, campaignDay, out error))
			{
				return false;
			}
		}
		if (settlement.Village != null)
		{
			if (!ExecuteDailyPolicyEffectTargetEntries(activeEffect, plan, PolicyEffectTargetKind.Village, settlement.StringId, campaignDay, out error))
			{
				return false;
			}
		}
		return true;
	}

	private bool ExecuteDailyPolicyEffectTargetEntries(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectDailyRuntimePlan plan,
		PolicyEffectTargetKind targetKind,
		string targetId,
		int campaignDay,
		out string error)
	{
		error = string.Empty;
		string targetKey = BuildDailyPolicyEffectTargetKey(targetKind, targetId);
		if (plan == null || !plan.SettlementEntries.TryGetValue(targetKey, out PolicyEffectDailyRuntimePlanEntry[] entries))
		{
			return true;
		}
		for (int index = 0; index < entries.Length; index++)
		{
			PolicyEffectDailyExecutionOutcome outcome
				= ExecuteDailyPolicyEffectPlanEntry(activeEffect, entries[index], campaignDay);
			if (outcome?.Failed == true)
			{
				MarkDailyMechanismBundleFailed(activeEffect, entries[index].Instance);
				error = FirstNonEmpty(outcome.Error, "settlement daily mechanism execution failed");
				return false;
			}
		}
		return true;
	}

	private static void MarkDailyMechanismBundleFailed(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectInstanceSaveData failedInstance)
	{
		if (activeEffect?.ModuleEffects == null || failedInstance == null)
		{
			return;
		}
		IEnumerable<PolicyEffectInstanceSaveData> affected = failedInstance.MechanismKind == PolicyEffectMechanismKind.Linked
			? activeEffect.ModuleEffects.Where(instance => instance != null
				&& string.Equals(instance.PolicyId ?? string.Empty, failedInstance.PolicyId ?? string.Empty, StringComparison.Ordinal)
				&& string.Equals(instance.MechanismId ?? string.Empty, failedInstance.MechanismId ?? string.Empty, StringComparison.Ordinal))
			: new[] { failedInstance };
		foreach (PolicyEffectInstanceSaveData instance in affected)
		{
			instance.LifecycleState = PolicyEffectLifecycleState.Failed;
		}
	}

	private bool TryExecuteNonSettlementDailyPolicyEffects(
		ActivePolicyEffectSaveData activeEffect,
		PendingActivePolicyApplicationSaveData pending,
		int attemptWindowDay,
		long startTimestamp,
		double budgetMs,
		out string error,
		out bool compensateBatch)
	{
		error = string.Empty;
		compensateBatch = false;
		int campaignDay = pending?.Day ?? 0;
		if (activeEffect == null || pending == null)
		{
			return true;
		}
		_policyEffectDailyRuntimePlans.TryGetValue(
			activeEffect.EffectId ?? string.Empty,
			out PolicyEffectDailyRuntimePlan plan);
		FreezePendingNonSettlementDailyEntries(activeEffect, pending);
		pending.NextNonSettlementIndex = Math.Max(0, Math.Min(
			pending.NextNonSettlementIndex,
			pending.NonSettlementEntryKeys.Count));
		HashSet<string> currentEntryKeys = new HashSet<string>(
			plan?.NonSettlementEntriesByKey?.Keys ?? Enumerable.Empty<string>(),
			StringComparer.OrdinalIgnoreCase);
		if (!TryPreflightFrozenLinkedDailyEntries(
			activeEffect.ModuleEffects,
			pending.NonSettlementEntryKeys,
			currentEntryKeys,
			out error))
		{
			_policyEffectDailyRuntimePlans.Remove(activeEffect.EffectId ?? string.Empty);
			string targetHash = BuildPolicyEffectTargetFingerprint(
				activeEffect.ModuleEffects,
				out int targetCount);
			PolicySystemLog.Transaction(
				(activeEffect.EffectId ?? string.Empty) + ":daily:" + campaignDay.ToString(CultureInfo.InvariantCulture),
				activeEffect.RecordId,
				activeEffect.EffectId,
				string.Empty,
				"suspended",
				"failed",
				errorKind: "FrozenTargetDrift",
				targetHash: targetHash,
				targetCount: targetCount,
				stateBefore: "active",
				stateAfter: "suspended");
			compensateBatch = _policyEffectDailyPersistenceTransactions.TryGetValue(
				activeEffect.EffectId ?? string.Empty,
				out List<PolicyEffectDailyExecutionOutcome> pendingCompensations)
				&& pendingCompensations.Count > 0;
			return false;
		}
		if (pending.NextNonSettlementIndex == 0
			&& pending.SkippedNonSettlementEntryKeys == null
			&& !TryPreflightNonSettlementHeroGold(plan, pending, out error))
		{
			compensateBatch = false;
			return false;
		}
		pending.SkippedNonSettlementEntryKeys ??= new List<string>();
		HashSet<string> skippedEntryKeys = new HashSet<string>(
			pending.SkippedNonSettlementEntryKeys,
			StringComparer.OrdinalIgnoreCase);
		int batchStartIndex = pending.NextNonSettlementIndex;
		string currentMechanismGroup = string.Empty;
		while (pending.NextNonSettlementIndex < pending.NonSettlementEntryKeys.Count)
		{
			string entryKey = pending.NonSettlementEntryKeys[pending.NextNonSettlementIndex] ?? string.Empty;
			if (plan == null || !plan.NonSettlementEntriesByKey.TryGetValue(entryKey, out PolicyEffectDailyRuntimePlanEntry entry))
			{
				// The target left the dynamic plan after this logical day was frozen.
				// Do not execute it and do not replace it with a newly joined target.
				pending.NextNonSettlementIndex++;
				continue;
			}
			string mechanismGroup = BuildDailyMechanismExecutionGroupKey(entry);
			if (pending.NextNonSettlementIndex > batchStartIndex
				&& !string.Equals(mechanismGroup, currentMechanismGroup, StringComparison.Ordinal)
				&& IsActivePolicyMaintenanceBudgetExceeded(startTimestamp, budgetMs))
			{
				return false;
			}
			currentMechanismGroup = mechanismGroup;
			if (skippedEntryKeys.Contains(entryKey))
			{
				RecordSkippedDailyHeroGoldReceipt(activeEffect, entry, campaignDay);
				pending.NextNonSettlementIndex++;
				continue;
			}
			PolicyEffectDailyExecutionOutcome outcome = ExecuteDailyPolicyEffectPlanEntry(
				activeEffect,
				entry,
				campaignDay,
				attemptWindowDay);
			if (outcome?.Failed == true)
			{
				MarkDailyMechanismBundleFailed(activeEffect, entry.Instance);
				error = FirstNonEmpty(outcome.Error, "non-settlement daily effect failed");
				compensateBatch = true;
				pending.NextNonSettlementIndex = batchStartIndex;
				return false;
			}
			pending.NextNonSettlementIndex++;
		}
		return true;
	}

	private static string BuildDailyMechanismExecutionGroupKey(PolicyEffectDailyRuntimePlanEntry entry)
	{
		PolicyEffectInstanceSaveData instance = entry?.Instance;
		return instance?.MechanismKind == PolicyEffectMechanismKind.Linked
			? "linked\u001f" + (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty)
			: "independent\u001f" + (instance?.InstanceId ?? string.Empty);
	}

	private bool TryPreflightNonSettlementHeroGold(
		PolicyEffectDailyRuntimePlan plan,
		PendingActivePolicyApplicationSaveData pending,
		out string error)
	{
		error = string.Empty;
		List<string> skippedEntryKeys = new List<string>();
		if (plan == null)
		{
			pending.SkippedNonSettlementEntryKeys = skippedEntryKeys;
			return true;
		}
		List<KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>> frozenEntries
			= new List<KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>>();
		foreach (string key in pending.NonSettlementEntryKeys ?? new List<string>())
		{
			if (plan.NonSettlementEntriesByKey.TryGetValue(key ?? string.Empty, out PolicyEffectDailyRuntimePlanEntry entry))
			{
				frozenEntries.Add(new KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>(key, entry));
			}
		}

		IEnumerable<IGrouping<string, KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>>> groups
			= frozenEntries.GroupBy(pair => pair.Value.Instance.MechanismKind == PolicyEffectMechanismKind.Linked
				? "linked\u001f" + (pair.Value.Instance.PolicyId ?? string.Empty)
					+ "\u001f" + (pair.Value.Instance.MechanismId ?? string.Empty)
				: "independent\u001f" + (pair.Value.Instance.InstanceId ?? string.Empty),
				StringComparer.Ordinal);
		foreach (IGrouping<string, KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>> group in groups)
		{
			List<KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>> entries = group.ToList();
			List<KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry>> atomic = entries
				.Where(pair => pair.Value.Module is IAtomicHeroGoldPolicyEffectModule)
				.ToList();
			if (atomic.Count == 0)
			{
				continue;
			}
			if (atomic.Count != entries.Count)
			{
				error = "daily Hero gold target batch is incomplete";
				return false;
			}

			if (atomic.Any(pair => pair.Value.Instance.MechanismKind != PolicyEffectMechanismKind.Independent))
			{
				error = "daily Hero gold effects only support independent mechanisms";
				return false;
			}

			bool skipGroup = false;
			foreach (KeyValuePair<string, PolicyEffectDailyRuntimePlanEntry> pair in atomic)
			{
				if (!((IAtomicHeroGoldPolicyEffectModule)pair.Value.Module).TryReadDelta(
					pair.Value.Prepared.Instance.Payload,
					out int delta))
				{
					error = "daily Hero gold payload is invalid";
					return false;
				}
				if (!TryPreflightDailyHeroGold(pair.Value.TargetId, delta, out bool shouldSkip, out error))
				{
					return false;
				}
				skipGroup |= shouldSkip;
			}
			if (skipGroup)
			{
				skippedEntryKeys.AddRange(atomic.Select(pair => pair.Key));
			}
		}
		pending.SkippedNonSettlementEntryKeys = skippedEntryKeys
			.Where(key => !string.IsNullOrWhiteSpace(key))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		return true;
	}

	private static bool TryPreflightDailyHeroGold(
		string heroId,
		int delta,
		out bool shouldSkip,
		out string error)
	{
		shouldSkip = false;
		error = string.Empty;
		if (!BannerlordPolicyEffectGameBridge.Instance.TryReadHeroGold(
			heroId,
			out bool available,
			out int before,
			out string bridgeError))
		{
			error = "daily hero gold preflight failed for " + heroId + ": " + bridgeError;
			return false;
		}
		long after = (long)before + delta;
		shouldSkip = !available || after < 0 || after > int.MaxValue;
		return true;
	}

	private static void RecordSkippedDailyHeroGoldReceipt(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectDailyRuntimePlanEntry entry,
		int campaignDay)
	{
		if (activeEffect == null
			|| entry?.Instance == null
			|| entry.Module is not IAtomicHeroGoldPolicyEffectModule module
			|| !module.TryReadDelta(entry.Prepared.Instance.Payload, out int delta))
		{
			return;
		}
		bool read = BannerlordPolicyEffectGameBridge.Instance.TryReadHeroGold(
			entry.TargetId,
			out bool available,
			out int before,
			out string readError);
		PolicyEffectExecutionReceipt receipt = new PolicyEffectExecutionReceipt
		{
			ReceiptId = entry.Instance.InstanceId + ":daily:" + campaignDay.ToString(CultureInfo.InvariantCulture)
				+ ":" + entry.TargetKind + ":" + entry.TargetId + ":skip",
			InstanceId = entry.Instance.InstanceId,
			PolicyId = entry.Instance.PolicyId,
			ModuleId = entry.Instance.ModuleId,
			TargetSet = entry.Instance.TargetSet,
			Status = PolicyEffectExecutionStatus.Skipped,
			RequestedValue = delta,
			AppliedValue = 0f,
			RequestedPayload = new JObject { ["value"] = delta },
			AppliedPayload = new JObject
			{
				["target"] = new JObject
				{
					["heroId"] = entry.TargetId,
					["requestedDelta"] = delta,
					["available"] = read && available,
					["before"] = read ? before : 0,
					["after"] = read ? before : 0,
					["actualDelta"] = 0,
					["readError"] = readError ?? string.Empty
				}
			},
			CampaignDay = campaignDay,
			Message = "atomic balance preflight skipped"
		};
		entry.Instance.ExecutionReceipt = receipt;
		activeEffect.ExecutionReceipts ??= new List<PolicyEffectExecutionReceipt>();
		activeEffect.ExecutionReceipts.RemoveAll(existing =>
			string.Equals(existing?.InstanceId, entry.Instance.InstanceId, StringComparison.Ordinal));
		activeEffect.ExecutionReceipts.Add(ClonePolicyEffectExecutionReceipt(receipt));
	}

	private void FreezePendingNonSettlementDailyEntries(
		ActivePolicyEffectSaveData activeEffect,
		PendingActivePolicyApplicationSaveData pending)
	{
		if (pending == null || pending.NonSettlementEntryKeys != null)
		{
			return;
		}
		_policyEffectDailyRuntimePlans.TryGetValue(
			activeEffect?.EffectId ?? string.Empty,
			out PolicyEffectDailyRuntimePlan plan);
		pending.NonSettlementEntryKeys = (plan?.NonSettlementEntries
			?? Array.Empty<PolicyEffectDailyRuntimePlanEntry>())
			.OrderBy(entry => entry?.Instance?.MechanismKind == PolicyEffectMechanismKind.Linked
				? "0\u001f" + (entry.Instance.PolicyId ?? string.Empty) + "\u001f" + (entry.Instance.MechanismId ?? string.Empty)
				: "1\u001f" + (entry?.Instance?.InstanceId ?? string.Empty), StringComparer.Ordinal)
			.ThenBy(entry => IsPolicyEffectSourceRole(entry?.Instance?.MechanismRole ?? PolicyEffectMechanismRole.Subject)
				? 0
				: IsPolicyEffectDestinationRole(entry?.Instance?.MechanismRole ?? PolicyEffectMechanismRole.Subject) ? 1 : 2)
			.ThenBy(entry => entry?.Instance?.InstanceId, StringComparer.Ordinal)
			.ThenBy(entry => entry?.TargetId, StringComparer.OrdinalIgnoreCase)
			.Select(BuildDailyPolicyEffectPlanEntryKey)
			.Where(key => !string.IsNullOrWhiteSpace(key))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private PolicyEffectDailyExecutionOutcome ExecuteDailyPolicyEffectPlanEntry(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectDailyRuntimePlanEntry entry,
		int campaignDay,
		int attemptWindowDay = -1)
	{
		if (entry?.Instance == null)
		{
			return null;
		}
		PolicyEffectDailyExecutionOutcome outcome = PolicyEffectActivationCoordinator.ExecuteDailyTarget(
			entry.Instance,
			entry.Module,
			entry.Prepared,
			entry.TargetKind,
			entry.TargetId,
			BannerlordPolicyEffectGameBridge.Instance,
			campaignDay,
			attemptWindowDay);
		if (outcome?.AppliedReceipt != null
			&& outcome.Module is ICompensatingDailyPolicyEffectModule
			&& !string.IsNullOrWhiteSpace(activeEffect?.EffectId))
		{
			if (!_policyEffectDailyPersistenceTransactions.TryGetValue(
				activeEffect.EffectId,
				out List<PolicyEffectDailyExecutionOutcome> pendingCompensations))
			{
				pendingCompensations = new List<PolicyEffectDailyExecutionOutcome>();
				_policyEffectDailyPersistenceTransactions[activeEffect.EffectId] = pendingCompensations;
			}
			pendingCompensations.Add(outcome);
			activeEffect.ExecutionReceipts ??= new List<PolicyEffectExecutionReceipt>();
			activeEffect.ExecutionReceipts.RemoveAll(receipt =>
				string.Equals(receipt?.InstanceId, outcome.Instance?.InstanceId, StringComparison.Ordinal));
			activeEffect.ExecutionReceipts.Add(ClonePolicyEffectExecutionReceipt(outcome.AppliedReceipt));
		}
		if (outcome?.Failed == true)
		{
			PolicySystemLog.Write("Effect", "daily-module-failed", "effectId=" + (activeEffect?.EffectId ?? string.Empty)
				+ " instanceId=" + (entry.Instance.InstanceId ?? string.Empty)
				+ " moduleId=" + (entry.Module?.Id ?? string.Empty)
				+ " targetKind=" + entry.TargetKind
				+ " targetId=" + (entry.TargetId ?? string.Empty)
				+ " day=" + campaignDay.ToString(CultureInfo.InvariantCulture)
				+ " attempts=" + outcome.Attempts.ToString(CultureInfo.InvariantCulture)
				+ " error=" + (outcome.Error ?? string.Empty));
		}
		return outcome;
	}

	private bool TryCompensatePendingDailyPolicyEffects(
		string effectId,
		ActivePolicyEffectSaveData activeEffect,
		out string error)
	{
		error = string.Empty;
		if (!_policyEffectDailyPersistenceTransactions.TryGetValue(
			effectId ?? string.Empty,
			out List<PolicyEffectDailyExecutionOutcome> pending)
			|| pending.Count == 0)
		{
			return true;
		}
		Dictionary<string, PolicyEffectExecutionReceipt> transactionPreviousReceipts
			= new Dictionary<string, PolicyEffectExecutionReceipt>(StringComparer.Ordinal);
		for (int index = 0; index < pending.Count; index++)
		{
			PolicyEffectDailyExecutionOutcome outcome = pending[index];
			string instanceId = (outcome?.Instance?.InstanceId ?? string.Empty).Trim();
			if (instanceId.Length > 0 && !transactionPreviousReceipts.ContainsKey(instanceId))
			{
				// The first outcome for this instance captured the receipt that
				// existed before any mutation in this persistence transaction.
				transactionPreviousReceipts[instanceId] = outcome.PreviousReceipt;
			}
		}
		List<string> failures = new List<string>();
		List<int> failedOrders = new List<int>();
		List<PolicyEffectDailyExecutionOutcome> failedOutcomes
			= new List<PolicyEffectDailyExecutionOutcome>();
		for (int index = pending.Count - 1; index >= 0; index--)
		{
			PolicyEffectDailyExecutionOutcome outcome = pending[index];
			if (!PolicyEffectActivationCoordinator.TryCompensateDailyTargetAfterPersistenceFailure(
				outcome,
				BannerlordPolicyEffectGameBridge.Instance,
				out string compensationError))
			{
				failures.Add((outcome?.Instance?.InstanceId ?? "unknown") + ": " + compensationError);
				failedOrders.Add(index);
				failedOutcomes.Add(outcome);
			}
		}
		for (int index = 0; index < failedOutcomes.Count; index++)
		{
			PolicyEffectDailyExecutionOutcome outcome = failedOutcomes[index];
			string instanceId = (outcome?.Instance?.InstanceId ?? string.Empty).Trim();
			transactionPreviousReceipts.TryGetValue(
				instanceId,
				out PolicyEffectExecutionReceipt transactionPreviousReceipt);
			if (!PolicyEffectActivationCoordinator.TryMarkDailyCompensationPending(
				outcome,
				transactionPreviousReceipt,
				failedOrders[index],
				failures[index],
				out string markerError))
			{
				failures.Add((instanceId.Length == 0 ? "unknown" : instanceId)
					+ ": daily compensation marker failed: " + markerError);
			}
		}
		_policyEffectDailyPersistenceTransactions.Remove(effectId ?? string.Empty);
		if (activeEffect != null)
		{
			HashSet<string> affectedIds = new HashSet<string>(
				pending.Select(outcome => outcome?.Instance?.InstanceId)
					.Where(id => !string.IsNullOrWhiteSpace(id)),
				StringComparer.Ordinal);
			activeEffect.ExecutionReceipts ??= new List<PolicyEffectExecutionReceipt>();
			activeEffect.ExecutionReceipts.RemoveAll(receipt =>
				affectedIds.Contains(receipt?.InstanceId ?? string.Empty));
			foreach (PolicyEffectInstanceSaveData instance in activeEffect.ModuleEffects
				?? new List<PolicyEffectInstanceSaveData>())
			{
				if (affectedIds.Contains(instance?.InstanceId ?? string.Empty)
					&& instance.ExecutionReceipt != null)
				{
					activeEffect.ExecutionReceipts.Add(ClonePolicyEffectExecutionReceipt(instance.ExecutionReceipt));
				}
			}
		}
		error = string.Join("; ", failures);
		PolicySystemLog.Transaction(
			(effectId ?? string.Empty) + ":daily-compensation",
			activeEffect?.RecordId,
			activeEffect?.EffectId ?? effectId,
			string.Empty,
			failures.Count == 0 ? "compensationCompleted" : "compensationPending",
			failures.Count == 0 ? "success" : "failed",
			errorKind: failures.Count == 0 ? string.Empty : "BridgeCompensationFailure",
			executionReceipt: "attempted=" + pending.Count.ToString(CultureInfo.InvariantCulture)
				+ ";failed=" + failures.Count.ToString(CultureInfo.InvariantCulture),
			stateBefore: "effectsAppliedUnpersisted",
			stateAfter: failures.Count == 0 ? "compensated" : "compensationPending");
		return failures.Count == 0;
	}

	private void LogDailyPolicyEffectCompletion(ActivePolicyEffectSaveData activeEffect, int campaignDay)
	{
		if (activeEffect == null
			|| !_policyEffectDailyRuntimePlans.TryGetValue(activeEffect.EffectId ?? string.Empty, out PolicyEffectDailyRuntimePlan plan)
			|| plan.TargetExecutionCount <= 0)
		{
			return;
		}
		int failedInstances = (activeEffect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			.Count(instance => instance?.LifecycleState == PolicyEffectLifecycleState.Failed
				&& PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				&& module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.DailyMutation);
		string targetHash = BuildPolicyEffectTargetFingerprint(
			activeEffect.ModuleEffects,
			out int targetCount);
		PolicySystemLog.Lifecycle("Effect", "daily-complete", failedInstances == 0 ? "success" : "failed", new PolicyLogContext
		{
			TransactionId = (activeEffect.EffectId ?? string.Empty) + ":daily:" + campaignDay.ToString(CultureInfo.InvariantCulture),
			RecordId = activeEffect.RecordId,
			EffectId = activeEffect.EffectId,
			CampaignDay = campaignDay,
			TargetHash = targetHash,
			TargetCount = targetCount,
			StateBefore = "active",
			StateAfter = failedInstances == 0 ? "active" : "failed",
			ErrorKind = failedInstances == 0 ? null : "DailyExecutionFailure",
			Counts = new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["targetExecutions"] = plan.TargetExecutionCount,
				["failedInstances"] = failedInstances
			}
		});
		PolicySystemLog.Transaction(
			(activeEffect.EffectId ?? string.Empty) + ":daily:" + campaignDay.ToString(CultureInfo.InvariantCulture),
			activeEffect.RecordId,
			activeEffect.EffectId,
			string.Empty,
			"effectsCommitted",
			failedInstances == 0 ? "success" : "failed",
			errorKind: failedInstances == 0 ? string.Empty : "DailyExecutionFailure",
			targetHash: targetHash,
			targetCount: targetCount,
			executionReceipt: "executions=" + plan.TargetExecutionCount.ToString(CultureInfo.InvariantCulture),
			stateBefore: "active",
			stateAfter: failedInstances == 0 ? "active" : "failed");
	}

	private static bool IsPolicyEffectRollbackPending(ActivePolicyEffectSaveData activeEffect)
	{
		return activeEffect != null
			&& (activeEffect.EndReason ?? string.Empty).StartsWith(PolicyEffectRollbackPendingPrefix, StringComparison.Ordinal);
	}

	private static bool HasPendingExceptionalPolicyEffectState(ActivePolicyEffectSaveData activeEffect)
	{
		return activeEffect != null
			&& (IsPolicyEffectRollbackPending(activeEffect)
				|| PolicyEffectActivationCoordinator.HasPendingDailyCompensation(activeEffect.ModuleEffects)
				|| PolicyEffectActivationCoordinator.HasPendingScheduledCompensation(activeEffect.ModuleEffects));
	}

	private static bool ShouldRetainActivePolicyEffect(ActivePolicyEffectSaveData activeEffect)
	{
		return activeEffect != null
			&& !string.IsNullOrWhiteSpace(activeEffect.EffectId)
			&& (activeEffect.IsPermanentEffect
				|| (!activeEffect.Ended && activeEffect.RemainingDays > 0)
				|| HasPendingExceptionalPolicyEffectState(activeEffect));
	}

	internal static bool HasPersistedPolicyEffectRollbackForExternal(string effectId)
	{
		string normalizedId = (effectId ?? string.Empty).Trim();
		if (normalizedId.Length == 0)
		{
			return false;
		}
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				return true;
			}
			return behavior._activePolicyEffects.ContainsKey(normalizedId);
		}
		catch
		{
			// Only a confirmed missing effect may move to terminal reconciliation.
			// Unknown/unreadable state keeps the persisted Core rollback owner.
			return true;
		}
	}

	private void ProcessPendingPolicyEffectRollback(
		string effectId,
		ActivePolicyEffectSaveData activeEffect,
		int campaignDay)
	{
		if (activeEffect == null || activeEffect.LastAppliedDay >= campaignDay)
		{
			return;
		}
		PolicySystemLog.Lifecycle("Effect", "rollback-start", "started", new PolicyLogContext
		{
			TransactionId = (effectId ?? string.Empty) + ":rollback",
			RecordId = activeEffect.RecordId,
			EffectId = activeEffect.EffectId ?? effectId,
			CampaignDay = campaignDay,
			Attempt = 1,
			StateBefore = "compensationPending",
			StateAfter = "rollingBack"
		});
		bool rolledBack = PolicyEffectActivationCoordinator.TryRollbackSavedOneShots(
			activeEffect.ModuleEffects,
			activeEffect.ExecutionReceipts,
			BannerlordPolicyEffectGameBridge.Instance,
			campaignDay,
			out string rollbackError);
		if (rolledBack)
		{
			if (!TryFinalizeSuspendedNpcPolicyCommitRollback(activeEffect, out string finalizationError))
			{
				activeEffect.LastAppliedDay = campaignDay;
				PersistActivePolicyEffect(effectId, activeEffect, structureChanged: false);
				PolicySystemLog.Failure("Effect", "rollback-pending-finalization-retry",
					finalizationError,
					"effectId=" + (effectId ?? string.Empty)
					+ " recordId=" + (activeEffect.RecordId ?? string.Empty)
					+ " day=" + campaignDay.ToString(CultureInfo.InvariantCulture));
				return;
			}
			RemoveActivePolicyEffect(effectId);
			PolicySystemLog.Lifecycle("Effect", "rollback-complete", "success", new PolicyLogContext
			{
				TransactionId = (effectId ?? string.Empty) + ":rollback",
				RecordId = activeEffect.RecordId,
				EffectId = activeEffect.EffectId ?? effectId,
				CampaignDay = campaignDay,
				Attempt = 1,
				StateBefore = "rollingBack",
				StateAfter = "ended"
			});
			PolicySystemLog.Transaction(
				(effectId ?? string.Empty) + ":rollback",
				activeEffect.RecordId,
				activeEffect.EffectId ?? effectId,
				string.Empty,
				"compensationCompleted",
				"success",
				stateBefore: "compensationPending",
				stateAfter: "ended");
			return;
		}
		activeEffect.LastAppliedDay = campaignDay;
		PersistActivePolicyEffect(effectId, activeEffect, structureChanged: false);
		PolicySystemLog.Lifecycle("Effect", "rollback-failed", "failed", new PolicyLogContext
		{
			TransactionId = (effectId ?? string.Empty) + ":rollback",
			RecordId = activeEffect.RecordId,
			EffectId = activeEffect.EffectId ?? effectId,
			CampaignDay = campaignDay,
			Attempt = 1,
			ErrorKind = "OneShotRollbackFailure",
			MessageChars = rollbackError?.Length ?? 0,
			MessageHash = PolicySystemLog.HashSensitive(rollbackError),
			StateBefore = "rollingBack",
			StateAfter = "compensationPending"
		});
		PolicySystemLog.Transaction(
			(effectId ?? string.Empty) + ":rollback",
			activeEffect.RecordId,
			activeEffect.EffectId ?? effectId,
			string.Empty,
			"compensationPending",
			"failed",
			errorKind: "OneShotRollbackFailure",
			stateBefore: "compensationPending",
			stateAfter: "compensationPending");
	}

	private bool TryFinalizeSuspendedNpcPolicyCommitRollback(
		ActivePolicyEffectSaveData activeEffect,
		out string failureReason)
	{
		failureReason = string.Empty;
		string recordId = (activeEffect?.RecordId ?? string.Empty).Trim();
		bool persistedRenewalKind = false;
		if (recordId.Length == 0
			|| !NpcRulerPolicyBehavior.TryGetSuspendedPolicyAgendaCommitForExternal(recordId, out persistedRenewalKind))
		{
			return true;
		}
		DynamicPolicySaveData data = LoadDynamicPolicies().FirstOrDefault(item => item != null
			&& string.Equals((item.RecordId ?? string.Empty).Trim(), recordId, StringComparison.OrdinalIgnoreCase));
		if (data == null)
		{
			if (!NpcRulerPolicyBehavior.CompleteSuspendedPolicyAgendaCommitRollbackForExternal(
				recordId,
				persistedRenewalKind,
				out string missingCoreFailure))
			{
				failureReason = "suspended NPC rollback cannot resolve its dynamic policy record: " + missingCoreFailure;
				return false;
			}
			return true;
		}
		if (!string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "suspended NPC rollback resolved a non-NPC dynamic policy record";
			return false;
		}
		bool isRenewal = string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(data.Status, DynamicPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
			|| (!string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(data.Status, DynamicPolicyStatusRejected, StringComparison.OrdinalIgnoreCase)
				&& persistedRenewalKind);
		if (!TryFailNpcPolicyEffectBundleCommit(
			recordId,
			isRenewal,
			"deferred NPC policy effect rollback completed",
			out string callbackFailure))
		{
			failureReason = "Core suspended-policy failure callback was not confirmed: " + callbackFailure;
			return false;
		}
		if (!NpcRulerPolicyBehavior.CompleteSuspendedPolicyAgendaCommitRollbackForExternal(
			recordId,
			isRenewal,
			out string npcFailure))
		{
			failureReason = "NPC suspended-policy rollback finalization failed: " + npcFailure;
			return false;
		}
		return true;
	}

	private bool TryDispatchPolicyEffectLifecycleForActiveEffect(
		ActivePolicyEffectSaveData activeEffect,
		PolicyEffectLifecycleEventKind eventKind,
		string eventKey,
		out bool stateChanged,
		out string failureReason)
	{
		stateChanged = false;
		failureReason = string.Empty;
		if (activeEffect == null)
		{
			return true;
		}
		bool success = PolicyEffectActivationCoordinator.TryDispatchLifecycle(
			activeEffect.ModuleEffects,
			eventKind,
			eventKey,
			BannerlordPolicyEffectGameBridge.Instance,
			GetCurrentCampaignDay(),
			out stateChanged,
			out failureReason);
		if (stateChanged || !success)
		{
			PolicySystemLog.Lifecycle("Effect", "lifecycle-dispatched", success ? "success" : "failed", new PolicyLogContext
			{
				TransactionId = eventKey,
				RecordId = activeEffect.RecordId,
				EffectId = activeEffect.EffectId,
				CampaignDay = GetCurrentCampaignDay(),
				StateAfter = eventKind.ToString(),
				ErrorKind = success ? null : "LifecycleDispatch",
				MessageChars = failureReason?.Length ?? 0,
				MessageHash = PolicySystemLog.HashSensitive(failureReason)
			});
		}
		return success;
	}

	// Low-frequency entry point for renewal/abolition paths implemented by other partial
	// files. It persists only module runtime progress and never rebuilds the model index.
	private bool TryDispatchPolicyEffectLifecycleForRecord(
		string recordId,
		PolicyEffectLifecycleEventKind eventKind,
		string eventKey,
		out string failureReason)
	{
		failureReason = string.Empty;
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		if (normalizedRecordId.Length == 0)
		{
			return true;
		}
		List<string> failures = new List<string>();
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToArray())
		{
			ActivePolicyEffectSaveData activeEffect;
			try
			{
				activeEffect = GetActivePolicyEffectForWork(item.Key, item.Value ?? string.Empty);
			}
			catch (Exception ex)
			{
				failures.Add(item.Key + ": " + ex.Message);
				continue;
			}
			if (activeEffect == null
				|| !string.Equals(activeEffect.RecordId ?? string.Empty, normalizedRecordId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			try
			{
				bool success = TryDispatchPolicyEffectLifecycleForActiveEffect(
					activeEffect,
					eventKind,
					eventKey,
					out bool stateChanged,
					out string callbackError);
				if (stateChanged)
				{
					PersistActivePolicyEffect(item.Key, activeEffect, structureChanged: false);
				}
				if (!success)
				{
					failures.Add(activeEffect.EffectId + ": " + callbackError);
				}
			}
			catch (Exception ex)
			{
				failures.Add(activeEffect.EffectId + ": " + ex.Message);
			}
		}
		failureReason = string.Join("; ", failures);
		return failures.Count == 0;
	}

	private void DispatchPolicyEffectAbolishedBeforeRemoval(
		ActivePolicyEffectSaveData activeEffect,
		string eventKey,
		string terminationSource)
	{
		string failureReason;
		try
		{
			bool success = TryDispatchPolicyEffectLifecycleForActiveEffect(
				activeEffect,
				PolicyEffectLifecycleEventKind.Abolished,
				eventKey,
				out bool stateChanged,
				out failureReason);
			string effectId = (activeEffect?.EffectId ?? string.Empty).Trim();
			if (stateChanged && effectId.Length > 0 && _activePolicyEffects.ContainsKey(effectId))
			{
				PersistActivePolicyEffect(effectId, activeEffect, structureChanged: false);
			}
			if (success)
			{
				return;
			}
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
		}
		PolicySystemLog.Write("Effect", "lifecycle-termination-failed",
			"source=" + (terminationSource ?? string.Empty)
			+ " effectId=" + (activeEffect?.EffectId ?? string.Empty)
			+ " recordId=" + (activeEffect?.RecordId ?? string.Empty)
			+ " eventKey=" + (eventKey ?? string.Empty)
			+ " error=" + (failureReason ?? string.Empty));
	}

	private void DispatchPolicyEffectRecordAbolishedBeforeRemoval(
		string recordId,
		string eventKey,
		string terminationSource)
	{
		string failureReason;
		try
		{
			if (TryDispatchPolicyEffectLifecycleForRecord(
				recordId,
				PolicyEffectLifecycleEventKind.Abolished,
				eventKey,
				out failureReason))
			{
				return;
			}
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
		}
		PolicySystemLog.Write("Effect", "lifecycle-termination-failed",
			"source=" + (terminationSource ?? string.Empty)
			+ " recordId=" + (recordId ?? string.Empty)
			+ " eventKey=" + (eventKey ?? string.Empty)
			+ " error=" + (failureReason ?? string.Empty));
	}

	private bool TryActivatePolicyEffectApplication(
		PolicyDraftRequest request,
		PolicyApplicationResult application,
		string recordId,
		bool isRenewal,
		out string activeEffectId,
		out string failureReason)
	{
		activeEffectId = string.Empty;
		failureReason = string.Empty;
		List<AppliedKingdomEffect> appliedEffects = (application?.KingdomEffects ?? new List<AppliedKingdomEffect>())
			.Where(effect => effect != null && (effect.IsPermanentEffect || effect.DurationDays > 0))
			.ToList();
		if (!TryCoalescePolicyEffectShellInstances(
			appliedEffects.SelectMany(effect => effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>()),
			out List<PolicyEffectInstanceSaveData> instances,
			out failureReason))
		{
			return false;
		}
		if (isRenewal)
		{
			instances = instances.Where(instance =>
				!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| !IsNonRenewableOneTimePolicyEffect(module)).ToList();
		}
		if (instances.Count == 0)
		{
			if (isRenewal)
			{
				return true;
			}
			failureReason = "政策效果应用不包含可执行模块实例";
			return false;
		}
		AppliedKingdomEffect source = appliedEffects.FirstOrDefault(effect =>
			string.Equals(effect.LocalTargetScope, LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase))
			?? appliedEffects.FirstOrDefault();
		List<PolicyEffectExecutionReceipt> receipts = appliedEffects
			.SelectMany(effect => effect.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
			.Where(receipt => receipt != null && !string.IsNullOrWhiteSpace(receipt.ReceiptId))
			.GroupBy(receipt => receipt.ReceiptId, StringComparer.Ordinal)
			.Select(group => ClonePolicyEffectExecutionReceipt(group.Last()))
			.ToList();
		PolicyEffectBundleRegistration bundle = new PolicyEffectBundleRegistration
		{
			ScopeKind = request?.ScopeKind ?? PolicyScopeKingdom,
			EffectId = Guid.NewGuid().ToString("N"),
			RecordId = recordId ?? string.Empty,
			ProposerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
			ActorHeroId = Hero.MainHero?.StringId ?? string.Empty,
			IssuerKingdomId = request?.IssuerKingdomId ?? string.Empty,
			PolicyName = request?.PolicyName ?? string.Empty,
			DateText = request?.DateText ?? string.Empty,
			SubmittedDay = Math.Max(0, request?.SubmittedDay ?? GetCurrentCampaignDay()),
			TargetKingdomId = FirstNonEmpty(request?.PlayerKingdomId, source?.KingdomId),
			TargetKingdomName = IsLocalPolicyRequest(request)
				? source?.KingdomName ?? string.Empty
				: FirstNonEmpty(request?.PlayerKingdomName, source?.KingdomName),
			TargetHandle = source?.TargetHandle ?? string.Empty,
			TargetLabel = FirstNonEmpty(source?.TargetLabel, source?.KingdomName),
			LocalTargetScope = NormalizeLocalPolicyTargetScope(source?.LocalTargetScope),
			TargetFiefIds = IsLocalPolicyRequest(request)
				? NormalizeIdList(request?.SelectedFiefIds)
				: NormalizeIdList(appliedEffects.SelectMany(effect => effect.TargetFiefIds ?? new List<string>())),
			TargetSettlementIds = NormalizeIdList(appliedEffects.SelectMany(effect => effect.TargetSettlementIds ?? new List<string>())),
			TargetClanIds = NormalizeIdList(appliedEffects.SelectMany(effect => effect.TargetClanIds ?? new List<string>())),
			DirectTargetSettlementIds = NormalizeIdList(appliedEffects.SelectMany(effect => effect.DirectTargetSettlementIds ?? new List<string>())),
			FollowCurrentRulingClan = appliedEffects.Any(effect => effect.FollowCurrentRulingClan),
			DurationDays = appliedEffects.Max(effect => effect.DurationDays),
			IsPermanentEffect = appliedEffects.Any(effect => effect.IsPermanentEffect),
			DailyMaintenanceGoldCost = Math.Max(0, request?.DailyMaintenanceGoldCost ?? 0),
			TotalMaintenancePaidGold = Math.Max(0, request?.TotalMaintenancePaidGold ?? 0),
			MaintenanceChargeEnabled = !IsVassalPolicyRequest(request),
			MaintenanceFunded = request?.MaintenanceFunded ?? true,
			LastMaintenanceSettlementDay = (request?.LastMaintenanceSettlementDay ?? -1) >= 0
				? request.LastMaintenanceSettlementDay
				: GetCurrentCampaignDay(),
			LastEffectProcessedDay = (request?.LastEffectProcessedDay ?? -1) >= 0
				? request.LastEffectProcessedDay
				: GetCurrentCampaignDay(),
			Reason = FirstNonEmpty(appliedEffects.Select(effect => effect.Reason).ToArray()),
			ModuleEffects = instances,
			ExecutionReceipts = receipts
		};
		if (!TryRegisterPolicyEffectBundleInternal(bundle, out activeEffectId, out failureReason))
		{
			return false;
		}
		foreach (AppliedKingdomEffect effect in appliedEffects)
		{
			effect.EffectId = activeEffectId;
			SynchronizeOwnedPolicyEffectProgress(
				effect.ModuleEffects,
				bundle.ModuleEffects,
				bundle.ExecutionReceipts,
				out List<PolicyEffectInstanceSaveData> synchronizedInstances,
				out List<PolicyEffectExecutionReceipt> synchronizedReceipts);
			effect.ModuleEffects = synchronizedInstances;
			effect.ExecutionReceipts = synchronizedReceipts;
		}
		NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(
			recordId,
			activeEffectId,
			bundle.TargetKingdomId,
			bundle.DurationDays,
			isEnded: false);
		PolicySystemLog.Write("Effect", "application-activated", "recordId=" + (recordId ?? string.Empty)
			+ " effectId=" + activeEffectId
			+ " modules=" + bundle.ModuleEffects.Count.ToString(CultureInfo.InvariantCulture)
			+ " receipts=" + bundle.ExecutionReceipts.Count.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private static bool HasExecutablePolicyModuleInstances(PolicyApplicationResult application)
	{
		return (application?.KingdomEffects ?? new List<AppliedKingdomEffect>())
			.Any(effect => effect?.ModuleEffects?.Any(instance => instance != null) == true);
	}

	private bool RollbackAndRemovePolicyEffectBundle(string effectId, string reason, out string failureReason)
	{
		failureReason = string.Empty;
		string normalizedId = (effectId ?? string.Empty).Trim();
		if (normalizedId.Length == 0)
		{
			return true;
		}
		ActivePolicyEffectSaveData active = null;
		bool activeEntryExists = _activePolicyEffects.TryGetValue(normalizedId, out string raw);
		bool activeReadFailed = false;
		if (activeEntryExists)
		{
			try
			{
				active = GetActivePolicyEffectForWork(normalizedId, raw);
				if (active == null)
				{
					activeReadFailed = true;
					failureReason = "active policy effect data is empty";
				}
			}
			catch (Exception ex)
			{
				activeReadFailed = true;
				failureReason = "读取活动政策效果失败：" + ex.Message;
			}
		}
		string rollbackError = string.Empty;
		bool rolledBack = !activeEntryExists
			|| (!activeReadFailed
				&& PolicyEffectActivationCoordinator.TryRollbackSavedOneShots(
					active.ModuleEffects,
					active.ExecutionReceipts,
					BannerlordPolicyEffectGameBridge.Instance,
					GetCurrentCampaignDay(),
					out rollbackError));
		if (rolledBack)
		{
			RemoveActivePolicyEffect(normalizedId);
		}
		else if (active != null)
		{
			foreach (PolicyEffectInstanceSaveData instance in active.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				if (instance == null
					|| instance.LifecycleState == PolicyEffectLifecycleState.Failed
					|| instance.LifecycleState == PolicyEffectLifecycleState.RolledBack
					|| (PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
						&& module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot))
				{
					continue;
				}
				instance.LifecycleState = PolicyEffectLifecycleState.Suspended;
			}
			active.Ended = true;
			active.EndReason = PolicyEffectRollbackPendingPrefix + (reason ?? string.Empty).Trim();
			active.PendingApplication = null;
			active.LastAppliedDay = GetCurrentCampaignDay();
			PersistActivePolicyEffect(normalizedId, active, structureChanged: true);
		}
		if (!rolledBack)
		{
			failureReason = FirstNonEmpty(failureReason, rollbackError);
		}
		PolicySystemLog.Write("Effect", "application-rolled-back", "effectId=" + normalizedId
			+ " reason=" + (reason ?? string.Empty)
			+ " success=" + rolledBack.ToString(CultureInfo.InvariantCulture)
			+ (string.IsNullOrWhiteSpace(failureReason) ? string.Empty : " error=" + failureReason));
		PolicySystemLog.Transaction(
			normalizedId + ":rollback",
			active?.RecordId,
			active?.EffectId ?? normalizedId,
			string.Empty,
			rolledBack ? "compensationCompleted" : "compensationPending",
			rolledBack ? "success" : "failed",
			errorKind: rolledBack ? string.Empty : "OneShotRollbackFailure",
			stateBefore: "active",
			stateAfter: rolledBack ? "compensated" : "compensationPending");
		return rolledBack;
	}

	private void RecordSuccessfulLocalPolicy(PolicyDraftRequest request, PolicyGenerationResult result, string feedback, List<AppliedKingdomEffect> effects, string recordId, List<Settlement> fiefs)
	{
		List<AppliedKingdomEffect> validEffects = (effects ?? new List<AppliedKingdomEffect>()).Where(x => x != null).ToList();
		AppliedKingdomEffect sourceEffect = validEffects.FirstOrDefault(x => string.Equals(NormalizeLocalPolicyTargetScope(x.LocalTargetScope), LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase))
			?? validEffects.FirstOrDefault();
		bool hasExecutableBundle = string.Equals(
			result?.Postprocess?.Disposition,
			"executable",
			StringComparison.Ordinal);
		LocalPolicyRecordSaveData record = new LocalPolicyRecordSaveData
		{
			Version = 6,
			ScopeKind = PolicyScopeLocal,
			RecordId = recordId,
			ReReviewRootRecordId = request?.ReReviewRootRecordId ?? string.Empty,
			ReReviewSourceRecordId = request?.ReReviewSourceRecordId ?? string.Empty,
			SupersedesRecordId = request?.SupersedesRecordId ?? string.Empty,
			ReReviewReplacementCommitted = false,
			ActiveEffectId = sourceEffect?.EffectId ?? "",
			SubmittedDay = Math.Max(0, request?.SubmittedDay ?? GetCurrentCampaignDay()),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			DateText = request?.DateText ?? "",
			PolicyName = request?.PolicyName ?? "",
			PolicyContent = request?.PolicyContent ?? "",
			PublicFeedback = CleanPolicyDisplayText(feedback ?? ""),
			ImpactSummary = CleanPolicyDisplayText(result?.MainAssessment?.ImpactSummary ?? ""),
			Status = LocalPolicyStatusActive,
			EffectStatus = hasExecutableBundle ? LocalPolicyStatusActive : LocalPolicyStatusExpired,
			UseAiEvaluatedCost = request?.UseAiEvaluatedCost == true,
			RequiredGoldCost = Math.Max(0, request?.RequiredGoldCost ?? 0),
			InitialActualGoldCost = Math.Max(0, request?.GoldCost ?? 0),
			TotalPaidGold = Math.Max(0, request?.GoldCost ?? 0),
			IsPermanentEffect = request?.IsPermanentEffect == true,
			DailyMaintenanceGoldCost = hasExecutableBundle
				? Math.Max(0, request?.DailyMaintenanceGoldCost ?? 0)
				: 0,
			TotalMaintenancePaidGold = Math.Max(0, request?.TotalMaintenancePaidGold ?? 0),
			MaintenanceFunded = request?.MaintenanceFunded ?? true,
			LastMaintenanceSettlementDay = (request?.LastMaintenanceSettlementDay ?? -1) >= 0
				? request.LastMaintenanceSettlementDay
				: GetCurrentCampaignDay(),
			LastEffectProcessedDay = (request?.LastEffectProcessedDay ?? -1) >= 0
				? request.LastEffectProcessedDay
				: GetCurrentCampaignDay(),
			GoldEffectScale = request?.GoldEffectScale ?? 1f,
			OriginalDurationDays = hasExecutableBundle
				? request?.IsPermanentEffect == true ? 0 : Math.Max(1, sourceEffect?.DurationDays ?? 1)
				: 0,
			RemainingDays = hasExecutableBundle
				? Math.Max(0, sourceEffect?.RemainingDays ?? sourceEffect?.DurationDays ?? 0)
				: 0,
			OriginalTargetFiefIds = NormalizeIdList((fiefs ?? new List<Settlement>()).Select(fief => fief?.StringId)),
			TargetFiefIds = NormalizeIdList((fiefs ?? new List<Settlement>()).Select(fief => fief?.StringId)),
			OriginalTargets = (fiefs ?? new List<Settlement>()).Where(x => x != null).Select(BuildLocalPolicyTargetSnapshot).ToList(),
			Effects = validEffects.Select(BuildLocalPolicyEffectRecord).ToList(),
			EffectReason = hasExecutableBundle
				? sourceEffect?.Reason ?? ""
				: result?.Postprocess?.Reason ?? ""
		};
		_localPolicyRecords[recordId] = JsonConvert.SerializeObject(record);
	}

	private void RecordSuccessfulVassalPolicy(PolicyDraftRequest request, PolicyGenerationResult result, string feedback, List<AppliedKingdomEffect> effects, string recordId)
	{
		List<AppliedKingdomEffect> validEffects = (effects ?? new List<AppliedKingdomEffect>()).Where(x => x != null).ToList();
		AppliedKingdomEffect sourceEffect = validEffects.FirstOrDefault(x => string.Equals(x.KingdomId ?? "", request?.PlayerKingdomId ?? "", StringComparison.OrdinalIgnoreCase))
			?? validEffects.FirstOrDefault();
		bool hasExecutableBundle = string.Equals(
			result?.Postprocess?.Disposition,
			"executable",
			StringComparison.Ordinal);
		LocalPolicyRecordSaveData record = new LocalPolicyRecordSaveData
		{
			Version = 6,
			ScopeKind = PolicyScopeVassal,
			RecordId = recordId,
			ReReviewRootRecordId = request?.ReReviewRootRecordId ?? string.Empty,
			ReReviewSourceRecordId = request?.ReReviewSourceRecordId ?? string.Empty,
			SupersedesRecordId = request?.SupersedesRecordId ?? string.Empty,
			ReReviewReplacementCommitted = false,
			ActiveEffectId = sourceEffect?.EffectId ?? "",
			SubmittedDay = Math.Max(0, request?.SubmittedDay ?? GetCurrentCampaignDay()),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			DateText = request?.DateText ?? "",
			PolicyName = request?.PolicyName ?? "",
			PolicyContent = request?.PolicyContent ?? "",
			PublicFeedback = CleanPolicyDisplayText(feedback ?? ""),
			ImpactSummary = CleanPolicyDisplayText(result?.MainAssessment?.ImpactSummary ?? ""),
			Status = LocalPolicyStatusActive,
			EffectStatus = hasExecutableBundle ? LocalPolicyStatusActive : LocalPolicyStatusExpired,
			TargetKingdomId = request?.PlayerKingdomId ?? "",
			TargetKingdomName = request?.PlayerKingdomName ?? "",
			IssuerKingdomId = request?.IssuerKingdomId ?? "",
			IssuerKingdomName = request?.IssuerKingdomName ?? "",
			InitialIndependenceCost = Math.Max(0, request?.VassalPublicationIndependenceCost ?? 0),
			TotalIndependenceCost = Math.Max(0, request?.VassalPublicationIndependenceCost ?? 0),
			VassalQualityIndependenceDelta = request?.VassalQualityIndependenceDelta ?? 0,
			IndependenceBefore = request?.VassalIndependenceBefore ?? 0,
			IndependenceAfter = request?.VassalIndependenceAfter ?? request?.VassalIndependenceBefore ?? 0,
			IndependenceReason = request?.VassalIndependenceReason ?? "",
			UseAiEvaluatedCost = false,
			GoldEffectScale = 1f,
			IsPermanentEffect = request?.IsPermanentEffect == true,
			OriginalDurationDays = hasExecutableBundle
				? request?.IsPermanentEffect == true ? 0 : Math.Max(1, sourceEffect?.DurationDays ?? 1)
				: 0,
			RemainingDays = hasExecutableBundle
				? Math.Max(0, sourceEffect?.RemainingDays ?? sourceEffect?.DurationDays ?? 0)
				: 0,
			Effects = validEffects.Select(BuildLocalPolicyEffectRecord).ToList(),
			EffectReason = hasExecutableBundle
				? sourceEffect?.Reason ?? ""
				: result?.Postprocess?.Reason ?? ""
		};
		_localPolicyRecords[recordId] = JsonConvert.SerializeObject(record);
	}

	private void UpdateVassalPolicyIndependenceRecord(string recordId, PolicyDraftRequest request)
	{
		if (!_localPolicyRecords.TryGetValue(recordId ?? "", out string raw))
		{
			return;
		}
		LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
		if (record == null)
		{
			return;
		}
		record.IndependenceBefore = request?.VassalIndependenceBefore ?? record.IndependenceBefore;
		record.IndependenceAfter = request?.VassalIndependenceAfter ?? record.IndependenceAfter;
		record.InitialIndependenceCost = Math.Max(0, request?.VassalPublicationIndependenceCost ?? record.InitialIndependenceCost);
		record.TotalIndependenceCost = Math.Max(record.TotalIndependenceCost, record.InitialIndependenceCost);
		record.VassalQualityIndependenceDelta = request?.VassalQualityIndependenceDelta ?? record.VassalQualityIndependenceDelta;
		record.IndependenceReason = request?.VassalIndependenceReason ?? record.IndependenceReason;
		_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
	}

	private void OnVassalRelationshipEndedInternal(string vassalKingdomId, string reason)
	{
		string targetId = (vassalKingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(targetId))
		{
			return;
		}
		string endReason = string.IsNullOrWhiteSpace(reason) ? "臣属关系终止" : reason.Trim();
		HashSet<string> affectedRecordIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> item in _localPolicyRecords.ToList())
		{
			LocalPolicyRecordSaveData record;
			try
			{
				record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(item.Value ?? ""));
			}
			catch
			{
				continue;
			}
			if (record == null
				|| !string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(record.TargetKingdomId ?? "", targetId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.Status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.Status, LocalPolicyStatusRelationshipEnded, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			record.Status = LocalPolicyStatusRelationshipEnded;
			record.EndReason = endReason;
			record.RemainingDays = 0;
			record.ActiveEffectId = "";
			foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
			{
				effect.ActiveEffectId = "";
				effect.RemainingDays = 0;
			}
			_localPolicyRecords[item.Key] = JsonConvert.SerializeObject(record);
			affectedRecordIds.Add(record.RecordId);
		}
		if (affectedRecordIds.Count == 0)
		{
			return;
		}
		foreach (string affectedRecordId in affectedRecordIds)
		{
			DispatchPolicyEffectRecordAbolishedBeforeRemoval(
				affectedRecordId,
				"record:" + affectedRecordId + ":relationship_ended",
				"relationship_ended");
		}
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			ActivePolicyEffectSaveData active;
			try
			{
				active = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(item.Value ?? "");
			}
			catch
			{
				continue;
			}
			if (active == null || !IsVassalActivePolicyEffect(active) || !affectedRecordIds.Contains(active.RecordId ?? ""))
			{
				continue;
			}
			active.RemainingDays = 0;
			active.Ended = true;
			active.EndReason = endReason;
			NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(active.RecordId, active.EffectId, active.TargetKingdomId, 0, isEnded: true);
			RemoveActivePolicyEffect(item.Key);
		}
		_activePolicyEffectModelCache.Clear();
		PolicySystemLog.Write("VassalPolicy", "relationship-ended", "target=" + targetId + " records=" + affectedRecordIds.Count.ToString(CultureInfo.InvariantCulture) + " reason=" + endReason);
	}

	private static LocalPolicyEffectRecordSaveData BuildLocalPolicyEffectRecord(AppliedKingdomEffect effect)
	{
		return new LocalPolicyEffectRecordSaveData
		{
			ModuleEffects = ClonePolicyEffectSaveDataList(effect?.ModuleEffects),
			ExecutionReceipts = ClonePolicyEffectExecutionReceipts(effect?.ExecutionReceipts),
			TargetScope = NormalizeLocalPolicyTargetScope(effect?.LocalTargetScope),
			TargetHandle = effect?.TargetHandle ?? "",
			TargetLabel = effect?.TargetLabel ?? effect?.KingdomName ?? "",
			ActiveEffectId = effect?.EffectId ?? "",
			TargetKingdomId = effect?.KingdomId ?? "",
			TargetKingdomName = effect?.KingdomName ?? "",
			TargetClanIds = NormalizeIdList(effect?.TargetClanIds),
			DirectTargetSettlementIds = NormalizeIdList(effect?.DirectTargetSettlementIds),
			FollowCurrentRulingClan = effect?.FollowCurrentRulingClan == true,
			RemainingDays = Math.Max(0, effect?.RemainingDays ?? effect?.DurationDays ?? 0),
			IsPermanentEffect = effect?.IsPermanentEffect == true,
			IsEnded = false,
			EndReason = string.Empty,
			Reason = effect?.Reason ?? ""
		};
	}

	private void TrimActivePolicyEffects()
	{
		foreach (string key in _activePolicyEffects.Keys.ToList())
		{
			if (_quarantinedActivePolicyEffectIds.Contains(key))
			{
				continue;
			}
			try
			{
				ActivePolicyEffectSaveData activeEffect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(_activePolicyEffects[key] ?? "");
				if (!ShouldRetainActivePolicyEffect(activeEffect))
				{
					RemoveActivePolicyEffect(key);
				}
			}
			catch (Exception ex)
			{
				QuarantineActivePolicyEffect(key, _activePolicyEffects[key], "trim: " + ex.Message);
			}
		}
	}

	private void QuarantineActivePolicyEffect(string effectId, string raw, string reason)
	{
		string id = (effectId ?? string.Empty).Trim();
		if (id.Length == 0)
		{
			return;
		}
		if (raw != null)
		{
			_activePolicyEffects[id] = raw;
		}
		RemovePolicyEffectDerivedIndexesAfterPersistFailure(id);
		_policyEffectDailyPersistenceTransactions.Remove(id);
		_policyEffectPendingDailyCompensationEffectIds.Remove(id);
		_queuedActivePolicyEffectIds.Remove(id);
		if (_quarantinedActivePolicyEffectIds.Add(id))
		{
			PolicySystemLog.Failure("Effect", "active-effect-quarantined",
				FirstNonEmpty(reason, "persisted active effect is unreadable"),
				"effectId=" + id + " rawChars=" + (raw?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
		}
	}

	private void UpdatePolicyRecordEffectProgress(ActivePolicyEffectSaveData activeEffect)
	{
		if (activeEffect == null || string.IsNullOrWhiteSpace(activeEffect.EffectId))
		{
			return;
		}
		bool ended = activeEffect.Ended || (!activeEffect.IsPermanentEffect && activeEffect.RemainingDays <= 0);
		if (ended)
		{
			foreach (PolicyEffectInstanceSaveData instance in activeEffect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				if (instance?.LifecycleState == PolicyEffectLifecycleState.Active
					&& PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
					&& module.Descriptor.ExecutionKind != PolicyEffectExecutionKind.OneShot)
				{
					instance.LifecycleState = PolicyEffectLifecycleState.Completed;
				}
			}
		}
		// Persist progress without touching the compiled runtime index. Every terminal caller
		// removes the active effect after record synchronization, so its buckets are removed once.
		PersistActivePolicyEffect(activeEffect.EffectId, activeEffect, structureChanged: false);
		if (string.IsNullOrWhiteSpace(activeEffect.RecordId))
		{
			return;
		}
		if (IsLocalActivePolicyEffect(activeEffect) || IsVassalActivePolicyEffect(activeEffect))
		{
			UpdateLocalPolicyProgress(activeEffect);
			if (IsVassalActivePolicyEffect(activeEffect))
			{
				NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(activeEffect.RecordId, activeEffect.EffectId, activeEffect.TargetKingdomId, activeEffect.RemainingDays, activeEffect.Ended);
			}
			return;
		}
		NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(activeEffect.RecordId, activeEffect.EffectId, activeEffect.TargetKingdomId, activeEffect.RemainingDays, activeEffect.Ended);
		try
		{
			if (!_policyRecordHistory.TryGetValue(activeEffect.RecordId, out string raw) || string.IsNullOrWhiteSpace(raw))
			{
				return;
			}
			PolicyRecordSaveData record = JsonConvert.DeserializeObject<PolicyRecordSaveData>(raw);
			if (record?.Effects == null)
			{
				return;
			}
			List<PolicyRecordEffectSaveData> matchingEffects = record.Effects
				.Where(effect => effect != null
					&& string.Equals(effect.EffectId, activeEffect.EffectId, StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (matchingEffects.Count == 0)
			{
				return;
			}
			foreach (PolicyRecordEffectSaveData effect in matchingEffects)
			{
				effect.RemainingDays = Math.Max(0, activeEffect.RemainingDays);
				effect.IsPermanentEffect = activeEffect.IsPermanentEffect;
				effect.LastAppliedDay = activeEffect.LastAppliedDay;
				effect.IsEnded = ended;
				effect.EndReason = activeEffect.EndReason ?? "";
				SynchronizeOwnedPolicyEffectProgress(
					effect.ModuleEffects,
					activeEffect.ModuleEffects,
					activeEffect.ExecutionReceipts,
					out List<PolicyEffectInstanceSaveData> synchronizedInstances,
					out List<PolicyEffectExecutionReceipt> synchronizedReceipts);
				effect.ModuleEffects = synchronizedInstances;
				effect.ExecutionReceipts = synchronizedReceipts;
			}
			record.ImpactEffectsSummary = LimitDisplayChars(BuildPolicyRecordEffectSummary(record), MaxPolicyRecordImpactChars);
			_policyRecordHistory[activeEffect.RecordId] = JsonConvert.SerializeObject(record);
		}
		catch (Exception ex)
		{
			PolicyDebugLog("history-progress-update-failed", "effectId=" + (activeEffect.EffectId ?? "") + " error=" + ex.Message);
		}
	}

	private void MarkPolicyRecordEffectEnded(ActivePolicyEffectSaveData activeEffect, string reason, bool queueNaturalExpiry = true)
	{
		if (activeEffect == null)
		{
			return;
		}
		activeEffect.RemainingDays = 0;
		activeEffect.Ended = true;
        activeEffect.EndReason = string.IsNullOrWhiteSpace(reason) ? "已结束" : reason.Trim();
		UpdatePolicyRecordEffectProgress(activeEffect);
		PolicyEffectLedgerLog("effect-ended", "recordId=" + (activeEffect.RecordId ?? "")
			+ " effectId=" + (activeEffect.EffectId ?? "")
			+ " reason=" + activeEffect.EndReason);
		if (queueNaturalExpiry)
		{
			TryQueueNaturalExpiryAbolition(activeEffect.RecordId, activeEffect.EffectId);
		}
	}

	private void UpdateLocalPolicyProgress(ActivePolicyEffectSaveData activeEffect)
	{
		try
		{
			if (activeEffect == null || !_localPolicyRecords.TryGetValue(activeEffect.RecordId ?? "", out string raw)) return;
			LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
			if (record == null) return;
			string targetScope = GetLocalPolicyTargetScope(activeEffect);
			List<LocalPolicyEffectRecordSaveData> effectRecords = record.Effects
				.Where(effect => effect != null
					&& string.Equals(effect.ActiveEffectId ?? "", activeEffect.EffectId ?? "", StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (effectRecords.Count == 0)
			{
				LocalPolicyEffectRecordSaveData fallback = record.Effects.FirstOrDefault(effect => effect != null
					&& string.Equals(effect.TargetScope ?? "", targetScope, StringComparison.OrdinalIgnoreCase));
				if (fallback != null)
				{
					effectRecords.Add(fallback);
				}
			}
			foreach (LocalPolicyEffectRecordSaveData effectRecord in effectRecords)
			{
				effectRecord.ActiveEffectId = activeEffect.EffectId ?? effectRecord.ActiveEffectId;
				effectRecord.TargetHandle = activeEffect.TargetHandle ?? effectRecord.TargetHandle;
				effectRecord.TargetLabel = activeEffect.TargetLabel ?? effectRecord.TargetLabel;
				effectRecord.RemainingDays = Math.Max(0, activeEffect.RemainingDays);
				effectRecord.IsPermanentEffect = activeEffect.IsPermanentEffect;
				effectRecord.IsEnded = activeEffect.Ended;
				effectRecord.EndReason = activeEffect.EndReason ?? string.Empty;
				SynchronizeOwnedPolicyEffectProgress(
					effectRecord.ModuleEffects,
					activeEffect.ModuleEffects,
					activeEffect.ExecutionReceipts,
					out List<PolicyEffectInstanceSaveData> synchronizedInstances,
					out List<PolicyEffectExecutionReceipt> synchronizedReceipts);
				effectRecord.ModuleEffects = synchronizedInstances;
				effectRecord.ExecutionReceipts = synchronizedReceipts;
			}
			bool isSourceEffect = IsLocalActivePolicyEffect(activeEffect)
				? !IsMentionedLocalPolicyEffect(activeEffect)
				: IsSourceVassalPolicyEffect(activeEffect);
			if (isSourceEffect)
			{
				record.ActiveEffectId = activeEffect.EffectId ?? record.ActiveEffectId;
				record.RemainingDays = Math.Max(0, activeEffect.RemainingDays);
				record.IsPermanentEffect = activeEffect.IsPermanentEffect;
				record.MaintenanceFunded = activeEffect.MaintenanceFunded;
				record.TotalMaintenancePaidGold = activeEffect.TotalMaintenancePaidGold;
				record.LastMaintenanceSettlementDay = activeEffect.LastMaintenanceSettlementDay;
				record.LastEffectProcessedDay = activeEffect.LastEffectProcessedDay;
				if (IsLocalActivePolicyEffect(activeEffect)) record.TargetFiefIds = NormalizeIdList(activeEffect.TargetFiefIds);
			}
			_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-progress-update-failed", ex.Message);
		}
	}

	private void UpdateLocalPolicyTargets(string recordId, List<string> targetFiefIds)
	{
		try
		{
			if (!_localPolicyRecords.TryGetValue(recordId ?? "", out string raw)) return;
			LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
			if (record == null) return;
			record.TargetFiefIds = NormalizeIdList(targetFiefIds);
			_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-target-update-failed", ex.Message);
		}
	}

	private void MarkLocalPolicyEnded(ActivePolicyEffectSaveData activeEffect, string status, string reason)
	{
		if (activeEffect == null) return;
		activeEffect.RemainingDays = 0;
		activeEffect.Ended = true;
		activeEffect.EndReason = reason ?? "";
		try
		{
			if (_localPolicyRecords.TryGetValue(activeEffect.RecordId ?? "", out string raw))
			{
				LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
				if (record != null)
				{
					record.Status = status ?? LocalPolicyStatusExpired;
					record.EndReason = reason ?? "";
					record.RemainingDays = 0;
					record.ActiveEffectId = "";
					if (!IsMentionedLocalPolicyEffect(activeEffect))
					{
						record.TargetFiefIds = NormalizeIdList(activeEffect.TargetFiefIds);
					}
					foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
					{
						effect.ActiveEffectId = "";
						effect.RemainingDays = 0;
					}
					_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
				}
			}
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-end-update-failed", ex.Message);
		}
		if (IsVassalActivePolicyEffect(activeEffect))
		{
			RemoveVassalPolicyEffectsByRecordId(activeEffect.RecordId);
		}
		else
		{
			RemoveLocalPolicyEffectsByRecordId(activeEffect.RecordId);
		}
		_activePolicyEffectModelCache.Clear();
		TrimLocalPolicyRecords();
	}

	private void MarkPlayerLocalPolicyEffectEnded(ActivePolicyEffectSaveData activeEffect, string effectStatus, string reason)
	{
		if (activeEffect == null)
		{
			return;
		}
		activeEffect.RemainingDays = 0;
		activeEffect.Ended = true;
		activeEffect.EndReason = reason ?? string.Empty;
		try
		{
			if (_localPolicyRecords.TryGetValue(activeEffect.RecordId ?? string.Empty, out string raw))
			{
				LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
				if (record != null)
				{
					record.Status = LocalPolicyStatusActive;
					record.EffectStatus = effectStatus ?? LocalPolicyStatusExpired;
					record.EndReason = reason ?? string.Empty;
					record.RemainingDays = 0;
					record.ActiveEffectId = string.Empty;
					record.MaintenanceFunded = activeEffect.MaintenanceFunded;
					record.TotalMaintenancePaidGold = activeEffect.TotalMaintenancePaidGold;
					record.LastMaintenanceSettlementDay = activeEffect.LastMaintenanceSettlementDay;
					record.LastEffectProcessedDay = activeEffect.LastEffectProcessedDay;
					if (!IsMentionedLocalPolicyEffect(activeEffect))
					{
						record.TargetFiefIds = NormalizeIdList(activeEffect.TargetFiefIds);
					}
					foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(item => item != null))
					{
						effect.ActiveEffectId = string.Empty;
						effect.RemainingDays = 0;
						effect.IsEnded = true;
						effect.EndReason = reason ?? string.Empty;
					}
					_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
				}
			}
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-effect-end-update-failed", ex.Message);
		}
		_activePolicyEffectModelCache.Clear();
	}

	private List<string> GetLocalPolicySourceFiefIds(string recordId)
	{
		try
		{
			if (_localPolicyRecords.TryGetValue(recordId ?? "", out string raw))
			{
				LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
				if (record != null)
				{
					return NormalizeIdList(record.TargetFiefIds);
				}
			}
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-source-target-load-failed", ex.Message);
		}
		return new List<string>();
	}

	private void RemoveLocalPolicyEffectsByRecordId(string recordId)
	{
		RemoveRecordedPolicyEffectsByRecordId(recordId, PolicyScopeLocal);
	}

	private void RemoveVassalPolicyEffectsByRecordId(string recordId)
	{
		RemoveRecordedPolicyEffectsByRecordId(recordId, PolicyScopeVassal);
	}

	private void RemoveRecordedPolicyEffectsByRecordId(string recordId, string scopeKind)
	{
		string normalizedRecordId = (recordId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalizedRecordId))
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			try
			{
				ActivePolicyEffectSaveData effect = GetActivePolicyEffectForWork(item.Key, item.Value ?? "");
				if (string.Equals(effect?.ScopeKind ?? "", scopeKind ?? "", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(effect.RecordId ?? "", normalizedRecordId, StringComparison.OrdinalIgnoreCase))
				{
					RemoveActivePolicyEffect(item.Key);
				}
			}
			catch
			{
			}
		}
	}

	private Kingdom ResolveKingdomByIdOrName(string id, string name)
	{
		id = (id ?? "").Trim();
		name = (name ?? "").Trim();
		try
		{
			foreach (Kingdom kingdom in Kingdom.All.Where(k => k != null))
			{
				if (!string.IsNullOrWhiteSpace(id) && string.Equals(kingdom.StringId, id, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
				if (!string.IsNullOrWhiteSpace(name) && string.Equals(GetKingdomName(kingdom), name, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static int ClampPolicyEffectDurationDays(int durationDays)
	{
		if (durationDays <= 0)
		{
			return 0;
		}
		return durationDays;
	}

	private static PolicyEffectTargetResolver CreatePlayerPolicyEffectTargetResolver(
		PolicyDraftRequest request,
		IDictionary<string, PolicyTargetHandleSaveData> handleByKey)
	{
		return delegate(
			string targetHandle,
			IPolicyEffectModule module,
			out PolicyEffectResolvedTarget resolved,
			out string targetError)
		{
			resolved = null;
			targetError = string.Empty;
			if (targetHandle == null
				|| handleByKey == null
				|| !handleByKey.TryGetValue(targetHandle, out PolicyTargetHandleSaveData target)
				|| !IsPolicyTargetHandleAllowedForRequest(request, target)
				|| !TryMapPolicyEffectSelectorKind(target, out PolicyEffectTargetKind selectorKind))
			{
				targetError = "非法或越界目标句柄";
				return false;
			}
			PolicyEffectCanonicalTargetSet canonicalTargetSet = BuildPolicyEffectCanonicalTargetSet(
				request,
				new[] { targetHandle },
				new[] { target },
				module,
				out targetError);
			if (!string.IsNullOrWhiteSpace(targetError))
			{
				return false;
			}
			PlayerPolicyTargetAuthorization authorization = EnsurePlayerPolicyTargetAuthorization(request);
			if (!PolicyEffectTargetJurisdiction.TryApply(
				canonicalTargetSet,
				module,
				request?.PlayerKingdomId,
				request?.IssuerKingdomId,
				authorization.ExplicitCrossKingdomIds,
				preserveLegacyCrossKingdoms: false,
				failOnUnauthorized: true,
				out canonicalTargetSet,
				out targetError))
			{
				return false;
			}
			resolved = new PolicyEffectResolvedTarget
			{
				Handle = targetHandle,
				SelectorKind = selectorKind,
				CanonicalTargetSet = canonicalTargetSet
			};
			return true;
		};
	}

	private static bool TryCompileSparsePolicyEffects(
		PolicyDraftRequest request,
		int? returnedDurationDays,
		List<PolicyEffectDto> rawEffects,
		IReadOnlyCollection<string> authorizedSourceModuleIds,
		IReadOnlyCollection<string> detailedSourceModuleIds,
		out List<PolicyEffectDto> compiledEffects,
		out string error,
		bool allowAlreadyCompiled = true)
	{
		compiledEffects = new List<PolicyEffectDto>();
		error = "";
		bool isPermanentPlayerEffect = IsPermanentPlayerPolicyEffect(request);
		if (!returnedDurationDays.HasValue
			|| (isPermanentPlayerEffect ? returnedDurationDays.Value != 0 : returnedDurationDays.Value <= 0))
		{
			error = "durationDays 必须是正整数";
			return false;
		}
		int durationDays = isPermanentPlayerEffect
			? 0
			: request?.ManualDurationDays > 0 ? request.ManualDurationDays : returnedDurationDays.Value;
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request?.TargetHandles);
		Dictionary<string, PolicyTargetHandleSaveData> handleByKey = handles.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
		if (handleByKey.Count <= 0)
		{
			error = "本次请求没有可引用的合法目标句柄";
			return false;
		}
		List<PolicyEffectDto> effects = (rawEffects ?? new List<PolicyEffectDto>()).Where(x => x != null).ToList();
		if (effects.Count > MaxCompiledPolicyEffectInstances)
		{
            error = "effects 最多允许 " + MaxCompiledPolicyEffectInstances.ToString(CultureInfo.InvariantCulture) + " 个模块实例";
			return false;
		}
		bool alreadyCompiled = allowAlreadyCompiled
			&& effects.Count > 0
			&& effects.All(x => !string.IsNullOrWhiteSpace(x.TargetHandle)
				&& x.Targets == null
				&& x.Changes == null
				&& (x.PreparedModuleEffect != null || string.IsNullOrWhiteSpace(x.ModuleId))
				&& !HasLegacyPolicyEffectShape(x));
		if (!alreadyCompiled && effects.Count > MaxWirePolicyEffects)
		{
			error = "effects wire 最多允许 " + MaxWirePolicyEffects.ToString(CultureInfo.InvariantCulture) + " 条";
			return false;
		}
		if (alreadyCompiled)
		{
			foreach (PolicyEffectDto effect in effects)
			{
				string targetKey = (effect.TargetHandle ?? "").Trim();
				if (!handleByKey.TryGetValue(targetKey, out PolicyTargetHandleSaveData target))
				{
                    error = "未知目标句柄：" + targetKey;
					return false;
				}
				PolicyEffectDto copy = CloneCompiledPolicyEffect(effect, target, durationDays);
				compiledEffects.Add(copy);
			}
			return EnsureSparsePolicyLifecycleAnchor(request, handleByKey, durationDays, compiledEffects, out error);
		}

		List<PolicyEffectWireEffect> wireEffects = new List<PolicyEffectWireEffect>();
		for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
		{
			PolicyEffectDto group = effects[effectIndex];
			bool hasModuleWireShape = !string.IsNullOrWhiteSpace(group.ModuleId)
				|| group.TargetHandles != null
				|| group.Payload != null;
			if (hasModuleWireShape)
			{
				if (group.Targets != null || group.Changes != null || HasLegacyPolicyEffectShape(group))
				{
					error = "结构化 moduleId/targetHandles/payload 不得与 changes 或旧固定字段混用";
					return false;
				}
				wireEffects.Add(new PolicyEffectWireEffect
				{
					SourceModuleId = group.SourceModuleId ?? string.Empty,
					EffectPlanVersion = group.EffectPlanVersion,
					MechanismId = group.MechanismId,
					MechanismKind = group.MechanismKind,
					MechanismRole = group.MechanismRole,
					SourceOmitted = group.SourceOmitted,
					DestinationOmitted = group.DestinationOmitted,
					ModuleId = (group.ModuleId ?? string.Empty).Trim(),
					TargetHandles = group.TargetHandles == null ? null : new List<string>(group.TargetHandles),
					Payload = group.Payload?.DeepClone(),
					Reason = NormalizePolicyEffectCompileReason(group.Reason)
				});
				continue;
			}
			List<string> targetKeys = (group.Targets ?? new List<string>())
				.Select(x => (x ?? "").Trim())
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToList();
			if (HasLegacyPolicyEffectShape(group))
			{
				if (targetKeys.Count == 0 && !string.IsNullOrWhiteSpace(group.TargetHandle))
				{
					targetKeys.Add(group.TargetHandle.Trim());
				}
				if (!TryReadLegacyPolicyEffectValues(group, out IReadOnlyDictionary<string, float> legacyValues, out error))
				{
					return false;
				}
				if (legacyValues.Count == 0)
				{
					error = "旧版扁平 effects 没有可迁移的数值字段";
					return false;
				}
				foreach (KeyValuePair<string, float> legacyValue in legacyValues)
				{
					wireEffects.Add(new PolicyEffectWireEffect
					{
						EffectPlanVersion = group.EffectPlanVersion,
						MechanismId = group.MechanismId,
						MechanismKind = group.MechanismKind,
						MechanismRole = group.MechanismRole,
						SourceOmitted = group.SourceOmitted,
						DestinationOmitted = group.DestinationOmitted,
						ModuleId = legacyValue.Key,
						TargetHandles = new List<string>(targetKeys),
						Payload = new JValue(legacyValue.Value),
						Reason = NormalizePolicyEffectCompileReason(group.Reason)
					});
				}
				continue;
			}
			if (targetKeys.Count <= 0 || targetKeys.Count != targetKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
			{
				error = "第 " + (effectIndex + 1).ToString(CultureInfo.InvariantCulture) + " 条 effect 的 targets 必须非空且不能重复";
				return false;
			}
			if (group.Changes == null)
			{
				error = "第 " + (effectIndex + 1).ToString(CultureInfo.InvariantCulture) + " 条 effect 缺少 changes 对象";
				return false;
			}
			foreach (string targetKey in targetKeys)
			{
				if (!handleByKey.TryGetValue(targetKey, out PolicyTargetHandleSaveData target)
					|| !IsPolicyTargetHandleAllowedForRequest(request, target))
				{
					error = "非法或越界目标句柄：" + targetKey;
					return false;
				}
			}
			foreach (KeyValuePair<string, float> change in group.Changes)
			{
				wireEffects.Add(new PolicyEffectWireEffect
				{
					EffectPlanVersion = group.EffectPlanVersion,
					MechanismId = group.MechanismId,
					MechanismKind = group.MechanismKind,
					MechanismRole = group.MechanismRole,
					SourceOmitted = group.SourceOmitted,
					DestinationOmitted = group.DestinationOmitted,
					ModuleId = (change.Key ?? string.Empty).Trim(),
					TargetHandles = new List<string>(targetKeys),
					Payload = new JValue(change.Value),
					Reason = NormalizePolicyEffectCompileReason(group.Reason)
				});
			}
		}
		string policyId = (request?.RequestId ?? string.Empty).Trim();
		float startDay = Math.Max(0, request?.SubmittedDay ?? 0);
		PlayerPolicyTargetAuthorization targetAuthorization = EnsurePlayerPolicyTargetAuthorization(request);
		PolicyEffectCompilerRequest compilerRequest = new PolicyEffectCompilerRequest
		{
			PolicyId = policyId,
			ActorHeroId = Hero.MainHero?.StringId ?? string.Empty,
			ActorClanId = Campaign.Current != null ? Clan.PlayerClan?.StringId ?? string.Empty : string.Empty,
			IssuerKingdomId = request?.IssuerKingdomId ?? string.Empty,
			TargetKingdomId = request?.PlayerKingdomId ?? string.Empty,
			AuthorizedCrossKingdomIds = targetAuthorization.ExplicitCrossKingdomIds.ToArray(),
			StartDay = startDay,
			EndDay = isPermanentPlayerEffect ? 0f : startDay + durationDays,
			IsPermanentEffect = isPermanentPlayerEffect,
			Scope = request?.ScopeKind ?? PolicyScopeKingdom,
			Funding = BuildPolicyEffectFundingContext(request),
			CandidateModuleIds = authorizedSourceModuleIds ?? Array.Empty<string>(),
			DetailedModuleIds = detailedSourceModuleIds ?? Array.Empty<string>(),
			EnforceDetailedModuleAuthorization = false,
			PromptAuthorizedModuleIds = null,
			MaxInstances = MaxWirePolicyEffects,
			MaxCompiledInstances = MaxCompiledPolicyEffectInstances,
			MaxPayloadBytes = MaxPolicyEffectPayloadBytes,
			MaxTotalPayloadBytes = MaxPolicyEffectPayloadTotalBytes
		};
		PolicyEffectTargetResolver targetResolver = CreatePlayerPolicyEffectTargetResolver(request, handleByKey);
		PolicyEffectInstanceIdFactory instanceIdFactory = (ordinal, moduleId, targetSet) =>
			FirstNonEmpty(policyId, "policy-" + Math.Max(0, request?.SubmittedDay ?? 0).ToString(CultureInfo.InvariantCulture))
			+ ":effect:" + ordinal.ToString(CultureInfo.InvariantCulture);
		if (!PolicyEffectCompiler.TryCompile(
			wireEffects,
			compilerRequest,
			targetResolver,
			instanceIdFactory,
			out PolicyEffectCompilerResult compilerResult,
			out error))
		{
			return false;
		}
		foreach (string outsideModuleId in compilerResult.OutsideDetailedRecallModuleIds)
		{
			PolicyDebugLog("effect-module-outside-recall", BuildPolicyRequestLogPrefix(request)
				+ " moduleId=" + outsideModuleId + " outsideDetailed=true");
		}
		foreach (PolicyEffectCompiledWireEffect item in compilerResult.Effects)
		{
			JToken normalizedPayloadToken = JToken.FromObject(item.NormalizedPayload);
			PolicyEffectCanonicalTargetSet rebuiltTargetSet = null;
			// A compiler wire remains one logical instance. Its target shells share the stable
			// instance id, but own independent prepared objects and target-specific target sets.
			foreach (string targetKey in item.TargetHandles)
			{
				if (!handleByKey.TryGetValue(targetKey, out PolicyTargetHandleSaveData target))
				{
					error = "未知目标句柄：" + targetKey;
					return false;
				}
				PolicyEffectCanonicalTargetSet shellTargetSet = BuildPolicyEffectCanonicalTargetSet(
					request,
					new[] { targetKey },
					new[] { target },
					item.Module,
					out string shellTargetError);
				if (!string.IsNullOrWhiteSpace(shellTargetError))
				{
					error = shellTargetError;
					return false;
				}
				shellTargetSet = PolicyEffectCompiler.ApplyActorClanTargetExclusion(
					item.Module,
					compilerRequest.ActorClanId,
					shellTargetSet);
				if (!PolicyEffectTargetJurisdiction.TryApply(
					shellTargetSet,
					item.Module,
					request?.PlayerKingdomId,
					request?.IssuerKingdomId,
					targetAuthorization.ExplicitCrossKingdomIds,
					preserveLegacyCrossKingdoms: false,
					failOnUnauthorized: true,
					out shellTargetSet,
					out shellTargetError))
				{
					error = shellTargetError;
					return false;
				}
				if (!HasPolicyEffectCanonicalTargetsForModule(item.Module, shellTargetSet))
				{
					error = "模块 " + item.Module.Id + " 的目标句柄 " + targetKey + " 没有可执行的规范目标";
					return false;
				}
				if (!TryCreatePolicyEffectTargetShellPreparedInstance(
					item,
					shellTargetSet,
					out PolicyEffectPreparedInstance shellPrepared,
					out error))
				{
					return false;
				}
				PolicyEffectDto compiled = CreateEmptyCompiledPolicyEffect(
					target,
					targetKey,
					durationDays,
					item.SaveData?.Reason ?? string.Empty);
				compiled.EffectPlanVersion = item.EffectPlanVersion;
				compiled.MechanismId = item.MechanismId;
				compiled.MechanismKind = item.MechanismKind;
				compiled.MechanismRole = item.MechanismRole;
				compiled.SourceOmitted = item.SourceOmitted;
				compiled.DestinationOmitted = item.DestinationOmitted;
				compiled.ModuleId = item.Module.Id;
				compiled.SourceModuleId = item.SourceModuleId;
				compiled.TargetHandles = new List<string> { targetKey };
				compiled.Payload = normalizedPayloadToken.DeepClone();
				compiled.PreparedModuleEffect = shellPrepared;
				compiledEffects.Add(compiled);
				rebuiltTargetSet = MergePolicyEffectCanonicalTargetSets(rebuiltTargetSet, shellTargetSet);
			}
			if (!AreSamePolicyEffectCanonicalTargetSets(item.TargetSet, rebuiltTargetSet))
			{
				error = "模块 " + item.Module.Id + " 的目标壳无法无损重建 compiler 规范目标集合";
				return false;
			}
		}
		return EnsureSparsePolicyLifecycleAnchor(request, handleByKey, durationDays, compiledEffects, out error);
	}

	private static string NormalizePolicyEffectCompileReason(string reason)
	{
		return LimitDisplayChars(CompactPolicyContextText(reason ?? string.Empty), 60);
	}

	private static bool TryValidatePolicyEffectPayloadSize(JToken payload, ref int totalPayloadBytes, out string error)
	{
		error = string.Empty;
		if (ContainsPolicyEffectTypeMetadata(payload))
		{
			error = "policy effect payload 不得包含 $type 元数据";
			return false;
		}
		int payloadBytes = Encoding.UTF8.GetByteCount(payload?.ToString(Formatting.None) ?? "null");
		if (payloadBytes > MaxPolicyEffectPayloadBytes)
		{
			error = "单个 policy effect payload 不得超过 "
				+ MaxPolicyEffectPayloadBytes.ToString(CultureInfo.InvariantCulture) + " bytes";
			return false;
		}
		if (totalPayloadBytes > MaxPolicyEffectPayloadTotalBytes - payloadBytes)
		{
			error = "policy effect payload 总量不得超过 "
				+ MaxPolicyEffectPayloadTotalBytes.ToString(CultureInfo.InvariantCulture) + " bytes";
			return false;
		}
		totalPayloadBytes += payloadBytes;
		return true;
	}

	private static bool ContainsPolicyEffectTypeMetadata(JToken token)
	{
		if (token is JObject objectToken)
		{
			foreach (JProperty property in objectToken.Properties())
			{
				if (string.Equals(property.Name, "$type", StringComparison.OrdinalIgnoreCase)
					|| ContainsPolicyEffectTypeMetadata(property.Value))
				{
					return true;
				}
			}
		}
		else if (token is JArray arrayToken)
		{
			foreach (JToken child in arrayToken)
			{
				if (ContainsPolicyEffectTypeMetadata(child))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static PolicyEffectFundingContext BuildPolicyEffectFundingContext(PolicyDraftRequest request)
	{
		return new PolicyEffectFundingContext
		{
			RequiredGold = Math.Max(0, request?.RequiredGoldCost ?? 0),
			PaidGold = Math.Max(0, request?.GoldCost ?? 0),
			RequiredInfluence = NormalizePolicyEffectFundingInteger(request?.RequiredInfluenceCost ?? 0f),
			PaidInfluence = NormalizePolicyEffectFundingInteger(request?.InfluenceCost ?? 0f),
			GoldScale = request?.GoldEffectScale ?? 1f,
			InfluenceScale = request?.InfluenceEffectScale ?? 1f
		};
	}

	private static int NormalizePolicyEffectFundingInteger(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
		{
			return 0;
		}
		return value >= int.MaxValue
			? int.MaxValue
			: (int)Math.Round(value, MidpointRounding.AwayFromZero);
	}

	private static bool TryMapPolicyEffectSelectorKind(PolicyTargetHandleSaveData target, out PolicyEffectTargetKind targetKind)
	{
		targetKind = PolicyEffectTargetKind.Settlement;
		if (target == null)
		{
			return false;
		}
		if (string.Equals(target.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(target.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase))
		{
			targetKind = PolicyEffectTargetKind.Settlement;
			return true;
		}
		if (string.Equals(target.Kind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(target.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase))
		{
			targetKind = PolicyEffectTargetKind.Clan;
			return true;
		}
		if (string.Equals(target.Kind, PolicyTargetKindKingdom, StringComparison.OrdinalIgnoreCase))
		{
			targetKind = PolicyEffectTargetKind.Kingdom;
			return true;
		}
		if (string.Equals(target.Kind, PolicyTargetKindHero, StringComparison.OrdinalIgnoreCase)
			&& PolicyHeroTargetSelectorResolver.IsKnownSelector(target.SelectorId))
		{
			targetKind = PolicyEffectTargetKind.Hero;
			return true;
		}
		if (string.Equals(target.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
			&& PolicyTargetSelectorCatalog.TryGet(target.SelectorId, out PolicyTargetSelectorDescriptor descriptor))
		{
			targetKind = descriptor.OutputTargetKind;
			return true;
		}
		if (string.Equals(target.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase)
			&& PolicyTargetPlanResolver.TryNormalizeAndValidate(target.TargetPlan, out PolicyTargetPlanSaveData plan, out _)
			&& plan.Branches.Count > 0)
		{
			switch (plan.Branches[0].Universe)
			{
				case PolicyTargetPlanUniverse.PrimaryFiefs:
					targetKind = PolicyEffectTargetKind.Settlement;
					return true;
				case PolicyTargetPlanUniverse.Clans:
					targetKind = PolicyEffectTargetKind.Clan;
					return true;
				case PolicyTargetPlanUniverse.Kingdoms:
					targetKind = PolicyEffectTargetKind.Kingdom;
					return true;
			}
		}
		return false;
	}

	private static PolicyEffectCanonicalTargetSet BuildPolicyEffectCanonicalTargetSet(
		PolicyDraftRequest request,
		IEnumerable<string> targetKeys,
		IEnumerable<PolicyTargetHandleSaveData> targets)
	{
		return BuildPolicyEffectCanonicalTargetSet(request, targetKeys, targets, null, out _);
	}

	private static PolicyEffectCanonicalTargetSet BuildPolicyEffectCanonicalTargetSet(
		PolicyDraftRequest request,
		IEnumerable<string> targetKeys,
		IEnumerable<PolicyTargetHandleSaveData> targets,
		IPolicyEffectModule module)
	{
		return BuildPolicyEffectCanonicalTargetSet(request, targetKeys, targets, module, out _);
	}

	private static PolicyEffectCanonicalTargetSet BuildPolicyEffectCanonicalTargetSet(
		PolicyDraftRequest request,
		IEnumerable<string> targetKeys,
		IEnumerable<PolicyTargetHandleSaveData> targets,
		IPolicyEffectModule module,
		out string error)
	{
		error = string.Empty;
		PolicyEffectCanonicalTargetSet result = new PolicyEffectCanonicalTargetSet
		{
			SelectorHandles = NormalizeIdList(targetKeys),
			SelectorIds = NormalizeIdList((targets ?? Enumerable.Empty<PolicyTargetHandleSaveData>())
				.Where(target => string.Equals(target?.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(target?.Kind, PolicyTargetKindHero, StringComparison.OrdinalIgnoreCase))
				.Select(target => target.SelectorId)),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans((targets ?? Enumerable.Empty<PolicyTargetHandleSaveData>())
				.Where(target => string.Equals(target?.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
				.Select(target => target.TargetPlan))
		};
		List<PolicyTargetHandleSaveData> normalizedTargets = (targets ?? Enumerable.Empty<PolicyTargetHandleSaveData>())
			.Where(target => target != null)
			.ToList();
		List<Settlement> sourceFiefs = ResolvePolicyEffectSettlementsById(request?.SelectedFiefIds)
			.Select(ResolvePrimaryPolicyFief)
			.Where(settlement => settlement != null)
			.GroupBy(settlement => settlement.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		HashSet<string> sourceSettlementIds = new HashSet<string>(
			sourceFiefs
				.Select(settlement => settlement?.StringId ?? string.Empty)
				.Where(id => id.Length > 0),
			StringComparer.OrdinalIgnoreCase);
		foreach (PolicyTargetHandleSaveData target in normalizedTargets)
		{
			bool isSource = string.Equals(target.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase);
			List<Settlement> resolved = new List<Settlement>();
			if (isSource)
			{
				resolved.AddRange(sourceFiefs);
			}
			else if (string.Equals(target.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase))
			{
				Settlement primary = ResolvePrimaryPolicyFief(ResolvePolicyEffectSettlementById(target.EntityId));
				if (primary != null)
				{
					resolved.Add(primary);
				}
				else if (!string.IsNullOrWhiteSpace(target.EntityId))
				{
					AddUniquePolicyEffectId(result.ParentSettlementIds, target.EntityId);
					AddUniquePolicyEffectId(result.SettlementIds, target.EntityId);
					AddUniquePolicyEffectId(result.TownIds, target.EntityId);
				}
			}
			else if (string.Equals(target.Kind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(target.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase))
			{
				Clan clan = ResolvePolicyEffectTargetClan(request, target);
				if (!string.IsNullOrWhiteSpace(clan?.StringId)
					&& !clan.IsEliminated
					&& (clan.Kingdom != null
						|| module?.Descriptor?.AllowIndependentClanTargets == true))
				{
					AddUniquePolicyEffectId(result.ClanIds, clan.StringId);
					resolved.AddRange((clan.Settlements ?? Enumerable.Empty<Settlement>())
						.Select(ResolvePrimaryPolicyFief)
						.Where(primary => primary != null));
				}
				result.FollowCurrentRulingClan |= target.FollowCurrentRulingClan;
			}
			else if (string.Equals(target.Kind, PolicyTargetKindKingdom, StringComparison.OrdinalIgnoreCase))
			{
				Kingdom kingdom = ResolvePolicyEffectTargetKingdom(request, target);
				string kingdomId = FirstNonEmpty(kingdom?.StringId, target.KingdomId, target.EntityId);
				AddUniquePolicyEffectId(result.KingdomIds, kingdomId);
				if (module?.Descriptor?.TargetKinds?.Contains(PolicyEffectTargetKind.Clan) == true)
				{
					foreach (Clan clan in ((IEnumerable<Clan>)kingdom?.Clans ?? Enumerable.Empty<Clan>())
						.Where(clan => clan != null
							&& !clan.IsEliminated
							&& clan.Kingdom == kingdom
							&& !string.IsNullOrWhiteSpace(clan.StringId)))
					{
						AddUniquePolicyEffectId(result.ClanIds, clan.StringId);
					}
				}
				resolved.AddRange(GetKingdomSettlements(kingdom));
			}
			else if (string.Equals(target.Kind, PolicyTargetKindHero, StringComparison.OrdinalIgnoreCase))
			{
				if (PolicyHeroTargetSelectorResolver.TryProjectSelector(
					target.SelectorId,
					module,
					Math.Max(0, request?.SubmittedDay ?? 0),
					out PolicyEffectCanonicalTargetSet projected,
					out string heroError))
				{
					foreach (string heroId in projected.HeroIds) AddUniquePolicyEffectId(result.HeroIds, heroId);
					foreach (string clanId in projected.ClanIds) AddUniquePolicyEffectId(result.ClanIds, clanId);
				}
				else
				{
					PolicySystemLog.Failure("Player", "hero-target-resolve-failed", heroError,
						"selectorId=" + (target.SelectorId ?? string.Empty));
				}
			}
			else if (string.Equals(target.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase))
			{
				Kingdom selectorKingdom = ResolvePolicyEffectTargetKingdom(request, target);
				string excludedClanId = Clan.PlayerClan?.StringId ?? string.Empty;
				if (TryResolvePolicyTargetSelectorSettlements(
					target.SelectorId,
					request?.ScopeKind,
					selectorKingdom,
					excludedClanId,
					out List<Settlement> selectorSettlements,
					out string selectorError))
				{
					resolved.AddRange(selectorSettlements);
					PolicySystemLog.Write("Player", "target-selector-resolved",
						"selectorId=" + target.SelectorId
						+ " kingdomId=" + (selectorKingdom?.StringId ?? string.Empty)
						+ " settlementCount=" + selectorSettlements.Count.ToString(CultureInfo.InvariantCulture));
				}
				else
				{
					PolicySystemLog.Failure("Player", "target-selector-resolve-failed",
						"selectorId=" + (target.SelectorId ?? string.Empty) + " " + selectorError);
				}
			}
			else if (string.Equals(target.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
			{
				if (TryResolvePolicyTargetPlanForRequest(request, target.TargetPlan, out PolicyTargetPlanResolution planResolution, out string planError))
				{
					IReadOnlyList<string> planPrimaryIds = PolicyTargetPlanResolver.ExpandPrimarySettlementIds(
						planResolution,
						request?.SemanticTargetSnapshot);
					foreach (string clanId in planResolution.ClanIds)
					{
						AddUniquePolicyEffectId(result.ClanIds, clanId);
					}
					foreach (string kingdomId in planResolution.KingdomIds)
					{
						AddUniquePolicyEffectId(result.KingdomIds, kingdomId);
					}
					if (module?.Descriptor?.TargetKinds?.Contains(PolicyEffectTargetKind.Clan) == true
						&& planPrimaryIds.Count > 0)
					{
						HashSet<string> planPrimaryIdSet = new HashSet<string>(planPrimaryIds, StringComparer.OrdinalIgnoreCase);
						foreach (PolicyTargetEntitySnapshot entity in request?.SemanticTargetSnapshot?.Entities
							?? Array.Empty<PolicyTargetEntitySnapshot>())
						{
							if (entity != null && planPrimaryIdSet.Contains(entity.EntityId ?? string.Empty))
							{
								AddUniquePolicyEffectId(result.ClanIds, entity.OwnerClanId);
							}
						}
					}
					resolved.AddRange(ResolvePolicyEffectSettlementsById(planPrimaryIds)
						.Select(ResolvePrimaryPolicyFief)
						.Where(primary => primary != null));
					string resolvedMessage = "signature=" + (target.TargetPlan?.NormalizedSignature ?? string.Empty)
						+ " primaryCount=" + planPrimaryIds.Count.ToString(CultureInfo.InvariantCulture)
						+ " temporarilyEmpty=" + (planResolution.IsTemporarilyEmpty ? "true" : "false");
					PolicyEffectCanonicalTargetSet planTargetSet = new PolicyEffectCanonicalTargetSet
					{
						ParentSettlementIds = planPrimaryIds.ToList(),
						ClanIds = (planResolution.ClanIds ?? Array.Empty<string>()).ToList(),
						KingdomIds = (planResolution.KingdomIds ?? Array.Empty<string>()).ToList(),
						TargetPlans = target.TargetPlan == null
							? new List<PolicyTargetPlanSaveData>()
							: new List<PolicyTargetPlanSaveData> { target.TargetPlan }
					};
					PolicyLogContext resolvedContext = BuildPolicyLogContext(request);
					resolvedContext.TargetKind = PolicyTargetKindPlan;
					resolvedContext.TargetKeys = target.Key;
					resolvedContext.TargetCount = planPrimaryIds.Count;
					resolvedContext.PlanSignature = target.TargetPlan?.NormalizedSignature;
					resolvedContext.TargetSummary = "temporarilyEmpty=" + (planResolution.IsTemporarilyEmpty ? "true" : "false")
						+ "; " + BuildPolicyEffectTargetSetLogSummary(planTargetSet);
					resolvedContext.MessageChars = resolvedMessage.Length;
					resolvedContext.MessageHash = PolicySystemLog.HashSensitive(resolvedMessage);
					PolicySystemLog.Lifecycle("Player", "target-plan-resolved", "event", resolvedContext);
				}
				else
				{
					PolicySystemLog.Failure("Player", "target-plan-resolve-failed", planError,
						"signature=" + (target.TargetPlan?.NormalizedSignature ?? string.Empty));
				}
			}

			foreach (Settlement settlement in resolved
				.Select(ResolvePrimaryPolicyFief)
				.Where(settlement => settlement != null && (settlement.IsTown || settlement.IsCastle))
				.GroupBy(settlement => settlement.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First()))
			{
				string settlementId = (settlement.StringId ?? string.Empty).Trim();
				if (!isSource
					&& !string.Equals(target.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase)
					&& IsLocalPolicyRequest(request)
					&& sourceSettlementIds.Contains(settlementId))
				{
					continue;
				}
				if (!AddPolicyEffectPrimaryTargetForModule(
					result,
					settlement,
					module,
					ResolvePolicyEffectPrimaryTargetOrigin(target),
					out error))
				{
					return NormalizePolicyEffectCanonicalTargetSet(result);
				}
			}
		}
		return NormalizePolicyEffectCanonicalTargetSet(result);
	}

	internal static bool AddPolicyEffectPrimaryTargetForModule(
		PolicyEffectCanonicalTargetSet targetSet,
		Settlement primary,
		IPolicyEffectModule module,
		PolicyEffectPrimaryTargetOrigin origin,
		out string error)
	{
		error = string.Empty;
		if (targetSet == null || primary == null || !(primary.IsTown || primary.IsCastle))
		{
			return true;
		}
		string primaryId = (primary.StringId ?? string.Empty).Trim();
		if (primaryId.Length == 0)
		{
			return true;
		}
		AddUniquePolicyEffectId(targetSet.ParentSettlementIds, primaryId);
		IReadOnlyCollection<PolicyEffectTargetKind> targetKinds = module?.Descriptor?.TargetKinds;
		bool legacyProjection = targetKinds == null;
		if (legacyProjection || targetKinds.Contains(PolicyEffectTargetKind.Settlement))
		{
			AddUniquePolicyEffectId(targetSet.SettlementIds, primaryId);
		}
		if (module?.Descriptor?.TargetProjection == PolicyEffectTargetProjectionKind.PrimaryFiefAndBoundSettlements)
		{
			foreach (Settlement village in GetBoundVillageSettlements(primary))
			{
				AddUniquePolicyEffectId(targetSet.SettlementIds, (village?.StringId ?? string.Empty).Trim());
			}
		}
		if (legacyProjection || targetKinds.Contains(PolicyEffectTargetKind.Town))
		{
			AddUniquePolicyEffectId(targetSet.TownIds, primaryId);
		}
		if (legacyProjection || targetKinds.Contains(PolicyEffectTargetKind.Village))
		{
			foreach (Settlement village in GetBoundVillageSettlements(primary))
			{
				string villageId = (village?.StringId ?? string.Empty).Trim();
				AddUniquePolicyEffectId(targetSet.VillageIds, villageId);
				if (legacyProjection)
				{
					AddUniquePolicyEffectId(targetSet.SettlementIds, villageId);
				}
			}
		}
		if (targetKinds?.Contains(PolicyEffectTargetKind.Clan) == true
			&& primary.OwnerClan != null
			&& !primary.OwnerClan.IsEliminated
			&& (primary.OwnerClan.Kingdom != null
				|| module?.Descriptor?.AllowIndependentClanTargets == true))
		{
			AddUniquePolicyEffectId(targetSet.ClanIds, primary.OwnerClan.StringId);
		}
		if (HasSettlementOwnerLeaderProjection(module)
			&& IsSettlementOwnerLeaderProjectionOrigin(origin))
		{
			Clan ownerClan = primary.OwnerClan;
			Hero leader = ownerClan?.Leader;
			if (ownerClan == null
				|| ownerClan.IsEliminated
				|| leader == null
				|| !leader.IsActive
				|| string.IsNullOrWhiteSpace(leader.StringId))
			{
				error = "Settlement " + primaryId
					+ " has no active owner clan leader for "
					+ module.Descriptor.TargetProjection + " projection.";
				return false;
			}
			AddUniquePolicyEffectId(targetSet.HeroIds, leader.StringId);
		}
		return true;
	}

	private static bool IsSettlementOwnerLeaderProjectionOrigin(PolicyEffectPrimaryTargetOrigin origin)
	{
		return origin == PolicyEffectPrimaryTargetOrigin.SettlementSelector
			|| origin == PolicyEffectPrimaryTargetOrigin.TargetPlanPrimarySettlement
			|| origin == PolicyEffectPrimaryTargetOrigin.LegacyCanonicalSettlement;
	}

	private static bool TryResolvePolicyTargetSelectorSettlements(
		string selectorId,
		string scope,
		Kingdom targetKingdom,
		string excludedClanId,
		out List<Settlement> settlements,
		out string error)
	{
		settlements = new List<Settlement>();
		error = string.Empty;
		if (targetKingdom == null || string.IsNullOrWhiteSpace(targetKingdom.StringId))
		{
			error = "政策目标 selector 缺少固定王国。";
			return false;
		}
		List<Settlement> primaryFiefs = GetKingdomSettlements(targetKingdom)
			.Select(ResolvePrimaryPolicyFief)
			.Where(primary => primary != null && (primary.IsTown || primary.IsCastle))
			.GroupBy(primary => primary.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		PolicyTargetSelectorResolutionContext context = new PolicyTargetSelectorResolutionContext
		{
			Scope = scope ?? string.Empty,
			TargetKingdomId = targetKingdom.StringId,
			ExcludedClanId = excludedClanId ?? string.Empty,
			Entities = primaryFiefs.Select(primary => new PolicyTargetSelectorEntitySnapshot
			{
				EntityId = primary.StringId ?? string.Empty,
				OwnerKingdomId = primary.OwnerClan?.Kingdom?.StringId ?? string.Empty,
				OwnerClanId = primary.OwnerClan?.StringId ?? string.Empty,
				IsPrimaryFief = true,
				BoundVillageIds = GetBoundVillageSettlements(primary)
					.Select(village => village?.StringId ?? string.Empty)
					.Where(id => id.Length > 0)
					.ToArray()
			}).ToArray()
		};
		if (!PolicyTargetSelectorCatalog.TryResolve(selectorId, context, out PolicyTargetSelectorResolution resolution, out error))
		{
			return false;
		}
		HashSet<string> ids = new HashSet<string>(resolution.SettlementIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		settlements = ExpandLocalPolicySettlements(primaryFiefs)
			.Where(settlement => settlement != null && ids.Contains(settlement.StringId ?? string.Empty))
			.GroupBy(settlement => settlement.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First())
			.ToList();
		if (settlements.Count <= 0)
		{
			error = "政策目标 selector 的实时对象已经失效。";
			return false;
		}
		return true;
	}

	private static List<Settlement> ResolvePolicyEffectSettlementsById(IEnumerable<string> ids)
	{
		HashSet<string> requested = new HashSet<string>(NormalizeIdList(ids), StringComparer.OrdinalIgnoreCase);
		return requested.Count <= 0 || Campaign.Current == null
			? new List<Settlement>()
			: (Settlement.All ?? Enumerable.Empty<Settlement>())
				.Where(settlement => settlement != null && requested.Contains(settlement.StringId ?? string.Empty))
				.ToList();
	}

	private static Settlement ResolvePolicyEffectSettlementById(string id)
	{
		string normalized = (id ?? string.Empty).Trim();
		return normalized.Length <= 0 || Campaign.Current == null
			? null
			: (Settlement.All ?? Enumerable.Empty<Settlement>()).FirstOrDefault(
				settlement => settlement != null && string.Equals(settlement.StringId, normalized, StringComparison.OrdinalIgnoreCase));
	}

	private static Clan ResolvePolicyEffectTargetClan(PolicyDraftRequest request, PolicyTargetHandleSaveData target)
	{
		if (target?.FollowCurrentRulingClan == true)
		{
			return ResolvePolicyEffectTargetKingdom(request, target)?.RulingClan;
		}
		string clanId = (target?.EntityId ?? string.Empty).Trim();
		return clanId.Length <= 0 || Campaign.Current == null
			? null
			: (Clan.All ?? Enumerable.Empty<Clan>()).FirstOrDefault(
				clan => clan != null && string.Equals(clan.StringId, clanId, StringComparison.OrdinalIgnoreCase));
	}

	private static Kingdom ResolvePolicyEffectTargetKingdom(PolicyDraftRequest request, PolicyTargetHandleSaveData target)
	{
		string kingdomId = FirstNonEmpty(target?.KingdomId, target?.EntityId);
		if (Campaign.Current == null)
		{
			return null;
		}
		return (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(
			kingdom => kingdom != null && string.Equals(kingdom.StringId, kingdomId, StringComparison.OrdinalIgnoreCase));
	}

	private static void AddUniquePolicyEffectId(List<string> ids, string value)
	{
		string normalized = (value ?? string.Empty).Trim();
		if (normalized.Length > 0 && !ids.Contains(normalized, StringComparer.OrdinalIgnoreCase))
		{
			ids.Add(normalized);
		}
	}

	private static void RefreshPolicyHeroSelectorTargets(
		PolicyEffectCanonicalTargetSet current,
		IPolicyEffectModule module,
		List<string> refreshedHeroIds,
		List<string> refreshedClanIds)
	{
		if (module == null || current?.SelectorIds == null
			|| refreshedHeroIds == null || refreshedClanIds == null)
		{
			return;
		}
		int campaignDay = CurrentPolicyEffectCampaignDay();
		foreach (string selectorId in current.SelectorIds.Where(PolicyHeroTargetSelectorResolver.IsKnownSelector))
		{
			if (!PolicyHeroTargetSelectorResolver.TryProjectSelector(
				selectorId,
				module,
				campaignDay,
				out PolicyEffectCanonicalTargetSet projected,
				out _))
			{
				continue;
			}
			foreach (string heroId in projected.HeroIds)
			{
				AddUniquePolicyEffectId(refreshedHeroIds, heroId);
			}
			foreach (string clanId in projected.ClanIds)
			{
				AddUniquePolicyEffectId(refreshedClanIds, clanId);
			}
		}
	}

	private static int CurrentPolicyEffectCampaignDay()
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

	private static PolicyEffectCanonicalTargetSet NormalizePolicyEffectCanonicalTargetSet(PolicyEffectCanonicalTargetSet targetSet)
	{
		return PolicyEffectBundleContract.NormalizeTargetSet(targetSet);
	}

	private static bool HasAnyPolicyEffectCanonicalTarget(PolicyEffectCanonicalTargetSet targetSet)
	{
		return targetSet != null
			&& ((targetSet.SettlementIds?.Count ?? 0) > 0
				|| (targetSet.TownIds?.Count ?? 0) > 0
				|| (targetSet.VillageIds?.Count ?? 0) > 0
				|| (targetSet.ClanIds?.Count ?? 0) > 0
				|| (targetSet.KingdomIds?.Count ?? 0) > 0
				|| (targetSet.HeroIds?.Count ?? 0) > 0);
	}

	private static bool HasPolicyEffectCanonicalTargetsForModule(IPolicyEffectModule module, PolicyEffectCanonicalTargetSet targetSet)
	{
		return PolicyEffectBundleContract.HasTargetsForModule(module, targetSet);
	}

	private static bool TryCreatePolicyEffectTargetShellPreparedInstance(
		PolicyEffectCompiledWireEffect compiled,
		PolicyEffectCanonicalTargetSet targetSet,
		out PolicyEffectPreparedInstance shellPrepared,
		out string error)
	{
		shellPrepared = null;
		error = string.Empty;
		PolicyEffectPreparedInstance sourcePrepared = compiled?.PreparedInstance;
		PolicyEffectInstance sourceInstance = sourcePrepared?.Instance;
		if (compiled?.Module == null
			|| sourceInstance?.Payload == null
			|| sourcePrepared.Descriptor == null
			|| string.IsNullOrWhiteSpace(sourceInstance.InstanceId))
		{
			error = "policy effect 目标壳缺少可复制的 prepared instance";
			return false;
		}
		PolicyEffectCanonicalTargetSet normalizedTargetSet = NormalizePolicyEffectCanonicalTargetSet(targetSet);
		if (!HasPolicyEffectCanonicalTargetsForModule(compiled.Module, normalizedTargetSet))
		{
			error = "模块 " + compiled.Module.Id + " 的目标壳没有可执行的规范目标";
			return false;
		}

		PolicyEffectPayload payload;
		try
		{
			payload = JToken.FromObject(sourceInstance.Payload).ToObject(compiled.Module.PayloadType) as PolicyEffectPayload;
		}
		catch (Exception ex)
		{
			error = "模块 " + compiled.Module.Id + " 的目标壳 payload 复制失败: " + ex.Message;
			return false;
		}
		if (payload == null)
		{
			error = "模块 " + compiled.Module.Id + " 的目标壳 payload 复制结果为空";
			return false;
		}

		shellPrepared = new PolicyEffectPreparedInstance
		{
			Descriptor = sourcePrepared.Descriptor,
			IdempotencyKey = sourcePrepared.IdempotencyKey ?? string.Empty,
			Instance = new PolicyEffectInstance
			{
				MechanismContractVersion = sourceInstance.MechanismContractVersion,
				MechanismContractHash = sourceInstance.MechanismContractHash ?? string.Empty,
				ExpectedMechanismLegIds = new List<string>(sourceInstance.ExpectedMechanismLegIds ?? new List<string>()),
				EffectPlanVersion = sourceInstance.EffectPlanVersion,
				MechanismId = sourceInstance.MechanismId ?? string.Empty,
				MechanismKind = sourceInstance.MechanismKind,
				MechanismRole = sourceInstance.MechanismRole,
				SourceOmitted = sourceInstance.SourceOmitted,
				DestinationOmitted = sourceInstance.DestinationOmitted,
				InstanceId = sourceInstance.InstanceId.Trim(),
				PolicyId = sourceInstance.PolicyId ?? string.Empty,
				ActorHeroId = sourceInstance.ActorHeroId ?? string.Empty,
				ModuleId = compiled.Module.Id,
				SourceModuleId = FirstNonEmpty(sourceInstance.SourceModuleId, compiled.SourceModuleId, compiled.Module.Id),
				TargetSet = normalizedTargetSet,
				Payload = payload,
				LifecycleState = sourceInstance.LifecycleState,
				StartDay = sourceInstance.StartDay,
				EndDay = sourceInstance.EndDay,
				SourceScope = sourceInstance.SourceScope ?? string.Empty,
				Reason = sourceInstance.Reason ?? string.Empty
			}
		};
		return true;
	}

	private static PolicyEffectExecutionReceipt ClonePolicyEffectExecutionReceipt(PolicyEffectExecutionReceipt receipt)
	{
		return PolicyEffectBundleContract.CloneReceipt(receipt);
	}

	private static List<PolicyEffectExecutionReceipt> ClonePolicyEffectExecutionReceipts(
		IEnumerable<PolicyEffectExecutionReceipt> receipts)
	{
		return (receipts ?? Enumerable.Empty<PolicyEffectExecutionReceipt>())
			.Where(receipt => receipt != null)
			.Select(ClonePolicyEffectExecutionReceipt)
			.ToList();
	}

	private static bool TryCoalescePolicyEffectShellInstances(
		IEnumerable<PolicyEffectInstanceSaveData> shellInstances,
		out List<PolicyEffectInstanceSaveData> instances,
		out string error)
	{
		return PolicyEffectBundleContract.TryCoalesceShellInstances(shellInstances, out instances, out error);
	}

	private static PolicyEffectCanonicalTargetSet MergePolicyEffectCanonicalTargetSets(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		return PolicyEffectBundleContract.MergeTargetSets(left, right);
	}

	private static bool PolicyEffectTokensEqual(JToken left, JToken right)
	{
		return PolicyEffectBundleContract.TokensEqual(left, right);
	}

	private static bool AreSamePolicyEffectCanonicalTargetSets(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		return PolicyEffectBundleContract.AreSameTargetSets(left, right);
	}

	private static void SynchronizeOwnedPolicyEffectProgress(
		IEnumerable<PolicyEffectInstanceSaveData> recordedInstances,
		IEnumerable<PolicyEffectInstanceSaveData> activeInstances,
		IEnumerable<PolicyEffectExecutionReceipt> activeReceipts,
		out List<PolicyEffectInstanceSaveData> synchronizedInstances,
		out List<PolicyEffectExecutionReceipt> synchronizedReceipts)
	{
		List<PolicyEffectInstanceSaveData> recorded = (recordedInstances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null && !string.IsNullOrWhiteSpace(instance.InstanceId))
			.ToList();
		List<PolicyEffectInstanceSaveData> active = (activeInstances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null && !string.IsNullOrWhiteSpace(instance.InstanceId))
			.ToList();
		if (recorded.Count <= 0)
		{
			synchronizedInstances = ClonePolicyEffectSaveDataList(active);
			synchronizedReceipts = ClonePolicyEffectExecutionReceipts(activeReceipts);
			return;
		}

		Dictionary<string, PolicyEffectInstanceSaveData> activeById = active
			.GroupBy(instance => instance.InstanceId.Trim(), StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
		synchronizedInstances = new List<PolicyEffectInstanceSaveData>(recorded.Count);
		Dictionary<string, PolicyEffectCanonicalTargetSet> shellTargetSetById
			= new Dictionary<string, PolicyEffectCanonicalTargetSet>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData shell in recorded)
		{
			string instanceId = shell.InstanceId.Trim();
			if (!activeById.TryGetValue(instanceId, out PolicyEffectInstanceSaveData activeInstance))
			{
				continue;
			}
			PolicyEffectCanonicalTargetSet shellTargetSet = NormalizePolicyEffectCanonicalTargetSet(shell.TargetSet);
			PolicyEffectInstanceSaveData synchronized = ClonePolicyEffectSaveData(activeInstance);
			synchronized.TargetSet = shellTargetSet;
			if (synchronized.ExecutionReceipt != null)
			{
				synchronized.ExecutionReceipt.TargetSet = NormalizePolicyEffectCanonicalTargetSet(shellTargetSet);
			}
			synchronizedInstances.Add(synchronized);
			if (!shellTargetSetById.ContainsKey(instanceId))
			{
				shellTargetSetById.Add(instanceId, shellTargetSet);
			}
		}

		synchronizedReceipts = new List<PolicyEffectExecutionReceipt>();
		foreach (PolicyEffectExecutionReceipt activeReceipt in activeReceipts
			?? Enumerable.Empty<PolicyEffectExecutionReceipt>())
		{
			string instanceId = (activeReceipt?.InstanceId ?? string.Empty).Trim();
			if (activeReceipt == null || !shellTargetSetById.TryGetValue(instanceId, out PolicyEffectCanonicalTargetSet shellTargetSet))
			{
				continue;
			}
			PolicyEffectExecutionReceipt synchronizedReceipt = ClonePolicyEffectExecutionReceipt(activeReceipt);
			synchronizedReceipt.TargetSet = NormalizePolicyEffectCanonicalTargetSet(shellTargetSet);
			synchronizedReceipts.Add(synchronizedReceipt);
		}
	}

	private static PolicyEffectInstanceSaveData ClonePolicyEffectSaveData(PolicyEffectInstanceSaveData instance)
	{
		return PolicyEffectBundleContract.CloneInstance(instance);
	}

	private static List<PolicyEffectInstanceSaveData> ClonePolicyEffectSaveDataList(
		IEnumerable<PolicyEffectInstanceSaveData> instances)
	{
		return (instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.Select(ClonePolicyEffectSaveData)
			.ToList();
	}

	private static List<PolicyEffectInstanceSaveData> CreatePolicyEffectSaveDataList(PolicyEffectPreparedInstance prepared)
	{
		PolicyEffectInstance instance = prepared?.Instance;
		if (instance?.Payload == null || prepared.Descriptor == null)
		{
			return new List<PolicyEffectInstanceSaveData>();
		}
		return new List<PolicyEffectInstanceSaveData>
		{
			new PolicyEffectInstanceSaveData
			{
				MechanismContractVersion = instance.MechanismContractVersion,
				MechanismContractHash = instance.MechanismContractHash ?? string.Empty,
				ExpectedMechanismLegIds = new List<string>(instance.ExpectedMechanismLegIds ?? new List<string>()),
				EffectPlanVersion = instance.EffectPlanVersion,
				MechanismId = instance.MechanismId ?? string.Empty,
				MechanismKind = instance.MechanismKind,
				MechanismRole = instance.MechanismRole,
				SourceOmitted = instance.SourceOmitted,
				DestinationOmitted = instance.DestinationOmitted,
				InstanceId = instance.InstanceId ?? string.Empty,
				PolicyId = instance.PolicyId ?? string.Empty,
				ActorHeroId = instance.ActorHeroId ?? string.Empty,
				ModuleId = instance.ModuleId ?? string.Empty,
				SourceModuleId = instance.SourceModuleId ?? string.Empty,
				PayloadSchemaVersion = prepared.Descriptor.PayloadSchemaVersion,
				Payload = JToken.FromObject(instance.Payload),
				TargetSet = NormalizePolicyEffectCanonicalTargetSet(instance.TargetSet),
				LifecycleState = PolicyEffectLifecycleState.Prepared,
				StateSchemaVersion = prepared.Descriptor.RuntimeStateSchemaVersion,
				StartDay = instance.StartDay,
				EndDay = instance.EndDay,
				SourceScope = instance.SourceScope ?? string.Empty,
				Reason = instance.Reason ?? string.Empty
			}
		};
	}

	private static bool EnsureSparsePolicyLifecycleAnchor(
		PolicyDraftRequest request,
		Dictionary<string, PolicyTargetHandleSaveData> handleByKey,
		int durationDays,
		List<PolicyEffectDto> effects,
		out string error)
	{
		error = "";
		string anchorKey = IsLocalPolicyRequest(request) ? "S" : "K0";
		bool requiresAnchor = effects.Count <= 0
			|| (IsLocalPolicyRequest(request) && !effects.Any(x => string.Equals(x.TargetHandle, anchorKey, StringComparison.OrdinalIgnoreCase)));
		if (!requiresAnchor)
		{
			return true;
		}
		if (!handleByKey.TryGetValue(anchorKey, out PolicyTargetHandleSaveData anchor))
		{
            error = "缺少政策计时所需目标句柄：" + anchorKey;
			return false;
		}
		effects.Insert(0, CreateEmptyCompiledPolicyEffect(anchor, anchorKey, durationDays, ""));
		return true;
	}

	private static PolicyEffectDto CreateEmptyCompiledPolicyEffect(PolicyTargetHandleSaveData target, string targetKey, int durationDays, string reason)
	{
		bool isSource = string.Equals(target?.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase);
		return new PolicyEffectDto
		{
			TargetHandle = (targetKey ?? "").Trim(),
			TargetScope = isSource ? LocalPolicyTargetScopeSource : LocalPolicyTargetScopeMentioned,
			TargetKingdomId = target?.KingdomId ?? "",
			TargetKingdomName = target?.KingdomName ?? "",
			DurationDays = durationDays,
			Reason = LimitDisplayChars(CompactPolicyContextText(reason ?? ""), 60)
		};
	}

	private static PolicyEffectDto CloneCompiledPolicyEffect(PolicyEffectDto effect, PolicyTargetHandleSaveData target, int durationDays)
	{
		PolicyEffectDto copy = CreateEmptyCompiledPolicyEffect(target, effect?.TargetHandle, durationDays, effect?.Reason);
		copy.EffectPlanVersion = effect?.EffectPlanVersion ?? 0;
		copy.MechanismId = effect?.MechanismId;
		copy.MechanismKind = effect?.MechanismKind ?? PolicyEffectMechanismKind.Independent;
		copy.MechanismRole = effect?.MechanismRole ?? PolicyEffectMechanismRole.Subject;
		copy.SourceOmitted = effect?.SourceOmitted == true;
		copy.DestinationOmitted = effect?.DestinationOmitted == true;
		copy.ModuleId = effect?.ModuleId;
		copy.SourceModuleId = effect?.SourceModuleId;
		copy.TargetHandles = effect?.TargetHandles == null ? null : new List<string>(effect.TargetHandles);
		copy.Payload = effect?.Payload?.DeepClone();
		copy.PreparedModuleEffect = effect?.PreparedModuleEffect;
		return copy;
	}

	private static bool HasLegacyPolicyEffectShape(PolicyEffectDto effect)
	{
		if (effect == null)
		{
			return false;
		}
		return effect.LegacyFields != null && effect.LegacyFields.Count > 0;
	}

	private static bool TryReadLegacyPolicyEffectValues(
		PolicyEffectDto effect,
		out IReadOnlyDictionary<string, float> values,
		out string error)
	{
		JObject legacyObject = new JObject();
		foreach (KeyValuePair<string, JToken> field in effect?.LegacyFields
			?? new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase))
		{
			if (!LegacyPolicyEffectFieldAdapter.TryResolveModuleId(field.Key, out _))
			{
				values = new Dictionary<string, float>();
				error = "旧版 effects 包含未知字段: " + field.Key;
				return false;
			}
			legacyObject[field.Key] = field.Value?.DeepClone();
		}
		return LegacyPolicyEffectFieldAdapter.TryReadLegacyFields(legacyObject, out values, out error);
	}

	private static bool TryResolvePolicyEffectModule(PolicyDraftRequest request, string metric, out IPolicyEffectModule module)
	{
		module = null;
		string moduleId = (metric ?? "").Trim();
		if (!PolicyEffectModuleCatalog.TryGet(moduleId, out module)
			|| !PolicyEffectModuleCatalog.IsAllowedForScope(module, request?.ScopeKind ?? PolicyScopeKingdom)
			|| request?.CandidateEffectModuleIds == null
			|| !request.CandidateEffectModuleIds.Contains(module.Id, StringComparer.Ordinal))
		{
			module = null;
			return false;
		}
		return true;
	}

	private static List<PolicyEffectDto> NormalizeMainAssessmentEffects(PolicyDraftRequest request, List<PolicyEffectDto> effects)
	{
		List<PolicyEffectDto> result = new List<PolicyEffectDto>();
		if (effects == null)
		{
			return result;
		}
		foreach (PolicyEffectDto effect in effects.Where(x => x != null))
		{
			if (IsLocalPolicyRequest(request))
			{
				string normalizedScope = NormalizeLocalPolicyTargetScope(effect.TargetScope);
				effect.TargetScope = string.IsNullOrWhiteSpace(normalizedScope)
					? (result.Count == 0 ? LocalPolicyTargetScopeSource : LocalPolicyTargetScopeMentioned)
					: normalizedScope;
			}
			if (IsLocalPolicyRequest(request))
			{
				effect.TargetKingdomId = request?.PlayerKingdomId ?? "";
			}
			if (IsLocalPolicyRequest(request))
			{
				effect.TargetKingdomName = request?.PlayerKingdomName ?? "";
			}
			effect.TargetKingdomId = (effect.TargetKingdomId ?? "").Trim();
			effect.TargetKingdomName = CleanPolicyDisplayText(effect.TargetKingdomName ?? "");
			effect.Reason = LimitDisplayChars(CompactPolicyContextText(effect.Reason ?? ""), 60);
			if (!IsLocalPolicyRequest(request) && request?.ManualDurationDays > 0)
			{
				effect.DurationDays = request.ManualDurationDays;
			}
			result.Add(effect);
		}
		return result;
	}
}
