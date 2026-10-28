using Bannerlord.ButterLib.Implementation.SaveSystem.Patches;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubSystems;
using HarmonyLib;

namespace Bannerlord.ButterLib.Implementation.SaveSystem;

internal class SaveSystemSubSystem : ISubSystem
{
	private readonly Harmony _harmony = new Harmony("Bannerlord.ButterLib.SaveSystem");

	private bool _wasInitialized;

	public static SaveSystemSubSystem? Instance { get; private set; }

	public string Id => "Save System";

	public string Name => "{=66A5N9278w}Save System";

	public string Description => "{=9ybOxGpWb5}Extends and fixes the game's save system:{NL}\n* Fixes possible collision with save names;{NL}\n* Fixes save corruption & crashes when duplicate types are defined;{NL}\n* Adds support for saving many more container types;{NL}\nThis might alter the save file, disabling the feature might render the save file unloadable!";

	public bool IsEnabled { get; private set; }

	public bool CanBeDisabled => true;

	public bool CanBeSwitchedAtRuntime => false;

	public SaveSystemSubSystem()
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
			BehaviourNamePatch.Enable(_harmony);
			TypeExtensionsPatch.Enable(_harmony);
			DefinitionContextPatch.Enable(_harmony);
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			IsEnabled = false;
			BehaviourNamePatch.Disable(_harmony);
			TypeExtensionsPatch.Disable(_harmony);
			DefinitionContextPatch.Disable(_harmony);
		}
	}
}
