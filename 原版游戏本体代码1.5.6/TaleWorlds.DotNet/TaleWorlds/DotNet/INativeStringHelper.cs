using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

[LibraryInterfaceBase]
internal interface INativeStringHelper
{
	[EngineMethod("create_rglVarString", false, null, false)]
	UIntPtr CreateRglVarString(string text);

	[EngineMethod("get_thread_local_cached_rglVarString", false, null, false)]
	UIntPtr GetThreadLocalCachedRglVarString();

	[EngineMethod("set_rglVarString", false, null, false)]
	void SetRglVarString(UIntPtr pointer, string text);

	[EngineMethod("delete_rglVarString", false, null, false)]
	void DeleteRglVarString(UIntPtr pointer);
}
