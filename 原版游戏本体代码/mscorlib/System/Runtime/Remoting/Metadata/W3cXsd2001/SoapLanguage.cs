using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F3 RID: 2035
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapLanguage : ISoapXsd
	{
		// Token: 0x17000E8D RID: 3725
		// (get) Token: 0x060057DB RID: 22491 RVA: 0x0013689A File Offset: 0x00134A9A
		public static string XsdType
		{
			get
			{
				return "language";
			}
		}

		// Token: 0x060057DC RID: 22492 RVA: 0x001368A1 File Offset: 0x00134AA1
		public string GetXsdType()
		{
			return SoapLanguage.XsdType;
		}

		// Token: 0x060057DD RID: 22493 RVA: 0x001368A8 File Offset: 0x00134AA8
		public SoapLanguage()
		{
		}

		// Token: 0x060057DE RID: 22494 RVA: 0x001368B0 File Offset: 0x00134AB0
		public SoapLanguage(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E8E RID: 3726
		// (get) Token: 0x060057DF RID: 22495 RVA: 0x001368BF File Offset: 0x00134ABF
		// (set) Token: 0x060057E0 RID: 22496 RVA: 0x001368C7 File Offset: 0x00134AC7
		public string Value
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

		// Token: 0x060057E1 RID: 22497 RVA: 0x001368D0 File Offset: 0x00134AD0
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x060057E2 RID: 22498 RVA: 0x001368DD File Offset: 0x00134ADD
		public static SoapLanguage Parse(string value)
		{
			return new SoapLanguage(value);
		}

		// Token: 0x04002827 RID: 10279
		private string _value;
	}
}
