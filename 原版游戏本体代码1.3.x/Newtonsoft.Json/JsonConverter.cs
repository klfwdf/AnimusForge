using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Converts an object to and from JSON.
	/// </summary>
	// Token: 0x0200001A RID: 26
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		// Token: 0x06000081 RID: 129
		public abstract void WriteJson(JsonWriter writer, [Nullable(2)] object value, JsonSerializer serializer);

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>The object value.</returns>
		// Token: 0x06000082 RID: 130
		[return: Nullable(2)]
		public abstract object ReadJson(JsonReader reader, Type objectType, [Nullable(2)] object existingValue, JsonSerializer serializer);

		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000083 RID: 131
		public abstract bool CanConvert(Type objectType);

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.
		/// </summary>
		/// <value><c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON; otherwise, <c>false</c>.</value>
		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000084 RID: 132 RVA: 0x00002E6F File Offset: 0x0000106F
		public virtual bool CanRead
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.
		/// </summary>
		/// <value><c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON; otherwise, <c>false</c>.</value>
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000085 RID: 133 RVA: 0x00002E72 File Offset: 0x00001072
		public virtual bool CanWrite
		{
			get
			{
				return true;
			}
		}
	}
}
