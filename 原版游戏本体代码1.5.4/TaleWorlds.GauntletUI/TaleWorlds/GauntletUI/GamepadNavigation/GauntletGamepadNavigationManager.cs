using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.GamepadNavigation;

public class GauntletGamepadNavigationManager
{
	private class GamepadNavigationContextComparer : IComparer<IGamepadNavigationContext>
	{
		public int Compare(IGamepadNavigationContext x, IGamepadNavigationContext y)
		{
			int lastScreenOrder = x.GetLastScreenOrder();
			int lastScreenOrder2 = y.GetLastScreenOrder();
			return -lastScreenOrder.CompareTo(lastScreenOrder2);
		}
	}

	private class ForcedScopeComparer : IComparer<GamepadNavigationForcedScopeCollection>
	{
		public int Compare(GamepadNavigationForcedScopeCollection x, GamepadNavigationForcedScopeCollection y)
		{
			return x.CollectionOrder.CompareTo(y.CollectionOrder);
		}
	}

	private class ContextGamepadNavigationGainHandler
	{
		private readonly IGamepadNavigationContext _context;

		private float _gainAfterTime;

		private float _gainTimer;

		private int _gainAfterFrames;

		private int _frameTicker;

		private Func<bool> _gainPredicate;

		public ContextGamepadNavigationGainHandler(IGamepadNavigationContext eventManager)
		{
			_context = eventManager;
			Clear();
		}

		public void GainNavigationAfterFrames(int frameCount, Func<bool> predicate = null)
		{
			Clear();
			if (frameCount >= 0)
			{
				_gainAfterFrames = frameCount;
				_gainPredicate = predicate;
			}
		}

		public void GainNavigationAfterTime(float seconds, Func<bool> predicate = null)
		{
			Clear();
			if (seconds >= 0f)
			{
				_gainAfterTime = seconds;
				_gainPredicate = predicate;
			}
		}

		public void Tick(float dt)
		{
			if (_gainAfterTime != -1f)
			{
				_gainTimer += dt;
				if (_gainTimer > _gainAfterTime)
				{
					Func<bool> gainPredicate = _gainPredicate;
					if (gainPredicate == null || gainPredicate())
					{
						_context.OnGainNavigation();
					}
					Clear();
				}
			}
			else
			{
				if (_gainAfterFrames == -1)
				{
					return;
				}
				_frameTicker++;
				if (_frameTicker > _gainAfterFrames)
				{
					Func<bool> gainPredicate2 = _gainPredicate;
					if (gainPredicate2 == null || gainPredicate2())
					{
						_context.OnGainNavigation();
					}
					Clear();
				}
			}
		}

		public void Clear()
		{
			_gainAfterTime = -1f;
			_gainAfterFrames = -1;
			_frameTicker = 0;
			_gainTimer = 0f;
			_gainPredicate = null;
		}
	}

	private IGamepadNavigationContext _latestCachedContext;

	private float _time;

	private bool _stopCursorNextFrame;

	private bool _isForcedCollectionsDirty;

	private GamepadNavigationContextComparer _cachedNavigationContextComparer;

	private ForcedScopeComparer _cachedForcedScopeComparer;

	private List<IGamepadNavigationContext> _sortedNavigationContexts;

	private Dictionary<IGamepadNavigationContext, GamepadNavigationScopeCollection> _navigationScopes;

	private List<GamepadNavigationScope> _availableScopesThisFrame;

	private List<GamepadNavigationScope> _unsortedScopes;

	private List<GamepadNavigationForcedScopeCollection> _forcedScopeCollections;

	private GamepadNavigationForcedScopeCollection _activeForcedScopeCollection;

	private GamepadNavigationScope _nextScopeToGainNavigation;

	private GamepadNavigationScope _activeNavigationScope;

	private Dictionary<Widget, List<GamepadNavigationScope>> _navigationScopeParents;

	private Dictionary<Widget, List<GamepadNavigationForcedScopeCollection>> _forcedNavigationScopeCollectionParents;

	private Dictionary<string, List<GamepadNavigationScope>> _layerNavigationScopes;

	private Dictionary<string, List<GamepadNavigationScope>> _navigationScopesById;

	private Dictionary<IGamepadNavigationContext, ContextGamepadNavigationGainHandler> _navigationGainControllers;

	private float _navigationHoldTimer;

	private Vector2 _lastNavigatedWidgetPosition;

	private readonly float _mouseCursorMoveTime = 0.09f;

	private Vector2 _cursorMoveStartPosition = new Vector2(float.NaN, float.NaN);

	private float _cursorMoveStartTime = -1f;

	private Widget _latestGamepadNavigationWidget;

	private List<Widget> _navigationBlockingWidgets;

	private bool _isAvailableScopesDirty;

	private bool _shouldUpdateAvailableScopes;

	private float _autoRefreshTimer;

	private bool _wasCursorInsideActiveScopeLastFrame;

	public static GauntletGamepadNavigationManager Instance { get; private set; }

	private IGamepadNavigationContext LatestContext
	{
		get
		{
			if (_latestCachedContext == null)
			{
				for (int i = 0; i < _sortedNavigationContexts.Count; i++)
				{
					if (_sortedNavigationContexts[i].IsAvailableForNavigation())
					{
						_latestCachedContext = _sortedNavigationContexts[i];
						break;
					}
				}
			}
			return _latestCachedContext;
		}
	}

	public bool IsTouchpadMouseEnabled { get; set; }

	public bool IsFollowingMobileTarget { get; private set; }

	public bool IsHoldingDpadKeysForNavigation { get; private set; }

	public bool IsCursorMovingForNavigation { get; private set; }

	public bool IsInWrapMovement { get; private set; }

	private Vector2 MousePosition
	{
		get
		{
			return (Vector2)Input.InputState.MousePositionPixel;
		}
		set
		{
			Input.SetMousePosition((int)value.X, (int)value.Y);
		}
	}

	private bool IsControllerActive => Input.IsGamepadActive;

	internal ReadOnlyDictionary<IGamepadNavigationContext, GamepadNavigationScopeCollection> NavigationScopes { get; private set; }

	internal ReadOnlyDictionary<Widget, List<GamepadNavigationScope>> NavigationScopeParents { get; private set; }

	internal ReadOnlyDictionary<Widget, List<GamepadNavigationForcedScopeCollection>> ForcedNavigationScopeParents { get; private set; }

	public Widget LastTargetedWidget
	{
		get
		{
			Widget widget = _activeNavigationScope?.LastNavigatedWidget;
			if (widget != null && (IsCursorMovingForNavigation || widget.IsPointInsideGamepadCursorArea(MousePosition)))
			{
				return widget;
			}
			return null;
		}
	}

