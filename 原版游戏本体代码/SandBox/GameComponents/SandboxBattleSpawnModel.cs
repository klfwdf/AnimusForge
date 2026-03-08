using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents
{
	// Token: 0x020000C2 RID: 194
	public class SandboxBattleSpawnModel : BattleSpawnModel
	{
		// Token: 0x060007FF RID: 2047 RVA: 0x0003978B File Offset: 0x0003798B
		public override void OnMissionStart()
		{
			MissionReinforcementsHelper.OnMissionStart();
		}

		// Token: 0x06000800 RID: 2048 RVA: 0x00039792 File Offset: 0x00037992
		public override void OnMissionEnd()
		{
			MissionReinforcementsHelper.OnMissionEnd();
		}

		// Token: 0x06000801 RID: 2049 RVA: 0x0003979C File Offset: 0x0003799C
		[return: TupleElementNames(new string[] { "origin", "formationIndex" })]
		public override List<ValueTuple<IAgentOriginBase, int>> GetInitialSpawnAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
		{
			List<ValueTuple<IAgentOriginBase, int>> list = new List<ValueTuple<IAgentOriginBase, int>>();
			SandboxBattleSpawnModel.FormationOrderOfBattleConfiguration[] array;
			if (SandboxBattleSpawnModel.GetOrderOfBattleConfigurationsForFormations(battleSide, troopOrigins, out array))
			{
				using (List<IAgentOriginBase>.Enumerator enumerator = troopOrigins.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						IAgentOriginBase agentOriginBase = enumerator.Current;
						SandboxBattleSpawnModel.OrderOfBattleInnerClassType orderOfBattleInnerClassType;
						FormationClass formationClass = SandboxBattleSpawnModel.FindBestOrderOfBattleFormationClassAssignmentForTroop(battleSide, agentOriginBase, array, out orderOfBattleInnerClassType);
						ValueTuple<IAgentOriginBase, int> item = new ValueTuple<IAgentOriginBase, int>(agentOriginBase, (int)formationClass);
						list.Add(item);
						if (orderOfBattleInnerClassType == SandboxBattleSpawnModel.OrderOfBattleInnerClassType.PrimaryClass)
						{
							SandboxBattleSpawnModel.FormationOrderOfBattleConfiguration[] array2 = array;
							FormationClass formationClass2 = formationClass;
							array2[(int)formationClass2].PrimaryClassTroopCount = array2[(int)formationClass2].PrimaryClassTroopCount + 1;
						}
						else if (orderOfBattleInnerClassType == SandboxBattleSpawnModel.OrderOfBattleInnerClassType.SecondaryClass)
						{
							SandboxBattleSpawnModel.FormationOrderOfBattleConfiguration[] array3 = array;
							FormationClass formationClass3 = formationClass;
							array3[(int)formationClass3].SecondaryClassTroopCount = array3[(int)formationClass3].SecondaryClassTroopCount + 1;
						}
					}
					return list;
				}
			}
			foreach (IAgentOriginBase agentOriginBase2 in troopOrigins)
			{
				ValueTuple<IAgentOriginBase, int> item2 = new ValueTuple<IAgentOriginBase, int>(agentOriginBase2, (int)Mission.Current.GetAgentTroopClass(battleSide, agentOriginBase2.Troop));
				list.Add(item2);
			}
			return list;
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x000398A0 File Offset: 0x00037AA0
		[return: TupleElementNames(new string[] { "origin", "formationIndex" })]
		public override List<ValueTuple<IAgentOriginBase, int>> GetReinforcementAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
		{
			return MissionReinforcementsHelper.GetReinforcementAssignments(battleSide, troopOrigins);
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x000398AC File Offset: 0x00037AAC
		private static bool GetOrderOfBattleConfigurationsForFormations(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins, out SandboxBattleSpawnModel.FormationOrderOfBattleConfiguration[] formationOrderOfBattleConfigurations)
		{
			formationOrderOfBattleConfigurations = new SandboxBattleSpawnModel.FormationOrderOfBattleConfiguration[8];
			Campaign campaign = Campaign.Current;
			OrderOfBattleCampaignBehavior orderOfBattleCampaignBehavior = ((campaign != null) ? campaign.GetCampaignBehavior<OrderOfBattleCampaignBehavior>() : null);
			if (orderOfBattleCampaignBehavior == null)
			{
				return false;
			}
			for (int i = 0; i < 8; i++)
			{
				OrderOfBattleCampaignBehavior orderOfBattleCampaignBehavior2 = orderOfBattleCampaignBehavior;
				int formationIndex = i;
				bool isSiegeBattle = Mission.Current.IsSiegeBattle;
				MobileParty mainParty = MobileParty.MainParty;
				if (orderOfBattleCampaignBehavior2.GetFormationDataAtIndex(formationIndex, isSiegeBattle, ((mainParty != null) ? mainParty.Army : null) != null) == null)
				{
					return false;
				}
			}
			int[] array = SandboxBattleSpawnModel.CalculateTroopCountsPerDefaultFormation(battleSide, troopOrigins);
			for (int j = 0; j < 8; j++)
			{
				OrderOfBattleCampaignBehavior orderOfBattleCampaignBehavior3 = orderOfBattleCampaignBehavior;
				int formationIndex2 = j;
				bool isSiegeBattle2 = Mission.Current.IsSiegeBattle;
				MobileParty mainParty2 = MobileParty.MainParty;
				OrderOfBattleCampaignBehavior.OrderOfBattleFormationData formationDataAtIndex = orderOfBattleCampaignBehavior3.GetFormationDataAtIndex(formationIndex2, isSiegeBattle2, ((mainParty2 != null) ? mainParty2.Army : null) != null);
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
					formationOrderOfBattleConfigurations[j].PrimaryClassDesiredTroopCount = (int)Math.Ceiling((double)((float)array[(int)formationClass] * ((float)formationDataAtIndex.PrimaryClassWeight / 100f)));
				}
				formationOrderOfBattleConfigurations[j].SecondaryFormationClass = formationClass2;
				if (formationClass2 != FormationClass.NumberOfAllFormations)
				{
					formationOrderOfBattleConfigurations[j].SecondaryClassDesiredTroopCount = (int)Math.Ceiling((double)((float)array[(int)formationClass2] * ((float)formationDataAtIndex.SecondaryClassWeight / 100f)));
				}
			}
			return true;
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x00039A48 File Offset: 0x00037C48
		private static int[] CalculateTroopCountsPerDefaultFormation(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
		{
			int[] array = new int[4];
			foreach (IAgentOriginBase agentOriginBase in troopOrigins)
			{
				FormationClass formationClass = Mission.Current.GetAgentTroopClass(battleSide, agentOriginBase.Troop).DefaultClass();
				array[(int)formationClass]++;
			}
			return array;
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x00039ABC File Offset: 0x00037CBC
		private static FormationClass FindBestOrderOfBattleFormationClassAssignmentForTroop(BattleSideEnum battleSide, IAgentOriginBase origin, SandboxBattleSpawnModel.FormationOrderOfBattleConfiguration[] formationOrderOfBattleConfigurations, out SandboxBattleSpawnModel.OrderOfBattleInnerClassType bestClassInnerClassType)
		{
			FormationClass formationClass = Mission.Current.GetAgentTroopClass(battleSide, origin.Troop).DefaultClass();
			FormationClass result = formationClass;
			float num = float.MinValue;
			bestClassInnerClassType = SandboxBattleSpawnModel.OrderOfBattleInnerClassType.None;
			for (int i = 0; i < 8; i++)
			{
				CharacterObject characterObject;
				if (origin.Troop.IsHero && (characterObject = origin.Troop as CharacterObject) != null && characterObject.HeroObject == formationOrderOfBattleConfigurations[i].Captain)
				{
					result = (FormationClass)i;
					bestClassInnerClassType = SandboxBattleSpawnModel.OrderOfBattleInnerClassType.None;
					break;
				}
				if (formationClass == formationOrderOfBattleConfigurations[i].PrimaryFormationClass)
				{
					float num2 = (float)formationOrderOfBattleConfigurations[i].PrimaryClassDesiredTroopCount;
					float num3 = (float)formationOrderOfBattleConfigurations[i].PrimaryClassTroopCount;
					float num4 = 1f - num3 / (num2 + 1f);
					if (num4 > num)
					{
						result = (FormationClass)i;
						bestClassInnerClassType = SandboxBattleSpawnModel.OrderOfBattleInnerClassType.PrimaryClass;
						num = num4;
					}
				}
				else if (formationClass == formationOrderOfBattleConfigurations[i].SecondaryFormationClass)
				{
					float num5 = (float)formationOrderOfBattleConfigurations[i].SecondaryClassDesiredTroopCount;
					float num6 = (float)formationOrderOfBattleConfigurations[i].SecondaryClassTroopCount;
					float num7 = 1f - num6 / (num5 + 1f);
					if (num7 > num)
					{
						result = (FormationClass)i;
						bestClassInnerClassType = SandboxBattleSpawnModel.OrderOfBattleInnerClassType.SecondaryClass;
						num = num7;
					}
				}
			}
			return result;
		}

		// Token: 0x020001D7 RID: 471
		private enum OrderOfBattleInnerClassType
		{
			// Token: 0x040008C6 RID: 2246
			None,
			// Token: 0x040008C7 RID: 2247
			PrimaryClass,
			// Token: 0x040008C8 RID: 2248
			SecondaryClass
		}

		// Token: 0x020001D8 RID: 472
		private struct FormationOrderOfBattleConfiguration
		{
			// Token: 0x040008C9 RID: 2249
			public DeploymentFormationClass OOBFormationClass;

			// Token: 0x040008CA RID: 2250
			public FormationClass PrimaryFormationClass;

			// Token: 0x040008CB RID: 2251
			public int PrimaryClassTroopCount;

			// Token: 0x040008CC RID: 2252
			public int PrimaryClassDesiredTroopCount;

			// Token: 0x040008CD RID: 2253
			public FormationClass SecondaryFormationClass;

			// Token: 0x040008CE RID: 2254
			public int SecondaryClassTroopCount;

			// Token: 0x040008CF RID: 2255
			public int SecondaryClassDesiredTroopCount;

			// Token: 0x040008D0 RID: 2256
			public Hero Captain;
		}
	}
}
