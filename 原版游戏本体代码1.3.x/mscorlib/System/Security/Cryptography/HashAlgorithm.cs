using System;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x02000269 RID: 617
	[ComVisible(true)]
	public abstract class HashAlgorithm : IDisposable, ICryptoTransform
	{
		// Token: 0x17000420 RID: 1056
		// (get) Token: 0x060021D6 RID: 8662 RVA: 0x00077F30 File Offset: 0x00076130
		public virtual int HashSize
		{
			get
			{
				return this.HashSizeValue;
			}
		}

		// Token: 0x17000421 RID: 1057
		// (get) Token: 0x060021D7 RID: 8663 RVA: 0x00077F38 File Offset: 0x00076138
		public virtual byte[] Hash
		{
			get
			{
				if (this.m_bDisposed)
				{
					throw new ObjectDisposedException(null);
				}
				if (this.State != 0)
				{
					throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_HashNotYetFinalized"));
				}
				return (byte[])this.HashValue.Clone();
			}
		}

		// Token: 0x060021D8 RID: 8664 RVA: 0x00077F71 File Offset: 0x00076171
		public static HashAlgorithm Create()
		{
			return HashAlgorithm.Create("System.Security.Cryptography.HashAlgorithm");
		}

		// Token: 0x060021D9 RID: 8665 RVA: 0x00077F7D File Offset: 0x0007617D
		public static HashAlgorithm Create(string hashName)
		{
			return (HashAlgorithm)CryptoConfig.CreateFromName(hashName);
		}

		// Token: 0x060021DA RID: 8666 RVA: 0x00077F8C File Offset: 0x0007618C
		public byte[] ComputeHash(Stream inputStream)
		{
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null);
			}
			byte[] array = new byte[4096];
			int num;
			do
			{
				num = inputStream.Read(array, 0, 4096);
				if (num > 0)
				{
					this.HashCore(array, 0, num);
				}
			}
			while (num > 0);
			this.HashValue = this.HashFinal();
			byte[] result = (byte[])this.HashValue.Clone();
			this.Initialize();
			return result;
		}

		// Token: 0x060021DB RID: 8667 RVA: 0x00077FF8 File Offset: 0x000761F8
		public byte[] ComputeHash(byte[] buffer)
		{
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null);
			}
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			this.HashCore(buffer, 0, buffer.Length);
			this.HashValue = this.HashFinal();
			byte[] result = (byte[])this.HashValue.Clone();
			this.Initialize();
			return result;
		}

		// Token: 0x060021DC RID: 8668 RVA: 0x00078054 File Offset: 0x00076254
		public byte[] ComputeHash(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0 || count > buffer.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
			}
			if (buffer.Length - count < offset)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null);
			}
			this.HashCore(buffer, offset, count);
			this.HashValue = this.HashFinal();
			byte[] result = (byte[])this.HashValue.Clone();
			this.Initialize();
			return result;
		}

		// Token: 0x17000422 RID: 1058
		// (get) Token: 0x060021DD RID: 8669 RVA: 0x000780F6 File Offset: 0x000762F6
		public virtual int InputBlockSize
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x17000423 RID: 1059
		// (get) Token: 0x060021DE RID: 8670 RVA: 0x000780F9 File Offset: 0x000762F9
		public virtual int OutputBlockSize
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x17000424 RID: 1060
		// (get) Token: 0x060021DF RID: 8671 RVA: 0x000780FC File Offset: 0x000762FC
		public virtual bool CanTransformMultipleBlocks
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000425 RID: 1061
		// (get) Token: 0x060021E0 RID: 8672 RVA: 0x000780FF File Offset: 0x000762FF
		public virtual bool CanReuseTransform
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060021E1 RID: 8673 RVA: 0x00078104 File Offset: 0x00076304
		public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
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
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null);
			}
			this.State = 1;
			this.HashCore(inputBuffer, inputOffset, inputCount);
			if (outputBuffer != null && (inputBuffer != outputBuffer || inputOffset != outputOffset))
			{
				Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
			}
			return inputCount;
		}

		// Token: 0x060021E2 RID: 8674 RVA: 0x000781A4 File Offset: 0x000763A4
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
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null);
			}
			this.HashCore(inputBuffer, inputOffset, inputCount);
			this.HashValue = this.HashFinal();
			byte[] array;
			if (inputCount != 0)
			{
				array = new byte[inputCount];
				Buffer.InternalBlockCopy(inputBuffer, inputOffset, array, 0, inputCount);
			}
			else
			{
				array = EmptyArray<byte>.Value;
			}
			this.State = 0;
			return array;
		}

		// Token: 0x060021E3 RID: 8675 RVA: 0x00078252 File Offset: 0x00076452
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060021E4 RID: 8676 RVA: 0x00078261 File Offset: 0x00076461
		public void Clear()
		{
			((IDisposable)this).Dispose();
		}

		// Token: 0x060021E5 RID: 8677 RVA: 0x00078269 File Offset: 0x00076469
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.HashValue != null)
				{
					Array.Clear(this.HashValue, 0, this.HashValue.Length);
				}
				this.HashValue = null;
				this.m_bDisposed = true;
			}
		}

		// Token: 0x060021E6 RID: 8678
		public abstract void Initialize();

		// Token: 0x060021E7 RID: 8679
		protected abstract void HashCore(byte[] array, int ibStart, int cbSize);

		// Token: 0x060021E8 RID: 8680
		protected abstract byte[] HashFinal();

		// Token: 0x04000C55 RID: 3157
		protected int HashSizeValue;

		// Token: 0x04000C56 RID: 3158
		protected internal byte[] HashValue;

		// Token: 0x04000C57 RID: 3159
		protected int State;

		// Token: 0x04000C58 RID: 3160
		private bool m_bDisposed;
	}
}
