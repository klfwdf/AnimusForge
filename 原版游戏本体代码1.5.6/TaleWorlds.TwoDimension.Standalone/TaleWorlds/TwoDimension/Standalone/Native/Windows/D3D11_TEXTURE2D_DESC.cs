namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct D3D11_TEXTURE2D_DESC
{
	public uint Width;

	public uint Height;

	public uint MipLevels;

	public uint ArraySize;

	public uint Format;

	public DXGI_SAMPLE_DESC SampleDesc;

	public uint Usage;

	public uint BindFlags;

	public uint CPUAccessFlags;

	public uint MiscFlags;
}
