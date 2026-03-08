using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200079C RID: 1948
	internal sealed class OpCodeHandler_Wbinvd : OpCodeHandler
	{
		// Token: 0x0600262A RID: 9770 RVA: 0x00081D68 File Offset: 0x0007FF68
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.options & DecoderOptions.NoWbnoinvd) != DecoderOptions.None || decoder.state.zs.mandatoryPrefix != MandatoryPrefixByte.PF3)
			{
				instruction.InternalSetCodeNoCheck(Code.Wbinvd);
				return;
			}
			decoder.ClearMandatoryPrefixF3(ref instruction);
			instruction.InternalSetCodeNoCheck(Code.Wbnoinvd);
		}
	}
}
