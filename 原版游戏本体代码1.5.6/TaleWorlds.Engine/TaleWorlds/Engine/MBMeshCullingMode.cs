using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglCull_mode", true, "rgl_cull", false)]
public enum MBMeshCullingMode : byte
{
	None,
	Backfaces,
	Frontfaces,
	Count,
	Invalid
}
