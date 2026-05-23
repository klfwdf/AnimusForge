using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000163 RID: 355
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public static class Nullable
	{
		// Token: 0x060015D9 RID: 5593 RVA: 0x0004049A File Offset: 0x0003E69A
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static int Compare<T>(T? n1, T? n2) where T : struct
		{
			if (n1 != null)
			{
				if (n2 != null)
				{
					return Comparer<T>.Default.Compare(n1.value, n2.value);
				}
				return 1;
			}
			else
			{
				if (n2 != null)
				{
					return -1;
				}
				return 0;
			}
		}

		// Token: 0x060015DA RID: 5594 RVA: 0x000404D3 File Offset: 0x0003E6D3
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static bool Equals<T>(T? n1, T? n2) where T : struct
		{
			if (n1 != null)
			{
				return n2 != null && EqualityComparer<T>.Default.Equals(n1.value, n2.value);
			}
			return n2 == null;
		}

		// Token: 0x060015DB RID: 5595 RVA: 0x0004050C File Offset: 0x0003E70C
		[__DynamicallyInvokable]
		public static Type GetUnderlyingType(Type nullableType)
		{
			if (nullableType == null)
			{
				throw new ArgumentNullException("nullableType");
			}
			Type result = null;
			if (nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition)
			{
				Type genericTypeDefinition = nullableType.GetGenericTypeDefinition();
				if (genericTypeDefinition == typeof(Nullable<>))
				{
					result = nullableType.GetGenericArguments()[0];
				}
			}
			return result;
		}
	}
}
