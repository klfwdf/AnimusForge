using System;

namespace System
{
	// Token: 0x0200008B RID: 139
	internal struct SwitchStructure
	{
		// Token: 0x06000731 RID: 1841 RVA: 0x000192A1 File Offset: 0x000174A1
		internal SwitchStructure(string n, int v)
		{
			this.name = n;
			this.value = v;
		}

		// Token: 0x0400036A RID: 874
		internal string name;

		// Token: 0x0400036B RID: 875
		internal int value;
	}
}
