using System;
using System.Reflection;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000450 RID: 1104
	internal sealed class PropertyAnalysis
	{
		// Token: 0x0600365E RID: 13918 RVA: 0x000D329D File Offset: 0x000D149D
		public PropertyAnalysis(string name, MethodInfo getterInfo, TraceLoggingTypeInfo typeInfo, EventFieldAttribute fieldAttribute)
		{
			this.name = name;
			this.getterInfo = getterInfo;
			this.typeInfo = typeInfo;
			this.fieldAttribute = fieldAttribute;
		}

		// Token: 0x04001857 RID: 6231
		internal readonly string name;

		// Token: 0x04001858 RID: 6232
		internal readonly MethodInfo getterInfo;

		// Token: 0x04001859 RID: 6233
		internal readonly TraceLoggingTypeInfo typeInfo;

		// Token: 0x0400185A RID: 6234
		internal readonly EventFieldAttribute fieldAttribute;
	}
}
