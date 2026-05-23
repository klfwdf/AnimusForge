using System;
using System.Collections.Generic;

namespace Mono.Cecil.Rocks
{
	// Token: 0x0200044C RID: 1100
	internal static class Functional
	{
		// Token: 0x060017E6 RID: 6118 RVA: 0x0004B5D0 File Offset: 0x000497D0
		public static Func<A, R> Y<A, R>(Func<Func<A, R>, Func<A, R>> f)
		{
			Func<A, R> g = null;
			g = f((A a) => g(a));
			return g;
		}

		// Token: 0x060017E7 RID: 6119 RVA: 0x0004B608 File Offset: 0x00049808
		public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return Functional.PrependIterator<TSource>(source, element);
		}

		// Token: 0x060017E8 RID: 6120 RVA: 0x0004B61F File Offset: 0x0004981F
		private static IEnumerable<TSource> PrependIterator<TSource>(IEnumerable<TSource> source, TSource element)
		{
			yield return element;
			foreach (TSource item in source)
			{
				yield return item;
			}
			IEnumerator<TSource> enumerator = null;
			yield break;
			yield break;
		}
	}
}
