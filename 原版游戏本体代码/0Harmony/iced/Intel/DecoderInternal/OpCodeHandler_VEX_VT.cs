using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007CE RID: 1998
	internal sealed class OpCodeHandler_VEX_VT : OpCodeHandlerModRM
	{
		// Token: 0x06002692 RID: 9874 RVA: 0x0008437F File Offset: 0x0008257F
		public OpCodeHandler_VEX_VT(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002693 RID: 9875 RVA: 0x00084390 File Offset: 0x00082590
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (((decoder.state.vvvv_invalidCheck | decoder.state.zs.extraRegisterBase) & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)decoder.state.reg + Register.TMM0;
		}

		// Token: 0x040038F0 RID: 14576
		private readonly Code code;
	}
}
