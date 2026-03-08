using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance
{
	// Token: 0x02000136 RID: 310
	public class ClanFinanceWorkshopItemVM : ClanFinanceIncomeItemBaseVM
	{
		// Token: 0x170009BF RID: 2495
		// (get) Token: 0x06001CAB RID: 7339 RVA: 0x00069CFF File Offset: 0x00067EFF
		// (set) Token: 0x06001CAC RID: 7340 RVA: 0x00069D07 File Offset: 0x00067F07
		public Workshop Workshop { get; private set; }

		// Token: 0x06001CAD RID: 7341 RVA: 0x00069D10 File Offset: 0x00067F10
		public ClanFinanceWorkshopItemVM(Workshop workshop, Action<ClanFinanceWorkshopItemVM> onSelection, Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
			: base(null, onRefresh)
		{
			this._workshopWarehouseBehavior = Campaign.Current.GetCampaignBehavior<IWorkshopWarehouseCampaignBehavior>();
			this.Workshop = workshop;
			this._openCardSelectionPopup = openCardSelectionPopup;
			this._workshopModel = Campaign.Current.Models.WorkshopModel;
			base.IncomeTypeAsEnum = IncomeTypes.Workshop;
			this._onSelection = new Action<ClanFinanceIncomeItemBaseVM>(this.tempOnSelection);
			this._onSelectionT = onSelection;
			SettlementComponent settlementComponent = this.Workshop.Settlement.SettlementComponent;
			base.ImageName = ((settlementComponent != null) ? settlementComponent.WaitMeshName : "");
			this.ManageWorkshopHint = new HintViewModel(new TextObject("{=LxWVtDF0}Manage Workshop", null), null);
			this.UseWarehouseAsInputHint = new HintViewModel(new TextObject("{=a4oqWgUi}If there are no raw materials in the warehouse, the workshop will buy raw materials from the market until the warehouse is restocked", null), null);
			this.StoreOutputPercentageHint = new HintViewModel(new TextObject("{=NVUi4bB9}When the warehouse is full, the workshop will sell the products to the town market", null), null);
			this.InputWarehouseCountsTooltip = new BasicTooltipViewModel();
			this.OutputWarehouseCountsTooltip = new BasicTooltipViewModel();
			this.ReceiveInputFromWarehouse = this._workshopWarehouseBehavior.IsGettingInputsFromWarehouse(workshop);
			this.WarehousePercentageSelector = new SelectorVM<WorkshopPercentageSelectorItemVM>(0, new Action<SelectorVM<WorkshopPercentageSelectorItemVM>>(this.OnStoreOutputInWarehousePercentageUpdated));
			this.RefreshStoragePercentages();
			float currentPercentage = this._workshopWarehouseBehavior.GetStockProductionInWarehouseRatio(workshop);
			WorkshopPercentageSelectorItemVM workshopPercentageSelectorItemVM = this.WarehousePercentageSelector.ItemList.FirstOrDefault((WorkshopPercentageSelectorItemVM x) => x.Percentage.ApproximatelyEqualsTo(currentPercentage, 0.1f));
			this.WarehousePercentageSelector.SelectedIndex = ((workshopPercentageSelectorItemVM != null) ? this.WarehousePercentageSelector.ItemList.IndexOf(workshopPercentageSelectorItemVM) : 0);
			this.RefreshValues();
		}

		// Token: 0x06001CAE RID: 7342 RVA: 0x00069EDD File Offset: 0x000680DD
		private void tempOnSelection(ClanFinanceIncomeItemBaseVM temp)
		{
			this._onSelectionT(this);
		}

		// Token: 0x06001CAF RID: 7343 RVA: 0x00069EEC File Offset: 0x000680EC
		public override void RefreshValues()
		{
			base.RefreshValues();
			base.Name = this.Workshop.WorkshopType.Name.ToString();
			this.WorkshopTypeId = this.Workshop.WorkshopType.StringId;
			base.Location = this.Workshop.Settlement.Name.ToString();
			base.Income = (int)((float)this.Workshop.ProfitMade * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction()));
			base.IncomeValueText = base.DetermineIncomeText(base.Income);
			this.InputsText = GameTexts.FindText("str_clan_workshop_inputs", null).ToString();
			this.OutputsText = GameTexts.FindText("str_clan_workshop_outputs", null).ToString();
			this.StoreOutputPercentageText = new TextObject("{=y6qCNFQj}Store Outputs in the Warehouse", null).ToString();
			this.UseWarehouseAsInputText = new TextObject("{=88WPmTKH}Get Input from the Warehouse", null).ToString();
			this.WarehouseCapacityText = new TextObject("{=X6eG4Q5V}Warehouse Capacity", null).ToString();
			float warehouseItemRosterWeight = this._workshopWarehouseBehavior.GetWarehouseItemRosterWeight(this.Workshop.Settlement);
			int warehouseCapacity = Campaign.Current.Models.WorkshopModel.WarehouseCapacity;
			this.WarehouseCapacityValue = GameTexts.FindText("str_LEFT_over_RIGHT", null).SetTextVariable("LEFT", warehouseItemRosterWeight, 2).SetTextVariable("RIGHT", warehouseCapacity)
				.ToString();
			this.WarehouseInputAmount = this._workshopWarehouseBehavior.GetInputCount(this.Workshop);
			this.WarehouseOutputAmount = this._workshopWarehouseBehavior.GetOutputCount(this.Workshop);
			this._inputDetails = this._workshopWarehouseBehavior.GetInputDailyChange(this.Workshop);
			this._outputDetails = this._workshopWarehouseBehavior.GetOutputDailyChange(this.Workshop);
			this.InputWarehouseCountsTooltip.SetToolipCallback(() => this.GetWarehouseInputOutputTooltip(true));
			this.OutputWarehouseCountsTooltip.SetToolipCallback(() => this.GetWarehouseInputOutputTooltip(false));
			base.ItemProperties.Clear();
			this.PopulateStatsList();
		}

		// Token: 0x06001CB0 RID: 7344 RVA: 0x0006A0F0 File Offset: 0x000682F0
		private List<TooltipProperty> GetWarehouseInputOutputTooltip(bool isInput)
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			ExplainedNumber explainedNumber = (isInput ? this._inputDetails : this._outputDetails);
			if (!explainedNumber.ResultNumber.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				list.Add(new TooltipProperty(new TextObject("{=Y9egTJg0}Daily Change", null).ToString(), "", 1, false, TooltipProperty.TooltipPropertyFlags.Title));
				list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
				foreach (ValueTuple<string, float> valueTuple in explainedNumber.GetLines())
				{
					string value = GameTexts.FindText("str_clan_workshop_material_daily_Change", null).SetTextVariable("CHANGE", MathF.Abs(valueTuple.Item2).ToString("F1")).SetTextVariable("IS_POSITIVE", (valueTuple.Item2 > 0f) ? 1 : 0)
						.ToString();
					list.Add(new TooltipProperty(valueTuple.Item1, value, 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x06001CB1 RID: 7345 RVA: 0x0006A21C File Offset: 0x0006841C
		private void RefreshStoragePercentages()
		{
			this.WarehousePercentageSelector.ItemList.Clear();
			TextObject textObject = GameTexts.FindText("str_NUMBER_percent", null);
			textObject.SetTextVariable("NUMBER", 0);
			this.WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0f));
			textObject.SetTextVariable("NUMBER", 25);
			this.WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0.25f));
			textObject.SetTextVariable("NUMBER", 50);
			this.WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0.5f));
			textObject.SetTextVariable("NUMBER", 75);
			this.WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0.75f));
			textObject.SetTextVariable("NUMBER", 100);
			this.WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 1f));
		}

		// Token: 0x06001CB2 RID: 7346 RVA: 0x0006A311 File Offset: 0x00068511
		public void ExecuteToggleWarehouseUsage()
		{
			this.ReceiveInputFromWarehouse = !this.ReceiveInputFromWarehouse;
			this._workshopWarehouseBehavior.SetIsGettingInputsFromWarehouse(this.Workshop, this.ReceiveInputFromWarehouse);
			base.ItemProperties.Clear();
			this.PopulateStatsList();
		}

		// Token: 0x06001CB3 RID: 7347 RVA: 0x0006A34C File Offset: 0x0006854C
		protected override void PopulateStatsList()
		{
			ValueTuple<TextObject, bool, BasicTooltipViewModel> workshopStatus = this.GetWorkshopStatus(this.Workshop);
			if (!TextObject.IsNullOrEmpty(workshopStatus.Item1))
			{
				base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=DXczLzml}Status", null).ToString(), workshopStatus.Item1.ToString(), workshopStatus.Item2, workshopStatus.Item3));
			}
			SelectableItemPropertyVM currentCapitalProperty = this.GetCurrentCapitalProperty();
			base.ItemProperties.Add(currentCapitalProperty);
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=CaRbMaZY}Daily Wage", null).ToString(), this.Workshop.Expense.ToString(), false, null));
			TextObject textObject;
			TextObject textObject2;
			ClanFinanceWorkshopItemVM.GetWorkshopTypeProductionTexts(this.Workshop.WorkshopType, out textObject, out textObject2);
			this.InputProducts = textObject.ToString();
			this.OutputProducts = textObject2.ToString();
		}

		// Token: 0x06001CB4 RID: 7348 RVA: 0x0006A420 File Offset: 0x00068620
		private SelectableItemPropertyVM GetCurrentCapitalProperty()
		{
			string name = new TextObject("{=Ra17aK4e}Current Capital", null).ToString();
			string value = this.Workshop.Capital.ToString();
			bool isWarning = false;
			BasicTooltipViewModel hint;
			if (this.Workshop.Capital < this._workshopModel.CapitalLowLimit)
			{
				isWarning = true;
				hint = new BasicTooltipViewModel(() => new TextObject("{=Qu5clctb}The workshop is losing money. The expenses are being paid from your treasury because the workshop's capital is below {LOWER_THRESHOLD} denars", null).SetTextVariable("LOWER_THRESHOLD", this._workshopModel.CapitalLowLimit).ToString());
			}
			else
			{
				TextObject text = new TextObject("{=dEMUqz2Y}This workshop will send 20% of its profits above {INITIAL_CAPITAL} capital to your treasury", null);
				text.SetTextVariable("INITIAL_CAPITAL", Campaign.Current.Models.WorkshopModel.InitialCapital);
				hint = new BasicTooltipViewModel(() => text.ToString());
			}
			return new SelectableItemPropertyVM(name, value, isWarning, hint);
		}

		// Token: 0x06001CB5 RID: 7349 RVA: 0x0006A4D8 File Offset: 0x000686D8
		[return: TupleElementNames(new string[] { "Status", "IsWarning", "Hint" })]
		private ValueTuple<TextObject, bool, BasicTooltipViewModel> GetWorkshopStatus(Workshop workshop)
		{
			bool item = false;
			BasicTooltipViewModel item2 = null;
			TextObject item3;
			if (workshop.LastRunCampaignTime.ElapsedDaysUntilNow >= 1f)
			{
				item3 = this._haltedText;
				item = true;
				TextObject tooltipText = TextObject.GetEmpty();
				if (workshop.Settlement.Town.InRebelliousState)
				{
					tooltipText = this._townRebellionText;
				}
				else if (!this._workshopWarehouseBehavior.IsRawMaterialsSufficientInTownMarket(workshop))
				{
					tooltipText = this._noRawMaterialsText;
				}
				else if (this.WarehousePercentageSelector.SelectedItem.Percentage < 1f)
				{
					tooltipText = this._noProfitText;
				}
				int num = (int)workshop.LastRunCampaignTime.ElapsedDaysUntilNow;
				tooltipText.SetTextVariable("DAY", num);
				tooltipText.SetTextVariable("PLURAL_DAYS", (num == 1) ? "0" : "1");
				item2 = new BasicTooltipViewModel(() => tooltipText.ToString());
			}
			else
			{
				item3 = this._runningText;
			}
			return new ValueTuple<TextObject, bool, BasicTooltipViewModel>(item3, item, item2);
		}

		// Token: 0x06001CB6 RID: 7350 RVA: 0x0006A5EC File Offset: 0x000687EC
		private static void GetWorkshopTypeProductionTexts(WorkshopType workshopType, out TextObject inputsText, out TextObject outputsText)
		{
			CampaignUIHelper.ProductInputOutputEqualityComparer comparer = new CampaignUIHelper.ProductInputOutputEqualityComparer();
			IEnumerable<TextObject> texts = from x in workshopType.Productions.SelectMany((WorkshopType.Production p) => p.Inputs).Distinct(comparer)
				select x.Item1.GetName();
			IEnumerable<TextObject> texts2 = from x in workshopType.Productions.SelectMany((WorkshopType.Production p) => p.Outputs).Distinct(comparer)
				select x.Item1.GetName();
			inputsText = CampaignUIHelper.GetCommaSeparatedText(null, texts);
			outputsText = CampaignUIHelper.GetCommaSeparatedText(null, texts2);
		}

		// Token: 0x06001CB7 RID: 7351 RVA: 0x0006A6BB File Offset: 0x000688BB
		public void ExecuteBeginWorkshopHint()
		{
			if (this.Workshop.WorkshopType != null)
			{
				InformationManager.ShowTooltip(typeof(Workshop), new object[] { this.Workshop });
			}
		}

		// Token: 0x06001CB8 RID: 7352 RVA: 0x0006A6E8 File Offset: 0x000688E8
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001CB9 RID: 7353 RVA: 0x0006A6F0 File Offset: 0x000688F0
		public void OnStoreOutputInWarehousePercentageUpdated(SelectorVM<WorkshopPercentageSelectorItemVM> selector)
		{
			if (selector.SelectedIndex != -1)
			{
				this._workshopWarehouseBehavior.SetStockProductionInWarehouseRatio(this.Workshop, selector.SelectedItem.Percentage);
				this._inputDetails = this._workshopWarehouseBehavior.GetInputDailyChange(this.Workshop);
				this._outputDetails = this._workshopWarehouseBehavior.GetOutputDailyChange(this.Workshop);
			}
		}

		// Token: 0x06001CBA RID: 7354 RVA: 0x0006A750 File Offset: 0x00068950
		public void ExecuteManageWorkshop()
		{
			TextObject title = new TextObject("{=LxWVtDF0}Manage Workshop", null);
			ClanCardSelectionInfo obj = new ClanCardSelectionInfo(title, this.GetManageWorkshopItems(), new Action<List<object>, Action>(this.OnManageWorkshopDone), false, 1, 0);
			Action<ClanCardSelectionInfo> openCardSelectionPopup = this._openCardSelectionPopup;
			if (openCardSelectionPopup == null)
			{
				return;
			}
			openCardSelectionPopup(obj);
		}

		// Token: 0x06001CBB RID: 7355 RVA: 0x0006A797 File Offset: 0x00068997
		private IEnumerable<ClanCardSelectionItemInfo> GetManageWorkshopItems()
		{
			int costForNotable = this._workshopModel.GetCostForNotable(this.Workshop);
			TextObject textObject = new TextObject("{=ysireFjT}Sell This Workshop for {GOLD_AMOUNT}{GOLD_ICON}", null);
			textObject.SetTextVariable("GOLD_AMOUNT", costForNotable);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			yield return new ClanCardSelectionItemInfo(textObject, false, null, ClanCardSelectionItemPropertyInfo.CreateActionGoldChangeText(costForNotable));
			int costOfChangingType = this._workshopModel.GetConvertProductionCost(this.Workshop.WorkshopType);
			TextObject cannotChangeTypeReason = new TextObject("{=av51ur2M}You need at least {REQUIRED_AMOUNT} denars to change the production type of this workshop.", null);
			cannotChangeTypeReason.SetTextVariable("REQUIRED_AMOUNT", costOfChangingType);
			foreach (WorkshopType workshopType in WorkshopType.All)
			{
				if (this.Workshop.WorkshopType != workshopType && !workshopType.IsHidden)
				{
					TextObject name = workshopType.Name;
					bool flag = costOfChangingType <= Hero.MainHero.Gold;
					yield return new ClanCardSelectionItemInfo(workshopType, name, null, CardSelectionItemSpriteType.Workshop, workshopType.StringId, null, this.GetWorkshopItemProperties(workshopType), !flag, cannotChangeTypeReason, ClanCardSelectionItemPropertyInfo.CreateActionGoldChangeText(-costOfChangingType));
				}
			}
			List<WorkshopType>.Enumerator enumerator = default(List<WorkshopType>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06001CBC RID: 7356 RVA: 0x0006A7A7 File Offset: 0x000689A7
		private IEnumerable<ClanCardSelectionItemPropertyInfo> GetWorkshopItemProperties(WorkshopType workshopType)
		{
			Workshop workshop = this.Workshop;
			int? num;
			if (workshop == null)
			{
				num = null;
			}
			else
			{
				Settlement settlement = workshop.Settlement;
				if (settlement == null)
				{
					num = null;
				}
				else
				{
					Town town = settlement.Town;
					if (town == null)
					{
						num = null;
					}
					else
					{
						Workshop[] workshops = town.Workshops;
						num = ((workshops != null) ? new int?(workshops.Count((Workshop x) => ((x != null) ? x.WorkshopType : null) == workshopType)) : null);
					}
				}
			}
			int num2 = num ?? 0;
			TextObject textObject = ((num2 == 0) ? new TextObject("{=gu5xmV0E}No other {WORKSHOP_NAME} in this town.", null) : new TextObject("{=lhIpaGt9}There {?(COUNT > 1)}are{?}is{\\?} {COUNT} more {?(COUNT > 1)}{PLURAL(WORKSHOP_NAME)}{?}{WORKSHOP_NAME}{\\?} in this town.", null));
			textObject.SetTextVariable("WORKSHOP_NAME", workshopType.Name);
			textObject.SetTextVariable("COUNT", num2);
			TextObject inputsText;
			TextObject outputsText;
			ClanFinanceWorkshopItemVM.GetWorkshopTypeProductionTexts(workshopType, out inputsText, out outputsText);
			yield return new ClanCardSelectionItemPropertyInfo(textObject);
			yield return new ClanCardSelectionItemPropertyInfo(ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(new TextObject("{=XCz81XYm}Inputs", null), inputsText));
			yield return new ClanCardSelectionItemPropertyInfo(ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(new TextObject("{=ErnykQEH}Outputs", null), outputsText));
			yield break;
		}

		// Token: 0x06001CBD RID: 7357 RVA: 0x0006A7C0 File Offset: 0x000689C0
		private void OnManageWorkshopDone(List<object> selectedItems, Action closePopup)
		{
			if (closePopup != null)
			{
				closePopup();
			}
			if (selectedItems.Count == 1)
			{
				WorkshopType workshopType = (WorkshopType)selectedItems[0];
				if (workshopType == null)
				{
					if (this.Workshop.Settlement.Town.Workshops.Count((Workshop x) => x.Owner == Hero.MainHero) == 1)
					{
						bool flag = Hero.MainHero.CurrentSettlement == this.Workshop.Settlement;
						InformationManager.ShowInquiry(new InquiryData(new TextObject("{=HiJTlBgF}Sell Workshop", null).ToString(), flag ? new TextObject("{=s06mScpJ}If you have goods in the warehouse, they will be transferred to your party. Are you sure?", null).ToString() : new TextObject("{=yuxBDKgM}If you have goods in the warehouse, they will be lost! Are you sure?", null).ToString(), true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=8OkPHu4f}No", null).ToString(), new Action(this.ExecuteSellWorkshop), null, "", 0f, null, null, null), false, false);
					}
					else
					{
						this.ExecuteSellWorkshop();
					}
				}
				else
				{
					ChangeProductionTypeOfWorkshopAction.Apply(this.Workshop, workshopType, false);
				}
				Action onRefresh = this._onRefresh;
				if (onRefresh == null)
				{
					return;
				}
				onRefresh();
			}
		}

		// Token: 0x06001CBE RID: 7358 RVA: 0x0006A8F4 File Offset: 0x00068AF4
		private void ExecuteSellWorkshop()
		{
			Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(this.Workshop);
			ChangeOwnerOfWorkshopAction.ApplyByPlayerSelling(this.Workshop, notableOwnerForWorkshop, this.Workshop.WorkshopType);
			Action onRefresh = this._onRefresh;
			if (onRefresh == null)
			{
				return;
			}
			onRefresh();
		}

		// Token: 0x170009C0 RID: 2496
		// (get) Token: 0x06001CBF RID: 7359 RVA: 0x0006A943 File Offset: 0x00068B43
		// (set) Token: 0x06001CC0 RID: 7360 RVA: 0x0006A94B File Offset: 0x00068B4B
		[DataSourceProperty]
		public HintViewModel UseWarehouseAsInputHint
		{
			get
			{
				return this._useWarehouseAsInputHint;
			}
			set
			{
				if (value != this._useWarehouseAsInputHint)
				{
					this._useWarehouseAsInputHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "UseWarehouseAsInputHint");
				}
			}
		}

		// Token: 0x170009C1 RID: 2497
		// (get) Token: 0x06001CC1 RID: 7361 RVA: 0x0006A969 File Offset: 0x00068B69
		// (set) Token: 0x06001CC2 RID: 7362 RVA: 0x0006A971 File Offset: 0x00068B71
		[DataSourceProperty]
		public HintViewModel StoreOutputPercentageHint
		{
			get
			{
				return this._storeOutputPercentageHint;
			}
			set
			{
				if (value != this._storeOutputPercentageHint)
				{
					this._storeOutputPercentageHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "StoreOutputPercentageHint");
				}
			}
		}

		// Token: 0x170009C2 RID: 2498
		// (get) Token: 0x06001CC3 RID: 7363 RVA: 0x0006A98F File Offset: 0x00068B8F
		// (set) Token: 0x06001CC4 RID: 7364 RVA: 0x0006A997 File Offset: 0x00068B97
		[DataSourceProperty]
		public HintViewModel ManageWorkshopHint
		{
			get
			{
				return this._manageWorkshopHint;
			}
			set
			{
				if (value != this._manageWorkshopHint)
				{
					this._manageWorkshopHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ManageWorkshopHint");
				}
			}
		}

		// Token: 0x170009C3 RID: 2499
		// (get) Token: 0x06001CC5 RID: 7365 RVA: 0x0006A9B5 File Offset: 0x00068BB5
		// (set) Token: 0x06001CC6 RID: 7366 RVA: 0x0006A9BD File Offset: 0x00068BBD
		[DataSourceProperty]
		public BasicTooltipViewModel InputWarehouseCountsTooltip
		{
			get
			{
				return this._inputWarehouseCountsTooltip;
			}
			set
			{
				if (value != this._inputWarehouseCountsTooltip)
				{
					this._inputWarehouseCountsTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "InputWarehouseCountsTooltip");
				}
			}
		}

		// Token: 0x170009C4 RID: 2500
		// (get) Token: 0x06001CC7 RID: 7367 RVA: 0x0006A9DB File Offset: 0x00068BDB
		// (set) Token: 0x06001CC8 RID: 7368 RVA: 0x0006A9E3 File Offset: 0x00068BE3
		[DataSourceProperty]
		public BasicTooltipViewModel OutputWarehouseCountsTooltip
		{
			get
			{
				return this._outputWarehouseCountsTooltip;
			}
			set
			{
				if (value != this._outputWarehouseCountsTooltip)
				{
					this._outputWarehouseCountsTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "OutputWarehouseCountsTooltip");
				}
			}
		}

		// Token: 0x170009C5 RID: 2501
		// (get) Token: 0x06001CC9 RID: 7369 RVA: 0x0006AA01 File Offset: 0x00068C01
		// (set) Token: 0x06001CCA RID: 7370 RVA: 0x0006AA09 File Offset: 0x00068C09
		public string WorkshopTypeId
		{
			get
			{
				return this._workshopTypeId;
			}
			set
			{
				if (value != this._workshopTypeId)
				{
					this._workshopTypeId = value;
					base.OnPropertyChangedWithValue<string>(value, "WorkshopTypeId");
				}
			}
		}

		// Token: 0x170009C6 RID: 2502
		// (get) Token: 0x06001CCB RID: 7371 RVA: 0x0006AA2C File Offset: 0x00068C2C
		// (set) Token: 0x06001CCC RID: 7372 RVA: 0x0006AA34 File Offset: 0x00068C34
		public string InputsText
		{
			get
			{
				return this._inputsText;
			}
			set
			{
				if (value != this._inputsText)
				{
					this._inputsText = value;
					base.OnPropertyChangedWithValue<string>(value, "InputsText");
				}
			}
		}

		// Token: 0x170009C7 RID: 2503
		// (get) Token: 0x06001CCD RID: 7373 RVA: 0x0006AA57 File Offset: 0x00068C57
		// (set) Token: 0x06001CCE RID: 7374 RVA: 0x0006AA5F File Offset: 0x00068C5F
		public string OutputsText
		{
			get
			{
				return this._outputsText;
			}
			set
			{
				if (value != this._outputsText)
				{
					this._outputsText = value;
					base.OnPropertyChangedWithValue<string>(value, "OutputsText");
				}
			}
		}

		// Token: 0x170009C8 RID: 2504
		// (get) Token: 0x06001CCF RID: 7375 RVA: 0x0006AA82 File Offset: 0x00068C82
		// (set) Token: 0x06001CD0 RID: 7376 RVA: 0x0006AA8A File Offset: 0x00068C8A
		public string InputProducts
		{
			get
			{
				return this._inputProducts;
			}
			set
			{
				if (value != this._inputProducts)
				{
					this._inputProducts = value;
					base.OnPropertyChangedWithValue<string>(value, "InputProducts");
				}
			}
		}

		// Token: 0x170009C9 RID: 2505
		// (get) Token: 0x06001CD1 RID: 7377 RVA: 0x0006AAAD File Offset: 0x00068CAD
		// (set) Token: 0x06001CD2 RID: 7378 RVA: 0x0006AAB5 File Offset: 0x00068CB5
		public string OutputProducts
		{
			get
			{
				return this._outputProducts;
			}
			set
			{
				if (value != this._outputProducts)
				{
					this._outputProducts = value;
					base.OnPropertyChangedWithValue<string>(value, "OutputProducts");
				}
			}
		}

		// Token: 0x170009CA RID: 2506
		// (get) Token: 0x06001CD3 RID: 7379 RVA: 0x0006AAD8 File Offset: 0x00068CD8
		// (set) Token: 0x06001CD4 RID: 7380 RVA: 0x0006AAE0 File Offset: 0x00068CE0
		public string UseWarehouseAsInputText
		{
			get
			{
				return this._useWarehouseAsInputText;
			}
			set
			{
				if (value != this._useWarehouseAsInputText)
				{
					this._useWarehouseAsInputText = value;
					base.OnPropertyChangedWithValue<string>(value, "UseWarehouseAsInputText");
				}
			}
		}

		// Token: 0x170009CB RID: 2507
		// (get) Token: 0x06001CD5 RID: 7381 RVA: 0x0006AB03 File Offset: 0x00068D03
		// (set) Token: 0x06001CD6 RID: 7382 RVA: 0x0006AB0B File Offset: 0x00068D0B
		public string StoreOutputPercentageText
		{
			get
			{
				return this._storeOutputPercentageText;
			}
			set
			{
				if (value != this._storeOutputPercentageText)
				{
					this._storeOutputPercentageText = value;
					base.OnPropertyChangedWithValue<string>(value, "StoreOutputPercentageText");
				}
			}
		}

		// Token: 0x170009CC RID: 2508
		// (get) Token: 0x06001CD7 RID: 7383 RVA: 0x0006AB2E File Offset: 0x00068D2E
		// (set) Token: 0x06001CD8 RID: 7384 RVA: 0x0006AB36 File Offset: 0x00068D36
		public string WarehouseCapacityText
		{
			get
			{
				return this._warehouseCapacityText;
			}
			set
			{
				if (value != this._warehouseCapacityText)
				{
					this._warehouseCapacityText = value;
					base.OnPropertyChangedWithValue<string>(value, "WarehouseCapacityText");
				}
			}
		}

		// Token: 0x170009CD RID: 2509
		// (get) Token: 0x06001CD9 RID: 7385 RVA: 0x0006AB59 File Offset: 0x00068D59
		// (set) Token: 0x06001CDA RID: 7386 RVA: 0x0006AB61 File Offset: 0x00068D61
		public string WarehouseCapacityValue
		{
			get
			{
				return this._warehouseCapacityValue;
			}
			set
			{
				if (value != this._warehouseCapacityValue)
				{
					this._warehouseCapacityValue = value;
					base.OnPropertyChangedWithValue<string>(value, "WarehouseCapacityValue");
				}
			}
		}

		// Token: 0x170009CE RID: 2510
		// (get) Token: 0x06001CDB RID: 7387 RVA: 0x0006AB84 File Offset: 0x00068D84
		// (set) Token: 0x06001CDC RID: 7388 RVA: 0x0006AB8C File Offset: 0x00068D8C
		public bool ReceiveInputFromWarehouse
		{
			get
			{
				return this._receiveInputFromWarehouse;
			}
			set
			{
				if (value != this._receiveInputFromWarehouse)
				{
					this._receiveInputFromWarehouse = value;
					base.OnPropertyChangedWithValue(value, "ReceiveInputFromWarehouse");
				}
			}
		}

		// Token: 0x170009CF RID: 2511
		// (get) Token: 0x06001CDD RID: 7389 RVA: 0x0006ABAA File Offset: 0x00068DAA
		// (set) Token: 0x06001CDE RID: 7390 RVA: 0x0006ABB2 File Offset: 0x00068DB2
		public int WarehouseInputAmount
		{
			get
			{
				return this._warehouseInputAmount;
			}
			set
			{
				if (value != this._warehouseInputAmount)
				{
					this._warehouseInputAmount = value;
					base.OnPropertyChangedWithValue(value, "WarehouseInputAmount");
				}
			}
		}

		// Token: 0x170009D0 RID: 2512
		// (get) Token: 0x06001CDF RID: 7391 RVA: 0x0006ABD0 File Offset: 0x00068DD0
		// (set) Token: 0x06001CE0 RID: 7392 RVA: 0x0006ABD8 File Offset: 0x00068DD8
		public int WarehouseOutputAmount
		{
			get
			{
				return this._warehouseOutputAmount;
			}
			set
			{
				if (value != this._warehouseOutputAmount)
				{
					this._warehouseOutputAmount = value;
					base.OnPropertyChangedWithValue(value, "WarehouseOutputAmount");
				}
			}
		}

		// Token: 0x170009D1 RID: 2513
		// (get) Token: 0x06001CE1 RID: 7393 RVA: 0x0006ABF6 File Offset: 0x00068DF6
		// (set) Token: 0x06001CE2 RID: 7394 RVA: 0x0006ABFE File Offset: 0x00068DFE
		public SelectorVM<WorkshopPercentageSelectorItemVM> WarehousePercentageSelector
		{
			get
			{
				return this._warehousePercentageSelector;
			}
			set
			{
				if (value != this._warehousePercentageSelector)
				{
					this._warehousePercentageSelector = value;
					base.OnPropertyChangedWithValue<SelectorVM<WorkshopPercentageSelectorItemVM>>(value, "WarehousePercentageSelector");
				}
			}
		}

		// Token: 0x04000D5F RID: 3423
		private readonly TextObject _runningText = new TextObject("{=iuKvbKJ7}Running", null);

		// Token: 0x04000D60 RID: 3424
		private readonly TextObject _haltedText = new TextObject("{=zgnEagTJ}Halted", null);

		// Token: 0x04000D61 RID: 3425
		private readonly TextObject _noRawMaterialsText = new TextObject("{=JRKC4ed4}This workshop has not been producing for {DAY} {?PLURAL_DAYS}days{?}day{\\?} due to lack of raw materials in the town market.", null);

		// Token: 0x04000D62 RID: 3426
		private readonly TextObject _noProfitText = new TextObject("{=no0chrAH}This workshop has not been running for {DAY} {?PLURAL_DAYS}days{?}day{\\?} because the production has not been profitable", null);

		// Token: 0x04000D63 RID: 3427
		private readonly TextObject _townRebellionText = new TextObject("{=pDAuV918}This workshop has not been producing for {DAY} {?PLURAL_DAYS}days{?}day{\\?} due to rebel activity in the town.", null);

		// Token: 0x04000D64 RID: 3428
		private readonly IWorkshopWarehouseCampaignBehavior _workshopWarehouseBehavior;

		// Token: 0x04000D65 RID: 3429
		private readonly WorkshopModel _workshopModel;

		// Token: 0x04000D66 RID: 3430
		private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

		// Token: 0x04000D67 RID: 3431
		private readonly Action<ClanFinanceWorkshopItemVM> _onSelectionT;

		// Token: 0x04000D68 RID: 3432
		private ExplainedNumber _inputDetails;

		// Token: 0x04000D69 RID: 3433
		private ExplainedNumber _outputDetails;

		// Token: 0x04000D6A RID: 3434
		private HintViewModel _useWarehouseAsInputHint;

		// Token: 0x04000D6B RID: 3435
		private HintViewModel _storeOutputPercentageHint;

		// Token: 0x04000D6C RID: 3436
		private HintViewModel _manageWorkshopHint;

		// Token: 0x04000D6D RID: 3437
		private BasicTooltipViewModel _inputWarehouseCountsTooltip;

		// Token: 0x04000D6E RID: 3438
		private BasicTooltipViewModel _outputWarehouseCountsTooltip;

		// Token: 0x04000D6F RID: 3439
		private string _workshopTypeId;

		// Token: 0x04000D70 RID: 3440
		private string _inputsText;

		// Token: 0x04000D71 RID: 3441
		private string _outputsText;

		// Token: 0x04000D72 RID: 3442
		private string _inputProducts;

		// Token: 0x04000D73 RID: 3443
		private string _outputProducts;

		// Token: 0x04000D74 RID: 3444
		private string _useWarehouseAsInputText;

		// Token: 0x04000D75 RID: 3445
		private string _storeOutputPercentageText;

		// Token: 0x04000D76 RID: 3446
		private string _warehouseCapacityText;

		// Token: 0x04000D77 RID: 3447
		private string _warehouseCapacityValue;

		// Token: 0x04000D78 RID: 3448
		private bool _receiveInputFromWarehouse;

		// Token: 0x04000D79 RID: 3449
		private int _warehouseInputAmount;

		// Token: 0x04000D7A RID: 3450
		private int _warehouseOutputAmount;

		// Token: 0x04000D7B RID: 3451
		private SelectorVM<WorkshopPercentageSelectorItemVM> _warehousePercentageSelector;
	}
}
