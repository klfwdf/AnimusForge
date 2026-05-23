using System;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x02000027 RID: 39
	[OverrideView(typeof(MenuRecruitVolunteersView))]
	public class GauntletMenuRecruitVolunteersView : MenuView
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060001FC RID: 508 RVA: 0x0000C809 File Offset: 0x0000AA09
		public override bool ShouldUpdateMenuAfterRemoved
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0000C80C File Offset: 0x0000AA0C
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._dataSource = new RecruitmentVM();
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			this._dataSource.SetRecruitAllInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("TakeAll"));
			this._dataSource.SetGetKeyTextFromKeyIDFunc(new Func<string, TextObject>(Game.Current.GameTextManager.GetHotKeyGameTextFromKeyID));
			base.Layer = new GauntletLayer("MapRecruit", 206, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.MenuViewContext.AddLayer(base.Layer);
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this._movie = this._layerAsGauntletLayer.LoadMovie("RecruitmentPopup", this._dataSource);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(base.Layer);
			this._dataSource.RefreshScreen();
			this._dataSource.Enabled = true;
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.RecruitmentWindow));
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInRecruitment(true);
			}
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0000C9B8 File Offset: 0x0000ABB8
		protected override void OnFinalize()
		{
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			base.MenuViewContext.RemoveLayer(base.Layer);
			this._movie = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			Game.Current.EventManager.TriggerEvent<TutorialContextChangedEvent>(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInRecruitment(false);
			}
			base.OnFinalize();
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0000CA58 File Offset: 0x0000AC58
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (base.Layer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ExecuteForceQuit();
			}
			else if (base.Layer.Input.IsHotKeyReleased("Confirm"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ExecuteDone();
			}
			else if (base.Layer.Input.IsHotKeyReleased("Reset"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ExecuteReset();
			}
			else if (base.Layer.Input.IsHotKeyReleased("TakeAll"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ExecuteRecruitAll();
			}
			else if (base.Layer.Input.IsGameKeyReleased(39))
			{
				if (this._dataSource.FocusedVolunteerOwner != null)
				{
					this._dataSource.FocusedVolunteerOwner.ExecuteOpenEncyclopedia();
				}
				else if (this._dataSource.FocusedVolunteerTroop != null)
				{
					this._dataSource.FocusedVolunteerTroop.ExecuteOpenEncyclopedia();
				}
			}
			if (!this._dataSource.Enabled)
			{
				base.MenuViewContext.CloseRecruitVolunteers();
			}
		}

		// Token: 0x06000200 RID: 512 RVA: 0x0000CB92 File Offset: 0x0000AD92
		protected override TutorialContexts GetTutorialContext()
		{
			return TutorialContexts.RecruitmentWindow;
		}

		// Token: 0x06000201 RID: 513 RVA: 0x0000CB95 File Offset: 0x0000AD95
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x06000202 RID: 514 RVA: 0x0000CBB1 File Offset: 0x0000ADB1
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x040000A7 RID: 167
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000A8 RID: 168
		private RecruitmentVM _dataSource;

		// Token: 0x040000A9 RID: 169
		private GauntletMovieIdentifier _movie;
	}
}
