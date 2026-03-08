using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000575 RID: 1397
	internal class TaskExceptionHolder
	{
		// Token: 0x06004197 RID: 16791 RVA: 0x000F4DF8 File Offset: 0x000F2FF8
		internal TaskExceptionHolder(Task task)
		{
			this.m_task = task;
			TaskExceptionHolder.EnsureADUnloadCallbackRegistered();
		}

		// Token: 0x06004198 RID: 16792 RVA: 0x000F4E0C File Offset: 0x000F300C
		[SecuritySafeCritical]
		private static bool ShouldFailFastOnUnobservedException()
		{
			return CLRConfig.CheckThrowUnobservedTaskExceptions();
		}

		// Token: 0x06004199 RID: 16793 RVA: 0x000F4E22 File Offset: 0x000F3022
		private static void EnsureADUnloadCallbackRegistered()
		{
			if (TaskExceptionHolder.s_adUnloadEventHandler == null && Interlocked.CompareExchange<EventHandler>(ref TaskExceptionHolder.s_adUnloadEventHandler, new EventHandler(TaskExceptionHolder.AppDomainUnloadCallback), null) == null)
			{
				AppDomain.CurrentDomain.DomainUnload += TaskExceptionHolder.s_adUnloadEventHandler;
			}
		}

		// Token: 0x0600419A RID: 16794 RVA: 0x000F4E57 File Offset: 0x000F3057
		private static void AppDomainUnloadCallback(object sender, EventArgs e)
		{
			TaskExceptionHolder.s_domainUnloadStarted = true;
		}

		// Token: 0x0600419B RID: 16795 RVA: 0x000F4E64 File Offset: 0x000F3064
		protected override void Finalize()
		{
			try
			{
				if (this.m_faultExceptions != null && !this.m_isHandled && !Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload() && !TaskExceptionHolder.s_domainUnloadStarted)
				{
					foreach (ExceptionDispatchInfo exceptionDispatchInfo in this.m_faultExceptions)
					{
						Exception sourceException = exceptionDispatchInfo.SourceException;
						AggregateException ex = sourceException as AggregateException;
						if (ex != null)
						{
							AggregateException ex2 = ex.Flatten();
							using (IEnumerator<Exception> enumerator2 = ex2.InnerExceptions.GetEnumerator())
							{
								while (enumerator2.MoveNext())
								{
									Exception ex3 = enumerator2.Current;
									if (ex3 is ThreadAbortException)
									{
										return;
									}
								}
								continue;
							}
						}
						if (sourceException is ThreadAbortException)
						{
							return;
						}
					}
					AggregateException ex4 = new AggregateException(Environment.GetResourceString("TaskExceptionHolder_UnhandledException"), this.m_faultExceptions);
					UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs = new UnobservedTaskExceptionEventArgs(ex4);
					TaskScheduler.PublishUnobservedTaskException(this.m_task, unobservedTaskExceptionEventArgs);
					if (TaskExceptionHolder.s_failFastOnUnobservedException && !unobservedTaskExceptionEventArgs.m_observed)
					{
						throw ex4;
					}
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x170009C3 RID: 2499
		// (get) Token: 0x0600419C RID: 16796 RVA: 0x000F4FDC File Offset: 0x000F31DC
		internal bool ContainsFaultList
		{
			get
			{
				return this.m_faultExceptions != null;
			}
		}

		// Token: 0x0600419D RID: 16797 RVA: 0x000F4FE9 File Offset: 0x000F31E9
		internal void Add(object exceptionObject)
		{
			this.Add(exceptionObject, false);
		}

		// Token: 0x0600419E RID: 16798 RVA: 0x000F4FF3 File Offset: 0x000F31F3
		internal void Add(object exceptionObject, bool representsCancellation)
		{
			if (representsCancellation)
			{
				this.SetCancellationException(exceptionObject);
				return;
			}
			this.AddFaultException(exceptionObject);
		}

		// Token: 0x0600419F RID: 16799 RVA: 0x000F5008 File Offset: 0x000F3208
		private void SetCancellationException(object exceptionObject)
		{
			OperationCanceledException ex = exceptionObject as OperationCanceledException;
			if (ex != null)
			{
				this.m_cancellationException = ExceptionDispatchInfo.Capture(ex);
			}
			else
			{
				ExceptionDispatchInfo cancellationException = exceptionObject as ExceptionDispatchInfo;
				this.m_cancellationException = cancellationException;
			}
			this.MarkAsHandled(false);
		}

		// Token: 0x060041A0 RID: 16800 RVA: 0x000F5044 File Offset: 0x000F3244
		private void AddFaultException(object exceptionObject)
		{
			List<ExceptionDispatchInfo> list = this.m_faultExceptions;
			if (list == null)
			{
				list = (this.m_faultExceptions = new List<ExceptionDispatchInfo>(1));
			}
			Exception ex = exceptionObject as Exception;
			if (ex != null)
			{
				list.Add(ExceptionDispatchInfo.Capture(ex));
			}
			else
			{
				ExceptionDispatchInfo exceptionDispatchInfo = exceptionObject as ExceptionDispatchInfo;
				if (exceptionDispatchInfo != null)
				{
					list.Add(exceptionDispatchInfo);
				}
				else
				{
					IEnumerable<Exception> enumerable = exceptionObject as IEnumerable<Exception>;
					if (enumerable != null)
					{
						using (IEnumerator<Exception> enumerator = enumerable.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								Exception source = enumerator.Current;
								list.Add(ExceptionDispatchInfo.Capture(source));
							}
							goto IL_B3;
						}
					}
					IEnumerable<ExceptionDispatchInfo> enumerable2 = exceptionObject as IEnumerable<ExceptionDispatchInfo>;
					if (enumerable2 == null)
					{
						throw new ArgumentException(Environment.GetResourceString("TaskExceptionHolder_UnknownExceptionType"), "exceptionObject");
					}
					list.AddRange(enumerable2);
				}
			}
			IL_B3:
			for (int i = 0; i < list.Count; i++)
			{
				Type type = list[i].SourceException.GetType();
				if (type != typeof(ThreadAbortException) && type != typeof(AppDomainUnloadedException))
				{
					this.MarkAsUnhandled();
					return;
				}
				if (i == list.Count - 1)
				{
					this.MarkAsHandled(false);
				}
			}
		}

		// Token: 0x060041A1 RID: 16801 RVA: 0x000F5180 File Offset: 0x000F3380
		private void MarkAsUnhandled()
		{
			if (this.m_isHandled)
			{
				GC.ReRegisterForFinalize(this);
				this.m_isHandled = false;
			}
		}

		// Token: 0x060041A2 RID: 16802 RVA: 0x000F519B File Offset: 0x000F339B
		internal void MarkAsHandled(bool calledFromFinalizer)
		{
			if (!this.m_isHandled)
			{
				if (!calledFromFinalizer)
				{
					GC.SuppressFinalize(this);
				}
				this.m_isHandled = true;
			}
		}

		// Token: 0x060041A3 RID: 16803 RVA: 0x000F51BC File Offset: 0x000F33BC
		internal AggregateException CreateExceptionObject(bool calledFromFinalizer, Exception includeThisException)
		{
			List<ExceptionDispatchInfo> faultExceptions = this.m_faultExceptions;
			this.MarkAsHandled(calledFromFinalizer);
			if (includeThisException == null)
			{
				return new AggregateException(faultExceptions);
			}
			Exception[] array = new Exception[faultExceptions.Count + 1];
			for (int i = 0; i < array.Length - 1; i++)
			{
				array[i] = faultExceptions[i].SourceException;
			}
			array[array.Length - 1] = includeThisException;
			return new AggregateException(array);
		}

		// Token: 0x060041A4 RID: 16804 RVA: 0x000F5220 File Offset: 0x000F3420
		internal ReadOnlyCollection<ExceptionDispatchInfo> GetExceptionDispatchInfos()
		{
			List<ExceptionDispatchInfo> faultExceptions = this.m_faultExceptions;
			this.MarkAsHandled(false);
			return new ReadOnlyCollection<ExceptionDispatchInfo>(faultExceptions);
		}

		// Token: 0x060041A5 RID: 16805 RVA: 0x000F5244 File Offset: 0x000F3444
		internal ExceptionDispatchInfo GetCancellationExceptionDispatchInfo()
		{
			return this.m_cancellationException;
		}

		// Token: 0x04001B63 RID: 7011
		private static readonly bool s_failFastOnUnobservedException = TaskExceptionHolder.ShouldFailFastOnUnobservedException();

		// Token: 0x04001B64 RID: 7012
		private static volatile bool s_domainUnloadStarted;

		// Token: 0x04001B65 RID: 7013
		private static volatile EventHandler s_adUnloadEventHandler;

		// Token: 0x04001B66 RID: 7014
		private readonly Task m_task;

		// Token: 0x04001B67 RID: 7015
		private volatile List<ExceptionDispatchInfo> m_faultExceptions;

		// Token: 0x04001B68 RID: 7016
		private ExceptionDispatchInfo m_cancellationException;

		// Token: 0x04001B69 RID: 7017
		private volatile bool m_isHandled;
	}
}
