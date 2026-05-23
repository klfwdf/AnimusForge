using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200094B RID: 2379
	[ComVisible(true)]
	public interface ICustomMarshaler
	{
		// Token: 0x060060AB RID: 24747
		object MarshalNativeToManaged(IntPtr pNativeData);

		// Token: 0x060060AC RID: 24748
		IntPtr MarshalManagedToNative(object ManagedObj);

		// Token: 0x060060AD RID: 24749
		void CleanUpNativeData(IntPtr pNativeData);

		// Token: 0x060060AE RID: 24750
		void CleanUpManagedData(object ManagedObj);

		// Token: 0x060060AF RID: 24751
		int GetNativeDataSize();
	}
}
