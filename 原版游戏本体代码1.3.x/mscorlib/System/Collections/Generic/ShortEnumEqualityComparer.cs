using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
	// Token: 0x020004C7 RID: 1223
	[Serializable]
	internal sealed class ShortEnumEqualityComparer<T> : EnumEqualityComparer<T>, ISerializable where T : struct
	{
		// Token: 0x06003AA0 RID: 15008 RVA: 0x000DF702 File Offset: 0x000DD902
		public ShortEnumEqualityComparer()
		{
		}

		// Token: 0x06003AA1 RID: 15009 RVA: 0x000DF70A File Offset: 0x000DD90A
		public ShortEnumEqualityComparer(SerializationInfo information, StreamingContext context)
		{
		}

		// Token: 0x06003AA2 RID: 15010 RVA: 0x000DF714 File Offset: 0x000DD914
		public override int GetHashCode(T obj)
		{
			int num = JitHelpers.UnsafeEnumCast<T>(obj);
			return ((short)num).GetHashCode();
		}
	}
}
