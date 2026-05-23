using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
	// Token: 0x02000174 RID: 372
	internal sealed class __ConsoleStream : Stream
	{
		// Token: 0x06001678 RID: 5752 RVA: 0x00047043 File Offset: 0x00045243
		[SecurityCritical]
		internal __ConsoleStream(SafeFileHandle handle, FileAccess access, bool useFileAPIs)
		{
			this._handle = handle;
			this._canRead = (access & FileAccess.Read) == FileAccess.Read;
			this._canWrite = (access & FileAccess.Write) == FileAccess.Write;
			this._useFileAPIs = useFileAPIs;
			this._isPipe = Win32Native.GetFileType(handle) == 3;
		}

		// Token: 0x17000273 RID: 627
		// (get) Token: 0x06001679 RID: 5753 RVA: 0x00047080 File Offset: 0x00045280
		public override bool CanRead
		{
			get
			{
				return this._canRead;
			}
		}

		// Token: 0x17000274 RID: 628
		// (get) Token: 0x0600167A RID: 5754 RVA: 0x00047088 File Offset: 0x00045288
		public override bool CanWrite
		{
			get
			{
				return this._canWrite;
			}
		}

		// Token: 0x17000275 RID: 629
		// (get) Token: 0x0600167B RID: 5755 RVA: 0x00047090 File Offset: 0x00045290
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000276 RID: 630
		// (get) Token: 0x0600167C RID: 5756 RVA: 0x00047093 File Offset: 0x00045293
		public override long Length
		{
			get
			{
				__Error.SeekNotSupported();
				return 0L;
			}
		}

		// Token: 0x17000277 RID: 631
		// (get) Token: 0x0600167D RID: 5757 RVA: 0x0004709C File Offset: 0x0004529C
		// (set) Token: 0x0600167E RID: 5758 RVA: 0x000470A5 File Offset: 0x000452A5
		public override long Position
		{
			get
			{
				__Error.SeekNotSupported();
				return 0L;
			}
			set
			{
				__Error.SeekNotSupported();
			}
		}

		// Token: 0x0600167F RID: 5759 RVA: 0x000470AC File Offset: 0x000452AC
		[SecuritySafeCritical]
		protected override void Dispose(bool disposing)
		{
			if (this._handle != null)
			{
				this._handle = null;
			}
			this._canRead = false;
			this._canWrite = false;
			base.Dispose(disposing);
		}

		// Token: 0x06001680 RID: 5760 RVA: 0x000470D2 File Offset: 0x000452D2
		[SecuritySafeCritical]
		public override void Flush()
		{
			if (this._handle == null)
			{
				__Error.FileNotOpen();
			}
			if (!this.CanWrite)
			{
				__Error.WriteNotSupported();
			}
		}

		// Token: 0x06001681 RID: 5761 RVA: 0x000470EE File Offset: 0x000452EE
		public override void SetLength(long value)
		{
			__Error.SeekNotSupported();
		}

		// Token: 0x06001682 RID: 5762 RVA: 0x000470F8 File Offset: 0x000452F8
		[SecuritySafeCritical]
		public override int Read([In] [Out] byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((offset < 0) ? "offset" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (!this._canRead)
			{
				__Error.ReadNotSupported();
			}
			int result;
			int num = __ConsoleStream.ReadFileNative(this._handle, buffer, offset, count, this._useFileAPIs, this._isPipe, out result);
			if (num != 0)
			{
				__Error.WinIOError(num, string.Empty);
			}
			return result;
		}

		// Token: 0x06001683 RID: 5763 RVA: 0x0004718C File Offset: 0x0004538C
		public override long Seek(long offset, SeekOrigin origin)
		{
			__Error.SeekNotSupported();
			return 0L;
		}

		// Token: 0x06001684 RID: 5764 RVA: 0x00047198 File Offset: 0x00045398
		[SecuritySafeCritical]
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((offset < 0) ? "offset" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (!this._canWrite)
			{
				__Error.WriteNotSupported();
			}
			int num = __ConsoleStream.WriteFileNative(this._handle, buffer, offset, count, this._useFileAPIs);
			if (num != 0)
			{
				__Error.WinIOError(num, string.Empty);
			}
		}

		// Token: 0x06001685 RID: 5765 RVA: 0x00047224 File Offset: 0x00045424
		[SecurityCritical]
		private unsafe static int ReadFileNative(SafeFileHandle hFile, byte[] bytes, int offset, int count, bool useFileAPIs, bool isPipe, out int bytesRead)
		{
			if (bytes.Length - offset < count)
			{
				throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_IORaceCondition"));
			}
			if (bytes.Length == 0)
			{
				bytesRead = 0;
				return 0;
			}
			__ConsoleStream.WaitForAvailableConsoleInput(hFile, isPipe);
			bool flag;
			if (useFileAPIs)
			{
				fixed (byte[] array = bytes)
				{
					byte* ptr;
					if (bytes == null || array.Length == 0)
					{
						ptr = null;
					}
					else
					{
						ptr = &array[0];
					}
					flag = Win32Native.ReadFile(hFile, ptr + offset, count, out bytesRead, IntPtr.Zero) != 0;
				}
			}
			else
			{
				fixed (byte[] array = bytes)
				{
					byte* ptr2;
					if (bytes == null || array.Length == 0)
					{
						ptr2 = null;
					}
					else
					{
						ptr2 = &array[0];
					}
					int num;
					flag = Win32Native.ReadConsoleW(hFile, ptr2 + offset, count / 2, out num, IntPtr.Zero);
					bytesRead = num * 2;
				}
			}
			if (flag)
			{
				return 0;
			}
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error == 232 || lastWin32Error == 109)
			{
				return 0;
			}
			return lastWin32Error;
		}

		// Token: 0x06001686 RID: 5766 RVA: 0x000472E4 File Offset: 0x000454E4
		[SecurityCritical]
		private unsafe static int WriteFileNative(SafeFileHandle hFile, byte[] bytes, int offset, int count, bool useFileAPIs)
		{
			if (bytes.Length == 0)
			{
				return 0;
			}
			bool flag;
			if (useFileAPIs)
			{
				fixed (byte[] array = bytes)
				{
					byte* ptr;
					if (bytes == null || array.Length == 0)
					{
						ptr = null;
					}
					else
					{
						ptr = &array[0];
					}
					int num;
					flag = Win32Native.WriteFile(hFile, ptr + offset, count, out num, IntPtr.Zero) != 0;
				}
			}
			else
			{
				fixed (byte[] array = bytes)
				{
					byte* ptr2;
					if (bytes == null || array.Length == 0)
					{
						ptr2 = null;
					}
					else
					{
						ptr2 = &array[0];
					}
					int num2;
					flag = Win32Native.WriteConsoleW(hFile, ptr2 + offset, count / 2, out num2, IntPtr.Zero);
				}
			}
			if (flag)
			{
				return 0;
			}
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error == 232 || lastWin32Error == 109)
			{
				return 0;
			}
			return lastWin32Error;
		}

		// Token: 0x06001687 RID: 5767
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void WaitForAvailableConsoleInput(SafeFileHandle file, bool isPipe);

		// Token: 0x040007EA RID: 2026
		private const int BytesPerWChar = 2;

		// Token: 0x040007EB RID: 2027
		[SecurityCritical]
		private SafeFileHandle _handle;

		// Token: 0x040007EC RID: 2028
		private bool _canRead;

		// Token: 0x040007ED RID: 2029
		private bool _canWrite;

		// Token: 0x040007EE RID: 2030
		private bool _useFileAPIs;

		// Token: 0x040007EF RID: 2031
		private bool _isPipe;
	}
}
