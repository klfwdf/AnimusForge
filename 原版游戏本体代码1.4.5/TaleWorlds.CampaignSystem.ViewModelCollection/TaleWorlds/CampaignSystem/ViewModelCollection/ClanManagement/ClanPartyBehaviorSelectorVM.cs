using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanPartyBehaviorSelectorVM : SelectorVM<SelectorItemVM>
{
	private bool _canUseActions;

	private HintViewModel _actionsDisabledHint;

	[DataSourceProperty]
	public bool CanUseActions
	{
		get
		{
			return _canUseActions;
		}
		set
		{
			if (value != _canUseActions)
			{
				_canUseActions = value;
				OnPropertyChangedWithValue(value, "CanUseActions");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ActionsDisabledHint
	{
		get
		{
			return _actionsDisabledHint;
		}
		set
		{
			if (value != _actionsDisabledHint)
			{
				_actionsDisabledHint = value;
				OnPropertyChangedWithValue(value, "ActionsDisabledHint");
			}
		}
	}

	public ClanPartyBehaviorSelectorVM(int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
		: base(selectedIndex, onChange)
	{
		ActionsDisabledHint = new HintViewModel();
	}
}