	public bool TargetedWidgetHasAction
	{
		get
		{
			if (LastTargetedWidget != null)
			{
				if (LastTargetedWidget.UsedNavigationMovements != GamepadNavigationTypes.None)
				{
					return true;
				}
				List<Widget> allParents = LastTargetedWidget.GetAllParents();
				for (int i = 0; i < allParents.Count; i++)
				{
					if (allParents[i].UsedNavigationMovements != GamepadNavigationTypes.None)
					{
						return true;
					}
				}
				if (LastTargetedWidget.GetFirstInChildrenAndThisRecursive((Widget child) => child.UsedNavigationMovements != GamepadNavigationTypes.None) != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool AnyWidgetUsingNavigation => _navigationBlockingWidgets.Any((Widget x) => x.IsUsingNavigation);

	private GauntletGamepadNavigationManager()
	{
		_cachedNavigationContextComparer = new GamepadNavigationContextComparer();
		_cachedForcedScopeComparer = new ForcedScopeComparer();
		_navigationScopes = new Dictionary<IGamepadNavigationContext, GamepadNavigationScopeCollection>();
		NavigationScopes = new ReadOnlyDictionary<IGamepadNavigationContext, GamepadNavigationScopeCollection>(_navigationScopes);
		_navigationScopeParents = new Dictionary<Widget, List<GamepadNavigationScope>>();
		_forcedNavigationScopeCollectionParents = new Dictionary<Widget, List<GamepadNavigationForcedScopeCollection>>();
		NavigationScopeParents = new ReadOnlyDictionary<Widget, List<GamepadNavigationScope>>(_navigationScopeParents);
		ForcedNavigationScopeParents = new ReadOnlyDictionary<Widget, List<GamepadNavigationForcedScopeCollection>>(_forcedNavigationScopeCollectionParents);
		_sortedNavigationContexts = new List<IGamepadNavigationContext>();
		_availableScopesThisFrame = new List<GamepadNavigationScope>();
		_unsortedScopes = new List<GamepadNavigationScope>();
		_forcedScopeCollections = new List<GamepadNavigationForcedScopeCollection>();
		_layerNavigationScopes = new Dictionary<string, List<GamepadNavigationScope>>();
		_navigationScopesById = new Dictionary<string, List<GamepadNavigationScope>>();
		_navigationGainControllers = new Dictionary<IGamepadNavigationContext, ContextGamepadNavigationGainHandler>();
		_navigationBlockingWidgets = new List<Widget>();
		_isAvailableScopesDirty = false;
		_isForcedCollectionsDirty = false;
		Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	private void OnGamepadActiveStateChanged()
	{
		if (IsControllerActive && Input.MouseMoveX == 0f && Input.MouseMoveY == 0f)
		{
			_isAvailableScopesDirty = true;
			_isForcedCollectionsDirty = true;
		}
	}

	public static void Initialize()
	{
		if (Instance != null)
		{
			Debug.FailedAssert("Gamepad Navigation already initialized", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "Initialize", 224);
		}
		else
		{
			Instance = new GauntletGamepadNavigationManager();
		}
	}

	public bool TryNavigateTo(Widget widget)
	{
		if (widget != null && widget.GamepadNavigationIndex != -1 && _navigationScopes.TryGetValue(widget.GamepadNavigationContext, out var value))
		{
			for (int i = 0; i < value.VisibleScopes.Count; i++)
			{
				GamepadNavigationScope gamepadNavigationScope = value.VisibleScopes[i];
				if (gamepadNavigationScope.IsAvailable() && (gamepadNavigationScope.ParentWidget == widget || gamepadNavigationScope.ParentWidget.CheckIsMyChildRecursive(widget)))
				{
					return SetCurrentNavigatedWidget(gamepadNavigationScope, widget);
				}
			}
		}
		return false;
	}

	public bool TryNavigateTo(GamepadNavigationScope scope)
	{
		if (scope != null && scope.IsAvailable())
		{
			Widget approximatelyClosestWidgetToPosition = scope.GetApproximatelyClosestWidgetToPosition(MousePosition);
			if (approximatelyClosestWidgetToPosition != null)
			{
				return SetCurrentNavigatedWidget(scope, approximatelyClosestWidgetToPosition);
			}
		}
		return false;
	}

	public void OnFinalize()
	{
		foreach (KeyValuePair<IGamepadNavigationContext, GamepadNavigationScopeCollection> navigationScope in _navigationScopes)
		{
			navigationScope.Value.OnFinalize();
		}
		_navigationScopes.Clear();
		_navigationScopeParents.Clear();
		Instance = null;
		Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	public void Update(float dt)
	{
		_time += dt;
		if (_stopCursorNextFrame)
		{
			IsCursorMovingForNavigation = false;
			_stopCursorNextFrame = false;
		}
		if (IsControllerActive && !(Input.MouseMoveX > 0f) && !(Input.MouseMoveY > 0f))
		{
			GamepadNavigationScope activeNavigationScope = _activeNavigationScope;
			if (activeNavigationScope != null && activeNavigationScope.IsAvailable() && _activeNavigationScope.ParentWidget.Context.GamepadNavigation.IsAvailableForNavigation() && (!Input.IsAnyTouchActive || !IsTouchpadMouseEnabled))
			{
				goto IL_008c;
			}
		}
		OnDpadNavigationStopped();
		goto IL_008c;
		IL_008c:
		foreach (KeyValuePair<IGamepadNavigationContext, ContextGamepadNavigationGainHandler> navigationGainController in _navigationGainControllers)
		{
			navigationGainController.Value.Tick(dt);
		}
		if (LastTargetedWidget != null)
		{
			Vector2.Distance(LastTargetedWidget.AreaRect.GetCenter(), MousePosition);
		}
		if (Input.GetKeyState(InputKey.ControllerRStick).X != 0f)
		{
			_ = 1;
		}
		else
			_ = Input.GetKeyState(InputKey.ControllerRStick).Y != 0f;
		if (_autoRefreshTimer > -1f)
		{
			_autoRefreshTimer += dt;
			if (_autoRefreshTimer > 0.6f)
			{
				_autoRefreshTimer = -1f;
				_isAvailableScopesDirty = true;
			}
		}
		if (!_isAvailableScopesDirty)
		{
			GamepadNavigationScope activeNavigationScope2 = _activeNavigationScope;
			if (activeNavigationScope2 == null || !activeNavigationScope2.IsAvailable())
			{
				_isAvailableScopesDirty = true;
			}
		}
		_sortedNavigationContexts.Clear();
		foreach (KeyValuePair<IGamepadNavigationContext, GamepadNavigationScopeCollection> navigationScope in _navigationScopes)
		{
			_sortedNavigationContexts.Add(navigationScope.Key);
			navigationScope.Value.HandleScopeVisibilities();
		}
		_sortedNavigationContexts.Sort(_cachedNavigationContextComparer);
		foreach (KeyValuePair<IGamepadNavigationContext, GamepadNavigationScopeCollection> navigationScope2 in _navigationScopes)
		{
			if (navigationScope2.Value.UninitializedScopes.Count > 0)
			{
				List<GamepadNavigationScope> list = navigationScope2.Value.UninitializedScopes.ToList();
				for (int i = 0; i < list.Count; i++)
				{
					InitializeScope(navigationScope2.Key, list[i]);
				}
			}
		}
		if (_unsortedScopes.Count > 0)
		{
			bool flag = false;
			for (int j = 0; j < _unsortedScopes.Count; j++)
			{
				if (_unsortedScopes[j] == _activeNavigationScope)
				{
					flag = true;
				}
				_unsortedScopes[j].SortWidgets();
			}
			_unsortedScopes.Clear();
			if (flag && !_activeNavigationScope.DoNotAutoNavigateAfterSort && _activeNavigationScope != null && _activeNavigationScope.IsAvailable() && (_wasCursorInsideActiveScopeLastFrame || _activeNavigationScope.GetRectangle().IsPointInside(MousePosition)))
			{
				if (_activeNavigationScope.ForceGainNavigationOnClosestChild)
				{
					MoveCursorToClosestAvailableWidgetInScope(_activeNavigationScope);
				}
				else
				{
					MoveCursorToFirstAvailableWidgetInScope(_activeNavigationScope);
				}
			}
		}
		if (_activeForcedScopeCollection != null && !_activeForcedScopeCollection.IsAvailable())
		{
			_isAvailableScopesDirty = true;
			_isForcedCollectionsDirty = true;
		}
		if (_shouldUpdateAvailableScopes)
		{
			GamepadNavigationForcedScopeCollection activeForcedScopeCollection = _activeForcedScopeCollection;
			_activeForcedScopeCollection = FindAvailableForcedScope();
			if (_activeForcedScopeCollection != null && activeForcedScopeCollection == null)
			{
				_activeForcedScopeCollection.PreviousScope = _activeNavigationScope;
			}
			RefreshAvailableScopes();
			_shouldUpdateAvailableScopes = false;
			if (activeForcedScopeCollection != null && !activeForcedScopeCollection.IsAvailable())
			{
				TryMoveCursorToPreviousScope(activeForcedScopeCollection);
			}
			else if (_nextScopeToGainNavigation != null)
			{
				MoveCursorToFirstAvailableWidgetInScope(_nextScopeToGainNavigation);
				_nextScopeToGainNavigation = null;
			}
			else
			{
				GamepadNavigationScope activeNavigationScope3 = _activeNavigationScope;
				if (activeNavigationScope3 == null || !activeNavigationScope3.IsAvailable() || !_availableScopesThisFrame.Contains(_activeNavigationScope))
				{
					MoveCursorToBestAvailableScope(isFromInput: false);
				}
			}
		}
		if (_isAvailableScopesDirty)
		{
			_shouldUpdateAvailableScopes = true;
			_isAvailableScopesDirty = false;
		}
		HandleInput(dt);
		HandleCursorMovement();
		_wasCursorInsideActiveScopeLastFrame = _activeNavigationScope?.GetRectangle().IsPointInside(MousePosition) ?? false;
	}

	internal void OnMovieLoaded(IGamepadNavigationContext context, string movieName)
	{
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			List<GamepadNavigationScope> list = value.UninitializedScopes.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].DoNotAutomaticallyFindChildren)
				{
					InitializeScope(context, list[i]);
				}
				AddItemToDictionaryList(_layerNavigationScopes, movieName, list[i]);
			}
		}
		_autoRefreshTimer = 0f;
		_isAvailableScopesDirty = true;
		_latestCachedContext = null;
	}

	internal void OnMovieReleased(IGamepadNavigationContext context, string movieName)
	{
		if (_layerNavigationScopes.TryGetValue(movieName, out var value))
		{
			List<GamepadNavigationScope> list = value.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				RemoveItemFromDictionaryList(_layerNavigationScopes, movieName, list[i]);
				RemoveNavigationScope(context, list[i]);
			}
			_latestCachedContext = null;
		}
		_autoRefreshTimer = 0f;
		_isAvailableScopesDirty = true;
	}

	internal void OnContextAdded(IGamepadNavigationContext context)
	{
		_navigationScopes.Add(context, new GamepadNavigationScopeCollection(context, OnScopeNavigatableWidgetsChanged, OnScopeVisibilityChanged));
		_navigationGainControllers.Add(context, new ContextGamepadNavigationGainHandler(context));
		_latestCachedContext = null;
	}

	private void OnContextRemoved(IGamepadNavigationContext context)
	{
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			value.OnFinalize();
			_navigationScopes.Remove(context);
		}
		if (_navigationGainControllers.TryGetValue(context, out var value2))
		{
			value2.Clear();
			_navigationGainControllers.Remove(context);
		}
		_sortedNavigationContexts.Remove(context);
		_latestCachedContext = null;
	}

