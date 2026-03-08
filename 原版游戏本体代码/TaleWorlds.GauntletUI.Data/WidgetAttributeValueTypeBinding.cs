using System;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000012 RID: 18
	public class WidgetAttributeValueTypeBinding : WidgetAttributeValueType
	{
		// Token: 0x060000A5 RID: 165 RVA: 0x0000452C File Offset: 0x0000272C
		public override bool CheckValueType(string value)
		{
			return value.StartsWith("@");
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00004539 File Offset: 0x00002739
		public override string GetAttributeValue(string value)
		{
			return value.Substring("@".Length);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000454B File Offset: 0x0000274B
		public override string GetSerializedValue(string value)
		{
			return "@" + value;
		}
	}
}
