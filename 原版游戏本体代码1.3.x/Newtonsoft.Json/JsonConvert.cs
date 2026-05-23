using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Provides methods for converting between .NET types and JSON types.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="SerializeObject" title="Serializing and Deserializing JSON with JsonConvert" />
	/// </example>
	// Token: 0x02000019 RID: 25
	[NullableContext(1)]
	[Nullable(0)]
	public static class JsonConvert
	{
		/// <summary>
		/// Gets or sets a function that creates default <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// Default settings are automatically used by serialization methods on <see cref="T:Newtonsoft.Json.JsonConvert" />,
		/// and <see cref="M:Newtonsoft.Json.Linq.JToken.ToObject``1" /> and <see cref="M:Newtonsoft.Json.Linq.JToken.FromObject(System.Object)" /> on <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// To serialize without using any default settings create a <see cref="T:Newtonsoft.Json.JsonSerializer" /> with
		/// <see cref="M:Newtonsoft.Json.JsonSerializer.Create" />.
		/// </summary>
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000039 RID: 57 RVA: 0x000024C4 File Offset: 0x000006C4
		// (set) Token: 0x0600003A RID: 58 RVA: 0x000024CB File Offset: 0x000006CB
		[Nullable(new byte[] { 2, 1 })]
		public static Func<JsonSerializerSettings> DefaultSettings
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Converts the <see cref="T:System.DateTime" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.DateTime" />.</returns>
		// Token: 0x0600003B RID: 59 RVA: 0x000024D3 File Offset: 0x000006D3
		public static string ToString(DateTime value)
		{
			return JsonConvert.ToString(value, DateFormatHandling.IsoDateFormat, DateTimeZoneHandling.RoundtripKind);
		}

		/// <summary>
		/// Converts the <see cref="T:System.DateTime" /> to its JSON string representation using the <see cref="T:Newtonsoft.Json.DateFormatHandling" /> specified.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="format">The format the date will be converted to.</param>
		/// <param name="timeZoneHandling">The time zone handling when the date is converted to a string.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.DateTime" />.</returns>
		// Token: 0x0600003C RID: 60 RVA: 0x000024E0 File Offset: 0x000006E0
		public static string ToString(DateTime value, DateFormatHandling format, DateTimeZoneHandling timeZoneHandling)
		{
			DateTime value2 = DateTimeUtils.EnsureDateTime(value, timeZoneHandling);
			string result;
			using (StringWriter stringWriter = StringUtils.CreateStringWriter(64))
			{
				stringWriter.Write('"');
				DateTimeUtils.WriteDateTimeString(stringWriter, value2, format, null, CultureInfo.InvariantCulture);
				stringWriter.Write('"');
				result = stringWriter.ToString();
			}
			return result;
		}

		/// <summary>
		/// Converts the <see cref="T:System.DateTimeOffset" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.DateTimeOffset" />.</returns>
		// Token: 0x0600003D RID: 61 RVA: 0x00002540 File Offset: 0x00000740
		public static string ToString(DateTimeOffset value)
		{
			return JsonConvert.ToString(value, DateFormatHandling.IsoDateFormat);
		}

		/// <summary>
		/// Converts the <see cref="T:System.DateTimeOffset" /> to its JSON string representation using the <see cref="T:Newtonsoft.Json.DateFormatHandling" /> specified.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="format">The format the date will be converted to.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.DateTimeOffset" />.</returns>
		// Token: 0x0600003E RID: 62 RVA: 0x0000254C File Offset: 0x0000074C
		public static string ToString(DateTimeOffset value, DateFormatHandling format)
		{
			string result;
			using (StringWriter stringWriter = StringUtils.CreateStringWriter(64))
			{
				stringWriter.Write('"');
				DateTimeUtils.WriteDateTimeOffsetString(stringWriter, value, format, null, CultureInfo.InvariantCulture);
				stringWriter.Write('"');
				result = stringWriter.ToString();
			}
			return result;
		}

		/// <summary>
		/// Converts the <see cref="T:System.Boolean" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Boolean" />.</returns>
		// Token: 0x0600003F RID: 63 RVA: 0x000025A4 File Offset: 0x000007A4
		public static string ToString(bool value)
		{
			if (!value)
			{
				return JsonConvert.False;
			}
			return JsonConvert.True;
		}

		/// <summary>
		/// Converts the <see cref="T:System.Char" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Char" />.</returns>
		// Token: 0x06000040 RID: 64 RVA: 0x000025B4 File Offset: 0x000007B4
		public static string ToString(char value)
		{
			return JsonConvert.ToString(char.ToString(value));
		}

		/// <summary>
		/// Converts the <see cref="T:System.Enum" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Enum" />.</returns>
		// Token: 0x06000041 RID: 65 RVA: 0x000025C1 File Offset: 0x000007C1
		public static string ToString(Enum value)
		{
			return value.ToString("D");
		}

		/// <summary>
		/// Converts the <see cref="T:System.Int32" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Int32" />.</returns>
		// Token: 0x06000042 RID: 66 RVA: 0x000025CE File Offset: 0x000007CE
		public static string ToString(int value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.Int16" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Int16" />.</returns>
		// Token: 0x06000043 RID: 67 RVA: 0x000025DD File Offset: 0x000007DD
		public static string ToString(short value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.UInt16" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.UInt16" />.</returns>
		// Token: 0x06000044 RID: 68 RVA: 0x000025EC File Offset: 0x000007EC
		[CLSCompliant(false)]
		public static string ToString(ushort value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.UInt32" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.UInt32" />.</returns>
		// Token: 0x06000045 RID: 69 RVA: 0x000025FB File Offset: 0x000007FB
		[CLSCompliant(false)]
		public static string ToString(uint value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.Int64" />  to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Int64" />.</returns>
		// Token: 0x06000046 RID: 70 RVA: 0x0000260A File Offset: 0x0000080A
		public static string ToString(long value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00002619 File Offset: 0x00000819
		private static string ToStringInternal(BigInteger value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.UInt64" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.UInt64" />.</returns>
		// Token: 0x06000048 RID: 72 RVA: 0x00002628 File Offset: 0x00000828
		[CLSCompliant(false)]
		public static string ToString(ulong value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.Single" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Single" />.</returns>
		// Token: 0x06000049 RID: 73 RVA: 0x00002637 File Offset: 0x00000837
		public static string ToString(float value)
		{
			return JsonConvert.EnsureDecimalPlace((double)value, value.ToString("R", CultureInfo.InvariantCulture));
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00002651 File Offset: 0x00000851
		internal static string ToString(float value, FloatFormatHandling floatFormatHandling, char quoteChar, bool nullable)
		{
			return JsonConvert.EnsureFloatFormat((double)value, JsonConvert.EnsureDecimalPlace((double)value, value.ToString("R", CultureInfo.InvariantCulture)), floatFormatHandling, quoteChar, nullable);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00002675 File Offset: 0x00000875
		private static string EnsureFloatFormat(double value, string text, FloatFormatHandling floatFormatHandling, char quoteChar, bool nullable)
		{
			if (floatFormatHandling == FloatFormatHandling.Symbol || (!double.IsInfinity(value) && !double.IsNaN(value)))
			{
				return text;
			}
			if (floatFormatHandling != FloatFormatHandling.DefaultValue)
			{
				return quoteChar.ToString() + text + quoteChar.ToString();
			}
			if (nullable)
			{
				return JsonConvert.Null;
			}
			return "0.0";
		}

		/// <summary>
		/// Converts the <see cref="T:System.Double" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Double" />.</returns>
		// Token: 0x0600004C RID: 76 RVA: 0x000026B5 File Offset: 0x000008B5
		public static string ToString(double value)
		{
			return JsonConvert.EnsureDecimalPlace(value, value.ToString("R", CultureInfo.InvariantCulture));
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000026CE File Offset: 0x000008CE
		internal static string ToString(double value, FloatFormatHandling floatFormatHandling, char quoteChar, bool nullable)
		{
			return JsonConvert.EnsureFloatFormat(value, JsonConvert.EnsureDecimalPlace(value, value.ToString("R", CultureInfo.InvariantCulture)), floatFormatHandling, quoteChar, nullable);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x000026F0 File Offset: 0x000008F0
		private static string EnsureDecimalPlace(double value, string text)
		{
			if (double.IsNaN(value) || double.IsInfinity(value) || StringUtils.IndexOf(text, '.') != -1 || StringUtils.IndexOf(text, 'E') != -1 || StringUtils.IndexOf(text, 'e') != -1)
			{
				return text;
			}
			return text + ".0";
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00002730 File Offset: 0x00000930
		private static string EnsureDecimalPlace(string text)
		{
			if (StringUtils.IndexOf(text, '.') != -1)
			{
				return text;
			}
			return text + ".0";
		}

		/// <summary>
		/// Converts the <see cref="T:System.Byte" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Byte" />.</returns>
		// Token: 0x06000050 RID: 80 RVA: 0x0000274A File Offset: 0x0000094A
		public static string ToString(byte value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.SByte" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.SByte" />.</returns>
		// Token: 0x06000051 RID: 81 RVA: 0x00002759 File Offset: 0x00000959
		[CLSCompliant(false)]
		public static string ToString(sbyte value)
		{
			return value.ToString(null, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the <see cref="T:System.Decimal" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Decimal" />.</returns>
		// Token: 0x06000052 RID: 82 RVA: 0x00002768 File Offset: 0x00000968
		public static string ToString(decimal value)
		{
			return JsonConvert.EnsureDecimalPlace(value.ToString(null, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Converts the <see cref="T:System.Guid" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Guid" />.</returns>
		// Token: 0x06000053 RID: 83 RVA: 0x0000277C File Offset: 0x0000097C
		public static string ToString(Guid value)
		{
			return JsonConvert.ToString(value, '"');
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002788 File Offset: 0x00000988
		internal static string ToString(Guid value, char quoteChar)
		{
			string str = value.ToString("D", CultureInfo.InvariantCulture);
			string text = quoteChar.ToString(CultureInfo.InvariantCulture);
			return text + str + text;
		}

		/// <summary>
		/// Converts the <see cref="T:System.TimeSpan" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.TimeSpan" />.</returns>
		// Token: 0x06000055 RID: 85 RVA: 0x000027BC File Offset: 0x000009BC
		public static string ToString(TimeSpan value)
		{
			return JsonConvert.ToString(value, '"');
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000027C6 File Offset: 0x000009C6
		internal static string ToString(TimeSpan value, char quoteChar)
		{
			return JsonConvert.ToString(value.ToString(), quoteChar);
		}

		/// <summary>
		/// Converts the <see cref="T:System.Uri" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Uri" />.</returns>
		// Token: 0x06000057 RID: 87 RVA: 0x000027DB File Offset: 0x000009DB
		public static string ToString([Nullable(2)] Uri value)
		{
			if (value == null)
			{
				return JsonConvert.Null;
			}
			return JsonConvert.ToString(value, '"');
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000027F4 File Offset: 0x000009F4
		internal static string ToString(Uri value, char quoteChar)
		{
			return JsonConvert.ToString(value.OriginalString, quoteChar);
		}

		/// <summary>
		/// Converts the <see cref="T:System.String" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.String" />.</returns>
		// Token: 0x06000059 RID: 89 RVA: 0x00002802 File Offset: 0x00000A02
		public static string ToString([Nullable(2)] string value)
		{
			return JsonConvert.ToString(value, '"');
		}

		/// <summary>
		/// Converts the <see cref="T:System.String" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="delimiter">The string delimiter character.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.String" />.</returns>
		// Token: 0x0600005A RID: 90 RVA: 0x0000280C File Offset: 0x00000A0C
		public static string ToString([Nullable(2)] string value, char delimiter)
		{
			return JsonConvert.ToString(value, delimiter, StringEscapeHandling.Default);
		}

		/// <summary>
		/// Converts the <see cref="T:System.String" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="delimiter">The string delimiter character.</param>
		/// <param name="stringEscapeHandling">The string escape handling.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.String" />.</returns>
		// Token: 0x0600005B RID: 91 RVA: 0x00002816 File Offset: 0x00000A16
		public static string ToString([Nullable(2)] string value, char delimiter, StringEscapeHandling stringEscapeHandling)
		{
			if (delimiter != '"' && delimiter != '\'')
			{
				throw new ArgumentException("Delimiter must be a single or double quote.", "delimiter");
			}
			return JavaScriptUtils.ToEscapedJavaScriptString(value, delimiter, true, stringEscapeHandling);
		}

		/// <summary>
		/// Converts the <see cref="T:System.Object" /> to its JSON string representation.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A JSON string representation of the <see cref="T:System.Object" />.</returns>
		// Token: 0x0600005C RID: 92 RVA: 0x0000283C File Offset: 0x00000A3C
		public static string ToString([Nullable(2)] object value)
		{
			if (value == null)
			{
				return JsonConvert.Null;
			}
			switch (ConvertUtils.GetTypeCode(value.GetType()))
			{
			case PrimitiveTypeCode.Char:
				return JsonConvert.ToString((char)value);
			case PrimitiveTypeCode.Boolean:
				return JsonConvert.ToString((bool)value);
			case PrimitiveTypeCode.SByte:
				return JsonConvert.ToString((sbyte)value);
			case PrimitiveTypeCode.Int16:
				return JsonConvert.ToString((short)value);
			case PrimitiveTypeCode.UInt16:
				return JsonConvert.ToString((ushort)value);
			case PrimitiveTypeCode.Int32:
				return JsonConvert.ToString((int)value);
			case PrimitiveTypeCode.Byte:
				return JsonConvert.ToString((byte)value);
			case PrimitiveTypeCode.UInt32:
				return JsonConvert.ToString((uint)value);
			case PrimitiveTypeCode.Int64:
				return JsonConvert.ToString((long)value);
			case PrimitiveTypeCode.UInt64:
				return JsonConvert.ToString((ulong)value);
			case PrimitiveTypeCode.Single:
				return JsonConvert.ToString((float)value);
			case PrimitiveTypeCode.Double:
				return JsonConvert.ToString((double)value);
			case PrimitiveTypeCode.DateTime:
				return JsonConvert.ToString((DateTime)value);
			case PrimitiveTypeCode.DateTimeOffset:
				return JsonConvert.ToString((DateTimeOffset)value);
			case PrimitiveTypeCode.Decimal:
				return JsonConvert.ToString((decimal)value);
			case PrimitiveTypeCode.Guid:
				return JsonConvert.ToString((Guid)value);
			case PrimitiveTypeCode.TimeSpan:
				return JsonConvert.ToString((TimeSpan)value);
			case PrimitiveTypeCode.BigInteger:
				return JsonConvert.ToStringInternal((BigInteger)value);
			case PrimitiveTypeCode.Uri:
				return JsonConvert.ToString((Uri)value);
			case PrimitiveTypeCode.String:
				return JsonConvert.ToString((string)value);
			case PrimitiveTypeCode.DBNull:
				return JsonConvert.Null;
			}
			throw new ArgumentException("Unsupported type: {0}. Use the JsonSerializer class to get the object's JSON representation.".FormatWith(CultureInfo.InvariantCulture, value.GetType()));
		}

		/// <summary>
		/// Serializes the specified object to a JSON string.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <returns>A JSON string representation of the object.</returns>
		// Token: 0x0600005D RID: 93 RVA: 0x00002A1B File Offset: 0x00000C1B
		[DebuggerStepThrough]
		public static string SerializeObject([Nullable(2)] object value)
		{
			return JsonConvert.SerializeObject(value, null, null);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using formatting.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <returns>
		/// A JSON string representation of the object.
		/// </returns>
		// Token: 0x0600005E RID: 94 RVA: 0x00002A25 File Offset: 0x00000C25
		[DebuggerStepThrough]
		public static string SerializeObject([Nullable(2)] object value, Formatting formatting)
		{
			return JsonConvert.SerializeObject(value, formatting, null);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using a collection of <see cref="T:Newtonsoft.Json.JsonConverter" />.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="converters">A collection of converters used while serializing.</param>
		/// <returns>A JSON string representation of the object.</returns>
		// Token: 0x0600005F RID: 95 RVA: 0x00002A30 File Offset: 0x00000C30
		[DebuggerStepThrough]
		public static string SerializeObject([Nullable(2)] object value, params JsonConverter[] converters)
		{
			object obj;
			if (converters == null || converters.Length == 0)
			{
				obj = null;
			}
			else
			{
				(obj = new JsonSerializerSettings()).Converters = converters;
			}
			JsonSerializerSettings settings = obj;
			return JsonConvert.SerializeObject(value, null, settings);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using formatting and a collection of <see cref="T:Newtonsoft.Json.JsonConverter" />.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <param name="converters">A collection of converters used while serializing.</param>
		/// <returns>A JSON string representation of the object.</returns>
		// Token: 0x06000060 RID: 96 RVA: 0x00002A5C File Offset: 0x00000C5C
		[DebuggerStepThrough]
		public static string SerializeObject([Nullable(2)] object value, Formatting formatting, params JsonConverter[] converters)
		{
			object obj;
			if (converters == null || converters.Length == 0)
			{
				obj = null;
			}
			else
			{
				(obj = new JsonSerializerSettings()).Converters = converters;
			}
			JsonSerializerSettings settings = obj;
			return JsonConvert.SerializeObject(value, null, formatting, settings);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to serialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.</param>
		/// <returns>
		/// A JSON string representation of the object.
		/// </returns>
		// Token: 0x06000061 RID: 97 RVA: 0x00002A89 File Offset: 0x00000C89
		[NullableContext(2)]
		[DebuggerStepThrough]
		[return: Nullable(1)]
		public static string SerializeObject(object value, JsonSerializerSettings settings)
		{
			return JsonConvert.SerializeObject(value, null, settings);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using a type, formatting and <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to serialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.</param>
		/// <param name="type">
		/// The type of the value being serialized.
		/// This parameter is used when <see cref="P:Newtonsoft.Json.JsonSerializer.TypeNameHandling" /> is <see cref="F:Newtonsoft.Json.TypeNameHandling.Auto" /> to write out the type name if the type of the value does not match.
		/// Specifying the type is optional.
		/// </param>
		/// <returns>
		/// A JSON string representation of the object.
		/// </returns>
		// Token: 0x06000062 RID: 98 RVA: 0x00002A94 File Offset: 0x00000C94
		[NullableContext(2)]
		[DebuggerStepThrough]
		[return: Nullable(1)]
		public static string SerializeObject(object value, Type type, JsonSerializerSettings settings)
		{
			JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
			return JsonConvert.SerializeObjectInternal(value, type, jsonSerializer);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using formatting and <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to serialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.</param>
		/// <returns>
		/// A JSON string representation of the object.
		/// </returns>
		// Token: 0x06000063 RID: 99 RVA: 0x00002AB0 File Offset: 0x00000CB0
		[NullableContext(2)]
		[DebuggerStepThrough]
		[return: Nullable(1)]
		public static string SerializeObject(object value, Formatting formatting, JsonSerializerSettings settings)
		{
			return JsonConvert.SerializeObject(value, null, formatting, settings);
		}

		/// <summary>
		/// Serializes the specified object to a JSON string using a type, formatting and <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The object to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to serialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.</param>
		/// <param name="type">
		/// The type of the value being serialized.
		/// This parameter is used when <see cref="P:Newtonsoft.Json.JsonSerializer.TypeNameHandling" /> is <see cref="F:Newtonsoft.Json.TypeNameHandling.Auto" /> to write out the type name if the type of the value does not match.
		/// Specifying the type is optional.
		/// </param>
		/// <returns>
		/// A JSON string representation of the object.
		/// </returns>
		// Token: 0x06000064 RID: 100 RVA: 0x00002ABC File Offset: 0x00000CBC
		[NullableContext(2)]
		[DebuggerStepThrough]
		[return: Nullable(1)]
		public static string SerializeObject(object value, Type type, Formatting formatting, JsonSerializerSettings settings)
		{
			JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
			jsonSerializer.Formatting = formatting;
			return JsonConvert.SerializeObjectInternal(value, type, jsonSerializer);
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00002AE0 File Offset: 0x00000CE0
		private static string SerializeObjectInternal([Nullable(2)] object value, [Nullable(2)] Type type, JsonSerializer jsonSerializer)
		{
			StringWriter stringWriter = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
			using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
			{
				jsonTextWriter.Formatting = jsonSerializer.Formatting;
				jsonSerializer.Serialize(jsonTextWriter, value, type);
			}
			return stringWriter.ToString();
		}

		/// <summary>
		/// Deserializes the JSON to a .NET object.
		/// </summary>
		/// <param name="value">The JSON to deserialize.</param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x06000066 RID: 102 RVA: 0x00002B40 File Offset: 0x00000D40
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static object DeserializeObject(string value)
		{
			return JsonConvert.DeserializeObject(value, null, null);
		}

		/// <summary>
		/// Deserializes the JSON to a .NET object using <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="settings">
		/// The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to deserialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.
		/// </param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x06000067 RID: 103 RVA: 0x00002B4A File Offset: 0x00000D4A
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static object DeserializeObject(string value, JsonSerializerSettings settings)
		{
			return JsonConvert.DeserializeObject(value, null, settings);
		}

		/// <summary>
		/// Deserializes the JSON to the specified .NET type.
		/// </summary>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="type">The <see cref="T:System.Type" /> of object being deserialized.</param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x06000068 RID: 104 RVA: 0x00002B54 File Offset: 0x00000D54
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static object DeserializeObject(string value, Type type)
		{
			return JsonConvert.DeserializeObject(value, type, null);
		}

		/// <summary>
		/// Deserializes the JSON to the specified .NET type.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize to.</typeparam>
		/// <param name="value">The JSON to deserialize.</param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x06000069 RID: 105 RVA: 0x00002B5E File Offset: 0x00000D5E
		[NullableContext(2)]
		[DebuggerStepThrough]
		public static T DeserializeObject<T>([Nullable(1)] string value)
		{
			return JsonConvert.DeserializeObject<T>(value, null);
		}

		/// <summary>
		/// Deserializes the JSON to the given anonymous type.
		/// </summary>
		/// <typeparam name="T">
		/// The anonymous type to deserialize to. This can't be specified
		/// traditionally and must be inferred from the anonymous type passed
		/// as a parameter.
		/// </typeparam>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="anonymousTypeObject">The anonymous type object.</param>
		/// <returns>The deserialized anonymous type from the JSON string.</returns>
		// Token: 0x0600006A RID: 106 RVA: 0x00002B67 File Offset: 0x00000D67
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static T DeserializeAnonymousType<[Nullable(2)] T>(string value, T anonymousTypeObject)
		{
			return JsonConvert.DeserializeObject<T>(value);
		}

		/// <summary>
		/// Deserializes the JSON to the given anonymous type using <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <typeparam name="T">
		/// The anonymous type to deserialize to. This can't be specified
		/// traditionally and must be inferred from the anonymous type passed
		/// as a parameter.
		/// </typeparam>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="anonymousTypeObject">The anonymous type object.</param>
		/// <param name="settings">
		/// The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to deserialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.
		/// </param>
		/// <returns>The deserialized anonymous type from the JSON string.</returns>
		// Token: 0x0600006B RID: 107 RVA: 0x00002B6F File Offset: 0x00000D6F
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static T DeserializeAnonymousType<[Nullable(2)] T>(string value, T anonymousTypeObject, JsonSerializerSettings settings)
		{
			return JsonConvert.DeserializeObject<T>(value, settings);
		}

		/// <summary>
		/// Deserializes the JSON to the specified .NET type using a collection of <see cref="T:Newtonsoft.Json.JsonConverter" />.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize to.</typeparam>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="converters">Converters to use while deserializing.</param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x0600006C RID: 108 RVA: 0x00002B78 File Offset: 0x00000D78
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static T DeserializeObject<[Nullable(2)] T>(string value, params JsonConverter[] converters)
		{
			return (T)((object)JsonConvert.DeserializeObject(value, typeof(T), converters));
		}

		/// <summary>
		/// Deserializes the JSON to the specified .NET type using <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize to.</typeparam>
		/// <param name="value">The object to deserialize.</param>
		/// <param name="settings">
		/// The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to deserialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.
		/// </param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x0600006D RID: 109 RVA: 0x00002B90 File Offset: 0x00000D90
		[NullableContext(2)]
		[DebuggerStepThrough]
		public static T DeserializeObject<T>([Nullable(1)] string value, JsonSerializerSettings settings)
		{
			return (T)((object)JsonConvert.DeserializeObject(value, typeof(T), settings));
		}

		/// <summary>
		/// Deserializes the JSON to the specified .NET type using a collection of <see cref="T:Newtonsoft.Json.JsonConverter" />.
		/// </summary>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="type">The type of the object to deserialize.</param>
		/// <param name="converters">Converters to use while deserializing.</param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x0600006E RID: 110 RVA: 0x00002BA8 File Offset: 0x00000DA8
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public static object DeserializeObject(string value, Type type, params JsonConverter[] converters)
		{
			object obj;
			if (converters == null || converters.Length == 0)
			{
				obj = null;
			}
			else
			{
				(obj = new JsonSerializerSettings()).Converters = converters;
			}
			JsonSerializerSettings settings = obj;
			return JsonConvert.DeserializeObject(value, type, settings);
		}

		/// <summary>
		/// Deserializes the JSON to the specified .NET type using <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The JSON to deserialize.</param>
		/// <param name="type">The type of the object to deserialize to.</param>
		/// <param name="settings">
		/// The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to deserialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.
		/// </param>
		/// <returns>The deserialized object from the JSON string.</returns>
		// Token: 0x0600006F RID: 111 RVA: 0x00002BD4 File Offset: 0x00000DD4
		[NullableContext(2)]
		public static object DeserializeObject([Nullable(1)] string value, Type type, JsonSerializerSettings settings)
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
			if (!jsonSerializer.IsCheckAdditionalContentSet())
			{
				jsonSerializer.CheckAdditionalContent = true;
			}
			object result;
			using (JsonTextReader jsonTextReader = new JsonTextReader(new StringReader(value)))
			{
				result = jsonSerializer.Deserialize(jsonTextReader, type);
			}
			return result;
		}

		/// <summary>
		/// Populates the object with values from the JSON string.
		/// </summary>
		/// <param name="value">The JSON to populate values from.</param>
		/// <param name="target">The target object to populate values onto.</param>
		// Token: 0x06000070 RID: 112 RVA: 0x00002C34 File Offset: 0x00000E34
		[DebuggerStepThrough]
		public static void PopulateObject(string value, object target)
		{
			JsonConvert.PopulateObject(value, target, null);
		}

		/// <summary>
		/// Populates the object with values from the JSON string using <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="value">The JSON to populate values from.</param>
		/// <param name="target">The target object to populate values onto.</param>
		/// <param name="settings">
		/// The <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> used to deserialize the object.
		/// If this is <c>null</c>, default serialization settings will be used.
		/// </param>
		// Token: 0x06000071 RID: 113 RVA: 0x00002C40 File Offset: 0x00000E40
		public static void PopulateObject(string value, object target, [Nullable(2)] JsonSerializerSettings settings)
		{
			JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
			using (JsonReader jsonReader = new JsonTextReader(new StringReader(value)))
			{
				jsonSerializer.Populate(jsonReader, target);
				if (settings != null && settings.CheckAdditionalContent)
				{
					while (jsonReader.Read())
					{
						if (jsonReader.TokenType != JsonToken.Comment)
						{
							throw JsonSerializationException.Create(jsonReader, "Additional text found in JSON string after finishing deserializing object.");
						}
					}
				}
			}
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.XmlNode" /> to a JSON string.
		/// </summary>
		/// <param name="node">The node to serialize.</param>
		/// <returns>A JSON string of the <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000072 RID: 114 RVA: 0x00002CB0 File Offset: 0x00000EB0
		public static string SerializeXmlNode([Nullable(2)] XmlNode node)
		{
			return JsonConvert.SerializeXmlNode(node, Formatting.None);
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.XmlNode" /> to a JSON string using formatting.
		/// </summary>
		/// <param name="node">The node to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <returns>A JSON string of the <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000073 RID: 115 RVA: 0x00002CBC File Offset: 0x00000EBC
		public static string SerializeXmlNode([Nullable(2)] XmlNode node, Formatting formatting)
		{
			XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();
			return JsonConvert.SerializeObject(node, formatting, new JsonConverter[] { xmlNodeConverter });
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.XmlNode" /> to a JSON string using formatting and omits the root object if <paramref name="omitRootObject" /> is <c>true</c>.
		/// </summary>
		/// <param name="node">The node to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <param name="omitRootObject">Omits writing the root object.</param>
		/// <returns>A JSON string of the <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000074 RID: 116 RVA: 0x00002CE0 File Offset: 0x00000EE0
		public static string SerializeXmlNode([Nullable(2)] XmlNode node, Formatting formatting, bool omitRootObject)
		{
			XmlNodeConverter xmlNodeConverter = new XmlNodeConverter
			{
				OmitRootObject = omitRootObject
			};
			return JsonConvert.SerializeObject(node, formatting, new JsonConverter[] { xmlNodeConverter });
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.XmlNode" /> from a JSON string.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <returns>The deserialized <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000075 RID: 117 RVA: 0x00002D0B File Offset: 0x00000F0B
		[return: Nullable(2)]
		public static XmlDocument DeserializeXmlNode(string value)
		{
			return JsonConvert.DeserializeXmlNode(value, null);
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.XmlNode" /> from a JSON string nested in a root element specified by <paramref name="deserializeRootElementName" />.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <param name="deserializeRootElementName">The name of the root element to append when deserializing.</param>
		/// <returns>The deserialized <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000076 RID: 118 RVA: 0x00002D14 File Offset: 0x00000F14
		[NullableContext(2)]
		public static XmlDocument DeserializeXmlNode([Nullable(1)] string value, string deserializeRootElementName)
		{
			return JsonConvert.DeserializeXmlNode(value, deserializeRootElementName, false);
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.XmlNode" /> from a JSON string nested in a root element specified by <paramref name="deserializeRootElementName" />
		/// and writes a Json.NET array attribute for collections.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <param name="deserializeRootElementName">The name of the root element to append when deserializing.</param>
		/// <param name="writeArrayAttribute">
		/// A value to indicate whether to write the Json.NET array attribute.
		/// This attribute helps preserve arrays when converting the written XML back to JSON.
		/// </param>
		/// <returns>The deserialized <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000077 RID: 119 RVA: 0x00002D1E File Offset: 0x00000F1E
		[NullableContext(2)]
		public static XmlDocument DeserializeXmlNode([Nullable(1)] string value, string deserializeRootElementName, bool writeArrayAttribute)
		{
			return JsonConvert.DeserializeXmlNode(value, deserializeRootElementName, writeArrayAttribute, false);
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.XmlNode" /> from a JSON string nested in a root element specified by <paramref name="deserializeRootElementName" />,
		/// writes a Json.NET array attribute for collections, and encodes special characters.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <param name="deserializeRootElementName">The name of the root element to append when deserializing.</param>
		/// <param name="writeArrayAttribute">
		/// A value to indicate whether to write the Json.NET array attribute.
		/// This attribute helps preserve arrays when converting the written XML back to JSON.
		/// </param>
		/// <param name="encodeSpecialCharacters">
		/// A value to indicate whether to encode special characters when converting JSON to XML.
		/// If <c>true</c>, special characters like ':', '@', '?', '#' and '$' in JSON property names aren't used to specify
		/// XML namespaces, attributes or processing directives. Instead special characters are encoded and written
		/// as part of the XML element name.
		/// </param>
		/// <returns>The deserialized <see cref="T:System.Xml.XmlNode" />.</returns>
		// Token: 0x06000078 RID: 120 RVA: 0x00002D2C File Offset: 0x00000F2C
		[NullableContext(2)]
		public static XmlDocument DeserializeXmlNode([Nullable(1)] string value, string deserializeRootElementName, bool writeArrayAttribute, bool encodeSpecialCharacters)
		{
			XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();
			xmlNodeConverter.DeserializeRootElementName = deserializeRootElementName;
			xmlNodeConverter.WriteArrayAttribute = writeArrayAttribute;
			xmlNodeConverter.EncodeSpecialCharacters = encodeSpecialCharacters;
			return (XmlDocument)JsonConvert.DeserializeObject(value, typeof(XmlDocument), new JsonConverter[] { xmlNodeConverter });
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.Linq.XNode" /> to a JSON string.
		/// </summary>
		/// <param name="node">The node to convert to JSON.</param>
		/// <returns>A JSON string of the <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x06000079 RID: 121 RVA: 0x00002D73 File Offset: 0x00000F73
		public static string SerializeXNode([Nullable(2)] XObject node)
		{
			return JsonConvert.SerializeXNode(node, Formatting.None);
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.Linq.XNode" /> to a JSON string using formatting.
		/// </summary>
		/// <param name="node">The node to convert to JSON.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <returns>A JSON string of the <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x0600007A RID: 122 RVA: 0x00002D7C File Offset: 0x00000F7C
		public static string SerializeXNode([Nullable(2)] XObject node, Formatting formatting)
		{
			return JsonConvert.SerializeXNode(node, formatting, false);
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.Linq.XNode" /> to a JSON string using formatting and omits the root object if <paramref name="omitRootObject" /> is <c>true</c>.
		/// </summary>
		/// <param name="node">The node to serialize.</param>
		/// <param name="formatting">Indicates how the output should be formatted.</param>
		/// <param name="omitRootObject">Omits writing the root object.</param>
		/// <returns>A JSON string of the <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x0600007B RID: 123 RVA: 0x00002D88 File Offset: 0x00000F88
		public static string SerializeXNode([Nullable(2)] XObject node, Formatting formatting, bool omitRootObject)
		{
			XmlNodeConverter xmlNodeConverter = new XmlNodeConverter
			{
				OmitRootObject = omitRootObject
			};
			return JsonConvert.SerializeObject(node, formatting, new JsonConverter[] { xmlNodeConverter });
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.Linq.XNode" /> from a JSON string.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <returns>The deserialized <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x0600007C RID: 124 RVA: 0x00002DB3 File Offset: 0x00000FB3
		[return: Nullable(2)]
		public static XDocument DeserializeXNode(string value)
		{
			return JsonConvert.DeserializeXNode(value, null);
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.Linq.XNode" /> from a JSON string nested in a root element specified by <paramref name="deserializeRootElementName" />.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <param name="deserializeRootElementName">The name of the root element to append when deserializing.</param>
		/// <returns>The deserialized <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x0600007D RID: 125 RVA: 0x00002DBC File Offset: 0x00000FBC
		[NullableContext(2)]
		public static XDocument DeserializeXNode([Nullable(1)] string value, string deserializeRootElementName)
		{
			return JsonConvert.DeserializeXNode(value, deserializeRootElementName, false);
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.Linq.XNode" /> from a JSON string nested in a root element specified by <paramref name="deserializeRootElementName" />
		/// and writes a Json.NET array attribute for collections.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <param name="deserializeRootElementName">The name of the root element to append when deserializing.</param>
		/// <param name="writeArrayAttribute">
		/// A value to indicate whether to write the Json.NET array attribute.
		/// This attribute helps preserve arrays when converting the written XML back to JSON.
		/// </param>
		/// <returns>The deserialized <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x0600007E RID: 126 RVA: 0x00002DC6 File Offset: 0x00000FC6
		[NullableContext(2)]
		public static XDocument DeserializeXNode([Nullable(1)] string value, string deserializeRootElementName, bool writeArrayAttribute)
		{
			return JsonConvert.DeserializeXNode(value, deserializeRootElementName, writeArrayAttribute, false);
		}

		/// <summary>
		/// Deserializes the <see cref="T:System.Xml.Linq.XNode" /> from a JSON string nested in a root element specified by <paramref name="deserializeRootElementName" />,
		/// writes a Json.NET array attribute for collections, and encodes special characters.
		/// </summary>
		/// <param name="value">The JSON string.</param>
		/// <param name="deserializeRootElementName">The name of the root element to append when deserializing.</param>
		/// <param name="writeArrayAttribute">
		/// A value to indicate whether to write the Json.NET array attribute.
		/// This attribute helps preserve arrays when converting the written XML back to JSON.
		/// </param>
		/// <param name="encodeSpecialCharacters">
		/// A value to indicate whether to encode special characters when converting JSON to XML.
		/// If <c>true</c>, special characters like ':', '@', '?', '#' and '$' in JSON property names aren't used to specify
		/// XML namespaces, attributes or processing directives. Instead special characters are encoded and written
		/// as part of the XML element name.
		/// </param>
		/// <returns>The deserialized <see cref="T:System.Xml.Linq.XNode" />.</returns>
		// Token: 0x0600007F RID: 127 RVA: 0x00002DD4 File Offset: 0x00000FD4
		[NullableContext(2)]
		public static XDocument DeserializeXNode([Nullable(1)] string value, string deserializeRootElementName, bool writeArrayAttribute, bool encodeSpecialCharacters)
		{
			XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();
			xmlNodeConverter.DeserializeRootElementName = deserializeRootElementName;
			xmlNodeConverter.WriteArrayAttribute = writeArrayAttribute;
			xmlNodeConverter.EncodeSpecialCharacters = encodeSpecialCharacters;
			return (XDocument)JsonConvert.DeserializeObject(value, typeof(XDocument), new JsonConverter[] { xmlNodeConverter });
		}

		/// <summary>
		/// Represents JavaScript's boolean value <c>true</c> as a string. This field is read-only.
		/// </summary>
		// Token: 0x04000035 RID: 53
		public static readonly string True = "true";

		/// <summary>
		/// Represents JavaScript's boolean value <c>false</c> as a string. This field is read-only.
		/// </summary>
		// Token: 0x04000036 RID: 54
		public static readonly string False = "false";

		/// <summary>
		/// Represents JavaScript's <c>null</c> as a string. This field is read-only.
		/// </summary>
		// Token: 0x04000037 RID: 55
		public static readonly string Null = "null";

		/// <summary>
		/// Represents JavaScript's <c>undefined</c> as a string. This field is read-only.
		/// </summary>
		// Token: 0x04000038 RID: 56
		public static readonly string Undefined = "undefined";

		/// <summary>
		/// Represents JavaScript's positive infinity as a string. This field is read-only.
		/// </summary>
		// Token: 0x04000039 RID: 57
		public static readonly string PositiveInfinity = "Infinity";

		/// <summary>
		/// Represents JavaScript's negative infinity as a string. This field is read-only.
		/// </summary>
		// Token: 0x0400003A RID: 58
		public static readonly string NegativeInfinity = "-Infinity";

		/// <summary>
		/// Represents JavaScript's <c>NaN</c> as a string. This field is read-only.
		/// </summary>
		// Token: 0x0400003B RID: 59
		public static readonly string NaN = "NaN";
	}
}
