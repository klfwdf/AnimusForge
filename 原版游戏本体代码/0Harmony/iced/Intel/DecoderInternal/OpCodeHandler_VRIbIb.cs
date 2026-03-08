using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000785 RID: 1925
	internal sealed class OpCodeHandler_VRIbIb : OpCodeHandlerModRM
	{
		// Token: 0x060025FB RID: 9723 RVA: 0x00080B7C File Offset: 0x0007ED7C
		public OpCodeHandler_VRIbIb(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025FC RID: 9724 RVA: 0x00080B8C File Offset: 0x0007ED8C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.XMM0;
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
			instruction.Op3Kind = OpKind.Immediate8_2nd;
			instruction.InternalImmediate8_2nd = decoder.ReadByte();
		}

		// Token: 0x0400385F RID: 14431
		private readonly Code code;
	}
}
