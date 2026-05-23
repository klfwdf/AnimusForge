using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D2 RID: 2002
	internal sealed class OpCodeHandler_VEX_K_Jb : OpCodeHandlerModRM
	{
		// Token: 0x0600269A RID: 9882 RVA: 0x00084681 File Offset: 0x00082881
		public OpCodeHandler_VEX_K_Jb(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600269B RID: 9883 RVA: 0x00084690 File Offset: 0x00082890
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.BranchImm8;
			if (decoder.invalidCheckMask != 0U && decoder.state.vvvv > 7U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op0Register = (int)(decoder.state.vvvv & 7U) + Register.K0;
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Kind = OpKind.NearBranch64;
			instruction.NearBranch64 = (ulong)((long)((sbyte)decoder.state.modrm) + (long)decoder.GetCurrentInstructionPointer64());
		}

		// Token: 0x040038F5 RID: 14581
		private readonly Code code;
	}
}
