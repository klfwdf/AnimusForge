using System;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000015 RID: 21
	[NullableContext(2)]
	[Nullable(0)]
	internal class Traverse2<T>
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x060000C9 RID: 201 RVA: 0x00005C59 File Offset: 0x00003E59
		// (set) Token: 0x060000CA RID: 202 RVA: 0x00005C66 File Offset: 0x00003E66
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

		// Token: 0x060000CB RID: 203 RVA: 0x00005C7A File Offset: 0x00003E7A
		private Traverse2()
		{
			this._traverse = new Traverse2(null);
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00005C90 File Offset: 0x00003E90
		[NullableContext(1)]
		public Traverse2(Traverse2 traverse)
		{
			this._traverse = traverse;
		}

		// Token: 0x04000011 RID: 17
		[Nullable(1)]
		private readonly Traverse2 _traverse;
	}
}
