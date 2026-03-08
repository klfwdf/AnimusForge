using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000049 RID: 73
	public struct PieceData
	{
		// Token: 0x17000210 RID: 528
		// (get) Token: 0x06000631 RID: 1585 RVA: 0x00015894 File Offset: 0x00013A94
		// (set) Token: 0x06000632 RID: 1586 RVA: 0x0001589C File Offset: 0x00013A9C
		public CraftingPiece.PieceTypes PieceType { get; private set; }

		// Token: 0x17000211 RID: 529
		// (get) Token: 0x06000633 RID: 1587 RVA: 0x000158A5 File Offset: 0x00013AA5
		// (set) Token: 0x06000634 RID: 1588 RVA: 0x000158AD File Offset: 0x00013AAD
		public int Order { get; private set; }

		// Token: 0x06000635 RID: 1589 RVA: 0x000158B6 File Offset: 0x00013AB6
		public PieceData(CraftingPiece.PieceTypes pieceType, int order)
		{
			this.PieceType = pieceType;
			this.Order = order;
		}
	}
}
