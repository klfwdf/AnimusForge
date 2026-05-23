using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers
{
	// Token: 0x02000043 RID: 67
	public class AnimatedBasicAreaIndicator : AreaMarker
	{
		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000289 RID: 649 RVA: 0x0000F259 File Offset: 0x0000D459
		// (set) Token: 0x0600028A RID: 650 RVA: 0x0000F261 File Offset: 0x0000D461
		public bool IsActive { get; private set; } = true;

		// Token: 0x0600028B RID: 651 RVA: 0x0000F26A File Offset: 0x0000D46A
		protected override void OnInit()
		{
			this._name = (string.IsNullOrEmpty(this.NameStringId) ? TextObject.GetEmpty() : GameTexts.FindText(this.NameStringId, null));
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000F292 File Offset: 0x0000D492
		public void SetIsActive(bool isActive)
		{
			this.IsActive = isActive;
			Campaign.Current.VisualTrackerManager.SetDirty();
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000F2AA File Offset: 0x0000D4AA
		public void SetOverriddenName(TextObject name)
		{
			this._overriddenName = name;
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000F2B3 File Offset: 0x0000D4B3
		public override TextObject GetName()
		{
			if (!TextObject.IsNullOrEmpty(this._overriddenName))
			{
				return this._overriddenName;
			}
			return this._name;
		}

		// Token: 0x0400011D RID: 285
		public string NameStringId = "";

		// Token: 0x0400011E RID: 286
		public string Type;

		// Token: 0x0400011F RID: 287
		[EditorVisibleScriptComponentVariable(false)]
		private TextObject _name;

		// Token: 0x04000120 RID: 288
		private TextObject _overriddenName;
	}
}
