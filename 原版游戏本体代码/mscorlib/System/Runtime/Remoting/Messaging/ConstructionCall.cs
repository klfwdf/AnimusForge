using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200086A RID: 2154
	[SecurityCritical]
	[CLSCompliant(false)]
	[ComVisible(true)]
	[Serializable]
	public class ConstructionCall : MethodCall, IConstructionCallMessage, IMethodCallMessage, IMethodMessage, IMessage
	{
		// Token: 0x06005B94 RID: 23444 RVA: 0x00141413 File Offset: 0x0013F613
		public ConstructionCall(Header[] headers)
			: base(headers)
		{
		}

		// Token: 0x06005B95 RID: 23445 RVA: 0x0014141C File Offset: 0x0013F61C
		public ConstructionCall(IMessage m)
			: base(m)
		{
		}

		// Token: 0x06005B96 RID: 23446 RVA: 0x00141425 File Offset: 0x0013F625
		internal ConstructionCall(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x06005B97 RID: 23447 RVA: 0x00141430 File Offset: 0x0013F630
		[SecurityCritical]
		internal override bool FillSpecialHeader(string key, object value)
		{
			if (key != null)
			{
				if (key.Equals("__ActivationType"))
				{
					this._activationType = null;
				}
				else if (key.Equals("__ContextProperties"))
				{
					this._contextProperties = (IList)value;
				}
				else if (key.Equals("__CallSiteActivationAttributes"))
				{
					this._callSiteActivationAttributes = (object[])value;
				}
				else if (key.Equals("__Activator"))
				{
					this._activator = (IActivator)value;
				}
				else
				{
					if (!key.Equals("__ActivationTypeName"))
					{
						return base.FillSpecialHeader(key, value);
					}
					this._activationTypeName = (string)value;
				}
			}
			return true;
		}

		// Token: 0x17000F90 RID: 3984
		// (get) Token: 0x06005B98 RID: 23448 RVA: 0x001414CF File Offset: 0x0013F6CF
		public object[] CallSiteActivationAttributes
		{
			[SecurityCritical]
			get
			{
				return this._callSiteActivationAttributes;
			}
		}

		// Token: 0x17000F91 RID: 3985
		// (get) Token: 0x06005B99 RID: 23449 RVA: 0x001414D7 File Offset: 0x0013F6D7
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

		// Token: 0x17000F92 RID: 3986
		// (get) Token: 0x06005B9A RID: 23450 RVA: 0x00141507 File Offset: 0x0013F707
		public string ActivationTypeName
		{
			[SecurityCritical]
			get
			{
				return this._activationTypeName;
			}
		}

		// Token: 0x17000F93 RID: 3987
		// (get) Token: 0x06005B9B RID: 23451 RVA: 0x0014150F File Offset: 0x0013F70F
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

		// Token: 0x17000F94 RID: 3988
		// (get) Token: 0x06005B9C RID: 23452 RVA: 0x0014152C File Offset: 0x0013F72C
		public override IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				IDictionary externalProperties;
				lock (this)
				{
					if (this.InternalProperties == null)
					{
						this.InternalProperties = new Hashtable();
					}
					if (this.ExternalProperties == null)
					{
						this.ExternalProperties = new CCMDictionary(this, this.InternalProperties);
					}
					externalProperties = this.ExternalProperties;
				}
				return externalProperties;
			}
		}

		// Token: 0x17000F95 RID: 3989
		// (get) Token: 0x06005B9D RID: 23453 RVA: 0x00141598 File Offset: 0x0013F798
		// (set) Token: 0x06005B9E RID: 23454 RVA: 0x001415A0 File Offset: 0x0013F7A0
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

		// Token: 0x0400296C RID: 10604
		internal Type _activationType;

		// Token: 0x0400296D RID: 10605
		internal string _activationTypeName;

		// Token: 0x0400296E RID: 10606
		internal IList _contextProperties;

		// Token: 0x0400296F RID: 10607
		internal object[] _callSiteActivationAttributes;

		// Token: 0x04002970 RID: 10608
		internal IActivator _activator;
	}
}
