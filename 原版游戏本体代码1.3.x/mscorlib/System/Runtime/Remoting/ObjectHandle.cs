using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007CF RID: 1999
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ComVisible(true)]
	public class ObjectHandle : MarshalByRefObject, IObjectHandle
	{
		// Token: 0x060056C6 RID: 22214 RVA: 0x001343F7 File Offset: 0x001325F7
		private ObjectHandle()
		{
		}

		// Token: 0x060056C7 RID: 22215 RVA: 0x001343FF File Offset: 0x001325FF
		public ObjectHandle(object o)
		{
			this.WrappedObject = o;
		}

		// Token: 0x060056C8 RID: 22216 RVA: 0x0013440E File Offset: 0x0013260E
		public object Unwrap()
		{
			return this.WrappedObject;
		}

		// Token: 0x060056C9 RID: 22217 RVA: 0x00134418 File Offset: 0x00132618
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			MarshalByRefObject marshalByRefObject = this.WrappedObject as MarshalByRefObject;
			if (marshalByRefObject != null && marshalByRefObject.InitializeLifetimeService() == null)
			{
				return null;
			}
			return (ILease)base.InitializeLifetimeService();
		}

		// Token: 0x040027B5 RID: 10165
		private object WrappedObject;
	}
}
