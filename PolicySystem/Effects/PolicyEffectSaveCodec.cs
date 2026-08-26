using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyTargets;

namespace AnimusForge.PolicyEffects;

internal sealed class PolicyEffectMigrationBatchSummary
{
	internal int RecordsVisited { get; set; }

	internal int RecordsChanged { get; set; }

	internal int InstancesCreated { get; set; }

	internal int ExecutableInstances { get; set; }

	internal int InertInstances { get; set; }

	internal int LegacyFieldsMigrated { get; set; }

	internal int LegacyAssumedCompletedReceipts { get; set; }

	internal List<string> Warnings { get; } = new List<string>();

	internal void Merge(PolicyEffectMigrationBatchSummary other)
	{
		if (other == null)
		{
			return;
		}
		RecordsVisited += other.RecordsVisited;
		RecordsChanged += other.RecordsChanged;
		InstancesCreated += other.InstancesCreated;
		ExecutableInstances += other.ExecutableInstances;
		InertInstances += other.InertInstances;
		LegacyFieldsMigrated += other.LegacyFieldsMigrated;
		LegacyAssumedCompletedReceipts += other.LegacyAssumedCompletedReceipts;
		Warnings.AddRange(other.Warnings);
	}

	public override string ToString()
	{
		return "records=" + RecordsVisited
			+ ", changed=" + RecordsChanged
			+ ", instances=" + InstancesCreated
			+ ", executable=" + ExecutableInstances
			+ ", inert=" + InertInstances
			+ ", legacyFields=" + LegacyFieldsMigrated
			+ ", legacyAssumedCompleted=" + LegacyAssumedCompletedReceipts
			+ ", warnings=" + Warnings.Count;
	}
}

internal sealed class PolicyEffectNormalizedInstance
{
	internal PolicyEffectInstanceSaveData SaveData { get; set; }

	internal PolicyEffectInstance RuntimeInstance { get; set; }

	internal IPolicyEffectModule Module { get; set; }

	internal bool IsInert { get; set; }

	internal string InertReason { get; set; } = string.Empty;
}

internal sealed class PolicyEffectSaveDecodeResult
{
	internal PolicyEffectSaveEnvelope Envelope { get; set; }

	internal List<PolicyEffectNormalizedInstance> Instances { get; } = new List<PolicyEffectNormalizedInstance>();

	internal PolicyEffectMigrationBatchSummary Summary { get; } = new PolicyEffectMigrationBatchSummary();
}

internal static class PolicyEffectSaveCodec
{
	internal const int MaxPayloadBytes = 4 * 1024;
	internal const int MaxTotalPayloadBytes = 32 * 1024;
	internal const int MaxRuntimeStateBytes = 512 * 1024;
	internal const int MaxReceiptPayloadBytes = 256 * 1024;
	internal const int MaxSerializedEnvelopeBytes = 2 * 1024 * 1024;
	internal const int MaxWireEffectsPerPolicy = 12;

	internal const int MaxInstancesPerPolicy = 24;
	private const int MaxJsonDepth = 64;
	private static readonly string[] LegacyStoppedEffectShapeProperties =
	{
		"prosperityDailyDeltaPerTown",
		"foodDailyDeltaPerTown",
		"hearthDailyDeltaPerVillage",
		"loyaltyDailyDeltaPerTown",
		"securityDailyDeltaPerTown",
		"militiaDailyDeltaPerTown",
		"townTaxPercent",
		"constructionPowerDailyDelta",
		"constructionSpeedPercent",
		"kingdomStabilityDailyDelta",
		"volunteerProductionPercent",
		"volunteerUpgradeRatePercent",
		"clanInfluenceDailyDelta"
	};

