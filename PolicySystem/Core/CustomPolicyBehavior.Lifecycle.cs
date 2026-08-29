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
	private static void ApplyDynamicPolicyPatchesOnce()
	{
		if (_dynamicPolicyPatchesApplied)
		{
			return;
		}
		_dynamicPolicyPatchesApplied = true;
		try
		{
			Harmony harmony = new Harmony("com.AnimusForge.custompolicy.agenda");
			harmony.Patch(AccessTools.Method(typeof(KingdomPolicyDecision), nameof(KingdomPolicyDecision.IsAllowed)),
				postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_KingdomPolicyDecision_IsAllowed_Postfix)));
			System.Reflection.MethodInfo shouldBeCancelled = AccessTools.Method(typeof(KingdomDecision), nameof(KingdomDecision.ShouldBeCancelled));
			if (shouldBeCancelled != null)
			{
				harmony.Patch(shouldBeCancelled,
					prefix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_KingdomDecision_ShouldBeCancelled_Prefix)));
			}
			System.Reflection.MethodInfo determineSupportOption = AccessTools.Method(typeof(KingdomDecision), nameof(KingdomDecision.DetermineSupportOption));
			if (determineSupportOption != null)
			{
				harmony.Patch(determineSupportOption,
					prefix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_KingdomDecision_DetermineSupportOption_Prefix)));
			}
			harmony.Patch(AccessTools.Method(typeof(KingdomPoliciesVM), nameof(KingdomPoliciesVM.RefreshPolicyList)),
				postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_KingdomPoliciesVM_RefreshPolicyList_Postfix)));
			System.Reflection.MethodInfo appendPotentialPolicies = AccessTools.Method(typeof(VoteDealBehavior), "AppendPotentialPolicyEntries");
			if (appendPotentialPolicies != null)
			{
				harmony.Patch(appendPotentialPolicies,
					prefix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_AppendPotentialPolicyEntries_Prefix)));
			}
			System.Reflection.MethodInfo executeDecisionDone = AccessTools.Method(typeof(DecisionItemBaseVM), "ExecuteDone");
			if (executeDecisionDone != null)
			{
				harmony.Patch(executeDecisionDone,
					prefix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_DecisionItemBaseVM_ExecuteDone_Prefix)));
			}
			Type concludedLogEntryType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.LogEntries.KingdomDecisionConcludedLogEntry");
			System.Reflection.ConstructorInfo concludedLogEntryConstructor = concludedLogEntryType == null
				? null
				: AccessTools.Constructor(concludedLogEntryType, new[] { typeof(KingdomDecision), typeof(DecisionOutcome), typeof(bool) });
			if (concludedLogEntryConstructor != null)
			{
				harmony.Patch(concludedLogEntryConstructor,
					prefix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_KingdomDecisionConcludedLogEntry_Constructor_Prefix)));
			}
			System.Reflection.MethodInfo getAiChoice = AccessTools.Method(typeof(KingdomElection), "GetAiChoice");
			if (getAiChoice != null)
			{
				harmony.Patch(getAiChoice,
					postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_KingdomElection_GetAiChoice_Postfix)));
			}
			System.Reflection.MethodInfo buildShoutPromptContext = AccessTools.Method(typeof(MyBehavior), nameof(MyBehavior.BuildShoutPromptContextForExternal));
			if (buildShoutPromptContext != null)
			{
				harmony.Patch(buildShoutPromptContext,
					postfix: new HarmonyMethod(typeof(CustomPolicyBehavior), nameof(Patch_MyBehavior_BuildShoutPromptContextForExternal_Postfix)));
			}
			PolicySystemLog.Write("Agenda", "patches-applied", "dynamic policy ownership, NPC proposer support/cancellation guard, policy list filters, duplicate NPC adoption chat suppression, ordered AF result popups, NPC ruler adoption, and mention-based policy knowledge retrieval applied");
		}
		catch (Exception ex)
		{
			_dynamicPolicyPatchesApplied = false;
			PolicySystemLog.Write("Agenda", "patches-failed", ex.ToString());
		}
	}

	private static void Patch_MyBehavior_BuildShoutPromptContextForExternal_Postfix(
		Hero targetHero,
		string input,
		CharacterObject targetCharacter,
		string kingdomIdOverride,
		bool suppressDynamicRuleAndLore,
		ref MyBehavior.ShoutPromptContext __result)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			if (__result == null || suppressDynamicRuleAndLore || __result.MentionedEntities == null || __result.MentionedEntities.IsEmpty)
			{
				return;
			}
			bool retrieved = NpcRulerPolicyBehavior.TryBuildPolicyDialogueContextForExternal(
				input,
				__result.MentionedEntities,
				__result.ExplicitMentionedKingdomIds,
				targetHero,
				targetCharacter,
				kingdomIdOverride,
				SaveRuntimeGuard.CurrentGeneration,
				out PolicyHistoryRetrievalResult retrieval);
			if (!retrieved || string.IsNullOrWhiteSpace(retrieval?.DialoguePrompt))
			{
				PolicySystemLog.Write("DialoguePolicy", "retrieval-skipped",
					"channel=shared_prompt_context"
					+ " code=" + (retrieval?.DialogueFailureCode ?? "unknown")
					+ " entities=" + (retrieval?.DialogueMentionTermCount ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " queries=" + (retrieval?.DialogueSuccessfulQueryCount ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " owners=" + FormatPolicyDialogueOwnerIds(retrieval?.DialogueOwnerKingdomIds)
					+ " candidates=" + (retrieval?.DialogueCandidateCount ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " elapsedMs=" + stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
				return;
			}
			__result.Extras = AppendPolicyDialogueKnowledgeBlock(__result.Extras, retrieval.DialoguePrompt);
			PolicySystemLog.Write("DialoguePolicy", "retrieval-injected",
				"channel=shared_prompt_context"
				+ " entities=" + retrieval.DialogueMentionTermCount.ToString(CultureInfo.InvariantCulture)
				+ " queries=" + retrieval.DialogueSuccessfulQueryCount.ToString(CultureInfo.InvariantCulture)
				+ " owners=" + FormatPolicyDialogueOwnerIds(retrieval.DialogueOwnerKingdomIds)
				+ " queryChars=" + retrieval.DialogueQueryChars.ToString(CultureInfo.InvariantCulture)
				+ " queryHash=" + (retrieval.DialogueQueryHash ?? string.Empty)
				+ " candidates=" + retrieval.DialogueCandidateCount.ToString(CultureInfo.InvariantCulture)
				+ " hits=" + retrieval.DialogueHitCount.ToString(CultureInfo.InvariantCulture)
				+ " cacheHits=" + retrieval.DocumentVectorCacheHits.ToString(CultureInfo.InvariantCulture)
				+ " cacheMisses=" + retrieval.DocumentVectorCacheMisses.ToString(CultureInfo.InvariantCulture)
				+ " promptChars=" + retrieval.DialoguePrompt.Length.ToString(CultureInfo.InvariantCulture)
				+ " promptHash=" + (retrieval.DialoguePromptHash ?? string.Empty)
				+ " elapsedMs=" + stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("DialoguePolicy", "retrieval-failed",
				"channel=shared_prompt_context code=unexpected_failure type=" + (ex.GetType().FullName ?? string.Empty)
				+ " elapsedMs=" + stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static string FormatPolicyDialogueOwnerIds(IEnumerable<string> ownerKingdomIds)
	{
		return string.Join(",", (ownerKingdomIds ?? Enumerable.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase));
	}

	private static string AppendPolicyDialogueKnowledgeBlock(string extras, string dialoguePrompt)
	{
		const string marker = "【以下是关于（当前对话相关政策）的背景知识，NPC可酌情参考】";
		string existing = extras ?? string.Empty;
		string knowledge = (dialoguePrompt ?? string.Empty).Trim();
		if (knowledge.Length == 0 || existing.IndexOf(marker, StringComparison.Ordinal) >= 0)
		{
			return existing;
		}
		return existing.TrimEnd()
			+ (string.IsNullOrWhiteSpace(existing) ? string.Empty : Environment.NewLine)
			+ knowledge;
	}

	private static void Patch_KingdomPolicyDecision_IsAllowed_Postfix(KingdomPolicyDecision __instance, ref bool __result)
	{
		if (!__result || __instance?.Policy == null || !IsDynamicPolicyId(__instance.Policy.StringId))
		{
			return;
		}
		if (!TryGetDynamicPolicyDataStatic(__instance.Policy.StringId, out DynamicPolicySaveData data))
		{
			__result = false;
			return;
		}
		__result = string.Equals(data.OwnerKingdomId ?? "", __instance.Kingdom?.StringId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private static bool Patch_KingdomDecision_ShouldBeCancelled_Prefix(KingdomDecision __instance, ref bool __result)
	{
		KingdomPolicyDecision decision = __instance as KingdomPolicyDecision;
		if (!IsPendingNpcRulerPolicyAdoption(decision))
		{
			return true;
		}
		__result = false;
		return false;
	}

	private static bool Patch_KingdomDecision_DetermineSupportOption_Prefix(
		KingdomDecision __instance,
		Supporter supporter,
		MBReadOnlyList<DecisionOutcome> possibleOutcomes,
		ref Supporter.SupportWeights supportWeightOfSelectedOutcome,
		ref DecisionOutcome __result)
	{
		KingdomPolicyDecision decision = __instance as KingdomPolicyDecision;
		if (!IsPendingNpcRulerPolicyAdoption(decision)
			|| supporter?.Clan != decision.ProposerClan
			|| possibleOutcomes == null)
		{
			return true;
		}
		KingdomPolicyDecision.PolicyDecisionOutcome adoption = possibleOutcomes
			.OfType<KingdomPolicyDecision.PolicyDecisionOutcome>()
			.FirstOrDefault(outcome => outcome.ShouldDecisionBeEnforced);
		if (adoption == null)
		{
			return true;
		}
		supportWeightOfSelectedOutcome = Supporter.SupportWeights.SlightlyFavor;
		__result = adoption;
		return false;
	}

	private static bool IsPendingNpcRulerPolicyAdoption(KingdomPolicyDecision decision)
	{
		PolicyObject policy = decision?.Policy;
		Kingdom kingdom = decision?.Kingdom;
		if (policy == null
			|| kingdom == null
			|| kingdom.IsEliminated
			|| !IsDynamicPolicyId(policy.StringId)
			|| !TryGetDynamicPolicyDataStatic(policy.StringId, out DynamicPolicySaveData data))
		{
			return false;
		}
		return string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(data.OwnerKingdomId ?? "", kingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase)
			&& decision.ProposerClan != null
			&& decision.ProposerClan == kingdom.RulingClan
			&& decision.ProposerClan.Kingdom == kingdom
			&& kingdom.ActivePolicies?.Contains(policy) != true;
	}

	private static void Patch_AppendPotentialPolicyEntries_Prefix(Kingdom kingdom, ref IEnumerable<PolicyObject> policies)
	{
		if (kingdom == null || policies == null)
		{
			return;
		}
		policies = policies.Where(policy => policy == null
			|| !IsDynamicPolicyId(policy.StringId)
			|| (TryGetDynamicPolicyDataStatic(policy.StringId, out DynamicPolicySaveData data)
				&& string.Equals(data.OwnerKingdomId ?? "", kingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase)));
	}

	private static bool Patch_DecisionItemBaseVM_ExecuteDone_Prefix(DecisionItemBaseVM __instance)
	{
		try
		{
			KingdomPolicyDecision decision = Traverse.Create(__instance).Field("_decision").GetValue<KingdomDecision>() as KingdomPolicyDecision;
			if (decision?.Policy == null || !IsDynamicPolicyId(decision.Policy.StringId))
			{
				return true;
			}
			string policyObjectId = decision.Policy.StringId ?? "";
			Action onDecisionOver = DecisionItemOnDecisionOverField?.GetValue(__instance) as Action;
			if (onDecisionOver == null || !TryDeferOriginalPolicyResult(policyObjectId, delegate
			{
				onDecisionOver();
			}))
			{
				return true;
			}
			// Mirror the state cleanup in DecisionItemBaseVM.ExecuteDone. Its native inquiry
			// is intentionally replaced by the custom result popup, but its _onDecisionOver
			// callback is still required to release the concluded decision VM.
			__instance.IsActive = false;
			CampaignEvents.KingdomDecisionConcluded.ClearListeners(__instance);
			PolicySystemLog.Write("Agenda", "original-result-cleanup-deferred", "policy=" + policyObjectId);
			return false;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "original-result-popup-defer-failed", ex.ToString());
			return true;
		}
	}

	private static void Patch_KingdomDecisionConcludedLogEntry_Constructor_Prefix(KingdomDecision decision, ref bool isPlayerInvolved)
	{
		if (isPlayerInvolved)
		{
			return;
		}
		try
		{
			KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
			PolicyObject policy = policyDecision?.Policy;
			if (policy == null || !IsDynamicPolicyId(policy.StringId))
			{
				return;
			}
			bool isInvertedDecision = Traverse.Create(policyDecision).Field("_isInvertedDecision").GetValue<bool>();
			if (isInvertedDecision)
			{
				return;
			}
			isPlayerInvolved = true;
			PolicySystemLog.Write("Notice", "original-adoption-chat-suppressed", "policy=" + (policy.StringId ?? ""));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Notice", "original-adoption-chat-suppress-failed", ex.Message);
		}
	}

	private static void Patch_KingdomElection_GetAiChoice_Postfix(KingdomElection __instance, ref DecisionOutcome __result)
	{
		try
		{
			KingdomPolicyDecision decision = Traverse.Create(__instance).Field("_decision").GetValue<KingdomDecision>() as KingdomPolicyDecision;
			PolicyObject policy = decision?.Policy;
			if (policy == null || !IsDynamicPolicyId(policy.StringId) || !TryGetDynamicPolicyDataStatic(policy.StringId, out DynamicPolicySaveData data))
			{
				return;
			}
			Kingdom kingdom = decision.Kingdom;
			if (!string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
				|| kingdom == null
				|| decision.ProposerClan == null
				|| decision.ProposerClan != kingdom.RulingClan
				|| !string.Equals(data.OwnerKingdomId ?? "", kingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase)
				|| kingdom.ActivePolicies?.Contains(policy) == true)
			{
				return;
			}
			KingdomPolicyDecision.PolicyDecisionOutcome adoption = __instance?.PossibleOutcomes?
				.OfType<KingdomPolicyDecision.PolicyDecisionOutcome>()
				.FirstOrDefault(outcome => outcome.ShouldDecisionBeEnforced);
			if (adoption == null || ReferenceEquals(__result, adoption))
			{
				return;
			}
			__result = adoption;
			PolicySystemLog.Write("Agenda", "npc-ruler-adoption-forced", "recordId=" + (data.RecordId ?? "")
				+ " policy=" + (data.PolicyObjectId ?? "")
				+ " kingdom=" + (data.OwnerKingdomId ?? ""));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "npc-ruler-adoption-force-failed", ex.ToString());
		}
	}

	private static void BeginPolicySuccessResultSequence(string policyObjectId)
	{
		string id = (policyObjectId ?? "").Trim();
		if (!string.Equals(_policySuccessResultPolicyObjectId, id, StringComparison.OrdinalIgnoreCase))
		{
			DeferredOriginalPolicyResults.Clear();
		}
		_policySuccessResultPolicyObjectId = id;
		_policySuccessResultVisible = !string.IsNullOrWhiteSpace(id);
	}

	private static void BeginPolicyApprovalResultSequence(string policyObjectId)
	{
		BeginPolicySuccessResultSequence(policyObjectId);
	}

	private static bool TryDeferOriginalPolicyResult(string policyObjectId, Action action)
	{
		string id = (policyObjectId ?? "").Trim();
		if (!_policySuccessResultVisible
			|| string.IsNullOrWhiteSpace(id)
			|| !string.Equals(_policySuccessResultPolicyObjectId, id, StringComparison.OrdinalIgnoreCase)
			|| action == null)
		{
			return false;
		}
		if (!DeferredOriginalPolicyResults.ContainsKey(id))
		{
			DeferredOriginalPolicyResults[id] = action;
		}
		return true;
	}

	private static void CompletePolicySuccessResultSequence(string policyObjectId, bool releaseDeferredResults = true)
	{
		string id = (policyObjectId ?? "").Trim();
		if (!_policySuccessResultVisible || !string.Equals(_policySuccessResultPolicyObjectId, id, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		List<Action> deferred = DeferredOriginalPolicyResults.Values.Where(action => action != null).ToList();
		DeferredOriginalPolicyResults.Clear();
		_policySuccessResultVisible = false;
		_policySuccessResultPolicyObjectId = "";
		if (releaseDeferredResults)
		{
			foreach (Action action in deferred)
			{
				MainThreadActions.Enqueue(action);
			}
		}
		PolicySystemLog.Write("Agenda", releaseDeferredResults ? "original-result-cleanup-released" : "original-result-cleanup-suppressed", "policy=" + id
			+ " deferred=" + deferred.Count.ToString(CultureInfo.InvariantCulture));
	}

	private static void Patch_KingdomPoliciesVM_RefreshPolicyList_Postfix(KingdomPoliciesVM __instance)
	{
		try
		{
			if (__instance?.OtherPolicies == null)
			{
				return;
			}
			bool selectedRemoved = false;
			for (int i = __instance.OtherPolicies.Count - 1; i >= 0; i--)
			{
				KingdomPolicyItemVM item = __instance.OtherPolicies[i];
				if (item?.Policy == null || !IsDynamicPolicyId(item.Policy.StringId))
				{
					continue;
				}
				selectedRemoved |= __instance.CurrentSelectedPolicy == item;
				__instance.OtherPolicies.RemoveAt(i);
			}
			GameTexts.SetVariable("STR", __instance.OtherPolicies.Count);
			__instance.NumOfOtherPoliciesText = GameTexts.FindText("str_STR_in_parentheses").ToString();
			if (selectedRemoved)
			{
				PolicyObject replacement = __instance.OtherPolicies.FirstOrDefault()?.Policy ?? __instance.ActivePolicies?.FirstOrDefault()?.Policy;
				if (replacement != null)
				{
					__instance.SelectPolicy(replacement);
				}
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "original-policy-filter-failed", ex.Message);
		}
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		ApplyPolicySettlementModelPatchesOnce();
		ApplyPolicyFinanceModelPatchesOnce();
		ApplyPolicyClanPoliticsModelPatchesOnce();
		ApplyPolicyArmyFormationPatchesOnce();
		ApplyPolicyPartySizeLimitPatchesOnce();
		ApplyPolicyVillageRaidBanPatchesOnce();
		RemoveLegacyStoppedDynamicPolicyMembershipAfterLoad();
		ReconcilePolicyReReviewReplacementsAfterLoad();
		EnsureDynamicPoliciesRegistered(reconcilePending: false);
		ReconcileEliminatedKingdomPoliciesAfterLoad();
	}

	private void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		string kingdomId = (destroyedKingdom?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return;
		}
		try
		{
			TerminatePoliciesForDestroyedKingdoms(
				new HashSet<string>(StringComparer.OrdinalIgnoreCase) { kingdomId },
				"政策所属国家已经灭亡",
				"event");
			RefreshPolicyEffectRuntimeTargetsAfterStructureChange("kingdom-destroyed");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Lifecycle", "destroyed-kingdom-policy-cleanup-failed", "kingdom=" + kingdomId + " error=" + ex);
		}
	}

	private void OnPolicyTargetClanChangedKingdom(
		Clan clan,
		Kingdom oldKingdom,
		Kingdom newKingdom,
		ChangeKingdomAction.ChangeKingdomActionDetail detail,
		bool showNotification)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("clan-changed-kingdom");
	}

	private void OnPolicyTargetSettlementOwnerChanged(
		Settlement settlement,
		bool openToClaimants,
		Hero newOwner,
		Hero oldOwner,
		Hero capturerHero,
		ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("settlement-owner-changed");
	}

	private void OnPolicyTargetClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("clan-leader-changed");
	}

	private void OnPolicyTargetClanDestroyed(Clan clan)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("clan-destroyed");
	}

	private void OnPolicyTargetRulingClanChanged(Kingdom kingdom, Clan changedClan)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("ruling-clan-changed");
	}

	private void OnPolicyTargetWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		PolicyTargetSemanticRouter.MarkDynamicDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("war-declared");
	}

	private void OnPolicyTargetPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		PolicyTargetSemanticRouter.MarkDynamicDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("peace-made");
	}

	private void OnPolicyTargetAllianceChanged(Kingdom kingdom1, Kingdom kingdom2)
	{
		PolicyTargetSemanticRouter.MarkDynamicDirty();
		RefreshPolicyEffectRuntimeTargetsAfterStructureChange("alliance-changed");
	}

	private void ReconcileEliminatedKingdomPoliciesAfterLoad()
	{
		HashSet<string> eliminatedKingdomIds = new HashSet<string>(
			(Kingdom.All ?? Enumerable.Empty<Kingdom>())
				.Where(x => x != null && x.IsEliminated && !string.IsNullOrWhiteSpace(x.StringId))
				.Select(x => x.StringId.Trim()),
			StringComparer.OrdinalIgnoreCase);
		if (eliminatedKingdomIds.Count <= 0)
		{
			return;
		}
		try
		{
			TerminatePoliciesForDestroyedKingdoms(
				eliminatedKingdomIds,
				"读档核对：政策所属国家已经灭亡",
				"load");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Lifecycle", "eliminated-policy-load-reconcile-failed", ex.ToString());
		}
	}

	private void TerminatePoliciesForDestroyedKingdoms(HashSet<string> destroyedKingdomIds, string ownerEndReason, string source)
	{
		if (destroyedKingdomIds == null || destroyedKingdomIds.Count <= 0)
		{
			return;
		}
		HashSet<string> endedVassalTargetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (LocalPolicyRecordSaveData record in LoadLocalPolicyRecords())
		{
			if (record == null
				|| !string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.Status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.Status, LocalPolicyStatusRelationshipEnded, StringComparison.OrdinalIgnoreCase)
				|| string.IsNullOrWhiteSpace(record.TargetKingdomId))
			{
				continue;
			}
			if (destroyedKingdomIds.Contains((record.TargetKingdomId ?? "").Trim())
				|| destroyedKingdomIds.Contains((record.IssuerKingdomId ?? "").Trim()))
			{
				endedVassalTargetIds.Add(record.TargetKingdomId.Trim());
			}
		}
		foreach (string vassalTargetId in endedVassalTargetIds)
		{
			OnVassalRelationshipEndedInternal(vassalTargetId, "目标附庸国或宗主国已经灭亡");
		}

		HashSet<string> ownedRecordIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int endedOwnedPolicyCount = 0;
		foreach (DynamicPolicySaveData data in LoadDynamicPolicies()
			.Where(x => x != null
				&& ShouldKeepDynamicPolicyRegistered(x.Status)
				&& destroyedKingdomIds.Contains((x.OwnerKingdomId ?? "").Trim()))
			.ToList())
		{
			if (!string.IsNullOrWhiteSpace(data.RecordId))
			{
				ownedRecordIds.Add(data.RecordId.Trim());
			}
			try
			{
				TerminateDynamicPolicyForDestroyedOwner(data, ownerEndReason);
				endedOwnedPolicyCount++;
			}
			catch (Exception ex)
			{
				PolicySystemLog.Write("Lifecycle", "destroyed-owner-policy-end-failed", "recordId=" + (data.RecordId ?? "")
					+ " owner=" + (data.OwnerKingdomId ?? "") + " error=" + ex);
			}
		}

		int endedTargetEffectCount = 0;
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			ActivePolicyEffectSaveData effect;
			try
			{
				effect = GetActivePolicyEffectForWork(item.Key, item.Value ?? "");
			}
			catch (Exception ex)
			{
				PolicySystemLog.Write("Lifecycle", "destroyed-target-effect-load-failed", "effectId=" + item.Key + " error=" + ex.Message);
				continue;
			}
			if (effect == null || IsLocalActivePolicyEffect(effect) || IsVassalActivePolicyEffect(effect))
			{
				continue;
			}
			bool policyOwnerDestroyed = ownedRecordIds.Contains((effect.RecordId ?? "").Trim());
			bool effectTargetDestroyed = destroyedKingdomIds.Contains((effect.TargetKingdomId ?? "").Trim());
			if (!policyOwnerDestroyed && !effectTargetDestroyed)
			{
				continue;
			}
			string endReason = policyOwnerDestroyed ? ownerEndReason : "效果目标国家已经灭亡";
			DispatchPolicyEffectAbolishedBeforeRemoval(
				effect,
				"effect:" + FirstNonEmpty(effect.EffectId, item.Key) + ":kingdom_destroyed",
				"kingdom_destroyed");
			MarkPolicyRecordEffectEnded(effect, endReason, queueNaturalExpiry: !policyOwnerDestroyed);
			RemoveActivePolicyEffect(item.Key);
			endedTargetEffectCount++;
		}
		_activePolicyEffectModelCache.Clear();
		PolicySystemLog.Write("Lifecycle", "destroyed-kingdom-policies-ended",
			"source=" + (source ?? "")
			+ " kingdoms=" + string.Join(",", destroyedKingdomIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
			+ " ownedPolicies=" + endedOwnedPolicyCount.ToString(CultureInfo.InvariantCulture)
			+ " targetEffects=" + endedTargetEffectCount.ToString(CultureInfo.InvariantCulture)
			+ " vassalPolicies=" + endedVassalTargetIds.Count.ToString(CultureInfo.InvariantCulture));
	}

	private void TerminateDynamicPolicyForDestroyedOwner(DynamicPolicySaveData data, string reason)
	{
		if (data == null)
		{
			return;
		}
		Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, "");
		List<KingdomPolicyDecision> decisions = FindDynamicPolicyDecisions(owner, data.PolicyObjectId);
		PolicyObject policy = owner?.ActivePolicies?.FirstOrDefault(x => x != null
			&& string.Equals(x.StringId ?? "", data.PolicyObjectId ?? "", StringComparison.OrdinalIgnoreCase))
			?? decisions.FirstOrDefault()?.Policy
			?? MBObjectManager.Instance?.GetObject<PolicyObject>(data.PolicyObjectId);
		foreach (KingdomPolicyDecision decision in decisions)
		{
			owner?.RemoveDecision(decision);
		}
		if (policy != null && owner?.ActivePolicies?.Contains(policy) == true)
		{
			owner.RemovePolicy(policy);
		}
		if (string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase))
		{
			EndPolicyEffectsForAgendaAbolition(
				data.RecordId,
				reason,
				"record:" + (data.RecordId ?? string.Empty).Trim() + ":kingdom_destroyed");
			RejectDynamicPolicyAdoption(data, policy, reason);
			return;
		}
		CompleteDynamicPolicyAbolition(
			data,
			policy,
			reason,
			"record:" + (data.RecordId ?? string.Empty).Trim() + ":kingdom_destroyed");
	}

	private bool TryCancelActiveKingdomPolicyInternal(
		string policyId,
		string ownerKingdomId,
		string reason,
		out string policyName,
		out string result)
	{
		policyName = string.Empty;
		result = "error";
		string normalizedPolicyId = (policyId ?? string.Empty).Trim();
		string normalizedOwnerKingdomId = (ownerKingdomId ?? string.Empty).Trim();
		if (normalizedPolicyId.Length == 0)
		{
			result = "not_found";
			return false;
		}
		if (normalizedOwnerKingdomId.Length == 0)
		{
			result = "owner_mismatch";
			return false;
		}

		List<DynamicPolicySaveData> recordMatches = LoadDynamicPolicies()
			.Where(candidate => candidate != null
				&& string.Equals((candidate.RecordId ?? string.Empty).Trim(), normalizedPolicyId, StringComparison.OrdinalIgnoreCase))
			.ToList();
		List<DynamicPolicySaveData> ownerMatches = recordMatches
			.Where(candidate => string.Equals((candidate.OwnerKingdomId ?? string.Empty).Trim(), normalizedOwnerKingdomId, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (ownerMatches.Count == 0)
		{
			result = recordMatches.Count > 0 ? "owner_mismatch" : "not_found";
			return false;
		}
		if (ownerMatches.Select(candidate => candidate.PolicyObjectId ?? string.Empty)
			.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
		{
			PolicySystemLog.Write("Agenda", "external-cancel-ambiguous",
				"policyId=" + normalizedPolicyId + " owner=" + normalizedOwnerKingdomId);
			return false;
		}

		DynamicPolicySaveData matched = ownerMatches[0];
		policyName = (matched.PolicyName ?? string.Empty).Trim();
		if (string.Equals(matched.Status, DynamicPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(matched.Status, DynamicPolicyStatusRejected, StringComparison.OrdinalIgnoreCase))
		{
			result = "already_inactive";
			return true;
		}
		bool cancellable = string.Equals(matched.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(matched.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase);
		if (!cancellable || !IsDynamicPolicyId(matched.PolicyObjectId))
		{
			PolicySystemLog.Write("Agenda", "external-cancel-invalid-state",
				"policyId=" + normalizedPolicyId
				+ " owner=" + normalizedOwnerKingdomId
				+ " status=" + (matched.Status ?? string.Empty)
				+ " policyObjectId=" + (matched.PolicyObjectId ?? string.Empty));
			return false;
		}

		Kingdom owner = ResolveKingdomByIdOrName(matched.OwnerKingdomId, string.Empty);
		if (owner == null || owner.IsEliminated
			|| !string.Equals(owner.StringId ?? string.Empty, normalizedOwnerKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			PolicySystemLog.Write("Agenda", "external-cancel-owner-unavailable",
				"policyId=" + normalizedPolicyId + " owner=" + normalizedOwnerKingdomId);
			return false;
		}

		List<KingdomPolicyDecision> decisions = FindDynamicPolicyDecisions(owner, matched.PolicyObjectId);
		List<PolicyObject> activePolicies = owner.ActivePolicies?
			.Where(candidate => candidate != null
				&& string.Equals(candidate.StringId ?? string.Empty, matched.PolicyObjectId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
			.ToList() ?? new List<PolicyObject>();
		PolicyObject policy = activePolicies.FirstOrDefault()
			?? decisions.FirstOrDefault()?.Policy
			?? MBObjectManager.Instance?.GetObject<PolicyObject>(matched.PolicyObjectId);
		foreach (KingdomPolicyDecision decision in decisions)
		{
			owner.RemoveDecision(decision);
		}
		if (FindDynamicPolicyDecisions(owner, matched.PolicyObjectId).Count > 0)
		{
			PolicySystemLog.Write("Agenda", "external-cancel-decision-retained",
				"policyId=" + normalizedPolicyId + " owner=" + normalizedOwnerKingdomId);
			return false;
		}
		foreach (PolicyObject activePolicy in activePolicies)
		{
			owner.RemovePolicy(activePolicy);
		}
		if (owner.ActivePolicies?.Any(candidate => candidate != null
			&& string.Equals(candidate.StringId ?? string.Empty, matched.PolicyObjectId ?? string.Empty, StringComparison.OrdinalIgnoreCase)) == true)
		{
			PolicySystemLog.Write("Agenda", "external-cancel-policy-retained",
				"policyId=" + normalizedPolicyId + " owner=" + normalizedOwnerKingdomId);
			return false;
		}

		string cancellationReason = string.IsNullOrWhiteSpace(reason)
			? "外交威胁退让，政策被废止"
			: reason.Trim();
		CompleteDynamicPolicyAbolition(
			matched,
			policy,
			cancellationReason,
			"record:" + normalizedPolicyId + ":external_cancel");
		if (!TryGetDynamicPolicyData(matched.PolicyObjectId, out DynamicPolicySaveData stored)
			|| !string.Equals(stored.Status, DynamicPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase))
		{
			PolicySystemLog.Write("Agenda", "external-cancel-state-not-committed",
				"policyId=" + normalizedPolicyId + " owner=" + normalizedOwnerKingdomId);
			return false;
		}
		result = "cancelled";
		PolicySystemLog.Write("Agenda", "external-cancelled",
			"policyId=" + normalizedPolicyId
			+ " policy=" + (matched.PolicyObjectId ?? string.Empty)
			+ " owner=" + normalizedOwnerKingdomId
			+ " name=" + policyName
			+ " reason=" + cancellationReason);
		return true;
	}

	private void InitializeLoadedDynamicPoliciesBeforeNonReadyCleanup()
	{
		int initializedReferences = 0;
		foreach (DynamicPolicySaveData data in LoadDynamicPolicies().Where(x => x != null && ShouldKeepDynamicPolicyRegistered(x.Status)))
		{
			try
			{
				List<PolicyObject> referencedPolicies = new List<PolicyObject>();
				PolicyObject registeredPolicy = MBObjectManager.Instance?.GetObject<PolicyObject>(data.PolicyObjectId);
				if (registeredPolicy != null)
				{
					referencedPolicies.Add(registeredPolicy);
				}
				Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, "");
				PolicyObject activePolicy = owner?.ActivePolicies?.FirstOrDefault(x => x != null
					&& string.Equals(x.StringId ?? "", data.PolicyObjectId ?? "", StringComparison.OrdinalIgnoreCase));
				if (activePolicy != null && !referencedPolicies.Any(x => ReferenceEquals(x, activePolicy)))
				{
					referencedPolicies.Add(activePolicy);
				}
				foreach (PolicyObject decisionPolicy in owner?.UnresolvedDecisions?.OfType<KingdomPolicyDecision>()
					.Select(x => x?.Policy)
					.Where(x => x != null && string.Equals(x.StringId ?? "", data.PolicyObjectId ?? "", StringComparison.OrdinalIgnoreCase))
					?? Enumerable.Empty<PolicyObject>())
				{
					if (!referencedPolicies.Any(x => ReferenceEquals(x, decisionPolicy)))
					{
						referencedPolicies.Add(decisionPolicy);
					}
				}
				PolicyObject canonicalPolicy = EnsureDynamicPolicyObject(data);
				if (canonicalPolicy != null && !referencedPolicies.Any(x => ReferenceEquals(x, canonicalPolicy)))
				{
					referencedPolicies.Add(canonicalPolicy);
				}
				foreach (PolicyObject policy in referencedPolicies)
				{
					if (TryInitializeDynamicPolicyObject(policy, data, out _))
					{
						initializedReferences++;
					}
				}
			}
			catch (Exception ex)
			{
				PolicySystemLog.Write("Agenda", "pre-cleanup-policy-restore-failed", "policy=" + (data?.PolicyObjectId ?? "") + " " + ex);
			}
		}
		if (initializedReferences > 0)
		{
			PolicySystemLog.Write("Agenda", "pre-cleanup-policy-restore-complete", "references=" + initializedReferences.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		PolicyTargetSemanticRouter.MarkStructureDirty();
		ApplyPolicySettlementModelPatchesOnce();
		ApplyPolicyFinanceModelPatchesOnce();
		ApplyPolicyClanPoliticsModelPatchesOnce();
		ApplyPolicyArmyFormationPatchesOnce();
		ApplyPolicyPartySizeLimitPatchesOnce();
		ApplyPolicyVillageRaidBanPatchesOnce();
		ReconcilePendingVassalExternalCommits();
		RemoveLegacyStoppedDynamicPolicyMembershipAfterLoad();
		ReconcilePolicyReReviewReplacementsAfterLoad();
		EnsureDynamicPoliciesRegistered(reconcilePending: true);
	}

	private string BuildLocalKingdomAgendaPolicyContext(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)
	{
		string targetKingdomId = ResolveKingdomAgendaTargetKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		string playerKingdomId = Clan.PlayerClan?.Kingdom?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(targetKingdomId)
			|| string.IsNullOrWhiteSpace(playerKingdomId)
			|| !string.Equals(targetKingdomId, playerKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		List<(LocalPolicyRecordSaveData Record, List<Settlement> Fiefs)> active = LoadLocalPolicyRecords()
			.Where(record => record != null
				&& string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(record.EffectStatus, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				&& record.MaintenanceFunded
				&& (record.IsPermanentEffect || record.RemainingDays > 0))
			.Select(record => (Record: record, Fiefs: ResolveOwnedLocalPolicyFiefs(record.TargetFiefIds)))
			.Where(item => item.Fiefs.Count > 0)
			.Take(KingdomAgendaLocalPolicyMaxCount)
			.ToList();
		if (active.Count <= 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("本国生效中的地方政策（只读；不可作为议程候选、投票、采纳或废除；作用于发布地及政策明确检索出的本国目标）：");
		foreach ((LocalPolicyRecordSaveData Record, List<Settlement> Fiefs) item in active)
		{
			LocalPolicyRecordSaveData record = item.Record;
			string summary = LimitDisplayChars(CleanPolicyDisplayText(FirstNonEmpty(record.ImpactSummary, record.PolicyContent, "无摘要")), KingdomAgendaLocalPolicySummaryChars);
			string scope = BuildLocalPolicyAgendaScopeText(item.Fiefs);
			string effects = LimitDisplayChars(BuildLocalPolicyAgendaEffectText(record), KingdomAgendaLocalPolicyEffectChars);
			string feedback = LimitDisplayChars(CleanPolicyDisplayText(FirstNonEmpty(record.PublicFeedback, "反馈未明")), KingdomAgendaLocalPolicyFeedbackChars);
			string line = "- 《" + LimitDisplayChars(FirstNonEmpty(record.PolicyName, "未命名地方政策"), KingdomAgendaLocalPolicyNameChars) + "》"
				+ "｜摘要：" + summary
				+ "｜范围：" + scope
				+ "｜每日：" + effects
				+ "｜余" + record.RemainingDays.ToString(CultureInfo.InvariantCulture) + "天"
				+ "｜反馈：" + feedback;
			sb.AppendLine(LimitDisplayChars(line, KingdomAgendaLocalPolicyLineChars));
		}
		return sb.ToString().TrimEnd();
	}

	private static string ResolveKingdomAgendaTargetKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride)
	{
		string targetKingdomId = (kingdomIdOverride ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(targetKingdomId))
		{
			return targetKingdomId;
		}
		return targetHero?.Clan?.Kingdom?.StringId
			?? targetHero?.MapFaction?.StringId
			?? targetCharacter?.HeroObject?.Clan?.Kingdom?.StringId
			?? targetCharacter?.HeroObject?.MapFaction?.StringId
			?? "";
	}

	private static string BuildLocalPolicyAgendaScopeText(List<Settlement> fiefs)
	{
		List<Settlement> valid = ExpandLocalPolicySettlements(fiefs);
		if (valid.Count <= 0)
		{
			return "无当前目标";
		}
		List<string> shown = new List<string>();
		for (int i = 0; i < valid.Count; i++)
		{
			Settlement fief = valid[i];
			string detail = fief.Name?.ToString() ?? fief.StringId ?? "未知封地";
			int remaining = valid.Count - i - 1;
			string candidate = string.Join("、", shown.Concat(new[] { detail }));
			string suffix = remaining > 0 ? "、另有" + remaining.ToString(CultureInfo.InvariantCulture) + "处" : "";
			if (candidate.Length + suffix.Length > KingdomAgendaLocalPolicyScopeChars)
			{
				if (shown.Count == 0)
				{
					shown.Add(LimitDisplayChars(detail, Math.Max(20, KingdomAgendaLocalPolicyScopeChars - suffix.Length)));
				}
				break;
			}
			shown.Add(detail);
		}
		int hiddenCount = valid.Count - shown.Count;
		string result = string.Join("、", shown);
		if (hiddenCount > 0)
		{
			result += (string.IsNullOrWhiteSpace(result) ? "" : "、") + "另有" + hiddenCount.ToString(CultureInfo.InvariantCulture) + "处";
		}
		return LimitDisplayChars(result, KingdomAgendaLocalPolicyScopeChars);
	}

	private string BuildLocalPolicyAgendaEffectText(LocalPolicyRecordSaveData record)
	{
		record = NormalizeLocalPolicyRecord(record);
		if (record?.Effects != null && record.Effects.Count > 0)
		{
			List<Settlement> sourceFiefs = ResolveOwnedLocalPolicyFiefs(record.TargetFiefIds);
			return string.Join("；", record.Effects.Where(x => x != null).Select(effect =>
			{
				bool isMentioned = string.Equals(effect.TargetScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase);
				List<Settlement> currentTargets = isMentioned
					? ResolveLocalMentionedPolicySettlements(
						effect.TargetClanIds,
						effect.DirectTargetSettlementIds,
						effect.FollowCurrentRulingClan,
						sourceFiefs)
					: ExpandLocalPolicySettlements(sourceFiefs);
				string label = BuildLocalPolicyEffectTargetLabel(
					effect.TargetScope,
					effect.TargetHandle,
					effect.TargetLabel,
					effect.TargetClanIds,
					effect.DirectTargetSettlementIds,
					effect.FollowCurrentRulingClan,
					currentTargets);
				return label + ":" + BuildLocalPolicyEffectValueText(effect);
			}));
		}
		return "无持续数值变化";
	}

	private static string BuildLocalPolicyEffectValueText(LocalPolicyEffectRecordSaveData effect)
	{
		List<string> values = BuildPlayerVisibleEffectValues(effect?.ModuleEffects);
		return values.Count <= 0 ? "无持续数值变化" : string.Join("/", values);
	}

	private static string MergeKingdomAgendaPolicyContexts(string nationwideContext, string localContext)
	{
		const string policyContextMarker = "【议程相关政策与事件】";
		List<string> nationwideLines = SplitKingdomAgendaPolicyContextLines(nationwideContext, policyContextMarker);
		List<string> localLines = SplitKingdomAgendaPolicyContextLines(localContext, policyContextMarker);
		if (nationwideLines.Count <= 0 && localLines.Count <= 0)
		{
			return "";
		}
		string BuildMergedText()
		{
			return policyContextMarker + Environment.NewLine
				+ string.Join(Environment.NewLine, nationwideLines.Concat(localLines));
		}
		string merged = BuildMergedText().TrimEnd();
		while (merged.Length > KingdomAgendaPolicyContextMaxChars)
		{
			int removableLocalIndex = localLines.FindLastIndex(line => line.StartsWith("- ", StringComparison.Ordinal));
			int localEntryCount = localLines.Count(line => line.StartsWith("- ", StringComparison.Ordinal));
			int removableNationwideIndex = nationwideLines.FindLastIndex(line => line.StartsWith("- ", StringComparison.Ordinal));
			if (removableLocalIndex >= 0 && localEntryCount > 1)
			{
				localLines.RemoveAt(removableLocalIndex);
			}
			else if (removableNationwideIndex >= 0)
			{
				nationwideLines.RemoveAt(removableNationwideIndex);
			}
			else if (removableLocalIndex >= 0)
			{
				localLines.RemoveAt(removableLocalIndex);
			}
			else
			{
				break;
			}
			merged = BuildMergedText().TrimEnd();
		}
		return merged.Length <= KingdomAgendaPolicyContextMaxChars
			? merged
			: policyContextMarker;
	}

	private static List<string> SplitKingdomAgendaPolicyContextLines(string context, string marker)
	{
		string text = (context ?? "").Trim();
		if (text.StartsWith(marker, StringComparison.Ordinal))
		{
			text = text.Substring(marker.Length).TrimStart();
		}
		return text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Trim())
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.ToList();
	}

	private void ReconcileDynamicPolicyEffectBindingsAfterLoad()
	{
		RemoveQuarantinedDynamicPolicyMembershipAfterLoad();
		Dictionary<string, ActivePolicyEffectSaveData> validEffects = new Dictionary<string, ActivePolicyEffectSaveData>(
			StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			if (_quarantinedActivePolicyEffectIds.Contains(item.Key))
			{
				continue;
			}
			try
			{
				ActivePolicyEffectSaveData effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(item.Value ?? string.Empty);
				if (effect == null || !string.Equals(
					(item.Key ?? string.Empty).Trim(),
					(effect.EffectId ?? string.Empty).Trim(),
					StringComparison.OrdinalIgnoreCase))
				{
					QuarantineActivePolicyEffect(item.Key, item.Value, "load reconciliation: store key does not match effectId");
					continue;
				}
				if (!TryValidateLoadedPolicyEffectBundle(effect, out bool lifecycleChanged, out string validationError))
				{
					QuarantineActivePolicyEffect(item.Key, item.Value, "load reconciliation: " + validationError);
					continue;
				}
				if (lifecycleChanged)
				{
					_activePolicyEffects[item.Key] = JsonConvert.SerializeObject(effect);
				}
				validEffects[effect.EffectId] = effect;
			}
			catch (Exception ex)
			{
				QuarantineActivePolicyEffect(item.Key, item.Value, "load reconciliation parse: " + ex.Message);
			}
		}

		foreach (DynamicPolicySaveData data in LoadDynamicPolicies().Where(item => item != null).ToList())
		{
			bool live = string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
				|| (string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase));
			if (!live)
			{
				continue;
			}
			List<ActivePolicyEffectSaveData> candidates = validEffects.Values
				.Where(effect => string.Equals(effect.RecordId ?? string.Empty, data.RecordId ?? string.Empty,
					StringComparison.OrdinalIgnoreCase))
				.ToList();
			bool blockedByQuarantine = HasAmbiguousQuarantinedActiveEffectForRecord(
				data.RecordId,
				data.RequiresEffectBundle ? data.ActiveEffectId : string.Empty);
			if (data.RequiresEffectBundle && !string.IsNullOrWhiteSpace(data.ActiveEffectId))
			{
				bool exact = candidates.Any(effect => string.Equals(effect.EffectId, data.ActiveEffectId, StringComparison.OrdinalIgnoreCase));
				if (!exact || blockedByQuarantine)
				{
					BlockDynamicPolicyEffectBinding(data, "required active effect bundle is missing, quarantined, or belongs to another record");
				}
				else if (!string.Equals(data.CommitState, PolicyCommitStateExternalCommitPending, StringComparison.Ordinal))
				{
					data.CommitState = PolicyCommitStateActive;
					StoreDynamicPolicy(data);
				}
				continue;
			}

			bool legacyAmbiguous = string.Equals(data.CommitState, PolicyCommitStateCommitPending, StringComparison.Ordinal)
				|| (string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase) && candidates.Count > 0);
			if (!legacyAmbiguous)
			{
				continue;
			}
			HashSet<string> provedEffectIds = CollectPersistedEffectIdsForRecord(data.RecordId);
			List<ActivePolicyEffectSaveData> provedCandidates = candidates
				.Where(effect => provedEffectIds.Contains(effect.EffectId ?? string.Empty))
				.ToList();
			ActivePolicyEffectSaveData unique = provedCandidates.Count == 1
				? provedCandidates[0]
				: provedCandidates.Count == 0 && candidates.Count == 1
					? candidates[0]
					: null;
			if (unique != null && !blockedByQuarantine)
			{
				data.ActiveEffectId = unique.EffectId;
				data.RequiresEffectBundle = true;
				data.CommitState = string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
					? PolicyCommitStateCommitPending
					: PolicyCommitStateActive;
				StoreDynamicPolicy(data);
				continue;
			}
			if (candidates.Count == 0 && !blockedByQuarantine && CanProveLegacyDynamicPolicyNeedsNoEffectBundle(data))
			{
				data.ActiveEffectId = string.Empty;
				data.RequiresEffectBundle = false;
				data.CommitState = PolicyCommitStateActive;
				StoreDynamicPolicy(data);
				continue;
			}
			BlockDynamicPolicyEffectBinding(data, "legacy effect binding is missing or not uniquely provable");
		}
	}

	private static bool TryValidateLoadedPolicyEffectBundle(
		ActivePolicyEffectSaveData effect,
		out bool lifecycleChanged,
		out string error)
	{
		lifecycleChanged = false;
		error = string.Empty;
		if (effect == null || effect.Version != 8 || string.IsNullOrWhiteSpace(effect.EffectId)
			|| string.IsNullOrWhiteSpace(effect.RecordId) || !ShouldRetainActivePolicyEffect(effect)
			|| effect.ModuleEffects == null || effect.ModuleEffects.Count == 0
			|| effect.ModuleEffects.Any(instance => instance == null))
		{
			error = "active effect identity, schema, or module bundle is incomplete";
			return false;
		}
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> group in effect.ModuleEffects
			.Where(instance => instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion
				&& instance.MechanismKind == PolicyEffectMechanismKind.Linked)
			.GroupBy(instance => (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty),
				StringComparer.Ordinal))
		{
			if (!PolicyEffectMechanismContract.TryValidateLinkedGroup(group, out error))
			{
				return false;
			}
		}
		if (effect.ModuleEffects.Any(instance => !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out _)))
		{
			error = "active effect references an unavailable module";
			return false;
		}
		if (!PolicyEffectActivationCoordinator.ReconcileMechanismLifecycleStates(
			effect.ModuleEffects, out lifecycleChanged, out error))
		{
			return false;
		}
		if (effect.ModuleEffects.Any(instance => instance.LifecycleState == PolicyEffectLifecycleState.Failed))
		{
			error = "active effect contains a failed executable instance";
			return false;
		}
		return true;
	}

	private HashSet<string> CollectPersistedEffectIdsForRecord(string recordId)
	{
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string id = (recordId ?? string.Empty).Trim();
		if (_policyRecordHistory.TryGetValue(id, out string historyRaw))
		{
			try
			{
				PolicyRecordSaveData history = JsonConvert.DeserializeObject<PolicyRecordSaveData>(historyRaw ?? string.Empty);
				foreach (string effectId in (history?.Effects ?? new List<PolicyRecordEffectSaveData>())
					.Select(effect => effect?.EffectId))
				{
					if (!string.IsNullOrWhiteSpace(effectId)) ids.Add(effectId);
				}
			}
			catch { }
		}
		if (_localPolicyRecords.TryGetValue(id, out string localRaw))
		{
			try
			{
				LocalPolicyRecordSaveData local = JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(localRaw ?? string.Empty);
				if (!string.IsNullOrWhiteSpace(local?.ActiveEffectId)) ids.Add(local.ActiveEffectId);
				foreach (string effectId in (local?.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
					.Select(effect => effect?.ActiveEffectId))
				{
					if (!string.IsNullOrWhiteSpace(effectId)) ids.Add(effectId);
				}
			}
			catch { }
		}
		return ids;
	}

	private static bool CanProveLegacyDynamicPolicyNeedsNoEffectBundle(DynamicPolicySaveData data)
	{
		try
		{
			PendingPlayerPolicyAgendaSaveData pending = JsonConvert.DeserializeObject<PendingPlayerPolicyAgendaSaveData>(
				data?.PlayerPayloadJson ?? string.Empty);
			return pending != null && (pending.ModuleEffects?.Count ?? 0) == 0;
		}
		catch
		{
			return false;
		}
	}

	private void BlockDynamicPolicyEffectBinding(DynamicPolicySaveData data, string reason)
	{
		if (data == null)
		{
			return;
		}
		data.CommitState = PolicyCommitStateQuarantinedBlocked;
		StoreDynamicPolicy(data);
		PolicySystemLog.Failure("Save", "dynamic-policy-binding-quarantined",
			reason,
			"recordId=" + (data.RecordId ?? string.Empty) + " effectId=" + (data.ActiveEffectId ?? string.Empty));
	}

	private bool IsDynamicPolicyEffectBindingExecutable(DynamicPolicySaveData data)
	{
		if (data == null || string.Equals(data.CommitState, PolicyCommitStateQuarantinedBlocked, StringComparison.Ordinal))
		{
			return false;
		}
		if (!data.RequiresEffectBundle)
		{
			return true;
		}
		string effectId = (data.ActiveEffectId ?? string.Empty).Trim();
		if (effectId.Length == 0 || _quarantinedActivePolicyEffectIds.Contains(effectId)
			|| !_activePolicyEffects.TryGetValue(effectId, out string raw))
		{
			return false;
		}
		try
		{
			ActivePolicyEffectSaveData effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(raw ?? string.Empty);
			return effect != null && effect.Version == 8
				&& string.Equals(effect.EffectId, effectId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(effect.RecordId ?? string.Empty, data.RecordId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
				&& !(effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
					.Any(instance => instance?.LifecycleState == PolicyEffectLifecycleState.Failed);
		}
		catch
		{
			return false;
		}
	}

	private void EnsureDynamicPoliciesRegistered(bool reconcilePending)
	{
		List<DynamicPolicySaveData> livePolicies = LoadDynamicPolicies()
			.Where(x => x != null && ShouldKeepDynamicPolicyRegistered(x.Status))
			.ToList();
		foreach (DynamicPolicySaveData data in livePolicies)
		{
			PolicyObject policy = EnsureDynamicPolicyObject(data);
			Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, "");
			PolicyObject activePolicy = owner?.ActivePolicies?.FirstOrDefault(x => x != null
				&& string.Equals(x.StringId, data.PolicyObjectId, StringComparison.OrdinalIgnoreCase));
			bool active = activePolicy != null;
			if (ShouldCancelLegacyPlayerExpiryAgenda(data, active))
			{
				data.Status = DynamicPolicyStatusActive;
				data.NaturalExpiryAgendaRejected = false;
				StoreDynamicPolicy(data);
				NpcRulerPolicyBehavior.UpdatePolicyAgendaStatusForExternal(data.RecordId, DynamicPolicyStatusActive);
				PolicySystemLog.Write("Agenda", "legacy-player-expiry-agenda-cancelled",
					"recordId=" + (data.RecordId ?? string.Empty) + " policy=" + (data.PolicyObjectId ?? string.Empty));
			}
			bool pendingNpcRenewal = false;
			bool hasPendingNpcCommit = string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase)
				&& NpcRulerPolicyBehavior.TryGetPendingPolicyAgendaCommitForExternal(data.RecordId, out pendingNpcRenewal);
			bool hasSuspendedNpcCommit = string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase)
				&& NpcRulerPolicyBehavior.IsPolicyAgendaCommitSuspendedForExternal(data.RecordId);
			bool hasPendingNpcAdoptionCommit = hasPendingNpcCommit
				&& !pendingNpcRenewal
				&& string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase);
			bool hasPendingNpcRenewalCommit = hasPendingNpcCommit
				&& pendingNpcRenewal
				&& string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase);
			if (activePolicy != null)
			{
				TryInitializeDynamicPolicyObject(activePolicy, data, out _);
				policy = activePolicy;
			}
			bool expectsPendingDecision = IsDynamicPolicyAgendaPending(data)
				&& !hasPendingNpcAdoptionCommit
				&& !hasPendingNpcRenewalCommit
				&& !hasSuspendedNpcCommit;
			bool expectedInverted = string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase);
			KingdomPolicyDecision unresolvedDecision = null;
			foreach (KingdomPolicyDecision candidate in FindDynamicPolicyDecisions(owner, data.PolicyObjectId))
			{
				PolicyObject loadedDecisionPolicy = candidate.Policy;
				TryInitializeDynamicPolicyObject(loadedDecisionPolicy, data, out _);
				bool repaired = expectsPendingDecision
					&& unresolvedDecision == null
					&& policy != null
					&& TryRebindDynamicPolicyDecision(candidate, policy, expectedInverted)
					&& IsUsableDynamicPolicyDecision(candidate, data.PolicyObjectId, policy, expectedInverted);
				if (repaired)
				{
					unresolvedDecision = candidate;
					continue;
				}
				owner?.RemoveDecision(candidate);
				PolicySystemLog.Write("Agenda", "invalid-or-duplicate-pending-decision-removed", "recordId=" + (data.RecordId ?? "")
					+ " policy=" + (data.PolicyObjectId ?? "")
					+ " expectedInverted=" + expectedInverted);
			}
			if (!reconcilePending || policy == null)
			{
				continue;
			}
			bool bindingExecutable = IsDynamicPolicyEffectBindingExecutable(data);
			bool shouldRestoreActiveMembership = bindingExecutable
				&& (string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
					|| hasPendingNpcAdoptionCommit
					|| hasSuspendedNpcCommit);
			if (active && !bindingExecutable
				&& (string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)))
			{
				owner?.RemovePolicy(activePolicy);
				active = false;
				PolicySystemLog.Failure("Save", "dynamic-policy-membership-blocked",
					"active policy membership was removed because its effect binding is not executable",
					"recordId=" + (data.RecordId ?? string.Empty)
					+ " effectId=" + (data.ActiveEffectId ?? string.Empty)
					+ " commitState=" + (data.CommitState ?? string.Empty));
			}
			if (!active && shouldRestoreActiveMembership && owner != null && !owner.IsEliminated)
			{
				owner.AddPolicy(policy);
				active = owner.ActivePolicies?.Contains(policy) == true;
				if (active)
				{
					PolicySystemLog.Write("Agenda", "active-membership-restored-after-load", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId + " kingdom=" + data.OwnerKingdomId);
				}
			}
			if (unresolvedDecision != null)
			{
				bool membershipStillPending = (string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase) && !active)
					|| (string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase) && active);
				if (membershipStillPending)
				{
					continue;
				}
				owner?.RemoveDecision(unresolvedDecision);
				unresolvedDecision = null;
				PolicySystemLog.Write("Agenda", "resolved-state-pending-decision-removed", "recordId=" + (data.RecordId ?? "") + " policy=" + (data.PolicyObjectId ?? ""));
			}
			if (string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase))
			{
				if (hasSuspendedNpcCommit)
				{
					PolicySystemLog.Failure("Agenda", "npc-commit-suspended-after-load",
						"NPC adoption commit remains suspended; agenda replay is disabled while persisted failure reconciliation resumes.",
						"recordId=" + (data.RecordId ?? string.Empty)
						+ " policy=" + (data.PolicyObjectId ?? string.Empty)
						+ " membership=" + active.ToString(CultureInfo.InvariantCulture));
				}
				else if (hasPendingNpcAdoptionCommit)
				{
					PolicySystemLog.Write("Agenda", "adoption-commit-restored-pending", "recordId=" + data.RecordId
						+ " policy=" + data.PolicyObjectId
						+ " membership=" + active.ToString(CultureInfo.InvariantCulture));
				}
				else if (active)
				{
					CompleteDynamicPolicyAdoption(data, policy);
				}
				else
				{
					if (!TryRestoreDynamicPolicyAgendaAfterLoad(data, policy, owner, isInvertedDecision: false, out string restoreFailure))
					{
						RejectDynamicPolicyAdoption(data, policy, "读档后恢复待处理采用议程失败：" + restoreFailure);
					}
				}
			}
			else if (string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase))
			{
				if (string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase) && active)
				{
					foreach (KingdomPolicyDecision decision in owner.UnresolvedDecisions
						.OfType<KingdomPolicyDecision>()
						.Where(candidate => candidate?.Policy == policy)
						.ToList())
					{
						owner.RemoveDecision(decision);
					}
					data.Status = DynamicPolicyStatusActive;
					data.NaturalExpiryAgendaRejected = false;
					StoreDynamicPolicy(data);
					NpcRulerPolicyBehavior.UpdatePolicyAgendaStatusForExternal(data.RecordId, DynamicPolicyStatusActive);
				}
				else if (hasSuspendedNpcCommit)
				{
					PolicySystemLog.Failure("Agenda", "npc-commit-suspended-after-load",
						"NPC renewal commit remains suspended; agenda replay is disabled while persisted failure reconciliation resumes.",
						"recordId=" + (data.RecordId ?? string.Empty)
						+ " policy=" + (data.PolicyObjectId ?? string.Empty)
						+ " membership=" + active.ToString(CultureInfo.InvariantCulture));
				}
				else if (hasPendingNpcRenewalCommit)
				{
					PolicySystemLog.Write("Agenda", "renewal-commit-restored-pending", "recordId=" + data.RecordId
						+ " policy=" + data.PolicyObjectId
						+ " membership=" + active.ToString(CultureInfo.InvariantCulture));
				}
				else if (active)
				{
					if (!TryRestoreDynamicPolicyAgendaAfterLoad(data, policy, owner, isInvertedDecision: true, out string restoreFailure))
					{
						CompleteNaturalExpiryRenewal(data, policy, "读档恢复续期议程失败，按兼容逻辑保留政策：" + restoreFailure);
					}
				}
				else
				{
					CompleteDynamicPolicyAbolition(data, policy, "读档核对：AF 政策已废除");
				}
			}
			else if (string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
			{
				if (!active)
				{
					CompleteDynamicPolicyAbolition(data, policy, "读档核对：AF 政策已不在有效政策中");
				}
				else if (data.NaturalExpiryAgendaRejected
					&& string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase))
				{
					data.NaturalExpiryAgendaRejected = false;
					StoreDynamicPolicy(data);
				}
				else if (data.NaturalExpiryAgendaRejected)
				{
					CompleteNaturalExpiryRenewal(data, policy, "兼容旧存档：补结算 AF 政策续期");
				}
			}
		}
		if (reconcilePending)
		{
			HashSet<string> remainingLivePolicyIds = new HashSet<string>(
				LoadDynamicPolicies()
					.Where(x => x != null && ShouldKeepDynamicPolicyRegistered(x.Status))
					.Select(x => x.PolicyObjectId)
					.Where(IsDynamicPolicyId),
				StringComparer.OrdinalIgnoreCase);
			RemoveOrphanedDynamicPolicyDecisions(remainingLivePolicyIds);
		}
	}

	private static bool ShouldCancelLegacyPlayerExpiryAgenda(DynamicPolicySaveData data, bool policyIsActive)
	{
		return policyIsActive
			&& string.Equals(data?.Source, "player", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(data?.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsDynamicPolicyAgendaPending(DynamicPolicySaveData data)
	{
		return data != null && (string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase));
	}

	private static List<KingdomPolicyDecision> FindDynamicPolicyDecisions(Kingdom owner, string policyObjectId)
	{
		return owner?.UnresolvedDecisions?.OfType<KingdomPolicyDecision>()
			.Where(x => x?.Policy != null && string.Equals(x.Policy.StringId ?? "", policyObjectId ?? "", StringComparison.OrdinalIgnoreCase))
			.ToList() ?? new List<KingdomPolicyDecision>();
	}

	private static KingdomPolicyDecision FindDynamicPolicyDecision(Kingdom owner, string policyObjectId)
	{
		return FindDynamicPolicyDecisions(owner, policyObjectId).FirstOrDefault();
	}

	private static bool IsUsableDynamicPolicyDecision(
		KingdomPolicyDecision decision,
		string policyObjectId,
		PolicyObject canonicalPolicy,
		bool expectedInverted)
	{
		return decision?.Policy != null
			&& ReferenceEquals(decision.Policy, canonicalPolicy)
			&& string.Equals(decision.Policy.StringId ?? "", policyObjectId ?? "", StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(decision.Policy.Name?.ToString())
			&& IsDynamicPolicyDecisionInverted(decision) == expectedInverted;
	}

	private static bool TryRebindDynamicPolicyDecision(KingdomPolicyDecision decision, PolicyObject policy, bool expectedInverted)
	{
		if (decision == null || policy == null)
		{
			return false;
		}
		if (IsDynamicPolicyDecisionInverted(decision) != expectedInverted)
		{
			return false;
		}
		try
		{
			PolicyObject previousPolicy = decision.Policy;
			if (!ReferenceEquals(previousPolicy, policy))
			{
				if (DynamicPolicyDecisionPolicyField == null)
				{
					return false;
				}
				DynamicPolicyDecisionPolicyField.SetValue(decision, policy);
				if (!ReferenceEquals(decision.Policy, policy))
				{
					return false;
				}
			}
			if (DynamicPolicyDecisionSnapshotField == null)
			{
				return false;
			}
			List<PolicyObject> policySnapshot = DynamicPolicyDecisionSnapshotField.GetValue(decision) as List<PolicyObject>;
			if (policySnapshot == null)
			{
				policySnapshot = new List<PolicyObject>();
				DynamicPolicyDecisionSnapshotField.SetValue(decision, policySnapshot);
			}
			policySnapshot.RemoveAll(x => x != null && (ReferenceEquals(x, previousPolicy)
				|| string.Equals(x.StringId ?? "", policy.StringId ?? "", StringComparison.OrdinalIgnoreCase)));
			if (expectedInverted)
			{
				policySnapshot.Add(policy);
			}
			return ReferenceEquals(decision.Policy, policy);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "pending-decision-rebind-failed", "policy=" + (policy.StringId ?? "") + " " + ex.Message);
			return false;
		}
	}

	private static bool IsDynamicPolicyDecisionInverted(KingdomPolicyDecision decision)
	{
		try
		{
			return decision != null && DynamicPolicyDecisionInvertedField?.GetValue(decision) is bool value && value;
		}
		catch
		{
			return false;
		}
	}

	private bool TryRestoreDynamicPolicyAgendaAfterLoad(
		DynamicPolicySaveData data,
		PolicyObject policy,
		Kingdom owner,
		bool isInvertedDecision,
		out string failureReason)
	{
		failureReason = "";
		Clan proposer = ResolveClanById(data?.ProposerClanId) ?? owner?.RulingClan;
		if (data == null || policy == null || owner == null || owner.IsEliminated || proposer == null || proposer.Kingdom != owner)
		{
			failureReason = "政策所属王国或提案氏族无效";
			return false;
		}
		KingdomPolicyDecision existingDecision = FindDynamicPolicyDecision(owner, data.PolicyObjectId);
		if (IsUsableDynamicPolicyDecision(existingDecision, data.PolicyObjectId, policy, isInvertedDecision))
		{
			return true;
		}
		if (existingDecision != null)
		{
			owner.RemoveDecision(existingDecision);
		}
		if (owner != Clan.PlayerClan?.Kingdom)
		{
			failureReason = "非玩家王国不存在可恢复的未决议程";
			return false;
		}
		try
		{
			KingdomPolicyDecision decision = new KingdomPolicyDecision(proposer, policy, isInvertedDecision);
			if (!decision.IsAllowed())
			{
				failureReason = "王国规则不允许恢复该政策议程";
				return false;
			}
			owner.AddDecision(decision, ignoreInfluenceCost: true);
			KingdomPolicyDecision restoredDecision = FindDynamicPolicyDecision(owner, data.PolicyObjectId);
			if (!IsUsableDynamicPolicyDecision(restoredDecision, data.PolicyObjectId, policy, isInvertedDecision))
			{
				if (restoredDecision != null)
				{
					owner.RemoveDecision(restoredDecision);
				}
				failureReason = "恢复后的政策议程未被王国保留";
				return false;
			}
			PolicySystemLog.Write("Agenda", "pending-agenda-restored-after-load", "recordId=" + (data.RecordId ?? "")
				+ " policy=" + (data.PolicyObjectId ?? "")
				+ " inverted=" + isInvertedDecision);
			return true;
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicySystemLog.Write("Agenda", "pending-agenda-restore-failed", "recordId=" + (data?.RecordId ?? "") + " " + ex);
			return false;
		}
	}

	private static void RemoveOrphanedDynamicPolicyDecisions(HashSet<string> livePolicyIds)
	{
		foreach (Kingdom kingdom in Kingdom.All?.Where(x => x != null).ToList() ?? new List<Kingdom>())
		{
			foreach (KingdomPolicyDecision decision in kingdom.UnresolvedDecisions?.OfType<KingdomPolicyDecision>().ToList()
				?? new List<KingdomPolicyDecision>())
			{
				string policyId = decision?.Policy?.StringId ?? "";
				if (!IsDynamicPolicyId(policyId) || livePolicyIds.Contains(policyId))
				{
					continue;
				}
				kingdom.RemoveDecision(decision);
				PolicySystemLog.Write("Agenda", "orphaned-pending-decision-removed", "policy=" + policyId + " kingdom=" + (kingdom.StringId ?? ""));
			}
		}
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		try
		{
			KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
			PolicyObject policy = policyDecision?.Policy;
			if (policy == null || !IsDynamicPolicyId(policy.StringId) || !TryGetDynamicPolicyData(policy.StringId, out DynamicPolicySaveData data))
			{
				return;
			}
			bool active = decision.Kingdom?.ActivePolicies?.Contains(policy) == true;
			if (string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase))
			{
				if (active)
				{
					PolicySystemLog.Lifecycle(
						string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase) ? "Npc" : "Player",
						"agenda-approved",
						"approved",
						new PolicyLogContext
						{
							TransactionId = data.RecordId + ":adoption",
							PolicyId = data.PolicyObjectId,
							RecordId = data.RecordId,
							TargetHash = data.OwnerKingdomId,
							TargetCount = 1,
							StateBefore = DynamicPolicyStatusPending,
							StateAfter = "approved"
						});
					if (string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase))
					{
						BeginPolicyApprovalResultSequence(data.PolicyObjectId);
					}
					CompleteDynamicPolicyAdoption(data, policy);
				}
				else
				{
					RejectDynamicPolicyAdoption(data, policy, "AF 议程否决");
				}
				return;
			}
			if (!active)
			{
				CompleteDynamicPolicyAbolition(data, policy, "AF 议程废除通过");
			}
			else if (string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase))
			{
				if (string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase))
				{
					data.Status = DynamicPolicyStatusActive;
					data.NaturalExpiryAgendaRejected = false;
					StoreDynamicPolicy(data);
				}
				else
				{
					CompleteNaturalExpiryRenewal(data, policy, "AF 政策续期通过");
				}
				PolicySystemLog.Write("Agenda", "expiry-abolition-rejected", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "decision-concluded-failed", ex.ToString());
		}
	}

	private void CompleteDynamicPolicyAbolition(
		DynamicPolicySaveData data,
		PolicyObject policy,
		string reason,
		string lifecycleEventKey = null)
	{
		if (data == null)
		{
			return;
		}
		data.Status = DynamicPolicyStatusAbolished;
		StoreDynamicPolicy(data);
		EndPolicyEffectsForAgendaAbolition(data.RecordId, reason, lifecycleEventKey);
		NpcRulerPolicyBehavior.UpdatePolicyAgendaStatusForExternal(data.RecordId, DynamicPolicyStatusAbolished);
		TryUnregisterDynamicPolicyObject(data, policy);
		PolicySystemLog.Write("Agenda", "abolished", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId + " reason=" + (reason ?? ""));
	}

	private void OnKingdomDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
	{
		try
		{
			PolicyObject policy = (decision as KingdomPolicyDecision)?.Policy;
			if (policy == null || !TryGetDynamicPolicyData(policy.StringId, out DynamicPolicySaveData data))
			{
				return;
			}
			if (string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase))
			{
				RejectDynamicPolicyAdoption(data, policy, "AF 议程取消");
			}
			else if (string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase))
			{
				PolicySystemLog.Write("Agenda", "renewal-agenda-cancelled", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId);
				if (string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase))
				{
					data.Status = DynamicPolicyStatusActive;
					data.NaturalExpiryAgendaRejected = false;
					StoreDynamicPolicy(data);
				}
				else
				{
					ExpireDynamicPolicyWithoutRenewal(data, policy, "AF 政策续期议程取消，政策到期终止");
				}
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "decision-cancelled-failed", ex.ToString());
		}
	}

	private bool TrySubmitDynamicPolicyAgenda(DynamicPolicySaveData data, out string failureReason)
	{
		failureReason = "";
		Kingdom owner = ResolveKingdomByIdOrName(data?.OwnerKingdomId, "");
		Clan proposer = ResolveClanById(data?.ProposerClanId) ?? owner?.RulingClan;
		if (data == null || owner == null || owner.IsEliminated || proposer == null || proposer.Kingdom != owner)
		{
			failureReason = "政策所属王国或提案氏族无效";
			return false;
		}
		PolicyObject policy = EnsureDynamicPolicyObject(data);
		if (policy == null)
		{
			failureReason = "动态 PolicyObject 注册失败";
			return false;
		}
		if (owner.ActivePolicies.Contains(policy) || owner.UnresolvedDecisions.OfType<KingdomPolicyDecision>().Any(x => x?.Policy == policy && !x.ShouldBeCancelled()))
		{
			failureReason = "同一政策已经生效或正在议程中";
			return false;
		}
		data.Status = DynamicPolicyStatusPending;
		StoreDynamicPolicy(data);
		KingdomPolicyDecision decision = new KingdomPolicyDecision(proposer, policy, isInvertedDecision: false);
		float reviewDays = GetDynamicPolicyAdoptionReviewDays(owner);
		if (!decision.IsAllowed())
		{
			failureReason = "王国规则不允许提交该政策议程";
			data.Status = DynamicPolicyStatusRejected;
			StoreDynamicPolicy(data);
			TryUnregisterDynamicPolicyObject(data, policy);
			return false;
		}
		if (!TryConfigureDynamicPolicyAdoptionReviewTime(decision, reviewDays, out string reviewTimeError))
		{
			failureReason = reviewTimeError;
			data.Status = DynamicPolicyStatusRejected;
			StoreDynamicPolicy(data);
			TryUnregisterDynamicPolicyObject(data, policy);
			PolicySystemLog.Write("Agenda", "review-time-config-failed", "recordId=" + (data.RecordId ?? "") + " policy=" + (data.PolicyObjectId ?? "") + " reason=" + reviewTimeError);
			return false;
		}
		owner.AddDecision(decision, ignoreInfluenceCost: true);
		if (owner.UnresolvedDecisions == null || !owner.UnresolvedDecisions.Contains(decision))
		{
			failureReason = "AF 议程未保留政策决定";
			data.Status = DynamicPolicyStatusRejected;
			StoreDynamicPolicy(data);
			TryUnregisterDynamicPolicyObject(data, policy);
			return false;
		}
		// Other agenda patches can change TriggerTime while Kingdom.AddDecision is running.
		// Re-apply and verify the AF adoption deadline after all AddDecision patches return.
		if (!TryConfigureDynamicPolicyAdoptionReviewTime(decision, reviewDays, out string postAddReviewTimeError))
		{
			failureReason = postAddReviewTimeError;
			try
			{
				owner.RemoveDecision(decision);
			}
			catch (Exception removeEx)
			{
				failureReason += "；移除无效 AF 议程失败：" + removeEx.Message;
			}
			bool stillQueued = owner.UnresolvedDecisions?.Contains(decision) == true;
			data.Status = DynamicPolicyStatusRejected;
			StoreDynamicPolicy(data);
			if (!stillQueued)
			{
				TryUnregisterDynamicPolicyObject(data, policy);
			}
			PolicySystemLog.Write("Agenda", "review-time-post-add-verify-failed",
				"recordId=" + (data.RecordId ?? "") + " policy=" + (data.PolicyObjectId ?? "")
				+ " stillQueued=" + stillQueued + " reason=" + failureReason);
			return false;
		}
		PolicySystemLog.Lifecycle(
			string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase) ? "Npc" : "Player",
			"agenda-submitted",
			"pending",
			new PolicyLogContext
			{
				TransactionId = data.RecordId + ":adoption",
				PolicyId = data.PolicyObjectId,
				RecordId = data.RecordId,
				TargetHash = data.OwnerKingdomId,
				TargetCount = 1,
				StateBefore = "generated",
				StateAfter = DynamicPolicyStatusPending,
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["reviewTenthsOfDay"] = Math.Max(0, (int)Math.Round(reviewDays * 10f))
				}
			});
		return true;
	}

	private static float GetDynamicPolicyAdoptionReviewDays(Kingdom owner)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		return owner != null && playerKingdom != null
			&& (ReferenceEquals(owner, playerKingdom)
				|| string.Equals(owner.StringId ?? "", playerKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			? PlayerKingdomDynamicPolicyAdoptionReviewDays
			: ForeignKingdomDynamicPolicyAdoptionReviewDays;
	}

	internal static float GetDynamicPolicyAdoptionReviewDaysForExternal(Kingdom owner)
	{
		return GetDynamicPolicyAdoptionReviewDays(owner);
	}

	private static bool TryConfigureDynamicPolicyAdoptionReviewTime(KingdomPolicyDecision decision, float reviewDays, out string failureReason)
	{
		failureReason = "";
		if (decision == null)
		{
			failureReason = "AF 政策议程决定为空";
			return false;
		}
		try
		{
			System.Reflection.PropertyInfo triggerTimeProperty = AccessTools.Property(typeof(KingdomDecision), nameof(KingdomDecision.TriggerTime));
			System.Reflection.MethodInfo setter = triggerTimeProperty?.GetSetMethod(nonPublic: true);
			if (setter == null)
			{
				failureReason = "无法访问 KingdomDecision.TriggerTime setter";
				return false;
			}
			CampaignTime triggerTime = CampaignTime.DaysFromNow(reviewDays);
			setter.Invoke(decision, new object[1] { triggerTime });
			float remainingDays = decision.TriggerTime.RemainingDaysFromNow;
			if (float.IsNaN(remainingDays) || float.IsInfinity(remainingDays) || Math.Abs(remainingDays - reviewDays) > 0.05f)
			{
				failureReason = "AF 政策议程审议时间验证失败，实际剩余天数=" + remainingDays.ToString("0.###", CultureInfo.InvariantCulture);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			failureReason = "设置 AF 政策议程 " + reviewDays.ToString("0.#", CultureInfo.InvariantCulture) + " 天审议时间失败：" + ex.Message;
			return false;
		}
	}

	private void CompleteDynamicPolicyAdoption(DynamicPolicySaveData data, PolicyObject policy)
	{
		bool committed = true;
		if (string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase))
		{
			committed = NpcRulerPolicyBehavior.OnPolicyAgendaApprovedForExternal(data.RecordId, isRenewal: false);
			if (committed)
			{
				StoreDynamicPolicy(data);
				PolicySystemLog.Write("Agenda", "adoption-commit-pending", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId);
				return;
			}
		}
		else
		{
			committed = CompleteApprovedPlayerPolicy(data);
		}
		if (!committed)
		{
			Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, string.Empty);
			if (owner != null && policy != null && owner.ActivePolicies.Contains(policy))
			{
				owner.RemovePolicy(policy);
			}
			RejectDynamicPolicyAdoption(data, policy, "AF 政策批准后的原子提交失败，已撤销采用");
			PolicySystemLog.Write("Agenda", "adoption-commit-reverted",
				"recordId=" + (data.RecordId ?? string.Empty)
				+ " policy=" + (data.PolicyObjectId ?? string.Empty));
			if (string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase))
			{
				ShowPolicyCommitFailureResultPopup(data.PolicyObjectId);
			}
			return;
		}
		data.Status = DynamicPolicyStatusActive;
		data.CommitState = PolicyCommitStateActive;
		StoreDynamicPolicy(data);
		if (string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase)
			&& !FinalizePolicyReReviewReplacement(data.RecordId))
		{
			PolicySystemLog.Failure("ReReview", "kingdom-replacement-reconciliation-pending",
				"新王国政策已通过并原子提交，但旧谱系停止步骤将依靠读档对账重试。",
				"recordId=" + (data.RecordId ?? string.Empty));
		}
		PolicySystemLog.Transaction(data.RecordId + ":adoption", data.RecordId, data.ActiveEffectId, string.Empty,
			"active", "success", stateBefore: PolicyCommitStateCommitPending, stateAfter: PolicyCommitStateActive);
		PolicySystemLog.Write("Agenda", "adopted", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId);
	}

	private void CompleteNaturalExpiryRenewal(DynamicPolicySaveData data, PolicyObject policy, string reason)
	{
		if (data == null)
		{
			return;
		}
		bool renewalStarted = true;
		if (string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase))
		{
			renewalStarted = NpcRulerPolicyBehavior.OnPolicyAgendaApprovedForExternal(data.RecordId, isRenewal: true);
			if (renewalStarted)
			{
				StoreDynamicPolicy(data);
				PolicySystemLog.Write("Agenda", "renewal-commit-pending", "recordId=" + data.RecordId
					+ " policy=" + data.PolicyObjectId
					+ " reason=" + (reason ?? string.Empty));
				return;
			}
		}
		else
		{
			renewalStarted = CompleteApprovedPlayerPolicy(data, isRenewal: true);
		}
		if (!renewalStarted)
		{
			ExpireDynamicPolicyWithoutRenewal(data, policy, "AF 政策续期结算失败，政策到期终止");
			return;
		}
		data.Status = DynamicPolicyStatusActive;
		data.CommitState = PolicyCommitStateActive;
		data.NaturalExpiryAgendaRejected = false;
		StoreDynamicPolicy(data);
		PolicySystemLog.Write("Agenda", "renewal-committed", "recordId=" + data.RecordId
			+ " policy=" + data.PolicyObjectId
			+ " source=" + (data.Source ?? "")
			+ " reason=" + (reason ?? ""));
	}

	private bool TryCompleteNpcPolicyEffectBundleCommit(
		string recordId,
		string activeEffectId,
		bool isRenewal,
		out string failureReason)
	{
		failureReason = string.Empty;
		string id = (recordId ?? string.Empty).Trim();
		DynamicPolicySaveData data = LoadDynamicPolicies().FirstOrDefault(item => item != null
			&& string.Equals((item.RecordId ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase));
		if (data == null || !string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "NPC dynamic policy record is missing";
			return false;
		}
		string expectedStatus = isRenewal ? DynamicPolicyStatusExpiryVotePending : DynamicPolicyStatusPending;
		if (!string.Equals(data.Status, expectedStatus, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "NPC dynamic policy commit state is incompatible: " + (data.Status ?? string.Empty);
			return false;
		}
		Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, string.Empty);
		PolicyObject policy = owner?.ActivePolicies?.FirstOrDefault(item => item != null
			&& string.Equals(item.StringId ?? string.Empty, data.PolicyObjectId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (owner == null || owner.IsEliminated || policy == null)
		{
			failureReason = "approved NPC dynamic policy is not present in its owner ActivePolicies";
			return false;
		}
		if (string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			return !data.RequiresEffectBundle
				|| string.Equals(data.ActiveEffectId ?? string.Empty, activeEffectId ?? string.Empty, StringComparison.Ordinal);
		}
		data.ActiveEffectId = activeEffectId ?? string.Empty;
		data.RequiresEffectBundle = true;
		data.CommitState = PolicyCommitStateActive;
		data.Status = DynamicPolicyStatusActive;
		if (isRenewal)
		{
			data.NaturalExpiryAgendaRejected = false;
		}
		StoreDynamicPolicy(data);
		PolicySystemLog.Write("Agenda", isRenewal ? "renewal-committed" : "adopted",
			"recordId=" + data.RecordId + " policy=" + data.PolicyObjectId + " source=npc callback=bundle-confirmed");
		return true;
	}

	private bool TryFailNpcPolicyEffectBundleCommit(
		string recordId,
		bool isRenewal,
		string reason,
		out string failureReason)
	{
		failureReason = string.Empty;
		string id = (recordId ?? string.Empty).Trim();
		DynamicPolicySaveData data = LoadDynamicPolicies().FirstOrDefault(item => item != null
			&& string.Equals((item.RecordId ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase));
		if (data == null)
		{
			PolicySystemLog.Write("Agenda", "npc-bundle-failure-callback-noop", "recordId=" + id + " reason=dynamic-policy-missing");
			return true;
		}
		if (!string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "dynamic policy record source is not NPC";
			return false;
		}
		if (string.Equals(data.Status, DynamicPolicyStatusRejected, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(data.Status, DynamicPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		PolicyObject policy = EnsureDynamicPolicyObject(data);
		string commitFailure = string.IsNullOrWhiteSpace(reason)
			? "NPC policy authoritative bundle commit failed"
			: reason.Trim();
		if (isRenewal)
		{
			if (!string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
			{
				failureReason = "NPC renewal failure callback state is incompatible: " + (data.Status ?? string.Empty);
				return false;
			}
			Kingdom renewalOwner = ResolveKingdomByIdOrName(data.OwnerKingdomId, string.Empty);
			PolicyObject activeRenewalPolicy = renewalOwner?.ActivePolicies?.FirstOrDefault(item => item != null
				&& string.Equals(item.StringId ?? string.Empty, data.PolicyObjectId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
			if (activeRenewalPolicy != null)
			{
				policy = activeRenewalPolicy;
			}
			ExpireDynamicPolicyWithoutRenewal(data, policy, commitFailure);
			return true;
		}
		if (!string.Equals(data.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "NPC adoption failure callback state is incompatible: " + (data.Status ?? string.Empty);
			return false;
		}
		Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, string.Empty);
		PolicyObject activePolicy = owner?.ActivePolicies?.FirstOrDefault(item => item != null
			&& string.Equals(item.StringId ?? string.Empty, data.PolicyObjectId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (activePolicy != null)
		{
			owner.RemovePolicy(activePolicy);
			policy = activePolicy;
		}
		RejectDynamicPolicyAdoption(data, policy, commitFailure);
		return true;
	}

	private void ExpireDynamicPolicyWithoutRenewal(DynamicPolicySaveData data, PolicyObject policy, string reason)
	{
		Kingdom owner = ResolveKingdomByIdOrName(data?.OwnerKingdomId, "");
		if (owner != null && policy != null && owner.ActivePolicies.Contains(policy))
		{
			owner.RemovePolicy(policy);
		}
		CompleteDynamicPolicyAbolition(data, policy, reason);
	}

	private void RejectDynamicPolicyAdoption(DynamicPolicySaveData data, PolicyObject policy, string reason)
	{
		data.Status = DynamicPolicyStatusRejected;
		StoreDynamicPolicy(data);
		if (string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase))
		{
			NpcRulerPolicyBehavior.OnPolicyAgendaRejectedForExternal(data.RecordId, reason);
		}
		TryUnregisterDynamicPolicyObject(data, policy);
		PolicySystemLog.Lifecycle(
			string.Equals(data.Source, "npc", StringComparison.OrdinalIgnoreCase) ? "Npc" : "Player",
			"agenda-rejected",
			"rejected",
			new PolicyLogContext
			{
				TransactionId = data.RecordId + ":adoption",
				PolicyId = data.PolicyObjectId,
				RecordId = data.RecordId,
				StateBefore = DynamicPolicyStatusPending,
				StateAfter = DynamicPolicyStatusRejected,
				MessageChars = reason?.Length ?? 0,
				MessageHash = PolicySystemLog.HashSensitive(reason)
			});
	}

	private bool TryBuildApprovedPlayerPolicyPostprocessFromPending(
		DynamicPolicySaveData data,
		PendingPlayerPolicyAgendaSaveData pending,
		PolicyDraftRequest request,
		PolicyMainAssessmentResult assessment,
		out PolicyPostprocessResult postprocess,
		out string error)
	{
		postprocess = null;
		error = string.Empty;
		bool hasCanonicalModuleEffects = pending?.ModuleEffects != null;
		if (!hasCanonicalModuleEffects)
		{
			error = "旧待审政策缺少可重新证明的 canonical ModuleEffects";
			return false;
		}
		List<PolicyEffectInstanceSaveData> submittedModuleEffects = ClonePolicyEffectSaveDataList(pending.ModuleEffects);
		if (!TryValidatePendingPlayerPolicySubmittedTargetAuthorization(
			submittedModuleEffects,
			request,
			out error))
		{
			return false;
		}
		List<PolicyEffectDto> executableWireEffects;
		List<PolicyEffectInstanceSaveData> inertModuleEffects;
		if (!TryReadPendingPlayerPolicyCanonicalEffects(
			pending.ModuleEffects,
			request,
			out executableWireEffects,
			out inertModuleEffects,
			out error))
		{
			return false;
		}

		if (!TryResolvePendingPlayerPolicyModuleAllowlists(
			pending,
			request.ScopeKind ?? PolicyScopeKingdom,
			executableWireEffects,
			out List<string> candidateModuleIds,
			out List<string> detailedModuleIds,
			out error))
		{
			return false;
		}
		if (!TryCompileSparsePolicyEffects(
			request,
			assessment.DurationDays,
			executableWireEffects,
			candidateModuleIds,
			detailedModuleIds,
			out List<PolicyEffectDto> compiledEffects,
			out error,
			allowAlreadyCompiled: false))
		{
			return false;
		}
		assessment.Effects = compiledEffects;
		assessment.UsesSparseEffectIr = true;
		assessment.EffectIrValidationError = string.Empty;
		postprocess = BuildPostprocessResultFromMainAssessment(request, assessment);

		if (!TryBuildPendingPlayerPolicyModuleEffects(
			postprocess.Effects,
			out List<PolicyEffectInstanceSaveData> refreshedModuleEffects,
			out error))
		{
			return false;
		}
		refreshedModuleEffects.AddRange(ClonePolicyEffectSaveDataList(inertModuleEffects));
		if (refreshedModuleEffects.Count > MaxCompiledPolicyEffectInstances)
		{
			error = "待审规范效果实例超过 " + MaxCompiledPolicyEffectInstances.ToString(CultureInfo.InvariantCulture) + " 个";
			return false;
		}
		if (!TryValidatePendingPlayerPolicyTargetSnapshot(
			submittedModuleEffects,
			refreshedModuleEffects,
			out error))
		{
			return false;
		}
		pending.Version = 5;
		pending.ModuleEffects = refreshedModuleEffects;
		pending.CandidateModuleIds = candidateModuleIds;
		pending.DetailedModuleIds = detailedModuleIds;
		pending.ObjectSnapshot ??= BuildPolicyEffectObjectSnapshot(refreshedModuleEffects, request.ScopeKind ?? PolicyScopeKingdom);
		pending.Assessment = ClonePlayerPolicyAgendaAssessmentWithoutEffects(assessment);
		data.PlayerPayloadJson = JsonConvert.SerializeObject(pending);
		StoreDynamicPolicy(data);
		return true;
	}

	private static bool TryValidatePendingPlayerPolicySubmittedTargetAuthorization(
		IEnumerable<PolicyEffectInstanceSaveData> submitted,
		PolicyDraftRequest request,
		out string error)
	{
		error = string.Empty;
		Dictionary<string, PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request?.TargetHandles)
			.ToDictionary(handle => handle.Key, StringComparer.OrdinalIgnoreCase);
		foreach (PolicyEffectInstanceSaveData instance in submitted ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			if (instance?.TargetSet == null)
			{
				error = "待审政策包含缺失 canonical targetSet 的实例";
				return false;
			}
			List<string> selectorHandles = NormalizeIdList(instance.TargetSet.SelectorHandles);
			if (selectorHandles.Count == 0)
			{
				error = "待审政策实例缺少 canonical selectorHandles";
				return false;
			}
			List<PolicyTargetHandleSaveData> selectedHandles = new List<PolicyTargetHandleSaveData>();
			foreach (string selectorHandle in selectorHandles)
			{
				if (!handles.TryGetValue(selectorHandle, out PolicyTargetHandleSaveData handle)
					|| !IsPolicyTargetHandleAllowedForRequest(request, handle))
				{
					error = "待审政策包含无法由当前原文重新证明的目标句柄：" + selectorHandle;
					return false;
				}
				selectedHandles.Add(handle);
			}
			HashSet<string> expectedSelectorIds = new HashSet<string>(selectedHandles
				.Select(handle => (handle.SelectorId ?? string.Empty).Trim())
				.Where(value => value.Length > 0), StringComparer.Ordinal);
			HashSet<string> submittedSelectorIds = new HashSet<string>(NormalizeIdList(instance.TargetSet.SelectorIds), StringComparer.Ordinal);
			if (!expectedSelectorIds.SetEquals(submittedSelectorIds))
			{
				error = "待审政策的 canonical selectorIds 与目标句柄不一致";
				return false;
			}
			List<PolicyTargetPlanSaveData> submittedPlans = instance.TargetSet.TargetPlans ?? new List<PolicyTargetPlanSaveData>();
			List<PolicyTargetPlanSaveData> normalizedSubmittedPlans = PolicyTargetPlanResolver.NormalizePlans(submittedPlans);
			if (submittedPlans.Count != normalizedSubmittedPlans.Count)
			{
				error = "待审政策包含无效或重复的 canonical TargetPlan";
				return false;
			}
			HashSet<string> expectedPlanSignatures = new HashSet<string>(selectedHandles
				.Where(handle => string.Equals(handle.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
				.Select(handle => PolicyTargetPlanResolver.Clone(handle.TargetPlan)?.NormalizedSignature)
				.Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
			HashSet<string> submittedPlanSignatures = new HashSet<string>(
				normalizedSubmittedPlans.Select(plan => plan.NormalizedSignature),
				StringComparer.Ordinal);
			if (!expectedPlanSignatures.SetEquals(submittedPlanSignatures))
			{
				error = "待审政策的 canonical TargetPlan 与目标句柄不一致";
				return false;
			}
		}
		return true;
	}

	private static bool TryValidatePendingPlayerPolicyTargetSnapshot(
		IEnumerable<PolicyEffectInstanceSaveData> submitted,
		IEnumerable<PolicyEffectInstanceSaveData> refreshed,
		out string error)
	{
		error = string.Empty;
		if (!TryCoalescePolicyEffectShellInstances(
			ClonePolicyEffectSaveDataList(submitted),
			out List<PolicyEffectInstanceSaveData> submittedCanonical,
			out error)
			|| !TryCoalescePolicyEffectShellInstances(
				ClonePolicyEffectSaveDataList(refreshed),
				out List<PolicyEffectInstanceSaveData> refreshedCanonical,
				out error))
		{
			return false;
		}
		if (submittedCanonical.Any(instance => instance?.TargetSet == null)
			|| refreshedCanonical.Any(instance => instance?.TargetSet == null))
		{
			error = "待审政策 canonical targetSet 缺失";
			return false;
		}
		string[] before = submittedCanonical
			.Select(BuildPendingPlayerPolicyTargetIdentity)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToArray();
		string[] after = refreshedCanonical
			.Select(BuildPendingPlayerPolicyTargetIdentity)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToArray();
		if (!before.SequenceEqual(after, StringComparer.Ordinal))
		{
			error = "待审政策的模块谱系、机制身份或 canonical 目标集合已发生漂移";
			return false;
		}
		return true;
	}

	private static string BuildPendingPlayerPolicyTargetIdentity(PolicyEffectInstanceSaveData instance)
	{
		PolicyEffectCanonicalTargetSet targets = instance?.TargetSet;
		if (instance == null || targets == null)
		{
			return "<invalid>";
		}
		JObject identity = new JObject
		{
			["sourceScope"] = instance.SourceScope ?? string.Empty,
			["effectPlanVersion"] = instance.EffectPlanVersion,
			["mechanismId"] = instance.MechanismId ?? string.Empty,
			["mechanismKind"] = instance.MechanismKind.ToString(),
			["mechanismRole"] = instance.MechanismRole.ToString(),
			["sourceOmitted"] = instance.SourceOmitted,
			["destinationOmitted"] = instance.DestinationOmitted,
			["moduleId"] = instance.ModuleId ?? string.Empty,
			["sourceModuleId"] = FirstNonEmpty(instance.SourceModuleId, instance.ModuleId),
			["targetStructureVersion"] = targets.StructureVersion,
			["selectorHandles"] = BuildPendingPlayerPolicyIdentityArray(targets.SelectorHandles, caseInsensitive: false),
			["selectorIds"] = BuildPendingPlayerPolicyIdentityArray(targets.SelectorIds, caseInsensitive: false),
			["targetPlanSignatures"] = new JArray(PolicyTargetPlanResolver.NormalizePlans(targets.TargetPlans)
				.Select(plan => plan.NormalizedSignature)
				.OrderBy(value => value, StringComparer.Ordinal)),
			["settlementIds"] = BuildPendingPlayerPolicyIdentityArray(targets.SettlementIds, caseInsensitive: true),
			["townIds"] = BuildPendingPlayerPolicyIdentityArray(targets.TownIds, caseInsensitive: true),
			["villageIds"] = BuildPendingPlayerPolicyIdentityArray(targets.VillageIds, caseInsensitive: true),
			["parentSettlementIds"] = BuildPendingPlayerPolicyIdentityArray(targets.ParentSettlementIds, caseInsensitive: true),
			["clanIds"] = BuildPendingPlayerPolicyIdentityArray(targets.ClanIds, caseInsensitive: true),
			["kingdomIds"] = BuildPendingPlayerPolicyIdentityArray(targets.KingdomIds, caseInsensitive: true),
			["heroIds"] = BuildPendingPlayerPolicyIdentityArray(targets.HeroIds, caseInsensitive: true),
			["followCurrentRulingClan"] = targets.FollowCurrentRulingClan
		};
		return identity.ToString(Formatting.None);
	}

	private static JArray BuildPendingPlayerPolicyIdentityArray(
		IEnumerable<string> values,
		bool caseInsensitive)
	{
		IEnumerable<string> normalized = (values ?? Array.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0);
		if (caseInsensitive)
		{
			normalized = normalized.Select(value => value.ToLowerInvariant());
		}
		return new JArray(normalized.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal));
	}

	private static bool TryReadPendingPlayerPolicyCanonicalEffects(
		IEnumerable<PolicyEffectInstanceSaveData> sourceEffects,
		PolicyDraftRequest request,
		out List<PolicyEffectDto> executableWireEffects,
		out List<PolicyEffectInstanceSaveData> inertModuleEffects,
		out string error)
	{
		executableWireEffects = new List<PolicyEffectDto>();
		inertModuleEffects = new List<PolicyEffectInstanceSaveData>();
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> source = ClonePolicyEffectSaveDataList(sourceEffects);
		if (source.Count > MaxCompiledPolicyEffectInstances
			|| !TryCoalescePolicyEffectShellInstances(source, out List<PolicyEffectInstanceSaveData> canonicalEffects, out error))
		{
			if (string.IsNullOrWhiteSpace(error))
			{
				error = "待审规范效果实例数量无效";
			}
			return false;
		}
		if (!TryValidateOrFreezeMissingPendingMechanismContracts(canonicalEffects, out error))
		{
			return false;
		}
		string scope = request?.ScopeKind ?? PolicyScopeKingdom;
		foreach (PolicyEffectInstanceSaveData sourceEffect in canonicalEffects)
		{
			string moduleId = (sourceEffect?.ModuleId ?? string.Empty).Trim();
			if (sourceEffect == null || string.IsNullOrWhiteSpace(sourceEffect.InstanceId) || moduleId.Length == 0)
			{
				error = "待审规范效果包含空 instanceId 或 moduleId";
				return false;
			}
			if (!PolicyEffectModuleCatalog.TryGet(moduleId, out IPolicyEffectModule module))
			{
				inertModuleEffects.Add(ClonePolicyEffectSaveData(sourceEffect));
				PolicyDebugLog("agenda-effect-inert", "moduleId=" + moduleId + " reason=unknownModule");
				continue;
			}
			if (!PolicyEffectModuleCatalog.IsAllowedForScope(module, scope))
			{
				error = "待审规范效果模块不支持当前作用域：" + module.Id;
				return false;
			}
			if (sourceEffect.LifecycleState != PolicyEffectLifecycleState.Prepared
				|| sourceEffect.RuntimeState != null
				|| sourceEffect.ExecutionReceipt != null)
			{
				error = "待审规范效果必须处于未执行 Prepared 状态：" + module.Id;
				return false;
			}
			PolicyEffectInstanceSaveData normalizedSource = ClonePolicyEffectSaveData(sourceEffect);
			if (!PolicyEffectSaveCodec.TryNormalizeInstance(
				normalizedSource,
				out PolicyEffectNormalizedInstance normalized,
				out string normalizeError))
			{
				error = "待审规范效果校验失败：" + module.Id + " / " + normalizeError;
				return false;
			}
			if (normalized?.IsInert == true || normalized?.SaveData == null)
			{
				if (string.Equals(normalized?.InertReason, "invalidSourceModuleLineage", StringComparison.Ordinal))
				{
					error = "待审规范效果包含非法源模块谱系："
						+ FirstNonEmpty(sourceEffect.SourceModuleId, "(missing-source)")
						+ " -> " + module.Id;
					return false;
				}
				inertModuleEffects.Add(ClonePolicyEffectSaveData(sourceEffect));
				PolicyDebugLog("agenda-effect-inert", "moduleId=" + module.Id + " reason=" + (normalized?.InertReason ?? "invalid"));
				continue;
			}
			List<string> targetHandles = NormalizeIdList(normalized.SaveData.TargetSet?.SelectorHandles);
			if (targetHandles.Count <= 0)
			{
				error = "待审规范效果缺少 canonical selectorHandles：" + module.Id;
				return false;
			}
			executableWireEffects.Add(new PolicyEffectDto
			{
				EffectPlanVersion = normalized.SaveData.EffectPlanVersion,
				MechanismId = normalized.SaveData.MechanismId ?? string.Empty,
				MechanismKind = normalized.SaveData.MechanismKind,
				MechanismRole = normalized.SaveData.MechanismRole,
				SourceOmitted = normalized.SaveData.SourceOmitted,
				DestinationOmitted = normalized.SaveData.DestinationOmitted,
				ModuleId = module.Id,
				SourceModuleId = FirstNonEmpty(normalized.SaveData.SourceModuleId, module.Id),
				TargetHandles = targetHandles,
				Payload = normalized.SaveData.Payload?.DeepClone(),
				Reason = normalized.SaveData.Reason ?? string.Empty
			});
		}
		return true;
	}

	private static bool TryValidateOrFreezeMissingPendingMechanismContracts(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		out string error)
	{
		error = string.Empty;
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> group in (instances
			?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null
				&& instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion
				&& instance.MechanismKind == PolicyEffectMechanismKind.Linked)
			.GroupBy(instance => (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty),
				StringComparer.Ordinal))
		{
			List<PolicyEffectInstanceSaveData> legs = group.ToList();
			bool allMissing = legs.All(leg => leg.MechanismContractVersion == 0
				&& string.IsNullOrWhiteSpace(leg.MechanismContractHash)
				&& (leg.ExpectedMechanismLegIds == null || leg.ExpectedMechanismLegIds.Count == 0));
			if (allMissing)
			{
				if (!PolicyEffectMechanismContract.TryFreeze(legs, out error))
				{
					return false;
				}
				continue;
			}
			if (!PolicyEffectMechanismContract.TryValidateLinkedGroup(legs, out error))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryReadLegacyPlayerPolicyAgendaEffects(
		IEnumerable<PolicyEffectDto> legacyEffects,
		PolicyDraftRequest request,
		out List<PolicyEffectDto> executableWireEffects,
		out List<PolicyEffectInstanceSaveData> inertModuleEffects,
		out string error)
	{
		executableWireEffects = new List<PolicyEffectDto>();
		inertModuleEffects = new List<PolicyEffectInstanceSaveData>();
		error = string.Empty;
		int ordinal = 0;
		foreach (PolicyEffectDto effect in legacyEffects ?? Enumerable.Empty<PolicyEffectDto>())
		{
			if (effect == null || string.IsNullOrWhiteSpace(effect.ModuleId))
			{
				continue;
			}
			if (effect.Targets != null || effect.Changes != null || HasLegacyPolicyEffectShape(effect)
				|| effect.Payload == null || effect.Payload.Type == JTokenType.Null
				|| effect.TargetHandles == null || effect.TargetHandles.Count <= 0)
			{
				error = "旧待审 effects 尚未经过 PolicyEffectSaveCodec 单向迁移";
				return false;
			}
			PolicyEffectDto wire = new PolicyEffectDto
			{
				ModuleId = effect.ModuleId.Trim(),
				SourceModuleId = FirstNonEmpty(effect.SourceModuleId, effect.ModuleId),
				TargetHandles = NormalizeIdList(effect.TargetHandles),
				Payload = effect.Payload.DeepClone(),
				Reason = effect.Reason ?? string.Empty
			};
			if (wire.TargetHandles.Count <= 0)
			{
				error = "旧待审 effect 缺少合法 targetHandles";
				return false;
			}
			if (PolicyEffectModuleCatalog.TryGet(wire.ModuleId, out _))
			{
				executableWireEffects.Add(wire);
			}
			else if (!TryCreateInertLegacyPlayerPolicyAgendaEffect(
				request,
				wire,
				ordinal,
				out PolicyEffectInstanceSaveData inert,
				out error))
			{
				return false;
			}
			else
			{
				inertModuleEffects.Add(inert);
			}
			ordinal++;
		}
		return true;
	}

	private static bool TryCreateInertLegacyPlayerPolicyAgendaEffect(
		PolicyDraftRequest request,
		PolicyEffectDto wire,
		int ordinal,
		out PolicyEffectInstanceSaveData inert,
		out string error)
	{
		inert = null;
		error = string.Empty;
		Dictionary<string, PolicyTargetHandleSaveData> handleByKey = NormalizePolicyTargetHandles(request?.TargetHandles)
			.ToDictionary(handle => handle.Key, StringComparer.OrdinalIgnoreCase);
		List<PolicyTargetHandleSaveData> targets = new List<PolicyTargetHandleSaveData>();
		foreach (string handle in NormalizeIdList(wire?.TargetHandles))
		{
			if (!handleByKey.TryGetValue(handle, out PolicyTargetHandleSaveData target)
				|| !IsPolicyTargetHandleAllowedForRequest(request, target))
			{
				error = "未知模块的旧待审 effect 含非法目标句柄：" + handle;
				return false;
			}
			targets.Add(target);
		}
		PolicyEffectCanonicalTargetSet targetSet = BuildPolicyEffectCanonicalTargetSet(
			request,
			wire.TargetHandles,
			targets);
		if (!HasAnyPolicyEffectCanonicalTarget(targetSet))
		{
			error = "未知模块的旧待审 effect 无法构造 canonical targetSet";
			return false;
		}
		int payloadSchemaVersion = 1;
		if (wire.Payload is JObject payloadObject
			&& payloadObject.TryGetValue("schemaVersion", StringComparison.OrdinalIgnoreCase, out JToken schemaToken)
			&& schemaToken.Type == JTokenType.Integer)
		{
			payloadSchemaVersion = Math.Max(1, schemaToken.Value<int>());
		}
		float startDay = Math.Max(0, request?.SubmittedDay ?? 0);
		string inertInstanceId = FirstNonEmpty(request?.RequestId, "legacy-player-agenda")
			+ ":inert:" + Math.Max(0, ordinal).ToString(CultureInfo.InvariantCulture);
		inert = new PolicyEffectInstanceSaveData
		{
			EffectPlanVersion = wire.EffectPlanVersion > 0
				? wire.EffectPlanVersion
				: PolicyEffectPlanVersions.CurrentVersion,
			MechanismId = !string.IsNullOrWhiteSpace(wire.MechanismId)
				? wire.MechanismId
				: PolicyEffectPlanDefaults.BuildIndependentMechanismId(
					FirstNonEmpty(request?.RequestId, inertInstanceId)),
			MechanismKind = wire.EffectPlanVersion > 0
				? wire.MechanismKind
				: PolicyEffectMechanismKind.Independent,
			MechanismRole = wire.EffectPlanVersion > 0
				? wire.MechanismRole
				: PolicyEffectMechanismRole.Subject,
			SourceOmitted = wire.EffectPlanVersion > 0 && wire.SourceOmitted,
			DestinationOmitted = wire.EffectPlanVersion > 0 && wire.DestinationOmitted,
			InstanceId = inertInstanceId,
			PolicyId = request?.RequestId ?? string.Empty,
			ModuleId = wire.ModuleId ?? string.Empty,
			SourceModuleId = FirstNonEmpty(wire.SourceModuleId, wire.ModuleId),
			PayloadSchemaVersion = payloadSchemaVersion,
			Payload = wire.Payload?.DeepClone(),
			TargetSet = targetSet,
			LifecycleState = PolicyEffectLifecycleState.Prepared,
			StateSchemaVersion = 1,
			StartDay = startDay,
			EndDay = request?.IsPermanentEffect == true
				? 0f
				: startDay + Math.Max(1, request?.ManualDurationDays ?? 0),
			SourceScope = request?.ScopeKind ?? PolicyScopeKingdom,
			Reason = wire.Reason ?? string.Empty
		};
		return true;
	}

	private static bool TryResolvePendingPlayerPolicyModuleAllowlists(
		PendingPlayerPolicyAgendaSaveData pending,
		string scope,
		IReadOnlyCollection<PolicyEffectDto> executableEffects,
		out List<string> candidateModuleIds,
		out List<string> detailedModuleIds,
		out string error)
	{
		error = string.Empty;
		List<string> executableSourceModuleIds = (executableEffects ?? Array.Empty<PolicyEffectDto>())
			.Where(effect => effect != null && !string.IsNullOrWhiteSpace(effect.ModuleId))
			.Select(effect => FirstNonEmpty(effect.SourceModuleId, effect.ModuleId))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		bool hasPersistedCandidates = pending?.CandidateModuleIds != null;
		candidateModuleIds = NormalizePlayerPolicyAgendaModuleAllowlist(
			hasPersistedCandidates ? pending.CandidateModuleIds : executableSourceModuleIds,
			scope);
		if (candidateModuleIds.Count > MaxCompiledPolicyEffectInstances)
		{
			detailedModuleIds = new List<string>();
			error = "待审候选模块超过 " + MaxCompiledPolicyEffectInstances.ToString(CultureInfo.InvariantCulture) + " 个";
			return false;
		}
		if (!PolicyEffectModuleCatalog.TryCreateAuthorization(
			candidateModuleIds,
			scope,
			out PolicyEffectModuleAuthorization candidateAuthorization,
			out error))
		{
			detailedModuleIds = new List<string>();
			return false;
		}
		IReadOnlyCollection<PolicyEffectDto> effects = executableEffects ?? Array.Empty<PolicyEffectDto>();
		bool hasMissingEffect = effects.Any(effect => effect == null);
		PolicyEffectDto unauthorizedEffect = effects
			.FirstOrDefault(effect => effect != null
				&& !candidateAuthorization.IsAuthorized(
					FirstNonEmpty(effect.SourceModuleId, effect.ModuleId),
					effect.ModuleId));
		if (hasMissingEffect || unauthorizedEffect != null)
		{
			detailedModuleIds = new List<string>();
			error = hasMissingEffect
				? "待审规范效果包含空实例"
				: "待审规范效果超出冻结的源模块授权："
					+ FirstNonEmpty(unauthorizedEffect.SourceModuleId, unauthorizedEffect.ModuleId)
					+ " -> " + unauthorizedEffect.ModuleId;
			return false;
		}
		HashSet<string> candidateSet = new HashSet<string>(candidateAuthorization.SourceModuleIds, StringComparer.Ordinal);
		bool hasPersistedDetails = pending?.DetailedModuleIds != null;
		detailedModuleIds = hasPersistedDetails
			? NormalizePlayerPolicyAgendaModuleAllowlist(pending.DetailedModuleIds, scope)
			: candidateModuleIds.Take(DuelSettings.PlayerPolicyEffectModuleEffectiveDetailCountMaximum).ToList();
		if (detailedModuleIds.Count > DuelSettings.PlayerPolicyEffectModuleEffectiveDetailCountMaximum
			|| detailedModuleIds.Any(moduleId => !candidateSet.Contains(moduleId)))
		{
			error = "待审详规模块快照无效";
			return false;
		}
		return true;
	}

	private static bool RequiresPendingPlayerPolicySemanticTargetSnapshot(PolicyDraftRequest request)
	{
		return NormalizePolicyTargetHandles(request?.TargetHandles)
			.Count > 0;
	}

	private static bool TryAttachPendingPlayerPolicySemanticTargetSnapshot(
		PolicyDraftRequest request,
		PolicyTargetWorldSnapshot snapshot,
		out string error)
	{
		error = string.Empty;
		if (request == null)
		{
			error = "玩家政策待审请求缺失";
			return false;
		}
		if (!RequiresPendingPlayerPolicySemanticTargetSnapshot(request))
		{
			return true;
		}
		if (request.SemanticTargetSnapshot?.Entities != null)
		{
			return true;
		}
		if (snapshot?.Entities == null)
		{
			error = "TargetPlan 缺少可用的当前世界快照";
			return false;
		}
		request.SemanticTargetSnapshot = snapshot;
		request.TargetAuthorization = null;
		return true;
	}

	private static bool TryRehydratePendingPlayerPolicySemanticTargetSnapshot(
		PolicyDraftRequest request,
		out string error)
	{
		error = string.Empty;
		if (request == null)
		{
			error = "玩家政策待审请求缺失";
			return false;
		}
		if (!RequiresPendingPlayerPolicySemanticTargetSnapshot(request)
			|| request.SemanticTargetSnapshot?.Entities != null)
		{
			return true;
		}
		try
		{
			return TryAttachPendingPlayerPolicySemanticTargetSnapshot(
				request,
				PolicyTargetSemanticRouter.CaptureWorldSnapshot(),
				out error);
		}
		catch (Exception ex)
		{
			error = "TargetPlan 当前世界快照捕获失败：" + ex.Message;
			return false;
		}
	}

	private bool CompleteApprovedPlayerPolicy(DynamicPolicySaveData data, bool isRenewal = false)
	{
		string recordId = data?.RecordId ?? string.Empty;
		string transactionId = recordId + ":adoption";
		string previousHistoryJson = null;
		bool hadPreviousHistory = !string.IsNullOrWhiteSpace(recordId)
			&& _policyRecordHistory.TryGetValue(recordId, out previousHistoryJson);
		bool hadPreviousUnifiedRecord = NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(
			recordId,
			out NpcRulerPolicyRecord previousUnifiedRecord);
		string activeEffectId = string.Empty;
		bool recordWritten = false;
		bool recordCommitAttempted = false;
		bool previousStewardXpAwarded = data?.PlayerStewardXpAwarded == true;
		PolicyPublishCostReceipt costReceipt = new PolicyPublishCostReceipt();
			PolicyDraftRequest request = null;
		PolicyGenerationResult result = null;
		PolicyApplicationResult application = null;
		string feedback = string.Empty;
		bool hasTimedEffect = false;
		bool committed = false;
		PolicySystemLog.Lifecycle("Player", "commit-start", "started", new PolicyLogContext
		{
			TransactionId = transactionId,
			PolicyId = data?.PolicyObjectId,
			RecordId = recordId,
			StateBefore = data?.Status,
			StateAfter = PolicyCommitStateCommitPending,
			Counts = new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["renewal"] = isRenewal ? 1 : 0
			}
		});
		try
		{
			data.CommitState = PolicyCommitStateCommitPending;
			StoreDynamicPolicy(data);
			PolicySystemLog.Transaction(transactionId, recordId, data.ActiveEffectId, string.Empty,
				"prepared", "success", stateBefore: data.Status, stateAfter: data.CommitState);
			PolicySystemLog.Lifecycle("Player", "commit-step", "prepared", new PolicyLogContext
			{
				TransactionId = transactionId,
				PolicyId = data.PolicyObjectId,
				RecordId = recordId,
				StateBefore = data.Status,
				StateAfter = data.CommitState
			});
			PendingPlayerPolicyAgendaSaveData pending = JsonConvert.DeserializeObject<PendingPlayerPolicyAgendaSaveData>(data.PlayerPayloadJson ?? "");
			request = pending?.Request;
			PolicyMainAssessmentResult assessment = pending?.Assessment;
			if (request == null || assessment == null)
			{
				throw new InvalidOperationException("玩家政策待审数据缺失");
			}
			if (pending.Version > 5)
			{
				throw new InvalidOperationException("不支持的玩家政策待审版本: " + pending.Version);
			}
			if (pending.Version < 3)
			{
				request.IsPermanentEffect = false;
				request.ManualDurationDays = Math.Max(1, assessment.DurationDays ?? 1);
				request.DailyMaintenanceGoldCost = 0;
				request.MaintenanceFunded = true;
				assessment.StartupGoldCost = assessment.RequiredGoldCost;
				assessment.DailyMaintenanceGoldCost = 0f;
				assessment.EffectDurationMode = "finite";
				pending.Version = 3;
			}
			if (pending.Version < 4)
			{
				pending.Version = 4;
			}
			if (pending.Version < 5)
			{
				pending.Version = 5;
			}
			if (isRenewal)
			{
				request.SubmittedDay = GetCurrentCampaignDay();
				request.DateText = FormatCurrentCampaignDate();
			}
			if (!TryRehydratePendingPlayerPolicySemanticTargetSnapshot(request, out string snapshotError))
			{
				throw new InvalidOperationException("玩家政策待审目标快照恢复失败：" + snapshotError);
			}
			PrepareApprovedPlayerPolicyCost(request, assessment);
			if (!TryBuildApprovedPlayerPolicyPostprocessFromPending(
				data,
				pending,
				request,
				assessment,
				out PolicyPostprocessResult postprocess,
				out string pendingError))
			{
				throw new InvalidOperationException("玩家政策待审规范效果无效：" + pendingError);
			}
			application = ApplyPolicyEffects(request, postprocess);
			result = new PolicyGenerationResult
			{
				MainAssessment = assessment,
				Postprocess = postprocess,
				PostprocessRaw = SafeSerializeForDebug(postprocess)
			};
			feedback = FirstNonEmpty(pending.Feedback, ResolveFeedbackText(result, request));
			hasTimedEffect = HasAnyTimedPolicyEffect(application);
			if (hasTimedEffect)
			{
				if (!TryActivatePolicyEffectApplication(
					request,
					application,
					data.RecordId,
					isRenewal,
					out activeEffectId,
					out string activationError))
				{
					throw new InvalidOperationException("政策效果激活失败：" + activationError);
				}
				data.ActiveEffectId = activeEffectId;
				data.RequiresEffectBundle = true;
				StoreDynamicPolicy(data);
				recordCommitAttempted = true;
				recordWritten = RecordSuccessfulPolicy(request, result, feedback, application, data.RecordId);
				if (!recordWritten)
				{
					throw new InvalidOperationException("政策历史或 NPC 统一记录写入失败");
				}
				// Payment is the final throwable business write. Crossing this line commits the policy.
				DeductPublishCost(request, costReceipt);
				PolicySystemLog.Transaction(transactionId, recordId, activeEffectId, string.Empty,
					"costCommitted", "success", costReceipt: "gold=" + request.GoldCost.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				data.ActiveEffectId = string.Empty;
				data.RequiresEffectBundle = false;
				request.GoldCost = 0;
				request.InfluenceCost = 0f;
			}
			committed = true;
			PolicySystemLog.Transaction(transactionId, recordId, activeEffectId, string.Empty,
				"effectsCommitted", "success", stateBefore: PolicyCommitStateCommitPending, stateAfter: PolicyCommitStateActive);
			PolicySystemLog.Lifecycle("Player", "commit-complete", "success", new PolicyLogContext
			{
				TransactionId = transactionId,
				PolicyId = data.PolicyObjectId,
				RecordId = recordId,
				EffectId = activeEffectId,
				StateBefore = PolicyCommitStateCommitPending,
				StateAfter = PolicyCommitStateActive,
				Gold = request?.GoldCost,
				Influence = request?.InfluenceCost
			});
		}
		catch (Exception ex)
		{
			if (data != null)
			{
				data.PlayerStewardXpAwarded = previousStewardXpAwarded;
				data.CommitState = PolicyCommitStateFailed;
				StoreDynamicPolicy(data);
			}
			List<string> rollbackFailures = new List<string>();
			try
			{
				if (!TryRefundPublishCost(costReceipt, out string refundError))
				{
					rollbackFailures.Add("支付退款失败：" + refundError);
				}
			}
			catch (Exception refundException)
			{
				rollbackFailures.Add("支付退款异常：" + refundException.Message);
			}
			try
			{
				if (!string.IsNullOrWhiteSpace(activeEffectId)
					&& !RollbackAndRemovePolicyEffectBundle(activeEffectId, "player-adoption-commit-failed", out string rollbackError))
				{
					rollbackFailures.Add("效果 bundle 回滚失败：" + rollbackError);
				}
			}
			catch (Exception rollbackException)
			{
				rollbackFailures.Add("效果 bundle 回滚异常：" + rollbackException.Message);
			}
			if (recordCommitAttempted)
			{
				try
				{
					if (hadPreviousHistory)
					{
						_policyRecordHistory[recordId] = previousHistoryJson;
					}
					else
					{
						_policyRecordHistory.Remove(recordId);
					}
				}
				catch (Exception historyRollbackException)
				{
					rollbackFailures.Add("政策历史回滚失败：" + historyRollbackException.Message);
				}
				try
				{
					bool unifiedRestored = hadPreviousUnifiedRecord
						? NpcRulerPolicyBehavior.RegisterPlayerPolicyForExternal(previousUnifiedRecord)
						: !NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(recordId, out _)
							|| NpcRulerPolicyBehavior.UnregisterPlayerPolicyForExternal(recordId);
					if (!unifiedRestored)
					{
						rollbackFailures.Add("NPC 统一记录回滚失败");
					}
				}
				catch (Exception unifiedRollbackException)
				{
					rollbackFailures.Add("NPC 统一记录回滚异常：" + unifiedRollbackException.Message);
				}
			}
			try
			{
				PolicySystemLog.Failure("Player", "commit-failed", ex, new PolicyLogContext
				{
					TransactionId = transactionId,
					PolicyId = data?.PolicyObjectId,
					RecordId = recordId,
					EffectId = activeEffectId,
					StateBefore = PolicyCommitStateCommitPending,
					StateAfter = PolicyCommitStateFailed,
					Counts = new Dictionary<string, int>(StringComparer.Ordinal)
					{
						["rollbackFailures"] = rollbackFailures.Count
					}
				});
				PolicySystemLog.Write("Agenda", "player-adoption-commit-failed", "recordId=" + recordId + " " + ex
					+ (rollbackFailures.Count == 0 ? string.Empty : " rollback=" + string.Join(" | ", rollbackFailures)));
			}
			catch
			{
			}
			try
			{
				TryQueueNaturalExpiryAbolition(data?.RecordId, "");
			}
			catch (Exception queueException)
			{
				try
				{
					PolicySystemLog.Write("Agenda", "player-adoption-failure-cleanup-failed", "recordId=" + recordId + " error=" + queueException.Message);
				}
				catch
				{
				}
			}
			return false;
		}
		if (!committed)
		{
			return false;
		}

		if (recordWritten)
		{
			RunApprovedPlayerPolicyPostCommitStep(recordId, "player-action", () =>
				RecordPolicyPublishAsPlayerAction(request, result, application, data.RecordId));
		}
		if (!isRenewal)
		{
			RunApprovedPlayerPolicyPostCommitStep(recordId, "presentation", () =>
			{
				if (!NpcRulerPolicyBehavior.TryPublishPlayerPolicyPresentationForExternal(data.RecordId))
				{
					throw new InvalidOperationException("玩家政策展示未全部发布，已保留持久化待重试标记");
				}
			});
			if (hasTimedEffect && recordWritten)
			{
				RunApprovedPlayerPolicyPostCommitStep(recordId, "steward-xp", () =>
					TryAwardPlayerPolicyStewardXp(data, request, application));
			}
		}
		if (isRenewal)
		{
			RunApprovedPlayerPolicyPostCommitStep(recordId, "renewal-popup", () =>
				ShowPolicyRenewalResultPopup(data.PolicyObjectId, request, application));
		}
		else
		{
			RunApprovedPlayerPolicyPostCommitStep(recordId, "success-popup", () =>
			{
				string impactText = BuildImpactPopupText(request, feedback, application, costDeducted: hasTimedEffect);
				ShowPolicySuccessResultPopup(data.PolicyObjectId, impactText);
			});
		}
		if (!hasTimedEffect)
		{
			RunApprovedPlayerPolicyPostCommitStep(recordId, "natural-expiry-queue", () =>
				TryQueueNaturalExpiryAbolition(data.RecordId, ""));
		}
		return true;
	}

	private static void RunApprovedPlayerPolicyPostCommitStep(string recordId, string stage, Action action)
	{
		RunPolicyPostCommitStep("Agenda", "player-adoption-post-commit-failed", recordId, stage, action);
	}

	private void TryAwardPlayerPolicyStewardXp(DynamicPolicySaveData data, PolicyDraftRequest request, PolicyApplicationResult application)
	{
		if (data == null || data.PlayerStewardXpAwarded)
		{
			return;
		}
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null)
			{
				PolicySystemLog.Write("Agenda", "player-steward-xp-skipped", "recordId=" + (data.RecordId ?? "") + " reason=main-hero-missing");
				return;
			}
			int actualGold = Math.Max(0, request?.GoldCost ?? 0);
			int affectedTownCount = CountPlayerPolicyAffectedTowns(application);
			int durationDays = GetPlayerPolicyExperienceDurationDays(application);
			int experience = CalculatePlayerPolicyStewardXp(
				actualGold,
				affectedTownCount,
				durationDays,
				out int goldExperience,
				out int scopeExperience,
				out int durationExperience);
			mainHero.AddSkillXp(DefaultSkills.Steward, experience);
			data.PlayerStewardXpAwarded = true;
			StoreDynamicPolicy(data);
			PolicySystemLog.Write("Agenda", "player-steward-xp-awarded", "recordId=" + (data.RecordId ?? "")
				+ " xp=" + experience.ToString(CultureInfo.InvariantCulture)
				+ " actualGold=" + actualGold.ToString(CultureInfo.InvariantCulture)
				+ " affectedTowns=" + affectedTownCount.ToString(CultureInfo.InvariantCulture)
				+ " durationDays=" + durationDays.ToString(CultureInfo.InvariantCulture)
				+ " components(base=" + PlayerPolicyStewardXpBase.ToString(CultureInfo.InvariantCulture)
				+ ",gold=" + goldExperience.ToString(CultureInfo.InvariantCulture)
				+ ",scope=" + scopeExperience.ToString(CultureInfo.InvariantCulture)
				+ ",duration=" + durationExperience.ToString(CultureInfo.InvariantCulture) + ")");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "player-steward-xp-failed", "recordId=" + (data?.RecordId ?? "") + " " + ex);
		}
	}

	private static int CountPlayerPolicyAffectedTowns(PolicyApplicationResult application)
	{
		Dictionary<string, int> townCountByKingdom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (AppliedKingdomEffect effect in application?.KingdomEffects?.Where(x => x != null && (x.IsPermanentEffect || x.DurationDays > 0)) ?? Enumerable.Empty<AppliedKingdomEffect>())
		{
			string key = FirstNonEmpty(effect.KingdomId, effect.KingdomName, effect.EffectId);
			int townCount = Math.Max(0, effect.TownCount);
			if (!townCountByKingdom.TryGetValue(key, out int currentCount) || townCount > currentCount)
			{
				townCountByKingdom[key] = townCount;
			}
		}
		long total = townCountByKingdom.Values.Aggregate(0L, (sum, count) => sum + count);
		return total >= int.MaxValue ? int.MaxValue : (int)total;
	}

	private static int GetPlayerPolicyExperienceDurationDays(PolicyApplicationResult application)
	{
		return application?.KingdomEffects?
			.Where(x => x != null && x.DurationDays > 0)
			.Select(x => x.DurationDays)
			.DefaultIfEmpty(0)
			.Max() ?? 0;
	}

	private static int CalculatePlayerPolicyStewardXp(
		int actualGold,
		int affectedTownCount,
		int durationDays,
		out int goldExperience,
		out int scopeExperience,
		out int durationExperience)
	{
		actualGold = Math.Max(0, actualGold);
		affectedTownCount = Math.Max(0, affectedTownCount);
		durationDays = Math.Max(0, durationDays);
		goldExperience = actualGold <= 0
			? 0
			: (int)Math.Round(100d * Math.Log10(1d + (actualGold / 10000d)), MidpointRounding.AwayFromZero);
		scopeExperience = (int)Math.Min(
			PlayerPolicyStewardXpScopeMax,
			25L + (2L * affectedTownCount));
		durationExperience = Math.Min(PlayerPolicyStewardXpDurationMax, durationDays);
		long total = PlayerPolicyStewardXpBase + (long)goldExperience + scopeExperience + durationExperience;
		return (int)Math.Max(PlayerPolicyStewardXpBase, Math.Min(PlayerPolicyStewardXpMax, total));
	}

	private void EndPolicyEffectsForAgendaAbolition(string recordId, string reason, string lifecycleEventKey = null)
	{
		string id = (recordId ?? "").Trim();
		DispatchPolicyEffectRecordAbolishedBeforeRemoval(
			id,
			string.IsNullOrWhiteSpace(lifecycleEventKey)
				? "record:" + id + ":abolished:agenda"
				: lifecycleEventKey,
			string.IsNullOrWhiteSpace(lifecycleEventKey) ? "agenda" : "kingdom_destroyed");
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			ActivePolicyEffectSaveData effect;
			try
			{
				effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(item.Value ?? "");
			}
			catch
			{
				continue;
			}
			if (effect == null || !string.Equals(effect.RecordId ?? "", id, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			MarkPolicyRecordEffectEnded(effect, reason, queueNaturalExpiry: false);
			RemoveActivePolicyEffect(item.Key);
		}
	}

	private void TryQueueNaturalExpiryAbolition(string recordId, string endingEffectId)
	{
		string id = (recordId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return;
		}
		DynamicPolicySaveData data = LoadDynamicPolicies().FirstOrDefault(x => x != null && string.Equals(x.RecordId ?? "", id, StringComparison.OrdinalIgnoreCase));
		if (data == null)
		{
			return;
		}
		bool hasOtherActiveEffect = _activePolicyEffects.Values.Any(raw =>
		{
			try
			{
				ActivePolicyEffectSaveData effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(raw ?? "");
				return effect != null
					&& !string.Equals(effect.EffectId ?? "", endingEffectId ?? "", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(effect.RecordId ?? "", id, StringComparison.OrdinalIgnoreCase)
					&& ShouldRetainActivePolicyEffect(effect);
			}
			catch
			{
				return false;
			}
		});
		if (hasOtherActiveEffect)
		{
			return;
		}
		if (!string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (ShouldAutoTerminatePlayerPolicyAtNaturalExpiry(data))
		{
			if (HasAmbiguousQuarantinedActiveEffectForRecord(id))
			{
				PolicySystemLog.Write("Agenda", "player-natural-expiry-deferred-quarantine",
					"recordId=" + id + " policy=" + (data.PolicyObjectId ?? string.Empty));
				return;
			}
			CompleteDynamicPolicyAbolition(
				data,
				EnsureDynamicPolicyObject(data),
				"AF 玩家全国政策效果自然到期");
			PolicySystemLog.Write("Agenda", "player-natural-expiry-terminated",
				"recordId=" + id + " policy=" + (data.PolicyObjectId ?? string.Empty));
			return;
		}
		if (data.NaturalExpiryAgendaRejected)
		{
			return;
		}
		Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, "");
		PolicyObject policy = EnsureDynamicPolicyObject(data);
		if (owner == null || policy == null || !owner.ActivePolicies.Contains(policy))
		{
			return;
		}
		if (owner.UnresolvedDecisions.OfType<KingdomPolicyDecision>().Any(x => x?.Policy == policy && !x.ShouldBeCancelled()))
		{
			return;
		}
		Clan proposer = owner.RulingClan;
		if (proposer == null)
		{
			return;
		}
		data.Status = DynamicPolicyStatusExpiryVotePending;
		StoreDynamicPolicy(data);
		NpcRulerPolicyBehavior.UpdatePolicyAgendaStatusForExternal(data.RecordId, DynamicPolicyStatusExpiryVotePending);
		KingdomPolicyDecision decision = new KingdomPolicyDecision(proposer, policy, isInvertedDecision: true);
		owner.AddDecision(decision, ignoreInfluenceCost: true);
		if (owner.UnresolvedDecisions == null || !owner.UnresolvedDecisions.Contains(decision))
		{
			data.Status = DynamicPolicyStatusActive;
			data.CommitState = PolicyCommitStateActive;
			StoreDynamicPolicy(data);
			NpcRulerPolicyBehavior.UpdatePolicyAgendaStatusForExternal(data.RecordId, DynamicPolicyStatusActive);
			return;
		}
		PolicySystemLog.Write("Agenda", "expiry-abolition-submitted", "recordId=" + data.RecordId + " policy=" + data.PolicyObjectId);
	}

	private static bool ShouldAutoTerminatePlayerPolicyAtNaturalExpiry(DynamicPolicySaveData data)
	{
		return data != null
			&& string.Equals(data.Source, "player", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase);
	}

	private bool HasAmbiguousQuarantinedActiveEffectForRecord(string recordId, string activeEffectId = "")
	{
		string id = (recordId ?? string.Empty).Trim();
		string exactEffectId = (activeEffectId ?? string.Empty).Trim();
		foreach (string effectId in _quarantinedActivePolicyEffectIds)
		{
			if (exactEffectId.Length > 0
				&& string.Equals(effectId ?? string.Empty, exactEffectId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (!_activePolicyEffects.TryGetValue(effectId, out string raw))
			{
				continue;
			}
			try
			{
				JObject value = JObject.Parse(raw ?? string.Empty);
				string ownerRecordId = ((string)value["RecordId"] ?? (string)value["recordId"] ?? string.Empty).Trim();
				string internalEffectId = ((string)value["EffectId"] ?? (string)value["effectId"] ?? string.Empty).Trim();
				if (string.Equals(ownerRecordId, id, StringComparison.OrdinalIgnoreCase)
					|| (exactEffectId.Length > 0
						&& string.Equals(internalEffectId, exactEffectId, StringComparison.OrdinalIgnoreCase))
					|| (exactEffectId.Length == 0 && ownerRecordId.Length == 0))
				{
					return true;
				}
			}
			catch
			{
				if (exactEffectId.Length == 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	private PolicyObject EnsureDynamicPolicyObject(DynamicPolicySaveData data)
	{
		if (data == null || !IsDynamicPolicyId(data.PolicyObjectId))
		{
			return null;
		}
		try
		{
			PolicyObject policy = MBObjectManager.Instance?.GetObject<PolicyObject>(data.PolicyObjectId);
			if (policy == null)
			{
				policy = MBObjectManager.Instance?.RegisterPresumedObject(new PolicyObject(data.PolicyObjectId));
			}
			return TryInitializeDynamicPolicyObject(policy, data, out string initializationError)
				? policy
				: throw new InvalidOperationException(initializationError);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "policy-object-register-failed", "policy=" + data.PolicyObjectId + " " + ex);
			return null;
		}
	}

	private static string BuildDynamicPolicyDisplaySummary(DynamicPolicySaveData data)
	{
		string summary = CompactPolicyContextText(CleanPolicyDisplayText(FirstNonEmpty(
			data?.LogEntryDescription,
			data?.SecondaryEffects,
			data?.PolicyContent,
			data?.PolicyName)));
		if (string.IsNullOrWhiteSpace(summary))
		{
			return "该政策尚无可用摘要。";
		}
		int sentenceEnd = summary.IndexOfAny(new[] { '。', '！', '？', '!', '?' });
		if (sentenceEnd >= 0)
		{
			summary = summary.Substring(0, sentenceEnd + 1).Trim();
		}
		return LimitDisplayChars(summary, 96);
	}

	private void TryUnregisterDynamicPolicyObject(DynamicPolicySaveData data, PolicyObject policy)
	{
		try
		{
			if (data == null || policy == null)
			{
				return;
			}
			bool referenced = Kingdom.All.Any(kingdom => kingdom != null
				&& ((kingdom.ActivePolicies?.Contains(policy) == true)
					|| (kingdom.UnresolvedDecisions?.OfType<KingdomPolicyDecision>().Any(x => x?.Policy == policy) == true)));
			if (!referenced)
			{
				MBObjectManager.Instance?.UnregisterObject(policy);
			}
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "policy-object-unregister-failed", "policy=" + (data?.PolicyObjectId ?? "") + " " + ex.Message);
		}
	}

	private static bool IsDynamicPolicyId(string policyId)
	{
		return !string.IsNullOrWhiteSpace(policyId) && policyId.StartsWith(DynamicPolicyIdPrefix, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeDynamicPolicyIdPart(string value)
	{
		string text = Regex.Replace((value ?? "").Trim(), "[^A-Za-z0-9_.-]+", "_");
		return string.IsNullOrWhiteSpace(text) ? Guid.NewGuid().ToString("N") : text;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
	}

	private static bool TryReadPoliticalWeights(float? authoritarian, float? oligarchic, float? egalitarian, out float authoritarianValue, out float oligarchicValue, out float egalitarianValue)
	{
		authoritarianValue = 0f;
		oligarchicValue = 0f;
		egalitarianValue = 0f;
		if (!authoritarian.HasValue || !oligarchic.HasValue || !egalitarian.HasValue
			|| float.IsNaN(authoritarian.Value) || float.IsInfinity(authoritarian.Value)
			|| float.IsNaN(oligarchic.Value) || float.IsInfinity(oligarchic.Value)
			|| float.IsNaN(egalitarian.Value) || float.IsInfinity(egalitarian.Value))
		{
			return false;
		}
		authoritarianValue = Math.Max(-1f, Math.Min(1f, authoritarian.Value));
		oligarchicValue = Math.Max(-1f, Math.Min(1f, oligarchic.Value));
		egalitarianValue = Math.Max(-1f, Math.Min(1f, egalitarian.Value));
		return Math.Abs(authoritarianValue) > 0.0001f || Math.Abs(oligarchicValue) > 0.0001f || Math.Abs(egalitarianValue) > 0.0001f;
	}

	private static bool ShouldKeepDynamicPolicyRegistered(string status)
	{
		return string.Equals(status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase);
	}

	private static Kingdom ResolveKingdomStatic(string kingdomId)
	{
		string id = (kingdomId ?? "").Trim();
		return Kingdom.All?.FirstOrDefault(x => x != null && string.Equals(x.StringId ?? "", id, StringComparison.OrdinalIgnoreCase));
	}

	private static Clan ResolveClanById(string clanId)
	{
		string id = (clanId ?? "").Trim();
		return Clan.All?.FirstOrDefault(x => x != null && string.Equals(x.StringId ?? "", id, StringComparison.OrdinalIgnoreCase));
	}

	private static bool TryGetDynamicPolicyDataStatic(string policyObjectId, out DynamicPolicySaveData data)
	{
		data = null;
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		return behavior != null && behavior.TryGetDynamicPolicyData(policyObjectId, out data);
	}

	private bool TryGetDynamicPolicyData(string policyObjectId, out DynamicPolicySaveData data)
	{
		data = null;
		string id = (policyObjectId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)
			|| _quarantinedDynamicPolicyIds.Contains(id)
			|| !_dynamicPolicyRegistry.TryGetValue(id, out string raw))
		{
			return false;
		}
		try
		{
			data = JsonConvert.DeserializeObject<DynamicPolicySaveData>(raw ?? "");
			return data != null;
		}
		catch
		{
			return false;
		}
	}

	private List<DynamicPolicySaveData> LoadDynamicPolicies()
	{
		return _dynamicPolicyRegistry
			.Where(item => !_quarantinedDynamicPolicyIds.Contains(item.Key))
			.Select(item => item.Value)
			.Select(raw =>
		{
			try
			{
				return JsonConvert.DeserializeObject<DynamicPolicySaveData>(raw ?? "");
			}
			catch
			{
				return null;
			}
		}).Where(x => x != null).ToList();
	}

	private void StoreDynamicPolicy(DynamicPolicySaveData data)
	{
		if (data == null || !IsDynamicPolicyId(data.PolicyObjectId))
		{
			return;
		}
		_dynamicPolicyRegistry[data.PolicyObjectId] = JsonConvert.SerializeObject(data);
		_quarantinedDynamicPolicyIds.Remove(data.PolicyObjectId);
	}

	private void QuarantineDynamicPolicy(string key, string raw, string reason)
	{
		string id = (key ?? string.Empty).Trim();
		if (id.Length == 0 || string.IsNullOrWhiteSpace(raw))
		{
			return;
		}
		_dynamicPolicyRegistry[id] = raw;
		if (_quarantinedDynamicPolicyIds.Add(id))
		{
			PolicySystemLog.Failure("Save", "dynamic-policy-raw-quarantined",
				reason,
				"policyObjectId=" + id);
		}
	}

	private void RemoveQuarantinedDynamicPolicyMembershipAfterLoad()
	{
		foreach (string storageKey in _quarantinedDynamicPolicyIds.ToList())
		{
			string policyObjectId = storageKey;
			string ownerKingdomId = string.Empty;
			if (_dynamicPolicyRegistry.TryGetValue(storageKey, out string raw))
			{
				try
				{
					JObject token = JObject.Parse(raw ?? string.Empty);
					policyObjectId = ((string)token.GetValue("PolicyObjectId", StringComparison.OrdinalIgnoreCase)
						?? storageKey).Trim();
					ownerKingdomId = ((string)token.GetValue("OwnerKingdomId", StringComparison.OrdinalIgnoreCase)
						?? string.Empty).Trim();
				}
				catch
				{
					// The storage key is still a sufficient fail-closed membership identity.
				}
			}
			if (!IsDynamicPolicyId(policyObjectId))
			{
				continue;
			}
			IEnumerable<Kingdom> owners = Enumerable.Empty<Kingdom>();
			try
			{
				Kingdom exactOwner = ResolveKingdomByIdOrName(ownerKingdomId, string.Empty);
				owners = exactOwner != null
					? new[] { exactOwner }
					: (Kingdom.All ?? Enumerable.Empty<Kingdom>());
			}
			catch
			{
				owners = Enumerable.Empty<Kingdom>();
			}
			foreach (Kingdom owner in owners.Where(value => value != null).ToList())
			{
				PolicyObject active = owner.ActivePolicies?.FirstOrDefault(policy => policy != null
					&& string.Equals(policy.StringId ?? string.Empty, policyObjectId, StringComparison.OrdinalIgnoreCase));
				if (active != null)
				{
					owner.RemovePolicy(active);
					PolicySystemLog.Failure("Save", "dynamic-policy-membership-quarantined",
						"active policy membership was removed because its dynamic record is unreadable or from a future schema",
						"policyObjectId=" + policyObjectId + " owner=" + (owner.StringId ?? string.Empty));
				}
			}
		}
	}

	private void RemoveLegacyStoppedDynamicPolicyMembershipAfterLoad()
	{
		foreach (string policyObjectId in _legacyStoppedDynamicPolicyIds.ToList())
		{
			if (!TryGetDynamicPolicyData(policyObjectId, out DynamicPolicySaveData data))
			{
				continue;
			}
			Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, string.Empty);
			if (owner == null)
			{
				continue;
			}
			foreach (KingdomPolicyDecision decision in FindDynamicPolicyDecisions(owner, policyObjectId).ToList())
			{
				owner.RemoveDecision(decision);
			}
			PolicyObject active = owner.ActivePolicies?.FirstOrDefault(policy => policy != null
				&& string.Equals(policy.StringId ?? string.Empty, policyObjectId, StringComparison.OrdinalIgnoreCase));
			if (active != null)
			{
				owner.RemovePolicy(active);
			}
			PolicySystemLog.Write("Save", "legacy-policy-stopped-after-load",
				"recordId=" + (data.RecordId ?? string.Empty)
				+ " policyObjectId=" + policyObjectId
				+ " owner=" + (owner.StringId ?? string.Empty));
		}
	}

	private void MarkLegacyStoppedPolicyHistoryAfterLoad()
	{
		foreach (string recordId in _legacyStoppedPolicyRecordIds)
		{
			if (!_policyRecordHistory.TryGetValue(recordId, out string raw))
			{
				continue;
			}
			try
			{
				PolicyRecordSaveData record = JsonConvert.DeserializeObject<PolicyRecordSaveData>(raw ?? string.Empty);
				if (record?.Effects == null)
				{
					continue;
				}
				foreach (PolicyRecordEffectSaveData effect in record.Effects.Where(value => value != null))
				{
					effect.RemainingDays = 0;
					effect.IsEnded = true;
					effect.EndReason = "旧政策系统升级后停止，可重新评议";
				}
				_policyRecordHistory[record.RecordId] = JsonConvert.SerializeObject(record);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("legacy-history-stop-failed", "recordId=" + recordId + " error=" + ex.Message);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			TrimPolicyRecordHistory();
			Dictionary<string, string> historyStore = CampaignSaveChunkHelper.FlattenStringDictionary(_policyRecordHistory, SaveKeyPolicyRecordHistory, "CustomPolicyHistory");
			dataStore.SyncData(SaveKeyPolicyRecordHistory, ref historyStore);
			TrimLocalPolicyRecords();
			Dictionary<string, string> localPolicyStore = CampaignSaveChunkHelper.FlattenStringDictionary(_localPolicyRecords, SaveKeyLocalPolicyRecords, "LocalPolicyRecords");
			dataStore.SyncData(SaveKeyLocalPolicyRecords, ref localPolicyStore);
			TrimActivePolicyEffects();
			Dictionary<string, string> activeEffectsStore = CampaignSaveChunkHelper.FlattenStringDictionary(_activePolicyEffects, SaveKeyActivePolicyEffects, "CustomPolicyActiveEffects");
			dataStore.SyncData(SaveKeyActivePolicyEffects, ref activeEffectsStore);
			Dictionary<string, string> dynamicPolicyStore = CampaignSaveChunkHelper.FlattenStringDictionary(_dynamicPolicyRegistry, SaveKeyDynamicPolicyRegistry, "DynamicPolicyRegistry");
			dataStore.SyncData(SaveKeyDynamicPolicyRegistry, ref dynamicPolicyStore);
			PolicySystemLog.Lifecycle("Player", "save-summary", "success", new PolicyLogContext
			{
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["history"] = _policyRecordHistory.Count,
					["local"] = _localPolicyRecords.Count,
					["active"] = _activePolicyEffects.Count,
					["dynamic"] = _dynamicPolicyRegistry.Count
				}
			});
			return;
		}
		ResetTransientPolicyGenerationStateAfterLoad();
		_policyRecordHistory.Clear();
		_localPolicyRecords.Clear();
		_activePolicyEffects.Clear();
		_activePolicyEffectRuntimeCache.Clear();
		_quarantinedActivePolicyEffectIds.Clear();
		ResetActivePolicyEffectRuntimeIndex();
		_dynamicPolicyRegistry.Clear();
		_quarantinedDynamicPolicyIds.Clear();
		_legacyStoppedDynamicPolicyIds.Clear();
		_legacyStoppedPolicyRecordIds.Clear();
		PolicyEffectMigrationBatchSummary moduleMigrationSummary = new PolicyEffectMigrationBatchSummary();
		Dictionary<string, string> storedHistory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPolicyRecordHistory, ref storedHistory);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedHistory, "CustomPolicyHistory"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			try
			{
				string migrationError = string.Empty;
				JObject rawRecord = JObject.Parse(value);
				if (PolicyEffectSaveCodec.TryNormalizePolicyRecordV1ToV3(
					rawRecord,
					out JObject normalizedObject,
					out PolicyEffectMigrationBatchSummary migration,
					out migrationError))
				{
					PolicyRecordSaveData normalizedRecord = normalizedObject.ToObject<PolicyRecordSaveData>();
					if (normalizedRecord != null && !string.IsNullOrWhiteSpace(normalizedRecord.RecordId))
					{
						moduleMigrationSummary.Merge(migration);
						_policyRecordHistory[key] = JsonConvert.SerializeObject(normalizedRecord);
						continue;
					}
					migrationError = "规范化后的政策记录缺少 recordId";
				}
				PolicyDebugLog("save-load-skip", "policy record module migration failed key=" + key + " error=" + migrationError);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("save-load-skip", "invalid policy record key=" + key + " error=" + ex.Message);
			}
		}
		TrimPolicyRecordHistory();
		Dictionary<string, string> storedLocalPolicies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyLocalPolicyRecords, ref storedLocalPolicies);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedLocalPolicies, "LocalPolicyRecords"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			try
			{
				string migrationError = string.Empty;
				JObject rawRecord = JObject.Parse(value);
				if (PolicyEffectSaveCodec.TryNormalizeLegacyStoppedLocalPolicy(
					rawRecord,
					out JObject stoppedLegacyObject,
					out migrationError))
				{
					LocalPolicyRecordSaveData stoppedLegacy = NormalizeLocalPolicyRecord(
						stoppedLegacyObject.ToObject<LocalPolicyRecordSaveData>());
					if (stoppedLegacy != null && !string.IsNullOrWhiteSpace(stoppedLegacy.RecordId))
					{
						moduleMigrationSummary.RecordsVisited++;
						moduleMigrationSummary.RecordsChanged++;
						_localPolicyRecords[key] = JsonConvert.SerializeObject(stoppedLegacy);
						continue;
					}
					migrationError = "停止归一化后的旧地方政策记录缺少 recordId";
				}
				else if (!string.IsNullOrWhiteSpace(migrationError))
				{
					PolicyDebugLog("local-save-load-skip", "legacy local policy stop normalization failed key=" + key + " error=" + migrationError);
					continue;
				}
				if (PolicyEffectSaveCodec.TryNormalizeLocalV1ToV6(
					rawRecord,
					out JObject normalizedObject,
					out PolicyEffectMigrationBatchSummary migration,
					out migrationError))
				{
					LocalPolicyRecordSaveData normalizedRecord = NormalizeLocalPolicyRecord(normalizedObject.ToObject<LocalPolicyRecordSaveData>());
					if (normalizedRecord != null && !string.IsNullOrWhiteSpace(normalizedRecord.RecordId))
					{
						moduleMigrationSummary.Merge(migration);
						_localPolicyRecords[key] = JsonConvert.SerializeObject(normalizedRecord);
						continue;
					}
					migrationError = "规范化后的地方政策记录缺少 recordId";
				}
				PolicyDebugLog("local-save-load-skip", "local policy module migration failed key=" + key + " error=" + migrationError);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("local-save-load-skip", "invalid local policy record key=" + key + " error=" + ex.Message);
			}
		}
		TrimLocalPolicyRecords();
		Dictionary<string, string> storedActiveEffects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyActivePolicyEffects, ref storedActiveEffects);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedActiveEffects, "CustomPolicyActiveEffects"))
		{
			string key = (item.Key ?? "").Trim();
			string value = (item.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			try
			{
				string migrationError = string.Empty;
				JObject rawRecord = JObject.Parse(value);
				if (PolicyEffectSaveCodec.IsLegacyStoppedActiveV5Shape(rawRecord))
				{
					moduleMigrationSummary.RecordsVisited++;
					moduleMigrationSummary.RecordsChanged++;
					PolicySystemLog.Write("Save", "legacy-active-effect-stopped-after-load",
						"recordId=" + (((string)rawRecord.GetValue("RecordId", StringComparison.OrdinalIgnoreCase) ?? string.Empty).Trim())
						+ " effectId=" + (((string)rawRecord.GetValue("EffectId", StringComparison.OrdinalIgnoreCase) ?? key).Trim()));
					_legacyStoppedPolicyRecordIds.Add(((string)rawRecord.GetValue("RecordId", StringComparison.OrdinalIgnoreCase) ?? string.Empty).Trim());
					continue;
				}
				if (PolicyEffectSaveCodec.TryNormalizeActiveV4ToV8(
					rawRecord,
					out JObject normalizedObject,
					out PolicyEffectMigrationBatchSummary migration,
					out migrationError))
				{
					ActivePolicyEffectSaveData normalizedEffect = normalizedObject.ToObject<ActivePolicyEffectSaveData>();
					if (ShouldRetainActivePolicyEffect(normalizedEffect))
					{
						moduleMigrationSummary.Merge(migration);
						_activePolicyEffects[key] = JsonConvert.SerializeObject(normalizedEffect);
						continue;
					}
					if (normalizedEffect == null || string.IsNullOrWhiteSpace(normalizedEffect.EffectId))
					{
						QuarantineActivePolicyEffect(key, value, "load normalization produced an invalid active effect identity");
					}
					continue;
				}
				PolicyDebugLog("active-save-load-skip", "active effect module migration failed key=" + key + " error=" + migrationError);
				QuarantineActivePolicyEffect(key, value, "load migration: " + migrationError);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("active-save-load-skip", "invalid active policy effect key=" + key + " error=" + ex.Message);
				QuarantineActivePolicyEffect(key, value, "load parse: " + ex.Message);
			}
		}
		TrimActivePolicyEffects();
		Dictionary<string, string> storedDynamicPolicies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyDynamicPolicyRegistry, ref storedDynamicPolicies);
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(storedDynamicPolicies, "DynamicPolicyRegistry"))
		{
			string key = (item.Key ?? "").Trim();
			string value = item.Value ?? string.Empty;
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			try
			{
				string migrationError = string.Empty;
				JObject rawRecord = JObject.Parse(value);
				if (PolicyEffectSaveCodec.TryNormalizeLegacyStoppedDynamicPolicy(
					rawRecord,
					out JObject stoppedLegacyObject,
					out migrationError))
				{
					DynamicPolicySaveData stoppedLegacy = stoppedLegacyObject.ToObject<DynamicPolicySaveData>();
					if (stoppedLegacy != null
						&& IsDynamicPolicyId(stoppedLegacy.PolicyObjectId)
						&& string.Equals(stoppedLegacy.PolicyObjectId, key, StringComparison.OrdinalIgnoreCase))
					{
						moduleMigrationSummary.RecordsVisited++;
						moduleMigrationSummary.RecordsChanged++;
						_dynamicPolicyRegistry[stoppedLegacy.PolicyObjectId] = JsonConvert.SerializeObject(stoppedLegacy);
						_legacyStoppedDynamicPolicyIds.Add(stoppedLegacy.PolicyObjectId);
						_legacyStoppedPolicyRecordIds.Add(stoppedLegacy.RecordId ?? string.Empty);
						continue;
					}
					migrationError = "停止归一化后的旧动态政策记录缺少合法 policyObjectId 或存储键不匹配";
				}
				else if (!string.IsNullOrWhiteSpace(migrationError))
				{
					PolicyDebugLog("dynamic-policy-load-skip", "legacy dynamic policy stop normalization failed key=" + key + " error=" + migrationError);
					QuarantineDynamicPolicy(key, value, "legacy stop normalization: " + migrationError);
					continue;
				}
				if (PolicyEffectSaveCodec.TryNormalizeDynamicV1ToV4(
					rawRecord,
					out JObject normalizedObject,
					out PolicyEffectMigrationBatchSummary migration,
					out migrationError))
				{
					DynamicPolicySaveData normalizedPolicy = normalizedObject.ToObject<DynamicPolicySaveData>();
					if (normalizedPolicy != null
						&& IsDynamicPolicyId(normalizedPolicy.PolicyObjectId)
						&& string.Equals(normalizedPolicy.PolicyObjectId, key, StringComparison.OrdinalIgnoreCase))
					{
						moduleMigrationSummary.Merge(migration);
						_dynamicPolicyRegistry[normalizedPolicy.PolicyObjectId] = JsonConvert.SerializeObject(normalizedPolicy);
						continue;
					}
					migrationError = "规范化后的动态政策记录缺少合法 policyObjectId 或存储键不匹配";
				}
				PolicyDebugLog("dynamic-policy-load-skip", "dynamic policy module migration failed key=" + key + " error=" + migrationError);
				QuarantineDynamicPolicy(key, value, "load migration: " + migrationError);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("dynamic-policy-load-skip", "key=" + key + " error=" + ex.Message);
				QuarantineDynamicPolicy(key, value, "load parse: " + ex.Message);
			}
		}
		_legacyStoppedPolicyRecordIds.Remove(string.Empty);
		MarkLegacyStoppedPolicyHistoryAfterLoad();
		ReconcileDynamicPolicyEffectBindingsAfterLoad();
		RebuildActivePolicyEffectRuntimeIndex();
		PolicySystemLog.Lifecycle("Player", "load-normalized",
			moduleMigrationSummary.Warnings.Count == 0 ? "success" : "warnings", new PolicyLogContext
			{
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["recordsVisited"] = moduleMigrationSummary.RecordsVisited,
					["recordsChanged"] = moduleMigrationSummary.RecordsChanged,
					["instancesCreated"] = moduleMigrationSummary.InstancesCreated,
					["executableInstances"] = moduleMigrationSummary.ExecutableInstances,
					["inertInstances"] = moduleMigrationSummary.InertInstances,
					["warnings"] = moduleMigrationSummary.Warnings.Count
				}
			});
		PolicySystemLog.WriteModuleLifecycle(
			"Save",
			"*",
			migration: moduleMigrationSummary.ToString(),
			index: "active=" + _activePolicyEffects.Count.ToString(CultureInfo.InvariantCulture)
				+ ", contributions=" + _policyEffectRuntimeIndex.ContributionCount.ToString(CultureInfo.InvariantCulture));
		PolicySystemLog.Lifecycle("Player", "load-summary", "success", new PolicyLogContext
		{
			Counts = new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["history"] = _policyRecordHistory.Count,
				["local"] = _localPolicyRecords.Count,
				["active"] = _activePolicyEffects.Count,
				["dynamic"] = _dynamicPolicyRegistry.Count,
				["quarantinedActive"] = _quarantinedActivePolicyEffectIds.Count,
				["quarantinedDynamic"] = _quarantinedDynamicPolicyIds.Count,
				["migrationVisited"] = moduleMigrationSummary.RecordsVisited,
				["migrationChanged"] = moduleMigrationSummary.RecordsChanged,
				["migrationWarnings"] = moduleMigrationSummary.Warnings.Count
			}
		});
	}

	private void ResetTransientPolicyGenerationStateAfterLoad()
	{
		bool hadTransientState = _generationInProgress || _policyWaitPopupShown || _waitTimeLocked;
		if (_waitTimeLocked || _policyWaitPopupShown)
		{
			EndPolicyWaitPause("load_reset");
		}
		_generationInProgress = false;
		_policyWaitPopupShown = false;
		_waitTimeLocked = false;
		_previousTimeControlMode = CampaignTimeControlMode.Stop;
		_previousTimeControlLock = false;
		_activePolicyRuntimeGeneration++;
		_pendingActivePolicyEffectWork.Clear();
		_queuedActivePolicyEffectIds.Clear();
		_activePolicyEffectModelCache.Clear();
		_activePolicyEffectRuntimeCache.Clear();
		_settlementByIdRuntimeCache.Clear();
		_settlementByIdRuntimeCacheCampaign = null;
		_lastActivePolicyScheduledDay = -1;
		_policySuccessResultVisible = false;
		_policySuccessResultPolicyObjectId = "";
		DeferredOriginalPolicyResults.Clear();
		if (hadTransientState)
		{
		}
	}

	private static bool TryInitializeDynamicPolicyObject(PolicyObject policy, DynamicPolicySaveData data, out string failureReason)
	{
		failureReason = "";
		if (policy == null || data == null || !IsDynamicPolicyId(data.PolicyObjectId))
		{
			failureReason = "动态政策对象或存档数据无效";
			return false;
		}
		try
		{
			string displaySummary = BuildDynamicPolicyDisplaySummary(data);
			policy.Initialize(
				new TextObject(data.PolicyName ?? ""),
				new TextObject(displaySummary),
				new TextObject(FirstNonEmpty(data.LogEntryDescription, data.PolicyContent)),
				new TextObject(data.SecondaryEffects ?? ""),
				data.AuthoritarianWeight,
				data.OligarchicWeight,
				data.EgalitarianWeight);
			return !string.IsNullOrWhiteSpace(policy.Name?.ToString());
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			return false;
		}
	}

	private static bool RegisterUnifiedPlayerPolicy(PolicyDraftRequest request, PolicyGenerationResult generationResult, string feedback, PolicyApplicationResult application, string recordId, long createdUtcTicks, bool effectsEnded = false)
	{
		if (request == null || string.IsNullOrWhiteSpace(recordId))
		{
			return false;
		}
		NpcRulerPolicyRecord unified = new NpcRulerPolicyRecord
		{
			Version = 6,
			PolicyId = recordId,
			ReReviewRootRecordId = request.ReReviewRootRecordId ?? string.Empty,
			ReReviewSourceRecordId = request.ReReviewSourceRecordId ?? string.Empty,
			SupersedesRecordId = request.SupersedesRecordId ?? string.Empty,
			ReReviewReplacementCommitted = request.ReReviewReplacementCommitted,
			PolicyObjectId = IsVassalPolicyRequest(request) ? "af_vassal_policy:" + recordId : DynamicPolicyIdPrefix + recordId,
			AgendaStatus = DynamicPolicyStatusActive,
			BatchId = IsVassalPolicyRequest(request) ? "player_vassal" : "player",
			KingdomId = request.PlayerKingdomId ?? "",
			KingdomName = request.PlayerKingdomName ?? "",
			PolicyKind = IsVassalPolicyRequest(request) ? PolicyScopeVassal : PolicyScopeKingdom,
			IssuerKingdomId = request.IssuerKingdomId ?? request.PlayerKingdomId ?? "",
			IssuerKingdomName = request.IssuerKingdomName ?? request.PlayerKingdomName ?? "",
			PolicyCooldownDay = Math.Max(0, request.SubmittedDay),
			RulerHeroId = Hero.MainHero?.StringId ?? "",
			RulerName = Hero.MainHero?.Name?.ToString() ?? "",
			PolicyName = request.PolicyName ?? "未命名政策",
			PolicyContent = request.PolicyContent ?? "",
			PolicyDigest = generationResult?.MainAssessment?.PolicyContentDigest ?? "",
			PublicFeedback = CleanPolicyDisplayText(feedback ?? ""),
			FeedbackTitle = "《" + (request.PolicyName ?? "未命名政策") + "》的民间回响",
			FeedbackDigest = generationResult?.MainAssessment?.FeedbackDigest ?? "",
			ImpactSummary = CleanPolicyDisplayText((generationResult?.Postprocess?.ImpactSummary ?? "")
				+ (IsVassalPolicyRequest(request)
					? "；独立度 " + request.VassalIndependenceBefore.ToString(CultureInfo.InvariantCulture) + "→" + request.VassalIndependenceAfter.ToString(CultureInfo.InvariantCulture)
					: "")),
			AuthoritarianWeight = generationResult?.MainAssessment?.AuthoritarianWeight,
			OligarchicWeight = generationResult?.MainAssessment?.OligarchicWeight,
			EgalitarianWeight = generationResult?.MainAssessment?.EgalitarianWeight,
			Day = Math.Max(0, request.SubmittedDay),
			GameDate = request.DateText ?? "",
			CreatedUtcTicks = createdUtcTicks,
			IsPlayerPolicy = true,
			DurationDays = (application?.KingdomEffects ?? new List<AppliedKingdomEffect>())
				.Where(x => x != null)
				.Select(x => Math.Max(0, x.DurationDays))
				.DefaultIfEmpty(0)
				.Max(),
			ExecutionReceipts = ClonePolicyEffectExecutionReceipts(
				(application?.KingdomEffects ?? new List<AppliedKingdomEffect>())
					.Where(x => x != null)
					.SelectMany(x => x.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())),
			Effects = (application?.KingdomEffects ?? new List<AppliedKingdomEffect>()).Where(x => x != null).Select(x => new NpcRulerPolicyEffectDto
			{
				TargetKingdomId = x.KingdomId ?? "",
				TargetKingdomName = x.KingdomName ?? "",
				DurationDays = x.DurationDays,
				EffectId = x.EffectId ?? "",
				RemainingDays = effectsEnded ? 0 : Math.Max(0, x.RemainingDays > 0 ? x.RemainingDays : x.DurationDays),
				IsEnded = effectsEnded || (!x.IsPermanentEffect && x.RemainingDays <= 0 && x.DurationDays <= 0),
				Reason = x.Reason ?? "",
				ModuleEffects = ClonePolicyEffectSaveDataList(x.ModuleEffects)
			}).ToList()
		};
		return NpcRulerPolicyBehavior.RegisterPlayerPolicyForExternal(unified);
	}
}
