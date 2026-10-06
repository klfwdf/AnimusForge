using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class DoubleTabControlListPanel : ListPanel
{
	private ButtonWidget _firstListButton;

	private ButtonWidget _secondListButton;

	private Widget _firstList;

	private Widget _secondList;

	[Editor(false)]
	public ButtonWidget FirstListButton
	{
		get
		{
			return _firstListButton;
		}
		set
		{
			if (_firstListButton != value)
			{
				_firstListButton = value;
				OnPropertyChanged(value, "FirstListButton");
				if (FirstListButton != null && !FirstListButton.ClickEventHandlers.Contains(OnFirstTabClick))
				{
					FirstListButton.ClickEventHandlers.Add(OnFirstTabClick);
				}
			}
		}
	}

	[Editor(false)]
	public ButtonWidget SecondListButton
	{
		get
		{
			return _secondListButton;
		}
		set
		{
			if (_secondListButton != value)
			{
				_secondListButton = value;
				OnPropertyChanged(value, "SecondListButton");
				if (SecondListButton != null && !SecondListButton.ClickEventHandlers.Contains(OnSecondTabClick))
				{
					SecondListButton.ClickEventHandlers.Add(OnSecondTabClick);
				}
			}
		}
	}

	[Editor(false)]
	public Widget FirstList
	{
		get
		{
			return _firstList;
		}
		set
		{
			if (_firstList != value)
			{
				_firstList = value;
				OnPropertyChanged(value, "FirstList");
			}
		}
	}

	[Editor(false)]
	public Widget SecondList
	{
		get
		{
			return _secondList;
		}
		set
		{
			if (_secondList != value)
			{
				_secondList = value;
				OnPropertyChanged(value, "SecondList");
			}
		}
	}

	public DoubleTabControlListPanel(UIContext context)
		: base(context)
	{
	}

	public void OnFirstTabClick(Widget widget)
	{
		if (!_firstList.IsVisible && _secondList.IsVisible)
		{
			_secondList.IsVisible = false;
			_firstList.IsVisible = true;
		}
	}

	public void OnSecondTabClick(Widget widget)
	{
		if (_firstList.IsVisible && !_secondList.IsVisible)
		{
			_secondList.IsVisible = true;
			_firstList.IsVisible = false;
		}
	}
}
