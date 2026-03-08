using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200076B RID: 1899
	internal sealed class OpCodeHandler_Eb_CL : OpCodeHandlerModRM
	{
		// Token: 0x060025C1 RID: 9665 RVA: 0x0007FAB1 File Offset: 0x0007DCB1
		public OpCodeHandler_Eb_CL(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025C2 RID: 9666 RVA: 0x0007FAC0 File Offset: 0x0007DCC0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = Register.CL;
			if (decoder.state.mod == 3U)
			{
				uint index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
				if ((decoder.state.zs.flags & StateFlags.HasRex) != (StateFlags)0U && index >= 4U)
				{
					index += 4U;
				}
				instruction.Op0Register = (int)index + Register.AL;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003838 RID: 14392
		private readonly Code code;
	}
}
