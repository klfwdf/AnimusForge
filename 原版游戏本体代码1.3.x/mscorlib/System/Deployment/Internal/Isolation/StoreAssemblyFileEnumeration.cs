using System;
using System.Collections;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000688 RID: 1672
	internal class StoreAssemblyFileEnumeration : IEnumerator
	{
		// Token: 0x06004F59 RID: 20313 RVA: 0x0011C5FC File Offset: 0x0011A7FC
		public StoreAssemblyFileEnumeration(IEnumSTORE_ASSEMBLY_FILE pI)
		{
			this._enum = pI;
		}

		// Token: 0x06004F5A RID: 20314 RVA: 0x0011C60B File Offset: 0x0011A80B
		public IEnumerator GetEnumerator()
		{
			return this;
		}

		// Token: 0x06004F5B RID: 20315 RVA: 0x0011C60E File Offset: 0x0011A80E
		private STORE_ASSEMBLY_FILE GetCurrent()
		{
			if (!this._fValid)
			{
				throw new InvalidOperationException();
			}
			return this._current;
		}

		// Token: 0x17000CA3 RID: 3235
		// (get) Token: 0x06004F5C RID: 20316 RVA: 0x0011C624 File Offset: 0x0011A824
		object IEnumerator.Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x17000CA4 RID: 3236
		// (get) Token: 0x06004F5D RID: 20317 RVA: 0x0011C631 File Offset: 0x0011A831
		public STORE_ASSEMBLY_FILE Current
		{
			get
			{
				return this.GetCurrent();
			}
		}

		// Token: 0x06004F5E RID: 20318 RVA: 0x0011C63C File Offset: 0x0011A83C
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			STORE_ASSEMBLY_FILE[] array = new STORE_ASSEMBLY_FILE[1];
			uint num = this._enum.Next(1U, array);
			if (num == 1U)
			{
				this._current = array[0];
			}
			return this._fValid = num == 1U;
		}

		// Token: 0x06004F5F RID: 20319 RVA: 0x0011C67C File Offset: 0x0011A87C
		[SecuritySafeCritical]
		public void Reset()
		{
			this._fValid = false;
			this._enum.Reset();
		}

		// Token: 0x04002205 RID: 8709
		private IEnumSTORE_ASSEMBLY_FILE _enum;

		// Token: 0x04002206 RID: 8710
		private bool _fValid;

		// Token: 0x04002207 RID: 8711
		private STORE_ASSEMBLY_FILE _current;
	}
}
