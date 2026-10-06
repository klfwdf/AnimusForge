using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents;

public class SandboxBattleSpawnModel : BattleSpawnModel
{
	private enum OrderOfBattleInnerClassType
	{
		None,
		PrimaryClass,
		SecondaryClass
	}

	private struct FormationOrderOfBattleConfiguration
	{
		public DeploymentFormationClass OOBFormationClass;

		public FormationClass PrimaryFormationClass;

		public int PrimaryClassTroopCount;

		public int PrimaryClassDesiredTroopCount;

		public FormationClass SecondaryFormationClass;

		public int SecondaryClassTroopCount;

		public int SecondaryClassDesiredTroopCount;

		public Hero Captain;
	}

	public override void OnMissionStart()
	{
		MissionReinforcementsHelper.OnMissionStart();
	}

	public override void OnMissionEnd()
	{
		MissionReinforcementsHelper.OnMissionEnd();
	}

	public override List<(IAgentOriginBase origin, int formationIndex)> GetInitialSpawnAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
	{
		List<(IAgentOriginBase, int)> list = new List<(IAgentOriginBase, int)>();
		if (GetOrderOfBattleConfigurationsForFormations(battleSide, troopOrigins, out var formationOrderOfBattleConfigurations))
		{
			foreach (IAgentOriginBase troopOrigin in troopOrigins)
			{
				OrderOfBattleInnerClassType bestClassInnerClassType;
				FormationClass formationClass = FindBestOrderOfBattleFormationClassAssignmentForTroop(battleSide, troopOrigin, formationOrderOfBattleConfigurations, out bestClassInnerClassType);
				(IAgentOriginBase, int) item = (troopOrigin, (int)formationClass);
				list.Add(item);
				switch (bestClassInnerClassType)
				{
				case OrderOfBattleInnerClassType.PrimaryClass:
					formationOrderOfBattleConfigurations[(int)formationClass].PrimaryClassTroopCount++;
					break;
				case OrderOfBattleInnerClassType.SecondaryClass:
					formationOrderOfBattleConfigurations[(int)formationClass].SecondaryClassTroopCount++;
					break;
				}
			}
		}
		else
		{
			foreach (IAgentOriginBase troopOrigin2 in troopOrigins)
			{
				(IAgentOriginBase, int) item2 = (troopOrigin2, (int)Mission.Current.GetAgentTroopClass(battleSide, troopOrigin2.Troop));
				list.Add(item2);
			}
		}
		return list;
	}

	public override List<(IAgentOriginBase origin, int formationIndex)> GetReinforcementAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
	{
		return MissionReinforcementsHelper.GetReinforcementAssignments(battleSide, troopOrigins);
	}

