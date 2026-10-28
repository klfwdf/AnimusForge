using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBodyPropertiesModel : BodyPropertiesModel
{
	public override int[] GetHairIndicesForCulture(int race, int gender, float age, CultureObject culture)
	{
		return FaceGen.GetHairIndicesByTag(race, gender, age, culture.StringId);
	}

	public override int[] GetBeardIndicesForCulture(int race, int gender, float age, CultureObject culture)
	{
		return FaceGen.GetFacialIndicesByTag(race, gender, age, culture.StringId);
	}

	public override int[] GetTattooIndicesForCulture(int race, int gender, float age, CultureObject culture)
	{
		return FaceGen.GetTattooIndicesByTag(race, gender, age, culture.StringId);
	}
}
