using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[Flags]
[EngineStruct("rglVisibility_mask_flags", true, "rgl_vismask", false)]
public enum VisibilityMaskFlags : uint
{
	Final = 1u,
	ShadowStatic = 0x10u,
	ShadowDynamic = 0x20u,
	ForEnvmap = 0x40u,
	EditModeAtmosphere = 0x10000000u,
	EditModeLight = 0x20000000u,
	EditModeParticleSystem = 0x40000000u,
	EditModeHelpers = 0x80000000u,
	EditModeTerrain = 0x1000000u,
	EditModeGameEntity = 0x2000000u,
	EditModeFloraEntity = 0x4000000u,
	EditModeLayerFlora = 0x8000000u,
	EditModeShadows = 0x100000u,
	EditModeBorders = 0x200000u,
	EditModeEditingEntity = 0x400000u,
	EditModeAnimations = 0x800000u,
	EditModeCubemapReflector = 0x10000u,
	EditModeDecals = 0x20000u,
	EditModeNavigation_mesh = 0x40000u,
	EditModeSound_entities = 0x80000u,
	EditModeWater = 0x1000u,
	EditModeIsolate_mode = 0x2000u,
	EditModeAny = 0xFFFFF000u,
	Default = 1u,
	DefaultStatic = 0x31u,
	DefaultDynamic = 0x21u,
	DefaultStaticWithoutDynamic = 0x11u
}
