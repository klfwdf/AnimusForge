using System.IO;

namespace TaleWorlds.Library;

public class ResourceDepotLocation
{
	public string BasePath { get; private set; }

	public string Path { get; private set; }

	public string FullPath { get; private set; }

	public FileSystemWatcher Watcher { get; private set; }

	public ResourceDepotLocation(string basePath, string path, string fullPath)
	{
		BasePath = basePath;
		Path = path;
		FullPath = fullPath;
	}

	public void StartWatchingChanges(FileSystemEventHandler onChangeEvent, RenamedEventHandler onRenameEvent)
	{
		Watcher = new FileSystemWatcher
		{
			Path = FullPath,
			NotifyFilter = (NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime),
			Filter = "*.*",
			IncludeSubdirectories = true,
			EnableRaisingEvents = true
		};
		Watcher.Changed += onChangeEvent;
		Watcher.Created += onChangeEvent;
		Watcher.Deleted += onChangeEvent;
		Watcher.Renamed += onRenameEvent;
	}

	public void StopWatchingChanges()
	{
		Watcher.Dispose();
	}
}
