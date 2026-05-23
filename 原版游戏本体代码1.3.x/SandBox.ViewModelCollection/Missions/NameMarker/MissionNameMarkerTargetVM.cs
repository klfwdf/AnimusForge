using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

namespace SandBox.ViewModelCollection.Missions.NameMarker
{
	// Token: 0x02000032 RID: 50
	public abstract class MissionNameMarkerTargetVM<T> : MissionNameMarkerTargetBaseVM
	{
		// Token: 0x1700012A RID: 298
		// (get) Token: 0x060003C7 RID: 967 RVA: 0x0000FE9F File Offset: 0x0000E09F
		// (set) Token: 0x060003C8 RID: 968 RVA: 0x0000FEA7 File Offset: 0x0000E0A7
		public T Target { get; private set; }

		// Token: 0x060003C9 RID: 969 RVA: 0x0000FEB0 File Offset: 0x0000E0B0
		protected MissionNameMarkerTargetVM(T target)
		{
			this.Target = target;
		}

		// Token: 0x060003CA RID: 970 RVA: 0x0000FEC0 File Offset: 0x0000E0C0
		public override bool Equals(MissionNameMarkerTargetBaseVM other)
		{
			MissionNameMarkerTargetVM<T> missionNameMarkerTargetVM;
			if ((missionNameMarkerTargetVM = other as MissionNameMarkerTargetVM<T>) != null)
			{
				T target = missionNameMarkerTargetVM.Target;
				if (target.Equals(this.Target) && this.AreQuestsEqual(missionNameMarkerTargetVM))
				{
					return base.IsPersistent == missionNameMarkerTargetVM.IsPersistent;
				}
			}
			return false;
		}

		// Token: 0x060003CB RID: 971 RVA: 0x0000FF14 File Offset: 0x0000E114
		private bool AreQuestsEqual(MissionNameMarkerTargetVM<T> tOther)
		{
			if (tOther.Quests == null || base.Quests == null)
			{
				return tOther.Quests == null && base.Quests == null;
			}
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
	}
}
