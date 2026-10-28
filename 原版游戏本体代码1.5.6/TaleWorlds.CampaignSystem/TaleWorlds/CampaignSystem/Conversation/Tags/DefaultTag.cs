namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class DefaultTag : ConversationTag
{
	public const string Id = "DefaultTag";

	public override string StringId => "DefaultTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return true;
	}
}
