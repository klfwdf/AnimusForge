using System;

namespace TaleWorlds.MountAndBlade;

public struct MissionTime : IComparable<MissionTime>
{
	public const long TimeTicksPerMilliSecond = 10000L;

	public const long TimeTicksPerSecond = 10000000L;

	public const long TimeTicksPerMinute = 600000000L;

	public const long TimeTicksPerHour = 36000000000L;

	public const float InvTimeTicksPerSecond = 1E-07f;

	private readonly long _numberOfTicks;

	public long NumberOfTicks => _numberOfTicks;

	private static long CurrentNumberOfTicks => Mission.Current.MissionTimeTracker.NumberOfTicks;

	public static MissionTime DeltaTime => new MissionTime(Mission.Current.MissionTimeTracker.DeltaTimeInTicks);

	private static long DeltaTimeInTicks => Mission.Current.MissionTimeTracker.DeltaTimeInTicks;

	public static MissionTime Now => new MissionTime(Mission.Current.MissionTimeTracker.NumberOfTicks);

	public bool IsFuture => CurrentNumberOfTicks < _numberOfTicks;

	public bool IsPast => CurrentNumberOfTicks > _numberOfTicks;

	public bool IsNow => CurrentNumberOfTicks == _numberOfTicks;

	public float ElapsedHours => (float)(CurrentNumberOfTicks - _numberOfTicks) / 3.6E+10f;

	public float ElapsedSeconds => (float)(CurrentNumberOfTicks - _numberOfTicks) * 1E-07f;

	public float ElapsedMilliseconds => (float)(CurrentNumberOfTicks - _numberOfTicks) / 10000f;

	public double ToHours => (double)_numberOfTicks / 36000000000.0;

	public double ToMinutes => (double)_numberOfTicks / 600000000.0;

	public double ToSeconds => (double)_numberOfTicks * 1.0000000116860974E-07;

	public double ToMilliseconds => (double)_numberOfTicks / 10000.0;

	public static MissionTime Zero => new MissionTime(0L);

	public MissionTime(long numberOfTicks)
	{
		_numberOfTicks = numberOfTicks;
	}

	public static MissionTime MillisecondsFromNow(float valueInMilliseconds)
	{
		return new MissionTime((long)(valueInMilliseconds * 10000f + (float)CurrentNumberOfTicks));
	}

	public static MissionTime SecondsFromNow(float valueInSeconds)
	{
		return new MissionTime((long)(valueInSeconds * 10000000f + (float)CurrentNumberOfTicks));
	}

	public bool Equals(MissionTime other)
	{
		return _numberOfTicks == other._numberOfTicks;
	}

	public override bool Equals(object obj)
	{
		if (obj != null && obj is MissionTime)
		{
			return Equals((MissionTime)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		long numberOfTicks = _numberOfTicks;
		return numberOfTicks.GetHashCode();
	}

	public int CompareTo(MissionTime other)
	{
		if (_numberOfTicks == other._numberOfTicks)
		{
			return 0;
		}
		if (_numberOfTicks > other._numberOfTicks)
		{
			return 1;
		}
		return -1;
	}

	public static bool operator <(MissionTime x, MissionTime y)
	{
		return x._numberOfTicks < y._numberOfTicks;
	}

	public static bool operator >(MissionTime x, MissionTime y)
	{
		return x._numberOfTicks > y._numberOfTicks;
	}

	public static bool operator ==(MissionTime x, MissionTime y)
	{
		return x._numberOfTicks == y._numberOfTicks;
	}

	public static bool operator !=(MissionTime x, MissionTime y)
	{
		return !(x == y);
	}

	public static bool operator <=(MissionTime x, MissionTime y)
	{
		return x._numberOfTicks <= y._numberOfTicks;
	}

	public static bool operator >=(MissionTime x, MissionTime y)
	{
		return x._numberOfTicks >= y._numberOfTicks;
	}

	public static MissionTime Milliseconds(float valueInMilliseconds)
	{
		return new MissionTime((long)(valueInMilliseconds * 10000f));
	}

	public static MissionTime Seconds(float valueInSeconds)
	{
		return new MissionTime((long)(valueInSeconds * 10000000f));
	}

	public static MissionTime Minutes(float valueInMinutes)
	{
		return new MissionTime((long)(valueInMinutes * 600000000f));
	}

	public static MissionTime Hours(float valueInHours)
	{
		return new MissionTime((long)(valueInHours * 3.6E+10f));
	}

	public static MissionTime operator +(MissionTime g1, MissionTime g2)
	{
		return new MissionTime(g1._numberOfTicks + g2._numberOfTicks);
	}

	public static MissionTime operator -(MissionTime g1, MissionTime g2)
	{
		return new MissionTime(g1._numberOfTicks - g2._numberOfTicks);
	}
}
