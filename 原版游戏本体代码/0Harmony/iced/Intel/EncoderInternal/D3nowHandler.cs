using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000678 RID: 1656
	internal sealed class D3nowHandler : OpCodeHandler
	{
		// Token: 0x060023DA RID: 9178 RVA: 0x00073BAD File Offset: 0x00071DAD
		public D3nowHandler(EncFlags2 encFlags2, EncFlags3 encFlags3)
			: base((EncFlags2)(((ulong)encFlags2 & 18446744073709486080UL) | 15UL), encFlags3, false, null, D3nowHandler.operands)
		{
			this.immediate = OpCodeHandler.GetOpCode(encFlags2);
		}

		// Token: 0x060023DB RID: 9179 RVA: 0x00073BD7 File Offset: 0x00071DD7
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			encoder.WritePrefixes(instruction, true);
			encoder.WriteByteInternal(15U);
			encoder.ImmSize = ImmSize.Size1OpCode;
			encoder.Immediate = this.immediate;
		}

		// Token: 0x04003474 RID: 13428
		private static readonly Op[] operands = new Op[]
		{
			new OpModRM_reg(Register.MM0, Register.MM7),
			new OpModRM_rm(Register.MM0, Register.MM7)
		};

		// Token: 0x04003475 RID: 13429
		private readonly uint immediate;
	}
}
