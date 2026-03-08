using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000874 RID: 2164
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public class MethodReturnMessageWrapper : InternalMessageWrapper, IMethodReturnMessage, IMethodMessage, IMessage
	{
		// Token: 0x06005C12 RID: 23570 RVA: 0x00142E58 File Offset: 0x00141058
		public MethodReturnMessageWrapper(IMethodReturnMessage msg)
			: base(msg)
		{
			this._msg = msg;
			this._args = this._msg.Args;
			this._returnValue = this._msg.ReturnValue;
			this._exception = this._msg.Exception;
		}

		// Token: 0x17000FCA RID: 4042
		// (get) Token: 0x06005C13 RID: 23571 RVA: 0x00142EA6 File Offset: 0x001410A6
		// (set) Token: 0x06005C14 RID: 23572 RVA: 0x00142EB3 File Offset: 0x001410B3
		public string Uri
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

		// Token: 0x17000FCB RID: 4043
		// (get) Token: 0x06005C15 RID: 23573 RVA: 0x00142ECB File Offset: 0x001410CB
		public virtual string MethodName
		{
			[SecurityCritical]
			get
			{
				return this._msg.MethodName;
			}
		}

		// Token: 0x17000FCC RID: 4044
		// (get) Token: 0x06005C16 RID: 23574 RVA: 0x00142ED8 File Offset: 0x001410D8
		public virtual string TypeName
		{
			[SecurityCritical]
			get
			{
				return this._msg.TypeName;
			}
		}

		// Token: 0x17000FCD RID: 4045
		// (get) Token: 0x06005C17 RID: 23575 RVA: 0x00142EE5 File Offset: 0x001410E5
		public virtual object MethodSignature
		{
			[SecurityCritical]
			get
			{
				return this._msg.MethodSignature;
			}
		}

		// Token: 0x17000FCE RID: 4046
		// (get) Token: 0x06005C18 RID: 23576 RVA: 0x00142EF2 File Offset: 0x001410F2
		public virtual LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this._msg.LogicalCallContext;
			}
		}

		// Token: 0x17000FCF RID: 4047
		// (get) Token: 0x06005C19 RID: 23577 RVA: 0x00142EFF File Offset: 0x001410FF
		public virtual MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				return this._msg.MethodBase;
			}
		}

		// Token: 0x17000FD0 RID: 4048
		// (get) Token: 0x06005C1A RID: 23578 RVA: 0x00142F0C File Offset: 0x0014110C
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

		// Token: 0x06005C1B RID: 23579 RVA: 0x00142F20 File Offset: 0x00141120
		[SecurityCritical]
		public virtual string GetArgName(int index)
		{
			return this._msg.GetArgName(index);
		}

		// Token: 0x06005C1C RID: 23580 RVA: 0x00142F2E File Offset: 0x0014112E
		[SecurityCritical]
		public virtual object GetArg(int argNum)
		{
			return this._args[argNum];
		}

		// Token: 0x17000FD1 RID: 4049
		// (get) Token: 0x06005C1D RID: 23581 RVA: 0x00142F38 File Offset: 0x00141138
		// (set) Token: 0x06005C1E RID: 23582 RVA: 0x00142F40 File Offset: 0x00141140
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

		// Token: 0x17000FD2 RID: 4050
		// (get) Token: 0x06005C1F RID: 23583 RVA: 0x00142F49 File Offset: 0x00141149
		public virtual bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				return this._msg.HasVarArgs;
			}
		}

		// Token: 0x17000FD3 RID: 4051
		// (get) Token: 0x06005C20 RID: 23584 RVA: 0x00142F56 File Offset: 0x00141156
		public virtual int OutArgCount
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

		// Token: 0x06005C21 RID: 23585 RVA: 0x00142F78 File Offset: 0x00141178
		[SecurityCritical]
		public virtual object GetOutArg(int argNum)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, true);
			}
			return this._argMapper.GetArg(argNum);
		}

		// Token: 0x06005C22 RID: 23586 RVA: 0x00142F9B File Offset: 0x0014119B
		[SecurityCritical]
		public virtual string GetOutArgName(int index)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, true);
			}
			return this._argMapper.GetArgName(index);
		}

		// Token: 0x17000FD4 RID: 4052
		// (get) Token: 0x06005C23 RID: 23587 RVA: 0x00142FBE File Offset: 0x001411BE
		public virtual object[] OutArgs
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

		// Token: 0x17000FD5 RID: 4053
		// (get) Token: 0x06005C24 RID: 23588 RVA: 0x00142FE0 File Offset: 0x001411E0
		// (set) Token: 0x06005C25 RID: 23589 RVA: 0x00142FE8 File Offset: 0x001411E8
		public virtual Exception Exception
		{
			[SecurityCritical]
			get
			{
				return this._exception;
			}
			set
			{
				this._exception = value;
			}
		}

		// Token: 0x17000FD6 RID: 4054
		// (get) Token: 0x06005C26 RID: 23590 RVA: 0x00142FF1 File Offset: 0x001411F1
		// (set) Token: 0x06005C27 RID: 23591 RVA: 0x00142FF9 File Offset: 0x001411F9
		public virtual object ReturnValue
		{
			[SecurityCritical]
			get
			{
				return this._returnValue;
			}
			set
			{
				this._returnValue = value;
			}
		}

		// Token: 0x17000FD7 RID: 4055
		// (get) Token: 0x06005C28 RID: 23592 RVA: 0x00143002 File Offset: 0x00141202
		public virtual IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				if (this._properties == null)
				{
					this._properties = new MethodReturnMessageWrapper.MRMWrapperDictionary(this, this._msg.Properties);
				}
				return this._properties;
			}
		}

		// Token: 0x04002999 RID: 10649
		private IMethodReturnMessage _msg;

		// Token: 0x0400299A RID: 10650
		private IDictionary _properties;

		// Token: 0x0400299B RID: 10651
		private ArgMapper _argMapper;

		// Token: 0x0400299C RID: 10652
		private object[] _args;

		// Token: 0x0400299D RID: 10653
		private object _returnValue;

		// Token: 0x0400299E RID: 10654
		private Exception _exception;

		// Token: 0x02000C7A RID: 3194
		private class MRMWrapperDictionary : Hashtable
		{
			// Token: 0x060070BD RID: 28861 RVA: 0x00184B70 File Offset: 0x00182D70
			public MRMWrapperDictionary(IMethodReturnMessage msg, IDictionary idict)
			{
				this._mrmsg = msg;
				this._idict = idict;
			}

			// Token: 0x17001354 RID: 4948
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
							return this._mrmsg.Uri;
						}
						if (text == "__MethodName")
						{
							return this._mrmsg.MethodName;
						}
						if (text == "__MethodSignature")
						{
							return this._mrmsg.MethodSignature;
						}
						if (text == "__TypeName")
						{
							return this._mrmsg.TypeName;
						}
						if (text == "__Return")
						{
							return this._mrmsg.ReturnValue;
						}
						if (text == "__OutArgs")
						{
							return this._mrmsg.OutArgs;
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
						if (text == "__MethodName" || text == "__MethodSignature" || text == "__TypeName" || text == "__Return" || text == "__OutArgs")
						{
							throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
						}
						this._idict[key] = value;
					}
				}
			}

			// Token: 0x0400380B RID: 14347
			private IMethodReturnMessage _mrmsg;

			// Token: 0x0400380C RID: 14348
			private IDictionary _idict;
		}
	}
}