	internal void OnContextFinalized(IGamepadNavigationContext context)
	{
		int count = _sortedNavigationContexts.Count;
		OnContextRemoved(context);
		List<GamepadNavigationForcedScopeCollection> list = new List<GamepadNavigationForcedScopeCollection>();
		foreach (KeyValuePair<Widget, List<GamepadNavigationForcedScopeCollection>> forcedNavigationScopeCollectionParent in _forcedNavigationScopeCollectionParents)
		{
			if (forcedNavigationScopeCollectionParent.Key.GamepadNavigationContext == context)
			{
				list.AddRange(forcedNavigationScopeCollectionParent.Value);
			}
		}
		foreach (GamepadNavigationForcedScopeCollection item in list)
		{
			RemoveForcedScopeCollection(item);
		}
		if (count != _sortedNavigationContexts.Count)
		{
			_sortedNavigationContexts = _navigationScopes.Keys.ToList();
			_sortedNavigationContexts.Sort(_cachedNavigationContextComparer);
		}
		_isAvailableScopesDirty = true;
	}

	private Vector2 GetTargetCursorPosition()
	{
		if (_latestGamepadNavigationWidget != null)
		{
			return _latestGamepadNavigationWidget.GamepadCursorAreaRect.GetCenter();
		}
		return (Vector2)Vec2.Invalid;
	}

	private void RefreshAvailableScopes()
	{
		_availableScopesThisFrame.Clear();
		if (_activeForcedScopeCollection != null)
		{
			for (int i = 0; i < _activeForcedScopeCollection.Scopes.Count; i++)
			{
				_availableScopesThisFrame.Add(_activeForcedScopeCollection.Scopes[i]);
			}
			return;
		}
		for (int j = 0; j < _sortedNavigationContexts.Count; j++)
		{
			IGamepadNavigationContext gamepadNavigationContext = _sortedNavigationContexts[j];
			if (!gamepadNavigationContext.IsAvailableForNavigation())
			{
				continue;
			}
			for (int k = 0; k < _navigationScopes[gamepadNavigationContext].VisibleScopes.Count; k++)
			{
				GamepadNavigationScope gamepadNavigationScope = _navigationScopes[gamepadNavigationContext].VisibleScopes[k];
				if (gamepadNavigationScope.IsAvailable())
				{
					Vector2 center = gamepadNavigationScope.ParentWidget.AreaRect.GetCenter();
					if (!gamepadNavigationContext.GetIsBlockedAtPosition(center))
					{
						_availableScopesThisFrame.Add(gamepadNavigationScope);
					}
				}
			}
		}
	}

	internal void OnWidgetUsedNavigationMovementsUpdated(Widget widget)
	{
		if (widget.UsedNavigationMovements != GamepadNavigationTypes.None && !_navigationBlockingWidgets.Contains(widget))
		{
			_navigationBlockingWidgets.Add(widget);
		}
		else if (widget.UsedNavigationMovements == GamepadNavigationTypes.None && _navigationBlockingWidgets.Contains(widget))
		{
			_navigationBlockingWidgets.Remove(widget);
		}
	}

	internal void AddForcedScopeCollection(GamepadNavigationForcedScopeCollection forcedCollection)
	{
		if (!_forcedScopeCollections.Contains(forcedCollection))
		{
			_forcedScopeCollections.Add(forcedCollection);
			AddItemToDictionaryList(_forcedNavigationScopeCollectionParents, forcedCollection.ParentWidget, forcedCollection);
			CollectScopesForForcedCollection(forcedCollection);
			forcedCollection.OnAvailabilityChanged = OnForcedScopeCollectionAvailabilityStateChanged;
			_isForcedCollectionsDirty = true;
		}
		else
		{
			Debug.FailedAssert("Trying to add a navigation scope collection more than once", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "AddForcedScopeCollection", 620);
		}
		_isAvailableScopesDirty = true;
	}

