using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005FD RID: 1533
	internal struct MetadataEnumResult
	{
		// Token: 0x17000AB1 RID: 2737
		// (get) Token: 0x06004694 RID: 18068 RVA: 0x00102BC3 File Offset: 0x00100DC3
		public int Length
		{
			get
			{
				return this.length;
			}
		}

		// Token: 0x17000AB2 RID: 2738
		public unsafe int this[int index]
		{
			[SecurityCritical]
			get
			{
				if (this.largeResult != null)
				{
					return this.largeResult[index];
				}
				fixed (int* ptr = &this.smallResult.FixedElementField)
				{
					int* ptr2 = ptr;
					return ptr2[index];
				}
			}
		}

		// Token: 0x04001D51 RID: 7505
		private int[] largeResult;

		// Token: 0x04001D52 RID: 7506
		private int length;

		// Token: 0x04001D53 RID: 7507
		[FixedBuffer(typeof(int), 16)]
		private MetadataEnumResult.<smallResult>e__FixedBuffer smallResult;

		// Token: 0x02000C3A RID: 3130
		[CompilerGenerated]
		[UnsafeValueType]
		[StructLayout(LayoutKind.Sequential, Size = 64)]
		public struct <smallResult>e__FixedBuffer
		{
			// Token: 0x0400373B RID: 14139
			public int FixedElementField;
		}
	}
}
