using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F7 RID: 247
	public class PawnPuluc : PawnBase
	{
		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000C7D RID: 3197 RVA: 0x0005CEFD File Offset: 0x0005B0FD
		public float Height
		{
			get
			{
				if (PawnPuluc._height == 0f)
				{
					PawnPuluc._height = (base.Entity.GetBoundingBoxMax() - base.Entity.GetBoundingBoxMin()).z;
				}
				return PawnPuluc._height;
			}
		}

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x06000C7E RID: 3198 RVA: 0x0005CF35 File Offset: 0x0005B135
		public override Vec3 PosBeforeMoving
		{
			get
			{
				return this.PosBeforeMovingBase - new Vec3(0f, 0f, this.Height * (float)this.PawnsBelow.Count, -1f);
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x06000C7F RID: 3199 RVA: 0x0005CF69 File Offset: 0x0005B169
		public override bool IsPlaced
		{
			get
			{
				return (this.InPlay || this.IsInSpawn) && this.IsTopPawn;
			}
		}

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x06000C80 RID: 3200 RVA: 0x0005CF83 File Offset: 0x0005B183
		// (set) Token: 0x06000C81 RID: 3201 RVA: 0x0005CF8B File Offset: 0x0005B18B
		public int X
		{
			get
			{
				return this._x;
			}
			set
			{
				this._x = value;
				if (value >= 0 && value < 11)
				{
					this.IsInSpawn = false;
					return;
				}
				this.IsInSpawn = true;
			}
		}

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x06000C82 RID: 3202 RVA: 0x0005CFAC File Offset: 0x0005B1AC
		public List<PawnPuluc> PawnsBelow { get; }

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x06000C83 RID: 3203 RVA: 0x0005CFB4 File Offset: 0x0005B1B4
		public bool InPlay
		{
			get
			{
				return this.X >= 0 && this.X < 11;
			}
		}

		// Token: 0x06000C84 RID: 3204 RVA: 0x0005CFCB File Offset: 0x0005B1CB
		public PawnPuluc(GameEntity entity, bool playerOne)
			: base(entity, playerOne)
		{
			this.PawnsBelow = new List<PawnPuluc>();
			this.SpawnPos = base.CurrentPos;
			this.X = -1;
		}

		// Token: 0x06000C85 RID: 3205 RVA: 0x0005D001 File Offset: 0x0005B201
		public override void Reset()
		{
			base.Reset();
			this.X = -1;
			this.State = PawnPuluc.MovementState.MovingForward;
			this.IsTopPawn = true;
			this.IsInSpawn = true;
			this.CapturedBy = null;
			this.PawnsBelow.Clear();
		}

		// Token: 0x06000C86 RID: 3206 RVA: 0x0005D038 File Offset: 0x0005B238
		public override void AddGoalPosition(Vec3 goal)
		{
			if (this.IsTopPawn)
			{
				goal.z += this.Height * (float)this.PawnsBelow.Count;
				int count = this.PawnsBelow.Count;
				for (int i = 0; i < count; i++)
				{
					this.PawnsBelow[i].AddGoalPosition(goal - new Vec3(0f, 0f, (float)(i + 1) * this.Height, -1f));
				}
			}
			base.GoalPositions.Add(goal);
		}

		// Token: 0x06000C87 RID: 3207 RVA: 0x0005D0C8 File Offset: 0x0005B2C8
		public override void MovePawnToGoalPositions(bool instantMove, float speed, bool dragged = false)
		{
			if (base.GoalPositions.Count == 0)
			{
				return;
			}
			base.MovePawnToGoalPositions(instantMove, speed, dragged);
			if (this.IsTopPawn)
			{
				foreach (PawnPuluc pawnPuluc in this.PawnsBelow)
				{
					pawnPuluc.MovePawnToGoalPositions(instantMove, speed, dragged);
				}
			}
		}

		// Token: 0x06000C88 RID: 3208 RVA: 0x0005D13C File Offset: 0x0005B33C
		public override void SetPawnAtPosition(Vec3 position)
		{
			base.SetPawnAtPosition(position);
			if (this.IsTopPawn)
			{
				int num = 1;
				foreach (PawnPuluc pawnPuluc in this.PawnsBelow)
				{
					pawnPuluc.SetPawnAtPosition(new Vec3(position.x, position.y, position.z - this.Height * (float)num, -1f));
					num++;
				}
			}
		}

		// Token: 0x06000C89 RID: 3209 RVA: 0x0005D1C8 File Offset: 0x0005B3C8
		public override void EnableCollisionBody()
		{
			base.EnableCollisionBody();
			foreach (PawnPuluc pawnPuluc in this.PawnsBelow)
			{
				pawnPuluc.Entity.BodyFlag &= ~BodyFlags.Disabled;
			}
		}

		// Token: 0x06000C8A RID: 3210 RVA: 0x0005D22C File Offset: 0x0005B42C
		public override void DisableCollisionBody()
		{
			base.DisableCollisionBody();
			foreach (PawnPuluc pawnPuluc in this.PawnsBelow)
			{
				pawnPuluc.Entity.BodyFlag |= BodyFlags.Disabled;
			}
		}

		// Token: 0x06000C8B RID: 3211 RVA: 0x0005D290 File Offset: 0x0005B490
		public void MovePawnBackToSpawn(bool instantMove, float speed, bool fake = false)
		{
			this.X = -1;
			this.State = PawnPuluc.MovementState.MovingForward;
			this.IsTopPawn = true;
			this.IsInSpawn = true;
			base.Captured = false;
			this.CapturedBy = null;
			this.PawnsBelow.Clear();
			if (!fake)
			{
				this.AddGoalPosition(this.SpawnPos);
				this.MovePawnToGoalPositions(instantMove, speed, false);
			}
		}

		// Token: 0x0400056B RID: 1387
		public PawnPuluc.MovementState State;

		// Token: 0x0400056C RID: 1388
		public PawnPuluc CapturedBy;

		// Token: 0x0400056D RID: 1389
		public Vec3 SpawnPos;

		// Token: 0x0400056E RID: 1390
		public bool IsInSpawn = true;

		// Token: 0x0400056F RID: 1391
		public bool IsTopPawn = true;

		// Token: 0x04000570 RID: 1392
		private static float _height;

		// Token: 0x04000571 RID: 1393
		private int _x;

		// Token: 0x02000223 RID: 547
		public enum MovementState
		{
			// Token: 0x040009A9 RID: 2473
			MovingForward,
			// Token: 0x040009AA RID: 2474
			MovingBackward,
			// Token: 0x040009AB RID: 2475
			ChangingDirection
		}
	}
}
