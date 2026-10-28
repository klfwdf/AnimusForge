namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class MetBeforeTag : ConversationTag
{
	public const string Id = "MetBeforeTag";

	public override string StringId => "MetBeforeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return !Campaign.Current.ConversationManager.CurrentConversationIsFirst;
	}
}
