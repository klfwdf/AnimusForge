using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.AdminPanel;

public class MultiplayerAdminPanelMultiSelectionOptionVM : MultiplayerAdminPanelOptionBaseVM
{
	public class AdminPanelOptionSelectorVM : SelectorVM<AdminPanelOptionSelectorItemVM>
	{
		private bool _isEnabled;

		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return _isEnabled;
			}
			set
			{
				if (value != _isEnabled)
				{
					_isEnabled = value;
					OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		public AdminPanelOptionSelectorVM(int selectedIndex, Action<SelectorVM<AdminPanelOptionSelectorItemVM>> onChange)
			: base(selectedIndex, onChange)
		{
		}
	}

	public class AdminPanelOptionSelectorItemVM : SelectorItemVM
	{
		public readonly IAdminPanelMultiSelectionItem SelectionItem;

		public AdminPanelOptionSelectorItemVM(IAdminPanelMultiSelectionItem selectionItem)
			: base(selectionItem?.DisplayName ?? selectionItem?.Value)
		{
			SelectionItem = selectionItem;
		}
	}

	private new readonly IAdminPanelMultiSelectionOption _option;

	private readonly SelectorItemVM _initialValue;

	private bool _isMultiSelectionOption;

	private AdminPanelOptionSelectorVM _multiSelectionOptions;

	[DataSourceProperty]
	public bool IsMultiSelectionOption
	{
		get
		{
			return _isMultiSelectionOption;
		}
		set
		{
			if (value != _isMultiSelectionOption)
			{
				_isMultiSelectionOption = value;
				OnPropertyChangedWithValue(value, "IsMultiSelectionOption");
			}
		}
	}

	[DataSourceProperty]
	public AdminPanelOptionSelectorVM MultiSelectionOptions
	{
		get
		{
			return _multiSelectionOptions;
		}
		set
		{
			if (value != _multiSelectionOptions)
			{
				_multiSelectionOptions = value;
				OnPropertyChangedWithValue(value, "MultiSelectionOptions");
			}
		}
	}

	public MultiplayerAdminPanelMultiSelectionOptionVM(IAdminPanelMultiSelectionOption option)
		: base(option)
	{
		_option = option;
		MultiSelectionOptions = new AdminPanelOptionSelectorVM(-1, null);
		IsMultiSelectionOption = true;
		RefreshValues();
		_initialValue = MultiSelectionOptions.SelectedItem;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RefreshOptions();
	}

	private void OnSelectorChange(SelectorVM<AdminPanelOptionSelectorItemVM> selector)
	{
		_option.SetValue(selector.SelectedItem?.SelectionItem);
	}

	public override void UpdateValues()
	{
		base.UpdateValues();
		if (!_option.GetAvailableOptions().SequenceEqual(MultiSelectionOptions.ItemList.Select((AdminPanelOptionSelectorItemVM i) => i.SelectionItem)))
		{
			RefreshOptions();
		}
		for (int num = 0; num < MultiSelectionOptions.ItemList.Count; num++)
		{
			if (MultiSelectionOptions.ItemList[num].SelectionItem == _option.GetValue())
			{
				MultiSelectionOptions.SelectedIndex = num;
				break;
			}
		}
	}

	public override void ExecuteRestoreDefaults()
	{
		base.ExecuteRestoreDefaults();
	}

	public override void ExecuteRevertChanges()
	{
		base.ExecuteRevertChanges();
	}

	private void RefreshOptions()
	{
		if (MultiSelectionOptions == null)
		{
			return;
		}
		List<AdminPanelOptionSelectorItemVM> list = new List<AdminPanelOptionSelectorItemVM>();
		if (_option == null)
		{
			MultiSelectionOptions.Refresh(list, 0, OnSelectorChange);
			return;
		}
		IAdminPanelMultiSelectionItem value = _option.GetValue();
		MBReadOnlyList<IAdminPanelMultiSelectionItem> mBReadOnlyList = _option.GetAvailableOptions() ?? new MBReadOnlyList<IAdminPanelMultiSelectionItem>();
		if (mBReadOnlyList != null)
		{
			for (int i = 0; i < mBReadOnlyList.Count; i++)
			{
				list.Add(new AdminPanelOptionSelectorItemVM(mBReadOnlyList[i]));
			}
		}
		MultiSelectionOptions.IsEnabled = !base.IsDisabled;
		MultiSelectionOptions.Refresh(list, 0, OnSelectorChange);
		if (MultiSelectionOptions == null)
		{
			return;
		}
		for (int j = 0; j < MultiSelectionOptions.ItemList.Count; j++)
		{
			if (MultiSelectionOptions.ItemList[j].SelectionItem == value)
			{
				MultiSelectionOptions.SelectedIndex = j;
				break;
			}
		}
	}
}
