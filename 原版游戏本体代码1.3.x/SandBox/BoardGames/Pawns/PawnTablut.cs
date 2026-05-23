using System;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F9 RID: 249
	public class PawnTablut : PawnBase
	{
		// Token: 0x17000105 RID: 261
		// (get) Token: 0x06000C98 RID: 3224 RVA: 0x0005D3F6 File Offset: 0x0005B5F6
		public override bool IsPlaced
		{
			get
			{
				return this.X >= 0 && this.X < 9 && this.Y >= 0 && this.Y < 9;
			}
		}

		// Token: 0x06000C99 RID: 3225 RVA: 0x0005D420 File Offset: 0x0005B620
		public PawnTablut(GameEntity entity, bool playerOne)
			: base(entity, playerOne)
		{
			this.X = -1;
			this.Y = -1;
		}

		// Token: 0x06000C9A RID: 3226 RVA: 0x0005D438 File Offset: 0x0005B638
		public override void Reset()
		{
			base.Reset();
			this.X = -1;
			this.Y = -1;
		}

		// Token: 0x04000578 RID: 1400
		public int X;

		// Token: 0x04000579 RID: 1401
		public int Y;
	}
}
