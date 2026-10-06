using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.GamepadNavigation;

internal class GamepadNavigationScopeCollection
{
	private Action<GamepadNavigationScope> _onScopeNavigatableWidgetsChanged;

	private Action<GamepadNavigationScope, bool> _onScopeVisibilityChanged;

	private List<GamepadNavigationScope> _allScopes;

	private List<GamepadNavigationScope> _uninitializedScopes;

	private List<GamepadNavigationScope> _visibleScopes;

	private List<GamepadNavigationScope> _invisibleScopes;

	private List<GamepadNavigationScope> _dirtyScopes;

	public IGamepadNavigationContext Source { get; private set; }

	public ReadOnlyCollection<GamepadNavigationScope> AllScopes { get; private set; }

	public ReadOnlyCollection<GamepadNavigationScope> UninitializedScopes { get; private set; }

	public ReadOnlyCollection<GamepadNavigationScope> VisibleScopes { get; private set; }

	public ReadOnlyCollection<GamepadNavigationScope> InvisibleScopes { get; private set; }

	public GamepadNavigationScopeCollection(IGamepadNavigationContext source, Action<GamepadNavigationScope> onScopeNavigatableWidgetsChanged, Action<GamepadNavigationScope, bool> onScopeVisibilityChanged)
	{
		_onScopeNavigatableWidgetsChanged = onScopeNavigatableWidgetsChanged;
		_onScopeVisibilityChanged = onScopeVisibilityChanged;
		Source = source;
		_allScopes = new List<GamepadNavigationScope>();
		AllScopes = new ReadOnlyCollection<GamepadNavigationScope>(_allScopes);
		_uninitializedScopes = new List<GamepadNavigationScope>();
		UninitializedScopes = new ReadOnlyCollection<GamepadNavigationScope>(_uninitializedScopes);
		_visibleScopes = new List<GamepadNavigationScope>();
		VisibleScopes = new ReadOnlyCollection<GamepadNavigationScope>(_visibleScopes);
		_invisibleScopes = new List<GamepadNavigationScope>();
		InvisibleScopes = new ReadOnlyCollection<GamepadNavigationScope>(_invisibleScopes);
		_dirtyScopes = new List<GamepadNavigationScope>();
	}

	internal void OnFinalize()
	{
		ClearAllScopes();
		_onScopeVisibilityChanged = null;
		_onScopeNavigatableWidgetsChanged = null;
	}

	internal void HandleScopeVisibilities()
	{
		lock (_dirtyScopes)
		{
			for (int i = 0; i < _dirtyScopes.Count; i++)
			{
				if (_dirtyScopes[i] == null)
				{
					continue;
				}
				for (int j = i + 1; j < _dirtyScopes.Count; j++)
				{
					if (_dirtyScopes[i] == _dirtyScopes[j])
					{
						_dirtyScopes[j] = null;
					}
				}
			}
			foreach (GamepadNavigationScope dirtyScope in _dirtyScopes)
			{
				if (dirtyScope != null)
				{
					bool flag = dirtyScope.IsVisible();
					_visibleScopes.Remove(dirtyScope);
					_invisibleScopes.Remove(dirtyScope);
					if (flag)
					{
						_visibleScopes.Add(dirtyScope);
					}
					else
					{
						_invisibleScopes.Add(dirtyScope);
					}
					_onScopeVisibilityChanged(dirtyScope, flag);
				}
			}
			_dirtyScopes.Clear();
		}
	}

	private void OnScopeVisibilityChanged(GamepadNavigationScope scope, bool isVisible)
	{
		lock (_dirtyScopes)
		{
			_dirtyScopes.Add(scope);
		}
	}

	private void OnScopeNavigatableWidgetsChanged(GamepadNavigationScope scope)
	{
		_onScopeNavigatableWidgetsChanged(scope);
	}

	internal int GetTotalNumberOfScopes()
	{
		return _visibleScopes.Count + _invisibleScopes.Count + _uninitializedScopes.Count;
	}

