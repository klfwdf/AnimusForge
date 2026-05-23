using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000028 RID: 40
	public enum TelemetryLevelMask : uint
	{
		// Token: 0x04000085 RID: 133
		All = 4294967295U,
		// Token: 0x04000086 RID: 134
		Level_0 = 1U,
		// Token: 0x04000087 RID: 135
		Level_1,
		// Token: 0x04000088 RID: 136
		Level_2 = 4U,
		// Token: 0x04000089 RID: 137
		Level_3 = 8U,
		// Token: 0x0400008A RID: 138
		Level_4 = 16U,
		// Token: 0x0400008B RID: 139
		Level_5 = 32U,
		// Token: 0x0400008C RID: 140
		Agent = 64U,
		// Token: 0x0400008D RID: 141
		Threading = 128U,
		// Token: 0x0400008E RID: 142
		Application = 256U,
		// Token: 0x0400008F RID: 143
		Graphics = 512U,
		// Token: 0x04000090 RID: 144
		Gui = 1024U,
		// Token: 0x04000091 RID: 145
		Agent_ai = 2048U,
		// Token: 0x04000092 RID: 146
		Mono_0 = 4096U,
		// Token: 0x04000093 RID: 147
		Mono_1 = 8192U,
		// Token: 0x04000094 RID: 148
		Mono_2 = 16384U,
		// Token: 0x04000095 RID: 149
		RenderThread = 32768U,
		// Token: 0x04000096 RID: 150
		Sound = 65536U,
		// Token: 0x04000097 RID: 151
		Idle = 131072U,
		// Token: 0x04000098 RID: 152
		AgentParallel = 262144U,
		// Token: 0x04000099 RID: 153
		AgentTest = 524288U,
		// Token: 0x0400009A RID: 154
		Network = 1048576U,
		// Token: 0x0400009B RID: 155
		Navmesh = 2097152U,
		// Token: 0x0400009C RID: 156
		Memory = 4194304U,
		// Token: 0x0400009D RID: 157
		LevelMaskCount = 24U
	}
}
