using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000049 RID: 73
	[Flags]
	[EngineStruct("rglBody_flags", true, "rgl_bf", false)]
	public enum BodyFlags : uint
	{
		// Token: 0x0400007C RID: 124
		None = 0U,
		// Token: 0x0400007D RID: 125
		Disabled = 1U,
		// Token: 0x0400007E RID: 126
		NotDestructible = 2U,
		// Token: 0x0400007F RID: 127
		TwoSided = 4U,
		// Token: 0x04000080 RID: 128
		Dynamic = 8U,
		// Token: 0x04000081 RID: 129
		Moveable = 16U,
		// Token: 0x04000082 RID: 130
		DynamicConvexHull = 32U,
		// Token: 0x04000083 RID: 131
		Ladder = 64U,
		// Token: 0x04000084 RID: 132
		OnlyCollideWithRaycast = 128U,
		// Token: 0x04000085 RID: 133
		[CustomEngineStructMemberData("ai_limiter")]
		AILimiter = 256U,
		// Token: 0x04000086 RID: 134
		Barrier = 512U,
		// Token: 0x04000087 RID: 135
		Barrier3D = 1024U,
		// Token: 0x04000088 RID: 136
		HasSteps = 2048U,
		// Token: 0x04000089 RID: 137
		Ragdoll = 4096U,
		// Token: 0x0400008A RID: 138
		RagdollLimiter = 8192U,
		// Token: 0x0400008B RID: 139
		DestructibleDoor = 16384U,
		// Token: 0x0400008C RID: 140
		DroppedItem = 32768U,
		// Token: 0x0400008D RID: 141
		DoNotCollideWithRaycast = 65536U,
		// Token: 0x0400008E RID: 142
		DontTransferToPhysicsEngine = 131072U,
		// Token: 0x0400008F RID: 143
		DontCollideWithCamera = 262144U,
		// Token: 0x04000090 RID: 144
		ExcludePathSnap = 524288U,
		// Token: 0x04000091 RID: 145
		WaterBody = 1048576U,
		// Token: 0x04000092 RID: 146
		AfterAddFlags = 0U,
		// Token: 0x04000093 RID: 147
		AgentOnly = 2097152U,
		// Token: 0x04000094 RID: 148
		MissileOnly = 4194304U,
		// Token: 0x04000095 RID: 149
		HasMaterial = 8388608U,
		// Token: 0x04000096 RID: 150
		IgnoreSoundOcclusion = 268435456U,
		// Token: 0x04000097 RID: 151
		StealthBox = 536870912U,
		// Token: 0x04000098 RID: 152
		Sinking = 1073741824U,
		// Token: 0x04000099 RID: 153
		FloatingDebris = 2147483648U,
		// Token: 0x0400009A RID: 154
		BodyFlagFilter = 4043309055U,
		// Token: 0x0400009B RID: 155
		BodyOwnerNone = 0U,
		// Token: 0x0400009C RID: 156
		BodyOwnerEntity = 16777216U,
		// Token: 0x0400009D RID: 157
		BodyOwnerTerrain = 33554432U,
		// Token: 0x0400009E RID: 158
		BodyOwnerFlora = 67108864U,
		// Token: 0x0400009F RID: 159
		BodyOwnerFilter = 251658240U,
		// Token: 0x040000A0 RID: 160
		CommonCollisionExcludeFlags = 544321929U,
		// Token: 0x040000A1 RID: 161
		CameraCollisionRayCastExludeFlags = 544323529U,
		// Token: 0x040000A2 RID: 162
		CommonCollisionExcludeFlagsForAgent = 542224777U,
		// Token: 0x040000A3 RID: 163
		CommonCollisionExcludeFlagsForMissile = 540129161U,
		// Token: 0x040000A4 RID: 164
		CommonCollisionExcludeFlagsForCombat = 540127625U,
		// Token: 0x040000A5 RID: 165
		CommonCollisionExcludeFlagsForEditor = 540127625U,
		// Token: 0x040000A6 RID: 166
		CommonFlagsThatDoNotBlockRay = 4043259711U,
		// Token: 0x040000A7 RID: 167
		CommonFocusRayCastExcludeFlags = 79617U
	}
}
