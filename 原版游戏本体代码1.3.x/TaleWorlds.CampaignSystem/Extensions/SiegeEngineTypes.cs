using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x0200016C RID: 364
	public static class SiegeEngineTypes
	{
		// Token: 0x170006E6 RID: 1766
		// (get) Token: 0x06001AF8 RID: 6904 RVA: 0x0008AC52 File Offset: 0x00088E52
		public static MBReadOnlyList<SiegeEngineType> All
		{
			get
			{
				return Campaign.Current.AllSiegeEngineTypes;
			}
		}
	}
}
