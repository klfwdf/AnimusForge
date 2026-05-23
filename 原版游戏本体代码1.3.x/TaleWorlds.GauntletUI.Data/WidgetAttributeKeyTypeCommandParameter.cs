using System;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000010 RID: 16
	public class WidgetAttributeKeyTypeCommandParameter : WidgetAttributeKeyType
	{
		// Token: 0x0600009D RID: 157 RVA: 0x000044D5 File Offset: 0x000026D5
		public override bool CheckKeyType(string key)
		{
			return key.StartsWith("CommandParameter.");
		}

		// Token: 0x0600009E RID: 158 RVA: 0x000044E2 File Offset: 0x000026E2
		public override string GetKeyName(string key)
		{
			return key.Substring("CommandParameter.".Length);
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000044F4 File Offset: 0x000026F4
		public override string GetSerializedKey(string key)
		{
			return "CommandParameter." + key;
		}
	}
}
