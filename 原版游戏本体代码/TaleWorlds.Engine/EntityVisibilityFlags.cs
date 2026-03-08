using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000048 RID: 72
	[Flags]
	[EngineStruct("rglEntity_visibility_mask_flags", true, "rgl_entity_vismask", false)]
	public enum EntityVisibilityFlags : uint
	{
		// Token: 0x04000076 RID: 118
		None = 0U,
		// Token: 0x04000077 RID: 119
		VisibleOnlyWhenEditing = 2U,
		// Token: 0x04000078 RID: 120
		NoShadow = 4U,
		// Token: 0x04000079 RID: 121
		VisibleOnlyForEnvmap = 8U,
		// Token: 0x0400007A RID: 122
		NotVisibleForEnvmap = 16U
	}
}
