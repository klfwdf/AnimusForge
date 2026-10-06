using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public abstract class MissionNameMarkerTargetVM<T> : MissionNameMarkerTargetBaseVM
{
	public T Target { get; private set; }

	protected MissionNameMarkerTargetVM(T target)
	{
		Target = target;
	}

	public override bool Equals(MissionNameMarkerTargetBaseVM other)
	{
		if (other is MissionNameMarkerTargetVM<T> { Target: var target } missionNameMarkerTargetVM && target.Equals(Target) && AreQuestsEqual(missionNameMarkerTargetVM))
		{
			return base.IsPersistent == missionNameMarkerTargetVM.IsPersistent;
		}
		return false;
	}

	private bool AreQuestsEqual(MissionNameMarkerTargetVM<T> tOther)
	{
		if (tOther.Quests != null && base.Quests != null)
		{
			if (tOther.Quests.Count != base.Quests.Count)
			{
				return false;
			}
			for (int i = 0; i < base.Quests.Count; i++)
			{
				QuestMarkerVM questMarkerVM = base.Quests[i];
				QuestMarkerVM questMarkerVM2 = tOther.Quests[i];
				if (questMarkerVM.IssueQuestFlag != questMarkerVM2.IssueQuestFlag || questMarkerVM.QuestMarkerType != questMarkerVM2.QuestMarkerType)
				{
					return false;
				}
			}
			return true;
		}
		if (tOther.Quests == null && base.Quests == null)
		{
			return true;
		}
		return false;
	}
}
