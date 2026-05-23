using System;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200066E RID: 1646
	// (Invoke) Token: 0x060023BF RID: 9151
	internal delegate bool TryConvertToDisp8N(Encoder encoder, OpCodeHandler handler, in Instruction instruction, int displ, out sbyte compressedValue);
}
