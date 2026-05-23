using System;
using SandBox.Conversation.MissionLogics;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x0200001D RID: 29
	[OverrideView(typeof(MissionConversationView))]
	public class MissionGauntletConversationView : MissionView, IConversationStateHandler
	{
		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x0000AFCC File Offset: 0x000091CC
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x0000AFD4 File Offset: 0x000091D4
		public MissionConversationLogic ConversationHandler { get; private set; }

		// Token: 0x060001A4 RID: 420 RVA: 0x0000AFDD File Offset: 0x000091DD
		public MissionGauntletConversationView()
		{
			this.ViewOrderPriority = 49;
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x0000AFF0 File Offset: 0x000091F0
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			MissionGauntletEscapeMenuBase escapeView = this._escapeView;
			if ((escapeView == null || !escapeView.IsActive) && this._gauntletLayer != null)
			{
				SceneLayer sceneLayer = base.MissionScreen.SceneLayer;
				if (sceneLayer != null && sceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
				{
					MissionConversationCameraView conversationCameraView = this._conversationCameraView;
					if (conversationCameraView == null || !conversationCameraView.IsCameraOverridden)
					{
						this._gauntletLayer.InputRestrictions.SetMouseVisibility(false);
						goto IL_8A;
					}
				}
				this._gauntletLayer.InputRestrictions.SetMouseVisibility(true);
				IL_8A:
				if (this.IsGameKeyReleasedInAnyLayer("ContinueKey"))
				{
					MissionConversationVM dataSource = this._dataSource;
					if (dataSource != null && dataSource.AnswerList.Count <= 0 && base.Mission.Mode != MissionMode.Barter)
					{
						MissionConversationVM dataSource2 = this._dataSource;
						if (dataSource2 != null && !dataSource2.SelectedAnOptionOrLinkThisFrame)
						{
							MissionConversationVM dataSource3 = this._dataSource;
							if (dataSource3 != null)
							{
								dataSource3.ExecuteContinue();
							}
						}
					}
				}
				if (this._dataSource != null)
				{
					this._dataSource.Tick(dt);
					this._dataSource.SelectedAnOptionOrLinkThisFrame = false;
				}
				if (this._gauntletLayer != null && this.IsGameKeyReleasedInAnyLayer("ToggleEscapeMenu"))
				{
					base.MissionScreen.OnEscape();
				}
			}
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x0000B12C File Offset: 0x0000932C
		public override void OnMissionScreenFinalize()
		{
			Campaign.Current.ConversationManager.Handler = null;
			if (this._dataSource != null)
			{
				MissionConversationVM dataSource = this._dataSource;
				if (dataSource != null)
				{
					dataSource.OnFinalize();
				}
				this._dataSource = null;
			}
			this._gauntletLayer = null;
			this.ConversationHandler = null;
			base.OnMissionScreenFinalize();
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x0000B17D File Offset: 0x0000937D
		public override void EarlyStart()
		{
			base.EarlyStart();
			this.ConversationHandler = base.Mission.GetMissionBehavior<MissionConversationLogic>();
			this._conversationCameraView = base.Mission.GetMissionBehavior<MissionConversationCameraView>();
			Campaign.Current.ConversationManager.Handler = this;
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x0000B1B7 File Offset: 0x000093B7
		public override void OnMissionScreenActivate()
		{
			base.OnMissionScreenActivate();
			if (this._dataSource != null)
			{
				base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[] { "MissionConversation", "SceneLayer" }, true);
				ScreenManager.TrySetFocus(this._gauntletLayer);
			}
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x0000B1F4 File Offset: 0x000093F4
		void IConversationStateHandler.OnConversationInstall()
		{
			base.MissionScreen.SetConversationActive(true);
			this._conversationCategory = UIResourceManager.LoadSpriteCategory("ui_conversation");
			this._dataSource = new MissionConversationVM(new Func<string>(this.GetContinueKeyText), false);
			this._gauntletLayer = new GauntletLayer("MissionConversation", this.ViewOrderPriority, false);
			this._gauntletLayer.LoadMovie("SPConversation", this._dataSource);
			GameKeyContext category = HotKeyManager.GetCategory("ConversationHotKeyCategory");
			this._gauntletLayer.Input.RegisterHotKeyCategory(category);
			if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
			{
				base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
			}
			GameKeyContext category2 = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
			this._gauntletLayer.Input.RegisterHotKeyCategory(category2);
			if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category2))
			{
				base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category2);
			}
			this._gauntletLayer.IsFocusLayer = true;
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._escapeView = base.Mission.GetMissionBehavior<MissionGauntletEscapeMenuBase>();
			base.MissionScreen.AddLayer(this._gauntletLayer);
			base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[] { "MissionConversation", "SceneLayer" }, true);
			ScreenManager.TrySetFocus(this._gauntletLayer);
			InformationManager.HideAllMessages();
		}

		// Token: 0x060001AA RID: 426 RVA: 0x0000B369 File Offset: 0x00009569
		public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
		{
			base.OnMissionModeChange(oldMissionMode, atStart);
			if (oldMissionMode == MissionMode.Barter && base.Mission.Mode == MissionMode.Conversation)
			{
				ScreenManager.TrySetFocus(this._gauntletLayer);
			}
		}

		// Token: 0x060001AB RID: 427 RVA: 0x0000B390 File Offset: 0x00009590
		void IConversationStateHandler.OnConversationUninstall()
		{
			base.MissionScreen.SetConversationActive(false);
			if (this._dataSource != null)
			{
				MissionConversationVM dataSource = this._dataSource;
				if (dataSource != null)
				{
					dataSource.OnFinalize();
				}
				this._dataSource = null;
			}
			this._conversationCategory.Unload();
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
			base.MissionScreen.SetLayerCategoriesStateAndToggleOthers(new string[] { "MissionConversation" }, false);
			base.MissionScreen.SetLayerCategoriesState(new string[] { "SceneLayer" }, true);
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer = null;
			this._escapeView = null;
		}

		// Token: 0x060001AC RID: 428 RVA: 0x0000B450 File Offset: 0x00009650
		private string GetContinueKeyText()
		{
			if (TaleWorlds.InputSystem.Input.IsGamepadActive)
			{
				return GameTexts.FindText("str_click_to_continue_console", null).SetTextVariable("CONSOLE_KEY_NAME", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ConversationHotKeyCategory", "ContinueClick"), 1f)).ToString();
			}
			return GameTexts.FindText("str_click_to_continue", null).ToString();
		}

		// Token: 0x060001AD RID: 429 RVA: 0x0000B4A8 File Offset: 0x000096A8
		void IConversationStateHandler.OnConversationActivate()
		{
			base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[] { "MissionConversation", "SceneLayer" }, true);
		}

		// Token: 0x060001AE RID: 430 RVA: 0x0000B4CC File Offset: 0x000096CC
		void IConversationStateHandler.OnConversationDeactivate()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x060001AF RID: 431 RVA: 0x0000B4D3 File Offset: 0x000096D3
		void IConversationStateHandler.OnConversationContinue()
		{
			this._dataSource.OnConversationContinue();
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x0000B4E0 File Offset: 0x000096E0
		void IConversationStateHandler.ExecuteConversationContinue()
		{
			this._dataSource.ExecuteContinue();
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x0000B4F0 File Offset: 0x000096F0
		private bool IsGameKeyReleasedInAnyLayer(string hotKeyID)
		{
			bool flag = this.IsReleasedInSceneLayer(hotKeyID);
			bool flag2 = this.IsReleasedInGauntletLayer(hotKeyID);
			return flag || flag2;
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x0000B50E File Offset: 0x0000970E
		private bool IsReleasedInSceneLayer(string hotKeyID)
		{
			SceneLayer sceneLayer = base.MissionScreen.SceneLayer;
			return sceneLayer != null && sceneLayer.Input.IsHotKeyReleased(hotKeyID);
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x0000B52C File Offset: 0x0000972C
		private bool IsReleasedInGauntletLayer(string hotKeyID)
		{
			GauntletLayer gauntletLayer = this._gauntletLayer;
			return gauntletLayer != null && gauntletLayer.Input.IsHotKeyReleased(hotKeyID);
		}

		// Token: 0x04000085 RID: 133
		private MissionConversationVM _dataSource;

		// Token: 0x04000086 RID: 134
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000088 RID: 136
		private MissionConversationCameraView _conversationCameraView;

		// Token: 0x04000089 RID: 137
		private MissionGauntletEscapeMenuBase _escapeView;

		// Token: 0x0400008A RID: 138
		private SpriteCategory _conversationCategory;
	}
}
