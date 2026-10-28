using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rgLMesh_billboard_type", true, "rgl_mesh_billboard", false)]
public enum BillboardType : byte
{
	None,
	Up,
	Full
}
