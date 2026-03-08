using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Iced.Intel;

namespace MonoMod.Core.Utils
{
	// Token: 0x020004EE RID: 1262
	[NullableContext(1)]
	[Nullable(0)]
	internal static class IcedExtensions
	{
		// Token: 0x06001C38 RID: 7224 RVA: 0x00003BBE File Offset: 0x00001DBE
		[Obsolete("This method is not supported.", true)]
		public static string FormatInsns(this IList<Instruction> insns)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06001C39 RID: 7225 RVA: 0x00003BBE File Offset: 0x00001DBE
		[Obsolete("This method is not supported.", true)]
		public static string FormatInsns(this InstructionList insns)
		{
			throw new NotSupportedException();
		}
	}
}
