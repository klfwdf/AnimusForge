using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000015 RID: 21
	public class WidgetAttributeValueTypeDefault : WidgetAttributeValueType
	{
		// Token: 0x06000081 RID: 129 RVA: 0x00002F40 File Offset: 0x00001140
		public override bool CheckValueType(string value)
		{
			return true;
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00002F43 File Offset: 0x00001143
		public override string GetAttributeValue(string value)
		{
			return value;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00002F46 File Offset: 0x00001146
		public override string GetSerializedValue(string value)
		{
			return value;
		}
	}
}
