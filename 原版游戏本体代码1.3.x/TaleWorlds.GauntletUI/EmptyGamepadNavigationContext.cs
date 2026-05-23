using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

// Token: 0x02000005 RID: 5
public class EmptyGamepadNavigationContext : IGamepadNavigationContext
{
	// Token: 0x06000015 RID: 21 RVA: 0x00002058 File Offset: 0x00000258
	public void AddForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
	}

	// Token: 0x06000016 RID: 22 RVA: 0x0000205A File Offset: 0x0000025A
	public void AddNavigationScope(GamepadNavigationScope scope, bool initialize)
	{
	}

	// Token: 0x06000017 RID: 23 RVA: 0x0000205C File Offset: 0x0000025C
	public void GainNavigationAfterFrames(int frameCount, Func<bool> predicate)
	{
	}

	// Token: 0x06000018 RID: 24 RVA: 0x0000205E File Offset: 0x0000025E
	public void GainNavigationAfterTime(float seconds, Func<bool> predicate)
	{
	}

	// Token: 0x06000019 RID: 25 RVA: 0x00002060 File Offset: 0x00000260
	public void OnFinalize()
	{
		GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.OnContextFinalized(this);
	}

	// Token: 0x0600001A RID: 26 RVA: 0x00002072 File Offset: 0x00000272
	public bool GetIsBlockedAtPosition(Vector2 position)
	{
		return true;
	}

	// Token: 0x0600001B RID: 27 RVA: 0x00002075 File Offset: 0x00000275
	public int GetLastScreenOrder()
	{
		return -1;
	}

	// Token: 0x0600001C RID: 28 RVA: 0x00002078 File Offset: 0x00000278
	public bool HasNavigationScope(GamepadNavigationScope scope)
	{
		return false;
	}

	// Token: 0x0600001D RID: 29 RVA: 0x0000207B File Offset: 0x0000027B
	public bool HasNavigationScope(Func<GamepadNavigationScope, bool> predicate)
	{
		return false;
	}

	// Token: 0x0600001E RID: 30 RVA: 0x0000207E File Offset: 0x0000027E
	public bool IsAvailableForNavigation()
	{
		return false;
	}

	// Token: 0x0600001F RID: 31 RVA: 0x00002081 File Offset: 0x00000281
	public void OnGainNavigation()
	{
	}

	// Token: 0x06000020 RID: 32 RVA: 0x00002083 File Offset: 0x00000283
	public void OnMovieLoaded(string movieName)
	{
	}

	// Token: 0x06000021 RID: 33 RVA: 0x00002085 File Offset: 0x00000285
	public void OnMovieReleased(string movieName)
	{
	}

	// Token: 0x06000022 RID: 34 RVA: 0x00002087 File Offset: 0x00000287
	public void OnWidgetNavigationIndexUpdated(Widget widget)
	{
	}

	// Token: 0x06000023 RID: 35 RVA: 0x00002089 File Offset: 0x00000289
	public void OnWidgetNavigationStatusChanged(Widget widget)
	{
	}

	// Token: 0x06000024 RID: 36 RVA: 0x0000208B File Offset: 0x0000028B
	public void OnWidgetUsedNavigationMovementsUpdated(Widget widget)
	{
	}

	// Token: 0x06000025 RID: 37 RVA: 0x0000208D File Offset: 0x0000028D
	public void RemoveForcedScopeCollection(GamepadNavigationForcedScopeCollection collection)
	{
	}

	// Token: 0x06000026 RID: 38 RVA: 0x0000208F File Offset: 0x0000028F
	public void RemoveNavigationScope(GamepadNavigationScope scope)
	{
	}
}
