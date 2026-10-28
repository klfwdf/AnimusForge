using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TaleWorlds.Library;

namespace Bannerlord.BUTR.Shared.Helpers;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal static class ApplicationVersionHelper
{
	public static bool TryParse(string? versionAsString, out ApplicationVersion version)
	{
		return TryParse(versionAsString, out version, asMin: true);
	}

	public static bool TryParse(string? versionAsString, out ApplicationVersion version, bool asMin)
	{
		version = default(ApplicationVersion);
		int result = ((!asMin) ? int.MaxValue : 0);
		int result2 = ((!asMin) ? int.MaxValue : 0);
		int result3 = ((!asMin) ? int.MaxValue : 0);
		int result4 = ((!asMin) ? int.MaxValue : 0);
		if (versionAsString == null)
		{
			return false;
		}
		string[] array = versionAsString.Split(new char[1] { '.' });
		if (array.Length != 3 && array.Length != 4 && array[0].Length == 0)
		{
			return false;
		}
		bool flag = false;
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
		ApplicationVersionType applicationVersionType = ApplicationVersion.ApplicationVersionTypeFromString(array[0][0].ToString());
		version = new ApplicationVersion(applicationVersionType, result, result2, result3, result4);
		return true;
	}

	public static string ToString(ApplicationVersion av)
	{
		string prefix = ApplicationVersion.GetPrefix(av.ApplicationVersionType);
		ApplicationVersion applicationVersion = ApplicationVersion.FromParametersFile();
		return string.Format("{0}{1}.{2}.{3}{4}", prefix, av.Major, av.Minor, av.Revision, (av.ChangeSet == applicationVersion.ChangeSet) ? "" : $".{av.ChangeSet}");
	}

	public static bool IsSame(this ApplicationVersion @this, ApplicationVersion other)
	{
		if (@this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor)
		{
			return @this.Revision == other.Revision;
		}
		return false;
	}

	public static bool IsSameWithChangeSet(this ApplicationVersion @this, ApplicationVersion other)
	{
		if (@this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor && @this.Revision == other.Revision)
		{
			return @this.ChangeSet == other.ChangeSet;
		}
		return false;
	}

	public static ApplicationVersion? GameVersion()
	{
		return ApplicationVersion.FromParametersFile();
	}

	public static string GameVersionStr()
	{
		return ToString(GameVersion() ?? ApplicationVersion.Empty);
	}
}
