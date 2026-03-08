using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000011 RID: 17
	public class WidgetAttributeKeyTypeParameter : WidgetAttributeKeyType
	{
		// Token: 0x0600006C RID: 108 RVA: 0x00002E84 File Offset: 0x00001084
		public override bool CheckKeyType(string key)
		{
			return key.StartsWith("Parameter.");
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00002E91 File Offset: 0x00001091
		public override string GetKeyName(string key)
		{
			return key.Substring("Parameter.".Length);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00002EA3 File Offset: 0x000010A3
		public override string GetSerializedKey(string key)
		{
			return "Parameter." + key;
		}
	}
}
