using System;

namespace SandBox.ViewModelCollection.GameOver
{
	// Token: 0x0200005C RID: 92
	public class StatItem
	{
		// Token: 0x06000592 RID: 1426 RVA: 0x00014BB8 File Offset: 0x00012DB8
		public StatItem(string id, string value, StatItem.StatType type = StatItem.StatType.None)
		{
			this.ID = id;
			this.Value = value;
			this.Type = type;
		}

		// Token: 0x040002BE RID: 702
		public readonly string ID;

		// Token: 0x040002BF RID: 703
		public readonly string Value;

		// Token: 0x040002C0 RID: 704
		public readonly StatItem.StatType Type;

		// Token: 0x020000B9 RID: 185
		public enum StatType
		{
			// Token: 0x04000403 RID: 1027
			None,
			// Token: 0x04000404 RID: 1028
			Influence,
			// Token: 0x04000405 RID: 1029
			Issue,
			// Token: 0x04000406 RID: 1030
			Tournament,
			// Token: 0x04000407 RID: 1031
			Gold,
			// Token: 0x04000408 RID: 1032
			Crime,
			// Token: 0x04000409 RID: 1033
			Kill
		}
	}
}
