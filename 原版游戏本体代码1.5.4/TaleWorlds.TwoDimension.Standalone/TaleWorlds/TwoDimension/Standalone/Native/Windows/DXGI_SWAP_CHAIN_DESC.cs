using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct DXGI_SWAP_CHAIN_DESC
{
	public DXGI_MODE_DESC BufferDesc;

	public DXGI_SAMPLE_DESC SampleDesc;

	public uint BufferUsage;

	public uint BufferCount;

	public IntPtr OutputWindow;

	public int Windowed;

	public uint SwapEffect;

	public uint Flags;
}
