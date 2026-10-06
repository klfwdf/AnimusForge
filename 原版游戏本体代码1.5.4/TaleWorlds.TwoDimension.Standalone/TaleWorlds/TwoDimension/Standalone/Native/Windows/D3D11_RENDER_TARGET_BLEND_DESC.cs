namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct D3D11_RENDER_TARGET_BLEND_DESC
{
	public int BlendEnable;

	public uint SrcBlend;

	public uint DestBlend;

	public uint BlendOp;

	public uint SrcBlendAlpha;

	public uint DestBlendAlpha;

	public uint BlendOpAlpha;

	public byte RenderTargetWriteMask;

	private byte _pad0;

	private byte _pad1;

	private byte _pad2;
}
