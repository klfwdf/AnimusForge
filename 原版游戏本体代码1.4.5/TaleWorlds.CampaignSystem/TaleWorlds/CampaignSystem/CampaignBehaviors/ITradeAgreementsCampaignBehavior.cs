namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface ITradeAgreementsCampaignBehavior
{
	void MakeTradeAgreement(Kingdom kingdom1, Kingdom kingdom2, CampaignTime duration);

	bool HasTradeAgreement(Kingdom kingdom, Kingdom other, out TradeAgreementsCampaignBehavior.TradeAgreement tradeAgreement);

	void EndTradeAgreement(Kingdom kingdom, Kingdom other);

	void OnTradeAgreementOfferedToPlayer(Kingdom fromKingdom);

	CampaignTime GetTradeAgreementEndDate(Kingdom kingdom, Kingdom other);

	void OnTradeGoldDistributedInKingdom(Kingdom kingdom1, Kingdom kingdom2, Clan clan, int share);
}
