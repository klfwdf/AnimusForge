using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("rglScript_field_type", false, null)]
public enum RglScriptFieldType
{
	RglSftInvalid = -1,
	RglSftString,
	RglSftDouble,
	RglSftFloat,
	RglSftBool,
	RglSftInt,
	[CustomEngineStructMemberData("Rgl_sft_vec3")]
	RglSftVec3,
	RglSftEntity,
	RglSftTexture,
	RglSftMesh,
	RglSftEnum,
	RglSftMaterial,
	RglSftButton,
	RglSftColor,
	RglSftMatrixFrame
}
