using System;

namespace TaleWorlds.DotNet;

[EngineStruct("ftlNative_object_pointer", false, null)]
internal struct NativeObjectPointer
{
	public UIntPtr Pointer;

	public int TypeId;
}
