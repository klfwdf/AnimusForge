using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E5 RID: 485
	public abstract class SiegeLordsHallFightModel : MBGameModel<SiegeLordsHallFightModel>
	{
		// Token: 0x170007A0 RID: 1952
		// (get) Token: 0x06001EA8 RID: 7848
		public abstract float AreaLostRatio { get; }

		// Token: 0x170007A1 RID: 1953
		// (get) Token: 0x06001EA9 RID: 7849
		public abstract float AttackerDefenderTroopCountRatio { get; }

		// Token: 0x170007A2 RID: 1954
		// (get) Token: 0x06001EAA RID: 7850
		public abstract int DefenderTroopNumberForSuccessfulPullBack { get; }

		// Token: 0x170007A3 RID: 1955
		// (get) Token: 0x06001EAB RID: 7851
		public abstract float DefenderMaxArcherRatio { get; }

		// Token: 0x170007A4 RID: 1956
		// (get) Token: 0x06001EAC RID: 7852
		public abstract int MaxDefenderSideTroopCount { get; }

		// Token: 0x170007A5 RID: 1957
		// (get) Token: 0x06001EAD RID: 7853
		public abstract int MaxDefenderArcherCount { get; }

		// Token: 0x170007A6 RID: 1958
		// (get) Token: 0x06001EAE RID: 7854
		public abstract int MaxAttackerSideTroopCount { get; }

		// Token: 0x06001EAF RID: 7855
		public abstract FlattenedTroopRoster GetPriorityListForLordsHallFightMission(MapEvent playerMapEvent, BattleSideEnum side, int troopCount);
	}
}
