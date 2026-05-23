using System;

namespace System.Runtime.ExceptionServices
{
	// Token: 0x020007AA RID: 1962
	[__DynamicallyInvokable]
	public sealed class ExceptionDispatchInfo
	{
		// Token: 0x06005504 RID: 21764 RVA: 0x0012E314 File Offset: 0x0012C514
		private ExceptionDispatchInfo(Exception exception)
		{
			this.m_Exception = exception;
			this.m_remoteStackTrace = exception.RemoteStackTrace;
			object stackTrace;
			object dynamicMethods;
			this.m_Exception.GetStackTracesDeepCopy(out stackTrace, out dynamicMethods);
			this.m_stackTrace = stackTrace;
			this.m_dynamicMethods = dynamicMethods;
			this.m_IPForWatsonBuckets = exception.IPForWatsonBuckets;
			this.m_WatsonBuckets = exception.WatsonBuckets;
		}

		// Token: 0x17000DF0 RID: 3568
		// (get) Token: 0x06005505 RID: 21765 RVA: 0x0012E36F File Offset: 0x0012C56F
		internal UIntPtr IPForWatsonBuckets
		{
			get
			{
				return this.m_IPForWatsonBuckets;
			}
		}

		// Token: 0x17000DF1 RID: 3569
		// (get) Token: 0x06005506 RID: 21766 RVA: 0x0012E377 File Offset: 0x0012C577
		internal object WatsonBuckets
		{
			get
			{
				return this.m_WatsonBuckets;
			}
		}

		// Token: 0x17000DF2 RID: 3570
		// (get) Token: 0x06005507 RID: 21767 RVA: 0x0012E37F File Offset: 0x0012C57F
		internal object BinaryStackTraceArray
		{
			get
			{
				return this.m_stackTrace;
			}
		}

		// Token: 0x17000DF3 RID: 3571
		// (get) Token: 0x06005508 RID: 21768 RVA: 0x0012E387 File Offset: 0x0012C587
		internal object DynamicMethodArray
		{
			get
			{
				return this.m_dynamicMethods;
			}
		}

		// Token: 0x17000DF4 RID: 3572
		// (get) Token: 0x06005509 RID: 21769 RVA: 0x0012E38F File Offset: 0x0012C58F
		internal string RemoteStackTrace
		{
			get
			{
				return this.m_remoteStackTrace;
			}
		}

		// Token: 0x0600550A RID: 21770 RVA: 0x0012E397 File Offset: 0x0012C597
		[__DynamicallyInvokable]
		public static ExceptionDispatchInfo Capture(Exception source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source", Environment.GetResourceString("ArgumentNull_Obj"));
			}
			return new ExceptionDispatchInfo(source);
		}

		// Token: 0x17000DF5 RID: 3573
		// (get) Token: 0x0600550B RID: 21771 RVA: 0x0012E3B7 File Offset: 0x0012C5B7
		[__DynamicallyInvokable]
		public Exception SourceException
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Exception;
			}
		}

		// Token: 0x0600550C RID: 21772 RVA: 0x0012E3BF File Offset: 0x0012C5BF
		[__DynamicallyInvokable]
		public void Throw()
		{
			this.m_Exception.RestoreExceptionDispatchInfo(this);
			throw this.m_Exception;
		}

		// Token: 0x04002721 RID: 10017
		private Exception m_Exception;

		// Token: 0x04002722 RID: 10018
		private string m_remoteStackTrace;

		// Token: 0x04002723 RID: 10019
		private object m_stackTrace;

		// Token: 0x04002724 RID: 10020
		private object m_dynamicMethods;

		// Token: 0x04002725 RID: 10021
		private UIntPtr m_IPForWatsonBuckets;

		// Token: 0x04002726 RID: 10022
		private object m_WatsonBuckets;
	}
}
