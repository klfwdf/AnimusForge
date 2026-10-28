using System.Runtime.InteropServices;

namespace TaleWorlds.Library;

public struct PlatformFilePath(PlatformDirectoryPath folderPath, string fileName)
{
	public PlatformDirectoryPath FolderPath = folderPath;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string FileName = fileName;

	public string FileFullPath => Common.PlatformFileHelper.GetFileFullPath(this);

	public static PlatformFilePath operator +(PlatformFilePath path, string str)
	{
		return new PlatformFilePath(path.FolderPath, path.FileName + str);
	}

	public string GetFileNameWithoutExtension()
	{
		int num = FileName.LastIndexOf('.');
		if (num == -1)
		{
			return FileName;
		}
		return FileName.Substring(0, num);
	}

	public override string ToString()
	{
		return FolderPath.ToString() + " - " + FileName;
	}
}
