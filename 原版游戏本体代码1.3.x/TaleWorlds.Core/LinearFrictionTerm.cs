using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000098 RID: 152
	public struct LinearFrictionTerm
	{
		// Token: 0x060008D1 RID: 2257 RVA: 0x0001CEE5 File Offset: 0x0001B0E5
		public LinearFrictionTerm(float right, float left, float forward, float backward, float up, float down)
		{
			this.Right = right;
			this.Left = left;
			this.Forward = forward;
			this.Backward = backward;
			this.Up = up;
			this.Down = down;
		}

		// Token: 0x060008D2 RID: 2258 RVA: 0x0001CF14 File Offset: 0x0001B114
		public static LinearFrictionTerm operator /(LinearFrictionTerm o, float f)
		{
			return o * (1f / f);
		}

		// Token: 0x060008D3 RID: 2259 RVA: 0x0001CF23 File Offset: 0x0001B123
		public static LinearFrictionTerm operator *(LinearFrictionTerm o, float f)
		{
			return new LinearFrictionTerm(o.Right * f, o.Left * f, o.Forward * f, o.Backward * f, o.Up * f, o.Down * f);
		}

		// Token: 0x060008D4 RID: 2260 RVA: 0x0001CF5C File Offset: 0x0001B15C
		public LinearFrictionTerm ElementWiseProduct(LinearFrictionTerm o)
		{
			return new LinearFrictionTerm(this.Right * o.Right, this.Left * o.Left, this.Forward * o.Forward, this.Backward * o.Backward, this.Up * o.Up, this.Down * o.Down);
		}

		// Token: 0x1700030E RID: 782
		// (get) Token: 0x060008D5 RID: 2261 RVA: 0x0001CFBC File Offset: 0x0001B1BC
		public bool IsValid
		{
			get
			{
				return this.Right > 0f && this.Left > 0f && this.Forward > 0f && this.Backward > 0f && this.Up > 0f && this.Down > 0f;
			}
		}

		// Token: 0x1700030F RID: 783
		// (get) Token: 0x060008D6 RID: 2262 RVA: 0x0001D019 File Offset: 0x0001B219
		public static LinearFrictionTerm Invalid
		{
			get
			{
				return new LinearFrictionTerm(0f, 0f, 0f, 0f, 0f, 0f);
			}
		}

		// Token: 0x17000310 RID: 784
		// (get) Token: 0x060008D7 RID: 2263 RVA: 0x0001D03E File Offset: 0x0001B23E
		public static LinearFrictionTerm One
		{
			get
			{
				return new LinearFrictionTerm(1f, 1f, 1f, 1f, 1f, 1f);
			}
		}

		// Token: 0x040004A9 RID: 1193
		public readonly float Right;

		// Token: 0x040004AA RID: 1194
		public readonly float Left;

		// Token: 0x040004AB RID: 1195
		public readonly float Forward;

		// Token: 0x040004AC RID: 1196
		public readonly float Backward;

		// Token: 0x040004AD RID: 1197
		public readonly float Up;

		// Token: 0x040004AE RID: 1198
		public readonly float Down;
	}
}
