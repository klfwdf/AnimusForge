using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000731 RID: 1841
	[ComVisible(true)]
	public interface IDeserializationCallback
	{
		// Token: 0x060051A6 RID: 20902
		void OnDeserialization(object sender);
	}
}
