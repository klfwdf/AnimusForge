using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.StubHelpers;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009EA RID: 2538
	[DebuggerDisplay("Count = {Count}")]
	internal sealed class IVectorViewToIReadOnlyListAdapter
	{
		// Token: 0x060064A1 RID: 25761 RVA: 0x00156E5F File Offset: 0x0015505F
		private IVectorViewToIReadOnlyListAdapter()
		{
		}

		// Token: 0x060064A2 RID: 25762 RVA: 0x00156E68 File Offset: 0x00155068
		[SecurityCritical]
		internal T Indexer_Get<T>(int index)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IVectorView<T> vectorView = JitHelpers.UnsafeCast<IVectorView<T>>(this);
			T at;
			try
			{
				at = vectorView.GetAt((uint)index);
			}
			catch (Exception ex)
			{
				if (-2147483637 == ex._HResult)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				throw;
			}
			return at;
		}

		// Token: 0x060064A3 RID: 25763 RVA: 0x00156EC4 File Offset: 0x001550C4
		[SecurityCritical]
		internal T Indexer_Get_Variance<T>(int index) where T : class
		{
			bool flag;
			Delegate targetForAmbiguousVariantCall = StubHelpers.GetTargetForAmbiguousVariantCall(this, typeof(IReadOnlyList<T>).TypeHandle.Value, out flag);
			if (targetForAmbiguousVariantCall != null)
			{
				return JitHelpers.UnsafeCast<Indexer_Get_Delegate<T>>(targetForAmbiguousVariantCall)(index);
			}
			if (flag)
			{
				return JitHelpers.UnsafeCast<T>(this.Indexer_Get<string>(index));
			}
			return this.Indexer_Get<T>(index);
		}
	}
}
