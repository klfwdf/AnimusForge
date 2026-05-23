using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

public class GauntletGamepadNavigationContext : IGamepadNavigationContext
{
	public readonly Func<Vector2, bool> OnGetIsBlockedAtPosition;

	public readonly Func<int> OnGetLastScreenOrder;

	public readonly Func<bool> OnGetIsAvailableForGamepadNavigation;

	static GauntletGamepadNavigationContext()
	{
	}

	private GauntletGamepadNavigationContext()
	{
	}

	public GauntletGamepadNavigationContext(Func<Vector2, bool> onGetIsBlockedAtPosition, Func<int> onGetLastScreenOrder, Func<bool> onGetIsAvailableForGamepadNavigation)
	{
		OnGetIsBlockedAtPosition = onGetIsBlockedAtPosition;
		OnGetLastScreenOrder = onGetLastScreenOrder;
		OnGetIsAvailableForGamepadNavigation = onGetIsAvailableForGamepadNavigation;
	}

	void IGamepadNavigationContext.OnFinalize()
	{
		GauntletGamepadNavigationManager.Instance.OnContextFinalized(this);
	}

	bool IGamepadNavigationContext.GetIsBlockedAtPosition(Vector2 position)
	{
		return OnGetIsBlockedAtPosition?.Invoke(position) ?? true;
	}

	int IGamepadNavigationContext.GetLastScreenOrder()
	{
		return OnGetLastScreenOrder?.Invoke() ?? (-1);
	}

	bool IGamepadNavigationContext.IsAvailableForNavigation()
	{
		return OnGetIsAvailableForGamepadNavigation?.Invoke() ?? false;
	}

	void IGamepadNavigationContext.OnWidgetUsedNavigationMovementsUpdated(Widget widget)
	{
		GauntletGamepadNavigationManager.Instance?.OnWidgetUsedNavigationMovementsUpdated(widget);
	}

	void IGamepadNavigationContext.OnGainNavigation()
	{
		GauntletGamepadNavigationManager.Instance?.OnContextGainedNavigation(this);
	}

	void IGamepadNavigationContext.GainNavigationAfterFrames(int frameCount, Func<bool> predicate)
	{
		GauntletGamepadNavigationManager.Instance?.SetContextNavigationGainAfterFrames(this, frameCount, predicate);
	}

	void IGamepadNavigationContext.GainNavigationAfterTime(float seconds, Func<bool> predicate)
	{
		GauntletGamepadNavigationManager.Instance?.SetContextNavigationGainAfterTime(this, seconds, predicate);
	}

	void IGamepadNavigationContext.OnWidgetNavigationStatusChanged(Widget widget)
	{
		GauntletGamepadNavigationManager.Instance?.OnWidgetNavigationStatusChanged(this, widget);
	}

	void IGamepadNavigationContext.OnWidgetNavigationIndexUpdated(Widget widget)
	{
		GauntletGamepadNavigationManager.Instance?.OnWidgetNavigationIndexUpdated(this, widget);
	}

	void IGamepadNavigationContext.AddNavigationScope(GamepadNavigationScope scope, bool initialize)
	{
		GauntletGamepadNavigationManager.Instance?.AddNavigationScope(this, scope, initialize);
	}

	void IGamepadNavigationContext.RemoveNavigationScope(GamepadNavigationScope scope)
	{
		GauntletGamepadNavigationManager.Instance?.RemoveNavigationScope(this, scope);
	}

	void IGamepadNavigationContext.AddForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
		GauntletGamepadNavigationManager.Instance?.AddForcedScopeCollection(collection);
	}

	void IGamepadNavigationContext.RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
		GauntletGamepadNavigationManager.Instance?.RemoveForcedScopeCollection(collection);
	}

	bool IGamepadNavigationContext.HasNavigationScope(GamepadNavigationScope scope)
	{
		return GauntletGamepadNavigationManager.Instance?.HasNavigationScope(this, scope) ?? false;
	}

	bool IGamepadNavigationContext.HasNavigationScope(Func<GamepadNavigationScope, bool> predicate)
	{
		return GauntletGamepadNavigationManager.Instance?.HasNavigationScope(this, predicate) ?? false;
	}

	void IGamepadNavigationContext.OnMovieLoaded(string movieName)
	{
		GauntletGamepadNavigationManager.Instance?.OnMovieLoaded(this, movieName);
	}

	void IGamepadNavigationContext.OnMovieReleased(string movieName)
	{
		GauntletGamepadNavigationManager.Instance?.OnMovieReleased(this, movieName);
	}
}
