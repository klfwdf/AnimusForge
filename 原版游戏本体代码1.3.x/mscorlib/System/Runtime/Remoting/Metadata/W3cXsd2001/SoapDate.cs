using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E1 RID: 2017
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapDate : ISoapXsd
	{
		// Token: 0x17000E64 RID: 3684
		// (get) Token: 0x06005732 RID: 22322 RVA: 0x0013591A File Offset: 0x00133B1A
		public static string XsdType
		{
			get
			{
				return "date";
			}
		}

		// Token: 0x06005733 RID: 22323 RVA: 0x00135921 File Offset: 0x00133B21
		public string GetXsdType()
		{
			return SoapDate.XsdType;
		}

		// Token: 0x06005734 RID: 22324 RVA: 0x00135928 File Offset: 0x00133B28
		public SoapDate()
		{
		}

		// Token: 0x06005735 RID: 22325 RVA: 0x00135950 File Offset: 0x00133B50
		public SoapDate(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x06005736 RID: 22326 RVA: 0x00135980 File Offset: 0x00133B80
		public SoapDate(DateTime value, int sign)
		{
			this._value = value;
			this._sign = sign;
		}

		// Token: 0x17000E65 RID: 3685
		// (get) Token: 0x06005737 RID: 22327 RVA: 0x001359B4 File Offset: 0x00133BB4
		// (set) Token: 0x06005738 RID: 22328 RVA: 0x001359BC File Offset: 0x00133BBC
		public DateTime Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = value.Date;
			}
		}

		// Token: 0x17000E66 RID: 3686
		// (get) Token: 0x06005739 RID: 22329 RVA: 0x001359CB File Offset: 0x00133BCB
		// (set) Token: 0x0600573A RID: 22330 RVA: 0x001359D3 File Offset: 0x00133BD3
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

		// Token: 0x0600573B RID: 22331 RVA: 0x001359DC File Offset: 0x00133BDC
		public override string ToString()
		{
			if (this._sign < 0)
			{
				return this._value.ToString("'-'yyyy-MM-dd", CultureInfo.InvariantCulture);
			}
			return this._value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		// Token: 0x0600573C RID: 22332 RVA: 0x00135A14 File Offset: 0x00133C14
		public static SoapDate Parse(string value)
		{
			int sign = 0;
			if (value[0] == '-')
			{
				sign = -1;
			}
			return new SoapDate(DateTime.ParseExact(value, SoapDate.formats, CultureInfo.InvariantCulture, DateTimeStyles.None), sign);
		}

		// Token: 0x04002809 RID: 10249
		private DateTime _value = DateTime.MinValue.Date;

		// Token: 0x0400280A RID: 10250
		private int _sign;

		// Token: 0x0400280B RID: 10251
		private static string[] formats = new string[] { "yyyy-MM-dd", "'+'yyyy-MM-dd", "'-'yyyy-MM-dd", "yyyy-MM-ddzzz", "'+'yyyy-MM-ddzzz", "'-'yyyy-MM-ddzzz" };
	}
}
