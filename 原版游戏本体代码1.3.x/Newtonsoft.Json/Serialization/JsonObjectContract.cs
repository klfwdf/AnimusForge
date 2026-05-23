using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000091 RID: 145
	[NullableContext(2)]
	[Nullable(0)]
	public class JsonObjectContract : JsonContainerContract
	{
		/// <summary>
		/// Gets or sets the object member serialization.
		/// </summary>
		/// <value>The member object serialization.</value>
		// Token: 0x17000111 RID: 273
		// (get) Token: 0x06000704 RID: 1796 RVA: 0x0001D4EA File Offset: 0x0001B6EA
		// (set) Token: 0x06000705 RID: 1797 RVA: 0x0001D4F2 File Offset: 0x0001B6F2
		public MemberSerialization MemberSerialization { get; set; }

		/// <summary>
		/// Gets or sets the missing member handling used when deserializing this object.
		/// </summary>
		/// <value>The missing member handling.</value>
		// Token: 0x17000112 RID: 274
		// (get) Token: 0x06000706 RID: 1798 RVA: 0x0001D4FB File Offset: 0x0001B6FB
		// (set) Token: 0x06000707 RID: 1799 RVA: 0x0001D503 File Offset: 0x0001B703
		public MissingMemberHandling? MissingMemberHandling { get; set; }

		/// <summary>
		/// Gets or sets a value that indicates whether the object's properties are required.
		/// </summary>
		/// <value>
		/// 	A value indicating whether the object's properties are required.
		/// </value>
		// Token: 0x17000113 RID: 275
		// (get) Token: 0x06000708 RID: 1800 RVA: 0x0001D50C File Offset: 0x0001B70C
		// (set) Token: 0x06000709 RID: 1801 RVA: 0x0001D514 File Offset: 0x0001B714
		public Required? ItemRequired { get; set; }

		/// <summary>
		/// Gets or sets how the object's properties with null values are handled during serialization and deserialization.
		/// </summary>
		/// <value>How the object's properties with null values are handled during serialization and deserialization.</value>
		// Token: 0x17000114 RID: 276
		// (get) Token: 0x0600070A RID: 1802 RVA: 0x0001D51D File Offset: 0x0001B71D
		// (set) Token: 0x0600070B RID: 1803 RVA: 0x0001D525 File Offset: 0x0001B725
		public NullValueHandling? ItemNullValueHandling { get; set; }

		/// <summary>
		/// Gets the object's properties.
		/// </summary>
		/// <value>The object's properties.</value>
		// Token: 0x17000115 RID: 277
		// (get) Token: 0x0600070C RID: 1804 RVA: 0x0001D52E File Offset: 0x0001B72E
		[Nullable(1)]
		public JsonPropertyCollection Properties
		{
			[NullableContext(1)]
			get;
		}

		/// <summary>
		/// Gets a collection of <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> instances that define the parameters used with <see cref="P:Newtonsoft.Json.Serialization.JsonObjectContract.OverrideCreator" />.
		/// </summary>
		// Token: 0x17000116 RID: 278
		// (get) Token: 0x0600070D RID: 1805 RVA: 0x0001D536 File Offset: 0x0001B736
		[Nullable(1)]
		public JsonPropertyCollection CreatorParameters
		{
			[NullableContext(1)]
			get
			{
				if (this._creatorParameters == null)
				{
					this._creatorParameters = new JsonPropertyCollection(base.UnderlyingType);
				}
				return this._creatorParameters;
			}
		}

		/// <summary>
		/// Gets or sets the function used to create the object. When set this function will override <see cref="P:Newtonsoft.Json.Serialization.JsonContract.DefaultCreator" />.
		/// This function is called with a collection of arguments which are defined by the <see cref="P:Newtonsoft.Json.Serialization.JsonObjectContract.CreatorParameters" /> collection.
		/// </summary>
		/// <value>The function used to create the object.</value>
		// Token: 0x17000117 RID: 279
		// (get) Token: 0x0600070E RID: 1806 RVA: 0x0001D557 File Offset: 0x0001B757
		// (set) Token: 0x0600070F RID: 1807 RVA: 0x0001D55F File Offset: 0x0001B75F
		[Nullable(new byte[] { 2, 1 })]
		public ObjectConstructor<object> OverrideCreator
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get
			{
				return this._overrideCreator;
			}
			[param: Nullable(new byte[] { 2, 1 })]
			set
			{
				this._overrideCreator = value;
			}
		}

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x06000710 RID: 1808 RVA: 0x0001D568 File Offset: 0x0001B768
		// (set) Token: 0x06000711 RID: 1809 RVA: 0x0001D570 File Offset: 0x0001B770
		[Nullable(new byte[] { 2, 1 })]
		internal ObjectConstructor<object> ParameterizedCreator
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get
			{
				return this._parameterizedCreator;
			}
			[param: Nullable(new byte[] { 2, 1 })]
			set
			{
				this._parameterizedCreator = value;
			}
		}

		/// <summary>
		/// Gets or sets the extension data setter.
		/// </summary>
		// Token: 0x17000119 RID: 281
		// (get) Token: 0x06000712 RID: 1810 RVA: 0x0001D579 File Offset: 0x0001B779
		// (set) Token: 0x06000713 RID: 1811 RVA: 0x0001D581 File Offset: 0x0001B781
		public ExtensionDataSetter ExtensionDataSetter { get; set; }

		/// <summary>
		/// Gets or sets the extension data getter.
		/// </summary>
		// Token: 0x1700011A RID: 282
		// (get) Token: 0x06000714 RID: 1812 RVA: 0x0001D58A File Offset: 0x0001B78A
		// (set) Token: 0x06000715 RID: 1813 RVA: 0x0001D592 File Offset: 0x0001B792
		public ExtensionDataGetter ExtensionDataGetter { get; set; }

		/// <summary>
		/// Gets or sets the extension data value type.
		/// </summary>
		// Token: 0x1700011B RID: 283
		// (get) Token: 0x06000716 RID: 1814 RVA: 0x0001D59B File Offset: 0x0001B79B
		// (set) Token: 0x06000717 RID: 1815 RVA: 0x0001D5A3 File Offset: 0x0001B7A3
		public Type ExtensionDataValueType
		{
			get
			{
				return this._extensionDataValueType;
			}
			set
			{
				this._extensionDataValueType = value;
				this.ExtensionDataIsJToken = value != null && typeof(JToken).IsAssignableFrom(value);
			}
		}

		/// <summary>
		/// Gets or sets the extension data name resolver.
		/// </summary>
		/// <value>The extension data name resolver.</value>
		// Token: 0x1700011C RID: 284
		// (get) Token: 0x06000718 RID: 1816 RVA: 0x0001D5CE File Offset: 0x0001B7CE
		// (set) Token: 0x06000719 RID: 1817 RVA: 0x0001D5D6 File Offset: 0x0001B7D6
		[Nullable(new byte[] { 2, 1, 1 })]
		public Func<string, string> ExtensionDataNameResolver
		{
			[return: Nullable(new byte[] { 2, 1, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1, 1 })]
			set;
		}

		// Token: 0x1700011D RID: 285
		// (get) Token: 0x0600071A RID: 1818 RVA: 0x0001D5E0 File Offset: 0x0001B7E0
		internal bool HasRequiredOrDefaultValueProperties
		{
			get
			{
				if (this._hasRequiredOrDefaultValueProperties == null)
				{
					this._hasRequiredOrDefaultValueProperties = new bool?(false);
					if (this.ItemRequired.GetValueOrDefault(Required.Default) != Required.Default)
					{
						this._hasRequiredOrDefaultValueProperties = new bool?(true);
					}
					else
					{
						foreach (JsonProperty jsonProperty in this.Properties)
						{
							if (jsonProperty.Required == Required.Default)
							{
								DefaultValueHandling? defaultValueHandling = jsonProperty.DefaultValueHandling & DefaultValueHandling.Populate;
								DefaultValueHandling defaultValueHandling2 = DefaultValueHandling.Populate;
								if (!((defaultValueHandling.GetValueOrDefault() == defaultValueHandling2) & (defaultValueHandling != null)))
								{
									continue;
								}
							}
							this._hasRequiredOrDefaultValueProperties = new bool?(true);
							break;
						}
					}
				}
				return this._hasRequiredOrDefaultValueProperties.GetValueOrDefault();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonObjectContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x0600071B RID: 1819 RVA: 0x0001D6CC File Offset: 0x0001B8CC
		[NullableContext(1)]
		public JsonObjectContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.Object;
			this.Properties = new JsonPropertyCollection(base.UnderlyingType);
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x0001D6ED File Offset: 0x0001B8ED
		[NullableContext(1)]
		[SecuritySafeCritical]
		internal object GetUninitializedObject()
		{
			if (!JsonTypeReflector.FullyTrusted)
			{
				throw new JsonException("Insufficient permissions. Creating an uninitialized '{0}' type requires full trust.".FormatWith(CultureInfo.InvariantCulture, this.NonNullableUnderlyingType));
			}
			return FormatterServices.GetUninitializedObject(this.NonNullableUnderlyingType);
		}

		// Token: 0x0400029B RID: 667
		internal bool ExtensionDataIsJToken;

		// Token: 0x0400029C RID: 668
		private bool? _hasRequiredOrDefaultValueProperties;

		// Token: 0x0400029D RID: 669
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _overrideCreator;

		// Token: 0x0400029E RID: 670
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _parameterizedCreator;

		// Token: 0x0400029F RID: 671
		private JsonPropertyCollection _creatorParameters;

		// Token: 0x040002A0 RID: 672
		private Type _extensionDataValueType;
	}
}
