using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Security.Cryptography
{
	// Token: 0x02000253 RID: 595
	[ComVisible(true)]
	public sealed class CryptoAPITransform : ICryptoTransform, IDisposable
	{
		// Token: 0x0600210E RID: 8462 RVA: 0x000733B0 File Offset: 0x000715B0
		private CryptoAPITransform()
		{
		}

		// Token: 0x0600210F RID: 8463 RVA: 0x000733B8 File Offset: 0x000715B8
		[SecurityCritical]
		internal CryptoAPITransform(int algid, int cArgs, int[] rgArgIds, object[] rgArgValues, byte[] rgbKey, PaddingMode padding, CipherMode cipherChainingMode, int blockSize, int feedbackSize, bool useSalt, CryptoAPITransformMode encDecMode)
		{
			this.BlockSizeValue = blockSize;
			this.ModeValue = cipherChainingMode;
			this.PaddingValue = padding;
			this.encryptOrDecrypt = encDecMode;
			int[] array = new int[rgArgIds.Length];
			Array.Copy(rgArgIds, array, rgArgIds.Length);
			this._rgbKey = new byte[rgbKey.Length];
			Array.Copy(rgbKey, this._rgbKey, rgbKey.Length);
			object[] array2 = new object[rgArgValues.Length];
			for (int i = 0; i < rgArgValues.Length; i++)
			{
				if (rgArgValues[i] is byte[])
				{
					byte[] array3 = (byte[])rgArgValues[i];
					byte[] array4 = new byte[array3.Length];
					Array.Copy(array3, array4, array3.Length);
					array2[i] = array4;
				}
				else if (rgArgValues[i] is int)
				{
					array2[i] = (int)rgArgValues[i];
				}
				else if (rgArgValues[i] is CipherMode)
				{
					array2[i] = (int)rgArgValues[i];
				}
			}
			this._safeProvHandle = Utils.AcquireProvHandle(new CspParameters(24));
			SafeKeyHandle invalidHandle = SafeKeyHandle.InvalidHandle;
			Utils._ImportBulkKey(this._safeProvHandle, algid, useSalt, this._rgbKey, ref invalidHandle);
			this._safeKeyHandle = invalidHandle;
			int j = 0;
			while (j < cArgs)
			{
				int num = rgArgIds[j];
				int dwValue;
				switch (num)
				{
				case 1:
				{
					this.IVValue = (byte[])array2[j];
					byte[] ivvalue = this.IVValue;
					Utils.SetKeyParamRgb(this._safeKeyHandle, array[j], ivvalue, ivvalue.Length);
					break;
				}
				case 2:
				case 3:
					goto IL_1D7;
				case 4:
					this.ModeValue = (CipherMode)array2[j];
					dwValue = (int)array2[j];
					goto IL_1AB;
				case 5:
					dwValue = (int)array2[j];
					goto IL_1AB;
				default:
					if (num != 19)
					{
						goto IL_1D7;
					}
					dwValue = (int)array2[j];
					goto IL_1AB;
				}
				IL_1EC:
				j++;
				continue;
				IL_1AB:
				Utils.SetKeyParamDw(this._safeKeyHandle, array[j], dwValue);
				goto IL_1EC;
				IL_1D7:
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeyParameter"), "_rgArgIds[i]");
			}
		}

		// Token: 0x06002110 RID: 8464 RVA: 0x000735BF File Offset: 0x000717BF
		public void Dispose()
		{
			this.Clear();
		}

		// Token: 0x06002111 RID: 8465 RVA: 0x000735C7 File Offset: 0x000717C7
		[SecuritySafeCritical]
		public void Clear()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06002112 RID: 8466 RVA: 0x000735D8 File Offset: 0x000717D8
		[SecurityCritical]
		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this._rgbKey != null)
				{
					Array.Clear(this._rgbKey, 0, this._rgbKey.Length);
					this._rgbKey = null;
				}
				if (this.IVValue != null)
				{
					Array.Clear(this.IVValue, 0, this.IVValue.Length);
					this.IVValue = null;
				}
				if (this._depadBuffer != null)
				{
					Array.Clear(this._depadBuffer, 0, this._depadBuffer.Length);
					this._depadBuffer = null;
				}
				if (this._safeKeyHandle != null && !this._safeKeyHandle.IsClosed)
				{
					this._safeKeyHandle.Dispose();
				}
				if (this._safeProvHandle != null && !this._safeProvHandle.IsClosed)
				{
					this._safeProvHandle.Dispose();
				}
			}
		}

		// Token: 0x170003FD RID: 1021
		// (get) Token: 0x06002113 RID: 8467 RVA: 0x00073694 File Offset: 0x00071894
		public IntPtr KeyHandle
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				return this._safeKeyHandle.DangerousGetHandle();
			}
		}

		// Token: 0x170003FE RID: 1022
		// (get) Token: 0x06002114 RID: 8468 RVA: 0x000736A1 File Offset: 0x000718A1
		public int InputBlockSize
		{
			get
			{
				return this.BlockSizeValue / 8;
			}
		}

		// Token: 0x170003FF RID: 1023
		// (get) Token: 0x06002115 RID: 8469 RVA: 0x000736AB File Offset: 0x000718AB
		public int OutputBlockSize
		{
			get
			{
				return this.BlockSizeValue / 8;
			}
		}

		// Token: 0x17000400 RID: 1024
		// (get) Token: 0x06002116 RID: 8470 RVA: 0x000736B5 File Offset: 0x000718B5
		public bool CanTransformMultipleBlocks
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000401 RID: 1025
		// (get) Token: 0x06002117 RID: 8471 RVA: 0x000736B8 File Offset: 0x000718B8
		public bool CanReuseTransform
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06002118 RID: 8472 RVA: 0x000736BC File Offset: 0x000718BC
		[SecuritySafeCritical]
		[ComVisible(false)]
		public void Reset()
		{
			this._depadBuffer = null;
			byte[] array = null;
			Utils._EncryptData(this._safeKeyHandle, EmptyArray<byte>.Value, 0, 0, ref array, 0, this.PaddingValue, true);
		}

		// Token: 0x06002119 RID: 8473 RVA: 0x000736F0 File Offset: 0x000718F0
		[SecuritySafeCritical]
		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			if (inputBuffer == null)
			{
				throw new ArgumentNullException("inputBuffer");
			}
			if (outputBuffer == null)
			{
				throw new ArgumentNullException("outputBuffer");
			}
			if (inputOffset < 0)
			{
				throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (inputCount <= 0 || inputCount % this.InputBlockSize != 0 || inputCount > inputBuffer.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
			}
			if (inputBuffer.Length - inputCount < inputOffset)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (this.encryptOrDecrypt == CryptoAPITransformMode.Encrypt)
			{
				return Utils._EncryptData(this._safeKeyHandle, inputBuffer, inputOffset, inputCount, ref outputBuffer, outputOffset, this.PaddingValue, false);
			}
			if (this.PaddingValue == PaddingMode.Zeros || this.PaddingValue == PaddingMode.None)
			{
				return Utils._DecryptData(this._safeKeyHandle, inputBuffer, inputOffset, inputCount, ref outputBuffer, outputOffset, this.PaddingValue, false);
			}
			if (this._depadBuffer == null)
			{
				this._depadBuffer = new byte[this.InputBlockSize];
				int num = inputCount - this.InputBlockSize;
				Buffer.InternalBlockCopy(inputBuffer, inputOffset + num, this._depadBuffer, 0, this.InputBlockSize);
				return Utils._DecryptData(this._safeKeyHandle, inputBuffer, inputOffset, num, ref outputBuffer, outputOffset, this.PaddingValue, false);
			}
			int num2 = Utils._DecryptData(this._safeKeyHandle, this._depadBuffer, 0, this._depadBuffer.Length, ref outputBuffer, outputOffset, this.PaddingValue, false);
			outputOffset += this.OutputBlockSize;
			int num3 = inputCount - this.InputBlockSize;
			Buffer.InternalBlockCopy(inputBuffer, inputOffset + num3, this._depadBuffer, 0, this.InputBlockSize);
			num2 = Utils._DecryptData(this._safeKeyHandle, inputBuffer, inputOffset, num3, ref outputBuffer, outputOffset, this.PaddingValue, false);
			return this.OutputBlockSize + num2;
		}

		// Token: 0x0600211A RID: 8474 RVA: 0x00073884 File Offset: 0x00071A84
		[SecuritySafeCritical]
		public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			if (inputBuffer == null)
			{
				throw new ArgumentNullException("inputBuffer");
			}
			if (inputOffset < 0)
			{
				throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (inputCount < 0 || inputCount > inputBuffer.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
			}
			if (inputBuffer.Length - inputCount < inputOffset)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (this.encryptOrDecrypt == CryptoAPITransformMode.Encrypt)
			{
				byte[] result = null;
				Utils._EncryptData(this._safeKeyHandle, inputBuffer, inputOffset, inputCount, ref result, 0, this.PaddingValue, true);
				this.Reset();
				return result;
			}
			if (inputCount % this.InputBlockSize != 0)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_SSD_InvalidDataSize"));
			}
			if (this._depadBuffer == null)
			{
				byte[] result2 = null;
				Utils._DecryptData(this._safeKeyHandle, inputBuffer, inputOffset, inputCount, ref result2, 0, this.PaddingValue, true);
				this.Reset();
				return result2;
			}
			byte[] array = new byte[this._depadBuffer.Length + inputCount];
			Buffer.InternalBlockCopy(this._depadBuffer, 0, array, 0, this._depadBuffer.Length);
			Buffer.InternalBlockCopy(inputBuffer, inputOffset, array, this._depadBuffer.Length, inputCount);
			byte[] result3 = null;
			Utils._DecryptData(this._safeKeyHandle, array, 0, array.Length, ref result3, 0, this.PaddingValue, true);
			this.Reset();
			return result3;
		}

		// Token: 0x04000BF8 RID: 3064
		private int BlockSizeValue;

		// Token: 0x04000BF9 RID: 3065
		private byte[] IVValue;

		// Token: 0x04000BFA RID: 3066
		private CipherMode ModeValue;

		// Token: 0x04000BFB RID: 3067
		private PaddingMode PaddingValue;

		// Token: 0x04000BFC RID: 3068
		private CryptoAPITransformMode encryptOrDecrypt;

		// Token: 0x04000BFD RID: 3069
		private byte[] _rgbKey;

		// Token: 0x04000BFE RID: 3070
		private byte[] _depadBuffer;

		// Token: 0x04000BFF RID: 3071
		[SecurityCritical]
		private SafeKeyHandle _safeKeyHandle;

		// Token: 0x04000C00 RID: 3072
		[SecurityCritical]
		private SafeProvHandle _safeProvHandle;
	}
}
