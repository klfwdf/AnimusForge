using System;

namespace TaleWorlds.Core;

[Flags]
public enum ItemFlags : uint
{
	ForceAttachOffHandPrimaryItemBone = 0x100u,
	ForceAttachOffHandSecondaryItemBone = 0x200u,
	AttachmentMask = 0x300u,
	NotUsableByFemale = 0x400u,
	NotUsableByMale = 0x800u,
	DropOnWeaponChange = 0x1000u,
	DropOnAnyAction = 0x2000u,
	CannotBePickedUp = 0x4000u,
	CanBePickedUpFromCorpse = 0x8000u,
	QuickFadeOut = 0x10000u,
	WoodenAttack = 0x20000u,
	WoodenParry = 0x40000u,
	HeldInOffHand = 0x80000u,
	HasToBeHeldUp = 0x100000u,
	UseTeamColor = 0x200000u,
	Civilian = 0x400000u,
	DoNotScaleBodyAccordingToWeaponLength = 0x800000u,
	DoesNotHideChest = 0x1000000u,
	NotStackable = 0x2000000u,
	Stealth = 0x4000000u,
	DoesNotSpawnWhenDropped = 0x8000000u
}
