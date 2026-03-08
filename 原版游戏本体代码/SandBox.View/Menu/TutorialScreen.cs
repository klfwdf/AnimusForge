using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Menu
{
	// Token: 0x0200003B RID: 59
	[GameStateScreen(typeof(TutorialState))]
	public class TutorialScreen : ScreenBase, IGameStateListener
	{
		// Token: 0x17000018 RID: 24
		// (get) Token: 0x060001E8 RID: 488 RVA: 0x000139BA File Offset: 0x00011BBA
		public MenuViewContext MenuViewContext { get; }

		// Token: 0x060001E9 RID: 489 RVA: 0x000139C2 File Offset: 0x00011BC2
		public TutorialScreen(TutorialState tutorialState)
		{
			this.MenuViewContext = new MenuViewContext(this, tutorialState.MenuContext);
		}

		// Token: 0x060001EA RID: 490 RVA: 0x000139DC File Offset: 0x00011BDC
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.MenuViewContext.OnFrameTick(dt);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x000139F1 File Offset: 0x00011BF1
		protected override void OnActivate()
		{
			base.OnActivate();
			this.MenuViewContext.OnActivate();
			LoadingWindow.DisableGlobalLoadingWindow();
		}

		// Token: 0x060001EC RID: 492 RVA: 0x00013A09 File Offset: 0x00011C09
		protected override void OnDeactivate()
		{
			this.MenuViewContext.OnDeactivate();
			base.OnDeactivate();
		}

		// Token: 0x060001ED RID: 493 RVA: 0x00013A1C File Offset: 0x00011C1C
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this.MenuViewContext.OnInitialize();
		}

		// Token: 0x060001EE RID: 494 RVA: 0x00013A2F File Offset: 0x00011C2F
		protected override void OnFinalize()
		{
			this.MenuViewContext.OnFinalize();
			base.OnFinalize();
		}

		// Token: 0x060001EF RID: 495 RVA: 0x00013A42 File Offset: 0x00011C42
		void IGameStateListener.OnActivate()
		{
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x00013A44 File Offset: 0x00011C44
		void IGameStateListener.OnDeactivate()
		{
			this.MenuViewContext.OnGameStateDeactivate();
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x00013A51 File Offset: 0x00011C51
		void IGameStateListener.OnInitialize()
		{
			this.MenuViewContext.OnGameStateInitialize();
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x00013A5E File Offset: 0x00011C5E
		void IGameStateListener.OnFinalize()
		{
			this.MenuViewContext.OnGameStateFinalize();
		}
	}
}
