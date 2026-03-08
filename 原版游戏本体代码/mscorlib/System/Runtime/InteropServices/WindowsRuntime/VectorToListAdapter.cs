using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D4 RID: 2516
	internal sealed class VectorToListAdapter
	{
		// Token: 0x06006409 RID: 25609 RVA: 0x001552A1 File Offset: 0x001534A1
		private VectorToListAdapter()
		{
		}

		// Token: 0x0600640A RID: 25610 RVA: 0x001552AC File Offset: 0x001534AC
		[SecurityCritical]
		internal T Indexer_Get<T>(int index)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IVector<T> this2 = JitHelpers.UnsafeCast<IVector<T>>(this);
			return VectorToListAdapter.GetAt<T>(this2, (uint)index);
		}

		// Token: 0x0600640B RID: 25611 RVA: 0x001552D8 File Offset: 0x001534D8
		[SecurityCritical]
		internal void Indexer_Set<T>(int index, T value)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IVector<T> this2 = JitHelpers.UnsafeCast<IVector<T>>(this);
			VectorToListAdapter.SetAt<T>(this2, (uint)index, value);
		}

		// Token: 0x0600640C RID: 25612 RVA: 0x00155304 File Offset: 0x00153504
		[SecurityCritical]
		internal int IndexOf<T>(T item)
		{
			IVector<T> vector = JitHelpers.UnsafeCast<IVector<T>>(this);
			uint num;
			if (!vector.IndexOf(item, out num))
			{
				return -1;
			}
			if (2147483647U < num)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			return (int)num;
		}

		// Token: 0x0600640D RID: 25613 RVA: 0x00155340 File Offset: 0x00153540
		[SecurityCritical]
		internal void Insert<T>(int index, T item)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IVector<T> this2 = JitHelpers.UnsafeCast<IVector<T>>(this);
			VectorToListAdapter.InsertAtHelper<T>(this2, (uint)index, item);
		}

		// Token: 0x0600640E RID: 25614 RVA: 0x0015536C File Offset: 0x0015356C
		[SecurityCritical]
		internal void RemoveAt<T>(int index)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IVector<T> this2 = JitHelpers.UnsafeCast<IVector<T>>(this);
			VectorToListAdapter.RemoveAtHelper<T>(this2, (uint)index);
		}

		// Token: 0x0600640F RID: 25615 RVA: 0x00155398 File Offset: 0x00153598
		internal static T GetAt<T>(IVector<T> _this, uint index)
		{
			T at;
			try
			{
				at = _this.GetAt(index);
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

		// Token: 0x06006410 RID: 25616 RVA: 0x001553DC File Offset: 0x001535DC
		private static void SetAt<T>(IVector<T> _this, uint index, T value)
		{
			try
			{
				_this.SetAt(index, value);
			}
			catch (Exception ex)
			{
				if (-2147483637 == ex._HResult)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				throw;
			}
		}

		// Token: 0x06006411 RID: 25617 RVA: 0x00155420 File Offset: 0x00153620
		private static void InsertAtHelper<T>(IVector<T> _this, uint index, T item)
		{
			try
			{
				_this.InsertAt(index, item);
			}
			catch (Exception ex)
			{
				if (-2147483637 == ex._HResult)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				throw;
			}
		}

		// Token: 0x06006412 RID: 25618 RVA: 0x00155464 File Offset: 0x00153664
		internal static void RemoveAtHelper<T>(IVector<T> _this, uint index)
		{
			try
			{
				_this.RemoveAt(index);
			}
			catch (Exception ex)
			{
				if (-2147483637 == ex._HResult)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				throw;
			}
		}
	}
}
