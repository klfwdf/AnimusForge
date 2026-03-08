using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x020007A6 RID: 1958
	[Serializable]
	internal sealed class BinaryMethodCallMessage
	{
		// Token: 0x060054F1 RID: 21745 RVA: 0x0012E140 File Offset: 0x0012C340
		[SecurityCritical]
		internal BinaryMethodCallMessage(string uri, string methodName, string typeName, Type[] instArgs, object[] args, object methodSignature, LogicalCallContext callContext, object[] properties)
		{
			this._methodName = methodName;
			this._typeName = typeName;
			if (args == null)
			{
				args = new object[0];
			}
			this._inargs = args;
			this._args = args;
			this._instArgs = instArgs;
			this._methodSignature = methodSignature;
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

		// Token: 0x17000DE3 RID: 3555
		// (get) Token: 0x060054F2 RID: 21746 RVA: 0x0012E1AE File Offset: 0x0012C3AE
		public string MethodName
		{
			get
			{
				return this._methodName;
			}
		}

		// Token: 0x17000DE4 RID: 3556
		// (get) Token: 0x060054F3 RID: 21747 RVA: 0x0012E1B6 File Offset: 0x0012C3B6
		public string TypeName
		{
			get
			{
				return this._typeName;
			}
		}

		// Token: 0x17000DE5 RID: 3557
		// (get) Token: 0x060054F4 RID: 21748 RVA: 0x0012E1BE File Offset: 0x0012C3BE
		public Type[] InstantiationArgs
		{
			get
			{
				return this._instArgs;
			}
		}

		// Token: 0x17000DE6 RID: 3558
		// (get) Token: 0x060054F5 RID: 21749 RVA: 0x0012E1C6 File Offset: 0x0012C3C6
		public object MethodSignature
		{
			get
			{
				return this._methodSignature;
			}
		}

		// Token: 0x17000DE7 RID: 3559
		// (get) Token: 0x060054F6 RID: 21750 RVA: 0x0012E1CE File Offset: 0x0012C3CE
		public object[] Args
		{
			get
			{
				return this._args;
			}
		}

		// Token: 0x17000DE8 RID: 3560
		// (get) Token: 0x060054F7 RID: 21751 RVA: 0x0012E1D6 File Offset: 0x0012C3D6
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this._logicalCallContext;
			}
		}

		// Token: 0x17000DE9 RID: 3561
		// (get) Token: 0x060054F8 RID: 21752 RVA: 0x0012E1DE File Offset: 0x0012C3DE
		public bool HasProperties
		{
			get
			{
				return this._properties != null;
			}
		}

		// Token: 0x060054F9 RID: 21753 RVA: 0x0012E1EC File Offset: 0x0012C3EC
		internal void PopulateMessageProperties(IDictionary dict)
		{
			foreach (DictionaryEntry dictionaryEntry in this._properties)
			{
				dict[dictionaryEntry.Key] = dictionaryEntry.Value;
			}
		}

		// Token: 0x04002712 RID: 10002
		private object[] _inargs;

		// Token: 0x04002713 RID: 10003
		private string _methodName;

		// Token: 0x04002714 RID: 10004
		private string _typeName;

		// Token: 0x04002715 RID: 10005
		private object _methodSignature;

		// Token: 0x04002716 RID: 10006
		private Type[] _instArgs;

		// Token: 0x04002717 RID: 10007
		private object[] _args;

		// Token: 0x04002718 RID: 10008
		[SecurityCritical]
		private LogicalCallContext _logicalCallContext;

		// Token: 0x04002719 RID: 10009
		private object[] _properties;
	}
}
