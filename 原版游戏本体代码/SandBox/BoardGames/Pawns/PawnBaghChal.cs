using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F3 RID: 243
	public class PawnBaghChal : PawnBase
	{
		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x06000C47 RID: 3143 RVA: 0x0005C609 File Offset: 0x0005A809
		public override bool IsPlaced
		{
			get
			{
				return this.X >= 0 && this.X < BoardGameBaghChal.BoardWidth && this.Y >= 0 && this.Y < BoardGameBaghChal.BoardHeight;
			}
		}

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x06000C48 RID: 3144 RVA: 0x0005C639 File Offset: 0x0005A839
		public MatrixFrame InitialFrame { get; }

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x06000C49 RID: 3145 RVA: 0x0005C641 File Offset: 0x0005A841
		public bool IsTiger { get; }

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x06000C4A RID: 3146 RVA: 0x0005C649 File Offset: 0x0005A849
		public bool IsGoat
		{
			get
			{
				return !this.IsTiger;
			}
		}

		// Token: 0x06000C4B RID: 3147 RVA: 0x0005C654 File Offset: 0x0005A854
		public PawnBaghChal(GameEntity entity, bool playerOne, bool isTiger)
			: base(entity, playerOne)
		{
			this.X = -1;
			this.Y = -1;
			this.PrevX = -1;
			this.PrevY = -1;
			this.IsTiger = isTiger;
			this.InitialFrame = base.Entity.GetFrame();
		}

		// Token: 0x06000C4C RID: 3148 RVA: 0x0005C692 File Offset: 0x0005A892
		public override void Reset()
		{
			base.Reset();
			this.X = -1;
			this.Y = -1;
			this.PrevX = -1;
			this.PrevY = -1;
		}

		// Token: 0x0400054A RID: 1354
		public int X;

		// Token: 0x0400054B RID: 1355
		public int Y;

		// Token: 0x0400054C RID: 1356
		public int PrevX;

		// Token: 0x0400054D RID: 1357
		public int PrevY;
	}
}
