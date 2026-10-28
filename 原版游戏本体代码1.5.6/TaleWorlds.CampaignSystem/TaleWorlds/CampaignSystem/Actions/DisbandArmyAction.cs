using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Actions;

public static class DisbandArmyAction
{
	private static void ApplyInternal(Army army, Army.ArmyDispersionReason reason)
	{
		if (reason == Army.ArmyDispersionReason.DismissalRequestedWithInfluence)
		{
			DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -diplomacyModel.GetInfluenceCostOfDisbandingArmy());
			foreach (MobileParty item in army.Parties.ToList())
			{
				if (item != MobileParty.MainParty && item.LeaderHero != null)
				{
					ChangeRelationAction.ApplyPlayerRelation(item.LeaderHero, diplomacyModel.GetRelationCostOfDisbandingArmy(item == item.Army.LeaderParty));
				}
			}
		}
		army.DisperseInternal(reason);
	}

	public static void ApplyByReleasedByPlayerAfterBattle(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.DismissalRequestedWithInfluence);
	}

	public static void ApplyByArmyLeaderIsDead(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.ArmyLeaderIsDead);
	}

	public static void ApplyByNotEnoughParty(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.NotEnoughParty);
	}

	public static void ApplyByObjectiveFinished(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.ObjectiveFinished);
	}

	public static void ApplyByPlayerTakenPrisoner(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.PlayerTakenPrisoner);
	}

	public static void ApplyByFoodProblem(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.FoodProblem);
	}

	public static void ApplyByInactivity(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.Inactivity);
	}

	public static void ApplyByCohesionDepleted(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.CohesionDepleted);
	}

	public static void ApplyByNoActiveWar(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.NoActiveWar);
	}

	public static void ApplyByUnknownReason(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.Unknown);
	}

	public static void ApplyByLeaderPartyRemoved(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.LeaderPartyRemoved);
	}

	public static void ApplyByNoShip(Army army)
	{
		ApplyInternal(army, Army.ArmyDispersionReason.NoShipToUse);
	}
}
