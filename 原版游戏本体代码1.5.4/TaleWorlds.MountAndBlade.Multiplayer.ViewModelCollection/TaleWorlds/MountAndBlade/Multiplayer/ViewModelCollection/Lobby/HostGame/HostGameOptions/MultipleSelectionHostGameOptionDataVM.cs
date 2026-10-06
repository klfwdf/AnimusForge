using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame.HostGameOptions;

public class MultipleSelectionHostGameOptionDataVM : GenericHostGameOptionDataVM
{
	public Action<MultipleSelectionHostGameOptionDataVM> OnChangedSelection;

	private SelectorVM<SelectorItemVM> _selector;

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> Selector
	{
		get
		{
			return _selector;
		}
		set
		{
			if (value != _selector)
			{
				_selector = value;
				OnPropertyChangedWithValue(value, "Selector");
			}
		}
	}

	public MultipleSelectionHostGameOptionDataVM(MultiplayerOptions.OptionType optionType, int preferredIndex)
		: base(OptionsVM.OptionsDataType.MultipleSelectionOption, optionType, preferredIndex)
	{
		List<string> multiplayerOptionsList = MultiplayerOptions.Instance.GetMultiplayerOptionsList(base.OptionType);
		List<string> multiplayerOptionsTextList = MultiplayerOptions.Instance.GetMultiplayerOptionsTextList(base.OptionType);
		List<string> list = new List<string>();
		foreach (string item in multiplayerOptionsTextList)
		{
			list.Add(item);
		}
		Selector = new SelectorVM<SelectorItemVM>(list, multiplayerOptionsList.IndexOf(MultiplayerOptions.Instance.GetValueTextForOptionWithMultipleSelection(base.OptionType)), null);
		Selector.SetOnChangeAction(OnChangeSelected);
	}

	public override void RefreshData()
	{
		Selector.SetOnChangeAction(null);
		List<string> multiplayerOptionsList = MultiplayerOptions.Instance.GetMultiplayerOptionsList(base.OptionType);
		List<string> multiplayerOptionsTextList = MultiplayerOptions.Instance.GetMultiplayerOptionsTextList(base.OptionType);
		List<string> list = new List<string>();
		foreach (string item in multiplayerOptionsTextList)
		{
			list.Add(item);
		}
		int num = multiplayerOptionsList.IndexOf(MultiplayerOptions.Instance.GetValueTextForOptionWithMultipleSelection(base.OptionType));
		if (num != Selector.SelectedIndex)
		{
			Selector.SelectedIndex = num;
		}
		Selector.SetOnChangeAction(OnChangeSelected);
	}

	public void RefreshList()
	{
		List<string> multiplayerOptionsList = MultiplayerOptions.Instance.GetMultiplayerOptionsList(base.OptionType);
		List<string> multiplayerOptionsTextList = MultiplayerOptions.Instance.GetMultiplayerOptionsTextList(base.OptionType);
		List<string> list = new List<string>();
		foreach (string item in multiplayerOptionsTextList)
		{
			list.Add(item);
		}
		Selector.Refresh(list, multiplayerOptionsList.IndexOf(MultiplayerOptions.Instance.GetValueTextForOptionWithMultipleSelection(base.OptionType)), OnChangeSelected);
	}

	private void OnChangeSelected(SelectorVM<SelectorItemVM> selector)
	{
		if (selector.SelectedIndex >= 0 && selector.SelectedIndex < selector.ItemList.Count)
		{
			string value = MultiplayerOptions.Instance.GetMultiplayerOptionsList(base.OptionType)[selector.SelectedIndex];
			MultiplayerOptions.Instance.SetValueForOptionWithMultipleSelectionFromText(base.OptionType, value);
			OnChangedSelection?.Invoke(this);
		}
	}
}
