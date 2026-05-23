using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007FB RID: 2043
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapIdref : ISoapXsd
	{
		// Token: 0x17000E9D RID: 3741
		// (get) Token: 0x0600581B RID: 22555 RVA: 0x00136AF2 File Offset: 0x00134CF2
		public static string XsdType
		{
			get
			{
				return "IDREF";
			}
		}

		// Token: 0x0600581C RID: 22556 RVA: 0x00136AF9 File Offset: 0x00134CF9
		public string GetXsdType()
		{
			return SoapIdref.XsdType;
		}

		// Token: 0x0600581D RID: 22557 RVA: 0x00136B00 File Offset: 0x00134D00
		public SoapIdref()
		{
		}

		// Token: 0x0600581E RID: 22558 RVA: 0x00136B08 File Offset: 0x00134D08
		public SoapIdref(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E9E RID: 3742
		// (get) Token: 0x0600581F RID: 22559 RVA: 0x00136B17 File Offset: 0x00134D17
		// (set) Token: 0x06005820 RID: 22560 RVA: 0x00136B1F File Offset: 0x00134D1F
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

		// Token: 0x06005821 RID: 22561 RVA: 0x00136B28 File Offset: 0x00134D28
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x06005822 RID: 22562 RVA: 0x00136B35 File Offset: 0x00134D35
		public static SoapIdref Parse(string value)
		{
			return new SoapIdref(value);
		}

		// Token: 0x0400282F RID: 10287
		private string _value;
	}
}
