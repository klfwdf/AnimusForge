using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Proxies;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000860 RID: 2144
	internal class ConstructorCallMessage : IConstructionCallMessage, IMethodCallMessage, IMethodMessage, IMessage
	{
		// Token: 0x06005ADA RID: 23258 RVA: 0x0013ED0A File Offset: 0x0013CF0A
		private ConstructorCallMessage()
		{
		}

		// Token: 0x06005ADB RID: 23259 RVA: 0x0013ED12 File Offset: 0x0013CF12
		[SecurityCritical]
		internal ConstructorCallMessage(object[] callSiteActivationAttributes, object[] womAttr, object[] typeAttr, RuntimeType serverType)
		{
			this._activationType = serverType;
			this._activationTypeName = RemotingServices.GetDefaultQualifiedTypeName(this._activationType);
			this._callSiteActivationAttributes = callSiteActivationAttributes;
			this._womGlobalAttributes = womAttr;
			this._typeAttributes = typeAttr;
		}

		// Token: 0x06005ADC RID: 23260 RVA: 0x0013ED48 File Offset: 0x0013CF48
		[SecurityCritical]
		public object GetThisPtr()
		{
			if (this._message != null)
			{
				return this._message.GetThisPtr();
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
		}

		// Token: 0x17000F45 RID: 3909
		// (get) Token: 0x06005ADD RID: 23261 RVA: 0x0013ED6D File Offset: 0x0013CF6D
		public object[] CallSiteActivationAttributes
		{
			[SecurityCritical]
			get
			{
				return this._callSiteActivationAttributes;
			}
		}

		// Token: 0x06005ADE RID: 23262 RVA: 0x0013ED75 File Offset: 0x0013CF75
		internal object[] GetWOMAttributes()
		{
			return this._womGlobalAttributes;
		}

		// Token: 0x06005ADF RID: 23263 RVA: 0x0013ED7D File Offset: 0x0013CF7D
		internal object[] GetTypeAttributes()
		{
			return this._typeAttributes;
		}

		// Token: 0x17000F46 RID: 3910
		// (get) Token: 0x06005AE0 RID: 23264 RVA: 0x0013ED85 File Offset: 0x0013CF85
		public Type ActivationType
		{
			[SecurityCritical]
			get
			{
				if (this._activationType == null && this._activationTypeName != null)
				{
					this._activationType = RemotingServices.InternalGetTypeFromQualifiedTypeName(this._activationTypeName, false);
				}
				return this._activationType;
			}
		}

		// Token: 0x17000F47 RID: 3911
		// (get) Token: 0x06005AE1 RID: 23265 RVA: 0x0013EDB5 File Offset: 0x0013CFB5
		public string ActivationTypeName
		{
			[SecurityCritical]
			get
			{
				return this._activationTypeName;
			}
		}

		// Token: 0x17000F48 RID: 3912
		// (get) Token: 0x06005AE2 RID: 23266 RVA: 0x0013EDBD File Offset: 0x0013CFBD
		public IList ContextProperties
		{
			[SecurityCritical]
			get
			{
				if (this._contextProperties == null)
				{
					this._contextProperties = new ArrayList();
				}
				return this._contextProperties;
			}
		}

		// Token: 0x17000F49 RID: 3913
		// (get) Token: 0x06005AE3 RID: 23267 RVA: 0x0013EDD8 File Offset: 0x0013CFD8
		// (set) Token: 0x06005AE4 RID: 23268 RVA: 0x0013EDFD File Offset: 0x0013CFFD
		public string Uri
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.Uri;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
			set
			{
				if (this._message != null)
				{
					this._message.Uri = value;
					return;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F4A RID: 3914
		// (get) Token: 0x06005AE5 RID: 23269 RVA: 0x0013EE23 File Offset: 0x0013D023
		public string MethodName
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.MethodName;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F4B RID: 3915
		// (get) Token: 0x06005AE6 RID: 23270 RVA: 0x0013EE48 File Offset: 0x0013D048
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.TypeName;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F4C RID: 3916
		// (get) Token: 0x06005AE7 RID: 23271 RVA: 0x0013EE6D File Offset: 0x0013D06D
		public object MethodSignature
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.MethodSignature;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F4D RID: 3917
		// (get) Token: 0x06005AE8 RID: 23272 RVA: 0x0013EE92 File Offset: 0x0013D092
		public MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.MethodBase;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F4E RID: 3918
		// (get) Token: 0x06005AE9 RID: 23273 RVA: 0x0013EEB7 File Offset: 0x0013D0B7
		public int InArgCount
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

		// Token: 0x06005AEA RID: 23274 RVA: 0x0013EED9 File Offset: 0x0013D0D9
		[SecurityCritical]
		public object GetInArg(int argNum)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, false);
			}
			return this._argMapper.GetArg(argNum);
		}

		// Token: 0x06005AEB RID: 23275 RVA: 0x0013EEFC File Offset: 0x0013D0FC
		[SecurityCritical]
		public string GetInArgName(int index)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, false);
			}
			return this._argMapper.GetArgName(index);
		}

		// Token: 0x17000F4F RID: 3919
		// (get) Token: 0x06005AEC RID: 23276 RVA: 0x0013EF1F File Offset: 0x0013D11F
		public object[] InArgs
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

		// Token: 0x17000F50 RID: 3920
		// (get) Token: 0x06005AED RID: 23277 RVA: 0x0013EF41 File Offset: 0x0013D141
		public int ArgCount
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.ArgCount;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x06005AEE RID: 23278 RVA: 0x0013EF66 File Offset: 0x0013D166
		[SecurityCritical]
		public object GetArg(int argNum)
		{
			if (this._message != null)
			{
				return this._message.GetArg(argNum);
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
		}

		// Token: 0x06005AEF RID: 23279 RVA: 0x0013EF8C File Offset: 0x0013D18C
		[SecurityCritical]
		public string GetArgName(int index)
		{
			if (this._message != null)
			{
				return this._message.GetArgName(index);
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
		}

		// Token: 0x17000F51 RID: 3921
		// (get) Token: 0x06005AF0 RID: 23280 RVA: 0x0013EFB2 File Offset: 0x0013D1B2
		public bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.HasVarArgs;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F52 RID: 3922
		// (get) Token: 0x06005AF1 RID: 23281 RVA: 0x0013EFD7 File Offset: 0x0013D1D7
		public object[] Args
		{
			[SecurityCritical]
			get
			{
				if (this._message != null)
				{
					return this._message.Args;
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
		}

		// Token: 0x17000F53 RID: 3923
		// (get) Token: 0x06005AF2 RID: 23282 RVA: 0x0013EFFC File Offset: 0x0013D1FC
		public IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				if (this._properties == null)
				{
					object value = new CCMDictionary(this, new Hashtable());
					Interlocked.CompareExchange(ref this._properties, value, null);
				}
				return (IDictionary)this._properties;
			}
		}

		// Token: 0x17000F54 RID: 3924
		// (get) Token: 0x06005AF3 RID: 23283 RVA: 0x0013F036 File Offset: 0x0013D236
		// (set) Token: 0x06005AF4 RID: 23284 RVA: 0x0013F03E File Offset: 0x0013D23E
		public IActivator Activator
		{
			[SecurityCritical]
			get
			{
				return this._activator;
			}
			[SecurityCritical]
			set
			{
				this._activator = value;
			}
		}

		// Token: 0x17000F55 RID: 3925
		// (get) Token: 0x06005AF5 RID: 23285 RVA: 0x0013F047 File Offset: 0x0013D247
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this.GetLogicalCallContext();
			}
		}

		// Token: 0x17000F56 RID: 3926
		// (get) Token: 0x06005AF6 RID: 23286 RVA: 0x0013F04F File Offset: 0x0013D24F
		// (set) Token: 0x06005AF7 RID: 23287 RVA: 0x0013F05C File Offset: 0x0013D25C
		internal bool ActivateInContext
		{
			get
			{
				return (this._iFlags & 1) != 0;
			}
			set
			{
				this._iFlags = (value ? (this._iFlags | 1) : (this._iFlags & -2));
			}
		}

		// Token: 0x06005AF8 RID: 23288 RVA: 0x0013F07A File Offset: 0x0013D27A
		[SecurityCritical]
		internal void SetFrame(MessageData msgData)
		{
			this._message = new Message();
			this._message.InitFields(msgData);
		}

		// Token: 0x06005AF9 RID: 23289 RVA: 0x0013F093 File Offset: 0x0013D293
		[SecurityCritical]
		internal LogicalCallContext GetLogicalCallContext()
		{
			if (this._message != null)
			{
				return this._message.GetLogicalCallContext();
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
		}

		// Token: 0x06005AFA RID: 23290 RVA: 0x0013F0B8 File Offset: 0x0013D2B8
		[SecurityCritical]
		internal LogicalCallContext SetLogicalCallContext(LogicalCallContext ctx)
		{
			if (this._message != null)
			{
				return this._message.SetLogicalCallContext(ctx);
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
		}

		// Token: 0x06005AFB RID: 23291 RVA: 0x0013F0DE File Offset: 0x0013D2DE
		internal Message GetMessage()
		{
			return this._message;
		}

		// Token: 0x0400292D RID: 10541
		private object[] _callSiteActivationAttributes;

		// Token: 0x0400292E RID: 10542
		private object[] _womGlobalAttributes;

		// Token: 0x0400292F RID: 10543
		private object[] _typeAttributes;

		// Token: 0x04002930 RID: 10544
		[NonSerialized]
		private RuntimeType _activationType;

		// Token: 0x04002931 RID: 10545
		private string _activationTypeName;

		// Token: 0x04002932 RID: 10546
		private IList _contextProperties;

		// Token: 0x04002933 RID: 10547
		private int _iFlags;

		// Token: 0x04002934 RID: 10548
		private Message _message;

		// Token: 0x04002935 RID: 10549
		private object _properties;

		// Token: 0x04002936 RID: 10550
		private ArgMapper _argMapper;

		// Token: 0x04002937 RID: 10551
		private IActivator _activator;

		// Token: 0x04002938 RID: 10552
		private const int CCM_ACTIVATEINCONTEXT = 1;
	}
}
