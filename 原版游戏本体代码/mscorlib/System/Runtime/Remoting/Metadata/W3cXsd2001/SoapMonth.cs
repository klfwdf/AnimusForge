using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E6 RID: 2022
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapMonth : ISoapXsd
	{
		// Token: 0x17000E71 RID: 3697
		// (get) Token: 0x06005768 RID: 22376 RVA: 0x00135DFC File Offset: 0x00133FFC
		public static string XsdType
		{
			get
			{
				return "gMonth";
			}
		}

		// Token: 0x06005769 RID: 22377 RVA: 0x00135E03 File Offset: 0x00134003
		public string GetXsdType()
		{
			return SoapMonth.XsdType;
		}

		// Token: 0x0600576A RID: 22378 RVA: 0x00135E0A File Offset: 0x0013400A
		public SoapMonth()
		{
		}

		// Token: 0x0600576B RID: 22379 RVA: 0x00135E1D File Offset: 0x0013401D
		public SoapMonth(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x17000E72 RID: 3698
		// (get) Token: 0x0600576C RID: 22380 RVA: 0x00135E37 File Offset: 0x00134037
		// (set) Token: 0x0600576D RID: 22381 RVA: 0x00135E3F File Offset: 0x0013403F
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

		// Token: 0x0600576E RID: 22382 RVA: 0x00135E48 File Offset: 0x00134048
		public override string ToString()
		{
			return this._value.ToString("--MM--", CultureInfo.InvariantCulture);
		}

		// Token: 0x0600576F RID: 22383 RVA: 0x00135E5F File Offset: 0x0013405F
		public static SoapMonth Parse(string value)
		{
			return new SoapMonth(DateTime.ParseExact(value, SoapMonth.formats, CultureInfo.InvariantCulture, DateTimeStyles.None));
		}

		// Token: 0x04002816 RID: 10262
		private DateTime _value = DateTime.MinValue;

		// Token: 0x04002817 RID: 10263
		private static string[] formats = new string[] { "--MM--", "--MM--zzz" };
	}
}
