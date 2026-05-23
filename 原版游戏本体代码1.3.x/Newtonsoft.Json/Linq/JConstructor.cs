using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a JSON constructor.
	/// </summary>
	// Token: 0x020000BB RID: 187
	[NullableContext(1)]
	[Nullable(0)]
	public class JConstructor : JContainer
	{
		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" /> asynchronously.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous write operation.</returns>
		// Token: 0x060009B1 RID: 2481 RVA: 0x000287C4 File Offset: 0x000269C4
		public override async Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
		{
			await writer.WriteStartConstructorAsync(this._name ?? string.Empty, cancellationToken).ConfigureAwait(false);
			for (int i = 0; i < this._values.Count; i++)
			{
				await this._values[i].WriteToAsync(writer, cancellationToken, converters).ConfigureAwait(false);
			}
			await writer.WriteEndConstructorAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously loads a <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" />.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous load. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns a <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x060009B2 RID: 2482 RVA: 0x0002881F File Offset: 0x00026A1F
		public new static Task<JConstructor> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			return JConstructor.LoadAsync(reader, null, cancellationToken);
		}

		/// <summary>
		/// Asynchronously loads a <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous load. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns a <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x060009B3 RID: 2483 RVA: 0x0002882C File Offset: 0x00026A2C
		public new static Task<JConstructor> LoadAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
		{
			JConstructor.<LoadAsync>d__2 <LoadAsync>d__;
			<LoadAsync>d__.<>t__builder = AsyncTaskMethodBuilder<JConstructor>.Create();
			<LoadAsync>d__.reader = reader;
			<LoadAsync>d__.settings = settings;
			<LoadAsync>d__.cancellationToken = cancellationToken;
			<LoadAsync>d__.<>1__state = -1;
			<LoadAsync>d__.<>t__builder.Start<JConstructor.<LoadAsync>d__2>(ref <LoadAsync>d__);
			return <LoadAsync>d__.<>t__builder.Task;
		}

		/// <summary>
		/// Gets the container's children tokens.
		/// </summary>
		/// <value>The container's children tokens.</value>
		// Token: 0x170001C0 RID: 448
		// (get) Token: 0x060009B4 RID: 2484 RVA: 0x0002887F File Offset: 0x00026A7F
		protected override IList<JToken> ChildrenTokens
		{
			get
			{
				return this._values;
			}
		}

		// Token: 0x060009B5 RID: 2485 RVA: 0x00028887 File Offset: 0x00026A87
		[NullableContext(2)]
		internal override int IndexOfItem(JToken item)
		{
			if (item == null)
			{
				return -1;
			}
			return this._values.IndexOfReference(item);
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x0002889C File Offset: 0x00026A9C
		internal override void MergeItem(object content, [Nullable(2)] JsonMergeSettings settings)
		{
			JConstructor jconstructor = content as JConstructor;
			if (jconstructor == null)
			{
				return;
			}
			if (jconstructor.Name != null)
			{
				this.Name = jconstructor.Name;
			}
			JContainer.MergeEnumerableContent(this, jconstructor, settings);
		}

		/// <summary>
		/// Gets or sets the name of this constructor.
		/// </summary>
		/// <value>The constructor name.</value>
		// Token: 0x170001C1 RID: 449
		// (get) Token: 0x060009B7 RID: 2487 RVA: 0x000288D0 File Offset: 0x00026AD0
		// (set) Token: 0x060009B8 RID: 2488 RVA: 0x000288D8 File Offset: 0x00026AD8
		[Nullable(2)]
		public string Name
		{
			[NullableContext(2)]
			get
			{
				return this._name;
			}
			[NullableContext(2)]
			set
			{
				this._name = value;
			}
		}

		/// <summary>
		/// Gets the node type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <value>The type.</value>
		// Token: 0x170001C2 RID: 450
		// (get) Token: 0x060009B9 RID: 2489 RVA: 0x000288E1 File Offset: 0x00026AE1
		public override JTokenType Type
		{
			get
			{
				return JTokenType.Constructor;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> class.
		/// </summary>
		// Token: 0x060009BA RID: 2490 RVA: 0x000288E4 File Offset: 0x00026AE4
		public JConstructor()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> class from another <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> object.
		/// </summary>
		/// <param name="other">A <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> object to copy from.</param>
		// Token: 0x060009BB RID: 2491 RVA: 0x000288F7 File Offset: 0x00026AF7
		public JConstructor(JConstructor other)
			: base(other, null)
		{
			this._name = other.Name;
		}

		// Token: 0x060009BC RID: 2492 RVA: 0x00028918 File Offset: 0x00026B18
		internal JConstructor(JConstructor other, [Nullable(2)] JsonCloneSettings settings)
			: base(other, settings)
		{
			this._name = other.Name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> class with the specified name and content.
		/// </summary>
		/// <param name="name">The constructor name.</param>
		/// <param name="content">The contents of the constructor.</param>
		// Token: 0x060009BD RID: 2493 RVA: 0x00028939 File Offset: 0x00026B39
		public JConstructor(string name, params object[] content)
			: this(name, content)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> class with the specified name and content.
		/// </summary>
		/// <param name="name">The constructor name.</param>
		/// <param name="content">The contents of the constructor.</param>
		// Token: 0x060009BE RID: 2494 RVA: 0x00028943 File Offset: 0x00026B43
		public JConstructor(string name, object content)
			: this(name)
		{
			this.Add(content);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> class with the specified name.
		/// </summary>
		/// <param name="name">The constructor name.</param>
		// Token: 0x060009BF RID: 2495 RVA: 0x00028953 File Offset: 0x00026B53
		public JConstructor(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException("Constructor name cannot be empty.", "name");
			}
			this._name = name;
		}

		// Token: 0x060009C0 RID: 2496 RVA: 0x00028994 File Offset: 0x00026B94
		internal override bool DeepEquals(JToken node)
		{
			JConstructor jconstructor = node as JConstructor;
			return jconstructor != null && this._name == jconstructor.Name && base.ContentsEqual(jconstructor);
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x000289C7 File Offset: 0x00026BC7
		internal override JToken CloneToken([Nullable(2)] JsonCloneSettings settings = null)
		{
			return new JConstructor(this, settings);
		}

		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		// Token: 0x060009C2 RID: 2498 RVA: 0x000289D0 File Offset: 0x00026BD0
		public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
		{
			writer.WriteStartConstructor(this._name);
			int count = this._values.Count;
			for (int i = 0; i < count; i++)
			{
				this._values[i].WriteTo(writer, converters);
			}
			writer.WriteEndConstructor();
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Linq.JToken" /> with the specified key.</value>
		// Token: 0x170001C3 RID: 451
		[Nullable(2)]
		public override JToken this[object key]
		{
			[return: Nullable(2)]
			get
			{
				ValidationUtils.ArgumentNotNull(key, "key");
				if (key is int)
				{
					int index = (int)key;
					return this.GetItem(index);
				}
				throw new ArgumentException("Accessed JConstructor values with invalid key value: {0}. Argument position index expected.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(key)));
			}
			[param: Nullable(2)]
			set
			{
				ValidationUtils.ArgumentNotNull(key, "key");
				if (key is int)
				{
					int index = (int)key;
					this.SetItem(index, value);
					return;
				}
				throw new ArgumentException("Set JConstructor values with invalid key value: {0}. Argument position index expected.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(key)));
			}
		}

		// Token: 0x060009C5 RID: 2501 RVA: 0x00028AB4 File Offset: 0x00026CB4
		internal override int GetDeepHashCode()
		{
			string name = this._name;
			return ((name != null) ? name.GetHashCode() : 0) ^ base.ContentsHashCode();
		}

		/// <summary>
		/// Loads a <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" />.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x060009C6 RID: 2502 RVA: 0x00028ACF File Offset: 0x00026CCF
		public new static JConstructor Load(JsonReader reader)
		{
			return JConstructor.Load(reader, null);
		}

		/// <summary>
		/// Loads a <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> from a <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">A <see cref="T:Newtonsoft.Json.JsonReader" /> that will be read for the content of the <see cref="T:Newtonsoft.Json.Linq.JConstructor" />.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonLoadSettings" /> used to load the JSON.
		/// If this is <c>null</c>, default load settings will be used.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JConstructor" /> that contains the JSON that was read from the specified <see cref="T:Newtonsoft.Json.JsonReader" />.</returns>
		// Token: 0x060009C7 RID: 2503 RVA: 0x00028AD8 File Offset: 0x00026CD8
		public new static JConstructor Load(JsonReader reader, [Nullable(2)] JsonLoadSettings settings)
		{
			if (reader.TokenType == JsonToken.None && !reader.Read())
			{
				throw JsonReaderException.Create(reader, "Error reading JConstructor from JsonReader.");
			}
			reader.MoveToContent();
			if (reader.TokenType != JsonToken.StartConstructor)
			{
				throw JsonReaderException.Create(reader, "Error reading JConstructor from JsonReader. Current JsonReader item is not a constructor: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
			}
			JConstructor jconstructor = new JConstructor((string)reader.Value);
			jconstructor.SetLineInfo(reader as IJsonLineInfo, settings);
			jconstructor.ReadTokenFrom(reader, settings);
			return jconstructor;
		}

		// Token: 0x0400037A RID: 890
		[Nullable(2)]
		private string _name;

		// Token: 0x0400037B RID: 891
		private readonly List<JToken> _values = new List<JToken>();
	}
}
