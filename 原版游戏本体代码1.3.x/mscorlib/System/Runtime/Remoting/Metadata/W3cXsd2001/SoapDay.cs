using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007E5 RID: 2021
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapDay : ISoapXsd
	{
		// Token: 0x17000E6F RID: 3695
		// (get) Token: 0x0600575F RID: 22367 RVA: 0x00135D64 File Offset: 0x00133F64
		public static string XsdType
		{
			get
			{
				return "gDay";
			}
		}

		// Token: 0x06005760 RID: 22368 RVA: 0x00135D6B File Offset: 0x00133F6B
		public string GetXsdType()
		{
			return SoapDay.XsdType;
		}

		// Token: 0x06005761 RID: 22369 RVA: 0x00135D72 File Offset: 0x00133F72
		public SoapDay()
		{
		}

		// Token: 0x06005762 RID: 22370 RVA: 0x00135D85 File Offset: 0x00133F85
		public SoapDay(DateTime value)
		{
			this._value = value;
		}

		// Token: 0x17000E70 RID: 3696
		// (get) Token: 0x06005763 RID: 22371 RVA: 0x00135D9F File Offset: 0x00133F9F
		// (set) Token: 0x06005764 RID: 22372 RVA: 0x00135DA7 File Offset: 0x00133FA7
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

		// Token: 0x06005765 RID: 22373 RVA: 0x00135DB0 File Offset: 0x00133FB0
		public override string ToString()
		{
			return this._value.ToString("---dd", CultureInfo.InvariantCulture);
		}

		// Token: 0x06005766 RID: 22374 RVA: 0x00135DC7 File Offset: 0x00133FC7
		public static SoapDay Parse(string value)
		{
			return new SoapDay(DateTime.ParseExact(value, SoapDay.formats, CultureInfo.InvariantCulture, DateTimeStyles.None));
		}

		// Token: 0x04002814 RID: 10260
		private DateTime _value = DateTime.MinValue;

		// Token: 0x04002815 RID: 10261
		private static string[] formats = new string[] { "---dd", "---ddzzz" };
	}
}
