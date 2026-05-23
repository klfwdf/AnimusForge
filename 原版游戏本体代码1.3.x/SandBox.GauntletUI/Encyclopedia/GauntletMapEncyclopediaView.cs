using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Encyclopedia
{
	// Token: 0x02000047 RID: 71
	[OverrideView(typeof(MapEncyclopediaView))]
	public class GauntletMapEncyclopediaView : MapEncyclopediaView
	{
		// Token: 0x06000331 RID: 817 RVA: 0x00012E78 File Offset: 0x00011078
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._spriteCategory = UIResourceManager.LoadSpriteCategory("ui_encyclopedia");
			this._homeDatasource = new EncyclopediaHomeVM(new EncyclopediaPageArgs(null));
			this._navigatorDatasource = new EncyclopediaNavigatorVM(new Func<string, object, bool, EncyclopediaPageVM>(this.ExecuteLink), new Action(this.CloseEncyclopedia));
			this.ListViewDataController = new EncyclopediaListViewDataController();
			this._game = Game.Current;
			Game game = this._game;
			game.AfterTick = (Action<float>)Delegate.Combine(game.AfterTick, new Action<float>(this.OnTick));
		}

		// Token: 0x06000332 RID: 818 RVA: 0x00012F0D File Offset: 0x0001110D
		internal void OnTick(float dt)
		{
			EncyclopediaData encyclopediaData = this._encyclopediaData;
			if (encyclopediaData == null)
			{
				return;
			}
			encyclopediaData.OnTick();
		}

		// Token: 0x06000333 RID: 819 RVA: 0x00012F20 File Offset: 0x00011120
		private EncyclopediaPageVM ExecuteLink(string pageId, object obj, bool needsRefresh)
		{
			this._navigatorDatasource.NavBarString = string.Empty;
			if (this._encyclopediaData == null)
			{
				this._encyclopediaData = new EncyclopediaData(this, ScreenManager.TopScreen, this._homeDatasource, this._navigatorDatasource);
			}
			if (pageId == "LastPage")
			{
				Tuple<string, object> lastPage = this._navigatorDatasource.GetLastPage();
				pageId = lastPage.Item1;
				obj = lastPage.Item2;
			}
			base.IsEncyclopediaOpen = true;
			if (!this._spriteCategory.IsLoaded)
			{
				this._spriteCategory.Load();
			}
			return this._encyclopediaData.ExecuteLink(pageId, obj, needsRefresh);
		}

		// Token: 0x06000334 RID: 820 RVA: 0x00012FB8 File Offset: 0x000111B8
		protected override void OnFinalize()
		{
			Game game = this._game;
			game.AfterTick = (Action<float>)Delegate.Remove(game.AfterTick, new Action<float>(this.OnTick));
			this._game = null;
			EncyclopediaHomeVM homeDatasource = this._homeDatasource;
			if (homeDatasource != null)
			{
				homeDatasource.OnFinalize();
			}
			this._homeDatasource = null;
			EncyclopediaNavigatorVM navigatorDatasource = this._navigatorDatasource;
			if (navigatorDatasource != null)
			{
				navigatorDatasource.OnFinalize();
			}
			this._navigatorDatasource = null;
			this._encyclopediaData = null;
			base.OnFinalize();
		}

		// Token: 0x06000335 RID: 821 RVA: 0x00013030 File Offset: 0x00011230
		public override void CloseEncyclopedia()
		{
			this._encyclopediaData.CloseEncyclopedia();
			this._encyclopediaData = null;
			base.IsEncyclopediaOpen = false;
		}

		// Token: 0x06000336 RID: 822 RVA: 0x0001304B File Offset: 0x0001124B
		protected override TutorialContexts GetTutorialContext()
		{
			if (base.IsEncyclopediaOpen)
			{
				return TutorialContexts.EncyclopediaWindow;
			}
			return base.GetTutorialContext();
		}

		// Token: 0x04000143 RID: 323
		private EncyclopediaHomeVM _homeDatasource;

		// Token: 0x04000144 RID: 324
		private EncyclopediaNavigatorVM _navigatorDatasource;

		// Token: 0x04000145 RID: 325
		private EncyclopediaData _encyclopediaData;

		// Token: 0x04000146 RID: 326
		public EncyclopediaListViewDataController ListViewDataController;

		// Token: 0x04000147 RID: 327
		private SpriteCategory _spriteCategory;

		// Token: 0x04000148 RID: 328
		private Game _game;
	}
}
