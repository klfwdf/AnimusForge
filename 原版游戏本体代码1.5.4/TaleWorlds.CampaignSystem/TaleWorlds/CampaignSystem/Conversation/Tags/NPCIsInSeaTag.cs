namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NPCIsInSeaTag : ConversationTag
{
	public const string Id = "NPCIsInSeaTag";

	public override string StringId => "NPCIsInSeaTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		bool result = false;
		if (character.IsHero)
		{
			result = (character.HeroObject.IsPrisoner ? character.HeroObject.PartyBelongedToAsPrisoner.MobileParty : character.HeroObject.PartyBelongedTo).IsCurrentlyAtSea;
		}
		return result;
	}
}
