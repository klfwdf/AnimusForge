using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x0200062C RID: 1580
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct BlockEncoderResult
	{
		// Token: 0x060020F1 RID: 8433 RVA: 0x00068522 File Offset: 0x00066722
		[NullableContext(2)]
		internal BlockEncoderResult(ulong rip, List<RelocInfo> relocInfos, uint[] newInstructionOffsets, ConstantOffsets[] constantOffsets)
		{
			this.RIP = rip;
			this.RelocInfos = relocInfos;
			this.NewInstructionOffsets = newInstructionOffsets ?? Array2.Empty<uint>();
			this.ConstantOffsets = constantOffsets ?? Array2.Empty<ConstantOffsets>();
		}

		// Token: 0x04001683 RID: 5763
		public readonly ulong RIP;

		// Token: 0x04001684 RID: 5764
		[Nullable(2)]
		public readonly List<RelocInfo> RelocInfos;

		// Token: 0x04001685 RID: 5765
		public readonly uint[] NewInstructionOffsets;

		// Token: 0x04001686 RID: 5766
		public readonly ConstantOffsets[] ConstantOffsets;
	}
}
