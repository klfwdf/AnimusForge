using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.PolicyEffects;

internal static class PolicyEffectPayloadSchemas
{
	internal static JObject CreateMarkerSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray(),
			["properties"] = new JObject(),
			["additionalProperties"] = false
		};
	}

	internal static JObject CreateNumericValueSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("value"),
			["properties"] = new JObject
			{
				["value"] = new JObject { ["type"] = "number" }
			},
			["additionalProperties"] = false
		};
	}

	internal static JObject CreateIntegerValueSchema()
	{
		return new JObject
		{
			["type"] = "object",
			["required"] = new JArray("value"),
			["properties"] = new JObject
			{
				["value"] = new JObject { ["type"] = "integer" }
			},
			["additionalProperties"] = false
		};
	}
}

internal static class PolicyEffectModuleRuntimeAdapters
{
	internal static IReadOnlyList<PolicyEffectModelContribution> BuildNumericModelContributions(
		IPolicyEffectModule module,
		PolicyEffectPreparedInstance preparedInstance)
	{
		if (module == null
			|| preparedInstance?.Instance?.Payload is not NumericPolicyEffectPayload payload
			|| preparedInstance.Instance.TargetSet == null)
		{
			return Array.Empty<PolicyEffectModelContribution>();
		}
		PolicyEffectCanonicalTargetSet targetSet = preparedInstance.Instance.TargetSet;
		List<PolicyEffectModelContribution> result = new List<PolicyEffectModelContribution>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectTargetKind targetKind in module.Descriptor.TargetKinds)
		{
			foreach (string targetId in GetTargetIds(targetSet, targetKind))
			{
				string normalizedTargetId = (targetId ?? string.Empty).Trim();
				if (normalizedTargetId.Length <= 0 || !seen.Add(targetKind + "\u001f" + normalizedTargetId))
				{
					continue;
				}
				result.Add(new PolicyEffectModelContribution
				{
					InstanceId = preparedInstance.Instance.InstanceId,
					ModuleId = module.Id,
					Hook = module.Descriptor.Hook,
					TargetKind = targetKind,
					TargetId = normalizedTargetId,
					Value = payload.Value,
					DisplayText = module.DescribePayload(payload)
				});
			}
		}
		return result;
	}

	private static IEnumerable<string> GetTargetIds(PolicyEffectCanonicalTargetSet targetSet, PolicyEffectTargetKind targetKind)
	{
		switch (targetKind)
		{
			case PolicyEffectTargetKind.Settlement: return targetSet.SettlementIds != null ? (IEnumerable<string>)targetSet.SettlementIds : Array.Empty<string>();
			case PolicyEffectTargetKind.Town: return targetSet.TownIds != null ? (IEnumerable<string>)targetSet.TownIds : Array.Empty<string>();
			case PolicyEffectTargetKind.Village: return targetSet.VillageIds != null ? (IEnumerable<string>)targetSet.VillageIds : Array.Empty<string>();
			case PolicyEffectTargetKind.Clan: return targetSet.ClanIds != null ? (IEnumerable<string>)targetSet.ClanIds : Array.Empty<string>();
			case PolicyEffectTargetKind.Kingdom: return targetSet.KingdomIds != null ? (IEnumerable<string>)targetSet.KingdomIds : Array.Empty<string>();
			case PolicyEffectTargetKind.Hero: return targetSet.HeroIds != null ? (IEnumerable<string>)targetSet.HeroIds : Array.Empty<string>();
			default: return Array.Empty<string>();
		}
	}
}

internal sealed class PolicyEffectModuleAuthorization
{
	private readonly HashSet<string> _sourceModuleIds;
	private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _sourceModuleIdsByRuntimeModuleId;

	internal PolicyEffectModuleAuthorization(
		IEnumerable<string> sourceModuleIds,
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> sourceModuleIdsByRuntimeModuleId)
	{
		_sourceModuleIds = new HashSet<string>(sourceModuleIds ?? Array.Empty<string>(), StringComparer.Ordinal);
		_sourceModuleIdsByRuntimeModuleId = sourceModuleIdsByRuntimeModuleId
			?? new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
	}

	internal IReadOnlyCollection<string> SourceModuleIds => _sourceModuleIds;

	internal bool ContainsSource(string sourceModuleId)
	{
		return _sourceModuleIds.Contains((sourceModuleId ?? string.Empty).Trim());
	}

	internal bool IsAuthorized(string sourceModuleId, string runtimeModuleId)
	{
		string sourceId = (sourceModuleId ?? string.Empty).Trim();
		string runtimeId = (runtimeModuleId ?? string.Empty).Trim();
		return sourceId.Length > 0
			&& runtimeId.Length > 0
			&& _sourceModuleIdsByRuntimeModuleId.TryGetValue(runtimeId, out IReadOnlyCollection<string> sources)
			&& sources.Contains(sourceId, StringComparer.Ordinal);
	}

	internal IReadOnlyCollection<string> GetAuthorizedSourceModuleIds(string runtimeModuleId)
	{
		return _sourceModuleIdsByRuntimeModuleId.TryGetValue(
			(runtimeModuleId ?? string.Empty).Trim(),
			out IReadOnlyCollection<string> sources)
			? sources
			: Array.Empty<string>();
	}
}

internal static class PolicyEffectModuleCatalog
{
	private sealed class CatalogSnapshot
	{
		internal CatalogSnapshot(
			IReadOnlyList<IPolicyEffectModule> ordered,
			IReadOnlyList<IPolicyEffectModule> promptVisible,
			IReadOnlyDictionary<string, IPolicyEffectModule> byCanonicalId,
			IReadOnlyDictionary<string, IPolicyEffectModule> byAnyId,
			IReadOnlyDictionary<string, IReadOnlyCollection<string>> authorizedRuntimeModuleIdsBySourceModuleId,
			IReadOnlyDictionary<string, IReadOnlyList<IPolicyEffectModule>> modulesByScope,
			IReadOnlyDictionary<PolicyEffectHook, IReadOnlyList<IPolicyEffectModule>> modulesByHook,
			IReadOnlyDictionary<string, string> capabilityCatalogByScope)
		{
			Ordered = ordered;
			PromptVisible = promptVisible;
			ByCanonicalId = byCanonicalId;
			ByAnyId = byAnyId;
			AuthorizedRuntimeModuleIdsBySourceModuleId = authorizedRuntimeModuleIdsBySourceModuleId;
			ModulesByScope = modulesByScope;
			ModulesByHook = modulesByHook;
			CapabilityCatalogByScope = capabilityCatalogByScope;
		}

		internal IReadOnlyList<IPolicyEffectModule> Ordered { get; }

		internal IReadOnlyList<IPolicyEffectModule> PromptVisible { get; }

		internal IReadOnlyDictionary<string, IPolicyEffectModule> ByCanonicalId { get; }

		internal IReadOnlyDictionary<string, IPolicyEffectModule> ByAnyId { get; }

