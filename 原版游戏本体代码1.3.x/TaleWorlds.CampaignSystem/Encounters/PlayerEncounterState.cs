using System;

namespace TaleWorlds.CampaignSystem.Encounters
{
	// Token: 0x020002EA RID: 746
	public enum PlayerEncounterState
	{
		// Token: 0x04000BED RID: 3053
		Begin,
		// Token: 0x04000BEE RID: 3054
		Wait,
		// Token: 0x04000BEF RID: 3055
		PrepareResults,
		// Token: 0x04000BF0 RID: 3056
		ApplyResults,
		// Token: 0x04000BF1 RID: 3057
		PlayerVictory,
		// Token: 0x04000BF2 RID: 3058
		PlayerTotalDefeat,
		// Token: 0x04000BF3 RID: 3059
		CaptureHeroes,
		// Token: 0x04000BF4 RID: 3060
		FreeHeroes,
		// Token: 0x04000BF5 RID: 3061
		LootParty,
		// Token: 0x04000BF6 RID: 3062
		LootInventory,
		// Token: 0x04000BF7 RID: 3063
		LootShips,
		// Token: 0x04000BF8 RID: 3064
		End
	}
}
