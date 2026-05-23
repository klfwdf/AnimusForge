using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000062 RID: 98
	[Flags]
	public enum TroopUsageFlags : ushort
	{
		// Token: 0x040003BD RID: 957
		None = 0,
		// Token: 0x040003BE RID: 958
		OnFoot = 1,
		// Token: 0x040003BF RID: 959
		Mounted = 2,
		// Token: 0x040003C0 RID: 960
		Melee = 4,
		// Token: 0x040003C1 RID: 961
		Ranged = 8,
		// Token: 0x040003C2 RID: 962
		OneHandedUser = 16,
		// Token: 0x040003C3 RID: 963
		ShieldUser = 32,
		// Token: 0x040003C4 RID: 964
		TwoHandedUser = 64,
		// Token: 0x040003C5 RID: 965
		PolearmUser = 128,
		// Token: 0x040003C6 RID: 966
		BowUser = 256,
		// Token: 0x040003C7 RID: 967
		ThrownUser = 512,
		// Token: 0x040003C8 RID: 968
		CrossbowUser = 1024,
		// Token: 0x040003C9 RID: 969
		Undefined = 65535
	}
}
