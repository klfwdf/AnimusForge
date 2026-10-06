using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IMaterial
{
	[EngineMethod("create_copy", false, null, false)]
	Material CreateCopy(UIntPtr materialPointer);

	[EngineMethod("get_from_resource", false, null, false)]
	Material GetFromResource(string materialName);

	[EngineMethod("get_default_material", false, null, false)]
	Material GetDefaultMaterial();

	[EngineMethod("get_outline_material", false, null, false)]
	Material GetOutlineMaterial(UIntPtr materialPointer);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr materialPointer);

	[EngineMethod("set_name", false, null, false)]
	void SetName(UIntPtr materialPointer, string name);

	[EngineMethod("get_alpha_blend_mode", false, null, false)]
	int GetAlphaBlendMode(UIntPtr materialPointer);

	[EngineMethod("set_alpha_blend_mode", false, null, false)]
	void SetAlphaBlendMode(UIntPtr materialPointer, int alphaBlendMode);

	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr materialPointer);

	[EngineMethod("set_shader", false, null, false)]
	void SetShader(UIntPtr materialPointer, UIntPtr shaderPointer);

	[EngineMethod("get_shader", false, null, false)]
	Shader GetShader(UIntPtr materialPointer);

	[EngineMethod("get_shader_flags", false, null, false)]
	ulong GetShaderFlags(UIntPtr materialPointer);

	[EngineMethod("set_shader_flags", false, null, false)]
	void SetShaderFlags(UIntPtr materialPointer, ulong shaderFlags);

	[EngineMethod("set_mesh_vector_argument", false, null, false)]
	void SetMeshVectorArgument(UIntPtr materialPointer, float x, float y, float z, float w);

	[EngineMethod("set_texture", false, null, false)]
	void SetTexture(UIntPtr materialPointer, int textureType, UIntPtr texturePointer);

	[EngineMethod("set_texture_at_slot", false, null, false)]
	void SetTextureAtSlot(UIntPtr materialPointer, int textureSlotIndex, UIntPtr texturePointer);

	[EngineMethod("get_texture", false, null, false)]
	Texture GetTexture(UIntPtr materialPointer, int textureType);

	[EngineMethod("set_alpha_test_value", false, null, false)]
	void SetAlphaTestValue(UIntPtr materialPointer, float alphaTestValue);

	[EngineMethod("get_alpha_test_value", false, null, false)]
	float GetAlphaTestValue(UIntPtr materialPointer);

	[EngineMethod("get_flags", false, null, false)]
	MaterialFlags GetFlags(UIntPtr materialPointer);

	[EngineMethod("set_flags", false, null, false)]
	void SetFlags(UIntPtr materialPointer, MaterialFlags flags);

	[EngineMethod("add_material_shader_flag", false, null, false)]
	void AddMaterialShaderFlag(UIntPtr materialPointer, string flagName, bool showErrors);

	[EngineMethod("remove_material_shader_flag", false, null, false)]
	void RemoveMaterialShaderFlag(UIntPtr materialPointer, string flagName);

	[EngineMethod("set_area_map_scale", false, null, false)]
	void SetAreaMapScale(UIntPtr materialPointer, float scale);

	[EngineMethod("set_enable_skinning", false, null, false)]
	void SetEnableSkinning(UIntPtr materialPointer, bool enable);

	[EngineMethod("using_skinning", false, null, false)]
	bool UsingSkinning(UIntPtr materialPointer);
}
