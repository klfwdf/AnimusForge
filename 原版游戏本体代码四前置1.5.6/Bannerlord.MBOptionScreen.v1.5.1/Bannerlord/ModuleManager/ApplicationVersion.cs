using System;

namespace Bannerlord.ModuleManager;

internal record ApplicationVersion : IComparable<Bannerlord.ModuleManager.ApplicationVersion>
{
	public static Bannerlord.ModuleManager.ApplicationVersion Empty { get; } = new Bannerlord.ModuleManager.ApplicationVersion();

	public Bannerlord.ModuleManager.ApplicationVersionType ApplicationVersionType { get; set; }

	public int Major { get; set; }

	public int Minor { get; set; }

	public int Revision { get; set; }

	public int ChangeSet { get; set; }

	public ApplicationVersion()
	{
	}

	public ApplicationVersion(Bannerlord.ModuleManager.ApplicationVersionType applicationVersionType, int major, int minor, int revision, int changeSet)
	{
		ApplicationVersionType = applicationVersionType;
		Major = major;
		Minor = minor;
		Revision = revision;
		ChangeSet = changeSet;
	}

	public bool IsSame(Bannerlord.ModuleManager.ApplicationVersion? other)
	{
		if (Major == other?.Major && Minor == other.Minor)
		{
			return Revision == other.Revision;
		}
		return false;
	}

	public bool IsSameWithChangeSet(Bannerlord.ModuleManager.ApplicationVersion? other)
	{
		if (Major == other?.Major && Minor == other.Minor && Revision == other.Revision)
		{
			return ChangeSet == other.ChangeSet;
		}
		return false;
	}

	public override string ToString()
	{
		return $"{GetPrefix(ApplicationVersionType)}{Major}.{Minor}.{Revision}.{ChangeSet}";
	}

	public int CompareTo(Bannerlord.ModuleManager.ApplicationVersion? other)
	{
		return Bannerlord.ModuleManager.ApplicationVersionComparer.CompareStandard(this, other);
	}

	public static bool operator <(Bannerlord.ModuleManager.ApplicationVersion left, Bannerlord.ModuleManager.ApplicationVersion right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator >(Bannerlord.ModuleManager.ApplicationVersion left, Bannerlord.ModuleManager.ApplicationVersion right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator <=(Bannerlord.ModuleManager.ApplicationVersion left, Bannerlord.ModuleManager.ApplicationVersion right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >=(Bannerlord.ModuleManager.ApplicationVersion left, Bannerlord.ModuleManager.ApplicationVersion right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static char GetPrefix(Bannerlord.ModuleManager.ApplicationVersionType applicationVersionType)
	{
		return applicationVersionType switch
		{
			Bannerlord.ModuleManager.ApplicationVersionType.Alpha => 'a', 
			Bannerlord.ModuleManager.ApplicationVersionType.Beta => 'b', 
			Bannerlord.ModuleManager.ApplicationVersionType.EarlyAccess => 'e', 
			Bannerlord.ModuleManager.ApplicationVersionType.Release => 'v', 
			Bannerlord.ModuleManager.ApplicationVersionType.Development => 'd', 
			_ => 'i', 
		};
	}

	public static Bannerlord.ModuleManager.ApplicationVersionType FromPrefix(char applicationVersionType)
	{
		return applicationVersionType switch
		{
			'a' => Bannerlord.ModuleManager.ApplicationVersionType.Alpha, 
			'b' => Bannerlord.ModuleManager.ApplicationVersionType.Beta, 
			'e' => Bannerlord.ModuleManager.ApplicationVersionType.EarlyAccess, 
			'v' => Bannerlord.ModuleManager.ApplicationVersionType.Release, 
			'd' => Bannerlord.ModuleManager.ApplicationVersionType.Development, 
			_ => Bannerlord.ModuleManager.ApplicationVersionType.Invalid, 
		};
	}

	public static bool TryParse(string? versionAsString, out Bannerlord.ModuleManager.ApplicationVersion version)
	{
		return TryParse(versionAsString, out version, asMin: true);
	}

	public static bool TryParse(string? versionAsString, out Bannerlord.ModuleManager.ApplicationVersion version, bool asMin)
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
		Bannerlord.ModuleManager.ApplicationVersionType applicationVersionType = FromPrefix(array[0][0]);
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
		version = new Bannerlord.ModuleManager.ApplicationVersion
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
