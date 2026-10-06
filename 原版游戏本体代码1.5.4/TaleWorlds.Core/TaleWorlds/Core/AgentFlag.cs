using System;

namespace TaleWorlds.Core;

[Flags]
public enum AgentFlag : uint
{
	None = 0u,
	Mountable = 1u,
	CanJump = 2u,
	CanRear = 4u,
	CanAttack = 8u,
	CanDefend = 0x10u,
	RunsAwayWhenHit = 0x20u,
	CanCharge = 0x40u,
	CanBeCharged = 0x80u,
	CanClimbLadders = 0x100u,
	CanBeInGroup = 0x200u,
	CanSprint = 0x400u,
	IsHumanoid = 0x800u,
	CanGetScared = 0x1000u,
	CanRide = 0x2000u,
	CanWieldWeapon = 0x4000u,
	CanCrouch = 0x8000u,
	CanGetAlarmed = 0x10000u,
	CanWander = 0x20000u,
	CanKick = 0x80000u,
	CanRetreat = 0x100000u,
	MoveAsHerd = 0x200000u,
	MoveForwardOnly = 0x400000u,
	IsUnique = 0x800000u,
	CanUseAllBowsMounted = 0x1000000u,
	CanReloadAllXBowsMounted = 0x2000000u,
	CanDeflectArrowsWith2HSword = 0x4000000u,
	UnreachableViaNavMesh = 0x8000000u
}
