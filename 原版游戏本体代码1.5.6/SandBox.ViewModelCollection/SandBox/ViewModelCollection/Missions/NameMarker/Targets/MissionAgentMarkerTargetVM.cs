using System.Collections.Generic;
using Helpers;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Engine;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets;

public class MissionAgentMarkerTargetVM : MissionNameMarkerTargetVM<Agent>
{
	private class QuestMarkerComparer : IComparer<QuestMarkerVM>
	{
		public int Compare(QuestMarkerVM x, QuestMarkerVM y)
		{
			return x.QuestMarkerType.CompareTo(y.QuestMarkerType);
		}
	}

	public MissionAgentMarkerTargetVM(Agent target)
		: base(target)
	{
		base.NameType = "Normal";
		base.IconType = "character";
		if (target.Character is CharacterObject characterObject)
		{
			Hero heroObject = characterObject.HeroObject;
			if (heroObject != null && heroObject.IsLord)
			{
				base.IconType = "noble";
				base.NameType = "Noble";
				if (FactionManager.IsAtWarAgainstFaction(characterObject.HeroObject.MapFaction, Hero.MainHero.MapFaction))
				{
					base.NameType = "Enemy";
					base.IsEnemy = true;
				}
				else if (DiplomacyHelper.IsSameFactionAndNotEliminated(characterObject.HeroObject.MapFaction, Hero.MainHero.MapFaction))
				{
					base.NameType = "Friendly";
					base.IsFriendly = true;
				}
			}
			if (characterObject.HeroObject != null && characterObject.HeroObject.IsPrisoner)
			{
				base.IconType = "prisoner";
			}
			if (target.IsHuman && target != Agent.Main)
			{
				UpdateQuestStatus();
			}
			if (characterObject == Settlement.CurrentSettlement?.Culture?.Barber)
			{
				base.IconType = "barber";
			}
			else if (characterObject == Settlement.CurrentSettlement?.Culture?.Blacksmith)
			{
				base.IconType = "blacksmith";
			}
			else if (characterObject == Settlement.CurrentSettlement?.Culture?.TavernGamehost)
			{
				base.IconType = "game_host";
			}
			else if (characterObject.StringId == "sp_hermit")
			{
				base.IconType = "hermit";
			}
			else if (base.Target.Character == Settlement.CurrentSettlement?.Culture?.Shipwright)
			{
				base.IconType = "shipwright";
			}
		}
		RefreshValues();
	}

	public override void UpdatePosition(Camera missionCamera)
	{
		UpdatePositionWith(missionCamera, base.Target.GetEyeGlobalPosition() + MissionNameMarkerHelper.AgentHeightOffset);
	}

	protected override TextObject GetName()
	{
		return base.Target.NameTextObject;
	}

	public void UpdateQuestStatus()
	{
		CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlags.None;
		Hero hero = ((CharacterObject)(base.Target?.Character))?.HeroObject;
		if (hero != null)
		{
			List<(CampaignUIHelper.IssueQuestFlags, TextObject, TextObject)> questStateOfHero = CampaignUIHelper.GetQuestStateOfHero(hero);
			for (int i = 0; i < questStateOfHero.Count; i++)
			{
				issueQuestFlags |= questStateOfHero[i].Item1;
			}
		}
		if (base.Target != null && (base.Target.Character as CharacterObject)?.HeroObject?.Clan?.Leader != Hero.MainHero)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null && currentSettlement.LocationComplex?.FindCharacter(base.Target)?.IsVisualTracked == true)
			{
				issueQuestFlags |= CampaignUIHelper.IssueQuestFlags.TrackedIssue;
			}
		}
		DisguiseMissionLogic missionBehavior = Mission.Current.GetMissionBehavior<DisguiseMissionLogic>();
		if (missionBehavior != null && missionBehavior.IsContactAgentTracked(base.Target))
		{
			issueQuestFlags |= CampaignUIHelper.IssueQuestFlags.TrackedIssue;
		}
		CampaignUIHelper.IssueQuestFlags[] issueQuestFlagsValues = CampaignUIHelper.IssueQuestFlagsValues;
		foreach (CampaignUIHelper.IssueQuestFlags questFlag in issueQuestFlagsValues)
		{
			if (questFlag != CampaignUIHelper.IssueQuestFlags.None && (issueQuestFlags & questFlag) != CampaignUIHelper.IssueQuestFlags.None && base.Quests.AllQ((QuestMarkerVM q) => q.IssueQuestFlag != questFlag))
			{
				base.Quests.Add(new QuestMarkerVM(questFlag));
				if ((questFlag & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.AvailableIssue) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.TrackedIssue) != CampaignUIHelper.IssueQuestFlags.None)
				{
					base.IsTracked = true;
				}
				else if ((questFlag & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) != CampaignUIHelper.IssueQuestFlags.None && (questFlag & CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
				{
					base.IsQuestMainStory = true;
				}
			}
		}
		base.Quests.Sort(new QuestMarkerComparer());
	}
}
