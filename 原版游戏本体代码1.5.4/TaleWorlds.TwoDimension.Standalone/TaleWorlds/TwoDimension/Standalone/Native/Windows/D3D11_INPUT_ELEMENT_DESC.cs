using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public struct D3D11_INPUT_ELEMENT_DESC
{
	[MarshalAs(UnmanagedType.LPStr)]
	public string SemanticName;

	public uint SemanticIndex;

	public uint Format;

	public uint InputSlot;

	public uint AlignedByteOffset;

	public uint InputSlotClass;

	public uint InstanceDataStepRate;
}
