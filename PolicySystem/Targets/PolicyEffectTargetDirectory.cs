using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyEffects;
using Newtonsoft.Json;

namespace AnimusForge.PolicyTargets;

internal static class PolicyTargetHandleDirectoryContract
{
	internal const int CurrentVersion = 3;
}

internal sealed class PolicyTargetHandleDirectory
{
	[JsonProperty("structureVersion", Order = 1)]
	public int StructureVersion { get; set; } = PolicyTargetHandleDirectoryContract.CurrentVersion;

	[JsonProperty("targets", Order = 2)]
	public Dictionary<string, PolicyTargetHandleDirectoryEntry> Targets { get; set; } =
		new Dictionary<string, PolicyTargetHandleDirectoryEntry>(StringComparer.Ordinal);

	[JsonProperty("capabilities", Order = 3)]
	public Dictionary<string, PolicyEffectCapabilityDirectoryEntry> Capabilities { get; set; } =
		new Dictionary<string, PolicyEffectCapabilityDirectoryEntry>(StringComparer.Ordinal);
}

internal sealed class PolicyEffectCapabilityDirectoryEntry
{
	[JsonProperty("defaultTargetHandle", Order = 1)]
	public string DefaultTargetHandle { get; set; } = string.Empty;

	[JsonProperty("allowedSubsetTargetHandles", Order = 2)]
	public List<string> AllowedSubsetTargetHandles { get; set; } = new List<string>();

	[JsonProperty("allowedTargetHandles", Order = 3)]
	public List<string> AllowedTargetHandles { get; set; } = new List<string>();

	[JsonIgnore]
	public Dictionary<string, PolicyEffectTargetJurisdictionKind> JurisdictionByTargetHandle { get; set; } =
		new Dictionary<string, PolicyEffectTargetJurisdictionKind>(StringComparer.Ordinal);
}

internal sealed class PolicyTargetHandleDirectoryEntry
{
	[JsonProperty("kind", Order = 1)]
	public string Kind { get; set; }

	[JsonProperty("description", Order = 2)]
	public string Description { get; set; }

