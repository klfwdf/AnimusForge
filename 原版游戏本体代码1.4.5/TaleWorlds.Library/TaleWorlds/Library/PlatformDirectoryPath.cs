using System.Runtime.InteropServices;

namespace TaleWorlds.Library;

public struct PlatformDirectoryPath
{
	public PlatformFileType Type;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string Path;

	public PlatformDirectoryPath(PlatformFileType type, string path)
	{
		Type = type;
		Path = path;
	}

	public static PlatformDirectoryPath operator +(PlatformDirectoryPath path, string str)
	{
		return new PlatformDirectoryPath(path.Type, path.Path + str);
	}

	public override string ToString()
	{
		return string.Concat(Type, " ", Path);
	}
}
