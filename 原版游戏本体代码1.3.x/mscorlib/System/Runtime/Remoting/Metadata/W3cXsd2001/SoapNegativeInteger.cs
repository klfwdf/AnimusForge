using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007ED RID: 2029
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNegativeInteger : ISoapXsd
	{
		// Token: 0x17000E7F RID: 3711
		// (get) Token: 0x060057A3 RID: 22435 RVA: 0x00136439 File Offset: 0x00134639
		public static string XsdType
		{
			get
			{
				return "negativeInteger";
			}
		}

		// Token: 0x060057A4 RID: 22436 RVA: 0x00136440 File Offset: 0x00134640
		public string GetXsdType()
		{
			return SoapNegativeInteger.XsdType;
		}

		// Token: 0x060057A5 RID: 22437 RVA: 0x00136447 File Offset: 0x00134647
		public SoapNegativeInteger()
		{
		}

		// Token: 0x060057A6 RID: 22438 RVA: 0x00136450 File Offset: 0x00134650
		public SoapNegativeInteger(decimal value)
		{
			this._value = decimal.Truncate(value);
			if (value > -1m)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:negativeInteger", value));
			}
		}

		// Token: 0x17000E80 RID: 3712
		// (get) Token: 0x060057A7 RID: 22439 RVA: 0x001364A1 File Offset: 0x001346A1
		// (set) Token: 0x060057A8 RID: 22440 RVA: 0x001364AC File Offset: 0x001346AC
		public decimal Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = decimal.Truncate(value);
				if (this._value > -1m)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:negativeInteger", value));
				}
			}
		}

		// Token: 0x060057A9 RID: 22441 RVA: 0x001364FC File Offset: 0x001346FC
		public override string ToString()
		{
			return this._value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x060057AA RID: 22442 RVA: 0x0013650E File Offset: 0x0013470E
		public static SoapNegativeInteger Parse(string value)
		{
			return new SoapNegativeInteger(decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		// Token: 0x0400281F RID: 10271
		private decimal _value;
	}
}
