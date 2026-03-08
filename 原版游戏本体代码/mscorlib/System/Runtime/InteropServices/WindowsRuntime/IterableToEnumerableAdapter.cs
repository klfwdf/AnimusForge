using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.StubHelpers;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009EE RID: 2542
	internal sealed class IterableToEnumerableAdapter
	{
		// Token: 0x060064B3 RID: 25779 RVA: 0x00157173 File Offset: 0x00155373
		private IterableToEnumerableAdapter()
		{
		}

		// Token: 0x060064B4 RID: 25780 RVA: 0x0015717C File Offset: 0x0015537C
		[SecurityCritical]
		internal IEnumerator<T> GetEnumerator_Stub<T>()
		{
			IIterable<T> iterable = JitHelpers.UnsafeCast<IIterable<T>>(this);
			return new IteratorToEnumeratorAdapter<T>(iterable.First());
		}

		// Token: 0x060064B5 RID: 25781 RVA: 0x0015719C File Offset: 0x0015539C
		[SecurityCritical]
		internal IEnumerator<T> GetEnumerator_Variance_Stub<T>() where T : class
		{
			bool flag;
			Delegate targetForAmbiguousVariantCall = StubHelpers.GetTargetForAmbiguousVariantCall(this, typeof(IEnumerable<T>).TypeHandle.Value, out flag);
			if (targetForAmbiguousVariantCall != null)
			{
				return JitHelpers.UnsafeCast<GetEnumerator_Delegate<T>>(targetForAmbiguousVariantCall)();
			}
			if (flag)
			{
				return JitHelpers.UnsafeCast<IEnumerator<T>>(this.GetEnumerator_Stub<string>());
			}
			return this.GetEnumerator_Stub<T>();
		}
	}
}
