using System;
using System.Collections;
using System.Reflection;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000867 RID: 2151
	internal class StackBasedReturnMessage : IMethodReturnMessage, IMethodMessage, IMessage, IInternalMessage
	{
		// Token: 0x06005B2D RID: 23341 RVA: 0x0013FDF8 File Offset: 0x0013DFF8
		internal StackBasedReturnMessage()
		{
		}

		// Token: 0x06005B2E RID: 23342 RVA: 0x0013FE00 File Offset: 0x0013E000
		internal void InitFields(Message m)
		{
			this._m = m;
			if (this._h != null)
			{
				this._h.Clear();
			}
			if (this._d != null)
			{
				this._d.Clear();
			}
		}

		// Token: 0x17000F64 RID: 3940
		// (get) Token: 0x06005B2F RID: 23343 RVA: 0x0013FE2F File Offset: 0x0013E02F
		public string Uri
		{
			[SecurityCritical]
			get
			{
				return this._m.Uri;
			}
		}

		// Token: 0x17000F65 RID: 3941
		// (get) Token: 0x06005B30 RID: 23344 RVA: 0x0013FE3C File Offset: 0x0013E03C
		public string MethodName
		{
			[SecurityCritical]
			get
			{
				return this._m.MethodName;
			}
		}

		// Token: 0x17000F66 RID: 3942
		// (get) Token: 0x06005B31 RID: 23345 RVA: 0x0013FE49 File Offset: 0x0013E049
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				return this._m.TypeName;
			}
		}

		// Token: 0x17000F67 RID: 3943
		// (get) Token: 0x06005B32 RID: 23346 RVA: 0x0013FE56 File Offset: 0x0013E056
		public object MethodSignature
		{
			[SecurityCritical]
			get
			{
				return this._m.MethodSignature;
			}
		}

		// Token: 0x17000F68 RID: 3944
		// (get) Token: 0x06005B33 RID: 23347 RVA: 0x0013FE63 File Offset: 0x0013E063
		public MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				return this._m.MethodBase;
			}
		}

		// Token: 0x17000F69 RID: 3945
		// (get) Token: 0x06005B34 RID: 23348 RVA: 0x0013FE70 File Offset: 0x0013E070
		public bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				return this._m.HasVarArgs;
			}
		}

		// Token: 0x17000F6A RID: 3946
		// (get) Token: 0x06005B35 RID: 23349 RVA: 0x0013FE7D File Offset: 0x0013E07D
		public int ArgCount
		{
			[SecurityCritical]
			get
			{
				return this._m.ArgCount;
			}
		}

		// Token: 0x06005B36 RID: 23350 RVA: 0x0013FE8A File Offset: 0x0013E08A
		[SecurityCritical]
		public object GetArg(int argNum)
		{
			return this._m.GetArg(argNum);
		}

		// Token: 0x06005B37 RID: 23351 RVA: 0x0013FE98 File Offset: 0x0013E098
		[SecurityCritical]
		public string GetArgName(int index)
		{
			return this._m.GetArgName(index);
		}

		// Token: 0x17000F6B RID: 3947
		// (get) Token: 0x06005B38 RID: 23352 RVA: 0x0013FEA6 File Offset: 0x0013E0A6
		public object[] Args
		{
			[SecurityCritical]
			get
			{
				return this._m.Args;
			}
		}

		// Token: 0x17000F6C RID: 3948
		// (get) Token: 0x06005B39 RID: 23353 RVA: 0x0013FEB3 File Offset: 0x0013E0B3
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this._m.GetLogicalCallContext();
			}
		}

		// Token: 0x06005B3A RID: 23354 RVA: 0x0013FEC0 File Offset: 0x0013E0C0
		[SecurityCritical]
		internal LogicalCallContext GetLogicalCallContext()
		{
			return this._m.GetLogicalCallContext();
		}

		// Token: 0x06005B3B RID: 23355 RVA: 0x0013FECD File Offset: 0x0013E0CD
		[SecurityCritical]
		internal LogicalCallContext SetLogicalCallContext(LogicalCallContext callCtx)
		{
			return this._m.SetLogicalCallContext(callCtx);
		}

		// Token: 0x17000F6D RID: 3949
		// (get) Token: 0x06005B3C RID: 23356 RVA: 0x0013FEDB File Offset: 0x0013E0DB
		public int OutArgCount
		{
			[SecurityCritical]
			get
			{
				if (this._argMapper == null)
				{
					this._argMapper = new ArgMapper(this, true);
				}
				return this._argMapper.ArgCount;
			}
		}

		// Token: 0x06005B3D RID: 23357 RVA: 0x0013FEFD File Offset: 0x0013E0FD
		[SecurityCritical]
		public object GetOutArg(int argNum)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, true);
			}
			return this._argMapper.GetArg(argNum);
		}

		// Token: 0x06005B3E RID: 23358 RVA: 0x0013FF20 File Offset: 0x0013E120
		[SecurityCritical]
		public string GetOutArgName(int index)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, true);
			}
			return this._argMapper.GetArgName(index);
		}

		// Token: 0x17000F6E RID: 3950
		// (get) Token: 0x06005B3F RID: 23359 RVA: 0x0013FF43 File Offset: 0x0013E143
		public object[] OutArgs
		{
			[SecurityCritical]
			get
			{
				if (this._argMapper == null)
				{
					this._argMapper = new ArgMapper(this, true);
				}
				return this._argMapper.Args;
			}
		}

		// Token: 0x17000F6F RID: 3951
		// (get) Token: 0x06005B40 RID: 23360 RVA: 0x0013FF65 File Offset: 0x0013E165
		public Exception Exception
		{
			[SecurityCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000F70 RID: 3952
		// (get) Token: 0x06005B41 RID: 23361 RVA: 0x0013FF68 File Offset: 0x0013E168
		public object ReturnValue
		{
			[SecurityCritical]
			get
			{
				return this._m.GetReturnValue();
			}
		}

		// Token: 0x17000F71 RID: 3953
		// (get) Token: 0x06005B42 RID: 23362 RVA: 0x0013FF78 File Offset: 0x0013E178
		public IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				IDictionary d;
				lock (this)
				{
					if (this._h == null)
					{
						this._h = new Hashtable();
					}
					if (this._d == null)
					{
						this._d = new MRMDictionary(this, this._h);
					}
					d = this._d;
				}
				return d;
			}
		}

		// Token: 0x17000F72 RID: 3954
		// (get) Token: 0x06005B43 RID: 23363 RVA: 0x0013FFE4 File Offset: 0x0013E1E4
		// (set) Token: 0x06005B44 RID: 23364 RVA: 0x0013FFE7 File Offset: 0x0013E1E7
		ServerIdentity IInternalMessage.ServerIdentityObject
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
			}
		}

		// Token: 0x17000F73 RID: 3955
		// (get) Token: 0x06005B45 RID: 23365 RVA: 0x0013FFE9 File Offset: 0x0013E1E9
		// (set) Token: 0x06005B46 RID: 23366 RVA: 0x0013FFEC File Offset: 0x0013E1EC
		Identity IInternalMessage.IdentityObject
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
			}
		}

		// Token: 0x06005B47 RID: 23367 RVA: 0x0013FFEE File Offset: 0x0013E1EE
		[SecurityCritical]
		void IInternalMessage.SetURI(string val)
		{
			this._m.Uri = val;
		}

		// Token: 0x06005B48 RID: 23368 RVA: 0x0013FFFC File Offset: 0x0013E1FC
		[SecurityCritical]
		void IInternalMessage.SetCallContext(LogicalCallContext newCallContext)
		{
			this._m.SetLogicalCallContext(newCallContext);
		}

		// Token: 0x06005B49 RID: 23369 RVA: 0x0014000B File Offset: 0x0013E20B
		[SecurityCritical]
		bool IInternalMessage.HasProperties()
		{
			return this._h != null;
		}

		// Token: 0x0400294A RID: 10570
		private Message _m;

		// Token: 0x0400294B RID: 10571
		private Hashtable _h;

		// Token: 0x0400294C RID: 10572
		private MRMDictionary _d;

		// Token: 0x0400294D RID: 10573
		private ArgMapper _argMapper;
	}
}
