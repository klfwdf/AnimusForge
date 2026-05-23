using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a collection of <see cref="T:Newtonsoft.Json.Linq.JToken" /> objects.
	/// </summary>
	/// <typeparam name="T">The type of token.</typeparam>
	// Token: 0x020000BD RID: 189
	[NullableContext(1)]
	[Nullable(0)]
	public readonly struct JEnumerable<[Nullable(0)] T> : IJEnumerable<T>, IEnumerable<T>, IEnumerable, IEquatable<JEnumerable<T>> where T : JToken
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> struct.
		/// </summary>
		/// <param name="enumerable">The enumerable.</param>
		// Token: 0x06000A2D RID: 2605 RVA: 0x00029D0C File Offset: 0x00027F0C
		public JEnumerable(IEnumerable<T> enumerable)
		{
			ValidationUtils.ArgumentNotNull(enumerable, "enumerable");
			this._enumerable = enumerable;
		}

		/// <summary>
		/// Returns an enumerator that can be used to iterate through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		// Token: 0x06000A2E RID: 2606 RVA: 0x00029D20 File Offset: 0x00027F20
		public IEnumerator<T> GetEnumerator()
		{
			return (this._enumerable ?? JEnumerable<T>.Empty).GetEnumerator();
		}

		// Token: 0x06000A2F RID: 2607 RVA: 0x00029D3B File Offset: 0x00027F3B
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.IJEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.
		/// </summary>
		/// <value></value>
		// Token: 0x170001D9 RID: 473
		public IJEnumerable<JToken> this[object key]
		{
			get
			{
				if (this._enumerable == null)
				{
					return JEnumerable<JToken>.Empty;
				}
				return new JEnumerable<JToken>(this._enumerable.Values(key));
			}
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> is equal to this instance.
		/// </summary>
		/// <param name="other">The <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000A31 RID: 2609 RVA: 0x00029D6E File Offset: 0x00027F6E
		public bool Equals([Nullable(new byte[] { 0, 1 })] JEnumerable<T> other)
		{
			return object.Equals(this._enumerable, other._enumerable);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object" /> is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object" /> to compare with this instance.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000A32 RID: 2610 RVA: 0x00029D84 File Offset: 0x00027F84
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is JEnumerable<T>)
			{
				JEnumerable<T> other = (JEnumerable<T>)obj;
				return this.Equals(other);
			}
			return false;
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		// Token: 0x06000A33 RID: 2611 RVA: 0x00029DA9 File Offset: 0x00027FA9
		public override int GetHashCode()
		{
			if (this._enumerable == null)
			{
				return 0;
			}
			return this._enumerable.GetHashCode();
		}

		/// <summary>
		/// An empty collection of <see cref="T:Newtonsoft.Json.Linq.JToken" /> objects.
		/// </summary>
		// Token: 0x04000381 RID: 897
		[Nullable(new byte[] { 0, 1 })]
		public static readonly JEnumerable<T> Empty = new JEnumerable<T>(Enumerable.Empty<T>());

		// Token: 0x04000382 RID: 898
		private readonly IEnumerable<T> _enumerable;
	}
}
