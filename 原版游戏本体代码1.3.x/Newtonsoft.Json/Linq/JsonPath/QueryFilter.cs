using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000DB RID: 219
	[NullableContext(1)]
	[Nullable(0)]
	internal class QueryFilter : PathFilter
	{
		// Token: 0x06000C2E RID: 3118 RVA: 0x000310C7 File Offset: 0x0002F2C7
		public QueryFilter(QueryExpression expression)
		{
			this.Expression = expression;
		}

		// Token: 0x06000C2F RID: 3119 RVA: 0x000310D6 File Offset: 0x0002F2D6
		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, [Nullable(2)] JsonSelectSettings settings)
		{
			foreach (JToken jtoken in current)
			{
				foreach (JToken jtoken2 in ((IEnumerable<JToken>)jtoken))
				{
					if (this.Expression.IsMatch(root, jtoken2, settings))
					{
						yield return jtoken2;
					}
				}
				IEnumerator<JToken> enumerator2 = null;
			}
			IEnumerator<JToken> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x040003E8 RID: 1000
		internal QueryExpression Expression;
	}
}
