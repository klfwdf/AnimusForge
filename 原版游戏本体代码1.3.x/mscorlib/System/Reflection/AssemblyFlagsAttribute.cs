using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C3 RID: 1475
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyFlagsAttribute : Attribute
	{
		// Token: 0x06004471 RID: 17521 RVA: 0x000FC58A File Offset: 0x000FA78A
		[Obsolete("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		[CLSCompliant(false)]
		public AssemblyFlagsAttribute(uint flags)
		{
			this.m_flags = (AssemblyNameFlags)flags;
		}

		// Token: 0x17000A1E RID: 2590
		// (get) Token: 0x06004472 RID: 17522 RVA: 0x000FC599 File Offset: 0x000FA799
		[Obsolete("This property has been deprecated. Please use AssemblyFlags instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		[CLSCompliant(false)]
		public uint Flags
		{
			get
			{
				return (uint)this.m_flags;
			}
		}

		// Token: 0x17000A1F RID: 2591
		// (get) Token: 0x06004473 RID: 17523 RVA: 0x000FC5A1 File Offset: 0x000FA7A1
		[__DynamicallyInvokable]
		public int AssemblyFlags
		{
			[__DynamicallyInvokable]
			get
			{
				return (int)this.m_flags;
			}
		}

		// Token: 0x06004474 RID: 17524 RVA: 0x000FC5A9 File Offset: 0x000FA7A9
		[Obsolete("This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public AssemblyFlagsAttribute(int assemblyFlags)
		{
			this.m_flags = (AssemblyNameFlags)assemblyFlags;
		}

		// Token: 0x06004475 RID: 17525 RVA: 0x000FC5B8 File Offset: 0x000FA7B8
		[__DynamicallyInvokable]
		public AssemblyFlagsAttribute(AssemblyNameFlags assemblyFlags)
		{
			this.m_flags = assemblyFlags;
		}

		// Token: 0x04001C0D RID: 7181
		private AssemblyNameFlags m_flags;
	}
}
