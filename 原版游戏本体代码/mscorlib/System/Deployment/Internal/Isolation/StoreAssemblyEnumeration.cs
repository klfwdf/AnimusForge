using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000686 RID: 1670
	internal class StoreAssemblyEnumeration : IEnumerator
	{
		// Token: 0x06004F4E RID: 20302 RVA: 0x0011C568 File Offset: 0x0011A768
		public StoreAssemblyEnumeration(IEnumSTORE_ASSEMBLY pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F4F RID: 20303 RVA: 0x0011C577 File Offset: 0x0011A777
		private STORE_ASSEMBLY GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000CA1 RID: 3233
		// (get) Token: 0x06004F50 RID: 20304 RVA: 0x0011C58D File Offset: 0x0011A78D
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000CA2 RID: 3234
		// (get) Token: 0x06004F51 RID: 20305 RVA: 0x0011C59A File Offset: 0x0011A79A
		public STORE_ASSEMBLY Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F52 RID: 20306 RVA: 0x0011C5A2 File Offset: 0x0011A7A2
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F53 RID: 20307 RVA: 0x0011C5A8 File Offset: 0x0011A7A8
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			STORE_ASSEMBLY[] array = new STORE_ASSEMBLY[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F54 RID: 20308 RVA: 0x0011C5E8 File Offset: 0x0011A7E8
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x04002202 RID: 8706
		private IEnumSTORE_ASSEMBLY _enum;

		// Token: 0x04002203 RID: 8707
		private bool _fValid;

		// Token: 0x04002204 RID: 8708
		private STORE_ASSEMBLY _current;
	}
}
