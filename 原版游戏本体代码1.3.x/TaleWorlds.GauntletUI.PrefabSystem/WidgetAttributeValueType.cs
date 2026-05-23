using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000013 RID: 19
	public abstract class WidgetAttributeValueType
	{
		// Token: 0x06000079 RID: 121
		public abstract bool CheckValueType(string value);

		// Token: 0x0600007A RID: 122
		public abstract string GetAttributeValue(string value);

		// Token: 0x0600007B RID: 123
		public abstract string GetSerializedValue(string value);
	}
}
