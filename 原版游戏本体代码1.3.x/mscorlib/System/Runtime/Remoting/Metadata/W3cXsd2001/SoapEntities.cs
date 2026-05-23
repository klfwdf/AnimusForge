using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007F6 RID: 2038
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapEntities : ISoapXsd
	{
		// Token: 0x17000E93 RID: 3731
		// (get) Token: 0x060057F3 RID: 22515 RVA: 0x0013697B File Offset: 0x00134B7B
		public static string XsdType
		{
			get
			{
				return "ENTITIES";
			}
		}

		// Token: 0x060057F4 RID: 22516 RVA: 0x00136982 File Offset: 0x00134B82
		public string GetXsdType()
		{
			return SoapEntities.XsdType;
		}

		// Token: 0x060057F5 RID: 22517 RVA: 0x00136989 File Offset: 0x00134B89
		public SoapEntities()
		{
		}

		// Token: 0x060057F6 RID: 22518 RVA: 0x00136991 File Offset: 0x00134B91
		public SoapEntities(string value)
		{
			this._value = value;
		}

		// Token: 0x17000E94 RID: 3732
		// (get) Token: 0x060057F7 RID: 22519 RVA: 0x001369A0 File Offset: 0x00134BA0
		// (set) Token: 0x060057F8 RID: 22520 RVA: 0x001369A8 File Offset: 0x00134BA8
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

		// Token: 0x060057F9 RID: 22521 RVA: 0x001369B1 File Offset: 0x00134BB1
		public override string ToString()
		{
			return SoapType.Escape(this._value);
		}

		// Token: 0x060057FA RID: 22522 RVA: 0x001369BE File Offset: 0x00134BBE
		public static SoapEntities Parse(string value)
		{
			return new SoapEntities(value);
		}

		// Token: 0x0400282A RID: 10282
		private string _value;
	}
}
