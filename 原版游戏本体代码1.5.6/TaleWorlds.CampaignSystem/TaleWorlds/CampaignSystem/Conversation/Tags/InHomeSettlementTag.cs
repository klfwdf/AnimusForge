using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class InHomeSettlementTag : ConversationTag
{
	public const string Id = "InHomeSettlementTag";

	public override string StringId => "InHomeSettlementTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && Settlement.CurrentSettlement != null && character.HeroObject.HomeSettlement == Settlement.CurrentSettlement)
		{
			return true;
		}
		if (character.IsHero && Settlement.CurrentSettlement != null)
		{
			return Settlement.CurrentSettlement.OwnerClan.Leader == character.HeroObject;
		}
		return false;
	}
}
