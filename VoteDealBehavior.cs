using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using Bannerlord.UIExtenderEx;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	internal sealed class ModuleInitializerAttribute : Attribute { }
}

namespace AnimusForge
{
	public partial class VoteDealBehavior : CampaignBehaviorBase
	{
		public static VoteDealBehavior Instance { get; private set; }

		private sealed class VoteDealRecord
		{
			public string DealId;
			public string NpcHeroStringId;
			public string NpcClanStringId;
			public string TargetDecisionKey;
			public string TargetDecisionBasicKey;
			public string TargetDecisionTitle;
			public string TargetOptionKey;
			public string TargetOptionBasicKey;
			public string TargetOptionTitle;
			public string TargetOptionSponsorClanId;
			public int SupportWeightValue;
			public float CreatedDay;
			public string Notes;
			public bool IsConsumed;               // true after first vote cast
		}

		private sealed class VoteDealAgendaEntry
		{
			public string Code;
			public KingdomDecision Decision;
			public string DecisionKey;
			public string DecisionBasicKey;
			public string TypeLabel;
			public string Title;
			public string ProposerName;
			public float RemainingDays;
			public List<VoteDealOptionEntry> Options = new List<VoteDealOptionEntry>();
		}

		private sealed class VoteDealOptionEntry
		{
			public string Code;
			public DecisionOutcome Outcome;
			public string OutcomeKey;
			public string OutcomeBasicKey;
			public string Title;
			public string Description;
			public string SponsorName;
			public string SponsorClanId;
		}

		private enum BilateralDiplomacyKind
		{
			None = 0,
			Peace = 1,
			Alliance = 2,
			Trade = 3
		}

		private sealed class BilateralDiplomacyRecord
		{
			public string RecordId;
			public string Kind;
			public string FirstKingdomId;
			public string SecondKingdomId;
			public int TributeFromFirstToSecond;
			public int TributeDurationDays;
			public float CreatedDay;
			public bool CounterpartCreated;
			public string FirstDecisionBasicKey;
			public string ParticipantHeroIds;
		}

		private sealed class BilateralDiplomacyMemoryMarker
		{
		}

		private List<VoteDealRecord> _activeDeals = new List<VoteDealRecord>();
		private Dictionary<string, string> _serializedDeals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private List<BilateralDiplomacyRecord> _bilateralDiplomacyRecords = new List<BilateralDiplomacyRecord>();
		private Dictionary<string, string> _serializedBilateralDiplomacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, string> _dialogueProposalDecisionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, string> _shownRequiredAgendaVoteKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private const string DealIdPrefix = "VD";
		private const string BilateralDiplomacyIdPrefix = "BD";
		private const char RecordDelimiter = '\x01';

		private static bool s_globalPatchesApplied;
		private static bool s_globalPatchesInProgress;
		// Both supported Bannerlord API lines keep the inversion flag private. Cache the
		// lookup once so agenda/prompt rendering does not need to infer a policy vote
		// from its ambiguous vanilla title (which is only the policy name).
		private static readonly FieldInfo KingdomPolicyDecisionInvertedField = AccessTools.Field(typeof(KingdomPolicyDecision), "_isInvertedDecision");
		private static readonly ConditionalWeakTable<KingdomDecision, BilateralDiplomacyMemoryMarker> BilateralDiplomacyMemoryMarkers = new ConditionalWeakTable<KingdomDecision, BilateralDiplomacyMemoryMarker>();
		internal static KingdomDecision s_pendingRequiredAgendaDecision;
		private bool _agendaAutoVoteTickRunning;
		private readonly HashSet<KingdomDecision> _agendaAutoVoteInProgress = new HashSet<KingdomDecision>();
		private readonly HashSet<KingdomDecision> _agendaDeadlineReminderShown = new HashSet<KingdomDecision>();

		// ── Module Initializer ─────────────────────────────────────────────

		[ModuleInitializer]
		internal static void ModuleInit()
		{
			ApplyGlobalPatchesOnce();
		}


		private static void ApplyGlobalPatchesOnce()
		{
			if (s_globalPatchesApplied) return;
			if (s_globalPatchesInProgress) return;
			s_globalPatchesInProgress = true;
			try
			{
				Harmony harmony = new Harmony("com.AnimusForge.votedeal");
				harmony.Patch(
					typeof(MyBehavior).GetMethod("BuildShoutPromptContextForExternal",
						BindingFlags.Public | BindingFlags.Static),
					postfix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_BuildContext_Postfix)));

