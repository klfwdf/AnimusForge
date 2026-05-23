using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000D0 RID: 208
	internal class ArrayIndexFilter : PathFilter
	{
		// Token: 0x1700020C RID: 524
		// (get) Token: 0x06000BF2 RID: 3058 RVA: 0x0002F7AD File Offset: 0x0002D9AD
		// (set) Token: 0x06000BF3 RID: 3059 RVA: 0x0002F7B5 File Offset: 0x0002D9B5
		public int? Index { get; set; }

		// Token: 0x06000BF4 RID: 3060 RVA: 0x0002F7BE File Offset: 0x0002D9BE
		[NullableContext(1)]
		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, [Nullable(2)] JsonSelectSettings settings)
		{
			foreach (JToken jtoken in current)
			{
				if (this.Index != null)
				{
					JToken tokenIndex = PathFilter.GetTokenIndex(jtoken, settings, this.Index.GetValueOrDefault());
					if (tokenIndex != null)
					{
						yield return tokenIndex;
					}
				}
				else if (jtoken is JArray || jtoken is JConstructor)
				{
					foreach (JToken jtoken2 in ((IEnumerable<JToken>)jtoken))
					{
						yield return jtoken2;
					}
					IEnumerator<JToken> enumerator2 = null;
				}
				else if (settings != null && settings.ErrorWhenNoMatch)
				{
					throw new JsonException("Index * not valid on {0}.".FormatWith(CultureInfo.InvariantCulture, jtoken.GetType().Name));
				}
			}
			IEnumerator<JToken> enumerator = null;
			yield break;
			yield break;
		}
	}
}
