using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000016 RID: 22
	public class WidgetAttributeValueTypeParameter : WidgetAttributeValueType
	{
		// Token: 0x06000085 RID: 133 RVA: 0x00002F51 File Offset: 0x00001151
		public override bool CheckValueType(string value)
		{
			return value.StartsWith("*");
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00002F5E File Offset: 0x0000115E
		public override string GetAttributeValue(string value)
		{
			return value.Substring("*".Length);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00002F70 File Offset: 0x00001170
		public override string GetSerializedValue(string value)
		{
			return "*" + value;
		}
	}
}
