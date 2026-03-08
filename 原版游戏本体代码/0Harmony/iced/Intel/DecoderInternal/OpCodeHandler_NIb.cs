using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200070F RID: 1807
	internal sealed class OpCodeHandler_NIb : OpCodeHandlerModRM
	{
		// Token: 0x06002501 RID: 9473 RVA: 0x0007B9A3 File Offset: 0x00079BA3
		public OpCodeHandler_NIb(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002502 RID: 9474 RVA: 0x0007B9B4 File Offset: 0x00079BB4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)decoder.state.rm + Register.MM0;
			}
			else
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003786 RID: 14214
		private readonly Code code;
	}
}
