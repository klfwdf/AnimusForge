using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001C5 RID: 453
	public abstract class CrimeModel : MBGameModel<CrimeModel>
	{
		// Token: 0x17000753 RID: 1875
		// (get) Token: 0x06001DB0 RID: 7600
		public abstract float DeclareWarCrimeRatingThreshold { get; }

		// Token: 0x06001DB1 RID: 7601
		public abstract float GetMaxCrimeRating();

		// Token: 0x06001DB2 RID: 7602
		public abstract float GetMinAcceptableCrimeRating(IFaction faction);

		// Token: 0x06001DB3 RID: 7603
		public abstract float GetCrimeRatingAfterPunishment();

		// Token: 0x06001DB4 RID: 7604
		public abstract bool DoesPlayerHaveAnyCrimeRating(IFaction faction);

		// Token: 0x06001DB5 RID: 7605
		public abstract bool IsPlayerCrimeRatingSevere(IFaction faction);

		// Token: 0x06001DB6 RID: 7606
		public abstract bool IsPlayerCrimeRatingModerate(IFaction faction);

		// Token: 0x06001DB7 RID: 7607
		public abstract bool IsPlayerCrimeRatingMild(IFaction faction);

		// Token: 0x06001DB8 RID: 7608
		public abstract float GetCost(IFaction faction, CrimeModel.PaymentMethod paymentMethod, float minimumCrimeRating);

		// Token: 0x06001DB9 RID: 7609
		public abstract ExplainedNumber GetDailyCrimeRatingChange(IFaction faction, bool includeDescriptions = false);

		// Token: 0x020005F6 RID: 1526
		[Flags]
		public enum PaymentMethod : uint
		{
			// Token: 0x040018A8 RID: 6312
			ExMachina = 4096U,
			// Token: 0x040018A9 RID: 6313
			Gold = 1U,
			// Token: 0x040018AA RID: 6314
			Influence = 2U,
			// Token: 0x040018AB RID: 6315
			Punishment = 4U,
			// Token: 0x040018AC RID: 6316
			Execution = 8U
		}
	}
}
