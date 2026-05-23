using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000012 RID: 18
	[NullableContext(1)]
	[Nullable(0)]
	internal class ApplicationVersion : IComparable<ApplicationVersion>, IEquatable<ApplicationVersion>
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000CB RID: 203 RVA: 0x00004FAC File Offset: 0x000031AC
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ApplicationVersion);
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000CC RID: 204 RVA: 0x00004FB8 File Offset: 0x000031B8
		public static ApplicationVersion Empty { get; } = new ApplicationVersion();

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000CD RID: 205 RVA: 0x00004FBF File Offset: 0x000031BF
		// (set) Token: 0x060000CE RID: 206 RVA: 0x00004FC7 File Offset: 0x000031C7
		public ApplicationVersionType ApplicationVersionType { get; set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000CF RID: 207 RVA: 0x00004FD0 File Offset: 0x000031D0
		// (set) Token: 0x060000D0 RID: 208 RVA: 0x00004FD8 File Offset: 0x000031D8
		public int Major { get; set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000D1 RID: 209 RVA: 0x00004FE1 File Offset: 0x000031E1
		// (set) Token: 0x060000D2 RID: 210 RVA: 0x00004FE9 File Offset: 0x000031E9
		public int Minor { get; set; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000D3 RID: 211 RVA: 0x00004FF2 File Offset: 0x000031F2
		// (set) Token: 0x060000D4 RID: 212 RVA: 0x00004FFA File Offset: 0x000031FA
		public int Revision { get; set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000D5 RID: 213 RVA: 0x00005003 File Offset: 0x00003203
		// (set) Token: 0x060000D6 RID: 214 RVA: 0x0000500B File Offset: 0x0000320B
		public int ChangeSet { get; set; }

		// Token: 0x060000D7 RID: 215 RVA: 0x00005014 File Offset: 0x00003214
		public ApplicationVersion()
		{
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000501E File Offset: 0x0000321E
		public ApplicationVersion(ApplicationVersionType applicationVersionType, int major, int minor, int revision, int changeSet)
		{
			this.ApplicationVersionType = applicationVersionType;
			this.Major = major;
			this.Minor = minor;
			this.Revision = revision;
			this.ChangeSet = changeSet;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x00005054 File Offset: 0x00003254
		[NullableContext(2)]
		public bool IsSame(ApplicationVersion other)
		{
			int major = this.Major;
			int? num = ((other != null) ? new int?(other.Major) : null);
			return ((major == num.GetValueOrDefault()) & (num != null)) && this.Minor == other.Minor && this.Revision == other.Revision;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x000050B4 File Offset: 0x000032B4
		[NullableContext(2)]
		public bool IsSameWithChangeSet(ApplicationVersion other)
		{
			int major = this.Major;
			int? num = ((other != null) ? new int?(other.Major) : null);
			return ((major == num.GetValueOrDefault()) & (num != null)) && this.Minor == other.Minor && this.Revision == other.Revision && this.ChangeSet == other.ChangeSet;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00005124 File Offset: 0x00003324
		public override string ToString()
		{
			string result;
			if (this.ChangeSet != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 5);
				defaultInterpolatedStringHandler.AppendFormatted<char>(ApplicationVersion.GetPrefix(this.ApplicationVersionType));
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.Major);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.Minor);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.Revision);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				defaultInterpolatedStringHandler.AppendFormatted<int>(this.ChangeSet);
				result = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(2, 4);
				defaultInterpolatedStringHandler2.AppendFormatted<char>(ApplicationVersion.GetPrefix(this.ApplicationVersionType));
				defaultInterpolatedStringHandler2.AppendFormatted<int>(this.Major);
				defaultInterpolatedStringHandler2.AppendLiteral(".");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(this.Minor);
				defaultInterpolatedStringHandler2.AppendLiteral(".");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(this.Revision);
				result = defaultInterpolatedStringHandler2.ToStringAndClear();
			}
			return result;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000522C File Offset: 0x0000342C
		public string ToStringWithoutChangeset()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 4);
			defaultInterpolatedStringHandler.AppendFormatted<char>(ApplicationVersion.GetPrefix(this.ApplicationVersionType));
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.Major);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.Minor);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted<int>(this.Revision);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060000DD RID: 221 RVA: 0x000052A0 File Offset: 0x000034A0
		[NullableContext(2)]
		public int CompareTo(ApplicationVersion other)
		{
			return ApplicationVersionComparer.CompareStandard(this, other);
		}

		// Token: 0x060000DE RID: 222 RVA: 0x000052A9 File Offset: 0x000034A9
		public static bool operator <(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) < 0;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x000052B5 File Offset: 0x000034B5
		public static bool operator >(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) > 0;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x000052C1 File Offset: 0x000034C1
		public static bool operator <=(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) <= 0;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x000052D0 File Offset: 0x000034D0
		public static bool operator >=(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) >= 0;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x000052E0 File Offset: 0x000034E0
		public static char GetPrefix(ApplicationVersionType applicationVersionType)
		{
			if (!true)
			{
			}
			char result;
			switch (applicationVersionType)
			{
			case ApplicationVersionType.Alpha:
				result = 'a';
				break;
			case ApplicationVersionType.Beta:
				result = 'b';
				break;
			case ApplicationVersionType.EarlyAccess:
				result = 'e';
				break;
			case ApplicationVersionType.Release:
				result = 'v';
				break;
			case ApplicationVersionType.Development:
				result = 'd';
				break;
			default:
				result = 'i';
				break;
			}
			if (!true)
			{
			}
			return result;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00005330 File Offset: 0x00003530
		public static ApplicationVersionType FromPrefix(char applicationVersionType)
		{
			if (!true)
			{
			}
			ApplicationVersionType result;
			switch (applicationVersionType)
			{
			case 'a':
				result = ApplicationVersionType.Alpha;
				goto IL_42;
			case 'b':
				result = ApplicationVersionType.Beta;
				goto IL_42;
			case 'c':
				break;
			case 'd':
				result = ApplicationVersionType.Development;
				goto IL_42;
			case 'e':
				result = ApplicationVersionType.EarlyAccess;
				goto IL_42;
			default:
				if (applicationVersionType == 'v')
				{
					result = ApplicationVersionType.Release;
					goto IL_42;
				}
				break;
			}
			result = ApplicationVersionType.Invalid;
			IL_42:
			if (!true)
			{
			}
			return result;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00005384 File Offset: 0x00003584
		public static bool TryParse([Nullable(2)] string versionAsString, out ApplicationVersion version)
		{
			return ApplicationVersion.TryParse(versionAsString, out version, true);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00005390 File Offset: 0x00003590
		public static bool TryParse([Nullable(2)] string versionAsString, out ApplicationVersion version, bool asMin)
		{
			int major = (asMin ? 0 : int.MaxValue);
			int minor = (asMin ? 0 : int.MaxValue);
			int revision = (asMin ? 0 : int.MaxValue);
			int changeSet = (asMin ? 0 : int.MaxValue);
			bool skipCheck = false;
			version = ApplicationVersion.Empty;
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
					ApplicationVersionType applicationVersionType = ApplicationVersion.FromPrefix(array[0][0]);
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
					version = new ApplicationVersion
					{
						ApplicationVersionType = applicationVersionType,
						Major = major,
						Minor = minor,
						Revision = revision,
						ChangeSet = changeSet
					};
					result = true;
				}
			}
			return result;
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000055D0 File Offset: 0x000037D0
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("ApplicationVersionType = ");
			builder.Append(this.ApplicationVersionType.ToString());
			builder.Append(", Major = ");
			builder.Append(this.Major.ToString());
			builder.Append(", Minor = ");
			builder.Append(this.Minor.ToString());
			builder.Append(", Revision = ");
			builder.Append(this.Revision.ToString());
			builder.Append(", ChangeSet = ");
			builder.Append(this.ChangeSet.ToString());
			return true;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x000056A6 File Offset: 0x000038A6
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ApplicationVersion left, ApplicationVersion right)
		{
			return !(left == right);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x000056B2 File Offset: 0x000038B2
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ApplicationVersion left, ApplicationVersion right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x000056C8 File Offset: 0x000038C8
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ApplicationVersionType>.Default.GetHashCode(this.<ApplicationVersionType>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Major>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Minor>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Revision>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<ChangeSet>k__BackingField);
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00005758 File Offset: 0x00003958
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ApplicationVersion);
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00005768 File Offset: 0x00003968
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ApplicationVersion other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ApplicationVersionType>.Default.Equals(this.<ApplicationVersionType>k__BackingField, other.<ApplicationVersionType>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Major>k__BackingField, other.<Major>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Minor>k__BackingField, other.<Minor>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Revision>k__BackingField, other.<Revision>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<ChangeSet>k__BackingField, other.<ChangeSet>k__BackingField));
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000581C File Offset: 0x00003A1C
		[CompilerGenerated]
		protected ApplicationVersion(ApplicationVersion original)
		{
			this.ApplicationVersionType = original.<ApplicationVersionType>k__BackingField;
			this.Major = original.<Major>k__BackingField;
			this.Minor = original.<Minor>k__BackingField;
			this.Revision = original.<Revision>k__BackingField;
			this.ChangeSet = original.<ChangeSet>k__BackingField;
		}
	}
}
