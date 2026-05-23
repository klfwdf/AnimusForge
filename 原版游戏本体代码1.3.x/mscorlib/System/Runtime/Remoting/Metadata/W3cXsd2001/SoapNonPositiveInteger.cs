using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007EB RID: 2027
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNonPositiveInteger : ISoapXsd
	{
		// Token: 0x17000E7B RID: 3707
		// (get) Token: 0x06005793 RID: 22419 RVA: 0x00136261 File Offset: 0x00134461
		public static string XsdType
		{
			get
			{
				return "nonPositiveInteger";
			}
		}

		// Token: 0x06005794 RID: 22420 RVA: 0x00136268 File Offset: 0x00134468
		public string GetXsdType()
		{
			return SoapNonPositiveInteger.XsdType;
		}

		// Token: 0x06005795 RID: 22421 RVA: 0x0013626F File Offset: 0x0013446F
		public SoapNonPositiveInteger()
		{
		}

		// Token: 0x06005796 RID: 22422 RVA: 0x00136278 File Offset: 0x00134478
		public SoapNonPositiveInteger(decimal value)
		{
			this._value = decimal.Truncate(value);
			if (this._value > 0m)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:nonPositiveInteger", value));
			}
		}

		// Token: 0x17000E7C RID: 3708
		// (get) Token: 0x06005797 RID: 22423 RVA: 0x001362CE File Offset: 0x001344CE
		// (set) Token: 0x06005798 RID: 22424 RVA: 0x001362D8 File Offset: 0x001344D8
		public decimal Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = decimal.Truncate(value);
				if (this._value > 0m)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:nonPositiveInteger", value));
				}
			}
		}

		// Token: 0x06005799 RID: 22425 RVA: 0x00136328 File Offset: 0x00134528
		public override string ToString()
		{
			return this._value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x0600579A RID: 22426 RVA: 0x0013633A File Offset: 0x0013453A
		public static SoapNonPositiveInteger Parse(string value)
		{
			return new SoapNonPositiveInteger(decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		// Token: 0x0400281D RID: 10269
		private decimal _value;
	}
}
