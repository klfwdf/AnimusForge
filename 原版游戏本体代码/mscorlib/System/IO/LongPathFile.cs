using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
	// Token: 0x020001AC RID: 428
	[ComVisible(false)]
	internal static class LongPathFile
	{
		// Token: 0x06001AE4 RID: 6884 RVA: 0x0005A720 File Offset: 0x00058920
		[SecurityCritical]
		internal static void Copy(string sourceFileName, string destFileName, bool overwrite)
		{
			string text = LongPath.NormalizePath(sourceFileName);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, text, false, false);
			string text2 = LongPath.NormalizePath(destFileName);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, text2, false, false);
			LongPathFile.InternalCopy(text, text2, sourceFileName, destFileName, overwrite);
		}

		// Token: 0x06001AE5 RID: 6885 RVA: 0x0005A758 File Offset: 0x00058958
		[SecurityCritical]
		private static string InternalCopy(string fullSourceFileName, string fullDestFileName, string sourceFileName, string destFileName, bool overwrite)
		{
			fullSourceFileName = Path.AddLongPathPrefix(fullSourceFileName);
			fullDestFileName = Path.AddLongPathPrefix(fullDestFileName);
			if (!Win32Native.CopyFile(fullSourceFileName, fullDestFileName, !overwrite))
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				string maybeFullPath = destFileName;
				if (lastWin32Error != 80)
				{
					using (SafeFileHandle safeFileHandle = Win32Native.UnsafeCreateFile(fullSourceFileName, int.MinValue, FileShare.Read, null, FileMode.Open, 0, IntPtr.Zero))
					{
						if (safeFileHandle.IsInvalid)
						{
							maybeFullPath = sourceFileName;
						}
					}
					if (lastWin32Error == 5 && LongPathDirectory.InternalExists(fullDestFileName))
					{
						throw new IOException(Environment.GetResourceString("Arg_FileIsDirectory_Name", new object[] { destFileName }), 5, fullDestFileName);
					}
				}
				__Error.WinIOError(lastWin32Error, maybeFullPath);
			}
			return fullDestFileName;
		}

		// Token: 0x06001AE6 RID: 6886 RVA: 0x0005A800 File Offset: 0x00058A00
		[SecurityCritical]
		internal static void Delete(string path)
		{
			string text = LongPath.NormalizePath(path);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, text, false, false);
			string path2 = Path.AddLongPathPrefix(text);
			if (!Win32Native.DeleteFile(path2))
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (lastWin32Error == 2)
				{
					return;
				}
				__Error.WinIOError(lastWin32Error, text);
			}
		}

		// Token: 0x06001AE7 RID: 6887 RVA: 0x0005A840 File Offset: 0x00058A40
		[SecurityCritical]
		internal static bool Exists(string path)
		{
			try
			{
				if (path == null)
				{
					return false;
				}
				if (path.Length == 0)
				{
					return false;
				}
				path = LongPath.NormalizePath(path);
				if (path.Length > 0 && Path.IsDirectorySeparator(path[path.Length - 1]))
				{
					return false;
				}
				FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, path, false, false);
				return LongPathFile.InternalExists(path);
			}
			catch (ArgumentException)
			{
			}
			catch (NotSupportedException)
			{
			}
			catch (SecurityException)
			{
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			return false;
		}

		// Token: 0x06001AE8 RID: 6888 RVA: 0x0005A8F0 File Offset: 0x00058AF0
		[SecurityCritical]
		internal static bool InternalExists(string path)
		{
			string path2 = Path.AddLongPathPrefix(path);
			return File.InternalExists(path2);
		}

		// Token: 0x06001AE9 RID: 6889 RVA: 0x0005A90C File Offset: 0x00058B0C
		[SecurityCritical]
		internal static DateTimeOffset GetCreationTime(string path)
		{
			string text = LongPath.NormalizePath(path);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, text, false, false);
			string path2 = Path.AddLongPathPrefix(text);
			Win32Native.WIN32_FILE_ATTRIBUTE_DATA win32_FILE_ATTRIBUTE_DATA = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
			int num = File.FillAttributeInfo(path2, ref win32_FILE_ATTRIBUTE_DATA, false, false);
			if (num != 0)
			{
				__Error.WinIOError(num, text);
			}
			DateTime dateTime = DateTime.FromFileTimeUtc(win32_FILE_ATTRIBUTE_DATA.ftCreationTime.ToTicks()).ToLocalTime();
			return new DateTimeOffset(dateTime).ToLocalTime();
		}

		// Token: 0x06001AEA RID: 6890 RVA: 0x0005A97C File Offset: 0x00058B7C
		[SecurityCritical]
		internal static DateTimeOffset GetLastAccessTime(string path)
		{
			string text = LongPath.NormalizePath(path);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, text, false, false);
			string path2 = Path.AddLongPathPrefix(text);
			Win32Native.WIN32_FILE_ATTRIBUTE_DATA win32_FILE_ATTRIBUTE_DATA = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
			int num = File.FillAttributeInfo(path2, ref win32_FILE_ATTRIBUTE_DATA, false, false);
			if (num != 0)
			{
				__Error.WinIOError(num, text);
			}
			DateTime dateTime = DateTime.FromFileTimeUtc(win32_FILE_ATTRIBUTE_DATA.ftLastAccessTime.ToTicks()).ToLocalTime();
			return new DateTimeOffset(dateTime).ToLocalTime();
		}

		// Token: 0x06001AEB RID: 6891 RVA: 0x0005A9EC File Offset: 0x00058BEC
		[SecurityCritical]
		internal static DateTimeOffset GetLastWriteTime(string path)
		{
			string text = LongPath.NormalizePath(path);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, text, false, false);
			string path2 = Path.AddLongPathPrefix(text);
			Win32Native.WIN32_FILE_ATTRIBUTE_DATA win32_FILE_ATTRIBUTE_DATA = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
			int num = File.FillAttributeInfo(path2, ref win32_FILE_ATTRIBUTE_DATA, false, false);
			if (num != 0)
			{
				__Error.WinIOError(num, text);
			}
			DateTime dateTime = DateTime.FromFileTimeUtc(win32_FILE_ATTRIBUTE_DATA.ftLastWriteTime.ToTicks()).ToLocalTime();
			return new DateTimeOffset(dateTime).ToLocalTime();
		}

		// Token: 0x06001AEC RID: 6892 RVA: 0x0005AA5C File Offset: 0x00058C5C
		[SecurityCritical]
		internal static void Move(string sourceFileName, string destFileName)
		{
			string text = LongPath.NormalizePath(sourceFileName);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, text, false, false);
			string text2 = LongPath.NormalizePath(destFileName);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, text2, false, false);
			if (!LongPathFile.InternalExists(text))
			{
				__Error.WinIOError(2, text);
			}
			string src = Path.AddLongPathPrefix(text);
			string dst = Path.AddLongPathPrefix(text2);
			if (!Win32Native.MoveFile(src, dst))
			{
				__Error.WinIOError();
			}
		}

		// Token: 0x06001AED RID: 6893 RVA: 0x0005AAB4 File Offset: 0x00058CB4
		[SecurityCritical]
		internal static long GetLength(string path)
		{
			string text = LongPath.NormalizePath(path);
			FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, text, false, false);
			string path2 = Path.AddLongPathPrefix(text);
			Win32Native.WIN32_FILE_ATTRIBUTE_DATA win32_FILE_ATTRIBUTE_DATA = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
			int num = File.FillAttributeInfo(path2, ref win32_FILE_ATTRIBUTE_DATA, false, true);
			if (num != 0)
			{
				__Error.WinIOError(num, path);
			}
			if ((win32_FILE_ATTRIBUTE_DATA.fileAttributes & 16) != 0)
			{
				__Error.WinIOError(2, path);
			}
			return ((long)win32_FILE_ATTRIBUTE_DATA.fileSizeHigh << 32) | ((long)win32_FILE_ATTRIBUTE_DATA.fileSizeLow & (long)((ulong)(-1)));
		}

		// Token: 0x04000943 RID: 2371
		private const int ERROR_ACCESS_DENIED = 5;
	}
}
