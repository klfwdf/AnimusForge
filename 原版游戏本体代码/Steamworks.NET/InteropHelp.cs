using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Steamworks
{
	// Token: 0x020001B7 RID: 439
	public class InteropHelp
	{
		// Token: 0x06000AEF RID: 2799 RVA: 0x0000CB91 File Offset: 0x0000AD91
		public static void TestIfPlatformSupported()
		{
		}

		// Token: 0x06000AF0 RID: 2800 RVA: 0x0000EED7 File Offset: 0x0000D0D7
		public static void TestIfAvailableClient()
		{
			InteropHelp.TestIfPlatformSupported();
			if (CSteamAPIContext.GetSteamClient() == IntPtr.Zero && !CSteamAPIContext.Init())
			{
				throw new InvalidOperationException("Steamworks is not initialized.");
			}
		}

		// Token: 0x06000AF1 RID: 2801 RVA: 0x0000EF01 File Offset: 0x0000D101
		public static void TestIfAvailableGameServer()
		{
			InteropHelp.TestIfPlatformSupported();
			if (CSteamGameServerAPIContext.GetSteamClient() == IntPtr.Zero && !CSteamGameServerAPIContext.Init())
			{
				throw new InvalidOperationException("Steamworks GameServer is not initialized.");
			}
		}

		// Token: 0x06000AF2 RID: 2802 RVA: 0x0000EF2C File Offset: 0x0000D12C
		public static string PtrToStringUTF8(IntPtr nativeUtf8)
		{
			if (nativeUtf8 == IntPtr.Zero)
			{
				return null;
			}
			int num = 0;
			while (Marshal.ReadByte(nativeUtf8, num) != 0)
			{
				num++;
			}
			if (num == 0)
			{
				return string.Empty;
			}
			byte[] array = new byte[num];
			Marshal.Copy(nativeUtf8, array, 0, array.Length);
			return Encoding.UTF8.GetString(array);
		}

		// Token: 0x06000AF3 RID: 2803 RVA: 0x0000EF80 File Offset: 0x0000D180
		public static string ByteArrayToStringUTF8(byte[] buffer)
		{
			int num = 0;
			while (num < buffer.Length && buffer[num] != 0)
			{
				num++;
			}
			return Encoding.UTF8.GetString(buffer, 0, num);
		}

		// Token: 0x06000AF4 RID: 2804 RVA: 0x0000EFB0 File Offset: 0x0000D1B0
		public static void StringToByteArrayUTF8(string str, byte[] outArrayBuffer, int outArrayBufferSize)
		{
			outArrayBuffer = new byte[outArrayBufferSize];
			int bytes = Encoding.UTF8.GetBytes(str, 0, str.Length, outArrayBuffer, 0);
			outArrayBuffer[bytes] = 0;
		}

		// Token: 0x020001CD RID: 461
		public class UTF8StringHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			// Token: 0x06000B5F RID: 2911 RVA: 0x00010258 File Offset: 0x0000E458
			public UTF8StringHandle(string str)
				: base(true)
			{
				if (str == null)
				{
					base.SetHandle(IntPtr.Zero);
					return;
				}
				byte[] array = new byte[Encoding.UTF8.GetByteCount(str) + 1];
				Encoding.UTF8.GetBytes(str, 0, str.Length, array, 0);
				IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
				Marshal.Copy(array, 0, intPtr, array.Length);
				base.SetHandle(intPtr);
			}

			// Token: 0x06000B60 RID: 2912 RVA: 0x000102BE File Offset: 0x0000E4BE
			protected override bool ReleaseHandle()
			{
				if (!this.IsInvalid)
				{
					Marshal.FreeHGlobal(this.handle);
				}
				return true;
			}
		}

		// Token: 0x020001CE RID: 462
		public class SteamParamStringArray
		{
			// Token: 0x06000B61 RID: 2913 RVA: 0x000102D4 File Offset: 0x0000E4D4
			public SteamParamStringArray(IList<string> strings)
			{
				if (strings == null)
				{
					this.m_pSteamParamStringArray = IntPtr.Zero;
					return;
				}
				this.m_Strings = new IntPtr[strings.Count];
				for (int i = 0; i < strings.Count; i++)
				{
					byte[] array = new byte[Encoding.UTF8.GetByteCount(strings[i]) + 1];
					Encoding.UTF8.GetBytes(strings[i], 0, strings[i].Length, array, 0);
					this.m_Strings[i] = Marshal.AllocHGlobal(array.Length);
					Marshal.Copy(array, 0, this.m_Strings[i], array.Length);
				}
				this.m_ptrStrings = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * this.m_Strings.Length);
				SteamParamStringArray_t steamParamStringArray_t = new SteamParamStringArray_t
				{
					m_ppStrings = this.m_ptrStrings,
					m_nNumStrings = this.m_Strings.Length
				};
				Marshal.Copy(this.m_Strings, 0, steamParamStringArray_t.m_ppStrings, this.m_Strings.Length);
				this.m_pSteamParamStringArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SteamParamStringArray_t)));
				Marshal.StructureToPtr(steamParamStringArray_t, this.m_pSteamParamStringArray, false);
			}

			// Token: 0x06000B62 RID: 2914 RVA: 0x00010404 File Offset: 0x0000E604
			protected override void Finalize()
			{
				try
				{
					if (this.m_Strings != null)
					{
						IntPtr[] strings = this.m_Strings;
						for (int i = 0; i < strings.Length; i++)
						{
							Marshal.FreeHGlobal(strings[i]);
						}
					}
					if (this.m_ptrStrings != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(this.m_ptrStrings);
					}
					if (this.m_pSteamParamStringArray != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(this.m_pSteamParamStringArray);
					}
				}
				finally
				{
					base.Finalize();
				}
			}

			// Token: 0x06000B63 RID: 2915 RVA: 0x0001048C File Offset: 0x0000E68C
			public static implicit operator IntPtr(InteropHelp.SteamParamStringArray that)
			{
				return that.m_pSteamParamStringArray;
			}

			// Token: 0x04000AD3 RID: 2771
			private IntPtr[] m_Strings;

			// Token: 0x04000AD4 RID: 2772
			private IntPtr m_ptrStrings;

			// Token: 0x04000AD5 RID: 2773
			private IntPtr m_pSteamParamStringArray;
		}
	}
}
