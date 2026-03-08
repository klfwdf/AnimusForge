using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D7 RID: 983
	public class CampaignWarManagerBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003A79 RID: 14969 RVA: 0x000F1CB2 File Offset: 0x000EFEB2
		public override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.MapEventEnded));
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
		}

		// Token: 0x06003A7A RID: 14970 RVA: 0x000F1CE4 File Offset: 0x000EFEE4
		private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
		{
			if (raidEvent.AttackerSide.LeaderParty.MapFaction != null && !raidEvent.AttackerSide.LeaderParty.MapFaction.IsBanditFaction && raidEvent.DefenderSide.LeaderParty.MapFaction != null && !raidEvent.DefenderSide.LeaderParty.MapFaction.IsBanditFaction)
			{
				IFaction mapFaction = raidEvent.AttackerSide.MapFaction;
				IFaction mapFaction2 = raidEvent.DefenderSide.MapFaction;
				if (mapFaction.MapFaction != mapFaction2.MapFaction)
				{
					StanceLink stanceWith = mapFaction.GetStanceWith(mapFaction2);
					if (raidEvent.MapEventSettlement != null && raidEvent.BattleState == BattleState.AttackerVictory && raidEvent.MapEventSettlement.IsVillage && raidEvent.MapEventSettlement.Village.VillageState == Village.VillageStates.Looted)
					{
						int num;
						if (mapFaction == stanceWith.Faction1)
						{
							StanceLink stanceLink = stanceWith;
							num = stanceLink.SuccessfulRaids1;
							stanceLink.SuccessfulRaids1 = num + 1;
							return;
						}
						StanceLink stanceLink2 = stanceWith;
						num = stanceLink2.SuccessfulRaids2;
						stanceLink2.SuccessfulRaids2 = num + 1;
					}
				}
			}
		}

		// Token: 0x06003A7B RID: 14971 RVA: 0x000F1DD8 File Offset: 0x000EFFD8
		private void MapEventEnded(MapEvent mapEvent)
		{
			if (mapEvent.AttackerSide.LeaderParty.MapFaction != null && !mapEvent.AttackerSide.LeaderParty.MapFaction.IsBanditFaction && mapEvent.DefenderSide.LeaderParty.MapFaction != null && !mapEvent.DefenderSide.LeaderParty.MapFaction.IsBanditFaction)
			{
				IFaction mapFaction = mapEvent.AttackerSide.MapFaction;
				IFaction mapFaction2 = mapEvent.DefenderSide.MapFaction;
				if (mapFaction.MapFaction != mapFaction2.MapFaction)
				{
					StanceLink stanceWith = mapFaction.GetStanceWith(mapFaction2);
					stanceWith.TroopCasualties1 += ((stanceWith.Faction1 == mapFaction) ? mapEvent.AttackerSide.TroopCasualties : mapEvent.DefenderSide.TroopCasualties);
					stanceWith.TroopCasualties2 += ((stanceWith.Faction2 == mapFaction) ? mapEvent.AttackerSide.TroopCasualties : mapEvent.DefenderSide.TroopCasualties);
					stanceWith.ShipCasualties1 += ((stanceWith.Faction1 == mapFaction) ? mapEvent.AttackerSide.ShipCasualties : mapEvent.DefenderSide.ShipCasualties);
					stanceWith.ShipCasualties2 += ((stanceWith.Faction2 == mapFaction) ? mapEvent.AttackerSide.ShipCasualties : mapEvent.DefenderSide.ShipCasualties);
					if (mapEvent.MapEventSettlement != null && mapEvent.BattleState == BattleState.AttackerVictory && mapEvent.MapEventSettlement.IsFortification && mapEvent.EventType == MapEvent.BattleTypes.Siege)
					{
						if (mapFaction == stanceWith.Faction1)
						{
							StanceLink stanceLink = stanceWith;
							int num = stanceLink.SuccessfulSieges1;
							stanceLink.SuccessfulSieges1 = num + 1;
							if (mapEvent.MapEventSettlement.IsTown)
							{
								StanceLink stanceLink2 = stanceWith;
								num = stanceLink2.SuccessfulTownSieges1;
								stanceLink2.SuccessfulTownSieges1 = num + 1;
								return;
							}
						}
						else
						{
							StanceLink stanceLink3 = stanceWith;
							int num = stanceLink3.SuccessfulSieges2;
							stanceLink3.SuccessfulSieges2 = num + 1;
							if (mapEvent.MapEventSettlement.IsTown)
							{
								StanceLink stanceLink4 = stanceWith;
								num = stanceLink4.SuccessfulTownSieges2;
								stanceLink4.SuccessfulTownSieges2 = num + 1;
							}
						}
					}
				}
			}
		}

		// Token: 0x06003A7C RID: 14972 RVA: 0x000F1FBA File Offset: 0x000F01BA
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
