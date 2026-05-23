using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004F8 RID: 1272
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class ExecutionContext : IDisposable, ISerializable
	{
		// Token: 0x1700090E RID: 2318
		// (get) Token: 0x06003BF6 RID: 15350 RVA: 0x000E3522 File Offset: 0x000E1722
		// (set) Token: 0x06003BF7 RID: 15351 RVA: 0x000E352F File Offset: 0x000E172F
		internal bool isNewCapture
		{
			get
			{
				return (this._flags & (ExecutionContext.Flags)5) > ExecutionContext.Flags.None;
			}
			set
			{
				if (value)
				{
					this._flags |= ExecutionContext.Flags.IsNewCapture;
					return;
				}
				this._flags &= (ExecutionContext.Flags)(-2);
			}
		}

		// Token: 0x1700090F RID: 2319
		// (get) Token: 0x06003BF8 RID: 15352 RVA: 0x000E3552 File Offset: 0x000E1752
		// (set) Token: 0x06003BF9 RID: 15353 RVA: 0x000E355F File Offset: 0x000E175F
		internal bool isFlowSuppressed
		{
			get
			{
				return (this._flags & ExecutionContext.Flags.IsFlowSuppressed) > ExecutionContext.Flags.None;
			}
			set
			{
				if (value)
				{
					this._flags |= ExecutionContext.Flags.IsFlowSuppressed;
					return;
				}
				this._flags &= (ExecutionContext.Flags)(-3);
			}
		}

		// Token: 0x17000910 RID: 2320
		// (get) Token: 0x06003BFA RID: 15354 RVA: 0x000E3582 File Offset: 0x000E1782
		internal static ExecutionContext PreAllocatedDefault
		{
			[SecuritySafeCritical]
			get
			{
				return ExecutionContext.s_dummyDefaultEC;
			}
		}

		// Token: 0x17000911 RID: 2321
		// (get) Token: 0x06003BFB RID: 15355 RVA: 0x000E3589 File Offset: 0x000E1789
		internal bool IsPreAllocatedDefault
		{
			get
			{
				return (this._flags & ExecutionContext.Flags.IsPreAllocatedDefault) != ExecutionContext.Flags.None;
			}
		}

		// Token: 0x06003BFC RID: 15356 RVA: 0x000E3598 File Offset: 0x000E1798
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal ExecutionContext()
		{
		}

		// Token: 0x06003BFD RID: 15357 RVA: 0x000E35A0 File Offset: 0x000E17A0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal ExecutionContext(bool isPreAllocatedDefault)
		{
			if (isPreAllocatedDefault)
			{
				this._flags = ExecutionContext.Flags.IsPreAllocatedDefault;
			}
		}

		// Token: 0x06003BFE RID: 15358 RVA: 0x000E35B4 File Offset: 0x000E17B4
		[SecurityCritical]
		internal static object GetLocalValue(IAsyncLocal local)
		{
			return Thread.CurrentThread.GetExecutionContextReader().GetLocalValue(local);
		}

		// Token: 0x06003BFF RID: 15359 RVA: 0x000E35D4 File Offset: 0x000E17D4
		[SecurityCritical]
		internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
		{
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			object obj = null;
			bool flag = mutableExecutionContext._localValues != null && mutableExecutionContext._localValues.TryGetValue(local, out obj);
			if (obj == newValue)
			{
				return;
			}
			IAsyncLocalValueMap asyncLocalValueMap = mutableExecutionContext._localValues;
			if (asyncLocalValueMap == null)
			{
				asyncLocalValueMap = AsyncLocalValueMap.Create(local, newValue, !needChangeNotifications);
			}
			else
			{
				asyncLocalValueMap = asyncLocalValueMap.Set(local, newValue, !needChangeNotifications);
			}
			mutableExecutionContext._localValues = asyncLocalValueMap;
			if (needChangeNotifications)
			{
				if (!flag)
				{
					IAsyncLocal[] array = mutableExecutionContext._localChangeNotifications;
					if (array == null)
					{
						array = new IAsyncLocal[] { local };
					}
					else
					{
						int num = array.Length;
						Array.Resize<IAsyncLocal>(ref array, num + 1);
						array[num] = local;
					}
					mutableExecutionContext._localChangeNotifications = array;
				}
				local.OnValueChanged(obj, newValue, false);
			}
		}

		// Token: 0x06003C00 RID: 15360 RVA: 0x000E3684 File Offset: 0x000E1884
		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		internal static void OnAsyncLocalContextChanged(ExecutionContext previous, ExecutionContext current)
		{
			IAsyncLocal[] array = ((previous == null) ? null : previous._localChangeNotifications);
			if (array != null)
			{
				foreach (IAsyncLocal asyncLocal in array)
				{
					object obj = null;
					if (previous != null && previous._localValues != null)
					{
						previous._localValues.TryGetValue(asyncLocal, out obj);
					}
					object obj2 = null;
					if (current != null && current._localValues != null)
					{
						current._localValues.TryGetValue(asyncLocal, out obj2);
					}
					if (obj != obj2)
					{
						asyncLocal.OnValueChanged(obj, obj2, true);
					}
				}
			}
			IAsyncLocal[] array3 = ((current == null) ? null : current._localChangeNotifications);
			if (array3 != null && array3 != array)
			{
				try
				{
					foreach (IAsyncLocal asyncLocal2 in array3)
					{
						object obj3 = null;
						if (previous == null || previous._localValues == null || !previous._localValues.TryGetValue(asyncLocal2, out obj3))
						{
							object obj4 = null;
							if (current != null && current._localValues != null)
							{
								current._localValues.TryGetValue(asyncLocal2, out obj4);
							}
							if (obj3 != obj4)
							{
								asyncLocal2.OnValueChanged(obj3, obj4, true);
							}
						}
					}
				}
				catch (Exception exception)
				{
					Environment.FailFast(Environment.GetResourceString("ExecutionContext_ExceptionInAsyncLocalNotification"), exception);
				}
			}
		}

		// Token: 0x17000912 RID: 2322
		// (get) Token: 0x06003C01 RID: 15361 RVA: 0x000E37B4 File Offset: 0x000E19B4
		// (set) Token: 0x06003C02 RID: 15362 RVA: 0x000E37CF File Offset: 0x000E19CF
		internal LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				if (this._logicalCallContext == null)
				{
					this._logicalCallContext = new LogicalCallContext();
				}
				return this._logicalCallContext;
			}
			[SecurityCritical]
			set
			{
				this._logicalCallContext = value;
			}
		}

		// Token: 0x17000913 RID: 2323
		// (get) Token: 0x06003C03 RID: 15363 RVA: 0x000E37D8 File Offset: 0x000E19D8
		// (set) Token: 0x06003C04 RID: 15364 RVA: 0x000E37F3 File Offset: 0x000E19F3
		internal IllogicalCallContext IllogicalCallContext
		{
			get
			{
				if (this._illogicalCallContext == null)
				{
					this._illogicalCallContext = new IllogicalCallContext();
				}
				return this._illogicalCallContext;
			}
			set
			{
				this._illogicalCallContext = value;
			}
		}

		// Token: 0x17000914 RID: 2324
		// (get) Token: 0x06003C05 RID: 15365 RVA: 0x000E37FC File Offset: 0x000E19FC
		// (set) Token: 0x06003C06 RID: 15366 RVA: 0x000E3804 File Offset: 0x000E1A04
		internal SynchronizationContext SynchronizationContext
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._syncContext;
			}
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			set
			{
				this._syncContext = value;
			}
		}

		// Token: 0x17000915 RID: 2325
		// (get) Token: 0x06003C07 RID: 15367 RVA: 0x000E380D File Offset: 0x000E1A0D
		// (set) Token: 0x06003C08 RID: 15368 RVA: 0x000E3815 File Offset: 0x000E1A15
		internal SynchronizationContext SynchronizationContextNoFlow
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._syncContextNoFlow;
			}
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			set
			{
				this._syncContextNoFlow = value;
			}
		}

		// Token: 0x17000916 RID: 2326
		// (get) Token: 0x06003C09 RID: 15369 RVA: 0x000E381E File Offset: 0x000E1A1E
		// (set) Token: 0x06003C0A RID: 15370 RVA: 0x000E3826 File Offset: 0x000E1A26
		internal HostExecutionContext HostExecutionContext
		{
			get
			{
				return this._hostExecutionContext;
			}
			set
			{
				this._hostExecutionContext = value;
			}
		}

		// Token: 0x17000917 RID: 2327
		// (get) Token: 0x06003C0B RID: 15371 RVA: 0x000E382F File Offset: 0x000E1A2F
		// (set) Token: 0x06003C0C RID: 15372 RVA: 0x000E3837 File Offset: 0x000E1A37
		internal SecurityContext SecurityContext
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._securityContext;
			}
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			set
			{
				this._securityContext = value;
				if (value != null)
				{
					this._securityContext.ExecutionContext = this;
				}
			}
		}

		// Token: 0x06003C0D RID: 15373 RVA: 0x000E384F File Offset: 0x000E1A4F
		public void Dispose()
		{
			if (this.IsPreAllocatedDefault)
			{
				return;
			}
			if (this._hostExecutionContext != null)
			{
				this._hostExecutionContext.Dispose();
			}
			if (this._securityContext != null)
			{
				this._securityContext.Dispose();
			}
		}

		// Token: 0x06003C0E RID: 15374 RVA: 0x000E3880 File Offset: 0x000E1A80
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static void Run(ExecutionContext executionContext, ContextCallback callback, object state)
		{
			if (executionContext == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullContext"));
			}
			if (!executionContext.isNewCapture)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
			}
			ExecutionContext.Run(executionContext, callback, state, false);
		}

		// Token: 0x06003C0F RID: 15375 RVA: 0x000E38B6 File Offset: 0x000E1AB6
		[SecurityCritical]
		[FriendAccessAllowed]
		internal static void Run(ExecutionContext executionContext, ContextCallback callback, object state, bool preserveSyncCtx)
		{
			ExecutionContext.RunInternal(executionContext, callback, state, preserveSyncCtx);
		}

		// Token: 0x06003C10 RID: 15376 RVA: 0x000E38C4 File Offset: 0x000E1AC4
		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		internal static void RunInternal(ExecutionContext executionContext, ContextCallback callback, object state, bool preserveSyncCtx)
		{
			if (!executionContext.IsPreAllocatedDefault)
			{
				executionContext.isNewCapture = false;
			}
			Thread currentThread = Thread.CurrentThread;
			ExecutionContextSwitcher executionContextSwitcher = default(ExecutionContextSwitcher);
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				ExecutionContext.Reader executionContextReader = currentThread.GetExecutionContextReader();
				if ((executionContextReader.IsNull || executionContextReader.IsDefaultFTContext(preserveSyncCtx)) && SecurityContext.CurrentlyInDefaultFTSecurityContext(executionContextReader) && executionContext.IsDefaultFTContext(preserveSyncCtx) && executionContextReader.HasSameLocalValues(executionContext))
				{
					ExecutionContext.EstablishCopyOnWriteScope(currentThread, true, ref executionContextSwitcher);
				}
				else
				{
					if (executionContext.IsPreAllocatedDefault)
					{
						executionContext = new ExecutionContext();
					}
					executionContextSwitcher = ExecutionContext.SetExecutionContext(executionContext, preserveSyncCtx);
				}
				callback(state);
			}
			finally
			{
				executionContextSwitcher.Undo();
			}
		}

		// Token: 0x06003C11 RID: 15377 RVA: 0x000E396C File Offset: 0x000E1B6C
		[SecurityCritical]
		internal static void EstablishCopyOnWriteScope(ref ExecutionContextSwitcher ecsw)
		{
			ExecutionContext.EstablishCopyOnWriteScope(Thread.CurrentThread, false, ref ecsw);
		}

		// Token: 0x06003C12 RID: 15378 RVA: 0x000E397C File Offset: 0x000E1B7C
		[SecurityCritical]
		private static void EstablishCopyOnWriteScope(Thread currentThread, bool knownNullWindowsIdentity, ref ExecutionContextSwitcher ecsw)
		{
			ecsw.outerEC = currentThread.GetExecutionContextReader();
			ecsw.outerECBelongsToScope = currentThread.ExecutionContextBelongsToCurrentScope;
			ecsw.cachedAlwaysFlowImpersonationPolicy = SecurityContext.AlwaysFlowImpersonationPolicy;
			if (!knownNullWindowsIdentity)
			{
				ecsw.wi = SecurityContext.GetCurrentWI(ecsw.outerEC, ecsw.cachedAlwaysFlowImpersonationPolicy);
			}
			ecsw.wiIsValid = true;
			currentThread.ExecutionContextBelongsToCurrentScope = false;
			ecsw.thread = currentThread;
		}

		// Token: 0x06003C13 RID: 15379 RVA: 0x000E39DC File Offset: 0x000E1BDC
		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static ExecutionContextSwitcher SetExecutionContext(ExecutionContext executionContext, bool preserveSyncCtx)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			ExecutionContextSwitcher executionContextSwitcher = default(ExecutionContextSwitcher);
			Thread currentThread = Thread.CurrentThread;
			ExecutionContext.Reader executionContextReader = currentThread.GetExecutionContextReader();
			executionContextSwitcher.thread = currentThread;
			executionContextSwitcher.outerEC = executionContextReader;
			executionContextSwitcher.outerECBelongsToScope = currentThread.ExecutionContextBelongsToCurrentScope;
			if (preserveSyncCtx)
			{
				executionContext.SynchronizationContext = executionContextReader.SynchronizationContext;
			}
			executionContext.SynchronizationContextNoFlow = executionContextReader.SynchronizationContextNoFlow;
			currentThread.SetExecutionContext(executionContext, true);
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				ExecutionContext.OnAsyncLocalContextChanged(executionContextReader.DangerousGetRawExecutionContext(), executionContext);
				SecurityContext securityContext = executionContext.SecurityContext;
				if (securityContext != null)
				{
					SecurityContext.Reader securityContext2 = executionContextReader.SecurityContext;
					executionContextSwitcher.scsw = SecurityContext.SetSecurityContext(securityContext, securityContext2, false, ref stackCrawlMark);
				}
				else if (!SecurityContext.CurrentlyInDefaultFTSecurityContext(executionContextSwitcher.outerEC))
				{
					SecurityContext.Reader securityContext3 = executionContextReader.SecurityContext;
					executionContextSwitcher.scsw = SecurityContext.SetSecurityContext(SecurityContext.FullTrustSecurityContext, securityContext3, false, ref stackCrawlMark);
				}
				HostExecutionContext hostExecutionContext = executionContext.HostExecutionContext;
				if (hostExecutionContext != null)
				{
					executionContextSwitcher.hecsw = HostExecutionContextManager.SetHostExecutionContextInternal(hostExecutionContext);
				}
			}
			catch
			{
				executionContextSwitcher.UndoNoThrow();
				throw;
			}
			return executionContextSwitcher;
		}

		// Token: 0x06003C14 RID: 15380 RVA: 0x000E3AE4 File Offset: 0x000E1CE4
		[SecuritySafeCritical]
		public ExecutionContext CreateCopy()
		{
			if (!this.isNewCapture)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotCopyUsedContext"));
			}
			ExecutionContext executionContext = new ExecutionContext();
			executionContext.isNewCapture = true;
			executionContext._syncContext = ((this._syncContext == null) ? null : this._syncContext.CreateCopy());
			executionContext._localValues = this._localValues;
			executionContext._localChangeNotifications = this._localChangeNotifications;
			executionContext._hostExecutionContext = ((this._hostExecutionContext == null) ? null : this._hostExecutionContext.CreateCopy());
			if (this._securityContext != null)
			{
				executionContext._securityContext = this._securityContext.CreateCopy();
				executionContext._securityContext.ExecutionContext = executionContext;
			}
			if (this._logicalCallContext != null)
			{
				executionContext.LogicalCallContext = (LogicalCallContext)this.LogicalCallContext.Clone();
			}
			return executionContext;
		}

		// Token: 0x06003C15 RID: 15381 RVA: 0x000E3BAC File Offset: 0x000E1DAC
		[SecuritySafeCritical]
		internal ExecutionContext CreateMutableCopy()
		{
			ExecutionContext executionContext = new ExecutionContext();
			executionContext._syncContext = this._syncContext;
			executionContext._syncContextNoFlow = this._syncContextNoFlow;
			executionContext._hostExecutionContext = ((this._hostExecutionContext == null) ? null : this._hostExecutionContext.CreateCopy());
			if (this._securityContext != null)
			{
				executionContext._securityContext = this._securityContext.CreateMutableCopy();
				executionContext._securityContext.ExecutionContext = executionContext;
			}
			if (this._logicalCallContext != null)
			{
				executionContext.LogicalCallContext = (LogicalCallContext)this.LogicalCallContext.Clone();
			}
			if (this._illogicalCallContext != null)
			{
				executionContext.IllogicalCallContext = this.IllogicalCallContext.CreateCopy();
			}
			executionContext._localValues = this._localValues;
			executionContext._localChangeNotifications = this._localChangeNotifications;
			executionContext.isFlowSuppressed = this.isFlowSuppressed;
			return executionContext;
		}

		// Token: 0x06003C16 RID: 15382 RVA: 0x000E3C74 File Offset: 0x000E1E74
		[SecurityCritical]
		public static AsyncFlowControl SuppressFlow()
		{
			if (ExecutionContext.IsFlowSuppressed())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotSupressFlowMultipleTimes"));
			}
			AsyncFlowControl result = default(AsyncFlowControl);
			result.Setup();
			return result;
		}

		// Token: 0x06003C17 RID: 15383 RVA: 0x000E3CA8 File Offset: 0x000E1EA8
		[SecuritySafeCritical]
		public static void RestoreFlow()
		{
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			if (!mutableExecutionContext.isFlowSuppressed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRestoreUnsupressedFlow"));
			}
			mutableExecutionContext.isFlowSuppressed = false;
		}

		// Token: 0x06003C18 RID: 15384 RVA: 0x000E3CE0 File Offset: 0x000E1EE0
		public static bool IsFlowSuppressed()
		{
			return Thread.CurrentThread.GetExecutionContextReader().IsFlowSuppressed;
		}

		// Token: 0x06003C19 RID: 15385 RVA: 0x000E3D00 File Offset: 0x000E1F00
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static ExecutionContext Capture()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ExecutionContext.Capture(ref stackCrawlMark, ExecutionContext.CaptureOptions.None);
		}

		// Token: 0x06003C1A RID: 15386 RVA: 0x000E3D18 File Offset: 0x000E1F18
		[SecuritySafeCritical]
		[FriendAccessAllowed]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static ExecutionContext FastCapture()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return ExecutionContext.Capture(ref stackCrawlMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
		}

		// Token: 0x06003C1B RID: 15387 RVA: 0x000E3D30 File Offset: 0x000E1F30
		[SecurityCritical]
		internal static ExecutionContext Capture(ref StackCrawlMark stackMark, ExecutionContext.CaptureOptions options)
		{
			ExecutionContext.Reader executionContextReader = Thread.CurrentThread.GetExecutionContextReader();
			if (executionContextReader.IsFlowSuppressed)
			{
				return null;
			}
			SecurityContext securityContext = SecurityContext.Capture(executionContextReader, ref stackMark);
			HostExecutionContext hostExecutionContext = HostExecutionContextManager.CaptureHostExecutionContext();
			SynchronizationContext synchronizationContext = null;
			LogicalCallContext logicalCallContext = null;
			if (!executionContextReader.IsNull)
			{
				if ((options & ExecutionContext.CaptureOptions.IgnoreSyncCtx) == ExecutionContext.CaptureOptions.None)
				{
					synchronizationContext = ((executionContextReader.SynchronizationContext == null) ? null : executionContextReader.SynchronizationContext.CreateCopy());
				}
				if (executionContextReader.LogicalCallContext.HasInfo)
				{
					logicalCallContext = executionContextReader.LogicalCallContext.Clone();
				}
			}
			IAsyncLocalValueMap asyncLocalValueMap = null;
			IAsyncLocal[] array = null;
			if (!executionContextReader.IsNull)
			{
				asyncLocalValueMap = executionContextReader.DangerousGetRawExecutionContext()._localValues;
				array = executionContextReader.DangerousGetRawExecutionContext()._localChangeNotifications;
			}
			if ((options & ExecutionContext.CaptureOptions.OptimizeDefaultCase) != ExecutionContext.CaptureOptions.None && securityContext == null && hostExecutionContext == null && synchronizationContext == null && (logicalCallContext == null || !logicalCallContext.HasInfo) && asyncLocalValueMap == null && array == null)
			{
				return ExecutionContext.s_dummyDefaultEC;
			}
			ExecutionContext executionContext = new ExecutionContext();
			executionContext.SecurityContext = securityContext;
			if (executionContext.SecurityContext != null)
			{
				executionContext.SecurityContext.ExecutionContext = executionContext;
			}
			executionContext._hostExecutionContext = hostExecutionContext;
			executionContext._syncContext = synchronizationContext;
			executionContext.LogicalCallContext = logicalCallContext;
			executionContext._localValues = asyncLocalValueMap;
			executionContext._localChangeNotifications = array;
			executionContext.isNewCapture = true;
			return executionContext;
		}

		// Token: 0x06003C1C RID: 15388 RVA: 0x000E3E60 File Offset: 0x000E2060
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			if (this._logicalCallContext != null)
			{
				info.AddValue("LogicalCallContext", this._logicalCallContext, typeof(LogicalCallContext));
			}
		}

		// Token: 0x06003C1D RID: 15389 RVA: 0x000E3E94 File Offset: 0x000E2094
		[SecurityCritical]
		private ExecutionContext(SerializationInfo info, StreamingContext context)
		{
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Name.Equals("LogicalCallContext"))
				{
					this._logicalCallContext = (LogicalCallContext)enumerator.Value;
				}
			}
		}

		// Token: 0x06003C1E RID: 15390 RVA: 0x000E3EDC File Offset: 0x000E20DC
		[SecurityCritical]
		internal bool IsDefaultFTContext(bool ignoreSyncCtx)
		{
			return this._hostExecutionContext == null && (ignoreSyncCtx || this._syncContext == null) && (this._securityContext == null || this._securityContext.IsDefaultFTSecurityContext()) && (this._logicalCallContext == null || !this._logicalCallContext.HasInfo) && (this._illogicalCallContext == null || !this._illogicalCallContext.HasUserData);
		}

		// Token: 0x0400198F RID: 6543
		private HostExecutionContext _hostExecutionContext;

		// Token: 0x04001990 RID: 6544
		private SynchronizationContext _syncContext;

		// Token: 0x04001991 RID: 6545
		private SynchronizationContext _syncContextNoFlow;

		// Token: 0x04001992 RID: 6546
		private SecurityContext _securityContext;

		// Token: 0x04001993 RID: 6547
		[SecurityCritical]
		private LogicalCallContext _logicalCallContext;

		// Token: 0x04001994 RID: 6548
		private IllogicalCallContext _illogicalCallContext;

		// Token: 0x04001995 RID: 6549
		private ExecutionContext.Flags _flags;

		// Token: 0x04001996 RID: 6550
		private IAsyncLocalValueMap _localValues;

		// Token: 0x04001997 RID: 6551
		private IAsyncLocal[] _localChangeNotifications;

		// Token: 0x04001998 RID: 6552
		private static readonly ExecutionContext s_dummyDefaultEC = new ExecutionContext(true);

		// Token: 0x02000BEE RID: 3054
		private enum Flags
		{
			// Token: 0x04003613 RID: 13843
			None,
			// Token: 0x04003614 RID: 13844
			IsNewCapture,
			// Token: 0x04003615 RID: 13845
			IsFlowSuppressed,
			// Token: 0x04003616 RID: 13846
			IsPreAllocatedDefault = 4
		}

		// Token: 0x02000BEF RID: 3055
		internal struct Reader
		{
			// Token: 0x06006F56 RID: 28502 RVA: 0x0017FAF9 File Offset: 0x0017DCF9
			public Reader(ExecutionContext ec)
			{
				this.m_ec = ec;
			}

			// Token: 0x06006F57 RID: 28503 RVA: 0x0017FB02 File Offset: 0x0017DD02
			public ExecutionContext DangerousGetRawExecutionContext()
			{
				return this.m_ec;
			}

			// Token: 0x1700131B RID: 4891
			// (get) Token: 0x06006F58 RID: 28504 RVA: 0x0017FB0A File Offset: 0x0017DD0A
			public bool IsNull
			{
				get
				{
					return this.m_ec == null;
				}
			}

			// Token: 0x06006F59 RID: 28505 RVA: 0x0017FB15 File Offset: 0x0017DD15
			[SecurityCritical]
			public bool IsDefaultFTContext(bool ignoreSyncCtx)
			{
				return this.m_ec.IsDefaultFTContext(ignoreSyncCtx);
			}

			// Token: 0x1700131C RID: 4892
			// (get) Token: 0x06006F5A RID: 28506 RVA: 0x0017FB23 File Offset: 0x0017DD23
			public bool IsFlowSuppressed
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return !this.IsNull && this.m_ec.isFlowSuppressed;
				}
			}

			// Token: 0x06006F5B RID: 28507 RVA: 0x0017FB3A File Offset: 0x0017DD3A
			public bool IsSame(ExecutionContext.Reader other)
			{
				return this.m_ec == other.m_ec;
			}

			// Token: 0x1700131D RID: 4893
			// (get) Token: 0x06006F5C RID: 28508 RVA: 0x0017FB4A File Offset: 0x0017DD4A
			public SynchronizationContext SynchronizationContext
			{
				get
				{
					if (!this.IsNull)
					{
						return this.m_ec.SynchronizationContext;
					}
					return null;
				}
			}

			// Token: 0x1700131E RID: 4894
			// (get) Token: 0x06006F5D RID: 28509 RVA: 0x0017FB61 File Offset: 0x0017DD61
			public SynchronizationContext SynchronizationContextNoFlow
			{
				get
				{
					if (!this.IsNull)
					{
						return this.m_ec.SynchronizationContextNoFlow;
					}
					return null;
				}
			}

			// Token: 0x1700131F RID: 4895
			// (get) Token: 0x06006F5E RID: 28510 RVA: 0x0017FB78 File Offset: 0x0017DD78
			public SecurityContext.Reader SecurityContext
			{
				[SecurityCritical]
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return new SecurityContext.Reader(this.IsNull ? null : this.m_ec.SecurityContext);
				}
			}

			// Token: 0x17001320 RID: 4896
			// (get) Token: 0x06006F5F RID: 28511 RVA: 0x0017FB95 File Offset: 0x0017DD95
			public LogicalCallContext.Reader LogicalCallContext
			{
				[SecurityCritical]
				get
				{
					return new LogicalCallContext.Reader(this.IsNull ? null : this.m_ec.LogicalCallContext);
				}
			}

			// Token: 0x17001321 RID: 4897
			// (get) Token: 0x06006F60 RID: 28512 RVA: 0x0017FBB2 File Offset: 0x0017DDB2
			public IllogicalCallContext.Reader IllogicalCallContext
			{
				[SecurityCritical]
				get
				{
					return new IllogicalCallContext.Reader(this.IsNull ? null : this.m_ec.IllogicalCallContext);
				}
			}

			// Token: 0x06006F61 RID: 28513 RVA: 0x0017FBD0 File Offset: 0x0017DDD0
			[SecurityCritical]
			public object GetLocalValue(IAsyncLocal local)
			{
				if (this.IsNull)
				{
					return null;
				}
				if (this.m_ec._localValues == null)
				{
					return null;
				}
				object result;
				this.m_ec._localValues.TryGetValue(local, out result);
				return result;
			}

			// Token: 0x06006F62 RID: 28514 RVA: 0x0017FC0C File Offset: 0x0017DE0C
			[SecurityCritical]
			public bool HasSameLocalValues(ExecutionContext other)
			{
				IAsyncLocalValueMap asyncLocalValueMap = (this.IsNull ? null : this.m_ec._localValues);
				IAsyncLocalValueMap asyncLocalValueMap2 = ((other == null) ? null : other._localValues);
				return asyncLocalValueMap == asyncLocalValueMap2;
			}

			// Token: 0x06006F63 RID: 28515 RVA: 0x0017FC41 File Offset: 0x0017DE41
			[SecurityCritical]
			public bool HasLocalValues()
			{
				return !this.IsNull && this.m_ec._localValues != null;
			}

			// Token: 0x04003617 RID: 13847
			private ExecutionContext m_ec;
		}

		// Token: 0x02000BF0 RID: 3056
		[Flags]
		internal enum CaptureOptions
		{
			// Token: 0x04003619 RID: 13849
			None = 0,
			// Token: 0x0400361A RID: 13850
			IgnoreSyncCtx = 1,
			// Token: 0x0400361B RID: 13851
			OptimizeDefaultCase = 2
		}
	}
}
