using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200009E RID: 158
	public struct WorldFrame
	{
		// Token: 0x06000EE6 RID: 3814 RVA: 0x00011444 File Offset: 0x0000F644
		public WorldFrame(Mat3 rotation, WorldPosition origin)
		{
			this.Rotation = rotation;
			this.Origin = origin;
		}

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x06000EE7 RID: 3815 RVA: 0x00011454 File Offset: 0x0000F654
		public bool IsValid
		{
			get
			{
				return this.Origin.IsValid;
			}
		}

		// Token: 0x06000EE8 RID: 3816 RVA: 0x00011464 File Offset: 0x0000F664
		public MatrixFrame ToGroundMatrixFrame()
		{
			Vec3 groundVec = this.Origin.GetGroundVec3();
			return new MatrixFrame(ref this.Rotation, ref groundVec);
		}

		// Token: 0x06000EE9 RID: 3817 RVA: 0x0001148C File Offset: 0x0000F68C
		public MatrixFrame ToGroundMatrixFrameMT()
		{
			Vec3 groundVec3MT = this.Origin.GetGroundVec3MT();
			return new MatrixFrame(ref this.Rotation, ref groundVec3MT);
		}

		// Token: 0x06000EEA RID: 3818 RVA: 0x000114B4 File Offset: 0x0000F6B4
		public MatrixFrame ToNavMeshMatrixFrame()
		{
			Vec3 navMeshVec = this.Origin.GetNavMeshVec3();
			return new MatrixFrame(ref this.Rotation, ref navMeshVec);
		}

		// Token: 0x04000205 RID: 517
		public Mat3 Rotation;

		// Token: 0x04000206 RID: 518
		public WorldPosition Origin;

		// Token: 0x04000207 RID: 519
		public static readonly WorldFrame Invalid = new WorldFrame(Mat3.Identity, WorldPosition.Invalid);
	}
}
