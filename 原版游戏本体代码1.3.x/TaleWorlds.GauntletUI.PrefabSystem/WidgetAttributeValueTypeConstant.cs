using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000014 RID: 20
	public class WidgetAttributeValueTypeConstant : WidgetAttributeValueType
	{
		// Token: 0x0600007D RID: 125 RVA: 0x00002F0C File Offset: 0x0000110C
		public override bool CheckValueType(string value)
		{
			return value.StartsWith("!");
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00002F19 File Offset: 0x00001119
		public override string GetAttributeValue(string value)
		{
			return value.Substring("!".Length);
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00002F2B File Offset: 0x0000112B
		public override string GetSerializedValue(string value)
		{
			return "!" + value;
		}
	}
}
