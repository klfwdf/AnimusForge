using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a collection of <see cref="T:Newtonsoft.Json.Linq.JToken" /> objects.
	/// </summary>
	/// <typeparam name="T">The type of token.</typeparam>
	// Token: 0x020000B9 RID: 185
	[NullableContext(1)]
	public interface IJEnumerable<[Nullable(0)] out T> : IEnumerable<T>, IEnumerable where T : JToken
	{
		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.IJEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.
		/// </summary>
		/// <value></value>
		// Token: 0x170001BA RID: 442
		IJEnumerable<JToken> this[object key] { get; }
	}
}
