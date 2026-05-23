using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x0200013D RID: 317
	public class ClanMembersVM : ViewModel
	{
		// Token: 0x06001D93 RID: 7571 RVA: 0x0006CF34 File Offset: 0x0006B134
		public ClanMembersVM(Action onRefresh, Action<Hero> showHeroOnMap)
		{
			this._onRefresh = onRefresh;
			this._faction = Hero.MainHero.Clan;
			this._showHeroOnMap = showHeroOnMap;
			this._teleportationBehavior = Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>();
			this.Family = new MBBindingList<ClanLordItemVM>();
			this.Companions = new MBBindingList<ClanLordItemVM>();
			MBBindingList<MBBindingList<ClanLordItemVM>> listsToControl = new MBBindingList<MBBindingList<ClanLordItemVM>> { this.Family, this.Companions };
			this.SortController = new ClanMembersSortControllerVM(listsToControl);
			this.RefreshMembersList();
			this.RefreshValues();
		}

		// Token: 0x06001D94 RID: 7572 RVA: 0x0006CFC4 File Offset: 0x0006B1C4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TraitsText = GameTexts.FindText("str_traits_group", null).ToString();
			this.SkillsText = GameTexts.FindText("str_skills", null).ToString();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
			this.Family.ApplyActionOnAllItems(delegate(ClanLordItemVM x)
			{
				x.RefreshValues();
			});
			this.Companions.ApplyActionOnAllItems(delegate(ClanLordItemVM x)
			{
				x.RefreshValues();
			});
			this.SortController.RefreshValues();
		}

		// Token: 0x06001D95 RID: 7573 RVA: 0x0006D090 File Offset: 0x0006B290
		public void RefreshMembersList()
		{
			this.Family.Clear();
			this.Companions.Clear();
			this.SortController.ResetAllStates();
			List<Hero> list = new List<Hero>();
			foreach (Hero hero in this._faction.AliveLords)
			{
				if (!hero.IsDisabled)
				{
					if (hero == Hero.MainHero)
					{
						list.Insert(0, hero);
					}
					else
					{
						list.Add(hero);
					}
				}
			}
			IEnumerable<Hero> enumerable = from m in this._faction.Companions
				where m.IsPlayerCompanion
				select m;
			foreach (Hero hero2 in list)
			{
				this.Family.Add(new ClanLordItemVM(hero2, this._teleportationBehavior, this._showHeroOnMap, new Action<ClanLordItemVM>(this.OnMemberSelection), new Action(this.OnRequestRecall), new Action(this.OnTalkWithMember)));
			}
			foreach (Hero hero3 in enumerable)
			{
				this.Companions.Add(new ClanLordItemVM(hero3, this._teleportationBehavior, this._showHeroOnMap, new Action<ClanLordItemVM>(this.OnMemberSelection), new Action(this.OnRequestRecall), new Action(this.OnTalkWithMember)));
			}
			GameTexts.SetVariable("RANK", GameTexts.FindText("str_family_group", null));
			GameTexts.SetVariable("NUMBER", this.Family.Count);
			this.FamilyText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_companions_group", null));
			GameTexts.SetVariable("LEFT", this._faction.Companions.Count);
			GameTexts.SetVariable("RIGHT", this._faction.CompanionLimit);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis", null));
			this.CompanionsText = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			this.OnMemberSelection(this.GetDefaultMember());
		}

		// Token: 0x06001D96 RID: 7574 RVA: 0x0006D308 File Offset: 0x0006B508
		private ClanLordItemVM GetDefaultMember()
		{
			if (this.Family.Count > 0)
			{
				return this.Family[0];
			}
			if (this.Companions.Count <= 0)
			{
				return null;
			}
			return this.Companions[0];
		}

		// Token: 0x06001D97 RID: 7575 RVA: 0x0006D344 File Offset: 0x0006B544
		public void SelectMember(Hero hero)
		{
			bool flag = false;
			foreach (ClanLordItemVM clanLordItemVM in this.Family)
			{
				if (clanLordItemVM.GetHero() == hero)
				{
					this.OnMemberSelection(clanLordItemVM);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				foreach (ClanLordItemVM clanLordItemVM2 in this.Companions)
				{
					if (clanLordItemVM2.GetHero() == hero)
					{
						this.OnMemberSelection(clanLordItemVM2);
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				foreach (ClanLordItemVM clanLordItemVM3 in this.Family)
				{
					if (clanLordItemVM3.GetHero() == Hero.MainHero)
					{
						this.OnMemberSelection(clanLordItemVM3);
						flag = true;
						break;
					}
				}
			}
		}

		// Token: 0x06001D98 RID: 7576 RVA: 0x0006D440 File Offset: 0x0006B640
		private void OnMemberSelection(ClanLordItemVM member)
		{
			if (this.CurrentSelectedMember != null)
			{
				this.CurrentSelectedMember.IsSelected = false;
			}
			this.CurrentSelectedMember = member;
			if (member != null)
			{
				member.IsSelected = true;
			}
		}

		// Token: 0x06001D99 RID: 7577 RVA: 0x0006D468 File Offset: 0x0006B668
		private void OnRequestRecall()
		{
			ClanLordItemVM currentSelectedMember = this.CurrentSelectedMember;
			Hero hero = ((currentSelectedMember != null) ? currentSelectedMember.GetHero() : null);
			if (hero != null)
			{
				int hours = (int)Math.Ceiling((double)Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, PartyBase.MainParty).ResultNumber);
				MBTextManager.SetTextVariable("TRAVEL_DURATION", CampaignUIHelper.GetHoursAndDaysTextFromHourValue(hours).ToString(), false);
				MBTextManager.SetTextVariable("HERO_NAME", hero.Name.ToString(), false);
				object obj = GameTexts.FindText("str_recall_member", null);
				TextObject textObject = GameTexts.FindText("str_recall_clan_member_inquiry", null);
				InformationManager.ShowInquiry(new InquiryData(obj.ToString(), textObject.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.OnConfirmRecall), null, "", 0f, null, null, null), false, false);
			}
		}

		// Token: 0x06001D9A RID: 7578 RVA: 0x0006D54E File Offset: 0x0006B74E
		private void OnConfirmRecall()
		{
			TeleportHeroAction.ApplyDelayedTeleportToParty(this.CurrentSelectedMember.GetHero(), MobileParty.MainParty);
			Action onRefresh = this._onRefresh;
			if (onRefresh == null)
			{
				return;
			}
			onRefresh();
		}

		// Token: 0x06001D9B RID: 7579 RVA: 0x0006D575 File Offset: 0x0006B775
		private void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x06001D9C RID: 7580 RVA: 0x0006D588 File Offset: 0x0006B788
		private void OnTalkWithMember()
		{
			ClanLordItemVM currentSelectedMember = this.CurrentSelectedMember;
			bool flag;
			if (currentSelectedMember == null)
			{
				flag = null != null;
			}
			else
			{
				Hero hero = currentSelectedMember.GetHero();
				flag = ((hero != null) ? hero.CharacterObject : null) != null;
			}
			if (flag)
			{
				CharacterObject characterObject = this.CurrentSelectedMember.GetHero().CharacterObject;
				LocationComplex locationComplex = LocationComplex.Current;
				if (((locationComplex != null) ? locationComplex.GetLocationOfCharacter(LocationComplex.Current.GetFirstLocationCharacterOfCharacter(characterObject)) : null) == null)
				{
					CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false), new ConversationCharacterData(characterObject, PartyBase.MainParty, false, false, false, false, false, false), "", "", false);
					return;
				}
				Game.Current.GameStateManager.PopState(0);
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false, false, false), new ConversationCharacterData(characterObject, PartyBase.MainParty, false, false, false, false, false, false));
			}
		}

		// Token: 0x06001D9D RID: 7581 RVA: 0x0006D65C File Offset: 0x0006B85C
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.Family.ApplyActionOnAllItems(delegate(ClanLordItemVM f)
			{
				f.OnFinalize();
			});
			this.Companions.ApplyActionOnAllItems(delegate(ClanLordItemVM f)
			{
				f.OnFinalize();
			});
		}

		// Token: 0x17000A0E RID: 2574
		// (get) Token: 0x06001D9E RID: 7582 RVA: 0x0006D6C3 File Offset: 0x0006B8C3
		// (set) Token: 0x06001D9F RID: 7583 RVA: 0x0006D6CB File Offset: 0x0006B8CB
		[DataSourceProperty]
		public bool IsAnyValidMemberSelected
		{
			get
			{
				return this._isAnyValidMemberSelected;
			}
			set
			{
				if (value != this._isAnyValidMemberSelected)
				{
					this._isAnyValidMemberSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyValidMemberSelected");
				}
			}
		}

		// Token: 0x17000A0F RID: 2575
		// (get) Token: 0x06001DA0 RID: 7584 RVA: 0x0006D6E9 File Offset: 0x0006B8E9
		// (set) Token: 0x06001DA1 RID: 7585 RVA: 0x0006D6F1 File Offset: 0x0006B8F1
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

		// Token: 0x17000A10 RID: 2576
		// (get) Token: 0x06001DA2 RID: 7586 RVA: 0x0006D70F File Offset: 0x0006B90F
		// (set) Token: 0x06001DA3 RID: 7587 RVA: 0x0006D717 File Offset: 0x0006B917
		[DataSourceProperty]
		public string FamilyText
		{
			get
			{
				return this._familyText;
			}
			set
			{
				if (value != this._familyText)
				{
					this._familyText = value;
					base.OnPropertyChangedWithValue<string>(value, "FamilyText");
				}
			}
		}

		// Token: 0x17000A11 RID: 2577
		// (get) Token: 0x06001DA4 RID: 7588 RVA: 0x0006D73A File Offset: 0x0006B93A
		// (set) Token: 0x06001DA5 RID: 7589 RVA: 0x0006D742 File Offset: 0x0006B942
		[DataSourceProperty]
		public string TraitsText
		{
			get
			{
				return this._traitsText;
			}
			set
			{
				if (value != this._traitsText)
				{
					this._traitsText = value;
					base.OnPropertyChangedWithValue<string>(value, "TraitsText");
				}
			}
		}

		// Token: 0x17000A12 RID: 2578
		// (get) Token: 0x06001DA6 RID: 7590 RVA: 0x0006D765 File Offset: 0x0006B965
		// (set) Token: 0x06001DA7 RID: 7591 RVA: 0x0006D76D File Offset: 0x0006B96D
		[DataSourceProperty]
		public string SkillsText
		{
			get
			{
				return this._skillsText;
			}
			set
			{
				if (value != this._skillsText)
				{
					this._skillsText = value;
					base.OnPropertyChangedWithValue<string>(value, "SkillsText");
				}
			}
		}

		// Token: 0x17000A13 RID: 2579
		// (get) Token: 0x06001DA8 RID: 7592 RVA: 0x0006D790 File Offset: 0x0006B990
		// (set) Token: 0x06001DA9 RID: 7593 RVA: 0x0006D798 File Offset: 0x0006B998
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x17000A14 RID: 2580
		// (get) Token: 0x06001DAA RID: 7594 RVA: 0x0006D7BB File Offset: 0x0006B9BB
		// (set) Token: 0x06001DAB RID: 7595 RVA: 0x0006D7C3 File Offset: 0x0006B9C3
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._locationText;
			}
			set
			{
				if (value != this._locationText)
				{
					this._locationText = value;
					base.OnPropertyChangedWithValue<string>(value, "LocationText");
				}
			}
		}

		// Token: 0x17000A15 RID: 2581
		// (get) Token: 0x06001DAC RID: 7596 RVA: 0x0006D7E6 File Offset: 0x0006B9E6
		// (set) Token: 0x06001DAD RID: 7597 RVA: 0x0006D7EE File Offset: 0x0006B9EE
		[DataSourceProperty]
		public string CompanionsText
		{
			get
			{
				return this._companionsText;
			}
			set
			{
				if (value != this._companionsText)
				{
					this._companionsText = value;
					base.OnPropertyChangedWithValue<string>(value, "CompanionsText");
				}
			}
		}

		// Token: 0x17000A16 RID: 2582
		// (get) Token: 0x06001DAE RID: 7598 RVA: 0x0006D811 File Offset: 0x0006BA11
		// (set) Token: 0x06001DAF RID: 7599 RVA: 0x0006D819 File Offset: 0x0006BA19
		[DataSourceProperty]
		public MBBindingList<ClanLordItemVM> Companions
		{
			get
			{
				return this._companions;
			}
			set
			{
				if (value != this._companions)
				{
					this._companions = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanLordItemVM>>(value, "Companions");
				}
			}
		}

		// Token: 0x17000A17 RID: 2583
		// (get) Token: 0x06001DB0 RID: 7600 RVA: 0x0006D837 File Offset: 0x0006BA37
		// (set) Token: 0x06001DB1 RID: 7601 RVA: 0x0006D83F File Offset: 0x0006BA3F
		[DataSourceProperty]
		public MBBindingList<ClanLordItemVM> Family
		{
			get
			{
				return this._family;
			}
			set
			{
				if (value != this._family)
				{
					this._family = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanLordItemVM>>(value, "Family");
				}
			}
		}

		// Token: 0x17000A18 RID: 2584
		// (get) Token: 0x06001DB2 RID: 7602 RVA: 0x0006D85D File Offset: 0x0006BA5D
		// (set) Token: 0x06001DB3 RID: 7603 RVA: 0x0006D865 File Offset: 0x0006BA65
		[DataSourceProperty]
		public ClanLordItemVM CurrentSelectedMember
		{
			get
			{
				return this._currentSelectedMember;
			}
			set
			{
				if (value != this._currentSelectedMember)
				{
					this._currentSelectedMember = value;
					base.OnPropertyChangedWithValue<ClanLordItemVM>(value, "CurrentSelectedMember");
					this.IsAnyValidMemberSelected = value != null;
				}
			}
		}

		// Token: 0x17000A19 RID: 2585
		// (get) Token: 0x06001DB4 RID: 7604 RVA: 0x0006D88D File Offset: 0x0006BA8D
		// (set) Token: 0x06001DB5 RID: 7605 RVA: 0x0006D895 File Offset: 0x0006BA95
		[DataSourceProperty]
		public ClanMembersSortControllerVM SortController
		{
			get
			{
				return this._sortController;
			}
			set
			{
				if (value != this._sortController)
				{
					this._sortController = value;
					base.OnPropertyChangedWithValue<ClanMembersSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x04000DD2 RID: 3538
		private readonly Clan _faction;

		// Token: 0x04000DD3 RID: 3539
		private readonly Action _onRefresh;

		// Token: 0x04000DD4 RID: 3540
		private readonly Action<Hero> _showHeroOnMap;

		// Token: 0x04000DD5 RID: 3541
		private readonly ITeleportationCampaignBehavior _teleportationBehavior;

		// Token: 0x04000DD6 RID: 3542
		private bool _isSelected;

		// Token: 0x04000DD7 RID: 3543
		private MBBindingList<ClanLordItemVM> _companions;

		// Token: 0x04000DD8 RID: 3544
		private MBBindingList<ClanLordItemVM> _family;

		// Token: 0x04000DD9 RID: 3545
		private ClanLordItemVM _currentSelectedMember;

		// Token: 0x04000DDA RID: 3546
		private string _familyText;

		// Token: 0x04000DDB RID: 3547
		private string _traitsText;

		// Token: 0x04000DDC RID: 3548
		private string _companionsText;

		// Token: 0x04000DDD RID: 3549
		private string _skillsText;

		// Token: 0x04000DDE RID: 3550
		private string _nameText;

		// Token: 0x04000DDF RID: 3551
		private string _locationText;

		// Token: 0x04000DE0 RID: 3552
		private bool _isAnyValidMemberSelected;

		// Token: 0x04000DE1 RID: 3553
		private ClanMembersSortControllerVM _sortController;
	}
}
