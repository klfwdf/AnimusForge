using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimusForge;

internal enum SettlementBalanceMetric
{
	CityProsperity,
	CastleProsperity,
	VillageHearth,
	CityMilitia,
	CastleMilitia,
	VillageMilitia,
	Food
}

internal sealed class SettlementBalanceDefinition
{
	internal SettlementBalanceDefinition(SettlementBalanceMetric metric, string name, string unit, int minimum, int maximum, int defaultValue, bool defaultEnabled = false)
	{
		Metric = metric;
		Name = name;
		Unit = unit;
		Minimum = minimum;
		Maximum = maximum;
		DefaultValue = defaultValue;
		DefaultEnabled = defaultEnabled;
	}
	internal SettlementBalanceMetric Metric { get; }
	internal string Id => Metric.ToString();
	internal string Name { get; }
	internal string Unit { get; }
	internal int Minimum { get; }
	internal int Maximum { get; }
	internal int DefaultValue { get; }
	internal bool DefaultEnabled { get; }
}

internal readonly struct SettlementBalanceLimit
{
	internal SettlementBalanceLimit(bool enabled, int value) { Enabled = enabled; Value = value; }
	internal bool Enabled { get; }
	internal int Value { get; }
}

internal static class SettlementBalanceRules
{
	internal static readonly IReadOnlyList<SettlementBalanceDefinition> Definitions = Array.AsReadOnly(new[]
	{
		new SettlementBalanceDefinition(SettlementBalanceMetric.CityProsperity, PolicyUiTexts.Get("AFPOL_UI_CityProsperity", "City Prosperity"), PolicyUiTexts.Get("AFPOL_UI_ProsperityUnit", "Prosperity"), 0, 50000, 10000),
		new SettlementBalanceDefinition(SettlementBalanceMetric.CastleProsperity, PolicyUiTexts.Get("AFPOL_UI_CastleProsperity", "Castle Prosperity"), PolicyUiTexts.Get("AFPOL_UI_ProsperityUnit", "Prosperity"), 0, 50000, 3000),
		new SettlementBalanceDefinition(SettlementBalanceMetric.VillageHearth, PolicyUiTexts.Get("AFPOL_UI_VillageHearth", "Village Hearth"), PolicyUiTexts.Get("AFPOL_UI_HearthUnit", "Hearth"), 10, 10000, 1500),
		new SettlementBalanceDefinition(SettlementBalanceMetric.CityMilitia, PolicyUiTexts.Get("AFPOL_UI_CityMilitia", "City Militia"), PolicyUiTexts.Get("AFPOL_UI_PersonUnit", "people"), 0, 5000, 500),
		new SettlementBalanceDefinition(SettlementBalanceMetric.CastleMilitia, PolicyUiTexts.Get("AFPOL_UI_CastleMilitia", "Castle Militia"), PolicyUiTexts.Get("AFPOL_UI_PersonUnit", "people"), 0, 5000, 300),
		new SettlementBalanceDefinition(SettlementBalanceMetric.VillageMilitia, PolicyUiTexts.Get("AFPOL_UI_VillageMilitia", "Village Militia"), PolicyUiTexts.Get("AFPOL_UI_PersonUnit", "people"), 0, 5000, 100),
		new SettlementBalanceDefinition(SettlementBalanceMetric.Food, PolicyUiTexts.Get("AFPOL_UI_FoodCapacity", "City/Castle Food Capacity"), PolicyUiTexts.Get("AFPOL_UI_FoodUnit", "Food (final capacity)"), 1, 10000, 1000, true)
	});

	internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

	internal static float LimitDelta(float current, float netDelta, bool enabled, int cap)
	{
		if (!enabled || netDelta <= 0f || !IsFinite(current) || !IsFinite(netDelta)) return netDelta;
		return Math.Min(netDelta, Math.Max(0f, cap - current));
	}

	internal static float LimitWrite(float current, float proposed, bool enabled, int cap)
	{
		if (!enabled || proposed <= current || !IsFinite(current) || !IsFinite(proposed)) return proposed;
		return Math.Min(proposed, Math.Max(current, cap));
	}

	// Native daily food clipping must not discard stock which predates a lower cap.
	internal static int DailyFoodCapacity(int capacity, float startingStock, bool enabled)
	{
		if (!enabled || !IsFinite(startingStock) || startingStock <= capacity) return capacity;
		return (int)Math.Min(int.MaxValue, Math.Ceiling((double)startingStock));
	}
}

internal sealed class SettlementBalanceSnapshot
{
	private readonly SettlementBalanceLimit[] _limits;
	internal static readonly SettlementBalanceSnapshot Default = CreateDefault();
	private SettlementBalanceSnapshot(SettlementBalanceLimit[] limits) { _limits = limits; }
	internal SettlementBalanceLimit Get(SettlementBalanceMetric metric) => _limits[(int)metric];
	internal SettlementBalanceSnapshot With(SettlementBalanceMetric metric, bool enabled, int value)
	{
		var copy = (SettlementBalanceLimit[])_limits.Clone();
		copy[(int)metric] = new SettlementBalanceLimit(enabled, value);
		return new SettlementBalanceSnapshot(copy);
	}
	internal bool TryValidate(out string error)
	{
		foreach (var definition in SettlementBalanceRules.Definitions)
		{
			int value = Get(definition.Metric).Value;
			if (value < definition.Minimum || value > definition.Maximum)
			{
				error = PolicyUiTexts.Format(
					"AFPOL_UI_BalanceValueOutOfRange",
					"{NAME} must be between {MIN} and {MAX}.",
					"NAME", definition.Name,
					"MIN", definition.Minimum.ToString(CultureInfo.InvariantCulture),
					"MAX", definition.Maximum.ToString(CultureInfo.InvariantCulture));
				return false;
			}
		}
		error = string.Empty;
		return true;
	}
	private static SettlementBalanceSnapshot CreateDefault()
	{
		var limits = new SettlementBalanceLimit[SettlementBalanceRules.Definitions.Count];
		foreach (var definition in SettlementBalanceRules.Definitions)
			limits[(int)definition.Metric] = new SettlementBalanceLimit(definition.DefaultEnabled, definition.DefaultValue);
		return new SettlementBalanceSnapshot(limits);
	}
}
