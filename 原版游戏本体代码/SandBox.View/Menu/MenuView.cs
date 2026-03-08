using System;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace SandBox.View.Menu
{
	// Token: 0x02000039 RID: 57
	public abstract class MenuView : SandboxView
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x060001A7 RID: 423 RVA: 0x00013078 File Offset: 0x00011278
		// (set) Token: 0x060001A8 RID: 424 RVA: 0x00013080 File Offset: 0x00011280
		internal bool Removed { get; set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x060001A9 RID: 425 RVA: 0x00013089 File Offset: 0x00011289
		public virtual bool ShouldUpdateMenuAfterRemoved
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x060001AA RID: 426 RVA: 0x0001308C File Offset: 0x0001128C
		// (set) Token: 0x060001AB RID: 427 RVA: 0x00013094 File Offset: 0x00011294
		public MenuViewContext MenuViewContext { get; internal set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x060001AC RID: 428 RVA: 0x0001309D File Offset: 0x0001129D
		// (set) Token: 0x060001AD RID: 429 RVA: 0x000130A5 File Offset: 0x000112A5
		public MenuContext MenuContext { get; internal set; }

		// Token: 0x060001AE RID: 430 RVA: 0x000130AE File Offset: 0x000112AE
		protected internal virtual void OnMenuContextUpdated(MenuContext newMenuContext)
		{
		}

		// Token: 0x060001AF RID: 431 RVA: 0x000130B0 File Offset: 0x000112B0
		protected internal virtual void OnMenuContextRefreshed()
		{
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x000130B2 File Offset: 0x000112B2
		protected internal virtual void OnOverlayTypeChange(GameMenu.MenuOverlayType newType)
		{
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x000130B4 File Offset: 0x000112B4
		protected internal virtual void OnCharacterDeveloperOpened()
		{
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x000130B6 File Offset: 0x000112B6
		protected internal virtual void OnCharacterDeveloperClosed()
		{
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x000130B8 File Offset: 0x000112B8
		protected internal virtual void OnBackgroundMeshNameSet(string name)
		{
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x000130BA File Offset: 0x000112BA
		protected internal virtual void OnHourlyTick()
		{
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x000130BC File Offset: 0x000112BC
		protected internal virtual void OnResume()
		{
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x000130BE File Offset: 0x000112BE
		protected internal virtual void OnMapConversationActivated()
		{
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x000130C0 File Offset: 0x000112C0
		protected internal virtual void OnMapConversationDeactivated()
		{
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x000130C2 File Offset: 0x000112C2
		protected internal virtual TutorialContexts GetTutorialContext()
		{
			return TutorialContexts.MapWindow;
		}

		// Token: 0x040000FC RID: 252
		protected const float ContextAlphaModifier = 8.5f;
	}
}
