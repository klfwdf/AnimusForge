using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglNative_script_component")]
public sealed class NativeScriptComponent : ScriptComponent
{
	internal NativeScriptComponent(UIntPtr pointer)
		: base(pointer)
	{
	}
}
