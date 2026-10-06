using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

public class EmptyGamepadNavigationContext : IGamepadNavigationContext
{
	public void AddForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
	}

	public void AddNavigationScope(GamepadNavigationScope scope, bool initialize)
	{
	}

	public void GainNavigationAfterFrames(int frameCount, Func<bool> predicate)
	{
	}

	public void GainNavigationAfterTime(float seconds, Func<bool> predicate)
	{
	}

	public void OnFinalize()
	{
		GauntletGamepadNavigationManager.Instance?.OnContextFinalized(this);
	}

	public bool GetIsBlockedAtPosition(Vector2 position)
	{
		return true;
	}

	public int GetLastScreenOrder()
	{
		return -1;
	}

	public bool HasNavigationScope(GamepadNavigationScope scope)
	{
		return false;
	}

	public bool HasNavigationScope(Func<GamepadNavigationScope, bool> predicate)
	{
		return false;
	}

	public bool IsAvailableForNavigation()
	{
		return false;
	}

	public void OnGainNavigation()
	{
	}

	public void OnMovieLoaded(string movieName)
	{
	}

	public void OnMovieReleased(string movieName)
	{
	}

	public void OnWidgetNavigationIndexUpdated(Widget widget)
	{
	}

	public void OnWidgetNavigationStatusChanged(Widget widget)
	{
	}

	public void OnWidgetUsedNavigationMovementsUpdated(Widget widget)
	{
	}

	public void RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
	}

	public void RemoveNavigationScope(GamepadNavigationScope scope)
	{
	}
}
