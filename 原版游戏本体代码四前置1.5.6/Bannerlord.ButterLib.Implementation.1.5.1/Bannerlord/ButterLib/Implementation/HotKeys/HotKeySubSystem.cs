using Bannerlord.ButterLib.HotKeys;
using Bannerlord.ButterLib.Implementation.HotKeys.Patches;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubSystems;
using HarmonyLib;
using TaleWorlds.InputSystem;

namespace Bannerlord.ButterLib.Implementation.HotKeys;

internal sealed class HotKeySubSystem : ISubSystem
{
	private readonly Harmony _harmony = new Harmony("Bannerlord.ButterLib.HotKeySystem");

	private bool _wasInitialized;

	public static HotKeySubSystem? Instance { get; private set; }

	public string Id => "Hot Keys";

	public string Name => "{=jvV6QDc7AJ}Hot Keys";

	public bool IsEnabled { get; private set; }

	public string Description => "{=wNNYwgUeus}Mod Developer feature! Provides a better way for mods to create hotkeys";

	public bool CanBeDisabled => true;

	public bool CanBeSwitchedAtRuntime => false;

	public HotKeySubSystem()
	{
		Instance = this;
	}

	public void Enable()
	{
		if (!_wasInitialized)
		{
			_wasInitialized = true;
			if (!(SettingsProvider.PopulateSubSystemSettings(this) ?? true))
			{
				return;
			}
		}
		if (!IsEnabled)
		{
			IsEnabled = true;
			ButterLibSubModule instance = ButterLibSubModule.Instance;
			if (instance != null)
			{
				instance.OnApplicationTickEvent += OnApplicationTick;
			}
			OptionsProviderPatches.Enable(_harmony);
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			IsEnabled = false;
			ButterLibSubModule instance = ButterLibSubModule.Instance;
			if (instance != null)
			{
				instance.OnApplicationTickEvent -= OnApplicationTick;
			}
			OptionsProviderPatches.Disable(_harmony);
		}
	}

	private static void OnApplicationTick(float dt)
	{
		for (int i = 0; i < HotKeyManagerImplementation.GlobalHotKeyStorage.Count; i++)
		{
			HotKeyBase hotKeyBase = HotKeyManagerImplementation.GlobalHotKeyStorage[i];
			if (!hotKeyBase.ShouldExecute() || hotKeyBase.GameKey == null)
			{
				continue;
			}
			Key keyboardKey = hotKeyBase.GameKey.KeyboardKey;
			if ((object)keyboardKey == null || !keyboardKey.InputKey.IsPressed())
			{
				Key controllerKey = hotKeyBase.GameKey.ControllerKey;
				if ((object)controllerKey == null || !controllerKey.InputKey.IsPressed())
				{
					goto IL_006b;
				}
			}
			hotKeyBase.OnPressedInternal();
			goto IL_006b;
			IL_00ad:
			Key keyboardKey2 = hotKeyBase.GameKey.KeyboardKey;
			if ((object)keyboardKey2 == null || !keyboardKey2.InputKey.IsReleased())
			{
				Key controllerKey2 = hotKeyBase.GameKey.ControllerKey;
				if ((object)controllerKey2 == null || !controllerKey2.InputKey.IsReleased())
				{
					continue;
				}
			}
			hotKeyBase.OnReleasedInternal();
			continue;
			IL_006b:
			Key keyboardKey3 = hotKeyBase.GameKey.KeyboardKey;
			if ((object)keyboardKey3 == null || !keyboardKey3.InputKey.IsDown())
			{
				Key controllerKey3 = hotKeyBase.GameKey.ControllerKey;
				if ((object)controllerKey3 == null || !controllerKey3.InputKey.IsDown())
				{
					goto IL_00ad;
				}
			}
			hotKeyBase.IsDownInternal();
			goto IL_00ad;
		}
	}
}
