using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class MultiplayerClassLoadoutItemTabControllerButtonWidget : ButtonWidget
{
	private readonly List<MultiplayerItemTabButtonWidget> _itemTabs;

	private float _targetPositionXOffset;

	private MultiplayerClassLoadoutItemTabListPanel _itemTabList;

	private Widget _cursorWidget;

	private float _animationSpeed;

	[DataSourceProperty]
	public MultiplayerClassLoadoutItemTabListPanel ItemTabList
	{
		get
		{
			return _itemTabList;
		}
		set
		{
			if (value != _itemTabList)
			{
				_itemTabList?.SelectEventHandlers.Remove(SelectedTabChanged);
				_itemTabList = value;
				_itemTabList?.SelectEventHandlers.Add(SelectedTabChanged);
				OnPropertyChanged(value, "ItemTabList");
			}
		}
	}

	[DataSourceProperty]
	public Widget CursorWidget
	{
		get
		{
			return _cursorWidget;
		}
		set
		{
			if (value != _cursorWidget)
			{
				_cursorWidget = value;
				OnPropertyChanged(value, "CursorWidget");
			}
		}
	}

	[DataSourceProperty]
	public float AnimationSpeed
	{
		get
		{
			return _animationSpeed;
		}
		set
		{
			if (value != _animationSpeed)
			{
				_animationSpeed = value;
				OnPropertyChanged(value, "AnimationSpeed");
			}
		}
	}

	public MultiplayerClassLoadoutItemTabControllerButtonWidget(UIContext context)
		: base(context)
	{
		_itemTabs = new List<MultiplayerItemTabButtonWidget>();
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		_itemTabs.Clear();
		for (int i = 0; i < ItemTabList.ChildCount; i++)
		{
			MultiplayerItemTabButtonWidget multiplayerItemTabButtonWidget = (MultiplayerItemTabButtonWidget)ItemTabList.GetChild(i);
			multiplayerItemTabButtonWidget.boolPropertyChanged += TabWidgetPropertyChanged;
			_itemTabs.Add(multiplayerItemTabButtonWidget);
		}
		ItemTabList.OnInitialized += ItemTabListInitialized;
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		for (int i = 0; i < _itemTabs.Count; i++)
		{
			_itemTabs[i].boolPropertyChanged -= TabWidgetPropertyChanged;
		}
		_itemTabs.Clear();
		ItemTabList.OnInitialized -= ItemTabListInitialized;
	}

	protected override void OnUpdate(float dt)
	{
		if (CursorWidget != null && !float.IsNaN(_targetPositionXOffset) && !(AnimationSpeed <= 0f) && !(MathF.Abs(CursorWidget.PositionXOffset - _targetPositionXOffset) <= 1E-05f))
		{
			int num = MathF.Sign(_targetPositionXOffset - CursorWidget.PositionXOffset);
			float amount = MathF.Min(AnimationSpeed * dt, 1f);
			CursorWidget.PositionXOffset = MathF.Lerp(CursorWidget.PositionXOffset, _targetPositionXOffset, amount);
			if ((num < 0 && CursorWidget.PositionXOffset < _targetPositionXOffset) || (num > 0 && CursorWidget.PositionXOffset > _targetPositionXOffset))
			{
				CursorWidget.PositionXOffset = _targetPositionXOffset;
			}
		}
	}

	private void TabWidgetPropertyChanged(PropertyOwnerObject sender, string propertyName, bool value)
	{
		if (propertyName == "IsSelected" && value)
		{
			SelectedTabChanged(null);
		}
	}

	private void ItemTabListInitialized()
	{
		SelectedTabChanged(null);
	}

	private void SelectedTabChanged(Widget widget)
	{
		if (CursorWidget == null || ItemTabList.IntValue < 0)
		{
			return;
		}
		int selectedIndex = -1;
		int num = 0;
		for (int i = 0; i < ItemTabList.ChildCount; i++)
		{
			ButtonWidget buttonWidget = (ButtonWidget)ItemTabList.GetChild(i);
			if (buttonWidget.IsVisible)
			{
				num++;
				if (buttonWidget.IsSelected)
				{
					selectedIndex = num - 1;
				}
			}
		}
		CalculateTargetPosition(selectedIndex, num);
	}

	private void CalculateTargetPosition(int selectedIndex, int activeTabCount)
	{
		float num = ItemTabList.Size.X / base._scaleToUse;
		float num2 = num / (float)activeTabCount;
		float num3 = (float)selectedIndex * num2 + num2 / 2f;
		_targetPositionXOffset = num3 - num / 2f;
	}
}
