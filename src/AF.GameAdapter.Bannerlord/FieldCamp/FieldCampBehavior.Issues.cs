using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Camp issue state and resolution (field-camp-v1 step 4, effects table Q29).
/// Frequency: issues are judged once when pitching finishes; resolution runs only
/// when a postprocess tag arrives; nothing here is on a per-frame path.
/// </summary>
public sealed partial class FieldCampBehavior
{
	internal const float MoraleGainCap = 20f;

	// Save fields (approved 2026-10-30).
	private List<string> _issues = new List<string>();
	private float _moraleGained;
	private List<string> _suppressedKinds = new List<string>();
	private double _lastDefeatHours = -1;

	private readonly List<FieldCampIssues.Issue> _issueCache = new List<FieldCampIssues.Issue>();
	private bool _issueCacheValid;

	private void SyncIssueData(IDataStore dataStore)
	{
		if (dataStore.IsSaving)
		{
			FlushIssues();
		}
		dataStore.SyncData("_afFieldCamp_issues_v1", ref _issues);
		dataStore.SyncData("_afFieldCamp_moraleGained_v1", ref _moraleGained);
		dataStore.SyncData("_afFieldCamp_suppressed_v1", ref _suppressedKinds);
		dataStore.SyncData("_afFieldCamp_lastDefeatHours_v1", ref _lastDefeatHours);
		if (dataStore.IsLoading)
		{
			_issues = _issues ?? new List<string>();
			_suppressedKinds = _suppressedKinds ?? new List<string>();
			_issueCacheValid = false;
		}
	}

	internal IReadOnlyList<FieldCampIssues.Issue> Issues
	{
		get
		{
			if (!_issueCacheValid)
			{
				_issueCache.Clear();
				for (int i = 0; i < _issues.Count; i++)
				{
					FieldCampIssues.Issue issue = FieldCampIssues.Issue.Decode(_issues[i]);
					if (issue != null)
					{
						_issueCache.Add(issue);
					}
				}
				_issueCacheValid = true;
			}
			return _issueCache;
		}
	}

	internal float MoraleGainRemaining => Math.Max(0f, MoraleGainCap - _moraleGained);

	private void FlushIssues()
	{
		if (!_issueCacheValid)
		{
			return;
		}
		_issues.Clear();
		for (int i = 0; i < _issueCache.Count; i++)
		{
			_issues.Add(_issueCache[i].Encode());
		}
	}

	private void JudgeIssues()
	{
		List<FieldCampIssues.Issue> judged = FieldCampIssues.Judge(MobileParty.MainParty, NowHours, _lastRestHours, _lastDefeatHours, _suppressedKinds);
		_issueCache.Clear();
		_issueCache.AddRange(judged);
		_issueCacheValid = true;
		FlushIssues();
		Logger.Log(LogSource, "issues judged: " + string.Join(", ", _issues));
	}

	private void ClearIssueState()
	{
		_issues.Clear();
		_issueCache.Clear();
		_issueCacheValid = true;
		_suppressedKinds.Clear();
		_moraleGained = 0f;
	}

	/// <summary>Strike or encounter: unanswered issues cost a little morale ("left unanswered").</summary>
	private void SettleIgnoredIssues()
	{
		int ignored = 0;
		IReadOnlyList<FieldCampIssues.Issue> issues = Issues;
		for (int i = 0; i < issues.Count; i++)
		{
			if (issues[i].IsOpen)
			{
				issues[i].State = FieldCampIssues.State.Ignored;
				ignored++;
			}
		}
		if (ignored > 0)
		{
			MobileParty.MainParty.RecentEventsMorale -= 2f * ignored;
			Logger.Log(LogSource, "issues ignored count=" + ignored);
		}
	}

