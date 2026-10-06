using System;
using System.IO;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public class SaveDirectoryWatcher : IDisposable
{
	private FileSystemWatcher _watcher;

	private volatile bool _hasUnhandledChange;

	public SaveDirectoryWatcher()
	{
		string saveDirectoryPath = GetSaveDirectoryPath();
		if (!Directory.Exists(saveDirectoryPath))
		{
			return;
		}
		try
		{
			_watcher = new FileSystemWatcher
			{
				Path = saveDirectoryPath,
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.LastWrite),
				Filter = "*.sav",
				EnableRaisingEvents = true
			};
			_watcher.Changed += OnSaveDirectoryChanged;
			_watcher.Created += OnSaveDirectoryChanged;
			_watcher.Deleted += OnSaveDirectoryChanged;
			_watcher.Renamed += OnSaveDirectoryChanged;
		}
		catch (Exception ex)
		{
			Debug.Print("Could not watch the save directory for changes: " + ex);
			Dispose();
		}
	}

	public bool HasAnyChanges()
	{
		if (!_hasUnhandledChange)
		{
			return false;
		}
		_hasUnhandledChange = false;
		return true;
	}

	public void Dispose()
	{
		_watcher?.Dispose();
		_watcher = null;
	}

	private static string GetSaveDirectoryPath()
	{
		return Path.GetDirectoryName(FileDriver.GetSaveFilePath("any.sav").FileFullPath);
	}

	private void OnSaveDirectoryChanged(object sender, FileSystemEventArgs args)
	{
		_hasUnhandledChange = true;
	}
}
