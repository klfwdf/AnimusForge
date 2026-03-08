using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Compares tokens to determine whether they are equal.
	/// </summary>
	// Token: 0x020000C8 RID: 200
	public class JTokenEqualityComparer : IEqualityComparer<JToken>
	{
		/// <summary>
		/// Determines whether the specified objects are equal.
		/// </summary>
		/// <param name="x">The first object of type <see cref="T:Newtonsoft.Json.Linq.JToken" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="T:Newtonsoft.Json.Linq.JToken" /> to compare.</param>
		/// <returns>
		/// <c>true</c> if the specified objects are equal; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000B77 RID: 2935 RVA: 0x0002DA0A File Offset: 0x0002BC0A
		[NullableContext(2)]
		public bool Equals(JToken x, JToken y)
		{
			return JToken.DeepEquals(x, y);
		}

		/// <summary>
		/// Returns a hash code for the specified object.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified object.</returns>
		/// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is <c>null</c>.</exception>
		// Token: 0x06000B78 RID: 2936 RVA: 0x0002DA13 File Offset: 0x0002BC13
		[NullableContext(1)]
		public int GetHashCode(JToken obj)
		{
			if (obj == null)
			{
				return 0;
			}
			return obj.GetDeepHashCode();
		}
	}
}
