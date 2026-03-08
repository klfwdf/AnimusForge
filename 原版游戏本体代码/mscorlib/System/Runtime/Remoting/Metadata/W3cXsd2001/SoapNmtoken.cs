using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F7 RID: 2039
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNmtoken : ISoapXsd
	{
		// Token: 0x17000E95 RID: 3733
		// (get) Token: 0x060057FB RID: 22523 RVA: 0x001369C6 File Offset: 0x00134BC6
		public static string XsdType
		{
			get
			{
				return "NMTOKEN";
			}
		}

		// Token: 0x060057FC RID: 22524 RVA: 0x001369CD File Offset: 0x00134BCD
		public string GetXsdType()
		{
			return SoapNmtoken.XsdType;
		}

		// Token: 0x060057FD RID: 22525 RVA: 0x001369D4 File Offset: 0x00134BD4
		public SoapNmtoken()
		{
		}

		// Token: 0x060057FE RID: 22526 RVA: 0x001369DC File Offset: 0x00134BDC
		public SoapNmtoken(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E96 RID: 3734
		// (get) Token: 0x060057FF RID: 22527 RVA: 0x001369EB File Offset: 0x00134BEB
		// (set) Token: 0x06005800 RID: 22528 RVA: 0x001369F3 File Offset: 0x00134BF3
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

		// Token: 0x06005801 RID: 22529 RVA: 0x001369FC File Offset: 0x00134BFC
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x06005802 RID: 22530 RVA: 0x00136A09 File Offset: 0x00134C09
		public static SoapNmtoken Parse(string value)
		{
			return new SoapNmtoken(value);
		}

		// Token: 0x0400282B RID: 10283
		private string _value;
	}
}
