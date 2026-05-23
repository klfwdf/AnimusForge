using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F1 RID: 2033
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNormalizedString : ISoapXsd
	{
		// Token: 0x17000E89 RID: 3721
		// (get) Token: 0x060057C9 RID: 22473 RVA: 0x001366B3 File Offset: 0x001348B3
		public static string XsdType
		{
			get
			{
				return "normalizedString";
			}
		}

		// Token: 0x060057CA RID: 22474 RVA: 0x001366BA File Offset: 0x001348BA
		public string GetXsdType()
		{
			return SoapNormalizedString.XsdType;
		}

		// Token: 0x060057CB RID: 22475 RVA: 0x001366C1 File Offset: 0x001348C1
		public SoapNormalizedString()
		{
		}

		// Token: 0x060057CC RID: 22476 RVA: 0x001366C9 File Offset: 0x001348C9
		public SoapNormalizedString(string value)
		{
			this._value = this.Validate(value);
		}

		// Token: 0x17000E8A RID: 3722
		// (get) Token: 0x060057CD RID: 22477 RVA: 0x001366DE File Offset: 0x001348DE
		// (set) Token: 0x060057CE RID: 22478 RVA: 0x001366E6 File Offset: 0x001348E6
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = this.Validate(value);
			}
		}

		// Token: 0x060057CF RID: 22479 RVA: 0x001366F5 File Offset: 0x001348F5
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x060057D0 RID: 22480 RVA: 0x00136702 File Offset: 0x00134902
		public static SoapNormalizedString Parse(string value)
		{
			return new SoapNormalizedString(value);
		}

		// Token: 0x060057D1 RID: 22481 RVA: 0x0013670C File Offset: 0x0013490C
		private string Validate(string value)
		{
			if (value == null || value.Length == 0)
			{
				return value;
			}
			char[] anyOf = new char[] { '\r', '\n', '\t' };
			int num = value.LastIndexOfAny(anyOf);
			if (num > -1)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid", new object[] { "xsd:normalizedString", value }));
			}
			return value;
		}

		// Token: 0x04002825 RID: 10277
		private string _value;
	}
}
