using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement
{
	// Token: 0x02000065 RID: 101
	public class KingdomManagementVM : ViewModel
	{
		// Token: 0x1700021E RID: 542
		// (get) Token: 0x06000790 RID: 1936 RVA: 0x00023793 File Offset: 0x00021993
		// (set) Token: 0x06000791 RID: 1937 RVA: 0x0002379B File Offset: 0x0002199B
		public Kingdom Kingdom { get; private set; }

		// Token: 0x06000792 RID: 1938 RVA: 0x000237A4 File Offset: 0x000219A4
		public KingdomManagementVM(Action onClose, Action onManageArmy, Action<Army> onShowArmyOnMap)
		{
			this._onClose = onClose;
			this._onShowArmyOnMap = onShowArmyOnMap;
			this.Army = new KingdomArmyVM(onManageArmy, new Action(this.OnRefreshDecision), this._onShowArmyOnMap);
			this.Settlement = this.CreateSettlementVM(new Action<KingdomDecision>(this.ForceDecideDecision), new Action<Settlement>(this.OnGrantFief));
			this.Clan = new KingdomClanVM(new Action<KingdomDecision>(this.ForceDecideDecision));
			this.Policy = new KingdomPoliciesVM(new Action<KingdomDecision>(this.ForceDecideDecision));
			this.Diplomacy = new KingdomDiplomacyVM(new Action<KingdomDecision>(this.ForceDecideDecision));
			this.GiftFief = new KingdomGiftFiefPopupVM(new Action(this.OnSettlementGranted));
			this.Decision = new KingdomDecisionsVM(new Action(this.OnRefresh));
			this._categoryCount = 5;
			this._leaveKingdomPermissionEvent = new LeaveKingdomPermissionEvent(new Action<bool, TextObject>(this.OnLeaveKingdomRequest));
			this.SetSelectedCategory(1);
			this.ChangeKingdomNameHint = new HintViewModel();
			this.RefreshValues();
		}

		// Token: 0x06000793 RID: 1939 RVA: 0x000238B0 File Offset: 0x00021AB0
		protected virtual KingdomSettlementVM CreateSettlementVM(Action<KingdomDecision> forceDecision, Action<Settlement> onGrantFief)
		{
			return new KingdomSettlementVM(forceDecision, onGrantFief);
		}

		// Token: 0x06000794 RID: 1940 RVA: 0x000238BC File Offset: 0x00021ABC
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.LeaderText = GameTexts.FindText("str_sort_by_leader_name_label", null).ToString();
			this.ClansText = GameTexts.FindText("str_encyclopedia_clans", null).ToString();
			this.FiefsText = GameTexts.FindText("str_fiefs", null).ToString();
			this.PoliciesText = GameTexts.FindText("str_policies", null).ToString();
			this.ArmiesText = GameTexts.FindText("str_armies", null).ToString();
			this.DiplomacyText = GameTexts.FindText("str_diplomatic_group", null).ToString();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.RefreshDynamicKingdomProperties();
			this.Army.RefreshValues();
			this.Policy.RefreshValues();
			this.Clan.RefreshValues();
			this.Settlement.RefreshValues();
			this.Diplomacy.RefreshValues();
		}

		// Token: 0x06000795 RID: 1941 RVA: 0x000239A8 File Offset: 0x00021BA8
		private void RefreshDynamicKingdomProperties()
		{
			this.Name = ((Hero.MainHero.MapFaction == null) ? new TextObject("{=kQsXUvgO}You are not under a kingdom.", null).ToString() : Hero.MainHero.MapFaction.Name.ToString());
			this.PlayerHasKingdom = Hero.MainHero.MapFaction is Kingdom;
			if (this.PlayerHasKingdom)
			{
				this.Kingdom = Hero.MainHero.MapFaction as Kingdom;
				this.Leader = new HeroVM(this.Kingdom.Leader, false);
				this.KingdomBanner = new BannerImageIdentifierVM(this.Kingdom.Banner, true);
				this._isPlayerTheRuler = this.Kingdom.Leader == Hero.MainHero;
				this.KingdomActionText = (this._isPlayerTheRuler ? GameTexts.FindText("str_abdicate_leadership", null).ToString() : GameTexts.FindText("str_leave_kingdom", null).ToString());
			}
			else
			{
				this.Kingdom = null;
				this.Leader = null;
				this.KingdomBanner = null;
				this._isPlayerTheRuler = false;
				this.KingdomActionText = string.Empty;
			}
			TextObject hintText;
			this.PlayerCanChangeKingdomName = this.GetCanChangeKingdomNameWithReason(out hintText);
			this.ChangeKingdomNameHint.HintText = hintText;
			List<TextObject> kingdomActionDisabledReasons;
			this.IsKingdomActionEnabled = this.GetIsKingdomActionEnabledWithReason(this._isPlayerTheRuler, out kingdomActionDisabledReasons);
			this.KingdomActionHint = new BasicTooltipViewModel(() => CampaignUIHelper.MergeTextObjectsWithNewline(kingdomActionDisabledReasons));
		}

		// Token: 0x06000796 RID: 1942 RVA: 0x00023B14 File Offset: 0x00021D14
		private bool GetCanChangeKingdomNameWithReason(out TextObject disabledReason)
		{
			if (!this.PlayerHasKingdom)
			{
				disabledReason = new TextObject("{=kQsXUvgO}You are not under a kingdom.", null);
				return false;
			}
			if (!this._isPlayerTheRuler)
			{
				disabledReason = new TextObject("{=HFZdseH9}Only the ruler of the kingdom can change its name.", null);
				return false;
			}
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				disabledReason = textObject;
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06000797 RID: 1943 RVA: 0x00023B68 File Offset: 0x00021D68
		private bool GetIsKingdomActionEnabledWithReason(bool isPlayerTheRuler, out List<TextObject> disabledReasons)
		{
			disabledReasons = new List<TextObject>();
			if (!this.PlayerHasKingdom)
			{
				disabledReasons.Add(new TextObject("{=kQsXUvgO}You are not under a kingdom.", null));
				return false;
			}
			List<TextObject> collection;
			if (isPlayerTheRuler && !Campaign.Current.Models.KingdomCreationModel.IsPlayerKingdomAbdicationPossible(out collection))
			{
				disabledReasons.AddRange(collection);
				return false;
			}
			if (!isPlayerTheRuler && MobileParty.MainParty.Army != null)
			{
				disabledReasons.Add(new TextObject("{=4Y8u4JKO}You can't leave the kingdom while in an army", null));
				return false;
			}
			Game.Current.EventManager.TriggerEvent<LeaveKingdomPermissionEvent>(this._leaveKingdomPermissionEvent);
			if (this._mostRecentLeaveKingdomPermission != null && !this._mostRecentLeaveKingdomPermission.GetValueOrDefault().Item1)
			{
				disabledReasons.Add((this._mostRecentLeaveKingdomPermission != null) ? this._mostRecentLeaveKingdomPermission.GetValueOrDefault().Item2 : null);
				return false;
			}
			TextObject item;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out item))
			{
				disabledReasons.Add(item);
				return false;
			}
			return true;
		}

		// Token: 0x06000798 RID: 1944 RVA: 0x00023C4F File Offset: 0x00021E4F
		public void OnRefresh()
		{
			this.RefreshDynamicKingdomProperties();
			this.Army.RefreshArmyList();
			this.Policy.RefreshPolicyList();
			this.Clan.RefreshClan();
			this.Settlement.RefreshSettlementList();
			this.Diplomacy.RefreshDiplomacyList();
		}

		// Token: 0x06000799 RID: 1945 RVA: 0x00023C8E File Offset: 0x00021E8E
		public void OnFrameTick()
		{
			KingdomDecisionsVM decision = this.Decision;
			if (decision == null)
			{
				return;
			}
			decision.OnFrameTick();
		}

		// Token: 0x0600079A RID: 1946 RVA: 0x00023CA0 File Offset: 0x00021EA0
		private void OnRefreshDecision()
		{
			this.Decision.HandleNextDecision();
		}

		// Token: 0x0600079B RID: 1947 RVA: 0x00023CAD File Offset: 0x00021EAD
		private void ForceDecideDecision(KingdomDecision decision)
		{
			this.Decision.RefreshWith(decision);
		}

		// Token: 0x0600079C RID: 1948 RVA: 0x00023CBC File Offset: 0x00021EBC
		private void OnGrantFief(Settlement settlement)
		{
			if (this.Kingdom.Leader == Hero.MainHero)
			{
				this.GiftFief.OpenWith(settlement);
				return;
			}
			string titleText = new TextObject("{=eIGFuGOx}Give Settlement", null).ToString();
			string text = new TextObject("{=rkubGa4K}Are you sure want to give this settlement back to your kingdom?", null).ToString();
			InformationManager.ShowInquiry(new InquiryData(titleText, text, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
			{
				Campaign.Current.KingdomManager.RelinquishSettlementOwnership(settlement);
				this.ForceDecideDecision(this.Kingdom.UnresolvedDecisions[this.Kingdom.UnresolvedDecisions.Count - 1]);
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x0600079D RID: 1949 RVA: 0x00023D6B File Offset: 0x00021F6B
		private void OnSettlementGranted()
		{
			this.Settlement.RefreshSettlementList();
		}

		// Token: 0x0600079E RID: 1950 RVA: 0x00023D78 File Offset: 0x00021F78
		public void ExecuteClose()
		{
			this._onClose();
		}

		// Token: 0x0600079F RID: 1951 RVA: 0x00023D85 File Offset: 0x00021F85
		private void ExecuteShowClan()
		{
			this.SetSelectedCategory(0);
		}

		// Token: 0x060007A0 RID: 1952 RVA: 0x00023D8E File Offset: 0x00021F8E
		private void ExecuteShowFiefs()
		{
			this.SetSelectedCategory(1);
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x00023D97 File Offset: 0x00021F97
		private void ExecuteShowPolicies()
		{
			if (this.PlayerHasKingdom)
			{
				this.SetSelectedCategory(2);
			}
		}

		// Token: 0x060007A2 RID: 1954 RVA: 0x00023DA8 File Offset: 0x00021FA8
		private void ExecuteShowDiplomacy()
		{
			if (this.PlayerHasKingdom)
			{
				this.SetSelectedCategory(4);
			}
		}

		// Token: 0x060007A3 RID: 1955 RVA: 0x00023DB9 File Offset: 0x00021FB9
		private void ExecuteShowArmy()
		{
			this.SetSelectedCategory(3);
		}

		// Token: 0x060007A4 RID: 1956 RVA: 0x00023DC4 File Offset: 0x00021FC4
		private void ExecuteKingdomAction()
		{
			if (this.IsKingdomActionEnabled)
			{
				if (this._isPlayerTheRuler)
				{
					GameTexts.SetVariable("WILL_DESTROY", (this.Kingdom.Clans.Count == 1) ? 1 : 0);
					InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_abdicate_leadership", null).ToString(), GameTexts.FindText("str_abdicate_leadership_question", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.OnConfirmAbdicateLeadership), null, "", 0f, null, null, null), false, false);
					return;
				}
				if (this._mostRecentLeaveKingdomPermission != null && this._mostRecentLeaveKingdomPermission.GetValueOrDefault().Item1 && ((this._mostRecentLeaveKingdomPermission != null) ? this._mostRecentLeaveKingdomPermission.GetValueOrDefault().Item2 : null) != null)
				{
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom", null).ToString(), (this._mostRecentLeaveKingdomPermission != null) ? this._mostRecentLeaveKingdomPermission.GetValueOrDefault().Item2.ToString() : null, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.OnConfirmLeaveKingdom), null, "", 0f, null, null, null), false, false);
					return;
				}
				if (TaleWorlds.CampaignSystem.Clan.PlayerClan.Settlements.Count == 0)
				{
					if (TaleWorlds.CampaignSystem.Clan.PlayerClan.IsUnderMercenaryService)
					{
						TextObject textObject = new TextObject("{=b7muQ9mt}Are you sure you want to end your mercenary contract with the {KINGDOM_INFORMALNAME}?", null);
						textObject.SetTextVariable("KINGDOM_INFORMALNAME", this.Kingdom.InformalName);
						InformationManager.ShowInquiry(new InquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom", null).ToString(), textObject.ToString(), true, true, new TextObject("{=5Unqsx3N}Confirm", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action(this.OnConfirmLeaveKingdom), null, "", 0f, null, null, null), false, false);
						return;
					}
					InformationManager.ShowInquiry(new InquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom", null).ToString(), new TextObject("{=BgqZWbga}The nobles of the realm will dislike you for abandoning your fealty. Are you sure you want to leave the Kingdom?", null).ToString(), true, true, new TextObject("{=5Unqsx3N}Confirm", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action(this.OnConfirmLeaveKingdom), null, "", 0f, null, null, null), false, false);
					return;
				}
				else
				{
					List<InquiryElement> inquiryElements = new List<InquiryElement>
					{
						new InquiryElement("keep", new TextObject("{=z8h0BRAb}Keep all holdings", null).ToString(), null, true, new TextObject("{=lkJfq1ap}Owned settlements remain under your control but nobles will dislike this dishonorable act and the kingdom will declare war on you.", null).ToString()),
						new InquiryElement("dontkeep", new TextObject("{=JIr3Jc7b}Relinquish all holdings", null).ToString(), null, true, new TextObject("{=ZjaSde0X}Owned settlements are returned to the kingdom. This will avert a war and nobles will dislike you less for abandoning your fealty.", null).ToString())
					};
					MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom", null).ToString(), new TextObject("{=xtlIFKaa}Are you sure you want to leave the Kingdom?{newline}If so, choose how you want to leave the kingdom.", null).ToString(), inquiryElements, true, 1, 1, new TextObject("{=5Unqsx3N}Confirm", null).ToString(), string.Empty, new Action<List<InquiryElement>>(this.OnConfirmLeaveKingdomWithOption), null, "", false), false, false);
				}
			}
		}

		// Token: 0x060007A5 RID: 1957 RVA: 0x00024100 File Offset: 0x00022300
		private void OnLeaveKingdomRequest(bool isPossible, TextObject disabledReasonOrWarning)
		{
			this._mostRecentLeaveKingdomPermission = new ValueTuple<bool, TextObject>?(new ValueTuple<bool, TextObject>(isPossible, disabledReasonOrWarning));
		}

		// Token: 0x060007A6 RID: 1958 RVA: 0x00024114 File Offset: 0x00022314
		private void OnConfirmAbdicateLeadership()
		{
			Campaign.Current.KingdomManager.AbdicateTheThrone(this.Kingdom);
			KingdomDecision kingdomDecision = this.Kingdom.UnresolvedDecisions.LastOrDefault<KingdomDecision>();
			if (kingdomDecision != null)
			{
				this.ForceDecideDecision(kingdomDecision);
				return;
			}
			this.ExecuteClose();
		}

		// Token: 0x060007A7 RID: 1959 RVA: 0x00024158 File Offset: 0x00022358
		private void OnConfirmLeaveKingdomWithOption(List<InquiryElement> obj)
		{
			InquiryElement inquiryElement = obj.FirstOrDefault<InquiryElement>();
			if (inquiryElement != null)
			{
				string a = inquiryElement.Identifier as string;
				if (a == "keep")
				{
					ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(TaleWorlds.CampaignSystem.Clan.PlayerClan, true);
				}
				else if (a == "dontkeep")
				{
					ChangeKingdomAction.ApplyByLeaveKingdom(TaleWorlds.CampaignSystem.Clan.PlayerClan, true);
				}
				this.ExecuteClose();
			}
		}

		// Token: 0x060007A8 RID: 1960 RVA: 0x000241B3 File Offset: 0x000223B3
		private void OnConfirmLeaveKingdom()
		{
			if (TaleWorlds.CampaignSystem.Clan.PlayerClan.IsUnderMercenaryService)
			{
				ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(TaleWorlds.CampaignSystem.Clan.PlayerClan, true);
			}
			else
			{
				ChangeKingdomAction.ApplyByLeaveKingdom(TaleWorlds.CampaignSystem.Clan.PlayerClan, true);
			}
			this.ExecuteClose();
		}

		// Token: 0x060007A9 RID: 1961 RVA: 0x000241E0 File Offset: 0x000223E0
		private void ExecuteChangeKingdomName()
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(GameTexts.FindText("str_change_kingdom_name", null).ToString(), string.Empty, true, true, GameTexts.FindText("str_done", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), new Action<string>(this.OnChangeKingdomNameDone), null, false, new Func<string, Tuple<bool, string>>(FactionHelper.IsKingdomNameApplicable), "", ""), false, false);
		}

		// Token: 0x060007AA RID: 1962 RVA: 0x00024254 File Offset: 0x00022454
		private void OnChangeKingdomNameDone(string newKingdomName)
		{
			TextObject variable = new TextObject(newKingdomName, null);
			TextObject textObject = GameTexts.FindText("str_generic_kingdom_name", null);
			TextObject textObject2 = GameTexts.FindText("str_generic_kingdom_short_name", null);
			textObject.SetTextVariable("KINGDOM_NAME", variable);
			textObject2.SetTextVariable("KINGDOM_SHORT_NAME", variable);
			this.Kingdom.ChangeKingdomName(textObject, textObject2);
			this.OnRefresh();
			this.RefreshValues();
		}

		// Token: 0x060007AB RID: 1963 RVA: 0x000242B4 File Offset: 0x000224B4
		public void SelectArmy(Army army)
		{
			this.SetSelectedCategory(3);
			this.Army.SelectArmy(army);
		}

		// Token: 0x060007AC RID: 1964 RVA: 0x000242C9 File Offset: 0x000224C9
		public void SelectSettlement(Settlement settlement)
		{
			this.SetSelectedCategory(1);
			this.Settlement.SelectSettlement(settlement);
		}

		// Token: 0x060007AD RID: 1965 RVA: 0x000242DE File Offset: 0x000224DE
		public void SelectClan(Clan clan)
		{
			this.SetSelectedCategory(0);
			this.Clan.SelectClan(clan);
		}

		// Token: 0x060007AE RID: 1966 RVA: 0x000242F3 File Offset: 0x000224F3
		public void SelectPolicy(PolicyObject policy)
		{
			this.SetSelectedCategory(2);
			this.Policy.SelectPolicy(policy);
		}

		// Token: 0x060007AF RID: 1967 RVA: 0x00024308 File Offset: 0x00022508
		public void SelectKingdom(Kingdom kingdom)
		{
			this.SetSelectedCategory(4);
			this.Diplomacy.SelectKingdom(kingdom);
		}

		// Token: 0x060007B0 RID: 1968 RVA: 0x00024320 File Offset: 0x00022520
		public void SelectPreviousCategory()
		{
			int selectedCategory = ((this._currentCategory == 0) ? (this._categoryCount - 1) : (this._currentCategory - 1));
			this.SetSelectedCategory(selectedCategory);
		}

		// Token: 0x060007B1 RID: 1969 RVA: 0x00024350 File Offset: 0x00022550
		public void SelectNextCategory()
		{
			int selectedCategory = (this._currentCategory + 1) % this._categoryCount;
			this.SetSelectedCategory(selectedCategory);
		}

		// Token: 0x060007B2 RID: 1970 RVA: 0x00024374 File Offset: 0x00022574
		private void SetSelectedCategory(int index)
		{
			this.Clan.Show = false;
			this.Settlement.Show = false;
			this.Policy.Show = false;
			this.Army.Show = false;
			this.Diplomacy.Show = false;
			this._currentCategory = index;
			if (index == 0)
			{
				this.Clan.Show = true;
				return;
			}
			if (index == 1)
			{
				this.Settlement.Show = true;
				return;
			}
			if (index == 2)
			{
				this.Policy.Show = true;
				return;
			}
			if (index == 3)
			{
				this.Army.Show = true;
				return;
			}
			this._currentCategory = 4;
			this.Diplomacy.Show = true;
		}

		// Token: 0x060007B3 RID: 1971 RVA: 0x0002441A File Offset: 0x0002261A
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.PreviousTabInputKey.OnFinalize();
			this.NextTabInputKey.OnFinalize();
			this.Decision.OnFinalize();
			this.Clan.OnFinalize();
		}

		// Token: 0x1700021F RID: 543
		// (get) Token: 0x060007B4 RID: 1972 RVA: 0x00024459 File Offset: 0x00022659
		// (set) Token: 0x060007B5 RID: 1973 RVA: 0x00024461 File Offset: 0x00022661
		[DataSourceProperty]
		public BasicTooltipViewModel KingdomActionHint
		{
			get
			{
				return this._kingdomActionHint;
			}
			set
			{
				if (value != this._kingdomActionHint)
				{
					this._kingdomActionHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "KingdomActionHint");
				}
			}
		}

		// Token: 0x17000220 RID: 544
		// (get) Token: 0x060007B6 RID: 1974 RVA: 0x0002447F File Offset: 0x0002267F
		// (set) Token: 0x060007B7 RID: 1975 RVA: 0x00024487 File Offset: 0x00022687
		[DataSourceProperty]
		public BannerImageIdentifierVM KingdomBanner
		{
			get
			{
				return this._kingdomBanner;
			}
			set
			{
				if (value != this._kingdomBanner)
				{
					this._kingdomBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "KingdomBanner");
				}
			}
		}

		// Token: 0x17000221 RID: 545
		// (get) Token: 0x060007B8 RID: 1976 RVA: 0x000244A5 File Offset: 0x000226A5
		// (set) Token: 0x060007B9 RID: 1977 RVA: 0x000244AD File Offset: 0x000226AD
		[DataSourceProperty]
		public HeroVM Leader
		{
			get
			{
				return this._leader;
			}
			set
			{
				if (value != this._leader)
				{
					this._leader = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Leader");
				}
			}
		}

		// Token: 0x17000222 RID: 546
		// (get) Token: 0x060007BA RID: 1978 RVA: 0x000244CB File Offset: 0x000226CB
		// (set) Token: 0x060007BB RID: 1979 RVA: 0x000244D3 File Offset: 0x000226D3
		[DataSourceProperty]
		public KingdomArmyVM Army
		{
			get
			{
				return this._army;
			}
			set
			{
				if (value != this._army)
				{
					this._army = value;
					base.OnPropertyChangedWithValue<KingdomArmyVM>(value, "Army");
				}
			}
		}

		// Token: 0x17000223 RID: 547
		// (get) Token: 0x060007BC RID: 1980 RVA: 0x000244F1 File Offset: 0x000226F1
		// (set) Token: 0x060007BD RID: 1981 RVA: 0x000244F9 File Offset: 0x000226F9
		[DataSourceProperty]
		public KingdomSettlementVM Settlement
		{
			get
			{
				return this._settlement;
			}
			set
			{
				if (value != this._settlement)
				{
					this._settlement = value;
					base.OnPropertyChangedWithValue<KingdomSettlementVM>(value, "Settlement");
				}
			}
		}

		// Token: 0x17000224 RID: 548
		// (get) Token: 0x060007BE RID: 1982 RVA: 0x00024517 File Offset: 0x00022717
		// (set) Token: 0x060007BF RID: 1983 RVA: 0x0002451F File Offset: 0x0002271F
		[DataSourceProperty]
		public KingdomClanVM Clan
		{
			get
			{
				return this._clan;
			}
			set
			{
				if (value != this._clan)
				{
					this._clan = value;
					base.OnPropertyChangedWithValue<KingdomClanVM>(value, "Clan");
				}
			}
		}

		// Token: 0x17000225 RID: 549
		// (get) Token: 0x060007C0 RID: 1984 RVA: 0x0002453D File Offset: 0x0002273D
		// (set) Token: 0x060007C1 RID: 1985 RVA: 0x00024545 File Offset: 0x00022745
		[DataSourceProperty]
		public KingdomPoliciesVM Policy
		{
			get
			{
				return this._policy;
			}
			set
			{
				if (value != this._policy)
				{
					this._policy = value;
					base.OnPropertyChangedWithValue<KingdomPoliciesVM>(value, "Policy");
				}
			}
		}

		// Token: 0x17000226 RID: 550
		// (get) Token: 0x060007C2 RID: 1986 RVA: 0x00024563 File Offset: 0x00022763
		// (set) Token: 0x060007C3 RID: 1987 RVA: 0x0002456B File Offset: 0x0002276B
		[DataSourceProperty]
		public KingdomDiplomacyVM Diplomacy
		{
			get
			{
				return this._diplomacy;
			}
			set
			{
				if (value != this._diplomacy)
				{
					this._diplomacy = value;
					base.OnPropertyChangedWithValue<KingdomDiplomacyVM>(value, "Diplomacy");
				}
			}
		}

		// Token: 0x17000227 RID: 551
		// (get) Token: 0x060007C4 RID: 1988 RVA: 0x00024589 File Offset: 0x00022789
		// (set) Token: 0x060007C5 RID: 1989 RVA: 0x00024591 File Offset: 0x00022791
		[DataSourceProperty]
		public KingdomGiftFiefPopupVM GiftFief
		{
			get
			{
				return this._giftFief;
			}
			set
			{
				if (value != this._giftFief)
				{
					this._giftFief = value;
					base.OnPropertyChangedWithValue<KingdomGiftFiefPopupVM>(value, "GiftFief");
				}
			}
		}

		// Token: 0x17000228 RID: 552
		// (get) Token: 0x060007C6 RID: 1990 RVA: 0x000245AF File Offset: 0x000227AF
		// (set) Token: 0x060007C7 RID: 1991 RVA: 0x000245B7 File Offset: 0x000227B7
		[DataSourceProperty]
		public KingdomDecisionsVM Decision
		{
			get
			{
				return this._decision;
			}
			set
			{
				if (value != this._decision)
				{
					this._decision = value;
					base.OnPropertyChangedWithValue<KingdomDecisionsVM>(value, "Decision");
				}
			}
		}

		// Token: 0x17000229 RID: 553
		// (get) Token: 0x060007C8 RID: 1992 RVA: 0x000245D5 File Offset: 0x000227D5
		// (set) Token: 0x060007C9 RID: 1993 RVA: 0x000245DD File Offset: 0x000227DD
		[DataSourceProperty]
		public HintViewModel ChangeKingdomNameHint
		{
			get
			{
				return this._changeKingdomNameHint;
			}
			set
			{
				if (value != this._changeKingdomNameHint)
				{
					this._changeKingdomNameHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ChangeKingdomNameHint");
				}
			}
		}

		// Token: 0x1700022A RID: 554
		// (get) Token: 0x060007CA RID: 1994 RVA: 0x000245FB File Offset: 0x000227FB
		// (set) Token: 0x060007CB RID: 1995 RVA: 0x00024603 File Offset: 0x00022803
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

		// Token: 0x1700022B RID: 555
		// (get) Token: 0x060007CC RID: 1996 RVA: 0x00024626 File Offset: 0x00022826
		// (set) Token: 0x060007CD RID: 1997 RVA: 0x0002462E File Offset: 0x0002282E
		[DataSourceProperty]
		public bool CanSwitchTabs
		{
			get
			{
				return this._canSwitchTabs;
			}
			set
			{
				if (value != this._canSwitchTabs)
				{
					this._canSwitchTabs = value;
					base.OnPropertyChangedWithValue(value, "CanSwitchTabs");
				}
			}
		}

		// Token: 0x1700022C RID: 556
		// (get) Token: 0x060007CE RID: 1998 RVA: 0x0002464C File Offset: 0x0002284C
		// (set) Token: 0x060007CF RID: 1999 RVA: 0x00024654 File Offset: 0x00022854
		[DataSourceProperty]
		public bool PlayerHasKingdom
		{
			get
			{
				return this._playerHasKingdom;
			}
			set
			{
				if (value != this._playerHasKingdom)
				{
					this._playerHasKingdom = value;
					base.OnPropertyChangedWithValue(value, "PlayerHasKingdom");
				}
			}
		}

		// Token: 0x1700022D RID: 557
		// (get) Token: 0x060007D0 RID: 2000 RVA: 0x00024672 File Offset: 0x00022872
		// (set) Token: 0x060007D1 RID: 2001 RVA: 0x0002467A File Offset: 0x0002287A
		[DataSourceProperty]
		public bool IsKingdomActionEnabled
		{
			get
			{
				return this._isKingdomActionEnabled;
			}
			set
			{
				if (value != this._isKingdomActionEnabled)
				{
					this._isKingdomActionEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsKingdomActionEnabled");
				}
			}
		}

		// Token: 0x1700022E RID: 558
		// (get) Token: 0x060007D2 RID: 2002 RVA: 0x00024698 File Offset: 0x00022898
		// (set) Token: 0x060007D3 RID: 2003 RVA: 0x000246A0 File Offset: 0x000228A0
		[DataSourceProperty]
		public bool PlayerCanChangeKingdomName
		{
			get
			{
				return this._playerCanChangeKingdomName;
			}
			set
			{
				if (value != this._playerCanChangeKingdomName)
				{
					this._playerCanChangeKingdomName = value;
					base.OnPropertyChangedWithValue(value, "PlayerCanChangeKingdomName");
				}
			}
		}

		// Token: 0x1700022F RID: 559
		// (get) Token: 0x060007D4 RID: 2004 RVA: 0x000246BE File Offset: 0x000228BE
		// (set) Token: 0x060007D5 RID: 2005 RVA: 0x000246C6 File Offset: 0x000228C6
		[DataSourceProperty]
		public string LeaderText
		{
			get
			{
				return this._leaderText;
			}
			set
			{
				if (value != this._leaderText)
				{
					this._leaderText = value;
					base.OnPropertyChangedWithValue<string>(value, "LeaderText");
				}
			}
		}

		// Token: 0x17000230 RID: 560
		// (get) Token: 0x060007D6 RID: 2006 RVA: 0x000246E9 File Offset: 0x000228E9
		// (set) Token: 0x060007D7 RID: 2007 RVA: 0x000246F1 File Offset: 0x000228F1
		[DataSourceProperty]
		public string KingdomActionText
		{
			get
			{
				return this._kingdomActionText;
			}
			set
			{
				if (value != this._kingdomActionText)
				{
					this._kingdomActionText = value;
					base.OnPropertyChangedWithValue<string>(value, "KingdomActionText");
				}
			}
		}

		// Token: 0x17000231 RID: 561
		// (get) Token: 0x060007D8 RID: 2008 RVA: 0x00024714 File Offset: 0x00022914
		// (set) Token: 0x060007D9 RID: 2009 RVA: 0x0002471C File Offset: 0x0002291C
		[DataSourceProperty]
		public string ClansText
		{
			get
			{
				return this._clansText;
			}
			set
			{
				if (value != this._clansText)
				{
					this._clansText = value;
					base.OnPropertyChangedWithValue<string>(value, "ClansText");
				}
			}
		}

		// Token: 0x17000232 RID: 562
		// (get) Token: 0x060007DA RID: 2010 RVA: 0x0002473F File Offset: 0x0002293F
		// (set) Token: 0x060007DB RID: 2011 RVA: 0x00024747 File Offset: 0x00022947
		[DataSourceProperty]
		public string DiplomacyText
		{
			get
			{
				return this._diplomacyText;
			}
			set
			{
				if (value != this._diplomacyText)
				{
					this._diplomacyText = value;
					base.OnPropertyChangedWithValue<string>(value, "DiplomacyText");
				}
			}
		}

		// Token: 0x17000233 RID: 563
		// (get) Token: 0x060007DC RID: 2012 RVA: 0x0002476A File Offset: 0x0002296A
		// (set) Token: 0x060007DD RID: 2013 RVA: 0x00024772 File Offset: 0x00022972
		[DataSourceProperty]
		public string DoneText
		{
			get
			{
				return this._doneText;
			}
			set
			{
				if (value != this._doneText)
				{
					this._doneText = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneText");
				}
			}
		}

		// Token: 0x17000234 RID: 564
		// (get) Token: 0x060007DE RID: 2014 RVA: 0x00024795 File Offset: 0x00022995
		// (set) Token: 0x060007DF RID: 2015 RVA: 0x0002479D File Offset: 0x0002299D
		[DataSourceProperty]
		public string FiefsText
		{
			get
			{
				return this._fiefsText;
			}
			set
			{
				if (value != this._fiefsText)
				{
					this._fiefsText = value;
					base.OnPropertyChangedWithValue<string>(value, "FiefsText");
				}
			}
		}

		// Token: 0x17000235 RID: 565
		// (get) Token: 0x060007E0 RID: 2016 RVA: 0x000247C0 File Offset: 0x000229C0
		// (set) Token: 0x060007E1 RID: 2017 RVA: 0x000247C8 File Offset: 0x000229C8
		[DataSourceProperty]
		public string PoliciesText
		{
			get
			{
				return this._policiesText;
			}
			set
			{
				if (value != this._policiesText)
				{
					this._policiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "PoliciesText");
				}
			}
		}

		// Token: 0x17000236 RID: 566
		// (get) Token: 0x060007E2 RID: 2018 RVA: 0x000247EB File Offset: 0x000229EB
		// (set) Token: 0x060007E3 RID: 2019 RVA: 0x000247F3 File Offset: 0x000229F3
		[DataSourceProperty]
		public string ArmiesText
		{
			get
			{
				return this._armiesText;
			}
			set
			{
				if (value != this._armiesText)
				{
					this._armiesText = value;
					base.OnPropertyChangedWithValue<string>(value, "ArmiesText");
				}
			}
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x00024816 File Offset: 0x00022A16
		public void SetDoneInputKey(HotKey hotkey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
			this.Decision.SetDoneInputKey(hotkey);
			this.GiftFief.SetDoneInputKey(hotkey);
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x0002483D File Offset: 0x00022A3D
		public void SetCancelInputKey(HotKey hotkey)
		{
			this.GiftFief.SetCancelInputKey(hotkey);
		}

		// Token: 0x060007E6 RID: 2022 RVA: 0x0002484B File Offset: 0x00022A4B
		public void SetPreviousTabInputKey(HotKey hotkey)
		{
			this.PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x060007E7 RID: 2023 RVA: 0x0002485A File Offset: 0x00022A5A
		public void SetNextTabInputKey(HotKey hotkey)
		{
			this.NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, true);
		}

		// Token: 0x17000237 RID: 567
		// (get) Token: 0x060007E8 RID: 2024 RVA: 0x00024869 File Offset: 0x00022A69
		// (set) Token: 0x060007E9 RID: 2025 RVA: 0x00024871 File Offset: 0x00022A71
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x17000238 RID: 568
		// (get) Token: 0x060007EA RID: 2026 RVA: 0x0002488F File Offset: 0x00022A8F
		// (set) Token: 0x060007EB RID: 2027 RVA: 0x00024897 File Offset: 0x00022A97
		public InputKeyItemVM PreviousTabInputKey
		{
			get
			{
				return this._previousTabInputKey;
			}
			set
			{
				if (value != this._previousTabInputKey)
				{
					this._previousTabInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "PreviousTabInputKey");
				}
			}
		}

		// Token: 0x17000239 RID: 569
		// (get) Token: 0x060007EC RID: 2028 RVA: 0x000248B5 File Offset: 0x00022AB5
		// (set) Token: 0x060007ED RID: 2029 RVA: 0x000248BD File Offset: 0x00022ABD
		public InputKeyItemVM NextTabInputKey
		{
			get
			{
				return this._nextTabInputKey;
			}
			set
			{
				if (value != this._nextTabInputKey)
				{
					this._nextTabInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "NextTabInputKey");
				}
			}
		}

		// Token: 0x04000345 RID: 837
		private readonly Action _onClose;

		// Token: 0x04000346 RID: 838
		private readonly Action<Army> _onShowArmyOnMap;

		// Token: 0x04000347 RID: 839
		private readonly int _categoryCount;

		// Token: 0x04000348 RID: 840
		private readonly LeaveKingdomPermissionEvent _leaveKingdomPermissionEvent;

		// Token: 0x04000349 RID: 841
		private ValueTuple<bool, TextObject>? _mostRecentLeaveKingdomPermission;

		// Token: 0x0400034A RID: 842
		private int _currentCategory;

		// Token: 0x0400034B RID: 843
		private bool _isPlayerTheRuler;

		// Token: 0x0400034D RID: 845
		private KingdomArmyVM _army;

		// Token: 0x0400034E RID: 846
		private KingdomSettlementVM _settlement;

		// Token: 0x0400034F RID: 847
		private KingdomClanVM _clan;

		// Token: 0x04000350 RID: 848
		private KingdomPoliciesVM _policy;

		// Token: 0x04000351 RID: 849
		private KingdomDiplomacyVM _diplomacy;

		// Token: 0x04000352 RID: 850
		private KingdomGiftFiefPopupVM _giftFief;

		// Token: 0x04000353 RID: 851
		private BannerImageIdentifierVM _kingdomBanner;

		// Token: 0x04000354 RID: 852
		private HeroVM _leader;

		// Token: 0x04000355 RID: 853
		private KingdomDecisionsVM _decision;

		// Token: 0x04000356 RID: 854
		private HintViewModel _changeKingdomNameHint;

		// Token: 0x04000357 RID: 855
		private string _name;

		// Token: 0x04000358 RID: 856
		private bool _canSwitchTabs;

		// Token: 0x04000359 RID: 857
		private bool _playerHasKingdom;

		// Token: 0x0400035A RID: 858
		private bool _isKingdomActionEnabled;

		// Token: 0x0400035B RID: 859
		private bool _playerCanChangeKingdomName;

		// Token: 0x0400035C RID: 860
		private string _kingdomActionText;

		// Token: 0x0400035D RID: 861
		private string _leaderText;

		// Token: 0x0400035E RID: 862
		private string _clansText;

		// Token: 0x0400035F RID: 863
		private string _fiefsText;

		// Token: 0x04000360 RID: 864
		private string _policiesText;

		// Token: 0x04000361 RID: 865
		private string _armiesText;

		// Token: 0x04000362 RID: 866
		private string _diplomacyText;

		// Token: 0x04000363 RID: 867
		private string _doneText;

		// Token: 0x04000364 RID: 868
		private BasicTooltipViewModel _kingdomActionHint;

		// Token: 0x04000365 RID: 869
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000366 RID: 870
		private InputKeyItemVM _previousTabInputKey;

		// Token: 0x04000367 RID: 871
		private InputKeyItemVM _nextTabInputKey;
	}
}
