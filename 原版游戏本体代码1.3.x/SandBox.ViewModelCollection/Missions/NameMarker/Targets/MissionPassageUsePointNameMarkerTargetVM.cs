using System;
using SandBox.Objects;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x0200003B RID: 59
	public class MissionPassageUsePointNameMarkerTargetVM : MissionNameMarkerTargetVM<PassageUsePoint>
	{
		// Token: 0x06000410 RID: 1040 RVA: 0x00010D44 File Offset: 0x0000EF44
		public MissionPassageUsePointNameMarkerTargetVM(PassageUsePoint target)
			: base(target)
		{
			base.NameType = "Passage";
			base.IconType = ((base.Target.ToLocation == null && base.Target.IsMissionExit) ? "center" : base.Target.ToLocation.StringId);
			base.Quests = new MBBindingList<QuestMarkerVM>();
			this.RefreshValues();
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x00010DAC File Offset: 0x0000EFAC
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, base.Target.GameEntity.GlobalPosition + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x00010DDD File Offset: 0x0000EFDD
		protected override TextObject GetName()
		{
			if (base.Target.ToLocation == null && base.Target.IsMissionExit)
			{
				return GameTexts.FindText("str_exit", null);
			}
			return base.Target.ToLocation.Name;
		}
	}
}
