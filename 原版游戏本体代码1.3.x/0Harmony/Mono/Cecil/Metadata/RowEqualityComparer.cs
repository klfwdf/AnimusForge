using System;
using System.Collections.Generic;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002DF RID: 735
	internal sealed class RowEqualityComparer : IEqualityComparer<Row<string, string>>, IEqualityComparer<Row<uint, uint>>, IEqualityComparer<Row<uint, uint, uint>>
	{
		// Token: 0x060012D3 RID: 4819 RVA: 0x0003B486 File Offset: 0x00039686
		public bool Equals(Row<string, string> x, Row<string, string> y)
		{
			return x.Col1 == y.Col1 && x.Col2 == y.Col2;
		}

		// Token: 0x060012D4 RID: 4820 RVA: 0x0003B4B0 File Offset: 0x000396B0
		public int GetHashCode(Row<string, string> obj)
		{
			string x = obj.Col1;
			string y = obj.Col2;
			return ((x != null) ? x.GetHashCode() : 0) ^ ((y != null) ? y.GetHashCode() : 0);
		}

		// Token: 0x060012D5 RID: 4821 RVA: 0x0003B4E4 File Offset: 0x000396E4
		public bool Equals(Row<uint, uint> x, Row<uint, uint> y)
		{
			return x.Col1 == y.Col1 && x.Col2 == y.Col2;
		}

		// Token: 0x060012D6 RID: 4822 RVA: 0x0003B504 File Offset: 0x00039704
		public int GetHashCode(Row<uint, uint> obj)
		{
			return (int)(obj.Col1 ^ obj.Col2);
		}

		// Token: 0x060012D7 RID: 4823 RVA: 0x0003B513 File Offset: 0x00039713
		public bool Equals(Row<uint, uint, uint> x, Row<uint, uint, uint> y)
		{
			return x.Col1 == y.Col1 && x.Col2 == y.Col2 && x.Col3 == y.Col3;
		}

		// Token: 0x060012D8 RID: 4824 RVA: 0x0003B541 File Offset: 0x00039741
		public int GetHashCode(Row<uint, uint, uint> obj)
		{
			return (int)(obj.Col1 ^ obj.Col2 ^ obj.Col3);
		}
	}
}
