using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200066F RID: 1647
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class OpCodeHandler
	{
		// Token: 0x060023C2 RID: 9154 RVA: 0x00072FF4 File Offset: 0x000711F4
		protected OpCodeHandler(EncFlags2 encFlags2, EncFlags3 encFlags3, bool isSpecialInstr, [Nullable(2)] TryConvertToDisp8N tryConvertToDisp8N, Op[] operands)
		{
			this.EncFlags3 = encFlags3;
			this.OpCode = OpCodeHandler.GetOpCode(encFlags2);
			this.Is2ByteOpCode = (encFlags2 & EncFlags2.OpCodeIs2Bytes) > EncFlags2.None;
			this.GroupIndex = (int)(((encFlags2 & (EncFlags2)2147483648U) == EncFlags2.None) ? ((EncFlags2)4294967295U) : ((encFlags2 >> 27) & EncFlags2.TableMask));
			this.RmGroupIndex = (int)(((encFlags3 & EncFlags3.HasRmGroupIndex) == EncFlags3.None) ? ((EncFlags2)4294967295U) : ((encFlags2 >> 27) & EncFlags2.TableMask));
			this.IsSpecialInstr = isSpecialInstr;
			this.OpSize = (CodeSize)((encFlags3 >> 3) & EncFlags3.OperandSizeShift);
			this.AddrSize = (CodeSize)((encFlags3 >> 5) & EncFlags3.OperandSizeShift);
			this.TryConvertToDisp8N = tryConvertToDisp8N;
			this.Operands = operands;
		}

		// Token: 0x060023C3 RID: 9155 RVA: 0x00073087 File Offset: 0x00071287
		protected static uint GetOpCode(EncFlags2 encFlags2)
		{
			return (uint)((ushort)encFlags2);
		}

		// Token: 0x060023C4 RID: 9156
		public abstract void Encode(Encoder encoder, in Instruction instruction);

		// Token: 0x04003455 RID: 13397
		internal readonly uint OpCode;

		// Token: 0x04003456 RID: 13398
		internal readonly bool Is2ByteOpCode;

		// Token: 0x04003457 RID: 13399
		internal readonly int GroupIndex;

		// Token: 0x04003458 RID: 13400
		internal readonly int RmGroupIndex;

		// Token: 0x04003459 RID: 13401
		internal readonly bool IsSpecialInstr;

		// Token: 0x0400345A RID: 13402
		internal readonly EncFlags3 EncFlags3;

		// Token: 0x0400345B RID: 13403
		internal readonly CodeSize OpSize;

		// Token: 0x0400345C RID: 13404
		internal readonly CodeSize AddrSize;

		// Token: 0x0400345D RID: 13405
		[Nullable(2)]
		internal readonly TryConvertToDisp8N TryConvertToDisp8N;

		// Token: 0x0400345E RID: 13406
		internal readonly Op[] Operands;
	}
}
