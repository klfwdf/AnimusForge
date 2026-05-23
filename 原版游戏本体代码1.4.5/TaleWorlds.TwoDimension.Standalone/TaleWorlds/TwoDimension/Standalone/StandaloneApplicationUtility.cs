using System;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension.Standalone;

internal static class StandaloneApplicationUtility
{
	public static void TerminateWithMessageBox(string title, string message)
	{
		Debug.ShowMessageBox(message, title, 1u);
		Environment.Exit(0);
	}
}
