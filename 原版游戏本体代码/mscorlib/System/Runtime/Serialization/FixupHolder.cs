using System;

namespace System.Runtime.Serialization
{
	// Token: 0x0200074D RID: 1869
	[Serializable]
	internal class FixupHolder
	{
		// Token: 0x060052B2 RID: 21170 RVA: 0x00122BD6 File Offset: 0x00120DD6
		internal FixupHolder(long id, object fixupInfo, int fixupType)
		{
			this.m_id = id;
			this.m_fixupInfo = fixupInfo;
			this.m_fixupType = fixupType;
		}

		// Token: 0x040024A0 RID: 9376
		internal const int ArrayFixup = 1;

		// Token: 0x040024A1 RID: 9377
		internal const int MemberFixup = 2;

		// Token: 0x040024A2 RID: 9378
		internal const int DelayedFixup = 4;

		// Token: 0x040024A3 RID: 9379
		internal long m_id;

		// Token: 0x040024A4 RID: 9380
		internal object m_fixupInfo;

		// Token: 0x040024A5 RID: 9381
		internal int m_fixupType;
	}
}
