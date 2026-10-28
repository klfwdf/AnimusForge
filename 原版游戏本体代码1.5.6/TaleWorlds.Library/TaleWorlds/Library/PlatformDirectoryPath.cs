using System.Runtime.InteropServices;

namespace TaleWorlds.Library;

public struct PlatformDirectoryPath(PlatformFileType type, string path)
{
	public PlatformFileType Type = type;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string Path = path;

	public static PlatformDirectoryPath operator +(PlatformDirectoryPath path, string str)
	{
		return new PlatformDirectoryPath(path.Type, path.Path + str);
	}

	public override string ToString()
	{
		return string.Concat(Type, " ", Path);
	}
}
