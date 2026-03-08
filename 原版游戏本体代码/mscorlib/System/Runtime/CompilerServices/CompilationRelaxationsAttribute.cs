using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008B4 RID: 2228
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Method)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class CompilationRelaxationsAttribute : Attribute
	{
		// Token: 0x06005DA5 RID: 23973 RVA: 0x0014982C File Offset: 0x00147A2C
		[__DynamicallyInvokable]
		public CompilationRelaxationsAttribute(int relaxations)
		{
			this.m_relaxations = relaxations;
		}

		// Token: 0x06005DA6 RID: 23974 RVA: 0x0014983B File Offset: 0x00147A3B
		public CompilationRelaxationsAttribute(CompilationRelaxations relaxations)
		{
			this.m_relaxations = (int)relaxations;
		}

		// Token: 0x17001013 RID: 4115
		// (get) Token: 0x06005DA7 RID: 23975 RVA: 0x0014984A File Offset: 0x00147A4A
		[__DynamicallyInvokable]
		public int CompilationRelaxations
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_relaxations;
			}
		}

		// Token: 0x04002A16 RID: 10774
		private int m_relaxations;
	}
}
