using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006F4 RID: 1780
	internal sealed class OpCodeHandler_Mf : OpCodeHandlerModRM
	{
		// Token: 0x060024CE RID: 9422 RVA: 0x0007AFAE File Offset: 0x000791AE
		public OpCodeHandler_Mf(Code code)
		{
			this.code16 = code;
			this.code32 = code;
		}

		// Token: 0x060024CF RID: 9423 RVA: 0x0007AFC4 File Offset: 0x000791C4
		public OpCodeHandler_Mf(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x060024D0 RID: 9424 RVA: 0x0007AFDA File Offset: 0x000791DA
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003765 RID: 14181
		private readonly Code code16;

		// Token: 0x04003766 RID: 14182
		private readonly Code code32;
	}
}