		internal IReadOnlyDictionary<string, IReadOnlyCollection<string>> AuthorizedRuntimeModuleIdsBySourceModuleId { get; }

		internal IReadOnlyDictionary<string, IReadOnlyList<IPolicyEffectModule>> ModulesByScope { get; }

		internal IReadOnlyDictionary<PolicyEffectHook, IReadOnlyList<IPolicyEffectModule>> ModulesByHook { get; }

		internal IReadOnlyDictionary<string, string> CapabilityCatalogByScope { get; }
	}

	private static readonly Lazy<CatalogSnapshot> Snapshot = new Lazy<CatalogSnapshot>(BuildSnapshot, true);

	// Candidate enumeration is prompt-facing. Hidden runtime and compatibility
	// modules remain available through ID and hook lookup.
	internal static IReadOnlyList<IPolicyEffectModule> Modules => Snapshot.Value.PromptVisible;

	internal static void ValidateModuleSetForContractTests(IEnumerable<IPolicyEffectModule> sourceModules)
	{
		List<IPolicyEffectModule> modules = (sourceModules ?? Enumerable.Empty<IPolicyEffectModule>())
			.Where(module => module != null)
			.ToList();
		if (modules.Count == 0)
		{
			throw new InvalidOperationException("契约测试模块集合不能为空。");
		}
		HashSet<string> knownScopes = new HashSet<string>(new[]
		{
			PolicyEffectScopes.Kingdom,
			PolicyEffectScopes.Local,
			PolicyEffectScopes.Vassal
		}, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, IPolicyEffectModule> byAnyId = new Dictionary<string, IPolicyEffectModule>(StringComparer.Ordinal);
		HashSet<int> orders = new HashSet<int>();
		foreach (IPolicyEffectModule module in modules)
		{
			ValidateModule(module, knownScopes);
			if (byAnyId.ContainsKey(module.Id))
			{
				throw new InvalidOperationException("政策效果模块 ID 重复: " + module.Id);
			}
			byAnyId.Add(module.Id, module);
			if (!orders.Add(module.Order))
			{
				throw new InvalidOperationException("政策效果模块 Order 重复: " + module.Order);
			}
		}
		foreach (IPolicyEffectModule module in modules)
		{
			foreach (string legacyId in module.Descriptor.LegacyIds)
			{
				if (byAnyId.ContainsKey(legacyId))
				{
					throw new InvalidOperationException("政策效果模块 LegacyId 与现有 ID 冲突: " + module.Id + " / " + legacyId);
				}
				byAnyId.Add(legacyId, module);
			}
		}
	}

	internal static bool TryGet(string id, out IPolicyEffectModule module)
	{
		return Snapshot.Value.ByAnyId.TryGetValue((id ?? string.Empty).Trim(), out module);
	}

	internal static bool TryGetCanonical(string id, out IPolicyEffectModule module)
	{
		return Snapshot.Value.ByCanonicalId.TryGetValue((id ?? string.Empty).Trim(), out module);
	}

	internal static bool TryResolveCanonicalId(string id, out string canonicalId)
	{
		if (TryGet(id, out IPolicyEffectModule module))
		{
			canonicalId = module.Id;
			return true;
		}
		canonicalId = string.Empty;
		return false;
	}

	internal static IReadOnlyCollection<string> GetAuthorizedRuntimeModuleIds(string sourceModuleId)
	{
		if (!TryResolveCanonicalId(sourceModuleId, out string canonicalSourceId))
		{
			return Array.Empty<string>();
		}
		return Snapshot.Value.AuthorizedRuntimeModuleIdsBySourceModuleId.TryGetValue(
			canonicalSourceId,
			out IReadOnlyCollection<string> runtimeModuleIds)
			? runtimeModuleIds
			: Array.Empty<string>();
	}

	internal static bool IsAuthorizedRuntimeModule(string sourceModuleId, string runtimeModuleId)
	{
		return TryResolveCanonicalId(sourceModuleId, out string canonicalSourceId)
			&& TryResolveCanonicalId(runtimeModuleId, out string canonicalRuntimeId)
			&& Snapshot.Value.AuthorizedRuntimeModuleIdsBySourceModuleId.TryGetValue(
				canonicalSourceId,
				out IReadOnlyCollection<string> runtimeModuleIds)
			&& runtimeModuleIds.Contains(canonicalRuntimeId, StringComparer.Ordinal);
	}

	internal static bool TryCreateAuthorization(
		IEnumerable<string> sourceModuleIds,
		string scope,
		out PolicyEffectModuleAuthorization authorization,
		out string error)
	{
		authorization = null;
		error = string.Empty;
		List<string> normalizedSources = new List<string>();
		HashSet<string> seenSources = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, HashSet<string>> sourceIdsByRuntimeId
			= new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		foreach (string requestedId in sourceModuleIds ?? Array.Empty<string>())
		{
			if (!TryGet(requestedId, out IPolicyEffectModule sourceModule)
				|| !IsAllowedForScope(sourceModule, scope)
				|| !seenSources.Add(sourceModule.Id))
			{
				continue;
			}
			normalizedSources.Add(sourceModule.Id);
			if (!Snapshot.Value.AuthorizedRuntimeModuleIdsBySourceModuleId.TryGetValue(
				sourceModule.Id,
				out IReadOnlyCollection<string> runtimeModuleIds))
			{
				error = "policy effect source module has no cached runtime lineage: " + sourceModule.Id;
				return false;
			}
			foreach (string runtimeModuleId in runtimeModuleIds)
			{
				if (!sourceIdsByRuntimeId.TryGetValue(runtimeModuleId, out HashSet<string> sources))
				{
					sources = new HashSet<string>(StringComparer.Ordinal);
					sourceIdsByRuntimeId.Add(runtimeModuleId, sources);
				}
				sources.Add(sourceModule.Id);
			}
		}
		authorization = new PolicyEffectModuleAuthorization(
			normalizedSources,
			sourceIdsByRuntimeId.ToDictionary(
				pair => pair.Key,
				pair => (IReadOnlyCollection<string>)pair.Value.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
				StringComparer.Ordinal));
		return true;
	}

	internal static bool IsAllowedForScope(IPolicyEffectModule module, string scope)
	{
		return module?.AllowedScopes != null
			&& module.AllowedScopes.Any(value => string.Equals(value, scope, StringComparison.OrdinalIgnoreCase));
	}

	internal static string GetCapabilityCatalog(string scope)
	{
		return Snapshot.Value.CapabilityCatalogByScope.TryGetValue((scope ?? string.Empty).Trim(), out string catalog)
			? catalog
			: string.Empty;
	}

	internal static string GetCapabilityCatalog(string scope, IEnumerable<string> candidateModuleIds)
	{
		return string.Join("\n", ResolveModulesForScope(scope, candidateModuleIds)
			.Select(module => "- " + module.Id + ": " + module.CatalogSummary.Trim()));
	}

	internal static IReadOnlyList<IPolicyEffectModule> GetModulesForScope(string scope)
	{
		return Snapshot.Value.ModulesByScope.TryGetValue((scope ?? string.Empty).Trim(), out IReadOnlyList<IPolicyEffectModule> modules)
			? modules
			: Array.Empty<IPolicyEffectModule>();
	}

	internal static IReadOnlyList<IPolicyEffectModule> GetModulesForHook(PolicyEffectHook hook)
	{
		return Snapshot.Value.ModulesByHook.TryGetValue(hook, out IReadOnlyList<IPolicyEffectModule> modules)
			? modules
			: Array.Empty<IPolicyEffectModule>();
	}

	internal static string BuildMainInstructions(IEnumerable<string> moduleIds)
	{
		return BuildInstructions(ResolveModules(moduleIds), BuildMainInstruction);
	}

	internal static string BuildMainInstructions(string scope, IEnumerable<string> candidateModuleIds)
	{
		return BuildInstructions(ResolveModulesForScope(scope, candidateModuleIds), BuildMainInstruction);
	}

	internal static string BuildMainInstructions(
		string scope,
		IEnumerable<string> frozenCandidateModuleIds,
		IEnumerable<string> requestedDetailedModuleIds)
	{
		return BuildInstructions(
			ResolveAuthorizedPromptModules(scope, frozenCandidateModuleIds, requestedDetailedModuleIds),
			BuildMainInstruction);
	}

	internal static string BuildPostprocessRules(IEnumerable<string> moduleIds)
	{
		return BuildEvaluationInstructions(ResolveModules(moduleIds), includePayloadSchema: false);
	}

	internal static string BuildPostprocessRules(string scope, IEnumerable<string> candidateModuleIds)
	{
		return BuildEvaluationInstructions(ResolveModulesForScope(scope, candidateModuleIds), includePayloadSchema: false);
	}

	internal static string BuildPostprocessRules(
		string scope,
		IEnumerable<string> frozenCandidateModuleIds,
		IEnumerable<string> requestedDetailedModuleIds)
	{
		return BuildEvaluationInstructions(
			ResolveAuthorizedPromptModules(scope, frozenCandidateModuleIds, requestedDetailedModuleIds),
			includePayloadSchema: false);
	}

	internal static string BuildPayloadPromptRules(string scope, IEnumerable<string> candidateModuleIds)
	{
		return BuildEvaluationInstructions(ResolveModulesForScope(scope, candidateModuleIds), includePayloadSchema: true);
	}

	internal static string BuildPayloadPromptRules(
		string scope,
		IEnumerable<string> frozenCandidateModuleIds,
		IEnumerable<string> requestedDetailedModuleIds)
	{
		return BuildCandidatePayloadInstructions(
			ResolveAuthorizedCandidateModules(scope, frozenCandidateModuleIds),
			ResolveAuthorizedPromptModules(scope, frozenCandidateModuleIds, requestedDetailedModuleIds));
	}

	private static string BuildCandidatePayloadInstructions(
		IReadOnlyList<IPolicyEffectModule> candidates,
		IReadOnlyList<IPolicyEffectModule> details)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return string.Empty;
		}
		StringBuilder builder = new StringBuilder();
		builder.Append("【全部效果共同要求】")
			.Append(PolicyEffectPromptService.GetCommonEvaluationPrompt());
		foreach (IPolicyEffectModule module in candidates)
		{
			builder.AppendLine()
				.Append("- ").Append(module.Id)
				.Append(" payload=")
				.Append(module.Descriptor.PayloadPromptSchema.ToString(Formatting.None))
				.Append("；")
				.Append(module.PostprocessRule.Trim());
		}
		if (details != null && details.Count > 0)
		{
			builder.AppendLine()
				.Append("【Detail 可编辑效果判定要求】");
			foreach (IPolicyEffectModule module in details)
			{
				builder.AppendLine()
					.Append("- ").Append(module.Id).Append(": ")
					.Append(PolicyEffectPromptService.GetEvaluationPrompt(module));
			}
		}
		return builder.ToString();
	}

	private static string BuildEvaluationInstructions(
		IReadOnlyList<IPolicyEffectModule> modules,
		bool includePayloadSchema)
	{
		if (modules == null || modules.Count <= 0)
		{
			return string.Empty;
		}
		StringBuilder builder = new StringBuilder();
		builder.Append("【全部效果共同要求】")
			.Append(PolicyEffectPromptService.GetCommonEvaluationPrompt());
		foreach (IPolicyEffectModule module in modules)
		{
			builder.AppendLine();
			builder.Append("- ").Append(module.Id);
			if (includePayloadSchema)
			{
				builder.Append(" payload=")
					.Append(module.Descriptor.PayloadPromptSchema.ToString(Formatting.None))
					.Append("；");
			}
			else
			{
				builder.Append(": ");
			}
			builder
				.Append(module.PostprocessRule.Trim())
				.Append("；【可编辑效果判定要求】")
				.Append(PolicyEffectPromptService.GetEvaluationPrompt(module));
		}
		return builder.ToString();
	}

	private static string BuildMainInstruction(IPolicyEffectModule module)
	{
		if (module == null)
		{
			return string.Empty;
		}
		return PolicyEffectPromptService.GetUnderstandingPrompt(module);
	}

	internal static IReadOnlyList<IPolicyEffectModule> ResolveModules(IEnumerable<string> moduleIds)
	{
		HashSet<string> requested = ResolveCanonicalIds(moduleIds);
		return requested.Count <= 0
			? Array.Empty<IPolicyEffectModule>()
			: Snapshot.Value.PromptVisible.Where(module => requested.Contains(module.Id)).ToArray();
	}

	internal static IReadOnlyList<IPolicyEffectModule> ResolveModulesForScope(string scope, IEnumerable<string> moduleIds)
	{
		HashSet<string> requested = ResolveCanonicalIds(moduleIds);
		if (requested.Count <= 0)
		{
			return Array.Empty<IPolicyEffectModule>();
		}
		return GetModulesForScope(scope).Where(module => requested.Contains(module.Id)).ToArray();
	}

	private static IReadOnlyList<IPolicyEffectModule> ResolveAuthorizedPromptModules(
		string scope,
		IEnumerable<string> frozenCandidateModuleIds,
		IEnumerable<string> requestedDetailedModuleIds)
	{
		if (!TryCreateAuthorization(
				frozenCandidateModuleIds,
				scope,
				out PolicyEffectModuleAuthorization authorization,
				out _))
		{
			return Array.Empty<IPolicyEffectModule>();
		}

		HashSet<string> requestedDetails = ResolveCanonicalIds(requestedDetailedModuleIds);
		if (requestedDetails.Count <= 0)
		{
			return Array.Empty<IPolicyEffectModule>();
		}

		return GetModulesForScope(scope)
			.Where(module => authorization.ContainsSource(module.Id) && requestedDetails.Contains(module.Id))
			.ToArray();
	}

	private static IReadOnlyList<IPolicyEffectModule> ResolveAuthorizedCandidateModules(
		string scope,
		IEnumerable<string> frozenCandidateModuleIds)
	{
		if (!TryCreateAuthorization(
				frozenCandidateModuleIds,
				scope,
				out PolicyEffectModuleAuthorization authorization,
				out _))
		{
			return Array.Empty<IPolicyEffectModule>();
		}
		return GetModulesForScope(scope)
			.Where(module => authorization.ContainsSource(module.Id))
			.ToArray();
	}

	internal static bool TryNormalizePayload(
		string moduleId,
		JToken rawPayload,
		string scope,
		out PolicyEffectPayload normalizedPayload,
		out string error)
	{
		normalizedPayload = null;
		if (!TryResolveModuleForPayload(moduleId, scope, requireScope: true, out IPolicyEffectModule module, out error)
			|| !TryPreparePayloadToken(module, rawPayload, module.Descriptor.PayloadSchemaVersion, out JToken preparedPayload, out error))
		{
			return false;
		}
		return module.TryNormalizePayload(preparedPayload, scope, out normalizedPayload, out error);
	}

	internal static bool TryDeserializePayload(
		string moduleId,
		JToken persistedPayload,
		int sourceVersion,
		out PolicyEffectPayload payload,
		out string error)
	{
		payload = null;
		if (!TryResolveModuleForPayload(moduleId, string.Empty, requireScope: false, out IPolicyEffectModule module, out error)
			|| !TryValidateMigrationSourceVersion(
				module.Descriptor.PayloadSchemaVersion,
				module.Descriptor.MinimumReadablePayloadSchemaVersion,
				module.Descriptor.PayloadMigrationSourceVersions,
				sourceVersion,
				"payload",
				module.Id,
				out error)
			|| !TryPreparePayloadToken(module, persistedPayload, sourceVersion, out JToken preparedPayload, out error))
		{
			return false;
		}
		return module.TryMigratePayload(preparedPayload, sourceVersion, out payload, out error);
	}

	internal static bool TryMigrateRuntimeState(
		string moduleId,
		JToken persistedState,
		int sourceVersion,
		out JToken migratedState,
		out string error)
	{
		migratedState = null;
		int effectiveSourceVersion = Math.Max(1, sourceVersion);
		if (!TryResolveModuleForPayload(moduleId, string.Empty, requireScope: false, out IPolicyEffectModule module, out error))
		{
			return false;
		}
		if (persistedState == null || persistedState.Type == JTokenType.Null)
		{
			error = string.Empty;
			return true;
		}
		if (!TryValidateMigrationSourceVersion(
				module.Descriptor.RuntimeStateSchemaVersion,
				module.Descriptor.MinimumReadableRuntimeStateSchemaVersion,
				module.Descriptor.RuntimeStateMigrationSourceVersions,
				effectiveSourceVersion,
				"runtime state",
				module.Id,
				out error))
		{
			return false;
		}
		return module.TryMigrateRuntimeState(persistedState, effectiveSourceVersion, out migratedState, out error);
	}

	internal static bool TryApplyFunding(
		string moduleId,
		PolicyEffectPayload payload,
		PolicyEffectFundingContext funding,
		out PolicyEffectPayload fundedPayload,
		out string error)
	{
		fundedPayload = null;
		if (!TryResolveModuleForPayload(moduleId, string.Empty, requireScope: false, out IPolicyEffectModule module, out error)
			|| !IsPayloadTypeCompatible(module, payload, out error))
		{
			return false;
		}
		return module.TryApplyFunding(payload, funding, out fundedPayload, out error);
	}

	internal static bool TryFormatPayload(
		string moduleId,
		PolicyEffectPayload payload,
		out string formattedPayload,
		out string error)
	{
		formattedPayload = string.Empty;
		if (!TryResolveModuleForPayload(moduleId, string.Empty, requireScope: false, out IPolicyEffectModule module, out error)
			|| !IsPayloadTypeCompatible(module, payload, out error))
		{
			return false;
		}
		formattedPayload = module.DescribePayload(payload) ?? string.Empty;
		error = string.Empty;
		return true;
	}

	private static string BuildInstructions(
		IEnumerable<IPolicyEffectModule> modules,
		Func<IPolicyEffectModule, string> selector)
	{
		StringBuilder builder = new StringBuilder();
		foreach (IPolicyEffectModule module in modules ?? Enumerable.Empty<IPolicyEffectModule>())
		{
			string instruction = (selector(module) ?? string.Empty).Trim();
			if (instruction.Length <= 0)
			{
				continue;
			}
			if (builder.Length > 0)
			{
				builder.AppendLine();
			}
			builder.Append("- ").Append(module.Id).Append("：").Append(instruction);
		}
		return builder.ToString();
	}

	private static HashSet<string> ResolveCanonicalIds(IEnumerable<string> moduleIds)
	{
		HashSet<string> requested = new HashSet<string>(StringComparer.Ordinal);
		foreach (string moduleId in moduleIds ?? Array.Empty<string>())
		{
			if (TryGet(moduleId, out IPolicyEffectModule module))
			{
				requested.Add(module.Id);
			}
		}
		return requested;
	}

	private static bool TryResolveModuleForPayload(
		string moduleId,
		string scope,
		bool requireScope,
		out IPolicyEffectModule module,
		out string error)
	{
		if (!TryGet(moduleId, out module))
		{
			error = "未注册的政策效果模块: " + ((moduleId ?? string.Empty).Trim());
			return false;
		}
		if (requireScope && !IsAllowedForScope(module, scope))
		{
			error = "政策效果模块不支持当前作用域: " + module.Id + " / " + ((scope ?? string.Empty).Trim());
			module = null;
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryPreparePayloadToken(
		IPolicyEffectModule module,
		JToken rawPayload,
		int schemaVersion,
		out JToken preparedPayload,
		out string error)
	{
		preparedPayload = null;
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
			error = string.Empty;
			return true;
		}
		string embeddedModuleId = (string)envelope["moduleId"];
		if (string.IsNullOrWhiteSpace(embeddedModuleId)
			|| (TryGet(embeddedModuleId, out IPolicyEffectModule embeddedModule)
				&& string.Equals(embeddedModule.Id, module.Id, StringComparison.Ordinal)))
		{
			envelope["moduleId"] = module.Id;
		}
		if (envelope["schemaVersion"] == null)
		{
			envelope["schemaVersion"] = schemaVersion;
		}
		preparedPayload = envelope;
		error = string.Empty;
		return true;
	}

	private static bool IsPayloadTypeCompatible(IPolicyEffectModule module, PolicyEffectPayload payload, out string error)
	{
		if (module == null || payload == null || !module.PayloadType.IsInstanceOfType(payload))
		{
			error = "效果 payload 类型与模块不匹配";
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static CatalogSnapshot BuildSnapshot()
	{
		List<IPolicyEffectModule> modules = Assembly.GetExecutingAssembly()
			.GetCustomAttributes<PolicyEffectModuleRegistrationAttribute>()
			.Select(attribute => attribute?.ModuleType)
			.Where(type => type != null)
			.Select(type => (IPolicyEffectModule)Activator.CreateInstance(type, nonPublic: true))
			.OrderBy(module => module.Order)
			.ThenBy(module => module.Id, StringComparer.Ordinal)
			.ToList();
		if (modules.Count <= 0)
		{
			throw new InvalidOperationException("没有发现政策效果模块。");
		}
		HashSet<string> knownScopes = new HashSet<string>(new[]
		{
			PolicyEffectScopes.Kingdom,
			PolicyEffectScopes.Local,
			PolicyEffectScopes.Vassal
		}, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, IPolicyEffectModule> byCanonicalId = new Dictionary<string, IPolicyEffectModule>(StringComparer.Ordinal);
		HashSet<int> orders = new HashSet<int>();
		foreach (IPolicyEffectModule module in modules)
		{
			ValidateModule(module, knownScopes);
			if (byCanonicalId.ContainsKey(module.Id))
			{
				throw new InvalidOperationException("政策效果模块 ID 重复: " + module.Id);
			}
			byCanonicalId.Add(module.Id, module);
			if (!orders.Add(module.Order))
			{
				throw new InvalidOperationException("政策效果模块 Order 重复: " + module.Order);
			}
		}
		Dictionary<string, IPolicyEffectModule> byAnyId = new Dictionary<string, IPolicyEffectModule>(byCanonicalId, StringComparer.Ordinal);
		foreach (IPolicyEffectModule module in modules)
		{
			foreach (string legacyId in module.Descriptor.LegacyIds)
			{
				if (byAnyId.ContainsKey(legacyId))
				{
					throw new InvalidOperationException("政策效果模块 LegacyId 与现有 ID 冲突: " + module.Id + " / " + legacyId);
				}
				byAnyId.Add(legacyId, module);
			}
		}
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> authorizedRuntimeModuleIdsBySourceModuleId
			= BuildAuthorizedRuntimeModuleIdsBySourceModuleId(modules, byCanonicalId);
		Dictionary<string, IReadOnlyList<IPolicyEffectModule>> modulesByScope = new Dictionary<string, IReadOnlyList<IPolicyEffectModule>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> capabilityCatalogByScope = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string scope in knownScopes)
		{
			IPolicyEffectModule[] scopedModules = modules
				.Where(module => module.Descriptor.PromptVisible && IsAllowedForScope(module, scope))
				.ToArray();
			modulesByScope[scope] = scopedModules;
			capabilityCatalogByScope[scope] = string.Join("\n", scopedModules
				.Select(module => "- " + module.Id + ": " + module.CatalogSummary.Trim()));
		}
		Dictionary<PolicyEffectHook, IReadOnlyList<IPolicyEffectModule>> modulesByHook = Enum.GetValues(typeof(PolicyEffectHook))
			.Cast<PolicyEffectHook>()
			.ToDictionary(
				hook => hook,
				hook => (IReadOnlyList<IPolicyEffectModule>)modules.Where(module => module.Descriptor.Hook == hook).ToArray());
		return new CatalogSnapshot(
			modules.ToArray(),
			modules.Where(module => module.Descriptor.PromptVisible).ToArray(),
			byCanonicalId,
			byAnyId,
			authorizedRuntimeModuleIdsBySourceModuleId,
			modulesByScope,
			modulesByHook,
			capabilityCatalogByScope);
	}

	private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> BuildAuthorizedRuntimeModuleIdsBySourceModuleId(
		IReadOnlyCollection<IPolicyEffectModule> modules,
		IReadOnlyDictionary<string, IPolicyEffectModule> byCanonicalId)
	{
		Dictionary<string, IReadOnlyCollection<string>> result
			= new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
		foreach (IPolicyEffectModule sourceModule in modules ?? Array.Empty<IPolicyEffectModule>())
		{
			List<string> runtimeModuleIds = new List<string> { sourceModule.Id };
			if (sourceModule is IPolicyEffectCompositeModule composite)
			{
				string[] declaredRuntimeIds = (composite.RuntimeModuleIds ?? Array.Empty<string>())
					.Select(value => (value ?? string.Empty).Trim())
					.Where(value => value.Length > 0)
					.ToArray();
				if (declaredRuntimeIds.Length == 0
					|| declaredRuntimeIds.Length != declaredRuntimeIds.Distinct(StringComparer.Ordinal).Count())
				{
					throw new InvalidOperationException(
						"Composite policy effect module must declare a non-empty unique runtime lineage: " + sourceModule.Id);
				}
				foreach (string runtimeModuleId in declaredRuntimeIds)
				{
					if (!byCanonicalId.TryGetValue(runtimeModuleId, out IPolicyEffectModule runtimeModule)
						|| runtimeModule.Descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite
						|| runtimeModule.Descriptor.PromptVisible
						|| sourceModule.AllowedScopes.Any(sourceScope => !IsAllowedForScope(runtimeModule, sourceScope))
						|| !HaveSameTargetKindSet(
							sourceModule.Descriptor.AllowedSelectorKinds,
							runtimeModule.Descriptor.AllowedSelectorKinds)
						|| !HaveSameTargetKindSet(
							sourceModule.Descriptor.TargetKinds,
							runtimeModule.Descriptor.TargetKinds)
						|| runtimeModule.Descriptor.TargetProjection != sourceModule.Descriptor.TargetProjection
						|| runtimeModule.Descriptor.TargetRefresh != sourceModule.Descriptor.TargetRefresh
						|| runtimeModule.Descriptor.AllowIndependentClanTargets
							!= sourceModule.Descriptor.AllowIndependentClanTargets
						|| runtimeModule.Descriptor.AllowCrossKingdomTargets
							!= sourceModule.Descriptor.AllowCrossKingdomTargets
						|| runtimeModule.Descriptor.ExcludeActorClanTargets
							!= sourceModule.Descriptor.ExcludeActorClanTargets)
					{
						throw new InvalidOperationException(
							"Composite policy effect module declares an invalid runtime descendant: "
							+ sourceModule.Id + " -> " + runtimeModuleId);
					}
					runtimeModuleIds.Add(runtimeModule.Id);
				}
			}
			result.Add(sourceModule.Id, runtimeModuleIds.ToArray());
		}
		return result;
	}

	private static bool HaveSameTargetKindSet(
		IReadOnlyCollection<PolicyEffectTargetKind> left,
		IReadOnlyCollection<PolicyEffectTargetKind> right)
	{
		return left != null
			&& right != null
			&& left.Count == right.Count
			&& left.All(right.Contains);
	}

	private static void ValidateModule(IPolicyEffectModule module, ISet<string> knownScopes)
	{
		PolicyEffectModuleDescriptor descriptor = module?.Descriptor;
		string id = (descriptor?.Id ?? string.Empty).Trim();
		string catalogSummary = (descriptor?.CatalogSummary ?? string.Empty).Trim();
		string playerDisplayName = (descriptor?.PlayerDisplayName ?? string.Empty).Trim();
		if (module == null || descriptor == null
			|| id.Length <= 0 || !string.Equals(id, descriptor.Id, StringComparison.Ordinal)
			|| module.Order < 0
			|| (descriptor.PromptVisible && string.IsNullOrWhiteSpace(descriptor.RetrievalText))
			|| (descriptor.PromptVisible && (catalogSummary.Length <= 0 || catalogSummary.Length > 60))
			|| catalogSummary.IndexOfAny(new[] { '\r', '\n' }) >= 0
			|| playerDisplayName.Length <= 0 || playerDisplayName.Length > 60
			|| playerDisplayName.IndexOfAny(new[] { '\r', '\n' }) >= 0
			|| (descriptor.PromptVisible && string.IsNullOrWhiteSpace(descriptor.MainInstruction))
			|| (descriptor.PromptVisible && string.IsNullOrWhiteSpace(descriptor.PostprocessRule))
			|| (descriptor.PromptVisible && (string.IsNullOrWhiteSpace(descriptor.EditableUnderstandingPrompt)
				|| descriptor.EditableUnderstandingPrompt.Length > 60000))
			|| (descriptor.PromptVisible && (string.IsNullOrWhiteSpace(descriptor.EditableEvaluationPrompt)
				|| descriptor.EditableEvaluationPrompt.Length > 60000))
			|| descriptor.PayloadPromptSchema == null
			|| descriptor.PayloadSchemaVersion <= 0
			|| module.PayloadType == null
			|| !typeof(PolicyEffectPayload).IsAssignableFrom(module.PayloadType))
		{
			throw new InvalidOperationException("政策效果模块定义不完整: " + (module?.GetType().FullName ?? "(null)"));
		}
		ValidateStringSet(descriptor.LegacyIds, "LegacyIds", id, allowEmpty: true, knownValues: null);
		ValidateStringSet(descriptor.CueTerms, "CueTerms", id, allowEmpty: !descriptor.PromptVisible, knownValues: null);
		ValidateStringSet(descriptor.AllowedScopes, "AllowedScopes", id, allowEmpty: false, knownScopes);
		ValidateEnumSet(descriptor.AllowedSelectorKinds, "AllowedSelectorKinds", id);
		ValidateEnumSet(descriptor.TargetKinds, "TargetKinds", id);
		if (!Enum.IsDefined(typeof(PolicyEffectTargetProjectionKind), descriptor.TargetProjection))
		{
			throw new InvalidOperationException("政策效果模块目标投影枚举无效: " + id);
		}
		if (!Enum.IsDefined(typeof(PolicyEffectTargetRefreshKind), descriptor.TargetRefresh))
		{
			throw new InvalidOperationException("政策效果模块目标刷新枚举无效: " + id);
		}
		if (!Enum.IsDefined(typeof(PolicyEffectTargetBindingKind), descriptor.TargetBinding))
		{
			throw new InvalidOperationException("政策效果模块目标绑定枚举无效: " + id);
		}
		if (descriptor.TargetBinding == PolicyEffectTargetBindingKind.IssuerKingdom
			&& (descriptor.AllowedScopes.Count != 1
				|| !descriptor.AllowedScopes.Contains(PolicyEffectScopes.Kingdom)
				|| descriptor.AllowedSelectorKinds.Count != 1
				|| !descriptor.AllowedSelectorKinds.Contains(PolicyEffectTargetKind.Kingdom)
				|| descriptor.TargetKinds.Count != 1
				|| !descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Kingdom)))
		{
			throw new InvalidOperationException(
				"IssuerKingdom 绑定要求 Kingdom-only scope、selector 和 target: " + id);
		}
		if (descriptor.AllowIndependentClanTargets
			&& !descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan))
		{
			throw new InvalidOperationException("独立家族目标能力要求 Clan target: " + id);
		}
		if (descriptor.ExcludeActorClanTargets
			&& !descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Clan))
		{
			throw new InvalidOperationException("排除发布者家族目标要求 Clan target: " + id);
		}
		if (descriptor.TargetProjection == PolicyEffectTargetProjectionKind.SettlementOwnerClanLeader
			&& (!descriptor.AllowedSelectorKinds.Contains(PolicyEffectTargetKind.Settlement)
				|| !descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Hero)))
		{
			throw new InvalidOperationException(
				"SettlementOwnerClanLeader 投影要求 Settlement selector 和 Hero target: " + id);
		}
		if (descriptor.TargetProjection == PolicyEffectTargetProjectionKind.PrimaryFiefAndBoundSettlements
			&& (!descriptor.AllowedSelectorKinds.Contains(PolicyEffectTargetKind.Settlement)
				|| descriptor.TargetKinds.Count != 1
				|| !descriptor.TargetKinds.Contains(PolicyEffectTargetKind.Settlement)))
		{
			throw new InvalidOperationException(
				"PrimaryFiefAndBoundSettlements 投影要求 Settlement selector 和唯一 Settlement target: " + id);
		}
		if (descriptor.AllowedSelectorKinds.Contains(PolicyEffectTargetKind.Village))
		{
			throw new InvalidOperationException("Village 不能作为一级政策效果 selector: " + id);
		}
		if (!Enum.IsDefined(typeof(PolicyEffectFamily), descriptor.Family)
			|| !Enum.IsDefined(typeof(PolicyEffectExecutionKind), descriptor.ExecutionKind)
			|| !Enum.IsDefined(typeof(PolicyEffectHook), descriptor.Hook)
			|| !Enum.IsDefined(typeof(PolicyEffectAggregationKind), descriptor.Aggregation)
			|| !Enum.IsDefined(typeof(PolicyEffectValueUnit), descriptor.ValueUnit)
			|| !Enum.IsDefined(typeof(PolicyEffectFundingMode), descriptor.FundingMode)
			|| !Enum.IsDefined(typeof(PolicyEffectFundingStrategy), descriptor.FundingStrategy))
		{
			throw new InvalidOperationException("政策效果模块枚举元数据无效: " + id);
		}
		ValidatePayloadSchema(descriptor, module.PayloadType);
		ValidateMigrationContract(module);
		ValidateHookCapability(descriptor);
		ValidateFundingContract(module);
		if (descriptor.SupportsRollback && !descriptor.SupportsIdempotency)
		{
			throw new InvalidOperationException("支持 rollback 的政策效果模块必须支持幂等: " + id);
		}
		if (descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot
			&& (!descriptor.SupportsRollback
				|| !descriptor.SupportsIdempotency
				|| descriptor.ValueUnit != PolicyEffectValueUnit.PointsOnce))
		{
			throw new InvalidOperationException("OneShot 政策效果模块必须声明 rollback、幂等和 PointsOnce: " + id);
		}
		if (descriptor.ExecutionKind == PolicyEffectExecutionKind.ScheduledOnce
			&& (!descriptor.SupportsRollback
				|| !descriptor.SupportsIdempotency
				|| (descriptor.ValueUnit != PolicyEffectValueUnit.PointsOnce
					&& descriptor.ValueUnit != PolicyEffectValueUnit.GoldOnce)))
		{
			throw new InvalidOperationException("ScheduledOnce effect module must declare rollback, idempotency, and an once value unit: " + id);
		}
		bool isModelModifier = module is IModelModifierPolicyEffectModule;
		bool isDailyMutation = module is IDailyPolicyEffectModule;
		bool isOneShot = module is IOneShotPolicyEffectModule;
		bool isComposite = module is IPolicyEffectCompositeModule;
		bool isScheduledOnce = module is IScheduledOncePolicyEffectModule;
		int executionInterfaceCount = (isModelModifier ? 1 : 0)
			+ (isDailyMutation ? 1 : 0)
			+ (isOneShot ? 1 : 0)
			+ (isComposite ? 1 : 0)
			+ (isScheduledOnce ? 1 : 0);
		if (executionInterfaceCount != 1
			|| isModelModifier != (descriptor.ExecutionKind == PolicyEffectExecutionKind.ModelModifier)
			|| isDailyMutation != (descriptor.ExecutionKind == PolicyEffectExecutionKind.DailyMutation)
			|| isOneShot != (descriptor.ExecutionKind == PolicyEffectExecutionKind.OneShot)
			|| isComposite != (descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite)
			|| isScheduledOnce != (descriptor.ExecutionKind == PolicyEffectExecutionKind.ScheduledOnce))
		{
			throw new InvalidOperationException("政策效果模块必须且只能实现与 executionKind 匹配的一个执行接口: " + id);
		}
	}

	private static void ValidateMigrationContract(IPolicyEffectModule module)
	{
		PolicyEffectModuleDescriptor descriptor = module.Descriptor;
		ValidateMigrationChain(
			descriptor.PayloadSchemaVersion,
			descriptor.MinimumReadablePayloadSchemaVersion,
			descriptor.PayloadMigrationSourceVersions,
			"payload",
			descriptor.Id);
		if (descriptor.MinimumReadablePayloadSchemaVersion < descriptor.PayloadSchemaVersion
			&& !HasModuleOverride(module, "TryMigrateTypedPayload", nameof(IPolicyEffectModule.TryMigratePayload)))
		{
			throw new InvalidOperationException("政策效果模块声明了 payload 迁移链但未实现迁移: " + descriptor.Id);
		}

		ValidateMigrationChain(
			descriptor.RuntimeStateSchemaVersion,
			descriptor.MinimumReadableRuntimeStateSchemaVersion,
			descriptor.RuntimeStateMigrationSourceVersions,
			"runtime state",
			descriptor.Id);
		if (descriptor.MinimumReadableRuntimeStateSchemaVersion < descriptor.RuntimeStateSchemaVersion
			&& !HasModuleOverride(module, null, nameof(IPolicyEffectModule.TryMigrateRuntimeState)))
		{
			throw new InvalidOperationException("政策效果模块声明了 runtime state 迁移链但未实现迁移: " + descriptor.Id);
		}
	}

	private static void ValidateMigrationChain(
		int currentVersion,
		int minimumReadableVersion,
		IReadOnlyCollection<int> sourceVersions,
		string stateKind,
		string moduleId)
	{
		if (currentVersion <= 0
			|| minimumReadableVersion <= 0
			|| minimumReadableVersion > currentVersion
			|| sourceVersions == null)
		{
			throw new InvalidOperationException("政策效果模块 " + stateKind + " 版本范围无效: " + moduleId);
		}
		int[] expected = Enumerable.Range(minimumReadableVersion, currentVersion - minimumReadableVersion).ToArray();
		if (!sourceVersions.SequenceEqual(expected))
		{
			throw new InvalidOperationException("政策效果模块 " + stateKind + " 迁移源版本必须连续: " + moduleId);
		}
	}

	private static bool TryValidateMigrationSourceVersion(
		int currentVersion,
		int minimumReadableVersion,
		IReadOnlyCollection<int> sourceVersions,
		int sourceVersion,
		string stateKind,
		string moduleId,
		out string error)
	{
		if (sourceVersion <= 0
			|| sourceVersion < minimumReadableVersion
			|| sourceVersion > currentVersion
			|| (sourceVersion != currentVersion && sourceVersions?.Contains(sourceVersion) != true))
		{
			error = "政策效果模块 " + stateKind + " 版本不受支持: " + moduleId + " / " + sourceVersion;
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static void ValidateHookCapability(PolicyEffectModuleDescriptor descriptor)
	{
		PolicyEffectExecutionKind executionKind;
		PolicyEffectTargetKind? targetKind;
		PolicyEffectAggregationKind aggregation;
		PolicyEffectValueUnit valueUnit;
		switch (descriptor.Hook)
		{
			case PolicyEffectHook.SettlementProsperityDaily:
			case PolicyEffectHook.TownFoodDaily:
			case PolicyEffectHook.TownLoyaltyDaily:
			case PolicyEffectHook.TownSecurityDaily:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Town;
				aggregation = PolicyEffectAggregationKind.Additive;
				valueUnit = PolicyEffectValueUnit.PointsPerDay;
				break;
			case PolicyEffectHook.VillageHearthDaily:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Village;
				aggregation = PolicyEffectAggregationKind.Additive;
				valueUnit = PolicyEffectValueUnit.PointsPerDay;
				break;
			case PolicyEffectHook.SettlementMilitiaDaily:
			case PolicyEffectHook.SettlementConstructionDaily:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Settlement;
				aggregation = PolicyEffectAggregationKind.Additive;
				valueUnit = PolicyEffectValueUnit.PointsPerDay;
				break;
			case PolicyEffectHook.TownTaxIncome:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Town;
				aggregation = PolicyEffectAggregationKind.PercentPoints;
				valueUnit = PolicyEffectValueUnit.PercentPoints;
				break;
			case PolicyEffectHook.ArmyFormationScore:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Clan;
				aggregation = PolicyEffectAggregationKind.Additive;
				valueUnit = PolicyEffectValueUnit.RelativePercent;
				break;
			case PolicyEffectHook.VolunteerProductionProbability:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Settlement;
				aggregation = PolicyEffectAggregationKind.Additive;
				valueUnit = PolicyEffectValueUnit.RelativePercent;
				break;
			case PolicyEffectHook.KingdomVillageRaidBlock:
				executionKind = PolicyEffectExecutionKind.ModelModifier;
				targetKind = PolicyEffectTargetKind.Kingdom;
				aggregation = PolicyEffectAggregationKind.AnyBlock;
				valueUnit = PolicyEffectValueUnit.BooleanFlag;
				break;
			case PolicyEffectHook.KingdomStabilityOnActivation:
				executionKind = PolicyEffectExecutionKind.OneShot;
				targetKind = PolicyEffectTargetKind.Kingdom;
				aggregation = PolicyEffectAggregationKind.IntegerDelta;
				valueUnit = PolicyEffectValueUnit.PointsOnce;
				break;
			case PolicyEffectHook.DailyScheduler:
				targetKind = null;
				aggregation = descriptor.ValueUnit == PolicyEffectValueUnit.GoldOnce
					|| descriptor.ValueUnit == PolicyEffectValueUnit.GoldPerDay
					? PolicyEffectAggregationKind.IntegerDelta
					: PolicyEffectAggregationKind.Additive;
				if (descriptor.ExecutionKind == PolicyEffectExecutionKind.DailyMutation)
				{
					executionKind = PolicyEffectExecutionKind.DailyMutation;
					valueUnit = descriptor.ValueUnit == PolicyEffectValueUnit.GoldPerDay
						? PolicyEffectValueUnit.GoldPerDay
						: PolicyEffectValueUnit.PointsPerDay;
					break;
				}
				if (descriptor.ExecutionKind == PolicyEffectExecutionKind.ScheduledOnce)
				{
					executionKind = PolicyEffectExecutionKind.ScheduledOnce;
					valueUnit = descriptor.ValueUnit == PolicyEffectValueUnit.GoldOnce
						? PolicyEffectValueUnit.GoldOnce
						: PolicyEffectValueUnit.PointsOnce;
					break;
				}
				if (descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite
					&& (descriptor.ValueUnit == PolicyEffectValueUnit.PointsPerDay
						|| descriptor.ValueUnit == PolicyEffectValueUnit.PointsOnce
						|| descriptor.ValueUnit == PolicyEffectValueUnit.GoldPerDay
						|| descriptor.ValueUnit == PolicyEffectValueUnit.GoldOnce))
				{
					executionKind = PolicyEffectExecutionKind.Composite;
					valueUnit = descriptor.ValueUnit;
					break;
				}
				throw new InvalidOperationException("DailyScheduler execution kind is unsupported: " + descriptor.Id);
			default:
				throw new InvalidOperationException("政策效果模块 hook 未定义 capability: " + descriptor.Id);
		}
		bool targetMatches = targetKind.HasValue
			? descriptor.TargetKinds.Count == 1 && descriptor.TargetKinds.Contains(targetKind.Value)
			: descriptor.TargetKinds.Count > 0;
		if (descriptor.ExecutionKind != executionKind
			|| !targetMatches
			|| descriptor.Aggregation != aggregation
			|| descriptor.ValueUnit != valueUnit)
		{
			throw new InvalidOperationException("政策效果模块 hook capability 不匹配: " + descriptor.Id);
		}
	}

	private static void ValidateFundingContract(IPolicyEffectModule module)
	{
		PolicyEffectModuleDescriptor descriptor = module.Descriptor;
		bool numericPayload = typeof(NumericPolicyEffectPayload).IsAssignableFrom(module.PayloadType);
		bool requiresOverride = descriptor.FundingStrategy == PolicyEffectFundingStrategy.Custom
			|| (!numericPayload && descriptor.FundingStrategy != PolicyEffectFundingStrategy.None);
		if (requiresOverride && !HasModuleOverride(module, "TryApplyTypedFunding", nameof(IPolicyEffectModule.TryApplyFunding)))
		{
			throw new InvalidOperationException("政策效果模块 funding 策略要求显式转换实现: " + descriptor.Id);
		}
	}

	private static bool HasModuleOverride(IPolicyEffectModule module, string typedMethodName, string untypedMethodName)
	{
		BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		if (!string.IsNullOrWhiteSpace(typedMethodName))
		{
			MethodInfo typedMethod = module.GetType().GetMethods(flags)
				.FirstOrDefault(method => string.Equals(method.Name, typedMethodName, StringComparison.Ordinal));
			if (typedMethod != null && !IsFrameworkModuleBaseType(typedMethod.DeclaringType))
			{
				return true;
			}
		}
		MethodInfo untypedMethod = module.GetType().GetMethods(flags)
			.FirstOrDefault(method => string.Equals(method.Name, untypedMethodName, StringComparison.Ordinal));
		return untypedMethod != null && !IsFrameworkModuleBaseType(untypedMethod.DeclaringType);
	}

	private static bool IsFrameworkModuleBaseType(Type type)
	{
		if (type == null || type == typeof(PolicyEffectModuleBase))
		{
			return true;
		}
		if (!type.IsGenericType)
		{
			return false;
		}
		Type definition = type.GetGenericTypeDefinition();
		return definition == typeof(PolicyEffectModuleBase<>)
			|| definition == typeof(NumericPolicyEffectModuleBase<>);
	}

	private static void ValidatePayloadSchema(PolicyEffectModuleDescriptor descriptor, Type payloadType)
	{
		JObject schema = descriptor.PayloadPromptSchema;
		if (!string.Equals((string)schema["type"], "object", StringComparison.Ordinal)
			|| !(schema["properties"] is JObject properties)
			|| !(schema["required"] is JArray required))
		{
			throw new InvalidOperationException("政策效果模块 payload prompt schema 无效: " + descriptor.Id);
		}
		if (typeof(NumericPolicyEffectPayload).IsAssignableFrom(payloadType)
			&& (properties["value"] == null || !required.Values<string>().Contains("value", StringComparer.Ordinal)))
		{
			throw new InvalidOperationException("数值政策效果模块 schema 必须要求 value: " + descriptor.Id);
		}
	}

	private static void ValidateStringSet(
		IReadOnlyCollection<string> values,
		string fieldName,
		string moduleId,
		bool allowEmpty,
		ISet<string> knownValues)
	{
		if (values == null || (!allowEmpty && values.Count <= 0))
		{
			throw new InvalidOperationException("政策效果模块 " + fieldName + " 不能为空: " + moduleId);
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string value in values ?? Array.Empty<string>())
		{
			string normalized = (value ?? string.Empty).Trim();
			if (normalized.Length <= 0
				|| !string.Equals(normalized, value, StringComparison.Ordinal)
				|| !seen.Add(normalized)
				|| (knownValues != null && !knownValues.Contains(normalized)))
			{
				throw new InvalidOperationException("政策效果模块 " + fieldName + " 无效或重复: " + moduleId + " / " + normalized);
			}
		}
	}

	private static void ValidateEnumSet<TEnum>(IReadOnlyCollection<TEnum> values, string fieldName, string moduleId)
		where TEnum : struct
	{
		if (values == null || values.Count <= 0)
		{
			throw new InvalidOperationException("政策效果模块 " + fieldName + " 不能为空: " + moduleId);
		}
		HashSet<TEnum> seen = new HashSet<TEnum>();
		foreach (TEnum value in values)
		{
			if (!Enum.IsDefined(typeof(TEnum), value) || !seen.Add(value))
			{
				throw new InvalidOperationException("政策效果模块 " + fieldName + " 无效或重复: " + moduleId + " / " + value);
			}
		}
	}
}
