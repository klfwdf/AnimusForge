using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class TradeAgreementModel : MBGameModel<TradeAgreementModel>
{
	public abstract int GetProfitPerCaravanVisit(MobileParty mobileParty);

	public abstract CampaignTime GetTradeAgreementDurationInYears(Kingdom iniatatingKingdom, Kingdom otherKingdom);

	public abstract int GetMaximumTradeAgreementCount(Kingdom kingdom);

	public abstract int GetInfluenceCostOfProposingTradeAgreement(Clan clan);

	public abstract float GetScoreOfStartingTradeAgreement(Kingdom kingdom, Kingdom targetKingdom, Clan clan, out TextObject explanation, bool includeExplanation = false);

	public abstract bool CanMakeTradeAgreement(Kingdom kingdom, Kingdom other, bool checkOtherSideTradeSupport, out TextObject reason, bool includeReason = false);
}
