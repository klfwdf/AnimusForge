namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsAffiliatedTag : ConversationTag
{
	public const string Id = "PlayerIsAffiliatedTag";

	public override string StringId => "PlayerIsAffiliatedTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Hero.MainHero.MapFaction.IsKingdomFaction;
	}
}
