using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000682 RID: 1666
	internal class StoreDeploymentMetadataEnumeration : IEnumerator
	{
		// Token: 0x06004F38 RID: 20280 RVA: 0x0011C449 File Offset: 0x0011A649
		public StoreDeploymentMetadataEnumeration(IEnumSTORE_DEPLOYMENT_METADATA pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F39 RID: 20281 RVA: 0x0011C458 File Offset: 0x0011A658
		private IDefinitionAppId GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000C9D RID: 3229
		// (get) Token: 0x06004F3A RID: 20282 RVA: 0x0011C46E File Offset: 0x0011A66E
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000C9E RID: 3230
		// (get) Token: 0x06004F3B RID: 20283 RVA: 0x0011C476 File Offset: 0x0011A676
		public IDefinitionAppId Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F3C RID: 20284 RVA: 0x0011C47E File Offset: 0x0011A67E
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F3D RID: 20285 RVA: 0x0011C484 File Offset: 0x0011A684
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			IDefinitionAppId[] array = new IDefinitionAppId[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F3E RID: 20286 RVA: 0x0011C4C0 File Offset: 0x0011A6C0
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x040021FC RID: 8700
		private IEnumSTORE_DEPLOYMENT_METADATA _enum;

		// Token: 0x040021FD RID: 8701
		private bool _fValid;

		// Token: 0x040021FE RID: 8702
		private IDefinitionAppId _current;
	}
}
