using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class MBHaltonColorGenerator
{
	public const int DefaultBase = 2;

	private int _base;

	private float _offset;

	public int Base => _base;

	public float Offset => _offset;

	public MBHaltonColorGenerator()
	{
		SetRandomOffset();
		SetBase();
	}

	public void SetBase()
	{
		_base = 2;
	}

	public void SetBase(int baseValue)
	{
		_base = baseValue;
	}

	public void SetOffset(float offset)
	{
		_offset = TaleWorlds.Library.MathF.Clamp(offset, 0f, 1f);
	}

	public void SetRandomOffset()
	{
		_offset = MBRandom.RandomFloat;
	}

	public Color GetColor(int index, int maxIndex)
	{
		return Color.FromHSV(HaltonSequence(((float)index / (float)maxIndex + _offset) % 1f, _base), 1f, 1f);
	}

	private static float HaltonSequence(float normalizedIndex, int baseValue)
	{
		float num = 1f;
		float num2 = 0f;
		for (float num3 = normalizedIndex * (float)baseValue; num3 > 0f; num3 = (float)Math.Floor(num3 / (float)baseValue))
		{
			num /= (float)baseValue;
			num2 += num3 % (float)baseValue * num;
		}
		return num2;
	}
}
