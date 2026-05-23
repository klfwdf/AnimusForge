using System;
using SandBox.Objects.Usables;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout
{
	// Token: 0x0200003E RID: 62
	public class MissionStealthAreaUsePointNameMarkerTargetVM : MissionNameMarkerTargetBaseVM
	{
		// Token: 0x06000419 RID: 1049 RVA: 0x00010EC4 File Offset: 0x0000F0C4
		public MissionStealthAreaUsePointNameMarkerTargetVM(StealthAreaUsePoint usePoint)
		{
			this._usePoint = usePoint;
			base.IconType = "call_troops";
			base.NameType = "Normal";
			this.RefreshValues();
		}

		// Token: 0x0600041A RID: 1050 RVA: 0x00010EEF File Offset: 0x0000F0EF
		public override bool Equals(MissionNameMarkerTargetBaseVM other)
		{
			return false;
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x00010EF4 File Offset: 0x0000F0F4
		public override void UpdatePosition(Camera missionCamera)
		{
			MatrixFrame globalFrame = this._usePoint.GameEntity.GetGlobalFrame();
			base.UpdatePositionWith(missionCamera, globalFrame.origin + Vec3.Up * 0.5f);
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x00010F36 File Offset: 0x0000F136
		protected override TextObject GetName()
		{
			return new TextObject("{=GmjiZk9P}Call Troops", null);
		}

		// Token: 0x04000219 RID: 537
		private StealthAreaUsePoint _usePoint;
	}
}
