using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge.PolicyEffects;

internal enum PolicyEffectPlanParseFailureKind
{
	None,
	InvalidStructure,
	IncompleteLinkedMechanism
}

internal static class PolicyEffectPlanWireNormalizer
{
	private const string PlayerRoleField = "role";
	private const string NpcRoleField = "mechanismRole";
	private static readonly HashSet<string> SemanticIntentFields = new HashSet<string>(
		new[] { "omittedSide", "legs" }, StringComparer.Ordinal);
	private static readonly HashSet<string> SemanticLegFields = new HashSet<string>(
		new[] { "role", "targetDescription", "effectDescription", "strength", "reason" }, StringComparer.Ordinal);
	private static readonly HashSet<string> MappingRootFields = new HashSet<string>(
		new[] { "effectMappingVersion", "assignments" }, StringComparer.Ordinal);
	private static readonly HashSet<string> MappingAssignmentFields = new HashSet<string>(
		new[] { "intentLeg", "moduleId", "targetHandles", "payload" }, StringComparer.Ordinal);
	private static readonly string[] DirectPlayerPlanCanonicalFields =
		{ "effectPlanVersion", "disposition", "reason", "effects" };
	private static readonly string[] DirectPlayerEffectCanonicalFields =
		{ "mechanismId", "mechanismKind", "role", "moduleId", "targetHandles", "payload", "reason" };
	internal static bool TryParseDirectPlayerEffectPlan(
		JObject root,
		out string disposition,
		out string reason,
		out List<PolicyEffectWireEffect> wires,
		out string error)
	{
		return TryParseDirectPlayerEffectPlan(
			root,
			out disposition,
			out reason,
			out wires,
			out error,
			out _);
	}

