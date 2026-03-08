using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000666 RID: 1638
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class StreamCodeReader : CodeReader
	{
		// Token: 0x06002393 RID: 9107 RVA: 0x00072D00 File Offset: 0x00070F00
		public StreamCodeReader(Stream stream)
		{
			this.Stream = stream;
		}

		// Token: 0x06002394 RID: 9108 RVA: 0x00072D0F File Offset: 0x00070F0F
		public override int ReadByte()
		{
			return this.Stream.ReadByte();
		}

		// Token: 0x04003434 RID: 13364
		public readonly Stream Stream;
	}
}
