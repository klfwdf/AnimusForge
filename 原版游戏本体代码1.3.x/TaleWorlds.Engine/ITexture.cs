using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200001B RID: 27
	[ApplicationInterfaceBase]
	internal interface ITexture
	{
		// Token: 0x0600012E RID: 302
		[EngineMethod("get_cur_object", false, null, false)]
		void GetCurObject(UIntPtr texturePointer, bool blocking);

		// Token: 0x0600012F RID: 303
		[EngineMethod("get_from_resource", false, null, false)]
		Texture GetFromResource(string textureName);

		// Token: 0x06000130 RID: 304
		[EngineMethod("load_texture_from_path", false, null, false)]
		Texture LoadTextureFromPath(string fileName, string folder);

		// Token: 0x06000131 RID: 305
		[EngineMethod("check_and_get_from_resource", false, null, false)]
		Texture CheckAndGetFromResource(string textureName);

		// Token: 0x06000132 RID: 306
		[EngineMethod("get_sdf_bounding_box_data", false, null, false)]
		void GetSDFBoundingBoxData(UIntPtr texturePointer, ref Vec3 min, ref Vec3 max);

		// Token: 0x06000133 RID: 307
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr texturePointer);

		// Token: 0x06000134 RID: 308
		[EngineMethod("set_name", false, null, false)]
		void SetName(UIntPtr texturePointer, string name);

		// Token: 0x06000135 RID: 309
		[EngineMethod("get_width", false, null, true)]
		int GetWidth(UIntPtr texturePointer);

		// Token: 0x06000136 RID: 310
		[EngineMethod("get_height", false, null, false)]
		int GetHeight(UIntPtr texturePointer);

		// Token: 0x06000137 RID: 311
		[EngineMethod("get_memory_size", false, null, false)]
		int GetMemorySize(UIntPtr texturePointer);

		// Token: 0x06000138 RID: 312
		[EngineMethod("is_render_target", false, null, false)]
		bool IsRenderTarget(UIntPtr texturePointer);

		// Token: 0x06000139 RID: 313
		[EngineMethod("release_next_frame", false, null, false)]
		void ReleaseNextFrame(UIntPtr texturePointer);

		// Token: 0x0600013A RID: 314
		[EngineMethod("release_after_number_of_frames", false, null, false)]
		void ReleaseAfterNumberOfFrames(UIntPtr texturePointer, int numberOfFrames);

		// Token: 0x0600013B RID: 315
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr texturePointer);

		// Token: 0x0600013C RID: 316
		[EngineMethod("create_render_target", false, null, false)]
		Texture CreateRenderTarget(string name, int width, int height, bool autoMipmaps, bool isTableau, bool createUninitialized, bool always_valid);

		// Token: 0x0600013D RID: 317
		[EngineMethod("create_depth_target", false, null, false)]
		Texture CreateDepthTarget(string name, int width, int height);

		// Token: 0x0600013E RID: 318
		[EngineMethod("create_from_byte_array", false, null, false)]
		Texture CreateFromByteArray(byte[] data, int width, int height);

		// Token: 0x0600013F RID: 319
		[EngineMethod("create_from_memory", false, null, false)]
		Texture CreateFromMemory(byte[] data);

		// Token: 0x06000140 RID: 320
		[EngineMethod("save_to_file", false, null, false)]
		void SaveToFile(UIntPtr texturePointer, string fileName, bool isRelativePath);

		// Token: 0x06000141 RID: 321
		[EngineMethod("set_texture_as_always_valid", false, null, false)]
		void SaveTextureAsAlwaysValid(UIntPtr texturePointer);

		// Token: 0x06000142 RID: 322
		[EngineMethod("release_gpu_memories", false, null, false)]
		void ReleaseGpuMemories();

		// Token: 0x06000143 RID: 323
		[EngineMethod("transform_render_target_to_resource_texture", false, null, false)]
		void TransformRenderTargetToResourceTexture(UIntPtr texturePointer, string name);

		// Token: 0x06000144 RID: 324
		[EngineMethod("remove_continous_tableau_texture", false, null, false)]
		void RemoveContinousTableauTexture(UIntPtr texturePointer);

		// Token: 0x06000145 RID: 325
		[EngineMethod("set_tableau_view", false, null, false)]
		void SetTableauView(UIntPtr texturePointer, UIntPtr tableauView);

		// Token: 0x06000146 RID: 326
		[EngineMethod("create_texture_from_path", false, null, false)]
		Texture CreateTextureFromPath(PlatformFilePath filePath);

		// Token: 0x06000147 RID: 327
		[EngineMethod("get_pixel_data", false, null, false)]
		void GetPixelData(UIntPtr texturePointer, byte[] bytes);

		// Token: 0x06000148 RID: 328
		[EngineMethod("get_render_target_component", false, null, false)]
		RenderTargetComponent GetRenderTargetComponent(UIntPtr texturePointer);

		// Token: 0x06000149 RID: 329
		[EngineMethod("get_tableau_view", false, null, false)]
		TableauView GetTableauView(UIntPtr texturePointer);

		// Token: 0x0600014A RID: 330
		[EngineMethod("is_loaded", false, null, false)]
		bool IsLoaded(UIntPtr texturePointer);
	}
}