	internal static bool TryParseDirectPlayerEffectPlan(
		JObject root,
		out string disposition,
		out string reason,
		out List<PolicyEffectWireEffect> wires,
		out string error,
		out PolicyEffectPlanParseFailureKind failureKind)
	{
		disposition = string.Empty;
		reason = string.Empty;
		wires = new List<PolicyEffectWireEffect>();
		error = string.Empty;
		failureKind = PolicyEffectPlanParseFailureKind.InvalidStructure;
		if (root == null
			|| HasConflictingKnownFieldAliases(root, DirectPlayerPlanCanonicalFields)
			|| root["effectPlanVersion"]?.Type != JTokenType.Integer
			|| root.Value<int>("effectPlanVersion") != PolicyEffectPlanVersions.CurrentVersion
			|| root["effects"] is not JArray effects
			|| effects.Count > PolicyEffectSemanticContract.MaximumLegs
			|| !TryReadRequiredText(root, "disposition", out disposition))
		{
			error = "玩家效果计划根对象、版本、disposition 或 effects 无效。";
			return false;
		}
		reason = ReadOptionalNonExecutingText(root, "reason");
		disposition = disposition.ToLowerInvariant();
		if (disposition != "executable" && disposition != "narrativeonly" && disposition != "unsupported")
		{
			error = "disposition 只能是 executable|narrativeOnly|unsupported。";
			return false;
		}
		if (disposition != "executable")
		{
			if (effects.Count != 0)
			{
				error = "narrativeOnly 或 unsupported 必须使用空 effects。";
				return false;
			}
			disposition = disposition == "narrativeonly" ? "narrativeOnly" : disposition;
			failureKind = PolicyEffectPlanParseFailureKind.None;
			return true;
		}
		if (effects.Count == 0)
		{
			error = "executable 必须至少包含一个效果。";
			return false;
		}

		List<DirectPlayerEffectShape> parsed = new List<DirectPlayerEffectShape>(effects.Count);
		for (int index = 0; index < effects.Count; index++)
		{
			if (effects[index] is not JObject effect)
			{
				error = BuildEffectError(index, "必须是对象。");
				wires.Clear();
				return false;
			}
			if (HasConflictingKnownFieldAliases(effect, DirectPlayerEffectCanonicalFields))
			{
				error = BuildEffectError(index, "包含重复或大小写冲突的已知字段。");
				wires.Clear();
				return false;
			}
			if (!TryReadRequiredText(effect, "mechanismKind", out string mechanismKindText)
				|| !TryReadRequiredText(effect, "role", out string roleText)
				|| !TryReadRequiredText(effect, "moduleId", out string moduleId))
			{
				error = BuildEffectError(index, "缺少 mechanismKind、role 或 moduleId。");
				wires.Clear();
				return false;
			}
			if (effect["targetHandles"] is not JArray targetTokens || targetTokens.Count == 0)
			{
				error = BuildEffectError(index, "targetHandles 必须是非空数组。");
				wires.Clear();
				return false;
			}
			if (effect["payload"]?.Type != JTokenType.Object)
			{
				error = BuildEffectError(index, "payload 必须是对象。");
				wires.Clear();
				return false;
			}
			string mechanismKind = mechanismKindText.ToLowerInvariant();
			if ((mechanismKind != "independent" && mechanismKind != "linked")
				|| !TryParseSemanticRole(roleText, out PolicyEffectMechanismRole role))
			{
				error = BuildEffectError(index, "mechanismKind 或 role 无效。");
				wires.Clear();
				return false;
			}
			if (mechanismKind == "independent" && role != PolicyEffectMechanismRole.Subject)
			{
				error = BuildEffectError(index, "independent 只能使用 subject role。");
				wires.Clear();
				return false;
			}
			if (mechanismKind == "linked"
				&& role != PolicyEffectMechanismRole.Source
				&& role != PolicyEffectMechanismRole.Destination
				&& role != PolicyEffectMechanismRole.Beneficiary)
			{
				error = BuildEffectError(index, "linked 只能使用真实 source、destination 或 beneficiary 资源流转腿。");
				wires.Clear();
				return false;
			}
			List<string> targetHandles = new List<string>(targetTokens.Count);
			foreach (JToken token in targetTokens)
			{
				if (token?.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
				{
					error = BuildEffectError(index, "targetHandles 只能包含非空字符串。");
					wires.Clear();
					return false;
				}
				string handle = token.Value<string>();
				if (!string.Equals(handle, handle.Trim(), StringComparison.Ordinal))
				{
					error = BuildEffectError(index, "targetHandles 必须精确使用 canonical handle，不能包含首尾空白。");
					wires.Clear();
					return false;
				}
				if (targetHandles.Contains(handle, StringComparer.Ordinal))
				{
					error = BuildEffectError(index, "targetHandles 包含重复句柄：" + handle);
					wires.Clear();
					return false;
				}
				targetHandles.Add(handle);
			}
			PolicyEffectWireEffect wire = new PolicyEffectWireEffect
			{
				EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
				MechanismId = string.Empty,
				MechanismKind = mechanismKind == "linked"
					? PolicyEffectMechanismKind.Linked
					: PolicyEffectMechanismKind.Independent,
				MechanismRole = role,
				SourceOmitted = false,
				DestinationOmitted = false,
				ModuleId = moduleId,
				TargetHandles = targetHandles,
				Payload = effect["payload"].DeepClone(),
				Reason = ReadOptionalNonExecutingText(effect, "reason")
			};
			parsed.Add(new DirectPlayerEffectShape
			{
				ExternalMechanismLabel = ReadOptionalGroupingLabel(effect, "mechanismId"),
				Wire = wire
			});
		}

		List<DirectPlayerEffectShape> linked = parsed
			.Where(shape => shape.Wire.MechanismKind == PolicyEffectMechanismKind.Linked)
			.ToList();
		if (linked.Count > 0)
		{
			int labeledCount = linked.Count(shape => shape.ExternalMechanismLabel.Length > 0);
			if (labeledCount != 0 && labeledCount != linked.Count)
			{
				error = "linked effects 混用了有标签和无标签的 mechanismId，无法安全确定分组。";
				return false;
			}
			if (labeledCount == 0)
			{
				bool uniquePair = linked.Count == 2
					&& linked.Count(shape => shape.Wire.MechanismRole == PolicyEffectMechanismRole.Source) == 1
					&& linked.Count(shape => shape.Wire.MechanismRole == PolicyEffectMechanismRole.Destination
						|| shape.Wire.MechanismRole == PolicyEffectMechanismRole.Beneficiary) == 1;
				if (!uniquePair)
				{
					error = "linked effects 缺少 mechanismId，且无法唯一推导一个 source→destination/beneficiary 分组。";
					failureKind = PolicyEffectPlanParseFailureKind.IncompleteLinkedMechanism;
					return false;
				}
			}
			int linkedGroupOrdinal = 0;
			foreach (IGrouping<string, DirectPlayerEffectShape> group in linked
				.GroupBy(shape => shape.ExternalMechanismLabel, StringComparer.Ordinal))
			{
				bool hasSource = group.Any(shape => shape.Wire.MechanismRole == PolicyEffectMechanismRole.Source);
				bool hasDestination = group.Any(shape => shape.Wire.MechanismRole == PolicyEffectMechanismRole.Destination
					|| shape.Wire.MechanismRole == PolicyEffectMechanismRole.Beneficiary);
				if (group.Count() < 2 || !hasSource || !hasDestination)
				{
					failureKind = PolicyEffectPlanParseFailureKind.IncompleteLinkedMechanism;
					error = "linked mechanism group "
						+ linkedGroupOrdinal.ToString(CultureInfo.InvariantCulture)
						+ " 必须包含至少两条真实资源流转腿，并同时包含 source 与 destination/beneficiary。";
					return false;
				}
				linkedGroupOrdinal++;
			}
		}

		Dictionary<string, string> linkedInternalIds = new Dictionary<string, string>(StringComparer.Ordinal);
		int mechanismOrdinal = 0;
		foreach (DirectPlayerEffectShape shape in parsed)
		{
			if (shape.Wire.MechanismKind == PolicyEffectMechanismKind.Independent)
			{
				shape.Wire.MechanismId = BuildInternalMechanismId(mechanismOrdinal++);
			}
			else if (!linkedInternalIds.TryGetValue(shape.ExternalMechanismLabel, out string internalId))
			{
				internalId = BuildInternalMechanismId(mechanismOrdinal++);
				linkedInternalIds.Add(shape.ExternalMechanismLabel, internalId);
				shape.Wire.MechanismId = internalId;
			}
			else
			{
				shape.Wire.MechanismId = internalId;
			}
			wires.Add(shape.Wire);
		}
		if (mechanismOrdinal > PolicyEffectPlanVersions.MaximumMechanisms)
		{
			wires.Clear();
			error = "EffectPlan mechanism count exceeds " + PolicyEffectPlanVersions.MaximumMechanisms + ".";
			return false;
		}
		failureKind = PolicyEffectPlanParseFailureKind.None;
		return true;
	}

	internal static bool TryNormalizeSemanticPlan(
		int effectIntentVersion,
		JArray effectIntents,
		out PolicyEffectSemanticPlan plan,
		out string error)
	{
		plan = null;
		error = string.Empty;
		if (effectIntentVersion != PolicyEffectSemanticContract.CurrentVersion)
		{
			error = "effectIntentVersion 必须严格等于 "
				+ PolicyEffectSemanticContract.CurrentVersion.ToString(CultureInfo.InvariantCulture) + "。";
			return false;
		}
		if (effectIntents == null)
		{
			error = "effectIntents 必须是数组。";
			return false;
		}
		if (effectIntents.Count > PolicyEffectSemanticContract.MaximumIntents)
		{
			error = "effectIntents 超过最大机制数量 "
				+ PolicyEffectSemanticContract.MaximumIntents.ToString(CultureInfo.InvariantCulture) + "。";
			return false;
		}

		PolicyEffectSemanticPlan normalized = new PolicyEffectSemanticPlan();
		int totalLegs = 0;
		for (int intentIndex = 0; intentIndex < effectIntents.Count; intentIndex++)
		{
			if (effectIntents[intentIndex] is not JObject intent
				|| !HasExactFields(intent, SemanticIntentFields))
			{
				error = BuildIntentError(intentIndex, "必须且只能包含 omittedSide 和 legs。");
				return false;
			}
			if (intent["omittedSide"]?.Type != JTokenType.String
				|| intent["legs"] is not JArray legs
				|| legs.Count == 0)
			{
				error = BuildIntentError(intentIndex, "omittedSide 或 legs 无效。");
				return false;
			}
			totalLegs += legs.Count;
			if (totalLegs > PolicyEffectSemanticContract.MaximumLegs)
			{
				error = "effectIntents 总语义腿数量超过 "
					+ PolicyEffectSemanticContract.MaximumLegs.ToString(CultureInfo.InvariantCulture) + "。";
				return false;
			}

			string omittedSide = (intent.Value<string>("omittedSide") ?? string.Empty).Trim().ToLowerInvariant();
			if (omittedSide != "none" && omittedSide != "source" && omittedSide != "destination")
			{
				error = BuildIntentError(intentIndex, "omittedSide 只能是 none|source|destination。");
				return false;
			}
			PolicyEffectSemanticIntent normalizedIntent = new PolicyEffectSemanticIntent
			{
				MechanismId = "M" + intentIndex.ToString(CultureInfo.InvariantCulture),
				SourceOmitted = omittedSide == "source",
				DestinationOmitted = omittedSide == "destination"
			};
			for (int legIndex = 0; legIndex < legs.Count; legIndex++)
			{
				if (legs[legIndex] is not JObject leg || !HasExactFields(leg, SemanticLegFields))
				{
					error = BuildLegError(intentIndex, legIndex, "字段缺失、重复或包含未知字段。");
					return false;
				}
				if (!TryReadRequiredText(leg, "role", PolicyEffectSemanticContract.MaximumStrengthLength, out string roleText)
					|| !TryReadRequiredText(leg, "targetDescription", PolicyEffectSemanticContract.MaximumTextLength, out string targetDescription)
					|| !TryReadRequiredText(leg, "effectDescription", PolicyEffectSemanticContract.MaximumTextLength, out string effectDescription)
					|| !TryReadRequiredText(leg, "strength", PolicyEffectSemanticContract.MaximumStrengthLength, out string strength)
					|| !TryReadRequiredText(leg, "reason", PolicyEffectSemanticContract.MaximumTextLength, out string reason)
					|| !TryParseSemanticRole(roleText, out PolicyEffectMechanismRole role))
				{
					error = BuildLegError(intentIndex, legIndex, "文本字段或 role 无效。");
					return false;
				}
				normalizedIntent.Legs.Add(new PolicyEffectSemanticLeg
				{
					IntentLegId = "I" + intentIndex.ToString(CultureInfo.InvariantCulture)
						+ "L" + legIndex.ToString(CultureInfo.InvariantCulture),
					Role = role,
					TargetDescription = targetDescription,
					EffectDescription = effectDescription,
					Strength = strength,
					Reason = reason
				});
			}

			bool hasSubject = normalizedIntent.Legs.Any(leg => leg.Role == PolicyEffectMechanismRole.Subject);
			bool hasNonSubject = normalizedIntent.Legs.Any(leg => leg.Role != PolicyEffectMechanismRole.Subject);
			bool hasSource = normalizedIntent.Legs.Any(leg => IsSourceRole(leg.Role));
			bool hasDestination = normalizedIntent.Legs.Any(leg => IsDestinationRole(leg.Role));
			if (hasSubject)
			{
				if (hasNonSubject || omittedSide != "none")
				{
					error = BuildIntentError(intentIndex, "independent 只能包含 subject 且 omittedSide=none。");
					return false;
				}
				normalizedIntent.MechanismKind = PolicyEffectMechanismKind.Independent;
			}
			else
			{
				bool validLinked = hasSource && hasDestination && omittedSide == "none"
					|| hasSource && !hasDestination && omittedSide == "destination"
					|| !hasSource && hasDestination && omittedSide == "source";
				if (!validLinked)
				{
					error = BuildIntentError(intentIndex, "linked 的已知腿与 omittedSide 不一致。");
					return false;
				}
				normalizedIntent.MechanismKind = PolicyEffectMechanismKind.Linked;
			}
			normalized.Intents.Add(normalizedIntent);
		}
		plan = normalized;
		return true;
	}

	internal static JObject BuildSemanticPlanPromptObject(PolicyEffectSemanticPlan plan)
	{
		JArray intents = new JArray();
		foreach (PolicyEffectSemanticIntent intent in plan?.Intents ?? new List<PolicyEffectSemanticIntent>())
		{
			JArray legs = new JArray();
			foreach (PolicyEffectSemanticLeg leg in intent?.Legs ?? new List<PolicyEffectSemanticLeg>())
			{
				legs.Add(new JObject
				{
					["intentLeg"] = leg.IntentLegId,
					["role"] = ToWireRole(leg.Role),
					["targetDescription"] = leg.TargetDescription,
					["effectDescription"] = leg.EffectDescription,
					["strength"] = leg.Strength,
					["reason"] = leg.Reason
				});
			}
			intents.Add(new JObject
			{
				["mechanismId"] = intent.MechanismId,
				["mechanismKind"] = intent.MechanismKind == PolicyEffectMechanismKind.Independent ? "independent" : "linked",
				["omittedSide"] = intent.SourceOmitted ? "source" : intent.DestinationOmitted ? "destination" : "none",
				["legs"] = legs
			});
		}
		return new JObject
		{
			["effectIntentVersion"] = plan?.Version ?? PolicyEffectSemanticContract.CurrentVersion,
			["effectIntents"] = intents
		};
	}

	internal static bool TryExpandEffectMapping(
		JObject root,
		PolicyEffectSemanticPlan semanticPlan,
		bool requireSingleTarget,
		out List<PolicyEffectWireEffect> wires,
		out string error)
	{
		wires = new List<PolicyEffectWireEffect>();
		error = string.Empty;
		if (root == null || !HasExactFields(root, MappingRootFields)
			|| root["effectMappingVersion"]?.Type != JTokenType.Integer
			|| root.Value<int>("effectMappingVersion") != PolicyEffectSemanticContract.CurrentMappingVersion
			|| root["assignments"] is not JArray assignments
			|| assignments.Count > PolicyEffectSemanticContract.MaximumLegs)
		{
			error = "效果映射根对象、版本或 assignments 无效。";
			return false;
		}
		Dictionary<string, Tuple<PolicyEffectSemanticIntent, PolicyEffectSemanticLeg>> legsById =
			new Dictionary<string, Tuple<PolicyEffectSemanticIntent, PolicyEffectSemanticLeg>>(StringComparer.Ordinal);
		foreach (PolicyEffectSemanticIntent intent in semanticPlan?.Intents ?? new List<PolicyEffectSemanticIntent>())
		{
			foreach (PolicyEffectSemanticLeg leg in intent?.Legs ?? new List<PolicyEffectSemanticLeg>())
			{
				legsById.Add(leg.IntentLegId, Tuple.Create(intent, leg));
			}
		}
		HashSet<string> mappedLegs = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> exactAssignments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int index = 0; index < assignments.Count; index++)
		{
			if (assignments[index] is not JObject assignment || !HasExactFields(assignment, MappingAssignmentFields)
				|| assignment["intentLeg"]?.Type != JTokenType.String
				|| assignment["moduleId"]?.Type != JTokenType.String
				|| assignment["targetHandles"] is not JArray targetTokens
				|| assignment["payload"]?.Type != JTokenType.Object)
			{
				error = BuildMappingError(index, "字段缺失、重复、类型无效或包含未知字段。");
				return false;
			}
			string intentLegId = (assignment.Value<string>("intentLeg") ?? string.Empty).Trim();
			string moduleId = (assignment.Value<string>("moduleId") ?? string.Empty).Trim();
			if (!legsById.TryGetValue(intentLegId, out Tuple<PolicyEffectSemanticIntent, PolicyEffectSemanticLeg> semantic)
				|| moduleId.Length == 0
				|| targetTokens.Count == 0
				|| (requireSingleTarget && targetTokens.Count != 1))
			{
				error = BuildMappingError(index, "intentLeg、moduleId 或 targetHandles 无效：" + intentLegId);
				return false;
			}
			List<string> targetHandles = new List<string>(targetTokens.Count);
			foreach (JToken targetToken in targetTokens)
			{
				if (targetToken?.Type != JTokenType.String || string.IsNullOrWhiteSpace(targetToken.Value<string>()))
				{
					error = BuildMappingError(index, "targetHandles 必须只包含非空字符串。");
					return false;
				}
				string targetHandle = targetToken.Value<string>().Trim();
				if (targetHandles.Contains(targetHandle, StringComparer.OrdinalIgnoreCase))
				{
					error = BuildMappingError(index, "targetHandles 包含重复句柄：" + targetHandle);
					return false;
				}
				targetHandles.Add(targetHandle);
			}
			string exactKey = intentLegId + "\u001e" + moduleId + "\u001e"
				+ string.Join("\u001f", targetHandles.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
			if (!exactAssignments.Add(exactKey))
			{
				error = BuildMappingError(index, "与先前 assignment 完全重复：" + intentLegId);
				return false;
			}
			mappedLegs.Add(intentLegId);
			PolicyEffectSemanticIntent semanticIntent = semantic.Item1;
			PolicyEffectSemanticLeg semanticLeg = semantic.Item2;
			wires.Add(new PolicyEffectWireEffect
			{
				EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
				MechanismId = semanticIntent.MechanismId,
				MechanismKind = semanticIntent.MechanismKind,
				MechanismRole = semanticLeg.Role,
				SourceOmitted = semanticIntent.SourceOmitted,
				DestinationOmitted = semanticIntent.DestinationOmitted,
				ModuleId = moduleId,
				TargetHandles = targetHandles,
				Payload = assignment["payload"].DeepClone(),
				Reason = semanticLeg.Reason
			});
		}
		string missingLeg = legsById.Keys.FirstOrDefault(intentLegId => !mappedLegs.Contains(intentLegId));
		if (missingLeg != null)
		{
			wires.Clear();
			error = "效果映射缺少语义腿 " + missingLeg + "。";
			return false;
		}
		return true;
	}

	internal static bool TryNormalizeModelEffectPlan(
		JArray effects,
		string preferredRoleField,
		out JArray normalized,
		out IReadOnlyList<string> diagnostics,
		out string error)
	{
		normalized = new JArray();
		List<string> actions = new List<string>();
		diagnostics = actions;
		error = string.Empty;
		if (!string.Equals(preferredRoleField, PlayerRoleField, StringComparison.Ordinal)
			&& !string.Equals(preferredRoleField, NpcRoleField, StringComparison.Ordinal))
		{
			error = "EffectPlan preferred role field is invalid.";
			return false;
		}

		JArray copy = effects == null ? new JArray() : (JArray)effects.DeepClone();
		List<NormalizedEffectShape> shapes = new List<NormalizedEffectShape>(copy.Count);
		for (int index = 0; index < copy.Count; index++)
		{
			if (copy[index] is not JObject effect)
			{
				error = "EffectPlan effect " + (index + 1).ToString(CultureInfo.InvariantCulture) + " must be an object.";
				return false;
			}
			if (!TryReadRole(effect, index, out string role, out error)
				|| !TryReadOptionalMechanismId(effect, index, out string mechanismId, out error)
				|| !TryReadOptionalBoolean(effect, "sourceOmitted", index, out bool? sourceOmitted, out error)
				|| !TryReadOptionalBoolean(effect, "destinationOmitted", index, out bool? destinationOmitted, out error))
			{
				return false;
			}

			bool independent = string.Equals(role, "subject", StringComparison.Ordinal);
			SetCanonicalProperty(effect, "mechanismKind", independent ? "independent" : "linked");
			SetCanonicalProperty(effect, preferredRoleField, role);
			RemoveProperties(effect, string.Equals(preferredRoleField, PlayerRoleField, StringComparison.Ordinal)
				? NpcRoleField
				: PlayerRoleField);
			if (!HasProperty(effect, "reason"))
			{
				effect["reason"] = string.Empty;
				actions.Add(BuildEffectAction(index, "reason=empty"));
			}
			if (independent)
			{
				SetCanonicalProperty(effect, "sourceOmitted", false);
				SetCanonicalProperty(effect, "destinationOmitted", false);
			}
			shapes.Add(new NormalizedEffectShape
			{
				Index = index,
				Effect = effect,
				Role = role,
				MechanismId = mechanismId,
				SourceOmitted = sourceOmitted,
				DestinationOmitted = destinationOmitted
			});
			actions.Add(BuildEffectAction(index, "kind=" + (independent ? "independent" : "linked")));
		}

		HashSet<string> usedMechanismIds = new HashSet<string>(
			shapes.Select(shape => shape.MechanismId).Where(value => !string.IsNullOrWhiteSpace(value)),
			StringComparer.Ordinal);
		foreach (NormalizedEffectShape subject in shapes.Where(shape => shape.IsIndependent))
		{
			if (string.IsNullOrWhiteSpace(subject.MechanismId))
			{
				subject.MechanismId = BuildStableGeneratedMechanismId("AFI", subject.Index, usedMechanismIds);
				SetCanonicalProperty(subject.Effect, "mechanismId", subject.MechanismId);
				actions.Add(BuildEffectAction(subject.Index, "mechanismId=derived"));
			}
		}

		List<NormalizedEffectShape> linked = shapes.Where(shape => !shape.IsIndependent).ToList();
		if (linked.Count > 0)
		{
			int linkedWithId = linked.Count(shape => !string.IsNullOrWhiteSpace(shape.MechanismId));
			if (linkedWithId != 0 && linkedWithId != linked.Count)
			{
				error = "Linked effects mix present and missing mechanismId values; grouping is ambiguous.";
				return false;
			}
			if (linkedWithId == 0)
			{
				int sourceSideCount = linked.Count(shape => shape.IsSourceSide);
				int destinationSideCount = linked.Count(shape => shape.IsDestinationSide);
				bool hasDuplicateRole = linked.GroupBy(shape => shape.Role, StringComparer.Ordinal)
					.Any(group => group.Count() > 1);
				bool oneCompletePair = sourceSideCount > 0 && destinationSideCount > 0 && !hasDuplicateRole;
				bool oneExplicitlyOneSided = linked.Count == 1
					&& ((sourceSideCount == 1 && linked[0].DestinationOmitted == true)
						|| (destinationSideCount == 1 && linked[0].SourceOmitted == true));
				if (!oneCompletePair && !oneExplicitlyOneSided)
				{
					error = "Linked effects require mechanismId because more than one grouping is possible.";
					return false;
				}
				string generatedId = BuildStableGeneratedMechanismId("AFL", 0, usedMechanismIds);
				foreach (NormalizedEffectShape shape in linked)
				{
					shape.MechanismId = generatedId;
					SetCanonicalProperty(shape.Effect, "mechanismId", generatedId);
					actions.Add(BuildEffectAction(shape.Index, "mechanismId=derived"));
				}
			}

			foreach (IGrouping<string, NormalizedEffectShape> group in linked.GroupBy(shape => shape.MechanismId, StringComparer.Ordinal))
			{
				bool hasSource = group.Any(shape => shape.IsSourceSide);
				bool hasDestination = group.Any(shape => shape.IsDestinationSide);
				bool sourceOmitted;
				bool destinationOmitted;
				if (hasSource && hasDestination)
				{
					sourceOmitted = false;
					destinationOmitted = false;
				}
				else if (hasSource)
				{
					if (!group.Any(shape => shape.DestinationOmitted == true))
					{
						error = "Linked mechanism " + group.Key + " has no destination side; destinationOmitted=true is required.";
						return false;
					}
					sourceOmitted = false;
					destinationOmitted = true;
				}
				else if (hasDestination)
				{
					if (!group.Any(shape => shape.SourceOmitted == true))
					{
						error = "Linked mechanism " + group.Key + " has no source side; sourceOmitted=true is required.";
						return false;
					}
					sourceOmitted = true;
					destinationOmitted = false;
				}
				else
				{
					error = "Linked mechanism " + group.Key + " has no recognized source or destination role.";
					return false;
				}
				foreach (NormalizedEffectShape shape in group)
				{
					SetCanonicalProperty(shape.Effect, "sourceOmitted", sourceOmitted);
					SetCanonicalProperty(shape.Effect, "destinationOmitted", destinationOmitted);
				}
				actions.Add("mechanism[" + group.Key + "]:omission="
					+ sourceOmitted.ToString().ToLowerInvariant() + "/"
					+ destinationOmitted.ToString().ToLowerInvariant());
			}
		}

		normalized = copy;
		diagnostics = actions.AsReadOnly();
		return true;
	}

	private static bool HasExactFields(JObject value, ISet<string> expected)
	{
		if (value == null)
		{
			return false;
		}
		List<string> actual = value.Properties().Select(property => property.Name).ToList();
		return actual.Count == expected.Count
			&& actual.Distinct(StringComparer.Ordinal).Count() == actual.Count
			&& new HashSet<string>(actual, StringComparer.Ordinal).SetEquals(expected);
	}

	private static bool HasConflictingKnownFieldAliases(JObject value, IEnumerable<string> canonicalFields)
	{
		if (value == null)
		{
			return false;
		}
		return (canonicalFields ?? Enumerable.Empty<string>()).Any(fieldName => value.Properties()
			.Count(property => string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase)) > 1);
	}

