using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200072C RID: 1836
	internal sealed class OpCodeHandler_Jdisp : OpCodeHandler
	{
		// Token: 0x0600253F RID: 9535 RVA: 0x0007D1E4 File Offset: 0x0007B3E4
		public OpCodeHandler_Jdisp(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x06002540 RID: 9536 RVA: 0x0007D1FC File Offset: 0x0007B3FC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Kind = OpKind.NearBranch32;
				instruction.NearBranch32 = decoder.ReadUInt32();
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code16);
			instruction.Op0Kind = OpKind.NearBranch16;
			instruction.InternalNearBranch16 = decoder.ReadUInt16();
		}

		// Token: 0x040037C3 RID: 14275
		private readonly Code code16;

		// Token: 0x040037C4 RID: 14276
		private readonly Code code32;
	}
}
