using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000085 RID: 133
	public struct Ray
	{
		// Token: 0x1700007F RID: 127
		// (get) Token: 0x060004D0 RID: 1232 RVA: 0x00011CA9 File Offset: 0x0000FEA9
		// (set) Token: 0x060004D1 RID: 1233 RVA: 0x00011CB1 File Offset: 0x0000FEB1
		public Vec3 Origin
		{
			get
			{
				return this._origin;
			}
			private set
			{
				this._origin = value;
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x060004D2 RID: 1234 RVA: 0x00011CBA File Offset: 0x0000FEBA
		// (set) Token: 0x060004D3 RID: 1235 RVA: 0x00011CC2 File Offset: 0x0000FEC2
		public Vec3 Direction
		{
			get
			{
				return this._direction;
			}
			private set
			{
				this._direction = value;
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060004D4 RID: 1236 RVA: 0x00011CCB File Offset: 0x0000FECB
		// (set) Token: 0x060004D5 RID: 1237 RVA: 0x00011CD3 File Offset: 0x0000FED3
		public float MaxDistance
		{
			get
			{
				return this._maxDistance;
			}
			private set
			{
				this._maxDistance = value;
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x060004D6 RID: 1238 RVA: 0x00011CDC File Offset: 0x0000FEDC
		public Vec3 EndPoint
		{
			get
			{
				return this.Origin + this.Direction * this.MaxDistance;
			}
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x00011CFA File Offset: 0x0000FEFA
		public Ray(Vec3 origin, Vec3 direction, float maxDistance = 3.4028235E+38f)
		{
			this = default(Ray);
			this.Reset(origin, direction, maxDistance);
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x00011D0C File Offset: 0x0000FF0C
		public Ray(Vec3 origin, Vec3 direction, bool useDirectionLenForMaxDistance)
		{
			this._origin = origin;
			this._direction = direction;
			float maxDistance = this._direction.Normalize();
			if (useDirectionLenForMaxDistance)
			{
				this._maxDistance = maxDistance;
				return;
			}
			this._maxDistance = float.MaxValue;
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x00011D49 File Offset: 0x0000FF49
		public void Reset(Vec3 origin, Vec3 direction, float maxDistance = 3.4028235E+38f)
		{
			this._origin = origin;
			this._direction = direction;
			this._maxDistance = maxDistance;
			this._direction.Normalize();
		}

		// Token: 0x04000174 RID: 372
		private Vec3 _origin;

		// Token: 0x04000175 RID: 373
		private Vec3 _direction;

		// Token: 0x04000176 RID: 374
		private float _maxDistance;
	}
}
