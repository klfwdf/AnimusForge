using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D3 RID: 2003
	internal sealed class OpCodeHandler_VEX_K_Jz : OpCodeHandlerModRM
	{
		// Token: 0x0600269C RID: 9884 RVA: 0x00084718 File Offset: 0x00082918
		public OpCodeHandler_VEX_K_Jz(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600269D RID: 9885 RVA: 0x00084728 File Offset: 0x00082928
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.invalidCheckMask != 0U && decoder.state.vvvv > 7U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op0Register = (int)(decoder.state.vvvv & 7U) + Register.K0;
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Kind = OpKind.NearBranch64;
			uint imm = decoder.state.modrm | (decoder.ReadByte() << 8) | (decoder.ReadByte() << 16) | (decoder.ReadByte() << 24);
			instruction.NearBranch64 = (ulong)((long)imm + (long)decoder.GetCurrentInstructionPointer64());
		}

		// Token: 0x040038F6 RID: 14582
		private readonly Code code;
	}
}
