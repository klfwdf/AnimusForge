using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000008 RID: 8
	[EngineStruct("ftlCapsule_data", false, null)]
	internal struct FtlCapsuleData
	{
		// Token: 0x0600002B RID: 43 RVA: 0x00002570 File Offset: 0x00000770
		public Vec3 GetBoxMin()
		{
			return new Vec3(MathF.Min(this.P1.x, this.P2.x) - this.Radius, MathF.Min(this.P1.y, this.P2.y) - this.Radius, MathF.Min(this.P1.z, this.P2.z) - this.Radius, -1f);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000025F0 File Offset: 0x000007F0
		public Vec3 GetBoxMax()
		{
			return new Vec3(MathF.Max(this.P1.x, this.P2.x) + this.Radius, MathF.Max(this.P1.y, this.P2.y) + this.Radius, MathF.Max(this.P1.z, this.P2.z) + this.Radius, -1f);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000266D File Offset: 0x0000086D
		public FtlCapsuleData(float radius, Vec3 p1, Vec3 p2)
		{
			this.P1 = p1;
			this.P2 = p2;
			this.Radius = radius;
		}

		// Token: 0x04000006 RID: 6
		public Vec3 P1;

		// Token: 0x04000007 RID: 7
		public Vec3 P2;

		// Token: 0x04000008 RID: 8
		public float Radius;
	}
}
