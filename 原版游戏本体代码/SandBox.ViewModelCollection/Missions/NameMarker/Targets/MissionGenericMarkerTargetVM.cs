using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x0200003A RID: 58
	public class MissionGenericMarkerTargetVM : MissionNameMarkerTargetBaseVM
	{
		// Token: 0x0600040C RID: 1036 RVA: 0x00010CC5 File Offset: 0x0000EEC5
		public MissionGenericMarkerTargetVM(string identifier, string nameType, string iconType, Vec3 position, TextObject name)
		{
			this.Identifier = identifier;
			base.NameType = nameType;
			base.IconType = iconType;
			this._position = position;
			this._name = name;
			this.RefreshValues();
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x00010CF8 File Offset: 0x0000EEF8
		public override bool Equals(MissionNameMarkerTargetBaseVM other)
		{
			MissionGenericMarkerTargetVM missionGenericMarkerTargetVM;
			return (missionGenericMarkerTargetVM = other as MissionGenericMarkerTargetVM) != null && missionGenericMarkerTargetVM.Identifier == this.Identifier;
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x00010D22 File Offset: 0x0000EF22
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, this._position + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x00010D3B File Offset: 0x0000EF3B
		protected override TextObject GetName()
		{
			return this._name;
		}

		// Token: 0x04000214 RID: 532
		public readonly string Identifier;

		// Token: 0x04000215 RID: 533
		private readonly Vec3 _position;

		// Token: 0x04000216 RID: 534
		private readonly TextObject _name;
	}
}
