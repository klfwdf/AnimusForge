using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200007D RID: 125
	[Flags]
	public enum CharacterRestrictionFlags : uint
	{
		// Token: 0x040004D2 RID: 1234
		None = 0U,
		// Token: 0x040004D3 RID: 1235
		NotTransferableInPartyScreen = 1U,
		// Token: 0x040004D4 RID: 1236
		CanNotGoInHideout = 2U
	}
}
