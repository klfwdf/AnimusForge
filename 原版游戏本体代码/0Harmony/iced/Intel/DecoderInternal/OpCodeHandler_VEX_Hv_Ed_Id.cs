using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C8 RID: 1992
	internal sealed class OpCodeHandler_VEX_Hv_Ed_Id : OpCodeHandlerModRM
	{
		// Token: 0x06002686 RID: 9862 RVA: 0x00083EA0 File Offset: 0x000820A0
		public OpCodeHandler_VEX_Hv_Ed_Id(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002687 RID: 9863 RVA: 0x00083EB8 File Offset: 0x000820B8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op0Register = (int)decoder.state.vvvv + Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = (int)decoder.state.vvvv + Register.EAX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.EAX;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op2Kind = OpKind.Immediate32;
			instruction.Immediate32 = decoder.ReadUInt32();
		}

		// Token: 0x040038E5 RID: 14565
		private readonly Code code32;

		// Token: 0x040038E6 RID: 14566
		private readonly Code code64;
	}
}
