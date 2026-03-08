using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

namespace System.Resources
{
	// Token: 0x0200038A RID: 906
	internal sealed class FastResourceComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
	{
		// Token: 0x06002CDC RID: 11484 RVA: 0x000A9280 File Offset: 0x000A7480
		public int GetHashCode(object key)
		{
			string key2 = (string)key;
			return FastResourceComparer.HashFunction(key2);
		}

		// Token: 0x06002CDD RID: 11485 RVA: 0x000A929A File Offset: 0x000A749A
		public int GetHashCode(string key)
		{
			return FastResourceComparer.HashFunction(key);
		}

		// Token: 0x06002CDE RID: 11486 RVA: 0x000A92A4 File Offset: 0x000A74A4
		internal static int HashFunction(string key)
		{
			uint num = 5381U;
			for (int i = 0; i < key.Length; i++)
			{
				num = ((num << 5) + num) ^ (uint)key[i];
			}
			return (int)num;
		}

		// Token: 0x06002CDF RID: 11487 RVA: 0x000A92D8 File Offset: 0x000A74D8
		public int Compare(object a, object b)
		{
			if (a == b)
			{
				return 0;
			}
			string strA = (string)a;
			string strB = (string)b;
			return string.CompareOrdinal(strA, strB);
		}

		// Token: 0x06002CE0 RID: 11488 RVA: 0x000A9300 File Offset: 0x000A7500
		public int Compare(string a, string b)
		{
			return string.CompareOrdinal(a, b);
		}

		// Token: 0x06002CE1 RID: 11489 RVA: 0x000A9309 File Offset: 0x000A7509
		public bool Equals(string a, string b)
		{
			return string.Equals(a, b);
		}

		// Token: 0x06002CE2 RID: 11490 RVA: 0x000A9314 File Offset: 0x000A7514
		public bool Equals(object a, object b)
		{
			if (a == b)
			{
				return true;
			}
			string a2 = (string)a;
			string b2 = (string)b;
			return string.Equals(a2, b2);
		}

		// Token: 0x06002CE3 RID: 11491 RVA: 0x000A933C File Offset: 0x000A753C
		[SecurityCritical]
		public unsafe static int CompareOrdinal(string a, byte[] bytes, int bCharLength)
		{
			int num = 0;
			int num2 = 0;
			int num3 = a.Length;
			if (num3 > bCharLength)
			{
				num3 = bCharLength;
			}
			if (bCharLength == 0)
			{
				if (a.Length != 0)
				{
					return -1;
				}
				return 0;
			}
			else
			{
				fixed (byte[] array = bytes)
				{
					byte* ptr;
					if (bytes == null || array.Length == 0)
					{
						ptr = null;
					}
					else
					{
						ptr = &array[0];
					}
					byte* ptr2 = ptr;
					while (num < num3 && num2 == 0)
					{
						int num4 = (int)(*ptr2) | ((int)ptr2[1] << 8);
						num2 = (int)a[num++] - num4;
						ptr2 += 2;
					}
				}
				if (num2 != 0)
				{
					return num2;
				}
				return a.Length - bCharLength;
			}
		}

		// Token: 0x06002CE4 RID: 11492 RVA: 0x000A93C2 File Offset: 0x000A75C2
		[SecurityCritical]
		public static int CompareOrdinal(byte[] bytes, int aCharLength, string b)
		{
			return -FastResourceComparer.CompareOrdinal(b, bytes, aCharLength);
		}

		// Token: 0x06002CE5 RID: 11493 RVA: 0x000A93D0 File Offset: 0x000A75D0
		[SecurityCritical]
		internal unsafe static int CompareOrdinal(byte* a, int byteLen, string b)
		{
			int num = 0;
			int num2 = 0;
			int num3 = byteLen >> 1;
			if (num3 > b.Length)
			{
				num3 = b.Length;
			}
			while (num2 < num3 && num == 0)
			{
				char c = (char)((int)(*(a++)) | ((int)(*(a++)) << 8));
				num = (int)(c - b[num2++]);
			}
			if (num != 0)
			{
				return num;
			}
			return byteLen - b.Length * 2;
		}

		// Token: 0x0400122A RID: 4650
		internal static readonly FastResourceComparer Default = new FastResourceComparer();
	}
}
