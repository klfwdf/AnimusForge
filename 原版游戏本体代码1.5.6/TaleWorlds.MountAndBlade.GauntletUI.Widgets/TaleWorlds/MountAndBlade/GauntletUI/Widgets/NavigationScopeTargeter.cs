using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class NavigationScopeTargeter : Widget
{
	private NavigationScopeTargeter _upNavigationScopeTargeter;

	private NavigationScopeTargeter _rightNavigationScopeTargeter;

	private NavigationScopeTargeter _downNavigationScopeTargeter;

	private NavigationScopeTargeter _leftNavigationScopeTargeter;

	public GamepadNavigationScope NavigationScope { get; private set; }

	public string ScopeID
	{
		get
		{
			return NavigationScope.ScopeID;
		}
		set
		{
			if (value != NavigationScope.ScopeID)
			{
				NavigationScope.ScopeID = value;
			}
		}
	}

	public GamepadNavigationTypes ScopeMovements
	{
		get
		{
			return NavigationScope.ScopeMovements;
		}
		set
		{
			if (value != NavigationScope.ScopeMovements)
			{
				NavigationScope.ScopeMovements = value;
			}
		}
	}

	public GamepadNavigationTypes AlternateScopeMovements
	{
		get
		{
			return NavigationScope.AlternateScopeMovements;
		}
		set
		{
			if (value != NavigationScope.AlternateScopeMovements)
			{
				NavigationScope.AlternateScopeMovements = value;
			}
		}
	}

	public int AlternateMovementStepSize
	{
		get
		{
			return NavigationScope.AlternateMovementStepSize;
		}
		set
		{
			if (value != NavigationScope.AlternateMovementStepSize)
			{
				NavigationScope.AlternateMovementStepSize = value;
			}
		}
	}

	public bool HasCircularMovement
	{
		get
		{
			return NavigationScope.HasCircularMovement;
		}
		set
		{
			if (value != NavigationScope.HasCircularMovement)
			{
				NavigationScope.HasCircularMovement = value;
			}
		}
	}

	public bool DoNotAutomaticallyFindChildren
	{
		get
		{
			return NavigationScope.DoNotAutomaticallyFindChildren;
		}
		set
		{
			if (value != NavigationScope.DoNotAutomaticallyFindChildren)
			{
				NavigationScope.DoNotAutomaticallyFindChildren = value;
			}
		}
	}

	public bool DoNotAutoGainNavigationOnInit
	{
		get
		{
			return NavigationScope.DoNotAutoGainNavigationOnInit;
		}
		set
		{
			if (value != NavigationScope.DoNotAutoGainNavigationOnInit)
			{
				NavigationScope.DoNotAutoGainNavigationOnInit = value;
			}
		}
	}

	public bool ForceGainNavigationBasedOnDirection
	{
		get
		{
			return NavigationScope.ForceGainNavigationBasedOnDirection;
		}
		set
		{
			if (value != NavigationScope.ForceGainNavigationBasedOnDirection)
			{
				NavigationScope.ForceGainNavigationBasedOnDirection = value;
			}
		}
	}

	public bool ForceGainNavigationOnClosestChild
	{
		get
		{
			return NavigationScope.ForceGainNavigationOnClosestChild;
		}
		set
		{
			if (value != NavigationScope.ForceGainNavigationOnClosestChild)
			{
				NavigationScope.ForceGainNavigationOnClosestChild = value;
			}
		}
	}

	public bool ForceGainNavigationOnFirstChild
	{
		get
		{
			return NavigationScope.ForceGainNavigationOnFirstChild;
		}
		set
		{
			if (value != NavigationScope.ForceGainNavigationOnFirstChild)
			{
				NavigationScope.ForceGainNavigationOnFirstChild = value;
			}
		}
	}

	public bool NavigateFromScopeEdges
	{
		get
		{
			return NavigationScope.NavigateFromScopeEdges;
		}
		set
		{
			if (value != NavigationScope.NavigateFromScopeEdges)
			{
				NavigationScope.NavigateFromScopeEdges = value;
			}
		}
	}

	public bool UseDiscoveryAreaAsScopeEdges
	{
		get
		{
			return NavigationScope.UseDiscoveryAreaAsScopeEdges;
		}
		set
		{
			if (value != NavigationScope.UseDiscoveryAreaAsScopeEdges)
			{
				NavigationScope.UseDiscoveryAreaAsScopeEdges = value;
			}
		}
	}

	public bool DoNotAutoNavigateAfterSort
	{
		get
		{
			return NavigationScope.DoNotAutoNavigateAfterSort;
		}
		set
		{
			if (value != NavigationScope.DoNotAutoNavigateAfterSort)
			{
				NavigationScope.DoNotAutoNavigateAfterSort = value;
			}
		}
	}

	public bool FollowMobileTargets
	{
		get
		{
			return NavigationScope.FollowMobileTargets;
		}
		set
		{
			if (value != NavigationScope.FollowMobileTargets)
			{
				NavigationScope.FollowMobileTargets = value;
			}
		}
	}

	public bool DoNotAutoCollectChildScopes
	{
		get
		{
			return NavigationScope.DoNotAutoCollectChildScopes;
		}
		set
		{
			if (value != NavigationScope.DoNotAutoCollectChildScopes)
			{
				NavigationScope.DoNotAutoCollectChildScopes = value;
			}
		}
	}

	public bool IsDefaultNavigationScope
	{
		get
		{
			return NavigationScope.IsDefaultNavigationScope;
		}
		set
		{
			if (value != NavigationScope.IsDefaultNavigationScope)
			{
				NavigationScope.IsDefaultNavigationScope = value;
			}
		}
	}

	public float ExtendDiscoveryAreaTop
	{
		get
		{
			return NavigationScope.ExtendDiscoveryAreaTop;
		}
		set
		{
			if (value != NavigationScope.ExtendDiscoveryAreaTop)
			{
				NavigationScope.ExtendDiscoveryAreaTop = value;
			}
		}
	}

	public float ExtendDiscoveryAreaRight
	{
		get
		{
			return NavigationScope.ExtendDiscoveryAreaRight;
		}
		set
		{
			if (value != NavigationScope.ExtendDiscoveryAreaRight)
			{
				NavigationScope.ExtendDiscoveryAreaRight = value;
			}
		}
	}

	public float ExtendDiscoveryAreaBottom
	{
		get
		{
			return NavigationScope.ExtendDiscoveryAreaBottom;
		}
		set
		{
			if (value != NavigationScope.ExtendDiscoveryAreaBottom)
			{
				NavigationScope.ExtendDiscoveryAreaBottom = value;
			}
		}
	}

	public float ExtendDiscoveryAreaLeft
	{
		get
		{
			return NavigationScope.ExtendDiscoveryAreaLeft;
		}
		set
		{
			if (value != NavigationScope.ExtendDiscoveryAreaLeft)
			{
				NavigationScope.ExtendDiscoveryAreaLeft = value;
			}
		}
	}

	public float ExtendChildrenCursorAreaLeft
	{
		get
		{
			return NavigationScope.ExtendChildrenCursorAreaLeft;
		}
		set
		{
			if (value != NavigationScope.ExtendChildrenCursorAreaLeft)
			{
				NavigationScope.ExtendChildrenCursorAreaLeft = value;
			}
		}
	}

	public float ExtendChildrenCursorAreaRight
	{
		get
		{
			return NavigationScope.ExtendChildrenCursorAreaRight;
		}
		set
		{
			if (value != NavigationScope.ExtendChildrenCursorAreaRight)
			{
				NavigationScope.ExtendChildrenCursorAreaRight = value;
			}
		}
	}

	public float ExtendChildrenCursorAreaTop
	{
		get
		{
			return NavigationScope.ExtendChildrenCursorAreaTop;
		}
		set
		{
			if (value != NavigationScope.ExtendChildrenCursorAreaTop)
			{
				NavigationScope.ExtendChildrenCursorAreaTop = value;
			}
		}
	}

	public float ExtendChildrenCursorAreaBottom
	{
		get
		{
			return NavigationScope.ExtendChildrenCursorAreaBottom;
		}
		set
		{
			if (value != NavigationScope.ExtendChildrenCursorAreaBottom)
			{
				NavigationScope.ExtendChildrenCursorAreaBottom = value;
			}
		}
	}

	public float DiscoveryAreaOffsetX
	{
		get
		{
			return NavigationScope.DiscoveryAreaOffsetX;
		}
		set
		{
			if (value != NavigationScope.DiscoveryAreaOffsetX)
			{
				NavigationScope.DiscoveryAreaOffsetX = value;
			}
		}
	}

	public float DiscoveryAreaOffsetY
	{
		get
		{
			return NavigationScope.DiscoveryAreaOffsetY;
		}
		set
		{
			if (value != NavigationScope.DiscoveryAreaOffsetY)
			{
				NavigationScope.DiscoveryAreaOffsetY = value;
			}
		}
	}

	public bool IsScopeEnabled
	{
		get
		{
			return NavigationScope.IsEnabled;
		}
		set
		{
			if (value != NavigationScope.IsEnabled)
			{
				NavigationScope.IsEnabled = value;
			}
		}
	}

	public bool IsScopeDisabled
	{
		get
		{
			return NavigationScope.IsDisabled;
		}
		set
		{
			if (value != NavigationScope.IsDisabled)
			{
				NavigationScope.IsDisabled = value;
			}
		}
	}

	public string UpNavigationScope
	{
		get
		{
			return NavigationScope.UpNavigationScopeID;
		}
		set
		{
			if (value != NavigationScope.UpNavigationScopeID)
			{
				NavigationScope.UpNavigationScopeID = value;
			}
		}
	}

	public string RightNavigationScope
	{
		get
		{
			return NavigationScope.RightNavigationScopeID;
		}
		set
		{
			if (value != NavigationScope.RightNavigationScopeID)
			{
				NavigationScope.RightNavigationScopeID = value;
			}
		}
	}

	public string DownNavigationScope
	{
		get
		{
			return NavigationScope.DownNavigationScopeID;
		}
		set
		{
			if (value != NavigationScope.DownNavigationScopeID)
			{
				NavigationScope.DownNavigationScopeID = value;
			}
		}
	}

	public string LeftNavigationScope
	{
		get
		{
			return NavigationScope.LeftNavigationScopeID;
		}
		set
		{
			if (value != NavigationScope.LeftNavigationScopeID)
			{
				NavigationScope.LeftNavigationScopeID = value;
			}
		}
	}

	public NavigationScopeTargeter UpNavigationScopeTargeter
	{
		get
		{
			return _upNavigationScopeTargeter;
		}
		set
		{
			if (value != _upNavigationScopeTargeter)
			{
				_upNavigationScopeTargeter = value;
				NavigationScope.UpNavigationScope = value.NavigationScope;
			}
		}
	}

	public NavigationScopeTargeter RightNavigationScopeTargeter
	{
		get
		{
			return _rightNavigationScopeTargeter;
		}
		set
		{
			if (value != _rightNavigationScopeTargeter)
			{
				_rightNavigationScopeTargeter = value;
				NavigationScope.RightNavigationScope = value.NavigationScope;
			}
		}
	}

	public NavigationScopeTargeter DownNavigationScopeTargeter
	{
		get
		{
			return _downNavigationScopeTargeter;
		}
		set
		{
			if (value != _downNavigationScopeTargeter)
			{
				_downNavigationScopeTargeter = value;
				NavigationScope.DownNavigationScope = value.NavigationScope;
			}
		}
	}

	public NavigationScopeTargeter LeftNavigationScopeTargeter
	{
		get
		{
			return _leftNavigationScopeTargeter;
		}
		set
		{
			if (value != _leftNavigationScopeTargeter)
			{
				_leftNavigationScopeTargeter = value;
				NavigationScope.LeftNavigationScope = value.NavigationScope;
			}
		}
	}

	public Widget ScopeParent
	{
		get
		{
			return NavigationScope.ParentWidget;
		}
		set
		{
			if (NavigationScope.ParentWidget != value)
			{
				if (NavigationScope.ParentWidget != null)
				{
					base.GamepadNavigationContext.RemoveNavigationScope(NavigationScope);
				}
				NavigationScope.ParentWidget = value;
				NavigationScope.ParentWidget.EventFire += OnParentConnectedToTheRoot;
				base.GamepadNavigationContext.AddNavigationScope(NavigationScope, initialize: false);
			}
		}
	}

	public NavigationScopeTargeter(UIContext context)
		: base(context)
	{
		NavigationScope = new GamepadNavigationScope();
		base.WidthSizePolicy = SizePolicy.Fixed;
		base.HeightSizePolicy = SizePolicy.Fixed;
		base.SuggestedHeight = 0f;
		base.SuggestedWidth = 0f;
		base.IsVisible = false;
	}

	private void OnParentConnectedToTheRoot(Widget widget, string eventName, object[] arguments)
	{
		if (eventName == "ConnectedToRoot" && !base.GamepadNavigationContext.HasNavigationScope(NavigationScope))
		{
			base.GamepadNavigationContext.AddNavigationScope(NavigationScope, initialize: false);
		}
	}
}
