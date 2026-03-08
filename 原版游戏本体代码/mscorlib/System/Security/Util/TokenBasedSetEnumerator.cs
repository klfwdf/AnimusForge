using System;

namespace System.Security.Util
{
	// Token: 0x0200037F RID: 895
	internal struct TokenBasedSetEnumerator
	{
		// Token: 0x06002C76 RID: 11382 RVA: 0x000A6063 File Offset: 0x000A4263
		public bool MoveNext()
		{
			return this._tb != null && this._tb.MoveNext(ref this);
		}

		// Token: 0x06002C77 RID: 11383 RVA: 0x000A607B File Offset: 0x000A427B
		public void Reset()
		{
			this.Index = -1;
			this.Current = null;
		}

		// Token: 0x06002C78 RID: 11384 RVA: 0x000A608B File Offset: 0x000A428B
		public TokenBasedSetEnumerator(TokenBasedSet tb)
		{
			this.Index = -1;
			this.Current = null;
			this._tb = tb;
		}

		// Token: 0x040011D9 RID: 4569
		public object Current;

		// Token: 0x040011DA RID: 4570
		public int Index;

		// Token: 0x040011DB RID: 4571
		private TokenBasedSet _tb;
	}
}
