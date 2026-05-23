using System;
using System.IO;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x0200002B RID: 43
	internal static class InlineSignatureParser
	{
		// Token: 0x060000D6 RID: 214 RVA: 0x00005A5C File Offset: 0x00003C5C
		internal static InlineSignature ImportCallSite(Module moduleFrom, byte[] data)
		{
			InlineSignatureParser.<>c__DisplayClass0_0 CS$<>8__locals1 = new InlineSignatureParser.<>c__DisplayClass0_0();
			CS$<>8__locals1.moduleFrom = moduleFrom;
			InlineSignature callsite = new InlineSignature();
			InlineSignature result;
			using (MemoryStream stream = new MemoryStream(data, false))
			{
				CS$<>8__locals1.reader = new BinaryReader(stream);
				try
				{
					CS$<>8__locals1.<ImportCallSite>g__ReadMethodSignature|0(callsite);
					result = callsite;
				}
				finally
				{
					if (CS$<>8__locals1.reader != null)
					{
						((IDisposable)CS$<>8__locals1.reader).Dispose();
					}
				}
			}
			return result;
		}
	}
}
