using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[Flags]
[EngineStruct("rglEntity_visibility_mask_flags", true, "rgl_entity_vismask", false)]
public enum EntityVisibilityFlags : uint
{
	None = 0u,
	VisibleOnlyWhenEditing = 2u,
	NoShadow = 4u,
	VisibleOnlyForEnvmap = 8u,
	NotVisibleForEnvmap = 0x10u
}
