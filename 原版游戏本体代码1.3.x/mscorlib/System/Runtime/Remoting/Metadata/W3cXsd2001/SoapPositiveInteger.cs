using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007EA RID: 2026
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapPositiveInteger : ISoapXsd
	{
		// Token: 0x17000E79 RID: 3705
		// (get) Token: 0x0600578B RID: 22411 RVA: 0x00136175 File Offset: 0x00134375
		public static string XsdType
		{
			get
			{
				return "positiveInteger";
			}
		}

		// Token: 0x0600578C RID: 22412 RVA: 0x0013617C File Offset: 0x0013437C
		public string GetXsdType()
		{
			return SoapPositiveInteger.XsdType;
		}

		// Token: 0x0600578D RID: 22413 RVA: 0x00136183 File Offset: 0x00134383
		public SoapPositiveInteger()
		{
		}

		// Token: 0x0600578E RID: 22414 RVA: 0x0013618C File Offset: 0x0013438C
		public SoapPositiveInteger(decimal value)
		{
			this._value = decimal.Truncate(value);
			if (this._value < 1m)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:positiveInteger", value));
			}
		}

		// Token: 0x17000E7A RID: 3706
		// (get) Token: 0x0600578F RID: 22415 RVA: 0x001361E2 File Offset: 0x001343E2
		// (set) Token: 0x06005790 RID: 22416 RVA: 0x001361EC File Offset: 0x001343EC
		public decimal Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = decimal.Truncate(value);
				if (this._value < 1m)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid"), "xsd:positiveInteger", value));
				}
			}
		}

		// Token: 0x06005791 RID: 22417 RVA: 0x0013623C File Offset: 0x0013443C
		public override string ToString()
		{
			return this._value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x06005792 RID: 22418 RVA: 0x0013624E File Offset: 0x0013444E
		public static SoapPositiveInteger Parse(string value)
		{
			return new SoapPositiveInteger(decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		// Token: 0x0400281C RID: 10268
		private decimal _value;
	}
}
