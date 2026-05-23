using System;
using System.Collections;
using System.Runtime.Remoting.Activation;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200085F RID: 2143
	[SecurityCritical]
	internal class ConstructorReturnMessage : ReturnMessage, IConstructionReturnMessage, IMethodReturnMessage, IMethodMessage, IMessage
	{
		// Token: 0x06005AD5 RID: 23253 RVA: 0x0013EC7F File Offset: 0x0013CE7F
		public ConstructorReturnMessage(MarshalByRefObject o, object[] outArgs, int outArgsCount, LogicalCallContext callCtx, IConstructionCallMessage ccm)
			: base(o, outArgs, outArgsCount, callCtx, ccm)
		{
			this._o = o;
			this._iFlags = 1;
		}

		// Token: 0x06005AD6 RID: 23254 RVA: 0x0013EC9C File Offset: 0x0013CE9C
		public ConstructorReturnMessage(Exception e, IConstructionCallMessage ccm)
			: base(e, ccm)
		{
		}

		// Token: 0x17000F43 RID: 3907
		// (get) Token: 0x06005AD7 RID: 23255 RVA: 0x0013ECA6 File Offset: 0x0013CEA6
		public override object ReturnValue
		{
			[SecurityCritical]
			get
			{
				if (this._iFlags == 1)
				{
					return RemotingServices.MarshalInternal(this._o, null, null);
				}
				return base.ReturnValue;
			}
		}

		// Token: 0x17000F44 RID: 3908
		// (get) Token: 0x06005AD8 RID: 23256 RVA: 0x0013ECC8 File Offset: 0x0013CEC8
		public override IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				if (this._properties == null)
				{
					object value = new CRMDictionary(this, new Hashtable());
					Interlocked.CompareExchange(ref this._properties, value, null);
				}
				return (IDictionary)this._properties;
			}
		}

		// Token: 0x06005AD9 RID: 23257 RVA: 0x0013ED02 File Offset: 0x0013CF02
		internal object GetObject()
		{
			return this._o;
		}

		// Token: 0x0400292A RID: 10538
		private const int Intercept = 1;

		// Token: 0x0400292B RID: 10539
		private MarshalByRefObject _o;

		// Token: 0x0400292C RID: 10540
		private int _iFlags;
	}
}
