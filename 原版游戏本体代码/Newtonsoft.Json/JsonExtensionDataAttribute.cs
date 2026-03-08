using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Instructs the <see cref="T:Newtonsoft.Json.JsonSerializer" /> to deserialize properties with no matching class member into the specified collection
	/// and write values during serialization.
	/// </summary>
	// Token: 0x02000020 RID: 32
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class JsonExtensionDataAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets a value that indicates whether to write extension data when serializing the object.
		/// </summary>
		/// <value>
		/// 	<c>true</c> to write extension data when serializing the object; otherwise, <c>false</c>. The default is <c>true</c>.
		/// </value>
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000099 RID: 153 RVA: 0x00002FEC File Offset: 0x000011EC
		// (set) Token: 0x0600009A RID: 154 RVA: 0x00002FF4 File Offset: 0x000011F4
		public bool WriteData { get; set; }

		/// <summary>
		/// Gets or sets a value that indicates whether to read extension data when deserializing the object.
		/// </summary>
		/// <value>
		/// 	<c>true</c> to read extension data when deserializing the object; otherwise, <c>false</c>. The default is <c>true</c>.
		/// </value>
		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600009B RID: 155 RVA: 0x00002FFD File Offset: 0x000011FD
		// (set) Token: 0x0600009C RID: 156 RVA: 0x00003005 File Offset: 0x00001205
		public bool ReadData { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonExtensionDataAttribute" /> class.
		/// </summary>
		// Token: 0x0600009D RID: 157 RVA: 0x0000300E File Offset: 0x0000120E
		public JsonExtensionDataAttribute()
		{
			this.WriteData = true;
			this.ReadData = true;
		}
	}
}
