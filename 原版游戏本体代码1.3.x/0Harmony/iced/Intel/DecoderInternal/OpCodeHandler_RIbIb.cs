using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000786 RID: 1926
	internal sealed class OpCodeHandler_RIbIb : OpCodeHandlerModRM
	{
		// Token: 0x060025FD RID: 9725 RVA: 0x00080C2B File Offset: 0x0007EE2B
		public OpCodeHandler_RIbIb(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025FE RID: 9726 RVA: 0x00080C3C File Offset: 0x0007EE3C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
			instruction.Op2Kind = OpKind.Immediate8_2nd;
			instruction.InternalImmediate8_2nd = decoder.ReadByte();
		}

		// Token: 0x04003860 RID: 14432
		private readonly Code code;
	}
}
