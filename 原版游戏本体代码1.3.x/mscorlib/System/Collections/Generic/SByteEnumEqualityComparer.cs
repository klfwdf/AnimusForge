using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
	// Token: 0x020004C6 RID: 1222
	[Serializable]
	internal sealed class SByteEnumEqualityComparer<T> : EnumEqualityComparer<T>, ISerializable where T : struct
	{
		// Token: 0x06003A9D RID: 15005 RVA: 0x000DF6D2 File Offset: 0x000DD8D2
		public SByteEnumEqualityComparer()
		{
		}

		// Token: 0x06003A9E RID: 15006 RVA: 0x000DF6DA File Offset: 0x000DD8DA
		public SByteEnumEqualityComparer(SerializationInfo information, StreamingContext context)
		{
		}

		// Token: 0x06003A9F RID: 15007 RVA: 0x000DF6E4 File Offset: 0x000DD8E4
		public override int GetHashCode(T obj)
		{
			int num = JitHelpers.UnsafeEnumCast<T>(obj);
			return ((sbyte)num).GetHashCode();
		}
	}
}