				// DetermineSupport is abstract → cannot patch base directly.
				// Enumerate all concrete subclasses and patch each override, like BellumCivile does.
				int patchedCount = 0;
				int errorCount = 0;
				foreach (Type subType in typeof(KingdomDecision).Assembly.GetTypes())
				{
					if (!subType.IsClass || subType.IsAbstract) continue;
					if (!typeof(KingdomDecision).IsAssignableFrom(subType)) continue;

					MethodInfo subMethod = subType.GetMethod("DetermineSupport",
						BindingFlags.Public | BindingFlags.Instance);
					if (subMethod == null || subMethod.IsAbstract) continue;

					try
					{
						harmony.Patch(subMethod,
							prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_DetermineSupport_Prefix)));
						patchedCount++;
					}
					catch (Exception ex)
					{
						errorCount++;
						Logger.Log("VoteDeal", $"[Harmony] Failed to patch {subType.Name}.DetermineSupport: {ex.Message}");
					}
				}
				Logger.Log("VoteDeal", $"[Harmony] DetermineSupport: patched {patchedCount} concrete subclass(es), {errorCount} error(s).");

				// DetermineSupport only returns a preference score. Vanilla can still
				// downgrade it to neutral when the clan lacks influence, so fulfillment
				// must also hook the method that selects the actual vote.
				MethodInfo determineSupportOptionMethod = AccessTools.Method(
					typeof(KingdomDecision),
					nameof(KingdomDecision.DetermineSupportOption));
				if (determineSupportOptionMethod != null)
				{
					harmony.Patch(
						determineSupportOptionMethod,
						prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_DetermineSupportOption_Prefix)));
					Logger.Log("VoteDeal", "[Harmony] KingdomDecision.DetermineSupportOption vote-deal fulfillment hook applied.");
				}
				else
				{
					Logger.Log("VoteDeal", "[Harmony] WARNING: KingdomDecision.DetermineSupportOption was not found; vote deals can only affect preference scores.");
				}

				// ── Agenda delay: 21 days for player kingdom, 3 days for other kingdoms ──
				try
				{
					MethodInfo addDecisionMethod = typeof(Kingdom).GetMethod("AddDecision");
					if (addDecisionMethod != null)
					{
						harmony.Patch(
							addDecisionMethod,
							prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_AddDecision_AgendaDelay_Prefix)));
					}
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] AddDecision prefix patch failed: {ex.Message}");
				}
				harmony.Patch(
					typeof(Kingdom).GetMethod("AddDecision"),
					postfix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_AddDecision_Delay_Postfix)));
				Logger.Log("VoteDeal", "[Harmony] Kingdom.AddDecision delay hook applied (player=21d, others=3d).");

				try
				{
					MethodInfo updateKingdomDecisionsMethod = AccessTools.Method(typeof(KingdomDecisionProposalBehavior), "UpdateKingdomDecisions", new[] { typeof(Kingdom) });
					if (updateKingdomDecisionsMethod != null)
					{
						harmony.Patch(
							updateKingdomDecisionsMethod,
							prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_UpdateKingdomDecisions_Delay_Prefix)));
						Logger.Log("VoteDeal", "[Harmony] KingdomDecisionProposalBehavior.UpdateKingdomDecisions delay hook applied.");
					}
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[AgendaDelay] UpdateKingdomDecisions patch failed: {ex.Message}");
				}

				PatchBilateralDiplomacyDecision(harmony, typeof(MakePeaceKingdomDecision));
				PatchBilateralDiplomacyDecision(harmony, typeof(StartAllianceDecision));
				PatchBilateralDiplomacyDecision(harmony, typeof(TradeAgreementDecision));

				try
				{
					MethodInfo shouldBeCancelledMethod = AccessTools.Method(typeof(KingdomDecision), nameof(KingdomDecision.ShouldBeCancelled));
					if (shouldBeCancelledMethod != null)
					{
						harmony.Patch(
							shouldBeCancelledMethod,
							prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_BilateralDiplomacy_ShouldBeCancelled_Prefix)));
						Logger.Log("VoteDeal", "[Harmony] Bilateral diplomacy cancellation guard applied.");
					}
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] ShouldBeCancelled patch failed: {ex.Message}");
				}

				MethodInfo needsPlayerResolutionGetter = AccessTools.PropertyGetter(typeof(KingdomDecision), nameof(KingdomDecision.NeedsPlayerResolution));
				if (needsPlayerResolutionGetter != null)
				{
					harmony.Patch(
						needsPlayerResolutionGetter,
						postfix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecision_NeedsPlayerResolution_Postfix)));
					Logger.Log("VoteDeal", "[Harmony] KingdomDecision.NeedsPlayerResolution agenda auto-resolve patch applied.");
				}
				else
				{
					Logger.Log("VoteDeal", "[Harmony] KingdomDecision.NeedsPlayerResolution getter not found.");
				}

				try
				{
					MethodInfo isSingleClanDecisionMethod = AccessTools.Method(
						typeof(KingdomDecision),
						nameof(KingdomDecision.IsSingleClanDecision));
					if (isSingleClanDecisionMethod != null)
					{
						harmony.Patch(
							isSingleClanDecisionMethod,
							prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecision_IsSingleClanDecision_Prefix)));
						Logger.Log("VoteDeal", "[Harmony] Interactive single-clan player agenda patch applied.");
					}
					else
					{
						Logger.Log("VoteDeal", "[Harmony] KingdomDecision.IsSingleClanDecision not found.");
					}
				}
				catch (Exception singleClanPatchEx)
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] Interactive single-clan agenda patch failed: {singleClanPatchEx.Message}");
				}

				// ── Block ForceDecideDecision when TriggerTime is still future ──
				Type kingdomMgmtVmType = typeof(KingdomManagementVM);
				if (kingdomMgmtVmType != null)
				{
					harmony.Patch(
						kingdomMgmtVmType.GetMethod("ForceDecideDecision", BindingFlags.NonPublic | BindingFlags.Instance),
						prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_ForceDecideDecision_Block_Prefix)));
					Logger.Log("VoteDeal", "[Harmony] ForceDecideDecision block patch applied.");
				}

				harmony.Patch(
					typeof(CampaignInformationManager).GetMethod(nameof(CampaignInformationManager.NewMapNoticeAdded),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_NewMapNoticeAdded_SuppressKingdomVoteReminder_Prefix)));
				harmony.Patch(
					typeof(KingdomDecisionMapNotification).GetMethod(nameof(KingdomDecisionMapNotification.IsValid),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecisionMapNotification_IsValid_Prefix)));
				harmony.Patch(
					typeof(KingdomDecisionsVM).GetMethod(nameof(KingdomDecisionsVM.HandleDecision),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecisionsVM_HandleDecision_SuppressVoteInquiry_Prefix)));
				harmony.Patch(
					typeof(KingdomDecisionsVM).GetMethod(nameof(KingdomDecisionsVM.OnFrameTick),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecisionsVM_OnFrameTick_MarkAgendaDecisionsExamined_Prefix)));
				MethodInfo kingdomVoteNotificationFinalize = AccessTools.Method(
					typeof(KingdomVoteNotificationItemVM),
					nameof(KingdomVoteNotificationItemVM.OnFinalize));
				if (kingdomVoteNotificationFinalize != null)
				{
					harmony.Patch(
						kingdomVoteNotificationFinalize,
						postfix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomVoteNotificationItemVM_OnFinalize_Postfix)));
				}
				Logger.Log("VoteDeal", "[Harmony] Kingdom vote reminders guarded; policy proposals restored with safe listener cleanup.");

				// ── Kingdom Agenda patches ──
				try
				{
					KingdomAgendaTabSelectionPatch.Apply(harmony);
					Logger.Log("VoteDeal", "[Agenda] TabSelection patches applied via VoteDeal Harmony.");
				}
				catch (Exception agendaEx)
				{
					Logger.Log("VoteDeal", $"[Agenda] TabSelection patches failed: {agendaEx.Message}");
				}


				s_globalPatchesApplied = true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Harmony Error] {ex.Message}");
			}
			finally
			{
				s_globalPatchesInProgress = false;
			}
		}

		// ── Support weight parsing ────────────────────────────────────────

		private static void PatchBilateralDiplomacyDecision(Harmony harmony, Type decisionType)
		{
			try
			{
				if (harmony == null || decisionType == null) return;
				MethodInfo applyMethod = AccessTools.Method(decisionType, nameof(KingdomDecision.ApplyChosenOutcome));
				if (applyMethod == null)
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] ApplyChosenOutcome not found on {decisionType.Name}.");
					return;
				}
				harmony.Patch(
					applyMethod,
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_BilateralDiplomacy_ApplyChosenOutcome_Prefix)));

				MethodInfo onShowMethod = AccessTools.Method(decisionType, nameof(KingdomDecision.OnShowDecision));
				if (onShowMethod != null)
				{
					harmony.Patch(
						onShowMethod,
						prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_BilateralDiplomacy_OnShowDecision_Prefix)));
				}
				else
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] OnShowDecision not found on {decisionType.Name}.");
				}
				Logger.Log("VoteDeal", $"[Harmony] Bilateral diplomacy hooks applied: {decisionType.Name}.");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Failed to patch {decisionType?.Name}: {ex.Message}");
			}
		}

		private static Supporter.SupportWeights ParseSupportWeight(string weightStr)
		{
			switch ((weightStr ?? "").Trim().ToUpperInvariant())
			{
				case "FULLY_PUSH": case "FULLYPUSH": case "FULLY": case "4":
					return Supporter.SupportWeights.FullyPush;
				case "STRONGLY_FAVOR": case "STRONGLYFAVOR": case "STRONGLY": case "3":
					return Supporter.SupportWeights.StronglyFavor;
				default:
					return Supporter.SupportWeights.SlightlyFavor;
			}
		}

		// ── ID generation ──────────────────────────────────────────────────

		private static string GenerateDealId()
		{
			byte[] bytes = new byte[4];
			new Random().NextBytes(bytes);
			return DealIdPrefix + bytes[0].ToString("x2") + bytes[1].ToString("x2")
				+ bytes[2].ToString("x2") + bytes[3].ToString("x2");
		}

		private static string GenerateBilateralDiplomacyId()
		{
			byte[] bytes = new byte[4];
			new Random().NextBytes(bytes);
			return BilateralDiplomacyIdPrefix + bytes[0].ToString("x2") + bytes[1].ToString("x2")
				+ bytes[2].ToString("x2") + bytes[3].ToString("x2");
		}

		// ── CampaignBehavior overrides ─────────────────────────────────────

		public override void RegisterEvents()
		{
			ApplyGlobalPatchesOnce();
			Instance = this;
			Logger.Log("VoteDeal", "[Lifecycle] RegisterEvents called, Instance set");
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
			CampaignEvents.KingdomDecisionCancelled.AddNonSerializedListener(this, OnKingdomDecisionCancelled);
			CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnAgendaKingdomDecisionAdded);
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
			CampaignEvents.TickEvent.AddNonSerializedListener(this, OnAgendaMapNotificationTick);
		}

		public override void SyncData(IDataStore dataStore)
		{
			try
			{
				if (!dataStore.IsSaving)
				{
					lock (KingdomAgendaVM._snapshots)
					{
						KingdomAgendaVM._snapshots.Clear();
					}
					_agendaDeadlineReminderShown.Clear();
					s_pendingRequiredAgendaDecision = null;
					ResetAgendaMapNotificationRuntime();
				}
				if (_activeDeals == null) _activeDeals = new List<VoteDealRecord>();
				if (_serializedDeals == null) _serializedDeals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				if (_bilateralDiplomacyRecords == null) _bilateralDiplomacyRecords = new List<BilateralDiplomacyRecord>();
				if (_serializedBilateralDiplomacy == null) _serializedBilateralDiplomacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				if (_dialogueProposalDecisionKeys == null) _dialogueProposalDecisionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				if (_shownRequiredAgendaVoteKeys == null) _shownRequiredAgendaVoteKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				if (dataStore.IsSaving)
				{
					CleanupRequiredAgendaVotePromptKeys();
					_serializedDeals.Clear();
					for (int i = 0; i < _activeDeals.Count; i++)
					{
						var d = _activeDeals[i];
						_serializedDeals["vd_" + i] = string.Join(RecordDelimiter.ToString(),
							d.DealId ?? "",
							d.NpcHeroStringId ?? "",
							d.NpcClanStringId ?? "",
							d.SupportWeightValue.ToString(),
							d.CreatedDay.ToString("F6"),
							d.Notes ?? "",
							d.IsConsumed ? "1" : "0",
							d.TargetDecisionKey ?? "",
							d.TargetDecisionBasicKey ?? "",
							d.TargetDecisionTitle ?? "",
							d.TargetOptionKey ?? "",
							d.TargetOptionBasicKey ?? "",
							d.TargetOptionTitle ?? "",
							d.TargetOptionSponsorClanId ?? "");
					}

					_serializedBilateralDiplomacy.Clear();
					CleanupBilateralDiplomacyRecords();
					for (int i = 0; i < _bilateralDiplomacyRecords.Count; i++)
					{
						var d = _bilateralDiplomacyRecords[i];
						_serializedBilateralDiplomacy["bd_" + i] = string.Join(RecordDelimiter.ToString(),
							d.RecordId ?? "",
							d.Kind ?? "",
							d.FirstKingdomId ?? "",
							d.SecondKingdomId ?? "",
							d.TributeFromFirstToSecond.ToString(),
							d.TributeDurationDays.ToString(),
							d.CreatedDay.ToString("F6"),
							d.CounterpartCreated ? "1" : "0",
							d.FirstDecisionBasicKey ?? "",
							d.ParticipantHeroIds ?? "");
					}
				}

				Dictionary<string, string> serializedDealsForSync = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_serializedDeals, "_vdSerializedDeals", "VoteDeal") : _serializedDeals;
				dataStore.SyncData("_vdSerializedDeals", ref serializedDealsForSync);

				Dictionary<string, string> serializedBilateralDiplomacyForSync = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_serializedBilateralDiplomacy, "_vdBilateralDiplomacy", "VoteDeal") : _serializedBilateralDiplomacy;
				dataStore.SyncData("_vdBilateralDiplomacy", ref serializedBilateralDiplomacyForSync);

				Dictionary<string, string> dialogueProposalKeysForSync = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_dialogueProposalDecisionKeys, "_vdDialogueProposalKeys", "VoteDeal") : _dialogueProposalDecisionKeys;
				dataStore.SyncData("_vdDialogueProposalKeys", ref dialogueProposalKeysForSync);

				Dictionary<string, string> shownRequiredAgendaVoteKeysForSync = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_shownRequiredAgendaVoteKeys, "_vdShownRequiredAgendaVoteKeys", "VoteDeal") : _shownRequiredAgendaVoteKeys;
				dataStore.SyncData("_vdShownRequiredAgendaVoteKeys", ref shownRequiredAgendaVoteKeysForSync);

				if (dataStore.IsLoading)
				{
					_serializedDeals = CampaignSaveChunkHelper.RestoreStringDictionary(serializedDealsForSync, "VoteDeal");
					_activeDeals.Clear();
					if (_serializedDeals != null && _serializedDeals.Count > 0)
					{
						foreach (string val in _serializedDeals.Values)
						{
							if (string.IsNullOrEmpty(val)) continue;
							string[] parts = val.Split(RecordDelimiter);
							if (parts.Length < 6) continue;
							int offset = IsLegacySerializedDirection(parts.Length > 3 ? parts[3] : null) ? 1 : 0;
							var record = new VoteDealRecord
							{
								DealId = parts[0],
								NpcHeroStringId = parts[1],
								NpcClanStringId = parts[2],
								SupportWeightValue = int.TryParse(parts[3 + offset], out int sw) ? sw : 2,
								CreatedDay = parts.Length > 4 + offset && float.TryParse(parts[4 + offset], out float cd) ? cd : 0f,
								Notes = parts.Length > 5 + offset ? parts[5 + offset] ?? "" : "",
								IsConsumed = parts.Length > 6 + offset && parts[6 + offset] == "1",
								TargetDecisionKey = parts.Length > 7 + offset ? parts[7 + offset] ?? "" : "",
								TargetDecisionBasicKey = parts.Length > 8 + offset ? parts[8 + offset] ?? "" : "",
								TargetDecisionTitle = parts.Length > 9 + offset ? parts[9 + offset] ?? "" : "",
								TargetOptionKey = parts.Length > 10 + offset ? parts[10 + offset] ?? "" : "",
								TargetOptionBasicKey = parts.Length > 11 + offset ? parts[11 + offset] ?? "" : "",
								TargetOptionTitle = parts.Length > 12 + offset ? parts[12 + offset] ?? "" : "",
								TargetOptionSponsorClanId = parts.Length > 13 + offset ? parts[13 + offset] ?? "" : ""
							};
							if (!string.IsNullOrWhiteSpace(record.TargetDecisionKey))
							{
								_activeDeals.Add(record);
							}
						}
					}

					_serializedBilateralDiplomacy = CampaignSaveChunkHelper.RestoreStringDictionary(serializedBilateralDiplomacyForSync, "VoteDeal");
					_bilateralDiplomacyRecords.Clear();
					if (_serializedBilateralDiplomacy != null && _serializedBilateralDiplomacy.Count > 0)
					{
						foreach (string val in _serializedBilateralDiplomacy.Values)
						{
							if (string.IsNullOrEmpty(val)) continue;
							string[] parts = val.Split(RecordDelimiter);
							if (parts.Length < 7) continue;
							var record = new BilateralDiplomacyRecord
							{
								RecordId = parts[0],
								Kind = parts[1],
								FirstKingdomId = parts[2],
								SecondKingdomId = parts[3],
								TributeFromFirstToSecond = int.TryParse(parts[4], out int tribute) ? tribute : 0,
								TributeDurationDays = int.TryParse(parts[5], out int duration) ? duration : 0,
								CreatedDay = float.TryParse(parts[6], out float createdDay) ? createdDay : 0f,
								CounterpartCreated = parts.Length > 7 && parts[7] == "1",
								FirstDecisionBasicKey = parts.Length > 8 ? parts[8] ?? "" : "",
								ParticipantHeroIds = parts.Length > 9 ? parts[9] ?? "" : ""
							};
							if (!string.IsNullOrWhiteSpace(record.Kind)
								&& !string.IsNullOrWhiteSpace(record.FirstKingdomId)
								&& !string.IsNullOrWhiteSpace(record.SecondKingdomId))
							{
								_bilateralDiplomacyRecords.Add(record);
							}
						}
					}
					CleanupBilateralDiplomacyRecords();
					_dialogueProposalDecisionKeys = CampaignSaveChunkHelper.RestoreStringDictionary(dialogueProposalKeysForSync, "VoteDeal")
						?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					_shownRequiredAgendaVoteKeys = CampaignSaveChunkHelper.RestoreStringDictionary(shownRequiredAgendaVoteKeysForSync, "VoteDeal")
						?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					CleanupRequiredAgendaVotePromptKeys();
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[SyncData Error] {ex.Message}");
				_activeDeals = new List<VoteDealRecord>();
				_serializedDeals = new Dictionary<string, string>();
				_bilateralDiplomacyRecords = new List<BilateralDiplomacyRecord>();
				_serializedBilateralDiplomacy = new Dictionary<string, string>();
				_dialogueProposalDecisionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_shownRequiredAgendaVoteKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
		}

		// ── KingdomDecisionConcluded handler ───────────────────────────────

		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
		{
			ForgetAgendaMapNotification(decision);
			try
			{
				UnregisterDialogueProposedDecision(decision);
				if (_activeDeals == null || _activeDeals.Count == 0) return;
				if (decision?.Kingdom == null) return;

				List<VoteDealRecord> processedDeals = new List<VoteDealRecord>();
				foreach (VoteDealRecord deal in _activeDeals.Where(d => !d.IsConsumed))
				{
					Hero npc = Hero.FindFirst(h => h.StringId == deal.NpcHeroStringId);
					if (npc?.Clan?.Kingdom != decision.Kingdom) continue;
					if (!DoesVoteDealMatchDecision(deal, decision)) continue;

					deal.IsConsumed = true;
					processedDeals.Add(deal);

					string completionText = !string.IsNullOrWhiteSpace(deal.TargetOptionTitle)
						? $"投票交易完成：{npc.Name}已按承诺投票选择「{deal.TargetOptionTitle}」。"
						: $"投票交易完成：{npc.Name}已按承诺投票。";
					AnimusForgeQuickInfo.ShowForDuration(
						completionText,
						5000, npc.CharacterObject);
					MyBehavior.RecordVoteDealFulfilledForExternal(npc, decision, chosenOutcome, deal.DealId, deal.TargetDecisionTitle, deal.TargetOptionTitle);
				}

				if (processedDeals.Count > 0)
					Logger.Log("VoteDeal", $"[DecisionConcluded] Processed {processedDeals.Count} deal(s) for kingdom={decision.Kingdom.StringId}.");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[OnKingdomDecisionConcluded Error] {ex.Message}");
			}
		}

		private void OnHourlyTick()
		{
			try
			{
				RemoveDuplicateBilateralDiplomacyAgendas();
				AutoStartExpiredAgendaVotes();
				NotifyPlayerKingdomAgendasNearingDeadline();
				ReconcileAndPublishAgendaMapNotifications();
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Hourly tick error: {ex.Message}");
			}
		}

		private void AutoStartExpiredAgendaVotes()
		{
			if (_agendaAutoVoteTickRunning) return;
			_agendaAutoVoteTickRunning = true;
			try
			{
				CleanupRequiredAgendaVotePromptKeys();
				List<Kingdom> kingdoms = Kingdom.All?.ToList();
				if (kingdoms == null || kingdoms.Count == 0) return;

				foreach (Kingdom kingdom in kingdoms)
				{
					AutoStartExpiredAgendaVotes(kingdom);
				}
			}
			finally
			{
				_agendaAutoVoteTickRunning = false;
			}
		}

		private void AutoStartExpiredAgendaVotes(Kingdom kingdom)
		{
			if (kingdom?.UnresolvedDecisions == null || kingdom.UnresolvedDecisions.Count == 0) return;

			List<KingdomDecision> decisions = kingdom.UnresolvedDecisions.ToList();
			foreach (KingdomDecision decision in decisions)
			{
				if (decision == null) continue;
				if (_agendaAutoVoteInProgress.Contains(decision)) continue;
				if (!kingdom.UnresolvedDecisions.Contains(decision)) continue;
				if (TryNormalizeRedundantSingleClanFiefAgenda(kingdom, decision, "hourly-cleanup"))
				{
					CancelExpiredAgendaDecision(kingdom, decision);
					continue;
				}

				bool shouldCancel;
				try
				{
					shouldCancel = decision.ShouldBeCancelled();
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[AgendaAutoVote] ShouldBeCancelled failed for {GetSafeDecisionTitle(decision)}: {ex.Message}");
					continue;
				}

				if (shouldCancel)
				{
					CancelExpiredAgendaDecision(kingdom, decision);
					continue;
				}

				if (RequiresPlayerRulerResolution(decision))
				{
					TryLaunchRequiredAgendaVote(decision);
					continue;
				}

				if (!ShouldAutoStartAgendaVote(decision)) continue;
				StartExpiredAgendaElection(kingdom, decision);
			}
		}

		private static bool ShouldAutoStartAgendaVote(KingdomDecision decision)
		{
			try
			{
				if (decision == null) return false;
				if (decision.Kingdom == null) return false;
				if (decision.IsEnforced) return false;
				if (decision.TriggerTime.IsFuture) return false;
				if (RequiresPlayerRulerResolution(decision)) return false;
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Match error: {ex.Message}");
				return false;
			}
		}

		private void StartExpiredAgendaElection(Kingdom kingdom, KingdomDecision decision)
		{
			if (kingdom == null || decision == null) return;
			if (_agendaAutoVoteInProgress.Contains(decision)) return;

			_agendaAutoVoteInProgress.Add(decision);
			try
			{
				if (!kingdom.UnresolvedDecisions.Contains(decision)) return;
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Starting expired agenda vote: kingdom={kingdom.StringId}, decision={GetSafeDecisionTitle(decision)}");
				new KingdomElection(decision).StartElectionWithoutPlayer();
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Start election failed for {GetSafeDecisionTitle(decision)}: {ex.Message}");
			}
			finally
			{
				_agendaAutoVoteInProgress.Remove(decision);
			}
		}

		private void CancelExpiredAgendaDecision(Kingdom kingdom, KingdomDecision decision)
		{
			if (kingdom == null || decision == null) return;
			try
			{
				if (!kingdom.UnresolvedDecisions.Contains(decision)) return;
				kingdom.RemoveDecision(decision);
				bool isPlayerInvolved = IsPlayerInvolvedInDecision(decision);
				CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(decision, isPlayerInvolved);
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Removed cancelled agenda: kingdom={kingdom.StringId}, decision={GetSafeDecisionTitle(decision)}");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Cancel failed for {GetSafeDecisionTitle(decision)}: {ex.Message}");
			}
		}

		private static bool IsPlayerInvolvedInDecision(KingdomDecision decision)
		{
			try
			{
				Clan chooser = decision?.DetermineChooser();
				if (chooser?.Leader?.IsHumanPlayerCharacter == true) return true;
				IEnumerable<Supporter> supporters = decision?.DetermineSupporters();
				return supporters?.Any(s => s != null && s.IsPlayer) == true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoVote] Player involvement check failed: {ex.Message}");
				return decision?.IsPlayerParticipant == true;
			}
		}

		private static string GetSafeDecisionTitle(KingdomDecision decision)
		{
			return GetDecisionDisplayTitleForAgenda(decision);
		}

		internal static string GetPolicyDirectionForAgenda(KingdomDecision decision)
		{
			KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
			if (policyDecision == null)
			{
				return "";
			}
			return IsPolicyDecisionInvertedForAgenda(policyDecision) ? "ABOLISH" : "ADOPT";
		}

		// Dynamic LLM policies can be reconstructed while an older instance is still
		// referenced by an active policy list or a pending decision. PolicyObject does
		// not provide stable value equality, so agenda state must be compared by its
		// persisted StringId rather than object identity.
		internal static bool IsSamePolicyForAgenda(PolicyObject left, PolicyObject right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}
			string leftId = left?.StringId;
			string rightId = right?.StringId;
			return !string.IsNullOrWhiteSpace(leftId)
				&& string.Equals(leftId, rightId, StringComparison.OrdinalIgnoreCase);
		}

		internal static PolicyObject ResolvePolicyForKingdomAgenda(Kingdom kingdom, PolicyObject policy, out bool isActive)
		{
			isActive = false;
			if (policy == null)
			{
				return null;
			}
			try
			{
				foreach (PolicyObject activePolicy in kingdom?.ActivePolicies ?? Enumerable.Empty<PolicyObject>())
				{
					if (!IsSamePolicyForAgenda(activePolicy, policy))
					{
						continue;
					}
					isActive = true;
					return activePolicy ?? policy;
				}
			}
			catch
			{
			}
			return policy;
		}

		internal static string GetDecisionDisplayTitleForAgenda(KingdomDecision decision)
		{
			if (decision == null)
			{
				return "<null>";
			}

			try
			{
				string bilateralTitle = GetBilateralDiplomacyAgendaTitle(decision);
				if (!string.IsNullOrWhiteSpace(bilateralTitle))
				{
					return CleanVoteDealText(bilateralTitle);
				}

				KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
				if (policyDecision != null)
				{
					string policyName = CleanVoteDealText(policyDecision.Policy?.Name?.ToString() ?? decision.GetGeneralTitle()?.ToString() ?? "未命名政策");
					if (string.IsNullOrWhiteSpace(policyName))
					{
						policyName = "未命名政策";
					}
					return (IsPolicyDecisionInvertedForAgenda(policyDecision) ? "废除政策：" : "推行政策：") + policyName;
				}

				string title = CleanVoteDealText(decision.GetGeneralTitle()?.ToString() ?? "");
				return string.IsNullOrWhiteSpace(title) ? "未命名提案" : title;
			}
			catch
			{
				return decision.GetType().Name ?? "<null>";
			}
		}

		internal static string GetDecisionOutcomeDisplayTitleForAgenda(KingdomDecision decision, DecisionOutcome outcome)
		{
			KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
			KingdomPolicyDecision.PolicyDecisionOutcome policyOutcome = outcome as KingdomPolicyDecision.PolicyDecisionOutcome;
			if (policyDecision != null && policyOutcome != null)
			{
				bool isAbolition = IsPolicyDecisionInvertedForAgenda(policyDecision);
				if (policyOutcome.ShouldDecisionBeEnforced)
				{
					return isAbolition ? "赞成废除（是）" : "赞成推行（是）";
				}
				return isAbolition ? "反对废除、保留政策（否）" : "反对推行、维持未生效（否）";
			}

			try
			{
				string title = CleanVoteDealText(outcome?.GetDecisionTitle()?.ToString() ?? "");
				return string.IsNullOrWhiteSpace(title) ? "未知选项" : title;
			}
			catch
			{
				return "未知选项";
			}
		}

		internal static string GetDecisionOutcomeDisplayDescriptionForAgenda(KingdomDecision decision, DecisionOutcome outcome)
		{
			KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
			KingdomPolicyDecision.PolicyDecisionOutcome policyOutcome = outcome as KingdomPolicyDecision.PolicyDecisionOutcome;
			if (policyDecision != null && policyOutcome != null)
			{
				bool isAbolition = IsPolicyDecisionInvertedForAgenda(policyDecision);
				if (policyOutcome.ShouldDecisionBeEnforced)
				{
					return isAbolition ? "投此项将废除该政策。" : "投此项将推行该政策。";
				}
				return isAbolition ? "投此项将保留该政策。" : "投此项将不推行该政策。";
			}

			try
			{
				return CleanVoteDealText(outcome?.GetDecisionDescription()?.ToString() ?? "");
			}
			catch
			{
				return "";
			}
		}

		private static bool IsPolicyDecisionInvertedForAgenda(KingdomPolicyDecision decision)
		{
			try
			{
				if (KingdomPolicyDecisionInvertedField?.GetValue(decision) is bool isInverted)
				{
					return isInverted;
				}
			}
			catch
			{
			}

			// The fallback preserves correct behavior if a future game build renames the
			// private field: a valid disavowal decision has an active policy, while a
			// valid adoption decision does not.
			try
			{
				ResolvePolicyForKingdomAgenda(decision?.Kingdom, decision?.Policy, out bool isActive);
				return isActive;
			}
			catch
			{
				return false;
			}
		}

		private static bool Patch_AddDecision_AgendaDelay_Prefix(Kingdom __instance, KingdomDecision kingdomDecision, bool ignoreInfluenceCost)
		{
			try
			{
				if (__instance == null || kingdomDecision == null) return true;
				if (TryNormalizeRedundantSingleClanFiefAgenda(__instance, kingdomDecision, "add-decision")) return false;

				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (behavior == null) return true;

				if (behavior.IsDuplicateWhileBilateralDiplomacyPending(kingdomDecision))
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] Duplicate pending diplomacy agenda blocked: kingdom={__instance.StringId}, decision={GetSafeDecisionTitle(kingdomDecision)}");
					return false;
				}

				if (__instance == Clan.PlayerClan?.Kingdom) return true;
				if (kingdomDecision.Kingdom != null && kingdomDecision.Kingdom != __instance) return true;
				if (kingdomDecision.IsEnforced) return true;
				if (kingdomDecision.ShouldBeCancelled()) return true;

				bool isBilateralCounterpart = behavior.FindCounterpartRecordForDecision(kingdomDecision) != null;
				if (isBilateralCounterpart && !IsBilateralCounterpartDecisionHardValid(kingdomDecision, out string hardFailureReason))
				{
					Logger.Log("VoteDeal", "[BilateralDiplomacy] Counterpart AddDecision skipped because hard validity failed: " + hardFailureReason);
					return false;
				}

				MBList<KingdomDecision> unresolved = Traverse.Create(__instance)
					.Field("_unresolvedDecisions")
					.GetValue<MBList<KingdomDecision>>();
				if (unresolved == null) return true;

				float delayDays = 3f;
				SetDecisionTriggerTime(kingdomDecision, delayDays);

				if (!ignoreInfluenceCost && kingdomDecision.ProposerClan != null)
				{
					int influenceCost = kingdomDecision.GetInfluenceCost(kingdomDecision.ProposerClan);
					ChangeClanInfluenceAction.Apply(kingdomDecision.ProposerClan, -(float)influenceCost);
				}

				try
				{
					CampaignEventDispatcher.Instance.OnKingdomDecisionAdded(kingdomDecision, IsPlayerInvolvedInDecision(kingdomDecision));
				}
				catch (Exception eventEx)
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] OnKingdomDecisionAdded failed: {eventEx.Message}");
				}

				if (!unresolved.Contains(kingdomDecision))
				{
					unresolved.Add(kingdomDecision);
				}
				RegisterAgendaDelaySnapshot(kingdomDecision, __instance, delayDays);
				Logger.Log("VoteDeal", $"[AgendaDelay] Queued foreign agenda in {__instance.StringId} instead of immediate AI election: {GetSafeDecisionTitle(kingdomDecision)}");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] AddDecision prefix error: {ex.Message}");
				return true;
			}
		}

		private static bool Patch_UpdateKingdomDecisions_Delay_Prefix(Kingdom kingdom)
		{
			try
			{
				if (kingdom == null)
				{
					Logger.Log("VoteDeal", "[AgendaDelay] Skipped UpdateKingdomDecisions for null kingdom.");
					return false;
				}
				if (IsKingdomEliminatedSafe(kingdom))
				{
					int removed = ClearUnresolvedDecisionsSafe(kingdom);
					Logger.Log("VoteDeal", $"[AgendaDelay] Skipped eliminated kingdom={kingdom.StringId ?? ""} removedDecisions={removed}");
					return false;
				}

				if (!UpdateDelayedKingdomDecisionsSafe(kingdom))
				{
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] UpdateKingdomDecisions prefix error: {ex.Message}");
				return true;
			}
		}

		private static bool UpdateDelayedKingdomDecisionsSafe(Kingdom kingdom)
		{
			try
			{
				List<KingdomDecision> decisions = kingdom?.UnresolvedDecisions?.ToList() ?? new List<KingdomDecision>();
				int cancelled = 0;
				int started = 0;

				foreach (KingdomDecision decision in decisions)
				{
					if (decision == null || !IsDecisionPendingInKingdom(decision, kingdom))
					{
						continue;
					}

					if (TryNormalizeRedundantSingleClanFiefAgenda(kingdom, decision, "update-cleanup"))
					{
						if (CancelDecisionSafe(kingdom, decision))
						{
							cancelled++;
						}
						continue;
					}

					if (ShouldCancelDecisionSafe(decision))
					{
						if (CancelDecisionSafe(kingdom, decision))
						{
							cancelled++;
						}
						continue;
					}

					if (decision.IsEnforced)
					{
						if (!NeedsPlayerResolutionSafe(decision) && StartDecisionElectionSafe(kingdom, decision))
						{
							started++;
						}
						continue;
					}

					if (IsDecisionTriggerFutureSafe(decision))
					{
						continue;
					}

					if (RequiresPlayerRulerResolution(decision))
					{
						TryLaunchRequiredAgendaVote(decision);
						continue;
					}

					if (NeedsPlayerResolutionSafe(decision))
					{
						continue;
					}

					if (StartDecisionElectionSafe(kingdom, decision))
					{
						started++;
					}
				}

				if (cancelled > 0 || started > 0)
				{
					Logger.Log("VoteDeal", $"[AgendaDelay] UpdateKingdomDecisions handled kingdom={kingdom.StringId ?? ""} cancelled={cancelled} started={started}");
				}
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] Delayed UpdateKingdomDecisions failed, falling back to original: {ex.Message}");
				return false;
			}
		}

		private static bool IsKingdomEliminatedSafe(Kingdom kingdom)
		{
			try
			{
				return kingdom?.IsEliminated == true;
			}
			catch
			{
				return true;
			}
		}

		private static int ClearUnresolvedDecisionsSafe(Kingdom kingdom)
		{
			int removed = 0;
			try
			{
				List<KingdomDecision> decisions = kingdom?.UnresolvedDecisions?.ToList() ?? new List<KingdomDecision>();
				foreach (KingdomDecision decision in decisions)
				{
					try
					{
						kingdom.RemoveDecision(decision);
						removed++;
					}
					catch (Exception ex)
					{
						Logger.Log("VoteDeal", $"[AgendaDelay] Failed to remove eliminated kingdom decision: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] Failed to enumerate eliminated kingdom decisions: {ex.Message}");
			}
			return removed;
		}

		private static bool ShouldCancelDecisionSafe(KingdomDecision decision)
		{
			try
			{
				return decision?.ShouldBeCancelled() == true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] ShouldBeCancelled failed: {ex.Message}");
				return false;
			}
		}

		private static bool TryNormalizeRedundantSingleClanFiefAgenda(
			Kingdom kingdom,
			KingdomDecision decision,
			string source)
		{
			if (!TryGetRedundantSingleClanFiefAgenda(kingdom, decision, out Settlement settlement, out Clan soleClan))
			{
				return false;
			}

			try
			{
				// Applying an owner change by kingdom decision removes the governor
				// even when the selected clan already owns the fief. With only one
				// eligible clan there is no ownership choice, so finalize the existing
				// owner marker directly and leave governor/garrison state untouched.
				settlement.Town.IsOwnerUnassigned = false;
				Logger.Log(
					"VoteDeal",
					$"[SingleClanFief] Suppressed redundant fief agenda: source={source ?? ""}, kingdom={kingdom.StringId ?? ""}, settlement={settlement.StringId ?? ""}, owner={soleClan.StringId ?? ""}, decision={decision.GetType().Name}");
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[SingleClanFief] Normalize failed: source={source ?? ""}, error={ex.Message}");
				return false;
			}
		}

		internal static bool TryGetRedundantSingleClanFiefAgenda(
			Kingdom kingdom,
			KingdomDecision decision,
			out Settlement settlement,
			out Clan soleClan)
		{
			settlement = null;
			soleClan = null;
			if (kingdom == null || decision == null || kingdom.Clans == null) return false;

			if (decision is SettlementClaimantDecision claimantDecision)
			{
				settlement = claimantDecision.Settlement;
			}
			else if (decision is SettlementClaimantPreliminaryDecision preliminaryDecision)
			{
				settlement = preliminaryDecision.Settlement;
			}
			else
			{
				return false;
			}

			if (decision.Kingdom != null && decision.Kingdom != kingdom) return false;
			if (settlement == null || !settlement.IsFortification || settlement.Town == null) return false;
			if (settlement.MapFaction != kingdom) return false;

			// Match claimant eligibility rather than Kingdom.Clans.Count so a
			// mercenary clan cannot turn a functionally single-clan realm into a
			// fake multi-clan fief election.
			foreach (Clan clan in kingdom.Clans)
			{
				if (clan == null || clan.IsUnderMercenaryService || clan.IsEliminated || clan.Leader == null || clan.Leader.IsDead)
				{
					continue;
				}
				if (soleClan != null) return false;
				soleClan = clan;
			}

			return soleClan != null && settlement.OwnerClan == soleClan;
		}

		private static bool IsDecisionTriggerFutureSafe(KingdomDecision decision)
		{
			try
			{
				return decision?.TriggerTime.IsFuture == true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] TriggerTime check failed: {ex.Message}");
				return false;
			}
		}

		private static bool NeedsPlayerResolutionSafe(KingdomDecision decision)
		{
			try
			{
				return decision?.NeedsPlayerResolution == true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] NeedsPlayerResolution check failed: {ex.Message}");
				return false;
			}
		}

		private static bool CancelDecisionSafe(Kingdom kingdom, KingdomDecision decision)
		{
			try
			{
				if (!IsDecisionPendingInKingdom(decision, kingdom))
				{
					return false;
				}
				kingdom.RemoveDecision(decision);
				CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(decision, IsPlayerInvolvedInDecision(decision));
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] Failed to cancel delayed kingdom decision: {ex.Message}");
				return false;
			}
		}

		private static bool StartDecisionElectionSafe(Kingdom kingdom, KingdomDecision decision)
		{
			try
			{
				if (!IsDecisionPendingInKingdom(decision, kingdom))
				{
					return false;
				}
				new KingdomElection(decision).StartElectionWithoutPlayer();
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDelay] Failed to start delayed kingdom decision election: {ex.Message}");
				return false;
			}
		}

		// Vanilla auto-resolves one-clan decisions and picks the outcome sponsored by
		// the player clan. That shortcut bypasses AnimusForge's agenda vote/final-ruling
		// flow, so keep pending player-kingdom agendas interactive. Redundant single-clan
		// fief decisions are normalized separately because they have no real choice.
		private static bool Patch_KingdomDecision_IsSingleClanDecision_Prefix(
			KingdomDecision __instance,
			ref bool __result)
		{
			try
			{
				if (!RequiresInteractiveSingleClanAgendaVote(__instance)) return true;

				__result = false;
				Logger.Log(
					"VoteDeal",
					$"[SingleClanAgenda] Pending agenda kept interactive: kingdom={__instance.Kingdom?.StringId}, decision={GetSafeDecisionTitle(__instance)}");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[SingleClanAgenda] Interactive check failed: {ex.Message}");
				return true;
			}
		}

		private static bool RequiresInteractiveSingleClanAgendaVote(KingdomDecision decision)
		{
			if (decision == null || decision.IsEnforced) return false;

			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (playerKingdom == null || decision.Kingdom != playerKingdom) return false;
			if (playerKingdom.RulingClan != Clan.PlayerClan || playerKingdom.Clans.Count != 1) return false;
			if (!decision.IsPlayerParticipant) return false;
			if (TryGetRedundantSingleClanFiefAgenda(playerKingdom, decision, out _, out _)) return false;

			return IsDecisionPendingInKingdom(decision, playerKingdom);
		}

		private static bool Patch_BilateralDiplomacy_ShouldBeCancelled_Prefix(KingdomDecision __instance, ref bool __result)
		{
			try
			{
				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (behavior?.IsDialogueProposedDecision(__instance) == true)
				{
					__result = !IsDialogueProposedDecisionHardValid(__instance, out string hardFailureReason);
					if (__result)
					{
						behavior.UnregisterDialogueProposedDecision(__instance);
						Logger.Log("ProposeAgenda", "Protected decision became invalid: " + hardFailureReason + " decision=" + GetSafeDecisionTitle(__instance));
					}
					return false;
				}
				if (behavior?.FindCounterpartRecordForDecision(__instance) == null) return true;

				__result = !IsBilateralCounterpartDecisionStillHardValid(__instance);
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] ShouldBeCancelled guard error: {ex.Message}");
				return true;
			}
		}

		private void RegisterDialogueProposedDecision(KingdomDecision decision, string supportWeight)
		{
			if (decision == null) return;
			string key = BuildVoteDealDecisionKey(decision, includeProposer: true);
			if (string.IsNullOrWhiteSpace(key)) return;
			Supporter.SupportWeights weight = ParseSupportWeight(supportWeight);
			string outcomeKey = BuildDialogueProposalQueriedOutcomeKey(decision);
			_dialogueProposalDecisionKeys[key] = string.Join(RecordDelimiter.ToString(),
				CampaignTime.Now.ElapsedDaysUntilNow.ToString("F6"),
				((int)weight).ToString(),
				outcomeKey ?? "");
			Logger.Log("ProposeAgenda", "Protected dialogue proposal registered: " + key + " weight=" + weight + " outcome=" + outcomeKey);
		}

		private static string BuildDialogueProposalQueriedOutcomeKey(KingdomDecision decision)
		{
			try
			{
				MBList<DecisionOutcome> initial = new MBList<DecisionOutcome>();
				foreach (DecisionOutcome outcome in decision?.DetermineInitialCandidates() ?? Enumerable.Empty<DecisionOutcome>())
				{
					if (outcome != null) initial.Add(outcome);
				}
				if (initial.Count == 0) return "";
				MBList<DecisionOutcome> narrowed = decision.NarrowDownCandidates(initial, 3);
				if (narrowed == null || narrowed.Count == 0) return "";
				decision.DetermineSponsors(narrowed);
				DecisionOutcome queried = decision.GetQueriedDecisionOutcome(narrowed) ?? narrowed[0];
				return BuildVoteDealOutcomeKey(queried, includeSponsor: false);
			}
			catch (Exception ex)
			{
				Logger.Log("ProposeAgenda", "Failed to resolve queried outcome: " + ex.Message);
				return "";
			}
		}

		private bool TryGetDialogueProposalSupport(KingdomDecision decision, Clan clan, DecisionOutcome outcome, out int supportWeightValue, out bool isPromisedOutcome)
		{
			supportWeightValue = (int)Supporter.SupportWeights.FullyPush;
			isPromisedOutcome = false;
			if (decision == null || clan == null || outcome == null || decision.ProposerClan != clan || _dialogueProposalDecisionKeys == null) return false;
			string decisionKey = BuildVoteDealDecisionKey(decision, includeProposer: true);
			if (string.IsNullOrWhiteSpace(decisionKey) || !_dialogueProposalDecisionKeys.TryGetValue(decisionKey, out string serialized)) return false;
			string[] parts = (serialized ?? "").Split(RecordDelimiter);
			if (parts.Length > 1 && int.TryParse(parts[1], out int parsedWeight)) supportWeightValue = parsedWeight;
			string promisedOutcomeKey = parts.Length > 2 ? parts[2] ?? "" : "";
			string currentOutcomeKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: false);
			isPromisedOutcome = string.IsNullOrWhiteSpace(promisedOutcomeKey)
				? string.Equals(currentOutcomeKey, BuildDialogueProposalQueriedOutcomeKey(decision), StringComparison.Ordinal)
				: string.Equals(currentOutcomeKey, promisedOutcomeKey, StringComparison.Ordinal);
			return true;
		}

		private bool IsDialogueProposedDecision(KingdomDecision decision)
		{
			if (decision == null || _dialogueProposalDecisionKeys == null) return false;
			string key = BuildVoteDealDecisionKey(decision, includeProposer: true);
			return !string.IsNullOrWhiteSpace(key) && _dialogueProposalDecisionKeys.ContainsKey(key);
		}

		private void UnregisterDialogueProposedDecision(KingdomDecision decision)
		{
			if (decision == null || _dialogueProposalDecisionKeys == null) return;
			string key = BuildVoteDealDecisionKey(decision, includeProposer: true);
			if (!string.IsNullOrWhiteSpace(key)) _dialogueProposalDecisionKeys.Remove(key);
		}

		private static bool IsDialogueProposedDecisionHardValid(KingdomDecision decision, out string reason)
		{
			reason = "";
			try
			{
				if (decision == null) { reason = "decision_null"; return false; }
				Kingdom kingdom = decision.Kingdom;
				Clan proposer = decision.ProposerClan;
				if (kingdom == null || kingdom.IsEliminated) { reason = "kingdom_invalid"; return false; }
				if (proposer == null || proposer.Kingdom != kingdom) { reason = "proposer_left_kingdom"; return false; }
				if (!decision.IsAllowed()) { reason = "decision_not_allowed"; return false; }

				MethodInfo internalCheck = AccessTools.Method(decision.GetType(), "ShouldBeCancelledInternal")
					?? AccessTools.Method(typeof(KingdomDecision), "ShouldBeCancelledInternal");
				if (internalCheck != null && Convert.ToBoolean(internalCheck.Invoke(decision, null)))
				{
					reason = "target_state_changed";
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				reason = "hard_validity_exception:" + ex.Message;
				return false;
			}
		}

		private static bool Patch_BilateralDiplomacy_OnShowDecision_Prefix(KingdomDecision __instance, ref bool __result)
		{
			try
			{
				if (IsBilateralDiplomacyCounterpartAgenda(__instance))
				{
					__result = true;
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] Counterpart OnShowDecision bypassed for auto agenda vote: {GetSafeDecisionTitle(__instance)}");
					return false;
				}

				if (ShouldBypassNativeDiplomacyOfferPopup(__instance))
				{
					__result = true;
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] Native diplomacy offer popup bypassed; routing to bilateral agenda: {GetSafeDecisionTitle(__instance)}");
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] OnShowDecision guard error: {ex.Message}");
				return true;
			}
		}

		private static bool Patch_BilateralDiplomacy_ApplyChosenOutcome_Prefix(KingdomDecision __instance, DecisionOutcome chosenOutcome)
		{
			try
			{
				if (!TryGetBilateralDiplomacyDecisionDetails(__instance, out BilateralDiplomacyKind kind, out Kingdom source, out Kingdom target, out int tribute, out int duration))
				{
					return true;
				}
				if (!TryGetBilateralDiplomacyApproval(__instance, chosenOutcome, out bool approved))
				{
					return true;
				}

				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (behavior == null) return true;

				return !behavior.HandleBilateralDiplomacyOutcome(__instance, kind, source, target, tribute, duration, approved);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] ApplyChosenOutcome prefix error: {ex.Message}");
				return true;
			}
		}

		private bool HandleBilateralDiplomacyOutcome(KingdomDecision decision, BilateralDiplomacyKind kind, Kingdom source, Kingdom target, int tribute, int duration, bool approved)
		{
			BilateralDiplomacyRecord counterpartRecord = FindCounterpartRecordForDecision(decision);
			if (counterpartRecord != null)
			{
				MergeBilateralDiplomacyParticipants(counterpartRecord, decision);
				MarkBilateralDiplomacyMemoryHandled(decision);
				if (approved)
				{
					bool applied = ApplyFinalBilateralDiplomacyEffect(counterpartRecord);
					RecordBilateralDiplomacyMemory(counterpartRecord, decision, applied ? "effective" : "invalid");
					_bilateralDiplomacyRecords.Remove(counterpartRecord);
					if (applied)
					{
						DisplayBilateralDiplomacyMessage(
							$"双向外交议程：{GetKingdomName(counterpartRecord.FirstKingdomId)}与{GetKingdomName(counterpartRecord.SecondKingdomId)}均已批准{GetBilateralDiplomacyKindText(counterpartRecord)}，协议已生效。",
							0x9BE564);
					}
					else
					{
						DisplayBilateralDiplomacyMessage(
							$"双向外交议程失效：{GetKingdomName(counterpartRecord.FirstKingdomId)}与{GetKingdomName(counterpartRecord.SecondKingdomId)}的{GetBilateralDiplomacyKindText(counterpartRecord)}条件已变化，协议未生效。",
							0xFFD166);
					}
				}
				else
				{
					RecordBilateralDiplomacyMemory(counterpartRecord, decision, "rejected");
					_bilateralDiplomacyRecords.Remove(counterpartRecord);
					DisplayBilateralDiplomacyMessage(
						$"双向外交议程：{GetKingdomName(counterpartRecord.SecondKingdomId)}拒绝了{GetKingdomName(counterpartRecord.FirstKingdomId)}的{GetBilateralDiplomacyKindText(counterpartRecord)}提案，外交效果未生效。",
						0xFF6B6B);
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] Counterpart rejected: id={counterpartRecord.RecordId} kind={counterpartRecord.Kind}.");
				}
				return true;
			}

			if (!approved)
			{
				return true;
			}

			if (!CanCreateBilateralCounterpart(kind, source, target))
			{
				BilateralDiplomacyRecord failedRecord = CreateBilateralDiplomacyRecord(decision, kind, source, target, tribute, duration);
				MarkBilateralDiplomacyMemoryHandled(decision);
				RecordBilateralDiplomacyMemory(failedRecord, decision, "creation_failed");
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Cannot create counterpart, original effect blocked. kind={kind} source={source?.StringId} target={target?.StringId}");
				DisplayBilateralDiplomacyMessage("双向外交议程：无法创建对等复议议程，外交效果未生效。", 0xFFD166);
				return true;
			}

			BilateralDiplomacyRecord existing = FindExistingBilateralRecord(kind, source, target);
			if (existing != null)
			{
				MergeBilateralDiplomacyParticipants(existing, decision);
				MarkBilateralDiplomacyMemoryHandled(decision);
				RecordBilateralDiplomacyMemory(existing, decision, "pending");
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Duplicate first-side approval blocked while pending counterpart exists. id={existing.RecordId}");
				DisplayBilateralDiplomacyMessage(
					$"双向外交议程：{GetKingdomName(existing.FirstKingdomId)}与{GetKingdomName(existing.SecondKingdomId)}已有等待复议的{GetBilateralDiplomacyKindText(existing)}提案。",
					0xFFD166);
				return true;
			}

			BilateralDiplomacyRecord record = CreateBilateralDiplomacyRecord(decision, kind, source, target, tribute, duration);

			_bilateralDiplomacyRecords.Add(record);
			if (!CreateBilateralCounterpartDecision(record, out string creationFailureReason))
			{
				MarkBilateralDiplomacyMemoryHandled(decision);
				RecordBilateralDiplomacyMemory(record, decision, "creation_failed");
				_bilateralDiplomacyRecords.Remove(record);
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Counterpart creation failed, original effect blocked. id={record.RecordId} reason={creationFailureReason}");
				DisplayBilateralDiplomacyMessage("双向外交议程：对等复议议程创建失败，外交效果未生效。", 0xFFD166);
				return true;
			}

			MarkBilateralDiplomacyMemoryHandled(decision);
			RecordBilateralDiplomacyMemory(record, decision, "pending");
			DisplayBilateralDiplomacyMessage(
				$"双向外交议程：{source.Name}已批准{GetBilateralDiplomacyKindText(record)}提案，已提交给{target.Name}复议。",
				0xFFD166);
			Logger.Log("VoteDeal", $"[BilateralDiplomacy] First-side approval recorded: id={record.RecordId} kind={record.Kind} {source.StringId}->{target.StringId}.");
			return true;
		}

		private bool CreateBilateralCounterpartDecision(BilateralDiplomacyRecord record, out string failureReason)
		{
			failureReason = "";
			try
			{
				if (record == null)
				{
					failureReason = "record_null";
					return false;
				}
				Kingdom first = ResolveKingdom(record.FirstKingdomId);
				Kingdom second = ResolveKingdom(record.SecondKingdomId);
				if (first == null)
				{
					failureReason = "first_kingdom_missing:" + (record.FirstKingdomId ?? "");
					return false;
				}
				if (second == null)
				{
					failureReason = "second_kingdom_missing:" + (record.SecondKingdomId ?? "");
					return false;
				}
				if (first == second)
				{
					failureReason = "same_kingdom:" + (first.StringId ?? "");
					return false;
				}
				Clan technicalProposer = second.RulingClan;
				if (technicalProposer == null)
				{
					failureReason = "second_ruling_clan_missing:" + (second.StringId ?? "");
					return false;
				}
				if (technicalProposer.Kingdom != second)
				{
					failureReason = "second_ruling_clan_not_in_second:" + (technicalProposer.StringId ?? "");
					return false;
				}

				BilateralDiplomacyKind kind = ParseKind(record.Kind);
				KingdomDecision counterpart;
				switch (kind)
				{
					case BilateralDiplomacyKind.Peace:
						counterpart = new MakePeaceKingdomDecision(technicalProposer, first, -record.TributeFromFirstToSecond, record.TributeDurationDays, true, true);
						break;
					case BilateralDiplomacyKind.Alliance:
						counterpart = new StartAllianceDecision(technicalProposer, first);
						break;
					case BilateralDiplomacyKind.Trade:
						counterpart = new TradeAgreementDecision(technicalProposer, first);
						break;
					default:
						failureReason = "unsupported_kind:" + (record.Kind ?? "");
						return false;
				}

				if (!IsBilateralCounterpartDecisionHardValid(counterpart, out failureReason))
				{
					return false;
				}
				if (!QueueBilateralCounterpartDecision(second, counterpart, record, out failureReason))
				{
					record.CounterpartCreated = false;
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				if (record != null) record.CounterpartCreated = false;
				failureReason = "exception:" + ex.Message;
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Create counterpart error: {ex.Message}");
				return false;
			}
		}

		private static bool QueueBilateralCounterpartDecision(Kingdom kingdom, KingdomDecision decision, BilateralDiplomacyRecord record, out string failureReason)
		{
			failureReason = "";
			try
			{
				if (kingdom == null)
				{
					failureReason = "queue_kingdom_null";
					return false;
				}
				if (decision == null)
				{
					failureReason = "queue_decision_null";
					return false;
				}
				if (record == null)
				{
					failureReason = "queue_record_null";
					return false;
				}

				MBList<KingdomDecision> unresolved = Traverse.Create(kingdom)
					.Field("_unresolvedDecisions")
					.GetValue<MBList<KingdomDecision>>();
				if (unresolved == null)
				{
					failureReason = "unresolved_field_null:" + (kingdom.StringId ?? "");
					return false;
				}

				record.CounterpartCreated = true;
				float delayDays = 3f;
				SetDecisionTriggerTime(decision, delayDays);
				try
				{
					CampaignEventDispatcher.Instance.OnKingdomDecisionAdded(decision, false);
				}
				catch (Exception eventEx)
				{
					Logger.Log("VoteDeal", $"[BilateralDiplomacy] Counterpart OnKingdomDecisionAdded failed: {eventEx.Message}");
				}

				if (!unresolved.Contains(decision))
				{
					unresolved.Add(decision);
				}
				if (kingdom.UnresolvedDecisions == null || !kingdom.UnresolvedDecisions.Contains(decision))
				{
					failureReason = "queue_contains_failed:" + (kingdom.StringId ?? "");
					return false;
				}

				RegisterAgendaDelaySnapshot(decision, kingdom, delayDays);
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Queued counterpart agenda directly: id={record.RecordId} kingdom={kingdom.StringId} decision={GetSafeDecisionTitle(decision)}");
				return true;
			}
			catch (Exception ex)
			{
				if (record != null) record.CounterpartCreated = false;
				failureReason = "queue_exception:" + ex.Message;
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Queue counterpart agenda failed: {ex.Message}");
				return false;
			}
		}

		private static void SetDecisionTriggerTime(KingdomDecision decision, float delayDays)
		{
			if (decision == null) return;
			Traverse.Create(decision).Property("TriggerTime")
				.SetValue(CampaignTime.DaysFromNow(delayDays));
		}

		internal static bool IsDecisionPendingInKingdom(KingdomDecision decision, Kingdom kingdom)
		{
			try
			{
				return decision != null
					&& kingdom?.UnresolvedDecisions != null
					&& kingdom.UnresolvedDecisions.Contains(decision);
			}
			catch
			{
				return false;
			}
		}

		private static void RegisterAgendaDelaySnapshot(KingdomDecision decision, Kingdom kingdom, float delayDays)
		{
			try
			{
				if (decision == null || kingdom == null)
				{
					return;
				}
				lock (KingdomAgendaVM._snapshots)
				{
					KingdomAgendaVM._snapshots.RemoveAll(snapshot => snapshot?.Decision == decision);
					if (!IsDecisionPendingInKingdom(decision, kingdom))
					{
						return;
					}
					SetDecisionTriggerTime(decision, delayDays);
					KingdomAgendaVM._snapshots.Add(new KingdomAgendaVM.Snapshot
					{
						Decision = decision,
						KingdomId = kingdom.StringId,
						CreatedAt = CampaignTime.Now,
						DelayDays = delayDays,
						IsPlayerKingdom = kingdom == Clan.PlayerClan?.Kingdom
					});
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Register agenda delay snapshot failed: {ex.Message}");
			}
		}

		private bool ApplyFinalBilateralDiplomacyEffect(BilateralDiplomacyRecord record)
		{
			try
			{
				if (!IsBilateralRecordStillPendingValid(record)) return false;
				Kingdom first = ResolveKingdom(record.FirstKingdomId);
				Kingdom second = ResolveKingdom(record.SecondKingdomId);
				BilateralDiplomacyKind kind = ParseKind(record.Kind);
				switch (kind)
				{
					case BilateralDiplomacyKind.Peace:
						MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("bilateral_diplomacy_peace", () =>
							MakePeaceAction.ApplyByKingdomDecision(first, second, record.TributeFromFirstToSecond, record.TributeDurationDays));
						DiplomacyRecentPeaceGuard.RegisterPeace(first, second, "bilateral_diplomacy_peace");
						return true;
					case BilateralDiplomacyKind.Alliance:
					{
						IAllianceCampaignBehavior allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
						if (allianceBehavior == null) return false;
						MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("bilateral_diplomacy_alliance", () =>
							allianceBehavior.StartAlliance(first, second));
						return true;
					}
					case BilateralDiplomacyKind.Trade:
					{
						ITradeAgreementsCampaignBehavior tradeBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
						if (tradeBehavior == null) return false;
						CampaignTime tradeDuration = Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(first, second);
						MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked("bilateral_diplomacy_trade", () =>
							tradeBehavior.MakeTradeAgreement(first, second, tradeDuration));
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Apply final effect error: {ex.Message}");
			}
			return false;
		}

		private BilateralDiplomacyRecord CreateBilateralDiplomacyRecord(KingdomDecision decision, BilateralDiplomacyKind kind, Kingdom source, Kingdom target, int tribute, int duration)
		{
			BilateralDiplomacyRecord record = new BilateralDiplomacyRecord
			{
				RecordId = GenerateBilateralDiplomacyId(),
				Kind = KindToString(kind),
				FirstKingdomId = source?.StringId ?? "",
				SecondKingdomId = target?.StringId ?? "",
				TributeFromFirstToSecond = tribute,
				TributeDurationDays = duration,
				CreatedDay = CampaignTime.Now.ElapsedDaysUntilNow,
				CounterpartCreated = false,
				FirstDecisionBasicKey = BuildVoteDealDecisionKey(decision, includeProposer: false),
				ParticipantHeroIds = ""
			};
			MergeBilateralDiplomacyParticipants(record, decision);
			return record;
		}

		private void MergeBilateralDiplomacyParticipants(BilateralDiplomacyRecord record, KingdomDecision decision)
		{
			if (record == null) return;
			HashSet<string> heroIds = new HashSet<string>(
				(record.ParticipantHeroIds ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(id => id.Trim()).Where(id => !string.IsNullOrWhiteSpace(id)),
				StringComparer.OrdinalIgnoreCase);
			try
			{
				if (_activeDeals != null && decision != null)
				{
					foreach (VoteDealRecord deal in _activeDeals.Where(deal => deal != null && DoesVoteDealMatchDecision(deal, decision)))
					{
						if (!string.IsNullOrWhiteSpace(deal.NpcHeroStringId)) heroIds.Add(deal.NpcHeroStringId.Trim());
					}
				}
				if (_activeDeals != null && !string.IsNullOrWhiteSpace(record.FirstDecisionBasicKey))
				{
					foreach (VoteDealRecord deal in _activeDeals.Where(deal => deal != null && string.Equals(deal.TargetDecisionBasicKey, record.FirstDecisionBasicKey, StringComparison.Ordinal)))
					{
						if (!string.IsNullOrWhiteSpace(deal.NpcHeroStringId)) heroIds.Add(deal.NpcHeroStringId.Trim());
					}
				}
				AddBilateralDiplomacyParticipant(heroIds, decision?.ProposerClan?.Leader);
				AddBilateralDiplomacyParticipant(heroIds, decision?.DetermineChooser()?.Leader);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", "[BilateralDiplomacy] Participant capture warning: " + ex.Message);
			}
			record.ParticipantHeroIds = string.Join(";", heroIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
		}

		private static void AddBilateralDiplomacyParticipant(ISet<string> heroIds, Hero hero)
		{
			if (heroIds == null || hero == null || string.IsNullOrWhiteSpace(hero.StringId)) return;
			heroIds.Add(hero.StringId.Trim());
		}

		private static void MarkBilateralDiplomacyMemoryHandled(KingdomDecision decision)
		{
			if (decision == null) return;
			try
			{
				BilateralDiplomacyMemoryMarkers.Remove(decision);
				BilateralDiplomacyMemoryMarkers.Add(decision, new BilateralDiplomacyMemoryMarker());
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", "[BilateralDiplomacy] Memory marker warning: " + ex.Message);
			}
		}

		private void OnKingdomDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
		{
			ForgetAgendaMapNotification(decision);
			try
			{
				BilateralDiplomacyRecord record = FindCounterpartRecordForDecision(decision);
				if (record == null) return;
				MergeBilateralDiplomacyParticipants(record, decision);
				MarkBilateralDiplomacyMemoryHandled(decision);
				RecordBilateralDiplomacyMemory(record, decision, "cancelled");
				_bilateralDiplomacyRecords.Remove(record);
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Counterpart agenda cancelled: id={record.RecordId} decision={GetSafeDecisionTitle(decision)} playerInvolved={isPlayerInvolved}.");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", "[BilateralDiplomacy] Cancelled agenda memory error: " + ex.Message);
			}
		}

		internal static bool IsBilateralDiplomacyMemoryHandledDecision(KingdomDecision decision)
		{
			return decision != null && BilateralDiplomacyMemoryMarkers.TryGetValue(decision, out _);
		}

		private void RecordBilateralDiplomacyMemory(BilateralDiplomacyRecord record, KingdomDecision decision, string state)
		{
			try
			{
				if (record == null) return;
				Kingdom first = ResolveKingdom(record.FirstKingdomId);
				Kingdom second = ResolveKingdom(record.SecondKingdomId);
				string firstName = first?.Name?.ToString() ?? record.FirstKingdomId ?? "第一方王国";
				string secondName = second?.Name?.ToString() ?? record.SecondKingdomId ?? "第二方王国";
				string kindText = GetBilateralDiplomacyKindText(record);
				string narrative;
				bool isFinal = !string.Equals(state, "pending", StringComparison.OrdinalIgnoreCase);
				bool? won = null;
				switch ((state ?? "").Trim().ToLowerInvariant())
				{
					case "effective":
						narrative = firstName + "与" + secondName + "均已批准" + kindText + "提案，双边协议已经正式生效。";
						won = true;
						break;
					case "rejected":
						narrative = firstName + "曾在本国议程中批准" + kindText + "提案，但" + secondName + "在对等复议中拒绝；该提案未获双方同意，协议未达成、未生效。";
						won = false;
						break;
					case "invalid":
						narrative = firstName + "与" + secondName + "虽先后批准" + kindText + "提案，但生效条件已经变化；协议最终未生效。";
						won = false;
						break;
					case "creation_failed":
						narrative = firstName + "仅在本国议程中批准了" + kindText + "提案，但未能建立由" + secondName + "进行的对等复议；该提案未获双方同意，协议未达成、未生效。";
						won = false;
						break;
					case "cancelled":
						narrative = firstName + "曾在本国议程中批准" + kindText + "提案，但" + secondName + "的对等复议已经取消或失效；该提案未获双方有效同意，协议未达成、未生效。";
						won = false;
						break;
					default:
						narrative = firstName + "仅在本国议程中批准了" + kindText + "提案，并已提交" + secondName + "复议；" + secondName + "尚未批准，双边协议尚未达成、尚未生效。";
						isFinal = false;
						break;
				}

				HashSet<string> heroIds = new HashSet<string>(
					(record.ParticipantHeroIds ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(id => id.Trim()).Where(id => !string.IsNullOrWhiteSpace(id)),
					StringComparer.OrdinalIgnoreCase);
				AddBilateralDiplomacyParticipant(heroIds, first?.RulingClan?.Leader);
				AddBilateralDiplomacyParticipant(heroIds, second?.RulingClan?.Leader);
				AddBilateralDiplomacyParticipant(heroIds, decision?.ProposerClan?.Leader);
				try { AddBilateralDiplomacyParticipant(heroIds, decision?.DetermineChooser()?.Leader); } catch { }

				string fact = "[AFEF NPC行为补充] 双边外交状态：" + narrative;
				string stableKey = "bilateral_diplomacy:" + (record.RecordId ?? "unknown") + ":" + (isFinal ? "outcome" : "pending");
				string actionKind = isFinal ? "bilateral_diplomacy_outcome" : "bilateral_diplomacy_pending";
				string locationText = firstName + "—" + secondName;
				Hero firstRuler = first?.RulingClan?.Leader;
				Hero secondRuler = second?.RulingClan?.Leader;
				int recorded = 0;
				foreach (string heroId in heroIds)
				{
					Hero hero = Hero.FindFirst(candidate => candidate != null && string.Equals(candidate.StringId, heroId, StringComparison.OrdinalIgnoreCase));
					if (hero == null) continue;
					Hero targetHero = hero.Clan?.Kingdom == first ? secondRuler : firstRuler;
					if (hero == Hero.MainHero)
					{
						MyBehavior.RecordPlayerActionForExternal(narrative, stableKey + ":player", actionKind, isMajor: isFinal, targetHero: targetHero, settlement: null, locationText: locationText, won: won);
					}
					else
					{
						MyBehavior.AppendExternalDialogueHistory(hero, null, null, fact);
						MyBehavior.RecordNpcActionForExternal(hero, narrative, stableKey, actionKind, isMajor: isFinal, isRecent: true, targetHero: targetHero, settlement: null, locationText: locationText, allowNonLordHero: false, won: won);
					}
					recorded++;
				}
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Memory recorded id={record.RecordId} state={state} audience={recorded} text={narrative}");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", "[BilateralDiplomacy] Memory recording failed: " + ex.Message);
			}
		}

		private static bool TryGetBilateralDiplomacyDecisionDetails(KingdomDecision decision, out BilateralDiplomacyKind kind, out Kingdom source, out Kingdom target, out int tribute, out int duration)
		{
			kind = BilateralDiplomacyKind.None;
			source = decision?.Kingdom;
			target = null;
			tribute = 0;
			duration = 0;
			if (decision == null || source == null) return false;

			if (decision is MakePeaceKingdomDecision peaceDecision)
			{
				kind = BilateralDiplomacyKind.Peace;
				target = peaceDecision.FactionToMakePeaceWith as Kingdom;
				tribute = peaceDecision.DailyTributeToBePaid;
				duration = peaceDecision.DailyTributeDurationInDays;
				return target != null;
			}
			if (decision is StartAllianceDecision allianceDecision)
			{
				kind = BilateralDiplomacyKind.Alliance;
				target = allianceDecision.KingdomToStartAllianceWith;
				return target != null;
			}
			if (decision is TradeAgreementDecision tradeDecision)
			{
				kind = BilateralDiplomacyKind.Trade;
				target = tradeDecision.TargetKingdom;
				return target != null;
			}
			return false;
		}

		private static bool TryGetBilateralDiplomacyApproval(KingdomDecision decision, DecisionOutcome outcome, out bool approved)
		{
			approved = false;
			if (decision == null || outcome == null) return false;
			string memberName = null;
			if (decision is MakePeaceKingdomDecision) memberName = "ShouldPeaceBeDeclared";
			else if (decision is StartAllianceDecision) memberName = "ShouldAllianceBeStarted";
			else if (decision is TradeAgreementDecision) memberName = "ShouldTradeAgreementStart";
			if (string.IsNullOrEmpty(memberName)) return false;

			Type outcomeType = outcome.GetType();
			PropertyInfo property = outcomeType.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (property != null && property.PropertyType == typeof(bool))
			{
				approved = (bool)property.GetValue(outcome, null);
				return true;
			}
			FieldInfo field = outcomeType.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (field != null && field.FieldType == typeof(bool))
			{
				approved = (bool)field.GetValue(outcome);
				return true;
			}
			return false;
		}

		private BilateralDiplomacyRecord FindCounterpartRecordForDecision(KingdomDecision decision)
		{
			if (_bilateralDiplomacyRecords == null || decision == null) return null;
			return _bilateralDiplomacyRecords.FirstOrDefault(r => DoesDecisionMatchCounterpartRecord(r, decision));
		}

		private BilateralDiplomacyRecord FindExistingBilateralRecord(BilateralDiplomacyKind kind, Kingdom first, Kingdom second)
		{
			if (_bilateralDiplomacyRecords == null || first == null || second == null) return null;
			string firstId = first.StringId;
			string secondId = second.StringId;
			return _bilateralDiplomacyRecords.FirstOrDefault(r =>
				ParseKind(r.Kind) == kind
				&& ((string.Equals(r.FirstKingdomId, firstId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(r.SecondKingdomId, secondId, StringComparison.OrdinalIgnoreCase))
					|| (string.Equals(r.FirstKingdomId, secondId, StringComparison.OrdinalIgnoreCase)
						&& string.Equals(r.SecondKingdomId, firstId, StringComparison.OrdinalIgnoreCase))));
		}

		private bool IsDuplicateWhileBilateralDiplomacyPending(KingdomDecision decision)
		{
			try
			{
				if (_bilateralDiplomacyRecords == null || _bilateralDiplomacyRecords.Count == 0) return false;
				if (FindCounterpartRecordForDecision(decision) != null) return false;
				if (!TryGetBilateralDiplomacyDecisionDetails(decision, out BilateralDiplomacyKind kind, out Kingdom source, out Kingdom target, out int tribute, out int duration))
				{
					return false;
				}
				return FindExistingBilateralRecord(kind, source, target) != null;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Duplicate check error: {ex.Message}");
				return false;
			}
		}

		private void RemoveDuplicateBilateralDiplomacyAgendas()
		{
			try
			{
				if (_bilateralDiplomacyRecords == null || _bilateralDiplomacyRecords.Count == 0) return;
				List<Kingdom> kingdoms = Kingdom.All?.ToList();
				if (kingdoms == null) return;

				foreach (Kingdom kingdom in kingdoms)
				{
					if (kingdom?.UnresolvedDecisions == null || kingdom.UnresolvedDecisions.Count == 0) continue;
					List<KingdomDecision> decisions = kingdom.UnresolvedDecisions.ToList();
					foreach (KingdomDecision decision in decisions)
					{
						if (decision == null) continue;
						if (!kingdom.UnresolvedDecisions.Contains(decision)) continue;
						if (!IsDuplicateWhileBilateralDiplomacyPending(decision)) continue;

						kingdom.RemoveDecision(decision);
						Logger.Log("VoteDeal", $"[BilateralDiplomacy] Removed duplicate pending diplomacy agenda: kingdom={kingdom.StringId}, decision={GetSafeDecisionTitle(decision)}");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Duplicate cleanup error: {ex.Message}");
			}
		}

		private static bool DoesDecisionMatchCounterpartRecord(BilateralDiplomacyRecord record, KingdomDecision decision)
		{
			if (record == null || !record.CounterpartCreated) return false;
			if (!TryGetBilateralDiplomacyDecisionDetails(decision, out BilateralDiplomacyKind kind, out Kingdom source, out Kingdom target, out int tribute, out int duration))
			{
				return false;
			}
			if (ParseKind(record.Kind) != kind) return false;
			if (!string.Equals(source.StringId, record.SecondKingdomId, StringComparison.OrdinalIgnoreCase)) return false;
			if (!string.Equals(target.StringId, record.FirstKingdomId, StringComparison.OrdinalIgnoreCase)) return false;
			if (kind == BilateralDiplomacyKind.Peace)
			{
				return tribute == -record.TributeFromFirstToSecond && duration == record.TributeDurationDays;
			}
			return true;
		}

		private static bool CanCreateBilateralCounterpart(BilateralDiplomacyKind kind, Kingdom source, Kingdom target)
		{
			if (kind == BilateralDiplomacyKind.None || source == null || target == null || source == target) return false;
			if (source.IsEliminated || target.IsEliminated) return false;
			if (target.RulingClan == null) return false;
			switch (kind)
			{
				case BilateralDiplomacyKind.Peace:
					return FactionManager.IsAtWarAgainstFaction(source, target);
				case BilateralDiplomacyKind.Alliance:
				{
					IAllianceCampaignBehavior allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
					if (allianceBehavior == null) return false;
					if (FactionManager.IsAtWarAgainstFaction(source, target)) return false;
					if (allianceBehavior.IsAllyWithKingdom(source, target)) return false;
					return source.AlliedKingdoms.Count < Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances
						&& target.AlliedKingdoms.Count < Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances;
				}
				case BilateralDiplomacyKind.Trade:
				{
					ITradeAgreementsCampaignBehavior tradeBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
					if (tradeBehavior == null) return false;
					if (FactionManager.IsAtWarAgainstFaction(source, target)) return false;
					return !HasTradeAgreementCompat(tradeBehavior, source, target);
				}
			}
			return false;
		}

		private static bool IsBilateralCounterpartDecisionStillHardValid(KingdomDecision decision)
		{
			return IsBilateralCounterpartDecisionHardValid(decision, out _);
		}

		private static bool IsBilateralCounterpartDecisionHardValid(KingdomDecision decision, out string failureReason)
		{
			if (!TryGetBilateralDiplomacyDecisionDetails(decision, out BilateralDiplomacyKind kind, out Kingdom source, out Kingdom target, out int tribute, out int duration))
			{
				failureReason = "details_unresolved";
				return false;
			}
			if (source == null)
			{
				failureReason = "source_null";
				return false;
			}
			if (target == null)
			{
				failureReason = "target_null";
				return false;
			}
			if (source == target)
			{
				failureReason = "same_kingdom:" + (source.StringId ?? "");
				return false;
			}
			if (source.IsEliminated)
			{
				failureReason = "source_eliminated:" + (source.StringId ?? "");
				return false;
			}
			if (target.IsEliminated)
			{
				failureReason = "target_eliminated:" + (target.StringId ?? "");
				return false;
			}
			if (decision.ProposerClan == null)
			{
				failureReason = "proposer_null";
				return false;
			}
			if (decision.ProposerClan.Kingdom != source)
			{
				failureReason = "proposer_not_in_source:" + (decision.ProposerClan.StringId ?? "");
				return false;
			}

			switch (kind)
			{
				case BilateralDiplomacyKind.Peace:
					if (!FactionManager.IsAtWarAgainstFaction(source, target))
					{
						failureReason = "peace_not_at_war:" + (source.StringId ?? "") + "->" + (target.StringId ?? "");
						return false;
					}
					failureReason = "";
					return true;
				case BilateralDiplomacyKind.Alliance:
				{
					IAllianceCampaignBehavior allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
					if (allianceBehavior == null)
					{
						failureReason = "alliance_behavior_missing";
						return false;
					}
					if (FactionManager.IsAtWarAgainstFaction(source, target))
					{
						failureReason = "alliance_at_war";
						return false;
					}
					if (allianceBehavior.IsAllyWithKingdom(source, target))
					{
						failureReason = "alliance_already_allied";
						return false;
					}
					if (source.AlliedKingdoms.Count >= Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances)
					{
						failureReason = "source_alliance_limit:" + (source.StringId ?? "");
						return false;
					}
					if (target.AlliedKingdoms.Count >= Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances)
					{
						failureReason = "target_alliance_limit:" + (target.StringId ?? "");
						return false;
					}
					failureReason = "";
					return true;
				}
				case BilateralDiplomacyKind.Trade:
				{
					ITradeAgreementsCampaignBehavior tradeBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
					if (tradeBehavior == null)
					{
						failureReason = "trade_behavior_missing";
						return false;
					}
					if (FactionManager.IsAtWarAgainstFaction(source, target))
					{
						failureReason = "trade_at_war";
						return false;
					}
					if (HasTradeAgreementCompat(tradeBehavior, source, target))
					{
						failureReason = "trade_already_exists";
						return false;
					}
					failureReason = "";
					return true;
				}
			}
			failureReason = "unsupported_kind:" + kind;
			return false;
		}

		private static bool ShouldBypassNativeDiplomacyOfferPopup(KingdomDecision decision)
		{
			try
			{
				if (!TryGetBilateralDiplomacyDecisionDetails(decision, out BilateralDiplomacyKind kind, out Kingdom source, out Kingdom target, out int tribute, out int duration))
				{
					return false;
				}

				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				if (kind == BilateralDiplomacyKind.None || playerKingdom == null || source == null || target == null)
				{
					return false;
				}

				if (source == playerKingdom || target != playerKingdom)
				{
					return false;
				}

				if (Hero.MainHero?.Clan?.IsUnderMercenaryService == true)
				{
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Native offer bypass check failed: {ex.Message}");
				return false;
			}
		}

		private bool IsBilateralRecordStillPendingValid(BilateralDiplomacyRecord record)
		{
			if (record == null) return false;
			Kingdom first = ResolveKingdom(record.FirstKingdomId);
			Kingdom second = ResolveKingdom(record.SecondKingdomId);
			if (first == null || second == null || first == second) return false;
			if (first.IsEliminated || second.IsEliminated) return false;
			switch (ParseKind(record.Kind))
			{
				case BilateralDiplomacyKind.Peace:
					return FactionManager.IsAtWarAgainstFaction(first, second);
				case BilateralDiplomacyKind.Alliance:
				{
					IAllianceCampaignBehavior allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
					if (allianceBehavior == null) return false;
					if (FactionManager.IsAtWarAgainstFaction(first, second)) return false;
					if (allianceBehavior.IsAllyWithKingdom(first, second)) return false;
					return first.AlliedKingdoms.Count < Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances
						&& second.AlliedKingdoms.Count < Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances;
				}
				case BilateralDiplomacyKind.Trade:
				{
					ITradeAgreementsCampaignBehavior tradeBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
					if (tradeBehavior == null) return false;
					if (FactionManager.IsAtWarAgainstFaction(first, second)) return false;
					return !HasTradeAgreementCompat(tradeBehavior, first, second);
				}
			}
			return false;
		}

		private void CleanupBilateralDiplomacyRecords()
		{
			try
			{
				if (_bilateralDiplomacyRecords == null) return;
				_bilateralDiplomacyRecords.RemoveAll(r =>
					r == null
					|| ParseKind(r.Kind) == BilateralDiplomacyKind.None
					|| ResolveKingdom(r.FirstKingdomId) == null
					|| ResolveKingdom(r.SecondKingdomId) == null);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Cleanup error: {ex.Message}");
			}
		}

		internal static Kingdom ResolveKingdom(string kingdomId)
		{
			if (string.IsNullOrWhiteSpace(kingdomId)) return null;
			return Kingdom.All.FirstOrDefault(k => k != null && string.Equals(k.StringId, kingdomId, StringComparison.OrdinalIgnoreCase));
		}

		private static string KindToString(BilateralDiplomacyKind kind)
		{
			switch (kind)
			{
				case BilateralDiplomacyKind.Peace: return "Peace";
				case BilateralDiplomacyKind.Alliance: return "Alliance";
				case BilateralDiplomacyKind.Trade: return "Trade";
				default: return "";
			}
		}

		private static BilateralDiplomacyKind ParseKind(string kind)
		{
			if (string.Equals(kind, "Peace", StringComparison.OrdinalIgnoreCase)) return BilateralDiplomacyKind.Peace;
			if (string.Equals(kind, "Alliance", StringComparison.OrdinalIgnoreCase)) return BilateralDiplomacyKind.Alliance;
			if (string.Equals(kind, "Trade", StringComparison.OrdinalIgnoreCase)) return BilateralDiplomacyKind.Trade;
			return BilateralDiplomacyKind.None;
		}

		private static string GetKingdomName(string kingdomId)
		{
			Kingdom kingdom = ResolveKingdom(kingdomId);
			return kingdom?.Name?.ToString() ?? kingdomId ?? "未知王国";
		}

		private static string GetBilateralDiplomacyKindText(BilateralDiplomacyRecord record)
		{
			return GetBilateralDiplomacyKindText(ParseKind(record?.Kind));
		}

		private static string GetBilateralDiplomacyKindText(BilateralDiplomacyKind kind)
		{
			switch (kind)
			{
				case BilateralDiplomacyKind.Peace: return "和平";
				case BilateralDiplomacyKind.Alliance: return "同盟";
				case BilateralDiplomacyKind.Trade: return "贸易协定";
				default: return "外交";
			}
		}

		private static void DisplayBilateralDiplomacyMessage(string text, uint color)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(color)));
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Display message error: {ex.Message}");
			}
		}

		private static bool HasTradeAgreementCompat(ITradeAgreementsCampaignBehavior tradeBeh, Kingdom kingdom, Kingdom other)
		{
			return BannerlordApiCompat.HasTradeAgreement(tradeBeh, kingdom, other);
		}

		internal static bool IsBilateralDiplomacyCounterpartAgenda(KingdomDecision decision)
		{
			try
			{
				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				return behavior?.FindCounterpartRecordForDecision(decision) != null;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BilateralDiplomacy] Counterpart agenda check failed: {ex.Message}");
				return false;
			}
		}

		internal static string GetBilateralDiplomacyAgendaTitle(KingdomDecision decision)
		{
			if (!IsBilateralDiplomacyCounterpartAgenda(decision)) return null;
			string title = decision?.GetGeneralTitle()?.ToString() ?? "外交提案";
			return "对等外交复议：" + title;
		}

		internal static string GetBilateralDiplomacyAgendaProposerText(KingdomDecision decision)
		{
			return IsBilateralDiplomacyCounterpartAgenda(decision) ? "无" : null;
		}

		internal static string GetBilateralDiplomacyAgendaTypeText(KingdomDecision decision)
		{
			return IsBilateralDiplomacyCounterpartAgenda(decision) ? "复议" : null;
		}

		// ── Tag processing ─────────────────────────────────────────────────

		private string ProcessTargetedVoteDealTag(Hero npc, string agendaCode, string optionCode, string weightStr, string notes)
		{
			try
			{
				Supporter.SupportWeights weight = ParseSupportWeight(weightStr);
				Clan clan = npc?.Clan;
				Kingdom kingdom = clan?.Kingdom;
				if (clan == null || kingdom == null) return "";
				if (clan.IsUnderMercenaryService) return "";

				// Only clan leader can make vote deals
				if (npc != clan.Leader)
				{
					Logger.Log("VoteDeal", $"Non-leader deal rejected: npc={npc.StringId} clan={clan.StringId}");
					return "";
				}

				if (!TryResolveVoteDealTarget(npc, agendaCode, optionCode, out VoteDealAgendaEntry agenda, out VoteDealOptionEntry option, out string error))
				{
					Logger.Log("VoteDeal", $"Targeted deal skipped: {error}");
					return "";
				}

				// Proposer clan cannot be persuaded — agenda would be cancelled
				if (agenda.Decision.ProposerClan != null && clan.StringId == agenda.Decision.ProposerClan.StringId)
				{
					Logger.Log("VoteDeal", $"Proposer clan deal rejected: clan={clan.StringId} agenda={agenda.Title}");
					return "";
				}

				if (_activeDeals.Any(d => d.NpcClanStringId == clan.StringId
					&& !d.IsConsumed
					&& (string.IsNullOrWhiteSpace(d.TargetDecisionKey) || DoesVoteDealMatchDecisionKeys(d, agenda.DecisionKey, agenda.DecisionBasicKey))))
				{
					Logger.Log("VoteDeal", $"Duplicate targeted deal skipped: clan={clan.StringId} decision={agenda.Title}");
					return "";
				}

				var record = new VoteDealRecord
				{
					DealId = GenerateDealId(),
					NpcHeroStringId = npc.StringId,
					NpcClanStringId = clan.StringId,
					TargetDecisionKey = agenda.DecisionKey,
					TargetDecisionBasicKey = agenda.DecisionBasicKey,
					TargetDecisionTitle = agenda.Title,
					TargetOptionKey = option.OutcomeKey,
					TargetOptionBasicKey = option.OutcomeBasicKey,
					TargetOptionTitle = option.Title,
					TargetOptionSponsorClanId = option.SponsorClanId,
					SupportWeightValue = (int)weight,
					CreatedDay = CampaignTime.Now.ElapsedDaysUntilNow,
					Notes = notes ?? "",
					IsConsumed = false
				};

				_activeDeals.Add(record);

				string clanName = clan.Name?.ToString() ?? "未知家族";
				AnimusForgeQuickInfo.ShowForDuration(
					$"{clanName}家族 承诺在「{agenda.Title}」中投票选择「{option.Title}」",
					6000, npc.CharacterObject);

				Logger.Log("VoteDeal", $"Targeted vote deal created: id={record.DealId} clan={clanName} agenda={agenda.Code}:{agenda.Title} option={option.Code}:{option.Title} weight={weight}");
				return "";
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ProcessTargetedVoteDealTag Error] {ex.Message}");
				return "";
			}
		}

		// ── AI context builder ─────────────────────────────────────────────

		private static bool IsVoteDealAgendaCode(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();
			return value.Length > 1
				&& (value[0] == 'A' || value[0] == 'a')
				&& int.TryParse(value.Substring(1), out int index)
				&& index > 0;
		}

		private static bool IsLegacySerializedDirection(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			string direction = value.Trim();
			return direction.Equals("SUPPORT", StringComparison.OrdinalIgnoreCase)
				|| direction.Equals("OPPOSE", StringComparison.OrdinalIgnoreCase)
				|| direction.Equals("OPTION", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsVoteDealOptionCode(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();
			return value.Length > 1
				&& (value[0] == 'O' || value[0] == 'o')
				&& int.TryParse(value.Substring(1), out int index)
				&& index > 0;
		}

		private static int ParseVoteDealCodeIndex(string value)
		{
			if (string.IsNullOrWhiteSpace(value) || value.Length < 2) return -1;
			return int.TryParse(value.Trim().Substring(1), out int index) ? index : -1;
		}

		private static string CleanVoteDealText(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return "";
			value = value.Replace(RecordDelimiter, ' ').Replace("\r", " ").Replace("\n", " ").Trim();
			return Regex.Replace(value, @"\s+", " ");
		}

		private static string NormalizeVoteDealKeyPart(string value)
		{
			return CleanVoteDealText(value).ToLowerInvariant();
		}

		private static string BuildVoteDealDecisionKey(KingdomDecision decision, bool includeProposer)
		{
			if (decision == null) return "";
			return string.Join("|",
				decision.GetType().FullName ?? "",
				decision.Kingdom?.StringId ?? "",
				includeProposer ? (decision.ProposerClan?.StringId ?? "") : "",
				NormalizeVoteDealKeyPart(GetDecisionTypeLabel(decision)),
				NormalizeVoteDealKeyPart(decision.GetGeneralTitle()?.ToString() ?? ""));
		}

		private static string BuildVoteDealOutcomeKey(DecisionOutcome outcome, bool includeSponsor)
		{
			if (outcome == null) return "";
			return string.Join("|",
				outcome.GetType().FullName ?? "",
				includeSponsor ? (outcome.SponsorClan?.StringId ?? "") : "",
				NormalizeVoteDealKeyPart(outcome.GetDecisionTitle()?.ToString() ?? ""),
				NormalizeVoteDealKeyPart(outcome.GetDecisionDescription()?.ToString() ?? ""));
		}

		private static bool DoesVoteDealMatchDecisionKeys(VoteDealRecord deal, string decisionKey, string decisionBasicKey)
		{
			if (deal == null) return false;
			return (!string.IsNullOrWhiteSpace(deal.TargetDecisionKey) && string.Equals(deal.TargetDecisionKey, decisionKey, StringComparison.Ordinal))
				|| (!string.IsNullOrWhiteSpace(deal.TargetDecisionBasicKey) && string.Equals(deal.TargetDecisionBasicKey, decisionBasicKey, StringComparison.Ordinal));
		}

		private static bool DoesVoteDealMatchDecision(VoteDealRecord deal, KingdomDecision decision)
		{
			if (deal == null || decision == null) return false;
			return DoesVoteDealMatchDecisionKeys(deal, BuildVoteDealDecisionKey(decision, includeProposer: true), BuildVoteDealDecisionKey(decision, includeProposer: false));
		}

		private static bool DoesVoteDealMatchOutcome(VoteDealRecord deal, DecisionOutcome outcome)
		{
			if (deal == null || outcome == null) return false;
			string outcomeKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: true);
			string outcomeBasicKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: false);
			if (!string.IsNullOrWhiteSpace(deal.TargetOptionKey) && string.Equals(deal.TargetOptionKey, outcomeKey, StringComparison.Ordinal))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(deal.TargetOptionBasicKey) && string.Equals(deal.TargetOptionBasicKey, outcomeBasicKey, StringComparison.Ordinal))
			{
				return true;
			}
			string title = CleanVoteDealText(outcome.GetDecisionTitle()?.ToString() ?? "");
			string sponsorId = outcome.SponsorClan?.StringId ?? "";
			return !string.IsNullOrWhiteSpace(deal.TargetOptionTitle)
				&& string.Equals(CleanVoteDealText(deal.TargetOptionTitle), title, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(deal.TargetOptionSponsorClanId) || string.Equals(deal.TargetOptionSponsorClanId, sponsorId, StringComparison.OrdinalIgnoreCase));
		}

		private static float GetForcedVoteDealSupportScore(int supportWeightValue)
		{
			Supporter.SupportWeights weight = (Supporter.SupportWeights)supportWeightValue;
			switch (weight)
			{
				case Supporter.SupportWeights.FullyPush:
					return 1000f;
				case Supporter.SupportWeights.StronglyFavor:
					return 700f;
				default:
					return 350f;
			}
		}

		internal static bool RequiresPlayerRulerResolution(KingdomDecision decision)
		{
			try
			{
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				return decision != null
					&& !decision.IsEnforced
					&& decision.TriggerTime.IsPast
					&& decision.Kingdom != null
					&& decision.Kingdom == playerKingdom
					&& decision.Kingdom.RulingClan == Clan.PlayerClan;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaRequiredVote] Match error: {ex.Message}");
				return false;
			}
		}

		private static void TryLaunchRequiredAgendaVote(KingdomDecision decision)
		{
			if (!RequiresPlayerRulerResolution(decision)) return;
			if (s_pendingRequiredAgendaDecision != null) return;
			VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
			string promptKey = BuildRequiredAgendaVotePromptKey(decision);
			if (behavior != null && !string.IsNullOrWhiteSpace(promptKey) && behavior._shownRequiredAgendaVoteKeys.ContainsKey(promptKey)) return;

			try
			{
				GameStateManager stateManager = Game.Current?.GameStateManager;
				if (stateManager == null || !(stateManager.ActiveState is MapState)) return;

				if (behavior != null && !string.IsNullOrWhiteSpace(promptKey)) behavior._shownRequiredAgendaVoteKeys[promptKey] = "1";
				s_pendingRequiredAgendaDecision = decision;
				KingdomState kingdomState = stateManager.CreateState<KingdomState>();
				stateManager.PushState(kingdomState, 0);
				InformationManager.DisplayMessage(new InformationMessage(
					$"议程《{GetSafeDecisionTitle(decision)}》期限已到，请作出最终裁决。",
					Color.FromUint(0xFFD166)));
				Logger.Log("VoteDeal", $"[AgendaRequiredVote] Opened required vote: decision={GetSafeDecisionTitle(decision)}");
			}
			catch (Exception ex)
			{
				if (behavior != null && !string.IsNullOrWhiteSpace(promptKey)) behavior._shownRequiredAgendaVoteKeys.Remove(promptKey);
				s_pendingRequiredAgendaDecision = null;
				Logger.Log("VoteDeal", $"[AgendaRequiredVote] Failed to open vote UI for {GetSafeDecisionTitle(decision)}: {ex.Message}");
			}
		}

		private static string BuildRequiredAgendaVotePromptKey(KingdomDecision decision)
		{
			return BuildVoteDealDecisionKey(decision, includeProposer: true);
		}

		private void ForgetRequiredAgendaVotePrompt(KingdomDecision decision)
		{
			string key = BuildRequiredAgendaVotePromptKey(decision);
			if (!string.IsNullOrWhiteSpace(key)) _shownRequiredAgendaVoteKeys?.Remove(key);
		}

		private void CleanupRequiredAgendaVotePromptKeys()
		{
			if (_shownRequiredAgendaVoteKeys == null || _shownRequiredAgendaVoteKeys.Count == 0) return;
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null) return;
			Kingdom playerKingdom = playerClan.Kingdom;
			if (playerKingdom?.UnresolvedDecisions == null)
			{
				_shownRequiredAgendaVoteKeys.Clear();
				return;
			}
			HashSet<string> activeKeys = new HashSet<string>(playerKingdom.UnresolvedDecisions
				.Where(decision => decision != null)
				.Select(BuildRequiredAgendaVotePromptKey)
				.Where(key => !string.IsNullOrWhiteSpace(key)), StringComparer.OrdinalIgnoreCase);
			foreach (string staleKey in _shownRequiredAgendaVoteKeys.Keys.Where(key => !activeKeys.Contains(key)).ToList())
			{
				_shownRequiredAgendaVoteKeys.Remove(staleKey);
			}
		}

		private void NotifyPlayerKingdomAgendasNearingDeadline()
		{
			Kingdom kingdom = Clan.PlayerClan?.Kingdom;
			if (kingdom?.UnresolvedDecisions == null) return;

			List<KingdomDecision> pending = kingdom.UnresolvedDecisions.ToList();
			_agendaDeadlineReminderShown.RemoveWhere(decision => decision == null || !pending.Contains(decision));

			foreach (KingdomDecision decision in pending)
			{
				if (decision == null || decision.IsEnforced || _agendaDeadlineReminderShown.Contains(decision)) continue;
				if (TryGetRedundantSingleClanFiefAgenda(kingdom, decision, out _, out _)) continue;
				if (ShouldCancelDecisionSafe(decision)) continue;

				float remainingHours;
				try
				{
					if (!decision.TriggerTime.IsFuture) continue;
					remainingHours = decision.TriggerTime.RemainingHoursFromNow;
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[AgendaReminder] Failed to read deadline for {GetSafeDecisionTitle(decision)}: {ex.Message}");
					continue;
				}

				if (remainingHours > 24f) continue;
				_agendaDeadlineReminderShown.Add(decision);
				string deadlineAction = kingdom.RulingClan == Clan.PlayerClan
					? "届时你将进入最终投票并作出裁决。"
					: "请及时查看议程并完成投票准备。";
				InformationManager.DisplayMessage(new InformationMessage(
					$"王国议程《{GetSafeDecisionTitle(decision)}》将在一天内结束；{deadlineAction}",
					Color.FromUint(0xFFD700)));
				Logger.Log("VoteDeal", $"[AgendaReminder] One-day reminder shown: kingdom={kingdom.StringId}, decision={GetSafeDecisionTitle(decision)}, remainingHours={remainingHours:F1}");
			}
		}

		private static Supporter.SupportWeights GetForcedVoteDealSupportWeight(int supportWeightValue)
		{
			Supporter.SupportWeights weight = (Supporter.SupportWeights)supportWeightValue;
			if (weight < Supporter.SupportWeights.SlightlyFavor || weight > Supporter.SupportWeights.FullyPush)
			{
				return Supporter.SupportWeights.SlightlyFavor;
			}
			return weight;
		}

		private static List<VoteDealAgendaEntry> BuildVoteDealAgendaEntries(Hero npc)
		{
			List<VoteDealAgendaEntry> result = new List<VoteDealAgendaEntry>();
			try
			{
				Kingdom kingdom = npc?.Clan?.Kingdom;
				if (kingdom?.UnresolvedDecisions == null) return result;
				int agendaIndex = 1;
				foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
				{
					if (decision == null || TryGetRedundantSingleClanFiefAgenda(kingdom, decision, out _, out _) || decision.ShouldBeCancelled()) continue;
					try
					{
						IEnumerable<DecisionOutcome> initialCandidates = decision.DetermineInitialCandidates();
						if (initialCandidates == null) continue;
						MBList<DecisionOutcome> candidateList = new MBList<DecisionOutcome>();
						foreach (DecisionOutcome candidate in initialCandidates)
						{
							if (candidate != null) candidateList.Add(candidate);
						}
						if (candidateList.Count == 0) continue;
						MBList<DecisionOutcome> narrowed = decision.NarrowDownCandidates(candidateList, 3);
						if (narrowed == null || narrowed.Count == 0) continue;
						decision.DetermineSponsors(narrowed);

						VoteDealAgendaEntry agenda = new VoteDealAgendaEntry
						{
							Code = "A" + agendaIndex,
							Decision = decision,
							DecisionKey = BuildVoteDealDecisionKey(decision, includeProposer: true),
							DecisionBasicKey = BuildVoteDealDecisionKey(decision, includeProposer: false),
							TypeLabel = VoteDealBehavior.GetBilateralDiplomacyAgendaTypeText(decision) ?? GetDecisionTypeLabel(decision),
							Title = GetDecisionDisplayTitleForAgenda(decision),
							ProposerName = CleanVoteDealText(VoteDealBehavior.GetBilateralDiplomacyAgendaProposerText(decision) ?? decision.ProposerClan?.Name?.ToString() ?? "未知"),
							RemainingDays = decision.TriggerTime.RemainingDaysFromNow
						};

						int optionIndex = 1;
						foreach (DecisionOutcome outcome in narrowed)
						{
							if (outcome == null) continue;
							agenda.Options.Add(new VoteDealOptionEntry
							{
								Code = "O" + optionIndex,
								Outcome = outcome,
								OutcomeKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: true),
								OutcomeBasicKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: false),
								Title = GetDecisionOutcomeDisplayTitleForAgenda(decision, outcome),
								Description = GetDecisionOutcomeDisplayDescriptionForAgenda(decision, outcome),
								SponsorName = CleanVoteDealText(outcome.SponsorClan?.Name?.ToString() ?? "未知"),
								SponsorClanId = outcome.SponsorClan?.StringId ?? ""
							});
							optionIndex++;
						}
						result.Add(agenda);
						agendaIndex++;
					}
					catch (Exception ex)
					{
						Logger.Log("VoteDeal", $"[BuildAgendaEntries] skipped decision: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildAgendaEntries Error] {ex.Message}");
			}
			return result;
		}

		private static bool TryResolveVoteDealTarget(Hero npc, string agendaCode, string optionCode, out VoteDealAgendaEntry agenda, out VoteDealOptionEntry option, out string error)
		{
			agenda = null;
			option = null;
			error = "";
			if (!IsVoteDealAgendaCode(agendaCode) || !IsVoteDealOptionCode(optionCode))
			{
				error = $"bad target code agenda={agendaCode} option={optionCode}";
				return false;
			}
			List<VoteDealAgendaEntry> agendas = BuildVoteDealAgendaEntries(npc);
			int agendaIndex = ParseVoteDealCodeIndex(agendaCode) - 1;
			if (agendaIndex < 0 || agendaIndex >= agendas.Count)
			{
				error = $"agenda code out of range: {agendaCode}";
				return false;
			}
			agenda = agendas[agendaIndex];
			int optionIndex = ParseVoteDealCodeIndex(optionCode) - 1;
			if (optionIndex < 0 || optionIndex >= agenda.Options.Count)
			{
				error = $"option code out of range: {agendaCode}/{optionCode}";
				return false;
			}
			option = agenda.Options[optionIndex];
			return true;
		}

		public static string BuildPendingDecisionsContext(Hero npc, bool includeAgendaDetails = true)
		{
			try
			{
				if (npc == null) return "";
				Clan clan = npc.Clan;
				Kingdom kingdom = clan?.Kingdom;
				if (kingdom == null) return "";
				if (clan.IsUnderMercenaryService) return "";

				StringBuilder sb = new StringBuilder();

				// Non-leader cannot make vote deals
				if (clan.Leader != null && npc != clan.Leader)
				{
					sb.Append("【投票交易身份限制】你不是家族族长（你的族长是");
					sb.Append(clan.Leader.Name?.ToString() ?? "未知");
					sb.AppendLine("），你不能代表家族做出投票或提案承诺。如果有人找你商议议程，你必须告知对方去找你的族长。");
				}

				// Existing commitments block
				VoteDealBehavior inst = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (inst != null && inst._activeDeals != null)
				{
					string clanId = clan.StringId;
					List<VoteDealRecord> existing = inst._activeDeals
						.Where(d => d.NpcClanStringId == clanId && !d.IsConsumed)
						.ToList();
					if (existing.Count > 0)
					{
						sb.AppendLine("【你已承诺的投票交易】（务必遵守！不可反悔！）");
						foreach (var deal in existing)
						{
							sb.Append(" - 议程:").Append(string.IsNullOrWhiteSpace(deal.TargetDecisionTitle) ? "未知议程" : deal.TargetDecisionTitle);
							sb.Append("，已承诺投票选择:").Append(string.IsNullOrWhiteSpace(deal.TargetOptionTitle) ? "未知选项" : deal.TargetOptionTitle);
							sb.Append("，权重:").Append((Supporter.SupportWeights)deal.SupportWeightValue);
							if (!string.IsNullOrEmpty(deal.Notes))
								sb.Append("，备注:").Append(deal.Notes);
							sb.AppendLine();
						}
					}
				}

				// Kingdom agenda context
				if (includeAgendaDetails && kingdom.UnresolvedDecisions != null && kingdom.UnresolvedDecisions.Count > 0)
				{
					sb.AppendLine();
					sb.AppendLine("【王国当前议程】（正在公示中的提案，公示期结束后将进入投票阶段。你可以按议程名称、城镇/国家/政策名、候选人或家族名理解玩家想拉票的对象；正文只自然说话，不要输出任何系统标签。）");
					List<VoteDealAgendaEntry> agendas = BuildVoteDealAgendaEntries(npc);
					foreach (VoteDealAgendaEntry agenda in agendas)
					{
						bool isOwnProposal = agenda.Decision.ProposerClan?.StringId == clan.StringId;
						string proposerNote = isOwnProposal ? "【你的家族提案，不可交易】" : "";
						if (agenda.RemainingDays > 0)
							sb.AppendLine($" - [{agenda.TypeLabel}] {agenda.Title}（提案人: {agenda.ProposerName}，剩余 {agenda.RemainingDays:F1} 天进入投票）{proposerNote}");
						else
							sb.AppendLine($" - [{agenda.TypeLabel}] {agenda.Title}（提案人: {agenda.ProposerName}，即将进入投票）{proposerNote}");
						foreach (VoteDealOptionEntry option in agenda.Options)
						{
							string sponsorText = string.IsNullOrWhiteSpace(option.SponsorName) || option.SponsorName == "未知" ? "" : $"，赞助/候选:{option.SponsorName}";
							string descriptionText = string.IsNullOrWhiteSpace(option.Description) ? "" : $"，说明:{option.Description}";
							sb.AppendLine($"   - 可投选项: {option.Title}{sponsorText}{descriptionText}");
						}
					}
				}

				return sb.ToString().TrimEnd();
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildPendingDecisionsContext Error] {ex.Message}");
				return "";
			}
		}

		internal static string GetSupportWeightImagePath(Supporter.SupportWeights weight)
		{
			switch (weight)
			{
				case Supporter.SupportWeights.SlightlyFavor: return "SPKingdom\\voter_strength1";
				case Supporter.SupportWeights.StronglyFavor: return "SPKingdom\\voter_strength2";
				case Supporter.SupportWeights.FullyPush: return "SPKingdom\\voter_strength3";
			}
			return string.Empty;
		}

		private static string GetDecisionTypeLabel(KingdomDecision decision)
	{
		if (decision is DeclareWarDecision) return "宣战";
		if (decision is MakePeaceKingdomDecision) return "和平";
		if (decision is KingdomPolicyDecision) return "政策";
		if (decision is SettlementClaimantPreliminaryDecision) return "封地";
		if (decision is SettlementClaimantDecision) return "封地";
		if (decision is KingSelectionKingdomDecision) return "王选";
		if (decision is ExpelClanFromKingdomDecision) return "驱逐";
		if (decision is StartAllianceDecision) return "联盟";
		if (decision is TradeAgreementDecision) return "贸易";
		return "决议";
	}

	public static bool CanUseVoteDealPostprocess(Hero npc)
		{
			try
			{
				Clan clan = npc?.Clan;
				return clan != null && clan.Kingdom != null && !clan.IsUnderMercenaryService;
			}
			catch
			{
				return false;
			}
		}

		private static string BuildRuntimeVoteDealInstruction(Hero npc)
		{
			try
			{
				Clan clan = npc?.Clan;
				Kingdom kingdom = clan?.Kingdom;

				string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(playerName))
				{
					playerName = "玩家";
				}
				var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["playerName"] = playerName,
				};

				string stateKey = "";
				if (kingdom == null)
				{
					stateKey = "no_kingdom";
				}
				else if (clan.IsUnderMercenaryService)
				{
					stateKey = "mercenary";
				}
				else
				{
					VoteDealBehavior inst = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
					bool hasExistingDeal = inst?._activeDeals?.Any(d => d.NpcClanStringId == clan.StringId && !d.IsConsumed) == true;
					if (hasExistingDeal)
					{
						stateKey = "has_existing_deal";
					}
					else if (kingdom.UnresolvedDecisions == null || kingdom.UnresolvedDecisions.Count == 0)
					{
						stateKey = "no_pending";
					}
				}

				var results = new List<string>();
				string proposalGuidance = AIConfigHandler.ResolveRuleRuntimeText("kingdom_agenda", "proposal_guidance", forConstraint: false, tokens);
				if (!string.IsNullOrWhiteSpace(proposalGuidance)) results.Add(proposalGuidance);

				if (!string.IsNullOrWhiteSpace(stateKey))
				{
					string stateTemplate = AIConfigHandler.ResolveRuleRuntimeText("kingdom_agenda", stateKey, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(stateTemplate))
					{
						results.Add(stateTemplate);
					}
					if (stateKey == "no_kingdom" || stateKey == "mercenary")
					{
						return string.Join("\n", results);
					}
				}

				if (kingdom != null && !clan.IsUnderMercenaryService)
				{
					int trustLevelIndex = 6;
					try
					{
						int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(npc) ?? 0;
						trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(trust);
					}
					catch
					{
						trustLevelIndex = 6;
					}

					string trustTemplate = AIConfigHandler.ResolveRuleRuntimeText("kingdom_agenda", "level_" + trustLevelIndex, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(trustTemplate))
					{
						results.Add(trustTemplate);
					}

					if (kingdom.RulingClan?.Leader == npc)
					{
						string kingTemplate = AIConfigHandler.ResolveRuleRuntimeText("kingdom_agenda", "is_king", forConstraint: false, tokens);
						if (!string.IsNullOrWhiteSpace(kingTemplate))
						{
							results.Add(kingTemplate);
						}
					}
				}

				return string.Join("\n", results);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildRuntimeVoteDealInstruction Error] {ex.Message}");
				return "";
			}
		}

		// ── Dispatch entry point (called directly by ShoutBehavior) ─────

		// ── Patch: Inject pending decisions context ───────────────────────

		private static void Patch_BuildContext_Postfix(Hero targetHero, string input, string extraFact,
			string cultureIdOverride, bool hasAnyHero, CharacterObject targetCharacter,
			string kingdomIdOverride, int targetAgentIndex, bool suppressDynamicRuleAndLore,
			bool usePrefetchedLoreContext, string prefetchedLoreContext,
			ref MyBehavior.ShoutPromptContext __result)
		{
			try
			{
				if (__result == null) return;
				Hero contextTarget = targetHero ?? (targetCharacter?.HeroObject);
				bool voteDealRuleInjected = (__result.Extras ?? "").IndexOf("【附加规则:kingdom_agenda】", StringComparison.OrdinalIgnoreCase) >= 0;
				if (voteDealRuleInjected)
				{
					string runtimeInstruction = BuildRuntimeVoteDealInstruction(contextTarget);
					if (!string.IsNullOrWhiteSpace(runtimeInstruction))
					{
						__result.Extras = (__result.Extras ?? "") + "\n" + runtimeInstruction;
					}
				}
				string ctx = voteDealRuleInjected ? BuildPendingDecisionsContext(contextTarget, includeAgendaDetails: false) : "";
				if (!string.IsNullOrEmpty(ctx))
				{
					__result.Extras = (__result.Extras ?? "") + "\n" + ctx;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Patch_BuildContext Error] {ex.Message}");
			}
		}

		// ── Patch: Override NPC vote via DetermineSupport ─

		private static bool Patch_DetermineSupport_Prefix(
			KingdomDecision __instance,
			Clan clan,
			DecisionOutcome possibleOutcome,
			ref float __result)
		{
			try
			{
				if (clan == null || string.IsNullOrEmpty(clan.StringId)) return true;
				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (behavior != null && behavior.TryGetDialogueProposalSupport(__instance, clan, possibleOutcome, out int dialogueWeight, out bool isPromisedOutcome))
				{
					__result = isPromisedOutcome ? GetForcedVoteDealSupportScore(dialogueWeight) : -1000f;
					return false;
				}

				if (IsBilateralDiplomacyCounterpartAgenda(__instance)
					&& string.Equals(clan.StringId, __instance.ProposerClan?.StringId, StringComparison.OrdinalIgnoreCase))
				{
					__result = 0f;
					return false;
				}

				// Safety: proposer clan vote must not be overridden — would cancel the decision
				if (clan.StringId == __instance.ProposerClan?.StringId) return true;

				if (behavior == null) return true;
				if (behavior._activeDeals == null || behavior._activeDeals.Count == 0) return true;

				VoteDealRecord deal = behavior._activeDeals
					.Where(d => d.NpcClanStringId == clan.StringId && !d.IsConsumed && !string.IsNullOrWhiteSpace(d.TargetDecisionKey))
					.FirstOrDefault(d => DoesVoteDealMatchDecision(d, __instance));
				if (deal != null)
				{
					// If the promised option has disappeared from this agenda, do not
					// force every remaining option to zero. Let vanilla handle the
					// changed agenda instead of making the clan abstain.
					if (DoesVoteDealMatchOutcome(deal, possibleOutcome))
					{
						__result = GetForcedVoteDealSupportScore(deal.SupportWeightValue);
						return false;
					}
				}
				return true;

			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Patch_DetermineSupport Error] {ex.Message}");
				return true;
			}
		}

		private static bool Patch_DetermineSupportOption_Prefix(
			KingdomDecision __instance,
			Supporter supporter,
			MBReadOnlyList<DecisionOutcome> possibleOutcomes,
			ref Supporter.SupportWeights supportWeightOfSelectedOutcome,
			ref DecisionOutcome __result)
		{
			try
			{
				Clan clan = supporter?.Clan;
				if (clan == null || string.IsNullOrWhiteSpace(clan.StringId) || __instance == null) return true;
				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (possibleOutcomes != null && behavior?.IsDialogueProposedDecision(__instance) == true && __instance.ProposerClan == clan)
				{
					DecisionOutcome dialoguePromisedOutcome = possibleOutcomes.FirstOrDefault(outcome =>
						outcome != null && behavior.TryGetDialogueProposalSupport(__instance, clan, outcome, out _, out bool promised) && promised);
					if (dialoguePromisedOutcome != null && behavior.TryGetDialogueProposalSupport(__instance, clan, dialoguePromisedOutcome, out int dialogueWeight, out _))
					{
						supportWeightOfSelectedOutcome = GetForcedVoteDealSupportWeight(dialogueWeight);
						__result = dialoguePromisedOutcome;
						Logger.Log("ProposeAgenda", "Forced proposer support decision=" + GetSafeDecisionTitle(__instance) + " clan=" + clan.StringId + " weight=" + supportWeightOfSelectedOutcome);
						return false;
					}
				}

				// Proposer votes participate in the decision's cancellation rules and
				// must retain vanilla behavior, just as in the score hook above.
				if (string.Equals(clan.StringId, __instance.ProposerClan?.StringId, StringComparison.OrdinalIgnoreCase)) return true;

				if (behavior?._activeDeals == null || behavior._activeDeals.Count == 0 || possibleOutcomes == null) return true;

				VoteDealRecord deal = behavior._activeDeals
					.Where(d => d != null
						&& !d.IsConsumed
						&& string.Equals(d.NpcClanStringId, clan.StringId, StringComparison.OrdinalIgnoreCase)
						&& !string.IsNullOrWhiteSpace(d.TargetDecisionKey))
					.FirstOrDefault(d => DoesVoteDealMatchDecision(d, __instance));
				if (deal == null) return true;

				DecisionOutcome promisedOutcome = possibleOutcomes.FirstOrDefault(outcome => DoesVoteDealMatchOutcome(deal, outcome));
				if (promisedOutcome == null)
				{
					Logger.Log("VoteDeal", $"[VoteFulfillment] Promised option unavailable; using vanilla vote. deal={deal.DealId} clan={clan.StringId} decision={GetSafeDecisionTitle(__instance)} promised={deal.TargetOptionTitle}");
					return true;
				}

				supportWeightOfSelectedOutcome = GetForcedVoteDealSupportWeight(deal.SupportWeightValue);
				__result = promisedOutcome;
				Logger.Log("VoteDeal", $"[VoteFulfillment] Forced promised vote. deal={deal.DealId} clan={clan.StringId} decision={GetSafeDecisionTitle(__instance)} option={promisedOutcome.GetDecisionTitle()} weight={supportWeightOfSelectedOutcome}");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Patch_DetermineSupportOption Error] {ex.Message}");
				return true;
			}
		}

		// ── Patch: 21-day delay for player kingdom decisions ──

		private static void Patch_AddDecision_Delay_Postfix(KingdomDecision kingdomDecision)
		{
			try
			{
				if (kingdomDecision == null) return;
				Kingdom kingdom = kingdomDecision.Kingdom;
				if (TryGetRedundantSingleClanFiefAgenda(kingdom, kingdomDecision, out _, out _)) return;
				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (kingdomDecision.TriggerTime.IsPast)
				{
					behavior?.TryPublishPendingAgendaMapNotifications();
					return;
				}
				if (kingdom == null) return;
				float delayDays = (kingdom == Clan.PlayerClan?.Kingdom) ? 21f : 3f;
				RegisterAgendaDelaySnapshot(kingdomDecision, kingdom, delayDays);
				behavior?.TryPublishPendingAgendaMapNotifications();
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Delay] Error: {ex.Message}");
			}
		}

		private static void Patch_KingdomDecision_NeedsPlayerResolution_Postfix(
			KingdomDecision __instance, ref bool __result)
		{
			try
			{
				if (!__result) return;
				if (!ShouldLetSuppressedKingdomDecisionAutoResolveWithoutPlayer(__instance)) return;

				__result = false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoResolve] NeedsPlayerResolution error: {ex.Message}");
			}
		}

		private static bool Patch_NewMapNoticeAdded_SuppressKingdomVoteReminder_Prefix(InformationData informationData)
		{
			try
			{
				if (informationData is KingdomDecisionMapNotification notification &&
					ShouldSuppressOriginalKingdomVoteReminder(notification.Decision))
				{
					// Keep the native policy-proposal notice for the whole delayed agenda.
					// Its remove command only dismisses the notice; the separate
					// HandleDecision guard below still prevents it from opening a vote.
					if (notification.Decision is KingdomPolicyDecision)
					{
						Logger.Log("VoteDeal", "[ReminderRestore] Kept 21-day policy proposal map reminder.");
						return true;
					}

					Logger.Log("VoteDeal", "[ReminderSuppress] Hidden original map kingdom vote reminder.");
					return false;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] NewMapNoticeAdded error: {ex.Message}");
			}
			return true;
		}

		private static bool Patch_KingdomDecisionMapNotification_IsValid_Prefix(
			KingdomDecisionMapNotification __instance, ref bool __result)
		{
			try
			{
				if (__instance != null && ShouldSuppressOriginalKingdomVoteReminder(__instance.Decision))
				{
					if (__instance.Decision is KingdomPolicyDecision)
					{
						// Let the native validity check keep the proposal notice alive until
						// the decision is concluded/cancelled (normally at the 21-day deadline).
						return true;
					}

					__result = false;
					return false;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] Map notification validity error: {ex.Message}");
			}
			return true;
		}

		private static bool Patch_KingdomDecisionsVM_HandleDecision_SuppressVoteInquiry_Prefix(
			KingdomDecisionsVM __instance, KingdomDecision curDecision)
		{
			bool shouldSuppress = false;
			try
			{
				shouldSuppress = ShouldSuppressOriginalKingdomVoteReminder(curDecision);
				if (!shouldSuppress) return true;

				try
				{
					MarkDecisionExaminedWithoutInquiry(__instance, curDecision);
				}
				catch (Exception markEx)
				{
					// Once matched, never fall through to vanilla: its inquiry can bypass
					// the 21-day agenda even if another UI mod changes private VM fields.
					Logger.Log("VoteDeal", $"[ReminderSuppress] Mark examined error: {markEx.Message}");
				}
				if (curDecision is KingdomPolicyDecision)
				{
					try
					{
						if (KingdomAgendaTabState.Select(__instance))
						{
							Logger.Log("VoteDeal", "[ReminderRestore] Policy proposal reminder opened the agenda tab.");
						}
						else
						{
							Logger.Log("VoteDeal", "[ReminderRestore] WARNING: Agenda tab owner was not registered for the policy reminder.");
						}
					}
					catch (Exception agendaEx)
					{
						// Navigation is best-effort, but the original early-vote inquiry must
						// remain suppressed even if another UI mod breaks tab selection.
						Logger.Log("VoteDeal", $"[ReminderRestore] Agenda navigation error: {agendaEx.Message}");
					}
				}
				Logger.Log("VoteDeal", "[ReminderSuppress] Hidden original kingdom vote inquiry.");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] HandleDecision error: {ex.Message}");
				return !shouldSuppress;
			}
		}

		private static void Patch_KingdomVoteNotificationItemVM_OnFinalize_Postfix(
			KingdomVoteNotificationItemVM __instance)
		{
			if (__instance == null) return;

			try
			{
				// Vanilla only clears OnClanChangedKingdomEvent here. Clear the two
				// decision listeners as well so dismissing X cannot leave a stale VM
				// alive until the delayed proposal concludes up to 21 days later.
				CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(__instance);
				CampaignEvents.KingdomDecisionCancelled.ClearListeners(__instance);
				CampaignEvents.KingdomDecisionConcluded.ClearListeners(__instance);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderRestore] Notification listener cleanup error: {ex.Message}");
			}
		}

		private static void Patch_KingdomDecisionsVM_OnFrameTick_MarkAgendaDecisionsExamined_Prefix(object __instance)
		{
			try
			{
				if (__instance == null) return;
				Kingdom kingdom = Clan.PlayerClan?.Kingdom;
				if (kingdom == null) return;

				foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
				{
					if (ShouldSuppressOriginalKingdomVoteReminder(decision))
					{
						MarkDecisionExaminedWithoutInquiry(__instance, decision);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] OnFrameTick mark error: {ex.Message}");
			}
		}

		private static bool ShouldSuppressOriginalKingdomVoteReminder(KingdomDecision decision)
		{
			try
			{
				if (decision == null) return false;
				if (decision.IsEnforced) return false;
				if (!decision.IsPlayerParticipant && !RequiresPlayerRulerResolution(decision)) return false;
				if (decision.Kingdom == null || decision.Kingdom != Clan.PlayerClan?.Kingdom) return false;
				if (decision.ShouldBeCancelled()) return false;
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] Match error: {ex.Message}");
				return false;
			}
		}

		private static bool ShouldLetSuppressedKingdomDecisionAutoResolveWithoutPlayer(KingdomDecision decision)
		{
			try
			{
				if (decision == null) return false;
				if (!decision.TriggerTime.IsPast) return false;
				return ShouldSuppressOriginalKingdomVoteReminder(decision);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaAutoResolve] Match error: {ex.Message}");
				return false;
			}
		}

		private static void MarkDecisionExaminedWithoutInquiry(object kingdomDecisionsVm, KingdomDecision decision)
		{
			if (kingdomDecisionsVm == null || decision == null) return;

			Traverse traverse = Traverse.Create(kingdomDecisionsVm);
			List<KingdomDecision> examined = traverse.Field("_examinedDecisionsSinceInit")
				.GetValue<List<KingdomDecision>>();
			if (examined != null && !examined.Contains(decision))
			{
				examined.Add(decision);
			}

			traverse.Property("_shouldCheckForDecision").SetValue(true);
			traverse.Field("_queryData").SetValue(null);
		}

		// ── Patch: Block ForceDecideDecision when TriggerTime is future ──

		private static bool Patch_ForceDecideDecision_Block_Prefix(
			object __instance, KingdomDecision decision)
		{
			try
			{
				if (decision == null) return true;
				if (!decision.TriggerTime.IsFuture) return true;
				if (decision.Kingdom != Clan.PlayerClan?.Kingdom) return true;

				float remainingDays = decision.TriggerTime.RemainingDaysFromNow;
				InformationManager.DisplayMessage(new InformationMessage(
					$"提案「{decision.GetGeneralTitle()}」将在 {remainingDays:F1} 天后开始投票",
					Color.FromUint(0xFFD700)));
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ForceDecideBlock] Error: {ex.Message}");
				return true;
			}
		}

		}

		// ════════════════════════════════════════════════════════════════════════
		//  Agenda UI: PrefabExtensions + ViewModels (property injection via KingdomAgendaPatch.cs)
		// ════════════════════════════════════════════════════════════════════════

		[PrefabExtension("KingdomManagement", "descendant::ButtonWidget[@Id='ArmiesTabButton']")]
		internal sealed class KingdomAgendaTabButtonPatch : PrefabExtensionInsertPatch
		{
			private readonly XmlDocument _document;

			public override InsertType Type => (InsertType)4;

			public KingdomAgendaTabButtonPatch()
			{
				_document = new XmlDocument();
				_document.LoadXml(@"
					<ButtonWidget Id='AgendaTabButton' IsSelected='@IsAgendaSelected' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='!Header.Tab.Center.Width.Scaled' SuggestedHeight='!Header.Tab.Center.Height.Scaled' VerticalAlignment='Center' PositionYOffset='2' Brush='Header.Tab.Center' Command.Click='ExecuteShowAgenda' UpdateChildrenStates='true'>
					  <Children>
					    <TextWidget DataSource='{..}' WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' HorizontalAlignment='Center' VerticalAlignment='Center' Brush='Clan.TabControl.Text' Text='@AgendaTabText' />
					  </Children>
					</ButtonWidget>");
			}

			[PrefabExtensionXmlDocument(false)]
			public XmlDocument GetPrefabExtension()
			{
				return _document;
			}
		}

	[PrefabExtension("KingdomManagement", "descendant::DiplomacyPanel[@Id='DiplomacyPanel']")]
	internal sealed class KingdomAgendaPanelPatch : PrefabExtensionInsertPatch
	{
		private readonly XmlDocument _document;

		public override InsertType Type => (InsertType)4;

		public KingdomAgendaPanelPatch()
		{
			_document = new XmlDocument();
									_document.LoadXml(@"
				<Widget Id='AgendaPanelRoot' IsVisible='@IsAgendaSelected' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginTop='188' MarginBottom='75'>
				  <Children>
				    <Widget Id='AgendaPanel' DataSource='{Agenda}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				      <Children>
				        <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				          <Children>
				            <BrushListPanel WidthSizePolicy='Fixed' SuggestedWidth='585' HeightSizePolicy='StretchToParent' VerticalAlignment='Bottom' MarginLeft='5' MarginTop='6' MarginBottom='9' Brush='Frame1Brush' StackLayout.LayoutMethod='VerticalBottomToTop'>
				              <Children>
				                <ListPanel WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' RenderLate='true'>
				                  <Children>
				                    <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='585' SuggestedHeight='60' Sprite='SPKingdom\header_policies' ExtendTop='21' ExtendRight='13' ExtendBottom='20' RenderLate='true'>
				                      <Children>
				                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesCollapserTitle.Text' MarginBottom='8' IsDisabled='true' Text='活跃议程' />
				                      </Children>
				                    </Widget>
				                    <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='23' SuggestedHeight='60' Sprite='StdAssets\scroll_header' ExtendRight='3' ExtendTop='6' ExtendLeft='3' ExtendBottom='4' HorizontalAlignment='Right' />
				                  </Children>
				                </ListPanel>

						<!-- Kingdom selector bar -->
						<BrushWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='72' MarginLeft='3' MarginRight='0' MarginTop='2' MarginBottom='4' Brush='Clan.Item.Tuple'>
						  <Children>
						    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='8' MarginRight='8'>
						      <Children>
						        <ButtonWidget IsVisible='@HasPrevKingdom' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='32' SuggestedHeight='32' VerticalAlignment='Center' Command.Click='ExecutePrevKingdom' UpdateChildrenStates='true'>
						          <Children>
						            <BrushWidget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='28' SuggestedHeight='28' Brush='ButtonRightBigArrowBrush1' HorizontalAlignment='Center' VerticalAlignment='Center' />
						          </Children>
						        </ButtonWidget>
						        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' HorizontalAlignment='Center' VerticalAlignment='Center' Brush='Kingdom.PoliciesItem.Text' Text='@KingdomSelectorName' />
						        <ButtonWidget IsVisible='@HasNextKingdom' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='32' SuggestedHeight='32' VerticalAlignment='Center' Command.Click='ExecuteNextKingdom' UpdateChildrenStates='true'>
						          <Children>
						            <BrushWidget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='28' SuggestedHeight='28' Brush='ButtonLeftBigArrowBrush1' HorizontalAlignment='Center' VerticalAlignment='Center' />
						          </Children>
						        </ButtonWidget>
						      </Children>
						    </ListPanel>
						  </Children>
						</BrushWidget>

				                <Widget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent'>
				                  <Children>
				                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginLeft='24' MarginRight='24' Brush='Popup.Description.Text' Brush.TextHorizontalAlignment='Center' Text='暂无进行中的决议' IsVisible='@!HasItems' DoNotAcceptEvents='true' />

				                    <ScrollablePanel WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' MarginLeft='0' MarginBottom='10' AutoHideScrollBars='true' ClipRect='AgendaClipRect' InnerPanel='AgendaClipRect\AgendaInnerPanel' VerticalScrollbar='..\AgendaScrollbar\Scrollbar' IsVisible='@HasItems'>
				                      <Children>
				                        <Widget Id='AgendaClipRect' WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' ClipContents='true'>
				                          <Children>
				                            <NavigatableListPanel Id='AgendaInnerPanel' DataSource='{AgendaItems}' WidthSizePolicy='Fixed' SuggestedWidth='585' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop' MinIndex='0' StepSize='1000'>
				                              <ItemTemplate>
				                                <ButtonWidget DoNotPassEventsToChildren='true' WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='85' Brush='Kingdom.Policy.Active.Tuple' Command.Click='ExecuteSelect' UpdateChildrenStates='true' IsSelected='@IsSelected'>
				                                  <Children>
				                                    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='VerticalBottomToTop' MarginLeft='12' MarginRight='12'>
				                                      <Children>
				                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='35' VerticalAlignment='Center' Brush='Kingdom.PoliciesItem.Text' Text='@TitleText' ClipContents='false' />
				                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='20' HorizontalAlignment='Center' VerticalAlignment='Center' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.TextHorizontalAlignment='Center' Text='@DaysRemainingText' />
				                                        <ListPanel WidthSizePolicy='CoverChildren' HeightSizePolicy='Fixed' SuggestedHeight='20' StackLayout.LayoutMethod='HorizontalLeftToRight' MarginTop='2'>
				                                          <Children>
				                                            <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='44' SuggestedHeight='20' HorizontalAlignment='Left' VerticalAlignment='Center' Sprite='BlankWhiteSquare_9' Color='#8A6E3EFF' AlphaFactor='0.18'>
				                                              <Children>
				                                                <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Popup.Description.Text' Brush.FontSize='12' Brush.TextHorizontalAlignment='Center' Text='@DecisionTypeText' />
				                                              </Children>
				                                            </Widget>
				                                          </Children>
				                                        </ListPanel>
				                                      </Children>
				                                    </ListPanel>
				                                  </Children>
				                                </ButtonWidget>
				                              </ItemTemplate>
				                            </NavigatableListPanel>
				                          </Children>
				                        </Widget>
				                      </Children>
				                    </ScrollablePanel>
				                    <Standard.VerticalScrollbar Id='AgendaScrollbar' WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' HorizontalAlignment='Left' MarginRight='2' MarginLeft='2' MarginBottom='10' />
				                  </Children>
				                </Widget>
				              </Children>
				            </BrushListPanel>

				<Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='8' MarginRight='8' MarginTop='8' Sprite='BlankWhiteSquare_9' Color='#C8A070FF' AlphaFactor='0.15'>
				  <Children>
				    <RichTextWidget IsVisible='@!HasItems' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginTop='80' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='24' Text='暂无进行中的议程' DoNotAcceptEvents='true' />
				    <Widget IsVisible='@HasItems' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren'>
				      <Children>
				        <RichTextWidget IsVisible='@!HasSelectedItem' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginTop='80' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='22' Text='选择一个议程条目查看详情' DoNotAcceptEvents='true' />
				      </Children>
				    </Widget>
				    <Widget IsVisible='@HasSelectedItem' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				      <Children>
				        <ScrollablePanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' AutoHideScrollBars='true' ClipRect='DetailClipRect2' InnerPanel='DetailClipRect2\DetailInnerPanel2'>
				          <Children>
				            <Widget Id='DetailClipRect2' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ClipContents='true'>
				              <Children>
				                <ListPanel Id='DetailInnerPanel2' DataSource='{SelectedItem}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop' MarginRight='5'>
				                  <Children>
				                    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='54' MarginTop='4' Brush='Kingdom.DecisionTitleBig.Text' Text='@TitleText' Brush.FontSize='46' />
				                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='4' Brush='Popup.Description.Text' Brush.FontSize='18' Text='@DaysRemainingText' />
				                    <ButtonWidget IsVisible='@CanCallVoteMeeting' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='260' SuggestedHeight='36' HorizontalAlignment='Center' MarginTop='10' Brush='ButtonBrush2' Command.Click='ExecuteCallVoteMeeting' UpdateChildrenStates='true'>
				                      <Children>
				                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.GeneralButtons.Text' Text='召开投票会议' DoNotAcceptEvents='true' />
				                      </Children>
				                    </ButtonWidget>
				                    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='2' Sprite='SPKingdom\Diplomacy\divider_left' MarginTop='10' MarginBottom='10' AlphaFactor='0.5' />
				                    <Widget IsVisible='@HasDetail' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				                      <Children>
				                        <ListPanel DataSource='{Options}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='HorizontalLeftToRight'>
				                          <ItemTemplate>
				                            <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginRight='6'>
				                              <Children>
				                                <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#F0D9A0FF' AlphaFactor='0.25' />
				                                <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='VerticalBottomToTop' MarginLeft='14' MarginRight='14' MarginTop='14' MarginBottom='14'>
				                                  <Children>
				                                    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='48' StackLayout.LayoutMethod='HorizontalLeftToRight'>
					                                      <Children>
					                                        <Widget DataSource='{SponsorVisual}' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='42' SuggestedHeight='42' VerticalAlignment='Center'>
					                                          <Children>
					                                            <ImageIdentifierWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' />
					                                          </Children>
					                                        </Widget>
					                                        <Widget IsVisible='@HasSponsorBanner' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='28' SuggestedHeight='28' VerticalAlignment='Center' MarginLeft='8'>
					                                          <Children>
					                                            <ImageIdentifierWidget DataSource='{SponsorBanner}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' />
					                                          </Children>
					                                        </Widget>
					                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' VerticalAlignment='Center' MarginLeft='8' Brush='Popup.Description.Text' Brush.FontSize='17' Text='@SponsorName' />
					                                      </Children>
					                                    </ListPanel>
					                                    <ListPanel DataSource='{Supporters}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop' MarginTop='8'>
				                                      <ItemTemplate>
				                                        <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='36' StackLayout.LayoutMethod='HorizontalLeftToRight'>
					                                          <Children>
					                                            <Widget DataSource='{Visual}' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='24' SuggestedHeight='24' VerticalAlignment='Center'>
					                                              <Children>
					                                                <ImageIdentifierWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' />
					                                              </Children>
					                                            </Widget>
					                                            <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='20' SuggestedHeight='20' Sprite='@SupportWeightImagePath' VerticalAlignment='Center' DoNotAcceptEvents='true' MarginLeft='4' />
				                                            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' VerticalAlignment='Center' MarginLeft='6' Brush='Popup.Description.Text' Brush.FontSize='14' Text='@Name' />
				                                            <RichTextWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' VerticalAlignment='Center' MarginLeft='6' Brush='Popup.Description.Text' Brush.FontSize='13' Text='@WeightText' />
				                                          </Children>
				                                        </ListPanel>
				                                      </ItemTemplate>
				                                    </ListPanel>
				                                    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='32' MarginTop='12' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='22' Text='@Name' />
				                                    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='36' MarginTop='10' Sprite='BlankWhiteSquare_9' Color='#000000FF' AlphaFactor='0.12'>
				                                      <Children>
				                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.FontSize='18' Brush.TextHorizontalAlignment='Center' VerticalAlignment='Center' IntText='@SupportPercentage' />
				                                      </Children>
				                                    </Widget>
				                                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='10' Brush='Popup.Description.Text' Brush.FontSize='16' Text='@Description' />
				                                  </Children>
				                                </ListPanel>
				                              </Children>
				                            </Widget>
				                          </ItemTemplate>
				                        </ListPanel>
				                      </Children>
				                    </Widget>
				                    <RichTextWidget IsVisible='@!HasDetail' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginTop='40' Brush='Popup.Description.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='18' Text='此议程暂无投票详情' DoNotAcceptEvents='true' />
				                  </Children>
				                </ListPanel>
				              </Children>
				            </Widget>
				          </Children>
				        </ScrollablePanel>
				      </Children>
				    </Widget>
				  </Children>
				</Widget>
				          </Children>
				        </ListPanel>
				      </Children>
				    </Widget>
				  </Children>
				</Widget>");
#if BANNERLORD_1_4_OR_GREATER
			foreach (XmlElement element in _document.SelectNodes("//*[@StackLayout.LayoutMethod='VerticalBottomToTop']"))
			{
				element.SetAttribute("StackLayout.LayoutMethod", "VerticalTopToBottom");
			}
#endif
		}

		[PrefabExtensionXmlDocument(false)]
		public XmlDocument GetPrefabExtension()
		{
			return _document;
		}
	}

	[PrefabExtension("KingdomManagement", "descendant::Constant[@Name='Header.Tab.Center.Width.Scaled']")]
	internal sealed class KingdomAgendaScalingPatch : PrefabExtensionSetAttributePatch
	{
		public override List<Attribute> Attributes => new List<Attribute>
		{
			new Attribute("MultiplyResult", "0.50")
		};
	}

		public class KingdomAgendaVM : ViewModel
		{
			public sealed class Snapshot
			{
				public KingdomDecision Decision;
				public string KingdomId;
				public CampaignTime CreatedAt;
				public float DelayDays;
				public bool IsPlayerKingdom;
			}

			internal static readonly List<Snapshot> _snapshots = new List<Snapshot>();
			private MBBindingList<KingdomAgendaItemVM> _agendaItems;
			private bool _hasItems;
			private KingdomAgendaItemVM _selectedItem;
			private Kingdom _targetKingdom;
			private string _currentKingdomName;
			private bool _isPlayerKingdom;
			private List<Kingdom> _activeKingdoms;
			private int _currentKingdomIndex;
			internal Action<KingdomDecision> CallVoteMeetingRequested;

			[DataSourceProperty]
			public MBBindingList<KingdomAgendaItemVM> AgendaItems
			{
				get => _agendaItems;
				set { if (_agendaItems != value) { _agendaItems = value; OnPropertyChanged("AgendaItems"); } }
			}

			[DataSourceProperty]
			public bool HasItems
			{
				get => _hasItems;
				set { if (_hasItems != value) { _hasItems = value; OnPropertyChanged("HasItems"); } }
			}

			[DataSourceProperty]
			public KingdomAgendaItemVM SelectedItem
			{
				get => _selectedItem;
				set { if (_selectedItem != value) { _selectedItem = value; OnPropertyChanged("SelectedItem"); OnPropertyChanged("HasSelectedItem"); } }
			}

			[DataSourceProperty]
			public bool HasSelectedItem => SelectedItem != null;

			[DataSourceProperty]
			public string CurrentKingdomName
			{
				get => _currentKingdomName;
				set { if (_currentKingdomName != value) { _currentKingdomName = value; OnPropertyChanged("CurrentKingdomName"); } }
			}

			[DataSourceProperty]
			public bool IsPlayerKingdom
			{
				get => _isPlayerKingdom;
				set { if (_isPlayerKingdom != value) { _isPlayerKingdom = value; OnPropertyChanged("IsPlayerKingdom"); } }
			}

			[DataSourceProperty]
			private string _kingdomSelectorName;
			[DataSourceProperty]
			public string KingdomSelectorName
			{
				get => _kingdomSelectorName;
				set { if (_kingdomSelectorName != value) { _kingdomSelectorName = value; OnPropertyChanged("KingdomSelectorName"); } }
			}

			[DataSourceProperty]
			private bool _hasPrevKingdom;
			[DataSourceProperty]
			public bool HasPrevKingdom
			{
				get => _hasPrevKingdom;
				set { if (_hasPrevKingdom != value) { _hasPrevKingdom = value; OnPropertyChanged("HasPrevKingdom"); } }
			}

			[DataSourceProperty]
			private bool _hasNextKingdom;
			[DataSourceProperty]
			public bool HasNextKingdom
			{
				get => _hasNextKingdom;
				set { if (_hasNextKingdom != value) { _hasNextKingdom = value; OnPropertyChanged("HasNextKingdom"); } }
			}

			public Kingdom TargetKingdom => _targetKingdom ?? Clan.PlayerClan?.Kingdom;

			public KingdomAgendaVM()
			{
				AgendaItems = new MBBindingList<KingdomAgendaItemVM>();
			}

			public void SetTargetKingdom(Kingdom kingdom)
			{
				_targetKingdom = kingdom;
				Kingdom active = TargetKingdom;
				CurrentKingdomName = (active?.Name?.ToString() ?? "未知王国") + " 议程";
				IsPlayerKingdom = active == Clan.PlayerClan?.Kingdom;
				UpdateKingdomSelector();
				ClearSelection();
				RefreshAgendaItems();
			}

			public void RefreshKingdomList()
			{
				_activeKingdoms = new List<Kingdom>();
				foreach (Kingdom k in Kingdom.All)
					if (!k.IsEliminated) _activeKingdoms.Add(k);
				UpdateKingdomSelector();
			}

			public void SetDefaultKingdom()
			{
				Kingdom pk = Clan.PlayerClan?.Kingdom;
				if (pk != null) { int idx = _activeKingdoms.FindIndex(k => k == pk); if (idx >= 0) _currentKingdomIndex = idx; }
				SetTargetKingdom(_activeKingdoms.Count > 0 && _currentKingdomIndex < _activeKingdoms.Count ? _activeKingdoms[_currentKingdomIndex] : null);
			}

			private void UpdateKingdomSelector()
			{
				if (_activeKingdoms == null || _activeKingdoms.Count == 0)
				{
					KingdomSelectorName = "无王国"; HasPrevKingdom = false; HasNextKingdom = false;
					return;
				}
				HasPrevKingdom = _activeKingdoms.Count > 1; HasNextKingdom = _activeKingdoms.Count > 1;
				if (_currentKingdomIndex < 0) _currentKingdomIndex = 0;
				if (_currentKingdomIndex >= _activeKingdoms.Count) _currentKingdomIndex = _activeKingdoms.Count - 1;
				KingdomSelectorName = _activeKingdoms[_currentKingdomIndex]?.Name?.ToString() ?? "未知";
			}

			[DataSourceMethod]
			public void ExecutePrevKingdom()
			{
				int count = _activeKingdoms?.Count ?? 0;
				if (count < 2) return;
				_currentKingdomIndex = (_currentKingdomIndex - 1 + count) % count;
				UpdateKingdomSelector();
				SetTargetKingdom(_activeKingdoms[_currentKingdomIndex]);
			}

			[DataSourceMethod]
			public void ExecuteNextKingdom()
			{
				int count = _activeKingdoms?.Count ?? 0;
				if (count < 2) return;
				_currentKingdomIndex = (_currentKingdomIndex + 1) % count;
				UpdateKingdomSelector();
				SetTargetKingdom(_activeKingdoms[_currentKingdomIndex]);
			}

			public void RefreshAgendaItems()
			{
				try
				{
					AgendaItems.Clear();
					Kingdom kingdom = TargetKingdom;
					if (kingdom == null) { HasItems = false; return; }

					bool isPlayerKd = kingdom == Clan.PlayerClan?.Kingdom;
					if (isPlayerKd)
					{
						foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
						{
							if (VoteDealBehavior.TryGetRedundantSingleClanFiefAgenda(kingdom, decision, out _, out _) || decision.ShouldBeCancelled()) continue;
							KingdomAgendaItemVM item = new KingdomAgendaItemVM(decision);
							item.Parent = this;
							AgendaItems.Add(item);
						}
					}
					else
					{
						CleanupSnapshots();
						HashSet<KingdomDecision> seen = new HashSet<KingdomDecision>();

						// 1. From live UnresolvedDecisions (survives save/load)
						foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
						{
							if (VoteDealBehavior.TryGetRedundantSingleClanFiefAgenda(kingdom, decision, out _, out _) || decision.ShouldBeCancelled()) continue;
							seen.Add(decision);
							KingdomAgendaItemVM item = new KingdomAgendaItemVM(decision);
							item.Parent = this;
							AgendaItems.Add(item);
						}

						// 2. From snapshots for decisions that are still truly pending.
						List<Snapshot> snaps;
						lock (_snapshots)
						{
							snaps = _snapshots
								.Where(s => s.Decision != null
									&& !seen.Contains(s.Decision)
									&& VoteDealBehavior.IsDecisionPendingInKingdom(s.Decision, kingdom)
									&& string.Equals(s.KingdomId, kingdom.StringId, StringComparison.OrdinalIgnoreCase))
								.ToList();
						}
						foreach (Snapshot snap in snaps)
						{
							KingdomAgendaItemVM item = new KingdomAgendaItemVM(snap.Decision);
							item.Parent = this;
							AgendaItems.Add(item);
						}
					}

					HasItems = AgendaItems.Count > 0;
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[AgendaVM] RefreshAgendaItems error: {ex.Message}");
					HasItems = false;
				}
			}

			private static void CleanupSnapshots()
			{
				try
				{
					lock (_snapshots)
					{
						_snapshots.RemoveAll(s =>
							s.Decision == null
							|| s.CreatedAt.ElapsedDaysUntilNow > s.DelayDays + 1f
							|| !VoteDealBehavior.IsDecisionPendingInKingdom(s.Decision, VoteDealBehavior.ResolveKingdom(s.KingdomId)));
					}
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[Snapshot] Cleanup error: {ex.Message}");
				}
			}

		public void SelectItem(KingdomAgendaItemVM item)
		{
			if (SelectedItem != null)
				SelectedItem.IsSelected = false;

			if (item != null)
			{
				item.IsSelected = true;
				item.LoadDetail();
			}

			SelectedItem = item;
		}

		internal bool TrySelectDecision(KingdomDecision decision)
		{
			if (decision == null) return false;
			KingdomAgendaItemVM item = AgendaItems.FirstOrDefault(x => x?.Decision == decision);
			if (item == null) return false;
			SelectItem(item);
			return true;
		}

		internal void CallVoteMeeting(KingdomAgendaItemVM item)
		{
			KingdomDecision decision = item?.Decision;
			if (decision == null) return;

			CallVoteMeetingRequested?.Invoke(decision);
			item.RefreshTimingState();
		}

		public void ClearSelection()
		{
			if (SelectedItem != null)
			{
				SelectedItem.IsSelected = false;
				SelectedItem = null;
			}
		}
	}

	public class KingdomAgendaItemVM : ViewModel
	{
		private string _titleText;
		private string _decisionTypeText;
		private string _daysRemainingText;
		private bool _isUrgent;
		private bool _isSelected;
		private bool _canCallVoteMeeting;
		private KingdomDecision _decision;
		internal KingdomAgendaVM Parent;
		private MBBindingList<AgendaOptionVM> _options;
		private bool _hasDetail;

		[DataSourceProperty]
		public string TitleText
		{
			get => _titleText;
			set { _titleText = value; OnPropertyChanged("TitleText"); }
		}

		[DataSourceProperty]
		public string DecisionTypeText
		{
			get => _decisionTypeText;
			set { _decisionTypeText = value; OnPropertyChanged("DecisionTypeText"); }
		}

		[DataSourceProperty]
		public string DaysRemainingText
		{
			get => _daysRemainingText;
			set { _daysRemainingText = value; OnPropertyChanged("DaysRemainingText"); }
		}

		[DataSourceProperty]
		public bool IsUrgent
		{
			get => _isUrgent;
			set { _isUrgent = value; OnPropertyChanged("IsUrgent"); }
		}

		[DataSourceProperty]
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					OnPropertyChanged("IsSelected");
				}
			}
		}

		public KingdomDecision Decision => _decision;

		[DataSourceProperty]
		public bool CanCallVoteMeeting
		{
			get => _canCallVoteMeeting;
			set
			{
				if (_canCallVoteMeeting != value)
				{
					_canCallVoteMeeting = value;
					OnPropertyChanged("CanCallVoteMeeting");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<AgendaOptionVM> Options
		{
			get => _options;
			set
			{
				if (_options != value)
				{
					_options = value;
					OnPropertyChanged("Options");
				}
			}
		}

		[DataSourceProperty]
		public bool HasDetail
		{
			get => _hasDetail;
			set
			{
				if (_hasDetail != value)
				{
					_hasDetail = value;
					OnPropertyChanged("HasDetail");
				}
			}
		}

		public KingdomAgendaItemVM(KingdomDecision decision)
		{
			_decision = decision;
			TitleText = VoteDealBehavior.GetDecisionDisplayTitleForAgenda(decision);
			DecisionTypeText = VoteDealBehavior.GetBilateralDiplomacyAgendaTypeText(decision) ?? GetDecisionTypeLabel(decision);
			RefreshTimingState();
		}

		internal void RefreshTimingState()
		{
			if (_decision == null)
			{
				DaysRemainingText = "提案人: 未知";
				CanCallVoteMeeting = false;
				return;
			}

			float remainingDays = _decision.TriggerTime.RemainingDaysFromNow;
			string proposerText = VoteDealBehavior.GetBilateralDiplomacyAgendaProposerText(_decision) ?? _decision.ProposerClan?.Name?.ToString() ?? "未知";
			DaysRemainingText = (remainingDays > 0)
				? $"提案人: {proposerText} · 剩余 {remainingDays:F1} 天"
				: $"提案人: {proposerText} · 可以投票";
			IsUrgent = remainingDays <= 1f;
			CanCallVoteMeeting = _decision.Kingdom == Clan.PlayerClan?.Kingdom &&
				_decision.IsPlayerParticipant &&
				!_decision.IsEnforced &&
				!_decision.ShouldBeCancelled();
		}

		[DataSourceMethod]
		public void ExecuteSelect()
		{
			Parent?.SelectItem(this);
		}

		[DataSourceMethod]
		public void ExecuteCallVoteMeeting()
		{
			Parent?.CallVoteMeeting(this);
		}

		private static string GetWeightText(Supporter.SupportWeights weight)
		{
			switch (weight)
			{
				case Supporter.SupportWeights.SlightlyFavor: return "略微支持";
				case Supporter.SupportWeights.StronglyFavor: return "强力支持";
				case Supporter.SupportWeights.FullyPush: return "全力推动";
				default: return "中立";
			}
		}

		internal void LoadDetail()
		{
			if (_options != null) return;
			if (_decision == null) return;

			try
			{
				var initialCandidates = _decision.DetermineInitialCandidates();
				if (initialCandidates == null) return;

				var candidateList = new MBList<DecisionOutcome>();
				foreach (var cnd in initialCandidates)
				{
					if (cnd != null) candidateList.Add(cnd);
				}
				if (candidateList.Count == 0) return;

				var narrowed = _decision.NarrowDownCandidates(candidateList, 3);
				if (narrowed == null || narrowed.Count == 0) return;

				_options = new MBBindingList<AgendaOptionVM>();

				_decision.DetermineSponsors(narrowed);

				var supporters = _decision.DetermineSupporters()?.ToList();
				if (supporters == null || supporters.Count == 0) return;

				var likelihoodProp = typeof(DecisionOutcome).GetProperty("Likelihood");
				var initialSupportProp = typeof(DecisionOutcome).GetProperty("InitialSupport");
				float totalInitial = 0f;
				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					float initSup = 0f;
					foreach (var supporter in supporters)
					{
						if (!supporter.IsPlayer)
							initSup += MathF.Clamp(_decision.DetermineSupport(supporter.Clan, outcome), 0f, 100f);
					}
					initialSupportProp?.SetValue(outcome, initSup);
					totalInitial += initSup;
				}
				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					float initSup = (float)(initialSupportProp?.GetValue(outcome) ?? 0f);
					likelihoodProp?.SetValue(outcome, totalInitial > 0f ? initSup / totalInitial : 0f);
				}
				var outcomeWeights = new Dictionary<DecisionOutcome, int>();
				var outcomeSupporters = new Dictionary<DecisionOutcome, List<(Supporter supporter, Supporter.SupportWeights weight)>>();
				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					outcomeWeights[outcome] = 0;
					outcomeSupporters[outcome] = new List<(Supporter, Supporter.SupportWeights)>();
				}

				int totalWeight = 0;
				foreach (var supporter in supporters)
				{
					if (supporter.IsPlayer) continue;
					try
					{
						Supporter.SupportWeights weight;
						var chosen = _decision.DetermineSupportOption(supporter, narrowed, out weight, false);
						if (chosen != null && weight > Supporter.SupportWeights.StayNeutral && outcomeWeights.ContainsKey(chosen))
						{
							outcomeSupporters[chosen].Add((supporter, weight));
						}
					}
					catch { }
				}

				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					outcomeWeights[outcome] = 0;
					outcomeSupporters[outcome].Clear();
				}
				totalWeight = 0;

				_decision.DetermineSponsors(narrowed);

				foreach (var supporter in supporters)
				{
					if (supporter.IsPlayer) continue;
					try
					{
						Supporter.SupportWeights weight;
						var chosen = _decision.DetermineSupportOption(supporter, narrowed, out weight, true);
						if (chosen != null && weight > Supporter.SupportWeights.StayNeutral && outcomeWeights.ContainsKey(chosen))
						{
							int w = (int)weight - (int)Supporter.SupportWeights.StayNeutral;
							outcomeWeights[chosen] += w;
							totalWeight += w;
							outcomeSupporters[chosen].Add((supporter, weight));
						}
					}
					catch { }
				}

				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					var opt = new AgendaOptionVM();
					opt.Name = VoteDealBehavior.GetDecisionOutcomeDisplayTitleForAgenda(_decision, outcome);
					opt.Description = VoteDealBehavior.GetDecisionOutcomeDisplayDescriptionForAgenda(_decision, outcome);
					opt.SponsorName = outcome.SponsorClan?.Name?.ToString() ?? "未知";
					opt.SupportPercentage = totalWeight > 0 ? (int)Math.Round(outcomeWeights[outcome] * 100.0 / totalWeight) : 0;

					// Sponsor portrait
					try
					{
						Hero sponsorLeader = outcome.SponsorClan?.Leader;
						if (sponsorLeader?.CharacterObject != null)
							opt.SponsorVisual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(sponsorLeader.CharacterObject));
					}
					catch { }

					// Sponsor clan banner
					try
					{
						Clan sponsorClan = outcome.SponsorClan;
						if (sponsorClan?.Banner != null)
							opt.SponsorBanner = new BannerImageIdentifierVM(sponsorClan.Banner, false);
					}
					catch { }

					foreach (var (supporter, weight) in outcomeSupporters[outcome])
					{
						var supporterVM = new AgendaSupporterVM
						{
							Name = supporter.Name?.ToString() ?? "未知",
							WeightText = GetWeightText(weight),
							SupportWeightImagePath = VoteDealBehavior.GetSupportWeightImagePath(weight)
						};
						try
						{
							Hero leader = supporter.Clan?.Leader;
							if (leader?.CharacterObject != null)
								supporterVM.Visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(leader.CharacterObject));
						}
						catch { }
						opt.Supporters.Add(supporterVM);
					}

					_options.Add(opt);
				}
				HasDetail = _options.Count > 0;
				OnPropertyChanged("Options");
				OnPropertyChanged("HasDetail");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDetail] LoadDetail error: {ex.Message}");
			}
		}

		private static string GetDecisionTypeLabel(KingdomDecision decision)
		{
			if (decision is DeclareWarDecision) return "宣战";
			if (decision is MakePeaceKingdomDecision) return "和平";
			if (decision is KingdomPolicyDecision) return "政策";
			if (decision is SettlementClaimantPreliminaryDecision) return "封地";
			if (decision is SettlementClaimantDecision) return "封地";
			if (decision is KingSelectionKingdomDecision) return "王选";
			if (decision is ExpelClanFromKingdomDecision) return "驱逐";
			if (decision is StartAllianceDecision) return "联盟";
			if (decision is TradeAgreementDecision) return "贸易";
			return "决议";
		}
	}

		public class AgendaSupporterVM : ViewModel
		{
			private string _name;
			private string _weightText;
			private CharacterImageIdentifierVM _visual;
			private string _supportWeightImagePath;

			[DataSourceProperty]
			public string Name
			{
				get => _name;
				set { _name = value; OnPropertyChanged("Name"); }
			}

			[DataSourceProperty]
			public string WeightText
			{
				get => _weightText;
				set { _weightText = value; OnPropertyChanged("WeightText"); }
			}

			[DataSourceProperty]
			public CharacterImageIdentifierVM Visual
			{
				get => _visual;
				set
				{
					if (_visual != value)
					{
						_visual = value;
						OnPropertyChanged("Visual");
					}
				}
			}

			[DataSourceProperty]
			public string SupportWeightImagePath
			{
				get => _supportWeightImagePath;
				set
				{
					if (_supportWeightImagePath != value)
					{
						_supportWeightImagePath = value;
						OnPropertyChanged("SupportWeightImagePath");
					}
				}
			}
		}
	public class AgendaOptionVM : ViewModel
	{
		private string _name;
		private string _description;
		private string _sponsorName;
		private int _supportPercentage;
		private MBBindingList<AgendaSupporterVM> _supporters;
			private CharacterImageIdentifierVM _sponsorVisual;
		private ImageIdentifierVM _sponsorBanner;
		private bool _hasSponsorBanner;

		[DataSourceProperty]
		public string Name
		{
			get => _name;
			set { _name = value; OnPropertyChanged("Name"); }
		}

		[DataSourceProperty]
		public string Description
		{
			get => _description;
			set { _description = value; OnPropertyChanged("Description"); }
		}

		[DataSourceProperty]
		public string SponsorName
		{
			get => _sponsorName;
			set { _sponsorName = value; OnPropertyChanged("SponsorName"); }
		}

		[DataSourceProperty]
		public int SupportPercentage
		{
			get => _supportPercentage;
			set
			{
				if (_supportPercentage != value)
				{
					_supportPercentage = value;
					OnPropertyChanged("SupportPercentage");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<AgendaSupporterVM> Supporters
		{
			get => _supporters;
			set
			{
				if (_supporters != value)
				{
					_supporters = value;
					OnPropertyChanged("Supporters");
				}
			}
		}


			[DataSourceProperty]
			public CharacterImageIdentifierVM SponsorVisual
			{
				get => _sponsorVisual;
				set
				{
					if (_sponsorVisual != value)
					{
						_sponsorVisual = value;
						OnPropertyChanged("SponsorVisual");
					}
				}
			}

		[DataSourceProperty]
		public ImageIdentifierVM SponsorBanner
		{
			get => _sponsorBanner;
			set
			{
				if (_sponsorBanner != value)
				{
					_sponsorBanner = value;
					OnPropertyChanged("SponsorBanner");
					HasSponsorBanner = _sponsorBanner != null;
				}
			}
		}

		[DataSourceProperty]
		public bool HasSponsorBanner
		{
			get => _hasSponsorBanner;
			set { _hasSponsorBanner = value; OnPropertyChanged("HasSponsorBanner"); }
		}

				public AgendaOptionVM()
		{
			Supporters = new MBBindingList<AgendaSupporterVM>();
		}
	}

	internal static class KingdomAgendaTabState
	{
		private static readonly ConditionalWeakTable<KingdomManagementVM, State> _states = new();
		private static readonly ConditionalWeakTable<KingdomDecisionsVM, State> _decisionStates = new();
		// Kingdom management is a single active screen. Keep a weak shortcut for
		// UI-level custom tabs, which do not have a vanilla VM method to patch.
		private static WeakReference _mostRecentlyRegisteredState;

		private sealed class State
		{
			public Action Clear;
			public Action Select;
			public bool ReturnToAgendaAfterRefresh;
		}

		public static void Register(KingdomManagementVM vm, Action clear, Action select)
		{
			State state = new State { Clear = clear, Select = select };
			_states.Add(vm, state);
			_mostRecentlyRegisteredState = new WeakReference(state);
			KingdomCustomTabIsolation.ResetForNewKingdomScreen();
			if (vm?.Decision != null)
			{
				_decisionStates.Add(vm.Decision, state);
			}
		}

		public static void Clear(KingdomManagementVM vm)
		{
			if (_states.TryGetValue(vm, out var state))
				state.Clear?.Invoke();
		}

		internal static void ClearForCustomTabClick()
		{
			if (_mostRecentlyRegisteredState?.Target is State state)
			{
				state.Clear?.Invoke();
			}
		}

		public static void Select(KingdomManagementVM vm)
		{
			if (_states.TryGetValue(vm, out var state))
				state.Select?.Invoke();
		}

		public static bool Select(KingdomDecisionsVM vm)
		{
			if (vm == null || !_decisionStates.TryGetValue(vm, out var state)) return false;

			state.Select?.Invoke();
			return true;
		}

		public static void RequestReturnAfterRefresh(KingdomManagementVM vm)
		{
			if (_states.TryGetValue(vm, out var state))
				state.ReturnToAgendaAfterRefresh = true;
		}

		public static void SelectAfterRefreshIfRequested(KingdomManagementVM vm)
		{
			if (!_states.TryGetValue(vm, out var state)) return;
			if (!state.ReturnToAgendaAfterRefresh) return;

			state.ReturnToAgendaAfterRefresh = false;
			state.Select?.Invoke();
		}
	}

		[ViewModelMixin("RefreshValues", true)]
		internal sealed class KingdomAgendaVMMixin : BaseViewModelMixin<KingdomManagementVM>
		{

			[DataSourceProperty]
			public string AgendaTabText { get; set; }

			[DataSourceProperty]
			public bool IsAgendaSelected { get; set; }

			[DataSourceProperty]
			public KingdomAgendaVM Agenda { get; set; }

			public KingdomAgendaVMMixin(KingdomManagementVM vm) : base(vm)
			{
				AgendaTabText = "议程";
				Agenda = new KingdomAgendaVM();
				Agenda.CallVoteMeetingRequested = StartVoteMeeting;
				IsAgendaSelected = false;

				KingdomAgendaTabState.Register(vm, ClearAgendaSelection, SelectAgenda);

				Agenda.RefreshKingdomList();
				Agenda.SetDefaultKingdom();

			}

			private void OpenPendingRequiredAgendaVote()
			{
				KingdomDecision decision = VoteDealBehavior.s_pendingRequiredAgendaDecision;
				if (decision == null) return;

				VoteDealBehavior.s_pendingRequiredAgendaDecision = null;
				if (!VoteDealBehavior.RequiresPlayerRulerResolution(decision)
					|| !VoteDealBehavior.IsDecisionPendingInKingdom(decision, decision.Kingdom)) return;
				StartVoteMeeting(decision);
			}

			private void OpenPendingAgendaMapNotice()
			{
				KingdomDecision decision = VoteDealBehavior.s_pendingAgendaMapNoticeDecision;
				if (decision == null) return;

				VoteDealBehavior.s_pendingAgendaMapNoticeDecision = null;
				if (!VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(decision)) return;
				if (VoteDealBehavior.RequiresPlayerRulerResolution(decision))
				{
					if (VoteDealBehavior.s_pendingRequiredAgendaDecision == decision)
					{
						VoteDealBehavior.s_pendingRequiredAgendaDecision = null;
					}
					StartVoteMeeting(decision);
					return;
				}

				SelectAgenda();
				Agenda?.TrySelectDecision(decision);
			}

			public override void OnRefresh()
			{
				Agenda?.RefreshAgendaItems();
				OpenPendingAgendaMapNotice();
				OpenPendingRequiredAgendaVote();
			}

			[DataSourceMethod]
			public void ExecuteShowAgenda()
			{
				SelectAgenda();
			}

			private void SelectAgenda()
			{
				ViewModel.Clan.Show = false;
				ViewModel.Settlement.Show = false;
				ViewModel.Policy.Show = false;
				ViewModel.Army.Show = false;
				ViewModel.Diplomacy.Show = false;

				Agenda.RefreshKingdomList();
				Agenda.SetDefaultKingdom();
				IsAgendaSelected = true;
				Agenda?.RefreshAgendaItems();
				Agenda?.RefreshValues();

				ViewModel.OnPropertyChanged("IsAgendaSelected");
				ViewModel.OnPropertyChangedWithValue(true, "IsAgendaSelected");
				ViewModel.OnPropertyChanged("Agenda");
				KingdomCustomTabIsolation.HideForeignPanelsForAgendaSelection();

			}

			private void StartVoteMeeting(KingdomDecision decision)
			{
				try
				{
					if (decision == null) return;
					if (decision.Kingdom != Clan.PlayerClan?.Kingdom) return;
					if (!decision.IsPlayerParticipant && !VoteDealBehavior.RequiresPlayerRulerResolution(decision)) return;
					if (decision.ShouldBeCancelled())
					{
						InformationManager.DisplayMessage(new InformationMessage("该议程已经失效，无法召开投票会议", Color.FromUint(0xFFD166)));
						Agenda?.RefreshAgendaItems();
						return;
					}

					if (decision.TriggerTime.IsFuture)
					{
						Traverse.Create(decision).Property("TriggerTime").SetValue(CampaignTime.Now);
					}

					KingdomAgendaTabState.RequestReturnAfterRefresh(ViewModel);
					ViewModel.Decision.RefreshWith(decision);
					ViewModel.Decision.IsActive = true;
					ViewModel.OnPropertyChanged("Decision");
					Agenda?.ClearSelection();
					InformationManager.DisplayMessage(new InformationMessage(
						$"已召开投票会议：{decision.GetGeneralTitle()}",
						Color.FromUint(0xFFD166)));
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[AgendaMeeting] StartVoteMeeting error: {ex.Message}");
					InformationManager.DisplayMessage(new InformationMessage("召开投票会议失败，请查看 AnimusForge 日志", Color.FromUint(0xFF6B6B)));
				}
			}

			private void ClearAgendaSelection()
			{
				IsAgendaSelected = false;
				ViewModel.OnPropertyChanged("IsAgendaSelected");
				ViewModel.OnPropertyChangedWithValue(false, "IsAgendaSelected");
			}
		}

		internal static class KingdomAgendaTabSelectionPatch
		{
			public static void Apply(Harmony harmony)
			{
				if (harmony == null) return;

				var postfix = new HarmonyMethod(typeof(KingdomAgendaTabSelectionPatch), nameof(ClearPostfix));

				string[] methodNames = { "ExecuteShowClan", "ExecuteShowFiefs", "ExecuteShowPolicies", "ExecuteShowArmy", "ExecuteShowDiplomacy" };

				foreach (string name in methodNames)
				{
					MethodInfo original = AccessTools.Method(typeof(KingdomManagementVM), name);
					if (original != null)
					{
						harmony.Patch(original, postfix: postfix);
					}
					else
					{
						Logger.Log("VoteDeal", $"[AgendaTabSelection] WARNING: KingdomManagementVM.{name} not found.");
					}
				}

				MethodInfo onRefresh = AccessTools.Method(typeof(KingdomManagementVM), "OnRefresh");
				if (onRefresh != null)
				{
					harmony.Patch(onRefresh,
						postfix: new HarmonyMethod(typeof(KingdomAgendaTabSelectionPatch), nameof(OnRefreshPostfix)));
				}
				else
				{
					Logger.Log("VoteDeal", "[AgendaTabSelection] WARNING: KingdomManagementVM.OnRefresh not found.");
				}
			}

			private static void ClearPostfix(KingdomManagementVM __instance)
			{
				KingdomAgendaTabState.Clear(__instance);
			}

			private static void OnRefreshPostfix(KingdomManagementVM __instance)
			{
				KingdomAgendaTabState.SelectAfterRefreshIfRequested(__instance);
			}

		}

	}
