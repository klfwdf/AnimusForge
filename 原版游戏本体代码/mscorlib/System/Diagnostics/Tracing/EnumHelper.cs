using System;
using System.Reflection;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000440 RID: 1088
	internal static class EnumHelper<UnderlyingType>
	{
		// Token: 0x060035F8 RID: 13816 RVA: 0x000D241C File Offset: 0x000D061C
		public static UnderlyingType Cast<ValueType>(ValueType value)
		{
			return EnumHelper<UnderlyingType>.Caster<ValueType>.Instance(value);
		}

		// Token: 0x060035F9 RID: 13817 RVA: 0x000D2429 File Offset: 0x000D0629
		internal static UnderlyingType Identity(UnderlyingType value)
		{
			return value;
		}

		// Token: 0x0400181D RID: 6173
		private static readonly MethodInfo IdentityInfo = Statics.GetDeclaredStaticMethod(typeof(EnumHelper<UnderlyingType>), "Identity");

		// Token: 0x02000B9A RID: 2970
		// (Invoke) Token: 0x06006C96 RID: 27798
		private delegate UnderlyingType Transformer<ValueType>(ValueType value);

		// Token: 0x02000B9B RID: 2971
		private static class Caster<ValueType>
		{
			// Token: 0x0400352E RID: 13614
			public static readonly EnumHelper<UnderlyingType>.Transformer<ValueType> Instance = (EnumHelper<UnderlyingType>.Transformer<ValueType>)Statics.CreateDelegate(typeof(EnumHelper<UnderlyingType>.Transformer<ValueType>), EnumHelper<UnderlyingType>.IdentityInfo);
		}
	}
}
