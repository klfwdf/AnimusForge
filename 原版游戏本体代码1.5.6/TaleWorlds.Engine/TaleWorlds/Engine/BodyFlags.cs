using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[Flags]
[EngineStruct("rglBody_flags", true, "rgl_bf", false)]
public enum BodyFlags : uint
{
	None = 0u,
	Disabled = 1u,
	NotDestructible = 2u,
	TwoSided = 4u,
	Dynamic = 8u,
	Moveable = 0x10u,
	DynamicConvexHull = 0x20u,
	Ladder = 0x40u,
	OnlyCollideWithRaycast = 0x80u,
	[CustomEngineStructMemberData("ai_limiter")]
	AILimiter = 0x100u,
	Barrier = 0x200u,
	Barrier3D = 0x400u,
	HasSteps = 0x800u,
	Ragdoll = 0x1000u,
	RagdollLimiter = 0x2000u,
	DestructibleDoor = 0x4000u,
	DroppedItem = 0x8000u,
	DoNotCollideWithRaycast = 0x10000u,
	DontTransferToPhysicsEngine = 0x20000u,
	DontCollideWithCamera = 0x40000u,
	ExcludePathSnap = 0x80000u,
	WaterBody = 0x100000u,
	AfterAddFlags = 0u,
	AgentOnly = 0x200000u,
	MissileOnly = 0x400000u,
	HasMaterial = 0x800000u,
	IgnoreSoundOcclusion = 0x10000000u,
	StealthBox = 0x20000000u,
	Sinking = 0x40000000u,
	FloatingDebris = 0x80000000u,
	BodyFlagFilter = Disabled | NotDestructible | TwoSided | Dynamic | Moveable | DynamicConvexHull | Ladder | OnlyCollideWithRaycast | AILimiter | Barrier | Barrier3D | HasSteps | Ragdoll | RagdollLimiter | DestructibleDoor | DroppedItem | DoNotCollideWithRaycast | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | WaterBody | AgentOnly | MissileOnly | HasMaterial | IgnoreSoundOcclusion | StealthBox | Sinking | FloatingDebris,
	BodyOwnerNone = 0u,
	BodyOwnerEntity = 0x1000000u,
	BodyOwnerTerrain = 0x2000000u,
	BodyOwnerFlora = 0x4000000u,
	BodyOwnerFilter = ~BodyFlagFilter,
	CommonCollisionExcludeFlags = ~(BodyOwnerFilter | NotDestructible | TwoSided | Moveable | DynamicConvexHull | Ladder | Barrier | Barrier3D | HasSteps | DestructibleDoor | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | HasMaterial | IgnoreSoundOcclusion | Sinking | FloatingDebris),
	CameraCollisionRayCastExludeFlags = ~(BodyOwnerFilter | NotDestructible | TwoSided | Moveable | DynamicConvexHull | HasSteps | DestructibleDoor | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | HasMaterial | IgnoreSoundOcclusion | Sinking | FloatingDebris),
	CommonCollisionExcludeFlagsForAgent = ~(BodyOwnerFilter | NotDestructible | TwoSided | Moveable | DynamicConvexHull | Ladder | Barrier | Barrier3D | HasSteps | DestructibleDoor | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | AgentOnly | HasMaterial | IgnoreSoundOcclusion | Sinking | FloatingDebris),
	CommonCollisionExcludeFlagsForMissile = ~(BodyOwnerFilter | NotDestructible | TwoSided | Moveable | DynamicConvexHull | Ladder | HasSteps | DestructibleDoor | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | MissileOnly | HasMaterial | IgnoreSoundOcclusion | Sinking | FloatingDebris),
	CommonCollisionExcludeFlagsForCombat = ~(BodyOwnerFilter | NotDestructible | TwoSided | Moveable | DynamicConvexHull | Ladder | Barrier | Barrier3D | HasSteps | DestructibleDoor | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | MissileOnly | HasMaterial | IgnoreSoundOcclusion | Sinking | FloatingDebris),
	CommonCollisionExcludeFlagsForEditor = ~(BodyOwnerFilter | NotDestructible | TwoSided | Moveable | DynamicConvexHull | Ladder | Barrier | Barrier3D | HasSteps | DestructibleDoor | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | MissileOnly | HasMaterial | IgnoreSoundOcclusion | Sinking | FloatingDebris),
	CommonFlagsThatDoNotBlockRay = ~(BodyOwnerFilter | Ladder | OnlyCollideWithRaycast | DestructibleDoor | DroppedItem),
	CommonFocusRayCastExcludeFlags = ~(BodyOwnerFilter | NotDestructible | TwoSided | Dynamic | Moveable | DynamicConvexHull | Ladder | OnlyCollideWithRaycast | HasSteps | DestructibleDoor | DroppedItem | DontTransferToPhysicsEngine | DontCollideWithCamera | ExcludePathSnap | WaterBody | AgentOnly | MissileOnly | HasMaterial | IgnoreSoundOcclusion | StealthBox | Sinking | FloatingDebris)
}
