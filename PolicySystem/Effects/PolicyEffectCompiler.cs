using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyTargets;

namespace AnimusForge.PolicyEffects;

internal delegate bool PolicyEffectModuleResolver(string moduleId, out IPolicyEffectModule module);

internal delegate bool PolicyEffectTargetResolver(
	string handle,
	IPolicyEffectModule module,
	out PolicyEffectResolvedTarget resolved,
	out string error);

internal delegate string PolicyEffectInstanceIdFactory(
	int ordinal,
	string moduleId,
	PolicyEffectCanonicalTargetSet targetSet);

internal sealed class PolicyEffectCompilerRequest
{
	internal string Scope { get; set; } = string.Empty;

	internal string PolicyId { get; set; } = string.Empty;

	internal string ActorHeroId { get; set; } = string.Empty;

	internal string ActorClanId { get; set; } = string.Empty;

	internal string IssuerKingdomId { get; set; } = string.Empty;

	internal string TargetKingdomId { get; set; } = string.Empty;

	internal IReadOnlyCollection<string> AuthorizedCrossKingdomIds { get; set; } = Array.Empty<string>();

	internal float StartDay { get; set; }

	internal float EndDay { get; set; }

	internal bool IsPermanentEffect { get; set; }

	internal PolicyEffectFundingContext Funding { get; set; }

	internal IReadOnlyCollection<string> CandidateModuleIds { get; set; }

	internal IReadOnlyCollection<string> DetailedModuleIds { get; set; }

	internal bool EnforceDetailedModuleAuthorization { get; set; }

	internal IReadOnlyCollection<string> PromptAuthorizedModuleIds { get; set; }

	internal int MaxInstances { get; set; } = 12;

	internal int MaxCompiledInstances { get; set; } = 24;

	internal int MaxPayloadBytes { get; set; } = 4 * 1024;

	internal int MaxTotalPayloadBytes { get; set; } = 32 * 1024;

	internal bool CoalesceEquivalentDisjointTargets { get; set; } = true;

	internal PolicyEffectModuleResolver ModuleResolver { get; set; }
}

internal static class PlayerPolicyMaintenancePlanner
{
	internal static bool IsSettlementDue(int submittedDay, int lastSettlementDay, int currentDay)
	{
		return currentDay > submittedDay && lastSettlementDay < currentDay;
	}

	internal static int AdvanceEffectDay(bool isPermanentEffect, int remainingDays)
	{
		return isPermanentEffect ? Math.Max(0, remainingDays) : Math.Max(0, remainingDays - 1);
	}

	internal static bool[] AllocateStrictOldestPrefix(IReadOnlyList<int> dailyCosts, int availableGold)
	{
		IReadOnlyList<int> costs = dailyCosts ?? Array.Empty<int>();
		bool[] funded = new bool[costs.Count];
		int remainingGold = Math.Max(0, availableGold);
		bool chargedPrefixOpen = true;
		for (int index = 0; index < costs.Count; index++)
		{
			int cost = Math.Max(0, costs[index]);
			if (cost == 0)
			{
				funded[index] = true;
				continue;
			}
			if (chargedPrefixOpen && cost <= remainingGold)
			{
				funded[index] = true;
				remainingGold -= cost;
				continue;
			}
			chargedPrefixOpen = false;
		}
		return funded;
	}

	internal static bool IsSettlementGoldDeltaConfirmed(int beforeGold, int afterGold, int expectedDelta)
	{
		return (long)afterGold - beforeGold == expectedDelta;
	}
}

internal sealed class PolicyEffectResolvedTarget
{
	internal string Handle { get; set; } = string.Empty;

	internal PolicyEffectTargetKind SelectorKind { get; set; }

	internal PolicyEffectCanonicalTargetSet CanonicalTargetSet { get; set; }
}

internal sealed class PolicyEffectCompiledWireEffect
{
	internal int WireIndex { get; set; }
	internal int EffectPlanVersion { get; set; }
	internal string MechanismId { get; set; } = string.Empty;
	internal PolicyEffectMechanismKind MechanismKind { get; set; }
	internal PolicyEffectMechanismRole MechanismRole { get; set; }
	internal bool SourceOmitted { get; set; }
	internal bool DestinationOmitted { get; set; }

	internal IPolicyEffectModule Module { get; set; }

	internal string SourceModuleId { get; set; } = string.Empty;

	internal IReadOnlyList<string> TargetHandles { get; set; }

	internal PolicyEffectCanonicalTargetSet TargetSet { get; set; }

	internal PolicyEffectPayload NormalizedPayload { get; set; }

	internal PolicyEffectPayload FundedPayload { get; set; }

	internal PolicyEffectPreparedInstance PreparedInstance { get; set; }

	internal PolicyEffectInstanceSaveData SaveData { get; set; }

	internal string DuplicateKey { get; set; } = string.Empty;

	internal bool OutsideDetailedRecall { get; set; }
}

internal sealed class PolicyEffectPendingWireEffect
{
	internal int WireIndex { get; set; }
	internal int InstanceOrdinal { get; set; }
	internal int EffectPlanVersion { get; set; }
	internal string MechanismId { get; set; } = string.Empty;
	internal PolicyEffectMechanismKind MechanismKind { get; set; }
	internal PolicyEffectMechanismRole MechanismRole { get; set; }
	internal bool SourceOmitted { get; set; }
	internal bool DestinationOmitted { get; set; }
	internal bool IsLegacyPlan { get; set; }

	internal IPolicyEffectModule Module { get; set; }
	internal string PromptModuleId { get; set; } = string.Empty;
	internal string SourceModuleId { get; set; } = string.Empty;

	internal List<string> TargetHandles { get; set; } = new List<string>();

	internal PolicyEffectCanonicalTargetSet TargetSet { get; set; }

	internal PolicyEffectPayload NormalizedPayload { get; set; }

	internal PolicyEffectPayload FundedPayload { get; set; }

	internal string Reason { get; set; } = string.Empty;
}

internal sealed class PolicyEffectCompilerResult
{
	internal IReadOnlyList<PolicyEffectCompiledWireEffect> Effects { get; set; }
		= Array.Empty<PolicyEffectCompiledWireEffect>();

	internal IReadOnlyCollection<string> OutsideDetailedRecallModuleIds { get; set; }
		= Array.Empty<string>();
}

