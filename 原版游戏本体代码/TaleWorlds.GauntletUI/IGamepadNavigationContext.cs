using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

// Token: 0x02000004 RID: 4
public interface IGamepadNavigationContext
{
	// Token: 0x06000003 RID: 3
	void OnFinalize();

	// Token: 0x06000004 RID: 4
	bool GetIsBlockedAtPosition(Vector2 position);

	// Token: 0x06000005 RID: 5
	int GetLastScreenOrder();

	// Token: 0x06000006 RID: 6
	bool IsAvailableForNavigation();

	// Token: 0x06000007 RID: 7
	void OnWidgetUsedNavigationMovementsUpdated(Widget widget);

	// Token: 0x06000008 RID: 8
	void OnGainNavigation();

	// Token: 0x06000009 RID: 9
	void GainNavigationAfterFrames(int frameCount, Func<bool> predicate);

	// Token: 0x0600000A RID: 10
	void GainNavigationAfterTime(float seconds, Func<bool> predicate);

	// Token: 0x0600000B RID: 11
	void OnWidgetNavigationStatusChanged(Widget widget);

	// Token: 0x0600000C RID: 12
	void OnWidgetNavigationIndexUpdated(Widget widget);

	// Token: 0x0600000D RID: 13
	void AddNavigationScope(GamepadNavigationScope scope, bool initialize);

	// Token: 0x0600000E RID: 14
	void RemoveNavigationScope(GamepadNavigationScope scope);

	// Token: 0x0600000F RID: 15
	void AddForcedScopeCollection(GamepadNavigationForcedScopeCollection collection);

	// Token: 0x06000010 RID: 16
	void RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection);

	// Token: 0x06000011 RID: 17
	bool HasNavigationScope(GamepadNavigationScope scope);

	// Token: 0x06000012 RID: 18
	bool HasNavigationScope(Func<GamepadNavigationScope, bool> predicate);

	// Token: 0x06000013 RID: 19
	void OnMovieLoaded(string movieName);

	// Token: 0x06000014 RID: 20
	void OnMovieReleased(string movieName);
}
