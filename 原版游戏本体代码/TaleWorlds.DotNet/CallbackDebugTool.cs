using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000004 RID: 4
	public static class CallbackDebugTool
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002060 File Offset: 0x00000260
		[Conditional("DEBUG_MORE")]
		public static void AddLog([CallerMemberName] string memberName = "")
		{
			Dictionary<string, CallbackDebugTool.CallbackLog> logs = CallbackDebugTool.Logs;
			lock (logs)
			{
				CallbackDebugTool.CallbackLog callbackLog;
				if (CallbackDebugTool.Logs.TryGetValue(memberName, out callbackLog))
				{
					callbackLog.CallCount += 1L;
				}
				else
				{
					CallbackDebugTool.Logs.Add(memberName, new CallbackDebugTool.CallbackLog
					{
						CallCount = 1L,
						FuncName = memberName
					});
				}
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020D8 File Offset: 0x000002D8
		[Conditional("DEBUG_MORE")]
		public static void FrameEnd()
		{
			CallbackDebugTool.FrameCount += 1UL;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020E7 File Offset: 0x000002E7
		[Conditional("DEBUG_MORE")]
		public static void Reset()
		{
			CallbackDebugTool.Logs = new Dictionary<string, CallbackDebugTool.CallbackLog>();
			CallbackDebugTool.FrameCount = 0UL;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000020FC File Offset: 0x000002FC
		public static string ShowResults()
		{
			List<CallbackDebugTool.CallbackLog> list = CallbackDebugTool.Logs.Values.ToList<CallbackDebugTool.CallbackLog>();
			list.Sort((CallbackDebugTool.CallbackLog x, CallbackDebugTool.CallbackLog y) => (int)(y.CallCount - x.CallCount));
			string text = "";
			double num = 1.0 / CallbackDebugTool.FrameCount;
			foreach (CallbackDebugTool.CallbackLog callbackLog in list)
			{
				double num2 = (double)callbackLog.CallCount * num;
				text = string.Concat(new object[]
				{
					text,
					callbackLog.FuncName,
					": ",
					callbackLog.CallCount,
					", ",
					num2,
					Environment.NewLine
				});
			}
			return text;
		}

		// Token: 0x04000002 RID: 2
		private static Dictionary<string, CallbackDebugTool.CallbackLog> Logs = new Dictionary<string, CallbackDebugTool.CallbackLog>();

		// Token: 0x04000003 RID: 3
		private static ulong FrameCount = 0UL;

		// Token: 0x02000034 RID: 52
		private class CallbackLog
		{
			// Token: 0x04000081 RID: 129
			public long CallCount;

			// Token: 0x04000082 RID: 130
			public string FuncName;
		}
	}
}
