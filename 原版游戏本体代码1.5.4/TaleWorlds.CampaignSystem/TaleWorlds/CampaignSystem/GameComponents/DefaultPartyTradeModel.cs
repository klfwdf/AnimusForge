using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyTradeModel : PartyTradeModel
{
	public override int CaravanTransactionHighestValueItemCount => 3;

	public override float GetTradePenaltyFactor(MobileParty party)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TradePenaltyReduction, party, ref explainedNumber);
		return 1f / explainedNumber.ResultNumber;
	}
}
