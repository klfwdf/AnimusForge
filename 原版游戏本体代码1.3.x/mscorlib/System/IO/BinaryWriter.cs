using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

namespace System.IO
{
	// Token: 0x02000178 RID: 376
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class BinaryWriter : IDisposable
	{
		// Token: 0x060016B8 RID: 5816 RVA: 0x000482F7 File Offset: 0x000464F7
		[__DynamicallyInvokable]
		protected BinaryWriter()
		{
			this.OutStream = Stream.Null;
			this._buffer = new byte[16];
			this._encoding = new UTF8Encoding(false, true);
			this._encoder = this._encoding.GetEncoder();
		}

		// Token: 0x060016B9 RID: 5817 RVA: 0x00048335 File Offset: 0x00046535
		[__DynamicallyInvokable]
		public BinaryWriter(Stream output)
			: this(output, new UTF8Encoding(false, true), false)
		{
		}

		// Token: 0x060016BA RID: 5818 RVA: 0x00048346 File Offset: 0x00046546
		[__DynamicallyInvokable]
		public BinaryWriter(Stream output, Encoding encoding)
			: this(output, encoding, false)
		{
		}

		// Token: 0x060016BB RID: 5819 RVA: 0x00048354 File Offset: 0x00046554
		[__DynamicallyInvokable]
		public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
		{
			if (output == null)
			{
				throw new ArgumentNullException("output");
			}
			if (encoding == null)
			{
				throw new ArgumentNullException("encoding");
			}
			if (!output.CanWrite)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"));
			}
			this.OutStream = output;
			this._buffer = new byte[16];
			this._encoding = encoding;
			this._encoder = this._encoding.GetEncoder();
			this._leaveOpen = leaveOpen;
		}

		// Token: 0x060016BC RID: 5820 RVA: 0x000483CE File Offset: 0x000465CE
		public virtual void Close()
		{
			this.Dispose(true);
		}

