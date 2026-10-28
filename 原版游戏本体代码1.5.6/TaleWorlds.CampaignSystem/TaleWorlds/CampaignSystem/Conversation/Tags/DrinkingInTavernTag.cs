using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class DrinkingInTavernTag : ConversationTag
{
	public const string Id = "DrinkingInTavernTag";

	public override string StringId => "DrinkingInTavernTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (LocationComplex.Current != null && character.IsHero)
		{
			Location locationOfCharacter = LocationComplex.Current.GetLocationOfCharacter(character.HeroObject);
			Location locationWithId = LocationComplex.Current.GetLocationWithId("tavern");
			if (character.HeroObject.IsWanderer && Settlement.CurrentSettlement != null && locationWithId == locationOfCharacter)
			{
				return true;
			}
		}
		else if (character.HeroObject == null && LocationComplex.Current != null && Settlement.CurrentSettlement != null && LocationComplex.Current.GetLocationWithId("tavern") == CampaignMission.Current.Location)
		{
			return true;
		}
		return false;
	}
}
