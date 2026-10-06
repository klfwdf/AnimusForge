using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[EngineStruct("rglWater_renderer::Volume_data_for_submerge_computation", false, null)]
public struct VolumeDataForSubmergeComputation
{
	public Vec3 DynamicLocalBottomPos;

	public MatrixFrame LocalFrame;

	public Vec3 LocalScale;

	public FloaterVolumeDynamicUpAxis DynamicUpAxis;

	public Vec3 OutGlobalWaterSurfaceNormal;

	public float InOutWaterHeightWrtVolume;

	public float Height => LocalScale[(int)DynamicUpAxis];

	public float Width => LocalScale[(int)(DynamicUpAxis + 1) % 3];

	public float Depth => LocalScale[(int)(DynamicUpAxis + 2) % 3];

	public Vec3 Up => LocalFrame.rotation[(int)DynamicUpAxis];

	public Vec3 Side => LocalFrame.rotation[(int)(DynamicUpAxis + 1) % 3];

	public Vec3 Forward => LocalFrame.rotation[(int)(DynamicUpAxis + 2) % 3];
}
