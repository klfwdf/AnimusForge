using System.IO;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public class InMemDriver : ISaveDriver
{
	private byte[] _data;

	public Task<SaveResultWithMessage> Save(string saveName, int version, MetaData metaData, GameData gameData)
	{
		byte[] data = gameData.GetData();
		MemoryStream memoryStream = new MemoryStream();
		metaData.Add("version", version.ToString());
		metaData.Serialize(memoryStream);
		memoryStream.Write(data, 0, data.Length);
		_data = memoryStream.GetBuffer();
		return Task.FromResult(SaveResultWithMessage.Default);
	}

	public MetaData LoadMetaData(string saveName)
	{
		MemoryStream memoryStream = new MemoryStream(_data);
		MetaData result = MetaData.Deserialize(memoryStream);
		memoryStream.Close();
		return result;
	}

	public LoadData Load(string saveName)
	{
		MemoryStream memoryStream = new MemoryStream(_data);
		MetaData metaData = MetaData.Deserialize(memoryStream);
		byte[] array = new byte[memoryStream.Length - memoryStream.Position];
		memoryStream.Read(array, 0, array.Length);
		GameData gameData = GameData.CreateFrom(array);
		return new LoadData(metaData, gameData);
	}

	public SaveGameFileInfo[] GetSaveGameFileInfos()
	{
		return new SaveGameFileInfo[0];
	}

	public string[] GetSaveGameFileNames()
	{
		return new string[0];
	}

	public bool Delete(string saveName)
	{
		_data = new byte[0];
		return true;
	}

	public bool IsSaveGameFileExists(string saveName)
	{
		return false;
	}

	public bool IsWorkingAsync()
	{
		return false;
	}
}
