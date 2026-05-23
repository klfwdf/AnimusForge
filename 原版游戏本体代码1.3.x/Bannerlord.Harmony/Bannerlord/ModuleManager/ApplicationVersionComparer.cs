using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000013 RID: 19
	[NullableContext(2)]
	[Nullable(0)]
	internal class ApplicationVersionComparer : IComparer<ApplicationVersion>, IComparer
	{
		// Token: 0x060000EF RID: 239 RVA: 0x00005879 File Offset: 0x00003A79
		public int Compare(object x, object y)
		{
			return this.Compare(x as ApplicationVersion, y as ApplicationVersion);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000588D File Offset: 0x00003A8D
		public virtual int Compare(ApplicationVersion x, ApplicationVersion y)
		{
			return ApplicationVersionComparer.CompareStandard(x, y);
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00005898 File Offset: 0x00003A98
		public static int CompareStandard(ApplicationVersion x, ApplicationVersion y)
		{
			bool flag = x == null && y == null;
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				bool flag2 = x == null;
				if (flag2)
				{
					result = -1;
				}
				else
				{
					bool flag3 = y == null;
					if (flag3)
					{
						result = 1;
					}
					else
					{
						int versionTypeComparison = x.ApplicationVersionType.CompareTo(y.ApplicationVersionType);
						bool flag4 = versionTypeComparison != 0;
						if (flag4)
						{
							result = versionTypeComparison;
						}
						else
						{
							int majorComparison = x.Major.CompareTo(y.Major);
							bool flag5 = majorComparison != 0;
							if (flag5)
							{
								result = majorComparison;
							}
							else
							{
								int minorComparison = x.Minor.CompareTo(y.Minor);
								bool flag6 = minorComparison != 0;
								if (flag6)
								{
									result = minorComparison;
								}
								else
								{
									int revisionComparison = x.Revision.CompareTo(y.Revision);
									bool flag7 = revisionComparison != 0;
									if (flag7)
									{
										result = revisionComparison;
									}
									else
									{
										int changeSetComparison = x.ChangeSet.CompareTo(y.ChangeSet);
										bool flag8 = changeSetComparison != 0;
										if (flag8)
										{
											result = changeSetComparison;
										}
										else
										{
											result = 0;
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
	}
}
