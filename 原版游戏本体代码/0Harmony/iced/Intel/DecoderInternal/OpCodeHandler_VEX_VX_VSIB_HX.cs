using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C3 RID: 1987
	internal sealed class OpCodeHandler_VEX_VX_VSIB_HX : OpCodeHandlerModRM
	{
		// Token: 0x0600267C RID: 9852 RVA: 0x00083A7E File Offset: 0x00081C7E
		public OpCodeHandler_VEX_VX_VSIB_HX(Register baseReg1, Register vsibIndex, Register baseReg3, Code code)
		{
			this.baseReg1 = baseReg1;
			this.vsibIndex = vsibIndex;
			this.baseReg3 = baseReg3;
			this.code = code;
		}

		// Token: 0x0600267D RID: 9853 RVA: 0x00083AA4 File Offset: 0x00081CA4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			int regNum = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase);
			instruction.Op0Register = regNum + this.baseReg1;
			instruction.Op2Register = (int)decoder.state.vvvv + this.baseReg3;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem_VSIB(ref instruction, this.vsibIndex, TupleType.N1);
			if (decoder.invalidCheckMask != 0U)
			{
				uint indexNum = (uint)((instruction.MemoryIndex - Register.XMM0) % 32);
				if (regNum == (int)indexNum || decoder.state.vvvv == indexNum || regNum == (int)decoder.state.vvvv)
				{
					decoder.SetInvalidInstruction();
				}
			}
		}

		// Token: 0x040038D9 RID: 14553
		private readonly Register baseReg1;

		// Token: 0x040038DA RID: 14554
		private readonly Register vsibIndex;

		// Token: 0x040038DB RID: 14555
		private readonly Register baseReg3;

		// Token: 0x040038DC RID: 14556
		private readonly Code code;
	}
}
