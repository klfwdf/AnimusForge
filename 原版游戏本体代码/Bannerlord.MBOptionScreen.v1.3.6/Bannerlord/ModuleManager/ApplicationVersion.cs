using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000054 RID: 84
	[NullableContext(1)]
	[Nullable(0)]
	internal class ApplicationVersion : IComparable<ApplicationVersion>, IEquatable<ApplicationVersion>
	{
		// Token: 0x170000DF RID: 223
		// (get) Token: 0x060002D4 RID: 724 RVA: 0x0000A2E4 File Offset: 0x000084E4
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ApplicationVersion);
			}
		}

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x060002D5 RID: 725 RVA: 0x0000A2F0 File Offset: 0x000084F0
		public static ApplicationVersion Empty { get; } = new ApplicationVersion();

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x060002D6 RID: 726 RVA: 0x0000A2F7 File Offset: 0x000084F7
		// (set) Token: 0x060002D7 RID: 727 RVA: 0x0000A2FF File Offset: 0x000084FF
		public ApplicationVersionType ApplicationVersionType { get; set; }

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x060002D8 RID: 728 RVA: 0x0000A308 File Offset: 0x00008508
		// (set) Token: 0x060002D9 RID: 729 RVA: 0x0000A310 File Offset: 0x00008510
		public int Major { get; set; }

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x060002DA RID: 730 RVA: 0x0000A319 File Offset: 0x00008519
		// (set) Token: 0x060002DB RID: 731 RVA: 0x0000A321 File Offset: 0x00008521
		public int Minor { get; set; }

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x060002DC RID: 732 RVA: 0x0000A32A File Offset: 0x0000852A
		// (set) Token: 0x060002DD RID: 733 RVA: 0x0000A332 File Offset: 0x00008532
		public int Revision { get; set; }

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x060002DE RID: 734 RVA: 0x0000A33B File Offset: 0x0000853B
		// (set) Token: 0x060002DF RID: 735 RVA: 0x0000A343 File Offset: 0x00008543
		public int ChangeSet { get; set; }

		// Token: 0x060002E0 RID: 736 RVA: 0x0000A34C File Offset: 0x0000854C
		public ApplicationVersion()
		{
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x0000A354 File Offset: 0x00008554
		public ApplicationVersion(ApplicationVersionType applicationVersionType, int major, int minor, int revision, int changeSet)
		{
			this.ApplicationVersionType = applicationVersionType;
			this.Major = major;
			this.Minor = minor;
			this.Revision = revision;
			this.ChangeSet = changeSet;
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x0000A384 File Offset: 0x00008584
		[NullableContext(2)]
		public bool IsSame(ApplicationVersion other)
		{
			int major = this.Major;
			int? num = ((other != null) ? new int?(other.Major) : null);
			return ((major == num.GetValueOrDefault()) & (num != null)) && this.Minor == other.Minor && this.Revision == other.Revision;
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x0000A3E4 File Offset: 0x000085E4
		[NullableContext(2)]
		public bool IsSameWithChangeSet(ApplicationVersion other)
		{
			int major = this.Major;
			int? num = ((other != null) ? new int?(other.Major) : null);
			return ((major == num.GetValueOrDefault()) & (num != null)) && this.Minor == other.Minor && this.Revision == other.Revision && this.ChangeSet == other.ChangeSet;
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x0000A450 File Offset: 0x00008650
		public override string ToString()
		{
			return string.Format("{0}{1}.{2}.{3}.{4}", new object[]
			{
				ApplicationVersion.GetPrefix(this.ApplicationVersionType),
				this.Major,
				this.Minor,
				this.Revision,
				this.ChangeSet
			});
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x0000A4B8 File Offset: 0x000086B8
		[NullableContext(2)]
		public int CompareTo(ApplicationVersion other)
		{
			return ApplicationVersionComparer.CompareStandard(this, other);
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x0000A4C1 File Offset: 0x000086C1
		public static bool operator <(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) < 0;
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x0000A4CD File Offset: 0x000086CD
		public static bool operator >(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) > 0;
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x0000A4D9 File Offset: 0x000086D9
		public static bool operator <=(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) <= 0;
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x0000A4E8 File Offset: 0x000086E8
		public static bool operator >=(ApplicationVersion left, ApplicationVersion right)
		{
			return left.CompareTo(right) >= 0;
		}

		// Token: 0x060002EA RID: 746 RVA: 0x0000A4F8 File Offset: 0x000086F8
		public static char GetPrefix(ApplicationVersionType applicationVersionType)
		{
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
			return result;
		}

		// Token: 0x060002EB RID: 747 RVA: 0x0000A540 File Offset: 0x00008740
		public static ApplicationVersionType FromPrefix(char applicationVersionType)
		{
			switch (applicationVersionType)
			{
			case 'a':
				return ApplicationVersionType.Alpha;
			case 'b':
				return ApplicationVersionType.Beta;
			case 'c':
				break;
			case 'd':
				return ApplicationVersionType.Development;
			case 'e':
				return ApplicationVersionType.EarlyAccess;
			default:
				if (applicationVersionType == 'v')
				{
					return ApplicationVersionType.Release;
				}
				break;
			}
			return ApplicationVersionType.Invalid;
		}

		// Token: 0x060002EC RID: 748 RVA: 0x0000A588 File Offset: 0x00008788
		public static bool TryParse([Nullable(2)] string versionAsString, out ApplicationVersion version)
		{
			return ApplicationVersion.TryParse(versionAsString, out version, true);
		}

		// Token: 0x060002ED RID: 749 RVA: 0x0000A594 File Offset: 0x00008794
		public static bool TryParse([Nullable(2)] string versionAsString, out ApplicationVersion version, bool asMin)
		{
			int major = (asMin ? 0 : int.MaxValue);
			int minor = (asMin ? 0 : int.MaxValue);
			int revision = (asMin ? 0 : int.MaxValue);
			int changeSet = (asMin ? 0 : int.MaxValue);
			bool skipCheck = false;
			version = ApplicationVersion.Empty;
			if (versionAsString == null)
			{
				return false;
			}
			string[] array = versionAsString.Split(new char[] { '.' });
			if (array.Length != 3 && array.Length != 4 && array[0].Length == 0)
			{
				return false;
			}
			ApplicationVersionType applicationVersionType = ApplicationVersion.FromPrefix(array[0][0]);
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
			version = new ApplicationVersion
			{
				ApplicationVersionType = applicationVersionType,
				Major = major,
				Minor = minor,
				Revision = revision,
				ChangeSet = changeSet
			};
			return true;
		}

		// Token: 0x060002EE RID: 750 RVA: 0x0000A758 File Offset: 0x00008958
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

		// Token: 0x060002EF RID: 751 RVA: 0x0000A82E File Offset: 0x00008A2E
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ApplicationVersion left, ApplicationVersion right)
		{
			return !(left == right);
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0000A83A File Offset: 0x00008A3A
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ApplicationVersion left, ApplicationVersion right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x0000A850 File Offset: 0x00008A50
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ApplicationVersionType>.Default.GetHashCode(this.<ApplicationVersionType>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Major>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Minor>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<Revision>k__BackingField)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(this.<ChangeSet>k__BackingField);
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0000A8E0 File Offset: 0x00008AE0
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ApplicationVersion);
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x0000A8F0 File Offset: 0x00008AF0
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ApplicationVersion other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ApplicationVersionType>.Default.Equals(this.<ApplicationVersionType>k__BackingField, other.<ApplicationVersionType>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Major>k__BackingField, other.<Major>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Minor>k__BackingField, other.<Minor>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<Revision>k__BackingField, other.<Revision>k__BackingField) && EqualityComparer<int>.Default.Equals(this.<ChangeSet>k__BackingField, other.<ChangeSet>k__BackingField));
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x0000A9A0 File Offset: 0x00008BA0
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
