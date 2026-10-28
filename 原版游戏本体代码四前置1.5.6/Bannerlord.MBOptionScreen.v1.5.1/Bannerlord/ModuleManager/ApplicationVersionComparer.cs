using System.Collections;
using System.Collections.Generic;

namespace Bannerlord.ModuleManager;

internal class ApplicationVersionComparer : IComparer<Bannerlord.ModuleManager.ApplicationVersion?>, IComparer
{
	public int Compare(object? x, object? y)
	{
		return Compare(x as Bannerlord.ModuleManager.ApplicationVersion, y as Bannerlord.ModuleManager.ApplicationVersion);
	}

	public virtual int Compare(Bannerlord.ModuleManager.ApplicationVersion? x, Bannerlord.ModuleManager.ApplicationVersion? y)
	{
		return CompareStandard(x, y);
	}

	public static int CompareStandard(Bannerlord.ModuleManager.ApplicationVersion? x, Bannerlord.ModuleManager.ApplicationVersion? y)
	{
		if ((object)x == null && (object)y == null)
		{
			return 0;
		}
		if ((object)x == null)
		{
			return -1;
		}
		if ((object)y == null)
		{
			return 1;
		}
		int num = x.ApplicationVersionType.CompareTo(y.ApplicationVersionType);
		if (num != 0)
		{
			return num;
		}
		int num2 = x.Major.CompareTo(y.Major);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = x.Minor.CompareTo(y.Minor);
		if (num3 != 0)
		{
			return num3;
		}
		int num4 = x.Revision.CompareTo(y.Revision);
		if (num4 != 0)
		{
			return num4;
		}
		int num5 = x.ChangeSet.CompareTo(y.ChangeSet);
		if (num5 != 0)
		{
			return num5;
		}
		return 0;
	}
}
