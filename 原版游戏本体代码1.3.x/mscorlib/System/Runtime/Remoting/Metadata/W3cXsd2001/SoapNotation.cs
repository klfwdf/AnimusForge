using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F0 RID: 2032
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapNotation : ISoapXsd
	{
		// Token: 0x17000E87 RID: 3719
		// (get) Token: 0x060057C1 RID: 22465 RVA: 0x0013666D File Offset: 0x0013486D
		public static string XsdType
		{
			get
			{
				return "NOTATION";
			}
		}

		// Token: 0x060057C2 RID: 22466 RVA: 0x00136674 File Offset: 0x00134874
		public string GetXsdType()
		{
			return SoapNotation.XsdType;
		}

		// Token: 0x060057C3 RID: 22467 RVA: 0x0013667B File Offset: 0x0013487B
		public SoapNotation()
		{
		}

		// Token: 0x060057C4 RID: 22468 RVA: 0x00136683 File Offset: 0x00134883
		public SoapNotation(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E88 RID: 3720
		// (get) Token: 0x060057C5 RID: 22469 RVA: 0x00136692 File Offset: 0x00134892
		// (set) Token: 0x060057C6 RID: 22470 RVA: 0x0013669A File Offset: 0x0013489A
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

		// Token: 0x060057C7 RID: 22471 RVA: 0x001366A3 File Offset: 0x001348A3
		public override string ToString()
		{
			return this._value;
		}

		// Token: 0x060057C8 RID: 22472 RVA: 0x001366AB File Offset: 0x001348AB
		public static SoapNotation Parse(string value)
		{
			return new SoapNotation(value);
		}

		// Token: 0x04002824 RID: 10276
		private string _value;
	}
}