	private static bool GetOrderOfBattleConfigurationsForFormations(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins, out FormationOrderOfBattleConfiguration[] formationOrderOfBattleConfigurations)
	{
		formationOrderOfBattleConfigurations = new FormationOrderOfBattleConfiguration[8];
		OrderOfBattleCampaignBehavior orderOfBattleCampaignBehavior = Campaign.Current?.GetCampaignBehavior<OrderOfBattleCampaignBehavior>();
		if (orderOfBattleCampaignBehavior == null)
		{
			return false;
		}
		for (int i = 0; i < 8; i++)
		{
			if (orderOfBattleCampaignBehavior.GetFormationDataAtIndex(i, Mission.Current.IsSiegeBattle, MobileParty.MainParty?.Army != null) == null)
			{
				return false;
			}
		}
		int[] array = CalculateTroopCountsPerDefaultFormation(battleSide, troopOrigins);
		for (int j = 0; j < 8; j++)
		{
			OrderOfBattleCampaignBehavior.OrderOfBattleFormationData formationDataAtIndex = orderOfBattleCampaignBehavior.GetFormationDataAtIndex(j, Mission.Current.IsSiegeBattle, MobileParty.MainParty?.Army != null);
			formationOrderOfBattleConfigurations[j].OOBFormationClass = formationDataAtIndex.FormationClass;
			formationOrderOfBattleConfigurations[j].Captain = formationDataAtIndex.Captain;
			FormationClass formationClass = FormationClass.NumberOfAllFormations;
			FormationClass formationClass2 = FormationClass.NumberOfAllFormations;
			switch (formationDataAtIndex.FormationClass)
			{
			case DeploymentFormationClass.Infantry:
				formationClass = FormationClass.Infantry;
				break;
			case DeploymentFormationClass.Ranged:
				formationClass = FormationClass.Ranged;
				break;
			case DeploymentFormationClass.Cavalry:
				formationClass = FormationClass.Cavalry;
				break;
			case DeploymentFormationClass.HorseArcher:
				formationClass = FormationClass.HorseArcher;
				break;
			case DeploymentFormationClass.InfantryAndRanged:
				formationClass = FormationClass.Infantry;
				formationClass2 = FormationClass.Ranged;
				break;
			case DeploymentFormationClass.CavalryAndHorseArcher:
				formationClass = FormationClass.Cavalry;
				formationClass2 = FormationClass.HorseArcher;
				break;
			}
			formationOrderOfBattleConfigurations[j].PrimaryFormationClass = formationClass;
			if (formationClass != FormationClass.NumberOfAllFormations)
			{
				formationOrderOfBattleConfigurations[j].PrimaryClassDesiredTroopCount = (int)Math.Ceiling((float)array[(int)formationClass] * ((float)formationDataAtIndex.PrimaryClassWeight / 100f));
			}
			formationOrderOfBattleConfigurations[j].SecondaryFormationClass = formationClass2;
			if (formationClass2 != FormationClass.NumberOfAllFormations)
			{
				formationOrderOfBattleConfigurations[j].SecondaryClassDesiredTroopCount = (int)Math.Ceiling((float)array[(int)formationClass2] * ((float)formationDataAtIndex.SecondaryClassWeight / 100f));
			}
		}
		return true;
	}

	private static int[] CalculateTroopCountsPerDefaultFormation(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
	{
		int[] array = new int[4];
		foreach (IAgentOriginBase troopOrigin in troopOrigins)
		{
			FormationClass formationClass = Mission.Current.GetAgentTroopClass(battleSide, troopOrigin.Troop).DefaultClass();
			array[(int)formationClass]++;
		}
		return array;
	}

	private static FormationClass FindBestOrderOfBattleFormationClassAssignmentForTroop(BattleSideEnum battleSide, IAgentOriginBase origin, FormationOrderOfBattleConfiguration[] formationOrderOfBattleConfigurations, out OrderOfBattleInnerClassType bestClassInnerClassType)
	{
		FormationClass formationClass = Mission.Current.GetAgentTroopClass(battleSide, origin.Troop).DefaultClass();
		FormationClass result = formationClass;
		float num = float.MinValue;
		bestClassInnerClassType = OrderOfBattleInnerClassType.None;
		for (int i = 0; i < 8; i++)
		{
			if (origin.Troop.IsHero && origin.Troop is CharacterObject characterObject && characterObject.HeroObject == formationOrderOfBattleConfigurations[i].Captain)
			{
				result = (FormationClass)i;
				bestClassInnerClassType = OrderOfBattleInnerClassType.None;
				break;
			}
			if (formationClass == formationOrderOfBattleConfigurations[i].PrimaryFormationClass)
			{
				float num2 = formationOrderOfBattleConfigurations[i].PrimaryClassDesiredTroopCount;
				float num3 = formationOrderOfBattleConfigurations[i].PrimaryClassTroopCount;
				float num4 = 1f - num3 / (num2 + 1f);
				if (num4 > num)
				{
					result = (FormationClass)i;
					bestClassInnerClassType = OrderOfBattleInnerClassType.PrimaryClass;
					num = num4;
				}
			}
			else if (formationClass == formationOrderOfBattleConfigurations[i].SecondaryFormationClass)
			{
				float num5 = formationOrderOfBattleConfigurations[i].SecondaryClassDesiredTroopCount;
				float num6 = formationOrderOfBattleConfigurations[i].SecondaryClassTroopCount;
				float num7 = 1f - num6 / (num5 + 1f);
				if (num7 > num)
				{
					result = (FormationClass)i;
					bestClassInnerClassType = OrderOfBattleInnerClassType.SecondaryClass;
					num = num7;
				}
			}
		}
		return result;
	}
}
