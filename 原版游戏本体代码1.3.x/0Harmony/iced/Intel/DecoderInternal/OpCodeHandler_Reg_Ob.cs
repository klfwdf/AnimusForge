using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200075E RID: 1886
	internal sealed class OpCodeHandler_Reg_Ob : OpCodeHandler
	{
		// Token: 0x060025A6 RID: 9638 RVA: 0x0007F265 File Offset: 0x0007D465
		public OpCodeHandler_Reg_Ob(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x060025A7 RID: 9639 RVA: 0x0007F27C File Offset: 0x0007D47C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = this.reg;
			decoder.displIndex = decoder.state.zs.instructionLength;
			instruction.Op1Kind = OpKind.Memory;
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.InternalSetMemoryDisplSize(4U);
				decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.Addr64;
				instruction.MemoryDisplacement64 = decoder.ReadUInt64();
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.InternalSetMemoryDisplSize(3U);
				instruction.MemoryDisplacement64 = (ulong)decoder.ReadUInt32();
				return;
			}
			instruction.InternalSetMemoryDisplSize(2U);
			instruction.MemoryDisplacement64 = (ulong)decoder.ReadUInt16();
		}

		// Token: 0x0400381E RID: 14366
		private readonly Code code;

		// Token: 0x0400381F RID: 14367
		private readonly Register reg;
	}
}
