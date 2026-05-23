using System;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x020007A4 RID: 1956
	internal sealed class SerObjectInfoCache
	{
		// Token: 0x060054EB RID: 21739 RVA: 0x0012E0AE File Offset: 0x0012C2AE
		internal SerObjectInfoCache(string typeName, string assemblyName, bool hasTypeForwardedFrom)
		{
			this.fullTypeName = typeName;
			this.assemblyString = assemblyName;
			this.hasTypeForwardedFrom = hasTypeForwardedFrom;
		}

		// Token: 0x060054EC RID: 21740 RVA: 0x0012E0CC File Offset: 0x0012C2CC
		internal SerObjectInfoCache(Type type)
		{
			TypeInformation typeInformation = BinaryFormatter.GetTypeInformation(type);
			this.fullTypeName = typeInformation.FullTypeName;
			this.assemblyString = typeInformation.AssemblyString;
			this.hasTypeForwardedFrom = typeInformation.HasTypeForwardedFrom;
		}

		// Token: 0x04002709 RID: 9993
		internal string fullTypeName;

		// Token: 0x0400270A RID: 9994
		internal string assemblyString;

		// Token: 0x0400270B RID: 9995
		internal bool hasTypeForwardedFrom;

		// Token: 0x0400270C RID: 9996
		internal MemberInfo[] memberInfos;

		// Token: 0x0400270D RID: 9997
		internal string[] memberNames;

		// Token: 0x0400270E RID: 9998
		internal Type[] memberTypes;
	}
}
