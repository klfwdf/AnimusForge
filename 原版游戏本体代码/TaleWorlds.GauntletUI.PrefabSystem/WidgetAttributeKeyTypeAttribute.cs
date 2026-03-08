using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200000F RID: 15
	public class WidgetAttributeKeyTypeAttribute : WidgetAttributeKeyType
	{
		// Token: 0x06000064 RID: 100 RVA: 0x00002E50 File Offset: 0x00001050
		public override bool CheckKeyType(string key)
		{
			return true;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00002E53 File Offset: 0x00001053
		public override string GetKeyName(string key)
		{
			return key;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00002E56 File Offset: 0x00001056
		public override string GetSerializedKey(string key)
		{
			return key;
		}
	}
}
