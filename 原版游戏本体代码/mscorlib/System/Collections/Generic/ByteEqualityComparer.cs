using System;
using System.Security;

namespace System.Collections.Generic
{
	// Token: 0x020004C4 RID: 1220
	[Serializable]
	internal class ByteEqualityComparer : EqualityComparer<byte>
	{
		// Token: 0x06003A8F RID: 14991 RVA: 0x000DF52E File Offset: 0x000DD72E
		public override bool Equals(byte x, byte y)
		{
			return x == y;
		}

		// Token: 0x06003A90 RID: 14992 RVA: 0x000DF534 File Offset: 0x000DD734
		public override int GetHashCode(byte b)
		{
			return b.GetHashCode();
		}

		// Token: 0x06003A91 RID: 14993 RVA: 0x000DF540 File Offset: 0x000DD740
		[SecuritySafeCritical]
		internal unsafe override int IndexOf(byte[] array, byte value, int startIndex, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
			}
			if (count > array.Length - startIndex)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			if (count == 0)
			{
				return -1;
			}
			byte* src;
			if (array == null || array.Length == 0)
			{
				src = null;
			}
			else
			{
				src = &array[0];
			}
			return Buffer.IndexOfByte(src, value, startIndex, count);
		}

		// Token: 0x06003A92 RID: 14994 RVA: 0x000DF5D0 File Offset: 0x000DD7D0
		internal override int LastIndexOf(byte[] array, byte value, int startIndex, int count)
		{
			int num = startIndex - count + 1;
			for (int i = startIndex; i >= num; i--)
			{
				if (array[i] == value)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06003A93 RID: 14995 RVA: 0x000DF5FC File Offset: 0x000DD7FC
		public override bool Equals(object obj)
		{
			ByteEqualityComparer byteEqualityComparer = obj as ByteEqualityComparer;
			return byteEqualityComparer != null;
		}

		// Token: 0x06003A94 RID: 14996 RVA: 0x000DF614 File Offset: 0x000DD814
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
