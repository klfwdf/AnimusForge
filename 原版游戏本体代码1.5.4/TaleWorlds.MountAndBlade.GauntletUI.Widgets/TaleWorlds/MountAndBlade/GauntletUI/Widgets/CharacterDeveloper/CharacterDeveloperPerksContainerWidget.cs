using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class CharacterDeveloperPerksContainerWidget : Widget
{
	private List<GamepadNavigationScope> _navigationScopes;

	private List<PerkItemButtonWidget> _perkWidgets;

	private bool _initialized;

	private int _lastPerkCount = -1;

	public string LeftScopeID { get; set; }

	public string RightScopeID { get; set; }

	public string DownScopeID { get; set; }

	public string UpScopeID { get; set; }

	public string FirstScopeID { get; set; }

	public CharacterDeveloperPerksContainerWidget(UIContext context)
		: base(context)
	{
		_perkWidgets = new List<PerkItemButtonWidget>();
		_navigationScopes = new List<GamepadNavigationScope>();
	}

	private void RefreshScopes()
	{
		foreach (GamepadNavigationScope navigationScope in _navigationScopes)
		{
			base.GamepadNavigationContext.RemoveNavigationScope(navigationScope);
		}
		_navigationScopes.Clear();
		GamepadNavigationScope gamepadNavigationScope = BuildNewScope(FirstScopeID);
		_navigationScopes.Add(gamepadNavigationScope);
		base.GamepadNavigationContext.AddNavigationScope(gamepadNavigationScope, initialize: true);
		int num = -1;
		if (_perkWidgets.Count > 0)
		{
			num = _perkWidgets[0].AlternativeType;
		}
		for (int i = 0; i < _perkWidgets.Count; i++)
		{
			if (_perkWidgets[i].AlternativeType == 0 || num == 0)
			{
				GamepadNavigationScope gamepadNavigationScope2 = BuildNewScope("Scope-" + i);
				_navigationScopes.Add(gamepadNavigationScope2);
				base.GamepadNavigationContext.AddNavigationScope(gamepadNavigationScope2, initialize: true);
			}
			_perkWidgets[i].GamepadNavigationIndex = 0;
			_navigationScopes[_navigationScopes.Count - 1].AddWidget(_perkWidgets[i]);
			num = _perkWidgets[i].AlternativeType;
		}
		for (int j = 0; j < _navigationScopes.Count; j++)
		{
			List<Widget> source = _navigationScopes[j].NavigatableWidgets.ToList();
			source = source.OrderBy((Widget w) => ((PerkItemButtonWidget)w).AlternativeType).ToList();
			_navigationScopes[j].ClearNavigatableWidgets();
			for (int num2 = 0; num2 < source.Count; num2++)
			{
				source[num2].GamepadNavigationIndex = num2;
				_navigationScopes[j].AddWidget(source[num2]);
			}
			if (_navigationScopes[j].NavigatableWidgets.Count > 1)
			{
				_navigationScopes[j].AlternateMovementStepSize = MathF.Round((float)_navigationScopes[j].NavigatableWidgets.Count / 2f);
				_navigationScopes[j].AlternateScopeMovements = GamepadNavigationTypes.Vertical;
			}
			_navigationScopes[j].DownNavigationScopeID = DownScopeID;
			_navigationScopes[j].UpNavigationScopeID = UpScopeID;
			if (j == 0)
			{
				_navigationScopes[j].LeftNavigationScopeID = LeftScopeID;
				if (_navigationScopes.Count > 1)
				{
					_navigationScopes[j].RightNavigationScopeID = _navigationScopes[j + 1].ScopeID;
				}
			}
			else if (j == _navigationScopes.Count - 1)
			{
				if (_navigationScopes.Count > 1)
				{
					_navigationScopes[j].LeftNavigationScopeID = _navigationScopes[j - 1].ScopeID;
				}
				_navigationScopes[j].RightNavigationScopeID = RightScopeID;
			}
			else if (j > 0 && j < _navigationScopes.Count - 1)
			{
				_navigationScopes[j].LeftNavigationScopeID = _navigationScopes[j - 1].ScopeID;
				_navigationScopes[j].RightNavigationScopeID = _navigationScopes[j + 1].ScopeID;
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		if (!_initialized || _lastPerkCount != _perkWidgets.Count)
		{
			RefreshScopes();
			_initialized = true;
			_lastPerkCount = _perkWidgets.Count;
		}
	}

	private GamepadNavigationScope BuildNewScope(string scopeID)
	{
		return new GamepadNavigationScope
		{
			ScopeID = scopeID,
			ParentWidget = this,
			ScopeMovements = GamepadNavigationTypes.Horizontal,
			DoNotAutomaticallyFindChildren = true
		};
	}

	protected override void OnChildAdded(Widget child)
	{
		if (child is PerkItemButtonWidget item)
		{
			_perkWidgets.Add(item);
			_initialized = false;
		}
	}

	protected override void OnBeforeChildRemoved(Widget child)
	{
		if (child is PerkItemButtonWidget item)
		{
			_perkWidgets.Remove(item);
			_initialized = false;
		}
	}
}
