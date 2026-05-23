using System;
using System.Collections;

namespace System.Security.AccessControl
{
	// Token: 0x0200020A RID: 522
	public sealed class AceEnumerator : IEnumerator
	{
		// Token: 0x06001E85 RID: 7813 RVA: 0x0006ACCF File Offset: 0x00068ECF
		internal AceEnumerator(GenericAcl collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this._acl = collection;
			this.Reset();
		}

		// Token: 0x1700037A RID: 890
		// (get) Token: 0x06001E86 RID: 7814 RVA: 0x0006ACF2 File Offset: 0x00068EF2
		object IEnumerator.Current
		{
			get
			{
				if (this._current == -1 || this._current >= this._acl.Count)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Arg_InvalidOperationException"));
				}
				return this._acl[this._current];
			}
		}

		// Token: 0x1700037B RID: 891
		// (get) Token: 0x06001E87 RID: 7815 RVA: 0x0006AD31 File Offset: 0x00068F31
		public GenericAce Current
		{
			get
			{
				return ((IEnumerator)this).Current as GenericAce;
			}
		}

		// Token: 0x06001E88 RID: 7816 RVA: 0x0006AD3E File Offset: 0x00068F3E
		public bool MoveNext()
		{
			this._current++;
			return this._current < this._acl.Count;
		}

		// Token: 0x06001E89 RID: 7817 RVA: 0x0006AD61 File Offset: 0x00068F61
		public void Reset()
		{
			this._current = -1;
		}

		// Token: 0x04000B04 RID: 2820
		private int _current;

		// Token: 0x04000B05 RID: 2821
		private readonly GenericAcl _acl;
	}
}
