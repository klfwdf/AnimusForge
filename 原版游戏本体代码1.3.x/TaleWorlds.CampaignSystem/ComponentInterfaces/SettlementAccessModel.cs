using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000184 RID: 388
	public abstract class SettlementAccessModel : MBGameModel<SettlementAccessModel>
	{
		// Token: 0x06001B9D RID: 7069
		public abstract void CanMainHeroEnterSettlement(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails);

		// Token: 0x06001B9E RID: 7070
		public abstract void CanMainHeroEnterLordsHall(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails);

		// Token: 0x06001B9F RID: 7071
		public abstract void CanMainHeroEnterDungeon(Settlement settlement, out SettlementAccessModel.AccessDetails accessDetails);

		// Token: 0x06001BA0 RID: 7072
		public abstract bool CanMainHeroAccessLocation(Settlement settlement, string locationId, out bool disableOption, out TextObject disabledText);

		// Token: 0x06001BA1 RID: 7073
		public abstract bool CanMainHeroDoSettlementAction(Settlement settlement, SettlementAccessModel.SettlementAction settlementAction, out bool disableOption, out TextObject disabledText);

		// Token: 0x06001BA2 RID: 7074
		public abstract bool IsRequestMeetingOptionAvailable(Settlement settlement, out bool disableOption, out TextObject disabledText);

		// Token: 0x020005E9 RID: 1513
		public enum AccessLevel
		{
			// Token: 0x0400186B RID: 6251
			NoAccess,
			// Token: 0x0400186C RID: 6252
			LimitedAccess,
			// Token: 0x0400186D RID: 6253
			FullAccess
		}

		// Token: 0x020005EA RID: 1514
		public enum AccessMethod
		{
			// Token: 0x0400186F RID: 6255
			None,
			// Token: 0x04001870 RID: 6256
			Direct,
			// Token: 0x04001871 RID: 6257
			ByRequest
		}

		// Token: 0x020005EB RID: 1515
		public enum AccessLimitationReason
		{
			// Token: 0x04001873 RID: 6259
			None,
			// Token: 0x04001874 RID: 6260
			HostileFaction,
			// Token: 0x04001875 RID: 6261
			RelationshipWithOwner,
			// Token: 0x04001876 RID: 6262
			CrimeRating,
			// Token: 0x04001877 RID: 6263
			VillageIsLooted,
			// Token: 0x04001878 RID: 6264
			Disguised,
			// Token: 0x04001879 RID: 6265
			ClanTier,
			// Token: 0x0400187A RID: 6266
			LocationEmpty
		}

		// Token: 0x020005EC RID: 1516
		public enum LimitedAccessSolution
		{
			// Token: 0x0400187C RID: 6268
			None,
			// Token: 0x0400187D RID: 6269
			Bribe,
			// Token: 0x0400187E RID: 6270
			Disguise
		}

		// Token: 0x020005ED RID: 1517
		public enum PreliminaryActionObligation
		{
			// Token: 0x04001880 RID: 6272
			None,
			// Token: 0x04001881 RID: 6273
			Optional
		}

		// Token: 0x020005EE RID: 1518
		public enum PreliminaryActionType
		{
			// Token: 0x04001883 RID: 6275
			None,
			// Token: 0x04001884 RID: 6276
			FaceCharges
		}

		// Token: 0x020005EF RID: 1519
		public enum SettlementAction
		{
			// Token: 0x04001886 RID: 6278
			RecruitTroops,
			// Token: 0x04001887 RID: 6279
			Craft,
			// Token: 0x04001888 RID: 6280
			WalkAroundTheArena,
			// Token: 0x04001889 RID: 6281
			JoinTournament,
			// Token: 0x0400188A RID: 6282
			WatchTournament,
			// Token: 0x0400188B RID: 6283
			Trade,
			// Token: 0x0400188C RID: 6284
			WaitInSettlement,
			// Token: 0x0400188D RID: 6285
			ManageTown
		}

		// Token: 0x020005F0 RID: 1520
		public struct AccessDetails
		{
			// Token: 0x0400188E RID: 6286
			public SettlementAccessModel.AccessLevel AccessLevel;

			// Token: 0x0400188F RID: 6287
			public SettlementAccessModel.AccessMethod AccessMethod;

			// Token: 0x04001890 RID: 6288
			public SettlementAccessModel.AccessLimitationReason AccessLimitationReason;

			// Token: 0x04001891 RID: 6289
			public SettlementAccessModel.LimitedAccessSolution LimitedAccessSolution;

			// Token: 0x04001892 RID: 6290
			public SettlementAccessModel.PreliminaryActionObligation PreliminaryActionObligation;

			// Token: 0x04001893 RID: 6291
			public SettlementAccessModel.PreliminaryActionType PreliminaryActionType;
		}
	}
}
