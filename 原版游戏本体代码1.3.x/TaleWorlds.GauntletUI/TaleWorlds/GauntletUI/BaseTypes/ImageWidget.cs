using System;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200005B RID: 91
	public class ImageWidget : BrushWidget
	{
		// Token: 0x170001BF RID: 447
		// (get) Token: 0x06000634 RID: 1588 RVA: 0x0001A9C7 File Offset: 0x00018BC7
		// (set) Token: 0x06000635 RID: 1589 RVA: 0x0001A9CF File Offset: 0x00018BCF
		public bool OverrideDefaultStateSwitchingEnabled { get; set; }

		// Token: 0x170001C0 RID: 448
		// (get) Token: 0x06000636 RID: 1590 RVA: 0x0001A9D8 File Offset: 0x00018BD8
		// (set) Token: 0x06000637 RID: 1591 RVA: 0x0001A9E3 File Offset: 0x00018BE3
		public bool OverrideDefaultStateSwitchingDisabled
		{
			get
			{
				return !this.OverrideDefaultStateSwitchingEnabled;
			}
			set
			{
				if (value != !this.OverrideDefaultStateSwitchingEnabled)
				{
					this.OverrideDefaultStateSwitchingEnabled = !value;
				}
			}
		}

		// Token: 0x06000638 RID: 1592 RVA: 0x0001A9FB File Offset: 0x00018BFB
		public ImageWidget(UIContext context)
			: base(context)
		{
			base.AddState("Pressed");
			base.AddState("Hovered");
			base.AddState("Disabled");
		}

		// Token: 0x06000639 RID: 1593 RVA: 0x0001AA28 File Offset: 0x00018C28
		protected override void RefreshState()
		{
			if (!this.OverrideDefaultStateSwitchingEnabled)
			{
				if (base.IsDisabled)
				{
					this.SetState("Disabled");
				}
				else if (base.IsPressed)
				{
					this.SetState("Pressed");
				}
				else if (base.IsHovered)
				{
					this.SetState("Hovered");
				}
				else
				{
					this.SetState("Default");
				}
			}
			base.RefreshState();
		}
	}
}
