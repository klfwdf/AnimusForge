using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007FA RID: 2042
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapId : ISoapXsd
	{
		// Token: 0x17000E9B RID: 3739
		// (get) Token: 0x06005813 RID: 22547 RVA: 0x00136AA7 File Offset: 0x00134CA7
		public static string XsdType
		{
			get
			{
				return "ID";
			}
		}

		// Token: 0x06005814 RID: 22548 RVA: 0x00136AAE File Offset: 0x00134CAE
		public string GetXsdType()
		{
			return SoapId.XsdType;
		}

		// Token: 0x06005815 RID: 22549 RVA: 0x00136AB5 File Offset: 0x00134CB5
		public SoapId()
		{
		}

		// Token: 0x06005816 RID: 22550 RVA: 0x00136ABD File Offset: 0x00134CBD
		public SoapId(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E9C RID: 3740
		// (get) Token: 0x06005817 RID: 22551 RVA: 0x00136ACC File Offset: 0x00134CCC
		// (set) Token: 0x06005818 RID: 22552 RVA: 0x00136AD4 File Offset: 0x00134CD4
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

		// Token: 0x06005819 RID: 22553 RVA: 0x00136ADD File Offset: 0x00134CDD
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x0600581A RID: 22554 RVA: 0x00136AEA File Offset: 0x00134CEA
		public static SoapId Parse(string value)
		{
			return new SoapId(value);
		}

		// Token: 0x0400282E RID: 10286
		private string _value;
	}
}
