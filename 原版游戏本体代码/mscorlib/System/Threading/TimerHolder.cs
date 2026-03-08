using System;
using System.Runtime.CompilerServices;
using System.Threading.NetCore;

namespace System.Threading
{
	// Token: 0x02000530 RID: 1328
	internal sealed class TimerHolder
	{
		// Token: 0x06003E4D RID: 15949 RVA: 0x000E85DD File Offset: 0x000E67DD
		public TimerHolder(object timer)
		{
			this.m_timer = timer;
		}

		// Token: 0x06003E4E RID: 15950 RVA: 0x000E85EC File Offset: 0x000E67EC
		~TimerHolder()
		{
			if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
			{
				if (Timer.UseNetCoreTimer)
				{
					this.NetCoreTimer.Close();
				}
				else
				{
					this.NetFxTimer.Close();
				}
			}
		}

		// Token: 0x1700093B RID: 2363
		// (get) Token: 0x06003E4F RID: 15951 RVA: 0x000E8648 File Offset: 0x000E6848
		private TimerQueueTimer NetFxTimer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (TimerQueueTimer)this.m_timer;
			}
		}

		// Token: 0x1700093C RID: 2364
		// (get) Token: 0x06003E50 RID: 15952 RVA: 0x000E8655 File Offset: 0x000E6855
		private TimerQueueTimer NetCoreTimer
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (TimerQueueTimer)this.m_timer;
			}
		}

		// Token: 0x06003E51 RID: 15953 RVA: 0x000E8662 File Offset: 0x000E6862
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Change(uint dueTime, uint period)
		{
			if (!Timer.UseNetCoreTimer)
			{
				return this.NetFxTimer.Change(dueTime, period);
			}
			return this.NetCoreTimer.Change(dueTime, period);
		}

		// Token: 0x06003E52 RID: 15954 RVA: 0x000E8686 File Offset: 0x000E6886
		public void Close()
		{
			if (Timer.UseNetCoreTimer)
			{
				this.NetCoreTimer.Close();
			}
			else
			{
				this.NetFxTimer.Close();
			}
			GC.SuppressFinalize(this);
		}

		// Token: 0x06003E53 RID: 15955 RVA: 0x000E86B0 File Offset: 0x000E68B0
		public bool Close(WaitHandle notifyObject)
		{
			bool result = (Timer.UseNetCoreTimer ? this.NetCoreTimer.Close(notifyObject) : this.NetFxTimer.Close(notifyObject));
			GC.SuppressFinalize(this);
			return result;
		}

		// Token: 0x04001A46 RID: 6726
		private object m_timer;
	}
}
