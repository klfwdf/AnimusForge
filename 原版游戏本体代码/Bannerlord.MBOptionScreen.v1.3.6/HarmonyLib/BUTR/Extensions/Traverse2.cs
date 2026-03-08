using System;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x0200006B RID: 107
	[NullableContext(2)]
	[Nullable(0)]
	internal class Traverse2<T>
	{
		// Token: 0x17000113 RID: 275
		// (get) Token: 0x06000469 RID: 1129 RVA: 0x00010B31 File Offset: 0x0000ED31
		// (set) Token: 0x0600046A RID: 1130 RVA: 0x00010B3E File Offset: 0x0000ED3E
		public T Value
		{
			get
			{
				return this._traverse.GetValue<T>();
			}
			set
			{
				this._traverse.SetValue(value);
			}
		}

		// Token: 0x0600046B RID: 1131 RVA: 0x00010B52 File Offset: 0x0000ED52
		private Traverse2()
		{
			this._traverse = new Traverse2(null);
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x00010B66 File Offset: 0x0000ED66
		[NullableContext(1)]
		public Traverse2(Traverse2 traverse)
		{
			this._traverse = traverse;
		}

		// Token: 0x04000156 RID: 342
		[Nullable(1)]
		private readonly Traverse2 _traverse;
	}
}
