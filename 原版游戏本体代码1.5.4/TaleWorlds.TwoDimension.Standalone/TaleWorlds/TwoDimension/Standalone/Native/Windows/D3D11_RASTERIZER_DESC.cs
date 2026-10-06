namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct D3D11_RASTERIZER_DESC
{
	public uint FillMode;

	public uint CullMode;

	public int FrontCounterClockwise;

	public int DepthBias;

	public float DepthBiasClamp;

	public float SlopeScaledDepthBias;

	public int DepthClipEnable;

	public int ScissorEnable;

	public int MultisampleEnable;

	public int AntialiasedLineEnable;
}
