using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class TabControlWidget : Widget
{
	private ButtonWidget _firstButton;

	private ButtonWidget _secondButton;

	private Widget _firstItem;

	private Widget _secondItem;

	[Editor(false)]
	public ButtonWidget FirstButton
	{
		get
		{
			return _firstButton;
		}
		set
		{
			if (_firstButton != value)
			{
				_firstButton = value;
				OnPropertyChanged(value, "FirstButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget SecondButton
	{
		get
		{
			return _secondButton;
		}
		set
		{
			if (_secondButton != value)
			{
				_secondButton = value;
				OnPropertyChanged(value, "SecondButton");
			}
		}
	}

	[Editor(false)]
	public Widget SecondItem
	{
		get
		{
			return _secondItem;
		}
		set
		{
			if (_secondItem != value)
			{
				_secondItem = value;
				OnPropertyChanged(value, "SecondItem");
			}
		}
	}

	[Editor(false)]
	public Widget FirstItem
	{
		get
		{
			return _firstItem;
		}
		set
		{
			if (_firstItem != value)
			{
				_firstItem = value;
				OnPropertyChanged(value, "FirstItem");
			}
		}
	}

	public TabControlWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!FirstButton.ClickEventHandlers.Contains(OnFirstButtonClick))
		{
			FirstButton.ClickEventHandlers.Add(OnFirstButtonClick);
		}
		if (!SecondButton.ClickEventHandlers.Contains(OnSecondButtonClick))
		{
			SecondButton.ClickEventHandlers.Add(OnSecondButtonClick);
		}
		FirstButton.IsSelected = FirstItem.IsVisible;
		SecondButton.IsSelected = SecondItem.IsVisible;
	}

	public void OnFirstButtonClick(Widget widget)
	{
		if (!_firstItem.IsVisible && _secondItem.IsVisible)
		{
			_secondItem.IsVisible = false;
			_firstItem.IsVisible = true;
		}
	}

	public void OnSecondButtonClick(Widget widget)
	{
		if (_firstItem.IsVisible && !_secondItem.IsVisible)
		{
			_secondItem.IsVisible = true;
			_firstItem.IsVisible = false;
		}
	}
}
