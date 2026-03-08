using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200075D RID: 1885
	internal sealed class OpCodeHandler_Ap : OpCodeHandler
	{
		// Token: 0x060025A4 RID: 9636 RVA: 0x0007F1D8 File Offset: 0x0007D3D8
		public OpCodeHandler_Ap(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x060025A5 RID: 9637 RVA: 0x0007F1F0 File Offset: 0x0007D3F0
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
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.Op0Kind = OpKind.FarBranch32;
				instruction.FarBranch32 = decoder.ReadUInt32();
			}
			else
			{
				instruction.Op0Kind = OpKind.FarBranch16;
				instruction.InternalFarBranch16 = decoder.ReadUInt16();
			}
			instruction.InternalFarBranchSelector = decoder.ReadUInt16();
		}

		// Token: 0x0400381C RID: 14364
		private readonly Code code16;

		// Token: 0x0400381D RID: 14365
		private readonly Code code32;
	}
}
