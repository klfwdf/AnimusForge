using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.GamepadNavigation;

public class GamepadNavigationScope
{
	private class WidgetNavigationIndexComparer : IComparer<Widget>
	{
		public int Compare(Widget x, Widget y)
		{
			return x.GamepadNavigationIndex.CompareTo(y.GamepadNavigationIndex);
		}
	}

	private List<Widget> _navigatableWidgets;

	private Dictionary<Widget, int> _widgetIndices;

	private Widget _parentWidget;

	private float _extendChildrenCursorAreaLeft;

	private float _extendChildrenCursorAreaRight;

	private float _extendChildrenCursorAreaTop;

	private float _extendChildrenCursorAreaBottom;

	private bool _isEnabled;

	private bool _isDisabled;

	private WidgetNavigationIndexComparer _navigatableWidgetComparer;

	private List<Widget> _invisibleParents;

	private List<GamepadNavigationScope> _childScopes;

	internal Action<GamepadNavigationScope> OnNavigatableWidgetsChanged;

	internal Action<GamepadNavigationScope, bool> OnVisibilityChanged;

	internal Action<GamepadNavigationScope, GamepadNavigationScope> OnParentScopeChanged;

	public string ScopeID { get; set; } = "DefaultScopeID";

	public bool IsActiveScope { get; private set; }

	public bool DoNotAutomaticallyFindChildren { get; set; }

	public GamepadNavigationTypes ScopeMovements { get; set; }

	public GamepadNavigationTypes AlternateScopeMovements { get; set; }

	public int AlternateMovementStepSize { get; set; }

	public bool HasCircularMovement { get; set; }

	public ReadOnlyCollection<Widget> NavigatableWidgets { get; }

