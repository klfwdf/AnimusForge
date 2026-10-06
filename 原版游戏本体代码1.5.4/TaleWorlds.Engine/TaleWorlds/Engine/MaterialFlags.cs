using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[Flags]
[EngineStruct("rglMaterial_flags", true, "rgl_mf", false)]
public enum MaterialFlags : uint
{
	RenderFrontToBack = 1u,
	NoDepthTest = 2u,
	DontDrawToDepthRenderTarget = 4u,
	NoModifyDepthBuffer = 8u,
	CullFrontFaces = 0x10u,
	TwoSided = 0x20u,
	AlphaBlendSort = 0x40u,
	DontOptimizeMesh = 0x80u,
	DontCastShadow = 0x100u,
	DisableStreaming = 0x200u,
	BillboardNone = 0u,
	Billboard_2d = 0x1000u,
	Billboard_3d = 0x2000u,
	BillboardMask = 0x3000u,
	Skybox = 0x20000u,
	MultiPassAlpha = 0x40000u,
	GbufferAlphaBlend = 0x80000u,
	RequiresForwardRendering = 0x100000u,
	AvoidRecomputationOfNormals = 0x200000u,
	RenderOrderPlus_1 = 0x9000000u,
	RenderOrderPlus_2 = 0xA000000u,
	RenderOrderPlus_3 = 0xB000000u,
	RenderOrderPlus_4 = 0xC000000u,
	RenderOrderPlus_5 = 0xD000000u,
	RenderOrderPlus_6 = 0xE000000u,
	RenderOrderPlus_7 = 0xF000000u,
	GreaterDepthNoWrite = 0x10000000u,
	[CustomEngineStructMemberData("render_after_postfx")]
	AlwaysDepthTest = 0x20000000u,
	RenderToAmbientOcclusionBuffer = 0x40000000u
}
