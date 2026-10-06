using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingHistoryVM : ViewModel
{
	private static TextObject _noItemsHint = new TextObject("{=saHYZKLt}There are no available items in history");

	private static TextObject _craftingHistoryText = new TextObject("{=xW4BPVLX}Crafting History");

	private ICraftingCampaignBehavior _craftingBehavior;

	private Func<CraftingOrder> _getActiveOrder;

	private Action<WeaponDesignSelectorVM> _onDone;

	private Crafting _crafting;

	private bool _isDoneAvailable;

	private bool _isVisible;

	private bool _hasItemsInHistory;

	private HintViewModel _historyHint;

	private HintViewModel _historyDisabledHint;

	private MBBindingList<WeaponDesignSelectorVM> _craftingHistory;

	private WeaponDesignSelectorVM _selectedDesign;

	private string _titleText;

	private string _doneText;

	private string _cancelText;

	private InputKeyItemVM _cancelKey;

	private InputKeyItemVM _doneKey;

	[DataSourceProperty]
	public bool IsDoneAvailable
	{
		get
		{
			return _isDoneAvailable;
		}
		set
		{
			if (value != _isDoneAvailable)
			{
				_isDoneAvailable = value;
				OnPropertyChangedWithValue(value, "IsDoneAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool HasItemsInHistory
	{
		get
		{
			return _hasItemsInHistory;
		}
		set
		{
			if (value != _hasItemsInHistory)
			{
				_hasItemsInHistory = value;
				OnPropertyChangedWithValue(value, "HasItemsInHistory");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HistoryHint
	{
		get
		{
			return _historyHint;
		}
		set
		{
			if (value != _historyHint)
			{
				_historyHint = value;
				OnPropertyChangedWithValue(value, "HistoryHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel HistoryDisabledHint
	{
		get
		{
			return _historyDisabledHint;
		}
		set
		{
			if (value != _historyDisabledHint)
			{
				_historyDisabledHint = value;
				OnPropertyChangedWithValue(value, "HistoryDisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<WeaponDesignSelectorVM> CraftingHistory
	{
		get
		{
			return _craftingHistory;
		}
		set
		{
			if (value != _craftingHistory)
			{
				_craftingHistory = value;
				OnPropertyChangedWithValue(value, "CraftingHistory");
			}
		}
	}

	[DataSourceProperty]
	public WeaponDesignSelectorVM SelectedDesign
	{
		get
		{
			return _selectedDesign;
		}
		set
		{
			if (value != _selectedDesign)
			{
				_selectedDesign = value;
				OnPropertyChangedWithValue(value, "SelectedDesign");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	public InputKeyItemVM CancelKey
	{
		get
		{
			return _cancelKey;
		}
		set
		{
			if (value != _cancelKey)
			{
				_cancelKey = value;
				OnPropertyChangedWithValue(value, "CancelKey");
			}
		}
	}

	public InputKeyItemVM DoneKey
	{
		get
		{
			return _doneKey;
		}
		set
		{
			if (value != _doneKey)
			{
				_doneKey = value;
				OnPropertyChangedWithValue(value, "DoneKey");
			}
		}
	}

	public CraftingHistoryVM(Crafting crafting, ICraftingCampaignBehavior craftingBehavior, Func<CraftingOrder> getActiveOrder, Action<WeaponDesignSelectorVM> onDone)
	{
		_crafting = crafting;
		_craftingBehavior = craftingBehavior;
		_getActiveOrder = getActiveOrder;
		_onDone = onDone;
		CraftingHistory = new MBBindingList<WeaponDesignSelectorVM>();
		HistoryHint = new HintViewModel(_craftingHistoryText);
		HistoryDisabledHint = new HintViewModel(_noItemsHint);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = _craftingHistoryText.ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		RefreshAvailability();
	}

	private void RefreshCraftingHistory()
	{
		FinalizeHistory();
		CraftingOrder craftingOrder = _getActiveOrder();
		foreach (TaleWorlds.Core.WeaponDesign item in _craftingBehavior.CraftingHistory)
		{
			if (craftingOrder == null || item.Template.TemplateName.ToString() == craftingOrder.PreCraftedWeaponDesignItem.WeaponDesign.Template.TemplateName.ToString())
			{
				CraftingHistory.Add(new WeaponDesignSelectorVM(item, ExecuteSelect));
			}
		}
		HasItemsInHistory = CraftingHistory.Count > 0;
		ExecuteSelect(null);
	}

	private void FinalizeHistory()
	{
		if (CraftingHistory.Count > 0)
		{
			foreach (WeaponDesignSelectorVM item in CraftingHistory)
			{
				item.OnFinalize();
			}
		}
		CraftingHistory.Clear();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		FinalizeHistory();
		DoneKey.OnFinalize();
		CancelKey.OnFinalize();
	}

	public void RefreshAvailability()
	{
		CraftingOrder activeOrder = _getActiveOrder();
		HasItemsInHistory = ((activeOrder == null) ? (_craftingBehavior.CraftingHistory.Count > 0) : _craftingBehavior.CraftingHistory.Any((TaleWorlds.Core.WeaponDesign x) => x.Template.StringId == activeOrder.PreCraftedWeaponDesignItem.WeaponDesign.Template.StringId));
	}

	public void ExecuteOpen()
	{
		RefreshCraftingHistory();
		IsVisible = true;
	}

	public void ExecuteCancel()
	{
		IsVisible = false;
	}

	public void ExecuteDone()
	{
		_onDone?.Invoke(SelectedDesign);
		ExecuteCancel();
	}

	private void ExecuteSelect(WeaponDesignSelectorVM selector)
	{
		IsDoneAvailable = selector != null;
		if (SelectedDesign != null)
		{
			SelectedDesign.IsSelected = false;
		}
		SelectedDesign = selector;
		if (SelectedDesign != null)
		{
			SelectedDesign.IsSelected = true;
		}
	}

	public void SetDoneKey(HotKey hotkey)
	{
		DoneKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelKey(HotKey hotkey)
	{
		CancelKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
