using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SandBox.AdvancedStartOptions;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.AdvancedStartOptions;

[UsedImplicitly]
public static class SandBoxStartOptionsProvider
{
	private const string KingdomSturgiaId = "sturgia";

	private const string KingdomVlandiaId = "vlandia";

	private const string KingdomBattaniaId = "battania";

	private const string KingdomNorthernEmpireId = "empire";

	private const string KingdomWesternEmpireId = "empire_w";

	private const string KingdomSouthernEmpireId = "empire_s";

	private const string KingdomKhuzaitId = "khuzait";

	private const string KingdomAseraiId = "aserai";

	[UsedImplicitly]
	[StartOptionsProvider]
	private static void AddStartOptions(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		options.Add(new BooleanAdvancedStartOption("EnableFastMode", "general", null));
		options.Add(new UIntAdvancedStartOption("Seed", "general", 0u, uint.MaxValue, null, (uint)Environment.TickCount));
		options.Add(new ListAdvancedStartOption("Scenario", "worldscenarios", new List<(string, ListAdvancedStartOption.ListItemCondition)>
		{
			("none", GetEnabledItem),
			("unitedempire", GetEnabledItem),
			("LastStand", GetEnabledItem),
			("twofactionwar", GetEnabledItem),
			("InvasionId", GetEnabledItem),
			("alternativecalradia", GetEnabledItem)
		}, null, "none"));
		options.Add(new ListAdvancedStartOption("UnitedEmpireUnifierKingdomId", "worldscenarios", GetImperialCultureItems(), MakeOptionHiddenCondition("UnitedEmpireUnifierKingdomId", (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => GetScenario(o) != "unitedempire")));
		options.Add(new ListAdvancedStartOption("LastStandKingdomId", "worldscenarios", GetCultureItems(), MakeOptionHiddenCondition("LastStandKingdomId", (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => GetScenario(o) != "LastStand")));
		options.Add(new ListAdvancedStartOption("InvasionScenarioFactionId", "worldscenarios", GetCultureItems(), MakeOptionHiddenCondition("InvasionScenarioFactionId", (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => GetScenario(o) != "InvasionId")));
		options.Add(new ListAdvancedStartOption("TwoFactionWarFaction1Id", "worldscenarios", GetCultureItems(GetFirstFactionIsDisabled), MakeOptionHiddenCondition("TwoFactionWarFaction1Id", (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => GetScenario(o) != "twofactionwar")));
		options.Add(new ListAdvancedStartOption("TwoFactionWarFaction2Id", "worldscenarios", GetCultureItems(GetSecondFactionIsDisabled), MakeOptionHiddenCondition("TwoFactionWarFaction2Id", (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => GetScenario(o) != "twofactionwar")));
		options.Add(new ListAdvancedStartOption("AlternativeCalradiaVariantId", "worldscenarios", new List<(string, ListAdvancedStartOption.ListItemCondition)>
		{
			("alternativecalradiadefault", GetEnabledItem),
			("alternativecalradiafractured", GetEnabledItem),
			("alternativecalradiashattered", GetEnabledItem)
		}, MakeOptionHiddenCondition("AlternativeCalradiaVariantId", (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => GetScenario(o) != "alternativecalradia")));
		options.Add(new ListAdvancedStartOption("StartType", "scenarios", new List<(string, ListAdvancedStartOption.ListItemCondition)>
		{
			("default", GetEnabledItem),
			("king", GetKingStartCondition),
			("vassal", GetVassalStartCondition),
			("mercenary", GetMercenaryStartCondition),
			("trader", GetEnabledItem),
			("outlaw", GetEnabledItem),
			("beggar", GetEnabledItem)
		}, null, "default"));
		options.Add(new ListAdvancedStartOption("KingdomId", "scenarios", GetCultureItems(GetStartingKingdomIsDisabled), GetStartingKingdomCondition));
		options.Add(new BooleanAdvancedStartOption("RisenBanditsMultiplier", "globalmodifiers", null));
		options.Add(new BooleanAdvancedStartOption("HighRebellion", "globalmodifiers", null));
		options.Add(new BooleanAdvancedStartOption("RecruitmentRate", "globalmodifiers", null));
		options.Add(new BooleanAdvancedStartOption("IncreasedGlobalMovementSpeed", "globalmodifiers", null));
	}

	private static AdvancedStartOption.AdvancedStartOptionCondition MakeOptionHiddenCondition(string optionId, Func<SandBox.AdvancedStartOptions.AdvancedStartOptions, bool> getIsHidden)
	{
		return (SandBox.AdvancedStartOptions.AdvancedStartOptions o) => getIsHidden(o);
	}

	private static TextObject GetLockedReason(string itemId)
	{
		return Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_locked_reason", itemId);
	}

	private static bool GetStartingKingdomCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		string startType = GetStartType(options);
		if (startType != "king" && startType != "vassal" && startType != "mercenary")
		{
			return startType != "fleetadmiral";
		}
		return false;
	}

	private static bool GetEnabledItem(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		return false;
	}

	private static bool GetKingStartCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = GetKingPlaythroughLockedReason();
		return !BannerlordConfig.CompletedKingPlaythrough;
	}

	private static bool GetVassalStartCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = GetVassalPlaythroughLockedReason();
		return !BannerlordConfig.CompletedVassalPlaythrough;
	}

	private static bool GetMercenaryStartCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = GetMercenaryPlaythroughLockedReason();
		return !BannerlordConfig.CompletedMercenaryPlaythrough;
	}

	private static bool GetSecondFactionIsDisabled(SandBox.AdvancedStartOptions.AdvancedStartOptions o, string identifier)
	{
		return identifier == GetTwoFactionWarFaction1Id(o);
	}

	private static bool GetFirstFactionIsDisabled(SandBox.AdvancedStartOptions.AdvancedStartOptions o, string identifier)
	{
		return identifier == GetTwoFactionWarFaction2Id(o);
	}

	private static TextObject GetKingPlaythroughLockedReason()
	{
		return GetLockedReason("king");
	}

	private static TextObject GetVassalPlaythroughLockedReason()
	{
		return GetLockedReason("vassal");
	}

	private static TextObject GetMercenaryPlaythroughLockedReason()
	{
		return GetLockedReason("mercenary");
	}

	private static ListAdvancedStartOption.ListItemCondition MakeCultureItem(Func<SandBox.AdvancedStartOptions.AdvancedStartOptions, string, bool> getIsDisabled, string identifier)
	{
		return delegate(SandBox.AdvancedStartOptions.AdvancedStartOptions o, out TextObject disabledText)
		{
			disabledText = null;
			return getIsDisabled?.Invoke(o, identifier) ?? false;
		};
	}

	private static List<(string, ListAdvancedStartOption.ListItemCondition)> GetCultureItems(Func<SandBox.AdvancedStartOptions.AdvancedStartOptions, string, bool> getIsDisabled = null)
	{
		return new List<(string, ListAdvancedStartOption.ListItemCondition)>
		{
			("sturgia", MakeCultureItem(getIsDisabled, "sturgia")),
			("vlandia", MakeCultureItem(getIsDisabled, "vlandia")),
			("battania", MakeCultureItem(getIsDisabled, "battania")),
			("empire", MakeCultureItem(getIsDisabled, "empire")),
			("empire_w", MakeCultureItem(getIsDisabled, "empire_w")),
			("empire_s", MakeCultureItem(getIsDisabled, "empire_s")),
			("khuzait", MakeCultureItem(getIsDisabled, "khuzait")),
			("aserai", MakeCultureItem(getIsDisabled, "aserai"))
		};
	}

	private static bool GetStartingKingdomIsDisabled(SandBox.AdvancedStartOptions.AdvancedStartOptions o, string identifier)
	{
		string scenario = GetScenario(o);
		bool flag = scenario == "twofactionwar";
		bool flag2 = scenario == "unitedempire";
		bool flag3 = identifier == "empire_s" || identifier == "empire" || identifier == "empire_w";
		if (!flag || !(identifier != GetTwoFactionWarFaction1Id(o)) || !(identifier != GetTwoFactionWarFaction2Id(o)))
		{
			return flag2 && identifier != GetUnitedEmpireUnifierKingdomId(o) && flag3;
		}
		return true;
	}

	private static List<(string, ListAdvancedStartOption.ListItemCondition)> GetImperialCultureItems()
	{
		return new List<(string, ListAdvancedStartOption.ListItemCondition)>
		{
			("empire", MakeCultureItem(null, "empire")),
			("empire_w", MakeCultureItem(null, "empire_w")),
			("empire_s", MakeCultureItem(null, "empire_s"))
		};
	}

	private static string GetTwoFactionWarFaction1Id(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("TwoFactionWarFaction1Id").GetValue<string>();
	}

	private static string GetUnitedEmpireUnifierKingdomId(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("UnitedEmpireUnifierKingdomId").GetValue<string>();
	}

	private static string GetTwoFactionWarFaction2Id(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("TwoFactionWarFaction2Id").GetValue<string>();
	}

	private static string GetScenario(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("Scenario").GetValue<string>();
	}

	private static string GetStartType(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("StartType").GetValue<string>();
	}
}
