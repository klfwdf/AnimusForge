using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IMeshBuilder
{
	[EngineMethod("create_tiling_window_mesh", false, null, false)]
	Mesh CreateTilingWindowMesh(string baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness, ref Vec2 backgroundBorderThickness);

	[EngineMethod("create_tiling_button_mesh", false, null, false)]
	Mesh CreateTilingButtonMesh(string baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness);

	[EngineMethod("finalize_mesh_builder", false, null, false)]
	Mesh FinalizeMeshBuilder(int num_vertices, Vec3[] vertices, int num_face_corners, MeshBuilder.FaceCorner[] faceCorners, int num_faces, MeshBuilder.Face[] faces);
}
