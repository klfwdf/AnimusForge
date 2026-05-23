using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007DF RID: 2015
	internal sealed class CodeWriterImpl : CodeWriter
	{
		// Token: 0x060026CA RID: 9930 RVA: 0x00085836 File Offset: 0x00083A36
		[NullableContext(1)]
		public CodeWriterImpl(CodeWriter codeWriter)
		{
			if (codeWriter == null)
			{
				ThrowHelper.ThrowArgumentNullException_codeWriter();
			}
			this.codeWriter = codeWriter;
		}

		// Token: 0x060026CB RID: 9931 RVA: 0x0008584D File Offset: 0x00083A4D
		public override void WriteByte(byte value)
		{
			this.BytesWritten += 1U;
			this.codeWriter.WriteByte(value);
		}

		// Token: 0x04003967 RID: 14695
		public uint BytesWritten;

		// Token: 0x04003968 RID: 14696
		private readonly CodeWriter codeWriter;
	}
}