internal static class PolicyEffectCompiler
{
	internal static bool TryCompile(
		IReadOnlyList<PolicyEffectWireEffect> wireEffects,
		PolicyEffectCompilerRequest request,
		PolicyEffectTargetResolver targetResolver,
		PolicyEffectInstanceIdFactory instanceIdFactory,
		out PolicyEffectCompilerResult result,
		out string error)
	{
		result = null;
		error = string.Empty;
		if (!TryValidateRequest(request, targetResolver, instanceIdFactory, out error))
		{
			return false;
		}

		IReadOnlyList<PolicyEffectWireEffect> effects = wireEffects ?? Array.Empty<PolicyEffectWireEffect>();
		if (effects.Count > request.MaxInstances)
		{
			error = "policy effect count before coalescing exceeds " + request.MaxInstances;
			return false;
		}

		PolicyEffectModuleResolver moduleResolver = request.ModuleResolver ?? PolicyEffectModuleCatalog.TryGet;
		if (!TryCreateCandidateAuthorization(
			request.CandidateModuleIds,
			request.Scope,
			request.ModuleResolver,
			moduleResolver,
			out PolicyEffectModuleAuthorization candidateAuthorization,
			out error))
		{
			return false;
		}
		PolicyEffectModuleAuthorization promptAuthorization = null;
		if (request.EnforceDetailedModuleAuthorization
			&& !TryCreateCandidateAuthorization(
				request.PromptAuthorizedModuleIds,
				request.Scope,
				request.ModuleResolver,
				moduleResolver,
				out promptAuthorization,
				out error))
		{
			return false;
		}
		HashSet<string> detailedIds = ResolveCanonicalModuleIds(request.DetailedModuleIds, moduleResolver);
		List<PolicyEffectPendingWireEffect> pending = new List<PolicyEffectPendingWireEffect>(effects.Count);
		int totalPayloadBytes = 0;

		for (int wireIndex = 0; wireIndex < effects.Count; wireIndex++)
		{
			PolicyEffectWireEffect wire = effects[wireIndex];
			if (!TryNormalizeEffectPlan(wire, wireIndex, out PolicyEffectPendingWireEffect plan, out error))
			{
				return false;
			}
			if (plan.IsLegacyPlan)
			{
				plan.MechanismId = PolicyEffectPlanDefaults.BuildIndependentMechanismId(request.PolicyId);
			}
			string requestedModuleId = (wire?.ModuleId ?? string.Empty).Trim();
			if (wire == null || requestedModuleId.Length == 0 || wire.Payload == null || wire.Payload.Type == JTokenType.Null)
			{
				error = "Policy effect wire " + (wireIndex + 1) + " is missing moduleId or payload.";
				return false;
			}
			if (!moduleResolver(requestedModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor == null
				|| !PolicyEffectModuleCatalog.IsAllowedForScope(module, request.Scope))
			{
				error = "moduleId is not registered, not recalled, or not valid for the current scope: " + requestedModuleId;
				return false;
			}
			if (!TryResolveAuthorizedSourceModuleId(
				wire.SourceModuleId,
				module.Id,
				candidateAuthorization,
				out string sourceModuleId,
				out error))
			{
				return false;
			}
			if (request.EnforceDetailedModuleAuthorization
				&& !TryResolveAuthorizedSourceModuleId(
					wire.SourceModuleId,
					module.Id,
					promptAuthorization,
					out _,
					out error))
			{
				error = "moduleId was not authorized by the detailed prompt: " + requestedModuleId
					+ "; " + error;
				return false;
			}
			if (!TryValidatePayloadSize(wire.Payload, request, ref totalPayloadBytes, out error))
			{
				return false;
			}

			List<string> handles = (wire.TargetHandles ?? new List<string>())
				.Select(value => (value ?? string.Empty).Trim())
				.Where(value => value.Length > 0)
				.ToList();
			if (handles.Count == 0 || handles.Count != handles.Distinct(StringComparer.OrdinalIgnoreCase).Count())
			{
				error = "Policy effect wire " + (wireIndex + 1) + " must contain non-empty unique targetHandles.";
				return false;
			}

			PolicyEffectCanonicalTargetSet targetSet = new PolicyEffectCanonicalTargetSet();
			foreach (string handle in handles)
			{
				if (!targetResolver(handle, module, out PolicyEffectResolvedTarget resolved, out string targetError)
					|| resolved?.CanonicalTargetSet == null)
				{
					error = "Module " + module.Id + " does not allow target handle " + handle + ": " + (targetError ?? string.Empty);
					return false;
				}
				resolved = ApplyActorClanTargetExclusion(module, request.ActorClanId, resolved);
				if (!IsResolvedTargetAuthorizedForModule(module, resolved, request.IssuerKingdomId))
				{
					error = !IsSelectorAuthorizedForModule(module, resolved)
						? "Module " + module.Id + " does not allow target handle " + handle + ": "
						: "Module " + module.Id + " target handle " + handle
							+ " has no executable target in its canonical target set.";
					return false;
				}
				MergeCanonicalTargetSet(targetSet, resolved.CanonicalTargetSet);
				AddUnique(targetSet.SelectorHandles, FirstNonEmpty(resolved.Handle, handle));
			}
			targetSet = ApplyActorClanTargetExclusion(module, request.ActorClanId, targetSet);
			if (targetSet.TargetPlans.Count > 0
				&& handles.Any(handle => !IsTargetPlanHandle(handle)))
			{
				error = "TargetPlan must be the sole authoritative target expression for a policy effect wire.";
				return false;
			}
			if (!HasTargetForModule(module, targetSet))
			{
				error = "Module " + module.Id + " has no executable target in its canonical target set.";
				return false;
			}
			if (!PolicyEffectTargetJurisdiction.TryAuthorizeExplicitKingdomTargets(
				module,
				targetSet,
				request.TargetKingdomId,
				request.IssuerKingdomId,
				request.AuthorizedCrossKingdomIds,
				out targetSet,
				out string jurisdictionError))
			{
				error = "Module " + module.Id + " target jurisdiction is invalid: " + jurisdictionError;
				return false;
			}

			PolicyEffectPayload normalizedPayload = null;
			string normalizeError = string.Empty;
			if (!TryPreparePayloadToken(module, wire.Payload, moduleResolver, out JToken preparedPayload, out string preparePayloadError)
				|| !module.TryNormalizePayload(
					preparedPayload,
					request.Scope,
					out normalizedPayload,
					out normalizeError))
			{
				error = "Module " + module.Id + " payload is invalid: "
					+ FirstNonEmpty(preparePayloadError, normalizeError);
				return false;
			}
			if (!module.TryApplyFunding(
				normalizedPayload,
				request.Funding,
				out PolicyEffectPayload fundedPayload,
				out string fundingError))
			{
				error = "Module " + module.Id + " funding failed: " + fundingError;
				return false;
			}
			plan.WireIndex = wireIndex;
			plan.InstanceOrdinal = wireIndex;
			plan.Module = module;
			plan.PromptModuleId = module.Id;
			plan.SourceModuleId = sourceModuleId;
			plan.TargetHandles = NormalizeIds(handles);
			plan.TargetSet = targetSet;
			plan.NormalizedPayload = normalizedPayload;
			plan.FundedPayload = fundedPayload;
			plan.Reason = (wire.Reason ?? string.Empty).Trim();

			if (module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite)
			{
				if (!(module is IPolicyEffectCompositeModule composite))
				{
					error = "Composite module does not implement its expansion contract: " + module.Id;
					return false;
				}
				PolicyEffectCompileContext expansionContext = new PolicyEffectCompileContext
				{
					PolicyId = request.PolicyId ?? string.Empty,
					ActorHeroId = request.ActorHeroId ?? string.Empty,
					Module = module,
					TargetSet = targetSet,
					Payload = fundedPayload,
					Funding = request.Funding,
					StartDay = request.StartDay,
					EndDay = request.EndDay,
					SourceScope = request.Scope,
					SourceModuleId = sourceModuleId,
					Reason = plan.Reason
				};
				if (!composite.TryExpand(
					expansionContext,
					fundedPayload,
					out IReadOnlyList<PolicyEffectCompositeChild> children,
					out string expansionError))
				{
					error = "Composite module " + module.Id + " expansion failed: " + (expansionError ?? string.Empty);
					return false;
				}

				IReadOnlyList<PolicyEffectCompositeChild> expandedChildren
					= children ?? Array.Empty<PolicyEffectCompositeChild>();
				for (int childIndex = 0; childIndex < expandedChildren.Count; childIndex++)
				{
					PolicyEffectCompositeChild child = expandedChildren[childIndex];
					string childModuleId = (child?.ModuleId ?? string.Empty).Trim();
					if (child?.Payload == null
						|| !moduleResolver(childModuleId, out IPolicyEffectModule childModule)
						|| childModule?.Descriptor == null
						|| childModule.Descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite
						|| !candidateAuthorization.IsAuthorized(sourceModuleId, childModule.Id)
						|| !PolicyEffectModuleCatalog.IsAllowedForScope(childModule, request.Scope)
						|| !HasTargetForModule(childModule, targetSet))
					{
						error = "Composite module " + module.Id + " produced an invalid child: " + childModuleId;
						return false;
					}
					JToken childPayloadToken = JToken.FromObject(child.Payload);
					string childPrepareError = string.Empty;
					string childNormalizeError = string.Empty;
					PolicyEffectPayload normalizedChildPayload = null;
					if (!TryPreparePayloadToken(childModule, childPayloadToken, moduleResolver, out JToken preparedChildPayload, out childPrepareError)
						|| !childModule.TryNormalizePayload(preparedChildPayload, request.Scope, out normalizedChildPayload, out childNormalizeError))
					{
						error = "Composite module " + module.Id + " child payload is invalid: "
							+ FirstNonEmpty(childPrepareError, childNormalizeError);
						return false;
					}
					PolicyEffectPendingWireEffect expanded = ClonePendingPlan(plan);
					expanded.InstanceOrdinal = wireIndex + (childIndex * request.MaxInstances);
					expanded.Module = childModule;
					expanded.PromptModuleId = module.Id;
					expanded.SourceModuleId = sourceModuleId;
					expanded.NormalizedPayload = normalizedChildPayload;
					expanded.FundedPayload = normalizedChildPayload;
					if (!TryAppendPendingEffect(pending, expanded, request, out error))
					{
						return false;
					}
				}
				continue;
			}

			if (!TryAppendPendingEffect(pending, plan, request, out error))
			{
				return false;
			}
		}

		if (pending.Count > request.MaxCompiledInstances)
		{
			error = "compiled policy effect count exceeds " + request.MaxCompiledInstances;
			return false;
		}

		if (!TryValidateMechanismGroups(pending, out error))
		{
			return false;
		}

		HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> outsideDetailedIds = new HashSet<string>(StringComparer.Ordinal);
		List<string> outsideDetailedOrdered = new List<string>();
		List<PolicyEffectCompiledWireEffect> compiled = new List<PolicyEffectCompiledWireEffect>(pending.Count);
		foreach (PolicyEffectPendingWireEffect item in pending.OrderBy(value => value.WireIndex))
		{
			IPolicyEffectModule module = item.Module;
			PolicyEffectCanonicalTargetSet targetSet = ApplyActorClanTargetExclusion(
				module,
				request.ActorClanId,
				item.TargetSet);
			if (!HasTargetForModule(module, targetSet))
			{
				error = "Module " + module.Id + " has no executable target in its canonical target set.";
				return false;
			}
			string duplicateKey = module.Id + "\u001f" + item.MechanismId + "\u001f" + item.MechanismRole
				+ "\u001f" + BuildCanonicalTargetSignature(targetSet);
			string instanceId = (instanceIdFactory(item.InstanceOrdinal, module.Id, targetSet) ?? string.Empty).Trim();
			if (instanceId.Length == 0 || !instanceIds.Add(instanceId))
			{
				error = "policy effect instanceId is empty or duplicated: " + instanceId;
				return false;
			}

			string idempotencyKey = instanceId + ":v" + module.Descriptor.PayloadSchemaVersion;
			PolicyEffectPrepareResult prepare = module.Prepare(new PolicyEffectCompileContext
			{
				InstanceId = instanceId,
				PolicyId = request.PolicyId ?? string.Empty,
				ActorHeroId = request.ActorHeroId ?? string.Empty,
				Module = module,
				TargetSet = targetSet,
				Payload = item.FundedPayload,
				Funding = request.Funding,
				IdempotencyKey = idempotencyKey,
				StartDay = request.StartDay,
				EndDay = request.EndDay,
				SourceScope = request.Scope,
				SourceModuleId = item.SourceModuleId,
				Reason = item.Reason
			}, item.FundedPayload);
			if (prepare?.Success != true
				|| prepare.PreparedInstance?.Instance?.Payload == null
				|| prepare.PreparedInstance.Descriptor == null)
			{
				error = "Module " + module.Id + " prepare failed: " + (prepare?.Error ?? "unknown error");
				return false;
			}

			PolicyEffectPreparedInstance prepared = prepare.PreparedInstance;
			PolicyEffectInstance runtime = prepared.Instance;
			runtime.ActorHeroId = FirstNonEmpty(runtime.ActorHeroId, request.ActorHeroId);
			// Module implementations own payload preparation, not identity or
			// authorization provenance. Reassert both IDs after Prepare so a
			// faulty module cannot widen or rewrite its frozen lineage.
			runtime.ModuleId = module.Id;
			runtime.SourceModuleId = item.SourceModuleId;
			runtime.EffectPlanVersion = item.EffectPlanVersion;
			runtime.MechanismId = item.MechanismId;
			runtime.MechanismKind = item.MechanismKind;
			runtime.MechanismRole = item.MechanismRole;
			runtime.SourceOmitted = item.SourceOmitted;
			runtime.DestinationOmitted = item.DestinationOmitted;
			PolicyEffectCanonicalTargetSet preparedTargetSet = ApplyActorClanTargetExclusion(
				module,
				request.ActorClanId,
				runtime.TargetSet ?? targetSet);
			if (!HasTargetForModule(module, preparedTargetSet))
			{
				error = "Module " + module.Id + " prepare produced no executable target.";
				return false;
			}
			if (!PolicyEffectTargetJurisdiction.TryAuthorizeExplicitKingdomTargets(
				module,
				preparedTargetSet,
				request.TargetKingdomId,
				request.IssuerKingdomId,
				request.AuthorizedCrossKingdomIds,
				out preparedTargetSet,
				out string preparedJurisdictionError))
			{
				error = "Module " + module.Id + " prepare produced invalid target jurisdiction: "
					+ preparedJurisdictionError;
				return false;
			}
			runtime.TargetSet = preparedTargetSet;
			PolicyEffectInstanceSaveData saveData = new PolicyEffectInstanceSaveData
			{
				EffectPlanVersion = item.EffectPlanVersion,
				MechanismId = item.MechanismId,
				MechanismKind = item.MechanismKind,
				MechanismRole = item.MechanismRole,
				SourceOmitted = item.SourceOmitted,
				DestinationOmitted = item.DestinationOmitted,
				InstanceId = runtime.InstanceId ?? instanceId,
				PolicyId = runtime.PolicyId ?? request.PolicyId ?? string.Empty,
				ActorHeroId = FirstNonEmpty(runtime.ActorHeroId, request.ActorHeroId),
				ModuleId = runtime.ModuleId,
				SourceModuleId = runtime.SourceModuleId,
				PayloadSchemaVersion = module.Descriptor.PayloadSchemaVersion,
				Payload = JToken.FromObject(runtime.Payload),
				TargetSet = preparedTargetSet,
				LifecycleState = PolicyEffectLifecycleState.Prepared,
				StateSchemaVersion = module.Descriptor.RuntimeStateSchemaVersion,
				StartDay = runtime.StartDay,
				EndDay = runtime.EndDay,
				SourceScope = runtime.SourceScope ?? request.Scope,
				Reason = runtime.Reason ?? item.Reason
			};
			string recallModuleId = FirstNonEmpty(item.SourceModuleId, item.PromptModuleId, module.Id);
			bool outsideDetailed = !detailedIds.Contains(recallModuleId);
			if (outsideDetailed && outsideDetailedIds.Add(recallModuleId))
			{
				outsideDetailedOrdered.Add(recallModuleId);
			}
			compiled.Add(new PolicyEffectCompiledWireEffect
			{
				WireIndex = item.WireIndex,
				EffectPlanVersion = item.EffectPlanVersion,
				MechanismId = item.MechanismId,
				MechanismKind = item.MechanismKind,
				MechanismRole = item.MechanismRole,
				SourceOmitted = item.SourceOmitted,
				DestinationOmitted = item.DestinationOmitted,
				Module = module,
				SourceModuleId = item.SourceModuleId,
				TargetHandles = item.TargetHandles,
				TargetSet = preparedTargetSet,
				NormalizedPayload = item.NormalizedPayload,
				FundedPayload = item.FundedPayload,
				PreparedInstance = prepared,
				SaveData = saveData,
				DuplicateKey = duplicateKey,
				OutsideDetailedRecall = outsideDetailed
			});
		}
		if (!PolicyEffectMechanismContract.TryFreeze(compiled.Select(effect => effect.SaveData), out error))
		{
			return false;
		}
		foreach (PolicyEffectCompiledWireEffect effect in compiled)
		{
			PolicyEffectInstance runtime = effect.PreparedInstance?.Instance;
			PolicyEffectInstanceSaveData frozen = effect.SaveData;
			if (runtime == null || frozen == null)
			{
				error = "Compiled EffectPlan lost its prepared runtime or frozen save contract.";
				return false;
			}
			runtime.MechanismContractVersion = frozen.MechanismContractVersion;
			runtime.MechanismContractHash = frozen.MechanismContractHash ?? string.Empty;
			runtime.ExpectedMechanismLegIds = new List<string>(
				frozen.ExpectedMechanismLegIds ?? new List<string>());
		}

		result = new PolicyEffectCompilerResult
		{
			Effects = compiled,
			OutsideDetailedRecallModuleIds = outsideDetailedOrdered
		};
		return true;
	}

	private static bool TryNormalizeEffectPlan(
		PolicyEffectWireEffect wire,
		int wireIndex,
		out PolicyEffectPendingWireEffect plan,
		out string error)
	{
		plan = null;
		error = string.Empty;
		if (wire == null)
		{
			error = "Policy effect wire " + (wireIndex + 1) + " is null.";
			return false;
		}
		if (wire.EffectPlanVersion == 0)
		{
			plan = new PolicyEffectPendingWireEffect
			{
				EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
				MechanismId = string.Empty,
				MechanismKind = PolicyEffectMechanismKind.Independent,
				MechanismRole = PolicyEffectMechanismRole.Subject,
				IsLegacyPlan = true
			};
			return true;
		}
		if (wire.EffectPlanVersion != PolicyEffectPlanVersions.CurrentVersion)
		{
			error = "Unsupported effectPlanVersion on wire " + (wireIndex + 1) + ": " + wire.EffectPlanVersion;
			return false;
		}

		string mechanismId = (wire.MechanismId ?? string.Empty).Trim();
		if (!IsValidMechanismId(mechanismId))
		{
			error = "Invalid mechanismId on wire " + (wireIndex + 1) + ".";
			return false;
		}
		if (!Enum.IsDefined(typeof(PolicyEffectMechanismKind), wire.MechanismKind)
			|| !Enum.IsDefined(typeof(PolicyEffectMechanismRole), wire.MechanismRole))
		{
			error = "Unknown EffectPlan enum on wire " + (wireIndex + 1) + ".";
			return false;
		}
		if (wire.MechanismKind == PolicyEffectMechanismKind.Independent
			&& (wire.MechanismRole != PolicyEffectMechanismRole.Subject
				|| wire.SourceOmitted
				|| wire.DestinationOmitted))
		{
			error = "Independent mechanism " + mechanismId + " only permits a subject role without omissions.";
			return false;
		}
		if (wire.MechanismKind == PolicyEffectMechanismKind.Linked
			&& (wire.MechanismRole == PolicyEffectMechanismRole.Subject
				|| (wire.SourceOmitted && wire.DestinationOmitted)))
		{
			error = "Linked mechanism " + mechanismId + " has an invalid role or omits both sides.";
			return false;
		}
		plan = new PolicyEffectPendingWireEffect
		{
			EffectPlanVersion = wire.EffectPlanVersion,
			MechanismId = mechanismId,
			MechanismKind = wire.MechanismKind,
			MechanismRole = wire.MechanismRole,
			SourceOmitted = wire.SourceOmitted,
			DestinationOmitted = wire.DestinationOmitted
		};
		return true;
	}

	private static bool TryValidateMechanismGroups(
		IReadOnlyCollection<PolicyEffectPendingWireEffect> effects,
		out string error)
	{
		error = string.Empty;
		List<PolicyEffectPendingWireEffect> explicitEffects = (effects ?? Array.Empty<PolicyEffectPendingWireEffect>())
			.Where(effect => effect != null && !effect.IsLegacyPlan)
			.ToList();
		IEnumerable<IGrouping<string, PolicyEffectPendingWireEffect>> groups = explicitEffects
			.GroupBy(effect => effect.MechanismId, StringComparer.Ordinal);
		List<IGrouping<string, PolicyEffectPendingWireEffect>> materialized = groups.ToList();
		if (materialized.Count > PolicyEffectPlanVersions.MaximumMechanisms)
		{
			error = "EffectPlan mechanism count exceeds " + PolicyEffectPlanVersions.MaximumMechanisms + ".";
			return false;
		}
		foreach (IGrouping<string, PolicyEffectPendingWireEffect> group in materialized)
		{
			PolicyEffectPendingWireEffect first = group.First();
			if (group.Any(effect => effect.MechanismKind != first.MechanismKind
				|| effect.SourceOmitted != first.SourceOmitted
				|| effect.DestinationOmitted != first.DestinationOmitted))
			{
				error = "Mechanism " + group.Key + " has conflicting kind or omission flags.";
				return false;
			}
			if (first.MechanismKind == PolicyEffectMechanismKind.Independent)
			{
				continue;
			}
			bool hasSource = group.Any(effect => IsSourceRole(effect.MechanismRole));
			bool hasDestination = group.Any(effect => IsDestinationRole(effect.MechanismRole));
			if ((first.SourceOmitted && hasSource) || (!first.SourceOmitted && !hasSource))
			{
				error = "Linked mechanism " + group.Key + " has a missing or contradictory source side.";
				return false;
			}
			if ((first.DestinationOmitted && hasDestination) || (!first.DestinationOmitted && !hasDestination))
			{
				error = "Linked mechanism " + group.Key + " has a missing or contradictory destination side.";
				return false;
			}
			if (!TryValidateAtomicHeroGoldMechanism(group, out error))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryValidateAtomicHeroGoldMechanism(
		IEnumerable<PolicyEffectPendingWireEffect> group,
		out string error)
	{
		if ((group ?? Array.Empty<PolicyEffectPendingWireEffect>())
			.Any(effect => effect?.Module is IAtomicHeroGoldPolicyEffectModule))
		{
			error = "Hero gold effects only support independent mechanisms.";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static PolicyEffectPendingWireEffect ClonePendingPlan(PolicyEffectPendingWireEffect source)
	{
		return new PolicyEffectPendingWireEffect
		{
			WireIndex = source?.WireIndex ?? 0,
			InstanceOrdinal = source?.InstanceOrdinal ?? 0,
			EffectPlanVersion = source?.EffectPlanVersion ?? PolicyEffectPlanVersions.CurrentVersion,
			MechanismId = source?.MechanismId ?? string.Empty,
			MechanismKind = source?.MechanismKind ?? PolicyEffectMechanismKind.Independent,
			MechanismRole = source?.MechanismRole ?? PolicyEffectMechanismRole.Subject,
			SourceOmitted = source?.SourceOmitted == true,
			DestinationOmitted = source?.DestinationOmitted == true,
			IsLegacyPlan = source?.IsLegacyPlan == true,
			Module = source?.Module,
			PromptModuleId = source?.PromptModuleId ?? string.Empty,
			SourceModuleId = source?.SourceModuleId ?? string.Empty,
			TargetHandles = NormalizeIds(source?.TargetHandles),
			TargetSet = NormalizeCanonicalTargetSet(source?.TargetSet),
			NormalizedPayload = source?.NormalizedPayload,
			FundedPayload = source?.FundedPayload,
			Reason = source?.Reason ?? string.Empty
		};
	}

	private static bool TryAppendPendingEffect(
		ICollection<PolicyEffectPendingWireEffect> pending,
		PolicyEffectPendingWireEffect candidate,
		PolicyEffectCompilerRequest request,
		out string error)
	{
		error = string.Empty;
		IPolicyEffectModule module = candidate?.Module;
		if (candidate == null || module?.Descriptor == null || candidate.FundedPayload == null)
		{
			error = "compiled policy effect candidate is incomplete";
			return false;
		}
		if (candidate.MechanismKind == PolicyEffectMechanismKind.Linked
			&& module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot)
		{
			error = "OneShot effect cannot participate in linked mechanism " + candidate.MechanismId + ".";
			return false;
		}

		PolicyEffectPendingWireEffect samePayload = request.CoalesceEquivalentDisjointTargets
			? pending.FirstOrDefault(item =>
				string.Equals(item.Module?.Id, module.Id, StringComparison.Ordinal)
				&& string.Equals(item.PromptModuleId, candidate.PromptModuleId, StringComparison.Ordinal)
				&& string.Equals(item.SourceModuleId, candidate.SourceModuleId, StringComparison.Ordinal)
				&& AreMechanismLegsCoalescible(item, candidate)
				&& ArePayloadsEqual(item.FundedPayload, candidate.FundedPayload))
			: null;
		PolicyEffectCanonicalTargetSet prospectiveTargetSet = new PolicyEffectCanonicalTargetSet();
		if (samePayload != null)
		{
			MergeCanonicalTargetSet(prospectiveTargetSet, samePayload.TargetSet);
		}
		MergeCanonicalTargetSet(prospectiveTargetSet, candidate.TargetSet);
		prospectiveTargetSet = NormalizeCanonicalTargetSet(prospectiveTargetSet);
		if (prospectiveTargetSet.TargetPlans.Count > 0
			&& prospectiveTargetSet.SelectorHandles.Any(handle => !IsTargetPlanHandle(handle)))
		{
			error = "TargetPlan cannot be coalesced with legacy or direct target handles.";
			return false;
		}

		PolicyEffectPendingWireEffect conflicting = pending.FirstOrDefault(item =>
			!ReferenceEquals(item, samePayload)
				&& string.Equals(item.Module?.Id, module.Id, StringComparison.Ordinal)
				&& HaveExecutableTargetOverlap(module, item.TargetSet, prospectiveTargetSet));
		if (conflicting != null)
		{
			error = "Different mechanism legs or funded payloads for the same module overlap the same executable target: " + module.Id
				+ " (wire " + (conflicting.WireIndex + 1) + " and wire " + (candidate.WireIndex + 1) + ")";
			AnimusForge.PolicySystemLog.Failure("Effect", "policy-effect-overlap-conflict", error,
				"policyId=" + (request.PolicyId ?? string.Empty));
			return false;
		}

		if (samePayload != null)
		{
			samePayload.TargetSet = prospectiveTargetSet;
			foreach (string handle in candidate.TargetHandles ?? new List<string>())
			{
				AddUnique(samePayload.TargetHandles, handle);
			}
			samePayload.TargetHandles = NormalizeIds(samePayload.TargetHandles);
			if (samePayload.Reason.Length == 0)
			{
				samePayload.Reason = candidate.Reason ?? string.Empty;
			}
			AnimusForge.PolicySystemLog.Write("Effect", "policy-effect-overlap-coalesced",
				"policyId=" + (request.PolicyId ?? string.Empty)
				+ " moduleId=" + module.Id
				+ " firstWire=" + (samePayload.WireIndex + 1)
				+ " mergedWire=" + (candidate.WireIndex + 1));
			return true;
		}

		candidate.TargetSet = prospectiveTargetSet;
		pending.Add(candidate);
		return true;
	}

	private static bool AreMechanismLegsCoalescible(
		PolicyEffectPendingWireEffect left,
		PolicyEffectPendingWireEffect right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (left.IsLegacyPlan && right.IsLegacyPlan)
		{
			return true;
		}
		return !left.IsLegacyPlan
			&& !right.IsLegacyPlan
			&& string.Equals(left.MechanismId, right.MechanismId, StringComparison.Ordinal)
			&& left.MechanismKind == right.MechanismKind
			&& left.MechanismRole == right.MechanismRole
			&& left.SourceOmitted == right.SourceOmitted
			&& left.DestinationOmitted == right.DestinationOmitted;
	}

	private static bool IsValidMechanismId(string value)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Length > PolicyEffectPlanVersions.MaximumMechanismIdLength)
		{
			return false;
		}
		return value.All(character => char.IsLetterOrDigit(character) || character == '_' || character == '-');
	}

	private static bool IsSourceRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Source || role == PolicyEffectMechanismRole.Cost;
	}

	private static bool IsDestinationRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Destination || role == PolicyEffectMechanismRole.Beneficiary;
	}

	private static bool TryValidateRequest(
		PolicyEffectCompilerRequest request,
		PolicyEffectTargetResolver targetResolver,
		PolicyEffectInstanceIdFactory instanceIdFactory,
		out string error)
	{
		error = string.Empty;
		if (request == null
			|| string.IsNullOrWhiteSpace(request.Scope)
			|| request.Funding == null
			|| request.CandidateModuleIds == null
			|| (request.EnforceDetailedModuleAuthorization && request.PromptAuthorizedModuleIds == null)
			|| targetResolver == null
			|| instanceIdFactory == null
			|| request.MaxInstances <= 0
			|| request.MaxCompiledInstances < request.MaxInstances
			|| request.MaxPayloadBytes <= 0
			|| request.MaxTotalPayloadBytes < request.MaxPayloadBytes
			|| float.IsNaN(request.StartDay)
			|| float.IsInfinity(request.StartDay)
			|| float.IsNaN(request.EndDay)
			|| float.IsInfinity(request.EndDay)
			|| (request.IsPermanentEffect
				? request.EndDay != 0f
				: request.EndDay <= request.StartDay))
		{
			error = "policy effect compiler request 无效";
			return false;
		}
		request.Scope = request.Scope.Trim();
		return true;
	}

	private static HashSet<string> ResolveCanonicalModuleIds(
		IEnumerable<string> moduleIds,
		PolicyEffectModuleResolver resolver)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
		foreach (string value in moduleIds ?? Array.Empty<string>())
		{
			string moduleId = (value ?? string.Empty).Trim();
			if (moduleId.Length > 0 && resolver(moduleId, out IPolicyEffectModule module) && module != null)
			{
				result.Add(module.Id);
			}
		}
		return result;
	}