	[JsonProperty("selectorId", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
	public string SelectorId { get; set; }

	[JsonProperty("entityId", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
	public string EntityId { get; set; }

	[JsonProperty("targetPlanVersion", Order = 5, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public int TargetPlanVersion { get; set; }

	[JsonProperty("currentSettlementCount", Order = 6, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public int CurrentSettlementCount { get; set; }

	[JsonProperty("followCurrentRulingClan", Order = 7, DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool FollowCurrentRulingClan { get; set; }
}

internal sealed class PolicyTargetHandleDirectoryCandidate
{
	internal string Handle { get; set; } = string.Empty;

	internal PolicyTargetHandleDirectoryEntry Entry { get; set; }
}

internal delegate bool PolicyEffectTargetDefaultHandleClassifier(
	string handle,
	PolicyTargetHandleDirectoryEntry entry,
	IPolicyEffectModule module,
	PolicyEffectResolvedTarget resolved);

internal static class PolicyTargetHandleDirectoryBuilder
{
	internal static PolicyTargetHandleDirectory Build(
		IReadOnlyList<PolicyTargetHandleDirectoryCandidate> candidates,
		IReadOnlyList<IPolicyEffectModule> injectedModules,
		PolicyEffectTargetResolver targetResolver,
		string issuerKingdomId = "",
		string actorClanId = "",
		PolicyEffectTargetDefaultHandleClassifier defaultHandleClassifier = null)
	{
		PolicyTargetHandleDirectory directory = new PolicyTargetHandleDirectory();
		if (targetResolver == null)
		{
			return directory;
		}
		Dictionary<string, PolicyTargetHandleDirectoryEntry> entryByHandle =
			new Dictionary<string, PolicyTargetHandleDirectoryEntry>(StringComparer.Ordinal);
		foreach (PolicyTargetHandleDirectoryCandidate candidate in candidates
			?? Array.Empty<PolicyTargetHandleDirectoryCandidate>())
		{
			string handle = (candidate?.Handle ?? string.Empty).Trim();
			if (handle.Length == 0 || candidate.Entry == null || entryByHandle.ContainsKey(handle))
			{
				continue;
			}
			entryByHandle.Add(handle, candidate.Entry);
		}

		HashSet<string> referencedHandles = new HashSet<string>(StringComparer.Ordinal);
		foreach (IPolicyEffectModule module in (injectedModules ?? Array.Empty<IPolicyEffectModule>())
			.Where(module => module?.Descriptor != null && !string.IsNullOrWhiteSpace(module.Id))
			.GroupBy(module => module.Id, StringComparer.Ordinal)
			.Select(group => group.First())
			.OrderBy(module => module.Order)
			.ThenBy(module => module.Id, StringComparer.Ordinal))
		{
			PolicyEffectCapabilityDirectoryEntry capability = new PolicyEffectCapabilityDirectoryEntry();
			foreach (string handle in entryByHandle.Keys)
			{
				if (!targetResolver(handle, module, out PolicyEffectResolvedTarget resolved, out _)
					|| !PolicyEffectCompiler.IsResolvedTargetAuthorizedForModule(
						module,
						PolicyEffectCompiler.ApplyActorClanTargetExclusion(module, actorClanId, resolved),
						issuerKingdomId))
				{
					continue;
				}
				capability.AllowedTargetHandles.Add(handle);
				capability.JurisdictionByTargetHandle[handle] =
					resolved?.CanonicalTargetSet?.JurisdictionKind
					?? PolicyEffectTargetJurisdictionKind.LegacyCompiled;
				if (defaultHandleClassifier != null
					&& capability.DefaultTargetHandle.Length == 0
					&& defaultHandleClassifier(handle, entryByHandle[handle], module, resolved))
				{
					capability.DefaultTargetHandle = handle;
				}
				else
				{
					capability.AllowedSubsetTargetHandles.Add(handle);
				}
				referencedHandles.Add(handle);
			}
			if (capability.AllowedTargetHandles.Count > 0)
			{
				directory.Capabilities.Add(module.Id, capability);
			}
		}
		foreach (KeyValuePair<string, PolicyTargetHandleDirectoryEntry> pair in entryByHandle)
		{
			if (referencedHandles.Contains(pair.Key))
			{
				directory.Targets.Add(pair.Key, pair.Value);
			}
		}
		return directory;
	}
}

internal static class PolicyEffectDirectPlanContract
{
	internal static string SerializeDirectory(PolicyTargetHandleDirectory directory)
	{
		return JsonConvert.SerializeObject(directory ?? new PolicyTargetHandleDirectory(), Formatting.None);
	}

	internal static string BuildOutputContract(bool requireExecutable, bool requireSingleTargetPerEffect)
	{
		string dispositionRule = requireExecutable
			? PolicyGenerationPromptTexts.Get("ContractExecutable", "disposition must be executable, and effects must contain at least one entry.")
			: PolicyGenerationPromptTexts.Get("ContractDisposition", "disposition may only be executable, narrativeOnly, or unsupported; the latter two require effects=[].");
		string targetCountRule = requireSingleTargetPerEffect
			? PolicyGenerationPromptTexts.Get("ContractSingleTarget", "Each effect's targetHandles must contain exactly one handle; output separate effects when the same module acts on different legal handles.")
			: PolicyGenerationPromptTexts.Get("ContractTargetCount", "targetHandles must contain at least one distinct legal handle.");
		return PolicyGenerationPromptTexts.Get("ContractRoot", "The root object must contain exactly effectPlanVersion, disposition, reason, and effects. effectPlanVersion must be 1.")
			+ dispositionRule
			+ PolicyGenerationPromptTexts.Get("ContractEffectFields", "Each executable effect must contain exactly mechanismId, mechanismKind, role, moduleId, targetHandles, payload, and reason.")
			+ PolicyGenerationPromptTexts.Get("ContractMechanismKind", "mechanismKind may only be independent or linked; independent must use role=subject.")
			+ PolicyGenerationPromptTexts.Get("ContractLinked", "linked must contain at least two genuine executable resource-transfer legs under the same mechanismId, including both source and destination/beneficiary; do not output cost.")
			+ PolicyGenerationPromptTexts.Get("ContractModuleAuthorization", "moduleId may only be a property name in the execution directory's capabilities map. targetHandles must exactly copy entries from that moduleId's allowedTargetHandles and must also exist in the targets map; do not concatenate kind, description, or any additional characters.")
			+ PolicyGenerationPromptTexts.Get("ContractDefaultTarget", "Each capability's defaultTargetHandle defines the module's default settlement scope for the current policy type. If nonempty, select it unless the original text explicitly requires a smaller scope or names specific characters, clans, fiefs, or foreign objects.")
			+ PolicyGenerationPromptTexts.Get("ContractSubsetTarget", "allowedSubsetTargetHandles replace the default only when the original text explicitly requires subdivision or direct settlement on a named object. A module with an empty defaultTargetHandle may be output only when the original text explicitly names a subset object; otherwise omit it.")
			+ PolicyGenerationPromptTexts.Get("ContractForeignTarget", "Do not treat foreign candidates appearing only in references, comparisons, reports, sources of experience, or background material as execution targets. Do not expand local policies beyond their publication location by default. Do not automatically treat K1 as an affected party in vassal policies. Do not output heroGold without an explicit character recipient.")
			+ PolicyGenerationPromptTexts.Get("ContractPayload", "payload must satisfy the detailed contract for that ID.")
			+ targetCountRule
			+ PolicyGenerationPromptTexts.Get("ContractPerTarget", "Each handle represents a complete canonical target set. Interpret payload values independently per canonical target and according to the capability's execution frequency; do not multiply or divide by target count or duration.")
			+ PolicyGenerationPromptTexts.Get("ContractShape", "Output shape: {\"effectPlanVersion\":1,\"disposition\":\"executable\",\"reason\":\"...\",\"effects\":[{\"mechanismId\":\"M0\",\"mechanismKind\":\"independent\",\"role\":\"subject\",\"moduleId\":\"...\",\"targetHandles\":[\"...\"],\"payload\":{},\"reason\":\"...\"}]}");
	}
}
