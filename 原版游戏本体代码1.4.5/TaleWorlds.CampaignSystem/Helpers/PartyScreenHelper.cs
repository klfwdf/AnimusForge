using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class PartyScreenHelper
{
	public enum PartyScreenMode
	{
		Normal,
		Shared,
		Loot,
		Ransom,
		PrisonerManage,
		TroopsManage,
		QuestTroopManage
	}

	private static readonly int _countToAddForEachTroopCheatMode = 10;

	public static PartyState GetActivePartyState()
	{
		if (GameStateManager.Current?.ActiveState is PartyState result)
		{
			return result;
		}
		Debug.FailedAssert("GetActivePartyState requested but the active state is not PartyState!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetActivePartyState", 7553);
		return null;
	}

	private static void OpenPartyScreen(bool isDonating = false)
	{
		Game current = Game.Current;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = new PartyScreenLogicInitializationData
		{
			LeftOwnerParty = null,
			RightOwnerParty = PartyBase.MainParty,
			LeftMemberRoster = TroopRoster.CreateDummyTroopRoster(),
			LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
			RightMemberRoster = PartyBase.MainParty.MemberRoster,
			RightPrisonerRoster = PartyBase.MainParty.PrisonRoster,
			LeftLeaderHero = null,
			RightLeaderHero = PartyBase.MainParty.LeaderHero,
			LeftPartyMembersSizeLimit = 0,
			LeftPartyPrisonersSizeLimit = 0,
			RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit,
			RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit,
			LeftPartyName = null,
			RightPartyName = PartyBase.MainParty.Name,
			TroopTransferableDelegate = TroopTransferableDelegate,
			PartyPresentationDoneButtonDelegate = DefaultDoneHandler,
			PartyPresentationDoneButtonConditionDelegate = null,
			PartyPresentationCancelButtonActivateDelegate = null,
			PartyPresentationCancelButtonDelegate = null,
			IsDismissMode = true,
			IsTroopUpgradesDisabled = false,
			Header = null,
			PartyScreenClosedDelegate = null,
			TransferHealthiesGetWoundedsFirst = false,
			ShowProgressBar = false,
			MemberTransferState = PartyScreenLogic.TransferState.Transferable,
			PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
			AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable
		};
		partyScreenLogic.Initialize(initializationData);
		PartyState partyState = current.GameStateManager.CreateState<PartyState>();
		partyState.PartyScreenLogic = partyScreenLogic;
		partyState.IsDonating = isDonating;
		partyState.PartyScreenMode = PartyScreenMode.Normal;
		current.GameStateManager.PushState(partyState);
	}

	public static void CloseScreen(bool isForced, bool fromCancel = false)
	{
		ClosePartyPresentation(isForced, fromCancel);
	}

	private static void ClosePartyPresentation(bool isForced, bool fromCancel)
	{
		PartyState activePartyState = GetActivePartyState();
		PartyScreenLogic partyScreenLogic = activePartyState?.PartyScreenLogic;
		if (partyScreenLogic == null)
		{
			Debug.FailedAssert("Trying to close party screen when it's already closed!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "ClosePartyPresentation", 7620);
			return;
		}
		bool flag = true;
		if (!fromCancel)
		{
			flag = partyScreenLogic?.DoneLogic(isForced) ?? false;
		}
		if (flag)
		{
			partyScreenLogic?.OnPartyScreenClosed(fromCancel);
			if (activePartyState != null)
			{
				activePartyState.PartyScreenLogic = null;
			}
			Game.Current.GameStateManager.PopState();
		}
	}

	public static void OpenScreenAsCheat()
	{
		if (!Game.Current.CheatMode)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=!}Cheat mode is not enabled!"));
			return;
		}
		Game current = Game.Current;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = new PartyScreenLogicInitializationData
		{
			LeftOwnerParty = null,
			RightOwnerParty = PartyBase.MainParty,
			LeftMemberRoster = GetRosterWithAllGameTroops(),
			LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
			RightMemberRoster = PartyBase.MainParty.MemberRoster,
			RightPrisonerRoster = PartyBase.MainParty.PrisonRoster,
			LeftLeaderHero = null,
			RightLeaderHero = PartyBase.MainParty.LeaderHero,
			LeftPartyMembersSizeLimit = 0,
			LeftPartyPrisonersSizeLimit = 0,
			RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit,
			RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit,
			LeftPartyName = null,
			RightPartyName = PartyBase.MainParty.Name,
			TroopTransferableDelegate = TroopTransferableDelegate,
			PartyPresentationDoneButtonDelegate = DefaultDoneHandler,
			PartyPresentationDoneButtonConditionDelegate = null,
			PartyPresentationCancelButtonActivateDelegate = null,
			PartyPresentationCancelButtonDelegate = null,
			IsDismissMode = true,
			IsTroopUpgradesDisabled = false,
			Header = null,
			PartyScreenClosedDelegate = null,
			TransferHealthiesGetWoundedsFirst = false,
			ShowProgressBar = false,
			MemberTransferState = PartyScreenLogic.TransferState.Transferable,
			PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
			AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable
		};
		partyScreenLogic.Initialize(initializationData);
		PartyState partyState = current.GameStateManager.CreateState<PartyState>();
		partyState.PartyScreenLogic = partyScreenLogic;
		partyState.IsDonating = false;
		current.GameStateManager.PushState(partyState);
	}

	private static TroopRoster GetRosterWithAllGameTroops()
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		List<CharacterObject> list = new List<CharacterObject>();
		EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject));
		for (int i = 0; i < CharacterObject.All.Count; i++)
		{
			CharacterObject characterObject = CharacterObject.All[i];
			if (pageOf.IsValidEncyclopediaItem(characterObject))
			{
				list.Add(characterObject);
			}
		}
		list.Sort((CharacterObject a, CharacterObject b) => a.Name.ToString().CompareTo(b.Name.ToString()));
		for (int num = 0; num < list.Count; num++)
		{
			CharacterObject character = list[num];
			troopRoster.AddToCounts(character, _countToAddForEachTroopCheatMode);
		}
		return troopRoster;
	}

	public static void OpenScreenAsNormal()
	{
		if (Game.Current.CheatMode)
		{
			OpenScreenAsCheat();
		}
		else
		{
			OpenPartyScreen();
		}
	}

	public static void OpenScreenAsRansom()
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.Ransom;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.TransferableWithTrade, PartyScreenLogic.TransferState.NotTransferable, TroopTransferableDelegate, partyState.PartyScreenMode, null, partyPresentationDoneButtonDelegate: SellPrisonersDoneHandler, header: new TextObject("{=SvahUNo6}Ransom Prisoners"), leftPartyName: GameTexts.FindText("str_ransom_broker"));
		initializationData.RightMemberRoster = MobileParty.MainParty.MemberRoster.CloneRosterData();
		initializationData.RightPrisonerRoster = MobileParty.MainParty.PrisonRoster.CloneRosterData();
		initializationData.DoNotApplyGoldTransactions = true;
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsLoot(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TextObject leftPartyName, int leftPartySizeLimit, PartyScreenClosedDelegate partyScreenClosedDelegate = null)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.Loot;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, TroopTransferableDelegate, partyState.PartyScreenMode, null, partyScreenClosedDelegate: partyScreenClosedDelegate, leftPartyName: leftPartyName, leftPartyMembersSizeLimit: leftPartySizeLimit, partyPresentationDoneButtonDelegate: DefaultDoneHandler, header: new TextObject("{=EOQcQa5l}Aftermath"));
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsManageTroopsAndPrisoners(MobileParty leftParty, PartyScreenClosedDelegate onPartyScreenClosed = null)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.Normal;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		IsTroopTransferableDelegate troopTransferableDelegate = ClanManageTroopAndPrisonerTransferableDelegate;
		PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
		PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = ManageTroopsAndPrisonersDoneHandler;
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(leftParty, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, troopTransferableDelegate, partyScreenMode, new TextObject("{=uQgNPJnc}Manage Troops"), partyPresentationDoneButtonDelegate, null, null, null, onPartyScreenClosed);
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsManagePlayerClanPartyClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		if (leftOwnerParty.MemberRoster.TotalManCount > 0)
		{
			return;
		}
		if (leftOwnerParty.Ships.Count > 0)
		{
			for (int num = leftOwnerParty.Ships.Count - 1; num >= 0; num--)
			{
				ChangeShipOwnerAction.ApplyByTransferring(rightOwnerParty, leftOwnerParty.Ships[num]);
			}
		}
		DestroyPartyAction.Apply(null, leftOwnerParty.MobileParty);
	}

	public static void OpenScreenAsReceiveTroops(TroopRoster leftMemberParty, TextObject leftPartyName, PartyScreenClosedDelegate partyScreenClosedDelegate = null)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.TroopsManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberParty, TroopRoster.CreateDummyTroopRoster(), PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, TroopTransferableDelegate, partyState.PartyScreenMode, null, partyScreenClosedDelegate: partyScreenClosedDelegate, leftPartyName: leftPartyName, leftPartyMembersSizeLimit: leftMemberParty.TotalManCount, partyPresentationDoneButtonDelegate: DefaultDoneHandler, header: new TextObject("{=uQgNPJnc}Manage Troops"));
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsManageTroops(MobileParty leftParty)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.TroopsManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(leftParty, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, ClanManageTroopTransferableDelegate, partyState.PartyScreenMode, new TextObject("{=uQgNPJnc}Manage Troops"), DefaultDoneHandler);
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsDonateTroops(MobileParty leftParty)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = leftParty.Owner.Clan != Clan.PlayerClan;
		partyState.PartyScreenMode = PartyScreenMode.TroopsManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(leftParty, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, DonateModeTroopTransferableDelegate, partyState.PartyScreenMode, partyPresentationDoneButtonConditionDelegate: DonateDonePossibleDelegate, partyPresentationDoneButtonDelegate: DefaultDoneHandler, header: new TextObject("{=4YfjgtO2}Donate Troops"));
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsDonateGarrisonWithCurrentSettlement()
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = true;
		partyState.PartyScreenMode = PartyScreenMode.TroopsManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		if (Hero.MainHero.CurrentSettlement.Town.GarrisonParty == null)
		{
			Hero.MainHero.CurrentSettlement.AddGarrisonParty();
		}
		MobileParty garrisonParty = Hero.MainHero.CurrentSettlement.Town.GarrisonParty;
		int num = Math.Max(garrisonParty.Party.PartySizeLimit - garrisonParty.Party.NumberOfAllMembers, 0);
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, TroopTransferableDelegate, partyState.PartyScreenMode, null, garrisonParty.Name, leftPartyMembersSizeLimit: num, partyPresentationDoneButtonDelegate: DonateGarrisonDoneHandler, header: new TextObject("{=uQgNPJnc}Manage Troops"));
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsDonatePrisoners()
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = true;
		partyState.PartyScreenMode = PartyScreenMode.PrisonerManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		if (Hero.MainHero.CurrentSettlement.Town.GarrisonParty == null)
		{
			Hero.MainHero.CurrentSettlement.AddGarrisonParty();
		}
		TroopRoster prisonRoster = Hero.MainHero.CurrentSettlement.Party.PrisonRoster;
		int num = Math.Max(Hero.MainHero.CurrentSettlement.Party.PrisonerSizeLimit - prisonRoster.Count, 0);
		TextObject textObject = new TextObject("{=SDzIAtiA}Prisoners of {SETTLEMENT_NAME}");
		textObject.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.Name);
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(TroopRoster.CreateDummyTroopRoster(), prisonRoster, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, DonatePrisonerTransferableDelegate, partyState.PartyScreenMode, null, textObject, leftPartyPrisonersSizeLimit: num, partyPresentationDoneButtonDelegate: DonatePrisonersDoneHandler, partyPresentationDoneButtonConditionDelegate: DonateDonePossibleDelegate, header: new TextObject("{=Z212GSiV}Leave Prisoners"));
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	private static Tuple<bool, TextObject> DonateDonePossibleDelegate(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		PartyScreenLogic partyScreenLogic = GetActivePartyState()?.PartyScreenLogic;
		if (partyScreenLogic.IsThereAnyChanges())
		{
			if (partyScreenLogic.CurrentData.TransferredPrisonersHistory.Any((Tuple<CharacterObject, int> p) => p.Item2 > 0))
			{
				return new Tuple<bool, TextObject>(item1: false, new TextObject("{=hI7eDbXs}You cannot take prisoners."));
			}
			if (partyScreenLogic.HaveRightSideGainedTroops())
			{
				return new Tuple<bool, TextObject>(item1: false, new TextObject("{=pvkl6pZh}You cannot take troops."));
			}
			if ((partyScreenLogic.MemberTransferState != PartyScreenLogic.TransferState.NotTransferable || partyScreenLogic.AccompanyingTransferState != PartyScreenLogic.TransferState.NotTransferable) && partyScreenLogic.LeftPartyMembersSizeLimit < partyScreenLogic.MemberRosters[0].TotalManCount)
			{
				return new Tuple<bool, TextObject>(item1: false, new TextObject("{=R7wiHjcL}Donated troops exceed party capacity."));
			}
			if (partyScreenLogic.PrisonerTransferState != PartyScreenLogic.TransferState.NotTransferable && partyScreenLogic.LeftPartyPrisonersSizeLimit < partyScreenLogic.PrisonerRosters[0].TotalManCount)
			{
				return new Tuple<bool, TextObject>(item1: false, new TextObject("{=3nfPGbN0}Donated prisoners exceed party capacity."));
			}
		}
		return new Tuple<bool, TextObject>(item1: true, TextObject.GetEmpty());
	}

	public static bool DonatePrisonerTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
	{
		if (side == PartyScreenLogic.PartyRosterSide.Right)
		{
			return type == PartyScreenLogic.TroopType.Prisoner;
		}
		return false;
	}

	public static void OpenScreenAsManagePrisoners()
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.PrisonerManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		TroopRoster prisonRoster = Hero.MainHero.CurrentSettlement.Party.PrisonRoster;
		TextObject textObject = new TextObject("{=SDzIAtiA}Prisoners of {SETTLEMENT_NAME}");
		textObject.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.Name);
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(TroopRoster.CreateDummyTroopRoster(), prisonRoster, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, TroopTransferableDelegate, partyState.PartyScreenMode, null, textObject, leftPartyPrisonersSizeLimit: Hero.MainHero.CurrentSettlement.Party.PrisonerSizeLimit, partyPresentationDoneButtonDelegate: ManageGarrisonDoneHandler, header: new TextObject("{=aadTnAEg}Manage Prisoners"));
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static bool TroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		Hero hero = leftOwnerParty?.LeaderHero;
		bool flag = (hero != null && hero.Clan == Clan.PlayerClan) || (leftOwnerParty != null && leftOwnerParty.IsMobile && leftOwnerParty.MobileParty.IsCaravan && leftOwnerParty.Owner == Hero.MainHero) || (leftOwnerParty != null && leftOwnerParty.IsMobile && leftOwnerParty.MobileParty.IsGarrison && leftOwnerParty.MobileParty.CurrentSettlement?.OwnerClan == Clan.PlayerClan);
		if (character.IsHero)
		{
			if (character.IsHero && character.HeroObject.Clan != Clan.PlayerClan)
			{
				if (character.HeroObject.IsPlayerCompanion)
				{
					return character.HeroObject.IsPlayerCompanion && flag;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public static bool ClanManageTroopAndPrisonerTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
	{
		if (character.IsHero)
		{
			return character.HeroObject.IsPrisoner;
		}
		return true;
	}

	public static bool ClanManageTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
	{
		return !character.IsHero;
	}

	public static bool DonateModeTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
	{
		if (!character.IsHero)
		{
			return side == PartyScreenLogic.PartyRosterSide.Right;
		}
		return false;
	}

	public static void OpenScreenWithCondition(IsTroopTransferableDelegate isTroopTransferable, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyPresentationDoneButtonDelegate onDoneClicked, PartyPresentationCancelButtonDelegate onCancelClicked, PartyScreenLogic.TransferState memberTransferState, PartyScreenLogic.TransferState prisonerTransferState, TextObject leftPartyName, int limit, bool showProgressBar, bool isDonating, PartyScreenMode screenMode = PartyScreenMode.Normal, TroopRoster memberRosterLeft = null, TroopRoster prisonerRosterLeft = null)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = isDonating;
		partyState.PartyScreenMode = screenMode;
		if (memberRosterLeft == null)
		{
			memberRosterLeft = TroopRoster.CreateDummyTroopRoster();
		}
		if (prisonerRosterLeft == null)
		{
			prisonerRosterLeft = TroopRoster.CreateDummyTroopRoster();
		}
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		TroopRoster leftMemberRoster = memberRosterLeft;
		TroopRoster leftPrisonerRoster = prisonerRosterLeft;
		PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
		bool showProgressBar2 = showProgressBar;
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, PartyScreenLogic.TransferState.NotTransferable, isTroopTransferable, partyScreenMode, null, leftPartyName, new TextObject("{=nZaeTlj8}Exchange Troops"), null, limit, 0, onDoneClicked, doneButtonCondition, onCancelClicked, null, null, isDismissMode: false, transferHealthiesGetWoundedsFirst: false, isTroopUpgradesDisabled: false, showProgressBar2);
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenForManagingAlley(bool isNewAlley, TroopRoster memberRosterLeft, IsTroopTransferableDelegate isTroopTransferable, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyPresentationDoneButtonDelegate onDoneClicked, TextObject leftPartyName, PartyPresentationCancelButtonDelegate onCancelButtonClicked)
	{
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.PartyScreenMode = PartyScreenMode.TroopsManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		TroopRoster troopRoster = (isNewAlley ? MobileParty.MainParty.MemberRoster.CloneRosterData() : MobileParty.MainParty.MemberRoster);
		TroopRosterElement troopRosterElement = memberRosterLeft.GetTroopRoster().Find((TroopRosterElement x) => x.Character.IsHero);
		if (troopRoster.Contains(troopRosterElement.Character))
		{
			troopRoster.RemoveTroop(troopRosterElement.Character);
		}
		PartyScreenLogicInitializationData initializationData = new PartyScreenLogicInitializationData
		{
			TroopTransferableDelegate = isTroopTransferable,
			PartyPresentationDoneButtonConditionDelegate = doneButtonCondition,
			PartyPresentationDoneButtonDelegate = onDoneClicked,
			LeftMemberRoster = memberRosterLeft,
			LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
			PartyPresentationCancelButtonDelegate = onCancelButtonClicked,
			RightMemberRoster = troopRoster,
			RightPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
			LeftPartyMembersSizeLimit = Campaign.Current.Models.AlleyModel.MaximumTroopCountInPlayerOwnedAlley + 1,
			RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit,
			RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit,
			MemberTransferState = PartyScreenLogic.TransferState.Transferable,
			PrisonerTransferState = PartyScreenLogic.TransferState.NotTransferable,
			AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable,
			PartyScreenMode = PartyScreenMode.TroopsManage,
			IsTroopUpgradesDisabled = true,
			ShowProgressBar = true,
			TransferHealthiesGetWoundedsFirst = false,
			IsDismissMode = false,
			QuestModeWageDaysMultiplier = 0,
			Header = null,
			RightOwnerParty = PartyBase.MainParty,
			RightPartyName = PartyBase.MainParty.Name
		};
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenAsQuest(TroopRoster leftMemberRoster, TextObject leftPartyName, int leftPartySizeLimit, int questDaysMultiplier, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null)
	{
		Debug.Print("PartyScreenManager::OpenScreenAsQuest");
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.QuestTroopManage;
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
		PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
		PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = ManageTroopsAndPrisonersDoneHandler;
		PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, isTroopTransferable, partyScreenMode, null, leftPartyName, new TextObject("{=nZaeTlj8}Exchange Troops"), null, leftPartySizeLimit, 0, partyPresentationDoneButtonDelegate, doneButtonCondition, null, partyPresentationCancelButtonActivateDelegate, onPartyScreenClosed, isDismissMode: false, transferHealthiesGetWoundedsFirst: true, isTroopUpgradesDisabled: false, showProgressBar: true, questDaysMultiplier);
		partyScreenLogic.Initialize(initializationData);
		partyState.PartyScreenLogic = partyScreenLogic;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenWithDummyRoster(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, TextObject leftPartyName, TextObject rightPartyName, int leftPartySizeLimit, int rightPartySizeLimit, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable, CanTalkToHeroDelegate canTalkToTroopDelegate = null, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null)
	{
		Debug.Print("PartyScreenManager::OpenScreenWithDummyRoster");
		PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
		PartyScreenLogicInitializationData initializationData = new PartyScreenLogicInitializationData
		{
			LeftOwnerParty = null,
			RightOwnerParty = MobileParty.MainParty.Party,
			LeftMemberRoster = leftMemberRoster,
			LeftPrisonerRoster = leftPrisonerRoster,
			RightMemberRoster = rightMemberRoster,
			RightPrisonerRoster = rightPrisonerRoster,
			LeftLeaderHero = null,
			RightLeaderHero = PartyBase.MainParty.LeaderHero,
			LeftPartyMembersSizeLimit = leftPartySizeLimit,
			LeftPartyPrisonersSizeLimit = 0,
			RightPartyMembersSizeLimit = rightPartySizeLimit,
			RightPartyPrisonersSizeLimit = 0,
			LeftPartyName = leftPartyName,
			RightPartyName = rightPartyName,
			TroopTransferableDelegate = isTroopTransferable,
			CanTalkToTroopDelegate = (canTalkToTroopDelegate ?? new CanTalkToHeroDelegate(CanTalkToHero)),
			PartyPresentationDoneButtonDelegate = ManageTroopsAndPrisonersDoneHandler,
			PartyPresentationDoneButtonConditionDelegate = doneButtonCondition,
			PartyPresentationCancelButtonActivateDelegate = partyPresentationCancelButtonActivateDelegate,
			PartyPresentationCancelButtonDelegate = null,
			PartyScreenClosedDelegate = onPartyScreenClosed,
			IsDismissMode = true,
			IsTroopUpgradesDisabled = true,
			Header = null,
			TransferHealthiesGetWoundedsFirst = true,
			ShowProgressBar = false,
			MemberTransferState = PartyScreenLogic.TransferState.Transferable,
			PrisonerTransferState = PartyScreenLogic.TransferState.NotTransferable,
			AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable
		};
		partyScreenLogic.Initialize(initializationData);
		PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
		partyState.PartyScreenLogic = partyScreenLogic;
		partyState.IsDonating = false;
		partyState.PartyScreenMode = PartyScreenMode.TroopsManage;
		Game.Current.GameStateManager.PushState(partyState);
	}

	public static void OpenScreenWithDummyRosterWithMainParty(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TextObject leftPartyName, int leftPartySizeLimit, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null)
	{
		Debug.Print("PartyScreenManager::OpenScreenWithDummyRosterWithMainParty");
		OpenScreenWithDummyRoster(leftMemberRoster, leftPrisonerRoster, MobileParty.MainParty.MemberRoster, MobileParty.MainParty.PrisonRoster, leftPartyName, MobileParty.MainParty.Name, leftPartySizeLimit, MobileParty.MainParty.Party.PartySizeLimit, doneButtonCondition, onPartyScreenClosed, isTroopTransferable, null, partyPresentationCancelButtonActivateDelegate);
	}

	public static void OpenScreenAsCreateClanPartyForHero(Hero hero, PartyScreenClosedDelegate onScreenClosed = null, IsTroopTransferableDelegate isTroopTransferable = null)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster troopRoster2 = MobileParty.MainParty.MemberRoster.CloneRosterData();
		TroopRoster rightPrisonerRoster = MobileParty.MainParty.PrisonRoster.CloneRosterData();
		troopRoster.AddToCounts(hero.CharacterObject, 1);
		if (troopRoster2.Contains(hero.CharacterObject))
		{
			troopRoster2.AddToCounts(hero.CharacterObject, -1);
		}
		CanTalkToHeroDelegate canTalkToTroopDelegate = delegate(Hero heroCharacter, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty, out TextObject cantTalkReason)
		{
			cantTalkReason = new TextObject("{=8muhth8y}You can't talk to your companions while creating a party.");
			return false;
		};
		TextObject textObject = GameTexts.FindText("str_lord_party_name");
		textObject.SetCharacterProperties("TROOP", hero.CharacterObject);
		OpenScreenWithDummyRoster(troopRoster, leftPrisonerRoster, troopRoster2, rightPrisonerRoster, textObject, MobileParty.MainParty.Name, Campaign.Current.Models.PartySizeLimitModel.GetAssumedPartySizeForLordParty(hero, hero.Clan.MapFaction, hero.Clan), MobileParty.MainParty.Party.PartySizeLimit, null, onScreenClosed ?? new PartyScreenClosedDelegate(OpenScreenAsCreateClanPartyForHeroPartyScreenClosed), isTroopTransferable ?? new IsTroopTransferableDelegate(OpenScreenAsCreateClanPartyForHeroTroopTransferableDelegate), canTalkToTroopDelegate);
	}

	private static void OpenScreenAsCreateClanPartyForHeroPartyScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		if (fromCancel)
		{
			return;
		}
		Hero hero = null;
		for (int i = 0; i < leftMemberRoster.data.Length; i++)
		{
			CharacterObject character = leftMemberRoster.data[i].Character;
			if (character != null && character.IsHero)
			{
				hero = leftMemberRoster.data[i].Character.HeroObject;
			}
		}
		int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
		if (hero.Gold < partyGoldLowerThreshold)
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, partyGoldLowerThreshold - hero.Gold);
		}
		MobileParty mobileParty = MobilePartyHelper.CreateNewClanMobileParty(hero, hero.Clan);
		foreach (TroopRosterElement item in leftMemberRoster.GetTroopRoster())
		{
			if (item.Character != hero.CharacterObject)
			{
				mobileParty.MemberRoster.Add(item);
				rightOwnerParty.MemberRoster.AddToCounts(item.Character, -item.Number, insertAtFront: false, -item.WoundedNumber, -item.Xp);
			}
		}
		foreach (TroopRosterElement item2 in leftPrisonRoster.GetTroopRoster())
		{
			mobileParty.PrisonRoster.Add(item2);
			rightOwnerParty.PrisonRoster.AddToCounts(item2.Character, -item2.Number, insertAtFront: false, -item2.WoundedNumber, -item2.Xp);
		}
	}

	private static bool OpenScreenAsCreateClanPartyForHeroTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
	{
		return !character.IsHero;
	}

	private static bool SellPrisonersDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		SellPrisonersAction.ApplyForSelectedPrisoners(MobileParty.MainParty.Party, null, leftPrisonRoster);
		return true;
	}

	private static bool DonateGarrisonDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
		MobileParty garrisonParty = currentSettlement.Town.GarrisonParty;
		if (garrisonParty == null)
		{
			currentSettlement.AddGarrisonParty();
			garrisonParty = currentSettlement.Town.GarrisonParty;
		}
		for (int i = 0; i < leftMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = leftMemberRoster.GetElementCopyAtIndex(i);
			garrisonParty.AddElementToMemberRoster(elementCopyAtIndex.Character, elementCopyAtIndex.Number);
			if (elementCopyAtIndex.Character.IsHero)
			{
				EnterSettlementAction.ApplyForCharacterOnly(elementCopyAtIndex.Character.HeroObject, currentSettlement);
			}
		}
		return true;
	}

	private static bool DonatePrisonersDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster leftSideTransferredPrisonerRoster, FlattenedTroopRoster rightSideTransferredPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		if (!rightSideTransferredPrisonerRoster.IsEmpty())
		{
			Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
			foreach (CharacterObject troop in rightSideTransferredPrisonerRoster.Troops)
			{
				if (troop.IsHero)
				{
					EnterSettlementAction.ApplyForPrisoner(troop.HeroObject, currentSettlement);
				}
			}
			CampaignEventDispatcher.Instance.OnPrisonerDonatedToSettlement(rightParty.MobileParty, rightSideTransferredPrisonerRoster, currentSettlement);
		}
		return true;
	}

	private static bool ManageGarrisonDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
		for (int i = 0; i < leftMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = leftMemberRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character.IsHero)
			{
				EnterSettlementAction.ApplyForCharacterOnly(elementCopyAtIndex.Character.HeroObject, currentSettlement);
			}
		}
		for (int j = 0; j < leftPrisonRoster.Count; j++)
		{
			TroopRosterElement elementCopyAtIndex2 = leftPrisonRoster.GetElementCopyAtIndex(j);
			if (elementCopyAtIndex2.Character.IsHero)
			{
				EnterSettlementAction.ApplyForPrisoner(elementCopyAtIndex2.Character.HeroObject, currentSettlement);
			}
		}
		return true;
	}

	private static bool CanTalkToHero(Hero hero, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty, out TextObject cantTalkReason)
	{
		cantTalkReason = TextObject.GetEmpty();
		return side == PartyScreenLogic.PartyRosterSide.Right;
	}

	private static bool ManageTroopsAndPrisonersDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		return true;
	}

	private static bool DefaultDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		HandleReleasedAndTakenPrisoners(takenPrisonerRoster, releasedPrisonerRoster);
		return true;
	}

	private static void HandleReleasedAndTakenPrisoners(FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster)
	{
		if (!releasedPrisonerRoster.IsEmpty())
		{
			EndCaptivityAction.ApplyByReleasedByChoice(releasedPrisonerRoster);
		}
		if (!takenPrisonerRoster.IsEmpty())
		{
			TakePrisonerAction.ApplyByTakenFromPartyScreen(takenPrisonerRoster);
		}
	}
}
