using System;

namespace Steamworks
{
	// Token: 0x0200014C RID: 332
	[Flags]
	public enum EChatSteamIDInstanceFlags
	{
		// Token: 0x04000835 RID: 2101
		k_EChatAccountInstanceMask = 4095,
		// Token: 0x04000836 RID: 2102
		k_EChatInstanceFlagClan = 524288,
		// Token: 0x04000837 RID: 2103
		k_EChatInstanceFlagLobby = 262144,
		// Token: 0x04000838 RID: 2104
		k_EChatInstanceFlagMMSLobby = 131072
	}
}
