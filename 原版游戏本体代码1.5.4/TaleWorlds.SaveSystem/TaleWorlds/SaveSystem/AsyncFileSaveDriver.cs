using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public class AsyncFileSaveDriver : ISaveDriver
{
	private FileDriver _saveDriver;

	private Task<SaveResultWithMessage> _currentSaveTask;

	private string _currentSaveName;

	public AsyncFileSaveDriver()
	{
		_saveDriver = new FileDriver();
	}

	private void WaitPreviousSave()
	{
		Task<SaveResultWithMessage> currentSaveTask = _currentSaveTask;
		if (currentSaveTask != null && !currentSaveTask.IsCompleted)
		{
			using (new PerformanceTestBlock("AsyncFileSaveDriver::Save - waiting previous save"))
			{
				currentSaveTask.Wait();
			}
		}
	}

	private void WaitPreviousSaveOfFile(string saveName)
	{
		if (_currentSaveName == saveName)
		{
			WaitPreviousSave();
		}
	}

	Task<SaveResultWithMessage> ISaveDriver.Save(string saveName, int version, MetaData metaData, GameData gameData)
	{
		WaitPreviousSave();
		_currentSaveName = saveName;
		_currentSaveTask = Task.Run(() => _saveDriver.Save(saveName, version, metaData, gameData));
		return _currentSaveTask;
	}

	SaveGameFileInfo[] ISaveDriver.GetSaveGameFileInfos()
	{
		return _saveDriver.GetSaveGameFileInfos();
	}

	string[] ISaveDriver.GetSaveGameFileNames()
	{
		return _saveDriver.GetSaveGameFileNames();
	}

	MetaData ISaveDriver.LoadMetaData(string saveName)
	{
		WaitPreviousSaveOfFile(saveName);
		return _saveDriver.LoadMetaData(saveName);
	}

	LoadData ISaveDriver.Load(string saveName)
	{
		WaitPreviousSaveOfFile(saveName);
		return _saveDriver.Load(saveName);
	}

	bool ISaveDriver.Delete(string saveName)
	{
		WaitPreviousSave();
		return _saveDriver.Delete(saveName);
	}

	bool ISaveDriver.IsSaveGameFileExists(string saveName)
	{
		WaitPreviousSaveOfFile(saveName);
		return _saveDriver.IsSaveGameFileExists(saveName);
	}

	bool ISaveDriver.IsWorkingAsync()
	{
		return true;
	}
}
