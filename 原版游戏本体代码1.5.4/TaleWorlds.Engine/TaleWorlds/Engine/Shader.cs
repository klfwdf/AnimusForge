using System;

namespace TaleWorlds.Engine;

public sealed class Shader : Resource
{
	public string Name => EngineApplicationInterface.IShader.GetName(base.Pointer);

	internal Shader(UIntPtr ptr)
		: base(ptr)
	{
	}

	public static Shader GetFromResource(string shaderName)
	{
		return EngineApplicationInterface.IShader.GetFromResource(shaderName);
	}

	public ulong GetMaterialShaderFlagMask(string flagName, bool showErrors = true)
	{
		return EngineApplicationInterface.IShader.GetMaterialShaderFlagMask(base.Pointer, flagName, showErrors);
	}
}
