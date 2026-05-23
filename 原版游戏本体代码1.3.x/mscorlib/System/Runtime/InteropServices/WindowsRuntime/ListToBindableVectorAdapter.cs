using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009DE RID: 2526
	internal sealed class ListToBindableVectorAdapter
	{
		// Token: 0x0600645E RID: 25694 RVA: 0x001563AD File Offset: 0x001545AD
		private ListToBindableVectorAdapter()
		{
		}

		// Token: 0x0600645F RID: 25695 RVA: 0x001563B8 File Offset: 0x001545B8
		[SecurityCritical]
		internal object GetAt(uint index)
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			ListToBindableVectorAdapter.EnsureIndexInt32(index, list.Count);
			object result;
			try
			{
				result = list[(int)index];
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw WindowsRuntimeMarshal.GetExceptionForHR(-2147483637, innerException, "ArgumentOutOfRange_IndexOutOfRange");
			}
			return result;
		}

		// Token: 0x06006460 RID: 25696 RVA: 0x00156408 File Offset: 0x00154608
		[SecurityCritical]
		internal uint Size()
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			return (uint)list.Count;
		}

		// Token: 0x06006461 RID: 25697 RVA: 0x00156424 File Offset: 0x00154624
		[SecurityCritical]
		internal IBindableVectorView GetView()
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			return new ListToBindableVectorViewAdapter(list);
		}

		// Token: 0x06006462 RID: 25698 RVA: 0x00156440 File Offset: 0x00154640
		[SecurityCritical]
		internal bool IndexOf(object value, out uint index)
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			int num = list.IndexOf(value);
			if (-1 == num)
			{
				index = 0U;
				return false;
			}
			index = (uint)num;
			return true;
		}

		// Token: 0x06006463 RID: 25699 RVA: 0x0015646C File Offset: 0x0015466C
		[SecurityCritical]
		internal void SetAt(uint index, object value)
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			ListToBindableVectorAdapter.EnsureIndexInt32(index, list.Count);
			try
			{
				list[(int)index] = value;
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw WindowsRuntimeMarshal.GetExceptionForHR(-2147483637, innerException, "ArgumentOutOfRange_IndexOutOfRange");
			}
		}

		// Token: 0x06006464 RID: 25700 RVA: 0x001564B8 File Offset: 0x001546B8
		[SecurityCritical]
		internal void InsertAt(uint index, object value)
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			ListToBindableVectorAdapter.EnsureIndexInt32(index, list.Count + 1);
			try
			{
				list.Insert((int)index, value);
			}
			catch (ArgumentOutOfRangeException ex)
			{
				ex.SetErrorCode(-2147483637);
				throw;
			}
		}

		// Token: 0x06006465 RID: 25701 RVA: 0x00156504 File Offset: 0x00154704
		[SecurityCritical]
		internal void RemoveAt(uint index)
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			ListToBindableVectorAdapter.EnsureIndexInt32(index, list.Count);
			try
			{
				list.RemoveAt((int)index);
			}
			catch (ArgumentOutOfRangeException ex)
			{
				ex.SetErrorCode(-2147483637);
				throw;
			}
		}

		// Token: 0x06006466 RID: 25702 RVA: 0x0015654C File Offset: 0x0015474C
		[SecurityCritical]
		internal void Append(object value)
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			list.Add(value);
		}

		// Token: 0x06006467 RID: 25703 RVA: 0x00156568 File Offset: 0x00154768
		[SecurityCritical]
		internal void RemoveAtEnd()
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			if (list.Count == 0)
			{
				Exception ex = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRemoveLastFromEmptyCollection"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
			uint count = (uint)list.Count;
			this.RemoveAt(count - 1U);
		}

		// Token: 0x06006468 RID: 25704 RVA: 0x001565B4 File Offset: 0x001547B4
		[SecurityCritical]
		internal void Clear()
		{
			IList list = JitHelpers.UnsafeCast<IList>(this);
			list.Clear();
		}

		// Token: 0x06006469 RID: 25705 RVA: 0x001565D0 File Offset: 0x001547D0
		private static void EnsureIndexInt32(uint index, int listCapacity)
		{
			if (2147483647U <= index || index >= (uint)listCapacity)
			{
				Exception ex = new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexLargerThanMaxValue"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
		}
	}
}
