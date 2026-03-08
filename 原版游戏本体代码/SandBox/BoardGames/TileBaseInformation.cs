using System;
using SandBox.BoardGames.Pawns;

namespace SandBox.BoardGames
{
	// Token: 0x020000E5 RID: 229
	public struct TileBaseInformation
	{
		// Token: 0x06000B33 RID: 2867 RVA: 0x00053FA3 File Offset: 0x000521A3
		public TileBaseInformation(ref PawnBase pawnOnTile)
		{
			this.PawnOnTile = pawnOnTile;
		}

		// Token: 0x040004DC RID: 1244
		public PawnBase PawnOnTile;
	}
}
