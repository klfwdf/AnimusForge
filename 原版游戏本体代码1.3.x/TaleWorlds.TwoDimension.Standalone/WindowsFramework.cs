using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x02000012 RID: 18
	public class WindowsFramework
	{
		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000EA RID: 234 RVA: 0x0000513A File Offset: 0x0000333A
		// (set) Token: 0x060000EB RID: 235 RVA: 0x00005142 File Offset: 0x00003342
		public WindowsFrameworkThreadConfig ThreadConfig { get; set; }

		// Token: 0x060000EC RID: 236 RVA: 0x0000514B File Offset: 0x0000334B
		public WindowsFramework()
		{
			this._timer = new Stopwatch();
			this._messageCommunicators = new List<IMessageCommunicator>();
			this.IsActive = false;
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00005170 File Offset: 0x00003370
		public void Initialize(FrameworkDomain[] frameworkDomains)
		{
			this._frameworkDomains = frameworkDomains;
			this.IsActive = true;
			if (this.ThreadConfig == WindowsFrameworkThreadConfig.SingleThread)
			{
				this._frameworkDomainThreads = new Thread[1];
				this.CreateThread(0);
				return;
			}
			if (this.ThreadConfig == WindowsFrameworkThreadConfig.MultiThread)
			{
				this._frameworkDomainThreads = new Thread[frameworkDomains.Length];
				for (int i = 0; i < frameworkDomains.Length; i++)
				{
					this.CreateThread(i);
				}
			}
		}

		// Token: 0x060000EE RID: 238 RVA: 0x000051D4 File Offset: 0x000033D4
		private void CreateThread(int index)
		{
			Common.SetInvariantCulture();
			this._frameworkDomainThreads[index] = new Thread(new ParameterizedThreadStart(this.MainLoop));
			this._frameworkDomainThreads[index].SetApartmentState(ApartmentState.STA);
			this._frameworkDomainThreads[index].Name = this._frameworkDomains[index].ToString() + " Thread";
			this._frameworkDomainThreads[index].CurrentCulture = CultureInfo.InvariantCulture;
			this._frameworkDomainThreads[index].CurrentUICulture = CultureInfo.InvariantCulture;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00005255 File Offset: 0x00003455
		public void RegisterMessageCommunicator(IMessageCommunicator communicator)
		{
			this._messageCommunicators.Add(communicator);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00005263 File Offset: 0x00003463
		public void UnRegisterMessageCommunicator(IMessageCommunicator communicator)
		{
			this._messageCommunicators.Remove(communicator);
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00005274 File Offset: 0x00003474
		private void MessageLoop()
		{
			try
			{
				if (this.ThreadConfig == WindowsFrameworkThreadConfig.NoThread)
				{
					int num = 0;
					while (this._frameworkDomains != null && num < this._frameworkDomains.Length)
					{
						this._frameworkDomains[num].Update();
						num++;
					}
				}
				for (int i = 0; i < this._messageCommunicators.Count; i++)
				{
					this._messageCommunicators[i].MessageLoop();
				}
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				Debug.Print(ex.StackTrace, 0, Debug.DebugColor.White, 17592186044416UL);
				throw;
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000531C File Offset: 0x0000351C
		private void MainLoop(object parameter)
		{
			try
			{
				if (this.ThreadConfig == WindowsFrameworkThreadConfig.SingleThread)
				{
					while (this.IsActive)
					{
						for (int i = 0; i < this._frameworkDomains.Length; i++)
						{
							this._frameworkDomains[i].Update();
						}
					}
				}
				else if (this.ThreadConfig == WindowsFrameworkThreadConfig.MultiThread)
				{
					FrameworkDomain frameworkDomain = parameter as FrameworkDomain;
					while (this.IsActive)
					{
						frameworkDomain.Update();
					}
				}
				Interlocked.Increment(ref this._abortedThreadCount);
				this.OnFinalize();
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				Debug.Print(ex.StackTrace, 0, Debug.DebugColor.White, 17592186044416UL);
				throw;
			}
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000053D4 File Offset: 0x000035D4
		public void Stop()
		{
			this.IsActive = false;
			this.OnFinalize();
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x000053E4 File Offset: 0x000035E4
		public void OnFinalize()
		{
			if (this._frameworkDomainThreads != null && this._abortedThreadCount != this._frameworkDomainThreads.Length)
			{
				return;
			}
			this._frameworkDomainThreads = null;
			FrameworkDomain[] frameworkDomains = this._frameworkDomains;
			for (int i = 0; i < frameworkDomains.Length; i++)
			{
				frameworkDomains[i].Destroy();
			}
			this._frameworkDomains = null;
			this.IsFinalized = true;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000543C File Offset: 0x0000363C
		public void Start()
		{
			this._timer.Start();
			this.IsActive = true;
			if (this.ThreadConfig == WindowsFrameworkThreadConfig.SingleThread)
			{
				this._frameworkDomainThreads[0].Start();
			}
			else if (this.ThreadConfig == WindowsFrameworkThreadConfig.MultiThread)
			{
				for (int i = 0; i < this._frameworkDomains.Length; i++)
				{
					this._frameworkDomainThreads[i].Start(this._frameworkDomains[i]);
				}
			}
			NativeMessage nativeMessage = default(NativeMessage);
			if (this.ThreadConfig == WindowsFrameworkThreadConfig.NoThread)
			{
				while (this.IsActive)
				{
					if (User32.PeekMessage(out nativeMessage, IntPtr.Zero, 0U, 0U, 1U))
					{
						User32.TranslateMessage(ref nativeMessage);
						User32.DispatchMessage(ref nativeMessage);
					}
					this.MessageLoop();
				}
				return;
			}
			while (this.IsActive)
			{
				if (User32.PeekMessage(out nativeMessage, IntPtr.Zero, 0U, 0U, 1U))
				{
					if (nativeMessage.msg == WindowMessage.Quit)
					{
						break;
					}
					User32.TranslateMessage(ref nativeMessage);
					User32.DispatchMessage(ref nativeMessage);
				}
				this.MessageLoop();
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x0000551F File Offset: 0x0000371F
		public long ElapsedTicks
		{
			get
			{
				return this._timer.ElapsedTicks;
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x0000552C File Offset: 0x0000372C
		public long TicksPerSecond
		{
			get
			{
				return Stopwatch.Frequency;
			}
		}

		// Token: 0x04000054 RID: 84
		public bool IsActive;

		// Token: 0x04000055 RID: 85
		private FrameworkDomain[] _frameworkDomains;

		// Token: 0x04000056 RID: 86
		private Thread[] _frameworkDomainThreads;

		// Token: 0x04000057 RID: 87
		private Stopwatch _timer;

		// Token: 0x04000059 RID: 89
		private List<IMessageCommunicator> _messageCommunicators;

		// Token: 0x0400005A RID: 90
		public bool IsFinalized;

		// Token: 0x0400005B RID: 91
		private int _abortedThreadCount;
	}
}
