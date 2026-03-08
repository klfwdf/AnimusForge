using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000048 RID: 72
	[NullableContext(1)]
	[Nullable(0)]
	internal class TypeInformation
	{
		// Token: 0x170000AC RID: 172
		// (get) Token: 0x0600046C RID: 1132 RVA: 0x00011366 File Offset: 0x0000F566
		public Type Type { get; }

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x0600046D RID: 1133 RVA: 0x0001136E File Offset: 0x0000F56E
		public PrimitiveTypeCode TypeCode { get; }

		// Token: 0x0600046E RID: 1134 RVA: 0x00011376 File Offset: 0x0000F576
		public TypeInformation(Type type, PrimitiveTypeCode typeCode)
		{
			this.Type = type;
			this.TypeCode = typeCode;
		}
	}
}
