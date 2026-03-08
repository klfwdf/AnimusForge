using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000129 RID: 297
	public class ClanPartyItemVM : ViewModel
	{
		// Token: 0x17000946 RID: 2374
		// (get) Token: 0x06001B5F RID: 7007 RVA: 0x0006573E File Offset: 0x0006393E
		// (set) Token: 0x06001B60 RID: 7008 RVA: 0x00065746 File Offset: 0x00063946
		public int Expense { get; private set; }

		// Token: 0x17000947 RID: 2375
		// (get) Token: 0x06001B61 RID: 7009 RVA: 0x0006574F File Offset: 0x0006394F
		// (set) Token: 0x06001B62 RID: 7010 RVA: 0x00065757 File Offset: 0x00063957
		public int Income { get; private set; }

		// Token: 0x17000948 RID: 2376
		// (get) Token: 0x06001B63 RID: 7011 RVA: 0x00065760 File Offset: 0x00063960
		public PartyBase Party { get; }

		// Token: 0x06001B64 RID: 7012 RVA: 0x00065768 File Offset: 0x00063968
		public ClanPartyItemVM(PartyBase party, Action<ClanPartyItemVM> onAssignment, Action onExpenseChange, Action onShowChangeLeaderPopup, ClanPartyItemVM.ClanPartyType type, IDisbandPartyCampaignBehavior disbandBehavior, ITeleportationCampaignBehavior teleportationBehavior)
		{
			this.Party = party;
			this._type = type;
			this._disbandBehavior = disbandBehavior;
			this._leader = CampaignUIHelper.GetVisualPartyLeader(this.Party);
			this.HasHeroMembers = party.IsMobile;
			if (this._leader == null)
			{
				TroopRosterElement troopRosterElement = this.Party.MemberRoster.GetTroopRoster().FirstOrDefault<TroopRosterElement>();
				if (!troopRosterElement.Equals(default(TroopRosterElement)))
				{
					this._leader = troopRosterElement.Character;
				}
				else
				{
					IFaction mapFaction = this.Party.MapFaction;
					this._leader = ((mapFaction != null) ? mapFaction.BasicTroop : null);
				}
			}
			CharacterObject leader = this._leader;
			if ((leader == null || !leader.IsHero) && party.IsMobile && (this._type == ClanPartyItemVM.ClanPartyType.Member || this._type == ClanPartyItemVM.ClanPartyType.Caravan))
			{
				Hero teleportingLeaderHero = CampaignUIHelper.GetTeleportingLeaderHero(party.MobileParty, teleportationBehavior);
				this._leader = ((teleportingLeaderHero != null) ? teleportingLeaderHero.CharacterObject : null);
				this._isLeaderTeleporting = this._leader != null;
			}
			if (this._leader != null)
			{
				CharacterCode characterCode = ClanPartyItemVM.GetCharacterCode(this._leader);
				this.LeaderVisual = new CharacterImageIdentifierVM(characterCode);
				this.CharacterModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
				CharacterViewModel characterModel = this.CharacterModel;
				BasicCharacterObject leader2 = this._leader;
				int seed = -1;
				Banner banner = this.Party.Banner;
				characterModel.FillFrom(leader2, seed, (banner != null) ? banner.BannerCode : null);
				CharacterViewModel characterModel2 = this.CharacterModel;
				IFaction mapFaction2 = this.Party.MapFaction;
				characterModel2.ArmorColor1 = ((mapFaction2 != null) ? mapFaction2.Color : 0U);
				CharacterViewModel characterModel3 = this.CharacterModel;
				IFaction mapFaction3 = this.Party.MapFaction;
				characterModel3.ArmorColor2 = ((mapFaction3 != null) ? mapFaction3.Color2 : 0U);
			}
			else
			{
				this.LeaderVisual = new CharacterImageIdentifierVM(null);
				this.CharacterModel = new CharacterViewModel();
			}
			this._onAssignment = onAssignment;
			this._onExpenseChange = onExpenseChange;
			this._onShowChangeLeaderPopup = onShowChangeLeaderPopup;
			bool isDisbanding;
			if (!this.Party.MobileParty.IsDisbanding)
			{
				IDisbandPartyCampaignBehavior disbandBehavior2 = this._disbandBehavior;
				isDisbanding = disbandBehavior2 != null && disbandBehavior2.IsPartyWaitingForDisband(party.MobileParty);
			}
			else
			{
				isDisbanding = true;
			}
			this.IsDisbanding = isDisbanding;
			bool flag = !party.MobileParty.IsMilitia && !party.MobileParty.IsVillager && party.MobileParty.IsActive && !this.IsDisbanding;
			this.ShouldPartyHaveExpense = flag && (type == ClanPartyItemVM.ClanPartyType.Garrison || type == ClanPartyItemVM.ClanPartyType.Member);
			this.IsCaravan = type == ClanPartyItemVM.ClanPartyType.Caravan;
			TextObject empty = TextObject.GetEmpty();
			this.IsChangeLeaderVisible = type == ClanPartyItemVM.ClanPartyType.Caravan || type == ClanPartyItemVM.ClanPartyType.Member;
			this.IsChangeLeaderEnabled = this.IsChangeLeaderVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out empty);
			this.ChangeLeaderHint = new HintViewModel(this.IsChangeLeaderEnabled ? this._changeLeaderHintText : empty, null);
			if (this.ShouldPartyHaveExpense)
			{
				if (party.MobileParty != null)
				{
					this.ExpenseItem = new ClanFinanceExpenseItemVM(party.MobileParty);
					this.OnExpenseChange();
				}
				else
				{
					Debug.FailedAssert("This party should have expense info but it doesn't", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanPartyItemVM.cs", ".ctor", 116);
				}
			}
			if (this.IsCaravan)
			{
				this.Income = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromCaravan(party.MobileParty);
			}
			this.AutoRecruitmentHint = new HintViewModel(GameTexts.FindText("str_clan_auto_recruitment_hint", null), null);
			this.IsAutoRecruitmentVisible = party.MobileParty.IsGarrison;
			this.AutoRecruitmentValue = party.MobileParty.IsGarrison && this.Party.MobileParty.CurrentSettlement.Town.GarrisonAutoRecruitmentIsEnabled;
			this.HeroMembers = new MBBindingList<ClanPartyMemberItemVM>();
			this.Roles = new MBBindingList<ClanRoleItemVM>();
			this.InfantryHint = new BasicTooltipViewModel(() => this.GetPartyTroopInfo(this.Party, FormationClass.Infantry));
			this.CavalryHint = new BasicTooltipViewModel(() => this.GetPartyTroopInfo(this.Party, FormationClass.Cavalry));
			this.RangedHint = new BasicTooltipViewModel(() => this.GetPartyTroopInfo(this.Party, FormationClass.Ranged));
			this.HorseArcherHint = new BasicTooltipViewModel(() => this.GetPartyTroopInfo(this.Party, FormationClass.HorseArcher));
			this.ActionsDisabledHint = new HintViewModel();
			this.InArmyHint = new HintViewModel();
			this.RefreshValues();
		}

		// Token: 0x06001B65 RID: 7013 RVA: 0x00065B6D File Offset: 0x00063D6D
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.UpdateProperties();
		}

		// Token: 0x06001B66 RID: 7014 RVA: 0x00065B7C File Offset: 0x00063D7C
		public void UpdateProperties()
		{
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.AssigneesText = GameTexts.FindText("str_clan_assignee_title", null).ToString();
			this.RolesText = GameTexts.FindText("str_clan_role_title", null).ToString();
			this.PartyLeaderRoleEffectsText = GameTexts.FindText("str_clan_party_leader_roles_and_effects", null).ToString();
			this.AutoRecruitmentText = GameTexts.FindText("str_clan_auto_recruitment", null).ToString();
			PartyBase party = this.Party;
			this.IsPartyBehaviorEnabled = ((party != null) ? party.LeaderHero : null) != null && this.Party.LeaderHero.Clan.Leader != this.Party.LeaderHero && !this.Party.MobileParty.IsCaravan && !this.IsDisbanding;
			if (this.Party == PartyBase.MainParty && Hero.MainHero.IsPrisoner)
			{
				TextObject textObject = new TextObject("{=shL0WElC}{TROOP.NAME}{.o} Party", null);
				textObject.SetCharacterProperties("TROOP", Hero.MainHero.CharacterObject, false);
				this.Name = textObject.ToString();
			}
			else if (this._isLeaderTeleporting)
			{
				TextObject textObject2 = new TextObject("{=P5YtNXHR}{LEADER.NAME}{.o} Party", null);
				StringHelpers.SetCharacterProperties("LEADER", this._leader, textObject2, false);
				this.Name = textObject2.ToString();
			}
			else
			{
				this.Name = this.Party.Name.ToString();
			}
			this.IsMainHeroParty = this._type == ClanPartyItemVM.ClanPartyType.Main;
			this.PartyLocationText = CampaignUIHelper.GetPartyLocationText(this.Party.MobileParty);
			GameTexts.SetVariable("LEFT", this.Party.MobileParty.MemberRoster.TotalManCount);
			GameTexts.SetVariable("RIGHT", this.Party.MobileParty.Party.PartySizeLimit);
			string text = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
			string content = GameTexts.FindText("str_party_morale_party_size", null).ToString();
			this.PartySizeText = text;
			GameTexts.SetVariable("LEFT", content);
			GameTexts.SetVariable("RIGHT", text);
			this.PartySizeSubTitleText = GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString();
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_party_wage", null));
			GameTexts.SetVariable("RIGHT", this.Party.MobileParty.TotalWage);
			this.PartyWageSubTitleText = GameTexts.FindText("str_LEFT_colon_RIGHT", null).ToString();
			this.InArmyText = "";
			if (this.Party.MobileParty.Army != null)
			{
				this.IsInArmy = true;
				TextObject textObject3 = GameTexts.FindText("str_clan_in_army_hint", null);
				TextObject textObject4 = textObject3;
				string tag = "ARMY_LEADER";
				MobileParty leaderParty = this.Party.MobileParty.Army.LeaderParty;
				string text2;
				if (leaderParty == null)
				{
					text2 = null;
				}
				else
				{
					Hero leaderHero = leaderParty.LeaderHero;
					text2 = ((leaderHero != null) ? leaderHero.Name.ToString() : null);
				}
				textObject4.SetTextVariable(tag, text2 ?? string.Empty);
				this.InArmyHint = new HintViewModel(textObject3, null);
				this.InArmyText = GameTexts.FindText("str_in_army", null).ToString();
			}
			this.DisbandingText = "";
			this.IsMembersAndRolesVisible = !this.IsDisbanding && this._type != ClanPartyItemVM.ClanPartyType.Garrison;
			if (this.IsDisbanding)
			{
				this.DisbandingText = GameTexts.FindText("str_disbanding", null).ToString();
			}
			this.PartyBehaviorText = "";
			if (this.IsPartyBehaviorEnabled)
			{
				this.PartyBehaviorSelector = new ClanPartyBehaviorSelectorVM(0, new Action<SelectorVM<SelectorItemVM>>(this.UpdatePartyBehaviorSelectionUpdate));
				for (int i = 0; i < 3; i++)
				{
					string s = GameTexts.FindText("str_clan_party_objective", i.ToString()).ToString();
					TextObject hint = GameTexts.FindText("str_clan_party_objective_hint", i.ToString());
					this.PartyBehaviorSelector.AddItem(new SelectorItemVM(s, hint));
				}
				this.PartyBehaviorSelector.SelectedIndex = (int)this.Party.MobileParty.Objective;
				this.PartyBehaviorText = GameTexts.FindText("str_clan_party_behavior", null).ToString();
			}
			if (this._leader != null)
			{
				CharacterViewModel characterModel = this.CharacterModel;
				BasicCharacterObject leader = this._leader;
				int seed = -1;
				Banner banner = this.Party.Banner;
				characterModel.FillFrom(leader, seed, (banner != null) ? banner.BannerCode : null);
				CharacterViewModel characterModel2 = this.CharacterModel;
				IFaction mapFaction = this.Party.MapFaction;
				characterModel2.ArmorColor1 = ((mapFaction != null) ? mapFaction.Color : 0U);
				CharacterViewModel characterModel3 = this.CharacterModel;
				IFaction mapFaction2 = this.Party.MapFaction;
				characterModel3.ArmorColor2 = ((mapFaction2 != null) ? mapFaction2.Color2 : 0U);
			}
			this.HeroMembers.Clear();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (TroopRosterElement troopRosterElement in this.Party.MemberRoster.GetTroopRoster())
			{
				Hero heroObject = troopRosterElement.Character.HeroObject;
				if (heroObject != null && heroObject.Clan == Clan.PlayerClan && heroObject.GovernorOf == null)
				{
					ClanPartyMemberItemVM clanPartyMemberItemVM = new ClanPartyMemberItemVM(troopRosterElement.Character.HeroObject, this.Party.MobileParty);
					this.HeroMembers.Add(clanPartyMemberItemVM);
					if (clanPartyMemberItemVM.IsLeader)
					{
						this.LeaderMember = clanPartyMemberItemVM;
					}
				}
				else if (troopRosterElement.Character.DefaultFormationClass.Equals(FormationClass.Infantry))
				{
					num += troopRosterElement.Number;
				}
				else if (troopRosterElement.Character.DefaultFormationClass.Equals(FormationClass.Ranged))
				{
					num2 += troopRosterElement.Number;
				}
				else if (troopRosterElement.Character.DefaultFormationClass.Equals(FormationClass.Cavalry))
				{
					num3 += troopRosterElement.Number;
				}
				else if (troopRosterElement.Character.DefaultFormationClass.Equals(FormationClass.HorseArcher))
				{
					num4 += troopRosterElement.Number;
				}
			}
			if (this._isLeaderTeleporting)
			{
				ClanPartyMemberItemVM clanPartyMemberItemVM2 = new ClanPartyMemberItemVM(this._leader.HeroObject, this.Party.MobileParty);
				this.LeaderMember = clanPartyMemberItemVM2;
				this.HeroMembers.Insert(0, clanPartyMemberItemVM2);
			}
			this.HasCompanion = this.HeroMembers.Count > 1;
			if (this.IsMembersAndRolesVisible)
			{
				this.Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
				{
					x.OnFinalize();
				});
				this.Roles.Clear();
				foreach (PartyRole role in this.GetAssignablePartyRoles())
				{
					this.Roles.Add(new ClanRoleItemVM(this.Party.MobileParty, role, this.HeroMembers, new Action<ClanRoleItemVM>(this.OnRoleSelectionToggled), new Action(this.OnRoleAssigned)));
				}
			}
			this.InfantryCount = num;
			this.RangedCount = num2;
			this.CavalryCount = num3;
			this.HorseArcherCount = num4;
			TextObject textObject5;
			this.CanUseActions = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject5);
			this.ActionsDisabledHint.HintText = (this.CanUseActions ? TextObject.GetEmpty() : textObject5);
			if (!this.CanUseActions)
			{
				this.AutoRecruitmentHint.HintText = this.ActionsDisabledHint.HintText;
				if (this.ExpenseItem != null)
				{
					this.ExpenseItem.IsEnabled = this.CanUseActions;
					this.ExpenseItem.WageLimitHint.HintText = this.ActionsDisabledHint.HintText;
				}
				foreach (ClanRoleItemVM clanRoleItemVM in this.Roles)
				{
					clanRoleItemVM.SetEnabled(false, this.ActionsDisabledHint.HintText);
				}
			}
			if (this.PartyBehaviorSelector != null)
			{
				this.PartyBehaviorSelector.CanUseActions = this.CanUseActions;
				this.PartyBehaviorSelector.ActionsDisabledHint.HintText = this.ActionsDisabledHint.HintText;
			}
			this.ShipCount = this.Party.Ships.Count;
			this.ShipCountText = GameTexts.FindText("str_LEFT_colon_RIGHT", null).SetTextVariable("LEFT", new TextObject("{=URbKirPS}Ship Count", null).ToString()).SetTextVariable("RIGHT", this.ShipCount)
				.ToString();
		}

		// Token: 0x06001B67 RID: 7015 RVA: 0x00066420 File Offset: 0x00064620
		private void OnExpenseChange()
		{
			this._onExpenseChange();
		}

		// Token: 0x06001B68 RID: 7016 RVA: 0x00066430 File Offset: 0x00064630
		public void OnPartySelection()
		{
			int selectedIndex = (this.IsPartyBehaviorEnabled ? this.PartyBehaviorSelector.SelectedIndex : (-1));
			this._onAssignment(this);
			if (this.IsPartyBehaviorEnabled)
			{
				this.PartyBehaviorSelector.SelectedIndex = selectedIndex;
			}
		}

		// Token: 0x06001B69 RID: 7017 RVA: 0x00066474 File Offset: 0x00064674
		public void ExecuteChangeLeader()
		{
			Action onShowChangeLeaderPopup = this._onShowChangeLeaderPopup;
			if (onShowChangeLeaderPopup == null)
			{
				return;
			}
			onShowChangeLeaderPopup();
		}

		// Token: 0x06001B6A RID: 7018 RVA: 0x00066486 File Offset: 0x00064686
		private void OnRoleAssigned()
		{
			this.Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
			{
				x.Refresh();
			});
		}

		// Token: 0x06001B6B RID: 7019 RVA: 0x000664B2 File Offset: 0x000646B2
		private void ExecuteLocationLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x06001B6C RID: 7020 RVA: 0x000664C4 File Offset: 0x000646C4
		private void UpdatePartyBehaviorSelectionUpdate(SelectorVM<SelectorItemVM> s)
		{
			if (s.SelectedIndex != (int)this.Party.MobileParty.Objective)
			{
				this.Party.MobileParty.SetPartyObjective((MobileParty.PartyObjective)s.SelectedIndex);
			}
		}

		// Token: 0x06001B6D RID: 7021 RVA: 0x000664F4 File Offset: 0x000646F4
		private void OnAutoRecruitChanged(bool value)
		{
			if (this.Party.IsMobile && this.Party.MobileParty.IsGarrison)
			{
				Settlement homeSettlement = this.Party.MobileParty.HomeSettlement;
				if (((homeSettlement != null) ? homeSettlement.Town : null) != null)
				{
					this.Party.MobileParty.HomeSettlement.Town.GarrisonAutoRecruitmentIsEnabled = value;
				}
			}
		}

		// Token: 0x06001B6E RID: 7022 RVA: 0x00066559 File Offset: 0x00064759
		private IEnumerable<PartyRole> GetAssignablePartyRoles()
		{
			yield return PartyRole.Quartermaster;
			yield return PartyRole.Scout;
			yield return PartyRole.Surgeon;
			yield return PartyRole.Engineer;
			yield break;
		}

		// Token: 0x06001B6F RID: 7023 RVA: 0x00066562 File Offset: 0x00064762
		private void OnRoleSelectionToggled(ClanRoleItemVM role)
		{
			this.LastOpenedRoleSelection = role;
		}

		// Token: 0x06001B70 RID: 7024 RVA: 0x0006656C File Offset: 0x0006476C
		private static CharacterCode GetCharacterCode(CharacterObject character)
		{
			if (character.IsHero)
			{
				return CampaignUIHelper.GetCharacterCode(character, false);
			}
			uint color = Hero.MainHero.MapFaction.Color;
			uint color2 = Hero.MainHero.MapFaction.Color2;
			Equipment equipment = character.Equipment;
			string equipmentCode = ((equipment != null) ? equipment.CalculateEquipmentCode() : null);
			BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment, -1);
			return CharacterCode.CreateFrom(equipmentCode, bodyProperties, character.IsFemale, character.IsHero, color, color2, character.DefaultFormationClass, character.Race);
		}

		// Token: 0x06001B71 RID: 7025 RVA: 0x000665EC File Offset: 0x000647EC
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.HeroMembers.ApplyActionOnAllItems(delegate(ClanPartyMemberItemVM h)
			{
				h.OnFinalize();
			});
			this.Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
			{
				x.OnFinalize();
			});
		}

		// Token: 0x17000949 RID: 2377
		// (get) Token: 0x06001B72 RID: 7026 RVA: 0x00066653 File Offset: 0x00064853
		// (set) Token: 0x06001B73 RID: 7027 RVA: 0x0006665B File Offset: 0x0006485B
		[DataSourceProperty]
		public CharacterViewModel CharacterModel
		{
			get
			{
				return this._characterModel;
			}
			set
			{
				if (value != this._characterModel)
				{
					this._characterModel = value;
					base.OnPropertyChangedWithValue<CharacterViewModel>(value, "CharacterModel");
				}
			}
		}

		// Token: 0x1700094A RID: 2378
		// (get) Token: 0x06001B74 RID: 7028 RVA: 0x00066679 File Offset: 0x00064879
		// (set) Token: 0x06001B75 RID: 7029 RVA: 0x00066681 File Offset: 0x00064881
		[DataSourceProperty]
		public ClanPartyBehaviorSelectorVM PartyBehaviorSelector
		{
			get
			{
				return this._partyBehaviorSelector;
			}
			set
			{
				if (value != this._partyBehaviorSelector)
				{
					this._partyBehaviorSelector = value;
					base.OnPropertyChangedWithValue<ClanPartyBehaviorSelectorVM>(value, "PartyBehaviorSelector");
				}
			}
		}

		// Token: 0x1700094B RID: 2379
		// (get) Token: 0x06001B76 RID: 7030 RVA: 0x0006669F File Offset: 0x0006489F
		// (set) Token: 0x06001B77 RID: 7031 RVA: 0x000666A7 File Offset: 0x000648A7
		[DataSourceProperty]
		public CharacterImageIdentifierVM LeaderVisual
		{
			get
			{
				return this._leaderVisual;
			}
			set
			{
				if (value != this._leaderVisual)
				{
					this._leaderVisual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "LeaderVisual");
				}
			}
		}

		// Token: 0x1700094C RID: 2380
		// (get) Token: 0x06001B78 RID: 7032 RVA: 0x000666C5 File Offset: 0x000648C5
		// (set) Token: 0x06001B79 RID: 7033 RVA: 0x000666CD File Offset: 0x000648CD
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x1700094D RID: 2381
		// (get) Token: 0x06001B7A RID: 7034 RVA: 0x000666EB File Offset: 0x000648EB
		// (set) Token: 0x06001B7B RID: 7035 RVA: 0x000666F3 File Offset: 0x000648F3
		[DataSourceProperty]
		public bool HasHeroMembers
		{
			get
			{
				return this._hasHeroMembers;
			}
			set
			{
				if (value != this._hasHeroMembers)
				{
					this._hasHeroMembers = value;
					base.OnPropertyChangedWithValue(value, "HasHeroMembers");
				}
			}
		}

		// Token: 0x1700094E RID: 2382
		// (get) Token: 0x06001B7C RID: 7036 RVA: 0x00066711 File Offset: 0x00064911
		// (set) Token: 0x06001B7D RID: 7037 RVA: 0x00066719 File Offset: 0x00064919
		[DataSourceProperty]
		public bool IsClanRoleSelectionHighlightEnabled
		{
			get
			{
				return this._isClanRoleSelectionHighlightEnabled;
			}
			set
			{
				if (value != this._isClanRoleSelectionHighlightEnabled)
				{
					this._isClanRoleSelectionHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsClanRoleSelectionHighlightEnabled");
				}
			}
		}

		// Token: 0x1700094F RID: 2383
		// (get) Token: 0x06001B7E RID: 7038 RVA: 0x00066737 File Offset: 0x00064937
		// (set) Token: 0x06001B7F RID: 7039 RVA: 0x0006673F File Offset: 0x0006493F
		[DataSourceProperty]
		public bool IsRoleSelectionPopupVisible
		{
			get
			{
				return this._isRoleSelectionPopupVisible;
			}
			set
			{
				if (value != this._isRoleSelectionPopupVisible)
				{
					this._isRoleSelectionPopupVisible = value;
					base.OnPropertyChangedWithValue(value, "IsRoleSelectionPopupVisible");
				}
			}
		}

		// Token: 0x17000950 RID: 2384
		// (get) Token: 0x06001B80 RID: 7040 RVA: 0x0006675D File Offset: 0x0006495D
		// (set) Token: 0x06001B81 RID: 7041 RVA: 0x00066765 File Offset: 0x00064965
		[DataSourceProperty]
		public bool IsDisbanding
		{
			get
			{
				return this._isDisbanding;
			}
			set
			{
				if (value != this._isDisbanding)
				{
					this._isDisbanding = value;
					base.OnPropertyChangedWithValue(value, "IsDisbanding");
				}
			}
		}

		// Token: 0x17000951 RID: 2385
		// (get) Token: 0x06001B82 RID: 7042 RVA: 0x00066783 File Offset: 0x00064983
		// (set) Token: 0x06001B83 RID: 7043 RVA: 0x0006678B File Offset: 0x0006498B
		[DataSourceProperty]
		public bool IsInArmy
		{
			get
			{
				return this._isInArmy;
			}
			set
			{
				if (value != this._isInArmy)
				{
					this._isInArmy = value;
					base.OnPropertyChangedWithValue(value, "IsInArmy");
				}
			}
		}

		// Token: 0x17000952 RID: 2386
		// (get) Token: 0x06001B84 RID: 7044 RVA: 0x000667A9 File Offset: 0x000649A9
		// (set) Token: 0x06001B85 RID: 7045 RVA: 0x000667B1 File Offset: 0x000649B1
		[DataSourceProperty]
		public bool CanUseActions
		{
			get
			{
				return this._canUseActions;
			}
			set
			{
				if (value != this._canUseActions)
				{
					this._canUseActions = value;
					base.OnPropertyChangedWithValue(value, "CanUseActions");
				}
			}
		}

		// Token: 0x17000953 RID: 2387
		// (get) Token: 0x06001B86 RID: 7046 RVA: 0x000667CF File Offset: 0x000649CF
		// (set) Token: 0x06001B87 RID: 7047 RVA: 0x000667D7 File Offset: 0x000649D7
		[DataSourceProperty]
		public bool IsChangeLeaderVisible
		{
			get
			{
				return this._isChangeLeaderVisible;
			}
			set
			{
				if (value != this._isChangeLeaderVisible)
				{
					this._isChangeLeaderVisible = value;
					base.OnPropertyChangedWithValue(value, "IsChangeLeaderVisible");
				}
			}
		}

		// Token: 0x17000954 RID: 2388
		// (get) Token: 0x06001B88 RID: 7048 RVA: 0x000667F5 File Offset: 0x000649F5
		// (set) Token: 0x06001B89 RID: 7049 RVA: 0x000667FD File Offset: 0x000649FD
		[DataSourceProperty]
		public bool IsChangeLeaderEnabled
		{
			get
			{
				return this._isChangeLeaderEnabled;
			}
			set
			{
				if (value != this._isChangeLeaderEnabled)
				{
					this._isChangeLeaderEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsChangeLeaderEnabled");
				}
			}
		}

		// Token: 0x17000955 RID: 2389
		// (get) Token: 0x06001B8A RID: 7050 RVA: 0x0006681B File Offset: 0x00064A1B
		// (set) Token: 0x06001B8B RID: 7051 RVA: 0x00066823 File Offset: 0x00064A23
		[DataSourceProperty]
		public HintViewModel ActionsDisabledHint
		{
			get
			{
				return this._actionsDisabledHint;
			}
			set
			{
				if (value != this._actionsDisabledHint)
				{
					this._actionsDisabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ActionsDisabledHint");
				}
			}
		}

		// Token: 0x17000956 RID: 2390
		// (get) Token: 0x06001B8C RID: 7052 RVA: 0x00066841 File Offset: 0x00064A41
		// (set) Token: 0x06001B8D RID: 7053 RVA: 0x00066849 File Offset: 0x00064A49
		[DataSourceProperty]
		public bool IsCaravan
		{
			get
			{
				return this._isCaravan;
			}
			set
			{
				if (value != this._isCaravan)
				{
					this._isCaravan = value;
					base.OnPropertyChangedWithValue(value, "IsCaravan");
				}
			}
		}

		// Token: 0x17000957 RID: 2391
		// (get) Token: 0x06001B8E RID: 7054 RVA: 0x00066867 File Offset: 0x00064A67
		// (set) Token: 0x06001B8F RID: 7055 RVA: 0x0006686F File Offset: 0x00064A6F
		[DataSourceProperty]
		public bool ShouldPartyHaveExpense
		{
			get
			{
				return this._shouldPartyHaveExpense;
			}
			set
			{
				if (value != this._shouldPartyHaveExpense)
				{
					this._shouldPartyHaveExpense = value;
					base.OnPropertyChangedWithValue(value, "ShouldPartyHaveExpense");
				}
			}
		}

		// Token: 0x17000958 RID: 2392
		// (get) Token: 0x06001B90 RID: 7056 RVA: 0x0006688D File Offset: 0x00064A8D
		// (set) Token: 0x06001B91 RID: 7057 RVA: 0x00066895 File Offset: 0x00064A95
		[DataSourceProperty]
		public bool HasCompanion
		{
			get
			{
				return this._hasCompanion;
			}
			set
			{
				if (value != this._hasCompanion)
				{
					this._hasCompanion = value;
					base.OnPropertyChangedWithValue(value, "HasCompanion");
				}
			}
		}

		// Token: 0x17000959 RID: 2393
		// (get) Token: 0x06001B92 RID: 7058 RVA: 0x000668B3 File Offset: 0x00064AB3
		// (set) Token: 0x06001B93 RID: 7059 RVA: 0x000668BB File Offset: 0x00064ABB
		[DataSourceProperty]
		public bool IsAutoRecruitmentVisible
		{
			get
			{
				return this._isAutoRecruitmentVisible;
			}
			set
			{
				if (value != this._isAutoRecruitmentVisible)
				{
					this._isAutoRecruitmentVisible = value;
					base.OnPropertyChangedWithValue(value, "IsAutoRecruitmentVisible");
				}
			}
		}

		// Token: 0x1700095A RID: 2394
		// (get) Token: 0x06001B94 RID: 7060 RVA: 0x000668D9 File Offset: 0x00064AD9
		// (set) Token: 0x06001B95 RID: 7061 RVA: 0x000668E1 File Offset: 0x00064AE1
		[DataSourceProperty]
		public bool AutoRecruitmentValue
		{
			get
			{
				return this._autoRecruitmentValue;
			}
			set
			{
				if (value != this._autoRecruitmentValue)
				{
					this._autoRecruitmentValue = value;
					base.OnPropertyChangedWithValue(value, "AutoRecruitmentValue");
					this.OnAutoRecruitChanged(value);
				}
			}
		}

		// Token: 0x1700095B RID: 2395
		// (get) Token: 0x06001B96 RID: 7062 RVA: 0x00066906 File Offset: 0x00064B06
		// (set) Token: 0x06001B97 RID: 7063 RVA: 0x0006690E File Offset: 0x00064B0E
		[DataSourceProperty]
		public bool IsPartyBehaviorEnabled
		{
			get
			{
				return this._isPartyBehaviorEnabled;
			}
			set
			{
				if (value != this._isPartyBehaviorEnabled)
				{
					this._isPartyBehaviorEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsPartyBehaviorEnabled");
				}
			}
		}

		// Token: 0x1700095C RID: 2396
		// (get) Token: 0x06001B98 RID: 7064 RVA: 0x0006692C File Offset: 0x00064B2C
		// (set) Token: 0x06001B99 RID: 7065 RVA: 0x00066934 File Offset: 0x00064B34
		[DataSourceProperty]
		public bool IsMembersAndRolesVisible
		{
			get
			{
				return this._isMembersAndRolesVisible;
			}
			set
			{
				if (value != this._isMembersAndRolesVisible)
				{
					this._isMembersAndRolesVisible = value;
					base.OnPropertyChangedWithValue(value, "IsMembersAndRolesVisible");
				}
			}
		}

		// Token: 0x1700095D RID: 2397
		// (get) Token: 0x06001B9A RID: 7066 RVA: 0x00066952 File Offset: 0x00064B52
		// (set) Token: 0x06001B9B RID: 7067 RVA: 0x0006695A File Offset: 0x00064B5A
		[DataSourceProperty]
		public bool IsMainHeroParty
		{
			get
			{
				return this._isMainHeroParty;
			}
			set
			{
				if (value != this._isMainHeroParty)
				{
					this._isMainHeroParty = value;
					base.OnPropertyChangedWithValue(value, "IsMainHeroParty");
				}
			}
		}

		// Token: 0x1700095E RID: 2398
		// (get) Token: 0x06001B9C RID: 7068 RVA: 0x00066978 File Offset: 0x00064B78
		// (set) Token: 0x06001B9D RID: 7069 RVA: 0x00066980 File Offset: 0x00064B80
		[DataSourceProperty]
		public ClanFinanceExpenseItemVM ExpenseItem
		{
			get
			{
				return this._expenseItem;
			}
			set
			{
				if (value != this._expenseItem)
				{
					this._expenseItem = value;
					base.OnPropertyChangedWithValue<ClanFinanceExpenseItemVM>(value, "ExpenseItem");
				}
			}
		}

		// Token: 0x1700095F RID: 2399
		// (get) Token: 0x06001B9E RID: 7070 RVA: 0x0006699E File Offset: 0x00064B9E
		// (set) Token: 0x06001B9F RID: 7071 RVA: 0x000669A6 File Offset: 0x00064BA6
		[DataSourceProperty]
		public ClanRoleItemVM LastOpenedRoleSelection
		{
			get
			{
				return this._lastOpenedRoleSelection;
			}
			set
			{
				if (value != this._lastOpenedRoleSelection)
				{
					this._lastOpenedRoleSelection = value;
					base.OnPropertyChangedWithValue<ClanRoleItemVM>(value, "LastOpenedRoleSelection");
				}
			}
		}

		// Token: 0x17000960 RID: 2400
		// (get) Token: 0x06001BA0 RID: 7072 RVA: 0x000669C4 File Offset: 0x00064BC4
		// (set) Token: 0x06001BA1 RID: 7073 RVA: 0x000669CC File Offset: 0x00064BCC
		[DataSourceProperty]
		public ClanPartyMemberItemVM LeaderMember
		{
			get
			{
				return this._leaderMember;
			}
			set
			{
				if (value != this._leaderMember)
				{
					this._leaderMember = value;
					base.OnPropertyChangedWithValue<ClanPartyMemberItemVM>(value, "LeaderMember");
				}
			}
		}

		// Token: 0x17000961 RID: 2401
		// (get) Token: 0x06001BA2 RID: 7074 RVA: 0x000669EA File Offset: 0x00064BEA
		// (set) Token: 0x06001BA3 RID: 7075 RVA: 0x000669F2 File Offset: 0x00064BF2
		[DataSourceProperty]
		public string PartySizeText
		{
			get
			{
				return this._partySizeText;
			}
			set
			{
				if (value != this._partySizeText)
				{
					this._partySizeText = value;
					base.OnPropertyChanged("PartyStrengthText");
				}
			}
		}

		// Token: 0x17000962 RID: 2402
		// (get) Token: 0x06001BA4 RID: 7076 RVA: 0x00066A14 File Offset: 0x00064C14
		// (set) Token: 0x06001BA5 RID: 7077 RVA: 0x00066A1C File Offset: 0x00064C1C
		[DataSourceProperty]
		public string ShipCountText
		{
			get
			{
				return this._shipCountText;
			}
			set
			{
				if (value != this._shipCountText)
				{
					this._shipCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "ShipCountText");
				}
			}
		}

		// Token: 0x17000963 RID: 2403
		// (get) Token: 0x06001BA6 RID: 7078 RVA: 0x00066A3F File Offset: 0x00064C3F
		// (set) Token: 0x06001BA7 RID: 7079 RVA: 0x00066A47 File Offset: 0x00064C47
		[DataSourceProperty]
		public string MembersText
		{
			get
			{
				return this._membersText;
			}
			set
			{
				if (value != null)
				{
					this._membersText = value;
					base.OnPropertyChangedWithValue<string>(value, "MembersText");
				}
			}
		}

		// Token: 0x17000964 RID: 2404
		// (get) Token: 0x06001BA8 RID: 7080 RVA: 0x00066A5F File Offset: 0x00064C5F
		// (set) Token: 0x06001BA9 RID: 7081 RVA: 0x00066A67 File Offset: 0x00064C67
		[DataSourceProperty]
		public string AssigneesText
		{
			get
			{
				return this._assigneesText;
			}
			set
			{
				if (value != this._assigneesText)
				{
					this._assigneesText = value;
					base.OnPropertyChangedWithValue<string>(value, "AssigneesText");
				}
			}
		}

		// Token: 0x17000965 RID: 2405
		// (get) Token: 0x06001BAA RID: 7082 RVA: 0x00066A8A File Offset: 0x00064C8A
		// (set) Token: 0x06001BAB RID: 7083 RVA: 0x00066A92 File Offset: 0x00064C92
		[DataSourceProperty]
		public string RolesText
		{
			get
			{
				return this._rolesText;
			}
			set
			{
				if (value != this._rolesText)
				{
					this._rolesText = value;
					base.OnPropertyChangedWithValue<string>(value, "RolesText");
				}
			}
		}

		// Token: 0x17000966 RID: 2406
		// (get) Token: 0x06001BAC RID: 7084 RVA: 0x00066AB5 File Offset: 0x00064CB5
		// (set) Token: 0x06001BAD RID: 7085 RVA: 0x00066ABD File Offset: 0x00064CBD
		[DataSourceProperty]
		public string PartyLeaderRoleEffectsText
		{
			get
			{
				return this._partyLeaderRoleEffectsText;
			}
			set
			{
				if (value != this._partyLeaderRoleEffectsText)
				{
					this._partyLeaderRoleEffectsText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyLeaderRoleEffectsText");
				}
			}
		}

		// Token: 0x17000967 RID: 2407
		// (get) Token: 0x06001BAE RID: 7086 RVA: 0x00066AE0 File Offset: 0x00064CE0
		// (set) Token: 0x06001BAF RID: 7087 RVA: 0x00066AE8 File Offset: 0x00064CE8
		[DataSourceProperty]
		public string PartyLocationText
		{
			get
			{
				return this._partyLocationText;
			}
			set
			{
				if (value != this._partyLocationText)
				{
					this._partyLocationText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyLocationText");
				}
			}
		}

		// Token: 0x17000968 RID: 2408
		// (get) Token: 0x06001BB0 RID: 7088 RVA: 0x00066B0B File Offset: 0x00064D0B
		// (set) Token: 0x06001BB1 RID: 7089 RVA: 0x00066B13 File Offset: 0x00064D13
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000969 RID: 2409
		// (get) Token: 0x06001BB2 RID: 7090 RVA: 0x00066B36 File Offset: 0x00064D36
		// (set) Token: 0x06001BB3 RID: 7091 RVA: 0x00066B3E File Offset: 0x00064D3E
		[DataSourceProperty]
		public string PartySizeSubTitleText
		{
			get
			{
				return this._partySizeSubTitleText;
			}
			set
			{
				if (value != this._partySizeSubTitleText)
				{
					this._partySizeSubTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartySizeSubTitleText");
				}
			}
		}

		// Token: 0x1700096A RID: 2410
		// (get) Token: 0x06001BB4 RID: 7092 RVA: 0x00066B61 File Offset: 0x00064D61
		// (set) Token: 0x06001BB5 RID: 7093 RVA: 0x00066B69 File Offset: 0x00064D69
		[DataSourceProperty]
		public string PartyWageSubTitleText
		{
			get
			{
				return this._partyWageSubTitleText;
			}
			set
			{
				if (value != this._partyWageSubTitleText)
				{
					this._partyWageSubTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyWageSubTitleText");
				}
			}
		}

		// Token: 0x1700096B RID: 2411
		// (get) Token: 0x06001BB6 RID: 7094 RVA: 0x00066B8C File Offset: 0x00064D8C
		// (set) Token: 0x06001BB7 RID: 7095 RVA: 0x00066B94 File Offset: 0x00064D94
		[DataSourceProperty]
		public string PartyBehaviorText
		{
			get
			{
				return this._partyBehaviorText;
			}
			set
			{
				if (value != this._partyBehaviorText)
				{
					this._partyBehaviorText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyBehaviorText");
				}
			}
		}

		// Token: 0x1700096C RID: 2412
		// (get) Token: 0x06001BB8 RID: 7096 RVA: 0x00066BB7 File Offset: 0x00064DB7
		// (set) Token: 0x06001BB9 RID: 7097 RVA: 0x00066BBF File Offset: 0x00064DBF
		[DataSourceProperty]
		public int InfantryCount
		{
			get
			{
				return this._infantryCount;
			}
			set
			{
				if (value != this._infantryCount)
				{
					this._infantryCount = value;
					base.OnPropertyChangedWithValue(value, "InfantryCount");
				}
			}
		}

		// Token: 0x1700096D RID: 2413
		// (get) Token: 0x06001BBA RID: 7098 RVA: 0x00066BDD File Offset: 0x00064DDD
		// (set) Token: 0x06001BBB RID: 7099 RVA: 0x00066BE5 File Offset: 0x00064DE5
		[DataSourceProperty]
		public int RangedCount
		{
			get
			{
				return this._rangedCount;
			}
			set
			{
				if (value != this._rangedCount)
				{
					this._rangedCount = value;
					base.OnPropertyChangedWithValue(value, "RangedCount");
				}
			}
		}

		// Token: 0x1700096E RID: 2414
		// (get) Token: 0x06001BBC RID: 7100 RVA: 0x00066C03 File Offset: 0x00064E03
		// (set) Token: 0x06001BBD RID: 7101 RVA: 0x00066C0B File Offset: 0x00064E0B
		[DataSourceProperty]
		public int CavalryCount
		{
			get
			{
				return this._cavalryCount;
			}
			set
			{
				if (value != this._cavalryCount)
				{
					this._cavalryCount = value;
					base.OnPropertyChangedWithValue(value, "CavalryCount");
				}
			}
		}

		// Token: 0x1700096F RID: 2415
		// (get) Token: 0x06001BBE RID: 7102 RVA: 0x00066C29 File Offset: 0x00064E29
		// (set) Token: 0x06001BBF RID: 7103 RVA: 0x00066C31 File Offset: 0x00064E31
		[DataSourceProperty]
		public int HorseArcherCount
		{
			get
			{
				return this._horseArcherCount;
			}
			set
			{
				if (value != this._horseArcherCount)
				{
					this._horseArcherCount = value;
					base.OnPropertyChangedWithValue(value, "HorseArcherCount");
				}
			}
		}

		// Token: 0x17000970 RID: 2416
		// (get) Token: 0x06001BC0 RID: 7104 RVA: 0x00066C4F File Offset: 0x00064E4F
		// (set) Token: 0x06001BC1 RID: 7105 RVA: 0x00066C57 File Offset: 0x00064E57
		[DataSourceProperty]
		public int ShipCount
		{
			get
			{
				return this._shipCount;
			}
			set
			{
				if (value != this._shipCount)
				{
					this._shipCount = value;
					base.OnPropertyChangedWithValue(value, "ShipCount");
				}
			}
		}

		// Token: 0x17000971 RID: 2417
		// (get) Token: 0x06001BC2 RID: 7106 RVA: 0x00066C75 File Offset: 0x00064E75
		// (set) Token: 0x06001BC3 RID: 7107 RVA: 0x00066C7D File Offset: 0x00064E7D
		[DataSourceProperty]
		public string InArmyText
		{
			get
			{
				return this._inArmyText;
			}
			set
			{
				if (value != this._inArmyText)
				{
					this._inArmyText = value;
					base.OnPropertyChangedWithValue<string>(value, "InArmyText");
				}
			}
		}

		// Token: 0x17000972 RID: 2418
		// (get) Token: 0x06001BC4 RID: 7108 RVA: 0x00066CA0 File Offset: 0x00064EA0
		// (set) Token: 0x06001BC5 RID: 7109 RVA: 0x00066CA8 File Offset: 0x00064EA8
		[DataSourceProperty]
		public string DisbandingText
		{
			get
			{
				return this._disbandingText;
			}
			set
			{
				if (value != this._disbandingText)
				{
					this._disbandingText = value;
					base.OnPropertyChangedWithValue<string>(value, "DisbandingText");
				}
			}
		}

		// Token: 0x17000973 RID: 2419
		// (get) Token: 0x06001BC6 RID: 7110 RVA: 0x00066CCB File Offset: 0x00064ECB
		// (set) Token: 0x06001BC7 RID: 7111 RVA: 0x00066CD3 File Offset: 0x00064ED3
		[DataSourceProperty]
		public string AutoRecruitmentText
		{
			get
			{
				return this._autoRecruitmentText;
			}
			set
			{
				if (value != this._autoRecruitmentText)
				{
					this._autoRecruitmentText = value;
					base.OnPropertyChangedWithValue<string>(value, "AutoRecruitmentText");
				}
			}
		}

		// Token: 0x17000974 RID: 2420
		// (get) Token: 0x06001BC8 RID: 7112 RVA: 0x00066CF6 File Offset: 0x00064EF6
		// (set) Token: 0x06001BC9 RID: 7113 RVA: 0x00066CFE File Offset: 0x00064EFE
		[DataSourceProperty]
		public HintViewModel AutoRecruitmentHint
		{
			get
			{
				return this._autoRecruitmentHint;
			}
			set
			{
				if (value != this._autoRecruitmentHint)
				{
					this._autoRecruitmentHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "AutoRecruitmentHint");
				}
			}
		}

		// Token: 0x17000975 RID: 2421
		// (get) Token: 0x06001BCA RID: 7114 RVA: 0x00066D1C File Offset: 0x00064F1C
		// (set) Token: 0x06001BCB RID: 7115 RVA: 0x00066D24 File Offset: 0x00064F24
		[DataSourceProperty]
		public HintViewModel InArmyHint
		{
			get
			{
				return this._inArmyHint;
			}
			set
			{
				if (value != this._inArmyHint)
				{
					this._inArmyHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "InArmyHint");
				}
			}
		}

		// Token: 0x17000976 RID: 2422
		// (get) Token: 0x06001BCC RID: 7116 RVA: 0x00066D42 File Offset: 0x00064F42
		// (set) Token: 0x06001BCD RID: 7117 RVA: 0x00066D4A File Offset: 0x00064F4A
		[DataSourceProperty]
		public HintViewModel ChangeLeaderHint
		{
			get
			{
				return this._changeLeaderHint;
			}
			set
			{
				if (value != this._changeLeaderHint)
				{
					this._changeLeaderHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ChangeLeaderHint");
				}
			}
		}

		// Token: 0x17000977 RID: 2423
		// (get) Token: 0x06001BCE RID: 7118 RVA: 0x00066D68 File Offset: 0x00064F68
		// (set) Token: 0x06001BCF RID: 7119 RVA: 0x00066D70 File Offset: 0x00064F70
		[DataSourceProperty]
		public BasicTooltipViewModel InfantryHint
		{
			get
			{
				return this._infantryHint;
			}
			set
			{
				if (value != this._infantryHint)
				{
					this._infantryHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "InfantryHint");
				}
			}
		}

		// Token: 0x17000978 RID: 2424
		// (get) Token: 0x06001BD0 RID: 7120 RVA: 0x00066D8E File Offset: 0x00064F8E
		// (set) Token: 0x06001BD1 RID: 7121 RVA: 0x00066D96 File Offset: 0x00064F96
		[DataSourceProperty]
		public BasicTooltipViewModel RangedHint
		{
			get
			{
				return this._rangedHint;
			}
			set
			{
				if (value != this._rangedHint)
				{
					this._rangedHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "RangedHint");
				}
			}
		}

		// Token: 0x17000979 RID: 2425
		// (get) Token: 0x06001BD2 RID: 7122 RVA: 0x00066DB4 File Offset: 0x00064FB4
		// (set) Token: 0x06001BD3 RID: 7123 RVA: 0x00066DBC File Offset: 0x00064FBC
		[DataSourceProperty]
		public BasicTooltipViewModel CavalryHint
		{
			get
			{
				return this._cavalryHint;
			}
			set
			{
				if (value != this._cavalryHint)
				{
					this._cavalryHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CavalryHint");
				}
			}
		}

		// Token: 0x1700097A RID: 2426
		// (get) Token: 0x06001BD4 RID: 7124 RVA: 0x00066DDA File Offset: 0x00064FDA
		// (set) Token: 0x06001BD5 RID: 7125 RVA: 0x00066DE2 File Offset: 0x00064FE2
		[DataSourceProperty]
		public BasicTooltipViewModel HorseArcherHint
		{
			get
			{
				return this._horseArcherHint;
			}
			set
			{
				if (value != this._horseArcherHint)
				{
					this._horseArcherHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "HorseArcherHint");
				}
			}
		}

		// Token: 0x1700097B RID: 2427
		// (get) Token: 0x06001BD6 RID: 7126 RVA: 0x00066E00 File Offset: 0x00065000
		// (set) Token: 0x06001BD7 RID: 7127 RVA: 0x00066E08 File Offset: 0x00065008
		[DataSourceProperty]
		public MBBindingList<ClanPartyMemberItemVM> HeroMembers
		{
			get
			{
				return this._heroMembers;
			}
			set
			{
				if (value != this._heroMembers)
				{
					this._heroMembers = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanPartyMemberItemVM>>(value, "HeroMembers");
				}
			}
		}

		// Token: 0x1700097C RID: 2428
		// (get) Token: 0x06001BD8 RID: 7128 RVA: 0x00066E26 File Offset: 0x00065026
		// (set) Token: 0x06001BD9 RID: 7129 RVA: 0x00066E2E File Offset: 0x0006502E
		[DataSourceProperty]
		public MBBindingList<ClanRoleItemVM> Roles
		{
			get
			{
				return this._roles;
			}
			set
			{
				if (value != this._roles)
				{
					this._roles = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanRoleItemVM>>(value, "Roles");
				}
			}
		}

		// Token: 0x06001BDA RID: 7130 RVA: 0x00066E4C File Offset: 0x0006504C
		private List<TooltipProperty> GetPartyTroopInfo(PartyBase party, FormationClass formationClass)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("", GameTexts.FindText("str_formation_class_string", formationClass.GetName()).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			foreach (TroopRosterElement troopRosterElement in this.Party.MemberRoster.GetTroopRoster())
			{
				if (!troopRosterElement.Character.IsHero && troopRosterElement.Character.DefaultFormationClass.Equals(formationClass))
				{
					list.Add(new TooltipProperty(troopRosterElement.Character.Name.ToString(), troopRosterElement.Number.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x04000CC7 RID: 3271
		private readonly Action<ClanPartyItemVM> _onAssignment;

		// Token: 0x04000CC8 RID: 3272
		private readonly Action _onExpenseChange;

		// Token: 0x04000CC9 RID: 3273
		private readonly Action _onShowChangeLeaderPopup;

		// Token: 0x04000CCA RID: 3274
		private readonly ClanPartyItemVM.ClanPartyType _type;

		// Token: 0x04000CCB RID: 3275
		private readonly TextObject _changeLeaderHintText = GameTexts.FindText("str_change_party_leader", null);

		// Token: 0x04000CCC RID: 3276
		private readonly IDisbandPartyCampaignBehavior _disbandBehavior;

		// Token: 0x04000CCD RID: 3277
		private readonly bool _isLeaderTeleporting;

		// Token: 0x04000CCF RID: 3279
		private readonly CharacterObject _leader;

		// Token: 0x04000CD0 RID: 3280
		private ClanPartyBehaviorSelectorVM _partyBehaviorSelector;

		// Token: 0x04000CD1 RID: 3281
		private ClanFinanceExpenseItemVM _expenseItem;

		// Token: 0x04000CD2 RID: 3282
		private ClanRoleItemVM _lastOpenedRoleSelection;

		// Token: 0x04000CD3 RID: 3283
		private ClanPartyMemberItemVM _leaderMember;

		// Token: 0x04000CD4 RID: 3284
		private CharacterImageIdentifierVM _leaderVisual;

		// Token: 0x04000CD5 RID: 3285
		private bool _isMainHeroParty;

		// Token: 0x04000CD6 RID: 3286
		private bool _isSelected;

		// Token: 0x04000CD7 RID: 3287
		private bool _hasHeroMembers;

		// Token: 0x04000CD8 RID: 3288
		private string _partyLocationText;

		// Token: 0x04000CD9 RID: 3289
		private string _partySizeText;

		// Token: 0x04000CDA RID: 3290
		private string _shipCountText;

		// Token: 0x04000CDB RID: 3291
		private string _membersText;

		// Token: 0x04000CDC RID: 3292
		private string _assigneesText;

		// Token: 0x04000CDD RID: 3293
		private string _rolesText;

		// Token: 0x04000CDE RID: 3294
		private string _partyLeaderRoleEffectsText;

		// Token: 0x04000CDF RID: 3295
		private string _name;

		// Token: 0x04000CE0 RID: 3296
		private string _partySizeSubTitleText;

		// Token: 0x04000CE1 RID: 3297
		private string _partyWageSubTitleText;

		// Token: 0x04000CE2 RID: 3298
		private string _partyBehaviorText;

		// Token: 0x04000CE3 RID: 3299
		private int _infantryCount;

		// Token: 0x04000CE4 RID: 3300
		private int _rangedCount;

		// Token: 0x04000CE5 RID: 3301
		private int _cavalryCount;

		// Token: 0x04000CE6 RID: 3302
		private int _horseArcherCount;

		// Token: 0x04000CE7 RID: 3303
		private int _shipCount;

		// Token: 0x04000CE8 RID: 3304
		private string _inArmyText;

		// Token: 0x04000CE9 RID: 3305
		private string _disbandingText;

		// Token: 0x04000CEA RID: 3306
		private string _autoRecruitmentText;

		// Token: 0x04000CEB RID: 3307
		private bool _autoRecruitmentValue;

		// Token: 0x04000CEC RID: 3308
		private bool _isAutoRecruitmentVisible;

		// Token: 0x04000CED RID: 3309
		private bool _shouldPartyHaveExpense;

		// Token: 0x04000CEE RID: 3310
		private bool _hasCompanion;

		// Token: 0x04000CEF RID: 3311
		private bool _isPartyBehaviorEnabled;

		// Token: 0x04000CF0 RID: 3312
		private bool _isMembersAndRolesVisible;

		// Token: 0x04000CF1 RID: 3313
		private bool _isCaravan;

		// Token: 0x04000CF2 RID: 3314
		private bool _isDisbanding;

		// Token: 0x04000CF3 RID: 3315
		private bool _isInArmy;

		// Token: 0x04000CF4 RID: 3316
		private bool _canUseActions;

		// Token: 0x04000CF5 RID: 3317
		private bool _isChangeLeaderVisible;

		// Token: 0x04000CF6 RID: 3318
		private bool _isChangeLeaderEnabled;

		// Token: 0x04000CF7 RID: 3319
		private bool _isClanRoleSelectionHighlightEnabled;

		// Token: 0x04000CF8 RID: 3320
		private bool _isRoleSelectionPopupVisible;

		// Token: 0x04000CF9 RID: 3321
		private HintViewModel _actionsDisabledHint;

		// Token: 0x04000CFA RID: 3322
		private CharacterViewModel _characterModel;

		// Token: 0x04000CFB RID: 3323
		private HintViewModel _autoRecruitmentHint;

		// Token: 0x04000CFC RID: 3324
		private HintViewModel _inArmyHint;

		// Token: 0x04000CFD RID: 3325
		private HintViewModel _changeLeaderHint;

		// Token: 0x04000CFE RID: 3326
		private BasicTooltipViewModel _infantryHint;

		// Token: 0x04000CFF RID: 3327
		private BasicTooltipViewModel _rangedHint;

		// Token: 0x04000D00 RID: 3328
		private BasicTooltipViewModel _cavalryHint;

		// Token: 0x04000D01 RID: 3329
		private BasicTooltipViewModel _horseArcherHint;

		// Token: 0x04000D02 RID: 3330
		private MBBindingList<ClanPartyMemberItemVM> _heroMembers;

		// Token: 0x04000D03 RID: 3331
		private MBBindingList<ClanRoleItemVM> _roles;

		// Token: 0x02000283 RID: 643
		public enum ClanPartyType
		{
			// Token: 0x040012C4 RID: 4804
			Main,
			// Token: 0x040012C5 RID: 4805
			Member,
			// Token: 0x040012C6 RID: 4806
			Caravan,
			// Token: 0x040012C7 RID: 4807
			Garrison
		}
	}
}
