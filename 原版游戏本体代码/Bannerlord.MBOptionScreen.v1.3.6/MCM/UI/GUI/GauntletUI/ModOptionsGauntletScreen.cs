using System;
using System.Runtime.CompilerServices;
using MCM.UI.GUI.ViewModels;
using Microsoft.Extensions.Logging;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace MCM.UI.GUI.GauntletUI
{
	// Token: 0x02000023 RID: 35
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ModOptionsGauntletScreen : ScreenBase, IMCMOptionsScreen
	{
		// Token: 0x06000170 RID: 368 RVA: 0x000069AB File Offset: 0x00004BAB
		public ModOptionsGauntletScreen(ILogger<ModOptionsGauntletScreen> logger)
		{
			this._logger = logger;
		}

		// Token: 0x06000171 RID: 369 RVA: 0x000069BC File Offset: 0x00004BBC
		protected override void OnInitialize()
		{
			base.OnInitialize();
			SpriteData spriteData = UIResourceManager.SpriteData;
			TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
			ResourceDepot uiresourceDepot = UIResourceManager.ResourceDepot;
			SpriteCategory spriteCategoryMCMVal;
			this._spriteCategoryMCM = (spriteData.SpriteCategories.TryGetValue("ui_mcm", out spriteCategoryMCMVal) ? spriteCategoryMCMVal : null);
			SpriteCategory spriteCategoryMCM = this._spriteCategoryMCM;
			if (spriteCategoryMCM != null)
			{
				spriteCategoryMCM.Load(resourceContext, uiresourceDepot);
			}
			this._dataSource = new ModOptionsVM();
			this._gauntletLayer = new GauntletLayer("GauntletLayer", 4000, false);
			this._gauntletMovie = this._gauntletLayer.LoadMovie("ModOptionsView_MCM", this._dataSource);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.IsFocusLayer = true;
			base.AddLayer(this._gauntletLayer);
			ScreenManager.TrySetFocus(this._gauntletLayer);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00006AA0 File Offset: 0x00004CA0
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (this._gauntletLayer != null && this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				this._dataSource.ExecuteClose();
				ScreenManager.TryLoseFocus(this._gauntletLayer);
				ScreenManager.PopScreen();
			}
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00006AF0 File Offset: 0x00004CF0
		protected override void OnFinalize()
		{
			base.OnFinalize();
			if (this._spriteCategoryMCM != null)
			{
				this._spriteCategoryMCM.Unload();
			}
			if (this._gauntletLayer != null)
			{
				base.RemoveLayer(this._gauntletLayer);
			}
			if (this._gauntletLayer != null && this._gauntletMovie != null)
			{
				this._gauntletLayer.ReleaseMovie(this._gauntletMovie);
			}
			this._gauntletLayer = null;
			this._gauntletMovie = null;
			this._dataSource.ExecuteSelect(null);
			this._dataSource = null;
		}

		// Token: 0x0400005B RID: 91
		private readonly ILogger<ModOptionsGauntletScreen> _logger;

		// Token: 0x0400005C RID: 92
		[Nullable(2)]
		private GauntletLayer _gauntletLayer;

		// Token: 0x0400005D RID: 93
		[Nullable(2)]
		private GauntletMovieIdentifier _gauntletMovie;

		// Token: 0x0400005E RID: 94
		private ModOptionsVM _dataSource;

		// Token: 0x0400005F RID: 95
		[Nullable(2)]
		private SpriteCategory _spriteCategoryMCM;
	}
}
