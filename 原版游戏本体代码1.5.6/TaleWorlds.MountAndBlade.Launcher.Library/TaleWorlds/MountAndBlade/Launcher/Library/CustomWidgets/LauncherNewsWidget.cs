using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;

public class LauncherNewsWidget : Widget
{
	private int _currentShownNewsIndex;

	private float _currentNewsVisibleTime;

	private ButtonWidget _templateRadioButton;

	private bool _firstFrame = true;

	private bool _isRadioButtonVisibilityDirty;

	public float TimeToShowNewsItemInSeconds { get; set; } = 6.5f;

	public ListPanel RadioButtonContainer { get; set; }

	public Widget TimeLeftFillWidget { get; set; }

	public LauncherNewsWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_firstFrame)
		{
			_templateRadioButton = RadioButtonContainer.GetChild(0) as ButtonWidget;
			_templateRadioButton.ClickEventHandlers.Add(OnNewsRadioButtonClick);
			_templateRadioButton.IsVisible = false;
			_firstFrame = false;
		}
		if (base.ChildCount > 1)
		{
			_currentNewsVisibleTime += dt;
			if (_currentNewsVisibleTime >= TimeToShowNewsItemInSeconds)
			{
				int currentNewsItemIndex = (_currentShownNewsIndex + 1) % base.ChildCount;
				SetCurrentNewsItemIndex(currentNewsItemIndex);
			}
			TimeLeftFillWidget.SuggestedWidth = _currentNewsVisibleTime / TimeToShowNewsItemInSeconds * TimeLeftFillWidget.ParentWidget.Size.X * base._inverseScaleToUse;
		}
		else
		{
			_currentNewsVisibleTime = 0f;
			TimeLeftFillWidget.SuggestedWidth = 0f;
		}
		if (_isRadioButtonVisibilityDirty)
		{
			bool isVisible = base.ChildCount > 1;
			for (int i = 0; i < RadioButtonContainer.ChildCount; i++)
			{
				RadioButtonContainer.GetChild(i).IsVisible = isVisible;
			}
			_isRadioButtonVisibilityDirty = false;
		}
	}

	private void OnNewsRadioButtonClick(Widget obj)
	{
		int siblingIndex = obj.GetSiblingIndex();
		SetCurrentNewsItemIndex(siblingIndex);
	}

	private void SetCurrentNewsItemIndex(int indexOfNewsItem)
	{
		if (indexOfNewsItem != _currentShownNewsIndex)
		{
			(RadioButtonContainer.GetChild(_currentShownNewsIndex) as ButtonWidget).IsSelected = false;
			GetChild(_currentShownNewsIndex).IsVisible = false;
			_currentShownNewsIndex = indexOfNewsItem;
			(RadioButtonContainer.GetChild(_currentShownNewsIndex) as ButtonWidget).IsSelected = true;
			GetChild(_currentShownNewsIndex).IsVisible = true;
			GetChild(_currentShownNewsIndex).GetChild(0).GetChild(0).SetGlobalAlphaRecursively(0f);
			_currentNewsVisibleTime = 0f;
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		child.IsVisible = child.GetSiblingIndex() == _currentShownNewsIndex;
		if (RadioButtonContainer.ChildCount != base.ChildCount)
		{
			RadioButtonContainer.AddChild(GetDefaultNewsItemRadioButton());
		}
		(RadioButtonContainer.GetChild(_currentShownNewsIndex) as ButtonWidget).IsSelected = true;
		_isRadioButtonVisibilityDirty = true;
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		base.OnAfterChildRemoved(child, previousIndexOfChild);
		if (RadioButtonContainer.ChildCount != base.ChildCount)
		{
			RadioButtonContainer.RemoveChild(RadioButtonContainer.GetChild(RadioButtonContainer.ChildCount - 1));
		}
		if (_currentShownNewsIndex >= RadioButtonContainer.ChildCount && _currentShownNewsIndex > 0)
		{
			_currentShownNewsIndex--;
			(RadioButtonContainer.GetChild(_currentShownNewsIndex) as ButtonWidget).IsSelected = true;
			GetChild(_currentShownNewsIndex).IsVisible = child.GetSiblingIndex() == _currentShownNewsIndex;
		}
		_isRadioButtonVisibilityDirty = true;
	}

	private ButtonWidget GetDefaultNewsItemRadioButton()
	{
		return new ButtonWidget(base.Context)
		{
			WidthSizePolicy = _templateRadioButton.WidthSizePolicy,
			HeightSizePolicy = _templateRadioButton.HeightSizePolicy,
			Brush = _templateRadioButton.ReadOnlyBrush,
			SuggestedHeight = _templateRadioButton.SuggestedHeight,
			SuggestedWidth = _templateRadioButton.SuggestedWidth,
			ScaledSuggestedWidth = _templateRadioButton.ScaledSuggestedWidth,
			ScaledSuggestedHeight = _templateRadioButton.ScaledSuggestedHeight,
			MarginLeft = _templateRadioButton.MarginLeft,
			MarginRight = _templateRadioButton.MarginRight,
			MarginTop = _templateRadioButton.MarginTop,
			MarginBottom = _templateRadioButton.MarginBottom,
			ClickEventHandlers = { (Action<Widget>)OnNewsRadioButtonClick }
		};
	}
}
