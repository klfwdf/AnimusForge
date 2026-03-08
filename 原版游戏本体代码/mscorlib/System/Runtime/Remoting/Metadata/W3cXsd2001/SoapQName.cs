using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata.W3cXsd2001
{
	// Token: 0x020007EF RID: 2031
	[ComVisible(true)]
	[Serializable]
	public sealed class SoapQName : ISoapXsd
	{
		// Token: 0x17000E83 RID: 3715
		// (get) Token: 0x060057B3 RID: 22451 RVA: 0x00136567 File Offset: 0x00134767
		public static string XsdType
		{
			get
			{
				return "QName";
			}
		}

		// Token: 0x060057B4 RID: 22452 RVA: 0x0013656E File Offset: 0x0013476E
		public string GetXsdType()
		{
			return SoapQName.XsdType;
		}

		// Token: 0x060057B5 RID: 22453 RVA: 0x00136575 File Offset: 0x00134775
		public SoapQName()
		{
		}

		// Token: 0x060057B6 RID: 22454 RVA: 0x0013657D File Offset: 0x0013477D
		public SoapQName(string value)
		{
			this._name = value;
		}

		// Token: 0x060057B7 RID: 22455 RVA: 0x0013658C File Offset: 0x0013478C
		public SoapQName(string key, string name)
		{
			this._name = name;
			this._key = key;
		}

		// Token: 0x060057B8 RID: 22456 RVA: 0x001365A2 File Offset: 0x001347A2
		public SoapQName(string key, string name, string namespaceValue)
		{
			this._name = name;
			this._namespace = namespaceValue;
			this._key = key;
		}

		// Token: 0x17000E84 RID: 3716
		// (get) Token: 0x060057B9 RID: 22457 RVA: 0x001365BF File Offset: 0x001347BF
		// (set) Token: 0x060057BA RID: 22458 RVA: 0x001365C7 File Offset: 0x001347C7
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this._name = value;
			}
		}

		// Token: 0x17000E85 RID: 3717
		// (get) Token: 0x060057BB RID: 22459 RVA: 0x001365D0 File Offset: 0x001347D0
		// (set) Token: 0x060057BC RID: 22460 RVA: 0x001365D8 File Offset: 0x001347D8
		public string Namespace
		{
			get
			{
				return this._namespace;
			}
			set
			{
				this._namespace = value;
			}
		}

		// Token: 0x17000E86 RID: 3718
		// (get) Token: 0x060057BD RID: 22461 RVA: 0x001365E1 File Offset: 0x001347E1
		// (set) Token: 0x060057BE RID: 22462 RVA: 0x001365E9 File Offset: 0x001347E9
		public string Key
		{
			get
			{
				return this._key;
			}
			set
			{
				this._key = value;
			}
		}

		// Token: 0x060057BF RID: 22463 RVA: 0x001365F2 File Offset: 0x001347F2
		public override string ToString()
		{
			if (this._key == null || this._key.Length == 0)
			{
				return this._name;
			}
			return this._key + ":" + this._name;
		}

		// Token: 0x060057C0 RID: 22464 RVA: 0x00136628 File Offset: 0x00134828
		public static SoapQName Parse(string value)
		{
			if (value == null)
			{
				return new SoapQName();
			}
			string key = "";
			string name = value;
			int num = value.IndexOf(':');
			if (num > 0)
			{
				key = value.Substring(0, num);
				name = value.Substring(num + 1);
			}
			return new SoapQName(key, name);
		}

		// Token: 0x04002821 RID: 10273
		private string _name;

		// Token: 0x04002822 RID: 10274
		private string _namespace;

		// Token: 0x04002823 RID: 10275
		private string _key;
	}
}
