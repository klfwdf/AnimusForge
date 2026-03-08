using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000055 RID: 85
	[NullableContext(2)]
	[Nullable(0)]
	internal class ApplicationVersionComparer : IComparer<ApplicationVersion>, IComparer
	{
		// Token: 0x060002F7 RID: 759 RVA: 0x0000A9FB File Offset: 0x00008BFB
		public int Compare(object x, object y)
		{
			return this.Compare(x as ApplicationVersion, y as ApplicationVersion);
		}

		// Token: 0x060002F8 RID: 760 RVA: 0x0000AA0F File Offset: 0x00008C0F
		public virtual int Compare(ApplicationVersion x, ApplicationVersion y)
		{
			return ApplicationVersionComparer.CompareStandard(x, y);
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x0000AA18 File Offset: 0x00008C18
		public static int CompareStandard(ApplicationVersion x, ApplicationVersion y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			int versionTypeComparison = x.ApplicationVersionType.CompareTo(y.ApplicationVersionType);
			if (versionTypeComparison != 0)
			{
				return versionTypeComparison;
			}
			int majorComparison = x.Major.CompareTo(y.Major);
			if (majorComparison != 0)
			{
				return majorComparison;
			}
			int minorComparison = x.Minor.CompareTo(y.Minor);
			if (minorComparison != 0)
			{
				return minorComparison;
			}
			int revisionComparison = x.Revision.CompareTo(y.Revision);
			if (revisionComparison != 0)
			{
				return revisionComparison;
			}
			int changeSetComparison = x.ChangeSet.CompareTo(y.ChangeSet);
			if (changeSetComparison != 0)
			{
				return changeSetComparison;
			}
			return 0;
		}
	}
}
