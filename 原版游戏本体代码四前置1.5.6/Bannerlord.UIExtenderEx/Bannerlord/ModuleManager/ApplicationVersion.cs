using System;

namespace Bannerlord.ModuleManager;

internal record ApplicationVersion : IComparable<ApplicationVersion>
{
	public static ApplicationVersion Empty { get; } = new ApplicationVersion();

	public ApplicationVersionType ApplicationVersionType { get; set; }

	public int Major { get; set; }

	public int Minor { get; set; }

	public int Revision { get; set; }

	public int ChangeSet { get; set; }

	public ApplicationVersion()
	{
	}

	public ApplicationVersion(ApplicationVersionType applicationVersionType, int major, int minor, int revision, int changeSet)
	{
		ApplicationVersionType = applicationVersionType;
		Major = major;
		Minor = minor;
		Revision = revision;
		ChangeSet = changeSet;
	}

	public bool IsSame(ApplicationVersion? other)
	{
		if (Major == other?.Major && Minor == other.Minor)
		{
			return Revision == other.Revision;
		}
		return false;
	}

	public bool IsSameWithChangeSet(ApplicationVersion? other)
	{
		if (Major == other?.Major && Minor == other.Minor && Revision == other.Revision)
		{
			return ChangeSet == other.ChangeSet;
		}
		return false;
	}

	public override string ToString()
	{
		if (ChangeSet != 0)
		{
			return $"{GetPrefix(ApplicationVersionType)}{Major}.{Minor}.{Revision}.{ChangeSet}";
		}
		return $"{GetPrefix(ApplicationVersionType)}{Major}.{Minor}.{Revision}";
	}

	public string ToStringWithoutChangeset()
	{
		return $"{GetPrefix(ApplicationVersionType)}{Major}.{Minor}.{Revision}";
	}

	public int CompareTo(ApplicationVersion? other)
	{
		return ApplicationVersionComparer.CompareStandard(this, other);
	}

	public static bool operator <(ApplicationVersion left, ApplicationVersion right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator >(ApplicationVersion left, ApplicationVersion right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator <=(ApplicationVersion left, ApplicationVersion right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >=(ApplicationVersion left, ApplicationVersion right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static char GetPrefix(ApplicationVersionType applicationVersionType)
	{
		return applicationVersionType switch
		{
			ApplicationVersionType.Alpha => 'a', 
			ApplicationVersionType.Beta => 'b', 
			ApplicationVersionType.EarlyAccess => 'e', 
			ApplicationVersionType.Release => 'v', 
			ApplicationVersionType.Development => 'd', 
			_ => 'i', 
		};
	}

	public static ApplicationVersionType FromPrefix(char applicationVersionType)
	{
		return applicationVersionType switch
		{
			'a' => ApplicationVersionType.Alpha, 
			'b' => ApplicationVersionType.Beta, 
			'e' => ApplicationVersionType.EarlyAccess, 
			'v' => ApplicationVersionType.Release, 
			'd' => ApplicationVersionType.Development, 
			_ => ApplicationVersionType.Invalid, 
		};
	}

	public static bool TryParse(string? versionAsString, out ApplicationVersion version)
	{
		return TryParse(versionAsString, out version, asMin: true);
	}

	public static bool TryParse(string? versionAsString, out ApplicationVersion version, bool asMin)
	{
		int result = ((!asMin) ? int.MaxValue : 0);
		int result2 = ((!asMin) ? int.MaxValue : 0);
		int result3 = ((!asMin) ? int.MaxValue : 0);
		int result4 = ((!asMin) ? int.MaxValue : 0);
		bool flag = false;
		version = Empty;
		if (versionAsString == null)
		{
			return false;
		}
		string[] array = versionAsString.Split(new char[1] { '.' });
		if (array.Length != 3 && array.Length != 4 && array[0].Length == 0)
		{
			return false;
		}
		ApplicationVersionType applicationVersionType = FromPrefix(array[0][0]);
		if (!flag && !int.TryParse(array[0].Substring(1), out result))
		{
			if (array[0].Substring(1) != "*")
			{
				return false;
			}
			result = int.MinValue;
			result2 = int.MinValue;
			result3 = int.MinValue;
			result4 = int.MinValue;
			flag = true;
		}
		if (!flag && !int.TryParse(array[1], out result2))
		{
			if (array[1] != "*")
			{
				return false;
			}
			result2 = ((!asMin) ? int.MaxValue : 0);
			result3 = ((!asMin) ? int.MaxValue : 0);
			result4 = ((!asMin) ? int.MaxValue : 0);
			flag = true;
		}
		if (!flag && !int.TryParse(array[2], out result3))
		{
			if (array[2] != "*")
			{
				return false;
			}
			result3 = ((!asMin) ? int.MaxValue : 0);
			result4 = ((!asMin) ? int.MaxValue : 0);
			flag = true;
		}
		if (!flag && array.Length == 4 && !int.TryParse(array[3], out result4))
		{
			if (array[3] != "*")
			{
				return false;
			}
			result4 = ((!asMin) ? int.MaxValue : 0);
			flag = true;
		}
		version = new ApplicationVersion
		{
			ApplicationVersionType = applicationVersionType,
			Major = result,
			Minor = result2,
			Revision = result3,
			ChangeSet = result4
		};
		return true;
	}
}
