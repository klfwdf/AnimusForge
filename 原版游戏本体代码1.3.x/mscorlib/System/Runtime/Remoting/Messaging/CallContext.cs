using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200088B RID: 2187
	[SecurityCritical]
	[ComVisible(true)]
	[Serializable]
	public sealed class CallContext
	{
		// Token: 0x06005CAC RID: 23724 RVA: 0x00145094 File Offset: 0x00143294
		private CallContext()
		{
		}

		// Token: 0x06005CAD RID: 23725 RVA: 0x0014509C File Offset: 0x0014329C
		internal static LogicalCallContext SetLogicalCallContext(LogicalCallContext callCtx)
		{
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			LogicalCallContext logicalCallContext = mutableExecutionContext.LogicalCallContext;
			mutableExecutionContext.LogicalCallContext = callCtx;
			return logicalCallContext;
		}

		// Token: 0x06005CAE RID: 23726 RVA: 0x001450C4 File Offset: 0x001432C4
		[SecurityCritical]
		public static void FreeNamedDataSlot(string name)
		{
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			mutableExecutionContext.LogicalCallContext.FreeNamedDataSlot(name);
			mutableExecutionContext.IllogicalCallContext.FreeNamedDataSlot(name);
		}

		// Token: 0x06005CAF RID: 23727 RVA: 0x001450F4 File Offset: 0x001432F4
		[SecurityCritical]
		public static object LogicalGetData(string name)
		{
			return Thread.CurrentThread.GetExecutionContextReader().LogicalCallContext.GetData(name);
		}

		// Token: 0x06005CB0 RID: 23728 RVA: 0x0014511C File Offset: 0x0014331C
		private static object IllogicalGetData(string name)
		{
			return Thread.CurrentThread.GetExecutionContextReader().IllogicalCallContext.GetData(name);
		}

		// Token: 0x17000FEB RID: 4075
		// (get) Token: 0x06005CB1 RID: 23729 RVA: 0x00145144 File Offset: 0x00143344
		// (set) Token: 0x06005CB2 RID: 23730 RVA: 0x0014516B File Offset: 0x0014336B
		internal static IPrincipal Principal
		{
			[SecurityCritical]
			get
			{
				return Thread.CurrentThread.GetExecutionContextReader().LogicalCallContext.Principal;
			}
			[SecurityCritical]
			set
			{
				Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext.Principal = value;
			}
		}

		// Token: 0x17000FEC RID: 4076
		// (get) Token: 0x06005CB3 RID: 23731 RVA: 0x00145184 File Offset: 0x00143384
		// (set) Token: 0x06005CB4 RID: 23732 RVA: 0x001451C0 File Offset: 0x001433C0
		public static object HostContext
		{
			[SecurityCritical]
			get
			{
				ExecutionContext.Reader executionContextReader = Thread.CurrentThread.GetExecutionContextReader();
				object hostContext = executionContextReader.IllogicalCallContext.HostContext;
				if (hostContext == null)
				{
					hostContext = executionContextReader.LogicalCallContext.HostContext;
				}
				return hostContext;
			}
			[SecurityCritical]
			set
			{
				ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
				if (value is ILogicalThreadAffinative)
				{
					mutableExecutionContext.IllogicalCallContext.HostContext = null;
					mutableExecutionContext.LogicalCallContext.HostContext = value;
					return;
				}
				mutableExecutionContext.IllogicalCallContext.HostContext = value;
				mutableExecutionContext.LogicalCallContext.HostContext = null;
			}
		}

		// Token: 0x06005CB5 RID: 23733 RVA: 0x00145214 File Offset: 0x00143414
		[SecurityCritical]
		public static object GetData(string name)
		{
			object obj = CallContext.LogicalGetData(name);
			if (obj == null)
			{
				return CallContext.IllogicalGetData(name);
			}
			return obj;
		}

		// Token: 0x06005CB6 RID: 23734 RVA: 0x00145234 File Offset: 0x00143434
		[SecurityCritical]
		public static void SetData(string name, object data)
		{
			if (data is ILogicalThreadAffinative)
			{
				CallContext.LogicalSetData(name, data);
				return;
			}
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			mutableExecutionContext.LogicalCallContext.FreeNamedDataSlot(name);
			mutableExecutionContext.IllogicalCallContext.SetData(name, data);
		}

		// Token: 0x06005CB7 RID: 23735 RVA: 0x00145278 File Offset: 0x00143478
		[SecurityCritical]
		public static void LogicalSetData(string name, object data)
		{
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			mutableExecutionContext.IllogicalCallContext.FreeNamedDataSlot(name);
			mutableExecutionContext.LogicalCallContext.SetData(name, data);
		}

		// Token: 0x06005CB8 RID: 23736 RVA: 0x001452AC File Offset: 0x001434AC
		[SecurityCritical]
		public static Header[] GetHeaders()
		{
			LogicalCallContext logicalCallContext = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext;
			return logicalCallContext.InternalGetHeaders();
		}

		// Token: 0x06005CB9 RID: 23737 RVA: 0x001452D0 File Offset: 0x001434D0
		[SecurityCritical]
		public static void SetHeaders(Header[] headers)
		{
			LogicalCallContext logicalCallContext = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext;
			logicalCallContext.InternalSetHeaders(headers);
		}
	}
}
