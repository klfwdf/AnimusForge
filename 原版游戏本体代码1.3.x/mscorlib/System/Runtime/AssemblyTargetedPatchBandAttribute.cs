using System;

namespace System.Runtime
{
	// Token: 0x02000718 RID: 1816
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	public sealed class AssemblyTargetedPatchBandAttribute : Attribute
	{
		// Token: 0x06005130 RID: 20784 RVA: 0x0011E6CF File Offset: 0x0011C8CF
		public AssemblyTargetedPatchBandAttribute(string targetedPatchBand)
		{
			this.m_targetedPatchBand = targetedPatchBand;
		}

		// Token: 0x17000D59 RID: 3417
		// (get) Token: 0x06005131 RID: 20785 RVA: 0x0011E6DE File Offset: 0x0011C8DE
		public string TargetedPatchBand
		{
			get
			{
				return this.m_targetedPatchBand;
			}
		}

		// Token: 0x040023FC RID: 9212
		private string m_targetedPatchBand;
	}
}