		// Token: 0x060016BD RID: 5821 RVA: 0x000483D7 File Offset: 0x000465D7
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this._leaveOpen)
				{
					this.OutStream.Flush();
					return;
				}
				this.OutStream.Close();
			}
		}

		// Token: 0x060016BE RID: 5822 RVA: 0x000483FB File Offset: 0x000465FB
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x17000279 RID: 633
		// (get) Token: 0x060016BF RID: 5823 RVA: 0x00048404 File Offset: 0x00046604
		[__DynamicallyInvokable]
		public virtual Stream BaseStream
		{
			[__DynamicallyInvokable]
			get
			{
				this.Flush();
				return this.OutStream;
			}
		}

		// Token: 0x060016C0 RID: 5824 RVA: 0x00048412 File Offset: 0x00046612
		[__DynamicallyInvokable]
		public virtual void Flush()
		{
			this.OutStream.Flush();
		}

		// Token: 0x060016C1 RID: 5825 RVA: 0x0004841F File Offset: 0x0004661F
		[__DynamicallyInvokable]
		public virtual long Seek(int offset, SeekOrigin origin)
		{
			return this.OutStream.Seek((long)offset, origin);
		}

		// Token: 0x060016C2 RID: 5826 RVA: 0x0004842F File Offset: 0x0004662F
		[__DynamicallyInvokable]
		public virtual void Write(bool value)
		{
			this._buffer[0] = (value ? 1 : 0);
			this.OutStream.Write(this._buffer, 0, 1);
		}

		// Token: 0x060016C3 RID: 5827 RVA: 0x00048454 File Offset: 0x00046654
		[__DynamicallyInvokable]
		public virtual void Write(byte value)
		{
			this.OutStream.WriteByte(value);
		}

		// Token: 0x060016C4 RID: 5828 RVA: 0x00048462 File Offset: 0x00046662
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void Write(sbyte value)
		{
			this.OutStream.WriteByte((byte)value);
		}

		// Token: 0x060016C5 RID: 5829 RVA: 0x00048471 File Offset: 0x00046671
		[__DynamicallyInvokable]
		public virtual void Write(byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			this.OutStream.Write(buffer, 0, buffer.Length);
		}

		// Token: 0x060016C6 RID: 5830 RVA: 0x00048491 File Offset: 0x00046691
		[__DynamicallyInvokable]
		public virtual void Write(byte[] buffer, int index, int count)
		{
			this.OutStream.Write(buffer, index, count);
		}

		// Token: 0x060016C7 RID: 5831 RVA: 0x000484A4 File Offset: 0x000466A4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe virtual void Write(char ch)
		{
			if (char.IsSurrogate(ch))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_SurrogatesNotAllowedAsSingleChar"));
			}
			byte[] array;
			byte* bytes;
			if ((array = this._buffer) == null || array.Length == 0)
			{
				bytes = null;
			}
			else
			{
				bytes = &array[0];
			}
			int bytes2 = this._encoder.GetBytes(&ch, 1, bytes, this._buffer.Length, true);
			array = null;
			this.OutStream.Write(this._buffer, 0, bytes2);
		}

		// Token: 0x060016C8 RID: 5832 RVA: 0x00048518 File Offset: 0x00046718
		[__DynamicallyInvokable]
		public virtual void Write(char[] chars)
		{
			if (chars == null)
			{
				throw new ArgumentNullException("chars");
			}
			byte[] bytes = this._encoding.GetBytes(chars, 0, chars.Length);
			this.OutStream.Write(bytes, 0, bytes.Length);
		}

		// Token: 0x060016C9 RID: 5833 RVA: 0x00048554 File Offset: 0x00046754
		[__DynamicallyInvokable]
		public virtual void Write(char[] chars, int index, int count)
		{
			byte[] bytes = this._encoding.GetBytes(chars, index, count);
			this.OutStream.Write(bytes, 0, bytes.Length);
		}

		// Token: 0x060016CA RID: 5834 RVA: 0x00048580 File Offset: 0x00046780
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe virtual void Write(double value)
		{
			ulong num = (ulong)(*(long*)(&value));
			this._buffer[0] = (byte)num;
			this._buffer[1] = (byte)(num >> 8);
			this._buffer[2] = (byte)(num >> 16);
			this._buffer[3] = (byte)(num >> 24);
			this._buffer[4] = (byte)(num >> 32);
			this._buffer[5] = (byte)(num >> 40);
			this._buffer[6] = (byte)(num >> 48);
			this._buffer[7] = (byte)(num >> 56);
			this.OutStream.Write(this._buffer, 0, 8);
		}

		// Token: 0x060016CB RID: 5835 RVA: 0x00048609 File Offset: 0x00046809
		[__DynamicallyInvokable]
		public virtual void Write(decimal value)
		{
			decimal.GetBytes(value, this._buffer);
			this.OutStream.Write(this._buffer, 0, 16);
		}

		// Token: 0x060016CC RID: 5836 RVA: 0x0004862B File Offset: 0x0004682B
		[__DynamicallyInvokable]
		public virtual void Write(short value)
		{
			this._buffer[0] = (byte)value;
			this._buffer[1] = (byte)(value >> 8);
			this.OutStream.Write(this._buffer, 0, 2);
		}

		// Token: 0x060016CD RID: 5837 RVA: 0x00048656 File Offset: 0x00046856
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void Write(ushort value)
		{
			this._buffer[0] = (byte)value;
			this._buffer[1] = (byte)(value >> 8);
			this.OutStream.Write(this._buffer, 0, 2);
		}

		// Token: 0x060016CE RID: 5838 RVA: 0x00048684 File Offset: 0x00046884
		[__DynamicallyInvokable]
		public virtual void Write(int value)
		{
			this._buffer[0] = (byte)value;
			this._buffer[1] = (byte)(value >> 8);
			this._buffer[2] = (byte)(value >> 16);
			this._buffer[3] = (byte)(value >> 24);
			this.OutStream.Write(this._buffer, 0, 4);
		}

		// Token: 0x060016CF RID: 5839 RVA: 0x000486D4 File Offset: 0x000468D4
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void Write(uint value)
		{
			this._buffer[0] = (byte)value;
			this._buffer[1] = (byte)(value >> 8);
			this._buffer[2] = (byte)(value >> 16);
			this._buffer[3] = (byte)(value >> 24);
			this.OutStream.Write(this._buffer, 0, 4);
		}

		// Token: 0x060016D0 RID: 5840 RVA: 0x00048724 File Offset: 0x00046924
		[__DynamicallyInvokable]
		public virtual void Write(long value)
		{
			this._buffer[0] = (byte)value;
			this._buffer[1] = (byte)(value >> 8);
			this._buffer[2] = (byte)(value >> 16);
			this._buffer[3] = (byte)(value >> 24);
			this._buffer[4] = (byte)(value >> 32);
			this._buffer[5] = (byte)(value >> 40);
			this._buffer[6] = (byte)(value >> 48);
			this._buffer[7] = (byte)(value >> 56);
			this.OutStream.Write(this._buffer, 0, 8);
		}

		// Token: 0x060016D1 RID: 5841 RVA: 0x000487A8 File Offset: 0x000469A8
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void Write(ulong value)
		{
			this._buffer[0] = (byte)value;
			this._buffer[1] = (byte)(value >> 8);
			this._buffer[2] = (byte)(value >> 16);
			this._buffer[3] = (byte)(value >> 24);
			this._buffer[4] = (byte)(value >> 32);
			this._buffer[5] = (byte)(value >> 40);
			this._buffer[6] = (byte)(value >> 48);
			this._buffer[7] = (byte)(value >> 56);
			this.OutStream.Write(this._buffer, 0, 8);
		}

		// Token: 0x060016D2 RID: 5842 RVA: 0x0004882C File Offset: 0x00046A2C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe virtual void Write(float value)
		{
			uint num = *(uint*)(&value);
			this._buffer[0] = (byte)num;
			this._buffer[1] = (byte)(num >> 8);
			this._buffer[2] = (byte)(num >> 16);
			this._buffer[3] = (byte)(num >> 24);
			this.OutStream.Write(this._buffer, 0, 4);
		}

		// Token: 0x060016D3 RID: 5843 RVA: 0x00048884 File Offset: 0x00046A84
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe virtual void Write(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			int byteCount = this._encoding.GetByteCount(value);
			this.Write7BitEncodedInt(byteCount);
			if (this._largeByteBuffer == null)
			{
				this._largeByteBuffer = new byte[256];
				this._maxChars = this._largeByteBuffer.Length / this._encoding.GetMaxByteCount(1);
			}
			if (byteCount <= this._largeByteBuffer.Length)
			{
				this._encoding.GetBytes(value, 0, value.Length, this._largeByteBuffer, 0);
				this.OutStream.Write(this._largeByteBuffer, 0, byteCount);
				return;
			}
			int num = 0;
			int num2;
			for (int i = value.Length; i > 0; i -= num2)
			{
				num2 = ((i > this._maxChars) ? this._maxChars : i);
				if (num < 0 || num2 < 0 || checked(num + num2) > value.Length)
				{
					throw new ArgumentOutOfRangeException("charCount");
				}
				int bytes2;
				fixed (string text = value)
				{
					char* ptr = text;
					if (ptr != null)
					{
						ptr += RuntimeHelpers.OffsetToStringData / 2;
					}
					byte[] array;
					byte* bytes;
					if ((array = this._largeByteBuffer) == null || array.Length == 0)
					{
						bytes = null;
					}
					else
					{
						bytes = &array[0];
					}
					bytes2 = this._encoder.GetBytes(checked(ptr + num), num2, bytes, this._largeByteBuffer.Length, num2 == i);
					array = null;
				}
				this.OutStream.Write(this._largeByteBuffer, 0, bytes2);
				num += num2;
			}
		}

		// Token: 0x060016D4 RID: 5844 RVA: 0x000489E4 File Offset: 0x00046BE4
		[__DynamicallyInvokable]
		protected void Write7BitEncodedInt(int value)
		{
			uint num;
			for (num = (uint)value; num >= 128U; num >>= 7)
			{
				this.Write((byte)(num | 128U));
			}
			this.Write((byte)num);
		}

		// Token: 0x04000805 RID: 2053
		[__DynamicallyInvokable]
		public static readonly BinaryWriter Null = new BinaryWriter();

		// Token: 0x04000806 RID: 2054
		[__DynamicallyInvokable]
		protected Stream OutStream;

		// Token: 0x04000807 RID: 2055
		private byte[] _buffer;

		// Token: 0x04000808 RID: 2056
		private Encoding _encoding;

		// Token: 0x04000809 RID: 2057
		private Encoder _encoder;

		// Token: 0x0400080A RID: 2058
		[OptionalField]
		private bool _leaveOpen;

		// Token: 0x0400080B RID: 2059
		[OptionalField]
		private char[] _tmpOneCharBuffer;

		// Token: 0x0400080C RID: 2060
		private byte[] _largeByteBuffer;

		// Token: 0x0400080D RID: 2061
		private int _maxChars;

		// Token: 0x0400080E RID: 2062
		private const int LargeByteBufferSize = 256;
	}
}