	private static bool TryCreateCandidateAuthorization(
		IEnumerable<string> sourceModuleIds,
		string scope,
		PolicyEffectModuleResolver requestedResolver,
		PolicyEffectModuleResolver effectiveResolver,
		out PolicyEffectModuleAuthorization authorization,
		out string error)
	{
		if (requestedResolver == null)
		{
			return PolicyEffectModuleCatalog.TryCreateAuthorization(
				sourceModuleIds,
				scope,
				out authorization,
				out error);
		}

		authorization = null;
		error = string.Empty;
		List<string> normalizedSources = new List<string>();
		HashSet<string> seenSources = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, HashSet<string>> sourcesByRuntimeId
			= new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		foreach (string requestedId in sourceModuleIds ?? Array.Empty<string>())
		{
			string cleanId = (requestedId ?? string.Empty).Trim();
			if (cleanId.Length == 0
				|| !effectiveResolver(cleanId, out IPolicyEffectModule sourceModule)
				|| sourceModule?.Descriptor == null
				|| !PolicyEffectModuleCatalog.IsAllowedForScope(sourceModule, scope)
				|| !seenSources.Add(sourceModule.Id))
			{
				continue;
			}
			normalizedSources.Add(sourceModule.Id);
			List<string> runtimeIds = new List<string> { sourceModule.Id };
			if (sourceModule is IPolicyEffectCompositeModule composite)
			{
				foreach (string runtimeId in composite.RuntimeModuleIds ?? Array.Empty<string>())
				{
					string cleanRuntimeId = (runtimeId ?? string.Empty).Trim();
					if (cleanRuntimeId.Length == 0
						|| !effectiveResolver(cleanRuntimeId, out IPolicyEffectModule runtimeModule)
						|| runtimeModule?.Descriptor == null
						|| runtimeModule.Descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite
						|| !PolicyEffectModuleCatalog.IsAllowedForScope(runtimeModule, scope))
					{
						error = "Composite module declares an invalid runtime descendant: "
							+ sourceModule.Id + " -> " + cleanRuntimeId;
						return false;
					}
					runtimeIds.Add(runtimeModule.Id);
				}
			}
			foreach (string runtimeId in runtimeIds.Distinct(StringComparer.Ordinal))
			{
				if (!sourcesByRuntimeId.TryGetValue(runtimeId, out HashSet<string> sources))
				{
					sources = new HashSet<string>(StringComparer.Ordinal);
					sourcesByRuntimeId.Add(runtimeId, sources);
				}
				sources.Add(sourceModule.Id);
			}
		}
		authorization = new PolicyEffectModuleAuthorization(
			normalizedSources,
			sourcesByRuntimeId.ToDictionary(
				pair => pair.Key,
				pair => (IReadOnlyCollection<string>)pair.Value.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
				StringComparer.Ordinal));
		return true;
	}

