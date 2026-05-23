using System;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000011 RID: 17
	public class WidgetAttributeKeyTypeDataSource : WidgetAttributeKeyType
	{
		// Token: 0x060000A1 RID: 161 RVA: 0x00004509 File Offset: 0x00002709
		public override bool CheckKeyType(string key)
		{
			return key == "DataSource";
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00004516 File Offset: 0x00002716
		public override string GetKeyName(string key)
		{
			return "DataSource";
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x0000451D File Offset: 0x0000271D
		public override string GetSerializedKey(string key)
		{
			return "DataSource";
		}
	}
}
