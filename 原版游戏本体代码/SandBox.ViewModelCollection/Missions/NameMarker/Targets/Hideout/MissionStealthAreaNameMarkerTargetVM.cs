using System;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout
{
	// Token: 0x0200003D RID: 61
	public class MissionStealthAreaNameMarkerTargetVM : MissionNameMarkerTargetVM<StealthAreaMarker>
	{
		// Token: 0x06000416 RID: 1046 RVA: 0x00010E72 File Offset: 0x0000F072
		public MissionStealthAreaNameMarkerTargetVM(StealthAreaMarker target, Vec3 position)
			: base(target)
		{
			this._position = position;
			base.NameType = "Passage";
			base.IconType = "stealth_area";
			this.RefreshValues();
		}

		// Token: 0x06000417 RID: 1047 RVA: 0x00010E9E File Offset: 0x0000F09E
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, this._position + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x00010EB7 File Offset: 0x0000F0B7
		protected override TextObject GetName()
		{
			return new TextObject("{=WcSky2KB}Stealth Area", null);
		}

		// Token: 0x04000218 RID: 536
		private readonly Vec3 _position;
	}
}
