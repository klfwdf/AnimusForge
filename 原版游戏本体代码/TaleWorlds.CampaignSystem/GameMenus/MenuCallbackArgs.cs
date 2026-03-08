using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus
{
	// Token: 0x020000E9 RID: 233
	public class MenuCallbackArgs
	{
		// Token: 0x170005F2 RID: 1522
		// (get) Token: 0x060015B4 RID: 5556 RVA: 0x00061CF9 File Offset: 0x0005FEF9
		// (set) Token: 0x060015B5 RID: 5557 RVA: 0x00061D01 File Offset: 0x0005FF01
		public MenuContext MenuContext { get; private set; }

		// Token: 0x170005F3 RID: 1523
		// (get) Token: 0x060015B6 RID: 5558 RVA: 0x00061D0A File Offset: 0x0005FF0A
		// (set) Token: 0x060015B7 RID: 5559 RVA: 0x00061D12 File Offset: 0x0005FF12
		public MapState MapState { get; private set; }

		// Token: 0x060015B8 RID: 5560 RVA: 0x00061D1B File Offset: 0x0005FF1B
		public MenuCallbackArgs(MenuContext menuContext, TextObject text)
		{
			this.MenuContext = menuContext;
			this.Text = text;
		}

		// Token: 0x060015B9 RID: 5561 RVA: 0x00061D38 File Offset: 0x0005FF38
		public MenuCallbackArgs(MapState mapState, TextObject text)
		{
			this.MapState = mapState;
			this.Text = text;
		}

		// Token: 0x060015BA RID: 5562 RVA: 0x00061D55 File Offset: 0x0005FF55
		public MenuCallbackArgs(MapState mapState, TextObject text, float dt)
		{
			this.MapState = mapState;
			this.Text = text;
			this.DeltaTime = dt;
		}

		// Token: 0x04000725 RID: 1829
		public float DeltaTime;

		// Token: 0x04000726 RID: 1830
		public bool IsEnabled = true;

		// Token: 0x04000727 RID: 1831
		public TextObject Text;

		// Token: 0x04000728 RID: 1832
		public TextObject Tooltip;

		// Token: 0x04000729 RID: 1833
		public GameMenuOption.IssueQuestFlags OptionQuestData;

		// Token: 0x0400072A RID: 1834
		public GameMenuOption.LeaveType optionLeaveType;

		// Token: 0x0400072B RID: 1835
		public TextObject MenuTitle;
	}
}
