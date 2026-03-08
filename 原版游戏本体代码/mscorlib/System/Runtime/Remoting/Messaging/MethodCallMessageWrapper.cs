using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000873 RID: 2163
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public class MethodCallMessageWrapper : InternalMessageWrapper, IMethodCallMessage, IMethodMessage, IMessage
	{
		// Token: 0x06005BFF RID: 23551 RVA: 0x00142CD6 File Offset: 0x00140ED6
		public MethodCallMessageWrapper(IMethodCallMessage msg)
			: base(msg)
		{
			this._msg = msg;
			this._args = this._msg.Args;
		}

		// Token: 0x17000FBE RID: 4030
		// (get) Token: 0x06005C00 RID: 23552 RVA: 0x00142CF7 File Offset: 0x00140EF7
		// (set) Token: 0x06005C01 RID: 23553 RVA: 0x00142D04 File Offset: 0x00140F04
		public virtual string Uri
		{
			[SecurityCritical]
			get
			{
				return this._msg.Uri;
			}
			set
			{
				this._msg.Properties[Message.UriKey] = value;
			}
		}

		// Token: 0x17000FBF RID: 4031
		// (get) Token: 0x06005C02 RID: 23554 RVA: 0x00142D1C File Offset: 0x00140F1C
		public virtual string MethodName
		{
			[SecurityCritical]
			get
			{
				return this._msg.MethodName;
			}
		}

		// Token: 0x17000FC0 RID: 4032
		// (get) Token: 0x06005C03 RID: 23555 RVA: 0x00142D29 File Offset: 0x00140F29
		public virtual string TypeName
		{
			[SecurityCritical]
			get
			{
				return this._msg.TypeName;
			}
		}

		// Token: 0x17000FC1 RID: 4033
		// (get) Token: 0x06005C04 RID: 23556 RVA: 0x00142D36 File Offset: 0x00140F36
		public virtual object MethodSignature
		{
			[SecurityCritical]
			get
			{
				return this._msg.MethodSignature;
			}
		}

		// Token: 0x17000FC2 RID: 4034
		// (get) Token: 0x06005C05 RID: 23557 RVA: 0x00142D43 File Offset: 0x00140F43
		public virtual LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this._msg.LogicalCallContext;
			}
		}

		// Token: 0x17000FC3 RID: 4035
		// (get) Token: 0x06005C06 RID: 23558 RVA: 0x00142D50 File Offset: 0x00140F50
		public virtual MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				return this._msg.MethodBase;
			}
		}

		// Token: 0x17000FC4 RID: 4036
		// (get) Token: 0x06005C07 RID: 23559 RVA: 0x00142D5D File Offset: 0x00140F5D
		public virtual int ArgCount
		{
			[SecurityCritical]
			get
			{
				if (this._args != null)
				{
					return this._args.Length;
				}
				return 0;
			}
		}

		// Token: 0x06005C08 RID: 23560 RVA: 0x00142D71 File Offset: 0x00140F71
		[SecurityCritical]
		public virtual string GetArgName(int index)
		{
			return this._msg.GetArgName(index);
		}

		// Token: 0x06005C09 RID: 23561 RVA: 0x00142D7F File Offset: 0x00140F7F
		[SecurityCritical]
		public virtual object GetArg(int argNum)
		{
			return this._args[argNum];
		}

		// Token: 0x17000FC5 RID: 4037
		// (get) Token: 0x06005C0A RID: 23562 RVA: 0x00142D89 File Offset: 0x00140F89
		// (set) Token: 0x06005C0B RID: 23563 RVA: 0x00142D91 File Offset: 0x00140F91
		public virtual object[] Args
		{
			[SecurityCritical]
			get
			{
				return this._args;
			}
			set
			{
				this._args = value;
			}
		}

		// Token: 0x17000FC6 RID: 4038
		// (get) Token: 0x06005C0C RID: 23564 RVA: 0x00142D9A File Offset: 0x00140F9A
		public virtual bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				return this._msg.HasVarArgs;
			}
		}

		// Token: 0x17000FC7 RID: 4039
		// (get) Token: 0x06005C0D RID: 23565 RVA: 0x00142DA7 File Offset: 0x00140FA7
		public virtual int InArgCount
		{
			[SecurityCritical]
			get
			{
				if (this._argMapper == null)
				{
					this._argMapper = new ArgMapper(this, false);
				}
				return this._argMapper.ArgCount;
			}
		}

		// Token: 0x06005C0E RID: 23566 RVA: 0x00142DC9 File Offset: 0x00140FC9
		[SecurityCritical]
		public virtual object GetInArg(int argNum)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, false);
			}
			return this._argMapper.GetArg(argNum);
		}

		// Token: 0x06005C0F RID: 23567 RVA: 0x00142DEC File Offset: 0x00140FEC
		[SecurityCritical]
		public virtual string GetInArgName(int index)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, false);
			}
			return this._argMapper.GetArgName(index);
		}

		// Token: 0x17000FC8 RID: 4040
		// (get) Token: 0x06005C10 RID: 23568 RVA: 0x00142E0F File Offset: 0x0014100F
		public virtual object[] InArgs
		{
			[SecurityCritical]
			get
			{
				if (this._argMapper == null)
				{
					this._argMapper = new ArgMapper(this, false);
				}
				return this._argMapper.Args;
			}
		}

		// Token: 0x17000FC9 RID: 4041
		// (get) Token: 0x06005C11 RID: 23569 RVA: 0x00142E31 File Offset: 0x00141031
		public virtual IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				if (this._properties == null)
				{
					this._properties = new MethodCallMessageWrapper.MCMWrapperDictionary(this, this._msg.Properties);
				}
				return this._properties;
			}
		}

		// Token: 0x04002995 RID: 10645
		private IMethodCallMessage _msg;

		// Token: 0x04002996 RID: 10646
		private IDictionary _properties;

		// Token: 0x04002997 RID: 10647
		private ArgMapper _argMapper;

		// Token: 0x04002998 RID: 10648
		private object[] _args;

		// Token: 0x02000C79 RID: 3193
		private class MCMWrapperDictionary : Hashtable
		{
			// Token: 0x060070BA RID: 28858 RVA: 0x00184A4B File Offset: 0x00182C4B
			public MCMWrapperDictionary(IMethodCallMessage msg, IDictionary idict)
			{
				this._mcmsg = msg;
				this._idict = idict;
			}

			// Token: 0x17001353 RID: 4947
			public override object this[object key]
			{
				[SecuritySafeCritical]
				get
				{
					string text = key as string;
					if (text != null)
					{
						if (text == "__Uri")
						{
							return this._mcmsg.Uri;
						}
						if (text == "__MethodName")
						{
							return this._mcmsg.MethodName;
						}
						if (text == "__MethodSignature")
						{
							return this._mcmsg.MethodSignature;
						}
						if (text == "__TypeName")
						{
							return this._mcmsg.TypeName;
						}
						if (text == "__Args")
						{
							return this._mcmsg.Args;
						}
					}
					return this._idict[key];
				}
				[SecuritySafeCritical]
				set
				{
					string text = key as string;
					if (text != null)
					{
						if (text == "__MethodName" || text == "__MethodSignature" || text == "__TypeName" || text == "__Args")
						{
							throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
						}
						this._idict[key] = value;
					}
				}
			}

			// Token: 0x04003809 RID: 14345
			private IMethodCallMessage _mcmsg;

			// Token: 0x0400380A RID: 14346
			private IDictionary _idict;
		}
	}
}
