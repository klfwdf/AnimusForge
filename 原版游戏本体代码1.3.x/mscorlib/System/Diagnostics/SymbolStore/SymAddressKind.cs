using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x02000405 RID: 1029
	[ComVisible(true)]
	[Serializable]
	public enum SymAddressKind
	{
		// Token: 0x040016F0 RID: 5872
		ILOffset = 1,
		// Token: 0x040016F1 RID: 5873
		NativeRVA,
		// Token: 0x040016F2 RID: 5874
		NativeRegister,
		// Token: 0x040016F3 RID: 5875
		NativeRegisterRelative,
		// Token: 0x040016F4 RID: 5876
		NativeOffset,
		// Token: 0x040016F5 RID: 5877
		NativeRegisterRegister,
		// Token: 0x040016F6 RID: 5878
		NativeRegisterStack,
		// Token: 0x040016F7 RID: 5879
		NativeStackRegister,
		// Token: 0x040016F8 RID: 5880
		BitField,
		// Token: 0x040016F9 RID: 5881
		NativeSectionOffset
	}
}
