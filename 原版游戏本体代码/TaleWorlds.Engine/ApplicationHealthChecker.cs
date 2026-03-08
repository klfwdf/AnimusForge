using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000005 RID: 5
	public class ApplicationHealthChecker
	{
		// Token: 0x06000007 RID: 7 RVA: 0x000020A8 File Offset: 0x000002A8
		public void Start()
		{
			Debug.Print("Starting ApplicationHealthChecker", 0, Debug.DebugColor.White, 17592186044416UL);
			try
			{
				File.WriteAllText(BasePath.Name + "Application.HealthCheckerStarted", "...");
			}
			catch (Exception arg)
			{
				ApplicationHealthChecker.Print("Blocked main thread file create e: " + arg);
			}
			this._isRunning = true;
			this._stopwatch = new Stopwatch();
			this._stopwatch.Start();
			this._thread = new Thread(new ThreadStart(this.ThreadUpdate));
			this._thread.IsBackground = true;
			this._thread.Start();
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002154 File Offset: 0x00000354
		public void Stop()
		{
			this._thread = null;
			this._stopwatch = null;
			this._isRunning = false;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000216B File Offset: 0x0000036B
		public void Update()
		{
			if (this._isRunning)
			{
				this._stopwatch.Restart();
				this._mainThread = Thread.CurrentThread;
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000218B File Offset: 0x0000038B
		private static void Print(string log)
		{
			Debug.Print(log, 0, Debug.DebugColor.White, 17592186044416UL);
			Console.WriteLine(log);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000021A8 File Offset: 0x000003A8
		private void ThreadUpdate()
		{
			while (this._isRunning)
			{
				long num = this._stopwatch.ElapsedMilliseconds / 1000L;
				if (num > 180L)
				{
					ApplicationHealthChecker.Print("Main thread is blocked for " + num + " seconds");
					try
					{
						File.WriteAllText(BasePath.Name + "Application.Blocked", num.ToString());
					}
					catch (Exception arg)
					{
						ApplicationHealthChecker.Print("Blocked main thread file create e: " + arg);
					}
					try
					{
						ApplicationHealthChecker.Print("Blocked main thread IsAlive: " + this._mainThread.IsAlive.ToString());
						ApplicationHealthChecker.Print("Blocked main thread ThreadState: " + this._mainThread.ThreadState);
					}
					catch (Exception arg2)
					{
						ApplicationHealthChecker.Print("Blocked main thread e: " + arg2);
					}
					Utilities.ExitProcess(1453);
				}
				else
				{
					try
					{
						if (File.Exists(BasePath.Name + "Application.Blocked"))
						{
							File.Delete(BasePath.Name + "Application.Blocked");
						}
					}
					catch (Exception arg3)
					{
						ApplicationHealthChecker.Print("Blocked main thread file delete e: " + arg3);
					}
				}
				Thread.Sleep(10000);
			}
		}

		// Token: 0x04000002 RID: 2
		private Thread _thread;

		// Token: 0x04000003 RID: 3
		private bool _isRunning;

		// Token: 0x04000004 RID: 4
		private Stopwatch _stopwatch;

		// Token: 0x04000005 RID: 5
		private Thread _mainThread;
	}
}
