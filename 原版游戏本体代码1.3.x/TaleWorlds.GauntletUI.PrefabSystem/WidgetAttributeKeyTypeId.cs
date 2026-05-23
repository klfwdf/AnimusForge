using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000010 RID: 16
	public class WidgetAttributeKeyTypeId : WidgetAttributeKeyType
	{
		// Token: 0x06000068 RID: 104 RVA: 0x00002E61 File Offset: 0x00001061
		public override bool CheckKeyType(string key)
		{
			return key == "Id";
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00002E6E File Offset: 0x0000106E
		public override string GetKeyName(string key)
		{
			return "Id";
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00002E75 File Offset: 0x00001075
		public override string GetSerializedKey(string key)
		{
			return "Id";
		}
	}
}
