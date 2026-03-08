using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200004D RID: 77
	[EngineStruct("rglWater_renderer::Volume_data_for_submerge_computation", false, null)]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VolumeDataForSubmergeComputation
	{
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060007DD RID: 2013 RVA: 0x00005C31 File Offset: 0x00003E31
		public float Height
		{
			get
			{
				return this.LocalScale[(int)this.DynamicUpAxis];
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060007DE RID: 2014 RVA: 0x00005C44 File Offset: 0x00003E44
		public float Width
		{
			get
			{
				return this.LocalScale[(int)((this.DynamicUpAxis + 1) % (FloaterVolumeDynamicUpAxis)3)];
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060007DF RID: 2015 RVA: 0x00005C5B File Offset: 0x00003E5B
		public float Depth
		{
			get
			{
				return this.LocalScale[(int)((this.DynamicUpAxis + 2) % (FloaterVolumeDynamicUpAxis)3)];
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060007E0 RID: 2016 RVA: 0x00005C72 File Offset: 0x00003E72
		public Vec3 Up
		{
			get
			{
				return this.LocalFrame.rotation[(int)this.DynamicUpAxis];
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060007E1 RID: 2017 RVA: 0x00005C8A File Offset: 0x00003E8A
		public Vec3 Side
		{
			get
			{
				return this.LocalFrame.rotation[(int)((this.DynamicUpAxis + 1) % (FloaterVolumeDynamicUpAxis)3)];
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060007E2 RID: 2018 RVA: 0x00005CA6 File Offset: 0x00003EA6
		public Vec3 Forward
		{
			get
			{
				return this.LocalFrame.rotation[(int)((this.DynamicUpAxis + 2) % (FloaterVolumeDynamicUpAxis)3)];
			}
		}

		// Token: 0x040000AC RID: 172
		public Vec3 DynamicLocalBottomPos;

		// Token: 0x040000AD RID: 173
		public MatrixFrame LocalFrame;

		// Token: 0x040000AE RID: 174
		public Vec3 LocalScale;

		// Token: 0x040000AF RID: 175
		public FloaterVolumeDynamicUpAxis DynamicUpAxis;

		// Token: 0x040000B0 RID: 176
		public Vec3 OutGlobalWaterSurfaceNormal;

		// Token: 0x040000B1 RID: 177
		public float InOutWaterHeightWrtVolume;
	}
}
