using System;
using System.Globalization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A65 RID: 2661
	[__DynamicallyInvokable]
	public abstract class DecoderFallbackBuffer
	{
		// Token: 0x060067A7 RID: 26535
		[__DynamicallyInvokable]
		public abstract bool Fallback(byte[] bytesUnknown, int index);

		// Token: 0x060067A8 RID: 26536
		[__DynamicallyInvokable]
		public abstract char GetNextChar();

		// Token: 0x060067A9 RID: 26537
		[__DynamicallyInvokable]
		public abstract bool MovePrevious();

		// Token: 0x170011AA RID: 4522
		// (get) Token: 0x060067AA RID: 26538
		[__DynamicallyInvokable]
		public abstract int Remaining
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060067AB RID: 26539 RVA: 0x0015E2BC File Offset: 0x0015C4BC
		[__DynamicallyInvokable]
		public virtual void Reset()
		{
			while (this.GetNextChar() != '\0')
			{
			}
		}

		// Token: 0x060067AC RID: 26540 RVA: 0x0015E2C6 File Offset: 0x0015C4C6
		[SecurityCritical]
		internal void InternalReset()
		{
			this.byteStart = null;
			this.Reset();
		}

		// Token: 0x060067AD RID: 26541 RVA: 0x0015E2D6 File Offset: 0x0015C4D6
		[SecurityCritical]
		internal unsafe void InternalInitialize(byte* byteStart, char* charEnd)
		{
			this.byteStart = byteStart;
			this.charEnd = charEnd;
		}

		// Token: 0x060067AE RID: 26542 RVA: 0x0015E2E8 File Offset: 0x0015C4E8
		[SecurityCritical]
		internal unsafe virtual bool InternalFallback(byte[] bytes, byte* pBytes, ref char* chars)
		{
			if (this.Fallback(bytes, (int)((long)(pBytes - this.byteStart) - (long)bytes.Length)))
			{
				char* ptr = chars;
				bool flag = false;
				char nextChar;
				while ((nextChar = this.GetNextChar()) != '\0')
				{
					if (char.IsSurrogate(nextChar))
					{
						if (char.IsHighSurrogate(nextChar))
						{
							if (flag)
							{
								throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
							}
							flag = true;
						}
						else
						{
							if (!flag)
							{
								throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
							}
							flag = false;
						}
					}
					if (ptr >= this.charEnd)
					{
						return false;
					}
					*(ptr++) = nextChar;
				}
				if (flag)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
				}
				chars = ptr;
			}
			return true;
		}

		// Token: 0x060067AF RID: 26543 RVA: 0x0015E388 File Offset: 0x0015C588
		[SecurityCritical]
		internal unsafe virtual int InternalFallback(byte[] bytes, byte* pBytes)
		{
			if (!this.Fallback(bytes, (int)((long)(pBytes - this.byteStart) - (long)bytes.Length)))
			{
				return 0;
			}
			int num = 0;
			bool flag = false;
			char nextChar;
			while ((nextChar = this.GetNextChar()) != '\0')
			{
				if (char.IsSurrogate(nextChar))
				{
					if (char.IsHighSurrogate(nextChar))
					{
						if (flag)
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
						}
						flag = true;
					}
					else
					{
						if (!flag)
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
						}
						flag = false;
					}
				}
				num++;
			}
			if (flag)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
			}
			return num;
		}

		// Token: 0x060067B0 RID: 26544 RVA: 0x0015E418 File Offset: 0x0015C618
		internal void ThrowLastBytesRecursive(byte[] bytesUnknown)
		{
			StringBuilder stringBuilder = new StringBuilder(bytesUnknown.Length * 3);
			int num = 0;
			while (num < bytesUnknown.Length && num < 20)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "\\x{0:X2}", bytesUnknown[num]));
				num++;
			}
			if (num == 20)
			{
				stringBuilder.Append(" ...");
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_RecursiveFallbackBytes", new object[] { stringBuilder.ToString() }), "bytesUnknown");
		}

		// Token: 0x060067B1 RID: 26545 RVA: 0x0015E4AA File Offset: 0x0015C6AA
		[__DynamicallyInvokable]
		protected DecoderFallbackBuffer()
		{
		}

		// Token: 0x04002E4F RID: 11855
		[SecurityCritical]
		internal unsafe byte* byteStart;

		// Token: 0x04002E50 RID: 11856
		[SecurityCritical]
		internal unsafe char* charEnd;
	}
}
