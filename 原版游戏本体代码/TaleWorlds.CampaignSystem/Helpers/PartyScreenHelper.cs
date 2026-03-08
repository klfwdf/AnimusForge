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

namespace Helpers
{
	// Token: 0x02000022 RID: 34
	public static class PartyScreenHelper
	{
		// Token: 0x06000113 RID: 275 RVA: 0x0000D6E4 File Offset: 0x0000B8E4
		public static PartyState GetActivePartyState()
		{
			GameStateManager gameStateManager = GameStateManager.Current;
			PartyState result;
			if ((result = ((gameStateManager != null) ? gameStateManager.ActiveState : null) as PartyState) != null)
			{
				return result;
			}
			Debug.FailedAssert("GetActivePartyState requested but the active state is not PartyState!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetActivePartyState", 7459);
			return null;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000D728 File Offset: 0x0000B928
		private static void OpenPartyScreen(bool isDonating = false)
		{
			Game game = Game.Current;
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
				TroopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate),
				PartyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DefaultDoneHandler),
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
			PartyState partyState = game.GameStateManager.CreateState<PartyState>();
			partyState.PartyScreenLogic = partyScreenLogic;
			partyState.IsDonating = isDonating;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
			game.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000D8B6 File Offset: 0x0000BAB6
		public static void CloseScreen(bool isForced, bool fromCancel = false)
		{
			PartyScreenHelper.ClosePartyPresentation(isForced, fromCancel);
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000D8C0 File Offset: 0x0000BAC0
		private static void ClosePartyPresentation(bool isForced, bool fromCancel)
		{
			PartyState activePartyState = PartyScreenHelper.GetActivePartyState();
			PartyScreenLogic partyScreenLogic = ((activePartyState != null) ? activePartyState.PartyScreenLogic : null);
			if (partyScreenLogic == null)
			{
				Debug.FailedAssert("Trying to close party screen when it's already closed!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "ClosePartyPresentation", 7526);
				return;
			}
			bool flag = true;
			if (!fromCancel)
			{
				flag = partyScreenLogic != null && partyScreenLogic.DoneLogic(isForced);
			}
			if (flag)
			{
				if (partyScreenLogic != null)
				{
					partyScreenLogic.OnPartyScreenClosed(fromCancel);
				}
				if (activePartyState != null)
				{
					activePartyState.PartyScreenLogic = null;
				}
				Game.Current.GameStateManager.PopState(0);
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000D938 File Offset: 0x0000BB38
		public static void OpenScreenAsCheat()
		{
			if (!Game.Current.CheatMode)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=!}Cheat mode is not enabled!", null), 0, null, null, "");
				return;
			}
			Game game = Game.Current;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			PartyScreenLogicInitializationData initializationData = new PartyScreenLogicInitializationData
			{
				LeftOwnerParty = null,
				RightOwnerParty = PartyBase.MainParty,
				LeftMemberRoster = PartyScreenHelper.GetRosterWithAllGameTroops(),
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
				TroopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate),
				PartyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DefaultDoneHandler),
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
			PartyState partyState = game.GameStateManager.CreateState<PartyState>();
			partyState.PartyScreenLogic = partyScreenLogic;
			partyState.IsDonating = false;
			game.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000DAE4 File Offset: 0x0000BCE4
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
			for (int j = 0; j < list.Count; j++)
			{
				CharacterObject character = list[j];
				troopRoster.AddToCounts(character, PartyScreenHelper._countToAddForEachTroopCheatMode, false, 0, 0, true, -1);
			}
			return troopRoster;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0000DBA3 File Offset: 0x0000BDA3
		public static void OpenScreenAsNormal()
		{
			if (Game.Current.CheatMode)
			{
				PartyScreenHelper.OpenScreenAsCheat();
				return;
			}
			PartyScreenHelper.OpenPartyScreen(false);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000DBC0 File Offset: 0x0000BDC0
		public static void OpenScreenAsRansom()
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Ransom;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			TroopRoster leftMemberRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.TransferableWithTrade;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.SellPrisonersDoneHandler);
			TextObject header = new TextObject("{=SvahUNo6}Ransom Prisoners", null);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, leftOwnerParty, GameTexts.FindText("str_ransom_broker", null), header, null, 0, 0, partyPresentationDoneButtonDelegate, null, null, null, null, false, false, false, false, 0);
			initializationData.RightMemberRoster = MobileParty.MainParty.MemberRoster.CloneRosterData();
			initializationData.RightPrisonerRoster = MobileParty.MainParty.PrisonRoster.CloneRosterData();
			initializationData.DoNotApplyGoldTransactions = true;
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000DCA0 File Offset: 0x0000BEA0
		public static void OpenScreenAsLoot(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TextObject leftPartyName, int leftPartySizeLimit, PartyScreenClosedDelegate partyScreenClosedDelegate = null)
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Loot;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DefaultDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, leftOwnerParty, leftPartyName, new TextObject("{=EOQcQa5l}Aftermath", null), null, leftPartySizeLimit, 0, partyPresentationDoneButtonDelegate, null, null, null, partyScreenClosedDelegate, false, false, false, false, 0);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x0000DD40 File Offset: 0x0000BF40
		public static void OpenScreenAsManageTroopsAndPrisoners(MobileParty leftParty, PartyScreenClosedDelegate onPartyScreenClosed = null)
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.ClanManageTroopAndPrisonerTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.ManageTroopsAndPrisonersDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(leftParty, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, new TextObject("{=uQgNPJnc}Manage Troops", null), partyPresentationDoneButtonDelegate, null, null, null, onPartyScreenClosed, false, false, true, false);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000DDD4 File Offset: 0x0000BFD4
		public static void OpenScreenAsReceiveTroops(TroopRoster leftMemberParty, TextObject leftPartyName, PartyScreenClosedDelegate partyScreenClosedDelegate = null)
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			int totalManCount = leftMemberParty.TotalManCount;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DefaultDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberParty, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, leftOwnerParty, leftPartyName, new TextObject("{=uQgNPJnc}Manage Troops", null), null, totalManCount, 0, partyPresentationDoneButtonDelegate, null, null, null, partyScreenClosedDelegate, false, false, false, false, 0);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000DE7C File Offset: 0x0000C07C
		public static void OpenScreenAsManageTroops(MobileParty leftParty)
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(leftParty, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.Transferable, new IsTroopTransferableDelegate(PartyScreenHelper.ClanManageTroopTransferableDelegate), partyState.PartyScreenMode, new TextObject("{=uQgNPJnc}Manage Troops", null), new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DefaultDoneHandler), null, null, null, null, false, false, true, false);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600011F RID: 287 RVA: 0x0000DF08 File Offset: 0x0000C108
		public static void OpenScreenAsDonateTroops(MobileParty leftParty)
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = leftParty.Owner.Clan != Clan.PlayerClan;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.DonateModeTroopTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = new PartyPresentationDoneButtonConditionDelegate(PartyScreenHelper.DonateDonePossibleDelegate);
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DefaultDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(leftParty, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, new TextObject("{=4YfjgtO2}Donate Troops", null), partyPresentationDoneButtonDelegate, partyPresentationDoneButtonConditionDelegate, null, null, null, false, false, true, false);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000DFB8 File Offset: 0x0000C1B8
		public static void OpenScreenAsDonateGarrisonWithCurrentSettlement()
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = true;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			if (Hero.MainHero.CurrentSettlement.Town.GarrisonParty == null)
			{
				Hero.MainHero.CurrentSettlement.AddGarrisonParty();
			}
			MobileParty garrisonParty = Hero.MainHero.CurrentSettlement.Town.GarrisonParty;
			int num = Math.Max(garrisonParty.Party.PartySizeLimit - garrisonParty.Party.NumberOfAllMembers, 0);
			TroopRoster leftMemberRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			TextObject name = garrisonParty.Name;
			int leftPartyMembersSizeLimit = num;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DonateGarrisonDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, leftOwnerParty, name, new TextObject("{=uQgNPJnc}Manage Troops", null), null, leftPartyMembersSizeLimit, 0, partyPresentationDoneButtonDelegate, null, null, null, null, false, false, false, false, 0);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000E0BC File Offset: 0x0000C2BC
		public static void OpenScreenAsDonatePrisoners()
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = true;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.PrisonerManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			if (Hero.MainHero.CurrentSettlement.Town.GarrisonParty == null)
			{
				Hero.MainHero.CurrentSettlement.AddGarrisonParty();
			}
			TroopRoster prisonRoster = Hero.MainHero.CurrentSettlement.Party.PrisonRoster;
			int num = Math.Max(Hero.MainHero.CurrentSettlement.Party.PrisonerSizeLimit - prisonRoster.Count, 0);
			TextObject textObject = new TextObject("{=SDzIAtiA}Prisoners of {SETTLEMENT_NAME}", null);
			textObject.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.Name);
			TroopRoster leftMemberRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster leftPrisonerRoster = prisonRoster;
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.DonatePrisonerTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			TextObject leftPartyName = textObject;
			int leftPartyPrisonersSizeLimit = num;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.DonatePrisonersDoneHandler);
			PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = new PartyPresentationDoneButtonConditionDelegate(PartyScreenHelper.DonateDonePossibleDelegate);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, leftOwnerParty, leftPartyName, new TextObject("{=Z212GSiV}Leave Prisoners", null), null, 0, leftPartyPrisonersSizeLimit, partyPresentationDoneButtonDelegate, partyPresentationDoneButtonConditionDelegate, null, null, null, false, false, false, false, 0);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000E1F4 File Offset: 0x0000C3F4
		private static Tuple<bool, TextObject> DonateDonePossibleDelegate(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
		{
			PartyState activePartyState = PartyScreenHelper.GetActivePartyState();
			PartyScreenLogic partyScreenLogic = ((activePartyState != null) ? activePartyState.PartyScreenLogic : null);
			if (partyScreenLogic.IsThereAnyChanges())
			{
				if (partyScreenLogic.CurrentData.TransferredPrisonersHistory.Any((Tuple<CharacterObject, int> p) => p.Item2 > 0))
				{
					return new Tuple<bool, TextObject>(false, new TextObject("{=hI7eDbXs}You cannot take prisoners.", null));
				}
				if (partyScreenLogic.HaveRightSideGainedTroops())
				{
					return new Tuple<bool, TextObject>(false, new TextObject("{=pvkl6pZh}You cannot take troops.", null));
				}
				if ((partyScreenLogic.MemberTransferState != PartyScreenLogic.TransferState.NotTransferable || partyScreenLogic.AccompanyingTransferState != PartyScreenLogic.TransferState.NotTransferable) && partyScreenLogic.LeftPartyMembersSizeLimit < partyScreenLogic.MemberRosters[0].TotalManCount)
				{
					return new Tuple<bool, TextObject>(false, new TextObject("{=R7wiHjcL}Donated troops exceed party capacity.", null));
				}
				if (partyScreenLogic.PrisonerTransferState != PartyScreenLogic.TransferState.NotTransferable && partyScreenLogic.LeftPartyPrisonersSizeLimit < partyScreenLogic.PrisonerRosters[0].TotalManCount)
				{
					return new Tuple<bool, TextObject>(false, new TextObject("{=3nfPGbN0}Donated prisoners exceed party capacity.", null));
				}
			}
			return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000E2EC File Offset: 0x0000C4EC
		public static bool DonatePrisonerTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return side == PartyScreenLogic.PartyRosterSide.Right && type == PartyScreenLogic.TroopType.Prisoner;
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0000E2F8 File Offset: 0x0000C4F8
		public static void OpenScreenAsManagePrisoners()
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.PrisonerManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			TroopRoster prisonRoster = Hero.MainHero.CurrentSettlement.Party.PrisonRoster;
			TextObject textObject = new TextObject("{=SDzIAtiA}Prisoners of {SETTLEMENT_NAME}", null);
			textObject.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.Name);
			TroopRoster leftMemberRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster leftPrisonerRoster = prisonRoster;
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable;
			IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenHelper.TroopTransferableDelegate);
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			TextObject leftPartyName = textObject;
			int prisonerSizeLimit = Hero.MainHero.CurrentSettlement.Party.PrisonerSizeLimit;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.ManageGarrisonDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, troopTransferableDelegate, partyScreenMode, leftOwnerParty, leftPartyName, new TextObject("{=aadTnAEg}Manage Prisoners", null), null, 0, prisonerSizeLimit, partyPresentationDoneButtonDelegate, null, null, null, null, false, false, false, false, 0);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x06000125 RID: 293 RVA: 0x0000E3E8 File Offset: 0x0000C5E8
		public static bool TroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
		{
			Hero hero = ((leftOwnerParty != null) ? leftOwnerParty.LeaderHero : null);
			bool flag;
			if ((hero == null || hero.Clan != Clan.PlayerClan) && (leftOwnerParty == null || !leftOwnerParty.IsMobile || !leftOwnerParty.MobileParty.IsCaravan || leftOwnerParty.Owner != Hero.MainHero))
			{
				if (leftOwnerParty != null && leftOwnerParty.IsMobile && leftOwnerParty.MobileParty.IsGarrison)
				{
					Settlement currentSettlement = leftOwnerParty.MobileParty.CurrentSettlement;
					flag = ((currentSettlement != null) ? currentSettlement.OwnerClan : null) == Clan.PlayerClan;
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = true;
			}
			bool flag2 = flag;
			return !character.IsHero || (character.IsHero && character.HeroObject.Clan != Clan.PlayerClan && (!character.HeroObject.IsPlayerCompanion || (character.HeroObject.IsPlayerCompanion && flag2)));
		}

		// Token: 0x06000126 RID: 294 RVA: 0x0000E4B6 File Offset: 0x0000C6B6
		public static bool ClanManageTroopAndPrisonerTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return !character.IsHero || character.HeroObject.IsPrisoner;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x0000E4CD File Offset: 0x0000C6CD
		public static bool ClanManageTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return !character.IsHero;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000E4D8 File Offset: 0x0000C6D8
		public static bool DonateModeTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return !character.IsHero && side == PartyScreenLogic.PartyRosterSide.Right;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000E4E8 File Offset: 0x0000C6E8
		public static void OpenScreenWithCondition(IsTroopTransferableDelegate isTroopTransferable, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyPresentationDoneButtonDelegate onDoneClicked, PartyPresentationCancelButtonDelegate onCancelClicked, PartyScreenLogic.TransferState memberTransferState, PartyScreenLogic.TransferState prisonerTransferState, TextObject leftPartyName, int limit, bool showProgressBar, bool isDonating, PartyScreenHelper.PartyScreenMode screenMode = PartyScreenHelper.PartyScreenMode.Normal, TroopRoster memberRosterLeft = null, TroopRoster prisonerRosterLeft = null)
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
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(memberRosterLeft, prisonerRosterLeft, memberTransferState, prisonerTransferState, PartyScreenLogic.TransferState.NotTransferable, isTroopTransferable, partyState.PartyScreenMode, null, leftPartyName, new TextObject("{=nZaeTlj8}Exchange Troops", null), null, limit, 0, onDoneClicked, doneButtonCondition, onCancelClicked, null, null, false, false, false, showProgressBar, 0);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000E598 File Offset: 0x0000C798
		public static void OpenScreenForManagingAlley(bool isNewAlley, TroopRoster memberRosterLeft, IsTroopTransferableDelegate isTroopTransferable, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyPresentationDoneButtonDelegate onDoneClicked, TextObject leftPartyName, PartyPresentationCancelButtonDelegate onCancelButtonClicked)
		{
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			TroopRoster troopRoster = (isNewAlley ? MobileParty.MainParty.MemberRoster.CloneRosterData() : MobileParty.MainParty.MemberRoster);
			TroopRosterElement troopRosterElement = memberRosterLeft.GetTroopRoster().Find((TroopRosterElement x) => x.Character.IsHero);
			if (troopRoster.Contains(troopRosterElement.Character))
			{
				troopRoster.RemoveTroop(troopRosterElement.Character, 1, default(UniqueTroopDescriptor), 0);
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
				PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage,
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
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000E754 File Offset: 0x0000C954
		public static void OpenScreenAsQuest(TroopRoster leftMemberRoster, TextObject leftPartyName, int leftPartySizeLimit, int questDaysMultiplier, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null)
		{
			Debug.Print("PartyScreenManager::OpenScreenAsQuest", 0, Debug.DebugColor.White, 17592186044416UL);
			PartyState partyState = Game.Current.GameStateManager.CreateState<PartyState>();
			partyState.IsDonating = false;
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.QuestTroopManage;
			PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
			TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
			PartyScreenLogic.TransferState memberTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenLogic.TransferState prisonerTransferState = PartyScreenLogic.TransferState.NotTransferable;
			PartyScreenLogic.TransferState accompanyingTransferState = PartyScreenLogic.TransferState.Transferable;
			PartyScreenHelper.PartyScreenMode partyScreenMode = partyState.PartyScreenMode;
			PartyBase leftOwnerParty = null;
			PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.ManageTroopsAndPrisonersDoneHandler);
			PartyScreenLogicInitializationData initializationData = PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(leftMemberRoster, leftPrisonerRoster, memberTransferState, prisonerTransferState, accompanyingTransferState, isTroopTransferable, partyScreenMode, leftOwnerParty, leftPartyName, new TextObject("{=nZaeTlj8}Exchange Troops", null), null, leftPartySizeLimit, 0, partyPresentationDoneButtonDelegate, doneButtonCondition, null, partyPresentationCancelButtonActivateDelegate, onPartyScreenClosed, false, true, false, true, questDaysMultiplier);
			partyScreenLogic.Initialize(initializationData);
			partyState.PartyScreenLogic = partyScreenLogic;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000E80C File Offset: 0x0000CA0C
		public static void OpenScreenWithDummyRoster(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, TextObject leftPartyName, TextObject rightPartyName, int leftPartySizeLimit, int rightPartySizeLimit, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable, CanTalkToHeroDelegate canTalkToTroopDelegate = null, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null)
		{
			Debug.Print("PartyScreenManager::OpenScreenWithDummyRoster", 0, Debug.DebugColor.White, 17592186044416UL);
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
				CanTalkToTroopDelegate = (canTalkToTroopDelegate ?? new CanTalkToHeroDelegate(PartyScreenHelper.CanTalkToHero)),
				PartyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenHelper.ManageTroopsAndPrisonersDoneHandler),
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
			partyState.PartyScreenMode = PartyScreenHelper.PartyScreenMode.TroopsManage;
			Game.Current.GameStateManager.PushState(partyState, 0);
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0000E99C File Offset: 0x0000CB9C
		public static void OpenScreenWithDummyRosterWithMainParty(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TextObject leftPartyName, int leftPartySizeLimit, PartyPresentationDoneButtonConditionDelegate doneButtonCondition, PartyScreenClosedDelegate onPartyScreenClosed, IsTroopTransferableDelegate isTroopTransferable, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null)
		{
			Debug.Print("PartyScreenManager::OpenScreenWithDummyRosterWithMainParty", 0, Debug.DebugColor.White, 17592186044416UL);
			PartyScreenHelper.OpenScreenWithDummyRoster(leftMemberRoster, leftPrisonerRoster, MobileParty.MainParty.MemberRoster, MobileParty.MainParty.PrisonRoster, leftPartyName, MobileParty.MainParty.Name, leftPartySizeLimit, MobileParty.MainParty.Party.PartySizeLimit, doneButtonCondition, onPartyScreenClosed, isTroopTransferable, null, partyPresentationCancelButtonActivateDelegate);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x0000EA00 File Offset: 0x0000CC00
		public static void OpenScreenAsCreateClanPartyForHero(Hero hero, PartyScreenClosedDelegate onScreenClosed = null, IsTroopTransferableDelegate isTroopTransferable = null)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster leftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster troopRoster2 = MobileParty.MainParty.MemberRoster.CloneRosterData();
			TroopRoster rightPrisonerRoster = MobileParty.MainParty.PrisonRoster.CloneRosterData();
			troopRoster.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
			if (troopRoster2.Contains(hero.CharacterObject))
			{
				troopRoster2.AddToCounts(hero.CharacterObject, -1, false, 0, 0, true, -1);
			}
			CanTalkToHeroDelegate canTalkToTroopDelegate = delegate(Hero heroCharacter, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty, out TextObject cantTalkReason)
			{
				cantTalkReason = new TextObject("{=8muhth8y}You can't talk to your companions while creating a party.", null);
				return false;
			};
			TextObject textObject = GameTexts.FindText("str_lord_party_name", null);
			textObject.SetCharacterProperties("TROOP", hero.CharacterObject, false);
			PartyScreenHelper.OpenScreenWithDummyRoster(troopRoster, leftPrisonerRoster, troopRoster2, rightPrisonerRoster, textObject, MobileParty.MainParty.Name, Campaign.Current.Models.PartySizeLimitModel.GetAssumedPartySizeForLordParty(hero, hero.Clan.MapFaction, hero.Clan), MobileParty.MainParty.Party.PartySizeLimit, null, onScreenClosed ?? new PartyScreenClosedDelegate(PartyScreenHelper.OpenScreenAsCreateClanPartyForHeroPartyScreenClosed), isTroopTransferable ?? new IsTroopTransferableDelegate(PartyScreenHelper.OpenScreenAsCreateClanPartyForHeroTroopTransferableDelegate), canTalkToTroopDelegate, null);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000EB1C File Offset: 0x0000CD1C
		private static void OpenScreenAsCreateClanPartyForHeroPartyScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
		{
			if (!fromCancel)
			{
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
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, partyGoldLowerThreshold - hero.Gold, false);
				}
				MobileParty mobileParty = MobilePartyHelper.CreateNewClanMobileParty(hero, hero.Clan);
				foreach (TroopRosterElement troopRosterElement in leftMemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character != hero.CharacterObject)
					{
						mobileParty.MemberRoster.Add(troopRosterElement);
						rightOwnerParty.MemberRoster.AddToCounts(troopRosterElement.Character, -troopRosterElement.Number, false, -troopRosterElement.WoundedNumber, -troopRosterElement.Xp, true, -1);
					}
				}
				foreach (TroopRosterElement troopRosterElement2 in leftPrisonRoster.GetTroopRoster())
				{
					mobileParty.PrisonRoster.Add(troopRosterElement2);
					rightOwnerParty.PrisonRoster.AddToCounts(troopRosterElement2.Character, -troopRosterElement2.Number, false, -troopRosterElement2.WoundedNumber, -troopRosterElement2.Xp, true, -1);
				}
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x0000ECC0 File Offset: 0x0000CEC0
		private static bool OpenScreenAsCreateClanPartyForHeroTroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return !character.IsHero;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000ECCB File Offset: 0x0000CECB
		private static bool SellPrisonersDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
		{
			SellPrisonersAction.ApplyForSelectedPrisoners(MobileParty.MainParty.Party, null, leftPrisonRoster);
			return true;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000ECE0 File Offset: 0x0000CEE0
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
				garrisonParty.AddElementToMemberRoster(elementCopyAtIndex.Character, elementCopyAtIndex.Number, false);
				if (elementCopyAtIndex.Character.IsHero)
				{
					EnterSettlementAction.ApplyForCharacterOnly(elementCopyAtIndex.Character.HeroObject, currentSettlement);
				}
			}
			return true;
		}

		// Token: 0x06000133 RID: 307 RVA: 0x0000ED68 File Offset: 0x0000CF68
		private static bool DonatePrisonersDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster leftSideTransferredPrisonerRoster, FlattenedTroopRoster rightSideTransferredPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
		{
			if (!rightSideTransferredPrisonerRoster.IsEmpty<FlattenedTroopRosterElement>())
			{
				Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
				foreach (CharacterObject characterObject in rightSideTransferredPrisonerRoster.Troops)
				{
					if (characterObject.IsHero)
					{
						EnterSettlementAction.ApplyForPrisoner(characterObject.HeroObject, currentSettlement);
					}
				}
				CampaignEventDispatcher.Instance.OnPrisonerDonatedToSettlement(rightParty.MobileParty, rightSideTransferredPrisonerRoster, currentSettlement);
			}
			return true;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x0000EDEC File Offset: 0x0000CFEC
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

		// Token: 0x06000135 RID: 309 RVA: 0x0000EE76 File Offset: 0x0000D076
		private static bool CanTalkToHero(Hero hero, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty, out TextObject cantTalkReason)
		{
			cantTalkReason = TextObject.GetEmpty();
			return side == PartyScreenLogic.PartyRosterSide.Right;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000EE84 File Offset: 0x0000D084
		private static bool ManageTroopsAndPrisonersDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
		{
			return true;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0000EE87 File Offset: 0x0000D087
		private static bool DefaultDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
		{
			PartyScreenHelper.HandleReleasedAndTakenPrisoners(takenPrisonerRoster, releasedPrisonerRoster);
			return true;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000EE93 File Offset: 0x0000D093
		private static void HandleReleasedAndTakenPrisoners(FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster)
		{
			if (!releasedPrisonerRoster.IsEmpty<FlattenedTroopRosterElement>())
			{
				EndCaptivityAction.ApplyByReleasedByChoice(releasedPrisonerRoster);
			}
			if (!takenPrisonerRoster.IsEmpty<FlattenedTroopRosterElement>())
			{
				TakePrisonerAction.ApplyByTakenFromPartyScreen(takenPrisonerRoster);
			}
		}

		// Token: 0x04000005 RID: 5
		private static readonly int _countToAddForEachTroopCheatMode = 10;

		// Token: 0x020004E6 RID: 1254
		public enum PartyScreenMode
		{
			// Token: 0x040014C3 RID: 5315
			Normal,
			// Token: 0x040014C4 RID: 5316
			Shared,
			// Token: 0x040014C5 RID: 5317
			Loot,
			// Token: 0x040014C6 RID: 5318
			Ransom,
			// Token: 0x040014C7 RID: 5319
			PrisonerManage,
			// Token: 0x040014C8 RID: 5320
			TroopsManage,
			// Token: 0x040014C9 RID: 5321
			QuestTroopManage
		}
	}
}
