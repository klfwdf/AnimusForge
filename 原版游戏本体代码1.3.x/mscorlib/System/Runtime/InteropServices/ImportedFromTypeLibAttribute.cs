using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200091E RID: 2334
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	public sealed class ImportedFromTypeLibAttribute : Attribute
	{
		// Token: 0x06006009 RID: 24585 RVA: 0x0014B7E6 File Offset: 0x001499E6
		public ImportedFromTypeLibAttribute(string tlbFile)
		{
			this._val = tlbFile;
		}

		// Token: 0x170010D9 RID: 4313
		// (get) Token: 0x0600600A RID: 24586 RVA: 0x0014B7F5 File Offset: 0x001499F5
		public string Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A76 RID: 10870
		internal string _val;
	}
}
