using System;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x0200003C RID: 60
	public class MissionWorkshopNameMarkerTargetVM : MissionNameMarkerTargetVM<Workshop>
	{
		// Token: 0x06000413 RID: 1043 RVA: 0x00010E15 File Offset: 0x0000F015
		public MissionWorkshopNameMarkerTargetVM(Workshop target, Vec3 signPosition)
			: base(target)
		{
			base.NameType = "Passage";
			base.IconType = target.WorkshopType.StringId;
			this._signPosition = signPosition;
			this.RefreshValues();
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x00010E47 File Offset: 0x0000F047
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, this._signPosition + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x00010E60 File Offset: 0x0000F060
		protected override TextObject GetName()
		{
			return base.Target.WorkshopType.Name;
		}

		// Token: 0x04000217 RID: 535
		private readonly Vec3 _signPosition;
	}
}
