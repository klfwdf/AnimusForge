using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000672 RID: 1650
	internal sealed class ZeroBytesHandler : OpCodeHandler
	{
		// Token: 0x060023C9 RID: 9161 RVA: 0x000731A0 File Offset: 0x000713A0
		public ZeroBytesHandler(Code code)
			: base(EncFlags2.None, EncFlags3.Bit16or32 | EncFlags3.Bit64, true, null, Array2.Empty<Op>())
		{
		}

		// Token: 0x060023CA RID: 9162 RVA: 0x0001B842 File Offset: 0x00019A42
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction)
		{
		}
	}
}
