namespace TaleWorlds.Library;

public struct Mat2
{
	public Vec2 s;

	public Vec2 f;

	public static readonly Mat2 Identity = new Mat2(1f, 0f, 0f, 1f);

	public Mat2(float sx, float sy, float fx, float fy)
	{
		s.x = sx;
		s.y = sy;
		f.x = fx;
		f.y = fy;
	}

	public void RotateCounterClockWise(float a)
	{
		MathF.SinCos(a, out var sa, out var ca);
		Vec2 vec = s * ca + f * sa;
		Vec2 vec2 = f * ca - s * sa;
		s = vec;
		f = vec2;
	}
}
