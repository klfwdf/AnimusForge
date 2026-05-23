using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F5 RID: 1781
	internal struct Code3
	{
		// Token: 0x060024D1 RID: 9425 RVA: 0x0007B013 File Offset: 0x00079213
		public unsafe Code3(Code code16, Code code32, Code code64)
		{
			this.codes.FixedElementField = (ushort)code16;
			*((ref this.codes.FixedElementField) + 2) = (ushort)code32;
			*((ref this.codes.FixedElementField) + (IntPtr)2 * 2) = (ushort)code64;
		}

		// Token: 0x04003767 RID: 14183
		[FixedBuffer(typeof(ushort), 3)]
		public Code3.<codes>e__FixedBuffer codes;

		// Token: 0x020006F6 RID: 1782
		[CompilerGenerated]
		[UnsafeValueType]
		[StructLayout(LayoutKind.Sequential, Size = 6)]
		public struct <codes>e__FixedBuffer
		{
			// Token: 0x04003768 RID: 14184
			public ushort FixedElementField;
		}
	}
}
