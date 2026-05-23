using System;
using System.IO;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x02000031 RID: 49
	public static class FileHelper
	{
		// Token: 0x060001A2 RID: 418 RVA: 0x00006C83 File Offset: 0x00004E83
		public static SaveResult SaveFile(PlatformFilePath path, byte[] data)
		{
			return Common.PlatformFileHelper.SaveFile(path, data);
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x00006C91 File Offset: 0x00004E91
		public static SaveResult SaveFileString(PlatformFilePath path, string data)
		{
			return Common.PlatformFileHelper.SaveFileString(path, data);
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x00006C9F File Offset: 0x00004E9F
		public static string GetFileFullPath(PlatformFilePath path)
		{
			return Common.PlatformFileHelper.GetFileFullPath(path);
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00006CAC File Offset: 0x00004EAC
		public static SaveResult AppendLineToFileString(PlatformFilePath path, string data)
		{
			return Common.PlatformFileHelper.AppendLineToFileString(path, data);
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x00006CBA File Offset: 0x00004EBA
		public static Task<SaveResult> SaveFileAsync(PlatformFilePath path, byte[] data)
		{
			return Common.PlatformFileHelper.SaveFileAsync(path, data);
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00006CC8 File Offset: 0x00004EC8
		public static Task<SaveResult> SaveFileStringAsync(PlatformFilePath path, string data)
		{
			return Common.PlatformFileHelper.SaveFileStringAsync(path, data);
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x00006CD6 File Offset: 0x00004ED6
		public static string GetError()
		{
			return Common.PlatformFileHelper.GetError();
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x00006CE2 File Offset: 0x00004EE2
		public static bool FileExists(PlatformFilePath path)
		{
			return Common.PlatformFileHelper.FileExists(path);
		}

		// Token: 0x060001AA RID: 426 RVA: 0x00006CEF File Offset: 0x00004EEF
		public static Task<string> GetFileContentStringAsync(PlatformFilePath path)
		{
			return Common.PlatformFileHelper.GetFileContentStringAsync(path);
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00006CFC File Offset: 0x00004EFC
		public static string GetFileContentString(PlatformFilePath path)
		{
			return Common.PlatformFileHelper.GetFileContentString(path);
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00006D09 File Offset: 0x00004F09
		public static void DeleteFile(PlatformFilePath path)
		{
			Common.PlatformFileHelper.DeleteFile(path);
		}

		// Token: 0x060001AD RID: 429 RVA: 0x00006D17 File Offset: 0x00004F17
		public static PlatformFilePath[] GetFiles(PlatformDirectoryPath path, string searchPattern, SearchOption searchOption)
		{
			return Common.PlatformFileHelper.GetFiles(path, searchPattern, searchOption);
		}

		// Token: 0x060001AE RID: 430 RVA: 0x00006D26 File Offset: 0x00004F26
		public static byte[] GetFileContent(PlatformFilePath filePath)
		{
			return Common.PlatformFileHelper.GetFileContent(filePath);
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00006D33 File Offset: 0x00004F33
		public static byte[] GetMetaDataContent(PlatformFilePath filePath)
		{
			return Common.PlatformFileHelper.GetMetaDataContent(filePath);
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00006D40 File Offset: 0x00004F40
		public static void CopyFile(PlatformFilePath source, PlatformFilePath target)
		{
			byte[] fileContent = FileHelper.GetFileContent(source);
			FileHelper.SaveFile(target, fileContent);
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x00006D5C File Offset: 0x00004F5C
		public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
			if (!directoryInfo.Exists)
			{
				return;
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			Directory.CreateDirectory(destinationDir);
			foreach (FileInfo fileInfo in directoryInfo.GetFiles())
			{
				string destFileName = Path.Combine(destinationDir, fileInfo.Name);
				fileInfo.CopyTo(destFileName);
			}
			if (recursive)
			{
				foreach (DirectoryInfo directoryInfo2 in directories)
				{
					string destinationDir2 = Path.Combine(destinationDir, directoryInfo2.Name);
					FileHelper.CopyDirectory(directoryInfo2.FullName, destinationDir2, true);
				}
			}
		}
	}
}
