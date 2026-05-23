using System;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F8 RID: 248
	public class PawnSeega : PawnBase
	{
		// Token: 0x17000101 RID: 257
		// (get) Token: 0x06000C8D RID: 3213 RVA: 0x0005D2EC File Offset: 0x0005B4EC
		public override bool IsPlaced
		{
			get
			{
				return this.X >= 0 && this.X < BoardGameSeega.BoardWidth && this.Y >= 0 && this.Y < BoardGameSeega.BoardHeight;
			}
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x06000C8E RID: 3214 RVA: 0x0005D31C File Offset: 0x0005B51C
		// (set) Token: 0x06000C8F RID: 3215 RVA: 0x0005D324 File Offset: 0x0005B524
		public bool MovedThisTurn { get; private set; }

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x06000C90 RID: 3216 RVA: 0x0005D32D File Offset: 0x0005B52D
		// (set) Token: 0x06000C91 RID: 3217 RVA: 0x0005D335 File Offset: 0x0005B535
		public int PrevX
		{
			get
			{
				return this._prevX;
			}
			set
			{
				this._prevX = value;
				if (value >= 0)
				{
					this.MovedThisTurn = true;
					return;
				}
				this.MovedThisTurn = false;
			}
		}

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x06000C92 RID: 3218 RVA: 0x0005D351 File Offset: 0x0005B551
		// (set) Token: 0x06000C93 RID: 3219 RVA: 0x0005D359 File Offset: 0x0005B559
		public int PrevY
		{
			get
			{
				return this._prevY;
			}
			set
			{
				this._prevY = value;
				if (value >= 0)
				{
					this.MovedThisTurn = true;
					return;
				}
				this.MovedThisTurn = false;
			}
		}

		// Token: 0x06000C94 RID: 3220 RVA: 0x0005D375 File Offset: 0x0005B575
		public PawnSeega(GameEntity entity, bool playerOne)
			: base(entity, playerOne)
		{
			this.X = -1;
			this.Y = -1;
			this.PrevX = -1;
			this.PrevY = -1;
			this.MovedThisTurn = false;
		}

		// Token: 0x06000C95 RID: 3221 RVA: 0x0005D3A2 File Offset: 0x0005B5A2
		public override void Reset()
		{
			base.Reset();
			this.X = -1;
			this.Y = -1;
			this.PrevX = -1;
			this.PrevY = -1;
			this.MovedThisTurn = false;
		}

		// Token: 0x06000C96 RID: 3222 RVA: 0x0005D3CD File Offset: 0x0005B5CD
		public void UpdateMoveBackAvailable()
		{
			if (this.MovedThisTurn)
			{
				this.MovedThisTurn = false;
				return;
			}
			this.PrevX = -1;
			this.PrevY = -1;
		}

		// Token: 0x06000C97 RID: 3223 RVA: 0x0005D3ED File Offset: 0x0005B5ED
		public void AISetMovedThisTurn(bool moved)
		{
			this.MovedThisTurn = moved;
		}

		// Token: 0x04000573 RID: 1395
		public int X;

		// Token: 0x04000574 RID: 1396
		public int Y;

		// Token: 0x04000575 RID: 1397
		private int _prevX;

		// Token: 0x04000576 RID: 1398
		private int _prevY;
	}
}
