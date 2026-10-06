namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class KhuzaitTag : ConversationTag
{
	public const string Id = "KhuzaitTag";

	public override string StringId => "KhuzaitTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "khuzait";
	}
}
