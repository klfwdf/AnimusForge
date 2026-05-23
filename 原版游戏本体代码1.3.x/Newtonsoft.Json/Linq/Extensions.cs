using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Contains the LINQ to JSON extension methods.
	/// </summary>
	// Token: 0x020000B8 RID: 184
	[NullableContext(1)]
	[Nullable(0)]
	public static class Extensions
	{
		/// <summary>
		/// Returns a collection of tokens that contains the ancestors of every token in the source collection.
		/// </summary>
		/// <typeparam name="T">The type of the objects in source, constrained to <see cref="T:Newtonsoft.Json.Linq.JToken" />.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the ancestors of every token in the source collection.</returns>
		// Token: 0x0600097A RID: 2426 RVA: 0x000280A7 File Offset: 0x000262A7
		public static IJEnumerable<JToken> Ancestors<[Nullable(0)] T>(this IEnumerable<T> source) where T : JToken
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			return source.SelectMany((T j) => j.Ancestors()).AsJEnumerable();
		}

		/// <summary>
		/// Returns a collection of tokens that contains every token in the source collection, and the ancestors of every token in the source collection.
		/// </summary>
		/// <typeparam name="T">The type of the objects in source, constrained to <see cref="T:Newtonsoft.Json.Linq.JToken" />.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains every token in the source collection, the ancestors of every token in the source collection.</returns>
		// Token: 0x0600097B RID: 2427 RVA: 0x000280DE File Offset: 0x000262DE
		public static IJEnumerable<JToken> AncestorsAndSelf<[Nullable(0)] T>(this IEnumerable<T> source) where T : JToken
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			return source.SelectMany((T j) => j.AncestorsAndSelf()).AsJEnumerable();
		}

		/// <summary>
		/// Returns a collection of tokens that contains the descendants of every token in the source collection.
		/// </summary>
		/// <typeparam name="T">The type of the objects in source, constrained to <see cref="T:Newtonsoft.Json.Linq.JContainer" />.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the descendants of every token in the source collection.</returns>
		// Token: 0x0600097C RID: 2428 RVA: 0x00028115 File Offset: 0x00026315
		public static IJEnumerable<JToken> Descendants<[Nullable(0)] T>(this IEnumerable<T> source) where T : JContainer
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			return source.SelectMany((T j) => j.Descendants()).AsJEnumerable();
		}

		/// <summary>
		/// Returns a collection of tokens that contains every token in the source collection, and the descendants of every token in the source collection.
		/// </summary>
		/// <typeparam name="T">The type of the objects in source, constrained to <see cref="T:Newtonsoft.Json.Linq.JContainer" />.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains every token in the source collection, and the descendants of every token in the source collection.</returns>
		// Token: 0x0600097D RID: 2429 RVA: 0x0002814C File Offset: 0x0002634C
		public static IJEnumerable<JToken> DescendantsAndSelf<[Nullable(0)] T>(this IEnumerable<T> source) where T : JContainer
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			return source.SelectMany((T j) => j.DescendantsAndSelf()).AsJEnumerable();
		}

		/// <summary>
		/// Returns a collection of child properties of every object in the source collection.
		/// </summary>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JObject" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JProperty" /> that contains the properties of every object in the source collection.</returns>
		// Token: 0x0600097E RID: 2430 RVA: 0x00028183 File Offset: 0x00026383
		public static IJEnumerable<JProperty> Properties(this IEnumerable<JObject> source)
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			return source.SelectMany((JObject d) => d.Properties()).AsJEnumerable<JProperty>();
		}

		/// <summary>
		/// Returns a collection of child values of every object in the source collection with the given key.
		/// </summary>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <param name="key">The token key.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the values of every token in the source collection with the given key.</returns>
		// Token: 0x0600097F RID: 2431 RVA: 0x000281BA File Offset: 0x000263BA
		public static IJEnumerable<JToken> Values(this IEnumerable<JToken> source, [Nullable(2)] object key)
		{
			return source.Values(key).AsJEnumerable();
		}

		/// <summary>
		/// Returns a collection of child values of every object in the source collection.
		/// </summary>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the values of every token in the source collection.</returns>
		// Token: 0x06000980 RID: 2432 RVA: 0x000281C8 File Offset: 0x000263C8
		public static IJEnumerable<JToken> Values(this IEnumerable<JToken> source)
		{
			return source.Values(null);
		}

		/// <summary>
		/// Returns a collection of converted child values of every object in the source collection with the given key.
		/// </summary>
		/// <typeparam name="U">The type to convert the values to.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <param name="key">The token key.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the converted values of every token in the source collection with the given key.</returns>
		// Token: 0x06000981 RID: 2433 RVA: 0x000281D1 File Offset: 0x000263D1
		[return: Nullable(new byte[] { 1, 2 })]
		public static IEnumerable<U> Values<[Nullable(2)] U>(this IEnumerable<JToken> source, object key)
		{
			return source.Values(key);
		}

		/// <summary>
		/// Returns a collection of converted child values of every object in the source collection.
		/// </summary>
		/// <typeparam name="U">The type to convert the values to.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the converted values of every token in the source collection.</returns>
		// Token: 0x06000982 RID: 2434 RVA: 0x000281DA File Offset: 0x000263DA
		[return: Nullable(new byte[] { 1, 2 })]
		public static IEnumerable<U> Values<[Nullable(2)] U>(this IEnumerable<JToken> source)
		{
			return source.Values(null);
		}

		/// <summary>
		/// Converts the value.
		/// </summary>
		/// <typeparam name="U">The type to convert the value to.</typeparam>
		/// <param name="value">A <see cref="T:Newtonsoft.Json.Linq.JToken" /> cast as a <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <returns>A converted value.</returns>
		// Token: 0x06000983 RID: 2435 RVA: 0x000281E3 File Offset: 0x000263E3
		[NullableContext(2)]
		public static U Value<U>([Nullable(1)] this IEnumerable<JToken> value)
		{
			return value.Value<JToken, U>();
		}

		/// <summary>
		/// Converts the value.
		/// </summary>
		/// <typeparam name="T">The source collection type.</typeparam>
		/// <typeparam name="U">The type to convert the value to.</typeparam>
		/// <param name="value">A <see cref="T:Newtonsoft.Json.Linq.JToken" /> cast as a <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <returns>A converted value.</returns>
		// Token: 0x06000984 RID: 2436 RVA: 0x000281EB File Offset: 0x000263EB
		[return: Nullable(2)]
		public static U Value<[Nullable(0)] T, [Nullable(2)] U>(this IEnumerable<T> value) where T : JToken
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			JToken jtoken = value as JToken;
			if (jtoken == null)
			{
				throw new ArgumentException("Source value must be a JToken.");
			}
			return jtoken.Convert<JToken, U>();
		}

		// Token: 0x06000985 RID: 2437 RVA: 0x00028211 File Offset: 0x00026411
		[return: Nullable(new byte[] { 1, 2 })]
		internal static IEnumerable<U> Values<[Nullable(0)] T, [Nullable(2)] U>(this IEnumerable<T> source, [Nullable(2)] object key) where T : JToken
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			if (key == null)
			{
				foreach (T t in source)
				{
					JValue jvalue = t as JValue;
					if (jvalue != null)
					{
						yield return jvalue.Convert<JValue, U>();
					}
					else
					{
						foreach (JToken token in t.Children())
						{
							yield return token.Convert<JToken, U>();
						}
						IEnumerator<JToken> enumerator2 = null;
					}
				}
				IEnumerator<T> enumerator = null;
			}
			else
			{
				foreach (T t2 in source)
				{
					JToken jtoken = t2[key];
					if (jtoken != null)
					{
						yield return jtoken.Convert<JToken, U>();
					}
				}
				IEnumerator<T> enumerator = null;
			}
			yield break;
			yield break;
		}

		/// <summary>
		/// Returns a collection of child tokens of every array in the source collection.
		/// </summary>
		/// <typeparam name="T">The source collection type.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the values of every token in the source collection.</returns>
		// Token: 0x06000986 RID: 2438 RVA: 0x00028228 File Offset: 0x00026428
		public static IJEnumerable<JToken> Children<[Nullable(0)] T>(this IEnumerable<T> source) where T : JToken
		{
			return source.Children<T, JToken>().AsJEnumerable();
		}

		/// <summary>
		/// Returns a collection of converted child tokens of every array in the source collection.
		/// </summary>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <typeparam name="U">The type to convert the values to.</typeparam>
		/// <typeparam name="T">The source collection type.</typeparam>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the converted values of every token in the source collection.</returns>
		// Token: 0x06000987 RID: 2439 RVA: 0x00028235 File Offset: 0x00026435
		[return: Nullable(new byte[] { 1, 2 })]
		public static IEnumerable<U> Children<[Nullable(0)] T, [Nullable(2)] U>(this IEnumerable<T> source) where T : JToken
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			return source.SelectMany((T c) => c.Children()).Convert<JToken, U>();
		}

		// Token: 0x06000988 RID: 2440 RVA: 0x0002826C File Offset: 0x0002646C
		[return: Nullable(new byte[] { 1, 2 })]
		internal static IEnumerable<U> Convert<[Nullable(0)] T, [Nullable(2)] U>(this IEnumerable<T> source) where T : JToken
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			foreach (T t in source)
			{
				yield return t.Convert<JToken, U>();
			}
			IEnumerator<T> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000989 RID: 2441 RVA: 0x0002827C File Offset: 0x0002647C
		[NullableContext(2)]
		internal static U Convert<[Nullable(0)] T, U>([Nullable(1)] this T token) where T : JToken
		{
			if (token == null)
			{
				return default(U);
			}
			if (token is U)
			{
				U result = token as U;
				if (typeof(U) != typeof(IComparable) && typeof(U) != typeof(IFormattable))
				{
					return result;
				}
			}
			JValue jvalue = token as JValue;
			if (jvalue == null)
			{
				throw new InvalidCastException("Cannot cast {0} to {1}.".FormatWith(CultureInfo.InvariantCulture, token.GetType(), typeof(T)));
			}
			object value = jvalue.Value;
			if (value is U)
			{
				return (U)((object)value);
			}
			Type type = typeof(U);
			if (ReflectionUtils.IsNullableType(type))
			{
				if (jvalue.Value == null)
				{
					return default(U);
				}
				type = Nullable.GetUnderlyingType(type);
			}
			return (U)((object)System.Convert.ChangeType(jvalue.Value, type, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Returns the input typed as <see cref="T:Newtonsoft.Json.Linq.IJEnumerable`1" />.
		/// </summary>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>The input typed as <see cref="T:Newtonsoft.Json.Linq.IJEnumerable`1" />.</returns>
		// Token: 0x0600098A RID: 2442 RVA: 0x0002838C File Offset: 0x0002658C
		public static IJEnumerable<JToken> AsJEnumerable(this IEnumerable<JToken> source)
		{
			return source.AsJEnumerable<JToken>();
		}

		/// <summary>
		/// Returns the input typed as <see cref="T:Newtonsoft.Json.Linq.IJEnumerable`1" />.
		/// </summary>
		/// <typeparam name="T">The source collection type.</typeparam>
		/// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the source collection.</param>
		/// <returns>The input typed as <see cref="T:Newtonsoft.Json.Linq.IJEnumerable`1" />.</returns>
		// Token: 0x0600098B RID: 2443 RVA: 0x00028394 File Offset: 0x00026594
		public static IJEnumerable<T> AsJEnumerable<[Nullable(0)] T>(this IEnumerable<T> source) where T : JToken
		{
			if (source == null)
			{
				return null;
			}
			IJEnumerable<T> ijenumerable = source as IJEnumerable<T>;
			if (ijenumerable != null)
			{
				return ijenumerable;
			}
			return new JEnumerable<T>(source);
		}
	}
}
