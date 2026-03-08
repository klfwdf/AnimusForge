using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A5E RID: 2654
	[Serializable]
	internal class DecoderNLS : Decoder, ISerializable
	{
		// Token: 0x06006770 RID: 26480 RVA: 0x0015D853 File Offset: 0x0015BA53
		internal DecoderNLS(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("NotSupported_TypeCannotDeserialized"), base.GetType()));
		}

		// Token: 0x06006771 RID: 26481 RVA: 0x0015D87A File Offset: 0x0015BA7A
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.SerializeDecoder(info);
			info.AddValue("encoding", this.m_encoding);
			info.SetType(typeof(Encoding.DefaultDecoder));
		}

		// Token: 0x06006772 RID: 26482 RVA: 0x0015D8A4 File Offset: 0x0015BAA4
		internal DecoderNLS(Encoding encoding)
		{
			this.m_encoding = encoding;
			this.m_fallback = this.m_encoding.DecoderFallback;
			this.Reset();
		}

		// Token: 0x06006773 RID: 26483 RVA: 0x0015D8CA File Offset: 0x0015BACA
		internal DecoderNLS()
		{
			this.m_encoding = null;
			this.Reset();
		}

		// Token: 0x06006774 RID: 26484 RVA: 0x0015D8DF File Offset: 0x0015BADF
		public override void Reset()
		{
			if (this.m_fallbackBuffer != null)
			{
				this.m_fallbackBuffer.Reset();
			}
		}

		// Token: 0x06006775 RID: 26485 RVA: 0x0015D8F4 File Offset: 0x0015BAF4
		public override int GetCharCount(byte[] bytes, int index, int count)
		{
			return this.GetCharCount(bytes, index, count, false);
		}

		// Token: 0x06006776 RID: 26486 RVA: 0x0015D900 File Offset: 0x0015BB00
		[SecuritySafeCritical]
		public unsafe override int GetCharCount(byte[] bytes, int index, int count, bool flush)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (bytes.Length - index < count)
			{
				throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (bytes.Length == 0)
			{
				bytes = new byte[1];
			}
			byte[] array;
			byte* ptr;
			if ((array = bytes) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			return this.GetCharCount(ptr + index, count, flush);
		}

		// Token: 0x06006777 RID: 26487 RVA: 0x0015D99C File Offset: 0x0015BB9C
		[SecurityCritical]
		public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			this.m_mustFlush = flush;
			this.m_throwOnOverflow = true;
			return this.m_encoding.GetCharCount(bytes, count, this);
		}

		// Token: 0x06006778 RID: 26488 RVA: 0x0015D9F8 File Offset: 0x0015BBF8
		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			return this.GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
		}

		// Token: 0x06006779 RID: 26489 RVA: 0x0015DA08 File Offset: 0x0015BC08
		[SecuritySafeCritical]
		public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
		{
			if (bytes == null || chars == null)
			{
				throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (byteIndex < 0 || byteCount < 0)
			{
				throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (bytes.Length - byteIndex < byteCount)
			{
				throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (charIndex < 0 || charIndex > chars.Length)
			{
				throw new ArgumentOutOfRangeException("charIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (bytes.Length == 0)
			{
				bytes = new byte[1];
			}
			int charCount = chars.Length - charIndex;
			if (chars.Length == 0)
			{
				chars = new char[1];
			}
			byte[] array;
			byte* ptr;
			if ((array = bytes) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			char[] array2;
			char* ptr2;
			if ((array2 = chars) == null || array2.Length == 0)
			{
				ptr2 = null;
			}
			else
			{
				ptr2 = &array2[0];
			}
			return this.GetChars(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, flush);
		}

		// Token: 0x0600677A RID: 26490 RVA: 0x0015DB0C File Offset: 0x0015BD0C
		[SecurityCritical]
		public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush)
		{
			if (chars == null || bytes == null)
			{
				throw new ArgumentNullException((chars == null) ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (byteCount < 0 || charCount < 0)
			{
				throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			this.m_mustFlush = flush;
			this.m_throwOnOverflow = true;
			return this.m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
		}

		// Token: 0x0600677B RID: 26491 RVA: 0x0015DB90 File Offset: 0x0015BD90
		[SecuritySafeCritical]
		public unsafe override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
		{
			if (bytes == null || chars == null)
			{
				throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (byteIndex < 0 || byteCount < 0)
			{
				throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (charIndex < 0 || charCount < 0)
			{
				throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (bytes.Length - byteIndex < byteCount)
			{
				throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (chars.Length - charIndex < charCount)
			{
				throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (bytes.Length == 0)
			{
				bytes = new byte[1];
			}
			if (chars.Length == 0)
			{
				chars = new char[1];
			}
			byte[] array;
			byte* ptr;
			if ((array = bytes) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			char[] array2;
			char* ptr2;
			if ((array2 = chars) == null || array2.Length == 0)
			{
				ptr2 = null;
			}
			else
			{
				ptr2 = &array2[0];
			}
			this.Convert(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, flush, out bytesUsed, out charsUsed, out completed);
			array2 = null;
			array = null;
		}

		// Token: 0x0600677C RID: 26492 RVA: 0x0015DCBC File Offset: 0x0015BEBC
		[SecurityCritical]
		public unsafe override void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
		{
			if (chars == null || bytes == null)
			{
				throw new ArgumentNullException((chars == null) ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (byteCount < 0 || charCount < 0)
			{
				throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			this.m_mustFlush = flush;
			this.m_throwOnOverflow = false;
			this.m_bytesUsed = 0;
			charsUsed = this.m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
			bytesUsed = this.m_bytesUsed;
			completed = bytesUsed == byteCount && (!flush || !this.HasState) && (this.m_fallbackBuffer == null || this.m_fallbackBuffer.Remaining == 0);
		}

		// Token: 0x1700119C RID: 4508
		// (get) Token: 0x0600677D RID: 26493 RVA: 0x0015DD81 File Offset: 0x0015BF81
		public bool MustFlush
		{
			get
			{
				return this.m_mustFlush;
			}
		}

		// Token: 0x1700119D RID: 4509
		// (get) Token: 0x0600677E RID: 26494 RVA: 0x0015DD89 File Offset: 0x0015BF89
		internal virtual bool HasState
		{
			get
			{
				return false;
			}
		}

		// Token: 0x0600677F RID: 26495 RVA: 0x0015DD8C File Offset: 0x0015BF8C
		internal void ClearMustFlush()
		{
			this.m_mustFlush = false;
		}

		// Token: 0x04002E3D RID: 11837
		protected Encoding m_encoding;

		// Token: 0x04002E3E RID: 11838
		[NonSerialized]
		protected bool m_mustFlush;

		// Token: 0x04002E3F RID: 11839
		[NonSerialized]
		internal bool m_throwOnOverflow;

		// Token: 0x04002E40 RID: 11840
		[NonSerialized]
		internal int m_bytesUsed;
	}
}
