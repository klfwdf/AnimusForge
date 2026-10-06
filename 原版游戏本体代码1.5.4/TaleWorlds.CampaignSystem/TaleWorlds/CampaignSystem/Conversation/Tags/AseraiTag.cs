namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class AseraiTag : ConversationTag
{
	public const string Id = "AseraiTag";

	public override string StringId => "AseraiTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "aserai";
	}
}
