namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class SturgianTag : ConversationTag
{
	public const string Id = "SturgianTag";

	public override string StringId => "SturgianTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "sturgia";
	}
}
