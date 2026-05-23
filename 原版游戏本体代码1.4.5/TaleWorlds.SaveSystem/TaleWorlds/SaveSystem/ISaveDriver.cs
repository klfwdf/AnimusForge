using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public interface ISaveDriver
{
	Task<SaveResultWithMessage> Save(string saveName, int version, MetaData metaData, GameData gameData);

	SaveGameFileInfo[] GetSaveGameFileInfos();

	string[] GetSaveGameFileNames();

	MetaData LoadMetaData(string saveName);

	LoadData Load(string saveName);

	bool Delete(string saveName);

	bool IsSaveGameFileExists(string saveName);

	bool IsWorkingAsync();
}
