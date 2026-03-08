using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Collections.Generic
{
	// Token: 0x020004C5 RID: 1221
	[Serializable]
	internal class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
	{
		// Token: 0x06003A96 RID: 14998 RVA: 0x000DF630 File Offset: 0x000DD830
		public override bool Equals(T x, T y)
		{
			int num = JitHelpers.UnsafeEnumCast<T>(x);
			int num2 = JitHelpers.UnsafeEnumCast<T>(y);
			return num == num2;
		}

		// Token: 0x06003A97 RID: 14999 RVA: 0x000DF650 File Offset: 0x000DD850
		public override int GetHashCode(T obj)
		{
			return JitHelpers.UnsafeEnumCast<T>(obj).GetHashCode();
		}

		// Token: 0x06003A98 RID: 15000 RVA: 0x000DF66B File Offset: 0x000DD86B
		public EnumEqualityComparer()
		{
		}

		// Token: 0x06003A99 RID: 15001 RVA: 0x000DF673 File Offset: 0x000DD873
		protected EnumEqualityComparer(SerializationInfo information, StreamingContext context)
		{
		}

		// Token: 0x06003A9A RID: 15002 RVA: 0x000DF67B File Offset: 0x000DD87B
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.Int32)
			{
				info.SetType(typeof(ObjectEqualityComparer<T>));
			}
		}

		// Token: 0x06003A9B RID: 15003 RVA: 0x000DF6A8 File Offset: 0x000DD8A8
		public override bool Equals(object obj)
		{
			EnumEqualityComparer<T> enumEqualityComparer = obj as EnumEqualityComparer<T>;
			return enumEqualityComparer != null;
		}

		// Token: 0x06003A9C RID: 15004 RVA: 0x000DF6C0 File Offset: 0x000DD8C0
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode();
		}
	}
}
