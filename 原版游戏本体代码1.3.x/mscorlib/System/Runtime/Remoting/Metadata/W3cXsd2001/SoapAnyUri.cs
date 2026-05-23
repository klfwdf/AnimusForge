using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007EE RID: 2030
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapAnyUri : ISoapXsd
	{
		// Token: 0x17000E81 RID: 3713
		// (get) Token: 0x060057AB RID: 22443 RVA: 0x00136521 File Offset: 0x00134721
		public static string XsdType
		{
			get
			{
				return "anyURI";
			}
		}

		// Token: 0x060057AC RID: 22444 RVA: 0x00136528 File Offset: 0x00134728
		public string GetXsdType()
		{
			return SoapAnyUri.XsdType;
		}

		// Token: 0x060057AD RID: 22445 RVA: 0x0013652F File Offset: 0x0013472F
		public SoapAnyUri()
		{
		}

		// Token: 0x060057AE RID: 22446 RVA: 0x00136537 File Offset: 0x00134737
		public SoapAnyUri(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E82 RID: 3714
		// (get) Token: 0x060057AF RID: 22447 RVA: 0x00136546 File Offset: 0x00134746
		// (set) Token: 0x060057B0 RID: 22448 RVA: 0x0013654E File Offset: 0x0013474E
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

		// Token: 0x060057B1 RID: 22449 RVA: 0x00136557 File Offset: 0x00134757
		public override string ToString()
		{
			return this._value;
		}

		// Token: 0x060057B2 RID: 22450 RVA: 0x0013655F File Offset: 0x0013475F
		public static SoapAnyUri Parse(string value)
		{
			return new SoapAnyUri(value);
		}

		// Token: 0x04002820 RID: 10272
		private string _value;
	}
}
