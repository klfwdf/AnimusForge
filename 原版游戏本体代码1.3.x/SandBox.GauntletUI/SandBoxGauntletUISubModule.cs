using System;
using SandBox.GauntletUI.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;

namespace SandBox.GauntletUI
{
	// Token: 0x02000012 RID: 18
	public class SandBoxGauntletUISubModule : MBSubModuleBase
	{
		// Token: 0x060000D0 RID: 208 RVA: 0x00007A03 File Offset: 0x00005C03
		public SandBoxGauntletUISubModule()
		{
			this._conversationListener = new SandBoxGauntletUISubModule.ConversationGameStateManagerListener();
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x00007A16 File Offset: 0x00005C16
		public override void OnCampaignStart(Game game, object starterObject)
		{
			base.OnCampaignStart(game, starterObject);
			if (!this._gameStarted && game.GameType is Campaign)
			{
				this._gameStarted = true;
			}
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x00007A3C File Offset: 0x00005C3C
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			base.OnGameStart(game, gameStarterObject);
			if (!this._gameStarted && game.GameType is Campaign)
			{
				this._gameStarted = true;
				SandBoxGauntletGameNotification.Initialize();
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00007A67 File Offset: 0x00005C67
		public override void OnGameEnd(Game game)
		{
			base.OnGameEnd(game);
			if (this._gameStarted && game.GameType is Campaign)
			{
				this._gameStarted = false;
				GauntletGameNotification.Initialize();
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00007A91 File Offset: 0x00005C91
		public override void BeginGameStart(Game game)
		{
			base.BeginGameStart(game);
			if (Campaign.Current != null)
			{
				Campaign.Current.VisualCreator.MapEventVisualCreator = new GauntletMapEventVisualCreator();
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00007AB8 File Offset: 0x00005CB8
		protected override void OnApplicationTick(float dt)
		{
			base.OnApplicationTick(dt);
			if (!this._initializedConversationHandler)
			{
				Game game = Game.Current;
				if (((game != null) ? game.GameStateManager : null) != null)
				{
					Game.Current.GameStateManager.RegisterListener(this._conversationListener);
					this._registeredGameStateManager = Game.Current.GameStateManager;
					this._initializedConversationHandler = true;
					goto IL_8C;
				}
			}
			if (this._initializedConversationHandler)
			{
				Game game2 = Game.Current;
				if (((game2 != null) ? game2.GameStateManager : null) == null)
				{
					this._registeredGameStateManager.UnregisterListener(this._conversationListener);
					this._initializedConversationHandler = false;
					this._registeredGameStateManager = null;
				}
			}
			IL_8C:
			if (!this._initialized && GauntletSceneNotification.Current != null)
			{
				if (!Utilities.CommandLineArgumentExists("VisualTests"))
				{
					GauntletSceneNotification.Current.RegisterContextProvider(new SandboxSceneNotificationContextProvider());
				}
				this._initialized = true;
			}
		}

		// Token: 0x04000059 RID: 89
		private bool _gameStarted;

		// Token: 0x0400005A RID: 90
		private bool _initialized;

		// Token: 0x0400005B RID: 91
		private GameStateManager _registeredGameStateManager;

		// Token: 0x0400005C RID: 92
		private bool _initializedConversationHandler;

		// Token: 0x0400005D RID: 93
		private SandBoxGauntletUISubModule.ConversationGameStateManagerListener _conversationListener;

		// Token: 0x02000056 RID: 86
		private class ConversationGameStateManagerListener : IGameStateManagerListener
		{
			// Token: 0x060003F7 RID: 1015 RVA: 0x00017DB4 File Offset: 0x00015FB4
			void IGameStateManagerListener.OnCleanStates()
			{
				this.UpdateCampaignMission();
			}

			// Token: 0x060003F8 RID: 1016 RVA: 0x00017DBC File Offset: 0x00015FBC
			void IGameStateManagerListener.OnCreateState(GameState gameState)
			{
			}

			// Token: 0x060003F9 RID: 1017 RVA: 0x00017DBE File Offset: 0x00015FBE
			void IGameStateManagerListener.OnPopState(GameState gameState)
			{
				this.UpdateCampaignMission();
			}

			// Token: 0x060003FA RID: 1018 RVA: 0x00017DC6 File Offset: 0x00015FC6
			void IGameStateManagerListener.OnPushState(GameState gameState, bool isTopGameState)
			{
				this.UpdateCampaignMission();
			}

			// Token: 0x060003FB RID: 1019 RVA: 0x00017DCE File Offset: 0x00015FCE
			void IGameStateManagerListener.OnSavedGameLoadFinished()
			{
			}

			// Token: 0x060003FC RID: 1020 RVA: 0x00017DD0 File Offset: 0x00015FD0
			private void UpdateCampaignMission()
			{
				ICampaignMission campaignMission = CampaignMission.Current;
				if (campaignMission == null)
				{
					return;
				}
				campaignMission.OnGameStateChanged();
			}
		}
	}
}
