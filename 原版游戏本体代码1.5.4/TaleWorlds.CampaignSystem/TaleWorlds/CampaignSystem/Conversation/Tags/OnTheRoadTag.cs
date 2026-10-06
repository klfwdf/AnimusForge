using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class OnTheRoadTag : ConversationTag
{
	public const string Id = "OnTheRoadTag";

	public override string StringId => "OnTheRoadTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Settlement.CurrentSettlement == null;
	}
}
