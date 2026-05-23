using System;
using System.IO;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000331 RID: 817
	internal class MyBinaryReader : BinaryReader
	{
		// Token: 0x060014E9 RID: 5353 RVA: 0x000375B2 File Offset: 0x000357B2
		public MyBinaryReader(Stream stream)
			: base(stream)
		{
		}

		// Token: 0x060014EA RID: 5354 RVA: 0x00041D6C File Offset: 0x0003FF6C
		public int ReadLeb128()
		{
			return base.Read7BitEncodedInt();
		}

		// Token: 0x060014EB RID: 5355 RVA: 0x00041D74 File Offset: 0x0003FF74
		public string ReadString(int offset)
		{
			long old_pos = this.BaseStream.Position;
			this.BaseStream.Position = (long)offset;
			string result = this.ReadString();
			this.BaseStream.Position = old_pos;
			return result;
		}
	}
}
