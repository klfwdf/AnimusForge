using System;
using System.Collections;
using System.Runtime.Remoting.Activation;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000861 RID: 2145
	internal class CCMDictionary : MessageDictionary
	{
		// Token: 0x06005AFC RID: 23292 RVA: 0x0013F0E6 File Offset: 0x0013D2E6
		public CCMDictionary(IConstructionCallMessage msg, IDictionary idict)
			: base(CCMDictionary.CCMkeys, idict)
		{
			this._ccmsg = msg;
		}

		// Token: 0x06005AFD RID: 23293 RVA: 0x0013F0FC File Offset: 0x0013D2FC
		[SecuritySafeCritical]
		internal override object GetMessageValue(int i)
		{
			switch (i)
			{
			case 0:
				return this._ccmsg.Uri;
			case 1:
				return this._ccmsg.MethodName;
			case 2:
				return this._ccmsg.MethodSignature;
			case 3:
				return this._ccmsg.TypeName;
			case 4:
				return this._ccmsg.Args;
			case 5:
				return this.FetchLogicalCallContext();
			case 6:
				return this._ccmsg.CallSiteActivationAttributes;
			case 7:
				return null;
			case 8:
				return this._ccmsg.ContextProperties;
			case 9:
				return this._ccmsg.Activator;
			case 10:
				return this._ccmsg.ActivationTypeName;
			default:
				throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
			}
		}

		// Token: 0x06005AFE RID: 23294 RVA: 0x0013F1C4 File Offset: 0x0013D3C4
		[SecurityCritical]
		private LogicalCallContext FetchLogicalCallContext()
		{
			ConstructorCallMessage constructorCallMessage = this._ccmsg as ConstructorCallMessage;
			if (constructorCallMessage != null)
			{
				return constructorCallMessage.GetLogicalCallContext();
			}
			if (this._ccmsg is ConstructionCall)
			{
				return ((MethodCall)this._ccmsg).GetLogicalCallContext();
			}
			throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadType"));
		}

		// Token: 0x06005AFF RID: 23295 RVA: 0x0013F214 File Offset: 0x0013D414
		[SecurityCritical]
		internal override void SetSpecialKey(int keyNum, object value)
		{
			if (keyNum == 0)
			{
				((ConstructorCallMessage)this._ccmsg).Uri = (string)value;
				return;
			}
			if (keyNum != 1)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
			}
			((ConstructorCallMessage)this._ccmsg).SetLogicalCallContext((LogicalCallContext)value);
		}

		// Token: 0x04002939 RID: 10553
		public static string[] CCMkeys = new string[]
		{
			"__Uri", "__MethodName", "__MethodSignature", "__TypeName", "__Args", "__CallContext", "__CallSiteActivationAttributes", "__ActivationType", "__ContextProperties", "__Activator",
			"__ActivationTypeName"
		};

		// Token: 0x0400293A RID: 10554
		internal IConstructionCallMessage _ccmsg;
	}
}
