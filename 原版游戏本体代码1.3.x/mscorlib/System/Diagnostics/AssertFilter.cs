using System;

namespace System.Diagnostics
{
	// Token: 0x020003E2 RID: 994
	[Serializable]
	internal abstract class AssertFilter
	{
		// Token: 0x060032F6 RID: 13046
		public abstract AssertFilters AssertFailure(string condition, string message, StackTrace location, StackTrace.TraceFormat stackTraceFormat, string windowTitle);
	}
}
