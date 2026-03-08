using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200005D RID: 93
	public readonly struct UndoRedoKey
	{
		// Token: 0x0600072E RID: 1838 RVA: 0x00018D64 File Offset: 0x00016F64
		public UndoRedoKey(int gender, int race, BodyProperties bodyProperties)
		{
			this.Gender = gender;
			this.Race = race;
			this.BodyProperties = bodyProperties;
		}

		// Token: 0x04000399 RID: 921
		public readonly int Gender;

		// Token: 0x0400039A RID: 922
		public readonly int Race;

		// Token: 0x0400039B RID: 923
		public readonly BodyProperties BodyProperties;
	}
}
