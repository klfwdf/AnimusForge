using System.Numerics;

namespace TaleWorlds.TwoDimension;

public struct SimpleRectangle
{
	public float X;

	public float Y;

	public float X2;

	public float Y2;

	public float Width => X2 - X;

	public float Height => Y2 - Y;

	public SimpleRectangle(float x, float y, float width, float height)
	{
		X = x;
		Y = y;
		X2 = x + width;
		Y2 = y + height;
	}

	public bool IsCollide(SimpleRectangle other)
	{
		if (!(other.X > X2) && !(other.X2 < X) && !(other.Y > Y2))
		{
			return !(other.Y2 < Y);
		}
		return false;
	}

	public Vector2 GetCenter()
	{
		return new Vector2((X + X2) * 0.5f, (Y + Y2) * 0.5f);
	}

	public bool IsSubRectOf(SimpleRectangle other)
	{
		if (other.X <= X && other.X2 >= X2 && other.Y <= Y)
		{
			return other.Y2 >= Y2;
		}
		return false;
	}

	public bool IsValid()
	{
		if (Width > 0f)
		{
			return Height > 0f;
		}
		return false;
	}

	public bool IsPointInside(Vector2 point)
	{
		if (point.X >= X && point.Y >= Y && point.X <= X2)
		{
			return point.Y <= Y2;
		}
		return false;
	}

	public void ReduceToIntersection(SimpleRectangle other)
	{
		X = Mathf.Max(X, other.X);
		Y = Mathf.Max(Y, other.Y);
		X2 = Mathf.Min(Y2, other.Y2);
		Y2 = Mathf.Min(Y2, other.Y2);
	}

	public static SimpleRectangle Lerp(SimpleRectangle from, SimpleRectangle to, float ratio)
	{
		return new SimpleRectangle(Mathf.Lerp(from.X, to.X, ratio), Mathf.Lerp(from.Y, to.Y, ratio), Mathf.Lerp(from.Width, to.Width, ratio), Mathf.Lerp(from.Height, to.Height, ratio));
	}
}
