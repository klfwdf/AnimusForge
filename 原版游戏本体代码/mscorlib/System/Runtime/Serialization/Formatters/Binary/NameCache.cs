using System;
using System.Collections.Concurrent;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000799 RID: 1945
	internal sealed class NameCache
	{
		// Token: 0x06005453 RID: 21587 RVA: 0x0012930C File Offset: 0x0012750C
		internal object GetCachedValue(string name)
		{
			this.name = name;
			object result;
			if (!NameCache.ht.TryGetValue(name, out result))
			{
				return null;
			}
			return result;
		}

		// Token: 0x06005454 RID: 21588 RVA: 0x00129332 File Offset: 0x00127532
		internal void SetCachedValue(object value)
		{
			NameCache.ht[this.name] = value;
		}

		// Token: 0x04002657 RID: 9815
		private static ConcurrentDictionary<string, object> ht = new ConcurrentDictionary<string, object>();

		// Token: 0x04002658 RID: 9816
		private string name;
	}
}
