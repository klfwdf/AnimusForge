using System;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x020003A1 RID: 929
	public class TutorialState : GameState
	{
		// Token: 0x17000C8F RID: 3215
		// (get) Token: 0x06003510 RID: 13584 RVA: 0x000D6A2F File Offset: 0x000D4C2F
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06003512 RID: 13586 RVA: 0x000D6A55 File Offset: 0x000D4C55
		protected override void OnActivate()
		{
			base.OnActivate();
			this.MenuContext.Refresh();
		}

		// Token: 0x06003513 RID: 13587 RVA: 0x000D6A68 File Offset: 0x000D4C68
		protected override void OnFinalize()
		{
			this.MenuContext.Destroy();
			this._objectManager.UnregisterObject(this.MenuContext);
			this.MenuContext = null;
			base.OnFinalize();
		}

		// Token: 0x06003514 RID: 13588 RVA: 0x000D6A93 File Offset: 0x000D4C93
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			this.MenuContext.OnTick(dt);
		}

		// Token: 0x04000F1A RID: 3866
		private MBObjectManager _objectManager = MBObjectManager.Instance;

		// Token: 0x04000F1B RID: 3867
		public MenuContext MenuContext = MBObjectManager.Instance.CreateObject<MenuContext>();
	}
}
