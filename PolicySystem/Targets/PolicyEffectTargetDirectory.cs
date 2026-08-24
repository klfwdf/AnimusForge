using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyEffects;
using Newtonsoft.Json;

namespace AnimusForge.PolicyTargets;

internal static class PolicyTargetHandleDirectoryContract
{
	internal const int CurrentVersion = 2;
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
	[JsonProperty("allowedTargetHandles", Order = 1)]
	public List<string> AllowedTargetHandles { get; set; } = new List<string>();
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

internal static class PolicyTargetHandleDirectoryBuilder
{
	internal static PolicyTargetHandleDirectory Build(
		IReadOnlyList<PolicyTargetHandleDirectoryCandidate> candidates,
		IReadOnlyList<IPolicyEffectModule> injectedModules,
		PolicyEffectTargetResolver targetResolver,
		string issuerKingdomId = "")
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
					|| !PolicyEffectCompiler.IsResolvedTargetAuthorizedForModule(module, resolved, issuerKingdomId))
				{
					continue;
				}
				capability.AllowedTargetHandles.Add(handle);
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
			? "disposition 必须是 executable，effects 必须至少包含一项。"
			: "disposition 只能是 executable、narrativeOnly 或 unsupported；后两者要求 effects=[]。";
		string targetCountRule = requireSingleTargetPerEffect
			? "每条 effect 的 targetHandles 必须恰好包含一个句柄；同一模块作用于不同合法句柄时分别输出 effect。"
			: "targetHandles 必须包含至少一个互不重复的合法句柄。";
		return "根对象必须且只能包含 effectPlanVersion、disposition、reason、effects。effectPlanVersion 必须为 1。"
			+ dispositionRule
			+ "executable 的每个 effect 必须且只能包含 mechanismId、mechanismKind、role、moduleId、targetHandles、payload、reason。"
			+ "mechanismKind 只能是 independent 或 linked；independent 必须使用 role=subject。"
			+ "linked 必须在同一 mechanismId 下包含至少两条真实可执行资源流转腿，并同时包含 source 与 destination/beneficiary；不得输出 cost。"
			+ "moduleId 只能取执行目录 capabilities 映射的属性名；targetHandles 只能精确复制该 moduleId 的 allowedTargetHandles，并且必须同时存在于 targets 映射中；不得拼接 kind、description 或任何附加字符；payload 必须符合该 ID 的详细契约。"
			+ targetCountRule
			+ "每个句柄代表完整 canonical target set；载荷数值按每个 canonical target 和能力自身执行频率独立解释，不按目标数或期限乘除。"
			+ "输出形状：{\"effectPlanVersion\":1,\"disposition\":\"executable\",\"reason\":\"...\",\"effects\":[{\"mechanismId\":\"M0\",\"mechanismKind\":\"independent\",\"role\":\"subject\",\"moduleId\":\"...\",\"targetHandles\":[\"...\"],\"payload\":{},\"reason\":\"...\"}]}";
	}
}
