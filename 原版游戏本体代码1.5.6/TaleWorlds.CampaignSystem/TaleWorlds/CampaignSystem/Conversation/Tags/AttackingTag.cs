using Helpers;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class AttackingTag : ConversationTag
{
	public const string Id = "AttackingTag";

	public override string StringId => "AttackingTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (HeroHelper.WillLordAttack())
		{
			return true;
		}
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.SiegeEvent != null)
		{
			return Settlement.CurrentSettlement.Parties.Contains(Hero.MainHero.PartyBelongedTo);
		}
		return false;
	}
}
