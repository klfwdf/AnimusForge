using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000B7 RID: 183
	public class GameMenuOverlay : ViewModel
	{
		// Token: 0x0600120B RID: 4619 RVA: 0x000488DA File Offset: 0x00046ADA
		public GameMenuOverlay()
		{
			this.ContextList = new MBBindingList<StringItemWithEnabledAndHintVM>();
		}

		// Token: 0x0600120C RID: 4620 RVA: 0x000488FB File Offset: 0x00046AFB
		public override void RefreshValues()
		{
			base.RefreshValues();
			GameMenuPartyItemVM contextMenuItem = this._contextMenuItem;
			if (contextMenuItem == null)
			{
				return;
			}
			contextMenuItem.RefreshValues();
		}

		// Token: 0x0600120D RID: 4621 RVA: 0x00048913 File Offset: 0x00046B13
		protected virtual void ExecuteOnSetAsActiveContextMenuItem(GameMenuPartyItemVM troop)
		{
			this._contextMenuItem = troop;
		}

		// Token: 0x0600120E RID: 4622 RVA: 0x0004891C File Offset: 0x00046B1C
		public virtual void ExecuteOnOverlayClosed()
		{
			if (!this._closedHandled)
			{
				CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpClosed();
				this._closedHandled = true;
			}
		}

		// Token: 0x0600120F RID: 4623 RVA: 0x00048937 File Offset: 0x00046B37
		public virtual void ExecuteOnOverlayOpened()
		{
			this._closedHandled = false;
		}

		// Token: 0x06001210 RID: 4624 RVA: 0x00048940 File Offset: 0x00046B40
		public override void OnFinalize()
		{
			base.OnFinalize();
			if (!this._closedHandled)
			{
				this.ExecuteOnOverlayClosed();
			}
			InputKeyItemVM exitInputKey = this.ExitInputKey;
			if (exitInputKey == null)
			{
				return;
			}
			exitInputKey.OnFinalize();
		}

		// Token: 0x06001211 RID: 4625 RVA: 0x00048968 File Offset: 0x00046B68
		protected void ExecuteTroopAction(object o)
		{
			switch ((GameMenuOverlay.MenuOverlayContextList)o)
			{
			case GameMenuOverlay.MenuOverlayContextList.Encyclopedia:
				if (this._contextMenuItem.Character != null)
				{
					if (this._contextMenuItem.Character.IsHero)
					{
						Campaign.Current.EncyclopediaManager.GoToLink(this._contextMenuItem.Character.HeroObject.EncyclopediaLink);
					}
					else
					{
						Debug.FailedAssert("Character object in menu overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\GameMenuOverlay.cs", "ExecuteTroopAction", 100);
						Campaign.Current.EncyclopediaManager.GoToLink(this._contextMenuItem.Character.EncyclopediaLink);
					}
				}
				else if (this._contextMenuItem.Party != null)
				{
					CharacterObject visualPartyLeader = CampaignUIHelper.GetVisualPartyLeader(this._contextMenuItem.Party);
					if (visualPartyLeader != null)
					{
						Campaign.Current.EncyclopediaManager.GoToLink(visualPartyLeader.EncyclopediaLink);
					}
				}
				else if (this._contextMenuItem.Settlement != null)
				{
					Campaign.Current.EncyclopediaManager.GoToLink(this._contextMenuItem.Settlement.EncyclopediaLink);
				}
				break;
			case GameMenuOverlay.MenuOverlayContextList.Conversation:
				if (this._contextMenuItem.Character != null)
				{
					if (this._contextMenuItem.Character.IsHero)
					{
						if (PlayerEncounter.Current != null || LocationComplex.Current != null || Campaign.Current.CurrentMenuContext != null)
						{
							Location location = LocationComplex.Current.GetLocationOfCharacter(this._contextMenuItem.Character.HeroObject);
							if (location.StringId == "alley")
							{
								location = LocationComplex.Current.GetLocationWithId("center");
							}
							CampaignEventDispatcher.Instance.OnPlayerStartTalkFromMenu(this._contextMenuItem.Character.HeroObject);
							PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, this._contextMenuItem.Character, null);
						}
						else
						{
							EncounterManager.StartPartyEncounter(PartyBase.MainParty, this._contextMenuItem.Character.HeroObject.PartyBelongedTo.Party);
						}
					}
					else
					{
						Debug.FailedAssert("Character object in menu overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\GameMenuOverlay.cs", "ExecuteTroopAction", 144);
					}
				}
				break;
			case GameMenuOverlay.MenuOverlayContextList.QuickConversation:
				if (this._contextMenuItem.Character != null)
				{
					if (this._contextMenuItem.Character.IsHero)
					{
						if (PlayerEncounter.Current != null || LocationComplex.Current != null || Campaign.Current.CurrentMenuContext != null)
						{
							CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, true, false, false), new ConversationCharacterData(this._contextMenuItem.Character, null, false, false, false, true, false, false));
						}
						else
						{
							EncounterManager.StartPartyEncounter(PartyBase.MainParty, this._contextMenuItem.Character.HeroObject.PartyBelongedTo.Party);
						}
					}
					else
					{
						Debug.FailedAssert("Character object in menu overlay", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\GameMenuOverlay.cs", "ExecuteTroopAction", 167);
					}
				}
				break;
			case GameMenuOverlay.MenuOverlayContextList.ConverseWithLeader:
			{
				PartyBase party = this._contextMenuItem.Party;
				if (((party != null) ? party.LeaderHero : null) != null)
				{
					if (Settlement.CurrentSettlement != null || LocationComplex.Current != null || Campaign.Current.CurrentMenuContext != null)
					{
						this.ConverseWithLeader(PartyBase.MainParty, this._contextMenuItem.Party);
					}
					else
					{
						EncounterManager.StartPartyEncounter(PartyBase.MainParty, this._contextMenuItem.Party);
					}
				}
				break;
			}
			case GameMenuOverlay.MenuOverlayContextList.ArmyDismiss:
			{
				PartyBase party2 = this._contextMenuItem.Party;
				if (((party2 != null) ? party2.MobileParty.Army : null) != null && this._contextMenuItem.Party.MapEvent == null && this._contextMenuItem.Party.MobileParty.Army.LeaderParty != this._contextMenuItem.Party.MobileParty)
				{
					if (this._contextMenuItem.Party.MobileParty.Army.LeaderParty == MobileParty.MainParty && this._contextMenuItem.Party.MobileParty.Army.Parties.Count <= 2)
					{
						DisbandArmyAction.ApplyByNotEnoughParty(this._contextMenuItem.Party.MobileParty.Army);
					}
					else
					{
						this._contextMenuItem.Party.MobileParty.Army = null;
						this._contextMenuItem.Party.MobileParty.SetMoveModeHold();
					}
				}
				break;
			}
			case GameMenuOverlay.MenuOverlayContextList.ManageGarrison:
				if (this._contextMenuItem.Party != null)
				{
					PartyScreenHelper.OpenScreenAsManageTroops(this._contextMenuItem.Party.MobileParty);
				}
				break;
			case GameMenuOverlay.MenuOverlayContextList.DonateTroops:
				if (this._contextMenuItem.Party != null)
				{
					if (this._contextMenuItem.Party.MobileParty.IsGarrison)
					{
						PartyScreenHelper.OpenScreenAsDonateGarrisonWithCurrentSettlement();
					}
					else
					{
						PartyScreenHelper.OpenScreenAsDonateTroops(this._contextMenuItem.Party.MobileParty);
					}
				}
				break;
			case GameMenuOverlay.MenuOverlayContextList.JoinArmy:
			{
				CharacterObject character = this._contextMenuItem.Character;
				if (character != null && character.IsHero && this._contextMenuItem.Character.HeroObject.PartyBelongedTo != null)
				{
					MobileParty.MainParty.Army = this._contextMenuItem.Character.HeroObject.PartyBelongedTo.Army;
					MobileParty.MainParty.Army.AddPartyToMergedParties(MobileParty.MainParty);
					MenuContext currentMenuContext = Campaign.Current.CurrentMenuContext;
					if (currentMenuContext != null)
					{
						currentMenuContext.Refresh();
					}
				}
				break;
			}
			case GameMenuOverlay.MenuOverlayContextList.TakeToParty:
			{
				CharacterObject character2 = this._contextMenuItem.Character;
				if (character2 != null && character2.IsHero && this._contextMenuItem.Character.HeroObject.PartyBelongedTo == null)
				{
					Settlement currentSettlement = this._contextMenuItem.Character.HeroObject.CurrentSettlement;
					bool flag;
					if (currentSettlement == null)
					{
						flag = false;
					}
					else
					{
						MBReadOnlyList<Hero> notables = currentSettlement.Notables;
						bool? flag2 = ((notables != null) ? new bool?(notables.Contains(this._contextMenuItem.Character.HeroObject)) : null);
						bool flag3 = true;
						flag = (flag2.GetValueOrDefault() == flag3) & (flag2 != null);
					}
					if (flag)
					{
						LeaveSettlementAction.ApplyForCharacterOnly(this._contextMenuItem.Character.HeroObject);
					}
					AddHeroToPartyAction.Apply(this._contextMenuItem.Character.HeroObject, MobileParty.MainParty, true);
				}
				break;
			}
			case GameMenuOverlay.MenuOverlayContextList.ManageTroops:
			{
				PartyBase party3 = this._contextMenuItem.Party;
				if (((party3 != null) ? party3.MobileParty : null) != null && this._contextMenuItem.Party.MobileParty.ActualClan == Clan.PlayerClan)
				{
					PartyScreenHelper.OpenScreenAsManageTroopsAndPrisoners(this._contextMenuItem.Party.MobileParty, null);
				}
				break;
			}
			}
			if (!this._closedHandled)
			{
				CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpClosed();
				this._closedHandled = true;
			}
		}

		// Token: 0x06001212 RID: 4626 RVA: 0x00048FF0 File Offset: 0x000471F0
		private void ConverseWithLeader(PartyBase mainParty1, PartyBase party2)
		{
			bool flag;
			if (mainParty1.Side != BattleSideEnum.Attacker)
			{
				PlayerEncounter playerEncounter = PlayerEncounter.Current;
				flag = playerEncounter != null && playerEncounter.PlayerSide == BattleSideEnum.Attacker;
			}
			else
			{
				flag = true;
			}
			bool flag2 = flag;
			if (LocationComplex.Current != null && !flag2)
			{
				Location locationOfCharacter = LocationComplex.Current.GetLocationOfCharacter(party2.LeaderHero);
				CampaignEventDispatcher.Instance.OnPlayerStartTalkFromMenu(party2.LeaderHero);
				PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(locationOfCharacter, null, party2.LeaderHero.CharacterObject, null);
				return;
			}
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, mainParty1, false, false, false, false, false, false);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(ConversationHelper.GetConversationCharacterPartyLeader(party2), party2, false, false, false, false, false, false);
			if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
			{
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
				return;
			}
			CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
		}

		// Token: 0x06001213 RID: 4627 RVA: 0x000490B8 File Offset: 0x000472B8
		public virtual void Refresh()
		{
		}

		// Token: 0x06001214 RID: 4628 RVA: 0x000490BA File Offset: 0x000472BA
		public virtual void UpdateOverlayType(GameMenu.MenuOverlayType newType)
		{
			this.Refresh();
		}

		// Token: 0x06001215 RID: 4629 RVA: 0x000490C2 File Offset: 0x000472C2
		public virtual void OnFrameTick(float dt)
		{
		}

		// Token: 0x06001216 RID: 4630 RVA: 0x000490C4 File Offset: 0x000472C4
		public void HourlyTick()
		{
			this.Refresh();
		}

		// Token: 0x170005EC RID: 1516
		// (get) Token: 0x06001217 RID: 4631 RVA: 0x000490CC File Offset: 0x000472CC
		// (set) Token: 0x06001218 RID: 4632 RVA: 0x000490D4 File Offset: 0x000472D4
		[DataSourceProperty]
		public bool IsContextMenuEnabled
		{
			get
			{
				return this._isContextMenuEnabled;
			}
			set
			{
				this._isContextMenuEnabled = value;
				base.OnPropertyChangedWithValue(value, "IsContextMenuEnabled");
			}
		}

		// Token: 0x170005ED RID: 1517
		// (get) Token: 0x06001219 RID: 4633 RVA: 0x000490E9 File Offset: 0x000472E9
		// (set) Token: 0x0600121A RID: 4634 RVA: 0x000490F1 File Offset: 0x000472F1
		[DataSourceProperty]
		public bool IsInitializationOver
		{
			get
			{
				return this._isInitializationOver;
			}
			set
			{
				this._isInitializationOver = value;
				base.OnPropertyChangedWithValue(value, "IsInitializationOver");
			}
		}

		// Token: 0x170005EE RID: 1518
		// (get) Token: 0x0600121B RID: 4635 RVA: 0x00049106 File Offset: 0x00047306
		// (set) Token: 0x0600121C RID: 4636 RVA: 0x0004910E File Offset: 0x0004730E
		[DataSourceProperty]
		public bool IsInfoBarExtended
		{
			get
			{
				return this._isInfoBarExtended;
			}
			set
			{
				this._isInfoBarExtended = value;
				base.OnPropertyChangedWithValue(value, "IsInfoBarExtended");
			}
		}

		// Token: 0x170005EF RID: 1519
		// (get) Token: 0x0600121D RID: 4637 RVA: 0x00049123 File Offset: 0x00047323
		// (set) Token: 0x0600121E RID: 4638 RVA: 0x0004912B File Offset: 0x0004732B
		[DataSourceProperty]
		public MBBindingList<StringItemWithEnabledAndHintVM> ContextList
		{
			get
			{
				return this._contextList;
			}
			set
			{
				if (value != this._contextList)
				{
					this._contextList = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringItemWithEnabledAndHintVM>>(value, "ContextList");
				}
			}
		}

		// Token: 0x170005F0 RID: 1520
		// (get) Token: 0x0600121F RID: 4639 RVA: 0x00049149 File Offset: 0x00047349
		// (set) Token: 0x06001220 RID: 4640 RVA: 0x00049151 File Offset: 0x00047351
		[DataSourceProperty]
		public int CurrentOverlayType
		{
			get
			{
				return this._currentOverlayType;
			}
			set
			{
				if (value != this._currentOverlayType)
				{
					this._currentOverlayType = value;
					base.OnPropertyChangedWithValue(value, "CurrentOverlayType");
				}
			}
		}

		// Token: 0x06001221 RID: 4641 RVA: 0x0004916F File Offset: 0x0004736F
		public void SetExitInputKey(HotKey hotKey)
		{
			this.ExitInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x170005F1 RID: 1521
		// (get) Token: 0x06001222 RID: 4642 RVA: 0x0004917E File Offset: 0x0004737E
		// (set) Token: 0x06001223 RID: 4643 RVA: 0x00049186 File Offset: 0x00047386
		[DataSourceProperty]
		public InputKeyItemVM ExitInputKey
		{
			get
			{
				return this._exitInputKey;
			}
			set
			{
				if (value != this._exitInputKey)
				{
					this._exitInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ExitInputKey");
				}
			}
		}

		// Token: 0x04000842 RID: 2114
		public string GameMenuOverlayName;

		// Token: 0x04000843 RID: 2115
		private bool _closedHandled = true;

		// Token: 0x04000844 RID: 2116
		private bool _isContextMenuEnabled;

		// Token: 0x04000845 RID: 2117
		private int _currentOverlayType = -1;

		// Token: 0x04000846 RID: 2118
		private bool _isInfoBarExtended;

		// Token: 0x04000847 RID: 2119
		private bool _isInitializationOver;

		// Token: 0x04000848 RID: 2120
		private MBBindingList<StringItemWithEnabledAndHintVM> _contextList;

		// Token: 0x04000849 RID: 2121
		protected GameMenuPartyItemVM _contextMenuItem;

		// Token: 0x0400084A RID: 2122
		private InputKeyItemVM _exitInputKey;

		// Token: 0x0200022D RID: 557
		protected internal enum MenuOverlayContextList
		{
			// Token: 0x04001204 RID: 4612
			Encyclopedia,
			// Token: 0x04001205 RID: 4613
			Conversation,
			// Token: 0x04001206 RID: 4614
			QuickConversation,
			// Token: 0x04001207 RID: 4615
			ConverseWithLeader,
			// Token: 0x04001208 RID: 4616
			ArmyDismiss,
			// Token: 0x04001209 RID: 4617
			ManageGarrison,
			// Token: 0x0400120A RID: 4618
			DonateTroops,
			// Token: 0x0400120B RID: 4619
			JoinArmy,
			// Token: 0x0400120C RID: 4620
			TakeToParty,
			// Token: 0x0400120D RID: 4621
			ManageTroops
		}
	}
}
