using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E3 RID: 2019
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapYear : ISoapXsd
	{
		// Token: 0x17000E6A RID: 3690
		// (get) Token: 0x0600574A RID: 22346 RVA: 0x00135BA8 File Offset: 0x00133DA8
		public static string XsdType
		{
			get
			{
				return "gYear";
			}
		}

		// Token: 0x0600574B RID: 22347 RVA: 0x00135BAF File Offset: 0x00133DAF
		public string GetXsdType()
		{
			return SoapYear.XsdType;
		}

		// Token: 0x0600574C RID: 22348 RVA: 0x00135BB6 File Offset: 0x00133DB6
		public SoapYear()
		{
		}

		// Token: 0x0600574D RID: 22349 RVA: 0x00135BC9 File Offset: 0x00133DC9
		public SoapYear(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x0600574E RID: 22350 RVA: 0x00135BE3 File Offset: 0x00133DE3
		public SoapYear(DateTime value, int sign)
		{
			this._value = value;
			this._sign = sign;
		}

		// Token: 0x17000E6B RID: 3691
		// (get) Token: 0x0600574F RID: 22351 RVA: 0x00135C04 File Offset: 0x00133E04
		// (set) Token: 0x06005750 RID: 22352 RVA: 0x00135C0C File Offset: 0x00133E0C
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

		// Token: 0x17000E6C RID: 3692
		// (get) Token: 0x06005751 RID: 22353 RVA: 0x00135C15 File Offset: 0x00133E15
		// (set) Token: 0x06005752 RID: 22354 RVA: 0x00135C1D File Offset: 0x00133E1D
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

		// Token: 0x06005753 RID: 22355 RVA: 0x00135C26 File Offset: 0x00133E26
		public override string ToString()
		{
			if (this._sign < 0)
			{
				return this._value.ToString("'-'yyyy", CultureInfo.InvariantCulture);
			}
			return this._value.ToString("yyyy", CultureInfo.InvariantCulture);
		}

		// Token: 0x06005754 RID: 22356 RVA: 0x00135C5C File Offset: 0x00133E5C
		public static SoapYear Parse(string value)
		{
			int sign = 0;
			if (value[0] == '-')
			{
				sign = -1;
			}
			return new SoapYear(DateTime.ParseExact(value, SoapYear.formats, CultureInfo.InvariantCulture, DateTimeStyles.None), sign);
		}

		// Token: 0x0400280F RID: 10255
		private DateTime _value = DateTime.MinValue;

		// Token: 0x04002810 RID: 10256
		private int _sign;

		// Token: 0x04002811 RID: 10257
		private static string[] formats = new string[] { "yyyy", "'+'yyyy", "'-'yyyy", "yyyyzzz", "'+'yyyyzzz", "'-'yyyyzzz" };
	}
}
