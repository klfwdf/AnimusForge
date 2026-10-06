using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public class ApplicationHealthChecker
{
	private Thread _thread;

	private bool _isRunning;

	private Stopwatch _stopwatch;

	private Thread _mainThread;

	public void Start()
	{
		TaleWorlds.Library.Debug.Print("Starting ApplicationHealthChecker");
		try
		{
			File.WriteAllText(BasePath.Name + "Application.HealthCheckerStarted", "...");
		}
		catch (Exception ex)
		{
			Print("Blocked main thread file create e: " + ex);
		}
		_isRunning = true;
		_stopwatch = new Stopwatch();
		_stopwatch.Start();
		_thread = new Thread(ThreadUpdate);
		_thread.IsBackground = true;
		_thread.Start();
	}

	public void Stop()
	{
		_thread = null;
		_stopwatch = null;
		_isRunning = false;
	}

	public void Update()
	{
		if (_isRunning)
		{
			_stopwatch.Restart();
			_mainThread = Thread.CurrentThread;
		}
	}

	private static void Print(string log)
	{
		TaleWorlds.Library.Debug.Print(log);
		Console.WriteLine(log);
	}

	private void ThreadUpdate()
	{
		while (_isRunning)
		{
			long num = _stopwatch.ElapsedMilliseconds / 1000;
			if (num > 180)
			{
				Print("Main thread is blocked for " + num + " seconds");
				try
				{
					File.WriteAllText(BasePath.Name + "Application.Blocked", num.ToString());
				}
				catch (Exception ex)
				{
					Print("Blocked main thread file create e: " + ex);
				}
				try
				{
					Print("Blocked main thread IsAlive: " + _mainThread.IsAlive);
					Print("Blocked main thread ThreadState: " + _mainThread.ThreadState);
				}
				catch (Exception ex2)
				{
					Print("Blocked main thread e: " + ex2);
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
				catch (Exception ex3)
				{
					Print("Blocked main thread file delete e: " + ex3);
				}
			}
			Thread.Sleep(10000);
		}
	}
}
