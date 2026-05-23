using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000092 RID: 146
	public struct ShipSlotAndPieceName
	{
		// Token: 0x060008AE RID: 2222 RVA: 0x0001CED5 File Offset: 0x0001B0D5
		public ShipSlotAndPieceName(string slotName, string pieceName)
		{
			this.SlotName = slotName;
			this.PieceName = pieceName;
		}

		// Token: 0x04000460 RID: 1120
		public string SlotName;

		// Token: 0x04000461 RID: 1121
		public string PieceName;
	}
}
