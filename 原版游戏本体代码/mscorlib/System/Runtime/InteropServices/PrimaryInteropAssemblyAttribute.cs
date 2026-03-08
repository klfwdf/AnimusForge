using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000939 RID: 2361
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	[ComVisible(true)]
	public sealed class PrimaryInteropAssemblyAttribute : Attribute
	{
		// Token: 0x0600604D RID: 24653 RVA: 0x0014BF73 File Offset: 0x0014A173
		public PrimaryInteropAssemblyAttribute(int major, int minor)
		{
			this._major = major;
			this._minor = minor;
		}

		// Token: 0x170010E7 RID: 4327
		// (get) Token: 0x0600604E RID: 24654 RVA: 0x0014BF89 File Offset: 0x0014A189
		public int MajorVersion
		{
			get
			{
				return this._major;
			}
		}

		// Token: 0x170010E8 RID: 4328
		// (get) Token: 0x0600604F RID: 24655 RVA: 0x0014BF91 File Offset: 0x0014A191
		public int MinorVersion
		{
			get
			{
				return this._minor;
			}
		}

		// Token: 0x04002B24 RID: 11044
		internal int _major;

		// Token: 0x04002B25 RID: 11045
		internal int _minor;
	}
}
