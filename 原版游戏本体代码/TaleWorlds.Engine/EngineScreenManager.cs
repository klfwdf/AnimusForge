using System;
using TaleWorlds.DotNet;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine
{
	// Token: 0x02000045 RID: 69
	internal class EngineScreenManager
	{
		// Token: 0x060006CC RID: 1740 RVA: 0x0000411A File Offset: 0x0000231A
		[EngineCallback(null, false)]
		internal static void PreTick(float dt)
		{
			ScreenManager.EarlyUpdate(EngineApplicationInterface.IScreen.GetUsableAreaPercentages());
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x0000412B File Offset: 0x0000232B
		[EngineCallback(null, false)]
		public static void Tick(float dt)
		{
			ScreenManager.Tick(dt);
		}

		// Token: 0x060006CE RID: 1742 RVA: 0x00004133 File Offset: 0x00002333
		[EngineCallback(null, false)]
		internal static void LateTick(float dt)
		{
			ScreenManager.LateTick(dt);
		}

		// Token: 0x060006CF RID: 1743 RVA: 0x0000413B File Offset: 0x0000233B
		[EngineCallback(null, false)]
		internal static void OnOnscreenKeyboardDone(string inputText)
		{
			ScreenManager.OnOnscreenKeyboardDone(inputText);
		}

		// Token: 0x060006D0 RID: 1744 RVA: 0x00004143 File Offset: 0x00002343
		[EngineCallback(null, false)]
		internal static void OnOnscreenKeyboardCanceled()
		{
			ScreenManager.OnOnscreenKeyboardCanceled();
		}

		// Token: 0x060006D1 RID: 1745 RVA: 0x0000414A File Offset: 0x0000234A
		[EngineCallback(null, false)]
		internal static void OnGameWindowFocusChange(bool focusGained)
		{
			ScreenManager.OnGameWindowFocusChange(focusGained);
		}

		// Token: 0x060006D2 RID: 1746 RVA: 0x00004152 File Offset: 0x00002352
		[EngineCallback(null, false)]
		internal static void Update()
		{
			ScreenManager.Update(EngineScreenManager._lastPressedKeys);
		}

		// Token: 0x060006D3 RID: 1747 RVA: 0x0000415E File Offset: 0x0000235E
		[EngineCallback(null, false)]
		internal static void InitializeLastPressedKeys(NativeArray lastKeysPressed)
		{
			EngineScreenManager._lastPressedKeys = new NativeArrayEnumerator<int>(lastKeysPressed);
		}

		// Token: 0x060006D4 RID: 1748 RVA: 0x0000416B File Offset: 0x0000236B
		internal static void Initialize()
		{
			ScreenManager.Initialize(new ScreenManagerEngineConnection());
		}

		// Token: 0x0400005A RID: 90
		private static NativeArrayEnumerator<int> _lastPressedKeys;
	}
}
