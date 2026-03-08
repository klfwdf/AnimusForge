using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x020007A7 RID: 1959
	[Serializable]
	internal class BinaryMethodReturnMessage
	{
		// Token: 0x060054FA RID: 21754 RVA: 0x0012E22C File Offset: 0x0012C42C
		[SecurityCritical]
		internal BinaryMethodReturnMessage(object returnValue, object[] args, Exception e, LogicalCallContext callContext, object[] properties)
		{
			this._returnValue = returnValue;
			if (args == null)
			{
				args = new object[0];
			}
			this._outargs = args;
			this._args = args;
			this._exception = e;
			if (callContext == null)
			{
				this._logicalCallContext = new LogicalCallContext();
			}
			else
			{
				this._logicalCallContext = callContext;
			}
			this._properties = properties;
		}

		// Token: 0x17000DEA RID: 3562
		// (get) Token: 0x060054FB RID: 21755 RVA: 0x0012E287 File Offset: 0x0012C487
		public Exception Exception
		{
			get
			{
				return this._exception;
			}
		}

		// Token: 0x17000DEB RID: 3563
		// (get) Token: 0x060054FC RID: 21756 RVA: 0x0012E28F File Offset: 0x0012C48F
		public object ReturnValue
		{
			get
			{
				return this._returnValue;
			}
		}

		// Token: 0x17000DEC RID: 3564
		// (get) Token: 0x060054FD RID: 21757 RVA: 0x0012E297 File Offset: 0x0012C497
		public object[] Args
		{
			get
			{
				return this._args;
			}
		}

		// Token: 0x17000DED RID: 3565
		// (get) Token: 0x060054FE RID: 21758 RVA: 0x0012E29F File Offset: 0x0012C49F
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this._logicalCallContext;
			}
		}

		// Token: 0x17000DEE RID: 3566
		// (get) Token: 0x060054FF RID: 21759 RVA: 0x0012E2A7 File Offset: 0x0012C4A7
		public bool HasProperties
		{
			get
			{
				return this._properties != null;
			}
		}

		// Token: 0x06005500 RID: 21760 RVA: 0x0012E2B4 File Offset: 0x0012C4B4
		internal void PopulateMessageProperties(IDictionary dict)
		{
			foreach (DictionaryEntry dictionaryEntry in this._properties)
			{
				dict[dictionaryEntry.Key] = dictionaryEntry.Value;
			}
		}

		// Token: 0x0400271A RID: 10010
		private object[] _outargs;

		// Token: 0x0400271B RID: 10011
		private Exception _exception;

		// Token: 0x0400271C RID: 10012
		private object _returnValue;

		// Token: 0x0400271D RID: 10013
		private object[] _args;

		// Token: 0x0400271E RID: 10014
		[SecurityCritical]
		private LogicalCallContext _logicalCallContext;

		// Token: 0x0400271F RID: 10015
		private object[] _properties;
	}
}
