using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsAtSeaTag : ConversationTag
{
	public const string Id = "PlayerIsAtSeaTag";

	public override string StringId => "PlayerIsAtSeaTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		MobileParty mobileParty = (Hero.MainHero.IsPrisoner ? Hero.MainHero.PartyBelongedToAsPrisoner.MobileParty : Hero.MainHero.PartyBelongedTo);
		MobileParty mobileParty2 = (character.HeroObject.IsPrisoner ? character.HeroObject.PartyBelongedToAsPrisoner.MobileParty : character.HeroObject.PartyBelongedTo);
		if (mobileParty.IsCurrentlyAtSea)
		{
			return mobileParty2 != mobileParty;
		}
		return false;
	}
}
