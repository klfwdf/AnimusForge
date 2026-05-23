using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200008D RID: 141
	public interface IMissionSiegeWeapon
	{
		// Token: 0x170002EB RID: 747
		// (get) Token: 0x060008A3 RID: 2211
		int Index { get; }

		// Token: 0x170002EC RID: 748
		// (get) Token: 0x060008A4 RID: 2212
		SiegeEngineType Type { get; }

		// Token: 0x170002ED RID: 749
		// (get) Token: 0x060008A5 RID: 2213
		float Health { get; }

		// Token: 0x170002EE RID: 750
		// (get) Token: 0x060008A6 RID: 2214
		float InitialHealth { get; }

		// Token: 0x170002EF RID: 751
		// (get) Token: 0x060008A7 RID: 2215
		float MaxHealth { get; }
	}
}
