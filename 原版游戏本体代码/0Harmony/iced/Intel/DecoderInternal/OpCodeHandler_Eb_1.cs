using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200076A RID: 1898
	internal sealed class OpCodeHandler_Eb_1 : OpCodeHandlerModRM
	{
		// Token: 0x060025BF RID: 9663 RVA: 0x0007FA01 File Offset: 0x0007DC01
		public OpCodeHandler_Eb_1(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025C0 RID: 9664 RVA: 0x0007FA10 File Offset: 0x0007DC10
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = 1U;
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.NoImm;
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

		// Token: 0x04003837 RID: 14391
		private readonly Code code;
	}
}
