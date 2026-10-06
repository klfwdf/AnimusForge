using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class WindowsFramework
{
	public bool IsActive;

	private FrameworkDomain[] _frameworkDomains;

	private Thread[] _frameworkDomainThreads;

	private Stopwatch _timer;

	private List<IMessageCommunicator> _messageCommunicators;

	public bool IsFinalized;

	private int _abortedThreadCount;

	public WindowsFrameworkThreadConfig ThreadConfig { get; set; }

	public long ElapsedTicks => _timer.ElapsedTicks;

	public long TicksPerSecond => Stopwatch.Frequency;

	public WindowsFramework()
	{
		_timer = new Stopwatch();
		_messageCommunicators = new List<IMessageCommunicator>();
		IsActive = false;
	}

	public void Initialize(FrameworkDomain[] frameworkDomains)
	{
		_frameworkDomains = frameworkDomains;
		IsActive = true;
		if (ThreadConfig == WindowsFrameworkThreadConfig.SingleThread)
		{
			_frameworkDomainThreads = new Thread[1];
			CreateThread(0);
		}
		else if (ThreadConfig == WindowsFrameworkThreadConfig.MultiThread)
		{
			_frameworkDomainThreads = new Thread[frameworkDomains.Length];
			for (int i = 0; i < frameworkDomains.Length; i++)
			{
				CreateThread(i);
			}
		}
	}

	private void CreateThread(int index)
	{
		Common.SetInvariantCulture();
		_frameworkDomainThreads[index] = new Thread(MainLoop);
		_frameworkDomainThreads[index].SetApartmentState(ApartmentState.STA);
		_frameworkDomainThreads[index].Name = _frameworkDomains[index].ToString() + " Thread";
		_frameworkDomainThreads[index].CurrentCulture = CultureInfo.InvariantCulture;
		_frameworkDomainThreads[index].CurrentUICulture = CultureInfo.InvariantCulture;
	}

	public void RegisterMessageCommunicator(IMessageCommunicator communicator)
	{
		_messageCommunicators.Add(communicator);
	}

	public void UnRegisterMessageCommunicator(IMessageCommunicator communicator)
	{
		_messageCommunicators.Remove(communicator);
	}

	private void MessageLoop()
	{
		try
		{
			if (ThreadConfig == WindowsFrameworkThreadConfig.NoThread)
			{
				int num = 0;
				while (_frameworkDomains != null && num < _frameworkDomains.Length)
				{
					_frameworkDomains[num].Update();
					num++;
				}
			}
			for (int i = 0; i < _messageCommunicators.Count; i++)
			{
				_messageCommunicators[i].MessageLoop();
			}
		}
		catch (Exception ex)
		{
			TaleWorlds.Library.Debug.Print(ex.Message);
			TaleWorlds.Library.Debug.Print(ex.StackTrace);
			throw;
		}
	}

	private void MainLoop(object parameter)
	{
		try
		{
			if (ThreadConfig == WindowsFrameworkThreadConfig.SingleThread)
			{
				while (IsActive)
				{
					for (int i = 0; i < _frameworkDomains.Length; i++)
					{
						_frameworkDomains[i].Update();
					}
				}
			}
			else if (ThreadConfig == WindowsFrameworkThreadConfig.MultiThread)
			{
				FrameworkDomain frameworkDomain = parameter as FrameworkDomain;
				while (IsActive)
				{
					frameworkDomain.Update();
				}
			}
			Interlocked.Increment(ref _abortedThreadCount);
			OnFinalize();
		}
		catch (Exception ex)
		{
			TaleWorlds.Library.Debug.Print(ex.Message);
			TaleWorlds.Library.Debug.Print(ex.StackTrace);
			throw;
		}
	}

	public void Stop()
	{
		IsActive = false;
		OnFinalize();
	}

	public void OnFinalize()
	{
		if (_frameworkDomainThreads == null || _abortedThreadCount == _frameworkDomainThreads.Length)
		{
			_frameworkDomainThreads = null;
			FrameworkDomain[] frameworkDomains = _frameworkDomains;
			for (int i = 0; i < frameworkDomains.Length; i++)
			{
				frameworkDomains[i].Destroy();
			}
			_frameworkDomains = null;
			IsFinalized = true;
		}
	}

	public void Start()
	{
		_timer.Start();
		IsActive = true;
		if (ThreadConfig == WindowsFrameworkThreadConfig.SingleThread)
		{
			_frameworkDomainThreads[0].Start();
		}
		else if (ThreadConfig == WindowsFrameworkThreadConfig.MultiThread)
		{
			for (int i = 0; i < _frameworkDomains.Length; i++)
			{
				_frameworkDomainThreads[i].Start(_frameworkDomains[i]);
			}
		}
		NativeMessage lpMsg = default(NativeMessage);
		if (ThreadConfig == WindowsFrameworkThreadConfig.NoThread)
		{
			while (IsActive)
			{
				if (User32.PeekMessage(out lpMsg, IntPtr.Zero, 0u, 0u, 1u))
				{
					User32.TranslateMessage(ref lpMsg);
					User32.DispatchMessage(ref lpMsg);
				}
				MessageLoop();
			}
			return;
		}
		while (IsActive)
		{
			if (User32.PeekMessage(out lpMsg, IntPtr.Zero, 0u, 0u, 1u))
			{
				if (lpMsg.msg == WindowMessage.Quit)
				{
					break;
				}
				User32.TranslateMessage(ref lpMsg);
				User32.DispatchMessage(ref lpMsg);
			}
			MessageLoop();
		}
	}
}
