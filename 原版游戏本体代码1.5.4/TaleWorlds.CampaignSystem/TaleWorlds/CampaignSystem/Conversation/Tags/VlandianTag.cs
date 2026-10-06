namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VlandianTag : ConversationTag
{
	public const string Id = "VlandianTag";

	public override string StringId => "VlandianTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "vlandia";
	}
}
