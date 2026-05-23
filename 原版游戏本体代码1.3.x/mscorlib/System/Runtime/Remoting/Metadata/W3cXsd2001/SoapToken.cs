using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F2 RID: 2034
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapToken : ISoapXsd
	{
		// Token: 0x17000E8B RID: 3723
		// (get) Token: 0x060057D2 RID: 22482 RVA: 0x00136767 File Offset: 0x00134967
		public static string XsdType
		{
			get
			{
				return "token";
			}
		}

		// Token: 0x060057D3 RID: 22483 RVA: 0x0013676E File Offset: 0x0013496E
		public string GetXsdType()
		{
			return SoapToken.XsdType;
		}

		// Token: 0x060057D4 RID: 22484 RVA: 0x00136775 File Offset: 0x00134975
		public SoapToken()
		{
		}

		// Token: 0x060057D5 RID: 22485 RVA: 0x0013677D File Offset: 0x0013497D
		public SoapToken(string value)
		{
			this._value = this.Validate(value);
		}

		// Token: 0x17000E8C RID: 3724
		// (get) Token: 0x060057D6 RID: 22486 RVA: 0x00136792 File Offset: 0x00134992
		// (set) Token: 0x060057D7 RID: 22487 RVA: 0x0013679A File Offset: 0x0013499A
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

		// Token: 0x060057D8 RID: 22488 RVA: 0x001367A9 File Offset: 0x001349A9
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x060057D9 RID: 22489 RVA: 0x001367B6 File Offset: 0x001349B6
		public static SoapToken Parse(string value)
		{
			return new SoapToken(value);
		}

		// Token: 0x060057DA RID: 22490 RVA: 0x001367C0 File Offset: 0x001349C0
		private string Validate(string value)
		{
			if (value == null || value.Length == 0)
			{
				return value;
			}
			char[] anyOf = new char[] { '\r', '\t' };
			int num = value.LastIndexOfAny(anyOf);
			if (num > -1)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid", new object[] { "xsd:token", value }));
			}
			if (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])))
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid", new object[] { "xsd:token", value }));
			}
			num = value.IndexOf("  ");
			if (num > -1)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_SOAPInteropxsdInvalid", new object[] { "xsd:token", value }));
			}
			return value;
		}

		// Token: 0x04002826 RID: 10278
		private string _value;
	}
}
