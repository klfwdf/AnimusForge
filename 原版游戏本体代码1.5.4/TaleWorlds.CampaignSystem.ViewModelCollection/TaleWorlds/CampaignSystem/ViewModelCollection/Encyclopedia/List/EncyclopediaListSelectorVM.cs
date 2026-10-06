using System;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaListSelectorVM : SelectorVM<EncyclopediaListSelectorItemVM>
{
	private Action _onActivate;

	public EncyclopediaListSelectorVM(int selectedIndex, Action<SelectorVM<EncyclopediaListSelectorItemVM>> onChange, Action onActivate)
		: base(selectedIndex, onChange)
	{
		_onActivate = onActivate;
	}

	public void ExecuteOnDropdownActivated()
	{
		_onActivate?.Invoke();
	}
}
