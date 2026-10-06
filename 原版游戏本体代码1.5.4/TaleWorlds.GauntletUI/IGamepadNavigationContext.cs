using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

public interface IGamepadNavigationContext
{
	void OnFinalize();

	bool GetIsBlockedAtPosition(Vector2 position);

	int GetLastScreenOrder();

	bool IsAvailableForNavigation();

	void OnWidgetUsedNavigationMovementsUpdated(Widget widget);

	void OnGainNavigation();

	void GainNavigationAfterFrames(int frameCount, Func<bool> predicate);

	void GainNavigationAfterTime(float seconds, Func<bool> predicate);

	void OnWidgetNavigationStatusChanged(Widget widget);

	void OnWidgetNavigationIndexUpdated(Widget widget);

	void AddNavigationScope(GamepadNavigationScope scope, bool initialize);

	void RemoveNavigationScope(GamepadNavigationScope scope);

	void AddForcedScopeCollection(GamepadNavigationForcedScopeCollection collection);

	void RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection);

	bool HasNavigationScope(GamepadNavigationScope scope);

	bool HasNavigationScope(Func<GamepadNavigationScope, bool> predicate);

	void OnMovieLoaded(string movieName);

	void OnMovieReleased(string movieName);
}
