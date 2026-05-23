using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000418 RID: 1048
	public class MilitiasCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004287 RID: 17031 RVA: 0x001406A9 File Offset: 0x0013E8A9
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUp));
			CampaignEvents.AfterSiegeCompletedEvent.AddNonSerializedListener(this, new Action<Settlement, MobileParty, bool, MapEvent.BattleTypes>(this.OnAfterSiegeCompleted));
		}

		// Token: 0x06004288 RID: 17032 RVA: 0x001406DC File Offset: 0x0013E8DC
		private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
		{
			int count = Town.AllTowns.Count;
			int count2 = Town.AllCastles.Count;
			int count3 = Village.All.Count;
			int num = count / 100 + ((count % 100 > i) ? 1 : 0);
			int num2 = count2 / 100 + ((count2 % 100 > i) ? 1 : 0);
			int num3 = count3 / 100 + ((count3 % 100 > i) ? 1 : 0);
			int num4 = count / 100 * i;
			int num5 = count2 / 100 * i;
			int num6 = count3 / 100 * i;
			for (int j = 0; j < i; j++)
			{
				num4 += ((count % 100 > j) ? 1 : 0);
				num5 += ((count2 % 100 > j) ? 1 : 0);
				num6 += ((count3 % 100 > j) ? 1 : 0);
			}
			for (int k = 0; k < num; k++)
			{
				Town.AllTowns[num4 + k].Settlement.Militia = Town.AllTowns[num4 + k].Settlement.Town.MilitiaChange * 45f;
			}
			for (int l = 0; l < num2; l++)
			{
				Town.AllCastles[num5 + l].Settlement.Militia = Town.AllCastles[num5 + l].Settlement.Town.MilitiaChange * 45f;
			}
			for (int m = 0; m < num3; m++)
			{
				Village.All[num6 + m].Settlement.Militia = Village.All[num6 + m].Settlement.Village.MilitiaChange * 45f;
			}
		}

		// Token: 0x06004289 RID: 17033 RVA: 0x00140883 File Offset: 0x0013EA83
		private void OnAfterSiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
		{
			if ((battleType == MapEvent.BattleTypes.SallyOut || battleType == MapEvent.BattleTypes.Siege) && isWin)
			{
				siegeSettlement.Militia += (float)Campaign.Current.Models.SettlementMilitiaModel.MilitiaToSpawnAfterSiege(siegeSettlement.Town);
			}
		}

		// Token: 0x0600428A RID: 17034 RVA: 0x001408BE File Offset: 0x0013EABE
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
