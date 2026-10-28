using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MPCultureItemVM : ViewModel
{
	private Action<MPCultureItemVM> _onSelection;

	private bool _isSelected;

	private string _cultureCode;

	private HintViewModel _hint;

	public BasicCultureObject Culture { get; private set; }

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
				OnPropertyChanged("IsSelected");
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
				OnPropertyChanged("CultureCode");
			}
		}
	}

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
				OnPropertyChanged("Hint");
			}
		}
	}

	public MPCultureItemVM(string cultureCode, Action<MPCultureItemVM> onSelection)
	{
		_onSelection = onSelection;
		CultureCode = cultureCode;
		Culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureCode);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Hint = new HintViewModel(Culture.Name);
	}

	private void ExecuteSelection()
	{
		_onSelection(this);
	}
}
