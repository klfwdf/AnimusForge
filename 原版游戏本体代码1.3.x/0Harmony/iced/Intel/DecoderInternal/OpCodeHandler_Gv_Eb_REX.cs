using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000798 RID: 1944
	internal sealed class OpCodeHandler_Gv_Eb_REX : OpCodeHandlerModRM
	{
		// Token: 0x06002621 RID: 9761 RVA: 0x000819B7 File Offset: 0x0007FBB7
		public OpCodeHandler_Gv_Eb_REX(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002622 RID: 9762 RVA: 0x000819D0 File Offset: 0x0007FBD0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				uint index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
				if ((decoder.state.zs.flags & StateFlags.HasRex) != (StateFlags)0U && index >= 4U)
				{
					index += 4U;
				}
				instruction.Op1Register = (int)index + Register.AL;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400387E RID: 14462
		private readonly Code code32;

		// Token: 0x0400387F RID: 14463
		private readonly Code code64;
	}
}
