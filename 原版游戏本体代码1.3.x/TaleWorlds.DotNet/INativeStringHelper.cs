using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200001C RID: 28
	[LibraryInterfaceBase]
	internal interface INativeStringHelper
	{
		// Token: 0x06000088 RID: 136
		[EngineMethod("create_rglVarString", false, null, false)]
		UIntPtr CreateRglVarString(string text);

		// Token: 0x06000089 RID: 137
		[EngineMethod("get_thread_local_cached_rglVarString", false, null, false)]
		UIntPtr GetThreadLocalCachedRglVarString();

		// Token: 0x0600008A RID: 138
		[EngineMethod("set_rglVarString", false, null, false)]
		void SetRglVarString(UIntPtr pointer, string text);

		// Token: 0x0600008B RID: 139
		[EngineMethod("delete_rglVarString", false, null, false)]
		void DeleteRglVarString(UIntPtr pointer);
	}
}
