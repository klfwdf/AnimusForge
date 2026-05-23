using System;
using System.Security.Permissions;

namespace System.Security.Policy
{
	// Token: 0x02000350 RID: 848
	[Serializable]
	internal sealed class LegacyEvidenceWrapper : EvidenceBase, ILegacyEvidenceAdapter
	{
		// Token: 0x06002A41 RID: 10817 RVA: 0x0009CBE4 File Offset: 0x0009ADE4
		internal LegacyEvidenceWrapper(object legacyEvidence)
		{
			this.m_legacyEvidence = legacyEvidence;
		}

		// Token: 0x17000595 RID: 1429
		// (get) Token: 0x06002A42 RID: 10818 RVA: 0x0009CBF3 File Offset: 0x0009ADF3
		public object EvidenceObject
		{
			get
			{
				return this.m_legacyEvidence;
			}
		}

		// Token: 0x17000596 RID: 1430
		// (get) Token: 0x06002A43 RID: 10819 RVA: 0x0009CBFB File Offset: 0x0009ADFB
		public Type EvidenceType
		{
			get
			{
				return this.m_legacyEvidence.GetType();
			}
		}

		// Token: 0x06002A44 RID: 10820 RVA: 0x0009CC08 File Offset: 0x0009AE08
		public override bool Equals(object obj)
		{
			return this.m_legacyEvidence.Equals(obj);
		}

		// Token: 0x06002A45 RID: 10821 RVA: 0x0009CC16 File Offset: 0x0009AE16
		public override int GetHashCode()
		{
			return this.m_legacyEvidence.GetHashCode();
		}

		// Token: 0x06002A46 RID: 10822 RVA: 0x0009CC23 File Offset: 0x0009AE23
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override EvidenceBase Clone()
		{
			return base.Clone();
		}

		// Token: 0x0400113D RID: 4413
		private object m_legacyEvidence;
	}
}
