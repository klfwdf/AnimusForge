using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x0200073E RID: 1854
	[ComVisible(true)]
	[Serializable]
	public abstract class SerializationBinder
	{
		// Token: 0x060051D1 RID: 20945 RVA: 0x0011FE9B File Offset: 0x0011E09B
		public virtual void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			assemblyName = null;
			typeName = null;
		}

		// Token: 0x060051D2 RID: 20946
		public abstract Type BindToType(string assemblyName, string typeName);
	}
}
