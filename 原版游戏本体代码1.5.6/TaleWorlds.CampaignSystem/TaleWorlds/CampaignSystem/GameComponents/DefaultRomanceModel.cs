using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultRomanceModel : RomanceModel
{
	public override int GetAttractionValuePercentage(Hero potentiallyInterestedCharacter, Hero heroOfInterest)
	{
		return MathF.Abs((potentiallyInterestedCharacter.StaticBodyProperties.GetHashCode() + heroOfInterest.StaticBodyProperties.GetHashCode()) % 100);
	}
}
