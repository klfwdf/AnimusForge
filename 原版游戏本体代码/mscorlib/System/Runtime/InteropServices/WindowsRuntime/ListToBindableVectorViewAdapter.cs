using System;
using System.Collections;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009DF RID: 2527
	internal sealed class ListToBindableVectorViewAdapter : IBindableVectorView, IBindableIterable
	{
		// Token: 0x0600646A RID: 25706 RVA: 0x0015660B File Offset: 0x0015480B
		internal ListToBindableVectorViewAdapter(IList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			this.list = list;
		}

		// Token: 0x0600646B RID: 25707 RVA: 0x00156628 File Offset: 0x00154828
		private static void EnsureIndexInt32(uint index, int listCapacity)
		{
			if (2147483647U <= index || index >= (uint)listCapacity)
			{
				Exception ex = new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexLargerThanMaxValue"));
				ex.SetErrorCode(-2147483637);
				throw ex;
			}
		}

		// Token: 0x0600646C RID: 25708 RVA: 0x00156664 File Offset: 0x00154864
		public IBindableIterator First()
		{
			IEnumerator enumerator = this.list.GetEnumerator();
			return new EnumeratorToIteratorAdapter<object>(new EnumerableToBindableIterableAdapter.NonGenericToGenericEnumerator(enumerator));
		}

		// Token: 0x0600646D RID: 25709 RVA: 0x00156688 File Offset: 0x00154888
		public object GetAt(uint index)
		{
			ListToBindableVectorViewAdapter.EnsureIndexInt32(index, this.list.Count);
			object result;
			try
			{
				result = this.list[(int)index];
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw WindowsRuntimeMarshal.GetExceptionForHR(-2147483637, innerException, "ArgumentOutOfRange_IndexOutOfRange");
			}
			return result;
		}

		// Token: 0x17001150 RID: 4432
		// (get) Token: 0x0600646E RID: 25710 RVA: 0x001566D8 File Offset: 0x001548D8
		public uint Size
		{
			get
			{
				return (uint)this.list.Count;
			}
		}

		// Token: 0x0600646F RID: 25711 RVA: 0x001566E8 File Offset: 0x001548E8
		public bool IndexOf(object value, out uint index)
		{
			int num = this.list.IndexOf(value);
			if (-1 == num)
			{
				index = 0U;
				return false;
			}
			index = (uint)num;
			return true;
		}

		// Token: 0x04002CEF RID: 11503
		private readonly IList list;
	}
}
