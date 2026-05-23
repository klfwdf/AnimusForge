using System;
using System.Security;

namespace System.Diagnostics
{
	// Token: 0x020003E3 RID: 995
	internal class DefaultFilter : AssertFilter
	{
		// Token: 0x060032F8 RID: 13048 RVA: 0x000C4B87 File Offset: 0x000C2D87
		internal DefaultFilter()
		{
		}

		// Token: 0x060032F9 RID: 13049 RVA: 0x000C4B8F File Offset: 0x000C2D8F
		[SecuritySafeCritical]
		public override AssertFilters AssertFailure(string condition, string message, StackTrace location, StackTrace.TraceFormat stackTraceFormat, string windowTitle)
		{
			return (AssertFilters)Assert.ShowDefaultAssertDialog(condition, message, location.ToString(stackTraceFormat), windowTitle);
		}
	}
}
