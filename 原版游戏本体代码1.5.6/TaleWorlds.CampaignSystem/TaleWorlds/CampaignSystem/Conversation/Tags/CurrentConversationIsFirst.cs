namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class CurrentConversationIsFirst : ConversationTag
{
	public const string Id = "CurrentConversationIsFirst";

	public override string StringId => "CurrentConversationIsFirst";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Campaign.Current.ConversationManager.CurrentConversationIsFirst;
	}
}
