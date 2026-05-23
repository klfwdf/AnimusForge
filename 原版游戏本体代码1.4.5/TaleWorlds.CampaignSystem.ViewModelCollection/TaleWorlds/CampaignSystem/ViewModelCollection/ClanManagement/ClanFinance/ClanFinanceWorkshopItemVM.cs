using System;
using System.Collections.Generic;
using System.Linq;
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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;

public class ClanFinanceWorkshopItemVM : ClanFinanceIncomeItemBaseVM
{
	private readonly TextObject _runningText = new TextObject("{=iuKvbKJ7}Running");

	private readonly TextObject _haltedText = new TextObject("{=zgnEagTJ}Halted");

	private readonly TextObject _noRawMaterialsText = new TextObject("{=JRKC4ed4}This workshop has not been producing for {DAY} {?PLURAL_DAYS}days{?}day{\\?} due to lack of raw materials in the town market.");

	private readonly TextObject _noProfitText = new TextObject("{=no0chrAH}This workshop has not been running for {DAY} {?PLURAL_DAYS}days{?}day{\\?} because the production has not been profitable");

	private readonly TextObject _townRebellionText = new TextObject("{=pDAuV918}This workshop has not been producing for {DAY} {?PLURAL_DAYS}days{?}day{\\?} due to rebel activity in the town.");

	private readonly IWorkshopWarehouseCampaignBehavior _workshopWarehouseBehavior;

	private readonly WorkshopModel _workshopModel;

	private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

	private readonly Action<ClanFinanceWorkshopItemVM> _onSelectionT;

	private ExplainedNumber _inputDetails;

	private ExplainedNumber _outputDetails;

	private HintViewModel _useWarehouseAsInputHint;

	private HintViewModel _storeOutputPercentageHint;

	private HintViewModel _manageWorkshopHint;

	private BasicTooltipViewModel _inputWarehouseCountsTooltip;

	private BasicTooltipViewModel _outputWarehouseCountsTooltip;

	private string _workshopTypeId;

	private string _inputsText;

	private string _outputsText;

	private string _inputProducts;

	private string _outputProducts;

	private string _useWarehouseAsInputText;

	private string _storeOutputPercentageText;

	private string _warehouseCapacityText;

	private string _warehouseCapacityValue;

	private bool _receiveInputFromWarehouse;

	private int _warehouseInputAmount;

	private int _warehouseOutputAmount;

	private SelectorVM<WorkshopPercentageSelectorItemVM> _warehousePercentageSelector;

	public Workshop Workshop { get; private set; }

