using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Converters
{
	/// <summary>
	/// Converts an <see cref="T:System.Enum" /> to and from its name string value.
	/// </summary>
	// Token: 0x020000ED RID: 237
	[NullableContext(1)]
	[Nullable(0)]
	public class StringEnumConverter : JsonConverter
	{
		/// <summary>
		/// Gets or sets a value indicating whether the written enum text should be camel case.
		/// The default value is <c>false</c>.
		/// </summary>
		/// <value><c>true</c> if the written enum text will be camel case; otherwise, <c>false</c>.</value>
		// Token: 0x17000217 RID: 535
		// (get) Token: 0x06000C87 RID: 3207 RVA: 0x00032DDD File Offset: 0x00030FDD
		// (set) Token: 0x06000C88 RID: 3208 RVA: 0x00032DEF File Offset: 0x00030FEF
		[Obsolete("StringEnumConverter.CamelCaseText is obsolete. Set StringEnumConverter.NamingStrategy with CamelCaseNamingStrategy instead.")]
		public bool CamelCaseText
		{
			get
			{
				return this.NamingStrategy is CamelCaseNamingStrategy;
			}
			set
			{
				if (value)
				{
					if (this.NamingStrategy is CamelCaseNamingStrategy)
					{
						return;
					}
					this.NamingStrategy = new CamelCaseNamingStrategy();
					return;
				}
				else
				{
					if (!(this.NamingStrategy is CamelCaseNamingStrategy))
					{
						return;
					}
					this.NamingStrategy = null;
					return;
				}
			}
		}

		/// <summary>
		/// Gets or sets the naming strategy used to resolve how enum text is written.
		/// </summary>
		/// <value>The naming strategy used to resolve how enum text is written.</value>
		// Token: 0x17000218 RID: 536
		// (get) Token: 0x06000C89 RID: 3209 RVA: 0x00032E23 File Offset: 0x00031023
		// (set) Token: 0x06000C8A RID: 3210 RVA: 0x00032E2B File Offset: 0x0003102B
		[Nullable(2)]
		public NamingStrategy NamingStrategy
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether integer values are allowed when serializing and deserializing.
		/// The default value is <c>true</c>.
		/// </summary>
		/// <value><c>true</c> if integers are allowed when serializing and deserializing; otherwise, <c>false</c>.</value>
		// Token: 0x17000219 RID: 537
		// (get) Token: 0x06000C8B RID: 3211 RVA: 0x00032E34 File Offset: 0x00031034
		// (set) Token: 0x06000C8C RID: 3212 RVA: 0x00032E3C File Offset: 0x0003103C
		public bool AllowIntegerValues { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.StringEnumConverter" /> class.
		/// </summary>
		// Token: 0x06000C8D RID: 3213 RVA: 0x00032E45 File Offset: 0x00031045
		public StringEnumConverter()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.StringEnumConverter" /> class.
		/// </summary>
		/// <param name="camelCaseText"><c>true</c> if the written enum text will be camel case; otherwise, <c>false</c>.</param>
		// Token: 0x06000C8E RID: 3214 RVA: 0x00032E54 File Offset: 0x00031054
		[Obsolete("StringEnumConverter(bool) is obsolete. Create a converter with StringEnumConverter(NamingStrategy, bool) instead.")]
		public StringEnumConverter(bool camelCaseText)
		{
			if (camelCaseText)
			{
				this.NamingStrategy = new CamelCaseNamingStrategy();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.StringEnumConverter" /> class.
		/// </summary>
		/// <param name="namingStrategy">The naming strategy used to resolve how enum text is written.</param>
		/// <param name="allowIntegerValues"><c>true</c> if integers are allowed when serializing and deserializing; otherwise, <c>false</c>.</param>
		// Token: 0x06000C8F RID: 3215 RVA: 0x00032E71 File Offset: 0x00031071
		public StringEnumConverter(NamingStrategy namingStrategy, bool allowIntegerValues = true)
		{
			this.NamingStrategy = namingStrategy;
			this.AllowIntegerValues = allowIntegerValues;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.StringEnumConverter" /> class.
		/// </summary>
		/// <param name="namingStrategyType">The <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> used to write enum text.</param>
		// Token: 0x06000C90 RID: 3216 RVA: 0x00032E8E File Offset: 0x0003108E
		public StringEnumConverter(Type namingStrategyType)
		{
			ValidationUtils.ArgumentNotNull(namingStrategyType, "namingStrategyType");
			this.NamingStrategy = JsonTypeReflector.CreateNamingStrategyInstance(namingStrategyType, null);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.StringEnumConverter" /> class.
		/// </summary>
		/// <param name="namingStrategyType">The <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> used to write enum text.</param>
		/// <param name="namingStrategyParameters">
		/// The parameter list to use when constructing the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> described by <paramref name="namingStrategyType" />.
		/// If <c>null</c>, the default constructor is used.
		/// When non-<c>null</c>, there must be a constructor defined in the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> that exactly matches the number,
		/// order, and type of these parameters.
		/// </param>
		// Token: 0x06000C91 RID: 3217 RVA: 0x00032EB5 File Offset: 0x000310B5
		public StringEnumConverter(Type namingStrategyType, object[] namingStrategyParameters)
		{
			ValidationUtils.ArgumentNotNull(namingStrategyType, "namingStrategyType");
			this.NamingStrategy = JsonTypeReflector.CreateNamingStrategyInstance(namingStrategyType, namingStrategyParameters);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Converters.StringEnumConverter" /> class.
		/// </summary>
		/// <param name="namingStrategyType">The <see cref="T:System.Type" /> of the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> used to write enum text.</param>
		/// <param name="namingStrategyParameters">
		/// The parameter list to use when constructing the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> described by <paramref name="namingStrategyType" />.
		/// If <c>null</c>, the default constructor is used.
		/// When non-<c>null</c>, there must be a constructor defined in the <see cref="T:Newtonsoft.Json.Serialization.NamingStrategy" /> that exactly matches the number,
		/// order, and type of these parameters.
		/// </param>
		/// <param name="allowIntegerValues"><c>true</c> if integers are allowed when serializing and deserializing; otherwise, <c>false</c>.</param>
		// Token: 0x06000C92 RID: 3218 RVA: 0x00032EDC File Offset: 0x000310DC
		public StringEnumConverter(Type namingStrategyType, object[] namingStrategyParameters, bool allowIntegerValues)
		{
			ValidationUtils.ArgumentNotNull(namingStrategyType, "namingStrategyType");
			this.NamingStrategy = JsonTypeReflector.CreateNamingStrategyInstance(namingStrategyType, namingStrategyParameters);
			this.AllowIntegerValues = allowIntegerValues;
		}

		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		// Token: 0x06000C93 RID: 3219 RVA: 0x00032F0C File Offset: 0x0003110C
		public override void WriteJson(JsonWriter writer, [Nullable(2)] object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			Enum @enum = (Enum)value;
			string value2;
			if (EnumUtils.TryToString(@enum.GetType(), value, this.NamingStrategy, out value2))
			{
				writer.WriteValue(value2);
				return;
			}
			if (!this.AllowIntegerValues)
			{
				throw JsonSerializationException.Create(null, writer.ContainerPath, "Integer value {0} is not allowed.".FormatWith(CultureInfo.InvariantCulture, @enum.ToString("D")), null);
			}
			writer.WriteValue(value);
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>The object value.</returns>
		// Token: 0x06000C94 RID: 3220 RVA: 0x00032F80 File Offset: 0x00031180
		[return: Nullable(2)]
		public override object ReadJson(JsonReader reader, Type objectType, [Nullable(2)] object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType != JsonToken.Null)
			{
				bool flag = ReflectionUtils.IsNullableType(objectType);
				Type type = (flag ? Nullable.GetUnderlyingType(objectType) : objectType);
				try
				{
					if (reader.TokenType == JsonToken.String)
					{
						object value = reader.Value;
						string value2 = ((value != null) ? value.ToString() : null);
						if (StringUtils.IsNullOrEmpty(value2) && flag)
						{
							return null;
						}
						return EnumUtils.ParseEnum(type, this.NamingStrategy, value2, !this.AllowIntegerValues);
					}
					else if (reader.TokenType == JsonToken.Integer)
					{
						if (!this.AllowIntegerValues)
						{
							throw JsonSerializationException.Create(reader, "Integer value {0} is not allowed.".FormatWith(CultureInfo.InvariantCulture, reader.Value));
						}
						return ConvertUtils.ConvertOrCast(reader.Value, CultureInfo.InvariantCulture, type);
					}
				}
				catch (Exception ex)
				{
					throw JsonSerializationException.Create(reader, "Error converting value {0} to type '{1}'.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.ToString(reader.Value), objectType), ex);
				}
				throw JsonSerializationException.Create(reader, "Unexpected token {0} when parsing enum.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
			}
			if (!ReflectionUtils.IsNullableType(objectType))
			{
				throw JsonSerializationException.Create(reader, "Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
			}
			return null;
		}

		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000C95 RID: 3221 RVA: 0x000330B4 File Offset: 0x000312B4
		public override bool CanConvert(Type objectType)
		{
			return (ReflectionUtils.IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType).IsEnum();
		}
	}
}
