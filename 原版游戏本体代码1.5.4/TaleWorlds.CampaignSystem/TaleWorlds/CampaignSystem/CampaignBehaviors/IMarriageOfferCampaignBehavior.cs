using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IMarriageOfferCampaignBehavior : ICampaignBehavior
{
	void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden);

	void OnMarriageOfferCanceled(Hero suitor, Hero maiden);

	MBBindingList<TextObject> GetMarriageAcceptedConsequences();

	void OnMarriageOfferAcceptedOnPopUp();

	void OnMarriageOfferDeclinedOnPopUp();

	bool IsHeroEngaged(Hero hero);
}
