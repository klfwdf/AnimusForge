using System;
using System.IO;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000330 RID: 816
	internal sealed class MyBinaryWriter : BinaryWriter
	{
		// Token: 0x060014E7 RID: 5351 RVA: 0x00037637 File Offset: 0x00035837
		public MyBinaryWriter(Stream stream)
			: base(stream)
		{
		}

		// Token: 0x060014E8 RID: 5352 RVA: 0x00041D63 File Offset: 0x0003FF63
		public void WriteLeb128(int value)
		{
			base.Write7BitEncodedInt(value);
		}
	}
}
