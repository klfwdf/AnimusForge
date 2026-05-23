using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000019 RID: 25
	[LibraryInterfaceBase]
	internal interface ILibrarySizeChecker
	{
		// Token: 0x06000079 RID: 121
		[EngineMethod("get_engine_struct_size", false, null, false)]
		int GetEngineStructSize(string str);

		// Token: 0x0600007A RID: 122
		[EngineMethod("get_engine_struct_member_offset", false, null, false)]
		IntPtr GetEngineStructMemberOffset(string className, string memberName);
	}
}
