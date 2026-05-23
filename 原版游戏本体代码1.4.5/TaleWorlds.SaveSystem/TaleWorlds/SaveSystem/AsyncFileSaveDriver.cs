using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public class AsyncFileSaveDriver : ISaveDriver
{
	private FileDriver _saveDriver;

	private Task _currentNonSaveTask;

	private Task<SaveResultWithMessage> _currentSaveTask;

	public AsyncFileSaveDriver()
	{
		_saveDriver = new FileDriver();
	}

	private void WaitPreviousTask()
	{
		Task currentNonSaveTask = _currentNonSaveTask;
		Task<SaveResultWithMessage> currentSaveTask = _currentSaveTask;
		if (currentNonSaveTask != null && !currentNonSaveTask.IsCompleted)
		{
			using (new PerformanceTestBlock("AsyncFileSaveDriver::Save - waiting previous save"))
			{
				currentNonSaveTask.Wait();
				return;
			}
		}
		if (currentSaveTask != null && !currentSaveTask.IsCompleted)
		{
			using (new PerformanceTestBlock("MBAsyncSaveDriver::Save - waiting previous save"))
			{
				currentSaveTask.Wait();
			}
		}
	}

	Task<SaveResultWithMessage> ISaveDriver.Save(string saveName, int version, MetaData metaData, GameData gameData)
	{
		WaitPreviousTask();
		_currentSaveTask = Task.Run(delegate
		{
			Task<SaveResultWithMessage> result = _saveDriver.Save(saveName, version, metaData, gameData);
			_currentNonSaveTask = null;
			return result;
		});
		return _currentSaveTask;
	}

	SaveGameFileInfo[] ISaveDriver.GetSaveGameFileInfos()
	{
		WaitPreviousTask();
		return _saveDriver.GetSaveGameFileInfos();
	}

	string[] ISaveDriver.GetSaveGameFileNames()
	{
		WaitPreviousTask();
		return _saveDriver.GetSaveGameFileNames();
	}

	MetaData ISaveDriver.LoadMetaData(string saveName)
	{
		WaitPreviousTask();
		return _saveDriver.LoadMetaData(saveName);
	}

	LoadData ISaveDriver.Load(string saveName)
	{
		WaitPreviousTask();
		return _saveDriver.Load(saveName);
	}

	bool ISaveDriver.Delete(string saveName)
	{
		WaitPreviousTask();
		return _saveDriver.Delete(saveName);
	}

	bool ISaveDriver.IsSaveGameFileExists(string saveName)
	{
		WaitPreviousTask();
		return _saveDriver.IsSaveGameFileExists(saveName);
	}

	bool ISaveDriver.IsWorkingAsync()
	{
		return true;
	}
}
