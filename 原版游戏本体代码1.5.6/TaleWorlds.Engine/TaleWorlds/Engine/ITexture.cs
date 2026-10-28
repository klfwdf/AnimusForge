using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ITexture
{
	[EngineMethod("get_cur_object", false, null, false)]
	void GetCurObject(UIntPtr texturePointer, bool blocking);

	[EngineMethod("get_from_resource", false, null, false)]
	Texture GetFromResource(string textureName);

	[EngineMethod("load_texture_from_path", false, null, false)]
	Texture LoadTextureFromPath(string fileName, string folder);

	[EngineMethod("check_and_get_from_resource", false, null, false)]
	Texture CheckAndGetFromResource(string textureName);

	[EngineMethod("get_sdf_bounding_box_data", false, null, false)]
	void GetSDFBoundingBoxData(UIntPtr texturePointer, ref Vec3 min, ref Vec3 max);

	[EngineMethod("get_name", false, null, false)]
	string GetName(UIntPtr texturePointer);

	[EngineMethod("set_name", false, null, false)]
	void SetName(UIntPtr texturePointer, string name);

	[EngineMethod("get_width", false, null, true)]
	int GetWidth(UIntPtr texturePointer);

	[EngineMethod("get_height", false, null, false)]
	int GetHeight(UIntPtr texturePointer);

	[EngineMethod("get_memory_size", false, null, false)]
	int GetMemorySize(UIntPtr texturePointer);

	[EngineMethod("is_render_target", false, null, false)]
	bool IsRenderTarget(UIntPtr texturePointer);

	[EngineMethod("release_next_frame", false, null, false)]
	void ReleaseNextFrame(UIntPtr texturePointer);

	[EngineMethod("release_after_number_of_frames", false, null, false)]
	void ReleaseAfterNumberOfFrames(UIntPtr texturePointer, int numberOfFrames);

	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr texturePointer);

	[EngineMethod("create_render_target", false, null, false)]
	Texture CreateRenderTarget(string name, int width, int height, bool autoMipmaps, bool isTableau, bool createUninitialized, bool always_valid);

	[EngineMethod("create_depth_target", false, null, false)]
	Texture CreateDepthTarget(string name, int width, int height);

	[EngineMethod("create_from_byte_array", false, null, false)]
	Texture CreateFromByteArray(byte[] data, int width, int height);

	[EngineMethod("create_from_memory", false, null, false)]
	Texture CreateFromMemory(byte[] data);

	[EngineMethod("save_to_file", false, null, false)]
	void SaveToFile(UIntPtr texturePointer, string fileName, bool isRelativePath);

	[EngineMethod("set_texture_as_always_valid", false, null, false)]
	void SaveTextureAsAlwaysValid(UIntPtr texturePointer);

	[EngineMethod("release_gpu_memories", false, null, false)]
	void ReleaseGpuMemories();

	[EngineMethod("transform_render_target_to_resource_texture", false, null, false)]
	void TransformRenderTargetToResourceTexture(UIntPtr texturePointer, string name);

	[EngineMethod("remove_continous_tableau_texture", false, null, false)]
	void RemoveContinousTableauTexture(UIntPtr texturePointer);

	[EngineMethod("set_tableau_view", false, null, false)]
	void SetTableauView(UIntPtr texturePointer, UIntPtr tableauView);

	[EngineMethod("create_texture_from_path", false, null, false)]
	Texture CreateTextureFromPath(PlatformFilePath filePath);

	[EngineMethod("get_pixel_data", false, null, false)]
	void GetPixelData(UIntPtr texturePointer, byte[] bytes);

	[EngineMethod("get_render_target_component", false, null, false)]
	RenderTargetComponent GetRenderTargetComponent(UIntPtr texturePointer);

	[EngineMethod("get_tableau_view", false, null, false)]
	TableauView GetTableauView(UIntPtr texturePointer);

	[EngineMethod("is_loaded", false, null, false)]
	bool IsLoaded(UIntPtr texturePointer);
}
