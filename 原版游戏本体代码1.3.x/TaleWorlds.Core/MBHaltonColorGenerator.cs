using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x020000AE RID: 174
	public class MBHaltonColorGenerator
	{
		// Token: 0x1700031D RID: 797
		// (get) Token: 0x06000921 RID: 2337 RVA: 0x0001DF3C File Offset: 0x0001C13C
		public int Base
		{
			get
			{
				return this._base;
			}
		}

		// Token: 0x1700031E RID: 798
		// (get) Token: 0x06000922 RID: 2338 RVA: 0x0001DF44 File Offset: 0x0001C144
		public float Offset
		{
			get
			{
				return this._offset;
			}
		}

		// Token: 0x06000923 RID: 2339 RVA: 0x0001DF4C File Offset: 0x0001C14C
		public MBHaltonColorGenerator()
		{
			this.SetRandomOffset();
			this.SetBase();
		}

		// Token: 0x06000924 RID: 2340 RVA: 0x0001DF60 File Offset: 0x0001C160
		public void SetBase()
		{
			this._base = 2;
		}

		// Token: 0x06000925 RID: 2341 RVA: 0x0001DF69 File Offset: 0x0001C169
		public void SetBase(int baseValue)
		{
			this._base = baseValue;
		}

		// Token: 0x06000926 RID: 2342 RVA: 0x0001DF72 File Offset: 0x0001C172
		public void SetOffset(float offset)
		{
			this._offset = MathF.Clamp(offset, 0f, 1f);
		}

		// Token: 0x06000927 RID: 2343 RVA: 0x0001DF8A File Offset: 0x0001C18A
		public void SetRandomOffset()
		{
			this._offset = MBRandom.RandomFloat;
		}

		// Token: 0x06000928 RID: 2344 RVA: 0x0001DF97 File Offset: 0x0001C197
		public Color GetColor(int index, int maxIndex)
		{
			return Color.FromHSV(MBHaltonColorGenerator.HaltonSequence(((float)index / (float)maxIndex + this._offset) % 1f, this._base), 1f, 1f);
		}

		// Token: 0x06000929 RID: 2345 RVA: 0x0001DFC8 File Offset: 0x0001C1C8
		private static float HaltonSequence(float normalizedIndex, int baseValue)
		{
			float num = 1f;
			float num2 = 0f;
			for (float num3 = normalizedIndex * (float)baseValue; num3 > 0f; num3 = (float)Math.Floor((double)(num3 / (float)baseValue)))
			{
				num /= (float)baseValue;
				num2 += num3 % (float)baseValue * num;
			}
			return num2;
		}

		// Token: 0x04000520 RID: 1312
		public const int DefaultBase = 2;

		// Token: 0x04000521 RID: 1313
		private int _base;

		// Token: 0x04000522 RID: 1314
		private float _offset;
	}
}
