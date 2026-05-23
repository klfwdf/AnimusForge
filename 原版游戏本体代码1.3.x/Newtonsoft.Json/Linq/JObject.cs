using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a JSON object.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParse" title="Parsing a JSON Object from Text" />
	/// </example>
	// Token: 0x020000BE RID: 190
	[NullableContext(1)]
	[Nullable(0)]
	public class JObject : JContainer, IDictionary<string, JToken>, ICollection<KeyValuePair<string, JToken>>, IEnumerable<KeyValuePair<string, JToken>>, IEnumerable, INotifyPropertyChanged, ICustomTypeDescriptor, INotifyPropertyChanging
	{
		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" /> asynchronously.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous write operation.</returns>
		// Token: 0x06000A35 RID: 2613 RVA: 0x00029DD4 File Offset: 0x00027FD4
		public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
		{
			Task task = writer.WriteStartObjectAsync(cancellationToken);
			if (!task.IsCompletedSuccessfully())
			{
				return this.<WriteToAsync>g__AwaitProperties|0_0(task, 0, writer, cancellationToken, converters);
			}
			for (int i = 0; i < this._properties.Count; i++)
			{
				task = this._properties[i].WriteToAsync(writer, cancellationToken, converters);
				if (!task.IsCompletedSuccessfully())
				{
					return this.<WriteToAsync>g__AwaitProperties|0_0(task, i + 1, writer, cancellationToken, converters);
				}
			}
			return writer.WriteEndObjectAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously loads a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JObject" />.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous load. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns a <see cref="T:Newtonsoft.Json.Linq.JObject" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x06000A36 RID: 2614 RVA: 0x00029E45 File Offset: 0x00028045
		public new static Task<JObject> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			return JObject.LoadAsync(reader, null, cancellationToken);
		}

		/// <summary>
		/// Asynchronously loads a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JObject" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous load. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns a <see cref="T:Newtonsoft.Json.Linq.JObject" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x06000A37 RID: 2615 RVA: 0x00029E50 File Offset: 0x00028050
		public new static Task<JObject> LoadAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
		{
			JObject.<LoadAsync>d__2 <LoadAsync>d__;
			<LoadAsync>d__.<>t__builder = AsyncTaskMethodBuilder<JObject>.Create();
			<LoadAsync>d__.reader = reader;
			<LoadAsync>d__.settings = settings;
			<LoadAsync>d__.cancellationToken = cancellationToken;
			<LoadAsync>d__.<>1__state = -1;
			<LoadAsync>d__.<>t__builder.Start<JObject.<LoadAsync>d__2>(ref <LoadAsync>d__);
			return <LoadAsync>d__.<>t__builder.Task;
		}

		/// <summary>
		/// Gets the container's children tokens.
		/// </summary>
		/// <value>The container's children tokens.</value>
		// Token: 0x170001DA RID: 474
		// (get) Token: 0x06000A38 RID: 2616 RVA: 0x00029EA3 File Offset: 0x000280A3
		protected override IList<JToken> ChildrenTokens
		{
			get
			{
				return this._properties;
			}
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		// Token: 0x14000007 RID: 7
		// (add) Token: 0x06000A39 RID: 2617 RVA: 0x00029EAC File Offset: 0x000280AC
		// (remove) Token: 0x06000A3A RID: 2618 RVA: 0x00029EE4 File Offset: 0x000280E4
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Occurs when a property value is changing.
		/// </summary>
		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000A3B RID: 2619 RVA: 0x00029F1C File Offset: 0x0002811C
		// (remove) Token: 0x06000A3C RID: 2620 RVA: 0x00029F54 File Offset: 0x00028154
		[Nullable(2)]
		[method: NullableContext(2)]
		[Nullable(2)]
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JObject" /> class.
		/// </summary>
		// Token: 0x06000A3D RID: 2621 RVA: 0x00029F89 File Offset: 0x00028189
		public JObject()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JObject" /> class from another <see cref="T:Newtonsoft.Json.Linq.JObject" /> object.
		/// </summary>
		/// <param name="other">A <see cref="T:Newtonsoft.Json.Linq.JObject" /> object to copy from.</param>
		// Token: 0x06000A3E RID: 2622 RVA: 0x00029F9C File Offset: 0x0002819C
		public JObject(JObject other)
			: base(other, null)
		{
		}

		// Token: 0x06000A3F RID: 2623 RVA: 0x00029FB1 File Offset: 0x000281B1
		internal JObject(JObject other, [Nullable(2)] JsonCloneSettings settings)
			: base(other, settings)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JObject" /> class with the specified content.
		/// </summary>
		/// <param name="content">The contents of the object.</param>
		// Token: 0x06000A40 RID: 2624 RVA: 0x00029FC6 File Offset: 0x000281C6
		public JObject(params object[] content)
			: this(content)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JObject" /> class with the specified content.
		/// </summary>
		/// <param name="content">The contents of the object.</param>
		// Token: 0x06000A41 RID: 2625 RVA: 0x00029FCF File Offset: 0x000281CF
		public JObject(object content)
		{
			this.Add(content);
		}

		// Token: 0x06000A42 RID: 2626 RVA: 0x00029FEC File Offset: 0x000281EC
		internal override bool DeepEquals(JToken node)
		{
			JObject jobject = node as JObject;
			return jobject != null && this._properties.Compare(jobject._properties);
		}

		// Token: 0x06000A43 RID: 2627 RVA: 0x0002A016 File Offset: 0x00028216
		[NullableContext(2)]
		internal override int IndexOfItem(JToken item)
		{
			if (item == null)
			{
				return -1;
			}
			return this._properties.IndexOfReference(item);
		}

		// Token: 0x06000A44 RID: 2628 RVA: 0x0002A029 File Offset: 0x00028229
		[NullableContext(2)]
		internal override bool InsertItem(int index, JToken item, bool skipParentCheck, bool copyAnnotations)
		{
			return (item == null || item.Type != JTokenType.Comment) && base.InsertItem(index, item, skipParentCheck, copyAnnotations);
		}

		// Token: 0x06000A45 RID: 2629 RVA: 0x0002A044 File Offset: 0x00028244
		internal override void ValidateToken(JToken o, [Nullable(2)] JToken existing)
		{
			ValidationUtils.ArgumentNotNull(o, "o");
			if (o.Type != JTokenType.Property)
			{
				throw new ArgumentException("Can not add {0} to {1}.".FormatWith(CultureInfo.InvariantCulture, o.GetType(), base.GetType()));
			}
			JProperty jproperty = (JProperty)o;
			if (existing != null)
			{
				JProperty jproperty2 = (JProperty)existing;
				if (jproperty.Name == jproperty2.Name)
				{
					return;
				}
			}
			if (this._properties.TryGetValue(jproperty.Name, out existing))
			{
				throw new ArgumentException("Can not add property {0} to {1}. Property with the same name already exists on object.".FormatWith(CultureInfo.InvariantCulture, jproperty.Name, base.GetType()));
			}
		}

		// Token: 0x06000A46 RID: 2630 RVA: 0x0002A0E4 File Offset: 0x000282E4
		internal override void MergeItem(object content, [Nullable(2)] JsonMergeSettings settings)
		{
			JObject jobject = content as JObject;
			if (jobject == null)
			{
				return;
			}
			foreach (KeyValuePair<string, JToken> keyValuePair in jobject)
			{
				JProperty jproperty = this.Property(keyValuePair.Key, (settings != null) ? settings.PropertyNameComparison : StringComparison.Ordinal);
				if (jproperty == null)
				{
					this.Add(keyValuePair.Key, keyValuePair.Value);
				}
				else if (keyValuePair.Value != null)
				{
					JContainer jcontainer = jproperty.Value as JContainer;
					if (jcontainer == null || jcontainer.Type != keyValuePair.Value.Type)
					{
						if (!JObject.IsNull(keyValuePair.Value) || (settings != null && settings.MergeNullValueHandling == MergeNullValueHandling.Merge))
						{
							jproperty.Value = keyValuePair.Value;
						}
					}
					else
					{
						jcontainer.Merge(keyValuePair.Value, settings);
					}
				}
			}
		}

		// Token: 0x06000A47 RID: 2631 RVA: 0x0002A1D0 File Offset: 0x000283D0
		private static bool IsNull(JToken token)
		{
			if (token.Type == JTokenType.Null)
			{
				return true;
			}
			JValue jvalue = token as JValue;
			return jvalue != null && jvalue.Value == null;
		}

		// Token: 0x06000A48 RID: 2632 RVA: 0x0002A200 File Offset: 0x00028400
		internal void InternalPropertyChanged(JProperty childProperty)
		{
			this.OnPropertyChanged(childProperty.Name);
			if (this._listChanged != null)
			{
				this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, this.IndexOfItem(childProperty)));
			}
			if (this._collectionChanged != null)
			{
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, childProperty, childProperty, this.IndexOfItem(childProperty)));
			}
		}

		// Token: 0x06000A49 RID: 2633 RVA: 0x0002A251 File Offset: 0x00028451
		internal void InternalPropertyChanging(JProperty childProperty)
		{
			this.OnPropertyChanging(childProperty.Name);
		}

		// Token: 0x06000A4A RID: 2634 RVA: 0x0002A25F File Offset: 0x0002845F
		internal override JToken CloneToken([Nullable(2)] JsonCloneSettings settings)
		{
			return new JObject(this, settings);
		}

		/// <summary>
		/// Gets the node type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <value>The type.</value>
		// Token: 0x170001DB RID: 475
		// (get) Token: 0x06000A4B RID: 2635 RVA: 0x0002A268 File Offset: 0x00028468
		public override JTokenType Type
		{
			get
			{
				return JTokenType.Object;
			}
		}

		/// <summary>
		/// Gets an <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JProperty" /> of this object's properties.
		/// </summary>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JProperty" /> of this object's properties.</returns>
		// Token: 0x06000A4C RID: 2636 RVA: 0x0002A26B File Offset: 0x0002846B
		public IEnumerable<JProperty> Properties()
		{
			return this._properties.Cast<JProperty>();
		}

		/// <summary>
		/// Gets a <see cref="T:Newtonsoft.Json.Linq.JProperty" /> with the specified name.
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JProperty" /> with the specified name or <c>null</c>.</returns>
		// Token: 0x06000A4D RID: 2637 RVA: 0x0002A278 File Offset: 0x00028478
		[return: Nullable(2)]
		public JProperty Property(string name)
		{
			return this.Property(name, StringComparison.Ordinal);
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JProperty" /> with the specified name.
		/// The exact name will be searched for first and if no matching property is found then
		/// the <see cref="T:System.StringComparison" /> will be used to match a property.
		/// </summary>
		/// <param name="name">The property name.</param>
		/// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JProperty" /> matched with the specified name or <c>null</c>.</returns>
		// Token: 0x06000A4E RID: 2638 RVA: 0x0002A284 File Offset: 0x00028484
		[return: Nullable(2)]
		public JProperty Property(string name, StringComparison comparison)
		{
			if (name == null)
			{
				return null;
			}
			JToken jtoken;
			if (this._properties.TryGetValue(name, out jtoken))
			{
				return (JProperty)jtoken;
			}
			if (comparison != StringComparison.Ordinal)
			{
				for (int i = 0; i < this._properties.Count; i++)
				{
					JProperty jproperty = (JProperty)this._properties[i];
					if (string.Equals(jproperty.Name, name, comparison))
					{
						return jproperty;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Gets a <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> of this object's property values.
		/// </summary>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> of this object's property values.</returns>
		// Token: 0x06000A4F RID: 2639 RVA: 0x0002A2EB File Offset: 0x000284EB
		[return: Nullable(new byte[] { 0, 1 })]
		public JEnumerable<JToken> PropertyValues()
		{
			return new JEnumerable<JToken>(from p in this.Properties()
				select p.Value);
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.</value>
		// Token: 0x170001DC RID: 476
		[Nullable(2)]
		public override JToken this[object key]
		{
			[return: Nullable(2)]
			get
			{
				ValidationUtils.ArgumentNotNull(key, "key");
				string text = key as string;
				if (text == null)
				{
					throw new ArgumentException("Accessed JObject values with invalid key value: {0}. Object property name expected.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(key)));
				}
				return this[text];
			}
			[param: Nullable(2)]
			set
			{
				ValidationUtils.ArgumentNotNull(key, "key");
				string text = key as string;
				if (text == null)
				{
					throw new ArgumentException("Set JObject values with invalid key value: {0}. Object property name expected.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(key)));
				}
				this[text] = value;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.
		/// </summary>
		/// <value></value>
		// Token: 0x170001DD RID: 477
		[Nullable(2)]
		public JToken this[string propertyName]
		{
			[return: Nullable(2)]
			get
			{
				ValidationUtils.ArgumentNotNull(propertyName, "propertyName");
				JProperty jproperty = this.Property(propertyName, StringComparison.Ordinal);
				if (jproperty == null)
				{
					return null;
				}
				return jproperty.Value;
			}
			[param: Nullable(2)]
			set
			{
				JProperty jproperty = this.Property(propertyName, StringComparison.Ordinal);
				if (jproperty != null)
				{
					jproperty.Value = value;
					return;
				}
				this.OnPropertyChanging(propertyName);
				this.Add(propertyName, value);
				this.OnPropertyChanged(propertyName);
			}
		}

		/// <summary>
		/// Loads a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JObject" />.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JObject" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		/// <exception cref="T:Newtonsoft.Json.JsonReaderException">
		///     <paramref name="reader" /> is not valid JSON.
		/// </exception>
		// Token: 0x06000A54 RID: 2644 RVA: 0x0002A3FF File Offset: 0x000285FF
		public new static JObject Load(JsonReader reader)
		{
			return JObject.Load(reader, null);
		}

		/// <summary>
		/// Loads a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JObject" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JObject" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		/// <exception cref="T:Newtonsoft.Json.JsonReaderException">
		///     <paramref name="reader" /> is not valid JSON.
		/// </exception>
		// Token: 0x06000A55 RID: 2645 RVA: 0x0002A408 File Offset: 0x00028608
		public new static JObject Load(JsonReader reader, [Nullable(2)] JsonLoadSettings settings)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			if (reader.TokenType == JsonToken.None && !reader.Read())
			{
				throw JsonReaderException.Create(reader, "Error reading JObject from JsonReader.");
			}
			reader.MoveToContent();
			if (reader.TokenType != JsonToken.StartObject)
			{
				throw JsonReaderException.Create(reader, "Error reading JObject from JsonReader. Current JsonReader item is not an object: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
			}
			JObject jobject = new JObject();
			jobject.SetLineInfo(reader as IJsonLineInfo, settings);
			jobject.ReadTokenFrom(reader, settings);
			return jobject;
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from a string that contains JSON.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JObject" /> populated from the string that contains JSON.</returns>
		/// <exception cref="T:Newtonsoft.Json.JsonReaderException">
		///     <paramref name="json" /> is not valid JSON.
		/// </exception>
		/// <example>
		///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParse" title="Parsing a JSON Object from Text" />
		/// </example>
		// Token: 0x06000A56 RID: 2646 RVA: 0x0002A487 File Offset: 0x00028687
		public new static JObject Parse(string json)
		{
			return JObject.Parse(json, null);
		}

		/// <summary>
		/// Load a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from a string that contains JSON.
		/// </summary>
		/// <param name="json">A <see cref="T:System.String" /> that contains JSON.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JObject" /> populated from the string that contains JSON.</returns>
		/// <exception cref="T:Newtonsoft.Json.JsonReaderException">
		///     <paramref name="json" /> is not valid JSON.
		/// </exception>
		/// <example>
		///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\LinqToJsonTests.cs" region="LinqToJsonCreateParse" title="Parsing a JSON Object from Text" />
		/// </example>
		// Token: 0x06000A57 RID: 2647 RVA: 0x0002A490 File Offset: 0x00028690
		public new static JObject Parse(string json, [Nullable(2)] JsonLoadSettings settings)
		{
			JObject result;
			using (JsonReader jsonReader = new JsonTextReader(new StringReader(json)))
			{
				JObject jobject = JObject.Load(jsonReader, settings);
				while (jsonReader.Read())
				{
				}
				result = jobject;
			}
			return result;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from an object.
		/// </summary>
		/// <param name="o">The object that will be used to create <see cref="T:Newtonsoft.Json.Linq.JObject" />.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JObject" /> with the values of the specified object.</returns>
		// Token: 0x06000A58 RID: 2648 RVA: 0x0002A4D8 File Offset: 0x000286D8
		public new static JObject FromObject(object o)
		{
			return JObject.FromObject(o, JsonSerializer.CreateDefault());
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JObject" /> from an object.
		/// </summary>
		/// <param name="o">The object that will be used to create <see cref="T:Newtonsoft.Json.Linq.JObject" />.</param>
		/// <param name="jsonSerializer">The <see cref="T:Newtonsoft.Json.JsonSerializer" /> that will be used to read the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JObject" /> with the values of the specified object.</returns>
		// Token: 0x06000A59 RID: 2649 RVA: 0x0002A4E8 File Offset: 0x000286E8
		public new static JObject FromObject(object o, JsonSerializer jsonSerializer)
		{
			JToken jtoken = JToken.FromObjectInternal(o, jsonSerializer);
			if (jtoken.Type != JTokenType.Object)
			{
				throw new ArgumentException("Object serialized to {0}. JObject instance expected.".FormatWith(CultureInfo.InvariantCulture, jtoken.Type));
			}
			return (JObject)jtoken;
		}

		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		// Token: 0x06000A5A RID: 2650 RVA: 0x0002A52C File Offset: 0x0002872C
		public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
		{
			writer.WriteStartObject();
			for (int i = 0; i < this._properties.Count; i++)
			{
				this._properties[i].WriteTo(writer, converters);
			}
			writer.WriteEndObject();
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.</returns>
		// Token: 0x06000A5B RID: 2651 RVA: 0x0002A56E File Offset: 0x0002876E
		[NullableContext(2)]
		public JToken GetValue(string propertyName)
		{
			return this.GetValue(propertyName, StringComparison.Ordinal);
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.
		/// The exact property name will be searched for first and if no matching property is found then
		/// the <see cref="T:System.StringComparison" /> will be used to match a property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.</returns>
		// Token: 0x06000A5C RID: 2652 RVA: 0x0002A578 File Offset: 0x00028778
		[NullableContext(2)]
		public JToken GetValue(string propertyName, StringComparison comparison)
		{
			if (propertyName == null)
			{
				return null;
			}
			JProperty jproperty = this.Property(propertyName, comparison);
			if (jproperty == null)
			{
				return null;
			}
			return jproperty.Value;
		}

		/// <summary>
		/// Tries to get the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.
		/// The exact property name will be searched for first and if no matching property is found then
		/// the <see cref="T:System.StringComparison" /> will be used to match a property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <param name="comparison">One of the enumeration values that specifies how the strings will be compared.</param>
		/// <returns><c>true</c> if a value was successfully retrieved; otherwise, <c>false</c>.</returns>
		// Token: 0x06000A5D RID: 2653 RVA: 0x0002A592 File Offset: 0x00028792
		public bool TryGetValue(string propertyName, StringComparison comparison, [Nullable(2)] [NotNullWhen(true)] out JToken value)
		{
			value = this.GetValue(propertyName, comparison);
			return value != null;
		}

		/// <summary>
		/// Adds the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		// Token: 0x06000A5E RID: 2654 RVA: 0x0002A5A3 File Offset: 0x000287A3
		public void Add(string propertyName, [Nullable(2)] JToken value)
		{
			this.Add(new JProperty(propertyName, value));
		}

		/// <summary>
		/// Determines whether the JSON object has the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns><c>true</c> if the JSON object has the specified property name; otherwise, <c>false</c>.</returns>
		// Token: 0x06000A5F RID: 2655 RVA: 0x0002A5B2 File Offset: 0x000287B2
		public bool ContainsKey(string propertyName)
		{
			ValidationUtils.ArgumentNotNull(propertyName, "propertyName");
			return this._properties.Contains(propertyName);
		}

		// Token: 0x170001DE RID: 478
		// (get) Token: 0x06000A60 RID: 2656 RVA: 0x0002A5CB File Offset: 0x000287CB
		ICollection<string> IDictionary<string, JToken>.Keys
		{
			get
			{
				return this._properties.Keys;
			}
		}

		/// <summary>
		/// Removes the property with the specified name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns><c>true</c> if item was successfully removed; otherwise, <c>false</c>.</returns>
		// Token: 0x06000A61 RID: 2657 RVA: 0x0002A5D8 File Offset: 0x000287D8
		public bool Remove(string propertyName)
		{
			JProperty jproperty = this.Property(propertyName, StringComparison.Ordinal);
			if (jproperty == null)
			{
				return false;
			}
			jproperty.Remove();
			return true;
		}

		/// <summary>
		/// Tries to get the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value.</param>
		/// <returns><c>true</c> if a value was successfully retrieved; otherwise, <c>false</c>.</returns>
		// Token: 0x06000A62 RID: 2658 RVA: 0x0002A5FC File Offset: 0x000287FC
		public bool TryGetValue(string propertyName, [Nullable(2)] [NotNullWhen(true)] out JToken value)
		{
			JProperty jproperty = this.Property(propertyName, StringComparison.Ordinal);
			if (jproperty == null)
			{
				value = null;
				return false;
			}
			value = jproperty.Value;
			return true;
		}

		// Token: 0x170001DF RID: 479
		// (get) Token: 0x06000A63 RID: 2659 RVA: 0x0002A623 File Offset: 0x00028823
		[Nullable(new byte[] { 1, 2 })]
		ICollection<JToken> IDictionary<string, JToken>.Values
		{
			[return: Nullable(new byte[] { 1, 2 })]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x06000A64 RID: 2660 RVA: 0x0002A62A File Offset: 0x0002882A
		void ICollection<KeyValuePair<string, JToken>>.Add([Nullable(new byte[] { 0, 1, 2 })] KeyValuePair<string, JToken> item)
		{
			this.Add(new JProperty(item.Key, item.Value));
		}

		// Token: 0x06000A65 RID: 2661 RVA: 0x0002A645 File Offset: 0x00028845
		void ICollection<KeyValuePair<string, JToken>>.Clear()
		{
			base.RemoveAll();
		}

		// Token: 0x06000A66 RID: 2662 RVA: 0x0002A650 File Offset: 0x00028850
		bool ICollection<KeyValuePair<string, JToken>>.Contains([Nullable(new byte[] { 0, 1, 2 })] KeyValuePair<string, JToken> item)
		{
			JProperty jproperty = this.Property(item.Key, StringComparison.Ordinal);
			return jproperty != null && jproperty.Value == item.Value;
		}

		// Token: 0x06000A67 RID: 2663 RVA: 0x0002A680 File Offset: 0x00028880
		void ICollection<KeyValuePair<string, JToken>>.CopyTo([Nullable(new byte[] { 1, 0, 1, 2 })] KeyValuePair<string, JToken>[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex is less than 0.");
			}
			if (arrayIndex >= array.Length && arrayIndex != 0)
			{
				throw new ArgumentException("arrayIndex is equal to or greater than the length of array.");
			}
			if (base.Count > array.Length - arrayIndex)
			{
				throw new ArgumentException("The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.");
			}
			int num = 0;
			foreach (JToken jtoken in this._properties)
			{
				JProperty jproperty = (JProperty)jtoken;
				array[arrayIndex + num] = new KeyValuePair<string, JToken>(jproperty.Name, jproperty.Value);
				num++;
			}
		}

		// Token: 0x170001E0 RID: 480
		// (get) Token: 0x06000A68 RID: 2664 RVA: 0x0002A73C File Offset: 0x0002893C
		bool ICollection<KeyValuePair<string, JToken>>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000A69 RID: 2665 RVA: 0x0002A73F File Offset: 0x0002893F
		bool ICollection<KeyValuePair<string, JToken>>.Remove([Nullable(new byte[] { 0, 1, 2 })] KeyValuePair<string, JToken> item)
		{
			if (!((ICollection<KeyValuePair<string, JToken>>)this).Contains(item))
			{
				return false;
			}
			((IDictionary<string, JToken>)this).Remove(item.Key);
			return true;
		}

		// Token: 0x06000A6A RID: 2666 RVA: 0x0002A75B File Offset: 0x0002895B
		internal override int GetDeepHashCode()
		{
			return base.ContentsHashCode();
		}

		/// <summary>
		/// Returns an enumerator that can be used to iterate through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		// Token: 0x06000A6B RID: 2667 RVA: 0x0002A763 File Offset: 0x00028963
		[return: Nullable(new byte[] { 1, 0, 1, 2 })]
		public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator()
		{
			foreach (JToken jtoken in this._properties)
			{
				JProperty jproperty = (JProperty)jtoken;
				yield return new KeyValuePair<string, JToken>(jproperty.Name, jproperty.Value);
			}
			IEnumerator<JToken> enumerator = null;
			yield break;
			yield break;
		}

		/// <summary>
		/// Raises the <see cref="E:Newtonsoft.Json.Linq.JObject.PropertyChanged" /> event with the provided arguments.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		// Token: 0x06000A6C RID: 2668 RVA: 0x0002A772 File Offset: 0x00028972
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged == null)
			{
				return;
			}
			propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Raises the <see cref="E:Newtonsoft.Json.Linq.JObject.PropertyChanging" /> event with the provided arguments.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		// Token: 0x06000A6D RID: 2669 RVA: 0x0002A78B File Offset: 0x0002898B
		protected virtual void OnPropertyChanging(string propertyName)
		{
			PropertyChangingEventHandler propertyChanging = this.PropertyChanging;
			if (propertyChanging == null)
			{
				return;
			}
			propertyChanging(this, new PropertyChangingEventArgs(propertyName));
		}

		// Token: 0x06000A6E RID: 2670 RVA: 0x0002A7A4 File Offset: 0x000289A4
		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
		{
			return ((ICustomTypeDescriptor)this).GetProperties(null);
		}

		// Token: 0x06000A6F RID: 2671 RVA: 0x0002A7B0 File Offset: 0x000289B0
		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties([Nullable(new byte[] { 2, 1 })] Attribute[] attributes)
		{
			PropertyDescriptor[] array = new PropertyDescriptor[base.Count];
			int num = 0;
			foreach (KeyValuePair<string, JToken> keyValuePair in this)
			{
				array[num] = new JPropertyDescriptor(keyValuePair.Key);
				num++;
			}
			return new PropertyDescriptorCollection(array);
		}

		// Token: 0x06000A70 RID: 2672 RVA: 0x0002A818 File Offset: 0x00028A18
		AttributeCollection ICustomTypeDescriptor.GetAttributes()
		{
			return AttributeCollection.Empty;
		}

		// Token: 0x06000A71 RID: 2673 RVA: 0x0002A81F File Offset: 0x00028A1F
		[NullableContext(2)]
		string ICustomTypeDescriptor.GetClassName()
		{
			return null;
		}

		// Token: 0x06000A72 RID: 2674 RVA: 0x0002A822 File Offset: 0x00028A22
		[NullableContext(2)]
		string ICustomTypeDescriptor.GetComponentName()
		{
			return null;
		}

		// Token: 0x06000A73 RID: 2675 RVA: 0x0002A825 File Offset: 0x00028A25
		TypeConverter ICustomTypeDescriptor.GetConverter()
		{
			return new TypeConverter();
		}

		// Token: 0x06000A74 RID: 2676 RVA: 0x0002A82C File Offset: 0x00028A2C
		[NullableContext(2)]
		EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
		{
			return null;
		}

		// Token: 0x06000A75 RID: 2677 RVA: 0x0002A82F File Offset: 0x00028A2F
		[NullableContext(2)]
		PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
		{
			return null;
		}

		// Token: 0x06000A76 RID: 2678 RVA: 0x0002A832 File Offset: 0x00028A32
		[return: Nullable(2)]
		object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
		{
			return null;
		}

		// Token: 0x06000A77 RID: 2679 RVA: 0x0002A835 File Offset: 0x00028A35
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents([Nullable(new byte[] { 2, 1 })] Attribute[] attributes)
		{
			return EventDescriptorCollection.Empty;
		}

		// Token: 0x06000A78 RID: 2680 RVA: 0x0002A83C File Offset: 0x00028A3C
		EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
		{
			return EventDescriptorCollection.Empty;
		}

		// Token: 0x06000A79 RID: 2681 RVA: 0x0002A843 File Offset: 0x00028A43
		[NullableContext(2)]
		object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
		{
			if (pd is JPropertyDescriptor)
			{
				return this;
			}
			return null;
		}

		/// <summary>
		/// Returns the <see cref="T:System.Dynamic.DynamicMetaObject" /> responsible for binding operations performed on this object.
		/// </summary>
		/// <param name="parameter">The expression tree representation of the runtime value.</param>
		/// <returns>
		/// The <see cref="T:System.Dynamic.DynamicMetaObject" /> to bind this object.
		/// </returns>
		// Token: 0x06000A7A RID: 2682 RVA: 0x0002A850 File Offset: 0x00028A50
		protected override DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return new DynamicProxyMetaObject<JObject>(parameter, this, new JObject.JObjectDynamicProxy());
		}

		// Token: 0x06000A7B RID: 2683 RVA: 0x0002A860 File Offset: 0x00028A60
		[CompilerGenerated]
		private async Task <WriteToAsync>g__AwaitProperties|0_0(Task task, int i, JsonWriter Writer, CancellationToken CancellationToken, JsonConverter[] Converters)
		{
			await task.ConfigureAwait(false);
			while (i < this._properties.Count)
			{
				await this._properties[i].WriteToAsync(Writer, CancellationToken, Converters).ConfigureAwait(false);
				i++;
			}
			await Writer.WriteEndObjectAsync(CancellationToken).ConfigureAwait(false);
		}

		// Token: 0x04000383 RID: 899
		private readonly JPropertyKeyedCollection _properties = new JPropertyKeyedCollection();

		// Token: 0x020001C6 RID: 454
		[Nullable(new byte[] { 0, 1 })]
		private class JObjectDynamicProxy : DynamicProxy<JObject>
		{
			// Token: 0x06000FA8 RID: 4008 RVA: 0x00044FFE File Offset: 0x000431FE
			public override bool TryGetMember(JObject instance, GetMemberBinder binder, [Nullable(2)] out object result)
			{
				result = instance[binder.Name];
				return true;
			}

			// Token: 0x06000FA9 RID: 4009 RVA: 0x00045010 File Offset: 0x00043210
			public override bool TrySetMember(JObject instance, SetMemberBinder binder, object value)
			{
				JToken jtoken = value as JToken;
				if (jtoken == null)
				{
					jtoken = new JValue(value);
				}
				instance[binder.Name] = jtoken;
				return true;
			}

			// Token: 0x06000FAA RID: 4010 RVA: 0x0004503C File Offset: 0x0004323C
			public override IEnumerable<string> GetDynamicMemberNames(JObject instance)
			{
				return from p in instance.Properties()
					select p.Name;
			}
		}
	}
}
