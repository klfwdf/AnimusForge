using System;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F5 RID: 245
	public class PawnKonane : PawnBase
	{
		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000C75 RID: 3189 RVA: 0x0005CE4F File Offset: 0x0005B04F
		public override bool IsPlaced
		{
			get
			{
				return this.X >= 0 && this.X < BoardGameKonane.BoardWidth && this.Y >= 0 && this.Y < BoardGameKonane.BoardHeight;
			}
		}

		// Token: 0x06000C76 RID: 3190 RVA: 0x0005CE7F File Offset: 0x0005B07F
		public PawnKonane(GameEntity entity, bool playerOne)
			: base(entity, playerOne)
		{
			this.X = -1;
			this.Y = -1;
			this.PrevX = -1;
			this.PrevY = -1;
		}

		// Token: 0x06000C77 RID: 3191 RVA: 0x0005CEA5 File Offset: 0x0005B0A5
		public override void Reset()
		{
			base.Reset();
			this.X = -1;
			this.Y = -1;
			this.PrevX = -1;
			this.PrevY = -1;
		}

		// Token: 0x04000566 RID: 1382
		public int X;

		// Token: 0x04000567 RID: 1383
		public int Y;

		// Token: 0x04000568 RID: 1384
		public int PrevX;

		// Token: 0x04000569 RID: 1385
		public int PrevY;
	}
}