	private static bool TryReadRequiredText(JObject value, string fieldName, int maximumLength, out string text)
	{
		text = string.Empty;
		if (value?[fieldName]?.Type != JTokenType.String)
		{
			return false;
		}
		text = (value.Value<string>(fieldName) ?? string.Empty).Trim();
		return text.Length > 0 && text.Length <= maximumLength;
	}

	private static bool TryReadRequiredText(JObject value, string fieldName, out string text)
	{
		text = string.Empty;
		if (value?[fieldName]?.Type != JTokenType.String)
		{
			return false;
		}
		text = (value.Value<string>(fieldName) ?? string.Empty).Trim();
		return text.Length > 0;
	}

	private static string ReadOptionalNonExecutingText(JObject value, string fieldName)
	{
		if (value?[fieldName]?.Type != JTokenType.String)
		{
			return string.Empty;
		}
		string text = (value.Value<string>(fieldName) ?? string.Empty).Trim();
		return text.Length <= PolicyEffectSemanticContract.MaximumTextLength
			? text
			: text.Substring(0, PolicyEffectSemanticContract.MaximumTextLength);
	}

	private static string ReadOptionalGroupingLabel(JObject value, string fieldName)
	{
		return value?[fieldName]?.Type == JTokenType.String
			? (value.Value<string>(fieldName) ?? string.Empty).Trim()
			: string.Empty;
	}

