using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000066 RID: 102
	[Flags]
	[EngineStruct("rglVisibility_mask_flags", true, "rgl_vismask", false)]
	public enum VisibilityMaskFlags : uint
	{
		// Token: 0x04000120 RID: 288
		Final = 1U,
		// Token: 0x04000121 RID: 289
		ShadowStatic = 16U,
		// Token: 0x04000122 RID: 290
		ShadowDynamic = 32U,
		// Token: 0x04000123 RID: 291
		ForEnvmap = 64U,
		// Token: 0x04000124 RID: 292
		EditModeAtmosphere = 268435456U,
		// Token: 0x04000125 RID: 293
		EditModeLight = 536870912U,
		// Token: 0x04000126 RID: 294
		EditModeParticleSystem = 1073741824U,
		// Token: 0x04000127 RID: 295
		EditModeHelpers = 2147483648U,
		// Token: 0x04000128 RID: 296
		EditModeTerrain = 16777216U,
		// Token: 0x04000129 RID: 297
		EditModeGameEntity = 33554432U,
		// Token: 0x0400012A RID: 298
		EditModeFloraEntity = 67108864U,
		// Token: 0x0400012B RID: 299
		EditModeLayerFlora = 134217728U,
		// Token: 0x0400012C RID: 300
		EditModeShadows = 1048576U,
		// Token: 0x0400012D RID: 301
		EditModeBorders = 2097152U,
		// Token: 0x0400012E RID: 302
		EditModeEditingEntity = 4194304U,
		// Token: 0x0400012F RID: 303
		EditModeAnimations = 8388608U,
		// Token: 0x04000130 RID: 304
		EditModeCubemapReflector = 65536U,
		// Token: 0x04000131 RID: 305
		EditModeDecals = 131072U,
		// Token: 0x04000132 RID: 306
		EditModeNavigation_mesh = 262144U,
		// Token: 0x04000133 RID: 307
		EditModeSound_entities = 524288U,
		// Token: 0x04000134 RID: 308
		EditModeWater = 4096U,
		// Token: 0x04000135 RID: 309
		EditModeIsolate_mode = 8192U,
		// Token: 0x04000136 RID: 310
		EditModeAny = 4294963200U,
		// Token: 0x04000137 RID: 311
		Default = 1U,
		// Token: 0x04000138 RID: 312
		DefaultStatic = 49U,
		// Token: 0x04000139 RID: 313
		DefaultDynamic = 33U,
		// Token: 0x0400013A RID: 314
		DefaultStaticWithoutDynamic = 17U
	}
}
