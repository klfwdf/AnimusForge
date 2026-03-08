using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200078D RID: 1933
	internal sealed class OpCodeHandler_B_MIB : OpCodeHandlerModRM
	{
		// Token: 0x0600260B RID: 9739 RVA: 0x0008115D File Offset: 0x0007F35D
		public OpCodeHandler_B_MIB(Code code)
		{
			this.code = code;
		}

		// Token: 0x0600260C RID: 9740 RVA: 0x0008116C File Offset: 0x0007F36C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.reg > 3U || (decoder.state.zs.extraRegisterBase & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.BND0;
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem_MPX(ref instruction);
			if (decoder.invalidCheckMask != 0U && instruction.MemoryBase == Register.RIP)
			{
				decoder.SetInvalidInstruction();
			}
		}

		// Token: 0x0400386C RID: 14444
		private readonly Code code;
	}
}
