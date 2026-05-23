using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000889 RID: 2185
	[ComVisible(true)]
	[Serializable]
	public class Header
	{
		// Token: 0x06005CA5 RID: 23717 RVA: 0x00145047 File Offset: 0x00143247
		public Header(string _Name, object _Value)
			: this(_Name, _Value, true)
		{
		}

		// Token: 0x06005CA6 RID: 23718 RVA: 0x00145052 File Offset: 0x00143252
		public Header(string _Name, object _Value, bool _MustUnderstand)
		{
			this.Name = _Name;
			this.Value = _Value;
			this.MustUnderstand = _MustUnderstand;
		}

		// Token: 0x06005CA7 RID: 23719 RVA: 0x0014506F File Offset: 0x0014326F
		public Header(string _Name, object _Value, bool _MustUnderstand, string _HeaderNamespace)
		{
			this.Name = _Name;
			this.Value = _Value;
			this.MustUnderstand = _MustUnderstand;
			this.HeaderNamespace = _HeaderNamespace;
		}

		// Token: 0x040029D4 RID: 10708
		public string Name;

		// Token: 0x040029D5 RID: 10709
		public object Value;

		// Token: 0x040029D6 RID: 10710
		public bool MustUnderstand;

		// Token: 0x040029D7 RID: 10711
		public string HeaderNamespace;
	}
}
