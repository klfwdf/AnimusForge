using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
	// Token: 0x02000336 RID: 822
	[ComVisible(false)]
	internal class IdentityReferenceEnumerator : IEnumerator<IdentityReference>, IDisposable, IEnumerator
	{
		// Token: 0x06002911 RID: 10513 RVA: 0x00097610 File Offset: 0x00095810
		internal IdentityReferenceEnumerator(IdentityReferenceCollection collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this._Collection = collection;
			this._Current = -1;
		}

		// Token: 0x1700055A RID: 1370
		// (get) Token: 0x06002912 RID: 10514 RVA: 0x00097634 File Offset: 0x00095834
		object IEnumerator.Current
		{
			get
			{
				return this._Collection.Identities[this._Current];
			}
		}

		// Token: 0x1700055B RID: 1371
		// (get) Token: 0x06002913 RID: 10515 RVA: 0x0009764C File Offset: 0x0009584C
		public IdentityReference Current
		{
			get
			{
				return ((IEnumerator)this).Current as IdentityReference;
			}
		}

		// Token: 0x06002914 RID: 10516 RVA: 0x00097659 File Offset: 0x00095859
		public bool MoveNext()
		{
			this._Current++;
			return this._Current < this._Collection.Count;
		}

		// Token: 0x06002915 RID: 10517 RVA: 0x0009767C File Offset: 0x0009587C
		public void Reset()
		{
			this._Current = -1;
		}

		// Token: 0x06002916 RID: 10518 RVA: 0x00097685 File Offset: 0x00095885
		public void Dispose()
		{
		}

		// Token: 0x04001092 RID: 4242
		private int _Current;

		// Token: 0x04001093 RID: 4243
		private readonly IdentityReferenceCollection _Collection;
	}
}
