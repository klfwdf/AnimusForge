namespace TaleWorlds.Library;

public static class ColorExtensions
{
	public static Color AddFactorInHSB(this Color rgbColor, float hueDifference, float saturationDifference, float brighnessDifference)
	{
		Vec3 vec = MBMath.RGBtoHSB(rgbColor);
		vec.x = (vec.x + hueDifference + 360f) % 360f;
		vec.y = MBMath.ClampFloat(vec.y + saturationDifference, 0f, 1f);
		vec.z = MBMath.ClampFloat(vec.z + brighnessDifference, 0f, 1f);
		return MBMath.HSBtoRGB(vec.x, vec.y, vec.z, rgbColor.Alpha);
	}
}
