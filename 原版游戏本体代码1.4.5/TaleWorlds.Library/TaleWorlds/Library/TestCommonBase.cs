using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public abstract class TestCommonBase
{
	public int TestRandomSeed;

	public bool IsTestEnabled;

	public bool isParallelThread;

	public string SceneNameToOpenOnStartup;

	public object TestLock = new object();

	private static TestCommonBase _baseInstance;

	private DateTime timeoutTimerStart = DateTime.Now;

	private bool timeoutTimerEnabled = true;

	private int commonWaitTimeoutLimits = 1140;

	public static TestCommonBase BaseInstance => _baseInstance;

	public abstract void Tick();

	public void StartTimeoutTimer()
	{
		timeoutTimerStart = DateTime.Now;
	}

	public void ToggleTimeoutTimer()
	{
		timeoutTimerEnabled = !timeoutTimerEnabled;
	}

	public bool CheckTimeoutTimer()
	{
		if (!timeoutTimerEnabled)
		{
			return false;
		}
		if (DateTime.Now.Subtract(timeoutTimerStart).TotalSeconds > (double)commonWaitTimeoutLimits)
		{
			return true;
		}
		return false;
	}

	protected TestCommonBase()
	{
		_baseInstance = this;
	}

	public virtual string GetGameStatus()
	{
		return "";
	}

	public void WaitFor(double seconds)
	{
		if (!isParallelThread)
		{
			DateTime now = DateTime.Now;
			while ((DateTime.Now - now).TotalSeconds < seconds)
			{
				Monitor.Pulse(TestLock);
				Monitor.Wait(TestLock);
			}
		}
	}

	public virtual async Task WaitUntil(Func<bool> func)
	{
		while (!func())
		{
			await WaitForAsync(0.1);
		}
	}

	public Task WaitForAsync(double seconds, Random random)
	{
		return Task.Delay((int)(seconds * 1000.0 * random.NextDouble()));
	}

	public Task WaitForAsync(double seconds)
	{
		return Task.Delay((int)(seconds * 1000.0));
	}

	public static string GetAttachmentsFolderPath()
	{
		return "..\\..\\..\\Tools\\TestAutomation\\Attachments\\";
	}

	public virtual void OnFinalize()
	{
		_baseInstance = null;
	}
}
