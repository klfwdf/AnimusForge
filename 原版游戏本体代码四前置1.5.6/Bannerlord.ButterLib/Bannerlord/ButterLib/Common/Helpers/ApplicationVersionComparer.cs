using System;
using System.Collections.Generic;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Common.Helpers;

public class ApplicationVersionComparer : IComparer<ApplicationVersion>
{
	private readonly ref struct VersionGameTypeWrapper
	{
		private delegate IComparable GetVersionGameTypeDelegate();

		private delegate void SetVersionGameTypeDelegate(IComparable? value);

		private readonly GetVersionGameTypeDelegate? _getVersionGameTypeDelegate;

		private readonly SetVersionGameTypeDelegate? _setVersionGameTypeDelegate;

		public IComparable? VersionGameType
		{
			get
			{
				return _getVersionGameTypeDelegate?.Invoke();
			}
			set
			{
				_setVersionGameTypeDelegate?.Invoke(value);
			}
		}

		public VersionGameTypeWrapper(object? @object)
		{
			Type type = @object?.GetType();
			_getVersionGameTypeDelegate = (((object)type != null) ? HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertyGetterDelegate<GetVersionGameTypeDelegate>(@object, type, "VersionGameType") : null);
			_setVersionGameTypeDelegate = (((object)type != null) ? HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertySetterDelegate<SetVersionGameTypeDelegate>(@object, type, "VersionGameType") : null);
		}
	}

	public int Compare(ApplicationVersion x, ApplicationVersion y)
	{
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
		int num6 = new VersionGameTypeWrapper(x).VersionGameType?.CompareTo(new VersionGameTypeWrapper(y).VersionGameType) ?? 0;
		if (num6 != 0)
		{
			return num6;
		}
		return 0;
	}
}
