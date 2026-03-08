using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x0200020A RID: 522
	public class CharacterCreationState : PlayerGameState
	{
		// Token: 0x170007D7 RID: 2007
		// (get) Token: 0x06001FB2 RID: 8114 RVA: 0x0008E520 File Offset: 0x0008C720
		// (set) Token: 0x06001FB3 RID: 8115 RVA: 0x0008E528 File Offset: 0x0008C728
		public CharacterCreationManager CharacterCreationManager
		{
			get
			{
				return this._characterCreationManager;
			}
			private set
			{
				this._characterCreationManager = value;
			}
		}

		// Token: 0x170007D8 RID: 2008
		// (get) Token: 0x06001FB4 RID: 8116 RVA: 0x0008E531 File Offset: 0x0008C731
		// (set) Token: 0x06001FB5 RID: 8117 RVA: 0x0008E539 File Offset: 0x0008C739
		public ICharacterCreationStateHandler Handler
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

		// Token: 0x06001FB6 RID: 8118 RVA: 0x0008E542 File Offset: 0x0008C742
		public CharacterCreationState()
		{
			this.CharacterCreationManager = new CharacterCreationManager(this);
		}

		// Token: 0x06001FB7 RID: 8119 RVA: 0x0008E556 File Offset: 0x0008C756
		protected override void OnInitialize()
		{
			base.OnInitialize();
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
		}

		// Token: 0x06001FB8 RID: 8120 RVA: 0x0008E56E File Offset: 0x0008C76E
		protected override void OnActivate()
		{
			base.OnActivate();
			this.CharacterCreationManager.OnStateActivated();
		}

		// Token: 0x06001FB9 RID: 8121 RVA: 0x0008E584 File Offset: 0x0008C784
		public void FinalizeCharacterCreationState()
		{
			Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<MapState>(), 0);
			PartyBase.MainParty.SetVisualAsDirty();
			ICharacterCreationStateHandler handler = this._handler;
			if (handler != null)
			{
				handler.OnCharacterCreationFinalized();
			}
			CampaignEventDispatcher.Instance.OnCharacterCreationIsOver();
		}

		// Token: 0x06001FBA RID: 8122 RVA: 0x0008E5E5 File Offset: 0x0008C7E5
		public void Refresh()
		{
			ICharacterCreationStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnRefresh();
		}

		// Token: 0x06001FBB RID: 8123 RVA: 0x0008E5F7 File Offset: 0x0008C7F7
		public void OnStageActivated(CharacterCreationStageBase stage)
		{
			ICharacterCreationStateHandler handler = this._handler;
			if (handler == null)
			{
				return;
			}
			handler.OnStageCreated(stage);
		}

		// Token: 0x0400093D RID: 2365
		private CharacterCreationManager _characterCreationManager;

		// Token: 0x0400093E RID: 2366
		private ICharacterCreationStateHandler _handler;
	}
}
