using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglWorld_position::z_validity_state", true, "zvs", false)]
public enum ZValidityState
{
	Invalid,
	BatchFormationUnitPosition,
	ValidAccordingToNavMesh,
	Valid
}
