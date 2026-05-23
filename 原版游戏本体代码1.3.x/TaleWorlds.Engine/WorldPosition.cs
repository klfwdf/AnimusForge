using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x020000A0 RID: 160
	[EngineStruct("rglWorld_position::Plain_world_position", false, null)]
	public struct WorldPosition
	{
		// Token: 0x170000BA RID: 186
		// (get) Token: 0x06000EEC RID: 3820 RVA: 0x000114F0 File Offset: 0x0000F6F0
		public Vec2 AsVec2
		{
			get
			{
				return this._position.AsVec2;
			}
		}

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x06000EED RID: 3821 RVA: 0x000114FD File Offset: 0x0000F6FD
		public float X
		{
			get
			{
				return this._position.x;
			}
		}

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x06000EEE RID: 3822 RVA: 0x0001150A File Offset: 0x0000F70A
		public float Y
		{
			get
			{
				return this._position.y;
			}
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x06000EEF RID: 3823 RVA: 0x00011518 File Offset: 0x0000F718
		public bool IsValid
		{
			get
			{
				return this.AsVec2.IsValid && this._scene != UIntPtr.Zero;
			}
		}

		// Token: 0x06000EF0 RID: 3824 RVA: 0x00011547 File Offset: 0x0000F747
		internal WorldPosition(UIntPtr scenePointer, Vec3 position)
		{
			this = new WorldPosition(scenePointer, UIntPtr.Zero, position, false);
		}

		// Token: 0x06000EF1 RID: 3825 RVA: 0x00011558 File Offset: 0x0000F758
		internal WorldPosition(UIntPtr scenePointer, UIntPtr navMesh, Vec3 position, bool hasValidZ)
		{
			this._scene = scenePointer;
			this._navMesh = navMesh;
			this._nearestNavMesh = this._navMesh;
			this._position = position;
			this.Normal = Vec3.Zero;
			if (hasValidZ)
			{
				this._lastValidZPosition = this._position.AsVec2;
				this.State = ZValidityState.Valid;
				return;
			}
			this._lastValidZPosition = Vec2.Invalid;
			this.State = ZValidityState.Invalid;
		}

		// Token: 0x06000EF2 RID: 3826 RVA: 0x000115C0 File Offset: 0x0000F7C0
		public WorldPosition(Scene scene, Vec3 position)
		{
			this = new WorldPosition((scene != null) ? scene.Pointer : UIntPtr.Zero, UIntPtr.Zero, position, false);
		}

		// Token: 0x06000EF3 RID: 3827 RVA: 0x000115E5 File Offset: 0x0000F7E5
		public WorldPosition(Scene scene, UIntPtr navMesh, Vec3 position, bool hasValidZ)
		{
			this = new WorldPosition((scene != null) ? scene.Pointer : UIntPtr.Zero, navMesh, position, hasValidZ);
		}

		// Token: 0x06000EF4 RID: 3828 RVA: 0x00011608 File Offset: 0x0000F808
		public void SetVec3(UIntPtr navMesh, Vec3 position, bool hasValidZ)
		{
			this._navMesh = navMesh;
			this._nearestNavMesh = this._navMesh;
			this._position = position;
			this.Normal = Vec3.Zero;
			if (hasValidZ)
			{
				this._lastValidZPosition = this._position.AsVec2;
				this.State = ZValidityState.Valid;
				return;
			}
			this._lastValidZPosition = Vec2.Invalid;
			this.State = ZValidityState.Invalid;
		}

		// Token: 0x06000EF5 RID: 3829 RVA: 0x00011668 File Offset: 0x0000F868
		private void ValidateZ(ZValidityState minimumValidityState)
		{
			if (this.State < minimumValidityState)
			{
				EngineApplicationInterface.IScene.WorldPositionValidateZ(ref this, (int)minimumValidityState);
			}
		}

		// Token: 0x06000EF6 RID: 3830 RVA: 0x00011680 File Offset: 0x0000F880
		private void ValidateZMT(ZValidityState minimumValidityState)
		{
			if (this.State < minimumValidityState)
			{
				using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
				{
					EngineApplicationInterface.IScene.WorldPositionValidateZ(ref this, (int)minimumValidityState);
				}
			}
		}

		// Token: 0x06000EF7 RID: 3831 RVA: 0x000116D0 File Offset: 0x0000F8D0
		public UIntPtr GetNavMesh()
		{
			this.ValidateZ(ZValidityState.ValidAccordingToNavMesh);
			return this._navMesh;
		}

		// Token: 0x06000EF8 RID: 3832 RVA: 0x000116DF File Offset: 0x0000F8DF
		public UIntPtr GetNavMeshMT()
		{
			this.ValidateZMT(ZValidityState.ValidAccordingToNavMesh);
			return this._navMesh;
		}

		// Token: 0x06000EF9 RID: 3833 RVA: 0x000116EE File Offset: 0x0000F8EE
		public UIntPtr GetNearestNavMesh()
		{
			EngineApplicationInterface.IScene.WorldPositionComputeNearestNavMesh(ref this);
			return this._nearestNavMesh;
		}

		// Token: 0x06000EFA RID: 3834 RVA: 0x00011701 File Offset: 0x0000F901
		public float GetNavMeshZ()
		{
			this.ValidateZ(ZValidityState.ValidAccordingToNavMesh);
			if (this.State >= ZValidityState.ValidAccordingToNavMesh)
			{
				return this._position.z;
			}
			return float.NaN;
		}

		// Token: 0x06000EFB RID: 3835 RVA: 0x00011724 File Offset: 0x0000F924
		public float GetNavMeshZMT()
		{
			this.ValidateZMT(ZValidityState.ValidAccordingToNavMesh);
			if (this.State >= ZValidityState.ValidAccordingToNavMesh)
			{
				return this._position.z;
			}
			return float.NaN;
		}

		// Token: 0x06000EFC RID: 3836 RVA: 0x00011747 File Offset: 0x0000F947
		public float GetGroundZ()
		{
			this.ValidateZ(ZValidityState.Valid);
			if (this.State >= ZValidityState.Valid)
			{
				return this._position.z;
			}
			return float.NaN;
		}

		// Token: 0x06000EFD RID: 3837 RVA: 0x0001176A File Offset: 0x0000F96A
		public float GetGroundZMT()
		{
			this.ValidateZMT(ZValidityState.Valid);
			if (this.State >= ZValidityState.Valid)
			{
				return this._position.z;
			}
			return float.NaN;
		}

		// Token: 0x06000EFE RID: 3838 RVA: 0x0001178D File Offset: 0x0000F98D
		public Vec3 GetNavMeshVec3()
		{
			return new Vec3(this._position.AsVec2, this.GetNavMeshZ(), -1f);
		}

		// Token: 0x06000EFF RID: 3839 RVA: 0x000117AA File Offset: 0x0000F9AA
		public Vec3 GetNavMeshVec3MT()
		{
			return new Vec3(this._position.AsVec2, this.GetNavMeshZMT(), -1f);
		}

		// Token: 0x06000F00 RID: 3840 RVA: 0x000117C7 File Offset: 0x0000F9C7
		public Vec3 GetGroundVec3()
		{
			return new Vec3(this._position.AsVec2, this.GetGroundZ(), -1f);
		}

		// Token: 0x06000F01 RID: 3841 RVA: 0x000117E4 File Offset: 0x0000F9E4
		public Vec3 GetGroundVec3MT()
		{
			return new Vec3(this._position.AsVec2, this.GetGroundZMT(), -1f);
		}

		// Token: 0x06000F02 RID: 3842 RVA: 0x00011801 File Offset: 0x0000FA01
		public Vec3 GetVec3WithoutValidity()
		{
			return this._position;
		}

		// Token: 0x06000F03 RID: 3843 RVA: 0x0001180C File Offset: 0x0000FA0C
		public void SetVec2MT(Vec2 value)
		{
			if (this._position.AsVec2 != value)
			{
				if (this.State != ZValidityState.Invalid)
				{
					this.State = ZValidityState.Invalid;
				}
				else if (!this._lastValidZPosition.IsValid)
				{
					this.ValidateZMT(ZValidityState.ValidAccordingToNavMesh);
					this.State = ZValidityState.Invalid;
				}
				this._position.x = value.x;
				this._position.y = value.y;
			}
		}

		// Token: 0x06000F04 RID: 3844 RVA: 0x0001187C File Offset: 0x0000FA7C
		public void SetVec2(Vec2 value)
		{
			if (this._position.AsVec2 != value)
			{
				if (this.State != ZValidityState.Invalid)
				{
					this.State = ZValidityState.Invalid;
				}
				else if (!this._lastValidZPosition.IsValid)
				{
					this.ValidateZ(ZValidityState.ValidAccordingToNavMesh);
					this.State = ZValidityState.Invalid;
				}
				this._position.x = value.x;
				this._position.y = value.y;
			}
		}

		// Token: 0x06000F05 RID: 3845 RVA: 0x000118EC File Offset: 0x0000FAEC
		public float DistanceSquaredWithLimit(in Vec3 targetPoint, float limitSquared)
		{
			Vec2 asVec = this._position.AsVec2;
			Vec3 vec = targetPoint;
			float num = asVec.DistanceSquared(vec.AsVec2);
			if (num <= limitSquared)
			{
				return this.GetGroundVec3().DistanceSquared(targetPoint);
			}
			return num;
		}

		// Token: 0x0400020D RID: 525
		private readonly UIntPtr _scene;

		// Token: 0x0400020E RID: 526
		private UIntPtr _navMesh;

		// Token: 0x0400020F RID: 527
		private UIntPtr _nearestNavMesh;

		// Token: 0x04000210 RID: 528
		private Vec3 _position;

		// Token: 0x04000211 RID: 529
		[CustomEngineStructMemberData("normal_")]
		public Vec3 Normal;

		// Token: 0x04000212 RID: 530
		private Vec2 _lastValidZPosition;

		// Token: 0x04000213 RID: 531
		[CustomEngineStructMemberData("z_validity_state_")]
		public ZValidityState State;

		// Token: 0x04000214 RID: 532
		public static readonly WorldPosition Invalid = new WorldPosition(UIntPtr.Zero, UIntPtr.Zero, Vec3.Invalid, false);

		// Token: 0x020000E2 RID: 226
		public enum WorldPositionEnforcedCache
		{
			// Token: 0x040004DC RID: 1244
			None,
			// Token: 0x040004DD RID: 1245
			NavMeshVec3,
			// Token: 0x040004DE RID: 1246
			GroundVec3
		}
	}
}
