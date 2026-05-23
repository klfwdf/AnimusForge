using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200001C RID: 28
	[ApplicationInterfaceBase]
	internal interface IMaterial
	{
		// Token: 0x0600014B RID: 331
		[EngineMethod("create_copy", false, null, false)]
		Material CreateCopy(UIntPtr materialPointer);

		// Token: 0x0600014C RID: 332
		[EngineMethod("get_from_resource", false, null, false)]
		Material GetFromResource(string materialName);

		// Token: 0x0600014D RID: 333
		[EngineMethod("get_default_material", false, null, false)]
		Material GetDefaultMaterial();

		// Token: 0x0600014E RID: 334
		[EngineMethod("get_outline_material", false, null, false)]
		Material GetOutlineMaterial(UIntPtr materialPointer);

		// Token: 0x0600014F RID: 335
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr materialPointer);

		// Token: 0x06000150 RID: 336
		[EngineMethod("set_name", false, null, false)]
		void SetName(UIntPtr materialPointer, string name);

		// Token: 0x06000151 RID: 337
		[EngineMethod("get_alpha_blend_mode", false, null, false)]
		int GetAlphaBlendMode(UIntPtr materialPointer);

		// Token: 0x06000152 RID: 338
		[EngineMethod("set_alpha_blend_mode", false, null, false)]
		void SetAlphaBlendMode(UIntPtr materialPointer, int alphaBlendMode);

		// Token: 0x06000153 RID: 339
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr materialPointer);

		// Token: 0x06000154 RID: 340
		[EngineMethod("set_shader", false, null, false)]
		void SetShader(UIntPtr materialPointer, UIntPtr shaderPointer);

		// Token: 0x06000155 RID: 341
		[EngineMethod("get_shader", false, null, false)]
		Shader GetShader(UIntPtr materialPointer);

		// Token: 0x06000156 RID: 342
		[EngineMethod("get_shader_flags", false, null, false)]
		ulong GetShaderFlags(UIntPtr materialPointer);

		// Token: 0x06000157 RID: 343
		[EngineMethod("set_shader_flags", false, null, false)]
		void SetShaderFlags(UIntPtr materialPointer, ulong shaderFlags);

		// Token: 0x06000158 RID: 344
		[EngineMethod("set_mesh_vector_argument", false, null, false)]
		void SetMeshVectorArgument(UIntPtr materialPointer, float x, float y, float z, float w);

		// Token: 0x06000159 RID: 345
		[EngineMethod("set_texture", false, null, false)]
		void SetTexture(UIntPtr materialPointer, int textureType, UIntPtr texturePointer);

		// Token: 0x0600015A RID: 346
		[EngineMethod("set_texture_at_slot", false, null, false)]
		void SetTextureAtSlot(UIntPtr materialPointer, int textureSlotIndex, UIntPtr texturePointer);

		// Token: 0x0600015B RID: 347
		[EngineMethod("get_texture", false, null, false)]
		Texture GetTexture(UIntPtr materialPointer, int textureType);

		// Token: 0x0600015C RID: 348
		[EngineMethod("set_alpha_test_value", false, null, false)]
		void SetAlphaTestValue(UIntPtr materialPointer, float alphaTestValue);

		// Token: 0x0600015D RID: 349
		[EngineMethod("get_alpha_test_value", false, null, false)]
		float GetAlphaTestValue(UIntPtr materialPointer);

		// Token: 0x0600015E RID: 350
		[EngineMethod("get_flags", false, null, false)]
		MaterialFlags GetFlags(UIntPtr materialPointer);

		// Token: 0x0600015F RID: 351
		[EngineMethod("set_flags", false, null, false)]
		void SetFlags(UIntPtr materialPointer, MaterialFlags flags);

		// Token: 0x06000160 RID: 352
		[EngineMethod("add_material_shader_flag", false, null, false)]
		void AddMaterialShaderFlag(UIntPtr materialPointer, string flagName, bool showErrors);

		// Token: 0x06000161 RID: 353
		[EngineMethod("remove_material_shader_flag", false, null, false)]
		void RemoveMaterialShaderFlag(UIntPtr materialPointer, string flagName);

		// Token: 0x06000162 RID: 354
		[EngineMethod("set_area_map_scale", false, null, false)]
		void SetAreaMapScale(UIntPtr materialPointer, float scale);

		// Token: 0x06000163 RID: 355
		[EngineMethod("set_enable_skinning", false, null, false)]
		void SetEnableSkinning(UIntPtr materialPointer, bool enable);

		// Token: 0x06000164 RID: 356
		[EngineMethod("using_skinning", false, null, false)]
		bool UsingSkinning(UIntPtr materialPointer);
	}
}
