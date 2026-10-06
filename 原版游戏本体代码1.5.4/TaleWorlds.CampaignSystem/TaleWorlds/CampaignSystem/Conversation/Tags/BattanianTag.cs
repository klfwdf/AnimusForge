namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class BattanianTag : ConversationTag
{
	public const string Id = "BattanianTag";

	public override string StringId => "BattanianTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "battania";
	}
}
