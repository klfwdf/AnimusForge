using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000702 RID: 1794
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_PrefixREX : OpCodeHandler
	{
		// Token: 0x060024E8 RID: 9448 RVA: 0x0007B309 File Offset: 0x00079509
		public OpCodeHandler_PrefixREX(OpCodeHandler handler, uint rex)
		{
			if (handler == null)
			{
				throw new InvalidOperationException();
			}
			this.handler = handler;
			this.rex = rex;
		}

		// Token: 0x060024E9 RID: 9449 RVA: 0x0007B32C File Offset: 0x0007952C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if ((this.rex & 8U) != 0U)
				{
					decoder.state.operandSize = OpSize.Size64;
					decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags.HasRex | StateFlags.W);
				}
				else
				{
					decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.HasRex;
					decoder.state.zs.flags = decoder.state.zs.flags & ~StateFlags.W;
					if ((decoder.state.zs.flags & StateFlags.Has66) == (StateFlags)0U)
					{
						decoder.state.operandSize = OpSize.Size32;
					}
					else
					{
						decoder.state.operandSize = OpSize.Size16;
					}
				}
				decoder.state.zs.extraRegisterBase = (this.rex << 1) & 8U;
				decoder.state.zs.extraIndexRegisterBase = (this.rex << 2) & 8U;
				decoder.state.zs.extraBaseRegisterBase = (this.rex << 3) & 8U;
				decoder.CallOpCodeHandlerXXTable(ref instruction);
				return;
			}
			this.handler.Decode(decoder, ref instruction);
		}

		// Token: 0x0400376F RID: 14191
		private readonly OpCodeHandler handler;

		// Token: 0x04003770 RID: 14192
		private readonly uint rex;
	}
}
