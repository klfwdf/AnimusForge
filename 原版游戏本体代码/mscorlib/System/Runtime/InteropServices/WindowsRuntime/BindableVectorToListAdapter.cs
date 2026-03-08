using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009DC RID: 2524
	internal sealed class BindableVectorToListAdapter
	{
		// Token: 0x06006449 RID: 25673 RVA: 0x00155FEE File Offset: 0x001541EE
		private BindableVectorToListAdapter()
		{
		}

		// Token: 0x0600644A RID: 25674 RVA: 0x00155FF8 File Offset: 0x001541F8
		[SecurityCritical]
		internal object Indexer_Get(int index)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IBindableVector this2 = JitHelpers.UnsafeCast<IBindableVector>(this);
			return BindableVectorToListAdapter.GetAt(this2, (uint)index);
		}

		// Token: 0x0600644B RID: 25675 RVA: 0x00156024 File Offset: 0x00154224
		[SecurityCritical]
		internal void Indexer_Set(int index, object value)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IBindableVector this2 = JitHelpers.UnsafeCast<IBindableVector>(this);
			BindableVectorToListAdapter.SetAt(this2, (uint)index, value);
		}

		// Token: 0x0600644C RID: 25676 RVA: 0x00156050 File Offset: 0x00154250
		[SecurityCritical]
		internal int Add(object value)
		{
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			bindableVector.Append(value);
			uint size = bindableVector.Size;
			if (2147483647U < size)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			return (int)(size - 1U);
		}

		// Token: 0x0600644D RID: 25677 RVA: 0x00156090 File Offset: 0x00154290
		[SecurityCritical]
		internal bool Contains(object item)
		{
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			uint num;
			return bindableVector.IndexOf(item, out num);
		}

		// Token: 0x0600644E RID: 25678 RVA: 0x001560B0 File Offset: 0x001542B0
		[SecurityCritical]
		internal void Clear()
		{
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			bindableVector.Clear();
		}

		// Token: 0x0600644F RID: 25679 RVA: 0x001560CA File Offset: 0x001542CA
		[SecurityCritical]
		internal bool IsFixedSize()
		{
			return false;
		}

		// Token: 0x06006450 RID: 25680 RVA: 0x001560CD File Offset: 0x001542CD
		[SecurityCritical]
		internal bool IsReadOnly()
		{
			return false;
		}

		// Token: 0x06006451 RID: 25681 RVA: 0x001560D0 File Offset: 0x001542D0
		[SecurityCritical]
		internal int IndexOf(object item)
		{
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			uint num;
			if (!bindableVector.IndexOf(item, out num))
			{
				return -1;
			}
			if (2147483647U < num)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			return (int)num;
		}

		// Token: 0x06006452 RID: 25682 RVA: 0x0015610C File Offset: 0x0015430C
		[SecurityCritical]
		internal void Insert(int index, object item)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IBindableVector this2 = JitHelpers.UnsafeCast<IBindableVector>(this);
			BindableVectorToListAdapter.InsertAtHelper(this2, (uint)index, item);
		}

		// Token: 0x06006453 RID: 25683 RVA: 0x00156138 File Offset: 0x00154338
		[SecurityCritical]
		internal void Remove(object item)
		{
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			uint num;
			bool flag = bindableVector.IndexOf(item, out num);
			if (flag)
			{
				if (2147483647U < num)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
				}
				BindableVectorToListAdapter.RemoveAtHelper(bindableVector, num);
			}
		}

		// Token: 0x06006454 RID: 25684 RVA: 0x00156178 File Offset: 0x00154378
		[SecurityCritical]
		internal void RemoveAt(int index)
		{
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			IBindableVector this2 = JitHelpers.UnsafeCast<IBindableVector>(this);
			BindableVectorToListAdapter.RemoveAtHelper(this2, (uint)index);
		}

		// Token: 0x06006455 RID: 25685 RVA: 0x001561A4 File Offset: 0x001543A4
		private static object GetAt(IBindableVector _this, uint index)
		{
			object at;
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

		// Token: 0x06006456 RID: 25686 RVA: 0x001561E8 File Offset: 0x001543E8
		private static void SetAt(IBindableVector _this, uint index, object value)
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

		// Token: 0x06006457 RID: 25687 RVA: 0x0015622C File Offset: 0x0015442C
		private static void InsertAtHelper(IBindableVector _this, uint index, object item)
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

		// Token: 0x06006458 RID: 25688 RVA: 0x00156270 File Offset: 0x00154470
		private static void RemoveAtHelper(IBindableVector _this, uint index)
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
