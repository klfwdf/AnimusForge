using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BodyPropertiesModel : MBGameModel<BodyPropertiesModel>
{
	public abstract int[] GetHairIndicesForCulture(int race, int gender, float age, CultureObject culture);

	public abstract int[] GetBeardIndicesForCulture(int race, int gender, float age, CultureObject culture);

	public abstract int[] GetTattooIndicesForCulture(int race, int gender, float age, CultureObject culture);
}
