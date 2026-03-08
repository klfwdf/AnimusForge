using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000094 RID: 148
	[Flags]
	public enum ItemFlags : uint
	{
		// Token: 0x04000463 RID: 1123
		ForceAttachOffHandPrimaryItemBone = 256U,
		// Token: 0x04000464 RID: 1124
		ForceAttachOffHandSecondaryItemBone = 512U,
		// Token: 0x04000465 RID: 1125
		AttachmentMask = 768U,
		// Token: 0x04000466 RID: 1126
		NotUsableByFemale = 1024U,
		// Token: 0x04000467 RID: 1127
		NotUsableByMale = 2048U,
		// Token: 0x04000468 RID: 1128
		DropOnWeaponChange = 4096U,
		// Token: 0x04000469 RID: 1129
		DropOnAnyAction = 8192U,
		// Token: 0x0400046A RID: 1130
		CannotBePickedUp = 16384U,
		// Token: 0x0400046B RID: 1131
		CanBePickedUpFromCorpse = 32768U,
		// Token: 0x0400046C RID: 1132
		QuickFadeOut = 65536U,
		// Token: 0x0400046D RID: 1133
		WoodenAttack = 131072U,
		// Token: 0x0400046E RID: 1134
		WoodenParry = 262144U,
		// Token: 0x0400046F RID: 1135
		HeldInOffHand = 524288U,
		// Token: 0x04000470 RID: 1136
		HasToBeHeldUp = 1048576U,
		// Token: 0x04000471 RID: 1137
		UseTeamColor = 2097152U,
		// Token: 0x04000472 RID: 1138
		Civilian = 4194304U,
		// Token: 0x04000473 RID: 1139
		DoNotScaleBodyAccordingToWeaponLength = 8388608U,
		// Token: 0x04000474 RID: 1140
		DoesNotHideChest = 16777216U,
		// Token: 0x04000475 RID: 1141
		NotStackable = 33554432U,
		// Token: 0x04000476 RID: 1142
		Stealth = 67108864U,
		// Token: 0x04000477 RID: 1143
		DoesNotSpawnWhenDropped = 134217728U
	}
}
