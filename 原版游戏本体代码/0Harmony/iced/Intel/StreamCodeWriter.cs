using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000667 RID: 1639
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class StreamCodeWriter : CodeWriter
	{
		// Token: 0x06002395 RID: 9109 RVA: 0x00072D1C File Offset: 0x00070F1C
		public StreamCodeWriter(Stream stream)
		{
			this.Stream = stream;
		}

		// Token: 0x06002396 RID: 9110 RVA: 0x00072D2B File Offset: 0x00070F2B
		public override void WriteByte(byte value)
		{
			this.Stream.WriteByte(value);
		}

		// Token: 0x04003435 RID: 13365
		public readonly Stream Stream;
	}
}
