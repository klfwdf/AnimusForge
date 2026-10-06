using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle.SelectionItem;

public class FactionItemVM : ViewModel
{
	private Action<FactionItemVM> _onSelected;

	private HintViewModel _hint;

	private string _cultureCode;

	private bool _isSelected;

	public BasicCultureObject Faction { get; private set; }

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public string CultureCode
	{
		get
		{
			return _cultureCode;
		}
		set
		{
			if (value != _cultureCode)
			{
				_cultureCode = value;
				OnPropertyChangedWithValue(value, "CultureCode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
				if (value)
				{
					_onSelected(this);
				}
			}
		}
	}

	public FactionItemVM(BasicCultureObject faction, Action<FactionItemVM> onSelected)
	{
		Faction = faction;
		_onSelected = onSelected;
		CultureCode = faction.StringId.ToLower();
		Hint = new HintViewModel(faction.Name);
	}
}
