using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
	// Token: 0x0200060A RID: 1546
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class Missing : ISerializable
	{
		// Token: 0x06004788 RID: 18312 RVA: 0x00104AFB File Offset: 0x00102CFB
		private Missing()
		{
		}

		// Token: 0x06004789 RID: 18313 RVA: 0x00104B03 File Offset: 0x00102D03
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			UnitySerializationHolder.GetUnitySerializationInfo(info, this);
		}

		// Token: 0x04001DAE RID: 7598
		[__DynamicallyInvokable]
		public static readonly Missing Value = new Missing();
	}
}
