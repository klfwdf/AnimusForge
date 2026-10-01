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
			bool previous = _isHotkeyEnabled;
			_isHotkeyEnabled = value;
			if (!Save()) _isHotkeyEnabled = previous;
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
			bool previous = _isMapIconEnabled;
			_isMapIconEnabled = value;
			if (!Save()) _isMapIconEnabled = previous;
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
			if (TryLoadSettingsFile(GetSettingsPath(), out AnimusForgeTerminalSettingsData data))
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
		catch (Exception)
		{
			Logger.Log("TerminalSettings", "[WARN] Failed to load user TerminalSettings; using built-in defaults.");
		}
		_isHotkeyEnabled = true;
		_isMapIconEnabled = true;
	}

	internal static string GetSettingsPath()
	{
		return Path.Combine(AnimusForgeDataPaths.GetSettingsDirectory(AnimusForgeDataPaths.GetCurrentRoot()), SettingsFileName);
	}

	internal static bool TryLoadSettingsFile(string path, out AnimusForgeTerminalSettingsData data)
	{
		data = null;
		if (!File.Exists(path))
		{
			return false;
		}
		if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
		{
			throw new InvalidOperationException("TerminalSettings file is a reparse point.");
		}
		data = JsonConvert.DeserializeObject<AnimusForgeTerminalSettingsData>(File.ReadAllText(path));
		return data != null;
	}

	internal static void TrySaveSettingsFile(string path, AnimusForgeTerminalSettingsData data)
	{
		string directory = Path.GetDirectoryName(path);
		Directory.CreateDirectory(directory);
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

	private static bool Save()
	{
		lock (_lock)
		{
			try
			{
				AnimusForgeTerminalSettingsData data = new AnimusForgeTerminalSettingsData
				{
					IsHotkeyEnabled = _isHotkeyEnabled,
					IsMapIconEnabled = _isMapIconEnabled
				};
				TrySaveSettingsFile(GetSettingsPath(), data);
				return true;
			}
			catch (Exception)
			{
				Logger.Log("TerminalSettings", "[WARN] Failed to save user TerminalSettings; existing file was preserved.");
				return false;
			}
		}
	}
}
