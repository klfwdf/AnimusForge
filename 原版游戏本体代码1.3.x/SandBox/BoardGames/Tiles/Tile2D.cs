using System;
using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Tiles
{
	// Token: 0x020000EF RID: 239
	public class Tile2D : TileBase
	{
		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000C31 RID: 3121 RVA: 0x0005C3C4 File Offset: 0x0005A5C4
		public int X { get; }

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x06000C32 RID: 3122 RVA: 0x0005C3CC File Offset: 0x0005A5CC
		public int Y { get; }

		// Token: 0x06000C33 RID: 3123 RVA: 0x0005C3D4 File Offset: 0x0005A5D4
		public Tile2D(GameEntity entity, BoardGameDecal decal, int x, int y)
			: base(entity, decal)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
