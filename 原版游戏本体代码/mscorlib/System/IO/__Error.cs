using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;

namespace System.IO
{
	// Token: 0x02000175 RID: 373
	internal static class __Error
	{
		// Token: 0x06001688 RID: 5768 RVA: 0x00047378 File Offset: 0x00045578
		internal static void EndOfFile()
		{
			throw new EndOfStreamException(Environment.GetResourceString("IO.EOF_ReadBeyondEOF"));
		}

		// Token: 0x06001689 RID: 5769 RVA: 0x00047389 File Offset: 0x00045589
		internal static void FileNotOpen()
		{
			throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_FileClosed"));
		}

		// Token: 0x0600168A RID: 5770 RVA: 0x0004739B File Offset: 0x0004559B
		internal static void StreamIsClosed()
		{
			throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
		}

		// Token: 0x0600168B RID: 5771 RVA: 0x000473AD File Offset: 0x000455AD
		internal static void MemoryStreamNotExpandable()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_MemStreamNotExpandable"));
		}

		// Token: 0x0600168C RID: 5772 RVA: 0x000473BE File Offset: 0x000455BE
		internal static void ReaderClosed()
		{
			throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ReaderClosed"));
		}

		// Token: 0x0600168D RID: 5773 RVA: 0x000473D0 File Offset: 0x000455D0
		internal static void ReadNotSupported()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
		}

		// Token: 0x0600168E RID: 5774 RVA: 0x000473E1 File Offset: 0x000455E1
		internal static void SeekNotSupported()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
		}

		// Token: 0x0600168F RID: 5775 RVA: 0x000473F2 File Offset: 0x000455F2
		internal static void WrongAsyncResult()
		{
			throw new ArgumentException(Environment.GetResourceString("Arg_WrongAsyncResult"));
		}

		// Token: 0x06001690 RID: 5776 RVA: 0x00047403 File Offset: 0x00045603
		internal static void EndReadCalledTwice()
		{
			throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EndReadCalledMultiple"));
		}

		// Token: 0x06001691 RID: 5777 RVA: 0x00047414 File Offset: 0x00045614
		internal static void EndWriteCalledTwice()
		{
			throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EndWriteCalledMultiple"));
		}

		// Token: 0x06001692 RID: 5778 RVA: 0x00047428 File Offset: 0x00045628
		[SecurityCritical]
		internal static string GetDisplayablePath(string path, bool isInvalidPath)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}
			if (path.Length < 2)
			{
				return path;
			}
			if (PathInternal.IsPartiallyQualified(path) && !isInvalidPath)
			{
				return path;
			}
			bool flag = false;
			try
			{
				if (!isInvalidPath)
				{
					FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, path, false, false);
					flag = true;
				}
			}
			catch (SecurityException)
			{
			}
			catch (ArgumentException)
			{
			}
			catch (NotSupportedException)
			{
			}
			if (!flag)
			{
				if (Path.IsDirectorySeparator(path[path.Length - 1]))
				{
					path = Environment.GetResourceString("IO.IO_NoPermissionToDirectoryName");
				}
				else
				{
					path = Path.GetFileName(path);
				}
			}
			return path;
		}

		// Token: 0x06001693 RID: 5779 RVA: 0x000474D0 File Offset: 0x000456D0
		[SecuritySafeCritical]
		internal static void WinIOError()
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			__Error.WinIOError(lastWin32Error, string.Empty);
		}

		// Token: 0x06001694 RID: 5780 RVA: 0x000474F0 File Offset: 0x000456F0
		[SecurityCritical]
		internal static void WinIOError(int errorCode, string maybeFullPath)
		{
			bool isInvalidPath = errorCode == 123 || errorCode == 161;
			string displayablePath = __Error.GetDisplayablePath(maybeFullPath, isInvalidPath);
			if (errorCode <= 80)
			{
				if (errorCode <= 15)
				{
					switch (errorCode)
					{
					case 2:
						if (displayablePath.Length == 0)
						{
							throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound"));
						}
						throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound_FileName", new object[] { displayablePath }), displayablePath);
					case 3:
						if (displayablePath.Length == 0)
						{
							throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_NoPathName"));
						}
						throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", new object[] { displayablePath }));
					case 4:
						break;
					case 5:
						if (displayablePath.Length == 0)
						{
							throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"));
						}
						throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", new object[] { displayablePath }));
					default:
						if (errorCode == 15)
						{
							throw new DriveNotFoundException(Environment.GetResourceString("IO.DriveNotFound_Drive", new object[] { displayablePath }));
						}
						break;
					}
				}
				else if (errorCode != 32)
				{
					if (errorCode == 80)
					{
						if (displayablePath.Length != 0)
						{
							throw new IOException(Environment.GetResourceString("IO.IO_FileExists_Name", new object[] { displayablePath }), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
						}
					}
				}
				else
				{
					if (displayablePath.Length == 0)
					{
						throw new IOException(Environment.GetResourceString("IO.IO_SharingViolation_NoFileName"), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
					}
					throw new IOException(Environment.GetResourceString("IO.IO_SharingViolation_File", new object[] { displayablePath }), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
				}
			}
			else if (errorCode <= 183)
			{
				if (errorCode == 87)
				{
					throw new IOException(Win32Native.GetMessage(errorCode), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
				}
				if (errorCode == 183)
				{
					if (displayablePath.Length != 0)
					{
						throw new IOException(Environment.GetResourceString("IO.IO_AlreadyExists_Name", new object[] { displayablePath }), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
					}
				}
			}
			else
			{
				if (errorCode == 206)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				if (errorCode == 995)
				{
					throw new OperationCanceledException();
				}
			}
			throw new IOException(Win32Native.GetMessage(errorCode), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
		}

		// Token: 0x06001695 RID: 5781 RVA: 0x00047718 File Offset: 0x00045918
		[SecuritySafeCritical]
		internal static void WinIODriveError(string driveName)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			__Error.WinIODriveError(driveName, lastWin32Error);
		}

		// Token: 0x06001696 RID: 5782 RVA: 0x00047732 File Offset: 0x00045932
		[SecurityCritical]
		internal static void WinIODriveError(string driveName, int errorCode)
		{
			if (errorCode == 3 || errorCode == 15)
			{
				throw new DriveNotFoundException(Environment.GetResourceString("IO.DriveNotFound_Drive", new object[] { driveName }));
			}
			__Error.WinIOError(errorCode, driveName);
		}

		// Token: 0x06001697 RID: 5783 RVA: 0x0004775E File Offset: 0x0004595E
		internal static void WriteNotSupported()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
		}

		// Token: 0x06001698 RID: 5784 RVA: 0x0004776F File Offset: 0x0004596F
		internal static void WriterClosed()
		{
			throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_WriterClosed"));
		}

		// Token: 0x040007F0 RID: 2032
		internal const int ERROR_FILE_NOT_FOUND = 2;

		// Token: 0x040007F1 RID: 2033
		internal const int ERROR_PATH_NOT_FOUND = 3;

		// Token: 0x040007F2 RID: 2034
		internal const int ERROR_ACCESS_DENIED = 5;

		// Token: 0x040007F3 RID: 2035
		internal const int ERROR_INVALID_PARAMETER = 87;
	}
}
