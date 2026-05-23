using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace Bannerlord.BUTR.Shared.Helpers
{
	// Token: 0x0200000D RID: 13
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class ApplicationVersionHelper
	{
		// Token: 0x06000095 RID: 149 RVA: 0x00003BE1 File Offset: 0x00001DE1
		[NullableContext(2)]
		public static bool TryParse(string versionAsString, out ApplicationVersion version)
		{
			return ApplicationVersionHelper.TryParse(versionAsString, out version, true);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00003BEC File Offset: 0x00001DEC
		[NullableContext(2)]
		public static bool TryParse(string versionAsString, out ApplicationVersion version, bool asMin)
		{
			version = default(ApplicationVersion);
			int major = (asMin ? 0 : int.MaxValue);
			int minor = (asMin ? 0 : int.MaxValue);
			int revision = (asMin ? 0 : int.MaxValue);
			int changeSet = (asMin ? 0 : int.MaxValue);
			bool flag = versionAsString == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				string[] array = versionAsString.Split(new char[] { '.' });
				bool flag2 = array.Length != 3 && array.Length != 4 && array[0].Length == 0;
				if (flag2)
				{
					result = false;
				}
				else
				{
					bool skipCheck = false;
					bool flag3 = !skipCheck && !int.TryParse(array[0].Substring(1), out major);
					if (flag3)
					{
						bool flag4 = array[0].Substring(1) != "*";
						if (flag4)
						{
							return false;
						}
						major = int.MinValue;
						minor = int.MinValue;
						revision = int.MinValue;
						changeSet = int.MinValue;
						skipCheck = true;
					}
					bool flag5 = !skipCheck && !int.TryParse(array[1], out minor);
					if (flag5)
					{
						bool flag6 = array[1] != "*";
						if (flag6)
						{
							return false;
						}
						minor = (asMin ? 0 : int.MaxValue);
						revision = (asMin ? 0 : int.MaxValue);
						changeSet = (asMin ? 0 : int.MaxValue);
						skipCheck = true;
					}
					bool flag7 = !skipCheck && !int.TryParse(array[2], out revision);
					if (flag7)
					{
						bool flag8 = array[2] != "*";
						if (flag8)
						{
							return false;
						}
						revision = (asMin ? 0 : int.MaxValue);
						changeSet = (asMin ? 0 : int.MaxValue);
						skipCheck = true;
					}
					bool flag9 = !skipCheck && array.Length == 4 && !int.TryParse(array[3], out changeSet);
					if (flag9)
					{
						bool flag10 = array[3] != "*";
						if (flag10)
						{
							return false;
						}
						changeSet = (asMin ? 0 : int.MaxValue);
					}
					ApplicationVersionType applicationVersionType = ApplicationVersion.ApplicationVersionTypeFromString(array[0][0].ToString());
					version = new ApplicationVersion(applicationVersionType, major, minor, revision, changeSet);
					result = true;
				}
			}
			return result;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003E18 File Offset: 0x00002018
		public static string ToString(ApplicationVersion av)
		{
			string prefix = ApplicationVersion.GetPrefix(av.ApplicationVersionType);
			ApplicationVersion def = ApplicationVersion.FromParametersFile(null);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 5);
			defaultInterpolatedStringHandler.AppendFormatted(prefix);
			defaultInterpolatedStringHandler.AppendFormatted<int>(av.Major);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted<int>(av.Minor);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted<int>(av.Revision);
			string value;
			if (av.ChangeSet != def.ChangeSet)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler2.AppendLiteral(".");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(av.ChangeSet);
				value = defaultInterpolatedStringHandler2.ToStringAndClear();
			}
			else
			{
				value = "";
			}
			defaultInterpolatedStringHandler.AppendFormatted(value);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00003EEC File Offset: 0x000020EC
		public static bool IsSame(this ApplicationVersion @this, ApplicationVersion other)
		{
			return @this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor && @this.Revision == other.Revision;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00003F3C File Offset: 0x0000213C
		public static bool IsSameWithChangeSet(this ApplicationVersion @this, ApplicationVersion other)
		{
			return @this.ApplicationVersionType == other.ApplicationVersionType && @this.Major == other.Major && @this.Minor == other.Minor && @this.Revision == other.Revision && @this.ChangeSet == other.ChangeSet;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00003F9C File Offset: 0x0000219C
		public static ApplicationVersion? GameVersion()
		{
			return new ApplicationVersion?(ApplicationVersion.FromParametersFile(null));
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00003FAC File Offset: 0x000021AC
		public static string GameVersionStr()
		{
			return ApplicationVersionHelper.ToString(ApplicationVersionHelper.GameVersion() ?? ApplicationVersion.Empty);
		}
	}
}
