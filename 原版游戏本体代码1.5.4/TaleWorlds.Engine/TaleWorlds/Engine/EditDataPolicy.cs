using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglMesh::Edit_data_policy", false, null)]
public enum EditDataPolicy : sbyte
{
	KeepInFile,
	KeepUntilFirstRender
}