	private static string BuildInternalMechanismId(int ordinal)
	{
		return "M" + Math.Max(0, ordinal).ToString(CultureInfo.InvariantCulture);
	}

	private static bool TryParseSemanticRole(string text, out PolicyEffectMechanismRole role)
	{
		switch ((text ?? string.Empty).Trim().ToLowerInvariant())
		{
			case "subject":
				role = PolicyEffectMechanismRole.Subject;
				return true;
			case "source":
				role = PolicyEffectMechanismRole.Source;
				return true;
			case "destination":
				role = PolicyEffectMechanismRole.Destination;
				return true;
			case "cost":
				role = PolicyEffectMechanismRole.Cost;
				return true;
			case "beneficiary":
				role = PolicyEffectMechanismRole.Beneficiary;
				return true;
			default:
				role = PolicyEffectMechanismRole.Subject;
				return false;
		}
	}

	private static string ToWireRole(PolicyEffectMechanismRole role)
	{
		return role.ToString().ToLowerInvariant();
	}

	private static bool IsSourceRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Source || role == PolicyEffectMechanismRole.Cost;
	}

	private static bool IsDestinationRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Destination || role == PolicyEffectMechanismRole.Beneficiary;
	}

	private static string BuildIntentError(int intentIndex, string message)
	{
		return "effectIntent[" + intentIndex.ToString(CultureInfo.InvariantCulture) + "] " + message;
	}

	private static string BuildLegError(int intentIndex, int legIndex, string message)
	{
		return "effectIntent[" + intentIndex.ToString(CultureInfo.InvariantCulture) + "].legs["
			+ legIndex.ToString(CultureInfo.InvariantCulture) + "] " + message;
	}

	private static string BuildMappingError(int assignmentIndex, string message)
	{
		return "assignment[" + assignmentIndex.ToString(CultureInfo.InvariantCulture) + "] " + message;
	}

	private static bool TryReadRole(JObject effect, int index, out string role, out string error)
	{
		role = string.Empty;
		error = string.Empty;
		if (!TryGetUniqueProperty(effect, PlayerRoleField, index, out JProperty roleProperty, out error)
			|| !TryGetUniqueProperty(effect, NpcRoleField, index, out JProperty mechanismRoleProperty, out error))
		{
			return false;
		}
		if (roleProperty == null && mechanismRoleProperty == null)
		{
			error = BuildEffectError(index, "requires role or mechanismRole.");
			return false;
		}
		if (!TryParseRoleValue(roleProperty, index, PlayerRoleField, out string playerRole, out error)
			|| !TryParseRoleValue(mechanismRoleProperty, index, NpcRoleField, out string npcRole, out error))
		{
			return false;
		}
		if (playerRole.Length > 0 && npcRole.Length > 0
			&& !string.Equals(playerRole, npcRole, StringComparison.Ordinal))
		{
			error = BuildEffectError(index, "role and mechanismRole conflict.");
			return false;
		}
		role = playerRole.Length > 0 ? playerRole : npcRole;
		return true;
	}

	private static bool TryParseRoleValue(JProperty property, int index, string fieldName, out string role, out string error)
	{
		role = string.Empty;
		error = string.Empty;
		if (property == null)
		{
			return true;
		}
		if (property.Value.Type != JTokenType.String)
		{
			error = BuildEffectError(index, fieldName + " must be a string.");
			return false;
		}
		role = (property.Value.Value<string>() ?? string.Empty).Trim().ToLowerInvariant();
		if (role != "subject" && role != "source" && role != "destination" && role != "cost" && role != "beneficiary")
		{
			error = BuildEffectError(index, fieldName + " is invalid.");
			return false;
		}
		return true;
	}

	private static bool TryReadOptionalMechanismId(JObject effect, int index, out string mechanismId, out string error)
	{
		mechanismId = string.Empty;
		if (!TryGetUniqueProperty(effect, "mechanismId", index, out JProperty property, out error))
		{
			return false;
		}
		if (property == null || property.Value.Type == JTokenType.Null)
		{
			return true;
		}
		if (property.Value.Type != JTokenType.String)
		{
			error = BuildEffectError(index, "mechanismId must be a string when provided.");
			return false;
		}
		mechanismId = (property.Value.Value<string>() ?? string.Empty).Trim();
		return true;
	}

	private static bool TryReadOptionalBoolean(
		JObject effect,
		string fieldName,
		int index,
		out bool? value,
		out string error)
	{
		value = null;
		if (!TryGetUniqueProperty(effect, fieldName, index, out JProperty property, out error))
		{
			return false;
		}
		if (property == null)
		{
			return true;
		}
		if (property.Value.Type == JTokenType.Boolean)
		{
			value = property.Value.Value<bool>();
			return true;
		}
		if (property.Value.Type == JTokenType.String)
		{
			string text = property.Value.Value<string>() ?? string.Empty;
			if (string.Equals(text, "true", StringComparison.Ordinal))
			{
				value = true;
				return true;
			}
			if (string.Equals(text, "false", StringComparison.Ordinal))
			{
				value = false;
				return true;
			}
		}
		error = BuildEffectError(index, fieldName + " must be a boolean or the exact string true/false.");
		return false;
	}

	private static bool TryGetUniqueProperty(
		JObject effect,
		string fieldName,
		int index,
		out JProperty property,
		out string error)
	{
		List<JProperty> matches = effect.Properties()
			.Where(candidate => string.Equals(candidate.Name, fieldName, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (matches.Count > 1)
		{
			property = null;
			error = BuildEffectError(index, "contains duplicate " + fieldName + " fields.");
			return false;
		}
		property = matches.Count == 1 ? matches[0] : null;
		error = string.Empty;
		return true;
	}

	private static bool HasProperty(JObject effect, string fieldName)
	{
		return effect.Properties().Any(property => string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase));
	}

	private static void SetCanonicalProperty(JObject effect, string fieldName, object value)
	{
		RemoveProperties(effect, fieldName);
		effect[fieldName] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
	}

	private static void RemoveProperties(JObject effect, string fieldName)
	{
		foreach (JProperty property in effect.Properties()
			.Where(candidate => string.Equals(candidate.Name, fieldName, StringComparison.OrdinalIgnoreCase))
			.ToList())
		{
			property.Remove();
		}
	}

	private static string BuildStableGeneratedMechanismId(string prefix, int ordinal, ISet<string> used)
	{
		string candidate = prefix + Math.Max(0, ordinal).ToString(CultureInfo.InvariantCulture);
		int collision = 0;
		while (used.Contains(candidate))
		{
			collision++;
			candidate = prefix + Math.Max(0, ordinal).ToString(CultureInfo.InvariantCulture)
				+ "_" + collision.ToString(CultureInfo.InvariantCulture);
		}
		used.Add(candidate);
		return candidate;
	}

	private static string BuildEffectAction(int index, string action)
	{
		return "effect[" + index.ToString(CultureInfo.InvariantCulture) + "]:" + action;
	}

	private static string BuildEffectError(int index, string message)
	{
		return "EffectPlan effect " + (index + 1).ToString(CultureInfo.InvariantCulture) + " " + message;
	}

	private sealed class NormalizedEffectShape
	{
		internal int Index;
		internal JObject Effect;
		internal string Role;
		internal string MechanismId;
		internal bool? SourceOmitted;
		internal bool? DestinationOmitted;

		internal bool IsIndependent => string.Equals(Role, "subject", StringComparison.Ordinal);

		internal bool IsSourceSide => string.Equals(Role, "source", StringComparison.Ordinal)
			|| string.Equals(Role, "cost", StringComparison.Ordinal);

		internal bool IsDestinationSide => string.Equals(Role, "destination", StringComparison.Ordinal)
			|| string.Equals(Role, "beneficiary", StringComparison.Ordinal);
	}

	private sealed class DirectPlayerEffectShape
	{
		internal string ExternalMechanismLabel = string.Empty;
		internal PolicyEffectWireEffect Wire;
	}
}
