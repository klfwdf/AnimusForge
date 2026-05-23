using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using Microsoft.Win32;
using Windows.Foundation.Diagnostics;

namespace System.Threading.Tasks
{
	// Token: 0x02000580 RID: 1408
	[FriendAccessAllowed]
	internal static class AsyncCausalityTracer
	{
		// Token: 0x0600423E RID: 16958 RVA: 0x000F698B File Offset: 0x000F4B8B
		internal static void EnableToETW(bool enabled)
		{
			if (enabled)
			{
				AsyncCausalityTracer.f_LoggingOn |= AsyncCausalityTracer.Loggers.ETW;
				return;
			}
			AsyncCausalityTracer.f_LoggingOn &= ~AsyncCausalityTracer.Loggers.ETW;
		}

		// Token: 0x170009D4 RID: 2516
		// (get) Token: 0x0600423F RID: 16959 RVA: 0x000F69AD File Offset: 0x000F4BAD
		[FriendAccessAllowed]
		internal static bool LoggingOn
		{
			[FriendAccessAllowed]
			get
			{
				return AsyncCausalityTracer.f_LoggingOn > (AsyncCausalityTracer.Loggers)0;
			}
		}

		// Token: 0x06004240 RID: 16960 RVA: 0x000F69B8 File Offset: 0x000F4BB8
		[SecuritySafeCritical]
		static AsyncCausalityTracer()
		{
			if (!Environment.IsWinRTSupported)
			{
				return;
			}
			string activatableClassId = "Windows.Foundation.Diagnostics.AsyncCausalityTracer";
			Guid guid = new Guid(1350896422, 9854, 17691, 168, 144, 171, 106, 55, 2, 69, 238);
			object obj = null;
			try
			{
				int num = Microsoft.Win32.UnsafeNativeMethods.RoGetActivationFactory(activatableClassId, ref guid, out obj);
				if (num >= 0 && obj != null)
				{
					AsyncCausalityTracer.s_TracerFactory = (IAsyncCausalityTracerStatics)obj;
					EventRegistrationToken eventRegistrationToken = AsyncCausalityTracer.s_TracerFactory.add_TracingStatusChanged(new EventHandler<TracingStatusChangedEventArgs>(AsyncCausalityTracer.TracingStatusChangedHandler));
				}
			}
			catch (Exception ex)
			{
				AsyncCausalityTracer.LogAndDisable(ex);
			}
		}

		// Token: 0x06004241 RID: 16961 RVA: 0x000F6A8C File Offset: 0x000F4C8C
		[SecuritySafeCritical]
		private static void TracingStatusChangedHandler(object sender, TracingStatusChangedEventArgs args)
		{
			if (args.Enabled)
			{
				AsyncCausalityTracer.f_LoggingOn |= AsyncCausalityTracer.Loggers.CausalityTracer;
				return;
			}
			AsyncCausalityTracer.f_LoggingOn &= ~AsyncCausalityTracer.Loggers.CausalityTracer;
		}

