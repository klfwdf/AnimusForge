using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoMod.Utils.Interop
{
	// Token: 0x020008EB RID: 2283
	internal static class Unix
	{
		// Token: 0x06002F7F RID: 12159
		[DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "uname", SetLastError = true)]
		public unsafe static extern int Uname(byte* buf);

		// Token: 0x06002F80 RID: 12160
		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlopen")]
		private unsafe static extern IntPtr DL1dlopen(byte* filename, Unix.DlopenFlags flags);

		// Token: 0x06002F81 RID: 12161
		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlclose")]
		private static extern int DL1dlclose(IntPtr handle);

		// Token: 0x06002F82 RID: 12162
		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlsym")]
		private unsafe static extern IntPtr DL1dlsym(IntPtr handle, byte* symbol);

		// Token: 0x06002F83 RID: 12163
		[DllImport("dl", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlerror")]
		private static extern IntPtr DL1dlerror();

		// Token: 0x06002F84 RID: 12164
		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlopen")]
		private unsafe static extern IntPtr DL2dlopen(byte* filename, Unix.DlopenFlags flags);

		// Token: 0x06002F85 RID: 12165
		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlclose")]
		private static extern int DL2dlclose(IntPtr handle);

		// Token: 0x06002F86 RID: 12166
		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlsym")]
		private unsafe static extern IntPtr DL2dlsym(IntPtr handle, byte* symbol);

		// Token: 0x06002F87 RID: 12167
		[DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlerror")]
		private static extern IntPtr DL2dlerror();

		// Token: 0x06002F88 RID: 12168 RVA: 0x000A5434 File Offset: 0x000A3634
		[NullableContext(2)]
		internal static byte[] MarshalToUtf8(string str)
		{
			if (str == null)
			{
				return null;
			}
			int len = Encoding.UTF8.GetByteCount(str);
			byte[] arr = ArrayPool<byte>.Shared.Rent(len + 1);
			arr.AsSpan<byte>().Clear();
			Encoding.UTF8.GetBytes(str, 0, str.Length, arr, 0);
			return arr;
		}

		// Token: 0x06002F89 RID: 12169 RVA: 0x000A5484 File Offset: 0x000A3684
		[NullableContext(2)]
		internal static void FreeMarshalledArray(byte[] arr)
		{
			if (arr == null)
			{
				return;
			}
			ArrayPool<byte>.Shared.Return(arr, false);
		}

		// Token: 0x06002F8A RID: 12170 RVA: 0x000A5498 File Offset: 0x000A3698
		[NullableContext(2)]
		public unsafe static IntPtr DlOpen(string filename, Unix.DlopenFlags flags)
		{
			byte[] arr = Unix.MarshalToUtf8(filename);
			IntPtr result;
			try
			{
				for (;;)
				{
					try
					{
						try
						{
							byte[] array;
							byte* pStr;
							if ((array = arr) == null || array.Length == 0)
							{
								pStr = null;
							}
							else
							{
								pStr = &array[0];
							}
							int num = Unix.dlVersion;
							if (num != 0 && num == 1)
							{
								result = Unix.DL2dlopen(pStr, flags);
								break;
							}
							result = Unix.DL1dlopen(pStr, flags);
							break;
						}
						finally
						{
							byte[] array = null;
						}
					}
					catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
					{
						Unix.dlVersion--;
					}
				}
			}
			finally
			{
				Unix.FreeMarshalledArray(arr);
			}
			return result;
		}

		// Token: 0x06002F8B RID: 12171 RVA: 0x000A5548 File Offset: 0x000A3748
		public static bool DlClose(IntPtr handle)
		{
			bool result;
			for (;;)
			{
				try
				{
					int num = Unix.dlVersion;
					if (num != 0 && num == 1)
					{
						result = Unix.DL2dlclose(handle) == 0;
					}
					else
					{
						result = Unix.DL1dlclose(handle) == 0;
					}
				}
				catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
				{
					Unix.dlVersion--;
					continue;
				}
				break;
			}
			return result;
		}

		// Token: 0x06002F8C RID: 12172 RVA: 0x000A55B8 File Offset: 0x000A37B8
		[NullableContext(1)]
		public unsafe static IntPtr DlSym(IntPtr handle, string symbol)
		{
			byte[] arr = Unix.MarshalToUtf8(symbol);
			IntPtr result;
			try
			{
				for (;;)
				{
					try
					{
						try
						{
							byte[] array;
							byte* pStr;
							if ((array = arr) == null || array.Length == 0)
							{
								pStr = null;
							}
							else
							{
								pStr = &array[0];
							}
							int num = Unix.dlVersion;
							if (num != 0 && num == 1)
							{
								result = Unix.DL2dlsym(handle, pStr);
								break;
							}
							result = Unix.DL1dlsym(handle, pStr);
							break;
						}
						finally
						{
							byte[] array = null;
						}
					}
					catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
					{
						Unix.dlVersion--;
					}
				}
			}
			finally
			{
				Unix.FreeMarshalledArray(arr);
			}
			return result;
		}

		// Token: 0x06002F8D RID: 12173 RVA: 0x000A5668 File Offset: 0x000A3868
		public static IntPtr DlError()
		{
			IntPtr result;
			for (;;)
			{
				try
				{
					int num = Unix.dlVersion;
					if (num != 0 && num == 1)
					{
						result = Unix.DL2dlerror();
					}
					else
					{
						result = Unix.DL1dlerror();
					}
				}
				catch (DllNotFoundException obj) when (Unix.dlVersion > 0)
				{
					Unix.dlVersion--;
					continue;
				}
				break;
			}
			return result;
		}

		// Token: 0x04003BB5 RID: 15285
		[Nullable(1)]
		public const string LibC = "libc";

		// Token: 0x04003BB6 RID: 15286
		[Nullable(1)]
		public const string DL1 = "dl";

		// Token: 0x04003BB7 RID: 15287
		[Nullable(1)]
		public const string DL2 = "libdl.so.2";

		// Token: 0x04003BB8 RID: 15288
		public const int AT_PLATFORM = 15;

		// Token: 0x04003BB9 RID: 15289
		private static int dlVersion = 1;

		// Token: 0x020008EC RID: 2284
		public struct LinuxAuxvEntry
		{
			// Token: 0x04003BBA RID: 15290
			[NativeInteger]
			public IntPtr Key;

			// Token: 0x04003BBB RID: 15291
			[NativeInteger]
			public IntPtr Value;
		}

		// Token: 0x020008ED RID: 2285
		public enum DlopenFlags
		{
			// Token: 0x04003BBD RID: 15293
			RTLD_LAZY = 1,
			// Token: 0x04003BBE RID: 15294
			RTLD_NOW,
			// Token: 0x04003BBF RID: 15295
			RTLD_LOCAL = 0,
			// Token: 0x04003BC0 RID: 15296
			RTLD_GLOBAL = 256
		}
	}
}
