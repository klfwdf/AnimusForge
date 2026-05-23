using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace Bannerlord.BUTR.Shared.Helpers
{
	// Token: 0x0200004F RID: 79
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class ApplicationVersionHelper
	{
		// Token: 0x0600029E RID: 670 RVA: 0x000091D3 File Offset: 0x000073D3
		[NullableContext(2)]
		public static bool TryParse(string versionAsString, out ApplicationVersion version)
		{
			return ApplicationVersionHelper.TryParse(versionAsString, out version, true);
		}

		// Token: 0x0600029F RID: 671 RVA: 0x000091E0 File Offset: 0x000073E0
		[NullableContext(2)]
		public static bool TryParse(string versionAsString, out ApplicationVersion version, bool asMin)
		{
			version = default(ApplicationVersion);
			int major = (asMin ? 0 : int.MaxValue);
			int minor = (asMin ? 0 : int.MaxValue);
			int revision = (asMin ? 0 : int.MaxValue);
			int changeSet = (asMin ? 0 : int.MaxValue);
			if (versionAsString == null)
			{
				return false;
			}
			string[] array = versionAsString.Split(new char[] { '.' });
			if (array.Length != 3 && array.Length != 4 && array[0].Length == 0)
			{
				return false;
			}
			bool skipCheck = false;
			if (!skipCheck && !int.TryParse(array[0].Substring(1), out major))
			{
				if (array[0].Substring(1) != "*")
				{
					return false;
				}
				major = int.MinValue;
				minor = int.MinValue;
				revision = int.MinValue;
				changeSet = int.MinValue;
				skipCheck = true;
			}
			if (!skipCheck && !int.TryParse(array[1], out minor))
			{
				if (array[1] != "*")
				{
					return false;
				}
				minor = (asMin ? 0 : int.MaxValue);
				revision = (asMin ? 0 : int.MaxValue);
				changeSet = (asMin ? 0 : int.MaxValue);
				skipCheck = true;
			}
			if (!skipCheck && !int.TryParse(array[2], out revision))
			{
				if (array[2] != "*")
				{
					return false;
				}
				revision = (asMin ? 0 : int.MaxValue);
				changeSet = (asMin ? 0 : int.MaxValue);
				skipCheck = true;
			}
			if (!skipCheck && array.Length == 4 && !int.TryParse(array[3], out changeSet))
			{
				if (array[3] != "*")
				{
					return false;
				}
				changeSet = (asMin ? 0 : int.MaxValue);
			}
			ApplicationVersionType applicationVersionType = ApplicationVersion.ApplicationVersionTypeFromString(array[0][0].ToString());
			version = new ApplicationVersion(applicationVersionType, major, minor, revision, changeSet);
			return true;
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x00009394 File Offset: 0x00007594
		public static string ToString(ApplicationVersion av)
		{
			string prefix = ApplicationVersion.GetPrefix(av.ApplicationVersionType);
			ApplicationVersion def = ApplicationVersion.FromParametersFile(null);
			return string.Format("{0}{1}.{2}.{3}{4}", new object[]
			{
				prefix,
				av.Major,
				av.Minor,
				av.Revision,
				(av.ChangeSet == def.ChangeSet) ? "" : string.Format(".{0}", av.ChangeSet)
			});
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x00009428 File Offset: 0x00007628
		public static bool IsSame(this ApplicationVersion @this, ApplicationVersion other)
		{
			return @this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor && @this.Revision == other.Revision;
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x00009478 File Offset: 0x00007678
		public static bool IsSameWithChangeSet(this ApplicationVersion @this, ApplicationVersion other)
		{
			return @this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor && @this.Revision == other.Revision && @this.ChangeSet == other.ChangeSet;
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x000094D7 File Offset: 0x000076D7
		public static ApplicationVersion? GameVersion()
		{
			return new ApplicationVersion?(ApplicationVersion.FromParametersFile(null));
		}

		// Token: 0x060002A4 RID: 676 RVA: 0x000094E4 File Offset: 0x000076E4
		public static string GameVersionStr()
		{
			return ApplicationVersionHelper.ToString(ApplicationVersionHelper.GameVersion() ?? ApplicationVersion.Empty);
		}
	}
}
