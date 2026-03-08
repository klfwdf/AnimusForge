using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009DA RID: 2522
	internal sealed class ListToVectorAdapter
	{
		// Token: 0x06006432 RID: 25650 RVA: 0x00155B71 File Offset: 0x00153D71
		private ListToVectorAdapter()
		{
		}

		// Token: 0x06006433 RID: 25651 RVA: 0x00155B7C File Offset: 0x00153D7C
		[SecurityCritical]
		internal T GetAt<T>(uint index)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			ListToVectorAdapter.EnsureIndexInt32(index, list.Count);
			T result;
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

		// Token: 0x06006434 RID: 25652 RVA: 0x00155BCC File Offset: 0x00153DCC
		[SecurityCritical]
		internal uint Size<T>()
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			return (uint)list.Count;
		}

		// Token: 0x06006435 RID: 25653 RVA: 0x00155BE8 File Offset: 0x00153DE8
		[SecurityCritical]
		internal IReadOnlyList<T> GetView<T>()
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			IReadOnlyList<T> readOnlyList = list as IReadOnlyList<T>;
			if (readOnlyList == null)
			{
				readOnlyList = new ReadOnlyCollection<T>(list);
			}
			return readOnlyList;
		}

		// Token: 0x06006436 RID: 25654 RVA: 0x00155C10 File Offset: 0x00153E10
		[SecurityCritical]
		internal bool IndexOf<T>(T value, out uint index)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			int num = list.IndexOf(value);
			if (-1 == num)
			{
				index = 0U;
				return false;
			}
			index = (uint)num;
			return true;
		}

		// Token: 0x06006437 RID: 25655 RVA: 0x00155C3C File Offset: 0x00153E3C
		[SecurityCritical]
		internal void SetAt<T>(uint index, T value)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			ListToVectorAdapter.EnsureIndexInt32(index, list.Count);
			try
			{
				list[(int)index] = value;
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw WindowsRuntimeMarshal.GetExceptionForHR(-2147483637, innerException, "ArgumentOutOfRange_IndexOutOfRange");
			}
		}

		// Token: 0x06006438 RID: 25656 RVA: 0x00155C88 File Offset: 0x00153E88
		[SecurityCritical]
		internal void InsertAt<T>(uint index, T value)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			ListToVectorAdapter.EnsureIndexInt32(index, list.Count + 1);
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

		// Token: 0x06006439 RID: 25657 RVA: 0x00155CD4 File Offset: 0x00153ED4
		[SecurityCritical]
		internal void RemoveAt<T>(uint index)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			ListToVectorAdapter.EnsureIndexInt32(index, list.Count);
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

		// Token: 0x0600643A RID: 25658 RVA: 0x00155D1C File Offset: 0x00153F1C
		[SecurityCritical]
		internal void Append<T>(T value)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			list.Add(value);
		}

		// Token: 0x0600643B RID: 25659 RVA: 0x00155D38 File Offset: 0x00153F38
		[SecurityCritical]
		internal void RemoveAtEnd<T>()
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			if (list.Count == 0)
			{
				Exception ex = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRemoveLastFromEmptyCollection"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
			uint count = (uint)list.Count;
			this.RemoveAt<T>(count - 1U);
		}

		// Token: 0x0600643C RID: 25660 RVA: 0x00155D84 File Offset: 0x00153F84
		[SecurityCritical]
		internal void Clear<T>()
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			list.Clear();
		}

		// Token: 0x0600643D RID: 25661 RVA: 0x00155DA0 File Offset: 0x00153FA0
		[SecurityCritical]
		internal uint GetMany<T>(uint startIndex, T[] items)
		{
			IList<T> sourceList = JitHelpers.UnsafeCast<IList<T>>(this);
			return ListToVectorAdapter.GetManyHelper<T>(sourceList, startIndex, items);
		}

		// Token: 0x0600643E RID: 25662 RVA: 0x00155DBC File Offset: 0x00153FBC
		[SecurityCritical]
		internal void ReplaceAll<T>(T[] items)
		{
			IList<T> list = JitHelpers.UnsafeCast<IList<T>>(this);
			list.Clear();
			if (items != null)
			{
				foreach (T item in items)
				{
					list.Add(item);
				}
			}
		}

		// Token: 0x0600643F RID: 25663 RVA: 0x00155DF8 File Offset: 0x00153FF8
		private static void EnsureIndexInt32(uint index, int listCapacity)
		{
			if (2147483647U <= index || index >= (uint)listCapacity)
			{
				Exception ex = new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexLargerThanMaxValue"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
		}

		// Token: 0x06006440 RID: 25664 RVA: 0x00155E34 File Offset: 0x00154034
		private static uint GetManyHelper<T>(IList<T> sourceList, uint startIndex, T[] items)
		{
			if ((ulong)startIndex == (ulong)((long)sourceList.Count))
			{
				return 0U;
			}
			ListToVectorAdapter.EnsureIndexInt32(startIndex, sourceList.Count);
			if (items == null)
			{
				return 0U;
			}
			uint num = Math.Min((uint)items.Length, (uint)(sourceList.Count - (int)startIndex));
			for (uint num2 = 0U; num2 < num; num2 += 1U)
			{
				items[(int)num2] = sourceList[(int)(num2 + startIndex)];
			}
			if (typeof(T) == typeof(string))
			{
				string[] array = items as string[];
				uint num3 = num;
				while ((ulong)num3 < (ulong)((long)items.Length))
				{
					array[(int)num3] = string.Empty;
					num3 += 1U;
				}
			}
			return num;
		}
	}
}
