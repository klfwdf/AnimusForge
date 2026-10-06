using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBBannerlordChecker
{
	[EngineMethod("get_engine_struct_size", false, null, false)]
	int GetEngineStructSize(string str);

	[EngineMethod("get_engine_struct_member_offset", false, null, false)]
	IntPtr GetEngineStructMemberOffset(string className, string memberName);
}
