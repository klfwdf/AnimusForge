using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000D1 RID: 209
	[NullableContext(1)]
	[Nullable(0)]
	internal class ArrayMultipleIndexFilter : PathFilter
	{
		// Token: 0x06000BF6 RID: 3062 RVA: 0x0002F7E4 File Offset: 0x0002D9E4
		public ArrayMultipleIndexFilter(List<int> indexes)
		{
			this.Indexes = indexes;
		}

		// Token: 0x06000BF7 RID: 3063 RVA: 0x0002F7F3 File Offset: 0x0002D9F3
		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, [Nullable(2)] JsonSelectSettings settings)
		{
			foreach (JToken t in current)
			{
				foreach (int index in this.Indexes)
				{
					JToken tokenIndex = PathFilter.GetTokenIndex(t, settings, index);
					if (tokenIndex != null)
					{
						yield return tokenIndex;
					}
				}
				List<int>.Enumerator enumerator2 = default(List<int>.Enumerator);
				t = null;
			}
			IEnumerator<JToken> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x040003CC RID: 972
		internal List<int> Indexes;
	}
}
