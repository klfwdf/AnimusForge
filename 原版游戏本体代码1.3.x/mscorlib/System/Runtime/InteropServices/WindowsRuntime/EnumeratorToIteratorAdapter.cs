using System;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D3 RID: 2515
	internal sealed class EnumeratorToIteratorAdapter<T> : IIterator<T>, IBindableIterator
	{
		// Token: 0x06006403 RID: 25603 RVA: 0x00155170 File Offset: 0x00153370
		internal EnumeratorToIteratorAdapter(IEnumerator<T> enumerator)
		{
			this.m_enumerator = enumerator;
		}

		// Token: 0x1700114D RID: 4429
		// (get) Token: 0x06006404 RID: 25604 RVA: 0x00155186 File Offset: 0x00153386
		public T Current
		{
			get
			{
				if (this.m_firstItem)
				{
					this.m_firstItem = false;
					this.MoveNext();
				}
				if (!this.m_hasCurrent)
				{
					throw WindowsRuntimeMarshal.GetExceptionForHR(-2147483637, null);
				}
				return this.m_enumerator.Current;
			}
		}

		// Token: 0x1700114E RID: 4430
		// (get) Token: 0x06006405 RID: 25605 RVA: 0x001551BD File Offset: 0x001533BD
		object IBindableIterator.Current
		{
			get
			{
				return ((IIterator<T>)this).Current;
			}
		}

		// Token: 0x1700114F RID: 4431
		// (get) Token: 0x06006406 RID: 25606 RVA: 0x001551CA File Offset: 0x001533CA
		public bool HasCurrent
		{
			get
			{
				if (this.m_firstItem)
				{
					this.m_firstItem = false;
					this.MoveNext();
				}
				return this.m_hasCurrent;
			}
		}

		// Token: 0x06006407 RID: 25607 RVA: 0x001551E8 File Offset: 0x001533E8
		public bool MoveNext()
		{
			try
			{
				this.m_hasCurrent = this.m_enumerator.MoveNext();
			}
			catch (InvalidOperationException innerException)
			{
				throw WindowsRuntimeMarshal.GetExceptionForHR(-2147483636, innerException);
			}
			return this.m_hasCurrent;
		}

		// Token: 0x06006408 RID: 25608 RVA: 0x0015522C File Offset: 0x0015342C
		public int GetMany(T[] items)
		{
			if (items == null)
			{
				return 0;
			}
			int num = 0;
			while (num < items.Length && this.HasCurrent)
			{
				items[num] = this.Current;
				this.MoveNext();
				num++;
			}
			if (typeof(T) == typeof(string))
			{
				string[] array = items as string[];
				for (int i = num; i < items.Length; i++)
				{
					array[i] = string.Empty;
				}
			}
			return num;
		}

		// Token: 0x04002CEC RID: 11500
		private IEnumerator<T> m_enumerator;

		// Token: 0x04002CED RID: 11501
		private bool m_firstItem = true;

		// Token: 0x04002CEE RID: 11502
		private bool m_hasCurrent;
	}
}
