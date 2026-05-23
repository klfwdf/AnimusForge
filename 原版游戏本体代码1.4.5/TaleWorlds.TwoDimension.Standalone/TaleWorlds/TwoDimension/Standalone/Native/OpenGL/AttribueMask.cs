namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL;

internal enum AttribueMask : uint
{
	CurrentBit = 1u,
	PointBit = 2u,
	LineBit = 4u,
	PolygonBit = 8u,
	PolygonStippleBit = 16u,
	PixelModeBit = 32u,
	LightingBit = 64u,
	FogBit = 128u,
	DepthBufferBit = 256u,
	AccumBufferBit = 512u,
	StencilBufferBit = 1024u,
	ViewportBit = 2048u,
	TransformBit = 4096u,
	EnableBit = 8192u,
	ColorBufferBit = 16384u,
	HintBit = 32768u,
	EvalBit = 65536u,
	ListBit = 131072u,
	TextureBit = 262144u,
	ScissorBit = 524288u,
	AllAttribBits = 1048575u
}
