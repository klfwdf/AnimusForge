using TaleWorlds.Library;

namespace TaleWorlds.Core;

public struct LinearFrictionTerm
{
	public readonly float Right;

	public readonly float Left;

	public readonly float Forward;

	public readonly float Backward;

	public readonly float Up;

	public readonly float Down;

	public static LinearFrictionTerm Invalid => new LinearFrictionTerm(0f, 0f, 0f, 0f, 0f, 0f);

	public static LinearFrictionTerm One => new LinearFrictionTerm(1f, 1f, 1f, 1f, 1f, 1f);

	public bool IsValid
	{
		get
		{
			if (Right > 0f && Left > 0f && Forward > 0f && Backward > 0f && Up > 0f)
			{
				return Down > 0f;
			}
			return false;
		}
	}

	public LinearFrictionTerm(float right, float left, float forward, float backward, float up, float down)
	{
		Right = right;
		Left = left;
		Forward = forward;
		Backward = backward;
		Up = up;
		Down = down;
	}

	public static LinearFrictionTerm operator /(LinearFrictionTerm o, float f)
	{
		return o * (1f / f);
	}

	public static LinearFrictionTerm operator *(LinearFrictionTerm o, float f)
	{
		return new LinearFrictionTerm(o.Right * f, o.Left * f, o.Forward * f, o.Backward * f, o.Up * f, o.Down * f);
	}

	public LinearFrictionTerm ElementWiseProduct(LinearFrictionTerm o)
	{
		return new LinearFrictionTerm(Right * o.Right, Left * o.Left, Forward * o.Forward, Backward * o.Backward, Up * o.Up, Down * o.Down);
	}

	public bool NearlyEquals(in LinearFrictionTerm o, float epsilon = 1E-05f)
	{
		if (Right.ApproximatelyEqualsTo(o.Right, epsilon) && Left.ApproximatelyEqualsTo(o.Left, epsilon) && Forward.ApproximatelyEqualsTo(o.Forward, epsilon) && Backward.ApproximatelyEqualsTo(o.Backward, epsilon) && Up.ApproximatelyEqualsTo(o.Up, epsilon))
		{
			return Down.ApproximatelyEqualsTo(o.Down, epsilon);
		}
		return false;
	}
}
