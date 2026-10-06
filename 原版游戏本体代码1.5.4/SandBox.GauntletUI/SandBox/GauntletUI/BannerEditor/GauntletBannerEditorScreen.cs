using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.BannerEditor;

[GameStateScreen(typeof(BannerEditorState))]
public class GauntletBannerEditorScreen : ScreenBase, IGameStateListener
{
	private const int ViewOrderPriority = 15;

	private readonly BannerEditorView _bannerEditorLayer;

	private readonly Clan _clan;

	public GauntletBannerEditorScreen(BannerEditorState bannerEditorState)
	{
		LoadingWindow.EnableGlobalLoadingWindow();
		_clan = bannerEditorState.GetClan();
		_bannerEditorLayer = new BannerEditorView(bannerEditorState.GetCharacter(), bannerEditorState.GetClan().Banner, OnDone, new TextObject("{=WiNRdfsm}Done"), OnCancel, new TextObject("{=3CpNUnVl}Cancel"));
		_bannerEditorLayer.DataSource.SetClanRelatedRules(bannerEditorState.GetClan().Kingdom == null);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		_bannerEditorLayer.OnTick(dt);
	}

	public void OnDone()
	{
		uint primaryColor = _bannerEditorLayer.DataSource.BannerVM.Banner.GetPrimaryColor();
		uint firstIconColor = _bannerEditorLayer.DataSource.BannerVM.Banner.GetFirstIconColor();
		_clan.Color2 = firstIconColor;
		if (_bannerEditorLayer.DataSource.CanChangeBackgroundColor)
		{
			_clan.Color = primaryColor;
			_clan.UpdateBannerColor(primaryColor, firstIconColor);
		}
		else
		{
			_clan.UpdateBannerColor(_clan.Color, firstIconColor);
		}
		Game.Current.GameStateManager.PopState();
	}

	public void OnCancel()
	{
		Game.Current.GameStateManager.PopState();
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
		InformationManager.HideAllMessages();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_bannerEditorLayer.OnFinalize();
		if (LoadingWindow.IsLoadingWindowActive)
		{
			LoadingWindow.DisableGlobalLoadingWindow();
		}
		Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		AddLayer(_bannerEditorLayer.GauntletLayer);
		AddLayer(_bannerEditorLayer.SceneLayer);
	}

	protected override void OnDeactivate()
	{
		_bannerEditorLayer.OnDeactivate();
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}
}
