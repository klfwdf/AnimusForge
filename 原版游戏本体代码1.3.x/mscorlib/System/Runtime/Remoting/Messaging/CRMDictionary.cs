using System;
using System.Collections;
using System.Runtime.Remoting.Activation;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000862 RID: 2146
	internal class CRMDictionary : MessageDictionary
	{
		// Token: 0x06005B01 RID: 23297 RVA: 0x0013F2DB File Offset: 0x0013D4DB
		[SecurityCritical]
		public CRMDictionary(IConstructionReturnMessage msg, IDictionary idict)
			: base((msg.Exception != null) ? CRMDictionary.CRMkeysFault : CRMDictionary.CRMkeysNoFault, idict)
		{
			this.fault = msg.Exception != null;
			this._crmsg = msg;
		}

		// Token: 0x06005B02 RID: 23298 RVA: 0x0013F310 File Offset: 0x0013D510
		[SecuritySafeCritical]
		internal override object GetMessageValue(int i)
		{
			switch (i)
			{
			case 0:
				return this._crmsg.Uri;
			case 1:
				return this._crmsg.MethodName;
			case 2:
				return this._crmsg.MethodSignature;
			case 3:
				return this._crmsg.TypeName;
			case 4:
				if (!this.fault)
				{
					return this._crmsg.ReturnValue;
				}
				return this.FetchLogicalCallContext();
			case 5:
				return this._crmsg.Args;
			case 6:
				return this.FetchLogicalCallContext();
			default:
				throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
			}
		}

		// Token: 0x06005B03 RID: 23299 RVA: 0x0013F3B0 File Offset: 0x0013D5B0
		[SecurityCritical]
		private LogicalCallContext FetchLogicalCallContext()
		{
			ReturnMessage returnMessage = this._crmsg as ReturnMessage;
			if (returnMessage != null)
			{
				return returnMessage.GetLogicalCallContext();
			}
			MethodResponse methodResponse = this._crmsg as MethodResponse;
			if (methodResponse != null)
			{
				return methodResponse.GetLogicalCallContext();
			}
			throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadType"));
		}

		// Token: 0x06005B04 RID: 23300 RVA: 0x0013F3F8 File Offset: 0x0013D5F8
		[SecurityCritical]
		internal override void SetSpecialKey(int keyNum, object value)
		{
			ReturnMessage returnMessage = this._crmsg as ReturnMessage;
			MethodResponse methodResponse = this._crmsg as MethodResponse;
			if (keyNum != 0)
			{
				if (keyNum != 1)
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
				}
				if (returnMessage != null)
				{
					returnMessage.SetLogicalCallContext((LogicalCallContext)value);
					return;
				}
				if (methodResponse != null)
				{
					methodResponse.SetLogicalCallContext((LogicalCallContext)value);
					return;
				}
				throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadType"));
			}
			else
			{
				if (returnMessage != null)
				{
					returnMessage.Uri = (string)value;
					return;
				}
				if (methodResponse != null)
				{
					methodResponse.Uri = (string)value;
					return;
				}
				throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadType"));
			}
		}

		// Token: 0x0400293B RID: 10555
		public static string[] CRMkeysFault = new string[] { "__Uri", "__MethodName", "__MethodSignature", "__TypeName", "__CallContext" };

		// Token: 0x0400293C RID: 10556
		public static string[] CRMkeysNoFault = new string[] { "__Uri", "__MethodName", "__MethodSignature", "__TypeName", "__Return", "__OutArgs", "__CallContext" };

		// Token: 0x0400293D RID: 10557
		internal IConstructionReturnMessage _crmsg;

		// Token: 0x0400293E RID: 10558
		internal bool fault;
	}
}
