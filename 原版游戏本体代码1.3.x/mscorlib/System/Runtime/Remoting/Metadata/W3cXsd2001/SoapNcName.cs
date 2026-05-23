using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F9 RID: 2041
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNcName : ISoapXsd
	{
		// Token: 0x17000E99 RID: 3737
		// (get) Token: 0x0600580B RID: 22539 RVA: 0x00136A5C File Offset: 0x00134C5C
		public static string XsdType
		{
			get
			{
				return "NCName";
			}
		}

		// Token: 0x0600580C RID: 22540 RVA: 0x00136A63 File Offset: 0x00134C63
		public string GetXsdType()
		{
			return SoapNcName.XsdType;
		}

		// Token: 0x0600580D RID: 22541 RVA: 0x00136A6A File Offset: 0x00134C6A
		public SoapNcName()
		{
		}

		// Token: 0x0600580E RID: 22542 RVA: 0x00136A72 File Offset: 0x00134C72
		public SoapNcName(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E9A RID: 3738
		// (get) Token: 0x0600580F RID: 22543 RVA: 0x00136A81 File Offset: 0x00134C81
		// (set) Token: 0x06005810 RID: 22544 RVA: 0x00136A89 File Offset: 0x00134C89
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

		// Token: 0x06005811 RID: 22545 RVA: 0x00136A92 File Offset: 0x00134C92
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x06005812 RID: 22546 RVA: 0x00136A9F File Offset: 0x00134C9F
		public static SoapNcName Parse(string value)
		{
			return new SoapNcName(value);
		}

		// Token: 0x0400282D RID: 10285
		private string _value;
	}
}
