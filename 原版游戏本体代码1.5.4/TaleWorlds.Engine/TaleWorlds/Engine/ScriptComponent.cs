using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineClass("rglScript_component")]
public abstract class ScriptComponent : NativeObject
{
	protected ScriptComponent()
	{
	}

	internal ScriptComponent(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public string GetName()
	{
		return EngineApplicationInterface.IScriptComponent.GetName(base.Pointer);
	}
}
