using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008C5 RID: 2245
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	[Serializable]
	public sealed class DependencyAttribute : Attribute
	{
		// Token: 0x06005DBF RID: 23999 RVA: 0x0014995E File Offset: 0x00147B5E
		public DependencyAttribute(string dependentAssemblyArgument, LoadHint loadHintArgument)
		{
			this.dependentAssembly = dependentAssemblyArgument;
			this.loadHint = loadHintArgument;
		}

		// Token: 0x1700101B RID: 4123
		// (get) Token: 0x06005DC0 RID: 24000 RVA: 0x00149974 File Offset: 0x00147B74
		public string DependentAssembly
		{
			get
			{
				return this.dependentAssembly;
			}
		}

		// Token: 0x1700101C RID: 4124
		// (get) Token: 0x06005DC1 RID: 24001 RVA: 0x0014997C File Offset: 0x00147B7C
		public LoadHint LoadHint
		{
			get
			{
				return this.loadHint;
			}
		}

		// Token: 0x04002A32 RID: 10802
		private string dependentAssembly;

		// Token: 0x04002A33 RID: 10803
		private LoadHint loadHint;
	}
}
