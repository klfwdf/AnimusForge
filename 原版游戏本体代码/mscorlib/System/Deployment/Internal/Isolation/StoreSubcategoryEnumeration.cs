using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200068C RID: 1676
	internal class StoreSubcategoryEnumeration : IEnumerator
	{
		// Token: 0x06004F6F RID: 20335 RVA: 0x0011C724 File Offset: 0x0011A924
		public StoreSubcategoryEnumeration(IEnumSTORE_CATEGORY_SUBCATEGORY pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F70 RID: 20336 RVA: 0x0011C733 File Offset: 0x0011A933
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F71 RID: 20337 RVA: 0x0011C736 File Offset: 0x0011A936
		private STORE_CATEGORY_SUBCATEGORY GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000CA7 RID: 3239
		// (get) Token: 0x06004F72 RID: 20338 RVA: 0x0011C74C File Offset: 0x0011A94C
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000CA8 RID: 3240
		// (get) Token: 0x06004F73 RID: 20339 RVA: 0x0011C759 File Offset: 0x0011A959
		public STORE_CATEGORY_SUBCATEGORY Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F74 RID: 20340 RVA: 0x0011C764 File Offset: 0x0011A964
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			STORE_CATEGORY_SUBCATEGORY[] array = new STORE_CATEGORY_SUBCATEGORY[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F75 RID: 20341 RVA: 0x0011C7A4 File Offset: 0x0011A9A4
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x0400220B RID: 8715
		private IEnumSTORE_CATEGORY_SUBCATEGORY _enum;

		// Token: 0x0400220C RID: 8716
		private bool _fValid;

		// Token: 0x0400220D RID: 8717
		private STORE_CATEGORY_SUBCATEGORY _current;
	}
}
