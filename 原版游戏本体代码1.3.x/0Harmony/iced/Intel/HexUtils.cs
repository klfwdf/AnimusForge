using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000644 RID: 1604
	internal static class HexUtils
	{
		// Token: 0x0600217A RID: 8570 RVA: 0x0006D0D8 File Offset: 0x0006B2D8
		[NullableContext(1)]
		public static byte[] ToByteArray(string hexData)
		{
			if (hexData == null)
			{
				throw new ArgumentNullException("hexData");
			}
			if (hexData.Length == 0)
			{
				return Array2.Empty<byte>();
			}
			int len = 0;
			for (int i = 0; i < hexData.Length; i++)
			{
				if (hexData[i] != ' ')
				{
					len++;
				}
			}
			byte[] data = new byte[len / 2];
			int w = 0;
			int j = 0;
			for (;;)
			{
				if (j >= hexData.Length || !char.IsWhiteSpace(hexData[j]))
				{
					if (j >= hexData.Length)
					{
						goto IL_10A;
					}
					int hi = HexUtils.TryParseHexChar(hexData[j++]);
					if (hi < 0)
					{
						break;
					}
					while (j < hexData.Length && char.IsWhiteSpace(hexData[j]))
					{
						j++;
					}
					if (j >= hexData.Length)
					{
						goto Block_9;
					}
					int lo = HexUtils.TryParseHexChar(hexData[j++]);
					if (lo < 0)
					{
						goto Block_10;
					}
					data[w++] = (byte)((hi << 4) | lo);
				}
				else
				{
					j++;
				}
			}
			throw new ArgumentOutOfRangeException("hexData");
			Block_9:
			throw new ArgumentOutOfRangeException("hexData");
			Block_10:
			throw new ArgumentOutOfRangeException("hexData");
			IL_10A:
			if (w != data.Length)
			{
				throw new InvalidOperationException();
			}
			return data;
		}

		// Token: 0x0600217B RID: 8571 RVA: 0x0006D1FC File Offset: 0x0006B3FC
		private static int TryParseHexChar(char c)
		{
			if ('0' <= c && c <= '9')
			{
				return (int)(c - '0');
			}
			if ('A' <= c && c <= 'F')
			{
				return (int)(c - 'A' + '\n');
			}
			if ('a' <= c && c <= 'f')
			{
				return (int)(c - 'a' + '\n');
			}
			return -1;
		}
	}
}
