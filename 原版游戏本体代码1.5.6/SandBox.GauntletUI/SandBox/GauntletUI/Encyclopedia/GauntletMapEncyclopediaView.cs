using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Encyclopedia;

[OverrideView(typeof(MapEncyclopediaView))]
public class GauntletMapEncyclopediaView : MapEncyclopediaView
{
	private EncyclopediaHomeVM _homeDatasource;

	private EncyclopediaNavigatorVM _navigatorDatasource;

	private EncyclopediaData _encyclopediaData;

	public EncyclopediaListViewDataController ListViewDataController;

	private SpriteCategory _spriteCategory;

	private Game _game;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_encyclopedia");
		_homeDatasource = new EncyclopediaHomeVM(new EncyclopediaPageArgs(null));
		_navigatorDatasource = new EncyclopediaNavigatorVM(ExecuteLink, CloseEncyclopedia);
		ListViewDataController = new EncyclopediaListViewDataController();
		_game = Game.Current;
		Game game = _game;
		game.AfterTick = (Action<float>)Delegate.Combine(game.AfterTick, new Action<float>(OnTick));
	}

	internal void OnTick(float dt)
	{
		_encyclopediaData?.OnTick();
	}

	private EncyclopediaPageVM ExecuteLink(string pageId, object obj, bool needsRefresh)
	{
		_navigatorDatasource.NavBarString = string.Empty;
		if (_encyclopediaData == null)
		{
			_encyclopediaData = new EncyclopediaData(this, ScreenManager.TopScreen, _homeDatasource, _navigatorDatasource);
		}
		if (pageId == "LastPage")
		{
			Tuple<string, object> lastPage = _navigatorDatasource.GetLastPage();
			pageId = lastPage.Item1;
			obj = lastPage.Item2;
		}
		base.IsEncyclopediaOpen = true;
		if (!_spriteCategory.IsLoaded)
		{
			_spriteCategory.Load();
		}
		return _encyclopediaData.ExecuteLink(pageId, obj, needsRefresh);
	}

	protected override void OnFinalize()
	{
		Game game = _game;
		game.AfterTick = (Action<float>)Delegate.Remove(game.AfterTick, new Action<float>(OnTick));
		_game = null;
		_homeDatasource?.OnFinalize();
		_homeDatasource = null;
		_navigatorDatasource?.OnFinalize();
		_navigatorDatasource = null;
		_encyclopediaData = null;
		base.OnFinalize();
	}

	public override void CloseEncyclopedia()
	{
		_encyclopediaData.CloseEncyclopedia();
		_encyclopediaData = null;
		base.IsEncyclopediaOpen = false;
	}

	protected override TutorialContexts GetTutorialContext()
	{
		if (base.IsEncyclopediaOpen)
		{
			return TutorialContexts.EncyclopediaWindow;
		}
		return base.GetTutorialContext();
	}
}
