using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000670 RID: 1648
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class InvalidHandler : OpCodeHandler
	{
		// Token: 0x060023C5 RID: 9157 RVA: 0x0007308B File Offset: 0x0007128B
		public InvalidHandler()
			: base(EncFlags2.None, EncFlags3.Bit16or32 | EncFlags3.Bit64, false, null, Array2.Empty<Op>())
		{
		}

		// Token: 0x060023C6 RID: 9158 RVA: 0x000730A0 File Offset: 0x000712A0
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
			encoder.ErrorMessage = "Can't encode an invalid instruction";
		}

		// Token: 0x0400345F RID: 13407
		internal const string ERROR_MESSAGE = "Can't encode an invalid instruction";
	}
}
