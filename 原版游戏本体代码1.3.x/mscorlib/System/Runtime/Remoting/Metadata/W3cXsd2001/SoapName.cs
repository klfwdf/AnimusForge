using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F4 RID: 2036
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapName : ISoapXsd
	{
		// Token: 0x17000E8F RID: 3727
		// (get) Token: 0x060057E3 RID: 22499 RVA: 0x001368E5 File Offset: 0x00134AE5
		public static string XsdType
		{
			get
			{
				return "Name";
			}
		}

		// Token: 0x060057E4 RID: 22500 RVA: 0x001368EC File Offset: 0x00134AEC
		public string GetXsdType()
		{
			return SoapName.XsdType;
		}

		// Token: 0x060057E5 RID: 22501 RVA: 0x001368F3 File Offset: 0x00134AF3
		public SoapName()
		{
		}

		// Token: 0x060057E6 RID: 22502 RVA: 0x001368FB File Offset: 0x00134AFB
		public SoapName(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E90 RID: 3728
		// (get) Token: 0x060057E7 RID: 22503 RVA: 0x0013690A File Offset: 0x00134B0A
		// (set) Token: 0x060057E8 RID: 22504 RVA: 0x00136912 File Offset: 0x00134B12
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

		// Token: 0x060057E9 RID: 22505 RVA: 0x0013691B File Offset: 0x00134B1B
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x060057EA RID: 22506 RVA: 0x00136928 File Offset: 0x00134B28
		public static SoapName Parse(string value)
		{
			return new SoapName(value);
		}

		// Token: 0x04002828 RID: 10280
		private string _value;
	}
}
