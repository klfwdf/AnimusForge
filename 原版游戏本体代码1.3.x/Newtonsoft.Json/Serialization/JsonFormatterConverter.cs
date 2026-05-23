using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	// Token: 0x0200008E RID: 142
	[NullableContext(1)]
	[Nullable(0)]
	internal class JsonFormatterConverter : IFormatterConverter
	{
		// Token: 0x060006ED RID: 1773 RVA: 0x0001D349 File Offset: 0x0001B549
		public JsonFormatterConverter(JsonSerializerInternalReader reader, JsonISerializableContract contract, [Nullable(2)] JsonProperty member)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			ValidationUtils.ArgumentNotNull(contract, "contract");
			this._reader = reader;
			this._contract = contract;
			this._member = member;
		}

		// Token: 0x060006EE RID: 1774 RVA: 0x0001D37C File Offset: 0x0001B57C
		private T GetTokenValue<[Nullable(2)] T>(object value)
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			return (T)((object)System.Convert.ChangeType(((JValue)value).Value, typeof(T), CultureInfo.InvariantCulture));
		}

		// Token: 0x060006EF RID: 1775 RVA: 0x0001D3B0 File Offset: 0x0001B5B0
		public object Convert(object value, Type type)
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			JToken jtoken = value as JToken;
			if (jtoken == null)
			{
				throw new ArgumentException("Value is not a JToken.", "value");
			}
			return this._reader.CreateISerializableItem(jtoken, type, this._contract, this._member);
		}

		// Token: 0x060006F0 RID: 1776 RVA: 0x0001D3FC File Offset: 0x0001B5FC
		public object Convert(object value, TypeCode typeCode)
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			JValue jvalue = value as JValue;
			return System.Convert.ChangeType((jvalue != null) ? jvalue.Value : value, typeCode, CultureInfo.InvariantCulture);
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x0001D432 File Offset: 0x0001B632
		public bool ToBoolean(object value)
		{
			return this.GetTokenValue<bool>(value);
		}

		// Token: 0x060006F2 RID: 1778 RVA: 0x0001D43B File Offset: 0x0001B63B
		public byte ToByte(object value)
		{
			return this.GetTokenValue<byte>(value);
		}

		// Token: 0x060006F3 RID: 1779 RVA: 0x0001D444 File Offset: 0x0001B644
		public char ToChar(object value)
		{
			return this.GetTokenValue<char>(value);
		}

		// Token: 0x060006F4 RID: 1780 RVA: 0x0001D44D File Offset: 0x0001B64D
		public DateTime ToDateTime(object value)
		{
			return this.GetTokenValue<DateTime>(value);
		}

		// Token: 0x060006F5 RID: 1781 RVA: 0x0001D456 File Offset: 0x0001B656
		public decimal ToDecimal(object value)
		{
			return this.GetTokenValue<decimal>(value);
		}

		// Token: 0x060006F6 RID: 1782 RVA: 0x0001D45F File Offset: 0x0001B65F
		public double ToDouble(object value)
		{
			return this.GetTokenValue<double>(value);
		}

		// Token: 0x060006F7 RID: 1783 RVA: 0x0001D468 File Offset: 0x0001B668
		public short ToInt16(object value)
		{
			return this.GetTokenValue<short>(value);
		}

		// Token: 0x060006F8 RID: 1784 RVA: 0x0001D471 File Offset: 0x0001B671
		public int ToInt32(object value)
		{
			return this.GetTokenValue<int>(value);
		}

		// Token: 0x060006F9 RID: 1785 RVA: 0x0001D47A File Offset: 0x0001B67A
		public long ToInt64(object value)
		{
			return this.GetTokenValue<long>(value);
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x0001D483 File Offset: 0x0001B683
		public sbyte ToSByte(object value)
		{
			return this.GetTokenValue<sbyte>(value);
		}

		// Token: 0x060006FB RID: 1787 RVA: 0x0001D48C File Offset: 0x0001B68C
		public float ToSingle(object value)
		{
			return this.GetTokenValue<float>(value);
		}

		// Token: 0x060006FC RID: 1788 RVA: 0x0001D495 File Offset: 0x0001B695
		public string ToString(object value)
		{
			return this.GetTokenValue<string>(value);
		}

		// Token: 0x060006FD RID: 1789 RVA: 0x0001D49E File Offset: 0x0001B69E
		public ushort ToUInt16(object value)
		{
			return this.GetTokenValue<ushort>(value);
		}

		// Token: 0x060006FE RID: 1790 RVA: 0x0001D4A7 File Offset: 0x0001B6A7
		public uint ToUInt32(object value)
		{
			return this.GetTokenValue<uint>(value);
		}

		// Token: 0x060006FF RID: 1791 RVA: 0x0001D4B0 File Offset: 0x0001B6B0
		public ulong ToUInt64(object value)
		{
			return this.GetTokenValue<ulong>(value);
		}

		// Token: 0x0400028F RID: 655
		private readonly JsonSerializerInternalReader _reader;

		// Token: 0x04000290 RID: 656
		private readonly JsonISerializableContract _contract;

		// Token: 0x04000291 RID: 657
		[Nullable(2)]
		private readonly JsonProperty _member;
	}
}
