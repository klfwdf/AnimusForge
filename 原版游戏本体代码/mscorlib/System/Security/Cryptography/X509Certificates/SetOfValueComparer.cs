using System;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002D2 RID: 722
	internal sealed class SetOfValueComparer : IComparer<ReadOnlyMemory<byte>>
	{
		// Token: 0x170004A0 RID: 1184
		// (get) Token: 0x06002572 RID: 9586 RVA: 0x00088D7F File Offset: 0x00086F7F
		internal static SetOfValueComparer Instance
		{
			get
			{
				return SetOfValueComparer._instance;
			}
		}

		// Token: 0x06002573 RID: 9587 RVA: 0x00088D86 File Offset: 0x00086F86
		public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
		{
			return SetOfValueComparer.Compare(x.Span, y.Span);
		}

		// Token: 0x06002574 RID: 9588 RVA: 0x00088D9C File Offset: 0x00086F9C
		internal static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
		{
			int num = Math.Min(x.Length, y.Length);
			for (int i = 0; i < num; i++)
			{
				int num2 = (int)x[i];
				byte b = y[i];
				int num3 = num2 - (int)b;
				if (num3 != 0)
				{
					return num3;
				}
			}
			return x.Length - y.Length;
		}

		// Token: 0x04000E23 RID: 3619
		private static SetOfValueComparer _instance = new SetOfValueComparer();
	}
}
