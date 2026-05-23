using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000D9 RID: 217
	[NullableContext(1)]
	[Nullable(0)]
	internal class CompositeExpression : QueryExpression
	{
		// Token: 0x17000211 RID: 529
		// (get) Token: 0x06000C23 RID: 3107 RVA: 0x00030B93 File Offset: 0x0002ED93
		// (set) Token: 0x06000C24 RID: 3108 RVA: 0x00030B9B File Offset: 0x0002ED9B
		public List<QueryExpression> Expressions { get; set; }

		// Token: 0x06000C25 RID: 3109 RVA: 0x00030BA4 File Offset: 0x0002EDA4
		public CompositeExpression(QueryOperator @operator)
			: base(@operator)
		{
			this.Expressions = new List<QueryExpression>();
		}

		// Token: 0x06000C26 RID: 3110 RVA: 0x00030BB8 File Offset: 0x0002EDB8
		public override bool IsMatch(JToken root, JToken t, [Nullable(2)] JsonSelectSettings settings)
		{
			QueryOperator @operator = this.Operator;
			if (@operator == QueryOperator.And)
			{
				using (List<QueryExpression>.Enumerator enumerator = this.Expressions.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (!enumerator.Current.IsMatch(root, t, settings))
						{
							return false;
						}
					}
				}
				return true;
			}
			if (@operator != QueryOperator.Or)
			{
				throw new ArgumentOutOfRangeException();
			}
			using (List<QueryExpression>.Enumerator enumerator = this.Expressions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsMatch(root, t, settings))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
