namespace NavalDLC.DWA;

internal struct DWAFloatPair
{
	private float _a;

	private float _b;

	internal DWAFloatPair(float a, float b)
	{
		_a = a;
		_b = b;
	}

	public static bool operator <(DWAFloatPair pair1, DWAFloatPair pair2)
	{
		if (!(pair1._a < pair2._a))
		{
			if (!(pair2._a < pair1._a))
			{
				return pair1._b < pair2._b;
			}
			return false;
		}
		return true;
	}

	public static bool operator <=(DWAFloatPair pair1, DWAFloatPair pair2)
	{
		if (pair1._a != pair2._a || pair1._b != pair2._b)
		{
			return pair1 < pair2;
		}
		return true;
	}

	public static bool operator >(DWAFloatPair pair1, DWAFloatPair pair2)
	{
		return !(pair1 <= pair2);
	}

	public static bool operator >=(DWAFloatPair pair1, DWAFloatPair pair2)
	{
		return !(pair1 < pair2);
	}
}
