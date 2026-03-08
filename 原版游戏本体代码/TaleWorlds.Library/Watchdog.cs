using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TaleWorlds.Library
{
	// Token: 0x020000A9 RID: 169
	public class Watchdog
	{
		// Token: 0x06000664 RID: 1636 RVA: 0x000164A4 File Offset: 0x000146A4
		public Watchdog(bool use_coreclr, string dumpdir)
		{
			if (Debugger.IsAttached)
			{
				return;
			}
			if (!File.Exists("Watchdog\\Watchdog.exe"))
			{
				return;
			}
			int id = Process.GetCurrentProcess().Id;
			string str = Path.Combine(dumpdir, "crashes");
			string str2 = Path.Combine(dumpdir, "logs");
			string str3 = "..\\..\\..\\WOTS\\Modules\\Test";
			string text = "";
			text = text + " -p " + id;
			text = text + " -dd \"" + str + "\"";
			text = text + " -dl \"" + str2 + "\"";
			text = text + " -dir-cdb-sos \"" + str3 + "\"";
			text = text + " --sync-event-name \"" + this.WatchdogMutexName + "\"";
			text += " -pu ";
			if (use_coreclr)
			{
				text += " --net6-plus ";
			}
			using (EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, this.WatchdogMutexName))
			{
				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = "Watchdog\\Watchdog.exe",
					Arguments = text,
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				new Process
				{
					StartInfo = startInfo
				}.Start();
				eventWaitHandle.WaitOne(15000);
			}
		}

		// Token: 0x06000665 RID: 1637 RVA: 0x0001660C File Offset: 0x0001480C
		public static void SetDumpDirectory(string Path)
		{
			string message = "#TW#-dd" + Path;
			Debugger.Log(0, null, message);
		}

		// Token: 0x06000666 RID: 1638 RVA: 0x00016630 File Offset: 0x00014830
		public static void DetachAndClose()
		{
			string message = "#TW#-dt1";
			Debugger.Log(0, null, message);
		}

		// Token: 0x06000667 RID: 1639 RVA: 0x0001664C File Offset: 0x0001484C
		public static void LogProperty(string FileName, string GroupName, string Key, string Value)
		{
			string text = "";
			text = text + "#TW#" + FileName;
			text = text + "#TW#" + GroupName;
			text = text + "#TW#" + Key;
			text = text + "#TW#" + Value;
			Debugger.Log(0, null, text);
		}

		// Token: 0x06000668 RID: 1640 RVA: 0x0001669B File Offset: 0x0001489B
		public static bool Attached()
		{
			return Debugger.IsAttached;
		}

		// Token: 0x040001E4 RID: 484
		public string WatchdogMutexName = "Global\\8DBAEBA5-40DB-4E8A-A997-E440DE7D7717";
	}
}
