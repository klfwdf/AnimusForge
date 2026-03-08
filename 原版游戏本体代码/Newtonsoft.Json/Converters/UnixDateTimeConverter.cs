using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Converters
{
	/// <summary>
	/// Converts a <see cref="T:System.DateTime" /> to and from Unix epoch time
	/// </summary>
	// Token: 0x020000EE RID: 238
	[NullableContext(1)]
	[Nullable(0)]
	public class UnixDateTimeConverter : DateTimeConverterBase
	{
		/// <summary>
		/// Gets or sets a value indicating whether the dates before Unix epoch
		/// should converted to and from JSON.
		/// </summary>
		/// <value>
		/// <c>true</c> to allow converting dates before Unix epoch to and from JSON;
		/// <c>false</c> to throw an exception when a date being converted to or from JSON
		/// occurred before Unix epoch. The default value is <c>false</c>.
		/// </value>
		// Token: 0x1700021A RID: 538
		// (get) Token: 0x06000C96 RID: 3222 RVA: 0x000330CC File Offset: 0x000312CC
		// (set) Token: 0x06000C97 RID: 3223 RVA: 0x000330D4 File Offset: 0x000312D4
		public bool AllowPreEpoch { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.UnixDateTimeConverter" /> class.
		/// </summary>
		// Token: 0x06000C98 RID: 3224 RVA: 0x000330DD File Offset: 0x000312DD
		public UnixDateTimeConverter()
			: this(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.UnixDateTimeConverter" /> class.
		/// </summary>
		/// <param name="allowPreEpoch">
		/// <c>true</c> to allow converting dates before Unix epoch to and from JSON;
		/// <c>false</c> to throw an exception when a date being converted to or from JSON
		/// occurred before Unix epoch. The default value is <c>false</c>.
		/// </param>
		// Token: 0x06000C99 RID: 3225 RVA: 0x000330E6 File Offset: 0x000312E6
		public UnixDateTimeConverter(bool allowPreEpoch)
		{
			this.AllowPreEpoch = allowPreEpoch;
		}

		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		// Token: 0x06000C9A RID: 3226 RVA: 0x000330F8 File Offset: 0x000312F8
		public override void WriteJson(JsonWriter writer, [Nullable(2)] object value, JsonSerializer serializer)
		{
			long num;
			if (value is DateTime)
			{
				num = (long)(((DateTime)value).ToUniversalTime() - UnixDateTimeConverter.UnixEpoch).TotalSeconds;
			}
			else
			{
				if (!(value is DateTimeOffset))
				{
					throw new JsonSerializationException("Expected date object value.");
				}
				num = (long)(((DateTimeOffset)value).ToUniversalTime() - UnixDateTimeConverter.UnixEpoch).TotalSeconds;
			}
			if (!this.AllowPreEpoch && num < 0L)
			{
				throw new JsonSerializationException("Cannot convert date value that is before Unix epoch of 00:00:00 UTC on 1 January 1970.");
			}
			writer.WriteValue(num);
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>The object value.</returns>
		// Token: 0x06000C9B RID: 3227 RVA: 0x0003318C File Offset: 0x0003138C
		[return: Nullable(2)]
		public override object ReadJson(JsonReader reader, Type objectType, [Nullable(2)] object existingValue, JsonSerializer serializer)
		{
			bool flag = ReflectionUtils.IsNullable(objectType);
			if (reader.TokenType == JsonToken.Null)
			{
				if (!flag)
				{
					throw JsonSerializationException.Create(reader, "Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
				}
				return null;
			}
			else
			{
				long num;
				if (reader.TokenType == JsonToken.Integer)
				{
					num = (long)reader.Value;
				}
				else
				{
					if (reader.TokenType != JsonToken.String)
					{
						throw JsonSerializationException.Create(reader, "Unexpected token parsing date. Expected Integer or String, got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
					}
					if (!long.TryParse((string)reader.Value, out num))
					{
						throw JsonSerializationException.Create(reader, "Cannot convert invalid value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
					}
				}
				if (!this.AllowPreEpoch && num < 0L)
				{
					throw JsonSerializationException.Create(reader, "Cannot convert value that is before Unix epoch of 00:00:00 UTC on 1 January 1970 to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
				}
				DateTime dateTime = UnixDateTimeConverter.UnixEpoch.AddSeconds((double)num);
				if ((flag ? Nullable.GetUnderlyingType(objectType) : objectType) == typeof(DateTimeOffset))
				{
					return new DateTimeOffset(dateTime, TimeSpan.Zero);
				}
				return dateTime;
			}
		}

		// Token: 0x04000404 RID: 1028
		internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}
}
