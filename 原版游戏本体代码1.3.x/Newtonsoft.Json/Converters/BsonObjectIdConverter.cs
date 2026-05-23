using System;
using System.Globalization;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Converters
{
	/// <summary>
	/// Converts a <see cref="T:Newtonsoft.Json.Bson.BsonObjectId" /> to and from JSON and BSON.
	/// </summary>
	// Token: 0x020000E1 RID: 225
	[Obsolete("BSON reading and writing has been moved to its own package. See https://www.nuget.org/packages/Newtonsoft.Json.Bson for more details.")]
	public class BsonObjectIdConverter : JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		// Token: 0x06000C40 RID: 3136 RVA: 0x00031440 File Offset: 0x0002F640
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			BsonObjectId bsonObjectId = (BsonObjectId)value;
			BsonWriter bsonWriter = writer as BsonWriter;
			if (bsonWriter != null)
			{
				bsonWriter.WriteObjectId(bsonObjectId.Value);
				return;
			}
			writer.WriteValue(bsonObjectId.Value);
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>The object value.</returns>
		// Token: 0x06000C41 RID: 3137 RVA: 0x00031477 File Offset: 0x0002F677
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType != JsonToken.Bytes)
			{
				throw new JsonSerializationException("Expected Bytes but got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
			}
			return new BsonObjectId((byte[])reader.Value);
		}

		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000C42 RID: 3138 RVA: 0x000314B3 File Offset: 0x0002F6B3
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(BsonObjectId);
		}
	}
}
