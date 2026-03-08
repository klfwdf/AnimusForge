using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a JSON array.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParseArray" title="Parsing a JSON Array from Text" />
	/// </example>
	// Token: 0x020000BA RID: 186
	[NullableContext(1)]
	[Nullable(0)]
	public class JArray : JContainer, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable
	{
		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" /> asynchronously.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous write operation.</returns>
		// Token: 0x0600098D RID: 2445 RVA: 0x000283C0 File Offset: 0x000265C0
		public override async Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
		{
			await writer.WriteStartArrayAsync(cancellationToken).ConfigureAwait(false);
			for (int i = 0; i < this._values.Count; i++)
			{
				await this._values[i].WriteToAsync(writer, cancellationToken, converters).ConfigureAwait(false);
			}
			await writer.WriteEndArrayAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously loads a <see cref="T:Newtonsoft.Json.Linq.JArray" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />. 
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> representing the asynchronous load. The <see cref="P:System.Threading.Tasks.Task`1.Result" /> property contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x0600098E RID: 2446 RVA: 0x0002841B File Offset: 0x0002661B
		public new static Task<JArray> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			return JArray.LoadAsync(reader, null, cancellationToken);
		}

		/// <summary>
		/// Asynchronously loads a <see cref="T:Newtonsoft.Json.Linq.JArray" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />. 
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> representing the asynchronous load. The <see cref="P:System.Threading.Tasks.Task`1.Result" /> property contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x0600098F RID: 2447 RVA: 0x00028428 File Offset: 0x00026628
		public new static Task<JArray> LoadAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
		{
			JArray.<LoadAsync>d__2 <LoadAsync>d__;
			<LoadAsync>d__.<>t__builder = AsyncTaskMethodBuilder<JArray>.Create();
			<LoadAsync>d__.reader = reader;
			<LoadAsync>d__.settings = settings;
			<LoadAsync>d__.cancellationToken = cancellationToken;
			<LoadAsync>d__.<>1__state = -1;
			<LoadAsync>d__.<>t__builder.Start<JArray.<LoadAsync>d__2>(ref <LoadAsync>d__);
			return <LoadAsync>d__.<>t__builder.Task;
		}

		/// <summary>
		/// Gets the container's children tokens.
		/// </summary>
		/// <value>The container's children tokens.</value>
		// Token: 0x170001BB RID: 443
		// (get) Token: 0x06000990 RID: 2448 RVA: 0x0002847B File Offset: 0x0002667B
		protected override IList<JToken> ChildrenTokens
		{
			get
			{
				return this._values;
			}
		}

		/// <summary>
		/// Gets the node type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <value>The type.</value>
		// Token: 0x170001BC RID: 444
		// (get) Token: 0x06000991 RID: 2449 RVA: 0x00028483 File Offset: 0x00026683
		public override JTokenType Type
		{
			get
			{
				return JTokenType.Array;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JArray" /> class.
		/// </summary>
		// Token: 0x06000992 RID: 2450 RVA: 0x00028486 File Offset: 0x00026686
		public JArray()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JArray" /> class from another <see cref="T:Newtonsoft.Json.Linq.JArray" /> object.
		/// </summary>
		/// <param name="other">A <see cref="T:Newtonsoft.Json.Linq.JArray" /> object to copy from.</param>
		// Token: 0x06000993 RID: 2451 RVA: 0x00028499 File Offset: 0x00026699
		public JArray(JArray other)
			: base(other, null)
		{
		}

		// Token: 0x06000994 RID: 2452 RVA: 0x000284AE File Offset: 0x000266AE
		internal JArray(JArray other, [Nullable(2)] JsonCloneSettings settings)
			: base(other, settings)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JArray" /> class with the specified content.
		/// </summary>
		/// <param name="content">The contents of the array.</param>
		// Token: 0x06000995 RID: 2453 RVA: 0x000284C3 File Offset: 0x000266C3
		public JArray(params object[] content)
			: this(content)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JArray" /> class with the specified content.
		/// </summary>
		/// <param name="content">The contents of the array.</param>
		// Token: 0x06000996 RID: 2454 RVA: 0x000284CC File Offset: 0x000266CC
		public JArray(object content)
		{
			this.Add(content);
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x000284E8 File Offset: 0x000266E8
		internal override bool DeepEquals(JToken node)
		{
			JArray jarray = node as JArray;
			return jarray != null && base.ContentsEqual(jarray);
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x00028508 File Offset: 0x00026708
		internal override JToken CloneToken([Nullable(2)] JsonCloneSettings settings = null)
		{
			return new JArray(this, settings);
		}

		/// <summary>
		/// Loads an <see cref="T:Newtonsoft.Json.Linq.JArray" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />. 
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JArray" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x06000999 RID: 2457 RVA: 0x00028511 File Offset: 0x00026711
		public new static JArray Load(JsonReader reader)
		{
			return JArray.Load(reader, null);
		}

		/// <summary>
		/// Loads an <see cref="T:Newtonsoft.Json.Linq.JArray" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />. 
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JArray" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x0600099A RID: 2458 RVA: 0x0002851C File Offset: 0x0002671C
		public new static JArray Load(JsonReader reader, [Nullable(2)] JsonLoadSettings settings)
		{
			if (reader.TokenType == JsonToken.None && !reader.Read())
			{
				throw JsonReaderException.Create(reader, "Error reading JArray from JsonReader.");
			}
			reader.MoveToContent();
			if (reader.TokenType != JsonToken.StartArray)
			{
				throw JsonReaderException.Create(reader, "Error reading JArray from JsonReader. Current JsonReader item is not an array: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
			}
			JArray jarray = new JArray();
			jarray.SetLineInfo(reader as IJsonLineInfo, settings);
			jarray.ReadTokenFrom(reader, settings);
			return jarray;
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Linq.JArray" /> from a string that contains JSON.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JArray" /> populated from the string that contains JSON.</returns>
		/// <example>
		///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParseArray" title="Parsing a JSON Array from Text" />
		/// </example>
		// Token: 0x0600099B RID: 2459 RVA: 0x00028590 File Offset: 0x00026790
		public new static JArray Parse(string json)
		{
			return JArray.Parse(json, null);
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Linq.JArray" /> from a string that contains JSON.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JArray" /> populated from the string that contains JSON.</returns>
		/// <example>
		///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParseArray" title="Parsing a JSON Array from Text" />
		/// </example>
		// Token: 0x0600099C RID: 2460 RVA: 0x0002859C File Offset: 0x0002679C
		public new static JArray Parse(string json, [Nullable(2)] JsonLoadSettings settings)
		{
			JArray result;
			using (JsonReader jsonReader = new JsonTextReader(new StringReader(json)))
			{
				JArray jarray = JArray.Load(jsonReader, settings);
				while (jsonReader.Read())
				{
				}
				result = jarray;
			}
			return result;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JArray" /> from an object.
		/// </summary>
		/// <param name="o">The object that will be used to create <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JArray" /> with the values of the specified object.</returns>
		// Token: 0x0600099D RID: 2461 RVA: 0x000285E4 File Offset: 0x000267E4
		public new static JArray FromObject(object o)
		{
			return JArray.FromObject(o, JsonSerializer.CreateDefault());
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JArray" /> from an object.
		/// </summary>
		/// <param name="o">The object that will be used to create <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <param name="jsonSerializer">The <see cref="T:Newtonsoft.Json.JsonSerializer" /> that will be used to read the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JArray" /> with the values of the specified object.</returns>
		// Token: 0x0600099E RID: 2462 RVA: 0x000285F4 File Offset: 0x000267F4
		public new static JArray FromObject(object o, JsonSerializer jsonSerializer)
		{
			JToken jtoken = JToken.FromObjectInternal(o, jsonSerializer);
			if (jtoken.Type != JTokenType.Array)
			{
				throw new ArgumentException("Object serialized to {0}. JArray instance expected.".FormatWith(CultureInfo.InvariantCulture, jtoken.Type));
			}
			return (JArray)jtoken;
		}

		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		// Token: 0x0600099F RID: 2463 RVA: 0x00028638 File Offset: 0x00026838
		public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
		{
			writer.WriteStartArray();
			for (int i = 0; i < this._values.Count; i++)
			{
				this._values[i].WriteTo(writer, converters);
			}
			writer.WriteEndArray();
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.</value>
		// Token: 0x170001BD RID: 445
		[Nullable(2)]
		public override JToken this[object key]
		{
			[return: Nullable(2)]
			get
			{
				ValidationUtils.ArgumentNotNull(key, "key");
				if (!(key is int))
				{
					throw new ArgumentException("Accessed JArray values with invalid key value: {0}. Int32 array index expected.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(key)));
				}
				return this.GetItem((int)key);
			}
			[param: Nullable(2)]
			set
			{
				ValidationUtils.ArgumentNotNull(key, "key");
				if (!(key is int))
				{
					throw new ArgumentException("Set JArray values with invalid key value: {0}. Int32 array index expected.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(key)));
				}
				this.SetItem((int)key, value);
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> at the specified index.
		/// </summary>
		/// <value></value>
		// Token: 0x170001BE RID: 446
		public JToken this[int index]
		{
			get
			{
				return this.GetItem(index);
			}
			set
			{
				this.SetItem(index, value);
			}
		}

		// Token: 0x060009A4 RID: 2468 RVA: 0x00028706 File Offset: 0x00026906
		[NullableContext(2)]
		internal override int IndexOfItem(JToken item)
		{
			if (item == null)
			{
				return -1;
			}
			return this._values.IndexOfReference(item);
		}

		// Token: 0x060009A5 RID: 2469 RVA: 0x0002871C File Offset: 0x0002691C
		internal override void MergeItem(object content, [Nullable(2)] JsonMergeSettings settings)
		{
			IEnumerable enumerable = ((base.IsMultiContent(content) || content is JArray) ? ((IEnumerable)content) : null);
			if (enumerable == null)
			{
				return;
			}
			JContainer.MergeEnumerableContent(this, enumerable, settings);
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <returns>
		/// The index of <paramref name="item" /> if found in the list; otherwise, -1.
		/// </returns>
		// Token: 0x060009A6 RID: 2470 RVA: 0x00028750 File Offset: 0x00026950
		public int IndexOf(JToken item)
		{
			return this.IndexOfItem(item);
		}

		/// <summary>
		/// Inserts an item to the <see cref="T:Newtonsoft.Json.Linq.JArray" /> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The object to insert into the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// <paramref name="index" /> is not a valid index in the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </exception>
		// Token: 0x060009A7 RID: 2471 RVA: 0x00028759 File Offset: 0x00026959
		public void Insert(int index, JToken item)
		{
			this.InsertItem(index, item, false, true);
		}

		/// <summary>
		/// Removes the <see cref="T:Newtonsoft.Json.Linq.JArray" /> item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// <paramref name="index" /> is not a valid index in the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </exception>
		// Token: 0x060009A8 RID: 2472 RVA: 0x00028766 File Offset: 0x00026966
		public void RemoveAt(int index)
		{
			this.RemoveItemAt(index);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> that can be used to iterate through the collection.
		/// </returns>
		// Token: 0x060009A9 RID: 2473 RVA: 0x00028770 File Offset: 0x00026970
		public IEnumerator<JToken> GetEnumerator()
		{
			return this.Children().GetEnumerator();
		}

		/// <summary>
		/// Adds an item to the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		// Token: 0x060009AA RID: 2474 RVA: 0x0002878B File Offset: 0x0002698B
		public void Add(JToken item)
		{
			this.Add(item);
		}

		/// <summary>
		/// Removes all items from the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </summary>
		// Token: 0x060009AB RID: 2475 RVA: 0x00028794 File Offset: 0x00026994
		public void Clear()
		{
			this.ClearItems();
		}

		/// <summary>
		/// Determines whether the <see cref="T:Newtonsoft.Json.Linq.JArray" /> contains a specific value.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <returns>
		/// <c>true</c> if <paramref name="item" /> is found in the <see cref="T:Newtonsoft.Json.Linq.JArray" />; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x060009AC RID: 2476 RVA: 0x0002879C File Offset: 0x0002699C
		public bool Contains(JToken item)
		{
			return this.ContainsItem(item);
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:Newtonsoft.Json.Linq.JArray" /> to an array, starting at a particular array index.
		/// </summary>
		/// <param name="array">The array.</param>
		/// <param name="arrayIndex">Index of the array.</param>
		// Token: 0x060009AD RID: 2477 RVA: 0x000287A5 File Offset: 0x000269A5
		public void CopyTo(JToken[] array, int arrayIndex)
		{
			this.CopyItemsTo(array, arrayIndex);
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:Newtonsoft.Json.Linq.JArray" /> is read-only.
		/// </summary>
		/// <returns><c>true</c> if the <see cref="T:Newtonsoft.Json.Linq.JArray" /> is read-only; otherwise, <c>false</c>.</returns>
		// Token: 0x170001BF RID: 447
		// (get) Token: 0x060009AE RID: 2478 RVA: 0x000287AF File Offset: 0x000269AF
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </summary>
		/// <param name="item">The object to remove from the <see cref="T:Newtonsoft.Json.Linq.JArray" />.</param>
		/// <returns>
		/// <c>true</c> if <paramref name="item" /> was successfully removed from the <see cref="T:Newtonsoft.Json.Linq.JArray" />; otherwise, <c>false</c>. This method also returns <c>false</c> if <paramref name="item" /> is not found in the original <see cref="T:Newtonsoft.Json.Linq.JArray" />.
		/// </returns>
		// Token: 0x060009AF RID: 2479 RVA: 0x000287B2 File Offset: 0x000269B2
		public bool Remove(JToken item)
		{
			return this.RemoveItem(item);
		}

		// Token: 0x060009B0 RID: 2480 RVA: 0x000287BB File Offset: 0x000269BB
		internal override int GetDeepHashCode()
		{
			return base.ContentsHashCode();
		}

		// Token: 0x04000379 RID: 889
		private readonly List<JToken> _values = new List<JToken>();
	}
}
