using System;
using System.Collections.Generic;

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
		new SettlementBalanceDefinition(SettlementBalanceMetric.CityProsperity, "城市繁荣度", "繁荣度", 0, 50000, 10000),
		new SettlementBalanceDefinition(SettlementBalanceMetric.CastleProsperity, "城堡繁荣度", "繁荣度", 0, 50000, 3000),
		new SettlementBalanceDefinition(SettlementBalanceMetric.VillageHearth, "村庄户数", "户", 10, 10000, 1500),
		new SettlementBalanceDefinition(SettlementBalanceMetric.CityMilitia, "城市民兵总人数", "人", 0, 5000, 500),
		new SettlementBalanceDefinition(SettlementBalanceMetric.CastleMilitia, "城堡民兵总人数", "人", 0, 5000, 300),
		new SettlementBalanceDefinition(SettlementBalanceMetric.VillageMilitia, "村庄民兵总人数", "人", 0, 5000, 100),
		new SettlementBalanceDefinition(SettlementBalanceMetric.Food, "城市／城堡粮食库存容量", "粮食（最终容量）", 1, 10000, 1000, true)
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
				error = definition.Name + "必须在 " + definition.Minimum + "–" + definition.Maximum + " 之间。";
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
