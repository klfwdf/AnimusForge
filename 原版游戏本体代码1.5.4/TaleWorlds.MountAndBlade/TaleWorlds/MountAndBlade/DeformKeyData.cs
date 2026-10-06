using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Deform_Key_Data", false, null)]
public struct DeformKeyData
{
	public int GroupId;

	public int KeyTimePoint;

	public float KeyMin;

	public float KeyMax;

	public float Value;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
	public string Id;
}
