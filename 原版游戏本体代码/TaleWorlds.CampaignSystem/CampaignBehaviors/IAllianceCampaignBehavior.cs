using System;
using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F8 RID: 1016
	public interface IAllianceCampaignBehavior
	{
		// Token: 0x06003F86 RID: 16262
		void OnAllianceOfferedToPlayerKingdom(Kingdom proposerKingdom);

		// Token: 0x06003F87 RID: 16263
		void OnAllianceOfferedToPlayer(Kingdom proposerKingdom);

		// Token: 0x06003F88 RID: 16264
		void OnCallToWarAgreementProposedToPlayerKingdom(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06003F89 RID: 16265
		void OnCallToWarAgreementProposedByPlayerKingdom(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06003F8A RID: 16266
		void OnCallToWarAgreementProposedToPlayer(Kingdom proposerKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06003F8B RID: 16267
		void OnCallToWarAgreementProposedByPlayer(Kingdom proposedKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06003F8C RID: 16268
		bool IsAllyWithKingdom(Kingdom kingdom1, Kingdom kingdom2);

		// Token: 0x06003F8D RID: 16269
		void StartAlliance(Kingdom proposerKingdom, Kingdom receiverKingdom);

		// Token: 0x06003F8E RID: 16270
		void EndAlliance(Kingdom kingdom1, Kingdom kingdom2);

		// Token: 0x06003F8F RID: 16271
		bool HasCalledToWar(Kingdom callingKingdom, Kingdom calledKingdom);

		// Token: 0x06003F90 RID: 16272
		bool IsAtWarByCallToWarAgreement(Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06003F91 RID: 16273
		void StartCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, int callToWarCost, bool isPlayerPaying = false);

		// Token: 0x06003F92 RID: 16274
		void EndCallToWarAgreement(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst);

		// Token: 0x06003F93 RID: 16275
		List<Kingdom> GetKingdomsToCallToWarAgainst(Kingdom callingKingdom, Kingdom calledKingdom);

		// Token: 0x06003F94 RID: 16276
		CampaignTime GetAllianceEndDate(Kingdom kingdom1, Kingdom kingdom2);
	}
}
