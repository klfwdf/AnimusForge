using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ITwoDimensionView
{
	[EngineMethod("create_twodimension_view", false, null, false)]
	TwoDimensionView CreateTwoDimensionView(string viewName);

	[EngineMethod("begin_frame", false, null, false)]
	void BeginFrame(UIntPtr pointer);

	[EngineMethod("end_frame", false, null, false)]
	void EndFrame(UIntPtr pointer);

	[EngineMethod("clear", false, null, false)]
	void Clear(UIntPtr pointer);

	[EngineMethod("add_new_mesh", false, null, false)]
	void AddNewMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

	[EngineMethod("add_new_quad_mesh", false, null, false)]
	void AddNewQuadMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

	[EngineMethod("add_cached_text_mesh", false, null, false)]
	bool AddCachedTextMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

	[EngineMethod("add_new_text_mesh", false, null, false)]
	void AddNewTextMesh(UIntPtr pointer, float[] vertices, float[] uvs, uint[] indices, int vertexCount, int indexCount, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

	[EngineMethod("get_or_create_material", false, null, false)]
	UIntPtr GetOrCreateMaterial(UIntPtr pointer, UIntPtr mainTexture, UIntPtr overlayTexture);
}
