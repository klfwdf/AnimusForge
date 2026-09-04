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
	private static readonly object _lock = new object();
	private static bool _loaded = false;
	private static bool _isHotkeyEnabled = true;
	private static bool _isMapIconEnabled = true;

	private const string SettingsFileName = "TerminalSettings.json";

	public static bool IsHotkeyEnabled
	{
		get
		{
			EnsureLoaded();
			return _isHotkeyEnabled;
		}
		set
		{
			EnsureLoaded();
			if (_isHotkeyEnabled == value)
			{
				return;
			}
			_isHotkeyEnabled = value;
			Save();
		}
	}

	public static bool IsMapIconEnabled
	{
		get
		{
			EnsureLoaded();
			return _isMapIconEnabled;
		}
		set
		{
			EnsureLoaded();
			if (_isMapIconEnabled == value)
			{
				return;
			}
			_isMapIconEnabled = value;
			Save();
		}
	}

	private static void EnsureLoaded()
	{
		if (_loaded)
		{
			return;
		}
		lock (_lock)
		{
			if (_loaded)
			{
				return;
			}
			LoadInternal();
			_loaded = true;
		}
	}

	private static void LoadInternal()
	{
		try
		{
			string path = AnimusForgeModulePaths.GetModuleDataFilePath(SettingsFileName);
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				AnimusForgeTerminalSettingsData data = JsonConvert.DeserializeObject<AnimusForgeTerminalSettingsData>(json);
				if (data != null)
				{
					_isHotkeyEnabled = data.IsHotkeyEnabled;
					_isMapIconEnabled = data.IsMapIconEnabled;
					// 防死锁规则：如果被外部文件修改导致两个都被关闭，强制恢复按键开启
					if (!_isHotkeyEnabled && !_isMapIconEnabled)
					{
						_isHotkeyEnabled = true;
					}
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TerminalSettings", "[WARN] Failed to load TerminalSettings: " + ex.Message);
		}
		_isHotkeyEnabled = true;
		_isMapIconEnabled = true;
	}

	private static void Save()
	{
		lock (_lock)
		{
			try
			{
				string path = AnimusForgeModulePaths.GetModuleDataFilePath(SettingsFileName);
				string dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AnimusForgeTerminalSettingsData data = new AnimusForgeTerminalSettingsData
				{
					IsHotkeyEnabled = _isHotkeyEnabled,
					IsMapIconEnabled = _isMapIconEnabled
				};
				string json = JsonConvert.SerializeObject(data, Formatting.Indented);
				File.WriteAllText(path, json);
			}
			catch (Exception ex)
			{
				Logger.Log("TerminalSettings", "[WARN] Failed to save TerminalSettings: " + ex.Message);
			}
		}
	}
}
