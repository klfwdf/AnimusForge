using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000684 RID: 1668
	internal class StoreDeploymentMetadataPropertyEnumeration : IEnumerator
	{
		// Token: 0x06004F43 RID: 20291 RVA: 0x0011C4D4 File Offset: 0x0011A6D4
		public StoreDeploymentMetadataPropertyEnumeration(IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F44 RID: 20292 RVA: 0x0011C4E3 File Offset: 0x0011A6E3
		private StoreOperationMetadataProperty GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000C9F RID: 3231
		// (get) Token: 0x06004F45 RID: 20293 RVA: 0x0011C4F9 File Offset: 0x0011A6F9
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000CA0 RID: 3232
		// (get) Token: 0x06004F46 RID: 20294 RVA: 0x0011C506 File Offset: 0x0011A706
		public StoreOperationMetadataProperty Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F47 RID: 20295 RVA: 0x0011C50E File Offset: 0x0011A70E
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F48 RID: 20296 RVA: 0x0011C514 File Offset: 0x0011A714
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			StoreOperationMetadataProperty[] array = new StoreOperationMetadataProperty[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F49 RID: 20297 RVA: 0x0011C554 File Offset: 0x0011A754
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x040021FF RID: 8703
		private IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY _enum;

		// Token: 0x04002200 RID: 8704
		private bool _fValid;

		// Token: 0x04002201 RID: 8705
		private StoreOperationMetadataProperty _current;
	}
}
