using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200071A RID: 1818
	internal sealed class OpCodeHandler_Ev_REXW : OpCodeHandlerModRM
	{
		// Token: 0x0600251B RID: 9499 RVA: 0x0007C0CF File Offset: 0x0007A2CF
		public OpCodeHandler_Ev_REXW(Code code32, Code code64, uint flags)
		{
			this.code32 = code32;
			this.code64 = code64;
			this.flags = flags;
			this.disallowReg = (((flags & 1U) != 0U) ? 0U : uint.MaxValue);
			this.disallowMem = (((flags & 2U) != 0U) ? 0U : uint.MaxValue);
		}

		// Token: 0x0600251C RID: 9500 RVA: 0x0007C10C File Offset: 0x0007A30C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			if ((((this.flags & 4U) | (uint)(decoder.state.zs.flags & StateFlags.Has66)) & decoder.invalidCheckMask) == 32772U)
			{
				decoder.SetInvalidInstruction();
			}
			if (decoder.state.mod == 3U)
			{
				if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
				{
					instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.RAX;
				}
				else
				{
					instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.EAX;
				}
				if ((this.disallowReg & decoder.invalidCheckMask) != 0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				if ((this.disallowMem & decoder.invalidCheckMask) != 0U)
				{
					decoder.SetInvalidInstruction();
				}
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
		}

		// Token: 0x04003797 RID: 14231
		private readonly Code code32;

		// Token: 0x04003798 RID: 14232
		private readonly Code code64;

		// Token: 0x04003799 RID: 14233
		private readonly uint flags;

		// Token: 0x0400379A RID: 14234
		private readonly uint disallowReg;

		// Token: 0x0400379B RID: 14235
		private readonly uint disallowMem;
	}
}
