using System;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security.Cryptography
{
	// Token: 0x02000250 RID: 592
	[ComVisible(true)]
	public class FromBase64Transform : ICryptoTransform, IDisposable
	{
		// Token: 0x060020FE RID: 8446 RVA: 0x00072F14 File Offset: 0x00071114
		public FromBase64Transform()
			: this(FromBase64TransformMode.IgnoreWhiteSpaces)
		{
		}

		// Token: 0x060020FF RID: 8447 RVA: 0x00072F1D File Offset: 0x0007111D
		public FromBase64Transform(FromBase64TransformMode whitespaces)
		{
			this._whitespaces = whitespaces;
			this._inputIndex = 0;
		}

		// Token: 0x170003F9 RID: 1017
		// (get) Token: 0x06002100 RID: 8448 RVA: 0x00072F3F File Offset: 0x0007113F
		public int InputBlockSize
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170003FA RID: 1018
		// (get) Token: 0x06002101 RID: 8449 RVA: 0x00072F42 File Offset: 0x00071142
		public int OutputBlockSize
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x170003FB RID: 1019
		// (get) Token: 0x06002102 RID: 8450 RVA: 0x00072F45 File Offset: 0x00071145
		public bool CanTransformMultipleBlocks
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170003FC RID: 1020
		// (get) Token: 0x06002103 RID: 8451 RVA: 0x00072F48 File Offset: 0x00071148
		public virtual bool CanReuseTransform
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06002104 RID: 8452 RVA: 0x00072F4C File Offset: 0x0007114C
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
			if (this._inputBuffer == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
			}
			byte[] array = new byte[inputCount];
			int num;
			if (this._whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces)
			{
				array = this.DiscardWhiteSpaces(inputBuffer, inputOffset, inputCount);
				num = array.Length;
			}
			else
			{
				Buffer.InternalBlockCopy(inputBuffer, inputOffset, array, 0, inputCount);
				num = inputCount;
			}
			if (num + this._inputIndex < 4)
			{
				Buffer.InternalBlockCopy(array, 0, this._inputBuffer, this._inputIndex, num);
				this._inputIndex += num;
				return 0;
			}
			int num2 = (num + this._inputIndex) / 4;
			byte[] array2 = new byte[this._inputIndex + num];
			Buffer.InternalBlockCopy(this._inputBuffer, 0, array2, 0, this._inputIndex);
			Buffer.InternalBlockCopy(array, 0, array2, this._inputIndex, num);
			this._inputIndex = (num + this._inputIndex) % 4;
			Buffer.InternalBlockCopy(array, num - this._inputIndex, this._inputBuffer, 0, this._inputIndex);
			char[] chars = Encoding.ASCII.GetChars(array2, 0, 4 * num2);
			byte[] array3 = Convert.FromBase64CharArray(chars, 0, 4 * num2);
			Buffer.BlockCopy(array3, 0, outputBuffer, outputOffset, array3.Length);
			return array3.Length;
		}

		// Token: 0x06002105 RID: 8453 RVA: 0x000730C0 File Offset: 0x000712C0
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
			if (this._inputBuffer == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
			}
			byte[] array = new byte[inputCount];
			int num;
			if (this._whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces)
			{
				array = this.DiscardWhiteSpaces(inputBuffer, inputOffset, inputCount);
				num = array.Length;
			}
			else
			{
				Buffer.InternalBlockCopy(inputBuffer, inputOffset, array, 0, inputCount);
				num = inputCount;
			}
			if (num + this._inputIndex < 4)
			{
				this.Reset();
				return EmptyArray<byte>.Value;
			}
			int num2 = (num + this._inputIndex) / 4;
			byte[] array2 = new byte[this._inputIndex + num];
			Buffer.InternalBlockCopy(this._inputBuffer, 0, array2, 0, this._inputIndex);
			Buffer.InternalBlockCopy(array, 0, array2, this._inputIndex, num);
			this._inputIndex = (num + this._inputIndex) % 4;
			Buffer.InternalBlockCopy(array, num - this._inputIndex, this._inputBuffer, 0, this._inputIndex);
			char[] chars = Encoding.ASCII.GetChars(array2, 0, 4 * num2);
			byte[] result = Convert.FromBase64CharArray(chars, 0, 4 * num2);
			this.Reset();
			return result;
		}

		// Token: 0x06002106 RID: 8454 RVA: 0x00073210 File Offset: 0x00071410
		private byte[] DiscardWhiteSpaces(byte[] inputBuffer, int inputOffset, int inputCount)
		{
			int num = 0;
			for (int i = 0; i < inputCount; i++)
			{
				if (char.IsWhiteSpace((char)inputBuffer[inputOffset + i]))
				{
					num++;
				}
			}
			byte[] array = new byte[inputCount - num];
			num = 0;
			for (int i = 0; i < inputCount; i++)
			{
				if (!char.IsWhiteSpace((char)inputBuffer[inputOffset + i]))
				{
					array[num++] = inputBuffer[inputOffset + i];
				}
			}
			return array;
		}

		// Token: 0x06002107 RID: 8455 RVA: 0x0007326B File Offset: 0x0007146B
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06002108 RID: 8456 RVA: 0x0007327A File Offset: 0x0007147A
		private void Reset()
		{
			this._inputIndex = 0;
		}

		// Token: 0x06002109 RID: 8457 RVA: 0x00073283 File Offset: 0x00071483
		public void Clear()
		{
			this.Dispose();
		}

		// Token: 0x0600210A RID: 8458 RVA: 0x0007328B File Offset: 0x0007148B
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this._inputBuffer != null)
				{
					Array.Clear(this._inputBuffer, 0, this._inputBuffer.Length);
				}
				this._inputBuffer = null;
				this._inputIndex = 0;
			}
		}

		// Token: 0x0600210B RID: 8459 RVA: 0x000732BC File Offset: 0x000714BC
		~FromBase64Transform()
		{
			this.Dispose(false);
		}

		// Token: 0x04000BEE RID: 3054
		private byte[] _inputBuffer = new byte[4];

		// Token: 0x04000BEF RID: 3055
		private int _inputIndex;

		// Token: 0x04000BF0 RID: 3056
		private FromBase64TransformMode _whitespaces;
	}
}
