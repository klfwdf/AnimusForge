using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007EC RID: 2028
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNonNegativeInteger : ISoapXsd
	{
		// Token: 0x17000E7D RID: 3709
		// (get) Token: 0x0600579B RID: 22427 RVA: 0x0013634D File Offset: 0x0013454D
		public static string XsdType
		{
			get
			{
				return "nonNegativeInteger";
			}
		}

		// Token: 0x0600579C RID: 22428 RVA: 0x00136354 File Offset: 0x00134554
		public string GetXsdType()
		{
			return SoapNonNegativeInteger.XsdType;
		}

		// Token: 0x0600579D RID: 22429 RVA: 0x0013635B File Offset: 0x0013455B
		public SoapNonNegativeInteger()
		{
		}

		// Token: 0x0600579E RID: 22430 RVA: 0x00136364 File Offset: 0x00134564
		public SoapNonNegativeInteger(decimal value)
		{
			this._value = decimal.Truncate(value);
			if (this._value < 0m)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:nonNegativeInteger", value));
			}
		}

		// Token: 0x17000E7E RID: 3710
		// (get) Token: 0x0600579F RID: 22431 RVA: 0x001363BA File Offset: 0x001345BA
		// (set) Token: 0x060057A0 RID: 22432 RVA: 0x001363C4 File Offset: 0x001345C4
		public decimal Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = decimal.Truncate(value);
				if (this._value < 0m)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:nonNegativeInteger", value));
				}
			}
		}

		// Token: 0x060057A1 RID: 22433 RVA: 0x00136414 File Offset: 0x00134614
		public override string ToString()
		{
			return this._value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x060057A2 RID: 22434 RVA: 0x00136426 File Offset: 0x00134626
		public static SoapNonNegativeInteger Parse(string value)
		{
			return new SoapNonNegativeInteger(decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		// Token: 0x0400281E RID: 10270
		private decimal _value;
	}
}
