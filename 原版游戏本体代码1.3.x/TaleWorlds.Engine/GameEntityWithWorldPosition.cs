using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200004F RID: 79
	public class GameEntityWithWorldPosition
	{
		// Token: 0x06000856 RID: 2134 RVA: 0x00006708 File Offset: 0x00004908
		public GameEntityWithWorldPosition(WeakGameEntity gameEntity)
		{
			this._customLocalFrame = MatrixFrame.Identity;
			this._gameEntity = gameEntity;
			Scene scene = gameEntity.Scene;
			float groundHeightAtPosition = scene.GetGroundHeightAtPosition(gameEntity.GlobalPosition, BodyFlags.CommonCollisionExcludeFlags);
			this._worldPosition = new WorldPosition(scene, UIntPtr.Zero, new Vec3(gameEntity.GlobalPosition.AsVec2, groundHeightAtPosition, -1f), false);
			this._worldPosition.GetGroundVec3();
			this._orthonormalRotation = gameEntity.GetGlobalFrame().rotation;
			this._orthonormalRotation.Orthonormalize();
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000857 RID: 2135 RVA: 0x0000679D File Offset: 0x0000499D
		public WeakGameEntity GameEntity
		{
			get
			{
				return this._gameEntity;
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000858 RID: 2136 RVA: 0x000067A5 File Offset: 0x000049A5
		public WorldPosition WorldPosition
		{
			get
			{
				this.ValidateWorldPosition();
				return this._worldPosition;
			}
		}

		// Token: 0x06000859 RID: 2137 RVA: 0x000067B4 File Offset: 0x000049B4
		private void ValidateWorldPosition()
		{
			Vec3 position = (this._customLocalFrame.IsIdentity ? this.GameEntity.GetGlobalFrame().origin : this.GameEntity.GetGlobalFrame().TransformToParent(this._customLocalFrame).origin);
			if (!this._worldPosition.AsVec2.NearlyEquals(position.AsVec2, 1E-05f))
			{
				this._worldPosition.SetVec3(UIntPtr.Zero, position, false);
			}
		}

		// Token: 0x0600085A RID: 2138 RVA: 0x00006838 File Offset: 0x00004A38
		public void InvalidateWorldPosition()
		{
			this._worldPosition.State = ZValidityState.Invalid;
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x0600085B RID: 2139 RVA: 0x00006848 File Offset: 0x00004A48
		public WorldFrame WorldFrame
		{
			get
			{
				Mat3 orthonormalRotation = (this._customLocalFrame.rotation.IsIdentity() ? this.GameEntity.GetGlobalFrame().rotation : this.GameEntity.GetGlobalFrame().rotation.TransformToParent(this._customLocalFrame.rotation));
				if (!orthonormalRotation.NearlyEquals(this._orthonormalRotation, 1E-05f))
				{
					this._orthonormalRotation = orthonormalRotation;
					this._orthonormalRotation.Orthonormalize();
				}
				return new WorldFrame(this._orthonormalRotation, this.WorldPosition);
			}
		}

		// Token: 0x0600085C RID: 2140 RVA: 0x000068DA File Offset: 0x00004ADA
		public void SetCustomLocalFrame(in MatrixFrame customLocalFrame)
		{
			this._customLocalFrame = customLocalFrame;
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x0600085D RID: 2141 RVA: 0x000068E8 File Offset: 0x00004AE8
		public Vec2 AsVec2
		{
			get
			{
				this.ValidateWorldPosition();
				return this._worldPosition.AsVec2;
			}
		}

		// Token: 0x0600085E RID: 2142 RVA: 0x000068FB File Offset: 0x00004AFB
		public UIntPtr GetNavMesh()
		{
			this.ValidateWorldPosition();
			return this._worldPosition.GetNavMesh();
		}

		// Token: 0x0600085F RID: 2143 RVA: 0x0000690E File Offset: 0x00004B0E
		public Vec3 GetNavMeshVec3()
		{
			this.ValidateWorldPosition();
			return this._worldPosition.GetNavMeshVec3();
		}

		// Token: 0x040000B2 RID: 178
		private MatrixFrame _customLocalFrame;

		// Token: 0x040000B3 RID: 179
		private readonly WeakGameEntity _gameEntity;

		// Token: 0x040000B4 RID: 180
		private WorldPosition _worldPosition;

		// Token: 0x040000B5 RID: 181
		private Mat3 _orthonormalRotation;
	}
}
