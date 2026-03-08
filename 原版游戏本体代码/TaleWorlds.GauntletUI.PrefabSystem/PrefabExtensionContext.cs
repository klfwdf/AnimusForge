using System;
using System.Collections.Generic;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200000A RID: 10
	public class PrefabExtensionContext
	{
		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600003F RID: 63 RVA: 0x0000285B File Offset: 0x00000A5B
		public IEnumerable<PrefabExtension> PrefabExtensions
		{
			get
			{
				return this._prefabExtensions;
			}
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002863 File Offset: 0x00000A63
		public PrefabExtensionContext()
		{
			this._prefabExtensions = new List<PrefabExtension>();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002876 File Offset: 0x00000A76
		public void AddExtension(PrefabExtension prefabExtension)
		{
			this._prefabExtensions.Add(prefabExtension);
		}

		// Token: 0x0400001E RID: 30
		private List<PrefabExtension> _prefabExtensions;
	}
}
