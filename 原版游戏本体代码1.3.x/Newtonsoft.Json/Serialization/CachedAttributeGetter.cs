using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	// Token: 0x02000072 RID: 114
	internal static class CachedAttributeGetter<T> where T : Attribute
	{
		// Token: 0x06000611 RID: 1553 RVA: 0x00019B48 File Offset: 0x00017D48
		[NullableContext(1)]
		[return: Nullable(2)]
		public static T GetAttribute(object type)
		{
			return CachedAttributeGetter<T>.TypeAttributeCache.Get(type);
		}

		// Token: 0x0400022A RID: 554
		[Nullable(new byte[] { 1, 1, 2 })]
		private static readonly ThreadSafeStore<object, T> TypeAttributeCache = new ThreadSafeStore<object, T>(new Func<object, T>(JsonTypeReflector.GetAttribute<T>));
	}
}
