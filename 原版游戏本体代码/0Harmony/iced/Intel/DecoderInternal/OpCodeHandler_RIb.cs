using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000787 RID: 1927
	internal sealed class OpCodeHandler_RIb : OpCodeHandlerModRM
	{
		// Token: 0x060025FF RID: 9727 RVA: 0x00080CB6 File Offset: 0x0007EEB6
		public OpCodeHandler_RIb(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002600 RID: 9728 RVA: 0x00080CC8 File Offset: 0x0007EEC8
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
		}

		// Token: 0x04003861 RID: 14433
		private readonly Code code;
	}
}
