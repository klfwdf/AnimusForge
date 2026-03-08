using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F5 RID: 2037
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapIdrefs : ISoapXsd
	{
		// Token: 0x17000E91 RID: 3729
		// (get) Token: 0x060057EB RID: 22507 RVA: 0x00136930 File Offset: 0x00134B30
		public static string XsdType
		{
			get
			{
				return "IDREFS";
			}
		}

		// Token: 0x060057EC RID: 22508 RVA: 0x00136937 File Offset: 0x00134B37
		public string GetXsdType()
		{
			return SoapIdrefs.XsdType;
		}

		// Token: 0x060057ED RID: 22509 RVA: 0x0013693E File Offset: 0x00134B3E
		public SoapIdrefs()
		{
		}

		// Token: 0x060057EE RID: 22510 RVA: 0x00136946 File Offset: 0x00134B46
		public SoapIdrefs(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E92 RID: 3730
		// (get) Token: 0x060057EF RID: 22511 RVA: 0x00136955 File Offset: 0x00134B55
		// (set) Token: 0x060057F0 RID: 22512 RVA: 0x0013695D File Offset: 0x00134B5D
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

		// Token: 0x060057F1 RID: 22513 RVA: 0x00136966 File Offset: 0x00134B66
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x060057F2 RID: 22514 RVA: 0x00136973 File Offset: 0x00134B73
		public static SoapIdrefs Parse(string value)
		{
			return new SoapIdrefs(value);
		}

		// Token: 0x04002829 RID: 10281
		private string _value;
	}
}
