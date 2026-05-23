using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000022 RID: 34
	public static class ColorExtensions
	{
		// Token: 0x060000C4 RID: 196 RVA: 0x0000468C File Offset: 0x0000288C
		public static Color AddFactorInHSB(this Color rgbColor, float hueDifference, float saturationDifference, float brighnessDifference)
		{
			Vec3 vec = MBMath.RGBtoHSB(rgbColor);
			vec.x = (vec.x + hueDifference + 360f) % 360f;
			vec.y = MBMath.ClampFloat(vec.y + saturationDifference, 0f, 1f);
			vec.z = MBMath.ClampFloat(vec.z + brighnessDifference, 0f, 1f);
			return MBMath.HSBtoRGB(vec.x, vec.y, vec.z, rgbColor.Alpha);
		}
	}
}
