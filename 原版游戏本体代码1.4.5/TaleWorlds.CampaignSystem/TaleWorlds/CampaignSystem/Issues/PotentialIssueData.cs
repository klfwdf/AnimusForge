using System;

namespace TaleWorlds.CampaignSystem.Issues;

public struct PotentialIssueData
{
	public delegate IssueBase StartIssueDelegate(in PotentialIssueData pid, Hero issueOwner);

	public StartIssueDelegate OnStartIssue { get; }

	public string IssueId { get; }

	public Type IssueType { get; }

	public IssueBase.IssueFrequency Frequency { get; }

	public object RelatedObject { get; }

	public bool IsValid => OnStartIssue != null;

	public PotentialIssueData(StartIssueDelegate onStartIssue, Type issueType, IssueBase.IssueFrequency frequency, object relatedObject = null)
	{
		OnStartIssue = onStartIssue;
		IssueId = issueType.Name;
		IssueType = issueType;
		Frequency = frequency;
		RelatedObject = relatedObject;
	}

	public PotentialIssueData(Type issueType, IssueBase.IssueFrequency frequency)
	{
		OnStartIssue = null;
		IssueId = issueType.Name;
		IssueType = issueType;
		Frequency = frequency;
		RelatedObject = null;
	}
}
