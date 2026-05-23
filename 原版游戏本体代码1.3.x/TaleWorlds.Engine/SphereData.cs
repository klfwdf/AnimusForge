using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200000A RID: 10
	[EngineStruct("ftlSphere_data", false, null)]
	public struct SphereData
	{
		// Token: 0x0600003C RID: 60 RVA: 0x0000281A File Offset: 0x00000A1A
		public SphereData(float radius, Vec3 origin)
		{
			this.Radius = radius;
			this.Origin = origin;
		}

		// Token: 0x0400000B RID: 11
		public Vec3 Origin;

		// Token: 0x0400000C RID: 12
		public float Radius;
	}
}
