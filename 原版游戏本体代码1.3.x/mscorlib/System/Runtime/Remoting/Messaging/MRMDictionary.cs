using System;
using System.Collections;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000864 RID: 2148
	internal class MRMDictionary : MessageDictionary
	{
		// Token: 0x06005B0B RID: 23307 RVA: 0x0013F6C3 File Offset: 0x0013D8C3
		[SecurityCritical]
		public MRMDictionary(IMethodReturnMessage msg, IDictionary idict)
			: base((msg.Exception != null) ? MRMDictionary.MCMkeysFault : MRMDictionary.MCMkeysNoFault, idict)
		{
			this.fault = msg.Exception != null;
			this._mrmsg = msg;
		}

		// Token: 0x06005B0C RID: 23308 RVA: 0x0013F6F8 File Offset: 0x0013D8F8
		[SecuritySafeCritical]
		internal override object GetMessageValue(int i)
		{
			switch (i)
			{
			case 0:
				if (this.fault)
				{
					return this.FetchLogicalCallContext();
				}
				return this._mrmsg.Uri;
			case 1:
				return this._mrmsg.MethodName;
			case 2:
				return this._mrmsg.MethodSignature;
			case 3:
				return this._mrmsg.TypeName;
			case 4:
				if (this.fault)
				{
					return this._mrmsg.Exception;
				}
				return this._mrmsg.ReturnValue;
			case 5:
				return this._mrmsg.Args;
			case 6:
				return this.FetchLogicalCallContext();
			default:
				throw new RemotingException(Environment.GetResourceString("Remoting_Default"));
			}
		}

		// Token: 0x06005B0D RID: 23309 RVA: 0x0013F7AC File Offset: 0x0013D9AC
		[SecurityCritical]
		private LogicalCallContext FetchLogicalCallContext()
		{
			ReturnMessage returnMessage = this._mrmsg as ReturnMessage;
			if (returnMessage != null)
			{
				return returnMessage.GetLogicalCallContext();
			}
			MethodResponse methodResponse = this._mrmsg as MethodResponse;
			if (methodResponse != null)
			{
				return methodResponse.GetLogicalCallContext();
			}
			StackBasedReturnMessage stackBasedReturnMessage = this._mrmsg as StackBasedReturnMessage;
			if (stackBasedReturnMessage != null)
			{
				return stackBasedReturnMessage.GetLogicalCallContext();
			}
			throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadType"));
		}

		// Token: 0x06005B0E RID: 23310 RVA: 0x0013F80C File Offset: 0x0013DA0C
		[SecurityCritical]
		internal override void SetSpecialKey(int keyNum, object value)
		{
			ReturnMessage returnMessage = this._mrmsg as ReturnMessage;
			MethodResponse methodResponse = this._mrmsg as MethodResponse;
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

		// Token: 0x04002941 RID: 10561
		public static string[] MCMkeysFault = new string[] { "__CallContext" };

		// Token: 0x04002942 RID: 10562
		public static string[] MCMkeysNoFault = new string[] { "__Uri", "__MethodName", "__MethodSignature", "__TypeName", "__Return", "__OutArgs", "__CallContext" };

		// Token: 0x04002943 RID: 10563
		internal IMethodReturnMessage _mrmsg;

		// Token: 0x04002944 RID: 10564
		internal bool fault;
	}
}