	private static bool TryResolveAuthorizedSourceModuleId(
		string trustedSourceModuleId,
		string runtimeModuleId,
		PolicyEffectModuleAuthorization authorization,
		out string sourceModuleId,
		out string error)
	{
		sourceModuleId = string.Empty;
		error = string.Empty;
		string runtimeId = (runtimeModuleId ?? string.Empty).Trim();
		string trustedSourceId = (trustedSourceModuleId ?? string.Empty).Trim();
		if (authorization == null || runtimeId.Length == 0)
		{
			error = "policy effect candidate authorization is unavailable";
			return false;
		}
		if (trustedSourceId.Length == 0)
		{
			if (!authorization.ContainsSource(runtimeId))
			{
				error = "runtime module requires trusted source lineage and cannot be selected directly: " + runtimeId;
				return false;
			}
			sourceModuleId = runtimeId;
			return true;
		}
		if (!authorization.IsAuthorized(trustedSourceId, runtimeId))
		{
			error = "runtime module is outside its frozen source-module authorization: "
				+ trustedSourceId + " -> " + runtimeId;
			return false;
		}
		sourceModuleId = trustedSourceId;
		return true;
	}

	private static bool TryValidatePayloadSize(
		JToken payload,
		PolicyEffectCompilerRequest request,
		ref int totalPayloadBytes,
		out string error)
	{
		error = string.Empty;
		if (ContainsTypeMetadata(payload))
		{
			error = "policy effect payload 不得包含 $type 元数据";
			return false;
		}
		int payloadBytes = Encoding.UTF8.GetByteCount(payload?.ToString(Formatting.None) ?? "null");
		if (payloadBytes > request.MaxPayloadBytes)
		{
			error = "单个 policy effect payload 不得超过 " + request.MaxPayloadBytes + " bytes";
			return false;
		}
		if (totalPayloadBytes > request.MaxTotalPayloadBytes - payloadBytes)
		{
			error = "policy effect payload 总量不得超过 " + request.MaxTotalPayloadBytes + " bytes";
			return false;
		}
		totalPayloadBytes += payloadBytes;
		return true;
	}

