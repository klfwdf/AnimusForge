using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI
{
	// Token: 0x02000005 RID: 5
	[GameStateScreen(typeof(BarberState))]
	public class GauntletBarberScreen : ScreenBase, IGameStateListener, IFaceGeneratorScreen
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000024F8 File Offset: 0x000006F8
		public IFaceGeneratorHandler Handler
		{
			get
			{
				return this._facegenLayer;
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002500 File Offset: 0x00000700
		public GauntletBarberScreen(BarberState state)
		{
			LoadingWindow.EnableGlobalLoadingWindow();
			this._facegenLayer = new BodyGeneratorView(new ControlCharacterCreationStage(this.OnExit), GameTexts.FindText("str_done", null), new ControlCharacterCreationStage(this.OnExit), GameTexts.FindText("str_cancel", null), Hero.MainHero.CharacterObject, false, state.Filter, null, null, null, null, null, null);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002568 File Offset: 0x00000768
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this._facegenLayer.OnTick(dt);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000257D File Offset: 0x0000077D
		public void OnExit()
		{
			Game.Current.GameStateManager.PopState(0);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000258F File Offset: 0x0000078F
		protected override void OnInitialize()
		{
			base.OnInitialize();
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			base.AddLayer(this._facegenLayer.GauntletLayer);
			InformationManager.HideAllMessages();
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000025BD File Offset: 0x000007BD
		protected override void OnFinalize()
		{
			base.OnFinalize();
			if (LoadingWindow.IsLoadingWindowActive)
			{
				LoadingWindow.DisableGlobalLoadingWindow();
			}
			Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000025E1 File Offset: 0x000007E1
		protected override void OnActivate()
		{
			base.OnActivate();
			base.AddLayer(this._facegenLayer.SceneLayer);
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000025FA File Offset: 0x000007FA
		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			this._facegenLayer.SceneLayer.SceneView.SetEnable(false);
			this._facegenLayer.OnFinalize();
			LoadingWindow.EnableGlobalLoadingWindow();
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000013 RID: 19 RVA: 0x0000262D File Offset: 0x0000082D
		void IGameStateListener.OnActivate()
		{
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000262F File Offset: 0x0000082F
		void IGameStateListener.OnDeactivate()
		{
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002631 File Offset: 0x00000831
		void IGameStateListener.OnInitialize()
		{
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002633 File Offset: 0x00000833
		void IGameStateListener.OnFinalize()
		{
		}

		// Token: 0x04000009 RID: 9
		private readonly BodyGeneratorView _facegenLayer;
	}
}