		// Token: 0x06004242 RID: 16962 RVA: 0x000F6AB4 File Offset: 0x000F4CB4
		[FriendAccessAllowed]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceOperationCreation(CausalityTraceLevel traceLevel, int taskId, string operationName, ulong relatedContext)
		{
			try
			{
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.ETW) != (AsyncCausalityTracer.Loggers)0)
				{
					TplEtwProvider.Log.TraceOperationBegin(taskId, operationName, (long)relatedContext);
				}
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.CausalityTracer) != (AsyncCausalityTracer.Loggers)0)
				{
					AsyncCausalityTracer.s_TracerFactory.TraceOperationCreation((CausalityTraceLevel)traceLevel, CausalitySource.Library, AsyncCausalityTracer.s_PlatformId, AsyncCausalityTracer.GetOperationId((uint)taskId), operationName, relatedContext);
				}
			}
			catch (Exception ex)
			{
				AsyncCausalityTracer.LogAndDisable(ex);
			}
		}

		// Token: 0x06004243 RID: 16963 RVA: 0x000F6B14 File Offset: 0x000F4D14
		[FriendAccessAllowed]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceOperationCompletion(CausalityTraceLevel traceLevel, int taskId, AsyncCausalityStatus status)
		{
			try
			{
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.ETW) != (AsyncCausalityTracer.Loggers)0)
				{
					TplEtwProvider.Log.TraceOperationEnd(taskId, status);
				}
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.CausalityTracer) != (AsyncCausalityTracer.Loggers)0)
				{
					AsyncCausalityTracer.s_TracerFactory.TraceOperationCompletion((CausalityTraceLevel)traceLevel, CausalitySource.Library, AsyncCausalityTracer.s_PlatformId, AsyncCausalityTracer.GetOperationId((uint)taskId), (AsyncCausalityStatus)status);
				}
			}
			catch (Exception ex)
			{
				AsyncCausalityTracer.LogAndDisable(ex);
			}
		}

		// Token: 0x06004244 RID: 16964 RVA: 0x000F6B74 File Offset: 0x000F4D74
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceOperationRelation(CausalityTraceLevel traceLevel, int taskId, CausalityRelation relation)
		{
			try
			{
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.ETW) != (AsyncCausalityTracer.Loggers)0)
				{
					TplEtwProvider.Log.TraceOperationRelation(taskId, relation);
				}
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.CausalityTracer) != (AsyncCausalityTracer.Loggers)0)
				{
					AsyncCausalityTracer.s_TracerFactory.TraceOperationRelation((CausalityTraceLevel)traceLevel, CausalitySource.Library, AsyncCausalityTracer.s_PlatformId, AsyncCausalityTracer.GetOperationId((uint)taskId), (CausalityRelation)relation);
				}
			}
			catch (Exception ex)
			{
				AsyncCausalityTracer.LogAndDisable(ex);
			}
		}

		// Token: 0x06004245 RID: 16965 RVA: 0x000F6BD4 File Offset: 0x000F4DD4
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, int taskId, CausalitySynchronousWork work)
		{
			try
			{
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.ETW) != (AsyncCausalityTracer.Loggers)0)
				{
					TplEtwProvider.Log.TraceSynchronousWorkBegin(taskId, work);
				}
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.CausalityTracer) != (AsyncCausalityTracer.Loggers)0)
				{
					AsyncCausalityTracer.s_TracerFactory.TraceSynchronousWorkStart((CausalityTraceLevel)traceLevel, CausalitySource.Library, AsyncCausalityTracer.s_PlatformId, AsyncCausalityTracer.GetOperationId((uint)taskId), (CausalitySynchronousWork)work);
				}
			}
			catch (Exception ex)
			{
				AsyncCausalityTracer.LogAndDisable(ex);
			}
		}

		// Token: 0x06004246 RID: 16966 RVA: 0x000F6C34 File Offset: 0x000F4E34
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySynchronousWork work)
		{
			try
			{
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.ETW) != (AsyncCausalityTracer.Loggers)0)
				{
					TplEtwProvider.Log.TraceSynchronousWorkEnd(work);
				}
				if ((AsyncCausalityTracer.f_LoggingOn & AsyncCausalityTracer.Loggers.CausalityTracer) != (AsyncCausalityTracer.Loggers)0)
				{
					AsyncCausalityTracer.s_TracerFactory.TraceSynchronousWorkCompletion((CausalityTraceLevel)traceLevel, CausalitySource.Library, (CausalitySynchronousWork)work);
				}
			}
			catch (Exception ex)
			{
				AsyncCausalityTracer.LogAndDisable(ex);
			}
		}

		// Token: 0x06004247 RID: 16967 RVA: 0x000F6C88 File Offset: 0x000F4E88
		private static void LogAndDisable(Exception ex)
		{
			AsyncCausalityTracer.f_LoggingOn = (AsyncCausalityTracer.Loggers)0;
			Debugger.Log(0, "AsyncCausalityTracer", ex.ToString());
		}

		// Token: 0x06004248 RID: 16968 RVA: 0x000F6CA1 File Offset: 0x000F4EA1
		private static ulong GetOperationId(uint taskId)
		{
			return (ulong)(((long)AppDomain.CurrentDomain.Id << 32) + (long)((ulong)taskId));
		}

		// Token: 0x04001B8D RID: 7053
		private static readonly Guid s_PlatformId = new Guid(1258385830U, 62416, 16800, 155, 51, 2, 85, 6, 82, 185, 149);

		// Token: 0x04001B8E RID: 7054
		private const CausalitySource s_CausalitySource = CausalitySource.Library;

		// Token: 0x04001B8F RID: 7055
		private static IAsyncCausalityTracerStatics s_TracerFactory;

		// Token: 0x04001B90 RID: 7056
		private static AsyncCausalityTracer.Loggers f_LoggingOn;

		// Token: 0x02000C25 RID: 3109
		[Flags]
		private enum Loggers : byte
		{
			// Token: 0x040036E0 RID: 14048
			CausalityTracer = 1,
			// Token: 0x040036E1 RID: 14049
			ETW = 2
		}
	}
}
