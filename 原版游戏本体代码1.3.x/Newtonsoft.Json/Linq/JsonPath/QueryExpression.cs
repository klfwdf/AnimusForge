using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000D8 RID: 216
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class QueryExpression
	{
		// Token: 0x06000C20 RID: 3104 RVA: 0x00030B79 File Offset: 0x0002ED79
		public QueryExpression(QueryOperator @operator)
		{
			this.Operator = @operator;
		}

		// Token: 0x06000C21 RID: 3105 RVA: 0x00030B88 File Offset: 0x0002ED88
		public bool IsMatch(JToken root, JToken t)
		{
			return this.IsMatch(root, t, null);
		}

		// Token: 0x06000C22 RID: 3106
		public abstract bool IsMatch(JToken root, JToken t, [Nullable(2)] JsonSelectSettings settings);

		// Token: 0x040003E4 RID: 996
		internal QueryOperator Operator;
	}
}
