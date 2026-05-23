using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	/// <summary>
	/// Builds a string. Unlike <see cref="T:System.Text.StringBuilder" /> this class lets you reuse its internal buffer.
	/// </summary>
	// Token: 0x0200006A RID: 106
	[NullableContext(2)]
	[Nullable(0)]
	internal struct StringBuffer
	{
		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x060005CD RID: 1485 RVA: 0x000191DD File Offset: 0x000173DD
		// (set) Token: 0x060005CE RID: 1486 RVA: 0x000191E5 File Offset: 0x000173E5
		public int Position
		{
			get
			{
				return this._position;
			}
			set
			{
				this._position = value;
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x060005CF RID: 1487 RVA: 0x000191EE File Offset: 0x000173EE
		public bool IsEmpty
		{
			get
			{
				return this._buffer == null;
			}
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x000191F9 File Offset: 0x000173F9
		public StringBuffer(IArrayPool<char> bufferPool, int initalSize)
		{
			this = new StringBuffer(BufferUtils.RentBuffer(bufferPool, initalSize));
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00019208 File Offset: 0x00017408
		[NullableContext(1)]
		private StringBuffer(char[] buffer)
		{
			this._buffer = buffer;
			this._position = 0;
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x00019218 File Offset: 0x00017418
		public void Append(IArrayPool<char> bufferPool, char value)
		{
			if (this._position == this._buffer.Length)
			{
				this.EnsureSize(bufferPool, 1);
			}
			char[] buffer = this._buffer;
			int position = this._position;
			this._position = position + 1;
			buffer[position] = value;
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x00019258 File Offset: 0x00017458
		[NullableContext(1)]
		public void Append([Nullable(2)] IArrayPool<char> bufferPool, char[] buffer, int startIndex, int count)
		{
			if (this._position + count >= this._buffer.Length)
			{
				this.EnsureSize(bufferPool, count);
			}
			Array.Copy(buffer, startIndex, this._buffer, this._position, count);
			this._position += count;
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x000192A5 File Offset: 0x000174A5
		public void Clear(IArrayPool<char> bufferPool)
		{
			if (this._buffer != null)
			{
				BufferUtils.ReturnBuffer(bufferPool, this._buffer);
				this._buffer = null;
			}
			this._position = 0;
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x000192CC File Offset: 0x000174CC
		private void EnsureSize(IArrayPool<char> bufferPool, int appendLength)
		{
			char[] array = BufferUtils.RentBuffer(bufferPool, (this._position + appendLength) * 2);
			if (this._buffer != null)
			{
				Array.Copy(this._buffer, array, this._position);
				BufferUtils.ReturnBuffer(bufferPool, this._buffer);
			}
			this._buffer = array;
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00019317 File Offset: 0x00017517
		[NullableContext(1)]
		public override string ToString()
		{
			return this.ToString(0, this._position);
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x00019326 File Offset: 0x00017526
		[NullableContext(1)]
		public string ToString(int start, int length)
		{
			return new string(this._buffer, start, length);
		}

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x060005D8 RID: 1496 RVA: 0x00019335 File Offset: 0x00017535
		public char[] InternalBuffer
		{
			get
			{
				return this._buffer;
			}
		}

		// Token: 0x0400021C RID: 540
		private char[] _buffer;

		// Token: 0x0400021D RID: 541
		private int _position;
	}
}
