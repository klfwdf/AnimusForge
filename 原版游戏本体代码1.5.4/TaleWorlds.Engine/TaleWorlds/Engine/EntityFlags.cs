using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[Flags]
[EngineStruct("rglEntity_flags", true, "rgl_ef", false)]
public enum EntityFlags : uint
{
	ForceLodMask = 0xF0u,
	ForceLodBits = 4u,
	NoOcclusionCulling = 0x200u,
	IsHelper = 0x400u,
	ComputePerComponentLod = 0x800u,
	DoesNotAffectParentsLocalBb = 0x1000u,
	ForceAsStatic = 0x2000u,
	HideInPrefabEditors = 0x4000u,
	PhysicsDisabled = 0x8000u,
	AlignToTerrain = 0x10000u,
	DontSaveToScene = 0x20000u,
	RecordToSceneReplay = 0x40000u,
	AffectedByEnvironmentDecals = 0x80000u,
	SmoothLodTransitions = 0x100000u,
	DontCheckHandness = 0x200000u,
	NotAffectedBySeason = 0x400000u,
	DontTickChildren = 0x800000u,
	WaitUntilReady = 0x1000000u,
	NonModifiableFromEditor = 0x2000000u,
	PrefabCannotBeBroken = 0x4000000u,
	PerComponentVisibility = 0x8000000u,
	Ignore = 0x10000000u,
	DoNotTick = 0x20000000u,
	DoNotRenderToEnvmap = 0x40000000u,
	AlignRotationToTerrain = 0x80000000u
}
