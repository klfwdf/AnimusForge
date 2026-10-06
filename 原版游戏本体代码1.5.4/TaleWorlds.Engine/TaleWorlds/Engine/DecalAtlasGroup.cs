using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglAtlas_packer::rglDecal_atlas_group", true, "rgl_atlas", false)]
public enum DecalAtlasGroup
{
	All,
	Worldmap,
	Battle,
	Town,
	Multiplayer,
	Count
}
