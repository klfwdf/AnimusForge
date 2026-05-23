using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200078E RID: 1934
	internal sealed class OpCodeHandler_MIB_B : OpCodeHandlerModRM
	{
		// Token: 0x0600260D RID: 9741 RVA: 0x000811F0 File Offset: 0x0007F3F0
		public OpCodeHandler_MIB_B(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600260E RID: 9742 RVA: 0x00081200 File Offset: 0x0007F400
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.reg > 3U || (decoder.state.zs.extraRegisterBase & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)decoder.state.reg + Register.BND0;
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem_MPX(ref instruction);
			if (decoder.invalidCheckMask != 0U && instruction.MemoryBase == Register.RIP)
			{
				decoder.SetInvalidInstruction();
			}
		}

		// Token: 0x0400386D RID: 14445
		private readonly Code code;
	}
}
