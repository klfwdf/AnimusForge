using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200009D RID: 157
	[Flags]
	public enum EquipmentFlags : uint
	{
		// Token: 0x04000502 RID: 1282
		None = 0U,
		// Token: 0x04000503 RID: 1283
		IsWandererEquipment = 1U,
		// Token: 0x04000504 RID: 1284
		IsGentryEquipment = 2U,
		// Token: 0x04000505 RID: 1285
		IsRebelHeroEquipment = 4U,
		// Token: 0x04000506 RID: 1286
		IsNoncombatantTemplate = 8U,
		// Token: 0x04000507 RID: 1287
		IsCombatantTemplate = 16U,
		// Token: 0x04000508 RID: 1288
		IsCivilianTemplate = 32U,
		// Token: 0x04000509 RID: 1289
		IsNobleTemplate = 64U,
		// Token: 0x0400050A RID: 1290
		IsFemaleTemplate = 128U,
		// Token: 0x0400050B RID: 1291
		IsMediumTemplate = 256U,
		// Token: 0x0400050C RID: 1292
		IsHeavyTemplate = 512U,
		// Token: 0x0400050D RID: 1293
		IsFlamboyantTemplate = 1024U,
		// Token: 0x0400050E RID: 1294
		IsStoicTemplate = 2048U,
		// Token: 0x0400050F RID: 1295
		IsNomadTemplate = 4096U,
		// Token: 0x04000510 RID: 1296
		IsWoodlandTemplate = 8192U,
		// Token: 0x04000511 RID: 1297
		IsChildEquipmentTemplate = 16384U,
		// Token: 0x04000512 RID: 1298
		IsTeenagerEquipmentTemplate = 32768U
	}
}
