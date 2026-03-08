using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000B4 RID: 180
	[MenuOverlay("ArmyMenuOverlay")]
	public class ArmyMenuOverlayVM : GameMenuOverlay
	{
		// Token: 0x170005C0 RID: 1472
		// (get) Token: 0x06001197 RID: 4503 RVA: 0x00045EB2 File Offset: 0x000440B2
		private Army ArmyToUse
		{
			get
			{
				Army army;
				if ((army = MobileParty.MainParty.Army) == null)
				{
					MobileParty targetParty = MobileParty.MainParty.TargetParty;
					if (targetParty == null)
					{
						return null;
					}
					army = targetParty.Army;
				}
				return army;
			}
		}

		// Token: 0x06001198 RID: 4504 RVA: 0x00045ED8 File Offset: 0x000440D8
		public ArmyMenuOverlayVM()
		{
			this.PartyList = new MBBindingList<GameMenuPartyItemVM>();
			base.CurrentOverlayType = 2;
			base.IsInitializationOver = false;
			this._savedPartyList = new List<MobileParty>();
			this.CohesionHint = new BasicTooltipViewModel();
			this.ManCountHint = new BasicTooltipViewModel();
			this.FoodHint = new BasicTooltipViewModel();
			this.TutorialNotification = new ElementNotificationVM();
			this.ManageArmyHint = new HintViewModel();
			this.Refresh();
			this._contextMenuItem = null;
			CampaignEvents.ArmyOverlaySetDirtyEvent.AddNonSerializedListener(this, new Action(this.Refresh));
			CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyAttachedAnotherParty));
			CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, Hero, CharacterObject, int>(this.OnTroopRecruited));
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this._cohesionConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_army_cohesion");
			base.IsInitializationOver = true;
		}

		// Token: 0x06001199 RID: 4505 RVA: 0x00045FEA File Offset: 0x000441EA
		public override void RefreshValues()
		{
			base.RefreshValues();
			ElementNotificationVM tutorialNotification = this.TutorialNotification;
			if (tutorialNotification != null)
			{
				tutorialNotification.RefreshValues();
			}
			this.Refresh();
		}

		// Token: 0x0600119A RID: 4506 RVA: 0x0004600C File Offset: 0x0004420C
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.ArmyOverlaySetDirtyEvent.ClearListeners(this);
			CampaignEvents.PartyAttachedAnotherParty.ClearListeners(this);
			CampaignEvents.OnTroopRecruitedEvent.ClearListeners(this);
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x0600119B RID: 4507 RVA: 0x0004605C File Offset: 0x0004425C
		protected override void ExecuteOnSetAsActiveContextMenuItem(GameMenuPartyItemVM troop)
		{
			base.ExecuteOnSetAsActiveContextMenuItem(troop);
			base.ContextList.Clear();
			MobileParty mobileParty = this._contextMenuItem.Party.MobileParty;
			if (((mobileParty != null) ? mobileParty.Army : null) != null && ArmyMenuOverlayVM.GetIsPlayerArmyLeader(this._contextMenuItem.Party.MobileParty.Army) && this._contextMenuItem.Party.MapEvent == null && this._contextMenuItem.Party != this._contextMenuItem.Party.MobileParty.Army.LeaderParty.Party)
			{
				TextObject hintText;
				bool mapScreenActionIsEnabledWithReason = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out hintText);
				base.ContextList.Add(new StringItemWithEnabledAndHintVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", GameMenuOverlay.MenuOverlayContextList.ArmyDismiss.ToString()).ToString(), mapScreenActionIsEnabledWithReason, GameMenuOverlay.MenuOverlayContextList.ArmyDismiss, hintText));
			}
			float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
			MapEvent mapEvent = MobileParty.MainParty.MapEvent;
			CampaignVec2 v = ((mapEvent != null) ? mapEvent.Position : MobileParty.MainParty.Position);
			MobileParty mobileParty2 = troop.Party.MobileParty;
			float? num = ((mobileParty2 != null) ? new float?(mobileParty2.Position.DistanceSquared(v)) : null);
			float num2 = getEncounterJoiningRadius * getEncounterJoiningRadius;
			bool flag = (num.GetValueOrDefault() < num2) & (num != null);
			bool flag2 = troop.Party.MobileParty.MapEvent == MobileParty.MainParty.MapEvent;
			bool flag3 = PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
			if (this._contextMenuItem.Party.LeaderHero != null && flag && flag2 && !flag3 && this._contextMenuItem.Party != PartyBase.MainParty)
			{
				PlayerEncounter playerEncounter = PlayerEncounter.Current;
				if (((playerEncounter != null) ? playerEncounter.BattleSimulation : null) == null)
				{
					base.ContextList.Add(new StringItemWithEnabledAndHintVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", GameMenuOverlay.MenuOverlayContextList.DonateTroops.ToString()).ToString(), true, GameMenuOverlay.MenuOverlayContextList.DonateTroops, null));
					if (MobileParty.MainParty.CurrentSettlement == null && LocationComplex.Current == null)
					{
						base.ContextList.Add(new StringItemWithEnabledAndHintVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", GameMenuOverlay.MenuOverlayContextList.ConverseWithLeader.ToString()).ToString(), true, GameMenuOverlay.MenuOverlayContextList.ConverseWithLeader, null));
					}
				}
			}
			base.ContextList.Add(new StringItemWithEnabledAndHintVM(new Action<object>(base.ExecuteTroopAction), GameTexts.FindText("str_menu_overlay_context_list", GameMenuOverlay.MenuOverlayContextList.Encyclopedia.ToString()).ToString(), true, GameMenuOverlay.MenuOverlayContextList.Encyclopedia, null));
			CharacterObject characterObject;
			if ((characterObject = this._contextMenuItem.Character) == null)
			{
				Hero leaderHero = this._contextMenuItem.Party.LeaderHero;
				characterObject = ((leaderHero != null) ? leaderHero.CharacterObject : null);
			}
			CharacterObject characterObject2 = characterObject;
			if (characterObject2 == null)
			{
				Debug.FailedAssert("ArmyMenuOverlayVM.ExecuteOnSetAsActiveContextMenuItem called on party with no leader hero: " + this._contextMenuItem.Party.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "ExecuteOnSetAsActiveContextMenuItem", 127);
				return;
			}
			CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpOpened(characterObject2);
		}

		// Token: 0x0600119C RID: 4508 RVA: 0x000463A0 File Offset: 0x000445A0
		public override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			TextObject hintText;
			this.CanManageArmy = CampaignUIHelper.GetCanManageCurrentArmyWithReason(out hintText);
			this.ManageArmyHint.HintText = hintText;
			for (int i = 0; i < this.PartyList.Count; i++)
			{
				this.PartyList[i].RefreshQuestStatus();
			}
			if (this._isVisualsDirty)
			{
				this.RefreshVisualsOfItems();
				this._isVisualsDirty = false;
			}
		}

		// Token: 0x0600119D RID: 4509 RVA: 0x00046409 File Offset: 0x00044609
		public sealed override void Refresh()
		{
			if (this.ArmyToUse != null)
			{
				base.IsInitializationOver = false;
				this.UpdateLists();
				this.UpdateProperties();
				base.IsInitializationOver = true;
			}
		}

		// Token: 0x0600119E RID: 4510 RVA: 0x00046430 File Offset: 0x00044630
		private void UpdateProperties()
		{
			MBTextManager.SetTextVariable("newline", "\n", false);
			Army army = this.ArmyToUse;
			if (army == null)
			{
				Debug.FailedAssert("Army is null but trying to update army overlay properties", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "UpdateProperties", 172);
				return;
			}
			float num = army.LeaderParty.Food;
			foreach (MobileParty mobileParty in army.LeaderParty.AttachedParties)
			{
				num += mobileParty.Food;
			}
			this.Food = (int)num;
			this.Cohesion = (int)army.Cohesion;
			this.ManCountText = CampaignUIHelper.GetPartyNameplateText(army.LeaderParty, true);
			this.FoodHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyFoodTooltip(army));
			this.CohesionHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyCohesionTooltip(army));
			this.ManCountHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyManCountTooltip(army));
			this.IsCohesionWarningEnabled = army.Cohesion <= 30f;
			this.IsPlayerArmyLeader = ArmyMenuOverlayVM.GetIsPlayerArmyLeader(army);
		}

		// Token: 0x0600119F RID: 4511 RVA: 0x00046588 File Offset: 0x00044788
		private void UpdateLists()
		{
			Army army = this.ArmyToUse;
			if (army == null)
			{
				Debug.FailedAssert("Army is null but trying to update army overlay lists", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "UpdateLists", 201);
				return;
			}
			List<MobileParty> list = army.Parties.Except(this._savedPartyList).ToList<MobileParty>();
			list.Remove(army.LeaderParty);
			List<MobileParty> list2 = this._savedPartyList.Except(army.Parties).ToList<MobileParty>();
			this._savedPartyList = army.Parties.ToList<MobileParty>();
			foreach (MobileParty mobileParty in list2)
			{
				for (int i = this.PartyList.Count - 1; i >= 0; i--)
				{
					if (this.PartyList[i].Party == mobileParty.Party)
					{
						this.PartyList.RemoveAt(i);
						break;
					}
				}
			}
			foreach (MobileParty mobileParty2 in list)
			{
				this.PartyList.Add(new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), mobileParty2.Party, true));
			}
			foreach (GameMenuPartyItemVM gameMenuPartyItemVM in this.PartyList)
			{
				gameMenuPartyItemVM.RefreshProperties();
			}
			if (this.PartyList.Count > 0 && this.PartyList[0].Party != army.LeaderParty.Party)
			{
				if (this.PartyList.SingleOrDefault((GameMenuPartyItemVM p) => p.Party == army.LeaderParty.Party) != null)
				{
					int index = this.PartyList.IndexOf(this.PartyList.SingleOrDefault((GameMenuPartyItemVM p) => p.Party == army.LeaderParty.Party));
					this.PartyList.RemoveAt(index);
				}
				GameMenuPartyItemVM gameMenuPartyItemVM2 = new GameMenuPartyItemVM(new Action<GameMenuPartyItemVM>(this.ExecuteOnSetAsActiveContextMenuItem), army.LeaderParty.Party, true)
				{
					IsLeader = true
				};
				this.PartyList.Insert(0, gameMenuPartyItemVM2);
				gameMenuPartyItemVM2.RefreshProperties();
			}
		}

		// Token: 0x060011A0 RID: 4512 RVA: 0x00046804 File Offset: 0x00044A04
		public void ExecuteOpenArmyManagement()
		{
			Army armyToUse = this.ArmyToUse;
			if (armyToUse != null && ArmyMenuOverlayVM.GetIsPlayerArmyLeader(armyToUse))
			{
				Action openArmyManagement = this.OpenArmyManagement;
				if (openArmyManagement == null)
				{
					return;
				}
				openArmyManagement();
			}
		}

		// Token: 0x060011A1 RID: 4513 RVA: 0x00046833 File Offset: 0x00044A33
		private void ExecuteCohesionLink()
		{
			if (this._cohesionConceptObj != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._cohesionConceptObj.EncyclopediaLink);
				return;
			}
			Debug.FailedAssert("Couldn't find Cohesion encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "ExecuteCohesionLink", 267);
		}

		// Token: 0x060011A2 RID: 4514 RVA: 0x00046874 File Offset: 0x00044A74
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = string.Empty;
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null)
				{
					this.TutorialNotification.ElementID = this._latestTutorialElementID;
				}
			}
		}

		// Token: 0x060011A3 RID: 4515 RVA: 0x000468D4 File Offset: 0x00044AD4
		private void RefreshVisualsOfItems()
		{
			for (int i = 0; i < this.PartyList.Count; i++)
			{
				this.PartyList[i].RefreshVisual();
			}
		}

		// Token: 0x060011A4 RID: 4516 RVA: 0x00046908 File Offset: 0x00044B08
		private void OnPartyAttachedAnotherParty(MobileParty party)
		{
			MobileParty attachedTo = party.AttachedTo;
			if (((attachedTo != null) ? attachedTo.Army : null) != null && party.AttachedTo.Army == MobileParty.MainParty.Army)
			{
				this._isVisualsDirty = true;
			}
		}

		// Token: 0x060011A5 RID: 4517 RVA: 0x0004693C File Offset: 0x00044B3C
		private void OnTroopRecruited(Hero recruiterHero, Settlement settlement, Hero troopSource, CharacterObject troop, int number)
		{
			if (((recruiterHero != null) ? recruiterHero.PartyBelongedTo : null) != null && recruiterHero.IsPartyLeader)
			{
				for (int i = 0; i < this.PartyList.Count; i++)
				{
					if (this.PartyList[i].Party == recruiterHero.PartyBelongedTo.Party)
					{
						this.PartyList[i].RefreshProperties();
						return;
					}
				}
			}
		}

		// Token: 0x060011A6 RID: 4518 RVA: 0x000469A5 File Offset: 0x00044BA5
		private static bool GetIsPlayerArmyLeader(Army army)
		{
			return army.LeaderParty == MobileParty.MainParty || army.LeaderParty == MobileParty.MainParty.TargetParty;
		}

		// Token: 0x170005C1 RID: 1473
		// (get) Token: 0x060011A7 RID: 4519 RVA: 0x000469C8 File Offset: 0x00044BC8
		// (set) Token: 0x060011A8 RID: 4520 RVA: 0x000469D0 File Offset: 0x00044BD0
		[DataSourceProperty]
		public ElementNotificationVM TutorialNotification
		{
			get
			{
				return this._tutorialNotification;
			}
			set
			{
				if (value != this._tutorialNotification)
				{
					this._tutorialNotification = value;
					base.OnPropertyChangedWithValue<ElementNotificationVM>(value, "TutorialNotification");
				}
			}
		}

		// Token: 0x170005C2 RID: 1474
		// (get) Token: 0x060011A9 RID: 4521 RVA: 0x000469EE File Offset: 0x00044BEE
		// (set) Token: 0x060011AA RID: 4522 RVA: 0x000469F6 File Offset: 0x00044BF6
		[DataSourceProperty]
		public HintViewModel ManageArmyHint
		{
			get
			{
				return this._manageArmyHint;
			}
			set
			{
				if (value != this._manageArmyHint)
				{
					this._manageArmyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ManageArmyHint");
				}
			}
		}

		// Token: 0x170005C3 RID: 1475
		// (get) Token: 0x060011AB RID: 4523 RVA: 0x00046A14 File Offset: 0x00044C14
		// (set) Token: 0x060011AC RID: 4524 RVA: 0x00046A1C File Offset: 0x00044C1C
		[DataSourceProperty]
		public int Cohesion
		{
			get
			{
				return this._cohesion;
			}
			set
			{
				if (value != this._cohesion)
				{
					this._cohesion = value;
					base.OnPropertyChangedWithValue(value, "Cohesion");
				}
			}
		}

		// Token: 0x170005C4 RID: 1476
		// (get) Token: 0x060011AD RID: 4525 RVA: 0x00046A3A File Offset: 0x00044C3A
		// (set) Token: 0x060011AE RID: 4526 RVA: 0x00046A42 File Offset: 0x00044C42
		[DataSourceProperty]
		public bool IsCohesionWarningEnabled
		{
			get
			{
				return this._isCohesionWarningEnabled;
			}
			set
			{
				if (value != this._isCohesionWarningEnabled)
				{
					this._isCohesionWarningEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsCohesionWarningEnabled");
				}
			}
		}

		// Token: 0x170005C5 RID: 1477
		// (get) Token: 0x060011AF RID: 4527 RVA: 0x00046A60 File Offset: 0x00044C60
		// (set) Token: 0x060011B0 RID: 4528 RVA: 0x00046A68 File Offset: 0x00044C68
		[DataSourceProperty]
		public bool CanManageArmy
		{
			get
			{
				return this._canManageArmy;
			}
			set
			{
				if (value != this._canManageArmy)
				{
					this._canManageArmy = value;
					base.OnPropertyChangedWithValue(value, "CanManageArmy");
				}
			}
		}

		// Token: 0x170005C6 RID: 1478
		// (get) Token: 0x060011B1 RID: 4529 RVA: 0x00046A86 File Offset: 0x00044C86
		// (set) Token: 0x060011B2 RID: 4530 RVA: 0x00046A8E File Offset: 0x00044C8E
		[DataSourceProperty]
		public bool IsPlayerArmyLeader
		{
			get
			{
				return this._isPlayerArmyLeader;
			}
			set
			{
				if (value != this._isPlayerArmyLeader)
				{
					this._isPlayerArmyLeader = value;
					base.OnPropertyChangedWithValue(value, "IsPlayerArmyLeader");
				}
			}
		}

		// Token: 0x170005C7 RID: 1479
		// (get) Token: 0x060011B3 RID: 4531 RVA: 0x00046AAC File Offset: 0x00044CAC
		// (set) Token: 0x060011B4 RID: 4532 RVA: 0x00046AB4 File Offset: 0x00044CB4
		[DataSourceProperty]
		public string ManCountText
		{
			get
			{
				return this._manCountText;
			}
			set
			{
				if (value != this._manCountText)
				{
					this._manCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "ManCountText");
				}
			}
		}

		// Token: 0x170005C8 RID: 1480
		// (get) Token: 0x060011B5 RID: 4533 RVA: 0x00046AD7 File Offset: 0x00044CD7
		// (set) Token: 0x060011B6 RID: 4534 RVA: 0x00046ADF File Offset: 0x00044CDF
		[DataSourceProperty]
		public int Food
		{
			get
			{
				return this._food;
			}
			set
			{
				if (value != this._food)
				{
					this._food = value;
					base.OnPropertyChangedWithValue(value, "Food");
				}
			}
		}

		// Token: 0x170005C9 RID: 1481
		// (get) Token: 0x060011B7 RID: 4535 RVA: 0x00046AFD File Offset: 0x00044CFD
		// (set) Token: 0x060011B8 RID: 4536 RVA: 0x00046B05 File Offset: 0x00044D05
		[DataSourceProperty]
		public MBBindingList<GameMenuPartyItemVM> PartyList
		{
			get
			{
				return this._partyList;
			}
			set
			{
				if (value != this._partyList)
				{
					this._partyList = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameMenuPartyItemVM>>(value, "PartyList");
				}
			}
		}

		// Token: 0x170005CA RID: 1482
		// (get) Token: 0x060011B9 RID: 4537 RVA: 0x00046B23 File Offset: 0x00044D23
		// (set) Token: 0x060011BA RID: 4538 RVA: 0x00046B2B File Offset: 0x00044D2B
		[DataSourceProperty]
		public BasicTooltipViewModel CohesionHint
		{
			get
			{
				return this._cohesionHint;
			}
			set
			{
				if (value != this._cohesionHint)
				{
					this._cohesionHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CohesionHint");
				}
			}
		}

		// Token: 0x170005CB RID: 1483
		// (get) Token: 0x060011BB RID: 4539 RVA: 0x00046B49 File Offset: 0x00044D49
		// (set) Token: 0x060011BC RID: 4540 RVA: 0x00046B51 File Offset: 0x00044D51
		[DataSourceProperty]
		public BasicTooltipViewModel ManCountHint
		{
			get
			{
				return this._manCountHint;
			}
			set
			{
				if (value != this._manCountHint)
				{
					this._manCountHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ManCountHint");
				}
			}
		}

		// Token: 0x170005CC RID: 1484
		// (get) Token: 0x060011BD RID: 4541 RVA: 0x00046B6F File Offset: 0x00044D6F
		// (set) Token: 0x060011BE RID: 4542 RVA: 0x00046B77 File Offset: 0x00044D77
		[DataSourceProperty]
		public BasicTooltipViewModel FoodHint
		{
			get
			{
				return this._foodHint;
			}
			set
			{
				if (value != this._foodHint)
				{
					this._foodHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "FoodHint");
				}
			}
		}

		// Token: 0x170005CD RID: 1485
		// (get) Token: 0x060011BF RID: 4543 RVA: 0x00046B95 File Offset: 0x00044D95
		[DataSourceProperty]
		public MBBindingList<StringItemWithHintVM> IssueList
		{
			get
			{
				if (this._issueList == null)
				{
					this._issueList = new MBBindingList<StringItemWithHintVM>();
				}
				return this._issueList;
			}
		}

		// Token: 0x0400080A RID: 2058
		private List<MobileParty> _savedPartyList;

		// Token: 0x0400080B RID: 2059
		private const float CohesionWarningMin = 30f;

		// Token: 0x0400080C RID: 2060
		public Action OpenArmyManagement;

		// Token: 0x0400080D RID: 2061
		private readonly Concept _cohesionConceptObj;

		// Token: 0x0400080E RID: 2062
		private string _latestTutorialElementID;

		// Token: 0x0400080F RID: 2063
		private bool _isVisualsDirty;

		// Token: 0x04000810 RID: 2064
		private MBBindingList<GameMenuPartyItemVM> _partyList;

		// Token: 0x04000811 RID: 2065
		private string _manCountText;

		// Token: 0x04000812 RID: 2066
		private int _cohesion;

		// Token: 0x04000813 RID: 2067
		private int _food;

		// Token: 0x04000814 RID: 2068
		private bool _isCohesionWarningEnabled;

		// Token: 0x04000815 RID: 2069
		private bool _isPlayerArmyLeader;

		// Token: 0x04000816 RID: 2070
		private bool _canManageArmy;

		// Token: 0x04000817 RID: 2071
		private HintViewModel _manageArmyHint;

		// Token: 0x04000818 RID: 2072
		public ElementNotificationVM _tutorialNotification;

		// Token: 0x04000819 RID: 2073
		private BasicTooltipViewModel _cohesionHint;

		// Token: 0x0400081A RID: 2074
		private BasicTooltipViewModel _manCountHint;

		// Token: 0x0400081B RID: 2075
		private BasicTooltipViewModel _foodHint;

		// Token: 0x0400081C RID: 2076
		private MBBindingList<StringItemWithHintVM> _issueList;
	}
}
