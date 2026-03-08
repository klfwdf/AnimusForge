using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D5 RID: 2517
	internal sealed class VectorToCollectionAdapter
	{
		// Token: 0x06006413 RID: 25619 RVA: 0x001554A8 File Offset: 0x001536A8
		private VectorToCollectionAdapter()
		{
		}

		// Token: 0x06006414 RID: 25620 RVA: 0x001554B0 File Offset: 0x001536B0
		[SecurityCritical]
		internal int Count<T>()
		{
			IVector<T> vector = JitHelpers.UnsafeCast<IVector<T>>(this);
			uint size = vector.Size;
			if (2147483647U < size)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			return (int)size;
		}

		// Token: 0x06006415 RID: 25621 RVA: 0x001554E4 File Offset: 0x001536E4
		[SecurityCritical]
		internal bool IsReadOnly<T>()
		{
			return false;
		}

		// Token: 0x06006416 RID: 25622 RVA: 0x001554E8 File Offset: 0x001536E8
		[SecurityCritical]
		internal void Add<T>(T item)
		{
			IVector<T> vector = JitHelpers.UnsafeCast<IVector<T>>(this);
			vector.Append(item);
		}

		// Token: 0x06006417 RID: 25623 RVA: 0x00155504 File Offset: 0x00153704
		[SecurityCritical]
		internal void Clear<T>()
		{
			IVector<T> vector = JitHelpers.UnsafeCast<IVector<T>>(this);
			vector.Clear();
		}

		// Token: 0x06006418 RID: 25624 RVA: 0x00155520 File Offset: 0x00153720
		[SecurityCritical]
		internal bool Contains<T>(T item)
		{
			IVector<T> vector = JitHelpers.UnsafeCast<IVector<T>>(this);
			uint num;
			return vector.IndexOf(item, out num);
		}

		// Token: 0x06006419 RID: 25625 RVA: 0x00155540 File Offset: 0x00153740
		[SecurityCritical]
		internal void CopyTo<T>(T[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			if (array.Length <= arrayIndex && this.Count<T>() > 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));
			}
			if (array.Length - arrayIndex < this.Count<T>())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
			}
			IVector<T> this2 = JitHelpers.UnsafeCast<IVector<T>>(this);
			int num = this.Count<T>();
			for (int i = 0; i < num; i++)
			{
				array[i + arrayIndex] = VectorToListAdapter.GetAt<T>(this2, (uint)i);
			}
		}

		// Token: 0x0600641A RID: 25626 RVA: 0x001555D0 File Offset: 0x001537D0
		[SecurityCritical]
		internal bool Remove<T>(T item)
		{
			IVector<T> vector = JitHelpers.UnsafeCast<IVector<T>>(this);
			uint num;
			if (!vector.IndexOf(item, out num))
			{
				return false;
			}
			if (2147483647U < num)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			VectorToListAdapter.RemoveAtHelper<T>(vector, num);
			return true;
		}
	}
}
