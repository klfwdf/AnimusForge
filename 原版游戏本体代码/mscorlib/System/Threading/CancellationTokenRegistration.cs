using System;
using System.Runtime.CompilerServices;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000544 RID: 1348
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public struct CancellationTokenRegistration : IEquatable<CancellationTokenRegistration>, IDisposable
	{
		// Token: 0x06003F3F RID: 16191 RVA: 0x000EB788 File Offset: 0x000E9988
		internal CancellationTokenRegistration(CancellationCallbackInfo callbackInfo, SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> registrationInfo)
		{
			this.m_callbackInfo = callbackInfo;
			this.m_registrationInfo = registrationInfo;
		}

		// Token: 0x06003F40 RID: 16192 RVA: 0x000EB798 File Offset: 0x000E9998
		[FriendAccessAllowed]
		internal bool TryDeregister()
		{
			if (this.m_registrationInfo.Source == null)
			{
				return false;
			}
			CancellationCallbackInfo cancellationCallbackInfo = this.m_registrationInfo.Source.SafeAtomicRemove(this.m_registrationInfo.Index, this.m_callbackInfo);
			return cancellationCallbackInfo == this.m_callbackInfo;
		}

		// Token: 0x06003F41 RID: 16193 RVA: 0x000EB7EC File Offset: 0x000E99EC
		[__DynamicallyInvokable]
		public void Dispose()
		{
			bool flag = this.TryDeregister();
			CancellationCallbackInfo callbackInfo = this.m_callbackInfo;
			if (callbackInfo != null)
			{
				CancellationTokenSource cancellationTokenSource = callbackInfo.CancellationTokenSource;
				if (cancellationTokenSource.IsCancellationRequested && !cancellationTokenSource.IsCancellationCompleted && !flag && cancellationTokenSource.ThreadIDExecutingCallbacks != Thread.CurrentThread.ManagedThreadId)
				{
					cancellationTokenSource.WaitForCallbackToComplete(this.m_callbackInfo);
				}
			}
		}

		// Token: 0x06003F42 RID: 16194 RVA: 0x000EB842 File Offset: 0x000E9A42
		[__DynamicallyInvokable]
		public static bool operator ==(CancellationTokenRegistration left, CancellationTokenRegistration right)
		{
			return left.Equals(right);
		}

		// Token: 0x06003F43 RID: 16195 RVA: 0x000EB84C File Offset: 0x000E9A4C
		[__DynamicallyInvokable]
		public static bool operator !=(CancellationTokenRegistration left, CancellationTokenRegistration right)
		{
			return !left.Equals(right);
		}

		// Token: 0x06003F44 RID: 16196 RVA: 0x000EB859 File Offset: 0x000E9A59
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is CancellationTokenRegistration && this.Equals((CancellationTokenRegistration)obj);
		}

		// Token: 0x06003F45 RID: 16197 RVA: 0x000EB874 File Offset: 0x000E9A74
		[__DynamicallyInvokable]
		public bool Equals(CancellationTokenRegistration other)
		{
			return this.m_callbackInfo == other.m_callbackInfo && this.m_registrationInfo.Source == other.m_registrationInfo.Source && this.m_registrationInfo.Index == other.m_registrationInfo.Index;
		}

		// Token: 0x06003F46 RID: 16198 RVA: 0x000EB8D0 File Offset: 0x000E9AD0
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			if (this.m_registrationInfo.Source != null)
			{
				return this.m_registrationInfo.Source.GetHashCode() ^ this.m_registrationInfo.Index.GetHashCode();
			}
			return this.m_registrationInfo.Index.GetHashCode();
		}

		// Token: 0x04001A9A RID: 6810
		private readonly CancellationCallbackInfo m_callbackInfo;

		// Token: 0x04001A9B RID: 6811
		private readonly SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> m_registrationInfo;
	}
}
