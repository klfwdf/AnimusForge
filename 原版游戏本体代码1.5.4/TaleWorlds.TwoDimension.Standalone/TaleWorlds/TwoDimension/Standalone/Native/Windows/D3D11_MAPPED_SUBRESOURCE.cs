using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct D3D11_MAPPED_SUBRESOURCE
{
	public IntPtr pData;

	public uint RowPitch;

	public uint DepthPitch;
}
