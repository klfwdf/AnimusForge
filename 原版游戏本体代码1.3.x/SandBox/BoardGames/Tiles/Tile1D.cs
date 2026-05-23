using System;
using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Tiles
{
	// Token: 0x020000EE RID: 238
	public class Tile1D : TileBase
	{
		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000C2F RID: 3119 RVA: 0x0005C3AB File Offset: 0x0005A5AB
		public int X { get; }

		// Token: 0x06000C30 RID: 3120 RVA: 0x0005C3B3 File Offset: 0x0005A5B3
		public Tile1D(GameEntity entity, BoardGameDecal decal, int x)
			: base(entity, decal)
		{
			this.X = x;
		}
	}
}
