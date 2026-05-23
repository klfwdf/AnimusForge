using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CompressionInfo
{
	[EngineStruct("Integer_compression_info", false, null)]
	public struct Integer
	{
		[CustomEngineStructMemberData("min_value")]
		private readonly int minimumValue;

		[CustomEngineStructMemberData("max_value")]
		private readonly int maximumValue;

		[CustomEngineStructMemberData("num_bits")]
		private readonly int numberOfBits;

		public Integer(int minimumValue, int maximumValue, bool maximumValueGiven)
		{
			this.maximumValue = maximumValue;
			this.minimumValue = minimumValue;
			uint value = (uint)(maximumValue - minimumValue);
			numberOfBits = MBMath.GetNumberOfBitsToRepresentNumber(value);
		}

		public Integer(int minimumValue, int numberOfBits)
		{
			this.minimumValue = minimumValue;
			this.numberOfBits = numberOfBits;
			if (minimumValue == int.MinValue && numberOfBits == 32)
			{
				maximumValue = int.MaxValue;
			}
			else
			{
				maximumValue = minimumValue + (1 << numberOfBits) - 1;
			}
		}

		public int GetNumBits()
		{
			return numberOfBits;
		}

		public int GetMaximumValue()
		{
			return maximumValue;
		}
	}

	[EngineStruct("Unsigned_integer_compression_info", false, null)]
	public struct UnsignedInteger
	{
		[CustomEngineStructMemberData("min_value")]
		private readonly uint minimumValue;

		[CustomEngineStructMemberData("max_value")]
		private readonly uint maximumValue;

		[CustomEngineStructMemberData("num_bits")]
		private readonly int numberOfBits;

		public UnsignedInteger(uint minimumValue, uint maximumValue, bool maximumValueGiven)
		{
			this.minimumValue = minimumValue;
			this.maximumValue = maximumValue;
			uint value = maximumValue - minimumValue;
			numberOfBits = MBMath.GetNumberOfBitsToRepresentNumber(value);
		}

		public UnsignedInteger(uint minimumValue, int numberOfBits)
		{
			this.minimumValue = minimumValue;
			this.numberOfBits = numberOfBits;
			if (minimumValue == 0 && numberOfBits == 32)
			{
				maximumValue = uint.MaxValue;
			}
			else
			{
				maximumValue = (uint)(minimumValue + (1L << numberOfBits) - 1);
			}
		}

		public int GetNumBits()
		{
			return numberOfBits;
		}
	}

	[EngineStruct("Integer64_compression_info", false, null)]
	public struct LongInteger
	{
		[CustomEngineStructMemberData("min_value")]
		private readonly long minimumValue;

		[CustomEngineStructMemberData("max_value")]
		private readonly long maximumValue;

		[CustomEngineStructMemberData("num_bits")]
		private readonly int numberOfBits;

		public LongInteger(long minimumValue, long maximumValue, bool maximumValueGiven)
		{
			this.maximumValue = maximumValue;
			this.minimumValue = minimumValue;
			ulong value = (ulong)(maximumValue - minimumValue);
			numberOfBits = MBMath.GetNumberOfBitsToRepresentNumber(value);
		}

		public LongInteger(long minimumValue, int numberOfBits)
		{
			this.minimumValue = minimumValue;
			this.numberOfBits = numberOfBits;
			if (minimumValue == long.MinValue && numberOfBits == 64)
			{
				maximumValue = long.MaxValue;
			}
			else
			{
				maximumValue = minimumValue + (1L << numberOfBits) - 1;
			}
		}

		public int GetNumBits()
		{
			return numberOfBits;
		}
	}

	[EngineStruct("Unsigned_integer64_compression_info", false, null)]
	public struct UnsignedLongInteger
	{
		[CustomEngineStructMemberData("min_value")]
		private readonly ulong minimumValue;

		[CustomEngineStructMemberData("max_value")]
		private readonly ulong maximumValue;

		[CustomEngineStructMemberData("num_bits")]
		private readonly int numberOfBits;

		public UnsignedLongInteger(ulong minimumValue, ulong maximumValue, bool maximumValueGiven)
		{
			this.minimumValue = minimumValue;
			this.maximumValue = maximumValue;
			ulong value = maximumValue - minimumValue;
			numberOfBits = MBMath.GetNumberOfBitsToRepresentNumber(value);
		}

		public UnsignedLongInteger(ulong minimumValue, int numberOfBits)
		{
			this.minimumValue = minimumValue;
			this.numberOfBits = numberOfBits;
			if (minimumValue == 0L && numberOfBits == 64)
			{
				maximumValue = ulong.MaxValue;
			}
			else
			{
				maximumValue = (ulong)((long)minimumValue + (1L << numberOfBits) - 1);
			}
		}

		public int GetNumBits()
		{
			return numberOfBits;
		}
	}

	[EngineStruct("Float_compression_info", false, null)]
	public struct Float
	{
		[CustomEngineStructMemberData("min_value")]
		private readonly float minimumValue;

		[CustomEngineStructMemberData("max_value")]
		private readonly float maximumValue;

		[CustomEngineStructMemberData(true)]
		private readonly float precision;

		[CustomEngineStructMemberData("num_bits")]
		private readonly int numberOfBits;

		public static Float FullPrecision { get; } = new Float(isFullPrecision: true);

		public Float(float minimumValue, float maximumValue, int numberOfBits)
		{
			this.minimumValue = minimumValue;
			this.maximumValue = maximumValue;
			this.numberOfBits = numberOfBits;
			float num = maximumValue - minimumValue;
			int num2 = (1 << numberOfBits) - 1;
			precision = num / (float)num2;
		}

		public Float(float minimumValue, int numberOfBits, float precision)
		{
			this.minimumValue = minimumValue;
			this.precision = precision;
			this.numberOfBits = numberOfBits;
			int num = (1 << numberOfBits) - 1;
			float num2 = precision * (float)num;
			maximumValue = num2 + minimumValue;
		}

		private Float(bool isFullPrecision)
		{
			minimumValue = float.MinValue;
			maximumValue = float.MaxValue;
			precision = 0f;
			numberOfBits = 32;
		}

		public int GetNumBits()
		{
			return numberOfBits;
		}

		public float GetMaximumValue()
		{
			return maximumValue;
		}

		public float GetMinimumValue()
		{
			return minimumValue;
		}

		public float GetPrecision()
		{
			return precision;
		}

		public void ClampValueAccordingToLimits(ref float x)
		{
			x = MathF.Clamp(x, minimumValue, maximumValue);
		}
	}
}
