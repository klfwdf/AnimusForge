using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000047 RID: 71
	[Flags]
	[EngineStruct("rglEntity_flags", true, "rgl_ef", false)]
	public enum EntityFlags : uint
	{
		// Token: 0x0400005C RID: 92
		ForceLodMask = 240U,
		// Token: 0x0400005D RID: 93
		ForceLodBits = 4U,
		// Token: 0x0400005E RID: 94
		NoOcclusionCulling = 512U,
		// Token: 0x0400005F RID: 95
		IsHelper = 1024U,
		// Token: 0x04000060 RID: 96
		ComputePerComponentLod = 2048U,
		// Token: 0x04000061 RID: 97
		DoesNotAffectParentsLocalBb = 4096U,
		// Token: 0x04000062 RID: 98
		ForceAsStatic = 8192U,
		// Token: 0x04000063 RID: 99
		HideInPrefabEditors = 16384U,
		// Token: 0x04000064 RID: 100
		PhysicsDisabled = 32768U,
		// Token: 0x04000065 RID: 101
		AlignToTerrain = 65536U,
		// Token: 0x04000066 RID: 102
		DontSaveToScene = 131072U,
		// Token: 0x04000067 RID: 103
		RecordToSceneReplay = 262144U,
		// Token: 0x04000068 RID: 104
		AffectedByEnvironmentDecals = 524288U,
		// Token: 0x04000069 RID: 105
		SmoothLodTransitions = 1048576U,
		// Token: 0x0400006A RID: 106
		DontCheckHandness = 2097152U,
		// Token: 0x0400006B RID: 107
		NotAffectedBySeason = 4194304U,
		// Token: 0x0400006C RID: 108
		DontTickChildren = 8388608U,
		// Token: 0x0400006D RID: 109
		WaitUntilReady = 16777216U,
		// Token: 0x0400006E RID: 110
		NonModifiableFromEditor = 33554432U,
		// Token: 0x0400006F RID: 111
		PrefabCannotBeBroken = 67108864U,
		// Token: 0x04000070 RID: 112
		PerComponentVisibility = 134217728U,
		// Token: 0x04000071 RID: 113
		Ignore = 268435456U,
		// Token: 0x04000072 RID: 114
		DoNotTick = 536870912U,
		// Token: 0x04000073 RID: 115
		DoNotRenderToEnvmap = 1073741824U,
		// Token: 0x04000074 RID: 116
		AlignRotationToTerrain = 2147483648U
	}
}
