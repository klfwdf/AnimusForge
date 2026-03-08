using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009DD RID: 2525
	internal sealed class BindableVectorToCollectionAdapter
	{
		// Token: 0x06006459 RID: 25689 RVA: 0x001562B4 File Offset: 0x001544B4
		private BindableVectorToCollectionAdapter()
		{
		}

		// Token: 0x0600645A RID: 25690 RVA: 0x001562BC File Offset: 0x001544BC
		[SecurityCritical]
		internal int Count()
		{
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			uint size = bindableVector.Size;
			if (2147483647U < size)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			return (int)size;
		}

		// Token: 0x0600645B RID: 25691 RVA: 0x001562F0 File Offset: 0x001544F0
		[SecurityCritical]
		internal bool IsSynchronized()
		{
			return false;
		}

		// Token: 0x0600645C RID: 25692 RVA: 0x001562F3 File Offset: 0x001544F3
		[SecurityCritical]
		internal object SyncRoot()
		{
			return this;
		}

		// Token: 0x0600645D RID: 25693 RVA: 0x001562F8 File Offset: 0x001544F8
		[SecurityCritical]
		internal void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			int lowerBound = array.GetLowerBound(0);
			int num = this.Count();
			int length = array.GetLength(0);
			if (arrayIndex < lowerBound)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			if (num > length - (arrayIndex - lowerBound))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
			}
			if (arrayIndex - lowerBound > length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));
			}
			IBindableVector bindableVector = JitHelpers.UnsafeCast<IBindableVector>(this);
			uint num2 = 0U;
			while ((ulong)num2 < (ulong)((long)num))
			{
				array.SetValue(bindableVector.GetAt(num2), (long)((ulong)num2 + (ulong)((long)arrayIndex)));
				num2 += 1U;
			}
		}
	}
}
