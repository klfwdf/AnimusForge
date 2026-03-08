using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A69 RID: 2665
	[Serializable]
	internal class EncoderNLS : Encoder, ISerializable
	{
		// Token: 0x060067CD RID: 26573 RVA: 0x0015EADF File Offset: 0x0015CCDF
		internal EncoderNLS(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("NotSupported_TypeCannotDeserialized"), base.GetType()));
		}

		// Token: 0x060067CE RID: 26574 RVA: 0x0015EB06 File Offset: 0x0015CD06
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.SerializeEncoder(info);
			info.AddValue("encoding", this.m_encoding);
			info.AddValue("charLeftOver", this.charLeftOver);
			info.SetType(typeof(Encoding.DefaultEncoder));
		}

		// Token: 0x060067CF RID: 26575 RVA: 0x0015EB41 File Offset: 0x0015CD41
		internal EncoderNLS(Encoding encoding)
		{
			this.m_encoding = encoding;
			this.m_fallback = this.m_encoding.EncoderFallback;
			this.Reset();
		}

		// Token: 0x060067D0 RID: 26576 RVA: 0x0015EB67 File Offset: 0x0015CD67
		internal EncoderNLS()
		{
			this.m_encoding = null;
			this.Reset();
		}

		// Token: 0x060067D1 RID: 26577 RVA: 0x0015EB7C File Offset: 0x0015CD7C
		public override void Reset()
		{
			this.charLeftOver = '\0';
			if (this.m_fallbackBuffer != null)
			{
				this.m_fallbackBuffer.Reset();
			}
		}

		// Token: 0x060067D2 RID: 26578 RVA: 0x0015EB98 File Offset: 0x0015CD98
		[SecuritySafeCritical]
		public unsafe override int GetByteCount(char[] chars, int index, int count, bool flush)
		{
			if (chars == null)
			{
				throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (index < 0 || count < 0)
			{
				throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (chars.Length - index < count)
			{
				throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (chars.Length == 0)
			{
				chars = new char[1];
			}
			char[] array;
			char* ptr;
			if ((array = chars) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			int byteCount = this.GetByteCount(ptr + index, count, flush);
			array = null;
			return byteCount;
		}

		// Token: 0x060067D3 RID: 26579 RVA: 0x0015EC3C File Offset: 0x0015CE3C
		[SecurityCritical]
		public unsafe override int GetByteCount(char* chars, int count, bool flush)
		{
			if (chars == null)
			{
				throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			this.m_mustFlush = flush;
			this.m_throwOnOverflow = true;
			return this.m_encoding.GetByteCount(chars, count, this);
		}

		// Token: 0x060067D4 RID: 26580 RVA: 0x0015EC98 File Offset: 0x0015CE98
		[SecuritySafeCritical]
		public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
		{
			if (chars == null || bytes == null)
			{
				throw new ArgumentNullException((chars == null) ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (charIndex < 0 || charCount < 0)
			{
				throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (chars.Length - charIndex < charCount)
			{
				throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			}
			if (byteIndex < 0 || byteIndex > bytes.Length)
			{
				throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (chars.Length == 0)
			{
				chars = new char[1];
			}
			int byteCount = bytes.Length - byteIndex;
			if (bytes.Length == 0)
			{
				bytes = new byte[1];
			}
			char[] array;
			char* ptr;
			if ((array = chars) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			byte[] array2;
			byte* ptr2;
			if ((array2 = bytes) == null || array2.Length == 0)
			{
				ptr2 = null;
			}
			else
			{
				ptr2 = &array2[0];
			}
			return this.GetBytes(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, flush);
		}

		// Token: 0x060067D5 RID: 26581 RVA: 0x0015ED9C File Offset: 0x0015CF9C
		[SecurityCritical]
		public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
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
			return this.m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
		}

		// Token: 0x060067D6 RID: 26582 RVA: 0x0015EE20 File Offset: 0x0015D020
		[SecuritySafeCritical]
		public unsafe override void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
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
			if (chars.Length == 0)
			{
				chars = new char[1];
			}
			if (bytes.Length == 0)
			{
				bytes = new byte[1];
			}
			char[] array;
			char* ptr;
			if ((array = chars) == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			byte[] array2;
			byte* ptr2;
			if ((array2 = bytes) == null || array2.Length == 0)
			{
				ptr2 = null;
			}
			else
			{
				ptr2 = &array2[0];
			}
			this.Convert(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
			array2 = null;
			array = null;
		}

		// Token: 0x060067D7 RID: 26583 RVA: 0x0015EF4C File Offset: 0x0015D14C
		[SecurityCritical]
		public unsafe override void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
		{
			if (bytes == null || chars == null)
			{
				throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (charCount < 0 || byteCount < 0)
			{
				throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			this.m_mustFlush = flush;
			this.m_throwOnOverflow = false;
			this.m_charsUsed = 0;
			bytesUsed = this.m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
			charsUsed = this.m_charsUsed;
			completed = charsUsed == charCount && (!flush || !this.HasState) && (this.m_fallbackBuffer == null || this.m_fallbackBuffer.Remaining == 0);
		}

		// Token: 0x170011B1 RID: 4529
		// (get) Token: 0x060067D8 RID: 26584 RVA: 0x0015F011 File Offset: 0x0015D211
		public Encoding Encoding
		{
			get
			{
				return this.m_encoding;
			}
		}

		// Token: 0x170011B2 RID: 4530
		// (get) Token: 0x060067D9 RID: 26585 RVA: 0x0015F019 File Offset: 0x0015D219
		public bool MustFlush
		{
			get
			{
				return this.m_mustFlush;
			}
		}

		// Token: 0x170011B3 RID: 4531
		// (get) Token: 0x060067DA RID: 26586 RVA: 0x0015F021 File Offset: 0x0015D221
		internal virtual bool HasState
		{
			get
			{
				return this.charLeftOver > '\0';
			}
		}

		// Token: 0x060067DB RID: 26587 RVA: 0x0015F02C File Offset: 0x0015D22C
		internal void ClearMustFlush()
		{
			this.m_mustFlush = false;
		}

		// Token: 0x04002E57 RID: 11863
		internal char charLeftOver;

		// Token: 0x04002E58 RID: 11864
		protected Encoding m_encoding;

		// Token: 0x04002E59 RID: 11865
		[NonSerialized]
		protected bool m_mustFlush;

		// Token: 0x04002E5A RID: 11866
		[NonSerialized]
		internal bool m_throwOnOverflow;

		// Token: 0x04002E5B RID: 11867
		[NonSerialized]
		internal int m_charsUsed;
	}
}
