using Helpers;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party;

public struct PartyScreenLogicInitializationData
{
	public TroopRoster LeftMemberRoster;

	public TroopRoster LeftPrisonerRoster;

	public TroopRoster RightMemberRoster;

	public TroopRoster RightPrisonerRoster;

	public PartyBase LeftOwnerParty;

	public PartyBase RightOwnerParty;

	public TextObject LeftPartyName;

	public TextObject RightPartyName;

	public TextObject Header;

	public Hero LeftLeaderHero;

	public Hero RightLeaderHero;

	public int LeftPartyMembersSizeLimit;

	public int LeftPartyPrisonersSizeLimit;

	public int RightPartyMembersSizeLimit;

	public int RightPartyPrisonersSizeLimit;

	public PartyPresentationDoneButtonDelegate PartyPresentationDoneButtonDelegate;

	public PartyPresentationDoneButtonConditionDelegate PartyPresentationDoneButtonConditionDelegate;

	public PartyPresentationCancelButtonActivateDelegate PartyPresentationCancelButtonActivateDelegate;

	public IsTroopTransferableDelegate TroopTransferableDelegate;

	public CanTalkToHeroDelegate CanTalkToTroopDelegate;

	public PartyPresentationCancelButtonDelegate PartyPresentationCancelButtonDelegate;

	public PartyScreenClosedDelegate PartyScreenClosedDelegate;

	public bool DoNotApplyGoldTransactions;

	public bool IsDismissMode;

	public bool TransferHealthiesGetWoundedsFirst;

	public bool IsTroopUpgradesDisabled;

	public bool ShowProgressBar;

	public int QuestModeWageDaysMultiplier;

	public PartyScreenLogic.TransferState MemberTransferState;

	public PartyScreenLogic.TransferState PrisonerTransferState;

	public PartyScreenLogic.TransferState AccompanyingTransferState;

	public PartyScreenHelper.PartyScreenMode PartyScreenMode;

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
}
