using System;

namespace System.Collections
{
	// Token: 0x020004A7 RID: 1191
	[__DynamicallyInvokable]
	public static class StructuralComparisons
	{
		// Token: 0x17000887 RID: 2183
		// (get) Token: 0x060038FD RID: 14589 RVA: 0x000DA55C File Offset: 0x000D875C
		[__DynamicallyInvokable]
		public static IComparer StructuralComparer
		{
			[__DynamicallyInvokable]
			get
			{
				IComparer comparer = StructuralComparisons.s_StructuralComparer;
				if (comparer == null)
				{
					comparer = new StructuralComparer();
					StructuralComparisons.s_StructuralComparer = comparer;
				}
				return comparer;
			}
		}

		// Token: 0x17000888 RID: 2184
		// (get) Token: 0x060038FE RID: 14590 RVA: 0x000DA584 File Offset: 0x000D8784
		[__DynamicallyInvokable]
		public static IEqualityComparer StructuralEqualityComparer
		{
			[__DynamicallyInvokable]
			get
			{
				IEqualityComparer equalityComparer = StructuralComparisons.s_StructuralEqualityComparer;
				if (equalityComparer == null)
				{
					equalityComparer = new StructuralEqualityComparer();
					StructuralComparisons.s_StructuralEqualityComparer = equalityComparer;
				}
				return equalityComparer;
			}
		}

		// Token: 0x0400190F RID: 6415
		private static volatile IComparer s_StructuralComparer;

		// Token: 0x04001910 RID: 6416
		private static volatile IEqualityComparer s_StructuralEqualityComparer;
	}
}
