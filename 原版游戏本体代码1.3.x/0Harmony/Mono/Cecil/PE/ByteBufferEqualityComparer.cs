using System;
using System.Collections.Generic;

namespace Mono.Cecil.PE
{
	// Token: 0x020002C0 RID: 704
	internal sealed class ByteBufferEqualityComparer : IEqualityComparer<ByteBuffer>
	{
		// Token: 0x06001224 RID: 4644 RVA: 0x00037FCC File Offset: 0x000361CC
		public bool Equals(ByteBuffer x, ByteBuffer y)
		{
			if (x.length != y.length)
			{
				return false;
			}
			byte[] x_buffer = x.buffer;
			byte[] y_buffer = y.buffer;
			for (int i = 0; i < x.length; i++)
			{
				if (x_buffer[i] != y_buffer[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001225 RID: 4645 RVA: 0x00038014 File Offset: 0x00036214
		public int GetHashCode(ByteBuffer buffer)
		{
			int hash_code = -2128831035;
			byte[] bytes = buffer.buffer;
			for (int i = 0; i < buffer.length; i++)
			{
				hash_code = (hash_code ^ (int)bytes[i]) * 16777619;
			}
			return hash_code;
		}
	}
}
