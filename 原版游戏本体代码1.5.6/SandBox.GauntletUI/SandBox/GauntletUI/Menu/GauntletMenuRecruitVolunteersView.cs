using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu;

[OverrideView(typeof(MenuRecruitVolunteersView))]
public class GauntletMenuRecruitVolunteersView : MenuView
{
	private GauntletLayer _layerAsGauntletLayer;

	private RecruitmentVM _dataSource;

	private GauntletMovieIdentifier _movie;

	public override bool ShouldUpdateMenuAfterRemoved => true;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_dataSource = new RecruitmentVM();
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		_dataSource.SetRecruitAllInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("TakeAll"));
		_dataSource.SetGetKeyTextFromKeyIDFunc(Game.Current.GameTextManager.GetHotKeyGameTextFromKeyID);
		base.Layer = new GauntletLayer("MapRecruit", 206);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.MenuViewContext.AddLayer(base.Layer);
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_movie = _layerAsGauntletLayer.LoadMovie("RecruitmentPopup", _dataSource);
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(base.Layer);
		_dataSource.RefreshScreen();
		_dataSource.Enabled = true;
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.RecruitmentWindow));
		if (ScreenManager.TopScreen is MapScreen mapScreen)
		{
			mapScreen.SetIsInRecruitment(isInRecruitment: true);
		}
	}

	protected override void OnFinalize()
	{
		base.Layer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(base.Layer);
		_dataSource.OnFinalize();
		_dataSource = null;
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		base.MenuViewContext.RemoveLayer(base.Layer);
		_movie = null;
		base.Layer = null;
		_layerAsGauntletLayer = null;
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.MapWindow));
		if (ScreenManager.TopScreen is MapScreen mapScreen)
		{
			mapScreen.SetIsInRecruitment(isInRecruitment: false);
		}
		base.OnFinalize();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (base.Layer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteForceQuit();
		}
		else if (base.Layer.Input.IsHotKeyReleased("Confirm"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteDone();
		}
		else if (base.Layer.Input.IsHotKeyReleased("Reset"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteReset();
		}
		else if (base.Layer.Input.IsHotKeyReleased("TakeAll"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteRecruitAll();
		}
		else if (base.Layer.Input.IsGameKeyReleased(39))
		{
			if (_dataSource.FocusedVolunteerOwner != null)
			{
				_dataSource.FocusedVolunteerOwner.ExecuteOpenEncyclopedia();
			}
			else if (_dataSource.FocusedVolunteerTroop != null)
			{
				_dataSource.FocusedVolunteerTroop.ExecuteOpenEncyclopedia();
			}
		}
		if (!_dataSource.Enabled)
		{
			base.MenuViewContext.CloseRecruitVolunteers();
		}
	}

	protected override TutorialContexts GetTutorialContext()
	{
		return TutorialContexts.RecruitmentWindow;
	}

	protected override void OnMapConversationActivated()
	{
		base.OnMapConversationActivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationDeactivated()
	{
		base.OnMapConversationDeactivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}
}
