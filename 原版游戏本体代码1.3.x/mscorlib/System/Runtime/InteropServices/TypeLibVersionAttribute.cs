using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200093C RID: 2364
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	public sealed class TypeLibVersionAttribute : Attribute
	{
		// Token: 0x06006055 RID: 24661 RVA: 0x0014BFD6 File Offset: 0x0014A1D6
		public TypeLibVersionAttribute(int major, int minor)
		{
			this._major = major;
			this._minor = minor;
		}

		// Token: 0x170010EC RID: 4332
		// (get) Token: 0x06006056 RID: 24662 RVA: 0x0014BFEC File Offset: 0x0014A1EC
		public int MajorVersion
		{
			get
			{
				return this._major;
			}
		}

		// Token: 0x170010ED RID: 4333
		// (get) Token: 0x06006057 RID: 24663 RVA: 0x0014BFF4 File Offset: 0x0014A1F4
		public int MinorVersion
		{
			get
			{
				return this._minor;
			}
		}

		// Token: 0x04002B29 RID: 11049
		internal int _major;

		// Token: 0x04002B2A RID: 11050
		internal int _minor;
	}
}
