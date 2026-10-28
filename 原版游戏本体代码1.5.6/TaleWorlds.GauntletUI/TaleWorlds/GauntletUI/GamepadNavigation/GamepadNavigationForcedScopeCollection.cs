using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.GamepadNavigation;

public class GamepadNavigationForcedScopeCollection
{
	public Action<GamepadNavigationForcedScopeCollection> OnAvailabilityChanged;

	private List<Widget> _invisibleParents;

	private bool _isEnabled;

	private Widget _parentWidget;

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
				OnAvailabilityChanged?.Invoke(this);
			}
		}
	}

	public bool IsDisabled
	{
		get
		{
			return !IsEnabled;
		}
		set
		{
			if (value == IsEnabled)
			{
				IsEnabled = !value;
			}
		}
	}

	public string CollectionID { get; set; }

	public int CollectionOrder { get; set; }

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

	public List<GamepadNavigationScope> Scopes { get; private set; }

	public GamepadNavigationScope ActiveScope { get; set; }

	public GamepadNavigationScope PreviousScope { get; set; }

	public GamepadNavigationForcedScopeCollection()
	{
		Scopes = new List<GamepadNavigationScope>();
		_invisibleParents = new List<Widget>();
		IsEnabled = true;
	}

	private void OnParentVisibilityChanged(Widget parent)
	{
		bool num = _invisibleParents.Count == 0;
		if (!parent.IsVisible)
		{
			_invisibleParents.Add(parent);
		}
		else
		{
			_invisibleParents.Remove(parent);
		}
		bool flag = _invisibleParents.Count == 0;
		if (num != flag)
		{
			OnAvailabilityChanged?.Invoke(this);
		}
	}

	public bool IsAvailable()
	{
		if (IsEnabled && _invisibleParents.Count == 0 && Scopes.Any((GamepadNavigationScope x) => x.IsAvailable()))
		{
			return ParentWidget.Context.GamepadNavigation.IsAvailableForNavigation();
		}
		return false;
	}

	public void AddScope(GamepadNavigationScope scope)
	{
		if (!Scopes.Contains(scope))
		{
			Scopes.Add(scope);
		}
		OnAvailabilityChanged?.Invoke(this);
	}

	public void RemoveScope(GamepadNavigationScope scope)
	{
		if (Scopes.Contains(scope))
		{
			Scopes.Remove(scope);
		}
		OnAvailabilityChanged?.Invoke(this);
	}

	public void ClearScopes()
	{
		Scopes.Clear();
	}

	public override string ToString()
	{
		return $"ID:{CollectionID} C.C.:{Scopes.Count}";
	}
}
