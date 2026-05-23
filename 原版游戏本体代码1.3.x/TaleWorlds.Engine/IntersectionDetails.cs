using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200000B RID: 11
	[EngineStruct("rglIntersection_details", false, null)]
	public enum IntersectionDetails : uint
	{
		// Token: 0x0400000E RID: 14
		None,
		// Token: 0x0400000F RID: 15
		Sphere,
		// Token: 0x04000010 RID: 16
		Plane,
		// Token: 0x04000011 RID: 17
		Capsule,
		// Token: 0x04000012 RID: 18
		Box,
		// Token: 0x04000013 RID: 19
		Convexmesh,
		// Token: 0x04000014 RID: 20
		Trianglemesh,
		// Token: 0x04000015 RID: 21
		Heightfield
	}
}
