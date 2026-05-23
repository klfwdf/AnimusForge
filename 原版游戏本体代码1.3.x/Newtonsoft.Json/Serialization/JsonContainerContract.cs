using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000085 RID: 133
	[NullableContext(2)]
	[Nullable(0)]
	public class JsonContainerContract : JsonContract
	{
		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x0600069F RID: 1695 RVA: 0x0001C8DA File Offset: 0x0001AADA
		// (set) Token: 0x060006A0 RID: 1696 RVA: 0x0001C8E2 File Offset: 0x0001AAE2
		internal JsonContract ItemContract
		{
			get
			{
				return this._itemContract;
			}
			set
			{
				this._itemContract = value;
				if (this._itemContract != null)
				{
					this._finalItemContract = (this._itemContract.UnderlyingType.IsSealed() ? this._itemContract : null);
					return;
				}
				this._finalItemContract = null;
			}
		}

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x060006A1 RID: 1697 RVA: 0x0001C91C File Offset: 0x0001AB1C
		internal JsonContract FinalItemContract
		{
			get
			{
				return this._finalItemContract;
			}
		}

		/// <summary>
		/// Gets or sets the default collection items <see cref="T:Newtonsoft.Json.JsonConverter" />.
		/// </summary>
		/// <value>The converter.</value>
		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x060006A2 RID: 1698 RVA: 0x0001C924 File Offset: 0x0001AB24
		// (set) Token: 0x060006A3 RID: 1699 RVA: 0x0001C92C File Offset: 0x0001AB2C
		public JsonConverter ItemConverter { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the collection items preserve object references.
		/// </summary>
		/// <value><c>true</c> if collection items preserve object references; otherwise, <c>false</c>.</value>
		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x060006A4 RID: 1700 RVA: 0x0001C935 File Offset: 0x0001AB35
		// (set) Token: 0x060006A5 RID: 1701 RVA: 0x0001C93D File Offset: 0x0001AB3D
		public bool? ItemIsReference { get; set; }

		/// <summary>
		/// Gets or sets the collection item reference loop handling.
		/// </summary>
		/// <value>The reference loop handling.</value>
		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x060006A6 RID: 1702 RVA: 0x0001C946 File Offset: 0x0001AB46
		// (set) Token: 0x060006A7 RID: 1703 RVA: 0x0001C94E File Offset: 0x0001AB4E
		public ReferenceLoopHandling? ItemReferenceLoopHandling { get; set; }

		/// <summary>
		/// Gets or sets the collection item type name handling.
		/// </summary>
		/// <value>The type name handling.</value>
		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x060006A8 RID: 1704 RVA: 0x0001C957 File Offset: 0x0001AB57
		// (set) Token: 0x060006A9 RID: 1705 RVA: 0x0001C95F File Offset: 0x0001AB5F
		public TypeNameHandling? ItemTypeNameHandling { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonContainerContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x060006AA RID: 1706 RVA: 0x0001C968 File Offset: 0x0001AB68
		[NullableContext(1)]
		internal JsonContainerContract(Type underlyingType)
			: base(underlyingType)
		{
			JsonContainerAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>(underlyingType);
			if (cachedAttribute != null)
			{
				if (cachedAttribute.ItemConverterType != null)
				{
					this.ItemConverter = JsonTypeReflector.CreateJsonConverterInstance(cachedAttribute.ItemConverterType, cachedAttribute.ItemConverterParameters);
				}
				this.ItemIsReference = cachedAttribute._itemIsReference;
				this.ItemReferenceLoopHandling = cachedAttribute._itemReferenceLoopHandling;
				this.ItemTypeNameHandling = cachedAttribute._itemTypeNameHandling;
			}
		}

		// Token: 0x04000259 RID: 601
		private JsonContract _itemContract;

		// Token: 0x0400025A RID: 602
		private JsonContract _finalItemContract;
	}
}
