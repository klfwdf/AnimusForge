using System;
using Helpers;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party
{
	// Token: 0x020002FF RID: 767
	public struct PartyScreenLogicInitializationData
	{
		// Token: 0x06002CAD RID: 11437 RVA: 0x000BBB30 File Offset: 0x000B9D30
		public static PartyScreenLogicInitializationData CreateBasicInitDataWithMainParty(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, PartyScreenLogic.TransferState memberTransferState, PartyScreenLogic.TransferState prisonerTransferState, PartyScreenLogic.TransferState accompanyingTransferState, IsTroopTransferableDelegate troopTransferableDelegate, PartyScreenHelper.PartyScreenMode partyScreenMode, PartyBase leftOwnerParty = null, TextObject leftPartyName = null, TextObject header = null, Hero leftLeaderHero = null, int leftPartyMembersSizeLimit = 0, int leftPartyPrisonersSizeLimit = 0, PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = null, PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = null, PartyPresentationCancelButtonDelegate partyPresentationCancelButtonDelegate = null, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null, PartyScreenClosedDelegate partyScreenClosedDelegate = null, bool isDismissMode = false, bool transferHealthiesGetWoundedsFirst = false, bool isTroopUpgradesDisabled = false, bool showProgressBar = false, int questModeWageDaysMultiplier = 0)
		{
			return new PartyScreenLogicInitializationData
			{
				LeftOwnerParty = leftOwnerParty,
				RightOwnerParty = PartyBase.MainParty,
				LeftMemberRoster = leftMemberRoster,
				LeftPrisonerRoster = leftPrisonerRoster,
				RightMemberRoster = PartyBase.MainParty.MemberRoster,
				RightPrisonerRoster = PartyBase.MainParty.PrisonRoster,
				LeftLeaderHero = leftLeaderHero,
				RightLeaderHero = PartyBase.MainParty.LeaderHero,
				LeftPartyMembersSizeLimit = leftPartyMembersSizeLimit,
				LeftPartyPrisonersSizeLimit = leftPartyPrisonersSizeLimit,
				RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit,
				RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit,
				LeftPartyName = leftPartyName,
				RightPartyName = PartyBase.MainParty.Name,
				TroopTransferableDelegate = troopTransferableDelegate,
				PartyScreenMode = partyScreenMode,
				PartyPresentationDoneButtonDelegate = partyPresentationDoneButtonDelegate,
				PartyPresentationDoneButtonConditionDelegate = partyPresentationDoneButtonConditionDelegate,
				PartyPresentationCancelButtonActivateDelegate = partyPresentationCancelButtonActivateDelegate,
				PartyPresentationCancelButtonDelegate = partyPresentationCancelButtonDelegate,
				IsDismissMode = isDismissMode,
				IsTroopUpgradesDisabled = isTroopUpgradesDisabled,
				Header = header,
				PartyScreenClosedDelegate = partyScreenClosedDelegate,
				TransferHealthiesGetWoundedsFirst = transferHealthiesGetWoundedsFirst,
				ShowProgressBar = showProgressBar,
				MemberTransferState = memberTransferState,
				PrisonerTransferState = prisonerTransferState,
				AccompanyingTransferState = accompanyingTransferState,
				QuestModeWageDaysMultiplier = questModeWageDaysMultiplier
			};
		}

		// Token: 0x06002CAE RID: 11438 RVA: 0x000BBC84 File Offset: 0x000B9E84
		public static PartyScreenLogicInitializationData CreateBasicInitDataWithMainPartyAndOther(MobileParty party, PartyScreenLogic.TransferState memberTransferState, PartyScreenLogic.TransferState prisonerTransferState, PartyScreenLogic.TransferState accompanyingTransferState, IsTroopTransferableDelegate troopTransferableDelegate, PartyScreenHelper.PartyScreenMode partyScreenMode, TextObject header = null, PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = null, PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = null, PartyPresentationCancelButtonDelegate partyPresentationCancelButtonDelegate = null, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null, PartyScreenClosedDelegate partyScreenClosedDelegate = null, bool isDismissMode = false, bool transferHealthiesGetWoundedsFirst = false, bool isTroopUpgradesDisabled = true, bool showProgressBar = false)
		{
			return new PartyScreenLogicInitializationData
			{
				LeftOwnerParty = party.Party,
				RightOwnerParty = PartyBase.MainParty,
				LeftMemberRoster = party.MemberRoster,
				LeftPrisonerRoster = party.PrisonRoster,
				RightMemberRoster = PartyBase.MainParty.MemberRoster,
				RightPrisonerRoster = PartyBase.MainParty.PrisonRoster,
				LeftLeaderHero = party.LeaderHero,
				RightLeaderHero = PartyBase.MainParty.LeaderHero,
				LeftPartyMembersSizeLimit = party.Party.PartySizeLimit,
				LeftPartyPrisonersSizeLimit = party.Party.PrisonerSizeLimit,
				RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit,
				RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit,
				LeftPartyName = party.Name,
				RightPartyName = PartyBase.MainParty.Name,
				TroopTransferableDelegate = troopTransferableDelegate,
				PartyScreenMode = partyScreenMode,
				PartyPresentationDoneButtonDelegate = partyPresentationDoneButtonDelegate,
				PartyPresentationDoneButtonConditionDelegate = partyPresentationDoneButtonConditionDelegate,
				PartyPresentationCancelButtonActivateDelegate = partyPresentationCancelButtonActivateDelegate,
				PartyPresentationCancelButtonDelegate = partyPresentationCancelButtonDelegate,
				IsDismissMode = isDismissMode,
				IsTroopUpgradesDisabled = isTroopUpgradesDisabled,
				Header = header,
				PartyScreenClosedDelegate = partyScreenClosedDelegate,
				TransferHealthiesGetWoundedsFirst = transferHealthiesGetWoundedsFirst,
				ShowProgressBar = showProgressBar,
				MemberTransferState = memberTransferState,
				PrisonerTransferState = prisonerTransferState,
				AccompanyingTransferState = accompanyingTransferState
			};
		}

		// Token: 0x04000CEC RID: 3308
		public TroopRoster LeftMemberRoster;

		// Token: 0x04000CED RID: 3309
		public TroopRoster LeftPrisonerRoster;

		// Token: 0x04000CEE RID: 3310
		public TroopRoster RightMemberRoster;

		// Token: 0x04000CEF RID: 3311
		public TroopRoster RightPrisonerRoster;

		// Token: 0x04000CF0 RID: 3312
		public PartyBase LeftOwnerParty;

		// Token: 0x04000CF1 RID: 3313
		public PartyBase RightOwnerParty;

		// Token: 0x04000CF2 RID: 3314
		public TextObject LeftPartyName;

		// Token: 0x04000CF3 RID: 3315
		public TextObject RightPartyName;

		// Token: 0x04000CF4 RID: 3316
		public TextObject Header;

		// Token: 0x04000CF5 RID: 3317
		public Hero LeftLeaderHero;

		// Token: 0x04000CF6 RID: 3318
		public Hero RightLeaderHero;

		// Token: 0x04000CF7 RID: 3319
		public int LeftPartyMembersSizeLimit;

		// Token: 0x04000CF8 RID: 3320
		public int LeftPartyPrisonersSizeLimit;

		// Token: 0x04000CF9 RID: 3321
		public int RightPartyMembersSizeLimit;

		// Token: 0x04000CFA RID: 3322
		public int RightPartyPrisonersSizeLimit;

		// Token: 0x04000CFB RID: 3323
		public PartyPresentationDoneButtonDelegate PartyPresentationDoneButtonDelegate;

		// Token: 0x04000CFC RID: 3324
		public PartyPresentationDoneButtonConditionDelegate PartyPresentationDoneButtonConditionDelegate;

		// Token: 0x04000CFD RID: 3325
		public PartyPresentationCancelButtonActivateDelegate PartyPresentationCancelButtonActivateDelegate;

		// Token: 0x04000CFE RID: 3326
		public IsTroopTransferableDelegate TroopTransferableDelegate;

		// Token: 0x04000CFF RID: 3327
		public CanTalkToHeroDelegate CanTalkToTroopDelegate;

		// Token: 0x04000D00 RID: 3328
		public PartyPresentationCancelButtonDelegate PartyPresentationCancelButtonDelegate;

		// Token: 0x04000D01 RID: 3329
		public PartyScreenClosedDelegate PartyScreenClosedDelegate;

		// Token: 0x04000D02 RID: 3330
		public bool DoNotApplyGoldTransactions;

		// Token: 0x04000D03 RID: 3331
		public bool IsDismissMode;

		// Token: 0x04000D04 RID: 3332
		public bool TransferHealthiesGetWoundedsFirst;

		// Token: 0x04000D05 RID: 3333
		public bool IsTroopUpgradesDisabled;

		// Token: 0x04000D06 RID: 3334
		public bool ShowProgressBar;

		// Token: 0x04000D07 RID: 3335
		public int QuestModeWageDaysMultiplier;

		// Token: 0x04000D08 RID: 3336
		public PartyScreenLogic.TransferState MemberTransferState;

		// Token: 0x04000D09 RID: 3337
		public PartyScreenLogic.TransferState PrisonerTransferState;

		// Token: 0x04000D0A RID: 3338
		public PartyScreenLogic.TransferState AccompanyingTransferState;

		// Token: 0x04000D0B RID: 3339
		public PartyScreenHelper.PartyScreenMode PartyScreenMode;
	}
}
