using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005EA RID: 1514
	[ComVisible(true)]
	public interface ICustomAttributeProvider
	{
		// Token: 0x0600464F RID: 17999
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06004650 RID: 18000
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06004651 RID: 18001
		bool IsDefined(Type attributeType, bool inherit);
	}
}
