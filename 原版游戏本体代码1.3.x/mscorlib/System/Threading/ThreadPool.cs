using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000526 RID: 1318
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public static class ThreadPool
	{
		// Token: 0x06003DF6 RID: 15862 RVA: 0x000E7760 File Offset: 0x000E5960
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
		public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
		{
			return ThreadPool.SetMaxThreadsNative(workerThreads, completionPortThreads);
		}

		// Token: 0x06003DF7 RID: 15863 RVA: 0x000E7769 File Offset: 0x000E5969
		[SecuritySafeCritical]
		public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
		{
			ThreadPool.GetMaxThreadsNative(out workerThreads, out completionPortThreads);
		}

		// Token: 0x06003DF8 RID: 15864 RVA: 0x000E7772 File Offset: 0x000E5972
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
		public static bool SetMinThreads(int workerThreads, int completionPortThreads)
		{
			return ThreadPool.SetMinThreadsNative(workerThreads, completionPortThreads);
		}

		// Token: 0x06003DF9 RID: 15865 RVA: 0x000E777B File Offset: 0x000E597B
		[SecuritySafeCritical]
		public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
		{
			ThreadPool.GetMinThreadsNative(out workerThreads, out completionPortThreads);
		}

		// Token: 0x06003DFA RID: 15866 RVA: 0x000E7784 File Offset: 0x000E5984
		[SecuritySafeCritical]
		public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
		{
			ThreadPool.GetAvailableThreadsNative(out workerThreads, out completionPortThreads);
		}

		// Token: 0x06003DFB RID: 15867 RVA: 0x000E7790 File Offset: 0x000E5990
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, ref stackCrawlMark, true);
		}

		// Token: 0x06003DFC RID: 15868 RVA: 0x000E77B0 File Offset: 0x000E59B0
		[SecurityCritical]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, ref stackCrawlMark, false);
		}

		// Token: 0x06003DFD RID: 15869 RVA: 0x000E77D0 File Offset: 0x000E59D0
		[SecurityCritical]
		private static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce, ref StackCrawlMark stackMark, bool compressStack)
		{
			if (RemotingServices.IsTransparentProxy(waitObject))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WaitOnTransparentProxy"));
			}
			RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle();
			if (callBack != null)
			{
				_ThreadPoolWaitOrTimerCallback threadPoolWaitOrTimerCallback = new _ThreadPoolWaitOrTimerCallback(callBack, state, compressStack, ref stackMark);
				state = threadPoolWaitOrTimerCallback;
				registeredWaitHandle.SetWaitObject(waitObject);
				IntPtr handle = ThreadPool.RegisterWaitForSingleObjectNative(waitObject, state, millisecondsTimeOutInterval, executeOnlyOnce, registeredWaitHandle, ref stackMark, compressStack);
				registeredWaitHandle.SetHandle(handle);
				return registeredWaitHandle;
			}
			throw new ArgumentNullException("WaitOrTimerCallback");
		}

		// Token: 0x06003DFE RID: 15870 RVA: 0x000E783C File Offset: 0x000E5A3C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
		{
			if (millisecondsTimeOutInterval < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, ref stackCrawlMark, true);
		}

		// Token: 0x06003DFF RID: 15871 RVA: 0x000E7874 File Offset: 0x000E5A74
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
		{
			if (millisecondsTimeOutInterval < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, ref stackCrawlMark, false);
		}

		// Token: 0x06003E00 RID: 15872 RVA: 0x000E78AC File Offset: 0x000E5AAC
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
		{
			if (millisecondsTimeOutInterval < -1L)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, ref stackCrawlMark, true);
		}

		// Token: 0x06003E01 RID: 15873 RVA: 0x000E78E4 File Offset: 0x000E5AE4
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
		{
			if (millisecondsTimeOutInterval < -1L)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, ref stackCrawlMark, false);
		}

		// Token: 0x06003E02 RID: 15874 RVA: 0x000E791C File Offset: 0x000E5B1C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, TimeSpan timeout, bool executeOnlyOnce)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			if (num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_LessEqualToIntegerMaxVal"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, (uint)num, executeOnlyOnce, ref stackCrawlMark, true);
		}

		// Token: 0x06003E03 RID: 15875 RVA: 0x000E797C File Offset: 0x000E5B7C
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, TimeSpan timeout, bool executeOnlyOnce)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			if (num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_LessEqualToIntegerMaxVal"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, (uint)num, executeOnlyOnce, ref stackCrawlMark, false);
		}

		// Token: 0x06003E04 RID: 15876 RVA: 0x000E79DC File Offset: 0x000E5BDC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool QueueUserWorkItem(WaitCallback callBack, object state)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.QueueUserWorkItemHelper(callBack, state, ref stackCrawlMark, true);
		}

		// Token: 0x06003E05 RID: 15877 RVA: 0x000E79F8 File Offset: 0x000E5BF8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool QueueUserWorkItem(WaitCallback callBack)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.QueueUserWorkItemHelper(callBack, null, ref stackCrawlMark, true);
		}

		// Token: 0x06003E06 RID: 15878 RVA: 0x000E7A14 File Offset: 0x000E5C14
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, object state)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ThreadPool.QueueUserWorkItemHelper(callBack, state, ref stackCrawlMark, false);
		}

		// Token: 0x06003E07 RID: 15879 RVA: 0x000E7A30 File Offset: 0x000E5C30
		[SecurityCritical]
		private static bool QueueUserWorkItemHelper(WaitCallback callBack, object state, ref StackCrawlMark stackMark, bool compressStack)
		{
			bool result = true;
			if (callBack != null)
			{
				ThreadPool.EnsureVMInitialized();
				try
				{
					return result;
				}
				finally
				{
					QueueUserWorkItemCallback callback = new QueueUserWorkItemCallback(callBack, state, compressStack, ref stackMark);
					ThreadPoolGlobals.workQueue.Enqueue(callback, true);
					result = true;
				}
			}
			throw new ArgumentNullException("WaitCallback");
		}

		// Token: 0x06003E08 RID: 15880 RVA: 0x000E7A80 File Offset: 0x000E5C80
		[SecurityCritical]
		internal static void UnsafeQueueCustomWorkItem(IThreadPoolWorkItem workItem, bool forceGlobal)
		{
			ThreadPool.EnsureVMInitialized();
			try
			{
			}
			finally
			{
				ThreadPoolGlobals.workQueue.Enqueue(workItem, forceGlobal);
			}
		}

		// Token: 0x06003E09 RID: 15881 RVA: 0x000E7AB4 File Offset: 0x000E5CB4
		[SecurityCritical]
		internal static bool TryPopCustomWorkItem(IThreadPoolWorkItem workItem)
		{
			return ThreadPoolGlobals.vmTpInitialized && ThreadPoolGlobals.workQueue.LocalFindAndPop(workItem);
		}

		// Token: 0x06003E0A RID: 15882 RVA: 0x000E7ACC File Offset: 0x000E5CCC
		[SecurityCritical]
		internal static IEnumerable<IThreadPoolWorkItem> GetQueuedWorkItems()
		{
			return ThreadPool.EnumerateQueuedWorkItems(ThreadPoolWorkQueue.allThreadQueues.Current, ThreadPoolGlobals.workQueue.queueTail);
		}

		// Token: 0x06003E0B RID: 15883 RVA: 0x000E7AE9 File Offset: 0x000E5CE9
		internal static IEnumerable<IThreadPoolWorkItem> EnumerateQueuedWorkItems(ThreadPoolWorkQueue.WorkStealingQueue[] wsQueues, ThreadPoolWorkQueue.QueueSegment globalQueueTail)
		{
			if (wsQueues != null)
			{
				foreach (ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue in wsQueues)
				{
					if (workStealingQueue != null && workStealingQueue.m_array != null)
					{
						IThreadPoolWorkItem[] items = workStealingQueue.m_array;
						int num;
						for (int i = 0; i < items.Length; i = num + 1)
						{
							IThreadPoolWorkItem threadPoolWorkItem = items[i];
							if (threadPoolWorkItem != null)
							{
								yield return threadPoolWorkItem;
							}
							num = i;
						}
						items = null;
					}
				}
				ThreadPoolWorkQueue.WorkStealingQueue[] array = null;
			}
			if (globalQueueTail != null)
			{
				ThreadPoolWorkQueue.QueueSegment segment;
				for (segment = globalQueueTail; segment != null; segment = segment.Next)
				{
					IThreadPoolWorkItem[] items = segment.nodes;
					int num;
					for (int j = 0; j < items.Length; j = num + 1)
					{
						IThreadPoolWorkItem threadPoolWorkItem2 = items[j];
						if (threadPoolWorkItem2 != null)
						{
							yield return threadPoolWorkItem2;
						}
						num = j;
					}
					items = null;
				}
				segment = null;
			}
			yield break;
		}

		// Token: 0x06003E0C RID: 15884 RVA: 0x000E7B00 File Offset: 0x000E5D00
		[SecurityCritical]
		internal static IEnumerable<IThreadPoolWorkItem> GetLocallyQueuedWorkItems()
		{
			return ThreadPool.EnumerateQueuedWorkItems(new ThreadPoolWorkQueue.WorkStealingQueue[] { ThreadPoolWorkQueueThreadLocals.threadLocals.workStealingQueue }, null);
		}

		// Token: 0x06003E0D RID: 15885 RVA: 0x000E7B1B File Offset: 0x000E5D1B
		[SecurityCritical]
		internal static IEnumerable<IThreadPoolWorkItem> GetGloballyQueuedWorkItems()
		{
			return ThreadPool.EnumerateQueuedWorkItems(null, ThreadPoolGlobals.workQueue.queueTail);
		}

		// Token: 0x06003E0E RID: 15886 RVA: 0x000E7B30 File Offset: 0x000E5D30
		private static object[] ToObjectArray(IEnumerable<IThreadPoolWorkItem> workitems)
		{
			int num = 0;
			foreach (IThreadPoolWorkItem threadPoolWorkItem in workitems)
			{
				num++;
			}
			object[] array = new object[num];
			num = 0;
			foreach (IThreadPoolWorkItem threadPoolWorkItem2 in workitems)
			{
				if (num < array.Length)
				{
					array[num] = threadPoolWorkItem2;
				}
				num++;
			}
			return array;
		}

		// Token: 0x06003E0F RID: 15887 RVA: 0x000E7BC8 File Offset: 0x000E5DC8
		[SecurityCritical]
		internal static object[] GetQueuedWorkItemsForDebugger()
		{
			return ThreadPool.ToObjectArray(ThreadPool.GetQueuedWorkItems());
		}

		// Token: 0x06003E10 RID: 15888 RVA: 0x000E7BD4 File Offset: 0x000E5DD4
		[SecurityCritical]
		internal static object[] GetGloballyQueuedWorkItemsForDebugger()
		{
			return ThreadPool.ToObjectArray(ThreadPool.GetGloballyQueuedWorkItems());
		}

		// Token: 0x06003E11 RID: 15889 RVA: 0x000E7BE0 File Offset: 0x000E5DE0
		[SecurityCritical]
		internal static object[] GetLocallyQueuedWorkItemsForDebugger()
		{
			return ThreadPool.ToObjectArray(ThreadPool.GetLocallyQueuedWorkItems());
		}

		// Token: 0x06003E12 RID: 15890
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern bool RequestWorkerThread();

		// Token: 0x06003E13 RID: 15891
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

		// Token: 0x06003E14 RID: 15892 RVA: 0x000E7BEC File Offset: 0x000E5DEC
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe static bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
		{
			return ThreadPool.PostQueuedCompletionStatus(overlapped);
		}

		// Token: 0x06003E15 RID: 15893 RVA: 0x000E7BF4 File Offset: 0x000E5DF4
		[SecurityCritical]
		private static void EnsureVMInitialized()
		{
			if (!ThreadPoolGlobals.vmTpInitialized)
			{
				ThreadPool.InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
				ThreadPoolGlobals.vmTpInitialized = true;
			}
		}

		// Token: 0x06003E16 RID: 15894
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool SetMinThreadsNative(int workerThreads, int completionPortThreads);

		// Token: 0x06003E17 RID: 15895
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool SetMaxThreadsNative(int workerThreads, int completionPortThreads);

		// Token: 0x06003E18 RID: 15896
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);

		// Token: 0x06003E19 RID: 15897
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);

		// Token: 0x06003E1A RID: 15898
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void GetAvailableThreadsNative(out int workerThreads, out int completionPortThreads);

		// Token: 0x06003E1B RID: 15899
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool NotifyWorkItemComplete();

		// Token: 0x06003E1C RID: 15900
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void ReportThreadStatus(bool isWorking);

		// Token: 0x06003E1D RID: 15901 RVA: 0x000E7C11 File Offset: 0x000E5E11
		[SecuritySafeCritical]
		internal static void NotifyWorkItemProgress()
		{
			if (!ThreadPoolGlobals.vmTpInitialized)
			{
				ThreadPool.InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
			}
			ThreadPool.NotifyWorkItemProgressNative();
		}

		// Token: 0x06003E1E RID: 15902
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void NotifyWorkItemProgressNative();

		// Token: 0x06003E1F RID: 15903
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsThreadPoolHosted();

		// Token: 0x06003E20 RID: 15904
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void InitializeVMTp(ref bool enableWorkerTracking);

		// Token: 0x06003E21 RID: 15905
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern IntPtr RegisterWaitForSingleObjectNative(WaitHandle waitHandle, object state, uint timeOutInterval, bool executeOnlyOnce, RegisteredWaitHandle registeredWaitHandle, ref StackCrawlMark stackMark, bool compressStack);

		// Token: 0x06003E22 RID: 15906 RVA: 0x000E7C2B File Offset: 0x000E5E2B
		[SecuritySafeCritical]
		[Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated.  Please use ThreadPool.BindHandle(SafeHandle) instead.", false)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static bool BindHandle(IntPtr osHandle)
		{
			return ThreadPool.BindIOCompletionCallbackNative(osHandle);
		}

		// Token: 0x06003E23 RID: 15907 RVA: 0x000E7C34 File Offset: 0x000E5E34
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static bool BindHandle(SafeHandle osHandle)
		{
			if (osHandle == null)
			{
				throw new ArgumentNullException("osHandle");
			}
			bool result = false;
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				osHandle.DangerousAddRef(ref flag);
				result = ThreadPool.BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
			}
			finally
			{
				if (flag)
				{
					osHandle.DangerousRelease();
				}
			}
			return result;
		}

		// Token: 0x06003E24 RID: 15908
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);
	}
}
