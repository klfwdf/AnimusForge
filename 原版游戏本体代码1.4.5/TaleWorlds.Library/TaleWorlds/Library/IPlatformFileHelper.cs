using System.IO;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public interface IPlatformFileHelper
{
	SaveResult SaveFile(PlatformFilePath path, byte[] data);

	SaveResult SaveFileString(PlatformFilePath path, string data);

	SaveResult AppendLineToFileString(PlatformFilePath path, string data);

	Task<SaveResult> SaveFileAsync(PlatformFilePath path, byte[] data);

	Task<SaveResult> SaveFileStringAsync(PlatformFilePath path, string data);

	bool FileExists(PlatformFilePath path);

	Task<string> GetFileContentStringAsync(PlatformFilePath path);

	string GetFileContentString(PlatformFilePath path);

	byte[] GetFileContent(PlatformFilePath filePath);

	byte[] GetMetaDataContent(PlatformFilePath filePath);

	bool DeleteFile(PlatformFilePath path);

	PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern, SearchOption searchOption);

	string GetFileFullPath(PlatformFilePath filePath);

	string GetError();
}
