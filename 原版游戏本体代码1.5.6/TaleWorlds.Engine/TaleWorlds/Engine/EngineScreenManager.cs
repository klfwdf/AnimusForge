using TaleWorlds.DotNet;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.Engine;

internal class EngineScreenManager
{
	private static NativeArrayEnumerator<int> _lastPressedKeys;

	static EngineScreenManager()
	{
	}

	[EngineCallback(null, false)]
	internal static void PreTick(float dt)
	{
		ScreenManager.EarlyUpdate(EngineApplicationInterface.IScreen.GetUsableAreaPercentages());
	}

	[EngineCallback(null, false)]
	public static void Tick(float dt)
	{
		ScreenManager.Tick(dt);
	}

	[EngineCallback(null, false)]
	internal static void LateTick(float dt)
	{
		ScreenManager.LateTick(dt);
	}

	[EngineCallback(null, false)]
	internal static void OnOnscreenKeyboardDone(string inputText)
	{
		ScreenManager.OnOnscreenKeyboardDone(inputText);
	}

	[EngineCallback(null, false)]
	internal static void OnOnscreenKeyboardCanceled()
	{
		ScreenManager.OnOnscreenKeyboardCanceled();
	}

	[EngineCallback(null, false)]
	internal static void OnGameWindowFocusChange(bool focusGained)
	{
		ScreenManager.OnGameWindowFocusChange(focusGained);
	}

	[EngineCallback(null, false)]
	internal static void Update()
	{
		ScreenManager.Update(_lastPressedKeys);
	}

	[EngineCallback(null, false)]
	internal static void InitializeLastPressedKeys(NativeArray lastKeysPressed)
	{
		_lastPressedKeys = new NativeArrayEnumerator<int>(lastKeysPressed);
	}

	internal static void Initialize()
	{
		ScreenManager.Initialize(new ScreenManagerEngineConnection());
	}
}