	private void OnMapEventEndedForCamp(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner)
			{
				return;
			}
			if (mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				_lastDefeatHours = NowHours;
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// Applies one resolution to the first open issue of the given index.
	/// Returns a short AFEF fact body on success, or null when the request is invalid
	/// (no such open issue, or the player cannot afford a paid option).
	/// </summary>
	internal string Resolve(int issueIndex, FieldCampIssues.Resolution resolution)
	{
		if (_state != (int)CampState.Camped)
		{
			return null;
		}
		IReadOnlyList<FieldCampIssues.Issue> issues = Issues;
		if (issueIndex < 0 || issueIndex >= issues.Count || !issues[issueIndex].IsOpen)
		{
			return null;
		}
		FieldCampIssues.Issue issue = issues[issueIndex];
		MobileParty main = MobileParty.MainParty;
		int cost = CostFor(issue, resolution);
		if (cost > 0 && (Hero.MainHero?.Gold ?? 0) < cost)
		{
			return null;
		}
		if (cost > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost, false);
		}
		float morale = 0f;
		int dismissed = 0;
		int relationLoss = 0;
		switch (resolution)
		{
			case FieldCampIssues.Resolution.Encourage:
				morale = 3f + 5f * LeadershipFactor();
				issue.State = FieldCampIssues.State.Encouraged;
				break;
			case FieldCampIssues.Resolution.Grant:
				morale = 5f;
				ApplyGrantEffect(issue, main);
				issue.State = FieldCampIssues.State.Granted;
				break;
			case FieldCampIssues.Resolution.Refuse:
				morale = -(2f + 3f * (1f - LeadershipFactor()));
				issue.State = FieldCampIssues.State.Refused;
				break;
			case FieldCampIssues.Resolution.Discipline:
				morale = -3f;
				dismissed = DismissTroublemakers(issue, main);
				if (issue.IsCompanionIssue)
				{
					relationLoss = ApplyCompanionDiscipline(issue);
				}
				if (!_suppressedKinds.Contains(issue.Kind.ToString()))
				{
					_suppressedKinds.Add(issue.Kind.ToString());
				}
				issue.State = FieldCampIssues.State.Disciplined;
				break;
		}
		if (morale > 0f)
		{
			morale = Math.Min(morale, MoraleGainRemaining);
			_moraleGained += morale;
		}
		if (morale != 0f)
		{
			main.RecentEventsMorale += morale;
		}
		FlushIssues();
		AnimusForgeQuickInfo.Show(FieldCampText.IssueResolved(resolution, morale, cost, dismissed));
		Logger.Log(LogSource, "issue resolved index=" + issueIndex + " kind=" + issue.Kind + " resolution=" + resolution + " morale=" + morale.ToString("0.0") + " cost=" + cost + " dismissed=" + dismissed + " relationLoss=" + relationLoss);
		return FieldCampText.ResolutionFact(issue, resolution, morale, cost, dismissed);
	}

	private static float LeadershipFactor()
	{
		try
		{
			int skill = Hero.MainHero?.GetSkillValue(DefaultSkills.Leadership) ?? 0;
			return Math.Max(0f, Math.Min(1f, skill / 300f));
		}
		catch
		{
			return 0f;
		}
	}

	/// <summary>Paid options (Q29): wages paid out, a round for the men, provisions bought.</summary>
	internal static int CostFor(FieldCampIssues.Issue issue, FieldCampIssues.Resolution resolution)
	{
		MobileParty main = MobileParty.MainParty;
		int men = Math.Max(1, main?.MemberRoster?.TotalManCount ?? 1);
		if (resolution == FieldCampIssues.Resolution.Grant)
		{
			switch (issue.Kind)
			{
				case FieldCampIssues.Kind.UnpaidWages:
					return Math.Max(50, main?.TotalWage ?? 0);
				case FieldCampIssues.Kind.Hunger:
				case FieldCampIssues.Kind.CompanionFeud:
					return 0;
				case FieldCampIssues.Kind.Petition:
					return men * 3;
				default:
					return men;
			}
		}
		if (resolution == FieldCampIssues.Resolution.Encourage && (issue.Kind == FieldCampIssues.Kind.LowMorale || issue.Kind == FieldCampIssues.Kind.RecentDefeat))
		{
			return men;
		}
		return 0;
	}

	private static void ApplyGrantEffect(FieldCampIssues.Issue issue, MobileParty main)
	{
		if (issue.Kind == FieldCampIssues.Kind.UnpaidWages)
		{
			main.HasUnpaidWages = 0f;
		}
		else if (issue.Kind == FieldCampIssues.Kind.CompanionFeud)
		{
			// Mediated in the open: the two companions make peace a little.
			Hero a = Hero.Find(issue.Subject);
			Hero b = Hero.Find(issue.Subject2);
			if (a != null && b != null && a.IsAlive && b.IsAlive)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(a, b, 5, false);
			}
		}
	}

	/// <summary>Disciplining feuding companions costs their regard for the player (-5 to -10, by leadership).</summary>
	private static int ApplyCompanionDiscipline(FieldCampIssues.Issue issue)
	{
		int loss = 5 + (int)Math.Round(5f * (1f - LeadershipFactor()));
		string[] ids = { issue.Subject, issue.Subject2 };
		for (int i = 0; i < ids.Length; i++)
		{
			Hero hero = string.IsNullOrEmpty(ids[i]) ? null : Hero.Find(ids[i]);
			if (hero != null && hero.IsAlive && hero != Hero.MainHero)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, -loss, false, true);
			}
		}
		return loss;
	}

	/// <summary>At most 1% of the men (at least 1) of the issue's troop type are discharged.</summary>
	private static int DismissTroublemakers(FieldCampIssues.Issue issue, MobileParty main)
	{
		if (issue.IsCompanionIssue || string.IsNullOrEmpty(issue.Subject))
		{
			return 0;
		}
		TroopRoster roster = main.MemberRoster;
		CharacterObject troop = CharacterObject.Find(issue.Subject);
		if (troop == null || troop.IsHero)
		{
			return 0;
		}
		int index = roster.FindIndexOfTroop(troop);
		if (index < 0)
		{
			return 0;
		}
		int healthy = roster.GetElementNumber(index) - roster.GetElementWoundedNumber(index);
		int cap = Math.Max(1, roster.TotalManCount / 100);
		int count = Math.Min(cap, Math.Max(0, healthy - 1));
		if (count <= 0)
		{
			return 0;
		}
		roster.AddToCounts(troop, -count);
		return count;
	}
}
