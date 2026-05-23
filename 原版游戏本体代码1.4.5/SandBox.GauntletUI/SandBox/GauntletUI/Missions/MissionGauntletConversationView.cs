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

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionConversationView))]
public class MissionGauntletConversationView : MissionView, IConversationStateHandler
{
	private MissionConversationVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionConversationCameraView _conversationCameraView;

	private MissionGauntletEscapeMenuBase _escapeView;

	private SpriteCategory _conversationCategory;

	public MissionConversationLogic ConversationHandler { get; private set; }

	public MissionGauntletConversationView()
	{
		ViewOrderPriority = 49;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		MissionGauntletEscapeMenuBase escapeView = _escapeView;
		if ((escapeView != null && escapeView.IsActive) || _gauntletLayer == null)
		{
			return;
		}
		SceneLayer sceneLayer = base.MissionScreen.SceneLayer;
		if (sceneLayer != null && sceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
		{
			MissionConversationCameraView conversationCameraView = _conversationCameraView;
			if (conversationCameraView == null || !conversationCameraView.IsCameraOverridden)
			{
				_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: false);
				goto IL_008a;
			}
		}
		_gauntletLayer.InputRestrictions.SetMouseVisibility(isVisible: true);
		goto IL_008a;
		IL_008a:
		if (IsGameKeyReleasedInAnyLayer("ContinueKey"))
		{
			MissionConversationVM dataSource = _dataSource;
			if (dataSource != null && dataSource.AnswerList.Count <= 0 && base.Mission.Mode != MissionMode.Barter)
			{
				MissionConversationVM dataSource2 = _dataSource;
				if (dataSource2 != null && !dataSource2.SelectedAnOptionOrLinkThisFrame)
				{
					_dataSource?.ExecuteContinue();
				}
			}
		}
		if (_dataSource != null)
		{
			_dataSource.Tick(dt);
			_dataSource.SelectedAnOptionOrLinkThisFrame = false;
		}
		if (_gauntletLayer != null && IsGameKeyReleasedInAnyLayer("ToggleEscapeMenu"))
		{
			base.MissionScreen.OnEscape();
		}
	}

	public override void OnMissionScreenFinalize()
	{
		Campaign.Current.ConversationManager.Handler = null;
		if (_dataSource != null)
		{
			_dataSource?.OnFinalize();
			_dataSource = null;
		}
		_gauntletLayer = null;
		ConversationHandler = null;
		base.OnMissionScreenFinalize();
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		ConversationHandler = base.Mission.GetMissionBehavior<MissionConversationLogic>();
		_conversationCameraView = base.Mission.GetMissionBehavior<MissionConversationCameraView>();
		Campaign.Current.ConversationManager.Handler = this;
	}

	public override void OnMissionScreenActivate()
	{
		base.OnMissionScreenActivate();
		if (_dataSource != null)
		{
			base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[2] { "MissionConversation", "SceneLayer" }, isActive: true);
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
	}

	void IConversationStateHandler.OnConversationInstall()
	{
		base.MissionScreen.SetConversationActive(isActive: true);
		_conversationCategory = UIResourceManager.LoadSpriteCategory("ui_conversation");
		_dataSource = new MissionConversationVM(GetContinueKeyText);
		_gauntletLayer = new GauntletLayer("MissionConversation", ViewOrderPriority);
		_gauntletLayer.LoadMovie("SPConversation", _dataSource);
		GameKeyContext category = HotKeyManager.GetCategory("ConversationHotKeyCategory");
		_gauntletLayer.Input.RegisterHotKeyCategory(category);
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
		}
		GameKeyContext category2 = HotKeyManager.GetCategory("GenericPanelGameKeyCategory");
		_gauntletLayer.Input.RegisterHotKeyCategory(category2);
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category2))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category2);
		}
		_gauntletLayer.IsFocusLayer = true;
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_escapeView = base.Mission.GetMissionBehavior<MissionGauntletEscapeMenuBase>();
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[2] { "MissionConversation", "SceneLayer" }, isActive: true);
		ScreenManager.TrySetFocus(_gauntletLayer);
		InformationManager.HideAllMessages();
	}

	public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
	{
		base.OnMissionModeChange(oldMissionMode, atStart);
		if (oldMissionMode == MissionMode.Barter && base.Mission.Mode == MissionMode.Conversation)
		{
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
	}

	void IConversationStateHandler.OnConversationUninstall()
	{
		base.MissionScreen.SetConversationActive(isActive: false);
		if (_dataSource != null)
		{
			_dataSource?.OnFinalize();
			_dataSource = null;
		}
		_conversationCategory.Unload();
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		base.MissionScreen.SetLayerCategoriesStateAndToggleOthers(new string[1] { "MissionConversation" }, isActive: false);
		base.MissionScreen.SetLayerCategoriesState(new string[1] { "SceneLayer" }, isActive: true);
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_escapeView = null;
	}

	private string GetContinueKeyText()
	{
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			return GameTexts.FindText("str_click_to_continue_console").SetTextVariable("CONSOLE_KEY_NAME", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ConversationHotKeyCategory", "ContinueClick"))).ToString();
		}
		return GameTexts.FindText("str_click_to_continue").ToString();
	}

	void IConversationStateHandler.OnConversationActivate()
	{
		base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[2] { "MissionConversation", "SceneLayer" }, isActive: true);
	}

	void IConversationStateHandler.OnConversationDeactivate()
	{
		MBInformationManager.HideInformations();
	}

	void IConversationStateHandler.OnConversationContinue()
	{
		_dataSource.OnConversationContinue();
	}

	void IConversationStateHandler.ExecuteConversationContinue()
	{
		_dataSource.ExecuteContinue();
	}

	private bool IsGameKeyReleasedInAnyLayer(string hotKeyID)
	{
		bool num = IsReleasedInSceneLayer(hotKeyID);
		bool flag = IsReleasedInGauntletLayer(hotKeyID);
		return num || flag;
	}

	private bool IsReleasedInSceneLayer(string hotKeyID)
	{
		return base.MissionScreen.SceneLayer?.Input.IsHotKeyReleased(hotKeyID) ?? false;
	}

	private bool IsReleasedInGauntletLayer(string hotKeyID)
	{
		return _gauntletLayer?.Input.IsHotKeyReleased(hotKeyID) ?? false;
	}
}
