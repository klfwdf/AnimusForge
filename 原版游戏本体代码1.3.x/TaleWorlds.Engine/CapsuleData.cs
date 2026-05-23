using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000009 RID: 9
	[EngineStruct("rglCapsule_data", false, null)]
	public struct CapsuleData
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002684 File Offset: 0x00000884
		// (set) Token: 0x0600002F RID: 47 RVA: 0x00002691 File Offset: 0x00000891
		public Vec3 P1
		{
			get
			{
				return this._globalData.P1;
			}
			set
			{
				this._globalData.P1 = value;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000030 RID: 48 RVA: 0x0000269F File Offset: 0x0000089F
		// (set) Token: 0x06000031 RID: 49 RVA: 0x000026AC File Offset: 0x000008AC
		public Vec3 P2
		{
			get
			{
				return this._globalData.P2;
			}
			set
			{
				this._globalData.P2 = value;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000032 RID: 50 RVA: 0x000026BA File Offset: 0x000008BA
		// (set) Token: 0x06000033 RID: 51 RVA: 0x000026C7 File Offset: 0x000008C7
		public float Radius
		{
			get
			{
				return this._globalData.Radius;
			}
			set
			{
				this._globalData.Radius = value;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000034 RID: 52 RVA: 0x000026D5 File Offset: 0x000008D5
		// (set) Token: 0x06000035 RID: 53 RVA: 0x000026E2 File Offset: 0x000008E2
		internal float LocalRadius
		{
			get
			{
				return this._localData.Radius;
			}
			set
			{
				this._localData.Radius = value;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000036 RID: 54 RVA: 0x000026F0 File Offset: 0x000008F0
		// (set) Token: 0x06000037 RID: 55 RVA: 0x000026FD File Offset: 0x000008FD
		internal Vec3 LocalP1
		{
			get
			{
				return this._localData.P1;
			}
			set
			{
				this._localData.P1 = value;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000038 RID: 56 RVA: 0x0000270B File Offset: 0x0000090B
		// (set) Token: 0x06000039 RID: 57 RVA: 0x00002718 File Offset: 0x00000918
		internal Vec3 LocalP2
		{
			get
			{
				return this._localData.P2;
			}
			set
			{
				this._localData.P2 = value;
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002726 File Offset: 0x00000926
		public CapsuleData(float radius, Vec3 p1, Vec3 p2)
		{
			this._globalData = new FtlCapsuleData(radius, p1, p2);
			this._localData = new FtlCapsuleData(radius, p1, p2);
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002744 File Offset: 0x00000944
		public ValueTuple<Vec3, Vec3> GetBoxMinMax()
		{
			ValueTuple<float, float> valueTuple = MathF.MinMax(this.P1.x, this.P2.x);
			float item = valueTuple.Item1;
			float item2 = valueTuple.Item2;
			ValueTuple<float, float> valueTuple2 = MathF.MinMax(this.P1.y, this.P2.y);
			float item3 = valueTuple2.Item1;
			float item4 = valueTuple2.Item2;
			ValueTuple<float, float> valueTuple3 = MathF.MinMax(this.P1.z, this.P2.z);
			float item5 = valueTuple3.Item1;
			float item6 = valueTuple3.Item2;
			Vec3 item7 = new Vec3(item - this.Radius, item3 - this.Radius, item5 - this.Radius, -1f);
			Vec3 item8 = new Vec3(item2 + this.Radius, item4 + this.Radius, item6 + this.Radius, -1f);
			return new ValueTuple<Vec3, Vec3>(item7, item8);
		}

		// Token: 0x04000009 RID: 9
		private FtlCapsuleData _globalData;

		// Token: 0x0400000A RID: 10
		private FtlCapsuleData _localData;
	}
}
