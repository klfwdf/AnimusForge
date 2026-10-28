using System.IO;

namespace System.Reflection.Metadata;

internal static class PathUtilities
{
	private const char DirectorySeparatorChar = '\\';

	private const char AltDirectorySeparatorChar = '/';

	private const char VolumeSeparatorChar = ':';

	private static string s_platformSpecificDirectorySeparator;

	private static string PlatformSpecificDirectorySeparator => s_platformSpecificDirectorySeparator ?? (s_platformSpecificDirectorySeparator = ((Array.IndexOf(Path.GetInvalidFileNameChars(), '*') >= 0) ? '\\' : '/').ToString());

	internal static int IndexOfFileName(string path)
	{
		if (path == null)
		{
			return -1;
		}
		for (int num = path.Length - 1; num >= 0; num--)
		{
			char c = path[num];
			if (c == '\\' || c == '/' || c == ':')
			{
				return num + 1;
			}
		}
		return 0;
	}

	internal static string GetFileName(string path)
	{
		int num = IndexOfFileName(path);
		if (num > 0)
		{
			return path.Substring(num);
		}
		return path;
	}

	internal static string CombinePathWithRelativePath(string root, string relativePath)
	{
		if (root.Length == 0)
		{
			return relativePath;
		}
		char c = root[root.Length - 1];
		if (c == '\\' || c == '/' || c == ':')
		{
			return root + relativePath;
		}
		return root + PlatformSpecificDirectorySeparator + relativePath;
	}
}
