using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public static class OrderOfBattleFormationExtensions
{
	public static void Refresh(this Formation formation)
	{
		if (formation != null)
		{
			MovementOrder readonlyMovementOrderReference = formation.GetReadonlyMovementOrderReference();
			if (readonlyMovementOrderReference.OrderEnum == MovementOrder.MovementOrderEnum.Stop)
			{
				OrderController.TryCancelStopOrder(formation);
			}
			formation.ApplyActionOnEachUnit(delegate(Agent u)
			{
				u.ForceUpdateCachedAndFormationValues(updateOnlyMovement: true, arrangementChangeAllowed: false);
			});
			formation.SetMovementOrder(readonlyMovementOrderReference);
		}
	}

	public static DeploymentFormationClass GetOrderOfBattleFormationClass(this FormationClass formationClass)
	{
		switch (formationClass)
		{
		case FormationClass.Infantry:
		case FormationClass.NumberOfDefaultFormations:
		case FormationClass.HeavyInfantry:
			return DeploymentFormationClass.Infantry;
		case FormationClass.Ranged:
			return DeploymentFormationClass.Ranged;
		case FormationClass.Cavalry:
		case FormationClass.LightCavalry:
		case FormationClass.HeavyCavalry:
			return DeploymentFormationClass.Cavalry;
		case FormationClass.HorseArcher:
			return DeploymentFormationClass.HorseArcher;
		default:
			return DeploymentFormationClass.Unset;
		}
	}

	public static List<FormationClass> GetFormationClasses(this DeploymentFormationClass orderOfBattleFormationClass)
	{
		List<FormationClass> list = new List<FormationClass>();
		switch (orderOfBattleFormationClass)
		{
		case DeploymentFormationClass.Infantry:
			list.Add(FormationClass.Infantry);
			break;
		case DeploymentFormationClass.Ranged:
			list.Add(FormationClass.Ranged);
			break;
		case DeploymentFormationClass.Cavalry:
			list.Add(FormationClass.Cavalry);
			break;
		case DeploymentFormationClass.HorseArcher:
			list.Add(FormationClass.HorseArcher);
			break;
		case DeploymentFormationClass.InfantryAndRanged:
			list.Add(FormationClass.Infantry);
			list.Add(FormationClass.Ranged);
			break;
		case DeploymentFormationClass.CavalryAndHorseArcher:
			list.Add(FormationClass.Cavalry);
			list.Add(FormationClass.HorseArcher);
			break;
		}
		return list;
	}

	public static TextObject GetFilterName(this FormationFilterType filterType)
	{
		return filterType switch
		{
			FormationFilterType.Shield => new TextObject("{=PSN8IaIg}Shields"), 
			FormationFilterType.Spear => new TextObject("{=f83FU4X6}Polearms"), 
			FormationFilterType.Thrown => new TextObject("{=Ea3K1PVR}Thrown Weapons"), 
			FormationFilterType.Heavy => new TextObject("{=Jw0GMgzv}Heavy Armors"), 
			FormationFilterType.HighTier => new TextObject("{=DzAkCzwd}High Tier"), 
			FormationFilterType.LowTier => new TextObject("{=qaPgbwZv}Low Tier"), 
			_ => new TextObject("{=w7Yrbi5t}Unset"), 
		};
	}

	public static TextObject GetFilterDescription(this FormationFilterType filterType)
	{
		return filterType switch
		{
			FormationFilterType.Unset => new TextObject("{=Q1Ga032B}Don't give preference to any type of troop."), 
			FormationFilterType.Shield => new TextObject("{=MVOPbhNj}Give preference to troops with Shields"), 
			FormationFilterType.Spear => new TextObject("{=K3Cr70PY}Give preference to troops with Polearms"), 
			FormationFilterType.Thrown => new TextObject("{=DWWa3aIb}Give preference to troops with Thrown Weapons"), 
			FormationFilterType.Heavy => new TextObject("{=ush8OHIw}Give preference to troops with Heavy Armors"), 
			FormationFilterType.HighTier => new TextObject("{=DRNDtkP2}Give preference to troops at higher tiers"), 
			FormationFilterType.LowTier => new TextObject("{=zbpCRmuJ}Give preference to troops at lower tiers"), 
			_ => new TextObject("{=w7Yrbi5t}Unset"), 
		};
	}

	public static TextObject GetClassName(this DeploymentFormationClass formationClass)
	{
		return formationClass switch
		{
			DeploymentFormationClass.Infantry => GameTexts.FindText("str_troop_type_name", "Infantry"), 
			DeploymentFormationClass.Ranged => GameTexts.FindText("str_troop_type_name", "Ranged"), 
			DeploymentFormationClass.Cavalry => GameTexts.FindText("str_troop_type_name", "Cavalry"), 
			DeploymentFormationClass.HorseArcher => GameTexts.FindText("str_troop_type_name", "HorseArcher"), 
			DeploymentFormationClass.InfantryAndRanged => new TextObject("{=mBDj5uG5}Infantry and Ranged"), 
			DeploymentFormationClass.CavalryAndHorseArcher => new TextObject("{=FNLfNWH3}Cavalry and Horse Archer"), 
			_ => new TextObject("{=w7Yrbi5t}Unset"), 
		};
	}

	public static List<Agent> GetHeroAgents(this Team team)
	{
		return team.ActiveAgents.Where((Agent a) => a.IsHero).ToList();
	}
}
