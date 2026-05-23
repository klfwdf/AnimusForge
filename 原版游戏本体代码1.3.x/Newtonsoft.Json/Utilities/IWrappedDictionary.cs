using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200004E RID: 78
	internal interface IWrappedDictionary : IDictionary, ICollection, IEnumerable
	{
		// Token: 0x170000AE RID: 174
		// (get) Token: 0x060004AE RID: 1198
		[Nullable(1)]
		object UnderlyingDictionary
		{
			[NullableContext(1)]
			get;
		}
	}
}
