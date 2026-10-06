using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

[LibraryInterfaceBase]
internal interface INativeString
{
	[EngineMethod("create", false, null, false)]
	NativeString Create();

	[EngineMethod("get_string", false, null, false)]
	string GetString(NativeString nativeString);

	[EngineMethod("set_string", false, null, false)]
	void SetString(NativeString nativeString, string newString);
}
