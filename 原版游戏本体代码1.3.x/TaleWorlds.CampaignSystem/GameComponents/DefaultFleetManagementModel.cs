using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000115 RID: 277
	public class DefaultFleetManagementModel : FleetManagementModel
	{
		// Token: 0x17000662 RID: 1634
		// (get) Token: 0x060017D6 RID: 6102 RVA: 0x000717FB File Offset: 0x0006F9FB
		public override int MinimumTroopCountRequiredToSendShips
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x060017D7 RID: 6103 RVA: 0x000717FE File Offset: 0x0006F9FE
		public override bool CanSendShipToPlayerClan(Ship ship, int playerShipsCount, int troopsCountToSend, out TextObject hint)
		{
			hint = TextObject.GetEmpty();
			return false;
		}

		// Token: 0x060017D8 RID: 6104 RVA: 0x00071809 File Offset: 0x0006FA09
		public override bool CanTroopsReturn()
		{
			return false;
		}

		// Token: 0x060017D9 RID: 6105 RVA: 0x0007180C File Offset: 0x0006FA0C
		public override CampaignTime GetReturnTimeForTroops(Ship ship)
		{
			return CampaignTime.Never;
		}
	}
}
