using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x0200016A RID: 362
	public static class Attributes
	{
		// Token: 0x170006E4 RID: 1764
		// (get) Token: 0x06001AF6 RID: 6902 RVA: 0x0008AC3A File Offset: 0x00088E3A
		public static MBReadOnlyList<CharacterAttribute> All
		{
			get
			{
				return Campaign.Current.AllCharacterAttributes;
			}
		}
	}
}
