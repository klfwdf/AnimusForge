using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000045 RID: 69
	internal interface IWrappedCollection : IList, ICollection, IEnumerable
	{
		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x06000451 RID: 1105
		[Nullable(1)]
		object UnderlyingCollection
		{
			[NullableContext(1)]
			get;
		}
	}
}
