using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialScreenWidget : Widget
{
	private bool _initalized;

	private TutorialHighlightItemBrushWidget _currentActiveHighligtFrame;

	private TutorialPanelImageWidget _currentActivePanelItem;

	public TutorialPanelImageWidget LeftItem { get; set; }

	public TutorialPanelImageWidget RightItem { get; set; }

	public TutorialPanelImageWidget BottomItem { get; set; }

	public TutorialPanelImageWidget TopItem { get; set; }

	public TutorialPanelImageWidget LeftTopItem { get; set; }

	public TutorialPanelImageWidget RightTopItem { get; set; }

	public TutorialPanelImageWidget LeftBottomItem { get; set; }

	public TutorialPanelImageWidget RightBottomItem { get; set; }

	public TutorialPanelImageWidget CenterItem { get; set; }

	public TutorialArrowWidget ArrowWidget { get; set; }

	public TutorialScreenWidget(UIContext context)
		: base(context)
	{
		EventManager.UIEventManager.RegisterEvent<TutorialHighlightItemBrushWidget.HighlightElementToggledEvent>(OnHighlightElementToggleEvent);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initalized)
		{
			LeftItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			RightItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			BottomItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			TopItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			LeftTopItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			RightTopItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			LeftBottomItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			RightBottomItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			CenterItem.boolPropertyChanged += OnTutorialItemPropertyChanged;
			_initalized = true;
		}
		if (_currentActiveHighligtFrame != null && _currentActivePanelItem != null)
		{
			Tuple<Widget, Widget> leftAndRightElements = GetLeftAndRightElements();
			Tuple<Widget, Widget> topAndBottomElements = GetTopAndBottomElements();
			float num = leftAndRightElements.Item1.GlobalPosition.X + leftAndRightElements.Item1.Size.X;
			float x = leftAndRightElements.Item2.GlobalPosition.X;
			float y = topAndBottomElements.Item1.GlobalPosition.Y;
			float y2 = topAndBottomElements.Item2.GlobalPosition.Y;
			float width = TaleWorlds.Library.MathF.Abs(num - x);
			float height = TaleWorlds.Library.MathF.Abs(y - y2);
			ArrowWidget.ScaledPositionXOffset = num;
			ArrowWidget.ScaledPositionYOffset = y;
			ArrowWidget.SetArrowProperties(width, height, GetIsArrowDirectionIsDownwards(), GetIsArrowDirectionIsTowardsRight());
			ArrowWidget.IsVisible = true;
		}
		else
		{
			ArrowWidget.IsVisible = false;
		}
	}

	private bool GetIsArrowDirectionIsDownwards()
	{
		if (_currentActiveHighligtFrame.GlobalPosition.Y < _currentActivePanelItem.GlobalPosition.Y)
		{
			return _currentActiveHighligtFrame.GlobalPosition.X < _currentActivePanelItem.GlobalPosition.X;
		}
		return _currentActivePanelItem.GlobalPosition.X < _currentActiveHighligtFrame.GlobalPosition.X;
	}

	private bool GetIsArrowDirectionIsTowardsRight()
	{
		return _currentActiveHighligtFrame.GlobalPosition.X > _currentActivePanelItem.GlobalPosition.X;
	}

	private Tuple<Widget, Widget> GetLeftAndRightElements()
	{
		if (_currentActiveHighligtFrame.GlobalPosition.X < _currentActivePanelItem.GlobalPosition.X)
		{
			return new Tuple<Widget, Widget>(_currentActiveHighligtFrame, _currentActivePanelItem);
		}
		return new Tuple<Widget, Widget>(_currentActivePanelItem, _currentActiveHighligtFrame);
	}

	private Tuple<Widget, Widget> GetTopAndBottomElements()
	{
		if (_currentActiveHighligtFrame.GlobalPosition.Y < _currentActivePanelItem.GlobalPosition.Y)
		{
			return new Tuple<Widget, Widget>(_currentActiveHighligtFrame, _currentActivePanelItem);
		}
		return new Tuple<Widget, Widget>(_currentActivePanelItem, _currentActiveHighligtFrame);
	}

	private void OnTutorialItemPropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsDisabled")
		{
			if (propertyValue)
			{
				_currentActivePanelItem = null;
				ArrowWidget.ResetFade();
			}
			else
			{
				_currentActivePanelItem = widget as TutorialPanelImageWidget;
				ArrowWidget.DisableFade();
			}
		}
	}

	private void OnHighlightElementToggleEvent(TutorialHighlightItemBrushWidget.HighlightElementToggledEvent obj)
	{
		if (obj.IsEnabled)
		{
			_currentActiveHighligtFrame = obj.HighlightFrameWidget;
			ArrowWidget.ResetFade();
		}
		else
		{
			ArrowWidget.DisableFade();
			_currentActiveHighligtFrame = null;
		}
	}
}
