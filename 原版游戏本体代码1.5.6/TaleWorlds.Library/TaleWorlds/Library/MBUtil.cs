using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TaleWorlds.Library;

public static class MBUtil
{
	public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirName);
		if (!directoryInfo.Exists)
		{
			return;
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			string destFileName = Path.Combine(destDirName, fileInfo.Name);
			fileInfo.CopyTo(destFileName, overwrite: false);
		}
		if (copySubDirs)
		{
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo2 in array)
			{
				string destDirName2 = Path.Combine(destDirName, directoryInfo2.Name);
				DirectoryCopy(directoryInfo2.FullName, destDirName2, copySubDirs);
			}
		}
	}

	public static T[] ArrayAdd<T>(T[] tArray, T t)
	{
		List<T> list = tArray.ToList();
		list.Add(t);
		return list.ToArray();
	}

	public static T[] ArrayRemove<T>(T[] tArray, T t)
	{
		List<T> list = tArray.ToList();
		if (!list.Remove(t))
		{
			return tArray;
		}
		return list.ToArray();
	}
}
