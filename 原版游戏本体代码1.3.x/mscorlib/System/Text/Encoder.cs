using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A68 RID: 2664
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class Encoder
	{
		// Token: 0x060067C0 RID: 26560 RVA: 0x0015E6BA File Offset: 0x0015C8BA
		internal void SerializeEncoder(SerializationInfo info)
		{
			info.AddValue("m_fallback", this.m_fallback);
		}

		// Token: 0x060067C1 RID: 26561 RVA: 0x0015E6CD File Offset: 0x0015C8CD
		[__DynamicallyInvokable]
		protected Encoder()
		{
		}

		// Token: 0x170011AE RID: 4526
		// (get) Token: 0x060067C2 RID: 26562 RVA: 0x0015E6D5 File Offset: 0x0015C8D5
		// (set) Token: 0x060067C3 RID: 26563 RVA: 0x0015E6E0 File Offset: 0x0015C8E0
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public EncoderFallback Fallback
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_fallback;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (this.m_fallbackBuffer != null && this.m_fallbackBuffer.Remaining > 0)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_FallbackBufferNotEmpty"), "value");
				}
				this.m_fallback = value;
				this.m_fallbackBuffer = null;
			}
		}

		// Token: 0x170011AF RID: 4527
		// (get) Token: 0x060067C4 RID: 26564 RVA: 0x0015E734 File Offset: 0x0015C934
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public EncoderFallbackBuffer FallbackBuffer
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_fallbackBuffer == null)
				{
					if (this.m_fallback != null)
					{
						this.m_fallbackBuffer = this.m_fallback.CreateFallbackBuffer();
					}
					else
					{
						this.m_fallbackBuffer = EncoderFallback.ReplacementFallback.CreateFallbackBuffer();
					}
				}
				return this.m_fallbackBuffer;
			}
		}

		// Token: 0x170011B0 RID: 4528
		// (get) Token: 0x060067C5 RID: 26565 RVA: 0x0015E76F File Offset: 0x0015C96F
		internal bool InternalHasFallbackBuffer
		{
			get
			{
				return this.m_fallbackBuffer != null;
			}
		}

		// Token: 0x060067C6 RID: 26566 RVA: 0x0015E77C File Offset: 0x0015C97C
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual void Reset()
		{
			char[] chars = new char[0];
			byte[] bytes = new byte[this.GetByteCount(chars, 0, 0, true)];
			this.GetBytes(chars, 0, 0, bytes, 0, true);
			if (this.m_fallbackBuffer != null)
			{
				this.m_fallbackBuffer.Reset();
			}
		}

		// Token: 0x060067C7 RID: 26567
		[__DynamicallyInvokable]
		public abstract int GetByteCount(char[] chars, int index, int count, bool flush);

		// Token: 0x060067C8 RID: 26568 RVA: 0x0015E7C0 File Offset: 0x0015C9C0
		[SecurityCritical]
		[CLSCompliant(false)]
		[ComVisible(false)]
		public unsafe virtual int GetByteCount(char* chars, int count, bool flush)
		{
			if (chars == null)
			{
				throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			char[] array = new char[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = chars[i];
			}
			return this.GetByteCount(array, 0, count, flush);
		}

		// Token: 0x060067C9 RID: 26569
		[__DynamicallyInvokable]
		public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush);

		// Token: 0x060067CA RID: 26570 RVA: 0x0015E828 File Offset: 0x0015CA28
		[SecurityCritical]
		[CLSCompliant(false)]
		[ComVisible(false)]
		public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
		{
			if (bytes == null || chars == null)
			{
				throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (charCount < 0 || byteCount < 0)
			{
				throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			char[] array = new char[charCount];
			for (int i = 0; i < charCount; i++)
			{
				array[i] = chars[i];
			}
			byte[] array2 = new byte[byteCount];
			int bytes2 = this.GetBytes(array, 0, charCount, array2, 0, flush);
			if (bytes2 < byteCount)
			{
				byteCount = bytes2;
			}
			for (int i = 0; i < byteCount; i++)
			{
				bytes[i] = array2[i];
			}
			return byteCount;
		}

		// Token: 0x060067CB RID: 26571 RVA: 0x0015E8DC File Offset: 0x0015CADC
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
		{
			if (chars == null || bytes == null)
			{
				throw new ArgumentNullException((chars == null) ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (charIndex < 0 || charCount < 0)
			{
				throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (byteIndex < 0 || byteCount < 0)
			{
				throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (chars.Length - charIndex < charCount)
			{
				throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (bytes.Length - byteIndex < byteCount)
			{
				throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			for (charsUsed = charCount; charsUsed > 0; charsUsed /= 2)
			{
				if (this.GetByteCount(chars, charIndex, charsUsed, flush) <= byteCount)
				{
					bytesUsed = this.GetBytes(chars, charIndex, charsUsed, bytes, byteIndex, flush);
					completed = charsUsed == charCount && (this.m_fallbackBuffer == null || this.m_fallbackBuffer.Remaining == 0);
					return;
				}
				flush = false;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
		}

		// Token: 0x060067CC RID: 26572 RVA: 0x0015EA10 File Offset: 0x0015CC10
		[SecurityCritical]
		[CLSCompliant(false)]
		[ComVisible(false)]
		public unsafe virtual void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
		{
			if (bytes == null || chars == null)
			{
				throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (charCount < 0 || byteCount < 0)
			{
				throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			for (charsUsed = charCount; charsUsed > 0; charsUsed /= 2)
			{
				if (this.GetByteCount(chars, charsUsed, flush) <= byteCount)
				{
					bytesUsed = this.GetBytes(chars, charsUsed, bytes, byteCount, flush);
					completed = charsUsed == charCount && (this.m_fallbackBuffer == null || this.m_fallbackBuffer.Remaining == 0);
					return;
				}
				flush = false;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
		}

		// Token: 0x04002E55 RID: 11861
		internal EncoderFallback m_fallback;

		// Token: 0x04002E56 RID: 11862
		[NonSerialized]
		internal EncoderFallbackBuffer m_fallbackBuffer;
	}
}
