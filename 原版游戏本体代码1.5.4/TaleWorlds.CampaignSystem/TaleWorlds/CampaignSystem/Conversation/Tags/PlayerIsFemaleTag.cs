namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsFemaleTag : ConversationTag
{
	public const string Id = "PlayerIsFemaleTag";

	public override string StringId => "PlayerIsFemaleTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Hero.MainHero.IsFemale;
	}
}
