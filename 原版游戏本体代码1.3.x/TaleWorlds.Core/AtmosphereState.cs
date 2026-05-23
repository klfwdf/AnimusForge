using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x0200000E RID: 14
	public class AtmosphereState
	{
		// Token: 0x0600007C RID: 124 RVA: 0x00002D7B File Offset: 0x00000F7B
		public AtmosphereState()
		{
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00002DB0 File Offset: 0x00000FB0
		public AtmosphereState(Vec3 position, float tempAv, float tempVar, float humAv, float humVar, string colorGradeTexture)
		{
			this.Position = position;
			this.TemperatureAverage = tempAv;
			this.TemperatureVariance = tempVar;
			this.HumidityAverage = humAv;
			this.HumidityVariance = humVar;
			this.ColorGradeTexture = colorGradeTexture;
		}

		// Token: 0x040000F1 RID: 241
		public Vec3 Position = Vec3.Zero;

		// Token: 0x040000F2 RID: 242
		public float TemperatureAverage;

		// Token: 0x040000F3 RID: 243
		public float TemperatureVariance;

		// Token: 0x040000F4 RID: 244
		public float HumidityAverage;

		// Token: 0x040000F5 RID: 245
		public float HumidityVariance;

		// Token: 0x040000F6 RID: 246
		public float distanceForMaxWeight = 1f;

		// Token: 0x040000F7 RID: 247
		public float distanceForMinWeight = 1f;

		// Token: 0x040000F8 RID: 248
		public string ColorGradeTexture = "";
	}
}
