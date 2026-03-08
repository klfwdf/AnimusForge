using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TaleWorlds.Library
{
	// Token: 0x02000071 RID: 113
	public static class MBUtil
	{
		// Token: 0x06000410 RID: 1040 RVA: 0x0000E62C File Offset: 0x0000C82C
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
			foreach (FileInfo fileInfo in directoryInfo.GetFiles())
			{
				string destFileName = Path.Combine(destDirName, fileInfo.Name);
				fileInfo.CopyTo(destFileName, false);
			}
			if (copySubDirs)
			{
				foreach (DirectoryInfo directoryInfo2 in directories)
				{
					string destDirName2 = Path.Combine(destDirName, directoryInfo2.Name);
					MBUtil.DirectoryCopy(directoryInfo2.FullName, destDirName2, copySubDirs);
				}
			}
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x0000E6CC File Offset: 0x0000C8CC
		public static T[] ArrayAdd<T>(T[] tArray, T t)
		{
			List<T> list = tArray.ToList<T>();
			list.Add(t);
			return list.ToArray();
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x0000E6E0 File Offset: 0x0000C8E0
		public static T[] ArrayRemove<T>(T[] tArray, T t)
		{
			List<T> list = tArray.ToList<T>();
			if (!list.Remove(t))
			{
				return tArray;
			}
			return list.ToArray();
		}
	}
}
