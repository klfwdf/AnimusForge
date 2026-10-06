using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IAllianceCampaignBehavior
{
	void OnAllianceOfferedToPlayerKingdom(Kingdom proposerKingdom);

	void OnAllianceOfferedToPlayer(Kingdom proposerKingdom);

	void OnCallToWarAgreementProposedToPlayerKingdom(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst);

	void OnCallToWarAgreementProposedByPlayerKingdom(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst);

	void OnCallToWarAgreementProposedToPlayer(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst);

	void OnCallToWarAgreementProposedByPlayer(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst);

	bool IsAllyWithKingdom(Kingdom kingdom1, Kingdom kingdom2);

	void StartAlliance(Kingdom proposerKingdom, Kingdom receiverKingdom);

	void EndAlliance(Kingdom kingdom1, Kingdom kingdom2);

	bool HasCalledToWar(Kingdom callingKingdom, Kingdom calledKingdom);

	bool IsAtWarByCallToWarAgreement(Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, out Kingdom callingKingdom);

	void StartCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, int callToWarCost, bool isPlayerPaying = false);

	void EndCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst);

	List<Kingdom> GetKingdomsToCallToWarAgainst(Kingdom callingKingdom, Kingdom calledKingdom);

	CampaignTime GetAllianceEndDate(Kingdom kingdom1, Kingdom kingdom2);

	void DenyCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom);
}
