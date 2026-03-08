using System;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B9 RID: 1977
	[Serializable]
	internal class DynamicTypeInfo : TypeInfo
	{
		// Token: 0x0600559A RID: 21914 RVA: 0x00130127 File Offset: 0x0012E327
		[SecurityCritical]
		internal DynamicTypeInfo(RuntimeType typeOfObj)
			: base(typeOfObj)
		{
		}

		// Token: 0x0600559B RID: 21915 RVA: 0x00130130 File Offset: 0x0012E330
		[SecurityCritical]
		public override bool CanCastTo(Type castType, object o)
		{
			return ((MarshalByRefObject)o).IsInstanceOfType(castType);
		}
	}
}