	internal void RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
		if (_forcedScopeCollections.Contains(collection))
		{
			collection.ClearScopes();
			_forcedScopeCollections.Remove(collection);
			if (collection.ParentWidget != null && _forcedNavigationScopeCollectionParents.ContainsKey(collection.ParentWidget))
			{
				RemoveItemFromDictionaryList(_forcedNavigationScopeCollectionParents, collection.ParentWidget, collection);
			}
		}
		collection.OnAvailabilityChanged = null;
		collection.ParentWidget = null;
		_isForcedCollectionsDirty = true;
		_isAvailableScopesDirty = true;
	}

	internal void AddNavigationScope(IGamepadNavigationContext context, GamepadNavigationScope scope, bool initializeScope = false)
	{
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			value.AddScope(scope);
		}
		else
		{
			OnContextAdded(context);
			_navigationScopes[context].AddScope(scope);
		}
		AddItemToDictionaryList(_navigationScopeParents, scope.ParentWidget, scope);
		if (initializeScope)
		{
			InitializeScope(context, scope);
		}
		_isAvailableScopesDirty = true;
	}

	internal void RemoveNavigationScope(IGamepadNavigationContext context, GamepadNavigationScope scope)
	{
		if (scope == null)
		{
			Debug.FailedAssert("Trying to remove null navigation data", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "RemoveNavigationScope", 677);
			return;
		}
		_availableScopesThisFrame.Remove(scope);
		_unsortedScopes.Remove(scope);
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			value.RemoveScope(scope);
			scope.ClearNavigatableWidgets();
			if (value.GetTotalNumberOfScopes() == 0)
			{
				OnContextRemoved(context);
			}
		}
		else
		{
			foreach (KeyValuePair<IGamepadNavigationContext, GamepadNavigationScopeCollection> item in _navigationScopes.Where((KeyValuePair<IGamepadNavigationContext, GamepadNavigationScopeCollection> x) => x.Value.AllScopes.Contains(scope)))
			{
				item.Value.RemoveScope(scope);
				scope.ClearNavigatableWidgets();
				if (item.Value.GetTotalNumberOfScopes() == 0)
				{
					OnContextRemoved(context);
				}
			}
		}
		for (int num = 0; num < _forcedScopeCollections.Count; num++)
		{
			if (_forcedScopeCollections[num].Scopes.Contains(scope))
			{
				_forcedScopeCollections[num].RemoveScope(scope);
			}
		}
		if (scope.ParentWidget != null)
		{
			_navigationScopeParents.Remove(scope.ParentWidget);
		}
		foreach (KeyValuePair<Widget, List<GamepadNavigationScope>> navigationScopeParent in _navigationScopeParents)
		{
			navigationScopeParent.Value.Remove(scope);
		}
		if (_navigationScopesById.TryGetValue(scope.ScopeID, out var value2) && value2.Contains(scope))
		{
			RemoveItemFromDictionaryList(_navigationScopesById, scope.ScopeID, scope);
		}
		bool flag = false;
		foreach (KeyValuePair<IGamepadNavigationContext, GamepadNavigationScopeCollection> navigationScope in _navigationScopes)
		{
			if (navigationScope.Value.HasScopeInAnyList(scope))
			{
				navigationScope.Value.RemoveScope(scope);
				if (scope.ParentWidget != null && _navigationScopeParents.TryGetValue(scope.ParentWidget, out var value3))
				{
					value3.Remove(scope);
				}
				scope.ClearNavigatableWidgets();
				flag = true;
			}
		}
		if (flag)
		{
			Debug.FailedAssert("Failed to remove scope from all containers: " + scope.ScopeID, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "RemoveNavigationScope", 760);
		}
		scope.ParentWidget = null;
		if (_activeNavigationScope == scope)
		{
			_activeNavigationScope = null;
		}
		_latestCachedContext = null;
		for (int num2 = 0; num2 < _availableScopesThisFrame.Count; num2++)
		{
			_availableScopesThisFrame[num2].IsAdditionalMovementsDirty = true;
		}
		_isAvailableScopesDirty = true;
	}

	internal void OnWidgetNavigationStatusChanged(IGamepadNavigationContext context, Widget widget)
	{
		if (!_navigationScopes.TryGetValue(context, out var value))
		{
			return;
		}
		for (int i = 0; i < value.AllScopes.Count; i++)
		{
			GamepadNavigationScope gamepadNavigationScope = value.AllScopes[i];
			if (gamepadNavigationScope.ParentWidget.CheckIsMyChildRecursive(widget) || widget.CheckIsMyChildRecursive(gamepadNavigationScope.ParentWidget))
			{
				gamepadNavigationScope.RefreshNavigatableChildren();
			}
		}
	}

	internal void OnWidgetNavigationIndexUpdated(IGamepadNavigationContext context, Widget widget)
	{
		if (widget == null)
		{
			return;
		}
		GamepadNavigationScope gamepadNavigationScope = FindClosestParentScopeOfWidget(widget);
		if (gamepadNavigationScope != null && !gamepadNavigationScope.DoNotAutomaticallyFindChildren)
		{
			gamepadNavigationScope.RemoveWidget(widget);
			if (widget.GamepadNavigationIndex != -1)
			{
				gamepadNavigationScope.AddWidget(widget);
			}
		}
	}

	internal bool HasNavigationScope(IGamepadNavigationContext context, GamepadNavigationScope scope)
	{
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			if (!value.VisibleScopes.Contains(scope) && !value.UninitializedScopes.Contains(scope))
			{
				return value.InvisibleScopes.Contains(scope);
			}
			return true;
		}
		return false;
	}

	internal bool HasNavigationScope(IGamepadNavigationContext context, Func<GamepadNavigationScope, bool> predicate)
	{
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			if (!value.VisibleScopes.Any((GamepadNavigationScope x) => predicate(x)))
			{
				return value.InvisibleScopes.Any((GamepadNavigationScope x) => predicate(x));
			}
			return true;
		}
		return false;
	}

	private void OnActiveScopeParentChanged(GamepadNavigationScope oldParent, GamepadNavigationScope newParent)
	{
		if (oldParent != null && newParent == null && oldParent.LatestNavigationElementIndex != -1 && oldParent.IsAvailable())
		{
			_isAvailableScopesDirty = true;
		}
	}

	private void OnScopeVisibilityChanged(GamepadNavigationScope scope, bool isVisible)
	{
		_isAvailableScopesDirty = true;
	}

	private void OnForcedScopeCollectionAvailabilityStateChanged(GamepadNavigationForcedScopeCollection scopeCollection)
	{
		_isAvailableScopesDirty = true;
		_isForcedCollectionsDirty = true;
	}

	private void OnScopeNavigatableWidgetsChanged(GamepadNavigationScope scope)
	{
		if (!_unsortedScopes.Contains(scope))
		{
			_unsortedScopes.Add(scope);
		}
		if (scope.IsInitialized)
		{
			_isAvailableScopesDirty = true;
		}
	}

	public void SetAllDirty()
	{
		_autoRefreshTimer = 0f;
		_isAvailableScopesDirty = true;
		_isForcedCollectionsDirty = true;
		_latestCachedContext = null;
	}

	private void CollectScopesForForcedCollection(GamepadNavigationForcedScopeCollection collection)
	{
		collection.ClearScopes();
		if (!_navigationScopes.TryGetValue(collection.ParentWidget.GamepadNavigationContext, out var value))
		{
			return;
		}
		for (int i = 0; i < value.AllScopes.Count; i++)
		{
			GamepadNavigationScope gamepadNavigationScope = value.AllScopes[i];
			if (collection.ParentWidget == gamepadNavigationScope.ParentWidget || collection.ParentWidget.CheckIsMyChildRecursive(gamepadNavigationScope.ParentWidget))
			{
				collection.AddScope(gamepadNavigationScope);
			}
		}
	}

	private void InitializeScope(IGamepadNavigationContext context, GamepadNavigationScope scope)
	{
		if (_navigationScopes.TryGetValue(context, out var value))
		{
			value.OnNavigationScopeInitialized(scope);
		}
		scope.Initialize();
		for (int num = _forcedScopeCollections.Count - 1; num >= 0; num--)
		{
			if (_forcedScopeCollections[num].ParentWidget == scope.ParentWidget || _forcedScopeCollections[num].ParentWidget.CheckIsMyChildRecursive(scope.ParentWidget))
			{
				_forcedScopeCollections[num].AddScope(scope);
				break;
			}
		}
		for (int i = 0; i < _availableScopesThisFrame.Count; i++)
		{
			_availableScopesThisFrame[i].IsAdditionalMovementsDirty = true;
		}
		if (!string.IsNullOrEmpty(scope.ScopeID))
		{
			AddItemToDictionaryList(_navigationScopesById, scope.ScopeID, scope);
		}
		if (scope.ParentScope == null)
		{
			List<Widget> allParents = scope.ParentWidget.GetAllParents();
			for (int j = 0; j < allParents.Count; j++)
			{
				Widget key = allParents[j];
				if (NavigationScopeParents.TryGetValue(key, out var value2))
				{
					if (value2.Count > 0)
					{
						scope.SetParentScope(value2[0]);
					}
					break;
				}
			}
		}
		_isAvailableScopesDirty = true;
	}

	private void AddItemToDictionaryList<TKey, TValue>(Dictionary<TKey, List<TValue>> sourceDict, TKey key, TValue item)
	{
		if (sourceDict.TryGetValue(key, out var value))
		{
			if (!value.Contains(item))
			{
				value.Add(item);
			}
			else
			{
				Debug.FailedAssert("Trying to add same item to source dictionary twice", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "AddItemToDictionaryList", 951);
			}
		}
		else
		{
			sourceDict.Add(key, new List<TValue> { item });
		}
	}

	private void RemoveItemFromDictionaryList<TKey, TValue>(Dictionary<TKey, List<TValue>> sourceDict, TKey key, TValue item)
	{
		if (sourceDict.TryGetValue(key, out var value))
		{
			value.Remove(item);
			if (value.Count == 0)
			{
				sourceDict.Remove(key);
			}
		}
		else
		{
			Debug.FailedAssert("Trying to remove non-existent item from source dictionary", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "RemoveItemFromDictionaryList", 972);
		}
	}

	internal void OnWidgetHoverBegin(Widget widget)
	{
		if (IsCursorMovingForNavigation || IsInWrapMovement || widget.GamepadNavigationIndex == -1 || _isAvailableScopesDirty || _shouldUpdateAvailableScopes)
		{
			return;
		}
		GamepadNavigationForcedScopeCollection activeForcedScopeCollection = _activeForcedScopeCollection;
		if (activeForcedScopeCollection != null && !activeForcedScopeCollection.Scopes.Contains(_activeNavigationScope))
		{
			return;
		}
		int num = _activeNavigationScope.FindIndexOfWidget(widget);
		if (_activeNavigationScope != null && num != -1)
		{
			_activeNavigationScope.LatestNavigationElementIndex = num;
			return;
		}
		for (int i = 0; i < _availableScopesThisFrame.Count; i++)
		{
			GamepadNavigationScope gamepadNavigationScope = _availableScopesThisFrame[i];
			int num2 = gamepadNavigationScope.FindIndexOfWidget(widget);
			if (!gamepadNavigationScope.DoNotAutoGainNavigationOnInit && num2 != -1)
			{
				if (_activeNavigationScope != gamepadNavigationScope && gamepadNavigationScope.IsAvailable())
				{
					SetActiveNavigationScope(gamepadNavigationScope);
					_activeNavigationScope.LatestNavigationElementIndex = num2;
				}
				break;
			}
		}
	}

	internal void OnWidgetHoverEnd(Widget widget)
	{
	}

	internal void OnWidgetDisconnectedFromRoot(Widget widget)
	{
		if (_navigationScopes.TryGetValue(widget.GamepadNavigationContext, out var value))
		{
			value.OnWidgetDisconnectedFromRoot(widget);
		}
		if (_navigationScopeParents.TryGetValue(widget, out var value2))
		{
			List<GamepadNavigationScope> list = value2.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				list[i].ClearNavigatableWidgets();
				RemoveNavigationScope(widget.GamepadNavigationContext, list[i]);
			}
		}
		if (_forcedNavigationScopeCollectionParents.TryGetValue(widget, out var value3))
		{
			for (int j = 0; j < value3.Count; j++)
			{
				RemoveForcedScopeCollection(value3[j]);
			}
		}
	}

	internal void SetContextNavigationGainAfterTime(IGamepadNavigationContext context, float seconds, Func<bool> predicate)
	{
		if (_navigationGainControllers.TryGetValue(context, out var value))
		{
			value.GainNavigationAfterTime(seconds, predicate);
		}
	}

	internal void SetContextNavigationGainAfterFrames(IGamepadNavigationContext context, int frames, Func<bool> predicate)
	{
		if (_navigationGainControllers.TryGetValue(context, out var value))
		{
			value.GainNavigationAfterFrames(frames, predicate);
		}
	}

	internal void OnContextGainedNavigation(IGamepadNavigationContext context)
	{
		if (!IsControllerActive || context == null || _activeNavigationScope?.ParentWidget?.GamepadNavigationContext == context || !context.IsAvailableForNavigation() || !_navigationScopes.TryGetValue(context, out var value))
		{
			return;
		}
		RefreshAvailableScopes();
		GamepadNavigationScope gamepadNavigationScope = value.VisibleScopes.FirstOrDefault((GamepadNavigationScope x) => x.IsDefaultNavigationScope && x.IsAvailable());
		if (gamepadNavigationScope != null && _availableScopesThisFrame.Contains(gamepadNavigationScope))
		{
			if (_availableScopesThisFrame.Contains(gamepadNavigationScope))
			{
				MoveCursorToFirstAvailableWidgetInScope(gamepadNavigationScope);
			}
			return;
		}
		for (int num = 0; num < _availableScopesThisFrame.Count; num++)
		{
			if (value.HasScopeInAnyList(_availableScopesThisFrame[num]))
			{
				MoveCursorToFirstAvailableWidgetInScope(_availableScopesThisFrame[num]);
				return;
			}
		}
		for (int num2 = 0; num2 < value.VisibleScopes.Count; num2++)
		{
			if (value.VisibleScopes[num2].IsAvailable() && _availableScopesThisFrame.Contains(value.VisibleScopes[num2]))
			{
				_nextScopeToGainNavigation = value.VisibleScopes[num2];
				break;
			}
		}
	}

	private void SetActiveNavigationScope(GamepadNavigationScope scope)
	{
		if (scope != null && scope != _activeNavigationScope)
		{
			if (_activeForcedScopeCollection != null && _activeForcedScopeCollection.Scopes.Contains(scope))
			{
				_activeForcedScopeCollection.ActiveScope = scope;
			}
			if (_activeNavigationScope != null)
			{
				GamepadNavigationScope activeNavigationScope = _activeNavigationScope;
				activeNavigationScope.OnParentScopeChanged = (Action<GamepadNavigationScope, GamepadNavigationScope>)Delegate.Remove(activeNavigationScope.OnParentScopeChanged, new Action<GamepadNavigationScope, GamepadNavigationScope>(OnActiveScopeParentChanged));
			}
			GamepadNavigationScope activeNavigationScope2 = _activeNavigationScope;
			_activeNavigationScope = scope;
			_activeNavigationScope.PreviousScope = activeNavigationScope2;
			activeNavigationScope2?.SetIsActiveScope(isActive: false);
			_activeNavigationScope.SetIsActiveScope(isActive: true);
			if (_activeNavigationScope != null)
			{
				GamepadNavigationScope activeNavigationScope3 = _activeNavigationScope;
				activeNavigationScope3.OnParentScopeChanged = (Action<GamepadNavigationScope, GamepadNavigationScope>)Delegate.Combine(activeNavigationScope3.OnParentScopeChanged, new Action<GamepadNavigationScope, GamepadNavigationScope>(OnActiveScopeParentChanged));
			}
		}
	}

	private void OnGamepadNavigation(GamepadNavigationTypes movement)
	{
		if (_isAvailableScopesDirty || _isForcedCollectionsDirty || LatestContext == null || AnyWidgetUsingNavigation)
		{
			return;
		}
		if (_activeNavigationScope?.ParentWidget != null)
		{
			GamepadNavigationScope activeNavigationScope = _activeNavigationScope;
			if (activeNavigationScope != null && activeNavigationScope.IsAvailable())
			{
				if (HandleGamepadNavigation(movement) && _latestGamepadNavigationWidget != null)
				{
					GamepadNavigationTypes gamepadNavigationTypes = GamepadNavigationHelper.GetMovementsToReachRectangle(rect: new SimpleRectangle(_latestGamepadNavigationWidget.GlobalPosition.X, _latestGamepadNavigationWidget.GlobalPosition.Y, _latestGamepadNavigationWidget.Size.X, _latestGamepadNavigationWidget.Size.Y), fromPosition: MousePosition);
					if (((gamepadNavigationTypes & GamepadNavigationTypes.Left) != GamepadNavigationTypes.None && movement == GamepadNavigationTypes.Right) || ((gamepadNavigationTypes & GamepadNavigationTypes.Right) != GamepadNavigationTypes.None && movement == GamepadNavigationTypes.Left) || ((gamepadNavigationTypes & GamepadNavigationTypes.Up) != GamepadNavigationTypes.None && movement == GamepadNavigationTypes.Down) || ((gamepadNavigationTypes & GamepadNavigationTypes.Down) != GamepadNavigationTypes.None && movement == GamepadNavigationTypes.Up))
					{
						IsInWrapMovement = true;
					}
				}
				else if (!IsCursorMovingForNavigation && !IsInWrapMovement && (_activeNavigationScope == null || !_activeNavigationScope.GetRectangle().IsPointInside(MousePosition)))
				{
					MoveCursorToBestAvailableScope(isFromInput: true, movement);
				}
				return;
			}
		}
		MoveCursorToBestAvailableScope(isFromInput: false, movement);
	}

	private bool HandleGamepadNavigation(GamepadNavigationTypes movement)
	{
		if (_activeNavigationScope?.ParentWidget == null)
		{
			Debug.FailedAssert("Active navigation scope or it's parent widget shouldn't be null", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\GamepadNavigation\\GauntletGamepadNavigationManager.cs", "HandleGamepadNavigation", 1188);
			MoveCursorToBestAvailableScope(isFromInput: true);
			return false;
		}
		if (!IsInWrapMovement)
		{
			if ((_activeNavigationScope.ScopeMovements & movement) == 0 && (_activeNavigationScope.AlternateScopeMovements & movement) == 0)
			{
				bool flag = NavigateBetweenScopes(movement, _activeNavigationScope);
				if (!flag && !IsHoldingDpadKeysForNavigation)
				{
					Widget lastNavigatedWidget = _activeNavigationScope.LastNavigatedWidget;
					if (lastNavigatedWidget == null || !lastNavigatedWidget.IsPointInsideGamepadCursorArea(MousePosition))
					{
						SetCurrentNavigatedWidget(_activeNavigationScope, _activeNavigationScope.LastNavigatedWidget);
						flag = true;
					}
				}
				return flag;
			}
			if (_activeNavigationScope.IsAvailable())
			{
				bool flag2 = NavigateWithinScope(_activeNavigationScope, movement);
				if (!flag2 && !IsHoldingDpadKeysForNavigation)
				{
					GamepadNavigationScope activeNavigationScope = _activeNavigationScope;
					if (activeNavigationScope == null || activeNavigationScope.LastNavigatedWidget?.IsPointInsideMeasuredArea(MousePosition) != true)
					{
						flag2 = MoveCursorToBestAvailableScope(isFromInput: true, movement);
					}
				}
				return flag2;
			}
		}
		return false;
	}

	private bool NavigateBetweenScopes(GamepadNavigationTypes movement, GamepadNavigationScope currentScope)
	{
		RefreshExitMovementForScope(currentScope, movement);
		GamepadNavigationScope gamepadNavigationScope = currentScope.InterScopeMovements[movement];
		if (gamepadNavigationScope != null)
		{
			Widget bestWidgetToScope = GetBestWidgetToScope(currentScope, gamepadNavigationScope, movement);
			if (bestWidgetToScope != null)
			{
				if (gamepadNavigationScope.ChildScopes.Count > 0)
				{
					float distanceToScope;
					GamepadNavigationScope closestChildScopeAtDirection = GamepadNavigationHelper.GetClosestChildScopeAtDirection(gamepadNavigationScope, MousePosition, movement, checkForAutoGain: false, out distanceToScope);
					float distanceToClosestWidgetEdge = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(gamepadNavigationScope.ParentWidget, MousePosition, movement);
					if (closestChildScopeAtDirection != null && closestChildScopeAtDirection != currentScope && distanceToScope < distanceToClosestWidgetEdge)
					{
						Widget bestWidgetToScope2 = GetBestWidgetToScope(currentScope, closestChildScopeAtDirection, movement);
						if (bestWidgetToScope2 != null)
						{
							SetCurrentNavigatedWidget(closestChildScopeAtDirection, bestWidgetToScope2);
							return true;
						}
					}
				}
				else if (currentScope.ParentScope != null && (currentScope.ParentScope.ScopeMovements & movement) != GamepadNavigationTypes.None)
				{
					Widget bestWidgetToScope3 = GetBestWidgetToScope(currentScope, currentScope.ParentScope, movement);
					if (bestWidgetToScope3 != null)
					{
						float distanceToClosestWidgetEdge2 = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(bestWidgetToScope3, MousePosition, movement);
						float distanceToClosestWidgetEdge3 = GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(gamepadNavigationScope.ParentWidget, MousePosition, movement);
						if (distanceToClosestWidgetEdge2 < distanceToClosestWidgetEdge3)
						{
							SetCurrentNavigatedWidget(currentScope.ParentScope, bestWidgetToScope3);
							return true;
						}
					}
				}
				SetCurrentNavigatedWidget(gamepadNavigationScope, bestWidgetToScope);
				return true;
			}
		}
		return false;
	}

	private bool NavigateWithinScope(GamepadNavigationScope scope, GamepadNavigationTypes movement)
	{
		if (scope.NavigatableWidgets.Count == 0)
		{
			return false;
		}
		if ((scope.ScopeMovements & movement) == 0 && (scope.AlternateScopeMovements & movement) == 0)
		{
			return false;
		}
		int num = ((movement == GamepadNavigationTypes.Right || movement == GamepadNavigationTypes.Down) ? 1 : (-1));
		if (scope.LatestNavigationElementIndex < 0 || scope.LatestNavigationElementIndex >= scope.NavigatableWidgets.Count)
		{
			scope.LatestNavigationElementIndex = scope.NavigatableWidgets.Count - 1;
		}
		int latestNavigationElementIndex = scope.LatestNavigationElementIndex;
		int num2 = latestNavigationElementIndex;
		if ((movement & scope.AlternateScopeMovements) != GamepadNavigationTypes.None)
		{
			num *= scope.AlternateMovementStepSize;
		}
		ReadOnlyCollection<Widget> navigatableWidgets = scope.NavigatableWidgets;
		bool flag = false;
		do
		{
			if (!scope.HasCircularMovement)
			{
				bool flag2 = false;
				if (scope.AlternateMovementStepSize > 0)
				{
					if ((movement & scope.ScopeMovements) != GamepadNavigationTypes.None && Math.Abs(num) == 1)
					{
						if (num2 % scope.AlternateMovementStepSize == 0 && num < 0)
						{
							flag2 = true;
						}
						else if (num2 % scope.AlternateMovementStepSize == scope.AlternateMovementStepSize - 1 && num > 0)
						{
							flag2 = true;
						}
						else if (num2 + num < 0 || num2 + num > scope.NavigatableWidgets.Count - 1)
						{
							flag2 = true;
						}
					}
					if (!flag2 && (movement & scope.AlternateScopeMovements) != GamepadNavigationTypes.None && Math.Abs(num) > 1)
					{
						int num3 = scope.NavigatableWidgets.Count % scope.AlternateMovementStepSize;
						if (scope.NavigatableWidgets.Count > 0 && num3 == 0)
						{
							num3 = scope.AlternateMovementStepSize;
						}
						int num4;
						if (num3 > 0)
						{
							num4 = scope.NavigatableWidgets.Count - num3;
							if (scope.NavigatableWidgets.Count != num3)
							{
								if (num2 < num4 && num2 + num >= scope.NavigatableWidgets.Count)
								{
									SetCurrentNavigatedWidget(scope, scope.GetLastAvailableWidget());
									return true;
								}
								if (num2 >= num4 && num2 + num >= scope.NavigatableWidgets.Count)
								{
									flag2 = true;
								}
							}
							else
							{
								flag2 = true;
							}
						}
						else
						{
							num4 = Math.Max(0, scope.NavigatableWidgets.Count - scope.AlternateMovementStepSize - 1);
						}
						if (num2 > num4 && num2 < scope.NavigatableWidgets.Count && num > 1)
						{
							flag2 = true;
						}
						if (num2 >= 0 && num2 < scope.AlternateMovementStepSize && num < 1)
						{
							flag2 = true;
						}
					}
				}
				else if (num2 + num < 0 || num2 + num > scope.NavigatableWidgets.Count - 1)
				{
					flag2 = true;
				}
				if (flag2)
				{
					return NavigateBetweenScopes(movement, _activeNavigationScope);
				}
			}
			num2 += num;
			if (num2 > scope.NavigatableWidgets.Count - 1 && !scope.HasCircularMovement)
			{
				return false;
			}
			num2 %= scope.NavigatableWidgets.Count;
			if (num2 < 0)
			{
				num2 = navigatableWidgets.Count - 1;
			}
			if (scope.IsWidgetVisible(navigatableWidgets[num2]))
			{
				flag = true;
				break;
			}
		}
		while (num2 >= 0 && num2 < navigatableWidgets.Count && num2 != latestNavigationElementIndex);
		if (num2 >= 0 && flag)
		{
			if (scope.ChildScopes.Count > 0)
			{
				float distanceToScope;
				GamepadNavigationScope closestChildScopeAtDirection = GamepadNavigationHelper.GetClosestChildScopeAtDirection(scope, MousePosition, movement, checkForAutoGain: false, out distanceToScope);
				if (distanceToScope != -1f && closestChildScopeAtDirection != null)
				{
					GamepadNavigationHelper.GetDistanceToClosestWidgetEdge(navigatableWidgets[num2], MousePosition, movement, out var closestPointOnEdge);
					if (GamepadNavigationHelper.GetDirectionalDistanceBetweenTwoPoints(movement, MousePosition, closestPointOnEdge) > distanceToScope)
					{
						SetCurrentNavigatedWidget(closestChildScopeAtDirection, closestChildScopeAtDirection.GetApproximatelyClosestWidgetToPosition(MousePosition, movement));
						return true;
					}
				}
			}
			SetCurrentNavigatedWidget(scope, scope.NavigatableWidgets[num2]);
			return true;
		}
		return false;
	}

	private bool SetCurrentNavigatedWidget(GamepadNavigationScope scope, Widget widget)
	{
		if (scope != null && Input.MouseMoveX == 0f && Input.MouseMoveY == 0f)
		{
			int num = scope.FindIndexOfWidget(widget);
			if (num != -1)
			{
				if (_activeNavigationScope != scope)
				{
					SetActiveNavigationScope(scope);
				}
				_latestGamepadNavigationWidget = widget;
				_activeNavigationScope.LatestNavigationElementIndex = num;
				if (IsControllerActive)
				{
					_cursorMoveStartTime = _time;
					_cursorMoveStartPosition = MousePosition;
					_stopCursorNextFrame = false;
					IsCursorMovingForNavigation = true;
					_latestGamepadNavigationWidget.OnGamepadNavigationFocusGain();
				}
				return true;
			}
		}
		return false;
	}

	private bool MoveCursorToBestAvailableScope(bool isFromInput, GamepadNavigationTypes preferredMovement = GamepadNavigationTypes.None)
	{
		GamepadNavigationScope gamepadNavigationScope = null;
		if (preferredMovement != GamepadNavigationTypes.None)
		{
			gamepadNavigationScope = GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(_availableScopesThisFrame, MousePosition, preferredMovement, isFromInput, false, out var _);
		}
		if (gamepadNavigationScope == null)
		{
			gamepadNavigationScope = GamepadNavigationHelper.GetClosestScopeFromList(_availableScopesThisFrame, MousePosition, checkForAutoGain: true);
		}
		if (gamepadNavigationScope != null)
		{
			bool flag = _activeForcedScopeCollection != null && _activeForcedScopeCollection.Scopes.Contains(_activeNavigationScope) && gamepadNavigationScope.LastNavigatedWidget != null;
			Widget widget = (((_activeNavigationScope != null && !_activeNavigationScope.IsAvailable() && _activeNavigationScope.ParentScope == gamepadNavigationScope) || flag) ? gamepadNavigationScope.LastNavigatedWidget : ((gamepadNavigationScope.ForceGainNavigationOnFirstChild || (!isFromInput && !gamepadNavigationScope.ForceGainNavigationOnClosestChild)) ? gamepadNavigationScope.GetFirstAvailableWidget() : ((preferredMovement == GamepadNavigationTypes.None) ? gamepadNavigationScope.GetApproximatelyClosestWidgetToPosition(MousePosition) : gamepadNavigationScope.GetApproximatelyClosestWidgetToPosition(MousePosition, preferredMovement))));
			if (widget != null)
			{
				SetCurrentNavigatedWidget(gamepadNavigationScope, widget);
				return true;
			}
		}
		return false;
	}

	private void MoveCursorToFirstAvailableWidgetInScope(GamepadNavigationScope scope)
	{
		SetCurrentNavigatedWidget(scope, scope.GetFirstAvailableWidget());
	}

	private void MoveCursorToClosestAvailableWidgetInScope(GamepadNavigationScope scope)
	{
		SetCurrentNavigatedWidget(scope, scope.GetApproximatelyClosestWidgetToPosition(MousePosition));
	}

	private void TryMoveCursorToPreviousScope(GamepadNavigationForcedScopeCollection fromCollection)
	{
		GamepadNavigationScope gamepadNavigationScope = fromCollection?.PreviousScope;
		if (gamepadNavigationScope != null && _availableScopesThisFrame.Contains(gamepadNavigationScope))
		{
			if (gamepadNavigationScope.LastNavigatedWidget == null)
			{
				SetCurrentNavigatedWidget(gamepadNavigationScope, gamepadNavigationScope.GetFirstAvailableWidget());
			}
			else
			{
				SetCurrentNavigatedWidget(gamepadNavigationScope, gamepadNavigationScope.LastNavigatedWidget);
			}
		}
	}

	private GamepadNavigationScope GetBestScopeAtDirectionFrom(GamepadNavigationScope fromScope, GamepadNavigationTypes movement)
	{
		if (fromScope.ChildScopes.Count > 0 && fromScope.HasMovement(movement))
		{
			float distanceToScope;
			GamepadNavigationScope closestChildScopeAtDirection = GamepadNavigationHelper.GetClosestChildScopeAtDirection(fromScope, MousePosition, movement, checkForAutoGain: false, out distanceToScope);
			if (closestChildScopeAtDirection != null && closestChildScopeAtDirection != _activeNavigationScope && distanceToScope > 0f)
			{
				return closestChildScopeAtDirection;
			}
		}
		GamepadNavigationScope gamepadNavigationScope = fromScope.ManualScopes[movement];
		if (gamepadNavigationScope == null)
		{
			if (!string.IsNullOrEmpty(fromScope.ManualScopeIDs[movement]))
			{
				gamepadNavigationScope = GetManualScopeAtDirection(movement, fromScope);
			}
			else if (fromScope.GetShouldFindScopeByPosition(movement))
			{
				if (fromScope.ParentScope != null && fromScope.ParentScope.HasMovement(movement))
				{
					List<GamepadNavigationScope> list = fromScope.ParentScope.ChildScopes.ToList();
					list.Remove(fromScope);
					float distanceToScope2;
					if (list.Count > 0)
					{
						gamepadNavigationScope = GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(list, fromScope, MousePosition, movement, checkForAutoGain: false, out distanceToScope2);
					}
					if (gamepadNavigationScope == null && fromScope.ParentScope != null)
					{
						GamepadNavigationForcedScopeCollection activeForcedScopeCollection = _activeForcedScopeCollection;
						if (activeForcedScopeCollection == null || activeForcedScopeCollection.Scopes.Contains(fromScope.ParentScope))
						{
							if (fromScope.ParentScope.HasMovement(movement))
							{
								gamepadNavigationScope = fromScope.ParentScope;
								if (gamepadNavigationScope.GetApproximatelyClosestWidgetToPosition(MousePosition, movement, angleCheck: true) == null)
								{
									return GetBestScopeAtDirectionFrom(gamepadNavigationScope, movement);
								}
							}
							else
							{
								bool num = _availableScopesThisFrame.Remove(fromScope.ParentScope);
								gamepadNavigationScope = GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(_availableScopesThisFrame, fromScope, MousePosition, movement, checkForAutoGain: false, out distanceToScope2);
								if (num)
								{
									_availableScopesThisFrame.Add(fromScope.ParentScope);
								}
							}
						}
					}
				}
				else
				{
					bool num2 = fromScope.ChildScopes.Count > 0;
					List<GamepadNavigationScope> list2 = _availableScopesThisFrame;
					if (num2)
					{
						list2 = list2.ToList();
						for (int i = 0; i < fromScope.ChildScopes.Count; i++)
						{
							list2.Remove(fromScope.ChildScopes[i]);
						}
					}
					gamepadNavigationScope = GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(list2, fromScope, MousePosition, movement, checkForAutoGain: false, out var distanceToScope3);
					if (gamepadNavigationScope != null && gamepadNavigationScope.ChildScopes.Count > 0)
					{
						float distanceToScope4;
						GamepadNavigationScope closestChildScopeAtDirection2 = GamepadNavigationHelper.GetClosestChildScopeAtDirection(gamepadNavigationScope, MousePosition, movement, checkForAutoGain: false, out distanceToScope4);
						float distance;
						Widget approximatelyClosestWidgetToPosition = gamepadNavigationScope.GetApproximatelyClosestWidgetToPosition(MousePosition, out distance, movement, angleCheck: true);
						if (closestChildScopeAtDirection2 != null && closestChildScopeAtDirection2 != _activeNavigationScope && (distanceToScope4 < distanceToScope3 || (approximatelyClosestWidgetToPosition != null && distanceToScope4 < distance)))
						{
							gamepadNavigationScope = closestChildScopeAtDirection2;
						}
					}
				}
			}
		}
		return gamepadNavigationScope;
	}

	private void RefreshExitMovementForScope(GamepadNavigationScope scope, GamepadNavigationTypes movement)
	{
		scope.InterScopeMovements[movement] = GetBestScopeAtDirectionFrom(scope, movement);
	}

	private GamepadNavigationTypes GetMovementForInput(InputKey input)
	{
		return input switch
		{
			InputKey.ControllerLUp => GamepadNavigationTypes.Up, 
			InputKey.ControllerLRight => GamepadNavigationTypes.Right, 
			InputKey.ControllerLDown => GamepadNavigationTypes.Down, 
			InputKey.ControllerLLeft => GamepadNavigationTypes.Left, 
			_ => GamepadNavigationTypes.None, 
		};
	}

	private GamepadNavigationScope GetManualScopeAtDirection(GamepadNavigationTypes movement, GamepadNavigationScope fromScope)
	{
		GamepadNavigationScope gamepadNavigationScope = fromScope.ManualScopes[movement];
		string text = fromScope.ManualScopeIDs[movement];
		if (gamepadNavigationScope == null)
		{
			if (string.IsNullOrEmpty(text) || text == "None")
			{
				return null;
			}
			if (_navigationScopesById.TryGetValue(text, out var value))
			{
				gamepadNavigationScope = ((value.Count != 1) ? GamepadNavigationHelper.GetClosestScopeAtDirectionFromList(value, MousePosition, movement, false, false, out var _) : value[0]);
				if (gamepadNavigationScope != null && !gamepadNavigationScope.IsAvailable())
				{
					gamepadNavigationScope = GetManualScopeAtDirection(movement, gamepadNavigationScope);
				}
			}
		}
		return gamepadNavigationScope;
	}

	private Widget GetBestWidgetToScope(GamepadNavigationScope fromScope, GamepadNavigationScope toScope, GamepadNavigationTypes movement)
	{
		if (toScope.ForceGainNavigationOnFirstChild)
		{
			return toScope.GetFirstAvailableWidget();
		}
		if (toScope.ForceGainNavigationBasedOnDirection && (fromScope == null || toScope != fromScope.ParentScope) && ((toScope.ScopeMovements & movement) != GamepadNavigationTypes.None || (toScope.AlternateScopeMovements & movement) != GamepadNavigationTypes.None))
		{
			if ((movement & GamepadNavigationTypes.Up) != GamepadNavigationTypes.None || (movement & GamepadNavigationTypes.Left) != GamepadNavigationTypes.None)
			{
				return toScope.GetLastAvailableWidget();
			}
			return toScope.GetFirstAvailableWidget();
		}
		if (fromScope.ParentScope == toScope)
		{
			return toScope.GetApproximatelyClosestWidgetToPosition(MousePosition, movement, angleCheck: true);
		}
		return toScope.GetApproximatelyClosestWidgetToPosition(MousePosition, movement);
	}

	private GamepadNavigationScope FindClosestParentScopeOfWidget(Widget widget)
	{
		Widget widget2 = widget;
		while (widget2 != null && !widget2.DoNotAcceptNavigation)
		{
			if (_navigationScopeParents.TryGetValue(widget2, out var value))
			{
				if (value.Count > 0)
				{
					return value[0];
				}
				return null;
			}
			widget2 = widget2.ParentWidget;
		}
		return null;
	}

	private GamepadNavigationForcedScopeCollection FindAvailableForcedScope()
	{
		if (_forcedScopeCollections.Count > 0)
		{
			if (_isForcedCollectionsDirty)
			{
				_forcedScopeCollections.Sort(_cachedForcedScopeComparer);
				_forcedScopeCollections.ForEach(delegate(GamepadNavigationForcedScopeCollection x)
				{
					CollectScopesForForcedCollection(x);
				});
				_isForcedCollectionsDirty = false;
				_isAvailableScopesDirty = true;
			}
			for (int num = _forcedScopeCollections.Count - 1; num >= 0; num--)
			{
				if (IsControllerActive && _forcedScopeCollections[num].IsAvailable())
				{
					return _forcedScopeCollections[num];
				}
			}
		}
		return null;
	}

	private void HandleInput(float dt)
	{
		if (IsControllerActive)
		{
			GamepadNavigationTypes gamepadNavigationTypes = GamepadNavigationTypes.None;
			if (Input.IsKeyPressed(InputKey.ControllerLLeft))
			{
				gamepadNavigationTypes = GamepadNavigationTypes.Left;
			}
			else if (Input.IsKeyPressed(InputKey.ControllerLRight))
			{
				gamepadNavigationTypes = GamepadNavigationTypes.Right;
			}
			else if (Input.IsKeyPressed(InputKey.ControllerLDown))
			{
				gamepadNavigationTypes = GamepadNavigationTypes.Down;
			}
			else if (Input.IsKeyPressed(InputKey.ControllerLUp))
			{
				gamepadNavigationTypes = GamepadNavigationTypes.Up;
			}
			if (gamepadNavigationTypes != GamepadNavigationTypes.None)
			{
				OnGamepadNavigation(gamepadNavigationTypes);
			}
			_navigationHoldTimer += dt;
			if (!IsHoldingDpadKeysForNavigation && _navigationHoldTimer > 0.15f)
			{
				IsHoldingDpadKeysForNavigation = true;
				_navigationHoldTimer = 0f;
			}
			else if (IsHoldingDpadKeysForNavigation && _navigationHoldTimer > 0.08f)
			{
				InputKey inputKey = (InputKey)0;
				if (Input.IsKeyDown(InputKey.ControllerLUp))
				{
					inputKey = InputKey.ControllerLUp;
				}
				else if (Input.IsKeyDown(InputKey.ControllerLRight))
				{
					inputKey = InputKey.ControllerLRight;
				}
				else if (Input.IsKeyDown(InputKey.ControllerLDown))
				{
					inputKey = InputKey.ControllerLDown;
				}
				else if (Input.IsKeyDown(InputKey.ControllerLLeft))
				{
					inputKey = InputKey.ControllerLLeft;
				}
				if (inputKey != 0)
				{
					GamepadNavigationTypes movementForInput = GetMovementForInput(inputKey);
					OnGamepadNavigation(movementForInput);
				}
				_navigationHoldTimer = 0f;
			}
		}
		if (!Input.IsKeyDown(InputKey.ControllerLUp) && !Input.IsKeyDown(InputKey.ControllerLRight) && !Input.IsKeyDown(InputKey.ControllerLDown) && !Input.IsKeyDown(InputKey.ControllerLLeft))
		{
			IsHoldingDpadKeysForNavigation = false;
			_navigationHoldTimer = 0f;
		}
	}

	private void HandleCursorMovement()
	{
		Vector2 targetCursorPosition = GetTargetCursorPosition();
		if (_latestGamepadNavigationWidget != null && targetCursorPosition != Vec2.Invalid)
		{
			if (IsCursorMovingForNavigation)
			{
				if (_time - _cursorMoveStartTime <= _mouseCursorMoveTime)
				{
					MousePosition = (IsFollowingMobileTarget ? targetCursorPosition : Vector2.Lerp(_cursorMoveStartPosition, targetCursorPosition, (_time - _cursorMoveStartTime) / _mouseCursorMoveTime));
					IsCursorMovingForNavigation = true;
				}
				else
				{
					bool num = _latestGamepadNavigationWidget != null && !IsHoldingDpadKeysForNavigation && IsControllerActive && (!Input.IsAnyTouchActive || !IsTouchpadMouseEnabled) && Vector2.Distance(MousePosition, targetCursorPosition) > 1.44f && Input.MouseMoveX == 0f && Input.MouseMoveY == 0f;
					MousePosition = targetCursorPosition;
					if (!num)
					{
						_latestGamepadNavigationWidget = null;
						_stopCursorNextFrame = true;
						IsInWrapMovement = false;
						IsFollowingMobileTarget = false;
					}
				}
			}
			else if (_latestGamepadNavigationWidget != null)
			{
				_latestGamepadNavigationWidget = null;
				_stopCursorNextFrame = true;
				IsFollowingMobileTarget = false;
				IsInWrapMovement = false;
			}
		}
		if (IsCursorMovingForNavigation || _activeNavigationScope == null || !_activeNavigationScope.FollowMobileTargets || !_wasCursorInsideActiveScopeLastFrame)
		{
			return;
		}
		Widget lastNavigatedWidget = _activeNavigationScope.LastNavigatedWidget;
		if (lastNavigatedWidget != null)
		{
			Vector2 vector = lastNavigatedWidget.GlobalPosition + lastNavigatedWidget.Size / 2f;
			if (_lastNavigatedWidgetPosition.X != float.NaN && Vector2.Distance(vector, _lastNavigatedWidgetPosition) > 1.44f)
			{
				SetCurrentNavigatedWidget(_activeNavigationScope, _activeNavigationScope.LastNavigatedWidget);
				_autoRefreshTimer = 0f;
				IsFollowingMobileTarget = true;
			}
			_lastNavigatedWidgetPosition = vector;
		}
	}

	private void OnDpadNavigationStopped()
	{
		_lastNavigatedWidgetPosition = new Vector2(float.NaN, float.NaN);
		_latestGamepadNavigationWidget = null;
		_stopCursorNextFrame = true;
		IsFollowingMobileTarget = false;
		IsInWrapMovement = false;
		_navigationHoldTimer = 0f;
	}
}
