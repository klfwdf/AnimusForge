using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

// Token: 0x02000006 RID: 6
public class GauntletGamepadNavigationContext : IGamepadNavigationContext
{
	// Token: 0x06000029 RID: 41 RVA: 0x0000209B File Offset: 0x0000029B
	private GauntletGamepadNavigationContext()
	{
	}

	// Token: 0x0600002A RID: 42 RVA: 0x000020A3 File Offset: 0x000002A3
	public GauntletGamepadNavigationContext(Func<Vector2, bool> onGetIsBlockedAtPosition, Func<int> onGetLastScreenOrder, Func<bool> onGetIsAvailableForGamepadNavigation)
	{
		this.OnGetIsBlockedAtPosition = onGetIsBlockedAtPosition;
		this.OnGetLastScreenOrder = onGetLastScreenOrder;
		this.OnGetIsAvailableForGamepadNavigation = onGetIsAvailableForGamepadNavigation;
	}

	// Token: 0x0600002B RID: 43 RVA: 0x000020C0 File Offset: 0x000002C0
	void IGamepadNavigationContext.OnFinalize()
	{
		GauntletGamepadNavigationManager.Instance.OnContextFinalized(this);
	}

	// Token: 0x0600002C RID: 44 RVA: 0x000020CD File Offset: 0x000002CD
	bool IGamepadNavigationContext.GetIsBlockedAtPosition(Vector2 position)
	{
		Func<Vector2, bool> onGetIsBlockedAtPosition = this.OnGetIsBlockedAtPosition;
		return onGetIsBlockedAtPosition == null || onGetIsBlockedAtPosition(position);
	}

	// Token: 0x0600002D RID: 45 RVA: 0x000020E1 File Offset: 0x000002E1
	int IGamepadNavigationContext.GetLastScreenOrder()
	{
		Func<int> onGetLastScreenOrder = this.OnGetLastScreenOrder;
		if (onGetLastScreenOrder == null)
		{
			return -1;
		}
		return onGetLastScreenOrder();
	}

	// Token: 0x0600002E RID: 46 RVA: 0x000020F4 File Offset: 0x000002F4
	bool IGamepadNavigationContext.IsAvailableForNavigation()
	{
		Func<bool> onGetIsAvailableForGamepadNavigation = this.OnGetIsAvailableForGamepadNavigation;
		return onGetIsAvailableForGamepadNavigation != null && onGetIsAvailableForGamepadNavigation();
	}

	// Token: 0x0600002F RID: 47 RVA: 0x00002107 File Offset: 0x00000307
	void IGamepadNavigationContext.OnWidgetUsedNavigationMovementsUpdated(Widget widget)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnWidgetUsedNavigationMovementsUpdated(widget);
	}

	// Token: 0x06000030 RID: 48 RVA: 0x00002119 File Offset: 0x00000319
	void IGamepadNavigationContext.OnGainNavigation()
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnContextGainedNavigation(this);
	}

	// Token: 0x06000031 RID: 49 RVA: 0x0000212B File Offset: 0x0000032B
	void IGamepadNavigationContext.GainNavigationAfterFrames(int frameCount, Func<bool> predicate)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.SetContextNavigationGainAfterFrames(this, frameCount, predicate);
	}

	// Token: 0x06000032 RID: 50 RVA: 0x0000213F File Offset: 0x0000033F
	void IGamepadNavigationContext.GainNavigationAfterTime(float seconds, Func<bool> predicate)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.SetContextNavigationGainAfterTime(this, seconds, predicate);
	}

	// Token: 0x06000033 RID: 51 RVA: 0x00002153 File Offset: 0x00000353
	void IGamepadNavigationContext.OnWidgetNavigationStatusChanged(Widget widget)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnWidgetNavigationStatusChanged(this, widget);
	}

	// Token: 0x06000034 RID: 52 RVA: 0x00002166 File Offset: 0x00000366
	void IGamepadNavigationContext.OnWidgetNavigationIndexUpdated(Widget widget)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnWidgetNavigationIndexUpdated(this, widget);
	}

	// Token: 0x06000035 RID: 53 RVA: 0x00002179 File Offset: 0x00000379
	void IGamepadNavigationContext.AddNavigationScope(GamepadNavigationScope scope, bool initialize)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.AddNavigationScope(this, scope, initialize);
	}

	// Token: 0x06000036 RID: 54 RVA: 0x0000218D File Offset: 0x0000038D
	void IGamepadNavigationContext.RemoveNavigationScope(GamepadNavigationScope scope)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.RemoveNavigationScope(this, scope);
	}

	// Token: 0x06000037 RID: 55 RVA: 0x000021A0 File Offset: 0x000003A0
	void IGamepadNavigationContext.AddForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.AddForcedScopeCollection(collection);
	}

	// Token: 0x06000038 RID: 56 RVA: 0x000021B2 File Offset: 0x000003B2
	void IGamepadNavigationContext.RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.RemoveForcedScopeCollection(collection);
	}

	// Token: 0x06000039 RID: 57 RVA: 0x000021C4 File Offset: 0x000003C4
	bool IGamepadNavigationContext.HasNavigationScope(GamepadNavigationScope scope)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		return instance != null && instance.HasNavigationScope(this, scope);
	}

	// Token: 0x0600003A RID: 58 RVA: 0x000021D8 File Offset: 0x000003D8
	bool IGamepadNavigationContext.HasNavigationScope(Func<GamepadNavigationScope, bool> predicate)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		return instance != null && instance.HasNavigationScope(this, predicate);
	}

	// Token: 0x0600003B RID: 59 RVA: 0x000021EC File Offset: 0x000003EC
	void IGamepadNavigationContext.OnMovieLoaded(string movieName)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnMovieLoaded(this, movieName);
	}

	// Token: 0x0600003C RID: 60 RVA: 0x000021FF File Offset: 0x000003FF
	void IGamepadNavigationContext.OnMovieReleased(string movieName)
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnMovieReleased(this, movieName);
	}

	// Token: 0x04000001 RID: 1
	public readonly Func<Vector2, bool> OnGetIsBlockedAtPosition;

	// Token: 0x04000002 RID: 2
	public readonly Func<int> OnGetLastScreenOrder;

	// Token: 0x04000003 RID: 3
	public readonly Func<bool> OnGetIsAvailableForGamepadNavigation;
}
