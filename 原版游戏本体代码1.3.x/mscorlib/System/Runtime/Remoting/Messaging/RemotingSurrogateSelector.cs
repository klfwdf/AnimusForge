using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200087B RID: 2171
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public class RemotingSurrogateSelector : ISurrogateSelector
	{
		// Token: 0x06005C4E RID: 23630 RVA: 0x00143847 File Offset: 0x00141A47
		public RemotingSurrogateSelector()
		{
			this._messageSurrogate = new MessageSurrogate(this);
		}

		// Token: 0x17000FDE RID: 4062
		// (get) Token: 0x06005C50 RID: 23632 RVA: 0x0014387A File Offset: 0x00141A7A
		// (set) Token: 0x06005C4F RID: 23631 RVA: 0x00143871 File Offset: 0x00141A71
		public MessageSurrogateFilter Filter
		{
			get
			{
				return this._filter;
			}
			set
			{
				this._filter = value;
			}
		}

		// Token: 0x06005C51 RID: 23633 RVA: 0x00143884 File Offset: 0x00141A84
		public void SetRootObject(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			this._rootObj = obj;
			SoapMessageSurrogate soapMessageSurrogate = this._messageSurrogate as SoapMessageSurrogate;
			if (soapMessageSurrogate != null)
			{
				soapMessageSurrogate.SetRootObject(this._rootObj);
			}
		}

		// Token: 0x06005C52 RID: 23634 RVA: 0x001438C1 File Offset: 0x00141AC1
		public object GetRootObject()
		{
			return this._rootObj;
		}

		// Token: 0x06005C53 RID: 23635 RVA: 0x001438C9 File Offset: 0x00141AC9
		[SecurityCritical]
		public virtual void ChainSelector(ISurrogateSelector selector)
		{
			this._next = selector;
		}

		// Token: 0x06005C54 RID: 23636 RVA: 0x001438D4 File Offset: 0x00141AD4
		[SecurityCritical]
		public virtual ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector ssout)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (type.IsMarshalByRef)
			{
				ssout = this;
				return this._remotingSurrogate;
			}
			if (RemotingSurrogateSelector.s_IMethodCallMessageType.IsAssignableFrom(type) || RemotingSurrogateSelector.s_IMethodReturnMessageType.IsAssignableFrom(type))
			{
				ssout = this;
				return this._messageSurrogate;
			}
			if (RemotingSurrogateSelector.s_ObjRefType.IsAssignableFrom(type))
			{
				ssout = this;
				return this._objRefSurrogate;
			}
			if (this._next != null)
			{
				return this._next.GetSurrogate(type, context, out ssout);
			}
			ssout = null;
			return null;
		}

		// Token: 0x06005C55 RID: 23637 RVA: 0x0014395D File Offset: 0x00141B5D
		[SecurityCritical]
		public virtual ISurrogateSelector GetNextSelector()
		{
			return this._next;
		}

		// Token: 0x06005C56 RID: 23638 RVA: 0x00143965 File Offset: 0x00141B65
		public virtual void UseSoapFormat()
		{
			this._messageSurrogate = new SoapMessageSurrogate(this);
			((SoapMessageSurrogate)this._messageSurrogate).SetRootObject(this._rootObj);
		}

		// Token: 0x040029AF RID: 10671
		private static Type s_IMethodCallMessageType = typeof(IMethodCallMessage);

		// Token: 0x040029B0 RID: 10672
		private static Type s_IMethodReturnMessageType = typeof(IMethodReturnMessage);

		// Token: 0x040029B1 RID: 10673
		private static Type s_ObjRefType = typeof(ObjRef);

		// Token: 0x040029B2 RID: 10674
		private object _rootObj;

		// Token: 0x040029B3 RID: 10675
		private ISurrogateSelector _next;

		// Token: 0x040029B4 RID: 10676
		private RemotingSurrogate _remotingSurrogate = new RemotingSurrogate();

		// Token: 0x040029B5 RID: 10677
		private ObjRefSurrogate _objRefSurrogate = new ObjRefSurrogate();

		// Token: 0x040029B6 RID: 10678
		private ISerializationSurrogate _messageSurrogate;

		// Token: 0x040029B7 RID: 10679
		private MessageSurrogateFilter _filter;
	}
}
