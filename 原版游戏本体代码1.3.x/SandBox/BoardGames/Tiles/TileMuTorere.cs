using System;
using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Tiles
{
	// Token: 0x020000F1 RID: 241
	public class TileMuTorere : Tile1D
	{
		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x06000C3A RID: 3130 RVA: 0x0005C482 File Offset: 0x0005A682
		public int XLeftTile { get; }

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x06000C3B RID: 3131 RVA: 0x0005C48A File Offset: 0x0005A68A
		public int XRightTile { get; }

		// Token: 0x06000C3C RID: 3132 RVA: 0x0005C492 File Offset: 0x0005A692
		public TileMuTorere(GameEntity entity, BoardGameDecal decal, int x, int xLeft, int xRight)
			: base(entity, decal, x)
		{
			this.XLeftTile = xLeft;
			this.XRightTile = xRight;
		}
	}
}