	public Widget ParentWidget
	{
		get
		{
			return _parentWidget;
		}
		set
		{
			if (value == _parentWidget)
			{
				return;
			}
			if (_parentWidget != null)
			{
				_invisibleParents.Clear();
				for (Widget parentWidget = _parentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
				{
					parentWidget.OnVisibilityChanged -= OnParentVisibilityChanged;
				}
			}
			_parentWidget = value;
			for (Widget parentWidget2 = _parentWidget; parentWidget2 != null; parentWidget2 = parentWidget2.ParentWidget)
			{
				if (!parentWidget2.IsVisible)
				{
					_invisibleParents.Add(parentWidget2);
				}
				parentWidget2.OnVisibilityChanged += OnParentVisibilityChanged;
			}
		}
	}

	public int LatestNavigationElementIndex { get; set; }

	public bool DoNotAutoGainNavigationOnInit { get; set; }

	public bool ForceGainNavigationBasedOnDirection { get; set; }

	public bool ForceGainNavigationOnClosestChild { get; set; }

	public bool ForceGainNavigationOnFirstChild { get; set; }

	public bool NavigateFromScopeEdges { get; set; }

	public bool UseDiscoveryAreaAsScopeEdges { get; set; }

	public bool DoNotAutoNavigateAfterSort { get; set; }

	public bool FollowMobileTargets { get; set; }

	public bool DoNotAutoCollectChildScopes { get; set; }

	public bool IsDefaultNavigationScope { get; set; }

	public float ExtendDiscoveryAreaRight { get; set; }

	public float ExtendDiscoveryAreaTop { get; set; }

	public float ExtendDiscoveryAreaBottom { get; set; }

	public float ExtendDiscoveryAreaLeft { get; set; }

	public float ExtendChildrenCursorAreaLeft
	{
		get
		{
			return _extendChildrenCursorAreaLeft;
		}
		set
		{
			if (value != _extendChildrenCursorAreaLeft)
			{
				_extendChildrenCursorAreaLeft = value;
				for (int i = 0; i < _navigatableWidgets.Count; i++)
				{
					_navigatableWidgets[i].ExtendCursorAreaLeft = value;
				}
			}
		}
	}

	public float ExtendChildrenCursorAreaRight
	{
		get
		{
			return _extendChildrenCursorAreaRight;
		}
		set
		{
			if (value != _extendChildrenCursorAreaRight)
			{
				_extendChildrenCursorAreaRight = value;
				for (int i = 0; i < _navigatableWidgets.Count; i++)
				{
					_navigatableWidgets[i].ExtendCursorAreaRight = value;
				}
			}
		}
	}

	public float ExtendChildrenCursorAreaTop
	{
		get
		{
			return _extendChildrenCursorAreaTop;
		}
		set
		{
			if (value != _extendChildrenCursorAreaTop)
			{
				_extendChildrenCursorAreaTop = value;
				for (int i = 0; i < _navigatableWidgets.Count; i++)
				{
					_navigatableWidgets[i].ExtendCursorAreaTop = value;
				}
			}
		}
	}

	public float ExtendChildrenCursorAreaBottom
	{
		get
		{
			return _extendChildrenCursorAreaBottom;
		}
		set
		{
			if (value != _extendChildrenCursorAreaBottom)
			{
				_extendChildrenCursorAreaBottom = value;
				for (int i = 0; i < _navigatableWidgets.Count; i++)
				{
					_navigatableWidgets[i].ExtendCursorAreaBottom = value;
				}
			}
		}
	}

	public float DiscoveryAreaOffsetX { get; set; }

	public float DiscoveryAreaOffsetY { get; set; }

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				IsDisabled = !value;
				OnNavigatableWidgetsChanged?.Invoke(this);
			}
		}
	}

	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				IsEnabled = !value;
			}
		}
	}

	public string UpNavigationScopeID
	{
		get
		{
			return ManualScopeIDs[GamepadNavigationTypes.Up];
		}
		set
		{
			ManualScopeIDs[GamepadNavigationTypes.Up] = value;
		}
	}

	public string RightNavigationScopeID
	{
		get
		{
			return ManualScopeIDs[GamepadNavigationTypes.Right];
		}
		set
		{
			ManualScopeIDs[GamepadNavigationTypes.Right] = value;
		}
	}

	public string DownNavigationScopeID
	{
		get
		{
			return ManualScopeIDs[GamepadNavigationTypes.Down];
		}
		set
		{
			ManualScopeIDs[GamepadNavigationTypes.Down] = value;
		}
	}

	public string LeftNavigationScopeID
	{
		get
		{
			return ManualScopeIDs[GamepadNavigationTypes.Left];
		}
		set
		{
			ManualScopeIDs[GamepadNavigationTypes.Left] = value;
		}
	}

	public GamepadNavigationScope UpNavigationScope
	{
		get
		{
			return ManualScopes[GamepadNavigationTypes.Up];
		}
		set
		{
			ManualScopes[GamepadNavigationTypes.Up] = value;
		}
	}

	public GamepadNavigationScope RightNavigationScope
	{
		get
		{
			return ManualScopes[GamepadNavigationTypes.Right];
		}
		set
		{
			ManualScopes[GamepadNavigationTypes.Right] = value;
		}
	}

	public GamepadNavigationScope DownNavigationScope
	{
		get
		{
			return ManualScopes[GamepadNavigationTypes.Down];
		}
		set
		{
			ManualScopes[GamepadNavigationTypes.Down] = value;
		}
	}

	public GamepadNavigationScope LeftNavigationScope
	{
		get
		{
			return ManualScopes[GamepadNavigationTypes.Left];
		}
		set
		{
			ManualScopes[GamepadNavigationTypes.Left] = value;
		}
	}

	internal Widget LastNavigatedWidget
	{
		get
		{
			if (LatestNavigationElementIndex >= 0 && LatestNavigationElementIndex < _navigatableWidgets.Count)
			{
				return _navigatableWidgets[LatestNavigationElementIndex];
			}
			return null;
		}
	}

	internal bool IsInitialized { get; private set; }

	internal GamepadNavigationScope PreviousScope { get; set; }

	internal Dictionary<GamepadNavigationTypes, string> ManualScopeIDs { get; private set; }

	internal Dictionary<GamepadNavigationTypes, GamepadNavigationScope> ManualScopes { get; private set; }

	internal bool IsAdditionalMovementsDirty { get; set; }

	internal Dictionary<GamepadNavigationTypes, GamepadNavigationScope> InterScopeMovements { get; private set; }

	internal GamepadNavigationScope ParentScope { get; private set; }

	internal ReadOnlyCollection<GamepadNavigationScope> ChildScopes { get; private set; }

	public GamepadNavigationScope()
	{
		_widgetIndices = new Dictionary<Widget, int>();
		_navigatableWidgets = new List<Widget>();
		NavigatableWidgets = new ReadOnlyCollection<Widget>(_navigatableWidgets);
		_invisibleParents = new List<Widget>();
		InterScopeMovements = new Dictionary<GamepadNavigationTypes, GamepadNavigationScope>
		{
			{
				GamepadNavigationTypes.Up,
				null
			},
			{
				GamepadNavigationTypes.Right,
				null
			},
			{
				GamepadNavigationTypes.Down,
				null
			},
			{
				GamepadNavigationTypes.Left,
				null
			}
		};
		ManualScopeIDs = new Dictionary<GamepadNavigationTypes, string>
		{
			{
				GamepadNavigationTypes.Up,
				null
			},
			{
				GamepadNavigationTypes.Right,
				null
			},
			{
				GamepadNavigationTypes.Down,
				null
			},
			{
				GamepadNavigationTypes.Left,
				null
			}
		};
		ManualScopes = new Dictionary<GamepadNavigationTypes, GamepadNavigationScope>
		{
			{
				GamepadNavigationTypes.Up,
				null
			},
			{
				GamepadNavigationTypes.Right,
				null
			},
			{
				GamepadNavigationTypes.Down,
				null
			},
			{
				GamepadNavigationTypes.Left,
				null
			}
		};
		_navigatableWidgetComparer = new WidgetNavigationIndexComparer();
		LatestNavigationElementIndex = -1;
		_childScopes = new List<GamepadNavigationScope>();
		ChildScopes = new ReadOnlyCollection<GamepadNavigationScope>(_childScopes);
		IsInitialized = false;
		IsEnabled = true;
	}

	public void AddWidgetAtIndex(Widget widget, int index)
	{
		if (index < _navigatableWidgets.Count)
		{
			_navigatableWidgets.Insert(index, widget);
			_widgetIndices.Add(widget, index);
		}
		else
		{
			_navigatableWidgets.Add(widget);
			_widgetIndices.Add(widget, _navigatableWidgets.Count - 1);
		}
		OnNavigatableWidgetsChanged?.Invoke(this);
		SetCursorAreaExtensionsForChild(widget);
	}

	public void AddWidget(Widget widget)
	{
		_navigatableWidgets.Add(widget);
		OnNavigatableWidgetsChanged?.Invoke(this);
		SetCursorAreaExtensionsForChild(widget);
	}

	public void RemoveWidget(Widget widget)
	{
		_navigatableWidgets.Remove(widget);
		OnNavigatableWidgetsChanged?.Invoke(this);
	}

	public void SetParentScope(GamepadNavigationScope scope)
	{
		if (ParentScope != null)
		{
			ParentScope._childScopes.Remove(this);
		}
		GamepadNavigationScope parentScope = ParentScope;
		ParentScope = scope;
		OnParentScopeChanged?.Invoke(parentScope, ParentScope);
		if (ParentScope != null)
		{
			ParentScope._childScopes.Add(this);
			ClearMyWidgetsFromParentScope();
		}
	}

	internal void SetIsActiveScope(bool isActive)
	{
		IsActiveScope = isActive;
	}

	internal bool IsVisible()
	{
		return _invisibleParents.Count == 0;
	}

	internal bool IsAvailable()
	{
		if (IsEnabled && _navigatableWidgets.Count > 0)
		{
			return IsVisible();
		}
		return false;
	}

	internal void Initialize()
	{
		if (!DoNotAutomaticallyFindChildren)
		{
			FindNavigatableChildren();
		}
		IsInitialized = true;
	}

	internal void RefreshNavigatableChildren()
	{
		if (IsInitialized)
		{
			FindNavigatableChildren();
		}
	}

	internal bool HasMovement(GamepadNavigationTypes movement)
	{
		if ((ScopeMovements & movement) == 0)
		{
			return (AlternateScopeMovements & movement) != 0;
		}
		return true;
	}

	private void FindNavigatableChildren()
	{
		_navigatableWidgets.Clear();
		if (IsParentWidgetAvailableForNavigation())
		{
			CollectNavigatableChildrenOfWidget(ParentWidget);
		}
		OnNavigatableWidgetsChanged?.Invoke(this);
	}

	private bool IsParentWidgetAvailableForNavigation()
	{
		for (Widget parentWidget = ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
		{
			if (parentWidget.DoNotAcceptNavigation)
			{
				return false;
			}
		}
		return true;
	}

	private void CollectNavigatableChildrenOfWidget(Widget widget)
	{
		if (widget.DoNotAcceptNavigation)
		{
			return;
		}
		for (int i = 0; i < _childScopes.Count; i++)
		{
			if (_childScopes[i].ParentWidget == widget)
			{
				return;
			}
		}
		if (widget.GamepadNavigationIndex != -1)
		{
			_navigatableWidgets.Add(widget);
		}
		if (!DoNotAutoCollectChildScopes && ParentWidget != widget && GauntletGamepadNavigationManager.Instance.NavigationScopeParents.TryGetValue(widget, out var value))
		{
			for (int j = 0; j < value.Count; j++)
			{
				value[j].SetParentScope(this);
			}
		}
		for (int k = 0; k < widget.Children.Count; k++)
		{
			CollectNavigatableChildrenOfWidget(widget.Children[k]);
		}
		ClearMyWidgetsFromParentScope();
	}

	internal GamepadNavigationTypes GetMovementsToReachMyPosition(Vector2 fromPosition)
	{
		SimpleRectangle rectangle = GetRectangle();
		GamepadNavigationTypes gamepadNavigationTypes = GamepadNavigationTypes.None;
		if (fromPosition.X > rectangle.X + rectangle.Width)
		{
			gamepadNavigationTypes |= GamepadNavigationTypes.Left;
		}
		else if (fromPosition.X < rectangle.X)
		{
			gamepadNavigationTypes |= GamepadNavigationTypes.Right;
		}
		if (fromPosition.Y > rectangle.Y + rectangle.Height)
		{
			gamepadNavigationTypes |= GamepadNavigationTypes.Up;
		}
		else if (fromPosition.Y < rectangle.Y)
		{
			gamepadNavigationTypes |= GamepadNavigationTypes.Down;
		}
		return gamepadNavigationTypes;
	}

	internal bool GetShouldFindScopeByPosition(GamepadNavigationTypes movement)
	{
		if (ManualScopeIDs[movement] == null)
		{
			return ManualScopes[movement] == null;
		}
		return false;
	}

	internal GamepadNavigationTypes GetMovementsInsideScope()
	{
		GamepadNavigationTypes gamepadNavigationTypes = ScopeMovements;
		GamepadNavigationTypes gamepadNavigationTypes2 = AlternateScopeMovements;
		if (!HasCircularMovement || _navigatableWidgets.Count == 1)
		{
			bool flag = false;
			bool flag2 = false;
			if (LatestNavigationElementIndex >= 0 && LatestNavigationElementIndex < _navigatableWidgets.Count)
			{
				for (int i = LatestNavigationElementIndex + 1; i < _navigatableWidgets.Count; i++)
				{
					if (IsWidgetVisible(_navigatableWidgets[i]))
					{
						flag2 = true;
						break;
					}
				}
				int num = LatestNavigationElementIndex - 1;
				if (HasCircularMovement && num < 0)
				{
					num = _navigatableWidgets.Count - 1;
				}
				for (int num2 = num; num2 >= 0; num2--)
				{
					if (IsWidgetVisible(_navigatableWidgets[num2]))
					{
						flag = true;
						break;
					}
				}
			}
			if (LatestNavigationElementIndex == 0 || !flag)
			{
				gamepadNavigationTypes &= ~GamepadNavigationTypes.Left;
				gamepadNavigationTypes &= ~GamepadNavigationTypes.Up;
			}
			if (LatestNavigationElementIndex == NavigatableWidgets.Count - 1 || !flag2)
			{
				gamepadNavigationTypes &= ~GamepadNavigationTypes.Right;
				gamepadNavigationTypes &= ~GamepadNavigationTypes.Down;
			}
			if (gamepadNavigationTypes2 != GamepadNavigationTypes.None && AlternateMovementStepSize > 0)
			{
				if (LatestNavigationElementIndex % AlternateMovementStepSize == 0)
				{
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Left;
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Up;
				}
				if (LatestNavigationElementIndex % AlternateMovementStepSize == AlternateMovementStepSize - 1)
				{
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Right;
					gamepadNavigationTypes &= ~GamepadNavigationTypes.Down;
				}
				if (LatestNavigationElementIndex - AlternateMovementStepSize < 0)
				{
					gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Up;
					gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Left;
				}
				int num3 = _navigatableWidgets.Count % AlternateMovementStepSize;
				if (_navigatableWidgets.Count > 0 && num3 == 0)
				{
					num3 = AlternateMovementStepSize;
				}
				if (LatestNavigationElementIndex + num3 > _navigatableWidgets.Count - 1)
				{
					gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Right;
					gamepadNavigationTypes2 &= ~GamepadNavigationTypes.Down;
				}
			}
		}
		return gamepadNavigationTypes | gamepadNavigationTypes2;
	}

	internal int FindIndexOfWidget(Widget widget)
	{
		if (widget != null && _navigatableWidgets.Count != 0 && _widgetIndices.TryGetValue(widget, out var value))
		{
			return value;
		}
		return -1;
	}

	internal void SortWidgets()
	{
		_navigatableWidgets.Sort(_navigatableWidgetComparer);
		_widgetIndices.Clear();
		for (int i = 0; i < _navigatableWidgets.Count; i++)
		{
			_widgetIndices[_navigatableWidgets[i]] = i;
		}
	}

	public void ClearNavigatableWidgets()
	{
		_navigatableWidgets.Clear();
		_widgetIndices.Clear();
	}

	internal SimpleRectangle GetDiscoveryRectangle()
	{
		float customScale = ParentWidget.EventManager.Context.CustomScale;
		return new SimpleRectangle(DiscoveryAreaOffsetX + ParentWidget.GlobalPosition.X - ExtendDiscoveryAreaLeft * customScale, DiscoveryAreaOffsetY + ParentWidget.GlobalPosition.Y - ExtendDiscoveryAreaTop * customScale, ParentWidget.Size.X + (ExtendDiscoveryAreaLeft + ExtendDiscoveryAreaRight) * customScale, ParentWidget.Size.Y + (ExtendDiscoveryAreaTop + ExtendDiscoveryAreaBottom) * customScale);
	}

	internal SimpleRectangle GetRectangle()
	{
		if (ParentWidget == null)
		{
			return new SimpleRectangle(0f, 0f, 1f, 1f);
		}
		return new SimpleRectangle(ParentWidget.GlobalPosition.X, ParentWidget.GlobalPosition.Y, ParentWidget.Size.X, ParentWidget.Size.Y);
	}

	internal bool IsWidgetVisible(Widget widget)
	{
		for (Widget widget2 = widget; widget2 != null; widget2 = widget2.ParentWidget)
		{
			if (!widget2.IsVisible)
			{
				return false;
			}
			if (widget2 == ParentWidget)
			{
				return IsVisible();
			}
		}
		return true;
	}

	internal Widget GetFirstAvailableWidget()
	{
		int num = -1;
		for (int i = 0; i < _navigatableWidgets.Count; i++)
		{
			if (IsWidgetVisible(_navigatableWidgets[i]))
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			return _navigatableWidgets[num];
		}
		return null;
	}

	internal Widget GetLastAvailableWidget()
	{
		int num = -1;
		for (int num2 = _navigatableWidgets.Count - 1; num2 >= 0; num2--)
		{
			if (IsWidgetVisible(_navigatableWidgets[num2]))
			{
				num = num2;
				break;
			}
		}
		if (num != -1)
		{
			return _navigatableWidgets[num];
		}
		return null;
	}

	private int GetApproximatelyClosestWidgetIndexToPosition(Vector2 position, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
	{
		if (_navigatableWidgets.Count > 0)
		{
			if (AlternateMovementStepSize > 0)
			{
				return GetClosesWidgetIndexForWithAlternateMovement(position, out distance, movement, angleCheck);
			}
			return GetClosesWidgetIndexForRegular(position, out distance, movement, angleCheck);
		}
		distance = -1f;
		return -1;
	}

	internal Widget GetApproximatelyClosestWidgetToPosition(Vector2 position, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
	{
		float distance;
		return GetApproximatelyClosestWidgetToPosition(position, out distance, movement, angleCheck);
	}

	internal Widget GetApproximatelyClosestWidgetToPosition(Vector2 position, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
	{
		int approximatelyClosestWidgetIndexToPosition = GetApproximatelyClosestWidgetIndexToPosition(position, out distance, movement, angleCheck);
		if (approximatelyClosestWidgetIndexToPosition != -1)
		{
			return _navigatableWidgets[approximatelyClosestWidgetIndexToPosition];
		}
		return null;
	}

	private void OnParentVisibilityChanged(Widget parent)
	{
		bool num = IsVisible();
		if (!parent.IsVisible)
		{
			_invisibleParents.Add(parent);
		}
		else
		{
			_invisibleParents.Remove(parent);
		}
		bool flag = IsVisible();
		if (num != flag)
		{
			OnVisibilityChanged?.Invoke(this, flag);
		}
	}

	private void ClearMyWidgetsFromParentScope()
	{
		if (ParentScope != null)
		{
			for (int i = 0; i < _navigatableWidgets.Count; i++)
			{
				ParentScope.RemoveWidget(_navigatableWidgets[i]);
			}
		}
	}

	private Vector2 GetRelativePositionRatio(Vector2 position)
	{
		float toValue = 0f;
		float fromValue = 0f;
		float toValue2 = 0f;
		float fromValue2 = 0f;
		for (int i = 0; i < _navigatableWidgets.Count; i++)
		{
			if (IsWidgetVisible(_navigatableWidgets[i]))
			{
				fromValue = _navigatableWidgets[i].GlobalPosition.Y;
				fromValue2 = _navigatableWidgets[i].GlobalPosition.X;
				break;
			}
		}
		for (int num = _navigatableWidgets.Count - 1; num >= 0; num--)
		{
			if (IsWidgetVisible(_navigatableWidgets[num]))
			{
				toValue = _navigatableWidgets[num].GlobalPosition.Y + _navigatableWidgets[num].Size.Y;
				toValue2 = _navigatableWidgets[num].GlobalPosition.X + _navigatableWidgets[num].Size.X;
				break;
			}
		}
		float x = Mathf.Clamp(InverseLerp(fromValue2, toValue2, position.X), 0f, 1f);
		float y = Mathf.Clamp(InverseLerp(fromValue, toValue, position.Y), 0f, 1f);
		return new Vector2(x, y);
	}

	private bool IsPositionAvailableForMovement(Vector2 fromPos, Vector2 toPos, GamepadNavigationTypes movement)
	{
		return movement switch
		{
			GamepadNavigationTypes.Right => fromPos.X <= toPos.X, 
			GamepadNavigationTypes.Left => fromPos.X >= toPos.X, 
			GamepadNavigationTypes.Up => fromPos.Y >= toPos.Y, 
			GamepadNavigationTypes.Down => fromPos.Y <= toPos.Y, 
			_ => true, 
		};
	}

	private int GetClosesWidgetIndexForWithAlternateMovement(Vector2 fromPos, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
	{
		distance = -1f;
		List<int> list = new List<int>();
		Vector2 relativePositionRatio = GetRelativePositionRatio(fromPos);
		float num = float.MaxValue;
		int result = -1;
		SimpleRectangle rectangle = GetRectangle();
		if (!rectangle.IsPointInside(fromPos))
		{
			List<int> list2 = new List<int>();
			if (fromPos.X < rectangle.X)
			{
				for (int i = 0; i < _navigatableWidgets.Count; i += AlternateMovementStepSize)
				{
					list2.Add(i);
				}
			}
			else if (fromPos.X > rectangle.X2)
			{
				for (int j = TaleWorlds.Library.MathF.Min(AlternateMovementStepSize - 1, _navigatableWidgets.Count - 1); j < _navigatableWidgets.Count; j += AlternateMovementStepSize)
				{
					list2.Add(j);
				}
			}
			if (list2.Count > 0)
			{
				int[] targetIndicesFromListByRatio = GetTargetIndicesFromListByRatio(relativePositionRatio.Y, list2);
				for (int k = 0; k < targetIndicesFromListByRatio.Length; k++)
				{
					list.Add(targetIndicesFromListByRatio[k]);
				}
			}
			if (fromPos.Y < rectangle.Y)
			{
				int endIndex = Mathf.Clamp(AlternateMovementStepSize - 1, 0, _navigatableWidgets.Count - 1);
				int[] targetIndicesByRatio = GetTargetIndicesByRatio(relativePositionRatio.X, 0, endIndex);
				for (int l = 0; l < targetIndicesByRatio.Length; l++)
				{
					list.Add(targetIndicesByRatio[l]);
				}
			}
			else if (fromPos.Y > rectangle.Y2)
			{
				int num2 = _navigatableWidgets.Count % AlternateMovementStepSize;
				if (_navigatableWidgets.Count > 0 && num2 == 0)
				{
					num2 = AlternateMovementStepSize;
				}
				int startIndex = Mathf.Clamp(_navigatableWidgets.Count - num2, 0, _navigatableWidgets.Count - 1);
				int[] targetIndicesByRatio2 = GetTargetIndicesByRatio(relativePositionRatio.X, startIndex, _navigatableWidgets.Count - 1);
				for (int m = 0; m < targetIndicesByRatio2.Length; m++)
				{
					list.Add(targetIndicesByRatio2[m]);
				}
			}
			for (int n = 0; n < list.Count; n++)
			{
				int num3 = list[n];
				Vector2 closestPointOnEdge;
				float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(_navigatableWidgets[num3], fromPos, movement, out closestPointOnEdge);
				if (distanceToClosestWidgetEdge < num && (!angleCheck || IsPositionAvailableForMovement(fromPos, closestPointOnEdge, movement)))
				{
					num = (distance = distanceToClosestWidgetEdge);
					result = num3;
				}
			}
		}
		else
		{
			result = GetClosesWidgetIndexForRegular(fromPos, out distance);
		}
		return result;
	}

	private int GetClosestWidgetIndexForRegularInefficient(Vector2 fromPos, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
	{
		distance = -1f;
		int result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < _navigatableWidgets.Count; i++)
		{
			Vector2 closestPointOnEdge;
			float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(_navigatableWidgets[i], fromPos, movement, out closestPointOnEdge);
			if (distanceToClosestWidgetEdge < num && IsWidgetVisible(_navigatableWidgets[i]) && (!angleCheck || IsPositionAvailableForMovement(fromPos, closestPointOnEdge, movement)))
			{
				num = (distance = distanceToClosestWidgetEdge);
				result = i;
			}
		}
		return result;
	}

	private int GetClosesWidgetIndexForRegular(Vector2 fromPos, out float distance, GamepadNavigationTypes movement = GamepadNavigationTypes.None, bool angleCheck = false)
	{
		distance = -1f;
		List<int> list = new List<int>();
		Vector2 relativePositionRatio = GetRelativePositionRatio(fromPos);
		int[] targetIndicesByRatio = GetTargetIndicesByRatio(relativePositionRatio.X, 0, _navigatableWidgets.Count - 1);
		int[] targetIndicesByRatio2 = GetTargetIndicesByRatio(relativePositionRatio.Y, 0, _navigatableWidgets.Count - 1);
		for (int i = 0; i < targetIndicesByRatio.Length; i++)
		{
			if (!list.Contains(targetIndicesByRatio[i]))
			{
				list.Add(targetIndicesByRatio[i]);
			}
		}
		for (int j = 0; j < targetIndicesByRatio2.Length; j++)
		{
			if (!list.Contains(targetIndicesByRatio2[j]))
			{
				list.Add(targetIndicesByRatio2[j]);
			}
		}
		float num = float.MaxValue;
		int result = -1;
		int num2 = 0;
		for (int k = 0; k < list.Count; k++)
		{
			int num3 = list[k];
			if (num3 != -1 && IsWidgetVisible(_navigatableWidgets[num3]))
			{
				num2++;
				Vector2 closestPointOnEdge;
				float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(_navigatableWidgets[num3], fromPos, movement, out closestPointOnEdge);
				if (distanceToClosestWidgetEdge < num && (!angleCheck || IsPositionAvailableForMovement(fromPos, closestPointOnEdge, movement)))
				{
					num = (distance = distanceToClosestWidgetEdge);
					result = num3;
				}
			}
		}
		if (num2 == 0)
		{
			return GetClosestWidgetIndexForRegularInefficient(fromPos, out distance);
		}
		return result;
	}

	private static float InverseLerp(float fromValue, float toValue, float value)
	{
		if (fromValue == toValue)
		{
			return 0f;
		}
		return (value - fromValue) / (toValue - fromValue);
	}

	private static int[] GetTargetIndicesFromListByRatio(float ratio, List<int> lookupIndices)
	{
		int num = TaleWorlds.Library.MathF.Round((float)lookupIndices.Count * ratio);
		return new int[5]
		{
			lookupIndices[Mathf.Clamp(num - 2, 0, lookupIndices.Count - 1)],
			lookupIndices[Mathf.Clamp(num - 1, 0, lookupIndices.Count - 1)],
			lookupIndices[Mathf.Clamp(num, 0, lookupIndices.Count - 1)],
			lookupIndices[Mathf.Clamp(num + 1, 0, lookupIndices.Count - 1)],
			lookupIndices[Mathf.Clamp(num + 2, 0, lookupIndices.Count - 1)]
		};
	}

	private static int[] GetTargetIndicesByRatio(float ratio, int startIndex, int endIndex, int arraySize = 5)
	{
		int num = TaleWorlds.Library.MathF.Round((float)startIndex + (float)(endIndex - startIndex) * ratio);
		int[] array = new int[arraySize];
		int num2 = TaleWorlds.Library.MathF.Floor((float)arraySize / 2f);
		for (int i = 0; i < arraySize; i++)
		{
			int num3 = -num2 + i;
			array[i] = Mathf.Clamp(num - num3, 0, endIndex);
		}
		return array;
	}

	private void SetCursorAreaExtensionsForChild(Widget child)
	{
		if (ExtendChildrenCursorAreaLeft != 0f)
		{
			child.ExtendCursorAreaLeft = ExtendChildrenCursorAreaLeft;
		}
		if (ExtendChildrenCursorAreaRight != 0f)
		{
			child.ExtendCursorAreaRight = ExtendChildrenCursorAreaRight;
		}
		if (ExtendChildrenCursorAreaTop != 0f)
		{
			child.ExtendCursorAreaTop = ExtendChildrenCursorAreaTop;
		}
		if (ExtendChildrenCursorAreaBottom != 0f)
		{
			child.ExtendCursorAreaBottom = ExtendChildrenCursorAreaBottom;
		}
	}
}
