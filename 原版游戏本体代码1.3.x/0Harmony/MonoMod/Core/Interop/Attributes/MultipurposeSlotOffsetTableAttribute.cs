using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Interop.Attributes
{
	// Token: 0x02000608 RID: 1544
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	internal sealed class MultipurposeSlotOffsetTableAttribute : Attribute
	{
		// Token: 0x17000756 RID: 1878
		// (get) Token: 0x060020D6 RID: 8406 RVA: 0x000683D8 File Offset: 0x000665D8
		public int Bits { get; }

		// Token: 0x17000757 RID: 1879
		// (get) Token: 0x060020D7 RID: 8407 RVA: 0x000683E0 File Offset: 0x000665E0
		public Type HelperType { get; }

		// Token: 0x060020D8 RID: 8408 RVA: 0x000683E8 File Offset: 0x000665E8
		public MultipurposeSlotOffsetTableAttribute(int bits, Type helperType)
		{
			this.Bits = bits;
			this.HelperType = helperType;
		}
	}
}
