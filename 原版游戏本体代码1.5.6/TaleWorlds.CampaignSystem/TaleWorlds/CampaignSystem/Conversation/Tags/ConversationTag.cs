namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public abstract class ConversationTag
{
	public abstract string StringId { get; }

	public abstract bool IsApplicableTo(CharacterObject character);

	public override string ToString()
	{
		return StringId;
	}
}
