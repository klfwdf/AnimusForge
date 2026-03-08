using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001FD RID: 509
	public abstract class BodyPropertiesModel : MBGameModel<BodyPropertiesModel>
	{
		// Token: 0x06001F4B RID: 8011
		public abstract int[] GetHairIndicesForCulture(int race, int gender, float age, CultureObject culture);

		// Token: 0x06001F4C RID: 8012
		public abstract int[] GetBeardIndicesForCulture(int race, int gender, float age, CultureObject culture);

		// Token: 0x06001F4D RID: 8013
		public abstract int[] GetTattooIndicesForCulture(int race, int gender, float age, CultureObject culture);
	}
}
