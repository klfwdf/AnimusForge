using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F6 RID: 246
	public class DefaultBodyPropertiesModel : BodyPropertiesModel
	{
		// Token: 0x06001663 RID: 5731 RVA: 0x000669B9 File Offset: 0x00064BB9
		public override int[] GetHairIndicesForCulture(int race, int gender, float age, CultureObject culture)
		{
			return FaceGen.GetHairIndicesByTag(race, gender, age, culture.StringId);
		}

		// Token: 0x06001664 RID: 5732 RVA: 0x000669CA File Offset: 0x00064BCA
		public override int[] GetBeardIndicesForCulture(int race, int gender, float age, CultureObject culture)
		{
			return FaceGen.GetFacialIndicesByTag(race, gender, age, culture.StringId);
		}

		// Token: 0x06001665 RID: 5733 RVA: 0x000669DB File Offset: 0x00064BDB
		public override int[] GetTattooIndicesForCulture(int race, int gender, float age, CultureObject culture)
		{
			return FaceGen.GetTattooIndicesByTag(race, gender, age, culture.StringId);
		}
	}
}
