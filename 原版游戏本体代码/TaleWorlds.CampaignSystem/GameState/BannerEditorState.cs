using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000385 RID: 901
	public class BannerEditorState : GameState
	{
		// Token: 0x17000C59 RID: 3161
		// (get) Token: 0x06003438 RID: 13368 RVA: 0x000D5E61 File Offset: 0x000D4061
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C5A RID: 3162
		// (get) Token: 0x06003439 RID: 13369 RVA: 0x000D5E64 File Offset: 0x000D4064
		// (set) Token: 0x0600343A RID: 13370 RVA: 0x000D5E6C File Offset: 0x000D406C
		public IBannerEditorStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x0600343B RID: 13371 RVA: 0x000D5E75 File Offset: 0x000D4075
		public BannerEditorState()
		{
		}

		// Token: 0x0600343C RID: 13372 RVA: 0x000D5E7D File Offset: 0x000D407D
		public BannerEditorState(Action endAction)
		{
			this._onEndAction = endAction;
		}

		// Token: 0x0600343D RID: 13373 RVA: 0x000D5E8C File Offset: 0x000D408C
		public Clan GetClan()
		{
			return Clan.PlayerClan;
		}

		// Token: 0x0600343E RID: 13374 RVA: 0x000D5E93 File Offset: 0x000D4093
		public CharacterObject GetCharacter()
		{
			return CharacterObject.PlayerCharacter;
		}

		// Token: 0x0600343F RID: 13375 RVA: 0x000D5E9A File Offset: 0x000D409A
		protected override void OnFinalize()
		{
			base.OnFinalize();
			Action onEndAction = this._onEndAction;
			if (onEndAction == null)
			{
				return;
			}
			onEndAction();
		}

		// Token: 0x04000EE2 RID: 3810
		private IBannerEditorStateHandler _handler;

		// Token: 0x04000EE3 RID: 3811
		private Action _onEndAction;
	}
}
