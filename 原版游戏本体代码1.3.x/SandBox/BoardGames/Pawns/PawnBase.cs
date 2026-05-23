using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.BoardGames.Pawns
{
	// Token: 0x020000F4 RID: 244
	public abstract class PawnBase
	{
		// Token: 0x170000EA RID: 234
		// (get) Token: 0x06000C4D RID: 3149 RVA: 0x0005C6B6 File Offset: 0x0005A8B6
		// (set) Token: 0x06000C4E RID: 3150 RVA: 0x0005C6BD File Offset: 0x0005A8BD
		public static int PawnMoveSoundCodeID { get; set; }

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x06000C4F RID: 3151 RVA: 0x0005C6C5 File Offset: 0x0005A8C5
		// (set) Token: 0x06000C50 RID: 3152 RVA: 0x0005C6CC File Offset: 0x0005A8CC
		public static int PawnSelectSoundCodeID { get; set; }

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000C51 RID: 3153 RVA: 0x0005C6D4 File Offset: 0x0005A8D4
		// (set) Token: 0x06000C52 RID: 3154 RVA: 0x0005C6DB File Offset: 0x0005A8DB
		public static int PawnTapSoundCodeID { get; set; }

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x06000C53 RID: 3155 RVA: 0x0005C6E3 File Offset: 0x0005A8E3
		// (set) Token: 0x06000C54 RID: 3156 RVA: 0x0005C6EA File Offset: 0x0005A8EA
		public static int PawnRemoveSoundCodeID { get; set; }

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x06000C55 RID: 3157
		public abstract bool IsPlaced { get; }

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x06000C56 RID: 3158 RVA: 0x0005C6F2 File Offset: 0x0005A8F2
		// (set) Token: 0x06000C57 RID: 3159 RVA: 0x0005C6FA File Offset: 0x0005A8FA
		public virtual Vec3 PosBeforeMoving
		{
			get
			{
				return this.PosBeforeMovingBase;
			}
			protected set
			{
				this.PosBeforeMovingBase = value;
			}
		}

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x06000C58 RID: 3160 RVA: 0x0005C703 File Offset: 0x0005A903
		public GameEntity Entity { get; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x06000C59 RID: 3161 RVA: 0x0005C70B File Offset: 0x0005A90B
		protected List<Vec3> GoalPositions { get; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x06000C5A RID: 3162 RVA: 0x0005C713 File Offset: 0x0005A913
		// (set) Token: 0x06000C5B RID: 3163 RVA: 0x0005C71B File Offset: 0x0005A91B
		private protected Vec3 CurrentPos { protected get; private set; }

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000C5C RID: 3164 RVA: 0x0005C724 File Offset: 0x0005A924
		// (set) Token: 0x06000C5D RID: 3165 RVA: 0x0005C72C File Offset: 0x0005A92C
		public bool Captured { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000C5E RID: 3166 RVA: 0x0005C735 File Offset: 0x0005A935
		// (set) Token: 0x06000C5F RID: 3167 RVA: 0x0005C73D File Offset: 0x0005A93D
		public bool MovingToDifferentTile { get; set; }

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x06000C60 RID: 3168 RVA: 0x0005C746 File Offset: 0x0005A946
		// (set) Token: 0x06000C61 RID: 3169 RVA: 0x0005C74E File Offset: 0x0005A94E
		public bool Moving { get; private set; }

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x06000C62 RID: 3170 RVA: 0x0005C757 File Offset: 0x0005A957
		// (set) Token: 0x06000C63 RID: 3171 RVA: 0x0005C75F File Offset: 0x0005A95F
		public bool PlayerOne { get; private set; }

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000C64 RID: 3172 RVA: 0x0005C768 File Offset: 0x0005A968
		public bool HasAnyGoalPosition
		{
			get
			{
				bool result = false;
				if (this.GoalPositions != null)
				{
					result = !this.GoalPositions.IsEmpty<Vec3>();
				}
				return result;
			}
		}

		// Token: 0x06000C65 RID: 3173 RVA: 0x0005C790 File Offset: 0x0005A990
		protected PawnBase(GameEntity entity, bool playerOne)
		{
			this.Entity = entity;
			this.PlayerOne = playerOne;
			this.CurrentPos = this.Entity.GetGlobalFrame().origin;
			this.PosBeforeMoving = this.CurrentPos;
			this.Moving = false;
			this._dragged = false;
			this.Captured = false;
			this._movePauseDuration = 0.3f;
			entity.CreateVariableRatePhysics(true);
			this.GoalPositions = new List<Vec3>();
		}

		// Token: 0x06000C66 RID: 3174 RVA: 0x0005C808 File Offset: 0x0005AA08
		public virtual void Reset()
		{
			this.ClearGoalPositions();
			this.Moving = false;
			this.MovingToDifferentTile = false;
			this._movePauseDuration = 0.3f;
			this._movePauseTimer = 0f;
			this._moveTiming = false;
			this._dragged = false;
			this.Captured = false;
		}

		// Token: 0x06000C67 RID: 3175 RVA: 0x0005C854 File Offset: 0x0005AA54
		public virtual void AddGoalPosition(Vec3 goal)
		{
			this.GoalPositions.Add(goal);
		}

		// Token: 0x06000C68 RID: 3176 RVA: 0x0005C864 File Offset: 0x0005AA64
		public virtual void SetPawnAtPosition(Vec3 position)
		{
			MatrixFrame globalFrame = this.Entity.GetGlobalFrame();
			globalFrame.origin = position;
			this.Entity.SetGlobalFrame(globalFrame, true);
		}

		// Token: 0x06000C69 RID: 3177 RVA: 0x0005C894 File Offset: 0x0005AA94
		public virtual void MovePawnToGoalPositions(bool instantMove, float speed, bool dragged = false)
		{
			this.PosBeforeMoving = this.Entity.GlobalPosition;
			this._moveSpeed = speed;
			this._currentGoalPos = 0;
			this._movePauseTimer = 0f;
			this._dtCounter = 0f;
			this._moveTiming = false;
			this._dragged = dragged;
			if (this.GoalPositions.Count == 1 && this.PosBeforeMoving.Equals(this.GoalPositions[0]))
			{
				instantMove = true;
			}
			if (instantMove)
			{
				MatrixFrame globalFrame = this.Entity.GetGlobalFrame();
				globalFrame.origin = this.GoalPositions[this.GoalPositions.Count - 1];
				this.Entity.SetGlobalFrame(globalFrame, true);
				this.ClearGoalPositions();
				return;
			}
			this.Moving = true;
		}

		// Token: 0x06000C6A RID: 3178 RVA: 0x0005C966 File Offset: 0x0005AB66
		public virtual void EnableCollisionBody()
		{
			this.Entity.BodyFlag &= ~BodyFlags.Disabled;
		}

		// Token: 0x06000C6B RID: 3179 RVA: 0x0005C97C File Offset: 0x0005AB7C
		public virtual void DisableCollisionBody()
		{
			this.Entity.BodyFlag |= BodyFlags.Disabled;
		}

		// Token: 0x06000C6C RID: 3180 RVA: 0x0005C994 File Offset: 0x0005AB94
		public void Tick(float dt)
		{
			if (this._moveTiming)
			{
				this._movePauseTimer += dt;
				if (this._movePauseTimer >= this._movePauseDuration)
				{
					this._moveTiming = false;
					this._movePauseTimer = 0f;
				}
				return;
			}
			if (this.Moving && dt > 0f)
			{
				Vec3 vec = new Vec3(0f, 0f, 0f, -1f);
				Vec3 v = this.GoalPositions[this._currentGoalPos] - this.PosBeforeMoving;
				float num = v.Normalize();
				float num2 = num / this._moveSpeed;
				float num3 = this._dtCounter / num2;
				if (this._dtCounter.Equals(0f))
				{
					float x = (this.Entity.GlobalBoxMax - this.Entity.GlobalBoxMin).x;
					float z = (this.Entity.GlobalBoxMax - this.Entity.GlobalBoxMin).z;
					Vec3 v2 = new Vec3(0f, 0f, z / 2f, -1f);
					Vec3 sourcePoint = this.Entity.GetGlobalFrame().origin + v2 + v * (x / 1.8f);
					Vec3 targetPoint = this.GoalPositions[this._currentGoalPos] + v2;
					float num4;
					if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(sourcePoint, targetPoint, out num4, 0.001f, BodyFlags.None))
					{
						this._freePathToDestination = false;
						num = num4;
					}
					else
					{
						this._freePathToDestination = true;
						if (!this._dragged)
						{
							this.PlayPawnMoveSound();
						}
						else
						{
							this.PlayPawnTapSound();
						}
					}
				}
				if (!this._freePathToDestination)
				{
					float num5 = MathF.Sin(num3 * 3.1415927f);
					float num6 = num / 6f;
					num5 *= num6;
					vec += new Vec3(0f, 0f, num5, -1f);
				}
				float dtCounter = this._dtCounter;
				this._dtCounter += dt;
				Vec3 vec2;
				if (num3 >= 1f)
				{
					this._dtCounter = 0f;
					this.CurrentPos = this.GoalPositions[this._currentGoalPos];
					vec = Vec3.Zero;
					if (!this._freePathToDestination && this.IsPlaced)
					{
						this.PlayPawnTapSound();
					}
					else if (!this.IsPlaced)
					{
						this.PlayPawnRemovedTapSound();
					}
					Vec3 v3 = this.GoalPositions[this._currentGoalPos];
					bool flag = true;
					while (this._currentGoalPos < this.GoalPositions.Count - 1)
					{
						this._currentGoalPos++;
						Vec3 v4 = this.GoalPositions[this._currentGoalPos];
						vec2 = v3 - v4;
						if (vec2.LengthSquared > 0f)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						Action<PawnBase, Vec3, Vec3> onArrivedFinalGoalPosition = this.OnArrivedFinalGoalPosition;
						if (onArrivedFinalGoalPosition != null)
						{
							onArrivedFinalGoalPosition(this, this.PosBeforeMoving, this.CurrentPos);
						}
						this.Moving = false;
						this.ClearGoalPositions();
					}
					else
					{
						Action<PawnBase, Vec3, Vec3> onArrivedIntermediateGoalPosition = this.OnArrivedIntermediateGoalPosition;
						if (onArrivedIntermediateGoalPosition != null)
						{
							onArrivedIntermediateGoalPosition(this, this.PosBeforeMoving, this.CurrentPos);
						}
						this._movePauseDuration = 0.3f;
						this._moveTiming = true;
					}
					this.PosBeforeMoving = this.CurrentPos;
				}
				else
				{
					this.Moving = true;
					this.CurrentPos = MBMath.Lerp(this.PosBeforeMoving, this.GoalPositions[this._currentGoalPos], num3, 0.005f);
				}
				ref MatrixFrame ptr = ref this.Entity.GetGlobalFrame();
				vec2 = this.CurrentPos + vec;
				MatrixFrame matrixFrame = new MatrixFrame(ref ptr.rotation, ref vec2);
				this.Entity.SetGlobalFrame(matrixFrame, true);
			}
		}

		// Token: 0x06000C6D RID: 3181 RVA: 0x0005CD3C File Offset: 0x0005AF3C
		public void MovePawnToGoalPositionsDelayed(bool instantMove, float speed, bool dragged, float delay)
		{
			if (this.GoalPositions.Count > 0)
			{
				if (this.GoalPositions.Count == 1 && this.PosBeforeMoving.Equals(this.GoalPositions[0]))
				{
					this.ClearGoalPositions();
					return;
				}
				this.MovePawnToGoalPositions(instantMove, speed, dragged);
				this._movePauseDuration = delay;
				this._moveTiming = delay > 0f;
			}
		}

		// Token: 0x06000C6E RID: 3182 RVA: 0x0005CDB3 File Offset: 0x0005AFB3
		public void SetPlayerOne(bool playerOne)
		{
			this.PlayerOne = playerOne;
		}

		// Token: 0x06000C6F RID: 3183 RVA: 0x0005CDBC File Offset: 0x0005AFBC
		public void ClearGoalPositions()
		{
			this.MovingToDifferentTile = false;
			this.GoalPositions.Clear();
		}

		// Token: 0x06000C70 RID: 3184 RVA: 0x0005CDD0 File Offset: 0x0005AFD0
		public void UpdatePawnPosition()
		{
			this.PosBeforeMoving = this.Entity.GlobalPosition;
		}

		// Token: 0x06000C71 RID: 3185 RVA: 0x0005CDE3 File Offset: 0x0005AFE3
		public void PlayPawnSelectSound()
		{
			Mission.Current.MakeSound(PawnBase.PawnSelectSoundCodeID, this.CurrentPos, true, false, -1, -1);
		}

		// Token: 0x06000C72 RID: 3186 RVA: 0x0005CDFE File Offset: 0x0005AFFE
		private void PlayPawnTapSound()
		{
			Mission.Current.MakeSound(PawnBase.PawnTapSoundCodeID, this.CurrentPos, true, false, -1, -1);
		}

		// Token: 0x06000C73 RID: 3187 RVA: 0x0005CE19 File Offset: 0x0005B019
		private void PlayPawnRemovedTapSound()
		{
			Mission.Current.MakeSound(PawnBase.PawnRemoveSoundCodeID, this.CurrentPos, true, false, -1, -1);
		}

		// Token: 0x06000C74 RID: 3188 RVA: 0x0005CE34 File Offset: 0x0005B034
		private void PlayPawnMoveSound()
		{
			Mission.Current.MakeSound(PawnBase.PawnMoveSoundCodeID, this.CurrentPos, true, false, -1, -1);
		}

		// Token: 0x04000554 RID: 1364
		public Action<PawnBase, Vec3, Vec3> OnArrivedIntermediateGoalPosition;

		// Token: 0x04000555 RID: 1365
		public Action<PawnBase, Vec3, Vec3> OnArrivedFinalGoalPosition;

		// Token: 0x04000556 RID: 1366
		protected Vec3 PosBeforeMovingBase;

		// Token: 0x04000557 RID: 1367
		private int _currentGoalPos;

		// Token: 0x04000558 RID: 1368
		private float _dtCounter;

		// Token: 0x04000559 RID: 1369
		private float _movePauseDuration;

		// Token: 0x0400055A RID: 1370
		private float _movePauseTimer;

		// Token: 0x0400055B RID: 1371
		private float _moveSpeed;

		// Token: 0x0400055C RID: 1372
		private bool _moveTiming;

		// Token: 0x0400055D RID: 1373
		private bool _dragged;

		// Token: 0x0400055E RID: 1374
		private bool _freePathToDestination;
	}
}
