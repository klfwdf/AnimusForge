using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200068E RID: 1678
	internal class StoreCategoryInstanceEnumeration : IEnumerator
	{
		// Token: 0x06004F7A RID: 20346 RVA: 0x0011C7B8 File Offset: 0x0011A9B8
		public StoreCategoryInstanceEnumeration(IEnumSTORE_CATEGORY_INSTANCE pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F7B RID: 20347 RVA: 0x0011C7C7 File Offset: 0x0011A9C7
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F7C RID: 20348 RVA: 0x0011C7CA File Offset: 0x0011A9CA
		private STORE_CATEGORY_INSTANCE GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000CA9 RID: 3241
		// (get) Token: 0x06004F7D RID: 20349 RVA: 0x0011C7E0 File Offset: 0x0011A9E0
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000CAA RID: 3242
		// (get) Token: 0x06004F7E RID: 20350 RVA: 0x0011C7ED File Offset: 0x0011A9ED
		public STORE_CATEGORY_INSTANCE Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F7F RID: 20351 RVA: 0x0011C7F8 File Offset: 0x0011A9F8
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			STORE_CATEGORY_INSTANCE[] array = new STORE_CATEGORY_INSTANCE[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F80 RID: 20352 RVA: 0x0011C838 File Offset: 0x0011AA38
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x0400220E RID: 8718
		private IEnumSTORE_CATEGORY_INSTANCE _enum;

		// Token: 0x0400220F RID: 8719
		private bool _fValid;

		// Token: 0x04002210 RID: 8720
		private STORE_CATEGORY_INSTANCE _current;
	}
}
