namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class FirstMeetingTag : ConversationTag
{
	public const string Id = "FirstMeetingTag";

	public override string StringId => "FirstMeetingTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Campaign.Current.ConversationManager.CurrentConversationIsFirst;
	}
}