	private static bool TryPreparePayloadToken(
		IPolicyEffectModule module,
		JToken rawPayload,
		PolicyEffectModuleResolver moduleResolver,
		out JToken preparedPayload,
		out string error)
	{
		preparedPayload = null;
		error = string.Empty;
		if (module == null || rawPayload == null || rawPayload.Type == JTokenType.Null)
		{
			error = "效果 payload 不能为空";
			return false;
		}

		JObject envelope;
		if (typeof(NumericPolicyEffectPayload).IsAssignableFrom(module.PayloadType)
			&& (rawPayload.Type == JTokenType.Integer || rawPayload.Type == JTokenType.Float))
		{
			envelope = new JObject { ["value"] = rawPayload.DeepClone() };
		}
		else if (rawPayload is JObject rawObject)
		{
			envelope = (JObject)rawObject.DeepClone();
		}
		else
		{
			preparedPayload = rawPayload.DeepClone();
			return true;
		}

		string embeddedModuleId = (string)envelope["moduleId"];
		if (string.IsNullOrWhiteSpace(embeddedModuleId)
			|| (moduleResolver(embeddedModuleId, out IPolicyEffectModule embeddedModule)
				&& string.Equals(embeddedModule?.Id, module.Id, StringComparison.Ordinal)))
		{
			envelope["moduleId"] = module.Id;
		}
		if (envelope["schemaVersion"] == null)
		{
			envelope["schemaVersion"] = module.Descriptor.PayloadSchemaVersion;
		}
		preparedPayload = envelope;
		return true;
	}

