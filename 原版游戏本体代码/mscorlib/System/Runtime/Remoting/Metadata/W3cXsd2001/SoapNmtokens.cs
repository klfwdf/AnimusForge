using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F8 RID: 2040
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNmtokens : ISoapXsd
	{
		// Token: 0x17000E97 RID: 3735
		// (get) Token: 0x06005803 RID: 22531 RVA: 0x00136A11 File Offset: 0x00134C11
		public static string XsdType
		{
			get
			{
				return "NMTOKENS";
			}
		}

		// Token: 0x06005804 RID: 22532 RVA: 0x00136A18 File Offset: 0x00134C18
		public string GetXsdType()
		{
			return SoapNmtokens.XsdType;
		}

		// Token: 0x06005805 RID: 22533 RVA: 0x00136A1F File Offset: 0x00134C1F
		public SoapNmtokens()
		{
		}

		// Token: 0x06005806 RID: 22534 RVA: 0x00136A27 File Offset: 0x00134C27
		public SoapNmtokens(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E98 RID: 3736
		// (get) Token: 0x06005807 RID: 22535 RVA: 0x00136A36 File Offset: 0x00134C36
		// (set) Token: 0x06005808 RID: 22536 RVA: 0x00136A3E File Offset: 0x00134C3E
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

		// Token: 0x06005809 RID: 22537 RVA: 0x00136A47 File Offset: 0x00134C47
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x0600580A RID: 22538 RVA: 0x00136A54 File Offset: 0x00134C54
		public static SoapNmtokens Parse(string value)
		{
			return new SoapNmtokens(value);
		}

		// Token: 0x0400282C RID: 10284
		private string _value;
	}
}
