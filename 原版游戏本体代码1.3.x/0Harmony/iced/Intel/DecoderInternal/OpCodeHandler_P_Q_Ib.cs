using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200077C RID: 1916
	internal sealed class OpCodeHandler_P_Q_Ib : OpCodeHandlerModRM
	{
		// Token: 0x060025E8 RID: 9704 RVA: 0x00080531 File Offset: 0x0007E731
		public OpCodeHandler_P_Q_Ib(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025E9 RID: 9705 RVA: 0x00080540 File Offset: 0x0007E740
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.MM0;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003850 RID: 14416
		private readonly Code code;
	}
}
