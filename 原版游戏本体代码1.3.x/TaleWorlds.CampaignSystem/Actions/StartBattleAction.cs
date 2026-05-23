using System;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C7 RID: 1223
	public static class StartBattleAction
	{
		// Token: 0x06004A26 RID: 18982 RVA: 0x00175920 File Offset: 0x00173B20
		private static void ApplyInternal(PartyBase attackerParty, PartyBase defenderParty, object subject, MapEvent.BattleTypes battleType)
		{
			if (defenderParty.MapEvent == null)
			{
				Campaign.Current.Models.EncounterModel.CreateMapEventComponentForEncounter(attackerParty, defenderParty, battleType);
				if (defenderParty.MapEvent == null)
				{
					return;
				}
			}
			else
			{
				BattleSideEnum side = BattleSideEnum.Attacker;
				if (defenderParty.Side == BattleSideEnum.Attacker)
				{
					side = BattleSideEnum.Defender;
				}
				attackerParty.MapEventSide = defenderParty.MapEvent.GetMapEventSide(side);
			}
			if (defenderParty.MapEvent.IsPlayerMapEvent && !defenderParty.MapEvent.IsSallyOut && PlayerEncounter.Current != null && MobileParty.MainParty.CurrentSettlement != null)
			{
				PlayerEncounter.Current.InterruptEncounter("encounter_interrupted");
			}
			MobileParty mobileParty = attackerParty.MobileParty;
			bool flag;
			if (((mobileParty != null) ? mobileParty.Army : null) != null)
			{
				MobileParty mobileParty2 = attackerParty.MobileParty;
				if (((mobileParty2 != null) ? mobileParty2.Army.LeaderParty : null) != attackerParty.MobileParty)
				{
					flag = false;
					goto IL_F0;
				}
			}
			MobileParty mobileParty3 = defenderParty.MobileParty;
			if (((mobileParty3 != null) ? mobileParty3.Army : null) != null)
			{
				MobileParty mobileParty4 = defenderParty.MobileParty;
				flag = ((mobileParty4 != null) ? mobileParty4.Army.LeaderParty : null) == defenderParty.MobileParty;
			}
			else
			{
				flag = true;
			}
			IL_F0:
			bool flag2 = flag;
			if (flag2 && defenderParty.IsSettlement && defenderParty.MapEvent != null && defenderParty.MapEvent.DefenderSide.Parties.Count > 1)
			{
				flag2 = false;
			}
			CampaignEventDispatcher.Instance.OnStartBattle(attackerParty, defenderParty, subject, flag2);
		}

		// Token: 0x06004A27 RID: 18983 RVA: 0x00175A5C File Offset: 0x00173C5C
		public static void Apply(PartyBase attackerParty, PartyBase defenderParty)
		{
			MapEvent.BattleTypes battleTypes = MapEvent.BattleTypes.None;
			object obj = null;
			Settlement settlement;
			if (defenderParty.MapEvent == null)
			{
				if (attackerParty.MobileParty != null && attackerParty.MobileParty.IsGarrison)
				{
					settlement = attackerParty.MobileParty.CurrentSettlement;
					battleTypes = (attackerParty.MobileParty.IsTargetingPort ? MapEvent.BattleTypes.BlockadeSallyOutBattle : MapEvent.BattleTypes.SallyOut);
				}
				else if (attackerParty.MobileParty.CurrentSettlement != null)
				{
					settlement = attackerParty.MobileParty.CurrentSettlement;
				}
				else if (defenderParty.MobileParty.CurrentSettlement != null)
				{
					settlement = defenderParty.MobileParty.CurrentSettlement;
				}
				else if (attackerParty.MobileParty.BesiegedSettlement != null)
				{
					settlement = attackerParty.MobileParty.BesiegedSettlement;
					if (!defenderParty.IsSettlement)
					{
						battleTypes = MapEvent.BattleTypes.SiegeOutside;
					}
				}
				else if (defenderParty.MobileParty.BesiegedSettlement != null)
				{
					settlement = defenderParty.MobileParty.BesiegedSettlement;
					battleTypes = MapEvent.BattleTypes.SiegeOutside;
				}
				else
				{
					battleTypes = MapEvent.BattleTypes.FieldBattle;
					settlement = null;
				}
				if (settlement != null && battleTypes == MapEvent.BattleTypes.None)
				{
					if (settlement.IsTown)
					{
						battleTypes = MapEvent.BattleTypes.Siege;
						if (attackerParty.IsMobile && defenderParty.SiegeEvent != null && attackerParty.SiegeEvent != null && attackerParty.MobileParty.IsCurrentlyAtSea && attackerParty.MobileParty.IsTargetingPort)
						{
							battleTypes = MapEvent.BattleTypes.BlockadeBattle;
						}
					}
					else if (settlement.IsHideout)
					{
						battleTypes = MapEvent.BattleTypes.Hideout;
					}
					else if (settlement.IsVillage)
					{
						battleTypes = MapEvent.BattleTypes.FieldBattle;
					}
					else
					{
						Debug.FailedAssert("Missing settlement type in StartBattleAction.GetGameAction", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\StartBattleAction.cs", "Apply", 134);
					}
				}
			}
			else
			{
				if (defenderParty.MapEvent.IsFieldBattle)
				{
					battleTypes = MapEvent.BattleTypes.FieldBattle;
				}
				else if (defenderParty.MapEvent.IsRaid)
				{
					battleTypes = MapEvent.BattleTypes.Raid;
				}
				else if (defenderParty.MapEvent.IsSiegeAssault)
				{
					battleTypes = MapEvent.BattleTypes.Siege;
				}
				else if (defenderParty.MapEvent.IsSallyOut)
				{
					battleTypes = MapEvent.BattleTypes.SallyOut;
				}
				else if (defenderParty.MapEvent.IsSiegeOutside)
				{
					battleTypes = MapEvent.BattleTypes.SiegeOutside;
				}
				else if (defenderParty.MapEvent.IsBlockade)
				{
					battleTypes = MapEvent.BattleTypes.BlockadeBattle;
				}
				else if (defenderParty.MapEvent.IsBlockadeSallyOut)
				{
					battleTypes = MapEvent.BattleTypes.BlockadeSallyOutBattle;
				}
				else
				{
					Debug.FailedAssert("Missing mapEventType?", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\StartBattleAction.cs", "Apply", 170);
				}
				settlement = defenderParty.MapEvent.MapEventSettlement;
			}
			obj = obj ?? settlement;
			StartBattleAction.ApplyInternal(attackerParty, defenderParty, obj, battleTypes);
		}

		// Token: 0x06004A28 RID: 18984 RVA: 0x00175C76 File Offset: 0x00173E76
		public static void ApplyStartBattle(MobileParty attackerParty, MobileParty defenderParty)
		{
			StartBattleAction.ApplyInternal(attackerParty.Party, defenderParty.Party, null, MapEvent.BattleTypes.FieldBattle);
		}

		// Token: 0x06004A29 RID: 18985 RVA: 0x00175C8B File Offset: 0x00173E8B
		public static void ApplyStartRaid(MobileParty attackerParty, Settlement settlement)
		{
			StartBattleAction.ApplyInternal(attackerParty.Party, settlement.Party, settlement, MapEvent.BattleTypes.Raid);
		}

		// Token: 0x06004A2A RID: 18986 RVA: 0x00175CA0 File Offset: 0x00173EA0
		public static void ApplyStartSallyOut(Settlement settlement, MobileParty defenderParty)
		{
			StartBattleAction.ApplyInternal(settlement.Town.GarrisonParty.Party, defenderParty.Party, settlement, MapEvent.BattleTypes.SallyOut);
		}

		// Token: 0x06004A2B RID: 18987 RVA: 0x00175CBF File Offset: 0x00173EBF
		public static void ApplyStartAssaultAgainstWalls(MobileParty attackerParty, Settlement settlement)
		{
			StartBattleAction.ApplyInternal(attackerParty.Party, settlement.Party, settlement, MapEvent.BattleTypes.Siege);
		}
	}
}
