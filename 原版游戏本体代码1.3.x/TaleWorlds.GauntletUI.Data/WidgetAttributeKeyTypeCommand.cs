using System;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x0200000F RID: 15
	public class WidgetAttributeKeyTypeCommand : WidgetAttributeKeyType
	{
		// Token: 0x06000099 RID: 153 RVA: 0x000044A1 File Offset: 0x000026A1
		public override bool CheckKeyType(string key)
		{
			return key.StartsWith("Command.");
		}

		// Token: 0x0600009A RID: 154 RVA: 0x000044AE File Offset: 0x000026AE
		public override string GetKeyName(string key)
		{
			return key.Substring("Command.".Length);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000044C0 File Offset: 0x000026C0
		public override string GetSerializedKey(string key)
		{
			return "Command." + key;
		}
	}
}
