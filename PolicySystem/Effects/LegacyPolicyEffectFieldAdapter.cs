using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AnimusForge.PolicyEffects;

/// <summary>
/// Temporary one-way bridge from the nine fixed numeric fields to canonical module ids.
/// New save/runtime code must not write those fields back to JSON.
/// </summary>
internal static class LegacyPolicyEffectFieldAdapter
{
	private sealed class FieldBinding
	{
		internal FieldBinding(
			string canonicalFieldName,
			params string[] aliases)
		{
			CanonicalFieldName = canonicalFieldName;
			Aliases = aliases ?? Array.Empty<string>();
		}

		internal string CanonicalFieldName { get; }

		internal IReadOnlyList<string> Aliases { get; }
	}

	private static readonly FieldBinding[] Bindings =
	{
		new FieldBinding("prosperityDailyDeltaPerTown"),
		new FieldBinding("foodDailyDeltaPerTown"),
		new FieldBinding("hearthDailyDeltaPerVillage"),
		new FieldBinding("loyaltyDailyDeltaPerTown"),
		new FieldBinding("securityDailyDeltaPerTown"),
		new FieldBinding("militiaDailyDeltaPerTown"),
		new FieldBinding("townTaxPercent"),
		new FieldBinding("constructionPowerDailyDelta", "constructionSpeedPercent"),
		new FieldBinding("kingdomStabilityDailyDelta")
	};

	private static readonly IReadOnlyDictionary<string, FieldBinding> ByFieldName = BuildByFieldName();

	internal static IReadOnlyList<string> CanonicalModuleIds { get; } = BuildCanonicalModuleIds();

	internal static bool TryResolveModuleId(string legacyFieldName, out string moduleId)
	{
		if (ByFieldName.TryGetValue((legacyFieldName ?? string.Empty).Trim(), out FieldBinding binding)
			&& PolicyEffectModuleCatalog.TryResolveCanonicalId(binding.CanonicalFieldName, out moduleId))
		{
			return true;
		}
		moduleId = string.Empty;
		return false;
	}

	internal static bool TryGetCanonicalLegacyFieldName(string moduleId, out string legacyFieldName)
	{
		if (PolicyEffectModuleCatalog.TryResolveCanonicalId(moduleId, out string canonicalModuleId))
		{
			foreach (FieldBinding binding in Bindings)
			{
				if (PolicyEffectModuleCatalog.TryResolveCanonicalId(binding.CanonicalFieldName, out string bindingModuleId)
					&& string.Equals(bindingModuleId, canonicalModuleId, StringComparison.Ordinal))
				{
					legacyFieldName = binding.CanonicalFieldName;
					return true;
				}
			}
		}
		legacyFieldName = string.Empty;
		return false;
	}

	internal static bool TryReadLegacyFields(
		JObject source,
		out IReadOnlyDictionary<string, float> values,
		out string error)
	{
		Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.Ordinal);
		values = result;
		error = string.Empty;
		if (source == null)
		{
			error = "legacy effect JSON 不能为空";
			return false;
		}
		foreach (FieldBinding binding in Bindings)
		{
			JToken token = FindValue(source, binding.CanonicalFieldName);
			if (token == null)
			{
				foreach (string alias in binding.Aliases)
				{
					token = FindValue(source, alias);
					if (token != null)
					{
						break;
					}
				}
			}
			if (token == null || token.Type == JTokenType.Null)
			{
				continue;
			}
			if ((token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
				|| !TryReadFiniteFloat(token, out float value))
			{
				error = "legacy effect 字段不是有限数字: " + binding.CanonicalFieldName;
				return false;
			}
			if (Math.Abs(value) > 0.0001f)
			{
				if (!PolicyEffectModuleCatalog.TryResolveCanonicalId(binding.CanonicalFieldName, out string moduleId))
				{
					error = "legacy effect 字段没有已注册模块: " + binding.CanonicalFieldName;
					return false;
				}
				result[moduleId] = value;
			}
		}
		return true;
	}

	private static JToken FindValue(JObject source, string name)
	{
		return source.GetValue(name, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryReadFiniteFloat(JToken token, out float value)
	{
		value = 0f;
		try
		{
			value = token.Value<float>();
			return IsFinite(value);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static IReadOnlyDictionary<string, FieldBinding> BuildByFieldName()
	{
		Dictionary<string, FieldBinding> result = new Dictionary<string, FieldBinding>(StringComparer.OrdinalIgnoreCase);
		foreach (FieldBinding binding in Bindings)
		{
			result.Add(binding.CanonicalFieldName, binding);
			foreach (string alias in binding.Aliases)
			{
				result.Add(alias, binding);
			}
		}
		return result;
	}

	private static IReadOnlyList<string> BuildCanonicalModuleIds()
	{
		List<string> result = new List<string>(Bindings.Length);
		foreach (FieldBinding binding in Bindings)
		{
			if (!PolicyEffectModuleCatalog.TryResolveCanonicalId(binding.CanonicalFieldName, out string moduleId))
			{
				throw new InvalidOperationException("legacy effect 字段没有已注册模块: " + binding.CanonicalFieldName);
			}
			result.Add(moduleId);
		}
		return result.ToArray();
	}
}