	[DataSourceProperty]
	public HintViewModel UseWarehouseAsInputHint
	{
		get
		{
			return _useWarehouseAsInputHint;
		}
		set
		{
			if (value != _useWarehouseAsInputHint)
			{
				_useWarehouseAsInputHint = value;
				OnPropertyChangedWithValue(value, "UseWarehouseAsInputHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel StoreOutputPercentageHint
	{
		get
		{
			return _storeOutputPercentageHint;
		}
		set
		{
			if (value != _storeOutputPercentageHint)
			{
				_storeOutputPercentageHint = value;
				OnPropertyChangedWithValue(value, "StoreOutputPercentageHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ManageWorkshopHint
	{
		get
		{
			return _manageWorkshopHint;
		}
		set
		{
			if (value != _manageWorkshopHint)
			{
				_manageWorkshopHint = value;
				OnPropertyChangedWithValue(value, "ManageWorkshopHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel InputWarehouseCountsTooltip
	{
		get
		{
			return _inputWarehouseCountsTooltip;
		}
		set
		{
			if (value != _inputWarehouseCountsTooltip)
			{
				_inputWarehouseCountsTooltip = value;
				OnPropertyChangedWithValue(value, "InputWarehouseCountsTooltip");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel OutputWarehouseCountsTooltip
	{
		get
		{
			return _outputWarehouseCountsTooltip;
		}
		set
		{
			if (value != _outputWarehouseCountsTooltip)
			{
				_outputWarehouseCountsTooltip = value;
				OnPropertyChangedWithValue(value, "OutputWarehouseCountsTooltip");
			}
		}
	}

	public string WorkshopTypeId
	{
		get
		{
			return _workshopTypeId;
		}
		set
		{
			if (value != _workshopTypeId)
			{
				_workshopTypeId = value;
				OnPropertyChangedWithValue(value, "WorkshopTypeId");
			}
		}
	}

	public string InputsText
	{
		get
		{
			return _inputsText;
		}
		set
		{
			if (value != _inputsText)
			{
				_inputsText = value;
				OnPropertyChangedWithValue(value, "InputsText");
			}
		}
	}

	public string OutputsText
	{
		get
		{
			return _outputsText;
		}
		set
		{
			if (value != _outputsText)
			{
				_outputsText = value;
				OnPropertyChangedWithValue(value, "OutputsText");
			}
		}
	}

	public string InputProducts
	{
		get
		{
			return _inputProducts;
		}
		set
		{
			if (value != _inputProducts)
			{
				_inputProducts = value;
				OnPropertyChangedWithValue(value, "InputProducts");
			}
		}
	}

	public string OutputProducts
	{
		get
		{
			return _outputProducts;
		}
		set
		{
			if (value != _outputProducts)
			{
				_outputProducts = value;
				OnPropertyChangedWithValue(value, "OutputProducts");
			}
		}
	}

	public string UseWarehouseAsInputText
	{
		get
		{
			return _useWarehouseAsInputText;
		}
		set
		{
			if (value != _useWarehouseAsInputText)
			{
				_useWarehouseAsInputText = value;
				OnPropertyChangedWithValue(value, "UseWarehouseAsInputText");
			}
		}
	}

	public string StoreOutputPercentageText
	{
		get
		{
			return _storeOutputPercentageText;
		}
		set
		{
			if (value != _storeOutputPercentageText)
			{
				_storeOutputPercentageText = value;
				OnPropertyChangedWithValue(value, "StoreOutputPercentageText");
			}
		}
	}

	public string WarehouseCapacityText
	{
		get
		{
			return _warehouseCapacityText;
		}
		set
		{
			if (value != _warehouseCapacityText)
			{
				_warehouseCapacityText = value;
				OnPropertyChangedWithValue(value, "WarehouseCapacityText");
			}
		}
	}

	public string WarehouseCapacityValue
	{
		get
		{
			return _warehouseCapacityValue;
		}
		set
		{
			if (value != _warehouseCapacityValue)
			{
				_warehouseCapacityValue = value;
				OnPropertyChangedWithValue(value, "WarehouseCapacityValue");
			}
		}
	}

	public bool ReceiveInputFromWarehouse
	{
		get
		{
			return _receiveInputFromWarehouse;
		}
		set
		{
			if (value != _receiveInputFromWarehouse)
			{
				_receiveInputFromWarehouse = value;
				OnPropertyChangedWithValue(value, "ReceiveInputFromWarehouse");
			}
		}
	}

	public int WarehouseInputAmount
	{
		get
		{
			return _warehouseInputAmount;
		}
		set
		{
			if (value != _warehouseInputAmount)
			{
				_warehouseInputAmount = value;
				OnPropertyChangedWithValue(value, "WarehouseInputAmount");
			}
		}
	}

	public int WarehouseOutputAmount
	{
		get
		{
			return _warehouseOutputAmount;
		}
		set
		{
			if (value != _warehouseOutputAmount)
			{
				_warehouseOutputAmount = value;
				OnPropertyChangedWithValue(value, "WarehouseOutputAmount");
			}
		}
	}

	public SelectorVM<WorkshopPercentageSelectorItemVM> WarehousePercentageSelector
	{
		get
		{
			return _warehousePercentageSelector;
		}
		set
		{
			if (value != _warehousePercentageSelector)
			{
				_warehousePercentageSelector = value;
				OnPropertyChangedWithValue(value, "WarehousePercentageSelector");
			}
		}
	}

	public ClanFinanceWorkshopItemVM(Workshop workshop, Action<ClanFinanceWorkshopItemVM> onSelection, Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
		: base(null, onRefresh)
	{
		_workshopWarehouseBehavior = Campaign.Current.GetCampaignBehavior<IWorkshopWarehouseCampaignBehavior>();
		Workshop = workshop;
		_openCardSelectionPopup = openCardSelectionPopup;
		_workshopModel = Campaign.Current.Models.WorkshopModel;
		base.IncomeTypeAsEnum = IncomeTypes.Workshop;
		_onSelection = tempOnSelection;
		_onSelectionT = onSelection;
		SettlementComponent settlementComponent = Workshop.Settlement.SettlementComponent;
		base.ImageName = ((settlementComponent != null) ? settlementComponent.WaitMeshName : "");
		ManageWorkshopHint = new HintViewModel(new TextObject("{=LxWVtDF0}Manage Workshop"));
		UseWarehouseAsInputHint = new HintViewModel(new TextObject("{=a4oqWgUi}If there are no raw materials in the warehouse, the workshop will buy raw materials from the market until the warehouse is restocked"));
		StoreOutputPercentageHint = new HintViewModel(new TextObject("{=NVUi4bB9}When the warehouse is full, the workshop will sell the products to the town market"));
		InputWarehouseCountsTooltip = new BasicTooltipViewModel();
		OutputWarehouseCountsTooltip = new BasicTooltipViewModel();
		ReceiveInputFromWarehouse = _workshopWarehouseBehavior.IsGettingInputsFromWarehouse(workshop);
		WarehousePercentageSelector = new SelectorVM<WorkshopPercentageSelectorItemVM>(0, OnStoreOutputInWarehousePercentageUpdated);
		RefreshStoragePercentages();
		float currentPercentage = _workshopWarehouseBehavior.GetStockProductionInWarehouseRatio(workshop);
		WorkshopPercentageSelectorItemVM workshopPercentageSelectorItemVM = WarehousePercentageSelector.ItemList.FirstOrDefault((WorkshopPercentageSelectorItemVM x) => x.Percentage.ApproximatelyEqualsTo(currentPercentage, 0.1f));
		WarehousePercentageSelector.SelectedIndex = ((workshopPercentageSelectorItemVM != null) ? WarehousePercentageSelector.ItemList.IndexOf(workshopPercentageSelectorItemVM) : 0);
		RefreshValues();
	}

	private void tempOnSelection(ClanFinanceIncomeItemBaseVM temp)
	{
		_onSelectionT(this);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Workshop.WorkshopType.Name.ToString();
		WorkshopTypeId = Workshop.WorkshopType.StringId;
		base.Location = Workshop.Settlement.Name.ToString();
		base.Income = (int)((float)Workshop.ProfitMade * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction()));
		base.IncomeValueText = DetermineIncomeText(base.Income);
		InputsText = GameTexts.FindText("str_clan_workshop_inputs").ToString();
		OutputsText = GameTexts.FindText("str_clan_workshop_outputs").ToString();
		StoreOutputPercentageText = new TextObject("{=y6qCNFQj}Store Outputs in the Warehouse").ToString();
		UseWarehouseAsInputText = new TextObject("{=88WPmTKH}Get Input from the Warehouse").ToString();
		WarehouseCapacityText = new TextObject("{=X6eG4Q5V}Warehouse Capacity").ToString();
		float warehouseItemRosterWeight = _workshopWarehouseBehavior.GetWarehouseItemRosterWeight(Workshop.Settlement);
		int warehouseCapacity = Campaign.Current.Models.WorkshopModel.WarehouseCapacity;
		WarehouseCapacityValue = GameTexts.FindText("str_LEFT_over_RIGHT").SetTextVariable("LEFT", warehouseItemRosterWeight).SetTextVariable("RIGHT", warehouseCapacity)
			.ToString();
		WarehouseInputAmount = _workshopWarehouseBehavior.GetInputCount(Workshop);
		WarehouseOutputAmount = _workshopWarehouseBehavior.GetOutputCount(Workshop);
		_inputDetails = _workshopWarehouseBehavior.GetInputDailyChange(Workshop);
		_outputDetails = _workshopWarehouseBehavior.GetOutputDailyChange(Workshop);
		InputWarehouseCountsTooltip.SetToolipCallback(() => GetWarehouseInputOutputTooltip(isInput: true));
		OutputWarehouseCountsTooltip.SetToolipCallback(() => GetWarehouseInputOutputTooltip(isInput: false));
		base.ItemProperties.Clear();
		PopulateStatsList();
	}

	private List<TooltipProperty> GetWarehouseInputOutputTooltip(bool isInput)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		ExplainedNumber explainedNumber = (isInput ? _inputDetails : _outputDetails);
		if (!explainedNumber.ResultNumber.ApproximatelyEqualsTo(0f))
		{
			list.Add(new TooltipProperty(new TextObject("{=Y9egTJg0}Daily Change").ToString(), "", 1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty("", "", 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			foreach (var line in explainedNumber.GetLines())
			{
				string value = GameTexts.FindText("str_clan_workshop_material_daily_Change").SetTextVariable("CHANGE", TaleWorlds.Library.MathF.Abs(line.number).ToString("F1")).SetTextVariable("IS_POSITIVE", (line.number > 0f) ? 1 : 0)
					.ToString();
				list.Add(new TooltipProperty(line.name, value, 0));
			}
		}
		return list;
	}

	private void RefreshStoragePercentages()
	{
		WarehousePercentageSelector.ItemList.Clear();
		TextObject textObject = GameTexts.FindText("str_NUMBER_percent");
		textObject.SetTextVariable("NUMBER", 0);
		WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0f));
		textObject.SetTextVariable("NUMBER", 25);
		WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0.25f));
		textObject.SetTextVariable("NUMBER", 50);
		WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0.5f));
		textObject.SetTextVariable("NUMBER", 75);
		WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 0.75f));
		textObject.SetTextVariable("NUMBER", 100);
		WarehousePercentageSelector.AddItem(new WorkshopPercentageSelectorItemVM(textObject.ToString(), 1f));
	}

	public void ExecuteToggleWarehouseUsage()
	{
		ReceiveInputFromWarehouse = !ReceiveInputFromWarehouse;
		_workshopWarehouseBehavior.SetIsGettingInputsFromWarehouse(Workshop, ReceiveInputFromWarehouse);
		base.ItemProperties.Clear();
		PopulateStatsList();
	}

	protected override void PopulateStatsList()
	{
		(TextObject, bool, BasicTooltipViewModel) workshopStatus = GetWorkshopStatus(Workshop);
		if (!TextObject.IsNullOrEmpty(workshopStatus.Item1))
		{
			base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=DXczLzml}Status").ToString(), workshopStatus.Item1.ToString(), workshopStatus.Item2, workshopStatus.Item3));
		}
		SelectableItemPropertyVM currentCapitalProperty = GetCurrentCapitalProperty();
		base.ItemProperties.Add(currentCapitalProperty);
		base.ItemProperties.Add(new SelectableItemPropertyVM(new TextObject("{=CaRbMaZY}Daily Wage").ToString(), Workshop.Expense.ToString()));
		GetWorkshopTypeProductionTexts(Workshop.WorkshopType, out var inputsText, out var outputsText);
		InputProducts = inputsText.ToString();
		OutputProducts = outputsText.ToString();
	}

	private SelectableItemPropertyVM GetCurrentCapitalProperty()
	{
		string name = new TextObject("{=Ra17aK4e}Current Capital").ToString();
		string value = Workshop.Capital.ToString();
		bool isWarning = false;
		BasicTooltipViewModel basicTooltipViewModel = null;
		if (Workshop.Capital < _workshopModel.CapitalLowLimit)
		{
			isWarning = true;
			basicTooltipViewModel = new BasicTooltipViewModel(() => new TextObject("{=Qu5clctb}The workshop is losing money. The expenses are being paid from your treasury because the workshop's capital is below {LOWER_THRESHOLD} denars").SetTextVariable("LOWER_THRESHOLD", _workshopModel.CapitalLowLimit).ToString());
		}
		else
		{
			TextObject text = new TextObject("{=dEMUqz2Y}This workshop will send 20% of its profits above {INITIAL_CAPITAL} capital to your treasury");
			text.SetTextVariable("INITIAL_CAPITAL", Campaign.Current.Models.WorkshopModel.InitialCapital);
			basicTooltipViewModel = new BasicTooltipViewModel(() => text.ToString());
		}
		return new SelectableItemPropertyVM(name, value, isWarning, basicTooltipViewModel);
	}

	private (TextObject Status, bool IsWarning, BasicTooltipViewModel Hint) GetWorkshopStatus(Workshop workshop)
	{
		bool item = false;
		BasicTooltipViewModel item2 = null;
		TextObject item3;
		if (workshop.LastRunCampaignTime.ElapsedDaysUntilNow >= 1f)
		{
			item3 = _haltedText;
			item = true;
			TextObject tooltipText = TextObject.GetEmpty();
			if (workshop.Settlement.Town.InRebelliousState)
			{
				tooltipText = _townRebellionText;
			}
			else if (!_workshopWarehouseBehavior.IsRawMaterialsSufficientInTownMarket(workshop))
			{
				tooltipText = _noRawMaterialsText;
			}
			else if (WarehousePercentageSelector.SelectedItem.Percentage < 1f)
			{
				tooltipText = _noProfitText;
			}
			int num = (int)workshop.LastRunCampaignTime.ElapsedDaysUntilNow;
			tooltipText.SetTextVariable("DAY", num);
			tooltipText.SetTextVariable("PLURAL_DAYS", (num == 1) ? "0" : "1");
			item2 = new BasicTooltipViewModel(() => tooltipText.ToString());
		}
		else
		{
			item3 = _runningText;
		}
		return (Status: item3, IsWarning: item, Hint: item2);
	}

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

	public void ExecuteBeginWorkshopHint()
	{
		if (Workshop.WorkshopType != null)
		{
			InformationManager.ShowTooltip(typeof(Workshop), Workshop);
		}
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void OnStoreOutputInWarehousePercentageUpdated(SelectorVM<WorkshopPercentageSelectorItemVM> selector)
	{
		if (selector.SelectedIndex != -1)
		{
			_workshopWarehouseBehavior.SetStockProductionInWarehouseRatio(Workshop, selector.SelectedItem.Percentage);
			_inputDetails = _workshopWarehouseBehavior.GetInputDailyChange(Workshop);
			_outputDetails = _workshopWarehouseBehavior.GetOutputDailyChange(Workshop);
		}
	}

	public void ExecuteManageWorkshop()
	{
		TextObject title = new TextObject("{=LxWVtDF0}Manage Workshop");
		ClanCardSelectionInfo obj = new ClanCardSelectionInfo(title, GetManageWorkshopItems(), OnManageWorkshopDone, isMultiSelection: false);
		_openCardSelectionPopup?.Invoke(obj);
	}

	private IEnumerable<ClanCardSelectionItemInfo> GetManageWorkshopItems()
	{
		int costForNotable = _workshopModel.GetCostForNotable(Workshop);
		TextObject textObject = new TextObject("{=ysireFjT}Sell This Workshop for {GOLD_AMOUNT}{GOLD_ICON}");
		textObject.SetTextVariable("GOLD_AMOUNT", costForNotable);
		textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		yield return new ClanCardSelectionItemInfo(textObject, isDisabled: false, null, ClanCardSelectionItemPropertyInfo.CreateActionGoldChangeText(costForNotable));
		foreach (WorkshopType item in WorkshopType.All)
		{
			if (Workshop.WorkshopType != item && !item.IsHidden)
			{
				TextObject name = item.Name;
				int convertProductionCost = _workshopModel.GetConvertProductionCost(item);
				TextObject textObject2 = new TextObject("{=av51ur2M}You need at least {REQUIRED_AMOUNT} denars to change the production type of this workshop.");
				textObject2.SetTextVariable("REQUIRED_AMOUNT", convertProductionCost);
				bool flag = convertProductionCost <= Hero.MainHero.Gold;
				yield return new ClanCardSelectionItemInfo(item, name, null, CardSelectionItemSpriteType.Workshop, item.StringId, null, GetWorkshopItemProperties(item), !flag, textObject2, ClanCardSelectionItemPropertyInfo.CreateActionGoldChangeText(-convertProductionCost));
			}
		}
	}

	private IEnumerable<ClanCardSelectionItemPropertyInfo> GetWorkshopItemProperties(WorkshopType workshopType)
	{
		int num = Workshop?.Settlement?.Town?.Workshops?.Count((Workshop x) => x?.WorkshopType == workshopType) ?? 0;
		TextObject textObject = ((num == 0) ? new TextObject("{=gu5xmV0E}No other {WORKSHOP_NAME} in this town.") : new TextObject("{=lhIpaGt9}There {?(COUNT > 1)}are{?}is{\\?} {COUNT} more {?(COUNT > 1)}{PLURAL(WORKSHOP_NAME)}{?}{WORKSHOP_NAME}{\\?} in this town."));
		textObject.SetTextVariable("WORKSHOP_NAME", workshopType.Name);
		textObject.SetTextVariable("COUNT", num);
		GetWorkshopTypeProductionTexts(workshopType, out var inputsText, out var outputsText);
		yield return new ClanCardSelectionItemPropertyInfo(textObject);
		yield return new ClanCardSelectionItemPropertyInfo(ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(new TextObject("{=XCz81XYm}Inputs"), inputsText));
		yield return new ClanCardSelectionItemPropertyInfo(ClanCardSelectionItemPropertyInfo.CreateLabeledValueText(new TextObject("{=ErnykQEH}Outputs"), outputsText));
	}

	private void OnManageWorkshopDone(List<object> selectedItems, Action closePopup)
	{
		closePopup?.Invoke();
		if (selectedItems.Count != 1)
		{
			return;
		}
		WorkshopType workshopType = (WorkshopType)selectedItems[0];
		if (workshopType == null)
		{
			if (Workshop.Settlement.Town.Workshops.Count((Workshop x) => x.Owner == Hero.MainHero) == 1)
			{
				bool flag = Hero.MainHero.CurrentSettlement == Workshop.Settlement;
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=HiJTlBgF}Sell Workshop").ToString(), flag ? new TextObject("{=s06mScpJ}If you have goods in the warehouse, they will be transferred to your party. Are you sure?").ToString() : new TextObject("{=yuxBDKgM}If you have goods in the warehouse, they will be lost! Are you sure?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), ExecuteSellWorkshop, null));
			}
			else
			{
				ExecuteSellWorkshop();
			}
		}
		else
		{
			ChangeProductionTypeOfWorkshopAction.Apply(Workshop, workshopType);
		}
		_onRefresh?.Invoke();
	}

	private void ExecuteSellWorkshop()
	{
		Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(Workshop);
		ChangeOwnerOfWorkshopAction.ApplyByPlayerSelling(Workshop, notableOwnerForWorkshop, Workshop.WorkshopType);
		_onRefresh?.Invoke();
	}
}
