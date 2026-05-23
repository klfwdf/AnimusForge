using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009EF RID: 2543
	internal sealed class BindableIterableToEnumerableAdapter
	{
		// Token: 0x060064B6 RID: 25782 RVA: 0x001571ED File Offset: 0x001553ED
		private BindableIterableToEnumerableAdapter()
		{
		}

		// Token: 0x060064B7 RID: 25783 RVA: 0x001571F8 File Offset: 0x001553F8
		[SecurityCritical]
		internal IEnumerator GetEnumerator_Stub()
		{
			IBindableIterable bindableIterable = JitHelpers.UnsafeCast<IBindableIterable>(this);
			return new IteratorToEnumeratorAdapter<object>(new BindableIterableToEnumerableAdapter.NonGenericToGenericIterator(bindableIterable.First()));
		}

		// Token: 0x02000CA4 RID: 3236
		private sealed class NonGenericToGenericIterator : IIterator<object>
		{
			// Token: 0x06007137 RID: 28983 RVA: 0x00185915 File Offset: 0x00183B15
			public NonGenericToGenericIterator(IBindableIterator iterator)
			{
				this.iterator = iterator;
			}

			// Token: 0x1700136A RID: 4970
			// (get) Token: 0x06007138 RID: 28984 RVA: 0x00185924 File Offset: 0x00183B24
			public object Current
			{
				get
				{
					return this.iterator.Current;
				}
			}

			// Token: 0x1700136B RID: 4971
			// (get) Token: 0x06007139 RID: 28985 RVA: 0x00185931 File Offset: 0x00183B31
			public bool HasCurrent
			{
				get
				{
					return this.iterator.HasCurrent;
				}
			}

			// Token: 0x0600713A RID: 28986 RVA: 0x0018593E File Offset: 0x00183B3E
			public bool MoveNext()
			{
				return this.iterator.MoveNext();
			}

			// Token: 0x0600713B RID: 28987 RVA: 0x0018594B File Offset: 0x00183B4B
			public int GetMany(object[] items)
			{
				throw new NotSupportedException();
			}

			// Token: 0x04003885 RID: 14469
			private IBindableIterator iterator;
		}
	}
}
