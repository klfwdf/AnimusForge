using System;

namespace TaleWorlds.CampaignSystem.Issues
{
	// Token: 0x0200037C RID: 892
	public struct PotentialIssueData
	{
		// Token: 0x17000C4E RID: 3150
		// (get) Token: 0x060033DE RID: 13278 RVA: 0x000D534C File Offset: 0x000D354C
		public PotentialIssueData.StartIssueDelegate OnStartIssue { get; }

		// Token: 0x17000C4F RID: 3151
		// (get) Token: 0x060033DF RID: 13279 RVA: 0x000D5354 File Offset: 0x000D3554
		public string IssueId { get; }

		// Token: 0x17000C50 RID: 3152
		// (get) Token: 0x060033E0 RID: 13280 RVA: 0x000D535C File Offset: 0x000D355C
		public Type IssueType { get; }

		// Token: 0x17000C51 RID: 3153
		// (get) Token: 0x060033E1 RID: 13281 RVA: 0x000D5364 File Offset: 0x000D3564
		public IssueBase.IssueFrequency Frequency { get; }

		// Token: 0x17000C52 RID: 3154
		// (get) Token: 0x060033E2 RID: 13282 RVA: 0x000D536C File Offset: 0x000D356C
		public object RelatedObject { get; }

		// Token: 0x17000C53 RID: 3155
		// (get) Token: 0x060033E3 RID: 13283 RVA: 0x000D5374 File Offset: 0x000D3574
		public bool IsValid
		{
			get
			{
				return this.OnStartIssue != null;
			}
		}

		// Token: 0x060033E4 RID: 13284 RVA: 0x000D537F File Offset: 0x000D357F
		public PotentialIssueData(PotentialIssueData.StartIssueDelegate onStartIssue, Type issueType, IssueBase.IssueFrequency frequency, object relatedObject = null)
		{
			this.OnStartIssue = onStartIssue;
			this.IssueId = issueType.Name;
			this.IssueType = issueType;
			this.Frequency = frequency;
			this.RelatedObject = relatedObject;
		}

		// Token: 0x060033E5 RID: 13285 RVA: 0x000D53AA File Offset: 0x000D35AA
		public PotentialIssueData(Type issueType, IssueBase.IssueFrequency frequency)
		{
			this.OnStartIssue = null;
			this.IssueId = issueType.Name;
			this.IssueType = issueType;
			this.Frequency = frequency;
			this.RelatedObject = null;
		}

		// Token: 0x02000760 RID: 1888
		// (Invoke) Token: 0x06005FF9 RID: 24569
		public delegate IssueBase StartIssueDelegate(in PotentialIssueData pid, Hero issueOwner);
	}
}
