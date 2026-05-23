using System;
using System.Security;
using System.Threading;

namespace System.Text
{
	// Token: 0x02000A60 RID: 2656
	internal sealed class InternalDecoderBestFitFallbackBuffer : DecoderFallbackBuffer
	{
		// Token: 0x1700119F RID: 4511
		// (get) Token: 0x06006785 RID: 26501 RVA: 0x0015DE00 File Offset: 0x0015C000
		private static object InternalSyncObject
		{
			get
			{
				if (InternalDecoderBestFitFallbackBuffer.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange<object>(ref InternalDecoderBestFitFallbackBuffer.s_InternalSyncObject, value, null);
				}
				return InternalDecoderBestFitFallbackBuffer.s_InternalSyncObject;
			}
		}

		// Token: 0x06006786 RID: 26502 RVA: 0x0015DE2C File Offset: 0x0015C02C
		public InternalDecoderBestFitFallbackBuffer(InternalDecoderBestFitFallback fallback)
		{
			this.oFallback = fallback;
			if (this.oFallback.arrayBestFit == null)
			{
				object internalSyncObject = InternalDecoderBestFitFallbackBuffer.InternalSyncObject;
				lock (internalSyncObject)
				{
					if (this.oFallback.arrayBestFit == null)
					{
						this.oFallback.arrayBestFit = fallback.encoding.GetBestFitBytesToUnicodeData();
					}
				}
			}
		}

		// Token: 0x06006787 RID: 26503 RVA: 0x0015DEAC File Offset: 0x0015C0AC
		public override bool Fallback(byte[] bytesUnknown, int index)
		{
			this.cBestFit = this.TryBestFit(bytesUnknown);
			if (this.cBestFit == '\0')
			{
				this.cBestFit = this.oFallback.cReplacement;
			}
			this.iCount = (this.iSize = 1);
			return true;
		}

		// Token: 0x06006788 RID: 26504 RVA: 0x0015DEF0 File Offset: 0x0015C0F0
		public override char GetNextChar()
		{
			this.iCount--;
			if (this.iCount < 0)
			{
				return '\0';
			}
			if (this.iCount == 2147483647)
			{
				this.iCount = -1;
				return '\0';
			}
			return this.cBestFit;
		}

		// Token: 0x06006789 RID: 26505 RVA: 0x0015DF27 File Offset: 0x0015C127
		public override bool MovePrevious()
		{
			if (this.iCount >= 0)
			{
				this.iCount++;
			}
			return this.iCount >= 0 && this.iCount <= this.iSize;
		}

		// Token: 0x170011A0 RID: 4512
		// (get) Token: 0x0600678A RID: 26506 RVA: 0x0015DF5C File Offset: 0x0015C15C
		public override int Remaining
		{
			get
			{
				if (this.iCount <= 0)
				{
					return 0;
				}
				return this.iCount;
			}
		}

		// Token: 0x0600678B RID: 26507 RVA: 0x0015DF6F File Offset: 0x0015C16F
		[SecuritySafeCritical]
		public override void Reset()
		{
			this.iCount = -1;
			this.byteStart = null;
		}

		// Token: 0x0600678C RID: 26508 RVA: 0x0015DF80 File Offset: 0x0015C180
		[SecurityCritical]
		internal unsafe override int InternalFallback(byte[] bytes, byte* pBytes)
		{
			return 1;
		}

		// Token: 0x0600678D RID: 26509 RVA: 0x0015DF84 File Offset: 0x0015C184
		private char TryBestFit(byte[] bytesCheck)
		{
			int num = 0;
			int num2 = this.oFallback.arrayBestFit.Length;
			if (num2 == 0)
			{
				return '\0';
			}
			if (bytesCheck.Length == 0 || bytesCheck.Length > 2)
			{
				return '\0';
			}
			char c;
			if (bytesCheck.Length == 1)
			{
				c = (char)bytesCheck[0];
			}
			else
			{
				c = (char)(((int)bytesCheck[0] << 8) + (int)bytesCheck[1]);
			}
			if (c < this.oFallback.arrayBestFit[0] || c > this.oFallback.arrayBestFit[num2 - 2])
			{
				return '\0';
			}
			int num3;
			while ((num3 = num2 - num) > 6)
			{
				int i = (num3 / 2 + num) & 65534;
				char c2 = this.oFallback.arrayBestFit[i];
				if (c2 == c)
				{
					return this.oFallback.arrayBestFit[i + 1];
				}
				if (c2 < c)
				{
					num = i;
				}
				else
				{
					num2 = i;
				}
			}
			for (int i = num; i < num2; i += 2)
			{
				if (this.oFallback.arrayBestFit[i] == c)
				{
					return this.oFallback.arrayBestFit[i + 1];
				}
			}
			return '\0';
		}

		// Token: 0x04002E44 RID: 11844
		internal char cBestFit;

		// Token: 0x04002E45 RID: 11845
		internal int iCount = -1;

		// Token: 0x04002E46 RID: 11846
		internal int iSize;

		// Token: 0x04002E47 RID: 11847
		private InternalDecoderBestFitFallback oFallback;

		// Token: 0x04002E48 RID: 11848
		private static object s_InternalSyncObject;
	}
}
