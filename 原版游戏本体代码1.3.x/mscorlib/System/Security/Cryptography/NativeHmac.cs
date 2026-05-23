using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000274 RID: 628
	internal sealed class NativeHmac : IDisposable
	{
		// Token: 0x06002231 RID: 8753 RVA: 0x00078DCD File Offset: 0x00076FCD
		internal NativeHmac(CapiNative.AlgorithmID algId)
		{
			this._algId = algId;
		}

		// Token: 0x06002232 RID: 8754 RVA: 0x00078DDC File Offset: 0x00076FDC
		[SecuritySafeCritical]
		public void Dispose()
		{
			this.Reset();
		}

		// Token: 0x06002233 RID: 8755 RVA: 0x00078DE4 File Offset: 0x00076FE4
		[SecuritySafeCritical]
		internal void SetKey(byte[] key)
		{
			SafeCspHandle value = CapiNative.SafeNativeMethods.DefaultProvider.Value;
			this._key = NativeHmac.OpenKeyHandle(value, key);
			try
			{
				this._hash = NativeHmac.OpenHmacHandle(value, this._algId, this._key);
			}
			catch (CryptographicException)
			{
				this._key.Dispose();
				this._key = null;
				throw;
			}
		}

		// Token: 0x06002234 RID: 8756 RVA: 0x00078E48 File Offset: 0x00077048
		[SecuritySafeCritical]
		internal unsafe void AppendData(byte[] data, int offset, int count)
		{
			fixed (byte[] array = data)
			{
				byte* ptr;
				if (data == null || array.Length == 0)
				{
					ptr = null;
				}
				else
				{
					ptr = &array[0];
				}
				if (!CapiNative.SafeNativeMethods.CryptHashData(this._hash, (IntPtr)((void*)(ptr + offset)), count, 0))
				{
					throw new CryptographicException(Marshal.GetLastWin32Error());
				}
			}
		}

		// Token: 0x06002235 RID: 8757 RVA: 0x00078E94 File Offset: 0x00077094
		[SecuritySafeCritical]
		internal void Finish(byte[] output)
		{
			int num = output.Length;
			Exception ex = null;
			if (!CapiNative.SafeNativeMethods.CryptGetHashParam(this._hash, CapiNative.HashProperty.HashValue, output, ref num, 0))
			{
				ex = new CryptographicException(Marshal.GetLastWin32Error());
			}
			this.Reset();
			if (ex != null)
			{
				throw ex;
			}
			if (num != output.Length)
			{
				throw new CryptographicException();
			}
		}

		// Token: 0x06002236 RID: 8758 RVA: 0x00078EDB File Offset: 0x000770DB
		[SecuritySafeCritical]
		internal void Reset()
		{
			SafeCspHashHandle hash = this._hash;
			if (hash != null)
			{
				hash.Dispose();
			}
			this._hash = null;
			SafeCspKeyHandle key = this._key;
			if (key != null)
			{
				key.Dispose();
			}
			this._key = null;
		}

		// Token: 0x06002237 RID: 8759 RVA: 0x00078F10 File Offset: 0x00077110
		[SecurityCritical]
		private unsafe static SafeCspKeyHandle OpenKeyHandle(SafeCspHandle hProv, byte[] key)
		{
			if (key.Length > 128)
			{
				throw new CryptographicException();
			}
			int num = sizeof(NativeHmac.BLOBHEADER) + 4;
			int num2 = num + 128;
			int dwDataLen = num + key.Length;
			byte* ptr = stackalloc byte[(UIntPtr)num2];
			NativeHmac.BLOBHEADER* ptr2 = (NativeHmac.BLOBHEADER*)ptr;
			ptr2->bType = 8;
			ptr2->bVersion = 2;
			ptr2->reserved = 0;
			ptr2->aiKeyAlg = CapiNative.AlgorithmID.Rc2;
			int* ptr3 = (int*)(ptr + sizeof(NativeHmac.BLOBHEADER));
			byte* ptr4 = ptr + sizeof(NativeHmac.BLOBHEADER) + 4;
			if (key.Length >= 2)
			{
				*ptr3 = key.Length;
			}
			else
			{
				*ptr3 = 2;
				*(short*)ptr4 = 0;
				dwDataLen = num + 2;
			}
			Marshal.Copy(key, 0, (IntPtr)((void*)ptr4), key.Length);
			SafeCspKeyHandle safeCspKeyHandle;
			bool flag = CapiNative.SafeNativeMethods.CryptImportKey(hProv, (IntPtr)((void*)ptr), dwDataLen, IntPtr.Zero, (CapiNative.KeyGenerationFlags)256, out safeCspKeyHandle);
			for (int i = 0; i < key.Length; i++)
			{
				ptr4[i] = 0;
			}
			if (!flag)
			{
				Exception ex = new CryptographicException(Marshal.GetLastWin32Error());
				safeCspKeyHandle.Dispose();
				throw ex;
			}
			return safeCspKeyHandle;
		}

		// Token: 0x06002238 RID: 8760 RVA: 0x00079004 File Offset: 0x00077204
		[SecurityCritical]
		private unsafe static SafeCspHashHandle OpenHmacHandle(SafeCspHandle hProv, CapiNative.AlgorithmID algId, SafeCspKeyHandle macKey)
		{
			SafeCspHashHandle safeCspHashHandle;
			if (!CapiNative.SafeNativeMethods.CryptCreateHash(hProv, CapiNative.AlgorithmID.Hmac, macKey.DangerousGetHandle(), 0, out safeCspHashHandle))
			{
				Exception ex = new CryptographicException(Marshal.GetLastWin32Error());
				safeCspHashHandle.Dispose();
				throw ex;
			}
			NativeHmac.HMAC_Info hmac_Info = default(NativeHmac.HMAC_Info);
			hmac_Info.HashAlgid = algId;
			IntPtr pbData = new IntPtr((void*)(&hmac_Info));
			if (!CapiNative.SafeNativeMethods.CryptSetHashParam(safeCspHashHandle, CapiNative.HashProperty.HmacInfo, pbData, 0))
			{
				Exception ex2 = new CryptographicException(Marshal.GetLastWin32Error());
				safeCspHashHandle.Dispose();
				throw ex2;
			}
			GC.KeepAlive(macKey);
			return safeCspHashHandle;
		}

		// Token: 0x04000C6B RID: 3179
		[SecurityCritical]
		private SafeCspHashHandle _hash;

		// Token: 0x04000C6C RID: 3180
		[SecurityCritical]
		private SafeCspKeyHandle _key;

		// Token: 0x04000C6D RID: 3181
		private CapiNative.AlgorithmID _algId;

		// Token: 0x02000B49 RID: 2889
		private struct BLOBHEADER
		{
			// Token: 0x040033C4 RID: 13252
			internal byte bType;

			// Token: 0x040033C5 RID: 13253
			internal byte bVersion;

			// Token: 0x040033C6 RID: 13254
			internal short reserved;

			// Token: 0x040033C7 RID: 13255
			internal CapiNative.AlgorithmID aiKeyAlg;
		}

		// Token: 0x02000B4A RID: 2890
		private struct HMAC_Info
		{
			// Token: 0x040033C8 RID: 13256
			internal CapiNative.AlgorithmID HashAlgid;

			// Token: 0x040033C9 RID: 13257
			internal IntPtr pbInnerString;

			// Token: 0x040033CA RID: 13258
			internal uint cbInnerString;

			// Token: 0x040033CB RID: 13259
			internal IntPtr pbOuterString;

			// Token: 0x040033CC RID: 13260
			internal uint cbOuterString;
		}
	}
}
