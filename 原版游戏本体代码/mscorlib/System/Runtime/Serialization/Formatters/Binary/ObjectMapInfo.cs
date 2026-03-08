using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200077C RID: 1916
	internal sealed class ObjectMapInfo
	{
		// Token: 0x060053B3 RID: 21427 RVA: 0x001269AD File Offset: 0x00124BAD
		internal ObjectMapInfo(int objectId, int numMembers, string[] memberNames, Type[] memberTypes)
		{
			this.objectId = objectId;
			this.numMembers = numMembers;
			this.memberNames = memberNames;
			this.memberTypes = memberTypes;
		}

		// Token: 0x060053B4 RID: 21428 RVA: 0x001269D4 File Offset: 0x00124BD4
		internal bool isCompatible(int numMembers, string[] memberNames, Type[] memberTypes)
		{
			bool result = true;
			if (this.numMembers == numMembers)
			{
				for (int i = 0; i < numMembers; i++)
				{
					if (!this.memberNames[i].Equals(memberNames[i]))
					{
						result = false;
						break;
					}
					if (memberTypes != null && this.memberTypes[i] != memberTypes[i])
					{
						result = false;
						break;
					}
				}
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x040025B5 RID: 9653
		internal int objectId;

		// Token: 0x040025B6 RID: 9654
		private int numMembers;

		// Token: 0x040025B7 RID: 9655
		private string[] memberNames;

		// Token: 0x040025B8 RID: 9656
		private Type[] memberTypes;
	}
}
