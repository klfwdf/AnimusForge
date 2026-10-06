namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NpcIsFemaleTag : ConversationTag
{
	public const string Id = "NpcIsFemaleTag";

	public override string StringId => "NpcIsFemaleTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.IsFemale;
	}
}
