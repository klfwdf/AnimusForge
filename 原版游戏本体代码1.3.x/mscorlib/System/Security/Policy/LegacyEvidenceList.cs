using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;

namespace System.Security.Policy
{
	// Token: 0x02000351 RID: 849
	[Serializable]
	internal sealed class LegacyEvidenceList : EvidenceBase, IEnumerable<EvidenceBase>, IEnumerable, ILegacyEvidenceAdapter
	{
		// Token: 0x17000597 RID: 1431
		// (get) Token: 0x06002A47 RID: 10823 RVA: 0x0009CC2B File Offset: 0x0009AE2B
		public object EvidenceObject
		{
			get
			{
				if (this.m_legacyEvidenceList.Count <= 0)
				{
					return null;
				}
				return this.m_legacyEvidenceList[0];
			}
		}

		// Token: 0x17000598 RID: 1432
		// (get) Token: 0x06002A48 RID: 10824 RVA: 0x0009CC4C File Offset: 0x0009AE4C
		public Type EvidenceType
		{
			get
			{
				ILegacyEvidenceAdapter legacyEvidenceAdapter = this.m_legacyEvidenceList[0] as ILegacyEvidenceAdapter;
				if (legacyEvidenceAdapter != null)
				{
					return legacyEvidenceAdapter.EvidenceType;
				}
				return this.m_legacyEvidenceList[0].GetType();
			}
		}

		// Token: 0x06002A49 RID: 10825 RVA: 0x0009CC86 File Offset: 0x0009AE86
		public void Add(EvidenceBase evidence)
		{
			this.m_legacyEvidenceList.Add(evidence);
		}

		// Token: 0x06002A4A RID: 10826 RVA: 0x0009CC94 File Offset: 0x0009AE94
		public IEnumerator<EvidenceBase> GetEnumerator()
		{
			return this.m_legacyEvidenceList.GetEnumerator();
		}

		// Token: 0x06002A4B RID: 10827 RVA: 0x0009CCA6 File Offset: 0x0009AEA6
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.m_legacyEvidenceList.GetEnumerator();
		}

		// Token: 0x06002A4C RID: 10828 RVA: 0x0009CCB8 File Offset: 0x0009AEB8
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override EvidenceBase Clone()
		{
			return base.Clone();
		}

		// Token: 0x0400113E RID: 4414
		private List<EvidenceBase> m_legacyEvidenceList = new List<EvidenceBase>();
	}
}
