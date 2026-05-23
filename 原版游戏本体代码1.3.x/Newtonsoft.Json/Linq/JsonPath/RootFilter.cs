using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000DD RID: 221
	[NullableContext(1)]
	[Nullable(0)]
	internal class RootFilter : PathFilter
	{
		// Token: 0x06000C32 RID: 3122 RVA: 0x0003112F File Offset: 0x0002F32F
		private RootFilter()
		{
		}

		// Token: 0x06000C33 RID: 3123 RVA: 0x00031137 File Offset: 0x0002F337
		public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, [Nullable(2)] JsonSelectSettings settings)
		{
			return new JToken[] { root };
		}

		// Token: 0x040003EA RID: 1002
		public static readonly RootFilter Instance = new RootFilter();
	}
}
