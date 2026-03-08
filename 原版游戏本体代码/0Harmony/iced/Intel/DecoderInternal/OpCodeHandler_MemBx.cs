using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000771 RID: 1905
	internal sealed class OpCodeHandler_MemBx : OpCodeHandler
	{
		// Token: 0x060025D1 RID: 9681 RVA: 0x0007FF4D File Offset: 0x0007E14D
		public OpCodeHandler_MemBx(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025D2 RID: 9682 RVA: 0x0007FF5C File Offset: 0x0007E15C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.InternalMemoryIndex = Register.AL;
			instruction.Op0Kind = OpKind.Memory;
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.InternalMemoryBase = Register.RBX;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.InternalMemoryBase = Register.EBX;
				return;
			}
			instruction.InternalMemoryBase = Register.BX;
		}

		// Token: 0x04003844 RID: 14404
		private readonly Code code;
	}
}
