using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200075F RID: 1887
	internal sealed class OpCodeHandler_Ob_Reg : OpCodeHandler
	{
		// Token: 0x060025A8 RID: 9640 RVA: 0x0007F331 File Offset: 0x0007D531
		public OpCodeHandler_Ob_Reg(Code code, Register reg)
		{
			this.code = code;
			this.reg = reg;
		}

		// Token: 0x060025A9 RID: 9641 RVA: 0x0007F348 File Offset: 0x0007D548
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			decoder.displIndex = decoder.state.zs.instructionLength;
			instruction.Op0Kind = OpKind.Memory;
			instruction.Op1Register = this.reg;
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

		// Token: 0x04003820 RID: 14368
		private readonly Code code;

		// Token: 0x04003821 RID: 14369
		private readonly Register reg;
	}
}
