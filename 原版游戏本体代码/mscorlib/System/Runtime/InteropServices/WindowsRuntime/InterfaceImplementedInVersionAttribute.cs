using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009C8 RID: 2504
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
	[__DynamicallyInvokable]
	public sealed class InterfaceImplementedInVersionAttribute : Attribute
	{
		// Token: 0x060063C2 RID: 25538 RVA: 0x00154988 File Offset: 0x00152B88
		[__DynamicallyInvokable]
		public InterfaceImplementedInVersionAttribute(Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion)
		{
			this.m_interfaceType = interfaceType;
			this.m_majorVersion = majorVersion;
			this.m_minorVersion = minorVersion;
			this.m_buildVersion = buildVersion;
			this.m_revisionVersion = revisionVersion;
		}

		// Token: 0x1700113A RID: 4410
		// (get) Token: 0x060063C3 RID: 25539 RVA: 0x001549B5 File Offset: 0x00152BB5
		[__DynamicallyInvokable]
		public Type InterfaceType
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_interfaceType;
			}
		}

		// Token: 0x1700113B RID: 4411
		// (get) Token: 0x060063C4 RID: 25540 RVA: 0x001549BD File Offset: 0x00152BBD
		[__DynamicallyInvokable]
		public byte MajorVersion
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_majorVersion;
			}
		}

		// Token: 0x1700113C RID: 4412
		// (get) Token: 0x060063C5 RID: 25541 RVA: 0x001549C5 File Offset: 0x00152BC5
		[__DynamicallyInvokable]
		public byte MinorVersion
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_minorVersion;
			}
		}

		// Token: 0x1700113D RID: 4413
		// (get) Token: 0x060063C6 RID: 25542 RVA: 0x001549CD File Offset: 0x00152BCD
		[__DynamicallyInvokable]
		public byte BuildVersion
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_buildVersion;
			}
		}

		// Token: 0x1700113E RID: 4414
		// (get) Token: 0x060063C7 RID: 25543 RVA: 0x001549D5 File Offset: 0x00152BD5
		[__DynamicallyInvokable]
		public byte RevisionVersion
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_revisionVersion;
			}
		}

		// Token: 0x04002CDC RID: 11484
		private Type m_interfaceType;

		// Token: 0x04002CDD RID: 11485
		private byte m_majorVersion;

		// Token: 0x04002CDE RID: 11486
		private byte m_minorVersion;

		// Token: 0x04002CDF RID: 11487
		private byte m_buildVersion;

		// Token: 0x04002CE0 RID: 11488
		private byte m_revisionVersion;
	}
}
