using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Converts an object to and from JSON.
	/// </summary>
	/// <typeparam name="T">The object type to convert.</typeparam>
	// Token: 0x0200001B RID: 27
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class JsonConverter<[Nullable(2)] T> : JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		// Token: 0x06000087 RID: 135 RVA: 0x00002E80 File Offset: 0x00001080
		public sealed override void WriteJson(JsonWriter writer, [Nullable(2)] object value, JsonSerializer serializer)
		{
			if (!((value != null) ? (value is T) : ReflectionUtils.IsNullable(typeof(T))))
			{
				throw new JsonSerializationException("Converter cannot write specified value to JSON. {0} is required.".FormatWith(CultureInfo.InvariantCulture, typeof(T)));
			}
			this.WriteJson(writer, (T)((object)value), serializer);
		}

		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		// Token: 0x06000088 RID: 136
		public abstract void WriteJson(JsonWriter writer, [Nullable(2)] T value, JsonSerializer serializer);

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>The object value.</returns>
		// Token: 0x06000089 RID: 137 RVA: 0x00002EDC File Offset: 0x000010DC
		[return: Nullable(2)]
		public sealed override object ReadJson(JsonReader reader, Type objectType, [Nullable(2)] object existingValue, JsonSerializer serializer)
		{
			bool flag = existingValue == null;
			if (!flag && !(existingValue is T))
			{
				throw new JsonSerializationException("Converter cannot read JSON with the specified existing value. {0} is required.".FormatWith(CultureInfo.InvariantCulture, typeof(T)));
			}
			return this.ReadJson(reader, objectType, flag ? default(T) : ((T)((object)existingValue)), !flag, serializer);
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
		/// <param name="hasExistingValue">The existing value has a value.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>The object value.</returns>
		// Token: 0x0600008A RID: 138
		[return: Nullable(2)]
		public abstract T ReadJson(JsonReader reader, Type objectType, [Nullable(2)] T existingValue, bool hasExistingValue, JsonSerializer serializer);

		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x0600008B RID: 139 RVA: 0x00002F3F File Offset: 0x0000113F
		public sealed override bool CanConvert(Type objectType)
		{
			return typeof(T).IsAssignableFrom(objectType);
		}
	}
}
