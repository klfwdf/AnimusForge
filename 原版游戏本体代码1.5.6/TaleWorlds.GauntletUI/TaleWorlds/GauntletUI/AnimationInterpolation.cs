using System;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public static class AnimationInterpolation
{
	public enum Type
	{
		Linear,
		EaseIn,
		EaseOut,
		EaseInOut
	}

	public enum Function
	{
		Sine,
		Quad,
		Cubic,
		Quart,
		Quint
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct EaseInInterpolator
	{
		public float Ease(Function function, float t)
		{
			switch (function)
			{
			case Function.Sine:
				return 1f - TaleWorlds.Library.MathF.Cos(t * System.MathF.PI / 2f);
			case Function.Quad:
				return t * t;
			case Function.Cubic:
				return t * t * t;
			case Function.Quart:
				return t * t * t * t;
			case Function.Quint:
				return t * t * t * t * t;
			default:
				Debug.FailedAssert($"Brush ease function not implemented: {function}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\AnimationInterpolation.cs", "Ease", 63);
				return t;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct EaseOutInterpolator
	{
		public float Ease(Function function, float t)
		{
			switch (function)
			{
			case Function.Sine:
				return TaleWorlds.Library.MathF.Sin(t * System.MathF.PI / 2f);
			case Function.Quad:
				return 1f - (1f - t) * (1f - t);
			case Function.Cubic:
				return 1f - TaleWorlds.Library.MathF.Pow(1f - t, 3f);
			case Function.Quart:
				return 1f - TaleWorlds.Library.MathF.Pow(1f - t, 4f);
			case Function.Quint:
				return 1f - TaleWorlds.Library.MathF.Pow(1f - t, 5f);
			default:
				Debug.FailedAssert($"Brush ease function not implemented: {function}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\AnimationInterpolation.cs", "Ease", 86);
				return t;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct EaseInOutInterpolator
	{
		public float Ease(Function function, float t)
		{
			switch (function)
			{
			case Function.Sine:
				return (0f - (TaleWorlds.Library.MathF.Cos(System.MathF.PI * t) - 1f)) / 2f;
			case Function.Quad:
				if (!(t < 0.5f))
				{
					return 1f - TaleWorlds.Library.MathF.Pow(-2f * t + 2f, 2f) / 2f;
				}
				return 2f * t * t;
			case Function.Cubic:
				if (!(t < 0.5f))
				{
					return 1f - TaleWorlds.Library.MathF.Pow(-2f * t + 2f, 3f) / 2f;
				}
				return 4f * t * t * t;
			case Function.Quart:
				if (!(t < 0.5f))
				{
					return 1f - TaleWorlds.Library.MathF.Pow(-2f * t + 2f, 4f) / 2f;
				}
				return 8f * t * t * t * t;
			case Function.Quint:
				if (!(t < 0.5f))
				{
					return 1f - TaleWorlds.Library.MathF.Pow(-2f * t + 2f, 5f) / 2f;
				}
				return 16f * t * t * t * t * t;
			default:
				Debug.FailedAssert($"Brush ease function not implemented: {function}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\AnimationInterpolation.cs", "Ease", 115);
				return t;
			}
		}
	}

	public static float Ease(Type type, Function function, float ratio)
	{
		switch (type)
		{
		case Type.Linear:
			return ratio;
		case Type.EaseIn:
			return default(EaseInInterpolator).Ease(function, ratio);
		case Type.EaseOut:
			return default(EaseOutInterpolator).Ease(function, ratio);
		case Type.EaseInOut:
			return default(EaseInOutInterpolator).Ease(function, ratio);
		default:
			Debug.FailedAssert($"Brush interpolation type not implemented: {type}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\AnimationInterpolation.cs", "Ease", 41);
			return ratio;
		}
	}
}
