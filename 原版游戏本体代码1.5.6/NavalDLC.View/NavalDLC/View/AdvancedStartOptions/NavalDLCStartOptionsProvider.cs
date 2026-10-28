using JetBrains.Annotations;
using SandBox.AdvancedStartOptions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.View.AdvancedStartOptions;

public static class NavalDLCStartOptionsProvider
{
	private const string NordKingdomId = "nord";

	[UsedImplicitly]
	[StartOptionsProvider]
	private static void AddStartOptions(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		ListAdvancedStartOption option = options.GetOption<ListAdvancedStartOption>("StartType");
		if (option != null)
		{
			option.AddItem((Identifier: "fleetadmiral", Condition: GetFleetAdmiralCondition));
			option.AddItem((Identifier: "merchantventurer", Condition: GetMerchantVenturerCondition));
		}
		else
		{
			Debug.FailedAssert("StartType option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 27);
		}
		ListAdvancedStartOption option2 = options.GetOption<ListAdvancedStartOption>("Scenario");
		if (option2 != null)
		{
			option2.AddItem((Identifier: "nordinvasion", Condition: GetNordInvasionCondition));
		}
		else
		{
			Debug.FailedAssert("scenario option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 37);
		}
		ListAdvancedStartOption option3 = options.GetOption<ListAdvancedStartOption>("KingdomId");
		if (option3 != null)
		{
			option3.AddItem((Identifier: "nord", Condition: GetNordKingdomCondition));
		}
		else
		{
			Debug.FailedAssert("KingdomId option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 47);
		}
		ListAdvancedStartOption option4 = options.GetOption<ListAdvancedStartOption>("LastStandKingdomId");
		if (option4 != null)
		{
			option4.AddItem((Identifier: "nord", Condition: GetNordCultureCondition));
		}
		else
		{
			Debug.FailedAssert("LastStandKingdomId option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 57);
		}
		ListAdvancedStartOption option5 = options.GetOption<ListAdvancedStartOption>("InvasionScenarioFactionId");
		if (option5 != null)
		{
			option5.AddItem((Identifier: "nord", Condition: GetNordCultureCondition));
		}
		else
		{
			Debug.FailedAssert("invaderFaction option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 67);
		}
		ListAdvancedStartOption option6 = options.GetOption<ListAdvancedStartOption>("TwoFactionWarFaction1Id");
		if (option6 != null)
		{
			option6.AddItem((Identifier: "nord", Condition: GetNordFaction1Condition));
		}
		else
		{
			Debug.FailedAssert("twoFaction1 option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 77);
		}
		ListAdvancedStartOption option7 = options.GetOption<ListAdvancedStartOption>("TwoFactionWarFaction2Id");
		if (option7 != null)
		{
			option7.AddItem((Identifier: "nord", Condition: GetNordFaction2Condition));
		}
		else
		{
			Debug.FailedAssert("twoFaction2 option not found for naval contribution", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\AdvancedStartOptions\\NavalDLCStartOptionsProvider.cs", "AddStartOptions", 87);
		}
		options.Add(new BooleanAdvancedStartOption("PersonalShip", "globalmodifiers", GetPersonalShipCondition));
	}

	private static bool GetFleetAdmiralCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = GetVassalPlaythroughLockedReason();
		return !BannerlordConfig.CompletedVassalPlaythrough;
	}

	private static bool GetMerchantVenturerCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		return false;
	}

	private static bool GetNordInvasionCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		return false;
	}

	private static bool GetNordCultureCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		return false;
	}

	private static bool GetNordKingdomCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		if (!(GetScenario(options) != "twofactionwar") && !("nord" == GetTwoFactionWarFaction1Id(options)))
		{
			return !("nord" == GetTwoFactionWarFaction2Id(options));
		}
		return false;
	}

	private static bool GetNordFaction1Condition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		return GetTwoFactionWarFaction2Id(options) == "nord";
	}

	private static bool GetNordFaction2Condition(SandBox.AdvancedStartOptions.AdvancedStartOptions options, out TextObject disabledText)
	{
		disabledText = null;
		return GetTwoFactionWarFaction1Id(options) == "nord";
	}

	private static bool GetPersonalShipCondition(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return false;
	}

	private static TextObject GetVassalPlaythroughLockedReason()
	{
		return Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_locked_reason", "vassal");
	}

	private static string GetTwoFactionWarFaction1Id(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("TwoFactionWarFaction1Id").GetValue<string>();
	}

	private static string GetTwoFactionWarFaction2Id(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("TwoFactionWarFaction2Id").GetValue<string>();
	}

	private static string GetScenario(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
	{
		return options.GetOption("Scenario").GetValue<string>();
	}
}
