using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009F0 RID: 2544
	internal sealed class IteratorToEnumeratorAdapter<T> : IEnumerator<!0>, IDisposable, IEnumerator
	{
		// Token: 0x060064B8 RID: 25784 RVA: 0x0015721C File Offset: 0x0015541C
		internal IteratorToEnumeratorAdapter(IIterator<T> iterator)
		{
			this.m_iterator = iterator;
			this.m_hadCurrent = true;
			this.m_isInitialized = false;
		}

		// Token: 0x17001157 RID: 4439
		// (get) Token: 0x060064B9 RID: 25785 RVA: 0x00157239 File Offset: 0x00155439
		public T Current
		{
			get
			{
				if (!this.m_isInitialized)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
				}
				if (!this.m_hadCurrent)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
				}
				return this.m_current;
			}
		}

		// Token: 0x17001158 RID: 4440
		// (get) Token: 0x060064BA RID: 25786 RVA: 0x0015725F File Offset: 0x0015545F
		object IEnumerator.Current
		{
			get
			{
				if (!this.m_isInitialized)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
				}
				if (!this.m_hadCurrent)
				{
					ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
				}
				return this.m_current;
			}
		}

		// Token: 0x060064BB RID: 25787 RVA: 0x0015728C File Offset: 0x0015548C
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			if (!this.m_hadCurrent)
			{
				return false;
			}
			try
			{
				if (!this.m_isInitialized)
				{
					this.m_hadCurrent = this.m_iterator.HasCurrent;
					this.m_isInitialized = true;
				}
				else
				{
					this.m_hadCurrent = this.m_iterator.MoveNext();
				}
				if (this.m_hadCurrent)
				{
					this.m_current = this.m_iterator.Current;
				}
			}
			catch (Exception e)
			{
				if (Marshal.GetHRForException(e) != -2147483636)
				{
					throw;
				}
				ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
			}
			return this.m_hadCurrent;
		}

		// Token: 0x060064BC RID: 25788 RVA: 0x00157324 File Offset: 0x00155524
		public void Reset()
		{
			throw new NotSupportedException();
		}

		// Token: 0x060064BD RID: 25789 RVA: 0x0015732B File Offset: 0x0015552B
		public void Dispose()
		{
		}

		// Token: 0x04002CF9 RID: 11513
		private IIterator<T> m_iterator;

		// Token: 0x04002CFA RID: 11514
		private bool m_hadCurrent;

		// Token: 0x04002CFB RID: 11515
		private T m_current;

		// Token: 0x04002CFC RID: 11516
		private bool m_isInitialized;
	}
}
