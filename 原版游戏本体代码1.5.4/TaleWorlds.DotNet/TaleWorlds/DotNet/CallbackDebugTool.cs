using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TaleWorlds.DotNet;

public static class CallbackDebugTool
{
	private class CallbackLog
	{
		public long CallCount;

		public string FuncName;
	}

	private static Dictionary<string, CallbackLog> Logs = new Dictionary<string, CallbackLog>();

	private static ulong FrameCount = 0uL;

	[Conditional("DEBUG_MORE")]
	public static void AddLog([CallerMemberName] string memberName = "")
	{
		lock (Logs)
		{
			if (Logs.TryGetValue(memberName, out var value))
			{
				value.CallCount++;
				return;
			}
			Logs.Add(memberName, new CallbackLog
			{
				CallCount = 1L,
				FuncName = memberName
			});
		}
	}

	[Conditional("DEBUG_MORE")]
	public static void FrameEnd()
	{
		FrameCount++;
	}

	[Conditional("DEBUG_MORE")]
	public static void Reset()
	{
		Logs = new Dictionary<string, CallbackLog>();
		FrameCount = 0uL;
	}

	public static string ShowResults()
	{
		List<CallbackLog> list = Logs.Values.ToList();
		list.Sort((CallbackLog x, CallbackLog y) => (int)(y.CallCount - x.CallCount));
		string text = "";
		double num = 1.0 / (double)FrameCount;
		foreach (CallbackLog item in list)
		{
			double num2 = (double)item.CallCount * num;
			text = text + item.FuncName + ": " + item.CallCount + ", " + num2 + Environment.NewLine;
		}
		return text;
	}
}
