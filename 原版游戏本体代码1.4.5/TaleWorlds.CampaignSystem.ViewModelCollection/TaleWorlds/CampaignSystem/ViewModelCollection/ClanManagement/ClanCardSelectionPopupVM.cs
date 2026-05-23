using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanCardSelectionPopupVM : ViewModel
{
	private TextObject _titleText;

	private bool _isMultiSelection;

	private int _minimumSelection;

	private int _maximumSelection;

	private ClanCardSelectionPopupItemVM _lastSelectedItem;

	private int _selectedItemCount;

	private Action<List<object>, Action> _onClosed;

	private MBBindingList<ClanCardSelectionPopupItemVM> _items;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private string _title;

	private string _actionResult;

	private string _doneLbl;

	private bool _isVisible;

	private bool _isDoneEnabled;

	private HintViewModel _disabledHint;

	[DataSourceProperty]
	public MBBindingList<ClanCardSelectionPopupItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string ActionResult
	{
		get
		{
			return _actionResult;
		}
		set
		{
			if (value != _actionResult)
			{
				_actionResult = value;
				OnPropertyChangedWithValue(value, "ActionResult");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
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
	public bool IsDoneEnabled
	{
		get
		{
			return _isDoneEnabled;
		}
		set
		{
			if (value != _isDoneEnabled)
			{
				_isDoneEnabled = value;
				OnPropertyChangedWithValue(value, "IsDoneEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledHint
	{
		get
		{
			return _disabledHint;
		}
		set
		{
			if (value != _disabledHint)
			{
				_disabledHint = value;
				OnPropertyChangedWithValue(value, "DisabledHint");
			}
		}
	}

	public ClanCardSelectionPopupVM()
	{
		_titleText = TextObject.GetEmpty();
		Items = new MBBindingList<ClanCardSelectionPopupItemVM>();
		DisabledHint = new HintViewModel();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (!_isMultiSelection)
		{
			ActionResult = _lastSelectedItem?.ActionResultText?.ToString() ?? string.Empty;
		}
		DoneLbl = GameTexts.FindText("str_done").ToString();
		Title = _titleText?.ToString() ?? string.Empty;
		Items.ApplyActionOnAllItems(delegate(ClanCardSelectionPopupItemVM x)
		{
			x.RefreshValues();
		});
		RefreshHintText();
	}

	private void RefreshHintText()
	{
		TextObject textObject = TextObject.GetEmpty();
		if (_isMultiSelection)
		{
			if (_maximumSelection > 0 && _selectedItemCount > _maximumSelection)
			{
				textObject = new TextObject("{=lIGdkJGm}You must choose less than {NUMBER} {?NUMBER>1}items{?}item{\\?}");
				textObject.SetTextVariable("NUMBER", _maximumSelection);
			}
			else if (_selectedItemCount < _minimumSelection)
			{
				textObject = new TextObject("{=woD234nb}You must choose more than {NUMBER} {?NUMBER>1}items{?}item{\\?}");
				textObject.SetTextVariable("NUMBER", _minimumSelection);
			}
		}
		else if (_selectedItemCount != 1)
		{
			textObject = new TextObject("{=aYm5Ehv1}You must choose an item");
		}
		DisabledHint.HintText = textObject;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void Open(ClanCardSelectionInfo info)
	{
		_isMultiSelection = info.IsMultiSelection;
		_minimumSelection = info.MinimumSelection;
		_maximumSelection = info.MaximumSelection;
		_titleText = info.Title;
		_onClosed = info.OnClosedAction;
		foreach (ClanCardSelectionItemInfo item in info.Items)
		{
			ClanCardSelectionItemInfo info2 = item;
			Items.Add(new ClanCardSelectionPopupItemVM(in info2, OnItemSelected));
		}
		_selectedItemCount = 0;
		RefreshValues();
		IsVisible = true;
		UpdateIsDoneEnabled();
	}

	public void ExecuteCancel()
	{
		_onClosed?.Invoke(new List<object>(), null);
		Close();
	}

	public void ExecuteDone()
	{
		List<object> selectedItems = new List<object>();
		Items.ApplyActionOnAllItems(delegate(ClanCardSelectionPopupItemVM x)
		{
			if (x.IsSelected)
			{
				selectedItems.Add(x.Identifier);
			}
		});
		_onClosed?.Invoke(selectedItems, Close);
	}

	private void Close()
	{
		IsVisible = false;
		_lastSelectedItem = null;
		_titleText = TextObject.GetEmpty();
		ActionResult = string.Empty;
		Title = string.Empty;
		_onClosed = null;
		Items.Clear();
	}

	private void OnItemSelected(ClanCardSelectionPopupItemVM item)
	{
		if (_isMultiSelection)
		{
			item.IsSelected = !item.IsSelected;
			if (item.IsSelected)
			{
				_selectedItemCount++;
			}
			else
			{
				_selectedItemCount--;
			}
		}
		else if (item != _lastSelectedItem)
		{
			if (_lastSelectedItem != null)
			{
				_lastSelectedItem.IsSelected = false;
			}
			item.IsSelected = true;
			ActionResult = item.ActionResultText?.ToString() ?? string.Empty;
			_selectedItemCount = 1;
		}
		_lastSelectedItem = item;
		UpdateIsDoneEnabled();
		RefreshHintText();
	}

	private void UpdateIsDoneEnabled()
	{
		if (_isMultiSelection)
		{
			IsDoneEnabled = _selectedItemCount >= _minimumSelection && (_maximumSelection <= 0 || _selectedItemCount <= _maximumSelection);
		}
		else
		{
			IsDoneEnabled = _selectedItemCount == 1;
		}
	}
}
