using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Common.Extensions;

public static class ApplicationVersionExtensions
{
	public static bool IsSameWithRevision(this ApplicationVersion @this, ApplicationVersion other)
	{
		if (@this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor)
		{
			return @this.Revision == other.Revision;
		}
		return false;
	}

	public static bool IsSameWithoutRevision(this ApplicationVersion @this, ApplicationVersion other)
	{
		if (@this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major)
		{
			return @this.Minor == other.Minor;
		}
		return false;
	}
}
