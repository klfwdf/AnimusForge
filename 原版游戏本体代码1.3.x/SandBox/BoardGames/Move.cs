using System;
using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;

namespace SandBox.BoardGames
{
	// Token: 0x020000E6 RID: 230
	public struct Move
	{
		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x06000B34 RID: 2868 RVA: 0x00053FAD File Offset: 0x000521AD
		public bool IsValid
		{
			get
			{
				return this.Unit != null && this.GoalTile != null;
			}
		}

		// Token: 0x06000B35 RID: 2869 RVA: 0x00053FC2 File Offset: 0x000521C2
		public Move(PawnBase unit, TileBase goalTile)
		{
			this.Unit = unit;
			this.GoalTile = goalTile;
		}

		// Token: 0x040004DD RID: 1245
		public static readonly Move Invalid = new Move
		{
			Unit = null,
			GoalTile = null
		};

		// Token: 0x040004DE RID: 1246
		public PawnBase Unit;

		// Token: 0x040004DF RID: 1247
		public TileBase GoalTile;
	}
}
