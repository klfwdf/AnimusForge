using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalTradeAgreementModel : TradeAgreementModel
{
	public override bool CanMakeTradeAgreement(Kingdom kingdom, Kingdom other, bool checkOtherSideTradeSupport, out TextObject reason, bool includeReason = false)
	{
		return base.BaseModel.CanMakeTradeAgreement(kingdom, other, checkOtherSideTradeSupport, out reason, includeReason);
	}

	public override int GetInfluenceCostOfProposingTradeAgreement(Clan clan)
	{
		return base.BaseModel.GetInfluenceCostOfProposingTradeAgreement(clan);
	}

	public override int GetMaximumTradeAgreementCount(Kingdom kingdom)
	{
		return base.BaseModel.GetMaximumTradeAgreementCount(kingdom);
	}

	public override int GetProfitPerCaravanVisit(MobileParty mobileParty)
	{
		if (mobileParty.HasNavalNavigationCapability)
		{
			return 1000;
		}
		return base.BaseModel.GetProfitPerCaravanVisit(mobileParty);
	}

	public override float GetScoreOfStartingTradeAgreement(Kingdom kingdom, Kingdom targetKingdom, Clan clan, out TextObject explanation, bool includeExplanation = false)
	{
		return base.BaseModel.GetScoreOfStartingTradeAgreement(kingdom, targetKingdom, clan, out explanation, includeExplanation);
	}

	public override CampaignTime GetTradeAgreementDurationInYears(Kingdom iniatatingKingdom, Kingdom otherKingdom)
	{
		return base.BaseModel.GetTradeAgreementDurationInYears(iniatatingKingdom, otherKingdom);
	}
}
