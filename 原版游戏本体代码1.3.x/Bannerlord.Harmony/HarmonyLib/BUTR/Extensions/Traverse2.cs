using System;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000041 RID: 65
	[NullableContext(2)]
	[Nullable(0)]
	internal class Traverse2<T>
	{
		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060003D7 RID: 983 RVA: 0x0000F1C9 File Offset: 0x0000D3C9
		// (set) Token: 0x060003D8 RID: 984 RVA: 0x0000F1D6 File Offset: 0x0000D3D6
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

		// Token: 0x060003D9 RID: 985 RVA: 0x0000F1EA File Offset: 0x0000D3EA
		private Traverse2()
		{
			this._traverse = new Traverse2(null);
		}

		// Token: 0x060003DA RID: 986 RVA: 0x0000F200 File Offset: 0x0000D400
		[NullableContext(1)]
		public Traverse2(Traverse2 traverse)
		{
			this._traverse = traverse;
		}

		// Token: 0x0400009C RID: 156
		[Nullable(1)]
		private readonly Traverse2 _traverse;
	}
}
