using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000010 RID: 16
	[EngineStruct("ftlObject_type_definition", false, null)]
	internal struct EngineClassTypeDefinition
	{
		// Token: 0x0400002B RID: 43
		public int TypeId;

		// Token: 0x0400002C RID: 44
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string TypeName;
	}
}