	internal void AddScope(GamepadNavigationScope scope)
	{
		_uninitializedScopes.Add(scope);
		_allScopes.Add(scope);
	}

	internal void RemoveScope(GamepadNavigationScope scope)
	{
		_allScopes.Remove(scope);
		_uninitializedScopes.Remove(scope);
		_visibleScopes.Remove(scope);
		_invisibleScopes.Remove(scope);
		scope.OnVisibilityChanged = (Action<GamepadNavigationScope, bool>)Delegate.Remove(scope.OnVisibilityChanged, new Action<GamepadNavigationScope, bool>(OnScopeVisibilityChanged));
		scope.OnNavigatableWidgetsChanged = (Action<GamepadNavigationScope>)Delegate.Remove(scope.OnNavigatableWidgetsChanged, new Action<GamepadNavigationScope>(OnScopeNavigatableWidgetsChanged));
	}

	internal bool HasScopeInAnyList(GamepadNavigationScope scope)
	{
		if (!_visibleScopes.Contains(scope) && !_invisibleScopes.Contains(scope))
		{
			return _uninitializedScopes.Contains(scope);
		}
		return true;
	}

	internal void OnNavigationScopeInitialized(GamepadNavigationScope scope)
	{
		_uninitializedScopes.Remove(scope);
		if (scope.IsVisible())
		{
			_visibleScopes.Add(scope);
		}
		else
		{
			_invisibleScopes.Add(scope);
		}
		scope.OnVisibilityChanged = (Action<GamepadNavigationScope, bool>)Delegate.Combine(scope.OnVisibilityChanged, new Action<GamepadNavigationScope, bool>(OnScopeVisibilityChanged));
		scope.OnNavigatableWidgetsChanged = (Action<GamepadNavigationScope>)Delegate.Combine(scope.OnNavigatableWidgetsChanged, new Action<GamepadNavigationScope>(OnScopeNavigatableWidgetsChanged));
	}

	internal void OnWidgetDisconnectedFromRoot(Widget widget)
	{
		for (int i = 0; i < _visibleScopes.Count; i++)
		{
			if (_visibleScopes[i].FindIndexOfWidget(widget) != -1)
			{
				_visibleScopes[i].RemoveWidget(widget);
				return;
			}
		}
		for (int j = 0; j < _invisibleScopes.Count; j++)
		{
			if (_invisibleScopes[j].FindIndexOfWidget(widget) != -1)
			{
				_invisibleScopes[j].RemoveWidget(widget);
				return;
			}
		}
		for (int k = 0; k < _uninitializedScopes.Count; k++)
		{
			if (_uninitializedScopes[k].FindIndexOfWidget(widget) != -1)
			{
				_uninitializedScopes[k].RemoveWidget(widget);
				break;
			}
		}
	}

	private void ClearAllScopes()
	{
		for (int i = 0; i < _allScopes.Count; i++)
		{
			_allScopes[i].ClearNavigatableWidgets();
			GamepadNavigationScope gamepadNavigationScope = _allScopes[i];
			gamepadNavigationScope.OnNavigatableWidgetsChanged = (Action<GamepadNavigationScope>)Delegate.Remove(gamepadNavigationScope.OnNavigatableWidgetsChanged, new Action<GamepadNavigationScope>(OnScopeNavigatableWidgetsChanged));
			GamepadNavigationScope gamepadNavigationScope2 = _allScopes[i];
			gamepadNavigationScope2.OnVisibilityChanged = (Action<GamepadNavigationScope, bool>)Delegate.Remove(gamepadNavigationScope2.OnVisibilityChanged, new Action<GamepadNavigationScope, bool>(OnScopeVisibilityChanged));
		}
		_allScopes.Clear();
		_uninitializedScopes.Clear();
		_invisibleScopes.Clear();
		_visibleScopes.Clear();
		_allScopes = null;
		_uninitializedScopes = null;
		_invisibleScopes = null;
		_visibleScopes = null;
	}
}