	private static bool ContainsTypeMetadata(JToken token)
	{
		if (token is JObject objectToken)
		{
			foreach (JProperty property in objectToken.Properties())
			{
				if (string.Equals(property.Name, "$type", StringComparison.OrdinalIgnoreCase)
					|| ContainsTypeMetadata(property.Value))
				{
					return true;
				}
			}
		}
		else if (token is JArray arrayToken)
		{
			foreach (JToken child in arrayToken)
			{
				if (ContainsTypeMetadata(child))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void MergeCanonicalTargetSet(
		PolicyEffectCanonicalTargetSet destination,
		PolicyEffectCanonicalTargetSet source)
	{
		if (destination == null || source == null)
		{
			return;
		}
		destination.StructureVersion = Math.Max(destination.StructureVersion, source.StructureVersion);
		destination.JurisdictionKind = PolicyEffectTargetJurisdiction.MergeKind(
			destination.JurisdictionKind,
			source.JurisdictionKind);
		MergeIds(destination.AuthorizedCrossKingdomIds, source.AuthorizedCrossKingdomIds);
		MergeIds(destination.SelectorHandles, source.SelectorHandles);
		MergeIds(destination.SelectorIds, source.SelectorIds);
		destination.TargetPlans = PolicyTargetPlanResolver.NormalizePlans(
			(destination.TargetPlans ?? new List<PolicyTargetPlanSaveData>())
				.Concat(source.TargetPlans ?? new List<PolicyTargetPlanSaveData>()));
		MergeIds(destination.SettlementIds, source.SettlementIds);
		MergeIds(destination.TownIds, source.TownIds);
		MergeIds(destination.VillageIds, source.VillageIds);
		MergeIds(destination.ClanIds, source.ClanIds);
		MergeIds(destination.KingdomIds, source.KingdomIds);
		MergeIds(destination.HeroIds, source.HeroIds);
		MergeIds(destination.ParentSettlementIds, source.ParentSettlementIds);
		destination.FollowCurrentRulingClan |= source.FollowCurrentRulingClan;
	}

	private static void MergeIds(List<string> destination, IEnumerable<string> source)
	{
		foreach (string value in source ?? Array.Empty<string>())
		{
			AddUnique(destination, value);
		}
	}

	private static void AddUnique(List<string> values, string value)
	{
		string normalized = (value ?? string.Empty).Trim();
		if (normalized.Length > 0 && !values.Contains(normalized, StringComparer.OrdinalIgnoreCase))
		{
			values.Add(normalized);
		}
	}

	private static PolicyEffectCanonicalTargetSet NormalizeCanonicalTargetSet(PolicyEffectCanonicalTargetSet targetSet)
	{
		return new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = Math.Max(1, targetSet?.StructureVersion ?? 1),
			JurisdictionKind = targetSet?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled,
			AuthorizedCrossKingdomIds = NormalizeIds(targetSet?.AuthorizedCrossKingdomIds),
			SelectorHandles = NormalizeIds(targetSet?.SelectorHandles),
			SelectorIds = NormalizeIds(targetSet?.SelectorIds),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(targetSet?.TargetPlans),
			SettlementIds = NormalizeIds(targetSet?.SettlementIds),
			TownIds = NormalizeIds(targetSet?.TownIds),
			VillageIds = NormalizeIds(targetSet?.VillageIds),
			ClanIds = NormalizeIds(targetSet?.ClanIds),
			KingdomIds = NormalizeIds(targetSet?.KingdomIds),
			HeroIds = NormalizeIds(targetSet?.HeroIds),
			ParentSettlementIds = NormalizeIds(targetSet?.ParentSettlementIds),
			FollowCurrentRulingClan = targetSet?.FollowCurrentRulingClan == true
		};
	}

	internal static PolicyEffectResolvedTarget ApplyActorClanTargetExclusion(
		IPolicyEffectModule module,
		string actorClanId,
		PolicyEffectResolvedTarget resolved)
	{
		if (resolved == null)
		{
			return null;
		}
		return new PolicyEffectResolvedTarget
		{
			Handle = resolved.Handle,
			SelectorKind = resolved.SelectorKind,
			CanonicalTargetSet = ApplyActorClanTargetExclusion(
				module,
				actorClanId,
				resolved.CanonicalTargetSet)
		};
	}

	internal static PolicyEffectCanonicalTargetSet ApplyActorClanTargetExclusion(
		IPolicyEffectModule module,
		string actorClanId,
		PolicyEffectCanonicalTargetSet targetSet)
	{
		PolicyEffectCanonicalTargetSet filtered = NormalizeCanonicalTargetSet(targetSet);
		string normalizedActorClanId = (actorClanId ?? string.Empty).Trim();
		if (module?.Descriptor?.ExcludeActorClanTargets == true
			&& normalizedActorClanId.Length > 0)
		{
			filtered.ClanIds = filtered.ClanIds
				.Where(clanId => !string.Equals(
					clanId,
					normalizedActorClanId,
					StringComparison.OrdinalIgnoreCase))
				.ToList();
		}
		return filtered;
	}

	private static List<string> NormalizeIds(IEnumerable<string> values)
	{
		return (values ?? Array.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	internal static bool IsResolvedTargetAuthorizedForModule(
		IPolicyEffectModule module,
		PolicyEffectResolvedTarget resolved,
		string issuerKingdomId = "")
	{
		return resolved?.CanonicalTargetSet != null
			&& IsSelectorAuthorizedForModule(module, resolved)
			&& HasTargetForModule(module, resolved.CanonicalTargetSet)
			&& IsTargetBindingAuthorizedForModule(module, resolved.CanonicalTargetSet, issuerKingdomId);
	}

	internal static bool IsTargetBindingAuthorizedForModule(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet targetSet,
		string issuerKingdomId)
	{
		if (module?.Descriptor?.TargetBinding != PolicyEffectTargetBindingKind.IssuerKingdom)
		{
			return true;
		}

		string normalizedIssuerKingdomId = (issuerKingdomId ?? string.Empty).Trim();
		if (normalizedIssuerKingdomId.Length == 0 || targetSet?.KingdomIds == null)
		{
			return false;
		}

		HashSet<string> kingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string kingdomId in targetSet.KingdomIds)
		{
			string normalizedKingdomId = (kingdomId ?? string.Empty).Trim();
			if (normalizedKingdomId.Length > 0)
			{
				kingdomIds.Add(normalizedKingdomId);
			}
		}
		return kingdomIds.Count == 1 && kingdomIds.Contains(normalizedIssuerKingdomId);
	}

	private static bool HasTargetForModule(IPolicyEffectModule module, PolicyEffectCanonicalTargetSet targetSet)
	{
		if (module?.Descriptor?.ExcludeActorClanTargets == true
			&& module.Descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan)
			&& (targetSet?.ClanIds?.Count ?? 0) == 0)
		{
			return false;
		}
		foreach (PolicyEffectTargetKind kind in module?.Descriptor?.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>())
		{
			switch (kind)
			{
				case PolicyEffectTargetKind.Settlement: if ((targetSet?.SettlementIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Town: if ((targetSet?.TownIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Village: if ((targetSet?.VillageIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Clan: if ((targetSet?.ClanIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Kingdom: if ((targetSet?.KingdomIds?.Count ?? 0) > 0) return true; break;
				case PolicyEffectTargetKind.Hero: if ((targetSet?.HeroIds?.Count ?? 0) > 0) return true; break;
			}
		}
		return module?.Descriptor?.ExecutionKind != PolicyEffectExecutionKind.OneShot
			&& PolicyTargetPlanResolver.NormalizePlans(targetSet?.TargetPlans).Count > 0;
	}

	private static bool IsSelectorAuthorizedForModule(
		IPolicyEffectModule module,
		PolicyEffectResolvedTarget resolved)
	{
		if (module?.Descriptor?.AllowedSelectorKinds?.Contains(resolved.SelectorKind) == true)
		{
			return true;
		}
		return module?.Descriptor?.TargetProjection
				== PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader
			&& PolicyTargetPlanResolver.NormalizePlans(resolved?.CanonicalTargetSet?.TargetPlans).Count > 0;
	}

	private static bool IsTargetPlanHandle(string handle)
	{
		string normalized = (handle ?? string.Empty).Trim();
		return normalized.Length > 0
			&& char.ToUpperInvariant(normalized[0]) == 'P'
			&& (normalized.Length == 1 || char.IsDigit(normalized[1]) || normalized[1] == ':');
	}

	private static bool ArePayloadsEqual(PolicyEffectPayload left, PolicyEffectPayload right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}
		if (left == null || right == null || left.GetType() != right.GetType())
		{
			return false;
		}
		return JToken.DeepEquals(JToken.FromObject(left), JToken.FromObject(right));
	}

	private static bool HaveExecutableTargetOverlap(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		HashSet<string> rightPlanSignatures = new HashSet<string>(
			PolicyTargetPlanResolver.NormalizePlans(right?.TargetPlans).Select(plan => plan.NormalizedSignature),
			StringComparer.Ordinal);
		if (rightPlanSignatures.Count > 0
			&& PolicyTargetPlanResolver.NormalizePlans(left?.TargetPlans)
				.Any(plan => rightPlanSignatures.Contains(plan.NormalizedSignature)))
		{
			return true;
		}
		foreach (PolicyEffectTargetKind kind in module?.Descriptor?.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>())
		{
			IEnumerable<string> leftIds = GetTargetIds(left, kind);
			HashSet<string> rightIds = new HashSet<string>(GetTargetIds(right, kind), StringComparer.OrdinalIgnoreCase);
			if (rightIds.Count > 0 && leftIds.Any(rightIds.Contains))
			{
				return true;
			}
		}
		return false;
	}

	private static IEnumerable<string> GetTargetIds(
		PolicyEffectCanonicalTargetSet targetSet,
		PolicyEffectTargetKind kind)
	{
		switch (kind)
		{
			case PolicyEffectTargetKind.Settlement: return (IEnumerable<string>)targetSet?.SettlementIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Town: return (IEnumerable<string>)targetSet?.TownIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Village: return (IEnumerable<string>)targetSet?.VillageIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Clan: return (IEnumerable<string>)targetSet?.ClanIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Kingdom: return (IEnumerable<string>)targetSet?.KingdomIds ?? Array.Empty<string>();
			case PolicyEffectTargetKind.Hero: return (IEnumerable<string>)targetSet?.HeroIds ?? Array.Empty<string>();
			default: return Array.Empty<string>();
		}
	}

	private static string BuildCanonicalTargetSignature(PolicyEffectCanonicalTargetSet targetSet)
	{
		PolicyEffectCanonicalTargetSet normalized = NormalizeCanonicalTargetSet(targetSet);
		return "X=" + string.Join(",", normalized.SelectorIds)
			+ "|J=" + normalized.JurisdictionKind
			+ "|AK=" + string.Join(",", normalized.AuthorizedCrossKingdomIds)
			+ "|TP=" + string.Join(",", normalized.TargetPlans.Select(plan => plan.NormalizedSignature))
			+ "|S=" + string.Join(",", normalized.SettlementIds)
			+ "|T=" + string.Join(",", normalized.TownIds)
			+ "|V=" + string.Join(",", normalized.VillageIds)
			+ "|C=" + string.Join(",", normalized.ClanIds)
			+ "|K=" + string.Join(",", normalized.KingdomIds)
			+ "|H=" + string.Join(",", normalized.HeroIds)
			+ "|P=" + string.Join(",", normalized.ParentSettlementIds)
			+ "|R=" + (normalized.FollowCurrentRulingClan ? "1" : "0");
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.FirstOrDefault(value => value.Length > 0) ?? string.Empty;
	}
}
