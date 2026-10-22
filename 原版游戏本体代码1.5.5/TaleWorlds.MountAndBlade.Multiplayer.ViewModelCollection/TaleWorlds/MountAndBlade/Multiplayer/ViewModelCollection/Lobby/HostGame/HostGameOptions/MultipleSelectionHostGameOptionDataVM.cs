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
		Selector = new SelectorVM<SelectorItemVM>(GetOptionTexts(), GetCurrentIndex(), null);
		Selector.SetOnChangeAction(OnChangeSelected);
	}

	public override void RefreshData()
	{
		Selector.SetOnChangeAction(null);
		int currentIndex = GetCurrentIndex();
		if (currentIndex != Selector.SelectedIndex)
		{
			Selector.SelectedIndex = currentIndex;
		}
		Selector.SetOnChangeAction(OnChangeSelected);
	}

	public void RefreshList()
	{
		Selector.SetOnChangeAction(null);
		Selector.Refresh(GetOptionTexts(), GetCurrentIndex(), null);
		Selector.SetOnChangeAction(OnChangeSelected);
	}

	private List<string> GetOptionTexts()
	{
		return new List<string>(MultiplayerOptions.Instance.GetMultiplayerOptionsTextList(base.OptionType));
	}

	private int GetCurrentIndex()
	{
		return MultiplayerOptions.Instance.GetMultiplayerOptionsList(base.OptionType).IndexOf(MultiplayerOptions.Instance.GetValueTextForOptionWithMultipleSelection(base.OptionType));
	}

	private void OnChangeSelected(SelectorVM<SelectorItemVM> selector)
	{
		string value = MultiplayerOptions.Instance.GetMultiplayerOptionsList(base.OptionType)[selector.SelectedIndex];
		MultiplayerOptions.Instance.SetValueForOptionWithMultipleSelectionFromText(base.OptionType, value);
		OnChangedSelection?.Invoke(this);
		NotifyValueChanged();
	}
}
