using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a value in JSON (string, integer, date, etc).
	/// </summary>
	// Token: 0x020000CC RID: 204
	[NullableContext(2)]
	[Nullable(0)]
	public class JValue : JToken, IEquatable<JValue>, IFormattable, IComparable, IComparable<JValue>, IConvertible
	{
		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" /> asynchronously.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" /> which will be used when writing the token.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous write operation.</returns>
		// Token: 0x06000BB3 RID: 2995 RVA: 0x0002E470 File Offset: 0x0002C670
		[NullableContext(1)]
		public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters)
		{
			if (converters != null && converters.Length != 0 && this._value != null)
			{
				JsonConverter matchingConverter = JsonSerializer.GetMatchingConverter(converters, this._value.GetType());
				if (matchingConverter != null && matchingConverter.CanWrite)
				{
					matchingConverter.WriteJson(writer, this._value, JsonSerializer.CreateDefault());
					return AsyncUtils.CompletedTask;
				}
			}
			switch (this._valueType)
			{
			case JTokenType.Comment:
			{
				object value = this._value;
				return writer.WriteCommentAsync((value != null) ? value.ToString() : null, cancellationToken);
			}
			case JTokenType.Integer:
			{
				object value2 = this._value;
				if (value2 is int)
				{
					int value3 = (int)value2;
					return writer.WriteValueAsync(value3, cancellationToken);
				}
				value2 = this._value;
				if (value2 is long)
				{
					long value4 = (long)value2;
					return writer.WriteValueAsync(value4, cancellationToken);
				}
				value2 = this._value;
				if (value2 is ulong)
				{
					ulong value5 = (ulong)value2;
					return writer.WriteValueAsync(value5, cancellationToken);
				}
				value2 = this._value;
				if (value2 is BigInteger)
				{
					BigInteger bigInteger = (BigInteger)value2;
					return writer.WriteValueAsync(bigInteger, cancellationToken);
				}
				return writer.WriteValueAsync(Convert.ToInt64(this._value, CultureInfo.InvariantCulture), cancellationToken);
			}
			case JTokenType.Float:
			{
				object value2 = this._value;
				if (value2 is decimal)
				{
					decimal value6 = (decimal)value2;
					return writer.WriteValueAsync(value6, cancellationToken);
				}
				value2 = this._value;
				if (value2 is double)
				{
					double value7 = (double)value2;
					return writer.WriteValueAsync(value7, cancellationToken);
				}
				value2 = this._value;
				if (value2 is float)
				{
					float value8 = (float)value2;
					return writer.WriteValueAsync(value8, cancellationToken);
				}
				return writer.WriteValueAsync(Convert.ToDouble(this._value, CultureInfo.InvariantCulture), cancellationToken);
			}
			case JTokenType.String:
			{
				object value9 = this._value;
				return writer.WriteValueAsync((value9 != null) ? value9.ToString() : null, cancellationToken);
			}
			case JTokenType.Boolean:
				return writer.WriteValueAsync(Convert.ToBoolean(this._value, CultureInfo.InvariantCulture), cancellationToken);
			case JTokenType.Null:
				return writer.WriteNullAsync(cancellationToken);
			case JTokenType.Undefined:
				return writer.WriteUndefinedAsync(cancellationToken);
			case JTokenType.Date:
			{
				object value2 = this._value;
				if (value2 is DateTimeOffset)
				{
					DateTimeOffset value10 = (DateTimeOffset)value2;
					return writer.WriteValueAsync(value10, cancellationToken);
				}
				return writer.WriteValueAsync(Convert.ToDateTime(this._value, CultureInfo.InvariantCulture), cancellationToken);
			}
			case JTokenType.Raw:
			{
				object value11 = this._value;
				return writer.WriteRawValueAsync((value11 != null) ? value11.ToString() : null, cancellationToken);
			}
			case JTokenType.Bytes:
				return writer.WriteValueAsync((byte[])this._value, cancellationToken);
			case JTokenType.Guid:
				return writer.WriteValueAsync((this._value != null) ? ((Guid?)this._value) : null, cancellationToken);
			case JTokenType.Uri:
				return writer.WriteValueAsync((Uri)this._value, cancellationToken);
			case JTokenType.TimeSpan:
				return writer.WriteValueAsync((this._value != null) ? ((TimeSpan?)this._value) : null, cancellationToken);
			default:
				throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", this._valueType, "Unexpected token type.");
			}
		}

		// Token: 0x06000BB4 RID: 2996 RVA: 0x0002E776 File Offset: 0x0002C976
		internal JValue(object value, JTokenType type)
		{
			this._value = value;
			this._valueType = type;
		}

		// Token: 0x06000BB5 RID: 2997 RVA: 0x0002E78C File Offset: 0x0002C98C
		[NullableContext(1)]
		internal JValue(JValue other, [Nullable(2)] JsonCloneSettings settings)
			: this(other.Value, other.Type)
		{
			if (settings == null || settings.CopyAnnotations)
			{
				base.CopyAnnotations(this, other);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class from another <see cref="T:Newtonsoft.Json.Linq.JValue" /> object.
		/// </summary>
		/// <param name="other">A <see cref="T:Newtonsoft.Json.Linq.JValue" /> object to copy from.</param>
		// Token: 0x06000BB6 RID: 2998 RVA: 0x0002E7B6 File Offset: 0x0002C9B6
		[NullableContext(1)]
		public JValue(JValue other)
			: this(other.Value, other.Type)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BB7 RID: 2999 RVA: 0x0002E7CA File Offset: 0x0002C9CA
		public JValue(long value)
			: this(BoxedPrimitives.Get(value), JTokenType.Integer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BB8 RID: 3000 RVA: 0x0002E7D9 File Offset: 0x0002C9D9
		public JValue(decimal value)
			: this(BoxedPrimitives.Get(value), JTokenType.Float)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BB9 RID: 3001 RVA: 0x0002E7E8 File Offset: 0x0002C9E8
		public JValue(char value)
			: this(value, JTokenType.String)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BBA RID: 3002 RVA: 0x0002E7F7 File Offset: 0x0002C9F7
		[CLSCompliant(false)]
		public JValue(ulong value)
			: this(value, JTokenType.Integer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BBB RID: 3003 RVA: 0x0002E806 File Offset: 0x0002CA06
		public JValue(double value)
			: this(BoxedPrimitives.Get(value), JTokenType.Float)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BBC RID: 3004 RVA: 0x0002E815 File Offset: 0x0002CA15
		public JValue(float value)
			: this(value, JTokenType.Float)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BBD RID: 3005 RVA: 0x0002E824 File Offset: 0x0002CA24
		public JValue(DateTime value)
			: this(value, JTokenType.Date)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BBE RID: 3006 RVA: 0x0002E834 File Offset: 0x0002CA34
		public JValue(DateTimeOffset value)
			: this(value, JTokenType.Date)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BBF RID: 3007 RVA: 0x0002E844 File Offset: 0x0002CA44
		public JValue(bool value)
			: this(BoxedPrimitives.Get(value), JTokenType.Boolean)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BC0 RID: 3008 RVA: 0x0002E854 File Offset: 0x0002CA54
		public JValue(string value)
			: this(value, JTokenType.String)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BC1 RID: 3009 RVA: 0x0002E85E File Offset: 0x0002CA5E
		public JValue(Guid value)
			: this(value, JTokenType.Guid)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BC2 RID: 3010 RVA: 0x0002E86E File Offset: 0x0002CA6E
		public JValue(Uri value)
			: this(value, (value != null) ? JTokenType.Uri : JTokenType.Null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BC3 RID: 3011 RVA: 0x0002E886 File Offset: 0x0002CA86
		public JValue(TimeSpan value)
			: this(value, JTokenType.TimeSpan)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JValue" /> class with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		// Token: 0x06000BC4 RID: 3012 RVA: 0x0002E898 File Offset: 0x0002CA98
		public JValue(object value)
			: this(value, JValue.GetValueType(null, value))
		{
		}

		// Token: 0x06000BC5 RID: 3013 RVA: 0x0002E8BC File Offset: 0x0002CABC
		[NullableContext(1)]
		internal override bool DeepEquals(JToken node)
		{
			JValue jvalue = node as JValue;
			return jvalue != null && (jvalue == this || JValue.ValuesEquals(this, jvalue));
		}

		/// <summary>
		/// Gets a value indicating whether this token has child tokens.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this token has child values; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x17000209 RID: 521
		// (get) Token: 0x06000BC6 RID: 3014 RVA: 0x0002E8E2 File Offset: 0x0002CAE2
		public override bool HasValues
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000BC7 RID: 3015 RVA: 0x0002E8E8 File Offset: 0x0002CAE8
		[NullableContext(1)]
		private static int CompareBigInteger(BigInteger i1, object i2)
		{
			int num = i1.CompareTo(ConvertUtils.ToBigInteger(i2));
			if (num != 0)
			{
				return num;
			}
			if (i2 is decimal)
			{
				decimal num2 = (decimal)i2;
				return 0m.CompareTo(Math.Abs(num2 - Math.Truncate(num2)));
			}
			if (i2 is double || i2 is float)
			{
				double num3 = Convert.ToDouble(i2, CultureInfo.InvariantCulture);
				return 0.0.CompareTo(Math.Abs(num3 - Math.Truncate(num3)));
			}
			return num;
		}

		// Token: 0x06000BC8 RID: 3016 RVA: 0x0002E974 File Offset: 0x0002CB74
		internal static int Compare(JTokenType valueType, object objA, object objB)
		{
			if (objA == objB)
			{
				return 0;
			}
			if (objB == null)
			{
				return 1;
			}
			if (objA == null)
			{
				return -1;
			}
			switch (valueType)
			{
			case JTokenType.Comment:
			case JTokenType.String:
			case JTokenType.Raw:
			{
				string strA = Convert.ToString(objA, CultureInfo.InvariantCulture);
				string strB = Convert.ToString(objB, CultureInfo.InvariantCulture);
				return string.CompareOrdinal(strA, strB);
			}
			case JTokenType.Integer:
				if (objA is BigInteger)
				{
					BigInteger i = (BigInteger)objA;
					return JValue.CompareBigInteger(i, objB);
				}
				if (objB is BigInteger)
				{
					BigInteger i2 = (BigInteger)objB;
					return -JValue.CompareBigInteger(i2, objA);
				}
				if (objA is ulong || objB is ulong || objA is decimal || objB is decimal)
				{
					return Convert.ToDecimal(objA, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(objB, CultureInfo.InvariantCulture));
				}
				if (objA is float || objB is float || objA is double || objB is double)
				{
					return JValue.CompareFloat(objA, objB);
				}
				return Convert.ToInt64(objA, CultureInfo.InvariantCulture).CompareTo(Convert.ToInt64(objB, CultureInfo.InvariantCulture));
			case JTokenType.Float:
				if (objA is BigInteger)
				{
					BigInteger i3 = (BigInteger)objA;
					return JValue.CompareBigInteger(i3, objB);
				}
				if (objB is BigInteger)
				{
					BigInteger i4 = (BigInteger)objB;
					return -JValue.CompareBigInteger(i4, objA);
				}
				if (objA is ulong || objB is ulong || objA is decimal || objB is decimal)
				{
					return Convert.ToDecimal(objA, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(objB, CultureInfo.InvariantCulture));
				}
				return JValue.CompareFloat(objA, objB);
			case JTokenType.Boolean:
			{
				bool flag = Convert.ToBoolean(objA, CultureInfo.InvariantCulture);
				bool value = Convert.ToBoolean(objB, CultureInfo.InvariantCulture);
				return flag.CompareTo(value);
			}
			case JTokenType.Date:
			{
				if (objA is DateTime)
				{
					DateTime dateTime = (DateTime)objA;
					DateTime value2;
					if (objB is DateTimeOffset)
					{
						value2 = ((DateTimeOffset)objB).DateTime;
					}
					else
					{
						value2 = Convert.ToDateTime(objB, CultureInfo.InvariantCulture);
					}
					return dateTime.CompareTo(value2);
				}
				DateTimeOffset dateTimeOffset = (DateTimeOffset)objA;
				DateTimeOffset other;
				if (objB is DateTimeOffset)
				{
					other = (DateTimeOffset)objB;
				}
				else
				{
					other = new DateTimeOffset(Convert.ToDateTime(objB, CultureInfo.InvariantCulture));
				}
				return dateTimeOffset.CompareTo(other);
			}
			case JTokenType.Bytes:
			{
				byte[] array = objB as byte[];
				if (array == null)
				{
					throw new ArgumentException("Object must be of type byte[].");
				}
				return MiscellaneousUtils.ByteArrayCompare(objA as byte[], array);
			}
			case JTokenType.Guid:
			{
				if (!(objB is Guid))
				{
					throw new ArgumentException("Object must be of type Guid.");
				}
				Guid guid = (Guid)objA;
				Guid value3 = (Guid)objB;
				return guid.CompareTo(value3);
			}
			case JTokenType.Uri:
			{
				Uri uri = objB as Uri;
				if (uri == null)
				{
					throw new ArgumentException("Object must be of type Uri.");
				}
				Uri uri2 = (Uri)objA;
				return Comparer<string>.Default.Compare(uri2.ToString(), uri.ToString());
			}
			case JTokenType.TimeSpan:
			{
				if (!(objB is TimeSpan))
				{
					throw new ArgumentException("Object must be of type TimeSpan.");
				}
				TimeSpan timeSpan = (TimeSpan)objA;
				TimeSpan value4 = (TimeSpan)objB;
				return timeSpan.CompareTo(value4);
			}
			}
			throw MiscellaneousUtils.CreateArgumentOutOfRangeException("valueType", valueType, "Unexpected value type: {0}".FormatWith(CultureInfo.InvariantCulture, valueType));
		}

		// Token: 0x06000BC9 RID: 3017 RVA: 0x0002ECA0 File Offset: 0x0002CEA0
		[NullableContext(1)]
		private static int CompareFloat(object objA, object objB)
		{
			double d = Convert.ToDouble(objA, CultureInfo.InvariantCulture);
			double num = Convert.ToDouble(objB, CultureInfo.InvariantCulture);
			if (MathUtils.ApproxEquals(d, num))
			{
				return 0;
			}
			return d.CompareTo(num);
		}

		// Token: 0x06000BCA RID: 3018 RVA: 0x0002ECD8 File Offset: 0x0002CED8
		private static bool Operation(ExpressionType operation, object objA, object objB, out object result)
		{
			if ((objA is string || objB is string) && (operation == ExpressionType.Add || operation == ExpressionType.AddAssign))
			{
				result = ((objA != null) ? objA.ToString() : null) + ((objB != null) ? objB.ToString() : null);
				return true;
			}
			if (objA is BigInteger || objB is BigInteger)
			{
				if (objA == null || objB == null)
				{
					result = null;
					return true;
				}
				BigInteger bigInteger = ConvertUtils.ToBigInteger(objA);
				BigInteger bigInteger2 = ConvertUtils.ToBigInteger(objB);
				if (operation <= ExpressionType.Subtract)
				{
					if (operation <= ExpressionType.Divide)
					{
						if (operation != ExpressionType.Add)
						{
							if (operation != ExpressionType.Divide)
							{
								goto IL_393;
							}
							goto IL_DE;
						}
					}
					else
					{
						if (operation == ExpressionType.Multiply)
						{
							goto IL_CE;
						}
						if (operation != ExpressionType.Subtract)
						{
							goto IL_393;
						}
						goto IL_BE;
					}
				}
				else if (operation <= ExpressionType.DivideAssign)
				{
					if (operation != ExpressionType.AddAssign)
					{
						if (operation != ExpressionType.DivideAssign)
						{
							goto IL_393;
						}
						goto IL_DE;
					}
				}
				else
				{
					if (operation == ExpressionType.MultiplyAssign)
					{
						goto IL_CE;
					}
					if (operation != ExpressionType.SubtractAssign)
					{
						goto IL_393;
					}
					goto IL_BE;
				}
				result = bigInteger + bigInteger2;
				return true;
				IL_BE:
				result = bigInteger - bigInteger2;
				return true;
				IL_CE:
				result = bigInteger * bigInteger2;
				return true;
				IL_DE:
				result = bigInteger / bigInteger2;
				return true;
			}
			else if (objA is ulong || objB is ulong || objA is decimal || objB is decimal)
			{
				if (objA == null || objB == null)
				{
					result = null;
					return true;
				}
				decimal d = Convert.ToDecimal(objA, CultureInfo.InvariantCulture);
				decimal d2 = Convert.ToDecimal(objB, CultureInfo.InvariantCulture);
				if (operation <= ExpressionType.Subtract)
				{
					if (operation <= ExpressionType.Divide)
					{
						if (operation != ExpressionType.Add)
						{
							if (operation != ExpressionType.Divide)
							{
								goto IL_393;
							}
							goto IL_1AD;
						}
					}
					else
					{
						if (operation == ExpressionType.Multiply)
						{
							goto IL_19D;
						}
						if (operation != ExpressionType.Subtract)
						{
							goto IL_393;
						}
						goto IL_18D;
					}
				}
				else if (operation <= ExpressionType.DivideAssign)
				{
					if (operation != ExpressionType.AddAssign)
					{
						if (operation != ExpressionType.DivideAssign)
						{
							goto IL_393;
						}
						goto IL_1AD;
					}
				}
				else
				{
					if (operation == ExpressionType.MultiplyAssign)
					{
						goto IL_19D;
					}
					if (operation != ExpressionType.SubtractAssign)
					{
						goto IL_393;
					}
					goto IL_18D;
				}
				result = d + d2;
				return true;
				IL_18D:
				result = d - d2;
				return true;
				IL_19D:
				result = d * d2;
				return true;
				IL_1AD:
				result = d / d2;
				return true;
			}
			else if (objA is float || objB is float || objA is double || objB is double)
			{
				if (objA == null || objB == null)
				{
					result = null;
					return true;
				}
				double num = Convert.ToDouble(objA, CultureInfo.InvariantCulture);
				double num2 = Convert.ToDouble(objB, CultureInfo.InvariantCulture);
				if (operation <= ExpressionType.Subtract)
				{
					if (operation <= ExpressionType.Divide)
					{
						if (operation != ExpressionType.Add)
						{
							if (operation != ExpressionType.Divide)
							{
								goto IL_393;
							}
							goto IL_278;
						}
					}
					else
					{
						if (operation == ExpressionType.Multiply)
						{
							goto IL_26A;
						}
						if (operation != ExpressionType.Subtract)
						{
							goto IL_393;
						}
						goto IL_25C;
					}
				}
				else if (operation <= ExpressionType.DivideAssign)
				{
					if (operation != ExpressionType.AddAssign)
					{
						if (operation != ExpressionType.DivideAssign)
						{
							goto IL_393;
						}
						goto IL_278;
					}
				}
				else
				{
					if (operation == ExpressionType.MultiplyAssign)
					{
						goto IL_26A;
					}
					if (operation != ExpressionType.SubtractAssign)
					{
						goto IL_393;
					}
					goto IL_25C;
				}
				result = num + num2;
				return true;
				IL_25C:
				result = num - num2;
				return true;
				IL_26A:
				result = num * num2;
				return true;
				IL_278:
				result = num / num2;
				return true;
			}
			else if (objA is int || objA is uint || objA is long || objA is short || objA is ushort || objA is sbyte || objA is byte || objB is int || objB is uint || objB is long || objB is short || objB is ushort || objB is sbyte || objB is byte)
			{
				if (objA == null || objB == null)
				{
					result = null;
					return true;
				}
				long num3 = Convert.ToInt64(objA, CultureInfo.InvariantCulture);
				long num4 = Convert.ToInt64(objB, CultureInfo.InvariantCulture);
				if (operation <= ExpressionType.Subtract)
				{
					if (operation <= ExpressionType.Divide)
					{
						if (operation != ExpressionType.Add)
						{
							if (operation != ExpressionType.Divide)
							{
								goto IL_393;
							}
							goto IL_385;
						}
					}
					else
					{
						if (operation == ExpressionType.Multiply)
						{
							goto IL_377;
						}
						if (operation != ExpressionType.Subtract)
						{
							goto IL_393;
						}
						goto IL_369;
					}
				}
				else if (operation <= ExpressionType.DivideAssign)
				{
					if (operation != ExpressionType.AddAssign)
					{
						if (operation != ExpressionType.DivideAssign)
						{
							goto IL_393;
						}
						goto IL_385;
					}
				}
				else
				{
					if (operation == ExpressionType.MultiplyAssign)
					{
						goto IL_377;
					}
					if (operation != ExpressionType.SubtractAssign)
					{
						goto IL_393;
					}
					goto IL_369;
				}
				result = num3 + num4;
				return true;
				IL_369:
				result = num3 - num4;
				return true;
				IL_377:
				result = num3 * num4;
				return true;
				IL_385:
				result = num3 / num4;
				return true;
			}
			IL_393:
			result = null;
			return false;
		}

		// Token: 0x06000BCB RID: 3019 RVA: 0x0002F07C File Offset: 0x0002D27C
		[NullableContext(1)]
		internal override JToken CloneToken([Nullable(2)] JsonCloneSettings settings)
		{
			return new JValue(this, settings);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JValue" /> comment with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JValue" /> comment with the given value.</returns>
		// Token: 0x06000BCC RID: 3020 RVA: 0x0002F085 File Offset: 0x0002D285
		[NullableContext(1)]
		public static JValue CreateComment([Nullable(2)] string value)
		{
			return new JValue(value, JTokenType.Comment);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JValue" /> string with the given value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JValue" /> string with the given value.</returns>
		// Token: 0x06000BCD RID: 3021 RVA: 0x0002F08E File Offset: 0x0002D28E
		[NullableContext(1)]
		public static JValue CreateString([Nullable(2)] string value)
		{
			return new JValue(value, JTokenType.String);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JValue" /> null value.
		/// </summary>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JValue" /> null value.</returns>
		// Token: 0x06000BCE RID: 3022 RVA: 0x0002F097 File Offset: 0x0002D297
		[NullableContext(1)]
		public static JValue CreateNull()
		{
			return new JValue(null, JTokenType.Null);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Linq.JValue" /> undefined value.
		/// </summary>
		/// <returns>A <see cref="T:Newtonsoft.Json.Linq.JValue" /> undefined value.</returns>
		// Token: 0x06000BCF RID: 3023 RVA: 0x0002F0A1 File Offset: 0x0002D2A1
		[NullableContext(1)]
		public static JValue CreateUndefined()
		{
			return new JValue(null, JTokenType.Undefined);
		}

		// Token: 0x06000BD0 RID: 3024 RVA: 0x0002F0AC File Offset: 0x0002D2AC
		private static JTokenType GetValueType(JTokenType? current, object value)
		{
			if (value == null)
			{
				return JTokenType.Null;
			}
			if (value == DBNull.Value)
			{
				return JTokenType.Null;
			}
			if (value is string)
			{
				return JValue.GetStringValueType(current);
			}
			if (value is long || value is int || value is short || value is sbyte || value is ulong || value is uint || value is ushort || value is byte)
			{
				return JTokenType.Integer;
			}
			if (value is Enum)
			{
				return JTokenType.Integer;
			}
			if (value is BigInteger)
			{
				return JTokenType.Integer;
			}
			if (value is double || value is float || value is decimal)
			{
				return JTokenType.Float;
			}
			if (value is DateTime)
			{
				return JTokenType.Date;
			}
			if (value is DateTimeOffset)
			{
				return JTokenType.Date;
			}
			if (value is byte[])
			{
				return JTokenType.Bytes;
			}
			if (value is bool)
			{
				return JTokenType.Boolean;
			}
			if (value is Guid)
			{
				return JTokenType.Guid;
			}
			if (value is Uri)
			{
				return JTokenType.Uri;
			}
			if (value is TimeSpan)
			{
				return JTokenType.TimeSpan;
			}
			throw new ArgumentException("Could not determine JSON object type for type {0}.".FormatWith(CultureInfo.InvariantCulture, value.GetType()));
		}

		// Token: 0x06000BD1 RID: 3025 RVA: 0x0002F1B0 File Offset: 0x0002D3B0
		private static JTokenType GetStringValueType(JTokenType? current)
		{
			if (current == null)
			{
				return JTokenType.String;
			}
			JTokenType valueOrDefault = current.GetValueOrDefault();
			if (valueOrDefault == JTokenType.Comment || valueOrDefault == JTokenType.String || valueOrDefault == JTokenType.Raw)
			{
				return current.GetValueOrDefault();
			}
			return JTokenType.String;
		}

		/// <summary>
		/// Gets the node type for this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <value>The type.</value>
		// Token: 0x1700020A RID: 522
		// (get) Token: 0x06000BD2 RID: 3026 RVA: 0x0002F1E6 File Offset: 0x0002D3E6
		public override JTokenType Type
		{
			get
			{
				return this._valueType;
			}
		}

		/// <summary>
		/// Gets or sets the underlying token value.
		/// </summary>
		/// <value>The underlying token value.</value>
		// Token: 0x1700020B RID: 523
		// (get) Token: 0x06000BD3 RID: 3027 RVA: 0x0002F1EE File Offset: 0x0002D3EE
		// (set) Token: 0x06000BD4 RID: 3028 RVA: 0x0002F1F8 File Offset: 0x0002D3F8
		public new object Value
		{
			get
			{
				return this._value;
			}
			set
			{
				object value2 = this._value;
				Type left = ((value2 != null) ? value2.GetType() : null);
				Type right = ((value != null) ? value.GetType() : null);
				if (left != right)
				{
					this._valueType = JValue.GetValueType(new JTokenType?(this._valueType), value);
				}
				this._value = value;
			}
		}

		/// <summary>
		/// Writes this token to a <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="writer">A <see cref="T:Newtonsoft.Json.JsonWriter" /> into which this method will write.</param>
		/// <param name="converters">A collection of <see cref="T:Newtonsoft.Json.JsonConverter" />s which will be used when writing the token.</param>
		// Token: 0x06000BD5 RID: 3029 RVA: 0x0002F24C File Offset: 0x0002D44C
		[NullableContext(1)]
		public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
		{
			if (converters != null && converters.Length != 0 && this._value != null)
			{
				JsonConverter matchingConverter = JsonSerializer.GetMatchingConverter(converters, this._value.GetType());
				if (matchingConverter != null && matchingConverter.CanWrite)
				{
					matchingConverter.WriteJson(writer, this._value, JsonSerializer.CreateDefault());
					return;
				}
			}
			switch (this._valueType)
			{
			case JTokenType.Comment:
			{
				object value = this._value;
				writer.WriteComment((value != null) ? value.ToString() : null);
				return;
			}
			case JTokenType.Integer:
			{
				object value2 = this._value;
				if (value2 is int)
				{
					int value3 = (int)value2;
					writer.WriteValue(value3);
					return;
				}
				value2 = this._value;
				if (value2 is long)
				{
					long value4 = (long)value2;
					writer.WriteValue(value4);
					return;
				}
				value2 = this._value;
				if (value2 is ulong)
				{
					ulong value5 = (ulong)value2;
					writer.WriteValue(value5);
					return;
				}
				value2 = this._value;
				if (value2 is BigInteger)
				{
					BigInteger bigInteger = (BigInteger)value2;
					writer.WriteValue(bigInteger);
					return;
				}
				writer.WriteValue(Convert.ToInt64(this._value, CultureInfo.InvariantCulture));
				return;
			}
			case JTokenType.Float:
			{
				object value2 = this._value;
				if (value2 is decimal)
				{
					decimal value6 = (decimal)value2;
					writer.WriteValue(value6);
					return;
				}
				value2 = this._value;
				if (value2 is double)
				{
					double value7 = (double)value2;
					writer.WriteValue(value7);
					return;
				}
				value2 = this._value;
				if (value2 is float)
				{
					float value8 = (float)value2;
					writer.WriteValue(value8);
					return;
				}
				writer.WriteValue(Convert.ToDouble(this._value, CultureInfo.InvariantCulture));
				return;
			}
			case JTokenType.String:
			{
				object value9 = this._value;
				writer.WriteValue((value9 != null) ? value9.ToString() : null);
				return;
			}
			case JTokenType.Boolean:
				writer.WriteValue(Convert.ToBoolean(this._value, CultureInfo.InvariantCulture));
				return;
			case JTokenType.Null:
				writer.WriteNull();
				return;
			case JTokenType.Undefined:
				writer.WriteUndefined();
				return;
			case JTokenType.Date:
			{
				object value2 = this._value;
				if (value2 is DateTimeOffset)
				{
					DateTimeOffset value10 = (DateTimeOffset)value2;
					writer.WriteValue(value10);
					return;
				}
				writer.WriteValue(Convert.ToDateTime(this._value, CultureInfo.InvariantCulture));
				return;
			}
			case JTokenType.Raw:
			{
				object value11 = this._value;
				writer.WriteRawValue((value11 != null) ? value11.ToString() : null);
				return;
			}
			case JTokenType.Bytes:
				writer.WriteValue((byte[])this._value);
				return;
			case JTokenType.Guid:
				writer.WriteValue((this._value != null) ? ((Guid?)this._value) : null);
				return;
			case JTokenType.Uri:
				writer.WriteValue((Uri)this._value);
				return;
			case JTokenType.TimeSpan:
				writer.WriteValue((this._value != null) ? ((TimeSpan?)this._value) : null);
				return;
			default:
				throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", this._valueType, "Unexpected token type.");
			}
		}

		// Token: 0x06000BD6 RID: 3030 RVA: 0x0002F538 File Offset: 0x0002D738
		internal override int GetDeepHashCode()
		{
			int num = ((this._value != null) ? this._value.GetHashCode() : 0);
			int valueType = (int)this._valueType;
			return valueType.GetHashCode() ^ num;
		}

		// Token: 0x06000BD7 RID: 3031 RVA: 0x0002F56C File Offset: 0x0002D76C
		[NullableContext(1)]
		private static bool ValuesEquals(JValue v1, JValue v2)
		{
			return v1 == v2 || (v1._valueType == v2._valueType && JValue.Compare(v1._valueType, v1._value, v2._value) == 0);
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		// Token: 0x06000BD8 RID: 3032 RVA: 0x0002F59E File Offset: 0x0002D79E
		public bool Equals(JValue other)
		{
			return other != null && JValue.ValuesEquals(this, other);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />.</param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000BD9 RID: 3033 RVA: 0x0002F5AC File Offset: 0x0002D7AC
		public override bool Equals(object obj)
		{
			JValue jvalue = obj as JValue;
			return jvalue != null && this.Equals(jvalue);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object" />.
		/// </returns>
		// Token: 0x06000BDA RID: 3034 RVA: 0x0002F5CC File Offset: 0x0002D7CC
		public override int GetHashCode()
		{
			if (this._value == null)
			{
				return 0;
			}
			return this._value.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents this instance.
		/// </summary>
		/// <remarks>
		/// <c>ToString()</c> returns a non-JSON string value for tokens with a type of <see cref="F:Newtonsoft.Json.Linq.JTokenType.String" />.
		/// If you want the JSON for all token types then you should use <see cref="M:Newtonsoft.Json.Linq.JValue.WriteTo(Newtonsoft.Json.JsonWriter,Newtonsoft.Json.JsonConverter[])" />.
		/// </remarks>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents this instance.
		/// </returns>
		// Token: 0x06000BDB RID: 3035 RVA: 0x0002F5E3 File Offset: 0x0002D7E3
		[NullableContext(1)]
		public override string ToString()
		{
			if (this._value == null)
			{
				return string.Empty;
			}
			return this._value.ToString();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents this instance.
		/// </summary>
		/// <param name="format">The format.</param>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents this instance.
		/// </returns>
		// Token: 0x06000BDC RID: 3036 RVA: 0x0002F5FE File Offset: 0x0002D7FE
		[NullableContext(1)]
		public string ToString(string format)
		{
			return this.ToString(format, CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents this instance.
		/// </summary>
		/// <param name="formatProvider">The format provider.</param>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents this instance.
		/// </returns>
		// Token: 0x06000BDD RID: 3037 RVA: 0x0002F60C File Offset: 0x0002D80C
		[NullableContext(1)]
		public string ToString([Nullable(2)] IFormatProvider formatProvider)
		{
			return this.ToString(null, formatProvider);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents this instance.
		/// </summary>
		/// <param name="format">The format.</param>
		/// <param name="formatProvider">The format provider.</param>
		/// <returns>
		/// A <see cref="T:System.String" /> that represents this instance.
		/// </returns>
		// Token: 0x06000BDE RID: 3038 RVA: 0x0002F618 File Offset: 0x0002D818
		[return: Nullable(1)]
		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (this._value == null)
			{
				return string.Empty;
			}
			IFormattable formattable = this._value as IFormattable;
			if (formattable != null)
			{
				return formattable.ToString(format, formatProvider);
			}
			return this._value.ToString();
		}

		/// <summary>
		/// Returns the <see cref="T:System.Dynamic.DynamicMetaObject" /> responsible for binding operations performed on this object.
		/// </summary>
		/// <param name="parameter">The expression tree representation of the runtime value.</param>
		/// <returns>
		/// The <see cref="T:System.Dynamic.DynamicMetaObject" /> to bind this object.
		/// </returns>
		// Token: 0x06000BDF RID: 3039 RVA: 0x0002F656 File Offset: 0x0002D856
		[NullableContext(1)]
		protected override DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return new DynamicProxyMetaObject<JValue>(parameter, this, new JValue.JValueDynamicProxy());
		}

		// Token: 0x06000BE0 RID: 3040 RVA: 0x0002F664 File Offset: 0x0002D864
		int IComparable.CompareTo(object obj)
		{
			if (obj == null)
			{
				return 1;
			}
			JValue jvalue = obj as JValue;
			object objB;
			JTokenType valueType;
			if (jvalue != null)
			{
				objB = jvalue.Value;
				valueType = ((this._valueType == JTokenType.String && this._valueType != jvalue._valueType) ? jvalue._valueType : this._valueType);
			}
			else
			{
				objB = obj;
				valueType = this._valueType;
			}
			return JValue.Compare(valueType, this._value, objB);
		}

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This instance is less than <paramref name="obj" />.
		/// Zero
		/// This instance is equal to <paramref name="obj" />.
		/// Greater than zero
		/// This instance is greater than <paramref name="obj" />.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// 	<paramref name="obj" /> is not of the same type as this instance.
		/// </exception>
		// Token: 0x06000BE1 RID: 3041 RVA: 0x0002F6C5 File Offset: 0x0002D8C5
		public int CompareTo(JValue obj)
		{
			if (obj == null)
			{
				return 1;
			}
			return JValue.Compare((this._valueType == JTokenType.String && this._valueType != obj._valueType) ? obj._valueType : this._valueType, this._value, obj._value);
		}

		// Token: 0x06000BE2 RID: 3042 RVA: 0x0002F704 File Offset: 0x0002D904
		TypeCode IConvertible.GetTypeCode()
		{
			if (this._value == null)
			{
				return TypeCode.Empty;
			}
			IConvertible convertible = this._value as IConvertible;
			if (convertible != null)
			{
				return convertible.GetTypeCode();
			}
			return TypeCode.Object;
		}

		// Token: 0x06000BE3 RID: 3043 RVA: 0x0002F732 File Offset: 0x0002D932
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return (bool)this;
		}

		// Token: 0x06000BE4 RID: 3044 RVA: 0x0002F73A File Offset: 0x0002D93A
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return (char)this;
		}

		// Token: 0x06000BE5 RID: 3045 RVA: 0x0002F742 File Offset: 0x0002D942
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return (sbyte)this;
		}

		// Token: 0x06000BE6 RID: 3046 RVA: 0x0002F74A File Offset: 0x0002D94A
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return (byte)this;
		}

		// Token: 0x06000BE7 RID: 3047 RVA: 0x0002F752 File Offset: 0x0002D952
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return (short)this;
		}

		// Token: 0x06000BE8 RID: 3048 RVA: 0x0002F75A File Offset: 0x0002D95A
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return (ushort)this;
		}

		// Token: 0x06000BE9 RID: 3049 RVA: 0x0002F762 File Offset: 0x0002D962
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return (int)this;
		}

		// Token: 0x06000BEA RID: 3050 RVA: 0x0002F76A File Offset: 0x0002D96A
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return (uint)this;
		}

		// Token: 0x06000BEB RID: 3051 RVA: 0x0002F772 File Offset: 0x0002D972
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return (long)this;
		}

		// Token: 0x06000BEC RID: 3052 RVA: 0x0002F77A File Offset: 0x0002D97A
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return (ulong)this;
		}

		// Token: 0x06000BED RID: 3053 RVA: 0x0002F782 File Offset: 0x0002D982
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return (float)this;
		}

		// Token: 0x06000BEE RID: 3054 RVA: 0x0002F78B File Offset: 0x0002D98B
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return (double)this;
		}

		// Token: 0x06000BEF RID: 3055 RVA: 0x0002F794 File Offset: 0x0002D994
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return (decimal)this;
		}

		// Token: 0x06000BF0 RID: 3056 RVA: 0x0002F79C File Offset: 0x0002D99C
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			return (DateTime)this;
		}

		// Token: 0x06000BF1 RID: 3057 RVA: 0x0002F7A4 File Offset: 0x0002D9A4
		[NullableContext(1)]
		object IConvertible.ToType(Type conversionType, [Nullable(2)] IFormatProvider provider)
		{
			return base.ToObject(conversionType);
		}

		// Token: 0x040003BE RID: 958
		private JTokenType _valueType;

		// Token: 0x040003BF RID: 959
		private object _value;

		// Token: 0x020001D6 RID: 470
		[NullableContext(1)]
		[Nullable(new byte[] { 0, 1 })]
		private class JValueDynamicProxy : DynamicProxy<JValue>
		{
			// Token: 0x06000FFA RID: 4090 RVA: 0x000466EC File Offset: 0x000448EC
			public override bool TryConvert(JValue instance, ConvertBinder binder, [Nullable(2)] [NotNullWhen(true)] out object result)
			{
				if (binder.Type == typeof(JValue) || binder.Type == typeof(JToken))
				{
					result = instance;
					return true;
				}
				object value = instance.Value;
				if (value == null)
				{
					result = null;
					return ReflectionUtils.IsNullable(binder.Type);
				}
				result = ConvertUtils.Convert(value, CultureInfo.InvariantCulture, binder.Type);
				return true;
			}

			// Token: 0x06000FFB RID: 4091 RVA: 0x0004675C File Offset: 0x0004495C
			public override bool TryBinaryOperation(JValue instance, BinaryOperationBinder binder, object arg, [Nullable(2)] [NotNullWhen(true)] out object result)
			{
				JValue jvalue = arg as JValue;
				object objB = ((jvalue != null) ? jvalue.Value : arg);
				ExpressionType operation = binder.Operation;
				if (operation <= ExpressionType.NotEqual)
				{
					if (operation <= ExpressionType.LessThanOrEqual)
					{
						if (operation != ExpressionType.Add)
						{
							switch (operation)
							{
							case ExpressionType.Divide:
								break;
							case ExpressionType.Equal:
								result = JValue.Compare(instance.Type, instance.Value, objB) == 0;
								return true;
							case ExpressionType.ExclusiveOr:
							case ExpressionType.Invoke:
							case ExpressionType.Lambda:
							case ExpressionType.LeftShift:
								goto IL_18D;
							case ExpressionType.GreaterThan:
								result = JValue.Compare(instance.Type, instance.Value, objB) > 0;
								return true;
							case ExpressionType.GreaterThanOrEqual:
								result = JValue.Compare(instance.Type, instance.Value, objB) >= 0;
								return true;
							case ExpressionType.LessThan:
								result = JValue.Compare(instance.Type, instance.Value, objB) < 0;
								return true;
							case ExpressionType.LessThanOrEqual:
								result = JValue.Compare(instance.Type, instance.Value, objB) <= 0;
								return true;
							default:
								goto IL_18D;
							}
						}
					}
					else if (operation != ExpressionType.Multiply)
					{
						if (operation != ExpressionType.NotEqual)
						{
							goto IL_18D;
						}
						result = JValue.Compare(instance.Type, instance.Value, objB) != 0;
						return true;
					}
				}
				else if (operation <= ExpressionType.AddAssign)
				{
					if (operation != ExpressionType.Subtract && operation != ExpressionType.AddAssign)
					{
						goto IL_18D;
					}
				}
				else if (operation != ExpressionType.DivideAssign && operation != ExpressionType.MultiplyAssign && operation != ExpressionType.SubtractAssign)
				{
					goto IL_18D;
				}
				if (JValue.Operation(binder.Operation, instance.Value, objB, out result))
				{
					result = new JValue(result);
					return true;
				}
				IL_18D:
				result = null;
				return false;
			}
		}
	}
}
