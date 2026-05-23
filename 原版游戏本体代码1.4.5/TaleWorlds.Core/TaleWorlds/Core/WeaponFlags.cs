using System;

namespace TaleWorlds.Core;

[Flags]
public enum WeaponFlags : ulong
{
	MeleeWeapon = 1uL,
	RangedWeapon = 2uL,
	WeaponMask = 3uL,
	FirearmAmmo = 4uL,
	NotUsableWithOneHand = 0x10uL,
	NotUsableWithTwoHand = 0x20uL,
	HandUsageMask = 0x30uL,
	WideGrip = 0x40uL,
	AttachAmmoToVisual = 0x80uL,
	Consumable = 0x100uL,
	HasHitPoints = 0x200uL,
	DataValueMask = 0x300uL,
	HasString = 0x400uL,
	StringHeldByHand = 0xC00uL,
	UnloadWhenSheathed = 0x1000uL,
	AffectsArea = 0x2000uL,
	AffectsAreaBig = 0x4000uL,
	Burning = 0x8000uL,
	BonusAgainstShield = 0x10000uL,
	CanPenetrateShield = 0x20000uL,
	CantReloadOnHorseback = 0x40000uL,
	AutoReload = 0x80000uL,
	CanBeUsedWhileCrouched = 0x100000uL,
	TwoHandIdleOnMount = 0x200000uL,
	NoBlood = 0x400000uL,
	PenaltyWithShield = 0x800000uL,
	CanDismount = 0x1000000uL,
	CanHook = 0x2000000uL,
	CanKnockDown = 0x4000000uL,
	CanCrushThrough = 0x8000000uL,
	CanBlockRanged = 0x10000000uL,
	MissileWithPhysics = 0x20000000uL,
	MultiplePenetration = 0x40000000uL,
	LeavesTrail = 0x80000000uL,
	UseHandAsThrowBase = 0x100000000uL,
	HeldBackwards = 0x200000000uL,
	CanKillEvenIfBlunt = 0x400000000uL,
	AmmoBreaksOnBounceBack = 0x1000000000uL,
	AmmoCanBreakOnBounceBack = 0x2000000000uL,
	AmmoBreakOnBounceBackMask = 0x3000000000uL,
	AmmoSticksWhenShot = 0x4000000000uL
}
