using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment
{
	// Token: 0x020000B0 RID: 176
	public class RecruitmentVM : ViewModel
	{
		// Token: 0x17000583 RID: 1411
		// (get) Token: 0x060010E9 RID: 4329 RVA: 0x00043E8E File Offset: 0x0004208E
		// (set) Token: 0x060010EA RID: 4330 RVA: 0x00043E96 File Offset: 0x00042096
		public bool IsQuitting { get; private set; }

		// Token: 0x060010EB RID: 4331 RVA: 0x00043EA0 File Offset: 0x000420A0
		public RecruitmentVM()
		{
			this.VolunteerList = new MBBindingList<RecruitVolunteerVM>();
			this.TroopsInCart = new MBBindingList<RecruitVolunteerTroopVM>();
			this.RefreshValues();
			if (Settlement.CurrentSettlement != null)
			{
				this.RefreshScreen();
			}
			Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			RecruitVolunteerTroopVM.OnFocused = (Action<RecruitVolunteerTroopVM>)Delegate.Combine(RecruitVolunteerTroopVM.OnFocused, new Action<RecruitVolunteerTroopVM>(this.OnVolunteerTroopFocusChanged));
			RecruitVolunteerOwnerVM.OnFocused = (Action<RecruitVolunteerOwnerVM>)Delegate.Combine(RecruitVolunteerOwnerVM.OnFocused, new Action<RecruitVolunteerOwnerVM>(this.OnVolunteerOwnerFocusChanged));
		}

		// Token: 0x060010EC RID: 4332 RVA: 0x00043F70 File Offset: 0x00042170
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PartyWageHint = new HintViewModel(GameTexts.FindText("str_weekly_wage", null), null);
			this.TotalWealthHint = new HintViewModel(GameTexts.FindText("str_wealth", null), null);
			this.TotalCostHint = new HintViewModel(GameTexts.FindText("str_total_cost", null), null);
			this.PartyCapacityHint = new HintViewModel();
			this.PartySpeedHint = new BasicTooltipViewModel();
			this.RemainingFoodHint = new HintViewModel();
			this.DoneHint = new HintViewModel();
			this.ResetHint = new HintViewModel(GameTexts.FindText("str_reset", null), null);
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			this.TitleText = GameTexts.FindText("str_recruitment", null).ToString();
			this._recruitAllTextObject = GameTexts.FindText("str_recruit_all", null);
			this.ResetAllText = GameTexts.FindText("str_reset_all", null).ToString();
			this.CancelText = GameTexts.FindText("str_party_cancel", null).ToString();
			this._playerDoesntHaveEnoughMoneyStr = GameTexts.FindText("str_warning_you_dont_have_enough_money", null).ToString();
			this._playerIsOverPartyLimitStr = GameTexts.FindText("str_party_size_limit_exceeded", null).ToString();
			this.VolunteerList.ApplyActionOnAllItems(delegate(RecruitVolunteerVM x)
			{
				x.RefreshValues();
			});
			this.TroopsInCart.ApplyActionOnAllItems(delegate(RecruitVolunteerTroopVM x)
			{
				x.RefreshValues();
			});
			this.SetRecruitAllHint();
			this.UpdateRecruitAllProperties();
			if (Settlement.CurrentSettlement != null)
			{
				this.RefreshScreen();
			}
		}

		// Token: 0x060010ED RID: 4333 RVA: 0x00044110 File Offset: 0x00042310
		public void RefreshScreen()
		{
			this.VolunteerList.Clear();
			this.TroopsInCart.Clear();
			int num = 0;
			this.InitialPartySize = PartyBase.MainParty.NumberOfAllMembers;
			this.RefreshPartyProperties();
			foreach (Hero hero in Settlement.CurrentSettlement.Notables)
			{
				if (hero.CanHaveRecruits)
				{
					MBTextManager.SetTextVariable("INDIVIDUAL_NAME", hero.Name, false);
					List<CharacterObject> volunteerTroopsOfHeroForRecruitment = HeroHelper.GetVolunteerTroopsOfHeroForRecruitment(hero);
					RecruitVolunteerVM item = new RecruitVolunteerVM(hero, volunteerTroopsOfHeroForRecruitment, new Action<RecruitVolunteerVM, RecruitVolunteerTroopVM>(this.OnRecruit), new Action<RecruitVolunteerVM, RecruitVolunteerTroopVM>(this.OnRemoveFromCart));
					this.VolunteerList.Add(item);
					num++;
				}
			}
			this.TotalWealth = Hero.MainHero.Gold;
			this.UpdateRecruitAllProperties();
		}

		// Token: 0x060010EE RID: 4334 RVA: 0x000441F8 File Offset: 0x000423F8
		private void OnRecruit(RecruitVolunteerVM recruitNotable, RecruitVolunteerTroopVM recruitTroop)
		{
			if (!recruitTroop.CanBeRecruited)
			{
				return;
			}
			recruitNotable.OnRecruitMoveToCart(recruitTroop);
			recruitTroop.CanBeRecruited = false;
			this.TroopsInCart.Add(recruitTroop);
			recruitTroop.IsInCart = true;
			CampaignEventDispatcher.Instance.OnPlayerStartRecruitment(recruitTroop.Character);
			this.RefreshPartyProperties();
		}

		// Token: 0x060010EF RID: 4335 RVA: 0x00044248 File Offset: 0x00042448
		private void RefreshPartyProperties()
		{
			int num = this.TroopsInCart.Sum((RecruitVolunteerTroopVM t) => t.Wage);
			this.PartyWage = MobileParty.MainParty.TotalWage;
			if (num > 0)
			{
				this.PartyWageText = CampaignUIHelper.GetValueChangeText((float)this.PartyWage, (float)num, "F0");
			}
			else
			{
				this.PartyWageText = this.PartyWage.ToString();
			}
			double num2 = 0.0;
			if (this.TroopsInCart.Count > 0)
			{
				int num3 = 0;
				int num4 = 0;
				using (IEnumerator<RecruitVolunteerTroopVM> enumerator = this.TroopsInCart.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Character.IsMounted)
						{
							num4++;
						}
						else
						{
							num3++;
						}
					}
				}
				ExplainedNumber finalSpeed = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(MobileParty.MainParty, false, num3, num4);
				ExplainedNumber explainedNumber = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(MobileParty.MainParty, finalSpeed);
				ExplainedNumber finalSpeed2 = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(MobileParty.MainParty, false, 0, 0);
				ExplainedNumber explainedNumber2 = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(MobileParty.MainParty, finalSpeed2);
				num2 = (double)(MathF.Round(explainedNumber.ResultNumber, 1) - MathF.Round(explainedNumber2.ResultNumber, 1));
			}
			this.PartySpeedText = MobileParty.MainParty.Speed.ToString("0.0");
			this.PartySpeedHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartySpeedTooltip(false));
			if (num2 != 0.0)
			{
				this.PartySpeedText = CampaignUIHelper.GetValueChangeText(MobileParty.MainParty.Speed, (float)num2, "0.0");
			}
			int partySizeLimit = PartyBase.MainParty.PartySizeLimit;
			this.CurrentPartySize = PartyBase.MainParty.NumberOfAllMembers + this.TroopsInCart.Count;
			this.PartyCapacity = partySizeLimit;
			this.IsPartyCapacityWarningEnabled = this.CurrentPartySize > this.PartyCapacity;
			GameTexts.SetVariable("LEFT", this.CurrentPartySize.ToString());
			GameTexts.SetVariable("RIGHT", partySizeLimit.ToString());
			this.PartyCapacityText = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
			this.PartyCapacityHint.HintText = new TextObject("{=!}" + PartyBase.MainParty.PartySizeLimitExplainer.ToString(), null);
			float food = MobileParty.MainParty.Food;
			this.RemainingFoodText = MathF.Round(food, 1).ToString();
			float foodChange = MobileParty.MainParty.FoodChange;
			int totalFoodAtInventory = MobileParty.MainParty.TotalFoodAtInventory;
			int numDaysForFoodToLast = MobileParty.MainParty.GetNumDaysForFoodToLast();
			MBTextManager.SetTextVariable("DAY_NUM", numDaysForFoodToLast);
			this.RemainingFoodHint.HintText = GameTexts.FindText("str_food_consumption_tooltip", null);
			this.RemainingFoodHint.HintText.SetTextVariable("DAILY_FOOD_CONSUMPTION", foodChange, 2);
			this.RemainingFoodHint.HintText.SetTextVariable("REMAINING_DAYS", GameTexts.FindText("str_party_food_left", null));
			this.RemainingFoodHint.HintText.SetTextVariable("TOTAL_FOOD_AMOUNT", ((double)totalFoodAtInventory + 0.01 * (double)PartyBase.MainParty.RemainingFoodPercentage).ToString("0.00"));
			this.RemainingFoodHint.HintText.SetTextVariable("TOTAL_FOOD", totalFoodAtInventory);
			int num5 = this.TroopsInCart.Sum((RecruitVolunteerTroopVM t) => t.Cost);
			this.TotalCostText = num5.ToString();
			bool flag = num5 <= Hero.MainHero.Gold;
			this.IsDoneEnabled = flag;
			this.DoneHint.HintText = new TextObject("{=!}" + this.GetDoneHint(flag), null);
			this.UpdateRecruitAllProperties();
		}

		// Token: 0x060010F0 RID: 4336 RVA: 0x0004466C File Offset: 0x0004286C
		public void ExecuteDone()
		{
			if (this.CurrentPartySize <= this.PartyCapacity)
			{
				this.OnDone();
				return;
			}
			GameTexts.SetVariable("newline", "\n");
			string text = GameTexts.FindText("str_party_over_limit_troops", null).ToString();
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=uJro3Bua}Over Limit", null).ToString(), text, true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
			{
				this.OnDone();
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x060010F1 RID: 4337 RVA: 0x00044708 File Offset: 0x00042908
		private void OnDone()
		{
			this.RefreshPartyProperties();
			int num = this.TroopsInCart.Sum((RecruitVolunteerTroopVM t) => t.Cost);
			if (num > Hero.MainHero.Gold)
			{
				Debug.FailedAssert("Execution shouldn't come here. The checks should happen before", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Recruitment\\RecruitmentVM.cs", "OnDone", 229);
				return;
			}
			foreach (RecruitVolunteerTroopVM recruitVolunteerTroopVM in this.TroopsInCart)
			{
				recruitVolunteerTroopVM.Owner.OwnerHero.VolunteerTypes[recruitVolunteerTroopVM.Index] = null;
				MobileParty.MainParty.MemberRoster.AddToCounts(recruitVolunteerTroopVM.Character, 1, false, 0, 0, true, -1);
				CampaignEventDispatcher.Instance.OnUnitRecruited(recruitVolunteerTroopVM.Character, 1);
			}
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, num, true);
			if (num > 0)
			{
				MBTextManager.SetTextVariable("GOLD_AMOUNT", MathF.Abs(num));
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_gold_removed_with_icon", null).ToString(), "event:/ui/notification/coins_negative"));
			}
			this.Deactivate();
		}

		// Token: 0x060010F2 RID: 4338 RVA: 0x00044834 File Offset: 0x00042A34
		public void ExecuteForceQuit()
		{
			if (!this.IsQuitting)
			{
				this.IsQuitting = true;
				if (this.TroopsInCart.Count > 0)
				{
					InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_quit", null).ToString(), GameTexts.FindText("str_quit_question", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
					{
						this.ExecuteReset();
						this.ExecuteDone();
						this.IsQuitting = false;
					}, delegate()
					{
						this.IsQuitting = false;
					}, "", 0f, null, null, null), true, false);
					return;
				}
				this.Deactivate();
			}
		}

		// Token: 0x060010F3 RID: 4339 RVA: 0x000448DC File Offset: 0x00042ADC
		public void ExecuteReset()
		{
			for (int i = this.TroopsInCart.Count - 1; i >= 0; i--)
			{
				this.TroopsInCart[i].ExecuteRemoveFromCart();
			}
		}

		// Token: 0x060010F4 RID: 4340 RVA: 0x00044914 File Offset: 0x00042B14
		public void ExecuteRecruitAll()
		{
			foreach (RecruitVolunteerVM recruitVolunteerVM in this.VolunteerList.ToList<RecruitVolunteerVM>())
			{
				foreach (RecruitVolunteerTroopVM recruitVolunteerTroopVM in recruitVolunteerVM.Troops.ToList<RecruitVolunteerTroopVM>())
				{
					recruitVolunteerTroopVM.ExecuteRecruit();
				}
			}
		}

		// Token: 0x060010F5 RID: 4341 RVA: 0x000449A8 File Offset: 0x00042BA8
		public void Deactivate()
		{
			this.ExecuteReset();
			this.Enabled = false;
		}

		// Token: 0x060010F6 RID: 4342 RVA: 0x000449B8 File Offset: 0x00042BB8
		public override void OnFinalize()
		{
			base.OnFinalize();
			RecruitVolunteerTroopVM.OnFocused = (Action<RecruitVolunteerTroopVM>)Delegate.Remove(RecruitVolunteerTroopVM.OnFocused, new Action<RecruitVolunteerTroopVM>(this.OnVolunteerTroopFocusChanged));
			RecruitVolunteerOwnerVM.OnFocused = (Action<RecruitVolunteerOwnerVM>)Delegate.Remove(RecruitVolunteerOwnerVM.OnFocused, new Action<RecruitVolunteerOwnerVM>(this.OnVolunteerOwnerFocusChanged));
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
			this.CancelInputKey.OnFinalize();
			this.DoneInputKey.OnFinalize();
			this.ResetInputKey.OnFinalize();
			this.RecruitAllInputKey.OnFinalize();
		}

		// Token: 0x060010F7 RID: 4343 RVA: 0x00044A54 File Offset: 0x00042C54
		private void OnRemoveFromCart(RecruitVolunteerVM recruitNotable, RecruitVolunteerTroopVM recruitTroop)
		{
			if (this.TroopsInCart.Any((RecruitVolunteerTroopVM r) => r == recruitTroop))
			{
				recruitNotable.OnRecruitRemovedFromCart(recruitTroop);
				recruitTroop.CanBeRecruited = true;
				recruitTroop.IsInCart = false;
				recruitTroop.IsHiglightEnabled = false;
				this.TroopsInCart.Remove(recruitTroop);
				this.RefreshPartyProperties();
			}
		}

		// Token: 0x060010F8 RID: 4344 RVA: 0x00044ACF File Offset: 0x00042CCF
		private static bool IsBitSet(int num, int bit)
		{
			return 1 == ((num >> bit) & 1);
		}

		// Token: 0x060010F9 RID: 4345 RVA: 0x00044ADC File Offset: 0x00042CDC
		private string GetDoneHint(bool doesPlayerHasEnoughMoney)
		{
			if (!doesPlayerHasEnoughMoney)
			{
				return this._playerDoesntHaveEnoughMoneyStr;
			}
			return null;
		}

		// Token: 0x060010FA RID: 4346 RVA: 0x00044AE9 File Offset: 0x00042CE9
		private void SetRecruitAllHint()
		{
			this.RecruitAllHint = new BasicTooltipViewModel(delegate()
			{
				GameTexts.SetVariable("HOTKEY", this.GetRecruitAllKey());
				GameTexts.SetVariable("TEXT", GameTexts.FindText("str_recruit_all", null));
				return GameTexts.FindText("str_hotkey_with_hint", null).ToString();
			});
		}

		// Token: 0x060010FB RID: 4347 RVA: 0x00044B04 File Offset: 0x00042D04
		private void UpdateRecruitAllProperties()
		{
			int numberOfAvailableRecruits = this.GetNumberOfAvailableRecruits();
			GameTexts.SetVariable("STR", numberOfAvailableRecruits);
			GameTexts.SetVariable("STR1", this._recruitAllTextObject);
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_STR_in_parentheses", null));
			this.RecruitAllText = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			this.CanRecruitAll = numberOfAvailableRecruits > 0;
		}

		// Token: 0x060010FC RID: 4348 RVA: 0x00044B68 File Offset: 0x00042D68
		private int GetNumberOfAvailableRecruits()
		{
			int num = 0;
			foreach (RecruitVolunteerVM recruitVolunteerVM in this.VolunteerList)
			{
				foreach (RecruitVolunteerTroopVM recruitVolunteerTroopVM in recruitVolunteerVM.Troops)
				{
					if (!recruitVolunteerTroopVM.IsInCart && recruitVolunteerTroopVM.CanBeRecruited)
					{
						num++;
					}
				}
			}
			return num;
		}

		// Token: 0x060010FD RID: 4349 RVA: 0x00044BF8 File Offset: 0x00042DF8
		private void OnVolunteerTroopFocusChanged(RecruitVolunteerTroopVM volunteer)
		{
			this.FocusedVolunteerTroop = volunteer;
		}

		// Token: 0x060010FE RID: 4350 RVA: 0x00044C01 File Offset: 0x00042E01
		private void OnVolunteerOwnerFocusChanged(RecruitVolunteerOwnerVM owner)
		{
			this.FocusedVolunteerOwner = owner;
		}

		// Token: 0x060010FF RID: 4351 RVA: 0x00044C0C File Offset: 0x00042E0C
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (obj.NewNotificationElementID != this._latestTutorialElementID)
			{
				if (this._latestTutorialElementID != null && this._isAvailableTroopsHighlightApplied)
				{
					this.SetAvailableTroopsHighlightState(false);
					this._isAvailableTroopsHighlightApplied = false;
				}
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._latestTutorialElementID != null && !this._isAvailableTroopsHighlightApplied && this._latestTutorialElementID == "AvailableTroops")
				{
					this.SetAvailableTroopsHighlightState(true);
					this._isAvailableTroopsHighlightApplied = true;
				}
			}
		}

		// Token: 0x06001100 RID: 4352 RVA: 0x00044C88 File Offset: 0x00042E88
		private void SetAvailableTroopsHighlightState(bool state)
		{
			foreach (RecruitVolunteerVM recruitVolunteerVM in this.VolunteerList)
			{
				foreach (RecruitVolunteerTroopVM recruitVolunteerTroopVM in recruitVolunteerVM.Troops)
				{
					if (recruitVolunteerTroopVM.Wage < Hero.MainHero.Gold && recruitVolunteerTroopVM.PlayerHasEnoughRelation && !recruitVolunteerTroopVM.IsTroopEmpty)
					{
						recruitVolunteerTroopVM.IsHiglightEnabled = state;
					}
				}
			}
		}

		// Token: 0x17000584 RID: 1412
		// (get) Token: 0x06001101 RID: 4353 RVA: 0x00044D2C File Offset: 0x00042F2C
		// (set) Token: 0x06001102 RID: 4354 RVA: 0x00044D34 File Offset: 0x00042F34
		[DataSourceProperty]
		public HintViewModel ResetHint
		{
			get
			{
				return this._resetHint;
			}
			set
			{
				if (value != this._resetHint)
				{
					this._resetHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResetHint");
				}
			}
		}

		// Token: 0x17000585 RID: 1413
		// (get) Token: 0x06001103 RID: 4355 RVA: 0x00044D52 File Offset: 0x00042F52
		// (set) Token: 0x06001104 RID: 4356 RVA: 0x00044D5A File Offset: 0x00042F5A
		[DataSourceProperty]
		public RecruitVolunteerTroopVM FocusedVolunteerTroop
		{
			get
			{
				return this._focusedVolunteerTroop;
			}
			set
			{
				if (value != this._focusedVolunteerTroop)
				{
					this._focusedVolunteerTroop = value;
					base.OnPropertyChangedWithValue<RecruitVolunteerTroopVM>(value, "FocusedVolunteerTroop");
				}
			}
		}

		// Token: 0x17000586 RID: 1414
		// (get) Token: 0x06001105 RID: 4357 RVA: 0x00044D78 File Offset: 0x00042F78
		// (set) Token: 0x06001106 RID: 4358 RVA: 0x00044D80 File Offset: 0x00042F80
		[DataSourceProperty]
		public RecruitVolunteerOwnerVM FocusedVolunteerOwner
		{
			get
			{
				return this._focusedVolunteerOwner;
			}
			set
			{
				if (value != this._focusedVolunteerOwner)
				{
					this._focusedVolunteerOwner = value;
					base.OnPropertyChangedWithValue<RecruitVolunteerOwnerVM>(value, "FocusedVolunteerOwner");
				}
			}
		}

		// Token: 0x17000587 RID: 1415
		// (get) Token: 0x06001107 RID: 4359 RVA: 0x00044D9E File Offset: 0x00042F9E
		// (set) Token: 0x06001108 RID: 4360 RVA: 0x00044DA6 File Offset: 0x00042FA6
		[DataSourceProperty]
		public HintViewModel PartyWageHint
		{
			get
			{
				return this._partyWageHint;
			}
			set
			{
				if (value != this._partyWageHint)
				{
					this._partyWageHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PartyWageHint");
				}
			}
		}

		// Token: 0x17000588 RID: 1416
		// (get) Token: 0x06001109 RID: 4361 RVA: 0x00044DC4 File Offset: 0x00042FC4
		// (set) Token: 0x0600110A RID: 4362 RVA: 0x00044DCC File Offset: 0x00042FCC
		[DataSourceProperty]
		public HintViewModel PartyCapacityHint
		{
			get
			{
				return this._partyCapacityHint;
			}
			set
			{
				if (value != this._partyCapacityHint)
				{
					this._partyCapacityHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "PartyCapacityHint");
				}
			}
		}

		// Token: 0x17000589 RID: 1417
		// (get) Token: 0x0600110B RID: 4363 RVA: 0x00044DEA File Offset: 0x00042FEA
		// (set) Token: 0x0600110C RID: 4364 RVA: 0x00044DF2 File Offset: 0x00042FF2
		[DataSourceProperty]
		public BasicTooltipViewModel PartySpeedHint
		{
			get
			{
				return this._partySpeedHint;
			}
			set
			{
				if (value != this._partySpeedHint)
				{
					this._partySpeedHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "PartySpeedHint");
				}
			}
		}

		// Token: 0x1700058A RID: 1418
		// (get) Token: 0x0600110D RID: 4365 RVA: 0x00044E10 File Offset: 0x00043010
		// (set) Token: 0x0600110E RID: 4366 RVA: 0x00044E18 File Offset: 0x00043018
		[DataSourceProperty]
		public HintViewModel RemainingFoodHint
		{
			get
			{
				return this._remainingFoodHint;
			}
			set
			{
				if (value != this._remainingFoodHint)
				{
					this._remainingFoodHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "RemainingFoodHint");
				}
			}
		}

		// Token: 0x1700058B RID: 1419
		// (get) Token: 0x0600110F RID: 4367 RVA: 0x00044E36 File Offset: 0x00043036
		// (set) Token: 0x06001110 RID: 4368 RVA: 0x00044E3E File Offset: 0x0004303E
		[DataSourceProperty]
		public HintViewModel TotalWealthHint
		{
			get
			{
				return this._totalWealthHint;
			}
			set
			{
				if (value != this._totalWealthHint)
				{
					this._totalWealthHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "TotalWealthHint");
				}
			}
		}

		// Token: 0x1700058C RID: 1420
		// (get) Token: 0x06001111 RID: 4369 RVA: 0x00044E5C File Offset: 0x0004305C
		// (set) Token: 0x06001112 RID: 4370 RVA: 0x00044E64 File Offset: 0x00043064
		[DataSourceProperty]
		public HintViewModel TotalCostHint
		{
			get
			{
				return this._totalCostHint;
			}
			set
			{
				if (value != this._totalCostHint)
				{
					this._totalCostHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "TotalCostHint");
				}
			}
		}

		// Token: 0x1700058D RID: 1421
		// (get) Token: 0x06001113 RID: 4371 RVA: 0x00044E82 File Offset: 0x00043082
		// (set) Token: 0x06001114 RID: 4372 RVA: 0x00044E8A File Offset: 0x0004308A
		[DataSourceProperty]
		public HintViewModel DoneHint
		{
			get
			{
				return this._doneHint;
			}
			set
			{
				if (value != this._doneHint)
				{
					this._doneHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DoneHint");
				}
			}
		}

		// Token: 0x1700058E RID: 1422
		// (get) Token: 0x06001115 RID: 4373 RVA: 0x00044EA8 File Offset: 0x000430A8
		// (set) Token: 0x06001116 RID: 4374 RVA: 0x00044EB0 File Offset: 0x000430B0
		[DataSourceProperty]
		public BasicTooltipViewModel RecruitAllHint
		{
			get
			{
				return this._recruitAllHint;
			}
			set
			{
				if (value != this._recruitAllHint)
				{
					this._recruitAllHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "RecruitAllHint");
				}
			}
		}

		// Token: 0x1700058F RID: 1423
		// (get) Token: 0x06001117 RID: 4375 RVA: 0x00044ECE File Offset: 0x000430CE
		// (set) Token: 0x06001118 RID: 4376 RVA: 0x00044ED6 File Offset: 0x000430D6
		[DataSourceProperty]
		public int PartyWage
		{
			get
			{
				return this._partyWage;
			}
			set
			{
				if (value != this._partyWage)
				{
					this._partyWage = value;
					base.OnPropertyChangedWithValue(value, "PartyWage");
				}
			}
		}

		// Token: 0x17000590 RID: 1424
		// (get) Token: 0x06001119 RID: 4377 RVA: 0x00044EF4 File Offset: 0x000430F4
		// (set) Token: 0x0600111A RID: 4378 RVA: 0x00044EFC File Offset: 0x000430FC
		[DataSourceProperty]
		public string PartyCapacityText
		{
			get
			{
				return this._partyCapacityText;
			}
			set
			{
				if (value != this._partyCapacityText)
				{
					this._partyCapacityText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyCapacityText");
				}
			}
		}

		// Token: 0x17000591 RID: 1425
		// (get) Token: 0x0600111B RID: 4379 RVA: 0x00044F1F File Offset: 0x0004311F
		// (set) Token: 0x0600111C RID: 4380 RVA: 0x00044F27 File Offset: 0x00043127
		[DataSourceProperty]
		public string PartyWageText
		{
			get
			{
				return this._partyWageText;
			}
			set
			{
				if (value != this._partyWageText)
				{
					this._partyWageText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartyWageText");
				}
			}
		}

		// Token: 0x17000592 RID: 1426
		// (get) Token: 0x0600111D RID: 4381 RVA: 0x00044F4A File Offset: 0x0004314A
		// (set) Token: 0x0600111E RID: 4382 RVA: 0x00044F52 File Offset: 0x00043152
		[DataSourceProperty]
		public string RecruitAllText
		{
			get
			{
				return this._recruitAllText;
			}
			set
			{
				if (value != this._recruitAllText)
				{
					this._recruitAllText = value;
					base.OnPropertyChangedWithValue<string>(value, "RecruitAllText");
				}
			}
		}

		// Token: 0x17000593 RID: 1427
		// (get) Token: 0x0600111F RID: 4383 RVA: 0x00044F75 File Offset: 0x00043175
		// (set) Token: 0x06001120 RID: 4384 RVA: 0x00044F7D File Offset: 0x0004317D
		[DataSourceProperty]
		public string PartySpeedText
		{
			get
			{
				return this._partySpeedText;
			}
			set
			{
				if (value != this._partySpeedText)
				{
					this._partySpeedText = value;
					base.OnPropertyChangedWithValue<string>(value, "PartySpeedText");
				}
			}
		}

		// Token: 0x17000594 RID: 1428
		// (get) Token: 0x06001121 RID: 4385 RVA: 0x00044FA0 File Offset: 0x000431A0
		// (set) Token: 0x06001122 RID: 4386 RVA: 0x00044FA8 File Offset: 0x000431A8
		[DataSourceProperty]
		public string ResetAllText
		{
			get
			{
				return this._resetAllText;
			}
			set
			{
				if (value != this._resetAllText)
				{
					this._resetAllText = value;
					base.OnPropertyChangedWithValue<string>(value, "ResetAllText");
				}
			}
		}

		// Token: 0x17000595 RID: 1429
		// (get) Token: 0x06001123 RID: 4387 RVA: 0x00044FCB File Offset: 0x000431CB
		// (set) Token: 0x06001124 RID: 4388 RVA: 0x00044FD3 File Offset: 0x000431D3
		[DataSourceProperty]
		public string CancelText
		{
			get
			{
				return this._cancelText;
			}
			set
			{
				if (value != this._cancelText)
				{
					this._cancelText = value;
					base.OnPropertyChangedWithValue<string>(value, "CancelText");
				}
			}
		}

		// Token: 0x17000596 RID: 1430
		// (get) Token: 0x06001125 RID: 4389 RVA: 0x00044FF6 File Offset: 0x000431F6
		// (set) Token: 0x06001126 RID: 4390 RVA: 0x00044FFE File Offset: 0x000431FE
		[DataSourceProperty]
		public string RemainingFoodText
		{
			get
			{
				return this._remainingFoodText;
			}
			set
			{
				if (value != this._remainingFoodText)
				{
					this._remainingFoodText = value;
					base.OnPropertyChangedWithValue<string>(value, "RemainingFoodText");
				}
			}
		}

		// Token: 0x17000597 RID: 1431
		// (get) Token: 0x06001127 RID: 4391 RVA: 0x00045021 File Offset: 0x00043221
		// (set) Token: 0x06001128 RID: 4392 RVA: 0x00045029 File Offset: 0x00043229
		[DataSourceProperty]
		public string TotalCostText
		{
			get
			{
				return this._totalCostText;
			}
			set
			{
				if (value != this._totalCostText)
				{
					this._totalCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalCostText");
				}
			}
		}

		// Token: 0x17000598 RID: 1432
		// (get) Token: 0x06001129 RID: 4393 RVA: 0x0004504C File Offset: 0x0004324C
		// (set) Token: 0x0600112A RID: 4394 RVA: 0x00045054 File Offset: 0x00043254
		[DataSourceProperty]
		public bool Enabled
		{
			get
			{
				return this._enabled;
			}
			set
			{
				if (value != this._enabled)
				{
					this._enabled = value;
					base.OnPropertyChangedWithValue(value, "Enabled");
				}
			}
		}

		// Token: 0x17000599 RID: 1433
		// (get) Token: 0x0600112B RID: 4395 RVA: 0x00045074 File Offset: 0x00043274
		// (set) Token: 0x0600112C RID: 4396 RVA: 0x0004507C File Offset: 0x0004327C
		[DataSourceProperty]
		public bool IsDoneEnabled
		{
			get
			{
				return this._isDoneEnabled;
			}
			set
			{
				if (value != this._isDoneEnabled)
				{
					this._isDoneEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsDoneEnabled");
				}
			}
		}

		// Token: 0x1700059A RID: 1434
		// (get) Token: 0x0600112D RID: 4397 RVA: 0x0004509A File Offset: 0x0004329A
		// (set) Token: 0x0600112E RID: 4398 RVA: 0x000450A2 File Offset: 0x000432A2
		[DataSourceProperty]
		public bool IsPartyCapacityWarningEnabled
		{
			get
			{
				return this._isPartyCapacityWarningEnabled;
			}
			set
			{
				if (value != this._isPartyCapacityWarningEnabled)
				{
					this._isPartyCapacityWarningEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsPartyCapacityWarningEnabled");
				}
			}
		}

		// Token: 0x1700059B RID: 1435
		// (get) Token: 0x0600112F RID: 4399 RVA: 0x000450C0 File Offset: 0x000432C0
		// (set) Token: 0x06001130 RID: 4400 RVA: 0x000450C8 File Offset: 0x000432C8
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x1700059C RID: 1436
		// (get) Token: 0x06001131 RID: 4401 RVA: 0x000450EB File Offset: 0x000432EB
		// (set) Token: 0x06001132 RID: 4402 RVA: 0x000450F3 File Offset: 0x000432F3
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

		// Token: 0x1700059D RID: 1437
		// (get) Token: 0x06001133 RID: 4403 RVA: 0x00045116 File Offset: 0x00043316
		// (set) Token: 0x06001134 RID: 4404 RVA: 0x0004511E File Offset: 0x0004331E
		[DataSourceProperty]
		public bool CanRecruitAll
		{
			get
			{
				return this._canRecruitAll;
			}
			set
			{
				if (value != this._canRecruitAll)
				{
					this._canRecruitAll = value;
					base.OnPropertyChangedWithValue(value, "CanRecruitAll");
				}
			}
		}

		// Token: 0x1700059E RID: 1438
		// (get) Token: 0x06001135 RID: 4405 RVA: 0x0004513C File Offset: 0x0004333C
		// (set) Token: 0x06001136 RID: 4406 RVA: 0x00045144 File Offset: 0x00043344
		[DataSourceProperty]
		public int TotalWealth
		{
			get
			{
				return this._totalWealth;
			}
			set
			{
				if (value != this._totalWealth)
				{
					this._totalWealth = value;
					base.OnPropertyChangedWithValue(value, "TotalWealth");
				}
			}
		}

		// Token: 0x1700059F RID: 1439
		// (get) Token: 0x06001137 RID: 4407 RVA: 0x00045162 File Offset: 0x00043362
		// (set) Token: 0x06001138 RID: 4408 RVA: 0x0004516A File Offset: 0x0004336A
		[DataSourceProperty]
		public int PartyCapacity
		{
			get
			{
				return this._partyCapacity;
			}
			set
			{
				if (value != this._partyCapacity)
				{
					this._partyCapacity = value;
					base.OnPropertyChangedWithValue(value, "PartyCapacity");
				}
			}
		}

		// Token: 0x170005A0 RID: 1440
		// (get) Token: 0x06001139 RID: 4409 RVA: 0x00045188 File Offset: 0x00043388
		// (set) Token: 0x0600113A RID: 4410 RVA: 0x00045190 File Offset: 0x00043390
		[DataSourceProperty]
		public int InitialPartySize
		{
			get
			{
				return this._initialPartySize;
			}
			set
			{
				if (value != this._initialPartySize)
				{
					this._initialPartySize = value;
					base.OnPropertyChangedWithValue(value, "InitialPartySize");
				}
			}
		}

		// Token: 0x170005A1 RID: 1441
		// (get) Token: 0x0600113B RID: 4411 RVA: 0x000451AE File Offset: 0x000433AE
		// (set) Token: 0x0600113C RID: 4412 RVA: 0x000451B6 File Offset: 0x000433B6
		[DataSourceProperty]
		public int CurrentPartySize
		{
			get
			{
				return this._currentPartySize;
			}
			set
			{
				if (value != this._currentPartySize)
				{
					this._currentPartySize = value;
					base.OnPropertyChangedWithValue(value, "CurrentPartySize");
				}
			}
		}

		// Token: 0x170005A2 RID: 1442
		// (get) Token: 0x0600113D RID: 4413 RVA: 0x000451D4 File Offset: 0x000433D4
		// (set) Token: 0x0600113E RID: 4414 RVA: 0x000451DC File Offset: 0x000433DC
		[DataSourceProperty]
		public MBBindingList<RecruitVolunteerVM> VolunteerList
		{
			get
			{
				return this._volunteerList;
			}
			set
			{
				if (value != this._volunteerList)
				{
					this._volunteerList = value;
					base.OnPropertyChangedWithValue<MBBindingList<RecruitVolunteerVM>>(value, "VolunteerList");
				}
			}
		}

		// Token: 0x170005A3 RID: 1443
		// (get) Token: 0x0600113F RID: 4415 RVA: 0x000451FA File Offset: 0x000433FA
		// (set) Token: 0x06001140 RID: 4416 RVA: 0x00045202 File Offset: 0x00043402
		[DataSourceProperty]
		public MBBindingList<RecruitVolunteerTroopVM> TroopsInCart
		{
			get
			{
				return this._troopsInCart;
			}
			set
			{
				if (value != this._troopsInCart)
				{
					this._troopsInCart = value;
					base.OnPropertyChangedWithValue<MBBindingList<RecruitVolunteerTroopVM>>(value, "TroopsInCart");
				}
			}
		}

		// Token: 0x06001141 RID: 4417 RVA: 0x00045220 File Offset: 0x00043420
		public void SetGetKeyTextFromKeyIDFunc(Func<string, TextObject> getKeyTextFromKeyId)
		{
			this._getKeyTextFromKeyId = getKeyTextFromKeyId;
		}

		// Token: 0x06001142 RID: 4418 RVA: 0x00045229 File Offset: 0x00043429
		private string GetRecruitAllKey()
		{
			if (this.RecruitAllInputKey == null || this._getKeyTextFromKeyId == null)
			{
				return string.Empty;
			}
			return this._getKeyTextFromKeyId(this.RecruitAllInputKey.KeyID).ToString();
		}

		// Token: 0x06001143 RID: 4419 RVA: 0x0004525C File Offset: 0x0004345C
		public void SetCancelInputKey(HotKey hotKey)
		{
			this.CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001144 RID: 4420 RVA: 0x0004526B File Offset: 0x0004346B
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x06001145 RID: 4421 RVA: 0x0004527A File Offset: 0x0004347A
		public void SetRecruitAllInputKey(HotKey hotKey)
		{
			this.RecruitAllInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
			this.SetRecruitAllHint();
		}

		// Token: 0x06001146 RID: 4422 RVA: 0x0004528F File Offset: 0x0004348F
		public void SetResetInputKey(HotKey hotKey)
		{
			this.ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x170005A4 RID: 1444
		// (get) Token: 0x06001147 RID: 4423 RVA: 0x0004529E File Offset: 0x0004349E
		// (set) Token: 0x06001148 RID: 4424 RVA: 0x000452A6 File Offset: 0x000434A6
		[DataSourceProperty]
		public InputKeyItemVM CancelInputKey
		{
			get
			{
				return this._cancelInputKey;
			}
			set
			{
				if (value != this._cancelInputKey)
				{
					this._cancelInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "CancelInputKey");
				}
			}
		}

		// Token: 0x170005A5 RID: 1445
		// (get) Token: 0x06001149 RID: 4425 RVA: 0x000452C4 File Offset: 0x000434C4
		// (set) Token: 0x0600114A RID: 4426 RVA: 0x000452CC File Offset: 0x000434CC
		[DataSourceProperty]
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

		// Token: 0x170005A6 RID: 1446
		// (get) Token: 0x0600114B RID: 4427 RVA: 0x000452EA File Offset: 0x000434EA
		// (set) Token: 0x0600114C RID: 4428 RVA: 0x000452F2 File Offset: 0x000434F2
		[DataSourceProperty]
		public InputKeyItemVM ResetInputKey
		{
			get
			{
				return this._resetInputKey;
			}
			set
			{
				if (value != this._resetInputKey)
				{
					this._resetInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "ResetInputKey");
				}
			}
		}

		// Token: 0x170005A7 RID: 1447
		// (get) Token: 0x0600114D RID: 4429 RVA: 0x00045310 File Offset: 0x00043510
		// (set) Token: 0x0600114E RID: 4430 RVA: 0x00045318 File Offset: 0x00043518
		[DataSourceProperty]
		public InputKeyItemVM RecruitAllInputKey
		{
			get
			{
				return this._recruitAllInputKey;
			}
			set
			{
				if (value != this._recruitAllInputKey)
				{
					this._recruitAllInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "RecruitAllInputKey");
				}
			}
		}

		// Token: 0x040007B9 RID: 1977
		private TextObject _recruitAllTextObject;

		// Token: 0x040007BA RID: 1978
		private string _playerDoesntHaveEnoughMoneyStr;

		// Token: 0x040007BB RID: 1979
		private string _playerIsOverPartyLimitStr;

		// Token: 0x040007BC RID: 1980
		private Func<string, TextObject> _getKeyTextFromKeyId;

		// Token: 0x040007BD RID: 1981
		private bool _isAvailableTroopsHighlightApplied;

		// Token: 0x040007BE RID: 1982
		private string _latestTutorialElementID;

		// Token: 0x040007BF RID: 1983
		private bool _enabled;

		// Token: 0x040007C0 RID: 1984
		private bool _isDoneEnabled;

		// Token: 0x040007C1 RID: 1985
		private bool _isPartyCapacityWarningEnabled;

		// Token: 0x040007C2 RID: 1986
		private bool _canRecruitAll;

		// Token: 0x040007C3 RID: 1987
		private string _titleText;

		// Token: 0x040007C4 RID: 1988
		private string _doneText;

		// Token: 0x040007C5 RID: 1989
		private string _recruitAllText;

		// Token: 0x040007C6 RID: 1990
		private string _resetAllText;

		// Token: 0x040007C7 RID: 1991
		private string _cancelText;

		// Token: 0x040007C8 RID: 1992
		private int _totalWealth;

		// Token: 0x040007C9 RID: 1993
		private int _partyCapacity;

		// Token: 0x040007CA RID: 1994
		private int _initialPartySize;

		// Token: 0x040007CB RID: 1995
		private int _currentPartySize;

		// Token: 0x040007CC RID: 1996
		private MBBindingList<RecruitVolunteerVM> _volunteerList;

		// Token: 0x040007CD RID: 1997
		private MBBindingList<RecruitVolunteerTroopVM> _troopsInCart;

		// Token: 0x040007CE RID: 1998
		private int _partyWage;

		// Token: 0x040007CF RID: 1999
		private string _partyCapacityText = "";

		// Token: 0x040007D0 RID: 2000
		private string _partyWageText = "";

		// Token: 0x040007D1 RID: 2001
		private string _partySpeedText = "";

		// Token: 0x040007D2 RID: 2002
		private string _remainingFoodText = "";

		// Token: 0x040007D3 RID: 2003
		private string _totalCostText = "";

		// Token: 0x040007D4 RID: 2004
		private RecruitVolunteerTroopVM _focusedVolunteerTroop;

		// Token: 0x040007D5 RID: 2005
		private RecruitVolunteerOwnerVM _focusedVolunteerOwner;

		// Token: 0x040007D6 RID: 2006
		private HintViewModel _partyWageHint;

		// Token: 0x040007D7 RID: 2007
		private HintViewModel _partyCapacityHint;

		// Token: 0x040007D8 RID: 2008
		private BasicTooltipViewModel _partySpeedHint;

		// Token: 0x040007D9 RID: 2009
		private HintViewModel _remainingFoodHint;

		// Token: 0x040007DA RID: 2010
		private HintViewModel _totalWealthHint;

		// Token: 0x040007DB RID: 2011
		private HintViewModel _totalCostHint;

		// Token: 0x040007DC RID: 2012
		private HintViewModel _resetHint;

		// Token: 0x040007DD RID: 2013
		private HintViewModel _doneHint;

		// Token: 0x040007DE RID: 2014
		private BasicTooltipViewModel _recruitAllHint;

		// Token: 0x040007DF RID: 2015
		private InputKeyItemVM _cancelInputKey;

		// Token: 0x040007E0 RID: 2016
		private InputKeyItemVM _doneInputKey;

		// Token: 0x040007E1 RID: 2017
		private InputKeyItemVM _resetInputKey;

		// Token: 0x040007E2 RID: 2018
		private InputKeyItemVM _recruitAllInputKey;
	}
}
