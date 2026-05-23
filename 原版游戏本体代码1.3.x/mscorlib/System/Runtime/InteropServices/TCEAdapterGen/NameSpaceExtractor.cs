using System;

namespace System.Runtime.InteropServices.TCEAdapterGen
{
	// Token: 0x020009C4 RID: 2500
	internal static class NameSpaceExtractor
	{
		// Token: 0x060063B6 RID: 25526 RVA: 0x00154660 File Offset: 0x00152860
		public static string ExtractNameSpace(string FullyQualifiedTypeName)
		{
			int num = FullyQualifiedTypeName.LastIndexOf(NameSpaceExtractor.NameSpaceSeperator);
			if (num == -1)
			{
				return "";
			}
			return FullyQualifiedTypeName.Substring(0, num);
		}

		// Token: 0x04002CD6 RID: 11478
		private static char NameSpaceSeperator = '.';
	}
}
