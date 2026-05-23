using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E2 RID: 2018
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapYearMonth : ISoapXsd
	{
		// Token: 0x17000E67 RID: 3687
		// (get) Token: 0x0600573E RID: 22334 RVA: 0x00135A84 File Offset: 0x00133C84
		public static string XsdType
		{
			get
			{
				return "gYearMonth";
			}
		}

		// Token: 0x0600573F RID: 22335 RVA: 0x00135A8B File Offset: 0x00133C8B
		public string GetXsdType()
		{
			return SoapYearMonth.XsdType;
		}

		// Token: 0x06005740 RID: 22336 RVA: 0x00135A92 File Offset: 0x00133C92
		public SoapYearMonth()
		{
		}

		// Token: 0x06005741 RID: 22337 RVA: 0x00135AA5 File Offset: 0x00133CA5
		public SoapYearMonth(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x06005742 RID: 22338 RVA: 0x00135ABF File Offset: 0x00133CBF
		public SoapYearMonth(DateTime value, int sign)
		{
			this._value = value;
			this._sign = sign;
		}

		// Token: 0x17000E68 RID: 3688
		// (get) Token: 0x06005743 RID: 22339 RVA: 0x00135AE0 File Offset: 0x00133CE0
		// (set) Token: 0x06005744 RID: 22340 RVA: 0x00135AE8 File Offset: 0x00133CE8
		public DateTime Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = value;
			}
		}

		// Token: 0x17000E69 RID: 3689
		// (get) Token: 0x06005745 RID: 22341 RVA: 0x00135AF1 File Offset: 0x00133CF1
		// (set) Token: 0x06005746 RID: 22342 RVA: 0x00135AF9 File Offset: 0x00133CF9
		public int Sign
		{
			get
			{
				return this._sign;
			}
			set
			{
				this._sign = value;
			}
		}

		// Token: 0x06005747 RID: 22343 RVA: 0x00135B02 File Offset: 0x00133D02
		public override string ToString()
		{
			if (this._sign < 0)
			{
				return this._value.ToString("'-'yyyy-MM", CultureInfo.InvariantCulture);
			}
			return this._value.ToString("yyyy-MM", CultureInfo.InvariantCulture);
		}

		// Token: 0x06005748 RID: 22344 RVA: 0x00135B38 File Offset: 0x00133D38
		public static SoapYearMonth Parse(string value)
		{
			int sign = 0;
			if (value[0] == '-')
			{
				sign = -1;
			}
			return new SoapYearMonth(DateTime.ParseExact(value, SoapYearMonth.formats, CultureInfo.InvariantCulture, DateTimeStyles.None), sign);
		}

		// Token: 0x0400280C RID: 10252
		private DateTime _value = DateTime.MinValue;

		// Token: 0x0400280D RID: 10253
		private int _sign;

		// Token: 0x0400280E RID: 10254
		private static string[] formats = new string[] { "yyyy-MM", "'+'yyyy-MM", "'-'yyyy-MM", "yyyy-MMzzz", "'+'yyyy-MMzzz", "'-'yyyy-MMzzz" };
	}
}
