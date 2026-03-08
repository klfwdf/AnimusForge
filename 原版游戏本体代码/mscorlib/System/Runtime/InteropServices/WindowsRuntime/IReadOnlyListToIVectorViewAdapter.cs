using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009EC RID: 2540
	[DebuggerDisplay("Size = {Size}")]
	internal sealed class IReadOnlyListToIVectorViewAdapter
	{
		// Token: 0x060064A9 RID: 25769 RVA: 0x00156FD5 File Offset: 0x001551D5
		private IReadOnlyListToIVectorViewAdapter()
		{
		}

		// Token: 0x060064AA RID: 25770 RVA: 0x00156FE0 File Offset: 0x001551E0
		[SecurityCritical]
		internal T GetAt<T>(uint index)
		{
			IReadOnlyList<T> readOnlyList = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
			IReadOnlyListToIVectorViewAdapter.EnsureIndexInt32(index, readOnlyList.Count);
			T result;
			try
			{
				result = readOnlyList[(int)index];
			}
			catch (ArgumentOutOfRangeException ex)
			{
				ex.SetErrorCode(-2147483637);
				throw;
			}
			return result;
		}

		// Token: 0x060064AB RID: 25771 RVA: 0x0015702C File Offset: 0x0015522C
		[SecurityCritical]
		internal uint Size<T>()
		{
			IReadOnlyList<T> readOnlyList = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
			return (uint)readOnlyList.Count;
		}

		// Token: 0x060064AC RID: 25772 RVA: 0x00157048 File Offset: 0x00155248
		[SecurityCritical]
		internal bool IndexOf<T>(T value, out uint index)
		{
			IReadOnlyList<T> readOnlyList = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
			int num = -1;
			int count = readOnlyList.Count;
			for (int i = 0; i < count; i++)
			{
				if (EqualityComparer<T>.Default.Equals(value, readOnlyList[i]))
				{
					num = i;
					break;
				}
			}
			if (-1 == num)
			{
				index = 0U;
				return false;
			}
			index = (uint)num;
			return true;
		}

		// Token: 0x060064AD RID: 25773 RVA: 0x00157098 File Offset: 0x00155298
		[SecurityCritical]
		internal uint GetMany<T>(uint startIndex, T[] items)
		{
			IReadOnlyList<T> readOnlyList = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
			if ((ulong)startIndex == (ulong)((long)readOnlyList.Count))
			{
				return 0U;
			}
			IReadOnlyListToIVectorViewAdapter.EnsureIndexInt32(startIndex, readOnlyList.Count);
			if (items == null)
			{
				return 0U;
			}
			uint num = Math.Min((uint)items.Length, (uint)(readOnlyList.Count - (int)startIndex));
			for (uint num2 = 0U; num2 < num; num2 += 1U)
			{
				items[(int)num2] = readOnlyList[(int)(num2 + startIndex)];
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

		// Token: 0x060064AE RID: 25774 RVA: 0x00157138 File Offset: 0x00155338
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
