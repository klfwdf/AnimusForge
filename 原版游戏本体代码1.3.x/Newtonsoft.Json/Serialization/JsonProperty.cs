using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Maps a JSON property to a .NET member or constructor parameter.
	/// </summary>
	// Token: 0x02000093 RID: 147
	[NullableContext(2)]
	[Nullable(0)]
	public class JsonProperty
	{
		// Token: 0x1700011F RID: 287
		// (get) Token: 0x06000721 RID: 1825 RVA: 0x0001D888 File Offset: 0x0001BA88
		// (set) Token: 0x06000722 RID: 1826 RVA: 0x0001D890 File Offset: 0x0001BA90
		internal JsonContract PropertyContract { get; set; }

		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		/// <value>The name of the property.</value>
		// Token: 0x17000120 RID: 288
		// (get) Token: 0x06000723 RID: 1827 RVA: 0x0001D899 File Offset: 0x0001BA99
		// (set) Token: 0x06000724 RID: 1828 RVA: 0x0001D8A1 File Offset: 0x0001BAA1
		public string PropertyName
		{
			get
			{
				return this._propertyName;
			}
			set
			{
				this._propertyName = value;
				this._skipPropertyNameEscape = !JavaScriptUtils.ShouldEscapeJavaScriptString(this._propertyName, JavaScriptUtils.HtmlCharEscapeFlags);
			}
		}

		/// <summary>
		/// Gets or sets the type that declared this property.
		/// </summary>
		/// <value>The type that declared this property.</value>
		// Token: 0x17000121 RID: 289
		// (get) Token: 0x06000725 RID: 1829 RVA: 0x0001D8C3 File Offset: 0x0001BAC3
		// (set) Token: 0x06000726 RID: 1830 RVA: 0x0001D8CB File Offset: 0x0001BACB
		public Type DeclaringType { get; set; }

		/// <summary>
		/// Gets or sets the order of serialization of a member.
		/// </summary>
		/// <value>The numeric order of serialization.</value>
		// Token: 0x17000122 RID: 290
		// (get) Token: 0x06000727 RID: 1831 RVA: 0x0001D8D4 File Offset: 0x0001BAD4
		// (set) Token: 0x06000728 RID: 1832 RVA: 0x0001D8DC File Offset: 0x0001BADC
		public int? Order { get; set; }

		/// <summary>
		/// Gets or sets the name of the underlying member or parameter.
		/// </summary>
		/// <value>The name of the underlying member or parameter.</value>
		// Token: 0x17000123 RID: 291
		// (get) Token: 0x06000729 RID: 1833 RVA: 0x0001D8E5 File Offset: 0x0001BAE5
		// (set) Token: 0x0600072A RID: 1834 RVA: 0x0001D8ED File Offset: 0x0001BAED
		public string UnderlyingName { get; set; }

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Serialization.IValueProvider" /> that will get and set the <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> during serialization.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Serialization.IValueProvider" /> that will get and set the <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> during serialization.</value>
		// Token: 0x17000124 RID: 292
		// (get) Token: 0x0600072B RID: 1835 RVA: 0x0001D8F6 File Offset: 0x0001BAF6
		// (set) Token: 0x0600072C RID: 1836 RVA: 0x0001D8FE File Offset: 0x0001BAFE
		public IValueProvider ValueProvider { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.IAttributeProvider" /> for this property.
		/// </summary>
		/// <value>The <see cref="T:Newtonsoft.Json.Serialization.IAttributeProvider" /> for this property.</value>
		// Token: 0x17000125 RID: 293
		// (get) Token: 0x0600072D RID: 1837 RVA: 0x0001D907 File Offset: 0x0001BB07
		// (set) Token: 0x0600072E RID: 1838 RVA: 0x0001D90F File Offset: 0x0001BB0F
		public IAttributeProvider AttributeProvider { get; set; }

		/// <summary>
		/// Gets or sets the type of the property.
		/// </summary>
		/// <value>The type of the property.</value>
		// Token: 0x17000126 RID: 294
		// (get) Token: 0x0600072F RID: 1839 RVA: 0x0001D918 File Offset: 0x0001BB18
		// (set) Token: 0x06000730 RID: 1840 RVA: 0x0001D920 File Offset: 0x0001BB20
		public Type PropertyType
		{
			get
			{
				return this._propertyType;
			}
			set
			{
				if (this._propertyType != value)
				{
					this._propertyType = value;
					this._hasGeneratedDefaultValue = false;
				}
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.JsonConverter" /> for the property.
		/// If set this converter takes precedence over the contract converter for the property type.
		/// </summary>
		/// <value>The converter.</value>
		// Token: 0x17000127 RID: 295
		// (get) Token: 0x06000731 RID: 1841 RVA: 0x0001D93E File Offset: 0x0001BB3E
		// (set) Token: 0x06000732 RID: 1842 RVA: 0x0001D946 File Offset: 0x0001BB46
		public JsonConverter Converter { get; set; }

		/// <summary>
		/// Gets or sets the member converter.
		/// </summary>
		/// <value>The member converter.</value>
		// Token: 0x17000128 RID: 296
		// (get) Token: 0x06000733 RID: 1843 RVA: 0x0001D94F File Offset: 0x0001BB4F
		// (set) Token: 0x06000734 RID: 1844 RVA: 0x0001D957 File Offset: 0x0001BB57
		[Obsolete("MemberConverter is obsolete. Use Converter instead.")]
		public JsonConverter MemberConverter
		{
			get
			{
				return this.Converter;
			}
			set
			{
				this.Converter = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> is ignored.
		/// </summary>
		/// <value><c>true</c> if ignored; otherwise, <c>false</c>.</value>
		// Token: 0x17000129 RID: 297
		// (get) Token: 0x06000735 RID: 1845 RVA: 0x0001D960 File Offset: 0x0001BB60
		// (set) Token: 0x06000736 RID: 1846 RVA: 0x0001D968 File Offset: 0x0001BB68
		public bool Ignored { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> is readable.
		/// </summary>
		/// <value><c>true</c> if readable; otherwise, <c>false</c>.</value>
		// Token: 0x1700012A RID: 298
		// (get) Token: 0x06000737 RID: 1847 RVA: 0x0001D971 File Offset: 0x0001BB71
		// (set) Token: 0x06000738 RID: 1848 RVA: 0x0001D979 File Offset: 0x0001BB79
		public bool Readable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> is writable.
		/// </summary>
		/// <value><c>true</c> if writable; otherwise, <c>false</c>.</value>
		// Token: 0x1700012B RID: 299
		// (get) Token: 0x06000739 RID: 1849 RVA: 0x0001D982 File Offset: 0x0001BB82
		// (set) Token: 0x0600073A RID: 1850 RVA: 0x0001D98A File Offset: 0x0001BB8A
		public bool Writable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> has a member attribute.
		/// </summary>
		/// <value><c>true</c> if has a member attribute; otherwise, <c>false</c>.</value>
		// Token: 0x1700012C RID: 300
		// (get) Token: 0x0600073B RID: 1851 RVA: 0x0001D993 File Offset: 0x0001BB93
		// (set) Token: 0x0600073C RID: 1852 RVA: 0x0001D99B File Offset: 0x0001BB9B
		public bool HasMemberAttribute { get; set; }

		/// <summary>
		/// Gets the default value.
		/// </summary>
		/// <value>The default value.</value>
		// Token: 0x1700012D RID: 301
		// (get) Token: 0x0600073D RID: 1853 RVA: 0x0001D9A4 File Offset: 0x0001BBA4
		// (set) Token: 0x0600073E RID: 1854 RVA: 0x0001D9B6 File Offset: 0x0001BBB6
		public object DefaultValue
		{
			get
			{
				if (!this._hasExplicitDefaultValue)
				{
					return null;
				}
				return this._defaultValue;
			}
			set
			{
				this._hasExplicitDefaultValue = true;
				this._defaultValue = value;
			}
		}

		// Token: 0x0600073F RID: 1855 RVA: 0x0001D9C6 File Offset: 0x0001BBC6
		internal object GetResolvedDefaultValue()
		{
			if (this._propertyType == null)
			{
				return null;
			}
			if (!this._hasExplicitDefaultValue && !this._hasGeneratedDefaultValue)
			{
				this._defaultValue = ReflectionUtils.GetDefaultValue(this._propertyType);
				this._hasGeneratedDefaultValue = true;
			}
			return this._defaultValue;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> is required.
		/// </summary>
		/// <value>A value indicating whether this <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> is required.</value>
		// Token: 0x1700012E RID: 302
		// (get) Token: 0x06000740 RID: 1856 RVA: 0x0001DA06 File Offset: 0x0001BC06
		// (set) Token: 0x06000741 RID: 1857 RVA: 0x0001DA13 File Offset: 0x0001BC13
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
		/// Gets a value indicating whether <see cref="P:Newtonsoft.Json.Serialization.JsonProperty.Required" /> has a value specified.
		/// </summary>
		// Token: 0x1700012F RID: 303
		// (get) Token: 0x06000742 RID: 1858 RVA: 0x0001DA21 File Offset: 0x0001BC21
		public bool IsRequiredSpecified
		{
			get
			{
				return this._required != null;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this property preserves object references.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is reference; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x17000130 RID: 304
		// (get) Token: 0x06000743 RID: 1859 RVA: 0x0001DA2E File Offset: 0x0001BC2E
		// (set) Token: 0x06000744 RID: 1860 RVA: 0x0001DA36 File Offset: 0x0001BC36
		public bool? IsReference { get; set; }

		/// <summary>
		/// Gets or sets the property null value handling.
		/// </summary>
		/// <value>The null value handling.</value>
		// Token: 0x17000131 RID: 305
		// (get) Token: 0x06000745 RID: 1861 RVA: 0x0001DA3F File Offset: 0x0001BC3F
		// (set) Token: 0x06000746 RID: 1862 RVA: 0x0001DA47 File Offset: 0x0001BC47
		public NullValueHandling? NullValueHandling { get; set; }

		/// <summary>
		/// Gets or sets the property default value handling.
		/// </summary>
		/// <value>The default value handling.</value>
		// Token: 0x17000132 RID: 306
		// (get) Token: 0x06000747 RID: 1863 RVA: 0x0001DA50 File Offset: 0x0001BC50
		// (set) Token: 0x06000748 RID: 1864 RVA: 0x0001DA58 File Offset: 0x0001BC58
		public DefaultValueHandling? DefaultValueHandling { get; set; }

		/// <summary>
		/// Gets or sets the property reference loop handling.
		/// </summary>
		/// <value>The reference loop handling.</value>
		// Token: 0x17000133 RID: 307
		// (get) Token: 0x06000749 RID: 1865 RVA: 0x0001DA61 File Offset: 0x0001BC61
		// (set) Token: 0x0600074A RID: 1866 RVA: 0x0001DA69 File Offset: 0x0001BC69
		public ReferenceLoopHandling? ReferenceLoopHandling { get; set; }

		/// <summary>
		/// Gets or sets the property object creation handling.
		/// </summary>
		/// <value>The object creation handling.</value>
		// Token: 0x17000134 RID: 308
		// (get) Token: 0x0600074B RID: 1867 RVA: 0x0001DA72 File Offset: 0x0001BC72
		// (set) Token: 0x0600074C RID: 1868 RVA: 0x0001DA7A File Offset: 0x0001BC7A
		public ObjectCreationHandling? ObjectCreationHandling { get; set; }

		/// <summary>
		/// Gets or sets or sets the type name handling.
		/// </summary>
		/// <value>The type name handling.</value>
		// Token: 0x17000135 RID: 309
		// (get) Token: 0x0600074D RID: 1869 RVA: 0x0001DA83 File Offset: 0x0001BC83
		// (set) Token: 0x0600074E RID: 1870 RVA: 0x0001DA8B File Offset: 0x0001BC8B
		public TypeNameHandling? TypeNameHandling { get; set; }

		/// <summary>
		/// Gets or sets a predicate used to determine whether the property should be serialized.
		/// </summary>
		/// <value>A predicate used to determine whether the property should be serialized.</value>
		// Token: 0x17000136 RID: 310
		// (get) Token: 0x0600074F RID: 1871 RVA: 0x0001DA94 File Offset: 0x0001BC94
		// (set) Token: 0x06000750 RID: 1872 RVA: 0x0001DA9C File Offset: 0x0001BC9C
		[Nullable(new byte[] { 2, 1 })]
		public Predicate<object> ShouldSerialize
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Gets or sets a predicate used to determine whether the property should be deserialized.
		/// </summary>
		/// <value>A predicate used to determine whether the property should be deserialized.</value>
		// Token: 0x17000137 RID: 311
		// (get) Token: 0x06000751 RID: 1873 RVA: 0x0001DAA5 File Offset: 0x0001BCA5
		// (set) Token: 0x06000752 RID: 1874 RVA: 0x0001DAAD File Offset: 0x0001BCAD
		[Nullable(new byte[] { 2, 1 })]
		public Predicate<object> ShouldDeserialize
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Gets or sets a predicate used to determine whether the property should be serialized.
		/// </summary>
		/// <value>A predicate used to determine whether the property should be serialized.</value>
		// Token: 0x17000138 RID: 312
		// (get) Token: 0x06000753 RID: 1875 RVA: 0x0001DAB6 File Offset: 0x0001BCB6
		// (set) Token: 0x06000754 RID: 1876 RVA: 0x0001DABE File Offset: 0x0001BCBE
		[Nullable(new byte[] { 2, 1 })]
		public Predicate<object> GetIsSpecified
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Gets or sets an action used to set whether the property has been deserialized.
		/// </summary>
		/// <value>An action used to set whether the property has been deserialized.</value>
		// Token: 0x17000139 RID: 313
		// (get) Token: 0x06000755 RID: 1877 RVA: 0x0001DAC7 File Offset: 0x0001BCC7
		// (set) Token: 0x06000756 RID: 1878 RVA: 0x0001DACF File Offset: 0x0001BCCF
		[Nullable(new byte[] { 2, 1, 2 })]
		public Action<object, object> SetIsSpecified
		{
			[return: Nullable(new byte[] { 2, 1, 2 })]
			get;
			[param: Nullable(new byte[] { 2, 1, 2 })]
			set;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents this instance.
		/// </returns>
		// Token: 0x06000757 RID: 1879 RVA: 0x0001DAD8 File Offset: 0x0001BCD8
		[NullableContext(1)]
		public override string ToString()
		{
			return this.PropertyName ?? string.Empty;
		}

		/// <summary>
		/// Gets or sets the converter used when serializing the property's collection items.
		/// </summary>
		/// <value>The collection's items converter.</value>
		// Token: 0x1700013A RID: 314
		// (get) Token: 0x06000758 RID: 1880 RVA: 0x0001DAE9 File Offset: 0x0001BCE9
		// (set) Token: 0x06000759 RID: 1881 RVA: 0x0001DAF1 File Offset: 0x0001BCF1
		public JsonConverter ItemConverter { get; set; }

		/// <summary>
		/// Gets or sets whether this property's collection items are serialized as a reference.
		/// </summary>
		/// <value>Whether this property's collection items are serialized as a reference.</value>
		// Token: 0x1700013B RID: 315
		// (get) Token: 0x0600075A RID: 1882 RVA: 0x0001DAFA File Offset: 0x0001BCFA
		// (set) Token: 0x0600075B RID: 1883 RVA: 0x0001DB02 File Offset: 0x0001BD02
		public bool? ItemIsReference { get; set; }

		/// <summary>
		/// Gets or sets the type name handling used when serializing the property's collection items.
		/// </summary>
		/// <value>The collection's items type name handling.</value>
		// Token: 0x1700013C RID: 316
		// (get) Token: 0x0600075C RID: 1884 RVA: 0x0001DB0B File Offset: 0x0001BD0B
		// (set) Token: 0x0600075D RID: 1885 RVA: 0x0001DB13 File Offset: 0x0001BD13
		public TypeNameHandling? ItemTypeNameHandling { get; set; }

		/// <summary>
		/// Gets or sets the reference loop handling used when serializing the property's collection items.
		/// </summary>
		/// <value>The collection's items reference loop handling.</value>
		// Token: 0x1700013D RID: 317
		// (get) Token: 0x0600075E RID: 1886 RVA: 0x0001DB1C File Offset: 0x0001BD1C
		// (set) Token: 0x0600075F RID: 1887 RVA: 0x0001DB24 File Offset: 0x0001BD24
		public ReferenceLoopHandling? ItemReferenceLoopHandling { get; set; }

		// Token: 0x06000760 RID: 1888 RVA: 0x0001DB30 File Offset: 0x0001BD30
		[NullableContext(1)]
		internal void WritePropertyName(JsonWriter writer)
		{
			string propertyName = this.PropertyName;
			if (this._skipPropertyNameEscape)
			{
				writer.WritePropertyName(propertyName, false);
				return;
			}
			writer.WritePropertyName(propertyName);
		}

		// Token: 0x040002A3 RID: 675
		internal Required? _required;

		// Token: 0x040002A4 RID: 676
		internal bool _hasExplicitDefaultValue;

		// Token: 0x040002A5 RID: 677
		private object _defaultValue;

		// Token: 0x040002A6 RID: 678
		private bool _hasGeneratedDefaultValue;

		// Token: 0x040002A7 RID: 679
		private string _propertyName;

		// Token: 0x040002A8 RID: 680
		internal bool _skipPropertyNameEscape;

		// Token: 0x040002A9 RID: 681
		private Type _propertyType;
	}
}
