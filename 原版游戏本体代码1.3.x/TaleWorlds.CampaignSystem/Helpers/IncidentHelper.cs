using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x02000015 RID: 21
	public static class IncidentHelper
	{
		// Token: 0x060000BC RID: 188 RVA: 0x0000A2F4 File Offset: 0x000084F4
		public static T GetSeededRandomElement<T>(List<T> list, long seed)
		{
			if (list == null || list.Count == 0)
			{
				return default(T);
			}
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)seed, list.Count)];
		}

		// Token: 0x060000BD RID: 189 RVA: 0x0000A330 File Offset: 0x00008530
		public static T GetSeededRandomElement<T>(MBList<T> list, long seed)
		{
			if (list == null || list.Count == 0)
			{
				return default(T);
			}
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)seed, list.Count)];
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000A36C File Offset: 0x0000856C
		public static T GetSeededRandomElement<T>(MBReadOnlyList<T> list, long seed)
		{
			if (list == null || list.Count == 0)
			{
				return default(T);
			}
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)seed, list.Count)];
		}
	}
}
