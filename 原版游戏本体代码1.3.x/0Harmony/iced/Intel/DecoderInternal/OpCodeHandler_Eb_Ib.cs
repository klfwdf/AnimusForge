using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000769 RID: 1897
	internal sealed class OpCodeHandler_Eb_Ib : OpCodeHandlerModRM
	{
		// Token: 0x060025BC RID: 9660 RVA: 0x0007F92E File Offset: 0x0007DB2E
		public OpCodeHandler_Eb_Ib(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025BD RID: 9661 RVA: 0x0007F93D File Offset: 0x0007DB3D
		public OpCodeHandler_Eb_Ib(Code code, HandlerFlags flags)
		{
			this.code = code;
			this.flags = flags;
		}

		// Token: 0x060025BE RID: 9662 RVA: 0x0007F954 File Offset: 0x0007DB54
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod < 3U)
			{
				decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((this.flags & HandlerFlags.Lock) << 10);
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			else
			{
				uint index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
				if ((decoder.state.zs.flags & StateFlags.HasRex) != (StateFlags)0U && index >= 4U)
				{
					index += 4U;
				}
				instruction.Op0Register = (int)index + Register.AL;
			}
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x04003835 RID: 14389
		private readonly Code code;

		// Token: 0x04003836 RID: 14390
		private readonly HandlerFlags flags;
	}
}
