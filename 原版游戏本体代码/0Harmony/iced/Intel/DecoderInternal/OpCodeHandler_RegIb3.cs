using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000742 RID: 1858
	internal sealed class OpCodeHandler_RegIb3 : OpCodeHandler
	{
		// Token: 0x0600256D RID: 9581 RVA: 0x0007E02A File Offset: 0x0007C22A
		public OpCodeHandler_RegIb3(int index)
		{
			this.index = index;
			this.withRexPrefix = OpCodeHandler_RegIb3.s_withRexPrefix;
		}

		// Token: 0x0600256E RID: 9582 RVA: 0x0007E044 File Offset: 0x0007C244
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register register;
			if ((decoder.state.zs.flags & StateFlags.HasRex) != (StateFlags)0U)
			{
				register = this.withRexPrefix[this.index + (int)decoder.state.zs.extraBaseRegisterBase];
			}
			else
			{
				register = this.index + Register.AL;
			}
			instruction.InternalSetCodeNoCheck(Code.Mov_r8_imm8);
			instruction.Op0Register = register;
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040037F0 RID: 14320
		private readonly int index;

		// Token: 0x040037F1 RID: 14321
		private readonly Register[] withRexPrefix;

		// Token: 0x040037F2 RID: 14322
		private static readonly Register[] s_withRexPrefix = new Register[]
		{
			Register.AL,
			Register.CL,
			Register.DL,
			Register.BL,
			Register.SPL,
			Register.BPL,
			Register.SIL,
			Register.DIL,
			Register.R8L,
			Register.R9L,
			Register.R10L,
			Register.R11L,
			Register.R12L,
			Register.R13L,
			Register.R14L,
			Register.R15L
		};
	}
}
