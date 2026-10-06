using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Selector;

public class SelectorItemVM : ViewModel
{
	private TextObject _s;

	private TextObject _hintObj;

	private string _stringItem;

	private HintViewModel _hint;

	private bool _canBeSelected = true;

	private bool _isSelected;

	[DataSourceProperty]
	public string StringItem
	{
		get
		{
			return _stringItem;
		}
		set
		{
			if (value != _stringItem)
			{
				_stringItem = value;
				OnPropertyChangedWithValue(value, "StringItem");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBeSelected
	{
		get
		{
			return _canBeSelected;
		}
		set
		{
			if (value != _canBeSelected)
			{
				_canBeSelected = value;
				OnPropertyChangedWithValue(value, "CanBeSelected");
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
				OnPropertyChangedWithValue(value, "Hint");
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
			}
		}
	}

	public SelectorItemVM(TextObject s)
	{
		_s = s;
		RefreshValues();
	}

	public SelectorItemVM(string s)
	{
		_stringItem = s;
		RefreshValues();
	}

	public SelectorItemVM(TextObject s, TextObject hint)
	{
		_s = s;
		_hintObj = hint;
		RefreshValues();
	}

	public SelectorItemVM(string s, TextObject hint)
	{
		_stringItem = s;
		_hintObj = hint;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_s != null)
		{
			_stringItem = _s.ToString();
		}
		if (_hintObj != null)
		{
			if (_hint == null)
			{
				_hint = new HintViewModel(_hintObj);
			}
			else
			{
				_hint.HintText = _hintObj;
			}
		}
	}
}
