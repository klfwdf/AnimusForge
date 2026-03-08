using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000077 RID: 119
	[Flags]
	[EngineStruct("rglPhysics_material::rglPhymat_flags", true, "rgl_phymat", false)]
	public enum PhysicsMaterialFlags : byte
	{
		// Token: 0x04000165 RID: 357
		None = 0,
		// Token: 0x04000166 RID: 358
		DontStickMissiles = 1,
		// Token: 0x04000167 RID: 359
		Flammable = 2,
		// Token: 0x04000168 RID: 360
		RainSplashesEnabled = 4,
		// Token: 0x04000169 RID: 361
		AttacksCanPassThrough = 8
	}
}
