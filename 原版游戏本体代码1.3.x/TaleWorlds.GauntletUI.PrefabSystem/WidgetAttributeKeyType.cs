using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200000E RID: 14
	public abstract class WidgetAttributeKeyType
	{
		// Token: 0x06000060 RID: 96
		public abstract bool CheckKeyType(string key);

		// Token: 0x06000061 RID: 97
		public abstract string GetKeyName(string key);

		// Token: 0x06000062 RID: 98
		public abstract string GetSerializedKey(string key);
	}
}
