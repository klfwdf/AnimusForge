using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000778 RID: 1912
	internal sealed class OpCodeHandler_VQ : OpCodeHandlerModRM
	{
		// Token: 0x060025E0 RID: 9696 RVA: 0x00080357 File Offset: 0x0007E557
		public OpCodeHandler_VQ(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025E1 RID: 9697 RVA: 0x00080368 File Offset: 0x0007E568
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.XMM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.MM0;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400384C RID: 14412
		private readonly Code code;
	}
}
