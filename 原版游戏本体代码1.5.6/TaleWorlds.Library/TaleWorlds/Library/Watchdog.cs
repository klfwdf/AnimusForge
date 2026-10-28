using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TaleWorlds.Library;

public class Watchdog
{
	public string WatchdogMutexName = "Global\\8DBAEBA5-40DB-4E8A-A997-E440DE7D7717";

	public Watchdog(bool use_coreclr, string dumpdir)
	{
		if (Debugger.IsAttached || !File.Exists("Watchdog\\Watchdog.exe"))
		{
			return;
		}
		int id = Process.GetCurrentProcess().Id;
		string text = Path.Combine(dumpdir, "crashes");
		string text2 = Path.Combine(dumpdir, "logs");
		string text3 = "..\\..\\..\\WOTS\\Modules\\Test";
		string text4 = "";
		text4 = text4 + " -p " + id;
		text4 = text4 + " -dd \"" + text + "\"";
		text4 = text4 + " -dl \"" + text2 + "\"";
		text4 = text4 + " -dir-cdb-sos \"" + text3 + "\"";
		text4 = text4 + " --sync-event-name \"" + WatchdogMutexName + "\"";
		text4 += " -pu ";
		if (use_coreclr)
		{
			text4 += " --net6-plus ";
		}
		using EventWaitHandle eventWaitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset, WatchdogMutexName);
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "Watchdog\\Watchdog.exe",
			Arguments = text4,
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		Process process = new Process();
		process.StartInfo = startInfo;
		process.Start();
		eventWaitHandle.WaitOne(15000);
	}

	public static void SetDumpDirectory(string Path)
	{
		string message = "#TW#-dd" + Path;
		Debugger.Log(0, null, message);
	}

	public static void DetachAndClose()
	{
		string message = "#TW#-dt1";
		Debugger.Log(0, null, message);
	}

	public static void LogProperty(string FileName, string GroupName, string Key, string Value)
	{
		string text = "";
		text = text + "#TW#" + FileName;
		text = text + "#TW#" + GroupName;
		text = text + "#TW#" + Key;
		text = text + "#TW#" + Value;
		Debugger.Log(0, null, text);
	}

	public static bool Attached()
	{
		return Debugger.IsAttached;
	}
}
