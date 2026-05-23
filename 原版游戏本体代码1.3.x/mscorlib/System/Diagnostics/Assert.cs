using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Diagnostics
{
	// Token: 0x020003E1 RID: 993
	internal static class Assert
	{
		// Token: 0x060032EF RID: 13039 RVA: 0x000C4ACD File Offset: 0x000C2CCD
		internal static void Check(bool condition, string conditionString, string message)
		{
			if (!condition)
			{
				Assert.Fail(conditionString, message, null, -2146232797);
			}
		}

		// Token: 0x060032F0 RID: 13040 RVA: 0x000C4ADF File Offset: 0x000C2CDF
		internal static void Check(bool condition, string conditionString, string message, int exitCode)
		{
			if (!condition)
			{
				Assert.Fail(conditionString, message, null, exitCode);
			}
		}

		// Token: 0x060032F1 RID: 13041 RVA: 0x000C4AED File Offset: 0x000C2CED
		internal static void Fail(string conditionString, string message)
		{
			Assert.Fail(conditionString, message, null, -2146232797);
		}

		// Token: 0x060032F2 RID: 13042 RVA: 0x000C4AFC File Offset: 0x000C2CFC
		internal static void Fail(string conditionString, string message, string windowTitle, int exitCode)
		{
			Assert.Fail(conditionString, message, windowTitle, exitCode, StackTrace.TraceFormat.Normal, 0);
		}

		// Token: 0x060032F3 RID: 13043 RVA: 0x000C4B09 File Offset: 0x000C2D09
		internal static void Fail(string conditionString, string message, int exitCode, StackTrace.TraceFormat stackTraceFormat)
		{
			Assert.Fail(conditionString, message, null, exitCode, stackTraceFormat, 0);
		}

		// Token: 0x060032F4 RID: 13044 RVA: 0x000C4B18 File Offset: 0x000C2D18
		[SecuritySafeCritical]
		internal static void Fail(string conditionString, string message, string windowTitle, int exitCode, StackTrace.TraceFormat stackTraceFormat, int numStackFramesToSkip)
		{
			StackTrace location = new StackTrace(numStackFramesToSkip, true);
			AssertFilters assertFilters = Assert.Filter.AssertFailure(conditionString, message, location, stackTraceFormat, windowTitle);
			if (assertFilters == AssertFilters.FailDebug)
			{
				if (Debugger.IsAttached)
				{
					Debugger.Break();
					return;
				}
				if (!Debugger.Launch())
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DebuggerLaunchFailed"));
				}
			}
			else if (assertFilters == AssertFilters.FailTerminate)
			{
				if (Debugger.IsAttached)
				{
					Environment._Exit(exitCode);
					return;
				}
				Environment.FailFast(message, (uint)exitCode);
			}
		}

		// Token: 0x060032F5 RID: 13045
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int ShowDefaultAssertDialog(string conditionString, string message, string stackTrace, string windowTitle);

		// Token: 0x04001699 RID: 5785
		internal const int COR_E_FAILFAST = -2146232797;

		// Token: 0x0400169A RID: 5786
		private static AssertFilter Filter = new DefaultFilter();
	}
}
