using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D2 RID: 2514
	internal sealed class EnumerableToBindableIterableAdapter
	{
		// Token: 0x06006401 RID: 25601 RVA: 0x00155143 File Offset: 0x00153343
		private EnumerableToBindableIterableAdapter()
		{
		}

		// Token: 0x06006402 RID: 25602 RVA: 0x0015514C File Offset: 0x0015334C
		[SecurityCritical]
		internal IBindableIterator First_Stub()
		{
			IEnumerable enumerable = JitHelpers.UnsafeCast<IEnumerable>(this);
			return new EnumeratorToIteratorAdapter<object>(new EnumerableToBindableIterableAdapter.NonGenericToGenericEnumerator(enumerable.GetEnumerator()));
		}

		// Token: 0x02000CA3 RID: 3235
		internal sealed class NonGenericToGenericEnumerator : IEnumerator<object>, IDisposable, IEnumerator
		{
			// Token: 0x06007132 RID: 28978 RVA: 0x001858DD File Offset: 0x00183ADD
			public NonGenericToGenericEnumerator(IEnumerator enumerator)
			{
				this.enumerator = enumerator;
			}

			// Token: 0x17001369 RID: 4969
			// (get) Token: 0x06007133 RID: 28979 RVA: 0x001858EC File Offset: 0x00183AEC
			public object Current
			{
				get
				{
					return this.enumerator.Current;
				}
			}

			// Token: 0x06007134 RID: 28980 RVA: 0x001858F9 File Offset: 0x00183AF9
			public bool MoveNext()
			{
				return this.enumerator.MoveNext();
			}

			// Token: 0x06007135 RID: 28981 RVA: 0x00185906 File Offset: 0x00183B06
			public void Reset()
			{
				this.enumerator.Reset();
			}

			// Token: 0x06007136 RID: 28982 RVA: 0x00185913 File Offset: 0x00183B13
			public void Dispose()
			{
			}

			// Token: 0x04003884 RID: 14468
			private IEnumerator enumerator;
		}
	}
}
