using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Collections.Generic
{
	// Token: 0x020004C8 RID: 1224
	[Serializable]
	internal sealed class LongEnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
	{
		// Token: 0x06003AA3 RID: 15011 RVA: 0x000DF734 File Offset: 0x000DD934
		public override bool Equals(T x, T y)
		{
			long num = JitHelpers.UnsafeEnumCastLong<T>(x);
			long num2 = JitHelpers.UnsafeEnumCastLong<T>(y);
			return num == num2;
		}

		// Token: 0x06003AA4 RID: 15012 RVA: 0x000DF754 File Offset: 0x000DD954
		public override int GetHashCode(T obj)
		{
			return JitHelpers.UnsafeEnumCastLong<T>(obj).GetHashCode();
		}

		// Token: 0x06003AA5 RID: 15013 RVA: 0x000DF770 File Offset: 0x000DD970
		public override bool Equals(object obj)
		{
			LongEnumEqualityComparer<T> longEnumEqualityComparer = obj as LongEnumEqualityComparer<T>;
			return longEnumEqualityComparer != null;
		}

		// Token: 0x06003AA6 RID: 15014 RVA: 0x000DF788 File Offset: 0x000DD988
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}

		// Token: 0x06003AA7 RID: 15015 RVA: 0x000DF79A File Offset: 0x000DD99A
		public LongEnumEqualityComparer()
		{
		}

		// Token: 0x06003AA8 RID: 15016 RVA: 0x000DF7A2 File Offset: 0x000DD9A2
		public LongEnumEqualityComparer(SerializationInfo information, StreamingContext context)
		{
		}

		// Token: 0x06003AA9 RID: 15017 RVA: 0x000DF7AA File Offset: 0x000DD9AA
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.SetType(typeof(ObjectEqualityComparer<T>));
		}
	}
}
