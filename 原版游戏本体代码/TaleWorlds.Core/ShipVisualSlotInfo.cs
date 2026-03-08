using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000091 RID: 145
	public struct ShipVisualSlotInfo
	{
		// Token: 0x060008AD RID: 2221 RVA: 0x0001CEC5 File Offset: 0x0001B0C5
		public ShipVisualSlotInfo(string visualSlotId, string visualPieceId)
		{
			this.VisualSlotTag = visualSlotId;
			this.VisualPieceId = visualPieceId;
		}

		// Token: 0x0400045E RID: 1118
		public string VisualSlotTag;

		// Token: 0x0400045F RID: 1119
		public string VisualPieceId;
	}
}
