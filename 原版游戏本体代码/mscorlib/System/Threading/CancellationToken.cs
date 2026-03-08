using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x0200054B RID: 1355
	[ComVisible(false)]
	[DebuggerDisplay("IsCancellationRequested = {IsCancellationRequested}")]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public struct CancellationToken
	{
		// Token: 0x17000967 RID: 2407
		// (get) Token: 0x06003F79 RID: 16249 RVA: 0x000EC46C File Offset: 0x000EA66C
		[__DynamicallyInvokable]
		public static CancellationToken None
		{
			[__DynamicallyInvokable]
			get
			{
				return default(CancellationToken);
			}
		}

		// Token: 0x17000968 RID: 2408
		// (get) Token: 0x06003F7A RID: 16250 RVA: 0x000EC482 File Offset: 0x000EA682
		[__DynamicallyInvokable]
		public bool IsCancellationRequested
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_source != null && this.m_source.IsCancellationRequested;
			}
		}

		// Token: 0x17000969 RID: 2409
		// (get) Token: 0x06003F7B RID: 16251 RVA: 0x000EC499 File Offset: 0x000EA699
		[__DynamicallyInvokable]
		public bool CanBeCanceled
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_source != null && this.m_source.CanBeCanceled;
			}
		}

		// Token: 0x1700096A RID: 2410
		// (get) Token: 0x06003F7C RID: 16252 RVA: 0x000EC4B0 File Offset: 0x000EA6B0
		[__DynamicallyInvokable]
		public WaitHandle WaitHandle
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_source == null)
				{
					this.InitializeDefaultSource();
				}
				return this.m_source.WaitHandle;
			}
		}

		// Token: 0x06003F7D RID: 16253 RVA: 0x000EC4CB File Offset: 0x000EA6CB
		internal CancellationToken(CancellationTokenSource source)
		{
			this.m_source = source;
		}

		// Token: 0x06003F7E RID: 16254 RVA: 0x000EC4D4 File Offset: 0x000EA6D4
		[__DynamicallyInvokable]
		public CancellationToken(bool canceled)
		{
			this = default(CancellationToken);
			if (canceled)
			{
				this.m_source = CancellationTokenSource.InternalGetStaticSource(canceled);
			}
		}

		// Token: 0x06003F7F RID: 16255 RVA: 0x000EC4EC File Offset: 0x000EA6EC
		private static void ActionToActionObjShunt(object obj)
		{
			Action action = obj as Action;
			action();
		}

		// Token: 0x06003F80 RID: 16256 RVA: 0x000EC506 File Offset: 0x000EA706
		[__DynamicallyInvokable]
		public CancellationTokenRegistration Register(Action callback)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			return this.Register(CancellationToken.s_ActionToActionObjShunt, callback, false, true);
		}

		// Token: 0x06003F81 RID: 16257 RVA: 0x000EC524 File Offset: 0x000EA724
		[__DynamicallyInvokable]
		public CancellationTokenRegistration Register(Action callback, bool useSynchronizationContext)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			return this.Register(CancellationToken.s_ActionToActionObjShunt, callback, useSynchronizationContext, true);
		}

		// Token: 0x06003F82 RID: 16258 RVA: 0x000EC542 File Offset: 0x000EA742
		[__DynamicallyInvokable]
		public CancellationTokenRegistration Register(Action<object> callback, object state)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			return this.Register(callback, state, false, true);
		}

		// Token: 0x06003F83 RID: 16259 RVA: 0x000EC55C File Offset: 0x000EA75C
		[__DynamicallyInvokable]
		public CancellationTokenRegistration Register(Action<object> callback, object state, bool useSynchronizationContext)
		{
			return this.Register(callback, state, useSynchronizationContext, true);
		}

		// Token: 0x06003F84 RID: 16260 RVA: 0x000EC568 File Offset: 0x000EA768
		internal CancellationTokenRegistration InternalRegisterWithoutEC(Action<object> callback, object state)
		{
			return this.Register(callback, state, false, false);
		}

		// Token: 0x06003F85 RID: 16261 RVA: 0x000EC574 File Offset: 0x000EA774
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private CancellationTokenRegistration Register(Action<object> callback, object state, bool useSynchronizationContext, bool useExecutionContext)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}
			if (!this.CanBeCanceled)
			{
				return default(CancellationTokenRegistration);
			}
			SynchronizationContext targetSyncContext = null;
			ExecutionContext executionContext = null;
			if (!this.IsCancellationRequested)
			{
				if (useSynchronizationContext)
				{
					targetSyncContext = SynchronizationContext.Current;
				}
				if (useExecutionContext)
				{
					executionContext = ExecutionContext.Capture(ref stackCrawlMark, ExecutionContext.CaptureOptions.OptimizeDefaultCase);
				}
			}
			return this.m_source.InternalRegister(callback, state, targetSyncContext, executionContext);
		}

		// Token: 0x06003F86 RID: 16262 RVA: 0x000EC5D4 File Offset: 0x000EA7D4
		[__DynamicallyInvokable]
		public bool Equals(CancellationToken other)
		{
			if (this.m_source == null && other.m_source == null)
			{
				return true;
			}
			if (this.m_source == null)
			{
				return other.m_source == CancellationTokenSource.InternalGetStaticSource(false);
			}
			if (other.m_source == null)
			{
				return this.m_source == CancellationTokenSource.InternalGetStaticSource(false);
			}
			return this.m_source == other.m_source;
		}

		// Token: 0x06003F87 RID: 16263 RVA: 0x000EC62F File Offset: 0x000EA82F
		[__DynamicallyInvokable]
		public override bool Equals(object other)
		{
			return other is CancellationToken && this.Equals((CancellationToken)other);
		}

		// Token: 0x06003F88 RID: 16264 RVA: 0x000EC647 File Offset: 0x000EA847
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			if (this.m_source == null)
			{
				return CancellationTokenSource.InternalGetStaticSource(false).GetHashCode();
			}
			return this.m_source.GetHashCode();
		}

		// Token: 0x06003F89 RID: 16265 RVA: 0x000EC668 File Offset: 0x000EA868
		[__DynamicallyInvokable]
		public static bool operator ==(CancellationToken left, CancellationToken right)
		{
			return left.Equals(right);
		}

		// Token: 0x06003F8A RID: 16266 RVA: 0x000EC672 File Offset: 0x000EA872
		[__DynamicallyInvokable]
		public static bool operator !=(CancellationToken left, CancellationToken right)
		{
			return !left.Equals(right);
		}

		// Token: 0x06003F8B RID: 16267 RVA: 0x000EC67F File Offset: 0x000EA87F
		[__DynamicallyInvokable]
		public void ThrowIfCancellationRequested()
		{
			if (this.IsCancellationRequested)
			{
				this.ThrowOperationCanceledException();
			}
		}

		// Token: 0x06003F8C RID: 16268 RVA: 0x000EC68F File Offset: 0x000EA88F
		internal void ThrowIfSourceDisposed()
		{
			if (this.m_source != null && this.m_source.IsDisposed)
			{
				CancellationToken.ThrowObjectDisposedException();
			}
		}

		// Token: 0x06003F8D RID: 16269 RVA: 0x000EC6AB File Offset: 0x000EA8AB
		private void ThrowOperationCanceledException()
		{
			throw new OperationCanceledException(Environment.GetResourceString("OperationCanceled"), this);
		}

		// Token: 0x06003F8E RID: 16270 RVA: 0x000EC6C2 File Offset: 0x000EA8C2
		private static void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(null, Environment.GetResourceString("CancellationToken_SourceDisposed"));
		}

		// Token: 0x06003F8F RID: 16271 RVA: 0x000EC6D4 File Offset: 0x000EA8D4
		private void InitializeDefaultSource()
		{
			this.m_source = CancellationTokenSource.InternalGetStaticSource(false);
		}

		// Token: 0x04001ABD RID: 6845
		private CancellationTokenSource m_source;

		// Token: 0x04001ABE RID: 6846
		private static readonly Action<object> s_ActionToActionObjShunt = new Action<object>(CancellationToken.ActionToActionObjShunt);
	}
}
