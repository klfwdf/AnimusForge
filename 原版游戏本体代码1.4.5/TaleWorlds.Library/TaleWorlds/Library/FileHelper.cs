using System.IO;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public static class FileHelper
{
	public static SaveResult SaveFile(PlatformFilePath path, byte[] data)
	{
		return Common.PlatformFileHelper.SaveFile(path, data);
	}

	public static SaveResult SaveFileString(PlatformFilePath path, string data)
	{
		return Common.PlatformFileHelper.SaveFileString(path, data);
	}

	public static string GetFileFullPath(PlatformFilePath path)
	{
		return Common.PlatformFileHelper.GetFileFullPath(path);
	}

	public static SaveResult AppendLineToFileString(PlatformFilePath path, string data)
	{
		return Common.PlatformFileHelper.AppendLineToFileString(path, data);
	}

	public static Task<SaveResult> SaveFileAsync(PlatformFilePath path, byte[] data)
	{
		return Common.PlatformFileHelper.SaveFileAsync(path, data);
	}

	public static Task<SaveResult> SaveFileStringAsync(PlatformFilePath path, string data)
	{
		return Common.PlatformFileHelper.SaveFileStringAsync(path, data);
	}

	public static string GetError()
	{
		return Common.PlatformFileHelper.GetError();
	}

	public static bool FileExists(PlatformFilePath path)
	{
		return Common.PlatformFileHelper.FileExists(path);
	}

	public static Task<string> GetFileContentStringAsync(PlatformFilePath path)
	{
		return Common.PlatformFileHelper.GetFileContentStringAsync(path);
	}

	public static string GetFileContentString(PlatformFilePath path)
	{
		return Common.PlatformFileHelper.GetFileContentString(path);
	}

	public static void DeleteFile(PlatformFilePath path)
	{
		Common.PlatformFileHelper.DeleteFile(path);
	}

	public static PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern, SearchOption searchOption)
	{
		return Common.PlatformFileHelper.GetFiles(path, searchPattern, searchOption);
	}

	public static byte[] GetFileContent(PlatformFilePath filePath)
	{
		return Common.PlatformFileHelper.GetFileContent(filePath);
	}

	public static byte[] GetMetaDataContent(PlatformFilePath filePath)
	{
		return Common.PlatformFileHelper.GetMetaDataContent(filePath);
	}

	public static void CopyFile(PlatformFilePath source, PlatformFilePath target)
	{
		byte[] fileContent = GetFileContent(source);
		SaveFile(target, fileContent);
	}

	public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
		if (!directoryInfo.Exists)
		{
			return;
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		Directory.CreateDirectory(destinationDir);
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			string destFileName = Path.Combine(destinationDir, fileInfo.Name);
			fileInfo.CopyTo(destFileName);
		}
		if (recursive)
		{
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo2 in array)
			{
				string destinationDir2 = Path.Combine(destinationDir, directoryInfo2.Name);
				CopyDirectory(directoryInfo2.FullName, destinationDir2, recursive: true);
			}
		}
	}
}
