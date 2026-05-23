using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000740 RID: 1856
	internal sealed class OpCodeHandler_Xchg_Reg_rAX : OpCodeHandler
	{
		// Token: 0x06002568 RID: 9576 RVA: 0x0007DE94 File Offset: 0x0007C094
		public OpCodeHandler_Xchg_Reg_rAX(int index)
		{
			this.index = index;
			this.codes = OpCodeHandler_Xchg_Reg_rAX.s_codes;
		}

		// Token: 0x06002569 RID: 9577 RVA: 0x0007DEB0 File Offset: 0x0007C0B0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (this.index == 0 && decoder.state.zs.mandatoryPrefix == MandatoryPrefixByte.PF3 && (decoder.options & DecoderOptions.NoPause) == DecoderOptions.None)
			{
				decoder.ClearMandatoryPrefixF3(ref instruction);
				instruction.InternalSetCodeNoCheck(Code.Pause);
				return;
			}
			int sizeIndex = (int)decoder.state.operandSize;
			int codeIndex = this.index + (int)decoder.state.zs.extraBaseRegisterBase;
			instruction.InternalSetCodeNoCheck(this.codes[sizeIndex * 16 + codeIndex]);
			if (codeIndex != 0)
			{
				Register reg = sizeIndex * 16 + codeIndex + Register.AX;
				instruction.Op0Register = reg;
				instruction.Op1Register = sizeIndex * 16 + Register.AX;
			}
		}

		// Token: 0x040037EA RID: 14314
		private readonly int index;

		// Token: 0x040037EB RID: 14315
		private readonly Code[] codes;

		// Token: 0x040037EC RID: 14316
		private static readonly Code[] s_codes = new Code[]
		{
			Code.Nopw,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Xchg_r16_AX,
			Code.Nopd,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Xchg_r32_EAX,
			Code.Nopq,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX,
			Code.Xchg_r64_RAX
		};
	}
}
