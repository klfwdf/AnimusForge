namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NordTag : ConversationTag
{
	public const string Id = "NordTag";

	public override string StringId => "NordTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "nord";
	}
}
