using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Instructs the <see cref="T:Newtonsoft.Json.JsonSerializer" /> how to serialize the object.
	/// </summary>
	// Token: 0x02000023 RID: 35
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
	public sealed class JsonObjectAttribute : JsonContainerAttribute
	{
		/// <summary>
		/// Gets or sets the member serialization.
		/// </summary>
		/// <value>The member serialization.</value>
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060000A1 RID: 161 RVA: 0x00003034 File Offset: 0x00001234
		// (set) Token: 0x060000A2 RID: 162 RVA: 0x0000303C File Offset: 0x0000123C
		public MemberSerialization MemberSerialization
		{
			get
			{
				return this._memberSerialization;
			}
			set
			{
				this._memberSerialization = value;
			}
		}

		/// <summary>
		/// Gets or sets the missing member handling used when deserializing this object.
		/// </summary>
		/// <value>The missing member handling.</value>
		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060000A3 RID: 163 RVA: 0x00003045 File Offset: 0x00001245
		// (set) Token: 0x060000A4 RID: 164 RVA: 0x00003052 File Offset: 0x00001252
		public MissingMemberHandling MissingMemberHandling
		{
			get
			{
				return this._missingMemberHandling.GetValueOrDefault();
			}
			set
			{
				this._missingMemberHandling = new MissingMemberHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how the object's properties with null values are handled during serialization and deserialization.
		/// </summary>
		/// <value>How the object's properties with null values are handled during serialization and deserialization.</value>
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060000A5 RID: 165 RVA: 0x00003060 File Offset: 0x00001260
		// (set) Token: 0x060000A6 RID: 166 RVA: 0x0000306D File Offset: 0x0000126D
		public NullValueHandling ItemNullValueHandling
		{
			get
			{
				return this._itemNullValueHandling.GetValueOrDefault();
			}
			set
			{
				this._itemNullValueHandling = new NullValueHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the object's properties are required.
		/// </summary>
		/// <value>
		/// 	A value indicating whether the object's properties are required.
		/// </value>
		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060000A7 RID: 167 RVA: 0x0000307B File Offset: 0x0000127B
		// (set) Token: 0x060000A8 RID: 168 RVA: 0x00003088 File Offset: 0x00001288
		public Required ItemRequired
		{
			get
			{
				return this._itemRequired.GetValueOrDefault();
			}
			set
			{
				this._itemRequired = new Required?(value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonObjectAttribute" /> class.
		/// </summary>
		// Token: 0x060000A9 RID: 169 RVA: 0x00003096 File Offset: 0x00001296
		public JsonObjectAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonObjectAttribute" /> class with the specified member serialization.
		/// </summary>
		/// <param name="memberSerialization">The member serialization.</param>
		// Token: 0x060000AA RID: 170 RVA: 0x0000309E File Offset: 0x0000129E
		public JsonObjectAttribute(MemberSerialization memberSerialization)
		{
			this.MemberSerialization = memberSerialization;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonObjectAttribute" /> class with the specified container Id.
		/// </summary>
		/// <param name="id">The container Id.</param>
		// Token: 0x060000AB RID: 171 RVA: 0x000030AD File Offset: 0x000012AD
		[NullableContext(1)]
		public JsonObjectAttribute(string id)
			: base(id)
		{
		}

		// Token: 0x04000040 RID: 64
		private MemberSerialization _memberSerialization;

		// Token: 0x04000041 RID: 65
		internal MissingMemberHandling? _missingMemberHandling;

		// Token: 0x04000042 RID: 66
		internal Required? _itemRequired;

		// Token: 0x04000043 RID: 67
		internal NullValueHandling? _itemNullValueHandling;
	}
}