	private static readonly JsonSerializerSettings SafeSettings = new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.None,
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
		DateParseHandling = DateParseHandling.None,
		FloatParseHandling = FloatParseHandling.Double,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		NullValueHandling = NullValueHandling.Include,
		MaxDepth = MaxJsonDepth
	};

	internal static bool IsLegacyStoppedActiveV5Shape(JObject source)
	{
		return source != null
			&& ReadInt(source, "Version", 0) == 5
			&& source.GetValue("ModuleEffects", StringComparison.OrdinalIgnoreCase) == null
			&& HasLegacyStoppedEffectShape(source);
	}

	internal static bool IsLegacyStoppedNpcPolicyShape(JObject source)
	{
		int version = ReadInt(source, "Version", 1);
		return source != null
			&& version >= 1
			&& version < 6
			&& !HasModuleEffectShape(source)
			&& HasLegacyStoppedEffectShape(source);
	}

	internal static bool TryNormalizeLegacyStoppedDynamicPolicy(
		JObject source,
		out JObject normalized,
		out string error)
	{
		normalized = null;
		error = string.Empty;
		if (source == null || ReadInt(source, "Version", 0) != 1)
		{
			return false;
		}
		if (ContainsTypeMetadata(source))
		{
			error = "旧 DynamicPolicy 存档不得包含 $type 元数据";
			return false;
		}
		normalized = (JObject)source.DeepClone();
		normalized["Version"] = 4;
		normalized["Status"] = "abolished";
		normalized["CommitState"] = "ended";
		normalized["ActiveEffectId"] = string.Empty;
		normalized["RequiresEffectBundle"] = false;
		normalized["NaturalExpiryAgendaRejected"] = false;
		normalized["PlayerPayloadJson"] = string.Empty;
		return true;
	}

	internal static bool TryNormalizeLegacyStoppedLocalPolicy(
		JObject source,
		out JObject normalized,
		out string error)
	{
		normalized = null;
		error = string.Empty;
		if (source == null
			|| ReadInt(source, "Version", 0) != 4
			|| HasModuleEffectShape(source)
			|| !HasLegacyStoppedEffectShape(source))
		{
			return false;
		}
		if (ContainsTypeMetadata(source))
		{
			error = "旧 LocalPolicy 存档不得包含 $type 元数据";
			return false;
		}
		normalized = (JObject)source.DeepClone();
		normalized["Version"] = 6;
		normalized["Status"] = "abolished";
		normalized["EffectStatus"] = "abolished";
		normalized["EndReason"] = "旧政策系统升级后停止，可重新评议";
		normalized["ActiveEffectId"] = string.Empty;
		normalized["RemainingDays"] = 0;
		normalized["Effects"] = new JArray();
		normalized["DailyMaintenanceGoldCost"] = 0;
		normalized["TotalMaintenancePaidGold"] = 0;
		normalized["MaintenanceFunded"] = true;
		normalized["LastMaintenanceSettlementDay"] = -1;
		normalized["LastEffectProcessedDay"] = -1;
		normalized["ExternalLastAttemptDay"] = -1;
		normalized["ExternalInputsCaptured"] = false;
		normalized["ExternalPublicationCost"] = 0;
		normalized["ExternalQualityDelta"] = 0;
		return true;
	}

	internal static bool TrySerialize(PolicyEffectSaveEnvelope envelope, out string json, out string error)
	{
		json = string.Empty;
		if (!TryUpgradeEnvelopeSchema(envelope, out error))
		{
			return false;
		}
		if (!TryValidateEnvelope(envelope, out error))
		{
			return false;
		}
		try
		{
			json = JsonConvert.SerializeObject(envelope, Formatting.None, SafeSettings);
			if (Utf8Size(json) > MaxSerializedEnvelopeBytes)
			{
				json = string.Empty;
				error = "policy effect save envelope 超过 2MiB";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "policy effect save 序列化失败: " + ex.Message;
			return false;
		}
	}

	internal static bool TryDeserialize(string json, out PolicyEffectSaveDecodeResult result, out string error)
	{
		result = null;
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(json))
		{
			error = "policy effect save 不能为空";
			return false;
		}
		if (Utf8Size(json) > MaxSerializedEnvelopeBytes)
		{
			error = "policy effect save envelope 超过 2MiB";
			return false;
		}
		try
		{
			JToken rawEnvelope = JToken.Parse(json);
			if (ContainsTypeMetadata(rawEnvelope))
			{
				error = "policy effect save envelope must not contain $type metadata";
				return false;
			}
		}
		catch (Exception ex)
		{
			error = "policy effect save JSON is invalid: " + ex.Message;
			return false;
		}
		PolicyEffectSaveEnvelope envelope;
		try
		{
			envelope = JsonConvert.DeserializeObject<PolicyEffectSaveEnvelope>(json, SafeSettings);
		}
		catch (Exception ex)
		{
			error = "policy effect save 反序列化失败: " + ex.Message;
			return false;
		}
		if (!TryUpgradeEnvelopeSchema(envelope, out error))
		{
			return false;
		}
		if (!TryValidateEnvelope(envelope, out error))
		{
			return false;
		}
		PolicyEffectSaveDecodeResult decoded = new PolicyEffectSaveDecodeResult { Envelope = envelope };
		foreach (PolicyEffectInstanceSaveData instance in envelope.Instances)
		{
			if (!TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out error))
			{
				return false;
			}
			decoded.Instances.Add(normalized);
			if (normalized.IsInert)
			{
				decoded.Summary.InertInstances++;
				decoded.Summary.Warnings.Add(normalized.SaveData?.InstanceId + ": " + normalized.InertReason);
			}
			else
			{
				decoded.Summary.ExecutableInstances++;
			}
		}
		decoded.Summary.RecordsVisited = 1;
		result = decoded;
		return true;
	}

	internal static bool TryNormalizeInstance(
		PolicyEffectInstanceSaveData source,
		out PolicyEffectNormalizedInstance result,
		out string error)
	{
		result = null;
		error = string.Empty;
		if (!TryValidateInstanceShape(source, out error))
		{
			return false;
		}
		PolicyEffectNormalizedInstance normalized = new PolicyEffectNormalizedInstance { SaveData = source };
		NormalizeLegacyEffectPlan(source);
		if (!PolicyEffectModuleCatalog.TryGet(source.ModuleId, out IPolicyEffectModule module))
		{
			normalized.IsInert = true;
			normalized.InertReason = "unknownModule";
			result = normalized;
			return true;
		}
		string declaredSourceModuleId = (source.SourceModuleId ?? string.Empty).Trim();
		if (declaredSourceModuleId.Length == 0)
		{
			declaredSourceModuleId = module.Id;
		}
		if (!PolicyEffectModuleCatalog.TryResolveCanonicalId(declaredSourceModuleId, out string canonicalSourceModuleId)
			|| !PolicyEffectModuleCatalog.IsAuthorizedRuntimeModule(canonicalSourceModuleId, module.Id))
		{
			normalized.Module = module;
			normalized.IsInert = true;
			normalized.InertReason = "invalidSourceModuleLineage";
			result = normalized;
			return true;
		}
		source.SourceModuleId = canonicalSourceModuleId;
		if (module.Descriptor.ExecutionKind == PolicyEffectExecutionKind.Composite)
		{
			normalized.Module = module;
			normalized.IsInert = true;
			normalized.InertReason = "compileTimeCompositeCannotRun";
			result = normalized;
			return true;
		}
		if (!TryValidateEffectPlanInstance(source, module, out string effectPlanError))
		{
			normalized.Module = module;
			normalized.IsInert = true;
			normalized.InertReason = "invalidEffectPlan: " + effectPlanError;
			result = normalized;
			return true;
		}
		if (!TryNormalizeSavedTargetPlans(source.TargetSet, out string targetPlanError))
		{
			normalized.Module = module;
			normalized.IsInert = true;
			normalized.InertReason = "invalidTargetPlan: " + targetPlanError;
			result = normalized;
			return true;
		}
		if (!TryValidateKnownModuleScope(source, module, out error))
		{
			return false;
		}
		if (!PolicyEffectModuleCatalog.TryDeserializePayload(
			module.Id,
			source.Payload,
			source.PayloadSchemaVersion,
			out PolicyEffectPayload payload,
			out string payloadError))
		{
			normalized.Module = module;
			normalized.IsInert = true;
			normalized.InertReason = "invalidPayload: " + payloadError;
			result = normalized;
			return true;
		}
		if (!TryMigrateKnownModuleRuntimeState(source, module, out string runtimeStateError))
		{
			normalized.Module = module;
			normalized.IsInert = true;
			normalized.InertReason = "invalidRuntimeState: " + runtimeStateError;
			result = normalized;
			return true;
		}
		source.ModuleId = module.Id;
		source.PayloadSchemaVersion = module.Descriptor.PayloadSchemaVersion;
		source.Payload = SerializeCanonicalPayload(module, payload);
		normalized.Module = module;
		normalized.RuntimeInstance = new PolicyEffectInstance
		{
			MechanismContractVersion = source.MechanismContractVersion,
			MechanismContractHash = source.MechanismContractHash ?? string.Empty,
			ExpectedMechanismLegIds = new List<string>(source.ExpectedMechanismLegIds ?? new List<string>()),
			EffectPlanVersion = source.EffectPlanVersion,
			MechanismId = source.MechanismId,
			MechanismKind = source.MechanismKind,
			MechanismRole = source.MechanismRole,
			SourceOmitted = source.SourceOmitted,
			DestinationOmitted = source.DestinationOmitted,
			InstanceId = source.InstanceId,
			PolicyId = source.PolicyId,
			ActorHeroId = source.ActorHeroId,
			ModuleId = module.Id,
			SourceModuleId = source.SourceModuleId,
			TargetSet = source.TargetSet,
			Payload = payload,
			LifecycleState = source.LifecycleState,
			StartDay = source.StartDay,
			EndDay = source.EndDay,
			SourceScope = source.SourceScope,
			Reason = source.Reason
		};
		result = normalized;
		return true;
	}

	private static JToken SerializeCanonicalPayload(IPolicyEffectModule module, PolicyEffectPayload payload)
	{
		JToken serialized = JToken.FromObject(payload, JsonSerializer.Create(SafeSettings));
		if (!(serialized is JObject payloadObject)
			|| !(module?.Descriptor?.PayloadPromptSchema?["properties"] is JObject schemaProperties))
		{
			return serialized;
		}
		foreach (JProperty schemaProperty in schemaProperties.Properties())
		{
			if (!string.Equals((string)schemaProperty.Value?["type"], "integer", StringComparison.Ordinal)
				|| !payloadObject.TryGetValue(schemaProperty.Name, StringComparison.Ordinal, out JToken value)
				|| value.Type != JTokenType.Float
				|| !decimal.TryParse(
					value.ToString(Formatting.None),
					System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture,
					out decimal numericValue)
				|| decimal.Truncate(numericValue) != numericValue
				|| numericValue < long.MinValue
				|| numericValue > long.MaxValue)
			{
				continue;
			}
			payloadObject[schemaProperty.Name] = new JValue(decimal.ToInt64(numericValue));
		}
		return payloadObject;
	}

	internal static bool TryMigrateKnownModuleRuntimeState(
		PolicyEffectInstanceSaveData instance,
		IPolicyEffectModule module,
		out string error)
	{
		error = string.Empty;
		if (instance == null || module?.Descriptor == null
			|| !PolicyEffectModuleCatalog.TryResolveCanonicalId(instance.ModuleId, out string canonicalModuleId)
			|| !string.Equals(module.Id, canonicalModuleId, StringComparison.Ordinal))
		{
			error = "runtime state 模块与实例不匹配";
			return false;
		}
		JToken moduleState = ExtractModuleRuntimeState(instance.RuntimeState);
		if (moduleState == null || moduleState.Type == JTokenType.Null)
		{
			instance.StateSchemaVersion = module.Descriptor.RuntimeStateSchemaVersion;
			return true;
		}
		int sourceVersion = instance.StateSchemaVersion;
		if (!PolicyEffectModuleCatalog.TryMigrateRuntimeState(
			module.Id,
			moduleState,
			sourceVersion,
			out JToken migratedState,
			out error))
		{
			return false;
		}
		if (ContainsTypeMetadata(migratedState))
		{
			error = "迁移后的 runtime state 不得包含 $type 元数据";
			return false;
		}
		JObject existingRoot = instance.RuntimeState as JObject;
		bool hasFrameworkState = existingRoot != null
			&& existingRoot.TryGetValue(PolicyEffectRuntimeStateEnvelope.FrameworkProperty, StringComparison.Ordinal, out _);
		bool hasModuleEnvelope = existingRoot != null
			&& existingRoot.TryGetValue(PolicyEffectRuntimeStateEnvelope.ModuleProperty, StringComparison.Ordinal, out _);
		JObject normalizedRoot = hasFrameworkState || hasModuleEnvelope
			? (JObject)existingRoot.DeepClone()
			: new JObject();
		if (migratedState == null || migratedState.Type == JTokenType.Null)
		{
			normalizedRoot.Remove(PolicyEffectRuntimeStateEnvelope.ModuleProperty);
		}
		else
		{
			normalizedRoot[PolicyEffectRuntimeStateEnvelope.ModuleProperty] = migratedState.DeepClone();
		}
		instance.RuntimeState = normalizedRoot.HasValues ? normalizedRoot : null;
		instance.StateSchemaVersion = module.Descriptor.RuntimeStateSchemaVersion;
		return true;
	}

	private static JToken ExtractModuleRuntimeState(JToken runtimeState)
	{
		if (runtimeState == null || runtimeState.Type == JTokenType.Null)
		{
			return null;
		}
		if (!(runtimeState is JObject root))
		{
			return runtimeState.DeepClone();
		}
		if (root.TryGetValue(PolicyEffectRuntimeStateEnvelope.ModuleProperty, StringComparison.Ordinal, out JToken moduleState))
		{
			return moduleState?.DeepClone();
		}
		JObject legacyModuleState = (JObject)root.DeepClone();
		legacyModuleState.Remove(PolicyEffectRuntimeStateEnvelope.FrameworkProperty);
		return legacyModuleState.HasValues ? legacyModuleState : null;
	}

	internal static string DescribeInstance(PolicyEffectInstanceSaveData source)
	{
		if (!TryNormalizeInstance(source, out PolicyEffectNormalizedInstance normalized, out string error))
		{
			return (source?.ModuleId ?? "(unknown)") + " [invalid: " + error + "]";
		}
		if (normalized.IsInert)
		{
			return normalized.SaveData.ModuleId + " [inert: " + normalized.InertReason + "]";
		}
		if (!PolicyEffectModuleCatalog.TryFormatPayload(
			normalized.Module.Id,
			normalized.RuntimeInstance.Payload,
			out string payloadText,
			out error))
		{
			return normalized.Module.Id + " [invalid: " + error + "]";
		}
		return normalized.Module.Id + "=" + payloadText;
	}

	internal static List<string> DescribeInstances(IEnumerable<PolicyEffectInstanceSaveData> sources)
	{
		return DescribeInstancesCore(sources, playerVisible: false);
	}

	internal static List<string> DescribePlayerVisibleInstances(IEnumerable<PolicyEffectInstanceSaveData> sources)
	{
		return DescribeInstancesCore(sources, playerVisible: true);
	}

	private static List<string> DescribeInstancesCore(
		IEnumerable<PolicyEffectInstanceSaveData> sources,
		bool playerVisible)
	{
		List<PolicyEffectInstanceSaveData> instances = (sources ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		List<string> result = new List<string>(instances.Count);
		HashSet<int> consumed = new HashSet<int>();
		for (int index = 0; index < instances.Count; index++)
		{
			if (consumed.Contains(index))
			{
				continue;
			}
			PolicyEffectInstanceSaveData instance = instances[index];
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
				|| module?.Descriptor == null
				|| module.Descriptor.PromptVisible
				|| string.IsNullOrWhiteSpace(module.Descriptor.DisplayGroup))
			{
				result.Add(playerVisible
					? DescribePlayerVisibleInstance(instance, module)
					: DescribeInstance(instance));
				continue;
			}

			string displayGroup = module.Descriptor.DisplayGroup.Trim();
			string groupingKey = BuildDisplayGroupingKey(displayGroup, instance);
			List<PolicyEffectInstanceSaveData> grouped = new List<PolicyEffectInstanceSaveData>();
			for (int candidateIndex = index; candidateIndex < instances.Count; candidateIndex++)
			{
				PolicyEffectInstanceSaveData candidate = instances[candidateIndex];
				if (consumed.Contains(candidateIndex)
					|| !PolicyEffectModuleCatalog.TryGet(candidate.ModuleId, out IPolicyEffectModule candidateModule)
					|| candidateModule?.Descriptor == null
					|| candidateModule.Descriptor.PromptVisible
					|| !string.Equals(candidateModule.Descriptor.DisplayGroup, displayGroup, StringComparison.Ordinal)
					|| !string.Equals(BuildDisplayGroupingKey(displayGroup, candidate), groupingKey, StringComparison.Ordinal))
				{
					continue;
				}
				consumed.Add(candidateIndex);
				grouped.Add(candidate);
			}

			result.Add(DescribeDisplayGroup(displayGroup, grouped, playerVisible));
		}
		return result.Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
	}

	private static string DescribePlayerVisibleInstance(
		PolicyEffectInstanceSaveData source,
		IPolicyEffectModule knownModule = null)
	{
		if (!TryNormalizeInstance(source, out PolicyEffectNormalizedInstance normalized, out _))
		{
			return "无法读取的模块效果";
		}
		if (normalized.IsInert)
		{
			return "当前版本无法执行的模块效果";
		}
		IPolicyEffectModule module = normalized.Module ?? knownModule;
		if (module == null
			|| !PolicyEffectModuleCatalog.TryFormatPayload(
				module.Id,
				normalized.RuntimeInstance.Payload,
				out string payloadText,
				out _))
		{
			return "无法读取的模块效果";
		}
		string displayName = string.IsNullOrWhiteSpace(module.Descriptor?.PlayerDisplayName)
			? "模块效果"
			: module.Descriptor.PlayerDisplayName.Trim();
		if (normalized.RuntimeInstance.Payload is NumericPolicyEffectPayload numericPayload)
		{
			return displayName + "：" + DescribePlayerVisibleNumericValue(module.Descriptor, numericPayload.Value);
		}
		return string.IsNullOrWhiteSpace(payloadText)
			? displayName
			: displayName + "：" + payloadText.Trim();
	}

	private static string DescribeDisplayGroup(
		string displayGroup,
		IReadOnlyCollection<PolicyEffectInstanceSaveData> instances,
		bool playerVisible)
	{
		if (string.Equals(displayGroup, "clanInfluence", StringComparison.Ordinal))
		{
			float once = SumNumericPayloadValues(instances, "clanInfluenceNextDayOnce");
			float daily = SumNumericPayloadValues(instances, "clanInfluencePerDay");
			List<string> parts = new List<string>();
			if (Math.Abs(once) > 0.0001f
				&& instances.Any(instance => string.Equals(instance?.ModuleId, "clanInfluenceNextDayOnce", StringComparison.Ordinal)))
			{
				parts.Add("下一游戏日一次 " + FormatSignedPlayerVisibleNumber(once));
			}
			if (Math.Abs(daily) > 0.0001f
				&& instances.Any(instance => string.Equals(instance?.ModuleId, "clanInfluencePerDay", StringComparison.Ordinal)))
			{
				parts.Add("随后每日 " + FormatSignedPlayerVisibleNumber(daily));
			}
			return "影响力：" + (parts.Count > 0 ? string.Join("；", parts) : "无变化");
		}
		if (string.Equals(displayGroup, "heroGold", StringComparison.Ordinal))
		{
			float once = SumNumericPayloadValues(instances, "heroGoldNextDayOnce");
			float daily = SumNumericPayloadValues(instances, "heroGoldPerDay");
			List<string> parts = new List<string>();
			if (instances.Any(instance => string.Equals(instance?.ModuleId, "heroGoldNextDayOnce", StringComparison.Ordinal)))
			{
				parts.Add("下一游戏日一次 " + FormatSignedPlayerVisibleNumber(once));
			}
			if (instances.Any(instance => string.Equals(instance?.ModuleId, "heroGoldPerDay", StringComparison.Ordinal)))
			{
				parts.Add("随后每日 " + FormatSignedPlayerVisibleNumber(daily));
			}
			return "人物第纳尔：" + (parts.Count > 0 ? string.Join("；", parts) : "无变化");
		}
		if (string.Equals(displayGroup, "soldierTroopXp", StringComparison.Ordinal))
		{
			float once = SumNumericPayloadValues(instances, "soldierTroopXpOnce");
			float daily = SumNumericPayloadValues(instances, "soldierTroopXpPerDay");
			List<string> parts = new List<string>();
			if (instances.Any(instance => string.Equals(instance?.ModuleId, "soldierTroopXpOnce", StringComparison.Ordinal)))
			{
				parts.Add("下一游戏日一次 " + FormatSignedPlayerVisibleNumber(once) + " XP/兵");
			}
			if (instances.Any(instance => string.Equals(instance?.ModuleId, "soldierTroopXpPerDay", StringComparison.Ordinal)))
			{
				parts.Add("随后每日 " + FormatSignedPlayerVisibleNumber(daily) + " XP/兵");
			}
			return "士兵精锐化：" + (parts.Count > 0 ? string.Join("；", parts) : "无变化");
		}
		if (string.Equals(displayGroup, "kingdomStability", StringComparison.Ordinal))
		{
			float value = SumNumericPayloadValues(
				instances,
				"kingdomStabilityNextDayOnce",
				"kingdomStabilityOnce");
			bool hasDeferredInstance = (instances ?? Array.Empty<PolicyEffectInstanceSaveData>()).Any(instance =>
				string.Equals(instance?.ModuleId, "kingdomStabilityNextDayOnce", StringComparison.Ordinal));
			return "王国稳定度：" + (hasDeferredInstance ? "下一游戏日一次 " : "生效时一次 ")
				+ FormatSignedPlayerVisibleNumber(value);
		}

		PolicyEffectInstanceSaveData first = instances?.FirstOrDefault();
		if (first == null)
		{
			return playerVisible ? "模块效果" : displayGroup;
		}
		if (TryNormalizeInstance(first, out PolicyEffectNormalizedInstance normalized, out _)
			&& !normalized.IsInert
			&& PolicyEffectModuleCatalog.TryFormatPayload(
				normalized.Module.Id,
				normalized.RuntimeInstance.Payload,
				out string payloadText,
				out _))
		{
			if (playerVisible)
			{
				string displayName = string.IsNullOrWhiteSpace(normalized.Module.Descriptor?.PlayerDisplayName)
					? "模块效果"
					: normalized.Module.Descriptor.PlayerDisplayName.Trim();
				return string.IsNullOrWhiteSpace(payloadText)
					? displayName
					: displayName + "：" + payloadText.Trim();
			}
			return displayGroup + "=" + payloadText;
		}
		return playerVisible ? "模块效果" : displayGroup;
	}

	private static string DescribePlayerVisibleNumericValue(
		PolicyEffectModuleDescriptor descriptor,
		double value)
	{
		string signed = FormatSignedPlayerVisibleNumber(value);
		if (descriptor?.ValueUnit == PolicyEffectValueUnit.PercentPoints)
		{
			return "原版主税收 " + signed + "%";
		}
		if (descriptor?.ValueUnit == PolicyEffectValueUnit.RelativePercent)
		{
			if (descriptor.Hook == PolicyEffectHook.VolunteerProductionProbability)
			{
				return "相对原版每日判定频率 " + signed + "%";
			}
			return "相对原版候选分数 " + signed + "%";
		}
		if (descriptor?.ExecutionKind == PolicyEffectExecutionKind.ScheduledOnce)
		{
			return "下一游戏日一次 " + signed;
		}
		if (descriptor?.ExecutionKind == PolicyEffectExecutionKind.OneShot
			|| descriptor?.ValueUnit == PolicyEffectValueUnit.PointsOnce)
		{
			return "生效时一次 " + signed;
		}
		return "每日 " + signed;
	}

	private static string FormatSignedPlayerVisibleNumber(double value)
	{
		string number = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
		return value > 0d ? "+" + number : number;
	}

	private static float SumNumericPayloadValues(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		params string[] moduleIds)
	{
		HashSet<string> acceptedIds = new HashSet<string>(moduleIds ?? Array.Empty<string>(), StringComparer.Ordinal);
		double sum = 0d;
		foreach (PolicyEffectInstanceSaveData instance in instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
		{
			if (instance == null || !acceptedIds.Contains(instance.ModuleId ?? string.Empty))
			{
				continue;
			}
			JToken valueToken = instance.Payload?["value"];
			if (valueToken != null
				&& double.TryParse(
					valueToken.ToString(),
					System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture,
					out double value)
				&& !double.IsNaN(value)
				&& !double.IsInfinity(value))
			{
				sum += value;
			}
		}
		return (float)sum;
	}

	private static string BuildDisplayGroupingKey(
		string displayGroup,
		PolicyEffectInstanceSaveData instance)
	{
		PolicyEffectCanonicalTargetSet targetSet = instance?.TargetSet;
		JObject stableTargetExpression = targetSet == null
			? new JObject()
			: new JObject
			{
				["selectorHandles"] = new JArray((targetSet.SelectorHandles ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["selectorIds"] = new JArray((targetSet.SelectorIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["targetPlans"] = JArray.FromObject(targetSet.TargetPlans ?? new List<PolicyTargetPlanSaveData>()),
				["settlementIds"] = new JArray((targetSet.SettlementIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["townIds"] = new JArray((targetSet.TownIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["villageIds"] = new JArray((targetSet.VillageIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["clanIds"] = new JArray((targetSet.ClanIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["kingdomIds"] = new JArray((targetSet.KingdomIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["heroIds"] = new JArray((targetSet.HeroIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["parentSettlementIds"] = new JArray((targetSet.ParentSettlementIds ?? new List<string>())
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["followCurrentRulingClan"] = targetSet.FollowCurrentRulingClan
			};
		return (displayGroup ?? string.Empty)
			+ "\u001f" + (instance?.MechanismId ?? string.Empty)
			+ "\u001f" + (instance?.MechanismRole.ToString() ?? string.Empty)
			+ "\u001f" + (instance?.Reason ?? string.Empty)
			+ "\u001f" + stableTargetExpression.ToString(Formatting.None);
	}

	internal static bool TryNormalizeActiveV4ToV5(
		global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeActiveV4ToV5, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeActiveV4ToV6(
		global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeActiveV4ToV6, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeActiveV4ToV7(
		global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeActiveV4ToV7, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeActiveV4ToV8(
		global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.ActivePolicyEffectSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeActiveV4ToV8, out normalized, out summary, out error);
	}

	internal static bool TryNormalizePolicyRecordV1ToV2(
		global::AnimusForge.CustomPolicyBehavior.PolicyRecordSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.PolicyRecordSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizePolicyRecordV1ToV2, out normalized, out summary, out error);
	}

	internal static bool TryNormalizePolicyRecordV1ToV3(
		global::AnimusForge.CustomPolicyBehavior.PolicyRecordSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.PolicyRecordSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizePolicyRecordV1ToV3, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeLocalV1ToV4(
		global::AnimusForge.CustomPolicyBehavior.LocalPolicyRecordSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.LocalPolicyRecordSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeLocalV1ToV4, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeLocalV1ToV5(
		global::AnimusForge.CustomPolicyBehavior.LocalPolicyRecordSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.LocalPolicyRecordSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeLocalV1ToV5, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeLocalV1ToV6(
		global::AnimusForge.CustomPolicyBehavior.LocalPolicyRecordSaveData source,
		out global::AnimusForge.CustomPolicyBehavior.LocalPolicyRecordSaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeLocalV1ToV6, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeDynamicV1ToV2(
		global::AnimusForge.CustomPolicyBehavior.DynamicPolicySaveData source,
		out global::AnimusForge.CustomPolicyBehavior.DynamicPolicySaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeDynamicV1ToV2, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeDynamicV1ToV3(
		global::AnimusForge.CustomPolicyBehavior.DynamicPolicySaveData source,
		out global::AnimusForge.CustomPolicyBehavior.DynamicPolicySaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeDynamicV1ToV3, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeDynamicV1ToV4(
		global::AnimusForge.CustomPolicyBehavior.DynamicPolicySaveData source,
		out global::AnimusForge.CustomPolicyBehavior.DynamicPolicySaveData normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		return TryNormalizeTyped(source, TryNormalizeDynamicV1ToV4, out normalized, out summary, out error);
	}

	internal static bool TryNormalizeActiveV4ToV5(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		if (!TryCloneVersioned(source, 4, 5, out normalized, out int version, out error))
		{
			return false;
		}
		if (version == 5)
		{
			if (!TryCanonicalizeActiveModuleEffects(normalized, out bool activeChanged, out error))
			{
				return false;
			}
			if (activeChanged)
			{
				summary.RecordsChanged = 1;
			}
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		JArray moduleEffects = GetOrCreateArray(normalized, "ModuleEffects");
		JArray receipts = GetOrCreateArray(normalized, "ExecutionReceipts");
		bool migratedLegacyActive = moduleEffects.Count == 0;
		if (migratedLegacyActive
			&& !TryAppendLegacyInstances(normalized, normalized, 4, moduleEffects, receipts, summary, out error))
		{
			return false;
		}
		if (migratedLegacyActive
			&& !TryEnsureLegacyActiveStabilityReceipt(normalized, moduleEffects, receipts, summary, out error))
		{
			return false;
		}
		if (!TryCanonicalizeActiveModuleEffects(normalized, out _, out error))
		{
			return false;
		}
		normalized["Version"] = 5;
		summary.RecordsChanged = 1;
		return TryValidateEmbeddedInstances(normalized, summary, out error);
	}

	internal static bool TryNormalizeActiveV4ToV6(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 4);
		if (version < 4 || version > 6)
		{
			error = "不支持的 active policy effect 版本: " + version;
			return false;
		}
		if (version == 6)
		{
			normalized = (JObject)source.DeepClone();
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		if (!TryNormalizeActiveV4ToV5(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 6;
		normalized["IsPermanentEffect"] = false;
		normalized["DailyMaintenanceGoldCost"] = 0;
		normalized["TotalMaintenancePaidGold"] = 0;
		normalized["MaintenanceChargeEnabled"] = false;
		normalized["MaintenanceFunded"] = true;
		int lastAppliedDay = ReadInt(normalized, "LastAppliedDay", -1);
		normalized["LastMaintenanceSettlementDay"] = lastAppliedDay;
		normalized["LastEffectProcessedDay"] = lastAppliedDay;
		summary.RecordsChanged = 1;
		return true;
	}

	internal static bool TryNormalizeActiveV4ToV7(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 4);
		if (version < 4 || version > 7)
		{
			error = "不支持的 active policy effect 版本: " + version;
			return false;
		}
		if (version == 7)
		{
			normalized = (JObject)source.DeepClone();
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		if (!TryNormalizeActiveV4ToV6(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 7;
		if (string.IsNullOrWhiteSpace(ReadString(normalized, "IssuerKingdomId")))
		{
			normalized["IssuerKingdomId"] = ReadString(normalized, "TargetKingdomId");
		}
		summary.RecordsChanged = 1;
		return true;
	}

	internal static bool TryNormalizeActiveV4ToV8(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 4);
		if (version < 4 || version > 8)
		{
			error = "不支持的 active policy effect 版本: " + version;
			return false;
		}
		if (version == 8)
		{
			normalized = (JObject)source.DeepClone();
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		if (!TryNormalizeActiveV4ToV7(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 8;
		summary.RecordsChanged = 1;
		return TryValidateEmbeddedInstances(normalized, summary, out error);
	}

	internal static bool TryNormalizePolicyRecordV1ToV2(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		if (!TryCloneVersioned(source, 1, 2, out normalized, out int version, out error))
		{
			return false;
		}
		if (version == 2)
		{
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		JArray effects = GetOrCreateArray(normalized, "Effects");
		foreach (JObject effect in effects.OfType<JObject>())
		{
			JArray moduleEffects = GetOrCreateArray(effect, "ModuleEffects");
			JArray effectReceipts = GetOrCreateArray(effect, "ExecutionReceipts");
			if (moduleEffects.Count == 0
				&& !TryAppendLegacyInstances(effect, normalized, 1, moduleEffects, effectReceipts, summary, out error))
			{
				return false;
			}
		}
		normalized["Version"] = 2;
		summary.RecordsChanged = 1;
		return TryValidateEmbeddedInstances(normalized, summary, out error);
	}

	internal static bool TryNormalizePolicyRecordV1ToV3(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 1);
		if (version < 1 || version > 3)
		{
			error = "不支持的 policy record 版本: " + version;
			return false;
		}
		if (version == 3)
		{
			normalized = (JObject)source.DeepClone();
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		if (!TryNormalizePolicyRecordV1ToV2(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 3;
		normalized["IsPermanentEffect"] = false;
		normalized["DailyMaintenanceGoldCost"] = 0;
		normalized["TotalMaintenancePaidGold"] = 0;
		normalized["MaintenanceFunded"] = true;
		normalized["LastMaintenanceSettlementDay"] = -1;
		normalized["LastEffectProcessedDay"] = -1;
		foreach (JObject effect in GetOrCreateArray(normalized, "Effects").OfType<JObject>())
		{
			effect["IsPermanentEffect"] = false;
		}
		summary.RecordsChanged = 1;
		return true;
	}

	internal static bool TryNormalizeLocalV1ToV4(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		if (source == null)
		{
			error = "local policy record 不能为空";
			return false;
		}
		int version = ReadInt(source, "Version", 1);
		if (version < 1 || version > 4)
		{
			error = "不支持的 local policy 版本: " + version;
			return false;
		}
		normalized = (JObject)source.DeepClone();
		if (version == 4)
		{
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		JArray effects = GetOrCreateArray(normalized, "Effects");
		if (effects.Count == 0)
		{
			effects.Add(new JObject
			{
				["TargetHandle"] = ReadString(normalized, "TargetHandle"),
				["ActiveEffectId"] = ReadString(normalized, "ActiveEffectId"),
				["TargetKingdomId"] = ReadString(normalized, "TargetKingdomId"),
				["Reason"] = ReadString(normalized, "EffectReason")
			});
		}
		foreach (JObject effect in effects.OfType<JObject>())
		{
			JArray moduleEffects = GetOrCreateArray(effect, "ModuleEffects");
			JArray effectReceipts = GetOrCreateArray(effect, "ExecutionReceipts");
			if (moduleEffects.Count == 0
				&& !TryAppendLegacyInstances(effect, normalized, version, moduleEffects, effectReceipts, summary, out error))
			{
				return false;
			}
		}
		normalized["Version"] = 4;
		summary.RecordsChanged = 1;
		return TryValidateEmbeddedInstances(normalized, summary, out error);
	}

	internal static bool TryNormalizeLocalV1ToV5(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 1);
		if (version < 1 || version > 5)
		{
			error = "不支持的 local policy record 版本: " + version;
			return false;
		}
		if (version == 5)
		{
			normalized = (JObject)source.DeepClone();
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		if (!TryNormalizeLocalV1ToV4(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		string legacyStatus = ReadString(normalized, "Status");
		bool isPlayerLocalPolicy = !string.Equals(ReadString(normalized, "ScopeKind"), "vassal", StringComparison.OrdinalIgnoreCase);
		bool terminalPolicy = string.Equals(legacyStatus, "abolished", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(legacyStatus, "relationship_ended", StringComparison.OrdinalIgnoreCase);
		normalized["Version"] = 5;
		normalized["EffectStatus"] = string.IsNullOrWhiteSpace(legacyStatus) ? "active" : legacyStatus;
		if (isPlayerLocalPolicy && !terminalPolicy)
		{
			normalized["Status"] = "active";
		}
		normalized["IsPermanentEffect"] = false;
		normalized["DailyMaintenanceGoldCost"] = 0;
		normalized["TotalMaintenancePaidGold"] = 0;
		normalized["MaintenanceFunded"] = true;
		normalized["LastMaintenanceSettlementDay"] = -1;
		normalized["LastEffectProcessedDay"] = -1;
		bool effectEnded = string.Equals(legacyStatus, "expired", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(legacyStatus, "targets_lost", StringComparison.OrdinalIgnoreCase);
		foreach (JObject effect in GetOrCreateArray(normalized, "Effects").OfType<JObject>())
		{
			effect["IsPermanentEffect"] = false;
			effect["IsEnded"] = effectEnded;
			if (effectEnded)
			{
				effect["EndReason"] = ReadString(normalized, "EndReason");
			}
		}
		summary.RecordsChanged = 1;
		return true;
	}

	internal static bool TryNormalizeLocalV1ToV6(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 1);
		if (version < 1 || version > 6)
		{
			error = "不支持的 local policy record 版本: " + version;
			return false;
		}
		if (version == 6)
		{
			normalized = (JObject)source.DeepClone();
			return TryValidateEmbeddedInstances(normalized, summary, out error);
		}
		if (!TryNormalizeLocalV1ToV5(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 6;
		normalized["ExternalLastAttemptDay"] = -1;
		normalized["ExternalInputsCaptured"] = false;
		normalized["ExternalPublicationCost"] = 0;
		normalized["ExternalQualityDelta"] = 0;
		string endReason = ReadString(normalized, "EndReason");
		if (endReason.StartsWith("rollbackPending:", StringComparison.Ordinal))
		{
			normalized["ExternalCommitState"] = "externalCommitPending";
			normalized["ExternalLastError"] = endReason;
		}
		summary.RecordsChanged = 1;
		return TryValidateEmbeddedInstances(normalized, summary, out error);
	}

	internal static bool TryNormalizeDynamicV1ToV2(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		if (!TryCloneVersioned(source, 1, 2, out normalized, out int version, out error))
		{
			return false;
		}
		string before = normalized.ToString(Formatting.None);
		if (!TryNormalizeDynamicPlayerPayload(normalized, summary, out error))
		{
			return false;
		}
		if (version == 1)
		{
			normalized["Version"] = 2;
		}
		summary.RecordsChanged = version == 1
			|| !string.Equals(before, normalized.ToString(Formatting.None), StringComparison.Ordinal)
			? 1
			: 0;
		return true;
	}

	internal static bool TryNormalizeDynamicV1ToV3(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 1);
		if (version < 1 || version > 3)
		{
			error = "不支持的 dynamic policy 版本: " + version;
			return false;
		}
		if (version == 3)
		{
			normalized = (JObject)source.DeepClone();
			string before = normalized.ToString(Formatting.None);
			if (!TryNormalizeDynamicPlayerPayload(normalized, summary, out error))
			{
				return false;
			}
			summary.RecordsChanged = string.Equals(before, normalized.ToString(Formatting.None), StringComparison.Ordinal) ? 0 : 1;
			return true;
		}
		if (!TryNormalizeDynamicV1ToV2(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 3;
		if (string.IsNullOrWhiteSpace(ReadString(normalized, "IssuerKingdomId")))
		{
			normalized["IssuerKingdomId"] = ReadString(normalized, "OwnerKingdomId");
		}
		summary.RecordsChanged = 1;
		return true;
	}

	internal static bool TryNormalizeDynamicV1ToV4(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		summary = NewSummary();
		normalized = null;
		error = string.Empty;
		int version = ReadInt(source, "Version", 1);
		if (version < 1 || version > 4)
		{
			error = "不支持的 dynamic policy 版本: " + version;
			return false;
		}
		if (version == 4)
		{
			normalized = (JObject)source.DeepClone();
			string before = normalized.ToString(Formatting.None);
			if (!TryNormalizeDynamicPlayerPayload(normalized, summary, out error))
			{
				return false;
			}
			summary.RecordsChanged = string.Equals(before, normalized.ToString(Formatting.None), StringComparison.Ordinal) ? 0 : 1;
			return true;
		}
		if (!TryNormalizeDynamicV1ToV3(source, out normalized, out PolicyEffectMigrationBatchSummary legacySummary, out error))
		{
			return false;
		}
		summary = legacySummary;
		normalized["Version"] = 4;
		normalized["ActiveEffectId"] = string.Empty;
		normalized["RequiresEffectBundle"] = false;
		string status = ReadString(normalized, "Status");
		normalized["CommitState"] = string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
			? "commitPending"
			: string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase)
				? "pending"
				: string.Equals(status, "ended", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(status, "abolished", StringComparison.OrdinalIgnoreCase)
					? "ended"
					: "failed";
		summary.RecordsChanged = 1;
		return true;
	}

	private static bool TryNormalizeDynamicPlayerPayload(
		JObject dynamicRecord,
		PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		error = string.Empty;
		if (!TryGetUniqueProperty(dynamicRecord, "PlayerPayloadJson", out JProperty playerPayloadProperty, out error))
		{
			return false;
		}
		if (playerPayloadProperty == null || playerPayloadProperty.Value.Type == JTokenType.Null)
		{
			return true;
		}
		if (playerPayloadProperty.Value.Type != JTokenType.String)
		{
			error = "DynamicPolicy PlayerPayloadJson 必须是 JSON 字符串";
			return false;
		}
		string rawPayload = playerPayloadProperty.Value.Value<string>() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(rawPayload))
		{
			return true;
		}

		JObject pending;
		try
		{
			pending = JObject.Parse(rawPayload, new JsonLoadSettings
			{
				CommentHandling = CommentHandling.Ignore,
				DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
				LineInfoHandling = LineInfoHandling.Ignore
			});
		}
		catch (Exception ex)
		{
			error = "DynamicPolicy PlayerPayloadJson 解析失败: " + ex.Message;
			return false;
		}
		if (ContainsTypeMetadata(pending))
		{
			error = "DynamicPolicy PlayerPayloadJson 不得包含 $type 元数据";
			return false;
		}
		if (!TryGetUniqueProperty(pending, "Assessment", out JProperty assessmentProperty, out error))
		{
			return false;
		}
		if (assessmentProperty == null || assessmentProperty.Value.Type == JTokenType.Null)
		{
			return true;
		}
		if (!(assessmentProperty.Value is JObject assessment))
		{
			error = "DynamicPolicy PlayerPayloadJson.Assessment 必须是对象";
			return false;
		}
		if (!TryGetUniqueProperty(assessment, "effects", out JProperty effectsProperty, out error))
		{
			return false;
		}
		if (effectsProperty == null || effectsProperty.Value.Type == JTokenType.Null)
		{
			return true;
		}
		if (!(effectsProperty.Value is JArray effects))
		{
			error = "DynamicPolicy PlayerPayloadJson.Assessment.effects 必须是数组";
			return false;
		}

		JArray normalizedEffects = new JArray();
		HashSet<string> seenModuleTargetSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int totalPayloadBytes = 0;
		foreach (JToken token in effects)
		{
			if (!(token is JObject effect))
			{
				error = "DynamicPolicy PlayerPayloadJson.Assessment.effects 每项必须是对象";
				return false;
			}
			if (!TryNormalizeDynamicWireEffect(
				pending,
				effect,
				normalizedEffects,
				seenModuleTargetSets,
				ref totalPayloadBytes,
				summary,
				out error))
			{
				return false;
			}
		}

		effectsProperty.Remove();
		assessment["effects"] = normalizedEffects;
		playerPayloadProperty.Value = pending.ToString(Formatting.None);
		return true;
	}

	private static bool TryNormalizeDynamicWireEffect(
		JObject pending,
		JObject effect,
		JArray normalizedEffects,
		HashSet<string> seenModuleTargetSets,
		ref int totalPayloadBytes,
		PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		error = string.Empty;
		if (!TryBuildCaseInsensitivePropertyMap(effect, out Dictionary<string, JProperty> properties, out error))
		{
			return false;
		}
		foreach (string propertyName in properties.Keys)
		{
			if (!IsAllowedDynamicWireProperty(propertyName))
			{
				error = "DynamicPolicy effect 包含未知字段: " + propertyName;
				return false;
			}
		}

		bool hasLegacyFields = properties.Keys.Any(name => LegacyPolicyEffectFieldAdapter.TryResolveModuleId(name, out _));
		IReadOnlyDictionary<string, float> legacyValues = new Dictionary<string, float>(StringComparer.Ordinal);
		if (hasLegacyFields
			&& !LegacyPolicyEffectFieldAdapter.TryReadLegacyFields(effect, out legacyValues, out error))
		{
			return false;
		}
		legacyValues = legacyValues ?? new Dictionary<string, float>(StringComparer.Ordinal);

		bool hasChanges = HasNonNullProperty(properties, "changes");
		bool hasModuleId = HasNonNullProperty(properties, "moduleId");
		bool hasPayload = HasNonNullProperty(properties, "payload");
		bool hasTargetHandles = HasNonNullProperty(properties, "targetHandles");
		bool hasNewWireShape = hasModuleId || hasPayload || hasTargetHandles;
		if (hasChanges)
		{
			if (hasModuleId || hasPayload || legacyValues.Count > 0)
			{
				error = "DynamicPolicy effect 不得混用 changes、新 module wire 或非零固定字段";
				return false;
			}
			return TryNormalizeDynamicChangesEffect(
				pending,
				properties,
				normalizedEffects,
				seenModuleTargetSets,
				ref totalPayloadBytes,
				out error);
		}

		if (hasNewWireShape)
		{
			if (!hasModuleId || !hasPayload || !hasTargetHandles || legacyValues.Count > 0
				|| HasNonNullProperty(properties, "targets"))
			{
				error = "DynamicPolicy 新 module wire 结构不完整或混入旧字段";
				return false;
			}
			if (!TryReadRequiredString(properties, "moduleId", out string moduleId, out error)
				|| !TryReadDynamicTargetHandles(properties, pending, allowRequestResolution: false, out List<string> targetHandles, out error))
			{
				return false;
			}
			if (!(properties["payload"].Value is JObject payload))
			{
				error = "DynamicPolicy 新 module wire 的 payload 必须是对象";
				return false;
			}
			if (!TryReadOptionalReason(properties, out string reason, out error))
			{
				return false;
			}
			return TryAppendDynamicWireEffect(
				moduleId,
				targetHandles,
				payload,
				reason,
				normalizedEffects,
				seenModuleTargetSets,
				ref totalPayloadBytes,
				out error);
		}

		if (!hasLegacyFields)
		{
			error = "DynamicPolicy effect 不是受支持的 module、changes 或旧固定字段结构";
			return false;
		}
		if (!TryValidateNoDuplicateLegacyModules(properties, out error))
		{
			return false;
		}
		if (legacyValues.Count == 0)
		{
			return true;
		}
		if (!TryReadDynamicTargetHandles(properties, pending, allowRequestResolution: true, out List<string> legacyTargets, out error)
			|| !TryReadOptionalReason(properties, out string legacyReason, out error))
		{
			return false;
		}
		foreach (KeyValuePair<string, float> legacyValue in legacyValues)
		{
			if (!TryAppendDynamicWireEffect(
				legacyValue.Key,
				legacyTargets,
				new JObject { ["value"] = legacyValue.Value },
				legacyReason,
				normalizedEffects,
				seenModuleTargetSets,
				ref totalPayloadBytes,
				out error))
			{
				return false;
			}
			summary.LegacyFieldsMigrated++;
		}
		return true;
	}

	private static bool TryNormalizeDynamicChangesEffect(
		JObject pending,
		IReadOnlyDictionary<string, JProperty> properties,
		JArray normalizedEffects,
		HashSet<string> seenModuleTargetSets,
		ref int totalPayloadBytes,
		out string error)
	{
		error = string.Empty;
		if (!(properties["changes"].Value is JObject changes)
			|| !TryBuildCaseInsensitivePropertyMap(changes, out Dictionary<string, JProperty> changeProperties, out error))
		{
			if (string.IsNullOrWhiteSpace(error))
			{
				error = "DynamicPolicy effect.changes 必须是对象";
			}
			return false;
		}
		List<KeyValuePair<string, JToken>> nonZeroChanges = new List<KeyValuePair<string, JToken>>();
		foreach (KeyValuePair<string, JProperty> change in changeProperties)
		{
			string moduleId = (change.Key ?? string.Empty).Trim();
			if (moduleId.Length == 0 || !TryReadFiniteNumber(change.Value.Value, out double value))
			{
				error = "DynamicPolicy effect.changes 包含空 moduleId 或非有限数值";
				return false;
			}
			if (Math.Abs(value) <= 0.0001d)
			{
				continue;
			}
			nonZeroChanges.Add(new KeyValuePair<string, JToken>(moduleId, change.Value.Value));
		}
		if (nonZeroChanges.Count == 0)
		{
			return true;
		}
		if (!TryReadDynamicTargetHandles(properties, pending, allowRequestResolution: true, out List<string> targetHandles, out error)
			|| !TryReadOptionalReason(properties, out string reason, out error))
		{
			return false;
		}
		foreach (KeyValuePair<string, JToken> change in nonZeroChanges)
		{
			if (!TryAppendDynamicWireEffect(
				change.Key,
				targetHandles,
				new JObject { ["value"] = change.Value.DeepClone() },
				reason,
				normalizedEffects,
				seenModuleTargetSets,
				ref totalPayloadBytes,
				out error))
			{
				return false;
			}
		}
		return true;
	}

	private static bool TryAppendDynamicWireEffect(
		string rawModuleId,
		IReadOnlyList<string> targetHandles,
		JObject payload,
		string reason,
		JArray normalizedEffects,
		HashSet<string> seenModuleTargetSets,
		ref int totalPayloadBytes,
		out string error)
	{
		error = string.Empty;
		string moduleId = (rawModuleId ?? string.Empty).Trim();
		if (moduleId.Length == 0 || payload == null || targetHandles == null || targetHandles.Count == 0)
		{
			error = "DynamicPolicy module wire 缺少 moduleId、targetHandles 或 payload";
			return false;
		}
		if (PolicyEffectModuleCatalog.TryResolveCanonicalId(moduleId, out string canonicalModuleId))
		{
			moduleId = canonicalModuleId;
		}
		if (ContainsTypeMetadata(payload) || ContainsNonFiniteNumber(payload))
		{
			error = "DynamicPolicy module wire payload 包含 $type 或非有限数值: " + moduleId;
			return false;
		}
		int payloadBytes = Utf8Size(payload.ToString(Formatting.None));
		if (payloadBytes > MaxPayloadBytes)
		{
			error = "DynamicPolicy module wire payload 超过 4KiB: " + moduleId;
			return false;
		}
		string targetSetKey = string.Join("\u001f", targetHandles
			.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
			.Select(value => value.ToUpperInvariant()));
		string duplicateKey = moduleId.ToUpperInvariant() + "\u001e" + targetSetKey;
		if (!seenModuleTargetSets.Add(duplicateKey))
		{
			JObject existing = normalizedEffects
				.OfType<JObject>()
				.FirstOrDefault(candidate =>
					string.Equals(candidate.Value<string>("moduleId"), moduleId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(
						string.Join("\u001f", ((candidate["targetHandles"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>())
							.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
							.Select(value => value.ToUpperInvariant())),
						targetSetKey,
						StringComparison.Ordinal));
			if (existing != null
				&& JToken.DeepEquals(existing["payload"], payload)
				&& string.Equals(existing.Value<string>("reason") ?? string.Empty, reason ?? string.Empty, StringComparison.Ordinal))
			{
				return true;
			}
			error = "DynamicPolicy module wire 包含冲突的重复 moduleId + targetHandles: " + moduleId;
			return false;
		}
		if (totalPayloadBytes + payloadBytes > MaxTotalPayloadBytes)
		{
			error = "DynamicPolicy module wire payload 总量超过 32KiB";
			return false;
		}
		if (normalizedEffects.Count >= MaxWireEffectsPerPolicy)
		{
			error = "DynamicPolicy module wire 超过 12 个效果实例";
			return false;
		}

		totalPayloadBytes += payloadBytes;
		normalizedEffects.Add(new JObject
		{
			["moduleId"] = moduleId,
			["targetHandles"] = new JArray(targetHandles.Select(value => (JToken)new JValue(value))),
			["payload"] = payload.DeepClone(),
			["reason"] = reason ?? string.Empty
		});
		return true;
	}

	private static bool TryReadDynamicTargetHandles(
		IReadOnlyDictionary<string, JProperty> properties,
		JObject pending,
		bool allowRequestResolution,
		out List<string> targetHandles,
		out string error)
	{
		targetHandles = new List<string>();
		error = string.Empty;
		List<JProperty> explicitTargets = new List<JProperty>();
		foreach (string propertyName in new[] { "targetHandles", "targets", "targetHandle" })
		{
			if (properties.TryGetValue(propertyName, out JProperty property)
				&& property.Value.Type != JTokenType.Null)
			{
				explicitTargets.Add(property);
			}
		}
		if (explicitTargets.Count > 1)
		{
			error = "DynamicPolicy effect 不得同时提供多种目标字段";
			return false;
		}
		if (explicitTargets.Count == 1)
		{
			JProperty property = explicitTargets[0];
			if (string.Equals(property.Name, "targetHandle", StringComparison.OrdinalIgnoreCase))
			{
				if (property.Value.Type != JTokenType.String || string.IsNullOrWhiteSpace(property.Value.Value<string>()))
				{
					error = "DynamicPolicy effect.targetHandle 必须是非空字符串";
					return false;
				}
				targetHandles.Add(property.Value.Value<string>().Trim());
				return true;
			}
			if (!(property.Value is JArray array))
			{
				error = "DynamicPolicy effect 目标字段必须是字符串数组";
				return false;
			}
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (JToken target in array)
			{
				if (target.Type != JTokenType.String)
				{
					error = "DynamicPolicy effect 目标句柄必须是字符串";
					return false;
				}
				string value = (target.Value<string>() ?? string.Empty).Trim();
				if (value.Length == 0 || !seen.Add(value))
				{
					error = "DynamicPolicy effect 目标句柄为空或重复";
					return false;
				}
				targetHandles.Add(value);
			}
			if (targetHandles.Count == 0)
			{
				error = "DynamicPolicy effect 目标句柄不能为空";
				return false;
			}
			return true;
		}
		if (!allowRequestResolution)
		{
			error = "DynamicPolicy 新 module wire 缺少 targetHandles";
			return false;
		}
		return TryResolveDynamicLegacyTargetFromRequest(pending, properties, out targetHandles, out error);
	}

	private static bool TryResolveDynamicLegacyTargetFromRequest(
		JObject pending,
		IReadOnlyDictionary<string, JProperty> effectProperties,
		out List<string> targetHandles,
		out string error)
	{
		targetHandles = new List<string>();
		error = string.Empty;
		if (!TryGetUniqueProperty(pending, "Request", out JProperty requestProperty, out error))
		{
			return false;
		}
		if (!(requestProperty?.Value is JObject request)
			|| !TryGetUniqueProperty(request, "TargetHandles", out JProperty handlesProperty, out error))
		{
			if (string.IsNullOrWhiteSpace(error))
			{
				error = "DynamicPolicy 旧固定 effect 缺少可解析的 Request.TargetHandles";
			}
			return false;
		}
		if (!(handlesProperty?.Value is JArray handles))
		{
			error = "DynamicPolicy Request.TargetHandles 必须是数组";
			return false;
		}

		if (!TryReadOptionalString(effectProperties, "targetScope", out string requestedScope, out error)
			|| !TryReadOptionalString(effectProperties, "targetKingdomId", out string requestedKingdomId, out error)
			|| !TryReadOptionalString(effectProperties, "targetKingdomName", out string requestedKingdomName, out error))
		{
			return false;
		}
		List<string> candidates = new List<string>();
		HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JToken handleToken in handles)
		{
			if (!(handleToken is JObject handle))
			{
				error = "DynamicPolicy Request.TargetHandles 每项必须是对象";
				return false;
			}
			if (!TryBuildCaseInsensitivePropertyMap(handle, out Dictionary<string, JProperty> handleProperties, out error)
				|| !TryReadRequiredString(handleProperties, "Key", out string key, out error)
				|| !TryReadOptionalString(handleProperties, "Kind", out string kind, out error)
				|| !TryReadOptionalString(handleProperties, "EntityId", out string entityId, out error)
				|| !TryReadOptionalString(handleProperties, "DisplayName", out string displayName, out error)
				|| !TryReadOptionalString(handleProperties, "KingdomId", out string kingdomId, out error)
				|| !TryReadOptionalString(handleProperties, "KingdomName", out string kingdomName, out error))
			{
				return false;
			}
			if (!seenKeys.Add(key))
			{
				error = "DynamicPolicy Request.TargetHandles 包含重复 Key";
				return false;
			}
			if (requestedKingdomId.Length > 0
				&& !string.Equals(kingdomId, requestedKingdomId, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(entityId, requestedKingdomId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (requestedKingdomName.Length > 0
				&& !string.Equals(kingdomName, requestedKingdomName, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(displayName, requestedKingdomName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (requestedScope.Length > 0)
			{
				if (string.Equals(requestedScope, "source", StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(kind, "source", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (string.Equals(requestedScope, "mentioned", StringComparison.OrdinalIgnoreCase)
					&& string.Equals(kind, "source", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (!string.Equals(requestedScope, "source", StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(requestedScope, "mentioned", StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(requestedScope, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(requestedScope, PolicyEffectScopes.Vassal, StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(requestedScope, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
				{
					error = "DynamicPolicy 旧固定 effect targetScope 无效: " + requestedScope;
					return false;
				}
			}
			candidates.Add(key);
		}
		if (candidates.Count != 1)
		{
			error = candidates.Count == 0
				? "DynamicPolicy 旧固定 effect 目标不合法"
				: "DynamicPolicy 旧固定 effect 目标不唯一";
			return false;
		}
		targetHandles.Add(candidates[0]);
		return true;
	}

	private static bool TryValidateNoDuplicateLegacyModules(
		IReadOnlyDictionary<string, JProperty> properties,
		out string error)
	{
		error = string.Empty;
		HashSet<string> modules = new HashSet<string>(StringComparer.Ordinal);
		foreach (string propertyName in properties.Keys)
		{
			if (LegacyPolicyEffectFieldAdapter.TryResolveModuleId(propertyName, out string moduleId)
				&& !modules.Add(moduleId))
			{
				error = "DynamicPolicy 旧固定 effect 包含重复模块字段: " + moduleId;
				return false;
			}
		}
		return true;
	}

	private static bool TryGetUniqueProperty(
		JObject source,
		string propertyName,
		out JProperty property,
		out string error)
	{
		property = null;
		error = string.Empty;
		if (source == null)
		{
			return true;
		}
		foreach (JProperty candidate in source.Properties())
		{
			if (!string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (property != null)
			{
				error = "JSON 字段大小写重复: " + propertyName;
				return false;
			}
			property = candidate;
		}
		return true;
	}

	private static bool TryBuildCaseInsensitivePropertyMap(
		JObject source,
		out Dictionary<string, JProperty> properties,
		out string error)
	{
		properties = new Dictionary<string, JProperty>(StringComparer.OrdinalIgnoreCase);
		error = string.Empty;
		if (source == null)
		{
			error = "JSON 对象不能为空";
			return false;
		}
		foreach (JProperty property in source.Properties())
		{
			if (properties.ContainsKey(property.Name))
			{
				error = "JSON 字段大小写重复: " + property.Name;
				return false;
			}
			properties.Add(property.Name, property);
		}
		return true;
	}

	private static bool TryReadRequiredString(
		IReadOnlyDictionary<string, JProperty> properties,
		string propertyName,
		out string value,
		out string error)
	{
		value = string.Empty;
		error = string.Empty;
		if (!properties.TryGetValue(propertyName, out JProperty property)
			|| property.Value.Type != JTokenType.String
			|| string.IsNullOrWhiteSpace(property.Value.Value<string>()))
		{
			error = "JSON 字段必须是非空字符串: " + propertyName;
			return false;
		}
		value = property.Value.Value<string>().Trim();
		return true;
	}

	private static bool TryReadOptionalString(
		IReadOnlyDictionary<string, JProperty> properties,
		string propertyName,
		out string value,
		out string error)
	{
		value = string.Empty;
		error = string.Empty;
		if (!properties.TryGetValue(propertyName, out JProperty property)
			|| property.Value.Type == JTokenType.Null)
		{
			return true;
		}
		if (property.Value.Type != JTokenType.String)
		{
			error = "JSON 字段必须是字符串: " + propertyName;
			return false;
		}
		value = (property.Value.Value<string>() ?? string.Empty).Trim();
		return true;
	}

	private static bool TryReadOptionalReason(
		IReadOnlyDictionary<string, JProperty> properties,
		out string reason,
		out string error)
	{
		return TryReadOptionalString(properties, "reason", out reason, out error);
	}

	private static bool HasNonNullProperty(
		IReadOnlyDictionary<string, JProperty> properties,
		string propertyName)
	{
		return properties.TryGetValue(propertyName, out JProperty property)
			&& property.Value.Type != JTokenType.Null;
	}

	private static bool IsAllowedDynamicWireProperty(string propertyName)
	{
		return LegacyPolicyEffectFieldAdapter.TryResolveModuleId(propertyName, out _)
			|| string.Equals(propertyName, "moduleId", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetHandles", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "payload", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targets", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "changes", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetHandle", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetScope", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetKingdomId", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetKingdomName", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "durationDays", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "reason", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryReadFiniteNumber(JToken token, out double value)
	{
		value = 0d;
		if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
		{
			return false;
		}
		try
		{
			value = token.Value<double>();
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}
		catch
		{
			return false;
		}
	}

	private static bool ContainsNonFiniteNumber(JToken token)
	{
		if (token == null)
		{
			return false;
		}
		if (token.Type == JTokenType.Float)
		{
			return !TryReadFiniteNumber(token, out _);
		}
		return token.Children().Any(ContainsNonFiniteNumber);
	}

	private delegate bool JObjectNormalizer(
		JObject source,
		out JObject normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error);

	private static bool TryNormalizeTyped<T>(
		T source,
		JObjectNormalizer normalizer,
		out T normalized,
		out PolicyEffectMigrationBatchSummary summary,
		out string error)
		where T : class
	{
		normalized = null;
		summary = null;
		error = string.Empty;
		if (source == null)
		{
			error = "save record 不能为空";
			return false;
		}
		try
		{
			JsonSerializer serializer = JsonSerializer.Create(SafeSettings);
			JObject raw = JObject.FromObject(source, serializer);
			if (!normalizer(raw, out JObject normalizedObject, out summary, out error))
			{
				return false;
			}
			normalized = normalizedObject.ToObject<T>(serializer);
			if (normalized == null)
			{
				error = "save record 规范化结果无法反序列化";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "save record 规范化失败: " + ex.Message;
			return false;
		}
	}

	private static bool TryAppendLegacyInstances(
		JObject effectSource,
		JObject recordSource,
		int sourceVersion,
		JArray moduleEffects,
		JArray receipts,
		PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		error = string.Empty;
		if (!LegacyPolicyEffectFieldAdapter.TryReadLegacyFields(effectSource, out IReadOnlyDictionary<string, float> values, out error))
		{
			return false;
		}
		if (values.Count == 0 && !ReferenceEquals(effectSource, recordSource)
			&& !LegacyPolicyEffectFieldAdapter.TryReadLegacyFields(recordSource, out values, out error))
		{
			return false;
		}
		PolicyEffectCanonicalTargetSet targetSet = BuildTargetSet(effectSource, recordSource);
		string policyId = FirstNonEmpty(ReadString(recordSource, "RecordId"), ReadString(recordSource, "PolicyObjectId"));
		string effectId = FirstNonEmpty(
			ReadString(effectSource, "EffectId"),
			ReadString(effectSource, "ActiveEffectId"),
			ReadString(recordSource, "EffectId"),
			policyId,
			"legacy");
		int startDay = ReadInt(recordSource, "SubmittedDay", 0);
		int duration = ReadInt(effectSource, "TotalDurationDays", ReadInt(recordSource, "OriginalDurationDays", 0));
		int remainingDays = ReadInt(effectSource, "RemainingDays", ReadInt(recordSource, "RemainingDays", duration));
		int lastAppliedDay = ReadInt(effectSource, "LastAppliedDay", 0);
		string scope = NormalizeSourceScope(
			FirstNonEmpty(ReadString(recordSource, "ScopeKind"), PolicyEffectScopes.Kingdom));
		string reason = FirstNonEmpty(ReadString(effectSource, "Reason"), ReadString(recordSource, "EffectReason"));
		bool ended = ReadBool(effectSource, "Ended")
			|| ReadBool(effectSource, "IsEnded")
			|| string.Equals(ReadString(recordSource, "Status"), "expired", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(ReadString(recordSource, "Status"), "abolished", StringComparison.OrdinalIgnoreCase);
		JsonSerializer serializer = JsonSerializer.Create(SafeSettings);
		foreach (KeyValuePair<string, float> pair in values)
		{
			int payloadVersion = PolicyEffectModuleCatalog.TryGet(pair.Key, out IPolicyEffectModule module)
				? module.Descriptor.PayloadSchemaVersion
				: 1;
			JObject payload = new JObject
			{
				["moduleId"] = pair.Key,
				["schemaVersion"] = payloadVersion,
				["value"] = pair.Value
			};
			string instanceId = effectId + ":" + pair.Key;
			bool assumedCompleted = string.Equals(pair.Key, "kingdomStabilityOnce", StringComparison.Ordinal);
			PolicyEffectExecutionReceipt receipt = assumedCompleted
				? CreateLegacyAssumedCompletedReceipt(instanceId, policyId, targetSet, payload, pair.Value, lastAppliedDay > 0 ? lastAppliedDay : startDay)
				: null;
			PolicyEffectInstanceSaveData saveData = new PolicyEffectInstanceSaveData
			{
				EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
				MechanismId = PolicyEffectPlanDefaults.BuildIndependentMechanismId(policyId),
				MechanismKind = PolicyEffectMechanismKind.Independent,
				MechanismRole = PolicyEffectMechanismRole.Subject,
				InstanceId = instanceId,
				PolicyId = policyId,
				ModuleId = pair.Key,
				SourceModuleId = pair.Key,
				PayloadSchemaVersion = payloadVersion,
				Payload = payload,
				TargetSet = targetSet,
				LifecycleState = assumedCompleted || ended ? PolicyEffectLifecycleState.Completed : PolicyEffectLifecycleState.Active,
				StateSchemaVersion = 1,
				RuntimeState = new JObject
				{
					["legacySourceVersion"] = sourceVersion,
					["remainingDays"] = remainingDays,
					["lastAppliedDay"] = lastAppliedDay,
					["totalDurationDays"] = duration
				},
				ExecutionReceipt = receipt,
				StartDay = startDay,
				EndDay = duration > 0 ? startDay + duration : 0,
				SourceScope = scope,
				Reason = reason
			};
			moduleEffects.Add(JObject.FromObject(saveData, serializer));
			if (receipt != null)
			{
				receipts.Add(JObject.FromObject(receipt, serializer));
				summary.LegacyAssumedCompletedReceipts++;
			}
			summary.InstancesCreated++;
			summary.LegacyFieldsMigrated++;
		}
		return true;
	}

	private static bool TryEnsureLegacyActiveStabilityReceipt(
		JObject source,
		JArray moduleEffects,
		JArray receipts,
		PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		error = string.Empty;
		if (source == null || moduleEffects == null || receipts == null || summary == null)
		{
			error = "legacy active stability migration 输入不能为空";
			return false;
		}
		if (moduleEffects.OfType<JObject>().Any(instance =>
			string.Equals(ReadString(instance, "ModuleId"), "kingdomStabilityOnce", StringComparison.Ordinal)))
		{
			return true;
		}
		if (!PolicyEffectModuleCatalog.TryGet("kingdomStabilityOnce", out IPolicyEffectModule module))
		{
			error = "legacy active stability 模块未注册";
			return false;
		}
		PolicyEffectCanonicalTargetSet targetSet = BuildTargetSet(source, source);
		string policyId = FirstNonEmpty(ReadString(source, "RecordId"), ReadString(source, "PolicyObjectId"));
		string effectId = FirstNonEmpty(ReadString(source, "EffectId"), policyId, "legacy");
		string instanceId = effectId + ":kingdomStabilityOnce";
		int startDay = ReadInt(source, "SubmittedDay", 0);
		int duration = ReadInt(source, "TotalDurationDays", 0);
		int remainingDays = ReadInt(source, "RemainingDays", duration);
		int lastAppliedDay = ReadInt(source, "LastAppliedDay", startDay);
		string sourceScope = NormalizeSourceScope(
			FirstNonEmpty(ReadString(source, "ScopeKind"), PolicyEffectScopes.Kingdom));
		if (!PolicyEffectModuleCatalog.IsAllowedForScope(module, sourceScope))
		{
			// This synthetic completed marker exists only to suppress replay of the
			// removed legacy stability field. Keep it catalog-valid even when an old
			// local Active shell never had a legal stability scope.
			sourceScope = PolicyEffectScopes.Kingdom;
		}
		JObject payload = new JObject
		{
			["moduleId"] = "kingdomStabilityOnce",
			["schemaVersion"] = module.Descriptor.PayloadSchemaVersion,
			["value"] = 0f
		};
		PolicyEffectExecutionReceipt receipt = CreateLegacyAssumedCompletedReceipt(
			instanceId,
			policyId,
			targetSet,
			payload,
			0f,
			lastAppliedDay);
		PolicyEffectInstanceSaveData saveData = new PolicyEffectInstanceSaveData
		{
			EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
			MechanismId = PolicyEffectPlanDefaults.BuildIndependentMechanismId(policyId),
			MechanismKind = PolicyEffectMechanismKind.Independent,
			MechanismRole = PolicyEffectMechanismRole.Subject,
			InstanceId = instanceId,
			PolicyId = policyId,
			ModuleId = "kingdomStabilityOnce",
			SourceModuleId = "kingdomStabilityOnce",
			PayloadSchemaVersion = module.Descriptor.PayloadSchemaVersion,
			Payload = payload,
			TargetSet = targetSet,
			LifecycleState = PolicyEffectLifecycleState.Completed,
			StateSchemaVersion = 1,
			RuntimeState = new JObject
			{
				["legacySourceVersion"] = 4,
				["remainingDays"] = remainingDays,
				["lastAppliedDay"] = lastAppliedDay,
				["totalDurationDays"] = duration,
				["legacyAssumedCompleted"] = true
			},
			ExecutionReceipt = receipt,
			StartDay = startDay,
			EndDay = duration > 0 ? startDay + duration : 0,
			SourceScope = sourceScope,
			Reason = ReadString(source, "Reason")
		};
		JsonSerializer serializer = JsonSerializer.Create(SafeSettings);
		moduleEffects.Add(JObject.FromObject(saveData, serializer));
		receipts.Add(JObject.FromObject(receipt, serializer));
		summary.InstancesCreated++;
		summary.LegacyAssumedCompletedReceipts++;
		return true;
	}

	private static PolicyEffectExecutionReceipt CreateLegacyAssumedCompletedReceipt(
		string instanceId,
		string policyId,
		PolicyEffectCanonicalTargetSet targetSet,
		JObject payload,
		float value,
		int campaignDay)
	{
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = "legacyAssumedCompleted:" + instanceId,
			InstanceId = instanceId,
			PolicyId = policyId,
			ModuleId = "kingdomStabilityOnce",
			TargetSet = targetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = value,
			AppliedValue = value,
			RequestedPayload = payload.DeepClone(),
			AppliedPayload = payload.DeepClone(),
			CampaignDay = campaignDay,
			Message = "legacyAssumedCompleted"
		};
	}

	private static PolicyEffectCanonicalTargetSet BuildTargetSet(JObject effectSource, JObject recordSource)
	{
		PolicyEffectCanonicalTargetSet result = new PolicyEffectCanonicalTargetSet();
		AddUnique(result.SelectorHandles, ReadString(effectSource, "TargetHandle"));
		AddUnique(result.SelectorHandles, ReadString(recordSource, "TargetHandle"));
		AddUniqueRange(result.SettlementIds, ReadStringArray(effectSource, "TargetSettlementIds"));
		AddUniqueRange(result.SettlementIds, ReadStringArray(effectSource, "DirectTargetSettlementIds"));
		AddUniqueRange(result.SettlementIds, ReadStringArray(effectSource, "TargetFiefIds"));
		AddUniqueRange(result.SettlementIds, ReadStringArray(recordSource, "TargetSettlementIds"));
		AddUniqueRange(result.SettlementIds, ReadStringArray(recordSource, "DirectTargetSettlementIds"));
		AddUniqueRange(result.SettlementIds, ReadStringArray(recordSource, "TargetFiefIds"));
		AddUniqueRange(result.ParentSettlementIds, ReadStringArray(effectSource, "TargetFiefIds"));
		AddUniqueRange(result.ParentSettlementIds, ReadStringArray(recordSource, "TargetFiefIds"));
		AddUniqueRange(result.ClanIds, ReadStringArray(effectSource, "TargetClanIds"));
		AddUniqueRange(result.ClanIds, ReadStringArray(recordSource, "TargetClanIds"));
		AddUnique(result.KingdomIds, ReadString(effectSource, "TargetKingdomId"));
		AddUnique(result.KingdomIds, ReadString(recordSource, "TargetKingdomId"));
		AddUnique(result.KingdomIds, ReadString(effectSource, "KingdomId"));
		result.FollowCurrentRulingClan = ReadBool(effectSource, "FollowCurrentRulingClan")
			|| ReadBool(recordSource, "FollowCurrentRulingClan");
		// Legacy records did not distinguish Town/Village ids. Mirroring the expanded
		// settlement ids into both typed lists is safe because every runtime hook also
		// queries by its concrete target kind and concrete current object id.
		AddUniqueRange(result.TownIds, result.SettlementIds);
		AddUniqueRange(result.VillageIds, result.SettlementIds);
		return result;
	}

	private static bool TryCanonicalizeActiveModuleEffects(
		JObject root,
		out bool changed,
		out string error)
	{
		changed = false;
		error = string.Empty;
		if (root == null)
		{
			error = "active policy effect save cannot be null";
			return false;
		}
		JProperty moduleEffectsProperty = root.Properties().FirstOrDefault(property =>
			string.Equals(property.Name, "ModuleEffects", StringComparison.OrdinalIgnoreCase));
		if (moduleEffectsProperty == null)
		{
			moduleEffectsProperty = new JProperty("ModuleEffects", new JArray());
			root.Add(moduleEffectsProperty);
			changed = true;
		}
		if (!(moduleEffectsProperty.Value is JArray moduleEffects))
		{
			error = "active policy effect ModuleEffects must be an array";
			return false;
		}
		if (!TryReadObjectArray(moduleEffects, "ModuleEffects", out List<JObject> shellObjects, out error)
			|| !TryCoalesceAuthoritativeShellInstances(
				shellObjects,
				out List<PolicyEffectInstanceSaveData> logicalInstances,
				out error))
		{
			return false;
		}

		JProperty receiptsProperty = root.Properties().FirstOrDefault(property =>
			string.Equals(property.Name, "ExecutionReceipts", StringComparison.OrdinalIgnoreCase));
		if (receiptsProperty == null)
		{
			receiptsProperty = new JProperty("ExecutionReceipts", new JArray());
			root.Add(receiptsProperty);
			changed = true;
		}
		if (!(receiptsProperty.Value is JArray receiptArray))
		{
			error = "active policy effect ExecutionReceipts must be an array";
			return false;
		}
		if (!TryReadObjectArray(receiptArray, "ExecutionReceipts", out List<JObject> receiptObjects, out error)
			|| !TryCoalesceAuthoritativeReceipts(
				receiptObjects,
				out List<PolicyEffectExecutionReceipt> logicalReceipts,
				out error)
			|| !TryValidateLogicalCollections(logicalInstances, logicalReceipts, out _, out error))
		{
			return false;
		}

		JsonSerializer serializer = JsonSerializer.Create(SafeSettings);
		JArray canonicalModuleEffects = new JArray(logicalInstances.Select(instance =>
			JObject.FromObject(instance, serializer)));
		JArray canonicalReceipts = new JArray(logicalReceipts.Select(receipt =>
			JObject.FromObject(receipt, serializer)));
		if (!JToken.DeepEquals(moduleEffects, canonicalModuleEffects))
		{
			moduleEffectsProperty.Value = canonicalModuleEffects;
			changed = true;
		}
		if (!JToken.DeepEquals(receiptArray, canonicalReceipts))
		{
			receiptsProperty.Value = canonicalReceipts;
			changed = true;
		}
		return true;
	}

	private static bool TryUpgradeEnvelopeSchema(PolicyEffectSaveEnvelope envelope, out string error)
	{
		error = string.Empty;
		if (envelope == null)
		{
			error = "policy effect save envelope cannot be null";
			return false;
		}
		if (envelope.SchemaVersion == 1)
		{
			foreach (PolicyEffectInstanceSaveData instance in envelope.Instances
				?? new List<PolicyEffectInstanceSaveData>())
			{
				NormalizeLegacyEffectPlan(instance);
			}
			envelope.SchemaVersion = PolicyEffectDataVersions.SaveSchemaVersion;
			return true;
		}
		if (envelope.SchemaVersion != PolicyEffectDataVersions.SaveSchemaVersion)
		{
			error = "unsupported policy effect save schemaVersion: " + envelope.SchemaVersion;
			return false;
		}
		return true;
	}

	private static void NormalizeLegacyEffectPlan(PolicyEffectInstanceSaveData instance)
	{
		if (instance == null || instance.EffectPlanVersion != 0)
		{
			return;
		}
		instance.EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion;
		instance.MechanismId = PolicyEffectPlanDefaults.BuildIndependentMechanismId(
			string.IsNullOrWhiteSpace(instance.PolicyId) ? instance.InstanceId : instance.PolicyId);
		instance.MechanismKind = PolicyEffectMechanismKind.Independent;
		instance.MechanismRole = PolicyEffectMechanismRole.Subject;
		instance.SourceOmitted = false;
		instance.DestinationOmitted = false;
	}

	private static bool TryValidateEffectPlanInstance(
		PolicyEffectInstanceSaveData instance,
		IPolicyEffectModule module,
		out string error)
	{
		error = string.Empty;
		if (instance == null || instance.EffectPlanVersion != PolicyEffectPlanVersions.CurrentVersion)
		{
			error = "unsupported effectPlanVersion: " + (instance?.EffectPlanVersion ?? 0);
			return false;
		}
		string mechanismId = (instance.MechanismId ?? string.Empty).Trim();
		if (mechanismId.Length == 0
			|| mechanismId.Length > PolicyEffectPlanVersions.MaximumMechanismIdLength
			|| mechanismId.Any(character => !char.IsLetterOrDigit(character) && character != '_' && character != '-'))
		{
			error = "invalid mechanismId";
			return false;
		}
		instance.MechanismId = mechanismId;
		if (!Enum.IsDefined(typeof(PolicyEffectMechanismKind), instance.MechanismKind)
			|| !Enum.IsDefined(typeof(PolicyEffectMechanismRole), instance.MechanismRole))
		{
			error = "unknown mechanism enum";
			return false;
		}
		if (instance.MechanismKind == PolicyEffectMechanismKind.Independent)
		{
			if (instance.MechanismRole != PolicyEffectMechanismRole.Subject
				|| instance.SourceOmitted
				|| instance.DestinationOmitted)
			{
				error = "independent mechanism must be a subject without omissions";
				return false;
			}
			return true;
		}
		if (instance.MechanismRole == PolicyEffectMechanismRole.Subject
			|| (instance.SourceOmitted && instance.DestinationOmitted))
		{
			error = "linked mechanism has an invalid role or omits both sides";
			return false;
		}
		if (module?.Descriptor?.ExecutionKind == PolicyEffectExecutionKind.OneShot)
		{
			error = "OneShot effect cannot participate in a linked mechanism";
			return false;
		}
		return true;
	}

	private static bool TryValidateEffectPlanGroups(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		out string error)
	{
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> current = (instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance?.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion)
			.ToList();
		List<IGrouping<string, PolicyEffectInstanceSaveData>> groups = current
			.GroupBy(instance => (instance.PolicyId ?? string.Empty) + "\u001f" + (instance.MechanismId ?? string.Empty).Trim(),
				StringComparer.Ordinal)
			.ToList();
		if (groups.GroupBy(group => group.First().PolicyId ?? string.Empty, StringComparer.Ordinal)
			.Any(policy => policy.Count() > PolicyEffectPlanVersions.MaximumMechanisms))
		{
			error = "EffectPlan mechanism count exceeds " + PolicyEffectPlanVersions.MaximumMechanisms + ".";
			return false;
		}
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> group in groups)
		{
			PolicyEffectInstanceSaveData first = group.First();
			string mechanismId = (first.MechanismId ?? string.Empty).Trim();
			if (first.MechanismKind == PolicyEffectMechanismKind.Independent)
			{
				continue;
			}
			if (group.Any(instance => instance.MechanismKind != first.MechanismKind
				|| instance.SourceOmitted != first.SourceOmitted
				|| instance.DestinationOmitted != first.DestinationOmitted))
			{
				error = "linked mechanism has conflicting metadata: " + mechanismId;
				return false;
			}
			bool hasSource = group.Any(instance => IsSourceRole(instance.MechanismRole));
			bool hasDestination = group.Any(instance => IsDestinationRole(instance.MechanismRole));
			if ((first.SourceOmitted && hasSource) || (!first.SourceOmitted && !hasSource)
				|| (first.DestinationOmitted && hasDestination) || (!first.DestinationOmitted && !hasDestination))
			{
				error = "linked mechanism has an undeclared missing or contradictory side: " + mechanismId;
				return false;
			}
			PolicyEffectInstanceSaveData[] legs = group.ToArray();
			bool groupIsRuntimeSuspended = legs.All(instance =>
				instance.LifecycleState == PolicyEffectLifecycleState.Suspended);
			for (int leftIndex = 0; leftIndex < legs.Length; leftIndex++)
			{
				for (int rightIndex = leftIndex + 1; rightIndex < legs.Length; rightIndex++)
				{
					PolicyEffectInstanceSaveData left = legs[leftIndex];
					PolicyEffectInstanceSaveData right = legs[rightIndex];
					if (string.Equals(left.ModuleId, right.ModuleId, StringComparison.Ordinal)
						&& IsSourceRole(left.MechanismRole) != IsSourceRole(right.MechanismRole)
						&& !groupIsRuntimeSuspended
						&& HaveCanonicalTargetOverlap(left.TargetSet, right.TargetSet))
					{
						error = "linked mechanism source and destination overlap for module " + left.ModuleId + ": " + mechanismId;
						return false;
					}
				}
			}
		}
		return true;
	}

	private static bool IsSourceRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Source || role == PolicyEffectMechanismRole.Cost;
	}

	private static bool IsDestinationRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Destination || role == PolicyEffectMechanismRole.Beneficiary;
	}

	private static bool HaveCanonicalTargetOverlap(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		IEnumerable<string> leftValues = (left?.SettlementIds ?? new List<string>())
			.Concat(left?.TownIds ?? new List<string>())
			.Concat(left?.VillageIds ?? new List<string>())
			.Concat(left?.ClanIds ?? new List<string>())
			.Concat(left?.KingdomIds ?? new List<string>())
			.Concat(left?.HeroIds ?? new List<string>());
		HashSet<string> rightValues = new HashSet<string>((right?.SettlementIds ?? new List<string>())
			.Concat(right?.TownIds ?? new List<string>())
			.Concat(right?.VillageIds ?? new List<string>())
			.Concat(right?.ClanIds ?? new List<string>())
			.Concat(right?.KingdomIds ?? new List<string>())
			.Concat(right?.HeroIds ?? new List<string>()), StringComparer.OrdinalIgnoreCase);
		if (leftValues.Any(rightValues.Contains))
		{
			return true;
		}
		HashSet<string> rightPlans = new HashSet<string>(
			PolicyTargetPlanResolver.NormalizePlans(right?.TargetPlans).Select(plan => plan.NormalizedSignature),
			StringComparer.Ordinal);
		return PolicyTargetPlanResolver.NormalizePlans(left?.TargetPlans)
			.Any(plan => rightPlans.Contains(plan.NormalizedSignature));
	}

	private static bool TryValidateEnvelope(PolicyEffectSaveEnvelope envelope, out string error)
	{
		if (envelope == null)
		{
			error = "policy effect save envelope 不能为空";
			return false;
		}
		if (envelope.SchemaVersion != PolicyEffectDataVersions.SaveSchemaVersion)
		{
			error = "不支持的 policy effect save schemaVersion: " + envelope.SchemaVersion;
			return false;
		}
		if (envelope.Instances == null)
		{
			error = "policy effect instances 不能为空";
			return false;
		}
		if (envelope.Instances.Count > MaxInstancesPerPolicy)
		{
			error = "单政策 policy effect instance 超过 24 个";
			return false;
		}
		if (envelope.Receipts == null)
		{
			error = "policy effect receipts cannot be null";
			return false;
		}
		if (!TryValidateLogicalCollections(envelope.Instances, envelope.Receipts, out _, out error))
		{
			return false;
		}
		if (!TryValidateEffectPlanGroups(envelope.Instances, out error))
		{
			return false;
		}
		int totalPayloadBytes = 0;
		foreach (PolicyEffectInstanceSaveData instance in envelope.Instances)
		{
			if (!TryValidateInstanceShape(instance, out error))
			{
				return false;
			}
			if (instance.EffectPlanVersion == PolicyEffectPlanVersions.CurrentVersion)
			{
				PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule planModule);
				if (!TryValidateEffectPlanInstance(instance, planModule, out error))
				{
					return false;
				}
			}
			totalPayloadBytes += Utf8Size(instance.Payload.ToString(Formatting.None));
			if (totalPayloadBytes > MaxTotalPayloadBytes)
			{
				error = "单政策 policy effect payload 总量超过 32KiB";
				return false;
			}
		}
		error = string.Empty;
		return true;
	}

	private static bool TryValidateInstanceShape(PolicyEffectInstanceSaveData instance, out string error)
	{
		if (instance == null
			|| string.IsNullOrWhiteSpace(instance.InstanceId)
			|| string.IsNullOrWhiteSpace(instance.ModuleId)
			|| instance.PayloadSchemaVersion <= 0
			|| instance.Payload == null
			|| instance.Payload.Type == JTokenType.Null
			|| instance.TargetSet == null)
		{
			error = "policy effect instance 结构不完整";
			return false;
		}
		instance.InstanceId = instance.InstanceId.Trim();
		instance.ModuleId = instance.ModuleId.Trim();
		instance.ActorHeroId = (instance.ActorHeroId ?? string.Empty).Trim();
		List<PolicyTargetPlanSaveData> rawTargetPlans = (instance.TargetSet.TargetPlans
			?? new List<PolicyTargetPlanSaveData>()).Where(plan => plan != null).ToList();
		instance.TargetSet = NormalizeCanonicalTargetSet(instance.TargetSet);
		if (rawTargetPlans.Count != instance.TargetSet.TargetPlans.Count)
		{
			// Unknown/future plan versions must survive the structural read so a known
			// module can be preserved inert instead of silently losing or executing them.
			instance.TargetSet.TargetPlans = rawTargetPlans;
		}
		if (!Enum.IsDefined(typeof(PolicyEffectLifecycleState), instance.LifecycleState))
		{
			error = "policy effect instance has an invalid lifecycle state: " + instance.InstanceId;
			return false;
		}
		if (!IsFinite(instance.StartDay) || !IsFinite(instance.EndDay))
		{
			error = "policy effect instance startDay/endDay must be finite: " + instance.InstanceId;
			return false;
		}
		if (instance.RuntimeState != null
			&& Utf8Size(instance.RuntimeState.ToString(Formatting.None)) > MaxRuntimeStateBytes)
		{
			error = "policy effect runtimeState exceeds 512KiB: " + instance.InstanceId;
			return false;
		}
		if (instance.ExecutionReceipt != null
			&& (!TryValidateReceiptShape(instance.ExecutionReceipt, out error)
				|| !TryValidateReceiptForInstance(instance.ExecutionReceipt, instance, out error)))
		{
			return false;
		}
		if (Utf8Size(instance.Payload.ToString(Formatting.None)) > MaxPayloadBytes)
		{
			error = "policy effect payload 超过 4KiB: " + instance.InstanceId;
			return false;
		}
		if (ContainsTypeMetadata(instance.Payload) || ContainsTypeMetadata(instance.RuntimeState))
		{
			error = "policy effect payload/runtimeState 不得包含 $type 元数据: " + instance.InstanceId;
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryNormalizeSavedTargetPlans(
		PolicyEffectCanonicalTargetSet targetSet,
		out string error)
	{
		error = string.Empty;
		List<PolicyTargetPlanSaveData> sourcePlans = (targetSet?.TargetPlans
			?? new List<PolicyTargetPlanSaveData>()).Where(plan => plan != null).ToList();
		List<PolicyTargetPlanSaveData> normalizedPlans = new List<PolicyTargetPlanSaveData>(sourcePlans.Count);
		foreach (PolicyTargetPlanSaveData sourcePlan in sourcePlans)
		{
			if (!PolicyTargetPlanResolver.TryNormalizeAndValidate(
				sourcePlan,
				out PolicyTargetPlanSaveData normalizedPlan,
				out error))
			{
				return false;
			}
			normalizedPlans.Add(normalizedPlan);
		}
		if (targetSet != null)
		{
			targetSet.TargetPlans = PolicyTargetPlanResolver.NormalizePlans(normalizedPlans);
		}
		return true;
	}

	private static bool TryValidateEmbeddedInstances(
		JObject root,
		PolicyEffectMigrationBatchSummary summary,
		out string error)
	{
		error = string.Empty;
		if (root == null || ContainsTypeMetadata(root))
		{
			error = "policy effect save record must not be null or contain $type metadata";
			return false;
		}
		List<JObject> containers = EnumerateAuthoritativeEffectContainers(root).ToList();
		if (!TryReadAuthoritativeObjects(
			containers,
			"ModuleEffects",
			out List<JObject> shellInstances,
			out error))
		{
			return false;
		}
		if (!TryCoalesceAuthoritativeShellInstances(
			shellInstances,
			out List<PolicyEffectInstanceSaveData> instances,
			out error))
		{
			return false;
		}
		if (!TryReadAuthoritativeObjects(
			containers,
			"ExecutionReceipts",
			out List<JObject> receiptObjects,
			out error)
			|| !TryCoalesceAuthoritativeReceipts(
				receiptObjects,
				out List<PolicyEffectExecutionReceipt> receipts,
				out error)
			|| !TryValidateLogicalCollections(instances, receipts, out _, out error))
		{
			return false;
		}
		if (!TryValidateEffectPlanGroups(instances, out error))
		{
			return false;
		}
		if (instances.Count > MaxInstancesPerPolicy)
		{
			error = "单政策迁移后的 policy effect instance 超过 24 个";
			return false;
		}
		int totalPayloadBytes = 0;
		foreach (PolicyEffectInstanceSaveData instance in instances)
		{
			JToken payload = instance.Payload;
			JToken runtimeState = instance.RuntimeState;
			if (payload == null || Utf8Size(payload.ToString(Formatting.None)) > MaxPayloadBytes)
			{
				error = "迁移后的 policy effect payload 缺失或超过 4KiB";
				return false;
			}
			if (ContainsTypeMetadata(payload) || ContainsTypeMetadata(runtimeState))
			{
				error = "迁移后的 policy effect payload/runtimeState 不得包含 $type 元数据";
				return false;
			}
			totalPayloadBytes += Utf8Size(payload.ToString(Formatting.None));
			if (totalPayloadBytes > MaxTotalPayloadBytes)
			{
				error = "单政策迁移后的 policy effect payload 总量超过 32KiB";
				return false;
			}
			string moduleId = instance.ModuleId;
			if (PolicyEffectModuleCatalog.TryGet(moduleId, out IPolicyEffectModule module)
				&& TryValidateEffectPlanInstance(instance, module, out _))
			{
				summary.ExecutableInstances++;
			}
			else
			{
				summary.InertInstances++;
				summary.Warnings.Add((module == null ? "unknownModule" : "invalidEffectPlan")
					+ " preserved inert: " + (moduleId ?? string.Empty));
			}
		}
		return true;
	}

	private static bool TryCoalesceAuthoritativeShellInstances(
		IEnumerable<JObject> shellInstances,
		out List<PolicyEffectInstanceSaveData> logicalInstances,
		out string error)
	{
		logicalInstances = new List<PolicyEffectInstanceSaveData>();
		error = string.Empty;
		Dictionary<string, PolicyEffectInstanceSaveData> byInstanceId =
			new Dictionary<string, PolicyEffectInstanceSaveData>(StringComparer.Ordinal);
		JsonSerializer serializer = JsonSerializer.Create(SafeSettings);
		foreach (JObject shellObject in shellInstances ?? Enumerable.Empty<JObject>())
		{
			PolicyEffectInstanceSaveData shell;
			try
			{
				shell = shellObject?.ToObject<PolicyEffectInstanceSaveData>(serializer);
			}
			catch (Exception ex)
			{
				error = "迁移后的 policy effect 目标壳无法反序列化: " + ex.Message;
				return false;
			}
			if (!TryValidateInstanceShape(shell, out error))
			{
				return false;
			}
			NormalizeLegacyEffectPlan(shell);
			if (PolicyEffectModuleCatalog.TryGet(shell.ModuleId, out IPolicyEffectModule module)
				&& !TryValidateKnownModuleScope(shell, module, out error))
			{
				return false;
			}
			string instanceId = (shell.InstanceId ?? string.Empty).Trim();
			shell.InstanceId = instanceId;
			if (!byInstanceId.TryGetValue(instanceId, out PolicyEffectInstanceSaveData aggregate))
			{
				shell.TargetSet = NormalizeCanonicalTargetSet(shell.TargetSet);
				if (shell.ExecutionReceipt != null)
				{
					shell.ExecutionReceipt.TargetSet = NormalizeCanonicalTargetSet(
						shell.ExecutionReceipt.TargetSet);
				}
				byInstanceId.Add(instanceId, shell);
				logicalInstances.Add(shell);
				continue;
			}
			if (!AreCompatibleAuthoritativeShellInstances(aggregate, shell))
			{
				error = "同一 policy effect instanceId 的目标壳静态数据不一致: " + instanceId;
				return false;
			}
			aggregate.ActorHeroId = FirstNonEmpty(aggregate.ActorHeroId, shell.ActorHeroId);
			aggregate.TargetSet = MergeCanonicalTargetSets(aggregate.TargetSet, shell.TargetSet);
			if (aggregate.ExecutionReceipt != null)
			{
				aggregate.ExecutionReceipt.TargetSet = MergeCanonicalTargetSets(
					aggregate.ExecutionReceipt.TargetSet,
					shell.ExecutionReceipt?.TargetSet);
			}
		}
		return true;
	}

	private static bool AreCompatibleAuthoritativeShellInstances(
		PolicyEffectInstanceSaveData left,
		PolicyEffectInstanceSaveData right)
	{
		// Keep this static-field contract aligned with Core's cold-path shell
		// coalescer. Instance/receipt TargetSet is intentionally shell-specific.
		return left != null
			&& right != null
			&& left.EffectPlanVersion == right.EffectPlanVersion
			&& string.Equals(left.MechanismId ?? string.Empty, right.MechanismId ?? string.Empty, StringComparison.Ordinal)
			&& left.MechanismKind == right.MechanismKind
			&& left.MechanismRole == right.MechanismRole
			&& left.SourceOmitted == right.SourceOmitted
			&& left.DestinationOmitted == right.DestinationOmitted
			&& string.Equals(left.InstanceId ?? string.Empty, right.InstanceId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.PolicyId ?? string.Empty, right.PolicyId ?? string.Empty, StringComparison.Ordinal)
			&& AreCompatibleAuthoritativeActorHeroIds(left.ActorHeroId, right.ActorHeroId)
			&& AreModuleIdsEquivalent(left.ModuleId, right.ModuleId)
			&& AreModuleIdsEquivalent(
				FirstNonEmpty(left.SourceModuleId, left.ModuleId),
				FirstNonEmpty(right.SourceModuleId, right.ModuleId))
			&& left.PayloadSchemaVersion == right.PayloadSchemaVersion
			&& PolicyEffectTokensEqual(left.Payload, right.Payload)
			&& left.LifecycleState == right.LifecycleState
			&& left.StateSchemaVersion == right.StateSchemaVersion
			&& PolicyEffectTokensEqual(left.RuntimeState, right.RuntimeState)
			&& AreCompatibleAuthoritativeShellReceipts(left.ExecutionReceipt, right.ExecutionReceipt)
			&& left.StartDay.Equals(right.StartDay)
			&& left.EndDay.Equals(right.EndDay)
			&& string.Equals(left.SourceScope ?? string.Empty, right.SourceScope ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.Reason ?? string.Empty, right.Reason ?? string.Empty, StringComparison.Ordinal);
	}

	private static bool AreCompatibleAuthoritativeActorHeroIds(string left, string right)
	{
		string normalizedLeft = (left ?? string.Empty).Trim();
		string normalizedRight = (right ?? string.Empty).Trim();
		return normalizedLeft.Length == 0
			|| normalizedRight.Length == 0
			|| string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
	}

	private static bool AreCompatibleAuthoritativeShellReceipts(
		PolicyEffectExecutionReceipt left,
		PolicyEffectExecutionReceipt right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return string.Equals(left.ReceiptId ?? string.Empty, right.ReceiptId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.InstanceId ?? string.Empty, right.InstanceId ?? string.Empty, StringComparison.Ordinal)
			&& string.Equals(left.PolicyId ?? string.Empty, right.PolicyId ?? string.Empty, StringComparison.Ordinal)
			&& AreModuleIdsEquivalent(left.ModuleId, right.ModuleId)
			&& left.Status == right.Status
			&& left.RequestedValue.Equals(right.RequestedValue)
			&& left.AppliedValue.Equals(right.AppliedValue)
			&& PolicyEffectTokensEqual(left.RequestedPayload, right.RequestedPayload)
			&& PolicyEffectTokensEqual(left.AppliedPayload, right.AppliedPayload)
			&& left.CampaignDay.Equals(right.CampaignDay)
			&& string.Equals(left.Message ?? string.Empty, right.Message ?? string.Empty, StringComparison.Ordinal);
	}

	private static bool PolicyEffectTokensEqual(JToken left, JToken right)
	{
		return ReferenceEquals(left, right)
			|| (left != null && right != null && JToken.DeepEquals(left, right));
	}

	private static bool TryReadObjectArray(
		JArray array,
		string propertyName,
		out List<JObject> objects,
		out string error)
	{
		objects = new List<JObject>();
		error = string.Empty;
		if (array == null)
		{
			error = (propertyName ?? "policy effect array") + " cannot be null";
			return false;
		}
		foreach (JToken token in array)
		{
			if (!(token is JObject item))
			{
				error = (propertyName ?? "policy effect array") + " may contain only objects";
				return false;
			}
			objects.Add(item);
		}
		return true;
	}

	private static bool TryReadAuthoritativeObjects(
		IEnumerable<JObject> containers,
		string propertyName,
		out List<JObject> objects,
		out string error)
	{
		objects = new List<JObject>();
		error = string.Empty;
		foreach (JObject container in containers ?? Enumerable.Empty<JObject>())
		{
			JToken token = container?.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);
			if (token == null)
			{
				continue;
			}
			if (!(token is JArray array)
				|| !TryReadObjectArray(array, propertyName, out List<JObject> current, out error))
			{
				if (string.IsNullOrWhiteSpace(error))
				{
					error = propertyName + " must be an array";
				}
				return false;
			}
			objects.AddRange(current);
		}
		return true;
	}

	private static bool TryCoalesceAuthoritativeReceipts(
		IEnumerable<JObject> receiptObjects,
		out List<PolicyEffectExecutionReceipt> logicalReceipts,
		out string error)
	{
		logicalReceipts = new List<PolicyEffectExecutionReceipt>();
		error = string.Empty;
		Dictionary<string, PolicyEffectExecutionReceipt> byReceiptId =
			new Dictionary<string, PolicyEffectExecutionReceipt>(StringComparer.Ordinal);
		JsonSerializer serializer = JsonSerializer.Create(SafeSettings);
		foreach (JObject receiptObject in receiptObjects ?? Enumerable.Empty<JObject>())
		{
			PolicyEffectExecutionReceipt receipt;
			try
			{
				receipt = receiptObject?.ToObject<PolicyEffectExecutionReceipt>(serializer);
			}
			catch (Exception ex)
			{
				error = "policy effect receipt cannot be deserialized: " + ex.Message;
				return false;
			}
			if (!TryValidateReceiptShape(receipt, out error))
			{
				return false;
			}
			if (!byReceiptId.TryGetValue(receipt.ReceiptId, out PolicyEffectExecutionReceipt aggregate))
			{
				byReceiptId.Add(receipt.ReceiptId, receipt);
				logicalReceipts.Add(receipt);
				continue;
			}
			if (!AreCompatibleAuthoritativeShellReceipts(aggregate, receipt))
			{
				error = "duplicate policy effect receiptId has conflicting static data: " + receipt.ReceiptId;
				return false;
			}
			aggregate.TargetSet = MergeCanonicalTargetSets(aggregate.TargetSet, receipt.TargetSet);
		}
		return true;
	}

	private static bool TryValidateLogicalCollections(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		IEnumerable<PolicyEffectExecutionReceipt> receipts,
		out int totalPayloadBytes,
		out string error)
	{
		totalPayloadBytes = 0;
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> instanceList =
			(instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>()).ToList();
		if (instanceList.Count > MaxInstancesPerPolicy)
		{
			error = "policy effect instance count exceeds 24";
			return false;
		}
		Dictionary<string, PolicyEffectInstanceSaveData> instanceById =
			new Dictionary<string, PolicyEffectInstanceSaveData>(StringComparer.Ordinal);
		Dictionary<string, PolicyEffectExecutionReceipt> embeddedReceiptById =
			new Dictionary<string, PolicyEffectExecutionReceipt>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData instance in instanceList)
		{
			if (!TryValidateInstanceShape(instance, out error))
			{
				return false;
			}
			if (PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule knownModule)
				&& knownModule?.Descriptor?.ExecutionKind == PolicyEffectExecutionKind.Composite)
			{
				error = "compile-time Composite policy effect cannot be persisted: " + instance.ModuleId;
				return false;
			}
			if (knownModule != null
				&& !TryValidateKnownModuleScope(instance, knownModule, out error))
			{
				return false;
			}
			if (instanceById.ContainsKey(instance.InstanceId))
			{
				error = "duplicate policy effect instanceId: " + instance.InstanceId;
				return false;
			}
			instanceById.Add(instance.InstanceId, instance);
			totalPayloadBytes += Utf8Size(instance.Payload.ToString(Formatting.None));
			if (totalPayloadBytes > MaxTotalPayloadBytes)
			{
				error = "policy effect payload total exceeds 32KiB";
				return false;
			}
			PolicyEffectExecutionReceipt embeddedReceipt = instance.ExecutionReceipt;
			if (embeddedReceipt != null
				&& embeddedReceiptById.ContainsKey(embeddedReceipt.ReceiptId))
			{
				error = "duplicate embedded policy effect receiptId: " + embeddedReceipt.ReceiptId;
				return false;
			}
			if (embeddedReceipt != null)
			{
				embeddedReceiptById.Add(embeddedReceipt.ReceiptId, embeddedReceipt);
			}
		}

		HashSet<string> topLevelReceiptIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectExecutionReceipt receipt in receipts ?? Enumerable.Empty<PolicyEffectExecutionReceipt>())
		{
			if (!TryValidateReceiptShape(receipt, out error))
			{
				return false;
			}
			if (!topLevelReceiptIds.Add(receipt.ReceiptId))
			{
				error = "duplicate top-level policy effect receiptId: " + receipt.ReceiptId;
				return false;
			}
			if (!instanceById.TryGetValue(receipt.InstanceId, out PolicyEffectInstanceSaveData instance))
			{
				error = "orphan policy effect receipt: " + receipt.ReceiptId;
				return false;
			}
			if (!TryValidateReceiptForInstance(receipt, instance, out error))
			{
				return false;
			}
			if (embeddedReceiptById.TryGetValue(receipt.ReceiptId, out PolicyEffectExecutionReceipt embedded))
			{
				if (!AreCompatibleAuthoritativeShellReceipts(embedded, receipt))
				{
					error = "embedded/top-level policy effect receipt mismatch: " + receipt.ReceiptId;
					return false;
				}
				PolicyEffectCanonicalTargetSet union = MergeCanonicalTargetSets(
					embedded.TargetSet,
					receipt.TargetSet);
				embedded.TargetSet = union;
				receipt.TargetSet = NormalizeCanonicalTargetSet(union);
			}
		}
		return true;
	}

	private static bool TryValidateReceiptShape(
		PolicyEffectExecutionReceipt receipt,
		out string error)
	{
		if (receipt == null
			|| string.IsNullOrWhiteSpace(receipt.ReceiptId)
			|| string.IsNullOrWhiteSpace(receipt.InstanceId)
			|| string.IsNullOrWhiteSpace(receipt.ModuleId)
			|| receipt.TargetSet == null)
		{
			error = "policy effect receipt structure is incomplete";
			return false;
		}
		receipt.ReceiptId = receipt.ReceiptId.Trim();
		receipt.InstanceId = receipt.InstanceId.Trim();
		receipt.ModuleId = receipt.ModuleId.Trim();
		receipt.TargetSet = NormalizeCanonicalTargetSet(receipt.TargetSet);
		if (!Enum.IsDefined(typeof(PolicyEffectExecutionStatus), receipt.Status))
		{
			error = "policy effect receipt has an invalid status: " + receipt.ReceiptId;
			return false;
		}
		if (!IsFinite(receipt.RequestedValue)
			|| !IsFinite(receipt.AppliedValue)
			|| !IsFinite(receipt.CampaignDay))
		{
			error = "policy effect receipt numeric fields must be finite: " + receipt.ReceiptId;
			return false;
		}
		if (!TryValidateReceiptPayload(receipt.RequestedPayload, receipt.ReceiptId, out error)
			|| !TryValidateReceiptPayload(receipt.AppliedPayload, receipt.ReceiptId, out error))
		{
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryValidateReceiptPayload(JToken payload, string receiptId, out string error)
	{
		if (payload == null || payload.Type == JTokenType.Null)
		{
			error = string.Empty;
			return true;
		}
		if (ContainsTypeMetadata(payload))
		{
			error = "policy effect receipt payload must not contain $type metadata: " + receiptId;
			return false;
		}
		if (Utf8Size(payload.ToString(Formatting.None)) > MaxReceiptPayloadBytes)
		{
			error = "policy effect receipt payload exceeds 256KiB: " + receiptId;
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool TryValidateReceiptForInstance(
		PolicyEffectExecutionReceipt receipt,
		PolicyEffectInstanceSaveData instance,
		out string error)
	{
		if (receipt == null || instance == null
			|| !string.Equals(receipt.InstanceId, instance.InstanceId, StringComparison.Ordinal)
			|| !string.Equals(receipt.PolicyId ?? string.Empty, instance.PolicyId ?? string.Empty, StringComparison.Ordinal)
			|| !AreModuleIdsEquivalent(receipt.ModuleId, instance.ModuleId))
		{
			error = "policy effect receipt identity does not match its instance: "
				+ (receipt?.ReceiptId ?? string.Empty);
			return false;
		}
		error = string.Empty;
		return true;
	}

	private static bool AreModuleIdsEquivalent(string left, string right)
	{
		string leftId = (left ?? string.Empty).Trim();
		string rightId = (right ?? string.Empty).Trim();
		bool leftKnown = PolicyEffectModuleCatalog.TryResolveCanonicalId(leftId, out string leftCanonical);
		bool rightKnown = PolicyEffectModuleCatalog.TryResolveCanonicalId(rightId, out string rightCanonical);
		return leftKnown && rightKnown
			? string.Equals(leftCanonical, rightCanonical, StringComparison.Ordinal)
			: !leftKnown && !rightKnown && string.Equals(leftId, rightId, StringComparison.Ordinal);
	}

	private static bool TryValidateKnownModuleScope(
		PolicyEffectInstanceSaveData instance,
		IPolicyEffectModule module,
		out string error)
	{
		string scope = NormalizeSourceScope(instance?.SourceScope);
		if (instance == null
			|| module == null
			|| scope.Length == 0
			|| !PolicyEffectModuleCatalog.IsAllowedForScope(module, scope))
		{
			error = "known policy effect module is not allowed for sourceScope: "
				+ (module?.Id ?? instance?.ModuleId ?? string.Empty)
				+ " / " + scope;
			return false;
		}
		instance.SourceScope = scope;
		error = string.Empty;
		return true;
	}

	private static string NormalizeSourceScope(string scope)
	{
		string normalized = (scope ?? string.Empty).Trim();
		if (string.Equals(normalized, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyEffectScopes.Kingdom;
		}
		if (string.Equals(normalized, PolicyEffectScopes.Local, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyEffectScopes.Local;
		}
		if (string.Equals(normalized, PolicyEffectScopes.Vassal, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyEffectScopes.Vassal;
		}
		return normalized;
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static PolicyEffectCanonicalTargetSet MergeCanonicalTargetSets(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		return NormalizeCanonicalTargetSet(new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = Math.Max(left?.StructureVersion ?? 1, right?.StructureVersion ?? 1),
			JurisdictionKind = PolicyEffectTargetJurisdiction.MergeKind(
				left?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled,
				right?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled),
			AuthorizedCrossKingdomIds = (left?.AuthorizedCrossKingdomIds ?? new List<string>())
				.Concat(right?.AuthorizedCrossKingdomIds ?? new List<string>()).ToList(),
			SelectorHandles = (left?.SelectorHandles ?? new List<string>())
				.Concat(right?.SelectorHandles ?? new List<string>()).ToList(),
			SelectorIds = (left?.SelectorIds ?? new List<string>())
				.Concat(right?.SelectorIds ?? new List<string>()).ToList(),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(
				(left?.TargetPlans ?? new List<PolicyTargetPlanSaveData>())
					.Concat(right?.TargetPlans ?? new List<PolicyTargetPlanSaveData>())),
			SettlementIds = (left?.SettlementIds ?? new List<string>())
				.Concat(right?.SettlementIds ?? new List<string>()).ToList(),
			TownIds = (left?.TownIds ?? new List<string>())
				.Concat(right?.TownIds ?? new List<string>()).ToList(),
			VillageIds = (left?.VillageIds ?? new List<string>())
				.Concat(right?.VillageIds ?? new List<string>()).ToList(),
			ClanIds = (left?.ClanIds ?? new List<string>())
				.Concat(right?.ClanIds ?? new List<string>()).ToList(),
			KingdomIds = (left?.KingdomIds ?? new List<string>())
				.Concat(right?.KingdomIds ?? new List<string>()).ToList(),
			HeroIds = (left?.HeroIds ?? new List<string>())
				.Concat(right?.HeroIds ?? new List<string>()).ToList(),
			ParentSettlementIds = (left?.ParentSettlementIds ?? new List<string>())
				.Concat(right?.ParentSettlementIds ?? new List<string>()).ToList(),
			FollowCurrentRulingClan = left?.FollowCurrentRulingClan == true
				|| right?.FollowCurrentRulingClan == true
		});
	}

	private static PolicyEffectCanonicalTargetSet NormalizeCanonicalTargetSet(
		PolicyEffectCanonicalTargetSet targetSet)
	{
		return new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = Math.Max(1, targetSet?.StructureVersion ?? 1),
			JurisdictionKind = targetSet?.JurisdictionKind ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled,
			AuthorizedCrossKingdomIds = NormalizeTargetIds(targetSet?.AuthorizedCrossKingdomIds),
			SelectorHandles = NormalizeTargetIds(targetSet?.SelectorHandles),
			SelectorIds = NormalizeTargetIds(targetSet?.SelectorIds),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(targetSet?.TargetPlans),
			SettlementIds = NormalizeTargetIds(targetSet?.SettlementIds),
			TownIds = NormalizeTargetIds(targetSet?.TownIds),
			VillageIds = NormalizeTargetIds(targetSet?.VillageIds),
			ClanIds = NormalizeTargetIds(targetSet?.ClanIds),
			KingdomIds = NormalizeTargetIds(targetSet?.KingdomIds),
			HeroIds = NormalizeTargetIds(targetSet?.HeroIds),
			ParentSettlementIds = NormalizeTargetIds(targetSet?.ParentSettlementIds),
			FollowCurrentRulingClan = targetSet?.FollowCurrentRulingClan == true
		};
	}

	private static List<string> NormalizeTargetIds(IEnumerable<string> values)
	{
		return (values ?? Enumerable.Empty<string>())
			.Select(value => (value ?? string.Empty).Trim())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.Ordinal)
			.ToList();
	}

	private static IEnumerable<JObject> EnumerateAuthoritativeEffectContainers(JObject root)
	{
		if (root == null)
		{
			yield break;
		}
		if (root.GetValue("ModuleEffects", StringComparison.OrdinalIgnoreCase) != null)
		{
			// Active v5/v6 owns its root ModuleEffects/ExecutionReceipts. The pending
			// application copy is only a crash-recovery mirror and must not be counted.
			yield return root;
			yield break;
		}
		if (!(root.GetValue("Effects", StringComparison.OrdinalIgnoreCase) is JArray effects))
		{
			yield break;
		}
		foreach (JObject effect in effects.OfType<JObject>())
		{
			// PolicyRecord/Local envelopes own ModuleEffects/ExecutionReceipts on
			// each direct effect; nested recovery snapshots are non-authoritative.
			yield return effect;
		}
	}

	private static bool ContainsTypeMetadata(JToken token)
	{
		if (token is JObject obj)
		{
			foreach (JProperty property in obj.Properties())
			{
				if (string.Equals(property.Name, "$type", StringComparison.OrdinalIgnoreCase)
					|| ContainsTypeMetadata(property.Value))
				{
					return true;
				}
			}
		}
		else if (token is JArray array)
		{
			foreach (JToken child in array)
			{
				if (ContainsTypeMetadata(child))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool HasLegacyStoppedEffectShape(JObject source)
	{
		if (source == null)
		{
			return false;
		}
		if (LegacyStoppedEffectShapeProperties.Any(propertyName =>
			source.GetValue(propertyName, StringComparison.OrdinalIgnoreCase) != null))
		{
			return true;
		}
		return (source.GetValue("Effects", StringComparison.OrdinalIgnoreCase) as JArray)
			?.OfType<JObject>()
			.Any(HasLegacyStoppedEffectShape) == true;
	}

	private static bool HasModuleEffectShape(JObject source)
	{
		if (source == null)
		{
			return false;
		}
		if (source.GetValue("ModuleEffects", StringComparison.OrdinalIgnoreCase) != null)
		{
			return true;
		}
		return (source.GetValue("Effects", StringComparison.OrdinalIgnoreCase) as JArray)
			?.OfType<JObject>()
			.Any(HasModuleEffectShape) == true;
	}

	private static bool TryCloneVersioned(
		JObject source,
		int legacyVersion,
		int currentVersion,
		out JObject normalized,
		out int version,
		out string error)
	{
		normalized = null;
		version = 0;
		error = string.Empty;
		if (source == null)
		{
			error = "save record 不能为空";
			return false;
		}
		version = ReadInt(source, "Version", legacyVersion);
		if (version != legacyVersion && version != currentVersion)
		{
			error = "不支持的 save record 版本: " + version;
			return false;
		}
		normalized = (JObject)source.DeepClone();
		return true;
	}

	private static PolicyEffectMigrationBatchSummary NewSummary()
	{
		return new PolicyEffectMigrationBatchSummary { RecordsVisited = 1 };
	}

	private static JArray GetOrCreateArray(JObject source, string propertyName)
	{
		JProperty property = source.Properties().FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
		if (property?.Value is JArray existing)
		{
			return existing;
		}
		JArray created = new JArray();
		if (property == null)
		{
			source[propertyName] = created;
		}
		else
		{
			property.Value = created;
		}
		return created;
	}

	private static string ReadString(JObject source, string propertyName)
	{
		return source == null ? string.Empty : ((string)source.GetValue(propertyName, StringComparison.OrdinalIgnoreCase) ?? string.Empty).Trim();
	}

	private static int ReadInt(JObject source, string propertyName, int fallback)
	{
		JToken token = source?.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);
		return token != null && token.Type == JTokenType.Integer ? token.Value<int>() : fallback;
	}

	private static bool ReadBool(JObject source, string propertyName)
	{
		JToken token = source?.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);
		return token != null && token.Type == JTokenType.Boolean && token.Value<bool>();
	}

	private static IEnumerable<string> ReadStringArray(JObject source, string propertyName)
	{
		return (source?.GetValue(propertyName, StringComparison.OrdinalIgnoreCase) as JArray)?.Values<string>()
			?? Enumerable.Empty<string>();
	}

	private static void AddUnique(List<string> target, string value)
	{
		string normalized = (value ?? string.Empty).Trim();
		if (normalized.Length > 0 && !target.Contains(normalized, StringComparer.OrdinalIgnoreCase))
		{
			target.Add(normalized);
		}
	}

	private static void AddUniqueRange(List<string> target, IEnumerable<string> values)
	{
		foreach (string value in values ?? Enumerable.Empty<string>())
		{
			AddUnique(target, value);
		}
	}

	private static string FirstNonEmpty(params string[] values)
	{
		return (values ?? Array.Empty<string>()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
	}

	private static int Utf8Size(string value)
	{
		return Encoding.UTF8.GetByteCount(value ?? string.Empty);
	}
}
