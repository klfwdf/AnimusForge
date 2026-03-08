using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Party
{
	// Token: 0x020002FC RID: 764
	public class PartyScreenLogic
	{
		// Token: 0x14000012 RID: 18
		// (add) Token: 0x06002C08 RID: 11272 RVA: 0x000B7764 File Offset: 0x000B5964
		// (remove) Token: 0x06002C09 RID: 11273 RVA: 0x000B779C File Offset: 0x000B599C
		public event PartyScreenLogic.PartyGoldDelegate PartyGoldChange;

		// Token: 0x14000013 RID: 19
		// (add) Token: 0x06002C0A RID: 11274 RVA: 0x000B77D4 File Offset: 0x000B59D4
		// (remove) Token: 0x06002C0B RID: 11275 RVA: 0x000B780C File Offset: 0x000B5A0C
		public event PartyScreenLogic.PartyMoraleDelegate PartyMoraleChange;

		// Token: 0x14000014 RID: 20
		// (add) Token: 0x06002C0C RID: 11276 RVA: 0x000B7844 File Offset: 0x000B5A44
		// (remove) Token: 0x06002C0D RID: 11277 RVA: 0x000B787C File Offset: 0x000B5A7C
		public event PartyScreenLogic.PartyInfluenceDelegate PartyInfluenceChange;

		// Token: 0x14000015 RID: 21
		// (add) Token: 0x06002C0E RID: 11278 RVA: 0x000B78B4 File Offset: 0x000B5AB4
		// (remove) Token: 0x06002C0F RID: 11279 RVA: 0x000B78EC File Offset: 0x000B5AEC
		public event PartyScreenLogic.PartyHorseDelegate PartyHorseChange;

		// Token: 0x14000016 RID: 22
		// (add) Token: 0x06002C10 RID: 11280 RVA: 0x000B7924 File Offset: 0x000B5B24
		// (remove) Token: 0x06002C11 RID: 11281 RVA: 0x000B795C File Offset: 0x000B5B5C
		public event PartyScreenLogic.PresentationUpdate Update;

		// Token: 0x14000017 RID: 23
		// (add) Token: 0x06002C12 RID: 11282 RVA: 0x000B7994 File Offset: 0x000B5B94
		// (remove) Token: 0x06002C13 RID: 11283 RVA: 0x000B79CC File Offset: 0x000B5BCC
		public event PartyScreenClosedDelegate PartyScreenClosedEvent;

		// Token: 0x14000018 RID: 24
		// (add) Token: 0x06002C14 RID: 11284 RVA: 0x000B7A04 File Offset: 0x000B5C04
		// (remove) Token: 0x06002C15 RID: 11285 RVA: 0x000B7A3C File Offset: 0x000B5C3C
		public event PartyScreenLogic.AfterResetDelegate AfterReset;

		// Token: 0x17000AD6 RID: 2774
		// (get) Token: 0x06002C16 RID: 11286 RVA: 0x000B7A71 File Offset: 0x000B5C71
		// (set) Token: 0x06002C17 RID: 11287 RVA: 0x000B7A79 File Offset: 0x000B5C79
		public PartyScreenLogic.TroopSortType ActiveOtherPartySortType
		{
			get
			{
				return this._activeOtherPartySortType;
			}
			set
			{
				this._activeOtherPartySortType = value;
			}
		}

		// Token: 0x17000AD7 RID: 2775
		// (get) Token: 0x06002C18 RID: 11288 RVA: 0x000B7A82 File Offset: 0x000B5C82
		// (set) Token: 0x06002C19 RID: 11289 RVA: 0x000B7A8A File Offset: 0x000B5C8A
		public PartyScreenLogic.TroopSortType ActiveMainPartySortType
		{
			get
			{
				return this._activeMainPartySortType;
			}
			set
			{
				this._activeMainPartySortType = value;
			}
		}

		// Token: 0x17000AD8 RID: 2776
		// (get) Token: 0x06002C1A RID: 11290 RVA: 0x000B7A93 File Offset: 0x000B5C93
		// (set) Token: 0x06002C1B RID: 11291 RVA: 0x000B7A9B File Offset: 0x000B5C9B
		public bool IsOtherPartySortAscending
		{
			get
			{
				return this._isOtherPartySortAscending;
			}
			set
			{
				this._isOtherPartySortAscending = value;
			}
		}

		// Token: 0x17000AD9 RID: 2777
		// (get) Token: 0x06002C1C RID: 11292 RVA: 0x000B7AA4 File Offset: 0x000B5CA4
		// (set) Token: 0x06002C1D RID: 11293 RVA: 0x000B7AAC File Offset: 0x000B5CAC
		public bool IsMainPartySortAscending
		{
			get
			{
				return this._isMainPartySortAscending;
			}
			set
			{
				this._isMainPartySortAscending = value;
			}
		}

		// Token: 0x17000ADA RID: 2778
		// (get) Token: 0x06002C1E RID: 11294 RVA: 0x000B7AB5 File Offset: 0x000B5CB5
		// (set) Token: 0x06002C1F RID: 11295 RVA: 0x000B7ABD File Offset: 0x000B5CBD
		public PartyScreenLogic.TransferState MemberTransferState { get; private set; }

		// Token: 0x17000ADB RID: 2779
		// (get) Token: 0x06002C20 RID: 11296 RVA: 0x000B7AC6 File Offset: 0x000B5CC6
		// (set) Token: 0x06002C21 RID: 11297 RVA: 0x000B7ACE File Offset: 0x000B5CCE
		public PartyScreenLogic.TransferState PrisonerTransferState { get; private set; }

		// Token: 0x17000ADC RID: 2780
		// (get) Token: 0x06002C22 RID: 11298 RVA: 0x000B7AD7 File Offset: 0x000B5CD7
		// (set) Token: 0x06002C23 RID: 11299 RVA: 0x000B7ADF File Offset: 0x000B5CDF
		public PartyScreenLogic.TransferState AccompanyingTransferState { get; private set; }

		// Token: 0x17000ADD RID: 2781
		// (get) Token: 0x06002C24 RID: 11300 RVA: 0x000B7AE8 File Offset: 0x000B5CE8
		// (set) Token: 0x06002C25 RID: 11301 RVA: 0x000B7AF0 File Offset: 0x000B5CF0
		public TextObject LeftPartyName { get; private set; }

		// Token: 0x17000ADE RID: 2782
		// (get) Token: 0x06002C26 RID: 11302 RVA: 0x000B7AF9 File Offset: 0x000B5CF9
		// (set) Token: 0x06002C27 RID: 11303 RVA: 0x000B7B01 File Offset: 0x000B5D01
		public TextObject RightPartyName { get; private set; }

		// Token: 0x17000ADF RID: 2783
		// (get) Token: 0x06002C28 RID: 11304 RVA: 0x000B7B0A File Offset: 0x000B5D0A
		// (set) Token: 0x06002C29 RID: 11305 RVA: 0x000B7B12 File Offset: 0x000B5D12
		public TextObject Header { get; private set; }

		// Token: 0x17000AE0 RID: 2784
		// (get) Token: 0x06002C2A RID: 11306 RVA: 0x000B7B1B File Offset: 0x000B5D1B
		// (set) Token: 0x06002C2B RID: 11307 RVA: 0x000B7B23 File Offset: 0x000B5D23
		public int LeftPartyMembersSizeLimit { get; private set; }

		// Token: 0x17000AE1 RID: 2785
		// (get) Token: 0x06002C2C RID: 11308 RVA: 0x000B7B2C File Offset: 0x000B5D2C
		// (set) Token: 0x06002C2D RID: 11309 RVA: 0x000B7B34 File Offset: 0x000B5D34
		public int LeftPartyPrisonersSizeLimit { get; private set; }

		// Token: 0x17000AE2 RID: 2786
		// (get) Token: 0x06002C2E RID: 11310 RVA: 0x000B7B3D File Offset: 0x000B5D3D
		// (set) Token: 0x06002C2F RID: 11311 RVA: 0x000B7B45 File Offset: 0x000B5D45
		public int RightPartyMembersSizeLimit { get; private set; }

		// Token: 0x17000AE3 RID: 2787
		// (get) Token: 0x06002C30 RID: 11312 RVA: 0x000B7B4E File Offset: 0x000B5D4E
		// (set) Token: 0x06002C31 RID: 11313 RVA: 0x000B7B56 File Offset: 0x000B5D56
		public int RightPartyPrisonersSizeLimit { get; private set; }

		// Token: 0x17000AE4 RID: 2788
		// (get) Token: 0x06002C32 RID: 11314 RVA: 0x000B7B5F File Offset: 0x000B5D5F
		// (set) Token: 0x06002C33 RID: 11315 RVA: 0x000B7B67 File Offset: 0x000B5D67
		public bool DoNotApplyGoldTransactions { get; private set; }

		// Token: 0x17000AE5 RID: 2789
		// (get) Token: 0x06002C34 RID: 11316 RVA: 0x000B7B70 File Offset: 0x000B5D70
		// (set) Token: 0x06002C35 RID: 11317 RVA: 0x000B7B78 File Offset: 0x000B5D78
		public bool ShowProgressBar { get; private set; }

		// Token: 0x17000AE6 RID: 2790
		// (get) Token: 0x06002C36 RID: 11318 RVA: 0x000B7B81 File Offset: 0x000B5D81
		// (set) Token: 0x06002C37 RID: 11319 RVA: 0x000B7B89 File Offset: 0x000B5D89
		public string DoneReasonString { get; private set; }

		// Token: 0x17000AE7 RID: 2791
		// (get) Token: 0x06002C38 RID: 11320 RVA: 0x000B7B92 File Offset: 0x000B5D92
		// (set) Token: 0x06002C39 RID: 11321 RVA: 0x000B7B9A File Offset: 0x000B5D9A
		public bool IsTroopUpgradesDisabled { get; private set; }

		// Token: 0x17000AE8 RID: 2792
		// (get) Token: 0x06002C3A RID: 11322 RVA: 0x000B7BA3 File Offset: 0x000B5DA3
		// (set) Token: 0x06002C3B RID: 11323 RVA: 0x000B7BAB File Offset: 0x000B5DAB
		public CharacterObject RightPartyLeader { get; private set; }

		// Token: 0x17000AE9 RID: 2793
		// (get) Token: 0x06002C3C RID: 11324 RVA: 0x000B7BB4 File Offset: 0x000B5DB4
		// (set) Token: 0x06002C3D RID: 11325 RVA: 0x000B7BBC File Offset: 0x000B5DBC
		public CharacterObject LeftPartyLeader { get; private set; }

		// Token: 0x17000AEA RID: 2794
		// (get) Token: 0x06002C3E RID: 11326 RVA: 0x000B7BC5 File Offset: 0x000B5DC5
		// (set) Token: 0x06002C3F RID: 11327 RVA: 0x000B7BCD File Offset: 0x000B5DCD
		public PartyBase LeftOwnerParty { get; private set; }

		// Token: 0x17000AEB RID: 2795
		// (get) Token: 0x06002C40 RID: 11328 RVA: 0x000B7BD6 File Offset: 0x000B5DD6
		// (set) Token: 0x06002C41 RID: 11329 RVA: 0x000B7BDE File Offset: 0x000B5DDE
		public PartyBase RightOwnerParty { get; private set; }

		// Token: 0x17000AEC RID: 2796
		// (get) Token: 0x06002C42 RID: 11330 RVA: 0x000B7BE7 File Offset: 0x000B5DE7
		// (set) Token: 0x06002C43 RID: 11331 RVA: 0x000B7BEF File Offset: 0x000B5DEF
		public PartyScreenData CurrentData { get; private set; }

		// Token: 0x17000AED RID: 2797
		// (get) Token: 0x06002C44 RID: 11332 RVA: 0x000B7BF8 File Offset: 0x000B5DF8
		// (set) Token: 0x06002C45 RID: 11333 RVA: 0x000B7C00 File Offset: 0x000B5E00
		public bool TransferHealthiesGetWoundedsFirst { get; private set; }

		// Token: 0x17000AEE RID: 2798
		// (get) Token: 0x06002C46 RID: 11334 RVA: 0x000B7C09 File Offset: 0x000B5E09
		// (set) Token: 0x06002C47 RID: 11335 RVA: 0x000B7C11 File Offset: 0x000B5E11
		public int QuestModeWageDaysMultiplier { get; private set; }

		// Token: 0x17000AEF RID: 2799
		// (get) Token: 0x06002C48 RID: 11336 RVA: 0x000B7C1A File Offset: 0x000B5E1A
		// (set) Token: 0x06002C49 RID: 11337 RVA: 0x000B7C22 File Offset: 0x000B5E22
		public Game Game
		{
			get
			{
				return this._game;
			}
			set
			{
				this._game = value;
			}
		}

		// Token: 0x06002C4A RID: 11338 RVA: 0x000B7C2C File Offset: 0x000B5E2C
		public PartyScreenLogic()
		{
			this._game = Game.Current;
			this.MemberRosters = new TroopRoster[2];
			this.PrisonerRosters = new TroopRoster[2];
			this.CurrentData = new PartyScreenData();
			this._initialData = new PartyScreenData();
			this._defaultComparers = new Dictionary<PartyScreenLogic.TroopSortType, PartyScreenLogic.TroopComparer>
			{
				{
					PartyScreenLogic.TroopSortType.Custom,
					new PartyScreenLogic.TroopDefaultComparer()
				},
				{
					PartyScreenLogic.TroopSortType.Type,
					new PartyScreenLogic.TroopTypeComparer()
				},
				{
					PartyScreenLogic.TroopSortType.Name,
					new PartyScreenLogic.TroopNameComparer()
				},
				{
					PartyScreenLogic.TroopSortType.Count,
					new PartyScreenLogic.TroopCountComparer()
				},
				{
					PartyScreenLogic.TroopSortType.Tier,
					new PartyScreenLogic.TroopTierComparer()
				}
			};
			this.IsTroopUpgradesDisabled = false;
		}

		// Token: 0x06002C4B RID: 11339 RVA: 0x000B7CC8 File Offset: 0x000B5EC8
		public void Initialize(PartyScreenLogicInitializationData initializationData)
		{
			this.MemberRosters[1] = initializationData.RightMemberRoster;
			this.PrisonerRosters[1] = initializationData.RightPrisonerRoster;
			this.MemberRosters[0] = initializationData.LeftMemberRoster;
			this.PrisonerRosters[0] = initializationData.LeftPrisonerRoster;
			Hero rightLeaderHero = initializationData.RightLeaderHero;
			this.RightPartyLeader = ((rightLeaderHero != null) ? rightLeaderHero.CharacterObject : null);
			Hero leftLeaderHero = initializationData.LeftLeaderHero;
			this.LeftPartyLeader = ((leftLeaderHero != null) ? leftLeaderHero.CharacterObject : null);
			this.RightOwnerParty = initializationData.RightOwnerParty;
			this.LeftOwnerParty = initializationData.LeftOwnerParty;
			this.RightPartyName = initializationData.RightPartyName;
			this.RightPartyMembersSizeLimit = initializationData.RightPartyMembersSizeLimit;
			this.RightPartyPrisonersSizeLimit = initializationData.RightPartyPrisonersSizeLimit;
			this.LeftPartyName = initializationData.LeftPartyName;
			this.LeftPartyMembersSizeLimit = initializationData.LeftPartyMembersSizeLimit;
			this.LeftPartyPrisonersSizeLimit = initializationData.LeftPartyPrisonersSizeLimit;
			this.Header = initializationData.Header;
			this.QuestModeWageDaysMultiplier = initializationData.QuestModeWageDaysMultiplier;
			this.TransferHealthiesGetWoundedsFirst = initializationData.TransferHealthiesGetWoundedsFirst;
			this.SetPartyGoldChangeAmount(0);
			this.SetHorseChangeAmount(0);
			this.SetInfluenceChangeAmount(0, 0, 0);
			this.SetMoraleChangeAmount(0);
			this.CurrentData.BindRostersFrom(this.MemberRosters[1], this.PrisonerRosters[1], this.MemberRosters[0], this.PrisonerRosters[0], this.RightOwnerParty, this.LeftOwnerParty);
			this._initialData.InitializeCopyFrom(initializationData.RightOwnerParty, initializationData.LeftOwnerParty);
			this._initialData.CopyFromPartyAndRoster(this.MemberRosters[1], this.PrisonerRosters[1], this.MemberRosters[0], this.PrisonerRosters[0], this.RightOwnerParty);
			if (initializationData.PartyPresentationDoneButtonDelegate == null)
			{
				Debug.FailedAssert("Done handler is given null for party screen!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "Initialize", 242);
				initializationData.PartyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(PartyScreenLogic.DefaultDoneHandler);
			}
			this.PartyPresentationDoneButtonDelegate = initializationData.PartyPresentationDoneButtonDelegate;
			this.PartyPresentationDoneButtonConditionDelegate = initializationData.PartyPresentationDoneButtonConditionDelegate;
			this.PartyPresentationCancelButtonActivateDelegate = initializationData.PartyPresentationCancelButtonActivateDelegate;
			this.PartyPresentationCancelButtonDelegate = initializationData.PartyPresentationCancelButtonDelegate;
			this.IsTroopUpgradesDisabled = initializationData.IsTroopUpgradesDisabled || initializationData.RightOwnerParty == null;
			this.MemberTransferState = initializationData.MemberTransferState;
			this.PrisonerTransferState = initializationData.PrisonerTransferState;
			this.AccompanyingTransferState = initializationData.AccompanyingTransferState;
			this.IsTroopTransferableDelegate = initializationData.TroopTransferableDelegate;
			this.CanTalkToHeroDelegate = initializationData.CanTalkToTroopDelegate;
			this.PartyPresentationCancelButtonActivateDelegate = initializationData.PartyPresentationCancelButtonActivateDelegate;
			this.PartyPresentationCancelButtonDelegate = initializationData.PartyPresentationCancelButtonDelegate;
			this.PartyScreenClosedEvent = initializationData.PartyScreenClosedDelegate;
			this.DoNotApplyGoldTransactions = initializationData.DoNotApplyGoldTransactions;
			this.ShowProgressBar = initializationData.ShowProgressBar;
			if (this._partyScreenMode == PartyScreenHelper.PartyScreenMode.QuestTroopManage)
			{
				int partyGoldChangeAmount = -this.MemberRosters[0].Sum((TroopRosterElement t) => t.Character.TroopWage * t.Number * this.QuestModeWageDaysMultiplier);
				this._initialData.PartyGoldChangeAmount = partyGoldChangeAmount;
				this.SetPartyGoldChangeAmount(partyGoldChangeAmount);
			}
		}

		// Token: 0x06002C4C RID: 11340 RVA: 0x000B7F8B File Offset: 0x000B618B
		private void SetPartyGoldChangeAmount(int newTotalAmount)
		{
			this.CurrentData.PartyGoldChangeAmount = newTotalAmount;
			PartyScreenLogic.PartyGoldDelegate partyGoldChange = this.PartyGoldChange;
			if (partyGoldChange == null)
			{
				return;
			}
			partyGoldChange();
		}

		// Token: 0x06002C4D RID: 11341 RVA: 0x000B7FA9 File Offset: 0x000B61A9
		private void SetMoraleChangeAmount(int newAmount)
		{
			this.CurrentData.PartyMoraleChangeAmount = newAmount;
			PartyScreenLogic.PartyMoraleDelegate partyMoraleChange = this.PartyMoraleChange;
			if (partyMoraleChange == null)
			{
				return;
			}
			partyMoraleChange();
		}

		// Token: 0x06002C4E RID: 11342 RVA: 0x000B7FC7 File Offset: 0x000B61C7
		private void SetHorseChangeAmount(int newAmount)
		{
			this.CurrentData.PartyHorseChangeAmount = newAmount;
			PartyScreenLogic.PartyHorseDelegate partyHorseChange = this.PartyHorseChange;
			if (partyHorseChange == null)
			{
				return;
			}
			partyHorseChange();
		}

		// Token: 0x06002C4F RID: 11343 RVA: 0x000B7FE5 File Offset: 0x000B61E5
		private void SetInfluenceChangeAmount(int heroInfluence, int troopInfluence, int prisonerInfluence)
		{
			this.CurrentData.PartyInfluenceChangeAmount = new ValueTuple<int, int, int>(heroInfluence, troopInfluence, prisonerInfluence);
			PartyScreenLogic.PartyInfluenceDelegate partyInfluenceChange = this.PartyInfluenceChange;
			if (partyInfluenceChange == null)
			{
				return;
			}
			partyInfluenceChange();
		}

		// Token: 0x06002C50 RID: 11344 RVA: 0x000B800C File Offset: 0x000B620C
		private void ProcessCommand(PartyScreenLogic.PartyCommand command)
		{
			switch (command.Code)
			{
			case PartyScreenLogic.PartyCommandCode.TransferTroop:
				this.TransferTroop(command, true);
				return;
			case PartyScreenLogic.PartyCommandCode.UpgradeTroop:
				this.UpgradeTroop(command);
				return;
			case PartyScreenLogic.PartyCommandCode.TransferPartyLeaderTroop:
				this.TransferPartyLeaderTroop(command);
				return;
			case PartyScreenLogic.PartyCommandCode.TransferTroopToLeaderSlot:
				this.TransferTroopToLeaderSlot(command);
				return;
			case PartyScreenLogic.PartyCommandCode.ShiftTroop:
				this.ShiftTroop(command);
				return;
			case PartyScreenLogic.PartyCommandCode.RecruitTroop:
				this.RecruitPrisoner(command);
				return;
			case PartyScreenLogic.PartyCommandCode.ExecuteTroop:
				this.ExecuteTroop(command);
				return;
			case PartyScreenLogic.PartyCommandCode.TransferAllTroops:
				this.TransferAllTroops(command);
				return;
			case PartyScreenLogic.PartyCommandCode.SortTroops:
				this.SortTroops(command);
				return;
			default:
				return;
			}
		}

		// Token: 0x06002C51 RID: 11345 RVA: 0x000B8093 File Offset: 0x000B6293
		public void AddCommand(PartyScreenLogic.PartyCommand command)
		{
			this.ProcessCommand(command);
		}

		// Token: 0x06002C52 RID: 11346 RVA: 0x000B809C File Offset: 0x000B629C
		public bool ValidateCommand(PartyScreenLogic.PartyCommand command)
		{
			if (command.Code == PartyScreenLogic.PartyCommandCode.TransferTroop || command.Code == PartyScreenLogic.PartyCommandCode.TransferTroopToLeaderSlot)
			{
				CharacterObject character = command.Character;
				if (character == CharacterObject.PlayerCharacter)
				{
					return false;
				}
				int num;
				if (command.Type == PartyScreenLogic.TroopType.Member)
				{
					num = this.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(character);
					bool flag = num != -1 && this.MemberRosters[(int)command.RosterSide].GetElementNumber(num) >= command.TotalNumber;
					bool flag2 = command.RosterSide != PartyScreenLogic.PartyRosterSide.Left || command.Index != 0;
					return flag && flag2;
				}
				num = this.PrisonerRosters[(int)command.RosterSide].FindIndexOfTroop(character);
				return num != -1 && this.PrisonerRosters[(int)command.RosterSide].GetElementNumber(num) >= command.TotalNumber;
			}
			else if (command.Code == PartyScreenLogic.PartyCommandCode.ShiftTroop)
			{
				CharacterObject character2 = command.Character;
				if (character2 == this.LeftPartyLeader || character2 == this.RightPartyLeader || ((command.RosterSide != PartyScreenLogic.PartyRosterSide.Left || (this.LeftPartyLeader != null && command.Index == 0)) && (command.RosterSide != PartyScreenLogic.PartyRosterSide.Right || (this.RightPartyLeader != null && command.Index == 0))))
				{
					return false;
				}
				int num2;
				if (command.Type == PartyScreenLogic.TroopType.Member)
				{
					num2 = this.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(character2);
					return num2 != -1 && num2 != command.Index;
				}
				num2 = this.PrisonerRosters[(int)command.RosterSide].FindIndexOfTroop(character2);
				return num2 != -1 && num2 != command.Index;
			}
			else
			{
				if (command.Code == PartyScreenLogic.PartyCommandCode.TransferPartyLeaderTroop)
				{
					CharacterObject character3 = command.Character;
					BasicCharacterObject playerTroop = this._game.PlayerTroop;
					return false;
				}
				if (command.Code == PartyScreenLogic.PartyCommandCode.UpgradeTroop)
				{
					CharacterObject character4 = command.Character;
					int num3 = this.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(character4);
					if (num3 == -1 || this.MemberRosters[(int)command.RosterSide].GetElementNumber(num3) < command.TotalNumber || character4.UpgradeTargets.Length == 0)
					{
						return false;
					}
					if (command.UpgradeTarget >= character4.UpgradeTargets.Length)
					{
						MBInformationManager.AddQuickInformation(new TextObject("{=kaQ7DsW3}Character does not have upgrade target.", null), 0, null, null, "");
						return false;
					}
					CharacterObject characterObject = character4.UpgradeTargets[command.UpgradeTarget];
					int upgradeXpCost = character4.GetUpgradeXpCost(PartyBase.MainParty, command.UpgradeTarget);
					int upgradeGoldCost = character4.GetUpgradeGoldCost(PartyBase.MainParty, command.UpgradeTarget);
					if (this.MemberRosters[(int)command.RosterSide].GetElementXp(num3) < upgradeXpCost * command.TotalNumber)
					{
						MBInformationManager.AddQuickInformation(new TextObject("{=m1bIfPf1}Character does not have enough experience for upgrade.", null), 0, null, null, "");
						return false;
					}
					CharacterObject characterObject2 = ((command.RosterSide == PartyScreenLogic.PartyRosterSide.Left) ? this.LeftPartyLeader : this.RightPartyLeader);
					int? num4 = ((characterObject2 != null) ? new int?(characterObject2.HeroObject.Gold) : null) + this.CurrentData.PartyGoldChangeAmount;
					int num5 = upgradeGoldCost * command.TotalNumber;
					if (!((num4.GetValueOrDefault() >= num5) & (num4 != null)))
					{
						MBTextManager.SetTextVariable("VALUE", upgradeGoldCost);
						MBInformationManager.AddQuickInformation(GameTexts.FindText("str_gold_needed_for_upgrade", null), 0, null, null, "");
						return false;
					}
					if (characterObject.UpgradeRequiresItemFromCategory == null)
					{
						return true;
					}
					foreach (ItemRosterElement itemRosterElement in this.RightOwnerParty.ItemRoster)
					{
						if (itemRosterElement.EquipmentElement.Item.ItemCategory == characterObject.UpgradeRequiresItemFromCategory)
						{
							return true;
						}
					}
					MBTextManager.SetTextVariable("REQUIRED_ITEM", characterObject.UpgradeRequiresItemFromCategory.GetName(), false);
					MBInformationManager.AddQuickInformation(GameTexts.FindText("str_item_needed_for_upgrade", null), 0, null, null, "");
					return false;
				}
				else
				{
					if (command.Code == PartyScreenLogic.PartyCommandCode.RecruitTroop)
					{
						return this.IsPrisonerRecruitable(command.Type, command.Character, command.RosterSide);
					}
					if (command.Code == PartyScreenLogic.PartyCommandCode.ExecuteTroop)
					{
						return this.IsExecutable(command.Type, command.Character, command.RosterSide);
					}
					if (command.Code == PartyScreenLogic.PartyCommandCode.TransferAllTroops)
					{
						return this.GetRoster(command.RosterSide, command.Type).Count != 0;
					}
					if (command.Code == PartyScreenLogic.PartyCommandCode.SortTroops)
					{
						return this.GetActiveSortTypeForSide(command.RosterSide) != command.SortType || this.GetIsAscendingSortForSide(command.RosterSide) != command.IsSortAscending;
					}
					throw new MBUnknownTypeException("Unknown command type in ValidateCommand.");
				}
			}
		}

		// Token: 0x06002C53 RID: 11347 RVA: 0x000B8550 File Offset: 0x000B6750
		private void OnReset(bool fromCancel)
		{
			PartyScreenLogic.AfterResetDelegate afterReset = this.AfterReset;
			if (afterReset == null)
			{
				return;
			}
			afterReset(this, fromCancel);
		}

		// Token: 0x06002C54 RID: 11348 RVA: 0x000B8564 File Offset: 0x000B6764
		protected void TransferTroopToLeaderSlot(PartyScreenLogic.PartyCommand command)
		{
			bool flag = false;
			if (this.ValidateCommand(command))
			{
				CharacterObject character = command.Character;
				if (command.Type == PartyScreenLogic.TroopType.Member)
				{
					int index = this.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(character);
					TroopRosterElement elementCopyAtIndex = this.MemberRosters[(int)command.RosterSide].GetElementCopyAtIndex(index);
					int num = command.TotalNumber * (elementCopyAtIndex.Xp / elementCopyAtIndex.Number);
					this.MemberRosters[(int)command.RosterSide].AddToCounts(character, -command.TotalNumber, false, -command.WoundedNumber, 0, true, index);
					this.MemberRosters[(int)(PartyScreenLogic.PartyRosterSide.Right - command.RosterSide)].AddToCounts(character, command.TotalNumber, false, command.WoundedNumber, 0, true, 0);
					if (elementCopyAtIndex.Number != command.TotalNumber)
					{
						this.MemberRosters[(int)command.RosterSide].AddXpToTroop(character, -num);
					}
					this.MemberRosters[(int)(PartyScreenLogic.PartyRosterSide.Right - command.RosterSide)].AddXpToTroop(character, num);
				}
				flag = true;
			}
			if (flag)
			{
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate != null)
				{
					updateDelegate(command);
				}
				PartyScreenLogic.PresentationUpdate update = this.Update;
				if (update == null)
				{
					return;
				}
				update(command);
			}
		}

		// Token: 0x06002C55 RID: 11349 RVA: 0x000B8684 File Offset: 0x000B6884
		protected void TransferTroop(PartyScreenLogic.PartyCommand command, bool invokeUpdate)
		{
			bool flag = false;
			if (this.ValidateCommand(command))
			{
				CharacterObject troop = command.Character;
				if (command.Type == PartyScreenLogic.TroopType.Member)
				{
					TroopRoster troopRoster = this.MemberRosters[(int)command.RosterSide];
					TroopRoster troopRoster2 = this.MemberRosters[(int)(PartyScreenLogic.PartyRosterSide.Right - command.RosterSide)];
					int index = troopRoster.FindIndexOfTroop(troop);
					TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(index);
					int num = ((troop.UpgradeTargets.Length != 0) ? troop.UpgradeTargets.Max((CharacterObject x) => Campaign.Current.Models.PartyTroopUpgradeModel.GetXpCostForUpgrade(PartyBase.MainParty, troop, x)) : 0);
					int num3;
					if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
					{
						int num2 = (elementCopyAtIndex.Number - command.TotalNumber) * num;
						num3 = ((elementCopyAtIndex.Xp >= num2 && num2 >= 0) ? (elementCopyAtIndex.Xp - num2) : 0);
					}
					else
					{
						int num4 = command.TotalNumber * num;
						num3 = ((elementCopyAtIndex.Xp > num4 && num4 >= 0) ? num4 : elementCopyAtIndex.Xp);
						troopRoster.AddXpToTroop(troop, -num3);
					}
					troopRoster.AddToCounts(troop, -command.TotalNumber, false, -command.WoundedNumber, 0, false, -1);
					int num5 = command.Index;
					if (num5 == troopRoster2.Count && troopRoster2.Contains(troop))
					{
						num5 = troopRoster2.Count - 1;
					}
					troopRoster2.AddToCounts(troop, command.TotalNumber, false, command.WoundedNumber, 0, false, num5);
					troopRoster2.AddXpToTroop(troop, num3);
				}
				else
				{
					TroopRoster troopRoster3 = this.PrisonerRosters[(int)command.RosterSide];
					TroopRoster troopRoster4 = this.PrisonerRosters[(int)(PartyScreenLogic.PartyRosterSide.Right - command.RosterSide)];
					int index2 = troopRoster3.FindIndexOfTroop(troop);
					TroopRosterElement elementCopyAtIndex2 = troopRoster3.GetElementCopyAtIndex(index2);
					int conformityNeededToRecruitPrisoner = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityNeededToRecruitPrisoner(elementCopyAtIndex2.Character);
					int num7;
					if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
					{
						this.UpdatePrisonerTransferHistory(troop, -command.TotalNumber);
						int num6 = (elementCopyAtIndex2.Number - command.TotalNumber) * conformityNeededToRecruitPrisoner;
						num7 = ((elementCopyAtIndex2.Xp >= num6 && num6 >= 0) ? (elementCopyAtIndex2.Xp - num6) : 0);
					}
					else
					{
						this.UpdatePrisonerTransferHistory(troop, command.TotalNumber);
						int num8 = command.TotalNumber * conformityNeededToRecruitPrisoner;
						num7 = ((elementCopyAtIndex2.Xp > num8 && num8 >= 0) ? num8 : elementCopyAtIndex2.Xp);
						troopRoster3.AddXpToTroop(troop, -num7);
					}
					troopRoster3.AddToCounts(troop, -command.TotalNumber, false, -command.WoundedNumber, 0, false, -1);
					int num9 = command.Index;
					if (num9 == troopRoster4.Count && troopRoster4.Contains(troop))
					{
						num9 = troopRoster4.Count - 1;
					}
					troopRoster4.AddToCounts(troop, command.TotalNumber, false, command.WoundedNumber, 0, false, num9);
					troopRoster4.AddXpToTroop(troop, num7);
					if (this.CurrentData.RightRecruitableData.ContainsKey(troop))
					{
						this.CurrentData.RightRecruitableData[troop] = MathF.Max(MathF.Min(this.CurrentData.RightRecruitableData[troop], this.PrisonerRosters[1].GetElementNumber(troop)), Campaign.Current.Models.PrisonerRecruitmentCalculationModel.CalculateRecruitableNumber(PartyBase.MainParty, troop));
					}
				}
				flag = true;
			}
			if (flag)
			{
				if (this.PrisonerTransferState == PartyScreenLogic.TransferState.TransferableWithTrade && command.Type == PartyScreenLogic.TroopType.Prisoner)
				{
					int num10 = ((command.RosterSide == PartyScreenLogic.PartyRosterSide.Right) ? 1 : (-1));
					this.SetPartyGoldChangeAmount(this.CurrentData.PartyGoldChangeAmount + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(command.Character, Hero.MainHero) * command.TotalNumber * num10);
				}
				if (this._partyScreenMode == PartyScreenHelper.PartyScreenMode.QuestTroopManage)
				{
					int num11 = ((command.RosterSide == PartyScreenLogic.PartyRosterSide.Right) ? (-1) : 1);
					this.SetPartyGoldChangeAmount(this.CurrentData.PartyGoldChangeAmount + command.Character.TroopWage * command.TotalNumber * this.QuestModeWageDaysMultiplier * num11);
				}
				PartyState activePartyState = PartyScreenHelper.GetActivePartyState();
				if (activePartyState != null && activePartyState.IsDonating)
				{
					Settlement currentSettlement = Hero.MainHero.CurrentSettlement;
					float num12 = 0f;
					float num13 = 0f;
					float num14 = 0f;
					foreach (TroopTradeDifference troopTradeDifference in this.CurrentData.GetTroopTradeDifferencesFromTo(this._initialData, PartyScreenLogic.PartyRosterSide.Left))
					{
						int differenceCount = troopTradeDifference.DifferenceCount;
						if (differenceCount > 0)
						{
							if (!troopTradeDifference.IsPrisoner)
							{
								num13 += (float)differenceCount * Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterTroopDonation(PartyBase.MainParty, troopTradeDifference.Troop, currentSettlement);
							}
							else if (troopTradeDifference.Troop.IsHero)
							{
								num12 += Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterPrisonerDonation(PartyBase.MainParty, troopTradeDifference.Troop, currentSettlement);
							}
							else
							{
								num14 += (float)differenceCount * Campaign.Current.Models.PrisonerDonationModel.CalculateInfluenceGainAfterPrisonerDonation(PartyBase.MainParty, troopTradeDifference.Troop, currentSettlement);
							}
						}
					}
					this.SetInfluenceChangeAmount((int)num12, (int)num13, (int)num14);
				}
				if (invokeUpdate)
				{
					PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
					if (updateDelegate != null)
					{
						updateDelegate(command);
					}
					PartyScreenLogic.PresentationUpdate update = this.Update;
					if (update == null)
					{
						return;
					}
					update(command);
				}
			}
		}

		// Token: 0x06002C56 RID: 11350 RVA: 0x000B8C14 File Offset: 0x000B6E14
		protected void ShiftTroop(PartyScreenLogic.PartyCommand command)
		{
			bool flag = false;
			if (this.ValidateCommand(command))
			{
				CharacterObject character = command.Character;
				if (command.Type == PartyScreenLogic.TroopType.Member)
				{
					int num = this.MemberRosters[(int)command.RosterSide].FindIndexOfTroop(character);
					int targetIndex = ((num < command.Index) ? (command.Index - 1) : command.Index);
					this.MemberRosters[(int)command.RosterSide].ShiftTroopToIndex(num, targetIndex);
				}
				else
				{
					int num2 = this.PrisonerRosters[(int)command.RosterSide].FindIndexOfTroop(character);
					this.PrisonerRosters[(int)command.RosterSide].GetElementCopyAtIndex(num2);
					int targetIndex2 = ((num2 < command.Index) ? (command.Index - 1) : command.Index);
					this.PrisonerRosters[(int)command.RosterSide].ShiftTroopToIndex(num2, targetIndex2);
				}
				flag = true;
			}
			if (flag)
			{
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate != null)
				{
					updateDelegate(command);
				}
				PartyScreenLogic.PresentationUpdate update = this.Update;
				if (update == null)
				{
					return;
				}
				update(command);
			}
		}

		// Token: 0x06002C57 RID: 11351 RVA: 0x000B8D07 File Offset: 0x000B6F07
		protected void TransferPartyLeaderTroop(PartyScreenLogic.PartyCommand command)
		{
			if (this.ValidateCommand(command))
			{
				PartyBase partyBase = ((command.RosterSide == PartyScreenLogic.PartyRosterSide.Left) ? this.LeftOwnerParty : this.RightOwnerParty);
			}
		}

		// Token: 0x06002C58 RID: 11352 RVA: 0x000B8D2C File Offset: 0x000B6F2C
		protected void UpgradeTroop(PartyScreenLogic.PartyCommand command)
		{
			if (this.ValidateCommand(command))
			{
				CharacterObject character = command.Character;
				CharacterObject characterObject = character.UpgradeTargets[command.UpgradeTarget];
				TroopRoster roster = this.GetRoster(command.RosterSide, command.Type);
				int index = roster.FindIndexOfTroop(character);
				int num = character.GetUpgradeXpCost(PartyBase.MainParty, command.UpgradeTarget) * command.TotalNumber;
				roster.SetElementXp(index, roster.GetElementXp(index) - num);
				List<ValueTuple<EquipmentElement, int>> usedHorses = null;
				this.SetPartyGoldChangeAmount(this.CurrentData.PartyGoldChangeAmount - character.GetUpgradeGoldCost(PartyBase.MainParty, command.UpgradeTarget) * command.TotalNumber);
				if (characterObject.UpgradeRequiresItemFromCategory != null)
				{
					usedHorses = this.RemoveItemFromItemRoster(characterObject.UpgradeRequiresItemFromCategory, command.TotalNumber);
				}
				int num2 = 0;
				foreach (TroopRosterElement troopRosterElement in roster.GetTroopRoster())
				{
					if (troopRosterElement.Character == character && command.TotalNumber > troopRosterElement.Number - troopRosterElement.WoundedNumber)
					{
						num2 = command.TotalNumber - (troopRosterElement.Number - troopRosterElement.WoundedNumber);
					}
				}
				roster.AddToCounts(character, -command.TotalNumber, false, -num2, 0, true, -1);
				roster.AddToCounts(characterObject, command.TotalNumber, false, num2, 0, true, command.Index);
				this.AddUpgradeToHistory(character, characterObject, command.TotalNumber);
				this.AddUsedHorsesToHistory(usedHorses);
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate == null)
				{
					return;
				}
				updateDelegate(command);
			}
		}

		// Token: 0x06002C59 RID: 11353 RVA: 0x000B8EBC File Offset: 0x000B70BC
		protected void RecruitPrisoner(PartyScreenLogic.PartyCommand command)
		{
			bool flag = false;
			if (this.ValidateCommand(command))
			{
				CharacterObject character = command.Character;
				TroopRoster troopRoster = this.PrisonerRosters[(int)command.RosterSide];
				int num = MathF.Min(this.CurrentData.RightRecruitableData[character], command.TotalNumber);
				if (num > 0)
				{
					Dictionary<CharacterObject, int> rightRecruitableData = this.CurrentData.RightRecruitableData;
					CharacterObject key = character;
					rightRecruitableData[key] -= num;
					int conformityNeededToRecruitPrisoner = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityNeededToRecruitPrisoner(character);
					troopRoster.AddXpToTroop(character, -conformityNeededToRecruitPrisoner * num);
					troopRoster.AddToCounts(character, -num, false, 0, 0, true, -1);
					this.MemberRosters[(int)command.RosterSide].AddToCounts(command.Character, num, false, 0, 0, true, command.Index);
					this.AddRecruitToHistory(character, num);
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			if (flag)
			{
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate != null)
				{
					updateDelegate(command);
				}
				PartyScreenLogic.PresentationUpdate update = this.Update;
				if (update == null)
				{
					return;
				}
				update(command);
			}
		}

		// Token: 0x06002C5A RID: 11354 RVA: 0x000B8FC0 File Offset: 0x000B71C0
		protected void ExecuteTroop(PartyScreenLogic.PartyCommand command)
		{
			bool flag = false;
			if (this.ValidateCommand(command))
			{
				CharacterObject character = command.Character;
				this.PrisonerRosters[(int)command.RosterSide].AddToCounts(character, -1, false, 0, 0, true, -1);
				KillCharacterAction.ApplyByExecution(character.HeroObject, Hero.MainHero, true, false);
				flag = true;
			}
			if (flag)
			{
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate != null)
				{
					updateDelegate(command);
				}
				PartyScreenLogic.PresentationUpdate update = this.Update;
				if (update != null)
				{
					update(command);
				}
				if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Left)
				{
					this._initialData.LeftPrisonerRoster.AddToCounts(command.Character, -1, false, 0, 0, true, -1);
					return;
				}
				if (PartyScreenLogic.PartyRosterSide.Right == command.RosterSide)
				{
					this._initialData.RightPrisonerRoster.AddToCounts(command.Character, -1, false, 0, 0, true, -1);
				}
			}
		}

		// Token: 0x06002C5B RID: 11355 RVA: 0x000B9080 File Offset: 0x000B7280
		protected void TransferAllTroops(PartyScreenLogic.PartyCommand command)
		{
			if (this.ValidateCommand(command))
			{
				PartyScreenLogic.PartyRosterSide side = PartyScreenLogic.PartyRosterSide.Right - command.RosterSide;
				TroopRoster roster = this.GetRoster(command.RosterSide, command.Type);
				List<TroopRosterElement> listFromRoster = this.GetListFromRoster(roster);
				int num = -1;
				if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
				{
					if (command.Type == PartyScreenLogic.TroopType.Prisoner)
					{
						num = this.LeftPartyPrisonersSizeLimit - this.PrisonerRosters[0].TotalManCount;
					}
					else
					{
						num = this.LeftPartyMembersSizeLimit - this.MemberRosters[0].TotalManCount;
					}
				}
				else if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Left)
				{
					if (command.Type == PartyScreenLogic.TroopType.Prisoner)
					{
						num = this.RightPartyPrisonersSizeLimit - this.PrisonerRosters[1].TotalManCount;
					}
					else
					{
						num = this.RightPartyMembersSizeLimit - this.MemberRosters[1].TotalManCount;
					}
				}
				if (num <= 0)
				{
					num = listFromRoster.Sum((TroopRosterElement x) => x.Number);
				}
				IEnumerable<string> source = ((command.Type == PartyScreenLogic.TroopType.Member) ? Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyTroopLocks() : Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks());
				int num2 = 0;
				while (num2 < listFromRoster.Count && num > 0)
				{
					TroopRosterElement troopRosterElement = listFromRoster[num2];
					if ((command.RosterSide != PartyScreenLogic.PartyRosterSide.Right || !source.Contains(troopRosterElement.Character.StringId)) && this.IsTroopTransferable(command.Type, troopRosterElement.Character, (int)command.RosterSide))
					{
						PartyScreenLogic.PartyCommand partyCommand = new PartyScreenLogic.PartyCommand();
						int num3 = MBMath.ClampInt(troopRosterElement.Number, 0, num);
						partyCommand.FillForTransferTroop(command.RosterSide, command.Type, troopRosterElement.Character, num3, troopRosterElement.WoundedNumber, -1);
						this.TransferTroop(partyCommand, false);
						num -= num3;
					}
					num2++;
				}
				PartyScreenLogic.TroopSortType activeSortTypeForSide = this.GetActiveSortTypeForSide(side);
				if (activeSortTypeForSide != PartyScreenLogic.TroopSortType.Custom)
				{
					TroopRoster roster2 = this.GetRoster(side, command.Type);
					this.SortRoster(roster2, activeSortTypeForSide);
				}
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate != null)
				{
					updateDelegate(command);
				}
				PartyScreenLogic.PresentationUpdate update = this.Update;
				if (update == null)
				{
					return;
				}
				update(command);
			}
		}

		// Token: 0x06002C5C RID: 11356 RVA: 0x000B928C File Offset: 0x000B748C
		protected void SortTroops(PartyScreenLogic.PartyCommand command)
		{
			if (this.ValidateCommand(command))
			{
				this.SetActiveSortTypeForSide(command.RosterSide, command.SortType);
				this.SetIsAscendingForSide(command.RosterSide, command.IsSortAscending);
				this.UpdateComparersAscendingOrder(command.IsSortAscending);
				if (command.SortType != PartyScreenLogic.TroopSortType.Custom)
				{
					TroopRoster roster = this.GetRoster(command.RosterSide, PartyScreenLogic.TroopType.Member);
					TroopRoster roster2 = this.GetRoster(command.RosterSide, PartyScreenLogic.TroopType.Prisoner);
					this.SortRoster(roster, command.SortType);
					this.SortRoster(roster2, command.SortType);
				}
				PartyScreenLogic.PresentationUpdate updateDelegate = this.UpdateDelegate;
				if (updateDelegate != null)
				{
					updateDelegate(command);
				}
				PartyScreenLogic.PresentationUpdate update = this.Update;
				if (update == null)
				{
					return;
				}
				update(command);
			}
		}

		// Token: 0x06002C5D RID: 11357 RVA: 0x000B9338 File Offset: 0x000B7538
		public int GetIndexToInsertTroop(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, TroopRosterElement troop)
		{
			PartyScreenLogic.TroopSortType activeSortTypeForSide = this.GetActiveSortTypeForSide(side);
			if (activeSortTypeForSide != PartyScreenLogic.TroopSortType.Custom)
			{
				return -1;
			}
			PartyScreenLogic.TroopComparer comparer = this.GetComparer(activeSortTypeForSide);
			TroopRoster roster = this.GetRoster(side, type);
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
				if (!elementCopyAtIndex.Character.IsHero)
				{
					if (elementCopyAtIndex.Character.StringId == troop.Character.StringId)
					{
						return -1;
					}
					if (comparer.Compare(elementCopyAtIndex, troop) < 0)
					{
						return i;
					}
				}
			}
			return -1;
		}

		// Token: 0x06002C5E RID: 11358 RVA: 0x000B93BA File Offset: 0x000B75BA
		public PartyScreenLogic.TroopSortType GetActiveSortTypeForSide(PartyScreenLogic.PartyRosterSide side)
		{
			if (side == PartyScreenLogic.PartyRosterSide.Left)
			{
				return this.ActiveOtherPartySortType;
			}
			if (side == PartyScreenLogic.PartyRosterSide.Right)
			{
				return this.ActiveMainPartySortType;
			}
			return PartyScreenLogic.TroopSortType.Invalid;
		}

		// Token: 0x06002C5F RID: 11359 RVA: 0x000B93D2 File Offset: 0x000B75D2
		private void SetActiveSortTypeForSide(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopSortType sortType)
		{
			if (side == PartyScreenLogic.PartyRosterSide.Left)
			{
				this.ActiveOtherPartySortType = sortType;
				return;
			}
			if (side == PartyScreenLogic.PartyRosterSide.Right)
			{
				this.ActiveMainPartySortType = sortType;
			}
		}

		// Token: 0x06002C60 RID: 11360 RVA: 0x000B93EA File Offset: 0x000B75EA
		public bool GetIsAscendingSortForSide(PartyScreenLogic.PartyRosterSide side)
		{
			if (side == PartyScreenLogic.PartyRosterSide.Left)
			{
				return this.IsOtherPartySortAscending;
			}
			return side == PartyScreenLogic.PartyRosterSide.Right && this.IsMainPartySortAscending;
		}

		// Token: 0x06002C61 RID: 11361 RVA: 0x000B9402 File Offset: 0x000B7602
		private void SetIsAscendingForSide(PartyScreenLogic.PartyRosterSide side, bool isAscending)
		{
			if (side == PartyScreenLogic.PartyRosterSide.Left)
			{
				this.IsOtherPartySortAscending = isAscending;
				return;
			}
			if (side == PartyScreenLogic.PartyRosterSide.Right)
			{
				this.IsMainPartySortAscending = isAscending;
			}
		}

		// Token: 0x06002C62 RID: 11362 RVA: 0x000B941C File Offset: 0x000B761C
		private List<TroopRosterElement> GetListFromRoster(TroopRoster roster)
		{
			List<TroopRosterElement> list = new List<TroopRosterElement>();
			for (int i = 0; i < roster.Count; i++)
			{
				list.Add(roster.GetElementCopyAtIndex(i));
			}
			return list;
		}

		// Token: 0x06002C63 RID: 11363 RVA: 0x000B9450 File Offset: 0x000B7650
		private void SyncRosterWithList(TroopRoster roster, List<TroopRosterElement> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				TroopRosterElement troopRosterElement = list[i];
				int firstIndex = roster.FindIndexOfTroop(troopRosterElement.Character);
				roster.SwapTroopsAtIndices(firstIndex, i);
			}
		}

		// Token: 0x06002C64 RID: 11364 RVA: 0x000B948C File Offset: 0x000B768C
		[Conditional("DEBUG")]
		private void EnsureRosterIsSyncedWithList(TroopRoster roster, List<TroopRosterElement> list)
		{
			if (roster.Count != list.Count)
			{
				Debug.FailedAssert("Roster count is not synced with the list count", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "EnsureRosterIsSyncedWithList", 1079);
				return;
			}
			for (int i = 0; i < roster.Count; i++)
			{
				if (roster.GetCharacterAtIndex(i).StringId != list[i].Character.StringId)
				{
					Debug.FailedAssert("Roster is not synced with the list at index: " + i, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "EnsureRosterIsSyncedWithList", 1089);
					return;
				}
			}
		}

		// Token: 0x06002C65 RID: 11365 RVA: 0x000B951C File Offset: 0x000B771C
		private void SortRoster(TroopRoster originalRoster, PartyScreenLogic.TroopSortType sortType)
		{
			PartyScreenLogic.TroopComparer comparer = this._defaultComparers[sortType];
			if (!this.IsRosterOrdered(originalRoster, comparer))
			{
				List<TroopRosterElement> listFromRoster = this.GetListFromRoster(originalRoster);
				listFromRoster.Sort(this._defaultComparers[sortType]);
				this.SyncRosterWithList(originalRoster, listFromRoster);
			}
		}

		// Token: 0x06002C66 RID: 11366 RVA: 0x000B9564 File Offset: 0x000B7764
		private bool IsRosterOrdered(TroopRoster roster, PartyScreenLogic.TroopComparer comparer)
		{
			for (int i = 1; i < roster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i - 1);
				TroopRosterElement elementCopyAtIndex2 = roster.GetElementCopyAtIndex(i);
				if (comparer.Compare(elementCopyAtIndex, elementCopyAtIndex2) >= 1)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002C67 RID: 11367 RVA: 0x000B95A4 File Offset: 0x000B77A4
		public bool IsDoneActive()
		{
			object obj = Hero.MainHero.Gold < -this.CurrentData.PartyGoldChangeAmount && this.CurrentData.PartyGoldChangeAmount < 0;
			PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = this.PartyPresentationDoneButtonConditionDelegate;
			Tuple<bool, TextObject> tuple = ((partyPresentationDoneButtonConditionDelegate != null) ? partyPresentationDoneButtonConditionDelegate(this.MemberRosters[0], this.PrisonerRosters[0], this.MemberRosters[1], this.PrisonerRosters[1], this.LeftPartyMembersSizeLimit, 0) : null);
			bool flag = this.PartyPresentationDoneButtonConditionDelegate == null || (tuple != null && tuple.Item1);
			this.DoneReasonString = null;
			object obj2 = obj;
			if (obj2 != null)
			{
				this.DoneReasonString = GameTexts.FindText("str_inventory_popup_player_not_enough_gold", null).ToString();
			}
			else
			{
				string text;
				if (tuple == null)
				{
					text = null;
				}
				else
				{
					TextObject item = tuple.Item2;
					text = ((item != null) ? item.ToString() : null);
				}
				this.DoneReasonString = text ?? string.Empty;
			}
			return obj2 == 0 && flag;
		}

		// Token: 0x06002C68 RID: 11368 RVA: 0x000B967A File Offset: 0x000B787A
		public bool IsCancelActive()
		{
			return this.PartyPresentationCancelButtonActivateDelegate == null || this.PartyPresentationCancelButtonActivateDelegate();
		}

		// Token: 0x06002C69 RID: 11369 RVA: 0x000B9694 File Offset: 0x000B7894
		public bool DoneLogic(bool isForced)
		{
			if (Hero.MainHero.Gold < -this.CurrentData.PartyGoldChangeAmount && this.CurrentData.PartyGoldChangeAmount < 0)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_inventory_popup_player_not_enough_gold", null), 0, null, null, "");
				return false;
			}
			FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster(4);
			FlattenedTroopRoster flattenedTroopRoster2 = new FlattenedTroopRoster(4);
			foreach (Tuple<CharacterObject, int> tuple in this.CurrentData.TransferredPrisonersHistory)
			{
				int number = MathF.Abs(tuple.Item2);
				if (tuple.Item2 < 0)
				{
					flattenedTroopRoster.Add(tuple.Item1, number, 0);
				}
				else if (tuple.Item2 > 0)
				{
					flattenedTroopRoster2.Add(tuple.Item1, number, 0);
				}
			}
			if (Settlement.CurrentSettlement != null && !flattenedTroopRoster2.IsEmpty<FlattenedTroopRosterElement>())
			{
				CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(Settlement.CurrentSettlement, flattenedTroopRoster2, null, true);
			}
			bool flag = this.PartyPresentationDoneButtonDelegate(this.MemberRosters[0], this.PrisonerRosters[0], this.MemberRosters[1], this.PrisonerRosters[1], flattenedTroopRoster2, flattenedTroopRoster, isForced, this.LeftOwnerParty, this.RightOwnerParty);
			if (flag)
			{
				if (!this.DoNotApplyGoldTransactions)
				{
					GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.CurrentData.PartyGoldChangeAmount, false);
				}
				if (this.CurrentData.PartyInfluenceChangeAmount.Item2 != 0)
				{
					GainKingdomInfluenceAction.ApplyForLeavingTroopToGarrison(Hero.MainHero, (float)this.CurrentData.PartyInfluenceChangeAmount.Item2);
				}
				this.FireCampaignRelatedEvents();
				this.SetPartyGoldChangeAmount(0);
				this.SetHorseChangeAmount(0);
				this.SetInfluenceChangeAmount(0, 0, 0);
				this.SetMoraleChangeAmount(0);
				this.CurrentData.UpgradedTroopsHistory = new List<Tuple<CharacterObject, CharacterObject, int>>();
				this.CurrentData.TransferredPrisonersHistory = new List<Tuple<CharacterObject, int>>();
				this.CurrentData.RecruitedPrisonersHistory = new List<Tuple<CharacterObject, int>>();
				this.CurrentData.UsedUpgradeHorsesHistory = new List<Tuple<EquipmentElement, int>>();
				this._initialData.CopyFromScreenData(this.CurrentData);
			}
			return flag;
		}

		// Token: 0x06002C6A RID: 11370 RVA: 0x000B9894 File Offset: 0x000B7A94
		public void OnPartyScreenClosed(bool fromCancel)
		{
			if (fromCancel)
			{
				PartyPresentationCancelButtonDelegate partyPresentationCancelButtonDelegate = this.PartyPresentationCancelButtonDelegate;
				if (partyPresentationCancelButtonDelegate != null)
				{
					partyPresentationCancelButtonDelegate();
				}
			}
			PartyScreenClosedDelegate partyScreenClosedEvent = this.PartyScreenClosedEvent;
			if (partyScreenClosedEvent == null)
			{
				return;
			}
			partyScreenClosedEvent(this.LeftOwnerParty, this.MemberRosters[0], this.PrisonerRosters[0], this.RightOwnerParty, this.MemberRosters[1], this.PrisonerRosters[1], fromCancel);
		}

		// Token: 0x06002C6B RID: 11371 RVA: 0x000B98F4 File Offset: 0x000B7AF4
		private void UpdateComparersAscendingOrder(bool isAscending)
		{
			foreach (KeyValuePair<PartyScreenLogic.TroopSortType, PartyScreenLogic.TroopComparer> keyValuePair in this._defaultComparers)
			{
				keyValuePair.Value.SetIsAscending(isAscending);
			}
		}

		// Token: 0x06002C6C RID: 11372 RVA: 0x000B9950 File Offset: 0x000B7B50
		private void FireCampaignRelatedEvents()
		{
			foreach (Tuple<CharacterObject, CharacterObject, int> tuple in this.CurrentData.UpgradedTroopsHistory)
			{
				CampaignEventDispatcher.Instance.OnPlayerUpgradedTroops(tuple.Item1, tuple.Item2, tuple.Item3);
			}
			FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster(4);
			foreach (Tuple<CharacterObject, int> tuple2 in this.CurrentData.RecruitedPrisonersHistory)
			{
				flattenedTroopRoster.Add(tuple2.Item1, tuple2.Item2, 0);
			}
			if (!flattenedTroopRoster.IsEmpty<FlattenedTroopRosterElement>())
			{
				CampaignEventDispatcher.Instance.OnMainPartyPrisonerRecruited(flattenedTroopRoster);
			}
		}

		// Token: 0x06002C6D RID: 11373 RVA: 0x000B9A30 File Offset: 0x000B7C30
		public bool IsTroopTransferable(PartyScreenLogic.TroopType troopType, CharacterObject character, int side)
		{
			return this.IsTroopRosterTransferable(troopType) && !character.IsNotTransferableInPartyScreen && character != CharacterObject.PlayerCharacter && (this.IsTroopTransferableDelegate == null || this.IsTroopTransferableDelegate(character, troopType, (PartyScreenLogic.PartyRosterSide)side, this.LeftOwnerParty));
		}

		// Token: 0x06002C6E RID: 11374 RVA: 0x000B9A6C File Offset: 0x000B7C6C
		public bool IsTroopRosterTransferable(PartyScreenLogic.TroopType troopType)
		{
			if (troopType == PartyScreenLogic.TroopType.Prisoner)
			{
				return this.PrisonerTransferState == PartyScreenLogic.TransferState.Transferable || this.PrisonerTransferState == PartyScreenLogic.TransferState.TransferableWithTrade;
			}
			return troopType == PartyScreenLogic.TroopType.Member && (this.MemberTransferState == PartyScreenLogic.TransferState.Transferable || this.MemberTransferState == PartyScreenLogic.TransferState.TransferableWithTrade);
		}

		// Token: 0x06002C6F RID: 11375 RVA: 0x000B9AA1 File Offset: 0x000B7CA1
		public bool IsPrisonerRecruitable(PartyScreenLogic.TroopType troopType, CharacterObject character, PartyScreenLogic.PartyRosterSide side)
		{
			return side == PartyScreenLogic.PartyRosterSide.Right && troopType == PartyScreenLogic.TroopType.Prisoner && !character.IsHero && this.CurrentData.RightRecruitableData.ContainsKey(character) && this.CurrentData.RightRecruitableData[character] > 0;
		}

		// Token: 0x06002C70 RID: 11376 RVA: 0x000B9AE0 File Offset: 0x000B7CE0
		public string GetRecruitableReasonString(CharacterObject character, bool isRecruitable, int troopCount, out bool showStackModifierText)
		{
			showStackModifierText = false;
			if (isRecruitable)
			{
				showStackModifierText = true;
				if (this.RightOwnerParty.PartySizeLimit <= this.MemberRosters[1].TotalManCount)
				{
					return GameTexts.FindText("str_recruit_party_size_limit", null).ToString();
				}
				return GameTexts.FindText("str_recruit_prisoner", null).ToString();
			}
			else
			{
				if (character.IsHero)
				{
					return GameTexts.FindText("str_cannot_recruit_hero", null).ToString();
				}
				return GameTexts.FindText("str_cannot_recruit_prisoner", null).ToString();
			}
		}

		// Token: 0x06002C71 RID: 11377 RVA: 0x000B9B60 File Offset: 0x000B7D60
		public bool IsExecutable(PartyScreenLogic.TroopType troopType, CharacterObject character, PartyScreenLogic.PartyRosterSide side)
		{
			return troopType == PartyScreenLogic.TroopType.Prisoner && side == PartyScreenLogic.PartyRosterSide.Right && character.IsHero && character.HeroObject.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && PlayerEncounter.Current == null && FaceGen.GetMaturityTypeWithAge(character.Age) > BodyMeshMaturityType.Tween;
		}

		// Token: 0x06002C72 RID: 11378 RVA: 0x000B9BB6 File Offset: 0x000B7DB6
		public string GetExecutableReasonString(CharacterObject character, bool isExecutable)
		{
			if (isExecutable)
			{
				return GameTexts.FindText("str_execute_prisoner", null).ToString();
			}
			if (!character.IsHero)
			{
				return GameTexts.FindText("str_cannot_execute_nonhero", null).ToString();
			}
			return GameTexts.FindText("str_cannot_execute_hero", null).ToString();
		}

		// Token: 0x06002C73 RID: 11379 RVA: 0x000B9BF8 File Offset: 0x000B7DF8
		public int GetCurrentQuestCurrentCount(bool includePrisoners, bool includeMembers)
		{
			int num = 0;
			if (includeMembers)
			{
				num += this.MemberRosters[0].Sum((TroopRosterElement item) => item.Number - item.WoundedNumber);
			}
			if (includePrisoners)
			{
				num += this.PrisonerRosters[0].Sum((TroopRosterElement item) => item.Number - item.WoundedNumber);
			}
			return num;
		}

		// Token: 0x06002C74 RID: 11380 RVA: 0x000B9C6C File Offset: 0x000B7E6C
		public int GetCurrentQuestRequiredCount()
		{
			return this.LeftPartyMembersSizeLimit;
		}

		// Token: 0x06002C75 RID: 11381 RVA: 0x000B9C74 File Offset: 0x000B7E74
		private static bool DefaultDoneHandler(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
		{
			return true;
		}

		// Token: 0x06002C76 RID: 11382 RVA: 0x000B9C78 File Offset: 0x000B7E78
		private void AddUpgradeToHistory(CharacterObject fromTroop, CharacterObject toTroop, int num)
		{
			Tuple<CharacterObject, CharacterObject, int> tuple = this.CurrentData.UpgradedTroopsHistory.Find((Tuple<CharacterObject, CharacterObject, int> t) => t.Item1 == fromTroop && t.Item2 == toTroop);
			if (tuple != null)
			{
				int item = tuple.Item3;
				this.CurrentData.UpgradedTroopsHistory.Remove(tuple);
				this.CurrentData.UpgradedTroopsHistory.Add(new Tuple<CharacterObject, CharacterObject, int>(fromTroop, toTroop, num + item));
				return;
			}
			this.CurrentData.UpgradedTroopsHistory.Add(new Tuple<CharacterObject, CharacterObject, int>(fromTroop, toTroop, num));
		}

		// Token: 0x06002C77 RID: 11383 RVA: 0x000B9D1C File Offset: 0x000B7F1C
		private void AddUsedHorsesToHistory(List<ValueTuple<EquipmentElement, int>> usedHorses)
		{
			if (usedHorses != null)
			{
				using (List<ValueTuple<EquipmentElement, int>>.Enumerator enumerator = usedHorses.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ValueTuple<EquipmentElement, int> usedHorse = enumerator.Current;
						Tuple<EquipmentElement, int> tuple = this.CurrentData.UsedUpgradeHorsesHistory.Find((Tuple<EquipmentElement, int> t) => t.Equals(usedHorse.Item1));
						if (tuple != null)
						{
							int item = tuple.Item2;
							this.CurrentData.UsedUpgradeHorsesHistory.Remove(tuple);
							this.CurrentData.UsedUpgradeHorsesHistory.Add(new Tuple<EquipmentElement, int>(usedHorse.Item1, item + usedHorse.Item2));
						}
						else
						{
							this.CurrentData.UsedUpgradeHorsesHistory.Add(new Tuple<EquipmentElement, int>(usedHorse.Item1, usedHorse.Item2));
						}
					}
				}
				PartyScreenData currentData = this.CurrentData;
				this.SetHorseChangeAmount(currentData.PartyHorseChangeAmount += usedHorses.Sum((ValueTuple<EquipmentElement, int> t) => t.Item2));
			}
		}

		// Token: 0x06002C78 RID: 11384 RVA: 0x000B9E50 File Offset: 0x000B8050
		private void UpdatePrisonerTransferHistory(CharacterObject troop, int amount)
		{
			Tuple<CharacterObject, int> tuple = this.CurrentData.TransferredPrisonersHistory.Find((Tuple<CharacterObject, int> t) => t.Item1 == troop);
			if (tuple != null)
			{
				int item = tuple.Item2;
				this.CurrentData.TransferredPrisonersHistory.Remove(tuple);
				this.CurrentData.TransferredPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount + item));
				return;
			}
			this.CurrentData.TransferredPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount));
		}

		// Token: 0x06002C79 RID: 11385 RVA: 0x000B9EE0 File Offset: 0x000B80E0
		private void AddRecruitToHistory(CharacterObject troop, int amount)
		{
			Tuple<CharacterObject, int> tuple = this.CurrentData.RecruitedPrisonersHistory.Find((Tuple<CharacterObject, int> t) => t.Item1 == troop);
			if (tuple != null)
			{
				int item = tuple.Item2;
				this.CurrentData.RecruitedPrisonersHistory.Remove(tuple);
				this.CurrentData.RecruitedPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount + item));
			}
			else
			{
				this.CurrentData.RecruitedPrisonersHistory.Add(new Tuple<CharacterObject, int>(troop, amount));
			}
			int prisonerRecruitmentMoraleEffect = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetPrisonerRecruitmentMoraleEffect(this.RightOwnerParty, troop, amount);
			this.SetMoraleChangeAmount(this.CurrentData.PartyMoraleChangeAmount + prisonerRecruitmentMoraleEffect);
		}

		// Token: 0x06002C7A RID: 11386 RVA: 0x000B9FA4 File Offset: 0x000B81A4
		private string GetItemLockStringID(EquipmentElement equipmentElement)
		{
			return equipmentElement.Item.StringId + ((equipmentElement.ItemModifier != null) ? equipmentElement.ItemModifier.StringId : "");
		}

		// Token: 0x06002C7B RID: 11387 RVA: 0x000B9FD4 File Offset: 0x000B81D4
		private List<ValueTuple<EquipmentElement, int>> RemoveItemFromItemRoster(ItemCategory itemCategory, int numOfItemsLeftToRemove = 1)
		{
			List<ValueTuple<EquipmentElement, int>> list = new List<ValueTuple<EquipmentElement, int>>();
			IEnumerable<string> lockedItems = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetInventoryLocks();
			foreach (ItemRosterElement itemRosterElement in from x in this.RightOwnerParty.ItemRoster.Where(delegate(ItemRosterElement x)
				{
					ItemObject item = x.EquipmentElement.Item;
					return ((item != null) ? item.ItemCategory : null) == itemCategory;
				})
				orderby x.EquipmentElement.Item.Value
				orderby lockedItems.Contains(this.GetItemLockStringID(x.EquipmentElement))
				select x)
			{
				int num = MathF.Min(numOfItemsLeftToRemove, itemRosterElement.Amount);
				this.RightOwnerParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -num);
				numOfItemsLeftToRemove -= num;
				list.Add(new ValueTuple<EquipmentElement, int>(itemRosterElement.EquipmentElement, num));
				if (numOfItemsLeftToRemove <= 0)
				{
					break;
				}
			}
			if (numOfItemsLeftToRemove > 0)
			{
				Debug.FailedAssert("Couldn't find enough upgrade req items in the inventory.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\PartyScreenLogic.cs", "RemoveItemFromItemRoster", 1507);
			}
			return list;
		}

		// Token: 0x06002C7C RID: 11388 RVA: 0x000BA0FC File Offset: 0x000B82FC
		public void Reset(bool fromCancel)
		{
			this.ResetLogic(fromCancel);
		}

		// Token: 0x06002C7D RID: 11389 RVA: 0x000BA105 File Offset: 0x000B8305
		private void ResetLogic(bool fromCancel)
		{
			if (this.CurrentData != this._initialData)
			{
				this.CurrentData.ResetUsing(this._initialData);
				PartyScreenLogic.AfterResetDelegate afterReset = this.AfterReset;
				if (afterReset == null)
				{
					return;
				}
				afterReset(this, fromCancel);
			}
		}

		// Token: 0x06002C7E RID: 11390 RVA: 0x000BA13D File Offset: 0x000B833D
		public void SavePartyScreenData()
		{
			this._savedData = new PartyScreenData();
			this._savedData.InitializeCopyFrom(this.CurrentData.RightParty, this.CurrentData.LeftParty);
			this._savedData.CopyFromScreenData(this.CurrentData);
		}

		// Token: 0x06002C7F RID: 11391 RVA: 0x000BA17C File Offset: 0x000B837C
		public void ResetToLastSavedPartyScreenData(bool fromCancel)
		{
			if (this.CurrentData != this._savedData)
			{
				this.CurrentData.ResetUsing(this._savedData);
				PartyScreenLogic.AfterResetDelegate afterReset = this.AfterReset;
				if (afterReset == null)
				{
					return;
				}
				afterReset(this, fromCancel);
			}
		}

		// Token: 0x06002C80 RID: 11392 RVA: 0x000BA1B4 File Offset: 0x000B83B4
		public void RemoveZeroCounts()
		{
			for (int i = 0; i < this.MemberRosters.Length; i++)
			{
				this.MemberRosters[i].RemoveZeroCounts();
			}
			for (int j = 0; j < this.PrisonerRosters.Length; j++)
			{
				this.PrisonerRosters[j].RemoveZeroCounts();
			}
		}

		// Token: 0x06002C81 RID: 11393 RVA: 0x000BA201 File Offset: 0x000B8401
		public int GetTroopRecruitableAmount(CharacterObject troop)
		{
			if (!this.CurrentData.RightRecruitableData.ContainsKey(troop))
			{
				return 0;
			}
			return this.CurrentData.RightRecruitableData[troop];
		}

		// Token: 0x06002C82 RID: 11394 RVA: 0x000BA229 File Offset: 0x000B8429
		public TroopRoster GetRoster(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType troopType)
		{
			if (troopType == PartyScreenLogic.TroopType.Member)
			{
				return this.MemberRosters[(int)side];
			}
			if (troopType == PartyScreenLogic.TroopType.Prisoner)
			{
				return this.PrisonerRosters[(int)side];
			}
			return null;
		}

		// Token: 0x06002C83 RID: 11395 RVA: 0x000BA246 File Offset: 0x000B8446
		internal void OnDoneEvent(List<TroopTradeDifference> freshlySellList)
		{
		}

		// Token: 0x06002C84 RID: 11396 RVA: 0x000BA248 File Offset: 0x000B8448
		public bool IsThereAnyChanges()
		{
			return this._initialData.IsThereAnyTroopTradeDifferenceBetween(this.CurrentData);
		}

		// Token: 0x06002C85 RID: 11397 RVA: 0x000BA25C File Offset: 0x000B845C
		public bool HaveRightSideGainedTroops()
		{
			foreach (TroopTradeDifference troopTradeDifference in this._initialData.GetTroopTradeDifferencesFromTo(this.CurrentData, PartyScreenLogic.PartyRosterSide.None))
			{
				if (!troopTradeDifference.IsPrisoner && troopTradeDifference.FromCount < troopTradeDifference.ToCount)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002C86 RID: 11398 RVA: 0x000BA2D8 File Offset: 0x000B84D8
		public PartyScreenLogic.TroopComparer GetComparer(PartyScreenLogic.TroopSortType sortType)
		{
			return this._defaultComparers[sortType];
		}

		// Token: 0x04000CA6 RID: 3238
		public PartyPresentationDoneButtonDelegate PartyPresentationDoneButtonDelegate;

		// Token: 0x04000CA7 RID: 3239
		public PartyPresentationDoneButtonConditionDelegate PartyPresentationDoneButtonConditionDelegate;

		// Token: 0x04000CA8 RID: 3240
		public PartyPresentationCancelButtonActivateDelegate PartyPresentationCancelButtonActivateDelegate;

		// Token: 0x04000CA9 RID: 3241
		public PartyPresentationCancelButtonDelegate PartyPresentationCancelButtonDelegate;

		// Token: 0x04000CAA RID: 3242
		public PartyScreenLogic.PresentationUpdate UpdateDelegate;

		// Token: 0x04000CAB RID: 3243
		public IsTroopTransferableDelegate IsTroopTransferableDelegate;

		// Token: 0x04000CAC RID: 3244
		public CanTalkToHeroDelegate CanTalkToHeroDelegate;

		// Token: 0x04000CB4 RID: 3252
		private PartyScreenLogic.TroopSortType _activeOtherPartySortType;

		// Token: 0x04000CB5 RID: 3253
		private PartyScreenLogic.TroopSortType _activeMainPartySortType;

		// Token: 0x04000CB6 RID: 3254
		private bool _isOtherPartySortAscending;

		// Token: 0x04000CB7 RID: 3255
		private bool _isMainPartySortAscending;

		// Token: 0x04000CCD RID: 3277
		public TroopRoster[] MemberRosters;

		// Token: 0x04000CCE RID: 3278
		public TroopRoster[] PrisonerRosters;

		// Token: 0x04000CCF RID: 3279
		public bool IsConsumablesChanges;

		// Token: 0x04000CD0 RID: 3280
		private PartyScreenHelper.PartyScreenMode _partyScreenMode;

		// Token: 0x04000CD1 RID: 3281
		private readonly Dictionary<PartyScreenLogic.TroopSortType, PartyScreenLogic.TroopComparer> _defaultComparers;

		// Token: 0x04000CD2 RID: 3282
		private readonly PartyScreenData _initialData;

		// Token: 0x04000CD3 RID: 3283
		private PartyScreenData _savedData;

		// Token: 0x04000CD4 RID: 3284
		private Game _game;

		// Token: 0x02000697 RID: 1687
		public enum TroopSortType
		{
			// Token: 0x04001A77 RID: 6775
			Invalid = -1,
			// Token: 0x04001A78 RID: 6776
			Custom,
			// Token: 0x04001A79 RID: 6777
			Type,
			// Token: 0x04001A7A RID: 6778
			Name,
			// Token: 0x04001A7B RID: 6779
			Count,
			// Token: 0x04001A7C RID: 6780
			Tier
		}

		// Token: 0x02000698 RID: 1688
		public enum PartyRosterSide : byte
		{
			// Token: 0x04001A7E RID: 6782
			None = 99,
			// Token: 0x04001A7F RID: 6783
			Right = 1,
			// Token: 0x04001A80 RID: 6784
			Left = 0
		}

		// Token: 0x02000699 RID: 1689
		[Flags]
		public enum TroopType
		{
			// Token: 0x04001A82 RID: 6786
			Member = 1,
			// Token: 0x04001A83 RID: 6787
			Prisoner = 2,
			// Token: 0x04001A84 RID: 6788
			None = 3
		}

		// Token: 0x0200069A RID: 1690
		public enum PartyCommandCode
		{
			// Token: 0x04001A86 RID: 6790
			TransferTroop,
			// Token: 0x04001A87 RID: 6791
			UpgradeTroop,
			// Token: 0x04001A88 RID: 6792
			TransferPartyLeaderTroop,
			// Token: 0x04001A89 RID: 6793
			TransferTroopToLeaderSlot,
			// Token: 0x04001A8A RID: 6794
			ShiftTroop,
			// Token: 0x04001A8B RID: 6795
			RecruitTroop,
			// Token: 0x04001A8C RID: 6796
			ExecuteTroop,
			// Token: 0x04001A8D RID: 6797
			TransferAllTroops,
			// Token: 0x04001A8E RID: 6798
			SortTroops
		}

		// Token: 0x0200069B RID: 1691
		public enum TransferState
		{
			// Token: 0x04001A90 RID: 6800
			NotTransferable,
			// Token: 0x04001A91 RID: 6801
			Transferable,
			// Token: 0x04001A92 RID: 6802
			TransferableWithTrade
		}

		// Token: 0x0200069C RID: 1692
		// (Invoke) Token: 0x06005236 RID: 21046
		public delegate void PresentationUpdate(PartyScreenLogic.PartyCommand command);

		// Token: 0x0200069D RID: 1693
		// (Invoke) Token: 0x0600523A RID: 21050
		public delegate void PartyGoldDelegate();

		// Token: 0x0200069E RID: 1694
		// (Invoke) Token: 0x0600523E RID: 21054
		public delegate void PartyMoraleDelegate();

		// Token: 0x0200069F RID: 1695
		// (Invoke) Token: 0x06005242 RID: 21058
		public delegate void PartyInfluenceDelegate();

		// Token: 0x020006A0 RID: 1696
		// (Invoke) Token: 0x06005246 RID: 21062
		public delegate void PartyHorseDelegate();

		// Token: 0x020006A1 RID: 1697
		// (Invoke) Token: 0x0600524A RID: 21066
		public delegate void AfterResetDelegate(PartyScreenLogic partyScreenLogic, bool fromCancel);

		// Token: 0x020006A2 RID: 1698
		public class PartyCommand : ISerializableObject
		{
			// Token: 0x17000F59 RID: 3929
			// (get) Token: 0x0600524D RID: 21069 RVA: 0x001867A3 File Offset: 0x001849A3
			// (set) Token: 0x0600524E RID: 21070 RVA: 0x001867AB File Offset: 0x001849AB
			public PartyScreenLogic.PartyCommandCode Code { get; private set; }

			// Token: 0x17000F5A RID: 3930
			// (get) Token: 0x0600524F RID: 21071 RVA: 0x001867B4 File Offset: 0x001849B4
			// (set) Token: 0x06005250 RID: 21072 RVA: 0x001867BC File Offset: 0x001849BC
			public PartyScreenLogic.PartyRosterSide RosterSide { get; private set; }

			// Token: 0x17000F5B RID: 3931
			// (get) Token: 0x06005251 RID: 21073 RVA: 0x001867C5 File Offset: 0x001849C5
			// (set) Token: 0x06005252 RID: 21074 RVA: 0x001867CD File Offset: 0x001849CD
			public CharacterObject Character { get; private set; }

			// Token: 0x17000F5C RID: 3932
			// (get) Token: 0x06005253 RID: 21075 RVA: 0x001867D6 File Offset: 0x001849D6
			// (set) Token: 0x06005254 RID: 21076 RVA: 0x001867DE File Offset: 0x001849DE
			public int TotalNumber { get; private set; }

			// Token: 0x17000F5D RID: 3933
			// (get) Token: 0x06005255 RID: 21077 RVA: 0x001867E7 File Offset: 0x001849E7
			// (set) Token: 0x06005256 RID: 21078 RVA: 0x001867EF File Offset: 0x001849EF
			public int WoundedNumber { get; private set; }

			// Token: 0x17000F5E RID: 3934
			// (get) Token: 0x06005257 RID: 21079 RVA: 0x001867F8 File Offset: 0x001849F8
			// (set) Token: 0x06005258 RID: 21080 RVA: 0x00186800 File Offset: 0x00184A00
			public int Index { get; private set; }

			// Token: 0x17000F5F RID: 3935
			// (get) Token: 0x06005259 RID: 21081 RVA: 0x00186809 File Offset: 0x00184A09
			// (set) Token: 0x0600525A RID: 21082 RVA: 0x00186811 File Offset: 0x00184A11
			public int UpgradeTarget { get; private set; }

			// Token: 0x17000F60 RID: 3936
			// (get) Token: 0x0600525B RID: 21083 RVA: 0x0018681A File Offset: 0x00184A1A
			// (set) Token: 0x0600525C RID: 21084 RVA: 0x00186822 File Offset: 0x00184A22
			public PartyScreenLogic.TroopType Type { get; private set; }

			// Token: 0x17000F61 RID: 3937
			// (get) Token: 0x0600525D RID: 21085 RVA: 0x0018682B File Offset: 0x00184A2B
			// (set) Token: 0x0600525E RID: 21086 RVA: 0x00186833 File Offset: 0x00184A33
			public PartyScreenLogic.TroopSortType SortType { get; private set; }

			// Token: 0x17000F62 RID: 3938
			// (get) Token: 0x0600525F RID: 21087 RVA: 0x0018683C File Offset: 0x00184A3C
			// (set) Token: 0x06005260 RID: 21088 RVA: 0x00186844 File Offset: 0x00184A44
			public bool IsSortAscending { get; private set; }

			// Token: 0x06005262 RID: 21090 RVA: 0x00186855 File Offset: 0x00184A55
			public void FillForTransferTroop(PartyScreenLogic.PartyRosterSide fromSide, PartyScreenLogic.TroopType type, CharacterObject character, int totalNumber, int woundedNumber, int targetIndex)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.TransferTroop;
				this.RosterSide = fromSide;
				this.TotalNumber = totalNumber;
				this.WoundedNumber = woundedNumber;
				this.Character = character;
				this.Type = type;
				this.Index = targetIndex;
			}

			// Token: 0x06005263 RID: 21091 RVA: 0x0018688B File Offset: 0x00184A8B
			public void FillForShiftTroop(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, CharacterObject character, int targetIndex)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.ShiftTroop;
				this.RosterSide = side;
				this.Character = character;
				this.Type = type;
				this.Index = targetIndex;
			}

			// Token: 0x06005264 RID: 21092 RVA: 0x001868B1 File Offset: 0x00184AB1
			public void FillForTransferTroopToLeaderSlot(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, CharacterObject character, int totalNumber, int woundedNumber, int targetIndex)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.TransferTroopToLeaderSlot;
				this.RosterSide = side;
				this.TotalNumber = totalNumber;
				this.WoundedNumber = woundedNumber;
				this.Character = character;
				this.Type = type;
				this.Index = targetIndex;
			}

			// Token: 0x06005265 RID: 21093 RVA: 0x001868E7 File Offset: 0x00184AE7
			public void FillForTransferPartyLeaderTroop(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, CharacterObject character, int totalNumber)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.TransferPartyLeaderTroop;
				this.RosterSide = side;
				this.TotalNumber = totalNumber;
				this.Character = character;
				this.Type = type;
			}

			// Token: 0x06005266 RID: 21094 RVA: 0x0018690D File Offset: 0x00184B0D
			public void FillForUpgradeTroop(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, CharacterObject character, int number, int upgradeTargetType, int index)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.UpgradeTroop;
				this.RosterSide = side;
				this.TotalNumber = number;
				this.Character = character;
				this.UpgradeTarget = upgradeTargetType;
				this.Type = type;
				this.Index = index;
			}

			// Token: 0x06005267 RID: 21095 RVA: 0x00186943 File Offset: 0x00184B43
			public void FillForRecruitTroop(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, CharacterObject character, int number, int index)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.RecruitTroop;
				this.RosterSide = side;
				this.Character = character;
				this.Type = type;
				this.TotalNumber = number;
				this.Index = index;
			}

			// Token: 0x06005268 RID: 21096 RVA: 0x00186971 File Offset: 0x00184B71
			public void FillForExecuteTroop(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type, CharacterObject character)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.ExecuteTroop;
				this.RosterSide = side;
				this.Character = character;
				this.Type = type;
			}

			// Token: 0x06005269 RID: 21097 RVA: 0x0018698F File Offset: 0x00184B8F
			public void FillForTransferAllTroops(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopType type)
			{
				this.Code = PartyScreenLogic.PartyCommandCode.TransferAllTroops;
				this.RosterSide = side;
				this.Type = type;
			}

			// Token: 0x0600526A RID: 21098 RVA: 0x001869A6 File Offset: 0x00184BA6
			public void FillForSortTroops(PartyScreenLogic.PartyRosterSide side, PartyScreenLogic.TroopSortType sortType, bool isAscending)
			{
				this.RosterSide = side;
				this.Code = PartyScreenLogic.PartyCommandCode.SortTroops;
				this.SortType = sortType;
				this.IsSortAscending = isAscending;
			}

			// Token: 0x0600526B RID: 21099 RVA: 0x001869C4 File Offset: 0x00184BC4
			void ISerializableObject.SerializeTo(IWriter writer)
			{
				writer.WriteByte((byte)this.Code);
				writer.WriteByte((byte)this.RosterSide);
				writer.WriteUInt(this.Character.Id.InternalValue);
				writer.WriteInt(this.TotalNumber);
				writer.WriteInt(this.WoundedNumber);
				writer.WriteInt(this.UpgradeTarget);
				writer.WriteByte((byte)this.Type);
			}

			// Token: 0x0600526C RID: 21100 RVA: 0x00186A34 File Offset: 0x00184C34
			void ISerializableObject.DeserializeFrom(IReader reader)
			{
				this.Code = (PartyScreenLogic.PartyCommandCode)reader.ReadByte();
				this.RosterSide = (PartyScreenLogic.PartyRosterSide)reader.ReadByte();
				MBGUID objectId = new MBGUID(reader.ReadUInt());
				this.Character = (CharacterObject)MBObjectManager.Instance.GetObject(objectId);
				this.TotalNumber = reader.ReadInt();
				this.WoundedNumber = reader.ReadInt();
				this.UpgradeTarget = reader.ReadInt();
				this.Type = (PartyScreenLogic.TroopType)reader.ReadByte();
			}
		}

		// Token: 0x020006A3 RID: 1699
		public abstract class TroopComparer : IComparer<TroopRosterElement>
		{
			// Token: 0x0600526D RID: 21101 RVA: 0x00186AAC File Offset: 0x00184CAC
			public void SetIsAscending(bool isAscending)
			{
				this._isAscending = isAscending;
			}

			// Token: 0x0600526E RID: 21102 RVA: 0x00186AB5 File Offset: 0x00184CB5
			private int GetHeroComparisonResult(TroopRosterElement x, TroopRosterElement y)
			{
				if (x.Character.HeroObject != null)
				{
					if (x.Character.HeroObject == Hero.MainHero)
					{
						return -2;
					}
					if (y.Character.HeroObject == null)
					{
						return -1;
					}
				}
				return 0;
			}

			// Token: 0x0600526F RID: 21103 RVA: 0x00186AEC File Offset: 0x00184CEC
			public int Compare(TroopRosterElement x, TroopRosterElement y)
			{
				int num = (this._isAscending ? 1 : (-1));
				int heroComparisonResult = this.GetHeroComparisonResult(x, y);
				if (heroComparisonResult != 0)
				{
					return heroComparisonResult;
				}
				heroComparisonResult = this.GetHeroComparisonResult(y, x);
				if (heroComparisonResult != 0)
				{
					return heroComparisonResult * -1;
				}
				return this.CompareTroops(x, y) * num;
			}

			// Token: 0x06005270 RID: 21104
			protected abstract int CompareTroops(TroopRosterElement x, TroopRosterElement y);

			// Token: 0x04001A9D RID: 6813
			private bool _isAscending;
		}

		// Token: 0x020006A4 RID: 1700
		private class TroopDefaultComparer : PartyScreenLogic.TroopComparer
		{
			// Token: 0x06005272 RID: 21106 RVA: 0x00186B36 File Offset: 0x00184D36
			protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
			{
				return 0;
			}
		}

		// Token: 0x020006A5 RID: 1701
		private class TroopTypeComparer : PartyScreenLogic.TroopComparer
		{
			// Token: 0x06005274 RID: 21108 RVA: 0x00186B44 File Offset: 0x00184D44
			protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
			{
				int defaultFormationClass = (int)x.Character.DefaultFormationClass;
				int defaultFormationClass2 = (int)y.Character.DefaultFormationClass;
				return defaultFormationClass.CompareTo(defaultFormationClass2);
			}
		}

		// Token: 0x020006A6 RID: 1702
		private class TroopNameComparer : PartyScreenLogic.TroopComparer
		{
			// Token: 0x06005276 RID: 21110 RVA: 0x00186B79 File Offset: 0x00184D79
			protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
			{
				return x.Character.Name.ToString().CompareTo(y.Character.Name.ToString());
			}
		}

		// Token: 0x020006A7 RID: 1703
		private class TroopCountComparer : PartyScreenLogic.TroopComparer
		{
			// Token: 0x06005278 RID: 21112 RVA: 0x00186BA8 File Offset: 0x00184DA8
			protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
			{
				return x.Number.CompareTo(y.Number);
			}
		}

		// Token: 0x020006A8 RID: 1704
		private class TroopTierComparer : PartyScreenLogic.TroopComparer
		{
			// Token: 0x0600527A RID: 21114 RVA: 0x00186BD4 File Offset: 0x00184DD4
			protected override int CompareTroops(TroopRosterElement x, TroopRosterElement y)
			{
				return x.Character.Tier.CompareTo(y.Character.Tier);
			}
		}
	}
}
