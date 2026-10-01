using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace AnimusForge;

internal static class CourierPartyTransitionRegistration
{
	private const string HarmonyId = "AnimusForge.courier.party-transition";
	private static bool _patchApplied;

	[ModuleInitializer]
	internal static void ModuleInit()
	{
		ApplyRegistrationPatch();
	}

	private static void ApplyRegistrationPatch()
	{
		if (_patchApplied)
		{
			return;
		}
		try
		{
			var target = AccessTools.Method(typeof(SubModule), "InitializeGameStarter", new[] { typeof(Game), typeof(IGameStarter) });
			if (target == null)
			{
				Logger.LogTrace("SubModule", ">>> Courier party transition registration target not found.");
				return;
			}
			new Harmony(HarmonyId).Patch(
				target,
				postfix: new HarmonyMethod(typeof(CourierPartyTransitionRegistration), nameof(InitializeGameStarterPostfix)));
			_patchApplied = true;
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Courier party transition registration patch failed: " + ex);
		}
	}

	public static void InitializeGameStarterPostfix(IGameStarter starterObject)
	{
		if (!(starterObject is CampaignGameStarter campaignGameStarter))
		{
			return;
		}
		try
		{
			PartyTransitionModel inner = null;
			foreach (GameModel model in campaignGameStarter.Models)
			{
				if (model is PartyTransitionModel transitionModel && !(transitionModel is CourierPartyTransitionModel))
				{
					inner = transitionModel;
				}
			}
			inner ??= new DefaultPartyTransitionModel();
			campaignGameStarter.AddModel<PartyTransitionModel>(new CourierPartyTransitionModel(inner));
			Logger.LogTrace("SubModule", ">>> Courier party transition model registered.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> Courier party transition model registration failed: " + ex);
		}
	}
}
