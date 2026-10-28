using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IShader
{
	[EngineMethod("get_from_resource", false, null, false)]
	Shader GetFromResource(string shaderName);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr shaderPointer);

	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr shaderPointer);

	[EngineMethod("get_material_shader_flag_mask", false, null, false)]
	ulong GetMaterialShaderFlagMask(UIntPtr shaderPointer, string flagName, bool showError);
}
