using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E4 RID: 2020
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapMonthDay : ISoapXsd
	{
		// Token: 0x17000E6D RID: 3693
		// (get) Token: 0x06005756 RID: 22358 RVA: 0x00135CCC File Offset: 0x00133ECC
		public static string XsdType
		{
			get
			{
				return "gMonthDay";
			}
		}

		// Token: 0x06005757 RID: 22359 RVA: 0x00135CD3 File Offset: 0x00133ED3
		public string GetXsdType()
		{
			return SoapMonthDay.XsdType;
		}

		// Token: 0x06005758 RID: 22360 RVA: 0x00135CDA File Offset: 0x00133EDA
		public SoapMonthDay()
		{
		}

		// Token: 0x06005759 RID: 22361 RVA: 0x00135CED File Offset: 0x00133EED
		public SoapMonthDay(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x17000E6E RID: 3694
		// (get) Token: 0x0600575A RID: 22362 RVA: 0x00135D07 File Offset: 0x00133F07
		// (set) Token: 0x0600575B RID: 22363 RVA: 0x00135D0F File Offset: 0x00133F0F
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

		// Token: 0x0600575C RID: 22364 RVA: 0x00135D18 File Offset: 0x00133F18
		public override string ToString()
		{
			return this._value.ToString("'--'MM'-'dd", CultureInfo.InvariantCulture);
		}

		// Token: 0x0600575D RID: 22365 RVA: 0x00135D2F File Offset: 0x00133F2F
		public static SoapMonthDay Parse(string value)
		{
			return new SoapMonthDay(DateTime.ParseExact(value, SoapMonthDay.formats, CultureInfo.InvariantCulture, DateTimeStyles.None));
		}

		// Token: 0x04002812 RID: 10258
		private DateTime _value = DateTime.MinValue;

		// Token: 0x04002813 RID: 10259
		private static string[] formats = new string[] { "--MM-dd", "--MM-ddzzz" };
	}
}
