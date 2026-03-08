using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006EF RID: 1775
	internal sealed class OpCodeHandler_EVEX_GvM_VX_Ib : OpCodeHandlerModRM
	{
		// Token: 0x060024C4 RID: 9412 RVA: 0x0007AC40 File Offset: 0x00078E40
		public OpCodeHandler_EVEX_GvM_VX_Ib(Register baseReg, Code code32, Code code64, TupleType tupleType32, TupleType tupleType64)
		{
			this.baseReg = baseReg;
			this.code32 = code32;
			this.code64 = code64;
			this.tupleType32 = tupleType32;
			this.tupleType64 = tupleType64;
		}

		// Token: 0x060024C5 RID: 9413 RVA: 0x0007AC70 File Offset: 0x00078E70
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				gpr = Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
				{
					decoder.ReadOpMem(ref instruction, this.tupleType64);
				}
				else
				{
					decoder.ReadOpMem(ref instruction, this.tupleType32);
				}
			}
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003759 RID: 14169
		private readonly Register baseReg;

		// Token: 0x0400375A RID: 14170
		private readonly Code code32;

		// Token: 0x0400375B RID: 14171
		private readonly Code code64;

		// Token: 0x0400375C RID: 14172
		private readonly TupleType tupleType32;

		// Token: 0x0400375D RID: 14173
		private readonly TupleType tupleType64;
	}
}
