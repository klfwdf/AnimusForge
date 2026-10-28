namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NpcIsMaleTag : ConversationTag
{
	public const string Id = "NpcIsMaleTag";

	public override string StringId => "NpcIsMaleTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return !character.IsFemale;
	}
}
