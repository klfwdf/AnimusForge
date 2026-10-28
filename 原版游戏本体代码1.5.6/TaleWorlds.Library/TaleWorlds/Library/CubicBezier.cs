using System;

namespace TaleWorlds.Library;

public class CubicBezier
{
	private readonly double _x1;

	private readonly double _y1;

	private readonly double _x2;

	private readonly double _y2;

	private readonly double _y0;

	private readonly double _y3;

	private const int NewtonIterations = 4;

	private const double NewtonMinSlope = 0.001;

	private const double SubdivisionPrecision = 1E-07;

	private const int SubdivisionMaxIterations = 10;

	private const int KSplineTableSize = 11;

	private const double KSampleStepSize = 0.1;

	private readonly double[] _sampleValues = new double[11];

	public static CubicBezier CreateEase(double controlPoint1X, double controlPoint1Y, double controlPoint2X, double controlPoint2Y)
	{
		return new CubicBezier(controlPoint1X, controlPoint1Y, controlPoint2X, controlPoint2Y);
	}

	public static CubicBezier CreateYBeginToYEndWithRelativeControlDirs(double yBegin, double yEnd, double controlDir1X, double controlDir1Y, double controlDir2X, double controlDir2Y)
	{
		return new CubicBezier(controlDir1X, yBegin + controlDir1Y, 1.0 + controlDir2X, yEnd + controlDir2Y, yBegin, yEnd);
	}

	public static CubicBezier CreateYBeginToYEnd(double yBegin, double yEnd, double controlPoint1X, double controlPoint1Y, double controlPoint2X, double controlPoint2Y)
	{
		return new CubicBezier(controlPoint1X, controlPoint1Y, controlPoint2X, controlPoint2Y, yBegin, yEnd);
	}

	private CubicBezier(double x1, double y1, double x2, double y2, double yBegin = 0.0, double yEnd = 1.0)
	{
		_y0 = yBegin;
		_y3 = yEnd;
		_x1 = x1;
		_y1 = y1;
		_x2 = x2;
		_y2 = y2;
		if (!(0.0 <= _x1) || !(_x1 <= 1.0) || !(0.0 <= _x2) || !(_x2 <= 1.0))
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\CubicBezier.cs", ".ctor", 129);
			throw new ArgumentOutOfRangeException();
		}
		for (int i = 0; i < 11; i++)
		{
			_sampleValues[i] = CalcBezierX((double)i * 0.1, _x1, _x2);
		}
	}

	public double Sample(double x)
	{
		if (AlmostEq(x, 0.0))
		{
			return _y0;
		}
		if (AlmostEq(x, 1.0))
		{
			return _y3;
		}
		double num = _y3 - _y0;
		if (AlmostEq(_x1 * num, _y1 - _y0) && AlmostEq((1.0 - _x2) * num, _y3 - _y2))
		{
			return _y0 + x * num;
		}
		return CalcBezierY(GetTForX(x), _y0, _y1, _y2, _y3);
	}

	private static bool AlmostEq(double a, double b)
	{
		return MathF.Abs(a - b) < 1E-09;
	}

	private static double AY(double aA0, double aA1, double aA2, double aA3)
	{
		return 1.0 * aA3 - 3.0 * aA2 + 3.0 * aA1 - 1.0 * aA0;
	}

	private static double A(double aA1, double aA2)
	{
		return 1.0 - 3.0 * aA2 + 3.0 * aA1;
	}

	private static double BY(double aA0, double aA1, double aA2)
	{
		return 3.0 * aA2 - 6.0 * aA1 + 3.0 * aA0;
	}

	private static double B(double aA1, double aA2)
	{
		return 3.0 * aA2 - 6.0 * aA1;
	}

	private static double CY(double aA0, double aA1)
	{
		return 3.0 * aA1 - 3.0 * aA0;
	}

	private static double C(double aA1)
	{
		return 3.0 * aA1;
	}

	private static double DY(double aA0)
	{
		return aA0;
	}

	private static double CalcBezierY(double aT, double aA0, double aA1, double aA2, double aA3)
	{
		return ((AY(aA0, aA1, aA2, aA3) * aT + BY(aA0, aA1, aA2)) * aT + CY(aA0, aA1)) * aT + DY(aA0);
	}

	private static double CalcBezierX(double aT, double aA1, double aA2)
	{
		return ((A(aA1, aA2) * aT + B(aA1, aA2)) * aT + C(aA1)) * aT;
	}

	private static double GetSlopeX(double aT, double aA1, double aA2)
	{
		return 3.0 * A(aA1, aA2) * aT * aT + 2.0 * B(aA1, aA2) * aT + C(aA1);
	}

	private static double BinarySubdivide(double aX, double aA, double aB, double mX1, double mX2)
	{
		int num = 0;
		double num2;
		double num3;
		do
		{
			num2 = aA + (aB - aA) / 2.0;
			num3 = CalcBezierX(num2, mX1, mX2) - aX;
			if (num3 > 0.0)
			{
				aB = num2;
			}
			else
			{
				aA = num2;
			}
		}
		while (MathF.Abs(num3) > 1E-07 && ++num < 10);
		return num2;
	}

	private double NewtonRaphsonIterate(double aX, double aGuessT, double mX1, double mX2)
	{
		for (int i = 0; i < 4; i++)
		{
			double slopeX = GetSlopeX(aGuessT, mX1, mX2);
			if (MathF.Abs(slopeX) < 1E-07)
			{
				return aGuessT;
			}
			double num = CalcBezierX(aGuessT, mX1, mX2) - aX;
			aGuessT -= num / slopeX;
		}
		return aGuessT;
	}

	private double GetTForX(double aX)
	{
		double num = 0.0;
		int i = 1;
		for (int num2 = 10; i != num2 && _sampleValues[i] <= aX; i++)
		{
			num += 0.1;
		}
		i--;
		double num3 = (aX - _sampleValues[i]) / (_sampleValues[i + 1] - _sampleValues[i]);
		double num4 = num + num3 * 0.1;
		double slopeX = GetSlopeX(num4, _x1, _x2);
		if (slopeX >= 0.001)
		{
			return NewtonRaphsonIterate(aX, num4, _x1, _x2);
		}
		if (AlmostEq(slopeX, 0.0))
		{
			return num4;
		}
		return BinarySubdivide(aX, num, num + 0.1, _x1, _x2);
	}
}
