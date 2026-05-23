using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200005D RID: 93
	[Flags]
	[EngineStruct("rglMaterial_flags", true, "rgl_mf", false)]
	public enum MaterialFlags : uint
	{
		// Token: 0x040000E9 RID: 233
		RenderFrontToBack = 1U,
		// Token: 0x040000EA RID: 234
		NoDepthTest = 2U,
		// Token: 0x040000EB RID: 235
		DontDrawToDepthRenderTarget = 4U,
		// Token: 0x040000EC RID: 236
		NoModifyDepthBuffer = 8U,
		// Token: 0x040000ED RID: 237
		CullFrontFaces = 16U,
		// Token: 0x040000EE RID: 238
		TwoSided = 32U,
		// Token: 0x040000EF RID: 239
		AlphaBlendSort = 64U,
		// Token: 0x040000F0 RID: 240
		DontOptimizeMesh = 128U,
		// Token: 0x040000F1 RID: 241
		DontCastShadow = 256U,
		// Token: 0x040000F2 RID: 242
		DisableStreaming = 512U,
		// Token: 0x040000F3 RID: 243
		BillboardNone = 0U,
		// Token: 0x040000F4 RID: 244
		Billboard_2d = 4096U,
		// Token: 0x040000F5 RID: 245
		Billboard_3d = 8192U,
		// Token: 0x040000F6 RID: 246
		BillboardMask = 12288U,
		// Token: 0x040000F7 RID: 247
		Skybox = 131072U,
		// Token: 0x040000F8 RID: 248
		MultiPassAlpha = 262144U,
		// Token: 0x040000F9 RID: 249
		GbufferAlphaBlend = 524288U,
		// Token: 0x040000FA RID: 250
		RequiresForwardRendering = 1048576U,
		// Token: 0x040000FB RID: 251
		AvoidRecomputationOfNormals = 2097152U,
		// Token: 0x040000FC RID: 252
		RenderOrderPlus_1 = 150994944U,
		// Token: 0x040000FD RID: 253
		RenderOrderPlus_2 = 167772160U,
		// Token: 0x040000FE RID: 254
		RenderOrderPlus_3 = 184549376U,
		// Token: 0x040000FF RID: 255
		RenderOrderPlus_4 = 201326592U,
		// Token: 0x04000100 RID: 256
		RenderOrderPlus_5 = 218103808U,
		// Token: 0x04000101 RID: 257
		RenderOrderPlus_6 = 234881024U,
		// Token: 0x04000102 RID: 258
		RenderOrderPlus_7 = 251658240U,
		// Token: 0x04000103 RID: 259
		GreaterDepthNoWrite = 268435456U,
		// Token: 0x04000104 RID: 260
		[CustomEngineStructMemberData("render_after_postfx")]
		AlwaysDepthTest = 536870912U,
		// Token: 0x04000105 RID: 261
		RenderToAmbientOcclusionBuffer = 1073741824U
	}
}
