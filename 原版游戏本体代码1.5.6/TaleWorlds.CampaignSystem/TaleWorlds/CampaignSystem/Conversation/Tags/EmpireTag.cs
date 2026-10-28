namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class EmpireTag : ConversationTag
{
	public const string Id = "EmpireTag";

	public override string StringId => "EmpireTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.Culture.StringId == "empire";
	}
}
