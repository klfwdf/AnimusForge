using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x0200060F RID: 1551
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	[ComVisible(true)]
	public sealed class ObfuscateAssemblyAttribute : Attribute
	{
		// Token: 0x060047EF RID: 18415 RVA: 0x00105E14 File Offset: 0x00104014
		public ObfuscateAssemblyAttribute(bool assemblyIsPrivate)
		{
			this.m_assemblyIsPrivate = assemblyIsPrivate;
		}

		// Token: 0x17000B10 RID: 2832
		// (get) Token: 0x060047F0 RID: 18416 RVA: 0x00105E2A File Offset: 0x0010402A
		public bool AssemblyIsPrivate
		{
			get
			{
				return this.m_assemblyIsPrivate;
			}
		}

		// Token: 0x17000B11 RID: 2833
		// (get) Token: 0x060047F1 RID: 18417 RVA: 0x00105E32 File Offset: 0x00104032
		// (set) Token: 0x060047F2 RID: 18418 RVA: 0x00105E3A File Offset: 0x0010403A
		public bool StripAfterObfuscation
		{
			get
			{
				return this.m_strip;
			}
			set
			{
				this.m_strip = value;
			}
		}

		// Token: 0x04001DC4 RID: 7620
		private bool m_assemblyIsPrivate;

		// Token: 0x04001DC5 RID: 7621
		private bool m_strip = true;
	}
}
