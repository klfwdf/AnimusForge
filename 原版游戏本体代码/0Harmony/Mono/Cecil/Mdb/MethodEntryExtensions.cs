using System;
using Mono.CompilerServices.SymbolWriter;

namespace Mono.Cecil.Mdb
{
	// Token: 0x02000351 RID: 849
	internal static class MethodEntryExtensions
	{
		// Token: 0x060015D4 RID: 5588 RVA: 0x00045A72 File Offset: 0x00043C72
		public static bool HasColumnInfo(this MethodEntry entry)
		{
			return (entry.MethodFlags & MethodEntry.Flags.ColumnsInfoIncluded) > (MethodEntry.Flags)0;
		}

		// Token: 0x060015D5 RID: 5589 RVA: 0x00045A7F File Offset: 0x00043C7F
		public static bool HasEndInfo(this MethodEntry entry)
		{
			return (entry.MethodFlags & MethodEntry.Flags.EndInfoIncluded) > (MethodEntry.Flags)0;
		}
	}
}
