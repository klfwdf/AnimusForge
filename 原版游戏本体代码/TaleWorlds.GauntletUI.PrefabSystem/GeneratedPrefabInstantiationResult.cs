using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000007 RID: 7
	public class GeneratedPrefabInstantiationResult
	{
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600002E RID: 46 RVA: 0x000027EB File Offset: 0x000009EB
		// (set) Token: 0x0600002F RID: 47 RVA: 0x000027F3 File Offset: 0x000009F3
		public Widget Root { get; private set; }

		// Token: 0x06000030 RID: 48 RVA: 0x000027FC File Offset: 0x000009FC
		public GeneratedPrefabInstantiationResult(Widget root)
		{
			this.Root = root;
			this._data = new Dictionary<string, object>();
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002816 File Offset: 0x00000A16
		public void AddData(string tag, object data)
		{
			this._data.Add(tag, data);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002828 File Offset: 0x00000A28
		public object GetExtensionData(string tag)
		{
			object result;
			this._data.TryGetValue(tag, out result);
			return result;
		}

		// Token: 0x0400001D RID: 29
		private Dictionary<string, object> _data;
	}
}
