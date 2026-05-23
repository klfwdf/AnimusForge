using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Instructs the <see cref="T:Newtonsoft.Json.JsonSerializer" /> to always serialize the member with the specified name.
	/// </summary>
	// Token: 0x02000026 RID: 38
	[NullableContext(2)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class JsonPropertyAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.JsonConverter" /> type used when serializing the property's collection items.
		/// </summary>
		/// <value>The collection's items <see cref="T:Newtonsoft.Json.JsonConverter" /> type.</value>
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060000B3 RID: 179 RVA: 0x0000336A File Offset: 0x0000156A
		// (set) Token: 0x060000B4 RID: 180 RVA: 0x00003372 File Offset: 0x00001572
		public Type ItemConverterType { get; set; }

		/// <summary>
		/// The parameter list to use when constructing the <see cref="T:Newtonsoft.Json.JsonConverter" /> described by <see cref="P:Newtonsoft.Json.JsonPropertyAttribute.ItemConverterType" />.
		/// If <c>null</c>, the default constructor is used.
		/// When non-<c>null</c>, there must be a constructor defined in the <see cref="T:Newtonsoft.Json.JsonConverter" /> that exactly matches the number,
		/// order, and type of these parameters.
		/// </summary>
		/// <example>
		/// <code>
		/// [JsonProperty(ItemConverterType = typeof(MyContainerConverter), ItemConverterParameters = new object[] { 123, "Four" })]
		/// </code>
		/// </example>
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060000B5 RID: 181 RVA: 0x0000337B File Offset: 0x0000157B
		// (set) Token: 0x060000B6 RID: 182 RVA: 0x00003383 File Offset: 0x00001583
		[Nullable(new byte[] { 2, 1 })]
		public object[] ItemConverterParameters
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Gets or sets the <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" />.
		/// </summary>
		/// <value>The <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" />.</value>
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000B7 RID: 183 RVA: 0x0000338C File Offset: 0x0000158C
		// (set) Token: 0x060000B8 RID: 184 RVA: 0x00003394 File Offset: 0x00001594
		public Type NamingStrategyType { get; set; }

		/// <summary>
		/// The parameter list to use when constructing the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> described by <see cref="P:Newtonsoft.Json.JsonPropertyAttribute.NamingStrategyType" />.
		/// If <c>null</c>, the default constructor is used.
		/// When non-<c>null</c>, there must be a constructor defined in the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> that exactly matches the number,
		/// order, and type of these parameters.
		/// </summary>
		/// <example>
		/// <code>
		/// [JsonProperty(NamingStrategyType = typeof(MyNamingStrategy), NamingStrategyParameters = new object[] { 123, "Four" })]
		/// </code>
		/// </example>
		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060000B9 RID: 185 RVA: 0x0000339D File Offset: 0x0000159D
		// (set) Token: 0x060000BA RID: 186 RVA: 0x000033A5 File Offset: 0x000015A5
		[Nullable(new byte[] { 2, 1 })]
		public object[] NamingStrategyParameters
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Gets or sets the null value handling used when serializing this property.
		/// </summary>
		/// <value>The null value handling.</value>
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000BB RID: 187 RVA: 0x000033AE File Offset: 0x000015AE
		// (set) Token: 0x060000BC RID: 188 RVA: 0x000033BB File Offset: 0x000015BB
		public NullValueHandling NullValueHandling
		{
			get
			{
				return this._nullValueHandling.GetValueOrDefault();
			}
			set
			{
				this._nullValueHandling = new NullValueHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the default value handling used when serializing this property.
		/// </summary>
		/// <value>The default value handling.</value>
		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000BD RID: 189 RVA: 0x000033C9 File Offset: 0x000015C9
		// (set) Token: 0x060000BE RID: 190 RVA: 0x000033D6 File Offset: 0x000015D6
		public DefaultValueHandling DefaultValueHandling
		{
			get
			{
				return this._defaultValueHandling.GetValueOrDefault();
			}
			set
			{
				this._defaultValueHandling = new DefaultValueHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the reference loop handling used when serializing this property.
		/// </summary>
		/// <value>The reference loop handling.</value>
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000BF RID: 191 RVA: 0x000033E4 File Offset: 0x000015E4
		// (set) Token: 0x060000C0 RID: 192 RVA: 0x000033F1 File Offset: 0x000015F1
		public ReferenceLoopHandling ReferenceLoopHandling
		{
			get
			{
				return this._referenceLoopHandling.GetValueOrDefault();
			}
			set
			{
				this._referenceLoopHandling = new ReferenceLoopHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the object creation handling used when deserializing this property.
		/// </summary>
		/// <value>The object creation handling.</value>
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000C1 RID: 193 RVA: 0x000033FF File Offset: 0x000015FF
		// (set) Token: 0x060000C2 RID: 194 RVA: 0x0000340C File Offset: 0x0000160C
		public ObjectCreationHandling ObjectCreationHandling
		{
			get
			{
				return this._objectCreationHandling.GetValueOrDefault();
			}
			set
			{
				this._objectCreationHandling = new ObjectCreationHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the type name handling used when serializing this property.
		/// </summary>
		/// <value>The type name handling.</value>
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x0000341A File Offset: 0x0000161A
		// (set) Token: 0x060000C4 RID: 196 RVA: 0x00003427 File Offset: 0x00001627
		public TypeNameHandling TypeNameHandling
		{
			get
			{
				return this._typeNameHandling.GetValueOrDefault();
			}
			set
			{
				this._typeNameHandling = new TypeNameHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets whether this property's value is serialized as a reference.
		/// </summary>
		/// <value>Whether this property's value is serialized as a reference.</value>
		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x00003435 File Offset: 0x00001635
		// (set) Token: 0x060000C6 RID: 198 RVA: 0x00003442 File Offset: 0x00001642
		public bool IsReference
		{
			get
			{
				return this._isReference.GetValueOrDefault();
			}
			set
			{
				this._isReference = new bool?(value);
			}
		}

		/// <summary>
		/// Gets or sets the order of serialization of a member.
		/// </summary>
		/// <value>The numeric order of serialization.</value>
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x00003450 File Offset: 0x00001650
		// (set) Token: 0x060000C8 RID: 200 RVA: 0x0000345D File Offset: 0x0000165D
		public int Order
		{
			get
			{
				return this._order.GetValueOrDefault();
			}
			set
			{
				this._order = new int?(value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this property is required.
		/// </summary>
		/// <value>
		/// 	A value indicating whether this property is required.
		/// </value>
		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000C9 RID: 201 RVA: 0x0000346B File Offset: 0x0000166B
		// (set) Token: 0x060000CA RID: 202 RVA: 0x00003478 File Offset: 0x00001678
		public Required Required
		{
			get
			{
				return this._required.GetValueOrDefault();
			}
			set
			{
				this._required = new Required?(value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		/// <value>The name of the property.</value>
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000CB RID: 203 RVA: 0x00003486 File Offset: 0x00001686
		// (set) Token: 0x060000CC RID: 204 RVA: 0x0000348E File Offset: 0x0000168E
		public string PropertyName { get; set; }

		/// <summary>
		/// Gets or sets the reference loop handling used when serializing the property's collection items.
		/// </summary>
		/// <value>The collection's items reference loop handling.</value>
		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000CD RID: 205 RVA: 0x00003497 File Offset: 0x00001697
		// (set) Token: 0x060000CE RID: 206 RVA: 0x000034A4 File Offset: 0x000016A4
		public ReferenceLoopHandling ItemReferenceLoopHandling
		{
			get
			{
				return this._itemReferenceLoopHandling.GetValueOrDefault();
			}
			set
			{
				this._itemReferenceLoopHandling = new ReferenceLoopHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the type name handling used when serializing the property's collection items.
		/// </summary>
		/// <value>The collection's items type name handling.</value>
		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000CF RID: 207 RVA: 0x000034B2 File Offset: 0x000016B2
		// (set) Token: 0x060000D0 RID: 208 RVA: 0x000034BF File Offset: 0x000016BF
		public TypeNameHandling ItemTypeNameHandling
		{
			get
			{
				return this._itemTypeNameHandling.GetValueOrDefault();
			}
			set
			{
				this._itemTypeNameHandling = new TypeNameHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets whether this property's collection items are serialized as a reference.
		/// </summary>
		/// <value>Whether this property's collection items are serialized as a reference.</value>
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000D1 RID: 209 RVA: 0x000034CD File Offset: 0x000016CD
		// (set) Token: 0x060000D2 RID: 210 RVA: 0x000034DA File Offset: 0x000016DA
		public bool ItemIsReference
		{
			get
			{
				return this._itemIsReference.GetValueOrDefault();
			}
			set
			{
				this._itemIsReference = new bool?(value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" /> class.
		/// </summary>
		// Token: 0x060000D3 RID: 211 RVA: 0x000034E8 File Offset: 0x000016E8
		public JsonPropertyAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" /> class with the specified name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		// Token: 0x060000D4 RID: 212 RVA: 0x000034F0 File Offset: 0x000016F0
		[NullableContext(1)]
		public JsonPropertyAttribute(string propertyName)
		{
			this.PropertyName = propertyName;
		}

		// Token: 0x0400004E RID: 78
		internal NullValueHandling? _nullValueHandling;

		// Token: 0x0400004F RID: 79
		internal DefaultValueHandling? _defaultValueHandling;

		// Token: 0x04000050 RID: 80
		internal ReferenceLoopHandling? _referenceLoopHandling;

		// Token: 0x04000051 RID: 81
		internal ObjectCreationHandling? _objectCreationHandling;

		// Token: 0x04000052 RID: 82
		internal TypeNameHandling? _typeNameHandling;

		// Token: 0x04000053 RID: 83
		internal bool? _isReference;

		// Token: 0x04000054 RID: 84
		internal int? _order;

		// Token: 0x04000055 RID: 85
		internal Required? _required;

		// Token: 0x04000056 RID: 86
		internal bool? _itemIsReference;

		// Token: 0x04000057 RID: 87
		internal ReferenceLoopHandling? _itemReferenceLoopHandling;

		// Token: 0x04000058 RID: 88
		internal TypeNameHandling? _itemTypeNameHandling;
	}
}
