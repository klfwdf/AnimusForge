using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct D3D11_SUBRESOURCE_DATA
{
	public IntPtr pSysMem;

	public uint SysMemPitch;

	public uint SysMemSlicePitch;
}
