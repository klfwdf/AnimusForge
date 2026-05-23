using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008E4 RID: 2276
	[Serializable]
	public sealed class RuntimeWrappedException : Exception
	{
		// Token: 0x06005DE4 RID: 24036 RVA: 0x00149B16 File Offset: 0x00147D16
		private RuntimeWrappedException(object thrownObject)
			: base(Environment.GetResourceString("RuntimeWrappedException"))
		{
			base.SetErrorCode(-2146233026);
			this.m_wrappedException = thrownObject;
		}

		// Token: 0x17001021 RID: 4129
		// (get) Token: 0x06005DE5 RID: 24037 RVA: 0x00149B3A File Offset: 0x00147D3A
		public object WrappedException
		{
			get
			{
				return this.m_wrappedException;
			}
		}

		// Token: 0x06005DE6 RID: 24038 RVA: 0x00149B42 File Offset: 0x00147D42
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("WrappedException", this.m_wrappedException, typeof(object));
		}

		// Token: 0x06005DE7 RID: 24039 RVA: 0x00149B75 File Offset: 0x00147D75
		internal RuntimeWrappedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.m_wrappedException = info.GetValue("WrappedException", typeof(object));
		}

		// Token: 0x04002A3E RID: 10814
		private object m_wrappedException;
	}
}
