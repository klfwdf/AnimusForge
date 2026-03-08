using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200019B RID: 411
	public abstract class FleetManagementModel : MBGameModel<FleetManagementModel>
	{
		// Token: 0x06001C41 RID: 7233
		public abstract bool CanTroopsReturn();

		// Token: 0x06001C42 RID: 7234
		public abstract CampaignTime GetReturnTimeForTroops(Ship ship);

		// Token: 0x06001C43 RID: 7235
		public abstract bool CanSendShipToPlayerClan(Ship ship, int playerShipsCount, int troopsCountToSend, out TextObject hint);

		// Token: 0x1700070B RID: 1803
		// (get) Token: 0x06001C44 RID: 7236
		public abstract int MinimumTroopCountRequiredToSendShips { get; }
	}
}
