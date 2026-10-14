using System;
using System.IO;
using Newtonsoft.Json;

namespace AnimusForge;

public sealed class AnimusForgeTerminalSettingsData
{
	public bool IsHotkeyEnabled { get; set; } = true;
	public bool IsMapIconEnabled { get; set; } = true;
}

public static class AnimusForgeTerminalSettings
{
    public static bool IsHotkeyEnabled
    {
        get
        {
            var settings = DuelSettings.GetSettings();
            return settings == null || settings.TerminalHotkeyEnabled || !settings.TerminalMapIconEnabled;
        }
        set => SetEntry(value, true);
    }

    public static bool IsMapIconEnabled
    {
        get => DuelSettings.GetSettings()?.TerminalMapIconEnabled ?? true;
        set => SetEntry(value, false);
    }

    private static void SetEntry(bool value, bool hotkey)
    {
        var settings = DuelSettings.GetSettings();
        if (settings == null) return;
        bool previous = hotkey ? settings.TerminalHotkeyEnabled : settings.TerminalMapIconEnabled;
        if (previous == value) return;
        if (!value && !(hotkey ? settings.TerminalMapIconEnabled : settings.TerminalHotkeyEnabled)) return;
        if (hotkey) settings.TerminalHotkeyEnabled = value;
        else settings.TerminalMapIconEnabled = value;
        if (!DuelSettings.SaveCurrentSettings())
        {
            if (hotkey) settings.TerminalHotkeyEnabled = previous;
            else settings.TerminalMapIconEnabled = previous;
        }
    }

    internal static string GetSettingsPath() => McmSettingsStorage.GetSharedSettingsFilePath();

    // Retained only for reading/validating the old module entry file and its
    // atomic compatibility fixtures. Active entries save via the MCM owner.
	internal static bool TryLoadSettingsFile(string path, out AnimusForgeTerminalSettingsData data)
	{
		data = null;
		AnimusForgeDataPaths.ValidatePath(path);
		if (!File.Exists(path))
		{
			return false;
		}
		if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
		{
			throw new InvalidOperationException("TerminalSettings file is a reparse point.");
		}
		data = JsonConvert.DeserializeObject<AnimusForgeTerminalSettingsData>(new System.Text.UTF8Encoding(false, true).GetString(File.ReadAllBytes(path)).TrimStart('\uFEFF'));
		return data != null;
	}

	internal static void TrySaveSettingsFile(string path, AnimusForgeTerminalSettingsData data)
	{
		AnimusForgeDataPaths.ValidatePath(path);
		string directory = Path.GetDirectoryName(path);
		Directory.CreateDirectory(directory);
		AnimusForgeDataPaths.ValidatePath(path);
		if ((new DirectoryInfo(directory).Attributes & FileAttributes.ReparsePoint) != 0)
		{
			throw new InvalidOperationException("TerminalSettings directory is a reparse point.");
		}
		if (File.Exists(path))
		{
			if (!TryLoadSettingsFile(path, out _))
			{
				throw new InvalidDataException("Existing TerminalSettings is invalid; refusing to overwrite it.");
			}
		}
		string candidate = Path.Combine(directory, ".afp-" + Guid.NewGuid().ToString("N"));
		try
		{
			File.WriteAllText(candidate, JsonConvert.SerializeObject(data, Formatting.Indented));
			if (!TryLoadSettingsFile(candidate, out _))
			{
				throw new InvalidDataException("TerminalSettings candidate is invalid.");
			}
			AnimusForgeDataPaths.ValidatePath(path);
			if (File.Exists(path))
			{
				File.Replace(candidate, path, null);
			}
			else
			{
				File.Move(candidate, path);
			}
		}
		finally
		{
			if (File.Exists(candidate)) File.Delete(candidate);
		}
	}

}
