using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HarmonyLib
{
	// Token: 0x0200002D RID: 45
	internal class LocalBuilderState
	{
		// Token: 0x060000DE RID: 222 RVA: 0x00005FAE File Offset: 0x000041AE
		public void Add(string key, LocalBuilder local)
		{
			this.locals[key] = local;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00005FBD File Offset: 0x000041BD
		public bool TryGetValue(string key, out LocalBuilder local)
		{
			return this.locals.TryGetValue(key, out local);
		}

		// Token: 0x1700000D RID: 13
		public LocalBuilder this[string key]
		{
			get
			{
				return this.locals[key];
			}
			set
			{
				this.locals[key] = value;
			}
		}

		// Token: 0x04000087 RID: 135
		private readonly Dictionary<string, LocalBuilder> locals = new Dictionary<string, LocalBuilder>();
	}
}
