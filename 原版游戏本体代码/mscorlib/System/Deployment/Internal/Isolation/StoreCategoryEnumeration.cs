using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200068A RID: 1674
	internal class StoreCategoryEnumeration : IEnumerator
	{
		// Token: 0x06004F64 RID: 20324 RVA: 0x0011C690 File Offset: 0x0011A890
		public StoreCategoryEnumeration(IEnumSTORE_CATEGORY pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F65 RID: 20325 RVA: 0x0011C69F File Offset: 0x0011A89F
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F66 RID: 20326 RVA: 0x0011C6A2 File Offset: 0x0011A8A2
		private STORE_CATEGORY GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000CA5 RID: 3237
		// (get) Token: 0x06004F67 RID: 20327 RVA: 0x0011C6B8 File Offset: 0x0011A8B8
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000CA6 RID: 3238
		// (get) Token: 0x06004F68 RID: 20328 RVA: 0x0011C6C5 File Offset: 0x0011A8C5
		public STORE_CATEGORY Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F69 RID: 20329 RVA: 0x0011C6D0 File Offset: 0x0011A8D0
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			STORE_CATEGORY[] array = new STORE_CATEGORY[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F6A RID: 20330 RVA: 0x0011C710 File Offset: 0x0011A910
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x04002208 RID: 8712
		private IEnumSTORE_CATEGORY _enum;

		// Token: 0x04002209 RID: 8713
		private bool _fValid;

		// Token: 0x0400220A RID: 8714
		private STORE_CATEGORY _current;
	}
}
