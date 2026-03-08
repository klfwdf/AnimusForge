using System;

namespace System.Security.Policy
{
	// Token: 0x02000352 RID: 850
	[Serializable]
	internal sealed class EvidenceTypeDescriptor
	{
		// Token: 0x06002A4E RID: 10830 RVA: 0x0009CCD3 File Offset: 0x0009AED3
		public EvidenceTypeDescriptor()
		{
		}

		// Token: 0x06002A4F RID: 10831 RVA: 0x0009CCDC File Offset: 0x0009AEDC
		private EvidenceTypeDescriptor(EvidenceTypeDescriptor descriptor)
		{
			this.m_hostCanGenerate = descriptor.m_hostCanGenerate;
			if (descriptor.m_assemblyEvidence != null)
			{
				this.m_assemblyEvidence = descriptor.m_assemblyEvidence.Clone();
			}
			if (descriptor.m_hostEvidence != null)
			{
				this.m_hostEvidence = descriptor.m_hostEvidence.Clone();
			}
		}

		// Token: 0x17000599 RID: 1433
		// (get) Token: 0x06002A50 RID: 10832 RVA: 0x0009CD2D File Offset: 0x0009AF2D
		// (set) Token: 0x06002A51 RID: 10833 RVA: 0x0009CD35 File Offset: 0x0009AF35
		public EvidenceBase AssemblyEvidence
		{
			get
			{
				return this.m_assemblyEvidence;
			}
			set
			{
				this.m_assemblyEvidence = value;
			}
		}

		// Token: 0x1700059A RID: 1434
		// (get) Token: 0x06002A52 RID: 10834 RVA: 0x0009CD3E File Offset: 0x0009AF3E
		// (set) Token: 0x06002A53 RID: 10835 RVA: 0x0009CD46 File Offset: 0x0009AF46
		public bool Generated
		{
			get
			{
				return this.m_generated;
			}
			set
			{
				this.m_generated = value;
			}
		}

		// Token: 0x1700059B RID: 1435
		// (get) Token: 0x06002A54 RID: 10836 RVA: 0x0009CD4F File Offset: 0x0009AF4F
		// (set) Token: 0x06002A55 RID: 10837 RVA: 0x0009CD57 File Offset: 0x0009AF57
		public bool HostCanGenerate
		{
			get
			{
				return this.m_hostCanGenerate;
			}
			set
			{
				this.m_hostCanGenerate = value;
			}
		}

		// Token: 0x1700059C RID: 1436
		// (get) Token: 0x06002A56 RID: 10838 RVA: 0x0009CD60 File Offset: 0x0009AF60
		// (set) Token: 0x06002A57 RID: 10839 RVA: 0x0009CD68 File Offset: 0x0009AF68
		public EvidenceBase HostEvidence
		{
			get
			{
				return this.m_hostEvidence;
			}
			set
			{
				this.m_hostEvidence = value;
			}
		}

		// Token: 0x06002A58 RID: 10840 RVA: 0x0009CD71 File Offset: 0x0009AF71
		public EvidenceTypeDescriptor Clone()
		{
			return new EvidenceTypeDescriptor(this);
		}

		// Token: 0x0400113F RID: 4415
		[NonSerialized]
		private bool m_hostCanGenerate;

		// Token: 0x04001140 RID: 4416
		[NonSerialized]
		private bool m_generated;

		// Token: 0x04001141 RID: 4417
		private EvidenceBase m_hostEvidence;

		// Token: 0x04001142 RID: 4418
		private EvidenceBase m_assemblyEvidence;
	}
}
