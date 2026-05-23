using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq.JsonPath;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents an abstract JSON token.
	/// </summary>
	// Token: 0x020000C7 RID: 199
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class JToken : IJEnumerable<JToken>, IEnumerable<JToken>, IEnumerable, IJsonLineInfo, ICloneable, IDynamicMetaObjectProvider
	{
		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" /> asynchronously.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous write operation.</returns>
		// Token: 0x06000AD6 RID: 2774 RVA: 0x0002B3EA File Offset: 0x000295EA
		public virtual Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" /> asynchronously.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous write operation.</returns>
		// Token: 0x06000AD7 RID: 2775 RVA: 0x0002B3F4 File Offset: 0x000295F4
		public Task WriteToAsync(JsonWriter writer, params JsonConverter[] converters)
		{
			return this.WriteToAsync(writer, default(CancellationToken), converters);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">An <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous creation. The
		/// <see cref="P:System.Threading.Tasks.Task`1.Result" /> property returns a <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains 
		/// the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000AD8 RID: 2776 RVA: 0x0002B412 File Offset: 0x00029612
		public static Task<JToken> ReadFromAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			return JToken.ReadFromAsync(reader, null, cancellationToken);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">An <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous creation. The
		/// <see cref="P:System.Threading.Tasks.Task`1.Result" /> property returns a <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains 
		/// the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000AD9 RID: 2777 RVA: 0x0002B41C File Offset: 0x0002961C
		public static async Task<JToken> ReadFromAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			if (reader.TokenType == JsonToken.None)
			{
				ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter = ((settings != null && settings.CommentHandling == CommentHandling.Ignore) ? reader.ReadAndMoveToContentAsync(cancellationToken) : reader.ReadAsync(cancellationToken)).ConfigureAwait(false).GetAwaiter();
				if (!configuredTaskAwaiter.IsCompleted)
				{
					await configuredTaskAwaiter;
					ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter2;
					configuredTaskAwaiter = configuredTaskAwaiter2;
					configuredTaskAwaiter2 = default(ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter);
				}
				if (!configuredTaskAwaiter.GetResult())
				{
					throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader.");
				}
			}
			IJsonLineInfo lineInfo = reader as IJsonLineInfo;
			switch (reader.TokenType)
			{
			case JsonToken.StartObject:
				return await JObject.LoadAsync(reader, settings, cancellationToken).ConfigureAwait(false);
			case JsonToken.StartArray:
				return await JArray.LoadAsync(reader, settings, cancellationToken).ConfigureAwait(false);
			case JsonToken.StartConstructor:
				return await JConstructor.LoadAsync(reader, settings, cancellationToken).ConfigureAwait(false);
			case JsonToken.PropertyName:
				return await JProperty.LoadAsync(reader, settings, cancellationToken).ConfigureAwait(false);
			case JsonToken.Comment:
			{
				object value = reader.Value;
				JValue jvalue = JValue.CreateComment((value != null) ? value.ToString() : null);
				jvalue.SetLineInfo(lineInfo, settings);
				return jvalue;
			}
			case JsonToken.Integer:
			case JsonToken.Float:
			case JsonToken.String:
			case JsonToken.Boolean:
			case JsonToken.Date:
			case JsonToken.Bytes:
			{
				JValue jvalue2 = new JValue(reader.Value);
				jvalue2.SetLineInfo(lineInfo, settings);
				return jvalue2;
			}
			case JsonToken.Null:
			{
				JValue jvalue3 = JValue.CreateNull();
				jvalue3.SetLineInfo(lineInfo, settings);
				return jvalue3;
			}
			case JsonToken.Undefined:
			{
				JValue jvalue4 = JValue.CreateUndefined();
				jvalue4.SetLineInfo(lineInfo, settings);
				return jvalue4;
			}
			}
			throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader. Unexpected token: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
		}

		/// <summary>
		/// Asynchronously creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous creation. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns a <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000ADA RID: 2778 RVA: 0x0002B46F File Offset: 0x0002966F
		public static Task<JToken> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			return JToken.LoadAsync(reader, null, cancellationToken);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous creation. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns a <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000ADB RID: 2779 RVA: 0x0002B479 File Offset: 0x00029679
		public static Task<JToken> LoadAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
		{
			return JToken.ReadFromAsync(reader, settings, cancellationToken);
		}

		/// <summary>
		/// Gets a comparer that can compare two tokens for value equality.
		/// </summary>
		/// <value>A <see cref="T:Newtonsoft.Json.Linq.JTokenEqualityComparer" /> that can compare two nodes for value equality.</value>
		// Token: 0x170001F5 RID: 501
		// (get) Token: 0x06000ADC RID: 2780 RVA: 0x0002B483 File Offset: 0x00029683
		public static JTokenEqualityComparer EqualityComparer
		{
			get
			{
				if (JToken._equalityComparer == null)
				{
					JToken._equalityComparer = new JTokenEqualityComparer();
				}
				return JToken._equalityComparer;
			}
		}

		/// <summary>
		/// Gets or sets the parent.
		/// </summary>
		/// <value>The parent.</value>
		// Token: 0x170001F6 RID: 502
		// (get) Token: 0x06000ADD RID: 2781 RVA: 0x0002B49B File Offset: 0x0002969B
		// (set) Token: 0x06000ADE RID: 2782 RVA: 0x0002B4A3 File Offset: 0x000296A3
		[Nullable(2)]
		public JContainer Parent
		{
			[NullableContext(2)]
			[DebuggerStepThrough]
			get
			{
				return this._parent;
			}
			[NullableContext(2)]
			internal set
			{
				this._parent = value;
			}
		}

		/// <summary>
		/// Gets the root <see cref="T:Newtonsoft.Json.Linq.JToken" /> of this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <value>The root <see cref="T:Newtonsoft.Json.Linq.JToken" /> of this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</value>
		// Token: 0x170001F7 RID: 503
		// (get) Token: 0x06000ADF RID: 2783 RVA: 0x0002B4AC File Offset: 0x000296AC
		public JToken Root
		{
			get
			{
				JContainer parent = this.Parent;
				if (parent == null)
				{
					return this;
				}
				while (parent.Parent != null)
				{
					parent = parent.Parent;
				}
				return parent;
			}
		}

		// Token: 0x06000AE0 RID: 2784
		internal abstract JToken CloneToken([Nullable(2)] JsonCloneSettings settings);

		// Token: 0x06000AE1 RID: 2785
		internal abstract bool DeepEquals(JToken node);

		/// <summary>
		/// Gets the node type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <value>The type.</value>
		// Token: 0x170001F8 RID: 504
		// (get) Token: 0x06000AE2 RID: 2786
		public abstract JTokenType Type { get; }

		/// <summary>
		/// Gets a value indicating whether this token has child tokens.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this token has child values; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170001F9 RID: 505
		// (get) Token: 0x06000AE3 RID: 2787
		public abstract bool HasValues { get; }

		/// <summary>
		/// Compares the values of two tokens, including the values of all descendant tokens.
		/// </summary>
		/// <param name="t1">The first <see cref="T:Newtonsoft.Json.Linq.JToken" /> to compare.</param>
		/// <param name="t2">The second <see cref="T:Newtonsoft.Json.Linq.JToken" /> to compare.</param>
		/// <returns><c>true</c> if the tokens are equal; otherwise <c>false</c>.</returns>
		// Token: 0x06000AE4 RID: 2788 RVA: 0x0002B4D5 File Offset: 0x000296D5
		[NullableContext(2)]
		public static bool DeepEquals(JToken t1, JToken t2)
		{
			return t1 == t2 || (t1 != null && t2 != null && t1.DeepEquals(t2));
		}

		/// <summary>
		/// Gets the next sibling token of this node.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the next sibling token.</value>
		// Token: 0x170001FA RID: 506
		// (get) Token: 0x06000AE5 RID: 2789 RVA: 0x0002B4EC File Offset: 0x000296EC
		// (set) Token: 0x06000AE6 RID: 2790 RVA: 0x0002B4F4 File Offset: 0x000296F4
		[Nullable(2)]
		public JToken Next
		{
			[NullableContext(2)]
			get
			{
				return this._next;
			}
			[NullableContext(2)]
			internal set
			{
				this._next = value;
			}
		}

		/// <summary>
		/// Gets the previous sibling token of this node.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the previous sibling token.</value>
		// Token: 0x170001FB RID: 507
		// (get) Token: 0x06000AE7 RID: 2791 RVA: 0x0002B4FD File Offset: 0x000296FD
		// (set) Token: 0x06000AE8 RID: 2792 RVA: 0x0002B505 File Offset: 0x00029705
		[Nullable(2)]
		public JToken Previous
		{
			[NullableContext(2)]
			get
			{
				return this._previous;
			}
			[NullableContext(2)]
			internal set
			{
				this._previous = value;
			}
		}

		/// <summary>
		/// Gets the path of the JSON token. 
		/// </summary>
		// Token: 0x170001FC RID: 508
		// (get) Token: 0x06000AE9 RID: 2793 RVA: 0x0002B510 File Offset: 0x00029710
		public string Path
		{
			get
			{
				if (this.Parent == null)
				{
					return string.Empty;
				}
				List<JsonPosition> list = new List<JsonPosition>();
				JToken jtoken = null;
				for (JToken jtoken2 = this; jtoken2 != null; jtoken2 = jtoken2.Parent)
				{
					JTokenType type = jtoken2.Type;
					if (type - JTokenType.Array > 1)
					{
						if (type == JTokenType.Property)
						{
							JProperty jproperty = (JProperty)jtoken2;
							List<JsonPosition> list2 = list;
							JsonPosition item = new JsonPosition(JsonContainerType.Object)
							{
								PropertyName = jproperty.Name
							};
							list2.Add(item);
						}
					}
					else if (jtoken != null)
					{
						int position = ((IList<JToken>)jtoken2).IndexOf(jtoken);
						List<JsonPosition> list3 = list;
						JsonPosition item = new JsonPosition(JsonContainerType.Array)
						{
							Position = position
						};
						list3.Add(item);
					}
					jtoken = jtoken2;
				}
				list.FastReverse<JsonPosition>();
				return JsonPosition.BuildPath(list, null);
			}
		}

		// Token: 0x06000AEA RID: 2794 RVA: 0x0002B5BD File Offset: 0x000297BD
		internal JToken()
		{
		}

		/// <summary>
		/// Adds the specified content immediately after this token.
		/// </summary>
		/// <param name="content">A content object that contains simple content or a collection of content objects to be added after this token.</param>
		// Token: 0x06000AEB RID: 2795 RVA: 0x0002B5C8 File Offset: 0x000297C8
		[NullableContext(2)]
		public void AddAfterSelf(object content)
		{
			if (this._parent == null)
			{
				throw new InvalidOperationException("The parent is missing.");
			}
			int num = this._parent.IndexOfItem(this);
			this._parent.TryAddInternal(num + 1, content, false, true);
		}

		/// <summary>
		/// Adds the specified content immediately before this token.
		/// </summary>
		/// <param name="content">A content object that contains simple content or a collection of content objects to be added before this token.</param>
		// Token: 0x06000AEC RID: 2796 RVA: 0x0002B608 File Offset: 0x00029808
		[NullableContext(2)]
		public void AddBeforeSelf(object content)
		{
			if (this._parent == null)
			{
				throw new InvalidOperationException("The parent is missing.");
			}
			int index = this._parent.IndexOfItem(this);
			this._parent.TryAddInternal(index, content, false, true);
		}

		/// <summary>
		/// Returns a collection of the ancestor tokens of this token.
		/// </summary>
		/// <returns>A collection of the ancestor tokens of this token.</returns>
		// Token: 0x06000AED RID: 2797 RVA: 0x0002B645 File Offset: 0x00029845
		public IEnumerable<JToken> Ancestors()
		{
			return this.GetAncestors(false);
		}

		/// <summary>
		/// Returns a collection of tokens that contain this token, and the ancestors of this token.
		/// </summary>
		/// <returns>A collection of tokens that contain this token, and the ancestors of this token.</returns>
		// Token: 0x06000AEE RID: 2798 RVA: 0x0002B64E File Offset: 0x0002984E
		public IEnumerable<JToken> AncestorsAndSelf()
		{
			return this.GetAncestors(true);
		}

		// Token: 0x06000AEF RID: 2799 RVA: 0x0002B657 File Offset: 0x00029857
		internal IEnumerable<JToken> GetAncestors(bool self)
		{
			JToken current;
			for (current = (self ? this : this.Parent); current != null; current = current.Parent)
			{
				yield return current;
			}
			current = null;
			yield break;
		}

		/// <summary>
		/// Returns a collection of the sibling tokens after this token, in document order.
		/// </summary>
		/// <returns>A collection of the sibling tokens after this tokens, in document order.</returns>
		// Token: 0x06000AF0 RID: 2800 RVA: 0x0002B66E File Offset: 0x0002986E
		public IEnumerable<JToken> AfterSelf()
		{
			if (this.Parent == null)
			{
				yield break;
			}
			JToken o;
			for (o = this.Next; o != null; o = o.Next)
			{
				yield return o;
			}
			o = null;
			yield break;
		}

		/// <summary>
		/// Returns a collection of the sibling tokens before this token, in document order.
		/// </summary>
		/// <returns>A collection of the sibling tokens before this token, in document order.</returns>
		// Token: 0x06000AF1 RID: 2801 RVA: 0x0002B67E File Offset: 0x0002987E
		public IEnumerable<JToken> BeforeSelf()
		{
			if (this.Parent == null)
			{
				yield break;
			}
			JToken o = this.Parent.First;
			while (o != this && o != null)
			{
				yield return o;
				o = o.Next;
			}
			o = null;
			yield break;
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.</value>
		// Token: 0x170001FD RID: 509
		[Nullable(2)]
		public virtual JToken this[object key]
		{
			[return: Nullable(2)]
			get
			{
				throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
			}
			[param: Nullable(2)]
			set
			{
				throw new InvalidOperationException("Cannot set child value on {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
			}
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key converted to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to convert the token to.</typeparam>
		/// <param name="key">The token key.</param>
		/// <returns>The converted token value.</returns>
		// Token: 0x06000AF4 RID: 2804 RVA: 0x0002B6C8 File Offset: 0x000298C8
		[NullableContext(2)]
		public virtual T Value<T>([Nullable(1)] object key)
		{
			JToken jtoken = this[key];
			if (jtoken != null)
			{
				return jtoken.Convert<JToken, T>();
			}
			return default(T);
		}

		/// <summary>
		/// Get the first child token of this token.
		/// </summary>
		/// <value>A <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the first child token of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.</value>
		// Token: 0x170001FE RID: 510
		// (get) Token: 0x06000AF5 RID: 2805 RVA: 0x0002B6F0 File Offset: 0x000298F0
		[Nullable(2)]
		public virtual JToken First
		{
			[NullableContext(2)]
			get
			{
				throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
			}
		}

		/// <summary>
		/// Get the last child token of this token.
		/// </summary>
		/// <value>A <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the last child token of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.</value>
		// Token: 0x170001FF RID: 511
		// (get) Token: 0x06000AF6 RID: 2806 RVA: 0x0002B70C File Offset: 0x0002990C
		[Nullable(2)]
		public virtual JToken Last
		{
			[NullableContext(2)]
			get
			{
				throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
			}
		}

		/// <summary>
		/// Returns a collection of the child tokens of this token, in document order.
		/// </summary>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the child tokens of this <see cref="T:Newtonsoft.Json.Linq.JToken" />, in document order.</returns>
		// Token: 0x06000AF7 RID: 2807 RVA: 0x0002B728 File Offset: 0x00029928
		[return: Nullable(new byte[] { 0, 1 })]
		public virtual JEnumerable<JToken> Children()
		{
			return JEnumerable<JToken>.Empty;
		}

		/// <summary>
		/// Returns a collection of the child tokens of this token, in document order, filtered by the specified type.
		/// </summary>
		/// <typeparam name="T">The type to filter the child tokens on.</typeparam>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> containing the child tokens of this <see cref="T:Newtonsoft.Json.Linq.JToken" />, in document order.</returns>
		// Token: 0x06000AF8 RID: 2808 RVA: 0x0002B72F File Offset: 0x0002992F
		[NullableContext(0)]
		[return: Nullable(new byte[] { 0, 1 })]
		public JEnumerable<T> Children<T>() where T : JToken
		{
			return new JEnumerable<T>(this.Children().OfType<T>());
		}

		/// <summary>
		/// Returns a collection of the child values of this token, in document order.
		/// </summary>
		/// <typeparam name="T">The type to convert the values to.</typeparam>
		/// <returns>A <see cref="T:System.Collections.Generic.IEnumerable`1" /> containing the child values of this <see cref="T:Newtonsoft.Json.Linq.JToken" />, in document order.</returns>
		// Token: 0x06000AF9 RID: 2809 RVA: 0x0002B746 File Offset: 0x00029946
		[NullableContext(2)]
		[return: Nullable(new byte[] { 1, 2 })]
		public virtual IEnumerable<T> Values<T>()
		{
			throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
		}

		/// <summary>
		/// Removes this token from its parent.
		/// </summary>
		// Token: 0x06000AFA RID: 2810 RVA: 0x0002B762 File Offset: 0x00029962
		public void Remove()
		{
			if (this._parent == null)
			{
				throw new InvalidOperationException("The parent is missing.");
			}
			this._parent.RemoveItem(this);
		}

		/// <summary>
		/// Replaces this token with the specified token.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000AFB RID: 2811 RVA: 0x0002B784 File Offset: 0x00029984
		public void Replace(JToken value)
		{
			if (this._parent == null)
			{
				throw new InvalidOperationException("The parent is missing.");
			}
			this._parent.ReplaceItem(this, value);
		}

		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		// Token: 0x06000AFC RID: 2812
		public abstract void WriteTo(JsonWriter writer, params JsonConverter[] converters);

		/// <summary>
		/// Returns the indented JSON for this token.
		/// </summary>
		/// <remarks>
		/// <c>ToString()</c> returns a non-JSON string value for tokens with a type of <see cref="F:Newtonsoft.Json.Linq.JTokenType.String" />.
		/// If you want the JSON for all token types then you should use <see cref="M:Newtonsoft.Json.Linq.JToken.WriteTo(Newtonsoft.Json.JsonWriter,Newtonsoft.Json.JsonConverter[])" />.
		/// </remarks>
		/// <returns>
		/// The indented JSON for this token.
		/// </returns>
		// Token: 0x06000AFD RID: 2813 RVA: 0x0002B7A6 File Offset: 0x000299A6
		public override string ToString()
		{
			return this.ToString(Formatting.Indented, new JsonConverter[0]);
		}

		/// <summary>
		/// Returns the JSON for this token using the given formatting and converters.
		/// </summary>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" />s which will be used when writing the token.</param>
		/// <returns>The JSON for this token using the given formatting and converters.</returns>
		// Token: 0x06000AFE RID: 2814 RVA: 0x0002B7B8 File Offset: 0x000299B8
		public string ToString(Formatting formatting, params JsonConverter[] converters)
		{
			string result;
			using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				this.WriteTo(new JsonTextWriter(stringWriter)
				{
					Formatting = formatting
				}, converters);
				result = stringWriter.ToString();
			}
			return result;
		}

		// Token: 0x06000AFF RID: 2815 RVA: 0x0002B80C File Offset: 0x00029A0C
		[return: Nullable(2)]
		private static JValue EnsureValue(JToken value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			JProperty jproperty = value as JProperty;
			if (jproperty != null)
			{
				value = jproperty.Value;
			}
			return value as JValue;
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x0002B840 File Offset: 0x00029A40
		private static string GetType(JToken token)
		{
			ValidationUtils.ArgumentNotNull(token, "token");
			JProperty jproperty = token as JProperty;
			if (jproperty != null)
			{
				token = jproperty.Value;
			}
			return token.Type.ToString();
		}

		// Token: 0x06000B01 RID: 2817 RVA: 0x0002B87E File Offset: 0x00029A7E
		private static bool ValidateToken(JToken o, JTokenType[] validTypes, bool nullable)
		{
			return Array.IndexOf<JTokenType>(validTypes, o.Type) != -1 || (nullable && (o.Type == JTokenType.Null || o.Type == JTokenType.Undefined));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Boolean" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B02 RID: 2818 RVA: 0x0002B8AC File Offset: 0x00029AAC
		public static explicit operator bool(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.BooleanTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Boolean.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return Convert.ToBoolean((int)value3);
			}
			return Convert.ToBoolean(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.DateTimeOffset" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B03 RID: 2819 RVA: 0x0002B920 File Offset: 0x00029B20
		public static explicit operator DateTimeOffset(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.DateTimeTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to DateTimeOffset.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is DateTimeOffset)
			{
				return (DateTimeOffset)value2;
			}
			string text = jvalue.Value as string;
			if (text != null)
			{
				return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
			}
			return new DateTimeOffset(Convert.ToDateTime(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B04 RID: 2820 RVA: 0x0002B9A8 File Offset: 0x00029BA8
		[NullableContext(2)]
		public static explicit operator bool?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.BooleanTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Boolean.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new bool?(Convert.ToBoolean((int)value3));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new bool?(Convert.ToBoolean(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int64" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B05 RID: 2821 RVA: 0x0002BA44 File Offset: 0x00029C44
		public static explicit operator long(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Int64.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (long)value3;
			}
			return Convert.ToInt64(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B06 RID: 2822 RVA: 0x0002BAB4 File Offset: 0x00029CB4
		[NullableContext(2)]
		public static explicit operator DateTime?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.DateTimeTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to DateTime.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is DateTimeOffset)
			{
				return new DateTime?(((DateTimeOffset)value2).DateTime);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new DateTime?(Convert.ToDateTime(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B07 RID: 2823 RVA: 0x0002BB4C File Offset: 0x00029D4C
		[NullableContext(2)]
		public static explicit operator DateTimeOffset?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.DateTimeTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to DateTimeOffset.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			object value2 = jvalue.Value;
			if (value2 is DateTimeOffset)
			{
				DateTimeOffset value3 = (DateTimeOffset)value2;
				return new DateTimeOffset?(value3);
			}
			string text = jvalue.Value as string;
			if (text != null)
			{
				return new DateTimeOffset?(DateTimeOffset.Parse(text, CultureInfo.InvariantCulture));
			}
			return new DateTimeOffset?(new DateTimeOffset(Convert.ToDateTime(jvalue.Value, CultureInfo.InvariantCulture)));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B08 RID: 2824 RVA: 0x0002BC08 File Offset: 0x00029E08
		[NullableContext(2)]
		public static explicit operator decimal?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Decimal.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new decimal?((decimal)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new decimal?(Convert.ToDecimal(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B09 RID: 2825 RVA: 0x0002BCA0 File Offset: 0x00029EA0
		[NullableContext(2)]
		public static explicit operator double?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Double.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new double?((double)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new double?(Convert.ToDouble(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Char" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B0A RID: 2826 RVA: 0x0002BD38 File Offset: 0x00029F38
		[NullableContext(2)]
		public static explicit operator char?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.CharTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Char.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new char?((char)(ushort)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new char?(Convert.ToChar(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Int32" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B0B RID: 2827 RVA: 0x0002BDD0 File Offset: 0x00029FD0
		public static explicit operator int(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Int32.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (int)value3;
			}
			return Convert.ToInt32(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Int16" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B0C RID: 2828 RVA: 0x0002BE40 File Offset: 0x0002A040
		public static explicit operator short(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Int16.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (short)value3;
			}
			return Convert.ToInt16(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.UInt16" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B0D RID: 2829 RVA: 0x0002BEB0 File Offset: 0x0002A0B0
		[CLSCompliant(false)]
		public static explicit operator ushort(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to UInt16.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (ushort)value3;
			}
			return Convert.ToUInt16(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Char" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B0E RID: 2830 RVA: 0x0002BF20 File Offset: 0x0002A120
		[CLSCompliant(false)]
		public static explicit operator char(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.CharTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Char.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (char)(ushort)value3;
			}
			return Convert.ToChar(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Byte" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B0F RID: 2831 RVA: 0x0002BF90 File Offset: 0x0002A190
		public static explicit operator byte(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Byte.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (byte)value3;
			}
			return Convert.ToByte(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.SByte" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B10 RID: 2832 RVA: 0x0002C000 File Offset: 0x0002A200
		[CLSCompliant(false)]
		public static explicit operator sbyte(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to SByte.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (sbyte)value3;
			}
			return Convert.ToSByte(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" /> .
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B11 RID: 2833 RVA: 0x0002C070 File Offset: 0x0002A270
		[NullableContext(2)]
		public static explicit operator int?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Int32.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new int?((int)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new int?(Convert.ToInt32(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int16" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B12 RID: 2834 RVA: 0x0002C108 File Offset: 0x0002A308
		[NullableContext(2)]
		public static explicit operator short?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Int16.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new short?((short)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new short?(Convert.ToInt16(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt16" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B13 RID: 2835 RVA: 0x0002C1A0 File Offset: 0x0002A3A0
		[NullableContext(2)]
		[CLSCompliant(false)]
		public static explicit operator ushort?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to UInt16.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new ushort?((ushort)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new ushort?(Convert.ToUInt16(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Byte" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B14 RID: 2836 RVA: 0x0002C238 File Offset: 0x0002A438
		[NullableContext(2)]
		public static explicit operator byte?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Byte.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new byte?((byte)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new byte?(Convert.ToByte(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.SByte" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B15 RID: 2837 RVA: 0x0002C2D0 File Offset: 0x0002A4D0
		[NullableContext(2)]
		[CLSCompliant(false)]
		public static explicit operator sbyte?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to SByte.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new sbyte?((sbyte)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new sbyte?(Convert.ToSByte(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B16 RID: 2838 RVA: 0x0002C368 File Offset: 0x0002A568
		public static explicit operator DateTime(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.DateTimeTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to DateTime.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is DateTimeOffset)
			{
				return ((DateTimeOffset)value2).DateTime;
			}
			return Convert.ToDateTime(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int64" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B17 RID: 2839 RVA: 0x0002C3D8 File Offset: 0x0002A5D8
		[NullableContext(2)]
		public static explicit operator long?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Int64.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new long?((long)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new long?(Convert.ToInt64(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Single" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B18 RID: 2840 RVA: 0x0002C470 File Offset: 0x0002A670
		[NullableContext(2)]
		public static explicit operator float?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Single.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new float?((float)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new float?(Convert.ToSingle(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Decimal" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B19 RID: 2841 RVA: 0x0002C508 File Offset: 0x0002A708
		public static explicit operator decimal(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Decimal.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (decimal)value3;
			}
			return Convert.ToDecimal(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt32" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B1A RID: 2842 RVA: 0x0002C578 File Offset: 0x0002A778
		[NullableContext(2)]
		[CLSCompliant(false)]
		public static explicit operator uint?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to UInt32.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new uint?((uint)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new uint?(Convert.ToUInt32(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt64" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B1B RID: 2843 RVA: 0x0002C610 File Offset: 0x0002A810
		[NullableContext(2)]
		[CLSCompliant(false)]
		public static explicit operator ulong?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to UInt64.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return new ulong?((ulong)value3);
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new ulong?(Convert.ToUInt64(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Double" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B1C RID: 2844 RVA: 0x0002C6A8 File Offset: 0x0002A8A8
		public static explicit operator double(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Double.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (double)value3;
			}
			return Convert.ToDouble(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Single" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B1D RID: 2845 RVA: 0x0002C718 File Offset: 0x0002A918
		public static explicit operator float(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Single.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (float)value3;
			}
			return Convert.ToSingle(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.String" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B1E RID: 2846 RVA: 0x0002C788 File Offset: 0x0002A988
		[NullableContext(2)]
		public static explicit operator string(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.StringTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to String.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			byte[] array = jvalue.Value as byte[];
			if (array != null)
			{
				return Convert.ToBase64String(array);
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				return ((BigInteger)value2).ToString(CultureInfo.InvariantCulture);
			}
			return Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.UInt32" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B1F RID: 2847 RVA: 0x0002C820 File Offset: 0x0002AA20
		[CLSCompliant(false)]
		public static explicit operator uint(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to UInt32.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (uint)value3;
			}
			return Convert.ToUInt32(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.UInt64" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B20 RID: 2848 RVA: 0x0002C890 File Offset: 0x0002AA90
		[CLSCompliant(false)]
		public static explicit operator ulong(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.NumberTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to UInt64.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				BigInteger value3 = (BigInteger)value2;
				return (ulong)value3;
			}
			return Convert.ToUInt64(jvalue.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Byte" />[].
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B21 RID: 2849 RVA: 0x0002C900 File Offset: 0x0002AB00
		[NullableContext(2)]
		public static explicit operator byte[](JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.BytesTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to byte array.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value is string)
			{
				return Convert.FromBase64String(Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture));
			}
			object value2 = jvalue.Value;
			if (value2 is BigInteger)
			{
				return ((BigInteger)value2).ToByteArray();
			}
			byte[] array = jvalue.Value as byte[];
			if (array != null)
			{
				return array;
			}
			throw new ArgumentException("Can not convert {0} to byte array.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Guid" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B22 RID: 2850 RVA: 0x0002C9B4 File Offset: 0x0002ABB4
		public static explicit operator Guid(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.GuidTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to Guid.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			byte[] array = jvalue.Value as byte[];
			if (array != null)
			{
				return new Guid(array);
			}
			object value2 = jvalue.Value;
			if (value2 is Guid)
			{
				return (Guid)value2;
			}
			return new Guid(Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.Guid" /> .
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B23 RID: 2851 RVA: 0x0002CA3C File Offset: 0x0002AC3C
		[NullableContext(2)]
		public static explicit operator Guid?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.GuidTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Guid.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			byte[] array = jvalue.Value as byte[];
			if (array != null)
			{
				return new Guid?(new Guid(array));
			}
			object value2 = jvalue.Value;
			Guid value3;
			if (value2 is Guid)
			{
				Guid guid = (Guid)value2;
				value3 = guid;
			}
			else
			{
				value3 = new Guid(Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture));
			}
			return new Guid?(value3);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.TimeSpan" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B24 RID: 2852 RVA: 0x0002CAF0 File Offset: 0x0002ACF0
		public static explicit operator TimeSpan(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.TimeSpanTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to TimeSpan.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			object value2 = jvalue.Value;
			if (value2 is TimeSpan)
			{
				return (TimeSpan)value2;
			}
			return ConvertUtils.ParseTimeSpan(Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Nullable`1" /> of <see cref="T:System.TimeSpan" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B25 RID: 2853 RVA: 0x0002CB60 File Offset: 0x0002AD60
		[NullableContext(2)]
		public static explicit operator TimeSpan?(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.TimeSpanTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to TimeSpan.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			object value2 = jvalue.Value;
			TimeSpan value3;
			if (value2 is TimeSpan)
			{
				TimeSpan timeSpan = (TimeSpan)value2;
				value3 = timeSpan;
			}
			else
			{
				value3 = ConvertUtils.ParseTimeSpan(Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture));
			}
			return new TimeSpan?(value3);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="T:Newtonsoft.Json.Linq.JToken" /> to <see cref="T:System.Uri" />.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The result of the conversion.</returns>
		// Token: 0x06000B26 RID: 2854 RVA: 0x0002CBF4 File Offset: 0x0002ADF4
		[NullableContext(2)]
		public static explicit operator Uri(JToken value)
		{
			if (value == null)
			{
				return null;
			}
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.UriTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to Uri.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			Uri uri = jvalue.Value as Uri;
			if (uri == null)
			{
				return new Uri(Convert.ToString(jvalue.Value, CultureInfo.InvariantCulture));
			}
			return uri;
		}

		// Token: 0x06000B27 RID: 2855 RVA: 0x0002CC6C File Offset: 0x0002AE6C
		private static BigInteger ToBigInteger(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.BigIntegerTypes, false))
			{
				throw new ArgumentException("Can not convert {0} to BigInteger.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			return ConvertUtils.ToBigInteger(jvalue.Value);
		}

		// Token: 0x06000B28 RID: 2856 RVA: 0x0002CCB8 File Offset: 0x0002AEB8
		private static BigInteger? ToBigIntegerNullable(JToken value)
		{
			JValue jvalue = JToken.EnsureValue(value);
			if (jvalue == null || !JToken.ValidateToken(jvalue, JToken.BigIntegerTypes, true))
			{
				throw new ArgumentException("Can not convert {0} to BigInteger.".FormatWith(CultureInfo.InvariantCulture, JToken.GetType(value)));
			}
			if (jvalue.Value == null)
			{
				return null;
			}
			return new BigInteger?(ConvertUtils.ToBigInteger(jvalue.Value));
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Boolean" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B29 RID: 2857 RVA: 0x0002CD1A File Offset: 0x0002AF1A
		public static implicit operator JToken(bool value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.DateTimeOffset" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B2A RID: 2858 RVA: 0x0002CD22 File Offset: 0x0002AF22
		public static implicit operator JToken(DateTimeOffset value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Byte" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B2B RID: 2859 RVA: 0x0002CD2A File Offset: 0x0002AF2A
		public static implicit operator JToken(byte value)
		{
			return new JValue((long)((ulong)value));
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Byte" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B2C RID: 2860 RVA: 0x0002CD33 File Offset: 0x0002AF33
		public static implicit operator JToken(byte? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.SByte" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B2D RID: 2861 RVA: 0x0002CD40 File Offset: 0x0002AF40
		[CLSCompliant(false)]
		public static implicit operator JToken(sbyte value)
		{
			return new JValue((long)value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.SByte" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B2E RID: 2862 RVA: 0x0002CD49 File Offset: 0x0002AF49
		[CLSCompliant(false)]
		public static implicit operator JToken(sbyte? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B2F RID: 2863 RVA: 0x0002CD56 File Offset: 0x0002AF56
		public static implicit operator JToken(bool? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int64" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B30 RID: 2864 RVA: 0x0002CD63 File Offset: 0x0002AF63
		public static implicit operator JToken(long value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B31 RID: 2865 RVA: 0x0002CD6B File Offset: 0x0002AF6B
		public static implicit operator JToken(DateTime? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B32 RID: 2866 RVA: 0x0002CD78 File Offset: 0x0002AF78
		public static implicit operator JToken(DateTimeOffset? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B33 RID: 2867 RVA: 0x0002CD85 File Offset: 0x0002AF85
		public static implicit operator JToken(decimal? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B34 RID: 2868 RVA: 0x0002CD92 File Offset: 0x0002AF92
		public static implicit operator JToken(double? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Int16" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B35 RID: 2869 RVA: 0x0002CD9F File Offset: 0x0002AF9F
		[CLSCompliant(false)]
		public static implicit operator JToken(short value)
		{
			return new JValue((long)value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.UInt16" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B36 RID: 2870 RVA: 0x0002CDA8 File Offset: 0x0002AFA8
		[CLSCompliant(false)]
		public static implicit operator JToken(ushort value)
		{
			return new JValue((long)((ulong)value));
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Int32" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B37 RID: 2871 RVA: 0x0002CDB1 File Offset: 0x0002AFB1
		public static implicit operator JToken(int value)
		{
			return new JValue((long)value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B38 RID: 2872 RVA: 0x0002CDBA File Offset: 0x0002AFBA
		public static implicit operator JToken(int? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.DateTime" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B39 RID: 2873 RVA: 0x0002CDC7 File Offset: 0x0002AFC7
		public static implicit operator JToken(DateTime value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int64" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B3A RID: 2874 RVA: 0x0002CDCF File Offset: 0x0002AFCF
		public static implicit operator JToken(long? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Single" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B3B RID: 2875 RVA: 0x0002CDDC File Offset: 0x0002AFDC
		public static implicit operator JToken(float? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Decimal" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B3C RID: 2876 RVA: 0x0002CDE9 File Offset: 0x0002AFE9
		public static implicit operator JToken(decimal value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int16" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B3D RID: 2877 RVA: 0x0002CDF1 File Offset: 0x0002AFF1
		[CLSCompliant(false)]
		public static implicit operator JToken(short? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt16" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B3E RID: 2878 RVA: 0x0002CDFE File Offset: 0x0002AFFE
		[CLSCompliant(false)]
		public static implicit operator JToken(ushort? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt32" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B3F RID: 2879 RVA: 0x0002CE0B File Offset: 0x0002B00B
		[CLSCompliant(false)]
		public static implicit operator JToken(uint? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt64" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B40 RID: 2880 RVA: 0x0002CE18 File Offset: 0x0002B018
		[CLSCompliant(false)]
		public static implicit operator JToken(ulong? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Double" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B41 RID: 2881 RVA: 0x0002CE25 File Offset: 0x0002B025
		public static implicit operator JToken(double value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Single" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B42 RID: 2882 RVA: 0x0002CE2D File Offset: 0x0002B02D
		public static implicit operator JToken(float value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.String" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B43 RID: 2883 RVA: 0x0002CE35 File Offset: 0x0002B035
		public static implicit operator JToken([Nullable(2)] string value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.UInt32" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B44 RID: 2884 RVA: 0x0002CE3D File Offset: 0x0002B03D
		[CLSCompliant(false)]
		public static implicit operator JToken(uint value)
		{
			return new JValue((long)((ulong)value));
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.UInt64" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B45 RID: 2885 RVA: 0x0002CE46 File Offset: 0x0002B046
		[CLSCompliant(false)]
		public static implicit operator JToken(ulong value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Byte" />[] to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B46 RID: 2886 RVA: 0x0002CE4E File Offset: 0x0002B04E
		public static implicit operator JToken(byte[] value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Uri" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B47 RID: 2887 RVA: 0x0002CE56 File Offset: 0x0002B056
		public static implicit operator JToken([Nullable(2)] Uri value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.TimeSpan" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B48 RID: 2888 RVA: 0x0002CE5E File Offset: 0x0002B05E
		public static implicit operator JToken(TimeSpan value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.TimeSpan" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B49 RID: 2889 RVA: 0x0002CE66 File Offset: 0x0002B066
		public static implicit operator JToken(TimeSpan? value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Guid" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B4A RID: 2890 RVA: 0x0002CE73 File Offset: 0x0002B073
		public static implicit operator JToken(Guid value)
		{
			return new JValue(value);
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="T:System.Nullable`1" /> of <see cref="T:System.Guid" /> to <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="value">The value to create a <see cref="T:Newtonsoft.Json.Linq.JValue" /> from.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JValue" /> initialized with the specified value.</returns>
		// Token: 0x06000B4B RID: 2891 RVA: 0x0002CE7B File Offset: 0x0002B07B
		public static implicit operator JToken(Guid? value)
		{
			return new JValue(value);
		}

		// Token: 0x06000B4C RID: 2892 RVA: 0x0002CE88 File Offset: 0x0002B088
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<JToken>)this).GetEnumerator();
		}

		// Token: 0x06000B4D RID: 2893 RVA: 0x0002CE90 File Offset: 0x0002B090
		IEnumerator<JToken> IEnumerable<JToken>.GetEnumerator()
		{
			return this.Children().GetEnumerator();
		}

		// Token: 0x06000B4E RID: 2894
		internal abstract int GetDeepHashCode();

		// Token: 0x17000200 RID: 512
		IJEnumerable<JToken> IJEnumerable<JToken>.this[object key]
		{
			get
			{
				return this[key];
			}
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.JsonReader" /> for this token.
		/// </summary>
		/// <returns>A <see cref="T:Newtonsoft.Json.JsonReader" /> that can be used to read this token and its descendants.</returns>
		// Token: 0x06000B50 RID: 2896 RVA: 0x0002CEB4 File Offset: 0x0002B0B4
		public JsonReader CreateReader()
		{
			return new JTokenReader(this);
		}

		// Token: 0x06000B51 RID: 2897 RVA: 0x0002CEBC File Offset: 0x0002B0BC
		internal static JToken FromObjectInternal(object o, JsonSerializer jsonSerializer)
		{
			ValidationUtils.ArgumentNotNull(o, "o");
			ValidationUtils.ArgumentNotNull(jsonSerializer, "jsonSerializer");
			JToken token;
			using (JTokenWriter jtokenWriter = new JTokenWriter())
			{
				jsonSerializer.Serialize(jtokenWriter, o);
				token = jtokenWriter.Token;
			}
			return token;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from an object.
		/// </summary>
		/// <param name="o">The object that will be used to create <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the value of the specified object.</returns>
		// Token: 0x06000B52 RID: 2898 RVA: 0x0002CF14 File Offset: 0x0002B114
		public static JToken FromObject(object o)
		{
			return JToken.FromObjectInternal(o, JsonSerializer.CreateDefault());
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from an object using the specified <see cref="T:Newtonsoft.Json.JsonSerializer" />.
		/// </summary>
		/// <param name="o">The object that will be used to create <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="jsonSerializer">The <see cref="T:Newtonsoft.Json.JsonSerializer" /> that will be used when reading the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the value of the specified object.</returns>
		// Token: 0x06000B53 RID: 2899 RVA: 0x0002CF21 File Offset: 0x0002B121
		public static JToken FromObject(object o, JsonSerializer jsonSerializer)
		{
			return JToken.FromObjectInternal(o, jsonSerializer);
		}

		/// <summary>
		/// Creates an instance of the specified .NET type from the <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <typeparam name="T">The object type that the token will be deserialized to.</typeparam>
		/// <returns>The new object created from the JSON value.</returns>
		// Token: 0x06000B54 RID: 2900 RVA: 0x0002CF2A File Offset: 0x0002B12A
		[NullableContext(2)]
		public T ToObject<T>()
		{
			return (T)((object)this.ToObject(typeof(T)));
		}

		/// <summary>
		/// Creates an instance of the specified .NET type from the <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="objectType">The object type that the token will be deserialized to.</param>
		/// <returns>The new object created from the JSON value.</returns>
		// Token: 0x06000B55 RID: 2901 RVA: 0x0002CF44 File Offset: 0x0002B144
		[return: Nullable(2)]
		public object ToObject(Type objectType)
		{
			if (JsonConvert.DefaultSettings == null)
			{
				bool flag;
				PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(objectType, out flag);
				if (flag)
				{
					if (this.Type == JTokenType.String)
					{
						try
						{
							return this.ToObject(objectType, JsonSerializer.CreateDefault());
						}
						catch (Exception innerException)
						{
							Type type = (objectType.IsEnum() ? objectType : Nullable.GetUnderlyingType(objectType));
							throw new ArgumentException("Could not convert '{0}' to {1}.".FormatWith(CultureInfo.InvariantCulture, (string)this, type.Name), innerException);
						}
					}
					if (this.Type == JTokenType.Integer)
					{
						return Enum.ToObject(objectType.IsEnum() ? objectType : Nullable.GetUnderlyingType(objectType), ((JValue)this).Value);
					}
				}
				switch (typeCode)
				{
				case PrimitiveTypeCode.Char:
					return (char)this;
				case PrimitiveTypeCode.CharNullable:
					return (char?)this;
				case PrimitiveTypeCode.Boolean:
					return (bool)this;
				case PrimitiveTypeCode.BooleanNullable:
					return (bool?)this;
				case PrimitiveTypeCode.SByte:
					return (sbyte)this;
				case PrimitiveTypeCode.SByteNullable:
					return (sbyte?)this;
				case PrimitiveTypeCode.Int16:
					return (short)this;
				case PrimitiveTypeCode.Int16Nullable:
					return (short?)this;
				case PrimitiveTypeCode.UInt16:
					return (ushort)this;
				case PrimitiveTypeCode.UInt16Nullable:
					return (ushort?)this;
				case PrimitiveTypeCode.Int32:
					return (int)this;
				case PrimitiveTypeCode.Int32Nullable:
					return (int?)this;
				case PrimitiveTypeCode.Byte:
					return (byte)this;
				case PrimitiveTypeCode.ByteNullable:
					return (byte?)this;
				case PrimitiveTypeCode.UInt32:
					return (uint)this;
				case PrimitiveTypeCode.UInt32Nullable:
					return (uint?)this;
				case PrimitiveTypeCode.Int64:
					return (long)this;
				case PrimitiveTypeCode.Int64Nullable:
					return (long?)this;
				case PrimitiveTypeCode.UInt64:
					return (ulong)this;
				case PrimitiveTypeCode.UInt64Nullable:
					return (ulong?)this;
				case PrimitiveTypeCode.Single:
					return (float)this;
				case PrimitiveTypeCode.SingleNullable:
					return (float?)this;
				case PrimitiveTypeCode.Double:
					return (double)this;
				case PrimitiveTypeCode.DoubleNullable:
					return (double?)this;
				case PrimitiveTypeCode.DateTime:
					return (DateTime)this;
				case PrimitiveTypeCode.DateTimeNullable:
					return (DateTime?)this;
				case PrimitiveTypeCode.DateTimeOffset:
					return (DateTimeOffset)this;
				case PrimitiveTypeCode.DateTimeOffsetNullable:
					return (DateTimeOffset?)this;
				case PrimitiveTypeCode.Decimal:
					return (decimal)this;
				case PrimitiveTypeCode.DecimalNullable:
					return (decimal?)this;
				case PrimitiveTypeCode.Guid:
					return (Guid)this;
				case PrimitiveTypeCode.GuidNullable:
					return (Guid?)this;
				case PrimitiveTypeCode.TimeSpan:
					return (TimeSpan)this;
				case PrimitiveTypeCode.TimeSpanNullable:
					return (TimeSpan?)this;
				case PrimitiveTypeCode.BigInteger:
					return JToken.ToBigInteger(this);
				case PrimitiveTypeCode.BigIntegerNullable:
					return JToken.ToBigIntegerNullable(this);
				case PrimitiveTypeCode.Uri:
					return (Uri)this;
				case PrimitiveTypeCode.String:
					return (string)this;
				}
			}
			return this.ToObject(objectType, JsonSerializer.CreateDefault());
		}

		/// <summary>
		/// Creates an instance of the specified .NET type from the <see cref="T:Newtonsoft.Json.Linq.JToken" /> using the specified <see cref="T:Newtonsoft.Json.JsonSerializer" />.
		/// </summary>
		/// <typeparam name="T">The object type that the token will be deserialized to.</typeparam>
		/// <param name="jsonSerializer">The <see cref="T:Newtonsoft.Json.JsonSerializer" /> that will be used when creating the object.</param>
		/// <returns>The new object created from the JSON value.</returns>
		// Token: 0x06000B56 RID: 2902 RVA: 0x0002D268 File Offset: 0x0002B468
		[NullableContext(2)]
		public T ToObject<T>([Nullable(1)] JsonSerializer jsonSerializer)
		{
			return (T)((object)this.ToObject(typeof(T), jsonSerializer));
		}

		/// <summary>
		/// Creates an instance of the specified .NET type from the <see cref="T:Newtonsoft.Json.Linq.JToken" /> using the specified <see cref="T:Newtonsoft.Json.JsonSerializer" />.
		/// </summary>
		/// <param name="objectType">The object type that the token will be deserialized to.</param>
		/// <param name="jsonSerializer">The <see cref="T:Newtonsoft.Json.JsonSerializer" /> that will be used when creating the object.</param>
		/// <returns>The new object created from the JSON value.</returns>
		// Token: 0x06000B57 RID: 2903 RVA: 0x0002D280 File Offset: 0x0002B480
		[NullableContext(2)]
		public object ToObject(Type objectType, [Nullable(1)] JsonSerializer jsonSerializer)
		{
			ValidationUtils.ArgumentNotNull(jsonSerializer, "jsonSerializer");
			object result;
			using (JTokenReader jtokenReader = new JTokenReader(this))
			{
				JsonSerializerProxy jsonSerializerProxy = jsonSerializer as JsonSerializerProxy;
				if (jsonSerializerProxy != null)
				{
					CultureInfo cultureInfo;
					DateTimeZoneHandling? dateTimeZoneHandling;
					DateParseHandling? dateParseHandling;
					FloatParseHandling? floatParseHandling;
					int? num;
					string text;
					jsonSerializerProxy._serializer.SetupReader(jtokenReader, out cultureInfo, out dateTimeZoneHandling, out dateParseHandling, out floatParseHandling, out num, out text);
				}
				result = jsonSerializer.Deserialize(jtokenReader, objectType);
			}
			return result;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <returns>
		/// A <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000B58 RID: 2904 RVA: 0x0002D2EC File Offset: 0x0002B4EC
		public static JToken ReadFrom(JsonReader reader)
		{
			return JToken.ReadFrom(reader, null);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">An <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>
		/// A <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000B59 RID: 2905 RVA: 0x0002D2F8 File Offset: 0x0002B4F8
		public static JToken ReadFrom(JsonReader reader, [Nullable(2)] JsonLoadSettings settings)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			bool flag;
			if (reader.TokenType == JsonToken.None)
			{
				flag = ((settings != null && settings.CommentHandling == CommentHandling.Ignore) ? reader.ReadAndMoveToContent() : reader.Read());
			}
			else
			{
				flag = reader.TokenType != JsonToken.Comment || settings == null || settings.CommentHandling != CommentHandling.Ignore || reader.ReadAndMoveToContent();
			}
			if (!flag)
			{
				throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader.");
			}
			IJsonLineInfo lineInfo = reader as IJsonLineInfo;
			switch (reader.TokenType)
			{
			case JsonToken.StartObject:
				return JObject.Load(reader, settings);
			case JsonToken.StartArray:
				return JArray.Load(reader, settings);
			case JsonToken.StartConstructor:
				return JConstructor.Load(reader, settings);
			case JsonToken.PropertyName:
				return JProperty.Load(reader, settings);
			case JsonToken.Comment:
			{
				JValue jvalue = JValue.CreateComment(reader.Value.ToString());
				jvalue.SetLineInfo(lineInfo, settings);
				return jvalue;
			}
			case JsonToken.Integer:
			case JsonToken.Float:
			case JsonToken.String:
			case JsonToken.Boolean:
			case JsonToken.Date:
			case JsonToken.Bytes:
			{
				JValue jvalue2 = new JValue(reader.Value);
				jvalue2.SetLineInfo(lineInfo, settings);
				return jvalue2;
			}
			case JsonToken.Null:
			{
				JValue jvalue3 = JValue.CreateNull();
				jvalue3.SetLineInfo(lineInfo, settings);
				return jvalue3;
			}
			case JsonToken.Undefined:
			{
				JValue jvalue4 = JValue.CreateUndefined();
				jvalue4.SetLineInfo(lineInfo, settings);
				return jvalue4;
			}
			}
			throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader. Unexpected token: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a string that contains JSON.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" /> populated from the string that contains JSON.</returns>
		// Token: 0x06000B5A RID: 2906 RVA: 0x0002D447 File Offset: 0x0002B647
		public static JToken Parse(string json)
		{
			return JToken.Parse(json, null);
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a string that contains JSON.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" /> populated from the string that contains JSON.</returns>
		// Token: 0x06000B5B RID: 2907 RVA: 0x0002D450 File Offset: 0x0002B650
		public static JToken Parse(string json, [Nullable(2)] JsonLoadSettings settings)
		{
			JToken result;
			using (JsonReader jsonReader = new JsonTextReader(new StringReader(json)))
			{
				JToken jtoken = JToken.Load(jsonReader, settings);
				while (jsonReader.Read())
				{
				}
				result = jtoken;
			}
			return result;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>
		/// A <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000B5C RID: 2908 RVA: 0x0002D498 File Offset: 0x0002B698
		public static JToken Load(JsonReader reader, [Nullable(2)] JsonLoadSettings settings)
		{
			return JToken.ReadFrom(reader, settings);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JToken" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> positioned at the token to read into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</param>
		/// <returns>
		/// A <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the token and its descendant tokens
		/// that were read from the reader. The runtime type of the token is determined
		/// by the token type of the first token encountered in the reader.
		/// </returns>
		// Token: 0x06000B5D RID: 2909 RVA: 0x0002D4A1 File Offset: 0x0002B6A1
		public static JToken Load(JsonReader reader)
		{
			return JToken.Load(reader, null);
		}

		// Token: 0x06000B5E RID: 2910 RVA: 0x0002D4AA File Offset: 0x0002B6AA
		[NullableContext(2)]
		internal void SetLineInfo(IJsonLineInfo lineInfo, JsonLoadSettings settings)
		{
			if (settings != null && settings.LineInfoHandling != LineInfoHandling.Load)
			{
				return;
			}
			if (lineInfo == null || !lineInfo.HasLineInfo())
			{
				return;
			}
			this.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
		}

		// Token: 0x06000B5F RID: 2911 RVA: 0x0002D4D7 File Offset: 0x0002B6D7
		internal void SetLineInfo(int lineNumber, int linePosition)
		{
			this.AddAnnotation(new JToken.LineInfoAnnotation(lineNumber, linePosition));
		}

		// Token: 0x06000B60 RID: 2912 RVA: 0x0002D4E6 File Offset: 0x0002B6E6
		bool IJsonLineInfo.HasLineInfo()
		{
			return this.Annotation<JToken.LineInfoAnnotation>() != null;
		}

		// Token: 0x17000201 RID: 513
		// (get) Token: 0x06000B61 RID: 2913 RVA: 0x0002D4F4 File Offset: 0x0002B6F4
		int IJsonLineInfo.LineNumber
		{
			get
			{
				JToken.LineInfoAnnotation lineInfoAnnotation = this.Annotation<JToken.LineInfoAnnotation>();
				if (lineInfoAnnotation != null)
				{
					return lineInfoAnnotation.LineNumber;
				}
				return 0;
			}
		}

		// Token: 0x17000202 RID: 514
		// (get) Token: 0x06000B62 RID: 2914 RVA: 0x0002D514 File Offset: 0x0002B714
		int IJsonLineInfo.LinePosition
		{
			get
			{
				JToken.LineInfoAnnotation lineInfoAnnotation = this.Annotation<JToken.LineInfoAnnotation>();
				if (lineInfoAnnotation != null)
				{
					return lineInfoAnnotation.LinePosition;
				}
				return 0;
			}
		}

		/// <summary>
		/// Selects a <see cref="T:Newtonsoft.Json.Linq.JToken" /> using a JSONPath expression. Selects the token that matches the object path.
		/// </summary>
		/// <param name="path">
		/// A <see cref="T:System.String" /> that contains a JSONPath expression.
		/// </param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" />, or <c>null</c>.</returns>
		// Token: 0x06000B63 RID: 2915 RVA: 0x0002D533 File Offset: 0x0002B733
		[return: Nullable(2)]
		public JToken SelectToken(string path)
		{
			return this.SelectToken(path, null);
		}

		/// <summary>
		/// Selects a <see cref="T:Newtonsoft.Json.Linq.JToken" /> using a JSONPath expression. Selects the token that matches the object path.
		/// </summary>
		/// <param name="path">
		/// A <see cref="T:System.String" /> that contains a JSONPath expression.
		/// </param>
		/// <param name="errorWhenNoMatch">A flag to indicate whether an error should be thrown if no tokens are found when evaluating part of the expression.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x06000B64 RID: 2916 RVA: 0x0002D540 File Offset: 0x0002B740
		[return: Nullable(2)]
		public JToken SelectToken(string path, bool errorWhenNoMatch)
		{
			object obj;
			if (!errorWhenNoMatch)
			{
				obj = null;
			}
			else
			{
				(obj = new JsonSelectSettings()).ErrorWhenNoMatch = true;
			}
			JsonSelectSettings settings = obj;
			return this.SelectToken(path, settings);
		}

		/// <summary>
		/// Selects a <see cref="T:Newtonsoft.Json.Linq.JToken" /> using a JSONPath expression. Selects the token that matches the object path.
		/// </summary>
		/// <param name="path">
		/// A <see cref="T:System.String" /> that contains a JSONPath expression.
		/// </param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonSelectSettings" /> used to select tokens.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x06000B65 RID: 2917 RVA: 0x0002D568 File Offset: 0x0002B768
		[NullableContext(2)]
		public JToken SelectToken([Nullable(1)] string path, JsonSelectSettings settings)
		{
			JPath jpath = new JPath(path);
			JToken jtoken = null;
			foreach (JToken jtoken2 in jpath.Evaluate(this, this, settings))
			{
				if (jtoken != null)
				{
					throw new JsonException("Path returned multiple tokens.");
				}
				jtoken = jtoken2;
			}
			return jtoken;
		}

		/// <summary>
		/// Selects a collection of elements using a JSONPath expression.
		/// </summary>
		/// <param name="path">
		/// A <see cref="T:System.String" /> that contains a JSONPath expression.
		/// </param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the selected elements.</returns>
		// Token: 0x06000B66 RID: 2918 RVA: 0x0002D5C8 File Offset: 0x0002B7C8
		public IEnumerable<JToken> SelectTokens(string path)
		{
			return this.SelectTokens(path, null);
		}

		/// <summary>
		/// Selects a collection of elements using a JSONPath expression.
		/// </summary>
		/// <param name="path">
		/// A <see cref="T:System.String" /> that contains a JSONPath expression.
		/// </param>
		/// <param name="errorWhenNoMatch">A flag to indicate whether an error should be thrown if no tokens are found when evaluating part of the expression.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the selected elements.</returns>
		// Token: 0x06000B67 RID: 2919 RVA: 0x0002D5D4 File Offset: 0x0002B7D4
		public IEnumerable<JToken> SelectTokens(string path, bool errorWhenNoMatch)
		{
			object obj;
			if (!errorWhenNoMatch)
			{
				obj = null;
			}
			else
			{
				(obj = new JsonSelectSettings()).ErrorWhenNoMatch = true;
			}
			JsonSelectSettings settings = obj;
			return this.SelectTokens(path, settings);
		}

		/// <summary>
		/// Selects a collection of elements using a JSONPath expression.
		/// </summary>
		/// <param name="path">
		/// A <see cref="T:System.String" /> that contains a JSONPath expression.
		/// </param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonSelectSettings" /> used to select tokens.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that contains the selected elements.</returns>
		// Token: 0x06000B68 RID: 2920 RVA: 0x0002D5FC File Offset: 0x0002B7FC
		public IEnumerable<JToken> SelectTokens(string path, [Nullable(2)] JsonSelectSettings settings)
		{
			return new JPath(path).Evaluate(this, this, settings);
		}

		/// <summary>
		/// Returns the <see cref="T:System.Dynamic.DynamicMetaObject" /> responsible for binding operations performed on this object.
		/// </summary>
		/// <param name="parameter">The expression tree representation of the runtime value.</param>
		/// <returns>
		/// The <see cref="T:System.Dynamic.DynamicMetaObject" /> to bind this object.
		/// </returns>
		// Token: 0x06000B69 RID: 2921 RVA: 0x0002D60C File Offset: 0x0002B80C
		protected virtual DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return new DynamicProxyMetaObject<JToken>(parameter, this, new DynamicProxy<JToken>());
		}

		/// <summary>
		/// Returns the <see cref="T:System.Dynamic.DynamicMetaObject" /> responsible for binding operations performed on this object.
		/// </summary>
		/// <param name="parameter">The expression tree representation of the runtime value.</param>
		/// <returns>
		/// The <see cref="T:System.Dynamic.DynamicMetaObject" /> to bind this object.
		/// </returns>
		// Token: 0x06000B6A RID: 2922 RVA: 0x0002D61A File Offset: 0x0002B81A
		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return this.GetMetaObject(parameter);
		}

		// Token: 0x06000B6B RID: 2923 RVA: 0x0002D623 File Offset: 0x0002B823
		object ICloneable.Clone()
		{
			return this.DeepClone();
		}

		/// <summary>
		/// Creates a new instance of the <see cref="T:Newtonsoft.Json.Linq.JToken" />. All child tokens are recursively cloned.
		/// </summary>
		/// <returns>A new instance of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x06000B6C RID: 2924 RVA: 0x0002D62B File Offset: 0x0002B82B
		public JToken DeepClone()
		{
			return this.CloneToken(null);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="T:Newtonsoft.Json.Linq.JToken" />. All child tokens are recursively cloned.
		/// </summary>
		/// <param name="settings">A <see cref="T:Newtonsoft.Json.Linq.JsonCloneSettings" /> object to configure cloning settings.</param>
		/// <returns>A new instance of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x06000B6D RID: 2925 RVA: 0x0002D634 File Offset: 0x0002B834
		public JToken DeepClone(JsonCloneSettings settings)
		{
			return this.CloneToken(settings);
		}

		/// <summary>
		/// Adds an object to the annotation list of this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="annotation">The annotation to add.</param>
		// Token: 0x06000B6E RID: 2926 RVA: 0x0002D640 File Offset: 0x0002B840
		public void AddAnnotation(object annotation)
		{
			if (annotation == null)
			{
				throw new ArgumentNullException("annotation");
			}
			if (this._annotations == null)
			{
				object annotations;
				if (!(annotation is object[]))
				{
					annotations = annotation;
				}
				else
				{
					(annotations = new object[1])[0] = annotation;
				}
				this._annotations = annotations;
				return;
			}
			object[] array = this._annotations as object[];
			if (array == null)
			{
				this._annotations = new object[] { this._annotations, annotation };
				return;
			}
			int num = 0;
			while (num < array.Length && array[num] != null)
			{
				num++;
			}
			if (num == array.Length)
			{
				Array.Resize<object>(ref array, num * 2);
				this._annotations = array;
			}
			array[num] = annotation;
		}

		/// <summary>
		/// Get the first annotation object of the specified type from this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <typeparam name="T">The type of the annotation to retrieve.</typeparam>
		/// <returns>The first annotation object that matches the specified type, or <c>null</c> if no annotation is of the specified type.</returns>
		// Token: 0x06000B6F RID: 2927 RVA: 0x0002D6D8 File Offset: 0x0002B8D8
		[return: Nullable(2)]
		public T Annotation<T>() where T : class
		{
			if (this._annotations != null)
			{
				object[] array = this._annotations as object[];
				if (array == null)
				{
					return this._annotations as T;
				}
				foreach (object obj in array)
				{
					if (obj == null)
					{
						break;
					}
					T t = obj as T;
					if (t != null)
					{
						return t;
					}
				}
			}
			return default(T);
		}

		/// <summary>
		/// Gets the first annotation object of the specified type from this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="type">The <see cref="P:Newtonsoft.Json.Linq.JToken.Type" /> of the annotation to retrieve.</param>
		/// <returns>The first annotation object that matches the specified type, or <c>null</c> if no annotation is of the specified type.</returns>
		// Token: 0x06000B70 RID: 2928 RVA: 0x0002D744 File Offset: 0x0002B944
		[return: Nullable(2)]
		public object Annotation(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (this._annotations != null)
			{
				object[] array = this._annotations as object[];
				if (array == null)
				{
					if (type.IsInstanceOfType(this._annotations))
					{
						return this._annotations;
					}
				}
				else
				{
					foreach (object obj in array)
					{
						if (obj == null)
						{
							break;
						}
						if (type.IsInstanceOfType(obj))
						{
							return obj;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Gets a collection of annotations of the specified type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <typeparam name="T">The type of the annotations to retrieve.</typeparam>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the annotations for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x06000B71 RID: 2929 RVA: 0x0002D7B2 File Offset: 0x0002B9B2
		public IEnumerable<T> Annotations<T>() where T : class
		{
			if (this._annotations == null)
			{
				yield break;
			}
			object annotations2 = this._annotations;
			object[] annotations = annotations2 as object[];
			if (annotations != null)
			{
				int num;
				for (int i = 0; i < annotations.Length; i = num + 1)
				{
					object obj = annotations[i];
					if (obj == null)
					{
						break;
					}
					T t = obj as T;
					if (t != null)
					{
						yield return t;
					}
					num = i;
				}
				yield break;
			}
			T t2 = this._annotations as T;
			if (t2 == null)
			{
				yield break;
			}
			yield return t2;
			yield break;
		}

		/// <summary>
		/// Gets a collection of annotations of the specified type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="type">The <see cref="P:Newtonsoft.Json.Linq.JToken.Type" /> of the annotations to retrieve.</param>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:System.Object" /> that contains the annotations that match the specified type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x06000B72 RID: 2930 RVA: 0x0002D7C2 File Offset: 0x0002B9C2
		public IEnumerable<object> Annotations(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (this._annotations == null)
			{
				yield break;
			}
			object annotations2 = this._annotations;
			object[] annotations = annotations2 as object[];
			if (annotations != null)
			{
				int num;
				for (int i = 0; i < annotations.Length; i = num + 1)
				{
					object obj = annotations[i];
					if (obj == null)
					{
						break;
					}
					if (type.IsInstanceOfType(obj))
					{
						yield return obj;
					}
					num = i;
				}
				yield break;
			}
			if (!type.IsInstanceOfType(this._annotations))
			{
				yield break;
			}
			yield return this._annotations;
			yield break;
		}

		/// <summary>
		/// Removes the annotations of the specified type from this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <typeparam name="T">The type of annotations to remove.</typeparam>
		// Token: 0x06000B73 RID: 2931 RVA: 0x0002D7DC File Offset: 0x0002B9DC
		public void RemoveAnnotations<T>() where T : class
		{
			if (this._annotations != null)
			{
				object[] array = this._annotations as object[];
				if (array == null)
				{
					if (this._annotations is T)
					{
						this._annotations = null;
						return;
					}
				}
				else
				{
					int i = 0;
					int j = 0;
					while (i < array.Length)
					{
						object obj = array[i];
						if (obj == null)
						{
							break;
						}
						if (!(obj is T))
						{
							array[j++] = obj;
						}
						i++;
					}
					if (j != 0)
					{
						while (j < i)
						{
							array[j++] = null;
						}
						return;
					}
					this._annotations = null;
				}
			}
		}

		/// <summary>
		/// Removes the annotations of the specified type from this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="type">The <see cref="P:Newtonsoft.Json.Linq.JToken.Type" /> of annotations to remove.</param>
		// Token: 0x06000B74 RID: 2932 RVA: 0x0002D858 File Offset: 0x0002BA58
		public void RemoveAnnotations(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (this._annotations != null)
			{
				object[] array = this._annotations as object[];
				if (array == null)
				{
					if (type.IsInstanceOfType(this._annotations))
					{
						this._annotations = null;
						return;
					}
				}
				else
				{
					int i = 0;
					int j = 0;
					while (i < array.Length)
					{
						object obj = array[i];
						if (obj == null)
						{
							break;
						}
						if (!type.IsInstanceOfType(obj))
						{
							array[j++] = obj;
						}
						i++;
					}
					if (j != 0)
					{
						while (j < i)
						{
							array[j++] = null;
						}
						return;
					}
					this._annotations = null;
				}
			}
		}

		// Token: 0x06000B75 RID: 2933 RVA: 0x0002D8E8 File Offset: 0x0002BAE8
		internal void CopyAnnotations(JToken target, JToken source)
		{
			object[] array = source._annotations as object[];
			if (array != null)
			{
				target._annotations = array.ToArray<object>();
				return;
			}
			target._annotations = source._annotations;
		}

		// Token: 0x04000394 RID: 916
		[Nullable(2)]
		private static JTokenEqualityComparer _equalityComparer;

		// Token: 0x04000395 RID: 917
		[Nullable(2)]
		private JContainer _parent;

		// Token: 0x04000396 RID: 918
		[Nullable(2)]
		private JToken _previous;

		// Token: 0x04000397 RID: 919
		[Nullable(2)]
		private JToken _next;

		// Token: 0x04000398 RID: 920
		[Nullable(2)]
		private object _annotations;

		// Token: 0x04000399 RID: 921
		private static readonly JTokenType[] BooleanTypes = new JTokenType[]
		{
			JTokenType.Integer,
			JTokenType.Float,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Boolean
		};

		// Token: 0x0400039A RID: 922
		private static readonly JTokenType[] NumberTypes = new JTokenType[]
		{
			JTokenType.Integer,
			JTokenType.Float,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Boolean
		};

		// Token: 0x0400039B RID: 923
		private static readonly JTokenType[] BigIntegerTypes = new JTokenType[]
		{
			JTokenType.Integer,
			JTokenType.Float,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Boolean,
			JTokenType.Bytes
		};

		// Token: 0x0400039C RID: 924
		private static readonly JTokenType[] StringTypes = new JTokenType[]
		{
			JTokenType.Date,
			JTokenType.Integer,
			JTokenType.Float,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Boolean,
			JTokenType.Bytes,
			JTokenType.Guid,
			JTokenType.TimeSpan,
			JTokenType.Uri
		};

		// Token: 0x0400039D RID: 925
		private static readonly JTokenType[] GuidTypes = new JTokenType[]
		{
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Guid,
			JTokenType.Bytes
		};

		// Token: 0x0400039E RID: 926
		private static readonly JTokenType[] TimeSpanTypes = new JTokenType[]
		{
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.TimeSpan
		};

		// Token: 0x0400039F RID: 927
		private static readonly JTokenType[] UriTypes = new JTokenType[]
		{
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Uri
		};

		// Token: 0x040003A0 RID: 928
		private static readonly JTokenType[] CharTypes = new JTokenType[]
		{
			JTokenType.Integer,
			JTokenType.Float,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw
		};

		// Token: 0x040003A1 RID: 929
		private static readonly JTokenType[] DateTimeTypes = new JTokenType[]
		{
			JTokenType.Date,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw
		};

		// Token: 0x040003A2 RID: 930
		private static readonly JTokenType[] BytesTypes = new JTokenType[]
		{
			JTokenType.Bytes,
			JTokenType.String,
			JTokenType.Comment,
			JTokenType.Raw,
			JTokenType.Integer
		};

		// Token: 0x020001CF RID: 463
		[NullableContext(0)]
		private class LineInfoAnnotation
		{
			// Token: 0x06000FCF RID: 4047 RVA: 0x00045C42 File Offset: 0x00043E42
			public LineInfoAnnotation(int lineNumber, int linePosition)
			{
				this.LineNumber = lineNumber;
				this.LinePosition = linePosition;
			}

			// Token: 0x040007E2 RID: 2018
			internal readonly int LineNumber;

			// Token: 0x040007E3 RID: 2019
			internal readonly int LinePosition;
		}
	}
}
