namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsMaleTag : ConversationTag
{
	public const string Id = "PlayerIsMaleTag";

	public override string StringId => "PlayerIsMaleTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return !Hero.MainHero.IsFemale;
	}
}
