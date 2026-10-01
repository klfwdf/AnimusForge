using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace AnimusForge;

/// <summary>
/// Thin live adapter for selling one captured lord through the same action used by taverns.
/// Role, direct-reply and mutual-exclusion policy remain in the standalone GCCZ core.
/// </summary>
internal static class CastleAftermathLordSaleRuntimeBridge
{
	internal static CastleAftermathLordSaleApplyResult Apply(Hero capturedLord)
	{
		PartyBase mainParty = PartyBase.MainParty;
		CharacterObject character = capturedLord?.CharacterObject;
		if (capturedLord == null || character == null || Hero.MainHero == null || mainParty?.PrisonRoster == null)
		{
			return CastleAftermathLordSaleApplyResult.Failed("lord_sale_roster_unavailable");
		}
		if (!capturedLord.IsAlive || capturedLord == Hero.MainHero)
		{
			return CastleAftermathLordSaleApplyResult.Failed("lord_sale_target_invalid");
		}
		if (capturedLord.PartyBelongedToAsPrisoner != mainParty
			|| mainParty.PrisonRoster.FindIndexOfTroop(character) < 0
			|| !CastleAftermathRuntimeBridge.ContainsSelectedLord(capturedLord))
		{
			return CastleAftermathLordSaleApplyResult.Failed("lord_not_selected_main_party_prisoner");
		}

		try
		{
			TroopRoster sellable = MobilePartyHelper.GetPlayerPrisonersPlayerCanSell();
			if (sellable == null || sellable.FindIndexOfTroop(character) < 0)
			{
				return CastleAftermathLordSaleApplyResult.Failed("lord_prisoner_locked_from_ransom");
			}
			if (Campaign.Current?.Models?.RansomValueCalculationModel == null)
			{
				return CastleAftermathLordSaleApplyResult.Failed("ransom_value_model_unavailable");
			}

			int sourceIndex = mainParty.PrisonRoster.FindIndexOfTroop(character);
			TroopRosterElement sourceElement = mainParty.PrisonRoster.GetElementCopyAtIndex(sourceIndex);
			TroopRoster selected = TroopRoster.CreateDummyTroopRoster();
			selected.AddToCounts(
				character,
				1,
				false,
				sourceElement.WoundedNumber > 0 ? 1 : 0,
				sourceElement.Xp,
				true,
				-1);

			int expectedGold = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(
				character,
				mainParty.LeaderHero);
			int goldBefore = Hero.MainHero.Gold;
			try
			{
				SellPrisonersAction.ApplyForSelectedPrisoners(mainParty, null, selected);
			}
			catch (Exception ex)
			{
				if (capturedLord.PartyBelongedToAsPrisoner == mainParty
					&& mainParty.PrisonRoster.FindIndexOfTroop(character) >= 0)
				{
					Logger.Log("CastleAftermath", "Vanilla captured-lord sale failed before release. Hero="
						+ (capturedLord.StringId ?? "N/A") + ", Error=" + ex);
					return CastleAftermathLordSaleApplyResult.Failed(
						"vanilla_lord_sale_" + ex.GetType().Name);
				}
				Logger.Log("CastleAftermath", "Vanilla captured-lord sale threw after release; preserving decisive result. Hero="
					+ (capturedLord.StringId ?? "N/A") + ", Error=" + ex);
			}

			if (capturedLord.PartyBelongedToAsPrisoner == mainParty
				|| mainParty.PrisonRoster.FindIndexOfTroop(character) >= 0)
			{
				return CastleAftermathLordSaleApplyResult.Failed("vanilla_lord_sale_did_not_release_target");
			}

			int actualGold = Math.Max(0, Hero.MainHero.Gold - goldBefore);
			bool sceneResolved = CastleAftermathRuntimeBridge.ResolveLordPrisoner(
				capturedLord,
				"castle_sell_lord_vanilla");
			Logger.Log("CastleAftermath", "Sold captured lord through vanilla tavern action. Hero="
				+ (capturedLord.StringId ?? "N/A") + ", ExpectedGold=" + expectedGold
				+ ", ActualGold=" + actualGold + ", SceneResolved=" + sceneResolved);
			return CastleAftermathLordSaleApplyResult.Completed(actualGold, expectedGold, sceneResolved);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Captured-lord sale bridge failed. Hero="
				+ (capturedLord.StringId ?? "N/A") + ", Error=" + ex);
			return CastleAftermathLordSaleApplyResult.Failed(
				"lord_sale_bridge_" + ex.GetType().Name);
		}
	}
}

internal sealed class CastleAftermathLordSaleApplyResult
{
	private CastleAftermathLordSaleApplyResult(
		bool succeeded,
		int gold,
		int expectedGold,
		bool sceneResolved,
		string reasonCode)
	{
		Succeeded = succeeded;
		Gold = Math.Max(0, gold);
		ExpectedGold = Math.Max(0, expectedGold);
		SceneResolved = sceneResolved;
		ReasonCode = reasonCode ?? string.Empty;
	}

	internal bool Succeeded { get; }
	internal int Gold { get; }
	internal int ExpectedGold { get; }
	internal bool SceneResolved { get; }
	internal string ReasonCode { get; }

	internal static CastleAftermathLordSaleApplyResult Completed(int gold, int expectedGold, bool sceneResolved)
		=> new CastleAftermathLordSaleApplyResult(
			succeeded: true,
			gold,
			expectedGold,
			sceneResolved,
			"sold_via_vanilla_tavern_action");

	internal static CastleAftermathLordSaleApplyResult Failed(string reasonCode)
		=> new CastleAftermathLordSaleApplyResult(
			succeeded: false,
			gold: 0,
			expectedGold: 0,
			sceneResolved: false,
			reasonCode);
}
