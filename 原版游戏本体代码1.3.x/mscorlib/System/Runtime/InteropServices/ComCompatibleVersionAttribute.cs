using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200093D RID: 2365
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	public sealed class ComCompatibleVersionAttribute : Attribute
	{
		// Token: 0x06006058 RID: 24664 RVA: 0x0014BFFC File Offset: 0x0014A1FC
		public ComCompatibleVersionAttribute(int major, int minor, int build, int revision)
		{
			this._major = major;
			this._minor = minor;
			this._build = build;
			this._revision = revision;
		}

		// Token: 0x170010EE RID: 4334
		// (get) Token: 0x06006059 RID: 24665 RVA: 0x0014C021 File Offset: 0x0014A221
		public int MajorVersion
		{
			get
			{
				return this._major;
			}
		}

		// Token: 0x170010EF RID: 4335
		// (get) Token: 0x0600605A RID: 24666 RVA: 0x0014C029 File Offset: 0x0014A229
		public int MinorVersion
		{
			get
			{
				return this._minor;
			}
		}

		// Token: 0x170010F0 RID: 4336
		// (get) Token: 0x0600605B RID: 24667 RVA: 0x0014C031 File Offset: 0x0014A231
		public int BuildNumber
		{
			get
			{
				return this._build;
			}
		}

		// Token: 0x170010F1 RID: 4337
		// (get) Token: 0x0600605C RID: 24668 RVA: 0x0014C039 File Offset: 0x0014A239
		public int RevisionNumber
		{
			get
			{
				return this._revision;
			}
		}

		// Token: 0x04002B2B RID: 11051
		internal int _major;

		// Token: 0x04002B2C RID: 11052
		internal int _minor;

		// Token: 0x04002B2D RID: 11053
		internal int _build;

		// Token: 0x04002B2E RID: 11054
		internal int _revision;
	}
}
