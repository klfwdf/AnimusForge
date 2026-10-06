using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

[LibraryInterfaceBase]
internal interface INativeObjectArray
{
	[EngineMethod("create", false, null, false)]
	NativeObjectArray Create();

	[EngineMethod("get_count", false, null, false)]
	int GetCount(UIntPtr pointer);

	[EngineMethod("add_element", false, null, false)]
	void AddElement(UIntPtr pointer, UIntPtr nativeObject);

	[EngineMethod("get_element_at_index", false, null, false)]
	NativeObject GetElementAtIndex(UIntPtr pointer, int index);

	[EngineMethod("clear", false, null, false)]
	void Clear(UIntPtr pointer);
}
